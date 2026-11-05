using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffOrientationTransformerTests
{
    [TestCase(TiffOrientation.TopLeft, new byte[] { 1, 2, 3, 4, 5, 6 }, 3, 2)]
    [TestCase(TiffOrientation.TopRight, new byte[] { 3, 2, 1, 6, 5, 4 }, 3, 2)]
    [TestCase(TiffOrientation.BottomRight, new byte[] { 6, 5, 4, 3, 2, 1 }, 3, 2)]
    [TestCase(TiffOrientation.BottomLeft, new byte[] { 4, 5, 6, 1, 2, 3 }, 3, 2)]
    [TestCase(TiffOrientation.LeftTop, new byte[] { 1, 4, 2, 5, 3, 6 }, 2, 3)]
    [TestCase(TiffOrientation.RightTop, new byte[] { 4, 1, 5, 2, 6, 3 }, 2, 3)]
    [TestCase(TiffOrientation.RightBottom, new byte[] { 6, 3, 5, 2, 4, 1 }, 2, 3)]
    [TestCase(TiffOrientation.LeftBottom, new byte[] { 3, 6, 2, 5, 1, 4 }, 2, 3)]
    public void TransformInterleavedBytes_NormalizesAllTiffOrientations(TiffOrientation orientation, byte[] expected, int expectedWidth, int expectedLength)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        TiffOrientationTransformResult result = TiffOrientationTransformer.TransformInterleavedBytes(3, 2, 1, orientation, new byte[] { 1, 2, 3, 4, 5, 6 }, diagnostics);

        Assert.That(result.Success, Is.True);
        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result.Width, Is.EqualTo(expectedWidth));
        Assert.That(result.Length, Is.EqualTo(expectedLength));
        Assert.That(result.RowByteCount, Is.EqualTo(expectedWidth));
        Assert.That(result.Bytes, Is.EqualTo(expected));
    }

    [Test]
    public void TransformInterleavedBytes_PreservesMultiBytePixels()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        TiffOrientationTransformResult result = TiffOrientationTransformer.TransformInterleavedBytes(
            2,
            2,
            2,
            TiffOrientation.BottomRight,
            new byte[] { 1, 10, 2, 20, 3, 30, 4, 40 },
            diagnostics);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 4, 40, 3, 30, 2, 20, 1, 10 }));
        Assert.That(result.RowByteCount, Is.EqualTo(4));
        Assert.That(result.TotalByteCount, Is.EqualTo(8));
    }

    [Test]
    public void ApplyOrientation_UsesImageInfoDimensionsAndOrientation()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 3;
        info.Length = 2;
        info.Orientation = TiffOrientation.RightTop;
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        TiffOrientationTransformResult result = TiffCodec.ApplyOrientation(info, new byte[] { 1, 2, 3, 4, 5, 6 }, 1, diagnostics);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Width, Is.EqualTo(2));
        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 4, 1, 5, 2, 6, 3 }));
    }

    [Test]
    public void TransformInterleavedBytes_ReportsUnknownOrientation()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        TiffOrientationTransformResult result = TiffOrientationTransformer.TransformInterleavedBytes(2, 2, 1, TiffOrientation.Unknown, new byte[] { 1, 2, 3, 4 }, diagnostics);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void TransformInterleavedBytes_ReportsSourceByteCountMismatch()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        TiffOrientationTransformResult result = TiffOrientationTransformer.TransformInterleavedBytes(2, 2, 1, TiffOrientation.TopLeft, new byte[] { 1, 2, 3 }, diagnostics);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void TransformInterleavedBytes_ReportsAllocationLimit()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 3;
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        TiffOrientationTransformResult result = TiffOrientationTransformer.TransformInterleavedBytes(2, 2, 1, TiffOrientation.TopLeft, new byte[] { 1, 2, 3, 4 }, limits, diagnostics);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }
}
