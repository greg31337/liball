using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace config;

/// <summary>Collects configuration sources and options, then builds merged snapshots.</summary>
/// <remarks>Sources are loaded in registration order. With the default override mode, each later source has higher precedence than earlier sources.</remarks>
public class ConfigBuilder : IConfigBuilder
{
    internal List<IConfigSource> Sources;
    internal string Delimiter;
    internal ConfigCaseMode CaseMode;
    internal ConfigMergeMode MergeMode;
    internal ConfigArrayMergeMode ArrayMergeMode;
    internal ConfigNullMergeMode NullMergeMode;
    internal ConfigDuplicateKeyMode DuplicateKeyMode;
    internal ConfigDotEnvExpansionMode DotEnvExpansionMode;
    internal ConfigDotEnvMissingValueMode DotEnvMissingValueMode;
    internal ConfigAliasTable Aliases;
    internal ConfigSourceGroupLabels SourceGroupLabels;
    internal ConfigResolutionOptions ResolutionOptions;

    /// <summary>Creates an empty builder with case-sensitive keys, dot-delimited paths, and replacement merge behavior.</summary>
    public ConfigBuilder()
    {
        Sources = new List<IConfigSource>();
        Delimiter = ".";
        CaseMode = ConfigCaseMode.Sensitive;
        MergeMode = ConfigMergeMode.Override;
        ArrayMergeMode = ConfigArrayMergeMode.Replace;
        NullMergeMode = ConfigNullMergeMode.Replace;
        DuplicateKeyMode = ConfigDuplicateKeyMode.Error;
        DotEnvExpansionMode = ConfigDotEnvExpansionMode.Disabled;
        DotEnvMissingValueMode = ConfigDotEnvMissingValueMode.Empty;
        Aliases = new ConfigAliasTable(ConfigCaseMode.Sensitive);
        SourceGroupLabels = new ConfigSourceGroupLabels("", "", "", "", "");
        ResolutionOptions = ConfigResolutionOptions.DefaultOptions();
    }

    /// <summary>Sets the delimiter used for paths in the resulting snapshot.</summary>
    /// <param name="delimiter">The delimiter text; an empty string selects <c>.</c>.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder SetDelimiter(string delimiter)
    {
        Delimiter = ConfigPath.CleanDelimiter(delimiter);
        return this;
    }

    /// <summary>Sets object-key comparison behavior for subsequently built snapshots.</summary>
    /// <param name="mode">The requested case mode.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder SetCaseMode(ConfigCaseMode mode)
    {
        CaseMode = mode;
        Aliases = new ConfigAliasTable(mode);
        return this;
    }

    /// <summary>Sets the main strategy for combining successive sources.</summary>
    /// <param name="mode">The merge strategy.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder SetMergeMode(ConfigMergeMode mode)
    {
        MergeMode = mode;
        return this;
    }

    /// <summary>Sets whether later arrays replace or append to existing arrays.</summary>
    /// <param name="mode">The array merge strategy.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder SetArrayMergeMode(ConfigArrayMergeMode mode)
    {
        ArrayMergeMode = mode;
        return this;
    }

    /// <summary>Sets whether a later explicit null replaces or leaves an existing value.</summary>
    /// <param name="mode">The null merge strategy.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder SetNullMergeMode(ConfigNullMergeMode mode)
    {
        NullMergeMode = mode;
        return this;
    }

    /// <summary>Sets how every registered text parser handles repeated keys.</summary>
    /// <param name="mode">The duplicate-key policy.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder SetDuplicateKeyMode(ConfigDuplicateKeyMode mode)
    {
        DuplicateKeyMode = mode;
        return this;
    }

    /// <summary>Sets whether dotenv sources expand references to other keys in the same source.</summary>
    /// <param name="mode">The dotenv expansion policy.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder SetDotEnvExpansionMode(ConfigDotEnvExpansionMode mode)
    {
        DotEnvExpansionMode = mode;
        return this;
    }

    /// <summary>Sets how enabled dotenv expansion handles references that are not present.</summary>
    /// <param name="mode">The missing-reference policy.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder SetDotEnvMissingValueMode(ConfigDotEnvMissingValueMode mode)
    {
        DotEnvMissingValueMode = mode;
        return this;
    }

    /// <summary>Sets optional labels for conventional source groups in merge traces.</summary>
    /// <param name="labels">The labels to copy; empty fields omit the corresponding group label.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the labels are null.</exception>
    public ConfigBuilder SetSourceGroupLabels(ConfigSourceGroupLabels labels)
    {
        if (labels == null)
        {
            throw new ArgumentNullException("labels");
        }

        SourceGroupLabels = labels.Copy();
        return this;
    }

    /// <summary>Sets substitution lookup, missing-reference, and local-include policies.</summary>
    /// <param name="options">The resolution options to validate and copy.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the options are null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either depth limit is negative.</exception>
    public ConfigBuilder SetResolutionOptions(ConfigResolutionOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (options.SubstitutionDepthLimit < 0)
        {
            throw new ArgumentOutOfRangeException("options");
        }

        if (options.IncludeDepthLimit < 0)
        {
            throw new ArgumentOutOfRangeException("options");
        }

        ResolutionOptions = options.Copy();
        return this;
    }

    /// <summary>Adds an application-defined configuration source at the next precedence level.</summary>
    /// <param name="source">The source to load for each build or reload.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the source is null.</exception>
    public ConfigBuilder AddSource(IConfigSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        Sources.Add(source);
        return this;
    }

    /// <summary>Adds an in-memory object source at the next precedence level.</summary>
    /// <param name="values">The values to copy when the source is loaded.</param>
    /// <param name="sourceName">The name shown in metadata and diagnostics.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddMemory(ConfigMap values, string sourceName)
    {
        Sources.Add(ConfigSource.Memory(values, sourceName));
        return this;
    }

    /// <summary>Adds in-memory default values at the next precedence level.</summary>
    /// <param name="values">The default values to copy.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddDefaults(ConfigMap values)
    {
        Sources.Add(ConfigSource.Defaults(values));
        return this;
    }

    /// <summary>Adds one programmatic string value at the next precedence level.</summary>
    /// <param name="path">The configuration path to set.</param>
    /// <param name="value">The text value to store.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddOverride(string path, string value)
    {
        Sources.Add(ConfigSource.ProgrammaticOverride(path, value, "override"));
        return this;
    }

    /// <summary>Adds a file parsed with an explicit format using UTF-8 text.</summary>
    /// <param name="path">The file to load.</param>
    /// <param name="format">The parser to use without consulting the file extension.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <param name="reload">Whether a reload controller watches the file.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddFile(string path, ConfigFormat format, ConfigOptional optional, ConfigReload reload)
    {
        Sources.Add(ConfigSource.File(path, format, optional, reload, path));
        return this;
    }

    /// <summary>Adds a file parsed with an explicit format using a selected text encoding.</summary>
    /// <param name="path">The file to load.</param>
    /// <param name="format">The parser to use without consulting the file extension.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <param name="reload">Whether a reload controller watches the file.</param>
    /// <param name="encoding">The text encoding to use when no byte-order mark overrides it.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the encoding is null.</exception>
    public ConfigBuilder AddFileWithEncoding(string path, ConfigFormat format, ConfigOptional optional, ConfigReload reload, Encoding encoding)
    {
        ConfigSource source = ConfigSource.File(path, format, optional, reload, path);
        source.TextEncoding = RequireEncoding(encoding);
        Sources.Add(source);
        return this;
    }

    /// <summary>Adds a UTF-8 file whose parser is selected from its extension.</summary>
    /// <param name="path">The file to load.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <param name="reload">Whether a reload controller watches the file.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddFileByExtension(string path, ConfigOptional optional, ConfigReload reload)
    {
        Sources.Add(ConfigSource.FileByExtension(path, optional, reload, path));
        return this;
    }

    /// <summary>Adds an encoded file whose parser is selected from its extension.</summary>
    /// <param name="path">The file to load.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <param name="reload">Whether a reload controller watches the file.</param>
    /// <param name="encoding">The text encoding to use when no byte-order mark overrides it.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the encoding is null.</exception>
    public ConfigBuilder AddFileByExtensionWithEncoding(string path, ConfigOptional optional, ConfigReload reload, Encoding encoding)
    {
        ConfigSource source = ConfigSource.FileByExtension(path, optional, reload, path);
        source.TextEncoding = RequireEncoding(encoding);
        Sources.Add(source);
        return this;
    }

    /// <summary>Adds a JSON file source.</summary>
    /// <param name="path">The file to load.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <param name="reload">Whether a reload controller watches the file.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddJsonFile(string path, ConfigOptional optional, ConfigReload reload)
    {
        Sources.Add(ConfigSource.File(path, ConfigFormat.Json, optional, reload, path));
        return this;
    }

    /// <summary>Adds an INI file source.</summary>
    /// <param name="path">The file to load.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <param name="reload">Whether a reload controller watches the file.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddIniFile(string path, ConfigOptional optional, ConfigReload reload)
    {
        Sources.Add(ConfigSource.File(path, ConfigFormat.Ini, optional, reload, path));
        return this;
    }

    /// <summary>Adds a Java properties file source.</summary>
    /// <param name="path">The file to load.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <param name="reload">Whether a reload controller watches the file.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddPropertiesFile(string path, ConfigOptional optional, ConfigReload reload)
    {
        Sources.Add(ConfigSource.File(path, ConfigFormat.Properties, optional, reload, path));
        return this;
    }

    /// <summary>Adds a dotenv file source with variable expansion.</summary>
    /// <param name="path">The file to load.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddDotEnvFile(string path, ConfigOptional optional)
    {
        Sources.Add(ConfigSource.File(path, ConfigFormat.DotEnv, optional, ConfigReload.None, path));
        return this;
    }

    /// <summary>Adds an XML file source.</summary>
    /// <param name="path">The file to load.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <param name="reload">Whether a reload controller watches the file.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddXmlFile(string path, ConfigOptional optional, ConfigReload reload)
    {
        Sources.Add(ConfigSource.File(path, ConfigFormat.Xml, optional, reload, path));
        return this;
    }

    /// <summary>Adds a UTF-8 flat key-value file source.</summary>
    /// <param name="path">The file to load.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <param name="reload">Whether a reload controller watches the file.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddFlatFile(string path, ConfigOptional optional, ConfigReload reload)
    {
        Sources.Add(ConfigSource.File(path, ConfigFormat.Flat, optional, reload, path));
        return this;
    }

    /// <summary>Adds a named UTF-8 file from the current working directory, detecting its format by extension.</summary>
    /// <param name="fileName">The file name relative to the current working directory.</param>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddCurrentDirectoryFile(string fileName, ConfigOptional optional)
    {
        Sources.Add(ConfigSource.SearchFile("", fileName, 0, "", optional, fileName));
        return this;
    }

    /// <summary>Adds the nearest named UTF-8 file found from the current working directory through a bounded number of parents.</summary>
    /// <param name="fileName">The file name to find.</param>
    /// <param name="maximumParentLevels">The greatest number of parent directories to inspect after the current directory.</param>
    /// <param name="optional">Whether no matching file is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the maximum parent level is negative.</exception>
    public ConfigBuilder AddParentDirectoryFile(string fileName, int maximumParentLevels, ConfigOptional optional)
    {
        Sources.Add(ConfigSource.SearchFile("", fileName, RequireParentSearchLimit(maximumParentLevels), "", optional, fileName));
        return this;
    }

    /// <summary>Adds the nearest named UTF-8 file found from a selected directory through a bounded number of parents.</summary>
    /// <param name="startDirectory">The first directory to inspect.</param>
    /// <param name="fileName">The file name to find.</param>
    /// <param name="maximumParentLevels">The greatest number of parent directories to inspect after the start directory.</param>
    /// <param name="optional">Whether no matching file is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the maximum parent level is negative.</exception>
    public ConfigBuilder AddParentDirectoryFileFromDirectory(string startDirectory, string fileName, int maximumParentLevels, ConfigOptional optional)
    {
        Sources.Add(ConfigSource.SearchFile(startDirectory, fileName, RequireParentSearchLimit(maximumParentLevels), "", optional, fileName));
        return this;
    }

    /// <summary>Adds a UTF-8 <c>.env</c> file from the current working directory.</summary>
    /// <param name="optional">Whether a missing file is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddCurrentDirectoryDotEnv(ConfigOptional optional)
    {
        Sources.Add(ConfigSource.SearchFile("", ".env", 0, "", optional, ".env"));
        return this;
    }

    /// <summary>Adds the nearest UTF-8 <c>.env</c> found from the current working directory through a bounded number of parents.</summary>
    /// <param name="maximumParentLevels">The greatest number of parent directories to inspect after the current directory.</param>
    /// <param name="optional">Whether no matching file is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the maximum parent level is negative.</exception>
    public ConfigBuilder AddParentDirectoryDotEnv(int maximumParentLevels, ConfigOptional optional)
    {
        Sources.Add(ConfigSource.SearchFile("", ".env", RequireParentSearchLimit(maximumParentLevels), "", optional, ".env"));
        return this;
    }

    /// <summary>Adds the nearest UTF-8 <c>.env</c> found from a selected directory through a bounded number of parents.</summary>
    /// <param name="startDirectory">The first directory to inspect.</param>
    /// <param name="maximumParentLevels">The greatest number of parent directories to inspect after the start directory.</param>
    /// <param name="optional">Whether no matching file is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the maximum parent level is negative.</exception>
    public ConfigBuilder AddParentDirectoryDotEnvFromDirectory(string startDirectory, int maximumParentLevels, ConfigOptional optional)
    {
        Sources.Add(ConfigSource.SearchFile(startDirectory, ".env", RequireParentSearchLimit(maximumParentLevels), "", optional, ".env"));
        return this;
    }

    /// <summary>Adds the nearest base file and its optional environment-specific sibling from the current working directory or bounded parents.</summary>
    /// <param name="baseFileName">The base file name used for the bounded search.</param>
    /// <param name="environmentName">The suffix inserted before the base file extension, or an empty string to load only the base file.</param>
    /// <param name="maximumParentLevels">The greatest number of parent directories to inspect after the current directory.</param>
    /// <param name="optional">Whether no base file is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the maximum parent level is negative.</exception>
    public ConfigBuilder AddEnvironmentFileLayers(string baseFileName, string environmentName, int maximumParentLevels, ConfigOptional optional)
    {
        Sources.Add(ConfigSource.SearchFile("", baseFileName, RequireParentSearchLimit(maximumParentLevels), environmentName, optional, baseFileName));
        return this;
    }

    /// <summary>Adds the nearest base file and its optional environment-specific sibling from a selected directory or bounded parents.</summary>
    /// <param name="startDirectory">The first directory to inspect.</param>
    /// <param name="baseFileName">The base file name used for the bounded search.</param>
    /// <param name="environmentName">The suffix inserted before the base file extension, or an empty string to load only the base file.</param>
    /// <param name="maximumParentLevels">The greatest number of parent directories to inspect after the start directory.</param>
    /// <param name="optional">Whether no base file is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the maximum parent level is negative.</exception>
    public ConfigBuilder AddEnvironmentFileLayersFromDirectory(string startDirectory, string baseFileName, string environmentName, int maximumParentLevels, ConfigOptional optional)
    {
        Sources.Add(ConfigSource.SearchFile(startDirectory, baseFileName, RequireParentSearchLimit(maximumParentLevels), environmentName, optional, baseFileName));
        return this;
    }

    /// <summary>Adds all matching files in a directory in stable name order, detecting each format by extension.</summary>
    /// <param name="path">The directory to scan.</param>
    /// <param name="pattern">The file search pattern.</param>
    /// <param name="optional">Whether a missing directory is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddDirectory(string path, string pattern, ConfigOptional optional)
    {
        Sources.Add(ConfigSource.DirectorySource(path, pattern, optional, "directory"));
        return this;
    }

    /// <summary>Adds matching encoded files in stable name order, detecting each format by extension.</summary>
    /// <param name="path">The directory to scan.</param>
    /// <param name="pattern">The file search pattern.</param>
    /// <param name="optional">Whether a missing directory is accepted.</param>
    /// <param name="encoding">The text encoding shared by every loaded file and include.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the encoding is null.</exception>
    public ConfigBuilder AddDirectoryWithEncoding(string path, string pattern, ConfigOptional optional, Encoding encoding)
    {
        ConfigSource source = ConfigSource.DirectorySource(path, pattern, optional, "directory");
        source.TextEncoding = RequireEncoding(encoding);
        Sources.Add(source);
        return this;
    }

    /// <summary>Adds a directory in which each file name is a key and its text is the value.</summary>
    /// <param name="path">The directory to scan.</param>
    /// <param name="optional">Whether a missing directory is accepted.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddKeyPerFileDirectory(string path, ConfigOptional optional)
    {
        Sources.Add(ConfigSource.KeyPerFileDirectory(path, optional, "key-per-file"));
        return this;
    }

    /// <summary>Adds a key-per-file directory using a selected text encoding.</summary>
    /// <param name="path">The directory to scan.</param>
    /// <param name="optional">Whether a missing directory is accepted.</param>
    /// <param name="encoding">The text encoding used for every value file.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the encoding is null.</exception>
    public ConfigBuilder AddKeyPerFileDirectoryWithEncoding(string path, ConfigOptional optional, Encoding encoding)
    {
        ConfigSource source = ConfigSource.KeyPerFileDirectory(path, optional, "key-per-file");
        source.TextEncoding = RequireEncoding(encoding);
        Sources.Add(source);
        return this;
    }

    /// <summary>Adds configuration text parsed in an explicit format.</summary>
    /// <param name="text">The complete source text.</param>
    /// <param name="format">The parser to use.</param>
    /// <param name="sourceName">The name shown in metadata and diagnostics.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddString(string text, ConfigFormat format, string sourceName)
    {
        Sources.Add(ConfigSource.StringSource(text, format, sourceName));
        return this;
    }

    /// <summary>Adds configuration read from a stream in an explicit format.</summary>
    /// <param name="stream">The readable stream. Loading consumes it but does not dispose it.</param>
    /// <param name="format">The parser to use.</param>
    /// <param name="sourceName">The name shown in metadata and diagnostics.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddStream(Stream stream, ConfigFormat format, string sourceName)
    {
        Sources.Add(ConfigSource.StreamSource(stream, format, sourceName));
        return this;
    }

    /// <summary>Adds configuration read from a stream using a selected text encoding.</summary>
    /// <param name="stream">The readable stream. Loading consumes it but does not dispose it.</param>
    /// <param name="format">The parser to use.</param>
    /// <param name="sourceName">The name shown in metadata and diagnostics.</param>
    /// <param name="encoding">The text encoding to use when no byte-order mark overrides it.</param>
    /// <returns>This builder for fluent calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the encoding is null.</exception>
    public ConfigBuilder AddStreamWithEncoding(Stream stream, ConfigFormat format, string sourceName, Encoding encoding)
    {
        ConfigSource source = ConfigSource.StreamSource(stream, format, sourceName);
        source.TextEncoding = RequireEncoding(encoding);
        Sources.Add(source);
        return this;
    }

    /// <summary>Adds all process environment variables, mapping double underscores to path delimiters.</summary>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddEnvironment()
    {
        Sources.Add(ConfigSource.Environment("", "__", "environment"));
        return this;
    }

    /// <summary>Adds process environment variables that begin with a prefix.</summary>
    /// <param name="prefix">The required prefix, which is removed from loaded keys.</param>
    /// <param name="delimiter">The environment key text replaced by the configured path delimiter.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddEnvironmentWithPrefix(string prefix, string delimiter)
    {
        Sources.Add(ConfigSource.Environment(prefix, delimiter, "environment"));
        return this;
    }

    /// <summary>Adds process environment variables with empty-value and explicit path-mapping controls.</summary>
    /// <param name="prefix">The prefix required for variables without explicit mappings.</param>
    /// <param name="delimiter">The environment key text replaced by the configured path delimiter.</param>
    /// <param name="emptyValueMode">Whether variables with empty values are retained.</param>
    /// <param name="pathMap">Direct environment names mapped to destination configuration paths.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddEnvironmentWithOptions(string prefix, string delimiter, ConfigEmptyEnvMode emptyValueMode, ConfigMap pathMap)
    {
        ConfigSource source = ConfigSource.EnvironmentWithMappings(prefix, delimiter, pathMap, "environment");
        source.EmptyEnvMode = emptyValueMode;
        Sources.Add(source);
        return this;
    }

    /// <summary>Adds environment-style values from a configuration map for deterministic input.</summary>
    /// <param name="envMap">Direct environment names and scalar values.</param>
    /// <param name="prefix">The required prefix, which is removed from loaded keys.</param>
    /// <param name="delimiter">The key text replaced by the configured path delimiter.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddEnvironmentMap(ConfigMap envMap, string prefix, string delimiter)
    {
        Sources.Add(ConfigSource.EnvironmentMap(EnvironmentDictionary(envMap), prefix, delimiter, "environment"));
        return this;
    }

    /// <summary>Adds environment-style map values with empty-value and explicit path-mapping controls.</summary>
    /// <param name="envMap">Direct environment names and scalar values.</param>
    /// <param name="prefix">The prefix required for variables without explicit mappings.</param>
    /// <param name="delimiter">The environment key text replaced by the configured path delimiter.</param>
    /// <param name="emptyValueMode">Whether variables with empty values are retained.</param>
    /// <param name="pathMap">Direct environment names mapped to destination configuration paths.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddEnvironmentMapWithOptions(ConfigMap envMap, string prefix, string delimiter, ConfigEmptyEnvMode emptyValueMode, ConfigMap pathMap)
    {
        ConfigSource source = ConfigSource.EnvironmentMapWithMappings(EnvironmentDictionary(envMap), prefix, delimiter, pathMap, "environment");
        source.EmptyEnvMode = emptyValueMode;
        Sources.Add(source);
        return this;
    }

    /// <summary>Adds environment-style values from a string dictionary for deterministic input.</summary>
    /// <param name="envMap">Environment names and values.</param>
    /// <param name="prefix">The required prefix, which is removed from loaded keys.</param>
    /// <param name="delimiter">The key text replaced by the configured path delimiter.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddEnvironmentDictionary(Dictionary<string, string> envMap, string prefix, string delimiter)
    {
        Sources.Add(ConfigSource.EnvironmentMap(envMap, prefix, delimiter, "environment"));
        return this;
    }

    /// <summary>Adds environment-style dictionary values with empty-value and explicit path-mapping controls.</summary>
    /// <param name="envMap">Environment names and values.</param>
    /// <param name="prefix">The prefix required for variables without explicit mappings.</param>
    /// <param name="delimiter">The environment key text replaced by the configured path delimiter.</param>
    /// <param name="emptyValueMode">Whether variables with empty values are retained.</param>
    /// <param name="pathMap">Direct environment names mapped to destination configuration paths.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddEnvironmentDictionaryWithOptions(Dictionary<string, string> envMap, string prefix, string delimiter, ConfigEmptyEnvMode emptyValueMode, ConfigMap pathMap)
    {
        ConfigSource source = ConfigSource.EnvironmentMapWithMappings(envMap, prefix, delimiter, pathMap, "environment");
        source.EmptyEnvMode = emptyValueMode;
        Sources.Add(source);
        return this;
    }

    /// <summary>Adds command-line values using long, short, or slash switch forms.</summary>
    /// <param name="args">The argument tokens to parse.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddCommandLine(string[] args)
    {
        Sources.Add(ConfigSource.CommandLine(args, new ConfigMap(), "command-line"));
        return this;
    }

    /// <summary>Adds command-line values and maps selected switch names to configuration paths.</summary>
    /// <param name="args">The argument tokens to parse.</param>
    /// <param name="switchMap">Direct switch names mapped to destination paths.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddMappedCommandLine(string[] args, ConfigMap switchMap)
    {
        Sources.Add(ConfigSource.CommandLine(args, switchMap, "command-line"));
        return this;
    }

    /// <summary>Adds a read alias from an old path to a current path.</summary>
    /// <param name="oldPath">The path accepted from callers.</param>
    /// <param name="newPath">The path actually read from the snapshot.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddAlias(string oldPath, string newPath)
    {
        Aliases.Add(oldPath, newPath, Delimiter);
        return this;
    }

    /// <summary>Adds the conventional optional files, process environment, and command line from the current directory.</summary>
    /// <param name="environmentName">The environment suffix used in <c>appsettings.Environment.json</c>, or an empty string to omit that file.</param>
    /// <param name="args">The command-line tokens applied last.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddDefaultAppSources(string environmentName, string[] args)
    {
        return AddDefaultAppSourcesFromDirectory(Directory.GetCurrentDirectory(), environmentName, args);
    }

    /// <summary>Adds optional appsettings files and dotenv from a directory, followed by process environment and command line.</summary>
    /// <param name="directory">The directory containing appsettings files and <c>.env</c>.</param>
    /// <param name="environmentName">The environment suffix, or an empty string to omit the environment-specific file.</param>
    /// <param name="args">The command-line tokens applied last.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddDefaultAppSourcesFromDirectory(string directory, string environmentName, string[] args)
    {
        AddJsonFile(Path.Combine(directory, "appsettings.json"), ConfigOptional.Optional, ConfigReload.None);
        if (environmentName.Length > 0)
        {
            AddJsonFile(Path.Combine(directory, AppSettingsEnvironmentFileName(environmentName)), ConfigOptional.Optional, ConfigReload.None);
        }

        AddDotEnvFile(Path.Combine(directory, ".env"), ConfigOptional.Optional);
        AddEnvironment();
        AddCommandLine(args);
        return this;
    }

    /// <summary>Adds in-memory defaults before conventional app sources from the current directory.</summary>
    /// <param name="defaults">The lowest-precedence values.</param>
    /// <param name="environmentName">The environment suffix, or an empty string to omit that file.</param>
    /// <param name="args">The command-line tokens applied last.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddDefaultAppSourcesWithDefaults(ConfigMap defaults, string environmentName, string[] args)
    {
        return AddDefaultAppSourcesWithDefaultsFromDirectory(defaults, Directory.GetCurrentDirectory(), environmentName, args);
    }

    /// <summary>Adds in-memory defaults before conventional app sources from a selected directory.</summary>
    /// <param name="defaults">The lowest-precedence values.</param>
    /// <param name="directory">The directory containing appsettings files and <c>.env</c>.</param>
    /// <param name="environmentName">The environment suffix, or an empty string to omit that file.</param>
    /// <param name="args">The command-line tokens applied last.</param>
    /// <returns>This builder for fluent calls.</returns>
    public ConfigBuilder AddDefaultAppSourcesWithDefaultsFromDirectory(ConfigMap defaults, string directory, string environmentName, string[] args)
    {
        AddDefaults(defaults);
        AddDefaultAppSourcesFromDirectory(directory, environmentName, args);
        return this;
    }

    internal static string AppSettingsEnvironmentFileName(string environmentName)
    {
        return "appsettings." + environmentName + ".json";
    }

    internal static Encoding RequireEncoding(Encoding encoding)
    {
        if (encoding == null)
        {
            throw new ArgumentNullException("encoding");
        }

        return encoding;
    }

    internal static int RequireParentSearchLimit(int maximumParentLevels)
    {
        if (maximumParentLevels < 0)
        {
            throw new ArgumentOutOfRangeException("maximumParentLevels");
        }

        return maximumParentLevels;
    }

    /// <summary>Loads every registered source, merges it, and creates a snapshot.</summary>
    /// <remarks>Load and merge problems are retained in the snapshot diagnostics instead of being thrown as configuration errors.</remarks>
    /// <returns>A new immutable configuration snapshot.</returns>
    public Config Build()
    {
        ConfigSourceResult[] results = LoadSources();
        ConfigMergeResult merge = ConfigMerger.MergeWithOptions(results, MergeMode, ArrayMergeMode, NullMergeMode);
        Config config = new Config(merge.Root, merge.Diagnostics, Aliases, Delimiter, CaseMode, ResolutionOptions);
        config.AddResolutionDiagnostics();
        return config;
    }

    /// <summary>Loads and merges every source, throwing the first classified configuration failure.</summary>
    /// <returns>A new immutable configuration snapshot when no errors are recorded.</returns>
    /// <exception cref="ConfigLoadException">Thrown when a source cannot be loaded or resolution fails.</exception>
    /// <exception cref="ConfigParseException">Thrown when source text cannot be parsed.</exception>
    /// <exception cref="ConfigMergeException">Thrown when configured merge rules reject source values.</exception>
    public Config BuildOrThrow()
    {
        Config config = Build();
        ConfigExceptionFactory.ThrowBuildFailure(config.Diagnostics());
        return config;
    }

    /// <summary>Builds an initial snapshot and a controller that can rebuild it from the registered source set.</summary>
    /// <remarks>Built-in source definitions are copied. Application-defined sources remain attached by interface reference and are invoked again on reload.</remarks>
    /// <returns>A reload controller. Call <see cref="ConfigReloadController.Start"/> to enable file watching.</returns>
    public ConfigReloadController BuildReloading()
    {
        return new ConfigReloadController(this);
    }

    /// <summary>Loads and merges all sources but returns only the resulting diagnostics.</summary>
    /// <returns>A copy of all load, parse, merge, and trace messages.</returns>
    public ConfigDiagnostics BuildDiagnostics()
    {
        ConfigSourceResult[] results = LoadSources();
        ConfigMergeResult merge = ConfigMerger.MergeWithOptions(results, MergeMode, ArrayMergeMode, NullMergeMode);
        Config config = new Config(merge.Root, merge.Diagnostics, Aliases, Delimiter, CaseMode, ResolutionOptions);
        config.AddResolutionDiagnostics();
        return config.Diagnostics();
    }

    internal ConfigSourceResult[] LoadSources()
    {
        ConfigParserOptions options = new ConfigParserOptions(DuplicateKeyMode, DotEnvExpansionMode, DotEnvMissingValueMode, ResolutionOptions.MissingIncludes, ResolutionOptions.IncludeDepthLimit);
        ConfigSourceResult[] results = ConfigSourceLoader.LoadAllWithParserOptions(Sources, options);
        ApplySourceGroupLabels(results, Sources, SourceGroupLabels);
        return results;
    }

    internal static void ApplySourceGroupLabels(ConfigSourceResult[] results, List<IConfigSource> sources, ConfigSourceGroupLabels labels)
    {
        int index = 0;
        while (index < results.Length && index < sources.Count)
        {
            ConfigSource? source = sources[index] as ConfigSource;
            if (source != null)
            {
                results[index].SourceGroup = SourceGroupForKind(source.Kind, labels);
            }

            index = index + 1;
        }
    }

    internal static string SourceGroupForKind(ConfigSourceKind kind, ConfigSourceGroupLabels labels)
    {
        if (kind == ConfigSourceKind.Defaults)
        {
            return labels.Defaults;
        }

        if (kind == ConfigSourceKind.File || kind == ConfigSourceKind.Directory || kind == ConfigSourceKind.KeyPerFile || kind == ConfigSourceKind.Search)
        {
            return labels.Files;
        }

        if (kind == ConfigSourceKind.Environment)
        {
            return labels.Environment;
        }

        if (kind == ConfigSourceKind.CommandLine)
        {
            return labels.CommandLine;
        }

        if (kind == ConfigSourceKind.ProgrammaticOverride)
        {
            return labels.Overrides;
        }

        return "";
    }

    internal int SourceCount()
    {
        return Sources.Count;
    }

    internal ConfigSource SourceAt(int index)
    {
        ConfigSource? source = Sources[index] as ConfigSource;
        if (source == null)
        {
            throw new InvalidOperationException("config source is not a built-in source");
        }

        return source;
    }

    internal static Dictionary<string, string> EnvironmentDictionary(ConfigMap envMap)
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        string[] keys = envMap.Keys();

        int index = 0;
        while (index < keys.Length)
        {
            ConfigValue value = envMap.Get(keys[index]);
            if (value.Kind == ConfigValueKind.String && value.StringValue != null)
            {
                values[keys[index]] = value.StringValue;
            }
            else
            {
                values[keys[index]] = value.ToDisplayString();
            }

            index = index + 1;
        }

        return values;
    }
}
