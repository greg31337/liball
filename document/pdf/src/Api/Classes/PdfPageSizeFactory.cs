using System;

namespace pdf;

/// <summary>
/// Creates standard, custom, portrait, and landscape PDF page dimensions.
/// </summary>
public class PdfPageSizeFactory : IPdfPageSizeFactory
{
    /// <summary>
    /// Creates a page-size factory.
    /// </summary>
    public PdfPageSizeFactory()
    {
    }

    /// <summary>
    /// Creates and returns a page dimensions configured for letter.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    public PdfPageDimensions Letter()
    {
        return new PdfPageDimensions(PdfPageSize.LetterWidth, PdfPageSize.LetterHeight);
    }

    /// <summary>
    /// Creates and returns a page dimensions configured for legal.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    public PdfPageDimensions Legal()
    {
        return new PdfPageDimensions(PdfPageSize.LegalWidth, PdfPageSize.LegalHeight);
    }

    /// <summary>
    /// Creates and returns a page dimensions configured for a3.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    public PdfPageDimensions A3()
    {
        return new PdfPageDimensions(PdfPageSize.A3Width, PdfPageSize.A3Height);
    }

    /// <summary>
    /// Creates and returns a page dimensions configured for a4.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    public PdfPageDimensions A4()
    {
        return new PdfPageDimensions(PdfPageSize.A4Width, PdfPageSize.A4Height);
    }

    /// <summary>
    /// Creates and returns a page dimensions configured for a5.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    public PdfPageDimensions A5()
    {
        return new PdfPageDimensions(PdfPageSize.A5Width, PdfPageSize.A5Height);
    }

    /// <summary>
    /// Creates and returns a page dimensions configured for receipt80.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    public PdfPageDimensions Receipt80()
    {
        return new PdfPageDimensions(PdfPageSize.Receipt80Width, PdfPageSize.Receipt80Height);
    }

    /// <summary>
    /// Creates and returns a page dimensions configured for receipt58.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    public PdfPageDimensions Receipt58()
    {
        return new PdfPageDimensions(PdfPageSize.Receipt58Width, PdfPageSize.Receipt58Height);
    }

    /// <summary>
    /// Creates and returns a page dimensions configured for custom.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page dimensions.</returns>
    public PdfPageDimensions Custom(double width, double height)
    {
        return new PdfPageDimensions(width, height);
    }

    /// <summary>
    /// Creates and returns a page dimensions configured for portrait.
    /// </summary>
    /// <param name="size">The size to use for this operation.</param>
    /// <returns>The resulting page dimensions.</returns>
    public PdfPageDimensions Portrait(PdfPageDimensions size)
    {
        if (size == null)
        {
            throw new ArgumentNullException("size");
        }

        if (size.Width <= size.Height)
        {
            return new PdfPageDimensions(size.Width, size.Height);
        }

        return new PdfPageDimensions(size.Height, size.Width);
    }

    /// <summary>
    /// Creates and returns a page dimensions configured for landscape.
    /// </summary>
    /// <param name="size">The size to use for this operation.</param>
    /// <returns>The resulting page dimensions.</returns>
    public PdfPageDimensions Landscape(PdfPageDimensions size)
    {
        if (size == null)
        {
            throw new ArgumentNullException("size");
        }

        if (size.Width >= size.Height)
        {
            return new PdfPageDimensions(size.Width, size.Height);
        }

        return new PdfPageDimensions(size.Height, size.Width);
    }
}
