namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Similarity"/>.
/// </summary>
/// <remarks>
/// Every metric returns one for two empty fingerprints and zero when exactly one fingerprint is empty. Indices outside the smaller fingerprint size are compared with zero.
/// </remarks>
public interface ISimilarity
{
    /// <summary>
    /// Calculates the Tanimoto similarity coefficient from the on bits in two fingerprints.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    double Tanimoto(BitFingerprint left, BitFingerprint right);

    /// <summary>
    /// Calculates the Dice similarity coefficient from the on bits in two fingerprints.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    double Dice(BitFingerprint left, BitFingerprint right);

    /// <summary>
    /// Cosine.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    double Cosine(BitFingerprint left, BitFingerprint right);

    /// <summary>
    /// Calculates asymmetric Tversky similarity using the supplied alpha and beta weights.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <param name="alpha">The alpha.</param>
    /// <param name="beta">The beta.</param>
    /// <returns>The calculated numeric value.</returns>
    double Tversky(BitFingerprint left, BitFingerprint right, double alpha, double beta);

    /// <summary>
    /// Calculates multiset Tanimoto similarity from counted fingerprint features.
    /// </summary>
    /// <param name="left">The left counted fingerprint.</param>
    /// <param name="right">The right counted fingerprint.</param>
    /// <returns>The sum of minimum counts divided by the sum of maximum counts.</returns>
    double TanimotoCount(CountFingerprint left, CountFingerprint right);

    /// <summary>
    /// Calculates multiset Dice similarity from counted fingerprint features.
    /// </summary>
    /// <param name="left">The left counted fingerprint.</param>
    /// <param name="right">The right counted fingerprint.</param>
    /// <returns>Twice the minimum-count overlap divided by the total feature count.</returns>
    double DiceCount(CountFingerprint left, CountFingerprint right);

    /// <summary>
    /// Calculates cosine similarity from counted fingerprint features.
    /// </summary>
    /// <param name="left">The left counted fingerprint.</param>
    /// <param name="right">The right counted fingerprint.</param>
    /// <returns>The count-vector dot product divided by the product of vector lengths.</returns>
    double CosineCount(CountFingerprint left, CountFingerprint right);

    /// <summary>
    /// Calculates asymmetric multiset Tversky similarity from counted fingerprint features.
    /// </summary>
    /// <param name="left">The left counted fingerprint.</param>
    /// <param name="right">The right counted fingerprint.</param>
    /// <param name="alpha">The weight applied to unmatched left occurrences.</param>
    /// <param name="beta">The weight applied to unmatched right occurrences.</param>
    /// <returns>The calculated similarity.</returns>
    double TverskyCount(CountFingerprint left, CountFingerprint right, double alpha, double beta);

    /// <summary>
    /// Calculates Tanimoto similarity from sparse fingerprint indices.
    /// </summary>
    /// <param name="left">The left sparse fingerprint.</param>
    /// <param name="right">The right sparse fingerprint.</param>
    /// <returns>The intersection size divided by the union size.</returns>
    double TanimotoSparse(SparseFingerprint left, SparseFingerprint right);

    /// <summary>
    /// Calculates Dice similarity from sparse fingerprint indices.
    /// </summary>
    /// <param name="left">The left sparse fingerprint.</param>
    /// <param name="right">The right sparse fingerprint.</param>
    /// <returns>Twice the intersection size divided by the total index count.</returns>
    double DiceSparse(SparseFingerprint left, SparseFingerprint right);

    /// <summary>
    /// Calculates cosine similarity from sparse fingerprint indices.
    /// </summary>
    /// <param name="left">The left sparse fingerprint.</param>
    /// <param name="right">The right sparse fingerprint.</param>
    /// <returns>The intersection size divided by the geometric mean of index counts.</returns>
    double CosineSparse(SparseFingerprint left, SparseFingerprint right);

    /// <summary>
    /// Calculates asymmetric Tversky similarity from sparse fingerprint indices.
    /// </summary>
    /// <param name="left">The left sparse fingerprint.</param>
    /// <param name="right">The right sparse fingerprint.</param>
    /// <param name="alpha">The weight applied to left-only indices.</param>
    /// <param name="beta">The weight applied to right-only indices.</param>
    /// <returns>The calculated similarity.</returns>
    double TverskySparse(SparseFingerprint left, SparseFingerprint right, double alpha, double beta);
}
