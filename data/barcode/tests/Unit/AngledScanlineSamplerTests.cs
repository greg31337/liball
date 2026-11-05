namespace barcode.Tests;

public class AngledScanlineSamplerTests
{
    [Test]
    public void ArbitraryLineSamplingClipsToImageAndSupportsReverseTraversal()
    {
        ModuleMatrix matrix = new ModuleMatrix(6, 6);
        int index = 0;
        while (index < 6)
        {
            matrix.SetModule(index, index, true);
            index = index + 1;
        }

        LineSegment crossing = new LineSegment(
            GeometryMath.CreatePoint(-2.0, -2.0),
            GeometryMath.CreatePoint(7.0, 7.0));
        AngledScanlineSample forward = AngledScanlineSampler.Sample(matrix, crossing, false);
        AngledScanlineSample reversed = AngledScanlineSampler.Sample(matrix, crossing, true);

        Assert.Multiple(() =>
        {
            Assert.That(forward.Start.X, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(forward.Start.Y, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(forward.End.X, Is.EqualTo(5.0).Within(0.000000001));
            Assert.That(forward.End.Y, Is.EqualTo(5.0).Within(0.000000001));
            Assert.That(forward.Row.CopyBits(), Is.All.True);
            Assert.That(forward.AngleDegrees, Is.EqualTo(45.0).Within(0.000000001));
            Assert.That(reversed.Start.X, Is.EqualTo(5.0).Within(0.000000001));
            Assert.That(reversed.End.X, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(reversed.Row.CopyBits(), Is.All.True);
            Assert.That(reversed.AngleDegrees, Is.EqualTo(225.0).Within(0.000000001));
            Assert.That(reversed.ReversedTraversal, Is.True);
        });
    }

    [Test]
    public void DeskewedFanInterpolatesParallelQuadrilateralCrossLinesCenterOut()
    {
        ModuleMatrix matrix = new ModuleMatrix(16, 14);
        BarcodeQuadrilateral region = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(2.0, 2.0),
            GeometryMath.CreatePoint(12.0, 5.0),
            GeometryMath.CreatePoint(10.0, 11.0),
            GeometryMath.CreatePoint(0.0, 8.0));

        AngledScanlineSample[] samples = DeskewedScanlineFan.Create(
            matrix,
            region,
            2.0,
            5,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(samples, Has.Length.EqualTo(3));
            Assert.That(samples.Select(sample => sample.FanIndex), Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That(samples.Select(sample => sample.Offset), Is.EqualTo(new double[] { 0.0, -2.0, 2.0 }).Within(0.000000001));
            Assert.That(samples[0].Start.X, Is.EqualTo(1.0).Within(0.000000001));
            Assert.That(samples[0].Start.Y, Is.EqualTo(5.0).Within(0.000000001));
            Assert.That(samples[0].End.X, Is.EqualTo(11.0).Within(0.000000001));
            Assert.That(samples[0].End.Y, Is.EqualTo(8.0).Within(0.000000001));
            Assert.That(samples.Select(sample => sample.AngleDegrees),
                Is.All.EqualTo(16.69924423399362).Within(0.000000001));
        });
    }

    [Test]
    public void AngledScannerDeskewsAndReconcilesOnlyTheMajorityPayload()
    {
        BitRow expected = EncodeCode128(new byte[] { 65, 66, 67 }, 3);
        BitRow conflicting = EncodeCode128(new byte[] { 88, 89, 90 }, 3);
        RotatedBarcodeFixture fixture = CreateRotatedBarcode(expected, conflicting, 30, 15.0, true);

        AngledScanlineConsensus? consensus = AngledLinearScanner.Scan(
            fixture.Matrix,
            BarcodeFormat.Code128,
            fixture.Region,
            5.0,
            5,
            3,
            false);

        Assert.That(consensus, Is.Not.Null);
        Assert.That(consensus!.Result.Location, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(consensus.Result.Payload!.RawBytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(consensus.VoteCount, Is.EqualTo(4));
            Assert.That(consensus.ObservationCount, Is.EqualTo(5));
            Assert.That(consensus.OrientationDegrees, Is.EqualTo(15.0).Within(0.05));
            Assert.That(consensus.Result.Rotation, Is.EqualTo(BarcodeRotation.None));
            Assert.That(consensus.Result.SkewDegrees, Is.EqualTo(15.0).Within(0.05));
            Assert.That(consensus.MinimumOffset, Is.EqualTo(-10.0).Within(0.000000001));
            Assert.That(consensus.MaximumOffset, Is.EqualTo(10.0).Within(0.000000001));
            Assert.That(consensus.Result.Location!.Bounds!.Width, Is.GreaterThan(200.0));
            Assert.That(consensus.Result.Metadata!.Confidence, Is.GreaterThan(0.7));
            Assert.That(consensus.Result.Metadata.Confidence, Is.LessThan(0.8));
        });
    }

    [Test]
    public void ReverseTraversalPreservesPayloadAndPhysicalOrientation()
    {
        BitRow encoded = EncodeCode128(new byte[] { 49, 50, 51, 52 }, 3);
        RotatedBarcodeFixture fixture = CreateRotatedBarcode(encoded, encoded, 30, 20.0, false);

        AngledScanlineConsensus? forward = AngledLinearScanner.Scan(
            fixture.Matrix,
            BarcodeFormat.Code128,
            fixture.Region,
            4.0,
            5,
            3,
            false);
        AngledScanlineConsensus? reversed = AngledLinearScanner.Scan(
            fixture.Matrix,
            BarcodeFormat.Code128,
            fixture.Region,
            4.0,
            5,
            3,
            true);

        Assert.That(forward, Is.Not.Null);
        Assert.That(reversed, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(forward!.Result.Payload!.Text, Is.EqualTo("1234"));
            Assert.That(reversed!.Result.Payload!.Text, Is.EqualTo("1234"));
            Assert.That(forward.OrientationDegrees, Is.EqualTo(20.0).Within(0.05));
            Assert.That(reversed.OrientationDegrees, Is.EqualTo(20.0).Within(0.05));
            Assert.That(reversed.Result.SkewDegrees, Is.EqualTo(20.0).Within(0.05));
            Assert.That(reversed.Result.Location!.Bounds!.Width,
                Is.EqualTo(forward.Result.Location!.Bounds!.Width).Within(0.1));
        });
    }

    [Test]
    public void AngledSamplingRejectsInvalidGeometryAndFanLimits()
    {
        ModuleMatrix matrix = new ModuleMatrix(10, 10);
        LineSegment disjoint = new LineSegment(
            GeometryMath.CreatePoint(-5.0, -5.0),
            GeometryMath.CreatePoint(-2.0, -2.0));
        BarcodeQuadrilateral flat = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(1.0, 1.0),
            GeometryMath.CreatePoint(8.0, 1.0),
            GeometryMath.CreatePoint(8.0, 1.0),
            GeometryMath.CreatePoint(1.0, 1.0));

        Assert.Multiple(() =>
        {
            Assert.That(
                () => AngledScanlineSampler.Sample(matrix, disjoint, false),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DeskewedScanlineFan.Create(matrix, flat, 1.0, 3, false),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DeskewedScanlineFan.Create(matrix, flat, double.NaN, 3, false),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DeskewedScanlineFan.Create(
                    matrix,
                    flat,
                    1.0,
                    ScanlineSampler.MaximumFanLineCount + 1,
                    false),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static BitRow EncodeCode128(byte[] bytes, int scale)
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(bytes, null);
        BitRow row = new BitRow((encoded.Modules.Length + 20) * scale);
        int module = 0;
        while (module < encoded.Modules.Length)
        {
            if (encoded.Modules[module] != 0)
            {
                int pixel = 0;
                while (pixel < scale)
                {
                    row.SetBit(((module + 10) * scale) + pixel, true);
                    pixel = pixel + 1;
                }
            }

            module = module + 1;
        }

        return row;
    }

    static RotatedBarcodeFixture CreateRotatedBarcode(
        BitRow expected,
        BitRow conflicting,
        int barHeight,
        double angleDegrees,
        bool conflictCenter)
    {
        double radians = angleDegrees * global::System.Math.PI / 180.0;
        double cosine = global::System.Math.Cos(radians);
        double sine = global::System.Math.Sin(radians);
        double originX = barHeight * sine + 10.0;
        double originY = 10.0;
        int width = (int)global::System.Math.Ceiling(
            originX + ((expected.Length - 1) * cosine) + 12.0);
        int height = (int)global::System.Math.Ceiling(
            originY + ((expected.Length - 1) * sine) + ((barHeight - 1) * cosine) + 12.0);
        ModuleMatrix matrix = new ModuleMatrix(width, height);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                double deltaX = x - originX;
                double deltaY = y - originY;
                double sourceX = (deltaX * cosine) + (deltaY * sine);
                double sourceY = (-deltaX * sine) + (deltaY * cosine);
                int sourceColumn = (int)global::System.Math.Floor(sourceX + 0.5);
                bool inside = sourceColumn >= 0 && sourceColumn < expected.Length &&
                    sourceY >= -0.5 && sourceY <= barHeight - 0.5;
                if (inside)
                {
                    BitRow source = conflictCenter &&
                        global::System.Math.Abs(sourceY - ((barHeight - 1) * 0.5)) < 2.0
                        ? conflicting
                        : expected;
                    if (source.GetBit(sourceColumn))
                    {
                        matrix.SetModule(x, y, true);
                    }
                }

                x = x + 1;
            }

            y = y + 1;
        }

        BarcodePoint topLeft = TransformPoint(0.0, 0.0, originX, originY, cosine, sine);
        BarcodePoint topRight = TransformPoint(expected.Length - 1.0, 0.0, originX, originY, cosine, sine);
        BarcodePoint bottomRight = TransformPoint(
            expected.Length - 1.0,
            barHeight - 1.0,
            originX,
            originY,
            cosine,
            sine);
        BarcodePoint bottomLeft = TransformPoint(
            0.0,
            barHeight - 1.0,
            originX,
            originY,
            cosine,
            sine);
        RotatedBarcodeFixture fixture = new RotatedBarcodeFixture();
        fixture.Matrix = matrix;
        fixture.Region = GeometryMath.CreateQuadrilateral(
            topLeft,
            topRight,
            bottomRight,
            bottomLeft);
        return fixture;
    }

    static BarcodePoint TransformPoint(
        double sourceX,
        double sourceY,
        double originX,
        double originY,
        double cosine,
        double sine)
    {
        return GeometryMath.CreatePoint(
            originX + (sourceX * cosine) - (sourceY * sine),
            originY + (sourceX * sine) + (sourceY * cosine));
    }
}

internal class RotatedBarcodeFixture
{
    internal ModuleMatrix Matrix = new ModuleMatrix(1, 1);
    internal BarcodeQuadrilateral Region = new BarcodeQuadrilateral();
}
