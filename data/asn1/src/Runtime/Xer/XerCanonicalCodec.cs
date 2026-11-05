namespace asn1;

internal class XerCanonicalCodec
{
    internal AsnResourceLimits Limits;

    internal XerCanonicalCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal XerValueResult EncodeCanonical(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        XerValueCodec values = new XerValueCodec(this.Limits);
        values.Canonical = true;
        return values.EncodeValue(type, value);
    }

    internal XerValueResult DecodeCanonical(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        byte[] input = new byte[count];
        System.Buffer.BlockCopy(data, offset, input, 0, count);
        XerXmlReadResult read = new XerXmlTokenReader(this.Limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = input,
                IsFinalBlock = true
            });
        if (read.Status != AsnDecodeStatus.Done)
        {
            return new XerValueResult
            {
                Status = read.Status,
                ConsumedCount = read.ConsumedCount,
                LimitExceeded = read.LimitExceeded,
                HasViolationOffset = true,
                ViolationOffset = read.ViolationOffset,
                ValuePath = "$",
                ErrorCode = read.ErrorCode,
                ErrorMessage = read.ErrorMessage
            };
        }

        XerValueResult decoded = new XerValueCodec(this.Limits).DecodeValue(
            type,
            read.Tokens,
            0,
            read.Tokens.Length);
        if (!decoded.Success || decoded.Value == null)
        {
            return decoded;
        }

        if (decoded.ConsumedCount != read.Tokens.Length)
        {
            return this.CreateCanonicalError(
                "The canonical XER document contains trailing content.");
        }

        XerValueResult canonical = this.EncodeCanonical(
            type,
            decoded.Value);
        if (!canonical.Success)
        {
            return canonical;
        }

        byte[] expected = this.RenderTokens(canonical.Tokens);
        if (expected.Length == 0 || !this.BytesEqual(input, expected))
        {
            return this.CreateCanonicalDifferenceError(
                input,
                expected,
                "The XER document is valid BASIC-XER but is not in canonical form.");
        }

        decoded.Tokens = read.Tokens;
        decoded.ConsumedCount = count;
        return decoded;
    }

    internal byte[] RenderTokens(XerXmlToken[] tokens)
    {
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        XerXmlTokenWriter writer = new XerXmlTokenWriter(output, this.Limits);
        int index = 0;
        while (index < tokens.Length)
        {
            XerXmlWriteResult written = writer.WriteToken(tokens[index]);
            if (!written.Success)
            {
                return [];
            }

            index = index + 1;
        }

        XerXmlWriteResult complete = writer.CompleteWriter();
        return complete.Success ? output.ToArray() : [];
    }

    internal bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal XerValueResult CreateCanonicalError(string message)
    {
        return new XerValueResult
        {
            Status = AsnDecodeStatus.Error,
            ErrorCode = "ASNXER0200",
            ErrorMessage = message
        };
    }

    internal XerValueResult CreateCanonicalDifferenceError(
        byte[] input,
        byte[] expected,
        string message)
    {
        int sharedCount = input.Length < expected.Length
            ? input.Length
            : expected.Length;
        int offset = 0;
        while (offset < sharedCount
            && input[offset] == expected[offset])
        {
            offset = offset + 1;
        }

        return new XerValueResult
        {
            Status = AsnDecodeStatus.Error,
            HasViolationOffset = true,
            ViolationOffset = offset,
            ValuePath = "$",
            ErrorCode = "ASNXER0200",
            ErrorMessage = message
        };
    }
}
