namespace barcode.Tests;

public class Pdf417ScanningDecoderTests
{
    [Test]
    public void StandardScanlinesRecoverAllThreeClustersIndicatorsAndData()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Standard);
        int rowNumber = 0;
        while (rowNumber < encoded.SymbolPlan.Rows)
        {
            BitRow row = CreateScaledRow(encoded.Symbol, rowNumber, 3, 2, 2);

            Pdf417ScanlineRow decoded = Pdf417ScanningDecoder.DecodeRow(row);
            int codewordStart = rowNumber * encoded.SymbolPlan.Columns;
            int left = Pdf417Layout.CalculateLeftRowIndicator(
                rowNumber,
                encoded.SymbolPlan.Rows,
                encoded.SymbolPlan.Columns,
                encoded.SymbolPlan.ErrorCorrectionLevel);
            int right = Pdf417Layout.CalculateRightRowIndicator(
                rowNumber,
                encoded.SymbolPlan.Rows,
                encoded.SymbolPlan.Columns,
                encoded.SymbolPlan.ErrorCorrectionLevel);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Variant, Is.EqualTo(Pdf417Variant.Standard));
                Assert.That(decoded.ClusterIndex, Is.EqualTo(rowNumber % 3));
                Assert.That(decoded.LeftIndicator, Is.EqualTo(left));
                Assert.That(decoded.RightIndicator, Is.EqualTo(right));
                Assert.That(decoded.DataCodewords, Is.EqualTo(
                    encoded.SymbolCodewords.Skip(codewordStart).Take(encoded.SymbolPlan.Columns)));
                Assert.That(decoded.Codewords, Is.EqualTo(
                    new int[] { left }
                        .Concat(encoded.SymbolCodewords.Skip(codewordStart).Take(encoded.SymbolPlan.Columns))
                        .Concat(new int[] { right })));
                Assert.That(decoded.Buckets, Has.All.EqualTo((rowNumber % 3) * 3));
                Assert.That(decoded.StartPosition, Is.EqualTo(6));
                Assert.That(decoded.EndPosition, Is.EqualTo(6 + (encoded.Symbol.WidthModules * 3)));
                Assert.That(decoded.ModuleSize, Is.EqualTo(3.0).Within(0.000001));
                Assert.That(decoded.AverageVariance, Is.EqualTo(0.0).Within(0.000001));
                Assert.That(decoded.IsReversed, Is.False);
            });
            rowNumber++;
        }
    }

    [Test]
    public void CompactScanlinesRecoverTheTruncatedStopWithoutARightIndicator()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Compact);
        int rowNumber = 0;
        while (rowNumber < encoded.SymbolPlan.Rows)
        {
            BitRow row = CreateScaledRow(encoded.Symbol, rowNumber, 4, 2, 3);

            Pdf417ScanlineRow decoded = Pdf417ScanningDecoder.DecodeRow(row);
            int codewordStart = rowNumber * encoded.SymbolPlan.Columns;
            int left = Pdf417Layout.CalculateLeftRowIndicator(
                rowNumber,
                encoded.SymbolPlan.Rows,
                encoded.SymbolPlan.Columns,
                encoded.SymbolPlan.ErrorCorrectionLevel);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Variant, Is.EqualTo(Pdf417Variant.Compact));
                Assert.That(decoded.ClusterIndex, Is.EqualTo(rowNumber % 3));
                Assert.That(decoded.LeftIndicator, Is.EqualTo(left));
                Assert.That(decoded.RightIndicator, Is.EqualTo(-1));
                Assert.That(decoded.DataCodewords, Is.EqualTo(
                    encoded.SymbolCodewords.Skip(codewordStart).Take(encoded.SymbolPlan.Columns)));
                Assert.That(decoded.Codewords, Has.Length.EqualTo(encoded.SymbolPlan.Columns + 1));
                Assert.That(decoded.StartPosition, Is.EqualTo(8));
                Assert.That(decoded.EndPosition, Is.EqualTo(8 + (encoded.Symbol.WidthModules * 4)));
                Assert.That(decoded.ModuleSize, Is.EqualTo(4.0).Within(0.000001));
            });
            rowNumber++;
        }
    }

    [Test]
    public void ReversedScanlineRestoresLogicalOrderAndSourceCoordinates()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Standard);
        BitRow row = CreateScaledRow(encoded.Symbol, 1, 3, 4, 5);
        int expectedStart = 12;
        int expectedEnd = expectedStart + (encoded.Symbol.WidthModules * 3);
        row.ReverseBits();

        Pdf417ScanlineRow decoded = Pdf417ScanningDecoder.DecodeRow(row);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.IsReversed, Is.True);
            Assert.That(decoded.ClusterIndex, Is.EqualTo(1));
            Assert.That(decoded.DataCodewords, Is.EqualTo(
                encoded.SymbolCodewords.Skip(encoded.SymbolPlan.Columns).Take(encoded.SymbolPlan.Columns)));
            Assert.That(decoded.StartPosition, Is.EqualTo(row.Length - expectedEnd));
            Assert.That(decoded.EndPosition, Is.EqualTo(row.Length - expectedStart));
        });
    }

    [Test]
    public void EveryCodewordPatternRecoversItsValueAndBucketFromPixelRuns()
    {
        int cluster = 0;
        while (cluster < Pdf417CodewordTable.ClusterCount)
        {
            int value = 0;
            while (value < Pdf417CodewordTable.CodewordCount)
            {
                int pattern = Pdf417CodewordTable.GetPattern(cluster, value);
                int[] modules = Pdf417CodewordTable.GetRunWidths(pattern);
                int[] pixels = modules.Select(width => width * 3).ToArray();

                Pdf417ScanlineCodeword? recovered = Pdf417ScanningDecoder.RecoverCodeword(
                    pixels,
                    -1);

                Assert.That(recovered, Is.Not.Null, $"Cluster {cluster}, value {value}");
                Assert.Multiple(() =>
                {
                    Assert.That(recovered!.Value, Is.EqualTo(value));
                    Assert.That(recovered.Pattern, Is.EqualTo(pattern));
                    Assert.That(recovered.ClusterIndex, Is.EqualTo(cluster));
                    Assert.That(recovered.Bucket, Is.EqualTo(cluster * 3));
                    Assert.That(recovered.ModuleBitCounts, Is.EqualTo(modules));
                    Assert.That(recovered.ModuleSize, Is.EqualTo(3.0));
                    Assert.That(recovered.Variance, Is.EqualTo(0.0));
                });
                value++;
            }

            cluster++;
        }
    }

    [Test]
    public void SeventeenModuleSamplingAndNearestMatchingTolerateRunJitter()
    {
        int pattern = Pdf417CodewordTable.GetPattern(1, 464);
        int[] modules = Pdf417CodewordTable.GetRunWidths(pattern);
        int[] observed = new int[modules.Length];
        int index = 0;
        while (index < observed.Length)
        {
            observed[index] = (modules[index] * 4) + (index % 2 == 0 ? 1 : -1);
            index++;
        }

        int[] sampled = Pdf417ScanningDecoder.SampleModuleBitCounts(observed);
        Pdf417ScanlineCodeword? recovered = Pdf417ScanningDecoder.RecoverCodeword(
            observed,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(sampled, Is.EqualTo(modules));
            Assert.That(Pdf417ScanningDecoder.CalculateBucket(sampled), Is.EqualTo(3));
            Assert.That(recovered, Is.Not.Null);
            Assert.That(recovered!.Value, Is.EqualTo(464));
            Assert.That(recovered.Pattern, Is.EqualTo(pattern));
            Assert.That(recovered.Variance, Is.GreaterThan(0.0).And.LessThanOrEqualTo(Pdf417ScanningDecoder.MaximumAverageVariance));
        });
    }

    [Test]
    public void CompleteRowDetectionToleratesBoundedElementWidthJitter()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Standard);
        BitRow row = CreateJitteredRow(encoded.Symbol, 2, 4, 3);

        Pdf417ScanlineRow decoded = Pdf417ScanningDecoder.DecodeRow(row);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Variant, Is.EqualTo(Pdf417Variant.Standard));
            Assert.That(decoded.ClusterIndex, Is.EqualTo(2));
            Assert.That(decoded.DataCodewords, Is.EqualTo(
                encoded.SymbolCodewords.Skip(4).Take(2)));
            Assert.That(decoded.ModuleSize, Is.EqualTo(4.0).Within(0.1));
            Assert.That(decoded.AverageVariance, Is.GreaterThan(0.0));
        });
    }

    [Test]
    public void InvalidQuietZonesClustersBucketsAndGuardsAreRejected()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Standard);
        BitRow noQuietZone = CreateScaledRow(encoded.Symbol, 0, 2, 0, 2);
        BitRow mixedCluster = CreateScaledRow(encoded.Symbol, 0, 2, 2, 2);
        int dataPattern = Pdf417CodewordTable.GetPattern(1, encoded.SymbolCodewords[0]);
        WritePattern(mixedCluster, 4 + (34 * 2), dataPattern, 17, 2);
        BitRow damagedStop = CreateScaledRow(encoded.Symbol, 0, 3, 2, 2);
        int stopStart = 6 + ((encoded.Symbol.WidthModules - 18) * 3);
        damagedStop.SetBit(stopStart + 3, false);
        BitRow empty = new BitRow(80);
        int[] invalidBucket = new int[] { 1, 1, 1, 1, 1, 1, 5, 6 };
        int[] clusterZeroRuns = Pdf417CodewordTable.GetRunWidths(
            Pdf417CodewordTable.GetPattern(0, 100));

        Assert.Multiple(() =>
        {
            Assert.That(Pdf417ScanningDecoder.TryDecodeRow(noQuietZone), Is.Null);
            Assert.That(Pdf417ScanningDecoder.TryDecodeRow(mixedCluster), Is.Null);
            Assert.That(Pdf417ScanningDecoder.TryDecodeRow(damagedStop), Is.Null);
            Assert.That(Pdf417ScanningDecoder.TryDecodeRow(empty), Is.Null);
            Assert.That(
                () => Pdf417ScanningDecoder.DecodeRow(empty),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417ScanningDecoder.TryDecodeRow(null!),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(Pdf417ScanningDecoder.CalculateBucket(invalidBucket), Is.EqualTo(5));
            Assert.That(Pdf417ScanningDecoder.RecoverCodeword(invalidBucket, -1), Is.Null);
            Assert.That(Pdf417ScanningDecoder.RecoverCodeword(clusterZeroRuns, 1), Is.Null);
            Assert.That(
                () => Pdf417ScanningDecoder.CalculateBucket(new int[] { 1, 2 }),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ScanningDecoder.RecoverCodeword(new int[] { 1, 2 }, -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ScanningDecoder.RecoverCodeword(clusterZeroRuns, 3),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static Pdf417EncodingResult CreateEncoded(Pdf417Variant variant)
    {
        byte[] data = Bytes("ABC");
        Pdf417Options options = new Pdf417Options();
        options.Columns = 2;
        options.Rows = 3;
        options.Variant = variant;
        options.CompactionMode = Pdf417CompactionMode.Text;
        options.ErrorCorrectionLevel = 0;
        return Pdf417Encoder.Encode(
            data,
            CreatePayload(data, "ABC"),
            options,
            null);
    }

    static BitRow CreateScaledRow(
        BarcodeSymbol symbol,
        int rowNumber,
        int scale,
        int leftQuietModules,
        int rightQuietModules)
    {
        int length = (leftQuietModules + symbol.WidthModules + rightQuietModules) * scale;
        BitRow row = new BitRow(length);
        int sourceRow = rowNumber * Pdf417Layout.DefaultRowHeightModules;
        int column = 0;
        while (column < symbol.WidthModules)
        {
            if (symbol.Modules![(sourceRow * symbol.WidthModules) + column] != 0)
            {
                int repeat = 0;
                while (repeat < scale)
                {
                    row.SetBit(((leftQuietModules + column) * scale) + repeat, true);
                    repeat++;
                }
            }

            column++;
        }

        return row;
    }

    static BitRow CreateJitteredRow(
        BarcodeSymbol symbol,
        int rowNumber,
        int scale,
        int quietModules)
    {
        BitRow logical = CreateScaledRow(symbol, rowNumber, 1, 0, 0);
        int[] logicalRuns = RunLength.ExtractAll(logical);
        int[] pixelRuns = new int[logicalRuns.Length];
        int index = 0;
        while (index < pixelRuns.Length)
        {
            pixelRuns[index] = (logicalRuns[index] * scale)
                + (index % 2 == 0 ? 1 : -1);
            index++;
        }

        BitRow content = RunLength.ExpandRuns(pixelRuns, true);
        BitRow row = new BitRow(content.Length + (quietModules * scale * 2));
        index = 0;
        while (index < content.Length)
        {
            if (content.GetBit(index))
            {
                row.SetBit((quietModules * scale) + index, true);
            }

            index++;
        }

        return row;
    }

    static void WritePattern(
        BitRow row,
        int start,
        int pattern,
        int moduleCount,
        int scale)
    {
        int module = 0;
        while (module < moduleCount)
        {
            bool dark = ((pattern >> (moduleCount - module - 1)) & 1) != 0;
            int repeat = 0;
            while (repeat < scale)
            {
                row.SetBit(start + (module * scale) + repeat, dark);
                repeat++;
            }

            module++;
        }
    }

    static BarcodePayload CreatePayload(byte[] data, string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = (byte[])data.Clone();
        return payload;
    }

    static byte[] Bytes(string text)
    {
        return global::System.Text.Encoding.Latin1.GetBytes(text);
    }
}
