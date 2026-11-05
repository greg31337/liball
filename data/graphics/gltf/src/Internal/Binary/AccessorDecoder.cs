using System;
using System.Buffers.Binary;

namespace gltf;

internal class AccessorDecoder
{
    internal double[] DecodeFloatingValues(
        ReadOnlySpan<byte> source,
        AccessorByteRange range)
    {
        ValidateDecodeInput(source, range);
        int outputLength = CalculateLogicalComponentCount(range);
        double[] result = new double[outputLength];
        int elementIndex = 0;
        int outputIndex = 0;
        while (elementIndex < range.Count)
        {
            int componentIndex = 0;
            while (componentIndex < range.ElementLayout.ComponentCount)
            {
                int sourceOffset = CalculateComponentByteOffset(
                    range,
                    elementIndex,
                    componentIndex);
                result[outputIndex] = ReadFloatingComponent(
                    source,
                    sourceOffset,
                    range.ElementLayout.ComponentType);
                componentIndex++;
                outputIndex++;
            }

            elementIndex++;
        }

        return result;
    }

    internal int[] DecodeSignedValues(
        ReadOnlySpan<byte> source,
        AccessorByteRange range)
    {
        ValidateDecodeInput(source, range);
        if (range.ElementLayout.ComponentType != GltfComponentType.SignedByte
            && range.ElementLayout.ComponentType != GltfComponentType.SignedShort)
        {
            throw new ArgumentException(
                "Signed decoding requires signed byte or signed short components.",
                "range");
        }

        int outputLength = CalculateLogicalComponentCount(range);
        int[] result = new int[outputLength];
        int elementIndex = 0;
        int outputIndex = 0;
        while (elementIndex < range.Count)
        {
            int componentIndex = 0;
            while (componentIndex < range.ElementLayout.ComponentCount)
            {
                int sourceOffset = CalculateComponentByteOffset(
                    range,
                    elementIndex,
                    componentIndex);
                result[outputIndex] = ReadSignedComponent(
                    source,
                    sourceOffset,
                    range.ElementLayout.ComponentType);
                componentIndex++;
                outputIndex++;
            }

            elementIndex++;
        }

        return result;
    }

    internal uint[] DecodeUnsignedValues(
        ReadOnlySpan<byte> source,
        AccessorByteRange range)
    {
        ValidateDecodeInput(source, range);
        ValidateUnsignedComponentType(range);
        int outputLength = CalculateLogicalComponentCount(range);
        uint[] result = new uint[outputLength];
        int elementIndex = 0;
        int outputIndex = 0;
        while (elementIndex < range.Count)
        {
            int componentIndex = 0;
            while (componentIndex < range.ElementLayout.ComponentCount)
            {
                int sourceOffset = CalculateComponentByteOffset(
                    range,
                    elementIndex,
                    componentIndex);
                result[outputIndex] = ReadUnsignedComponent(
                    source,
                    sourceOffset,
                    range.ElementLayout.ComponentType);
                componentIndex++;
                outputIndex++;
            }

            elementIndex++;
        }

        return result;
    }

    internal uint[] DecodeIndices(
        ReadOnlySpan<byte> source,
        AccessorByteRange range)
    {
        ValidateDecodeInput(source, range);
        if (range.ElementLayout.Type != GltfAccessorType.Scalar)
        {
            throw new ArgumentException(
                "Index decoding requires scalar accessor elements.",
                "range");
        }

        ValidateUnsignedComponentType(range);
        return DecodeUnsignedValues(source, range);
    }

    internal double[] DecodeMatrixValues(
        ReadOnlySpan<byte> source,
        AccessorByteRange range)
    {
        ValidateDecodeInput(source, range);
        if (range.ElementLayout.ColumnCount < 2)
        {
            throw new ArgumentException(
                "Matrix decoding requires matrix accessor elements.",
                "range");
        }

        return DecodeFloatingValues(source, range);
    }

    internal byte[] DecodeRawElements(
        ReadOnlySpan<byte> source,
        AccessorByteRange range)
    {
        ValidateDecodeInput(source, range);
        int outputLength = CheckedArithmetic.MultiplyNonNegativeInt32(
            range.Count,
            range.ElementLayout.ElementByteLength);
        byte[] result = new byte[outputLength];
        Span<byte> output = result;
        int elementIndex = 0;
        int outputOffset = 0;
        while (elementIndex < range.Count)
        {
            int sourceOffset = CalculateElementByteOffset(range, elementIndex);
            source.Slice(
                sourceOffset,
                range.ElementLayout.ElementByteLength).CopyTo(
                    output.Slice(
                        outputOffset,
                        range.ElementLayout.ElementByteLength));
            outputOffset = CheckedArithmetic.AddNonNegativeInt32(
                outputOffset,
                range.ElementLayout.ElementByteLength);
            elementIndex++;
        }

        return result;
    }

    internal void ValidateDecodeInput(
        ReadOnlySpan<byte> source,
        AccessorByteRange range)
    {
        if (range == null || range.ElementLayout == null)
        {
            throw new ArgumentNullException("range");
        }

        if (range.Count < 0)
        {
            throw new ArgumentOutOfRangeException("range");
        }

        if (range.ElementLayout.ComponentCount < 1
            || range.ElementLayout.ComponentByteLength < 1
            || range.ElementLayout.ElementByteLength < 1
            || range.ElementByteStride < range.ElementLayout.ElementByteLength)
        {
            throw new ArgumentException("The accessor byte range has an invalid layout.", "range");
        }

        if (range.ElementLayout.ComponentByteOffsets == null
            || range.ElementLayout.ComponentByteOffsets.Count
                != range.ElementLayout.ComponentCount)
        {
            throw new ArgumentException("The accessor component offsets are incomplete.", "range");
        }

        int componentIndex = 0;
        while (componentIndex < range.ElementLayout.ComponentByteOffsets.Count)
        {
            int componentOffset = range.ElementLayout.ComponentByteOffsets[componentIndex];
            if (componentOffset < 0
                || (long)componentOffset
                    + range.ElementLayout.ComponentByteLength
                    > range.ElementLayout.ElementByteLength)
            {
                throw new ArgumentException("An accessor component offset is outside its element.", "range");
            }

            componentIndex++;
        }

        long requiredEnd = range.FirstElementBufferByteOffset;
        if (range.Count > 0)
        {
            requiredEnd += (long)(range.Count - 1) * range.ElementByteStride;
            requiredEnd += range.ElementLayout.ElementByteLength;
        }

        if (range.FirstElementBufferByteOffset < 0
            || requiredEnd < range.FirstElementBufferByteOffset
            || requiredEnd > source.Length)
        {
            throw new ArgumentException(
                "The accessor byte range is outside the source span.",
                "source");
        }
    }

    internal int CalculateLogicalComponentCount(AccessorByteRange range)
    {
        return CheckedArithmetic.MultiplyNonNegativeInt32(
            range.Count,
            range.ElementLayout.ComponentCount);
    }

    internal int CalculateElementByteOffset(
        AccessorByteRange range,
        int elementIndex)
    {
        long result = range.FirstElementBufferByteOffset
            + (long)elementIndex * range.ElementByteStride;
        return (int)result;
    }

    internal int CalculateComponentByteOffset(
        AccessorByteRange range,
        int elementIndex,
        int componentIndex)
    {
        return CheckedArithmetic.AddNonNegativeInt32(
            CalculateElementByteOffset(range, elementIndex),
            range.ElementLayout.ComponentByteOffsets[componentIndex]);
    }

    internal double ReadFloatingComponent(
        ReadOnlySpan<byte> source,
        int offset,
        GltfComponentType componentType)
    {
        switch (componentType)
        {
            case GltfComponentType.SignedByte:
                return ReadSignedByte(source, offset);
            case GltfComponentType.UnsignedByte:
                return source[offset];
            case GltfComponentType.SignedShort:
                return BinaryPrimitives.ReadInt16LittleEndian(source.Slice(offset, 2));
            case GltfComponentType.UnsignedShort:
                return BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset, 2));
            case GltfComponentType.UnsignedInt:
                return BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(offset, 4));
            case GltfComponentType.Float:
                return BinaryPrimitives.ReadSingleLittleEndian(source.Slice(offset, 4));
            default:
                throw new ArgumentOutOfRangeException("componentType");
        }
    }

    internal int ReadSignedComponent(
        ReadOnlySpan<byte> source,
        int offset,
        GltfComponentType componentType)
    {
        switch (componentType)
        {
            case GltfComponentType.SignedByte:
                return ReadSignedByte(source, offset);
            case GltfComponentType.SignedShort:
                return BinaryPrimitives.ReadInt16LittleEndian(source.Slice(offset, 2));
            default:
                throw new ArgumentOutOfRangeException("componentType");
        }
    }

    internal uint ReadUnsignedComponent(
        ReadOnlySpan<byte> source,
        int offset,
        GltfComponentType componentType)
    {
        switch (componentType)
        {
            case GltfComponentType.UnsignedByte:
                return source[offset];
            case GltfComponentType.UnsignedShort:
                return BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset, 2));
            case GltfComponentType.UnsignedInt:
                return BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(offset, 4));
            default:
                throw new ArgumentOutOfRangeException("componentType");
        }
    }

    internal int ReadSignedByte(ReadOnlySpan<byte> source, int offset)
    {
        int value = source[offset];
        if (value >= 128)
        {
            return value - 256;
        }

        return value;
    }

    internal void ValidateUnsignedComponentType(AccessorByteRange range)
    {
        if (range.ElementLayout.ComponentType != GltfComponentType.UnsignedByte
            && range.ElementLayout.ComponentType != GltfComponentType.UnsignedShort
            && range.ElementLayout.ComponentType != GltfComponentType.UnsignedInt)
        {
            throw new ArgumentException(
                "Unsigned decoding requires unsigned byte, unsigned short, or unsigned int components.",
                "range");
        }
    }
}
