using System;
using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class StandardQueryChangesData
{
    internal bool CanCalculateChanges = true;
    internal string? CurrentQueryState;
    internal List<string> OldIds = new List<string>();
    internal List<string> NewIds = new List<string>();
    internal List<string> ReinsertIds = new List<string>();
}

internal class StandardQueryDelta
{
    internal List<string> Removed = new List<string>();
    internal List<JmapAddedItem> Added = new List<JmapAddedItem>();
}

internal class StandardQueryChangesResult
{
    internal bool IsSuccess;
    internal JmapQueryChangesResponse? Response;
    internal JmapMethodError? Error;
}

internal class StandardQueryChangesProcessor
{
    internal StandardQueryProcessor QueryProcessor;
    internal ValueValidator Values = new ValueValidator();

    internal StandardQueryChangesProcessor(StandardQueryProcessor queryProcessor)
    {
        if (queryProcessor == null)
        {
            throw new ArgumentNullException("queryProcessor");
        }

        QueryProcessor = queryProcessor;
    }

    internal StandardQueryChangesResult ProcessQueryChanges(JmapQueryChangesRequest? request, StandardQueryChangesData? data)
    {
        if (request == null)
        {
            return ErrorResult(MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A queryChanges request is required."));
        }

        JmapMethodError? requestError = ValidateQueryChangesRequest(request);
        if (requestError != null)
        {
            return ErrorResult(requestError);
        }

        if (data == null)
        {
            throw ProtocolFailure("Query changes data is required.", "", default(JsonElement));
        }

        if (!data.CanCalculateChanges)
        {
            return CannotCalculateResult("Changes cannot be calculated from the requested query state.");
        }

        ValidateQueryChangesData(data);
        StandardQueryDelta delta = CalculateDelta(data);
        ApplyUpToBoundary(request.UpToId, data, delta);

        long changeCount = (long)delta.Removed.Count + delta.Added.Count;
        if (request.HasMaxChanges && !request.IsMaxChangesNull && changeCount > request.MaxChanges)
        {
            return ErrorResult(MethodError(JmapErrorCodes.TooManyChanges, JmapMethodErrorKind.TooManyChanges, "The query changes exceed maxChanges."));
        }

        JmapQueryChangesResponse response = new JmapQueryChangesResponse();
        response.AccountId = request.AccountId;
        response.OldQueryState = request.SinceQueryState;
        response.NewQueryState = data.CurrentQueryState;
        response.Removed = delta.Removed;
        response.Added = delta.Added;
        if (request.HasCalculateTotal && request.CalculateTotal)
        {
            response.Total = data.NewIds.Count;
            response.HasTotal = true;
        }

        StandardQueryChangesResult result = new StandardQueryChangesResult();
        result.IsSuccess = true;
        result.Response = response;
        return result;
    }

    internal JmapMethodError? ValidateQueryChangesRequest(JmapQueryChangesRequest request)
    {
        if (!Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (request.SinceQueryState == null || !Values.IsValidUnicode(request.SinceQueryState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid sinceQueryState is required.");
        }

        if (request.IsMaxChangesNull && !request.HasMaxChanges)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The maxChanges presence flags are inconsistent.");
        }

        if (request.HasMaxChanges && !request.IsMaxChangesNull && !Values.IsUnsignedInt(request.MaxChanges))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "maxChanges must be null or an UnsignedInt.");
        }

        if (request.UpToId != null && !Values.IsId(request.UpToId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "upToId must be an Id.");
        }

        JmapMethodError? filterError = QueryProcessor.ValidateFilterTree(request.Filter, 1);
        if (filterError != null)
        {
            return filterError;
        }

        return QueryProcessor.ValidateSortList(request.Sort);
    }

    internal void ValidateQueryChangesData(StandardQueryChangesData data)
    {
        if (data.CurrentQueryState == null || !Values.IsValidUnicode(data.CurrentQueryState))
        {
            throw ProtocolFailure("A valid current query state is required.", "newQueryState", default(JsonElement));
        }

        Dictionary<string, int> oldIndexes = ValidateIdSequence(data.OldIds, "oldIds");
        Dictionary<string, int> newIndexes = ValidateIdSequence(data.NewIds, "newIds");
        if (data.ReinsertIds == null)
        {
            throw ProtocolFailure("A reinsert id collection is required.", "reinsertIds", default(JsonElement));
        }

        HashSet<string> reinserted = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (string id in data.ReinsertIds)
        {
            if (!Values.IsId(id))
            {
                throw ProtocolFailure("A reinsert entry contains an invalid id.", "reinsertIds/" + index, default(JsonElement));
            }

            if (!reinserted.Add(id))
            {
                throw ProtocolFailure("A reinsert id is duplicated.", "reinsertIds/" + index, default(JsonElement));
            }

            if (!oldIndexes.ContainsKey(id) || !newIndexes.ContainsKey(id))
            {
                throw ProtocolFailure("A reinsert id must occur in both query states.", "reinsertIds/" + index, default(JsonElement));
            }

            index = index + 1;
        }
    }

    internal Dictionary<string, int> ValidateIdSequence(List<string>? ids, string path)
    {
        if (ids == null)
        {
            throw ProtocolFailure("An ordered query id collection is required.", path, default(JsonElement));
        }

        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        int index = 0;
        foreach (string id in ids)
        {
            if (!Values.IsId(id))
            {
                throw ProtocolFailure("A query state contains an invalid id.", path + "/" + index, default(JsonElement));
            }

            if (indexes.ContainsKey(id))
            {
                throw ProtocolFailure("A query state contains a duplicate id.", path + "/" + index, default(JsonElement));
            }

            indexes.Add(id, index);
            index = index + 1;
        }

        return indexes;
    }

    internal StandardQueryDelta CalculateDelta(StandardQueryChangesData data)
    {
        Dictionary<string, int> oldIndexes = IndexIds(data.OldIds);
        HashSet<string> forced = new HashSet<string>(data.ReinsertIds, StringComparer.Ordinal);
        List<string> candidates = new List<string>();
        List<int> oldPositions = new List<int>();
        foreach (string id in data.NewIds)
        {
            if (oldIndexes.ContainsKey(id) && !forced.Contains(id))
            {
                candidates.Add(id);
                oldPositions.Add(oldIndexes[id]);
            }
        }

        HashSet<string> kept = LongestStableSubsequence(candidates, oldPositions);
        StandardQueryDelta delta = new StandardQueryDelta();
        foreach (string id in data.OldIds)
        {
            if (!kept.Contains(id))
            {
                delta.Removed.Add(id);
            }
        }

        int newIndex = 0;
        foreach (string id in data.NewIds)
        {
            if (!kept.Contains(id))
            {
                JmapAddedItem item = new JmapAddedItem();
                item.Id = id;
                item.Index = newIndex;
                delta.Added.Add(item);
            }

            newIndex = newIndex + 1;
        }

        return delta;
    }

    internal HashSet<string> LongestStableSubsequence(List<string> ids, List<int> oldPositions)
    {
        HashSet<string> kept = new HashSet<string>(StringComparer.Ordinal);
        if (ids.Count == 0)
        {
            return kept;
        }

        int[] tails = new int[ids.Count];
        int[] previous = new int[ids.Count];
        int length = 0;
        int candidateIndex = 0;
        while (candidateIndex < ids.Count)
        {
            int low = 0;
            int high = length;
            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (oldPositions[tails[middle]] < oldPositions[candidateIndex])
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            previous[candidateIndex] = low == 0 ? -1 : tails[low - 1];
            tails[low] = candidateIndex;
            if (low == length)
            {
                length = length + 1;
            }

            candidateIndex = candidateIndex + 1;
        }

        int current = tails[length - 1];
        while (current >= 0)
        {
            kept.Add(ids[current]);
            current = previous[current];
        }

        return kept;
    }

    internal void ApplyUpToBoundary(string? upToId, StandardQueryChangesData data, StandardQueryDelta delta)
    {
        if (upToId == null)
        {
            return;
        }

        Dictionary<string, int> oldIndexes = IndexIds(data.OldIds);
        if (oldIndexes.ContainsKey(upToId))
        {
            int oldBoundary = oldIndexes[upToId];
            List<string> boundedRemoved = new List<string>();
            foreach (string id in delta.Removed)
            {
                if (oldIndexes[id] <= oldBoundary)
                {
                    boundedRemoved.Add(id);
                }
            }

            delta.Removed = boundedRemoved;
        }

        Dictionary<string, int> newIndexes = IndexIds(data.NewIds);
        if (newIndexes.ContainsKey(upToId))
        {
            int newBoundary = newIndexes[upToId];
            List<JmapAddedItem> boundedAdded = new List<JmapAddedItem>();
            foreach (JmapAddedItem item in delta.Added)
            {
                if (item.Index <= newBoundary)
                {
                    boundedAdded.Add(item);
                }
            }

            delta.Added = boundedAdded;
        }
    }

    internal Dictionary<string, int> IndexIds(List<string> ids)
    {
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        int index = 0;
        foreach (string id in ids)
        {
            indexes.Add(id, index);
            index = index + 1;
        }

        return indexes;
    }

    internal StandardQueryChangesResult CannotCalculateResult(string description)
    {
        return ErrorResult(MethodError(JmapErrorCodes.CannotCalculateChanges, JmapMethodErrorKind.CannotCalculateChanges, description));
    }

    internal StandardQueryChangesResult ErrorResult(JmapMethodError error)
    {
        StandardQueryChangesResult result = new StandardQueryChangesResult();
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
