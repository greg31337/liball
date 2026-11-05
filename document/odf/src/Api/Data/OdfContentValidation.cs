namespace odf;

/// <summary>Describes a spreadsheet cell-content validation rule.</summary>
public class OdfContentValidation
{
    /// <summary>The validation name referenced by table cells.</summary>
    public string Name = "";
    /// <summary>The OpenFormula validation condition.</summary>
    public string Condition = "";
    /// <summary>The base cell address used to resolve relative references.</summary>
    public string BaseCellAddress = "";
    /// <summary>Whether an empty cell satisfies the validation.</summary>
    public bool AllowEmptyCell = true;
    /// <summary>The list-display behavior: none, unsorted, or sort-ascending.</summary>
    public string DisplayList = "none";
    /// <summary>Whether the validation has an input help message.</summary>
    public bool HasHelpMessage;
    /// <summary>Whether the input help message is displayed.</summary>
    public bool DisplayHelp;
    /// <summary>The input help-message title.</summary>
    public string HelpTitle = "";
    /// <summary>The plain text of the input help message.</summary>
    public string HelpText = "";
    /// <summary>Whether the validation has an error message.</summary>
    public bool HasErrorMessage;
    /// <summary>Whether the error message is displayed.</summary>
    public bool DisplayError;
    /// <summary>The error-message title.</summary>
    public string ErrorTitle = "";
    /// <summary>The plain text of the error message.</summary>
    public string ErrorText = "";
    /// <summary>The error-message type: stop, warning, or information.</summary>
    public string ErrorMessageType = "stop";
    /// <summary>The underlying content-validation element.</summary>
    public OdfNode? Node;
    /// <summary>The underlying help-message element, when present.</summary>
    public OdfNode? HelpMessageNode;
    /// <summary>The underlying error-message element, when present.</summary>
    public OdfNode? ErrorMessageNode;
    internal OdfNode? OriginalNode;
    internal string OriginalName = "";
    internal string OriginalCondition = "";
    internal string OriginalBaseCellAddress = "";
    internal bool OriginalAllowEmptyCell = true;
    internal string OriginalDisplayList = "none";
    internal bool OriginalHasHelpMessage;
    internal bool OriginalDisplayHelp;
    internal string OriginalHelpTitle = "";
    internal string OriginalHelpText = "";
    internal bool OriginalHasErrorMessage;
    internal bool OriginalDisplayError;
    internal string OriginalErrorTitle = "";
    internal string OriginalErrorText = "";
    internal string OriginalErrorMessageType = "stop";
}
