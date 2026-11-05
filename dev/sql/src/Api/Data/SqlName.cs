using System;

namespace sql;

/// <summary>Represents a name node in the SQL syntax tree.</summary>
public class SqlName
{
    /// <summary>The parts in source or insertion order.</summary>
    public SqlNamePart[] Parts;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlName"/> from the supplied values.</summary>
    /// <param name="parts">The parts to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlName(SqlNamePart[] parts, SqlTextSpan span)
    {
        ValidateParts(parts);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Parts = parts;
        Span = span;
    }

    /// <summary>Returns the number of part items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetPartCount()
    {
        return Parts.Length;
    }

    /// <summary>Returns whether this value contains no elements or source characters.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEmpty()
    {
        return Parts.Length == 0;
    }

    /// <summary>Returns whether qualifier is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasQualifier()
    {
        return Parts.Length > 1;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting name part.</returns>
    internal SqlNamePart GetPartAt(int index)
    {
        return Parts[index];
    }

    /// <summary>Returns the last part associated with this value.</summary>
    /// <returns>The resulting name part.</returns>
    internal SqlNamePart GetLastPart()
    {
        return Parts[Parts.Length - 1];
    }

    /// <summary>Returns the source text covered by the supplied span.</summary>
    /// <returns>The resulting string.</returns>
    internal string GetText()
    {
        string text = "";
        int index = 0;
        while (index < Parts.Length)
        {
            if (index > 0)
            {
                text = text + ".";
            }

            text = text + Parts[index].Text;
            index = index + 1;
        }

        return text;
    }

    /// <summary>Returns the source text associated with this value.</summary>
    /// <returns>The resulting string.</returns>
    internal string GetSourceText()
    {
        string text = "";
        int index = 0;
        while (index < Parts.Length)
        {
            if (index > 0)
            {
                text = text + ".";
            }

            text = text + Parts[index].SourceText;
            index = index + 1;
        }

        return text;
    }

    internal static void ValidateParts(SqlNamePart[] parts)
    {
        if (parts == null)
        {
            throw new ArgumentNullException("parts");
        }

        int index = 0;
        while (index < parts.Length)
        {
            if (parts[index] == null)
            {
                throw new ArgumentNullException("parts");
            }

            index = index + 1;
        }
    }
}
