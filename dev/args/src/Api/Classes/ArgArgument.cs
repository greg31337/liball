using System.Collections.Generic;

namespace args;

/// <summary>
/// Describes a positional argument accepted by a command.
/// </summary>
public class ArgArgument : IArgArgument
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
    /// The static value candidates offered by command-line completion.
    /// </summary>
    internal List<string> Completions;
    /// <summary>
    /// The longer description displayed in generated help.
    /// </summary>
    internal string DescriptionText;
    /// <summary>
    /// The value used when the symbol was not supplied.
    /// </summary>
    internal string DefaultValue;
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
    /// Whether the symbol must be supplied.
    /// </summary>
    internal bool IsRequired;
    /// <summary>
    /// Whether the positional argument consumes all remaining positional values.
    /// </summary>
    internal bool IsVariadic;
    /// <summary>
    /// Whether a default value has been configured.
    /// </summary>
    internal bool HasDefault;
    /// <summary>
    /// Whether environment-variable fallback has been configured.
    /// </summary>
    internal bool HasEnv;

    internal ArgArgument(string name)
    {
        ArgName.ValidateArgumentName(name);
        Name = name;
        Kind = ArgSymbolKind.Argument;
        Choices = new List<string>();
        ValueNormalizer = null;
        ChoiceConverter = null;
        ChoiceValidationStage = ArgChoiceValidationStage.Raw;
        Completions = new List<string>();
        DescriptionText = "";
        DefaultValue = "";
        EnvName = "";
        DeprecatedMessage = "";
        ValueArity = new ArgArity(0, 1);
        IsRequired = false;
        IsVariadic = false;
        HasDefault = false;
        HasEnv = false;
    }

    /// <summary>Returns the canonical argument name.</summary>
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

    /// <summary>Returns the configured completion values.</summary>
    /// <returns>The retained completion list.</returns>
    public List<string> GetCompletions()
    {
        return Completions;
    }

    /// <summary>Returns the description text.</summary>
    /// <returns>The description text.</returns>
    public string GetDescriptionText()
    {
        return DescriptionText;
    }

    /// <summary>Returns the configured default value.</summary>
    /// <returns>The default value.</returns>
    public string GetDefaultValue()
    {
        return DefaultValue;
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

    /// <summary>Returns whether the argument is required.</summary>
    /// <returns>True when the argument is required; otherwise false.</returns>
    public bool GetIsRequired()
    {
        return IsRequired;
    }

    /// <summary>Returns whether the argument is variadic.</summary>
    /// <returns>True when the argument is variadic; otherwise false.</returns>
    public bool GetIsVariadic()
    {
        return IsVariadic;
    }

    /// <summary>Returns whether a default value is configured.</summary>
    /// <returns>True when a default is configured; otherwise false.</returns>
    public bool GetHasDefault()
    {
        return HasDefault;
    }

    /// <summary>Returns whether an environment fallback is configured.</summary>
    /// <returns>True when an environment fallback is configured; otherwise false.</returns>
    public bool GetHasEnv()
    {
        return HasEnv;
    }

    /// <summary>
    /// Sets the description shown in generated help and returns the same object for fluent configuration.
    /// </summary>
    /// <param name="text">The text to store or process.</param>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    public ArgArgument Description(string text)
    {
        DescriptionText = text;
        return this;
    }

    /// <summary>
    /// Marks this symbol as required and returns it for fluent configuration.
    /// </summary>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    public ArgArgument Required()
    {
        IsRequired = true;
        if (ValueArity.Maximum == ArgArity.Unbounded)
        {
            ValueArity = new ArgArity(1, ArgArity.Unbounded);
        }
        else
        {
            ValueArity = new ArgArity(1, ValueArity.Maximum);
        }

        return this;
    }

    /// <summary>
    /// Marks this positional argument as optional and returns it for fluent configuration.
    /// </summary>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    public ArgArgument Optional()
    {
        IsRequired = false;
        ValueArity = new ArgArity(0, ValueArity.Maximum);
        return this;
    }

    /// <summary>
    /// Makes this argument consume all remaining positional values.
    /// </summary>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    public ArgArgument Variadic()
    {
        IsVariadic = true;
        if (IsRequired)
        {
            ValueArity = new ArgArity(1, ArgArity.Unbounded);
        }
        else
        {
            ValueArity = new ArgArity(0, ArgArity.Unbounded);
        }

        return this;
    }

    /// <summary>
    /// Sets the accepted value-count range and updates the symbol's required and variadic behavior.
    /// </summary>
    /// <param name="minimum">The minimum accepted number of values.</param>
    /// <param name="maximum">The maximum accepted number of values; use ArgArity.Unbounded for no upper limit.</param>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">The minimum is negative, or the finite maximum is smaller than the minimum.</exception>
    public ArgArgument Arity(int minimum, int maximum)
    {
        ValueArity = new ArgArity(minimum, maximum);
        IsRequired = minimum > 0;
        IsVariadic = maximum == ArgArity.Unbounded;
        return this;
    }

    /// <summary>
    /// Configures the value used when this symbol is absent.
    /// </summary>
    /// <param name="value">The source text, converted value, default, fallback, or completion value.</param>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    public ArgArgument Default(string value)
    {
        DefaultValue = value;
        HasDefault = true;
        return this;
    }

    /// <summary>
    /// Configures an environment variable as a fallback value source.
    /// </summary>
    /// <param name="name">The canonical name or alias.</param>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    public ArgArgument Env(string name)
    {
        EnvName = name;
        HasEnv = true;
        return this;
    }

    /// <summary>
    /// Adds one allowed literal value.
    /// </summary>
    /// <param name="value">The source text, converted value, default, fallback, or completion value.</param>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    public ArgArgument Choice(string value)
    {
        Choices.Add(value);
        return this;
    }

    /// <summary>
    /// Configures the text transformation applied before value retrieval and conversion.
    /// </summary>
    /// <param name="normalizer">The transformation to apply to each resolved value. It must not return null.</param>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentNullException">The normalizer is null.</exception>
    public ArgArgument Normalize(System.Func<string, string> normalizer)
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
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    /// <exception cref="System.ArgumentNullException">The converter is null.</exception>
    public ArgArgument ConvertChoices(System.Func<string, ArgConvertResult?> converter)
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
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    /// <remarks>Converted-stage validation rejects values when no choice converter is configured or conversion fails.</remarks>
    /// <exception cref="System.ArgumentOutOfRangeException">The stage is not a defined choice-validation stage.</exception>
    public ArgArgument ValidateChoicesAt(ArgChoiceValidationStage stage)
    {
        if (stage != ArgChoiceValidationStage.Raw && stage != ArgChoiceValidationStage.Normalized && stage != ArgChoiceValidationStage.Converted)
        {
            throw new System.ArgumentOutOfRangeException("stage");
        }

        ChoiceValidationStage = stage;
        return this;
    }

    /// <summary>
    /// Associates a deprecation message with this symbol.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    public ArgArgument Deprecated(string message)
    {
        DeprecatedMessage = message;
        return this;
    }

    /// <summary>
    /// Adds a static completion candidate for this symbol.
    /// </summary>
    /// <param name="value">The source text, converted value, default, fallback, or completion value.</param>
    /// <returns>The same ArgArgument instance so additional configuration calls can be chained.</returns>
    public ArgArgument Complete(string value)
    {
        Completions.Add(value);
        return this;
    }
}
