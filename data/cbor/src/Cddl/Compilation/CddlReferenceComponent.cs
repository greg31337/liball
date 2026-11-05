using System.Collections.Generic;

namespace cbor;

internal class CddlReferenceComponent
{
    internal int Index = -1;
    internal List<CddlReferenceGraphNode> Nodes =
        new List<CddlReferenceGraphNode>();
    internal bool IsRecursive;
    internal bool IsProductive;
}
