using System;

namespace tiff;

internal class TiffIptcMetadataReader
{
    internal static TiffIptcReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return Read(directory, fileBytes, new TiffLimits());
    }

    internal static TiffIptcReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read IPTC metadata.");
            return TiffIptcReadResult.Create(new TiffIptcMetadata(), diagnostics);
        }

        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.Iptc);
        if (entry == null)
        {
            return TiffIptcReadResult.Create(new TiffIptcMetadata(), diagnostics);
        }

        if (entry.DataType != TiffDataType.Undefined && entry.DataType != TiffDataType.Byte)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "IPTC metadata tag must use UNDEFINED or BYTE data.", directory.ImageIndex, TiffTagId.Iptc);
            return TiffIptcReadResult.Create(new TiffIptcMetadata(), diagnostics);
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return TiffIptcReadResult.Create(new TiffIptcMetadata(), diagnostics);
        }

        TiffIptcBlock[] blocks = ParseBlocks(loadResult.Bytes, diagnostics);
        TiffIptcMetadata metadata = TiffIptcMetadata.Create(true, loadResult.Bytes, blocks);
        return TiffIptcReadResult.Create(metadata, diagnostics);
    }

    internal static TiffIptcBlock[] ParseBlocks(byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffIptcBlock[] blocks = new TiffIptcBlock[0];
        if (bytes == null || bytes.Length == 0)
        {
            return blocks;
        }

        int offset = 0;
        while (offset < bytes.Length)
        {
            if (offset > bytes.Length - 5)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "IPTC dataset is truncated.");
                return new TiffIptcBlock[0];
            }

            if (bytes[offset] != 0x1c)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "IPTC dataset must start with the 0x1C marker.");
                return new TiffIptcBlock[0];
            }

            byte recordNumber = bytes[offset + 1];
            byte dataSetNumber = bytes[offset + 2];
            ushort dataLength = TiffBinaryReader.ReadUInt16(bytes.AsSpan(offset + 3, 2), TiffByteOrder.BigEndian);
            int dataOffset = offset + 5;
            if (dataLength > bytes.Length - dataOffset)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "IPTC dataset data is truncated.");
                return new TiffIptcBlock[0];
            }

            byte[] data = TiffPhotoshopMetadataReader.CopyRange(bytes, dataOffset, dataLength);
            blocks = AppendBlock(blocks, TiffIptcBlock.Create(recordNumber, dataSetNumber, data));
            offset = dataOffset + dataLength;
        }

        return blocks;
    }

    internal static TiffIptcBlock[] AppendBlock(TiffIptcBlock[] blocks, TiffIptcBlock block)
    {
        TiffIptcBlock[] copy = new TiffIptcBlock[blocks.Length + 1];
        int index = 0;
        while (index < blocks.Length)
        {
            copy[index] = blocks[index].Copy();
            index = index + 1;
        }

        copy[copy.Length - 1] = block.Copy();
        return copy;
    }
}
