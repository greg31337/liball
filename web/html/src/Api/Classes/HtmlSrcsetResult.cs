using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Reports parsed <c>srcset</c> candidates, diagnostics, and truncation state.</summary>
public class HtmlSrcsetResult : IHtmlSrcsetResult
{
    internal HtmlSrcsetCandidate[] candidates;
    internal int errorCount;
    internal bool limitExceeded;
    internal HtmlDiagnostic[] diagnostics;
    internal long diagnosticCount;
    internal long droppedDiagnosticCount;
    internal bool diagnosticsTruncated;
    internal HtmlSrcsetResult(HtmlSrcsetCandidate[] candidatesValue,
        int errorCountValue, bool limitExceededValue,
        HtmlDiagnostic[] diagnosticsValue, long diagnosticCountValue,
        long droppedDiagnosticCountValue, bool diagnosticsTruncatedValue)
    {
        this.candidates = CopyCandidates(candidatesValue);
        this.errorCount = errorCountValue;
        this.limitExceeded = limitExceededValue;
        this.diagnostics = HtmlUrlResult.CopyDiagnostics(diagnosticsValue);
        this.diagnosticCount = diagnosticCountValue;
        this.droppedDiagnosticCount = droppedDiagnosticCountValue;
        this.diagnosticsTruncated = diagnosticsTruncatedValue;
    }

    /// <summary>Reports whether every candidate parsed successfully and no resource limit was exceeded.</summary>
    /// <returns><c>true</c> when parsing completed without errors or a limit violation; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return this.errorCount == 0 && !this.limitExceeded;
    }

    /// <summary>Reports whether the operation produced a complete result.</summary>
    /// <returns><c>true</c> when processing completed without truncation; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return !this.limitExceeded;
    }

    /// <summary>Reports whether processing exceeded a configured resource limit.</summary>
    /// <returns><c>true</c> when a configured limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    /// <summary>Gets how many malformed candidates or descriptors the parser encountered.</summary>
    /// <returns>The nonnegative parse-error count; malformed candidates are omitted from the retained candidate array.</returns>
    public int GetErrorCount()
    {
        return this.errorCount;
    }

    /// <summary>Gets the successfully parsed candidates in their original source order.</summary>
    /// <returns>A snapshot of the candidates; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSrcsetCandidate[] GetCandidates()
    {
        return CopyCandidates(this.candidates);
    }

    /// <summary>Reports whether recoverable errors or limits left a partial candidate list.</summary>
    /// <returns><c>true</c> when at least one candidate was retained and the result is not valid; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return this.candidates.Length != 0 && !this.IsValid();
    }

    /// <summary>Gets detailed malformed-input and limit diagnostics.</summary>
    /// <returns>A caller-owned snapshot in source order.</returns>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return HtmlUrlResult.CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets the primary failure code.</summary>
    /// <returns>The primary diagnostic code, or <see cref="HtmlDiagnosticCode.None"/> for valid input.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.IsValid())
        {
            return HtmlDiagnosticCode.None;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            this.limitExceeded
                ? HtmlDiagnosticCode.MicrosyntaxLimitExceeded
                : HtmlDiagnosticCode.MicrosyntaxInvalid);
    }

    /// <summary>Gets how many diagnostics were retained.</summary>
    /// <returns>The number of entries returned by <see cref="GetDiagnostics"/>.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets the total number of diagnostics produced.</summary>
    /// <returns>The total diagnostic count before retention limits.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnosticCount;
    }

    /// <summary>Gets how many diagnostics were dropped.</summary>
    /// <returns>The number omitted because of diagnostic limits.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return this.droppedDiagnosticCount;
    }

    /// <summary>Reports whether diagnostic retention was truncated.</summary>
    /// <returns><c>true</c> when one or more diagnostics were omitted; otherwise, <c>false</c>.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return this.diagnosticsTruncated;
    }

    internal static HtmlSrcsetCandidate[] CopyCandidates(HtmlSrcsetCandidate[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("candidatesValue");
        }

        HtmlSrcsetCandidate[] result = new HtmlSrcsetCandidate[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A srcset candidate cannot be null.", "candidatesValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
