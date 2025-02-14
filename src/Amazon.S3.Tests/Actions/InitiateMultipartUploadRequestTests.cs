﻿namespace Amazon.S3.Actions.Tests;

public class InitiateMultipartUploadRequestTests
{
    [Fact]
    public void Construct()
    {
        var request = new InitiateMultipartUploadRequest("s3.aws.com", "bucket-name", "object-key")
        {
            ContentType = "application/json"
        };

        Assert.Equal("/bucket-name/object-key?uploads", request.RequestUri.PathAndQuery);

        Assert.Equal("application/json", request.Content.Headers.ContentType.ToString());
        Assert.Equal("application/json", request.ContentType);

        request.ContentType = "text/plain";

        Assert.Equal("text/plain", request.Content.Headers.ContentType.ToString());
    }

    [Fact]
    public void Construct2()
    {
        var request = new InitiateMultipartUploadRequest("s3.aws.com", "bucket-name", "object-key", new Dictionary<string, string> {
            { "Content-Type", "image/png" },
            { "Content-Encoding", "gzip" }
        });

        Assert.Equal("image/png", request.ContentType);
        Assert.Equal("gzip", request.Content.Headers.TryGetValues("Content-Encoding", out var values) ? string.Join(';', values) : null);
    }

    [Fact]
    public void Construct3()
    {
        var request = new InitiateMultipartUploadRequest("s3.aws.com", "bucket-name", "object-key", new Dictionary<string, string> {
            { "Content-Type", "image/png" },
            { "Content-Encoding", "gzip" }
        });

        request.ContentType = null;

        Assert.Null(request.ContentType);
    }
}
