using System;

namespace cbor;

/// <summary>
/// Reads back-to-back CBOR values from a stream.
/// </summary>
public class CborStreamReader : ICborStreamReader
{
    internal const int ReadChunkSize = 4096;
    internal System.IO.Stream Input;
    internal CborDecodeOptions Options;
    internal byte[] Buffer = new byte[ReadChunkSize];
    internal int BufferOffset;
    internal int BufferCount;
    internal long AbsoluteConsumed;
    internal int ItemConsumed;
    internal bool EndOfInput;
    internal bool Active;
    internal bool Faulted;
    internal CborTokenizerCore? Tokenizer;
    internal CborTagLookup? Tags;
    internal CborDomBuilder Builder = new CborDomBuilder();
    internal CborSequenceState Sequence;

    /// <summary>Creates a CBOR stream reader.</summary>
    /// <param name="input">The readable source stream.</param>
    /// <param name="options">The decoding policies and limits.</param>
    public CborStreamReader(System.IO.Stream input, CborDecodeOptions options)
    {
        Input = input;
        Options = options;
        Sequence = new CborSequenceState(
            options == null ? -1 : options.MaxSequenceItems,
            options == null ? -1 : options.MaxSequenceBytes
        );
    }

    /// <summary>Reads the next complete CBOR value from the stream.</summary>
    /// <returns>The read outcome and decoded value.</returns>
    public CborReadResult ReadNext()
    {
        if (Faulted)
        {
            return CreateFailure(
                "CBOR_STREAM_READER_FAULTED",
                "The stream reader cannot continue after a decoding failure.",
                AbsoluteConsumed
            );
        }
        if (Input == null || Options == null)
        {
            Faulted = true;
            return CreateFailure(
                "CBOR_STREAM_READER_CONFIGURATION_INVALID",
                "The stream reader requires a readable stream and decoding options.",
                AbsoluteConsumed
            );
        }
        if (!Input.CanRead)
        {
            Faulted = true;
            return CreateFailure(
                "CBOR_STREAM_NOT_READABLE",
                "The source stream does not support reading.",
                AbsoluteConsumed
            );
        }
        if (!Sequence.ValidateEmpty())
        {
            Faulted = true;
            return CreateSequenceStateFailure();
        }

        if (!Active)
        {
            Tags = new CborTagLookup(Options.TagRegistrations);
            if (Tags.ErrorCode != null)
            {
                Faulted = true;
                return CreateFailure(
                    Tags.ErrorCode,
                    Tags.ErrorMessage ?? "The tag registrations are invalid.",
                    AbsoluteConsumed
                );
            }
            if (BufferCount == 0 && !EndOfInput)
            {
                ReadMore();
            }
            if (BufferCount == 0)
            {
                return CreateEndOfData();
            }
            if (!Sequence.CanStartItem())
            {
                Faulted = true;
                return CreateSequenceStateFailure();
            }
            BeginItem();
        }

        while (true)
        {
            if (ItemConsumed == 0 && BufferCount == 0)
            {
                if (!ReadMore())
                {
                    Active = false;
                    return CreateEndOfData();
                }
            }

            if (Tokenizer == null)
            {
                Faulted = true;
                Active = false;
                return CreateFailure(
                    "CBOR_STREAM_READER_STATE_INVALID",
                    "The stream reader has no active tokenizer.",
                    AbsoluteConsumed
                );
            }

            byte[] available = Buffer.AsSpan(BufferOffset, BufferCount).ToArray();
            CborTokenReadResult tokenResult = Tokenizer.ReadToken(available);
            if (!ConsumeBufferedBytes(tokenResult.Consumed))
            {
                Faulted = true;
                Active = false;
                return CreateFailure(
                    "CBOR_STREAM_OFFSET_OVERFLOW",
                    "The consumed stream offset exceeds the supported range.",
                    AbsoluteConsumed
                );
            }
            if (tokenResult.Status == CborReadStatus.Error)
            {
                Faulted = true;
                Active = false;
                return CreateTokenFailure(tokenResult);
            }
            if (tokenResult.Status == CborReadStatus.NeedMoreData)
            {
                if (!ReadMore())
                {
                    Faulted = true;
                    Active = false;
                    return CreateFailure(
                        "CBOR_UNEXPECTED_END_OF_DATA",
                        "The encoded input ended before one complete CBOR value was available.",
                        AbsoluteConsumed
                    );
                }
                continue;
            }
            if (tokenResult.Token == null)
            {
                Faulted = true;
                Active = false;
                return CreateFailure(
                    "CBOR_DECODE_TOKEN_MISSING",
                    "The tokenizer completed without producing a token.",
                    AbsoluteConsumed
                );
            }
            if (!Builder.AddToken(tokenResult.Token))
            {
                Faulted = true;
                Active = false;
                return CreateFailure(
                    Builder.ErrorCode ?? "CBOR_DOM_BUILD_FAILED",
                    Builder.ErrorMessage ?? "The decoded tokens could not form a CBOR value.",
                    AbsoluteConsumed
                );
            }
            if (Builder.Completed)
            {
                return CompleteItem();
            }
        }
    }

    internal void BeginItem()
    {
        Builder.Reset();
        Tokenizer = new CborTokenizerCore(Options, null);
        Tokenizer.AbsoluteOffset = AbsoluteConsumed;
        ItemConsumed = 0;
        Active = true;
    }

    internal bool ReadMore()
    {
        if (EndOfInput)
        {
            return false;
        }
        EnsureWritableBuffer();
        int destinationOffset = BufferOffset + BufferCount;
        int writable = Buffer.Length - destinationOffset;
        if (writable > ReadChunkSize)
        {
            writable = ReadChunkSize;
        }
        int read = Input.Read(Buffer, destinationOffset, writable);
        if (read == 0)
        {
            EndOfInput = true;
            return false;
        }
        BufferCount = BufferCount + read;
        return true;
    }

    internal void EnsureWritableBuffer()
    {
        int writable = Buffer.Length - BufferOffset - BufferCount;
        if (writable >= ReadChunkSize)
        {
            return;
        }
        if (BufferOffset > 0)
        {
            Buffer.AsSpan(BufferOffset, BufferCount).CopyTo(Buffer);
            BufferOffset = 0;
            writable = Buffer.Length - BufferCount;
            if (writable >= ReadChunkSize)
            {
                return;
            }
        }
        int required = BufferCount + ReadChunkSize;
        int capacity = Buffer.Length;
        while (capacity < required)
        {
            if (capacity > int.MaxValue / 2)
            {
                capacity = required;
                break;
            }
            capacity = capacity * 2;
        }
        byte[] replacement = new byte[capacity];
        Buffer.AsSpan(BufferOffset, BufferCount).CopyTo(replacement);
        Buffer = replacement;
        BufferOffset = 0;
    }

    internal bool ConsumeBufferedBytes(int count)
    {
        if (count < 0 || count > BufferCount)
        {
            return false;
        }
        if (ItemConsumed > int.MaxValue - count
            || AbsoluteConsumed > long.MaxValue - count)
        {
            return false;
        }
        BufferOffset = BufferOffset + count;
        BufferCount = BufferCount - count;
        ItemConsumed = ItemConsumed + count;
        AbsoluteConsumed = AbsoluteConsumed + count;
        if (BufferCount == 0)
        {
            BufferOffset = 0;
        }
        return true;
    }

    internal CborReadResult CompleteItem()
    {
        if (Builder.Value == null)
        {
            Faulted = true;
            Active = false;
            return CreateFailure(
                "CBOR_DOM_VALUE_MISSING",
                "The DOM builder completed without producing a value.",
                AbsoluteConsumed
            );
        }
        if (Tags == null)
        {
            Faulted = true;
            Active = false;
            return CreateFailure(
                "CBOR_TAG_LOOKUP_MISSING",
                "The decoding operation has no tag-registration snapshot.",
                AbsoluteConsumed
            );
        }
        CborSharingDecoder sharingDecoder = new CborSharingDecoder(Options, Tags);
        if (!sharingDecoder.Process(Builder.Value))
        {
            Faulted = true;
            Active = false;
            return CreateFailureWithPath(
                sharingDecoder.ErrorCode ?? "CBOR_REFERENCE_PROCESSING_FAILED",
                sharingDecoder.ErrorMessage ?? "The reference tags could not be processed.",
                sharingDecoder.ErrorOffset,
                sharingDecoder.ErrorPath
            );
        }
        if (!Sequence.AddItem(ItemConsumed))
        {
            Faulted = true;
            Active = false;
            return CreateSequenceStateFailure();
        }

        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Done;
        result.Value = sharingDecoder.Result;
        result.Consumed = ItemConsumed;
        result.Remaining = BufferCount;
        result.ItemIndex = Sequence.ItemCount - 1;
        result.SourceOffset = AbsoluteConsumed - ItemConsumed;
        Active = false;
        Tokenizer = null;
        Tags = null;
        ItemConsumed = 0;
        return result;
    }

    internal CborReadResult CreateTokenFailure(CborTokenReadResult tokenResult)
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.Consumed = ItemConsumed;
        result.Remaining = BufferCount;
        result.ItemIndex = Sequence.ItemCount;
        result.SourceOffset = AbsoluteConsumed - ItemConsumed;
        result.ErrorCode = tokenResult.ErrorCode;
        result.ErrorMessage = tokenResult.ErrorMessage;
        result.ErrorOffset = tokenResult.ErrorOffset;
        result.ErrorPath = tokenResult.ErrorPath;
        return result;
    }

    internal CborReadResult CreateFailure(
        string errorCode,
        string errorMessage,
        long errorOffset)
    {
        return CreateFailureWithPath(errorCode, errorMessage, errorOffset, "$");
    }

    internal CborReadResult CreateFailureWithPath(
        string errorCode,
        string errorMessage,
        long errorOffset,
        string? errorPath)
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.Consumed = ItemConsumed;
        result.Remaining = BufferCount;
        result.ItemIndex = Sequence.ItemCount;
        result.SourceOffset = AbsoluteConsumed - ItemConsumed;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.ErrorOffset = errorOffset;
        result.ErrorPath = errorPath;
        return result;
    }

    internal CborReadResult CreateSequenceStateFailure()
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.Consumed = ItemConsumed;
        result.Remaining = BufferCount;
        result.ItemIndex = Sequence.ItemCount;
        result.SourceOffset = AbsoluteConsumed - ItemConsumed;
        result.ErrorCode = Sequence.ErrorCode ?? "CBOR_SEQUENCE_LIMIT_EXCEEDED";
        result.ErrorMessage = Sequence.ErrorMessage
            ?? "The CBOR Sequence exceeds its configured limits.";
        result.ErrorOffset = result.SourceOffset;
        result.ErrorPath = "$";
        return result;
    }

    internal CborReadResult CreateEndOfData()
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.EndOfData;
        result.Remaining = BufferCount;
        return result;
    }
}
