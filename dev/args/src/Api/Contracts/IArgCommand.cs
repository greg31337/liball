using System;
using System.Collections.Generic;

namespace args;

/// <summary>
/// Defines command inspection and fluent configuration operations.
/// </summary>
public interface IArgCommand
{
    /// <summary>Returns the canonical name.</summary>
    string GetName();
    /// <summary>Returns the symbol category.</summary>
    ArgSymbolKind GetKind();
    /// <summary>Returns the parent command.</summary>
    ArgCommand? GetParent();
    /// <summary>Returns command aliases.</summary>
    List<string> GetAliases();
    /// <summary>Returns child commands.</summary>
    List<ArgCommand> GetCommands();
    /// <summary>Returns local options.</summary>
    List<ArgOption> GetOptions();
    /// <summary>Returns global options.</summary>
    List<ArgOption> GetGlobalOptions();
    /// <summary>Returns positional arguments.</summary>
    List<ArgArgument> GetArguments();
    /// <summary>Returns command-local option groups.</summary>
    List<ArgOptionGroup> GetOptionGroups();
    /// <summary>Returns help examples.</summary>
    List<string> GetExamples();
    /// <summary>Returns command-context validators.</summary>
    List<Func<ArgContext, ArgParseError?>> GetCommandValidators();
    /// <summary>Returns cross-option validators.</summary>
    List<Func<ArgValues, ArgParseError?>> GetCrossOptionValidators();
    /// <summary>Returns summary text.</summary>
    string GetSummaryText();
    /// <summary>Returns description text.</summary>
    string GetDescriptionText();
    /// <summary>Returns usage text.</summary>
    string GetUsageText();
    /// <summary>Returns version text.</summary>
    string GetVersionText();
    /// <summary>Returns the group name.</summary>
    string GetGroupName();
    /// <summary>Returns the selected default child command.</summary>
    ArgCommand? GetDefaultCommand();
    /// <summary>Returns the deprecation message.</summary>
    string GetDeprecatedMessage();
    /// <summary>Returns the command handler.</summary>
    Func<ArgContext, int>? GetHandler();
    /// <summary>Returns the before hook.</summary>
    Func<ArgContext, int>? GetBefore();
    /// <summary>Returns the after hook.</summary>
    Func<ArgContext, int>? GetAfter();
    /// <summary>Returns whether the command is hidden.</summary>
    bool GetIsHidden();
    /// <summary>Returns whether a child command is required.</summary>
    bool GetRequiresSubcommand();
    /// <summary>Returns whether the command is a branch.</summary>
    bool GetIsBranch();
    /// <summary>Sets description text.</summary>
    ArgCommand Description(string text);
    /// <summary>Sets summary text.</summary>
    ArgCommand Summary(string text);
    /// <summary>Sets custom usage text.</summary>
    ArgCommand Usage(string text);
    /// <summary>Sets the help-listing group name.</summary>
    ArgCommand Group(string name);
    /// <summary>Adds an alias.</summary>
    ArgCommand Alias(string name);
    /// <summary>Adds a help example.</summary>
    ArgCommand Example(string text);
    /// <summary>Hides the command.</summary>
    ArgCommand Hidden();
    /// <summary>Marks the command as deprecated.</summary>
    ArgCommand Deprecated(string message);
    /// <summary>Requires a child command.</summary>
    ArgCommand RequiredSubcommand();
    /// <summary>Marks the command as a branch.</summary>
    ArgCommand Branch();
    /// <summary>Selects a default child command.</summary>
    ArgCommand DefaultCommand(string name);
    /// <summary>Sets the command handler.</summary>
    ArgCommand Handler(Func<ArgContext, int> handler);
    /// <summary>Sets the before hook.</summary>
    ArgCommand Before(Func<ArgContext, int> handler);
    /// <summary>Sets the after hook.</summary>
    ArgCommand After(Func<ArgContext, int> handler);
    /// <summary>Adds a command-specific validator.</summary>
    ArgCommand ValidateCommand(Func<ArgContext, ArgParseError?> validator);
    /// <summary>Adds a cross-option validator.</summary>
    ArgCommand ValidateOptions(Func<ArgValues, ArgParseError?> validator);
    /// <summary>Adds a child command.</summary>
    ArgCommand Command(string name);
    /// <summary>Adds a local option.</summary>
    ArgOption Option(string name);
    /// <summary>Adds a global option.</summary>
    ArgOption GlobalOption(string name);
    /// <summary>Adds a positional argument.</summary>
    ArgArgument Argument(string name);
    /// <summary>Adds a mutually exclusive option group.</summary>
    ArgOptionGroup MutuallyExclusiveGroup(string name, string[] optionNames);
    /// <summary>Adds an at-least-one-required option group.</summary>
    ArgOptionGroup AtLeastOneRequiredGroup(string name, string[] optionNames);
}
