namespace docx;

/// <summary>
/// Provides package-part enumeration, content access, mutation, and orphan inspection.
/// </summary>
public interface IDocxPackageParts
{
    /// <summary>
    /// Returns the package parts in catalog order.
    /// </summary>
    /// <returns>A new list containing the package parts.</returns>
    List<DocxPart> GetParts();

    /// <summary>
    /// Reads an XML package part as text.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>The XML text stored in the part.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the requested part does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the requested part is not an XML part.</exception>
    string GetPartXml(string partName);

    /// <summary>
    /// Reads a package part as binary data.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>A copy of the bytes stored in the part.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the requested part does not exist.</exception>
    byte[] GetPartBytes(string partName);

    /// <summary>
    /// Adds an XML package part and registers its content type.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The MIME content type for the part.</param>
    /// <param name="xml">The XML text to store.</param>
    /// <returns>The added package part.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the package part already exists.</exception>
    DocxPart AddXmlPartWithContent(string partName, string contentType, string xml);

    /// <summary>
    /// Adds a binary package part and registers its content type.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The MIME content type for the part.</param>
    /// <param name="bytes">The binary data to store. The data is copied.</param>
    /// <returns>The added package part.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the package part already exists.</exception>
    DocxPart AddBinaryPart(string partName, string contentType, byte[] bytes);

    /// <summary>
    /// Replaces an XML package part and updates its content type.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The MIME content type for the part.</param>
    /// <param name="xml">The replacement XML text.</param>
    /// <returns>Whether the requested part existed and was replaced.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    bool ReplaceXmlPart(string partName, string contentType, string xml);

    /// <summary>
    /// Replaces a binary package part and updates its content type.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The MIME content type for the part.</param>
    /// <param name="bytes">The replacement binary data. The data is copied.</param>
    /// <returns>Whether the requested part existed and was replaced.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    bool ReplaceBinaryPart(string partName, string contentType, byte[] bytes);

    /// <summary>
    /// Removes a package part and its part-specific content-type registration.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>Whether the requested part existed and was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the part name is malformed.</exception>
    bool RemovePart(string partName);

    /// <summary>
    /// Returns parts that cannot be reached from package-level internal relationships.
    /// </summary>
    /// <returns>A new list containing the orphaned package parts.</returns>
    List<DocxPart> GetOrphanParts();

    /// <summary>
    /// Indicates whether an existing package part cannot be reached from package-level internal relationships.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>Whether the existing part is orphaned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the part name is malformed.</exception>
    bool IsOrphanPart(string partName);
}
