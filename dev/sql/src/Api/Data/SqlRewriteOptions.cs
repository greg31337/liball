namespace sql;

/// <summary>Controls metadata preservation during direct-child AST rewrites.</summary>
public class SqlRewriteOptions
{
    /// <summary>Preserves source spans on rewritten root nodes.</summary>
    public bool PreserveSpans;
    /// <summary>Preserves token arrays, token ranges, terminators, and quoted-token metadata.</summary>
    public bool PreserveTokens;
    /// <summary>Preserves leading and trailing trivia on rewritten roots.</summary>
    public bool PreserveTrivia;
    /// <summary>Preserves retained source text on rewritten roots and parse results.</summary>
    public bool PreserveSourceText;
    /// <summary>Preserves the diagnostic bag when rewriting a parse result.</summary>
    public bool PreserveDiagnostics;

    /// <summary>Initializes rewrite options that preserve all existing metadata by default.</summary>
    public SqlRewriteOptions()
    {
        PreserveSpans = true;
        PreserveTokens = true;
        PreserveTrivia = true;
        PreserveSourceText = true;
        PreserveDiagnostics = true;
    }
}
