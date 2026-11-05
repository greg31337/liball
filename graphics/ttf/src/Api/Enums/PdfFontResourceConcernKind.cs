namespace ttf;

/// <summary>Identifies one requested-profile concern for a PDF font resource.</summary>
public enum PdfFontResourceConcernKind
{
    /// <summary>PDF/A was requested while a reported rights conflict remains.</summary>
    PdfAEmbeddingRightsConflict = 1,

    /// <summary>PDF/A-1 cannot represent an OpenType FontFile3 resource.</summary>
    PdfA1OpenTypeFontProgram = 2,

    /// <summary>PDF/UA was requested but allocated codes lack original Unicode.</summary>
    PdfUaMissingUnicodeMappings = 3,

    /// <summary>PDF/UA was requested and ambiguous text needs ActualText.</summary>
    PdfUaActualTextRequired = 4,

    /// <summary>The target PDF version predates the selected font strategy.</summary>
    PdfVersionTooLow = 5
}
