namespace svg;

/// <summary>Reports one node or attribute mutation without throwing for structural failures.</summary>
public class SvgMutationResult
{
    /// <summary>Whether the requested mutation completed.</summary>
    public bool Success;
    /// <summary>The stable mutation failure.</summary>
    public SvgMutationError Error;
    /// <summary>The inserted, moved, removed, or replacement node, or <c>null</c>.</summary>
    public SvgNode? Node;
    /// <summary>The inserted, moved, removed, or replacement attribute, or <c>null</c>.</summary>
    public SvgAttribute? Attribute;
    /// <summary>The node displaced by replacement, or <c>null</c>.</summary>
    public SvgNode? RemovedNode;
    /// <summary>The attribute displaced by replacement, or <c>null</c>.</summary>
    public SvgAttribute? RemovedAttribute;
    /// <summary>The stable human-readable result message.</summary>
    public string Message;

    /// <summary>Creates an explicit mutation result.</summary>
    /// <param name="success">Whether the mutation completed.</param>
    /// <param name="error">The stable mutation failure.</param>
    /// <param name="node">The affected node.</param>
    /// <param name="attribute">The affected attribute.</param>
    /// <param name="removedNode">The displaced node.</param>
    /// <param name="removedAttribute">The displaced attribute.</param>
    /// <param name="message">The result message.</param>
    internal SvgMutationResult(bool success, SvgMutationError error, SvgNode? node, SvgAttribute? attribute, SvgNode? removedNode, SvgAttribute? removedAttribute, string message)
    {
        Success = success;
        Error = error;
        Node = node;
        Attribute = attribute;
        RemovedNode = removedNode;
        RemovedAttribute = removedAttribute;
        Message = message;
    }
}
