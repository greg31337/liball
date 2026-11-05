using System;
using System.Collections.Generic;
using System.Text;

namespace config;

/// <summary>Provides delimiter-aware operations for constructing and comparing configuration paths.</summary>
/// <remarks>Empty segments are omitted. A backslash escapes the following character, allowing delimiters and backslashes inside a segment.</remarks>
public class ConfigPath : IConfigPath
{
    internal string[] Segments;
    internal string Delimiter;

    /// <summary>Creates a path-operations instance using no retained parsed path.</summary>
    public ConfigPath()
    {
        Segments = new string[0];
        Delimiter = ".";
    }

    /// <summary>Returns the effective delimiter, using <c>.</c> when the supplied value is empty.</summary>
    /// <param name="delimiter">The requested delimiter.</param>
    /// <returns>The requested delimiter, or <c>.</c> when it is empty.</returns>
    public string Clean(string delimiter)
    {
        return CleanDelimiter(delimiter);
    }

    /// <summary>Escapes backslashes and delimiter occurrences inside one literal segment.</summary>
    /// <param name="segment">The literal, unescaped segment.</param>
    /// <param name="delimiter">The path delimiter; an empty string selects <c>.</c>.</param>
    /// <returns>The escaped segment text.</returns>
    public string Escape(string segment, string delimiter)
    {
        return EscapeSegment(segment, delimiter);
    }

    /// <summary>Parses a path into unescaped nonempty segments.</summary>
    /// <param name="path">The path text to parse.</param>
    /// <param name="delimiter">The path delimiter; an empty string selects <c>.</c>.</param>
    /// <returns>A new array containing the parsed segments.</returns>
    public string[] ParseSegments(string path, string delimiter)
    {
        ConfigPath parsed = Parse(path, delimiter);
        return CopySegments(parsed.Segments);
    }

    /// <summary>Parses and rejoins a path into its canonical escaped form.</summary>
    /// <param name="path">The path text to normalize.</param>
    /// <param name="delimiter">The path delimiter; an empty string selects <c>.</c>.</param>
    /// <returns>The normalized path.</returns>
    public string NormalizePath(string path, string delimiter)
    {
        return Normalize(path, delimiter);
    }

    /// <summary>Escapes and joins nonempty literal path segments.</summary>
    /// <param name="segments">The literal, unescaped path segments.</param>
    /// <param name="delimiter">The path delimiter; an empty string selects <c>.</c>.</param>
    /// <returns>A path that parses back into the supplied nonempty segments.</returns>
    public string JoinPath(string[] segments, string delimiter)
    {
        return JoinSegments(segments, delimiter);
    }

    /// <summary>Compares two paths segment by segment after parsing and delimiter cleanup.</summary>
    /// <param name="leftPath">The first path.</param>
    /// <param name="rightPath">The second path.</param>
    /// <param name="delimiter">The path delimiter; an empty string selects <c>.</c>.</param>
    /// <param name="caseMode">Whether segment casing is significant.</param>
    /// <returns>A negative value when the first path sorts earlier, zero when equivalent, or a positive value when it sorts later.</returns>
    public int Compare(string leftPath, string rightPath, string delimiter, ConfigCaseMode caseMode)
    {
        ConfigPath left = Parse(leftPath, delimiter);
        ConfigPath right = Parse(rightPath, delimiter);
        StringComparer comparer = StringComparer.Ordinal;
        if (caseMode == ConfigCaseMode.Insensitive)
        {
            comparer = StringComparer.OrdinalIgnoreCase;
        }

        int length = left.Count();
        if (right.Count() < length)
        {
            length = right.Count();
        }

        int index = 0;
        while (index < length)
        {
            int comparison = comparer.Compare(left.Segment(index), right.Segment(index));
            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        return left.Count().CompareTo(right.Count());
    }

    internal static ConfigPath ParseDefault(string path)
    {
        return Parse(path, ".");
    }

    internal static ConfigPath Parse(string path, string delimiter)
    {
        string cleanDelimiter = CleanDelimiter(delimiter);
        List<string> segments = new List<string>();
        StringBuilder current = new StringBuilder();

        int index = 0;
        while (index < path.Length)
        {
            if (path[index] == '\\')
            {
                if (index + 1 < path.Length)
                {
                    current.Append(path[index + 1]);
                    index = index + 2;
                    continue;
                }

                current.Append(path[index]);
                index = index + 1;
                continue;
            }

            if (IsDelimiterAt(path, cleanDelimiter, index))
            {
                AddSegment(segments, current.ToString());
                current.Clear();
                index = index + cleanDelimiter.Length;
                continue;
            }

            current.Append(path[index]);
            index = index + 1;
        }

        AddSegment(segments, current.ToString());
        ConfigPath parsed = new ConfigPath();
        parsed.Segments = segments.ToArray();
        parsed.Delimiter = cleanDelimiter;
        return parsed;
    }

    internal static string Normalize(string path, string delimiter)
    {
        ConfigPath parsed = Parse(path, delimiter);
        return parsed.Join();
    }

    internal static string JoinSegments(string[] segments, string delimiter)
    {
        string cleanDelimiter = CleanDelimiter(delimiter);
        StringBuilder builder = new StringBuilder();

        int index = 0;
        while (index < segments.Length)
        {
            if (segments[index].Length > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Append(cleanDelimiter);
                }

                builder.Append(EscapeSegment(segments[index], cleanDelimiter));
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal string Join()
    {
        return JoinSegments(Segments, Delimiter);
    }

    internal int Count()
    {
        return Segments.Length;
    }

    internal bool IsEmpty()
    {
        return Segments.Length == 0;
    }

    internal string Segment(int index)
    {
        if (index < 0)
        {
            return "";
        }

        if (index >= Segments.Length)
        {
            return "";
        }

        return Segments[index];
    }

    internal static string EscapeSegment(string segment, string delimiter)
    {
        string cleanDelimiter = CleanDelimiter(delimiter);
        StringBuilder builder = new StringBuilder();

        int index = 0;
        while (index < segment.Length)
        {
            if (segment[index] == '\\')
            {
                builder.Append("\\\\");
                index = index + 1;
                continue;
            }

            if (IsDelimiterAt(segment, cleanDelimiter, index))
            {
                builder.Append("\\");
                builder.Append(segment[index]);
                index = index + 1;
                continue;
            }

            builder.Append(segment[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string CleanDelimiter(string delimiter)
    {
        if (delimiter.Length == 0)
        {
            return ".";
        }

        return delimiter;
    }

    internal static void AddSegment(List<string> segments, string segment)
    {
        if (segment.Length == 0)
        {
            return;
        }

        segments.Add(segment);
    }

    internal static bool IsDelimiterAt(string text, string delimiter, int index)
    {
        if (delimiter.Length == 0)
        {
            return false;
        }

        if (index < 0 || index > text.Length)
        {
            return false;
        }

        if (delimiter.Length > text.Length - index)
        {
            return false;
        }

        int offset = 0;
        while (offset < delimiter.Length)
        {
            if (text[index + offset] != delimiter[offset])
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }

    internal static string[] CopySegments(string[] segments)
    {
        string[] copy = new string[segments.Length];
        int index = 0;
        while (index < segments.Length)
        {
            copy[index] = segments[index];
            index = index + 1;
        }

        return copy;
    }
}
