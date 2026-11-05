using System.Collections.Generic;

namespace args;

/// <summary>
/// Describes a named command-line option and how its values are collected.
/// </summary>
public class ArgOption : IArgOption
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
    /// The alternate names accepted for this symbol.
    /// </summary>
    internal List<string> Aliases;
    /// <summary>
    /// The allowed literal values. An empty list permits any value.
    /// </summary>
    internal List<string> Choices;
    /// <summary>
    /// The optional text transformation applied before value retrieval and conversion.
    /// </summary>
    internal System.Func<string, string>? ValueNormalizer;
    /// <summary>
    /// The optional conversion used for converted-stage choice comparison.
    /// </summary>
    internal System.Func<string, ArgConvertResult?>? ChoiceConverter;
    /// <summary>
    /// The representation compared during choice validation.
    /// </summary>
    internal ArgChoiceValidationStage ChoiceValidationStage;
    /// <summary>
    /// The option names that cannot be supplied with this option.
    /// </summary>
    internal List<string> ConflictNames;
    /// <summary>
    /// The option names that must also be supplied with this option.
    /// </summary>
    internal List<string> RequiredNames;
    /// <summary>
    /// The static value candidates offered by command-line completion.
    /// </summary>
    internal List<string> Completions;
    /// <summary>
    /// The options and logical values supplied when this option is present.
    /// </summary>
    internal List<ArgOptionImplication> Implications;
    /// <summary>
    /// The longer description displayed in generated help.
    /// </summary>
    internal string DescriptionText;
    /// <summary>
    /// The placeholder name displayed for this option's value.
    /// </summary>
    internal string ValueName;
    /// <summary>
    /// The value used when the symbol was not supplied.
    /// </summary>
    internal string DefaultValue;
    /// <summary>
    /// The value used when an optional-value option is present without a value.
    /// </summary>
    internal string FallbackValue;
    /// <summary>
    /// The environment variable consulted when the command line does not provide a value.
    /// </summary>
    internal string EnvName;
    /// <summary>
    /// The deprecation message associated with this symbol, or an empty string when it is not deprecated.
    /// </summary>
    internal string DeprecatedMessage;
    /// <summary>
    /// The number of values this symbol accepts.
    /// </summary>
    internal ArgArity ValueArity;
    /// <summary>
    /// The command that owns this option.
    /// </summary>
    internal ArgCommand? ParentCommand;
    /// <summary>
    /// Whether descendants of the owning command inherit this option.
    /// </summary>
    internal bool IsGlobal;
    /// <summary>
    /// Whether the symbol must be supplied.
    /// </summary>
    internal bool IsRequired;
    /// <summary>
    /// Whether generated help and completion omit this symbol.
    /// </summary>
    internal bool IsHidden;
    /// <summary>
    /// Whether the option records presence without consuming a value.
    /// </summary>
    internal bool IsFlag;
    /// <summary>
    /// Whether a long flag also accepts its no-prefixed form.
    /// </summary>
    internal bool IsNegatable;
    /// <summary>
    /// Whether the option accepts a value.
    /// </summary>
    internal bool HasValue;
    /// <summary>
    /// Whether the option may appear without its value.
    /// </summary>
    internal bool ValueIsOptional;
    /// <summary>
    /// Whether the option may occur more than once.
    /// </summary>
    internal bool IsRepeatable;
    /// <summary>
    /// Whether repeated appearances increment a count.
    /// </summary>
    internal bool IsCount;
    /// <summary>
    /// Whether a default value has been configured.
    /// </summary>
    internal bool HasDefault;
    /// <summary>
    /// Whether a present-but-valueless fallback has been configured.
    /// </summary>
    internal bool HasFallback;
    /// <summary>
    /// Whether environment-variable fallback has been configured.
    /// </summary>
    internal bool HasEnv;

    internal ArgOption(string name)
    {
        ArgName.ValidateOptionAlias(name);
        Name = name;
        Kind = ArgSymbolKind.Option;
        Aliases = new List<string>();
        if (name.Length > 0)
        {
            Aliases.Add(name);
        }

        Choices = new List<string>();
        ValueNormalizer = null;
        ChoiceConverter = null;
        ChoiceValidationStage = ArgChoiceValidationStage.Raw;
        ConflictNames = new List<string>();
        RequiredNames = new List<string>();
        Completions = new List<string>();
        Implications = new List<ArgOptionImplication>();
        DescriptionText = "";
        ValueName = "";
        DefaultValue = "";
        FallbackValue = "";
        EnvName = "";
        DeprecatedMessage = "";
        ValueArity = new ArgArity(0, 0);
        ParentCommand = null;
        IsGlobal = false;
        IsRequired = false;
        IsHidden = false;
        IsFlag = false;
        IsNegatable = false;
        HasValue = false;
        ValueIsOptional = false;
        IsRepeatable = false;
        IsCount = false;
        HasDefault = false;
        HasFallback = false;
        HasEnv = false;
    }

    /// <summary>Returns the canonical option name.</summary>
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

    /// <summary>Returns the accepted aliases.</summary>
    /// <returns>The retained alias list.</returns>
    public List<string> GetAliases()
    {
        return Aliases;
    }

    /// <summary>Returns the allowed literal values.</summary>
    /// <returns>The retained choice list.</returns>
    public List<string> GetChoices()
    {
        return Choices;
    }

    /// <summary>Returns the configured value normalizer.</summary>
    /// <returns>The normalizer, or null when normalization is disabled.</returns>
    public System.Func<string, string>? GetValueNormalizer()
    {
        return ValueNormalizer;
    }

    /// <summary>Returns the configured converter used for choice comparison.</summary>
    /// <returns>The converter, or null when no choice converter is configured.</returns>
    public System.Func<string, ArgConvertResult?>? GetChoiceConverter()
    {
        return ChoiceConverter;
    }

    /// <summary>Returns the configured choice-validation stage.</summary>
    /// <returns>The selected validation stage.</returns>
    public ArgChoiceValidationStage GetChoiceValidationStage()
    {
        return ChoiceValidationStage;
    }

    /// <summary>Returns the conflicting option names.</summary>
    /// <returns>The retained conflict list.</returns>
    public List<string> GetConflictNames()
    {
        return ConflictNames;
    }

    /// <summary>Returns the required peer option names.</summary>
    /// <returns>The retained requirement list.</returns>
    public List<string> GetRequiredNames()
    {
        return RequiredNames;
    }

    /// <summary>Returns the configured completion values.</summary>
    /// <returns>The retained completion list.</returns>
    public List<string> GetCompletions()
    {
        return Completions;
    }

    /// <summary>Returns the configured option implications.</summary>
    /// <returns>The retained implication list.</returns>
    public List<ArgOptionImplication> GetImplications()
    {
        return Implications;
    }

    /// <summary>Returns the description text.</summary>
    /// <returns>The description text.</returns>
    public string GetDescriptionText()
    {
        return DescriptionText;
    }

    /// <summary>Returns the display name for option values.</summary>
    /// <returns>The value name.</returns>
    public string GetValueName()
    {
        return ValueName;
    }

    /// <summary>Returns the configured default value.</summary>
    /// <returns>The default value.</returns>
    public string GetDefaultValue()
    {
        return DefaultValue;
    }

    /// <summary>Returns the configured optional-value fallback.</summary>
    /// <returns>The fallback value.</returns>
    public string GetFallbackValue()
    {
        return FallbackValue;
    }

    /// <summary>Returns the configured environment-variable name.</summary>
    /// <returns>The environment-variable name.</returns>
    public string GetEnvName()
    {
        return EnvName;
    }

    /// <summary>Returns the deprecation message.</summary>
    /// <returns>The deprecation message.</returns>
    public string GetDeprecatedMessage()
    {
        return DeprecatedMessage;
    }

    /// <summary>Returns the configured value arity.</summary>
    /// <returns>The value arity.</returns>
    public ArgArity GetValueArity()
    {
        return ValueArity;
    }

    /// <summary>Returns the containing command.</summary>
    /// <returns>The containing command, or null before registration.</returns>
    public ArgCommand? GetParentCommand()
    {
        return ParentCommand;
    }

    /// <summary>Returns whether the option is global.</summary>
    /// <returns>True when global; otherwise false.</returns>
    public bool GetIsGlobal()
    {
        return IsGlobal;
    }

    /// <summary>Returns whether the option is required.</summary>
    /// <returns>True when required; otherwise false.</returns>
    public bool GetIsRequired()
    {
        return IsRequired;
    }

    /// <summary>Returns whether the option is hidden.</summary>
    /// <returns>True when hidden; otherwise false.</returns>
    public bool GetIsHidden()
    {
        return IsHidden;
    }

    /// <summary>Returns whether the option is a flag.</summary>
    /// <returns>True when the option is a flag; otherwise false.</returns>
    public bool GetIsFlag()
    {
        return IsFlag;
    }

    /// <summary>Returns whether the flag is negatable.</summary>
    /// <returns>True when negatable; otherwise false.</returns>
    public bool GetIsNegatable()
    {
        return IsNegatable;
    }

    /// <summary>Returns whether the option accepts values.</summary>
    /// <returns>True when the option accepts values; otherwise false.</returns>
    public bool GetHasValue()
    {
        return HasValue;
    }

    /// <summary>Returns whether the option value is optional.</summary>
    /// <returns>True when the value is optional; otherwise false.</returns>
    public bool GetValueIsOptional()
    {
        return ValueIsOptional;
    }

    /// <summary>Returns whether the option may repeat.</summary>
    /// <returns>True when repeatable; otherwise false.</returns>
    public bool GetIsRepeatable()
    {
        return IsRepeatable;
    }

    /// <summary>Returns whether occurrences are counted.</summary>
    /// <returns>True when occurrences are counted; otherwise false.</returns>
    public bool GetIsCount()
    {
        return IsCount;
    }

    /// <summary>Returns whether a default value is configured.</summary>
    /// <returns>True when a default is configured; otherwise false.</returns>
    public bool GetHasDefault()
    {
        return HasDefault;
    }

    /// <summary>Returns whether a fallback value is configured.</summary>
    /// <returns>True when a fallback is configured; otherwise false.</returns>
    public bool GetHasFallback()
    {
        return HasFallback;
    }

    /// <summary>Returns whether an environment fallback is configured.</summary>
    /// <returns>True when an environment fallback is configured; otherwise false.</returns>
    public bool GetHasEnv()
    {
        return HasEnv;
    }

    internal static ArgOption Empty()
    {
        ArgOption option = new ArgOption("--option");
        option.Name = "";
        option.Aliases.Clear();
        return option;
    }

    /// <summary>
    /// Registers an additional name for this symbol after checking for duplicates.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid option alias.</exception>
    /// <exception cref="System.InvalidOperationException">The alias duplicates this option or another option owned by the same command.</exception>
    public ArgOption Alias(string name)
    {
        ArgName.ValidateOptionAlias(name);
        if (HasAlias(name))
        {
            throw new System.InvalidOperationException("Duplicate option alias '" + name + "'.");
        }

        if (ParentCommand != null)
        {
            ParentCommand.EnsureOptionAliasAvailable(name, this);
        }

        Aliases.Add(name);
        return this;
    }

    /// <summary>
    /// Sets the description shown in generated help and returns the same object for fluent configuration.
    /// </summary>
    /// <param name="text">The text to store or process.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Description(string text)
    {
        DescriptionText = text;
        return this;
    }

    /// <summary>
    /// Marks this symbol as required and returns it for fluent configuration.
    /// </summary>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Required()
    {
        IsRequired = true;
        return this;
    }

    /// <summary>
    /// Hides this symbol from generated help and completion.
    /// </summary>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Hidden()
    {
        IsHidden = true;
        return this;
    }

    /// <summary>
    /// Configures this option as a valueless Boolean flag.
    /// </summary>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Flag()
    {
        IsFlag = true;
        IsNegatable = false;
        HasValue = false;
        ValueIsOptional = false;
        IsCount = false;
        ValueArity = new ArgArity(0, 0);
        return this;
    }

    /// <summary>
    /// Configures this option as a Boolean flag that also accepts a no-prefixed long alias.
    /// </summary>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Negatable()
    {
        IsNegatable = true;
        IsFlag = true;
        HasValue = false;
        ValueIsOptional = false;
        IsCount = false;
        ValueArity = new ArgArity(0, 0);
        return this;
    }

    /// <summary>
    /// Configures this option to require one value with the supplied display name.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Value(string name)
    {
        ValueName = name;
        HasValue = true;
        ValueIsOptional = false;
        IsFlag = false;
        IsNegatable = false;
        IsCount = false;
        ValueArity = new ArgArity(1, 1);
        return this;
    }

    /// <summary>
    /// Configures this option to accept zero or one value.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption OptionalValue(string name)
    {
        ValueName = name;
        HasValue = true;
        ValueIsOptional = true;
        IsFlag = false;
        IsNegatable = false;
        IsCount = false;
        ValueArity = new ArgArity(0, 1);
        return this;
    }

    /// <summary>
    /// Sets the accepted value-count range and derives whether this option behaves as a flag or accepts an optional value.
    /// </summary>
    /// <param name="minimum">The minimum accepted number of values.</param>
    /// <param name="maximum">The maximum accepted number of values; use ArgArity.Unbounded for no upper limit.</param>
    /// <remarks>This controls values consumed by an occurrence; call <c>Required</c> separately to require the option itself.</remarks>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">The minimum is negative, or the finite maximum is smaller than the minimum.</exception>
    public ArgOption Arity(int minimum, int maximum)
    {
        ValueArity = new ArgArity(minimum, maximum);
        HasValue = maximum != 0;
        ValueIsOptional = minimum == 0 && maximum != 0;
        IsFlag = maximum == 0;
        IsNegatable = false;
        IsCount = false;
        return this;
    }

    /// <summary>
    /// Allows this option to appear more than once.
    /// </summary>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Repeatable()
    {
        IsRepeatable = true;
        return this;
    }

    /// <summary>
    /// Configures this option as a repeat-counting flag.
    /// </summary>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Count()
    {
        IsCount = true;
        IsFlag = true;
        IsNegatable = false;
        HasValue = false;
        ValueIsOptional = false;
        ValueArity = new ArgArity(0, 0);
        return this;
    }

    /// <summary>
    /// Configures the value used when this symbol is absent.
    /// </summary>
    /// <param name="value">The source text, converted value, default, fallback, or completion value.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Default(string value)
    {
        DefaultValue = value;
        HasDefault = true;
        return this;
    }

    /// <summary>
    /// Configures the value used when this optional-value option appears without a value.
    /// </summary>
    /// <param name="value">The source text, converted value, default, fallback, or completion value.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Fallback(string value)
    {
        FallbackValue = value;
        HasFallback = true;
        return this;
    }

    /// <summary>
    /// Configures an environment variable as a fallback value source.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Env(string name)
    {
        EnvName = name;
        HasEnv = true;
        return this;
    }

    /// <summary>
    /// Adds one allowed literal value.
    /// </summary>
    /// <param name="value">The source text, converted value, default, fallback, or completion value.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Choice(string value)
    {
        Choices.Add(value);
        return this;
    }

    /// <summary>
    /// Configures the text transformation applied before value retrieval and conversion.
    /// </summary>
    /// <param name="normalizer">The transformation to apply to each resolved value. It must not return null.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentNullException">The normalizer is null.</exception>
    public ArgOption Normalize(System.Func<string, string> normalizer)
    {
        if (normalizer == null)
        {
            throw new System.ArgumentNullException("normalizer");
        }

        ValueNormalizer = normalizer;
        return this;
    }

    /// <summary>
    /// Configures the conversion used when choice validation runs at the converted stage.
    /// </summary>
    /// <param name="converter">The conversion to apply after normalization to both resolved values and configured choices.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentNullException">The converter is null.</exception>
    public ArgOption ConvertChoices(System.Func<string, ArgConvertResult?> converter)
    {
        if (converter == null)
        {
            throw new System.ArgumentNullException("converter");
        }

        ChoiceConverter = converter;
        return this;
    }

    /// <summary>
    /// Selects the representation compared during choice validation.
    /// </summary>
    /// <param name="stage">The raw, normalized, or converted validation stage.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    /// <remarks>Converted-stage validation rejects values when no choice converter is configured or conversion fails.</remarks>
    /// <exception cref="System.ArgumentOutOfRangeException">The stage is not a defined choice-validation stage.</exception>
    public ArgOption ValidateChoicesAt(ArgChoiceValidationStage stage)
    {
        if (stage != ArgChoiceValidationStage.Raw && stage != ArgChoiceValidationStage.Normalized && stage != ArgChoiceValidationStage.Converted)
        {
            throw new System.ArgumentOutOfRangeException("stage");
        }

        ChoiceValidationStage = stage;
        return this;
    }

    /// <summary>
    /// Adds an option name that cannot be used together with this option.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid option alias.</exception>
    public ArgOption Conflicts(string name)
    {
        ArgName.ValidateOptionAlias(name);
        ConflictNames.Add(name);
        return this;
    }

    /// <summary>
    /// Adds an option name that must also be present when this option is supplied.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentException">The name is not a valid option alias.</exception>
    public ArgOption Requires(string name)
    {
        ArgName.ValidateOptionAlias(name);
        RequiredNames.Add(name);
        return this;
    }

    /// <summary>
    /// Associates a deprecation message with this symbol.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Deprecated(string message)
    {
        DeprecatedMessage = message;
        return this;
    }

    /// <summary>
    /// Supplies another visible option and logical value when this option is explicitly present or implied.
    /// </summary>
    /// <param name="name">The canonical name or alias of the target option.</param>
    /// <param name="value">The logical value supplied to the target option.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    /// <remarks>Explicit command-line and fallback target values take precedence. Implied values take precedence over environment and default values.</remarks>
    /// <exception cref="System.ArgumentException">The target name is not a valid option alias.</exception>
    /// <exception cref="System.ArgumentNullException">The value is null.</exception>
    public ArgOption Implies(string name, string value)
    {
        ArgName.ValidateOptionAlias(name);
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        Implications.Add(new ArgOptionImplication(name, value));
        return this;
    }

    /// <summary>
    /// Adds a static completion candidate for this symbol.
    /// </summary>
    /// <param name="value">The source text, converted value, default, fallback, or completion value.</param>
    /// <returns>The same ArgOption instance so additional configuration calls can be chained.</returns>
    public ArgOption Complete(string value)
    {
        Completions.Add(value);
        return this;
    }

    internal bool HasAlias(string name)
    {
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
}
