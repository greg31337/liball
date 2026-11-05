namespace ttf;

/// <summary>Identifies the FontDescriptor entry containing an embedded program.</summary>
public enum PdfEmbeddedFontFileKind
{
    /// <summary>A standalone TrueType program stored in <c>FontFile2</c>.</summary>
    FontFile2 = 0,

    /// <summary>A CFF or OpenType program stored in <c>FontFile3</c>.</summary>
    FontFile3 = 1
}
