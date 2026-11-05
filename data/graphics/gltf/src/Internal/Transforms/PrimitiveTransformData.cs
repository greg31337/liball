using System.Collections.Generic;

namespace gltf;

internal class PrimitiveTransformSettings
{
    internal int MaxVertices = 65535;
}

internal class PrimitiveSplitChunk
{
    internal List<int> Vertices = new List<int>();
    internal List<uint> Indices = new List<uint>();
    internal Dictionary<int, int> VertexRemap = new Dictionary<int, int>();
}

internal class NodeReferenceCounts
{
    internal int[] ParentReferences = [];
    internal int[] SceneReferences = [];
}
