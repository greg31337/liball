using System;
using System.Collections.Generic;

namespace htmlgui;

internal enum DiagnosticLevel
{
    Information = 0,
    Warning = 1,
    Error = 2
}

internal class DiagnosticEntry
{
    internal DiagnosticLevel Level;
    internal string Code;
    internal string Message;
    internal string Location;

    internal DiagnosticEntry(DiagnosticLevel level, string code, string message, string location)
    {
        Level = level;
        Code = code;
        Message = message;
        Location = location;
    }
}

internal class DiagnosticSink
{
    internal List<DiagnosticEntry> Entries;
    internal int MaximumCount;
    internal bool Truncated;

    internal DiagnosticSink(int maximumCount)
    {
        MaximumCount = Guard.ArgumentPositive(maximumCount, "maximumCount");
        Entries = new List<DiagnosticEntry>();
        Truncated = false;
    }

    internal bool Add(DiagnosticLevel level, string? code, string? message, string? location)
    {
        if (level < DiagnosticLevel.Information || level > DiagnosticLevel.Error)
        {
            throw new ArgumentOutOfRangeException("level", level, "The diagnostic level is not supported.");
        }

        string checkedCode = Guard.ArgumentNotWhiteSpace(code, "code");
        string checkedMessage = Guard.ArgumentNotWhiteSpace(message, "message");
        if (Entries.Count >= MaximumCount)
        {
            Truncated = true;
            return false;
        }

        Entries.Add(new DiagnosticEntry(level, checkedCode, checkedMessage, location ?? string.Empty));
        return true;
    }

    internal int GetCount()
    {
        return Entries.Count;
    }

    internal bool HasErrors()
    {
        int index = 0;
        while (index < Entries.Count)
        {
            if (Entries[index].Level == DiagnosticLevel.Error)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool WasTruncated()
    {
        return Truncated;
    }

    internal DiagnosticEntry[] CreateSnapshot()
    {
        DiagnosticEntry[] result = new DiagnosticEntry[Entries.Count];
        int index = 0;
        while (index < Entries.Count)
        {
            DiagnosticEntry entry = Entries[index];
            result[index] = new DiagnosticEntry(entry.Level, entry.Code, entry.Message, entry.Location);
            index++;
        }

        return result;
    }

    internal void Clear()
    {
        Entries.Clear();
        Truncated = false;
    }
}
