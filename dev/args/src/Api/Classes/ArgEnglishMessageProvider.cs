namespace args;

/// <summary>
/// Provides the complete built-in English command-line message set.
/// </summary>
public class ArgEnglishMessageProvider : IArgMessageProvider
{
    /// <summary>
    /// Creates an English message provider.
    /// </summary>
    public ArgEnglishMessageProvider()
    {
    }

    /// <summary>
    /// Returns the English message associated with a stable key, or null when the key is unknown.
    /// </summary>
    /// <param name="key">A key from <see cref="ArgMessageKeys"/>.</param>
    /// <param name="values">The positional values documented by the key.</param>
    /// <returns>The formatted English message, or null for an unknown key.</returns>
    public string? GetMessage(string key, string[] values)
    {
        if (key == ArgMessageKeys.HelpOptionDescription)
        {
            return "Show help.";
        }

        if (key == ArgMessageKeys.VersionOptionDescription)
        {
            return "Show version.";
        }

        if (key == ArgMessageKeys.HelpCommandName)
        {
            return "help";
        }

        if (key == ArgMessageKeys.UnexpectedOptionValue)
        {
            return "Option '" + Value(values, 0) + "' does not accept a value.";
        }

        if (key == ArgMessageKeys.MissingOptionValue)
        {
            return "Option '" + Value(values, 0) + "' requires a value.";
        }

        if (key == ArgMessageKeys.UnknownOption)
        {
            return "Unknown option '" + Value(values, 0) + "'.";
        }

        if (key == ArgMessageKeys.MissingRequiredOption)
        {
            return "Missing required option '" + Value(values, 0) + "'.";
        }

        if (key == ArgMessageKeys.MissingRequiredArgument)
        {
            return "Missing required argument '" + Value(values, 0) + "'.";
        }

        if (key == ArgMessageKeys.MissingRequiredSubcommand)
        {
            return "Missing required subcommand for command '" + Value(values, 0) + "'.";
        }

        if (key == ArgMessageKeys.OptionTooFewValues)
        {
            return "Option '" + Value(values, 0) + "' expects at least " + Value(values, 1) + ".";
        }

        if (key == ArgMessageKeys.OptionTooManyValues)
        {
            return "Option '" + Value(values, 0) + "' expects at most " + Value(values, 1) + ".";
        }

        if (key == ArgMessageKeys.ArgumentTooFewValues)
        {
            return "Argument '" + Value(values, 0) + "' expects at least " + Value(values, 1) + ".";
        }

        if (key == ArgMessageKeys.ArgumentTooManyValues)
        {
            return "Argument '" + Value(values, 0) + "' expects at most " + Value(values, 1) + ".";
        }

        if (key == ArgMessageKeys.OneValue)
        {
            return Value(values, 0) + " value";
        }

        if (key == ArgMessageKeys.MultipleValues)
        {
            return Value(values, 0) + " values";
        }

        if (key == ArgMessageKeys.DuplicateOption)
        {
            return "Option '" + Value(values, 0) + "' cannot be specified multiple times.";
        }

        if (key == ArgMessageKeys.UnknownCommand)
        {
            return "Unknown command '" + Value(values, 0) + "'.";
        }

        if (key == ArgMessageKeys.InvalidOptionChoice)
        {
            return "Option '" + Value(values, 0) + "' value '" + Value(values, 1) + "' is not one of the allowed choices.";
        }

        if (key == ArgMessageKeys.InvalidArgumentChoice)
        {
            return "Argument '" + Value(values, 0) + "' value '" + Value(values, 1) + "' is not one of the allowed choices.";
        }

        if (key == ArgMessageKeys.ConflictingOption)
        {
            return "Option '" + Value(values, 0) + "' cannot be used with option '" + Value(values, 1) + "'.";
        }

        if (key == ArgMessageKeys.MissingDependentOption)
        {
            return "Option '" + Value(values, 0) + "' requires option '" + Value(values, 1) + "'.";
        }

        if (key == ArgMessageKeys.MutuallyExclusiveOptionGroup)
        {
            return "Option group '" + Value(values, 0) + "' allows at most one option.";
        }

        if (key == ArgMessageKeys.MissingRequiredOptionGroup)
        {
            return "Option group '" + Value(values, 0) + "' requires at least one option.";
        }

        if (key == ArgMessageKeys.UsageLabel)
        {
            return "Usage:";
        }

        if (key == ArgMessageKeys.CommandsHeading)
        {
            return "Commands:";
        }

        if (key == ArgMessageKeys.ArgumentsHeading)
        {
            return "Arguments:";
        }

        if (key == ArgMessageKeys.OptionsHeading)
        {
            return "Options:";
        }

        if (key == ArgMessageKeys.OptionGroupsHeading)
        {
            return "Option groups:";
        }

        if (key == ArgMessageKeys.ExamplesHeading)
        {
            return "Examples:";
        }

        if (key == ArgMessageKeys.OptionsPlaceholder)
        {
            return "[options]";
        }

        if (key == ArgMessageKeys.RequiredCommandPlaceholder)
        {
            return "<command>";
        }

        if (key == ArgMessageKeys.OptionalCommandPlaceholder)
        {
            return "[command]";
        }

        if (key == ArgMessageKeys.RequiredDetail)
        {
            return "required";
        }

        if (key == ArgMessageKeys.DefaultDetail)
        {
            return "default: " + Value(values, 0);
        }

        if (key == ArgMessageKeys.EnvironmentDetail)
        {
            return "env: " + Value(values, 0);
        }

        if (key == ArgMessageKeys.ChoicesDetail)
        {
            return "choices: " + Value(values, 0);
        }

        if (key == ArgMessageKeys.MutuallyExclusiveDetail)
        {
            return "mutually exclusive";
        }

        if (key == ArgMessageKeys.AtLeastOneRequiredDetail)
        {
            return "at least one required";
        }

        if (key == ArgMessageKeys.ValuePlaceholder)
        {
            return "value";
        }

        if (key == ArgMessageKeys.UnsupportedCompletionShell)
        {
            return "Unsupported completion shell.";
        }

        return null;
    }

    internal string Value(string[] values, int index)
    {
        if (values == null || index < 0 || index >= values.Length || values[index] == null)
        {
            return "";
        }

        return values[index];
    }
}
