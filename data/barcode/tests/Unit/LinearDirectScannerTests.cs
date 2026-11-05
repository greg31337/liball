namespace barcode.Tests;

public class LinearDirectScannerTests
{
    [Test]
    public void FanSamplesCenterOutAndReturnsIndependentRows()
    {
        ModuleMatrix matrix = new ModuleMatrix(4, 9);
        int y = 0;
        while (y < matrix.Height)
        {
            matrix.SetModule(y % matrix.Width, y, true);
            y = y + 1;
        }

        LinearScanlineSample[] samples = ScanlineSampler.CreateFan(matrix, false, 4, 1, 6);
        matrix.ClearModules();

        Assert.Multiple(() =>
        {
            Assert.That(samples.Select(sample => sample.Coordinate), Is.EqualTo(new int[] { 4, 3, 5, 2, 6, 1 }));
            Assert.That(samples.Select(sample => sample.IsVertical), Is.All.False);
            Assert.That(samples[0].Row.GetBit(0), Is.True);
            Assert.That(samples[1].Row.GetBit(3), Is.True);
            Assert.That(samples[5].Row.GetBit(1), Is.True);
        });
    }

    [Test]
    public void HorizontalScanUsesExactPayloadMajorityAndReportsVotedBand()
    {
        BitRow expected = EncodeCode128(new byte[] { 65, 66, 67 });
        BitRow conflicting = EncodeCode128(new byte[] { 88, 89, 90 });
        ModuleMatrix matrix = new ModuleMatrix(expected.Length, 5);
        matrix.SetRow(0, expected.CopyBits());
        matrix.SetRow(1, conflicting.CopyBits());
        matrix.SetRow(2, expected.CopyBits());
        matrix.SetRow(3, conflicting.CopyBits());
        matrix.SetRow(4, expected.CopyBits());

        LinearScanlineConsensus? consensus = LinearDirectScanner.Scan(
            matrix,
            BarcodeFormat.Automatic,
            false,
            2,
            1,
            5,
            3);

        Assert.That(consensus, Is.Not.Null);
        Assert.That(consensus!.Result.Location, Is.Not.Null);
        Assert.That(consensus.Result.Location!.Bounds, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(consensus.Result.Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(consensus.Result.Payload!.RawBytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(consensus.VoteCount, Is.EqualTo(3));
            Assert.That(consensus.ObservationCount, Is.EqualTo(5));
            Assert.That(consensus.MinimumCoordinate, Is.EqualTo(0));
            Assert.That(consensus.MaximumCoordinate, Is.EqualTo(4));
            Assert.That(consensus.Result.Location.Bounds!.Y, Is.EqualTo(0.0));
            Assert.That(consensus.Result.Location.Bounds.Height, Is.EqualTo(5.0));
            Assert.That(consensus.Result.Metadata!.Confidence, Is.EqualTo(0.6));
        });
    }

    [Test]
    public void VerticalScanRestoresVerticalOrientationAndGeometry()
    {
        BitRow encoded = EncodeCode128(new byte[] { 49, 50, 51, 52 });
        ModuleMatrix matrix = new ModuleMatrix(5, encoded.Length);
        int x = 0;
        while (x < matrix.Width)
        {
            matrix.SetColumn(x, encoded.CopyBits());
            x = x + 1;
        }

        LinearScanlineConsensus? consensus = LinearDirectScanner.Scan(
            matrix,
            BarcodeFormat.Code128,
            true,
            2,
            1,
            5,
            3);

        Assert.That(consensus, Is.Not.Null);
        Assert.That(consensus!.Result.Location, Is.Not.Null);
        Assert.That(consensus.Result.Location!.Bounds, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(consensus.IsVertical, Is.True);
            Assert.That(consensus.VoteCount, Is.EqualTo(5));
            Assert.That(consensus.Result.Payload!.Text, Is.EqualTo("1234"));
            Assert.That(consensus.Result.Rotation, Is.EqualTo(BarcodeRotation.Clockwise90));
            Assert.That(consensus.Result.OrientationDegrees, Is.EqualTo(90.0));
            Assert.That(consensus.Result.Location.Bounds!.X, Is.EqualTo(0.0));
            Assert.That(consensus.Result.Location.Bounds.Width, Is.EqualTo(5.0));
            Assert.That(consensus.Result.Location.Bounds.Y, Is.EqualTo(10.0));
        });
    }

    [TestCase("12", BarcodeFormat.EanUpcAddon2)]
    [TestCase("51234", BarcodeFormat.EanUpcAddon5)]
    public void StandaloneEanUpcAddOnsParticipateInDirectScanning(
        string text,
        BarcodeFormat format)
    {
        EanUpcEncodedData encoded = EanUpcAddon.Encode(text);
        BitRow row = CreateScaledRow(encoded.Modules, 7, 5, 4);
        ModuleMatrix matrix = new ModuleMatrix(row.Length, 5);
        int y = 0;
        while (y < matrix.Height)
        {
            matrix.SetRow(y, row.CopyBits());
            y++;
        }

        LinearScanlineConsensus? consensus = LinearDirectScanner.Scan(
            matrix,
            format,
            false,
            2,
            1,
            5,
            3);

        Assert.That(consensus, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(consensus!.Result.Format, Is.EqualTo(format));
            Assert.That(consensus.Result.Payload!.Text, Is.EqualTo(text));
            Assert.That(consensus.VoteCount, Is.EqualTo(5));
            Assert.That(consensus.AverageModuleSize, Is.EqualTo(4.0));
        });
    }

    [Test]
    public void ScanRequiresEnoughAgreeingRowsAndRejectsUnsupportedFormats()
    {
        BitRow encoded = EncodeCode128(new byte[] { 65, 66, 67 });
        ModuleMatrix matrix = new ModuleMatrix(encoded.Length, 3);
        matrix.SetRow(1, encoded.CopyBits());

        Assert.Multiple(() =>
        {
            Assert.That(
                LinearDirectScanner.Scan(matrix, BarcodeFormat.Code128, false, 1, 1, 3, 2),
                Is.Null);
            Assert.That(
                () => LinearDirectScanner.Scan(matrix, BarcodeFormat.DataMatrix, false, 1, 1, 1, 1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ScanlineSampler.CreateFan(matrix, false, 1, 0, 3),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static BitRow EncodeCode128(byte[] bytes)
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(bytes, null);
        BitRow row = new BitRow(encoded.Modules.Length + 20);
        int index = 0;
        while (index < encoded.Modules.Length)
        {
            if (encoded.Modules[index] != 0)
            {
                row.SetBit(index + 10, true);
            }

            index = index + 1;
        }

        return row;
    }

    static BitRow CreateScaledRow(
        byte[] modules,
        int leftQuietZone,
        int rightQuietZone,
        int scale)
    {
        BitRow row = new BitRow(
            (leftQuietZone + modules.Length + rightQuietZone) * scale);
        int module = 0;
        while (module < modules.Length)
        {
            if (modules[module] != 0)
            {
                int repeat = 0;
                while (repeat < scale)
                {
                    row.SetBit(
                        (leftQuietZone + module) * scale + repeat,
                        true);
                    repeat++;
                }
            }

            module++;
        }

        return row;
    }
}
