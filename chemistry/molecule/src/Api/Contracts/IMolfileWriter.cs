namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="MolfileWriter"/>.
/// </summary>
public interface IMolfileWriter
{

    /// <summary>
    /// Writes the v 3000.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteV3000(Molecule? molecule);

    /// <summary>
    /// Writes a molfile using automatic or explicitly selected version and feature policies.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The molfile write options; <c>null</c> uses automatic feature-preserving defaults.</param>
    /// <returns>The output text, selected version, and diagnostics.</returns>
    MolfileWriteResult WriteMolfileWithOptions(Molecule? molecule, MolfileWriteOptions? options);
}
