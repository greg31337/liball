using System;
using System.Threading;
using System.Globalization;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerChange"/>.</summary>
public interface IHtmlSanitizerChange
{
    /// <summary>Executes the GetKind operation.</summary>
    public HtmlSanitizerChangeKind GetKind();
    /// <summary>Executes the GetReason operation.</summary>
    public HtmlSanitizerChangeReason GetReason();
    /// <summary>Executes the GetReasonIdentifier operation.</summary>
    public string GetReasonIdentifier();
    /// <summary>Executes the GetOriginalName operation.</summary>
    public string GetOriginalName();
    /// <summary>Executes the WasNameRedacted operation.</summary>
    public bool WasNameRedacted();
    /// <summary>Executes the GetOriginalValue operation.</summary>
    public string GetOriginalValue();
    /// <summary>Executes the WasValueRedacted operation.</summary>
    public bool WasValueRedacted();
    /// <summary>Executes the GetSpan operation.</summary>
    public HtmlSourceSpan GetSpan();
}
