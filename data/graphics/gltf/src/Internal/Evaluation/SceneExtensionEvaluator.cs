using System.Collections.Generic;

namespace gltf;

internal class SceneExtensionEvaluator
{
    internal void PopulateSceneExtensionMetadata(
        GltfDocument document,
        GltfScenePose pose,
        SceneGraphAnalysis graph)
    {
        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count && nodeIndex < pose.Nodes.Count)
        {
            PopulateLocalNodeMetadata(
                document,
                document.Nodes[nodeIndex],
                pose.Nodes[nodeIndex]);
            nodeIndex++;
        }

        int traversalIndex = 0;
        while (traversalIndex < graph.TraversalOrder.Count)
        {
            int currentIndex = graph.TraversalOrder[traversalIndex];
            if (currentIndex >= 0
                && currentIndex < pose.Nodes.Count
                && currentIndex < graph.Parents.Count)
            {
                int parentIndex = graph.Parents[currentIndex];
                if (parentIndex >= 0 && parentIndex < pose.Nodes.Count)
                {
                    GltfNodePose parent = pose.Nodes[parentIndex];
                    GltfNodePose current = pose.Nodes[currentIndex];
                    current.Visible = parent.Visible && current.Visible;
                    current.Hoverable = parent.Hoverable && current.Hoverable;
                    current.Selectable = parent.Selectable && current.Selectable;
                }
            }

            traversalIndex++;
        }
    }

    internal void PopulateLocalNodeMetadata(
        GltfDocument document,
        GltfNode node,
        GltfNodePose pose)
    {
        GltfTypedExtensionData? visibility = FindTypedExtension(
            node.PropertyJson,
            GltfExtensionNames.KhrNodeVisibility);
        if (visibility != null && visibility.NodeVisibility != null)
        {
            pose.Visible = visibility.NodeVisibility.Visible;
        }

        GltfTypedExtensionData? hoverability = FindTypedExtension(
            node.PropertyJson,
            GltfExtensionNames.KhrNodeHoverability);
        if (hoverability != null && hoverability.NodeHoverability != null)
        {
            pose.Hoverable = hoverability.NodeHoverability.Hoverable;
        }

        GltfTypedExtensionData? selectability = FindTypedExtension(
            node.PropertyJson,
            GltfExtensionNames.KhrNodeSelectability);
        if (selectability != null && selectability.NodeSelectability != null)
        {
            pose.Selectable = selectability.NodeSelectability.Selectable;
        }

        GltfTypedExtensionData? light = FindTypedExtension(
            node.PropertyJson,
            GltfExtensionNames.KhrLightsPunctual);
        if (light != null && light.PunctualLightNode != null)
        {
            pose.HasPunctualLight = true;
            pose.PunctualLight = light.PunctualLightNode.Light;
        }

        GltfTypedExtensionData? instancing = FindTypedExtension(
            node.PropertyJson,
            GltfExtensionNames.ExtMeshGpuInstancing);
        if (instancing != null && instancing.MeshGpuInstancing != null)
        {
            PopulateGpuInstancingMetadata(
                document,
                pose,
                instancing.MeshGpuInstancing);
        }
    }

    internal GltfTypedExtensionData? FindTypedExtension(
        GltfPropertyJson propertyJson,
        string name)
    {
        if (propertyJson == null
            || propertyJson.TypedExtensions == null
            || !propertyJson.TypedExtensions.ContainsKey(name))
        {
            return null;
        }

        return propertyJson.TypedExtensions[name];
    }

    internal void PopulateGpuInstancingMetadata(
        GltfDocument document,
        GltfNodePose pose,
        GltfMeshGpuInstancingExtension extension)
    {
        List<string> names = new SceneExtensionPropertyJson().GetSortedIndexKeys(
            extension.Attributes);
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            int accessorIndex = extension.Attributes[name];
            pose.InstanceAttributes[name] = accessorIndex;
            if (pose.InstanceCount == 0
                && accessorIndex >= 0
                && accessorIndex < document.Accessors.Count)
            {
                pose.InstanceCount = document.Accessors[accessorIndex].Count;
            }

            index++;
        }
    }
}
