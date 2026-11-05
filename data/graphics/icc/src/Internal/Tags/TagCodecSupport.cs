namespace icc;

internal static class TagCodecSupport
{
    internal static bool HasZeroReservedBytes(System.ReadOnlyMemory<byte> payload)
    {
        System.ReadOnlySpan<byte> bytes = payload.Span;
        return bytes[4] == 0 && bytes[5] == 0 && bytes[6] == 0 && bytes[7] == 0;
    }

    internal static IccTagData CreateTypedData(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, IccTagDataKind kind)
    {
        IccTagData data = new IccTagData();
        data.Kind = kind;
        data.RawData.TypeSignature = typeSignature;
        data.RawData.HasTypeSignature = true;
        if (state.PreserveOriginalBytes)
        {
            data.RawData.Bytes = payload.ToArray();
        }

        return data;
    }

    internal static void CompleteSuccessfulDecode(TagDecodeState state, IccTagData data, string message)
    {
        IccOperationResult result = UnknownTagCodec.CreateDecodeResult(state, IccOperationStatus.Success);
        result.Message = message;
        UnknownTagCodec.CompleteDecodeState(state, data, result);
    }

    internal static void CompleteInvalidDecode(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, string message)
    {
        IccTagData data = new IccTagData();
        if (state.PreserveOriginalBytes)
        {
            UnknownTagCodec.PopulateRawFallback(data, payload, typeSignature, true);
        }

        IccOperationResult result = UnknownTagCodec.CreateDecodeResult(state, IccOperationStatus.InvalidData);
        result.Message = message;
        UnknownTagCodec.CompleteDecodeState(state, data, result);
    }

    internal static bool IsSevenBitAscii(System.ReadOnlyMemory<byte> bytes)
    {
        System.ReadOnlySpan<byte> values = bytes.Span;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] > 127)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string DecodeSevenBitAscii(System.ReadOnlyMemory<byte> bytes, int count)
    {
        System.ReadOnlySpan<byte> values = bytes.Span;
        char[] characters = new char[count];
        int index = 0;
        while (index < count)
        {
            characters[index] = (char)values[index];
            index = index + 1;
        }

        return new string(characters);
    }
}
