using System.Text;
using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class LegacyModel1DecoderTests
{
    [TestCase("matrices/legacy-model1-v02-m.txt")]
    [TestCase("damaged/legacy-model1-v02-m-format-3.txt")]
    public void DecodesIndependentReferenceAndReportsLegacyMetadata(string file)
    {
        string[] rows = File.ReadAllLines(GoldenPath(file));
        QrMatrix matrix = new QrMatrix { Width = rows.Length, Height = rows.Length,
            Values = string.Concat(rows).Select(c => c == '1' ? QrModuleValue.Dark : QrModuleValue.Light).ToArray() };
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(GoldenPath("vectors.json")));
        byte[] expected = document.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(v => v.GetProperty("id").GetString() == "legacy-model1-v02-m")
            .GetProperty("full_codewords").EnumerateArray().Select(v => v.GetByte()).ToArray();
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        MatrixCodewordResult words = MatrixCodewordReader.Read(matrix, QrSymbolKind.LegacyModel1, new QrDecodeOptions());
        Assert.That(words.Success, Is.True);
        Assert.That(words.ExtractedCodewords, Is.EqualTo(expected));
        QrDecodeResult result = LegacyModel1Decoder.Decode(matrix, new QrDecodeOptions());
        Assert.That(result.Text, Is.EqualTo("QR Code Model 1 "));
        Assert.That(result.SymbologyIdentifier, Is.EqualTo("]Q0"));
        Assert.That(result.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes("QR Code Model 1 ")));
        Assert.That(result.Symbol!.SymbolKind, Is.EqualTo(QrSymbolKind.LegacyModel1));
        Assert.That(result.Symbol.Version, Is.EqualTo(2));
        Assert.That(result.Symbol.Width, Is.EqualTo(25));
        Assert.That(result.Symbol.Height, Is.EqualTo(25));
        Assert.That(result.Symbol.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Medium));
        Assert.That((int)result.Symbol.MaskPattern, Is.EqualTo(5));
        Assert.That(result.Symbol.Codewords, Is.EqualTo(expected));
        Assert.That(result.Symbol.DataCodewordCount, Is.EqualTo(30));
        Assert.That(result.Symbol.ErrorCorrectionCodewordCount, Is.EqualTo(16));
        Assert.That(result.Symbol.TotalCodewordCount, Is.EqualTo(46));
        Assert.That(result.Symbol.DataBitCapacity, Is.EqualTo(236));
        Assert.That(result.Symbol.UsedDataBitCount, Is.EqualTo(140));
        Assert.That(result.Symbol.UnusedDataBitCount, Is.EqualTo(96));
        Assert.That(result.CorrectedErrorCount, Is.Zero);
        Assert.That(result.IsConformant, Is.False);
        Assert.That(result.Segments.Single().Mode, Is.EqualTo(QrEncodingMode.Byte));
        Assert.That(matrix.Values, Is.EqualTo(before));
    }

    static IEnumerable<TestCaseData> Configurations()
    {
        for (int version = 1; version <= 14; version++)
            for (int level = 0; level < 4; level++) yield return new TestCaseData(version, level);
    }

    [TestCaseSource(nameof(Configurations))]
    public void DecodesEveryVersionLevelAndMaskWithBothExtensionAppearances(int version, int level)
    {
        QrSegment segment = new QrSegment { Mode = QrEncodingMode.Numeric, Text = "123", CharacterCount = 3 };
        byte[] data = CodewordFinalizer.CreateDataCodewords([segment], QrSymbolKind.LegacyModel1, version, level, false);
        byte[] codewords = BlockInterleaver.CreateLegacyModel1Codewords(data, version, level);
        for (int mask = 0; mask < 8; mask++)
            foreach (bool early in new[] { false, true })
            {
                QrMatrix matrix = Build(codewords, version, level, mask, early);
                QrDecodeResult result = LegacyModel1Decoder.Decode(matrix, new QrDecodeOptions());
                Assert.That(result.Text, Is.EqualTo("123"));
                Assert.That(result.Symbol!.Codewords, Is.EqualTo(codewords));
                Assert.That(result.Symbol.DataBitCapacity, Is.EqualTo(data.Length * 8 - 4));
                Assert.That(result.Symbol.UsedDataBitCount, Is.EqualTo(version < 10 ? 24 : 26));
                Assert.That(result.CorrectedErrorCount, Is.Zero);
            }
    }

    [TestCase(1, 0)]
    [TestCase(7, 2)]
    [TestCase(14, 3)]
    public void RepairsDataAndParityErrorsInEverySequentialBlock(int version, int level)
    {
        byte[] original = Codewords(version, level);
        byte[] damaged = (byte[])original.Clone();
        int blocks = LegacyModel1Table.GetErrorCorrectionBlockCount(version, level);
        for (int block = 0; block < blocks; block++)
        {
            damaged[LegacyModel1Table.GetDataBlockStart(version, level, block)] ^= 1;
            damaged[LegacyModel1Table.GetErrorCorrectionBlockStart(version, level, block)] ^= 0x83;
        }
        QrDecodeResult result = LegacyModel1Decoder.Decode(Build(damaged, version, level, 7, false), new QrDecodeOptions());
        Assert.That(result.Text, Is.EqualTo("123"));
        Assert.That(result.Symbol!.Codewords, Is.EqualTo(original));
        Assert.That(result.CorrectedErrorCount, Is.EqualTo(blocks * 2));
        Assert.That(result.CorrectedErasureCount, Is.Zero);
    }

    [Test]
    public void RejectsUncorrectablePayloadAndRecoversUnknownModules()
    {
        byte[] damaged = Codewords(1, 0);
        for (int index = 1; index < 15; index++) damaged[index] ^= (byte)(index + 0x80);
        QrMatrix matrix = Build(damaged, 1, 0, 0, false);
        MatrixCodewordResult result = LegacyModel1Decoder.ReadCodewords(matrix, new QrDecodeOptions());
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(result.Success, Is.False);
        Assert.That(result.DataCodewords, Is.Empty);
        Assert.Throws<QrDecodeException>(() => LegacyModel1Decoder.Decode(matrix, new QrDecodeOptions()));
        matrix = Build(Codewords(1, 0), 1, 0, 0, false);
        matrix.Values[LegacyModel1Placement.CreateOffsets(1)[8]] = QrModuleValue.Unknown;
        result = LegacyModel1Decoder.ReadCodewords(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErasurePositions, Is.EqualTo(new[] { 1 }));
        Assert.That(result.UnknownPayloadModuleCount, Is.EqualTo(1));
    }

    [Test]
    public void RejectsNonzeroRemainderCodewords()
    {
        foreach (int version in Enumerable.Range(1, 14))
            foreach (int level in Enumerable.Range(0, 4))
            {
                if (LegacyModel1Table.GetRemainderCodewordCount(version, level) == 0) continue;
                byte[] words = Codewords(version, level);
                words[^1] = 1;
                MatrixCodewordResult result = LegacyModel1Decoder.ReadCodewords(Build(words, version, level, 4, false), new QrDecodeOptions());
                Assert.That(result.Success, Is.False);
                Assert.That(result.InvalidRemainderBitCount, Is.EqualTo(1));
                Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            }
    }

    [Test]
    public void EnforcesProfileGeometryFormatAndResourceLimits()
    {
        QrMatrix matrix = Build(Codewords(2, 1), 2, 1, 5, false);
        Assert.Throws<QrUnsupportedFeatureException>(() => LegacyModel1Decoder.Decode(matrix, new QrDecodeOptions { AllowLegacyModel1 = false }));
        Assert.Throws<QrResourceLimitException>(() => LegacyModel1Decoder.Decode(matrix, new QrDecodeOptions { MaximumAllocationBytes = 1024 }));
        Assert.Throws<QrInputException>(() => LegacyModel1Decoder.Decode(null!, new QrDecodeOptions()));
        QrMatrix invalid = new QrMatrix { Width = 22, Height = 22, Values = new QrModuleValue[22 * 22] };
        Array.Fill(invalid.Values, QrModuleValue.Light);
        Assert.That(LegacyModel1Decoder.ReadCodewords(invalid, new QrDecodeOptions()).FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
        WriteFormat(matrix, 2, 0, 0, true);
        Assert.That(LegacyModel1Decoder.ReadCodewords(matrix, new QrDecodeOptions()).FailureReason, Is.EqualTo(QrFailureReason.InvalidFormat));
        matrix = Build(Codewords(2, 1), 2, 1, 5, false);
        foreach ((int x, int y) in new[] { (0, 0), (18, 0), (0, 18) })
            for (int row = 0; row < 7; row++)
                for (int col = 0; col < 7; col++) matrix.Values[(y + row) * matrix.Width + x + col] = QrModuleValue.Light;
        Assert.That(LegacyModel1Decoder.ReadCodewords(matrix, new QrDecodeOptions()).FailureReason, Is.EqualTo(QrFailureReason.InvalidGeometry));
    }

    [Test]
    public void ReadsSymbolWithAsymmetricQuietZoneAndMissingPrimaryFormat()
    {
        QrMatrix original = Build(Codewords(2, 1), 2, 1, 5, false);
        QrMatrix matrix = new QrMatrix { Width = 32, Height = 30, QuietZoneLeft = 3, QuietZoneRight = 4,
            QuietZoneTop = 1, QuietZoneBottom = 4, Values = new QrModuleValue[32 * 30] };
        Array.Fill(matrix.Values, QrModuleValue.Light);
        for (int row = 0; row < 25; row++)
            Array.Copy(original.Values, row * 25, matrix.Values, (row + 1) * 32 + 3, 25);
        for (int bit = 0; bit < 15; bit++)
            matrix.Values[(LegacyModel1Table.GetFormatPrimaryRow(bit) + 1) * 32 + LegacyModel1Table.GetFormatPrimaryColumn(bit) + 3] = QrModuleValue.Unknown;
        QrDecodeResult result = LegacyModel1Decoder.Decode(matrix, new QrDecodeOptions());
        Assert.That(result.Text, Is.EqualTo("123"));
        Assert.That(result.Symbol!.Width, Is.EqualTo(25));
        Assert.That(result.Symbol.Height, Is.EqualTo(25));
    }

    [TestCase(3)]
    [TestCase(5)]
    [TestCase(7)]
    [TestCase(9)]
    [TestCase(13)]
    public void RejectsModernControlModesEvenInLenientMode(int indicator)
    {
        byte[] data = new byte[19];
        data[0] = (byte)indicator;
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => PayloadParser.Parse(data, 148,
            QrSymbolKind.LegacyModel1, 1, new QrDecodeOptions { Strict = false, EnableHanziCompatibility = true }))!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.InvalidMode));
    }

    [Test]
    public void ValidatesLeadingNibbleBitLengthAndLegacyPadding()
    {
        QrSegment segment = new QrSegment { Mode = QrEncodingMode.Numeric, Text = "123", CharacterCount = 3 };
        byte[] data = CodewordFinalizer.CreateDataCodewords([segment], QrSymbolKind.LegacyModel1, 1, 0, false);
        int bits = data.Length * 8 - 4;
        data[^1] ^= 1;
        Assert.Throws<QrDecodeException>(() => PayloadParser.Parse(data, bits, QrSymbolKind.LegacyModel1, 1, new QrDecodeOptions()));
        Assert.That(PayloadParser.Parse(data, bits, QrSymbolKind.LegacyModel1, 1, new QrDecodeOptions { Strict = false }).Segments.Single().Text, Is.EqualTo("123"));
        data[0] |= 0x10;
        Assert.Throws<QrDecodeException>(() => PayloadParser.Parse(data, bits, QrSymbolKind.LegacyModel1, 1, new QrDecodeOptions { Strict = false }));
        foreach (int invalid in new[] { 0, 8, bits + 1, int.MaxValue })
            Assert.Throws<QrInputException>(() => PayloadParser.Parse(data, invalid, QrSymbolKind.LegacyModel1, 1, new QrDecodeOptions()));
    }

    static byte[] Codewords(int version, int level)
    {
        byte[] data = CodewordFinalizer.CreateDataCodewords(
            [new QrSegment { Mode = QrEncodingMode.Numeric, Text = "123", CharacterCount = 3 }], QrSymbolKind.LegacyModel1, version, level, false);
        return BlockInterleaver.CreateLegacyModel1Codewords(data, version, level);
    }

    internal static QrMatrix Build(byte[] codewords, int version, int level, int mask, bool early)
    {
        QrMatrix matrix = LegacyPatterns(version, early);
        int[] offsets = LegacyModel1Placement.CreateOffsets(version);
        Assert.That(offsets.Take(4), Is.All.EqualTo(-1));
        Assert.That(offsets.Skip(4).Distinct().Count(), Is.EqualTo(codewords.Length * 8 - 4));
        for (int bit = 4; bit < offsets.Length; bit++)
        {
            int index = offsets[bit];
            bool dark = ((codewords[bit / 8] >> (7 - bit % 8)) & 1) != 0;
            dark ^= Model2MaskTable.IsInverted(mask, index / matrix.Width, index % matrix.Width);
            matrix.Values[index] = dark ? QrModuleValue.Dark : QrModuleValue.Light;
        }
        WriteFormat(matrix, version, level, mask, false);
        WriteFormat(matrix, version, level, mask, true);
        return matrix;
    }

    static void WriteFormat(QrMatrix matrix, int version, int level, int mask, bool secondary)
    {
        int format = LegacyModel1Table.GetFormatCode(level, mask);
        for (int bit = 0; bit < 15; bit++)
        {
            int row = secondary ? LegacyModel1Table.GetFormatSecondaryRow(version, bit) : LegacyModel1Table.GetFormatPrimaryRow(bit);
            int col = secondary ? LegacyModel1Table.GetFormatSecondaryColumn(version, bit) : LegacyModel1Table.GetFormatPrimaryColumn(bit);
            matrix.Values[row * matrix.Width + col] = (format & (1 << bit)) != 0 ? QrModuleValue.Dark : QrModuleValue.Light;
        }
    }

    static string GoldenPath(string relative)
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../tests/Fixtures/Golden", relative));
    }

    static QrMatrix LegacyPatterns(int version, bool early)
    {
        int size = 17 + version * 4;
        QrMatrix matrix = new QrMatrix { Width = size, Height = size, Values = new QrModuleValue[size * size] };
        Array.Fill(matrix.Values, QrModuleValue.Light);
        foreach ((int x, int y) in new[] { (0, 0), (size - 7, 0), (0, size - 7) })
            for (int dy = 0; dy < 7; dy++)
                for (int dx = 0; dx < 7; dx++)
                    if (dx == 0 || dx == 6 || dy == 0 || dy == 6 || dx >= 2 && dx <= 4 && dy >= 2 && dy <= 4)
                        matrix.Values[(y + dy) * size + x + dx] = QrModuleValue.Dark;
        for (int index = 8; index < size - 8; index += 2)
        {
            matrix.Values[6 * size + index] = QrModuleValue.Dark;
            matrix.Values[index * size + 6] = QrModuleValue.Dark;
        }
        matrix.Values[(size - 8) * size + 8] = QrModuleValue.Dark;
        matrix.Values[size * size - 1] = QrModuleValue.Dark;
        for (int block = 0; block < version / 2; block++)
        {
            int start = (version % 2 == 0 ? 13 : 17) + block * 8;
            for (int offset = 0; offset < 4; offset++)
            {
                matrix.Values[(size - 1) * size + start + offset] = QrModuleValue.Dark;
                matrix.Values[(start + offset) * size + size - 1] = QrModuleValue.Dark;
                if (early && (offset == 0 || offset == 3))
                {
                    matrix.Values[(size - 2) * size + start + offset] = QrModuleValue.Dark;
                    matrix.Values[(start + offset) * size + size - 2] = QrModuleValue.Dark;
                }
            }
        }
        return matrix;
    }
}
