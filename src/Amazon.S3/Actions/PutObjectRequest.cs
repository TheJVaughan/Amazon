﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;

using Amazon.Helpers;

namespace Amazon.S3
{
    public class PutObjectRequest : S3Request
    {
        public PutObjectRequest(string host, string bucketName, string key)
            : base(HttpMethod.Put, host, bucketName, key)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));

            CompletionOption = HttpCompletionOption.ResponseContentRead;
        }

        public void SetStream(Stream stream, string mediaType = "application/octet-stream")
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            if (stream.Length == 0)
                throw new ArgumentException("Must not be empty", nameof(stream));

            if (mediaType is null)
                throw new ArgumentNullException(nameof(mediaType));

            if (mediaType.Length == 0)
                throw new ArgumentException(nameof(mediaType), "Required");

            Content = new StreamContent(stream);

            Content.Headers.ContentLength = stream.Length;

            Content.Headers.Add("Content-Type", mediaType);

            Headers.Add("x-amz-content-sha256", stream.CanSeek 
                ? HexString.FromBytes(ComputeSHA256(stream))
                : "UNSIGNED-PAYLOAD"
            );
        }

        internal void SetCustomerEncryptionKey(in ServerSideEncryptionKey key)
        {
            Headers.Add(S3HeaderNames.ServerSideEncryptionCustomerAlgorithm, key.Algorithm);
            Headers.Add(S3HeaderNames.ServerSideEncryptionCustomerKey,       Convert.ToBase64String(key.Key));
            Headers.Add(S3HeaderNames.ServerSideEncryptionCustomerKeyMD5,    Convert.ToBase64String(key.KeyMD5));
        }

        internal void SetTagSet(IReadOnlyList<KeyValuePair<string, string>> tags)
        {
            if (tags is null || tags.Count == 0) return;

            if (tags.Count > 10)
            {
                throw new ArgumentException("Must be less than 10", nameof(tags));
            }

            // The tag-set for the object. The tag-set must be encoded as URL Query parameters. (For example, "Key1=Value1")

            using var writer = new StringWriter();

            for (int i = 0; i < tags.Count; i++)
            {
                if (i > 0)
                {
                    writer.Write('&');
                }

                KeyValuePair<string, string> tag = tags[i];

                if (tag.Key.Length > 128)
                {
                    throw new ArgumentException("Tag key > 128 chars. Was " + tag.Key);
                }

                if (tag.Value.Length > 256)
                {
                    throw new ArgumentException("Tag value > 256 chars. Was " + tag.Value);
                }

                UrlEncoder.Default.Encode(writer, tag.Key);
                writer.Write('=');
                UrlEncoder.Default.Encode(writer, tag.Value);
            }

            Headers.Add(S3HeaderNames.Tagging, writer.ToString());
        }

        public void SetStream(Stream stream, long length, string mediaType = "application/octet-stream")
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (length <= 0)
            {
                throw new ArgumentException("Must be greater than 0.", nameof(length));
            }

            Content = new StreamContent(stream);
            Content.Headers.ContentLength = length;
            Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

            // TODO: Support chunked streaming...

            Headers.Add("x-amz-content-sha256", stream.CanSeek
                ? HexString.FromBytes(ComputeSHA256(stream))
                : "UNSIGNED-PAYLOAD");
        }
    }
}