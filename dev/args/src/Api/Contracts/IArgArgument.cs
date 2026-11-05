using System.Collections.Generic;

namespace args;

/// <summary>
/// Defines positional-argument inspection and fluent configuration operations.
/// </summary>
public interface IArgArgument
{
    /// <summary>Returns the canonical name.</summary>
    string GetName();
    /// <summary>Returns the symbol category.</summary>
    ArgSymbolKind GetKind();
    /// <summary>Returns the allowed values.</summary>
    List<string> GetChoices();
    /// <summary>Returns the value normalizer.</summary>
    System.Func<string, string>? GetValueNormalizer();
    /// <summary>Returns the converted-choice converter.</summary>
    System.Func<string, ArgConvertResult?>? GetChoiceConverter();
    /// <summary>Returns the choice-validation stage.</summary>
    ArgChoiceValidationStage GetChoiceValidationStage();
    /// <summary>Returns completion values.</summary>
    List<string> GetCompletions();
    /// <summary>Returns description text.</summary>
    string GetDescriptionText();
    /// <summary>Returns the default value.</summary>
    string GetDefaultValue();
    /// <summary>Returns the environment-variable name.</summary>
    string GetEnvName();
    /// <summary>Returns the deprecation message.</summary>
    string GetDeprecatedMessage();
    /// <summary>Returns the value arity.</summary>
    ArgArity GetValueArity();
    /// <summary>Returns whether the argument is required.</summary>
    bool GetIsRequired();
    /// <summary>Returns whether the argument is variadic.</summary>
    bool GetIsVariadic();
    /// <summary>Returns whether a default is configured.</summary>
    bool GetHasDefault();
    /// <summary>Returns whether environment fallback is configured.</summary>
    bool GetHasEnv();
    /// <summary>Sets description text.</summary>
    ArgArgument Description(string text);
    /// <summary>Marks the argument as required.</summary>
    ArgArgument Required();
    /// <summary>Marks the argument as optional.</summary>
    ArgArgument Optional();
    /// <summary>Marks the argument as variadic.</summary>
    ArgArgument Variadic();
    /// <summary>Sets the accepted value-count range.</summary>
    ArgArgument Arity(int minimum, int maximum);
    /// <summary>Sets the default value.</summary>
    ArgArgument Default(string value);
    /// <summary>Sets the environment fallback.</summary>
    ArgArgument Env(string name);
    /// <summary>Adds an allowed value.</summary>
    ArgArgument Choice(string value);
    /// <summary>Sets the value normalizer.</summary>
    ArgArgument Normalize(System.Func<string, string> normalizer);
    /// <summary>Sets the converted-choice converter.</summary>
    ArgArgument ConvertChoices(System.Func<string, ArgConvertResult?> converter);
    /// <summary>Sets the choice-validation stage.</summary>
    ArgArgument ValidateChoicesAt(ArgChoiceValidationStage stage);
    /// <summary>Sets a deprecation message.</summary>
    ArgArgument Deprecated(string message);
    /// <summary>Adds a completion value.</summary>
    ArgArgument Complete(string value);
}
