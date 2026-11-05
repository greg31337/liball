using System.Collections.Generic;
using System.Numerics;

namespace gltf;

internal class SceneTransformSettings
{
    internal List<int> Nodes = new List<int>();
    internal bool HasNodes;
    internal Matrix4x4 Matrix = Matrix4x4.Identity;
    internal bool HasMatrix;
    internal double UnitScale = 1.0;
    internal Matrix4x4 AxisBasis = Matrix4x4.Identity;
    internal bool HasAxisBasis;
    internal int Scene = GltfConstants.AbsentIndex;
    internal bool HasScene;
}

internal class AccessorTransformCacheEntry
{
    internal int Source = GltfConstants.AbsentIndex;
    internal int Kind;
    internal int Result = GltfConstants.AbsentIndex;
}

internal class BakedAccessorTransformCacheEntry
{
    internal int Source = GltfConstants.AbsentIndex;
    internal int Kind;
    internal Matrix4x4 Transform = Matrix4x4.Identity;
    internal Matrix4x4 NormalTransform = Matrix4x4.Identity;
    internal bool ReversesOrientation;
    internal int Result = GltfConstants.AbsentIndex;
}
