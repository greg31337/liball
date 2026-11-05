using System;

namespace sql;

/// <summary>Represents a select core node in the SQL syntax tree.</summary>
public class SqlSelectCore
{
    /// <summary>Indicates whether distinct.</summary>
    public bool IsDistinct;
    /// <summary>Indicates whether all.</summary>
    public bool IsAll;
    /// <summary>The result columns in source or insertion order.</summary>
    public SqlResultColumn[] ResultColumns;
    /// <summary>The from in source or insertion order.</summary>
    public SqlTableReference[] From;
    /// <summary>The where associated with this value.</summary>
    public SqlExpression? Where;
    /// <summary>The group by in source or insertion order.</summary>
    public SqlExpression[] GroupBy;
    /// <summary>The having associated with this value.</summary>
    public SqlExpression? Having;
    /// <summary>The windows in source or insertion order.</summary>
    public SqlWindowDefinition[] Windows;
    /// <summary>Indicates whether values.</summary>
    public bool IsValues;
    /// <summary>The values in source or insertion order.</summary>
    public SqlExpression[][] Values;
    /// <summary>The optional SQL Server-style <c>TOP</c> clause.</summary>
    public SqlTopClause? Top;
    /// <summary>Preserved dialect-specific clauses attached to this query core.</summary>
    public SqlUnsupportedQueryClause[] UnsupportedClauses;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlSelectCore"/> from the supplied values.</summary>
    /// <param name="resultColumns">The result columns to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlSelectCore Create(SqlResultColumn[] resultColumns, SqlTextSpan span)
    {
        return new SqlSelectCore(false, false, resultColumns, new SqlTableReference[0], null, new SqlExpression[0], null, new SqlWindowDefinition[0], span);
    }

    /// <summary>Creates a select core from all select-core clauses.</summary>
    /// <param name="isDistinct">The distinct flag to use.</param>
    /// <param name="isAll">The all flag to use.</param>
    /// <param name="resultColumns">The result columns to use.</param>
    /// <param name="from">The table references to use.</param>
    /// <param name="where">The optional predicate to use.</param>
    /// <param name="groupBy">The grouping expressions to use.</param>
    /// <param name="having">The optional grouping predicate to use.</param>
    /// <param name="windows">The window definitions to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting select core.</returns>
    internal static SqlSelectCore Create(bool isDistinct, bool isAll, SqlResultColumn[] resultColumns, SqlTableReference[] from, SqlExpression? where, SqlExpression[] groupBy, SqlExpression? having, SqlWindowDefinition[] windows, SqlTextSpan span)
    {
        return new SqlSelectCore(isDistinct, isAll, resultColumns, from, where, groupBy, having, windows, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlSelectCore"/> from the supplied values.</summary>
    /// <param name="isDistinct">The is distinct to use.</param>
    /// <param name="isAll">The is all to use.</param>
    /// <param name="resultColumns">The result columns to use.</param>
    /// <param name="from">The from to use.</param>
    /// <param name="where">The where to use.</param>
    /// <param name="groupBy">The group by to use.</param>
    /// <param name="having">The having to use.</param>
    /// <param name="windows">The windows to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlSelectCore(bool isDistinct, bool isAll, SqlResultColumn[] resultColumns, SqlTableReference[] from, SqlExpression? where, SqlExpression[] groupBy, SqlExpression? having, SqlWindowDefinition[] windows, SqlTextSpan span)
    {
        ValidateResultColumns(resultColumns);
        ValidateTableReferences(from);
        ValidateExpressions("groupBy", groupBy);
        ValidateWindows(windows);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IsDistinct = isDistinct;
        IsAll = isAll;
        ResultColumns = resultColumns;
        From = from;
        Where = where;
        GroupBy = groupBy;
        Having = having;
        Windows = windows;
        IsValues = false;
        Values = new SqlExpression[0][];
        Top = null;
        UnsupportedClauses = new SqlUnsupportedQueryClause[0];
        Span = span;
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

    /// <summary>Returns whether from is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasFrom()
    {
        return From.Length > 0;
    }

    /// <summary>Returns the number of from items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetFromCount()
    {
        return From.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting table reference.</returns>
    internal SqlTableReference GetFromAt(int index)
    {
        return From[index];
    }

    /// <summary>Returns whether where is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasWhere()
    {
        return Where != null;
    }

    /// <summary>Returns whether group by is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasGroupBy()
    {
        return GroupBy.Length > 0;
    }

    /// <summary>Returns the number of group by items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetGroupByCount()
    {
        return GroupBy.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting expression.</returns>
    internal SqlExpression GetGroupByAt(int index)
    {
        return GroupBy[index];
    }

    /// <summary>Returns whether having is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasHaving()
    {
        return Having != null;
    }

    /// <summary>Returns whether windows is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasWindows()
    {
        return Windows.Length > 0;
    }

    /// <summary>Returns the number of window items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetWindowCount()
    {
        return Windows.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting window definition.</returns>
    internal SqlWindowDefinition GetWindowAt(int index)
    {
        return Windows[index];
    }

    /// <summary>Returns whether values is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasValues()
    {
        return Values.Length > 0;
    }

    /// <summary>Returns the number of value row items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetValueRowCount()
    {
        return Values.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting expression.</returns>
    internal SqlExpression[] GetValueRowAt(int index)
    {
        return Values[index];
    }

    internal static void ValidateResultColumns(SqlResultColumn[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("resultColumns");
        }

        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentNullException("resultColumns");
            }

            index = index + 1;
        }
    }

    internal static void ValidateTableReferences(SqlTableReference[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("from");
        }

        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentNullException("from");
            }

            index = index + 1;
        }
    }

    internal static void ValidateExpressions(string parameterName, SqlExpression[] expressions)
    {
        if (expressions == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < expressions.Length)
        {
            if (expressions[index] == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            index = index + 1;
        }
    }

    internal static void ValidateWindows(SqlWindowDefinition[] windows)
    {
        if (windows == null)
        {
            throw new ArgumentNullException("windows");
        }

        int index = 0;
        while (index < windows.Length)
        {
            if (windows[index] == null)
            {
                throw new ArgumentNullException("windows");
            }

            index = index + 1;
        }
    }
}

/// <summary>Represents an ordering term node in the SQL syntax tree.</summary>
public class SqlOrderingTerm
{
    /// <summary>The expression, or <c>null</c> when this syntax form has no expression.</summary>
    public SqlExpression Expression;
    /// <summary>The collation associated with this value.</summary>
    public SqlName? Collation;
    /// <summary>The direction associated with this value.</summary>
    public SqlOrderingDirection Direction;
    /// <summary>The null ordering associated with this value.</summary>
    public SqlNullOrdering NullOrdering;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlOrderingTerm"/> from the supplied values.</summary>
    /// <param name="expression">The expression syntax tree to process.</param>
    /// <param name="collation">The collation to use.</param>
    /// <param name="direction">The direction to use.</param>
    /// <param name="nullOrdering">The null ordering to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">expression is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlOrderingTerm(SqlExpression expression, SqlName? collation, SqlOrderingDirection direction, SqlNullOrdering nullOrdering, SqlTextSpan span)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Expression = expression;
        Collation = collation;
        Direction = direction;
        NullOrdering = nullOrdering;
        Span = span;
    }

    /// <summary>Returns whether collation is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasCollation()
    {
        return Collation != null;
    }
}

/// <summary>Represents a limit clause node in the SQL syntax tree.</summary>
public class SqlLimitClause
{
    /// <summary>The limit expression associated with this value.</summary>
    public SqlExpression LimitExpression;
    /// <summary>The offset expression associated with this value.</summary>
    public SqlExpression? OffsetExpression;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlLimitClause"/> from the supplied values.</summary>
    /// <param name="limitExpression">The limit expression to use.</param>
    /// <param name="offsetExpression">The offset expression to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">limitExpression is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlLimitClause(SqlExpression limitExpression, SqlExpression? offsetExpression, SqlTextSpan span)
    {
        if (limitExpression == null)
        {
            throw new ArgumentNullException("limitExpression");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        LimitExpression = limitExpression;
        OffsetExpression = offsetExpression;
        Span = span;
    }

    /// <summary>Returns whether offset is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasOffset()
    {
        return OffsetExpression != null;
    }
}

/// <summary>Represents a window definition node in the SQL syntax tree.</summary>
public class SqlWindowDefinition
{
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName? Name;
    /// <summary>The base window name associated with this value.</summary>
    public SqlName? BaseWindowName;
    /// <summary>The partition by in source or insertion order.</summary>
    public SqlExpression[] PartitionBy;
    /// <summary>The order by in source or insertion order.</summary>
    public SqlOrderingTerm[] OrderBy;
    /// <summary>The frame associated with this value.</summary>
    public SqlWindowFrame? Frame;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlWindowDefinition"/> from the supplied values.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="baseWindowName">The base window name to use.</param>
    /// <param name="partitionBy">The partition by to use.</param>
    /// <param name="orderBy">The order by to use.</param>
    /// <param name="frame">The frame to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlWindowDefinition(SqlName? name, SqlName? baseWindowName, SqlExpression[] partitionBy, SqlOrderingTerm[] orderBy, SqlWindowFrame? frame, SqlTextSpan span)
    {
        ValidateExpressions("partitionBy", partitionBy);
        ValidateOrderBy(orderBy);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Name = name;
        BaseWindowName = baseWindowName;
        PartitionBy = partitionBy;
        OrderBy = orderBy;
        Frame = frame;
        Span = span;
    }

    /// <summary>Returns whether name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasName()
    {
        return Name != null;
    }

    /// <summary>Returns whether base window name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasBaseWindowName()
    {
        return BaseWindowName != null;
    }

    /// <summary>Returns whether partition by is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasPartitionBy()
    {
        return PartitionBy.Length > 0;
    }

    /// <summary>Returns the number of partition by items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetPartitionByCount()
    {
        return PartitionBy.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting expression.</returns>
    internal SqlExpression GetPartitionByAt(int index)
    {
        return PartitionBy[index];
    }

    /// <summary>Returns whether order by is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasOrderBy()
    {
        return OrderBy.Length > 0;
    }

    /// <summary>Returns the number of order by items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetOrderByCount()
    {
        return OrderBy.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting ordering term.</returns>
    internal SqlOrderingTerm GetOrderByAt(int index)
    {
        return OrderBy[index];
    }

    /// <summary>Returns whether frame is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasFrame()
    {
        return Frame != null;
    }

    internal static void ValidateExpressions(string parameterName, SqlExpression[] expressions)
    {
        if (expressions == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < expressions.Length)
        {
            if (expressions[index] == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            index = index + 1;
        }
    }

    internal static void ValidateOrderBy(SqlOrderingTerm[] orderBy)
    {
        if (orderBy == null)
        {
            throw new ArgumentNullException("orderBy");
        }

        int index = 0;
        while (index < orderBy.Length)
        {
            if (orderBy[index] == null)
            {
                throw new ArgumentNullException("orderBy");
            }

            index = index + 1;
        }
    }
}

/// <summary>Represents a window frame node in the SQL syntax tree.</summary>
public class SqlWindowFrame
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlWindowFrameKind Kind;
    /// <summary>The start associated with this value.</summary>
    public SqlWindowFrameBoundary Start;
    /// <summary>The end associated with this value.</summary>
    public SqlWindowFrameBoundary? End;
    /// <summary>The exclusion associated with this value.</summary>
    public SqlWindowFrameExclusionKind Exclusion;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlWindowFrame"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="start">The zero-based start position.</param>
    /// <param name="end">The zero-based exclusive end position.</param>
    /// <param name="exclusion">The exclusion to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">start is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlWindowFrame(SqlWindowFrameKind kind, SqlWindowFrameBoundary start, SqlWindowFrameBoundary? end, SqlWindowFrameExclusionKind exclusion, SqlTextSpan span)
    {
        if (start == null)
        {
            throw new ArgumentNullException("start");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Kind = kind;
        Start = start;
        End = end;
        Exclusion = exclusion;
        Span = span;
    }

    /// <summary>Returns whether end is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasEnd()
    {
        return End != null;
    }

    /// <summary>Returns whether exclusion is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasExclusion()
    {
        return Exclusion != SqlWindowFrameExclusionKind.None;
    }
}

/// <summary>Represents a window frame boundary node in the SQL syntax tree.</summary>
public class SqlWindowFrameBoundary
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlWindowFrameBoundaryKind Kind;
    /// <summary>The expression, or <c>null</c> when this syntax form has no expression.</summary>
    public SqlExpression? Expression;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlWindowFrameBoundary"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="expression">The expression syntax tree to process.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind kind, SqlExpression? expression, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Kind = kind;
        Expression = expression;
        Span = span;
    }

    /// <summary>Returns whether expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasExpression()
    {
        return Expression != null;
    }
}
