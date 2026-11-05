using System;
using System.Collections.Generic;
using System.Globalization;

namespace gltf;

internal class SkinValidator
{
    internal SkinEvaluator SkinEvaluator = new SkinEvaluator();
    internal AccessorCodecEngine AccessorCodec = new AccessorCodecEngine();
    internal AttributeSemanticParser SemanticParser = new AttributeSemanticParser();

    internal GltfValidationReport ValidateDocumentSkinRules(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        GltfValidationReport report = new GltfValidationReport();
        ValidateSkins(document, report);
        ValidateSkinnedNodes(document, report);
        return report;
    }

    internal void ValidateSkins(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Skins == null)
        {
            return;
        }

        int skinIndex = 0;
        while (skinIndex < document.Skins.Count)
        {
            if (document.Skins[skinIndex] != null)
            {
                SkinEvaluation evaluation = SkinEvaluator.EvaluateSkin(
                    document,
                    skinIndex);
                CopySkinIssues(evaluation.Diagnostics, report);
            }

            skinIndex++;
        }
    }

    internal void ValidateSkinnedNodes(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Nodes == null)
        {
            return;
        }

        SceneGraphAnalysis graph = new SceneGraphEvaluator().AnalyzeDocument(document);
        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[nodeIndex];
            if (node != null)
            {
                ValidateNodeSkin(document, node, nodeIndex, graph, report);
            }

            nodeIndex++;
        }
    }

    internal void ValidateNodeSkin(
        GltfDocument document,
        GltfNode node,
        int nodeIndex,
        SceneGraphAnalysis graph,
        GltfValidationReport report)
    {
        GltfMesh? mesh = ResolveNodeMesh(document, node);
        bool meshHasJointData = MeshHasJointData(mesh);
        string pointer = $"/nodes/{nodeIndex}";
        if (!node.HasSkin)
        {
            if (meshHasJointData)
            {
                AddSkinValidationIssue(
                    report,
                    GltfIssueCodes.NodeSkinnedMeshWithoutSkin,
                    GltfValidationSeverity.Warning,
                    "The node uses mesh joint attributes without defining a skin.",
                    $"{pointer}/mesh");
            }

            return;
        }

        if (mesh == null || !meshHasJointData)
        {
            AddSkinValidationIssue(
                report,
                GltfIssueCodes.NodeSkinWithNonSkinnedMesh,
                GltfValidationSeverity.Error,
                "The node defines a skin but its mesh has no joint and weight attributes.",
                $"{pointer}/skin");
        }

        ValidateSkinnedNodeTransforms(document, node, nodeIndex, graph, report);
        GltfSkin? skin = ResolveNodeSkin(document, node);
        if (mesh == null || skin == null || skin.Joints == null)
        {
            return;
        }

        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            if (primitive != null)
            {
                bool hasJoints = PrimitiveHasSemanticKind(
                    primitive,
                    AttributeSemanticKind.Joints);
                bool hasWeights = PrimitiveHasSemanticKind(
                    primitive,
                    AttributeSemanticKind.Weights);
                if (!hasJoints || !hasWeights)
                {
                    AddSkinValidationIssue(
                        report,
                        GltfIssueCodes.NodeSkinWithNonSkinnedMesh,
                        GltfValidationSeverity.Error,
                        $"Mesh primitive {primitiveIndex} does not define paired joint and weight attributes.",
                        $"{pointer}/skin");
                }
                else
                {
                    ValidatePrimitiveInfluences(
                        document,
                        primitive,
                        node.Skin,
                        skin.Joints.Count,
                        report);
                }
            }

            primitiveIndex++;
        }
    }

    internal GltfMesh? ResolveNodeMesh(GltfDocument document, GltfNode node)
    {
        if (!node.HasMesh
            || document.Meshes == null
            || node.Mesh < 0
            || node.Mesh >= document.Meshes.Count)
        {
            return null;
        }

        return document.Meshes[node.Mesh];
    }

    internal GltfSkin? ResolveNodeSkin(GltfDocument document, GltfNode node)
    {
        if (!node.HasSkin
            || document.Skins == null
            || node.Skin < 0
            || node.Skin >= document.Skins.Count)
        {
            return null;
        }

        return document.Skins[node.Skin];
    }

    internal bool MeshHasJointData(GltfMesh? mesh)
    {
        if (mesh == null || mesh.Primitives == null)
        {
            return false;
        }

        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            if (primitive != null
                && PrimitiveHasSemanticKind(primitive, AttributeSemanticKind.Joints))
            {
                return true;
            }

            primitiveIndex++;
        }

        return false;
    }

    internal bool PrimitiveHasSemanticKind(
        GltfMeshPrimitive primitive,
        AttributeSemanticKind kind)
    {
        if (primitive.Attributes == null)
        {
            return false;
        }

        int attributeIndex = 0;
        while (attributeIndex < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[attributeIndex];
            if (attribute != null && attribute.Semantic != null)
            {
                AttributeSemantic semantic = SemanticParser.ParseAttributeSemantic(
                    attribute.Semantic);
                if (semantic.Valid && semantic.Kind == kind)
                {
                    return true;
                }
            }

            attributeIndex++;
        }

        return false;
    }

    internal void ValidateSkinnedNodeTransforms(
        GltfDocument document,
        GltfNode node,
        int nodeIndex,
        SceneGraphAnalysis graph,
        GltfValidationReport report)
    {
        string pointer = $"/nodes/{nodeIndex}";
        if (HasNodeLocalTransform(node))
        {
            AddSkinValidationIssue(
                report,
                GltfIssueCodes.NodeSkinnedMeshLocalTransforms,
                GltfValidationSeverity.Warning,
                "Local transforms on a skinned mesh node do not affect skinning.",
                pointer);
        }

        if (nodeIndex < graph.ParentReferences.Count
            && graph.ParentReferences[nodeIndex].Count > 0)
        {
            AddSkinValidationIssue(
                report,
                GltfIssueCodes.NodeSkinnedMeshNonRoot,
                GltfValidationSeverity.Information,
                "The skinned mesh node is not a hierarchy root.",
                pointer);
        }

        if (HasTransformedAncestor(document, nodeIndex, graph))
        {
            AddSkinValidationIssue(
                report,
                GltfIssueCodes.NodeSkinnedMeshParentTransforms,
                GltfValidationSeverity.Warning,
                "An ancestor of the skinned mesh node has a local transform.",
                pointer);
        }
    }

    internal bool HasNodeLocalTransform(GltfNode node)
    {
        return node.HasMatrix
            || node.HasRotation
            || node.HasScale
            || node.HasTranslation;
    }

    internal bool HasTransformedAncestor(
        GltfDocument document,
        int nodeIndex,
        SceneGraphAnalysis graph)
    {
        if (nodeIndex < 0 || nodeIndex >= graph.Parents.Count)
        {
            return false;
        }

        int parent = graph.Parents[nodeIndex];
        int steps = 0;
        while (parent != GltfConstants.AbsentIndex
            && parent >= 0
            && parent < document.Nodes.Count
            && steps <= document.Nodes.Count)
        {
            GltfNode ancestor = document.Nodes[parent];
            if (ancestor != null && HasNodeLocalTransform(ancestor))
            {
                return true;
            }

            parent = graph.Parents[parent];
            steps++;
        }

        return false;
    }

    internal void ValidatePrimitiveInfluences(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        int skinIndex,
        int jointCount,
        GltfValidationReport report)
    {
        List<SkinInfluenceSet> sets = new List<SkinInfluenceSet>();
        int attributeIndex = 0;
        while (attributeIndex < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[attributeIndex];
            if (attribute != null && attribute.Semantic != null)
            {
                AttributeSemantic semantic = SemanticParser.ParseAttributeSemantic(
                    attribute.Semantic);
                if (semantic.Valid
                    && semantic.Kind == AttributeSemanticKind.Joints
                    && semantic.HasSetIndex)
                {
                    SkinInfluenceSet? set = ReadInfluenceSet(
                        document,
                        primitive,
                        attribute,
                        semantic.SetIndex,
                        report);
                    if (set != null)
                    {
                        sets.Add(set);
                    }
                }
            }

            attributeIndex++;
        }

        if (sets.Count == 0)
        {
            return;
        }

        SkinInfluenceValidation validation = SkinEvaluator.ValidateInfluences(
            sets,
            jointCount,
            skinIndex);
        CopySkinIssues(validation.Diagnostics, report);
    }

    internal SkinInfluenceSet? ReadInfluenceSet(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        GltfAttribute jointAttribute,
        int setIndex,
        GltfValidationReport report)
    {
        string weightSemantic = GltfSemantics.WeightsPrefix
            + setIndex.ToString(CultureInfo.InvariantCulture);
        int weightAccessor = FindAttributeAccessor(primitive, weightSemantic);
        if (weightAccessor == GltfConstants.AbsentIndex)
        {
            AddSkinValidationIssue(
                report,
                GltfIssueCodes.MeshPrimitiveJointsWeightsMismatch,
                GltfValidationSeverity.Error,
                $"JOINTS_{setIndex} does not have a matching {weightSemantic} attribute.",
                $"/skins/influences/{setIndex}");
            return null;
        }

        GltfAccessorReadRequest jointRequest = new GltfAccessorReadRequest();
        jointRequest.Document = document;
        jointRequest.Accessor = jointAttribute.Accessor;
        GltfAccessorData joints = AccessorCodec.ReadUnsignedData(jointRequest);
        GltfAccessorReadRequest weightRequest = new GltfAccessorReadRequest();
        weightRequest.Document = document;
        weightRequest.Accessor = weightAccessor;
        weightRequest.ApplyNormalization = true;
        GltfAccessorData weights = AccessorCodec.ReadFloatingData(weightRequest);
        if (joints.Status != GltfOperationStatus.Success
            || weights.Status != GltfOperationStatus.Success)
        {
            CopyAccessorIssue(joints, report);
            CopyAccessorIssue(weights, report);
            return null;
        }

        SkinInfluenceSet result = new SkinInfluenceSet();
        result.Set = setIndex;
        result.Joints = joints.UnsignedValues;
        result.Weights = weights.FloatingValues;
        return result;
    }

    internal int FindAttributeAccessor(
        GltfMeshPrimitive primitive,
        string semantic)
    {
        int attributeIndex = 0;
        while (attributeIndex < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[attributeIndex];
            if (attribute != null && attribute.Semantic == semantic)
            {
                return attribute.Accessor;
            }

            attributeIndex++;
        }

        return GltfConstants.AbsentIndex;
    }

    internal void CopyAccessorIssue(
        GltfAccessorData data,
        GltfValidationReport report)
    {
        if (data.Status != GltfOperationStatus.Success && data.Diagnostic != null)
        {
            report.Issues.Add(data.Diagnostic);
            if (data.Diagnostic.Severity == GltfValidationSeverity.Error)
            {
                report.IsValid = false;
            }
        }
    }

    internal void CopySkinIssues(
        List<GltfValidationIssue> issues,
        GltfValidationReport report)
    {
        int index = 0;
        while (index < issues.Count)
        {
            GltfValidationIssue issue = issues[index];
            report.Issues.Add(issue);
            if (issue.Severity == GltfValidationSeverity.Error)
            {
                report.IsValid = false;
            }

            index++;
        }
    }

    internal void AddSkinValidationIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = severity;
        issue.Message = message;
        issue.JsonPointer = pointer;
        report.Issues.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            report.IsValid = false;
        }
    }
}
