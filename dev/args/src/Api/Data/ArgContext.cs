namespace args;

/// <summary>
/// Provides the selected command and parsed values to command handlers.
/// </summary>
public class ArgContext
{
    /// <summary>
    /// The parse result associated with this operation.
    /// </summary>
    public ArgParseResult ParseResult;
    /// <summary>
    /// The value lookup and conversion facade for the parse result.
    /// </summary>
    public ArgValues Values;
    /// <summary>
    /// The deepest command selected by the parsed command path.
    /// </summary>
    public ArgCommand Command;

    internal ArgContext(ArgParseResult parseResult)
    {
        if (parseResult == null)
        {
            throw new System.ArgumentNullException("parseResult");
        }

        ParseResult = parseResult;
        Values = new ArgValues(parseResult);
        Command = parseResult.Command;
    }

    internal static ArgContext Empty()
    {
        ArgCommand command = new ArgCommand("command");
        ArgParseResult parseResult = new ArgParseResult(command, new System.Collections.Generic.List<ArgToken>());
        return new ArgContext(parseResult);
    }
}
