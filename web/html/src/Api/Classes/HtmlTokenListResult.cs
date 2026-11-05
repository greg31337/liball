using System;

namespace html;
/// <summary>Reports bounded token-list parsing output and completion state.</summary>
public class HtmlTokenListResult : IHtmlTokenListResult
{
    internal string[] tokens;
    internal bool limitExceeded;
    internal HtmlDiagnostic[] diagnostics;

    internal HtmlTokenListResult(string[] tokensValue,
        bool limitExceededValue, HtmlDiagnostic[] diagnosticsValue)
    {
        this.tokens = CopyTokens(tokensValue);
        this.limitExceeded = limitExceededValue;
        this.diagnostics = HtmlUrlResult.CopyDiagnostics(diagnosticsValue);
    }

    /// <summary>Reports whether parsing completed without a resource limit.</summary>
    /// <returns><c>true</c> when the complete input was parsed; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return !this.limitExceeded;
    }

    /// <summary>Reports whether parsing exceeded a configured limit.</summary>
    /// <returns><c>true</c> when a limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    /// <summary>Gets the tokens retained before completion or a limit.</summary>
    /// <returns>A caller-owned token snapshot.</returns>
    public string[] GetTokens()
    {
        return CopyTokens(this.tokens);
    }

    /// <summary>Gets bounded parsing diagnostics.</summary>
    /// <returns>A caller-owned diagnostic snapshot.</returns>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return HtmlUrlResult.CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets the stable failure code.</summary>
    /// <returns><see cref="HtmlDiagnosticCode.None"/> on completion; otherwise, the limit code.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        return this.limitExceeded
            ? HtmlDiagnosticCode.MicrosyntaxLimitExceeded
            : HtmlDiagnosticCode.None;
    }

    internal static string[] CopyTokens(string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("tokensValue");
        }

        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A token cannot be null.",
                    "tokensValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
