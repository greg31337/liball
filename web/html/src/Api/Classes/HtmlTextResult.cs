using System;

namespace html;
/// <summary>Reports plain-text conversion success, completeness, output, and diagnostics.</summary>
public class HtmlTextResult : IHtmlTextResult
{
    internal bool success;
    internal bool complete;
    internal bool truncated;
    internal string text;
    internal HtmlDiagnostic[] diagnostics;
    internal long diagnosticCount;
    internal long droppedDiagnosticCount;
    internal HtmlTextResult(bool successValue, bool completeValue, bool truncatedValue, string textValue, HtmlDiagnostic[] diagnosticsValue, long diagnosticCountValue, long droppedDiagnosticCountValue)
    {
        if (textValue == null)
        {
            throw new ArgumentNullException("textValue");
        }

        if (diagnosticsValue == null)
        {
            throw new ArgumentNullException("diagnosticsValue");
        }

        if (diagnosticCountValue < 0 || droppedDiagnosticCountValue < 0 || droppedDiagnosticCountValue > diagnosticCountValue || diagnosticsValue.Length != diagnosticCountValue - droppedDiagnosticCountValue)
        {
            throw new ArgumentOutOfRangeException("diagnosticCountValue");
        }

        if (completeValue && truncatedValue)
        {
            throw new ArgumentException("A complete text result cannot be truncated.", "completeValue");
        }

        this.success = successValue;
        this.complete = completeValue;
        this.truncated = truncatedValue;
        this.text = textValue;
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

    /// <summary>Reports whether incomplete conversion retained safe text.</summary>
    /// <returns><c>true</c> when an incomplete result contains retained output; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return !this.complete && this.text.Length > 0;
    }

    /// <summary>Reports whether cancellation stopped conversion.</summary>
    /// <returns><c>true</c> when the result contains the cancellation diagnostic; otherwise, <c>false</c>.</returns>
    public bool WasCancelled()
    {
        return HtmlResultDiagnostics.ContainsCode(this.diagnostics,
            HtmlDiagnosticCode.OperationCancelled);
    }

    /// <summary>Gets the stable code explaining unsuccessful conversion.</summary>
    /// <returns>The primary failure code, or <see cref = "HtmlDiagnosticCode.None"/> when conversion succeeded.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.success && this.complete)
        {
            return HtmlDiagnosticCode.None;
        }

        if (this.WasCancelled())
        {
            return HtmlDiagnosticCode.OperationCancelled;
        }

        if (this.truncated)
        {
            return HtmlDiagnosticCode.TextConversionLimitExceeded;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            HtmlDiagnosticCode.OperationFailed);
    }

    /// <summary>Gets the plain-text output retained by the conversion.</summary>
    /// <returns>The complete output, or the safely retained prefix when a configured limit stopped conversion.</returns>
    public string GetText()
    {
        return this.text;
    }

    /// <summary>Gets the bounded diagnostics retained by this result.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets how many conversion diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets how many conversion diagnostics were produced before retention limits were applied.</summary>
    /// <returns>The total diagnostic count, including retained and dropped entries.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnosticCount;
    }

    /// <summary>Gets how many conversion diagnostics could not be retained.</summary>
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
                throw new ArgumentException("Text conversion diagnostics cannot contain null.", "diagnosticsValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
