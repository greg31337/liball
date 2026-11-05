using System;
using System.Collections.Generic;
using System.Numerics;

namespace gltf.Tests.Internal.Evaluation;

[TestFixture]
public class SkinEvaluatorTests
{
    [Test]
    public void EvaluateSkin_FindsClosestCommonRootScenesAndReferencingNodes()
    {
        GltfDocument document = CreateDocumentWithNodes(4);
        document.Nodes[0].Children.Add(1);
        document.Nodes[0].Children.Add(2);
        document.Nodes[1].Children.Add(3);
        document.Nodes[2].HasSkin = true;
        document.Nodes[2].Skin = 0;
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        document.Scenes.Add(scene);
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(1);
        skin.Joints.Add(3);
        skin.HasSkeleton = true;
        skin.Skeleton = 0;
        document.Skins.Add(skin);

        SkinEvaluation result = new SkinEvaluator().EvaluateSkin(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasCommonRoot, Is.True);
            Assert.That(result.CommonRoot, Is.EqualTo(1));
            Assert.That(result.SkeletonValid, Is.True);
            Assert.That(result.SameScene, Is.True);
            Assert.That(result.CommonScenes, Is.EqualTo(new int[] { 0 }));
            Assert.That(result.ReferencingNodes, Is.EqualTo(new int[] { 2 }));
            Assert.That(result.ReferencingScenes, Is.EqualTo(new int[] { 0 }));
            Assert.That(result.UsesDefaultInverseBindMatrices, Is.True);
            Assert.That(result.InverseBindMatrices, Has.Count.EqualTo(2));
            Assert.That(result.InverseBindMatrices[0], Is.EqualTo(Matrix4x4.Identity));
            Assert.That(result.HasPalette, Is.True);
            Assert.That(result.JointMatrices, Is.EqualTo(new[] { Matrix4x4.Identity, Matrix4x4.Identity }));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void EvaluateSkin_ReportsMissingCommonRootAndInvalidSkeleton()
    {
        GltfDocument disconnected = CreateDocumentWithNodes(2);
        GltfSkin disconnectedSkin = new GltfSkin();
        disconnectedSkin.Joints.Add(0);
        disconnectedSkin.Joints.Add(1);
        disconnectedSkin.HasSkeleton = true;
        disconnectedSkin.Skeleton = 0;
        disconnected.Skins.Add(disconnectedSkin);

        GltfDocument invalidSkeleton = CreateDocumentWithNodes(4);
        invalidSkeleton.Nodes[0].Children.Add(1);
        invalidSkeleton.Nodes[1].Children.Add(2);
        GltfSkin skeletonSkin = new GltfSkin();
        skeletonSkin.Joints.Add(1);
        skeletonSkin.Joints.Add(2);
        skeletonSkin.HasSkeleton = true;
        skeletonSkin.Skeleton = 3;
        invalidSkeleton.Skins.Add(skeletonSkin);
        SkinEvaluator evaluator = new SkinEvaluator();

        SkinEvaluation disconnectedResult = evaluator.EvaluateSkin(disconnected, 0);
        SkinEvaluation skeletonResult = evaluator.EvaluateSkin(invalidSkeleton, 0);

        Assert.Multiple(() =>
        {
            Assert.That(disconnectedResult.HasCommonRoot, Is.False);
            Assert.That(disconnectedResult.SkeletonValid, Is.False);
            Assert.That(HasIssue(disconnectedResult.Diagnostics, GltfIssueCodes.SkinNoCommonRoot), Is.True);
            Assert.That(skeletonResult.CommonRoot, Is.EqualTo(1));
            Assert.That(skeletonResult.SkeletonValid, Is.False);
            Assert.That(HasIssue(skeletonResult.Diagnostics, GltfIssueCodes.SkinSkeletonInvalid), Is.True);
        });
    }

    [Test]
    public void EvaluateSkin_ReportsWhenSkinnedNodeSceneOmitsJoints()
    {
        GltfDocument document = CreateDocumentWithNodes(2);
        document.Nodes[0].HasSkin = true;
        document.Nodes[0].Skin = 0;
        GltfScene meshScene = new GltfScene();
        meshScene.Nodes.Add(0);
        GltfScene jointScene = new GltfScene();
        jointScene.Nodes.Add(1);
        document.Scenes.Add(meshScene);
        document.Scenes.Add(jointScene);
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(1);
        document.Skins.Add(skin);

        SkinEvaluation result = new SkinEvaluator().EvaluateSkin(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.SameScene, Is.False);
            Assert.That(result.CommonScenes, Is.EqualTo(new int[] { 1 }));
            Assert.That(result.ReferencingScenes, Is.EqualTo(new int[] { 0 }));
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.NodeSkinNoScene), Is.True);
        });
    }

    [Test]
    public void EvaluateSkin_RejectsJointWhoseAncestorPathEntersACycle()
    {
        GltfDocument document = CreateDocumentWithNodes(3);
        document.Nodes[0].Children.Add(1);
        document.Nodes[1].Children.Add(0);
        document.Nodes[1].Children.Add(2);
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(2);
        document.Skins.Add(skin);

        SkinEvaluation result = new SkinEvaluator().EvaluateSkin(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.JointReferencesValid, Is.False);
            Assert.That(result.HasCommonRoot, Is.False);
            Assert.That(result.HasPalette, Is.False);
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.NodeLoop), Is.True);
        });
    }

    [Test]
    public void EvaluateSkin_ReadsInverseBindMatricesAndCalculatesPaletteInJointOrder()
    {
        GltfDocument document = CreateDocumentWithNodes(2);
        document.Nodes[0].HasTranslation = true;
        document.Nodes[0].Translation = [1.0, 0.0, 0.0];
        document.Nodes[0].Children.Add(1);
        document.Nodes[1].HasTranslation = true;
        document.Nodes[1].Translation = [1.0, 0.0, 0.0];
        Matrix4x4 firstInverse = Matrix4x4.CreateTranslation(-1.0f, 0.0f, 0.0f);
        Matrix4x4 secondInverse = Matrix4x4.CreateTranslation(-2.0f, 0.0f, 0.0f);
        int accessor = CreateMatrixAccessor(document, firstInverse, secondInverse);
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(0);
        skin.Joints.Add(1);
        skin.HasInverseBindMatrices = true;
        skin.InverseBindMatrices = accessor;
        document.Skins.Add(skin);

        SkinEvaluation result = new SkinEvaluator().EvaluateSkin(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.UsesDefaultInverseBindMatrices, Is.False);
            Assert.That(result.InverseBindMatrices, Is.EqualTo(new[] { firstInverse, secondInverse }));
            Assert.That(result.HasPalette, Is.True);
            Assert.That(result.JointMatrices, Has.Count.EqualTo(2));
            Assert.That(result.JointMatrices[0], Is.EqualTo(Matrix4x4.Identity));
            Assert.That(result.JointMatrices[1], Is.EqualTo(Matrix4x4.Identity));
        });
    }

    [Test]
    public void EvaluateSkin_ReportsInvalidInverseBindFormatRowsAndStride()
    {
        GltfDocument invalidFormat = CreateDocumentWithNodes(1);
        GltfAccessor formatAccessor = new GltfAccessor();
        formatAccessor.ComponentType = GltfComponentType.Float;
        formatAccessor.Type = GltfAccessorType.Matrix3;
        formatAccessor.Count = 1;
        invalidFormat.Accessors.Add(formatAccessor);
        AddSkinWithAccessor(invalidFormat, 0);

        GltfDocument invalidMatrix = CreateDocumentWithNodes(1);
        Matrix4x4 malformed = Matrix4x4.Identity;
        malformed.M14 = 1.0f;
        int malformedAccessor = CreateMatrixAccessor(invalidMatrix, malformed);
        invalidMatrix.BufferViews[0].HasByteStride = true;
        invalidMatrix.BufferViews[0].ByteStride = 64;
        AddSkinWithAccessor(invalidMatrix, malformedAccessor);
        SkinEvaluator evaluator = new SkinEvaluator();

        SkinEvaluation formatResult = evaluator.EvaluateSkin(invalidFormat, 0);
        SkinEvaluation matrixResult = evaluator.EvaluateSkin(invalidMatrix, 0);

        Assert.Multiple(() =>
        {
            Assert.That(formatResult.IsValid, Is.False);
            Assert.That(HasIssue(formatResult.Diagnostics, GltfIssueCodes.SkinIbmInvalidFormat), Is.True);
            Assert.That(matrixResult.IsValid, Is.False);
            Assert.That(
                HasIssue(matrixResult.Diagnostics, GltfIssueCodes.SkinIbmAccessorWithBytestride),
                Is.True);
            Assert.That(HasIssue(matrixResult.Diagnostics, GltfIssueCodes.AccessorInvalidIbm), Is.True);
            Assert.That(matrixResult.InverseBindMatrices, Has.Count.EqualTo(1));
            Assert.That(matrixResult.HasPalette, Is.True);
        });
    }

    [Test]
    public void EvaluateSkin_PropagatesUnavailableInverseBindData()
    {
        GltfDocument document = CreateDocumentWithNodes(1);
        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Type = GltfAccessorType.Matrix4;
        accessor.Count = 1;
        document.Accessors.Add(accessor);
        AddSkinWithAccessor(document, 0);

        SkinEvaluation result = new SkinEvaluator().EvaluateSkin(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.InverseBindMatrices, Is.Empty);
            Assert.That(result.HasPalette, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(GltfIssueCodes.UnresolvedReference));
        });
    }

    [Test]
    public void ValidateInfluences_AcceptsNormalizedDistinctInfluencesAcrossSets()
    {
        SkinInfluenceSet first = new SkinInfluenceSet();
        first.Set = 0;
        first.Joints = [0, 1, 0, 0, 2, 3, 0, 0];
        first.Weights = [0.5, 0.5, 0.0, 0.0, 0.25, 0.25, 0.0, 0.0];
        SkinInfluenceSet second = new SkinInfluenceSet();
        second.Set = 1;
        second.Joints = [0, 0, 0, 0, 4, 5, 0, 0];
        second.Weights = [0.0, 0.0, 0.0, 0.0, 0.25, 0.25, 0.0, 0.0];

        SkinInfluenceValidation result = new SkinEvaluator().ValidateInfluences(
            new List<SkinInfluenceSet> { first, second },
            6,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.VertexCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ValidateInfluences_ReportsRangeDuplicateZeroWeightAndNormalizationIssues()
    {
        SkinInfluenceSet first = new SkinInfluenceSet();
        first.Set = 0;
        first.Joints = [1, 1, 7, 3];
        first.Weights = [0.5, 0.25, -0.25, 0.0];
        SkinInfluenceSet second = new SkinInfluenceSet();
        second.Set = 1;
        second.Joints = [2, 0, 0, 0];
        second.Weights = [0.25, 0.0, 0.0, 0.0];

        SkinInfluenceValidation result = new SkinEvaluator().ValidateInfluences(
            new List<SkinInfluenceSet> { first, second },
            4,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.AccessorJointsIndexDuplicate), Is.True);
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.AccessorJointsIndexOob), Is.True);
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.AccessorWeightsNegative), Is.True);
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.AccessorJointsUsedZeroWeight), Is.True);
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.AccessorWeightsNonNormalized), Is.True);
            Assert.That(
                FindIssue(result.Diagnostics, GltfIssueCodes.AccessorJointsUsedZeroWeight)!.Severity,
                Is.EqualTo(GltfValidationSeverity.Warning));
        });
    }

    [Test]
    public void ValidateInfluences_RejectsMismatchedSetLayouts()
    {
        SkinInfluenceSet first = new SkinInfluenceSet();
        first.Joints = [0, 0, 0, 0];
        first.Weights = [1.0, 0.0, 0.0];

        SkinInfluenceValidation result = new SkinEvaluator().ValidateInfluences(
            new List<SkinInfluenceSet> { first },
            1,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.VertexCount, Is.Zero);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(GltfIssueCodes.MeshPrimitiveJointsWeightsMismatch));
        });
    }

    [Test]
    public void EvaluateSkin_ThrowsForNullDocument()
    {
        Assert.That(
            () => new SkinEvaluator().EvaluateSkin(null!, 0),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
    }

    private static GltfDocument CreateDocumentWithNodes(int nodeCount)
    {
        GltfDocument result = new GltfDocument();
        int nodeIndex = 0;
        while (nodeIndex < nodeCount)
        {
            result.Nodes.Add(new GltfNode());
            nodeIndex++;
        }

        return result;
    }

    private static GltfDocument CreateDocument(params GltfNode[] nodes)
    {
        GltfDocument result = new GltfDocument();
        int nodeIndex = 0;
        while (nodeIndex < nodes.Length)
        {
            result.Nodes.Add(nodes[nodeIndex]);
            nodeIndex++;
        }

        return result;
    }

    private static int CreateMatrixAccessor(GltfDocument document, params Matrix4x4[] matrices)
    {
        CoordinateConverter conversion = new CoordinateConverter();
        double[] values = new double[matrices.Length * 16];
        int matrixIndex = 0;
        while (matrixIndex < matrices.Length)
        {
            conversion.WriteColumnMajorMatrix(matrices[matrixIndex]).CopyTo(
                values,
                matrixIndex * 16);
            matrixIndex++;
        }

        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.ComponentType = GltfComponentType.Float;
        request.Type = GltfAccessorType.Matrix4;
        request.FloatingValues = values;
        GltfAccessorData result = new GltfAccessorCodec().CreateDenseAccessor(request);
        return result.Accessor;
    }

    private static void AddSkinWithAccessor(GltfDocument document, int accessor)
    {
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(0);
        skin.HasInverseBindMatrices = true;
        skin.InverseBindMatrices = accessor;
        document.Skins.Add(skin);
    }

    private static bool HasIssue(List<GltfValidationIssue> issues, string code)
    {
        return FindIssue(issues, code) != null;
    }

    private static GltfValidationIssue? FindIssue(List<GltfValidationIssue> issues, string code)
    {
        int issueIndex = 0;
        while (issueIndex < issues.Count)
        {
            if (issues[issueIndex].Code == code)
            {
                return issues[issueIndex];
            }

            issueIndex++;
        }

        return null;
    }
}
