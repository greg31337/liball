using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class VirtualizationMaterializerTests
{
    [Test]
    public void ListWindowsKeepLogicalPositionsAndKnownTotals()
    {
        UiCollectionItem[] source = new UiCollectionItem[] { Item("ten"), Item("eleven") };

        VirtualListMaterialization result = new VirtualizationMaterializer().MaterializeListRange(source, 10, true, 100, false);
        source[0].Key = "mutated";
        source[1].Value.StringValue = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.Items.Select(item => item.Key), Is.EqualTo(new string[] { "ten", "eleven" }));
            Assert.That(result.Items[1].Value.StringValue, Is.EqualTo("eleven"));
            Assert.That(result.Range.Start, Is.EqualTo(10));
            Assert.That(result.Range.Count, Is.EqualTo(2));
            Assert.That(result.Range.HasTotalCount, Is.True);
            Assert.That(result.Range.TotalCount, Is.EqualTo(100));
            Assert.That(result.Range.IsEnd, Is.False);
            Assert.That(result.Positions.Select(position => position.LogicalIndex), Is.EqualTo(new int[] { 10, 11 }));
            Assert.That(result.Positions.Select(position => position.PositionInSet), Is.EqualTo(new int[] { 11, 12 }));
            Assert.That(result.Positions.Select(position => position.SetSize), Is.All.EqualTo(100));
            Assert.That(result.Positions.Select(position => position.HasSetSize), Is.All.True);
        });
    }

    [Test]
    public void ListWindowsPreserveUnknownTotalsAndDetectTheKnownEnd()
    {
        VirtualizationMaterializer materializer = new VirtualizationMaterializer();

        VirtualListMaterialization unknown = materializer.MaterializeListRange(new UiCollectionItem[] { Item("later") }, 50, false, 999, false);
        VirtualListMaterialization end = materializer.MaterializeListRange(new UiCollectionItem[] { Item("last") }, 4, true, 5, false);

        Assert.Multiple(() =>
        {
            Assert.That(unknown.Range.HasTotalCount, Is.False);
            Assert.That(unknown.Range.TotalCount, Is.Zero);
            Assert.That(unknown.Positions.Single().PositionInSet, Is.EqualTo(51));
            Assert.That(unknown.Positions.Single().HasSetSize, Is.False);
            Assert.That(unknown.Positions.Single().SetSize, Is.Zero);
            Assert.That(end.Range.IsEnd, Is.True);
        });
    }

    [Test]
    public void GridWindowsIncludeHeaderOffsetAndLogicalColumnPositions()
    {
        UiTableRow[] rows = new UiTableRow[] { Row("row-20", "column-3"), Row("row-21", "column-4") };
        UiTableColumn[] columns = new UiTableColumn[] { Column("column-3"), Column("column-4") };

        VirtualGridMaterialization result = new VirtualizationMaterializer().MaterializeGridRange(rows, 20, true, 100, columns, 3, true, 8);
        rows[0].Key = "mutated";
        columns[0].Header = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.Rows.Select(row => row.Key), Is.EqualTo(new string[] { "row-20", "row-21" }));
            Assert.That(result.Columns.Select(column => column.Header), Is.EqualTo(new string[] { "column-3", "column-4" }));
            Assert.That(result.HasRowCount, Is.True);
            Assert.That(result.RowCount, Is.EqualTo(101));
            Assert.That(result.HasColumnCount, Is.True);
            Assert.That(result.ColumnCount, Is.EqualTo(8));
            Assert.That(result.RowPositions.Select(position => position.LogicalIndex), Is.EqualTo(new int[] { 20, 21 }));
            Assert.That(result.RowPositions.Select(position => position.PositionInSet), Is.EqualTo(new int[] { 21, 22 }));
            Assert.That(result.RowPositions.Select(position => position.RowIndex), Is.EqualTo(new int[] { 22, 23 }));
            Assert.That(result.RowPositions.Select(position => position.SetSize), Is.All.EqualTo(100));
            Assert.That(result.ColumnPositions.Select(position => position.LogicalIndex), Is.EqualTo(new int[] { 3, 4 }));
            Assert.That(result.ColumnPositions.Select(position => position.PositionInSet), Is.EqualTo(new int[] { 4, 5 }));
            Assert.That(result.ColumnPositions.Select(position => position.ColumnIndex), Is.EqualTo(new int[] { 4, 5 }));
            Assert.That(result.ColumnPositions.Select(position => position.SetSize), Is.All.EqualTo(8));
        });
    }

    [Test]
    public void GridWindowsExposeUnknownAccessibleCountsWithoutUsingDomCounts()
    {
        VirtualGridMaterialization result = new VirtualizationMaterializer().MaterializeGridRange(new UiTableRow[] { Row("row", "column") }, 500, false, 0, new UiTableColumn[] { Column("column") }, 30, false, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasRowCount, Is.False);
            Assert.That(result.RowCount, Is.EqualTo(-1));
            Assert.That(result.HasColumnCount, Is.False);
            Assert.That(result.ColumnCount, Is.EqualTo(-1));
            Assert.That(result.RowPositions.Single().RowIndex, Is.EqualTo(502));
            Assert.That(result.RowPositions.Single().HasSetSize, Is.False);
            Assert.That(result.ColumnPositions.Single().ColumnIndex, Is.EqualTo(31));
            Assert.That(result.ColumnPositions.Single().HasSetSize, Is.False);
        });
    }

    [Test]
    public void TreesFlattenExpandedBranchesAndKeepSiblingRelationships()
    {
        UiTreeItem grandchild = TreeItem("a-2-1");
        UiTreeItem firstChild = TreeItem("a-1");
        UiTreeItem secondChild = TreeItem("a-2");
        secondChild.IsExpanded = true;
        secondChild.Children = new UiTreeItem[] { grandchild };
        UiTreeItem firstRoot = TreeItem("a");
        firstRoot.IsExpanded = true;
        firstRoot.Children = new UiTreeItem[] { firstChild, secondChild };
        UiTreeItem hidden = TreeItem("b-1");
        UiTreeItem secondRoot = TreeItem("b");
        secondRoot.Children = new UiTreeItem[] { hidden };

        VirtualTreeMaterialization result = new VirtualizationMaterializer().MaterializeTreeRange(new UiTreeItem[] { firstRoot, secondRoot }, 1, 3, true, 5, "a-2");
        firstChild.Label = "mutated";
        grandchild.Value.StringValue = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.Items.Select(item => item.Key), Is.EqualTo(new string[] { "a-1", "a-2", "a-2-1" }));
            Assert.That(result.Items.Select(item => item.Children.Length), Is.All.Zero);
            Assert.That(result.Items[0].Label, Is.EqualTo("a-1"));
            Assert.That(result.Items[2].Value.StringValue, Is.EqualTo("a-2-1"));
            Assert.That(result.Range.Start, Is.EqualTo(1));
            Assert.That(result.Range.Count, Is.EqualTo(3));
            Assert.That(result.Range.TotalCount, Is.EqualTo(5));
            Assert.That(result.Range.AnchorKey, Is.EqualTo("a-2"));
            Assert.That(result.Positions.Select(position => position.LogicalIndex), Is.EqualTo(new int[] { 1, 2, 3 }));
            Assert.That(result.Positions.Select(position => position.RowIndex), Is.EqualTo(new int[] { 2, 3, 4 }));
            Assert.That(result.Positions.Select(position => position.Depth), Is.EqualTo(new int[] { 2, 2, 3 }));
            Assert.That(result.Positions.Select(position => position.PositionInSet), Is.EqualTo(new int[] { 1, 2, 1 }));
            Assert.That(result.Positions.Select(position => position.SetSize), Is.EqualTo(new int[] { 2, 2, 1 }));
            Assert.That(result.Positions.Select(position => position.ParentKey), Is.EqualTo(new string[] { "a", "a", "a-2" }));
            Assert.That(result.Items[1].Relationship.Depth, Is.EqualTo(2));
            Assert.That(result.Items[1].Relationship.PositionInSet, Is.EqualTo(2));
            Assert.That(result.Items[1].Relationship.SetSize, Is.EqualTo(2));
        });
    }

    [Test]
    public void TreesExcludeCollapsedChildrenAndSupportUnknownTotals()
    {
        UiTreeItem root = TreeItem("root");
        root.Children = new UiTreeItem[] { TreeItem("hidden") };

        VirtualTreeMaterialization result = new VirtualizationMaterializer().MaterializeTreeRange(new UiTreeItem[] { root }, 0, 10, false, 999, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items.Select(item => item.Key), Is.EqualTo(new string[] { "root" }));
            Assert.That(result.Range.HasTotalCount, Is.False);
            Assert.That(result.Range.TotalCount, Is.Zero);
            Assert.That(result.Positions.Single().Depth, Is.EqualTo(1));
            Assert.That(result.Positions.Single().ParentKey, Is.Null);
        });
    }

    [Test]
    public void RejectsInvalidRangesKeysAnchorsAndExpandedCycles()
    {
        VirtualizationMaterializer materializer = new VirtualizationMaterializer();
        UiCollectionItem duplicateItem = Item("duplicate");
        UiTableRow duplicateRow = Row("duplicate", "column");
        UiTableColumn duplicateColumn = Column("duplicate");
        UiTreeItem duplicateTree = TreeItem("duplicate");
        UiTreeItem cycle = TreeItem("cycle");
        cycle.IsExpanded = true;
        cycle.Children = new UiTreeItem[] { cycle };

        Assert.Multiple(() =>
        {
            Assert.That(() => materializer.MaterializeListRange(new UiCollectionItem[] { duplicateItem, duplicateItem }, 0, false, 0, false), Throws.ArgumentException);
            Assert.That(() => materializer.MaterializeListRange(new UiCollectionItem[] { Item("outside") }, 5, true, 5, false), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => materializer.MaterializeListRange(new UiCollectionItem[] { Item("overflow") }, int.MaxValue, false, 0, false), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => materializer.MaterializeGridRange(new UiTableRow[] { duplicateRow, duplicateRow }, 0, false, 0, null, 0, false, 0), Throws.ArgumentException);
            Assert.That(() => materializer.MaterializeGridRange(null, 0, false, 0, new UiTableColumn[] { duplicateColumn, duplicateColumn }, 0, false, 0), Throws.ArgumentException);
            Assert.That(() => materializer.MaterializeGridRange(null, 0, true, int.MaxValue, null, 0, false, 0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => materializer.MaterializeTreeRange(new UiTreeItem[] { duplicateTree, duplicateTree }, 0, 2, false, 0, null), Throws.ArgumentException);
            Assert.That(() => materializer.MaterializeTreeRange(new UiTreeItem[] { cycle }, 0, 2, false, 0, null), Throws.ArgumentException);
            Assert.That(() => materializer.MaterializeTreeRange(new UiTreeItem[] { TreeItem("only") }, 0, 1, true, 0, null), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => materializer.MaterializeTreeRange(new UiTreeItem[] { TreeItem("only") }, 0, 1, false, 0, "missing"), Throws.ArgumentException);
            Assert.That(() => materializer.MaterializeTreeRange(new UiTreeItem[] { TreeItem("first"), TreeItem("second") }, 1, 1, false, 0, "first"), Throws.ArgumentException);
        });
    }

    private static UiCollectionItem Item(string key)
    {
        return new UiCollectionItem { Key = key, Value = Text(key) };
    }

    private static UiTableRow Row(string key, string columnKey)
    {
        return new UiTableRow
        {
            Key = key,
            Cells = new UiTableCell[] { new UiTableCell { ColumnKey = columnKey, Value = Text(key) } }
        };
    }

    private static UiTableColumn Column(string key)
    {
        return new UiTableColumn { Key = key, Header = key };
    }

    private static UiTreeItem TreeItem(string key)
    {
        return new UiTreeItem { Key = key, Label = key, Value = Text(key) };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
