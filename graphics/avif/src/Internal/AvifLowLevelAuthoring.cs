using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace avif;

internal class AvifLowLevelItemResult
{
    internal AvifResultCode Code;
    internal CanonicalItemPayload? Item;
    internal int Width;
    internal int Height;
    internal int BitDepth;

    internal AvifLowLevelItemResult(AvifResultCode code)
    {
        Code = code;
    }
}

internal static class AvifLowLevelAuthoring
{
    internal static AvifLowLevelItemResult CreateAv1Item(
        uint itemId,
        byte[] payload,
        Av1CodecConfiguration configuration,
        int width,
        int height,
        bool hidden,
        AvifSecurityLimits limits)
    {
        AvifLowLevelItemResult result = new AvifLowLevelItemResult(
            AvifResultCode.InvalidArgument);
        if (itemId == 0 || payload == null || payload.Length == 0
            || configuration == null || limits == null
            || ResourceGuard.ValidateLimits(limits) != AvifResultCode.Success
            || payload.LongLength > limits.MaxSourceBytes)
        {
            return result;
        }
        ResourceGuard resources = new ResourceGuard(limits);
        AvifResultCode dimensions = resources.CheckImageDimensions(
            width,
            height);
        if (dimensions != AvifResultCode.Success)
        {
            result.Code = dimensions;
            return result;
        }
        Av1ObuParseResult obus = Av1ObuParser.ParseLowOverhead(
            payload,
            limits,
            0);
        if (obus.Code != AvifResultCode.Success)
        {
            result.Code = obus.Code;
            return result;
        }
        Av1SequenceHeaderParseResult sequence =
            Av1SequenceHeaderParser.ParseObuStream(
                payload,
                false,
                true);
        if (sequence.Code != AvifResultCode.Success
            || sequence.Header == null)
        {
            result.Code = sequence.Code;
            return result;
        }
        ParsedAv1SequenceHeader header = sequence.Header;
        if (header.GetMaximumWidth() != (uint)width
            || header.GetMaximumHeight() != (uint)height)
        {
            result.Code = AvifResultCode.NonConformingData;
            return result;
        }
        Av1CodecConfigurationCodec codec =
            new Av1CodecConfigurationCodec();
        AvifEncodeResult encodedConfiguration =
            codec.GenerateConfigurationRecord(configuration);
        if (encodedConfiguration.GetCode() != AvifResultCode.Success)
        {
            result.Code = encodedConfiguration.GetCode();
            return result;
        }
        byte[] configurationData = encodedConfiguration.GetData();
        Av1ItemConfigurationParseResult parsedConfiguration =
            Av1ItemConfigurationBox.ParsePayload(configurationData);
        if (parsedConfiguration.Code != AvifResultCode.Success
            || parsedConfiguration.Configuration == null)
        {
            result.Code = parsedConfiguration.Code;
            return result;
        }
        result.Code = parsedConfiguration.Configuration
            .ReconcileSequenceHeader(payload);
        if (result.Code != AvifResultCode.Success)
        {
            return result;
        }

        CanonicalItemPayload item = CanonicalItemPayload.Create(
            itemId,
            "av01",
            string.Empty,
            hidden,
            payload);
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildBox("av1C", configurationData, false),
            true));
        byte[] extents = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(
            extents.AsSpan(0, 4),
            (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(
            extents.AsSpan(4, 4),
            (uint)height);
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildBox("ispe", extents, true),
            false));
        int channelCount = header.IsMonochrome() ? 1 : 3;
        byte[] pixels = new byte[1 + channelCount];
        pixels[0] = (byte)channelCount;
        int channel = 0;
        while (channel < channelCount)
        {
            pixels[channel + 1] = (byte)header.GetBitDepth();
            channel++;
        }
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildBox("pixi", pixels, true),
            false));
        result.Code = AvifResultCode.Success;
        result.Item = item;
        result.Width = width;
        result.Height = height;
        result.BitDepth = header.GetBitDepth();
        return result;
    }

    internal static bool IsValidGenericItem(
        string itemType,
        string name,
        string contentType,
        string contentEncoding,
        string uriType,
        byte[] payload)
    {
        return IsoBoxType.IsValid(itemType)
            && itemType != "uuid" && itemType != "av01"
            && name != null && name.IndexOf('\0') < 0
            && contentType != null && contentType.IndexOf('\0') < 0
            && contentEncoding != null
            && contentEncoding.IndexOf('\0') < 0
            && uriType != null && uriType.IndexOf('\0') < 0
            && payload != null
            && (itemType == "mime" && contentType.Length != 0
                || itemType != "mime" && contentType.Length == 0
                    && contentEncoding.Length == 0)
            && (itemType == "uri " && uriType.Length != 0
                || itemType != "uri " && uriType.Length == 0);
    }

    internal static bool IsValidReference(
        string type,
        uint fromItemId,
        uint[] targetItemIds)
    {
        if (!IsoBoxType.IsValid(type) || type == "uuid"
            || type == "free" || type == "skip"
            || fromItemId == 0 || targetItemIds == null
            || targetItemIds.Length == 0
            || targetItemIds.Length > ushort.MaxValue)
        {
            return false;
        }
        int index = 0;
        while (index < targetItemIds.Length)
        {
            if (targetItemIds[index] == 0
                || targetItemIds[index] == fromItemId)
            {
                return false;
            }
            int previous = 0;
            while (previous < index)
            {
                if (targetItemIds[previous] == targetItemIds[index])
                {
                    return false;
                }
                previous++;
            }
            index++;
        }
        return true;
    }

    internal static bool IsValidGroup(
        string type,
        uint[] entityIds)
    {
        if (!IsoBoxType.IsValid(type) || type == "uuid"
            || type == "free" || type == "skip"
            || entityIds == null || entityIds.Length == 0
            || type == "ster" && entityIds.Length != 2)
        {
            return false;
        }
        int index = 0;
        while (index < entityIds.Length)
        {
            if (entityIds[index] == 0)
            {
                return false;
            }
            int previous = 0;
            while (previous < index)
            {
                if (entityIds[previous] == entityIds[index])
                {
                    return false;
                }
                previous++;
            }
            index++;
        }
        return true;
    }

    internal static bool IsSafeTopLevelBox(
        AvifOpaqueBox box,
        AvifSecurityLimits limits)
    {
        if (box == null || box.GetCode() != AvifResultCode.Success
            || box.IsEssential())
        {
            return false;
        }
        byte[] raw = box.GetRawData();
        IsoBoxParseResult parsed = IsoBoxReader.Parse(raw, limits, true);
        return parsed.Code == AvifResultCode.Success
            && parsed.Boxes.Count == 1
            && AvifRemuxer.IsSafeOpaqueTopLevelBox(parsed.Boxes[0]);
    }

    internal static byte[]? AppendTopLevelBoxes(
        byte[] source,
        List<byte[]> boxes,
        long maximumBytes)
    {
        long total = source.LongLength;
        int index = 0;
        while (index < boxes.Count)
        {
            if (total > maximumBytes - boxes[index].LongLength
                || total > System.Array.MaxLength - boxes[index].LongLength)
            {
                return null;
            }
            total += boxes[index].LongLength;
            index++;
        }
        byte[] result = new byte[(int)total];
        source.CopyTo(result, 0);
        int offset = source.Length;
        index = 0;
        while (index < boxes.Count)
        {
            boxes[index].CopyTo(result, offset);
            offset += boxes[index].Length;
            index++;
        }
        return result;
    }

    static byte[] BuildBox(
        string type,
        byte[] payload,
        bool fullBox)
    {
        int headerSize = fullBox ? 12 : 8;
        byte[] raw = new byte[headerSize + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(raw, (uint)raw.Length);
        raw[4] = (byte)type[0];
        raw[5] = (byte)type[1];
        raw[6] = (byte)type[2];
        raw[7] = (byte)type[3];
        payload.CopyTo(raw, headerSize);
        return raw;
    }
}
