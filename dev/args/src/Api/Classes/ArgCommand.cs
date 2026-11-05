using System;
using System.Collections.Generic;

namespace args;

/// <summary>
/// Describes a command, its child symbols, and the handlers that run when it is selected.
/// </summary>
public class ArgCommand : IArgCommand
{
    /// <summary>
    /// The canonical name used to identify this symbol.
    /// </summary>
    internal string Name;
    /// <summary>
    /// The category represented by this value.
    /// </summary>
    internal ArgSymbolKind Kind;
    /// <summary>
    /// The containing command, or null for the root command.
    /// </summary>
    internal ArgCommand? Parent;
    /// <summary>
    /// The alternate names accepted for this symbol.
    /// </summary>
    internal List<string> Aliases;
    /// <summary>
    /// The direct child commands.
    /// </summary>
    internal List<ArgCommand> Commands;
    /// <summary>
    /// The options available only on this command.
    /// </summary>
    internal List<ArgOption> Options;
    /// <summary>
    /// The options inherited by this command and its descendants.
    /// </summary>
    internal List<ArgOption> GlobalOptions;
    /// <summary>
    /// The positional arguments accepted in declaration order.
    /// </summary>
    internal List<ArgArgument> Arguments;
    /// <summary>
    /// The option groups validated for this command.
    /// </summary>
    internal List<ArgOptionGroup> OptionGroups;
    /// <summary>
    /// The example command lines displayed in generated help.
    /// </summary>
    internal List<string> Examples;
    /// <summary>
    /// The command-context validation callbacks executed when this command belongs to the selected path.
    /// </summary>
    internal List<Func<ArgContext, ArgParseError?>> CommandValidators;
    /// <summary>
    /// The cross-option validation callbacks executed when this command belongs to the selected path.
    /// </summary>
    internal List<Func<ArgValues, ArgParseError?>> CrossOptionValidators;
    /// <summary>
    /// The short description used in command listings.
    /// </summary>
    internal string SummaryText;
    /// <summary>
    /// The longer description displayed in generated help.
    /// </summary>
    internal string DescriptionText;
    /// <summary>
    /// The custom usage text, or an empty string when usage is generated.
    /// </summary>
    internal string UsageText;
    /// <summary>
    /// The version text displayed for a version request.
    /// </summary>
    internal string VersionText;
    /// <summary>
    /// The dynamic version text provider configured on the root command, or null when static text is used.
    /// </summary>
    internal Func<string>? VersionProviderFunc;
    /// <summary>
    /// The help-listing group name, or an empty string for the default group.
    /// </summary>
    internal string GroupName;
    /// <summary>
    /// The child command selected when no explicit child is supplied, or null when no default is configured.
    /// </summary>
    internal ArgCommand? DefaultCommandValue;
    /// <summary>
    /// The deprecation message associated with this symbol, or an empty string when it is not deprecated.
    /// </summary>
    internal string DeprecatedMessage;
    /// <summary>
    /// The handler invoked after successful validation, or null when no handler is configured. Its return value becomes the process-style exit code.
    /// </summary>
    internal Func<ArgContext, int>? HandlerFunc;
    /// <summary>
    /// The hook invoked before the selected handler, or null when no before hook is configured. A nonzero result stops invocation and becomes the exit code.
    /// </summary>
    internal Func<ArgContext, int>? BeforeFunc;
    /// <summary>
    /// The hook invoked after a successful handler call, or null when no after hook is configured. Its result can replace the handler's exit code.
    /// </summary>
    internal Func<ArgContext, int>? AfterFunc;
    /// <summary>
    /// Whether generated help and completion omit this symbol.
    /// </summary>
    internal bool IsHidden;
    /// <summary>
    /// Whether invoking this command without a child command is a validation error.
    /// </summary>
    internal bool RequiresSubcommand;
    /// <summary>
    /// Whether this command exists only to group child commands and displays help when no handler runs.
    /// </summary>
    internal bool IsBranch;

    internal ArgCommand(string name)
    {
        ArgName.ValidateCommandName(name);
        Name = name;
        Kind = ArgSymbolKind.Command;
        Parent = null;
        Aliases = new List<string>();
        Commands = new List<ArgCommand>();
        Options = new List<ArgOption>();
        GlobalOptions = new List<ArgOption>();
        Arguments = new List<ArgArgument>();
        OptionGroups = new List<ArgOptionGroup>();
        Examples = new List<string>();
        CommandValidators = new List<Func<ArgContext, ArgParseError?>>();
        CrossOptionValidators = new List<Func<ArgValues, ArgParseError?>>();
        SummaryText = "";
        DescriptionText = "";
        UsageText = "";
        VersionText = "";
        VersionProviderFunc = null;
        GroupName = "";
        DefaultCommandValue = null;
        DeprecatedMessage = "";
        HandlerFunc = null;
        BeforeFunc = null;
        AfterFunc = null;
        IsHidden = false;
        RequiresSubcommand = false;
        IsBranch = false;
    }

    /// <summary>Returns the canonical command name.</summary>
    /// <returns>The canonical name.</returns>
    public string GetName()
    {
        return Name;
    }

    /// <summary>Returns the symbol category.</summary>
    /// <returns>The symbol category.</returns>
    public ArgSymbolKind GetKind()
    {
        return Kind;
    }

    /// <summary>Returns the containing command.</summary>
    /// <returns>The parent command, or null for the root.</returns>
    public ArgCommand? GetParent()
    {
        return Parent;
    }

    /// <summary>Returns the alternate command names.</summary>
    /// <returns>The retained alias list.</returns>
    public List<string> GetAliases()
    {
        return Aliases;
    }

    /// <summary>Returns the direct child commands.</summary>
    /// <returns>The retained command list.</returns>
    public List<ArgCommand> GetCommands()
    {
        return Commands;
    }

    /// <summary>Returns the local options.</summary>
    /// <returns>The retained option list.</returns>
    public List<ArgOption> GetOptions()
    {
        return Options;
    }

    /// <summary>Returns the inheritable global options.</summary>
    /// <returns>The retained global-option list.</returns>
    public List<ArgOption> GetGlobalOptions()
    {
        return GlobalOptions;
    }

    /// <summary>Returns the positional arguments.</summary>
    /// <returns>The retained argument list.</returns>
    public List<ArgArgument> GetArguments()
    {
        return Arguments;
    }

    /// <summary>Returns the command-local option groups.</summary>
    /// <returns>The retained option-group list.</returns>
    public List<ArgOptionGroup> GetOptionGroups()
    {
        return OptionGroups;
    }

    /// <summary>Returns the help examples.</summary>
    /// <returns>The retained example list.</returns>
    public List<string> GetExamples()
    {
        return Examples;
    }

    /// <summary>Returns the configured command-context validators.</summary>
    /// <returns>The retained command-validator list.</returns>
    public List<Func<ArgContext, ArgParseError?>> GetCommandValidators()
    {
        return CommandValidators;
    }

    /// <summary>Returns the configured cross-option validators.</summary>
    /// <returns>The retained cross-option-validator list.</returns>
    public List<Func<ArgValues, ArgParseError?>> GetCrossOptionValidators()
    {
        return CrossOptionValidators;
    }

    /// <summary>Returns the command summary.</summary>
    /// <returns>The summary text.</returns>
    public string GetSummaryText()
    {
        return SummaryText;
    }

    /// <summary>Returns the command description.</summary>
    /// <returns>The description text.</returns>
    public string GetDescriptionText()
    {
        return DescriptionText;
    }

    /// <summary>Returns the custom usage text.</summary>
    /// <returns>The usage text.</returns>
    public string GetUsageText()
    {
        return UsageText;
    }

    /// <summary>Returns the root version text.</summary>
    /// <returns>The version text.</returns>
    public string GetVersionText()
    {
        return VersionText;
    }

    /// <summary>Returns the help-listing group name.</summary>
    /// <returns>The group name.</returns>
    public string GetGroupName()
    {
        return GroupName;
    }

    /// <summary>Returns the selected default child command.</summary>
    /// <returns>The default child command, or null when none is configured.</returns>
    public ArgCommand? GetDefaultCommand()
    {
        return DefaultCommandValue;
    }

    /// <summary>Returns the deprecation message.</summary>
    /// <returns>The deprecation message.</returns>
    public string GetDeprecatedMessage()
    {
        return DeprecatedMessage;
    }

    /// <summary>Returns the selected-command handler.</summary>
    /// <returns>The handler, or null when none is configured.</returns>
    public Func<ArgContext, int>? GetHandler()
    {
        return HandlerFunc;
    }

    /// <summary>Returns the before hook.</summary>
    /// <returns>The hook, or null when none is configured.</returns>
    public Func<ArgContext, int>? GetBefore()
    {
        return BeforeFunc;
    }

    /// <summary>Returns the after hook.</summary>
    /// <returns>The hook, or null when none is configured.</returns>
    public Func<ArgContext, int>? GetAfter()
    {
        return AfterFunc;
    }

    /// <summary>Returns whether the command is hidden.</summary>
    /// <returns>True when hidden; otherwise false.</returns>
    public bool GetIsHidden()
    {
        return IsHidden;
    }

    /// <summary>Returns whether a child command is required.</summary>
    /// <returns>True when a child command is required; otherwise false.</returns>
    public bool GetRequiresSubcommand()
    {
        return RequiresSubcommand;
    }

    /// <summary>Returns whether the command is a branch.</summary>
    /// <returns>True when the command is a branch; otherwise false.</returns>
    public bool GetIsBranch()
    {
        return IsBranch;
    }

    /// <summary>
    /// Sets the description shown in generated help and returns the same object for fluent configuration.
    /// </summary>
    /// <param name="text">The command description shown in detailed help.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    public ArgCommand Description(string text)
    {
        DescriptionText = text;
        return this;
    }

    /// <summary>
    /// Sets the short text used when this command appears in a command listing.
    /// </summary>
    /// <param name="text">The short description shown beside the command in listings.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    public ArgCommand Summary(string text)
    {
        SummaryText = text;
        return this;
    }

    /// <summary>
    /// Sets custom usage text in place of generated usage.
    /// </summary>
    /// <param name="text">The text displayed after the <c>Usage:</c> prefix.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    public ArgCommand Usage(string text)
    {
        UsageText = text;
        return this;
    }

    /// <summary>
    /// Assigns this command to a named group in its parent's help listing.
    /// </summary>
    /// <param name="name">The help-listing group name.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    public ArgCommand Group(string name)
    {
        GroupName = name;
        return this;
    }

    /// <summary>
    /// Registers an additional name for this symbol after checking for duplicates.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentException">The alias is not a valid command name.</exception>
    /// <exception cref="System.InvalidOperationException">The alias duplicates this command or a sibling command name or alias.</exception>
    public ArgCommand Alias(string name)
    {
        ArgName.ValidateCommandName(name);
        if (MatchesNameOrAlias(name))
        {
            throw new InvalidOperationException("Duplicate command name or alias '" + name + "'.");
        }

        if (Parent != null)
        {
            Parent.EnsureCommandNameAvailable(name, this);
        }

        Aliases.Add(name);
        return this;
    }

    /// <summary>
    /// Adds an example command line to generated help.
    /// </summary>
    /// <param name="text">An example command line to include in help.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    public ArgCommand Example(string text)
    {
        Examples.Add(text);
        return this;
    }

    /// <summary>
    /// Hides this symbol from generated help and completion.
    /// </summary>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    public ArgCommand Hidden()
    {
        IsHidden = true;
        return this;
    }

    /// <summary>
    /// Associates a deprecation message with this command.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    public ArgCommand Deprecated(string message)
    {
        DeprecatedMessage = message;
        return this;
    }

    /// <summary>
    /// Requires callers to select one of this command's child commands.
    /// </summary>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    public ArgCommand RequiredSubcommand()
    {
        RequiresSubcommand = true;
        return this;
    }

    /// <summary>
    /// Marks this command as a branch that displays help when no handler runs.
    /// </summary>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    public ArgCommand Branch()
    {
        IsBranch = true;
        return this;
    }

    /// <summary>
    /// Selects an existing direct child as the command used when no explicit child is supplied.
    /// </summary>
    /// <param name="name">The canonical name or alias of an existing direct child command.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid command name.</exception>
    /// <exception cref="System.InvalidOperationException">No direct child command has the supplied name or alias.</exception>
    public ArgCommand DefaultCommand(string name)
    {
        ArgName.ValidateCommandName(name);
        int index = 0;
        while (index < Commands.Count)
        {
            ArgCommand command = Commands[index];
            if (command.MatchesNameOrAlias(name))
            {
                DefaultCommandValue = command;
                return this;
            }

            index = index + 1;
        }

        throw new InvalidOperationException("Unknown default command '" + name + "'.");
    }

    /// <summary>
    /// Sets the handler that runs after successful parsing and validation.
    /// </summary>
    /// <param name="handler">The synchronous handler or hook, which returns a process-style exit code.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    public ArgCommand Handler(Func<ArgContext, int> handler)
    {
        HandlerFunc = handler;
        return this;
    }

    /// <summary>
    /// Sets the hook that runs before the selected command handler.
    /// </summary>
    /// <param name="handler">The synchronous handler or hook, which returns a process-style exit code.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    /// <remarks>Before hooks run from the root toward the selected command. The first nonzero result stops later hooks and prevents the command handler from running.</remarks>
    public ArgCommand Before(Func<ArgContext, int> handler)
    {
        BeforeFunc = handler;
        return this;
    }

    /// <summary>
    /// Sets the hook that runs after the selected command handler.
    /// </summary>
    /// <param name="handler">The synchronous handler or hook, which returns a process-style exit code.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    /// <remarks>After hooks run from the selected command back toward the root. A nonzero hook result replaces the current exit code.</remarks>
    public ArgCommand After(Func<ArgContext, int> handler)
    {
        AfterFunc = handler;
        return this;
    }

    /// <summary>
    /// Adds a command-specific validation callback.
    /// </summary>
    /// <param name="validator">The callback that returns a structured error, or null when its rule succeeds.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    /// <remarks>After built-in validation, each selected-path command runs its command callbacks in registration order followed by its cross-option callbacks. Commands run from root to leaf. Help and version requests skip all callbacks.</remarks>
    /// <exception cref="System.ArgumentNullException">The validator is null.</exception>
    public ArgCommand ValidateCommand(Func<ArgContext, ArgParseError?> validator)
    {
        if (validator == null)
        {
            throw new ArgumentNullException("validator");
        }

        CommandValidators.Add(validator);
        return this;
    }

    /// <summary>
    /// Adds a cross-option validation callback.
    /// </summary>
    /// <param name="validator">The callback that inspects resolved values and returns a structured error, or null when its rule succeeds.</param>
    /// <returns>The same ArgCommand instance so additional configuration calls can be chained.</returns>
    /// <remarks>After built-in validation, each selected-path command runs its command callbacks in registration order followed by its cross-option callbacks. Commands run from root to leaf. Help and version requests skip all callbacks.</remarks>
    /// <exception cref="System.ArgumentNullException">The validator is null.</exception>
    public ArgCommand ValidateOptions(Func<ArgValues, ArgParseError?> validator)
    {
        if (validator == null)
        {
            throw new ArgumentNullException("validator");
        }

        CrossOptionValidators.Add(validator);
        return this;
    }

    /// <summary>
    /// Creates, registers, and returns a child command with the supplied name.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The newly registered child <c>ArgCommand</c>.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid command name.</exception>
    /// <exception cref="System.InvalidOperationException">The name duplicates an existing child command name or alias.</exception>
    public ArgCommand Command(string name)
    {
        ArgName.ValidateCommandName(name);
        EnsureCommandNameAvailable(name, null);
        ArgCommand command = new ArgCommand(name);
        command.Parent = this;
        Commands.Add(command);
        return command;
    }

    /// <summary>
    /// Creates, registers, and returns a command-local option with the supplied alias.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The newly registered <c>ArgOption</c>.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid option alias.</exception>
    /// <exception cref="System.InvalidOperationException">The alias duplicates an existing local or global option on this command.</exception>
    public ArgOption Option(string name)
    {
        ArgName.ValidateOptionAlias(name);
        EnsureOptionAliasAvailable(name, null);
        ArgOption option = new ArgOption(name);
        option.ParentCommand = this;
        option.IsGlobal = false;
        Options.Add(option);
        return option;
    }

    /// <summary>
    /// Creates and returns an option inherited by descendant commands.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The newly registered <c>ArgOption</c>.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid option alias.</exception>
    /// <exception cref="System.InvalidOperationException">The alias duplicates an existing local or global option on this command.</exception>
    public ArgOption GlobalOption(string name)
    {
        ArgName.ValidateOptionAlias(name);
        EnsureOptionAliasAvailable(name, null);
        ArgOption option = new ArgOption(name);
        option.ParentCommand = this;
        option.IsGlobal = true;
        GlobalOptions.Add(option);
        return option;
    }

    /// <summary>
    /// Creates, appends, and returns a positional argument definition.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The newly registered <c>ArgArgument</c>.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid argument name.</exception>
    /// <exception cref="System.InvalidOperationException">The name duplicates an existing positional argument on this command.</exception>
    public ArgArgument Argument(string name)
    {
        ArgName.ValidateArgumentName(name);
        int index = 0;
        while (index < Arguments.Count)
        {
            if (Arguments[index].Name == name)
            {
                throw new InvalidOperationException("Duplicate argument name '" + name + "'.");
            }

            index = index + 1;
        }

        ArgArgument argument = new ArgArgument(name);
        Arguments.Add(argument);
        return argument;
    }

    /// <summary>
    /// Creates a group that permits at most one of its named options.
    /// </summary>
    /// <param name="name">The nonempty group name used in errors and help.</param>
    /// <param name="optionNames">Canonical option names or aliases visible to this command.</param>
    /// <returns>The newly registered option group.</returns>
    /// <exception cref="System.ArgumentException">The group name is empty or an option name is invalid.</exception>
    /// <exception cref="System.ArgumentNullException">The option-name array is null.</exception>
    /// <exception cref="System.InvalidOperationException">The group name is already registered on this command.</exception>
    public ArgOptionGroup MutuallyExclusiveGroup(string name, string[] optionNames)
    {
        ValidateOptionGroup(name, optionNames);
        ArgOptionGroup group = new ArgOptionGroup(name, optionNames, true, false, this);
        OptionGroups.Add(group);
        return group;
    }

    /// <summary>
    /// Creates a group that requires at least one of its named options.
    /// </summary>
    /// <param name="name">The nonempty group name used in errors and help.</param>
    /// <param name="optionNames">Canonical option names or aliases visible to this command.</param>
    /// <returns>The newly registered option group.</returns>
    /// <exception cref="System.ArgumentException">The group name is empty or an option name is invalid.</exception>
    /// <exception cref="System.ArgumentNullException">The option-name array is null.</exception>
    /// <exception cref="System.InvalidOperationException">The group name is already registered on this command.</exception>
    public ArgOptionGroup AtLeastOneRequiredGroup(string name, string[] optionNames)
    {
        ValidateOptionGroup(name, optionNames);
        ArgOptionGroup group = new ArgOptionGroup(name, optionNames, false, true, this);
        OptionGroups.Add(group);
        return group;
    }

    internal void ValidateOptionGroup(string name, string[] optionNames)
    {
        if (name == null || name.Length == 0)
        {
            throw new ArgumentException("Option group name cannot be empty.", "name");
        }

        if (optionNames == null)
        {
            throw new ArgumentNullException("optionNames");
        }

        int groupIndex = 0;
        while (groupIndex < OptionGroups.Count)
        {
            if (OptionGroups[groupIndex].Name == name)
            {
                throw new InvalidOperationException("Duplicate option group name '" + name + "'.");
            }

            groupIndex = groupIndex + 1;
        }

        int optionIndex = 0;
        while (optionIndex < optionNames.Length)
        {
            ArgName.ValidateOptionAlias(optionNames[optionIndex]);
            optionIndex = optionIndex + 1;
        }
    }

    internal bool MatchesNameOrAlias(string name)
    {
        if (Name == name)
        {
            return true;
        }

        int index = 0;
        while (index < Aliases.Count)
        {
            if (Aliases[index] == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void EnsureCommandNameAvailable(string name, ArgCommand? exclude)
    {
        int index = 0;
        while (index < Commands.Count)
        {
            ArgCommand command = Commands[index];
            if (command != exclude && command.MatchesNameOrAlias(name))
            {
                throw new InvalidOperationException("Duplicate command name or alias '" + name + "'.");
            }

            index = index + 1;
        }
    }

    internal void EnsureOptionAliasAvailable(string alias, ArgOption? exclude)
    {
        int localIndex = 0;
        while (localIndex < Options.Count)
        {
            ArgOption option = Options[localIndex];
            if (option != exclude && option.HasAlias(alias))
            {
                throw new InvalidOperationException("Duplicate option alias '" + alias + "'.");
            }

            localIndex = localIndex + 1;
        }

        int globalIndex = 0;
        while (globalIndex < GlobalOptions.Count)
        {
            ArgOption option = GlobalOptions[globalIndex];
            if (option != exclude && option.HasAlias(alias))
            {
                throw new InvalidOperationException("Duplicate option alias '" + alias + "'.");
            }

            globalIndex = globalIndex + 1;
        }
    }
}
