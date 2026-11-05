namespace template;

/// <summary>
/// Encodes ordinary template output for a selected output format.
/// </summary>
/// <remarks>
/// The renderer calls this service only when automatic escaping is enabled. Raw output and values marked safe bypass it.
/// </remarks>
public interface ITemplateOutputEncoder
{
    /// <summary>
    /// Encodes text for the requested output format without adding surrounding delimiters.
    /// </summary>
    /// <param name="text">The unencoded template value text.</param>
    /// <param name="mode">The output format selected for the active render scope.</param>
    /// <returns>The encoded output text.</returns>
    string Encode(string text, TemplateEscapingMode mode);
}
