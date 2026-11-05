namespace odf;

/// <summary>
/// Describes one operation in a drawing transform expression.
/// </summary>
public class OdfTransformOperation
{
    /// <summary>The recognized operation kind.</summary>
    public OdfTransformOperationKind Kind;
    /// <summary>The case-preserving operation name.</summary>
    public string Name = "";
    /// <summary>The preserved numeric, angle, or length arguments.</summary>
    public string[] Arguments = [];
}
