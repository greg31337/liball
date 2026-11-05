using System;

namespace tiff;

internal class TiffHighLevelMetadataEncoder
{
    internal static TiffTagValue CreateXmpValue(TiffXmpPacket packet, TiffDiagnostics diagnostics)
    {
        if (packet == null || !packet.HasXmp)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "XMP writing requires a populated XMP packet.", TiffDiagnostic.NoImageIndex, TiffTagId.Xmp);
            return new TiffTagValue();
        }

        if (packet.Bytes != null && packet.Bytes.Length > 0)
        {
            TiffDataType dataType = packet.DataType;
            if (dataType != TiffDataType.Byte && dataType != TiffDataType.Undefined)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "XMP packet data must use BYTE or UNDEFINED storage.", TiffDiagnostic.NoImageIndex, TiffTagId.Xmp);
                return new TiffTagValue();
            }

            return TiffTagValue.CreateRaw(TiffTagId.Xmp, dataType, packet.Bytes.Length, packet.Bytes);
        }

        if (packet.Text == null || packet.Text.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "XMP writing requires packet bytes or text.", TiffDiagnostic.NoImageIndex, TiffTagId.Xmp);
            return new TiffTagValue();
        }

        return TiffXmpMetadataWriter.CreateValue(packet.Text);
    }

    internal static TiffTagValue CreateIccValue(TiffIccProfile profile, TiffDiagnostics diagnostics)
    {
        if (profile == null || !profile.HasProfile || profile.Bytes == null || profile.Bytes.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ICC writing requires non-empty profile bytes.", TiffDiagnostic.NoImageIndex, TiffTagId.IccProfile);
            return new TiffTagValue();
        }

        return TiffIccProfileWriter.CreateValue(profile.Bytes);
    }

    internal static TiffTagValue CreatePhotoshopValue(TiffPhotoshopMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null || !metadata.HasPhotoshop)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Photoshop writing requires populated image-resource metadata.", TiffDiagnostic.NoImageIndex, TiffTagId.Photoshop);
            return new TiffTagValue();
        }

        byte[] bytes = metadata.RawBytes;
        if (bytes == null || bytes.Length == 0)
        {
            bytes = EncodePhotoshopBlocks(metadata.Blocks, diagnostics);
        }
        else
        {
            TiffPhotoshopMetadataReader.ParseBlocks(bytes, diagnostics);
        }

        if (diagnostics.HasErrors() || bytes.Length == 0)
        {
            if (!diagnostics.HasErrors())
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Photoshop writing requires at least one image-resource block.", TiffDiagnostic.NoImageIndex, TiffTagId.Photoshop);
            }

            return new TiffTagValue();
        }

        return TiffTagValue.CreateRaw(TiffTagId.Photoshop, TiffDataType.Byte, bytes.Length, bytes);
    }

    internal static TiffTagValue CreateIptcValue(TiffIptcMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null || !metadata.HasIptc)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "IPTC writing requires populated metadata.", TiffDiagnostic.NoImageIndex, TiffTagId.Iptc);
            return new TiffTagValue();
        }

        byte[] bytes = metadata.RawBytes;
        if (bytes == null || bytes.Length == 0)
        {
            bytes = EncodeIptcBlocks(metadata.Blocks, diagnostics);
        }
        else
        {
            TiffIptcMetadataReader.ParseBlocks(bytes, diagnostics);
        }

        if (diagnostics.HasErrors() || bytes.Length == 0)
        {
            if (!diagnostics.HasErrors())
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "IPTC writing requires at least one dataset.", TiffDiagnostic.NoImageIndex, TiffTagId.Iptc);
            }

            return new TiffTagValue();
        }

        return TiffTagValue.CreateRaw(TiffTagId.Iptc, TiffDataType.Undefined, bytes.Length, bytes);
    }

    internal static byte[] EncodePhotoshopBlocks(TiffPhotoshopImageResourceBlock[] blocks, TiffDiagnostics diagnostics)
    {
        long byteCount = 0;
        int index = 0;
        while (blocks != null && index < blocks.Length)
        {
            TiffPhotoshopImageResourceBlock block = blocks[index];
            if (!ValidatePhotoshopBlock(block, diagnostics))
            {
                return new byte[0];
            }

            int nameLength = block.Name.Length;
            int nameByteCount = 1 + nameLength;
            if (nameByteCount % 2 != 0)
            {
                nameByteCount = nameByteCount + 1;
            }

            int dataLength = block.Data.Length;
            int dataByteCount = dataLength;
            if (dataByteCount % 2 != 0)
            {
                dataByteCount = dataByteCount + 1;
            }

            byteCount = byteCount + 6L + nameByteCount + 4L + dataByteCount;
            if (byteCount > int.MaxValue)
            {
                diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Photoshop image resources are too large to allocate.");
                return new byte[0];
            }

            index = index + 1;
        }

        byte[] bytes = new byte[(int)byteCount];
        int offset = 0;
        index = 0;
        while (blocks != null && index < blocks.Length)
        {
            TiffPhotoshopImageResourceBlock block = blocks[index];
            WriteAscii(bytes, offset, block.Signature);
            TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 4, 2), block.ResourceId, TiffByteOrder.BigEndian);
            int nameLength = block.Name.Length;
            bytes[offset + 6] = (byte)nameLength;
            WriteAscii(bytes, offset + 7, block.Name);
            int dataLengthOffset = offset + 7 + nameLength;
            if ((1 + nameLength) % 2 != 0)
            {
                dataLengthOffset = dataLengthOffset + 1;
            }

            TiffBinaryWriter.WriteUInt32(bytes.AsSpan(dataLengthOffset, 4), (uint)block.Data.Length, TiffByteOrder.BigEndian);
            int dataOffset = dataLengthOffset + 4;
            block.Data.AsSpan().CopyTo(bytes.AsSpan(dataOffset, block.Data.Length));
            offset = dataOffset + block.Data.Length;
            if (block.Data.Length % 2 != 0)
            {
                offset = offset + 1;
            }

            index = index + 1;
        }

        return bytes;
    }

    internal static bool ValidatePhotoshopBlock(TiffPhotoshopImageResourceBlock block, TiffDiagnostics diagnostics)
    {
        if (block == null || (block.Signature != "8BIM" && block.Signature != "8B64"))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Photoshop image resources require an 8BIM or 8B64 signature.", TiffDiagnostic.NoImageIndex, TiffTagId.Photoshop);
            return false;
        }

        if (block.Name == null || block.Name.Length > 255 || !IsAscii(block.Name))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Photoshop image-resource names must contain at most 255 ASCII characters.", TiffDiagnostic.NoImageIndex, TiffTagId.Photoshop);
            return false;
        }

        if (block.Data == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Photoshop image-resource data is required.", TiffDiagnostic.NoImageIndex, TiffTagId.Photoshop);
            return false;
        }

        return true;
    }

    internal static byte[] EncodeIptcBlocks(TiffIptcBlock[] blocks, TiffDiagnostics diagnostics)
    {
        long byteCount = 0;
        int index = 0;
        while (blocks != null && index < blocks.Length)
        {
            if (blocks[index] == null || blocks[index].Data == null || blocks[index].Data.Length > ushort.MaxValue)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "IPTC datasets require data no longer than 65535 bytes.", TiffDiagnostic.NoImageIndex, TiffTagId.Iptc);
                return new byte[0];
            }

            byteCount = byteCount + 5L + blocks[index].Data.Length;
            if (byteCount > int.MaxValue)
            {
                diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "IPTC metadata is too large to allocate.");
                return new byte[0];
            }

            index = index + 1;
        }

        byte[] bytes = new byte[(int)byteCount];
        int offset = 0;
        index = 0;
        while (blocks != null && index < blocks.Length)
        {
            TiffIptcBlock block = blocks[index];
            bytes[offset] = 0x1c;
            bytes[offset + 1] = block.RecordNumber;
            bytes[offset + 2] = block.DataSetNumber;
            TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 3, 2), (ushort)block.Data.Length, TiffByteOrder.BigEndian);
            block.Data.AsSpan().CopyTo(bytes.AsSpan(offset + 5, block.Data.Length));
            offset = offset + 5 + block.Data.Length;
            index = index + 1;
        }

        return bytes;
    }

    internal static bool IsAscii(string value)
    {
        int index = 0;
        while (value != null && index < value.Length)
        {
            if (value[index] > 127)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void WriteAscii(byte[] destination, int offset, string value)
    {
        int index = 0;
        while (value != null && index < value.Length)
        {
            destination[offset + index] = (byte)value[index];
            index = index + 1;
        }
    }
}
