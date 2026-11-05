using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="SmartsParser"/>.
/// </summary>
public interface ISmartsParser
{
    /// <summary>
    /// Parses the supported text syntax and returns its object model.
    /// </summary>
    /// <param name="smarts">The smarts.</param>
    /// <returns>The resulting query molecule.</returns>
    QueryMolecule Parse(string? smarts);

    /// <summary>
    /// Parses the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="smarts">The smarts.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    SmartsParseResult ParseWithResult(string? smarts);
}

