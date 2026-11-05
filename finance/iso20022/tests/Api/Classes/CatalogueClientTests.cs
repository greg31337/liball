using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using iso20022;

namespace iso20022.Tests;

public class CatalogueClientTests
{
    [Test]
    public void DownloadCarriesConditionsAndBuildsVerifiedProvenance()
    {
        byte[] bytes = "official catalogue bytes"u8.ToArray();
        HttpResponseMessage response = CreateContentResponse(bytes, Iso20022MediaTypes.Zip);
        response.Headers.ETag = new EntityTagHeaderValue("\"edition-1\"");
        response.Content.Headers.LastModified = new DateTimeOffset(2026, 5, 28, 14, 30, 0, TimeSpan.Zero);
        SequenceHttpHandler handler = new SequenceHttpHandler(response);
        using HttpClient httpClient = new HttpClient(handler);
        ICatalogueClient client = new CatalogueClient(httpClient);
        CatalogueDownloadRequest request = CreateRequest("https://example.test/catalogue.zip");
        request.IfNoneMatch = "\"edition-0\"";
        request.IfModifiedSince = CreateDateTime(2026, 5, 27, 13, 28, 0);
        request.ExpectedHash = new IsoBinaryValue { Value = SHA256.HashData(bytes) };
        request.PublicationDate = new IsoPartialDateValue
        {
            Kind = PartialDateKind.YearMonth,
            Year = 2026,
            Month = 5,
            LexicalValue = "2026-05"
        };
        request.LicenseNotice = "ISO publication terms";
        request.IsApproved = true;

        CatalogueDownloadResult result = client.DownloadCatalogue(request);
        using Stream content = result.Content ?? throw new AssertionException("Expected downloaded content.");
        using MemoryStream copy = new MemoryStream();
        content.CopyTo(copy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.NotModified, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.FinalUri, Is.EqualTo("https://example.test/catalogue.zip"));
            Assert.That(result.EntityTag, Is.EqualTo("\"edition-1\""));
            Assert.That(result.LastModified?.LexicalValue, Is.EqualTo("2026-05-28T14:30:00Z"));
            Assert.That(result.ByteCount, Is.EqualTo(bytes.Length));
            Assert.That(copy.ToArray(), Is.EqualTo(bytes));
            Assert.That(result.Artifact.Coordinate, Is.SameAs(request.Coordinate));
            Assert.That(result.Artifact.OriginUri, Is.EqualTo(request.Uri));
            Assert.That(result.Artifact.RetrievedAt, Is.Not.Null);
            Assert.That(result.Artifact.PublicationDate?.LexicalValue, Is.EqualTo("2026-05"));
            Assert.That(result.Artifact.HashAlgorithm, Is.EqualTo("SHA-256"));
            Assert.That(result.Artifact.Hash.Value, Is.EqualTo(SHA256.HashData(bytes)));
            Assert.That(result.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.Zip));
            Assert.That(result.Artifact.LicenseNotice, Is.EqualTo("ISO publication terms"));
            Assert.That(result.Artifact.IsApproved, Is.True);
            Assert.That(handler.RequestUris, Is.EqualTo(new[] { "https://example.test/catalogue.zip" }));
            Assert.That(handler.IfNoneMatchValues, Is.EqualTo(new[] { "\"edition-0\"" }));
            Assert.That(handler.IfModifiedSinceValues,
                Is.EqualTo(new[] { "Wed, 27 May 2026 13:28:00 GMT" }));
        });
    }

    [Test]
    public void DownloadFollowsRelativeRedirectAndPreservesConditionalHeaders()
    {
        HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.Found);
        redirect.Headers.Location = new Uri("/release/catalogue.json", UriKind.Relative);
        HttpResponseMessage content = CreateContentResponse("{}"u8.ToArray(), Iso20022MediaTypes.Json);
        SequenceHttpHandler handler = new SequenceHttpHandler(redirect, content);
        using HttpClient httpClient = new HttpClient(handler);
        ICatalogueClient client = new CatalogueClient(httpClient);
        CatalogueDownloadRequest request = CreateRequest("http://example.test/start");
        request.AcceptedMediaTypes.Clear();
        request.AcceptedMediaTypes.Add(Iso20022MediaTypes.Json);
        request.IfNoneMatch = "W/\"catalogue\"";
        request.MaxRedirectCount = 2;

        CatalogueDownloadResult result = client.DownloadCatalogue(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FinalUri, Is.EqualTo("http://example.test/release/catalogue.json"));
            Assert.That(result.RedirectUris,
                Is.EqualTo(new[] { "http://example.test/release/catalogue.json" }));
            Assert.That(handler.RequestUris, Is.EqualTo(new[]
            {
                "http://example.test/start",
                "http://example.test/release/catalogue.json"
            }));
            Assert.That(handler.IfNoneMatchValues,
                Is.EqualTo(new[] { "W/\"catalogue\"", "W/\"catalogue\"" }));
        });

        result.Content?.Dispose();
    }

    [Test]
    public void NotModifiedReturnsConditionalMetadataWithoutContent()
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.NotModified);
        response.Headers.ETag = new EntityTagHeaderValue("\"unchanged\"");
        SequenceHttpHandler handler = new SequenceHttpHandler(response);
        using HttpClient httpClient = new HttpClient(handler);
        ICatalogueClient client = new CatalogueClient(httpClient);
        CatalogueDownloadRequest request = CreateRequest("https://example.test/catalogue.zip");
        request.IfNoneMatch = "\"unchanged\"";

        CatalogueDownloadResult result = client.DownloadCatalogue(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.NotModified, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(304));
            Assert.That(result.Content, Is.Null);
            Assert.That(result.ByteCount, Is.Zero);
            Assert.That(result.EntityTag, Is.EqualTo("\"unchanged\""));
            Assert.That(result.Artifact.Coordinate, Is.SameAs(request.Coordinate));
            Assert.That(result.Artifact.RetrievedAt, Is.Not.Null);
        });
    }

    [Test]
    public void DownloadRejectsMissingOrUnexpectedMediaType()
    {
        HttpResponseMessage missingMedia = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 1 })
        };
        HttpResponseMessage wrongMedia = CreateContentResponse(new byte[] { 1 }, "text/plain");
        SequenceHttpHandler handler = new SequenceHttpHandler(missingMedia, wrongMedia);
        using HttpClient httpClient = new HttpClient(handler);
        ICatalogueClient client = new CatalogueClient(httpClient);

        CatalogueDownloadResult missing = client.DownloadCatalogue(
            CreateRequest("https://example.test/missing-media"));
        CatalogueDownloadResult rejected = client.DownloadCatalogue(
            CreateRequest("https://example.test/wrong-media"));

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.ErrorCode, Is.EqualTo("catalogue.media_type_missing"));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.ErrorCode, Is.EqualTo("catalogue.media_type_rejected"));
        });
    }

    [Test]
    public void DownloadEnforcesDeclaredStreamedAndTruncatedSizeBounds()
    {
        HttpResponseMessage declaredResponse = CreateContentResponse(new byte[] { 1, 2, 3 }, Iso20022MediaTypes.Zip);
        declaredResponse.Content.Headers.ContentLength = 100;
        HttpResponseMessage streamedResponse = CreateContentResponse(new byte[] { 1, 2, 3, 4, 5 }, Iso20022MediaTypes.Zip);
        streamedResponse.Content.Headers.ContentLength = null;
        HttpResponseMessage truncatedResponse = CreateContentResponse(new byte[] { 1, 2, 3 }, Iso20022MediaTypes.Zip);
        truncatedResponse.Content.Headers.ContentLength = 10;
        SequenceHttpHandler handler = new SequenceHttpHandler(
            declaredResponse,
            streamedResponse,
            truncatedResponse);
        using HttpClient httpClient = new HttpClient(handler);
        ICatalogueClient client = new CatalogueClient(httpClient);
        CatalogueDownloadRequest declaredRequest = CreateRequest("https://example.test/declared");
        declaredRequest.MaxArtifactBytes = 4;
        CatalogueDownloadRequest streamedRequest = CreateRequest("https://example.test/streamed");
        streamedRequest.MaxArtifactBytes = 4;
        CatalogueDownloadRequest truncatedRequest = CreateRequest("https://example.test/truncated");

        CatalogueDownloadResult declared = client.DownloadCatalogue(declaredRequest);
        CatalogueDownloadResult streamed = client.DownloadCatalogue(streamedRequest);
        CatalogueDownloadResult truncated = client.DownloadCatalogue(truncatedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(declared.ErrorCode, Is.EqualTo("catalogue.size_limit"));
            Assert.That(streamed.ErrorCode, Is.EqualTo("catalogue.size_limit"));
            Assert.That(truncated.ErrorCode, Is.EqualTo("catalogue.content_truncated"));
            Assert.That(declared.Content, Is.Null);
            Assert.That(streamed.Content, Is.Null);
            Assert.That(truncated.Content, Is.Null);
        });
    }

    [Test]
    public void DownloadRejectsHashMismatchAndPreservesNoContent()
    {
        byte[] bytes = "catalogue"u8.ToArray();
        SequenceHttpHandler handler = new SequenceHttpHandler(
            CreateContentResponse(bytes, Iso20022MediaTypes.Zip));
        using HttpClient httpClient = new HttpClient(handler);
        ICatalogueClient client = new CatalogueClient(httpClient);
        CatalogueDownloadRequest request = CreateRequest("https://example.test/catalogue.zip");
        request.ExpectedHash = new IsoBinaryValue { Value = new byte[32] };

        CatalogueDownloadResult result = client.DownloadCatalogue(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("catalogue.hash_mismatch"));
            Assert.That(result.Content, Is.Null);
        });
    }

    [Test]
    public void RedirectLimitAndCancellationStopBeforeAnotherRequest()
    {
        HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
        redirect.Headers.Location = new Uri("https://example.test/next");
        SequenceHttpHandler redirectHandler = new SequenceHttpHandler(redirect);
        using HttpClient redirectHttpClient = new HttpClient(redirectHandler);
        ICatalogueClient redirectClient = new CatalogueClient(redirectHttpClient);
        CatalogueDownloadRequest redirectRequest = CreateRequest("https://example.test/start");
        redirectRequest.MaxRedirectCount = 0;

        CatalogueDownloadResult limited = redirectClient.DownloadCatalogue(redirectRequest);

        SequenceHttpHandler cancellationHandler = new SequenceHttpHandler(
            CreateContentResponse(new byte[] { 1 }, Iso20022MediaTypes.Zip));
        using HttpClient cancellationHttpClient = new HttpClient(cancellationHandler);
        ICatalogueClient cancellationClient = new CatalogueClient(cancellationHttpClient);
        CatalogueDownloadRequest cancellationRequest = CreateRequest("https://example.test/cancelled");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        cancellationRequest.CancellationToken = cancellation.Token;

        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.ErrorCode, Is.EqualTo("catalogue.redirect_limit"));
            Assert.That(redirectHandler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(
                () => cancellationClient.DownloadCatalogue(cancellationRequest),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(cancellationHandler.RequestUris, Is.Empty);
        });
    }

    [Test]
    public void InvalidRequestReturnsFailureWithoutUsingHttpClient()
    {
        SequenceHttpHandler handler = new SequenceHttpHandler(
            CreateContentResponse(new byte[] { 1 }, Iso20022MediaTypes.Zip));
        using HttpClient httpClient = new HttpClient(handler);
        ICatalogueClient client = new CatalogueClient(httpClient);
        CatalogueDownloadRequest request = CreateRequest("file:///tmp/catalogue.zip");

        CatalogueDownloadResult result = client.DownloadCatalogue(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("catalogue.uri_invalid"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    [Test]
    public void RequestValidationReportsEveryIndependentInvalidField()
    {
        CatalogueDownloadRequest coordinate = CreateRequest("https://example.test/catalogue");
        coordinate.Coordinate.Kind = ArtifactKind.Unknown;
        CatalogueDownloadRequest missingCoordinate = CreateRequest("https://example.test/catalogue");
        missingCoordinate.Coordinate = null!;
        CatalogueDownloadRequest media = CreateRequest("https://example.test/catalogue");
        media.AcceptedMediaTypes.Clear();
        CatalogueDownloadRequest badMedia = CreateRequest("https://example.test/catalogue");
        badMedia.AcceptedMediaTypes[0] = "text/plain; charset=utf-8";
        CatalogueDownloadRequest redirects = CreateRequest("https://example.test/catalogue");
        redirects.MaxRedirectCount = -1;
        CatalogueDownloadRequest size = CreateRequest("https://example.test/catalogue");
        size.MaxArtifactBytes = 0;
        CatalogueDownloadRequest entityTag = CreateRequest("https://example.test/catalogue");
        entityTag.IfNoneMatch = "not-quoted";
        CatalogueDownloadRequest missingEntityTag = CreateRequest("https://example.test/catalogue");
        missingEntityTag.IfNoneMatch = null!;
        CatalogueDownloadRequest modified = CreateRequest("https://example.test/catalogue");
        modified.IfModifiedSince = CreateDateTime(2026, 5, 27, 13, 28, 0);
        modified.IfModifiedSince.Time.HasTimezone = false;
        CatalogueDownloadRequest hashBytes = CreateRequest("https://example.test/catalogue");
        hashBytes.ExpectedHash = new IsoBinaryValue { Value = new byte[31] };
        CatalogueDownloadRequest hashAlgorithm = CreateRequest("https://example.test/catalogue");
        hashAlgorithm.ExpectedHash = new IsoBinaryValue { Value = new byte[32] };
        hashAlgorithm.ExpectedHashAlgorithm = "SHA-1";
        CatalogueDownloadRequest provenance = CreateRequest("https://example.test/catalogue");
        provenance.LicenseNotice = null!;

        Assert.Multiple(() =>
        {
            Assert.That(CatalogueClient.ValidateRequest(coordinate)?.ErrorCode,
                Is.EqualTo("catalogue.coordinate_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(missingCoordinate)?.ErrorCode,
                Is.EqualTo("catalogue.coordinate_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(media)?.ErrorCode,
                Is.EqualTo("catalogue.media_types_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(badMedia)?.ErrorCode,
                Is.EqualTo("catalogue.media_types_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(redirects)?.ErrorCode,
                Is.EqualTo("catalogue.redirect_limit_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(size)?.ErrorCode,
                Is.EqualTo("catalogue.size_limit_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(entityTag)?.ErrorCode,
                Is.EqualTo("catalogue.entity_tag_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(missingEntityTag)?.ErrorCode,
                Is.EqualTo("catalogue.entity_tag_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(modified)?.ErrorCode,
                Is.EqualTo("catalogue.modified_since_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(hashBytes)?.ErrorCode,
                Is.EqualTo("catalogue.expected_hash_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(hashAlgorithm)?.ErrorCode,
                Is.EqualTo("catalogue.expected_hash_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(provenance)?.ErrorCode,
                Is.EqualTo("catalogue.provenance_invalid"));
            Assert.That(CatalogueClient.ValidateRequest(
                CreateRequest("https://example.test/catalogue")), Is.Null);
        });
    }

    [Test]
    public void UriHeaderAndMediaLexicalsCoverAllBoundaryForms()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CatalogueClient.IsHttpUriText(null), Is.False);
            Assert.That(CatalogueClient.IsHttpUriText(string.Empty), Is.False);
            Assert.That(CatalogueClient.IsHttpUriText("https://example.test/a#fragment"), Is.False);
            Assert.That(CatalogueClient.IsHttpUriText("relative"), Is.False);
            Assert.That(CatalogueClient.IsHttpUriText("ftp://example.test/a"), Is.False);
            Assert.That(CatalogueClient.IsHttpUriText("http:///missing"), Is.False);
            Assert.That(CatalogueClient.IsHttpUriText("http://example.test?x=1"), Is.True);
            Assert.That(CatalogueClient.IsHttpUriText("HTTPS://example.test/a"), Is.True);

            Assert.That(CatalogueClient.HttpAuthorityIsUsable("user@example.test"), Is.False);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable("[broken"), Is.False);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable("[::1]x"), Is.False);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable("[::1]:"), Is.False);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable("[::1]:443"), Is.True);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable(":443"), Is.False);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable("example.test"), Is.True);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable("example.test:"), Is.False);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable("example.test:x"), Is.False);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable("example.test:/"), Is.False);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable("example.test:65536"), Is.False);
            Assert.That(CatalogueClient.HttpAuthorityIsUsable("example.test:65535"), Is.True);

            Assert.That(CatalogueClient.MediaTypeIsValid(null), Is.False);
            Assert.That(CatalogueClient.MediaTypeIsValid("a/"), Is.False);
            Assert.That(CatalogueClient.MediaTypeIsValid("/b"), Is.False);
            Assert.That(CatalogueClient.MediaTypeIsValid("a/b/c"), Is.False);
            Assert.That(CatalogueClient.MediaTypeIsValid("a b/c"), Is.False);
            Assert.That(CatalogueClient.MediaTypeIsValid("a/b"), Is.True);
            Assert.That(CatalogueClient.AllMediaTypesAreValid(
                new List<string> { "a/b", "c/d" }), Is.True);
            Assert.That(CatalogueClient.AllMediaTypesAreValid(
                new List<string> { "a/b", "bad" }), Is.False);
            Assert.That(CatalogueClient.MediaTypeIsAccepted(
                "APPLICATION/XML", new List<string> { "application/xml" }), Is.True);
            Assert.That(CatalogueClient.MediaTypeIsAccepted(
                "text/plain", new List<string> { "application/xml" }), Is.False);

            Assert.That(CatalogueClient.EntityTagIsValid(string.Empty), Is.True);
            Assert.That(CatalogueClient.EntityTagIsValid("\"tag\""), Is.True);
            Assert.That(CatalogueClient.EntityTagIsValid("W/\"tag\""), Is.True);
            Assert.That(CatalogueClient.EntityTagIsValid("W/"), Is.False);
            Assert.That(CatalogueClient.EntityTagIsValid("\"unterminated"), Is.False);
            Assert.That(CatalogueClient.EntityTagIsValid("\"bad tag\""), Is.False);
            Assert.That(CatalogueClient.EntityTagIsValid("\"bad\"tag\""), Is.False);
            Assert.That(CatalogueClient.IsSha256(null), Is.False);
            Assert.That(CatalogueClient.IsSha256("sha256"), Is.True);
            Assert.That(CatalogueClient.IsSha256("sha-256"), Is.True);
            Assert.That(CatalogueClient.IsSha256("sha-1"), Is.False);
            Assert.That(CatalogueClient.ExpectedHashMatches(new byte[1], new byte[2]), Is.False);
            Assert.That(CatalogueClient.ExpectedHashMatches(new byte[] { 1 }, new byte[] { 1 }),
                Is.True);
        });
    }

    [Test]
    public void RedirectAndConditionalHelpersPreserveStableOutcomes()
    {
        using HttpResponseMessage automatic = new HttpResponseMessage(HttpStatusCode.OK);
        automatic.RequestMessage = new HttpRequestMessage(
            HttpMethod.Get, "https://example.test/final");
        using HttpResponseMessage unchanged = new HttpResponseMessage(HttpStatusCode.OK);
        unchanged.RequestMessage = new HttpRequestMessage(
            HttpMethod.Get, "https://example.test/start");
        List<string> redirects = new List<string> { "https://example.test/second" };
        CatalogueDownloadRequest conditional = CreateRequest("https://example.test/catalogue");
        conditional.IfNoneMatch = "\"tag\"";
        conditional.IfModifiedSince = CreateDateTime(2026, 5, 27, 13, 28, 0);
        using HttpRequestMessage message = CatalogueClient.CreateHttpRequest(
            new Uri(conditional.Uri), conditional);

        Assert.Multiple(() =>
        {
            Assert.That(CatalogueClient.DetectAutomaticRedirect(
                new Uri("https://example.test/start"), automatic, redirects)?.ErrorCode,
                Is.EqualTo("catalogue.automatic_redirect"));
            Assert.That(CatalogueClient.DetectAutomaticRedirect(
                new Uri("https://example.test/start"), unchanged, redirects), Is.Null);
            Assert.That(CatalogueClient.IsRedirectStatus(HttpStatusCode.MovedPermanently), Is.True);
            Assert.That(CatalogueClient.IsRedirectStatus(HttpStatusCode.Found), Is.True);
            Assert.That(CatalogueClient.IsRedirectStatus(HttpStatusCode.SeeOther), Is.True);
            Assert.That(CatalogueClient.IsRedirectStatus(HttpStatusCode.TemporaryRedirect), Is.True);
            Assert.That(CatalogueClient.IsRedirectStatus(HttpStatusCode.PermanentRedirect), Is.True);
            Assert.That(CatalogueClient.IsRedirectStatus(HttpStatusCode.OK), Is.False);
            Assert.That(CatalogueClient.RedirectWasVisited(
                "a", redirects, "a"), Is.True);
            Assert.That(CatalogueClient.RedirectWasVisited(
                "a", redirects, "https://example.test/second"), Is.True);
            Assert.That(CatalogueClient.RedirectWasVisited(
                "a", redirects, "missing"), Is.False);
            Assert.That(message.Headers.GetValues("If-None-Match").Single(), Is.EqualTo("\"tag\""));
            Assert.That(message.Headers.GetValues("If-Modified-Since").Single(),
                Is.EqualTo("Wed, 27 May 2026 13:28:00 GMT"));
        });
    }

    [Test]
    public void ConstructorNullRequestAndRedirectFailuresAreExplicit()
    {
        Assert.That(() => new CatalogueClient(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("client"));

        SequenceHttpHandler unused = new SequenceHttpHandler(
            CreateContentResponse(new byte[] { 1 }, Iso20022MediaTypes.Zip));
        using HttpClient unusedClient = new HttpClient(unused);
        CatalogueDownloadResult missing = new CatalogueClient(unusedClient)
            .DownloadCatalogue(null);

        HttpResponseMessage noLocation = new HttpResponseMessage(HttpStatusCode.Found);
        HttpResponseMessage ftp = new HttpResponseMessage(HttpStatusCode.Found);
        ftp.Headers.Location = new Uri("ftp://example.test/catalogue");
        HttpResponseMessage downgrade = new HttpResponseMessage(HttpStatusCode.Found);
        downgrade.Headers.Location = new Uri("http://example.test/catalogue");
        HttpResponseMessage loopOne = new HttpResponseMessage(HttpStatusCode.Found);
        loopOne.Headers.Location = new Uri("https://example.test/second");
        HttpResponseMessage loopTwo = new HttpResponseMessage(HttpStatusCode.Found);
        loopTwo.Headers.Location = new Uri("https://example.test/start");
        SequenceHttpHandler handler = new SequenceHttpHandler(
            noLocation, ftp, downgrade, loopOne, loopTwo);
        using HttpClient client = new HttpClient(handler);
        CatalogueClient catalogue = new CatalogueClient(client);

        CatalogueDownloadResult missingLocation = catalogue.DownloadCatalogue(
            CreateRequest("https://example.test/missing-location"));
        CatalogueDownloadResult invalidTarget = catalogue.DownloadCatalogue(
            CreateRequest("https://example.test/invalid-target"));
        CatalogueDownloadResult insecure = catalogue.DownloadCatalogue(
            CreateRequest("https://example.test/downgrade"));
        CatalogueDownloadResult loop = catalogue.DownloadCatalogue(
            CreateRequest("https://example.test/start"));

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCode, Is.EqualTo("catalogue.request_missing"));
            Assert.That(unused.RequestUris, Is.Empty);
            Assert.That(missingLocation.ErrorCode,
                Is.EqualTo("catalogue.redirect_location_missing"));
            Assert.That(invalidTarget.ErrorCode,
                Is.EqualTo("catalogue.redirect_uri_invalid"));
            Assert.That(insecure.ErrorCode,
                Is.EqualTo("catalogue.redirect_downgrade"));
            Assert.That(loop.ErrorCode, Is.EqualTo("catalogue.redirect_loop"));
        });
    }

    [Test]
    public void AutomaticRedirectHttpFailureAndPartialContentAreRejected()
    {
        HttpResponseMessage automatic = CreateContentResponse(
            new byte[] { 1 }, Iso20022MediaTypes.Zip);
        automatic.RequestMessage = new HttpRequestMessage(
            HttpMethod.Get, "https://example.test/final");
        HttpResponseMessage failure = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        failure.Headers.ETag = new EntityTagHeaderValue("\"retry\"");
        failure.Content = new ByteArrayContent(Array.Empty<byte>());
        failure.Content.Headers.LastModified = new DateTimeOffset(
            2026, 5, 28, 14, 30, 0, TimeSpan.Zero);
        HttpResponseMessage belowSuccess = new HttpResponseMessage((HttpStatusCode)199);
        belowSuccess.Content = new ByteArrayContent(Array.Empty<byte>());
        HttpResponseMessage partial = CreateContentResponse(
            new byte[] { 1 }, Iso20022MediaTypes.Zip);
        partial.StatusCode = HttpStatusCode.PartialContent;
        SequenceHttpHandler handler = new SequenceHttpHandler(
            automatic, failure, belowSuccess, partial);
        using HttpClient client = new HttpClient(handler);
        CatalogueClient catalogue = new CatalogueClient(client);

        CatalogueDownloadResult redirected = catalogue.DownloadCatalogue(
            CreateRequest("https://example.test/start"));
        CatalogueDownloadResult unavailable = catalogue.DownloadCatalogue(
            CreateRequest("https://example.test/unavailable"));
        CatalogueDownloadResult tooLow = catalogue.DownloadCatalogue(
            CreateRequest("https://example.test/below-success"));
        CatalogueDownloadResult incomplete = catalogue.DownloadCatalogue(
            CreateRequest("https://example.test/partial"));

        Assert.Multiple(() =>
        {
            Assert.That(redirected.ErrorCode, Is.EqualTo("catalogue.automatic_redirect"));
            Assert.That(unavailable.ErrorCode, Is.EqualTo("catalogue.http_status"));
            Assert.That(unavailable.StatusCode, Is.EqualTo(503));
            Assert.That(unavailable.EntityTag, Is.EqualTo("\"retry\""));
            Assert.That(unavailable.LastModified, Is.Not.Null);
            Assert.That(tooLow.ErrorCode, Is.EqualTo("catalogue.http_status"));
            Assert.That(incomplete.ErrorCode, Is.EqualTo("catalogue.partial_content"));
        });
    }

    [Test]
    public void StreamCancellationAfterReadIsObservedAndDiscardsContent()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StreamContent(new CancellingReadStream(
            new byte[] { 1, 2, 3 }, cancellation));
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(Iso20022MediaTypes.Zip);
        CatalogueDownloadRequest request = CreateRequest("https://example.test/cancel");
        request.CancellationToken = cancellation.Token;

        Assert.That(() => CatalogueClient.ReadBoundedContent(response, request),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void UndeclaredContentLengthCanCompleteSuccessfully()
    {
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StreamContent(new MemoryStream(new byte[] { 1, 2, 3 }));
        response.Content.Headers.ContentLength = null;
        CatalogueDownloadRequest request = CreateRequest("https://example.test/stream");

        CatalogueContentReadResult result = CatalogueClient.ReadBoundedContent(response, request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ByteCount, Is.EqualTo(3));
            Assert.That(result.Content.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
        result.Content.Dispose();
    }

    private static CatalogueDownloadRequest CreateRequest(string uri)
    {
        CatalogueDownloadRequest request = new CatalogueDownloadRequest
        {
            Uri = uri,
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.ExternalCodeSet,
                Edition = "2026-Q1",
                Identifier = "external-code-sets",
                Version = "1"
            },
            MaxArtifactBytes = 4096
        };
        request.AcceptedMediaTypes.Add(Iso20022MediaTypes.Zip);
        return request;
    }

    private static IsoDateTimeValue CreateDateTime(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        return new IsoDateTimeValue
        {
            Date = new IsoDateValue
            {
                Year = year,
                Month = month,
                Day = day,
                HasTimezone = true,
                IsUtc = true
            },
            Time = new IsoTimeValue
            {
                Hour = hour,
                Minute = minute,
                Second = second,
                HasTimezone = true,
                IsUtc = true
            }
        };
    }

    private static HttpResponseMessage CreateContentResponse(byte[] bytes, string mediaType)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return response;
    }

    private sealed class SequenceHttpHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses;

        internal List<string> RequestUris = new List<string>();
        internal List<string> IfNoneMatchValues = new List<string>();
        internal List<string> IfModifiedSinceValues = new List<string>();

        internal SequenceHttpHandler(params HttpResponseMessage[] responses)
        {
            this.responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestUris.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);
            IfNoneMatchValues.Add(ReadHeader(request, "If-None-Match"));
            IfModifiedSinceValues.Add(ReadHeader(request, "If-Modified-Since"));
            return responses.Dequeue();
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Send(request, cancellationToken));
        }

        private static string ReadHeader(HttpRequestMessage request, string name)
        {
            if (!request.Headers.Contains(name))
            {
                return string.Empty;
            }

            return request.Headers.GetValues(name).Single();
        }
    }

    private sealed class CancellingReadStream : MemoryStream
    {
        private readonly CancellationTokenSource cancellation;

        internal CancellingReadStream(
            byte[] content,
            CancellationTokenSource cancellation) : base(content)
        {
            this.cancellation = cancellation;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = base.Read(buffer, offset, count);
            if (read > 0)
            {
                cancellation.Cancel();
            }
            return read;
        }
    }
}
