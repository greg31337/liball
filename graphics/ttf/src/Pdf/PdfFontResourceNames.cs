namespace ttf;

internal static class PdfFontResourceNamesBuilder
{
    internal static PdfFontResourceNames Build(string baseFontName)
    {
        if (baseFontName == null)
        {
            throw new System.ArgumentNullException("baseFontName");
        }

        if (!SfntPostScriptNameNormalizer.IsValid(baseFontName))
        {
            throw FontFormatException.Create(
                "A PDF embedded font requires a valid PostScript base name.");
        }

        return new PdfFontResourceNames(new PdfName(baseFontName));
    }
}
