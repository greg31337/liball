using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class VirtualizationIntegrationTests
{
    [Test]
    public async Task NewQueryWinsWhenTheOldProviderResultArrivesOutOfOrder()
    {
        OutOfOrderRangeProvider provider = new OutOfOrderRangeProvider();
        IUiVirtualizationEngine engine = new UiVirtualizationEngine();
        engine.RegisterUiRangeProvider("query", provider);
        UiSort[] oldSorts = new UiSort[] { new UiSort { Key = "name", Direction = UiSortDirection.Ascending } };
        UiFilter[] oldFilters = new UiFilter[] { Filter("status", "old") };
        UiSort[] newSorts = new UiSort[] { new UiSort { Key = "rank", Direction = UiSortDirection.Descending } };
        UiFilter[] newFilters = new UiFilter[] { Filter("status", "new") };

        Task<UiRangeResult> oldTask = engine.RequestUiRange("query", 0, 1, oldSorts, oldFilters, null, null);
        UiRangeRequest oldRequest = await provider.FirstStarted.Task;
        Task<UiRangeResult> newTask = engine.RequestUiRange("query", 0, 1, newSorts, newFilters, null, null);
        UiRangeRequest newRequest = await provider.SecondStarted.Task;
        provider.SecondCompletion.SetResult(Result(newRequest, true, 1, true, "new-item"));
        UiRangeResult accepted = await newTask;
        provider.FirstCompletion.SetResult(Result(oldRequest, true, 1, true, "old-item"));
        UiRangeResult stale = await oldTask;
        UiRangeResult stored = engine.ReadUiRangeResult()!;

        Assert.Multiple(() =>
        {
            Assert.That(oldRequest.Generation, Is.EqualTo(1));
            Assert.That(oldRequest.Sorts.Single().Key, Is.EqualTo("name"));
            Assert.That(oldRequest.Filters.Single().Value.StringValue, Is.EqualTo("old"));
            Assert.That(newRequest.Generation, Is.EqualTo(2));
            Assert.That(newRequest.Sorts.Single().Key, Is.EqualTo("rank"));
            Assert.That(newRequest.Filters.Single().Value.StringValue, Is.EqualTo("new"));
            Assert.That(accepted.Items.Single().Key, Is.EqualTo("new-item"));
            Assert.That(stale.Diagnostic, Is.Not.Null);
            Assert.That(stale.Diagnostic!.Code, Is.EqualTo("virtualization.stale-result"));
            Assert.That(stored.Generation, Is.EqualTo(2));
            Assert.That(stored.Items.Single().Key, Is.EqualTo("new-item"));
        });
    }

    [Test]
    public async Task ExplicitCancellationRejectsAProviderResultThatCompletesLater()
    {
        ControlledRangeProvider provider = new ControlledRangeProvider();
        IUiVirtualizationEngine engine = new UiVirtualizationEngine();
        engine.RegisterUiRangeProvider("cancel", provider);

        Task<UiRangeResult> pending = engine.RequestUiRange("cancel", 10, 2, null, null, null, null);
        UiRangeRequest request = await provider.Started.Task;
        UiResult cancellation = engine.CancelUiRangeRequest(request.Generation);
        provider.Completion.SetResult(Result(request, false, 0, false, "late"));
        UiRangeResult late = await pending;

        Assert.Multiple(() =>
        {
            Assert.That(cancellation.Success, Is.True);
            Assert.That(late.Diagnostic, Is.Not.Null);
            Assert.That(late.Diagnostic!.Code, Is.EqualTo("virtualization.stale-result"));
            Assert.That(engine.ReadUiRangeResult(), Is.Null);
        });
    }

    [Test]
    public async Task UnknownProviderTotalsFlowIntoSelectedListAccessibilityMetadata()
    {
        UnknownTotalRangeProvider provider = new UnknownTotalRangeProvider();
        IUiVirtualizationEngine engine = new UiVirtualizationEngine();
        engine.RegisterUiRangeProvider("unknown", provider);

        UiRangeResult partial = await engine.RequestUiRange("unknown", 50, 4, null, null, null, null);
        UiRangeResult payload = engine.ReadUiRangeResult()!;
        VirtualListMaterialization materialized = new VirtualizationMaterializer().MaterializeListRange(payload.Items, payload.Start, payload.HasTotalCount, payload.TotalCount, payload.IsEnd);
        UiNode node = CollectionNode(materialized, "item-51");
        DomNode rendered = new SemanticExpander().ExpandNode(node);
        UiRangeResult end = await engine.RequestUiRange("unknown", 52, 4, null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(partial.Items.Select(item => item.Key), Is.EqualTo(new string[] { "item-50", "item-51", "$loading:1" }));
            Assert.That(partial.Items.Last().IsPlaceholder, Is.True);
            Assert.That(payload.HasTotalCount, Is.False);
            Assert.That(payload.IsEnd, Is.False);
            Assert.That(materialized.Positions.Select(position => position.PositionInSet), Is.EqualTo(new int[] { 51, 52 }));
            Assert.That(materialized.Positions.Select(position => position.HasSetSize), Is.All.False);
            Assert.That(AttributeValue(rendered.Children[0], AriaAttributes.PositionInSet), Is.EqualTo("51"));
            Assert.That(AttributeValue(rendered.Children[1], AriaAttributes.PositionInSet), Is.EqualTo("52"));
            Assert.That(HasAttribute(rendered.Children[0], AriaAttributes.SetSize), Is.False);
            Assert.That(AttributeValue(rendered.Children[1], AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(end.IsEnd, Is.True);
            Assert.That(end.HasTotalCount, Is.False);
            Assert.That(end.Items.Select(item => item.Key), Is.EqualTo(new string[] { "item-52" }));
        });
    }

    [Test]
    public void TreeExpansionAndGridWindowsPreserveSelectionAndLogicalIndices()
    {
        UiTreeItem grandchild = TreeItem("grandchild", false);
        UiTreeItem child = TreeItem("child", true);
        child.IsExpanded = true;
        child.Children = new UiTreeItem[] { grandchild };
        UiTreeItem root = TreeItem("root", false);
        root.Children = new UiTreeItem[] { child };
        VirtualizationMaterializer materializer = new VirtualizationMaterializer();

        VirtualTreeMaterialization collapsed = materializer.MaterializeTreeRange(new UiTreeItem[] { root }, 0, 10, false, 0, null);
        root.IsExpanded = true;
        VirtualTreeMaterialization expanded = materializer.MaterializeTreeRange(new UiTreeItem[] { root }, 0, 10, true, 3, "child");
        UiTableRow selectedRow = new UiTableRow { Key = "row-99", IsSelected = true };
        UiTableColumn column = new UiTableColumn { Key = "column-4", Header = "Column" };
        VirtualGridMaterialization grid = materializer.MaterializeGridRange(new UiTableRow[] { selectedRow }, 99, true, 100, new UiTableColumn[] { column }, 4, true, 10);

        Assert.Multiple(() =>
        {
            Assert.That(collapsed.Items.Select(item => item.Key), Is.EqualTo(new string[] { "root" }));
            Assert.That(expanded.Items.Select(item => item.Key), Is.EqualTo(new string[] { "root", "child", "grandchild" }));
            Assert.That(expanded.Items[1].IsSelected, Is.True);
            Assert.That(expanded.Positions.Select(position => position.Depth), Is.EqualTo(new int[] { 1, 2, 3 }));
            Assert.That(expanded.Positions.Select(position => position.RowIndex), Is.EqualTo(new int[] { 1, 2, 3 }));
            Assert.That(expanded.Positions[1].ParentKey, Is.EqualTo("root"));
            Assert.That(expanded.Positions[2].ParentKey, Is.EqualTo("child"));
            Assert.That(grid.Rows.Single().IsSelected, Is.True);
            Assert.That(grid.RowCount, Is.EqualTo(101));
            Assert.That(grid.RowPositions.Single().RowIndex, Is.EqualTo(101));
            Assert.That(grid.RowPositions.Single().PositionInSet, Is.EqualTo(100));
            Assert.That(grid.ColumnCount, Is.EqualTo(10));
            Assert.That(grid.ColumnPositions.Single().ColumnIndex, Is.EqualTo(5));
            Assert.That(grid.ColumnPositions.Single().PositionInSet, Is.EqualTo(5));
        });
    }

    [Test]
    public async Task ProviderErrorRetryKeepsQueryTreeAndGroupContext()
    {
        ErrorThenSuccessRangeProvider provider = new ErrorThenSuccessRangeProvider();
        IUiVirtualizationEngine engine = new UiVirtualizationEngine();
        engine.RegisterUiRangeProvider("retry", provider);
        UiSort[] sorts = new UiSort[] { new UiSort { Key = "rank", Direction = UiSortDirection.Descending } };
        UiFilter[] filters = new UiFilter[] { Filter("visible", "yes") };
        UiGroup[] groups = new UiGroup[] { new UiGroup { Key = "section", ItemKeys = new string[] { "expected" } } };

        UiRangeResult failed = await engine.RequestUiRange("retry", 20, 2, sorts, filters, groups, "tree-parent");
        sorts[0].Key = "mutated";
        filters[0].Value.StringValue = "mutated";
        groups[0].ItemKeys[0] = "mutated";
        UiRangeResult recovered = await engine.RetryUiRange("retry");
        UiRangeRequest retry = provider.Requests[1];

        Assert.Multiple(() =>
        {
            Assert.That(failed.Diagnostic!.Code, Is.EqualTo("provider.temporary"));
            Assert.That(failed.Items.Select(item => item.Key), Is.EqualTo(new string[] { "$error:1", "$retry:1" }));
            Assert.That(recovered.Diagnostic, Is.Null);
            Assert.That(recovered.Items.Single().Key, Is.EqualTo("recovered"));
            Assert.That(retry.Generation, Is.EqualTo(2));
            Assert.That(retry.Start, Is.EqualTo(20));
            Assert.That(retry.Count, Is.EqualTo(2));
            Assert.That(retry.Sorts.Single().Key, Is.EqualTo("rank"));
            Assert.That(retry.Filters.Single().Value.StringValue, Is.EqualTo("yes"));
            Assert.That(retry.Groups.Single().ItemKeys, Is.EqualTo(new string[] { "expected" }));
            Assert.That(retry.ParentKey, Is.EqualTo("tree-parent"));
        });
    }

    private static UiFilter Filter(string key, string value)
    {
        return new UiFilter { Key = key, Operator = "equals", Value = Text(value) };
    }

    private static UiRangeResult Result(UiRangeRequest request, bool hasTotalCount, int totalCount, bool isEnd, params string[] keys)
    {
        return new UiRangeResult
        {
            Generation = request.Generation,
            Start = request.Start,
            Items = keys.Select(key => Item(key)).ToArray(),
            HasTotalCount = hasTotalCount,
            TotalCount = totalCount,
            IsEnd = isEnd
        };
    }

    private static UiCollectionItem Item(string key)
    {
        return new UiCollectionItem { Key = key, Value = Text(key) };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiTreeItem TreeItem(string key, bool selected)
    {
        return new UiTreeItem { Key = key, Label = key, Value = Text(key), IsSelected = selected };
    }

    private static UiNode CollectionNode(VirtualListMaterialization materialization, string selectedKey)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "interactive-list",
            Identity = new UiNodeIdentity
            {
                AuthorKey = "virtual-list",
                SiblingIdentity = "virtual-list",
                StableId = "virtual-list-id"
            },
            Collection = new UiCollection
            {
                Key = "virtual-items",
                Items = materialization.Items,
                PageRange = materialization.Range,
                Selection = new UiSelection
                {
                    Mode = UiSelectionMode.Multiple,
                    SelectedKeys = new string[] { selectedKey },
                    ActiveKey = selectedKey
                }
            }
        };
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        return node.Attributes.Any(attribute => attribute.Name == name);
    }

    private static string AttributeValue(DomNode node, string name)
    {
        return node.Attributes.Single(attribute => attribute.Name == name).Value;
    }

    private sealed class OutOfOrderRangeProvider : IUiRangeProvider
    {
        public int Calls;
        public TaskCompletionSource<UiRangeRequest> FirstStarted = new TaskCompletionSource<UiRangeRequest>();
        public TaskCompletionSource<UiRangeRequest> SecondStarted = new TaskCompletionSource<UiRangeRequest>();
        public TaskCompletionSource<UiRangeResult> FirstCompletion = new TaskCompletionSource<UiRangeResult>();
        public TaskCompletionSource<UiRangeResult> SecondCompletion = new TaskCompletionSource<UiRangeResult>();

        public Task<UiRangeResult> ProvideUiRange(UiRangeRequest request)
        {
            Calls++;
            if (Calls == 1)
            {
                FirstStarted.SetResult(request);
                return FirstCompletion.Task;
            }

            SecondStarted.SetResult(request);
            return SecondCompletion.Task;
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

    private sealed class UnknownTotalRangeProvider : IUiRangeProvider
    {
        public Task<UiRangeResult> ProvideUiRange(UiRangeRequest request)
        {
            if (request.Start == 50)
            {
                return Task.FromResult(Result(request, false, 0, false, "item-50", "item-51"));
            }

            return Task.FromResult(Result(request, false, 0, true, "item-52"));
        }
    }

    private sealed class ErrorThenSuccessRangeProvider : IUiRangeProvider
    {
        public List<UiRangeRequest> Requests = new List<UiRangeRequest>();

        public Task<UiRangeResult> ProvideUiRange(UiRangeRequest request)
        {
            Requests.Add(request);
            if (Requests.Count == 1)
            {
                UiRangeResult failure = Result(request, false, 0, false);
                failure.Diagnostic = new UiDiagnostic
                {
                    Severity = UiDiagnosticSeverity.Error,
                    Code = "provider.temporary",
                    Message = "Try again",
                    Location = "/provider"
                };
                return Task.FromResult(failure);
            }

            return Task.FromResult(Result(request, false, 0, true, "recovered"));
        }
    }
}
