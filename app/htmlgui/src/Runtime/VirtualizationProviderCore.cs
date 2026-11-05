using System;

namespace htmlgui;

internal class VirtualRangeProviderOperation
{
    internal bool Success;
    internal bool Applied;
    internal bool Cancelled;
    internal bool Stale;
    internal bool IsLoading;
    internal bool IsPartial;
    internal bool IsEnd;
    internal bool HasError;
    internal bool CanRetry;
    internal int CancelledGeneration;
    internal string Code = string.Empty;
    internal UiRangeRequest? Request;
    internal UiRangeResult? Result;
    internal UiCollectionItem[] Rows = Array.Empty<UiCollectionItem>();
}

internal class VirtualizationProviderCore
{
    internal const string LoadingRowPrefix = "$loading:";
    internal const string ErrorRowPrefix = "$error:";
    internal const string RetryRowPrefix = "$retry:";

    internal int NextGeneration;
    internal UiRangeRequest? PendingRequest;
    internal UiRangeRequest? FailedRequest;
    internal UiRangeResult? CurrentResult;

    internal VirtualRangeProviderOperation BeginRangeProviderRequest(int start, int count, UiSort[]? sorts, UiFilter[]? filters, UiGroup[]? groups, string? parentKey)
    {
        ValidateRangeProviderRequest(start, count, sorts, filters, groups);
        if (NextGeneration == int.MaxValue)
        {
            return CreateRangeProviderFailure("virtualization.generation-exhausted", false);
        }

        int cancelledGeneration = 0;
        if (PendingRequest != null)
        {
            cancelledGeneration = PendingRequest.Generation;
        }

        NextGeneration++;
        UiRangeRequest request = new UiRangeRequest
        {
            Generation = NextGeneration,
            Start = start,
            Count = count,
            Sorts = CopyRangeProviderSorts(sorts),
            Filters = CopyRangeProviderFilters(filters),
            Groups = CopyRangeProviderGroups(groups),
            ParentKey = string.IsNullOrWhiteSpace(parentKey) ? null : parentKey
        };
        PendingRequest = CopyRangeProviderRequest(request);
        FailedRequest = null;
        VirtualRangeProviderOperation result = CreateRangeProviderSuccess();
        result.Applied = true;
        result.IsLoading = true;
        result.Request = CopyRangeProviderRequest(request);
        result.Rows = new UiCollectionItem[] { CreateRangeProviderStatusRow(LoadingRowPrefix, request.Generation, "loading") };
        if (cancelledGeneration > 0)
        {
            result.Cancelled = true;
            result.CancelledGeneration = cancelledGeneration;
        }

        return result;
    }

    internal VirtualRangeProviderOperation CancelRangeProviderRequest(int generation)
    {
        if (generation < 0)
        {
            return CreateRangeProviderFailure("virtualization.invalid-generation", false);
        }

        if (PendingRequest == null || PendingRequest.Generation != generation)
        {
            return CreateRangeProviderFailure("virtualization.stale-cancellation", true);
        }

        PendingRequest = null;
        VirtualRangeProviderOperation result = CreateRangeProviderSuccess();
        result.Applied = true;
        result.Cancelled = true;
        result.CancelledGeneration = generation;
        return result;
    }

    internal VirtualRangeProviderOperation AcceptRangeProviderResult(UiRangeResult? rangeResult)
    {
        if (rangeResult == null)
        {
            throw new ArgumentNullException("rangeResult");
        }

        if (rangeResult.Generation < 0)
        {
            return CreateRangeProviderFailure("virtualization.invalid-generation", false);
        }

        if (PendingRequest == null || PendingRequest.Generation != rangeResult.Generation)
        {
            return CreateRangeProviderFailure("virtualization.stale-result", true);
        }

        ValidateRangeProviderResult(rangeResult, PendingRequest);
        UiRangeRequest completedRequest = CopyRangeProviderRequest(PendingRequest);
        UiRangeResult accepted = CopyRangeProviderResult(rangeResult);
        bool endReached = accepted.IsEnd;
        if (accepted.HasTotalCount && (long)accepted.Start + accepted.Items.Length >= accepted.TotalCount)
        {
            endReached = true;
        }

        accepted.IsEnd = endReached;
        PendingRequest = null;
        CurrentResult = CopyRangeProviderResult(accepted);
        VirtualRangeProviderOperation result = CreateRangeProviderSuccess();
        result.Applied = true;
        result.Request = completedRequest;
        result.Result = CopyRangeProviderResult(accepted);
        result.IsEnd = endReached;
        if (accepted.Diagnostic != null)
        {
            FailedRequest = CopyRangeProviderRequest(completedRequest);
            result.Code = "virtualization.provider-error";
            result.HasError = true;
            result.CanRetry = true;
            result.Rows = new UiCollectionItem[]
            {
                CreateRangeProviderStatusRow(ErrorRowPrefix, accepted.Generation, accepted.Diagnostic.Message),
                CreateRangeProviderStatusRow(RetryRowPrefix, accepted.Generation, "retry")
            };
            return result;
        }

        FailedRequest = null;
        result.IsPartial = accepted.Items.Length < completedRequest.Count && !endReached;
        result.IsLoading = result.IsPartial;
        result.Rows = CreateRangeProviderRows(accepted.Items, result.IsPartial, accepted.Generation);
        return result;
    }

    internal VirtualRangeProviderOperation RetryRangeProviderRequest()
    {
        if (FailedRequest == null)
        {
            return CreateRangeProviderFailure("virtualization.nothing-to-retry", false);
        }

        if (NextGeneration == int.MaxValue)
        {
            return CreateRangeProviderFailure("virtualization.generation-exhausted", false);
        }

        UiRangeRequest failed = CopyRangeProviderRequest(FailedRequest);
        NextGeneration++;
        failed.Generation = NextGeneration;
        PendingRequest = CopyRangeProviderRequest(failed);
        FailedRequest = null;
        VirtualRangeProviderOperation result = CreateRangeProviderSuccess();
        result.Applied = true;
        result.IsLoading = true;
        result.Request = CopyRangeProviderRequest(failed);
        result.Rows = new UiCollectionItem[] { CreateRangeProviderStatusRow(LoadingRowPrefix, failed.Generation, "loading") };
        return result;
    }

    internal UiRangeRequest? ReadPendingRangeProviderRequest()
    {
        return PendingRequest == null ? null : CopyRangeProviderRequest(PendingRequest);
    }

    internal UiRangeResult? ReadCurrentRangeProviderResult()
    {
        return CurrentResult == null ? null : CopyRangeProviderResult(CurrentResult);
    }

    internal void ValidateRangeProviderRequest(int start, int count, UiSort[]? sorts, UiFilter[]? filters, UiGroup[]? groups)
    {
        if (start < 0 || count < 1)
        {
            throw new ArgumentOutOfRangeException("range", "Range provider requests require a nonnegative start and positive count.");
        }

        UiSort[] sortValues = sorts ?? Array.Empty<UiSort>();
        int sortIndex = 0;
        while (sortIndex < sortValues.Length)
        {
            UiSort? sort = sortValues[sortIndex];
            if (sort == null || string.IsNullOrWhiteSpace(sort.Key) || sort.Priority < 0 || (sort.Direction != UiSortDirection.Ascending && sort.Direction != UiSortDirection.Descending))
            {
                throw new ArgumentException("Range provider sort context is invalid.", "sorts");
            }

            sortIndex++;
        }

        UiFilter[] filterValues = filters ?? Array.Empty<UiFilter>();
        int filterIndex = 0;
        while (filterIndex < filterValues.Length)
        {
            UiFilter? filter = filterValues[filterIndex];
            if (filter == null || string.IsNullOrWhiteSpace(filter.Key) || string.IsNullOrWhiteSpace(filter.Operator) || filter.Value == null)
            {
                throw new ArgumentException("Range provider filter context is invalid.", "filters");
            }

            filterIndex++;
        }

        UiGroup[] groupValues = groups ?? Array.Empty<UiGroup>();
        int groupIndex = 0;
        while (groupIndex < groupValues.Length)
        {
            UiGroup? group = groupValues[groupIndex];
            if (group == null || string.IsNullOrWhiteSpace(group.Key) || group.ItemKeys == null)
            {
                throw new ArgumentException("Range provider group context is invalid.", "groups");
            }

            groupIndex++;
        }
    }

    internal void ValidateRangeProviderResult(UiRangeResult rangeResult, UiRangeRequest request)
    {
        if (rangeResult.Start != request.Start)
        {
            throw new ArgumentException("The range provider result start does not match its request.", "rangeResult");
        }

        if (rangeResult.Items == null || rangeResult.Items.Length > request.Count)
        {
            throw new ArgumentException("The range provider result item count is invalid.", "rangeResult");
        }

        int itemIndex = 0;
        while (itemIndex < rangeResult.Items.Length)
        {
            UiCollectionItem? item = rangeResult.Items[itemIndex];
            if (item == null || string.IsNullOrWhiteSpace(item.Key))
            {
                throw new ArgumentException("Range provider results require keyed non-null items.", "rangeResult");
            }

            int earlierIndex = 0;
            while (earlierIndex < itemIndex)
            {
                if (string.Equals(rangeResult.Items[earlierIndex].Key, item.Key, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Range provider result item keys must be unique.", "rangeResult");
                }

                earlierIndex++;
            }

            itemIndex++;
        }

        if (rangeResult.HasTotalCount && (rangeResult.TotalCount < 0 || (long)rangeResult.Start + rangeResult.Items.Length > rangeResult.TotalCount))
        {
            throw new ArgumentException("The range provider result total count is invalid.", "rangeResult");
        }
    }

    internal UiRangeRequest CopyRangeProviderRequest(UiRangeRequest source)
    {
        return new UiRangeRequest
        {
            Generation = source.Generation,
            Start = source.Start,
            Count = source.Count,
            Sorts = CopyRangeProviderSorts(source.Sorts),
            Filters = CopyRangeProviderFilters(source.Filters),
            Groups = CopyRangeProviderGroups(source.Groups),
            ParentKey = source.ParentKey
        };
    }

    internal UiRangeResult CopyRangeProviderResult(UiRangeResult source)
    {
        return new UiRangeResult
        {
            Generation = source.Generation,
            Start = source.Start,
            Items = CopyRangeProviderItems(source.Items),
            HasTotalCount = source.HasTotalCount,
            TotalCount = source.TotalCount,
            IsEnd = source.IsEnd,
            Diagnostic = source.Diagnostic == null ? null : CopyRangeProviderDiagnostic(source.Diagnostic)
        };
    }

    internal UiSort[] CopyRangeProviderSorts(UiSort[]? source)
    {
        UiSort[] values = source ?? Array.Empty<UiSort>();
        object? copied = new ModelNormalizer().CopyValue(values);
        return copied == null ? Array.Empty<UiSort>() : (UiSort[])copied;
    }

    internal UiFilter[] CopyRangeProviderFilters(UiFilter[]? source)
    {
        UiFilter[] values = source ?? Array.Empty<UiFilter>();
        object? copied = new ModelNormalizer().CopyValue(values);
        return copied == null ? Array.Empty<UiFilter>() : (UiFilter[])copied;
    }

    internal UiGroup[] CopyRangeProviderGroups(UiGroup[]? source)
    {
        UiGroup[] values = source ?? Array.Empty<UiGroup>();
        object? copied = new ModelNormalizer().CopyValue(values);
        return copied == null ? Array.Empty<UiGroup>() : (UiGroup[])copied;
    }

    internal UiCollectionItem[] CopyRangeProviderItems(UiCollectionItem[]? source)
    {
        UiCollectionItem[] values = source ?? Array.Empty<UiCollectionItem>();
        object? copied = new ModelNormalizer().CopyValue(values);
        return copied == null ? Array.Empty<UiCollectionItem>() : (UiCollectionItem[])copied;
    }

    internal UiDiagnostic CopyRangeProviderDiagnostic(UiDiagnostic source)
    {
        return new UiDiagnostic
        {
            Severity = source.Severity,
            Code = source.Code,
            Message = source.Message,
            Location = source.Location
        };
    }

    internal UiCollectionItem[] CreateRangeProviderRows(UiCollectionItem[] items, bool includeLoading, int generation)
    {
        UiCollectionItem[] copied = CopyRangeProviderItems(items);
        int additional = includeLoading ? 1 : 0;
        UiCollectionItem[] result = new UiCollectionItem[copied.Length + additional];
        Array.Copy(copied, result, copied.Length);
        if (includeLoading)
        {
            result[copied.Length] = CreateRangeProviderStatusRow(LoadingRowPrefix, generation, "loading");
        }

        return result;
    }

    internal UiCollectionItem CreateRangeProviderStatusRow(string prefix, int generation, string? text)
    {
        return new UiCollectionItem
        {
            Key = prefix + InvariantFormatter.FormatInt32(generation),
            Value = new UiValue { Kind = UiValueKind.String, StringValue = text ?? string.Empty },
            IsDisabled = true,
            IsPlaceholder = true
        };
    }

    internal VirtualRangeProviderOperation CreateRangeProviderSuccess()
    {
        return new VirtualRangeProviderOperation { Success = true };
    }

    internal VirtualRangeProviderOperation CreateRangeProviderFailure(string code, bool stale)
    {
        return new VirtualRangeProviderOperation
        {
            Code = code,
            Stale = stale
        };
    }
}
