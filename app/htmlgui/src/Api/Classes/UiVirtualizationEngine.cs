using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Provides public virtualization calculations and asynchronous range-provider coordination.</summary>
public class UiVirtualizationEngine : IUiVirtualizationEngine
{
    internal VirtualizationCore Core;
    internal VirtualizationProviderCore ProviderCore;
    internal List<string> ProviderIds;
    internal List<IUiRangeProvider> Providers;

    /// <summary>Initializes an empty virtualization engine.</summary>
    public UiVirtualizationEngine()
    {
        Core = new VirtualizationCore();
        ProviderCore = new VirtualizationProviderCore();
        ProviderIds = new List<string>();
        Providers = new List<IUiRangeProvider>();
    }

    /// <summary>Calculates a fixed- or variable-extent viewport range.</summary>
    public UiVirtualization CalculateUiViewport(UiVirtualization? virtualization, double scrollX, double scrollY, double viewportWidth, double viewportHeight)
    {
        if (virtualization == null)
        {
            throw new ArgumentNullException("virtualization");
        }

        UiVirtualization result = CopyPublicVirtualization(virtualization);
        if (virtualization.HasItemExtent)
        {
            FixedVirtualRange range = Core.CalculateFixedViewportRange(virtualization, scrollX, scrollY, viewportWidth, viewportHeight);
            result.Request = CopyPublicRangeRequest(range.Request);
        }
        else
        {
            VariableVirtualRange range = Core.CalculateVariableViewportRange(virtualization, scrollX, scrollY, viewportWidth, viewportHeight);
            result.Request = CopyPublicRangeRequest(range.Request);
            result.Measurements = Core.ReadVariableMeasurements();
        }
        return result;
    }

    /// <summary>Applies one variable item measurement.</summary>
    public UiVirtualization ApplyUiMeasurement(UiVirtualization? virtualization, UiMeasuredItem? measurement, int anchorIndex)
    {
        if (virtualization == null)
        {
            throw new ArgumentNullException("virtualization");
        }

        Core.ApplyVariableMeasurement(virtualization, measurement, anchorIndex);
        UiVirtualization result = CopyPublicVirtualization(virtualization);
        result.Measurements = Core.ReadVariableMeasurements();
        return result;
    }

    /// <summary>Calculates a fixed- or variable-extent scroll target.</summary>
    public UiScrollTarget CalculateUiScrollTarget(UiVirtualization? virtualization, UiScrollTarget? target, double scrollX, double scrollY, double viewportWidth, double viewportHeight)
    {
        if (virtualization == null)
        {
            throw new ArgumentNullException("virtualization");
        }

        if (virtualization.HasItemExtent)
        {
            return Core.CalculateFixedScrollTarget(virtualization, target, scrollX, scrollY, viewportWidth, viewportHeight);
        }

        return Core.CalculateVariableScrollTarget(virtualization, target, scrollX, scrollY, viewportWidth, viewportHeight);
    }

    /// <summary>Registers one stable asynchronous range provider.</summary>
    public UiResult RegisterUiRangeProvider(string? providerId, IUiRangeProvider? provider)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return CreatePublicProviderResult(false, "virtualization.invalid-provider-id", "/virtualization/providerId");
        }

        if (provider == null)
        {
            return CreatePublicProviderResult(false, "virtualization.null-provider", "/virtualization/provider");
        }

        if (FindPublicProviderIndex(providerId) >= 0)
        {
            return CreatePublicProviderResult(false, "virtualization.duplicate-provider", "/virtualization/providerId");
        }

        ProviderIds.Add(providerId);
        Providers.Add(provider);
        return new UiResult { Success = true };
    }

    /// <summary>Unregisters one asynchronous range provider.</summary>
    public UiResult UnregisterUiRangeProvider(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return CreatePublicProviderResult(false, "virtualization.invalid-provider-id", "/virtualization/providerId");
        }

        int index = FindPublicProviderIndex(providerId);
        if (index < 0)
        {
            return CreatePublicProviderResult(false, "virtualization.unknown-provider", "/virtualization/providerId");
        }

        ProviderIds.RemoveAt(index);
        Providers.RemoveAt(index);
        return new UiResult { Success = true };
    }

    /// <summary>Generates and dispatches one range-provider request.</summary>
    public Task<UiRangeResult> RequestUiRange(string? providerId, int start, int count, UiSort[]? sorts, UiFilter[]? filters, UiGroup[]? groups, string? parentKey)
    {
        VirtualRangeProviderOperation operation = ProviderCore.BeginRangeProviderRequest(start, count, sorts, filters, groups, parentKey);
        if (!operation.Success || operation.Request == null)
        {
            return Task.FromResult(CreatePublicRangeOperationResult(operation));
        }

        int providerIndex = FindPublicProviderIndex(providerId);
        if (providerIndex < 0)
        {
            return Task.FromResult(FailPublicRangeRequest(operation.Request, "virtualization.unknown-provider"));
        }

        return DispatchPublicRangeProvider(Providers[providerIndex], operation.Request);
    }

    /// <summary>Retries the most recent failed range request.</summary>
    public Task<UiRangeResult> RetryUiRange(string? providerId)
    {
        VirtualRangeProviderOperation operation = ProviderCore.RetryRangeProviderRequest();
        if (!operation.Success || operation.Request == null)
        {
            return Task.FromResult(CreatePublicRangeOperationResult(operation));
        }

        int providerIndex = FindPublicProviderIndex(providerId);
        if (providerIndex < 0)
        {
            return Task.FromResult(FailPublicRangeRequest(operation.Request, "virtualization.unknown-provider"));
        }

        return DispatchPublicRangeProvider(Providers[providerIndex], operation.Request);
    }

    /// <summary>Cancels the active range generation.</summary>
    public UiResult CancelUiRangeRequest(int generation)
    {
        VirtualRangeProviderOperation operation = ProviderCore.CancelRangeProviderRequest(generation);
        if (operation.Success)
        {
            return new UiResult { Success = true };
        }

        return CreatePublicProviderResult(false, operation.Code, "/virtualization/generation");
    }

    /// <summary>Reads a detached copy of the most recently accepted provider payload.</summary>
    public UiRangeResult? ReadUiRangeResult()
    {
        return ProviderCore.ReadCurrentRangeProviderResult();
    }

    internal Task<UiRangeResult> DispatchPublicRangeProvider(IUiRangeProvider provider, UiRangeRequest request)
    {
        UiRangeRequest copied = CopyPublicRangeRequest(request);
        Task<UiRangeResult> callback = Task.Run(() => provider.ProvideUiRange(copied));
        return callback.ContinueWith(task => CompletePublicRangeProvider(request, task));
    }

    internal UiRangeResult CompletePublicRangeProvider(UiRangeRequest request, Task<UiRangeResult> callback)
    {
        if (!IsPublicRangeRequestPending(request.Generation))
        {
            return CreatePublicRangeFailure(request.Generation, request.Start, "virtualization.stale-result");
        }

        if (callback.IsCanceled)
        {
            return FailPublicRangeRequest(request, "virtualization.provider-canceled");
        }

        if (callback.IsFaulted)
        {
            return FailPublicRangeRequest(request, "virtualization.provider-exception");
        }

        UiRangeResult? result = callback.Result;
        if (result == null)
        {
            return FailPublicRangeRequest(request, "virtualization.null-provider-result");
        }

        if (result.Generation != request.Generation)
        {
            return FailPublicRangeRequest(request, "virtualization.invalid-provider-result");
        }

        string validationCode = ValidatePublicRangeProviderResult(result, request);
        if (validationCode.Length > 0)
        {
            return FailPublicRangeRequest(request, validationCode);
        }

        return CreatePublicRangeOperationResult(ProviderCore.AcceptRangeProviderResult(result));
    }

    internal UiRangeResult FailPublicRangeRequest(UiRangeRequest request, string code)
    {
        if (!IsPublicRangeRequestPending(request.Generation))
        {
            return CreatePublicRangeFailure(request.Generation, request.Start, "virtualization.stale-result");
        }

        UiRangeResult failure = CreatePublicRangeFailure(request.Generation, request.Start, code);
        return CreatePublicRangeOperationResult(ProviderCore.AcceptRangeProviderResult(failure));
    }

    internal string ValidatePublicRangeProviderResult(UiRangeResult result, UiRangeRequest request)
    {
        if (result.Start != request.Start || result.Items == null || result.Items.Length > request.Count)
        {
            return "virtualization.invalid-provider-result";
        }

        int index = 0;
        while (index < result.Items.Length)
        {
            UiCollectionItem? item = result.Items[index];
            if (item == null || string.IsNullOrWhiteSpace(item.Key))
            {
                return "virtualization.invalid-provider-result";
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (result.Items[earlierIndex].Key == item.Key)
                {
                    return "virtualization.invalid-provider-result";
                }

                earlierIndex++;
            }

            index++;
        }

        if (result.HasTotalCount && (result.TotalCount < 0 || (long)result.Start + result.Items.Length > result.TotalCount))
        {
            return "virtualization.invalid-provider-result";
        }

        return string.Empty;
    }

    internal UiRangeResult CreatePublicRangeOperationResult(VirtualRangeProviderOperation operation)
    {
        if (operation.Result == null)
        {
            int generation = operation.Request == null ? 0 : operation.Request.Generation;
            int start = operation.Request == null ? 0 : operation.Request.Start;
            return CreatePublicRangeFailure(generation, start, operation.Code);
        }

        UiRangeResult result = ProviderCore.CopyRangeProviderResult(operation.Result);
        result.Items = ProviderCore.CopyRangeProviderItems(operation.Rows);
        result.IsEnd = operation.IsEnd;
        return result;
    }

    internal UiRangeResult CreatePublicRangeFailure(int generation, int start, string code)
    {
        return new UiRangeResult
        {
            Generation = generation,
            Start = start,
            Diagnostic = new UiDiagnostic
            {
                Severity = UiDiagnosticSeverity.Error,
                Code = code.Length == 0 ? "virtualization.operation-failed" : code,
                Message = "The virtualization operation did not complete successfully.",
                Location = "/virtualization/range"
            }
        };
    }

    internal UiResult CreatePublicProviderResult(bool success, string code, string location)
    {
        UiResult result = new UiResult { Success = success };
        if (!success)
        {
            result.Diagnostics = new UiDiagnostic[]
            {
                new UiDiagnostic
                {
                    Severity = UiDiagnosticSeverity.Error,
                    Code = code,
                    Message = "The range-provider operation did not complete successfully.",
                    Location = location
                }
            };
        }

        return result;
    }

    internal bool IsPublicRangeRequestPending(int generation)
    {
        UiRangeRequest? pending = ProviderCore.ReadPendingRangeProviderRequest();
        return pending != null && pending.Generation == generation;
    }

    internal int FindPublicProviderIndex(string? providerId)
    {
        int index = 0;
        while (index < ProviderIds.Count)
        {
            if (ValueComparer.TextEquals(ProviderIds[index], providerId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal UiVirtualization CopyPublicVirtualization(UiVirtualization source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        return copied == null ? new UiVirtualization() : (UiVirtualization)copied;
    }

    internal UiRangeRequest CopyPublicRangeRequest(UiRangeRequest source)
    {
        return ProviderCore.CopyRangeProviderRequest(source);
    }
}
