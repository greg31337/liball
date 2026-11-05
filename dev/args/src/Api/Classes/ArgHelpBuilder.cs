using System.Collections.Generic;

namespace args;

/// <summary>
/// Builds version, help, and usage output from a command model, with optional ANSI styling.
/// </summary>
public class ArgHelpBuilder : IArgHelpBuilder
{
    /// <summary>
    /// The target line width used when wrapping generated help.
    /// </summary>
    internal int HelpWidth;
    /// <summary>
    /// The settings used for matching, tokenization, completion, and help behavior.
    /// </summary>
    internal ArgParserSettings Settings;
    internal ArgOption DefaultHelpOption;
    internal ArgOption DefaultVersionOption;

    /// <summary>
    /// Creates a help builder with default parser settings and help width.
    /// </summary>
    public ArgHelpBuilder()
    {
        Settings = new ArgParserSettings();
        HelpWidth = Settings.HelpWidth;
        DefaultHelpOption = CreateDefaultHelpOption();
        DefaultVersionOption = CreateDefaultVersionOption();
    }

    /// <summary>
    /// Replaces the parser settings and rebuilds the built-in help options.
    /// </summary>
    /// <param name="settings">The settings instance to retain.</param>
    /// <returns>The same help builder.</returns>
    /// <exception cref="System.ArgumentNullException">The settings value is null.</exception>
    public ArgHelpBuilder UseSettings(ArgParserSettings settings)
    {
        if (settings == null)
        {
            throw new System.ArgumentNullException("settings");
        }

        Settings = settings;
        HelpWidth = settings.HelpWidth;
        DefaultHelpOption = CreateDefaultHelpOption();
        DefaultVersionOption = CreateDefaultVersionOption();
        return this;
    }

    /// <summary>
    /// Sets the target help width and returns the same object.
    /// </summary>
    /// <param name="width">The target line width.</param>
    /// <returns>The same help builder.</returns>
    public ArgHelpBuilder Width(int width)
    {
        HelpWidth = width;
        return this;
    }

    /// <summary>
    /// Returns the target help width.
    /// </summary>
    /// <returns>The target line width.</returns>
    public int GetHelpWidth()
    {
        return HelpWidth;
    }

    /// <summary>
    /// Returns the active parser settings.
    /// </summary>
    /// <returns>The retained settings instance.</returns>
    public ArgParserSettings GetSettings()
    {
        return Settings;
    }

    internal ArgOption CreateDefaultHelpOption()
    {
        ArgOption option = ArgOption.Empty();
        if (Settings.HelpAliases.Count > 0)
        {
            string alias = Settings.HelpAliases[0];
            ArgName.ValidateOptionAlias(alias);
            option.Name = alias;
            option.Aliases.Add(alias);
            int index = 1;
            while (index < Settings.HelpAliases.Count)
            {
                option.Alias(Settings.HelpAliases[index]);
                index = index + 1;
            }
        }

        option.Flag();
        option.Description(ArgMessageLookup.Get(Settings, ArgMessageKeys.HelpOptionDescription, new string[] { }));
        return option;
    }

    internal ArgOption CreateDefaultVersionOption()
    {
        ArgOption option = new ArgOption("--version");
        option.Alias("-V");
        option.Flag();
        option.Description(ArgMessageLookup.Get(Settings, ArgMessageKeys.VersionOptionDescription, new string[] { }));
        return option;
    }

    /// <summary>
    /// Returns version text for the application or command root.
    /// </summary>
    /// <param name="app">The application model to describe or format.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">The application is null.</exception>
    public string Version(ArgApp app)
    {
        if (app == null)
        {
            throw new System.ArgumentNullException("app");
        }

        return app.GetVersionText();
    }

    /// <summary>
    /// Returns version text for the application or command root.
    /// </summary>
    /// <param name="command">The command model to inspect or complete.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">The command is null.</exception>
    public string CommandVersion(ArgCommand command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        return RootVersionText(command);
    }

    /// <summary>
    /// Builds complete help text for the supplied application or command.
    /// </summary>
    /// <param name="app">The application model to describe or format.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">The application is null.</exception>
    public string RootHelp(ArgApp app)
    {
        if (app == null)
        {
            throw new System.ArgumentNullException("app");
        }

        return Help(app.RootCommand);
    }

    /// <summary>
    /// Builds complete help text for the supplied application or command.
    /// </summary>
    /// <param name="command">The command model to inspect or complete.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">The command is null.</exception>
    public string Help(ArgCommand command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        string text = UsageLine(command);
        if (command.SummaryText.Length > 0)
        {
            text = text + "\n\n" + WrapText(command.SummaryText);
        }

        if (command.DescriptionText.Length > 0)
        {
            text = text + "\n\n" + WrapText(command.DescriptionText);
        }

        string subcommands = SubcommandListing(command);
        if (subcommands.Length > 0)
        {
            text = text + "\n\n" + subcommands;
        }

        string arguments = ArgumentListing(command);
        if (arguments.Length > 0)
        {
            text = text + "\n\n" + arguments;
        }

        string options = OptionListing(command);
        if (options.Length > 0)
        {
            text = text + "\n\n" + options;
        }

        string optionGroups = OptionGroupListing(command);
        if (optionGroups.Length > 0)
        {
            text = text + "\n\n" + optionGroups;
        }

        string examples = ExampleListing(command);
        if (examples.Length > 0)
        {
            text = text + "\n\n" + examples;
        }

        return text;
    }

    internal string CommandHelp(ArgCommand command)
    {
        return Help(command);
    }

    /// <summary>
    /// Builds the usage line for the supplied command.
    /// </summary>
    /// <param name="command">The command model to inspect or complete.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">The command is null.</exception>
    public string UsageLine(ArgCommand command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        string usageLabel = Message(ArgMessageKeys.UsageLabel, new string[] { });
        if (command.UsageText.Length > 0)
        {
            return StyleUsageLine(usageLabel + " " + command.UsageText, usageLabel);
        }

        string line = usageLabel + " " + CommandPath(command);
        if (HasOptions(command))
        {
            line = line + " " + Message(ArgMessageKeys.OptionsPlaceholder, new string[] { });
        }

        if (command.Commands.Count > 0)
        {
            if (command.RequiresSubcommand)
            {
                line = line + " " + Message(ArgMessageKeys.RequiredCommandPlaceholder, new string[] { });
            }
            else
            {
                line = line + " " + Message(ArgMessageKeys.OptionalCommandPlaceholder, new string[] { });
            }
        }

        int index = 0;
        while (index < command.Arguments.Count)
        {
            line = line + " " + ArgumentUsage(command.Arguments[index]);
            index = index + 1;
        }

        return StyleUsageLine(line, usageLabel);
    }

    internal string Usage(ArgCommand command)
    {
        return UsageLine(command);
    }

    internal string CommandPath(ArgCommand command)
    {
        if (command.Parent == null)
        {
            return command.Name;
        }

        string parentPath = CommandPath(command.Parent);
        if (parentPath.Length == 0)
        {
            return command.Name;
        }

        return parentPath + " " + command.Name;
    }

    internal bool HasOptions(ArgCommand command)
    {
        if (HasDefaultVersionOption(command) && !HasVersionOption(command))
        {
            return true;
        }

        if (DefaultHelpOption.Aliases.Count > 0 && !HasHelpOption(command))
        {
            return true;
        }

        if (HasVisibleLocalOptions(command))
        {
            return true;
        }

        ArgCommand? current = command;
        while (current != null)
        {
            if (HasVisibleGlobalOptions(current))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    internal bool HasVisibleLocalOptions(ArgCommand command)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            if (ShouldShowOption(command.Options[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasVisibleGlobalOptions(ArgCommand command)
    {
        int index = 0;
        while (index < command.GlobalOptions.Count)
        {
            if (ShouldShowOption(command.GlobalOptions[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string ArgumentUsage(ArgArgument argument)
    {
        string name = argument.Name;
        if (argument.IsVariadic || argument.ValueArity.Maximum == ArgArity.Unbounded)
        {
            name = name + "...";
        }

        if (argument.IsRequired || argument.ValueArity.Minimum > 0)
        {
            return "<" + name + ">";
        }

        return "[" + name + "]";
    }

    internal string SubcommandListing(ArgCommand command)
    {
        if (!HasVisibleSubcommands(command))
        {
            return "";
        }

        string text = StyleHeading(Message(ArgMessageKeys.CommandsHeading, new string[] { }));
        List<string> groups = new List<string>();
        int index = 0;
        while (index < command.Commands.Count)
        {
            ArgCommand child = command.Commands[index];
            if (ShouldShowCommand(child) && child.GroupName.Length > 0 && !ContainsText(groups, child.GroupName))
            {
                groups.Add(child.GroupName);
                text = text + "\n  " + child.GroupName + ":";
                text = AppendCommandsInGroup(text, command, child.GroupName);
            }

            index = index + 1;
        }

        index = 0;
        while (index < command.Commands.Count)
        {
            ArgCommand child = command.Commands[index];
            if (ShouldShowCommand(child) && child.GroupName.Length == 0)
            {
                text = text + "\n" + WrapIndentedLine(CommandListItem(child), "  ", "    ");
            }

            index = index + 1;
        }

        return text;
    }

    internal string AppendCommandsInGroup(string text, ArgCommand command, string groupName)
    {
        int index = 0;
        while (index < command.Commands.Count)
        {
            ArgCommand child = command.Commands[index];
            if (ShouldShowCommand(child) && child.GroupName == groupName)
            {
                text = text + "\n" + WrapIndentedLine(CommandListItem(child), "    ", "      ");
            }

            index = index + 1;
        }

        return text;
    }

    internal bool HasVisibleSubcommands(ArgCommand command)
    {
        int index = 0;
        while (index < command.Commands.Count)
        {
            if (ShouldShowCommand(command.Commands[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string CommandListItem(ArgCommand command)
    {
        string text = CommandNames(command);
        if (command.SummaryText.Length > 0)
        {
            text = text + " - " + command.SummaryText;
        }

        return text;
    }

    internal string CommandNames(ArgCommand command)
    {
        string text = command.Name;
        int index = 0;
        while (index < command.Aliases.Count)
        {
            text = text + ", " + command.Aliases[index];
            index = index + 1;
        }

        return text;
    }

    internal bool ContainsText(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string ArgumentListing(ArgCommand command)
    {
        if (command.Arguments.Count == 0)
        {
            return "";
        }

        string text = StyleHeading(Message(ArgMessageKeys.ArgumentsHeading, new string[] { }));
        int index = 0;
        while (index < command.Arguments.Count)
        {
            text = text + "\n" + WrapIndentedLine(ArgumentListItem(command.Arguments[index]), "  ", "    ");
            index = index + 1;
        }

        return text;
    }

    internal string ArgumentListItem(ArgArgument argument)
    {
        string text = ArgumentUsage(argument);
        string details = "";
        if (argument.IsRequired || argument.ValueArity.Minimum > 0)
        {
            details = AppendDetail(details, Message(ArgMessageKeys.RequiredDetail, new string[] { }));
        }

        if (argument.HasDefault)
        {
            details = AppendDetail(details, Message(ArgMessageKeys.DefaultDetail, new string[] { argument.DefaultValue }));
        }

        if (argument.HasEnv)
        {
            details = AppendDetail(details, Message(ArgMessageKeys.EnvironmentDetail, new string[] { argument.EnvName }));
        }

        if (argument.Choices.Count > 0)
        {
            details = AppendDetail(details, Message(ArgMessageKeys.ChoicesDetail, new string[] { JoinText(argument.Choices) }));
        }

        return HelpListItem(text, argument.DescriptionText, details);
    }

    internal string OptionListing(ArgCommand command)
    {
        if (!HasOptions(command))
        {
            return "";
        }

        string text = StyleHeading(Message(ArgMessageKeys.OptionsHeading, new string[] { }));
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            if (ShouldShowOption(option))
            {
                text = text + "\n" + WrapIndentedLine(OptionListItem(option), "  ", "    ");
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
                if (ShouldShowOption(option))
                {
                    text = text + "\n" + WrapIndentedLine(OptionListItem(option), "  ", "    ");
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        if (HasDefaultVersionOption(command) && !HasVersionOption(command))
        {
            text = text + "\n" + WrapIndentedLine(OptionListItem(DefaultVersionOption), "  ", "    ");
        }

        if (DefaultHelpOption.Aliases.Count > 0 && !HasHelpOption(command))
        {
            text = text + "\n" + WrapIndentedLine(OptionListItem(DefaultHelpOption), "  ", "    ");
        }

        return text;
    }

    internal string OptionGroupListing(ArgCommand command)
    {
        if (command.OptionGroups.Count == 0)
        {
            return "";
        }

        string text = "";
        int index = 0;
        while (index < command.OptionGroups.Count)
        {
            ArgOptionGroup group = command.OptionGroups[index];
            List<string> optionNames = VisibleOptionGroupNames(command, group);
            if (optionNames.Count == 0)
            {
                index = index + 1;
                continue;
            }

            if (text.Length == 0)
            {
                text = StyleHeading(Message(ArgMessageKeys.OptionGroupsHeading, new string[] { }));
            }

            string kind = "";
            if (group.IsMutuallyExclusive)
            {
                kind = Message(ArgMessageKeys.MutuallyExclusiveDetail, new string[] { });
            }

            if (group.IsAtLeastOneRequired)
            {
                if (kind.Length > 0)
                {
                    kind = kind + "; " + Message(ArgMessageKeys.AtLeastOneRequiredDetail, new string[] { });
                }
                else
                {
                    kind = Message(ArgMessageKeys.AtLeastOneRequiredDetail, new string[] { });
                }
            }

            string line = group.Name;
            if (kind.Length > 0)
            {
                line = line + " (" + kind + ")";
            }

            line = line + ": " + JoinText(optionNames);
            text = text + "\n" + WrapIndentedLine(line, "  ", "    ");
            index = index + 1;
        }

        return text;
    }

    internal List<string> VisibleOptionGroupNames(ArgCommand command, ArgOptionGroup group)
    {
        List<string> names = new List<string>();
        int index = 0;
        while (index < group.OptionNames.Count)
        {
            string name = group.OptionNames[index];
            ArgOption? option = FindVisibleHelpOption(command, name);
            if (option != null && ShouldShowOption(option))
            {
                names.Add(name);
            }

            index = index + 1;
        }

        return names;
    }

    internal ArgOption? FindVisibleHelpOption(ArgCommand command, string name)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            if (option.HasAlias(name))
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
                if (option.HasAlias(name))
                {
                    return option;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return null;
    }

    internal bool HasHelpOption(ArgCommand command)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            if (ShouldShowOption(option) && IsHelpOption(option))
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
                if (ShouldShowOption(option) && IsHelpOption(option))
                {
                    return true;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return false;
    }

    internal bool IsHelpOption(ArgOption option)
    {
        int index = 0;
        while (index < Settings.HelpAliases.Count)
        {
            if (option.HasAlias(Settings.HelpAliases[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasVersionOption(ArgCommand command)
    {
        int index = 0;
        while (index < command.Options.Count)
        {
            ArgOption option = command.Options[index];
            if (ShouldShowOption(option) && IsVersionOption(option))
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
                if (ShouldShowOption(option) && IsVersionOption(option))
                {
                    return true;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return false;
    }

    internal bool ShouldShowCommand(ArgCommand command)
    {
        return Settings.IncludeHiddenInHelp || !command.IsHidden;
    }

    internal bool ShouldShowOption(ArgOption option)
    {
        return Settings.IncludeHiddenInHelp || !option.IsHidden;
    }

    internal bool IsVersionOption(ArgOption option)
    {
        if (option.HasAlias("--version"))
        {
            return true;
        }

        if (option.HasAlias("-V"))
        {
            return true;
        }

        if (option.HasAlias("/version"))
        {
            return true;
        }

        return false;
    }

    internal bool HasDefaultVersionOption(ArgCommand command)
    {
        return RootVersionText(command).Length > 0;
    }

    internal string RootVersionText(ArgCommand command)
    {
        return ArgVersionText.ResolveCommand(command);
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

    internal string OptionListItem(ArgOption option)
    {
        string text = OptionNames(option) + OptionValueUsage(option);
        string details = "";
        if (option.IsRequired)
        {
            details = AppendDetail(details, Message(ArgMessageKeys.RequiredDetail, new string[] { }));
        }

        if (option.HasDefault)
        {
            details = AppendDetail(details, Message(ArgMessageKeys.DefaultDetail, new string[] { option.DefaultValue }));
        }

        if (option.HasEnv)
        {
            details = AppendDetail(details, Message(ArgMessageKeys.EnvironmentDetail, new string[] { option.EnvName }));
        }

        if (option.Choices.Count > 0)
        {
            details = AppendDetail(details, Message(ArgMessageKeys.ChoicesDetail, new string[] { JoinText(option.Choices) }));
        }

        return HelpListItem(text, option.DescriptionText, details);
    }

    internal string OptionNames(ArgOption option)
    {
        string text = "";
        int index = 0;
        while (index < option.Aliases.Count)
        {
            if (text.Length == 0)
            {
                text = option.Aliases[index];
            }
            else
            {
                text = text + ", " + option.Aliases[index];
            }

            index = index + 1;
        }

        if (text.Length == 0)
        {
            return option.Name;
        }

        return text;
    }

    internal string OptionValueUsage(ArgOption option)
    {
        if (!option.HasValue && option.ValueArity.Maximum == 0)
        {
            return "";
        }

        string valueName = option.ValueName;
        if (valueName.Length == 0)
        {
            valueName = Message(ArgMessageKeys.ValuePlaceholder, new string[] { });
        }

        if (option.ValueArity.Maximum == ArgArity.Unbounded)
        {
            valueName = valueName + "...";
        }

        if (option.ValueArity.Minimum == 0)
        {
            return " [<" + valueName + ">]";
        }

        return " <" + valueName + ">";
    }

    internal string HelpListItem(string text, string description, string details)
    {
        if (description.Length > 0)
        {
            text = text + " - " + description;
        }

        if (details.Length > 0)
        {
            text = text + " (" + details + ")";
        }

        return text;
    }

    internal string AppendDetail(string details, string value)
    {
        if (details.Length == 0)
        {
            return value;
        }

        return details + "; " + value;
    }

    internal string JoinText(List<string> values)
    {
        string text = "";
        int index = 0;
        while (index < values.Count)
        {
            if (text.Length == 0)
            {
                text = values[index];
            }
            else
            {
                text = text + ", " + values[index];
            }

            index = index + 1;
        }

        return text;
    }

    internal string ExampleListing(ArgCommand command)
    {
        if (command.Examples.Count == 0)
        {
            return "";
        }

        string text = StyleHeading(Message(ArgMessageKeys.ExamplesHeading, new string[] { }));
        int index = 0;
        while (index < command.Examples.Count)
        {
            text = text + "\n  " + command.Examples[index];
            index = index + 1;
        }

        return text;
    }

    internal string WrapText(string text)
    {
        return WrapIndentedLine(text, "", "");
    }

    internal string Message(string key, string[] values)
    {
        return ArgMessageLookup.Get(Settings, key, values);
    }

    internal string StyleUsageLine(string line, string usageLabel)
    {
        if (!Settings.EnableAnsiHelpStyling)
        {
            return line;
        }

        return "\u001b[1m" + usageLabel + "\u001b[0m" + line.Substring(usageLabel.Length);
    }

    internal string StyleHeading(string heading)
    {
        if (!Settings.EnableAnsiHelpStyling)
        {
            return heading;
        }

        return "\u001b[1m" + heading + "\u001b[0m";
    }

    internal string WrapIndentedLine(string text, string initialIndent, string continuationIndent)
    {
        if (HelpWidth <= 0)
        {
            return initialIndent + text;
        }

        if (initialIndent.Length + text.Length <= HelpWidth)
        {
            return initialIndent + text;
        }

        string result = "";
        string current = initialIndent;
        int index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && text[index] == ' ')
            {
                index = index + 1;
            }

            if (index >= text.Length)
            {
                break;
            }

            int wordStart = index;
            while (index < text.Length && text[index] != ' ')
            {
                index = index + 1;
            }

            string word = text.Substring(wordStart, index - wordStart);
            if (current.Length == initialIndent.Length || current.Length == continuationIndent.Length)
            {
                current = current + word;
            }
            else if (current.Length + 1 + word.Length <= HelpWidth)
            {
                current = current + " " + word;
            }
            else
            {
                if (result.Length == 0)
                {
                    result = current;
                }
                else
                {
                    result = result + "\n" + current;
                }

                current = continuationIndent + word;
            }
        }

        if (current.Length > 0)
        {
            if (result.Length == 0)
            {
                result = current;
            }
            else
            {
                result = result + "\n" + current;
            }
        }

        return result;
    }
}
