using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class VirtualizationCoreTests
{
    [Test]
    public void CalculatesOverscannedVerticalRangesAndClampsAtTheEnd()
    {
        UiVirtualization options = FixedOptions(UiOrientation.Vertical, 100, 20, 2);
        VirtualizationCore core = new VirtualizationCore();

        FixedVirtualRange middle = core.CalculateFixedViewportRange(options, 900, 45, 10, 50);
        FixedVirtualRange end = core.CalculateFixedViewportRange(options, 900, 5000, 10, 50);

        Assert.Multiple(() =>
        {
            Assert.That(middle.Orientation, Is.EqualTo(UiOrientation.Vertical));
            Assert.That(middle.ViewportOffset, Is.EqualTo(45));
            Assert.That(middle.ViewportExtent, Is.EqualTo(50));
            Assert.That(middle.VisibleStart, Is.EqualTo(2));
            Assert.That(middle.VisibleCount, Is.EqualTo(3));
            Assert.That(middle.Request.Start, Is.Zero);
            Assert.That(middle.Request.Count, Is.EqualTo(7));
            Assert.That(middle.BeforeExtent, Is.Zero);
            Assert.That(middle.AfterExtent, Is.EqualTo(1860));
            Assert.That(middle.TotalExtent, Is.EqualTo(2000));
            Assert.That(end.ViewportOffset, Is.EqualTo(1950));
            Assert.That(end.VisibleStart, Is.EqualTo(97));
            Assert.That(end.VisibleCount, Is.EqualTo(3));
            Assert.That(end.Request.Start, Is.EqualTo(95));
            Assert.That(end.Request.Count, Is.EqualTo(5));
            Assert.That(end.BeforeExtent, Is.EqualTo(1900));
            Assert.That(end.AfterExtent, Is.Zero);
        });
    }

    [Test]
    public void HorizontalRangesUseTheHorizontalViewportAxis()
    {
        UiVirtualization options = FixedOptions(UiOrientation.Horizontal, 10, 25, 1);

        FixedVirtualRange result = new VirtualizationCore().CalculateFixedViewportRange(options, 30, 175, 50, 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Orientation, Is.EqualTo(UiOrientation.Horizontal));
            Assert.That(result.ViewportOffset, Is.EqualTo(30));
            Assert.That(result.ViewportExtent, Is.EqualTo(50));
            Assert.That(result.VisibleStart, Is.EqualTo(1));
            Assert.That(result.VisibleCount, Is.EqualTo(3));
            Assert.That(result.Request.Start, Is.Zero);
            Assert.That(result.Request.Count, Is.EqualTo(5));
            Assert.That(result.AfterExtent, Is.EqualTo(125));
        });
    }

    [Test]
    public void SmallCollectionsClampWithoutCreatingPhantomItems()
    {
        UiVirtualization options = FixedOptions(UiOrientation.Vertical, 2, 30, 4);

        FixedVirtualRange result = new VirtualizationCore().CalculateFixedViewportRange(options, 0, -50, 100, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.ViewportOffset, Is.Zero);
            Assert.That(result.VisibleStart, Is.Zero);
            Assert.That(result.VisibleCount, Is.EqualTo(2));
            Assert.That(result.Request.Start, Is.Zero);
            Assert.That(result.Request.Count, Is.EqualTo(2));
            Assert.That(result.TotalExtent, Is.EqualTo(60));
            Assert.That(result.BeforeExtent, Is.Zero);
            Assert.That(result.AfterExtent, Is.Zero);
        });
    }

    [TestCase("start", 8, 50, 80)]
    [TestCase("center", 8, 50, 60)]
    [TestCase("end", 8, 50, 40)]
    [TestCase("nearest", 8, 50, 50)]
    [TestCase("nearest", 8, 0, 40)]
    [TestCase("nearest", 8, 100, 80)]
    [TestCase("start", 19, 50, 150)]
    [TestCase("end", 0, 50, 0)]
    public void CalculatesAlignedAndClampedScrollTargets(string alignment, int index, double currentOffset, double expectedOffset)
    {
        UiVirtualization options = FixedOptions(UiOrientation.Vertical, 20, 10, 0);
        UiScrollTarget request = new UiScrollTarget
        {
            HasIndex = true,
            Index = index,
            Key = "item-" + index,
            Alignment = alignment,
            Offset = 999
        };

        UiScrollTarget result = new VirtualizationCore().CalculateFixedScrollTarget(options, request, 700, currentOffset, 25, 50);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(request));
            Assert.That(result.HasIndex, Is.True);
            Assert.That(result.Index, Is.EqualTo(index));
            Assert.That(result.Key, Is.EqualTo("item-" + index));
            Assert.That(result.Alignment, Is.EqualTo(alignment));
            Assert.That(result.Offset, Is.EqualTo(expectedOffset));
        });
    }

    [Test]
    public void CapturedAnchorKeepsItsViewportPositionAfterItsIndexMoves()
    {
        UiVirtualization before = FixedOptions(UiOrientation.Vertical, 100, 20, 1);
        UiVirtualization after = FixedOptions(UiOrientation.Vertical, 101, 20, 1);
        VirtualizationCore core = new VirtualizationCore();

        FixedVirtualAnchor anchor = core.CaptureFixedAnchor(before, 11, "stable-item", 0, 205, 100, 80);
        UiScrollTarget restored = core.RestoreFixedAnchor(after, anchor, 12, 100, 80);

        Assert.Multiple(() =>
        {
            Assert.That(anchor.Index, Is.EqualTo(11));
            Assert.That(anchor.Key, Is.EqualTo("stable-item"));
            Assert.That(anchor.ViewportOffset, Is.EqualTo(15));
            Assert.That(restored.HasIndex, Is.True);
            Assert.That(restored.Index, Is.EqualTo(12));
            Assert.That(restored.Key, Is.EqualTo("stable-item"));
            Assert.That(restored.Alignment, Is.EqualTo("nearest"));
            Assert.That(restored.Offset, Is.EqualTo(225));
        });
    }

    [Test]
    public void RejectsInvalidFixedMetricsTargetsAndAnchors()
    {
        VirtualizationCore core = new VirtualizationCore();
        UiVirtualization valid = FixedOptions(UiOrientation.Vertical, 10, 20, 1);
        UiVirtualization missingExtent = FixedOptions(UiOrientation.Vertical, 10, 20, 1);
        missingExtent.HasItemExtent = false;
        UiVirtualization missingCount = FixedOptions(UiOrientation.Vertical, 10, 20, 1);
        missingCount.HasTotalCount = false;
        UiVirtualization negativeOverscan = FixedOptions(UiOrientation.Vertical, 10, 20, -1);
        UiScrollTarget invalidIndex = new UiScrollTarget { HasIndex = true, Index = 10, Alignment = "start" };
        UiScrollTarget invalidAlignment = new UiScrollTarget { HasIndex = true, Index = 1, Alignment = "middle" };

        Assert.Multiple(() =>
        {
            Assert.That(() => core.CalculateFixedViewportRange(null, 0, 0, 10, 10), Throws.ArgumentNullException);
            Assert.That(() => core.CalculateFixedViewportRange(missingExtent, 0, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.CalculateFixedViewportRange(missingCount, 0, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.CalculateFixedViewportRange(negativeOverscan, 0, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.CalculateFixedViewportRange(valid, 0, double.NaN, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.CalculateFixedViewportRange(valid, 0, 0, 10, -1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.CalculateFixedScrollTarget(valid, null, 0, 0, 10, 10), Throws.ArgumentNullException);
            Assert.That(() => core.CalculateFixedScrollTarget(valid, invalidIndex, 0, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.CalculateFixedScrollTarget(valid, invalidAlignment, 0, 0, 10, 10), Throws.ArgumentException);
            Assert.That(() => core.CaptureFixedAnchor(valid, -1, "item", 0, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.RestoreFixedAnchor(valid, null, 0, 10, 10), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void VariableMeasurementsCacheResizesAndReturnAnchorCorrections()
    {
        UiVirtualization options = VariableOptions(UiOrientation.Vertical, true, 10, 20, 1);
        VirtualizationCore core = new VirtualizationCore();

        VariableMeasurementUpdate measured = core.ApplyVariableMeasurement(options, Measurement("item-one", 1, 30, 1), 4);
        VariableMeasurementUpdate resized = core.ApplyVariableMeasurement(options, Measurement("item-one", 1, 35, 2), 4);
        VariableMeasurementUpdate stale = core.ApplyVariableMeasurement(options, Measurement("item-one", 1, 50, 1), 4);
        VariableMeasurementUpdate afterAnchor = core.ApplyVariableMeasurement(options, Measurement("item-six", 6, 10, 1), 4);
        UiMeasuredItem[] snapshot = core.ReadVariableMeasurements();
        snapshot[0].Extent = 999;
        UiMeasuredItem[] repeatedSnapshot = core.ReadVariableMeasurements();

        Assert.Multiple(() =>
        {
            Assert.That(measured.Accepted, Is.True);
            Assert.That(measured.IsResize, Is.False);
            Assert.That(measured.PreviousExtent, Is.EqualTo(20));
            Assert.That(measured.Extent, Is.EqualTo(30));
            Assert.That(measured.CorrectionDelta, Is.EqualTo(10));
            Assert.That(measured.HasTotalExtent, Is.True);
            Assert.That(measured.TotalExtent, Is.EqualTo(210));
            Assert.That(resized.Accepted, Is.True);
            Assert.That(resized.IsResize, Is.True);
            Assert.That(resized.PreviousExtent, Is.EqualTo(30));
            Assert.That(resized.CorrectionDelta, Is.EqualTo(5));
            Assert.That(resized.TotalExtent, Is.EqualTo(215));
            Assert.That(stale.Accepted, Is.False);
            Assert.That(stale.Extent, Is.EqualTo(35));
            Assert.That(stale.CorrectionDelta, Is.Zero);
            Assert.That(afterAnchor.Accepted, Is.True);
            Assert.That(afterAnchor.CorrectionDelta, Is.Zero);
            Assert.That(afterAnchor.TotalExtent, Is.EqualTo(205));
            Assert.That(repeatedSnapshot.Select(item => item.Key), Is.EqualTo(new string[] { "item-one", "item-six" }));
            Assert.That(repeatedSnapshot.Select(item => item.Extent), Is.EqualTo(new double[] { 35, 10 }));
        });
    }

    [Test]
    public void VariableRangesCombineMeasuredAndEstimatedExtents()
    {
        UiVirtualization options = VariableOptions(UiOrientation.Vertical, true, 10, 20, 1);
        VirtualizationCore core = new VirtualizationCore();
        core.ApplyVariableMeasurement(options, Measurement("zero", 0, 10, 1), 0);
        core.ApplyVariableMeasurement(options, Measurement("one", 1, 30, 1), 0);
        core.ApplyVariableMeasurement(options, Measurement("two", 2, 40, 1), 0);

        VariableVirtualRange result = core.CalculateVariableViewportRange(options, 700, 15, 25, 40);

        Assert.Multiple(() =>
        {
            Assert.That(result.Orientation, Is.EqualTo(UiOrientation.Vertical));
            Assert.That(result.HasTotalExtent, Is.True);
            Assert.That(result.TotalExtent, Is.EqualTo(220));
            Assert.That(result.ViewportOffset, Is.EqualTo(15));
            Assert.That(result.ViewportExtent, Is.EqualTo(40));
            Assert.That(result.VisibleStart, Is.EqualTo(1));
            Assert.That(result.VisibleCount, Is.EqualTo(2));
            Assert.That(result.Request.Start, Is.Zero);
            Assert.That(result.Request.Count, Is.EqualTo(4));
            Assert.That(result.BeforeExtent, Is.Zero);
            Assert.That(result.AfterExtent, Is.EqualTo(120));
        });
    }

    [Test]
    public void UnknownTotalsProduceBoundedRangesWithoutInventingAnEndExtent()
    {
        UiVirtualization options = VariableOptions(UiOrientation.Vertical, false, 0, 20, 1);
        VirtualizationCore core = new VirtualizationCore();
        core.ApplyVariableMeasurement(options, Measurement("zero", 0, 10, 1), 0);
        core.ApplyVariableMeasurement(options, Measurement("one", 1, 30, 1), 0);
        core.ApplyVariableMeasurement(options, Measurement("two", 2, 40, 1), 0);

        VariableVirtualRange result = core.CalculateVariableViewportRange(options, 0, 200, 100, 40);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasTotalExtent, Is.False);
            Assert.That(result.TotalExtent, Is.Zero);
            Assert.That(result.VisibleStart, Is.EqualTo(9));
            Assert.That(result.VisibleCount, Is.EqualTo(2));
            Assert.That(result.Request.Start, Is.EqualTo(8));
            Assert.That(result.Request.Count, Is.EqualTo(4));
            Assert.That(result.BeforeExtent, Is.EqualTo(180));
            Assert.That(result.AfterExtent, Is.Zero);
        });
    }

    [Test]
    public void StableVariableAnchorFollowsItsKeyAndIncludesEarlierResizeCorrections()
    {
        UiVirtualization options = VariableOptions(UiOrientation.Vertical, true, 20, 20, 1);
        VirtualizationCore core = new VirtualizationCore();
        core.ApplyVariableMeasurement(options, Measurement("anchor", 5, 30, 1), 0);
        VariableVirtualAnchor anchor = core.CaptureVariableAnchor(options, 5, "anchor", 0, 95, 100, 80);

        VariableMeasurementUpdate correction = core.ApplyVariableMeasurement(options, Measurement("before", 2, 35, 1), 5);
        core.ApplyVariableMeasurement(options, Measurement("anchor", 7, 30, 2), 5);
        UiScrollTarget restored = core.RestoreVariableAnchor(options, anchor, 100, 80);

        Assert.Multiple(() =>
        {
            Assert.That(anchor.Index, Is.EqualTo(5));
            Assert.That(anchor.Key, Is.EqualTo("anchor"));
            Assert.That(anchor.ViewportOffset, Is.EqualTo(5));
            Assert.That(correction.CorrectionDelta, Is.EqualTo(15));
            Assert.That(restored.HasIndex, Is.True);
            Assert.That(restored.Index, Is.EqualTo(7));
            Assert.That(restored.Key, Is.EqualTo("anchor"));
            Assert.That(restored.Offset, Is.EqualTo(150));
        });
    }

    [Test]
    public void RejectsInvalidVariableEstimatesMeasurementsAndAnchors()
    {
        VirtualizationCore core = new VirtualizationCore();
        UiVirtualization valid = VariableOptions(UiOrientation.Vertical, true, 10, 20, 1);
        UiVirtualization invalidEstimate = VariableOptions(UiOrientation.Vertical, true, 10, 0, 1);
        UiVirtualization invalidCount = VariableOptions(UiOrientation.Vertical, true, -1, 20, 1);
        UiMeasuredItem invalidMeasurement = Measurement("item", 0, double.NaN, 1);

        Assert.Multiple(() =>
        {
            Assert.That(() => core.CalculateVariableViewportRange(null, 0, 0, 10, 10), Throws.ArgumentNullException);
            Assert.That(() => core.CalculateVariableViewportRange(invalidEstimate, 0, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.CalculateVariableViewportRange(invalidCount, 0, 0, 10, 10), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.ApplyVariableMeasurement(valid, null, 0), Throws.ArgumentNullException);
            Assert.That(() => core.ApplyVariableMeasurement(valid, invalidMeasurement, 0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.ApplyVariableMeasurement(valid, Measurement(" ", 0, 20, 1), 0), Throws.ArgumentException);
            Assert.That(() => core.CaptureVariableAnchor(valid, 0, null, 0, 0, 10, 10), Throws.ArgumentException);
            Assert.That(() => core.RestoreVariableAnchor(valid, null, 10, 10), Throws.ArgumentNullException);
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

    private static UiVirtualization VariableOptions(UiOrientation orientation, bool hasTotalCount, int totalCount, double estimatedExtent, int overscan)
    {
        return new UiVirtualization
        {
            IsEnabled = true,
            Orientation = orientation,
            HasItemExtent = false,
            ItemExtent = estimatedExtent,
            Overscan = overscan,
            HasTotalCount = hasTotalCount,
            TotalCount = totalCount
        };
    }

    private static UiMeasuredItem Measurement(string key, int index, double extent, int generation)
    {
        return new UiMeasuredItem
        {
            Key = key,
            Index = index,
            Extent = extent,
            Generation = generation
        };
    }
}
