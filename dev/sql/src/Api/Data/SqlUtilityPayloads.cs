using System;

namespace sql;

/// <summary>Represents the target of a rollback-to-savepoint clause.</summary>
public class SqlRollbackTarget
{
    /// <summary>Indicates whether the optional <c>SAVEPOINT</c> keyword was present.</summary>
    public bool HasSavepointKeyword;
    /// <summary>The savepoint name.</summary>
    public SqlName Name;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlRollbackTarget"/> from the supplied values.</summary>
    /// <param name="hasSavepointKeyword">Whether the optional keyword was present.</param>
    /// <param name="name">The savepoint name.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name or span is <c>null</c>.</exception>
    public SqlRollbackTarget(bool hasSavepointKeyword, SqlName name, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        HasSavepointKeyword = hasSavepointKeyword;
        Name = name;
        Span = span;
    }
}
