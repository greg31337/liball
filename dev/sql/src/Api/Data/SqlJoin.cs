using System;

namespace sql;

/// <summary>Represents a join node in the SQL syntax tree.</summary>
public class SqlJoin
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlJoinKind Kind;
    /// <summary>Indicates whether natural.</summary>
    public bool IsNatural;
    /// <summary>Indicates whether outer.</summary>
    public bool IsOuter;
    /// <summary>The right associated with this value.</summary>
    public SqlTableReference Right;
    /// <summary>The constraint kind associated with this value.</summary>
    public SqlJoinConstraintKind ConstraintKind;
    /// <summary>The on expression associated with this value.</summary>
    public SqlExpression? OnExpression;
    /// <summary>The using columns in source or insertion order.</summary>
    public SqlName[] UsingColumns;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The explicitly written join modifiers in source order.</summary>
    public SqlJoinModifierKind[] Modifiers;
    /// <summary>Preserved unsupported join modifiers or constraints.</summary>
    public SqlUnsupportedQueryClause[] UnsupportedClauses;

    /// <summary>Initializes a new instance of <see cref="SqlJoin"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="isNatural">The is natural to use.</param>
    /// <param name="isOuter">The is outer to use.</param>
    /// <param name="right">The right to use.</param>
    /// <param name="constraintKind">The constraint kind to use.</param>
    /// <param name="onExpression">The on expression to use.</param>
    /// <param name="usingColumns">The using columns to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">right is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlJoin(SqlJoinKind kind, bool isNatural, bool isOuter, SqlTableReference right, SqlJoinConstraintKind constraintKind, SqlExpression? onExpression, SqlName[] usingColumns, SqlTextSpan span)
    {
        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        ValidateUsingColumns(usingColumns);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Kind = kind;
        IsNatural = isNatural;
        IsOuter = isOuter;
        Right = right;
        ConstraintKind = constraintKind;
        OnExpression = onExpression;
        UsingColumns = usingColumns;
        Span = span;
        Modifiers = new SqlJoinModifierKind[0];
        UnsupportedClauses = new SqlUnsupportedQueryClause[0];
    }

    /// <summary>Returns whether natural join.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsNaturalJoin()
    {
        return IsNatural || Kind == SqlJoinKind.Natural;
    }

    /// <summary>Returns whether on expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasOnExpression()
    {
        return OnExpression != null;
    }

    /// <summary>Returns whether using columns is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasUsingColumns()
    {
        return UsingColumns.Length > 0;
    }

    /// <summary>Returns the number of using column items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetUsingColumnCount()
    {
        return UsingColumns.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting name.</returns>
    internal SqlName GetUsingColumnAt(int index)
    {
        return UsingColumns[index];
    }

    /// <summary>Returns whether comma join.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsCommaJoin()
    {
        return Kind == SqlJoinKind.Comma;
    }

    internal static void ValidateUsingColumns(SqlName[] usingColumns)
    {
        if (usingColumns == null)
        {
            throw new ArgumentNullException("usingColumns");
        }

        int index = 0;
        while (index < usingColumns.Length)
        {
            if (usingColumns[index] == null)
            {
                throw new ArgumentNullException("usingColumns");
            }

            index = index + 1;
        }
    }
}
