using System.Collections.Generic;

namespace gltf;

internal class AccessorElementLayout
{
    internal GltfComponentType ComponentType = GltfComponentType.Float;
    internal GltfAccessorType Type = GltfAccessorType.Scalar;
    internal int ComponentByteLength;
    internal int ComponentCount;
    internal int RowCount;
    internal int ColumnCount;
    internal int LogicalColumnByteLength;
    internal int ColumnByteLength;
    internal int LogicalElementByteLength;
    internal int ElementByteLength;
    internal int ElementByteAlignment;
    internal bool HasColumnPadding;
    internal List<int> ComponentByteOffsets = new List<int>();
}

internal class BufferViewByteRange
{
    internal int Buffer;
    internal int ByteOffset;
    internal int ByteLength;
    internal long ByteEnd;
    internal bool HasByteStride;
    internal int ByteStride;
    internal bool HasTarget;
    internal GltfBufferViewTarget Target = GltfBufferViewTarget.None;
}

internal class AccessorByteRange
{
    internal AccessorElementLayout ElementLayout = new AccessorElementLayout();
    internal BufferViewByteRange? BufferView;
    internal int Count;
    internal int AccessorByteOffset;
    internal int ElementByteStride;
    internal bool Interleaved;
    internal bool TightlyPacked;
    internal bool HasElements;
    internal long AccessorByteLength;
    internal long AccessorByteEnd;
    internal long FirstElementBufferByteOffset;
    internal long FinalElementBufferByteOffset;
    internal long BufferByteEnd;
}

internal class AccessorLayout
{
    internal AccessorElementLayout CalculateElementLayout(
        GltfComponentType componentType,
        GltfAccessorType accessorType)
    {
        AccessorElementLayout result = new AccessorElementLayout();
        result.ComponentType = componentType;
        result.Type = accessorType;
        result.ComponentByteLength = GetComponentByteLength(componentType);
        result.RowCount = GetRowCount(accessorType);
        result.ColumnCount = GetColumnCount(accessorType);
        result.ComponentCount = CheckedArithmetic.MultiplyNonNegativeInt32(
            result.RowCount,
            result.ColumnCount);
        result.LogicalColumnByteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
            result.ComponentByteLength,
            result.RowCount);
        result.ColumnByteLength = result.LogicalColumnByteLength;
        result.ElementByteAlignment = result.ComponentByteLength;
        if (result.ColumnCount > 1)
        {
            result.ColumnByteLength = FourByteAlignment.Align(
                result.LogicalColumnByteLength);
            result.ElementByteAlignment = 4;
        }

        result.HasColumnPadding = result.ColumnByteLength
            != result.LogicalColumnByteLength;
        result.LogicalElementByteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
            result.ComponentByteLength,
            result.ComponentCount);
        result.ElementByteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
            result.ColumnByteLength,
            result.ColumnCount);

        int columnIndex = 0;
        while (columnIndex < result.ColumnCount)
        {
            int columnByteOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
                columnIndex,
                result.ColumnByteLength);
            int rowIndex = 0;
            while (rowIndex < result.RowCount)
            {
                int rowByteOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
                    rowIndex,
                    result.ComponentByteLength);
                result.ComponentByteOffsets.Add(
                    CheckedArithmetic.AddNonNegativeInt32(
                        columnByteOffset,
                        rowByteOffset));
                rowIndex++;
            }

            columnIndex++;
        }

        return result;
    }

    internal int GetComponentByteLength(GltfComponentType componentType)
    {
        switch (componentType)
        {
            case GltfComponentType.SignedByte:
            case GltfComponentType.UnsignedByte:
                return 1;
            case GltfComponentType.SignedShort:
            case GltfComponentType.UnsignedShort:
                return 2;
            case GltfComponentType.UnsignedInt:
            case GltfComponentType.Float:
                return 4;
            default:
                throw new System.ArgumentOutOfRangeException("componentType");
        }
    }

    internal int GetComponentCount(GltfAccessorType accessorType)
    {
        return CheckedArithmetic.MultiplyNonNegativeInt32(
            GetRowCount(accessorType),
            GetColumnCount(accessorType));
    }

    internal int GetRowCount(GltfAccessorType accessorType)
    {
        switch (accessorType)
        {
            case GltfAccessorType.Scalar:
                return 1;
            case GltfAccessorType.Vector2:
                return 2;
            case GltfAccessorType.Vector3:
                return 3;
            case GltfAccessorType.Vector4:
                return 4;
            case GltfAccessorType.Matrix2:
                return 2;
            case GltfAccessorType.Matrix3:
                return 3;
            case GltfAccessorType.Matrix4:
                return 4;
            default:
                throw new System.ArgumentOutOfRangeException("accessorType");
        }
    }

    internal int GetColumnCount(GltfAccessorType accessorType)
    {
        switch (accessorType)
        {
            case GltfAccessorType.Scalar:
            case GltfAccessorType.Vector2:
            case GltfAccessorType.Vector3:
            case GltfAccessorType.Vector4:
                return 1;
            case GltfAccessorType.Matrix2:
                return 2;
            case GltfAccessorType.Matrix3:
                return 3;
            case GltfAccessorType.Matrix4:
                return 4;
            default:
                throw new System.ArgumentOutOfRangeException("accessorType");
        }
    }

    internal bool IsKnownComponentType(GltfComponentType componentType)
    {
        switch (componentType)
        {
            case GltfComponentType.SignedByte:
            case GltfComponentType.UnsignedByte:
            case GltfComponentType.SignedShort:
            case GltfComponentType.UnsignedShort:
            case GltfComponentType.UnsignedInt:
            case GltfComponentType.Float:
                return true;
            default:
                return false;
        }
    }

    internal bool IsKnownAccessorType(GltfAccessorType accessorType)
    {
        switch (accessorType)
        {
            case GltfAccessorType.Scalar:
            case GltfAccessorType.Vector2:
            case GltfAccessorType.Vector3:
            case GltfAccessorType.Vector4:
            case GltfAccessorType.Matrix2:
            case GltfAccessorType.Matrix3:
            case GltfAccessorType.Matrix4:
                return true;
            default:
                return false;
        }
    }

    internal bool IsKnownBufferViewTarget(GltfBufferViewTarget target)
    {
        switch (target)
        {
            case GltfBufferViewTarget.None:
            case GltfBufferViewTarget.ArrayBuffer:
            case GltfBufferViewTarget.ElementArrayBuffer:
                return true;
            default:
                return false;
        }
    }

    internal BufferViewByteRange CalculateBufferViewRange(GltfBufferView bufferView)
    {
        if (bufferView == null)
        {
            throw new System.ArgumentNullException("bufferView");
        }

        BufferViewByteRange result = new BufferViewByteRange();
        result.Buffer = bufferView.Buffer;
        if (bufferView.HasByteOffset)
        {
            result.ByteOffset = bufferView.ByteOffset;
        }

        result.ByteLength = bufferView.ByteLength;
        result.ByteEnd = (long)result.ByteOffset + result.ByteLength;
        result.HasByteStride = bufferView.HasByteStride;
        if (result.HasByteStride)
        {
            result.ByteStride = bufferView.ByteStride;
        }

        result.HasTarget = bufferView.HasTarget;
        if (result.HasTarget)
        {
            result.Target = bufferView.Target;
        }

        return result;
    }

    internal AccessorByteRange CalculateAccessorByteRange(
        GltfAccessor accessor,
        BufferViewByteRange? bufferView)
    {
        if (accessor == null)
        {
            throw new System.ArgumentNullException("accessor");
        }

        if (accessor.Count < 0)
        {
            throw new System.ArgumentOutOfRangeException("accessor");
        }

        AccessorByteRange result = new AccessorByteRange();
        result.ElementLayout = CalculateElementLayout(
            accessor.ComponentType,
            accessor.Type);
        result.BufferView = bufferView;
        result.Count = accessor.Count;
        if (accessor.HasByteOffset)
        {
            result.AccessorByteOffset = accessor.ByteOffset;
        }

        result.ElementByteStride = result.ElementLayout.ElementByteLength;
        if (bufferView != null && bufferView.HasByteStride)
        {
            result.ElementByteStride = bufferView.ByteStride;
            result.Interleaved = true;
        }

        result.TightlyPacked = result.ElementByteStride
            == result.ElementLayout.ElementByteLength;
        long bufferViewByteOffset = 0;
        if (bufferView != null)
        {
            bufferViewByteOffset = bufferView.ByteOffset;
        }

        result.FirstElementBufferByteOffset = bufferViewByteOffset
            + result.AccessorByteOffset;
        result.FinalElementBufferByteOffset = result.FirstElementBufferByteOffset;
        result.AccessorByteEnd = result.AccessorByteOffset;
        result.BufferByteEnd = result.FirstElementBufferByteOffset;
        if (result.Count > 0)
        {
            result.HasElements = true;
            long finalElementStep = (long)(result.Count - 1)
                * result.ElementByteStride;
            result.FinalElementBufferByteOffset += finalElementStep;
            result.AccessorByteLength = finalElementStep
                + result.ElementLayout.ElementByteLength;
            result.AccessorByteEnd = result.AccessorByteOffset
                + result.AccessorByteLength;
            result.BufferByteEnd = bufferViewByteOffset
                + result.AccessorByteEnd;
        }

        return result;
    }

    internal bool IsComponentOffsetAligned(AccessorElementLayout layout, int byteOffset)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        return byteOffset >= 0
            && byteOffset % layout.ComponentByteLength == 0;
    }

    internal bool IsElementOffsetAligned(AccessorElementLayout layout, long byteOffset)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        return byteOffset >= 0
            && byteOffset % layout.ElementByteAlignment == 0;
    }
}
