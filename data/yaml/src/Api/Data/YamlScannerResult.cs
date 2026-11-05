using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Contains scanner tokens and diagnostics produced from YAML text.
/// </summary>
public class YamlScannerResult
{
    internal string SourceText;
    internal int DiagnosticLimit;
    internal bool DiagnosticLimitReached;
    /// <summary>
    /// Whether the operation completed without errors.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The scanned tokens in source order.
    /// </summary>
    public List<YamlToken> Tokens;
    /// <summary>
    /// The diagnostics in the order they were reported.
    /// </summary>
    public List<YamlDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new YAML scanner result with empty or default state.
    /// </summary>
    public YamlScannerResult()
    {
        SourceText = "";
        DiagnosticLimit = 0;
        DiagnosticLimitReached = false;
        Success = true;
        Tokens = new List<YamlToken>();
        Diagnostics = new List<YamlDiagnostic>();
    }

    internal void ConfigureDiagnosticLimit(int limit)
    {
        DiagnosticLimit = limit;
    }

    internal void AddToken(YamlToken token)
    {
        if (token == null)
        {
            return;
        }

        Tokens.Add(token);
    }

    internal void AddDiagnostic(YamlDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            return;
        }

        if (DiagnosticLimit > 0 && Diagnostics.Count >= DiagnosticLimit)
        {
            DiagnosticLimitReached = true;
            if (diagnostic.IsError())
            {
                Success = false;
            }

            return;
        }

        Diagnostics.Add(diagnostic);
        if (diagnostic.IsError())
        {
            Success = false;
        }

        if (DiagnosticLimit > 0 && Diagnostics.Count >= DiagnosticLimit)
        {
            DiagnosticLimitReached = true;
        }
    }

    internal void AddError(string code, string message, YamlSourceSpan span)
    {
        AddDiagnostic(YamlDiagnostic.Error(code, message, span));
    }

    internal bool HasErrors()
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].IsError())
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
