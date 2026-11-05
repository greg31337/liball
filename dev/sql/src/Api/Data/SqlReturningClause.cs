using System;

namespace sql;

/// <summary>Represents a returning clause node in the SQL syntax tree.</summary>
public class SqlReturningClause
{
    /// <summary>The result columns in source or insertion order.</summary>
    public SqlResultColumn[] ResultColumns;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlReturningClause"/> from the supplied values.</summary>
    /// <param name="resultColumns">The result columns to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlReturningClause(SqlResultColumn[] resultColumns, SqlTextSpan span)
    {
        ValidateResultColumns(resultColumns);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        ResultColumns = resultColumns;
        Span = span;
    }

    /// <summary>Returns whether result columns is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasResultColumns()
    {
        return ResultColumns.Length > 0;
    }

    /// <summary>Returns the number of result column items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetResultColumnCount()
    {
        return ResultColumns.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting result column.</returns>
    internal SqlResultColumn GetResultColumnAt(int index)
    {
        return ResultColumns[index];
    }

    internal static void ValidateResultColumns(SqlResultColumn[] resultColumns)
    {
        if (resultColumns == null)
        {
            throw new ArgumentNullException("resultColumns");
        }

        int index = 0;
        while (index < resultColumns.Length)
        {
            if (resultColumns[index] == null)
            {
                throw new ArgumentNullException("resultColumns");
            }

            index = index + 1;
        }
    }
}
