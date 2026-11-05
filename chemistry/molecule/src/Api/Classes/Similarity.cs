namespace molecule;


/// <summary>
/// Provides similarity data and operations for molecular graphs.
/// </summary>
/// <remarks>
/// Every metric returns one for two empty fingerprints and zero when exactly one fingerprint is empty. Indices outside the smaller fingerprint size are compared with zero.
/// </remarks>
public class Similarity : ISimilarity
{
    /// <summary>
    /// Initializes a fingerprint similarity calculator.
    /// </summary>
    public Similarity()
    {
    }

    /// <summary>
    /// Calculates the Tanimoto similarity coefficient from the on bits in two fingerprints.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    public double Tanimoto(BitFingerprint left, BitFingerprint right)
    {
        int common = CountCommonBits(left, right);
        int union = CountUnionBits(left, right);
        if (union == 0)
        {
            return 1.0;
        }

        return (double)common / (double)union;
    }

    /// <summary>
    /// Calculates the Dice similarity coefficient from the on bits in two fingerprints.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    public double Dice(BitFingerprint left, BitFingerprint right)
    {
        int common = CountCommonBits(left, right);
        int leftCount = CountOnBits(left);
        int rightCount = CountOnBits(right);
        int denominator = leftCount + rightCount;
        if (denominator == 0)
        {
            return 1.0;
        }

        return (2.0 * (double)common) / (double)denominator;
    }

    /// <summary>
    /// Cosine.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    public double Cosine(BitFingerprint left, BitFingerprint right)
    {
        int common = CountCommonBits(left, right);
        int leftCount = CountOnBits(left);
        int rightCount = CountOnBits(right);
        if (leftCount == 0 && rightCount == 0)
        {
            return 1.0;
        }

        if (leftCount == 0 || rightCount == 0)
        {
            return 0.0;
        }

        double denominator = System.Math.Sqrt((double)leftCount * (double)rightCount);
        return (double)common / denominator;
    }

    /// <summary>
    /// Calculates asymmetric Tversky similarity using the supplied alpha and beta weights.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <param name="alpha">The alpha.</param>
    /// <param name="beta">The beta.</param>
    /// <returns>The calculated numeric value.</returns>
    public double Tversky(BitFingerprint left, BitFingerprint right, double alpha, double beta)
    {
        int common = CountCommonBits(left, right);
        int leftCount = CountOnBits(left);
        int rightCount = CountOnBits(right);
        if (leftCount == 0 && rightCount == 0)
        {
            return 1.0;
        }

        if (leftCount == 0 || rightCount == 0)
        {
            return 0.0;
        }

        int leftOnly = CountLeftOnlyBits(left, right);
        int rightOnly = CountLeftOnlyBits(right, left);
        return CalculateTversky((double)common, (double)leftOnly, (double)rightOnly, alpha, beta);
    }

    /// <summary>
    /// Calculates multiset Tanimoto similarity from counted fingerprint features.
    /// </summary>
    /// <param name="left">The left counted fingerprint.</param>
    /// <param name="right">The right counted fingerprint.</param>
    /// <returns>The sum of minimum counts divided by the sum of maximum counts.</returns>
    public double TanimotoCount(CountFingerprint left, CountFingerprint right)
    {
        double common = CountCommonOccurrences(left, right);
        double leftTotal = GetCountTotal(left);
        double rightTotal = GetCountTotal(right);
        double union = leftTotal + rightTotal - common;
        if (union == 0.0)
        {
            return 1.0;
        }

        return common / union;
    }

    /// <summary>
    /// Calculates multiset Dice similarity from counted fingerprint features.
    /// </summary>
    /// <param name="left">The left counted fingerprint.</param>
    /// <param name="right">The right counted fingerprint.</param>
    /// <returns>Twice the minimum-count overlap divided by the total feature count.</returns>
    public double DiceCount(CountFingerprint left, CountFingerprint right)
    {
        double common = CountCommonOccurrences(left, right);
        double denominator = GetCountTotal(left) + GetCountTotal(right);
        if (denominator == 0.0)
        {
            return 1.0;
        }

        return 2.0 * common / denominator;
    }

    /// <summary>
    /// Calculates cosine similarity from counted fingerprint features.
    /// </summary>
    /// <param name="left">The left counted fingerprint.</param>
    /// <param name="right">The right counted fingerprint.</param>
    /// <returns>The count-vector dot product divided by the product of vector lengths.</returns>
    public double CosineCount(CountFingerprint left, CountFingerprint right)
    {
        double leftSquare = GetCountSquareTotal(left);
        double rightSquare = GetCountSquareTotal(right);
        if (leftSquare == 0.0 && rightSquare == 0.0)
        {
            return 1.0;
        }

        if (leftSquare == 0.0 || rightSquare == 0.0)
        {
            return 0.0;
        }

        return CountDotProduct(left, right) / System.Math.Sqrt(leftSquare * rightSquare);
    }

    /// <summary>
    /// Calculates asymmetric multiset Tversky similarity from counted fingerprint features.
    /// </summary>
    /// <param name="left">The left counted fingerprint.</param>
    /// <param name="right">The right counted fingerprint.</param>
    /// <param name="alpha">The weight applied to unmatched left occurrences.</param>
    /// <param name="beta">The weight applied to unmatched right occurrences.</param>
    /// <returns>The calculated similarity.</returns>
    public double TverskyCount(CountFingerprint left, CountFingerprint right, double alpha, double beta)
    {
        double leftTotal = GetCountTotal(left);
        double rightTotal = GetCountTotal(right);
        if (leftTotal == 0.0 && rightTotal == 0.0)
        {
            return 1.0;
        }

        if (leftTotal == 0.0 || rightTotal == 0.0)
        {
            return 0.0;
        }

        double common = CountCommonOccurrences(left, right);
        return CalculateTversky(common, leftTotal - common, rightTotal - common, alpha, beta);
    }

    /// <summary>
    /// Calculates Tanimoto similarity from sparse fingerprint indices.
    /// </summary>
    /// <param name="left">The left sparse fingerprint.</param>
    /// <param name="right">The right sparse fingerprint.</param>
    /// <returns>The intersection size divided by the union size.</returns>
    public double TanimotoSparse(SparseFingerprint left, SparseFingerprint right)
    {
        int common = CountCommonSparseIndices(left, right);
        int leftCount = CountSparseIndices(left);
        int rightCount = CountSparseIndices(right);
        int union = leftCount + rightCount - common;
        if (union == 0)
        {
            return 1.0;
        }

        return (double)common / (double)union;
    }

    /// <summary>
    /// Calculates Dice similarity from sparse fingerprint indices.
    /// </summary>
    /// <param name="left">The left sparse fingerprint.</param>
    /// <param name="right">The right sparse fingerprint.</param>
    /// <returns>Twice the intersection size divided by the total index count.</returns>
    public double DiceSparse(SparseFingerprint left, SparseFingerprint right)
    {
        int common = CountCommonSparseIndices(left, right);
        int denominator = CountSparseIndices(left) + CountSparseIndices(right);
        if (denominator == 0)
        {
            return 1.0;
        }

        return 2.0 * (double)common / (double)denominator;
    }

    /// <summary>
    /// Calculates cosine similarity from sparse fingerprint indices.
    /// </summary>
    /// <param name="left">The left sparse fingerprint.</param>
    /// <param name="right">The right sparse fingerprint.</param>
    /// <returns>The intersection size divided by the geometric mean of index counts.</returns>
    public double CosineSparse(SparseFingerprint left, SparseFingerprint right)
    {
        int common = CountCommonSparseIndices(left, right);
        int leftCount = CountSparseIndices(left);
        int rightCount = CountSparseIndices(right);
        if (leftCount == 0 && rightCount == 0)
        {
            return 1.0;
        }

        if (leftCount == 0 || rightCount == 0)
        {
            return 0.0;
        }

        return (double)common / System.Math.Sqrt((double)leftCount * (double)rightCount);
    }

    /// <summary>
    /// Calculates asymmetric Tversky similarity from sparse fingerprint indices.
    /// </summary>
    /// <param name="left">The left sparse fingerprint.</param>
    /// <param name="right">The right sparse fingerprint.</param>
    /// <param name="alpha">The weight applied to left-only indices.</param>
    /// <param name="beta">The weight applied to right-only indices.</param>
    /// <returns>The calculated similarity.</returns>
    public double TverskySparse(SparseFingerprint left, SparseFingerprint right, double alpha, double beta)
    {
        int leftCount = CountSparseIndices(left);
        int rightCount = CountSparseIndices(right);
        if (leftCount == 0 && rightCount == 0)
        {
            return 1.0;
        }

        if (leftCount == 0 || rightCount == 0)
        {
            return 0.0;
        }

        int common = CountCommonSparseIndices(left, right);
        return CalculateTversky((double)common, (double)(leftCount - common), (double)(rightCount - common), alpha, beta);
    }

    internal double CalculateTversky(double common, double leftOnly, double rightOnly, double alpha, double beta)
    {
        double denominator = common + alpha * leftOnly + beta * rightOnly;
        if (denominator == 0.0)
        {
            return 1.0;
        }

        return common / denominator;
    }

    internal double CountCommonOccurrences(CountFingerprint? left, CountFingerprint? right)
    {
        if (left == null || right == null)
        {
            return 0.0;
        }

        double common = 0.0;
        int position = 0;
        while (position < left.Indices.Count && position < left.Counts.Count)
        {
            int index = left.Indices[position];
            int leftCount = GetCountValue(left, index);
            int rightCount = GetCountValue(right, index);
            if (leftCount < rightCount)
            {
                common = common + (double)leftCount;
            }
            else
            {
                common = common + (double)rightCount;
            }

            position = position + 1;
        }

        return common;
    }

    internal double CountDotProduct(CountFingerprint? left, CountFingerprint? right)
    {
        if (left == null || right == null)
        {
            return 0.0;
        }

        double product = 0.0;
        int position = 0;
        while (position < left.Indices.Count && position < left.Counts.Count)
        {
            int index = left.Indices[position];
            product = product + (double)GetCountValue(left, index) * (double)GetCountValue(right, index);
            position = position + 1;
        }

        return product;
    }

    internal double GetCountTotal(CountFingerprint? fingerprint)
    {
        if (fingerprint == null)
        {
            return 0.0;
        }

        double total = 0.0;
        int position = 0;
        while (position < fingerprint.Indices.Count && position < fingerprint.Counts.Count)
        {
            int index = fingerprint.Indices[position];
            total = total + (double)GetCountValue(fingerprint, index);
            position = position + 1;
        }

        return total;
    }

    internal double GetCountSquareTotal(CountFingerprint? fingerprint)
    {
        if (fingerprint == null)
        {
            return 0.0;
        }

        double total = 0.0;
        int position = 0;
        while (position < fingerprint.Indices.Count && position < fingerprint.Counts.Count)
        {
            int count = GetCountValue(fingerprint, fingerprint.Indices[position]);
            total = total + (double)count * (double)count;
            position = position + 1;
        }

        return total;
    }

    internal int GetCountValue(CountFingerprint? fingerprint, int index)
    {
        if (fingerprint == null || index < 0 || index >= fingerprint.Size)
        {
            return 0;
        }

        int position = 0;
        while (position < fingerprint.Indices.Count && position < fingerprint.Counts.Count)
        {
            if (fingerprint.Indices[position] == index)
            {
                int count = fingerprint.Counts[position];
                if (count > 0)
                {
                    return count;
                }

                return 0;
            }

            position = position + 1;
        }

        return 0;
    }

    internal int CountCommonSparseIndices(SparseFingerprint? left, SparseFingerprint? right)
    {
        if (left == null || right == null)
        {
            return 0;
        }

        int common = 0;
        int position = 0;
        while (position < left.Indices.Count)
        {
            int index = left.Indices[position];
            if (index >= 0 && index < left.Size && ContainsSparseIndex(right, index))
            {
                common = common + 1;
            }

            position = position + 1;
        }

        return common;
    }

    internal int CountSparseIndices(SparseFingerprint? fingerprint)
    {
        if (fingerprint == null)
        {
            return 0;
        }

        int count = 0;
        int position = 0;
        while (position < fingerprint.Indices.Count)
        {
            int index = fingerprint.Indices[position];
            if (index >= 0 && index < fingerprint.Size)
            {
                count = count + 1;
            }

            position = position + 1;
        }

        return count;
    }

    internal bool ContainsSparseIndex(SparseFingerprint? fingerprint, int index)
    {
        if (fingerprint == null || index < 0 || index >= fingerprint.Size)
        {
            return false;
        }

        int position = 0;
        while (position < fingerprint.Indices.Count)
        {
            if (fingerprint.Indices[position] == index)
            {
                return true;
            }

            position = position + 1;
        }

        return false;
    }

    internal int CountCommonBits(BitFingerprint left, BitFingerprint right)
    {
        int maximum = GetMaximumSize(left, right);
        int count = 0;
        int index = 0;
        while (index < maximum)
        {
            if (GetBit(left, index) && GetBit(right, index))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountUnionBits(BitFingerprint left, BitFingerprint right)
    {
        int maximum = GetMaximumSize(left, right);
        int count = 0;
        int index = 0;
        while (index < maximum)
        {
            if (GetBit(left, index) || GetBit(right, index))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountOnBits(BitFingerprint? fingerprint)
    {
        if (fingerprint == null)
        {
            return 0;
        }

        return fingerprint.CountOnBits();
    }

    internal int CountLeftOnlyBits(BitFingerprint left, BitFingerprint right)
    {
        int maximum = GetMaximumSize(left, right);
        int count = 0;
        int index = 0;
        while (index < maximum)
        {
            if (GetBit(left, index) && !GetBit(right, index))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool GetBit(BitFingerprint? fingerprint, int index)
    {
        if (fingerprint == null)
        {
            return false;
        }

        return fingerprint.GetBit(index);
    }

    internal int GetMaximumSize(BitFingerprint? left, BitFingerprint? right)
    {
        int leftSize = 0;
        int rightSize = 0;
        if (left != null)
        {
            leftSize = left.Size;
        }

        if (right != null)
        {
            rightSize = right.Size;
        }

        if (leftSize > rightSize)
        {
            return leftSize;
        }

        return rightSize;
    }
}
