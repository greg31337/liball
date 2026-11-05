using htmlgui;

namespace htmlgui.Tests;

public class VirtualizationBoundaryCoverageTests
{
    [Test]
    public void EmptyAndSmallKnownCollectionsNeverCreatePhantomRanges()
    {
        VirtualizationCore core = new VirtualizationCore();
        FixedVirtualRange emptyFixed = core.CalculateFixedViewportRange(FixedOptions(UiOrientation.Vertical, 0, 20, 5), 0, 100, 50, 50);
        VariableVirtualRange emptyVariable = core.CalculateVariableViewportRange(VariableOptions(UiOrientation.Vertical, true, 0, 20, 5), 0, 100, 50, 50);
        FixedVirtualRange smallFixed = core.CalculateFixedViewportRange(FixedOptions(UiOrientation.Vertical, 2, 30, 100), 0, 500, 50, 100);
        VariableVirtualRange smallVariable = core.CalculateVariableViewportRange(VariableOptions(UiOrientation.Vertical, true, 2, 30, 100), 0, 500, 50, 100);

        Assert.Multiple(() =>
        {
            Assert.That(emptyFixed.VisibleCount, Is.Zero);
            Assert.That(emptyFixed.Request.Count, Is.Zero);
            Assert.That(emptyFixed.TotalExtent, Is.Zero);
            Assert.That(emptyVariable.VisibleCount, Is.Zero);
            Assert.That(emptyVariable.Request.Count, Is.Zero);
            Assert.That(emptyVariable.HasTotalExtent, Is.True);
            Assert.That(emptyVariable.TotalExtent, Is.Zero);
            Assert.That(smallFixed.Request.Start, Is.Zero);
            Assert.That(smallFixed.Request.Count, Is.EqualTo(2));
            Assert.That(smallFixed.BeforeExtent, Is.Zero);
            Assert.That(smallFixed.AfterExtent, Is.Zero);
            Assert.That(smallVariable.Request.Start, Is.Zero);
            Assert.That(smallVariable.Request.Count, Is.EqualTo(2));
            Assert.That(smallVariable.BeforeExtent, Is.Zero);
            Assert.That(smallVariable.AfterExtent, Is.Zero);
        });
    }

    [Test]
    public void HugeKnownCountsKeepRangeArithmeticInsideInt32Boundaries()
    {
        const int scrollOffset = 2147483600;
        VirtualizationCore core = new VirtualizationCore();
        FixedVirtualRange fixedRange = core.CalculateFixedViewportRange(FixedOptions(UiOrientation.Vertical, int.MaxValue, 1, 3), 0, scrollOffset, 1, 25);
        VariableVirtualRange variableRange = core.CalculateVariableViewportRange(VariableOptions(UiOrientation.Vertical, true, int.MaxValue, 1, 3), 0, scrollOffset, 1, 25);

        Assert.Multiple(() =>
        {
            Assert.That(fixedRange.VisibleStart, Is.EqualTo(2147483600));
            Assert.That(fixedRange.VisibleCount, Is.EqualTo(25));
            Assert.That(fixedRange.Request.Start, Is.EqualTo(2147483597));
            Assert.That(fixedRange.Request.Count, Is.EqualTo(31));
            Assert.That(fixedRange.AfterExtent, Is.EqualTo(19));
            Assert.That(variableRange.VisibleStart, Is.EqualTo(2147483600));
            Assert.That(variableRange.VisibleCount, Is.EqualTo(25));
            Assert.That(variableRange.Request.Start, Is.EqualTo(2147483597));
            Assert.That(variableRange.Request.Count, Is.EqualTo(31));
            Assert.That(variableRange.AfterExtent, Is.EqualTo(19));
        });
    }

    [Test]
    public void ExactItemBoundariesAndZeroViewportsHaveDeterministicRanges()
    {
        VirtualizationCore core = new VirtualizationCore();
        UiVirtualization fixedOptions = FixedOptions(UiOrientation.Vertical, 10, 10, 2);
        UiVirtualization variableOptions = VariableOptions(UiOrientation.Vertical, true, 10, 10, 2);

        FixedVirtualRange fixedBoundary = core.CalculateFixedViewportRange(fixedOptions, 0, 20, 10, 20);
        VariableVirtualRange variableBoundary = core.CalculateVariableViewportRange(variableOptions, 0, 20, 10, 20);
        FixedVirtualRange fixedZero = core.CalculateFixedViewportRange(fixedOptions, 0, 50, 10, 0);
        VariableVirtualRange variableZero = core.CalculateVariableViewportRange(variableOptions, 0, 50, 10, 0);

        Assert.Multiple(() =>
        {
            Assert.That(fixedBoundary.VisibleStart, Is.EqualTo(2));
            Assert.That(fixedBoundary.VisibleCount, Is.EqualTo(2));
            Assert.That(fixedBoundary.Request.Start, Is.Zero);
            Assert.That(fixedBoundary.Request.Count, Is.EqualTo(6));
            Assert.That(fixedBoundary.AfterExtent, Is.EqualTo(40));
            Assert.That(variableBoundary.VisibleStart, Is.EqualTo(2));
            Assert.That(variableBoundary.VisibleCount, Is.EqualTo(2));
            Assert.That(variableBoundary.Request.Start, Is.Zero);
            Assert.That(variableBoundary.Request.Count, Is.EqualTo(6));
            Assert.That(variableBoundary.AfterExtent, Is.EqualTo(40));
            Assert.That(fixedZero.Request.Count, Is.Zero);
            Assert.That(variableZero.Request.Count, Is.Zero);
        });
    }

    [Test]
    public void HorizontalVariableRangesUseWidthScrollXAndMeasuredExtents()
    {
        UiVirtualization options = VariableOptions(UiOrientation.Horizontal, true, 10, 20, 1);
        VirtualizationCore core = new VirtualizationCore();
        core.ApplyVariableMeasurement(options, Measurement("wide", 1, 40, 1), 0);

        VariableVirtualRange result = core.CalculateVariableViewportRange(options, 25, 999, 50, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Orientation, Is.EqualTo(UiOrientation.Horizontal));
            Assert.That(result.ViewportOffset, Is.EqualTo(25));
            Assert.That(result.ViewportExtent, Is.EqualTo(50));
            Assert.That(result.VisibleStart, Is.EqualTo(1));
            Assert.That(result.VisibleCount, Is.EqualTo(2));
            Assert.That(result.Request.Start, Is.Zero);
            Assert.That(result.Request.Count, Is.EqualTo(4));
            Assert.That(result.TotalExtent, Is.EqualTo(220));
            Assert.That(result.BeforeExtent, Is.Zero);
            Assert.That(result.AfterExtent, Is.EqualTo(120));
        });
    }

    [Test]
    public void RepeatedResizesReportSignedCorrectionsOnlyBeforeTheAnchor()
    {
        UiVirtualization options = VariableOptions(UiOrientation.Vertical, true, 5, 20, 1);
        VirtualizationCore core = new VirtualizationCore();

        VariableMeasurementUpdate initial = core.ApplyVariableMeasurement(options, Measurement("first", 0, 30, 1), 3);
        VariableMeasurementUpdate repeated = core.ApplyVariableMeasurement(options, Measurement("first", 0, 30, 1), 3);
        VariableMeasurementUpdate smaller = core.ApplyVariableMeasurement(options, Measurement("first", 0, 10, 2), 3);
        VariableMeasurementUpdate atAnchor = core.ApplyVariableMeasurement(options, Measurement("anchor", 3, 40, 1), 3);
        VariableMeasurementUpdate afterAnchor = core.ApplyVariableMeasurement(options, Measurement("after", 4, 50, 1), 3);

        Assert.Multiple(() =>
        {
            Assert.That(initial.Accepted, Is.True);
            Assert.That(initial.CorrectionDelta, Is.EqualTo(10));
            Assert.That(initial.TotalExtent, Is.EqualTo(110));
            Assert.That(repeated.Accepted, Is.False);
            Assert.That(repeated.CorrectionDelta, Is.Zero);
            Assert.That(smaller.Accepted, Is.True);
            Assert.That(smaller.IsResize, Is.True);
            Assert.That(smaller.PreviousExtent, Is.EqualTo(30));
            Assert.That(smaller.CorrectionDelta, Is.EqualTo(-20));
            Assert.That(smaller.TotalExtent, Is.EqualTo(90));
            Assert.That(atAnchor.CorrectionDelta, Is.Zero);
            Assert.That(afterAnchor.CorrectionDelta, Is.Zero);
            Assert.That(afterAnchor.TotalExtent, Is.EqualTo(140));
        });
    }

    [Test]
    public void HorizontalAnchorsPreserveTheirViewportPositionAcrossIndexMoves()
    {
        VirtualizationCore core = new VirtualizationCore();
        UiVirtualization fixedOptions = FixedOptions(UiOrientation.Horizontal, 20, 10, 1);
        FixedVirtualAnchor fixedAnchor = core.CaptureFixedAnchor(fixedOptions, 4, "fixed", 35, 900, 30, 1);
        UiScrollTarget fixedRestored = core.RestoreFixedAnchor(fixedOptions, fixedAnchor, 6, 30, 1);

        UiVirtualization variableOptions = VariableOptions(UiOrientation.Horizontal, false, 0, 10, 1);
        core.ApplyVariableMeasurement(variableOptions, Measurement("moving", 5, 20, 1), 0);
        VariableVirtualAnchor variableAnchor = core.CaptureVariableAnchor(variableOptions, 5, "moving", 47, 900, 20, 1);
        core.ApplyVariableMeasurement(variableOptions, Measurement("before", 2, 15, 1), 5);
        core.ApplyVariableMeasurement(variableOptions, Measurement("moving", 7, 20, 2), 5);
        UiScrollTarget variableRestored = core.RestoreVariableAnchor(variableOptions, variableAnchor, 20, 1);

        Assert.Multiple(() =>
        {
            Assert.That(fixedAnchor.ViewportOffset, Is.EqualTo(5));
            Assert.That(fixedRestored.Index, Is.EqualTo(6));
            Assert.That(fixedRestored.Offset, Is.EqualTo(55));
            Assert.That(variableAnchor.ViewportOffset, Is.EqualTo(3));
            Assert.That(variableRestored.Index, Is.EqualTo(7));
            Assert.That(variableRestored.Key, Is.EqualTo("moving"));
            Assert.That(variableRestored.Offset, Is.EqualTo(72));
        });
    }

    [TestCase("start", 40)]
    [TestCase("center", 35)]
    [TestCase("end", 30)]
    [TestCase("nearest", 30)]
    public void VariableScrollTargetsHonorEveryAlignment(string alignment, double expectedOffset)
    {
        UiVirtualization options = VariableOptions(UiOrientation.Vertical, true, 10, 10, 0);
        VirtualizationCore core = new VirtualizationCore();
        core.ApplyVariableMeasurement(options, Measurement("wide", 4, 30, 1), 0);
        UiScrollTarget target = new UiScrollTarget { HasIndex = true, Index = 4, Key = "wide", Alignment = alignment };

        UiScrollTarget result = core.CalculateVariableScrollTarget(options, target, 700, 0, 1, 40);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(target));
            Assert.That(result.Index, Is.EqualTo(4));
            Assert.That(result.Key, Is.EqualTo("wide"));
            Assert.That(result.Alignment, Is.EqualTo(alignment));
            Assert.That(result.Offset, Is.EqualTo(expectedOffset));
        });
    }

    [Test]
    public void VariableScrollTargetsClampKnownEndsAndPermitUnknownLogicalIndices()
    {
        VirtualizationCore core = new VirtualizationCore();
        UiVirtualization known = VariableOptions(UiOrientation.Vertical, true, 10, 10, 0);
        core.ApplyVariableMeasurement(known, Measurement("wide", 4, 30, 1), 0);
        UiScrollTarget knownEnd = core.CalculateVariableScrollTarget(known, Target(9, "start"), 0, 0, 1, 40);

        UiVirtualization unknown = VariableOptions(UiOrientation.Vertical, false, 0, 20, 0);
        UiScrollTarget unknownEnd = new VirtualizationCore().CalculateVariableScrollTarget(unknown, Target(100, "end"), 0, 0, 1, 100);

        Assert.Multiple(() =>
        {
            Assert.That(knownEnd.Offset, Is.EqualTo(80));
            Assert.That(unknownEnd.Index, Is.EqualTo(100));
            Assert.That(unknownEnd.Offset, Is.EqualTo(1920));
        });
    }

    [Test]
    public void VariableScrollTargetsRejectMissingIndicesInvalidAlignmentsAndMetrics()
    {
        VirtualizationCore core = new VirtualizationCore();
        UiVirtualization options = VariableOptions(UiOrientation.Vertical, true, 10, 20, 0);
        UiScrollTarget missingIndex = new UiScrollTarget { Alignment = "start" };
        UiScrollTarget negativeIndex = Target(-1, "start");
        UiScrollTarget outside = Target(10, "start");
        UiScrollTarget invalidAlignment = Target(1, "middle");

        Assert.Multiple(() =>
        {
            Assert.That(() => core.CalculateVariableScrollTarget(options, null, 0, 0, 10, 10), Throws.ArgumentNullException);
            Assert.That(() => core.CalculateVariableScrollTarget(options, missingIndex, 0, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.CalculateVariableScrollTarget(options, negativeIndex, 0, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.CalculateVariableScrollTarget(options, outside, 0, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.CalculateVariableScrollTarget(options, invalidAlignment, 0, 0, 10, 10), Throws.ArgumentException);
            Assert.That(() => core.CalculateVariableScrollTarget(options, Target(1, "start"), double.NaN, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        });
    }

    private static UiVirtualization FixedOptions(UiOrientation orientation, int totalCount, double itemExtent, int overscan)
    {
        return new UiVirtualization
        {
            IsEnabled = true,
            Orientation = orientation,
            HasItemExtent = true,
            ItemExtent = itemExtent,
            Overscan = overscan,
            HasTotalCount = true,
            TotalCount = totalCount
        };
    }

    private static UiVirtualization VariableOptions(UiOrientation orientation, bool hasTotalCount, int totalCount, double itemExtent, int overscan)
    {
        return new UiVirtualization
        {
            IsEnabled = true,
            Orientation = orientation,
            ItemExtent = itemExtent,
            Overscan = overscan,
            HasTotalCount = hasTotalCount,
            TotalCount = totalCount
        };
    }

    private static UiMeasuredItem Measurement(string key, int index, double extent, int generation)
    {
        return new UiMeasuredItem { Key = key, Index = index, Extent = extent, Generation = generation };
    }

    private static UiScrollTarget Target(int index, string alignment)
    {
        return new UiScrollTarget { HasIndex = true, Index = index, Alignment = alignment };
    }
}
