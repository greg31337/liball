using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8DeblockingFilterTests
{
    [Test]
    public void CalculateParameters_DerivesInteriorEdgeAndHevThresholds()
    {
        WebpVp8LoopFilterParameters keyFrame = WebpVp8DeblockingFilter.CalculateParameters(20, 0, true);
        WebpVp8LoopFilterParameters sharp = WebpVp8DeblockingFilter.CalculateParameters(30, 5, false);

        Assert.That(keyFrame.InteriorLimit, Is.EqualTo(20));
        Assert.That(keyFrame.MacroblockEdgeLimit, Is.EqualTo(64));
        Assert.That(keyFrame.SubblockEdgeLimit, Is.EqualTo(60));
        Assert.That(keyFrame.HighEdgeVarianceThreshold, Is.EqualTo(1));
        Assert.That(sharp.InteriorLimit, Is.EqualTo(4));
        Assert.That(sharp.HighEdgeVarianceThreshold, Is.EqualTo(2));
    }

    [Test]
    public void FilterSimpleSegment_AdjustsEdgePixelsWhenBelowLimit()
    {
        byte[] segment = new byte[] { 100, 100, 120, 120 };

        WebpVp8DeblockingFilter.FilterSimpleSegment(segment, 100);

        Assert.That(segment, Is.EqualTo(new byte[] { 100, 105, 115, 120 }));
    }

    [Test]
    public void FilterSimpleSegment_LeavesSegmentUnchangedWhenAboveLimit()
    {
        byte[] segment = new byte[] { 10, 10, 240, 240 };

        WebpVp8DeblockingFilter.FilterSimpleSegment(segment, 10);

        Assert.That(segment, Is.EqualTo(new byte[] { 10, 10, 240, 240 }));
    }

    [Test]
    public void FilterSubblockSegment_AdjustsP1P0Q0Q1WhenVarianceIsLow()
    {
        byte[] segment = new byte[] { 100, 102, 104, 106, 116, 118, 120, 122 };

        WebpVp8DeblockingFilter.FilterSubblockSegment(segment, 100, 100, 100);

        Assert.That(segment, Is.EqualTo(new byte[] { 100, 102, 106, 110, 112, 116, 120, 122 }));
    }

    [Test]
    public void FilterMacroblockSegment_AdjustsSixPixelsWhenVarianceIsLow()
    {
        byte[] segment = new byte[] { 100, 102, 104, 106, 116, 118, 120, 122 };

        WebpVp8DeblockingFilter.FilterMacroblockSegment(segment, 100, 100, 100);

        Assert.That(segment, Is.EqualTo(new byte[] { 100, 103, 106, 109, 113, 116, 119, 122 }));
    }

    [Test]
    public void ThresholdHelpers_DetectFilteringAndHighEdgeVariance()
    {
        Assert.That(WebpVp8DeblockingFilter.ShouldFilterSimple(100, 100, 120, 120, 100), Is.True);
        Assert.That(WebpVp8DeblockingFilter.ShouldFilterSimple(10, 10, 240, 240, 10), Is.False);
        Assert.That(WebpVp8DeblockingFilter.ShouldFilterNormal(100, 102, 104, 106, 116, 118, 120, 122, 100, 100), Is.True);
        Assert.That(WebpVp8DeblockingFilter.HasHighEdgeVariance(50, 100, 116, 180, 10), Is.True);
    }

    [Test]
    public void FilterVerticalAndHorizontalSimpleEdges_UpdatePlaneEdgeSamples()
    {
        byte[] vertical = NewPlaneColumns(6, 4, new byte[] { 100, 100, 120, 120, 0, 0 });
        byte[] horizontal = NewPlaneRows(4, 6, new byte[] { 100, 100, 120, 120, 0, 0 });

        WebpVp8DeblockingFilter.FilterVerticalSimpleEdge(vertical, 6, 4, 6, 2, 0, 4, 100);
        WebpVp8DeblockingFilter.FilterHorizontalSimpleEdge(horizontal, 4, 6, 4, 2, 0, 4, 100);

        Assert.That(vertical[1], Is.EqualTo(105));
        Assert.That(vertical[2], Is.EqualTo(115));
        Assert.That(vertical[(3 * 6) + 1], Is.EqualTo(105));
        Assert.That(horizontal[4], Is.EqualTo(105));
        Assert.That(horizontal[8], Is.EqualTo(115));
        Assert.That(horizontal[7], Is.EqualTo(105));
    }

    [Test]
    public void FilterVerticalNormalSubblockEdge_UpdatesPlaneInteriorSamples()
    {
        byte[] plane = NewWidePlane();
        WebpVp8LoopFilterParameters parameters = new WebpVp8LoopFilterParameters();
        parameters.InteriorLimit = 100;
        parameters.SubblockEdgeLimit = 100;
        parameters.HighEdgeVarianceThreshold = 100;

        WebpVp8DeblockingFilter.FilterVerticalNormalSubblockEdge(plane, 8, 1, 8, 4, 0, 1, parameters);

        Assert.That(plane, Is.EqualTo(new byte[] { 100, 102, 106, 110, 112, 116, 120, 122 }));
    }

    [Test]
    public void DeblockingFilter_RejectsInvalidInputs()
    {
        byte[] plane = new byte[16];

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8DeblockingFilter.CalculateParameters(64, 0, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8DeblockingFilter.CalculateParameters(1, 8, true));
        Assert.Throws<ArgumentNullException>(() => WebpVp8DeblockingFilter.FilterSimpleSegment(null!, 1));
        Assert.Throws<ArgumentException>(() => WebpVp8DeblockingFilter.FilterSimpleSegment(new byte[3], 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8DeblockingFilter.FilterSimpleSegment(new byte[4], -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8DeblockingFilter.FilterVerticalSimpleEdge(plane, 4, 4, 4, 1, 0, 4, 1));
        Assert.Throws<ArgumentException>(() => WebpVp8DeblockingFilter.FilterVerticalSimpleEdge(new byte[15], 4, 4, 4, 2, 0, 4, 1));
        Assert.Throws<ArgumentNullException>(() => WebpVp8DeblockingFilter.FilterVerticalNormalSubblockEdge(plane, 4, 4, 4, 2, 0, 4, null!));
    }

    static byte[] NewPlaneColumns(int width, int height, byte[] edgeValues)
    {
        byte[] plane = new byte[width * height];
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                plane[(row * width) + column] = edgeValues[column];
                column = column + 1;
            }

            row = row + 1;
        }

        return plane;
    }

    static byte[] NewPlaneRows(int width, int height, byte[] edgeValues)
    {
        byte[] plane = new byte[width * height];
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                plane[(row * width) + column] = edgeValues[row];
                column = column + 1;
            }

            row = row + 1;
        }

        return plane;
    }

    static byte[] NewWidePlane()
    {
        return new byte[] { 100, 102, 104, 106, 116, 118, 120, 122 };
    }
}
