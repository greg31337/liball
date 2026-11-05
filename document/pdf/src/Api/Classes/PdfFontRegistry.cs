using System;
using System.Collections.Generic;
using System.IO;

namespace pdf;

/// <summary>
/// Represents a font registry used by the PDF generation API.
/// </summary>
public class PdfFontRegistry : IPdfFontRegistry
{
    internal List<PdfStandardFont> _fonts;
    internal List<PdfTrueTypeFont> _trueTypeFonts;
    /// <summary>
    /// Exposes the fonts currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfStandardFont> Fonts;
    /// <summary>
    /// Exposes the loaded TrueType and OpenType fonts currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfTrueTypeFont> TrueTypeFonts;

    /// <summary>
    /// Initializes a new font registry.
    /// </summary>
    public PdfFontRegistry()
    {
        _fonts = new List<PdfStandardFont>();
        _trueTypeFonts = new List<PdfTrueTypeFont>();
        Fonts = _fonts;
        TrueTypeFonts = _trueTypeFonts;
    }

    /// <summary>
    /// Registers a standard font for reuse while writing the document.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <param name="resourceName">The resource name to use for this operation.</param>
    /// <param name="baseFontName">The base font name to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a font name is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public void RegisterStandardFont(PdfFont font, string resourceName, string baseFontName)
    {
        if (HasFont(font))
        {
            throw new ArgumentException("Font is already registered.", "font");
        }

        _fonts.Add(new PdfStandardFont(font, resourceName, baseFontName));
    }

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from an in-memory byte array.
    /// </summary>
    /// <param name="fontData">The complete TrueType or OpenType font data.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when font data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    public PdfTrueTypeFont RegisterTrueTypeFont(byte[] fontData)
    {
        PdfTrueTypeFontLoader loader = new PdfTrueTypeFontLoader();
        return StoreTrueTypeFont(loader.Load(fontData));
    }

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from the current position of a readable stream.
    /// </summary>
    /// <param name="stream">The readable stream containing complete TrueType or OpenType font data.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    public PdfTrueTypeFont RegisterTrueTypeFontFromStream(Stream stream)
    {
        PdfTrueTypeFontLoader loader = new PdfTrueTypeFontLoader();
        return StoreTrueTypeFont(loader.LoadFromStream(stream));
    }

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from a file.
    /// </summary>
    /// <param name="path">The path to a TrueType or OpenType font file.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    public PdfTrueTypeFont RegisterTrueTypeFontFromFile(string path)
    {
        PdfTrueTypeFontLoader loader = new PdfTrueTypeFontLoader();
        return StoreTrueTypeFont(loader.LoadFromFile(path));
    }

    /// <summary>
    /// Determines whether a loaded font with the supplied deterministic identity is registered.
    /// </summary>
    /// <param name="identity">The SHA-256 font identity returned by the loader.</param>
    /// <returns>True when the font is registered; otherwise, false.</returns>
    public bool HasTrueTypeFont(string identity)
    {
        if (identity == null)
        {
            throw new ArgumentNullException("identity");
        }

        foreach (PdfTrueTypeFont font in _trueTypeFonts)
        {
            if (font.Identity == identity)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the registered TrueType or OpenType font with the supplied deterministic identity.
    /// </summary>
    /// <param name="identity">The SHA-256 font identity returned by the loader.</param>
    /// <returns>The registered font.</returns>
    /// <exception cref="ArgumentException">Thrown when the identity is not registered.</exception>
    public PdfTrueTypeFont GetTrueTypeFont(string identity)
    {
        if (identity == null)
        {
            throw new ArgumentNullException("identity");
        }

        foreach (PdfTrueTypeFont font in _trueTypeFonts)
        {
            if (font.Identity == identity)
            {
                return font;
            }
        }

        throw new ArgumentException("TrueType or OpenType font is not registered.", "identity");
    }

    /// <summary>
    /// Returns the number of distinct registered TrueType and OpenType fonts.
    /// </summary>
    /// <returns>The current registered font count.</returns>
    public int GetTrueTypeFontCount()
    {
        return _trueTypeFonts.Count;
    }

    internal PdfTrueTypeFont StoreTrueTypeFont(PdfTrueTypeFont font)
    {
        foreach (PdfTrueTypeFont existingFont in _trueTypeFonts)
        {
            if (existingFont.Identity == font.Identity)
            {
                return existingFont;
            }
        }

        _trueTypeFonts.Add(font);
        return font;
    }

    /// <summary>
    /// Determines whether the current object has font.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    public bool HasFont(PdfFont font)
    {
        foreach (PdfStandardFont standardFont in _fonts)
        {
            if (standardFont.Font == font)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the font calculated from the object's current state.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting standard font.</returns>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public PdfStandardFont GetFont(PdfFont font)
    {
        foreach (PdfStandardFont standardFont in _fonts)
        {
            if (standardFont.Font == font)
            {
                return standardFont;
            }
        }

        throw new ArgumentException("Font is not registered.", "font");
    }

    /// <summary>
    /// Returns the resource name calculated from the object's current state.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting text.</returns>
    public string GetResourceName(PdfFont font)
    {
        return GetFont(font).ResourceName;
    }

    /// <summary>
    /// Returns the base font name calculated from the object's current state.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting text.</returns>
    public string GetBaseFontName(PdfFont font)
    {
        return GetFont(font).BaseFontName;
    }

    /// <summary>
    /// Creates and returns a standard configured for this PDF workflow.
    /// </summary>
    /// <returns>The newly built font registry.</returns>
    internal static PdfFontRegistry CreateStandard()
    {
        PdfFontRegistry registry = new PdfFontRegistry();

        registry.RegisterStandardFont(PdfFont.Regular, "F1", "Helvetica");
        registry.RegisterStandardFont(PdfFont.Bold, "F2", "Helvetica-Bold");
        registry.RegisterStandardFont(PdfFont.Italic, "F3", "Helvetica-Oblique");
        registry.RegisterStandardFont(PdfFont.BoldItalic, "F4", "Helvetica-BoldOblique");
        registry.RegisterStandardFont(PdfFont.Helvetica, "F1", "Helvetica");
        registry.RegisterStandardFont(PdfFont.HelveticaBold, "F2", "Helvetica-Bold");
        registry.RegisterStandardFont(PdfFont.HelveticaOblique, "F3", "Helvetica-Oblique");
        registry.RegisterStandardFont(PdfFont.HelveticaBoldOblique, "F4", "Helvetica-BoldOblique");
        registry.RegisterStandardFont(PdfFont.Courier, "F5", "Courier");
        registry.RegisterStandardFont(PdfFont.CourierBold, "F6", "Courier-Bold");
        registry.RegisterStandardFont(PdfFont.CourierOblique, "F7", "Courier-Oblique");
        registry.RegisterStandardFont(PdfFont.CourierBoldOblique, "F8", "Courier-BoldOblique");
        registry.RegisterStandardFont(PdfFont.TimesRoman, "F9", "Times-Roman");
        registry.RegisterStandardFont(PdfFont.TimesBold, "F10", "Times-Bold");
        registry.RegisterStandardFont(PdfFont.TimesItalic, "F11", "Times-Italic");
        registry.RegisterStandardFont(PdfFont.TimesBoldItalic, "F12", "Times-BoldItalic");
        registry.RegisterStandardFont(PdfFont.Symbol, "F13", "Symbol");
        registry.RegisterStandardFont(PdfFont.ZapfDingbats, "F14", "ZapfDingbats");

        return registry;
    }

    /// <summary>
    /// Returns the standard resource name calculated from the object's current state.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting text.</returns>
    internal static string GetStandardResourceName(PdfFont font)
    {
        PdfFontRegistry registry = CreateStandard();
        return registry.GetResourceName(font);
    }

    /// <summary>
    /// Returns the standard base font name calculated from the object's current state.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting text.</returns>
    internal static string GetStandardBaseFontName(PdfFont font)
    {
        PdfFontRegistry registry = CreateStandard();
        return registry.GetBaseFontName(font);
    }
}
