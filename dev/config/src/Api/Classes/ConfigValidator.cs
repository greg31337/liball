using System;
using System.Collections.Generic;
using System.Globalization;

namespace config;

/// <summary>Collects reusable validation rules and applies them to configuration snapshots.</summary>
public class ConfigValidator : IConfigValidator
{
    internal List<ConfigValidationRule> Rules;
    internal Dictionary<string, IConfigNamedValidator> NamedValidators;

    /// <summary>Creates an empty validator.</summary>
    public ConfigValidator()
    {
        Rules = new List<ConfigValidationRule>();
        NamedValidators = new Dictionary<string, IConfigNamedValidator>(StringComparer.Ordinal);
    }

    /// <summary>Adds a copy of a validation rule.</summary>
    /// <param name="rule">The rule to add.</param>
    /// <returns>This validator for fluent calls.</returns>
    public ConfigValidator Add(ConfigValidationRule rule)
    {
        Rules.Add(rule.Copy());
        return this;
    }

    /// <summary>Adds a rule that rejects a missing or explicit null value.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>This validator for fluent calls.</returns>
    public ConfigValidator Require(string path)
    {
        return Add(ConfigValidationRule.Required(path));
    }

    /// <summary>Adds a type rule that is evaluated only when the path has a non-null value.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="valueType">The accepted conversion or container type.</param>
    /// <returns>This validator for fluent calls.</returns>
    public ConfigValidator Type(string path, ConfigValidationValueType valueType)
    {
        return Add(ConfigValidationRule.Type(path, valueType));
    }

    /// <summary>Adds both required and native-string rules for a path.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <returns>This validator for fluent calls.</returns>
    public ConfigValidator RequireString(string path)
    {
        Require(path);
        Type(path, ConfigValidationValueType.String);
        return this;
    }

    /// <summary>Adds both required and 32-bit integer-conversion rules for a path.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <returns>This validator for fluent calls.</returns>
    public ConfigValidator RequireInt(string path)
    {
        Require(path);
        Type(path, ConfigValidationValueType.Integer);
        return this;
    }

    /// <summary>Adds an inclusive numeric range rule.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="min">The minimum accepted value.</param>
    /// <param name="max">The maximum accepted value.</param>
    /// <returns>This validator for fluent calls.</returns>
    public ConfigValidator Range(string path, decimal min, decimal max)
    {
        return Add(ConfigValidationRule.Range(path, min, max));
    }

    /// <summary>Adds an inclusive string-length rule.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="min">The minimum accepted character count.</param>
    /// <param name="max">The maximum accepted character count.</param>
    /// <returns>This validator for fluent calls.</returns>
    public ConfigValidator Length(string path, int min, int max)
    {
        return Add(ConfigValidationRule.Length(path, min, max));
    }

    /// <summary>Adds an inclusive native-array length rule.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="min">The minimum accepted item count.</param>
    /// <param name="max">The maximum accepted item count.</param>
    /// <returns>This validator for fluent calls.</returns>
    public ConfigValidator ArrayLength(string path, int min, int max)
    {
        return Add(ConfigValidationRule.ArrayLength(path, min, max));
    }

    /// <summary>Adds an exact, case-sensitive allowed-values rule.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="values">The accepted values, which are copied when the rule is added.</param>
    /// <returns>This validator for fluent calls.</returns>
    public ConfigValidator Allowed(string path, string[] values)
    {
        return Add(ConfigValidationRule.Allowed(path, values));
    }

    /// <summary>Registers or replaces an application-defined validator by its case-sensitive name.</summary>
    /// <param name="validator">The named validator instance.</param>
    /// <returns>This validator for fluent calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the validator is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the validator name is null or empty.</exception>
    public ConfigValidator Register(IConfigNamedValidator validator)
    {
        if (validator == null)
        {
            throw new ArgumentNullException("validator");
        }

        string name = validator.Name();
        if (name == null || name.Length == 0)
        {
            throw new ArgumentException("validator name must not be empty", "validator");
        }

        NamedValidators[name] = validator;
        return this;
    }

    /// <summary>Adds a rule that invokes an application-defined validator by name.</summary>
    /// <param name="path">The configuration path to check.</param>
    /// <param name="validatorName">The case-sensitive registered validator name.</param>
    /// <returns>This validator for fluent calls.</returns>
    /// <exception cref="ArgumentException">Thrown when the validator name is null or empty.</exception>
    public ConfigValidator Custom(string path, string validatorName)
    {
        if (validatorName == null || validatorName.Length == 0)
        {
            throw new ArgumentException("validator name must not be empty", "validatorName");
        }

        return Add(ConfigValidationRule.Named(path, validatorName));
    }

    /// <summary>Gets the number of collected rules.</summary>
    /// <returns>The rule count.</returns>
    public int RuleCount()
    {
        return Rules.Count;
    }

    /// <summary>Gets a copy of one rule without throwing for an out-of-range index.</summary>
    /// <param name="index">The zero-based rule index.</param>
    /// <returns>A rule copy, or an empty required rule when the index is outside the collection.</returns>
    public ConfigValidationRule Rule(int index)
    {
        if (index < 0)
        {
            return new ConfigValidationRule();
        }

        if (index >= Rules.Count)
        {
            return new ConfigValidationRule();
        }

        return Rules[index].Copy();
    }

    /// <summary>Copies all collected rules.</summary>
    /// <returns>A new array containing independent rule copies.</returns>
    public ConfigValidationRule[] ToArray()
    {
        ConfigValidationRule[] rules = new ConfigValidationRule[Rules.Count];

        int index = 0;
        while (index < Rules.Count)
        {
            rules[index] = Rules[index].Copy();
            index = index + 1;
        }

        return rules;
    }

    /// <summary>Applies every collected rule and gathers all errors.</summary>
    /// <param name="config">The snapshot to validate.</param>
    /// <returns>The validation result.</returns>
    public ConfigValidationResult Validate(Config config)
    {
        return ValidateWithMode(config, ConfigErrorMode.Collect);
    }

    /// <summary>Applies collected rules using the requested error collection mode.</summary>
    /// <param name="config">The snapshot to validate.</param>
    /// <param name="mode">Whether to collect all errors or stop after the first.</param>
    /// <returns>The validation result.</returns>
    public ConfigValidationResult ValidateWithMode(Config config, ConfigErrorMode mode)
    {
        return ValidateRegistered(config, ToArray(), mode, NamedValidators);
    }

    /// <summary>Applies every collected rule and throws the first validation failure.</summary>
    /// <param name="config">The snapshot to validate.</param>
    /// <returns>The successful validation result.</returns>
    /// <exception cref="ConfigValidationException">Thrown when a rule fails.</exception>
    public ConfigValidationResult ValidateOrThrow(Config config)
    {
        ConfigValidationResult result = ValidateWithMode(config, ConfigErrorMode.FailFast);
        ConfigExceptionFactory.ThrowValidationFailure(result);
        return result;
    }

    /// <summary>Applies a supplied rule array and gathers all errors.</summary>
    /// <param name="config">The snapshot to validate.</param>
    /// <param name="rules">The rules to apply in array order.</param>
    /// <returns>The validation result.</returns>
    internal static ConfigValidationResult Validate(Config config, ConfigValidationRule[] rules)
    {
        return Validate(config, rules, ConfigErrorMode.Collect);
    }

    /// <summary>Applies a supplied rule array using the requested error collection mode.</summary>
    /// <param name="config">The snapshot to validate.</param>
    /// <param name="rules">The rules to apply in array order.</param>
    /// <param name="mode">Whether to collect all errors or stop after the first.</param>
    /// <returns>The validation result.</returns>
    internal static ConfigValidationResult Validate(Config config, ConfigValidationRule[] rules, ConfigErrorMode mode)
    {
        Dictionary<string, IConfigNamedValidator> namedValidators = new Dictionary<string, IConfigNamedValidator>(StringComparer.Ordinal);
        return ValidateRegistered(config, rules, mode, namedValidators);
    }

    internal static ConfigValidationResult ValidateRegistered(Config config, ConfigValidationRule[] rules, ConfigErrorMode mode, Dictionary<string, IConfigNamedValidator> namedValidators)
    {
        ConfigValidationResult result = new ConfigValidationResult();

        int index = 0;
        while (index < rules.Length)
        {
            bool stop = ValidateRule(config, rules[index], result, mode, namedValidators);
            if (stop)
            {
                result.Refresh();
                return result;
            }

            index = index + 1;
        }

        result.Refresh();
        return result;
    }

    internal static bool ValidateRule(Config config, ConfigValidationRule rule, ConfigValidationResult result, ConfigErrorMode mode, Dictionary<string, IConfigNamedValidator> namedValidators)
    {
        ConfigValue value = config.Value(rule.Path);

        if (rule.Kind == ConfigValidationRuleKind.Required)
        {
            if (value.Kind == ConfigValueKind.Missing || value.Kind == ConfigValueKind.Null)
            {
                return AddError(result, rule, value, "missing required value", mode);
            }

            return false;
        }

        if (value.Kind == ConfigValueKind.Missing || value.Kind == ConfigValueKind.Null)
        {
            return false;
        }

        if (rule.Kind == ConfigValidationRuleKind.Type)
        {
            if (!MatchesType(value, rule.ValueType))
            {
                return AddError(result, rule, value, "expected " + ValueTypeName(rule.ValueType) + " value", mode);
            }

            return false;
        }

        if (rule.Kind == ConfigValidationRuleKind.NumericRange)
        {
            ConfigValidationNumber number = NumberValue(value);
            if (!number.HasValue)
            {
                return AddError(result, rule, value, "expected numeric value", mode);
            }

            if (rule.HasMinNumber && number.Value < rule.MinNumber)
            {
                return AddError(result, rule, value, "numeric value outside range", mode);
            }

            if (rule.HasMaxNumber && number.Value > rule.MaxNumber)
            {
                return AddError(result, rule, value, "numeric value outside range", mode);
            }

            return false;
        }

        if (rule.Kind == ConfigValidationRuleKind.StringLength)
        {
            if (value.Kind != ConfigValueKind.String)
            {
                return AddError(result, rule, value, "expected string value", mode);
            }

            int length = 0;
            if (value.StringValue != null)
            {
                length = value.StringValue.Length;
            }

            if (rule.HasMinLength && length < rule.MinLength)
            {
                return AddError(result, rule, value, "string length outside range", mode);
            }

            if (rule.HasMaxLength && length > rule.MaxLength)
            {
                return AddError(result, rule, value, "string length outside range", mode);
            }

            return false;
        }

        if (rule.Kind == ConfigValidationRuleKind.ArrayLength)
        {
            if (value.Kind != ConfigValueKind.Array || value.ArrayValue == null)
            {
                return AddError(result, rule, value, "expected array value", mode);
            }

            int length = value.ArrayValue.Count();
            if (rule.HasMinLength && length < rule.MinLength)
            {
                return AddError(result, rule, value, "array length outside range", mode);
            }

            if (rule.HasMaxLength && length > rule.MaxLength)
            {
                return AddError(result, rule, value, "array length outside range", mode);
            }

            return false;
        }

        if (rule.Kind == ConfigValidationRuleKind.AllowedValues)
        {
            string text = ConfigConversions.StringValue(value);
            if (!ContainsAllowedValue(rule.AllowedValues, text))
            {
                return AddError(result, rule, value, "value is not allowed", mode);
            }

            return false;
        }

        if (rule.Kind == ConfigValidationRuleKind.Named)
        {
            if (!namedValidators.ContainsKey(rule.ValidatorName))
            {
                return AddError(result, rule, value, "named validator is not registered: " + rule.ValidatorName, mode);
            }

            string message = namedValidators[rule.ValidatorName].Validate(value.Copy());
            if (message != null && message.Length > 0)
            {
                return AddError(result, rule, value, message, mode);
            }
        }

        return false;
    }

    internal static bool AddError(ConfigValidationResult result, ConfigValidationRule rule, ConfigValue value, string message, ConfigErrorMode mode)
    {
        result.Diagnostics.AddError(ValueSourceName(value), rule.Path, message, value.LineNumber, value.ColumnNumber);
        return mode == ConfigErrorMode.FailFast;
    }

    internal static string ValueSourceName(ConfigValue value)
    {
        if (value.SourceName == null)
        {
            return "";
        }

        return value.SourceName;
    }

    internal static bool MatchesType(ConfigValue value, ConfigValidationValueType valueType)
    {
        if (valueType == ConfigValidationValueType.Any)
        {
            return true;
        }

        if (valueType == ConfigValidationValueType.String)
        {
            return value.Kind == ConfigValueKind.String;
        }

        if (valueType == ConfigValidationValueType.Boolean)
        {
            return CanReadBool(value);
        }

        if (valueType == ConfigValidationValueType.Integer)
        {
            return CanReadInt(value);
        }

        if (valueType == ConfigValidationValueType.Long)
        {
            return CanReadLong(value);
        }

        if (valueType == ConfigValidationValueType.Decimal)
        {
            return NumberValue(value).HasValue;
        }

        if (valueType == ConfigValidationValueType.Double)
        {
            return CanReadDouble(value);
        }

        if (valueType == ConfigValidationValueType.TimeSpan)
        {
            return CanReadTimeSpan(value);
        }

        if (valueType == ConfigValidationValueType.ByteSize)
        {
            return CanReadByteSize(value);
        }

        if (valueType == ConfigValidationValueType.StringArray)
        {
            return value.Kind == ConfigValueKind.Array || value.Kind == ConfigValueKind.String || value.Kind == ConfigValueKind.Number || value.Kind == ConfigValueKind.Boolean;
        }

        if (valueType == ConfigValidationValueType.Array)
        {
            return value.Kind == ConfigValueKind.Array;
        }

        if (valueType == ConfigValidationValueType.Object)
        {
            return value.Kind == ConfigValueKind.Object;
        }

        return false;
    }

    internal static ConfigValidationNumber NumberValue(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            return new ConfigValidationNumber(true, value.NumberValue);
        }

        if (value.Kind == ConfigValueKind.Boolean)
        {
            if (value.BooleanValue)
            {
                return new ConfigValidationNumber(true, 1m);
            }

            return new ConfigValidationNumber(true, 0m);
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            decimal parsed = 0m;
            if (decimal.TryParse(value.StringValue.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                return new ConfigValidationNumber(true, parsed);
            }
        }

        return new ConfigValidationNumber(false, 0m);
    }

    internal static bool CanReadBool(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Boolean || value.Kind == ConfigValueKind.Number)
        {
            return true;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            string text = value.StringValue.Trim().ToLowerInvariant();
            return text == "true" || text == "1" || text == "yes" || text == "y" || text == "on" || text == "false" || text == "0" || text == "no" || text == "n" || text == "off";
        }

        return false;
    }

    internal static bool CanReadInt(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            return ConfigConversions.IsWholeDecimal(value.NumberValue) && value.NumberValue >= int.MinValue && value.NumberValue <= int.MaxValue;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            int parsed = 0;
            return int.TryParse(value.StringValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
        }

        return false;
    }

    internal static bool CanReadLong(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            return ConfigConversions.IsWholeDecimal(value.NumberValue) && value.NumberValue >= long.MinValue && value.NumberValue <= long.MaxValue;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            long parsed = 0L;
            return long.TryParse(value.StringValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
        }

        return false;
    }

    internal static bool CanReadDouble(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Number || value.Kind == ConfigValueKind.Boolean)
        {
            return true;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            double parsed = 0d;
            return double.TryParse(value.StringValue.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsed);
        }

        return false;
    }

    internal static bool CanReadTimeSpan(ConfigValue value)
    {
        TimeSpan first = ConfigConversions.TimeSpanValue(value, TimeSpan.MinValue);
        TimeSpan second = ConfigConversions.TimeSpanValue(value, TimeSpan.MaxValue);
        return first == second;
    }

    internal static bool CanReadByteSize(ConfigValue value)
    {
        long first = ConfigConversions.ByteSizeValue(value, long.MinValue);
        long second = ConfigConversions.ByteSizeValue(value, long.MaxValue);
        return first == second;
    }

    internal static bool ContainsAllowedValue(string[] values, string value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string ValueTypeName(ConfigValidationValueType valueType)
    {
        if (valueType == ConfigValidationValueType.String)
        {
            return "string";
        }

        if (valueType == ConfigValidationValueType.Boolean)
        {
            return "boolean";
        }

        if (valueType == ConfigValidationValueType.Integer)
        {
            return "integer";
        }

        if (valueType == ConfigValidationValueType.Long)
        {
            return "long";
        }

        if (valueType == ConfigValidationValueType.Decimal)
        {
            return "decimal";
        }

        if (valueType == ConfigValidationValueType.Double)
        {
            return "double";
        }

        if (valueType == ConfigValidationValueType.TimeSpan)
        {
            return "timespan";
        }

        if (valueType == ConfigValidationValueType.ByteSize)
        {
            return "byte-size";
        }

        if (valueType == ConfigValidationValueType.StringArray)
        {
            return "string array";
        }

        if (valueType == ConfigValidationValueType.Array)
        {
            return "array";
        }

        if (valueType == ConfigValidationValueType.Object)
        {
            return "object";
        }

        return "configured";
    }
}
