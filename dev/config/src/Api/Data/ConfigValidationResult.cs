namespace config;

/// <summary>Contains diagnostics produced by applying validation rules.</summary>
public class ConfigValidationResult
{
    /// <summary>Contains all validation errors collected according to the selected error mode.</summary>
    public ConfigDiagnostics Diagnostics;
    /// <summary>Indicates whether validation completed without an error.</summary>
    public bool IsValid;
    /// <summary>Contains the number of failed validation rules.</summary>
    public int ErrorCount;
    /// <summary>Contains independent copies of validation errors in rule order.</summary>
    public ConfigError[] Errors;

    internal ConfigValidationResult()
    {
        Diagnostics = new ConfigDiagnostics();
        IsValid = true;
        ErrorCount = 0;
        Errors = new ConfigError[0];
    }

    internal void Refresh()
    {
        ErrorCount = Diagnostics.ErrorCount();
        IsValid = ErrorCount == 0;
        Errors = new ConfigError[ErrorCount];

        int index = 0;
        while (index < ErrorCount)
        {
            ConfigError error = Diagnostics.Error(index);
            ConfigError copiedError = new ConfigError(error.SourceName, error.Path, error.Message, error.LineNumber, error.ColumnNumber);
            copiedError.FailureKind = error.FailureKind;
            Errors[index] = copiedError;
            index = index + 1;
        }
    }

    internal ConfigValidationResult Copy()
    {
        ConfigValidationResult result = new ConfigValidationResult();
        result.Diagnostics = Diagnostics.Copy();
        result.Refresh();
        return result;
    }
}
