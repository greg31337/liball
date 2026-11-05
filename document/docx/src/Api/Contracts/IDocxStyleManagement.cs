namespace docx;

/// <summary>
/// Provides style discovery, relationship resolution, mutation, diagnostics, and import operations.
/// </summary>
public interface IDocxStyleManagement
{
    /// <summary>
    /// Returns all defined styles in document order.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>A new list containing all styles.</returns>
    List<DocxStyle> GetStyles(DocxStyles styles);

    /// <summary>
    /// Returns styles of one WordprocessingML type in document order.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="type">The style type to match.</param>
    /// <returns>A new list containing matching styles.</returns>
    List<DocxStyle> GetStylesByType(DocxStyles styles, string type);

    /// <summary>
    /// Finds a style by identifier.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="styleId">The style identifier to find.</param>
    /// <returns>The matching style, or null.</returns>
    DocxStyle? FindStyleById(DocxStyles styles, string styleId);

    /// <summary>
    /// Finds a style by name.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="name">The style name to find.</param>
    /// <returns>The matching style, or null.</returns>
    DocxStyle? FindStyleByName(DocxStyles styles, string name);

    /// <summary>
    /// Finds a style by identifier and type.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="styleId">The style identifier to find.</param>
    /// <param name="type">The style type to match.</param>
    /// <returns>The matching style, or null.</returns>
    DocxStyle? FindStyleByIdAndType(DocxStyles styles, string styleId, string type);

    /// <summary>
    /// Finds a style by name and type.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="name">The style name to find.</param>
    /// <param name="type">The style type to match.</param>
    /// <returns>The matching style, or null.</returns>
    DocxStyle? FindStyleByNameAndType(DocxStyles styles, string name, string type);

    /// <summary>
    /// Returns the default style for a type.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="type">The style type to match.</param>
    /// <returns>The default matching style, or null.</returns>
    DocxStyle? GetDefaultStyle(DocxStyles styles, string type);

    /// <summary>
    /// Returns all latent style exceptions in document order.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>A new list containing latent style exceptions.</returns>
    List<DocxLatentStyleException> GetLatentStyleExceptions(DocxStyles styles);

    /// <summary>
    /// Finds a latent style exception by name.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="name">The latent style name to find.</param>
    /// <returns>The matching latent style exception, or null.</returns>
    DocxLatentStyleException? FindLatentStyleException(DocxStyles styles, string name);

    /// <summary>
    /// Resolves the direct based-on style.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="style">The style whose relationship is resolved.</param>
    /// <returns>The based-on style, or null.</returns>
    DocxStyle? ResolveBasedOnStyle(DocxStyles styles, DocxStyle style);

    /// <summary>
    /// Returns the based-on ancestors nearest first, stopping safely at cycles.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="style">The style whose ancestors are resolved.</param>
    /// <returns>A new list containing based-on ancestors.</returns>
    List<DocxStyle> GetBasedOnChain(DocxStyles styles, DocxStyle style);

    /// <summary>
    /// Resolves the linked paragraph or character style.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="style">The style whose relationship is resolved.</param>
    /// <returns>The linked style, or null.</returns>
    DocxStyle? ResolveLinkedStyle(DocxStyles styles, DocxStyle style);

    /// <summary>
    /// Resolves the next paragraph style.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="style">The style whose relationship is resolved.</param>
    /// <returns>The next style, or null.</returns>
    DocxStyle? ResolveNextStyle(DocxStyles styles, DocxStyle style);

    /// <summary>
    /// Maps a built-in style identifier to its English WordprocessingML name.
    /// </summary>
    /// <param name="styleId">The built-in style identifier.</param>
    /// <returns>The mapped name, or the supplied identifier when no mapping is known.</returns>
    string GetBuiltInStyleName(string styleId);

    /// <summary>
    /// Maps an English built-in style name to its WordprocessingML identifier.
    /// </summary>
    /// <param name="name">The built-in style name.</param>
    /// <returns>The mapped identifier, or the supplied name when no mapping is known.</returns>
    string GetBuiltInStyleId(string name);

    /// <summary>
    /// Creates and persists a style definition.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="type">The style type.</param>
    /// <param name="styleId">The new style identifier.</param>
    /// <param name="name">The new style name.</param>
    /// <returns>The new style.</returns>
    DocxStyle CreateStyle(DocxDocument document, string type, string styleId, string name);

    /// <summary>
    /// Persists edits made to a style data object.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="style">The style to add or replace.</param>
    /// <returns>True when an existing style was replaced; otherwise false.</returns>
    bool UpdateStyle(DocxDocument document, DocxStyle style);

    /// <summary>
    /// Persists edits made to the document latent style defaults.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    void UpdateLatentStyles(DocxDocument document);

    /// <summary>
    /// Adds or replaces and persists one latent style exception.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="exception">The latent style exception to store.</param>
    void UpdateLatentStyleException(DocxDocument document, DocxLatentStyleException exception);

    /// <summary>
    /// Deletes and persists one latent style exception.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="name">The latent style name to delete.</param>
    /// <returns>True when an exception was deleted; otherwise false.</returns>
    bool DeleteLatentStyleException(DocxDocument document, string name);

    /// <summary>
    /// Reports references and document content that use a style.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="styleId">The style identifier to diagnose.</param>
    /// <returns>The usage and reference diagnostics.</returns>
    DocxValidationReport GetStyleDeleteDiagnostics(DocxDocument document, string styleId);

    /// <summary>
    /// Deletes a style and returns its pre-deletion usage diagnostics.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="styleId">The style identifier to delete.</param>
    /// <returns>The usage and reference diagnostics.</returns>
    DocxValidationReport DeleteStyle(DocxDocument document, string styleId);

    /// <summary>
    /// Reports whether a style can be imported without an identifier conflict.
    /// </summary>
    /// <param name="document">The target document.</param>
    /// <param name="style">The style to inspect.</param>
    /// <returns>The import diagnostics.</returns>
    DocxValidationReport GetStyleImportDiagnostics(DocxDocument document, DocxStyle style);

    /// <summary>
    /// Imports and persists one non-conflicting style.
    /// </summary>
    /// <param name="document">The target document.</param>
    /// <param name="style">The style to import.</param>
    /// <returns>True when the style was imported; otherwise false.</returns>
    bool ImportStyle(DocxDocument document, DocxStyle style);

    /// <summary>
    /// Imports and persists all non-conflicting styles from a styles model.
    /// </summary>
    /// <param name="document">The target document.</param>
    /// <param name="source">The source styles model.</param>
    /// <returns>The number of imported styles.</returns>
    int ImportStyles(DocxDocument document, DocxStyles source);
}
