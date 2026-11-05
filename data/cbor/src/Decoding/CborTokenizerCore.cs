using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace cbor;

internal class CborTokenizerCore
{
    internal CborDecodeOptions Options;
    internal long AbsoluteOffset;
    internal List<CborTokenFrame> Frames = new List<CborTokenFrame>();
    internal byte[] PendingBytes = Array.Empty<byte>();
    internal CborSkipState SkipState = new CborSkipState();
    internal CborLimitState Limits;
    internal bool HoldsEmbeddedLimit;

    internal CborTokenizerCore(CborDecodeOptions options, CborLimitState? limits)
    {
        Options = options;
        if (limits == null)
        {
            Limits = new CborLimitState(options);
        }
        else
        {
            Limits = limits;
        }
    }

    internal CborTokenizerCore? CreateEmbeddedCore()
    {
        if (!Limits.AllowsEmbeddedEntry())
        {
            return null;
        }
        Limits.EnterEmbedded();
        CborTokenizerCore embeddedCore = new CborTokenizerCore(Options, Limits);
        embeddedCore.HoldsEmbeddedLimit = true;
        return embeddedCore;
    }

    internal void ExitEmbeddedCore()
    {
        if (HoldsEmbeddedLimit)
        {
            HoldsEmbeddedLimit = false;
            Limits.ExitEmbedded();
        }
    }

    internal CborTokenReadResult ReadToken(byte[] source)
    {
        if (PendingBytes.Length == 0)
        {
            CborTokenReadResult directResult = ReadAvailableToken(source);
            if (directResult.Status == CborReadStatus.NeedMoreData)
            {
                if (!Limits.AllowsPendingBytes(source.Length))
                {
                    return CreateErrorResult(
                        "CBOR_MAX_TOTAL_BYTES",
                        "The buffered token exceeds the configured encoded-byte limit.",
                        0,
                        source.Length
                    );
                }
                PendingBytes = source.AsSpan().ToArray();
                directResult.Consumed = source.Length;
                directResult.Remaining = 0;
            }
            return directResult;
        }
        if (PendingBytes.Length > int.MaxValue - source.Length)
        {
            return CreateErrorResult(
                "CBOR_BUFFER_LENGTH_OVERFLOW",
                "The buffered token bytes exceed the supported array length.",
                0,
                source.Length
            );
        }

        int pendingLength = PendingBytes.Length;
        int sourceBytesToCopy = Limits.LimitPendingSourceBytes(
            pendingLength,
            source.Length
        );
        if (sourceBytesToCopy < 0 || (sourceBytesToCopy == 0 && source.Length > 0))
        {
            CborTokenReadResult byteLimitFailure = CreateErrorResult(
                "CBOR_MAX_TOTAL_BYTES",
                "The buffered token exceeds the configured encoded-byte limit.",
                0,
                source.Length
            );
            byteLimitFailure.Remaining = source.Length;
            return byteLimitFailure;
        }
        byte[] available = new byte[pendingLength + sourceBytesToCopy];
        PendingBytes.AsSpan().CopyTo(available);
        source.AsSpan(0, sourceBytesToCopy).CopyTo(available.AsSpan(pendingLength));
        CborTokenReadResult bufferedResult = ReadAvailableToken(available);
        if (bufferedResult.Status == CborReadStatus.NeedMoreData)
        {
            if (sourceBytesToCopy < source.Length
                || !Limits.AllowsPendingBytes(available.Length))
            {
                bufferedResult = CreateErrorResult(
                    "CBOR_MAX_TOTAL_BYTES",
                    "The buffered token exceeds the configured encoded-byte limit.",
                    0,
                    source.Length
                );
                bufferedResult.Remaining = source.Length;
                return bufferedResult;
            }
            PendingBytes = available;
            bufferedResult.Consumed = source.Length;
            bufferedResult.Remaining = 0;
            return bufferedResult;
        }
        if (bufferedResult.Status == CborReadStatus.Done)
        {
            int currentConsumed = bufferedResult.Consumed - pendingLength;
            PendingBytes = Array.Empty<byte>();
            bufferedResult.Consumed = currentConsumed;
            bufferedResult.Remaining = source.Length - currentConsumed;
            return bufferedResult;
        }
        bufferedResult.Remaining = source.Length;
        return bufferedResult;
    }

    internal CborTokenReadResult ReadAvailableToken(byte[] source)
    {
        CborTokenReadResult? completion = CreateContainerCompletionResult(source.Length);
        if (completion != null)
        {
            return completion;
        }
        if (source.Length == 0)
        {
            return CreateNeedMoreResult(source.Length);
        }

        byte initialByte = source[0];
        int majorType = initialByte >> 5;
        int additionalInformation = initialByte & 31;
        if (additionalInformation == 31)
        {
            if (majorType == 7)
            {
                return ReadBreakToken(source.Length);
            }
            return ReadIndefiniteStartToken(majorType, source.Length);
        }
        if (additionalInformation >= 28 && additionalInformation <= 30)
        {
            return CreateErrorResult(
                "CBOR_RESERVED_ADDITIONAL_INFORMATION",
                "CBOR additional information values 28 through 30 are reserved.",
                0,
                source.Length
            );
        }
        CborTokenReadResult? chunkFailure = ValidateStringChunkType(majorType, source.Length);
        if (chunkFailure != null)
        {
            return chunkFailure;
        }
        int argumentWidth = GetArgumentWidth(additionalInformation);
        int headLength = argumentWidth + 1;
        if (source.Length < headLength)
        {
            return CreateNeedMoreResult(source.Length);
        }
        ulong argument = ReadArgumentValue(source, additionalInformation, argumentWidth);
        if (majorType == 7 && additionalInformation == 24 && argument < 32)
        {
            return CreateErrorResult(
                "CBOR_INVALID_SIMPLE_VALUE_ENCODING",
                "A two-byte simple value must have a value of at least 32.",
                1,
                source.Length
            );
        }
        CborTokenReadResult? capacityFailure = ValidateContainerCapacity(source.Length);
        if (capacityFailure != null)
        {
            return capacityFailure;
        }
        CborTokenReadResult? itemCapacityFailure = ValidateItemCapacity(source.Length);
        if (itemCapacityFailure != null)
        {
            return itemCapacityFailure;
        }

        CborToken token = new CborToken();
        int consumed = headLength;
        switch (majorType)
        {
            case 0:
                token.Kind = CborTokenKind.UnsignedInteger;
                token.IntegerValue = new BigInteger(argument);
                break;
            case 1:
                token.Kind = CborTokenKind.NegativeInteger;
                token.IntegerValue = new BigInteger(-1) - new BigInteger(argument);
                break;
            case 2:
                if (IsInsideIndefiniteString(CborTokenKind.StartByteString))
                {
                    token.Kind = CborTokenKind.ByteStringChunk;
                }
                else
                {
                    token.Kind = CborTokenKind.ByteString;
                }
                token.LengthKind = CborLengthKind.Definite;
                token.Length = argument;
                CborTokenReadResult? byteStringLimitFailure = ValidateStringLength(
                    argument,
                    source.Length
                );
                if (byteStringLimitFailure != null)
                {
                    return byteStringLimitFailure;
                }
                if (argument > (ulong)(int.MaxValue - headLength))
                {
                    return CreateErrorResult(
                        "CBOR_LENGTH_OUT_OF_RANGE",
                        "The declared byte string length cannot be represented by this API.",
                        0,
                        source.Length
                    );
                }
                consumed = headLength + (int)argument;
                CborTokenReadResult? byteStringByteFailure = ValidateEncodedLength(
                    consumed,
                    source.Length
                );
                if (byteStringByteFailure != null)
                {
                    return byteStringByteFailure;
                }
                if (source.Length < consumed)
                {
                    return CreateNeedMoreResult(source.Length);
                }
                token.ByteStringValue = source.AsSpan(headLength, (int)argument).ToArray();
                break;
            case 3:
                if (IsInsideIndefiniteString(CborTokenKind.StartTextString))
                {
                    token.Kind = CborTokenKind.TextStringChunk;
                }
                else
                {
                    token.Kind = CborTokenKind.TextString;
                }
                token.LengthKind = CborLengthKind.Definite;
                token.Length = argument;
                CborTokenReadResult? textStringLimitFailure = ValidateStringLength(
                    argument,
                    source.Length
                );
                if (textStringLimitFailure != null)
                {
                    return textStringLimitFailure;
                }
                if (argument > (ulong)(int.MaxValue - headLength))
                {
                    return CreateErrorResult(
                        "CBOR_LENGTH_OUT_OF_RANGE",
                        "The declared text string length cannot be represented by this API.",
                        0,
                        source.Length
                    );
                }
                consumed = headLength + (int)argument;
                CborTokenReadResult? textStringByteFailure = ValidateEncodedLength(
                    consumed,
                    source.Length
                );
                if (textStringByteFailure != null)
                {
                    return textStringByteFailure;
                }
                if (source.Length < consumed)
                {
                    return CreateNeedMoreResult(source.Length);
                }
                CborTokenReadResult? textFailure = DecodeTextToken(
                    token,
                    source,
                    headLength,
                    (int)argument
                );
                if (textFailure != null)
                {
                    return textFailure;
                }
                break;
            case 4:
                token.Kind = CborTokenKind.StartArray;
                token.LengthKind = CborLengthKind.Definite;
                token.Length = argument;
                break;
            case 5:
                token.Kind = CborTokenKind.StartMap;
                token.LengthKind = CborLengthKind.Definite;
                token.Length = argument;
                break;
            case 6:
                token.Kind = CborTokenKind.Tag;
                token.TagNumber = argument;
                break;
            case 7:
                CborTokenReadResult? simpleFailure = DecodeSimpleOrFloatToken(
                    token,
                    source,
                    additionalInformation,
                    argument
                );
                if (simpleFailure != null)
                {
                    return simpleFailure;
                }
                break;
            default:
                return CreateErrorResult(
                    "CBOR_INVALID_MAJOR_TYPE",
                    "The CBOR major type is invalid.",
                    0,
                    source.Length
                );
        }

        CborTokenReadResult? tokenLimitFailure = ValidateTokenLimits(
            token,
            consumed,
            source.Length
        );
        if (tokenLimitFailure != null)
        {
            return tokenLimitFailure;
        }
        token.Representation = CreateRepresentation(argumentWidth, consumed, token);
        CborTokenReadResult result = CreateDoneResult(token, consumed, source.Length);
        if (result.Status == CborReadStatus.Done)
        {
            CommitTokenLimits(token, consumed);
            TrackToken(token);
        }
        return result;
    }

    internal void Reset()
    {
        AbsoluteOffset = 0;
        Frames.Clear();
        PendingBytes = Array.Empty<byte>();
        ResetSkipState();
        Limits.Reset();
    }

    internal CborReadResult SkipValue(byte[] source)
    {
        if (!SkipState.Active)
        {
            SkipState.Active = true;
            SkipState.BaseFrameCount = Frames.Count;
        }

        int sourceOffset = 0;
        while (true)
        {
            byte[] available;
            if (sourceOffset == 0)
            {
                available = source;
            }
            else
            {
                available = source.AsSpan(sourceOffset).ToArray();
            }
            CborTokenReadResult tokenResult = ReadToken(available);
            sourceOffset = sourceOffset + tokenResult.Consumed;

            if (tokenResult.Status == CborReadStatus.Error)
            {
                CborReadResult failure = CreateSkipTokenFailure(
                    tokenResult,
                    sourceOffset,
                    source.Length - sourceOffset
                );
                ResetSkipState();
                return failure;
            }

            if (tokenResult.Status == CborReadStatus.NeedMoreData)
            {
                return CreateSkipStatusResult(
                    CborReadStatus.NeedMoreData,
                    sourceOffset,
                    source.Length - sourceOffset
                );
            }
            if (tokenResult.Token == null)
            {
                CborReadResult missingToken = CreateSkipErrorResult(
                    "CBOR_SKIP_TOKEN_MISSING",
                    "The tokenizer completed without producing a token.",
                    AbsoluteOffset,
                    sourceOffset,
                    source.Length - sourceOffset
                );
                ResetSkipState();
                return missingToken;
            }

            CborToken token = tokenResult.Token;
            if (!SkipState.Started)
            {
                if (IsSkipEndToken(token.Kind) || IsSkipChunkToken(token.Kind))
                {
                    CborReadResult noValue = CreateSkipErrorResult(
                        "CBOR_NO_VALUE_TO_SKIP",
                        "The reader is not positioned at the start of a CBOR value.",
                        AbsoluteOffset - tokenResult.Consumed,
                        sourceOffset,
                        source.Length - sourceOffset
                    );
                    ResetSkipState();
                    return noValue;
                }
                SkipState.Started = true;
            }

            if (Frames.Count <= SkipState.BaseFrameCount)
            {
                CborReadResult completed = CreateSkipStatusResult(
                    CborReadStatus.Done,
                    sourceOffset,
                    source.Length - sourceOffset
                );
                ResetSkipState();
                return completed;
            }
        }
    }

    internal CborTokenReadResult? ValidateContainerCapacity(int sourceLength)
    {
        if (Frames.Count == 0)
        {
            return null;
        }
        CborTokenFrame frame = Frames[Frames.Count - 1];
        if (frame.Kind == CborTokenKind.StartArray
            && frame.IsIndefinite
            && (frame.CompletedCount == ulong.MaxValue
                || !Limits.AllowsArrayItems(frame.CompletedCount + 1)))
        {
            return CreateErrorResult(
                "CBOR_MAX_ARRAY_ITEMS",
                "The indefinite array exceeds the configured item limit.",
                0,
                sourceLength
            );
        }
        if (frame.Kind == CborTokenKind.StartMap
            && frame.IsIndefinite
            && !frame.MapExpectsValue
            && (frame.CompletedCount == ulong.MaxValue
                || !Limits.AllowsMapPairs(frame.CompletedCount + 1)))
        {
            return CreateErrorResult(
                "CBOR_MAX_MAP_PAIRS",
                "The indefinite map exceeds the configured pair limit.",
                0,
                sourceLength
            );
        }
        return null;
    }

    internal CborTokenReadResult? ValidateItemCapacity(int sourceLength)
    {
        if (Frames.Count > 0)
        {
            CborTokenFrame frame = Frames[Frames.Count - 1];
            if (frame.Kind == CborTokenKind.StartByteString
                || frame.Kind == CborTokenKind.StartTextString)
            {
                return null;
            }
        }
        if (!Limits.AllowsItem())
        {
            return CreateErrorResult(
                "CBOR_MAX_TOTAL_ITEMS",
                "The operation exceeds the configured item limit.",
                0,
                sourceLength
            );
        }
        return null;
    }

    internal CborTokenReadResult? ValidateStringLength(ulong length, int sourceLength)
    {
        if (!Limits.AllowsStringBytes(length))
        {
            return CreateErrorResult(
                "CBOR_MAX_STRING_BYTES",
                "The string exceeds the configured byte-length limit.",
                0,
                sourceLength
            );
        }
        if (Frames.Count == 0)
        {
            return null;
        }
        CborTokenFrame frame = Frames[Frames.Count - 1];
        if ((frame.Kind == CborTokenKind.StartByteString
                || frame.Kind == CborTokenKind.StartTextString)
            && !Limits.AllowsStringTotal(frame.StringByteCount, length))
        {
            return CreateErrorResult(
                "CBOR_MAX_STRING_BYTES",
                "The indefinite string exceeds the configured cumulative byte-length limit.",
                0,
                sourceLength
            );
        }
        return null;
    }

    internal CborTokenReadResult? ValidateEncodedLength(int encodedLength, int sourceLength)
    {
        if (!Limits.AllowsEncodedBytes(encodedLength))
        {
            return CreateErrorResult(
                "CBOR_MAX_TOTAL_BYTES",
                "The token exceeds the configured encoded-byte limit.",
                0,
                sourceLength
            );
        }
        return null;
    }

    internal CborTokenReadResult? ValidateTokenLimits(
        CborToken token,
        int encodedLength,
        int sourceLength)
    {
        CborTokenReadResult? byteFailure = ValidateEncodedLength(encodedLength, sourceLength);
        if (byteFailure != null)
        {
            return byteFailure;
        }
        if (CountsAsDecodeItem(token.Kind) && !Limits.AllowsItem())
        {
            return CreateErrorResult(
                "CBOR_MAX_TOTAL_ITEMS",
                "The operation exceeds the configured item limit.",
                0,
                sourceLength
            );
        }
        if (OpensNestingFrame(token.Kind))
        {
            if (Frames.Count == int.MaxValue
                || !Limits.AllowsNesting(Frames.Count + 1))
            {
                return CreateErrorResult(
                    "CBOR_MAX_NESTING_DEPTH",
                    "The operation exceeds the configured nesting-depth limit.",
                    0,
                    sourceLength
                );
            }
        }
        if (token.Kind == CborTokenKind.StartArray
            && token.LengthKind == CborLengthKind.Definite
            && !Limits.AllowsArrayItems(token.Length))
        {
            return CreateErrorResult(
                "CBOR_MAX_ARRAY_ITEMS",
                "The array exceeds the configured item limit.",
                0,
                sourceLength
            );
        }
        if (token.Kind == CborTokenKind.StartMap
            && token.LengthKind == CborLengthKind.Definite
            && !Limits.AllowsMapPairs(token.Length))
        {
            return CreateErrorResult(
                "CBOR_MAX_MAP_PAIRS",
                "The map exceeds the configured pair limit.",
                0,
                sourceLength
            );
        }
        if ((Options.RetainRepresentation || Options.RetainSourceRanges)
            && !Limits.AllowsRetainedBytes(encodedLength))
        {
            return CreateErrorResult(
                "CBOR_MAX_RETAINED_SOURCE_BYTES",
                "The operation exceeds the configured retained-representation byte limit.",
                0,
                sourceLength
            );
        }
        return null;
    }

    internal void CommitTokenLimits(CborToken token, int encodedLength)
    {
        Limits.CommitEncodedBytes(encodedLength);
        if (CountsAsDecodeItem(token.Kind))
        {
            Limits.CommitItem();
        }
        if (Options.RetainRepresentation || Options.RetainSourceRanges)
        {
            Limits.CommitRetainedBytes(encodedLength);
        }
    }

    internal static bool OpensNestingFrame(CborTokenKind kind)
    {
        return kind == CborTokenKind.StartByteString
            || kind == CborTokenKind.StartTextString
            || kind == CborTokenKind.StartArray
            || kind == CborTokenKind.StartMap
            || kind == CborTokenKind.Tag;
    }

    internal static bool CountsAsDecodeItem(CborTokenKind kind)
    {
        return kind != CborTokenKind.None
            && kind != CborTokenKind.ByteStringChunk
            && kind != CborTokenKind.TextStringChunk
            && !IsSkipEndToken(kind);
    }

    internal static bool IsSkipEndToken(CborTokenKind kind)
    {
        return kind == CborTokenKind.EndByteString
            || kind == CborTokenKind.EndTextString
            || kind == CborTokenKind.EndArray
            || kind == CborTokenKind.EndMap;
    }

    internal static bool IsSkipChunkToken(CborTokenKind kind)
    {
        return kind == CborTokenKind.ByteStringChunk
            || kind == CborTokenKind.TextStringChunk;
    }

    internal static CborReadResult CreateSkipStatusResult(
        CborReadStatus status,
        int consumed,
        int remaining)
    {
        CborReadResult result = new CborReadResult();
        result.Status = status;
        result.Consumed = consumed;
        result.Remaining = remaining;
        return result;
    }

    internal static CborReadResult CreateSkipTokenFailure(
        CborTokenReadResult failure,
        int consumed,
        int remaining)
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.Consumed = consumed;
        result.Remaining = remaining;
        result.ErrorCode = failure.ErrorCode;
        result.ErrorMessage = failure.ErrorMessage;
        result.ErrorOffset = failure.ErrorOffset;
        result.ErrorPath = failure.ErrorPath;
        return result;
    }

    internal static CborReadResult CreateSkipErrorResult(
        string errorCode,
        string errorMessage,
        long errorOffset,
        int consumed,
        int remaining)
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.Consumed = consumed;
        result.Remaining = remaining;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.ErrorOffset = errorOffset;
        result.ErrorPath = "$";
        return result;
    }

    internal void ResetSkipState()
    {
        SkipState.Active = false;
        SkipState.Started = false;
        SkipState.BaseFrameCount = 0;
    }

    internal void TrackToken(CborToken token)
    {
        CborTokenFrame frame;
        switch (token.Kind)
        {
            case CborTokenKind.StartByteString:
                frame = CreateIndefiniteFrame(CborTokenKind.StartByteString, token);
                Frames.Add(frame);
                return;
            case CborTokenKind.ByteStringChunk:
                RecordStringChunk(token);
                return;
            case CborTokenKind.StartTextString:
                frame = CreateIndefiniteFrame(CborTokenKind.StartTextString, token);
                Frames.Add(frame);
                return;
            case CborTokenKind.TextStringChunk:
                RecordStringChunk(token);
                return;
            case CborTokenKind.StartArray:
                frame = new CborTokenFrame();
                frame.Kind = CborTokenKind.StartArray;
                frame.RemainingCount = token.Length;
                frame.IsIndefinite = token.LengthKind == CborLengthKind.Indefinite;
                frame.StartRepresentation = token.Representation;
                Frames.Add(frame);
                return;
            case CborTokenKind.StartMap:
                frame = new CborTokenFrame();
                frame.Kind = CborTokenKind.StartMap;
                frame.RemainingCount = token.Length;
                frame.IsIndefinite = token.LengthKind == CborLengthKind.Indefinite;
                frame.StartRepresentation = token.Representation;
                Frames.Add(frame);
                return;
            case CborTokenKind.Tag:
                frame = new CborTokenFrame();
                frame.Kind = CborTokenKind.Tag;
                frame.RemainingCount = 1;
                Frames.Add(frame);
                return;
            default:
                CompleteValue();
                return;
        }
    }

    internal CborTokenReadResult? CreateContainerCompletionResult(int sourceLength)
    {
        if (Frames.Count == 0)
        {
            return null;
        }
        CborTokenFrame frame = Frames[Frames.Count - 1];
        bool isArrayComplete = frame.Kind == CborTokenKind.StartArray
            && !frame.IsIndefinite
            && frame.RemainingCount == 0;
        bool isMapComplete = frame.Kind == CborTokenKind.StartMap
            && !frame.IsIndefinite
            && frame.RemainingCount == 0
            && !frame.MapExpectsValue;
        if (!isArrayComplete && !isMapComplete)
        {
            return null;
        }

        Frames.RemoveAt(Frames.Count - 1);
        CborToken token = new CborToken();
        token.LengthKind = CborLengthKind.Definite;
        if (isArrayComplete)
        {
            token.Kind = CborTokenKind.EndArray;
        }
        else
        {
            token.Kind = CborTokenKind.EndMap;
        }
        CompleteValue();
        return CreateSyntheticDoneResult(token, sourceLength);
    }

    internal void CompleteValue()
    {
        while (Frames.Count > 0)
        {
            CborTokenFrame frame = Frames[Frames.Count - 1];
            if (frame.Kind == CborTokenKind.Tag)
            {
                Frames.RemoveAt(Frames.Count - 1);
                continue;
            }
            if (frame.Kind == CborTokenKind.StartArray)
            {
                if (!frame.IsIndefinite && frame.RemainingCount > 0)
                {
                    frame.RemainingCount = frame.RemainingCount - 1;
                }
                if (frame.CompletedCount < ulong.MaxValue)
                {
                    frame.CompletedCount = frame.CompletedCount + 1;
                }
                return;
            }
            if (frame.Kind == CborTokenKind.StartByteString
                || frame.Kind == CborTokenKind.StartTextString)
            {
                return;
            }
            if (!frame.MapExpectsValue)
            {
                frame.MapExpectsValue = true;
                return;
            }
            frame.MapExpectsValue = false;
            if (!frame.IsIndefinite && frame.RemainingCount > 0)
            {
                frame.RemainingCount = frame.RemainingCount - 1;
            }
            if (frame.CompletedCount < ulong.MaxValue)
            {
                frame.CompletedCount = frame.CompletedCount + 1;
            }
            return;
        }
    }

    internal CborTokenReadResult ReadIndefiniteStartToken(int majorType, int sourceLength)
    {
        CborTokenReadResult? chunkFailure = ValidateIndefiniteStringStart(majorType, sourceLength);
        if (chunkFailure != null)
        {
            return chunkFailure;
        }
        if (majorType < 2 || majorType > 5)
        {
            return CreateErrorResult(
                "CBOR_ILLEGAL_INDEFINITE_HEAD",
                "This CBOR major type cannot use an indefinite-length head.",
                0,
                sourceLength
            );
        }
        CborTokenReadResult? itemCapacityFailure = ValidateItemCapacity(sourceLength);
        if (itemCapacityFailure != null)
        {
            return itemCapacityFailure;
        }
        if (!Options.AllowIndefiniteLength)
        {
            return CreateErrorResult(
                "CBOR_INDEFINITE_LENGTH_DISABLED",
                "Indefinite-length items are disabled by the decoding options.",
                0,
                sourceLength
            );
        }

        CborToken token = new CborToken();
        token.LengthKind = CborLengthKind.Indefinite;
        switch (majorType)
        {
            case 2:
                token.Kind = CborTokenKind.StartByteString;
                break;
            case 3:
                token.Kind = CborTokenKind.StartTextString;
                break;
            case 4:
                token.Kind = CborTokenKind.StartArray;
                break;
            case 5:
                token.Kind = CborTokenKind.StartMap;
                break;
            default:
                return CreateErrorResult(
                    "CBOR_ILLEGAL_INDEFINITE_HEAD",
                    "This CBOR major type cannot use an indefinite-length head.",
                    0,
                    sourceLength
                );
        }

        CborTokenReadResult? tokenLimitFailure = ValidateTokenLimits(token, 1, sourceLength);
        if (tokenLimitFailure != null)
        {
            return tokenLimitFailure;
        }
        token.Representation = CreateRepresentation(0, 1, token);
        CborTokenReadResult result = CreateDoneResult(token, 1, sourceLength);
        if (result.Status == CborReadStatus.Done)
        {
            CommitTokenLimits(token, 1);
            TrackToken(token);
        }
        return result;
    }

    internal CborTokenReadResult ReadBreakToken(int sourceLength)
    {
        if (Frames.Count == 0)
        {
            return CreateErrorResult(
                "CBOR_UNEXPECTED_BREAK",
                "A break code can appear only inside an indefinite-length item.",
                0,
                sourceLength
            );
        }
        CborTokenFrame frame = Frames[Frames.Count - 1];
        if (frame.Kind == CborTokenKind.Tag)
        {
            return CreateErrorResult(
                "CBOR_BREAK_AFTER_TAG",
                "A semantic tag must be followed by one complete value.",
                0,
                sourceLength
            );
        }
        if (!frame.IsIndefinite)
        {
            return CreateErrorResult(
                "CBOR_BREAK_IN_DEFINITE_CONTAINER",
                "A definite-length item cannot be closed by a break code.",
                0,
                sourceLength
            );
        }
        if (frame.Kind == CborTokenKind.StartMap && frame.MapExpectsValue)
        {
            return CreateErrorResult(
                "CBOR_ODD_INDEFINITE_MAP",
                "An indefinite-length map cannot end after a key without its value.",
                0,
                sourceLength
            );
        }

        CborToken token = new CborToken();
        token.LengthKind = CborLengthKind.Indefinite;
        switch (frame.Kind)
        {
            case CborTokenKind.StartByteString:
                token.Kind = CborTokenKind.EndByteString;
                break;
            case CborTokenKind.StartTextString:
                token.Kind = CborTokenKind.EndTextString;
                break;
            case CborTokenKind.StartArray:
                token.Kind = CborTokenKind.EndArray;
                break;
            case CborTokenKind.StartMap:
                token.Kind = CborTokenKind.EndMap;
                break;
            default:
                return CreateErrorResult(
                    "CBOR_UNEXPECTED_BREAK",
                    "A break code cannot replace a required tagged value.",
                    0,
                    sourceLength
                );
        }
        CborTokenReadResult? tokenLimitFailure = ValidateTokenLimits(token, 1, sourceLength);
        if (tokenLimitFailure != null)
        {
            return tokenLimitFailure;
        }
        token.Representation = CreateRepresentation(0, 1, token);
        Frames.RemoveAt(Frames.Count - 1);
        CompleteValue();
        CborTokenReadResult result = CreateDoneResult(token, 1, sourceLength);
        if (result.Status == CborReadStatus.Done)
        {
            CommitTokenLimits(token, 1);
        }
        return result;
    }

    internal CborTokenReadResult? ValidateStringChunkType(int majorType, int sourceLength)
    {
        if (Frames.Count == 0)
        {
            return null;
        }
        CborTokenFrame frame = Frames[Frames.Count - 1];
        if (frame.Kind == CborTokenKind.StartByteString && majorType != 2)
        {
            return CreateErrorResult(
                "CBOR_INVALID_BYTE_STRING_CHUNK",
                "An indefinite-length byte string can contain only definite byte string chunks.",
                0,
                sourceLength
            );
        }
        if (frame.Kind == CborTokenKind.StartTextString && majorType != 3)
        {
            return CreateErrorResult(
                "CBOR_INVALID_TEXT_STRING_CHUNK",
                "An indefinite-length text string can contain only definite text string chunks.",
                0,
                sourceLength
            );
        }
        return null;
    }

    internal CborTokenReadResult? ValidateIndefiniteStringStart(int majorType, int sourceLength)
    {
        if (Frames.Count == 0)
        {
            return null;
        }
        CborTokenFrame frame = Frames[Frames.Count - 1];
        if (frame.Kind == CborTokenKind.StartByteString)
        {
            if (majorType == 2)
            {
                return CreateErrorResult(
                    "CBOR_NESTED_INDEFINITE_BYTE_STRING",
                    "An indefinite-length byte string chunk must have a definite length.",
                    0,
                    sourceLength
                );
            }
            return CreateErrorResult(
                "CBOR_INVALID_BYTE_STRING_CHUNK",
                "An indefinite-length byte string can contain only definite byte string chunks.",
                0,
                sourceLength
            );
        }
        if (frame.Kind == CborTokenKind.StartTextString)
        {
            if (majorType == 3)
            {
                return CreateErrorResult(
                    "CBOR_NESTED_INDEFINITE_TEXT_STRING",
                    "An indefinite-length text string chunk must have a definite length.",
                    0,
                    sourceLength
                );
            }
            return CreateErrorResult(
                "CBOR_INVALID_TEXT_STRING_CHUNK",
                "An indefinite-length text string can contain only definite text string chunks.",
                0,
                sourceLength
            );
        }
        return null;
    }

    internal bool IsInsideIndefiniteString(CborTokenKind expectedKind)
    {
        if (Frames.Count == 0)
        {
            return false;
        }
        CborTokenFrame frame = Frames[Frames.Count - 1];
        return frame.IsIndefinite && frame.Kind == expectedKind;
    }

    internal CborTokenFrame CreateIndefiniteFrame(CborTokenKind kind, CborToken token)
    {
        CborTokenFrame frame = new CborTokenFrame();
        frame.Kind = kind;
        frame.IsIndefinite = true;
        frame.StartRepresentation = token.Representation;
        if (Options.RetainRepresentation && frame.StartRepresentation != null)
        {
            frame.StartRepresentation.ChunkByteLengths = new List<int>();
        }
        return frame;
    }

    internal void RecordStringChunk(CborToken token)
    {
        if (Frames.Count == 0)
        {
            return;
        }
        CborTokenFrame frame = Frames[Frames.Count - 1];
        if (frame.StringByteCount <= ulong.MaxValue - token.Length)
        {
            frame.StringByteCount = frame.StringByteCount + token.Length;
        }
        else
        {
            frame.StringByteCount = ulong.MaxValue;
        }
        if (frame.StartRepresentation != null
            && frame.StartRepresentation.ChunkByteLengths != null)
        {
            frame.StartRepresentation.ChunkByteLengths.Add((int)token.Length);
        }
    }

    internal CborTokenReadResult? DecodeTextToken(
        CborToken token,
        byte[] source,
        int payloadOffset,
        int payloadLength)
    {
        if (!Options.RejectInvalidUtf8)
        {
            token.TextStringValue = Encoding.UTF8.GetString(source, payloadOffset, payloadLength);
            return null;
        }

        CborUtf8 decoder = new CborUtf8();
        CborUtf8Result decoded = decoder.ProcessChunk(
            source.AsSpan(payloadOffset, payloadLength),
            true
        );
        if (decoded.Status == CborReadStatus.Error)
        {
            return CreateErrorResult(
                decoded.ErrorCode ?? "CBOR_UTF8_INVALID",
                decoded.ErrorMessage ?? "The text string is not valid UTF-8.",
                payloadOffset + decoded.ErrorOffset,
                source.Length
            );
        }
        token.TextStringValue = decoded.Text;
        return null;
    }

    internal CborTokenReadResult? DecodeSimpleOrFloatToken(
        CborToken token,
        byte[] source,
        int additionalInformation,
        ulong argument)
    {
        if (additionalInformation <= 23)
        {
            byte simpleValue = (byte)additionalInformation;
            if (!Options.AllowUnknownSimpleValues && !IsKnownSimpleValue(simpleValue))
            {
                return CreateUnknownSimpleResult(simpleValue, source.Length);
            }
            token.Kind = CborTokenKind.SimpleValue;
            token.SimpleValue = simpleValue;
            return null;
        }
        if (additionalInformation == 24)
        {
            byte simpleValue = (byte)argument;
            if (simpleValue < 32)
            {
                return CreateErrorResult(
                    "CBOR_INVALID_SIMPLE_VALUE_ENCODING",
                    "A two-byte simple value must have a value of at least 32.",
                    1,
                    source.Length
                );
            }
            if (!Options.AllowUnknownSimpleValues)
            {
                return CreateUnknownSimpleResult(simpleValue, source.Length);
            }
            token.Kind = CborTokenKind.SimpleValue;
            token.SimpleValue = simpleValue;
            return null;
        }
        if (additionalInformation == 25)
        {
            token.Kind = CborTokenKind.Float;
            token.FloatWidth = CborFloatWidth.Half;
            token.FloatValue = CborHalfPrecision.CreateHalfFloat((ushort)argument);
            return null;
        }
        if (additionalInformation == 26)
        {
            token.Kind = CborTokenKind.Float;
            token.FloatWidth = CborFloatWidth.Single;
            token.FloatValue = CborHalfPrecision.CreateSingleFloat((uint)argument);
            return null;
        }
        token.Kind = CborTokenKind.Float;
        token.FloatWidth = CborFloatWidth.Double;
        token.FloatValue = CborHalfPrecision.CreateDoubleFloat(argument);
        return null;
    }

    internal CborTokenReadResult CreateUnknownSimpleResult(byte simpleValue, int sourceLength)
    {
        return CreateErrorResult(
            "CBOR_UNKNOWN_SIMPLE_VALUE",
            $"Simple value {simpleValue} is not enabled by the decoding options.",
            0,
            sourceLength
        );
    }

    internal static bool IsKnownSimpleValue(byte simpleValue)
    {
        return simpleValue >= CborSimpleValues.False && simpleValue <= CborSimpleValues.Undefined;
    }

    internal CborRepresentation? CreateRepresentation(
        int argumentWidth,
        int consumed,
        CborToken token)
    {
        if (!Options.RetainRepresentation && !Options.RetainSourceRanges)
        {
            return null;
        }

        CborRepresentation representation = new CborRepresentation();
        representation.HeadArgumentWidth = argumentWidth;
        representation.LengthKind = token.LengthKind;
        representation.FloatWidth = token.FloatWidth;
        if (token.FloatValue != null)
        {
            representation.FloatBits = token.FloatValue.RawBits;
            representation.HasFloatBits = true;
        }
        if (Options.RetainSourceRanges)
        {
            representation.HasSourceRange = true;
            representation.SourceOffset = AbsoluteOffset;
            representation.SourceLength = consumed;
        }
        return representation;
    }

    internal CborTokenReadResult CreateDoneResult(CborToken token, int consumed, int sourceLength)
    {
        if (AbsoluteOffset > long.MaxValue - consumed)
        {
            return CreateErrorResult(
                "CBOR_SOURCE_OFFSET_OVERFLOW",
                "The absolute token source offset exceeds the supported range.",
                0,
                sourceLength
            );
        }
        CborTokenReadResult result = new CborTokenReadResult();
        result.Status = CborReadStatus.Done;
        result.Token = token;
        result.Consumed = consumed;
        result.Remaining = sourceLength - consumed;
        AbsoluteOffset = AbsoluteOffset + consumed;
        return result;
    }

    internal static CborTokenReadResult CreateSyntheticDoneResult(
        CborToken token,
        int sourceLength)
    {
        CborTokenReadResult result = new CborTokenReadResult();
        result.Status = CborReadStatus.Done;
        result.Token = token;
        result.Remaining = sourceLength;
        return result;
    }

    internal static CborTokenReadResult CreateNeedMoreResult(int sourceLength)
    {
        CborTokenReadResult result = new CborTokenReadResult();
        result.Status = CborReadStatus.NeedMoreData;
        result.Remaining = sourceLength;
        return result;
    }

    internal CborTokenReadResult CreateErrorResult(
        string errorCode,
        string errorMessage,
        long relativeOffset,
        int sourceLength)
    {
        CborTokenReadResult result = new CborTokenReadResult();
        result.Status = CborReadStatus.Error;
        result.Remaining = sourceLength;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        if (relativeOffset > long.MaxValue - AbsoluteOffset)
        {
            result.ErrorOffset = long.MaxValue;
        }
        else
        {
            result.ErrorOffset = AbsoluteOffset + relativeOffset;
        }
        result.ErrorPath = "$";
        return result;
    }

    internal static int GetArgumentWidth(int additionalInformation)
    {
        if (additionalInformation < 24)
        {
            return 0;
        }
        if (additionalInformation == 24)
        {
            return 1;
        }
        if (additionalInformation == 25)
        {
            return 2;
        }
        if (additionalInformation == 26)
        {
            return 4;
        }
        return 8;
    }

    internal static ulong ReadArgumentValue(
        byte[] source,
        int additionalInformation,
        int argumentWidth)
    {
        if (additionalInformation < 24)
        {
            return (ulong)additionalInformation;
        }
        return CborBinaryPrimitives.ReadArgument(
            source.AsSpan(1, argumentWidth),
            additionalInformation
        );
    }
}
