﻿#pragma warning disable IDE0057 // Use range operator

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using Amazon.Helpers;

namespace Amazon.Security;

public static class SignerV4
{
    private const string algorithmName     = "AWS4-HMAC-SHA256";
    private const string isoDateTimeFormat = "yyyyMMddTHHmmssZ";  // ISO8601

    public static string GetStringToSign(in CredentialScope scope, HttpRequestMessage request)
    {
        return GetStringToSign(scope, request, out _);
    }

    public static string GetStringToSign(in CredentialScope scope, HttpRequestMessage request, out List<string> signedHeaders)
    {
        string timestamp = request.Headers.NonValidated.TryGetValues("x-amz-date", out var xAmzDateHeader)
            ? xAmzDateHeader.ToString()
            : throw new Exception("Missing 'x-amz-date' header");

        return GetStringToSign(
            scope            : scope,
            timestamp        : timestamp,
            canonicalRequest : GetCanonicalRequest(request, out signedHeaders)
        );
    }

    [SkipLocalsInit]
    public static string GetStringToSign(in CredentialScope scope, string timestamp, string canonicalRequest)
    {
        ArgumentNullException.ThrowIfNull(canonicalRequest);

        Span<byte> sha256 = stackalloc byte[32];

        SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest), destination: sha256);

        var output = new ValueStringBuilder(256);  // avg ~138

        output.Append(algorithmName)  ; output.Append('\n');    // Algorithm + \n
        output.Append(timestamp)      ; output.Append('\n');    // Timestamp + \n
        scope.AppendTo(ref output)    ; output.Append('\n');    // Scope     + \n
        HexString.DecodeBytesTo(sha256, output.AppendSpan(64)); // Hex(SHA256(CanonicalRequest))

        return output.ToString();
    }

    // Timestamp format: ISO8601 Basic format, YYYYMMDD'T'HHMMSS'Z'

    public static string GetCanonicalRequest(HttpRequestMessage request)
    {
        return GetCanonicalRequest(request, out _);
    }
        
    public static string GetCanonicalRequest(HttpRequestMessage request, out List<string> signedHeaderNames)
    {
        ArgumentNullException.ThrowIfNull(request);

        var output = new ValueStringBuilder(512);

        output.Append(request.Method.Method)                                  ; output.Append('\n'); // HTTPRequestMethod      + \n
        WriteCanonicalizedUri(ref output, request.RequestUri!.AbsolutePath)   ; output.Append('\n'); // CanonicalURI           + \n
        WriteCanonicalizedQueryString(ref output, request.RequestUri)         ; output.Append('\n'); // CanonicalQueryString   + \n
        WriteCanonicalizedHeaders(ref output, request, out signedHeaderNames) ; output.Append('\n'); // CanonicalHeaders       + \n
        output.Append(string.Empty)                                           ; output.Append('\n'); //                        + \n
        output.AppendJoin(';', signedHeaderNames)                             ; output.Append('\n'); // SignedHeaders          + \n
        output.Append(GetPayloadHash(request));                                                      // HexEncode(Hash(Payload))

        return output.ToString();
    }     

    public static string CanonicalizeUri(string path)
    {
        if (path is "/")
        {
            return path;
        }

        var output = new ValueStringBuilder(256);

        WriteCanonicalizedUri(ref output, path);

        return output.ToString();
    }

    private static void WriteCanonicalizedUri(ref ValueStringBuilder output, string path)
    {
        if (path is "/")
        {
            output.Append('/');
            return;
        }

        var splitter = new Splitter(path.AsSpan(), '/');

        while (splitter.TryGetNext(out var segment))
        {
            if (segment.Length is 0) continue;

            output.Append('/');

            // Do not double escape
            if (segment.Contains('%'))
            {
                output.Append(segment);
            }
            else
            {
                output.Append(Uri.EscapeDataString(segment.ToString()));
            }
        }
    }

    public static string GetCanonicalRequest(
        HttpMethod method,
        string canonicalURI,
        string canonicalQueryString,
        string canonicalHeaders,
        string signedHeaders,
        string payloadHash)
    {
        var sb = new ValueStringBuilder(512);

        sb.Append(method.Method);        sb.Append('\n'); // HTTPRequestMethod           + \n
        sb.Append(canonicalURI);         sb.Append('\n'); // CanonicalURI                + \n
        sb.Append(canonicalQueryString); sb.Append('\n'); // CanonicalQueryString        + \n
        sb.Append(canonicalHeaders);     sb.Append('\n'); // CanonicalHeaders            + \n
        sb.Append(string.Empty);         sb.Append('\n'); //                             + \n
        sb.Append(signedHeaders);        sb.Append('\n'); // SignedHeaders               + \n
        sb.Append(payloadHash);                           // HexEncode(Hash(Payload))

        return sb.ToString();
    }

    // HexEncode(Hash(Payload))
    // If the payload is empty, use an empty string
    private static string GetPayloadHash(HttpRequestMessage request)
    {
        // http://docs.aws.amazon.com/AmazonS3/latest/API/sigv4-streaming.html
        // x-amz-content-sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b785

        // STREAMING-AWS4-HMAC-SHA256-PAYLOAD
        // UNSIGNED-PAYLOAD

        return request.Headers.NonValidated.TryGetValues("x-amz-content-sha256", out var contentSha256Header)
            ? contentSha256Header.ToString()
            : ComputeSHA256(request.Content);
    }

    private static readonly byte[] aws4_request_bytes = Encoding.ASCII.GetBytes("aws4_request");

    [SkipLocalsInit]
    public static byte[] ComputeSigningKey(string secretAccessKey, in CredentialScope scope)
    {
        static byte[] GetBytes(string text) => Encoding.ASCII.GetBytes(text);

        byte[] kSecret = GetBytes("AWS4" + secretAccessKey);

        Span<byte> formattedDateBytes = stackalloc byte[8];

        scope.FormatDateTo(formattedDateBytes);

        var signingKey = GC.AllocateUninitializedArray<byte>(32);

        HMACSHA256.HashData(kSecret,     formattedDateBytes,     signingKey);
        HMACSHA256.HashData(signingKey,  scope.Region.Utf8Name,  signingKey);
        HMACSHA256.HashData(signingKey,  scope.Service.Utf8Name, signingKey);
        HMACSHA256.HashData(signingKey,  aws4_request_bytes,     signingKey);

        return signingKey;
    }

    // http://docs.aws.amazon.com/general/latest/gr/sigv4-add-signature-to-request.html

    public static void Presign(
        IAwsCredential credential,
        CredentialScope scope,
        DateTime date,
        TimeSpan expires,
        HttpRequestMessage request,
        string payloadHash = emptySha256)
    {
        ArgumentNullException.ThrowIfNull(request);

        string presignedUrl = GetPresignedUrl(
            credential  : credential,
            scope       : scope,
            date        : date,
            expires     : expires,
            method      : request.Method,
            requestUri  : request.RequestUri!,
            payloadHash : payloadHash
        );

        request.RequestUri = new Uri(presignedUrl);
    }

    public static string GetPresignedUrl(
        IAwsCredential credential,
        CredentialScope scope,
        DateTime date,
        TimeSpan expires,
        HttpMethod method,
        Uri requestUri,
        string payloadHash = emptySha256)
    {
        const string signedHeaders = "host";

        byte[] signingKey = ComputeSigningKey(credential.SecretAccessKey, scope);

        SortedDictionary<string, string> queryParameters = requestUri.Query is { Length: > 0 } queryString
            ? ParseQueryString(queryString)
            : new SortedDictionary<string, string>();
      
        // 16 chars
        string timestamp = date.ToString(format: isoDateTimeFormat, CultureInfo.InvariantCulture);

        queryParameters[SigningParameterNames.Algorithm]  = algorithmName;
        queryParameters[SigningParameterNames.Credential] = $"{credential.AccessKeyId}/{scope}";
        queryParameters[SigningParameterNames.Date]       = timestamp;
        queryParameters[SigningParameterNames.Expires]    = expires.TotalSeconds.ToString(CultureInfo.InvariantCulture); // in seconds

        if (credential.SecurityToken is not null)
        {
            queryParameters[SigningParameterNames.SecurityToken] = credential.SecurityToken;
        }

        queryParameters[SigningParameterNames.SignedHeaders] = signedHeaders;

        string canonicalHeaders = requestUri.IsDefaultPort
            ? "host:" + requestUri.Host
            : string.Create(CultureInfo.InvariantCulture, $"host:{requestUri.Host}:{requestUri.Port}");

        string canonicalRequest = GetCanonicalRequest(
            method               : method,
            canonicalURI         : CanonicalizeUri(requestUri.AbsolutePath),
            canonicalQueryString : CanonicalizeQueryString(queryParameters),
            canonicalHeaders     : canonicalHeaders,
            signedHeaders        : signedHeaders,
            payloadHash          : payloadHash
        );

        string stringToSign = GetStringToSign(
            scope,
            timestamp,
            canonicalRequest
        );
                        
        /*
        queryString = Action=action
        queryString += &X-Amz-Algorithm=algorithm
        queryString += &X-Amz-Credential= urlencode(access_key_ID + '/' + credential_scope)
        queryString += &X-Amz-Date=date
        queryString += &X-Amz-Expires=timeout interval
        queryString += &X-Amz-SignedHeaders=signed_headers
        */

        var urlBuilder = new ValueStringBuilder(512);

        urlBuilder.Append("https://");
        urlBuilder.Append(requestUri.Host);

        if (!requestUri.IsDefaultPort)
        {
            urlBuilder.Append(':');
            urlBuilder.Append(requestUri.Port.ToString(CultureInfo.InvariantCulture));
        }

        urlBuilder.Append(requestUri.AbsolutePath);
        urlBuilder.Append('?');

        foreach (KeyValuePair<string, string> pair in queryParameters)
        {
            urlBuilder.Append(UrlEncoder.Default.Encode(pair.Key));
            urlBuilder.Append('=');
            urlBuilder.Append(UrlEncoder.Default.Encode(pair.Value));
            urlBuilder.Append('&');         
        }


        urlBuilder.Append(SigningParameterNames.Signature);
        urlBuilder.Append('=');
        HMACSHA256_Hex(signingKey, stringToSign, urlBuilder.AppendSpan(64)); //signature

        return urlBuilder.ToString();
    }

    public static void Sign(IAwsCredential credential, in CredentialScope scope, HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(credential);

        // If we're using S3, ensure the request content has been signed
        if (scope.Service.Equals(AwsService.S3) && !request.Headers.NonValidated.Contains("x-amz-content-sha256"))
        {
            request.Headers.TryAddWithoutValidation("x-amz-content-sha256", ComputeSHA256(request.Content));
        }

        byte[] signingKey = ComputeSigningKey(credential.SecretAccessKey, scope);

        string stringToSign = GetStringToSign(scope, request, out var signedHeaderNames);

        var authWriter = new ValueStringBuilder(512);

        // AWS4-HMAC-SHA256 Credential={0},SignedHeaders={0},Signature={0}
        // $"AWS4-HMAC-SHA256 Credential={credential.AccessKeyId}/{scope},SignedHeaders={signedHeaders},Signature={signature}";

        authWriter.Append("AWS4-HMAC-SHA256 Credential=");
        authWriter.Append(credential.AccessKeyId);
        authWriter.Append('/');
        scope.AppendTo(ref authWriter);
        authWriter.Append(",SignedHeaders=");
        authWriter.AppendJoin(';', signedHeaderNames);
        authWriter.Append(",Signature=");
        HMACSHA256_Hex(signingKey, stringToSign, authWriter.AppendSpan(64)); // signature

        request.Headers.TryAddWithoutValidation("Authorization", authWriter.ToString());
    }

    public static string CanonicalizeQueryString(Uri uri)
    {
        if (string.IsNullOrEmpty(uri.Query) || uri.Query is "?")
        {
            return string.Empty;
        }

        return CanonicalizeQueryString(ParseQueryString(uri.Query));
    }

    private static string CanonicalizeQueryString(SortedDictionary<string, string> sortedValues)
    {
        if (sortedValues.Count is 0)
        {
            return string.Empty;
        }

        var output = new ValueStringBuilder(256);

        WriteCanonicalQueryString(ref output, sortedValues);

        return output.ToString();
    }

    private static void WriteCanonicalizedQueryString(ref ValueStringBuilder output, Uri uri)
    {
        if (string.IsNullOrEmpty(uri.Query) || uri.Query is "?")
        {
            return;
        }

        WriteCanonicalQueryString(ref output, ParseQueryString(uri.Query));
    }

    private static void WriteCanonicalQueryString(ref ValueStringBuilder output, SortedDictionary<string, string> sortedValues)
    {
        int i = 0;

        foreach (var pair in sortedValues)
        {
            if (i > 0)
            {
                output.Append('&');
            }

            output.Append(UrlEncoder.Default.Encode(pair.Key));
            output.Append('=');
            output.Append(UrlEncoder.Default.Encode(pair.Value));

            i++;
        }
    }

    private static SortedDictionary<string, string> ParseQueryString(string query)
    {
        return ParseQueryString(query.AsSpan());
    }

    private static SortedDictionary<string, string> ParseQueryString(ReadOnlySpan<char> query)
    {
        if (query.Length is 0)
        {
            return new SortedDictionary<string, string>();
        }

        var dictionary = new SortedDictionary<string, string>();

        if (query[0] is '?')
        {
            query = query[1..];
        }

        var splitter = new Splitter(query, '&');

        while (splitter.TryGetNext(out ReadOnlySpan<char> segment))
        {
            if (segment.Length == 0) continue;

            int equalIndex = segment.IndexOf('=');

            string lhs  = equalIndex > -1 ? segment.Slice(0, equalIndex).ToString() : segment.ToString();
            string? rhs = equalIndex > -1 ? segment.Slice(equalIndex + 1).ToString() : null;

            dictionary[WebUtility.UrlDecode(lhs)] = rhs is not null
                ? WebUtility.UrlDecode(rhs)
                : string.Empty;
        }

        return dictionary;
    }

    public static string CanonicalizeHeaders(HttpRequestMessage request, out List<string> signedHeaderNames)
    {
        var output = new ValueStringBuilder(256);

        WriteCanonicalizedHeaders(ref output, request, out signedHeaderNames);

        return output.ToString();
    }

    private static void WriteCanonicalizedHeaders(ref ValueStringBuilder output, HttpRequestMessage request, out List<string> signedHeaderNames)
    {
        signedHeaderNames = new List<string>(8);

        if (request.Content is not null && request.Content.Headers.NonValidated.TryGetValues("Content-MD5", out var md5Header))
        {
            signedHeaderNames.Add("content-md5");

            output.Append("content-md5:");
            output.Append(md5Header.ToString());
            output.Append('\n');
        }

        if (!request.Headers.NonValidated.Contains("x-amz-date") && request.Headers.NonValidated.TryGetValues("Date", out var dateHeader))
        {
            signedHeaderNames.Add("date");

            output.Append("date:");
            output.Append(dateHeader.ToString());
            output.Append('\n');
        }

        signedHeaderNames.Add("host");

        output.Append("host:");
        output.Append(request.Headers.Host);

        foreach (var header in request.Headers.NonValidated
            .Where(item => item.Key.StartsWith("x-amz-", StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            string headerName = header.Key.ToLowerInvariant();

            output.Append('\n');

            output.Append(headerName);
            output.Append(':');
            output.Append(header.Value.ToString());

            signedHeaderNames.Add(headerName);
        }
    }

    // Convert all header names to lowercase
    // Sort them by character code
    // Use a semicolon to separate the header names

    // The host header must be included as a signed header.

    #region SHA Helpers

    private const string emptySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    [SkipLocalsInit]
    public static string ComputeSHA256(HttpContent? content)
    {
        if (content?.ReadAsByteArrayAsync().Result is byte[] { Length: > 0 } source)
        {
            Span<byte> sha256 = stackalloc byte[32];

            SHA256.HashData(source, sha256);

            return HexString.FromBytes(sha256);
        }
        else
        {
            return emptySha256;
        }
    }

    [SkipLocalsInit]
    private static void HMACSHA256_Hex(byte[] key, ReadOnlySpan<char> data, Span<char> destination)
    {
        var dataBuffer = ArrayPool<byte>.Shared.Rent(data.Length * 4);

        int encodedByteCount = Encoding.UTF8.GetBytes(data, dataBuffer);

        Span<byte> hash = stackalloc byte[32];

        HMACSHA256.HashData(key, dataBuffer.AsSpan(0, encodedByteCount), hash);

        ArrayPool<byte>.Shared.Return(dataBuffer);

        HexString.DecodeBytesTo(hash, destination);
    }

    #endregion
}

// Signed Header Notes ---

// - Convert all header names to lowercase
// - Sort them by character code
// - Use a semicolon to separate the header names