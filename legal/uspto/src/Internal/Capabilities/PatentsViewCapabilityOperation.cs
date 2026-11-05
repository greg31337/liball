namespace uspto;

internal class PatentsViewCapabilityAttempt
{
    internal BufferedHttpResponse? Response = null;
    internal global::System.Exception? Failure = null;
    internal bool IsSuccessful = false;
}

internal class PatentsViewCapabilityOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal RateLimiter RateLimiter;
    internal RetryEngine RetryEngine;
    internal MemoryResponseCache MemoryCache;
    internal FileResponseCache FileCache;

    internal PatentsViewCapabilityOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal PatentsViewCapabilityResult Probe(RequestContext sourceContext)
    {
        ValidateContext(sourceContext);
        ValidateBaseUri();
        RequestContext context = CreateRequestContext(sourceContext);
        RetryExecutionState retryState = new RetryExecutionState();
        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            using (global::System.Net.Http.HttpRequestMessage request = CreateProbeRequest())
            {
                RequestHeaderPolicy.Apply(request, ApiService.PatentsView, Options, false, DocumentFormat.Json);
                string[] secrets = CredentialSecrets(request);
                string cacheKey = CacheKeyBuilder.Build(ApiService.PatentsView, request, secrets, context.CancellationToken);
                BufferedHttpResponse? cached = GetCached(cacheKey);
                if (cached != null)
                {
                    return CreateAvailableResult(cached, sourceContext, 0, true);
                }

                RateLimiter.Acquire(ApiService.PatentsView, context.CancellationToken);
                PatentsViewCapabilityAttempt attempt = ExecuteAttempt(request, context, secrets);
                if (attempt.IsSuccessful && attempt.Response != null)
                {
                    global::System.DateTimeOffset now = global::System.DateTimeOffset.UtcNow;
                    MemoryCache.Store(cacheKey, attempt.Response, now, secrets);
                    FileCache.Store(cacheKey, attempt.Response, now, secrets);
                    return CreateAvailableResult(attempt.Response, sourceContext, retryState.AttemptCount, false);
                }

                global::System.Exception failure = attempt.Failure ?? new ApiException("The PatentsView capability probe did not complete.", ApiService.PatentsView, 0, false, "CAPABILITY_PROBE_INCOMPLETE", null, null, false, null);
                context.CancellationToken.ThrowIfCancellationRequested();
                if (!RetryEngine.TryWaitForRetry(retryState, request, true, failure, context.CancellationToken))
                {
                    return CreateUnavailableResult(failure, sourceContext, retryState.AttemptCount);
                }
            }
        }
    }

    internal global::System.Net.Http.HttpRequestMessage CreateProbeRequest()
    {
        SearchRequest search = new SearchRequest();
        search.Filters = new SearchFilter[]
        {
            new SearchFilter
            {
                Field = "patent_id",
                Operator = SearchOperator.Equal,
                Value = "0"
            }
        };
        search.FieldSelection.Fields = new string[] { "patent_id" };
        search.Sorts = new SearchSort[]
        {
            new SearchSort
            {
                Field = "patent_id",
                Direction = SortDirection.Ascending
            }
        };
        search.UseCursorPage = true;
        search.CursorPage.Size = 1;
        global::System.Collections.Generic.Dictionary<string, string?> query = PatentsViewQueryWriter.WriteGetQuery(search, Options.Limits);
        global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.PatentsViewBaseUri, new string[] { "api", "v1", "patent" }, query);
        PatentsViewQueryWriter.ValidateGetRequestUri(request.RequestUri, Options.Limits);
        return request;
    }

    internal PatentsViewCapabilityAttempt ExecuteAttempt(global::System.Net.Http.HttpRequestMessage request, RequestContext context, string[] secrets)
    {
        global::System.Threading.Tasks.Task<BufferedHttpResponse> task = global::System.Threading.Tasks.Task.Run(() => SendProbe(request, context, secrets));
        while (!task.IsCompleted)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            global::System.Threading.Thread.Yield();
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentsViewCapabilityAttempt result = new PatentsViewCapabilityAttempt();
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
            result.Failure = new ApiException("The PatentsView capability probe did not complete.", ApiService.PatentsView, 0, false, "CAPABILITY_PROBE_INCOMPLETE", null, null, false, null);
        }

        return result;
    }

    internal BufferedHttpResponse SendProbe(global::System.Net.Http.HttpRequestMessage request, RequestContext context, string[] secrets)
    {
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
        using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
        {
            RateLimiter.ObserveResponse(ApiService.PatentsView, response);
            int statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw ErrorMapper.MapResponse(response, ApiService.PatentsView, "PatentsView capability probe", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            ValidateSuccessfulResponse(buffered);
            return buffered;
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

    internal PatentsViewCapabilityResult CreateAvailableResult(BufferedHttpResponse response, RequestContext sourceContext, int attemptCount, bool fromCache)
    {
        PatentsViewCapabilityResult result = CreateBaseResult(sourceContext);
        result.IsAvailable = true;
        result.SupportsOnlineSearch = true;
        result.StatusCode = response.Metadata.StatusCode;
        result.HasStatusCode = true;
        result.RequestIdentifier = response.Metadata.RequestIdentifier;
        result.AttemptCount = attemptCount;
        result.FromCache = fromCache;
        result.Provenance.RequestIdentifier = response.Metadata.RequestIdentifier;
        result.Provenance.FromCache = fromCache;
        ProvenanceLabeler.AddPatentField(result.Provenance, "*", result.Provenance);
        if (response.HasRawText)
        {
            result.RawResponse = response.RawText;
        }

        return result;
    }

    internal PatentsViewCapabilityResult CreateUnavailableResult(global::System.Exception failure, RequestContext sourceContext, int attemptCount)
    {
        PatentsViewCapabilityResult result = CreateBaseResult(sourceContext);
        result.AttemptCount = attemptCount;
        result.UnavailabilityCode = "PATENTSVIEW_UNAVAILABLE";
        result.UnavailabilityReason = "The configured PatentsView PatentSearch service is unavailable.";
        result.IsRetryable = RetryClassifier.IsTransientFailure(failure, sourceContext.CancellationToken);

        if (RetryClassifier.HasExceptionType(failure, "uspto.AuthenticationException"))
        {
            AuthenticationException authentication = (AuthenticationException)failure;
            result.UnavailabilityCode = "PATENTSVIEW_AUTHENTICATION_FAILED";
            result.UnavailabilityReason = "The configured PatentsView PatentSearch service rejected the request credential.";
            result.StatusCode = authentication.StatusCode;
            result.HasStatusCode = authentication.HasStatusCode;
            result.RequestIdentifier = authentication.RequestIdentifier;
        }
        else if (RetryClassifier.HasExceptionType(failure, "uspto.NotFoundException"))
        {
            NotFoundException notFound = (NotFoundException)failure;
            result.UnavailabilityCode = "PATENTSVIEW_TRANSITION_UNAVAILABLE";
            result.UnavailabilityReason = "The configured PatentsView PatentSearch endpoint is not available and may have moved during the service transition.";
            result.StatusCode = notFound.StatusCode;
            result.HasStatusCode = notFound.HasStatusCode;
            result.RequestIdentifier = notFound.RequestIdentifier;
            result.IsTransitionUnavailable = true;
        }
        else if (RetryClassifier.HasExceptionType(failure, "uspto.RateLimitException"))
        {
            RateLimitException rateLimit = (RateLimitException)failure;
            result.UnavailabilityCode = "PATENTSVIEW_RATE_LIMITED";
            result.UnavailabilityReason = "The configured PatentsView PatentSearch service rate limit prevented the capability probe.";
            result.StatusCode = rateLimit.StatusCode;
            result.HasStatusCode = rateLimit.HasStatusCode;
            result.RequestIdentifier = rateLimit.RequestIdentifier;
            result.RetryAfterMilliseconds = rateLimit.RetryAfterMilliseconds;
            result.HasRetryAfterMilliseconds = rateLimit.HasRetryAfterMilliseconds;
        }
        else if (RetryClassifier.HasExceptionType(failure, "uspto.ServiceUnavailableException"))
        {
            ServiceUnavailableException serviceUnavailable = (ServiceUnavailableException)failure;
            result.UnavailabilityCode = "PATENTSVIEW_SERVICE_UNAVAILABLE";
            result.UnavailabilityReason = "The configured PatentsView PatentSearch service is temporarily unavailable.";
            result.StatusCode = serviceUnavailable.StatusCode;
            result.HasStatusCode = serviceUnavailable.HasStatusCode;
            result.RequestIdentifier = serviceUnavailable.RequestIdentifier;
            result.RetryAfterMilliseconds = serviceUnavailable.RetryAfterMilliseconds;
            result.HasRetryAfterMilliseconds = serviceUnavailable.HasRetryAfterMilliseconds;
            if (serviceUnavailable.StatusCode == 502 || serviceUnavailable.StatusCode == 503 || serviceUnavailable.StatusCode == 504)
            {
                result.IsTransitionUnavailable = true;
            }
        }
        else if (RetryClassifier.HasExceptionType(failure, "uspto.ApiException"))
        {
            ApiException api = (ApiException)failure;
            result.StatusCode = api.StatusCode;
            result.HasStatusCode = api.HasStatusCode;
            result.RequestIdentifier = api.RequestIdentifier;
            result.IsRetryable = api.IsRetryable;
            if (api.StatusCode == 410)
            {
                result.UnavailabilityCode = "PATENTSVIEW_TRANSITION_UNAVAILABLE";
                result.UnavailabilityReason = "The configured PatentsView PatentSearch endpoint is no longer available and may have moved during the service transition.";
                result.IsTransitionUnavailable = true;
            }
            else if (api.ErrorCode == "CAPABILITY_RESPONSE_INVALID")
            {
                result.UnavailabilityCode = "PATENTSVIEW_INVALID_RESPONSE";
                result.UnavailabilityReason = "The configured PatentsView PatentSearch endpoint returned an invalid capability response.";
            }
        }
        else if (RetryClassifier.HasExceptionType(failure, "System.Text.Json.JsonException"))
        {
            result.UnavailabilityCode = "PATENTSVIEW_INVALID_RESPONSE";
            result.UnavailabilityReason = "The configured PatentsView PatentSearch endpoint returned malformed JSON.";
        }
        else if (RetryClassifier.HasExceptionType(failure, "System.Net.Http.HttpRequestException") || RetryClassifier.HasExceptionType(failure, "System.Net.Sockets.SocketException") || RetryClassifier.HasExceptionType(failure, "System.IO.IOException"))
        {
            result.UnavailabilityCode = "PATENTSVIEW_UNREACHABLE";
            result.UnavailabilityReason = "The configured PatentsView PatentSearch endpoint could not be reached.";
        }

        result.Provenance.RequestIdentifier = result.RequestIdentifier;
        return result;
    }

    internal PatentsViewCapabilityResult CreateBaseResult(RequestContext sourceContext)
    {
        PatentsViewCapabilityResult result = new PatentsViewCapabilityResult();
        result.CapabilityName = "PatentsView PatentSearch";
        result.BaseUri = Options.PatentsViewBaseUri;
        result.IsBulkDiscoveryAvailable = true;
        result.BulkDatasetFamilies = new DatasetFamily[]
        {
            DatasetFamily.PatentsViewGrant,
            DatasetFamily.PatentsViewPreGrant,
            DatasetFamily.PatentsViewAnnualized,
            DatasetFamily.PatentsViewSorted
        };
        result.BulkProductSearch.Query = "PatentsView";
        result.BulkProductSearch.Context = CreateBulkSearchContext(sourceContext);
        result.Provenance.Source = DataProvenance.PatentsView;
        result.Provenance.SourceUri = Options.PatentsViewBaseUri;
        result.Provenance.DatasetIdentifier = "PatentsView PatentSearch";
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = false;
        return result;
    }

    internal static void ValidateSuccessfulResponse(BufferedHttpResponse response)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(response.Text))
        {
            if (document.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw new ApiException("The PatentsView capability response was not a JSON object.", ApiService.PatentsView, response.Metadata.StatusCode, true, "CAPABILITY_RESPONSE_INVALID", null, response.Metadata.RequestIdentifier, false, null);
            }

            bool hasError = false;
            bool isError = false;
            foreach (global::System.Text.Json.JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (property.NameEquals("error"))
                {
                    hasError = true;
                    if (property.Value.ValueKind == global::System.Text.Json.JsonValueKind.True)
                    {
                        isError = true;
                    }
                    else if (property.Value.ValueKind != global::System.Text.Json.JsonValueKind.False)
                    {
                        throw new ApiException("The PatentsView capability response contained an invalid error indicator.", ApiService.PatentsView, response.Metadata.StatusCode, true, "CAPABILITY_RESPONSE_INVALID", null, response.Metadata.RequestIdentifier, false, null);
                    }
                }
            }

            if (!hasError || isError)
            {
                throw new ApiException("The PatentsView capability response did not report a successful query.", ApiService.PatentsView, response.Metadata.StatusCode, true, "CAPABILITY_RESPONSE_INVALID", null, response.Metadata.RequestIdentifier, false, null);
            }
        }
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

    internal static RequestContext CreateBulkSearchContext(RequestContext source)
    {
        RequestContext context = CreateRequestContext(source);
        context.Service = ApiService.Odp;
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

    internal static void ValidateContext(RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A PatentsView capability context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
    }
}
