namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class SceneNodeCameraValidatorTests
{
    [Test]
    public void ValidateDocumentSceneNodeCameraRules_AcceptsValidPerspectiveScene()
    {
        GltfDocument document = new GltfDocument();
        GltfPerspectiveCamera perspective = new GltfPerspectiveCamera();
        perspective.VerticalFieldOfView = 1.0;
        perspective.NearPlane = 0.1;
        GltfCamera camera = new GltfCamera();
        camera.Type = GltfCameraType.Perspective;
        camera.Perspective = perspective;
        document.Cameras.Add(camera);
        GltfNode node = new GltfNode();
        node.HasCamera = true;
        node.Camera = 0;
        document.Nodes.Add(node);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        document.Scenes.Add(scene);

        GltfValidationReport report = new SceneNodeCameraValidator()
            .ValidateDocumentSceneNodeCameraRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void ValidateDocumentSceneNodeCameraRules_ValidatesTransformsAndEmptyNodes()
    {
        GltfDocument document = new GltfDocument();
        GltfNode conflicting = new GltfNode();
        conflicting.HasMatrix = true;
        conflicting.HasTranslation = true;
        document.Nodes.Add(conflicting);
        GltfNode skewed = new GltfNode();
        skewed.HasMatrix = true;
        skewed.Matrix[4] = 0.5;
        document.Nodes.Add(skewed);
        GltfNode rotated = new GltfNode();
        rotated.HasRotation = true;
        rotated.Rotation = new double[] { 0.0, 0.0, 0.0, 2.0 };
        document.Nodes.Add(rotated);
        document.Nodes.Add(new GltfNode());

        GltfValidationReport report = new SceneNodeCameraValidator()
            .ValidateDocumentSceneNodeCameraRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.NodeMatrixTrs, "/nodes/0");
            AssertIssue(report, GltfIssueCodes.NodeMatrixDefault, "/nodes/0/matrix");
            AssertIssue(report, GltfIssueCodes.NodeMatrixNonTrs, "/nodes/1/matrix");
            AssertIssue(report, GltfIssueCodes.RotationNonUnit, "/nodes/2/rotation");
            AssertIssue(report, GltfIssueCodes.NodeEmpty, "/nodes/3");
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentSceneNodeCameraRules_CollectsPerspectiveAndOrthographicErrors()
    {
        GltfDocument document = new GltfDocument();
        GltfPerspectiveCamera perspective = new GltfPerspectiveCamera();
        perspective.HasAspectRatio = true;
        perspective.AspectRatio = -1.0;
        perspective.VerticalFieldOfView = Math.PI;
        perspective.NearPlane = 1.0;
        perspective.HasFarPlane = true;
        perspective.FarPlane = 0.5;
        GltfCamera perspectiveCamera = new GltfCamera();
        perspectiveCamera.Perspective = perspective;
        perspectiveCamera.Orthographic = new GltfOrthographicCamera();
        document.Cameras.Add(perspectiveCamera);
        GltfOrthographicCamera orthographic = new GltfOrthographicCamera();
        orthographic.HorizontalMagnification = 0.0;
        orthographic.VerticalMagnification = -2.0;
        orthographic.NearPlane = 2.0;
        orthographic.FarPlane = 1.0;
        GltfCamera orthographicCamera = new GltfCamera();
        orthographicCamera.Type = GltfCameraType.Orthographic;
        orthographicCamera.Orthographic = orthographic;
        document.Cameras.Add(orthographicCamera);

        GltfValidationReport report = new SceneNodeCameraValidator()
            .ValidateDocumentSceneNodeCameraRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.ValueNotInRange, "/cameras/0/perspective/aspectRatio");
            AssertIssue(report, GltfIssueCodes.CameraYfovGequalPi, "/cameras/0/perspective/yfov");
            AssertIssue(report, GltfIssueCodes.CameraZfarLequalZnear, "/cameras/0/perspective/zfar");
            AssertIssue(report, GltfIssueCodes.ExtraProperty, "/cameras/0/orthographic");
            AssertIssue(report, GltfIssueCodes.CameraXmagYmagZero, "/cameras/1/orthographic/xmag");
            AssertIssue(report, GltfIssueCodes.CameraXmagYmagNegative, "/cameras/1/orthographic/ymag");
            AssertIssue(report, GltfIssueCodes.CameraZfarLequalZnear, "/cameras/1/orthographic/zfar");
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentSceneNodeCameraRules_ReportsHierarchyConstraints()
    {
        GltfDocument document = new GltfDocument();
        GltfNode first = new GltfNode();
        first.Children.Add(1);
        GltfNode second = new GltfNode();
        second.Children.Add(0);
        GltfNode third = new GltfNode();
        third.Children.Add(1);
        document.Nodes.Add(first);
        document.Nodes.Add(second);
        document.Nodes.Add(third);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(1);
        document.Scenes.Add(scene);

        GltfValidationReport report = new SceneNodeCameraValidator()
            .ValidateDocumentSceneNodeCameraRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(HasIssue(report, GltfIssueCodes.NodeLoop), Is.True);
            Assert.That(HasIssue(report, GltfIssueCodes.NodeParentOverride), Is.True);
            Assert.That(HasIssue(report, GltfIssueCodes.SceneNonRootNode), Is.True);
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentSceneNodeCameraRules_ValidatesNodeMorphWeights()
    {
        GltfDocument document = new GltfDocument();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Targets.Add(new GltfMorphTarget());
        primitive.Targets.Add(new GltfMorphTarget());
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        GltfNode wrongCount = new GltfNode();
        wrongCount.HasMesh = true;
        wrongCount.Mesh = 0;
        wrongCount.Weights = new double[] { 1.0 };
        document.Nodes.Add(wrongCount);
        GltfNode invalidValue = new GltfNode();
        invalidValue.HasMesh = true;
        invalidValue.Mesh = 0;
        invalidValue.Weights = new double[] { double.NaN, 0.0 };
        document.Nodes.Add(invalidValue);

        GltfValidationReport report = new SceneNodeCameraValidator()
            .ValidateDocumentSceneNodeCameraRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.NodeWeightsInvalid, "/nodes/0/weights");
            AssertIssue(report, GltfIssueCodes.AccessorInvalidFloat, "/nodes/1/weights/0");
            Assert.That(report.IsValid, Is.False);
        });
    }

    private static bool HasIssue(GltfValidationReport report, string code)
    {
        return report.Issues.Any(issue => issue.Code == code);
    }

    private static void AssertIssue(
        GltfValidationReport report,
        string code,
        string pointer)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code && issue.JsonPointer == pointer),
            Is.True,
            $"Expected {code} at {pointer}; got "
                + string.Join(", ", report.Issues.Select(
                    issue => $"{issue.Code}@{issue.JsonPointer}")));
    }
}
