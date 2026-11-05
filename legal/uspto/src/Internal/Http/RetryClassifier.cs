namespace uspto;

internal static class RetryClassifier
{
    internal static bool ShouldRetry(global::System.Net.Http.HttpRequestMessage request, bool isSearchOperation, global::System.Exception failure, global::System.Threading.CancellationToken callerCancellationToken)
    {
        if (!IsRequestReplaySafe(request, isSearchOperation))
        {
            return false;
        }

        return IsTransientFailure(failure, callerCancellationToken);
    }

    internal static bool IsRequestReplaySafe(global::System.Net.Http.HttpRequestMessage request, bool isSearchOperation)
    {
        if (request == null)
        {
            throw new ValidationException("An HTTP request is required for retry classification.", "request", "REQUIRED", null);
        }

        if (request.Method == global::System.Net.Http.HttpMethod.Get)
        {
            return true;
        }

        if (request.Method == global::System.Net.Http.HttpMethod.Post && isSearchOperation)
        {
            return true;
        }

        return false;
    }

    internal static bool IsTransientFailure(global::System.Exception failure, global::System.Threading.CancellationToken callerCancellationToken)
    {
        if (failure == null)
        {
            throw new ValidationException("A failure is required for retry classification.", "failure", "REQUIRED", null);
        }

        if (callerCancellationToken.IsCancellationRequested)
        {
            return false;
        }

        if (HasExceptionType(failure, "System.OperationCanceledException"))
        {
            return false;
        }

        if (HasExceptionType(failure, "uspto.ApiException"))
        {
            ApiException apiFailure = (ApiException)failure;
            if (IsDeadlineFailure(apiFailure.ErrorCode))
            {
                return false;
            }

            return apiFailure.IsRetryable;
        }

        if (HasExceptionType(failure, "uspto.RateLimitException"))
        {
            RateLimitException rateFailure = (RateLimitException)failure;
            return rateFailure.HasStatusCode && rateFailure.StatusCode == 429;
        }

        if (HasExceptionType(failure, "uspto.ServiceUnavailableException"))
        {
            ServiceUnavailableException serviceFailure = (ServiceUnavailableException)failure;
            return serviceFailure.HasStatusCode && IsTransientStatusCode(serviceFailure.StatusCode);
        }

        if (HasExceptionType(failure, "System.Net.Http.HttpRequestException"))
        {
            global::System.Net.Http.HttpRequestException requestFailure = (global::System.Net.Http.HttpRequestException)failure;
            if (requestFailure.StatusCode.HasValue)
            {
                return IsTransientStatusCode((int)requestFailure.StatusCode.GetValueOrDefault());
            }

            return true;
        }

        if (HasExceptionType(failure, "System.Net.Sockets.SocketException"))
        {
            return true;
        }

        if (HasExceptionType(failure, "System.IO.IOException"))
        {
            return true;
        }

        return false;
    }

    internal static bool IsTransientStatusCode(int statusCode)
    {
        return statusCode == 408 || statusCode == 429 || statusCode == 500 || statusCode == 502 || statusCode == 503 || statusCode == 504;
    }

    internal static bool IsDeadlineFailure(string? errorCode)
    {
        return errorCode == "CONNECT_TIMEOUT" || errorCode == "HEADER_TIMEOUT" || errorCode == "BODY_TIMEOUT" || errorCode == "OVERALL_TIMEOUT";
    }

    internal static bool HasExceptionType(global::System.Exception failure, string fullName)
    {
        global::System.Type? current = failure.GetType();
        while (current != null)
        {
            if (current.FullName == fullName)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
