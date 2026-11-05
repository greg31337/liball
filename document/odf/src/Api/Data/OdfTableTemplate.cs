namespace odf;

/// <summary>Describes an ODF table-template style assignment.</summary>
public class OdfTableTemplate
{
    /// <summary>The template name.</summary>
    public string Name = "";
    /// <summary>The first-row start boundary token.</summary>
    public string FirstRowStartColumn = "";
    /// <summary>The first-row end boundary token.</summary>
    public string FirstRowEndColumn = "";
    /// <summary>The last-row start boundary token.</summary>
    public string LastRowStartColumn = "";
    /// <summary>The last-row end boundary token.</summary>
    public string LastRowEndColumn = "";
    /// <summary>The table-cell style for the first row.</summary>
    public string FirstRowStyleName = "";
    /// <summary>The paragraph style for the first row.</summary>
    public string FirstRowParagraphStyleName = "";
    /// <summary>The table-cell style for the last row.</summary>
    public string LastRowStyleName = "";
    /// <summary>The paragraph style for the last row.</summary>
    public string LastRowParagraphStyleName = "";
    /// <summary>The table-cell style for the first column.</summary>
    public string FirstColumnStyleName = "";
    /// <summary>The paragraph style for the first column.</summary>
    public string FirstColumnParagraphStyleName = "";
    /// <summary>The table-cell style for the last column.</summary>
    public string LastColumnStyleName = "";
    /// <summary>The paragraph style for the last column.</summary>
    public string LastColumnParagraphStyleName = "";
    /// <summary>The table-cell style for the table body.</summary>
    public string BodyStyleName = "";
    /// <summary>The paragraph style for the table body.</summary>
    public string BodyParagraphStyleName = "";
    /// <summary>The table-cell style for even rows.</summary>
    public string EvenRowsStyleName = "";
    /// <summary>The paragraph style for even rows.</summary>
    public string EvenRowsParagraphStyleName = "";
    /// <summary>The table-cell style for odd rows.</summary>
    public string OddRowsStyleName = "";
    /// <summary>The paragraph style for odd rows.</summary>
    public string OddRowsParagraphStyleName = "";
    /// <summary>The table-cell style for even columns.</summary>
    public string EvenColumnsStyleName = "";
    /// <summary>The paragraph style for even columns.</summary>
    public string EvenColumnsParagraphStyleName = "";
    /// <summary>The table-cell style for odd columns.</summary>
    public string OddColumnsStyleName = "";
    /// <summary>The paragraph style for odd columns.</summary>
    public string OddColumnsParagraphStyleName = "";
    /// <summary>The table style used for the template background.</summary>
    public string BackgroundStyleName = "";
    /// <summary>The XML part containing the template.</summary>
    public string PartPath = "";
    /// <summary>The underlying table-template element.</summary>
    public OdfNode? Node;
    internal string OriginalSignature = "";
}
