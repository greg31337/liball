namespace svg;

/// <summary>Contains the result of XML 1.0 well-formedness validation and value normalization.</summary>
public class SvgXmlValidationResult
{
    /// <summary>Whether validation and normalization succeeded.</summary>
    public bool Success;
    /// <summary>The stable validation failure.</summary>
    public SvgXmlValidationError Error;
    /// <summary>The exact UTF-16 failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The one-based failure line, or zero.</summary>
    public int ErrorLine;
    /// <summary>The one-based failure column, or zero.</summary>
    public int ErrorColumn;
    /// <summary>The stable human-readable failure message.</summary>
    public string Message;

    /// <summary>Creates an XML validation result.</summary>
    /// <param name="success">Whether validation succeeded.</param>
    /// <param name="error">The stable validation failure.</param>
    /// <param name="errorOffset">The failure offset.</param>
    /// <param name="errorLine">The failure line.</param>
    /// <param name="errorColumn">The failure column.</param>
    /// <param name="message">The failure message.</param>
    public SvgXmlValidationResult(bool success, SvgXmlValidationError error, int errorOffset, int errorLine, int errorColumn, string message)
    {
        Success = success;
        Error = error;
        ErrorOffset = errorOffset;
        ErrorLine = errorLine;
        ErrorColumn = errorColumn;
        Message = message;
    }
}
