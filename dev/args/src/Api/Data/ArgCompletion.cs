namespace args;

/// <summary>
/// Represents one command-line completion candidate.
/// </summary>
public class ArgCompletion
{
    /// <summary>
    /// The text inserted for this completion candidate.
    /// </summary>
    public string Text;
    /// <summary>
    /// The category represented by this value.
    /// </summary>
    public ArgCompletionKind Kind;
    /// <summary>
    /// Supporting text displayed beside the completion candidate when available.
    /// </summary>
    public string DescriptionText;
    /// <summary>
    /// The canonical name of the command, option, or argument that produced this candidate.
    /// </summary>
    public string SourceName;
    /// <summary>
    /// The command represented by this candidate, or null for non-command candidates.
    /// </summary>
    public ArgCommand? Command;
    /// <summary>
    /// The option associated with this object, or null when it does not apply.
    /// </summary>
    public ArgOption? Option;
    /// <summary>
    /// The positional argument associated with this object, or null when it does not apply.
    /// </summary>
    public ArgArgument? Argument;

    internal ArgCompletion(string text, ArgCompletionKind kind)
    {
        Text = text;
        Kind = kind;
        DescriptionText = "";
        SourceName = "";
        Command = null;
        Option = null;
        Argument = null;
    }

    internal ArgCompletion Description(string text)
    {
        DescriptionText = text;
        return this;
    }

    internal static ArgCompletion ForCommand(ArgCommand command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        ArgCompletion completion = new ArgCompletion(command.Name, ArgCompletionKind.Command);
        completion.DescriptionText = command.SummaryText;
        completion.SourceName = command.Name;
        completion.Command = command;
        return completion;
    }

    internal static ArgCompletion ForOption(ArgOption option, string alias)
    {
        if (option == null)
        {
            throw new System.ArgumentNullException("option");
        }

        ArgName.ValidateOptionAlias(alias);
        ArgCompletion completion = new ArgCompletion(alias, ArgCompletionKind.Option);
        completion.DescriptionText = option.DescriptionText;
        completion.SourceName = option.Name;
        completion.Option = option;
        return completion;
    }

    internal static ArgCompletion ForOptionValue(ArgOption option, string value)
    {
        if (option == null)
        {
            throw new System.ArgumentNullException("option");
        }

        ArgCompletion completion = new ArgCompletion(value, ArgCompletionKind.Value);
        completion.SourceName = option.Name;
        completion.Option = option;
        return completion;
    }

    internal static ArgCompletion ForArgumentValue(ArgArgument argument, string value)
    {
        if (argument == null)
        {
            throw new System.ArgumentNullException("argument");
        }

        ArgCompletion completion = new ArgCompletion(value, ArgCompletionKind.Value);
        completion.SourceName = argument.Name;
        completion.Argument = argument;
        return completion;
    }
}
