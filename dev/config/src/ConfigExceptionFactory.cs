namespace config;

internal class ConfigExceptionFactory
{
    internal static void ThrowBuildFailure(ConfigDiagnostics diagnostics)
    {
        if (!diagnostics.HasErrors())
        {
            return;
        }

        ConfigError error = diagnostics.Error(0);
        if (error.FailureKind == ConfigFailureKind.Parse)
        {
            throw new ConfigParseException(error.Message, error.SourceName, error.Path, error.LineNumber, error.ColumnNumber);
        }

        if (error.FailureKind == ConfigFailureKind.Merge)
        {
            throw new ConfigMergeException(error.Message, error.SourceName, error.Path, error.LineNumber, error.ColumnNumber);
        }

        throw new ConfigLoadException(error.Message, error.SourceName, error.Path, error.LineNumber, error.ColumnNumber);
    }

    internal static void ThrowValidationFailure(ConfigValidationResult result)
    {
        if (result.IsValid)
        {
            return;
        }

        ConfigError error = result.Diagnostics.Error(0);
        throw new ConfigValidationException(error.Message, error.SourceName, error.Path, error.LineNumber, error.ColumnNumber);
    }

    internal static void ThrowReloadFailure(ConfigDiagnostics diagnostics)
    {
        if (!diagnostics.HasErrors())
        {
            return;
        }

        ConfigError error = diagnostics.Error(0);
        throw new ConfigReloadException(error.Message, error.SourceName, error.Path, error.LineNumber, error.ColumnNumber);
    }

    internal static ConfigConversionException ConversionFromDiagnostic(string message, ConfigError diagnostic)
    {
        return new ConfigConversionException(message, diagnostic.SourceName, diagnostic.Path, diagnostic.LineNumber, diagnostic.ColumnNumber);
    }

    internal static ConfigConversionException ConversionFromValue(string message, string path, ConfigValue value)
    {
        string sourceName = "";
        if (value.SourceName != null)
        {
            sourceName = value.SourceName;
        }

        string configPath = path;
        if (value.OriginalKey != null && value.OriginalKey.Length > 0)
        {
            configPath = value.OriginalKey;
        }

        return new ConfigConversionException(message, sourceName, configPath, value.LineNumber, value.ColumnNumber);
    }

    internal static ConfigConversionException ConversionWithoutSource(string message, string path)
    {
        return new ConfigConversionException(message, "", path, 0, 0);
    }
}
