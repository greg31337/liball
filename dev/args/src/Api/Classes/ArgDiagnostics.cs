namespace args;

/// <summary>
/// Produces readable diagnostics and deterministic JSON metadata for application and command models.
/// </summary>
public class ArgDiagnostics : IArgDiagnostics
{
    /// <summary>
    /// Creates a diagnostic model formatter.
    /// </summary>
    public ArgDiagnostics()
    {
    }

    /// <summary>
    /// Returns a multi-line diagnostic description of the supplied command model.
    /// </summary>
    /// <param name="app">The application model to describe or format.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">The application is null.</exception>
    public string Describe(ArgApp app)
    {
        if (app == null)
        {
            throw new System.ArgumentNullException("app");
        }

        return DescribeCommand(app.RootCommand);
    }

    /// <summary>
    /// Returns a multi-line diagnostic description of the supplied command model.
    /// </summary>
    /// <param name="command">The command model to inspect or complete.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">The command is null.</exception>
    public string DescribeCommand(ArgCommand command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        return DescribeCommandAtIndent(command, 0);
    }

    /// <summary>
    /// Returns deterministic, machine-readable JSON metadata for the supplied application model.
    /// </summary>
    /// <param name="app">The application model to serialize.</param>
    /// <returns>A versioned JSON document containing the application and complete command tree.</returns>
    /// <exception cref="System.ArgumentNullException">The application is null.</exception>
    /// <exception cref="System.InvalidOperationException">The configured version provider returns null.</exception>
    public string MetadataJson(ArgApp app)
    {
        if (app == null)
        {
            throw new System.ArgumentNullException("app");
        }

        return new ArgMetadataJsonWriter().WriteApplication(app);
    }

    /// <summary>
    /// Returns deterministic, machine-readable JSON metadata for the supplied command tree.
    /// </summary>
    /// <param name="command">The root command or descendant command to serialize.</param>
    /// <returns>A versioned JSON document containing the supplied command and its descendants.</returns>
    /// <exception cref="System.ArgumentNullException">The command is null.</exception>
    public string CommandMetadataJson(ArgCommand command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        return new ArgMetadataJsonWriter().WriteCommandDocument(command);
    }

    internal string DescribeCommandAtIndent(ArgCommand command, int indent)
    {
        string text = "";
        text = AppendLine(text, indent, "command " + CommandPath(command));
        text = AppendLine(text, indent + 2, "name: " + command.Name);
        text = AppendLine(text, indent + 2, "aliases: " + Join(command.Aliases));
        text = AppendLine(text, indent + 2, "hidden: " + Bool(command.IsHidden));
        text = AppendLine(text, indent + 2, "required-subcommand: " + Bool(command.RequiresSubcommand));
        text = AppendLine(text, indent + 2, "branch: " + Bool(command.IsBranch));
        text = AppendLine(text, indent + 2, "handler: " + Bool(command.HandlerFunc != null));
        text = AppendLine(text, indent + 2, "before: " + Bool(command.BeforeFunc != null));
        text = AppendLine(text, indent + 2, "after: " + Bool(command.AfterFunc != null));

        if (command.SummaryText.Length > 0)
        {
            text = AppendLine(text, indent + 2, "summary: " + command.SummaryText);
        }

        if (command.DescriptionText.Length > 0)
        {
            text = AppendLine(text, indent + 2, "description: " + command.DescriptionText);
        }

        if (command.UsageText.Length > 0)
        {
            text = AppendLine(text, indent + 2, "usage: " + command.UsageText);
        }

        if (command.VersionText.Length > 0)
        {
            text = AppendLine(text, indent + 2, "version: " + command.VersionText);
        }

        if (command.GroupName.Length > 0)
        {
            text = AppendLine(text, indent + 2, "group: " + command.GroupName);
        }

        if (command.DefaultCommandValue != null)
        {
            text = AppendLine(text, indent + 2, "default-command: " + command.DefaultCommandValue.Name);
        }

        if (command.DeprecatedMessage.Length > 0)
        {
            text = AppendLine(text, indent + 2, "deprecated: " + command.DeprecatedMessage);
        }

        if (command.GlobalOptions.Count > 0)
        {
            text = AppendLine(text, indent + 2, "global-options:");
            int index = 0;
            while (index < command.GlobalOptions.Count)
            {
                text = text + "\n" + DescribeOption(command.GlobalOptions[index], indent + 4);
                index = index + 1;
            }
        }

        if (command.Options.Count > 0)
        {
            text = AppendLine(text, indent + 2, "options:");
            int index = 0;
            while (index < command.Options.Count)
            {
                text = text + "\n" + DescribeOption(command.Options[index], indent + 4);
                index = index + 1;
            }
        }

        if (command.Arguments.Count > 0)
        {
            text = AppendLine(text, indent + 2, "arguments:");
            int index = 0;
            while (index < command.Arguments.Count)
            {
                text = text + "\n" + DescribeArgument(command.Arguments[index], indent + 4);
                index = index + 1;
            }
        }

        if (command.OptionGroups.Count > 0)
        {
            text = AppendLine(text, indent + 2, "option-groups:");
            int index = 0;
            while (index < command.OptionGroups.Count)
            {
                ArgOptionGroup group = command.OptionGroups[index];
                text = AppendLine(text, indent + 4, "group " + group.Name);
                text = AppendLine(text, indent + 6, "mutually-exclusive: " + Bool(group.IsMutuallyExclusive));
                text = AppendLine(text, indent + 6, "at-least-one-required: " + Bool(group.IsAtLeastOneRequired));
                text = AppendLine(text, indent + 6, "options: " + Join(group.OptionNames));
                index = index + 1;
            }
        }

        if (command.Examples.Count > 0)
        {
            text = AppendLine(text, indent + 2, "examples:");
            int index = 0;
            while (index < command.Examples.Count)
            {
                text = AppendLine(text, indent + 4, command.Examples[index]);
                index = index + 1;
            }
        }

        if (command.Commands.Count > 0)
        {
            text = AppendLine(text, indent + 2, "commands:");
            int index = 0;
            while (index < command.Commands.Count)
            {
                text = text + "\n" + DescribeCommandAtIndent(command.Commands[index], indent + 4);
                index = index + 1;
            }
        }

        return text;
    }

    internal string DescribeOption(ArgOption option, int indent)
    {
        string text = "";
        text = AppendLine(text, indent, "option " + option.Name);
        text = AppendLine(text, indent + 2, "aliases: " + Join(option.Aliases));
        text = AppendLine(text, indent + 2, "global: " + Bool(option.IsGlobal));
        text = AppendLine(text, indent + 2, "required: " + Bool(option.IsRequired));
        text = AppendLine(text, indent + 2, "hidden: " + Bool(option.IsHidden));
        text = AppendLine(text, indent + 2, "flag: " + Bool(option.IsFlag));
        text = AppendLine(text, indent + 2, "negatable: " + Bool(option.IsNegatable));
        text = AppendLine(text, indent + 2, "value: " + Bool(option.HasValue));
        text = AppendLine(text, indent + 2, "optional-value: " + Bool(option.ValueIsOptional));
        text = AppendLine(text, indent + 2, "repeatable: " + Bool(option.IsRepeatable));
        text = AppendLine(text, indent + 2, "count: " + Bool(option.IsCount));
        text = AppendLine(text, indent + 2, "arity: " + ArityText(option.ValueArity));

        if (option.ValueName.Length > 0)
        {
            text = AppendLine(text, indent + 2, "value-name: " + option.ValueName);
        }

        if (option.DescriptionText.Length > 0)
        {
            text = AppendLine(text, indent + 2, "description: " + option.DescriptionText);
        }

        if (option.HasDefault)
        {
            text = AppendLine(text, indent + 2, "default: " + option.DefaultValue);
        }

        if (option.HasFallback)
        {
            text = AppendLine(text, indent + 2, "fallback: " + option.FallbackValue);
        }

        if (option.HasEnv)
        {
            text = AppendLine(text, indent + 2, "env: " + option.EnvName);
        }

        if (option.Choices.Count > 0)
        {
            text = AppendLine(text, indent + 2, "choices: " + Join(option.Choices));
        }

        if (option.Completions.Count > 0)
        {
            text = AppendLine(text, indent + 2, "completions: " + Join(option.Completions));
        }

        if (option.ConflictNames.Count > 0)
        {
            text = AppendLine(text, indent + 2, "conflicts: " + Join(option.ConflictNames));
        }

        if (option.RequiredNames.Count > 0)
        {
            text = AppendLine(text, indent + 2, "requires: " + Join(option.RequiredNames));
        }

        if (option.Implications.Count > 0)
        {
            text = AppendLine(text, indent + 2, "implies: " + ImplicationsText(option.Implications));
        }

        if (option.DeprecatedMessage.Length > 0)
        {
            text = AppendLine(text, indent + 2, "deprecated: " + option.DeprecatedMessage);
        }

        return text;
    }

    internal string ImplicationsText(System.Collections.Generic.List<ArgOptionImplication> implications)
    {
        string text = "";
        int index = 0;
        while (index < implications.Count)
        {
            if (index > 0)
            {
                text = text + ", ";
            }

            ArgOptionImplication implication = implications[index];
            text = text + implication.OptionName + "=" + implication.Value;
            index = index + 1;
        }

        return text;
    }

    internal string DescribeArgument(ArgArgument argument, int indent)
    {
        string text = "";
        text = AppendLine(text, indent, "argument " + argument.Name);
        text = AppendLine(text, indent + 2, "required: " + Bool(argument.IsRequired));
        text = AppendLine(text, indent + 2, "variadic: " + Bool(argument.IsVariadic));
        text = AppendLine(text, indent + 2, "arity: " + ArityText(argument.ValueArity));

        if (argument.DescriptionText.Length > 0)
        {
            text = AppendLine(text, indent + 2, "description: " + argument.DescriptionText);
        }

        if (argument.HasDefault)
        {
            text = AppendLine(text, indent + 2, "default: " + argument.DefaultValue);
        }

        if (argument.HasEnv)
        {
            text = AppendLine(text, indent + 2, "env: " + argument.EnvName);
        }

        if (argument.Choices.Count > 0)
        {
            text = AppendLine(text, indent + 2, "choices: " + Join(argument.Choices));
        }

        if (argument.Completions.Count > 0)
        {
            text = AppendLine(text, indent + 2, "completions: " + Join(argument.Completions));
        }

        if (argument.DeprecatedMessage.Length > 0)
        {
            text = AppendLine(text, indent + 2, "deprecated: " + argument.DeprecatedMessage);
        }

        return text;
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

    internal string ArityText(ArgArity arity)
    {
        string maximum = arity.Maximum.ToString();
        if (arity.Maximum == ArgArity.Unbounded)
        {
            maximum = "*";
        }

        return arity.Minimum.ToString() + ".." + maximum;
    }

    internal string Join(System.Collections.Generic.List<string> values)
    {
        string text = "";
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0)
            {
                text = text + ", ";
            }

            text = text + values[index];
            index = index + 1;
        }

        return text;
    }

    internal string Bool(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }

    internal string AppendLine(string text, int indent, string line)
    {
        if (text.Length == 0)
        {
            return Indent(indent) + line;
        }

        return text + "\n" + Indent(indent) + line;
    }

    internal string Indent(int count)
    {
        string text = "";
        int index = 0;
        while (index < count)
        {
            text = text + " ";
            index = index + 1;
        }

        return text;
    }
}
