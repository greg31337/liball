using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace avif;

internal class FileTypeBox
{
    const int CompatibleBrandStorageBytes = 32;

    internal AvifResultCode Code;
    internal IsoBox? Box;
    internal uint MajorBrandValue;
    internal string MajorBrand;
    internal uint MinorVersion;
    internal List<uint> CompatibleBrandValues;
    internal List<string> CompatibleBrands;

    internal FileTypeBox()
    {
        Code = AvifResultCode.Success;
        Box = null;
        MajorBrandValue = 0;
        MajorBrand = string.Empty;
        MinorVersion = 0;
        CompatibleBrandValues = new List<uint>();
        CompatibleBrands = new List<string>();
    }

    internal bool HasBrand(string brand)
    {
        if (MajorBrand == brand)
        {
            return true;
        }

        int index = 0;
        while (index < CompatibleBrands.Count)
        {
            if (CompatibleBrands[index] == brand)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static FileTypeBox Parse(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources)
    {
        FileTypeBox result = new FileTypeBox();
        result.Box = box;
        if (box == null || box.Type != "ftyp" || box.IsUuid
            || resources == null)
        {
            result.Code = AvifResultCode.InvalidArgument;
            return result;
        }

        if (box.PayloadSize < 8 || (box.PayloadSize - 8) % 4 != 0)
        {
            result.Code = AvifResultCode.InvalidContainer;
            return result;
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            box.PayloadOffset,
            0,
            box.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            result.Code = range.Code;
            return result;
        }

        long compatibleBrandCount = (box.PayloadSize - 8) / 4;
        CheckedIntegerResult storage = CheckedMath.MultiplyInt64(
            compatibleBrandCount,
            CompatibleBrandStorageBytes);
        if (!storage.Success
            || resources.ReserveTemporaryBytes(storage.Value)
                != AvifResultCode.Success)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }

        int offset = (int)box.PayloadOffset;
        result.MajorBrandValue = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(offset, 4));
        result.MajorBrand = new FourCc(result.MajorBrandValue).Text;
        result.MinorVersion = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(offset + 4, 4));

        int compatibleOffset = offset + 8;
        int end = (int)box.EndOffset;
        while (compatibleOffset < end)
        {
            uint value = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(compatibleOffset, 4));
            result.CompatibleBrandValues.Add(value);
            result.CompatibleBrands.Add(new FourCc(value).Text);
            compatibleOffset += 4;
        }

        return result;
    }

    internal static AvifResultCode Write(
        IsoBoxWriter writer,
        string majorBrand,
        uint minorVersion,
        string[] compatibleBrands)
    {
        if (writer == null
            || !IsoBoxType.IsValid(majorBrand)
            || compatibleBrands == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        int index = 0;
        while (index < compatibleBrands.Length)
        {
            if (!IsoBoxType.IsValid(compatibleBrands[index]))
            {
                return AvifResultCode.InvalidArgument;
            }

            index++;
        }

        CheckedIntegerResult brandBytes = CheckedMath.MultiplyInt32(compatibleBrands.Length, 4);
        if (!brandBytes.Success)
        {
            return brandBytes.Code;
        }

        CheckedIntegerResult payloadSize = CheckedMath.AddInt32((int)brandBytes.Value, 8);
        if (!payloadSize.Success)
        {
            return payloadSize.Code;
        }

        AvifResultCode state = writer.CheckCanAppend();
        if (state != AvifResultCode.Success)
        {
            return state;
        }

        long remainingOutput = writer.Output.MaxBytes - writer.Output.Length;
        if (remainingOutput < 8
            || payloadSize.Value > remainingOutput - 8
            || payloadSize.Value > System.Array.MaxLength)
        {
            return writer.SetError(AvifResultCode.LimitExceeded);
        }

        byte[] payload = new byte[(int)payloadSize.Value];
        ByteWriter payloadWriter = new ByteWriter(payload.Length);
        payloadWriter.WriteFourCc(payload, majorBrand);
        payloadWriter.WriteUInt32BigEndian(payload, minorVersion);
        index = 0;
        while (index < compatibleBrands.Length)
        {
            payloadWriter.WriteFourCc(payload, compatibleBrands[index]);
            index++;
        }

        if (payloadWriter.Code != AvifResultCode.Success)
        {
            return payloadWriter.Code;
        }

        AvifResultCode beginCode = writer.BeginBox("ftyp");
        if (beginCode != AvifResultCode.Success)
        {
            return beginCode;
        }

        AvifResultCode writeCode = writer.Write(payload);
        if (writeCode != AvifResultCode.Success)
        {
            return writeCode;
        }

        return writer.EndBox();
    }
}

internal class FileTypeParseResult
{
    internal AvifResultCode Code;
    internal FileTypeBox? FileType;
    internal IsoBoxParseResult Boxes;

    internal FileTypeParseResult(
        AvifResultCode code,
        FileTypeBox? fileType,
        IsoBoxParseResult boxes)
    {
        Code = code;
        FileType = fileType;
        Boxes = boxes;
    }
}

internal static class FileTypeParser
{
    internal static FileTypeParseResult Parse(
        ReadOnlySpan<byte> source,
        AvifSecurityLimits limits)
    {
        IsoBoxParseResult boxes = IsoBoxReader.Parse(source, limits, false);
        if (boxes.Code != AvifResultCode.Success)
        {
            return new FileTypeParseResult(boxes.Code, null, boxes);
        }

        if (boxes.Boxes.Count == 0 || boxes.Boxes[0].Type != "ftyp")
        {
            return new FileTypeParseResult(AvifResultCode.NotFound, null, boxes);
        }

        FileTypeBox fileType = FileTypeBox.Parse(
            source,
            boxes.Boxes[0],
            boxes.Resources);
        return new FileTypeParseResult(fileType.Code, fileType, boxes);
    }
}
