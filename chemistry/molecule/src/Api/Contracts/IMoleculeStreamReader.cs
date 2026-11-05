using System.Collections.Generic;
using System.IO;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="MoleculeStreamReader"/>.
/// </summary>
public interface IMoleculeStreamReader
{
    /// <summary>
    /// Open.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    void Open(TextReader? reader, FormatOptions? options);

    /// <summary>
    /// Reads the next.
    /// </summary>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    MoleculeStreamReadResult ReadNext();

    /// <summary>
    /// Reads all available records and returns their molecules in input order.
    /// </summary>
    /// <returns>The resulting values in deterministic order.</returns>
    List<Molecule> ReadAll();
}

