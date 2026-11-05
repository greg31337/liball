using System;
using System.Text;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlParseResult"/>.</summary>
public interface IHtmlParseResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Executes the IsComplete operation.</summary>
    public bool IsComplete();
    /// <summary>Executes the WasTruncated operation.</summary>
    public bool WasTruncated();
    /// <summary>Reports whether the result contains a safely inspectable incomplete tree.</summary>
    /// <returns><c>true</c> when parsing stopped after producing partial tree state; otherwise, <c>false</c>.</returns>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped parsing.</summary>
    /// <returns><c>true</c> when cancellation was observed before completion; otherwise, <c>false</c>.</returns>
    public bool WasCancelled();
    /// <summary>Gets the stable code explaining an unsuccessful parse.</summary>
    /// <returns>The primary failure code, or <see cref = "HtmlDiagnosticCode.None"/> for a successful parse.</returns>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Executes the GetDocument operation.</summary>
    public HtmlDocument GetDocument();
    /// <summary>Executes the GetFragment operation.</summary>
    public HtmlNode? GetFragment();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets how many parse diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount();
    /// <summary>Gets the total parse diagnostic count before retention limits were applied.</summary>
    /// <returns>The retained plus dropped diagnostic count.</returns>
    public long GetDiagnosticCount();
    /// <summary>Gets how many parse diagnostics were dropped.</summary>
    /// <returns>The diagnostic count omitted because the retention limit was exhausted.</returns>
    public long GetDroppedDiagnosticCount();
    /// <summary>Reports whether parse diagnostics were dropped.</summary>
    /// <returns><c>true</c> when the diagnostic retention limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WereDiagnosticsTruncated();
    /// <summary>Executes the GetDetectedEncoding operation.</summary>
    public Encoding? GetDetectedEncoding();
    /// <summary>Executes the GetDetectedEncodingLabel operation.</summary>
    public string GetDetectedEncodingLabel();
    /// <summary>Executes the GetEncodingDetection operation.</summary>
    public HtmlEncodingDetectionResult? GetEncodingDetection();
}
