namespace config;

/// <summary>Identifies the shape of a <see cref="ConfigValue"/>.</summary>
public enum ConfigValueKind
{
    /// <summary>Indicates that no value exists at the requested path.</summary>
    Missing = 0,
    /// <summary>Indicates an explicitly configured null value.</summary>
    Null = 1,
    /// <summary>Indicates a text value.</summary>
    String = 2,
    /// <summary>Indicates a decimal numeric value.</summary>
    Number = 3,
    /// <summary>Indicates a true or false value.</summary>
    Boolean = 4,
    /// <summary>Indicates an ordered collection of values.</summary>
    Array = 5,
    /// <summary>Indicates an object containing named values.</summary>
    Object = 6
}

/// <summary>Specifies the text format used to parse a configuration source.</summary>
public enum ConfigFormat
{
    /// <summary>Parses JavaScript Object Notation.</summary>
    Json = 0,
    /// <summary>Parses section-based INI text.</summary>
    Ini = 1,
    /// <summary>Parses Java properties text.</summary>
    Properties = 2,
    /// <summary>Parses dotenv key-value text.</summary>
    DotEnv = 3,
    /// <summary>Parses simple flat key-value text.</summary>
    Flat = 4,
    /// <summary>Parses XML elements into configuration objects.</summary>
    Xml = 5
}

/// <summary>Controls whether a missing file or directory is an error.</summary>
public enum ConfigOptional
{
    /// <summary>Reports a diagnostic error when the source does not exist.</summary>
    Required = 0,
    /// <summary>Silently contributes no values when the source does not exist.</summary>
    Optional = 1
}

/// <summary>Controls whether a file participates in automatic reload.</summary>
public enum ConfigReload
{
    /// <summary>Loads the file only when the configuration is built or manually reloaded.</summary>
    None = 0,
    /// <summary>Watches the file and schedules a reload when it changes.</summary>
    OnChange = 1
}

/// <summary>Controls how values from successive sources are combined.</summary>
public enum ConfigMergeMode
{
    /// <summary>Lets values from later sources replace values from earlier sources.</summary>
    Override = 0,
    /// <summary>Uses override behavior but reports incompatible value kinds as errors.</summary>
    Strict = 1,
    /// <summary>Keeps existing values and uses later sources only to fill missing paths.</summary>
    Fallback = 2
}

/// <summary>Controls how arrays from successive sources are combined.</summary>
public enum ConfigArrayMergeMode
{
    /// <summary>Replaces an existing array with the later array.</summary>
    Replace = 0,
    /// <summary>Adds later array items after existing items.</summary>
    Append = 1
}

/// <summary>Controls how explicit null values affect an existing value.</summary>
public enum ConfigNullMergeMode
{
    /// <summary>Lets a later null replace the existing value.</summary>
    Replace = 0,
    /// <summary>Ignores a later null and keeps the existing value.</summary>
    Ignore = 1
}

/// <summary>Identifies the merge outcome represented by a configuration trace.</summary>
public enum ConfigMergeDecision
{
    /// <summary>The trace is not a merge-decision trace.</summary>
    None = 0,
    /// <summary>An incoming value replaced an existing value.</summary>
    Replaced = 1,
    /// <summary>Fallback merging retained an existing value.</summary>
    FallbackKept = 2,
    /// <summary>Strict merging rejected an incoming value with an incompatible type.</summary>
    StrictConflict = 3,
    /// <summary>An incoming array was appended to an existing array.</summary>
    Appended = 4,
    /// <summary>An incoming null was ignored by the configured null policy.</summary>
    Ignored = 5,
    /// <summary>The source supplied a value present in the completed snapshot.</summary>
    FinalSupplier = 6
}

/// <summary>Controls how a parser handles repeated keys within one text source.</summary>
public enum ConfigDuplicateKeyMode
{
    /// <summary>Keeps the first value and reports the repeated key as an error.</summary>
    Error = 0,
    /// <summary>Replaces the earlier value with the last value.</summary>
    LastWins = 1,
    /// <summary>Recursively combines repeated object values and reports other repeated value kinds as errors.</summary>
    MergeObjects = 2
}

/// <summary>Controls whether dotenv values expand references to other dotenv keys.</summary>
public enum ConfigDotEnvExpansionMode
{
    /// <summary>Preserves variable-reference text without expansion.</summary>
    Disabled = 0,
    /// <summary>Expands brace and bare variable references after parsing the complete dotenv source.</summary>
    Enabled = 1
}

/// <summary>Controls the result of an enabled dotenv expansion when a referenced key is missing.</summary>
public enum ConfigDotEnvMissingValueMode
{
    /// <summary>Uses an empty string and reports a warning.</summary>
    Empty = 0,
    /// <summary>Preserves the reference text and reports a warning.</summary>
    Preserve = 1,
    /// <summary>Uses an empty string and reports an error.</summary>
    Error = 2
}

/// <summary>Controls whether a substitution source participates in reference lookup.</summary>
public enum ConfigSubstitutionLookupMode
{
    /// <summary>Does not consult the substitution source.</summary>
    Disabled = 0,
    /// <summary>Consults the substitution source.</summary>
    Enabled = 1
}

/// <summary>Controls diagnostics for a required substitution reference that cannot be found.</summary>
public enum ConfigSubstitutionMissingMode
{
    /// <summary>Replaces the missing reference with empty text without an error.</summary>
    Empty = 0,
    /// <summary>Replaces the missing reference with empty text and records an error.</summary>
    Error = 1
}

/// <summary>Controls whether an environment source stores variables whose values are empty strings.</summary>
public enum ConfigEmptyEnvMode
{
    /// <summary>Stores empty strings as present configuration values.</summary>
    Keep = 0,
    /// <summary>Skips environment variables whose values are empty strings.</summary>
    Ignore = 1
}

/// <summary>Controls whether object-key comparisons consider letter casing.</summary>
public enum ConfigCaseMode
{
    /// <summary>Uses ordinal, case-sensitive key comparisons.</summary>
    Sensitive = 0,
    /// <summary>Uses ordinal, case-insensitive key comparisons.</summary>
    Insensitive = 1
}

/// <summary>Controls whether validation stops after its first error.</summary>
public enum ConfigErrorMode
{
    /// <summary>Evaluates every rule and collects all validation errors.</summary>
    Collect = 0,
    /// <summary>Stops validation as soon as one rule fails.</summary>
    FailFast = 1
}

/// <summary>Identifies the outcome of an exception-less typed configuration read.</summary>
public enum ConfigReadStatus
{
    /// <summary>The value was present, non-null, and converted successfully.</summary>
    Success = 0,
    /// <summary>The configuration path was not present.</summary>
    Missing = 1,
    /// <summary>The configuration path contained an explicit null.</summary>
    Null = 2,
    /// <summary>The value or requested runtime type could not be converted.</summary>
    Malformed = 3
}
