namespace sql;

/// <summary>Defines AST normalization and canonicalization operations.</summary>
public interface ISqlAstTransformer
{
    /// <summary>Creates a normalized SQL syntax tree using explicit formatting and dialect options.</summary>
    /// <param name="script">The input syntax tree.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized syntax tree, text, diagnostics, and unresolved-change markers.</returns>
    SqlNormalizationResult NormalizeScript(SqlScript script, SqlNormalizationOptions options);

    /// <summary>Creates a canonical SQL syntax tree and deterministic text for stable comparison.</summary>
    /// <param name="script">The input syntax tree.</param>
    /// <param name="options">The canonicalization options.</param>
    /// <returns>The canonical syntax tree, comparison text, diagnostics, and unresolved-change markers.</returns>
    SqlCanonicalizationResult CanonicalizeScript(SqlScript script, SqlCanonicalizationOptions options);

    /// <summary>Maps safely representable syntax to a target dialect and marks syntax without a lossless mapping.</summary>
    /// <param name="script">The input syntax tree.</param>
    /// <param name="options">The explicit read, write, and output options.</param>
    /// <returns>The target SQL, transformed syntax tree, mappings, markers, and diagnostics.</returns>
    SqlTranspileResult TranspileScript(SqlScript script, SqlTranspileOptions options);

    /// <summary>Compares two scripts and reports node changes at stable AST paths.</summary>
    /// <param name="originalScript">The original syntax tree.</param>
    /// <param name="modifiedScript">The modified syntax tree.</param>
    /// <returns>The path-based AST diff.</returns>
    SqlAstDiffResult DiffScripts(SqlScript originalScript, SqlScript modifiedScript);
}
