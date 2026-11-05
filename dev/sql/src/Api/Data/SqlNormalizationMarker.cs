using System;

namespace sql;

/// <summary>Identifies a source region that could not be represented by a lossless normalization change.</summary>
public class SqlNormalizationMarker
{
    /// <summary>The reason lossless normalization was unavailable.</summary>
    public SqlNormalizationMarkerKind Kind;
    /// <summary>A human-readable description of the unresolved change.</summary>
    public string Description;
    /// <summary>The exact source text associated with the unresolved change.</summary>
    public string SourceText;
    /// <summary>The original half-open source span associated with the unresolved change.</summary>
    public SqlTextSpan Span;
    /// <summary>The preserved unsupported-syntax marker, when one was present in the input AST.</summary>
    public SqlUnsupportedSyntax? UnsupportedSyntax;

    /// <summary>Initializes an explicit marker for a change that cannot be made losslessly.</summary>
    /// <param name="kind">The marker category.</param>
    /// <param name="description">The human-readable description.</param>
    /// <param name="sourceText">The exact associated source text.</param>
    /// <param name="span">The original source span.</param>
    /// <param name="unsupportedSyntax">The preserved unsupported syntax, when available.</param>
    /// <exception cref="ArgumentNullException">description, sourceText, or span is <c>null</c>.</exception>
    public SqlNormalizationMarker(SqlNormalizationMarkerKind kind, string description, string sourceText, SqlTextSpan span, SqlUnsupportedSyntax? unsupportedSyntax)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        if (sourceText == null)
        {
            throw new ArgumentNullException("sourceText");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Kind = kind;
        Description = description;
        SourceText = sourceText;
        Span = span;
        UnsupportedSyntax = unsupportedSyntax;
    }
}
