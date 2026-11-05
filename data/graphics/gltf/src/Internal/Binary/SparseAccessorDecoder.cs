using System;

namespace gltf;

internal class SparseAccessorDecoder
{
    internal AccessorLayout Layout = new AccessorLayout();
    internal AccessorDecoder Decoder = new AccessorDecoder();

    internal AccessorByteRange CalculateSparseIndicesRange(
        GltfAccessorSparse sparse,
        BufferViewByteRange bufferView)
    {
        if (sparse == null || sparse.Indices == null)
        {
            throw new ArgumentNullException("sparse");
        }

        GltfAccessor payload = new GltfAccessor();
        payload.HasBufferView = true;
        payload.BufferView = sparse.Indices.BufferView;
        payload.HasByteOffset = sparse.Indices.HasByteOffset;
        payload.ByteOffset = sparse.Indices.ByteOffset;
        payload.ComponentType = sparse.Indices.ComponentType;
        payload.Count = sparse.Count;
        payload.Type = GltfAccessorType.Scalar;
        return Layout.CalculateAccessorByteRange(payload, bufferView);
    }

    internal AccessorByteRange CalculateSparseValuesRange(
        GltfAccessor accessor,
        GltfAccessorSparse sparse,
        BufferViewByteRange bufferView)
    {
        if (accessor == null || sparse == null || sparse.Values == null)
        {
            throw new ArgumentNullException("accessor");
        }

        GltfAccessor payload = new GltfAccessor();
        payload.HasBufferView = true;
        payload.BufferView = sparse.Values.BufferView;
        payload.HasByteOffset = sparse.Values.HasByteOffset;
        payload.ByteOffset = sparse.Values.ByteOffset;
        payload.ComponentType = accessor.ComponentType;
        payload.Count = sparse.Count;
        payload.Type = accessor.Type;
        return Layout.CalculateAccessorByteRange(payload, bufferView);
    }

    internal uint[] DecodeSparseIndices(
        ReadOnlySpan<byte> source,
        AccessorByteRange indicesRange,
        int accessorCount)
    {
        ValidateSparseIndicesLayout(indicesRange, accessorCount);
        uint[] result = Decoder.DecodeIndices(source, indicesRange);
        uint previous = 0U;
        int index = 0;
        while (index < result.Length)
        {
            uint value = result[index];
            if ((long)value >= accessorCount)
            {
                throw new ArgumentOutOfRangeException(
                    "indicesRange",
                    $"Sparse index {value} at position {index} is outside accessor count {accessorCount}.");
            }

            if (index > 0 && value <= previous)
            {
                throw new ArgumentException(
                    $"Sparse index {value} at position {index} is not strictly greater than {previous}.",
                    "indicesRange");
            }

            previous = value;
            index++;
        }

        return result;
    }

    internal byte[] OverlayRawElements(
        ReadOnlySpan<byte> baseSource,
        AccessorByteRange? baseRange,
        ReadOnlySpan<byte> indicesSource,
        AccessorByteRange indicesRange,
        ReadOnlySpan<byte> valuesSource,
        AccessorByteRange valuesRange,
        int accessorCount)
    {
        ValidateSparseIndicesLayout(indicesRange, accessorCount);
        ValidateSparseValuesLayout(
            valuesRange,
            accessorCount,
            indicesRange.Count);
        uint[] indices = DecodeSparseIndices(
            indicesSource,
            indicesRange,
            accessorCount);
        byte[] sparseValues = Decoder.DecodeRawElements(
            valuesSource,
            valuesRange);
        int elementByteLength = valuesRange.ElementLayout.ElementByteLength;
        int outputByteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
            accessorCount,
            elementByteLength);
        byte[] result = new byte[outputByteLength];
        if (baseRange != null)
        {
            ValidateBaseLayout(baseRange, valuesRange, accessorCount);
            result = Decoder.DecodeRawElements(baseSource, baseRange);
        }

        ReadOnlySpan<byte> sparseBytes = sparseValues;
        Span<byte> output = result;
        int sparseIndex = 0;
        while (sparseIndex < indices.Length)
        {
            int sourceOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
                sparseIndex,
                elementByteLength);
            int destinationElement = CheckedArithmetic.ConvertUInt32ToInt32(
                indices[sparseIndex]);
            int destinationOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
                destinationElement,
                elementByteLength);
            sparseBytes.Slice(sourceOffset, elementByteLength).CopyTo(
                output.Slice(destinationOffset, elementByteLength));
            sparseIndex++;
        }

        return result;
    }

    internal void ValidateSparseIndicesLayout(
        AccessorByteRange indicesRange,
        int accessorCount)
    {
        ValidateSparseCount(indicesRange, accessorCount);
        if (indicesRange.ElementLayout.Type != GltfAccessorType.Scalar)
        {
            throw new ArgumentException(
                "Sparse indices must use scalar elements.",
                "indicesRange");
        }

        Decoder.ValidateUnsignedComponentType(indicesRange);
        ValidateSparseBufferView(indicesRange, "indicesRange");
        if (!Layout.IsComponentOffsetAligned(
                indicesRange.ElementLayout,
                indicesRange.AccessorByteOffset)
            || !Layout.IsElementOffsetAligned(
                indicesRange.ElementLayout,
                indicesRange.FirstElementBufferByteOffset))
        {
            throw new ArgumentException(
                "Sparse indices are not aligned to their component byte length.",
                "indicesRange");
        }
    }

    internal void ValidateSparseValuesLayout(
        AccessorByteRange valuesRange,
        int accessorCount,
        int sparseCount)
    {
        ValidateSparseCount(valuesRange, accessorCount);
        if (valuesRange.Count != sparseCount)
        {
            throw new ArgumentException(
                "Sparse index and value counts must match.",
                "valuesRange");
        }

        ValidateSparseBufferView(valuesRange, "valuesRange");
        if (!Layout.IsComponentOffsetAligned(
                valuesRange.ElementLayout,
                valuesRange.AccessorByteOffset)
            || !Layout.IsElementOffsetAligned(
                valuesRange.ElementLayout,
                valuesRange.FirstElementBufferByteOffset))
        {
            throw new ArgumentException(
                "Sparse values are not aligned to their accessor element layout.",
                "valuesRange");
        }
    }

    internal void ValidateSparseCount(
        AccessorByteRange range,
        int accessorCount)
    {
        if (range == null)
        {
            throw new ArgumentNullException("range");
        }

        if (accessorCount < 0)
        {
            throw new ArgumentOutOfRangeException("accessorCount");
        }

        if (range.Count < 1 || range.Count > accessorCount)
        {
            throw new ArgumentOutOfRangeException(
                "range",
                $"Sparse count {range.Count} must be positive and no greater than accessor count {accessorCount}.");
        }
    }

    internal void ValidateSparseBufferView(
        AccessorByteRange range,
        string parameterName)
    {
        if (range.BufferView == null)
        {
            throw new ArgumentException(
                "Sparse payloads require a buffer view.",
                parameterName);
        }

        if (range.BufferView.HasByteStride || range.Interleaved)
        {
            throw new ArgumentException(
                "Sparse payload buffer views must not define byteStride.",
                parameterName);
        }

        if (range.BufferView.HasTarget)
        {
            throw new ArgumentException(
                "Sparse payload buffer views must not define target.",
                parameterName);
        }

        if (range.AccessorByteEnd > range.BufferView.ByteLength)
        {
            throw new ArgumentException(
                "The sparse payload byte range exceeds its buffer view.",
                parameterName);
        }
    }

    internal void ValidateBaseLayout(
        AccessorByteRange baseRange,
        AccessorByteRange valuesRange,
        int accessorCount)
    {
        if (baseRange.Count != accessorCount)
        {
            throw new ArgumentException(
                "The base accessor count does not match the sparse accessor count.",
                "baseRange");
        }

        if (baseRange.ElementLayout.ComponentType
                != valuesRange.ElementLayout.ComponentType
            || baseRange.ElementLayout.Type != valuesRange.ElementLayout.Type
            || baseRange.ElementLayout.ElementByteLength
                != valuesRange.ElementLayout.ElementByteLength)
        {
            throw new ArgumentException(
                "The base and sparse value element layouts do not match.",
                "baseRange");
        }
    }
}
