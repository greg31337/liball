namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="MolfileReader"/>.
/// </summary>
public interface IMolfileReader
{

    /// <summary>
    /// Reads the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    MolfileReadResult ReadWithResult(string? text);

    /// <summary>
    /// Reads a molfile using explicit version, feature, and count policies.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The molfile read options; <c>null</c> uses automatic feature-preserving defaults.</param>
    /// <returns>The molecule, selected version, feature counts, and diagnostics.</returns>
    MolfileReadResult ReadMolfileWithOptions(string? text, MolfileReadOptions? options);
}
