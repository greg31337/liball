namespace uspto;

internal class DataFallbackCapabilityExecution
{
    internal DataFallbackCapability? Capability = null;
    internal global::System.Exception? Failure = null;
    internal bool IsSuccessful = false;
}

internal class DataFallbackValueExecution
{
    internal DataFallbackValue? Result = null;
    internal global::System.Exception? Failure = null;
    internal bool IsSuccessful = false;
}

internal class DataFallbackCoordinator
{
    internal DataFallbackPolicy Policy;

    internal DataFallbackCoordinator(DataFallbackPolicy policy)
    {
        ValidatePolicy(policy);
        Policy = policy;
    }

    internal DataFallbackResult Resolve(IDataFallbackSource source, RequestContext context, global::System.DateTimeOffset now)
    {
        if (source == null)
        {
            throw new ValidationException("A fallback data source is required.", "source", "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("A fallback request context is required.", "context", "REQUIRED", null);
        }

        DataFallbackPolicy policy = context.Fallback ?? Policy;
        ValidatePolicy(policy);
        context.CancellationToken.ThrowIfCancellationRequested();
        global::System.Collections.Generic.List<DataFallbackAttempt> attempts = new global::System.Collections.Generic.List<DataFallbackAttempt>();
        global::System.Collections.Generic.List<SectionError> errors = new global::System.Collections.Generic.List<SectionError>();
        DataFallbackResult result = new DataFallbackResult();
        int index;
        for (index = 0; index < policy.AccessOrder.Length; index += 1)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            FallbackAccess access = policy.AccessOrder[index];
            DataFallbackAttempt attempt = new DataFallbackAttempt();
            attempt.Access = access;
            attempts.Add(attempt);
            DataFallbackCapabilityExecution capabilityExecution = ReadCapability(source, access, context);
            attempt.CapabilityChecked = true;
            if (!capabilityExecution.IsSuccessful || capabilityExecution.Capability == null)
            {
                attempt.RejectionCode = "CAPABILITY_CHECK_FAILED";
                attempt.Error = AddFailure(errors, access, capabilityExecution.Failure);
                continue;
            }

            DataFallbackCapability capability = capabilityExecution.Capability;
            if (capability.Provenance == null)
            {
                attempt.RejectionCode = "CAPABILITY_PROVENANCE_MISSING";
                continue;
            }

            attempt.IsAvailable = capability.IsAvailable;
            attempt.SupportsOperation = capability.SupportsOperation;
            attempt.Provenance = ProvenanceLabeler.CopyPatent(capability.Provenance);
            if (!capability.IsAvailable)
            {
                attempt.RejectionCode = "SOURCE_UNAVAILABLE";
                continue;
            }

            if (!capability.SupportsOperation || capability.Access != access)
            {
                attempt.RejectionCode = "CAPABILITY_UNSUPPORTED";
                continue;
            }

            if (!MatchesAuthority(policy.Authority, capability.Provenance))
            {
                attempt.RejectionCode = "AUTHORITY_MISMATCH";
                continue;
            }

            attempt.AuthorityAccepted = true;
            if (!MatchesDelivery(access, capability.Provenance))
            {
                attempt.RejectionCode = "DELIVERY_MISMATCH";
                continue;
            }

            if (!IsFresh(access, capability.Provenance, policy, now))
            {
                attempt.RejectionCode = FreshnessCode(access, capability.Provenance);
                continue;
            }

            attempt.IsFresh = true;
            attempt.LoadAttempted = true;
            DataFallbackValueExecution valueExecution = ReadValue(source, access, context);
            if (!valueExecution.IsSuccessful || valueExecution.Result == null || valueExecution.Result.Value == null)
            {
                attempt.RejectionCode = "LOAD_FAILED";
                attempt.Error = AddFailure(errors, access, valueExecution.Failure);
                continue;
            }

            DataFallbackValue value = valueExecution.Result;
            if (value.Provenance == null)
            {
                attempt.RejectionCode = "RESULT_PROVENANCE_MISSING";
                continue;
            }

            attempt.Provenance = ProvenanceLabeler.CopyPatent(value.Provenance);
            if (!MatchesAuthority(policy.Authority, value.Provenance))
            {
                attempt.AuthorityAccepted = false;
                attempt.RejectionCode = "RESULT_AUTHORITY_MISMATCH";
                continue;
            }

            if (!MatchesDelivery(access, value.Provenance))
            {
                attempt.RejectionCode = "RESULT_DELIVERY_MISMATCH";
                continue;
            }

            if (!IsFresh(access, value.Provenance, policy, now))
            {
                attempt.IsFresh = false;
                attempt.RejectionCode = "RESULT_STALE";
                continue;
            }

            attempt.IsSelected = true;
            result.Value = value.Value;
            result.Provenance = ProvenanceLabeler.CopyPatent(value.Provenance);
            result.SelectedAccess = access;
            result.IsSuccessful = true;
            break;
        }

        result.Attempts = attempts.ToArray();
        result.Errors = errors.ToArray();
        return result;
    }

    internal static DataFallbackCapabilityExecution ReadCapability(IDataFallbackSource source, FallbackAccess access, RequestContext context)
    {
        global::System.Threading.Tasks.Task<DataFallbackCapability> task = global::System.Threading.Tasks.Task.Run(() => ExecuteCapability(source, access, context));
        while (!task.IsCompleted)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            global::System.Threading.Thread.Yield();
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        DataFallbackCapabilityExecution result = new DataFallbackCapabilityExecution();
        if (task.IsCompletedSuccessfully)
        {
            result.Capability = task.Result;
            result.IsSuccessful = true;
        }
        else if (task.Exception != null)
        {
            result.Failure = task.Exception.GetBaseException();
        }

        return result;
    }

    internal static DataFallbackCapability ExecuteCapability(IDataFallbackSource source, FallbackAccess access, RequestContext context)
    {
        switch (access)
        {
            case FallbackAccess.Online:
                return source.GetOnlineFallbackCapability(context);
            case FallbackAccess.Cache:
                return source.GetCacheFallbackCapability(context);
            case FallbackAccess.Local:
                return source.GetLocalFallbackCapability(context);
            default:
                throw new ValidationException("The fallback access path is invalid.", "AccessOrder", "INVALID_FALLBACK_ACCESS", null);
        }
    }

    internal static DataFallbackValueExecution ReadValue(IDataFallbackSource source, FallbackAccess access, RequestContext context)
    {
        global::System.Threading.Tasks.Task<DataFallbackValue> task = global::System.Threading.Tasks.Task.Run(() => ExecuteValue(source, access, context));
        while (!task.IsCompleted)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            global::System.Threading.Thread.Yield();
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        DataFallbackValueExecution result = new DataFallbackValueExecution();
        if (task.IsCompletedSuccessfully)
        {
            result.Result = task.Result;
            result.IsSuccessful = true;
        }
        else if (task.Exception != null)
        {
            result.Failure = task.Exception.GetBaseException();
        }

        return result;
    }

    internal static DataFallbackValue ExecuteValue(IDataFallbackSource source, FallbackAccess access, RequestContext context)
    {
        switch (access)
        {
            case FallbackAccess.Online:
                return source.LoadOnlineFallbackValue(context);
            case FallbackAccess.Cache:
                return source.LoadCacheFallbackValue(context);
            case FallbackAccess.Local:
                return source.LoadLocalFallbackValue(context);
            default:
                throw new ValidationException("The fallback access path is invalid.", "AccessOrder", "INVALID_FALLBACK_ACCESS", null);
        }
    }

    internal static bool MatchesAuthority(FallbackAuthority requirement, PatentDataProvenance provenance)
    {
        switch (requirement)
        {
            case FallbackAuthority.AuthoritativeOnly:
                return IsVerifiedAuthoritative(provenance);
            case FallbackAuthority.ResearchOnly:
                return !provenance.IsAuthoritative;
            case FallbackAuthority.Either:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsVerifiedAuthoritative(PatentDataProvenance provenance)
    {
        if (!provenance.IsAuthoritative || provenance.Source == DataProvenance.PatentsView || provenance.Source == DataProvenance.Unknown)
        {
            return false;
        }

        if (provenance.FieldSources.Length > 0)
        {
            int index;
            for (index = 0; index < provenance.FieldSources.Length; index += 1)
            {
                FieldDataProvenance field = provenance.FieldSources[index];
                if (!field.IsAuthoritative || field.Source == DataProvenance.PatentsView || field.Source == DataProvenance.Unknown)
                {
                    return false;
                }
            }
        }

        if (provenance.Source == DataProvenance.LocalMirror || provenance.Source == DataProvenance.Cache || provenance.Source == DataProvenance.ClientDerived)
        {
            return provenance.FieldSources.Length > 0;
        }

        return true;
    }

    internal static bool MatchesDelivery(FallbackAccess access, PatentDataProvenance provenance)
    {
        if (access == FallbackAccess.Online)
        {
            return !provenance.FromCache && provenance.Source != DataProvenance.Cache && provenance.Source != DataProvenance.LocalMirror;
        }

        if (access == FallbackAccess.Cache)
        {
            return provenance.FromCache || provenance.Source == DataProvenance.Cache || HasDelivery(provenance.FieldSources, DataProvenance.Cache);
        }

        if (access == FallbackAccess.Local)
        {
            return provenance.Source == DataProvenance.LocalMirror || HasDelivery(provenance.FieldSources, DataProvenance.LocalMirror);
        }

        return false;
    }

    internal static bool HasDelivery(FieldDataProvenance[] fields, DataProvenance delivery)
    {
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            if (fields[index].Delivery == delivery)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsFresh(FallbackAccess access, PatentDataProvenance provenance, DataFallbackPolicy policy, global::System.DateTimeOffset now)
    {
        int maximumAge = 0;
        bool enforce = false;
        if (access == FallbackAccess.Cache)
        {
            maximumAge = policy.MaximumCacheAgeSeconds;
            enforce = policy.HasMaximumCacheAge;
        }
        else if (access == FallbackAccess.Local)
        {
            maximumAge = policy.MaximumLocalAgeSeconds;
            enforce = policy.HasMaximumLocalAge;
        }

        if (!enforce)
        {
            return true;
        }

        global::System.DateTimeOffset timestamp = provenance.RetrievedAt;
        bool hasTimestamp = provenance.HasRetrievedAt;
        if (access == FallbackAccess.Local && provenance.HasLastUpdatedAt)
        {
            timestamp = provenance.LastUpdatedAt;
            hasTimestamp = true;
        }

        if (!hasTimestamp || timestamp > now)
        {
            return false;
        }

        global::System.TimeSpan age = now - timestamp;
        return age.TotalSeconds <= maximumAge;
    }

    internal static string FreshnessCode(FallbackAccess access, PatentDataProvenance provenance)
    {
        if (!provenance.HasRetrievedAt && !provenance.HasLastUpdatedAt)
        {
            return "FRESHNESS_UNKNOWN";
        }

        if (access == FallbackAccess.Cache)
        {
            return "CACHE_STALE";
        }

        return "LOCAL_STALE";
    }

    internal static SectionError? AddFailure(global::System.Collections.Generic.List<SectionError> errors, FallbackAccess access, global::System.Exception? failure)
    {
        if (failure == null)
        {
            return null;
        }

        SectionError error = DossierBuilder.CreateSectionError(0, failure);
        error.Section = AccessName(access);
        errors.Add(error);
        return error;
    }

    internal static string AccessName(FallbackAccess access)
    {
        switch (access)
        {
            case FallbackAccess.Online:
                return "online";
            case FallbackAccess.Cache:
                return "cache";
            case FallbackAccess.Local:
                return "local";
            default:
                return "unknown";
        }
    }

    internal static void ValidatePolicy(DataFallbackPolicy policy)
    {
        if (policy == null || policy.AccessOrder == null)
        {
            throw new ValidationException("A complete fallback policy is required.", "Fallback", "REQUIRED", null);
        }

        if (policy.AccessOrder.Length == 0 || policy.AccessOrder.Length > 3)
        {
            throw new ValidationException("Fallback access order must contain one to three paths.", "AccessOrder", "INVALID_FALLBACK_ORDER", null);
        }

        if (policy.HasMaximumCacheAge && policy.MaximumCacheAgeSeconds < 0 || policy.HasMaximumLocalAge && policy.MaximumLocalAgeSeconds < 0)
        {
            throw new ValidationException("Fallback freshness limits cannot be negative.", "Fallback", "INVALID_FALLBACK_FRESHNESS", null);
        }

        if (policy.Authority != FallbackAuthority.AuthoritativeOnly && policy.Authority != FallbackAuthority.ResearchOnly && policy.Authority != FallbackAuthority.Either)
        {
            throw new ValidationException("The fallback authority requirement is invalid.", "Authority", "INVALID_FALLBACK_AUTHORITY", null);
        }

        bool online = false;
        bool cache = false;
        bool local = false;
        int index;
        for (index = 0; index < policy.AccessOrder.Length; index += 1)
        {
            FallbackAccess access = policy.AccessOrder[index];
            if (access == FallbackAccess.Online)
            {
                if (online)
                {
                    throw new ValidationException("Fallback access paths cannot be repeated.", "AccessOrder", "DUPLICATE_FALLBACK_ACCESS", null);
                }

                online = true;
            }
            else if (access == FallbackAccess.Cache)
            {
                if (cache)
                {
                    throw new ValidationException("Fallback access paths cannot be repeated.", "AccessOrder", "DUPLICATE_FALLBACK_ACCESS", null);
                }

                cache = true;
            }
            else if (access == FallbackAccess.Local)
            {
                if (local)
                {
                    throw new ValidationException("Fallback access paths cannot be repeated.", "AccessOrder", "DUPLICATE_FALLBACK_ACCESS", null);
                }

                local = true;
            }
            else
            {
                throw new ValidationException("Fallback access order contains an invalid path.", "AccessOrder", "INVALID_FALLBACK_ACCESS", null);
            }
        }
    }
}
