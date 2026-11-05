namespace chart.Tests;

[TestFixture]
public class ChartCopyMutationBehaviorTests
{
    [Test]
    public void PreviouslyUncoveredValueCopies_RemainIndependentAfterEveryMutation()
    {
        ChartHexbinCell hexbin = new ChartHexbinCell(1.0, 2.0);
        hexbin.SetCount(3);
        ChartHexbinCell hexbinCopy = hexbin.Copy();
        hexbin.SetCenterX(4.0);
        hexbin.SetCenterY(5.0);
        hexbin.SetCount(6);
        Assert.That(hexbinCopy.GetCenterX(), Is.EqualTo(1.0));
        Assert.That(hexbinCopy.GetCenterY(), Is.EqualTo(2.0));
        Assert.That(hexbinCopy.GetCount(), Is.EqualTo(3));

        ChartHierarchyRectItem hierarchy = new ChartHierarchyRectItem(1, "node", 2.0, 3, new ChartRect(1.0, 2.0, 3.0, 4.0), new ChartColor(5, 6, 7, 8));
        ChartHierarchyRectItem hierarchyCopy = hierarchy.Copy();
        hierarchy.SetNodeIndex(9);
        hierarchy.SetLabel("changed");
        hierarchy.SetValue(10.0);
        hierarchy.SetDepth(11);
        hierarchy.GetBounds().SetX(12.0);
        hierarchy.GetColor().SetR(13);
        Assert.That(hierarchyCopy.GetNodeIndex(), Is.EqualTo(1));
        Assert.That(hierarchyCopy.GetLabel(), Is.EqualTo("node"));
        Assert.That(hierarchyCopy.GetValue(), Is.EqualTo(2.0));
        Assert.That(hierarchyCopy.GetDepth(), Is.EqualTo(3));
        Assert.That(hierarchyCopy.GetBounds().GetX(), Is.EqualTo(1.0));
        Assert.That(hierarchyCopy.GetColor().GetR(), Is.EqualTo(5));

        ChartParallelAxisLayout axis = new ChartParallelAxisLayout(1, "axis", 2.0, 3.0, 4.0);
        ChartParallelAxisLayout axisCopy = axis.Copy();
        axis.SetAxisIndex(5);
        axis.SetLabel("changed");
        axis.SetX(6.0);
        axis.SetMinimum(7.0);
        axis.SetMaximum(8.0);
        Assert.That(axisCopy.GetAxisIndex(), Is.EqualTo(1));
        Assert.That(axisCopy.GetLabel(), Is.EqualTo("axis"));
        Assert.That(axisCopy.GetX(), Is.EqualTo(2.0));
        Assert.That(axisCopy.GetMinimum(), Is.EqualTo(3.0));
        Assert.That(axisCopy.GetMaximum(), Is.EqualTo(4.0));

        ChartParallelCategoryFlow flow = new ChartParallelCategoryFlow(1, 2, 3, 4.0, 5.0, 6.0, 7.0);
        ChartParallelCategoryFlow flowCopy = flow.Copy();
        flow.SetRowIndex(8);
        flow.SetSourceBandIndex(9);
        flow.SetTargetBandIndex(10);
        flow.SetX1(11.0);
        flow.SetY1(12.0);
        flow.SetX2(13.0);
        flow.SetY2(14.0);
        Assert.That(flowCopy.GetRowIndex(), Is.EqualTo(1));
        Assert.That(flowCopy.GetSourceBandIndex(), Is.EqualTo(2));
        Assert.That(flowCopy.GetTargetBandIndex(), Is.EqualTo(3));
        Assert.That(flowCopy.GetX1(), Is.EqualTo(4.0));
        Assert.That(flowCopy.GetY1(), Is.EqualTo(5.0));
        Assert.That(flowCopy.GetX2(), Is.EqualTo(6.0));
        Assert.That(flowCopy.GetY2(), Is.EqualTo(7.0));

        ChartParallelRecordLayout record = new ChartParallelRecordLayout(1, new ChartPoint[] { new ChartPoint(2.0, 3.0), new ChartPoint(4.0, 5.0) });
        ChartParallelRecordLayout recordCopy = record.Copy();
        record.SetRowIndex(4);
        record.GetPoints()[0].SetX(5.0);
        Assert.That(recordCopy.GetRowIndex(), Is.EqualTo(1));
        Assert.That(recordCopy.GetPoints()[0].GetX(), Is.EqualTo(2.0));

        ChartPointAccessibility accessible = new ChartPointAccessibility("point", "description", 1);
        ChartPointAccessibility accessibleCopy = accessible.Copy();
        accessible.SetPointId("changed");
        accessible.SetDescription("changed description");
        accessible.SetRowIndex(2);
        Assert.That(accessibleCopy.GetPointId(), Is.EqualTo("point"));
        Assert.That(accessibleCopy.GetDescription(), Is.EqualTo("description"));
        Assert.That(accessibleCopy.GetRowIndex(), Is.EqualTo(1));

        ChartTableCellLayout cell = new ChartTableCellLayout(1, 2, true, "cell", new ChartRect(3.0, 4.0, 5.0, 6.0));
        ChartTableCellLayout cellCopy = cell.Copy();
        cell.SetRowIndex(7);
        cell.SetColumnIndex(8);
        cell.SetHeader(false);
        cell.SetText("changed");
        cell.GetBounds().SetX(9.0);
        Assert.That(cellCopy.GetRowIndex(), Is.EqualTo(1));
        Assert.That(cellCopy.GetColumnIndex(), Is.EqualTo(2));
        Assert.That(cellCopy.GetHeader(), Is.True);
        Assert.That(cellCopy.GetText(), Is.EqualTo("cell"));
        Assert.That(cellCopy.GetBounds().GetX(), Is.EqualTo(3.0));

        ChartVisualMapBin bin = new ChartVisualMapBin(1.0, 2.0, new ChartColor(3, 4, 5, 6), "bin");
        ChartVisualMapBin binCopy = bin.Copy();
        bin.SetMinimum(7.0);
        bin.SetMaximum(8.0);
        bin.GetColor().SetR(9);
        bin.SetLabel("changed");
        Assert.That(binCopy.GetMinimum(), Is.EqualTo(1.0));
        Assert.That(binCopy.GetMaximum(), Is.EqualTo(2.0));
        Assert.That(binCopy.GetColor().GetR(), Is.EqualTo(3));
        Assert.That(binCopy.GetLabel(), Is.EqualTo("bin"));

        ChartVisualMapCategory category = new ChartVisualMapCategory("category", new ChartColor(1, 2, 3, 4));
        ChartVisualMapCategory categoryCopy = category.Copy();
        category.SetCategory("changed");
        category.GetColor().SetR(5);
        Assert.That(categoryCopy.GetCategory(), Is.EqualTo("category"));
        Assert.That(categoryCopy.GetColor().GetR(), Is.EqualTo(1));
    }
}
