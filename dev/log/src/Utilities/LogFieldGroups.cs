using System.Collections.Generic;

namespace log;

internal class LogFieldGroups
{
    internal static LogField CreateGroup(string? name, LogField[]? fields)
    {
        string normalizedName = LogResult.NormalizeText(name);
        Dictionary<string, LogValue> values = LogFieldGroups.ValuesFromFields(fields);
        return new LogField(normalizedName, LogValue.Dictionary(values));
    }

    internal static LogField CreateNamespace(string? namespaceName, LogField[]? fields)
    {
        string normalizedName = LogResult.NormalizeText(namespaceName);
        if (!LogName.IsValidFieldName(normalizedName))
        {
            return new LogField(normalizedName, null);
        }

        string[] segments = normalizedName.Split('.');
        int index = segments.Length - 1;
        LogField current = LogFieldGroups.CreateGroup(segments[index], fields);
        index = index - 1;
        while (index >= 0)
        {
            Dictionary<string, LogValue> values = new Dictionary<string, LogValue>();
            values[current.Name] = current.Value;
            current = new LogField(segments[index], LogValue.Dictionary(values));
            index = index - 1;
        }

        return current;
    }

    internal static Dictionary<string, LogValue> ValuesFromFields(LogField[]? fields)
    {
        Dictionary<string, LogValue> values = new Dictionary<string, LogValue>();
        if (fields == null)
        {
            return values;
        }

        LogField[] resolved = LogFieldNameResolver.Resolve(fields);
        foreach (LogField field in resolved)
        {
            values[field.Name] = field.Value;
        }

        return values;
    }
}
