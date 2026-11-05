namespace args;

/// <summary>
/// Defines stable keys used by command-line message providers.
/// </summary>
public static class ArgMessageKeys
{
    /// <summary>The built-in help option description.</summary>
    public const string HelpOptionDescription = "help.option.description";
    /// <summary>The built-in version option description.</summary>
    public const string VersionOptionDescription = "version.option.description";
    /// <summary>The help-command token.</summary>
    public const string HelpCommandName = "parser.helpCommand.name";
    /// <summary>An option was supplied a value it does not accept. Values: option alias.</summary>
    public const string UnexpectedOptionValue = "parser.option.unexpectedValue";
    /// <summary>An option is missing a required value. Values: option alias.</summary>
    public const string MissingOptionValue = "parser.option.missingValue";
    /// <summary>An option is unknown. Values: option alias.</summary>
    public const string UnknownOption = "parser.option.unknown";
    /// <summary>A required option is absent. Values: option name.</summary>
    public const string MissingRequiredOption = "validation.option.required";
    /// <summary>A required argument is absent. Values: argument name.</summary>
    public const string MissingRequiredArgument = "validation.argument.required";
    /// <summary>A required subcommand is absent. Values: command name.</summary>
    public const string MissingRequiredSubcommand = "validation.command.requiredSubcommand";
    /// <summary>An option has too few values. Values: option name, value count text.</summary>
    public const string OptionTooFewValues = "validation.option.tooFewValues";
    /// <summary>An option has too many values. Values: option name, value count text.</summary>
    public const string OptionTooManyValues = "validation.option.tooManyValues";
    /// <summary>An argument has too few values. Values: argument name, value count text.</summary>
    public const string ArgumentTooFewValues = "validation.argument.tooFewValues";
    /// <summary>An argument has too many values. Values: argument name, value count text.</summary>
    public const string ArgumentTooManyValues = "validation.argument.tooManyValues";
    /// <summary>A singular value count. Values: invariant count.</summary>
    public const string OneValue = "validation.valueCount.one";
    /// <summary>A plural value count. Values: invariant count.</summary>
    public const string MultipleValues = "validation.valueCount.multiple";
    /// <summary>A non-repeatable option occurs more than once. Values: option name.</summary>
    public const string DuplicateOption = "validation.option.duplicate";
    /// <summary>A command is unknown. Values: command token.</summary>
    public const string UnknownCommand = "validation.command.unknown";
    /// <summary>An option value is outside its choices. Values: option name, value.</summary>
    public const string InvalidOptionChoice = "validation.option.invalidChoice";
    /// <summary>An argument value is outside its choices. Values: argument name, value.</summary>
    public const string InvalidArgumentChoice = "validation.argument.invalidChoice";
    /// <summary>Two options conflict. Values: option name, conflicting option name.</summary>
    public const string ConflictingOption = "validation.option.conflict";
    /// <summary>An option dependency is absent. Values: option name, required option name.</summary>
    public const string MissingDependentOption = "validation.option.missingDependency";
    /// <summary>A mutually exclusive option group has too many supplied options. Values: group name.</summary>
    public const string MutuallyExclusiveOptionGroup = "validation.optionGroup.mutuallyExclusive";
    /// <summary>A required option group has no supplied option. Values: group name.</summary>
    public const string MissingRequiredOptionGroup = "validation.optionGroup.required";
    /// <summary>The usage label.</summary>
    public const string UsageLabel = "help.heading.usage";
    /// <summary>The commands section heading.</summary>
    public const string CommandsHeading = "help.heading.commands";
    /// <summary>The arguments section heading.</summary>
    public const string ArgumentsHeading = "help.heading.arguments";
    /// <summary>The options section heading.</summary>
    public const string OptionsHeading = "help.heading.options";
    /// <summary>The option-groups section heading.</summary>
    public const string OptionGroupsHeading = "help.heading.optionGroups";
    /// <summary>The examples section heading.</summary>
    public const string ExamplesHeading = "help.heading.examples";
    /// <summary>The optional options usage placeholder.</summary>
    public const string OptionsPlaceholder = "help.placeholder.options";
    /// <summary>The required command usage placeholder.</summary>
    public const string RequiredCommandPlaceholder = "help.placeholder.requiredCommand";
    /// <summary>The optional command usage placeholder.</summary>
    public const string OptionalCommandPlaceholder = "help.placeholder.optionalCommand";
    /// <summary>The required help detail.</summary>
    public const string RequiredDetail = "help.detail.required";
    /// <summary>The default-value help detail. Values: default value.</summary>
    public const string DefaultDetail = "help.detail.default";
    /// <summary>The environment-variable help detail. Values: environment name.</summary>
    public const string EnvironmentDetail = "help.detail.environment";
    /// <summary>The choice-list help detail. Values: joined choices.</summary>
    public const string ChoicesDetail = "help.detail.choices";
    /// <summary>The mutually-exclusive group detail.</summary>
    public const string MutuallyExclusiveDetail = "help.detail.mutuallyExclusive";
    /// <summary>The at-least-one-required group detail.</summary>
    public const string AtLeastOneRequiredDetail = "help.detail.atLeastOneRequired";
    /// <summary>The default option value placeholder.</summary>
    public const string ValuePlaceholder = "help.placeholder.value";
    /// <summary>An unsupported completion shell was requested.</summary>
    public const string UnsupportedCompletionShell = "completion.shell.unsupported";
}
