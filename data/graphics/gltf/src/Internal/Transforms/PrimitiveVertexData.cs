using System.Collections.Generic;

namespace gltf;

internal class PrimitiveVertexStream
{
    internal int Kind;
    internal int OuterIndex;
    internal int InnerIndex;
    internal int SourceAccessor;
    internal GltfAccessor Accessor = new GltfAccessor();
    internal byte[] RawBytes = [];
    internal int ElementByteLength;
}

internal class PrimitiveVertexData
{
    internal List<PrimitiveVertexStream> Streams = new List<PrimitiveVertexStream>();
    internal int VertexCount;
    internal bool HasIndices;
    internal int IndexAccessor = GltfConstants.AbsentIndex;
    internal GltfComponentType IndexComponentType = GltfComponentType.UnsignedInt;
    internal uint[] Indices = [];
}
