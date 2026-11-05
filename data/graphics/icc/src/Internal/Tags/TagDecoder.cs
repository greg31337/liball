namespace icc;

internal static class TagDecoder
{
    internal static IccTagData GetDecodedData(IccTag tag)
    {
        if (tag == null)
        {
            throw new IccProfileValidationException("The tag to decode cannot be null.");
        }

        TagDecodeState? state = tag.DecodeState;
        if (state == null)
        {
            if (tag.IsDecodeAttempted)
            {
                return tag.Data;
            }

            throw new IccProfileValidationException("The tag is not associated with an encoded payload that can be decoded.");
        }

        lock (state.SyncRoot)
        {
            if (!state.IsAttempted)
            {
                TagCodecDispatcher.DecodeTagPayload(state);
            }

            tag.Data = state.Data;
            tag.DecodeResult = state.Result;
            tag.IsDecodeAttempted = true;
            return state.Data;
        }
    }

}
