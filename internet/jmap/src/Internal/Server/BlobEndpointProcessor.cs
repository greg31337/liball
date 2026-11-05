using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class BlobEndpointProcessor
{
    internal JmapServerOptions Options;
    internal JmapServerServices Services;
    internal RequestLimitGate Limits;
    internal ValueValidator Values = new ValueValidator();
    internal ProblemFactory Problems = new ProblemFactory();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal BlobPushJsonCodec Codec;

    internal BlobEndpointProcessor(JmapServerOptions options, JmapServerServices services)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (services == null)
        {
            throw new ArgumentNullException("services");
        }

        ValidateOptions(options);
        Options = options;
        Services = services;
        Limits = new RequestLimitGate(options);
        Codec = new BlobPushJsonCodec(Reader);
    }

    internal Task<JmapEndpointResponse> ProcessUploadEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => ProcessUploadEndpoint(request, cancellationToken), cancellationToken);
    }

    internal Task<JmapEndpointResponse> ProcessDownloadEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => ProcessDownloadEndpoint(request, cancellationToken), cancellationToken);
    }

    internal JmapEndpointResponse ProcessUploadEndpoint(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return Problem(400, "Invalid Upload Request", "An endpoint request is required.", null);
        }

        if (!string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            JmapEndpointResponse method = Problem(405, "Method Not Allowed", "The upload endpoint requires POST.", null);
            method.Headers.Add("Allow", "POST");
            return method;
        }

        string? accountId = EndpointValue(request, JmapProperties.AccountId);
        JmapEndpointResponse? commonFailure = ValidateCommonRequest(request, accountId);
        if (commonFailure != null)
        {
            return commonFailure;
        }

        if (!IsSafeMediaType(request.ContentType))
        {
            return Problem(415, "Unsupported Media Type", "A valid upload Content-Type is required.", null);
        }

        if (request.Body == null || !request.Body.CanRead)
        {
            return Problem(400, "Invalid Upload Request", "A readable upload body is required.", null);
        }

        RequestLimitAdmission admission = Limits.AcquireUpload(
            request.ContentLength, request.HasContentLength, cancellationToken);
        if (!admission.IsAllowed)
        {
            RequestValidationResult? problem = admission.Problem;
            string? limit = problem == null ? null : problem.Limit;
            if (limit == JmapProperties.MaxSizeUpload)
            {
                return LimitProblem(413, "Upload Too Large",
                    "The upload exceeds maxSizeUpload.", JmapProperties.MaxSizeUpload);
            }

            return LimitProblem(429, "Too Many Uploads",
                "The concurrent upload limit was reached.", JmapProperties.MaxConcurrentUpload);
        }

        RequestLimitLease? uploadLease = admission.Lease;
        if (uploadLease == null)
        {
            throw ProtocolFailure("The upload limit gate did not return a lease.", "upload");
        }

        using RequestLimitLease lease = uploadLease;
        string safeAccountId = accountId == null ? "" : accountId;
        JmapEndpointResponse? accountFailure = ValidateAccount(safeAccountId, request.Context, cancellationToken);
        if (accountFailure != null)
        {
            return accountFailure;
        }

        IBlobService? blobs = Services.BlobService;
        if (blobs == null)
        {
            return Problem(500, "Blob Service Unavailable", "The blob backend is not configured.", null);
        }

        BlobUploadRequest upload = new BlobUploadRequest();
        upload.AccountId = safeAccountId;
        upload.MediaType = request.ContentType;
        upload.Content = request.Body;
        upload.ContentLength = request.ContentLength;
        upload.HasContentLength = request.HasContentLength;
        BlobUploadResponse stored = blobs.StoreUploadedBlobAsync(
            upload,
            request.Context,
            cancellationToken).GetAwaiter().GetResult();
        ValidateStoredUpload(stored, upload);
        string blobId = stored.BlobId == null ? "" : stored.BlobId;
        if (stored.Size > Options.MaxUploadBytes)
        {
            blobs.EvictUnreferencedBlobAsync(
                safeAccountId,
                blobId,
                request.Context,
                cancellationToken).GetAwaiter().GetResult();
            return LimitProblem(413, "Upload Too Large", "The upload exceeds maxSizeUpload.", JmapProperties.MaxSizeUpload);
        }

        RefreshAndEnforceQuota(blobs, stored, request.Context, cancellationToken);
        return UploadResponse(stored);
    }

    internal JmapEndpointResponse ProcessDownloadEndpoint(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return Problem(400, "Invalid Download Request", "An endpoint request is required.", null);
        }

        if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            JmapEndpointResponse method = Problem(405, "Method Not Allowed", "The download endpoint requires GET.", null);
            method.Headers.Add("Allow", "GET");
            return method;
        }

        string? accountId = EndpointValue(request, JmapProperties.AccountId);
        string? blobId = EndpointValue(request, JmapProperties.BlobId);
        string? type = EndpointValue(request, JmapProperties.Type);
        string? name = EndpointValue(request, JmapProperties.Name);
        JmapEndpointResponse? commonFailure = ValidateCommonRequest(request, accountId);
        if (commonFailure != null)
        {
            return commonFailure;
        }

        if (!Values.IsId(blobId) || !Values.IsMediaType(type) || !IsSafeFileName(name))
        {
            return Problem(400, "Invalid Download Request", "The blobId, type, or name route value is invalid.", null);
        }

        string safeAccountId = accountId == null ? "" : accountId;
        JmapEndpointResponse? accountFailure = ValidateAccount(safeAccountId, request.Context, cancellationToken);
        if (accountFailure != null)
        {
            return accountFailure;
        }

        IBlobService? blobs = Services.BlobService;
        if (blobs == null)
        {
            return Problem(500, "Blob Service Unavailable", "The blob backend is not configured.", null);
        }

        BlobDownloadRequest download = new BlobDownloadRequest();
        download.AccountId = safeAccountId;
        download.BlobId = blobId;
        download.Type = type;
        download.Name = name;
        JmapDownloadContent? content = blobs.OpenStoredBlobAsync(
            download,
            request.Context,
            cancellationToken).GetAwaiter().GetResult();
        if (content == null)
        {
            return Problem(404, "Blob Not Found", "The blob was not found or is not accessible.", null);
        }

        ValidateDownloadContent(content, download);
        JmapEndpointResponse response = new JmapEndpointResponse();
        response.StatusCode = 200;
        response.ContentType = content.MediaType;
        response.BodyStream = content.Content;
        response.ContentLength = content.Size;
        response.HasContentLength = content.HasSize;
        response.Headers.Add("Cache-Control", "private, immutable, max-age=" + content.CacheLifetimeSeconds);
        response.Headers.Add("Content-Disposition", ContentDisposition(content.FileName));
        return response;
    }

    internal JmapEndpointResponse? ValidateCommonRequest(JmapEndpointRequest request, string? accountId)
    {
        if (request.Context == null || request.Context.UserId == null || request.Context.UserId.Length == 0 ||
            !Values.IsValidUnicode(request.Context.UserId))
        {
            return Problem(401, "Authentication Required", "An authenticated user is required.", null);
        }

        if (!Values.IsId(accountId))
        {
            return Problem(400, "Invalid Account", "A valid accountId route value is required.", null);
        }

        return null;
    }

    internal JmapEndpointResponse? ValidateAccount(
        string accountId,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        IJmapSessionService? sessions = Services.SessionService;
        if (sessions == null)
        {
            return Problem(500, "Session Service Unavailable", "The Session backend is not configured.", null);
        }

        JmapSession session = sessions.GetServerSessionAsync(context, cancellationToken).GetAwaiter().GetResult();
        if (session == null || session.Accounts == null || !session.Accounts.ContainsKey(accountId))
        {
            return Problem(404, "Account Not Found", "The account is not accessible.", null);
        }

        return null;
    }

    internal void RefreshAndEnforceQuota(
        IBlobService blobs,
        BlobUploadResponse stored,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        string accountId = stored.AccountId == null ? "" : stored.AccountId;
        string blobId = stored.BlobId == null ? "" : stored.BlobId;
        DateTimeOffset now = CurrentTime();
        DateTimeOffset retainUntil = now.AddSeconds(Options.UnreferencedBlobMinimumLifetimeSeconds);
        blobs.RefreshUnreferencedBlobAsync(
            accountId,
            blobId,
            retainUntil,
            context,
            cancellationToken).GetAwaiter().GetResult();
        List<JmapUnreferencedBlob> records = blobs.ListUnreferencedBlobsAsync(
            accountId,
            context,
            cancellationToken).GetAwaiter().GetResult();
        if (records == null)
        {
            throw ProtocolFailure("The blob backend did not return unreferenced quota records.", "unreferencedBlobs");
        }

        records.Sort((first, second) => first.UploadedAtUtc.CompareTo(second.UploadedAtUtc));
        long total = ValidateQuotaRecords(records, accountId);
        int index = 0;
        while (total > Options.UnreferencedBlobQuotaBytes && index < records.Count)
        {
            JmapUnreferencedBlob record = records[index];
            if (record.BlobId != blobId)
            {
                string evictedId = record.BlobId == null ? "" : record.BlobId;
                blobs.EvictUnreferencedBlobAsync(
                    accountId,
                    evictedId,
                    context,
                    cancellationToken).GetAwaiter().GetResult();
                total = total - record.Size;
            }

            index = index + 1;
        }

        if (total > Options.UnreferencedBlobQuotaBytes)
        {
            throw ProtocolFailure("The configured unreferenced blob quota cannot retain a valid upload.", "unreferencedBlobQuotaBytes");
        }
    }

    internal long ValidateQuotaRecords(List<JmapUnreferencedBlob> records, string accountId)
    {
        HashSet<string> identifiers = new HashSet<string>(StringComparer.Ordinal);
        long total = 0L;
        foreach (JmapUnreferencedBlob record in records)
        {
            if (record == null || record.AccountId != accountId || !Values.IsId(record.BlobId) ||
                !Values.IsUnsignedInt(record.Size) || record.BlobId == null || !identifiers.Add(record.BlobId))
            {
                throw ProtocolFailure("The blob backend returned invalid unreferenced quota metadata.", "unreferencedBlobs");
            }

            if (total > JmapProtocolLimits.MaximumUnsignedInt - record.Size)
            {
                throw ProtocolFailure("The blob backend returned an invalid unreferenced quota total.", "unreferencedBlobs");
            }

            total = total + record.Size;
        }

        return total;
    }

    internal void ValidateStoredUpload(BlobUploadResponse stored, BlobUploadRequest request)
    {
        if (stored == null || stored.AccountId != request.AccountId || !Values.IsId(stored.BlobId) ||
            stored.Type != request.MediaType || !Values.IsUnsignedInt(stored.Size) ||
            (request.HasContentLength && stored.Size != request.ContentLength))
        {
            throw ProtocolFailure("The blob backend returned invalid upload metadata.", "upload");
        }
    }

    internal void ValidateDownloadContent(JmapDownloadContent content, BlobDownloadRequest request)
    {
        if (content.Content == null || !content.Content.CanRead || content.MediaType != request.Type ||
            content.FileName != request.Name || !IsSafeMediaType(content.MediaType) ||
            !IsSafeFileName(content.FileName) || content.CacheLifetimeSeconds < 0 ||
            (content.HasSize && !Values.IsUnsignedInt(content.Size)))
        {
            if (content.Content != null)
            {
                content.Content.Dispose();
            }

            throw ProtocolFailure("The blob backend returned invalid download content.", "download");
        }
    }

    internal JmapEndpointResponse UploadResponse(BlobUploadResponse upload)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeBlobUploadResponse(upload, writer);
        writer.DisposeWriter();
        byte[] body = stream.ToArray();
        JmapEndpointResponse response = new JmapEndpointResponse();
        response.StatusCode = 201;
        response.ContentType = JmapMediaTypes.Json;
        response.BodyBytes = body;
        response.ContentLength = body.Length;
        response.HasContentLength = true;
        return response;
    }

    internal JmapEndpointResponse LimitProblem(int status, string title, string detail, string limit)
    {
        return Problems.CreateHttpProblemResponse(status, title, detail, JmapErrorCodes.LimitProblemType, limit);
    }

    internal JmapEndpointResponse Problem(int status, string title, string detail, string? type)
    {
        return Problems.CreateHttpProblemResponse(status, title, detail, type, null);
    }

    internal string? EndpointValue(JmapEndpointRequest request, string name)
    {
        if (request.RouteValues.ContainsKey(name))
        {
            return request.RouteValues[name];
        }

        if (request.QueryParameters.ContainsKey(name))
        {
            return request.QueryParameters[name];
        }

        return null;
    }

    internal bool IsSafeMediaType(string? mediaType)
    {
        if (mediaType == null || mediaType.Length == 0 || mediaType.IndexOf('\r') >= 0 || mediaType.IndexOf('\n') >= 0)
        {
            return false;
        }

        int separator = mediaType.IndexOf(';');
        string baseType = separator < 0 ? mediaType.Trim() : mediaType.Substring(0, separator).Trim();
        return Values.IsMediaType(baseType) && Values.IsValidUnicode(mediaType);
    }

    internal bool IsSafeFileName(string? fileName)
    {
        if (fileName == null || fileName.Length == 0 || fileName == "." || fileName == ".." ||
            !Values.IsUtf8WithinSize(fileName, SecurityLimits.MaximumFileNameBytes))
        {
            return false;
        }

        int index = 0;
        while (index < fileName.Length)
        {
            char character = fileName[index];
            if (character < ' ' || character == '\u007f' || character == '/' || character == '\\' || character == '"')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string ContentDisposition(string? fileName)
    {
        ContentDispositionHeaderValue disposition = new ContentDispositionHeaderValue("attachment");
        disposition.FileName = fileName;
        disposition.FileNameStar = fileName;
        return disposition.ToString();
    }

    internal DateTimeOffset CurrentTime()
    {
        IJmapClock? clock = Services.Clock;
        return clock == null ? DateTimeOffset.UtcNow : clock.GetCurrentUtcDateTime();
    }

    internal void ValidateOptions(JmapServerOptions options)
    {
        if (!Values.IsUnsignedInt(options.MaxUploadBytes) || options.MaxUploadBytes == 0L ||
            options.MaxConcurrentUploads <= 0 ||
            !Values.IsUnsignedInt(options.UnreferencedBlobQuotaBytes) ||
            options.UnreferencedBlobQuotaBytes < options.MaxUploadBytes ||
            options.UnreferencedBlobMinimumLifetimeSeconds < JmapProtocolLimits.MinimumUnreferencedBlobLifetimeSeconds)
        {
            throw new ArgumentException("Blob endpoint options are invalid.", "options");
        }
    }

    internal JmapProtocolException ProtocolFailure(string message, string path)
    {
        return new JmapProtocolException(message, path, default(System.Text.Json.JsonElement));
    }
}
