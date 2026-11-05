using System;
using System.Collections.Generic;

namespace template;

internal class TemplateSourceMapState
{
    internal List<TemplateSourceMapEntry> Entries;
    internal int OutputLength;

    internal TemplateSourceMapState()
    {
        Entries = new List<TemplateSourceMapEntry>();
        OutputLength = 0;
    }

    internal void Reset()
    {
        Entries.Clear();
        OutputLength = 0;
    }

    internal void Record(string text, TemplateSourceSpan sourceSpan)
    {
        if (text == "")
        {
            return;
        }

        TemplateSourceMapEntry entry = new TemplateSourceMapEntry(OutputLength, text.Length, sourceSpan);
        Entries.Add(entry);
        OutputLength = OutputLength + text.Length;
    }

    internal void TrimEnd(int length)
    {
        if (length <= 0)
        {
            return;
        }

        int remaining = length;
        while (remaining > 0 && Entries.Count > 0)
        {
            int index = Entries.Count - 1;
            TemplateSourceMapEntry entry = Entries[index];
            if (remaining >= entry.OutputLength)
            {
                remaining = remaining - entry.OutputLength;
                OutputLength = OutputLength - entry.OutputLength;
                Entries.RemoveAt(index);
            }
            else
            {
                entry.OutputLength = entry.OutputLength - remaining;
                entry.OutputEnd = entry.OutputStart + entry.OutputLength;
                OutputLength = OutputLength - remaining;
                remaining = 0;
            }
        }
    }

    internal void FinishNode(int outputStart, string text, TemplateSourceSpan sourceSpan)
    {
        int recordedLength = OutputLength - outputStart;
        if (recordedLength == text.Length)
        {
            return;
        }

        TrimEnd(recordedLength);
        Record(text, sourceSpan);
    }

    internal void TransformOutput(string originalText, string transformedText, TemplateSourceSpan fallbackSpan)
    {
        if (originalText == transformedText)
        {
            return;
        }

        List<TemplateSourceMapEntry> originalEntries = Entries;
        Entries = new List<TemplateSourceMapEntry>();
        OutputLength = 0;
        int originalIndex = 0;
        int entryIndex = 0;
        int transformedIndex = 0;
        while (transformedIndex < transformedText.Length)
        {
            int matchedIndex = originalIndex;
            int mappedLength = 1;
            if (transformedText[transformedIndex] == '\n' && originalIndex < originalText.Length && originalText[originalIndex] == '\r')
            {
                matchedIndex = originalIndex;
                originalIndex = originalIndex + 1;
                if (originalIndex < originalText.Length && originalText[originalIndex] == '\n')
                {
                    originalIndex = originalIndex + 1;
                }
            }
            else if (transformedText[transformedIndex] == '\r' && transformedIndex + 1 < transformedText.Length && transformedText[transformedIndex + 1] == '\n' && originalIndex < originalText.Length && (originalText[originalIndex] == '\r' || originalText[originalIndex] == '\n'))
            {
                matchedIndex = originalIndex;
                mappedLength = 2;
                if (originalText[originalIndex] == '\r' && originalIndex + 1 < originalText.Length && originalText[originalIndex + 1] == '\n')
                {
                    originalIndex = originalIndex + 2;
                }
                else
                {
                    originalIndex = originalIndex + 1;
                }
            }
            else
            {
                while (matchedIndex < originalText.Length && originalText[matchedIndex] != transformedText[transformedIndex])
                {
                    matchedIndex = matchedIndex + 1;
                }

                if (matchedIndex < originalText.Length)
                {
                    originalIndex = matchedIndex + 1;
                }
                else
                {
                    matchedIndex = originalIndex;
                }
            }

            while (entryIndex + 1 < originalEntries.Count && matchedIndex >= originalEntries[entryIndex].OutputEnd)
            {
                entryIndex = entryIndex + 1;
            }

            TemplateSourceSpan sourceSpan = fallbackSpan;
            if (entryIndex < originalEntries.Count && matchedIndex >= originalEntries[entryIndex].OutputStart && matchedIndex < originalEntries[entryIndex].OutputEnd)
            {
                sourceSpan = originalEntries[entryIndex].SourceSpan;
            }

            RecordLength(mappedLength, sourceSpan);
            transformedIndex = transformedIndex + mappedLength;
        }
    }

    internal void RecordLength(int length, TemplateSourceSpan sourceSpan)
    {
        if (length <= 0)
        {
            return;
        }

        if (Entries.Count > 0)
        {
            TemplateSourceMapEntry previous = Entries[Entries.Count - 1];
            if (previous.SourceSpan == sourceSpan && previous.OutputEnd == OutputLength)
            {
                previous.OutputLength = previous.OutputLength + length;
                previous.OutputEnd = previous.OutputEnd + length;
                OutputLength = OutputLength + length;
                return;
            }
        }

        Entries.Add(new TemplateSourceMapEntry(OutputLength, length, sourceSpan));
        OutputLength = OutputLength + length;
    }

    internal List<TemplateSourceMapEntry> CopyEntries()
    {
        List<TemplateSourceMapEntry> copy = new List<TemplateSourceMapEntry>();
        int index = 0;
        while (index < Entries.Count)
        {
            TemplateSourceMapEntry entry = Entries[index];
            copy.Add(new TemplateSourceMapEntry(entry.OutputStart, entry.OutputLength, entry.SourceSpan));
            index = index + 1;
        }

        return copy;
    }
}
