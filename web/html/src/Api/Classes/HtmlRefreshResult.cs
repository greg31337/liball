using System;
using System.Threading;

namespace html;
/// <summary>Reports whether a refresh value parsed and provides its delay and optional URL.</summary>
public class HtmlRefreshResult : IHtmlRefreshResult
{
    internal bool success;
    internal bool hasUrl;
    internal bool limitExceeded;
    internal long delaySeconds;
    internal string url;
    internal HtmlDiagnostic[] diagnostics;
    internal long diagnosticCount;
    internal long droppedDiagnosticCount;
    internal bool diagnosticsTruncated;
    internal HtmlRefreshResult(bool successValue, bool hasUrlValue,
        bool limitExceededValue, long delaySecondsValue, string urlValue,
        HtmlDiagnostic[] diagnosticsValue, long diagnosticCountValue,
        long droppedDiagnosticCountValue, bool diagnosticsTruncatedValue)
    {
        this.success = successValue;
        this.hasUrl = hasUrlValue;
        this.limitExceeded = limitExceededValue;
        this.delaySeconds = delaySecondsValue;
        this.url = urlValue;
        this.diagnostics = HtmlUrlResult.CopyDiagnostics(diagnosticsValue);
        this.diagnosticCount = diagnosticCountValue;
        this.droppedDiagnosticCount = droppedDiagnosticCountValue;
        this.diagnosticsTruncated = diagnosticsTruncatedValue;
    }

    /// <summary>Reports whether the operation completed successfully.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.success;
    }

    /// <summary>Indicates whether this object has the URL.</summary>
    /// <returns><c>true</c> when the refresh value contains a URL; otherwise, <c>false</c>.</returns>
    public bool HasUrl()
    {
        return this.hasUrl;
    }

    /// <summary>Reports whether processing exceeded a configured resource limit.</summary>
    /// <returns><c>true</c> when a configured limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    /// <summary>Reports whether refresh parsing reached the end of the input without a resource limit.</summary>
    /// <returns><c>true</c> when parsing completed; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return !this.limitExceeded;
    }

    /// <summary>Gets the nonnegative whole-second delay parsed from the start of the refresh value.</summary>
    /// <returns>The delay in seconds, or <c>0</c> when parsing failed.</returns>
    public long GetDelaySeconds()
    {
        return this.delaySeconds;
    }

    /// <summary>Gets the optional URL portion after its surrounding whitespace and quotes are removed.</summary>
    /// <returns>The URL text, or an empty string when <see cref = "HasUrl"/> is <c>false</c>.</returns>
    public string GetUrl()
    {
        return this.url;
    }

    /// <summary>Gets the bounded diagnostics produced while parsing.</summary>
    /// <returns>A caller-owned snapshot; the array is empty for valid input.</returns>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return HtmlUrlResult.CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets the stable code explaining refresh parsing failure.</summary>
    /// <returns>The primary diagnostic code, or <see cref="HtmlDiagnosticCode.None"/> when parsing succeeded.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.success)
        {
            return HtmlDiagnosticCode.None;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            this.limitExceeded
                ? HtmlDiagnosticCode.MicrosyntaxLimitExceeded
                : HtmlDiagnosticCode.MicrosyntaxInvalid);
    }

    /// <summary>Gets how many refresh diagnostics were retained.</summary>
    /// <returns>The number of entries returned by <see cref="GetDiagnostics"/>.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets the total number of refresh diagnostics produced.</summary>
    /// <returns>The total diagnostic count before retention limits.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnosticCount;
    }

    /// <summary>Gets how many refresh diagnostics were dropped.</summary>
    /// <returns>The number omitted because of diagnostic limits.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return this.droppedDiagnosticCount;
    }

    /// <summary>Reports whether refresh diagnostic retention was truncated.</summary>
    /// <returns><c>true</c> when one or more diagnostics were omitted; otherwise, <c>false</c>.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return this.diagnosticsTruncated;
    }
}
