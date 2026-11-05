using System;

namespace sql;

/// <summary>Represents one SQL statement with a kind tag and the payload used by that statement kind.</summary>
/// <remarks>Inspect <see cref="Kind"/> to determine which explicitly typed payload field is populated.</remarks>
public class SqlStatement
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlStatementKind Kind;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The tokens in source or insertion order.</summary>
    public SqlToken[] Tokens;
    /// <summary>The range of this statement within its script token array.</summary>
    public SqlTokenRange TokenRange;
    /// <summary>The leading trivia immediately before this statement.</summary>
    public SqlTrivia[] LeadingTrivia;
    /// <summary>The trailing trivia immediately after this statement.</summary>
    public SqlTrivia[] TrailingTrivia;
    /// <summary>The statement terminator, or <c>null</c> when no terminator was present.</summary>
    public SqlToken? Terminator;
    /// <summary>The exact source text covered by this statement.</summary>
    public string SourceText;
    /// <summary>The quoted identifier and literal tokens in source order.</summary>
    public SqlToken[] QuotedTokens;
    /// <summary>The unsupported syntax markers in source order.</summary>
    public SqlUnsupportedSyntax[] UnsupportedSyntax;
    /// <summary>The nested query, or <c>null</c> when this syntax form has no query.</summary>
    public SqlQuery? Query;
    /// <summary>The insert payload, or <c>null</c> when this is not an insert statement.</summary>
    public SqlInsertStatement? Insert;
    /// <summary>The update payload, or <c>null</c> when this is not an update statement.</summary>
    public SqlUpdateStatement? Update;
    /// <summary>The delete payload, or <c>null</c> when this is not a delete statement.</summary>
    public SqlDeleteStatement? Delete;
    /// <summary>The merge payload, or <c>null</c> when this is not a merge statement.</summary>
    public SqlMergeStatement? Merge;
    /// <summary>The create-table payload, or <c>null</c> when this is not a create-table statement.</summary>
    public SqlTableDefinition? CreateTable;
    /// <summary>The alter-table payload, or <c>null</c> when this is not an alter-table statement.</summary>
    public SqlAlterTableDefinition? AlterTable;
    /// <summary>The drop-table payload, or <c>null</c> when this is not a drop-table statement.</summary>
    public SqlDropTableDefinition? DropTable;
    /// <summary>The create-index payload, or <c>null</c> when this is not a create-index statement.</summary>
    public SqlIndexDefinition? CreateIndex;
    /// <summary>The drop-index payload, or <c>null</c> when this is not a drop-index statement.</summary>
    public SqlDropIndexDefinition? DropIndex;
    /// <summary>The create-view payload, or <c>null</c> when this is not a create-view statement.</summary>
    public SqlViewDefinition? CreateView;
    /// <summary>The drop-view payload, or <c>null</c> when this is not a drop-view statement.</summary>
    public SqlDropViewDefinition? DropView;
    /// <summary>The create-trigger payload, or <c>null</c> when this is not a create-trigger statement.</summary>
    public SqlTriggerDefinition? CreateTrigger;
    /// <summary>The drop-trigger payload, or <c>null</c> when this is not a drop-trigger statement.</summary>
    public SqlDropTriggerDefinition? DropTrigger;
    /// <summary>The create-virtual-table payload, or <c>null</c> when this is not a create-virtual-table statement.</summary>
    public SqlVirtualTableDefinition? CreateVirtualTable;
    /// <summary>The pragma payload, or <c>null</c> when this is not a pragma statement.</summary>
    public SqlPragmaStatement? Pragma;
    /// <summary>The analyze payload, or <c>null</c> when this is not an analyze statement.</summary>
    public SqlAnalyzeStatement? Analyze;
    /// <summary>The vacuum payload, or <c>null</c> when this is not a vacuum statement.</summary>
    public SqlVacuumStatement? Vacuum;
    /// <summary>The attach payload, or <c>null</c> when this is not an attach statement.</summary>
    public SqlAttachStatement? Attach;
    /// <summary>The detach payload, or <c>null</c> when this is not a detach statement.</summary>
    public SqlDetachStatement? Detach;
    /// <summary>The reindex payload, or <c>null</c> when this is not a reindex statement.</summary>
    public SqlReindexStatement? Reindex;
    /// <summary>The transaction payload for begin, commit, or rollback, or <c>null</c> otherwise.</summary>
    public SqlTransactionStatement? Transaction;
    /// <summary>The savepoint payload, or <c>null</c> when this is not a savepoint statement.</summary>
    public SqlSavepointStatement? Savepoint;
    /// <summary>The release payload, or <c>null</c> when this is not a release statement.</summary>
    public SqlReleaseStatement? Release;
    /// <summary>The explain payload, or <c>null</c> when this is not an explain statement.</summary>
    public SqlExplainStatement? Explain;

    /// <summary>Initializes a new instance of <see cref="SqlStatement"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlStatement Create(SqlStatementKind kind, SqlTextSpan span)
    {
        return new SqlStatement(kind, span, new SqlToken[0]);
    }

    /// <summary>Creates a statement with retained tokens.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="tokens">The tokens in source order.</param>
    /// <returns>The resulting statement.</returns>
    internal static SqlStatement Create(SqlStatementKind kind, SqlTextSpan span, SqlToken[] tokens)
    {
        return new SqlStatement(kind, span, tokens);
    }

    /// <summary>Initializes a new instance of <see cref="SqlStatement"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="tokens">The tokens in source order.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlStatement(SqlStatementKind kind, SqlTextSpan span, SqlToken[] tokens)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        ValidateTokens(tokens);

        Kind = kind;
        Span = span;
        Tokens = tokens;
        TokenRange = new SqlTokenRange(0, tokens.Length);
        LeadingTrivia = new SqlTrivia[0];
        TrailingTrivia = new SqlTrivia[0];
        Terminator = null;
        SourceText = "";
        QuotedTokens = new SqlToken[0];
        UnsupportedSyntax = new SqlUnsupportedSyntax[0];
        Query = null;
        Insert = null;
        Update = null;
        Delete = null;
        Merge = null;
        CreateTable = null;
        AlterTable = null;
        DropTable = null;
        CreateIndex = null;
        DropIndex = null;
        CreateView = null;
        DropView = null;
        CreateTrigger = null;
        DropTrigger = null;
        CreateVirtualTable = null;
        Pragma = null;
        Analyze = null;
        Vacuum = null;
        Attach = null;
        Detach = null;
        Reindex = null;
        Transaction = null;
        Savepoint = null;
        Release = null;
        Explain = null;
    }

    /// <summary>Returns whether the requested kind matches this value's discriminator.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsKind(SqlStatementKind kind)
    {
        return Kind == kind;
    }

    /// <summary>Returns whether empty statement.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEmptyStatement()
    {
        return Kind == SqlStatementKind.Empty;
    }

    /// <summary>Returns whether tokens is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTokens()
    {
        return Tokens.Length > 0;
    }

    /// <summary>Returns the number of token items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetTokenCount()
    {
        return Tokens.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting token.</returns>
    internal SqlToken GetTokenAt(int index)
    {
        return Tokens[index];
    }

    internal static void ValidateTokens(SqlToken[] tokens)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == null)
            {
                throw new ArgumentNullException("tokens");
            }

            index = index + 1;
        }
    }
}
