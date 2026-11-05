using System;

namespace sql;

/// <summary>Describes one lossless syntax rewrite applied for a target dialect.</summary>
public class SqlTranspileMapping
{
    /// <summary>The category of syntax mapping.</summary>
    public SqlTranspileMappingKind Kind;
    /// <summary>The exact source spelling before the mapping.</summary>
    public string SourceText;
    /// <summary>The target-dialect spelling after the mapping.</summary>
    public string TargetText;
    /// <summary>The half-open span of the source syntax.</summary>
    public SqlTextSpan SourceSpan;
    /// <summary>Indicates that the mapping preserves represented SQL semantics.</summary>
    public bool IsLossless;

    /// <summary>Initializes a dialect syntax mapping result.</summary>
    /// <param name="kind">The mapping category.</param>
    /// <param name="sourceText">The source spelling.</param>
    /// <param name="targetText">The target spelling.</param>
    /// <param name="sourceSpan">The source span.</param>
    /// <param name="isLossless">Whether the mapping preserves represented semantics.</param>
    /// <exception cref="ArgumentNullException">sourceText, targetText, or sourceSpan is <c>null</c>.</exception>
    public SqlTranspileMapping(SqlTranspileMappingKind kind, string sourceText, string targetText, SqlTextSpan sourceSpan, bool isLossless)
    {
        SourceText = sourceText ?? throw new ArgumentNullException("sourceText");
        TargetText = targetText ?? throw new ArgumentNullException("targetText");
        SourceSpan = sourceSpan ?? throw new ArgumentNullException("sourceSpan");
        Kind = kind;
        IsLossless = isLossless;
    }
}
