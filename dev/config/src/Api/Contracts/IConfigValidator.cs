namespace config;

/// <summary>Defines reusable configuration validation operations.</summary>
public interface IConfigValidator
{
    /// <summary>Adds a validation rule.</summary>
    ConfigValidator Add(ConfigValidationRule rule);
    /// <summary>Adds a required-value rule.</summary>
    ConfigValidator Require(string path);
    /// <summary>Adds a value-type rule.</summary>
    ConfigValidator Type(string path, ConfigValidationValueType valueType);
    /// <summary>Adds required native-string rules.</summary>
    ConfigValidator RequireString(string path);
    /// <summary>Adds required integer-conversion rules.</summary>
    ConfigValidator RequireInt(string path);
    /// <summary>Adds an inclusive numeric range rule.</summary>
    ConfigValidator Range(string path, decimal min, decimal max);
    /// <summary>Adds an inclusive string-length rule.</summary>
    ConfigValidator Length(string path, int min, int max);
    /// <summary>Adds an inclusive native-array length rule.</summary>
    ConfigValidator ArrayLength(string path, int min, int max);
    /// <summary>Adds an allowed-values rule.</summary>
    ConfigValidator Allowed(string path, string[] values);
    /// <summary>Registers or replaces an application-defined named validator.</summary>
    ConfigValidator Register(IConfigNamedValidator validator);
    /// <summary>Adds a rule that invokes a registered named validator.</summary>
    ConfigValidator Custom(string path, string validatorName);
    /// <summary>Gets the rule count.</summary>
    int RuleCount();
    /// <summary>Gets a copy of one rule.</summary>
    ConfigValidationRule Rule(int index);
    /// <summary>Copies all rules.</summary>
    ConfigValidationRule[] ToArray();
    /// <summary>Validates a snapshot and collects all errors.</summary>
    ConfigValidationResult Validate(Config config);
    /// <summary>Validates a snapshot using a selected error mode.</summary>
    ConfigValidationResult ValidateWithMode(Config config, ConfigErrorMode mode);
    /// <summary>Validates a snapshot and throws the first failed rule.</summary>
    ConfigValidationResult ValidateOrThrow(Config config);
}
