using System.Collections.Generic;

namespace args;

/// <summary>
/// Contains the selected command, classified tokens, resolved values, and parse errors.
/// </summary>
public class ArgParseResult
{
    /// <summary>
    /// The deepest command selected from the input, or the root when no child command matched.
    /// </summary>
    public ArgCommand Command;
    /// <summary>
    /// The resolved command path from the root through the selected command.
    /// </summary>
    public List<ArgCommand> CommandPath;
    /// <summary>
    /// All input tokens in their original order.
    /// </summary>
    public List<ArgToken> Tokens;
    /// <summary>
    /// The resolved option and argument occurrences in encounter order.
    /// </summary>
    public List<ArgOccurrence> Occurrences;
    /// <summary>
    /// Tokens classified as positional input before argument assignment.
    /// </summary>
    public List<ArgToken> Positionals;
    /// <summary>
    /// Tokens retained because unmatched collection or stop-at-first-unmatched behavior was enabled.
    /// </summary>
    public List<ArgToken> UnmatchedTokens;
    /// <summary>
    /// The errors produced by this operation.
    /// </summary>
    public List<ArgParseError> Errors;
    /// <summary>
    /// The non-error diagnostics produced by this operation.
    /// </summary>
    public List<ArgDiagnostic> Diagnostics;
    /// <summary>
    /// The settings used for matching, tokenization, conversion, completion, and help behavior.
    /// </summary>
    public ArgParserSettings Settings;
    /// <summary>
    /// The version text resolved for this parse operation.
    /// </summary>
    internal string VersionText;
    /// <summary>
    /// Whether input used the built-in help-command form.
    /// </summary>
    public bool HelpCommandRequested;
    /// <summary>
    /// The zero-based index of the help-command token, or -1 when it was not used.
    /// </summary>
    public int HelpCommandTokenIndex;

    internal ArgParseResult(ArgCommand command, List<ArgToken> tokens)
    {
        Command = command;
        CommandPath = new List<ArgCommand>();
        CommandPath.Add(command);
        Tokens = tokens;
        Occurrences = new List<ArgOccurrence>();
        Positionals = new List<ArgToken>();
        UnmatchedTokens = new List<ArgToken>();
        Errors = new List<ArgParseError>();
        Diagnostics = new List<ArgDiagnostic>();
        Settings = new ArgParserSettings();
        VersionText = "";
        HelpCommandRequested = false;
        HelpCommandTokenIndex = -1;
    }

    /// <summary>
    /// Returns whether this result contains one or more errors.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasErrors()
    {
        return Errors.Count > 0;
    }

    internal void AddError(string message, int tokenIndex, string symbolName)
    {
        Errors.Add(new ArgParseError("parse_error", ArgSymbolKind.None, message, tokenIndex, symbolName));
    }

    internal void AddOptionError(string code, string message, int tokenIndex, string symbolName)
    {
        Errors.Add(new ArgParseError(code, ArgSymbolKind.Option, message, tokenIndex, symbolName));
    }

    internal void AddDiagnostic(string code, ArgSymbolKind symbolKind, string message, int tokenIndex, string symbolName)
    {
        Diagnostics.Add(new ArgDiagnostic(code, symbolKind, message, tokenIndex, symbolName));
    }

    internal void AddUnmatchedToken(ArgToken token)
    {
        int index = 0;
        while (index < UnmatchedTokens.Count)
        {
            if (UnmatchedTokens[index] == token)
            {
                return;
            }

            index = index + 1;
        }

        UnmatchedTokens.Add(token);
    }

    internal ArgOccurrence AddImpliedOption(ArgOption option, string value)
    {
        if (option == null)
        {
            throw new System.ArgumentNullException("option");
        }

        ArgOccurrence occurrence = new ArgOccurrence();
        occurrence.SymbolKind = ArgSymbolKind.Option;
        occurrence.SymbolName = option.Name;
        occurrence.Alias = "";
        occurrence.TokenIndex = -1;
        occurrence.Option = option;
        occurrence.Source = ArgValueSource.Implied;
        occurrence.Values.Add(value);
        Occurrences.Add(occurrence);
        return occurrence;
    }

    internal ArgOccurrence AddImpliedArgument(ArgArgument argument, string value)
    {
        if (argument == null)
        {
            throw new System.ArgumentNullException("argument");
        }

        ArgOccurrence occurrence = new ArgOccurrence();
        occurrence.SymbolKind = ArgSymbolKind.Argument;
        occurrence.SymbolName = argument.Name;
        occurrence.Alias = "";
        occurrence.TokenIndex = -1;
        occurrence.Argument = argument;
        occurrence.Source = ArgValueSource.Implied;
        occurrence.Values.Add(value);
        Occurrences.Add(occurrence);
        return occurrence;
    }

    /// <summary>
    /// Returns all occurrences of the named option in encounter order.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>A new list containing the matching occurrences.</returns>
    internal List<ArgOccurrence> OptionOccurrences(string name)
    {
        List<ArgOccurrence> matches = new List<ArgOccurrence>();
        int index = 0;
        while (index < Occurrences.Count)
        {
            ArgOccurrence occurrence = Occurrences[index];
            if (occurrence.SymbolKind == ArgSymbolKind.Option && occurrence.SymbolName == name)
            {
                matches.Add(occurrence);
            }

            index = index + 1;
        }

        return matches;
    }

    /// <summary>
    /// Returns the accumulated count for all occurrences of the named option.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The total count, or zero when the option is absent.</returns>
    internal int CountOption(string name)
    {
        int total = 0;
        int index = 0;
        while (index < Occurrences.Count)
        {
            ArgOccurrence occurrence = Occurrences[index];
            if (occurrence.SymbolKind == ArgSymbolKind.Option && occurrence.SymbolName == name)
            {
                total = total + occurrence.Count;
            }

            index = index + 1;
        }

        return total;
    }
}
