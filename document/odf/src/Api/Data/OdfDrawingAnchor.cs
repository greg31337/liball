namespace odf;

/// <summary>
/// Describes how a drawing object is anchored in text or spreadsheet content.
/// </summary>
public class OdfDrawingAnchor
{
    /// <summary>The typed anchor kind.</summary>
    public OdfTextAnchorKind Kind;
    /// <summary>The preserved <c>text:anchor-type</c> value.</summary>
    public string Type = "";
    /// <summary>The preserved anchor page number.</summary>
    public string PageNumberValue = "";
    /// <summary>The parsed positive anchor page number.</summary>
    public long PageNumber;
    /// <summary>Whether a page number was specified.</summary>
    public bool HasPageNumber;
    /// <summary>Whether the specified page number is valid.</summary>
    public bool IsPageNumberValid;
    /// <summary>The spreadsheet end-cell address.</summary>
    public string EndCellAddress = "";
    /// <summary>The horizontal offset within the spreadsheet end cell.</summary>
    public string EndX = "";
    /// <summary>The vertical offset within the spreadsheet end cell.</summary>
    public string EndY = "";
    /// <summary>The preserved table-background value.</summary>
    public string TableBackgroundValue = "";
    /// <summary>Whether the object is displayed as a table background.</summary>
    public bool IsTableBackground;
    /// <summary>Whether the table-background value is valid when specified.</summary>
    public bool IsTableBackgroundValid = true;
    internal OdfTextAnchorKind OriginalKind;
    internal string OriginalType = "";
    internal string OriginalPageNumberValue = "";
    internal string OriginalEndCellAddress = "";
    internal string OriginalEndX = "";
    internal string OriginalEndY = "";
    internal string OriginalTableBackgroundValue = "";
    internal long OriginalPageNumber;
    internal bool OriginalIsTableBackground;
}
