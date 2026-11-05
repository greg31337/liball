using System;
using System.Threading;

namespace html;
/// <summary>Reports URL parsing or resolution output, classification flags, and diagnostics.</summary>
public class HtmlUrlResult : IHtmlUrlResult
{
    internal string url;
    internal string scheme;
    internal bool success;
    internal bool relative;
    internal bool deobfuscated;
    internal bool limitExceeded;
    internal HtmlDiagnostic[] diagnostics;
    internal HtmlUrlResult(string urlValue, string schemeValue, bool successValue, bool relativeValue, bool deobfuscatedValue, bool limitExceededValue, HtmlDiagnostic[] diagnosticsValue)
    {
        this.url = urlValue;
        this.scheme = schemeValue;
        this.success = successValue;
        this.relative = relativeValue;
        this.deobfuscated = deobfuscatedValue;
        this.limitExceeded = limitExceededValue;
        this.diagnostics = CopyDiagnostics(diagnosticsValue);
    }

    /// <summary>Reports whether the operation completed successfully.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.success;
    }

    /// <summary>Gets the normalized absolute URL produced by parsing or resolution.</summary>
    /// <returns>The absolute URL when <see cref = "IsSuccess"/> is <c>true</c>; otherwise, an empty string.</returns>
    public string GetUrl()
    {
        return this.url;
    }

    /// <summary>Gets the normalized URL scheme without its trailing colon.</summary>
    /// <returns>The lowercase scheme when <see cref = "IsSuccess"/> is <c>true</c>; otherwise, an empty string.</returns>
    public string GetScheme()
    {
        return this.scheme;
    }

    /// <summary>Reports whether the resolved URL originated from a relative reference.</summary>
    /// <returns><c>true</c> when the input was a relative URL reference; otherwise, <c>false</c>.</returns>
    public bool WasRelative()
    {
        return this.relative;
    }

    /// <summary>Reports whether ignored control or whitespace characters were removed while reading the scheme.</summary>
    /// <returns><c>true</c> when ignored characters were removed from the scheme; otherwise, <c>false</c>.</returns>
    public bool WasDeobfuscated()
    {
        return this.deobfuscated;
    }

    /// <summary>Reports whether processing exceeded a configured resource limit.</summary>
    /// <returns><c>true</c> when a configured limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    /// <summary>Reports whether a failed URL operation retained partial output.</summary>
    /// <returns><c>false</c>; URL failures expose no partially normalized URL.</returns>
    public bool IsPartial()
    {
        return false;
    }

    /// <summary>Reports whether cancellation stopped URL processing.</summary>
    /// <returns><c>false</c>; URL operations that observe cancellation do not return a result.</returns>
    public bool WasCancelled()
    {
        return false;
    }

    /// <summary>Gets the bounded diagnostics retained by this result.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets the stable code explaining URL failure.</summary>
    /// <returns>The primary diagnostic code, or <see cref = "HtmlDiagnosticCode.None"/> when processing succeeded.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.success)
        {
            return HtmlDiagnosticCode.None;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            this.limitExceeded ? HtmlDiagnosticCode.ResourceLimitExceeded
                : HtmlDiagnosticCode.UrlInvalid);
    }

    /// <summary>Gets how many URL diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets the total URL diagnostic count.</summary>
    /// <returns>The number of diagnostics produced by URL processing.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets how many URL diagnostics were dropped.</summary>
    /// <returns><c>0</c>; URL results retain every diagnostic they produce.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return 0;
    }

    /// <summary>Reports whether URL diagnostic retention was truncated.</summary>
    /// <returns><c>false</c>; URL results retain every diagnostic they produce.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return false;
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("diagnosticsValue");
        }

        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A URL diagnostic cannot be null.", "diagnosticsValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
