namespace asn1;

internal class PerOpenTypeCodec
{
    internal AsnResourceLimits Limits;
    internal PerLengthCodec Lengths;

    internal PerOpenTypeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Lengths = new PerLengthCodec(limits);
    }

    internal PerOpenTypeResult CompleteEncoding(
        byte[] data,
        int bitCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (bitCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        int requiredBytes = bitCount / 8;
        if (bitCount % 8 != 0)
        {
            requiredBytes = requiredBytes + 1;
        }
        if (requiredBytes > data.Length)
        {
            return this.CreateError(
                "ASNP0053",
                "The open-type source does not contain its declared bits.",
                false);
        }

        int outputBytes = requiredBytes == 0 ? 1 : requiredBytes;
        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(outputBytes)
            || !new ResourceCounter(this.Limits).TryChargeAllocationBytes(
                outputBytes))
        {
            return this.CreateError(
                "ASNR0006",
                "The complete open-type encoding exceeds the configured limits.",
                true);
        }

        byte[] complete = new byte[outputBytes];
        if (requiredBytes > 0)
        {
            System.Buffer.BlockCopy(data, 0, complete, 0, requiredBytes);
            int remainder = bitCount % 8;
            if (remainder != 0)
            {
                int mask = 255 << (8 - remainder);
                complete[requiredBytes - 1] = (byte)(
                    complete[requiredBytes - 1] & mask);
            }
        }

        return new PerOpenTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = complete,
            ContentBitCount = bitCount,
            PaddingBitCount = outputBytes * 8 - bitCount
        };
    }

    internal PerOpenTypeResult WriteOpenType(
        PerBitWriter writer,
        byte[] data,
        int bitCount,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        PerOpenTypeResult complete = this.CompleteEncoding(data, bitCount);
        if (!complete.Success)
        {
            return complete;
        }

        int start = writer.GetWrittenBitCount();
        PerLengthResult written = this.Lengths.WriteFragmentedOctets(
            writer,
            complete.Data,
            0,
            complete.Data.Length,
            aligned);
        if (!written.Success)
        {
            return this.CopyLengthFailure(written);
        }

        complete.BitCount = writer.GetWrittenBitCount() - start;
        complete.AlignmentPaddingBitCount = written.PaddingBitCount;
        return complete;
    }

    internal PerOpenTypeResult ReadOpenType(
        PerBitReader reader,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        int start = reader.Position;
        PerLengthResult content = this.Lengths.ReadFragmentedOctets(
            reader,
            aligned);
        if (!content.Success)
        {
            return this.CopyLengthFailure(content);
        }

        if (content.Data.Length == 0)
        {
            reader.CreateError(
                "ASNP0054",
                "A PER open type must contain a complete encoding of at least one octet.",
                false,
                true,
                start);
            return this.CopyReaderFailure(reader);
        }

        return new PerOpenTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = content.Data,
            BitCount = reader.Position - start,
            ContentBitCount = content.Data.Length * 8,
            AlignmentPaddingBitCount = content.PaddingBitCount
        };
    }

    internal PerOpenTypeResult CopyLengthFailure(PerLengthResult source)
    {
        return new PerOpenTypeResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerOpenTypeResult CopyReaderFailure(PerBitReader reader)
    {
        return new PerOpenTypeResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = reader.LimitExceeded,
            ErrorCode = reader.ErrorCode,
            ErrorMessage = reader.ErrorMessage
        };
    }

    internal PerOpenTypeResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerOpenTypeResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
