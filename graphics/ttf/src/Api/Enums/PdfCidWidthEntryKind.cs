namespace ttf;

/// <summary>Identifies a compact PDF CID width entry representation.</summary>
public enum PdfCidWidthEntryKind
{
    /// <summary>One width applies to every CID from first through last.</summary>
    Range = 0,

    /// <summary>An array supplies one width for each consecutive CID.</summary>
    Array = 1
}
