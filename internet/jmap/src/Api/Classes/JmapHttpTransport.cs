using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Sends JMAP requests with the built-in HTTP stack and caller-supplied authorization.
/// </summary>
public class JmapHttpTransport : IJmapTransport
{
    internal static readonly HttpClient SharedClient = new HttpClient(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
    });

    internal HttpClient Client;
    internal JmapDiscoveryOptions DiscoveryOptions;
    internal IJmapAuthorizationProvider? AuthorizationProvider;
    internal JmapJsonCodec Codec = new JmapJsonCodec();
    internal HttpContentReader ContentReader = new HttpContentReader();
    internal HttpResponseSender Sender = new HttpResponseSender();
    internal RequestProblemParser Problems = new RequestProblemParser();
    internal UriTemplateExpander Templates = new UriTemplateExpander();
    internal ValueValidator Values = new ValueValidator();
    internal int MaximumResponseBytes = JmapProtocolLimits.SuggestedMinimumMaxSizeRequest;
    internal long MaximumUploadBytes = JmapProtocolLimits.SuggestedMinimumMaxSizeUpload;

    /// <summary>Creates an HTTP transport with Session redirect policy and optional authorization.</summary>
    /// <param name="discoveryOptions">The TLS and Session redirect policy.</param>
    /// <param name="authorizationProvider">The authorization provider, or null for unauthenticated requests.</param>
    public JmapHttpTransport(
        JmapDiscoveryOptions discoveryOptions,
        IJmapAuthorizationProvider? authorizationProvider)
    {
        if (discoveryOptions == null)
        {
            throw new ArgumentNullException("discoveryOptions");
        }

        Client = SharedClient;
        DiscoveryOptions = discoveryOptions;
        AuthorizationProvider = authorizationProvider;
    }

    /// <summary>Retrieves and decodes a Session resource using bounded, credential-safe redirects.</summary>
    /// <param name="sessionUrl">The absolute Session resource URL.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Session resource.</returns>
    public Task<JmapSession> GetTransportSessionAsync(string sessionUrl, CancellationToken cancellationToken)
    {
        return Task.Run(() => GetSession(sessionUrl, cancellationToken), cancellationToken);
    }

    /// <summary>Sends a JSON JMAP API request and decodes its response.</summary>
    /// <param name="apiUrl">The absolute API endpoint URL.</param>
    /// <param name="request">The ordered JMAP request.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the complete JMAP response.</returns>
    public Task<JmapResponse> SendTransportApiRequestAsync(
        string apiUrl,
        JmapRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SendApiRequest(apiUrl, request, cancellationToken), cancellationToken);
    }

    /// <summary>Uploads a raw binary stream.</summary>
    /// <param name="uploadUrl">The expanded absolute upload URL.</param>
    /// <param name="request">The upload stream and metadata.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the uploaded blob metadata.</returns>
    public Task<BlobUploadResponse> SendTransportUploadAsync(
        string uploadUrl,
        BlobUploadRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SendUpload(uploadUrl, request, cancellationToken), cancellationToken);
    }

    /// <summary>Downloads a raw binary stream.</summary>
    /// <param name="downloadUrl">The expanded absolute download URL.</param>
    /// <param name="request">The requested blob and response metadata.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the streaming download response.</returns>
    public Task<BlobDownloadResponse> SendTransportDownloadAsync(
        string downloadUrl,
        BlobDownloadRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SendDownload(downloadUrl, request, cancellationToken), cancellationToken);
    }

    /// <summary>Opens an authenticated EventSource response stream.</summary>
    /// <param name="eventSourceUrl">The expanded absolute EventSource URL.</param>
    /// <param name="lastEventId">The last received event id, or null for a fresh stream.</param>
    /// <param name="cancellationToken">A token that can cancel connection establishment.</param>
    /// <returns>A task containing the readable event stream.</returns>
    public Task<Stream> OpenTransportEventSourceAsync(
        string eventSourceUrl,
        string? lastEventId,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => OpenEventSource(eventSourceUrl, lastEventId, cancellationToken), cancellationToken);
    }

    internal Stream OpenEventSource(
        string eventSourceUrl,
        string? lastEventId,
        CancellationToken cancellationToken)
    {
        SessionRedirectHandler uriPolicy = new SessionRedirectHandler(Client, null);
        Uri endpoint = uriPolicy.RequireInitialUri(eventSourceUrl, DiscoveryOptions.AllowInsecureHttp);
        if (!IsSafeEventId(lastEventId))
        {
            throw Validation("The EventSource last event identifier is invalid.", "lastEventId");
        }

        string? authorization = null;
        if (AuthorizationProvider != null)
        {
            authorization = AuthorizationProvider.GetAuthorizationValueAsync(
                endpoint.AbsoluteUri,
                cancellationToken).GetAwaiter().GetResult();
            if (!IsSafeAuthorizationValue(authorization))
            {
                throw Validation("The authorization provider returned an invalid header value.", "authorization");
            }
        }

        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Accept.ParseAdd(JmapMediaTypes.EventStream);
        request.Headers.CacheControl = new CacheControlHeaderValue();
        request.Headers.CacheControl.NoCache = true;
        if (authorization != null && authorization.Length != 0)
        {
            request.Headers.Add("Authorization", authorization);
        }

        if (lastEventId != null && lastEventId.Length != 0)
        {
            request.Headers.TryAddWithoutValidation("Last-Event-ID", lastEventId);
        }

        HttpResponseMessage response = Sender.SendResponseAsync(
            Client,
            request,
            endpoint.AbsoluteUri,
            cancellationToken).GetAwaiter().GetResult();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            using HttpResponseMessage ownedResponse = response;
            byte[] body = ContentReader.ReadBoundedContent(
                ownedResponse.Content,
                MaximumResponseBytes,
                endpoint.AbsoluteUri,
                (int)ownedResponse.StatusCode,
                cancellationToken);
            throw TransportFailure(
                "The EventSource endpoint returned HTTP status " + (int)ownedResponse.StatusCode + ".",
                endpoint.AbsoluteUri,
                ownedResponse,
                body,
                Problems.IsRetryableStatus((int)ownedResponse.StatusCode));
        }

        string? contentType = GetContentType(response);
        if (!string.Equals(contentType, JmapMediaTypes.EventStream, StringComparison.OrdinalIgnoreCase))
        {
            response.Dispose();
            throw new JmapProtocolException(
                "The EventSource endpoint did not return text/event-stream.",
                "contentType",
                default(System.Text.Json.JsonElement));
        }

        return ContentReader.GetStreamingContentAsync(
            response,
            endpoint.AbsoluteUri,
            (int)response.StatusCode,
            cancellationToken).GetAwaiter().GetResult();
    }

    internal JmapSession GetSession(string sessionUrl, CancellationToken cancellationToken)
    {
        SessionRedirectHandler redirects = new SessionRedirectHandler(Client, AuthorizationProvider);
        using HttpResponseMessage response = redirects.GetSessionResponseAsync(
            sessionUrl,
            DiscoveryOptions,
            cancellationToken).GetAwaiter().GetResult();
        byte[] body = ContentReader.ReadBoundedContent(
            response.Content,
            MaximumResponseBytes,
            sessionUrl,
            (int)response.StatusCode,
            cancellationToken);
        string? contentType = GetContentType(response);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw TransportFailure(
                "The Session endpoint returned HTTP status " + (int)response.StatusCode + ".",
                sessionUrl,
                response,
                body,
                Problems.IsRetryableStatus((int)response.StatusCode));
        }

        if (!IsJsonMediaType(contentType))
        {
            throw TransportFailure(
                "The Session endpoint did not return application/json.",
                sessionUrl,
                response,
                body,
                false);
        }

        return Codec.DecodeSessionMemory(body);
    }

    internal JmapResponse SendApiRequest(
        string apiUrl,
        JmapRequest request,
        CancellationToken cancellationToken)
    {
        SessionRedirectHandler uriPolicy = new SessionRedirectHandler(Client, null);
        Uri endpoint = uriPolicy.RequireInitialUri(apiUrl, DiscoveryOptions.AllowInsecureHttp);
        byte[] requestBody = Codec.EncodeRequestBytes(request);
        string? authorization = null;
        if (AuthorizationProvider != null)
        {
            authorization = AuthorizationProvider.GetAuthorizationValueAsync(
                endpoint.AbsoluteUri,
                cancellationToken).GetAwaiter().GetResult();
            if (!IsSafeAuthorizationValue(authorization))
            {
                throw Validation("The authorization provider returned an invalid header value.", "authorization");
            }
        }

        using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Accept.ParseAdd(JmapMediaTypes.Json);
        httpRequest.Headers.Accept.ParseAdd(JmapMediaTypes.ProblemJson);
        if (authorization != null && authorization.Length != 0)
        {
            httpRequest.Headers.Add("Authorization", authorization);
        }

        ByteArrayContent content = new ByteArrayContent(requestBody);
        MediaTypeHeaderValue contentType = new MediaTypeHeaderValue(JmapMediaTypes.Json);
        contentType.CharSet = "utf-8";
        content.Headers.ContentType = contentType;
        httpRequest.Content = content;
        using HttpResponseMessage response = Sender.SendResponseAsync(
            Client,
            httpRequest,
            endpoint.AbsoluteUri,
            cancellationToken).GetAwaiter().GetResult();
        byte[] responseBody = ContentReader.ReadBoundedContent(
            response.Content,
            MaximumResponseBytes,
            endpoint.AbsoluteUri,
            (int)response.StatusCode,
            cancellationToken);
        string? responseType = GetContentType(response);
        if (!response.IsSuccessStatusCode)
        {
            throw Problems.ParseProblemResponse((int)response.StatusCode, responseType, responseBody);
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw TransportFailure(
                "The JMAP API endpoint returned an unexpected success status.",
                endpoint.AbsoluteUri,
                response,
                responseBody,
                false);
        }

        if (!IsJsonMediaType(responseType))
        {
            throw TransportFailure(
                "The JMAP API endpoint did not return application/json.",
                endpoint.AbsoluteUri,
                response,
                responseBody,
                false);
        }

        return Codec.DecodeResponseMemory(responseBody);
    }

    internal BlobUploadResponse SendUpload(
        string uploadUrl,
        BlobUploadRequest request,
        CancellationToken cancellationToken)
    {
        ValidateUploadRequest(request);
        string expandedUrl = uploadUrl.Contains('{') || uploadUrl.Contains('}') ?
            Templates.ExpandUploadUrl(uploadUrl, request.AccountId) : uploadUrl;
        SessionRedirectHandler uriPolicy = new SessionRedirectHandler(Client, null);
        Uri endpoint = uriPolicy.RequireInitialUri(expandedUrl, DiscoveryOptions.AllowInsecureHttp);
        string? authorization = null;
        if (AuthorizationProvider != null)
        {
            authorization = AuthorizationProvider.GetAuthorizationValueAsync(
                endpoint.AbsoluteUri,
                cancellationToken).GetAwaiter().GetResult();
            if (!IsSafeAuthorizationValue(authorization))
            {
                throw Validation("The authorization provider returned an invalid header value.", "authorization");
            }
        }

        HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Accept.ParseAdd(JmapMediaTypes.Json);
        httpRequest.Headers.Accept.ParseAdd(JmapMediaTypes.ProblemJson);
        if (authorization != null && authorization.Length != 0)
        {
            httpRequest.Headers.Add("Authorization", authorization);
        }

        if (!request.HasContentLength)
        {
            httpRequest.Headers.TransferEncodingChunked = true;
        }

        Stream? uploadContent = request.Content;
        if (uploadContent == null)
        {
            throw Validation("The blob upload content is required.", "content");
        }

        StreamPipeReaderOptions pipeOptions = new StreamPipeReaderOptions(null, 4096, 1024, true);
        PipeReader pipeReader = PipeReader.Create(uploadContent, pipeOptions);
        Stream protectedContent = pipeReader.AsStream();
        StreamContent content = new StreamContent(protectedContent);
        content.Headers.TryAddWithoutValidation("Content-Type", request.MediaType);
        if (request.HasContentLength)
        {
            content.Headers.ContentLength = request.ContentLength;
        }

        httpRequest.Content = content;
        using HttpResponseMessage response = Sender.SendStreamingResponseAsync(
            Client,
            httpRequest,
            endpoint.AbsoluteUri,
            cancellationToken).GetAwaiter().GetResult();
        byte[] responseBody = ContentReader.ReadBoundedContent(
            response.Content,
            MaximumResponseBytes,
            endpoint.AbsoluteUri,
            (int)response.StatusCode,
            cancellationToken);
        string? responseType = GetContentType(response);
        if (!response.IsSuccessStatusCode)
        {
            throw Problems.ParseProblemResponse((int)response.StatusCode, responseType, responseBody);
        }

        if (!IsJsonMediaType(responseType))
        {
            throw TransportFailure(
                "The upload endpoint did not return application/json.",
                endpoint.AbsoluteUri,
                response,
                responseBody,
                false);
        }

        BlobUploadResponse result = Codec.Push.DecodeBlobUploadResponse(Codec.Reader.ReadUtf8Memory(responseBody));
        ValidateUploadResponse(result, request);
        return result;
    }

    internal BlobDownloadResponse SendDownload(
        string downloadUrl,
        BlobDownloadRequest request,
        CancellationToken cancellationToken)
    {
        ValidateDownloadRequest(request);
        bool isTemplate = downloadUrl.Contains('{') || downloadUrl.Contains('}');
        string expandedUrl = isTemplate ?
            Templates.ExpandDownloadUrl(downloadUrl, request.AccountId, request.BlobId, request.Type, request.Name) : downloadUrl;
        SessionRedirectHandler uriPolicy = new SessionRedirectHandler(Client, null);
        Uri endpoint = isTemplate ? new Uri(expandedUrl, UriKind.RelativeOrAbsolute) :
            uriPolicy.RequireInitialUri(expandedUrl, DiscoveryOptions.AllowInsecureHttp);
        if (!endpoint.IsAbsoluteUri)
        {
            throw Validation("The expanded download URL is not absolute.", "downloadUrl");
        }

        uriPolicy.ValidateHttpUri(endpoint, DiscoveryOptions.AllowInsecureHttp, "The download URL is not permitted.");
        string? authorization = null;
        if (AuthorizationProvider != null)
        {
            authorization = AuthorizationProvider.GetAuthorizationValueAsync(
                endpoint.AbsoluteUri,
                cancellationToken).GetAwaiter().GetResult();
            if (!IsSafeAuthorizationValue(authorization))
            {
                throw Validation("The authorization provider returned an invalid header value.", "authorization");
            }
        }

        using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
        httpRequest.Headers.Accept.ParseAdd(request.Type);
        httpRequest.Headers.Accept.ParseAdd(JmapMediaTypes.ProblemJson);
        if (authorization != null && authorization.Length != 0)
        {
            httpRequest.Headers.Add("Authorization", authorization);
        }

        HttpResponseMessage response = Sender.SendResponseAsync(
            Client,
            httpRequest,
            endpoint.AbsoluteUri,
            cancellationToken).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            throw ReadDownloadFailure(response, endpoint.AbsoluteUri, cancellationToken);
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            response.Dispose();
            throw new JmapTransportException(
                "The download endpoint returned an unexpected success status.",
                endpoint.AbsoluteUri,
                (int)response.StatusCode,
                true,
                false,
                null,
                null);
        }

        string? mediaType = GetContentType(response);
        if (!IsSafeMediaType(mediaType) || !string.Equals(mediaType, request.Type, StringComparison.OrdinalIgnoreCase))
        {
            response.Dispose();
            throw new JmapProtocolException(
                "The download response contains an invalid or unexpected media type.",
                "type",
                default(System.Text.Json.JsonElement));
        }

        string? fileName = GetDownloadFileName(response);
        if (fileName != null && (!IsSafeFileName(fileName) || fileName != request.Name))
        {
            response.Dispose();
            throw new JmapProtocolException(
                "The download response contains an invalid or unexpected filename.",
                "name",
                default(System.Text.Json.JsonElement));
        }

        long contentLength = 0L;
        bool hasContentLength = response.Content.Headers.ContentLength.HasValue;
        if (hasContentLength)
        {
            contentLength = response.Content.Headers.ContentLength.GetValueOrDefault();
            if (!Values.IsUnsignedInt(contentLength))
            {
                response.Dispose();
                throw new JmapProtocolException(
                    "The download response contains an invalid content length.",
                    "contentLength",
                    default(System.Text.Json.JsonElement));
            }
        }

        Stream content = ContentReader.GetStreamingContentAsync(
            response,
            endpoint.AbsoluteUri,
            (int)response.StatusCode,
            cancellationToken).GetAwaiter().GetResult();
        BlobDownloadResponse result = new BlobDownloadResponse();
        result.Content = content;
        result.MediaType = mediaType;
        result.FileName = fileName;
        result.ContentLength = contentLength;
        result.HasContentLength = hasContentLength;
        result.CacheControl = response.Headers.CacheControl == null ? null : response.Headers.CacheControl.ToString();
        return result;
    }

    internal JmapRequestException ReadDownloadFailure(
        HttpResponseMessage response,
        string endpoint,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage ownedResponse = response;
        byte[] responseBody = ContentReader.ReadBoundedContent(
            ownedResponse.Content,
            MaximumResponseBytes,
            endpoint,
            (int)ownedResponse.StatusCode,
            cancellationToken);
        string? responseType = GetContentType(ownedResponse);
        return Problems.ParseProblemResponse((int)ownedResponse.StatusCode, responseType, responseBody);
    }

    internal void ValidateDownloadRequest(BlobDownloadRequest request)
    {
        System.Collections.Generic.List<string> invalid = new System.Collections.Generic.List<string>();
        if (request == null)
        {
            invalid.Add("");
        }
        else
        {
            if (!Values.IsId(request.AccountId))
            {
                invalid.Add("accountId");
            }

            if (!Values.IsId(request.BlobId))
            {
                invalid.Add("blobId");
            }

            if (!Values.IsMediaType(request.Type))
            {
                invalid.Add("type");
            }

            if (!IsSafeFileName(request.Name))
            {
                invalid.Add("name");
            }
        }

        if (invalid.Count != 0)
        {
            throw new JmapValidationException(
                "The blob download request is invalid.",
                invalid,
                default(System.Text.Json.JsonElement));
        }
    }

    internal string? GetDownloadFileName(HttpResponseMessage response)
    {
        ContentDispositionHeaderValue? disposition = response.Content.Headers.ContentDisposition;
        if (disposition == null)
        {
            return null;
        }

        string? fileName = disposition.FileNameStar;
        if (fileName == null || fileName.Length == 0)
        {
            fileName = disposition.FileName;
        }

        if (fileName != null && fileName.Length >= 2 && fileName[0] == '"' && fileName[fileName.Length - 1] == '"')
        {
            fileName = fileName.Substring(1, fileName.Length - 2);
        }

        return fileName;
    }

    internal bool IsSafeFileName(string? fileName)
    {
        if (fileName == null || fileName.Length == 0 || fileName == "." || fileName == ".." ||
            !Values.IsUtf8WithinSize(fileName, 255L))
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

    internal void ValidateUploadRequest(BlobUploadRequest request)
    {
        System.Collections.Generic.List<string> invalid = new System.Collections.Generic.List<string>();
        if (request == null)
        {
            invalid.Add("");
        }
        else
        {
            if (!Values.IsId(request.AccountId))
            {
                invalid.Add("accountId");
            }

            if (!IsSafeMediaType(request.MediaType))
            {
                invalid.Add("mediaType");
            }

            if (request.Content == null || !request.Content.CanRead)
            {
                invalid.Add("content");
            }

            if (request.HasContentLength &&
                (!Values.IsUnsignedInt(request.ContentLength) || request.ContentLength > MaximumUploadBytes))
            {
                invalid.Add("contentLength");
            }

            if (request.Content != null && request.Content.CanSeek)
            {
                long remaining = request.Content.Length - request.Content.Position;
                if (remaining < 0L || remaining > MaximumUploadBytes ||
                    (request.HasContentLength && remaining != request.ContentLength))
                {
                    if (!invalid.Contains("contentLength"))
                    {
                        invalid.Add("contentLength");
                    }
                }
            }
        }

        if (invalid.Count != 0)
        {
            throw new JmapValidationException(
                "The blob upload request is invalid.",
                invalid,
                default(System.Text.Json.JsonElement));
        }
    }

    internal void ValidateUploadResponse(BlobUploadResponse response, BlobUploadRequest request)
    {
        if (response.AccountId != request.AccountId)
        {
            throw new JmapProtocolException(
                "The upload response accountId does not match the requested account.",
                "accountId",
                default(System.Text.Json.JsonElement));
        }

        if (!Values.IsId(response.BlobId))
        {
            throw new JmapProtocolException(
                "The upload response contains an invalid blobId.",
                "blobId",
                default(System.Text.Json.JsonElement));
        }

        if (!IsSafeMediaType(response.Type))
        {
            throw new JmapProtocolException(
                "The upload response contains an invalid media type.",
                "type",
                default(System.Text.Json.JsonElement));
        }

        if (request.HasContentLength && response.Size != request.ContentLength)
        {
            throw new JmapProtocolException(
                "The upload response size does not match the uploaded content length.",
                "size",
                default(System.Text.Json.JsonElement));
        }
    }

    internal bool IsSafeMediaType(string? mediaType)
    {
        if (mediaType == null || mediaType.Length == 0 ||
            mediaType.IndexOf('\r') >= 0 || mediaType.IndexOf('\n') >= 0 ||
            !Values.IsValidUnicode(mediaType))
        {
            return false;
        }

        int separator = mediaType.IndexOf(';');
        string baseType = separator < 0 ? mediaType.Trim() : mediaType.Substring(0, separator).Trim();
        return Values.IsMediaType(baseType);
    }

    internal string? GetContentType(HttpResponseMessage response)
    {
        MediaTypeHeaderValue? contentType = response.Content.Headers.ContentType;
        return contentType == null ? null : contentType.MediaType;
    }

    internal bool IsJsonMediaType(string? contentType)
    {
        return string.Equals(contentType, JmapMediaTypes.Json, StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsSafeAuthorizationValue(string? authorization)
    {
        return authorization == null ||
            (authorization.IndexOf('\r') < 0 && authorization.IndexOf('\n') < 0);
    }

    internal bool IsSafeEventId(string? eventId)
    {
        if (eventId == null)
        {
            return true;
        }

        if (eventId.Length > EventSourceParser.MaximumLineCharacters || !Values.IsValidUnicode(eventId))
        {
            return false;
        }

        int index = 0;
        while (index < eventId.Length)
        {
            char character = eventId[index];
            if (character == '\r' || character == '\n' || character == '\0')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal JmapValidationException Validation(string message, string path)
    {
        System.Collections.Generic.List<string> invalid = new System.Collections.Generic.List<string>();
        invalid.Add(path);
        return new JmapValidationException(message, invalid, default(System.Text.Json.JsonElement));
    }

    internal JmapTransportException TransportFailure(
        string message,
        string endpoint,
        HttpResponseMessage response,
        byte[] responseBody,
        bool retryable)
    {
        return new JmapTransportException(
            message,
            endpoint,
            (int)response.StatusCode,
            true,
            retryable,
            responseBody,
            null);
    }
}
