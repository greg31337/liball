namespace molecule;


/// <summary>
/// Writes one molecular graph as text.
/// </summary>
public interface IMoleculeWriter
{
    /// <summary>
    /// Writes one molecular graph as text.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string Write(Molecule? molecule);
}
