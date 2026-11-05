using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlDecodedInput"/>.</summary>
public interface IHtmlDecodedInput
{
    /// <summary>Executes the IsSupported operation.</summary>
    public bool IsSupported();
    /// <summary>Reports whether decoding produced a complete supported result.</summary>
    public bool IsComplete();
    /// <summary>Reports whether decoding retained partial text.</summary>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped decoding.</summary>
    public bool WasCancelled();
    /// <summary>Gets the stable code explaining decoding failure.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Executes the GetEncodingName operation.</summary>
    public string GetEncodingName();
    /// <summary>Executes the GetString operation.</summary>
    public string GetString();
    /// <summary>Executes the GetCharacterCount operation.</summary>
    public int GetCharacterCount();
    /// <summary>Executes the GetByteCount operation.</summary>
    public int GetByteCount();
    /// <summary>Executes the GetByteOffset operation.</summary>
    public int GetByteOffset(int characterOffset);
    /// <summary>Executes the GetCharacterOffset operation.</summary>
    public int GetCharacterOffset(int byteOffset);
    /// <summary>Executes the HadErrors operation.</summary>
    public bool HadErrors();
    /// <summary>Executes the GetErrorCount operation.</summary>
    public int GetErrorCount();
    /// <summary>Gets how many decoding errors were retained.</summary>
    public int GetRetainedErrorCount();
    /// <summary>Gets how many decoding errors were dropped.</summary>
    public long GetDroppedErrorCount();
    /// <summary>Reports whether decoding error retention was truncated.</summary>
    public bool WereErrorsTruncated();
    /// <summary>Executes the GetErrors operation.</summary>
    public HtmlDecodingError[] GetErrors();
}
