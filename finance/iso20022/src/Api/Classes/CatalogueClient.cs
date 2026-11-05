using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;

namespace iso20022;

/// <summary>Downloads verified ISO catalogue artifacts through a caller-provided HTTP client.</summary>
public class CatalogueClient : ICatalogueClient
{
    internal HttpClient Client;

    /// <summary>Initializes a catalogue client using an HTTP client configured to expose redirect responses.</summary>
    /// <param name="client">The caller-owned HTTP client, whose handler must not automatically follow redirects.</param>
    /// <exception cref="ArgumentNullException">Thrown when the HTTP client is null.</exception>
    public CatalogueClient(HttpClient client)
    {
        if (client == null)
        {
            throw new ArgumentNullException("client");
        }

        Client = client;
    }

    /// <summary>Downloads one catalogue artifact and captures its integrity provenance.</summary>
    /// <param name="request">The URI, conditional headers, limits, and provenance inputs.</param><returns>The download result.</returns>
    public CatalogueDownloadResult DownloadCatalogue(CatalogueDownloadRequest? request)
    {
        if (request == null)
        {
            return CreateFailure(
                "catalogue.request_missing",
                "A catalogue download request is required.",
                string.Empty,
                0,
                new List<string>());
        }

        CatalogueDownloadResult? validation = ValidateRequest(request);
        if (validation != null)
        {
            return validation;
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        Uri currentUri = new Uri(request.Uri, UriKind.Absolute);
        List<string> redirects = new List<string>();
        int redirectCount = 0;
        while (true)
        {
            using (HttpRequestMessage message = CreateHttpRequest(currentUri, request))
            using (HttpResponseMessage response = Client.Send(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                request.CancellationToken))
            {
                CatalogueDownloadResult? automaticRedirect = DetectAutomaticRedirect(
                    currentUri,
                    response,
                    redirects);
                if (automaticRedirect != null)
                {
                    return automaticRedirect;
                }

                if (IsRedirectStatus(response.StatusCode))
                {
                    if (redirectCount >= request.MaxRedirectCount)
                    {
                        return CreateFailure(
                            "catalogue.redirect_limit",
                            "The catalogue response exceeded the configured redirect limit.",
                            currentUri.AbsoluteUri,
                            (int)response.StatusCode,
                            redirects);
                    }

                    Uri? location = response.Headers.Location;
                    if (location == null)
                    {
                        return CreateFailure(
                            "catalogue.redirect_location_missing",
                            "The catalogue redirect response does not contain a Location header.",
                            currentUri.AbsoluteUri,
                            (int)response.StatusCode,
                            redirects);
                    }

                    Uri targetUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
                    if (!IsHttpUriText(targetUri.AbsoluteUri))
                    {
                        return CreateFailure(
                            "catalogue.redirect_uri_invalid",
                            "The catalogue redirect target is not an absolute HTTP or HTTPS URI.",
                            currentUri.AbsoluteUri,
                            (int)response.StatusCode,
                            redirects);
                    }

                    if (currentUri.Scheme == "https" && targetUri.Scheme != "https")
                    {
                        return CreateFailure(
                            "catalogue.redirect_downgrade",
                            "An HTTPS catalogue download cannot redirect to an insecure URI.",
                            currentUri.AbsoluteUri,
                            (int)response.StatusCode,
                            redirects);
                    }

                    if (RedirectWasVisited(request.Uri, redirects, targetUri.AbsoluteUri))
                    {
                        return CreateFailure(
                            "catalogue.redirect_loop",
                            "The catalogue redirect chain contains a loop.",
                            currentUri.AbsoluteUri,
                            (int)response.StatusCode,
                            redirects);
                    }

                    redirects.Add(targetUri.AbsoluteUri);
                    currentUri = targetUri;
                    redirectCount++;
                    continue;
                }

                return ProcessFinalResponse(currentUri, response, request, redirects);
            }
        }
    }

    internal static CatalogueDownloadResult ProcessFinalResponse(
        Uri currentUri,
        HttpResponseMessage response,
        CatalogueDownloadRequest request,
        List<string> redirects)
    {
        int statusCode = (int)response.StatusCode;
        string entityTag = ReadEntityTag(response);
        IsoDateTimeValue? lastModified = ReadLastModified(response);
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            CatalogueDownloadResult unchanged = CreateResponseResult(
                currentUri,
                statusCode,
                entityTag,
                lastModified,
                redirects);
            unchanged.Success = true;
            unchanged.NotModified = true;
            unchanged.Artifact = CreateConditionalManifest(request, currentUri);
            return unchanged;
        }

        if (statusCode < 200 || statusCode > 299)
        {
            CatalogueDownloadResult failure = CreateFailure(
                "catalogue.http_status",
                "The catalogue server returned a non-success HTTP status.",
                currentUri.AbsoluteUri,
                statusCode,
                redirects);
            failure.EntityTag = entityTag;
            failure.LastModified = lastModified;
            return failure;
        }

        if (response.StatusCode == HttpStatusCode.PartialContent)
        {
            return CreateFailure(
                "catalogue.partial_content",
                "A catalogue artifact download must return complete content.",
                currentUri.AbsoluteUri,
                statusCode,
                redirects);
        }

        string mediaType = ReadMediaType(response);
        if (mediaType.Length == 0)
        {
            return CreateFailure(
                "catalogue.media_type_missing",
                "The catalogue response does not declare a media type.",
                currentUri.AbsoluteUri,
                statusCode,
                redirects);
        }

        if (!MediaTypeIsAccepted(mediaType, request.AcceptedMediaTypes))
        {
            return CreateFailure(
                "catalogue.media_type_rejected",
                "The catalogue response media type is not accepted by the request.",
                currentUri.AbsoluteUri,
                statusCode,
                redirects);
        }

        if (response.Content.Headers.ContentLength.HasValue &&
            response.Content.Headers.ContentLength.Value > request.MaxArtifactBytes)
        {
            return CreateFailure(
                "catalogue.size_limit",
                "The declared catalogue content length exceeds the configured byte limit.",
                currentUri.AbsoluteUri,
                statusCode,
                redirects);
        }

        CatalogueContentReadResult content = ReadBoundedContent(response, request);
        if (!content.Success)
        {
            content.Content.Dispose();
            return CreateFailure(
                content.ErrorCode,
                content.ErrorMessage,
                currentUri.AbsoluteUri,
                statusCode,
                redirects);
        }

        if (request.ExpectedHash != null &&
            !ExpectedHashMatches(request.ExpectedHash.Value, content.Hash))
        {
            content.Content.Dispose();
            return CreateFailure(
                "catalogue.hash_mismatch",
                "The downloaded catalogue content does not match the expected SHA-256 hash.",
                currentUri.AbsoluteUri,
                statusCode,
                redirects);
        }

        ArtifactManifest manifest = CreateDownloadedManifest(
            request,
            currentUri,
            mediaType,
            content.Hash);
        CatalogueDownloadResult result = CreateResponseResult(
            currentUri,
            statusCode,
            entityTag,
            lastModified,
            redirects);
        result.Success = true;
        result.Content = content.Content;
        result.Artifact = manifest;
        result.ByteCount = content.ByteCount;
        return result;
    }

    internal static CatalogueContentReadResult ReadBoundedContent(
        HttpResponseMessage response,
        CatalogueDownloadRequest request)
    {
        CatalogueContentReadResult result = new CatalogueContentReadResult();
        byte[] buffer = new byte[81920];
        using (Stream source = response.Content.ReadAsStream(request.CancellationToken))
        using (IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            int read = source.Read(buffer, 0, buffer.Length);
            while (read > 0)
            {
                if (request.CancellationToken.IsCancellationRequested)
                {
                    result.Content.Dispose();
                }
                request.CancellationToken.ThrowIfCancellationRequested();

                if (result.ByteCount > request.MaxArtifactBytes - read)
                {
                    result.ErrorCode = "catalogue.size_limit";
                    result.ErrorMessage = "The catalogue content exceeds the configured byte limit.";
                    return result;
                }

                result.Content.Write(buffer, 0, read);
                hash.AppendData(buffer, 0, read);
                result.ByteCount = result.ByteCount + read;
                read = source.Read(buffer, 0, buffer.Length);
            }

            result.Hash = hash.GetHashAndReset();
        }

        if (response.Content.Headers.ContentLength.HasValue &&
            response.Content.Headers.ContentLength.Value != result.ByteCount)
        {
            result.ErrorCode = "catalogue.content_truncated";
            result.ErrorMessage = "The catalogue content length does not match the declared response length.";
            return result;
        }

        result.Content.Position = 0;
        result.Success = true;
        return result;
    }

    internal static HttpRequestMessage CreateHttpRequest(
        Uri uri,
        CatalogueDownloadRequest request)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, uri);
        if (request.IfNoneMatch.Length > 0)
        {
            message.Headers.TryAddWithoutValidation("If-None-Match", request.IfNoneMatch);
        }

        if (request.IfModifiedSince != null)
        {
            message.Headers.TryAddWithoutValidation(
                "If-Modified-Since",
                FormatHttpDate(request.IfModifiedSince));
        }

        return message;
    }

    internal static CatalogueDownloadResult? ValidateRequest(CatalogueDownloadRequest request)
    {
        List<string> redirects = new List<string>();
        if (!IsHttpUriText(request.Uri))
        {
            return CreateFailure(
                "catalogue.uri_invalid",
                "An absolute HTTP or HTTPS catalogue URI without a fragment is required.",
                string.Empty,
                0,
                redirects);
        }

        if (request.Coordinate == null || !CatalogueCoordinateIsValid(request.Coordinate))
        {
            return CreateFailure(
                "catalogue.coordinate_invalid",
                "A complete non-unknown artifact coordinate is required.",
                request.Uri,
                0,
                redirects);
        }

        if (request.AcceptedMediaTypes == null || request.AcceptedMediaTypes.Count == 0 ||
            !AllMediaTypesAreValid(request.AcceptedMediaTypes))
        {
            return CreateFailure(
                "catalogue.media_types_invalid",
                "At least one exact accepted media type is required.",
                request.Uri,
                0,
                redirects);
        }

        if (request.MaxRedirectCount < 0)
        {
            return CreateFailure(
                "catalogue.redirect_limit_invalid",
                "The maximum redirect count cannot be negative.",
                request.Uri,
                0,
                redirects);
        }

        if (request.MaxArtifactBytes <= 0)
        {
            return CreateFailure(
                "catalogue.size_limit_invalid",
                "The maximum catalogue byte count must be positive.",
                request.Uri,
                0,
                redirects);
        }

        if (request.IfNoneMatch == null || !EntityTagIsValid(request.IfNoneMatch))
        {
            return CreateFailure(
                "catalogue.entity_tag_invalid",
                "The conditional entity tag is not valid.",
                request.Uri,
                0,
                redirects);
        }

        if (request.IfModifiedSince != null && !HttpDateTimeIsValid(request.IfModifiedSince))
        {
            return CreateFailure(
                "catalogue.modified_since_invalid",
                "The conditional modification time is not a valid timezone-qualified HTTP date.",
                request.Uri,
                0,
                redirects);
        }

        if (request.ExpectedHash != null &&
            (request.ExpectedHash.Value == null || request.ExpectedHash.Value.Length != 32 ||
            !IsSha256(request.ExpectedHashAlgorithm)))
        {
            return CreateFailure(
                "catalogue.expected_hash_invalid",
                "An expected hash must be a 32-byte SHA-256 value.",
                request.Uri,
                0,
                redirects);
        }

        if (request.LicenseNotice == null)
        {
            return CreateFailure(
                "catalogue.provenance_invalid",
                "The catalogue license notice cannot be null.",
                request.Uri,
                0,
                redirects);
        }

        return null;
    }

    internal static CatalogueDownloadResult? DetectAutomaticRedirect(
        Uri requestedUri,
        HttpResponseMessage response,
        List<string> redirects)
    {
        HttpRequestMessage? responseRequest = response.RequestMessage;
        Uri? responseUri = responseRequest?.RequestUri;
        if (responseUri != null && responseUri.AbsoluteUri != requestedUri.AbsoluteUri)
        {
            return CreateFailure(
                "catalogue.automatic_redirect",
                "The supplied HTTP client followed a redirect automatically; explicit redirect responses are required.",
                responseUri.AbsoluteUri,
                (int)response.StatusCode,
                redirects);
        }

        return null;
    }

    internal static bool IsRedirectStatus(HttpStatusCode status)
    {
        int value = (int)status;
        return value == 301 || value == 302 || value == 303 || value == 307 || value == 308;
    }

    internal static bool RedirectWasVisited(
        string initialUri,
        List<string> redirects,
        string targetUri)
    {
        if (initialUri == targetUri)
        {
            return true;
        }

        foreach (string redirect in redirects)
        {
            if (redirect == targetUri)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsHttpUriText(string? value)
    {
        if (value == null || value.Length == 0 || value.IndexOf('#') >= 0 ||
            !UriLexical.IsAbsoluteUri(value.AsSpan()))
        {
            return false;
        }

        int authorityStart = 0;
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            authorityStart = 7;
        }
        else if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            authorityStart = 8;
        }
        else
        {
            return false;
        }

        int authorityEnd = authorityStart;
        while (authorityEnd < value.Length && value[authorityEnd] != '/' &&
            value[authorityEnd] != '?' && value[authorityEnd] != '#')
        {
            authorityEnd++;
        }

        if (authorityEnd == authorityStart)
        {
            return false;
        }

        return HttpAuthorityIsUsable(value.Substring(authorityStart, authorityEnd - authorityStart));
    }

    internal static bool HttpAuthorityIsUsable(string authority)
    {
        int index = 0;
        while (index < authority.Length)
        {
            if (authority[index] == '@')
            {
                return false;
            }

            index++;
        }

        int portStart = -1;
        if (authority[0] == '[')
        {
            int closingBracket = authority.IndexOf(']');
            if (closingBracket < 2)
            {
                return false;
            }

            if (closingBracket + 1 < authority.Length)
            {
                if (authority[closingBracket + 1] != ':')
                {
                    return false;
                }

                portStart = closingBracket + 2;
            }
        }
        else
        {
            int colon = authority.IndexOf(':');
            if (colon == 0)
            {
                return false;
            }

            if (colon > 0)
            {
                portStart = colon + 1;
            }
        }

        if (portStart < 0)
        {
            return true;
        }

        if (portStart >= authority.Length)
        {
            return false;
        }

        int port = 0;
        index = portStart;
        while (index < authority.Length)
        {
            char character = authority[index];
            if (character < '0' || character > '9')
            {
                return false;
            }

            int digit = character - '0';
            if (port > (65535 - digit) / 10)
            {
                return false;
            }

            port = port * 10 + digit;
            index++;
        }

        return true;
    }

    internal static bool CatalogueCoordinateIsValid(ArtifactCoordinate coordinate)
    {
        return coordinate.Kind != ArtifactKind.Unknown &&
            coordinate.Edition != null && coordinate.Edition.Length > 0 &&
            coordinate.Identifier != null && coordinate.Identifier.Length > 0 &&
            coordinate.Version != null && coordinate.Version.Length > 0;
    }

    internal static bool AllMediaTypesAreValid(List<string> mediaTypes)
    {
        foreach (string mediaType in mediaTypes)
        {
            if (!MediaTypeIsValid(mediaType))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool MediaTypeIsValid(string? mediaType)
    {
        if (mediaType == null || mediaType.Length < 3)
        {
            return false;
        }

        int slash = -1;
        int index = 0;
        while (index < mediaType.Length)
        {
            char character = mediaType[index];
            if (character == '/')
            {
                if (slash >= 0 || index == 0 || index == mediaType.Length - 1)
                {
                    return false;
                }

                slash = index;
            }
            else if (character <= 32 || character >= 127 || character == ';' || character == ',')
            {
                return false;
            }

            index++;
        }

        return slash > 0;
    }

    internal static bool MediaTypeIsAccepted(string mediaType, List<string> accepted)
    {
        foreach (string candidate in accepted)
        {
            if (string.Equals(mediaType, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool EntityTagIsValid(string value)
    {
        if (value.Length == 0)
        {
            return true;
        }

        int quoteIndex = 0;
        if (value.StartsWith("W/", StringComparison.Ordinal))
        {
            quoteIndex = 2;
        }

        if (quoteIndex >= value.Length || value[quoteIndex] != '"' ||
            value[value.Length - 1] != '"' || value.Length == quoteIndex + 1)
        {
            return false;
        }

        int index = quoteIndex + 1;
        while (index < value.Length - 1)
        {
            char character = value[index];
            if (character == '"' || character < 33 || character == 127)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool HttpDateTimeIsValid(IsoDateTimeValue value)
    {
        if (value.Date == null || value.Time == null ||
            value.Date.Year < 1 || value.Date.Year > 9999 ||
            value.Time.Hour < 0 || value.Time.Hour > 23 ||
            value.Time.Minute < 0 || value.Time.Minute > 59 ||
            value.Time.Second < 0 || value.Time.Second > 59 ||
            !value.Date.HasTimezone || !value.Time.HasTimezone ||
            value.Date.TimezoneOffsetMinutes != value.Time.TimezoneOffsetMinutes ||
            value.Date.IsUtc != value.Time.IsUtc ||
            value.Date.IsNegativeTimezoneZero != value.Time.IsNegativeTimezoneZero)
        {
            return false;
        }

        return DateTimeLexical.FormatDateTime(value) != null;
    }

    internal static string FormatHttpDate(IsoDateTimeValue value)
    {
        TimeSpan offset = TimeSpan.FromMinutes(value.Time.TimezoneOffsetMinutes);
        DateTimeOffset date = new DateTimeOffset(
            value.Date.Year,
            value.Date.Month,
            value.Date.Day,
            value.Time.Hour,
            value.Time.Minute,
            value.Time.Second,
            offset);
        return date.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture);
    }

    internal static bool IsSha256(string? value)
    {
        return value != null &&
            (string.Equals(value, "SHA-256", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "SHA256", StringComparison.OrdinalIgnoreCase));
    }

    internal static bool ExpectedHashMatches(byte[] expected, byte[] actual)
    {
        if (expected.Length != actual.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    internal static string ReadMediaType(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentType == null ||
            response.Content.Headers.ContentType.MediaType == null)
        {
            return string.Empty;
        }

        return response.Content.Headers.ContentType.MediaType;
    }

    internal static string ReadEntityTag(HttpResponseMessage response)
    {
        if (response.Headers.ETag == null)
        {
            return string.Empty;
        }

        return response.Headers.ETag.ToString();
    }

    internal static IsoDateTimeValue? ReadLastModified(HttpResponseMessage response)
    {
        if (!response.Content.Headers.LastModified.HasValue)
        {
            return null;
        }

        return CreateIsoDateTime(response.Content.Headers.LastModified.Value);
    }

    internal static IsoDateTimeValue CreateIsoDateTime(DateTimeOffset source)
    {
        DateTimeOffset value = source.ToUniversalTime();
        IsoDateTimeValue result = new IsoDateTimeValue();
        result.Date = new IsoDateValue();
        result.Date.Year = value.Year;
        result.Date.Month = value.Month;
        result.Date.Day = value.Day;
        result.Date.HasTimezone = true;
        result.Date.IsUtc = true;
        result.Date.LexicalMode = DateTimeLexicalMode.Normalize;
        result.Date.LexicalValue = value.ToString("yyyy-MM-dd'Z'", CultureInfo.InvariantCulture);
        result.Time = new IsoTimeValue();
        result.Time.Hour = value.Hour;
        result.Time.Minute = value.Minute;
        result.Time.Second = value.Second;
        result.Time.HasTimezone = true;
        result.Time.IsUtc = true;
        result.Time.LexicalMode = DateTimeLexicalMode.Normalize;
        result.Time.LexicalValue = value.ToString("HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        result.LexicalMode = DateTimeLexicalMode.Normalize;
        result.LexicalValue = value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        return result;
    }

    internal static ArtifactManifest CreateDownloadedManifest(
        CatalogueDownloadRequest request,
        Uri finalUri,
        string mediaType,
        byte[] hash)
    {
        ArtifactManifest manifest = CreateConditionalManifest(request, finalUri);
        manifest.Hash = new IsoBinaryValue();
        manifest.Hash.Value = hash;
        manifest.Hash.Encoding = BinaryEncodingKind.Hexadecimal;
        manifest.Hash.LexicalValue = BinaryLexical.FormatHexadecimal(hash);
        manifest.HashAlgorithm = "SHA-256";
        manifest.MediaType = mediaType;
        return manifest;
    }

    internal static ArtifactManifest CreateConditionalManifest(
        CatalogueDownloadRequest request,
        Uri finalUri)
    {
        ArtifactManifest manifest = new ArtifactManifest();
        manifest.Coordinate = request.Coordinate;
        manifest.OriginUri = request.Uri;
        manifest.RetrievedAt = CreateIsoDateTime(DateTimeOffset.UtcNow);
        manifest.PublicationDate = request.PublicationDate;
        manifest.LicenseNotice = request.LicenseNotice;
        manifest.IsApproved = request.IsApproved;
        return manifest;
    }

    internal static CatalogueDownloadResult CreateResponseResult(
        Uri finalUri,
        int statusCode,
        string entityTag,
        IsoDateTimeValue? lastModified,
        List<string> redirects)
    {
        CatalogueDownloadResult result = new CatalogueDownloadResult();
        result.FinalUri = finalUri.AbsoluteUri;
        result.StatusCode = statusCode;
        result.EntityTag = entityTag;
        result.LastModified = lastModified;
        result.RedirectUris = redirects;
        return result;
    }

    internal static CatalogueDownloadResult CreateFailure(
        string errorCode,
        string errorMessage,
        string finalUri,
        int statusCode,
        List<string> redirects)
    {
        CatalogueDownloadResult result = new CatalogueDownloadResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.FinalUri = finalUri;
        result.StatusCode = statusCode;
        result.RedirectUris = redirects;
        return result;
    }
}
