using System;
using System.Threading;

namespace jmap;

internal class RequestLimitLease : IDisposable
{
    internal SemaphoreSlim? Gate;

    internal RequestLimitLease(SemaphoreSlim gate)
    {
        Gate = gate;
    }

    /// <summary>Releases the admitted operation once.</summary>
    public void Dispose()
    {
        SemaphoreSlim? gate = Gate;
        Gate = null;
        if (gate != null)
        {
            gate.Release();
        }
    }
}

internal class RequestLimitAdmission
{
    internal bool IsAllowed;
    internal RequestLimitLease? Lease;
    internal RequestValidationResult? Problem;
}

internal class RequestLimitGate
{
    internal JmapServerOptions Options;
    internal SemaphoreSlim ApiRequests;
    internal SemaphoreSlim Uploads;
    internal ValueValidator Values = new ValueValidator();

    internal RequestLimitGate(JmapServerOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ValidateOptions(options);
        Options = options;
        ApiRequests = new SemaphoreSlim(
            options.MaxConcurrentRequests, options.MaxConcurrentRequests);
        Uploads = new SemaphoreSlim(
            options.MaxConcurrentUploads, options.MaxConcurrentUploads);
    }

    internal RequestLimitAdmission AcquireApiRequest(
        long requestBytes,
        int methodCallCount,
        CancellationToken cancellationToken)
    {
        RequestValidationResult validation = ValidateApiRequest(
            requestBytes, methodCallCount);
        if (!validation.IsValid)
        {
            return DeniedAdmission(validation);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!ApiRequests.Wait(0, cancellationToken))
        {
            return DeniedAdmission(LimitProblem(
                JmapProperties.MaxConcurrentRequests,
                "The concurrent API request limit was reached."));
        }

        return AllowedAdmission(new RequestLimitLease(ApiRequests));
    }

    internal RequestLimitAdmission AcquireUpload(
        long contentLength,
        bool hasContentLength,
        CancellationToken cancellationToken)
    {
        if (hasContentLength &&
            (!Values.IsUnsignedInt(contentLength) || contentLength > Options.MaxUploadBytes))
        {
            return DeniedAdmission(LimitProblem(
                JmapProperties.MaxSizeUpload,
                "The upload exceeds the configured byte limit."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!Uploads.Wait(0, cancellationToken))
        {
            return DeniedAdmission(LimitProblem(
                JmapProperties.MaxConcurrentUpload,
                "The concurrent upload limit was reached."));
        }

        return AllowedAdmission(new RequestLimitLease(Uploads));
    }

    internal RequestValidationResult ValidateApiRequest(
        long requestBytes,
        int methodCallCount)
    {
        if (requestBytes < 0L || methodCallCount < 0)
        {
            return RequestProblem(
                JmapProblemKind.NotRequest,
                null,
                "The measured request size or method-call count is invalid.");
        }

        if (requestBytes > Options.MaxRequestBytes)
        {
            return LimitProblem(
                JmapProperties.MaxSizeRequest,
                "The request exceeds the configured byte limit.");
        }

        if (methodCallCount > Options.MaxCallsInRequest)
        {
            return LimitProblem(
                JmapProperties.MaxCallsInRequest,
                "The request contains too many method calls.");
        }

        RequestValidationResult result = new RequestValidationResult();
        result.IsValid = true;
        return result;
    }

    internal JmapMethodError? ValidateGetObjectCount(int objectCount)
    {
        if (objectCount < 0)
        {
            throw new ArgumentOutOfRangeException("objectCount");
        }

        if (objectCount <= Options.MaxObjectsInGet)
        {
            return null;
        }

        return ObjectLimitError("The get request exceeds maxObjectsInGet.");
    }

    internal JmapMethodError? ValidateSetObjectCount(int objectCount)
    {
        if (objectCount < 0)
        {
            throw new ArgumentOutOfRangeException("objectCount");
        }

        if (objectCount <= Options.MaxObjectsInSet)
        {
            return null;
        }

        return ObjectLimitError("The set request exceeds maxObjectsInSet.");
    }

    internal RequestLimitAdmission AllowedAdmission(RequestLimitLease lease)
    {
        RequestLimitAdmission admission = new RequestLimitAdmission();
        admission.IsAllowed = true;
        admission.Lease = lease;
        return admission;
    }

    internal RequestLimitAdmission DeniedAdmission(RequestValidationResult problem)
    {
        RequestLimitAdmission admission = new RequestLimitAdmission();
        admission.Problem = problem;
        return admission;
    }

    internal RequestValidationResult LimitProblem(string limit, string detail)
    {
        return RequestProblem(JmapProblemKind.Limit, limit, detail);
    }

    internal RequestValidationResult RequestProblem(
        JmapProblemKind kind,
        string? limit,
        string detail)
    {
        RequestValidationResult result = new RequestValidationResult();
        result.ProblemKind = kind;
        result.Limit = limit;
        result.Detail = detail;
        return result;
    }

    internal JmapMethodError ObjectLimitError(string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = JmapErrorCodes.RequestTooLarge;
        error.Kind = JmapMethodErrorKind.RequestTooLarge;
        error.Description = description;
        return error;
    }

    internal void ValidateOptions(JmapServerOptions options)
    {
        if (!Values.IsUnsignedInt(options.MaxRequestBytes) || options.MaxRequestBytes == 0L ||
            options.MaxRequestBytes > int.MaxValue ||
            !Values.IsUnsignedInt(options.MaxUploadBytes) || options.MaxUploadBytes == 0L ||
            options.MaxConcurrentRequests <= 0 || options.MaxConcurrentUploads <= 0 ||
            options.MaxCallsInRequest < 0 || options.MaxObjectsInGet < 0 ||
            options.MaxObjectsInSet < 0 || options.MaxJsonDepth <= 0 ||
            options.MaxJsonDepth > SecurityLimits.MaximumJsonDepth)
        {
            throw new ArgumentException("Request limit options are invalid.", "options");
        }
    }
}
