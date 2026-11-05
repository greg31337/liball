namespace uri;

/// <summary>
/// Identifies whether a URI Template part is literal text or a variable expression.
/// </summary>
public enum UriTemplatePartKind
{
    /// <summary>A literal sequence copied or encoded during expansion.</summary>
    Literal = 0,

    /// <summary>A brace-delimited variable expression.</summary>
    Expression = 1
}
