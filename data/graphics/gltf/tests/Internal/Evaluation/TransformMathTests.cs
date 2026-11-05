using System;
using System.Numerics;

namespace gltf.Tests.Internal.Evaluation;

[TestFixture]
public class TransformMathTests
{
    [Test]
    public void EvaluateDocumentTransforms_UsesIdentityForAbsentTrsBackingValues()
    {
        GltfNode node = new GltfNode();
        node.Translation = [double.NaN];
        node.Rotation = [double.NaN];
        node.Scale = [double.NaN];
        GltfDocument document = CreateDocument(node);

        SceneTransformAnalysis result = new TransformMath().EvaluateDocumentTransforms(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Nodes[0].HasLocalTransform, Is.True);
            Assert.That(result.Nodes[0].LocalTransform, Is.EqualTo(Matrix4x4.Identity));
            Assert.That(result.Nodes[0].WorldTransform, Is.EqualTo(Matrix4x4.Identity));
            Assert.That(result.Nodes[0].HasNormalTransform, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void EvaluateDocumentTransforms_ComposesScaleRotationAndTranslationInGltfOrder()
    {
        GltfNode node = new GltfNode();
        node.HasScale = true;
        node.Scale = [2.0, 3.0, 4.0];
        node.HasRotation = true;
        node.Rotation = [0.0, 0.0, Math.Sqrt(0.5), Math.Sqrt(0.5)];
        node.HasTranslation = true;
        node.Translation = [10.0, 20.0, 30.0];
        GltfDocument document = CreateDocument(node);

        NodeTransformEvaluation result = new TransformMath()
            .EvaluateDocumentTransforms(document)
            .Nodes[0];
        Vector3 transformed = Vector3.Transform(new Vector3(1.0f, 0.0f, 0.0f), result.LocalTransform);

        Assert.Multiple(() =>
        {
            Assert.That(transformed.X, Is.EqualTo(10.0f).Within(0.00001f));
            Assert.That(transformed.Y, Is.EqualTo(22.0f).Within(0.00001f));
            Assert.That(transformed.Z, Is.EqualTo(30.0f).Within(0.00001f));
            Assert.That(result.WorldTransform, Is.EqualTo(result.LocalTransform));
            Assert.That(result.WorldDeterminant, Is.EqualTo(24.0).Within(0.0001));
            Assert.That(result.HasNegativeWorldDeterminant, Is.False);
        });
    }

    [Test]
    public void EvaluateDocumentTransforms_ConvertsColumnMajorMatricesAndComposesWorldTransforms()
    {
        GltfNode parent = new GltfNode();
        parent.HasMatrix = true;
        parent.Matrix =
        [
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            10.0, 0.0, 0.0, 1.0
        ];
        parent.Children.Add(1);
        GltfNode child = new GltfNode();
        child.HasTranslation = true;
        child.Translation = [0.0, 2.0, 0.0];
        GltfDocument document = CreateDocument(parent, child);

        SceneTransformAnalysis result = new TransformMath().EvaluateDocumentTransforms(document);
        Vector3 transformed = Vector3.Transform(Vector3.Zero, result.Nodes[1].WorldTransform);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Nodes[0].LocalTransform.M41, Is.EqualTo(10.0f));
            Assert.That(result.Nodes[0].MatrixDecomposable, Is.True);
            Assert.That(transformed, Is.EqualTo(new Vector3(10.0f, 2.0f, 0.0f)));
            Assert.That(result.Nodes[1].HasWorldTransform, Is.True);
        });
    }

    [Test]
    public void EvaluateDocumentTransforms_NormalizesQuaternionAndReportsNonUnitInput()
    {
        GltfNode node = new GltfNode();
        node.HasRotation = true;
        node.Rotation = [0.0, 0.0, Math.Sqrt(2.0), Math.Sqrt(2.0)];
        GltfDocument document = CreateDocument(node);

        SceneTransformAnalysis analysis = new TransformMath().EvaluateDocumentTransforms(document);
        NodeTransformEvaluation result = analysis.Nodes[0];
        Vector3 transformed = Vector3.Transform(Vector3.UnitX, result.LocalTransform);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.IsValid, Is.False);
            Assert.That(result.RotationWasNormalized, Is.True);
            Assert.That(result.HasWorldTransform, Is.True);
            Assert.That(transformed.X, Is.Zero.Within(0.00001f));
            Assert.That(transformed.Y, Is.EqualTo(1.0f).Within(0.00001f));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(GltfIssueCodes.RotationNonUnit));
            Assert.That(result.Diagnostics[0].JsonPointer, Is.EqualTo("/nodes/0/rotation"));
        });
    }

    [Test]
    public void EvaluateDocumentTransforms_ReportsNonDecomposableMatrixAndMatrixTrsConflict()
    {
        GltfNode node = new GltfNode();
        node.HasMatrix = true;
        node.Matrix =
        [
            1.0, 0.5, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 1.0
        ];
        node.HasTranslation = true;
        node.Translation = [100.0, 200.0, 300.0];
        GltfDocument document = CreateDocument(node);

        SceneTransformAnalysis analysis = new TransformMath().EvaluateDocumentTransforms(document);
        NodeTransformEvaluation result = analysis.Nodes[0];

        Assert.Multiple(() =>
        {
            Assert.That(analysis.IsValid, Is.False);
            Assert.That(result.HasLocalTransform, Is.True);
            Assert.That(result.HasWorldTransform, Is.True);
            Assert.That(result.MatrixDecomposable, Is.False);
            Assert.That(result.LocalTransform.M12, Is.EqualTo(0.5f));
            Assert.That(result.LocalTransform.M41, Is.Zero);
            Assert.That(
                result.Diagnostics.ConvertAll(issue => issue.Code),
                Is.EqualTo(new string[]
                {
                    GltfIssueCodes.NodeMatrixTrs,
                    GltfIssueCodes.NodeMatrixNonTrs
                }));
        });
    }

    [Test]
    public void EvaluateDocumentTransforms_CalculatesNormalMatrixAndNegativeDeterminant()
    {
        GltfNode reflected = new GltfNode();
        reflected.HasScale = true;
        reflected.Scale = [-2.0, 4.0, 5.0];
        GltfNode singular = new GltfNode();
        singular.HasScale = true;
        singular.Scale = [0.0, 2.0, 3.0];
        GltfDocument document = CreateDocument(reflected, singular);

        SceneTransformAnalysis analysis = new TransformMath().EvaluateDocumentTransforms(document);
        NodeTransformEvaluation reflectedResult = analysis.Nodes[0];
        NodeTransformEvaluation singularResult = analysis.Nodes[1];

        Assert.Multiple(() =>
        {
            Assert.That(reflectedResult.WorldDeterminant, Is.EqualTo(-40.0));
            Assert.That(reflectedResult.HasNegativeWorldDeterminant, Is.True);
            Assert.That(reflectedResult.HasNormalTransform, Is.True);
            Assert.That(reflectedResult.NormalTransform.M11, Is.EqualTo(-0.5f));
            Assert.That(reflectedResult.NormalTransform.M22, Is.EqualTo(0.25f));
            Assert.That(reflectedResult.NormalTransform.M33, Is.EqualTo(0.2f));
            Assert.That(reflectedResult.NormalTransform.M41, Is.Zero);
            Assert.That(singularResult.WorldDeterminant, Is.Zero);
            Assert.That(singularResult.HasNegativeWorldDeterminant, Is.False);
            Assert.That(singularResult.HasNormalTransform, Is.False);
        });
    }

    [Test]
    public void EvaluateDocumentTransforms_RejectsMalformedAndNonFiniteTransformArrays()
    {
        GltfNode malformed = new GltfNode();
        malformed.HasTranslation = true;
        malformed.Translation = [1.0, 2.0];
        GltfNode nonFinite = new GltfNode();
        nonFinite.HasMatrix = true;
        nonFinite.Matrix[5] = double.NaN;
        GltfNode zeroRotation = new GltfNode();
        zeroRotation.HasRotation = true;
        zeroRotation.Rotation = [0.0, 0.0, 0.0, 0.0];
        GltfDocument document = CreateDocument(malformed, nonFinite, zeroRotation);

        SceneTransformAnalysis result = new TransformMath().EvaluateDocumentTransforms(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Nodes[0].HasLocalTransform, Is.False);
            Assert.That(result.Nodes[0].Diagnostics[0].Code, Is.EqualTo(GltfIssueCodes.ArrayLengthNotInList));
            Assert.That(result.Nodes[1].HasLocalTransform, Is.False);
            Assert.That(result.Nodes[1].Diagnostics[0].JsonPointer, Is.EqualTo("/nodes/1/matrix/5"));
            Assert.That(result.Nodes[2].HasLocalTransform, Is.False);
            Assert.That(result.Nodes[2].Diagnostics[0].Code, Is.EqualTo(GltfIssueCodes.ValueNotInRange));
        });
    }

    [Test]
    public void EvaluateDocumentTransforms_LeavesAmbiguousAndCyclicWorldTransformsUnavailable()
    {
        GltfDocument multipleParents = CreateDocument(
            new GltfNode(),
            new GltfNode(),
            new GltfNode());
        multipleParents.Nodes[0].Children.Add(2);
        multipleParents.Nodes[1].Children.Add(2);
        GltfDocument cycle = CreateDocument(new GltfNode(), new GltfNode());
        cycle.Nodes[0].Children.Add(1);
        cycle.Nodes[1].Children.Add(0);
        TransformMath evaluator = new TransformMath();

        SceneTransformAnalysis parentResult = evaluator.EvaluateDocumentTransforms(multipleParents);
        SceneTransformAnalysis cycleResult = evaluator.EvaluateDocumentTransforms(cycle);

        Assert.Multiple(() =>
        {
            Assert.That(parentResult.IsValid, Is.False);
            Assert.That(parentResult.Nodes[0].HasWorldTransform, Is.True);
            Assert.That(parentResult.Nodes[1].HasWorldTransform, Is.True);
            Assert.That(parentResult.Nodes[2].HasWorldTransform, Is.False);
            Assert.That(cycleResult.IsValid, Is.False);
            Assert.That(cycleResult.Nodes[0].HasWorldTransform, Is.False);
            Assert.That(cycleResult.Nodes[1].HasWorldTransform, Is.False);
        });
    }

    [Test]
    public void EvaluateDocumentTransforms_ThrowsForNullDocument()
    {
        Assert.That(
            () => new TransformMath().EvaluateDocumentTransforms(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
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
}
