using System;
using System.Collections.Generic;

namespace gltf;

internal class DocumentGpuInstancer
{
    internal AccessorEncoder Encoder = new AccessorEncoder();
    internal AnimationPointerResolver PointerResolver = new AnimationPointerResolver();

    internal bool CreateDocumentInstances(
        GltfDocument document,
        GltfTransformStepResult result)
    {
        NodeReferenceCounts references = CountNodePlacements(document);
        bool changed = false;
        int sceneIndex = 0;
        while (sceneIndex < document.Scenes.Count)
        {
            GltfScene scene = document.Scenes[sceneIndex];
            if (scene.PropertyJson.Extensions.Count == 0
                && scene.PropertyJson.TypedExtensions.Count == 0)
            {
                changed = InstanceOwnerNodes(
                    document,
                    scene.Nodes,
                    references,
                    $"scenes/{sceneIndex}/nodes",
                    result) || changed;
            }

            sceneIndex++;
        }

        int parentIndex = 0;
        while (parentIndex < document.Nodes.Count)
        {
            GltfNode parent = document.Nodes[parentIndex];
            if (parent.PropertyJson.Extensions.Count == 0
                && parent.PropertyJson.TypedExtensions.Count == 0)
            {
                changed = InstanceOwnerNodes(
                    document,
                    parent.Children,
                    references,
                    $"nodes/{parentIndex}/children",
                    result) || changed;
            }

            parentIndex++;
        }

        return changed;
    }

    internal NodeReferenceCounts CountNodePlacements(GltfDocument document)
    {
        NodeReferenceCounts result = new NodeReferenceCounts();
        result.ParentReferences = new int[document.Nodes.Count];
        result.SceneReferences = new int[document.Nodes.Count];
        int parentIndex = 0;
        while (parentIndex < document.Nodes.Count)
        {
            GltfNode parent = document.Nodes[parentIndex];
            int childPosition = 0;
            while (childPosition < parent.Children.Count)
            {
                int child = parent.Children[childPosition];
                if (child >= 0 && child < document.Nodes.Count)
                {
                    result.ParentReferences[child]++;
                }

                childPosition++;
            }

            parentIndex++;
        }

        int sceneIndex = 0;
        while (sceneIndex < document.Scenes.Count)
        {
            GltfScene scene = document.Scenes[sceneIndex];
            int rootPosition = 0;
            while (rootPosition < scene.Nodes.Count)
            {
                int root = scene.Nodes[rootPosition];
                if (root >= 0 && root < document.Nodes.Count)
                {
                    result.SceneReferences[root]++;
                }

                rootPosition++;
            }

            sceneIndex++;
        }

        return result;
    }

    internal bool InstanceOwnerNodes(
        GltfDocument document,
        List<int> owner,
        NodeReferenceCounts references,
        string ownerPath,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int position = 0;
        while (position < owner.Count)
        {
            int firstIndex = owner[position];
            if (!IsInstancingCandidate(document, firstIndex, references))
            {
                position++;
                continue;
            }

            GltfNode first = document.Nodes[firstIndex];
            int runLength = 1;
            while (position + runLength < owner.Count)
            {
                int candidateIndex = owner[position + runLength];
                if (!IsInstancingCandidate(document, candidateIndex, references)
                    || document.Nodes[candidateIndex].Mesh != first.Mesh)
                {
                    break;
                }

                runLength++;
            }

            if (runLength > 1)
            {
                List<int> nodes = new List<int>();
                int runIndex = 0;
                while (runIndex < runLength)
                {
                    nodes.Add(owner[position + runIndex]);
                    runIndex++;
                }

                CreateInstanceGroup(document, nodes, result);
                runIndex = 1;
                while (runIndex < runLength)
                {
                    owner.RemoveAt(position + 1);
                    runIndex++;
                }

                result.ChangedObjects.Add(ownerPath);
                changed = true;
            }

            position++;
        }

        return changed;
    }

    internal bool IsInstancingCandidate(
        GltfDocument document,
        int nodeIndex,
        NodeReferenceCounts references)
    {
        if (nodeIndex < 0 || nodeIndex >= document.Nodes.Count)
        {
            return false;
        }

        if (references.ParentReferences[nodeIndex]
                + references.SceneReferences[nodeIndex] != 1)
        {
            return false;
        }

        GltfNode node = document.Nodes[nodeIndex];
        if (!node.HasMesh
            || node.Mesh < 0
            || node.Mesh >= document.Meshes.Count
            || node.HasCamera
            || node.HasSkin
            || node.HasMatrix
            || node.Children.Count > 0
            || node.Weights != null
            || node.Name != null
            || HasPerNodePropertyState(node.PropertyJson)
            || IsAnimatedInstanceNode(document, nodeIndex)
            || IsSkinInstanceNode(document, nodeIndex)
            || !HasExactInstanceTransform(node))
        {
            return false;
        }

        return MeshSupportsInstances(document.Meshes[node.Mesh]);
    }

    internal bool MeshSupportsInstances(GltfMesh mesh)
    {
        if (mesh.Weights != null
            || mesh.PropertyJson.Extensions.Count > 0
            || mesh.PropertyJson.TypedExtensions.Count > 0)
        {
            return false;
        }

        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            if (primitive.Targets.Count > 0
                || primitive.PropertyJson.Extensions.Count > 0
                || primitive.PropertyJson.TypedExtensions.Count > 0)
            {
                return false;
            }

            primitiveIndex++;
        }

        return mesh.Primitives.Count > 0;
    }

    internal bool HasPerNodePropertyState(GltfPropertyJson property)
    {
        return property.HasExtras
            || property.Extensions.Count > 0
            || property.TypedExtensions.Count > 0
            || property.UnknownMembers.Count > 0;
    }

    internal bool IsAnimatedInstanceNode(GltfDocument document, int nodeIndex)
    {
        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            int channelIndex = 0;
            while (channelIndex < animation.Channels.Count)
            {
                GltfAnimationTarget target = animation.Channels[channelIndex].Target;
                if (target.HasNode && target.Node == nodeIndex)
                {
                    return true;
                }

                string? pointer = PointerResolver.GetTargetPointer(target);
                if (pointer != null)
                {
                    AnimationPointerTarget resolved = PointerResolver.ResolvePointer(
                        document,
                        pointer);
                    if (resolved.TargetCollection == "nodes"
                        && resolved.TargetIndex == nodeIndex)
                    {
                        return true;
                    }
                }

                channelIndex++;
            }

            animationIndex++;
        }

        return false;
    }

    internal bool IsSkinInstanceNode(GltfDocument document, int nodeIndex)
    {
        int skinIndex = 0;
        while (skinIndex < document.Skins.Count)
        {
            GltfSkin skin = document.Skins[skinIndex];
            if ((skin.HasSkeleton && skin.Skeleton == nodeIndex)
                || skin.Joints.Contains(nodeIndex))
            {
                return true;
            }

            skinIndex++;
        }

        return false;
    }

    internal bool HasExactInstanceTransform(GltfNode node)
    {
        if (node.HasTranslation && !IsExactFloatArray(node.Translation, 3))
        {
            return false;
        }

        if (node.HasRotation && !IsExactFloatArray(node.Rotation, 4))
        {
            return false;
        }

        if (node.HasScale && !IsExactFloatArray(node.Scale, 3))
        {
            return false;
        }

        return true;
    }

    internal bool IsExactFloatArray(double[] values, int expectedLength)
    {
        if (values == null || values.Length != expectedLength)
        {
            return false;
        }

        int index = 0;
        while (index < values.Length)
        {
            double value = values[index];
            if (!double.IsFinite(value) || (double)(float)value != value)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal void CreateInstanceGroup(
        GltfDocument document,
        List<int> nodes,
        GltfTransformStepResult result)
    {
        double[] translations = new double[nodes.Count * 3];
        double[] rotations = new double[nodes.Count * 4];
        double[] scales = new double[nodes.Count * 3];
        int instanceIndex = 0;
        while (instanceIndex < nodes.Count)
        {
            GltfNode node = document.Nodes[nodes[instanceIndex]];
            CopyInstanceVector(
                node.HasTranslation ? node.Translation : new double[] { 0.0, 0.0, 0.0 },
                translations,
                instanceIndex * 3);
            CopyInstanceVector(
                node.HasRotation ? node.Rotation : new double[] { 0.0, 0.0, 0.0, 1.0 },
                rotations,
                instanceIndex * 4);
            CopyInstanceVector(
                node.HasScale ? node.Scale : new double[] { 1.0, 1.0, 1.0 },
                scales,
                instanceIndex * 3);
            instanceIndex++;
        }

        GltfMeshGpuInstancingExtension extension =
            new GltfMeshGpuInstancingExtension();
        extension.Attributes["TRANSLATION"] = CreateInstanceAccessor(
            document,
            translations,
            GltfAccessorType.Vector3,
            result);
        extension.Attributes["ROTATION"] = CreateInstanceAccessor(
            document,
            rotations,
            GltfAccessorType.Vector4,
            result);
        extension.Attributes["SCALE"] = CreateInstanceAccessor(
            document,
            scales,
            GltfAccessorType.Vector3,
            result);

        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MeshGpuInstancing = extension;
        GltfNode target = document.Nodes[nodes[0]];
        target.PropertyJson.TypedExtensions[
            GltfExtensionNames.ExtMeshGpuInstancing] = typed;
        ResetInstanceNodeTransform(target);

        instanceIndex = 0;
        while (instanceIndex < nodes.Count)
        {
            result.ChangedObjects.Add($"nodes/{nodes[instanceIndex]}");
            instanceIndex++;
        }

        AddInstanceExtensionDeclarations(document, result);
    }

    internal void CopyInstanceVector(double[] source, double[] target, int offset)
    {
        int index = 0;
        while (index < source.Length)
        {
            target[offset + index] = source[index];
            index++;
        }
    }

    internal int CreateInstanceAccessor(
        GltfDocument document,
        double[] values,
        GltfAccessorType type,
        GltfTransformStepResult result)
    {
        AccessorEncodingResult encoding = Encoder.EncodeFloatingAccessor(
            values,
            GltfComponentType.Float,
            type,
            false);
        AccessorCreationResult created = Encoder.CreateAccessorStorage(
            document,
            encoding,
            true);
        result.ChangedObjects.Add($"accessors/{created.Accessor}");
        return created.Accessor;
    }

    internal void ResetInstanceNodeTransform(GltfNode node)
    {
        node.HasMatrix = false;
        node.Matrix =
        [
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 1.0
        ];
        node.HasTranslation = false;
        node.Translation = [0.0, 0.0, 0.0];
        node.HasRotation = false;
        node.Rotation = [0.0, 0.0, 0.0, 1.0];
        node.HasScale = false;
        node.Scale = [1.0, 1.0, 1.0];
    }

    internal void AddInstanceExtensionDeclarations(
        GltfDocument document,
        GltfTransformStepResult result)
    {
        if (!document.ExtensionsUsed.Contains(GltfExtensionNames.ExtMeshGpuInstancing))
        {
            document.ExtensionsUsed.Add(GltfExtensionNames.ExtMeshGpuInstancing);
            result.ChangedObjects.Add("extensionsUsed");
        }

        if (!document.ExtensionsRequired.Contains(
            GltfExtensionNames.ExtMeshGpuInstancing))
        {
            document.ExtensionsRequired.Add(GltfExtensionNames.ExtMeshGpuInstancing);
            result.ChangedObjects.Add("extensionsRequired");
        }
    }
}
