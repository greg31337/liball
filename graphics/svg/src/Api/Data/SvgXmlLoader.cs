namespace svg;

/// <summary>Contains a bounded XML-to-document construction result.</summary>
public class SvgXmlBuildResult
{
    /// <summary>Whether document construction succeeded.</summary>
    public bool Success;
    /// <summary>The stable construction failure.</summary>
    public SvgXmlBuildError Error;
    /// <summary>The constructed document or valid partial document.</summary>
    public SvgDocument? Document;
    /// <summary>The exact UTF-16 failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The one-based failure line, or zero.</summary>
    public int ErrorLine;
    /// <summary>The one-based failure column, or zero.</summary>
    public int ErrorColumn;
    /// <summary>The stable human-readable failure message.</summary>
    public string Message;

    /// <summary>Creates an XML construction result.</summary>
    /// <param name="success">Whether construction succeeded.</param>
    /// <param name="error">The stable construction failure.</param>
    /// <param name="document">The complete or partial document.</param>
    /// <param name="errorOffset">The failure offset.</param>
    /// <param name="errorLine">The failure line.</param>
    /// <param name="errorColumn">The failure column.</param>
    /// <param name="message">The failure message.</param>
    public SvgXmlBuildResult(bool success, SvgXmlBuildError error, SvgDocument? document, int errorOffset, int errorLine, int errorColumn, string message)
    {
        Success = success;
        Error = error;
        Document = document;
        ErrorOffset = errorOffset;
        ErrorLine = errorLine;
        ErrorColumn = errorColumn;
        Message = message;
    }
}
