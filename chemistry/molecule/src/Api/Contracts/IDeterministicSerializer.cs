using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="DeterministicSerializer"/>.
/// </summary>
public interface IDeterministicSerializer
{

    /// <summary>
    /// Writes the complete molecular graph in stable canonical order using the library-specific text format.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string Serialize(Molecule? molecule);

    /// <summary>
    /// Reads the library-specific deterministic text representation into a molecule.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule Deserialize(string? text);

    /// <summary>
    /// Parses the supported text syntax and returns its object model.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    DeterministicParseResult Parse(string? text);
}
