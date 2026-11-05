using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Reports whether Markdown settings compiled successfully and provides diagnostics when they did not.</summary>
public class HtmlMarkdownCompileResult : IHtmlMarkdownCompileResult
{
    internal HtmlCompiledMarkdownOptions? options;
    internal HtmlDiagnostic[] diagnostics;
    internal long diagnosticCount;
    internal long droppedDiagnosticCount;
    internal HtmlMarkdownCompileResult(HtmlCompiledMarkdownOptions? optionsValue, HtmlDiagnostic[] diagnosticsValue, long diagnosticCountValue, long droppedDiagnosticCountValue)
    {
        if (diagnosticsValue == null)
        {
            throw new ArgumentNullException("diagnosticsValue");
        }

        if (diagnosticCountValue < 0 || droppedDiagnosticCountValue < 0 || droppedDiagnosticCountValue > diagnosticCountValue || diagnosticsValue.Length != diagnosticCountValue - droppedDiagnosticCountValue)
        {
            throw new ArgumentOutOfRangeException("diagnosticCountValue");
        }

        this.options = optionsValue;
        this.diagnostics = CopyDiagnostics(diagnosticsValue);
        this.diagnosticCount = diagnosticCountValue;
        this.droppedDiagnosticCount = droppedDiagnosticCountValue;
    }

    /// <summary>Reports whether the operation completed successfully.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.options != null;
    }

    /// <summary>Reports whether compilation returned partial options.</summary>
    /// <returns><c>false</c>; failed compilation never exposes compiled options.</returns>
    public bool IsPartial()
    {
        return false;
    }

    /// <summary>Reports whether cancellation stopped compilation.</summary>
    /// <returns><c>true</c> when diagnostics contain the cancellation code; otherwise, <c>false</c>.</returns>
    public bool WasCancelled()
    {
        return HtmlResultDiagnostics.ContainsCode(this.diagnostics,
            HtmlDiagnosticCode.OperationCancelled);
    }

    /// <summary>Gets the stable code explaining compilation failure.</summary>
    /// <returns>The primary failure code, or <see cref = "HtmlDiagnosticCode.None"/> when compilation succeeded.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.IsSuccess())
        {
            return HtmlDiagnosticCode.None;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            this.WasCancelled() ? HtmlDiagnosticCode.OperationCancelled
                : HtmlDiagnosticCode.MarkdownInvalidConfiguration);
    }

    /// <summary>Gets the reusable compiled configuration when validation succeeded.</summary>
    /// <returns>The compiled options. Returns null when validation failed.</returns>
    public HtmlCompiledMarkdownOptions? GetOptions()
    {
        return this.options;
    }

    /// <summary>Gets the bounded diagnostics retained by this result.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets how many compile diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets how many compilation diagnostics were produced before retention limits were applied.</summary>
    /// <returns>The total diagnostic count, including retained and dropped entries.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnosticCount;
    }

    /// <summary>Gets how many compilation diagnostics could not be retained.</summary>
    /// <returns>The number omitted from <see cref = "GetDiagnostics"/> because the diagnostic limit was exhausted.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return this.droppedDiagnosticCount;
    }

    /// <summary>Reports whether the diagnostic retention limit discarded entries.</summary>
    /// <returns><c>true</c> when one or more diagnostics were not retained; otherwise, <c>false</c>.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return this.droppedDiagnosticCount > 0;
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] values)
    {
        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Markdown diagnostics cannot contain null.", "diagnosticsValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
