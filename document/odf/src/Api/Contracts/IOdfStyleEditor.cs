namespace odf;

/// <summary>
/// Finds, resolves, creates, copies, renames, and cleans document styles.
/// </summary>
public interface IOdfStyleEditor
{
    /// <summary>
    /// Finds a named or default style by family.
    /// </summary>
    /// <param name="document">The document to search.</param>
    /// <param name="name">The style name, or an empty string for a default style.</param>
    /// <param name="family">The style family.</param>
    /// <returns>The matching style, or <c>null</c>.</returns>
    public OdfStyle? FindStyle(
        OdfDocument document,
        string name,
        OdfStyleFamily family);
    /// <summary>
    /// Finds all styles in a family.
    /// </summary>
    /// <param name="document">The document to search.</param>
    /// <param name="family">The style family.</param>
    /// <returns>The matching styles in document order.</returns>
    public OdfStyle[] FindStyles(
        OdfDocument document,
        OdfStyleFamily family);
    /// <summary>
    /// Resolves the effective properties and inheritance sources of a style.
    /// </summary>
    /// <param name="document">The document containing the style.</param>
    /// <param name="style">The style to resolve.</param>
    /// <returns>The style resolution.</returns>
    public OdfStyleResolution ResolveEffectiveProperties(
        OdfDocument document,
        OdfStyle style);
    /// <summary>
    /// Creates a common, automatic, or default style from a definition.
    /// </summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="definition">The style definition to copy.</param>
    /// <returns>The independent style added to the document.</returns>
    public OdfStyle CreateStyle(
        OdfDocument document,
        OdfStyle definition);
    /// <summary>
    /// Synthesizes or reuses an equivalent automatic style.
    /// </summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="source">The style definition to synthesize.</param>
    /// <param name="preferredName">The preferred automatic style name.</param>
    /// <returns>The created or equivalent automatic style.</returns>
    public OdfStyle CreateAutomaticStyle(
        OdfDocument document,
        OdfStyle source,
        string preferredName);
    /// <summary>
    /// Applies a replacement definition while preserving the style identity object.
    /// </summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="style">The existing style to modify.</param>
    /// <param name="replacement">The replacement definition.</param>
    /// <returns>The modified style.</returns>
    public OdfStyle ModifyStyle(
        OdfDocument document,
        OdfStyle style,
        OdfStyle replacement);
    /// <summary>
    /// Copies a style and its style dependencies to another document.
    /// </summary>
    /// <param name="sourceDocument">The document containing the style.</param>
    /// <param name="targetDocument">The document receiving the copied styles.</param>
    /// <param name="style">The style to copy.</param>
    /// <param name="preferredName">The preferred name for the copied root style.</param>
    /// <returns>The copied root style.</returns>
    public OdfStyle CopyStyle(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfStyle style,
        string preferredName);
    /// <summary>
    /// Renames a style and updates matching typed and XML references.
    /// </summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="style">The style to rename.</param>
    /// <param name="newName">The new style name.</param>
    public void RenameStyle(
        OdfDocument document,
        OdfStyle style,
        string newName);
    /// <summary>
    /// Coalesces equivalent automatic styles and updates their references.
    /// </summary>
    /// <param name="document">The document to clean.</param>
    /// <returns>The styles removed from the document.</returns>
    public OdfStyle[] DeduplicateAutomaticStyles(OdfDocument document);
    /// <summary>
    /// Finds named styles without document or style references.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The unused styles in document order.</returns>
    public OdfStyle[] FindUnusedStyles(OdfDocument document);
    /// <summary>
    /// Removes currently unused named styles.
    /// </summary>
    /// <param name="document">The document to clean.</param>
    /// <returns>The styles removed from the document.</returns>
    public OdfStyle[] RemoveUnusedStyles(OdfDocument document);
}
