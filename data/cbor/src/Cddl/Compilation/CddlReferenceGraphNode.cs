using System.Collections.Generic;

namespace cbor;

internal class CddlReferenceGraphNode
{
    internal CddlNormalizedRule Rule = new CddlNormalizedRule();
    internal List<CddlReferenceGraphEdge> Outgoing =
        new List<CddlReferenceGraphEdge>();
    internal List<CddlReferenceGraphEdge> Incoming =
        new List<CddlReferenceGraphEdge>();
    internal int ComponentIndex = -1;
    internal bool IsRecursive;
    internal bool IsProductive;
    internal bool CanMatchEmpty;
    internal int MinimumItems = -1;
}
