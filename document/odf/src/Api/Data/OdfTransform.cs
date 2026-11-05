namespace odf;

/// <summary>
/// Preserves a drawing transform expression for later geometric interpretation.
/// </summary>
public class OdfTransform
{
    /// <summary>The lexical <c>draw:transform</c> expression.</summary>
    public string Expression = "";
    /// <summary>Whether a transform expression is present.</summary>
    public bool IsSpecified;
    /// <summary>Whether the specified transform expression is valid.</summary>
    public bool IsValid;
    /// <summary>The parsed transform operations in application order.</summary>
    public OdfTransformOperation[] Operations = [];
    /// <summary>The element carrying the transform.</summary>
    public OdfNode? Node;
    internal string OriginalExpression = "";
    internal string OriginalCanonicalExpression = "";
}
