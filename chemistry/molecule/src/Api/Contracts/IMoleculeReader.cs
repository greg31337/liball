namespace molecule;


/// <summary>
/// Reads one molecular graph from text.
/// </summary>
public interface IMoleculeReader
{
    /// <summary>
    /// Reads one molecular graph from the supplied text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting molecule.</returns>
    Molecule Read(string? text);
}
