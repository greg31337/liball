namespace ttf;

/// <summary>Identifies the PDF font resource and embedded program strategy.</summary>
public enum PdfFontKind
{
    /// <summary>Select a Type 0 resource appropriate for the font outlines.</summary>
    Automatic = 0,

    /// <summary>Use a Type 0 font with a CIDFontType2 descendant.</summary>
    Type0CidFontType2 = 1,

    /// <summary>Use a Type 0 font with a CIDFontType0 descendant.</summary>
    Type0CidFontType0 = 2,

    /// <summary>Use a Type 0 font backed by an OpenType FontFile3 program.</summary>
    Type0OpenType = 3,

    /// <summary>Use a restricted one-byte simple TrueType font.</summary>
    SimpleTrueType = 4
}
