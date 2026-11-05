using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class UiVirtualizationEngineTests
{
    [Test]
    public void CalculatesFixedAndMeasuredVariableOperationsThroughTheContract()
    {
        IUiVirtualizationEngine engine = new UiVirtualizationEngine();
        UiVirtualization fixedOptions = FixedOptions(100, 20, 1);
        UiVirtualization fixedViewport = engine.CalculateUiViewport(fixedOptions, 0, 45, 100, 50);
        UiScrollTarget fixedTarget = engine.CalculateUiScrollTarget(fixedOptions, Target(5, "center"), 0, 0, 100, 20);
        UiVirtualization variableOptions = VariableOptions(false, 0, 10, 1);
        UiMeasuredItem measurement = new UiMeasuredItem { Key = "one", Index = 1, Extent = 30, Generation = 1 };
        UiVirtualization measured = engine.ApplyUiMeasurement(variableOptions, measurement, 2);
        measurement.Extent = 999;
        UiVirtualization variableViewport = engine.CalculateUiViewport(variableOptions, 0, 15, 100, 25);
        UiScrollTarget variableTarget = engine.CalculateUiScrollTarget(variableOptions, Target(2, "start"), 0, 15, 100, 25);

        Assert.Multiple(() =>
        {
            Assert.That(fixedViewport, Is.Not.SameAs(fixedOptions));
            Assert.That(fixedViewport.Request, Is.Not.Null);
            Assert.That(fixedViewport.Request!.Start, Is.EqualTo(1));
            Assert.That(fixedViewport.Request.Count, Is.EqualTo(5));
            Assert.That(fixedOptions.Request, Is.Null);
            Assert.That(fixedTarget.Offset, Is.EqualTo(100));
            Assert.That(measured.Measurements.Single().Key, Is.EqualTo("one"));
            Assert.That(measured.Measurements.Single().Extent, Is.EqualTo(30));
            Assert.That(variableViewport.Measurements.Single().Extent, Is.EqualTo(30));
            Assert.That(variableViewport.Request, Is.Not.Null);
            Assert.That(variableViewport.Request!.Start, Is.Zero);
            Assert.That(variableViewport.Request.Count, Is.EqualTo(3));
            Assert.That(variableTarget.Offset, Is.EqualTo(40));
        });
    }

    [Test]
    public async Task DispatchesDetachedProviderRequestsAndStoresDetachedResults()
    {
        RecordingRangeProvider provider = new RecordingRangeProvider();
        IUiVirtualizationEngine engine = new UiVirtualizationEngine();
        UiResult registered = engine.RegisterUiRangeProvider("items", provider);
        UiSort[] sorts = new UiSort[] { new UiSort { Key = "name", Direction = UiSortDirection.Ascending } };
        UiFilter[] filters = new UiFilter[] { new UiFilter { Key = "active", Operator = "equals", Value = Text("yes") } };
        UiGroup[] groups = new UiGroup[] { new UiGroup { Key = "region", ItemKeys = new string[] { "west" } } };

        Task<UiRangeResult> pending = engine.RequestUiRange("items", 10, 2, sorts, filters, groups, "branch");
        sorts[0].Key = "mutated";
        filters[0].Value.StringValue = "mutated";
        groups[0].ItemKeys[0] = "mutated";
        UiRangeResult result = await pending;
        UiRangeRequest request = provider.Requests.Single();
        result.Items[0].Key = "mutated result";
        UiRangeResult stored = engine.ReadUiRangeResult()!;
        stored.Items[0].Key = "mutated stored copy";
        UiRangeResult repeated = engine.ReadUiRangeResult()!;
        UiResult unregistered = engine.UnregisterUiRangeProvider("items");

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(request.Generation, Is.EqualTo(1));
            Assert.That(request.Start, Is.EqualTo(10));
            Assert.That(request.Count, Is.EqualTo(2));
            Assert.That(request.Sorts.Single().Key, Is.EqualTo("name"));
            Assert.That(request.Filters.Single().Value.StringValue, Is.EqualTo("yes"));
            Assert.That(request.Groups.Single().ItemKeys, Is.EqualTo(new string[] { "west" }));
            Assert.That(request.ParentKey, Is.EqualTo("branch"));
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.IsEnd, Is.True);
            Assert.That(repeated.Items.Select(item => item.Key), Is.EqualTo(new string[] { "item-10", "item-11" }));
            Assert.That(unregistered.Success, Is.True);
        });
    }

    [Test]
    public async Task ProviderErrorsCreateStatusRowsAndCanBeRetried()
    {
        RetryRangeProvider provider = new RetryRangeProvider();
        IUiVirtualizationEngine engine = new UiVirtualizationEngine();
        engine.RegisterUiRangeProvider("retry", provider);

        UiRangeResult failed = await engine.RequestUiRange("retry", 4, 1, null, null, null, "parent");
        UiRangeResult retried = await engine.RetryUiRange("retry");
        UiRangeResult nothingToRetry = await engine.RetryUiRange("retry");

        Assert.Multiple(() =>
        {
            Assert.That(failed.Diagnostic, Is.Not.Null);
            Assert.That(failed.Diagnostic!.Code, Is.EqualTo("provider.unavailable"));
            Assert.That(failed.Items.Select(item => item.Key), Is.EqualTo(new string[] { "$error:1", "$retry:1" }));
            Assert.That(failed.Items.Select(item => item.IsPlaceholder), Is.All.True);
            Assert.That(retried.Diagnostic, Is.Null);
            Assert.That(retried.Generation, Is.EqualTo(2));
            Assert.That(retried.Items.Single().Key, Is.EqualTo("recovered"));
            Assert.That(provider.Requests.Select(request => request.Generation), Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(provider.Requests.Select(request => request.Start), Is.All.EqualTo(4));
            Assert.That(provider.Requests.Select(request => request.ParentKey), Is.All.EqualTo("parent"));
            Assert.That(nothingToRetry.Diagnostic!.Code, Is.EqualTo("virtualization.nothing-to-retry"));
        });
    }

    [Test]
    public async Task CancellationMakesLateCallbackResultsStale()
    {
        ControlledRangeProvider provider = new ControlledRangeProvider();
        IUiVirtualizationEngine engine = new UiVirtualizationEngine();
        engine.RegisterUiRangeProvider("controlled", provider);

        Task<UiRangeResult> pending = engine.RequestUiRange("controlled", 0, 1, null, null, null, null);
        UiRangeRequest request = await provider.Started.Task;
        UiResult cancelled = engine.CancelUiRangeRequest(request.Generation);
        provider.Completion.SetResult(Result(request, "late"));
        UiRangeResult stale = await pending;
        UiResult repeatedCancellation = engine.CancelUiRangeRequest(request.Generation);

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Success, Is.True);
            Assert.That(stale.Diagnostic, Is.Not.Null);
            Assert.That(stale.Diagnostic!.Code, Is.EqualTo("virtualization.stale-result"));
            Assert.That(engine.ReadUiRangeResult(), Is.Null);
            Assert.That(repeatedCancellation.Success, Is.False);
            Assert.That(repeatedCancellation.Diagnostics.Single().Code, Is.EqualTo("virtualization.stale-cancellation"));
        });
    }

    [Test]
    public async Task ReportsRegistrationDispatchAndCallbackFailuresWithoutLeakingExceptions()
    {
        IUiVirtualizationEngine engine = new UiVirtualizationEngine();
        ThrowingRangeProvider throwing = new ThrowingRangeProvider();

        UiResult missingId = engine.RegisterUiRangeProvider(null, throwing);
        UiResult missingProvider = engine.RegisterUiRangeProvider("missing", null);
        UiResult registered = engine.RegisterUiRangeProvider("throwing", throwing);
        UiResult duplicate = engine.RegisterUiRangeProvider("throwing", throwing);
        UiRangeResult callbackFailure = await engine.RequestUiRange("throwing", 0, 1, null, null, null, null);
        UiRangeResult malformed = await engine.RetryUiRange("throwing");
        UiResult unknownRemoval = engine.UnregisterUiRangeProvider("unknown");
        UiRangeResult unknownDispatch = await engine.RequestUiRange("unknown", 0, 1, null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(missingId.Diagnostics.Single().Code, Is.EqualTo("virtualization.invalid-provider-id"));
            Assert.That(missingProvider.Diagnostics.Single().Code, Is.EqualTo("virtualization.null-provider"));
            Assert.That(registered.Success, Is.True);
            Assert.That(duplicate.Diagnostics.Single().Code, Is.EqualTo("virtualization.duplicate-provider"));
            Assert.That(callbackFailure.Diagnostic!.Code, Is.EqualTo("virtualization.provider-exception"));
            Assert.That(malformed.Diagnostic!.Code, Is.EqualTo("virtualization.invalid-provider-result"));
            Assert.That(unknownRemoval.Diagnostics.Single().Code, Is.EqualTo("virtualization.unknown-provider"));
            Assert.That(unknownDispatch.Diagnostic!.Code, Is.EqualTo("virtualization.unknown-provider"));
        });
    }

    [Test]
    public void RejectsInvalidCalculationInputs()
    {
        IUiVirtualizationEngine engine = new UiVirtualizationEngine();
        UiVirtualization fixedOptions = FixedOptions(10, 20, 1);
        UiVirtualization variableOptions = VariableOptions(true, 10, 20, 1);

        Assert.Multiple(() =>
        {
            Assert.That(() => engine.CalculateUiViewport(null, 0, 0, 10, 10), Throws.ArgumentNullException);
            Assert.That(() => engine.ApplyUiMeasurement(null, null, 0), Throws.ArgumentNullException);
            Assert.That(() => engine.ApplyUiMeasurement(variableOptions, null, 0), Throws.ArgumentNullException);
            Assert.That(() => engine.CalculateUiScrollTarget(fixedOptions, null, 0, 0, 10, 10), Throws.ArgumentNullException);
            Assert.That(() => engine.CalculateUiScrollTarget(variableOptions, Target(10, "start"), 0, 0, 10, 10), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => engine.RequestUiRange("missing", -1, 1, null, null, null, null), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static UiVirtualization FixedOptions(int totalCount, double itemExtent, int overscan)
    {
        return new UiVirtualization
        {
            IsEnabled = true,
            Orientation = UiOrientation.Vertical,
            HasItemExtent = true,
            ItemExtent = itemExtent,
            Overscan = overscan,
            HasTotalCount = true,
            TotalCount = totalCount
        };
    }

    private static UiVirtualization VariableOptions(bool hasTotalCount, int totalCount, double itemExtent, int overscan)
    {
        return new UiVirtualization
        {
            IsEnabled = true,
            Orientation = UiOrientation.Vertical,
            ItemExtent = itemExtent,
            Overscan = overscan,
            HasTotalCount = hasTotalCount,
            TotalCount = totalCount
        };
    }

    private static UiScrollTarget Target(int index, string alignment)
    {
        return new UiScrollTarget { HasIndex = true, Index = index, Alignment = alignment };
    }

    private static UiRangeResult Result(UiRangeRequest request, params string[] keys)
    {
        return new UiRangeResult
        {
            Generation = request.Generation,
            Start = request.Start,
            Items = keys.Select(key => new UiCollectionItem { Key = key, Value = Text(key) }).ToArray(),
            HasTotalCount = true,
            TotalCount = request.Start + keys.Length,
            IsEnd = true
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private sealed class RecordingRangeProvider : IUiRangeProvider
    {
        public List<UiRangeRequest> Requests = new List<UiRangeRequest>();

        public Task<UiRangeResult> ProvideUiRange(UiRangeRequest request)
        {
            Requests.Add(request);
            return Task.FromResult(Result(request, "item-10", "item-11"));
        }
    }

    private sealed class RetryRangeProvider : IUiRangeProvider
    {
        public List<UiRangeRequest> Requests = new List<UiRangeRequest>();

        public Task<UiRangeResult> ProvideUiRange(UiRangeRequest request)
        {
            Requests.Add(request);
            if (Requests.Count == 1)
            {
                UiRangeResult failure = Result(request);
                failure.Diagnostic = new UiDiagnostic { Code = "provider.unavailable", Message = "Unavailable" };
                return Task.FromResult(failure);
            }

            return Task.FromResult(Result(request, "recovered"));
        }
    }

    private sealed class ControlledRangeProvider : IUiRangeProvider
    {
        public TaskCompletionSource<UiRangeRequest> Started = new TaskCompletionSource<UiRangeRequest>();
        public TaskCompletionSource<UiRangeResult> Completion = new TaskCompletionSource<UiRangeResult>();

        public Task<UiRangeResult> ProvideUiRange(UiRangeRequest request)
        {
            Started.SetResult(request);
            return Completion.Task;
        }
    }

    private sealed class ThrowingRangeProvider : IUiRangeProvider
    {
        public int Calls;

        public Task<UiRangeResult> ProvideUiRange(UiRangeRequest request)
        {
            Calls++;
            if (Calls == 1)
            {
                throw new InvalidOperationException("provider failed");
            }

            UiRangeResult malformed = Result(request, "one", "two");
            malformed.Start++;
            return Task.FromResult(malformed);
        }
    }
}
