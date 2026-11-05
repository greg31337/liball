using System.Collections.Generic;
using System.IO;
using System.Text;

namespace config;

internal enum ConfigSourceKind
{
    Memory = 0,
    Defaults = 1,
    ProgrammaticOverride = 2,
    File = 3,
    Stream = 4,
    String = 5,
    Directory = 6,
    KeyPerFile = 7,
    Environment = 8,
    CommandLine = 9,
    Search = 10
}

internal class ConfigSource : IConfigSource
{
    internal ConfigSourceKind Kind;
    internal string SourceName;
    internal ConfigMap? Values;
    internal string Path;
    internal string Pattern;
    internal ConfigFormat Format;
    internal bool UseExtensionFormat;
    internal ConfigOptional Optional;
    internal ConfigReload Reload;
    internal Stream? StreamValue;
    internal string Text;
    internal string Prefix;
    internal string Delimiter;
    internal string OverridePath;
    internal string OverrideValue;
    internal string[]? Args;
    internal ConfigMap? SwitchMap;
    internal Dictionary<string, string>? EnvironmentValues;
    internal ConfigMap? EnvironmentPathMap;
    internal ConfigEmptyEnvMode EmptyEnvMode;
    internal ConfigDuplicateKeyMode DuplicateKeyMode;
    internal ConfigDotEnvExpansionMode DotEnvExpansionMode;
    internal ConfigDotEnvMissingValueMode DotEnvMissingValueMode;
    internal Encoding TextEncoding;
    internal int ParentSearchLimit;
    internal string EnvironmentName;
    internal ConfigOptional MissingIncludes;
    internal int IncludeDepthLimit;

    internal ConfigSource()
    {
        Kind = ConfigSourceKind.Memory;
        SourceName = "";
        Values = null;
        Path = "";
        Pattern = "";
        Format = ConfigFormat.Json;
        UseExtensionFormat = false;
        Optional = ConfigOptional.Required;
        Reload = ConfigReload.None;
        StreamValue = null;
        Text = "";
        Prefix = "";
        Delimiter = ".";
        OverridePath = "";
        OverrideValue = "";
        Args = null;
        SwitchMap = null;
        EnvironmentValues = null;
        EnvironmentPathMap = null;
        EmptyEnvMode = ConfigEmptyEnvMode.Keep;
        DuplicateKeyMode = ConfigDuplicateKeyMode.Error;
        DotEnvExpansionMode = ConfigDotEnvExpansionMode.Disabled;
        DotEnvMissingValueMode = ConfigDotEnvMissingValueMode.Empty;
        TextEncoding = Encoding.UTF8;
        ParentSearchLimit = 0;
        EnvironmentName = "";
        MissingIncludes = ConfigOptional.Required;
        IncludeDepthLimit = 32;
    }

    internal static ConfigSource Memory(ConfigMap values, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Memory;
        source.Values = values;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource Defaults(ConfigMap values)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Defaults;
        source.Values = values;
        source.SourceName = "defaults";
        return source;
    }

    internal static ConfigSource ProgrammaticOverride(string path, string value, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.ProgrammaticOverride;
        source.OverridePath = path;
        source.OverrideValue = value;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource File(string path, ConfigFormat format, ConfigOptional optional, ConfigReload reload, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.File;
        source.Path = path;
        source.Format = format;
        source.Optional = optional;
        source.Reload = reload;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource FileByExtension(string path, ConfigOptional optional, ConfigReload reload, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.File;
        source.Path = path;
        source.Optional = optional;
        source.Reload = reload;
        source.SourceName = sourceName;
        source.UseExtensionFormat = true;
        return source;
    }

    internal static ConfigSource StreamSource(Stream stream, ConfigFormat format, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Stream;
        source.StreamValue = stream;
        source.Format = format;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource StringSource(string text, ConfigFormat format, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.String;
        source.Text = text;
        source.Format = format;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource DirectorySource(string path, string pattern, ConfigOptional optional, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Directory;
        source.Path = path;
        source.Pattern = pattern;
        source.Optional = optional;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource KeyPerFileDirectory(string path, ConfigOptional optional, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.KeyPerFile;
        source.Path = path;
        source.Optional = optional;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource SearchFile(string startDirectory, string fileName, int maximumParentLevels, string environmentName, ConfigOptional optional, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Search;
        source.Path = startDirectory;
        source.Pattern = fileName;
        source.ParentSearchLimit = maximumParentLevels;
        source.EnvironmentName = environmentName;
        source.Optional = optional;
        source.SourceName = sourceName;
        source.UseExtensionFormat = true;
        return source;
    }

    internal static ConfigSource Environment(string prefix, string delimiter, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Environment;
        source.Prefix = prefix;
        source.Delimiter = delimiter;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource EnvironmentMap(Dictionary<string, string> values, string prefix, string delimiter, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Environment;
        source.EnvironmentValues = values;
        source.Prefix = prefix;
        source.Delimiter = delimiter;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource EnvironmentWithMappings(string prefix, string delimiter, ConfigMap pathMap, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Environment;
        source.Prefix = prefix;
        source.Delimiter = delimiter;
        source.EnvironmentPathMap = pathMap;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource EnvironmentMapWithMappings(Dictionary<string, string> values, string prefix, string delimiter, ConfigMap pathMap, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Environment;
        source.EnvironmentValues = values;
        source.Prefix = prefix;
        source.Delimiter = delimiter;
        source.EnvironmentPathMap = pathMap;
        source.SourceName = sourceName;
        return source;
    }

    internal static ConfigSource CommandLine(string[] args, ConfigMap switchMap, string sourceName)
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.CommandLine;
        source.Args = args;
        source.SwitchMap = switchMap;
        source.SourceName = sourceName;
        return source;
    }

    internal ConfigSource Copy()
    {
        ConfigSource copy = new ConfigSource();
        copy.Kind = Kind;
        copy.SourceName = SourceName;
        if (Values != null)
        {
            copy.Values = Values.Copy();
        }

        copy.Path = Path;
        copy.Pattern = Pattern;
        copy.Format = Format;
        copy.UseExtensionFormat = UseExtensionFormat;
        copy.Optional = Optional;
        copy.Reload = Reload;
        copy.StreamValue = StreamValue;
        copy.Text = Text;
        copy.Prefix = Prefix;
        copy.Delimiter = Delimiter;
        copy.OverridePath = OverridePath;
        copy.OverrideValue = OverrideValue;
        if (Args != null)
        {
            copy.Args = CopyStringArray(Args);
        }

        if (SwitchMap != null)
        {
            copy.SwitchMap = SwitchMap.Copy();
        }

        if (EnvironmentValues != null)
        {
            copy.EnvironmentValues = CopyStringDictionary(EnvironmentValues);
        }

        if (EnvironmentPathMap != null)
        {
            copy.EnvironmentPathMap = EnvironmentPathMap.Copy();
        }

        copy.EmptyEnvMode = EmptyEnvMode;
        copy.DuplicateKeyMode = DuplicateKeyMode;
        copy.DotEnvExpansionMode = DotEnvExpansionMode;
        copy.DotEnvMissingValueMode = DotEnvMissingValueMode;
        copy.TextEncoding = TextEncoding;
        copy.ParentSearchLimit = ParentSearchLimit;
        copy.EnvironmentName = EnvironmentName;
        copy.MissingIncludes = MissingIncludes;
        copy.IncludeDepthLimit = IncludeDepthLimit;
        return copy;
    }

    /// <summary>Loads this built-in source at the supplied precedence rank.</summary>
    /// <param name="precedenceRank">The one-based position in builder registration order.</param>
    /// <returns>The loaded root and source diagnostics.</returns>
    public ConfigSourceResult Load(int precedenceRank)
    {
        return ConfigSourceLoader.Load(this, precedenceRank);
    }

    internal static string[] CopyStringArray(string[] values)
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

    internal static Dictionary<string, string> CopyStringDictionary(Dictionary<string, string> values)
    {
        Dictionary<string, string> copy = new Dictionary<string, string>();
        string[] keys = new string[values.Keys.Count];
        values.Keys.CopyTo(keys, 0);

        int index = 0;
        while (index < keys.Length)
        {
            copy[keys[index]] = values[keys[index]];
            index = index + 1;
        }

        return copy;
    }
}
