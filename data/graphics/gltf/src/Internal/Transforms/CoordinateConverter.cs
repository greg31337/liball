using System;
using System.Numerics;

namespace gltf;

internal class CoordinateConversion
{
    internal Matrix4x4 AxisBasis = Matrix4x4.Identity;
    internal Matrix4x4 InverseAxisBasis = Matrix4x4.Identity;
    internal Matrix4x4 CoordinateTransform = Matrix4x4.Identity;
    internal Matrix4x4 InverseCoordinateTransform = Matrix4x4.Identity;
    internal double UnitScale = 1.0;
    internal double AxisDeterminant = 1.0;
    internal bool ReversesOrientation;
}

internal class CoordinateConverter
{
    internal CoordinateConversion CreateAxisUnitConversion(
        Matrix4x4 axisBasis,
        double unitScale)
    {
        double inverseUnitScale = 1.0 / unitScale;
        if (!double.IsFinite(unitScale)
            || unitScale <= 0.0
            || unitScale > float.MaxValue
            || (float)unitScale == 0.0f
            || !double.IsFinite(inverseUnitScale)
            || inverseUnitScale > float.MaxValue
            || (float)inverseUnitScale == 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                "unitScale",
                "The unit scale must be finite, positive, and representable as a matrix value.");
        }

        if (!IsSignedAxisBasis(axisBasis))
        {
            throw new ArgumentException(
                "The axis basis must be a finite signed permutation of the three coordinate axes.",
                "axisBasis");
        }

        Matrix4x4 canonicalBasis = CanonicalizeAxisBasis(axisBasis);
        CoordinateConversion result = new CoordinateConversion();
        result.AxisBasis = canonicalBasis;
        result.InverseAxisBasis = Matrix4x4.Transpose(canonicalBasis);
        result.UnitScale = unitScale;
        result.AxisDeterminant = CalculateAxisDeterminant(canonicalBasis);
        result.ReversesOrientation = result.AxisDeterminant < 0.0;
        result.CoordinateTransform = Matrix4x4.CreateScale((float)unitScale)
            * canonicalBasis;
        result.InverseCoordinateTransform = result.InverseAxisBasis
            * Matrix4x4.CreateScale((float)inverseUnitScale);
        return result;
    }

    internal Matrix4x4 CanonicalizeAxisBasis(Matrix4x4 matrix)
    {
        return new Matrix4x4(
            CanonicalizeAxisEntry(matrix.M11),
            CanonicalizeAxisEntry(matrix.M12),
            CanonicalizeAxisEntry(matrix.M13),
            0.0f,
            CanonicalizeAxisEntry(matrix.M21),
            CanonicalizeAxisEntry(matrix.M22),
            CanonicalizeAxisEntry(matrix.M23),
            0.0f,
            CanonicalizeAxisEntry(matrix.M31),
            CanonicalizeAxisEntry(matrix.M32),
            CanonicalizeAxisEntry(matrix.M33),
            0.0f,
            0.0f,
            0.0f,
            0.0f,
            1.0f);
    }

    internal float CanonicalizeAxisEntry(float value)
    {
        if (value > 0.5f)
        {
            return 1.0f;
        }

        if (value < -0.5f)
        {
            return -1.0f;
        }

        return 0.0f;
    }

    internal bool IsSignedAxisBasis(Matrix4x4 matrix)
    {
        if (!IsFiniteAxisMatrix(matrix)
            || Math.Abs(matrix.M14) > 0.00001f
            || Math.Abs(matrix.M24) > 0.00001f
            || Math.Abs(matrix.M34) > 0.00001f
            || Math.Abs(matrix.M41) > 0.00001f
            || Math.Abs(matrix.M42) > 0.00001f
            || Math.Abs(matrix.M43) > 0.00001f
            || Math.Abs(matrix.M44 - 1.0f) > 0.00001f)
        {
            return false;
        }

        int firstRowCount = CountAxisEntries(matrix.M11, matrix.M12, matrix.M13);
        int secondRowCount = CountAxisEntries(matrix.M21, matrix.M22, matrix.M23);
        int thirdRowCount = CountAxisEntries(matrix.M31, matrix.M32, matrix.M33);
        int firstColumnCount = CountAxisEntries(matrix.M11, matrix.M21, matrix.M31);
        int secondColumnCount = CountAxisEntries(matrix.M12, matrix.M22, matrix.M32);
        int thirdColumnCount = CountAxisEntries(matrix.M13, matrix.M23, matrix.M33);
        return firstRowCount == 1
            && secondRowCount == 1
            && thirdRowCount == 1
            && firstColumnCount == 1
            && secondColumnCount == 1
            && thirdColumnCount == 1;
    }

    internal int CountAxisEntries(float first, float second, float third)
    {
        int count = 0;
        if (IsSignedUnit(first))
        {
            count++;
        }
        else if (Math.Abs(first) > 0.00001f)
        {
            return -1;
        }

        if (IsSignedUnit(second))
        {
            count++;
        }
        else if (Math.Abs(second) > 0.00001f)
        {
            return -1;
        }

        if (IsSignedUnit(third))
        {
            count++;
        }
        else if (Math.Abs(third) > 0.00001f)
        {
            return -1;
        }

        return count;
    }

    internal bool IsSignedUnit(float value)
    {
        return Math.Abs(Math.Abs(value) - 1.0f) <= 0.00001f;
    }

    internal bool IsFiniteAxisMatrix(Matrix4x4 matrix)
    {
        return float.IsFinite(matrix.M11)
            && float.IsFinite(matrix.M12)
            && float.IsFinite(matrix.M13)
            && float.IsFinite(matrix.M14)
            && float.IsFinite(matrix.M21)
            && float.IsFinite(matrix.M22)
            && float.IsFinite(matrix.M23)
            && float.IsFinite(matrix.M24)
            && float.IsFinite(matrix.M31)
            && float.IsFinite(matrix.M32)
            && float.IsFinite(matrix.M33)
            && float.IsFinite(matrix.M34)
            && float.IsFinite(matrix.M41)
            && float.IsFinite(matrix.M42)
            && float.IsFinite(matrix.M43)
            && float.IsFinite(matrix.M44);
    }

    internal double CalculateAxisDeterminant(Matrix4x4 matrix)
    {
        return (double)matrix.M11 * (matrix.M22 * matrix.M33 - matrix.M23 * matrix.M32)
            - (double)matrix.M12 * (matrix.M21 * matrix.M33 - matrix.M23 * matrix.M31)
            + (double)matrix.M13 * (matrix.M21 * matrix.M32 - matrix.M22 * matrix.M31);
    }

    internal Vector3 ConvertPosition(Vector3 position, CoordinateConversion conversion)
    {
        return Vector3.TransformNormal(position, conversion.AxisBasis)
            * (float)conversion.UnitScale;
    }

    internal Vector3 ConvertNormal(Vector3 normal, CoordinateConversion conversion)
    {
        return Vector3.TransformNormal(normal, conversion.AxisBasis);
    }

    internal Vector4 ConvertTangent(Vector4 tangent, CoordinateConversion conversion)
    {
        Vector3 direction = Vector3.TransformNormal(
            new Vector3(tangent.X, tangent.Y, tangent.Z),
            conversion.AxisBasis);
        float handedness = tangent.W;
        if (conversion.ReversesOrientation)
        {
            handedness = -handedness;
        }

        return new Vector4(direction, handedness);
    }

    internal double[] ConvertPositionValues(double[] values, CoordinateConversion conversion)
    {
        ValidateComponentArray(values, 3, "values");
        double[] result = new double[values.Length];
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            ConvertVectorComponents(
                values,
                result,
                valueIndex,
                conversion,
                conversion.UnitScale);
            valueIndex += 3;
        }

        return result;
    }

    internal double[] ConvertNormalValues(double[] values, CoordinateConversion conversion)
    {
        ValidateComponentArray(values, 3, "values");
        double[] result = new double[values.Length];
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            ConvertVectorComponents(values, result, valueIndex, conversion, 1.0);
            valueIndex += 3;
        }

        return result;
    }

    internal double[] ConvertTangentValues(double[] values, CoordinateConversion conversion)
    {
        ValidateComponentArray(values, 4, "values");
        double[] result = new double[values.Length];
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            ConvertVectorComponents(values, result, valueIndex, conversion, 1.0);
            result[valueIndex + 3] = conversion.ReversesOrientation
                ? -values[valueIndex + 3]
                : values[valueIndex + 3];
            valueIndex += 4;
        }

        return result;
    }

    internal void ConvertVectorComponents(
        double[] source,
        double[] destination,
        int valueIndex,
        CoordinateConversion conversion,
        double scale)
    {
        double x = source[valueIndex];
        double y = source[valueIndex + 1];
        double z = source[valueIndex + 2];
        destination[valueIndex] = scale * (x * conversion.AxisBasis.M11
            + y * conversion.AxisBasis.M21
            + z * conversion.AxisBasis.M31);
        destination[valueIndex + 1] = scale * (x * conversion.AxisBasis.M12
            + y * conversion.AxisBasis.M22
            + z * conversion.AxisBasis.M32);
        destination[valueIndex + 2] = scale * (x * conversion.AxisBasis.M13
            + y * conversion.AxisBasis.M23
            + z * conversion.AxisBasis.M33);
    }

    internal Matrix4x4 ConvertNodeTransform(
        Matrix4x4 transform,
        CoordinateConversion conversion)
    {
        return conversion.InverseCoordinateTransform
            * transform
            * conversion.CoordinateTransform;
    }

    internal Matrix4x4 ConvertCameraTransform(
        Matrix4x4 transform,
        CoordinateConversion conversion)
    {
        return conversion.InverseCoordinateTransform
            * transform
            * conversion.CoordinateTransform;
    }

    internal double[] ConvertNodeMatrixValues(
        double[] values,
        CoordinateConversion conversion)
    {
        if (values == null || values.Length != 16)
        {
            throw new ArgumentException("A node matrix must contain 16 values.", "values");
        }

        Matrix4x4 source = new TransformMath().ReadColumnMajorMatrix(values);
        Matrix4x4 converted = ConvertNodeTransform(source, conversion);
        return WriteColumnMajorMatrix(converted);
    }

    internal double[] ConvertInverseBindMatrices(
        double[] values,
        CoordinateConversion conversion)
    {
        ValidateComponentArray(values, 16, "values");
        double[] result = new double[values.Length];
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            Matrix4x4 source = ReadMatrixAt(values, valueIndex);
            Matrix4x4 converted = conversion.InverseCoordinateTransform
                * source
                * conversion.CoordinateTransform;
            WriteMatrixAt(result, valueIndex, converted);
            valueIndex += 16;
        }

        return result;
    }

    internal Matrix4x4 ReadMatrixAt(double[] values, int valueIndex)
    {
        return new Matrix4x4(
            (float)values[valueIndex],
            (float)values[valueIndex + 1],
            (float)values[valueIndex + 2],
            (float)values[valueIndex + 3],
            (float)values[valueIndex + 4],
            (float)values[valueIndex + 5],
            (float)values[valueIndex + 6],
            (float)values[valueIndex + 7],
            (float)values[valueIndex + 8],
            (float)values[valueIndex + 9],
            (float)values[valueIndex + 10],
            (float)values[valueIndex + 11],
            (float)values[valueIndex + 12],
            (float)values[valueIndex + 13],
            (float)values[valueIndex + 14],
            (float)values[valueIndex + 15]);
    }

    internal double[] WriteColumnMajorMatrix(Matrix4x4 matrix)
    {
        double[] result = new double[16];
        WriteMatrixAt(result, 0, matrix);
        return result;
    }

    internal void WriteMatrixAt(double[] values, int valueIndex, Matrix4x4 matrix)
    {
        values[valueIndex] = matrix.M11;
        values[valueIndex + 1] = matrix.M12;
        values[valueIndex + 2] = matrix.M13;
        values[valueIndex + 3] = matrix.M14;
        values[valueIndex + 4] = matrix.M21;
        values[valueIndex + 5] = matrix.M22;
        values[valueIndex + 6] = matrix.M23;
        values[valueIndex + 7] = matrix.M24;
        values[valueIndex + 8] = matrix.M31;
        values[valueIndex + 9] = matrix.M32;
        values[valueIndex + 10] = matrix.M33;
        values[valueIndex + 11] = matrix.M34;
        values[valueIndex + 12] = matrix.M41;
        values[valueIndex + 13] = matrix.M42;
        values[valueIndex + 14] = matrix.M43;
        values[valueIndex + 15] = matrix.M44;
    }

    internal void ConvertPerspectiveCameraParameters(
        GltfPerspectiveCamera camera,
        CoordinateConversion conversion)
    {
        if (camera == null)
        {
            throw new ArgumentNullException("camera");
        }

        double nearPlane = ScaleCameraValue(camera.NearPlane, conversion);
        double farPlane = camera.FarPlane;
        if (camera.HasFarPlane)
        {
            farPlane = ScaleCameraValue(camera.FarPlane, conversion);
        }

        camera.NearPlane = nearPlane;
        if (camera.HasFarPlane)
        {
            camera.FarPlane = farPlane;
        }
    }

    internal void ConvertOrthographicCameraParameters(
        GltfOrthographicCamera camera,
        CoordinateConversion conversion)
    {
        if (camera == null)
        {
            throw new ArgumentNullException("camera");
        }

        double horizontal = ScaleCameraValue(camera.HorizontalMagnification, conversion);
        double vertical = ScaleCameraValue(camera.VerticalMagnification, conversion);
        double nearPlane = ScaleCameraValue(camera.NearPlane, conversion);
        double farPlane = ScaleCameraValue(camera.FarPlane, conversion);
        camera.HorizontalMagnification = horizontal;
        camera.VerticalMagnification = vertical;
        camera.NearPlane = nearPlane;
        camera.FarPlane = farPlane;
    }

    internal double ScaleCameraValue(double value, CoordinateConversion conversion)
    {
        double result = value * conversion.UnitScale;
        if (!double.IsFinite(result))
        {
            throw new ArgumentOutOfRangeException(
                "value",
                "The scaled camera value must be finite.");
        }

        return result;
    }

    internal uint[] ConvertPrimitiveIndices(
        uint[] indices,
        GltfPrimitiveMode mode,
        CoordinateConversion conversion)
    {
        if (indices == null)
        {
            throw new ArgumentNullException("indices");
        }

        if (!IsPrimitiveModeRecognized(mode))
        {
            throw new ArgumentException("The primitive mode is not recognized.", "mode");
        }

        if (!conversion.ReversesOrientation
            || mode == GltfPrimitiveMode.Points
            || mode == GltfPrimitiveMode.Lines
            || mode == GltfPrimitiveMode.LineLoop
            || mode == GltfPrimitiveMode.LineStrip)
        {
            return (uint[])indices.Clone();
        }

        switch (mode)
        {
            case GltfPrimitiveMode.Triangles:
                return ReverseTriangleList(indices);
            case GltfPrimitiveMode.TriangleStrip:
                return ReverseTriangleStrip(indices);
            case GltfPrimitiveMode.TriangleFan:
                return ReverseTriangleFan(indices);
            default:
                return (uint[])indices.Clone();
        }
    }

    internal bool IsPrimitiveModeRecognized(GltfPrimitiveMode mode)
    {
        switch (mode)
        {
            case GltfPrimitiveMode.Points:
            case GltfPrimitiveMode.Lines:
            case GltfPrimitiveMode.LineLoop:
            case GltfPrimitiveMode.LineStrip:
            case GltfPrimitiveMode.Triangles:
            case GltfPrimitiveMode.TriangleStrip:
            case GltfPrimitiveMode.TriangleFan:
                return true;
            default:
                return false;
        }
    }

    internal uint[] ReverseTriangleList(uint[] indices)
    {
        if (indices.Length % 3 != 0)
        {
            throw new ArgumentException(
                "A triangle-list index count must be divisible by three.",
                "indices");
        }

        uint[] result = (uint[])indices.Clone();
        int index = 0;
        while (index < result.Length)
        {
            uint temporary = result[index + 1];
            result[index + 1] = result[index + 2];
            result[index + 2] = temporary;
            index += 3;
        }

        return result;
    }

    internal uint[] ReverseTriangleStrip(uint[] indices)
    {
        if (indices.Length < 3)
        {
            return (uint[])indices.Clone();
        }

        uint[] result = new uint[indices.Length + 1];
        result[0] = indices[0];
        int index = 0;
        while (index < indices.Length)
        {
            result[index + 1] = indices[index];
            index++;
        }

        return result;
    }

    internal uint[] ReverseTriangleFan(uint[] indices)
    {
        uint[] result = new uint[indices.Length];
        if (indices.Length == 0)
        {
            return result;
        }

        result[0] = indices[0];
        int index = 1;
        while (index < indices.Length)
        {
            result[index] = indices[indices.Length - index];
            index++;
        }

        return result;
    }

    internal double[] ConvertAnimationValues(
        GltfAnimationPath path,
        double[] values,
        CoordinateConversion conversion)
    {
        switch (path)
        {
            case GltfAnimationPath.Translation:
                return ConvertPositionValues(values, conversion);
            case GltfAnimationPath.Rotation:
                return ConvertRotationAnimationValues(values, conversion);
            case GltfAnimationPath.Scale:
                return ConvertScaleAnimationValues(values, conversion);
            case GltfAnimationPath.Weights:
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }

                return (double[])values.Clone();
            case GltfAnimationPath.Pointer:
                throw new ArgumentException(
                    "Pointer animation values require target-specific conversion.",
                    "path");
            default:
                throw new ArgumentException("The animation path is not recognized.", "path");
        }
    }

    internal double[] ConvertRotationAnimationValues(
        double[] values,
        CoordinateConversion conversion)
    {
        ValidateComponentArray(values, 4, "values");
        double[] result = new double[values.Length];
        double orientation = conversion.AxisDeterminant;
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            double x = values[valueIndex];
            double y = values[valueIndex + 1];
            double z = values[valueIndex + 2];
            result[valueIndex] = orientation * (x * conversion.AxisBasis.M11
                + y * conversion.AxisBasis.M21
                + z * conversion.AxisBasis.M31);
            result[valueIndex + 1] = orientation * (x * conversion.AxisBasis.M12
                + y * conversion.AxisBasis.M22
                + z * conversion.AxisBasis.M32);
            result[valueIndex + 2] = orientation * (x * conversion.AxisBasis.M13
                + y * conversion.AxisBasis.M23
                + z * conversion.AxisBasis.M33);
            result[valueIndex + 3] = values[valueIndex + 3];
            valueIndex += 4;
        }

        return result;
    }

    internal double[] ConvertScaleAnimationValues(
        double[] values,
        CoordinateConversion conversion)
    {
        ValidateComponentArray(values, 3, "values");
        double[] result = new double[values.Length];
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            double x = values[valueIndex];
            double y = values[valueIndex + 1];
            double z = values[valueIndex + 2];
            result[valueIndex] = x * conversion.AxisBasis.M11 * conversion.AxisBasis.M11
                + y * conversion.AxisBasis.M21 * conversion.AxisBasis.M21
                + z * conversion.AxisBasis.M31 * conversion.AxisBasis.M31;
            result[valueIndex + 1] = x * conversion.AxisBasis.M12 * conversion.AxisBasis.M12
                + y * conversion.AxisBasis.M22 * conversion.AxisBasis.M22
                + z * conversion.AxisBasis.M32 * conversion.AxisBasis.M32;
            result[valueIndex + 2] = x * conversion.AxisBasis.M13 * conversion.AxisBasis.M13
                + y * conversion.AxisBasis.M23 * conversion.AxisBasis.M23
                + z * conversion.AxisBasis.M33 * conversion.AxisBasis.M33;
            valueIndex += 3;
        }

        return result;
    }

    internal void ValidateComponentArray(
        double[] values,
        int componentCount,
        string parameterName)
    {
        if (values == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (values.Length % componentCount != 0)
        {
            throw new ArgumentException(
                $"The value count must be divisible by {componentCount}.",
                parameterName);
        }
    }
}
