using System;

namespace sql;

/// <summary>Summarizes the tables read and written by a statement and whether the result is conservative.</summary>
public class SqlStatementSummary
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlStatementKind Kind;
    /// <summary>The read tables in source or insertion order.</summary>
    public SqlName[] ReadTables;
    /// <summary>The write tables in source or insertion order.</summary>
    public SqlName[] WriteTables;
    /// <summary>The has unknown effects associated with this value.</summary>
    public bool HasUnknownEffects;

    /// <summary>Initializes a new instance of <see cref="SqlStatementSummary"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="readTables">The read tables to use.</param>
    /// <param name="writeTables">The write tables to use.</param>
    /// <param name="hasUnknownEffects">The has unknown effects to use.</param>
    public SqlStatementSummary(SqlStatementKind kind, SqlName[] readTables, SqlName[] writeTables, bool hasUnknownEffects)
    {
        ValidateNames("readTables", readTables);
        ValidateNames("writeTables", writeTables);

        Kind = kind;
        ReadTables = readTables;
        WriteTables = writeTables;
        HasUnknownEffects = hasUnknownEffects;
    }

    /// <summary>Returns whether read tables is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasReadTables()
    {
        return ReadTables.Length > 0;
    }

    /// <summary>Returns the number of read table items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetReadTableCount()
    {
        return ReadTables.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting name.</returns>
    internal SqlName GetReadTableAt(int index)
    {
        return ReadTables[index];
    }

    /// <summary>Returns whether write tables is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasWriteTables()
    {
        return WriteTables.Length > 0;
    }

    /// <summary>Returns the number of write table items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetWriteTableCount()
    {
        return WriteTables.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting name.</returns>
    internal SqlName GetWriteTableAt(int index)
    {
        return WriteTables[index];
    }

    internal static void ValidateNames(string parameterName, SqlName[] names)
    {
        if (names == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            index = index + 1;
        }
    }
}
