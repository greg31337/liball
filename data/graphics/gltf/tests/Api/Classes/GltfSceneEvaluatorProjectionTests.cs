using System;
using System.Numerics;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfSceneEvaluatorProjectionTests
{
    [Test]
    public void CalculateProjection_CreatesFinitePerspectiveMatrix()
    {
        GltfPerspectiveCamera perspective = CreatePerspectiveCamera();
        perspective.HasFarPlane = true;
        perspective.FarPlane = 101.0;
        GltfDocument document = CreatePerspectiveDocument(perspective);

        GltfProjectionResult result = new GltfSceneEvaluator().CalculateProjection(document, 0);

        Matrix4x4 expected = new Matrix4x4(
            0.5f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, -1.02f, -2.02f,
            0.0f, 0.0f, -1.0f, 0.0f);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Projection, Is.EqualTo(expected));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CalculateProjection_CreatesInfinitePerspectiveMatrixWhenFarPlaneIsAbsent()
    {
        GltfPerspectiveCamera perspective = CreatePerspectiveCamera();
        perspective.FarPlane = double.NaN;
        GltfDocument document = CreatePerspectiveDocument(perspective);

        GltfProjectionResult result = new GltfSceneEvaluator().CalculateProjection(document, 0);

        Matrix4x4 expected = new Matrix4x4(
            0.5f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, -2.0f,
            0.0f, 0.0f, -1.0f, 0.0f);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Projection, Is.EqualTo(expected));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CalculateProjection_CreatesOrthographicMatrix()
    {
        GltfOrthographicCamera orthographic = new GltfOrthographicCamera();
        orthographic.HorizontalMagnification = 4.0;
        orthographic.VerticalMagnification = 2.0;
        orthographic.NearPlane = 1.0;
        orthographic.FarPlane = 11.0;
        GltfDocument document = CreateOrthographicDocument(orthographic);

        GltfProjectionResult result = new GltfSceneEvaluator().CalculateProjection(document, 0);

        Matrix4x4 expected = new Matrix4x4(
            0.25f, 0.0f, 0.0f, 0.0f,
            0.0f, 0.5f, 0.0f, 0.0f,
            0.0f, 0.0f, -0.2f, -1.2f,
            0.0f, 0.0f, 0.0f, 1.0f);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Projection, Is.EqualTo(expected));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CalculateProjection_RequiresViewportAspectRatioWhenCameraOmitsIt()
    {
        GltfPerspectiveCamera perspective = CreatePerspectiveCamera();
        perspective.HasAspectRatio = false;
        perspective.AspectRatio = double.NaN;
        GltfDocument document = CreatePerspectiveDocument(perspective);

        GltfProjectionResult result = new GltfSceneEvaluator().CalculateProjection(document, 0);

        AssertFailure(
            result,
            GltfOperationStatus.Unavailable,
            GltfIssueCodes.OperationUnavailable,
            "/cameras/0/perspective/aspectRatio");
    }

    [Test]
    public void CalculateProjection_ReportsInvalidCameraReferencesAndDefinitions()
    {
        IGltfSceneEvaluator evaluator = new GltfSceneEvaluator();
        GltfDocument emptyDocument = new GltfDocument();
        GltfDocument nullCameraDocument = new GltfDocument();
        nullCameraDocument.Cameras.Add(null!);
        GltfCamera unknownCamera = new GltfCamera();
        unknownCamera.Type = (GltfCameraType)99;
        GltfDocument unknownDocument = new GltfDocument();
        unknownDocument.Cameras.Add(unknownCamera);
        GltfCamera missingParameters = new GltfCamera();
        GltfDocument missingDocument = new GltfDocument();
        missingDocument.Cameras.Add(missingParameters);

        GltfProjectionResult absent = evaluator.CalculateProjection(emptyDocument, 0);
        GltfProjectionResult negative = evaluator.CalculateProjection(emptyDocument, -1);
        GltfProjectionResult nullCamera = evaluator.CalculateProjection(nullCameraDocument, 0);
        GltfProjectionResult unknown = evaluator.CalculateProjection(unknownDocument, 0);
        GltfProjectionResult missing = evaluator.CalculateProjection(missingDocument, 0);

        Assert.Multiple(() =>
        {
            AssertFailure(
                absent,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                "/cameras/0");
            AssertFailure(
                negative,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                "/cameras/-1");
            AssertFailure(
                nullCamera,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ArrayTypeMismatch,
                "/cameras/0");
            AssertFailure(
                unknown,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInList,
                "/cameras/0/type");
            AssertFailure(
                missing,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                "/cameras/0/perspective");
        });
    }

    [Test]
    public void CalculateProjection_RejectsInvalidPerspectiveRanges()
    {
        IGltfSceneEvaluator evaluator = new GltfSceneEvaluator();
        GltfPerspectiveCamera aspect = CreatePerspectiveCamera();
        aspect.AspectRatio = 0.0;
        GltfPerspectiveCamera fieldOfView = CreatePerspectiveCamera();
        fieldOfView.VerticalFieldOfView = double.PositiveInfinity;
        GltfPerspectiveCamera nearPlane = CreatePerspectiveCamera();
        nearPlane.NearPlane = 0.0;
        GltfPerspectiveCamera farPlane = CreatePerspectiveCamera();
        farPlane.HasFarPlane = true;
        farPlane.FarPlane = farPlane.NearPlane;

        GltfProjectionResult invalidAspect = evaluator.CalculateProjection(
            CreatePerspectiveDocument(aspect),
            0);
        GltfProjectionResult invalidFieldOfView = evaluator.CalculateProjection(
            CreatePerspectiveDocument(fieldOfView),
            0);
        GltfProjectionResult invalidNearPlane = evaluator.CalculateProjection(
            CreatePerspectiveDocument(nearPlane),
            0);
        GltfProjectionResult invalidFarPlane = evaluator.CalculateProjection(
            CreatePerspectiveDocument(farPlane),
            0);

        Assert.Multiple(() =>
        {
            AssertFailure(
                invalidAspect,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "/cameras/0/perspective/aspectRatio");
            AssertFailure(
                invalidFieldOfView,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "/cameras/0/perspective/yfov");
            AssertFailure(
                invalidNearPlane,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "/cameras/0/perspective/znear");
            AssertFailure(
                invalidFarPlane,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.CameraZfarLequalZnear,
                "/cameras/0/perspective/zfar");
        });
    }

    [Test]
    public void CalculateProjection_RejectsInvalidOrthographicRanges()
    {
        IGltfSceneEvaluator evaluator = new GltfSceneEvaluator();
        GltfOrthographicCamera zeroMagnification = CreateOrthographicCamera();
        zeroMagnification.HorizontalMagnification = 0.0;
        GltfOrthographicCamera nearPlane = CreateOrthographicCamera();
        nearPlane.NearPlane = -1.0;
        GltfOrthographicCamera farPlane = CreateOrthographicCamera();
        farPlane.FarPlane = farPlane.NearPlane;

        GltfProjectionResult invalidMagnification = evaluator.CalculateProjection(
            CreateOrthographicDocument(zeroMagnification),
            0);
        GltfProjectionResult invalidNearPlane = evaluator.CalculateProjection(
            CreateOrthographicDocument(nearPlane),
            0);
        GltfProjectionResult invalidFarPlane = evaluator.CalculateProjection(
            CreateOrthographicDocument(farPlane),
            0);

        Assert.Multiple(() =>
        {
            AssertFailure(
                invalidMagnification,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.CameraXmagYmagZero,
                "/cameras/0/orthographic/xmag");
            AssertFailure(
                invalidNearPlane,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "/cameras/0/orthographic/znear");
            AssertFailure(
                invalidFarPlane,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.CameraZfarLequalZnear,
                "/cameras/0/orthographic/zfar");
        });
    }

    [Test]
    public void CalculateProjection_ProducesMatricesWithAdvisoryCameraWarnings()
    {
        GltfPerspectiveCamera perspective = CreatePerspectiveCamera();
        perspective.VerticalFieldOfView = Math.PI;
        GltfOrthographicCamera orthographic = CreateOrthographicCamera();
        orthographic.HorizontalMagnification = -4.0;
        orthographic.VerticalMagnification = -2.0;
        IGltfSceneEvaluator evaluator = new GltfSceneEvaluator();

        GltfProjectionResult perspectiveResult = evaluator.CalculateProjection(
            CreatePerspectiveDocument(perspective),
            0);
        GltfProjectionResult orthographicResult = evaluator.CalculateProjection(
            CreateOrthographicDocument(orthographic),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(perspectiveResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(perspectiveResult.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(
                perspectiveResult.Diagnostics[0].Code,
                Is.EqualTo(GltfIssueCodes.CameraYfovGequalPi));
            Assert.That(
                perspectiveResult.Diagnostics[0].Severity,
                Is.EqualTo(GltfValidationSeverity.Warning));
            Assert.That(orthographicResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(orthographicResult.Projection.M11, Is.EqualTo(-0.25f));
            Assert.That(orthographicResult.Projection.M22, Is.EqualTo(-0.5f));
            Assert.That(orthographicResult.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(
                orthographicResult.Diagnostics[0].Code,
                Is.EqualTo(GltfIssueCodes.CameraXmagYmagNegative));
            Assert.That(
                orthographicResult.Diagnostics[1].Code,
                Is.EqualTo(GltfIssueCodes.CameraXmagYmagNegative));
        });
    }

    [Test]
    public void CalculateProjection_ThrowsForNullDocument()
    {
        IGltfSceneEvaluator evaluator = new GltfSceneEvaluator();

        Assert.That(
            () => evaluator.CalculateProjection(null!, 0),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
    }

    private static GltfPerspectiveCamera CreatePerspectiveCamera()
    {
        GltfPerspectiveCamera result = new GltfPerspectiveCamera();
        result.HasAspectRatio = true;
        result.AspectRatio = 2.0;
        result.VerticalFieldOfView = Math.PI / 2.0;
        result.NearPlane = 1.0;
        return result;
    }

    private static GltfOrthographicCamera CreateOrthographicCamera()
    {
        GltfOrthographicCamera result = new GltfOrthographicCamera();
        result.HorizontalMagnification = 4.0;
        result.VerticalMagnification = 2.0;
        result.NearPlane = 1.0;
        result.FarPlane = 11.0;
        return result;
    }

    private static GltfDocument CreatePerspectiveDocument(GltfPerspectiveCamera perspective)
    {
        GltfCamera camera = new GltfCamera();
        camera.Type = GltfCameraType.Perspective;
        camera.Perspective = perspective;
        GltfDocument result = new GltfDocument();
        result.Cameras.Add(camera);
        return result;
    }

    private static GltfDocument CreateOrthographicDocument(GltfOrthographicCamera orthographic)
    {
        GltfCamera camera = new GltfCamera();
        camera.Type = GltfCameraType.Orthographic;
        camera.Orthographic = orthographic;
        GltfDocument result = new GltfDocument();
        result.Cameras.Add(camera);
        return result;
    }

    private static void AssertFailure(
        GltfProjectionResult result,
        GltfOperationStatus status,
        string code,
        string pointer)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.Projection, Is.EqualTo(Matrix4x4.Identity));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(GltfValidationSeverity.Error));
            Assert.That(result.Diagnostics[0].JsonPointer, Is.EqualTo(pointer));
        });
    }
}
