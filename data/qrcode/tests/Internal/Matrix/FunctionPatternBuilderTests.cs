using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class FunctionPatternBuilderTests
{
    [TestCase("model2-v01-l.txt", 1, EccBlockTable.Low, 0)]
    [TestCase("model2-v01-l-kanji.txt", 1, EccBlockTable.Low, 5)]
    [TestCase("structured-append-3-part-3.txt", 2, EccBlockTable.Low, 2)]
    [TestCase("model2-v09-m.txt", 9, EccBlockTable.Medium, 1)]
    [TestCase("model2-v10-q.txt", 10, EccBlockTable.Quartile, 2)]
    [TestCase("model2-v26-h.txt", 26, EccBlockTable.High, 3)]
    [TestCase("model2-v27-l.txt", 27, EccBlockTable.Low, 4)]
    [TestCase("model2-v40-h.txt", 40, EccBlockTable.High, 7)]
    public void ReservedFunctionModulesMatchGoldenMatrices(
        string fileName,
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        string[] expected = File.ReadAllLines(GetGoldenMatrixPath(fileName));
        BitMatrixBuffer modules = FunctionPatternBuilder.CreateModel2FunctionPatterns(
            version,
            errorCorrectionIndex,
            mask);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
        int dimension = Model2VersionTable.GetDimension(version);
        int unreservedCount = 0;

        Assert.That(expected.Length, Is.EqualTo(dimension));
        for (int row = 0; row < dimension; row++)
        {
            Assert.That(expected[row].Length, Is.EqualTo(dimension));
            for (int column = 0; column < dimension; column++)
            {
                if (reserved.GetModule(row, column))
                {
                    bool expectedDark = expected[row][column] == '1';
                    Assert.That(
                        modules.GetModule(row, column),
                        Is.EqualTo(expectedDark),
                        $"Reserved module ({row}, {column}) differs from {fileName}.");
                }
                else
                {
                    unreservedCount++;
                }
            }
        }

        Assert.That(
            unreservedCount,
            Is.EqualTo(Model2VersionTable.GetRawDataModuleCount(version)));
    }

    [Test]
    public void ReservedMapsLeaveExactlyTheRawDataModulesForEveryVersion()
    {
        for (int version = Model2VersionTable.MinimumVersion;
            version <= Model2VersionTable.MaximumVersion;
            version++)
        {
            BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
            int dimension = Model2VersionTable.GetDimension(version);
            int unreservedCount = 0;
            for (int row = 0; row < dimension; row++)
            {
                for (int column = 0; column < dimension; column++)
                {
                    if (!reserved.GetModule(row, column))
                    {
                        unreservedCount++;
                    }
                }
            }

            Assert.That(
                unreservedCount,
                Is.EqualTo(Model2VersionTable.GetRawDataModuleCount(version)),
                $"Version {version} has an incorrect reserved map.");
        }
    }

    [Test]
    public void PlacesEveryFormatCodeLeastSignificantBitFirstInBothCopies()
    {
        int version = 7;
        for (int level = EccBlockTable.Low; level <= EccBlockTable.High; level++)
        {
            for (int mask = 0; mask < 8; mask++)
            {
                int formatCode = FormatInformationCodec.CreateModel2Code(level, mask);
                BitMatrixBuffer modules = FunctionPatternBuilder.CreateModel2FunctionPatterns(
                    version,
                    level,
                    mask);
                for (int bitIndex = 0; bitIndex < 15; bitIndex++)
                {
                    bool expected = ((formatCode >> bitIndex) & 1) != 0;
                    Assert.That(
                        modules.GetModule(
                            Model2PlacementTable.GetFormatPrimaryRow(bitIndex),
                            Model2PlacementTable.GetFormatPrimaryColumn(bitIndex)),
                        Is.EqualTo(expected));
                    Assert.That(
                        modules.GetModule(
                            Model2PlacementTable.GetFormatSecondaryRow(version, bitIndex),
                            Model2PlacementTable.GetFormatSecondaryColumn(version, bitIndex)),
                        Is.EqualTo(expected));
                }
            }
        }
    }

    [Test]
    public void PlacesVersionCodesInBothCopiesFromVersionSevenOnward()
    {
        for (int version = 7; version <= Model2VersionTable.MaximumVersion; version++)
        {
            int versionCode = VersionInformationCodec.CreateCode(version);
            BitMatrixBuffer modules = FunctionPatternBuilder.CreateModel2FunctionPatterns(
                version,
                EccBlockTable.Low,
                0);
            BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
            for (int bitIndex = 0; bitIndex < 18; bitIndex++)
            {
                int primaryRow = Model2PlacementTable.GetVersionPrimaryRow(version, bitIndex);
                int primaryColumn = Model2PlacementTable.GetVersionPrimaryColumn(version, bitIndex);
                int secondaryRow = Model2PlacementTable.GetVersionSecondaryRow(version, bitIndex);
                int secondaryColumn = Model2PlacementTable.GetVersionSecondaryColumn(version, bitIndex);
                bool expected = ((versionCode >> bitIndex) & 1) != 0;

                Assert.Multiple(() =>
                {
                    Assert.That(reserved.GetModule(primaryRow, primaryColumn), Is.True);
                    Assert.That(reserved.GetModule(secondaryRow, secondaryColumn), Is.True);
                    Assert.That(modules.GetModule(primaryRow, primaryColumn), Is.EqualTo(expected));
                    Assert.That(modules.GetModule(secondaryRow, secondaryColumn), Is.EqualTo(expected));
                });
            }
        }
    }

    [Test]
    public void DrawsTheFixedDarkModuleOutsideTheFormatCopies()
    {
        for (int version = 1; version <= Model2VersionTable.MaximumVersion; version++)
        {
            BitMatrixBuffer modules = FunctionPatternBuilder.CreateModel2FunctionPatterns(
                version,
                EccBlockTable.Low,
                0);
            BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
            int row = Model2PlacementTable.GetDarkModuleRow(version);
            int column = Model2PlacementTable.GetDarkModuleColumn(version);

            Assert.Multiple(() =>
            {
                Assert.That(reserved.GetModule(row, column), Is.True);
                Assert.That(modules.GetModule(row, column), Is.True);
            });
        }
    }

    [Test]
    public void RejectsInvalidVersionsErrorCorrectionLevelsAndMasks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => FunctionPatternBuilder.CreateModel2ReservedMap(0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FunctionPatternBuilder.CreateModel2ReservedMap(41),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FunctionPatternBuilder.CreateModel2FunctionPatterns(1, -1, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FunctionPatternBuilder.CreateModel2FunctionPatterns(1, 0, 8),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static string GetGoldenMatrixPath(string fileName)
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
            "matrices",
            fileName));
    }
}
