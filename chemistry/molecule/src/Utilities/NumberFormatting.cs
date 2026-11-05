using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace molecule;

internal class NumberFormatting
{
    internal static string FormatInt(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatLong(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatDouble(double value)
    {
        return value.ToString("0.###############", CultureInfo.InvariantCulture);
    }

    internal static string FormatDoubleWithPattern(double value, string? pattern)
    {
        if (pattern == null)
        {
            return FormatDouble(value);
        }

        if (pattern == "")
        {
            return FormatDouble(value);
        }

        return value.ToString(pattern, CultureInfo.InvariantCulture);
    }

    internal static int ParseInt(string text)
    {
        return int.Parse(NormalizeString(text), CultureInfo.InvariantCulture);
    }

    internal static long ParseLong(string text)
    {
        return long.Parse(NormalizeString(text), CultureInfo.InvariantCulture);
    }

    internal static double ParseDouble(string text)
    {
        return double.Parse(NormalizeString(text), CultureInfo.InvariantCulture);
    }

    internal static string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}

internal class StableTextWriter
{
    internal StringBuilder Builder;

    internal StableTextWriter()
    {
        Builder = new StringBuilder();
    }

    internal void Append(string? text)
    {
        Builder.Append(NumberFormatting.NormalizeString(text));
    }

    internal void AppendLine(string? text)
    {
        Builder.Append(NumberFormatting.NormalizeString(text));
        Builder.Append("\n");
    }

    internal void AppendKeyValue(string? key, string? value)
    {
        Builder.Append(NumberFormatting.NormalizeString(key));
        Builder.Append("=");
        Builder.Append(NumberFormatting.NormalizeString(value));
        Builder.Append("\n");
    }

    internal void AppendInt(string key, int value)
    {
        AppendKeyValue(key, NumberFormatting.FormatInt(value));
    }

    internal void AppendLong(string key, long value)
    {
        AppendKeyValue(key, NumberFormatting.FormatLong(value));
    }

    internal void AppendDouble(string key, double value)
    {
        AppendKeyValue(key, NumberFormatting.FormatDouble(value));
    }

    internal void AppendDoubleWithPattern(string key, double value, string pattern)
    {
        AppendKeyValue(key, NumberFormatting.FormatDoubleWithPattern(value, pattern));
    }

    internal void AppendSortedFields(List<MoleculeMetadataEntry> fields)
    {
        List<MoleculeMetadataEntry> sorted = CopyFields(fields);
        SortFields(sorted);

        int index = 0;
        while (index < sorted.Count)
        {
            AppendKeyValue(sorted[index].Key, sorted[index].Value);
            index = index + 1;
        }
    }

    internal List<MoleculeMetadataEntry> CopyFields(List<MoleculeMetadataEntry>? fields)
    {
        List<MoleculeMetadataEntry> copy = new List<MoleculeMetadataEntry>();

        if (fields == null)
        {
            return copy;
        }

        int index = 0;
        while (index < fields.Count)
        {
            copy.Add(new MoleculeMetadataEntry(fields[index].Key, fields[index].Value));
            index = index + 1;
        }

        return copy;
    }

    internal void SortFields(List<MoleculeMetadataEntry> fields)
    {
        int index = 1;
        while (index < fields.Count)
        {
            MoleculeMetadataEntry value = fields[index];
            int scan = index - 1;

            while (scan >= 0 && CompareFields(fields[scan], value) > 0)
            {
                fields[scan + 1] = fields[scan];
                scan = scan - 1;
            }

            fields[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareFields(MoleculeMetadataEntry left, MoleculeMetadataEntry right)
    {
        int keyComparison = string.CompareOrdinal(left.Key, right.Key);
        if (keyComparison != 0)
        {
            return keyComparison;
        }

        return string.CompareOrdinal(left.Value, right.Value);
    }

    internal string ToText()
    {
        return Builder.ToString();
    }

    internal void Clear()
    {
        Builder.Clear();
    }
}
