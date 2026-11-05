using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="CanonicalSmilesWriter"/>.
/// </summary>
public interface ICanonicalSmilesWriter
{
    /// <summary>
    /// Writes one molecule to the configured format or output stream.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string Write(Molecule? molecule);

    /// <summary>
    /// Writes canonical SMILES including supported atom and bond stereochemistry.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteIsomeric(Molecule? molecule);

    /// <summary>
    /// Writes the canonical isomeric.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteCanonicalIsomeric(Molecule? molecule);

    /// <summary>
    /// Writes SMILES with explicit control over whether stereochemistry is included.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="includeStereo">The include stereo.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteWithOptions(Molecule? molecule, bool includeStereo);
}

