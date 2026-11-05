using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace cbor;

internal class CborTokenWriterCore
{
    internal CborEncodeOptions Options;
    internal long TotalBytes;
    internal int TotalItems;
    internal List<CborTokenWriterFrame> Frames = new List<CborTokenWriterFrame>();
    internal bool Faulted;

    internal CborTokenWriterCore(CborEncodeOptions options)
    {
        Options = options;
    }

    internal CborWriteResult WriteTokenToSpan(CborToken token, Span<byte> destination)
    {
        CborTokenWritePlan plan = CborTokenWritePlan.Create(token);
        CborWriteResult? failure = ValidatePlan(plan);
        if (failure != null)
        {
            return failure;
        }
        if (destination.Length < plan.Required)
        {
            return CreateDestinationTooSmallResult(plan.Required);
        }
        if (plan.Required > 0)
        {
            WritePlan(plan, destination);
        }
        CommitPlan(plan);
        return CreateDoneResult(plan.Required);
    }

    internal CborWriteResult WriteTokenToBuffer(
        CborToken token,
        IBufferWriter<byte> destination)
    {
        CborTokenWritePlan plan = CborTokenWritePlan.Create(token);
        CborWriteResult? failure = ValidatePlan(plan);
        if (failure != null)
        {
            return failure;
        }
        if (destination == null)
        {
            return CreateErrorResult(
                "CBOR_DESTINATION_MISSING",
                "A destination buffer writer is required.",
                plan.Required
            );
        }
        if (plan.Required > 0)
        {
            Span<byte> output = destination.GetSpan(plan.Required);
            if (output.Length < plan.Required)
            {
                return CreateDestinationTooSmallResult(plan.Required);
            }
            WritePlan(plan, output);
            destination.Advance(plan.Required);
        }
        CommitPlan(plan);
        return CreateDoneResult(plan.Required);
    }

    internal CborWriteResult CompleteWriter(Span<byte> destination)
    {
        if (Faulted)
        {
            return CreateFaultedResult();
        }
        if (Frames.Count > 0)
        {
            return CreateIncompleteResult();
        }
        return CreateDoneResult(0);
    }

    internal void Reset()
    {
        TotalBytes = 0;
        TotalItems = 0;
        Frames.Clear();
        Faulted = false;
    }

    internal CborWriteResult? ValidatePlan(CborTokenWritePlan plan)
    {
        if (Faulted)
        {
            return CreateFaultedResult();
        }
        if (plan.ErrorCode != null)
        {
            return CreateErrorResult(
                plan.ErrorCode,
                plan.ErrorMessage ?? "The token cannot be encoded.",
                plan.Required
            );
        }
        if (Options.PreserveRepresentation
            && Options.EncodingProfile != CborEncodingProfile.CoreDeterministic
            && Options.EncodingProfile
                != CborEncodingProfile.LengthFirstCoreDeterministic)
        {
            plan.ApplyRepresentation();
            if (plan.ErrorCode != null)
            {
                return CreateErrorResult(
                    plan.ErrorCode,
                    plan.ErrorMessage ?? "The retained representation is invalid.",
                    plan.Required
                );
            }
        }
        CborWriteResult? stateFailure = ValidateStateTransition(plan);
        if (stateFailure != null)
        {
            return stateFailure;
        }
        if (plan.CountsAsItem
            && (Options.MaxTotalItems < 0 || TotalItems >= Options.MaxTotalItems))
        {
            return CreateErrorResult(
                "CBOR_MAX_TOTAL_ITEMS",
                "The token output exceeds the configured item limit.",
                plan.Required
            );
        }
        if (plan.IsStart && plan.IsIndefinite && !Options.AllowIndefiniteLength)
        {
            return CreateErrorResult(
                "CBOR_INDEFINITE_LENGTH_DISABLED",
                "Indefinite-length output is disabled by the encoding options.",
                plan.Required
            );
        }
        if (plan.IsStart
            && (Options.MaxNestingDepth < 0
                || Frames.Count >= Options.MaxNestingDepth))
        {
            return CreateErrorResult(
                "CBOR_MAX_NESTING_DEPTH",
                "The token output exceeds the configured nesting-depth limit.",
                plan.Required
            );
        }
        if (Options.MaxOutputBytes < 0
            || TotalBytes > Options.MaxOutputBytes - plan.Required)
        {
            return CreateErrorResult(
                "CBOR_MAX_OUTPUT_BYTES",
                "The token output exceeds the configured byte limit.",
                plan.Required
            );
        }
        return null;
    }

    internal CborWriteResult? ValidateStateTransition(CborTokenWritePlan plan)
    {
        if (plan.IsEnd)
        {
            return ValidateEndTransition(plan);
        }
        if (Frames.Count == 0)
        {
            if (plan.IsChunk)
            {
                return CreateStateErrorResult(
                    "CBOR_UNEXPECTED_STRING_CHUNK",
                    "A string chunk requires an open indefinite-length string."
                );
            }
            return null;
        }
        CborTokenWriterFrame frame = Frames[Frames.Count - 1];
        if (frame.Kind == CborTokenKind.StartByteString)
        {
            if (plan.TokenKind == CborTokenKind.ByteStringChunk)
            {
                return null;
            }
            if (plan.TokenKind == CborTokenKind.StartByteString)
            {
                return CreateStateErrorResult(
                    "CBOR_NESTED_INDEFINITE_BYTE_STRING",
                    "An indefinite byte string cannot contain another indefinite byte string."
                );
            }
            return CreateStateErrorResult(
                "CBOR_INVALID_BYTE_STRING_CHUNK",
                "An indefinite byte string can contain only definite byte string chunks."
            );
        }
        if (frame.Kind == CborTokenKind.StartTextString)
        {
            if (plan.TokenKind == CborTokenKind.TextStringChunk)
            {
                return null;
            }
            if (plan.TokenKind == CborTokenKind.StartTextString)
            {
                return CreateStateErrorResult(
                    "CBOR_NESTED_INDEFINITE_TEXT_STRING",
                    "An indefinite text string cannot contain another indefinite text string."
                );
            }
            return CreateStateErrorResult(
                "CBOR_INVALID_TEXT_STRING_CHUNK",
                "An indefinite text string can contain only definite text string chunks."
            );
        }
        if (plan.IsChunk)
        {
            return CreateStateErrorResult(
                "CBOR_UNEXPECTED_STRING_CHUNK",
                "A string chunk requires a matching open indefinite-length string."
            );
        }
        if (frame.Kind == CborTokenKind.StartArray
            && !frame.IsIndefinite
            && frame.RemainingCount == 0)
        {
            return CreateStateErrorResult(
                "CBOR_ARRAY_ITEM_COUNT_EXCEEDED",
                "The definite array already contains its declared number of items."
            );
        }
        if (frame.Kind == CborTokenKind.StartMap
            && !frame.IsIndefinite
            && frame.RemainingCount == 0
            && !frame.MapExpectsValue)
        {
            return CreateStateErrorResult(
                "CBOR_MAP_PAIR_COUNT_EXCEEDED",
                "The definite map already contains its declared number of pairs."
            );
        }
        return null;
    }

    internal CborWriteResult? ValidateEndTransition(CborTokenWritePlan plan)
    {
        if (Frames.Count == 0)
        {
            return CreateStateErrorResult(
                "CBOR_UNEXPECTED_END_TOKEN",
                "A container end token requires an open container."
            );
        }
        CborTokenWriterFrame frame = Frames[Frames.Count - 1];
        bool matchesArray = plan.TokenKind == CborTokenKind.EndArray
            && frame.Kind == CborTokenKind.StartArray;
        bool matchesMap = plan.TokenKind == CborTokenKind.EndMap
            && frame.Kind == CborTokenKind.StartMap;
        bool matchesByteString = plan.TokenKind == CborTokenKind.EndByteString
            && frame.Kind == CborTokenKind.StartByteString;
        bool matchesTextString = plan.TokenKind == CborTokenKind.EndTextString
            && frame.Kind == CborTokenKind.StartTextString;
        if (!matchesArray && !matchesMap && !matchesByteString && !matchesTextString)
        {
            return CreateStateErrorResult(
                "CBOR_MISMATCHED_END_TOKEN",
                "The container end token does not match the open writer frame."
            );
        }
        CborLengthKind expectedLengthKind = frame.IsIndefinite
            ? CborLengthKind.Indefinite
            : CborLengthKind.Definite;
        if (plan.LengthKind != CborLengthKind.None
            && plan.LengthKind != expectedLengthKind)
        {
            return CreateStateErrorResult(
                "CBOR_CONTAINER_LENGTH_KIND_MISMATCH",
                "The end token length kind does not match the open writer frame."
            );
        }
        plan.IsIndefinite = frame.IsIndefinite;
        if (frame.IsIndefinite)
        {
            plan.WritesBreak = true;
            plan.Required = 1;
        }
        if (matchesArray && !frame.IsIndefinite && frame.RemainingCount != 0)
        {
            return CreateStateErrorResult(
                "CBOR_ARRAY_ITEM_COUNT_MISMATCH",
                "The definite array ended before its declared number of items was written."
            );
        }
        if (matchesMap && frame.MapExpectsValue)
        {
            return CreateStateErrorResult(
                "CBOR_MAP_VALUE_MISSING",
                "The definite map ended after a key without a value."
            );
        }
        if (matchesMap && !frame.IsIndefinite && frame.RemainingCount != 0)
        {
            return CreateStateErrorResult(
                "CBOR_MAP_PAIR_COUNT_MISMATCH",
                "The definite map ended before its declared number of pairs was written."
            );
        }
        return null;
    }

    internal static void WritePlan(CborTokenWritePlan plan, Span<byte> destination)
    {
        if (plan.WritesBreak)
        {
            CborBinaryPrimitives.WriteBreak(destination);
            return;
        }
        if (plan.IsStart && plan.IsIndefinite)
        {
            CborBinaryPrimitives.WriteIndefiniteHead(destination, plan.MajorType);
            return;
        }
        if (plan.IsFloat)
        {
            WriteFloatPlan(plan, destination);
            return;
        }
        int headSize;
        if (plan.UsesRequestedHeadWidth)
        {
            headSize = CborBinaryPrimitives.WriteHeadWithWidth(
                destination,
                plan.MajorType,
                plan.Argument,
                plan.RequestedHeadArgumentWidth
            );
        }
        else
        {
            headSize = CborBinaryPrimitives.WriteHead(
                destination,
                plan.MajorType,
                plan.Argument
            );
        }
        if (plan.BytePayload != null)
        {
            plan.BytePayload.AsSpan().CopyTo(destination.Slice(headSize));
        }
        if (plan.TextPayload != null)
        {
            Encoding.UTF8.GetBytes(plan.TextPayload, destination.Slice(headSize));
        }
    }

    internal static void WriteFloatPlan(CborTokenWritePlan plan, Span<byte> destination)
    {
        if (plan.FloatWidth == CborFloatWidth.Half)
        {
            destination[0] = 249;
            CborBinaryPrimitives.WriteUInt16BigEndian(
                destination.Slice(1, 2),
                (ushort)plan.FloatBits
            );
            return;
        }
        if (plan.FloatWidth == CborFloatWidth.Single)
        {
            destination[0] = 250;
            CborBinaryPrimitives.WriteUInt32BigEndian(
                destination.Slice(1, 4),
                (uint)plan.FloatBits
            );
            return;
        }
        destination[0] = 251;
        CborBinaryPrimitives.WriteUInt64BigEndian(
            destination.Slice(1, 8),
            plan.FloatBits
        );
    }

    internal void CommitPlan(CborTokenWritePlan plan)
    {
        TotalBytes = TotalBytes + plan.Required;
        if (plan.CountsAsItem)
        {
            TotalItems = TotalItems + 1;
        }
        ApplyState(plan);
    }

    internal void ApplyState(CborTokenWritePlan plan)
    {
        if (plan.IsEnd)
        {
            Frames.RemoveAt(Frames.Count - 1);
            CompleteValue();
            return;
        }
        if (plan.IsStart)
        {
            CborTokenWriterFrame frame = new CborTokenWriterFrame();
            frame.Kind = plan.TokenKind;
            frame.IsIndefinite = plan.IsIndefinite;
            if (plan.TokenKind == CborTokenKind.StartArray
                || plan.TokenKind == CborTokenKind.StartMap)
            {
                frame.RemainingCount = plan.Argument;
            }
            else
            {
                frame.RemainingCount = 1;
            }
            Frames.Add(frame);
            return;
        }
        if (plan.IsChunk)
        {
            return;
        }
        CompleteValue();
    }

    internal void CompleteValue()
    {
        while (Frames.Count > 0)
        {
            CborTokenWriterFrame frame = Frames[Frames.Count - 1];
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
            return;
        }
    }

    internal CborWriteResult CreateIncompleteResult()
    {
        CborTokenWriterFrame frame = Frames[Frames.Count - 1];
        if (frame.Kind == CborTokenKind.Tag)
        {
            return CreateStateErrorResult(
                "CBOR_TAG_CONTENT_MISSING",
                "A semantic tag must be followed by one complete value."
            );
        }
        if (frame.IsIndefinite)
        {
            if (frame.Kind == CborTokenKind.StartMap && frame.MapExpectsValue)
            {
                return CreateStateErrorResult(
                    "CBOR_MAP_VALUE_MISSING",
                    "The indefinite map has a key without a value."
                );
            }
            return CreateStateErrorResult(
                "CBOR_INDEFINITE_END_MISSING",
                "An indefinite-length item has not received its break token."
            );
        }
        if (frame.Kind == CborTokenKind.StartArray && frame.RemainingCount > 0)
        {
            return CreateStateErrorResult(
                "CBOR_ARRAY_ITEM_COUNT_MISMATCH",
                "The definite array does not contain its declared number of items."
            );
        }
        if (frame.Kind == CborTokenKind.StartMap && frame.MapExpectsValue)
        {
            return CreateStateErrorResult(
                "CBOR_MAP_VALUE_MISSING",
                "The definite map has a key without a value."
            );
        }
        if (frame.Kind == CborTokenKind.StartMap && frame.RemainingCount > 0)
        {
            return CreateStateErrorResult(
                "CBOR_MAP_PAIR_COUNT_MISMATCH",
                "The definite map does not contain its declared number of pairs."
            );
        }
        return CreateStateErrorResult(
            "CBOR_CONTAINER_END_MISSING",
            "A definite container has not received its matching end token."
        );
    }

    internal CborWriteResult CreateStateErrorResult(
        string errorCode,
        string errorMessage)
    {
        Faulted = true;
        return CreateErrorResult(errorCode, errorMessage, 0);
    }

    internal static CborWriteResult CreateFaultedResult()
    {
        return CreateErrorResult(
            "CBOR_WRITER_FAULTED",
            "The token writer is faulted and must be reset before reuse.",
            0
        );
    }

    internal static CborWriteResult CreateDoneResult(int written)
    {
        CborWriteResult result = new CborWriteResult();
        result.Status = CborWriteStatus.Done;
        result.Written = written;
        result.Required = written;
        return result;
    }

    internal static CborWriteResult CreateDestinationTooSmallResult(int required)
    {
        CborWriteResult result = new CborWriteResult();
        result.Status = CborWriteStatus.DestinationTooSmall;
        result.Required = required;
        result.ErrorCode = "CBOR_DESTINATION_TOO_SMALL";
        result.ErrorMessage = "The destination is too small for the encoded token.";
        result.ErrorPath = "$";
        return result;
    }

    internal static CborWriteResult CreateErrorResult(
        string errorCode,
        string errorMessage,
        int required)
    {
        CborWriteResult result = new CborWriteResult();
        result.Status = CborWriteStatus.Error;
        result.Required = required;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.ErrorPath = "$";
        return result;
    }
}
