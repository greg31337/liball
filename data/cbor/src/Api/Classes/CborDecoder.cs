using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Provides high-level CBOR decoding operations.
/// </summary>
public class CborDecoder : ICborDecoder
{
    internal CborDecoderCore Core;

    /// <summary>Creates a CBOR decoder.</summary>
    public CborDecoder()
    {
        Core = new CborDecoderCore();
    }

    /// <summary>Decodes exactly one CBOR data item.</summary>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <param name="options">The decoding policies and limits.</param>
    /// <returns>The read outcome and decoded value.</returns>
    public CborReadResult Decode(byte[] source, CborDecodeOptions options)
    {
        CborDecoderCore operation = new CborDecoderCore();
        CborReadResult result = operation.DecodeValue(
            source,
            options,
            options.AllowTrailingData
        );
        Core = operation;
        return result;
    }

    /// <summary>Decodes the first CBOR data item and reports its consumption.</summary>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <param name="options">The decoding policies and limits.</param>
    /// <returns>The read outcome, decoded value, and consumed byte count.</returns>
    public CborReadResult DecodeFirst(byte[] source, CborDecodeOptions options)
    {
        CborDecoderCore operation = new CborDecoderCore();
        CborReadResult result = operation.DecodeValue(source, options, true);
        Core = operation;
        return result;
    }

    /// <summary>Decodes all concatenated items in an RFC 8742 CBOR Sequence.</summary>
    /// <param name="source">The encoded CBOR Sequence bytes.</param>
    /// <param name="options">The decoding policies and sequence limits.</param>
    /// <returns>One result per decoded item, or a final error result for an invalid item or limit.</returns>
    public List<CborReadResult> DecodeSequence(byte[] source, CborDecodeOptions options)
    {
        List<CborReadResult> results = new List<CborReadResult>();
        if (options == null)
        {
            results.Add(CreateSequenceFailure(
                "CBOR_DECODE_OPTIONS_MISSING",
                "Decoding options are required.",
                0,
                0,
                0
            ));
            return results;
        }
        if (source == null)
        {
            results.Add(CreateSequenceFailure(
                "CBOR_SOURCE_MISSING",
                "Encoded CBOR Sequence bytes are required.",
                0,
                0,
                0
            ));
            return results;
        }

        CborSequenceState state = new CborSequenceState(
            options.MaxSequenceItems,
            options.MaxSequenceBytes
        );
        if (!state.ValidateEmpty())
        {
            results.Add(CreateSequenceFailure(
                state.ErrorCode ?? "CBOR_SEQUENCE_LIMIT_INVALID",
                state.ErrorMessage ?? "The CBOR Sequence limits are invalid.",
                0,
                0,
                source.Length
            ));
            return results;
        }

        int sourceOffset = 0;
        CborStreamReader reader = new CborStreamReader(
            new System.IO.MemoryStream(source, false),
            options
        );
        reader.Sequence = new CborSequenceState(int.MaxValue, long.MaxValue);
        while (sourceOffset < source.Length)
        {
            if (!state.CanStartItem())
            {
                results.Add(CreateSequenceFailure(
                    state.ErrorCode ?? "CBOR_SEQUENCE_LIMIT_EXCEEDED",
                    state.ErrorMessage ?? "The CBOR Sequence exceeds its configured limits.",
                    state.ItemCount,
                    sourceOffset,
                    source.Length - sourceOffset
                ));
                return results;
            }

            CborReadResult item = reader.ReadNext();
            item.ItemIndex = state.ItemCount;
            item.SourceOffset = sourceOffset;
            if (item.Status != CborReadStatus.Done)
            {
                item.Consumed = 0;
                item.Remaining = source.Length - sourceOffset;
                results.Add(item);
                return results;
            }
            if (!state.AddItem(item.Consumed))
            {
                results.Add(CreateSequenceFailure(
                    state.ErrorCode ?? "CBOR_SEQUENCE_LIMIT_EXCEEDED",
                    state.ErrorMessage ?? "The CBOR Sequence exceeds its configured limits.",
                    state.ItemCount,
                    sourceOffset,
                    source.Length - sourceOffset
                ));
                return results;
            }
            item.Remaining = source.Length - sourceOffset - item.Consumed;
            results.Add(item);
            sourceOffset = sourceOffset + item.Consumed;
        }
        return results;
    }

    internal static CborReadResult CreateSequenceFailure(
        string errorCode,
        string errorMessage,
        int itemIndex,
        long sourceOffset,
        int remaining)
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.ItemIndex = itemIndex;
        result.SourceOffset = sourceOffset;
        result.Remaining = remaining;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.ErrorOffset = sourceOffset;
        result.ErrorPath = "$";
        return result;
    }
}
