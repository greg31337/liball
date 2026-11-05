using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace gltf;

internal class AccessorSparseConversionResult
{
    internal bool Changed;
    internal int Buffer = GltfConstants.AbsentIndex;
    internal int DenseBufferView = GltfConstants.AbsentIndex;
    internal int SparseIndicesBufferView = GltfConstants.AbsentIndex;
    internal int SparseValuesBufferView = GltfConstants.AbsentIndex;
    internal int SparseCount;
}

internal class AccessorSparseConverter
{
    internal AccessorLayout Layout = new AccessorLayout();
    internal AccessorDecoder Decoder = new AccessorDecoder();
    internal AccessorNormalization Normalization = new AccessorNormalization();
    internal SparseAccessorDecoder SparseDecoder = new SparseAccessorDecoder();

    internal AccessorSparseConversionResult MaterializeSparseAccessor(
        GltfDocument document,
        int accessorIndex)
    {
        GltfAccessor accessor = GetRequiredAccessor(document, accessorIndex);
        if (accessor.Sparse == null)
        {
            throw new ArgumentException(
                $"Accessor {accessorIndex} does not define sparse storage.",
                "accessorIndex");
        }

        GltfAccessorSparse sparse = accessor.Sparse;
        AccessorByteRange? baseRange = null;
        byte[] baseSource = [];
        GltfBufferView? baseView = null;
        if (accessor.HasBufferView)
        {
            baseView = GetRequiredBufferView(
                document,
                accessor.BufferView,
                "accessor.BufferView");
            baseSource = GetResolvedBufferBytes(
                document,
                baseView,
                "accessor.BufferView");
            baseRange = Layout.CalculateAccessorByteRange(
                accessor,
                Layout.CalculateBufferViewRange(baseView));
        }

        GltfBufferView indicesView = GetRequiredBufferView(
            document,
            sparse.Indices.BufferView,
            "sparse.Indices.BufferView");
        GltfBufferView valuesView = GetRequiredBufferView(
            document,
            sparse.Values.BufferView,
            "sparse.Values.BufferView");
        byte[] indicesSource = GetResolvedBufferBytes(
            document,
            indicesView,
            "sparse.Indices.BufferView");
        byte[] valuesSource = GetResolvedBufferBytes(
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
        byte[] denseBytes = SparseDecoder.OverlayRawElements(
            baseSource,
            baseRange,
            indicesSource,
            indicesRange,
            valuesSource,
            valuesRange,
            accessor.Count);

        int bufferIndex = document.Buffers.Count;
        int bufferViewIndex = document.BufferViews.Count;
        GltfBuffer generatedBuffer = CreateGeneratedBuffer(denseBytes);
        GltfBufferView generatedView = CreateGeneratedBufferView(
            bufferIndex,
            0,
            denseBytes.Length);
        if (baseView != null && baseView.HasTarget)
        {
            generatedView.HasTarget = true;
            generatedView.Target = baseView.Target;
        }

        document.Buffers.Add(generatedBuffer);
        document.BufferViews.Add(generatedView);
        accessor.HasBufferView = true;
        accessor.BufferView = bufferViewIndex;
        accessor.HasByteOffset = false;
        accessor.ByteOffset = 0;
        accessor.Sparse = null;

        AccessorSparseConversionResult result = new AccessorSparseConversionResult();
        result.Changed = true;
        result.Buffer = bufferIndex;
        result.DenseBufferView = bufferViewIndex;
        return result;
    }

    internal AccessorSparseConversionResult GenerateSparseAccessor(
        GltfDocument document,
        int accessorIndex,
        double zeroTolerance)
    {
        ValidateZeroTolerance(zeroTolerance);
        GltfAccessor accessor = GetRequiredAccessor(document, accessorIndex);
        if (accessor.Sparse != null)
        {
            throw new ArgumentException(
                $"Accessor {accessorIndex} already defines sparse storage.",
                "accessorIndex");
        }

        if (!accessor.HasBufferView)
        {
            throw new ArgumentException(
                $"Accessor {accessorIndex} does not define dense buffer storage.",
                "accessorIndex");
        }

        GltfBufferView baseView = GetRequiredBufferView(
            document,
            accessor.BufferView,
            "accessor.BufferView");
        byte[] baseSource = GetResolvedBufferBytes(
            document,
            baseView,
            "accessor.BufferView");
        AccessorByteRange baseRange = Layout.CalculateAccessorByteRange(
            accessor,
            Layout.CalculateBufferViewRange(baseView));
        byte[] packedElements = Decoder.DecodeRawElements(baseSource, baseRange);
        double[] logicalValues = Decoder.DecodeFloatingValues(baseSource, baseRange);
        if (accessor.Normalized)
        {
            ApplyNormalization(logicalValues, accessor.ComponentType);
        }

        List<int> nonzeroElements = FindNonzeroElements(
            logicalValues,
            baseRange.ElementLayout.ComponentCount,
            accessor.Count,
            zeroTolerance);
        AccessorSparseConversionResult result = new AccessorSparseConversionResult();
        result.Changed = true;
        result.SparseCount = nonzeroElements.Count;
        if (nonzeroElements.Count == 0)
        {
            accessor.HasBufferView = false;
            accessor.BufferView = GltfConstants.AbsentIndex;
            accessor.HasByteOffset = false;
            accessor.ByteOffset = 0;
            accessor.Sparse = null;
            return result;
        }

        GltfComponentType indexComponentType = SelectSparseIndexComponentType(
            nonzeroElements[nonzeroElements.Count - 1]);
        byte[] indexBytes = EncodeSparseIndices(
            nonzeroElements,
            indexComponentType);
        byte[] valueBytes = CopySparseValues(
            packedElements,
            nonzeroElements,
            baseRange.ElementLayout.ElementByteLength);
        int valuesByteOffset = FourByteAlignment.Align(indexBytes.Length);
        int bufferByteLength = CheckedArithmetic.AddNonNegativeInt32(
            valuesByteOffset,
            valueBytes.Length);
        byte[] generatedBytes = new byte[bufferByteLength];
        indexBytes.CopyTo(generatedBytes, 0);
        valueBytes.CopyTo(generatedBytes, valuesByteOffset);

        int bufferIndex = document.Buffers.Count;
        int indicesViewIndex = document.BufferViews.Count;
        int valuesViewIndex = CheckedArithmetic.AddNonNegativeInt32(
            indicesViewIndex,
            1);
        GltfBuffer generatedBuffer = CreateGeneratedBuffer(generatedBytes);
        GltfBufferView indicesView = CreateGeneratedBufferView(
            bufferIndex,
            0,
            indexBytes.Length);
        GltfBufferView valuesView = CreateGeneratedBufferView(
            bufferIndex,
            valuesByteOffset,
            valueBytes.Length);
        GltfAccessorSparse sparse = new GltfAccessorSparse();
        sparse.Count = nonzeroElements.Count;
        sparse.Indices.BufferView = indicesViewIndex;
        sparse.Indices.ComponentType = indexComponentType;
        sparse.Values.BufferView = valuesViewIndex;

        document.Buffers.Add(generatedBuffer);
        document.BufferViews.Add(indicesView);
        document.BufferViews.Add(valuesView);
        accessor.HasBufferView = false;
        accessor.BufferView = GltfConstants.AbsentIndex;
        accessor.HasByteOffset = false;
        accessor.ByteOffset = 0;
        accessor.Sparse = sparse;
        result.Buffer = bufferIndex;
        result.SparseIndicesBufferView = indicesViewIndex;
        result.SparseValuesBufferView = valuesViewIndex;
        return result;
    }

    internal GltfAccessor GetRequiredAccessor(
        GltfDocument document,
        int accessorIndex)
    {
        if (document == null || document.Accessors == null)
        {
            throw new ArgumentNullException("document");
        }

        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count)
        {
            throw new ArgumentOutOfRangeException("accessorIndex");
        }

        GltfAccessor accessor = document.Accessors[accessorIndex];
        if (accessor == null)
        {
            throw new ArgumentException(
                $"Accessor {accessorIndex} is null.",
                "accessorIndex");
        }

        return accessor;
    }

    internal GltfBufferView GetRequiredBufferView(
        GltfDocument document,
        int bufferViewIndex,
        string parameterName)
    {
        if (document.BufferViews == null
            || bufferViewIndex < 0
            || bufferViewIndex >= document.BufferViews.Count)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        GltfBufferView bufferView = document.BufferViews[bufferViewIndex];
        if (bufferView == null)
        {
            throw new ArgumentException(
                $"Buffer view {bufferViewIndex} is null.",
                parameterName);
        }

        return bufferView;
    }

    internal byte[] GetResolvedBufferBytes(
        GltfDocument document,
        GltfBufferView bufferView,
        string parameterName)
    {
        if (document.Buffers == null
            || bufferView.Buffer < 0
            || bufferView.Buffer >= document.Buffers.Count)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        GltfBuffer buffer = document.Buffers[bufferView.Buffer];
        if (buffer == null
            || !buffer.Resolved
            || buffer.ResolvedBytes == null)
        {
            throw new ArgumentException(
                $"Buffer {bufferView.Buffer} is not resolved.",
                parameterName);
        }

        return buffer.ResolvedBytes;
    }

    internal GltfBuffer CreateGeneratedBuffer(byte[] bytes)
    {
        GltfBuffer result = new GltfBuffer();
        result.ByteLength = bytes.Length;
        result.Resolved = true;
        result.ResolvedBytes = bytes;
        return result;
    }

    internal GltfBufferView CreateGeneratedBufferView(
        int bufferIndex,
        int byteOffset,
        int byteLength)
    {
        GltfBufferView result = new GltfBufferView();
        result.Buffer = bufferIndex;
        if (byteOffset != 0)
        {
            result.HasByteOffset = true;
            result.ByteOffset = byteOffset;
        }

        result.ByteLength = byteLength;
        return result;
    }

    internal void ValidateZeroTolerance(double zeroTolerance)
    {
        if (!double.IsFinite(zeroTolerance) || zeroTolerance < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                "zeroTolerance",
                "The sparse zero tolerance must be finite and non-negative.");
        }
    }

    internal void ApplyNormalization(
        double[] values,
        GltfComponentType componentType)
    {
        int index = 0;
        switch (componentType)
        {
            case GltfComponentType.SignedByte:
            case GltfComponentType.SignedShort:
                while (index < values.Length)
                {
                    values[index] = Normalization.DecodeSignedValue(
                        (int)values[index],
                        componentType);
                    index++;
                }

                return;
            case GltfComponentType.UnsignedByte:
            case GltfComponentType.UnsignedShort:
                while (index < values.Length)
                {
                    values[index] = Normalization.DecodeUnsignedValue(
                        (uint)values[index],
                        componentType);
                    index++;
                }

                return;
            default:
                throw new ArgumentException(
                    "Normalized accessors require 8-bit or 16-bit integer components.",
                    "componentType");
        }
    }

    internal List<int> FindNonzeroElements(
        double[] values,
        int componentCount,
        int elementCount,
        double zeroTolerance)
    {
        int expectedValueCount = CheckedArithmetic.MultiplyNonNegativeInt32(
            componentCount,
            elementCount);
        if (values == null || values.Length != expectedValueCount)
        {
            throw new ArgumentException(
                "The logical value count does not match the accessor layout.",
                "values");
        }

        List<int> result = new List<int>();
        int elementIndex = 0;
        int valueIndex = 0;
        while (elementIndex < elementCount)
        {
            bool nonzero = false;
            int componentIndex = 0;
            while (componentIndex < componentCount)
            {
                double value = values[valueIndex];
                if (value < -zeroTolerance || value > zeroTolerance
                    || !double.IsFinite(value))
                {
                    nonzero = true;
                }

                componentIndex++;
                valueIndex++;
            }

            if (nonzero)
            {
                result.Add(elementIndex);
            }

            elementIndex++;
        }

        return result;
    }

    internal GltfComponentType SelectSparseIndexComponentType(int maximumIndex)
    {
        if (maximumIndex < 0)
        {
            throw new ArgumentOutOfRangeException("maximumIndex");
        }

        if (maximumIndex <= byte.MaxValue)
        {
            return GltfComponentType.UnsignedByte;
        }

        if (maximumIndex <= ushort.MaxValue)
        {
            return GltfComponentType.UnsignedShort;
        }

        return GltfComponentType.UnsignedInt;
    }

    internal byte[] EncodeSparseIndices(
        List<int> indices,
        GltfComponentType componentType)
    {
        if (indices == null)
        {
            throw new ArgumentNullException("indices");
        }

        if (componentType != GltfComponentType.UnsignedByte
            && componentType != GltfComponentType.UnsignedShort
            && componentType != GltfComponentType.UnsignedInt)
        {
            throw new ArgumentException(
                "Sparse indices require an unsigned integer component type.",
                "componentType");
        }

        int componentByteLength = Layout.GetComponentByteLength(componentType);
        int outputByteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
            indices.Count,
            componentByteLength);
        byte[] result = new byte[outputByteLength];
        Span<byte> output = result;
        int index = 0;
        int previous = -1;
        while (index < indices.Count)
        {
            int value = indices[index];
            if (value < 0 || value <= previous)
            {
                throw new ArgumentException(
                    "Sparse indices must be non-negative and strictly increasing.",
                    "indices");
            }

            int offset = CheckedArithmetic.MultiplyNonNegativeInt32(
                index,
                componentByteLength);
            switch (componentType)
            {
                case GltfComponentType.UnsignedByte:
                    if (value > byte.MaxValue)
                    {
                        throw new ArgumentOutOfRangeException("indices");
                    }

                    output[offset] = (byte)value;
                    break;
                case GltfComponentType.UnsignedShort:
                    if (value > ushort.MaxValue)
                    {
                        throw new ArgumentOutOfRangeException("indices");
                    }

                    BinaryPrimitives.WriteUInt16LittleEndian(
                        output.Slice(offset, 2),
                        (ushort)value);
                    break;
                case GltfComponentType.UnsignedInt:
                    BinaryPrimitives.WriteUInt32LittleEndian(
                        output.Slice(offset, 4),
                        (uint)value);
                    break;
                default:
                    throw new ArgumentException(
                        "Sparse indices require an unsigned integer component type.",
                        "componentType");
            }

            previous = value;
            index++;
        }

        return result;
    }

    internal byte[] CopySparseValues(
        byte[] packedElements,
        List<int> indices,
        int elementByteLength)
    {
        if (packedElements == null || indices == null)
        {
            throw new ArgumentNullException("packedElements");
        }

        int outputByteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
            indices.Count,
            elementByteLength);
        byte[] result = new byte[outputByteLength];
        ReadOnlySpan<byte> source = packedElements;
        Span<byte> output = result;
        int index = 0;
        while (index < indices.Count)
        {
            int sourceOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
                indices[index],
                elementByteLength);
            int destinationOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
                index,
                elementByteLength);
            source.Slice(sourceOffset, elementByteLength).CopyTo(
                output.Slice(destinationOffset, elementByteLength));
            index++;
        }

        return result;
    }
}
