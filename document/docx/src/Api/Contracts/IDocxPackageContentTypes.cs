namespace docx;

/// <summary>
/// Provides lookup and mutation operations for package content-type declarations.
/// </summary>
public interface IDocxPackageContentTypes
{
    /// <summary>
    /// Returns the default content-type declarations in catalog order.
    /// </summary>
    /// <returns>A new list containing the default declarations.</returns>
    List<DocxContentTypeDefault> GetContentTypeDefaults();

    /// <summary>
    /// Returns the part-specific content-type declarations in catalog order.
    /// </summary>
    /// <returns>A new list containing the override declarations.</returns>
    List<DocxContentTypeOverride> GetContentTypeOverrides();

    /// <summary>
    /// Finds a default content-type declaration by file extension.
    /// </summary>
    /// <param name="extension">The file extension, with or without a leading period.</param>
    /// <returns>The matching declaration, or null when it is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the extension is empty or malformed.</exception>
    DocxContentTypeDefault? FindDefaultContentType(string extension);

    /// <summary>
    /// Finds a part-specific content-type declaration by package part name.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>The matching declaration, or null when it is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the part name is malformed.</exception>
    DocxContentTypeOverride? FindOverrideContentType(string partName);

    /// <summary>
    /// Creates a default content-type declaration.
    /// </summary>
    /// <param name="extension">The file extension, with or without a leading period.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>The created declaration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a declaration for the extension already exists.</exception>
    DocxContentTypeDefault CreateDefaultContentType(string extension, string contentType);

    /// <summary>
    /// Updates an existing default content-type declaration.
    /// </summary>
    /// <param name="extension">The file extension, with or without a leading period.</param>
    /// <param name="contentType">The replacement MIME content type.</param>
    /// <returns>Whether the declaration existed and was updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    bool UpdateDefaultContentType(string extension, string contentType);

    /// <summary>
    /// Removes a default content-type declaration.
    /// </summary>
    /// <param name="extension">The file extension, with or without a leading period.</param>
    /// <returns>Whether the declaration existed and was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the extension is empty or malformed.</exception>
    bool RemoveDefaultContentType(string extension);

    /// <summary>
    /// Creates a part-specific content-type declaration.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>The created declaration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a declaration for the part already exists.</exception>
    DocxContentTypeOverride CreateOverrideContentType(string partName, string contentType);

    /// <summary>
    /// Updates an existing part-specific content-type declaration.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The replacement MIME content type.</param>
    /// <returns>Whether the declaration existed and was updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    bool UpdateOverrideContentType(string partName, string contentType);

    /// <summary>
    /// Removes a part-specific content-type declaration.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>Whether the declaration existed and was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the part name is malformed.</exception>
    bool RemoveOverrideContentType(string partName);

    /// <summary>
    /// Resolves the effective content type for a package part, preferring a part-specific declaration over an extension default.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>The effective MIME content type, or an empty string when none is declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the part name is malformed.</exception>
    string ResolveContentType(string partName);
}
