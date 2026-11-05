using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class RectangularMicroMatrixBuilderTests
{
    [Test]
    public void EveryStandardizedSizeMatchesItsGoldenMatrixAndExtractsCodewords()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(GetGoldenPath(
            "vectors.json")));
        int testedCount = 0;
        foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
        {
            if (vector.GetProperty("profile").GetString() != "rMQR")
            {
                continue;
            }

            int height = vector.GetProperty("height").GetInt32();
            int width = vector.GetProperty("width").GetInt32();
            int versionIndex = RectangularMicroVersionTable.GetVersionIndex(height, width);
            int level = vector.GetProperty("error_correction").GetString() == "M"
                ? RectangularMicroVersionTable.Medium
                : RectangularMicroVersionTable.High;
            byte[] codewords = ReadCodewords(vector.GetProperty("full_codewords"));
            string matrixFile = vector.GetProperty("matrix_file").GetString()!;
            string[] expectedRows = File.ReadAllLines(GetGoldenPath(matrixFile));

            BitMatrixBuffer constructed =
                RectangularMicroMatrixBuilder.CreateSelectedRectangularMicroMatrix(
                    codewords,
                    versionIndex,
                    level,
                    QrMaskPattern.Automatic);
            Assert.That(
                CopyPackedBytes(constructed),
                Is.EqualTo(CopyPackedBytes(LoadMatrix(expectedRows))),
                vector.GetProperty("id").GetString());

            BitMatrixBuffer encoded = LoadMatrix(expectedRows);
            BitMatrixBuffer reserved =
                FunctionPatternBuilder.CreateRectangularMicroReservedMap(versionIndex);
            MaskEvaluator.ApplyRectangularMicroMask(encoded, reserved);
            Assert.That(
                DataModuleWalker.ExtractRectangularMicroCodewords(versionIndex, encoded),
                Is.EqualTo(codewords),
                vector.GetProperty("id").GetString());
            testedCount++;
        }

        Assert.That(testedCount, Is.EqualTo(32));
    }

    [Test]
    public void ReservedMapsLeaveExactlyCodewordAndRemainderModulesForEverySize()
    {
        for (int versionIndex = RectangularMicroVersionTable.MinimumVersionIndex;
            versionIndex <= RectangularMicroVersionTable.MaximumVersionIndex;
            versionIndex++)
        {
            BitMatrixBuffer reserved =
                FunctionPatternBuilder.CreateRectangularMicroReservedMap(versionIndex);
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

            int expected = RectangularMicroVersionTable.GetTotalCodewordCount(versionIndex) * 8
                + RectangularMicroVersionTable.GetRemainderBitCount(versionIndex);
            Assert.That(
                unreservedCount,
                Is.EqualTo(expected),
                RectangularMicroVersionTable.GetVersionName(versionIndex));
        }
    }

    [Test]
    public void BothFormatCopiesArePlacedLeastSignificantBitFirst()
    {
        for (int versionIndex = RectangularMicroVersionTable.MinimumVersionIndex;
            versionIndex <= RectangularMicroVersionTable.MaximumVersionIndex;
            versionIndex++)
        {
            for (int level = RectangularMicroVersionTable.Medium;
                level <= RectangularMicroVersionTable.High;
                level++)
            {
                int finderCode = FormatInformationCodec.CreateRectangularMicroFinderCode(
                    versionIndex,
                    level);
                int subfinderCode = FormatInformationCodec.CreateRectangularMicroSubfinderCode(
                    versionIndex,
                    level);
                BitMatrixBuffer matrix =
                    FunctionPatternBuilder.CreateRectangularMicroFunctionPatterns(
                        versionIndex,
                        level);
                for (int bitIndex = 0; bitIndex < 18; bitIndex++)
                {
                    Assert.That(
                        matrix.GetModule(
                            RectangularMicroPlacementTable.GetFinderSideFormatRow(bitIndex),
                            RectangularMicroPlacementTable.GetFinderSideFormatColumn(bitIndex)),
                        Is.EqualTo(((finderCode >> bitIndex) & 1) != 0));
                    Assert.That(
                        matrix.GetModule(
                            RectangularMicroPlacementTable.GetSubfinderSideFormatRow(
                                versionIndex,
                                bitIndex),
                            RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(
                                versionIndex,
                                bitIndex)),
                        Is.EqualTo(((subfinderCode >> bitIndex) & 1) != 0));
                }
            }
        }
    }

    [Test]
    public void EverySizeRoundTripsCodewordsAndClearsRemainderModules()
    {
        for (int versionIndex = RectangularMicroVersionTable.MinimumVersionIndex;
            versionIndex <= RectangularMicroVersionTable.MaximumVersionIndex;
            versionIndex++)
        {
            byte[] codewords = CreateCodewords(versionIndex);
            BitMatrixBuffer matrix = new BitMatrixBuffer(
                RectangularMicroVersionTable.GetWidth(versionIndex),
                RectangularMicroVersionTable.GetHeight(versionIndex));
            matrix.ClearModules(true);

            DataModuleWalker.PlaceRectangularMicroCodewords(
                codewords,
                versionIndex,
                matrix);
            byte[] extracted = DataModuleWalker.ExtractRectangularMicroCodewords(
                versionIndex,
                matrix);
            BitMatrixBuffer reserved =
                FunctionPatternBuilder.CreateRectangularMicroReservedMap(versionIndex);

            Assert.Multiple(() =>
            {
                Assert.That(extracted, Is.EqualTo(codewords));
                Assert.That(
                    CountDarkUnreservedModules(matrix, reserved),
                    Is.EqualTo(CountSetBits(codewords)),
                    RectangularMicroVersionTable.GetVersionName(versionIndex));
            });
        }
    }

    [Test]
    public void FixedMaskTogglesOnlyUnreservedModulesAndIsReversible()
    {
        int versionIndex = 31;
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        BitMatrixBuffer matrix = CreateAlternatingMatrix(width, height);
        BitMatrixBuffer reserved =
            FunctionPatternBuilder.CreateRectangularMicroReservedMap(versionIndex);
        byte[] original = CopyPackedBytes(matrix);

        MaskEvaluator.ApplyRectangularMicroMask(matrix, reserved);
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                bool before = (row + column) % 2 == 0;
                bool invert = !reserved.GetModule(row, column)
                    && RectangularMicroMaskTable.IsInverted(row, column);
                Assert.That(matrix.GetModule(row, column), Is.EqualTo(before != invert));
            }
        }

        MaskEvaluator.ApplyRectangularMicroMask(matrix, reserved);
        Assert.That(CopyPackedBytes(matrix), Is.EqualTo(original));
    }

    [Test]
    public void AutomaticAndForcedSelectionBothResolveToFixedMaskFour()
    {
        byte[] codewords = CreateCodewords(0);

        Assert.Multiple(() =>
        {
            Assert.That(
                RectangularMicroMatrixBuilder.SelectRectangularMicroMask(
                    codewords,
                    0,
                    RectangularMicroVersionTable.Medium,
                    QrMaskPattern.Automatic),
                Is.EqualTo(QrMaskPattern.Four));
            Assert.That(
                RectangularMicroMatrixBuilder.SelectRectangularMicroMask(
                    codewords,
                    0,
                    RectangularMicroVersionTable.Medium,
                    QrMaskPattern.Four),
                Is.EqualTo(QrMaskPattern.Four));
        });
    }

    [Test]
    public void RejectsOtherMasksInvalidCountsConfigurationsAndDimensions()
    {
        byte[] codewords = CreateCodewords(0);
        BitMatrixBuffer correct = new BitMatrixBuffer(43, 7);
        BitMatrixBuffer wrong = new BitMatrixBuffer(43, 9);

        Assert.Multiple(() =>
        {
            Assert.That(() => RectangularMicroMatrixBuilder.SelectRectangularMicroMask(
                    codewords,
                    0,
                    RectangularMicroVersionTable.Medium,
                    QrMaskPattern.Three),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroMatrixBuilder.SelectRectangularMicroMask(
                    null!,
                    0,
                    RectangularMicroVersionTable.Medium,
                    QrMaskPattern.Automatic),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.PlaceRectangularMicroCodewords(
                    new byte[12],
                    0,
                    correct),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.PlaceRectangularMicroCodewords(
                    codewords,
                    0,
                    wrong),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => DataModuleWalker.ExtractRectangularMicroCodewords(0, wrong),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MaskEvaluator.ApplyRectangularMicroMask(correct, wrong),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MaskEvaluator.ApplyRectangularMicroMask(
                    new BitMatrixBuffer(27, 7),
                    new BitMatrixBuffer(27, 7)),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => FunctionPatternBuilder.CreateRectangularMicroFunctionPatterns(
                    0,
                    2),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static byte[] CreateCodewords(int versionIndex)
    {
        byte[] result = new byte[
            RectangularMicroVersionTable.GetTotalCodewordCount(versionIndex)];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = (byte)((index * 131 + versionIndex * 43) & 255);
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

    private static BitMatrixBuffer CreateAlternatingMatrix(int width, int height)
    {
        BitMatrixBuffer result = new BitMatrixBuffer(width, height);
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                result.SetModule(row, column, (row + column) % 2 == 0);
            }
        }

        return result;
    }

    private static byte[] ReadCodewords(JsonElement values)
    {
        byte[] result = new byte[values.GetArrayLength()];
        int index = 0;
        foreach (JsonElement value in values.EnumerateArray())
        {
            result[index] = (byte)value.GetInt32();
            index++;
        }

        return result;
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

    private static byte[] CopyPackedBytes(BitMatrixBuffer matrix)
    {
        byte[] result = new byte[matrix.GetPackedByteLength()];
        matrix.CopyPackedBytes(result);
        return result;
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
