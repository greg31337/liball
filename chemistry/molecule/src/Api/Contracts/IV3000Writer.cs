using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="V3000Writer"/>.
/// </summary>
public interface IV3000Writer
{
    /// <summary>
    /// Writes one molecule to the configured format or output stream.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string Write(Molecule? molecule);

    /// <summary>
    /// Writes MOL V3000 using explicit feature policies.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The molfile write options; <c>null</c> uses feature-preserving defaults.</param>
    /// <returns>The output text and diagnostics.</returns>
    MolfileWriteResult WriteV3000WithOptions(Molecule? molecule, MolfileWriteOptions? options);
}
