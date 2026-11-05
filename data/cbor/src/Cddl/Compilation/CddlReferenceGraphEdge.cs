namespace cbor;

internal class CddlReferenceGraphEdge
{
    internal CddlReferenceGraphNode Source = new CddlReferenceGraphNode();
    internal CddlReferenceGraphNode Target = new CddlReferenceGraphNode();
    internal CddlNormalizedNode ReferenceNode = new CddlNormalizedNode();
    internal bool IsRequired;
}
