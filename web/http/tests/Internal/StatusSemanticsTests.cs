namespace http.Tests;

public class StatusSemanticsTests
{
    static readonly int[] RegisteredStatusCodes =
    [
        100, 101, 102, 103, 104,
        200, 201, 202, 203, 204, 205, 206, 207, 208, 226,
        300, 301, 302, 303, 304, 305, 306, 307, 308,
        400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412,
        413, 414, 415, 416, 417, 418, 421, 422, 423, 424, 425, 426, 428, 429,
        431, 451,
        500, 501, 502, 503, 504, 505, 506, 507, 508, 510, 511
    ];

    static readonly int[] DefaultCacheableStatusCodes =
    [
        200, 203, 204, 206, 300, 301, 308, 404, 405, 410, 414, 501
    ];

    static readonly int[] RedirectStatusCodes = [300, 301, 302, 303, 305, 307, 308];
    static readonly int[] PostToGetStatusCodes = [301, 302, 303];
    static readonly int[] RetryableStatusCodes = [408, 413, 421, 425, 429, 500, 502, 503, 504];

    [Test]
    public void ClassificationCoversEveryValidStatusClassAndBoundary()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StatusSemantics.Classify(99), Is.EqualTo(HttpStatusClass.Invalid));
            Assert.That(StatusSemantics.Classify(100), Is.EqualTo(HttpStatusClass.Informational));
            Assert.That(StatusSemantics.Classify(199), Is.EqualTo(HttpStatusClass.Informational));
            Assert.That(StatusSemantics.Classify(200), Is.EqualTo(HttpStatusClass.Successful));
            Assert.That(StatusSemantics.Classify(299), Is.EqualTo(HttpStatusClass.Successful));
            Assert.That(StatusSemantics.Classify(300), Is.EqualTo(HttpStatusClass.Redirection));
            Assert.That(StatusSemantics.Classify(399), Is.EqualTo(HttpStatusClass.Redirection));
            Assert.That(StatusSemantics.Classify(400), Is.EqualTo(HttpStatusClass.ClientError));
            Assert.That(StatusSemantics.Classify(499), Is.EqualTo(HttpStatusClass.ClientError));
            Assert.That(StatusSemantics.Classify(500), Is.EqualTo(HttpStatusClass.ServerError));
            Assert.That(StatusSemantics.Classify(599), Is.EqualTo(HttpStatusClass.ServerError));
            Assert.That(StatusSemantics.Classify(600), Is.EqualTo(HttpStatusClass.Extension));
            Assert.That(StatusSemantics.Classify(999), Is.EqualTo(HttpStatusClass.Extension));
            Assert.That(StatusSemantics.Classify(1000), Is.EqualTo(HttpStatusClass.Invalid));
        });
    }

    [Test]
    public void EverySnapshotCodeHasRegisteredMetadataAndReasonPhrase()
    {
        Assert.That(RegisteredStatusCodes, Has.Length.EqualTo(64));
        Assert.Multiple(() =>
        {
            foreach (int statusCode in RegisteredStatusCodes)
            {
                HttpStatusMetadata metadata = StatusSemantics.Describe(statusCode);
                Assert.That(metadata.StatusCode, Is.EqualTo(statusCode), statusCode.ToString());
                Assert.That(metadata.IsRegistered, Is.True, statusCode.ToString());
                Assert.That(metadata.ReasonPhrase, Is.Not.Null.And.Not.Empty, statusCode.ToString());
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(StatusSemantics.Describe(104).ReasonPhrase,
                Is.EqualTo("Upload Resumption Supported"));
            Assert.That(StatusSemantics.Describe(200).ReasonPhrase, Is.EqualTo("OK"));
            Assert.That(StatusSemantics.Describe(418).ReasonPhrase, Is.EqualTo("(Unused)"));
            Assert.That(StatusSemantics.Describe(510).ReasonPhrase, Is.EqualTo("Not Extended"));
        });
    }

    [Test]
    public void UnknownValidCodesRetainClassWithConservativeMetadata()
    {
        int[] unknown = [199, 218, 399, 430, 599, 699, 999];

        Assert.Multiple(() =>
        {
            foreach (int statusCode in unknown)
            {
                HttpStatusMetadata metadata = StatusSemantics.Describe(statusCode);
                Assert.That(metadata.StatusClass, Is.Not.EqualTo(HttpStatusClass.Invalid));
                Assert.That(metadata.IsRegistered, Is.False, statusCode.ToString());
                Assert.That(metadata.ReasonPhrase, Is.Null, statusCode.ToString());
                Assert.That(metadata.IsCacheableByDefault, Is.False, statusCode.ToString());
                Assert.That(metadata.IsRedirect, Is.False, statusCode.ToString());
                Assert.That(metadata.RedirectsPostToGet, Is.False, statusCode.ToString());
                Assert.That(metadata.IsRetryable, Is.False, statusCode.ToString());
            }
        });
    }

    [Test]
    public void InvalidCodesAreRejectedByMetadataLookup()
    {
        int[] invalid = [-1, 0, 99, 1000, int.MaxValue];

        Assert.Multiple(() =>
        {
            foreach (int statusCode in invalid)
            {
                HttpProtocolException? exception = Assert.Throws<HttpProtocolException>(() =>
                    StatusSemantics.Describe(statusCode));
                Assert.That(
                    exception!.DiagnosticCode,
                    Is.EqualTo("invalid-status-code"),
                    statusCode.ToString());
            }
        });
    }

    [Test]
    public void ContentAllowanceFollowsStatusIndependentRules()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StatusSemantics.Describe(100).AllowsContent, Is.False);
            Assert.That(StatusSemantics.Describe(150).AllowsContent, Is.False);
            Assert.That(StatusSemantics.Describe(204).AllowsContent, Is.False);
            Assert.That(StatusSemantics.Describe(205).AllowsContent, Is.False);
            Assert.That(StatusSemantics.Describe(304).AllowsContent, Is.False);
            Assert.That(StatusSemantics.Describe(200).AllowsContent, Is.True);
            Assert.That(StatusSemantics.Describe(299).AllowsContent, Is.True);
            Assert.That(StatusSemantics.Describe(399).AllowsContent, Is.True);
            Assert.That(StatusSemantics.Describe(404).AllowsContent, Is.True);
            Assert.That(StatusSemantics.Describe(599).AllowsContent, Is.True);
            Assert.That(StatusSemantics.Describe(699).AllowsContent, Is.True);
        });
    }

    [Test]
    public void CacheRedirectAndRetryMetadataUseDefinedStatusSets()
    {
        Assert.Multiple(() =>
        {
            foreach (int statusCode in RegisteredStatusCodes)
            {
                HttpStatusMetadata metadata = StatusSemantics.Describe(statusCode);
                Assert.That(
                    metadata.IsCacheableByDefault,
                    Is.EqualTo(DefaultCacheableStatusCodes.Contains(statusCode)),
                    statusCode.ToString());
                Assert.That(
                    metadata.IsRedirect,
                    Is.EqualTo(RedirectStatusCodes.Contains(statusCode)),
                    statusCode.ToString());
                Assert.That(
                    metadata.RedirectsPostToGet,
                    Is.EqualTo(PostToGetStatusCodes.Contains(statusCode)),
                    statusCode.ToString());
                Assert.That(
                    metadata.IsRetryable,
                    Is.EqualTo(RetryableStatusCodes.Contains(statusCode)),
                    statusCode.ToString());
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(StatusSemantics.Describe(304).IsRedirect, Is.False);
            Assert.That(StatusSemantics.Describe(307).RedirectsPostToGet, Is.False);
            Assert.That(StatusSemantics.Describe(308).RedirectsPostToGet, Is.False);
            Assert.That(StatusSemantics.Describe(503).IsRetryable, Is.True);
        });
    }
}
