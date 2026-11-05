using System;
using System.Text;

namespace html;
/// <summary>Reports parse success, completeness, tree output, and diagnostics.</summary>
public class HtmlParseResult : IHtmlParseResult
{
    internal bool success;
    internal bool complete;
    internal bool truncated;
    internal bool cancelled;
    internal HtmlDocument document;
    internal HtmlNode? fragment;
    internal HtmlDiagnostic[] diagnostics;
    internal long diagnosticCount;
    internal long droppedDiagnosticCount;
    internal Encoding? detectedEncoding;
    internal string detectedEncodingLabel;
    internal HtmlEncodingDetectionResult? encodingDetection;
    internal HtmlParseResult(bool success, bool complete, bool truncated,
        bool cancelled, HtmlDocument document, HtmlNode? fragment,
        HtmlDiagnostic[] diagnostics, long diagnosticCount,
        long droppedDiagnosticCount, Encoding? detectedEncoding,
        string detectedEncodingLabel,
        HtmlEncodingDetectionResult? encodingDetection)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        if (diagnosticCount < 0 || droppedDiagnosticCount < 0
            || droppedDiagnosticCount > diagnosticCount
            || diagnostics.Length != diagnosticCount - droppedDiagnosticCount)
        {
            throw new ArgumentOutOfRangeException("diagnosticCount");
        }

        this.success = success;
        this.complete = complete;
        this.truncated = truncated;
        this.cancelled = cancelled;
        this.document = document;
        this.fragment = fragment;
        this.diagnostics = CopyDiagnostics(diagnostics);
        this.diagnosticCount = diagnosticCount;
        this.droppedDiagnosticCount = droppedDiagnosticCount;
        this.detectedEncoding = detectedEncoding == null ? null : (Encoding)detectedEncoding.Clone();
        this.detectedEncodingLabel = detectedEncodingLabel;
        this.encodingDetection = encodingDetection;
    }

    /// <summary>Reports whether the operation completed successfully.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.success;
    }

    /// <summary>Reports whether the operation produced a complete result.</summary>
    /// <returns><c>true</c> when processing completed without truncation; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return this.complete;
    }

    /// <summary>Reports whether the result was truncated by a configured limit.</summary>
    /// <returns><c>true</c> when a configured limit truncated the result; otherwise, <c>false</c>.</returns>
    public bool WasTruncated()
    {
        return this.truncated;
    }

    /// <summary>Reports whether the result contains a safely inspectable incomplete tree.</summary>
    /// <returns><c>true</c> when parsing stopped after producing partial tree state; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return !this.complete;
    }

    /// <summary>Reports whether cancellation stopped parsing.</summary>
    /// <returns><c>true</c> when cancellation was observed before completion; otherwise, <c>false</c>.</returns>
    public bool WasCancelled()
    {
        return this.cancelled;
    }

    /// <summary>Gets the stable code explaining an unsuccessful parse.</summary>
    /// <returns>The primary failure code, or <see cref = "HtmlDiagnosticCode.None"/> for a successful parse.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.success)
        {
            return HtmlDiagnosticCode.None;
        }

        if (this.cancelled)
        {
            return HtmlDiagnosticCode.OperationCancelled;
        }

        HtmlDiagnosticCode fallback = this.truncated
            ? HtmlDiagnosticCode.ResourceLimitExceeded
            : HtmlDiagnosticCode.OperationFailed;
        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            fallback);
    }

    /// <summary>Gets the document tree produced by document parsing.</summary>
    /// <returns>The document. Returns null when this result came from fragment parsing.</returns>
    public HtmlDocument GetDocument()
    {
        return this.document;
    }

    /// <summary>Gets the fragment node produced by context-sensitive fragment parsing.</summary>
    /// <returns>The fragment. Returns null when this result came from document parsing.</returns>
    public HtmlNode? GetFragment()
    {
        return this.fragment;
    }

    /// <summary>Gets the bounded diagnostics retained by this result.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets how many parse diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets the total parse diagnostic count before retention limits were applied.</summary>
    /// <returns>The retained plus dropped diagnostic count.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnosticCount;
    }

    /// <summary>Gets how many parse diagnostics were dropped.</summary>
    /// <returns>The diagnostic count omitted because the retention limit was exhausted.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return this.droppedDiagnosticCount;
    }

    /// <summary>Reports whether parse diagnostics were dropped.</summary>
    /// <returns><c>true</c> when the diagnostic retention limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return this.droppedDiagnosticCount > 0;
    }

    /// <summary>Gets the .NET encoding instance used to decode byte input.</summary>
    /// <returns>The detected encoding. Returns null when parsing character input that did not require byte decoding.</returns>
    public Encoding? GetDetectedEncoding()
    {
        return this.detectedEncoding == null ? null : (Encoding)this.detectedEncoding.Clone();
    }

    /// <summary>Gets the canonical decoder label recorded for byte input.</summary>
    /// <returns>The label, or an empty string when parsing character input.</returns>
    public string GetDetectedEncodingLabel()
    {
        return this.detectedEncodingLabel;
    }

    /// <summary>Gets how byte input selected its decoder.</summary>
    /// <returns>The encoding-detection result. Returns null when parsing character input.</returns>
    public HtmlEncodingDetectionResult? GetEncodingDetection()
    {
        return this.encodingDetection;
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] source)
    {
        HtmlDiagnostic[] copy = new HtmlDiagnostic[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }
}
