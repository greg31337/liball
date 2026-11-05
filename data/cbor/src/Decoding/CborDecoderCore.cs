using System;

namespace cbor;

internal class CborDecoderCore
{
    internal CborTokenizerCore? Tokenizer;
    internal CborDomBuilder Builder = new CborDomBuilder();
    internal CborSharingDecoder? SharingDecoder;
    internal CborTagLookup? Tags;
    internal bool Active;

    internal CborDecoderCore()
    {
    }

    internal CborReadResult DecodeValue(
        byte[] source,
        CborDecodeOptions options,
        bool allowTrailingData)
    {
        CborTokenizerCore tokenizer = ResetOperation(options);
        int sourceOffset = 0;
        while (!Builder.Completed)
        {
            byte[] remainingSource = source.AsSpan(sourceOffset).ToArray();
            CborTokenReadResult tokenResult = tokenizer.ReadToken(remainingSource);
            if (tokenResult.Status == CborReadStatus.Error)
            {
                Active = false;
                return CreateTokenFailure(tokenResult, source.Length);
            }
            if (tokenResult.Status == CborReadStatus.NeedMoreData)
            {
                Active = false;
                return CreateFailure(
                    "CBOR_UNEXPECTED_END_OF_DATA",
                    "The encoded input ended before one complete CBOR value was available.",
                    source.Length,
                    source.Length
                );
            }
            if (tokenResult.Token == null)
            {
                Active = false;
                return CreateFailure(
                    "CBOR_DECODE_TOKEN_MISSING",
                    "The tokenizer completed without producing a token.",
                    sourceOffset,
                    source.Length
                );
            }
            if (!Builder.AddToken(tokenResult.Token))
            {
                Active = false;
                return CreateFailure(
                    Builder.ErrorCode ?? "CBOR_DOM_BUILD_FAILED",
                    Builder.ErrorMessage ?? "The decoded tokens could not form a CBOR value.",
                    tokenizer.AbsoluteOffset,
                    source.Length
                );
            }
            sourceOffset = sourceOffset + tokenResult.Consumed;
        }

        if (!allowTrailingData && sourceOffset < source.Length)
        {
            Active = false;
            return CreateFailure(
                "CBOR_TRAILING_DATA",
                "Bytes remain after the complete CBOR value.",
                sourceOffset,
                source.Length
            );
        }

        if (Builder.Value == null)
        {
            Active = false;
            return CreateFailure(
                "CBOR_DOM_VALUE_MISSING",
                "The DOM builder completed without producing a value.",
                sourceOffset,
                source.Length
            );
        }
        if (Tags == null)
        {
            Active = false;
            return CreateFailure(
                "CBOR_TAG_LOOKUP_MISSING",
                "The decoding operation has no tag-registration snapshot.",
                sourceOffset,
                source.Length
            );
        }
        CborDuplicateKeyProcessor duplicateKeys = new CborDuplicateKeyProcessor();
        if (!duplicateKeys.Process(Builder.Value, options.DuplicateKeyPolicy))
        {
            Active = false;
            return CreatePolicyFailure(
                duplicateKeys.ErrorCode ?? "CBOR_DUPLICATE_KEY_PROCESSING_FAILED",
                duplicateKeys.ErrorMessage ?? "The duplicate map-key policy could not be applied.",
                duplicateKeys.ErrorOffset,
                duplicateKeys.ErrorPath,
                source.Length
            );
        }
        CborSharingDecoder sharingDecoder = new CborSharingDecoder(options, Tags);
        SharingDecoder = sharingDecoder;
        if (!sharingDecoder.Process(Builder.Value))
        {
            Active = false;
            return CreateFailure(
                sharingDecoder.ErrorCode ?? "CBOR_REFERENCE_PROCESSING_FAILED",
                sharingDecoder.ErrorMessage ?? "The reference tags could not be processed.",
                sharingDecoder.ErrorOffset,
                source.Length
            );
        }

        Active = false;
        return CreateSuccess(sharingDecoder.Result, sourceOffset, source.Length - sourceOffset);
    }

    internal CborTokenizerCore ResetOperation(CborDecodeOptions options)
    {
        Builder.Reset();
        Tokenizer = new CborTokenizerCore(options, null);
        Tags = new CborTagLookup(options.TagRegistrations);
        SharingDecoder = null;
        Active = true;
        return Tokenizer;
    }

    internal static CborReadResult CreateSuccess(
        CborValue? value,
        int consumed,
        int remaining)
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Done;
        result.Value = value;
        result.Consumed = consumed;
        result.Remaining = remaining;
        return result;
    }

    internal static CborReadResult CreateTokenFailure(
        CborTokenReadResult tokenResult,
        int sourceLength)
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.Consumed = 0;
        result.Remaining = sourceLength;
        result.ErrorCode = tokenResult.ErrorCode;
        result.ErrorMessage = tokenResult.ErrorMessage;
        result.ErrorOffset = tokenResult.ErrorOffset;
        result.ErrorPath = tokenResult.ErrorPath;
        return result;
    }

    internal static CborReadResult CreateFailure(
        string errorCode,
        string errorMessage,
        long errorOffset,
        int sourceLength)
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.Consumed = 0;
        result.Remaining = sourceLength;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.ErrorOffset = errorOffset;
        result.ErrorPath = "$";
        return result;
    }

    internal static CborReadResult CreatePolicyFailure(
        string errorCode,
        string errorMessage,
        long errorOffset,
        string errorPath,
        int sourceLength)
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.Remaining = sourceLength;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.ErrorOffset = errorOffset;
        result.ErrorPath = errorPath;
        return result;
    }
}
