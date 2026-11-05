using System.Collections.Generic;
using System.IO;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="MoleculeFormats"/>.
/// </summary>
public interface IMoleculeFormats
{
    /// <summary>
    /// Registers or replaces the molecular graph reader for a format kind.
    /// </summary>
    /// <param name="kind">The format kind.</param>
    /// <param name="reader">The reader implementation.</param>
    void RegisterReader(FormatKind kind, IMoleculeReader reader);

    /// <summary>
    /// Registers or replaces the molecular graph writer for a format kind.
    /// </summary>
    /// <param name="kind">The format kind.</param>
    /// <param name="writer">The writer implementation.</param>
    void RegisterWriter(FormatKind kind, IMoleculeWriter writer);

    /// <summary>
    /// Opens the reader.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule stream reader.</returns>
    MoleculeStreamReader OpenReader(TextReader? reader, FormatOptions? options);

    /// <summary>
    /// Opens the writer.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule stream writer.</returns>
    MoleculeStreamWriter OpenWriter(TextWriter? writer, FormatOptions? options);
}
