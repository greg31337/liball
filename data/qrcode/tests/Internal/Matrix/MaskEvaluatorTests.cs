using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MaskEvaluatorTests
{
    [Test]
    public void EveryMaskTogglesExactlyItsUnreservedPredicateAndIsReversible()
    {
        int version = 7;
        int dimension = Model2VersionTable.GetDimension(version);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
        for (int mask = 0; mask < 8; mask++)
        {
            BitMatrixBuffer matrix = CreateAlternatingMatrix(dimension);
            byte[] original = CopyPackedBytes(matrix);

            MaskEvaluator.ApplyModel2Mask(matrix, reserved, mask);

            for (int row = 0; row < dimension; row++)
            {
                for (int column = 0; column < dimension; column++)
                {
                    bool before = (row + column) % 2 == 0;
                    bool invert = !reserved.GetModule(row, column)
                        && Model2MaskTable.IsInverted(mask, row, column);
                    Assert.That(
                        matrix.GetModule(row, column),
                        Is.EqualTo(before != invert),
                        $"Mask {mask} differs at ({row}, {column}).");
                }
            }

            MaskEvaluator.ApplyModel2Mask(matrix, reserved, mask);
            Assert.That(CopyPackedBytes(matrix), Is.EqualTo(original));
        }
    }

    [Test]
    public void UniformMatricesHaveExactRuleOneTwoAndFourPenalties()
    {
        BitMatrixBuffer light = new BitMatrixBuffer(21, 21);
        BitMatrixBuffer dark = new BitMatrixBuffer(21, 21);
        dark.ClearModules(true);

        Assert.Multiple(() =>
        {
            Assert.That(MaskEvaluator.CalculateAdjacentPenalty(light), Is.EqualTo(798));
            Assert.That(MaskEvaluator.CalculateBlockPenalty(light), Is.EqualTo(1200));
            Assert.That(MaskEvaluator.CalculateFinderLikePenalty(light), Is.Zero);
            Assert.That(MaskEvaluator.CalculateBalancePenalty(light), Is.EqualTo(100));
            Assert.That(MaskEvaluator.CalculateModel2Penalty(light), Is.EqualTo(2098));
            Assert.That(MaskEvaluator.CalculateAdjacentPenalty(dark), Is.EqualTo(798));
            Assert.That(MaskEvaluator.CalculateBlockPenalty(dark), Is.EqualTo(1200));
            Assert.That(MaskEvaluator.CalculateFinderLikePenalty(dark), Is.Zero);
            Assert.That(MaskEvaluator.CalculateBalancePenalty(dark), Is.EqualTo(100));
            Assert.That(MaskEvaluator.CalculateModel2Penalty(dark), Is.EqualTo(2098));
        });
    }

    [Test]
    public void CheckerboardHasNoPenalty()
    {
        BitMatrixBuffer matrix = CreateAlternatingMatrix(21);

        Assert.Multiple(() =>
        {
            Assert.That(MaskEvaluator.CalculateAdjacentPenalty(matrix), Is.Zero);
            Assert.That(MaskEvaluator.CalculateBlockPenalty(matrix), Is.Zero);
            Assert.That(MaskEvaluator.CalculateFinderLikePenalty(matrix), Is.Zero);
            Assert.That(MaskEvaluator.CalculateBalancePenalty(matrix), Is.Zero);
            Assert.That(MaskEvaluator.CalculateModel2Penalty(matrix), Is.Zero);
        });
    }

    [Test]
    public void FinderLikeRuleCountsTheLightQuietZoneAtSymbolBoundaries()
    {
        BitMatrixBuffer matrix = new BitMatrixBuffer(21, 21);
        matrix.SetModule(10, 0, true);
        matrix.SetModule(10, 2, true);
        matrix.SetModule(10, 3, true);
        matrix.SetModule(10, 4, true);
        matrix.SetModule(10, 6, true);

        Assert.That(
            MaskEvaluator.CalculateFinderLikePenalty(matrix),
            Is.EqualTo(80));
    }

    [Test]
    public void FinderLikeRuleRecognizesScaledRunRatios()
    {
        BitMatrixBuffer matrix = new BitMatrixBuffer(41, 41);
        for (int column = 8; column <= 9; column++)
        {
            matrix.SetModule(20, column, true);
        }

        for (int column = 12; column <= 17; column++)
        {
            matrix.SetModule(20, column, true);
        }

        for (int column = 20; column <= 21; column++)
        {
            matrix.SetModule(20, column, true);
        }

        Assert.That(
            MaskEvaluator.CalculateFinderLikePenalty(matrix),
            Is.EqualTo(80));
    }

    [Test]
    public void BalanceRuleUsesCompleteFivePercentSteps()
    {
        BitMatrixBuffer belowThreshold = CreateMatrixWithDarkCount(21, 199);
        BitMatrixBuffer beyondThreshold = CreateMatrixWithDarkCount(21, 198);
        BitMatrixBuffer beyondTwoThresholds = CreateMatrixWithDarkCount(21, 176);

        Assert.Multiple(() =>
        {
            Assert.That(
                MaskEvaluator.CalculateBalancePenalty(belowThreshold),
                Is.Zero);
            Assert.That(
                MaskEvaluator.CalculateBalancePenalty(beyondThreshold),
                Is.EqualTo(10));
            Assert.That(
                MaskEvaluator.CalculateBalancePenalty(beyondTwoThresholds),
                Is.EqualTo(20));
        });
    }

    [TestCase("model2-v01-l.txt", 1190)]
    [TestCase("model2-v01-l-kanji.txt", 1169)]
    [TestCase("model2-v09-m.txt", 3100)]
    [TestCase("model2-v10-q.txt", 2777)]
    [TestCase("model2-v26-h.txt", 10338)]
    [TestCase("model2-v27-l.txt", 7691)]
    [TestCase("model2-v40-h.txt", 23041)]
    public void GoldenMatricesHaveExactPenaltyScores(string fileName, int expected)
    {
        BitMatrixBuffer matrix = LoadGoldenMatrix(fileName);

        Assert.That(MaskEvaluator.CalculateModel2Penalty(matrix), Is.EqualTo(expected));
    }

    [Test]
    public void RejectsInvalidMasksMapsAndModel2Dimensions()
    {
        BitMatrixBuffer valid = new BitMatrixBuffer(21, 21);
        BitMatrixBuffer wrongSize = new BitMatrixBuffer(25, 21);
        BitMatrixBuffer wrongShape = new BitMatrixBuffer(22, 22);

        Assert.Multiple(() =>
        {
            Assert.That(() => MaskEvaluator.ApplyModel2Mask(null!, valid, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MaskEvaluator.ApplyModel2Mask(valid, null!, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MaskEvaluator.ApplyModel2Mask(valid, wrongSize, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MaskEvaluator.ApplyModel2Mask(valid, valid, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => MaskEvaluator.ApplyModel2Mask(valid, valid, 8),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => MaskEvaluator.CalculateModel2Penalty(null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MaskEvaluator.CalculateModel2Penalty(wrongSize),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MaskEvaluator.CalculateModel2Penalty(wrongShape),
                Throws.TypeOf<QrInputException>());
        });
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

    private static BitMatrixBuffer CreateMatrixWithDarkCount(int dimension, int darkCount)
    {
        BitMatrixBuffer result = new BitMatrixBuffer(dimension, dimension);
        int index = 0;
        while (index < darkCount)
        {
            result.SetModule(index / dimension, index % dimension, true);
            index++;
        }

        return result;
    }

    private static byte[] CopyPackedBytes(BitMatrixBuffer matrix)
    {
        byte[] result = new byte[matrix.GetPackedByteLength()];
        matrix.CopyPackedBytes(result);
        return result;
    }

    private static BitMatrixBuffer LoadGoldenMatrix(string fileName)
    {
        string path = Path.GetFullPath(Path.Combine(
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
        string[] rows = File.ReadAllLines(path);
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
}
