using System;
using System.Collections.Generic;
using System.Numerics;

namespace gltf;

internal class DocumentHierarchyTransformer
{
    internal TransformMath TransformMath = new TransformMath();
    internal SceneEvaluator SceneEvaluator = new SceneEvaluator();
    internal AccessorCodecEngine Accessors = new AccessorCodecEngine();
    internal AccessorEncoder Encoder = new AccessorEncoder();
    internal CoordinateConverter Converter = new CoordinateConverter();
    internal DocumentVertexTransformer Vertices = new DocumentVertexTransformer();
    internal BufferViewTargetCleaner Targets = new BufferViewTargetCleaner();
    internal AnimationPointerResolver PointerResolver = new AnimationPointerResolver();
    internal List<BakedAccessorTransformCacheEntry> BakeCache =
        new List<BakedAccessorTransformCacheEntry>();
    internal List<int> SourceBufferViews = new List<int>();

    internal bool ApplySelectedNodeMatrix(
        GltfDocument document,
        SceneTransformSettings settings,
        GltfTransformStepResult result)
    {
        if (!settings.HasMatrix)
        {
            throw new ArgumentException(
                "ApplyNodeTransform requires a 16-value 'matrix' setting.",
                "step");
        }

        bool changed = false;
        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            if (IsSelectedNode(settings, nodeIndex)
                && CanChangeNodeTransform(document, nodeIndex))
            {
                NodeTransformEvaluation evaluated =
                    TransformMath.EvaluateLocalTransform(
                        document.Nodes[nodeIndex],
                        nodeIndex);
                if (evaluated.HasLocalTransform)
                {
                    StoreNodeMatrix(
                        document.Nodes[nodeIndex],
                        evaluated.LocalTransform * settings.Matrix);
                    result.ChangedObjects.Add($"nodes/{nodeIndex}");
                    changed = true;
                }
            }

            nodeIndex++;
        }

        return changed;
    }

    internal bool BakeSelectedNodeTransforms(
        GltfDocument document,
        SceneTransformSettings settings,
        GltfTransformStepResult result)
    {
        BakeCache.Clear();
        SourceBufferViews.Clear();
        bool changed = false;
        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[nodeIndex];
            if (IsSelectedNode(settings, nodeIndex)
                && CanBakeNode(document, nodeIndex))
            {
                NodeTransformEvaluation evaluated =
                    TransformMath.EvaluateLocalTransform(node, nodeIndex);
                if (evaluated.HasLocalTransform
                    && evaluated.LocalTransform != Matrix4x4.Identity)
                {
                    NormalTransformCalculation normal =
                        TransformMath.CalculateNormalTransform(
                            evaluated.LocalTransform);
                    if (normal.HasTransform)
                    {
                        BakeMesh(
                            document,
                            node.Mesh,
                            evaluated.LocalTransform,
                            normal.Transform,
                            evaluated.LocalTransform.GetDeterminant() < 0.0f,
                            result);
                        ResetNodeTransform(node);
                        result.ChangedObjects.Add($"nodes/{nodeIndex}");
                        changed = true;
                    }
                }
            }

            nodeIndex++;
        }

        if (changed)
        {
            Targets.ClearUnusedTargets(document, result, SourceBufferViews);
        }

        return changed;
    }

    internal bool FlattenDocumentHierarchy(
        GltfDocument document,
        SceneTransformSettings settings,
        GltfTransformStepResult result)
    {
        if (HasGlobalHierarchyConflict(document))
        {
            result.Status = GltfOperationStatus.Skipped;
            result.Reason = "Hierarchy flattening skipped animation, skin, or extension state.";
            return false;
        }

        SceneTransformAnalysis transforms = TransformMath.EvaluateDocumentTransforms(document);
        if (!transforms.IsValid)
        {
            result.Status = GltfOperationStatus.Skipped;
            result.Reason = "Hierarchy flattening requires a valid single-parent node graph.";
            return false;
        }

        List<int> selectedScenes = SelectSceneIndices(document, settings);
        bool[] activeNodes = new bool[document.Nodes.Count];
        int selectionIndex = 0;
        while (selectionIndex < selectedScenes.Count)
        {
            int sceneIndex = selectedScenes[selectionIndex];
            SceneGraphTraversal traversal = transforms.Graph.SceneTraversals[sceneIndex];
            int nodePosition = 0;
            while (nodePosition < traversal.Nodes.Count)
            {
                activeNodes[traversal.Nodes[nodePosition]] = true;
                nodePosition++;
            }

            selectionIndex++;
        }

        int nodeIndex = 0;
        while (nodeIndex < activeNodes.Length)
        {
            if (activeNodes[nodeIndex]
                && transforms.Graph.NodeScenes[nodeIndex].Count
                    != selectedScenes.Count
                && settings.HasScene)
            {
                result.Status = GltfOperationStatus.Skipped;
                result.Reason = "Selected-scene flattening cannot modify nodes shared by other scenes.";
                return false;
            }

            nodeIndex++;
        }

        bool changed = false;
        selectionIndex = 0;
        while (selectionIndex < selectedScenes.Count)
        {
            int sceneIndex = selectedScenes[selectionIndex];
            SceneGraphTraversal traversal = transforms.Graph.SceneTraversals[sceneIndex];
            GltfScene scene = document.Scenes[sceneIndex];
            scene.Nodes.Clear();
            int nodePosition = 0;
            while (nodePosition < traversal.Nodes.Count)
            {
                int selectedNode = traversal.Nodes[nodePosition];
                scene.Nodes.Add(selectedNode);
                nodePosition++;
            }

            result.ChangedObjects.Add($"scenes/{sceneIndex}");
            changed = true;
            selectionIndex++;
        }

        nodeIndex = 0;
        while (nodeIndex < activeNodes.Length)
        {
            if (activeNodes[nodeIndex])
            {
                StoreNodeMatrix(
                    document.Nodes[nodeIndex],
                    transforms.Nodes[nodeIndex].WorldTransform);
                document.Nodes[nodeIndex].Children.Clear();
                result.ChangedObjects.Add($"nodes/{nodeIndex}");
            }

            nodeIndex++;
        }

        return changed;
    }

    internal bool AlignSceneBounds(
        GltfDocument document,
        SceneTransformSettings settings,
        bool ground,
        GltfTransformStepResult result)
    {
        int sceneIndex = ResolveSceneIndex(document, settings);
        if (sceneIndex < 0
            || sceneIndex >= document.Scenes.Count
            || HasSceneAlignmentConflict(document, sceneIndex))
        {
            result.Status = GltfOperationStatus.Skipped;
            result.Reason = "Scene alignment skipped animation, skin, shared-root, or extension state.";
            return false;
        }

        GltfSceneEvaluationOptions options = new GltfSceneEvaluationOptions();
        options.HasScene = true;
        options.Scene = sceneIndex;
        options.CalculateCameras = false;
        options.CalculateSkins = false;
        options.IncludeMorphTargets = true;
        SceneBoundsEvaluation bounds = SceneEvaluator.CalculateDocumentSceneBounds(
            document,
            options);
        if (bounds.Status != GltfOperationStatus.Success || bounds.Bounds.IsEmpty)
        {
            result.Status = GltfOperationStatus.Skipped;
            result.Reason = "Scene alignment requires finite non-empty scene bounds.";
            return false;
        }

        Vector3 translation;
        if (ground)
        {
            translation = new Vector3(0.0f, -bounds.Bounds.Minimum.Y, 0.0f);
        }
        else
        {
            translation = -0.5f * (bounds.Bounds.Minimum + bounds.Bounds.Maximum);
        }

        if (translation == Vector3.Zero)
        {
            return false;
        }

        Matrix4x4 adjustment = Matrix4x4.CreateTranslation(translation);
        GltfScene scene = document.Scenes[sceneIndex];
        int rootPosition = 0;
        while (rootPosition < scene.Nodes.Count)
        {
            int nodeIndex = scene.Nodes[rootPosition];
            NodeTransformEvaluation local = TransformMath.EvaluateLocalTransform(
                document.Nodes[nodeIndex],
                nodeIndex);
            StoreNodeMatrix(
                document.Nodes[nodeIndex],
                local.LocalTransform * adjustment);
            result.ChangedObjects.Add($"nodes/{nodeIndex}");
            rootPosition++;
        }

        return true;
    }

    internal bool IsSelectedNode(SceneTransformSettings settings, int nodeIndex)
    {
        return !settings.HasNodes || settings.Nodes.Contains(nodeIndex);
    }

    internal bool CanChangeNodeTransform(GltfDocument document, int nodeIndex)
    {
        GltfNode node = document.Nodes[nodeIndex];
        return !node.HasSkin
            && !IsNodeAnimated(document, nodeIndex)
            && !IsNodeUsedBySkin(document, nodeIndex)
            && node.PropertyJson.Extensions.Count == 0
            && !node.PropertyJson.TypedExtensions.ContainsKey(
                GltfExtensionNames.ExtMeshGpuInstancing);
    }

    internal bool CanBakeNode(GltfDocument document, int nodeIndex)
    {
        GltfNode node = document.Nodes[nodeIndex];
        if (!node.HasMesh
            || node.Mesh < 0
            || node.Mesh >= document.Meshes.Count
            || node.HasCamera
            || node.Children.Count > 0
            || node.Weights != null
            || !CanChangeNodeTransform(document, nodeIndex)
            || CountMeshNodeReferences(document, node.Mesh) != 1)
        {
            return false;
        }

        GltfMesh mesh = document.Meshes[node.Mesh];
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
                || !Vertices.IsCompatiblePrimitive(primitive)
                || !PrimitiveUsesFloatingGeometry(document, primitive))
            {
                return false;
            }

            primitiveIndex++;
        }

        return true;
    }

    internal int CountMeshNodeReferences(GltfDocument document, int meshIndex)
    {
        int count = 0;
        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            if (document.Nodes[nodeIndex].HasMesh
                && document.Nodes[nodeIndex].Mesh == meshIndex)
            {
                count++;
            }

            nodeIndex++;
        }

        return count;
    }

    internal bool PrimitiveUsesFloatingGeometry(
        GltfDocument document,
        GltfMeshPrimitive primitive)
    {
        int attributeIndex = 0;
        while (attributeIndex < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[attributeIndex];
            if ((attribute.Semantic == "POSITION"
                    || attribute.Semantic == "NORMAL"
                    || attribute.Semantic == "TANGENT")
                && (attribute.Accessor < 0
                    || attribute.Accessor >= document.Accessors.Count
                    || document.Accessors[attribute.Accessor].ComponentType
                        != GltfComponentType.Float))
            {
                return false;
            }

            attributeIndex++;
        }

        return true;
    }

    internal void BakeMesh(
        GltfDocument document,
        int meshIndex,
        Matrix4x4 transform,
        Matrix4x4 normalTransform,
        bool reversesOrientation,
        GltfTransformStepResult result)
    {
        GltfMesh mesh = document.Meshes[meshIndex];
        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            int attributeIndex = 0;
            while (attributeIndex < primitive.Attributes.Count)
            {
                GltfAttribute attribute = primitive.Attributes[attributeIndex];
                int kind = 0;
                if (attribute.Semantic == "POSITION")
                {
                    kind = 1;
                }
                else if (attribute.Semantic == "NORMAL")
                {
                    kind = 2;
                }
                else if (attribute.Semantic == "TANGENT")
                {
                    kind = 3;
                }

                if (kind != 0)
                {
                    attribute.Accessor = BakeAccessor(
                        document,
                        attribute.Accessor,
                        kind,
                        transform,
                        normalTransform,
                        reversesOrientation);
                }

                attributeIndex++;
            }

            if (reversesOrientation && IsTriangleTopology(primitive.Mode))
            {
                PrimitiveVertexData? data = Vertices.ReadPrimitiveData(document, primitive);
                if (data != null)
                {
                    CoordinateConversion reflection = Converter.CreateAxisUnitConversion(
                        Matrix4x4.CreateScale(-1.0f, 1.0f, 1.0f),
                        1.0);
                    uint[] indices = Converter.ConvertPrimitiveIndices(
                        data.Indices,
                        primitive.Mode,
                        reflection);
                    int sourceAccessor = data.HasIndices
                        ? data.IndexAccessor
                        : GltfConstants.AbsentIndex;
                    GltfComponentType componentType = data.HasIndices
                        ? data.IndexComponentType
                        : Vertices.SelectIndexComponentType(data.VertexCount);
                    primitive.Indices = Vertices.WriteIndexAccessor(
                        document,
                        indices,
                        componentType,
                        sourceAccessor);
                    primitive.HasIndices = true;
                    if (data.HasIndices)
                    {
                        RecordSourceView(document, data.IndexAccessor);
                    }
                }
            }

            result.ChangedObjects.Add(
                $"meshes/{meshIndex}/primitives/{primitiveIndex}");
            primitiveIndex++;
        }
    }

    internal int BakeAccessor(
        GltfDocument document,
        int accessorIndex,
        int kind,
        Matrix4x4 transform,
        Matrix4x4 normalTransform,
        bool reversesOrientation)
    {
        int cacheIndex = 0;
        while (cacheIndex < BakeCache.Count)
        {
            BakedAccessorTransformCacheEntry cached = BakeCache[cacheIndex];
            if (cached.Source == accessorIndex
                && cached.Kind == kind
                && cached.Transform == transform
                && cached.NormalTransform == normalTransform
                && cached.ReversesOrientation == reversesOrientation)
            {
                return cached.Result;
            }

            cacheIndex++;
        }

        GltfAccessor source = document.Accessors[accessorIndex];
        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        GltfAccessorData decoded = Accessors.ReadFloatingData(request);
        if (decoded.Status != GltfOperationStatus.Success)
        {
            throw new GltfUnsupportedFeatureException(
                $"Accessor {accessorIndex} cannot be baked.");
        }

        double[] values = TransformBakedValues(
            decoded.FloatingValues,
            kind,
            transform,
            normalTransform,
            reversesOrientation);
        AccessorEncodingResult encoding = Encoder.EncodeFloatingAccessor(
            values,
            source.ComponentType,
            source.Type,
            source.Normalized);
        AccessorCreationResult created = Encoder.CreateAccessorStorage(
            document,
            encoding,
            true);
        Vertices.CopyAccessorMetadata(source, document.Accessors[created.Accessor]);
        RecordSourceView(document, accessorIndex);
        BakedAccessorTransformCacheEntry entry =
            new BakedAccessorTransformCacheEntry();
        entry.Source = accessorIndex;
        entry.Kind = kind;
        entry.Transform = transform;
        entry.NormalTransform = normalTransform;
        entry.ReversesOrientation = reversesOrientation;
        entry.Result = created.Accessor;
        BakeCache.Add(entry);
        return created.Accessor;
    }

    internal double[] TransformBakedValues(
        double[] source,
        int kind,
        Matrix4x4 transform,
        Matrix4x4 normalTransform,
        bool reversesOrientation)
    {
        int components = kind == 3 ? 4 : 3;
        if (source.Length % components != 0)
        {
            throw new GltfUnsupportedFeatureException(
                "Baked geometry accessor has an incompatible shape.");
        }

        double[] result = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Vector3 value = new Vector3(
                (float)source[index],
                (float)source[index + 1],
                (float)source[index + 2]);
            if (kind == 1)
            {
                value = Vector3.Transform(value, transform);
            }
            else if (kind == 2)
            {
                value = Vector3.TransformNormal(value, normalTransform);
                if (value != Vector3.Zero)
                {
                    value = Vector3.Normalize(value);
                }
            }
            else
            {
                value = Vector3.TransformNormal(value, transform);
                if (value != Vector3.Zero)
                {
                    value = Vector3.Normalize(value);
                }
            }

            result[index] = value.X;
            result[index + 1] = value.Y;
            result[index + 2] = value.Z;
            if (kind == 3)
            {
                result[index + 3] = reversesOrientation
                    ? -source[index + 3]
                    : source[index + 3];
            }

            index += components;
        }

        return result;
    }

    internal bool HasGlobalHierarchyConflict(GltfDocument document)
    {
        if (document.Animations.Count > 0 || document.Skins.Count > 0)
        {
            return true;
        }

        int sceneIndex = 0;
        while (sceneIndex < document.Scenes.Count)
        {
            GltfPropertyJson property = document.Scenes[sceneIndex].PropertyJson;
            if (property.Extensions.Count > 0 || property.TypedExtensions.Count > 0)
            {
                return true;
            }

            sceneIndex++;
        }

        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfPropertyJson property = document.Nodes[nodeIndex].PropertyJson;
            if (property.Extensions.Count > 0 || property.TypedExtensions.Count > 0)
            {
                return true;
            }

            nodeIndex++;
        }

        return false;
    }

    internal bool HasSceneAlignmentConflict(GltfDocument document, int sceneIndex)
    {
        if (document.Animations.Count > 0
            || document.Skins.Count > 0
            || document.Scenes[sceneIndex].PropertyJson.Extensions.Count > 0
            || document.Scenes[sceneIndex].PropertyJson.TypedExtensions.Count > 0)
        {
            return true;
        }

        SceneGraphAnalysis graph = new SceneGraphEvaluator().AnalyzeDocument(document);
        if (!graph.IsValid)
        {
            return true;
        }

        GltfScene scene = document.Scenes[sceneIndex];
        SceneGraphTraversal traversal = graph.SceneTraversals[sceneIndex];
        int nodePosition = 0;
        while (nodePosition < traversal.Nodes.Count)
        {
            int sceneNode = traversal.Nodes[nodePosition];
            GltfPropertyJson nodeProperty = document.Nodes[sceneNode].PropertyJson;
            if (graph.NodeScenes[sceneNode].Count != 1
                || nodeProperty.Extensions.Count > 0
                || nodeProperty.TypedExtensions.ContainsKey(
                    GltfExtensionNames.ExtMeshGpuInstancing))
            {
                return true;
            }

            nodePosition++;
        }

        int rootPosition = 0;
        while (rootPosition < scene.Nodes.Count)
        {
            int root = scene.Nodes[rootPosition];
            if (graph.RootSceneReferences[root].Count != 1
                || document.Nodes[root].PropertyJson.Extensions.Count > 0
                || document.Nodes[root].PropertyJson.TypedExtensions.ContainsKey(
                    GltfExtensionNames.ExtMeshGpuInstancing))
            {
                return true;
            }

            rootPosition++;
        }

        return false;
    }

    internal List<int> SelectSceneIndices(
        GltfDocument document,
        SceneTransformSettings settings)
    {
        List<int> result = new List<int>();
        if (settings.HasScene)
        {
            result.Add(settings.Scene);
            return result;
        }

        int sceneIndex = 0;
        while (sceneIndex < document.Scenes.Count)
        {
            result.Add(sceneIndex);
            sceneIndex++;
        }

        return result;
    }

    internal int ResolveSceneIndex(
        GltfDocument document,
        SceneTransformSettings settings)
    {
        if (settings.HasScene)
        {
            return settings.Scene;
        }

        if (document.HasDefaultScene)
        {
            return document.DefaultScene;
        }

        return document.Scenes.Count > 0 ? 0 : GltfConstants.AbsentIndex;
    }

    internal bool IsNodeAnimated(GltfDocument document, int nodeIndex)
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

    internal bool IsNodeUsedBySkin(GltfDocument document, int nodeIndex)
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

    internal bool IsTriangleTopology(GltfPrimitiveMode mode)
    {
        return mode == GltfPrimitiveMode.Triangles
            || mode == GltfPrimitiveMode.TriangleStrip
            || mode == GltfPrimitiveMode.TriangleFan;
    }

    internal void StoreNodeMatrix(GltfNode node, Matrix4x4 matrix)
    {
        node.HasMatrix = true;
        node.Matrix = Converter.WriteColumnMajorMatrix(matrix);
        node.HasTranslation = false;
        node.Translation = [0.0, 0.0, 0.0];
        node.HasRotation = false;
        node.Rotation = [0.0, 0.0, 0.0, 1.0];
        node.HasScale = false;
        node.Scale = [1.0, 1.0, 1.0];
    }

    internal void ResetNodeTransform(GltfNode node)
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

    internal void RecordSourceView(GltfDocument document, int accessorIndex)
    {
        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count)
        {
            return;
        }

        GltfAccessor accessor = document.Accessors[accessorIndex];
        if (accessor.HasBufferView
            && accessor.BufferView >= 0
            && accessor.BufferView < document.BufferViews.Count
            && !SourceBufferViews.Contains(accessor.BufferView))
        {
            SourceBufferViews.Add(accessor.BufferView);
        }
    }
}
