using System;
using System.Threading;
using System.Globalization;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerReport"/>.</summary>
public interface IHtmlSanitizerReport
{
    /// <summary>Executes the GetChanges operation.</summary>
    public HtmlSanitizerChange[] GetChanges();
    /// <summary>Executes the GetRetainedCount operation.</summary>
    public int GetRetainedCount();
    /// <summary>Executes the GetTotalCount operation.</summary>
    public long GetTotalCount();
    /// <summary>Executes the GetDroppedCount operation.</summary>
    public long GetDroppedCount();
    /// <summary>Executes the GetReasonCount operation.</summary>
    public long GetReasonCount(HtmlSanitizerChangeReason reason);
    /// <summary>Executes the WasTruncated operation.</summary>
    public bool WasTruncated();
    /// <summary>Executes the WasLimitExceeded operation.</summary>
    public bool WasLimitExceeded();
    /// <summary>Executes the WasCancelled operation.</summary>
    public bool WasCancelled();
    /// <summary>Reports whether report retention or sanitization output was partial.</summary>
    public bool IsPartial();
    /// <summary>Gets the stable code explaining why sanitization stopped.</summary>
    public HtmlDiagnosticCode GetFailureCode();
}
