namespace uspto;

internal class PatentsViewSearchAttempt
{
    internal BufferedHttpResponse? Response = null;
    internal global::System.Exception? Failure = null;
    internal bool IsSuccessful = false;
}

internal class PatentsViewBufferedResult
{
    internal BufferedHttpResponse Response = new BufferedHttpResponse();
    internal bool FromCache = false;
}

internal class PatentsViewPatentOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal RateLimiter RateLimiter;
    internal RetryEngine RetryEngine;
    internal MemoryResponseCache MemoryCache;
    internal FileResponseCache FileCache;

    internal PatentsViewPatentOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        Client = client;
        Options = options;
        RateLimiter = new RateLimiter(options.RateLimits);
        RetryEngine = global::uspto.RetryEngine.CreateDefault(options.Retry);
        MemoryCache = new MemoryResponseCache(options.Cache);
        FileCache = new FileResponseCache(options.Cache);
    }

    internal PatentsViewPatentResponse QueryPatentsGet(PatentsViewQueryRequest request)
    {
        ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = ExecuteQuery("patent", request, false);
        return PatentsViewPatentParser.ParsePatentResponse(buffered.Response.Text, request, buffered.Response.Metadata, GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewPatentResponse QueryPatentsPost(PatentsViewQueryRequest request)
    {
        ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = ExecuteQuery("patent", request, true);
        return PatentsViewPatentParser.ParsePatentResponse(buffered.Response.Text, request, buffered.Response.Metadata, GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewPublicationResponse QueryPublicationsGet(PatentsViewQueryRequest request)
    {
        ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = ExecuteQuery("publication", request, false);
        return PatentsViewPatentParser.ParsePublicationResponse(buffered.Response.Text, request, buffered.Response.Metadata, GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewPublicationResponse QueryPublicationsPost(PatentsViewQueryRequest request)
    {
        ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = ExecuteQuery("publication", request, true);
        return PatentsViewPatentParser.ParsePublicationResponse(buffered.Response.Text, request, buffered.Response.Metadata, GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewPatent GetPatent(string patentIdentifier, RequestContext context)
    {
        ValidateRetrievalRequest(patentIdentifier, context, "patentIdentifier");
        PatentsViewBufferedResult buffered = ExecuteRetrieval("patent", patentIdentifier, context);
        PatentsViewQueryRequest request = CreateRetrievalQuery(context);
        PatentsViewPatentResponse response = PatentsViewPatentParser.ParsePatentResponse(buffered.Response.Text, request, buffered.Response.Metadata, GetRawResponse(buffered.Response), buffered.FromCache);
        if (response.Patents.Length == 0)
        {
            throw new NotFoundException("The requested PatentsView patent was not found.", ApiService.PatentsView, patentIdentifier, buffered.Response.Metadata.StatusCode, true, buffered.Response.Metadata.RequestIdentifier, null);
        }

        if (response.Patents.Length != 1 || !global::System.String.Equals(response.Patents[0].PatentIdentifier, patentIdentifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView patent response does not match the requested identifier.", "$.patents");
        }

        return response.Patents[0];
    }

    internal PatentsViewPublication GetPublication(string documentNumber, RequestContext context)
    {
        ValidateRetrievalRequest(documentNumber, context, "publicationIdentifier");
        PatentsViewBufferedResult buffered = ExecuteRetrieval("publication", documentNumber, context);
        PatentsViewQueryRequest request = CreateRetrievalQuery(context);
        PatentsViewPublicationResponse response = PatentsViewPatentParser.ParsePublicationResponse(buffered.Response.Text, request, buffered.Response.Metadata, GetRawResponse(buffered.Response), buffered.FromCache);
        if (response.Publications.Length == 0)
        {
            throw new NotFoundException("The requested PatentsView publication was not found.", ApiService.PatentsView, documentNumber, buffered.Response.Metadata.StatusCode, true, buffered.Response.Metadata.RequestIdentifier, null);
        }

        if (response.Publications.Length != 1 || !global::System.String.Equals(response.Publications[0].DocumentNumber, documentNumber, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView publication response does not match the requested identifier.", "$.publications");
        }

        return response.Publications[0];
    }

    internal PatentsViewBufferedResult ExecuteQuery(string resource, PatentsViewQueryRequest request, bool usePost)
    {
        ValidateBaseUri();
        RequestContext context = CreateRequestContext(request.Search.Context);
        RetryExecutionState retryState = new RetryExecutionState();
        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            using (global::System.Net.Http.HttpRequestMessage message = CreateQueryMessage(resource, request, usePost))
            {
                RequestHeaderPolicy.Apply(message, ApiService.PatentsView, Options, false, DocumentFormat.Json);
                string[] secrets = CredentialSecrets(message);
                string cacheKey = CacheKeyBuilder.Build(ApiService.PatentsView, message, secrets, context.CancellationToken);
                BufferedHttpResponse? cached = GetCached(cacheKey);
                if (cached != null)
                {
                    PatentsViewBufferedResult cachedResult = new PatentsViewBufferedResult();
                    cachedResult.Response = cached;
                    cachedResult.FromCache = true;
                    return cachedResult;
                }

                RateLimiter.Acquire(ApiService.PatentsView, context.CancellationToken);
                PatentsViewSearchAttempt attempt = ExecuteAttempt(message, context, secrets, "PatentsView " + resource + " search");
                if (attempt.IsSuccessful && attempt.Response != null)
                {
                    StoreCached(cacheKey, attempt.Response, secrets);
                    PatentsViewBufferedResult result = new PatentsViewBufferedResult();
                    result.Response = attempt.Response;
                    return result;
                }

                global::System.Exception failure = attempt.Failure ?? CreateIncompleteFailure("search");
                context.CancellationToken.ThrowIfCancellationRequested();
                if (!RetryEngine.TryWaitForRetry(retryState, message, true, failure, context.CancellationToken))
                {
                    throw failure;
                }
            }
        }
    }

    internal PatentsViewBufferedResult ExecuteRetrieval(string resource, string identifier, RequestContext sourceContext)
    {
        ValidateBaseUri();
        RequestContext context = CreateRequestContext(sourceContext);
        RetryExecutionState retryState = new RetryExecutionState();
        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.PatentsViewBaseUri, CreateRetrievalSegments(resource, identifier), new global::System.Collections.Generic.Dictionary<string, string?>()))
            {
                PatentsViewQueryWriter.ValidateGetRequestUri(message.RequestUri, Options.Limits);
                RequestHeaderPolicy.Apply(message, ApiService.PatentsView, Options, false, DocumentFormat.Json);
                string[] secrets = CredentialSecrets(message);
                string cacheKey = CacheKeyBuilder.Build(ApiService.PatentsView, message, secrets, context.CancellationToken);
                BufferedHttpResponse? cached = GetCached(cacheKey);
                if (cached != null)
                {
                    PatentsViewBufferedResult cachedResult = new PatentsViewBufferedResult();
                    cachedResult.Response = cached;
                    cachedResult.FromCache = true;
                    return cachedResult;
                }

                RateLimiter.Acquire(ApiService.PatentsView, context.CancellationToken);
                PatentsViewSearchAttempt attempt = ExecuteAttempt(message, context, secrets, "PatentsView " + resource + " retrieval");
                if (attempt.IsSuccessful && attempt.Response != null)
                {
                    StoreCached(cacheKey, attempt.Response, secrets);
                    PatentsViewBufferedResult result = new PatentsViewBufferedResult();
                    result.Response = attempt.Response;
                    return result;
                }

                global::System.Exception failure = attempt.Failure ?? CreateIncompleteFailure("retrieval");
                context.CancellationToken.ThrowIfCancellationRequested();
                if (!RetryEngine.TryWaitForRetry(retryState, message, false, failure, context.CancellationToken))
                {
                    throw failure;
                }
            }
        }
    }

    internal global::System.Net.Http.HttpRequestMessage CreateQueryMessage(string resource, PatentsViewQueryRequest request, bool usePost)
    {
        if (usePost)
        {
            string body;
            if (global::System.String.Equals(resource, "patent", global::System.StringComparison.Ordinal))
            {
                body = PatentsViewQueryWriter.WritePatentPostBody(request, Options.Limits);
            }
            else if (UsesPatentIdentifierOptions(resource))
            {
                body = PatentsViewQueryWriter.WritePatentLinkedPostBody(request, Options.Limits);
            }
            else
            {
                body = PatentsViewQueryWriter.WritePostBody(request.Search, Options.Limits);
            }

            return RequestBuilder.CreateJsonPostRequest(Options.PatentsViewBaseUri, CreateResourceSegments(resource), new global::System.Collections.Generic.Dictionary<string, string?>(), body);
        }

        global::System.Collections.Generic.Dictionary<string, string?> query;
        if (global::System.String.Equals(resource, "patent", global::System.StringComparison.Ordinal))
        {
            query = PatentsViewQueryWriter.WritePatentGetQuery(request, Options.Limits);
        }
        else if (UsesPatentIdentifierOptions(resource))
        {
            query = PatentsViewQueryWriter.WritePatentLinkedGetQuery(request, Options.Limits);
        }
        else
        {
            query = PatentsViewQueryWriter.WriteGetQuery(request.Search, Options.Limits);
        }

        global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.PatentsViewBaseUri, CreateResourceSegments(resource), query);
        PatentsViewQueryWriter.ValidateGetRequestUri(message.RequestUri, Options.Limits);
        return message;
    }

    internal static bool UsesPatentIdentifierOptions(string resource)
    {
        return resource.StartsWith("patent/", global::System.StringComparison.Ordinal) || resource.StartsWith("g_", global::System.StringComparison.Ordinal);
    }

    internal static string[] CreateResourceSegments(string resource)
    {
        string[] resourceSegments = resource.Split('/');
        string[] result = new string[resourceSegments.Length + 2];
        result[0] = "api";
        result[1] = "v1";
        global::System.Array.Copy(resourceSegments, 0, result, 2, resourceSegments.Length);
        return result;
    }

    internal static string[] CreateRetrievalSegments(string resource, string identifier)
    {
        string[] resourceSegments = CreateResourceSegments(resource);
        string[] result = new string[resourceSegments.Length + 1];
        global::System.Array.Copy(resourceSegments, result, resourceSegments.Length);
        result[result.Length - 1] = identifier;
        return result;
    }

    internal PatentsViewSearchAttempt ExecuteAttempt(global::System.Net.Http.HttpRequestMessage request, RequestContext context, string[] secrets, string operationName)
    {
        global::System.Threading.Tasks.Task<BufferedHttpResponse> task = global::System.Threading.Tasks.Task.Run(() => SendRequest(request, context, secrets, operationName));
        while (!task.IsCompleted)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            global::System.Threading.Thread.Yield();
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentsViewSearchAttempt result = new PatentsViewSearchAttempt();
        if (task.IsCompletedSuccessfully)
        {
            result.Response = task.Result;
            result.IsSuccessful = true;
        }
        else if (task.Exception != null)
        {
            result.Failure = task.Exception.GetBaseException();
        }
        else
        {
            result.Failure = CreateIncompleteFailure("request");
        }

        return result;
    }

    internal BufferedHttpResponse SendRequest(global::System.Net.Http.HttpRequestMessage request, RequestContext context, string[] secrets, string operationName)
    {
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
        using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
        {
            RateLimiter.ObserveResponse(ApiService.PatentsView, response);
            int statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw ErrorMapper.MapResponse(response, ApiService.PatentsView, operationName, Options.Limits, secrets, context.CancellationToken);
            }

            return TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
        }
    }

    internal BufferedHttpResponse? GetCached(string cacheKey)
    {
        global::System.DateTimeOffset now = global::System.DateTimeOffset.UtcNow;
        BufferedHttpResponse? cached = MemoryCache.Get(cacheKey, now);
        if (cached != null)
        {
            return cached;
        }

        cached = FileCache.Get(cacheKey, now);
        if (cached != null)
        {
            MemoryCache.Store(cacheKey, cached, now, new string[0]);
        }

        return cached;
    }

    internal void StoreCached(string cacheKey, BufferedHttpResponse response, string[] secrets)
    {
        global::System.DateTimeOffset now = global::System.DateTimeOffset.UtcNow;
        MemoryCache.Store(cacheKey, response, now, secrets);
        FileCache.Store(cacheKey, response, now, secrets);
    }

    internal static string? GetRawResponse(BufferedHttpResponse response)
    {
        return response.HasRawText ? response.RawText : null;
    }

    internal static PatentsViewQueryRequest CreateRetrievalQuery(RequestContext context)
    {
        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.Search.Context = context;
        request.Search.CursorPage.Size = 1;
        return request;
    }

    internal static RequestContext CreateRequestContext(RequestContext source)
    {
        RequestContext context = new RequestContext();
        context.Service = ApiService.PatentsView;
        context.CancellationToken = source.CancellationToken;
        context.RetainRawResponse = source.RetainRawResponse;
        context.MaximumRawResponseBytes = source.MaximumRawResponseBytes;
        context.Deadline = source.Deadline;
        context.HasDeadline = source.HasDeadline;
        context.CorrelationIdentifier = source.CorrelationIdentifier;
        context.Fallback = source.Fallback;
        return context;
    }

    internal static string[] CredentialSecrets(global::System.Net.Http.HttpRequestMessage request)
    {
        if (!request.Headers.Contains(UsptoHeaders.PatentsViewApiKey))
        {
            return new string[0];
        }

        global::System.Collections.Generic.List<string> values = new global::System.Collections.Generic.List<string>();
        foreach (string value in request.Headers.GetValues(UsptoHeaders.PatentsViewApiKey))
        {
            values.Add(value);
        }

        return values.ToArray();
    }

    internal static global::System.Exception CreateIncompleteFailure(string operation)
    {
        return new ApiException("The PatentsView " + operation + " did not complete.", ApiService.PatentsView, 0, false, "PATENTSVIEW_REQUEST_INCOMPLETE", null, null, false, null);
    }

    internal static void ValidateQueryRequest(PatentsViewQueryRequest request)
    {
        PatentsViewQueryWriter.ValidateTypedRequest(request);
        if (request.Search.Context == null)
        {
            throw new ValidationException("A PatentsView request context is required.", "Search.Context", "REQUIRED", null);
        }

        request.Search.Context.CancellationToken.ThrowIfCancellationRequested();
    }

    internal static void ValidateRetrievalRequest(string identifier, RequestContext context, string fieldName)
    {
        if (global::System.String.IsNullOrWhiteSpace(identifier))
        {
            throw new ValidationException("A PatentsView resource identifier is required.", fieldName, "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("A PatentsView request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
    }

    internal void ValidateBaseUri()
    {
        global::System.Uri baseUri = Options.PatentsViewBaseUri;
        if (baseUri == null)
        {
            throw new ValidationException("A PatentsView base URI is required.", "PatentsViewBaseUri", "REQUIRED", null);
        }

        if (!baseUri.IsAbsoluteUri || global::System.String.IsNullOrWhiteSpace(baseUri.Host))
        {
            throw new ValidationException("The PatentsView base URI must be absolute and include a host.", "PatentsViewBaseUri", "ABSOLUTE_URI_REQUIRED", null);
        }

        if (baseUri.UserInfo.Length != 0 || baseUri.Query.Length != 0 || baseUri.Fragment.Length != 0)
        {
            throw new ValidationException("The PatentsView base URI cannot contain credentials, a query, or a fragment.", "PatentsViewBaseUri", "BASE_URI_COMPONENT", null);
        }

        bool isHttps = global::System.String.Equals(baseUri.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase);
        bool isPermittedLoopback = Options.PermitInsecureTestEndpoints && baseUri.IsLoopback && global::System.String.Equals(baseUri.Scheme, global::System.Uri.UriSchemeHttp, global::System.StringComparison.OrdinalIgnoreCase);
        if (!isHttps && !isPermittedLoopback)
        {
            throw new ValidationException("The PatentsView base URI must use HTTPS unless loopback test endpoints are explicitly permitted.", "PatentsViewBaseUri", "HTTPS_REQUIRED", null);
        }
    }
}
