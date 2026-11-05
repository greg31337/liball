using System;

namespace tiff;

internal class TiffPhotoshopMetadataReader
{
    internal static TiffPhotoshopReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return Read(directory, fileBytes, new TiffLimits());
    }

    internal static TiffPhotoshopReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read Photoshop metadata.");
            return TiffPhotoshopReadResult.Create(new TiffPhotoshopMetadata(), diagnostics);
        }

        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.Photoshop);
        if (entry == null)
        {
            return TiffPhotoshopReadResult.Create(new TiffPhotoshopMetadata(), diagnostics);
        }

        if (entry.DataType != TiffDataType.Byte && entry.DataType != TiffDataType.Undefined)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Photoshop metadata tag must use BYTE or UNDEFINED data.", directory.ImageIndex, TiffTagId.Photoshop);
            return TiffPhotoshopReadResult.Create(new TiffPhotoshopMetadata(), diagnostics);
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return TiffPhotoshopReadResult.Create(new TiffPhotoshopMetadata(), diagnostics);
        }

        TiffPhotoshopImageResourceBlock[] blocks = ParseBlocks(loadResult.Bytes, diagnostics);
        TiffPhotoshopMetadata metadata = TiffPhotoshopMetadata.Create(true, loadResult.Bytes, blocks);
        return TiffPhotoshopReadResult.Create(metadata, diagnostics);
    }

    internal static TiffPhotoshopImageResourceBlock[] ParseBlocks(byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffPhotoshopImageResourceBlock[] blocks = new TiffPhotoshopImageResourceBlock[0];
        if (bytes == null || bytes.Length == 0)
        {
            return blocks;
        }

        int offset = 0;
        while (offset < bytes.Length)
        {
            TiffPhotoshopImageResourceBlock block = ParseOneBlock(bytes, offset, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new TiffPhotoshopImageResourceBlock[0];
            }

            blocks = AppendBlock(blocks, block);
            offset = CalculateNextBlockOffset(bytes, offset, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new TiffPhotoshopImageResourceBlock[0];
            }
        }

        return blocks;
    }

    internal static TiffPhotoshopImageResourceBlock ParseOneBlock(byte[] bytes, int offset, TiffDiagnostics diagnostics)
    {
        if (offset < 0 || bytes == null || offset > bytes.Length - 12)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Photoshop image resource block is truncated.");
            return new TiffPhotoshopImageResourceBlock();
        }

        string signature = DecodeAscii(bytes, offset, 4);
        if (signature != "8BIM" && signature != "8B64")
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Photoshop image resource block has an unsupported signature.");
            return new TiffPhotoshopImageResourceBlock();
        }

        ushort resourceId = TiffBinaryReader.ReadUInt16(bytes.AsSpan(offset + 4, 2), TiffByteOrder.BigEndian);
        int nameLength = bytes[offset + 6];
        int nameStart = offset + 7;
        if (nameStart > bytes.Length || nameLength > bytes.Length - nameStart)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Photoshop image resource block name is truncated.");
            return new TiffPhotoshopImageResourceBlock();
        }

        string name = DecodeAscii(bytes, nameStart, nameLength);
        int dataLengthOffset = nameStart + nameLength;
        if (((1 + nameLength) % 2) != 0)
        {
            dataLengthOffset = dataLengthOffset + 1;
        }

        if (dataLengthOffset > bytes.Length - 4)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Photoshop image resource block data length is truncated.");
            return new TiffPhotoshopImageResourceBlock();
        }

        uint dataLength = TiffBinaryReader.ReadUInt32(bytes.AsSpan(dataLengthOffset, 4), TiffByteOrder.BigEndian);
        int dataOffset = dataLengthOffset + 4;
        if (dataLength > int.MaxValue || dataOffset > bytes.Length || (int)dataLength > bytes.Length - dataOffset)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Photoshop image resource block data is truncated.");
            return new TiffPhotoshopImageResourceBlock();
        }

        byte[] data = CopyRange(bytes, dataOffset, (int)dataLength);
        return TiffPhotoshopImageResourceBlock.Create(signature, resourceId, name, data);
    }

    internal static int CalculateNextBlockOffset(byte[] bytes, int offset, TiffDiagnostics diagnostics)
    {
        if (offset < 0 || bytes == null || offset > bytes.Length - 12)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Photoshop image resource block is truncated.");
            return bytes == null ? 0 : bytes.Length;
        }

        int nameLength = bytes[offset + 6];
        int dataLengthOffset = offset + 7 + nameLength;
        if (((1 + nameLength) % 2) != 0)
        {
            dataLengthOffset = dataLengthOffset + 1;
        }

        if (dataLengthOffset > bytes.Length - 4)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Photoshop image resource block data length is truncated.");
            return bytes.Length;
        }

        uint dataLength = TiffBinaryReader.ReadUInt32(bytes.AsSpan(dataLengthOffset, 4), TiffByteOrder.BigEndian);
        if (dataLength > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Photoshop image resource block data length exceeds the supported range.");
            return bytes.Length;
        }

        int next = dataLengthOffset + 4 + (int)dataLength;
        if ((dataLength % 2) != 0)
        {
            next = next + 1;
        }

        if (next > bytes.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Photoshop image resource block extends past the metadata bytes.");
            return bytes.Length;
        }

        return next;
    }

    internal static TiffPhotoshopImageResourceBlock[] AppendBlock(TiffPhotoshopImageResourceBlock[] blocks, TiffPhotoshopImageResourceBlock block)
    {
        TiffPhotoshopImageResourceBlock[] copy = new TiffPhotoshopImageResourceBlock[blocks.Length + 1];
        int index = 0;
        while (index < blocks.Length)
        {
            copy[index] = blocks[index].Copy();
            index = index + 1;
        }

        copy[copy.Length - 1] = block.Copy();
        return copy;
    }

    internal static string DecodeAscii(byte[] bytes, int offset, int length)
    {
        char[] chars = new char[length];
        int index = 0;
        while (index < length)
        {
            byte value = bytes[offset + index];
            if (value > 127)
            {
                chars[index] = '?';
            }
            else
            {
                chars[index] = (char)value;
            }

            index = index + 1;
        }

        return new string(chars);
    }

    internal static byte[] CopyRange(byte[] bytes, int offset, int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = bytes[offset + index];
            index = index + 1;
        }

        return result;
    }
}
