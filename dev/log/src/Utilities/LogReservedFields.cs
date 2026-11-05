using System;
using System.Collections.Generic;

namespace log;

internal class LogReservedFields
{
    internal static LogField[] Resolve(LogField[] fields)
    {
        if (fields == null)
        {
            return new LogField[0];
        }

        Dictionary<string, int> originalCounts = LogFieldNameResolver.CountOriginalNames(fields);
        Dictionary<string, int> usedNames = new Dictionary<string, int>();
        int validCount = LogFieldNameResolver.CountValidFields(fields);
        LogField[] staged = new LogField[validCount];
        int index = 0;

        foreach (LogField field in fields)
        {
            if (field == null || !field.IsValid)
            {
                continue;
            }

            string resolvedName = field.Name;
            if (LogReservedFields.IsReservedFieldName(field.Name))
            {
                resolvedName = LogReservedFields.ResolveReservedName(field.Name, originalCounts, usedNames);
            }

            staged[index] = new LogField(resolvedName, field.Value);
            usedNames[resolvedName] = 1;
            index = index + 1;
        }

        return LogFieldNameResolver.Resolve(staged);
    }

    internal static bool IsReservedFieldName(string name)
    {
        return LogReservedFields.EqualsReserved(name, "timestamp")
            || LogReservedFields.EqualsReserved(name, "level")
            || LogReservedFields.EqualsReserved(name, "logger")
            || LogReservedFields.EqualsReserved(name, "loggerName")
            || LogReservedFields.EqualsReserved(name, "eventId")
            || LogReservedFields.EqualsReserved(name, "eventName")
            || LogReservedFields.EqualsReserved(name, "messageTemplate")
            || LogReservedFields.EqualsReserved(name, "message")
            || LogReservedFields.EqualsReserved(name, "renderedMessage")
            || LogReservedFields.EqualsReserved(name, "exception")
            || LogReservedFields.EqualsReserved(name, "traceId")
            || LogReservedFields.EqualsReserved(name, "spanId")
            || LogReservedFields.EqualsReserved(name, "parentSpanId")
            || LogReservedFields.EqualsReserved(name, "threadId")
            || LogReservedFields.EqualsReserved(name, "processId")
            || LogReservedFields.EqualsReserved(name, "machineName")
            || LogReservedFields.EqualsReserved(name, "applicationName")
            || LogReservedFields.EqualsReserved(name, "sourceFile")
            || LogReservedFields.EqualsReserved(name, "sourceMemberName")
            || LogReservedFields.EqualsReserved(name, "sourceLineNumber")
            || LogReservedFields.EqualsReserved(name, "fields");
    }

    internal static bool EqualsReserved(string name, string reservedName)
    {
        return string.Equals(LogResult.NormalizeText(name), reservedName, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ResolveReservedName(string name, Dictionary<string, int> originalCounts, Dictionary<string, int> usedNames)
    {
        string baseName = LogReservedFields.ReservedFieldName(name);
        string candidate = baseName;
        int suffix = 1;

        while (usedNames.ContainsKey(candidate) || originalCounts.ContainsKey(candidate))
        {
            candidate = LogFieldNameResolver.SuffixedName(baseName, suffix);
            suffix = suffix + 1;
        }

        return candidate;
    }

    internal static string ReservedFieldName(string name)
    {
        return "field_" + LogResult.NormalizeText(name);
    }
}
