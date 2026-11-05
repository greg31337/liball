using System;
using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class StandardQueryData
{
    internal string? QueryState;
    internal bool CanCalculateChanges;
    internal List<string> OrderedIds = new List<string>();
}

internal class StandardQueryResult
{
    internal bool IsSuccess;
    internal JmapQueryResponse? Response;
    internal JmapMethodError? Error;
}

internal class StandardQueryProcessor
{
    internal long MaximumResults;
    internal int MaximumFilterDepth;
    internal HashSet<string> SupportedFilterProperties;
    internal HashSet<string> SupportedSortProperties;
    internal HashSet<string> StringSortProperties;
    internal HashSet<string> SupportedCollations;
    internal HashSet<string> SupportedComparatorProperties;
    internal ValueValidator Values = new ValueValidator();

    internal StandardQueryProcessor(
        long maximumResults,
        int maximumFilterDepth,
        List<string>? supportedFilterProperties,
        List<string>? supportedSortProperties,
        List<string>? stringSortProperties,
        List<string>? supportedCollations,
        List<string>? supportedComparatorProperties)
    {
        if (maximumResults <= 0L || !Values.IsUnsignedInt(maximumResults))
        {
            throw new ArgumentOutOfRangeException("maximumResults");
        }

        if (maximumFilterDepth <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumFilterDepth");
        }

        MaximumResults = maximumResults;
        MaximumFilterDepth = maximumFilterDepth;
        SupportedFilterProperties = NameSet(supportedFilterProperties, "supportedFilterProperties");
        SupportedSortProperties = NameSet(supportedSortProperties, "supportedSortProperties");
        StringSortProperties = NameSet(stringSortProperties, "stringSortProperties");
        SupportedCollations = NameSet(supportedCollations, "supportedCollations");
        SupportedComparatorProperties = NameSet(supportedComparatorProperties, "supportedComparatorProperties");

        foreach (string property in StringSortProperties)
        {
            if (!SupportedSortProperties.Contains(property))
            {
                throw new ArgumentException("Every string sort property must also be a supported sort property.", "stringSortProperties");
            }
        }
    }

    internal StandardQueryResult ProcessQuery(JmapQueryRequest? request, StandardQueryData? data)
    {
        if (request == null)
        {
            return ErrorResult(MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A query request is required."));
        }

        JmapMethodError? requestError = ValidateQueryRequest(request);
        if (requestError != null)
        {
            return ErrorResult(requestError);
        }

        if (data == null)
        {
            throw ProtocolFailure("Query data is required.", "", default(JsonElement));
        }

        ValidateQueryData(data);
        long position = ResolvePosition(request, data.OrderedIds);
        if (position < 0L)
        {
            return ErrorResult(MethodError(JmapErrorCodes.AnchorNotFound, JmapMethodErrorKind.AnchorNotFound, "The query anchor was not found in the result set."));
        }

        long limit = EffectiveLimit(request);
        JmapQueryResponse response = new JmapQueryResponse();
        response.AccountId = request.AccountId;
        response.QueryState = data.QueryState;
        response.CanCalculateChanges = data.CanCalculateChanges;
        response.Position = position;
        AddWindow(data.OrderedIds, position, limit, response.Ids);

        if (request.HasCalculateTotal && request.CalculateTotal)
        {
            response.Total = data.OrderedIds.Count;
            response.HasTotal = true;
        }

        if (!request.HasLimit || request.IsLimitNull || request.Limit > MaximumResults)
        {
            response.Limit = limit;
            response.HasLimit = true;
        }

        StandardQueryResult result = new StandardQueryResult();
        result.IsSuccess = true;
        result.Response = response;
        return result;
    }

    internal JmapMethodError? ValidateQueryRequest(JmapQueryRequest request)
    {
        if (!Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (request.HasPosition && !Values.IsInt(request.Position))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "position must be an Int.");
        }

        if (request.Anchor != null && !Values.IsId(request.Anchor))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "anchor must be an Id.");
        }

        if (request.HasAnchorOffset && !Values.IsInt(request.AnchorOffset))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "anchorOffset must be an Int.");
        }

        if (request.IsLimitNull && !request.HasLimit)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The limit presence flags are inconsistent.");
        }

        if (request.HasLimit && !request.IsLimitNull && !Values.IsUnsignedInt(request.Limit))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "limit must be null or an UnsignedInt.");
        }

        JmapMethodError? filterError = ValidateFilterTree(request.Filter, 1);
        if (filterError != null)
        {
            return filterError;
        }

        return ValidateSortList(request.Sort);
    }

    internal JmapMethodError? ValidateFilterTree(JmapFilterNode? filter, int depth)
    {
        FilterTraversalBudget budget = new FilterTraversalBudget();
        return ValidateFilterTreeNode(filter, depth, budget);
    }

    internal JmapMethodError? ValidateFilterTreeNode(
        JmapFilterNode? filter,
        int depth,
        FilterTraversalBudget budget)
    {
        if (filter == null)
        {
            return null;
        }

        budget.Nodes = budget.Nodes + 1;
        if (budget.Nodes > SecurityLimits.MaximumFilterNodes)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter,
                JmapMethodErrorKind.UnsupportedFilter,
                "The filter contains too many conditions.");
        }

        if (depth > MaximumFilterDepth)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter, "The filter exceeds the supported recursion depth.");
        }

        if (filter.HasOperator)
        {
            if (!IsFilterOperator(filter.Operator) || filter.Conditions == null || filter.FilterProperties == null || filter.FilterProperties.Count != 0)
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The filter operator is malformed.");
            }

            foreach (JmapFilterNode condition in filter.Conditions)
            {
                if (condition == null)
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A filter operator contains an invalid condition.");
                }

                JmapMethodError? childError = ValidateFilterTreeNode(
                    condition, depth + 1, budget);
                if (childError != null)
                {
                    return childError;
                }
            }

            return null;
        }

        if (filter.Conditions != null)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A filter condition cannot contain nested conditions.");
        }

        if (filter.FilterProperties == null)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A filter condition is invalid.");
        }

        foreach (KeyValuePair<string, JsonElement> property in filter.FilterProperties)
        {
            if (!SupportedFilterProperties.Contains(property.Key))
            {
                return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter, "The filter contains an unsupported property.");
            }

            if (property.Value.ValueKind == JsonValueKind.Undefined)
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The filter contains an undefined property value.");
            }
        }

        return null;
    }

    internal JmapMethodError? ValidateSortList(List<JmapComparator>? sort)
    {
        if (sort == null)
        {
            return null;
        }

        foreach (JmapComparator comparator in sort)
        {
            if (comparator == null || comparator.Property == null || comparator.Property.Length == 0 || !Values.IsValidUnicode(comparator.Property))
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A sort comparator is invalid.");
            }

            if (!SupportedSortProperties.Contains(comparator.Property))
            {
                return MethodError(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort, "The sort property is not supported.");
            }

            if (comparator.UnknownProperties == null)
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A sort comparator is invalid.");
            }

            foreach (KeyValuePair<string, JsonElement> property in comparator.UnknownProperties)
            {
                if (property.Value.ValueKind == JsonValueKind.Undefined)
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The comparator contains an undefined property value.");
                }

                if (!SupportedComparatorProperties.Contains(property.Key))
                {
                    return MethodError(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort, "The comparator contains an unsupported property.");
                }
            }

            if (comparator.Collation != null && StringSortProperties.Contains(comparator.Property))
            {
                if (comparator.Collation.Length == 0 || !Values.IsValidUnicode(comparator.Collation) || !SupportedCollations.Contains(comparator.Collation))
                {
                    return MethodError(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort, "The collation algorithm is not supported.");
                }
            }
        }

        return null;
    }

    internal void ValidateQueryData(StandardQueryData data)
    {
        if (data.QueryState == null || !Values.IsValidUnicode(data.QueryState))
        {
            throw ProtocolFailure("A valid query state is required.", "queryState", default(JsonElement));
        }

        if (data.OrderedIds == null)
        {
            throw ProtocolFailure("An ordered query result is required.", "ids", default(JsonElement));
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (string id in data.OrderedIds)
        {
            if (!Values.IsId(id))
            {
                throw ProtocolFailure("The query result contains an invalid id.", "ids/" + index, default(JsonElement));
            }

            if (!ids.Add(id))
            {
                throw ProtocolFailure("The query result contains a duplicate id.", "ids/" + index, default(JsonElement));
            }

            index = index + 1;
        }
    }

    internal long ResolvePosition(JmapQueryRequest request, List<string> ids)
    {
        if (request.Anchor != null)
        {
            int anchorIndex = -1;
            int index = 0;
            while (index < ids.Count)
            {
                if (ids[index] == request.Anchor)
                {
                    anchorIndex = index;
                    break;
                }

                index = index + 1;
            }

            if (anchorIndex < 0)
            {
                return -1L;
            }

            long anchorOffset = request.HasAnchorOffset ? request.AnchorOffset : 0L;
            long anchoredPosition = anchorIndex + anchorOffset;
            return anchoredPosition < 0L ? 0L : anchoredPosition;
        }

        long requestedPosition = request.HasPosition ? request.Position : 0L;
        if (requestedPosition < 0L)
        {
            long relativePosition = ids.Count + requestedPosition;
            return relativePosition < 0L ? 0L : relativePosition;
        }

        return requestedPosition;
    }

    internal long EffectiveLimit(JmapQueryRequest request)
    {
        if (request.HasLimit && !request.IsLimitNull && request.Limit <= MaximumResults)
        {
            return request.Limit;
        }

        return MaximumResults;
    }

    internal void AddWindow(List<string> ids, long position, long limit, List<string> output)
    {
        if (position >= ids.Count || limit == 0L)
        {
            return;
        }

        long available = ids.Count - position;
        long count = limit < available ? limit : available;
        int index = (int)position;
        long added = 0L;
        while (added < count)
        {
            output.Add(ids[index]);
            index = index + 1;
            added = added + 1L;
        }
    }

    internal bool IsFilterOperator(JmapFilterOperator value)
    {
        return value == JmapFilterOperator.And || value == JmapFilterOperator.Or || value == JmapFilterOperator.Not;
    }

    internal HashSet<string> NameSet(List<string>? names, string parameterName)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        if (names == null)
        {
            return result;
        }

        foreach (string name in names)
        {
            if (name == null || name.Length == 0 || !Values.IsValidUnicode(name))
            {
                throw new ArgumentException("Supported names must be non-empty valid Unicode strings.", parameterName);
            }

            result.Add(name);
        }

        return result;
    }

    internal StandardQueryResult ErrorResult(JmapMethodError error)
    {
        StandardQueryResult result = new StandardQueryResult();
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
