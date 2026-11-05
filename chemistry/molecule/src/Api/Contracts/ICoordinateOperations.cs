namespace molecule;


/// <summary>
/// Defines deterministic coordinate normalization and conformer alignment operations.
/// </summary>
public interface ICoordinateOperations
{
    /// <summary>
    /// Centers assigned atom coordinates, aligns their principal axis, and applies the requested two-dimensional scale policy.
    /// </summary>
    /// <param name="molecule">The molecule whose atom coordinates are normalized without mutating the input.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized molecule and applied transform.</returns>
    CoordinateNormalizationResult Normalize2D(Molecule? molecule, CoordinateNormalizationOptions? options);

    /// <summary>
    /// Aligns a moving conformer copy to a reference using mapped coordinates and a deterministic proper rotation.
    /// </summary>
    /// <param name="moving">The conformer to transform without mutating the input.</param>
    /// <param name="reference">The fixed reference conformer.</param>
    /// <param name="options">The mapping and scale options.</param>
    /// <returns>The aligned conformer, applied transform, RMSD, and diagnostics.</returns>
    ConformerAlignmentResult AlignConformer(Conformer? moving, Conformer? reference, ConformerAlignmentOptions? options);
}
