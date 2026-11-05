using System;
using System.Buffers.Binary;

namespace gltf;

internal class AccessorEncodingResult
{
    internal byte[] Bytes = [];
    internal GltfComponentType ComponentType = GltfComponentType.Float;
    internal GltfAccessorType Type = GltfAccessorType.Scalar;
    internal int Count;
    internal bool Normalized;
    internal bool IndexData;
}

internal class AccessorCreationResult
{
    internal int Buffer = GltfConstants.AbsentIndex;
    internal int BufferView = GltfConstants.AbsentIndex;
    internal int Accessor = GltfConstants.AbsentIndex;
}

internal class AccessorEncoder
{
    internal AccessorLayout Layout = new AccessorLayout();
    internal AccessorNormalization Normalization = new AccessorNormalization();
    internal AccessorBoundsCalculator BoundsCalculator = new AccessorBoundsCalculator();

    internal AccessorEncodingResult EncodeFloatingAccessor(
        double[] values,
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        bool normalized)
    {
        ValidateScalarOrVectorShape(accessorType);
        return EncodeFloatingComponents(
            values,
            componentType,
            accessorType,
            normalized);
    }

    internal AccessorEncodingResult EncodeSignedAccessor(
        int[] values,
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        bool normalized)
    {
        ValidateScalarOrVectorShape(accessorType);
        if (componentType != GltfComponentType.SignedByte
            && componentType != GltfComponentType.SignedShort)
        {
            throw new ArgumentException(
                "Signed input requires signed byte or signed short components.",
                "componentType");
        }

        AccessorElementLayout elementLayout = Layout.CalculateElementLayout(
            componentType,
            accessorType);
        int count = ValidateLogicalValueCount(values, elementLayout.ComponentCount);
        byte[] bytes = CreateElementBytes(elementLayout, count);
        int valueIndex = 0;
        int elementIndex = 0;
        while (elementIndex < count)
        {
            int componentIndex = 0;
            while (componentIndex < elementLayout.ComponentCount)
            {
                int offset = CalculateComponentOffset(
                    elementLayout,
                    elementIndex,
                    componentIndex);
                WriteSignedComponent(
                    bytes,
                    offset,
                    values[valueIndex],
                    componentType);
                componentIndex++;
                valueIndex++;
            }

            elementIndex++;
        }

        return CreateEncodingResult(
            bytes,
            componentType,
            accessorType,
            count,
            normalized,
            false);
    }

    internal AccessorEncodingResult EncodeUnsignedAccessor(
        uint[] values,
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        bool normalized)
    {
        ValidateScalarOrVectorShape(accessorType);
        if (componentType != GltfComponentType.UnsignedByte
            && componentType != GltfComponentType.UnsignedShort)
        {
            throw new ArgumentException(
                "Unsigned attribute input requires unsigned byte or unsigned short components.",
                "componentType");
        }

        return EncodeUnsignedComponents(
            values,
            componentType,
            accessorType,
            normalized,
            false);
    }

    internal AccessorEncodingResult EncodeIndexAccessor(
        uint[] indices,
        GltfComponentType componentType)
    {
        if (componentType != GltfComponentType.UnsignedByte
            && componentType != GltfComponentType.UnsignedShort
            && componentType != GltfComponentType.UnsignedInt)
        {
            throw new ArgumentException(
                "Index input requires unsigned byte, unsigned short, or unsigned int components.",
                "componentType");
        }

        return EncodeUnsignedComponents(
            indices,
            componentType,
            GltfAccessorType.Scalar,
            false,
            true);
    }

    internal AccessorEncodingResult EncodeMatrixAccessor(
        double[] values,
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        bool normalized)
    {
        ValidateMatrixShape(accessorType);
        return EncodeFloatingComponents(
            values,
            componentType,
            accessorType,
            normalized);
    }

    internal AccessorEncodingResult EncodeRawAccessor(
        byte[] rawBytes,
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        bool normalized,
        bool indexData)
    {
        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        ValidateNormalization(componentType, normalized);
        if (indexData)
        {
            if (normalized
                || accessorType != GltfAccessorType.Scalar
                || (componentType != GltfComponentType.UnsignedByte
                    && componentType != GltfComponentType.UnsignedShort
                    && componentType != GltfComponentType.UnsignedInt))
            {
                throw new ArgumentException(
                    "Raw index input requires scalar unsigned integer elements.",
                    "indexData");
            }
        }
        else if (componentType == GltfComponentType.UnsignedInt)
        {
            throw new ArgumentException(
                "Unsigned int components are reserved for index accessors.",
                "componentType");
        }

        AccessorElementLayout elementLayout = Layout.CalculateElementLayout(
            componentType,
            accessorType);
        if (rawBytes.Length == 0
            || rawBytes.Length % elementLayout.ElementByteLength != 0)
        {
            throw new ArgumentException(
                "Raw input must contain one or more complete stored elements, including matrix padding.",
                "rawBytes");
        }

        int count = rawBytes.Length / elementLayout.ElementByteLength;
        byte[] ownedBytes = new byte[rawBytes.Length];
        rawBytes.CopyTo(ownedBytes, 0);
        return CreateEncodingResult(
            ownedBytes,
            componentType,
            accessorType,
            count,
            normalized,
            indexData);
    }

    internal AccessorCreationResult CreateAccessorStorage(
        GltfDocument document,
        AccessorEncodingResult encoding,
        bool calculateBounds)
    {
        ValidateCreationInput(document, encoding);
        byte[] ownedBytes = new byte[encoding.Bytes.Length];
        encoding.Bytes.CopyTo(ownedBytes, 0);
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = ownedBytes.Length;
        buffer.Resolved = true;
        buffer.ResolvedBytes = ownedBytes;
        GltfBufferView bufferView = new GltfBufferView();
        bufferView.Buffer = document.Buffers.Count;
        bufferView.ByteLength = ownedBytes.Length;
        if (encoding.IndexData)
        {
            bufferView.HasTarget = true;
            bufferView.Target = GltfBufferViewTarget.ElementArrayBuffer;
        }

        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = document.BufferViews.Count;
        accessor.ComponentType = encoding.ComponentType;
        accessor.Count = encoding.Count;
        accessor.Type = encoding.Type;
        if (encoding.Normalized)
        {
            accessor.HasNormalized = true;
            accessor.Normalized = true;
        }

        if (calculateBounds && IsScalarOrVectorShape(encoding.Type))
        {
            CalculateCreatedBounds(buffer, bufferView, accessor);
        }

        AccessorCreationResult result = new AccessorCreationResult();
        result.Buffer = document.Buffers.Count;
        result.BufferView = document.BufferViews.Count;
        result.Accessor = document.Accessors.Count;
        document.Buffers.Add(buffer);
        document.BufferViews.Add(bufferView);
        document.Accessors.Add(accessor);
        return result;
    }

    internal AccessorEncodingResult EncodeFloatingComponents(
        double[] values,
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        bool normalized)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        ValidateFloatingConfiguration(componentType, normalized);
        AccessorElementLayout elementLayout = Layout.CalculateElementLayout(
            componentType,
            accessorType);
        int count = ValidateLogicalValueCount(values, elementLayout.ComponentCount);
        byte[] bytes = CreateElementBytes(elementLayout, count);
        int valueIndex = 0;
        int elementIndex = 0;
        while (elementIndex < count)
        {
            int componentIndex = 0;
            while (componentIndex < elementLayout.ComponentCount)
            {
                int offset = CalculateComponentOffset(
                    elementLayout,
                    elementIndex,
                    componentIndex);
                WriteFloatingComponent(
                    bytes,
                    offset,
                    values[valueIndex],
                    componentType,
                    normalized);
                componentIndex++;
                valueIndex++;
            }

            elementIndex++;
        }

        return CreateEncodingResult(
            bytes,
            componentType,
            accessorType,
            count,
            normalized,
            false);
    }

    internal AccessorEncodingResult EncodeUnsignedComponents(
        uint[] values,
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        bool normalized,
        bool indexData)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        ValidateNormalization(componentType, normalized);
        AccessorElementLayout elementLayout = Layout.CalculateElementLayout(
            componentType,
            accessorType);
        int count = ValidateLogicalValueCount(values, elementLayout.ComponentCount);
        byte[] bytes = CreateElementBytes(elementLayout, count);
        int valueIndex = 0;
        int elementIndex = 0;
        while (elementIndex < count)
        {
            int componentIndex = 0;
            while (componentIndex < elementLayout.ComponentCount)
            {
                int offset = CalculateComponentOffset(
                    elementLayout,
                    elementIndex,
                    componentIndex);
                WriteUnsignedComponent(
                    bytes,
                    offset,
                    values[valueIndex],
                    componentType);
                componentIndex++;
                valueIndex++;
            }

            elementIndex++;
        }

        return CreateEncodingResult(
            bytes,
            componentType,
            accessorType,
            count,
            normalized,
            indexData);
    }

    internal void WriteFloatingComponent(
        byte[] destination,
        int offset,
        double value,
        GltfComponentType componentType,
        bool normalized)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                "values",
                "Floating accessor values must be finite.");
        }

        if (componentType == GltfComponentType.Float)
        {
            float encoded = (float)value;
            if (!float.IsFinite(encoded))
            {
                throw new ArgumentOutOfRangeException(
                    "values",
                    "A floating accessor value is outside the finite single-precision range.");
            }

            BinaryPrimitives.WriteSingleLittleEndian(
                destination.AsSpan(offset, 4),
                encoded);
            return;
        }

        if (componentType == GltfComponentType.SignedByte
            || componentType == GltfComponentType.SignedShort)
        {
            int encoded = Normalization.EncodeSignedValue(value, componentType);
            WriteSignedComponent(destination, offset, encoded, componentType);
            return;
        }

        uint unsignedEncoded = Normalization.EncodeUnsignedValue(
            value,
            componentType);
        WriteUnsignedComponent(
            destination,
            offset,
            unsignedEncoded,
            componentType);
    }

    internal void WriteSignedComponent(
        byte[] destination,
        int offset,
        int value,
        GltfComponentType componentType)
    {
        switch (componentType)
        {
            case GltfComponentType.SignedByte:
                if (value < sbyte.MinValue || value > sbyte.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("values");
                }

                destination[offset] = (byte)(value & byte.MaxValue);
                return;
            case GltfComponentType.SignedShort:
                if (value < short.MinValue || value > short.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("values");
                }

                BinaryPrimitives.WriteInt16LittleEndian(
                    destination.AsSpan(offset, 2),
                    (short)value);
                return;
            default:
                throw new ArgumentException(
                    "Signed input requires signed byte or signed short components.",
                    "componentType");
        }
    }

    internal void WriteUnsignedComponent(
        byte[] destination,
        int offset,
        uint value,
        GltfComponentType componentType)
    {
        switch (componentType)
        {
            case GltfComponentType.UnsignedByte:
                if (value > byte.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("values");
                }

                destination[offset] = (byte)value;
                return;
            case GltfComponentType.UnsignedShort:
                if (value > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("values");
                }

                BinaryPrimitives.WriteUInt16LittleEndian(
                    destination.AsSpan(offset, 2),
                    (ushort)value);
                return;
            case GltfComponentType.UnsignedInt:
                BinaryPrimitives.WriteUInt32LittleEndian(
                    destination.AsSpan(offset, 4),
                    value);
                return;
            default:
                throw new ArgumentException(
                    "Unsigned input requires an unsigned integer component type.",
                    "componentType");
        }
    }

    internal byte[] CreateElementBytes(
        AccessorElementLayout elementLayout,
        int count)
    {
        int byteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
            elementLayout.ElementByteLength,
            count);
        return new byte[byteLength];
    }

    internal int CalculateComponentOffset(
        AccessorElementLayout elementLayout,
        int elementIndex,
        int componentIndex)
    {
        int elementOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
            elementIndex,
            elementLayout.ElementByteLength);
        return CheckedArithmetic.AddNonNegativeInt32(
            elementOffset,
            elementLayout.ComponentByteOffsets[componentIndex]);
    }

    internal int ValidateLogicalValueCount(
        Array values,
        int componentCount)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (values.Length == 0 || values.Length % componentCount != 0)
        {
            throw new ArgumentException(
                "Typed input must contain one or more complete logical elements.",
                "values");
        }

        return values.Length / componentCount;
    }

    internal void ValidateFloatingConfiguration(
        GltfComponentType componentType,
        bool normalized)
    {
        if (componentType == GltfComponentType.Float)
        {
            if (normalized)
            {
                throw new ArgumentException(
                    "Float components cannot be normalized.",
                    "normalized");
            }

            return;
        }

        if (!normalized
            || (componentType != GltfComponentType.SignedByte
                && componentType != GltfComponentType.UnsignedByte
                && componentType != GltfComponentType.SignedShort
                && componentType != GltfComponentType.UnsignedShort))
        {
            throw new ArgumentException(
                "Floating input requires float components or normalized 8-bit or 16-bit integer components.",
                "componentType");
        }
    }

    internal void ValidateNormalization(
        GltfComponentType componentType,
        bool normalized)
    {
        if (normalized
            && componentType != GltfComponentType.SignedByte
            && componentType != GltfComponentType.UnsignedByte
            && componentType != GltfComponentType.SignedShort
            && componentType != GltfComponentType.UnsignedShort)
        {
            throw new ArgumentException(
                "Normalized accessors require 8-bit or 16-bit integer components.",
                "normalized");
        }
    }

    internal void ValidateScalarOrVectorShape(GltfAccessorType accessorType)
    {
        if (!IsScalarOrVectorShape(accessorType))
        {
            throw new ArgumentException(
                "This typed input requires scalar or vector accessor elements.",
                "accessorType");
        }
    }

    internal bool IsScalarOrVectorShape(GltfAccessorType accessorType)
    {
        switch (accessorType)
        {
            case GltfAccessorType.Scalar:
            case GltfAccessorType.Vector2:
            case GltfAccessorType.Vector3:
            case GltfAccessorType.Vector4:
                return true;
            default:
                return false;
        }
    }

    internal void ValidateMatrixShape(GltfAccessorType accessorType)
    {
        switch (accessorType)
        {
            case GltfAccessorType.Matrix2:
            case GltfAccessorType.Matrix3:
            case GltfAccessorType.Matrix4:
                return;
            default:
                throw new ArgumentException(
                    "Matrix input requires a matrix accessor type.",
                    "accessorType");
        }
    }

    internal AccessorEncodingResult CreateEncodingResult(
        byte[] bytes,
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        int count,
        bool normalized,
        bool indexData)
    {
        AccessorEncodingResult result = new AccessorEncodingResult();
        result.Bytes = bytes;
        result.ComponentType = componentType;
        result.Type = accessorType;
        result.Count = count;
        result.Normalized = normalized;
        result.IndexData = indexData;
        return result;
    }

    internal void ValidateCreationInput(
        GltfDocument document,
        AccessorEncodingResult encoding)
    {
        if (document == null
            || document.Buffers == null
            || document.BufferViews == null
            || document.Accessors == null)
        {
            throw new ArgumentNullException("document");
        }

        if (encoding == null || encoding.Bytes == null)
        {
            throw new ArgumentNullException("encoding");
        }

        AccessorElementLayout elementLayout = Layout.CalculateElementLayout(
            encoding.ComponentType,
            encoding.Type);
        int expectedByteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
            encoding.Count,
            elementLayout.ElementByteLength);
        if (encoding.Count < 1 || encoding.Bytes.Length != expectedByteLength)
        {
            throw new ArgumentException(
                "The encoded byte length does not match the accessor layout.",
                "encoding");
        }

        ValidateNormalization(encoding.ComponentType, encoding.Normalized);
        if (encoding.IndexData
            && (encoding.Normalized
                || encoding.Type != GltfAccessorType.Scalar
                || (encoding.ComponentType != GltfComponentType.UnsignedByte
                    && encoding.ComponentType != GltfComponentType.UnsignedShort
                    && encoding.ComponentType != GltfComponentType.UnsignedInt)))
        {
            throw new ArgumentException(
                "Index storage requires scalar unsigned integer elements.",
                "encoding");
        }

        if (!encoding.IndexData
            && encoding.ComponentType == GltfComponentType.UnsignedInt)
        {
            throw new ArgumentException(
                "Unsigned int components are reserved for index accessors.",
                "encoding");
        }
    }

    internal void CalculateCreatedBounds(
        GltfBuffer buffer,
        GltfBufferView bufferView,
        GltfAccessor accessor)
    {
        int destinationBuffer = bufferView.Buffer;
        int destinationBufferView = accessor.BufferView;
        GltfDocument temporary = new GltfDocument();
        bufferView.Buffer = 0;
        accessor.BufferView = 0;
        temporary.Buffers.Add(buffer);
        temporary.BufferViews.Add(bufferView);
        temporary.Accessors.Add(accessor);
        AccessorBoundsResult bounds = BoundsCalculator.CalculateAccessorBounds(
            temporary,
            0);
        if (!bounds.HasValues || !bounds.ValuesFinite)
        {
            throw new ArgumentException(
                "Accessor bounds require finite stored values.",
                "encoding");
        }

        bufferView.Buffer = destinationBuffer;
        accessor.BufferView = destinationBufferView;
        accessor.Minimum = bounds.Minimum;
        accessor.Maximum = bounds.Maximum;
    }
}
