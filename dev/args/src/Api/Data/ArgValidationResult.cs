using System.Collections.Generic;

namespace args;

/// <summary>
/// Contains validation errors and help or version short-circuit state.
/// </summary>
public class ArgValidationResult
{
    /// <summary>
    /// The parse result associated with this operation.
    /// </summary>
    public ArgParseResult ParseResult;
    /// <summary>
    /// The errors produced by this operation.
    /// </summary>
    public List<ArgParseError> Errors;
    /// <summary>
    /// The non-error diagnostics produced while parsing.
    /// </summary>
    public List<ArgDiagnostic> Diagnostics;
    /// <summary>
    /// Whether help or version handling intentionally bypassed ordinary validation and invocation.
    /// </summary>
    public bool ShortCircuited;
    /// <summary>
    /// Whether help output was requested.
    /// </summary>
    public bool HelpRequested;
    /// <summary>
    /// Whether version output was requested.
    /// </summary>
    public bool VersionRequested;
    /// <summary>
    /// The version text displayed for a version request.
    /// </summary>
    public string VersionText;

    internal ArgValidationResult(ArgParseResult parseResult)
    {
        if (parseResult == null)
        {
            throw new System.ArgumentNullException("parseResult");
        }

        ParseResult = parseResult;
        Errors = new List<ArgParseError>();
        Diagnostics = parseResult.Diagnostics;
        ShortCircuited = false;
        HelpRequested = false;
        VersionRequested = false;
        VersionText = "";
        int index = 0;
        while (index < parseResult.Errors.Count)
        {
            Errors.Add(parseResult.Errors[index]);
            index = index + 1;
        }
    }

    /// <summary>
    /// Returns whether this result contains one or more errors.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasErrors()
    {
        return Errors.Count > 0;
    }

    internal void MarkHelpRequested()
    {
        ShortCircuited = true;
        HelpRequested = true;
    }

    internal void MarkVersionRequested()
    {
        ShortCircuited = true;
        VersionRequested = true;
    }

    internal void MarkVersionRequestedWithText(string text)
    {
        ShortCircuited = true;
        VersionRequested = true;
        VersionText = text;
    }

    internal void AddOptionError(string code, string message, string symbolName)
    {
        Errors.Add(new ArgParseError(code, ArgSymbolKind.Option, message, -1, symbolName));
    }

    internal void AddOptionErrorAtToken(string code, string message, int tokenIndex, string symbolName)
    {
        Errors.Add(new ArgParseError(code, ArgSymbolKind.Option, message, tokenIndex, symbolName));
    }

    internal void AddArgumentError(string code, string message, string symbolName)
    {
        Errors.Add(new ArgParseError(code, ArgSymbolKind.Argument, message, -1, symbolName));
    }

    internal void AddArgumentErrorAtToken(string code, string message, int tokenIndex, string symbolName)
    {
        Errors.Add(new ArgParseError(code, ArgSymbolKind.Argument, message, tokenIndex, symbolName));
    }

    internal void AddCommandError(string code, string message, string symbolName)
    {
        Errors.Add(new ArgParseError(code, ArgSymbolKind.Command, message, -1, symbolName));
    }

    internal void AddCommandErrorAtToken(string code, string message, int tokenIndex, string symbolName)
    {
        Errors.Add(new ArgParseError(code, ArgSymbolKind.Command, message, tokenIndex, symbolName));
    }
}
