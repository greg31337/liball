using raw;

namespace raw.Tests;

public class OpticalBlackTests
{
    [Test]
    public void RepeatingPerChannelEstimateFeedsPreprocessing()
    {
        RawImageInfo info = RgbInfo(4, 2, 10);
        double[] expected = new double[]
        {
            10, 20, 30, 11, 21, 31,
            12, 22, 32, 13, 23, 33
        };
        uint[] samples = new uint[24];
        int y = 0;
        while (y < info.Height)
        {
            int x = 0;
            while (x < info.Width)
            {
                int channel = 0;
                while (channel < 3)
                {
                    int cell = ((y & 1) * 2 + (x & 1)) * 3 + channel;
                    samples[(y * info.Width + x) * 3 + channel] = (uint)expected[cell];
                    channel++;
                }
                x++;
            }
            y++;
        }
        RawStoredImage stored = new RawStoredImage(info, samples, null, null, 12, true, null);
        RawNormalizationOptions options = new RawNormalizationOptions();
        options.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 0, 0, 4, 2) };
        options.OpticalBlackRepeatWidth = 2;
        options.OpticalBlackRepeatHeight = 2;
        options.UseOpticalBlack = true;
        options.WhiteLevels = new double[] { 100, 100, 100 };

        RawBlackLevel estimate = RawRenderer.EstimateBlackLevel(stored, options);
        RawSensorImage normalized = RawRenderer.Preprocess(stored, options);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.RepeatWidth, Is.EqualTo(2));
            Assert.That(estimate.RepeatHeight, Is.EqualTo(2));
            Assert.That(estimate.ChannelCount, Is.EqualTo(3));
            Assert.That(estimate.Values, Is.EqualTo(expected));
            Assert.That(estimate.RowDeltas, Is.Empty);
            Assert.That(estimate.ColumnDeltas, Is.Empty);
            Assert.That((normalized.BlackLevel ?? throw new System.InvalidOperationException("A required value is unavailable.")).Values, Is.EqualTo(expected));
            Assert.That(normalized.FloatingSamples, Is.All.Zero);
            Assert.That((normalized.ClippingMask ?? throw new System.InvalidOperationException("A required value is unavailable.")).BlackClipped, Is.All.EqualTo(1));
            Assert.That(normalized.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void RowAndColumnResidualsAreEstimatedWhenEveryAxisCoordinateIsCovered()
    {
        RawImageInfo rowInfo = GrayInfo(3, 4, 10, null);
        uint[] rowSamples = new uint[]
        {
            97, 500, 500,
            99, 500, 500,
            101, 500, 500,
            103, 500, 500
        };
        RawStoredImage rows = new RawStoredImage(rowInfo, rowSamples, null, null, 3, true, null);
        RawNormalizationOptions rowOptions = new RawNormalizationOptions();
        rowOptions.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 0, 0, 1, 4) };
        rowOptions.EstimateBlackRowDeltas = true;

        RawBlackLevel rowEstimate = RawRenderer.EstimateBlackLevel(rows, rowOptions);

        RawImageInfo columnInfo = GrayInfo(4, 2, 10, null);
        uint[] columnSamples = new uint[] { 96, 99, 101, 104, 500, 500, 500, 500 };
        RawStoredImage columns = new RawStoredImage(columnInfo, columnSamples, null, null, 4, true, null);
        RawNormalizationOptions columnOptions = new RawNormalizationOptions();
        columnOptions.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 0, 0, 4, 1) };
        columnOptions.EstimateBlackColumnDeltas = true;

        RawBlackLevel columnEstimate = RawRenderer.EstimateBlackLevel(columns, columnOptions);

        Assert.Multiple(() =>
        {
            Assert.That(rowEstimate.Values, Is.EqualTo(new double[] { 100 }));
            Assert.That(rowEstimate.RowDeltas, Is.EqualTo(new double[] { -3, -1, 1, 3 }));
            Assert.That(rowEstimate.ColumnDeltas, Is.Empty);
            Assert.That(rowEstimate.GetValue(2, 0, 0), Is.EqualTo(97));
            Assert.That(rowEstimate.GetValue(2, 3, 0), Is.EqualTo(103));
            Assert.That(columnEstimate.Values, Is.EqualTo(new double[] { 100 }));
            Assert.That(columnEstimate.RowDeltas, Is.Empty);
            Assert.That(columnEstimate.ColumnDeltas, Is.EqualTo(new double[] { -4, -1, 1, 4 }));
            Assert.That(columnEstimate.GetValue(0, 1, 0), Is.EqualTo(96));
            Assert.That(columnEstimate.GetValue(3, 1, 0), Is.EqualTo(104));
            Assert.That(rowEstimate.Validate(3, 4, 1).IsValid(), Is.True);
            Assert.That(columnEstimate.Validate(4, 2, 1).IsValid(), Is.True);
        });
    }

    [Test]
    public void OpticalEstimateUsesTheAppliedLinearizationDomain()
    {
        RawImageInfo info = GrayInfo(2, 1, 2, null);
        RawStoredImage stored = new RawStoredImage(info, new uint[] { 0, 1 }, null, null, 2, true, null);
        RawNormalizationOptions options = new RawNormalizationOptions();
        options.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 0, 0, 2, 1) };
        options.LinearizationTable = new ushort[] { 100, 200 };

        RawBlackLevel estimate = RawRenderer.EstimateBlackLevel(stored, options);

        Assert.That(estimate.Values, Is.EqualTo(new double[] { 150 }));
    }

    [Test]
    public void SuppliedRepeatingRowAndColumnCalibrationIsAppliedAtEveryCoordinate()
    {
        RawImageInfo info = GrayInfo(2, 2, 8, null);
        RawStoredImage stored = new RawStoredImage(info, new uint[] { 14, 25, 15, 26 }, null, null, 2, true, null);
        RawNormalizationOptions options = new RawNormalizationOptions();
        options.BlackLevel = new RawBlackLevel(2, 1, 1, new double[] { 10, 20 }, new double[] { 1, 2 }, new double[] { 3, 4 });
        options.WhiteLevels = new double[] { 100 };

        RawSensorImage result = RawRenderer.Preprocess(stored, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.All.Zero);
            Assert.That((result.ClippingMask ?? throw new System.InvalidOperationException("A required value is unavailable.")).BlackClipped, Is.All.EqualTo(1));
            Assert.That((result.BlackLevel ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(0, 0, 0), Is.EqualTo(14));
            Assert.That(result.BlackLevel.GetValue(1, 1, 0), Is.EqualTo(26));
        });
    }

    [Test]
    public void MaskValidationRejectsOverlapActivePixelsDuplicateCoverageAndMissingRepeatCells()
    {
        RawArea active = new RawArea(RawAreaKind.Active, 1, 1, 2, 2);
        RawImageInfo info = GrayInfo(4, 4, 10, active);
        RawStoredImage stored = new RawStoredImage(info, Enumerable.Repeat<uint>(100, 16).ToArray(), null, null, 4, true, null);
        RawNormalizationOptions activeOverlap = new RawNormalizationOptions();
        activeOverlap.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 1, 1, 1, 1) };
        RawNormalizationOptions duplicate = new RawNormalizationOptions();
        duplicate.MaskedAreas = new RawArea[]
        {
            new RawArea(RawAreaKind.OpticalBlack, 0, 0, 4, 1),
            new RawArea(RawAreaKind.Masked, 0, 0, 1, 4)
        };
        RawNormalizationOptions missingCell = new RawNormalizationOptions();
        missingCell.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 0, 0, 4, 1) };
        missingCell.OpticalBlackRepeatWidth = 2;
        missingCell.OpticalBlackRepeatHeight = 2;
        RawNormalizationOptions missingOptical = new RawNormalizationOptions();
        missingOptical.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.Masked, 0, 0, 4, 1) };
        RawNormalizationOptions ambiguous = new RawNormalizationOptions();
        ambiguous.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 0, 0, 4, 1) };
        ambiguous.UseOpticalBlack = true;
        ambiguous.BlackLevel = RawBlackLevel.CreateScalar(100, 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => RawRenderer.Preprocess(stored, activeOverlap));
            Assert.Throws<RawFormatException>(() => RawRenderer.Preprocess(stored, duplicate));
            Assert.Throws<RawFormatException>(() => RawRenderer.EstimateBlackLevel(stored, missingCell));
            Assert.Throws<RawFormatException>(() => RawRenderer.EstimateBlackLevel(stored, missingOptical));
            Assert.Throws<RawFormatException>(() => RawRenderer.Preprocess(stored, ambiguous));
        });
    }

    private static RawImageInfo RgbInfo(int width, int height, int bitDepth)
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        RawPlaneInfo[] planes = new RawPlaneInfo[]
        {
            new RawPlaneInfo(0, "R", RawChannelKind.Red, width, height, bitDepth, width),
            new RawPlaneInfo(1, "G", RawChannelKind.Green, width, height, bitDepth, width),
            new RawPlaneInfo(2, "B", RawChannelKind.Blue, width, height, bitDepth, width)
        };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, bitDepth, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.Rgb, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }

    private static RawImageInfo GrayInfo(int width, int height, int bitDepth, RawArea? active)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, width, height, bitDepth, width) };
        RawArea? crop = active == null ? null : new RawArea(RawAreaKind.DefaultCrop, active.X, active.Y, active.Width, active.Height);
        return new RawImageInfo(0, RawImageKind.Raw, width, height, bitDepth, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), active, crop, null);
    }
}
