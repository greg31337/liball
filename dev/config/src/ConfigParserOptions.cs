namespace config;

internal class ConfigParserOptions
{
    internal ConfigDuplicateKeyMode DuplicateKeyMode;
    internal ConfigDotEnvExpansionMode DotEnvExpansionMode;
    internal ConfigDotEnvMissingValueMode DotEnvMissingValueMode;
    internal ConfigOptional MissingIncludes;
    internal int IncludeDepthLimit;

    internal ConfigParserOptions(ConfigDuplicateKeyMode duplicateKeyMode, ConfigDotEnvExpansionMode dotEnvExpansionMode, ConfigDotEnvMissingValueMode dotEnvMissingValueMode, ConfigOptional missingIncludes, int includeDepthLimit)
    {
        DuplicateKeyMode = duplicateKeyMode;
        DotEnvExpansionMode = dotEnvExpansionMode;
        DotEnvMissingValueMode = dotEnvMissingValueMode;
        MissingIncludes = missingIncludes;
        IncludeDepthLimit = includeDepthLimit;
    }

    internal static ConfigParserOptions Conservative()
    {
        return new ConfigParserOptions(ConfigDuplicateKeyMode.Error, ConfigDotEnvExpansionMode.Disabled, ConfigDotEnvMissingValueMode.Empty, ConfigOptional.Required, 32);
    }
}
