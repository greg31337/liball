using System;
using System.Text;

namespace cbor;

internal class CborUtf8
{
    internal StringBuilder Builder = new StringBuilder();
    internal int ExpectedContinuationBytes;
    internal uint CodePoint;
    internal uint MinimumCodePoint;
    internal long SequenceStartOffset;
    internal long TotalBytes;
    internal bool Completed;
    internal bool Faulted;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal long ErrorOffset = -1;

    internal CborUtf8Result ProcessChunk(ReadOnlySpan<byte> chunk, bool isFinal)
    {
        if (Faulted)
        {
            return CreateResult(CborReadStatus.Error, 0);
        }
        if (Completed)
        {
            return FailResult(
                "CBOR_UTF8_ALREADY_COMPLETED",
                "The UTF-8 operation is complete and must be reset before more input is supplied.",
                TotalBytes,
                0
            );
        }
        if (TotalBytes > long.MaxValue - chunk.Length)
        {
            return FailResult(
                "CBOR_UTF8_OFFSET_OVERFLOW",
                "The absolute UTF-8 byte offset exceeds the supported range.",
                TotalBytes,
                0
            );
        }

        long chunkStartOffset = TotalBytes;
        for (int index = 0; index < chunk.Length; index++)
        {
            byte currentByte = chunk[index];
            long absoluteOffset = chunkStartOffset + index;
            if (ExpectedContinuationBytes == 0)
            {
                if (currentByte <= 0x7f)
                {
                    Builder.Append((char)currentByte);
                    continue;
                }
                SequenceStartOffset = absoluteOffset;
                if (currentByte >= 0xc2 && currentByte <= 0xdf)
                {
                    ExpectedContinuationBytes = 1;
                    CodePoint = (uint)currentByte & 0x1fU;
                    MinimumCodePoint = 0x80U;
                    continue;
                }
                if (currentByte >= 0xe0 && currentByte <= 0xef)
                {
                    ExpectedContinuationBytes = 2;
                    CodePoint = (uint)currentByte & 0x0fU;
                    MinimumCodePoint = 0x800U;
                    continue;
                }
                if (currentByte >= 0xf0 && currentByte <= 0xf4)
                {
                    ExpectedContinuationBytes = 3;
                    CodePoint = (uint)currentByte & 0x07U;
                    MinimumCodePoint = 0x10000U;
                    continue;
                }

                TotalBytes = absoluteOffset + 1;
                if (currentByte == 0xc0 || currentByte == 0xc1)
                {
                    return FailResult(
                        "CBOR_UTF8_NON_SHORTEST",
                        "The UTF-8 sequence uses a non-shortest form.",
                        absoluteOffset,
                        index + 1
                    );
                }
                if (currentByte >= 0xf5)
                {
                    return FailResult(
                        "CBOR_UTF8_OUT_OF_RANGE",
                        "The UTF-8 leading byte cannot encode a Unicode scalar value.",
                        absoluteOffset,
                        index + 1
                    );
                }
                return FailResult(
                    "CBOR_UTF8_INVALID_LEADING_BYTE",
                    "The byte is not a valid UTF-8 leading byte.",
                    absoluteOffset,
                    index + 1
                );
            }

            if (((uint)currentByte & 0xc0U) != 0x80U)
            {
                TotalBytes = absoluteOffset + 1;
                return FailResult(
                    "CBOR_UTF8_INVALID_CONTINUATION",
                    "A UTF-8 continuation byte was expected.",
                    absoluteOffset,
                    index + 1
                );
            }

            CodePoint = (CodePoint << 6) | ((uint)currentByte & 0x3fU);
            ExpectedContinuationBytes = ExpectedContinuationBytes - 1;
            if (ExpectedContinuationBytes == 0)
            {
                if (CodePoint < MinimumCodePoint)
                {
                    TotalBytes = absoluteOffset + 1;
                    return FailResult(
                        "CBOR_UTF8_NON_SHORTEST",
                        "The UTF-8 sequence uses a non-shortest form.",
                        SequenceStartOffset,
                        index + 1
                    );
                }
                if (CodePoint >= 0xd800U && CodePoint <= 0xdfffU)
                {
                    TotalBytes = absoluteOffset + 1;
                    return FailResult(
                        "CBOR_UTF8_SURROGATE",
                        "UTF-8 cannot encode a UTF-16 surrogate code point.",
                        SequenceStartOffset,
                        index + 1
                    );
                }
                if (CodePoint > 0x10ffffU)
                {
                    TotalBytes = absoluteOffset + 1;
                    return FailResult(
                        "CBOR_UTF8_OUT_OF_RANGE",
                        "The UTF-8 sequence exceeds U+10FFFF.",
                        SequenceStartOffset,
                        index + 1
                    );
                }
                AppendCodePoint();
            }
        }

        TotalBytes = chunkStartOffset + chunk.Length;
        if (isFinal)
        {
            if (ExpectedContinuationBytes != 0)
            {
                return FailResult(
                    "CBOR_UTF8_INCOMPLETE_SEQUENCE",
                    "The final UTF-8 input ends inside a multi-byte sequence.",
                    SequenceStartOffset,
                    chunk.Length
                );
            }
            Completed = true;
            return CreateResult(CborReadStatus.Done, chunk.Length);
        }
        return CreateResult(CborReadStatus.NeedMoreData, chunk.Length);
    }

    internal void Reset()
    {
        Builder.Clear();
        ExpectedContinuationBytes = 0;
        CodePoint = 0;
        MinimumCodePoint = 0;
        SequenceStartOffset = 0;
        TotalBytes = 0;
        Completed = false;
        Faulted = false;
        ErrorCode = null;
        ErrorMessage = null;
        ErrorOffset = -1;
    }

    internal void AppendCodePoint()
    {
        if (CodePoint <= 0xffffU)
        {
            Builder.Append((char)CodePoint);
            return;
        }
        uint remainder = CodePoint - 0x10000U;
        char highSurrogate = (char)(0xd800U + (remainder >> 10));
        char lowSurrogate = (char)(0xdc00U + (remainder & 0x03ffU));
        Builder.Append(highSurrogate);
        Builder.Append(lowSurrogate);
    }

    internal CborUtf8Result FailResult(string errorCode, string errorMessage, long errorOffset, int consumed)
    {
        Faulted = true;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorOffset = errorOffset;
        return CreateResult(CborReadStatus.Error, consumed);
    }

    internal CborUtf8Result CreateResult(CborReadStatus status, int consumed)
    {
        CborUtf8Result result = new CborUtf8Result();
        result.Status = status;
        result.Text = Builder.ToString();
        result.Consumed = consumed;
        result.TotalBytes = TotalBytes;
        result.ErrorCode = ErrorCode;
        result.ErrorMessage = ErrorMessage;
        result.ErrorOffset = ErrorOffset;
        return result;
    }
}
