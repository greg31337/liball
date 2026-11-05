using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SimilarityTests
{
    [Test]
    public void Tanimoto_ReturnsIntersectionOverUnion()
    {
        Similarity similarity = new Similarity();
        BitFingerprint left = BuildFingerprint(16, new int[] { 1, 2, 3 });
        BitFingerprint right = BuildFingerprint(16, new int[] { 2, 3, 4, 5 });

        double value = similarity.Tanimoto(left, right);

        Assert.That(value, Is.EqualTo(0.4).Within(0.0000000001));
    }

    [Test]
    public void Dice_ReturnsTwiceIntersectionOverTotalOnBits()
    {
        Similarity similarity = new Similarity();
        BitFingerprint left = BuildFingerprint(16, new int[] { 1, 2, 3 });
        BitFingerprint right = BuildFingerprint(16, new int[] { 2, 3, 4, 5 });

        double value = similarity.Dice(left, right);

        Assert.That(value, Is.EqualTo(4.0 / 7.0).Within(0.0000000001));
    }

    [Test]
    public void Cosine_ReturnsIntersectionOverGeometricMean()
    {
        Similarity similarity = new Similarity();
        BitFingerprint left = BuildFingerprint(16, new int[] { 1, 2, 3 });
        BitFingerprint right = BuildFingerprint(16, new int[] { 2, 3, 4, 5 });

        double value = similarity.Cosine(left, right);

        Assert.That(value, Is.EqualTo(2.0 / System.Math.Sqrt(12.0)).Within(0.0000000001));
    }

    [Test]
    public void Tversky_UsesAsymmetricLeftAndRightWeights()
    {
        Similarity similarity = new Similarity();
        BitFingerprint left = BuildFingerprint(16, new int[] { 1, 2, 3 });
        BitFingerprint right = BuildFingerprint(16, new int[] { 2, 3, 4, 5 });

        double leftWeighted = similarity.Tversky(left, right, 0.25, 0.75);
        double rightWeighted = similarity.Tversky(right, left, 0.25, 0.75);

        Assert.That(leftWeighted, Is.EqualTo(2.0 / 3.75).Within(0.0000000001));
        Assert.That(rightWeighted, Is.EqualTo(2.0 / 3.25).Within(0.0000000001));
    }

    [Test]
    public void Metrics_ReturnOneForTwoEmptyFingerprints()
    {
        Similarity similarity = new Similarity();
        BitFingerprint left = new BitFingerprint(16);
        BitFingerprint right = new BitFingerprint(16);

        Assert.That(similarity.Tanimoto(left, right), Is.EqualTo(1.0));
        Assert.That(similarity.Dice(left, right), Is.EqualTo(1.0));
        Assert.That(similarity.Cosine(left, right), Is.EqualTo(1.0));
        Assert.That(similarity.Tversky(left, right, 0.5, 0.5), Is.EqualTo(1.0));
    }

    [Test]
    public void Metrics_ReturnZeroForEmptyAgainstNonEmptyFingerprint()
    {
        Similarity similarity = new Similarity();
        BitFingerprint left = new BitFingerprint(16);
        BitFingerprint right = BuildFingerprint(16, new int[] { 2, 3 });

        Assert.That(similarity.Tanimoto(left, right), Is.EqualTo(0.0));
        Assert.That(similarity.Dice(left, right), Is.EqualTo(0.0));
        Assert.That(similarity.Cosine(left, right), Is.EqualTo(0.0));
        Assert.That(similarity.Tversky(left, right, 0.5, 0.5), Is.EqualTo(0.0));
    }

    [Test]
    public void Metrics_CompareFingerprintsWithDifferentSizes()
    {
        Similarity similarity = new Similarity();
        BitFingerprint left = BuildFingerprint(8, new int[] { 1, 7 });
        BitFingerprint right = BuildFingerprint(16, new int[] { 1, 7, 12 });

        Assert.That(similarity.CountCommonBits(left, right), Is.EqualTo(2));
        Assert.That(similarity.CountUnionBits(left, right), Is.EqualTo(3));
        Assert.That(similarity.Tanimoto(left, right), Is.EqualTo(2.0 / 3.0).Within(0.0000000001));
    }

    [Test]
    public void CountMetrics_UseOccurrenceOverlapAndVectorCosine()
    {
        Similarity similarity = new Similarity();
        CountFingerprint left = BuildCountFingerprint(16, new int[] { 1, 2, 3 }, new int[] { 2, 1, 3 });
        CountFingerprint right = BuildCountFingerprint(16, new int[] { 1, 2, 4 }, new int[] { 1, 4, 2 });

        double tversky = similarity.TverskyCount(left, right, 0.25, 0.75);
        double reverseTversky = similarity.TverskyCount(right, left, 0.25, 0.75);

        Assert.That(similarity.TanimotoCount(left, right), Is.EqualTo(2.0 / 11.0).Within(0.0000000001));
        Assert.That(similarity.DiceCount(left, right), Is.EqualTo(4.0 / 13.0).Within(0.0000000001));
        Assert.That(similarity.CosineCount(left, right), Is.EqualTo(6.0 / System.Math.Sqrt(294.0)).Within(0.0000000001));
        Assert.That(tversky, Is.EqualTo(2.0 / 6.75).Within(0.0000000001));
        Assert.That(reverseTversky, Is.EqualTo(2.0 / 6.25).Within(0.0000000001));
    }

    [Test]
    public void SparseMetrics_MatchBitMetricsForTheSameIndices()
    {
        Similarity similarity = new Similarity();
        BitFingerprint leftBits = BuildFingerprint(16, new int[] { 1, 2, 3 });
        BitFingerprint rightBits = BuildFingerprint(16, new int[] { 2, 3, 4, 5 });
        SparseFingerprint left = BuildSparseFingerprint(16, new int[] { 1, 2, 3 });
        SparseFingerprint right = BuildSparseFingerprint(16, new int[] { 2, 3, 4, 5 });

        Assert.That(similarity.TanimotoSparse(left, right), Is.EqualTo(similarity.Tanimoto(leftBits, rightBits)).Within(0.0000000001));
        Assert.That(similarity.DiceSparse(left, right), Is.EqualTo(similarity.Dice(leftBits, rightBits)).Within(0.0000000001));
        Assert.That(similarity.CosineSparse(left, right), Is.EqualTo(similarity.Cosine(leftBits, rightBits)).Within(0.0000000001));
        Assert.That(similarity.TverskySparse(left, right, 0.25, 0.75), Is.EqualTo(similarity.Tversky(leftBits, rightBits, 0.25, 0.75)).Within(0.0000000001));
    }

    [Test]
    public void CountAndSparseMetrics_DefineEmptyFingerprintBehavior()
    {
        Similarity similarity = new Similarity();
        CountFingerprint emptyCount = new CountFingerprint(8);
        CountFingerprint populatedCount = BuildCountFingerprint(16, new int[] { 12 }, new int[] { 3 });
        SparseFingerprint emptySparse = new SparseFingerprint(8);
        SparseFingerprint populatedSparse = BuildSparseFingerprint(16, new int[] { 12 });
        BitFingerprint emptyBits = new BitFingerprint(8);
        BitFingerprint populatedBits = BuildFingerprint(16, new int[] { 12 });

        Assert.That(similarity.TanimotoCount(emptyCount, new CountFingerprint(16)), Is.EqualTo(1.0));
        Assert.That(similarity.DiceCount(emptyCount, populatedCount), Is.EqualTo(0.0));
        Assert.That(similarity.CosineCount(emptyCount, populatedCount), Is.EqualTo(0.0));
        Assert.That(similarity.TverskyCount(emptyCount, populatedCount, 0.0, 0.0), Is.EqualTo(0.0));
        Assert.That(similarity.TanimotoSparse(emptySparse, new SparseFingerprint(16)), Is.EqualTo(1.0));
        Assert.That(similarity.DiceSparse(emptySparse, populatedSparse), Is.EqualTo(0.0));
        Assert.That(similarity.CosineSparse(emptySparse, populatedSparse), Is.EqualTo(0.0));
        Assert.That(similarity.TverskySparse(emptySparse, populatedSparse, 0.0, 0.0), Is.EqualTo(0.0));
        Assert.That(similarity.Tversky(emptyBits, populatedBits, 0.0, 0.0), Is.EqualTo(0.0));
    }

    [Test]
    public void CountAndSparseMetrics_ReturnOneForTwoEmptyFingerprints()
    {
        Similarity similarity = new Similarity();
        CountFingerprint leftCount = new CountFingerprint(8);
        CountFingerprint rightCount = new CountFingerprint(16);
        SparseFingerprint leftSparse = new SparseFingerprint(8);
        SparseFingerprint rightSparse = new SparseFingerprint(16);

        Assert.That(similarity.DiceCount(leftCount, rightCount), Is.EqualTo(1.0));
        Assert.That(similarity.CosineCount(leftCount, rightCount), Is.EqualTo(1.0));
        Assert.That(similarity.TverskyCount(leftCount, rightCount, 0.5, 0.5), Is.EqualTo(1.0));
        Assert.That(similarity.DiceSparse(leftSparse, rightSparse), Is.EqualTo(1.0));
        Assert.That(similarity.CosineSparse(leftSparse, rightSparse), Is.EqualTo(1.0));
        Assert.That(similarity.TverskySparse(leftSparse, rightSparse, 0.5, 0.5), Is.EqualTo(1.0));
    }

    [Test]
    public void TverskyMetrics_DefineZeroWeightDisjointBehavior()
    {
        Similarity similarity = new Similarity();
        BitFingerprint leftBits = BuildFingerprint(8, new int[] { 1 });
        BitFingerprint rightBits = BuildFingerprint(8, new int[] { 2 });
        CountFingerprint leftCount = BuildCountFingerprint(8, new int[] { 1 }, new int[] { 2 });
        CountFingerprint rightCount = BuildCountFingerprint(8, new int[] { 2 }, new int[] { 3 });
        SparseFingerprint leftSparse = BuildSparseFingerprint(8, new int[] { 1 });
        SparseFingerprint rightSparse = BuildSparseFingerprint(8, new int[] { 2 });

        Assert.That(similarity.Tversky(leftBits, rightBits, 0.0, 0.0), Is.EqualTo(1.0));
        Assert.That(similarity.TverskyCount(leftCount, rightCount, 0.0, 0.0), Is.EqualTo(1.0));
        Assert.That(similarity.TverskySparse(leftSparse, rightSparse, 0.0, 0.0), Is.EqualTo(1.0));
    }

    [Test]
    public void CountAndSparseMetrics_CompareAbsoluteIndicesAcrossDifferentSizes()
    {
        Similarity similarity = new Similarity();
        CountFingerprint leftCount = BuildCountFingerprint(8, new int[] { 1, 7 }, new int[] { 2, 1 });
        CountFingerprint rightCount = BuildCountFingerprint(16, new int[] { 1, 7, 12 }, new int[] { 1, 2, 5 });
        SparseFingerprint leftSparse = BuildSparseFingerprint(8, new int[] { 1, 7 });
        SparseFingerprint rightSparse = BuildSparseFingerprint(16, new int[] { 1, 7, 12 });

        Assert.That(similarity.TanimotoCount(leftCount, rightCount), Is.EqualTo(2.0 / 9.0).Within(0.0000000001));
        Assert.That(similarity.DiceCount(leftCount, rightCount), Is.EqualTo(4.0 / 11.0).Within(0.0000000001));
        Assert.That(similarity.CosineCount(leftCount, rightCount), Is.EqualTo(4.0 / System.Math.Sqrt(150.0)).Within(0.0000000001));
        Assert.That(similarity.TanimotoSparse(leftSparse, rightSparse), Is.EqualTo(2.0 / 3.0).Within(0.0000000001));
        Assert.That(similarity.DiceSparse(leftSparse, rightSparse), Is.EqualTo(4.0 / 5.0).Within(0.0000000001));
        Assert.That(similarity.CosineSparse(leftSparse, rightSparse), Is.EqualTo(2.0 / System.Math.Sqrt(6.0)).Within(0.0000000001));
    }

    static BitFingerprint BuildFingerprint(int size, int[] indices)
    {
        BitFingerprint fingerprint = new BitFingerprint(size);
        int index = 0;
        while (index < indices.Length)
        {
            fingerprint.SetBit(indices[index]);
            index = index + 1;
        }

        return fingerprint;
    }

    static CountFingerprint BuildCountFingerprint(int size, int[] indices, int[] counts)
    {
        CountFingerprint fingerprint = new CountFingerprint(size);
        int index = 0;
        while (index < indices.Length && index < counts.Length)
        {
            fingerprint.AddCount(indices[index], counts[index]);
            index = index + 1;
        }

        return fingerprint;
    }

    static SparseFingerprint BuildSparseFingerprint(int size, int[] indices)
    {
        SparseFingerprint fingerprint = new SparseFingerprint(size);
        int index = 0;
        while (index < indices.Length)
        {
            fingerprint.AddIndex(indices[index]);
            index = index + 1;
        }

        return fingerprint;
    }
}
