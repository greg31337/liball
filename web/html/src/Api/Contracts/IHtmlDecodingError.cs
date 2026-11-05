using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlDecodingError"/>.</summary>
public interface IHtmlDecodingError
{
    /// <summary>Executes the GetCode operation.</summary>
    public HtmlDiagnosticCode GetCode();
    /// <summary>Executes the GetByteOffset operation.</summary>
    public int GetByteOffset();
    /// <summary>Executes the GetByteLength operation.</summary>
    public int GetByteLength();
}
