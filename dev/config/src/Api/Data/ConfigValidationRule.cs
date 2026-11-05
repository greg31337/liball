namespace config;

/// <summary>Represents one built-in or application-defined validation check.</summary>
public class ConfigValidationRule
{
    /// <summary>Identifies the validation operation.</summary>
    public ConfigValidationRuleKind Kind;
    /// <summary>Identifies the configuration path evaluated by this rule.</summary>
    public string Path;
    /// <summary>Identifies the expected conversion or container type for a type rule.</summary>
    public ConfigValidationValueType ValueType;
    /// <summary>Contains the inclusive minimum for a numeric range rule.</summary>
    public decimal MinNumber;
    /// <summary>Contains the inclusive maximum for a numeric range rule.</summary>
    public decimal MaxNumber;
    /// <summary>Indicates whether the numeric minimum is active.</summary>
    public bool HasMinNumber;
    /// <summary>Indicates whether the numeric maximum is active.</summary>
    public bool HasMaxNumber;
    /// <summary>Contains the inclusive minimum for a string- or array-length rule.</summary>
    public int MinLength;
    /// <summary>Contains the inclusive maximum for a string- or array-length rule.</summary>
    public int MaxLength;
    /// <summary>Indicates whether the length minimum is active.</summary>
    public bool HasMinLength;
    /// <summary>Indicates whether the length maximum is active.</summary>
    public bool HasMaxLength;
    /// <summary>Contains copied exact text accepted by an allowed-values rule.</summary>
    public string[] AllowedValues;
    /// <summary>Contains the registered validator name used by a named rule.</summary>
    public string ValidatorName;

    internal ConfigValidationRule()
    {
        Kind = ConfigValidationRuleKind.Required;
        Path = "";
        ValueType = ConfigValidationValueType.Any;
        MinNumber = 0m;
        MaxNumber = 0m;
        HasMinNumber = false;
        HasMaxNumber = false;
        MinLength = 0;
        MaxLength = 0;
        HasMinLength = false;
        HasMaxLength = false;
        AllowedValues = new string[0];
        ValidatorName = "";
    }

    internal ConfigValidationRule Copy()
    {
        ConfigValidationRule copy = new ConfigValidationRule();
        copy.Kind = Kind;
        copy.Path = Path;
        copy.ValueType = ValueType;
        copy.MinNumber = MinNumber;
        copy.MaxNumber = MaxNumber;
        copy.HasMinNumber = HasMinNumber;
        copy.HasMaxNumber = HasMaxNumber;
        copy.MinLength = MinLength;
        copy.MaxLength = MaxLength;
        copy.HasMinLength = HasMinLength;
        copy.HasMaxLength = HasMaxLength;
        copy.AllowedValues = CopyStrings(AllowedValues);
        copy.ValidatorName = ValidatorName;
        return copy;
    }

    /// <summary>Creates a rule that rejects missing and explicit null values.</summary>
    /// <param name="path">The configuration path to require.</param>
    /// <returns>A required-value rule.</returns>
    internal static ConfigValidationRule Required(string path)
    {
        ConfigValidationRule rule = new ConfigValidationRule();
        rule.Kind = ConfigValidationRuleKind.Required;
        rule.Path = path;
        return rule;
    }

    /// <summary>Creates a rule that checks a present value against a conversion or container type.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="valueType">The accepted value type.</param>
    /// <returns>A type-checking rule. Missing and null values are ignored unless a required rule is also used.</returns>
    internal static ConfigValidationRule Type(string path, ConfigValidationValueType valueType)
    {
        ConfigValidationRule rule = new ConfigValidationRule();
        rule.Kind = ConfigValidationRuleKind.Type;
        rule.Path = path;
        rule.ValueType = valueType;
        return rule;
    }

    /// <summary>Creates an inclusive numeric range rule.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="min">The minimum accepted value.</param>
    /// <param name="max">The maximum accepted value.</param>
    /// <returns>A numeric range rule.</returns>
    internal static ConfigValidationRule Range(string path, decimal min, decimal max)
    {
        ConfigValidationRule rule = new ConfigValidationRule();
        rule.Kind = ConfigValidationRuleKind.NumericRange;
        rule.Path = path;
        rule.MinNumber = min;
        rule.MaxNumber = max;
        rule.HasMinNumber = true;
        rule.HasMaxNumber = true;
        return rule;
    }

    /// <summary>Creates an inclusive string-length rule.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="min">The minimum accepted character count.</param>
    /// <param name="max">The maximum accepted character count.</param>
    /// <returns>A string-length rule.</returns>
    internal static ConfigValidationRule Length(string path, int min, int max)
    {
        ConfigValidationRule rule = new ConfigValidationRule();
        rule.Kind = ConfigValidationRuleKind.StringLength;
        rule.Path = path;
        rule.MinLength = min;
        rule.MaxLength = max;
        rule.HasMinLength = true;
        rule.HasMaxLength = true;
        return rule;
    }

    /// <summary>Creates an inclusive native-array length rule.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="min">The minimum accepted item count.</param>
    /// <param name="max">The maximum accepted item count.</param>
    /// <returns>An array-length rule.</returns>
    internal static ConfigValidationRule ArrayLength(string path, int min, int max)
    {
        ConfigValidationRule rule = new ConfigValidationRule();
        rule.Kind = ConfigValidationRuleKind.ArrayLength;
        rule.Path = path;
        rule.MinLength = min;
        rule.MaxLength = max;
        rule.HasMinLength = true;
        rule.HasMaxLength = true;
        return rule;
    }

    /// <summary>Creates a rule that accepts only exact, case-sensitive text values.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="values">The accepted text values, which are copied into the rule.</param>
    /// <returns>An allowed-values rule.</returns>
    internal static ConfigValidationRule Allowed(string path, string[] values)
    {
        ConfigValidationRule rule = new ConfigValidationRule();
        rule.Kind = ConfigValidationRuleKind.AllowedValues;
        rule.Path = path;
        rule.AllowedValues = CopyStrings(values);
        return rule;
    }

    /// <summary>Creates a rule that invokes a registered application validator.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="validatorName">The case-sensitive registered validator name.</param>
    /// <returns>A named-validator rule.</returns>
    internal static ConfigValidationRule Named(string path, string validatorName)
    {
        ConfigValidationRule rule = new ConfigValidationRule();
        rule.Kind = ConfigValidationRuleKind.Named;
        rule.Path = path;
        rule.ValidatorName = validatorName;
        return rule;
    }

    internal static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];

        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }
}
