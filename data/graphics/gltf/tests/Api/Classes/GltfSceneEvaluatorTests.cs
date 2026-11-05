using System;
using System.Numerics;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfSceneEvaluatorTests
{
    [Test]
    public void EvaluateScene_SelectsDefaultOrExplicitSceneAndBuildsNodeIndexedTransforms()
    {
        GltfDocument document = CreateDocument(3);
        document.Nodes[0].HasTranslation = true;
        document.Nodes[0].Translation = [1.0, 0.0, 0.0];
        document.Nodes[0].Children.Add(1);
        document.Nodes[1].HasTranslation = true;
        document.Nodes[1].Translation = [0.0, 2.0, 0.0];
        document.Nodes[2].HasTranslation = true;
        document.Nodes[2].Translation = [0.0, 0.0, 3.0];
        GltfScene firstScene = new GltfScene();
        firstScene.Nodes.Add(0);
        GltfScene secondScene = new GltfScene();
        secondScene.Nodes.Add(2);
        document.Scenes.Add(firstScene);
        document.Scenes.Add(secondScene);
        document.HasDefaultScene = true;
        document.DefaultScene = 1;
        GltfSceneEvaluationOptions defaultOptions = CreateTransformOptions();
        GltfSceneEvaluationOptions explicitOptions = CreateTransformOptions();
        explicitOptions.HasScene = true;
        explicitOptions.Scene = 0;
        GltfSceneEvaluator evaluator = new GltfSceneEvaluator();

        GltfScenePose defaultPose = evaluator.EvaluateScene(document, defaultOptions);
        GltfScenePose explicitPose = evaluator.EvaluateScene(document, explicitOptions);

        Assert.Multiple(() =>
        {
            Assert.That(defaultPose.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(defaultPose.Scene, Is.EqualTo(1));
            Assert.That(defaultPose.Nodes, Has.Count.EqualTo(3));
            Assert.That(defaultPose.Nodes[2].Node, Is.EqualTo(2));
            Assert.That(defaultPose.Nodes[2].WorldTransform.M43, Is.EqualTo(3.0f));
            Assert.That(explicitPose.Scene, Is.EqualTo(0));
            Assert.That(explicitPose.Nodes[1].WorldTransform.M41, Is.EqualTo(1.0f));
            Assert.That(explicitPose.Nodes[1].WorldTransform.M42, Is.EqualTo(2.0f));
        });
    }

    [Test]
    public void EvaluateScene_ComposesTrsAndColumnMajorMatrixWorldTransforms()
    {
        GltfDocument document = CreateDocument(2);
        GltfNode parent = document.Nodes[0];
        parent.HasScale = true;
        parent.Scale = [2.0, 3.0, 4.0];
        parent.HasRotation = true;
        parent.Rotation = [0.0, 0.0, Math.Sqrt(0.5), Math.Sqrt(0.5)];
        parent.HasTranslation = true;
        parent.Translation = [10.0, 20.0, 30.0];
        parent.Children.Add(1);
        GltfNode child = document.Nodes[1];
        child.HasMatrix = true;
        child.Matrix = TranslationMatrixValues(1.0, 2.0, 3.0);
        AddScene(document, 0);

        GltfScenePose result = new GltfSceneEvaluator().EvaluateScene(
            document,
            CreateTransformOptions());
        Matrix4x4 expectedParent = Matrix4x4.CreateScale(2.0f, 3.0f, 4.0f)
            * Matrix4x4.CreateRotationZ(MathF.PI / 2.0f)
            * Matrix4x4.CreateTranslation(10.0f, 20.0f, 30.0f);
        Matrix4x4 expectedChild = Matrix4x4.CreateTranslation(1.0f, 2.0f, 3.0f)
            * expectedParent;
        Vector3 transformed = Vector3.Transform(
            Vector3.Zero,
            result.Nodes[1].WorldTransform);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            AssertMatrix(result.Nodes[0].LocalTransform, expectedParent);
            AssertMatrix(result.Nodes[0].WorldTransform, expectedParent);
            AssertMatrix(
                result.Nodes[1].LocalTransform,
                Matrix4x4.CreateTranslation(1.0f, 2.0f, 3.0f));
            AssertMatrix(result.Nodes[1].WorldTransform, expectedChild);
            Assert.That(transformed.X, Is.EqualTo(4.0f).Within(0.00001f));
            Assert.That(transformed.Y, Is.EqualTo(22.0f).Within(0.00001f));
            Assert.That(transformed.Z, Is.EqualTo(42.0f).Within(0.00001f));
        });
    }

    [Test]
    public void SceneOperations_AllowSharedRootsAndPreserveReflectedBoundsAndNormals()
    {
        GltfDocument document = CreateDocument(1);
        document.Nodes[0].HasScale = true;
        document.Nodes[0].Scale = [-2.0, 4.0, 5.0];
        int positions = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            -1.0, -2.0, -3.0,
            4.0, 5.0, 6.0);
        AddMeshNode(document, 0, positions, GltfConstants.AbsentIndex);
        AddScene(document, 0);
        AddScene(document, 0);
        GltfSceneEvaluationOptions firstOptions = CreateTransformOptions();
        firstOptions.HasScene = true;
        firstOptions.Scene = 0;
        firstOptions.CalculateBounds = true;
        GltfSceneEvaluationOptions secondOptions = CreateTransformOptions();
        secondOptions.HasScene = true;
        secondOptions.Scene = 1;
        secondOptions.CalculateBounds = true;
        GltfSceneEvaluator evaluator = new GltfSceneEvaluator();

        GltfScenePose first = evaluator.EvaluateScene(document, firstOptions);
        GltfScenePose second = evaluator.EvaluateScene(document, secondOptions);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(second.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(first.Scene, Is.Zero);
            Assert.That(second.Scene, Is.EqualTo(1));
            AssertMatrix(first.Nodes[0].WorldTransform, second.Nodes[0].WorldTransform);
            Assert.That(first.Nodes[0].HasNormalTransform, Is.True);
            Assert.That(first.Nodes[0].NormalTransform.M11, Is.EqualTo(-0.5f));
            Assert.That(first.Nodes[0].NormalTransform.M22, Is.EqualTo(0.25f));
            Assert.That(first.Nodes[0].NormalTransform.M33, Is.EqualTo(0.2f));
            Assert.That(first.Bounds.Minimum, Is.EqualTo(new Vector3(-8.0f, -8.0f, -15.0f)));
            Assert.That(first.Bounds.Maximum, Is.EqualTo(new Vector3(2.0f, 20.0f, 30.0f)));
            Assert.That(second.Bounds.Minimum, Is.EqualTo(first.Bounds.Minimum));
            Assert.That(second.Bounds.Maximum, Is.EqualTo(first.Bounds.Maximum));
            Assert.That(first.Diagnostics, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void EvaluateScene_AttachesCameraProjectionAndSkinPaletteToActiveNodes()
    {
        GltfDocument document = CreateDocument(3);
        document.Nodes[0].Children.Add(1);
        document.Nodes[0].HasSkin = true;
        document.Nodes[0].Skin = 0;
        document.Nodes[1].HasTranslation = true;
        document.Nodes[1].Translation = [2.0, 0.0, 0.0];
        document.Nodes[2].HasCamera = true;
        document.Nodes[2].Camera = 0;
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(1);
        document.Skins.Add(skin);
        GltfOrthographicCamera orthographic = new GltfOrthographicCamera();
        orthographic.HorizontalMagnification = 2.0;
        orthographic.VerticalMagnification = 4.0;
        orthographic.NearPlane = 1.0;
        orthographic.FarPlane = 11.0;
        GltfCamera camera = new GltfCamera();
        camera.Type = GltfCameraType.Orthographic;
        camera.Orthographic = orthographic;
        document.Cameras.Add(camera);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        scene.Nodes.Add(2);
        document.Scenes.Add(scene);

        GltfScenePose result = new GltfSceneEvaluator().EvaluateScene(
            document,
            new GltfSceneEvaluationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Nodes[0].SkinMatrices, Has.Count.EqualTo(1));
            Assert.That(result.Nodes[0].SkinMatrices[0].M41, Is.EqualTo(2.0f));
            Assert.That(result.Nodes[2].HasProjection, Is.True);
            Assert.That(result.Nodes[2].Projection.M11, Is.EqualTo(0.5f));
            Assert.That(result.Nodes[2].Projection.M22, Is.EqualTo(0.25f));
            Assert.That(result.HasBounds, Is.True);
            Assert.That(result.Bounds.IsEmpty, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void EvaluateScene_AppliesInverseBindMatricesInJointOrder()
    {
        GltfDocument document = CreateDocument(3);
        document.Nodes[0].HasTranslation = true;
        document.Nodes[0].Translation = [1.0, 0.0, 0.0];
        document.Nodes[0].Children.Add(1);
        document.Nodes[1].HasTranslation = true;
        document.Nodes[1].Translation = [2.0, 0.0, 0.0];
        document.Nodes[2].HasSkin = true;
        document.Nodes[2].Skin = 0;
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(0);
        skin.Joints.Add(1);
        skin.HasSkeleton = true;
        skin.Skeleton = 0;
        skin.HasInverseBindMatrices = true;
        skin.InverseBindMatrices = CreateFloatingAccessor(
            document,
            GltfAccessorType.Matrix4,
            TranslationMatrixValues(-1.0, 0.0, 0.0)
                .Concat(TranslationMatrixValues(-3.0, 0.0, 0.0))
                .ToArray());
        document.Skins.Add(skin);
        AddScene(document, 0, 2);
        GltfSceneEvaluationOptions options = CreateTransformOptions();
        options.CalculateSkins = true;

        GltfScenePose result = new GltfSceneEvaluator().EvaluateScene(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Nodes[2].SkinMatrices, Has.Count.EqualTo(2));
            AssertMatrix(result.Nodes[2].SkinMatrices[0], Matrix4x4.Identity);
            AssertMatrix(result.Nodes[2].SkinMatrices[1], Matrix4x4.Identity);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CalculateBounds_UsesOnlyIndexedVerticesAndWorldTransforms()
    {
        GltfDocument document = CreateDocument(1);
        document.Nodes[0].HasTranslation = true;
        document.Nodes[0].Translation = [10.0, 1.0, -2.0];
        int positions = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            -100.0, -100.0, -100.0,
            1.0, 2.0, 3.0,
            4.0, 5.0, 6.0);
        int indices = CreateIndexAccessor(document, 1, 2);
        AddMeshNode(document, 0, positions, indices);
        AddScene(document, 0);
        GltfSceneEvaluationOptions options = CreateTransformOptions();
        options.CalculateBounds = true;

        GltfBounds result = new GltfSceneEvaluator().CalculateBounds(document, options);
        SceneBoundsEvaluation detailed = new SceneEvaluator().CalculateDocumentSceneBounds(
            document,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Minimum, Is.EqualTo(new Vector3(11.0f, 3.0f, 1.0f)));
            Assert.That(result.Maximum, Is.EqualTo(new Vector3(14.0f, 6.0f, 4.0f)));
            Assert.That(detailed.Meshes, Has.Count.EqualTo(1));
            Assert.That(detailed.Meshes[0].Primitives, Has.Count.EqualTo(1));
            Assert.That(detailed.Meshes[0].Bounds.Minimum, Is.EqualTo(result.Minimum));
            Assert.That(detailed.Meshes[0].Primitives[0].Bounds.Maximum, Is.EqualTo(result.Maximum));
        });
    }

    [Test]
    public void CalculateBounds_DecodesSparsePositionAccessors()
    {
        GltfDocument document = CreateDocument(1);
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.Type = GltfAccessorType.Vector3;
        request.FloatingValues = [0.0, 0.0, 0.0, 2.0, 3.0, 4.0];
        request.AllowSparse = true;
        int positions = new GltfAccessorCodec().CreateSparseAccessor(request).Accessor;
        AddMeshNode(document, 0, positions, GltfConstants.AbsentIndex);
        AddScene(document, 0);

        GltfBounds result = new GltfSceneEvaluator().CalculateBounds(
            document,
            CreateTransformOptions());

        Assert.Multiple(() =>
        {
            Assert.That(document.Accessors[positions].Sparse, Is.Not.Null);
            Assert.That(document.Accessors[positions].HasBufferView, Is.False);
            Assert.That(result.Minimum, Is.EqualTo(Vector3.Zero));
            Assert.That(result.Maximum, Is.EqualTo(new Vector3(2.0f, 3.0f, 4.0f)));
        });
    }

    [Test]
    public void CalculateBounds_OptionallyIncludesWeightedMorphPositions()
    {
        GltfDocument document = CreateDocument(1);
        int positions = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0,
            1.0, 1.0, 1.0);
        int morphPositions = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            2.0, 0.0, 0.0,
            -1.0, 2.0, 0.0);
        GltfMeshPrimitive primitive = CreatePrimitive(positions);
        GltfMorphTarget target = new GltfMorphTarget();
        GltfAttribute targetPosition = new GltfAttribute();
        targetPosition.Semantic = "POSITION";
        targetPosition.Accessor = morphPositions;
        target.Attributes.Add(targetPosition);
        primitive.Targets.Add(target);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        document.Nodes[0].HasMesh = true;
        document.Nodes[0].Mesh = 0;
        document.Nodes[0].Weights = [0.5];
        AddScene(document, 0);
        GltfSceneEvaluationOptions baseOptions = CreateTransformOptions();
        GltfSceneEvaluationOptions morphOptions = CreateTransformOptions();
        morphOptions.IncludeMorphTargets = true;

        GltfBounds baseBounds = new GltfSceneEvaluator().CalculateBounds(document, baseOptions);
        GltfBounds morphBounds = new GltfSceneEvaluator().CalculateBounds(document, morphOptions);

        Assert.Multiple(() =>
        {
            Assert.That(baseBounds.Minimum, Is.EqualTo(Vector3.Zero));
            Assert.That(baseBounds.Maximum, Is.EqualTo(Vector3.One));
            Assert.That(morphBounds.Minimum, Is.EqualTo(new Vector3(0.5f, 0.0f, 0.0f)));
            Assert.That(morphBounds.Maximum, Is.EqualTo(new Vector3(1.0f, 2.0f, 1.0f)));
        });
    }

    [Test]
    public void CalculateBounds_UsesMeshMorphWeightsUnlessNodeOverridesThem()
    {
        GltfDocument document = CreateDocument(2);
        int positions = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0);
        int morphPositions = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            8.0, 0.0, 0.0);
        GltfMeshPrimitive primitive = CreatePrimitive(positions);
        GltfMorphTarget target = new GltfMorphTarget();
        GltfAttribute targetPosition = new GltfAttribute();
        targetPosition.Semantic = "POSITION";
        targetPosition.Accessor = morphPositions;
        target.Attributes.Add(targetPosition);
        primitive.Targets.Add(target);
        GltfMesh mesh = new GltfMesh();
        mesh.Weights = [0.25];
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        document.Nodes[0].HasMesh = true;
        document.Nodes[0].Mesh = 0;
        document.Nodes[1].HasMesh = true;
        document.Nodes[1].Mesh = 0;
        document.Nodes[1].Weights = [0.75];
        AddScene(document, 0);
        AddScene(document, 1);
        GltfSceneEvaluationOptions baseOptions = CreateTransformOptions();
        baseOptions.HasScene = true;
        baseOptions.CalculateBounds = true;
        GltfSceneEvaluationOptions morphOptions = CreateTransformOptions();
        morphOptions.HasScene = true;
        morphOptions.CalculateBounds = true;
        morphOptions.IncludeMorphTargets = true;
        GltfSceneEvaluator evaluator = new GltfSceneEvaluator();

        baseOptions.Scene = 0;
        morphOptions.Scene = 0;
        GltfBounds baseBounds = evaluator.CalculateBounds(document, baseOptions);
        GltfBounds meshWeightBounds = evaluator.CalculateBounds(document, morphOptions);
        morphOptions.Scene = 1;
        GltfBounds nodeWeightBounds = evaluator.CalculateBounds(document, morphOptions);

        Assert.Multiple(() =>
        {
            Assert.That(baseBounds.Minimum, Is.EqualTo(Vector3.Zero));
            Assert.That(baseBounds.Maximum, Is.EqualTo(Vector3.Zero));
            Assert.That(meshWeightBounds.Minimum, Is.EqualTo(new Vector3(2.0f, 0.0f, 0.0f)));
            Assert.That(meshWeightBounds.Maximum, Is.EqualTo(meshWeightBounds.Minimum));
            Assert.That(nodeWeightBounds.Minimum, Is.EqualTo(new Vector3(6.0f, 0.0f, 0.0f)));
            Assert.That(nodeWeightBounds.Maximum, Is.EqualTo(nodeWeightBounds.Minimum));
        });
    }

    [Test]
    public void CalculateBounds_IncludesOnlyInstancesFromTheSelectedScene()
    {
        GltfDocument document = CreateDocument(2);
        int positions = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0);
        AddMeshNode(document, 0, positions, GltfConstants.AbsentIndex);
        AddMeshNode(document, 1, positions, GltfConstants.AbsentIndex);
        document.Nodes[0].HasTranslation = true;
        document.Nodes[0].Translation = [1.0, 0.0, 0.0];
        document.Nodes[1].HasTranslation = true;
        document.Nodes[1].Translation = [10.0, 0.0, 0.0];
        AddScene(document, 0);
        AddScene(document, 1);
        document.HasDefaultScene = true;
        document.DefaultScene = 1;
        GltfSceneEvaluationOptions defaultOptions = CreateTransformOptions();
        GltfSceneEvaluationOptions explicitOptions = CreateTransformOptions();
        explicitOptions.HasScene = true;
        explicitOptions.Scene = 0;
        GltfSceneEvaluator evaluator = new GltfSceneEvaluator();

        GltfBounds defaultBounds = evaluator.CalculateBounds(document, defaultOptions);
        GltfBounds explicitBounds = evaluator.CalculateBounds(document, explicitOptions);

        Assert.Multiple(() =>
        {
            Assert.That(defaultBounds.Minimum, Is.EqualTo(new Vector3(10.0f, 0.0f, 0.0f)));
            Assert.That(defaultBounds.Maximum, Is.EqualTo(defaultBounds.Minimum));
            Assert.That(explicitBounds.Minimum, Is.EqualTo(new Vector3(1.0f, 0.0f, 0.0f)));
            Assert.That(explicitBounds.Maximum, Is.EqualTo(explicitBounds.Minimum));
        });
    }

    [Test]
    public void EvaluateScene_RespectsDisabledOptionalCalculations()
    {
        GltfDocument document = CreateDocument(1);
        document.Nodes[0].HasCamera = true;
        document.Nodes[0].Camera = 20;
        document.Nodes[0].HasSkin = true;
        document.Nodes[0].Skin = 30;
        document.Nodes[0].HasMesh = true;
        document.Nodes[0].Mesh = 40;
        AddScene(document, 0);
        GltfSceneEvaluationOptions options = CreateTransformOptions();

        GltfScenePose result = new GltfSceneEvaluator().EvaluateScene(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Nodes[0].HasProjection, Is.False);
            Assert.That(result.Nodes[0].SkinMatrices, Is.Empty);
            Assert.That(result.HasBounds, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void EvaluateScene_PropagatesUnavailableBoundsAndRejectsInvalidScenes()
    {
        GltfDocument unavailable = CreateDocument(1);
        GltfAccessor positions = new GltfAccessor();
        positions.Type = GltfAccessorType.Vector3;
        positions.Count = 1;
        unavailable.Accessors.Add(positions);
        AddMeshNode(unavailable, 0, 0, GltfConstants.AbsentIndex);
        AddScene(unavailable, 0);
        GltfSceneEvaluationOptions boundsOptions = CreateTransformOptions();
        boundsOptions.CalculateBounds = true;

        GltfDocument invalidScene = CreateDocument(1);
        GltfSceneEvaluationOptions sceneOptions = CreateTransformOptions();
        sceneOptions.HasScene = true;
        sceneOptions.Scene = 4;
        GltfSceneEvaluator evaluator = new GltfSceneEvaluator();
        GltfScenePose unavailableResult = evaluator.EvaluateScene(unavailable, boundsOptions);
        GltfScenePose sceneResult = evaluator.EvaluateScene(invalidScene, sceneOptions);

        Assert.Multiple(() =>
        {
            Assert.That(unavailableResult.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(unavailableResult.HasBounds, Is.False);
            Assert.That(unavailableResult.Diagnostics[0].Code, Is.EqualTo(GltfIssueCodes.UnresolvedReference));
            Assert.That(sceneResult.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(sceneResult.Scene, Is.EqualTo(4));
            Assert.That(sceneResult.Diagnostics[0].Code, Is.EqualTo(GltfIssueCodes.UnresolvedReference));
            Assert.That(
                () => evaluator.CalculateBounds(unavailable, boundsOptions),
                Throws.TypeOf<GltfValidationException>());
        });
    }

    [Test]
    public void EvaluateScene_ReportsCyclesAndMultipleParentsWithoutPublishingDerivedData()
    {
        GltfDocument cycle = CreateDocument(2);
        cycle.Nodes[0].Children.Add(1);
        cycle.Nodes[1].Children.Add(0);
        AddScene(cycle, 0);
        GltfDocument multipleParents = CreateDocument(3);
        multipleParents.Nodes[0].Children.Add(2);
        multipleParents.Nodes[1].Children.Add(2);
        AddScene(multipleParents, 0, 1);
        GltfSceneEvaluationOptions options = CreateTransformOptions();
        GltfSceneEvaluator evaluator = new GltfSceneEvaluator();

        GltfScenePose cycleResult = evaluator.EvaluateScene(cycle, options);
        GltfScenePose parentResult = evaluator.EvaluateScene(multipleParents, options);

        Assert.Multiple(() =>
        {
            Assert.That(cycleResult.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(cycleResult.Nodes, Has.Count.EqualTo(2));
            Assert.That(cycleResult.Nodes.All(node => !node.HasNormalTransform), Is.True);
            Assert.That(cycleResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.NodeLoop), Is.True);
            Assert.That(parentResult.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(parentResult.Nodes, Has.Count.EqualTo(3));
            Assert.That(parentResult.Nodes[0].HasNormalTransform, Is.True);
            Assert.That(parentResult.Nodes[1].HasNormalTransform, Is.True);
            Assert.That(parentResult.Nodes[2].HasNormalTransform, Is.False);
            Assert.That(parentResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.NodeParentOverride), Is.True);
            Assert.That(cycleResult.HasBounds, Is.False);
            Assert.That(parentResult.HasBounds, Is.False);
        });
    }

    [Test]
    public void SceneOperations_RejectNullArguments()
    {
        GltfSceneEvaluator evaluator = new GltfSceneEvaluator();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => evaluator.EvaluateScene(null!, new GltfSceneEvaluationOptions()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => evaluator.EvaluateScene(new GltfDocument(), null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(
                () => evaluator.CalculateBounds(null!, new GltfSceneEvaluationOptions()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => evaluator.CalculateBounds(new GltfDocument(), null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
        });
    }

    private static GltfDocument CreateDocument(int nodeCount)
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

    private static GltfSceneEvaluationOptions CreateTransformOptions()
    {
        GltfSceneEvaluationOptions result = new GltfSceneEvaluationOptions();
        result.CalculateSkins = false;
        result.CalculateCameras = false;
        result.CalculateBounds = false;
        return result;
    }

    private static int CreateFloatingAccessor(
        GltfDocument document,
        GltfAccessorType type,
        params double[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.Type = type;
        request.FloatingValues = values;
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static int CreateIndexAccessor(GltfDocument document, params uint[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.ComponentType = GltfComponentType.UnsignedShort;
        request.Type = GltfAccessorType.Scalar;
        request.UnsignedValues = values;
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static GltfMeshPrimitive CreatePrimitive(int positionAccessor)
    {
        GltfAttribute position = new GltfAttribute();
        position.Semantic = "POSITION";
        position.Accessor = positionAccessor;
        GltfMeshPrimitive result = new GltfMeshPrimitive();
        result.Attributes.Add(position);
        return result;
    }

    private static void AddMeshNode(
        GltfDocument document,
        int nodeIndex,
        int positionAccessor,
        int indexAccessor)
    {
        GltfMeshPrimitive primitive = CreatePrimitive(positionAccessor);
        if (indexAccessor != GltfConstants.AbsentIndex)
        {
            primitive.HasIndices = true;
            primitive.Indices = indexAccessor;
        }

        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        document.Nodes[nodeIndex].HasMesh = true;
        document.Nodes[nodeIndex].Mesh = document.Meshes.Count - 1;
    }

    private static void AddScene(GltfDocument document, params int[] nodes)
    {
        GltfScene scene = new GltfScene();
        scene.Nodes.AddRange(nodes);
        document.Scenes.Add(scene);
    }

    private static double[] TranslationMatrixValues(double x, double y, double z)
    {
        return
        [
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            x, y, z, 1.0
        ];
    }

    private static void AssertMatrix(Matrix4x4 actual, Matrix4x4 expected)
    {
        Assert.That(actual.M11, Is.EqualTo(expected.M11).Within(0.00001f));
        Assert.That(actual.M12, Is.EqualTo(expected.M12).Within(0.00001f));
        Assert.That(actual.M13, Is.EqualTo(expected.M13).Within(0.00001f));
        Assert.That(actual.M14, Is.EqualTo(expected.M14).Within(0.00001f));
        Assert.That(actual.M21, Is.EqualTo(expected.M21).Within(0.00001f));
        Assert.That(actual.M22, Is.EqualTo(expected.M22).Within(0.00001f));
        Assert.That(actual.M23, Is.EqualTo(expected.M23).Within(0.00001f));
        Assert.That(actual.M24, Is.EqualTo(expected.M24).Within(0.00001f));
        Assert.That(actual.M31, Is.EqualTo(expected.M31).Within(0.00001f));
        Assert.That(actual.M32, Is.EqualTo(expected.M32).Within(0.00001f));
        Assert.That(actual.M33, Is.EqualTo(expected.M33).Within(0.00001f));
        Assert.That(actual.M34, Is.EqualTo(expected.M34).Within(0.00001f));
        Assert.That(actual.M41, Is.EqualTo(expected.M41).Within(0.00001f));
        Assert.That(actual.M42, Is.EqualTo(expected.M42).Within(0.00001f));
        Assert.That(actual.M43, Is.EqualTo(expected.M43).Within(0.00001f));
        Assert.That(actual.M44, Is.EqualTo(expected.M44).Within(0.00001f));
    }

}
