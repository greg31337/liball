using System.Collections.Generic;

namespace args;

/// <summary>
/// Generates context-sensitive completion candidates from a command model and partial arguments.
/// </summary>
public class ArgCompletionProvider : IArgCompletionProvider
{
    /// <summary>
    /// The settings used for matching, tokenization, completion, and help behavior.
    /// </summary>
    internal ArgParserSettings Settings;

    /// <summary>
    /// Creates a completion provider with default parser settings.
    /// </summary>
    public ArgCompletionProvider()
    {
        Settings = new ArgParserSettings();
    }

    /// <summary>
    /// Replaces the parser settings used for completion and returns the same object.
    /// </summary>
    /// <param name="settings">The settings instance to retain.</param>
    /// <returns>The same completion provider.</returns>
    /// <exception cref="System.ArgumentNullException">The settings value is null.</exception>
    public ArgCompletionProvider UseSettings(ArgParserSettings settings)
    {
        if (settings == null)
        {
            throw new System.ArgumentNullException("settings");
        }

        Settings = settings;
        return this;
    }

    /// <summary>
    /// Returns the active parser settings.
    /// </summary>
    /// <returns>The retained settings instance.</returns>
    public ArgParserSettings GetSettings()
    {
        return Settings;
    }

    /// <summary>
    /// Resolves the partial command line and returns visible command, option, or configured value candidates for its final token.
    /// </summary>
    /// <param name="command">The command model to inspect or complete.</param>
    /// <param name="argv">The raw command-line arguments, excluding the executable name.</param>
    /// <returns>The completion candidates valid at the current input position.</returns>
    /// <exception cref="System.ArgumentNullException">The command or argument array is null.</exception>
    public List<ArgCompletion> Complete(ArgCommand command, string[] argv)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        if (argv == null)
        {
            throw new System.ArgumentNullException("argv");
        }

        string incomplete = IncompleteToken(argv);
        ArgCommand current = ResolveCurrentCommand(command, argv);
        ArgOption? valueOption = ValueOption(current, argv);
        if (valueOption != null)
        {
            return OptionValueCompletions(valueOption, incomplete);
        }

        if (IsOptionLike(incomplete))
        {
            return OptionCompletions(current, incomplete);
        }

        List<ArgCompletion> completions = new List<ArgCompletion>();
        AddCommandCompletions(completions, current, incomplete);
        AddOptionCompletions(completions, current, incomplete);
        AddArgumentValueCompletions(completions, current, argv, incomplete);
        return completions;
    }

    /// <summary>
    /// Generates a self-contained completion script from the visible commands, options, and static values in the command model.
    /// </summary>
    /// <param name="command">The root command or one of its descendants.</param>
    /// <param name="shell">The shell syntax to generate.</param>
    /// <returns>A deterministic completion script that can be loaded by the selected shell.</returns>
    /// <exception cref="System.ArgumentNullException">The command is null.</exception>
    /// <exception cref="System.ArgumentException">The shell value is unsupported.</exception>
    public string GenerateScript(ArgCommand command, ArgCompletionShell shell)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        if (shell != ArgCompletionShell.Bash && shell != ArgCompletionShell.Zsh && shell != ArgCompletionShell.Fish && shell != ArgCompletionShell.PowerShell)
        {
            throw new System.ArgumentException(ArgMessageLookup.Get(Settings, ArgMessageKeys.UnsupportedCompletionShell, new string[] { }), "shell");
        }

        ArgCommand root = CommandRoot(command);
        return new ArgCompletionScriptBuilder(this, root).Build(shell);
    }

    internal string IncompleteToken(string[] argv)
    {
        if (argv.Length == 0)
        {
            return "";
        }

        return argv[argv.Length - 1];
    }

    internal int CompletedTokenCount(string[] argv)
    {
        if (argv.Length == 0)
        {
            return 0;
        }

        return argv.Length - 1;
    }

    internal ArgCommand ResolveCurrentCommand(ArgCommand command, string[] argv)
    {
        ArgCommand current = command;
        int count = CompletedTokenCount(argv);
        int index = 0;
        while (index < count)
        {
            string token = argv[index];
            if (IsOptionLike(token))
            {
                return current;
            }

            ArgCommand? next = FindSubcommand(current, token);
            if (next == null)
            {
                return current;
            }

            current = next;
            index = index + 1;
        }

        return current;
    }

    internal ArgCommand? FindSubcommand(ArgCommand command, string name)
    {
        int index = 0;
        while (index < command.Commands.Count)
        {
            ArgCommand child = command.Commands[index];
            if (CommandMatchesNameOrAlias(child, name))
            {
                return child;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool CommandMatchesNameOrAlias(ArgCommand command, string name)
    {
        if (TextEquals(command.Name, name, Settings.CommandMatchingCaseInsensitive()))
        {
            return true;
        }

        int index = 0;
        while (index < command.Aliases.Count)
        {
            if (TextEquals(command.Aliases[index], name, Settings.CommandMatchingCaseInsensitive()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal List<ArgCompletion> OptionCompletions(ArgCommand command, string prefix)
    {
        List<ArgCompletion> completions = new List<ArgCompletion>();
        AddOptionCompletions(completions, command, prefix);
        return completions;
    }

    internal void AddCommandCompletions(List<ArgCompletion> completions, ArgCommand command, string prefix)
    {
        int index = 0;
        while (index < command.Commands.Count)
        {
            ArgCommand child = command.Commands[index];
            if (!child.IsHidden)
            {
                AddCommandCandidate(completions, child, child.Name, prefix);
                int aliasIndex = 0;
                while (aliasIndex < child.Aliases.Count)
                {
                    AddCommandCandidate(completions, child, child.Aliases[aliasIndex], prefix);
                    aliasIndex = aliasIndex + 1;
                }
            }

            index = index + 1;
        }
    }

    internal void AddCommandCandidate(List<ArgCompletion> completions, ArgCommand command, string text, string prefix)
    {
        if (!StartsWith(text, prefix, Settings.CommandMatchingCaseInsensitive()))
        {
            return;
        }

        ArgCompletion completion = new ArgCompletion(text, ArgCompletionKind.Command);
        completion.DescriptionText = command.SummaryText;
        completion.SourceName = command.Name;
        completion.Command = command;
        completions.Add(completion);
    }

    internal void AddOptionCompletions(List<ArgCompletion> completions, ArgCommand command, string prefix)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            if (!option.IsHidden)
            {
                AddOptionAliasCompletions(completions, option, prefix);
            }

            index = index + 1;
        }

        ArgCommand? current = command;
        while (current != null)
        {
            index = 0;
            while (index < current.GlobalOptions.Count)
            {
                ArgOption option = current.GlobalOptions[index];
                if (!option.IsHidden)
                {
                    AddOptionAliasCompletions(completions, option, prefix);
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        AddDefaultHelpOptionCompletions(completions, command, prefix);
        AddDefaultVersionOptionCompletions(completions, command, prefix);
    }

    internal void AddOptionAliasCompletions(List<ArgCompletion> completions, ArgOption option, string prefix)
    {
        int index = 0;
        while (index < option.Aliases.Count)
        {
            string alias = option.Aliases[index];
            if (AliasCanBeCompleted(alias) && StartsWith(alias, prefix, Settings.OptionMatchingCaseInsensitive()))
            {
                completions.Add(ArgCompletion.ForOption(option, alias));
            }

            index = index + 1;
        }
    }

    internal void AddDefaultHelpOptionCompletions(List<ArgCompletion> completions, ArgCommand command, string prefix)
    {
        int index = 0;
        while (index < Settings.HelpAliases.Count)
        {
            string alias = Settings.HelpAliases[index];
            if (AliasCanBeCompleted(alias) && !HasVisibleOptionAlias(command, alias) && StartsWith(alias, prefix, Settings.OptionMatchingCaseInsensitive()))
            {
                ArgOption option = ArgOption.Empty();
                option.Name = Settings.HelpAliases[0];
                option.Aliases.Add(alias);
                option.Description(ArgMessageLookup.Get(Settings, ArgMessageKeys.HelpOptionDescription, new string[] { }));
                completions.Add(ArgCompletion.ForOption(option, alias));
            }

            index = index + 1;
        }
    }

    internal void AddDefaultVersionOptionCompletions(List<ArgCompletion> completions, ArgCommand command, string prefix)
    {
        if (ArgVersionText.ResolveCommand(command).Length == 0)
        {
            return;
        }

        AddDefaultVersionAliasCompletion(completions, command, "--version", prefix);
        AddDefaultVersionAliasCompletion(completions, command, "-V", prefix);
    }

    internal void AddDefaultVersionAliasCompletion(List<ArgCompletion> completions, ArgCommand command, string alias, string prefix)
    {
        if (!AliasCanBeCompleted(alias))
        {
            return;
        }

        if (HasVisibleOptionAlias(command, alias))
        {
            return;
        }

        if (!StartsWith(alias, prefix, Settings.OptionMatchingCaseInsensitive()))
        {
            return;
        }

        ArgOption option = new ArgOption("--version");
        option.Alias("-V");
        option.Description(ArgMessageLookup.Get(Settings, ArgMessageKeys.VersionOptionDescription, new string[] { }));
        completions.Add(ArgCompletion.ForOption(option, alias));
    }

    internal bool AliasCanBeCompleted(string alias)
    {
        if (alias.Length == 0)
        {
            return true;
        }

        if (alias[0] == '/' && !Settings.SlashOptionsEnabled())
        {
            return false;
        }

        return true;
    }

    internal bool HasVisibleOptionAlias(ArgCommand command, string alias)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            if (!option.IsHidden && OptionMatchesAlias(option, alias))
            {
                return true;
            }

            index = index + 1;
        }

        ArgCommand? current = command;
        while (current != null)
        {
            index = 0;
            while (index < current.GlobalOptions.Count)
            {
                ArgOption option = current.GlobalOptions[index];
                if (!option.IsHidden && OptionMatchesAlias(option, alias))
                {
                    return true;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return false;
    }

    internal ArgOption? ValueOption(ArgCommand command, string[] argv)
    {
        if (argv.Length < 2)
        {
            return null;
        }

        string previous = argv[argv.Length - 2];
        if (!IsOptionLike(previous))
        {
            return null;
        }

        ArgOption? option = FindOption(command, previous);
        if (option != null && OptionAcceptsValue(option))
        {
            return option;
        }

        return null;
    }

    internal List<ArgCompletion> OptionValueCompletions(ArgOption option, string prefix)
    {
        List<ArgCompletion> completions = new List<ArgCompletion>();
        int index = 0;
        while (index < option.Completions.Count)
        {
            string value = option.Completions[index];
            if (StartsWith(value, prefix, false))
            {
                completions.Add(ArgCompletion.ForOptionValue(option, value));
            }

            index = index + 1;
        }

        return completions;
    }

    internal void AddArgumentValueCompletions(List<ArgCompletion> completions, ArgCommand command, string[] argv, string prefix)
    {
        ArgArgument? argument = NextArgument(command, argv);
        if (argument == null)
        {
            return;
        }

        int index = 0;
        while (index < argument.Completions.Count)
        {
            string value = argument.Completions[index];
            if (StartsWith(value, prefix, false))
            {
                completions.Add(ArgCompletion.ForArgumentValue(argument, value));
            }

            index = index + 1;
        }
    }

    internal ArgArgument? NextArgument(ArgCommand command, string[] argv)
    {
        int positionalCount = CompletedPositionalCount(command, argv);
        if (positionalCount < command.Arguments.Count)
        {
            return command.Arguments[positionalCount];
        }

        return null;
    }

    internal int CompletedPositionalCount(ArgCommand command, string[] argv)
    {
        int count = 0;
        int tokenCount = CompletedTokenCount(argv);
        int index = CommandPathTokenCount(command, argv);
        while (index < tokenCount)
        {
            string token = argv[index];
            if (IsOptionLike(token))
            {
                ArgOption? option = FindOption(command, token);
                if (option != null && OptionAcceptsValue(option))
                {
                    index = index + 2;
                    continue;
                }
            }
            else
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CommandPathTokenCount(ArgCommand command, string[] argv)
    {
        ArgCommand root = CommandRoot(command);
        ArgCommand current = root;
        int count = CompletedTokenCount(argv);
        int index = 0;
        while (index < count)
        {
            string token = argv[index];
            if (IsOptionLike(token))
            {
                return index;
            }

            ArgCommand? next = FindSubcommand(current, token);
            if (next == null)
            {
                return index;
            }

            current = next;
            index = index + 1;
        }

        return index;
    }

    internal ArgOption? FindOption(ArgCommand command, string alias)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            if (OptionMatchesAlias(option, alias))
            {
                return option;
            }

            index = index + 1;
        }

        ArgCommand? current = command;
        while (current != null)
        {
            index = 0;
            while (index < current.GlobalOptions.Count)
            {
                ArgOption option = current.GlobalOptions[index];
                if (OptionMatchesAlias(option, alias))
                {
                    return option;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return null;
    }

    internal bool OptionMatchesAlias(ArgOption option, string alias)
    {
        int index = 0;
        while (index < option.Aliases.Count)
        {
            if (TextEquals(option.Aliases[index], alias, Settings.OptionMatchingCaseInsensitive()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool OptionAcceptsValue(ArgOption option)
    {
        if (option.HasValue)
        {
            return true;
        }

        if (option.ValueArity.Maximum != 0)
        {
            return true;
        }

        return false;
    }

    internal bool IsOptionLike(string token)
    {
        if (token.Length == 0)
        {
            return false;
        }

        if (token[0] == '-')
        {
            return true;
        }

        if (Settings.SlashOptionsEnabled() && token[0] == '/')
        {
            return true;
        }

        return false;
    }

    internal bool StartsWith(string text, string prefix, bool ignoreCase)
    {
        if (prefix.Length == 0)
        {
            return true;
        }

        if (ignoreCase)
        {
            return text.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
        }

        return text.StartsWith(prefix, System.StringComparison.Ordinal);
    }

    internal bool TextEquals(string left, string right, bool ignoreCase)
    {
        if (ignoreCase)
        {
            return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
        }

        return left == right;
    }

    internal ArgCommand CommandRoot(ArgCommand command)
    {
        ArgCommand current = command;
        while (current.Parent != null)
        {
            current = current.Parent;
        }

        return current;
    }
}
