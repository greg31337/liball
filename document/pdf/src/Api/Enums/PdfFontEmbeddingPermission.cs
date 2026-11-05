namespace pdf;

/// <summary>
/// Describes the embedding permission declared by an OpenType or TrueType font.
/// </summary>
public enum PdfFontEmbeddingPermission
{
    /// <summary>
    /// Allows installation and unrestricted embedding.
    /// </summary>
    Installable = 0,
    /// <summary>
    /// Prohibits embedding without permission from the font owner.
    /// </summary>
    Restricted = 1,
    /// <summary>
    /// Allows embedding for previewing and printing documents.
    /// </summary>
    PreviewAndPrint = 2,
    /// <summary>
    /// Allows embedding in documents that recipients may edit.
    /// </summary>
    Editable = 3,
    /// <summary>
    /// Allows only bitmap strikes to be embedded, not the outline font.
    /// </summary>
    BitmapOnly = 4
}
