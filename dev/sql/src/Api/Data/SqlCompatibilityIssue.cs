using System;

namespace sql;

/// <summary>Describes one syntax feature that is unavailable in a selected target dialect.</summary>
public class SqlCompatibilityIssue
{
    /// <summary>The category of the compatibility issue.</summary>
    public SqlCompatibilityIssueKind Kind;
    /// <summary>The human-readable syntax feature name.</summary>
    public string Feature;
    /// <summary>The half-open source span covered by the incompatible syntax.</summary>
    public SqlTextSpan Span;
    /// <summary>The diagnostic associated with this issue.</summary>
    public SqlDiagnostic Diagnostic;

    /// <summary>Initializes a target-dialect compatibility issue.</summary>
    /// <param name="kind">The issue category.</param>
    /// <param name="feature">The human-readable syntax feature name.</param>
    /// <param name="span">The source span associated with the issue.</param>
    /// <param name="diagnostic">The diagnostic associated with the issue.</param>
    /// <exception cref="ArgumentNullException">feature, span, or diagnostic is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">feature is empty or whitespace.</exception>
    public SqlCompatibilityIssue(SqlCompatibilityIssueKind kind, string feature, SqlTextSpan span, SqlDiagnostic diagnostic)
    {
        if (feature == null)
        {
            throw new ArgumentNullException("feature");
        }

        if (string.IsNullOrWhiteSpace(feature))
        {
            throw new ArgumentException("feature must not be empty or whitespace");
        }

        Span = span ?? throw new ArgumentNullException("span");
        Diagnostic = diagnostic ?? throw new ArgumentNullException("diagnostic");
        Kind = kind;
        Feature = feature;
    }
}
