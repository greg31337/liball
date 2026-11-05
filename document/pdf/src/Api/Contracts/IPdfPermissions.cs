namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfPermissions"/>.
/// </summary>
public interface IPdfPermissions
{
    /// <summary>
    /// Clears the permission that allows printing in compatible PDF readers.
    /// </summary>
    void DisallowPrinting();

    /// <summary>
    /// Clears the permission that allows modification in compatible PDF readers.
    /// </summary>
    void DisallowModification();

    /// <summary>
    /// Clears the permission that allows copying in compatible PDF readers.
    /// </summary>
    void DisallowCopying();

    /// <summary>
    /// Clears the permission that allows annotations in compatible PDF readers.
    /// </summary>
    void DisallowAnnotations();

    /// <summary>
    /// Converts the object's current state to a permission value.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    int ToPdfPermissionValue();
}
