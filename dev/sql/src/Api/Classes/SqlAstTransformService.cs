namespace sql;

/// <summary>Normalizes and canonicalizes syntax trees through deterministic generation and reparsing.</summary>
public class SqlAstTransformService : ISqlAstTransformer
{
    /// <summary>Initializes a new AST transform service.</summary>
    public SqlAstTransformService()
    {
    }

    /// <summary>Creates a normalized SQL syntax tree using explicit formatting and dialect options.</summary>
    /// <param name="script">The input syntax tree.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized syntax tree, text, diagnostics, and unresolved-change markers.</returns>
    public SqlNormalizationResult NormalizeScript(SqlScript script, SqlNormalizationOptions options)
    {
        return SqlNormalizationTransform.NormalizeScript(script, options);
    }

    /// <summary>Creates a canonical SQL syntax tree and deterministic text for stable comparison.</summary>
    /// <param name="script">The input syntax tree.</param>
    /// <param name="options">The canonicalization options.</param>
    /// <returns>The canonical syntax tree, comparison text, diagnostics, and unresolved-change markers.</returns>
    public SqlCanonicalizationResult CanonicalizeScript(SqlScript script, SqlCanonicalizationOptions options)
    {
        return SqlNormalizationTransform.CanonicalizeScript(script, options);
    }

    /// <summary>Maps safely representable syntax to a target dialect and marks syntax without a lossless mapping.</summary>
    /// <param name="script">The input syntax tree.</param>
    /// <param name="options">The explicit read, write, and output options.</param>
    /// <returns>The target SQL, transformed syntax tree, mappings, markers, and diagnostics.</returns>
    public SqlTranspileResult TranspileScript(SqlScript script, SqlTranspileOptions options)
    {
        return SqlDialectTranspilation.TranspileScript(script, options);
    }

    /// <summary>Compares two scripts and reports node changes at stable AST paths.</summary>
    /// <param name="originalScript">The original syntax tree.</param>
    /// <param name="modifiedScript">The modified syntax tree.</param>
    /// <returns>The path-based AST diff.</returns>
    public SqlAstDiffResult DiffScripts(SqlScript originalScript, SqlScript modifiedScript)
    {
        return SqlAstDiff.CompareScripts(originalScript, modifiedScript);
    }
}
