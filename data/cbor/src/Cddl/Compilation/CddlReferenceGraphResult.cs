using System.Collections.Generic;

namespace cbor;

internal class CddlReferenceGraphResult
{
    internal List<CddlReferenceGraphNode> Nodes =
        new List<CddlReferenceGraphNode>();
    internal List<CddlReferenceGraphEdge> Edges =
        new List<CddlReferenceGraphEdge>();
    internal List<CddlReferenceComponent> Components =
        new List<CddlReferenceComponent>();
    internal List<CddlDiagnostic> Diagnostics = new List<CddlDiagnostic>();
    internal bool HasErrors;
}
