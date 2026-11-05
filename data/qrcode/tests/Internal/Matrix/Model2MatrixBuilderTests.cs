using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class Model2MatrixBuilderTests
{
    [TestCase("model2-v01-l", 1, EccBlockTable.Low, QrMaskPattern.Seven)]
    [TestCase("model2-v01-l-kanji", 1, EccBlockTable.Low, QrMaskPattern.Three)]
    [TestCase("model2-v09-m", 9, EccBlockTable.Medium, QrMaskPattern.Two)]
    [TestCase("model2-v10-q", 10, EccBlockTable.Quartile, QrMaskPattern.Six)]
    [TestCase("model2-v26-h", 26, EccBlockTable.High, QrMaskPattern.Four)]
    [TestCase("model2-v27-l", 27, EccBlockTable.Low, QrMaskPattern.Four)]
    [TestCase("model2-v40-h", 40, EccBlockTable.High, QrMaskPattern.Four)]
    public void AutomaticSelectionChoosesTheIndependentMinimumPenaltyMask(
        string vectorId,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern expected)
    {
        byte[] codewords = ReadGoldenCodewords(vectorId);

        QrMaskPattern selected = Model2MatrixBuilder.SelectModel2Mask(
            codewords,
            version,
            errorCorrectionIndex,
            QrMaskPattern.Automatic);

        Assert.That(selected, Is.EqualTo(expected));
    }

    [TestCase("model2-v01-l", "model2-v01-l.txt", 1, EccBlockTable.Low, QrMaskPattern.Zero)]
    [TestCase("model2-v01-l-kanji", "model2-v01-l-kanji.txt", 1, EccBlockTable.Low, QrMaskPattern.Five)]
    [TestCase("model2-v09-m", "model2-v09-m.txt", 9, EccBlockTable.Medium, QrMaskPattern.One)]
    [TestCase("model2-v10-q", "model2-v10-q.txt", 10, EccBlockTable.Quartile, QrMaskPattern.Two)]
    [TestCase("model2-v26-h", "model2-v26-h.txt", 26, EccBlockTable.High, QrMaskPattern.Three)]
    [TestCase("model2-v27-l", "model2-v27-l.txt", 27, EccBlockTable.Low, QrMaskPattern.Four)]
    [TestCase("model2-v40-h", "model2-v40-h.txt", 40, EccBlockTable.High, QrMaskPattern.Seven)]
    public void ForcedSelectionBuildsTheExactGoldenMatrix(
        string vectorId,
        string matrixFileName,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern requestedMask)
    {
        byte[] codewords = ReadGoldenCodewords(vectorId);
        byte[] original = (byte[])codewords.Clone();

        QrMaskPattern selected = Model2MatrixBuilder.SelectModel2Mask(
            codewords,
            version,
            errorCorrectionIndex,
            requestedMask);
        BitMatrixBuffer matrix = Model2MatrixBuilder.CreateSelectedModel2Matrix(
            codewords,
            version,
            errorCorrectionIndex,
            requestedMask);

        Assert.Multiple(() =>
        {
            Assert.That(selected, Is.EqualTo(requestedMask));
            Assert.That(codewords, Is.EqualTo(original));
            AssertMatrixEquals(LoadGoldenRows(matrixFileName), matrix);
        });
    }

    [Test]
    public void AutomaticMatrixUsesTheSameWinningMaskAsForcedConstruction()
    {
        byte[] codewords = ReadGoldenCodewords("model2-v10-q");

        BitMatrixBuffer automatic = Model2MatrixBuilder.CreateSelectedModel2Matrix(
            codewords,
            10,
            EccBlockTable.Quartile,
            QrMaskPattern.Automatic);
        BitMatrixBuffer forced = Model2MatrixBuilder.CreateSelectedModel2Matrix(
            codewords,
            10,
            EccBlockTable.Quartile,
            QrMaskPattern.Six);

        Assert.That(CopyPackedBytes(automatic), Is.EqualTo(CopyPackedBytes(forced)));
    }

    [Test]
    public void EqualMinimumPenaltiesSelectTheLowerMask()
    {
        byte[] codewords = new byte[]
        {
            109, 1, 201, 111, 156, 18, 237, 249, 29, 182, 3, 235, 168,
            152, 181, 54, 2, 179, 95, 205, 181, 4, 51, 220, 251, 101
        };
        int[] penalties = new int[8];
        for (int mask = 0; mask < 8; mask++)
        {
            BitMatrixBuffer matrix = Model2MatrixBuilder.CreateSelectedModel2Matrix(
                codewords,
                1,
                EccBlockTable.Low,
                (QrMaskPattern)mask);
            penalties[mask] = MaskEvaluator.CalculateModel2Penalty(matrix);
        }

        QrMaskPattern selected = Model2MatrixBuilder.SelectModel2Mask(
            codewords,
            1,
            EccBlockTable.Low,
            QrMaskPattern.Automatic);

        Assert.Multiple(() =>
        {
            Assert.That(penalties, Is.EqualTo(new int[]
            {
                1165, 1125, 1134, 1057, 1374, 1183, 1057, 1123
            }));
            Assert.That(selected, Is.EqualTo(QrMaskPattern.Three));
        });
    }

    [Test]
    public void RejectsUndefinedMasksAndInvalidCandidateInputs()
    {
        byte[] codewords = new byte[Model2VersionTable.GetTotalCodewordCount(1)];

        Assert.Multiple(() =>
        {
            Assert.That(() => Model2MatrixBuilder.SelectModel2Mask(
                    codewords,
                    1,
                    EccBlockTable.Low,
                    (QrMaskPattern)(-2)),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2MatrixBuilder.SelectModel2Mask(
                    codewords,
                    1,
                    EccBlockTable.Low,
                    (QrMaskPattern)8),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2MatrixBuilder.SelectModel2Mask(
                    null!,
                    1,
                    EccBlockTable.Low,
                    QrMaskPattern.Automatic),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2MatrixBuilder.SelectModel2Mask(
                    new byte[25],
                    1,
                    EccBlockTable.Low,
                    QrMaskPattern.Automatic),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2MatrixBuilder.SelectModel2Mask(
                    codewords,
                    0,
                    EccBlockTable.Low,
                    QrMaskPattern.Automatic),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Model2MatrixBuilder.SelectModel2Mask(
                    codewords,
                    1,
                    -1,
                    QrMaskPattern.Automatic),
                Throws.TypeOf<QrInputException>());
        });
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
