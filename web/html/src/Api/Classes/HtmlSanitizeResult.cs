using System;
using System.Threading;

namespace html;
/// <summary>Reports sanitization success, truncation, output, diagnostics, and the bounded change report.</summary>
public class HtmlSanitizeResult : IHtmlSanitizeResult
{
    internal bool success;
    internal bool truncated;
    internal HtmlNode? node;
    internal string html;
    internal HtmlSanitizerReport report;
    internal HtmlDiagnostic[] diagnostics;
    internal long diagnosticCount;
    internal long droppedDiagnosticCount;
    internal HtmlSanitizeResult(bool successValue, bool truncatedValue,
        HtmlNode? nodeValue, string htmlValue,
        HtmlSanitizerReport reportValue, HtmlDiagnostic[] diagnosticsValue,
        long diagnosticCountValue, long droppedDiagnosticCountValue)
    {
        if (htmlValue == null)
        {
            throw new ArgumentNullException("htmlValue");
        }

        if (reportValue == null)
        {
            throw new ArgumentNullException("reportValue");
        }

        if (diagnosticsValue == null)
        {
            throw new ArgumentNullException("diagnosticsValue");
        }

        if (diagnosticCountValue < 0 || droppedDiagnosticCountValue < 0
            || droppedDiagnosticCountValue > diagnosticCountValue
            || diagnosticsValue.Length != diagnosticCountValue
                - droppedDiagnosticCountValue)
        {
            throw new ArgumentOutOfRangeException("diagnosticCountValue");
        }

        if (successValue && nodeValue == null)
        {
            throw new ArgumentException("A successful sanitizer result requires a node.", "nodeValue");
        }

        if (!successValue && (nodeValue != null || htmlValue.Length != 0))
        {
            throw new ArgumentException("A failed sanitizer result cannot expose partial output.", "nodeValue");
        }

        this.success = successValue;
        this.truncated = truncatedValue;
        this.node = nodeValue;
        this.html = htmlValue;
        this.report = reportValue;
        this.diagnostics = CopyDiagnostics(diagnosticsValue);
        this.diagnosticCount = diagnosticCountValue;
        this.droppedDiagnosticCount = droppedDiagnosticCountValue;
    }

    /// <summary>Reports whether the operation completed successfully.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.success;
    }

    /// <summary>Reports whether the result was truncated by a configured limit.</summary>
    /// <returns><c>true</c> when a configured limit truncated the result; otherwise, <c>false</c>.</returns>
    public bool WasTruncated()
    {
        return this.truncated;
    }

    /// <summary>Reports whether a successful sanitizer result is partial.</summary>
    /// <returns><c>true</c> when a safe result was returned with truncation; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return this.success && this.truncated;
    }

    /// <summary>Reports whether cancellation stopped sanitization.</summary>
    /// <returns><c>true</c> when the sanitizer report records cancellation; otherwise, <c>false</c>.</returns>
    public bool WasCancelled()
    {
        return this.report.WasCancelled();
    }

    /// <summary>Gets the stable code explaining sanitizer failure.</summary>
    /// <returns>The primary failure code, or <see cref = "HtmlDiagnosticCode.None"/> when sanitization succeeded.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.success)
        {
            return HtmlDiagnosticCode.None;
        }

        if (this.report.WasCancelled())
        {
            return HtmlDiagnosticCode.OperationCancelled;
        }

        if (this.report.WasLimitExceeded())
        {
            return HtmlDiagnosticCode.SanitizerLimitExceeded;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            HtmlDiagnosticCode.OperationFailed);
    }

    /// <summary>Gets the fully sanitized DOM output.</summary>
    /// <returns>The sanitized node. Returns null when sanitization failed.</returns>
    public HtmlNode? GetNode()
    {
        return this.node;
    }

    /// <summary>Gets the sanitized document produced by a document workflow.</summary>
    /// <returns>The cloned or in-place document when the output root is a document node; otherwise, null.</returns>
    public HtmlDocument? GetDocument()
    {
        if (this.node == null
            || this.node.GetKindStorage() != HtmlNodeKind.Document)
        {
            return null;
        }

        return this.node.GetOwnerDocumentStorage();
    }

    /// <summary>Gets the sanitized fragment produced by a fragment workflow.</summary>
    /// <returns>The sanitized fragment node, or null when the output has another kind or sanitization failed.</returns>
    public HtmlNode? GetFragment()
    {
        return this.node != null
                && this.node.GetKindStorage() == HtmlNodeKind.Fragment
            ? this.node : null;
    }

    /// <summary>Gets the sanitized subtree produced by a subtree workflow.</summary>
    /// <returns>The sanitized non-document, non-fragment node, or null when the output has another kind or sanitization failed.</returns>
    public HtmlNode? GetSubtree()
    {
        if (this.node == null
            || this.node.GetKindStorage() == HtmlNodeKind.Document
            || this.node.GetKindStorage() == HtmlNodeKind.Fragment)
        {
            return null;
        }

        return this.node;
    }

    /// <summary>Gets deterministic serialization of the sanitized node.</summary>
    /// <returns>The sanitized HTML, or an empty string when sanitization failed.</returns>
    public string GetHtml()
    {
        return this.html;
    }

    /// <summary>Gets the bounded audit report for policy-driven changes.</summary>
    /// <returns>The report, including truncation, limit, and cancellation state.</returns>
    public HtmlSanitizerReport GetReport()
    {
        return this.report;
    }

    /// <summary>Gets the bounded diagnostics retained by this result.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets how many sanitizer diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets the total sanitizer diagnostic count before retention limits were applied.</summary>
    /// <returns>The retained plus dropped diagnostic count.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnosticCount;
    }

    /// <summary>Gets how many sanitizer diagnostics were dropped.</summary>
    /// <returns>The count omitted because the retention limit was exhausted.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return this.droppedDiagnosticCount;
    }

    /// <summary>Reports whether sanitizer diagnostic retention was truncated.</summary>
    /// <returns><c>true</c> when diagnostics were dropped; otherwise, <c>false</c>.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return this.droppedDiagnosticCount > 0;
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] values)
    {
        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}
