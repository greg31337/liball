using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MicroMatrixBuilderTests
{
    [TestCase("micro-m1-l", "micro-m1-l.txt", 1, MicroVersionTable.DetectionOnly, QrMaskPattern.Zero)]
    [TestCase("micro-m2-l", "micro-m2-l.txt", 2, EccBlockTable.Low, QrMaskPattern.One)]
    [TestCase("micro-m3-l", "micro-m3-l.txt", 3, EccBlockTable.Low, QrMaskPattern.Two)]
    [TestCase("micro-m4-l", "micro-m4-l.txt", 4, EccBlockTable.Low, QrMaskPattern.Three)]
    public void ForcedConstructionAndExtractionMatchGoldenMatrices(
        string vectorId,
        string matrixFileName,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern mask)
    {
        byte[] expectedCodewords = ReadGoldenCodewords(vectorId);
        string[] expectedRows = LoadGoldenRows(matrixFileName);

        BitMatrixBuffer constructed = MicroMatrixBuilder.CreateSelectedMicroMatrix(
            expectedCodewords,
            version,
            errorCorrectionIndex,
            mask);
        AssertMatrixEquals(expectedRows, constructed);

        BitMatrixBuffer encoded = LoadMatrix(expectedRows);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateMicroReservedMap(version);
        MaskEvaluator.ApplyMicroMask(encoded, reserved, (int)mask);
        byte[] extracted = DataModuleWalker.ExtractMicroCodewords(
            version,
            errorCorrectionIndex,
            encoded);
        Assert.That(extracted, Is.EqualTo(expectedCodewords));
    }

    [TestCase("micro-m1-l", 1, MicroVersionTable.DetectionOnly, QrMaskPattern.Two)]
    [TestCase("micro-m2-l", 2, EccBlockTable.Low, QrMaskPattern.Zero)]
    [TestCase("micro-m3-l", 3, EccBlockTable.Low, QrMaskPattern.Two)]
    [TestCase("micro-m4-l", 4, EccBlockTable.Low, QrMaskPattern.Zero)]
    public void AutomaticSelectionUsesTheHighestScoreAndLowestMaskOnTies(
        string vectorId,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern expected)
    {
        byte[] codewords = ReadGoldenCodewords(vectorId);

        QrMaskPattern selected = MicroMatrixBuilder.SelectMicroMask(
            codewords,
            version,
            errorCorrectionIndex,
            QrMaskPattern.Automatic);

        Assert.That(selected, Is.EqualTo(expected));
    }

    [Test]
    public void M2EqualBestScoresUseTheLowestMask()
    {
        byte[] codewords = ReadGoldenCodewords("micro-m2-l");
        int[] scores = new int[4];
        for (int mask = 0; mask < 4; mask++)
        {
            BitMatrixBuffer matrix = MicroMatrixBuilder.CreateSelectedMicroMatrix(
                codewords,
                2,
                EccBlockTable.Low,
                (QrMaskPattern)mask);
            scores[mask] = MaskEvaluator.CalculateMicroScore(matrix);
        }

        QrMaskPattern selected = MicroMatrixBuilder.SelectMicroMask(
            codewords,
            2,
            EccBlockTable.Low,
            QrMaskPattern.Automatic);

        Assert.Multiple(() =>
        {
            Assert.That(scores, Is.EqualTo(new int[] { 71, 54, 71, 71 }));
            Assert.That(selected, Is.EqualTo(QrMaskPattern.Zero));
        });
    }

    [Test]
    public void ReservedMapsLeaveExactlyTheEncodingRegionForEveryVersion()
    {
        for (int version = MicroVersionTable.MinimumVersion;
            version <= MicroVersionTable.MaximumVersion;
            version++)
        {
            BitMatrixBuffer reserved = FunctionPatternBuilder.CreateMicroReservedMap(version);
            int unreservedCount = 0;
            for (int row = 0; row < reserved.GetHeight(); row++)
            {
                for (int column = 0; column < reserved.GetWidth(); column++)
                {
                    if (!reserved.GetModule(row, column))
                    {
                        unreservedCount++;
                    }
                }
            }

            Assert.That(
                unreservedCount,
                Is.EqualTo(MicroVersionTable.GetTotalEncodingBitCount(version)));
        }
    }

    [Test]
    public void FormatBitsArePlacedLeastSignificantBitFirstForEveryConfiguration()
    {
        AssertFormatConfigurations(1, MicroVersionTable.DetectionOnly);
        AssertFormatConfigurations(2, EccBlockTable.Low);
        AssertFormatConfigurations(2, EccBlockTable.Medium);
        AssertFormatConfigurations(3, EccBlockTable.Low);
        AssertFormatConfigurations(3, EccBlockTable.Medium);
        AssertFormatConfigurations(4, EccBlockTable.Low);
        AssertFormatConfigurations(4, EccBlockTable.Medium);
        AssertFormatConfigurations(4, EccBlockTable.Quartile);
    }

    [Test]
    public void EveryConfigurationRoundTripsItsSplitDataAndEccBitstream()
    {
        AssertRoundTrip(1, MicroVersionTable.DetectionOnly);
        AssertRoundTrip(2, EccBlockTable.Low);
        AssertRoundTrip(2, EccBlockTable.Medium);
        AssertRoundTrip(3, EccBlockTable.Low);
        AssertRoundTrip(3, EccBlockTable.Medium);
        AssertRoundTrip(4, EccBlockTable.Low);
        AssertRoundTrip(4, EccBlockTable.Medium);
        AssertRoundTrip(4, EccBlockTable.Quartile);
    }

    [Test]
    public void EveryMaskTogglesOnlyUnreservedModulesAndIsReversible()
    {
        int version = 4;
        int dimension = MicroVersionTable.GetDimension(version);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateMicroReservedMap(version);
        for (int mask = 0; mask < 4; mask++)
        {
            BitMatrixBuffer matrix = CreateAlternatingMatrix(dimension);
            byte[] original = CopyPackedBytes(matrix);

            MaskEvaluator.ApplyMicroMask(matrix, reserved, mask);
            for (int row = 0; row < dimension; row++)
            {
                for (int column = 0; column < dimension; column++)
                {
                    bool before = (row + column) % 2 == 0;
                    bool invert = !reserved.GetModule(row, column)
                        && MicroMaskTable.IsInverted(mask, row, column);
                    Assert.That(
                        matrix.GetModule(row, column),
                        Is.EqualTo(before != invert));
                }
            }

            MaskEvaluator.ApplyMicroMask(matrix, reserved, mask);
            Assert.That(CopyPackedBytes(matrix), Is.EqualTo(original));
        }
    }

    [Test]
    public void ScoreWeightsTheSmallerEdgeCountBySixteen()
    {
        BitMatrixBuffer matrix = new BitMatrixBuffer(11, 11);
        for (int row = 1; row <= 3; row++)
        {
            matrix.SetModule(row, 10, true);
        }

        for (int column = 1; column <= 5; column++)
        {
            matrix.SetModule(10, column, true);
        }

        Assert.That(MaskEvaluator.CalculateMicroScore(matrix), Is.EqualTo(53));
    }

    [TestCase("micro-m1-l.txt", 69)]
    [TestCase("micro-m2-l.txt", 54)]
    [TestCase("micro-m3-l.txt", 137)]
    [TestCase("micro-m4-l.txt", 104)]
    public void GoldenMatricesHaveExactMicroScores(string fileName, int expected)
    {
        Assert.That(
            MaskEvaluator.CalculateMicroScore(LoadMatrix(LoadGoldenRows(fileName))),
            Is.EqualTo(expected));
    }

    [Test]
    public void RejectsInvalidMasksConfigurationsCountsAndDimensions()
    {
        byte[] m1Codewords = new byte[MicroVersionTable.GetSymbolCharacterCount(1)];
        BitMatrixBuffer m1 = new BitMatrixBuffer(11, 11);
        BitMatrixBuffer wrong = new BitMatrixBuffer(13, 11);

        Assert.Multiple(() =>
        {
            Assert.That(() => MicroMatrixBuilder.SelectMicroMask(
                    m1Codewords,
                    1,
                    MicroVersionTable.DetectionOnly,
                    QrMaskPattern.Four),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroMatrixBuilder.SelectMicroMask(
                    null!,
                    1,
                    MicroVersionTable.DetectionOnly,
                    QrMaskPattern.Automatic),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.PlaceMicroCodewords(
                    new byte[4],
                    1,
                    MicroVersionTable.DetectionOnly,
                    m1),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.PlaceMicroCodewords(
                    m1Codewords,
                    1,
                    MicroVersionTable.DetectionOnly,
                    wrong),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.ExtractMicroCodewords(
                    2,
                    EccBlockTable.Quartile,
                    new BitMatrixBuffer(13, 13)),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => MaskEvaluator.ApplyMicroMask(m1, m1, 4),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => MaskEvaluator.ApplyMicroMask(m1, wrong, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MaskEvaluator.CalculateMicroScore(new BitMatrixBuffer(12, 12)),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static void AssertFormatConfigurations(int version, int errorCorrectionIndex)
    {
        for (int mask = 0; mask < 4; mask++)
        {
            int code = FormatInformationCodec.CreateMicroCode(
                version,
                errorCorrectionIndex,
                mask);
            BitMatrixBuffer matrix = FunctionPatternBuilder.CreateMicroFunctionPatterns(
                version,
                errorCorrectionIndex,
                mask);
            for (int bitIndex = 0; bitIndex <= 7; bitIndex++)
            {
                Assert.That(
                    matrix.GetModule(
                        MicroPlacementTable.GetFormatVerticalRow(bitIndex),
                        MicroPlacementTable.GetFormatVerticalColumn(bitIndex)),
                    Is.EqualTo(((code >> bitIndex) & 1) != 0));
            }

            for (int bitIndex = 7; bitIndex < 15; bitIndex++)
            {
                Assert.That(
                    matrix.GetModule(
                        MicroPlacementTable.GetFormatHorizontalRow(bitIndex),
                        MicroPlacementTable.GetFormatHorizontalColumn(bitIndex)),
                    Is.EqualTo(((code >> bitIndex) & 1) != 0));
            }
        }
    }

    private static void AssertRoundTrip(int version, int errorCorrectionIndex)
    {
        int dataCharacterCount = MicroVersionTable.GetDataSymbolCharacterCount(
            version,
            errorCorrectionIndex);
        int length = dataCharacterCount
            + MicroVersionTable.GetEccCodewordCount(version, errorCorrectionIndex);
        byte[] codewords = new byte[length];
        for (int index = 0; index < codewords.Length; index++)
        {
            codewords[index] = (byte)((index * 137 + version * 41 + errorCorrectionIndex) & 255);
        }

        if (MicroVersionTable.GetFinalDataSymbolBitWidth(version) == 4)
        {
            codewords[dataCharacterCount - 1] = (byte)(codewords[dataCharacterCount - 1] & 0xF0);
        }

        int dimension = MicroVersionTable.GetDimension(version);
        BitMatrixBuffer matrix = new BitMatrixBuffer(dimension, dimension);
        DataModuleWalker.PlaceMicroCodewords(
            codewords,
            version,
            errorCorrectionIndex,
            matrix);

        Assert.That(
            DataModuleWalker.ExtractMicroCodewords(version, errorCorrectionIndex, matrix),
            Is.EqualTo(codewords));
    }

    private static BitMatrixBuffer CreateAlternatingMatrix(int dimension)
    {
        BitMatrixBuffer result = new BitMatrixBuffer(dimension, dimension);
        for (int row = 0; row < dimension; row++)
        {
            for (int column = 0; column < dimension; column++)
            {
                result.SetModule(row, column, (row + column) % 2 == 0);
            }
        }

        return result;
    }

    private static byte[] CopyPackedBytes(BitMatrixBuffer matrix)
    {
        byte[] result = new byte[matrix.GetPackedByteLength()];
        matrix.CopyPackedBytes(result);
        return result;
    }

    private static void AssertMatrixEquals(string[] expected, BitMatrixBuffer actual)
    {
        Assert.That(actual.GetHeight(), Is.EqualTo(expected.Length));
        for (int row = 0; row < expected.Length; row++)
        {
            Assert.That(actual.GetWidth(), Is.EqualTo(expected[row].Length));
            for (int column = 0; column < expected[row].Length; column++)
            {
                Assert.That(
                    actual.GetModule(row, column),
                    Is.EqualTo(expected[row][column] == '1'),
                    $"Module ({row}, {column}) differs from the golden matrix.");
            }
        }
    }

    private static BitMatrixBuffer LoadMatrix(string[] rows)
    {
        BitMatrixBuffer result = new BitMatrixBuffer(rows[0].Length, rows.Length);
        for (int row = 0; row < rows.Length; row++)
        {
            for (int column = 0; column < rows[row].Length; column++)
            {
                result.SetModule(row, column, rows[row][column] == '1');
            }
        }

        return result;
    }

    private static byte[] ReadGoldenCodewords(string vectorId)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(GetGoldenPath(
            "vectors.json")));
        foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
        {
            if (vector.GetProperty("id").GetString() == vectorId)
            {
                JsonElement values = vector.GetProperty("full_codewords");
                byte[] result = new byte[values.GetArrayLength()];
                int index = 0;
                foreach (JsonElement value in values.EnumerateArray())
                {
                    result[index] = (byte)value.GetInt32();
                    index++;
                }

                return result;
            }
        }

        throw new AssertionException($"Golden vector {vectorId} was not found.");
    }

    private static string[] LoadGoldenRows(string fileName)
    {
        return File.ReadAllLines(GetGoldenPath(Path.Combine("matrices", fileName)));
    }

    private static string GetGoldenPath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "..",
            "tests",
            "Fixtures",
            "Golden",
            relativePath));
    }
}
