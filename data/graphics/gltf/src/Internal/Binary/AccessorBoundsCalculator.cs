using System;

namespace gltf;

internal class AccessorBoundsResult
{
    internal bool Available;
    internal bool HasValues;
    internal bool ValuesFinite = true;
    internal int InvalidElement = GltfConstants.AbsentIndex;
    internal int InvalidComponent = GltfConstants.AbsentIndex;
    internal double[] Minimum = [];
    internal double[] Maximum = [];
    internal double[] DequantizedMinimum = [];
    internal double[] DequantizedMaximum = [];
}

internal class AccessorBoundsVerification
{
    internal AccessorBoundsResult Calculated = new AccessorBoundsResult();
    internal bool Valid = true;
    internal bool MinimumLengthValid = true;
    internal bool MaximumLengthValid = true;
    internal bool MinimumFinite = true;
    internal bool MaximumFinite = true;
    internal bool OrderValid = true;
    internal bool MinimumMatches = true;
    internal bool MaximumMatches = true;
    internal bool RequiredBoundsPresent = true;
    internal bool PositionShapeValid = true;
}

internal class AccessorBoundsSource
{
    internal bool Available;
    internal byte[] Bytes = [];
    internal AccessorByteRange Range = new AccessorByteRange();
}

internal class AccessorBoundsCalculator
{
    internal AccessorLayout Layout = new AccessorLayout();
    internal AccessorDecoder Decoder = new AccessorDecoder();
    internal MeshQuantization Quantization = new MeshQuantization();
    internal SparseAccessorDecoder SparseDecoder = new SparseAccessorDecoder();
    internal AccessorSparseConverter SparseConverter = new AccessorSparseConverter();

    internal AccessorBoundsResult CalculateAccessorBounds(
        GltfDocument document,
        int accessorIndex)
    {
        GltfAccessor accessor = SparseConverter.GetRequiredAccessor(
            document,
            accessorIndex);
        ValidateBoundsShape(accessor.Type);
        AccessorBoundsSource source = CreateBoundsSource(document, accessor);
        AccessorBoundsResult result = new AccessorBoundsResult();
        result.Available = source.Available;
        if (!source.Available || accessor.Count == 0)
        {
            return result;
        }

        double[] values = Decoder.DecodeFloatingValues(
            source.Bytes,
            source.Range);
        int componentCount = source.Range.ElementLayout.ComponentCount;
        result.Minimum = new double[componentCount];
        result.Maximum = new double[componentCount];
        int elementIndex = 0;
        int valueIndex = 0;
        while (elementIndex < accessor.Count)
        {
            int componentIndex = 0;
            while (componentIndex < componentCount)
            {
                double value = values[valueIndex];
                if (!double.IsFinite(value))
                {
                    result.ValuesFinite = false;
                    result.InvalidElement = elementIndex;
                    result.InvalidComponent = componentIndex;
                    return result;
                }

                if (elementIndex == 0)
                {
                    result.Minimum[componentIndex] = value;
                    result.Maximum[componentIndex] = value;
                }
                else
                {
                    if (value < result.Minimum[componentIndex])
                    {
                        result.Minimum[componentIndex] = value;
                    }

                    if (value > result.Maximum[componentIndex])
                    {
                        result.Maximum[componentIndex] = value;
                    }
                }

                componentIndex++;
                valueIndex++;
            }

            elementIndex++;
        }

        result.HasValues = true;
        result.DequantizedMinimum = Quantization.DequantizeStoredBounds(
            accessor,
            result.Minimum);
        result.DequantizedMaximum = Quantization.DequantizeStoredBounds(
            accessor,
            result.Maximum);
        return result;
    }

    internal AccessorBoundsVerification VerifyAccessorBounds(
        GltfDocument document,
        int accessorIndex)
    {
        GltfAccessor accessor = SparseConverter.GetRequiredAccessor(
            document,
            accessorIndex);
        AccessorBoundsVerification result = new AccessorBoundsVerification();
        result.Calculated = CalculateAccessorBounds(document, accessorIndex);
        int componentCount = Layout.GetComponentCount(accessor.Type);
        if (accessor.Minimum != null
            && accessor.Minimum.Length != componentCount)
        {
            result.MinimumLengthValid = false;
        }

        if (accessor.Minimum != null)
        {
            result.MinimumFinite = AreBoundsFinite(
                accessor.Minimum,
                accessor.ComponentType);
        }

        if (accessor.Maximum != null
            && accessor.Maximum.Length != componentCount)
        {
            result.MaximumLengthValid = false;
        }

        if (accessor.Maximum != null)
        {
            result.MaximumFinite = AreBoundsFinite(
                accessor.Maximum,
                accessor.ComponentType);
        }

        if (accessor.Minimum != null
            && accessor.Maximum != null
            && result.MinimumLengthValid
            && result.MaximumLengthValid)
        {
            int componentIndex = 0;
            while (componentIndex < componentCount)
            {
                double minimum = NormalizeDeclaredBound(
                    accessor.Minimum[componentIndex],
                    accessor.ComponentType);
                double maximum = NormalizeDeclaredBound(
                    accessor.Maximum[componentIndex],
                    accessor.ComponentType);
                if (!double.IsFinite(minimum)
                    || !double.IsFinite(maximum)
                    || minimum > maximum)
                {
                    result.OrderValid = false;
                }

                componentIndex++;
            }
        }

        if (result.Calculated.Available
            && result.Calculated.HasValues
            && result.Calculated.ValuesFinite)
        {
            if (accessor.Minimum != null && result.MinimumLengthValid)
            {
                result.MinimumMatches = BoundsMatch(
                    accessor.Minimum,
                    result.Calculated.Minimum,
                    accessor.ComponentType);
            }

            if (accessor.Maximum != null && result.MaximumLengthValid)
            {
                result.MaximumMatches = BoundsMatch(
                    accessor.Maximum,
                    result.Calculated.Maximum,
                    accessor.ComponentType);
            }
        }

        result.Valid = result.Calculated.ValuesFinite
            && result.MinimumLengthValid
            && result.MaximumLengthValid
            && result.MinimumFinite
            && result.MaximumFinite
            && result.OrderValid
            && result.MinimumMatches
            && result.MaximumMatches;
        return result;
    }

    internal AccessorBoundsVerification VerifyPositionAccessorBounds(
        GltfDocument document,
        int accessorIndex)
    {
        GltfAccessor accessor = SparseConverter.GetRequiredAccessor(
            document,
            accessorIndex);
        AccessorBoundsVerification result = VerifyAccessorBounds(
            document,
            accessorIndex);
        result.RequiredBoundsPresent = accessor.Minimum != null
            && accessor.Maximum != null;
        result.PositionShapeValid = accessor.Type == GltfAccessorType.Vector3;
        result.Valid = result.Valid
            && result.RequiredBoundsPresent
            && result.PositionShapeValid;
        return result;
    }

    internal AccessorBoundsSource CreateBoundsSource(
        GltfDocument document,
        GltfAccessor accessor)
    {
        AccessorBoundsSource result = new AccessorBoundsSource();
        result.Range = CreateTightRange(accessor);
        if (accessor.Sparse == null)
        {
            if (!accessor.HasBufferView)
            {
                return result;
            }

            GltfBufferView bufferView = SparseConverter.GetRequiredBufferView(
                document,
                accessor.BufferView,
                "accessor.BufferView");
            result.Bytes = SparseConverter.GetResolvedBufferBytes(
                document,
                bufferView,
                "accessor.BufferView");
            result.Range = Layout.CalculateAccessorByteRange(
                accessor,
                Layout.CalculateBufferViewRange(bufferView));
            result.Available = true;
            return result;
        }

        GltfAccessorSparse sparse = accessor.Sparse;
        AccessorByteRange? baseRange = null;
        byte[] baseSource = [];
        if (accessor.HasBufferView)
        {
            GltfBufferView baseView = SparseConverter.GetRequiredBufferView(
                document,
                accessor.BufferView,
                "accessor.BufferView");
            baseSource = SparseConverter.GetResolvedBufferBytes(
                document,
                baseView,
                "accessor.BufferView");
            baseRange = Layout.CalculateAccessorByteRange(
                accessor,
                Layout.CalculateBufferViewRange(baseView));
        }

        GltfBufferView indicesView = SparseConverter.GetRequiredBufferView(
            document,
            sparse.Indices.BufferView,
            "sparse.Indices.BufferView");
        GltfBufferView valuesView = SparseConverter.GetRequiredBufferView(
            document,
            sparse.Values.BufferView,
            "sparse.Values.BufferView");
        byte[] indicesSource = SparseConverter.GetResolvedBufferBytes(
            document,
            indicesView,
            "sparse.Indices.BufferView");
        byte[] valuesSource = SparseConverter.GetResolvedBufferBytes(
            document,
            valuesView,
            "sparse.Values.BufferView");
        AccessorByteRange indicesRange = SparseDecoder.CalculateSparseIndicesRange(
            sparse,
            Layout.CalculateBufferViewRange(indicesView));
        AccessorByteRange valuesRange = SparseDecoder.CalculateSparseValuesRange(
            accessor,
            sparse,
            Layout.CalculateBufferViewRange(valuesView));
        result.Bytes = SparseDecoder.OverlayRawElements(
            baseSource,
            baseRange,
            indicesSource,
            indicesRange,
            valuesSource,
            valuesRange,
            accessor.Count);
        result.Available = true;
        return result;
    }

    internal AccessorByteRange CreateTightRange(GltfAccessor accessor)
    {
        GltfAccessor payload = new GltfAccessor();
        payload.ComponentType = accessor.ComponentType;
        payload.Count = accessor.Count;
        payload.Type = accessor.Type;
        return Layout.CalculateAccessorByteRange(payload, null);
    }

    internal void ValidateBoundsShape(GltfAccessorType accessorType)
    {
        switch (accessorType)
        {
            case GltfAccessorType.Scalar:
            case GltfAccessorType.Vector2:
            case GltfAccessorType.Vector3:
            case GltfAccessorType.Vector4:
                return;
            default:
                throw new ArgumentException(
                    "Accessor bounds are supported only for scalar and vector elements.",
                    "accessorType");
        }
    }

    internal bool BoundsMatch(
        double[] declared,
        double[] calculated,
        GltfComponentType componentType)
    {
        if (declared == null
            || calculated == null
            || declared.Length != calculated.Length)
        {
            return false;
        }

        int index = 0;
        while (index < declared.Length)
        {
            double declaredValue = NormalizeDeclaredBound(
                declared[index],
                componentType);
            if (!double.IsFinite(declaredValue)
                || declaredValue != calculated[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool AreBoundsFinite(
        double[] bounds,
        GltfComponentType componentType)
    {
        int index = 0;
        while (index < bounds.Length)
        {
            double value = NormalizeDeclaredBound(
                bounds[index],
                componentType);
            if (!IsDeclaredBoundValueValid(value, componentType))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsDeclaredBoundValueValid(
        double value,
        GltfComponentType componentType)
    {
        if (!double.IsFinite(value))
        {
            return false;
        }

        switch (componentType)
        {
            case GltfComponentType.SignedByte:
                return value == Math.Truncate(value)
                    && value >= -128.0
                    && value <= 127.0;
            case GltfComponentType.UnsignedByte:
                return value == Math.Truncate(value)
                    && value >= 0.0
                    && value <= 255.0;
            case GltfComponentType.SignedShort:
                return value == Math.Truncate(value)
                    && value >= -32768.0
                    && value <= 32767.0;
            case GltfComponentType.UnsignedShort:
                return value == Math.Truncate(value)
                    && value >= 0.0
                    && value <= 65535.0;
            case GltfComponentType.UnsignedInt:
                return value == Math.Truncate(value)
                    && value >= 0.0
                    && value <= 4294967295.0;
            case GltfComponentType.Float:
                return true;
            default:
                return false;
        }
    }

    internal double NormalizeDeclaredBound(
        double value,
        GltfComponentType componentType)
    {
        if (componentType == GltfComponentType.Float)
        {
            return (float)value;
        }

        return value;
    }
}
