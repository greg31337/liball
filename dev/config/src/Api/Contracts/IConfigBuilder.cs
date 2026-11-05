using System.Collections.Generic;
using System.IO;
using System.Text;

namespace config;

/// <summary>Defines construction of configuration snapshots from ordered sources.</summary>
public interface IConfigBuilder
{
    /// <summary>Sets the hierarchical path delimiter.</summary>
    ConfigBuilder SetDelimiter(string delimiter);
    /// <summary>Sets key case sensitivity.</summary>
    ConfigBuilder SetCaseMode(ConfigCaseMode mode);
    /// <summary>Sets object merge behavior.</summary>
    ConfigBuilder SetMergeMode(ConfigMergeMode mode);
    /// <summary>Sets array merge behavior.</summary>
    ConfigBuilder SetArrayMergeMode(ConfigArrayMergeMode mode);
    /// <summary>Sets null merge behavior.</summary>
    ConfigBuilder SetNullMergeMode(ConfigNullMergeMode mode);
    /// <summary>Sets duplicate-key behavior for text parsers.</summary>
    ConfigBuilder SetDuplicateKeyMode(ConfigDuplicateKeyMode mode);
    /// <summary>Sets whether dotenv references are expanded.</summary>
    ConfigBuilder SetDotEnvExpansionMode(ConfigDotEnvExpansionMode mode);
    /// <summary>Sets how enabled dotenv expansion handles missing references.</summary>
    ConfigBuilder SetDotEnvMissingValueMode(ConfigDotEnvMissingValueMode mode);
    /// <summary>Sets optional labels for conventional source groups in merge traces.</summary>
    ConfigBuilder SetSourceGroupLabels(ConfigSourceGroupLabels labels);
    /// <summary>Sets substitution and local-include resolution policies.</summary>
    ConfigBuilder SetResolutionOptions(ConfigResolutionOptions options);
    /// <summary>Adds an application-defined source.</summary>
    ConfigBuilder AddSource(IConfigSource source);
    /// <summary>Adds an in-memory map.</summary>
    ConfigBuilder AddMemory(ConfigMap values, string sourceName);
    /// <summary>Adds default values.</summary>
    ConfigBuilder AddDefaults(ConfigMap values);
    /// <summary>Adds one programmatic override.</summary>
    ConfigBuilder AddOverride(string path, string value);
    /// <summary>Adds a file parsed with an explicit format and UTF-8 text.</summary>
    ConfigBuilder AddFile(string path, ConfigFormat format, ConfigOptional optional, ConfigReload reload);
    /// <summary>Adds a file parsed with an explicit format and encoding.</summary>
    ConfigBuilder AddFileWithEncoding(string path, ConfigFormat format, ConfigOptional optional, ConfigReload reload, Encoding encoding);
    /// <summary>Adds a UTF-8 file whose format is detected from its extension.</summary>
    ConfigBuilder AddFileByExtension(string path, ConfigOptional optional, ConfigReload reload);
    /// <summary>Adds an encoded file whose format is detected from its extension.</summary>
    ConfigBuilder AddFileByExtensionWithEncoding(string path, ConfigOptional optional, ConfigReload reload, Encoding encoding);
    /// <summary>Adds a JSON file.</summary>
    ConfigBuilder AddJsonFile(string path, ConfigOptional optional, ConfigReload reload);
    /// <summary>Adds an INI file.</summary>
    ConfigBuilder AddIniFile(string path, ConfigOptional optional, ConfigReload reload);
    /// <summary>Adds a properties file.</summary>
    ConfigBuilder AddPropertiesFile(string path, ConfigOptional optional, ConfigReload reload);
    /// <summary>Adds a dotenv file.</summary>
    ConfigBuilder AddDotEnvFile(string path, ConfigOptional optional);
    /// <summary>Adds an XML file.</summary>
    ConfigBuilder AddXmlFile(string path, ConfigOptional optional, ConfigReload reload);
    /// <summary>Adds a flat key-value file.</summary>
    ConfigBuilder AddFlatFile(string path, ConfigOptional optional, ConfigReload reload);
    /// <summary>Adds a named file from the current working directory.</summary>
    ConfigBuilder AddCurrentDirectoryFile(string fileName, ConfigOptional optional);
    /// <summary>Adds the nearest named file from the current working directory or bounded parents.</summary>
    ConfigBuilder AddParentDirectoryFile(string fileName, int maximumParentLevels, ConfigOptional optional);
    /// <summary>Adds the nearest named file from a selected directory or bounded parents.</summary>
    ConfigBuilder AddParentDirectoryFileFromDirectory(string startDirectory, string fileName, int maximumParentLevels, ConfigOptional optional);
    /// <summary>Adds <c>.env</c> from the current working directory.</summary>
    ConfigBuilder AddCurrentDirectoryDotEnv(ConfigOptional optional);
    /// <summary>Adds the nearest <c>.env</c> from the current working directory or bounded parents.</summary>
    ConfigBuilder AddParentDirectoryDotEnv(int maximumParentLevels, ConfigOptional optional);
    /// <summary>Adds the nearest <c>.env</c> from a selected directory or bounded parents.</summary>
    ConfigBuilder AddParentDirectoryDotEnvFromDirectory(string startDirectory, int maximumParentLevels, ConfigOptional optional);
    /// <summary>Adds nearest base and environment-specific file layers from the current working directory or bounded parents.</summary>
    ConfigBuilder AddEnvironmentFileLayers(string baseFileName, string environmentName, int maximumParentLevels, ConfigOptional optional);
    /// <summary>Adds nearest base and environment-specific file layers from a selected directory or bounded parents.</summary>
    ConfigBuilder AddEnvironmentFileLayersFromDirectory(string startDirectory, string baseFileName, string environmentName, int maximumParentLevels, ConfigOptional optional);
    /// <summary>Adds all matching files in a directory.</summary>
    ConfigBuilder AddDirectory(string path, string pattern, ConfigOptional optional);
    /// <summary>Adds encoded matching files from a directory.</summary>
    ConfigBuilder AddDirectoryWithEncoding(string path, string pattern, ConfigOptional optional, Encoding encoding);
    /// <summary>Adds a key-per-file directory.</summary>
    ConfigBuilder AddKeyPerFileDirectory(string path, ConfigOptional optional);
    /// <summary>Adds an encoded key-per-file directory.</summary>
    ConfigBuilder AddKeyPerFileDirectoryWithEncoding(string path, ConfigOptional optional, Encoding encoding);
    /// <summary>Adds formatted configuration text.</summary>
    ConfigBuilder AddString(string text, ConfigFormat format, string sourceName);
    /// <summary>Adds a formatted configuration stream.</summary>
    ConfigBuilder AddStream(Stream stream, ConfigFormat format, string sourceName);
    /// <summary>Adds an encoded formatted configuration stream.</summary>
    ConfigBuilder AddStreamWithEncoding(Stream stream, ConfigFormat format, string sourceName, Encoding encoding);
    /// <summary>Adds the process environment.</summary>
    ConfigBuilder AddEnvironment();
    /// <summary>Adds prefixed process environment variables.</summary>
    ConfigBuilder AddEnvironmentWithPrefix(string prefix, string delimiter);
    /// <summary>Adds process environment variables with empty-value and path-mapping controls.</summary>
    ConfigBuilder AddEnvironmentWithOptions(string prefix, string delimiter, ConfigEmptyEnvMode emptyValueMode, ConfigMap pathMap);
    /// <summary>Adds environment-style values from a map.</summary>
    ConfigBuilder AddEnvironmentMap(ConfigMap envMap, string prefix, string delimiter);
    /// <summary>Adds map-backed environment values with empty-value and path-mapping controls.</summary>
    ConfigBuilder AddEnvironmentMapWithOptions(ConfigMap envMap, string prefix, string delimiter, ConfigEmptyEnvMode emptyValueMode, ConfigMap pathMap);
    /// <summary>Adds environment-style values from a dictionary.</summary>
    ConfigBuilder AddEnvironmentDictionary(Dictionary<string, string> envMap, string prefix, string delimiter);
    /// <summary>Adds dictionary-backed environment values with empty-value and path-mapping controls.</summary>
    ConfigBuilder AddEnvironmentDictionaryWithOptions(Dictionary<string, string> envMap, string prefix, string delimiter, ConfigEmptyEnvMode emptyValueMode, ConfigMap pathMap);
    /// <summary>Adds command-line arguments.</summary>
    ConfigBuilder AddCommandLine(string[] args);
    /// <summary>Adds command-line arguments with switch mappings.</summary>
    ConfigBuilder AddMappedCommandLine(string[] args, ConfigMap switchMap);
    /// <summary>Adds a legacy-path alias.</summary>
    ConfigBuilder AddAlias(string oldPath, string newPath);
    /// <summary>Adds the conventional application source stack.</summary>
    ConfigBuilder AddDefaultAppSources(string environmentName, string[] args);
    /// <summary>Adds the conventional application source stack from a directory.</summary>
    ConfigBuilder AddDefaultAppSourcesFromDirectory(string directory, string environmentName, string[] args);
    /// <summary>Adds defaults and the conventional application source stack.</summary>
    ConfigBuilder AddDefaultAppSourcesWithDefaults(ConfigMap defaults, string environmentName, string[] args);
    /// <summary>Adds defaults and the conventional application source stack from a directory.</summary>
    ConfigBuilder AddDefaultAppSourcesWithDefaultsFromDirectory(ConfigMap defaults, string directory, string environmentName, string[] args);
    /// <summary>Builds an immutable snapshot.</summary>
    Config Build();
    /// <summary>Builds an immutable snapshot or throws the classified first configuration error.</summary>
    Config BuildOrThrow();
    /// <summary>Builds a reload controller.</summary>
    ConfigReloadController BuildReloading();
    /// <summary>Builds and returns its diagnostics.</summary>
    ConfigDiagnostics BuildDiagnostics();
}
