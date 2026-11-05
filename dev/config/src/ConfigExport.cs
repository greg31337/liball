using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace config;

internal class ConfigExporter
{
    internal static string ToJson(Config config)
    {
        return ToJsonValue(config.Value(""));
    }

    internal static string ToJson(Config config, ConfigRedactor redactor)
    {
        return ToJsonValue(config.Value(""), config.Delimiter, redactor);
    }

    internal static Dictionary<string, string> ToFlatMap(Config config)
    {
        return ToFlatMapValue(config.Value(""), config.Delimiter);
    }

    internal static Dictionary<string, string> ToFlatMap(Config config, ConfigRedactor redactor)
    {
        return ToFlatMapValue(config.Value(""), config.Delimiter, redactor);
    }

    internal static string ToFlatText(Config config)
    {
        return ToFlatTextMap(ToFlatMap(config));
    }

    internal static string ToFlatText(Config config, ConfigRedactor redactor)
    {
        return ToFlatTextMap(ToFlatMap(config, redactor));
    }

    internal static string ToFlatTextMap(Dictionary<string, string> map)
    {
        string[] keys = new string[map.Keys.Count];
        map.Keys.CopyTo(keys, 0);
        Array.Sort(keys, StringComparer.Ordinal);

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < keys.Length)
        {
            builder.Append(keys[index]);
            builder.Append("=");
            AppendFlatString(builder, map[keys[index]]);
            builder.Append("\n");
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static void WriteJsonFile(Config config, string path)
    {
        WriteNewFile(path, ToJson(config));
    }

    internal static void OverwriteJsonFile(Config config, string path)
    {
        WriteOverwriteFile(path, ToJson(config));
    }

    internal static void WriteFlatFile(Config config, string path)
    {
        WriteNewFile(path, ToFlatText(config));
    }

    internal static void OverwriteFlatFile(Config config, string path)
    {
        WriteOverwriteFile(path, ToFlatText(config));
    }

    internal static void WriteNewFile(string path, string text)
    {
        using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(text);
            }
        }
    }

    internal static void WriteOverwriteFile(string path, string text)
    {
        File.WriteAllText(path, text, Encoding.UTF8);
    }

    internal static Dictionary<string, string> ToFlatMapValue(ConfigValue value, string delimiter)
    {
        Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
        AddFlatValue(map, "", value, ConfigPath.CleanDelimiter(delimiter));
        return map;
    }

    internal static Dictionary<string, string> ToFlatMapValue(ConfigValue value, string delimiter, ConfigRedactor redactor)
    {
        Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
        AddFlatValue(map, "", value, ConfigPath.CleanDelimiter(delimiter), redactor);
        return map;
    }

    internal static string ToJsonValue(ConfigValue value)
    {
        StringBuilder builder = new StringBuilder();
        AppendJsonValue(builder, value);
        return builder.ToString();
    }

    internal static string ToJsonValue(ConfigValue value, string delimiter, ConfigRedactor redactor)
    {
        StringBuilder builder = new StringBuilder();
        AppendJsonValue(builder, value, "", ConfigPath.CleanDelimiter(delimiter), redactor);
        return builder.ToString();
    }

    internal static void AppendJsonValue(StringBuilder builder, ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Missing || value.Kind == ConfigValueKind.Null)
        {
            builder.Append("null");
            return;
        }

        if (value.Kind == ConfigValueKind.String)
        {
            string text = "";
            if (value.StringValue != null)
            {
                text = value.StringValue;
            }

            AppendJsonString(builder, text);
            return;
        }

        if (value.Kind == ConfigValueKind.Number)
        {
            builder.Append(value.NumberValue.ToString(CultureInfo.InvariantCulture));
            return;
        }

        if (value.Kind == ConfigValueKind.Boolean)
        {
            if (value.BooleanValue)
            {
                builder.Append("true");
            }
            else
            {
                builder.Append("false");
            }

            return;
        }

        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            AppendJsonArray(builder, value.ArrayValue);
            return;
        }

        if (value.Kind == ConfigValueKind.Object && value.ObjectValue != null)
        {
            AppendJsonObject(builder, value.ObjectValue);
            return;
        }

        builder.Append("null");
    }

    internal static void AppendJsonValue(StringBuilder builder, ConfigValue value, string path, string delimiter, ConfigRedactor redactor)
    {
        if (path.Length > 0 && redactor.IsSecretPath(path))
        {
            AppendJsonString(builder, redactor.Replacement);
            return;
        }

        if (value.Kind == ConfigValueKind.Missing || value.Kind == ConfigValueKind.Null)
        {
            builder.Append("null");
            return;
        }

        if (value.Kind == ConfigValueKind.String)
        {
            string text = "";
            if (value.StringValue != null)
            {
                text = value.StringValue;
            }

            AppendJsonString(builder, text);
            return;
        }

        if (value.Kind == ConfigValueKind.Number)
        {
            builder.Append(value.NumberValue.ToString(CultureInfo.InvariantCulture));
            return;
        }

        if (value.Kind == ConfigValueKind.Boolean)
        {
            if (value.BooleanValue)
            {
                builder.Append("true");
            }
            else
            {
                builder.Append("false");
            }

            return;
        }

        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            AppendJsonArray(builder, value.ArrayValue, path, delimiter, redactor);
            return;
        }

        if (value.Kind == ConfigValueKind.Object && value.ObjectValue != null)
        {
            AppendJsonObject(builder, value.ObjectValue, path, delimiter, redactor);
            return;
        }

        builder.Append("null");
    }

    internal static void AppendJsonObject(StringBuilder builder, ConfigMap map)
    {
        string[] keys = map.Keys();
        builder.Append("{");

        int index = 0;
        while (index < keys.Length)
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            AppendJsonString(builder, keys[index]);
            builder.Append(":");
            AppendJsonValue(builder, map.Get(keys[index]));
            index = index + 1;
        }

        builder.Append("}");
    }

    internal static void AppendJsonObject(StringBuilder builder, ConfigMap map, string path, string delimiter, ConfigRedactor redactor)
    {
        string[] keys = map.Keys();
        builder.Append("{");

        int index = 0;
        while (index < keys.Length)
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            string childPath = FlatChildPath(path, keys[index], delimiter);
            AppendJsonString(builder, keys[index]);
            builder.Append(":");
            AppendJsonValue(builder, map.Get(keys[index]), childPath, delimiter, redactor);
            index = index + 1;
        }

        builder.Append("}");
    }

    internal static void AppendJsonArray(StringBuilder builder, ConfigArray array)
    {
        builder.Append("[");

        int index = 0;
        while (index < array.Count())
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            AppendJsonValue(builder, array.Get(index));
            index = index + 1;
        }

        builder.Append("]");
    }

    internal static void AppendJsonArray(StringBuilder builder, ConfigArray array, string path, string delimiter, ConfigRedactor redactor)
    {
        builder.Append("[");

        int index = 0;
        while (index < array.Count())
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            string childPath = FlatChildPath(path, index.ToString(CultureInfo.InvariantCulture), delimiter);
            AppendJsonValue(builder, array.Get(index), childPath, delimiter, redactor);
            index = index + 1;
        }

        builder.Append("]");
    }

    internal static void AppendJsonString(StringBuilder builder, string text)
    {
        builder.Append("\"");

        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '"')
            {
                builder.Append("\\\"");
            }
            else if (character == '\\')
            {
                builder.Append("\\\\");
            }
            else if (character == '\b')
            {
                builder.Append("\\b");
            }
            else if (character == '\f')
            {
                builder.Append("\\f");
            }
            else if (character == '\n')
            {
                builder.Append("\\n");
            }
            else if (character == '\r')
            {
                builder.Append("\\r");
            }
            else if (character == '\t')
            {
                builder.Append("\\t");
            }
            else if ((int)character < 32)
            {
                builder.Append("\\u");
                builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }

        builder.Append("\"");
    }

    internal static void AppendFlatString(StringBuilder builder, string text)
    {
        builder.Append("\"");

        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '"')
            {
                builder.Append("\\\"");
            }
            else if (character == '\\')
            {
                builder.Append("\\\\");
            }
            else if (character == '\n')
            {
                builder.Append("\\n");
            }
            else if (character == '\r')
            {
                builder.Append("\\r");
            }
            else if (character == '\t')
            {
                builder.Append("\\t");
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }

        builder.Append("\"");
    }

    internal static void AddFlatValue(Dictionary<string, string> map, string path, ConfigValue value, string delimiter)
    {
        if (value.Kind == ConfigValueKind.Missing)
        {
            return;
        }

        if (value.Kind == ConfigValueKind.Object && value.ObjectValue != null)
        {
            AddFlatObject(map, path, value.ObjectValue, delimiter);
            return;
        }

        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            AddFlatArray(map, path, value.ArrayValue, delimiter);
            return;
        }

        map[path] = FlatScalarValue(value);
    }

    internal static void AddFlatValue(Dictionary<string, string> map, string path, ConfigValue value, string delimiter, ConfigRedactor redactor)
    {
        if (value.Kind == ConfigValueKind.Missing)
        {
            return;
        }

        if (path.Length > 0 && redactor.IsSecretPath(path))
        {
            map[path] = redactor.RedactValue(path, FlatScalarValue(value));
            return;
        }

        if (value.Kind == ConfigValueKind.Object && value.ObjectValue != null)
        {
            AddFlatObject(map, path, value.ObjectValue, delimiter, redactor);
            return;
        }

        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            AddFlatArray(map, path, value.ArrayValue, delimiter, redactor);
            return;
        }

        map[path] = FlatScalarValue(value);
    }

    internal static void AddFlatObject(Dictionary<string, string> map, string path, ConfigMap value, string delimiter)
    {
        string[] keys = value.Keys();

        int index = 0;
        while (index < keys.Length)
        {
            string childPath = FlatChildPath(path, keys[index], delimiter);
            AddFlatValue(map, childPath, value.Get(keys[index]), delimiter);
            index = index + 1;
        }
    }

    internal static void AddFlatObject(Dictionary<string, string> map, string path, ConfigMap value, string delimiter, ConfigRedactor redactor)
    {
        string[] keys = value.Keys();

        int index = 0;
        while (index < keys.Length)
        {
            string childPath = FlatChildPath(path, keys[index], delimiter);
            AddFlatValue(map, childPath, value.Get(keys[index]), delimiter, redactor);
            index = index + 1;
        }
    }

    internal static void AddFlatArray(Dictionary<string, string> map, string path, ConfigArray value, string delimiter)
    {
        int index = 0;
        while (index < value.Count())
        {
            string childPath = FlatChildPath(path, index.ToString(CultureInfo.InvariantCulture), delimiter);
            AddFlatValue(map, childPath, value.Get(index), delimiter);
            index = index + 1;
        }
    }

    internal static void AddFlatArray(Dictionary<string, string> map, string path, ConfigArray value, string delimiter, ConfigRedactor redactor)
    {
        int index = 0;
        while (index < value.Count())
        {
            string childPath = FlatChildPath(path, index.ToString(CultureInfo.InvariantCulture), delimiter);
            AddFlatValue(map, childPath, value.Get(index), delimiter, redactor);
            index = index + 1;
        }
    }

    internal static string FlatScalarValue(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Null)
        {
            return "";
        }

        return ConfigConversions.StringValue(value);
    }

    internal static string FlatChildPath(string parentPath, string childSegment, string delimiter)
    {
        string escaped = ConfigPath.EscapeSegment(childSegment, delimiter);
        if (parentPath.Length == 0)
        {
            return escaped;
        }

        return parentPath + delimiter + escaped;
    }
}
