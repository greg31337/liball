namespace odf;

/// <summary>
/// Describes the data and XForms bindings of a form control.
/// </summary>
public class OdfFormBinding
{
    /// <summary>The linked spreadsheet cell address.</summary>
    public string LinkedCell = "";
    /// <summary>The spreadsheet cell range supplying list values.</summary>
    public string SourceCellRange = "";
    /// <summary>The database field supplying the value.</summary>
    public string DataField = "";
    /// <summary>The database or application list source.</summary>
    public string ListSource = "";
    /// <summary>The list-source type token.</summary>
    public string ListSourceType = "";
    /// <summary>The bound-column token.</summary>
    public string BoundColumn = "";
    /// <summary>The referenced XForms bind identifier.</summary>
    public string XFormsBind = "";
    /// <summary>The referenced XForms list-source identifier.</summary>
    public string XFormsListSource = "";
    /// <summary>The referenced XForms submission identifier.</summary>
    public string XFormsSubmission = "";
    /// <summary>The resolved XForms bind.</summary>
    public OdfXFormsBind? XFormsBindTarget;
    /// <summary>The resolved XForms list-source bind.</summary>
    public OdfXFormsBind? XFormsListSourceTarget;
    /// <summary>The resolved XForms submission.</summary>
    public OdfXFormsSubmission? XFormsSubmissionTarget;
    /// <summary>Whether the non-empty XForms bind reference is unresolved.</summary>
    public bool IsXFormsBindBroken;
    /// <summary>Whether the non-empty XForms list-source reference is unresolved.</summary>
    public bool IsXFormsListSourceBroken;
    /// <summary>Whether the non-empty XForms submission reference is unresolved.</summary>
    public bool IsXFormsSubmissionBroken;
    internal string OriginalLinkedCell = "";
    internal string OriginalSourceCellRange = "";
    internal string OriginalDataField = "";
    internal string OriginalListSource = "";
    internal string OriginalListSourceType = "";
    internal string OriginalBoundColumn = "";
    internal string OriginalXFormsBind = "";
    internal string OriginalXFormsListSource = "";
    internal string OriginalXFormsSubmission = "";
}
