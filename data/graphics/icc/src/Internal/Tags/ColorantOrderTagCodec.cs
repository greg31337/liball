namespace icc;

internal static class ColorantOrderTagCodec
{
    internal static void DecodeColorantOrder(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A colorantOrderType payload requires at least 12 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The colorantOrderType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader headerReader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
        uint encodedCount = headerReader.ReadUInt32();
        if (encodedCount < 1 || encodedCount > IccConstants.MaximumIcc1ChannelCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The colorantOrderType channel count {encodedCount} shall be between 1 and {IccConstants.MaximumIcc1ChannelCount}.");
            return;
        }

        int channelCount = (int)encodedCount;
        int expectedSize = 12 + channelCount;
        if (payload.Length != expectedSize)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The colorantOrderType channel count {channelCount} requires exactly {expectedSize} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        string contextError = ValidateProfileContext(state, channelCount);
        if (contextError.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, contextError);
            return;
        }

        long[] order = new long[channelCount];
        bool[] used = new bool[channelCount];
        System.ReadOnlySpan<byte> values = payload.Span.Slice(12);
        int index = 0;
        while (index < channelCount)
        {
            int value = values[index];
            if (value >= channelCount)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Colorant-order index {value} at position {index} is outside the valid range zero through {channelCount - 1}.");
                return;
            }

            if (used[value])
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Colorant-order index {value} occurs more than once; the order shall be a permutation of all channels.");
                return;
            }

            used[value] = true;
            order[index] = value;
            index = index + 1;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.IntegerArray);
        data.IntegerValues = order;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded colorantOrderType data for {channelCount} channels.");
    }

    internal static byte[] SerializeColorantOrder(long[] order)
    {
        if (order == null || order.Length < 1 || order.Length > IccConstants.MaximumIcc1ChannelCount)
        {
            throw new IccProfileWriteException($"A colorant order shall contain between 1 and {IccConstants.MaximumIcc1ChannelCount} channel indices.");
        }

        bool[] used = new bool[order.Length];
        byte[] payload = new byte[12 + order.Length];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.ColorantOrder);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)order.Length);
        int index = 0;
        while (index < order.Length)
        {
            long value = order[index];
            if (value < 0 || value >= order.Length)
            {
                throw new IccProfileWriteException($"Colorant-order index {value} at position {index} is outside the valid range zero through {order.Length - 1}.");
            }

            int channel = (int)value;
            if (used[channel])
            {
                throw new IccProfileWriteException($"Colorant-order index {value} occurs more than once; the order shall be a permutation of all channels.");
            }

            used[channel] = true;
            writer.WriteUInt8((byte)channel);
            index = index + 1;
        }

        return payload;
    }

    internal static string ValidateProfileContext(TagDecodeState state, int channelCount)
    {
        if (!state.HasProfileContext)
        {
            return string.Empty;
        }

        uint colorSpaceSignature = state.DataColorSpaceSignature;
        if (state.TagSignature == IccTagSignatures.ColorantOrderOut)
        {
            if (state.ProfileClass != IccProfileClass.DeviceLink)
            {
                return "The colorantOrderOutTag is permitted only in a DeviceLink profile.";
            }

            colorSpaceSignature = state.ProfileConnectionSpaceSignature;
        }

        int expectedCount = ColorSpaceChannelCount.Decode(colorSpaceSignature);
        if (expectedCount == 0)
        {
            return $"The associated colour-space signature 0x{colorSpaceSignature:X8} does not define a 1-15 channel count for colorantOrderType.";
        }

        if (channelCount != expectedCount)
        {
            return $"The colorantOrderType channel count {channelCount} does not match the associated colour space's {expectedCount} channels.";
        }

        return string.Empty;
    }
}
