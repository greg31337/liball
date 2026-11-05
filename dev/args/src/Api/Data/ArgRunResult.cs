using System.Collections.Generic;

namespace args;

/// <summary>
/// Contains the complete outcome of parsing, validating, and optionally invoking a command.
/// </summary>
public class ArgRunResult
{
    /// <summary>
    /// The parse result associated with this operation.
    /// </summary>
    public ArgParseResult ParseResult;
    /// <summary>
    /// The validation result associated with this run.
    /// </summary>
    public ArgValidationResult ValidationResult;
    /// <summary>
    /// The handler context created for the selected command.
    /// </summary>
    public ArgContext Context;
    /// <summary>
    /// The process-style exit code returned by invocation.
    /// </summary>
    public int ExitCode;
    /// <summary>
    /// Whether a command handler was actually called.
    /// </summary>
    public bool HandlerInvoked;
    /// <summary>
    /// Generated help text, or an empty string when help was not requested.
    /// </summary>
    public string HelpText;
    /// <summary>
    /// Resolved version text, or an empty string when version was not requested.
    /// </summary>
    public string VersionText;
    /// <summary>
    /// The non-error diagnostics produced while parsing.
    /// </summary>
    public List<ArgDiagnostic> Diagnostics;

    internal ArgRunResult(ArgParseResult parseResult, ArgValidationResult validationResult, ArgContext context, int exitCode, bool handlerInvoked)
    {
        if (parseResult == null)
        {
            throw new System.ArgumentNullException("parseResult");
        }

        if (validationResult == null)
        {
            throw new System.ArgumentNullException("validationResult");
        }

        if (context == null)
        {
            throw new System.ArgumentNullException("context");
        }

        ParseResult = parseResult;
        ValidationResult = validationResult;
        Context = context;
        ExitCode = exitCode;
        HandlerInvoked = handlerInvoked;
        HelpText = "";
        VersionText = validationResult.VersionText;
        Diagnostics = parseResult.Diagnostics;
    }

    internal static ArgRunResult Empty()
    {
        ArgContext context = ArgContext.Empty();
        ArgParseResult parseResult = context.ParseResult;
        ArgValidationResult validationResult = new ArgValidationResult(parseResult);
        return new ArgRunResult(parseResult, validationResult, context, 0, false);
    }

    /// <summary>
    /// Returns whether this result contains one or more errors.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasErrors()
    {
        return ValidationResult.HasErrors();
    }

    internal void SetHelpText(string text)
    {
        HelpText = text;
    }
}
