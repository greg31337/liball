namespace svg;

/// <summary>Contains the result of namespace scope processing and expanded-name assignment.</summary>
public class SvgXmlNamespaceResult
{
    /// <summary>Whether namespace processing succeeded.</summary>
    public bool Success;
    /// <summary>The stable namespace failure.</summary>
    public SvgXmlNamespaceError Error;
    /// <summary>The exact UTF-16 failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The one-based failure line, or zero.</summary>
    public int ErrorLine;
    /// <summary>The one-based failure column, or zero.</summary>
    public int ErrorColumn;
    /// <summary>The stable human-readable failure message.</summary>
    public string Message;

    /// <summary>Creates an XML namespace result.</summary>
    /// <param name="success">Whether processing succeeded.</param>
    /// <param name="error">The stable namespace failure.</param>
    /// <param name="errorOffset">The failure offset.</param>
    /// <param name="errorLine">The failure line.</param>
    /// <param name="errorColumn">The failure column.</param>
    /// <param name="message">The failure message.</param>
    public SvgXmlNamespaceResult(bool success, SvgXmlNamespaceError error, int errorOffset, int errorLine, int errorColumn, string message)
    {
        Success = success;
        Error = error;
        ErrorOffset = errorOffset;
        ErrorLine = errorLine;
        ErrorColumn = errorColumn;
        Message = message;
    }
}
