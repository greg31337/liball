namespace config;

/// <summary>Identifies the operation performed by a configuration validation rule.</summary>
public enum ConfigValidationRuleKind
{
    /// <summary>Rejects a missing or explicit null value.</summary>
    Required = 0,
    /// <summary>Checks a value conversion or container type.</summary>
    Type = 1,
    /// <summary>Checks an inclusive numeric range.</summary>
    NumericRange = 2,
    /// <summary>Checks an inclusive string-length range.</summary>
    StringLength = 3,
    /// <summary>Checks an exact allowed-value set.</summary>
    AllowedValues = 4,
    /// <summary>Checks an inclusive native-array length range.</summary>
    ArrayLength = 5,
    /// <summary>Invokes an application-defined named validator.</summary>
    Named = 6
}
