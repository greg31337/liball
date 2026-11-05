namespace molecule;


/// <summary>
/// Defines the format facade used by streaming and application workflows.
/// </summary>
public interface IMoleculeFormatService
{
    /// <summary>
    /// Detects a supported molecule format from a caller hint, file name, or characteristic content.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>An object containing the detected format and diagnostics.</returns>
    FormatDetectionResult Detect(string? text, string? fileName);

    /// <summary>
    /// Determines whether the facade has a molecular graph reader for the format.
    /// </summary>
    /// <param name="kind">The format kind.</param>
    /// <returns><c>true</c> when a molecular graph reader is registered; otherwise, <c>false</c>.</returns>
    bool CanReadMolecule(FormatKind kind);

    /// <summary>
    /// Determines whether the facade has a molecular graph writer for the format.
    /// </summary>
    /// <param name="kind">The format kind.</param>
    /// <returns><c>true</c> when a molecular graph writer is registered; otherwise, <c>false</c>.</returns>
    bool CanWriteMolecule(FormatKind kind);

    /// <summary>
    /// Reads one molecule after selecting a format from the supplied options and content.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule.</returns>
    Molecule ReadString(string? text, FormatOptions? options);

    /// <summary>
    /// Reads one molecule using the explicitly selected format.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="kind">The format kind.</param>
    /// <returns>The resulting molecule.</returns>
    Molecule ReadStringAs(string? text, FormatKind kind);

    /// <summary>
    /// Writes one molecule using the format selected by the supplied options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteString(Molecule? molecule, FormatOptions? options);

    /// <summary>
    /// Writes one molecule using the explicitly selected format.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="kind">The format kind.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteStringAs(Molecule? molecule, FormatKind kind);
}
