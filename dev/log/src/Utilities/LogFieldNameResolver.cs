using System.Collections.Generic;

namespace log;

internal class LogFieldNameResolver
{
    internal static LogField[] Resolve(LogField[] fields)
    {
        if (fields == null)
        {
            return new LogField[0];
        }

        Dictionary<string, int> originalCounts = LogFieldNameResolver.CountOriginalNames(fields);
        Dictionary<string, int> usedNames = new Dictionary<string, int>();
        Dictionary<string, int> occurrenceCounts = new Dictionary<string, int>();
        int validCount = LogFieldNameResolver.CountValidFields(fields);
        LogField[] resolved = new LogField[validCount];
        int index = 0;

        foreach (LogField field in fields)
        {
            if (field == null || !field.IsValid)
            {
                continue;
            }

            string resolvedName = LogFieldNameResolver.ResolveName(field.Name, originalCounts, usedNames, occurrenceCounts);
            resolved[index] = new LogField(resolvedName, field.Value);
            usedNames[resolvedName] = 1;
            index = index + 1;
        }

        return resolved;
    }

    internal static string ResolveName(string name, Dictionary<string, int> originalCounts, Dictionary<string, int> usedNames, Dictionary<string, int> occurrenceCounts)
    {
        int occurrence = LogFieldNameResolver.GetCount(occurrenceCounts, name);
        occurrenceCounts[name] = occurrence + 1;

        if (occurrence == 0 && !usedNames.ContainsKey(name))
        {
            return name;
        }

        int suffix = occurrence;
        string candidate = LogFieldNameResolver.SuffixedName(name, suffix);
        while (usedNames.ContainsKey(candidate) || LogFieldNameResolver.IsReservedOriginalName(candidate, originalCounts))
        {
            suffix = suffix + 1;
            candidate = LogFieldNameResolver.SuffixedName(name, suffix);
        }

        return candidate;
    }

    internal static Dictionary<string, int> CountOriginalNames(LogField[] fields)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        if (fields == null)
        {
            return counts;
        }

        foreach (LogField field in fields)
        {
            if (field == null || !field.IsValid)
            {
                continue;
            }

            int count = LogFieldNameResolver.GetCount(counts, field.Name);
            counts[field.Name] = count + 1;
        }

        return counts;
    }

    internal static int CountValidFields(LogField[] fields)
    {
        if (fields == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LogField field in fields)
        {
            if (field != null && field.IsValid)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static int GetCount(Dictionary<string, int> counts, string name)
    {
        if (counts == null)
        {
            return 0;
        }

        if (!counts.ContainsKey(name))
        {
            return 0;
        }

        return counts[name];
    }

    internal static bool IsReservedOriginalName(string name, Dictionary<string, int> originalCounts)
    {
        if (originalCounts == null)
        {
            return false;
        }

        return originalCounts.ContainsKey(name);
    }

    internal static string SuffixedName(string name, int suffix)
    {
        return name + "_" + suffix.ToString();
    }
}
