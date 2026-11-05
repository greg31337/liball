using System;
using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal enum StandardChangeKind
{
    None = 0,
    Created = 1,
    Updated = 2,
    Destroyed = 3
}

internal class StandardChange
{
    internal string? Id;
    internal StandardChangeKind Kind;
    internal string? StateAfter;
}

internal class StandardChangesData
{
    internal bool CanCalculateChanges = true;
    internal bool CanCalculateIntermediateStates = true;
    internal string? CurrentState;
    internal List<StandardChange> Changes = new List<StandardChange>();
}

internal class StandardChangeStatus
{
    internal bool WasCreated;
    internal bool WasUpdated;
    internal bool WasDestroyed;
}

internal class StandardChangesResult
{
    internal bool IsSuccess;
    internal JmapChangesResponse? Response;
    internal JmapMethodError? Error;
}

internal class StandardChangesProcessor
{
    internal long DefaultMaximumChanges;
    internal ValueValidator Values = new ValueValidator();

    internal StandardChangesProcessor(long defaultMaximumChanges)
    {
        if (defaultMaximumChanges <= 0L || !Values.IsUnsignedInt(defaultMaximumChanges))
        {
            throw new ArgumentOutOfRangeException("defaultMaximumChanges");
        }

        DefaultMaximumChanges = defaultMaximumChanges;
    }

    internal StandardChangesResult ProcessChanges(JmapChangesRequest? request, StandardChangesData? data)
    {
        if (request == null)
        {
            return ErrorResult(MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A changes request is required."));
        }

        JmapMethodError? requestError = ValidateRequest(request);
        if (requestError != null)
        {
            return ErrorResult(requestError);
        }

        if (data == null)
        {
            throw ProtocolFailure("Changes data is required.", "", default(JsonElement));
        }

        if (!data.CanCalculateChanges)
        {
            return CannotCalculateResult("Changes cannot be calculated from the requested state.");
        }

        ValidateChangesData(data);
        long maximumChanges = EffectiveMaximum(request);
        Dictionary<string, StandardChangeStatus> completeStatuses = AccumulateStatuses(data.Changes, data.Changes.Count);
        long completeCount = CountClassified(completeStatuses);
        if (completeCount <= maximumChanges)
        {
            return SuccessResult(request, data, completeStatuses, data.CurrentState, false, data.Changes.Count);
        }

        if (!data.CanCalculateIntermediateStates)
        {
            return CannotCalculateResult("The changes exceed the limit and no intermediate state can be calculated.");
        }

        int boundary = FindIntermediateBoundary(data.Changes, maximumChanges);
        if (boundary == 0 || data.Changes[boundary - 1].StateAfter == null)
        {
            return CannotCalculateResult("A usable intermediate changes state is unavailable.");
        }

        Dictionary<string, StandardChangeStatus> pageStatuses = AccumulateStatuses(data.Changes, boundary);
        return SuccessResult(request, data, pageStatuses, data.Changes[boundary - 1].StateAfter, true, boundary);
    }

    internal JmapMethodError? ValidateRequest(JmapChangesRequest request)
    {
        if (!Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (request.SinceState == null || !Values.IsValidUnicode(request.SinceState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid sinceState is required.");
        }

        if (request.IsMaxChangesNull && !request.HasMaxChanges)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The maxChanges presence flags are inconsistent.");
        }

        if (request.HasMaxChanges && !request.IsMaxChangesNull &&
            (request.MaxChanges <= 0L || !Values.IsUnsignedInt(request.MaxChanges)))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "maxChanges must be null or a positive UnsignedInt.");
        }

        return null;
    }

    internal void ValidateChangesData(StandardChangesData data)
    {
        if (data.CurrentState == null || !Values.IsValidUnicode(data.CurrentState))
        {
            throw ProtocolFailure("A valid current changes state is required.", "newState", default(JsonElement));
        }

        if (data.Changes == null)
        {
            throw ProtocolFailure("A change event collection is required.", "changes", default(JsonElement));
        }

        int index = 0;
        foreach (StandardChange change in data.Changes)
        {
            if (change == null)
            {
                throw ProtocolFailure("A change event is required.", "changes/" + index, default(JsonElement));
            }

            if (!Values.IsId(change.Id))
            {
                throw ProtocolFailure("A change event contains an invalid id.", "changes/" + index + "/id", default(JsonElement));
            }

            if (!IsChangeKind(change.Kind))
            {
                throw ProtocolFailure("A change event contains an invalid kind.", "changes/" + index + "/kind", default(JsonElement));
            }

            if (change.StateAfter != null && !Values.IsValidUnicode(change.StateAfter))
            {
                throw ProtocolFailure("A change event contains an invalid state checkpoint.", "changes/" + index + "/stateAfter", default(JsonElement));
            }

            index = index + 1;
        }
    }

    internal long EffectiveMaximum(JmapChangesRequest request)
    {
        if (request.HasMaxChanges && !request.IsMaxChangesNull)
        {
            return request.MaxChanges;
        }

        return DefaultMaximumChanges;
    }

    internal Dictionary<string, StandardChangeStatus> AccumulateStatuses(List<StandardChange> changes, int count)
    {
        Dictionary<string, StandardChangeStatus> statuses = new Dictionary<string, StandardChangeStatus>(StringComparer.Ordinal);
        int index = 0;
        while (index < count)
        {
            StandardChange change = changes[index];
            if (change.Id != null)
            {
                StandardChangeStatus status;
                if (statuses.ContainsKey(change.Id))
                {
                    status = statuses[change.Id];
                }
                else
                {
                    status = new StandardChangeStatus();
                    statuses.Add(change.Id, status);
                }

                ApplyChange(status, change.Kind);
            }

            index = index + 1;
        }

        return statuses;
    }

    internal void ApplyChange(StandardChangeStatus status, StandardChangeKind kind)
    {
        if (kind == StandardChangeKind.Created)
        {
            status.WasCreated = true;
        }
        else if (kind == StandardChangeKind.Updated)
        {
            status.WasUpdated = true;
        }
        else if (kind == StandardChangeKind.Destroyed)
        {
            status.WasDestroyed = true;
        }
    }

    internal StandardChangeKind ClassifyStatus(StandardChangeStatus status)
    {
        if (status.WasCreated && status.WasDestroyed)
        {
            return StandardChangeKind.None;
        }

        if (status.WasCreated)
        {
            return StandardChangeKind.Created;
        }

        if (status.WasDestroyed)
        {
            return StandardChangeKind.Destroyed;
        }

        if (status.WasUpdated)
        {
            return StandardChangeKind.Updated;
        }

        return StandardChangeKind.None;
    }

    internal long CountClassified(Dictionary<string, StandardChangeStatus> statuses)
    {
        long count = 0L;
        foreach (KeyValuePair<string, StandardChangeStatus> item in statuses)
        {
            if (ClassifyStatus(item.Value) != StandardChangeKind.None)
            {
                count = count + 1L;
            }
        }

        return count;
    }

    internal int FindIntermediateBoundary(List<StandardChange> changes, long maximumChanges)
    {
        Dictionary<string, StandardChangeStatus> statuses = new Dictionary<string, StandardChangeStatus>(StringComparer.Ordinal);
        long classifiedCount = 0L;
        int boundary = 0;
        int index = 0;
        while (index < changes.Count)
        {
            StandardChange change = changes[index];
            if (change.Id != null)
            {
                StandardChangeStatus status;
                if (statuses.ContainsKey(change.Id))
                {
                    status = statuses[change.Id];
                }
                else
                {
                    status = new StandardChangeStatus();
                    statuses.Add(change.Id, status);
                }

                StandardChangeKind before = ClassifyStatus(status);
                ApplyChange(status, change.Kind);
                StandardChangeKind after = ClassifyStatus(status);
                if (before == StandardChangeKind.None && after != StandardChangeKind.None)
                {
                    classifiedCount = classifiedCount + 1L;
                }
                else if (before != StandardChangeKind.None && after == StandardChangeKind.None)
                {
                    classifiedCount = classifiedCount - 1L;
                }
            }

            if (classifiedCount <= maximumChanges && change.StateAfter != null)
            {
                boundary = index + 1;
            }

            index = index + 1;
        }

        return boundary;
    }

    internal StandardChangesResult SuccessResult(
        JmapChangesRequest request,
        StandardChangesData data,
        Dictionary<string, StandardChangeStatus> statuses,
        string? newState,
        bool hasMoreChanges,
        int eventCount)
    {
        JmapChangesResponse response = new JmapChangesResponse();
        response.AccountId = request.AccountId;
        response.OldState = request.SinceState;
        response.NewState = newState;
        response.HasMoreChanges = hasMoreChanges;
        AddClassifiedIds(data.Changes, eventCount, statuses, response);

        StandardChangesResult result = new StandardChangesResult();
        result.IsSuccess = true;
        result.Response = response;
        return result;
    }

    internal void AddClassifiedIds(
        List<StandardChange> changes,
        int eventCount,
        Dictionary<string, StandardChangeStatus> statuses,
        JmapChangesResponse response)
    {
        HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < eventCount)
        {
            StandardChange change = changes[index];
            if (change.Id != null && added.Add(change.Id))
            {
                StandardChangeKind kind = ClassifyStatus(statuses[change.Id]);
                if (kind == StandardChangeKind.Created)
                {
                    response.Created.Add(change.Id);
                }
                else if (kind == StandardChangeKind.Updated)
                {
                    response.Updated.Add(change.Id);
                }
                else if (kind == StandardChangeKind.Destroyed)
                {
                    response.Destroyed.Add(change.Id);
                }
            }

            index = index + 1;
        }
    }

    internal bool IsChangeKind(StandardChangeKind kind)
    {
        return kind == StandardChangeKind.Created || kind == StandardChangeKind.Updated || kind == StandardChangeKind.Destroyed;
    }

    internal StandardChangesResult CannotCalculateResult(string description)
    {
        return ErrorResult(MethodError(JmapErrorCodes.CannotCalculateChanges, JmapMethodErrorKind.CannotCalculateChanges, description));
    }

    internal StandardChangesResult ErrorResult(JmapMethodError error)
    {
        StandardChangesResult result = new StandardChangesResult();
        result.Error = error;
        return result;
    }

    internal JmapMethodError MethodError(string type, JmapMethodErrorKind kind, string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        return error;
    }

    internal JmapProtocolException ProtocolFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapProtocolException(message, path, context);
    }
}
