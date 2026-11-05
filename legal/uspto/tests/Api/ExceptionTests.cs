using uspto;

namespace uspto.Tests;

public class ExceptionTests
{
    [Test]
    public void ApiExceptionRetainsRemoteFailureContext()
    {
        InvalidOperationException cause = new InvalidOperationException("transport failed");
        ApiException exception = new ApiException(
            "The request failed.",
            ApiService.Odp,
            500,
            true,
            "SERVER_ERROR",
            "A bounded service error.",
            "request-123",
            true,
            cause);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("The request failed."));
            Assert.That(exception.InnerException, Is.SameAs(cause));
            Assert.That(exception.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(exception.StatusCode, Is.EqualTo(500));
            Assert.That(exception.HasStatusCode, Is.True);
            Assert.That(exception.ErrorCode, Is.EqualTo("SERVER_ERROR"));
            Assert.That(exception.ErrorDetails, Is.EqualTo("A bounded service error."));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("request-123"));
            Assert.That(exception.IsRetryable, Is.True);
        });
    }

    [Test]
    public void AuthenticationAndNotFoundExceptionsDistinguishFailureKinds()
    {
        AuthenticationException authentication = new AuthenticationException(
            "Authentication failed.",
            ApiService.Tsdr,
            403,
            true,
            "auth-request",
            null);
        NotFoundException notFound = new NotFoundException(
            "The application was not found.",
            ApiService.Odp,
            "16123456",
            404,
            true,
            "lookup-request",
            null);

        Assert.Multiple(() =>
        {
            Assert.That(authentication.Service, Is.EqualTo(ApiService.Tsdr));
            Assert.That(authentication.StatusCode, Is.EqualTo(403));
            Assert.That(authentication.HasStatusCode, Is.True);
            Assert.That(authentication.RequestIdentifier, Is.EqualTo("auth-request"));
            Assert.That(notFound.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(notFound.ResourceIdentifier, Is.EqualTo("16123456"));
            Assert.That(notFound.StatusCode, Is.EqualTo(404));
            Assert.That(notFound.RequestIdentifier, Is.EqualTo("lookup-request"));
        });
    }

    [Test]
    public void RateExceptionsPreserveOptionalRetryTiming()
    {
        RateLimitException rateLimit = new RateLimitException(
            "Rate limited.",
            ApiService.TsdrPdfAndZip,
            429,
            true,
            "rate-request",
            15000L,
            true,
            null);
        ServiceUnavailableException unavailable = new ServiceUnavailableException(
            "Service unavailable.",
            ApiService.PatentsView,
            503,
            true,
            "service-request",
            0L,
            false,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(rateLimit.Service, Is.EqualTo(ApiService.TsdrPdfAndZip));
            Assert.That(rateLimit.RetryAfterMilliseconds, Is.EqualTo(15000L));
            Assert.That(rateLimit.HasRetryAfterMilliseconds, Is.True);
            Assert.That(unavailable.Service, Is.EqualTo(ApiService.PatentsView));
            Assert.That(unavailable.StatusCode, Is.EqualTo(503));
            Assert.That(unavailable.HasStatusCode, Is.True);
            Assert.That(unavailable.RetryAfterMilliseconds, Is.Zero);
            Assert.That(unavailable.HasRetryAfterMilliseconds, Is.False);
        });
    }

    [Test]
    public void TransferAndParsingExceptionsRetainTypedContext()
    {
        Uri source = new Uri("https://bulkdata.uspto.gov/file.zip");
        DownloadException download = new DownloadException(
            "Download interrupted.",
            source,
            "/data/file.zip",
            DownloadState.Failed,
            4096L,
            0,
            false,
            true,
            null);
        ChecksumException checksum = new ChecksumException(
            "Checksum mismatch.",
            ChecksumAlgorithm.Sha256,
            "AABB",
            "CCDD",
            "/data/file.zip",
            null);
        ArchiveException archive = new ArchiveException(
            "Unsafe archive entry.",
            CompressionFormat.Zip,
            "/data/file.zip",
            "../escape.xml",
            null);
        ParseException parse = new ParseException(
            "Malformed JSON.",
            DocumentFormat.Json,
            "response.json",
            "$.results[2]",
            814L,
            true,
            23,
            true,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(download.SourceUri, Is.SameAs(source));
            Assert.That(download.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(download.BytesTransferred, Is.EqualTo(4096L));
            Assert.That(download.HasStatusCode, Is.False);
            Assert.That(download.IsRetryable, Is.True);
            Assert.That(checksum.Algorithm, Is.EqualTo(ChecksumAlgorithm.Sha256));
            Assert.That(checksum.ExpectedChecksum, Is.EqualTo("AABB"));
            Assert.That(checksum.ActualChecksum, Is.EqualTo("CCDD"));
            Assert.That(archive.Format, Is.EqualTo(CompressionFormat.Zip));
            Assert.That(archive.EntryName, Is.EqualTo("../escape.xml"));
            Assert.That(parse.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(parse.DataPath, Is.EqualTo("$.results[2]"));
            Assert.That(parse.Offset, Is.EqualTo(814L));
            Assert.That(parse.HasOffset, Is.True);
            Assert.That(parse.LineNumber, Is.EqualTo(23));
            Assert.That(parse.HasLineNumber, Is.True);
        });
    }

    [Test]
    public void LocalOperationExceptionsRetainActionableContext()
    {
        InvalidOperationException cause = new InvalidOperationException("disk error");
        ValidationException validation = new ValidationException(
            "The page size is invalid.",
            "PageSize",
            "OUT_OF_RANGE",
            null);
        CacheException cache = new CacheException(
            "Cache entry is corrupt.",
            "sha256:key",
            "/cache/entry",
            cause);
        MirrorException mirror = new MirrorException(
            "Mirror verification failed.",
            DatasetFamily.OfficeAction,
            UsptoDatasetIdentifiers.OfficeActionWeekly,
            "archive.zip",
            MirrorAction.Corrupt,
            cause);

        Assert.Multiple(() =>
        {
            Assert.That(validation.FieldName, Is.EqualTo("PageSize"));
            Assert.That(validation.ErrorCode, Is.EqualTo("OUT_OF_RANGE"));
            Assert.That(cache.CacheKey, Is.EqualTo("sha256:key"));
            Assert.That(cache.CachePath, Is.EqualTo("/cache/entry"));
            Assert.That(cache.InnerException, Is.SameAs(cause));
            Assert.That(mirror.Family, Is.EqualTo(DatasetFamily.OfficeAction));
            Assert.That(mirror.ProductIdentifier, Is.EqualTo("OACT"));
            Assert.That(mirror.FileName, Is.EqualTo("archive.zip"));
            Assert.That(mirror.Action, Is.EqualTo(MirrorAction.Corrupt));
            Assert.That(mirror.InnerException, Is.SameAs(cause));
        });
    }
}
