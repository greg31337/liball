using System.IO;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="MoleculeStreamWriter"/>.
/// </summary>
public interface IMoleculeStreamWriter
{
    /// <summary>
    /// Open.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    void Open(TextWriter? writer, FormatOptions? options);

    /// <summary>
    /// Writes one molecule to the configured format or output stream.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    void Write(Molecule? molecule);
}

