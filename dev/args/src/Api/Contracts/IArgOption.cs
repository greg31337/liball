using System.Collections.Generic;

namespace args;

/// <summary>
/// Defines option inspection and fluent configuration operations.
/// </summary>
public interface IArgOption
{
    /// <summary>Returns the canonical name.</summary>
    string GetName();
    /// <summary>Returns the symbol category.</summary>
    ArgSymbolKind GetKind();
    /// <summary>Returns accepted aliases.</summary>
    List<string> GetAliases();
    /// <summary>Returns allowed values.</summary>
    List<string> GetChoices();
    /// <summary>Returns the value normalizer.</summary>
    System.Func<string, string>? GetValueNormalizer();
    /// <summary>Returns the converted-choice converter.</summary>
    System.Func<string, ArgConvertResult?>? GetChoiceConverter();
    /// <summary>Returns the choice-validation stage.</summary>
    ArgChoiceValidationStage GetChoiceValidationStage();
    /// <summary>Returns conflicting option names.</summary>
    List<string> GetConflictNames();
    /// <summary>Returns required peer option names.</summary>
    List<string> GetRequiredNames();
    /// <summary>Returns completion values.</summary>
    List<string> GetCompletions();
    /// <summary>Returns configured option implications.</summary>
    List<ArgOptionImplication> GetImplications();
    /// <summary>Returns description text.</summary>
    string GetDescriptionText();
    /// <summary>Returns the value display name.</summary>
    string GetValueName();
    /// <summary>Returns the default value.</summary>
    string GetDefaultValue();
    /// <summary>Returns the optional-value fallback.</summary>
    string GetFallbackValue();
    /// <summary>Returns the environment-variable name.</summary>
    string GetEnvName();
    /// <summary>Returns the deprecation message.</summary>
    string GetDeprecatedMessage();
    /// <summary>Returns the value arity.</summary>
    ArgArity GetValueArity();
    /// <summary>Returns the containing command.</summary>
    ArgCommand? GetParentCommand();
    /// <summary>Returns whether the option is global.</summary>
    bool GetIsGlobal();
    /// <summary>Returns whether the option is required.</summary>
    bool GetIsRequired();
    /// <summary>Returns whether the option is hidden.</summary>
    bool GetIsHidden();
    /// <summary>Returns whether the option is a flag.</summary>
    bool GetIsFlag();
    /// <summary>Returns whether the flag is negatable.</summary>
    bool GetIsNegatable();
    /// <summary>Returns whether the option accepts values.</summary>
    bool GetHasValue();
    /// <summary>Returns whether the value is optional.</summary>
    bool GetValueIsOptional();
    /// <summary>Returns whether the option may repeat.</summary>
    bool GetIsRepeatable();
    /// <summary>Returns whether occurrences are counted.</summary>
    bool GetIsCount();
    /// <summary>Returns whether a default is configured.</summary>
    bool GetHasDefault();
    /// <summary>Returns whether a fallback is configured.</summary>
    bool GetHasFallback();
    /// <summary>Returns whether environment fallback is configured.</summary>
    bool GetHasEnv();
    /// <summary>Adds an alias.</summary>
    ArgOption Alias(string name);
    /// <summary>Sets description text.</summary>
    ArgOption Description(string text);
    /// <summary>Marks the option as required.</summary>
    ArgOption Required();
    /// <summary>Hides the option.</summary>
    ArgOption Hidden();
    /// <summary>Makes the option a flag.</summary>
    ArgOption Flag();
    /// <summary>Makes the flag negatable.</summary>
    ArgOption Negatable();
    /// <summary>Configures a required value.</summary>
    ArgOption Value(string name);
    /// <summary>Configures an optional value.</summary>
    ArgOption OptionalValue(string name);
    /// <summary>Sets the accepted value-count range.</summary>
    ArgOption Arity(int minimum, int maximum);
    /// <summary>Makes the option repeatable.</summary>
    ArgOption Repeatable();
    /// <summary>Makes the option count occurrences.</summary>
    ArgOption Count();
    /// <summary>Sets the default value.</summary>
    ArgOption Default(string value);
    /// <summary>Sets the optional-value fallback.</summary>
    ArgOption Fallback(string value);
    /// <summary>Sets the environment fallback.</summary>
    ArgOption Env(string name);
    /// <summary>Adds an allowed value.</summary>
    ArgOption Choice(string value);
    /// <summary>Sets the value normalizer.</summary>
    ArgOption Normalize(System.Func<string, string> normalizer);
    /// <summary>Sets the converted-choice converter.</summary>
    ArgOption ConvertChoices(System.Func<string, ArgConvertResult?> converter);
    /// <summary>Sets the choice-validation stage.</summary>
    ArgOption ValidateChoicesAt(ArgChoiceValidationStage stage);
    /// <summary>Adds a conflicting option name.</summary>
    ArgOption Conflicts(string name);
    /// <summary>Adds a required peer option name.</summary>
    ArgOption Requires(string name);
    /// <summary>Sets a deprecation message.</summary>
    ArgOption Deprecated(string message);
    /// <summary>Adds an implied option and value.</summary>
    ArgOption Implies(string name, string value);
    /// <summary>Adds a completion value.</summary>
    ArgOption Complete(string value);
}
