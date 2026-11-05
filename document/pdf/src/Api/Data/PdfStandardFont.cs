using System;

namespace pdf;

/// <summary>
/// Represents a standard font used by the PDF generation API.
/// </summary>
public class PdfStandardFont
{
    /// <summary>
    /// Stores the font used by this object.
    /// </summary>
    public PdfFont Font;
    /// <summary>
    /// Stores the resource name used by this object.
    /// </summary>
    public string ResourceName;
    /// <summary>
    /// Stores the base font name used by this object.
    /// </summary>
    public string BaseFontName;

    /// <summary>
    /// Initializes a new standard font.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <param name="resourceName">The resource name to use for this operation.</param>
    /// <param name="baseFontName">The base font name to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a font name is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a font name is empty.</exception>
    public PdfStandardFont(PdfFont font, string resourceName, string baseFontName)
    {
        if (resourceName == null)
        {
            throw new ArgumentNullException("resourceName");
        }

        if (baseFontName == null)
        {
            throw new ArgumentNullException("baseFontName");
        }

        if (resourceName.Length == 0)
        {
            throw new ArgumentException("Font resource name cannot be empty.", "resourceName");
        }

        if (baseFontName.Length == 0)
        {
            throw new ArgumentException("Base font name cannot be empty.", "baseFontName");
        }

        Font = font;
        ResourceName = resourceName;
        BaseFontName = baseFontName;
    }
}
