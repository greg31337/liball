using System.Collections.Generic;

namespace gltf;

internal class BufferViewTargetCleaner
{
    internal void ClearUnusedTargets(
        GltfDocument document,
        GltfTransformStepResult result,
        List<int> candidates)
    {
        bool[] vertexViews = new bool[document.BufferViews.Count];
        bool[] indexViews = new bool[document.BufferViews.Count];
        CollectPrimitiveViews(document, vertexViews, indexViews);
        CollectInstanceViews(document, vertexViews);
        int candidateIndex = 0;
        while (candidateIndex < candidates.Count)
        {
            int viewIndex = candidates[candidateIndex];
            GltfBufferView view = document.BufferViews[viewIndex];
            bool clear = view.HasTarget
                && ((view.Target == GltfBufferViewTarget.ArrayBuffer
                        && !vertexViews[viewIndex])
                    || (view.Target == GltfBufferViewTarget.ElementArrayBuffer
                        && !indexViews[viewIndex]));
            if (clear)
            {
                view.HasTarget = false;
                view.Target = GltfBufferViewTarget.None;
                result.ChangedObjects.Add($"bufferViews/{viewIndex}");
            }

            candidateIndex++;
        }
    }

    internal void CollectPrimitiveViews(
        GltfDocument document,
        bool[] vertexViews,
        bool[] indexViews)
    {
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                int attributeIndex = 0;
                while (attributeIndex < primitive.Attributes.Count)
                {
                    MarkAccessorView(
                        document,
                        primitive.Attributes[attributeIndex].Accessor,
                        vertexViews);
                    attributeIndex++;
                }

                int targetIndex = 0;
                while (targetIndex < primitive.Targets.Count)
                {
                    GltfMorphTarget target = primitive.Targets[targetIndex];
                    attributeIndex = 0;
                    while (attributeIndex < target.Attributes.Count)
                    {
                        MarkAccessorView(
                            document,
                            target.Attributes[attributeIndex].Accessor,
                            vertexViews);
                        attributeIndex++;
                    }

                    targetIndex++;
                }

                if (primitive.HasIndices)
                {
                    MarkAccessorView(document, primitive.Indices, indexViews);
                }

                primitiveIndex++;
            }

            meshIndex++;
        }
    }

    internal void CollectInstanceViews(GltfDocument document, bool[] vertexViews)
    {
        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfPropertyJson property = document.Nodes[nodeIndex].PropertyJson;
            if (property.TypedExtensions.ContainsKey(
                GltfExtensionNames.ExtMeshGpuInstancing))
            {
                GltfTypedExtensionData typed = property.TypedExtensions[
                    GltfExtensionNames.ExtMeshGpuInstancing];
                if (typed.MeshGpuInstancing != null)
                {
                    foreach (KeyValuePair<string, int> attribute
                        in typed.MeshGpuInstancing.Attributes)
                    {
                        MarkAccessorView(document, attribute.Value, vertexViews);
                    }
                }
            }

            nodeIndex++;
        }
    }

    internal void MarkAccessorView(
        GltfDocument document,
        int accessorIndex,
        bool[] views)
    {
        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count)
        {
            return;
        }

        GltfAccessor accessor = document.Accessors[accessorIndex];
        if (accessor.HasBufferView
            && accessor.BufferView >= 0
            && accessor.BufferView < views.Length)
        {
            views[accessor.BufferView] = true;
        }
    }
}
