using System;
using System.Globalization;
using System.Threading;

namespace html;
/// <summary>Contains decoded or encoded entity text, completion state, and diagnostics.</summary>
public class HtmlEntityResult : IHtmlEntityResult
{
    internal string text;
    internal HtmlDiagnostic[] diagnostics;
    internal bool complete;
    internal bool limitExceeded;
    internal long diagnosticCount;
    internal long droppedDiagnosticCount;
    internal HtmlEntityResult(string text, HtmlDiagnostic[] diagnostics,
        bool complete, bool limitExceeded, long diagnosticCount,
        long droppedDiagnosticCount)
    {
        this.text = text;
        this.diagnostics = Copy(diagnostics);
        this.complete = complete;
        this.limitExceeded = limitExceeded;
        this.diagnosticCount = diagnosticCount;
        this.droppedDiagnosticCount = droppedDiagnosticCount;
    }

    internal static HtmlEntityResult Create(string text,
        HtmlDiagnostic[] diagnostics, bool complete, bool limitExceeded,
        long diagnosticCount, long droppedDiagnosticCount)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

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

        return new HtmlEntityResult(text, diagnostics, complete,
            limitExceeded, diagnosticCount, droppedDiagnosticCount);
    }

    /// <summary>Gets the decoded or encoded text produced by the operation.</summary>
    /// <returns>The complete output, or the safely retained prefix when a configured limit stopped processing.</returns>
    public string GetString()
    {
        return this.text;
    }

    /// <summary>Gets bounded diagnostics for malformed references and resource-limit failures.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return Copy(this.diagnostics);
    }

    /// <summary>Gets the stable code explaining an incomplete entity operation.</summary>
    /// <returns>The primary failure code, or <see cref = "HtmlDiagnosticCode.None"/> when complete.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.complete)
        {
            return HtmlDiagnosticCode.None;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            this.limitExceeded ? HtmlDiagnosticCode.ResourceLimitExceeded
                : HtmlDiagnosticCode.OperationFailed);
    }

    /// <summary>Gets how many diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets the total diagnostic count before retention limits were applied.</summary>
    /// <returns>The retained plus dropped diagnostic count.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnosticCount;
    }

    /// <summary>Gets how many diagnostics were dropped.</summary>
    /// <returns>The diagnostic count omitted because the retention limit was exhausted.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return this.droppedDiagnosticCount;
    }

    /// <summary>Reports whether diagnostic retention was truncated.</summary>
    /// <returns><c>true</c> when one or more diagnostics were dropped; otherwise, <c>false</c>.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return this.droppedDiagnosticCount > 0;
    }

    /// <summary>Reports whether the operation produced a complete result.</summary>
    /// <returns><c>true</c> when processing completed without truncation; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return this.complete;
    }

    /// <summary>Reports whether processing exceeded a configured resource limit.</summary>
    /// <returns><c>true</c> when a configured limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    /// <summary>Reports whether the retained string is a partial result.</summary>
    /// <returns><c>true</c> when output was retained before incomplete termination; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return !this.complete && this.text.Length > 0;
    }

    /// <summary>Reports whether cancellation stopped the operation.</summary>
    /// <returns><c>false</c>; entity operations that observe cancellation do not return a result.</returns>
    public bool WasCancelled()
    {
        return false;
    }

    internal static HtmlDiagnostic[] Copy(HtmlDiagnostic[] values)
    {
        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}
