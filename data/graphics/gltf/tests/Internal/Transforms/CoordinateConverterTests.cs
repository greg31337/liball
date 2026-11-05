using System.Numerics;

namespace gltf.Tests.Internal.Transforms;

[TestFixture]
public class CoordinateConverterTests
{
    [Test]
    public void CreateAxisUnitConversion_AcceptsProperAndReflectedSignedAxisBases()
    {
        CoordinateConverter converter = new CoordinateConverter();
        Matrix4x4 properBasis = CreateYUpToZUpBasis();
        Matrix4x4 reflectedBasis = Matrix4x4.CreateScale(-1.0f, 1.0f, 1.0f);

        CoordinateConversion proper = converter.CreateAxisUnitConversion(properBasis, 100.0);
        CoordinateConversion reflected = converter.CreateAxisUnitConversion(reflectedBasis, 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(proper.AxisDeterminant, Is.EqualTo(1.0));
            Assert.That(proper.ReversesOrientation, Is.False);
            Assert.That(proper.UnitScale, Is.EqualTo(100.0));
            Assert.That(reflected.AxisDeterminant, Is.EqualTo(-1.0));
            Assert.That(reflected.ReversesOrientation, Is.True);
            Assert.That(proper.InverseAxisBasis * proper.AxisBasis, Is.EqualTo(Matrix4x4.Identity));
        });
    }

    [Test]
    public void ConvertGeometryValues_AppliesUnitsAxesAndTangentHandedness()
    {
        CoordinateConverter converter = new CoordinateConverter();
        CoordinateConversion proper = converter.CreateAxisUnitConversion(
            CreateYUpToZUpBasis(),
            10.0);
        CoordinateConversion reflected = converter.CreateAxisUnitConversion(
            Matrix4x4.CreateScale(-1.0f, 1.0f, 1.0f),
            1.0);

        double[] positions = converter.ConvertPositionValues([1.0, 2.0, 3.0], proper);
        double[] normals = converter.ConvertNormalValues([1.0, 2.0, 3.0], proper);
        double[] properTangents = converter.ConvertTangentValues([1.0, 2.0, 3.0, -1.0], proper);
        double[] reflectedTangents = converter.ConvertTangentValues([1.0, 2.0, 3.0, -1.0], reflected);

        Assert.Multiple(() =>
        {
            Assert.That(positions, Is.EqualTo(new double[] { 10.0, 30.0, -20.0 }));
            Assert.That(normals, Is.EqualTo(new double[] { 1.0, 3.0, -2.0 }));
            Assert.That(properTangents, Is.EqualTo(new double[] { 1.0, 3.0, -2.0, -1.0 }));
            Assert.That(reflectedTangents, Is.EqualTo(new double[] { -1.0, 2.0, 3.0, 1.0 }));
            Assert.That(
                converter.ConvertPosition(new Vector3(1.0f, 2.0f, 3.0f), proper),
                Is.EqualTo(new Vector3(10.0f, 30.0f, -20.0f)));
            Assert.That(
                converter.ConvertNormal(new Vector3(1.0f, 2.0f, 3.0f), proper),
                Is.EqualTo(new Vector3(1.0f, 3.0f, -2.0f)));
        });
    }

    [Test]
    public void ConvertNodeAndCameraTransforms_PreservePointTransformationBehavior()
    {
        CoordinateConverter converter = new CoordinateConverter();
        CoordinateConversion conversion = converter.CreateAxisUnitConversion(
            CreateYUpToZUpBasis(),
            10.0);
        Matrix4x4 sourceTransform = Matrix4x4.CreateRotationY(0.5f)
            * Matrix4x4.CreateTranslation(2.0f, 3.0f, 4.0f);
        Vector3 sourcePoint = new Vector3(5.0f, 6.0f, 7.0f);

        Matrix4x4 convertedTransform = converter.ConvertNodeTransform(sourceTransform, conversion);
        Matrix4x4 convertedCamera = converter.ConvertCameraTransform(sourceTransform, conversion);
        double[] convertedValues = converter.ConvertNodeMatrixValues(
            converter.WriteColumnMajorMatrix(sourceTransform),
            conversion);
        Vector3 expected = converter.ConvertPosition(
            Vector3.Transform(sourcePoint, sourceTransform),
            conversion);
        Vector3 actual = Vector3.Transform(
            converter.ConvertPosition(sourcePoint, conversion),
            convertedTransform);

        Assert.Multiple(() =>
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(0.0001f));
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(0.0001f));
            Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(0.0001f));
            Assert.That(convertedCamera, Is.EqualTo(convertedTransform));
            Assert.That(converter.ReadMatrixAt(convertedValues, 0), Is.EqualTo(convertedTransform));
        });
    }

    [Test]
    public void ConvertCameraParameters_ScalesOnlyLengthValuesAndHonorsPresence()
    {
        CoordinateConverter converter = new CoordinateConverter();
        CoordinateConversion conversion = converter.CreateAxisUnitConversion(Matrix4x4.Identity, 100.0);
        GltfPerspectiveCamera perspective = new GltfPerspectiveCamera();
        perspective.HasAspectRatio = true;
        perspective.AspectRatio = 1.5;
        perspective.VerticalFieldOfView = 0.75;
        perspective.NearPlane = 0.1;
        perspective.FarPlane = double.NaN;
        GltfOrthographicCamera orthographic = new GltfOrthographicCamera();
        orthographic.HorizontalMagnification = 2.0;
        orthographic.VerticalMagnification = -3.0;
        orthographic.NearPlane = 0.0;
        orthographic.FarPlane = 10.0;

        converter.ConvertPerspectiveCameraParameters(perspective, conversion);
        converter.ConvertOrthographicCameraParameters(orthographic, conversion);

        Assert.Multiple(() =>
        {
            Assert.That(perspective.AspectRatio, Is.EqualTo(1.5));
            Assert.That(perspective.VerticalFieldOfView, Is.EqualTo(0.75));
            Assert.That(perspective.NearPlane, Is.EqualTo(10.0));
            Assert.That(perspective.FarPlane, Is.NaN);
            Assert.That(orthographic.HorizontalMagnification, Is.EqualTo(200.0));
            Assert.That(orthographic.VerticalMagnification, Is.EqualTo(-300.0));
            Assert.That(orthographic.NearPlane, Is.Zero);
            Assert.That(orthographic.FarPlane, Is.EqualTo(1000.0));
        });
    }

    [Test]
    public void ConvertPrimitiveIndices_ReversesEveryTriangleTopologyForReflections()
    {
        CoordinateConverter converter = new CoordinateConverter();
        CoordinateConversion reflected = converter.CreateAxisUnitConversion(
            Matrix4x4.CreateScale(-1.0f, 1.0f, 1.0f),
            1.0);
        CoordinateConversion proper = converter.CreateAxisUnitConversion(Matrix4x4.Identity, 1.0);
        uint[] triangles = [0, 1, 2, 3, 4, 5];

        uint[] triangleResult = converter.ConvertPrimitiveIndices(
            triangles,
            GltfPrimitiveMode.Triangles,
            reflected);
        uint[] stripResult = converter.ConvertPrimitiveIndices(
            [0, 1, 2, 3],
            GltfPrimitiveMode.TriangleStrip,
            reflected);
        uint[] fanResult = converter.ConvertPrimitiveIndices(
            [0, 1, 2, 3],
            GltfPrimitiveMode.TriangleFan,
            reflected);
        uint[] unchanged = converter.ConvertPrimitiveIndices(
            triangles,
            GltfPrimitiveMode.Triangles,
            proper);
        triangles[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(triangleResult, Is.EqualTo(new uint[] { 0, 2, 1, 3, 5, 4 }));
            Assert.That(stripResult, Is.EqualTo(new uint[] { 0, 0, 1, 2, 3 }));
            Assert.That(fanResult, Is.EqualTo(new uint[] { 0, 3, 2, 1 }));
            Assert.That(unchanged, Is.EqualTo(new uint[] { 0, 1, 2, 3, 4, 5 }));
        });
    }

    [Test]
    public void ConvertInverseBindMatrices_UsesTheSameCoordinateConjugation()
    {
        CoordinateConverter converter = new CoordinateConverter();
        CoordinateConversion conversion = converter.CreateAxisUnitConversion(
            CreateYUpToZUpBasis(),
            10.0);
        Matrix4x4 first = Matrix4x4.CreateTranslation(-2.0f, -3.0f, -4.0f);
        Matrix4x4 second = Matrix4x4.CreateScale(0.5f);
        double[] values = JoinMatrices(
            converter.WriteColumnMajorMatrix(first),
            converter.WriteColumnMajorMatrix(second));

        double[] result = converter.ConvertInverseBindMatrices(values, conversion);
        Matrix4x4 convertedFirst = converter.ReadMatrixAt(result, 0);
        Matrix4x4 convertedSecond = converter.ReadMatrixAt(result, 16);

        Assert.Multiple(() =>
        {
            Assert.That(
                convertedFirst,
                Is.EqualTo(converter.ConvertNodeTransform(first, conversion)));
            Assert.That(
                convertedSecond,
                Is.EqualTo(converter.ConvertNodeTransform(second, conversion)));
            Assert.That(result, Is.Not.SameAs(values));
        });
    }

    [Test]
    public void ConvertAnimationValues_ConvertsCorePathsAndPreservesQuaternionMagnitude()
    {
        CoordinateConverter converter = new CoordinateConverter();
        CoordinateConversion proper = converter.CreateAxisUnitConversion(
            CreateYUpToZUpBasis(),
            10.0);
        CoordinateConversion reflected = converter.CreateAxisUnitConversion(
            Matrix4x4.CreateScale(-1.0f, 1.0f, 1.0f),
            1.0);
        double[] weights = [0.25, 0.75];

        double[] translation = converter.ConvertAnimationValues(
            GltfAnimationPath.Translation,
            [1.0, 2.0, 3.0],
            proper);
        double[] scale = converter.ConvertAnimationValues(
            GltfAnimationPath.Scale,
            [2.0, 3.0, 4.0],
            proper);
        double[] rotationTangent = converter.ConvertAnimationValues(
            GltfAnimationPath.Rotation,
            [2.0, 3.0, 4.0, 5.0],
            reflected);
        double[] convertedWeights = converter.ConvertAnimationValues(
            GltfAnimationPath.Weights,
            weights,
            proper);
        weights[0] = 1.0;

        Assert.Multiple(() =>
        {
            Assert.That(translation, Is.EqualTo(new double[] { 10.0, 30.0, -20.0 }));
            Assert.That(scale, Is.EqualTo(new double[] { 2.0, 4.0, 3.0 }));
            Assert.That(rotationTangent, Is.EqualTo(new double[] { 2.0, -3.0, -4.0, 5.0 }));
            Assert.That(convertedWeights, Is.EqualTo(new double[] { 0.25, 0.75 }));
        });
    }

    [Test]
    public void CoordinateConversion_RejectsUnsupportedBasesAndMalformedArrays()
    {
        CoordinateConverter converter = new CoordinateConverter();
        Matrix4x4 sheared = Matrix4x4.Identity;
        sheared.M12 = 0.5f;
        CoordinateConversion identity = converter.CreateAxisUnitConversion(Matrix4x4.Identity, 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => converter.CreateAxisUnitConversion(sheared, 1.0),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("axisBasis"));
            Assert.That(
                () => converter.CreateAxisUnitConversion(Matrix4x4.Identity, 0.0),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("unitScale"));
            Assert.That(
                () => converter.CreateAxisUnitConversion(Matrix4x4.Identity, 1.0e-100),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("unitScale"));
            Assert.That(
                () => converter.ConvertPositionValues([1.0, 2.0], identity),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("values"));
            Assert.That(
                () => converter.ConvertAnimationValues(
                    GltfAnimationPath.Pointer,
                    [],
                    identity),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("path"));
            Assert.That(
                () => converter.ConvertPrimitiveIndices(
                    [],
                    (GltfPrimitiveMode)99,
                    identity),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("mode"));
        });
    }

    private static Matrix4x4 CreateYUpToZUpBasis()
    {
        return new Matrix4x4(
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f);
    }

    private static double[] JoinMatrices(double[] first, double[] second)
    {
        double[] result = new double[32];
        first.CopyTo(result, 0);
        second.CopyTo(result, 16);
        return result;
    }
}
