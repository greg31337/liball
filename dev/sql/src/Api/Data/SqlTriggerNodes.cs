using System;

namespace sql;

/// <summary>Represents a trigger definition node in the SQL syntax tree.</summary>
public class SqlTriggerDefinition
{
    /// <summary>Indicates whether temporary.</summary>
    public bool IsTemporary;
    /// <summary>Indicates whether an <c>IF NOT EXISTS</c> clause was present.</summary>
    public bool IfNotExists;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The timing associated with this value.</summary>
    public SqlTriggerTiming Timing;
    /// <summary>The event associated with this value.</summary>
    public SqlTriggerEvent Event;
    /// <summary>The update columns in source or insertion order.</summary>
    public SqlName[] UpdateColumns;
    /// <summary>The target name associated with this value.</summary>
    public SqlName TargetName;
    /// <summary>The for each row associated with this value.</summary>
    public bool ForEachRow;
    /// <summary>The when expression associated with this value.</summary>
    public SqlExpression? WhenExpression;
    /// <summary>The body in source or insertion order.</summary>
    public SqlStatement[] Body;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlTriggerDefinition"/> from the supplied values.</summary>
    /// <param name="isTemporary">The is temporary to use.</param>
    /// <param name="ifNotExists">The if not exists to use.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="timing">The timing to use.</param>
    /// <param name="triggerEvent">The trigger event to use.</param>
    /// <param name="updateColumns">The update columns to use.</param>
    /// <param name="targetName">The target name to use.</param>
    /// <param name="forEachRow">The for each row to use.</param>
    /// <param name="whenExpression">The when expression to use.</param>
    /// <param name="body">The body to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">targetName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlTriggerDefinition(bool isTemporary, bool ifNotExists, SqlName name, SqlTriggerTiming timing, SqlTriggerEvent triggerEvent, SqlName[] updateColumns, SqlName targetName, bool forEachRow, SqlExpression? whenExpression, SqlStatement[] body, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        ValidateNames("updateColumns", updateColumns);
        if (targetName == null)
        {
            throw new ArgumentNullException("targetName");
        }

        ValidateBody(body);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IsTemporary = isTemporary;
        IfNotExists = ifNotExists;
        Name = name;
        Timing = timing;
        Event = triggerEvent;
        UpdateColumns = updateColumns;
        TargetName = targetName;
        ForEachRow = forEachRow;
        WhenExpression = whenExpression;
        Body = body;
        Span = span;
    }

    /// <summary>Returns whether update columns is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasUpdateColumns()
    {
        return UpdateColumns.Length > 0;
    }

    /// <summary>Returns the number of update column items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetUpdateColumnCount()
    {
        return UpdateColumns.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting name.</returns>
    internal SqlName GetUpdateColumnAt(int index)
    {
        return UpdateColumns[index];
    }

    /// <summary>Returns whether when expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasWhenExpression()
    {
        return WhenExpression != null;
    }

    /// <summary>Returns whether body is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasBody()
    {
        return Body.Length > 0;
    }

    /// <summary>Returns the number of body statement items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetBodyStatementCount()
    {
        return Body.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting statement.</returns>
    internal SqlStatement GetBodyStatementAt(int index)
    {
        return Body[index];
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

    internal static void ValidateBody(SqlStatement[] body)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        int index = 0;
        while (index < body.Length)
        {
            if (body[index] == null)
            {
                throw new ArgumentNullException("body");
            }

            index = index + 1;
        }
    }
}
