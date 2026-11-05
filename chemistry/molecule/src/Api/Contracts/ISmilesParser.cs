using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="SmilesParser"/>.
/// </summary>
public interface ISmilesParser
{

    /// <summary>
    /// Parses the supported text syntax and returns its object model.
    /// </summary>
    /// <param name="smiles">The SMILES text to parse.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule Parse(string? smiles);

    /// <summary>
    /// Parses the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="smiles">The SMILES text to parse.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    SmilesParseResult ParseWithResult(string? smiles);

    /// <summary>
    /// Parses the with recovery.
    /// </summary>
    /// <param name="smiles">The SMILES text to parse.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    SmilesParseResult ParseWithRecovery(string? smiles);

    /// <summary>
    /// Parses SMILES from a non-owning in-memory character span.
    /// </summary>
    /// <param name="smiles">The SMILES characters to parse.</param>
    /// <returns>The resulting molecule.</returns>
    Molecule ParseSmilesSpan(System.ReadOnlySpan<char> smiles);

    /// <summary>
    /// Parses SMILES from a character span and returns detailed diagnostics.
    /// </summary>
    /// <param name="smiles">The SMILES characters to parse.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    SmilesParseResult ParseSmilesSpanWithResult(System.ReadOnlySpan<char> smiles);

    /// <summary>
    /// Parses owned SMILES text using strict or recovery behavior selected by options.
    /// </summary>
    /// <param name="smiles">The SMILES text to parse.</param>
    /// <param name="options">The SMILES parsing options; <c>null</c> uses strict parsing.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    SmilesParseResult ParseSmilesWithOptions(string? smiles, SmilesReadOptions? options);

    /// <summary>
    /// Parses SMILES from a character span using strict or recovery behavior selected by options.
    /// </summary>
    /// <param name="smiles">The SMILES characters to parse.</param>
    /// <param name="options">The SMILES parsing options; <c>null</c> uses strict parsing.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    SmilesParseResult ParseSmilesSpanWithOptions(System.ReadOnlySpan<char> smiles, SmilesReadOptions? options);
}
