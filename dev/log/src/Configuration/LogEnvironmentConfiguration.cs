using System;
using System.Collections.Generic;

namespace log;

internal class LogEnvironmentConfiguration
{
    internal const string Prefix = "LOG_";

    internal static LogConfiguration Apply(LogConfiguration configuration)
    {
        return LogEnvironmentConfiguration.Apply(configuration, LogEnvironmentConfiguration.ProcessEnvironment());
    }

    internal static LogConfiguration Apply(LogConfiguration configuration, Dictionary<string, string> environment)
    {
        if (configuration == null)
        {
            return LogConfiguration.Create();
        }

        if (environment == null)
        {
            configuration.Validate();
            return configuration;
        }

        LogEnvironmentConfiguration.ApplyName(configuration, environment);
        LogEnvironmentConfiguration.ApplyLevel(configuration, environment);
        LogEnvironmentConfiguration.ApplyRuntime(configuration, environment);
        LogEnvironmentConfiguration.ApplyActivity(configuration, environment);
        LogEnvironmentConfiguration.ApplySinks(configuration, environment);
        LogEnvironmentConfiguration.ApplyFilePath(configuration, environment);
        LogEnvironmentConfiguration.ApplyFields(configuration, environment);
        LogEnvironmentConfiguration.ApplyCategoryLevels(configuration, environment);
        configuration.Validate();
        return configuration;
    }

    internal static Dictionary<string, string> ProcessEnvironment()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        System.Collections.IDictionary environment = Environment.GetEnvironmentVariables();
        foreach (System.Collections.DictionaryEntry entry in environment)
        {
            string key = "";
            string value = "";
            if (entry.Key != null)
            {
                key = LogResult.NormalizeText(entry.Key.ToString());
            }

            if (entry.Value != null)
            {
                value = LogResult.NormalizeText(entry.Value.ToString());
            }

            if (key.Length > 0)
            {
                values[key] = value;
            }
        }

        return values;
    }

    internal static void ApplyName(LogConfiguration configuration, Dictionary<string, string> environment)
    {
        string value = LogEnvironmentConfiguration.Value(environment, "LOG_NAME");
        if (value.Length == 0)
        {
            value = LogEnvironmentConfiguration.Value(environment, "LOG_LOGGER_NAME");
        }

        if (value.Length > 0)
        {
            configuration.Name(value);
        }
    }

    internal static void ApplyLevel(LogConfiguration configuration, Dictionary<string, string> environment)
    {
        string value = LogEnvironmentConfiguration.Value(environment, "LOG_MINIMUM_LEVEL");
        if (value.Length == 0)
        {
            value = LogEnvironmentConfiguration.Value(environment, "LOG_LEVEL");
        }

        if (value.Length > 0)
        {
            configuration.MinimumLevel(LogJsonConfigurationLoader.LevelValue(LogEnvironmentConfiguration.JsonString(value), configuration.ConfiguredLevelSwitch.GetLevel()));
        }
    }

    internal static void ApplyRuntime(LogConfiguration configuration, Dictionary<string, string> environment)
    {
        string value = LogEnvironmentConfiguration.Value(environment, "LOG_RUNTIME_METADATA");
        if (value.Length == 0)
        {
            value = LogEnvironmentConfiguration.Value(environment, "LOG_INCLUDE_RUNTIME_METADATA");
        }

        if (value.Length > 0)
        {
            configuration.RuntimeMetadata(LogEnvironmentConfiguration.BoolValue(value, configuration.IncludeRuntimeMetadata));
        }
    }

    internal static void ApplyActivity(LogConfiguration configuration, Dictionary<string, string> environment)
    {
        string value = LogEnvironmentConfiguration.Value(environment, "LOG_ACTIVITY");
        if (value.Length == 0)
        {
            value = LogEnvironmentConfiguration.Value(environment, "LOG_INCLUDE_ACTIVITY");
        }

        if (value.Length > 0)
        {
            configuration.Activity(LogEnvironmentConfiguration.BoolValue(value, configuration.IncludeActivity));
        }
    }

    internal static void ApplySinks(LogConfiguration configuration, Dictionary<string, string> environment)
    {
        string value = LogEnvironmentConfiguration.Value(environment, "LOG_SINKS");
        if (value.Length == 0)
        {
            value = LogEnvironmentConfiguration.Value(environment, "LOG_SINK");
        }

        if (value.Length == 0)
        {
            return;
        }

        configuration.ClearSinks();
        string[] tokens = LogEnvironmentConfiguration.SplitList(value);
        int index = 0;
        while (index < tokens.Length)
        {
            string sinkName = LogEnvironmentConfiguration.NormalizeToken(tokens[index]);
            if (sinkName.Length > 0)
            {
                configuration.AddSink(LogEnvironmentConfiguration.SinkFromEnvironment(sinkName, environment));
            }

            index = index + 1;
        }
    }

    internal static LogSink SinkFromEnvironment(string sinkName, Dictionary<string, string> environment)
    {
        string normalizedName = LogJsonConfigurationLoader.NormalizeName(sinkName);
        if (normalizedName == "file")
        {
            string path = LogEnvironmentConfiguration.Value(environment, "LOG_FILE_PATH");
            string formatterName = LogEnvironmentConfiguration.Value(environment, "LOG_FORMATTER");
            bool append = LogEnvironmentConfiguration.BoolValue(LogEnvironmentConfiguration.Value(environment, "LOG_FILE_APPEND"), true);
            return FileLogSink.WithFormatter(path, LogFormatter.Named(formatterName), append);
        }

        if (normalizedName == "console")
        {
            return ConsoleLogSink.Create();
        }

        if (normalizedName == "debug")
        {
            return DebugLogSink.Create();
        }

        if (normalizedName == "trace")
        {
            return TraceLogSink.Create();
        }

        if (normalizedName == "null")
        {
            return NullLogSink.Create();
        }

        return MemoryLogSink.Create();
    }

    internal static void ApplyFilePath(LogConfiguration configuration, Dictionary<string, string> environment)
    {
        string path = LogEnvironmentConfiguration.Value(environment, "LOG_FILE_PATH");
        if (path.Length == 0)
        {
            return;
        }

        bool found = false;
        foreach (LogSink sink in configuration.Sinks)
        {
            if (sink != null && sink.IsFileSink)
            {
                sink.FilePath = FileLogSink.NormalizePath(path);
                sink.ActiveFilePath = sink.FilePath;
                found = true;
            }
        }

        if (!found)
        {
            string formatterName = LogEnvironmentConfiguration.Value(environment, "LOG_FORMATTER");
            bool append = LogEnvironmentConfiguration.BoolValue(LogEnvironmentConfiguration.Value(environment, "LOG_FILE_APPEND"), true);
            configuration.AddSink(FileLogSink.WithFormatter(path, LogFormatter.Named(formatterName), append));
        }
    }

    internal static void ApplyFields(LogConfiguration configuration, Dictionary<string, string> environment)
    {
        string value = LogEnvironmentConfiguration.Value(environment, "LOG_FIELDS");
        if (value.Length > 0)
        {
            string[] pairs = LogEnvironmentConfiguration.SplitList(value);
            int index = 0;
            while (index < pairs.Length)
            {
                LogEnvironmentConfiguration.ApplyFieldPair(configuration, pairs[index]);
                index = index + 1;
            }
        }

        foreach (KeyValuePair<string, string> entry in environment)
        {
            if (LogEnvironmentConfiguration.StartsWith(entry.Key, "LOG_FIELD__"))
            {
                string name = entry.Key.Substring("LOG_FIELD__".Length).Replace("__", ".");
                LogEnvironmentConfiguration.SetGlobalField(configuration, name, entry.Value);
            }
        }
    }

    internal static void ApplyFieldPair(LogConfiguration configuration, string pair)
    {
        int equalsIndex = LogResult.NormalizeText(pair).IndexOf('=');
        if (equalsIndex < 1)
        {
            return;
        }

        string name = pair.Substring(0, equalsIndex).Trim();
        string value = pair.Substring(equalsIndex + 1).Trim();
        LogEnvironmentConfiguration.SetGlobalField(configuration, name, value);
    }

    internal static void SetGlobalField(LogConfiguration configuration, string name, string value)
    {
        LogField field = LogField.String(name, value);
        int index = 0;
        while (index < configuration.GlobalFields.Length)
        {
            if (configuration.GlobalFields[index] != null && configuration.GlobalFields[index].Name == field.Name)
            {
                configuration.GlobalFields[index] = field;
                return;
            }

            index = index + 1;
        }

        configuration.AddGlobalLogField(field);
    }

    internal static void ApplyCategoryLevels(LogConfiguration configuration, Dictionary<string, string> environment)
    {
        string value = LogEnvironmentConfiguration.Value(environment, "LOG_CATEGORY_LEVELS");
        if (value.Length > 0)
        {
            string[] pairs = LogEnvironmentConfiguration.SplitList(value);
            int index = 0;
            while (index < pairs.Length)
            {
                LogEnvironmentConfiguration.ApplyCategoryLevelPair(configuration, pairs[index]);
                index = index + 1;
            }
        }

        foreach (KeyValuePair<string, string> entry in environment)
        {
            if (LogEnvironmentConfiguration.StartsWith(entry.Key, "LOG_CATEGORY_LEVEL__"))
            {
                string name = entry.Key.Substring("LOG_CATEGORY_LEVEL__".Length).Replace("__", ".");
                configuration.CategoryLevel(name, LogJsonConfigurationLoader.LevelValue(LogEnvironmentConfiguration.JsonString(entry.Value), LogLevel.Info));
            }
        }
    }

    internal static void ApplyCategoryLevelPair(LogConfiguration configuration, string pair)
    {
        int equalsIndex = LogResult.NormalizeText(pair).IndexOf('=');
        if (equalsIndex < 1)
        {
            return;
        }

        string name = pair.Substring(0, equalsIndex).Trim();
        string value = pair.Substring(equalsIndex + 1).Trim();
        configuration.CategoryLevel(name, LogJsonConfigurationLoader.LevelValue(LogEnvironmentConfiguration.JsonString(value), LogLevel.Info));
    }

    internal static string Value(Dictionary<string, string> environment, string name)
    {
        if (environment == null)
        {
            return "";
        }

        foreach (KeyValuePair<string, string> entry in environment)
        {
            if (LogEnvironmentConfiguration.NameEquals(entry.Key, name))
            {
                return LogResult.NormalizeText(entry.Value);
            }
        }

        return "";
    }

    internal static bool NameEquals(string left, string right)
    {
        return string.Equals(LogResult.NormalizeText(left), LogResult.NormalizeText(right), StringComparison.OrdinalIgnoreCase);
    }

    internal static bool StartsWith(string value, string prefix)
    {
        return LogResult.NormalizeText(value).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    internal static string[] SplitList(string value)
    {
        return LogResult.NormalizeText(value).Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
    }

    internal static string NormalizeToken(string value)
    {
        return LogResult.NormalizeText(value).Trim();
    }

    internal static bool BoolValue(string value, bool fallback)
    {
        return LogJsonConfigurationLoader.BoolValue(LogEnvironmentConfiguration.JsonString(value), fallback);
    }

    internal static System.Text.Json.JsonElement JsonString(string value)
    {
        return System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(LogResult.NormalizeText(value))).RootElement.Clone();
    }
}
