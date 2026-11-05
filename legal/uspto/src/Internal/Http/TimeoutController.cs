namespace uspto;

internal class HttpTimeoutScope
{
    internal global::System.Threading.CancellationToken CallerToken = default(global::System.Threading.CancellationToken);
    internal ApiService Service = ApiService.Unknown;
    internal int ConnectTimeoutMilliseconds = 0;
    internal int HeaderTimeoutMilliseconds = 0;
    internal int BodyTimeoutMilliseconds = 0;
    internal global::System.DateTimeOffset OverallDeadline = default(global::System.DateTimeOffset);

    internal HttpTimeoutScope(UsptoClientOptions options, RequestContext context, bool isDownload)
    {
        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        if (options.ConnectTimeoutMilliseconds <= 0)
        {
            throw new ValidationException("The connection timeout must be positive.", "ConnectTimeoutMilliseconds", "INVALID_TIMEOUT", null);
        }

        if (options.HeaderTimeoutMilliseconds <= 0)
        {
            throw new ValidationException("The response header timeout must be positive.", "HeaderTimeoutMilliseconds", "INVALID_TIMEOUT", null);
        }

        if (options.BodyTimeoutMilliseconds <= 0)
        {
            throw new ValidationException("The response body timeout must be positive.", "BodyTimeoutMilliseconds", "INVALID_TIMEOUT", null);
        }

        int overallTimeoutMilliseconds = options.OverallTimeoutMilliseconds;
        string overallTimeoutField = "OverallTimeoutMilliseconds";
        if (isDownload)
        {
            if (options.Download == null)
            {
                throw new ValidationException("A download policy is required.", "Download", "REQUIRED", null);
            }

            overallTimeoutMilliseconds = options.Download.TimeoutMilliseconds;
            overallTimeoutField = "Download.TimeoutMilliseconds";
        }

        if (overallTimeoutMilliseconds <= 0)
        {
            throw new ValidationException("The overall operation timeout must be positive.", overallTimeoutField, "INVALID_TIMEOUT", null);
        }

        CallerToken = context.CancellationToken;
        Service = context.Service;
        ConnectTimeoutMilliseconds = options.ConnectTimeoutMilliseconds;
        HeaderTimeoutMilliseconds = options.HeaderTimeoutMilliseconds;
        BodyTimeoutMilliseconds = options.BodyTimeoutMilliseconds;
        global::System.DateTimeOffset configuredDeadline = global::System.DateTimeOffset.UtcNow.AddMilliseconds(overallTimeoutMilliseconds);
        OverallDeadline = configuredDeadline;
        if (context.HasDeadline && context.Deadline < configuredDeadline)
        {
            OverallDeadline = context.Deadline;
        }
    }
}

internal static class TimeoutController
{
    internal static void ApplyConnectTimeout(global::System.Net.Http.SocketsHttpHandler handler, HttpTimeoutScope scope)
    {
        if (handler == null)
        {
            throw new ValidationException("A sockets HTTP handler is required.", "handler", "REQUIRED", null);
        }

        if (scope == null)
        {
            throw new ValidationException("A timeout scope is required.", "scope", "REQUIRED", null);
        }

        handler.ConnectTimeout = global::System.TimeSpan.FromMilliseconds(scope.ConnectTimeoutMilliseconds);
    }

    internal static void RunConnectPhase(global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task> operation, HttpTimeoutScope scope)
    {
        if (operation == null)
        {
            throw new ValidationException("A connection operation is required.", "operation", "REQUIRED", null);
        }

        ThrowIfStopped(scope);
        global::System.Threading.CancellationTokenSource linked = global::System.Threading.CancellationTokenSource.CreateLinkedTokenSource(scope.CallerToken);
        global::System.Threading.Tasks.Task task = operation(linked.Token);
        if (task == null)
        {
            linked.Dispose();
            throw new ValidationException("The connection operation returned no task.", "operation", "INVALID_OPERATION", null);
        }

        WaitForPhase(task, scope.ConnectTimeoutMilliseconds, "CONNECT_TIMEOUT", scope, linked);
    }

    internal static global::System.Net.Http.HttpResponseMessage SendHeaderPhase(global::System.Net.Http.HttpClient client, global::System.Net.Http.HttpRequestMessage request, HttpTimeoutScope scope)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("An HTTP request is required.", "request", "REQUIRED", null);
        }

        ThrowIfStopped(scope);
        global::System.Threading.CancellationTokenSource linked = global::System.Threading.CancellationTokenSource.CreateLinkedTokenSource(scope.CallerToken);
        global::System.Threading.Tasks.Task<global::System.Net.Http.HttpResponseMessage> task = client.SendAsync(request, global::System.Net.Http.HttpCompletionOption.ResponseHeadersRead, linked.Token);
        WaitForPhase(task, scope.HeaderTimeoutMilliseconds, "HEADER_TIMEOUT", scope, linked);
        global::System.Net.Http.HttpResponseMessage response = task.GetAwaiter().GetResult();
        if (response.RequestMessage == null)
        {
            response.RequestMessage = request;
        }

        return response;
    }

    internal static BufferedHttpResponse ReadTextBodyPhase(global::System.Net.Http.HttpResponseMessage response, DocumentFormat expectedFormat, RequestContext context, UsptoLimits limits, string[]? secrets, HttpTimeoutScope scope)
    {
        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        ThrowIfStopped(scope);
        global::System.Threading.CancellationTokenSource linked = global::System.Threading.CancellationTokenSource.CreateLinkedTokenSource(scope.CallerToken);
        RequestContext phaseContext = CreatePhaseContext(context, linked.Token);
        global::System.Threading.Tasks.Task<BufferedHttpResponse> task = global::System.Threading.Tasks.Task.Run(() => ResponseReader.ReadTextResponse(response, expectedFormat, phaseContext, limits, secrets), global::System.Threading.CancellationToken.None);
        WaitForPhase(task, scope.BodyTimeoutMilliseconds, "BODY_TIMEOUT", scope, linked);
        return task.GetAwaiter().GetResult();
    }

    internal static StreamedHttpResponse CopyBinaryBodyPhase(global::System.Net.Http.HttpResponseMessage response, global::System.IO.Stream destination, int bufferSize, RequestContext context, string[]? secrets, HttpTimeoutScope scope)
    {
        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        ThrowIfStopped(scope);
        global::System.Threading.CancellationTokenSource linked = global::System.Threading.CancellationTokenSource.CreateLinkedTokenSource(scope.CallerToken);
        RequestContext phaseContext = CreatePhaseContext(context, linked.Token);
        global::System.Threading.Tasks.Task<StreamedHttpResponse> task = global::System.Threading.Tasks.Task.Run(() => ResponseReader.CopyBinaryResponse(response, destination, bufferSize, phaseContext, secrets), global::System.Threading.CancellationToken.None);
        WaitForPhase(task, scope.BodyTimeoutMilliseconds, "BODY_TIMEOUT", scope, linked);
        return task.GetAwaiter().GetResult();
    }

    internal static StreamedHttpResponse CopyBoundedBinaryBodyPhase(global::System.Net.Http.HttpResponseMessage response, global::System.IO.Stream destination, RequestContext context, string[]? secrets, HttpTimeoutScope scope, DownloadPolicy policy, DownloadRequest request, string? fileName)
    {
        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        ThrowIfStopped(scope);
        global::System.Threading.CancellationTokenSource linked = global::System.Threading.CancellationTokenSource.CreateLinkedTokenSource(scope.CallerToken);
        RequestContext phaseContext = CreatePhaseContext(context, linked.Token);
        global::System.Threading.Tasks.Task<StreamedHttpResponse> task = global::System.Threading.Tasks.Task.Run(() => ResponseReader.CopyBoundedBinaryResponse(response, destination, phaseContext, secrets, policy, request, fileName), global::System.Threading.CancellationToken.None);
        WaitForPhase(task, scope.BodyTimeoutMilliseconds, "BODY_TIMEOUT", scope, linked);
        return task.GetAwaiter().GetResult();
    }

    internal static StreamedHttpResponse CopyChecksummedBinaryBodyPhase(global::System.Net.Http.HttpResponseMessage response, global::System.IO.Stream destination, RequestContext context, string[]? secrets, HttpTimeoutScope scope, DownloadPolicy policy, DownloadRequest request, string? fileName, BulkChecksumSession checksumSession)
    {
        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        ThrowIfStopped(scope);
        global::System.Threading.CancellationTokenSource linked = global::System.Threading.CancellationTokenSource.CreateLinkedTokenSource(scope.CallerToken);
        RequestContext phaseContext = CreatePhaseContext(context, linked.Token);
        global::System.Threading.Tasks.Task<StreamedHttpResponse> task = global::System.Threading.Tasks.Task.Run(() => ResponseReader.CopyChecksummedBinaryResponse(response, destination, phaseContext, secrets, policy, request, fileName, checksumSession), global::System.Threading.CancellationToken.None);
        WaitForPhase(task, scope.BodyTimeoutMilliseconds, "BODY_TIMEOUT", scope, linked);
        return task.GetAwaiter().GetResult();
    }

    internal static void WaitForPhase(global::System.Threading.Tasks.Task operation, int phaseTimeoutMilliseconds, string timeoutCode, HttpTimeoutScope scope, global::System.Threading.CancellationTokenSource linked)
    {
        global::System.DateTimeOffset now = global::System.DateTimeOffset.UtcNow;
        global::System.DateTimeOffset phaseDeadline = now.AddMilliseconds(phaseTimeoutMilliseconds);
        global::System.TimeSpan phaseRemaining = phaseDeadline - now;
        global::System.TimeSpan overallRemaining = scope.OverallDeadline - now;
        if (overallRemaining <= global::System.TimeSpan.Zero)
        {
            linked.Cancel();
            linked.Dispose();
            throw CreateTimeoutFailure("OVERALL_TIMEOUT", scope.Service);
        }

        global::System.Threading.Tasks.Task phaseDelay = global::System.Threading.Tasks.Task.Delay(phaseRemaining);
        global::System.Threading.Tasks.Task overallDelay = global::System.Threading.Tasks.Task.Delay(overallRemaining);
        global::System.Threading.Tasks.Task callerDelay = global::System.Threading.Tasks.Task.Delay(global::System.Threading.Timeout.Infinite, scope.CallerToken);
        global::System.Threading.Tasks.Task winner = global::System.Threading.Tasks.Task.WhenAny(new global::System.Threading.Tasks.Task[] { operation, phaseDelay, overallDelay, callerDelay }).GetAwaiter().GetResult();

        if (scope.CallerToken.IsCancellationRequested)
        {
            linked.Cancel();
            linked.Dispose();
            scope.CallerToken.ThrowIfCancellationRequested();
        }

        if (global::System.Object.ReferenceEquals(winner, operation) || operation.IsCompleted)
        {
            if (operation.IsCanceled)
            {
                linked.Dispose();
                throw new global::System.OperationCanceledException("The HTTP operation was canceled.");
            }

            if (operation.IsFaulted)
            {
                global::System.Exception failure = operation.Exception.GetBaseException();
                linked.Dispose();
                throw failure;
            }

            linked.Dispose();
            return;
        }

        linked.Cancel();
        linked.Dispose();
        if (global::System.Object.ReferenceEquals(winner, overallDelay) || global::System.DateTimeOffset.UtcNow >= scope.OverallDeadline)
        {
            throw CreateTimeoutFailure("OVERALL_TIMEOUT", scope.Service);
        }

        throw CreateTimeoutFailure(timeoutCode, scope.Service);
    }

    internal static void ThrowIfStopped(HttpTimeoutScope scope)
    {
        if (scope == null)
        {
            throw new ValidationException("A timeout scope is required.", "scope", "REQUIRED", null);
        }

        scope.CallerToken.ThrowIfCancellationRequested();
        if (global::System.DateTimeOffset.UtcNow >= scope.OverallDeadline)
        {
            throw CreateTimeoutFailure("OVERALL_TIMEOUT", scope.Service);
        }
    }

    internal static RequestContext CreatePhaseContext(RequestContext context, global::System.Threading.CancellationToken cancellationToken)
    {
        RequestContext phaseContext = new RequestContext();
        phaseContext.Service = context.Service;
        phaseContext.CancellationToken = cancellationToken;
        phaseContext.RetainRawResponse = context.RetainRawResponse;
        phaseContext.MaximumRawResponseBytes = context.MaximumRawResponseBytes;
        phaseContext.Deadline = context.Deadline;
        phaseContext.HasDeadline = context.HasDeadline;
        phaseContext.CorrelationIdentifier = context.CorrelationIdentifier;
        phaseContext.Fallback = context.Fallback;
        return phaseContext;
    }

    internal static ApiException CreateTimeoutFailure(string errorCode, ApiService service)
    {
        string message = "The HTTP operation exceeded its configured timeout.";
        if (errorCode == "CONNECT_TIMEOUT")
        {
            message = "The HTTP connection was not established before its timeout.";
        }
        else if (errorCode == "HEADER_TIMEOUT")
        {
            message = "The HTTP response headers were not received before their timeout.";
        }
        else if (errorCode == "BODY_TIMEOUT")
        {
            message = "The HTTP response body was not completed before its timeout.";
        }
        else if (errorCode == "OVERALL_TIMEOUT")
        {
            message = "The HTTP operation exceeded its overall deadline.";
        }

        return new ApiException(message, service, 0, false, errorCode, null, null, false, null);
    }
}
