namespace icc;

internal static class UnknownTagCodec
{
    internal static void DecodeFallbackPayload(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, bool isSupported, string generationName)
    {
        IccTagData data = new IccTagData();
        PopulateRawFallback(data, payload, typeSignature, true);
        IccOperationResult result = CreateDecodeResult(state, IccOperationStatus.UnsupportedFeature);
        string formattedSignature = FourCcCodec.Format(typeSignature);
        if (isSupported)
        {
            result.Message = $"The {generationName} tag type '{formattedSignature}' (0x{typeSignature:X8}) is recognized but its typed codec is not available; the payload was retained losslessly as raw data.";
        }
        else
        {
            result.Message = $"The tag type '{formattedSignature}' (0x{typeSignature:X8}) is unknown or unsupported for {generationName}; the payload was retained losslessly as raw data.";
        }

        CompleteDecodeState(state, data, result);
    }

    internal static void DecodeMalformedPayload(TagDecodeState state, System.ReadOnlyMemory<byte> payload)
    {
        IccTagData data = new IccTagData();
        if (state.PreserveOriginalBytes)
        {
            uint typeSignature = 0;
            bool hasTypeSignature = payload.Length >= 4;
            if (hasTypeSignature)
            {
                BigEndianReader reader = new BigEndianReader(payload, state.AbsolutePayloadOffset);
                typeSignature = reader.ReadUInt32();
            }

            PopulateRawFallback(data, payload, typeSignature, hasTypeSignature);
        }

        IccOperationResult result = CreateDecodeResult(state, IccOperationStatus.InvalidData);
        result.Message = $"The tag payload at byte offset {state.AbsolutePayloadOffset} is {state.PayloadSize} bytes; a complete tag type header requires at least 8 bytes.";
        CompleteDecodeState(state, data, result);
    }

    internal static void PopulateRawFallback(IccTagData data, System.ReadOnlyMemory<byte> payload, uint typeSignature, bool hasTypeSignature)
    {
        data.Kind = IccTagDataKind.Raw;
        data.RawData.TypeSignature = typeSignature;
        data.RawData.HasTypeSignature = hasTypeSignature;
        data.RawData.Bytes = payload.ToArray();
    }

    internal static IccOperationResult CreateDecodeResult(TagDecodeState state, IccOperationStatus status)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Location.Offset = state.AbsolutePayloadOffset;
        result.Location.Length = state.PayloadSize;
        return result;
    }

    internal static void CompleteDecodeState(TagDecodeState state, IccTagData data, IccOperationResult result)
    {
        state.Data = data;
        state.Result = result;
        state.IsAttempted = true;
    }
}
