namespace ttf;

/// <summary>Describes the embedding-right result of a finalized PDF font.</summary>
public enum PdfFontEmbeddingRightsOutcome
{
    /// <summary>The source did not provide usable OS/2 embedding-right data.</summary>
    Unavailable = 0,

    /// <summary>The finalized embedding request does not conflict with known rights.</summary>
    Permitted = 1,

    /// <summary>A rights conflict was reported while preserving the requested output.</summary>
    ConflictReported = 2,

    /// <summary>No-subsetting rights changed the output to complete-font embedding.</summary>
    FullFontForced = 3
}
