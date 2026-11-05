using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace config;

internal class ConfigSourceLoader
{
    internal static ConfigSourceResult[] LoadAll(List<IConfigSource> sources)
    {
        return LoadAllWithParserOptions(sources, ConfigParserOptions.Conservative());
    }

    internal static ConfigSourceResult[] LoadAllWithParserOptions(List<IConfigSource> sources, ConfigParserOptions options)
    {
        ConfigSourceResult[] results = new ConfigSourceResult[sources.Count];

        int index = 0;
        while (index < sources.Count)
        {
            results[index] = LoadWithParserOptions(sources[index], index + 1, options);

            index = index + 1;
        }

        return results;
    }

    internal static ConfigSourceResult LoadWithParserOptions(IConfigSource source, int precedenceRank, ConfigParserOptions options)
    {
        ConfigSource? configuredSource = source as ConfigSource;
        if (configuredSource == null)
        {
            return source.Load(precedenceRank);
        }

        ConfigSource configured = configuredSource.Copy();
        configured.DuplicateKeyMode = options.DuplicateKeyMode;
        configured.DotEnvExpansionMode = options.DotEnvExpansionMode;
        configured.DotEnvMissingValueMode = options.DotEnvMissingValueMode;
        configured.MissingIncludes = options.MissingIncludes;
        configured.IncludeDepthLimit = options.IncludeDepthLimit;
        return configured.Load(precedenceRank);
    }

    internal static ConfigSourceResult Load(ConfigSource source, int precedenceRank)
    {
        if (source.Kind == ConfigSourceKind.Memory || source.Kind == ConfigSourceKind.Defaults)
        {
            return LoadMapSource(source, precedenceRank);
        }

        if (source.Kind == ConfigSourceKind.ProgrammaticOverride)
        {
            return LoadProgrammaticOverrideSource(source, precedenceRank);
        }

        if (source.Kind == ConfigSourceKind.File)
        {
            return LoadFileSource(source, precedenceRank);
        }

        if (source.Kind == ConfigSourceKind.String)
        {
            return LoadStringSource(source, precedenceRank);
        }

        if (source.Kind == ConfigSourceKind.Stream)
        {
            return LoadStreamSource(source, precedenceRank);
        }

        if (source.Kind == ConfigSourceKind.Directory)
        {
            return LoadDirectorySource(source, precedenceRank);
        }

        if (source.Kind == ConfigSourceKind.KeyPerFile)
        {
            return LoadKeyPerFileDirectorySource(source, precedenceRank);
        }

        if (source.Kind == ConfigSourceKind.Environment)
        {
            return LoadEnvironmentSource(source, precedenceRank);
        }

        if (source.Kind == ConfigSourceKind.CommandLine)
        {
            return LoadCommandLineSource(source, precedenceRank);
        }

        if (source.Kind == ConfigSourceKind.Search)
        {
            return LoadSearchSource(source, precedenceRank);
        }

        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        ConfigMap map = new ConfigMap();
        ConfigValue root = ConfigValue.Object(map).SetSource(source.SourceName, precedenceRank, "", 0, 0);
        diagnostics.AddLoadError(source.SourceName, "", "unsupported source kind", 0, 0);
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static ConfigSourceResult LoadMapSource(ConfigSource source, int precedenceRank)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        ConfigMap map = new ConfigMap();

        if (source.Values == null)
        {
            diagnostics.AddLoadError(source.SourceName, "", "missing memory source values", 0, 0);
        }
        else
        {
            map = source.Values.Copy();
        }

        ApplySourceMetadataToMap(map, source.SourceName, precedenceRank, "");
        ConfigValue root = ConfigValue.Object(map).SetSource(source.SourceName, precedenceRank, "", 0, 0);
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static ConfigSourceResult LoadProgrammaticOverrideSource(ConfigSource source, int precedenceRank)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        ConfigMap map = new ConfigMap();

        string path = ConfigPath.Normalize(source.OverridePath, ".");
        if (path.Length == 0)
        {
            diagnostics.AddLoadError(source.SourceName, "", "missing override path", 0, 0);
        }
        else
        {
            ConfigValue value = ConfigValue.String(source.OverrideValue).SetSource(source.SourceName, precedenceRank, path, 0, 0);
            map.SetPathDefault(path, value);
        }

        ConfigValue root = ConfigValue.Object(map).SetSource(source.SourceName, precedenceRank, "", 0, 0);
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static ConfigSourceResult LoadFileSource(ConfigSource source, int precedenceRank)
    {
        List<string> includeStack = new List<string>();
        return LoadFileSourceWithIncludeStack(source, precedenceRank, includeStack);
    }

    internal static ConfigSourceResult LoadFileSourceWithIncludeStack(ConfigSource source, int precedenceRank, List<string> includeStack)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        string fullPath = FullPath(source.Path);

        if (ContainsFilePath(includeStack, fullPath))
        {
            ConfigValue cycleRoot = EmptyRoot(source.SourceName, precedenceRank);
            diagnostics.AddLoadError(source.SourceName, source.Path, "include cycle", 0, 0);
            return new ConfigSourceResult(cycleRoot, diagnostics);
        }

        if (includeStack.Count > source.IncludeDepthLimit)
        {
            ConfigValue depthRoot = EmptyRoot(source.SourceName, precedenceRank);
            diagnostics.AddLoadError(source.SourceName, source.Path, "include depth limit exceeded", 0, 0);
            return new ConfigSourceResult(depthRoot, diagnostics);
        }

        if (!File.Exists(source.Path))
        {
            ConfigValue missingRoot = EmptyRoot(source.SourceName, precedenceRank);
            if (source.Optional == ConfigOptional.Required)
            {
                diagnostics.AddLoadError(source.SourceName, source.Path, "missing required file", 0, 0);
            }

            return new ConfigSourceResult(missingRoot, diagnostics);
        }

        ConfigFormat format = source.Format;
        if (source.UseExtensionFormat)
        {
            if (!TryDetectFormat(source.Path, diagnostics, source.SourceName))
            {
                ConfigValue unsupportedRoot = EmptyRoot(source.SourceName, precedenceRank);
                return new ConfigSourceResult(unsupportedRoot, diagnostics);
            }

            format = DetectFormat(source.Path);
        }

        includeStack.Add(fullPath);
        string text = File.ReadAllText(source.Path, source.TextEncoding);
        ConfigParserOptions options = ParserOptions(source);
        ConfigValue root = ParseTextWithOptions(text, format, source.SourceName, precedenceRank, diagnostics, options);
        root = ApplyIncludes(source, root, precedenceRank, diagnostics, DirectoryName(fullPath), includeStack);
        includeStack.RemoveAt(includeStack.Count - 1);
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static ConfigSourceResult LoadStringSource(ConfigSource source, int precedenceRank)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        ConfigParserOptions options = ParserOptions(source);
        ConfigValue root = ParseTextWithOptions(source.Text, source.Format, source.SourceName, precedenceRank, diagnostics, options);
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static ConfigSourceResult LoadStreamSource(ConfigSource source, int precedenceRank)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        if (source.StreamValue == null)
        {
            ConfigValue missingRoot = EmptyRoot(source.SourceName, precedenceRank);
            diagnostics.AddLoadError(source.SourceName, "", "missing stream source", 0, 0);
            return new ConfigSourceResult(missingRoot, diagnostics);
        }

        StreamReader reader = new StreamReader(source.StreamValue, source.TextEncoding, true, 1024, true);
        string text = reader.ReadToEnd();
        reader.Dispose();

        ConfigParserOptions options = ParserOptions(source);
        ConfigValue root = ParseTextWithOptions(text, source.Format, source.SourceName, precedenceRank, diagnostics, options);
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static ConfigSourceResult LoadDirectorySource(ConfigSource source, int precedenceRank)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        ConfigMap map = new ConfigMap();

        if (!Directory.Exists(source.Path))
        {
            ConfigValue missingRoot = EmptyRoot(source.SourceName, precedenceRank);
            if (source.Optional == ConfigOptional.Required)
            {
                diagnostics.AddLoadError(source.SourceName, source.Path, "missing required directory", 0, 0);
            }

            return new ConfigSourceResult(missingRoot, diagnostics);
        }

        string pattern = source.Pattern;
        if (pattern.Length == 0)
        {
            pattern = "*";
        }

        string[] files = Directory.GetFiles(source.Path, pattern);
        Array.Sort(files, StringComparer.Ordinal);

        int index = 0;
        while (index < files.Length)
        {
            ConfigSource fileSource = ConfigSource.FileByExtension(files[index], ConfigOptional.Required, ConfigReload.None, files[index]);
            CopyLoadOptions(source, fileSource);
            ConfigSourceResult fileResult = LoadFileSource(fileSource, precedenceRank);
            AppendDiagnostics(diagnostics, fileResult.Diagnostics);
            MergeObjectRoot(map, fileResult.Root);
            index = index + 1;
        }

        ConfigValue root = ConfigValue.Object(map).SetSource(source.SourceName, precedenceRank, "", 0, 0);
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static ConfigSourceResult LoadKeyPerFileDirectorySource(ConfigSource source, int precedenceRank)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        ConfigMap map = new ConfigMap();

        if (!Directory.Exists(source.Path))
        {
            ConfigValue missingRoot = EmptyRoot(source.SourceName, precedenceRank);
            if (source.Optional == ConfigOptional.Required)
            {
                diagnostics.AddLoadError(source.SourceName, source.Path, "missing required key-per-file directory", 0, 0);
            }

            return new ConfigSourceResult(missingRoot, diagnostics);
        }

        string[] files = Directory.GetFiles(source.Path);
        Array.Sort(files, StringComparer.Ordinal);

        int index = 0;
        while (index < files.Length)
        {
            string key = Path.GetFileName(files[index]);
            string path = ConfigPath.Normalize(key, ".");
            if (path.Length > 0)
            {
                string text = File.ReadAllText(files[index], source.TextEncoding);
                ConfigValue value = ConfigValue.String(text).SetSource(files[index], precedenceRank, path, 0, 0);
                map.SetPathDefault(path, value);
            }

            index = index + 1;
        }

        ConfigValue root = ConfigValue.Object(map).SetSource(source.SourceName, precedenceRank, "", 0, 0);
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static ConfigSourceResult LoadSearchSource(ConfigSource source, int precedenceRank)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        string foundPath = FindNearestFile(source.Path, source.Pattern, source.ParentSearchLimit);
        if (foundPath.Length == 0)
        {
            ConfigValue missingRoot = EmptyRoot(source.SourceName, precedenceRank);
            if (source.Optional == ConfigOptional.Required)
            {
                diagnostics.AddLoadError(source.SourceName, source.Pattern, "missing required search file", 0, 0);
            }

            return new ConfigSourceResult(missingRoot, diagnostics);
        }

        ConfigSource baseSource = ConfigSource.FileByExtension(foundPath, ConfigOptional.Required, ConfigReload.None, foundPath);
        CopyLoadOptions(source, baseSource);
        ConfigSourceResult baseResult = LoadFileSource(baseSource, precedenceRank);
        if (source.EnvironmentName.Length == 0)
        {
            return baseResult;
        }

        string environmentFileName = EnvironmentLayerFileName(Path.GetFileName(foundPath), source.EnvironmentName);
        string environmentPath = Path.Combine(DirectoryName(foundPath), environmentFileName);
        if (!File.Exists(environmentPath))
        {
            return baseResult;
        }

        ConfigSource environmentSource = ConfigSource.FileByExtension(environmentPath, ConfigOptional.Required, ConfigReload.None, environmentPath);
        CopyLoadOptions(source, environmentSource);
        ConfigSourceResult environmentResult = LoadFileSource(environmentSource, precedenceRank);
        AppendDiagnostics(diagnostics, baseResult.Diagnostics);
        AppendDiagnostics(diagnostics, environmentResult.Diagnostics);
        ConfigValue root = ConfigMerger.MergeRoot(baseResult.Root, environmentResult.Root, ConfigMergeMode.Override, ConfigArrayMergeMode.Replace, ConfigNullMergeMode.Replace, diagnostics, "");
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static string FindNearestFile(string startDirectory, string fileName, int maximumParentLevels)
    {
        string current = startDirectory.Trim();
        if (current.Length == 0)
        {
            current = Directory.GetCurrentDirectory();
        }

        current = FullPath(current);
        int level = 0;
        while (level <= maximumParentLevels)
        {
            string candidate = Path.Combine(current, fileName);
            if (File.Exists(candidate))
            {
                return FullPath(candidate);
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }

            current = parent.FullName;
            level = level + 1;
        }

        return "";
    }

    internal static string EnvironmentLayerFileName(string fileName, string environmentName)
    {
        string extension = Path.GetExtension(fileName);
        string stem = Path.GetFileNameWithoutExtension(fileName);
        return stem + "." + environmentName + extension;
    }

    internal static ConfigSourceResult LoadEnvironmentSource(ConfigSource source, int precedenceRank)
    {
        if (source.EnvironmentValues != null)
        {
            return LoadEnvironmentMapSource(source, precedenceRank);
        }

        Dictionary<string, string> values = new Dictionary<string, string>();
        IDictionary variables = Environment.GetEnvironmentVariables();

        foreach (DictionaryEntry entry in variables)
        {
            string key = "";
            if (entry.Key != null)
            {
                string? keyText = entry.Key.ToString();
                if (keyText != null)
                {
                    key = keyText;
                }
            }

            string valueText = "";
            if (entry.Value != null)
            {
                string? text = entry.Value.ToString();
                if (text != null)
                {
                    valueText = text;
                }
            }

            values[key] = valueText;
        }

        ConfigSource deterministicSource = source.Copy();
        deterministicSource.EnvironmentValues = values;
        return LoadEnvironmentMapSource(deterministicSource, precedenceRank);
    }

    internal static ConfigSourceResult LoadEnvironmentMapSource(ConfigSource source, int precedenceRank)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        ConfigMap map = new ConfigMap();

        if (source.EnvironmentValues == null)
        {
            diagnostics.AddLoadError(source.SourceName, "", "missing environment map", 0, 0);
        }
        else
        {
            string[] keys = new string[source.EnvironmentValues.Keys.Count];
            source.EnvironmentValues.Keys.CopyTo(keys, 0);
            Array.Sort(keys, StringComparer.Ordinal);

            int index = 0;
            while (index < keys.Length)
            {
                SetEnvironmentValue(map, keys[index], source.EnvironmentValues[keys[index]], source, precedenceRank);
                index = index + 1;
            }
        }

        ConfigValue root = ConfigValue.Object(map).SetSource(source.SourceName, precedenceRank, "", 0, 0);
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static void SetEnvironmentValue(ConfigMap map, string key, string valueText, ConfigSource source, int precedenceRank)
    {
        if (valueText.Length == 0 && source.EmptyEnvMode == ConfigEmptyEnvMode.Ignore)
        {
            return;
        }

        string path = EnvironmentPath(key, source);
        if (path.Length > 0)
        {
            ConfigValue value = ConfigValue.String(valueText).SetSource(source.SourceName, precedenceRank, path, 0, 0);
            map.SetPathDefault(path, value);
        }
    }

    internal static string EnvironmentPath(string key, ConfigSource source)
    {
        string mappedPath = EnvironmentMappedPath(key, source);
        if (mappedPath.Length > 0)
        {
            return ConfigPath.Normalize(mappedPath, ".");
        }

        if (source.Prefix.Length > 0)
        {
            if (!key.StartsWith(source.Prefix, StringComparison.Ordinal))
            {
                return "";
            }

            key = key.Substring(source.Prefix.Length);
        }

        ConfigPath parsed = ConfigPath.Parse(key, source.Delimiter);
        return ConfigPath.JoinSegments(parsed.Segments, ".");
    }

    internal static string EnvironmentMappedPath(string key, ConfigSource source)
    {
        if (source.EnvironmentPathMap == null)
        {
            return "";
        }

        ConfigValue value = source.EnvironmentPathMap.Get(key);
        if (value.Kind != ConfigValueKind.String)
        {
            return "";
        }

        if (value.StringValue == null)
        {
            return "";
        }

        return value.StringValue;
    }

    internal static ConfigSourceResult LoadCommandLineSource(ConfigSource source, int precedenceRank)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        ConfigMap map = new ConfigMap();

        if (source.Args == null)
        {
            diagnostics.AddLoadError(source.SourceName, "", "missing command-line args", 0, 0);
        }
        else
        {
            int index = 0;
            while (index < source.Args.Length)
            {
                index = ParseCommandLineArgument(map, source.Args, index, source, precedenceRank, diagnostics);
            }
        }

        ConfigValue root = ConfigValue.Object(map).SetSource(source.SourceName, precedenceRank, "", 0, 0);
        return new ConfigSourceResult(root, diagnostics);
    }

    internal static int ParseCommandLineArgument(ConfigMap map, string[] args, int index, ConfigSource source, int precedenceRank, ConfigDiagnostics diagnostics)
    {
        string token = args[index];
        if (IsLongCommandLineSwitch(token))
        {
            return ParseCommandLineSwitch(map, args, index, token.Substring(2), source, precedenceRank, diagnostics, false);
        }

        if (IsShortCommandLineSwitch(token))
        {
            return ParseCommandLineSwitch(map, args, index, token.Substring(1), source, precedenceRank, diagnostics, false);
        }

        if (IsSlashCommandLineSwitch(token))
        {
            return ParseCommandLineSwitch(map, args, index, token.Substring(1), source, precedenceRank, diagnostics, true);
        }

        return index + 1;
    }

    internal static int ParseCommandLineSwitch(ConfigMap map, string[] args, int index, string text, ConfigSource source, int precedenceRank, ConfigDiagnostics diagnostics, bool allowColon)
    {
        int assignmentIndex = FindCommandLineAssignment(text, allowColon);
        string key = text;
        string valueText = "true";

        if (assignmentIndex >= 0)
        {
            key = text.Substring(0, assignmentIndex);
            valueText = text.Substring(assignmentIndex + 1);
            SetCommandLineValue(map, key, valueText, source, precedenceRank, diagnostics);
            return index + 1;
        }

        if (index + 1 < args.Length)
        {
            if (!IsCommandLineSwitch(args[index + 1]))
            {
                valueText = args[index + 1];
                SetCommandLineValue(map, key, valueText, source, precedenceRank, diagnostics);
                return index + 2;
            }
        }

        SetCommandLineValue(map, key, valueText, source, precedenceRank, diagnostics);
        return index + 1;
    }

    internal static void SetCommandLineValue(ConfigMap map, string key, string valueText, ConfigSource source, int precedenceRank, ConfigDiagnostics diagnostics)
    {
        string path = CommandLinePath(key, source);
        if (path.Length == 0)
        {
            diagnostics.AddLoadError(source.SourceName, "", "missing command-line key", 0, 0);
            return;
        }

        ConfigValue value = ConfigValue.String(valueText).SetSource(source.SourceName, precedenceRank, path, 0, 0);
        map.SetPathDefault(path, value);
    }

    internal static string CommandLinePath(string key, ConfigSource source)
    {
        if (source.SwitchMap != null)
        {
            ConfigValue alias = source.SwitchMap.Get(key);
            if (alias.Kind == ConfigValueKind.String && alias.StringValue != null)
            {
                return ConfigPath.Normalize(alias.StringValue, ".");
            }
        }

        return ConfigPath.Normalize(key, ".");
    }

    internal static int FindCommandLineAssignment(string text, bool allowColon)
    {
        int equalsIndex = text.IndexOf('=');
        int colonIndex = -1;
        if (allowColon)
        {
            colonIndex = text.IndexOf(':');
        }

        if (equalsIndex < 0)
        {
            return colonIndex;
        }

        if (colonIndex < 0)
        {
            return equalsIndex;
        }

        if (equalsIndex < colonIndex)
        {
            return equalsIndex;
        }

        return colonIndex;
    }

    internal static bool IsCommandLineSwitch(string token)
    {
        if (IsLongCommandLineSwitch(token))
        {
            return true;
        }

        if (IsShortCommandLineSwitch(token))
        {
            return true;
        }

        return IsSlashCommandLineSwitch(token);
    }

    internal static bool IsLongCommandLineSwitch(string token)
    {
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            return false;
        }

        return token.Length > 2;
    }

    internal static bool IsShortCommandLineSwitch(string token)
    {
        if (token.StartsWith("--", StringComparison.Ordinal))
        {
            return false;
        }

        if (!token.StartsWith("-", StringComparison.Ordinal))
        {
            return false;
        }

        return token.Length > 1;
    }

    internal static bool IsSlashCommandLineSwitch(string token)
    {
        if (!token.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        return token.Length > 1;
    }

    internal static ConfigParserOptions ParserOptions(ConfigSource source)
    {
        return new ConfigParserOptions(source.DuplicateKeyMode, source.DotEnvExpansionMode, source.DotEnvMissingValueMode, source.MissingIncludes, source.IncludeDepthLimit);
    }

    internal static void CopyLoadOptions(ConfigSource source, ConfigSource target)
    {
        target.DuplicateKeyMode = source.DuplicateKeyMode;
        target.DotEnvExpansionMode = source.DotEnvExpansionMode;
        target.DotEnvMissingValueMode = source.DotEnvMissingValueMode;
        target.TextEncoding = source.TextEncoding;
        target.MissingIncludes = source.MissingIncludes;
        target.IncludeDepthLimit = source.IncludeDepthLimit;
    }

    internal static ConfigValue ParseText(string text, ConfigFormat format, string sourceName, int precedenceRank, ConfigDiagnostics diagnostics)
    {
        return ParseTextWithOptions(text, format, sourceName, precedenceRank, diagnostics, ConfigParserOptions.Conservative());
    }

    internal static ConfigValue ParseTextWithOptions(string text, ConfigFormat format, string sourceName, int precedenceRank, ConfigDiagnostics diagnostics, ConfigParserOptions options)
    {
        if (format == ConfigFormat.Json)
        {
            return ConfigJsonParser.ParseWithDiagnostics(text, sourceName, precedenceRank, options.DuplicateKeyMode, diagnostics);
        }

        if (format == ConfigFormat.Ini)
        {
            return ConfigIniParser.Parse(text, sourceName, precedenceRank, options.DuplicateKeyMode, diagnostics);
        }

        if (format == ConfigFormat.Properties)
        {
            return ConfigPropertiesParser.Parse(text, sourceName, precedenceRank, options.DuplicateKeyMode, diagnostics);
        }

        if (format == ConfigFormat.DotEnv)
        {
            if (options.DotEnvExpansionMode == ConfigDotEnvExpansionMode.Enabled)
            {
                return ConfigDotEnvParser.ParseAndExpandWithMissingMode(text, sourceName, precedenceRank, options.DuplicateKeyMode, options.DotEnvMissingValueMode, diagnostics);
            }

            return ConfigDotEnvParser.Parse(text, sourceName, precedenceRank, options.DuplicateKeyMode, diagnostics);
        }

        if (format == ConfigFormat.Flat)
        {
            return ConfigFlatParser.Parse(text, sourceName, precedenceRank, options.DuplicateKeyMode, diagnostics);
        }

        if (format == ConfigFormat.Xml)
        {
            return ConfigXmlParser.ParseWithDiagnostics(text, sourceName, precedenceRank, diagnostics);
        }

        diagnostics.AddParseError(sourceName, "", "unsupported file format", 0, 0);
        return EmptyRoot(sourceName, precedenceRank);
    }

    internal static ConfigValue ApplyIncludes(ConfigSource source, ConfigValue root, int precedenceRank, ConfigDiagnostics diagnostics, string baseDirectory, List<string> includeStack)
    {
        if (root.Kind != ConfigValueKind.Object)
        {
            return root;
        }

        if (root.ObjectValue == null)
        {
            return root;
        }

        ConfigMap includingMap = root.ObjectValue.Copy();
        ConfigMap includedMap = ConfigMap.Create(includingMap.CaseMode);
        LoadIncludesFromValue(includedMap, includingMap.Get("include"), source, "include", precedenceRank, diagnostics, baseDirectory, includeStack);
        LoadIncludesFromValue(includedMap, includingMap.Get("includes"), source, "includes", precedenceRank, diagnostics, baseDirectory, includeStack);
        includingMap.Remove("include");
        includingMap.Remove("includes");
        MergeMap(includedMap, includingMap);

        ConfigValue includedRoot = ConfigValue.Object(includedMap);
        root.CopyMetadataTo(includedRoot);
        return includedRoot;
    }

    internal static void LoadIncludesFromValue(ConfigMap target, ConfigValue value, ConfigSource source, string path, int precedenceRank, ConfigDiagnostics diagnostics, string baseDirectory, List<string> includeStack)
    {
        if (value.Kind == ConfigValueKind.Missing)
        {
            return;
        }

        if (value.Kind == ConfigValueKind.String)
        {
            if (value.StringValue != null)
            {
                LoadIncludePath(target, value.StringValue, baseDirectory, precedenceRank, diagnostics, includeStack, source);
            }

            return;
        }

        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            int index = 0;
            while (index < value.ArrayValue.Count())
            {
                ConfigValue item = value.ArrayValue.Get(index);
                if (item.Kind == ConfigValueKind.String && item.StringValue != null)
                {
                    LoadIncludePath(target, item.StringValue, baseDirectory, precedenceRank, diagnostics, includeStack, source);
                }
                else
                {
                    diagnostics.AddLoadError(source.SourceName, path, "invalid include path", item.LineNumber, item.ColumnNumber);
                }

                index = index + 1;
            }

            return;
        }

        diagnostics.AddLoadError(source.SourceName, path, "invalid include path", value.LineNumber, value.ColumnNumber);
    }

    internal static void LoadIncludePath(ConfigMap target, string path, string baseDirectory, int precedenceRank, ConfigDiagnostics diagnostics, List<string> includeStack, ConfigSource includingSource)
    {
        string includePath = ResolveIncludePath(path, baseDirectory);
        if (includePath.Length == 0)
        {
            diagnostics.AddLoadError("", "", "invalid include path", 0, 0);
            return;
        }

        ConfigSource includeSource = ConfigSource.FileByExtension(includePath, includingSource.MissingIncludes, ConfigReload.None, includePath);
        CopyLoadOptions(includingSource, includeSource);
        ConfigSourceResult includeResult = LoadFileSourceWithIncludeStack(includeSource, precedenceRank, includeStack);
        AppendDiagnostics(diagnostics, includeResult.Diagnostics);
        if (File.Exists(includePath) && (includeResult.Root.Kind != ConfigValueKind.Object || includeResult.Root.ObjectValue == null))
        {
            diagnostics.AddLoadError(includePath, includePath, "invalid include root", includeResult.Root.LineNumber, includeResult.Root.ColumnNumber);
            return;
        }

        MergeObjectRoot(target, includeResult.Root);
    }

    internal static string ResolveIncludePath(string path, string baseDirectory)
    {
        string includePath = path.Trim();
        if (includePath.Length == 0)
        {
            return "";
        }

        if (Path.IsPathRooted(includePath))
        {
            return FullPath(includePath);
        }

        if (baseDirectory.Length == 0)
        {
            return FullPath(includePath);
        }

        return FullPath(Path.Combine(baseDirectory, includePath));
    }

    internal static string FullPath(string path)
    {
        return Path.GetFullPath(path);
    }

    internal static string DirectoryName(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (directory == null)
        {
            return "";
        }

        return directory;
    }

    internal static bool ContainsFilePath(List<string> paths, string path)
    {
        string fullPath = FullPath(path);

        int index = 0;
        while (index < paths.Count)
        {
            if (paths[index] == fullPath)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool TryDetectFormat(string path, ConfigDiagnostics diagnostics, string sourceName)
    {
        string extension = FileExtension(path);
        if (extension == ".json")
        {
            return true;
        }

        if (extension == ".ini")
        {
            return true;
        }

        if (extension == ".properties")
        {
            return true;
        }

        if (extension == ".env")
        {
            return true;
        }

        if (extension == ".flat")
        {
            return true;
        }

        if (extension == ".kv")
        {
            return true;
        }

        if (extension == ".xml")
        {
            return true;
        }

        diagnostics.AddLoadError(sourceName, "", "unsupported file extension", 0, 0);
        return false;
    }

    internal static ConfigFormat DetectFormat(string path)
    {
        string extension = FileExtension(path);
        if (extension == ".ini")
        {
            return ConfigFormat.Ini;
        }

        if (extension == ".properties")
        {
            return ConfigFormat.Properties;
        }

        if (extension == ".env")
        {
            return ConfigFormat.DotEnv;
        }

        if (extension == ".flat")
        {
            return ConfigFormat.Flat;
        }

        if (extension == ".kv")
        {
            return ConfigFormat.Flat;
        }

        if (extension == ".xml")
        {
            return ConfigFormat.Xml;
        }

        return ConfigFormat.Json;
    }

    internal static string FileExtension(string path)
    {
        string fileName = Path.GetFileName(path);
        if (fileName == ".env")
        {
            return ".env";
        }

        return Path.GetExtension(path).ToLowerInvariant();
    }

    internal static ConfigValue EmptyRoot(string sourceName, int precedenceRank)
    {
        ConfigMap map = new ConfigMap();
        return ConfigValue.Object(map).SetSource(sourceName, precedenceRank, "", 0, 0);
    }

    internal static void AppendDiagnostics(ConfigDiagnostics target, ConfigDiagnostics source)
    {
        int errorIndex = 0;
        while (errorIndex < source.Errors.Count)
        {
            target.Errors.Add(source.Errors[errorIndex]);
            errorIndex = errorIndex + 1;
        }

        int warningIndex = 0;
        while (warningIndex < source.Warnings.Count)
        {
            target.Warnings.Add(source.Warnings[warningIndex]);
            warningIndex = warningIndex + 1;
        }

        int traceIndex = 0;
        while (traceIndex < source.Traces.Count)
        {
            target.Traces.Add(source.Traces[traceIndex]);
            traceIndex = traceIndex + 1;
        }
    }

    internal static void MergeObjectRoot(ConfigMap target, ConfigValue root)
    {
        if (root.Kind != ConfigValueKind.Object)
        {
            return;
        }

        if (root.ObjectValue == null)
        {
            return;
        }

        MergeMap(target, root.ObjectValue);
    }

    internal static void MergeMap(ConfigMap target, ConfigMap source)
    {
        string[] keys = source.Keys();

        int index = 0;
        while (index < keys.Length)
        {
            string key = keys[index];
            ConfigValue existing = target.Get(key);
            ConfigValue incoming = source.Get(key);

            if (existing.Kind == ConfigValueKind.Object && existing.ObjectValue != null && incoming.Kind == ConfigValueKind.Object && incoming.ObjectValue != null)
            {
                MergeMap(existing.ObjectValue, incoming.ObjectValue);
            }
            else
            {
                target.Set(key, incoming.Copy());
            }

            index = index + 1;
        }
    }

    internal static void ApplySourceMetadataToMap(ConfigMap map, string sourceName, int precedenceRank, string parentPath)
    {
        string[] keys = map.Keys();

        int index = 0;
        while (index < keys.Length)
        {
            string path = ConfigJsonParser.ChildPath(parentPath, keys[index]);
            ConfigValue value = map.Get(keys[index]);
            ApplySourceMetadataToValue(value, sourceName, precedenceRank, path);
            index = index + 1;
        }
    }

    internal static void ApplySourceMetadataToArray(ConfigArray array, string sourceName, int precedenceRank, string parentPath)
    {
        int index = 0;
        while (index < array.Count())
        {
            string path = ConfigJsonParser.ChildPath(parentPath, index.ToString());
            ConfigValue value = array.Get(index);
            ApplySourceMetadataToValue(value, sourceName, precedenceRank, path);
            index = index + 1;
        }
    }

    internal static void ApplySourceMetadataToValue(ConfigValue value, string sourceName, int precedenceRank, string path)
    {
        value.SetSource(sourceName, precedenceRank, path, 0, 0);

        if (value.Kind == ConfigValueKind.Object && value.ObjectValue != null)
        {
            ApplySourceMetadataToMap(value.ObjectValue, sourceName, precedenceRank, path);
        }

        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            ApplySourceMetadataToArray(value.ArrayValue, sourceName, precedenceRank, path);
        }
    }
}
