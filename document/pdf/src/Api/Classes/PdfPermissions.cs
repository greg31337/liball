namespace pdf;

/// <summary>
/// Describes which operations an encrypted PDF reader should allow the document recipient to perform.
/// </summary>
public class PdfPermissions : IPdfPermissions
{
    /// <summary>
    /// Indicates whether compatible PDF readers should allow printing.
    /// </summary>
    internal bool AllowPrinting;
    /// <summary>
    /// Indicates whether compatible PDF readers should allow document modification.
    /// </summary>
    internal bool AllowModification;
    /// <summary>
    /// Indicates whether compatible PDF readers should allow content copying.
    /// </summary>
    internal bool AllowCopying;
    /// <summary>
    /// Indicates whether compatible PDF readers should allow annotation changes.
    /// </summary>
    internal bool AllowAnnotations;

    /// <summary>
    /// Initializes a permission set that allows printing, modification, copying, and annotations.
    /// </summary>
    public PdfPermissions()
    {
        AllowPrinting = true;
        AllowModification = true;
        AllowCopying = true;
        AllowAnnotations = true;
    }

    /// <summary>
    /// Clears the permission that allows printing in compatible PDF readers.
    /// </summary>
    public void DisallowPrinting()
    {
        AllowPrinting = false;
    }

    /// <summary>
    /// Clears the permission that allows modification in compatible PDF readers.
    /// </summary>
    public void DisallowModification()
    {
        AllowModification = false;
    }

    /// <summary>
    /// Clears the permission that allows copying in compatible PDF readers.
    /// </summary>
    public void DisallowCopying()
    {
        AllowCopying = false;
    }

    /// <summary>
    /// Clears the permission that allows annotations in compatible PDF readers.
    /// </summary>
    public void DisallowAnnotations()
    {
        AllowAnnotations = false;
    }

    /// <summary>
    /// Converts the object's current state to a permission value.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    public int ToPdfPermissionValue()
    {
        int value = -4;
        if (!AllowPrinting)
        {
            value = value & ~4;
        }

        if (!AllowModification)
        {
            value = value & ~8;
        }

        if (!AllowCopying)
        {
            value = value & ~16;
        }

        if (!AllowAnnotations)
        {
            value = value & ~32;
        }

        return value;
    }
}
