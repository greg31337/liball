using System;
using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Provides high-level CBOR encoding operations.
/// </summary>
public class CborEncoder : ICborEncoder
{
    /// <summary>Creates a CBOR encoder.</summary>
    public CborEncoder()
    {
    }

    /// <summary>Encodes a value into a new byte array.</summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="options">The encoding policies.</param>
    /// <returns>The encoded CBOR bytes.</returns>
    public byte[] Encode(CborValue value, CborEncodeOptions options)
    {
        if (options == null)
        {
            throw new CborEncodeException(
                "CBOR_ENCODE_OPTIONS_MISSING",
                "Encoding options are required.",
                -1,
                "$"
            );
        }
        CborEncoderCore core = new CborEncoderCore(options);
        return core.EncodeValue(value);
    }

    /// <summary>Encodes a value into a caller-provided destination.</summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The destination byte array.</param>
    /// <param name="options">The encoding policies.</param>
    /// <returns>The write outcome and size information.</returns>
    public CborWriteResult EncodeInto(CborValue value, byte[] destination, CborEncodeOptions options)
    {
        if (options == null)
        {
            CborWriteResult result = new CborWriteResult();
            result.Status = CborWriteStatus.Error;
            result.ErrorCode = "CBOR_ENCODE_OPTIONS_MISSING";
            result.ErrorMessage = "Encoding options are required.";
            result.ErrorPath = "$";
            return result;
        }
        CborEncoderCore core = new CborEncoderCore(options);
        return core.EncodeValueInto(value, destination);
    }

    /// <summary>Measures the encoded size of a value.</summary>
    /// <param name="value">The value to measure.</param>
    /// <param name="options">The encoding policies.</param>
    /// <returns>The required number of bytes.</returns>
    public int Measure(CborValue value, CborEncodeOptions options)
    {
        if (options == null)
        {
            throw new CborEncodeException(
                "CBOR_ENCODE_OPTIONS_MISSING",
                "Encoding options are required.",
                -1,
                "$"
            );
        }
        CborEncoderCore core = new CborEncoderCore(options);
        return core.MeasureValue(value);
    }

    /// <summary>Encodes zero or more values as an RFC 8742 CBOR Sequence.</summary>
    /// <param name="values">The values to concatenate in sequence order.</param>
    /// <param name="options">The encoding policies and sequence limits.</param>
    /// <returns>The concatenated encoded CBOR items.</returns>
    public byte[] EncodeSequence(List<CborValue> values, CborEncodeOptions options)
    {
        if (options == null)
        {
            throw new CborEncodeException(
                "CBOR_ENCODE_OPTIONS_MISSING",
                "Encoding options are required.",
                -1,
                "$"
            );
        }
        if (values == null)
        {
            throw new CborEncodeException(
                "CBOR_SEQUENCE_VALUES_MISSING",
                "A CBOR Sequence value collection is required.",
                -1,
                "$"
            );
        }

        CborSequenceState state = new CborSequenceState(
            options.MaxSequenceItems,
            options.MaxSequenceBytes
        );
        if (!state.ValidateEmpty())
        {
            throw CreateSequenceException(state, 0, "$", 0);
        }

        List<byte[]> encodings = new List<byte[]>();
        int index = 0;
        while (index < values.Count)
        {
            string itemPath = $"$[{index}]";
            if (!state.CanStartItem())
            {
                throw CreateSequenceException(state, state.ByteCount, itemPath, index);
            }

            CborSizeCalculator calculator = new CborSizeCalculator(options);
            if (!calculator.Calculate(values[index]))
            {
                throw new CborEncodeException(
                    calculator.ErrorCode ?? "CBOR_ENCODE_INVALID_VALUE",
                    calculator.ErrorMessage ?? "The CBOR Sequence item cannot be encoded.",
                    state.ByteCount,
                    CreateSequencePath(index, calculator.ErrorPath)
                );
            }
            byte[] encoded = new byte[(int)calculator.RequiredBytes];
            CborEncodingPlanWriter writer = new CborEncodingPlanWriter(options);
            if (!writer.WritePlan(calculator, encoded))
            {
                throw new CborEncodeException(
                    writer.ErrorCode ?? "CBOR_ENCODE_INTERNAL_FAILURE",
                    writer.ErrorMessage ?? "The CBOR Sequence item could not be written.",
                    state.ByteCount,
                    CreateSequencePath(index, writer.ErrorPath)
                );
            }
            if (state.ByteCount > int.MaxValue - encoded.Length)
            {
                throw new CborEncodeException(
                    "CBOR_SEQUENCE_SIZE_OUT_OF_RANGE",
                    "The encoded CBOR Sequence is too large for a byte-array result.",
                    state.ByteCount,
                    itemPath
                );
            }
            if (!state.AddItem(encoded.Length))
            {
                throw CreateSequenceException(state, state.ByteCount, itemPath, index);
            }
            encodings.Add(encoded);
            index = index + 1;
        }

        byte[] sequence = new byte[(int)state.ByteCount];
        int destinationOffset = 0;
        index = 0;
        while (index < encodings.Count)
        {
            byte[] encoded = encodings[index];
            encoded.AsSpan().CopyTo(sequence.AsSpan(destinationOffset));
            destinationOffset = destinationOffset + encoded.Length;
            index = index + 1;
        }
        return sequence;
    }

    internal static CborEncodeException CreateSequenceException(
        CborSequenceState state,
        long errorOffset,
        string errorPath,
        int itemIndex)
    {
        return new CborEncodeException(
            state.ErrorCode ?? "CBOR_SEQUENCE_LIMIT_EXCEEDED",
            state.ErrorMessage ?? $"CBOR Sequence item {itemIndex} exceeds a configured limit.",
            errorOffset,
            errorPath
        );
    }

    internal static string CreateSequencePath(int itemIndex, string? itemPath)
    {
        string prefix = $"$[{itemIndex}]";
        if (itemPath == null || itemPath == "$")
        {
            return prefix;
        }
        if (itemPath.Length > 0 && itemPath[0] == '$')
        {
            return prefix + itemPath.Substring(1);
        }
        return prefix;
    }
}
