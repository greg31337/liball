using System;
using htmlgui;

namespace htmlgui.Tests;

public class CollectionTableTreeTests
{
    [Test]
    public void CollectionTableAndTreeModelsHaveSafeDefaults()
    {
        UiCollection collection = new UiCollection();
        UiCollectionItem item = new UiCollectionItem();
        UiSelection selection = new UiSelection();
        UiSort sort = new UiSort();
        UiFilter filter = new UiFilter();
        UiGroup group = new UiGroup();
        UiPageRange pageRange = new UiPageRange();
        UiTable table = new UiTable();
        UiTableColumn column = new UiTableColumn();
        UiTableRow row = new UiTableRow();
        UiTableCell cell = new UiTableCell();
        UiTableSummary summary = new UiTableSummary();
        UiEditState edit = new UiEditState();
        UiTree tree = new UiTree();
        UiTreeItem treeItem = new UiTreeItem();
        UiTreeRelationship relationship = new UiTreeRelationship();
        UiTreeRange treeRange = new UiTreeRange();

        Assert.Multiple(() =>
        {
            Assert.That(collection.Items, Is.Empty);
            Assert.That(collection.Selection, Is.Not.Null);
            Assert.That(collection.Sorts, Is.Empty);
            Assert.That(collection.Filters, Is.Empty);
            Assert.That(collection.Groups, Is.Empty);
            Assert.That(collection.PageRange, Is.Not.Null);
            Assert.That(collection.EmptyContent, Is.Null);
            Assert.That(collection.HasError, Is.False);
            Assert.That(item.Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(item.Content, Is.Null);
            Assert.That(selection.Mode, Is.EqualTo(UiSelectionMode.None));
            Assert.That(selection.SelectedKeys, Is.Empty);
            Assert.That(sort.Direction, Is.EqualTo(UiSortDirection.None));
            Assert.That(filter.Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(group.ItemKeys, Is.Empty);
            Assert.That(group.IsExpanded, Is.True);
            Assert.That(pageRange.HasTotalCount, Is.False);
            Assert.That(table.Columns, Is.Empty);
            Assert.That(table.Rows, Is.Empty);
            Assert.That(table.Summaries, Is.Empty);
            Assert.That(table.Filters, Is.Empty);
            Assert.That(table.Groups, Is.Empty);
            Assert.That(table.Selection, Is.Not.Null);
            Assert.That(table.EditState, Is.Null);
            Assert.That(column.Width, Is.Null);
            Assert.That(row.Cells, Is.Empty);
            Assert.That(row.DetailContent, Is.Null);
            Assert.That(cell.Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(summary.Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(edit.OriginalValue, Is.Not.SameAs(edit.CurrentValue));
            Assert.That(edit.IsValid, Is.True);
            Assert.That(tree.Items, Is.Empty);
            Assert.That(tree.Selection, Is.Not.Null);
            Assert.That(tree.Range, Is.Not.Null);
            Assert.That(tree.TypeAhead, Is.Not.Null);
            Assert.That(treeItem.Children, Is.Empty);
            Assert.That(treeItem.Relationship, Is.Not.Null);
            Assert.That(relationship.ParentKey, Is.Null);
            Assert.That(relationship.HasSetSize, Is.False);
            Assert.That(treeRange.HasTotalCount, Is.False);
            Assert.That(treeRange.AnchorKey, Is.Null);
        });
    }

    [Test]
    public void PopulatedCollectionRetainsSelectionQueryGroupingAndPaging()
    {
        UiNode content = new UiNode { Kind = UiNodeKind.Text, Text = "Item" };
        UiCollectionItem item = new UiCollectionItem { Key = "item-1", Value = TextValue("one"), Content = content, GroupKey = "group-a", IsDisabled = true, IsPlaceholder = true };
        UiSelection selection = new UiSelection { Mode = UiSelectionMode.Range, SelectedKeys = new string[] { "item-1" }, ActiveKey = "item-1", AnchorKey = "item-0" };
        UiSort sort = new UiSort { Key = "name", Direction = UiSortDirection.Descending, Priority = 1 };
        UiFilter filter = new UiFilter { Key = "name", Operator = "contains", Value = TextValue("o"), IgnoreCase = true };
        UiGroup group = new UiGroup { Key = "group-a", Label = "A", ItemKeys = new string[] { "item-1" }, IsExpanded = false };
        UiPageRange range = new UiPageRange { Start = 0, Count = 1, HasTotalCount = true, TotalCount = 10, IsEnd = false };
        UiCollection collection = new UiCollection
        {
            Key = "items",
            Items = new UiCollectionItem[] { item },
            Selection = selection,
            Sorts = new UiSort[] { sort },
            Filters = new UiFilter[] { filter },
            Groups = new UiGroup[] { group },
            PageRange = range,
            EmptyContent = content,
            LoadingContent = content,
            ErrorContent = content,
            CanReorder = true,
            IsLoading = true,
            HasError = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(collection.Items[0], Is.SameAs(item));
            Assert.That(item.Content, Is.SameAs(content));
            Assert.That(selection.Mode, Is.EqualTo(UiSelectionMode.Range));
            Assert.That(sort.Direction, Is.EqualTo(UiSortDirection.Descending));
            Assert.That(filter.IgnoreCase, Is.True);
            Assert.That(group.IsExpanded, Is.False);
            Assert.That(range.HasTotalCount, Is.True);
            Assert.That(range.TotalCount, Is.EqualTo(10));
            Assert.That(collection.CanReorder, Is.True);
            Assert.That(collection.IsLoading, Is.True);
            Assert.That(collection.HasError, Is.True);
        });
    }

    [Test]
    public void PopulatedTableRetainsColumnsRowsSummariesAndEditState()
    {
        UiLength width = new UiLength { HasValue = true, Value = 20, Unit = UiLengthUnit.Rem };
        UiTableColumn column = new UiTableColumn { Key = "name", Header = "Name", FieldKey = "name", Width = width, SortDirection = UiSortDirection.Ascending, IsSortable = true, IsResizable = true, IsReorderable = true, IsHidden = true, IsFrozen = true };
        UiTableCell cell = new UiTableCell { ColumnKey = "name", Value = TextValue("Ada"), Content = new UiNode { Text = "Ada" }, IsHeader = true, IsEditable = true, HasColumnSpan = true, ColumnSpan = 0, HasRowSpan = true, RowSpan = 2 };
        UiTableRow row = new UiTableRow { Key = "row-1", Cells = new UiTableCell[] { cell }, DetailContent = new UiNode { Text = "Details" }, IsSelected = true, IsExpanded = true, IsDisabled = true };
        UiTableSummary summary = new UiTableSummary { ColumnKey = "name", Label = "Count", Value = IntegerValue(1) };
        UiEditState edit = new UiEditState { RowKey = "row-1", ColumnKey = "name", OriginalValue = TextValue("Ada"), CurrentValue = TextValue("Grace"), IsDirty = true, IsPending = true, IsValid = false };
        UiFilter filter = new UiFilter { Key = "name", Operator = "starts-with", Value = TextValue("A") };
        UiGroup group = new UiGroup { Key = "people-a", Label = "A", ItemKeys = new string[] { "row-1" } };
        UiTable table = new UiTable { Key = "people", Caption = "People", Columns = new UiTableColumn[] { column }, Rows = new UiTableRow[] { row }, Summaries = new UiTableSummary[] { summary }, Filters = new UiFilter[] { filter }, Groups = new UiGroup[] { group }, Selection = new UiSelection { Mode = UiSelectionMode.Single }, EditState = edit, IsInteractive = true, HasRowDetails = true };

        Assert.Multiple(() =>
        {
            Assert.That(table.Columns[0], Is.SameAs(column));
            Assert.That(column.Width, Is.SameAs(width));
            Assert.That(column.IsFrozen, Is.True);
            Assert.That(table.Rows[0].Cells[0], Is.SameAs(cell));
            Assert.That(cell.HasColumnSpan, Is.True);
            Assert.That(cell.ColumnSpan, Is.Zero);
            Assert.That(row.IsExpanded, Is.True);
            Assert.That(table.Summaries[0], Is.SameAs(summary));
            Assert.That(table.Filters[0], Is.SameAs(filter));
            Assert.That(table.Groups[0], Is.SameAs(group));
            Assert.That(table.EditState, Is.SameAs(edit));
            Assert.That(edit.IsValid, Is.False);
            Assert.That(table.IsInteractive, Is.True);
        });
    }

    [Test]
    public void PopulatedTreeRetainsHierarchyRelationshipAndRangeMetadata()
    {
        UiTreeRelationship relationship = new UiTreeRelationship { ParentKey = "root", Depth = 2, PositionInSet = 1, HasSetSize = true, SetSize = 0 };
        UiTreeItem child = new UiTreeItem { Key = "child", Label = "Child", Value = TextValue("child"), Content = new UiNode { Text = "Child" }, Relationship = relationship, IsExpanded = true, HasLazyChildren = true, IsSelected = true, IsDisabled = true, HasCheckState = true, CheckState = UiCommandCheckState.Mixed };
        UiTreeItem root = new UiTreeItem { Key = "root", Label = "Root", Children = new UiTreeItem[] { child } };
        UiTreeRange range = new UiTreeRange { Start = 0, Count = 2, HasTotalCount = true, TotalCount = 2, AnchorKey = "child" };
        UiTypeAheadState typeAhead = new UiTypeAheadState { Buffer = "ch", MatchedKey = "child", Generation = 3 };
        UiTree tree = new UiTree { Key = "tree", Items = new UiTreeItem[] { root }, Selection = new UiSelection { Mode = UiSelectionMode.Multiple, SelectedKeys = new string[] { "child" } }, Range = range, TypeAhead = typeAhead, IsTreeGrid = true, CanReorder = true };

        Assert.Multiple(() =>
        {
            Assert.That(tree.Items[0].Children[0], Is.SameAs(child));
            Assert.That(child.Relationship, Is.SameAs(relationship));
            Assert.That(relationship.Depth, Is.EqualTo(2));
            Assert.That(relationship.HasSetSize, Is.True);
            Assert.That(relationship.SetSize, Is.Zero);
            Assert.That(child.HasLazyChildren, Is.True);
            Assert.That(child.CheckState, Is.EqualTo(UiCommandCheckState.Mixed));
            Assert.That(tree.Range, Is.SameAs(range));
            Assert.That(tree.TypeAhead, Is.SameAs(typeAhead));
            Assert.That(tree.IsTreeGrid, Is.True);
            Assert.That(tree.CanReorder, Is.True);
        });
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue IntegerValue(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }
}
