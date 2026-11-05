using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class DataModuleWalkerTests
{
    [TestCase("model2-v01-l", "model2-v01-l.txt", 1, EccBlockTable.Low, 0)]
    [TestCase("model2-v01-l-kanji", "model2-v01-l-kanji.txt", 1, EccBlockTable.Low, 5)]
    [TestCase("model2-v09-m", "model2-v09-m.txt", 9, EccBlockTable.Medium, 1)]
    [TestCase("model2-v10-q", "model2-v10-q.txt", 10, EccBlockTable.Quartile, 2)]
    [TestCase("model2-v26-h", "model2-v26-h.txt", 26, EccBlockTable.High, 3)]
    [TestCase("model2-v27-l", "model2-v27-l.txt", 27, EccBlockTable.Low, 4)]
    [TestCase("model2-v40-h", "model2-v40-h.txt", 40, EccBlockTable.High, 7)]
    public void PlacementAndExtractionMatchGoldenMatrices(
        string vectorId,
        string matrixFileName,
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        byte[] expectedCodewords = ReadGoldenCodewords(vectorId);
        string[] expectedRows = File.ReadAllLines(GetGoldenPath(
            "matrices",
            matrixFileName));
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);

        BitMatrixBuffer placed = FunctionPatternBuilder.CreateModel2FunctionPatterns(
            version,
            errorCorrectionIndex,
            mask);
        DataModuleWalker.PlaceModel2Codewords(expectedCodewords, version, placed);
        ApplyMask(placed, reserved, mask);
        AssertMatrixEquals(expectedRows, placed);

        BitMatrixBuffer extracted = LoadMatrix(expectedRows);
        ApplyMask(extracted, reserved, mask);
        Assert.That(
            DataModuleWalker.ExtractModel2Codewords(version, extracted),
            Is.EqualTo(expectedCodewords));
    }

    [Test]
    public void EveryVersionRoundTripsAllCodewordsAndClearsRemainderModules()
    {
        for (int version = Model2VersionTable.MinimumVersion;
            version <= Model2VersionTable.MaximumVersion;
            version++)
        {
            byte[] codewords = CreateCodewords(version);
            int dimension = Model2VersionTable.GetDimension(version);
            BitMatrixBuffer matrix = new BitMatrixBuffer(dimension, dimension);
            matrix.ClearModules(true);

            DataModuleWalker.PlaceModel2Codewords(codewords, version, matrix);
            byte[] extracted = DataModuleWalker.ExtractModel2Codewords(version, matrix);
            BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
            int darkDataAndRemainderCount = CountDarkUnreservedModules(matrix, reserved);

            Assert.Multiple(() =>
            {
                Assert.That(extracted, Is.EqualTo(codewords));
                Assert.That(
                    darkDataAndRemainderCount,
                    Is.EqualTo(CountSetBits(codewords)),
                    $"Version {version} did not clear every remainder module.");
            });
        }
    }

    [Test]
    public void PlacementDoesNotChangeReservedFunctionModules()
    {
        int version = 40;
        BitMatrixBuffer expected = FunctionPatternBuilder.CreateModel2FunctionPatterns(
            version,
            EccBlockTable.High,
            6);
        BitMatrixBuffer actual = FunctionPatternBuilder.CreateModel2FunctionPatterns(
            version,
            EccBlockTable.High,
            6);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
        byte[] codewords = new byte[Model2VersionTable.GetTotalCodewordCount(version)];
        global::System.Array.Fill(codewords, (byte)0xFF);

        DataModuleWalker.PlaceModel2Codewords(codewords, version, actual);

        int dimension = Model2VersionTable.GetDimension(version);
        for (int row = 0; row < dimension; row++)
        {
            for (int column = 0; column < dimension; column++)
            {
                if (reserved.GetModule(row, column))
                {
                    Assert.That(
                        actual.GetModule(row, column),
                        Is.EqualTo(expected.GetModule(row, column)),
                        $"Reserved module ({row}, {column}) was changed.");
                }
            }
        }
    }

    [Test]
    public void RejectsNullIncorrectlySizedMatricesAndWrongCodewordCounts()
    {
        int dimension = Model2VersionTable.GetDimension(1);
        BitMatrixBuffer correct = new BitMatrixBuffer(dimension, dimension);
        BitMatrixBuffer wrong = new BitMatrixBuffer(dimension + 1, dimension);
        byte[] codewords = new byte[Model2VersionTable.GetTotalCodewordCount(1)];

        Assert.Multiple(() =>
        {
            Assert.That(() => DataModuleWalker.PlaceModel2Codewords(null!, 1, correct),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.PlaceModel2Codewords(codewords, 1, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.PlaceModel2Codewords(new byte[25], 1, correct),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.PlaceModel2Codewords(new byte[27], 1, correct),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.PlaceModel2Codewords(codewords, 1, wrong),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.ExtractModel2Codewords(1, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.ExtractModel2Codewords(1, wrong),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.ExtractModel2Codewords(0, correct),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static byte[] CreateCodewords(int version)
    {
        byte[] result = new byte[Model2VersionTable.GetTotalCodewordCount(version)];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = (byte)((index * 149 + version * 37) & 255);
        }

        return result;
    }

    private static int CountDarkUnreservedModules(
        BitMatrixBuffer matrix,
        BitMatrixBuffer reserved)
    {
        int count = 0;
        for (int row = 0; row < matrix.GetHeight(); row++)
        {
            for (int column = 0; column < matrix.GetWidth(); column++)
            {
                if (!reserved.GetModule(row, column) && matrix.GetModule(row, column))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountSetBits(byte[] bytes)
    {
        int count = 0;
        for (int index = 0; index < bytes.Length; index++)
        {
            int value = bytes[index];
            for (int bit = 0; bit < 8; bit++)
            {
                count += value >> bit & 1;
            }
        }

        return count;
    }

    private static void ApplyMask(
        BitMatrixBuffer matrix,
        BitMatrixBuffer reserved,
        int mask)
    {
        for (int row = 0; row < matrix.GetHeight(); row++)
        {
            for (int column = 0; column < matrix.GetWidth(); column++)
            {
                if (!reserved.GetModule(row, column) && IsMasked(row, column, mask))
                {
                    matrix.ToggleModule(row, column);
                }
            }
        }
    }

    private static bool IsMasked(int row, int column, int mask)
    {
        if (mask == 0)
        {
            return (row + column) % 2 == 0;
        }

        if (mask == 1)
        {
            return row % 2 == 0;
        }

        if (mask == 2)
        {
            return column % 3 == 0;
        }

        if (mask == 3)
        {
            return (row + column) % 3 == 0;
        }

        if (mask == 4)
        {
            return (row / 2 + column / 3) % 2 == 0;
        }

        int product = row * column;
        if (mask == 5)
        {
            return product % 2 + product % 3 == 0;
        }

        if (mask == 6)
        {
            return (product % 2 + product % 3) % 2 == 0;
        }

        return ((row + column) % 2 + product % 3) % 2 == 0;
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

    private static void AssertMatrixEquals(
        string[] expected,
        BitMatrixBuffer actual)
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
            "vectors.json",
            "")));
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

    private static string GetGoldenPath(string directoryOrFile, string fileName)
    {
        string root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "..",
            "tests",
            "Fixtures",
            "Golden"));
        if (fileName.Length == 0)
        {
            return Path.Combine(root, directoryOrFile);
        }

        return Path.Combine(root, directoryOrFile, fileName);
    }
}
