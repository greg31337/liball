namespace asn1;

internal class BerLengthWriter
{
    internal ResourceCounter Resources;

    internal BerLengthWriter(AsnResourceLimits limits)
    {
        this.Resources = new ResourceCounter(limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits);
    }

    internal BerLengthWriteResult WriteLength(long length, bool indefinite)
    {
        if (indefinite)
        {
            if (!this.Resources.AllowsLengthOctets(1))
            {
                return this.CreateLengthLimitResult();
            }

            return new BerLengthWriteResult
            {
                Success = true,
                Data = [128],
                WrittenCount = 1
            };
        }

        if (length < 0)
        {
            return new BerLengthWriteResult
            {
                ErrorCode = "ASNE0003",
                ErrorMessage = "A definite length must be non-negative."
            };
        }

        if (length < 128)
        {
            if (!this.Resources.AllowsLengthOctets(1))
            {
                return this.CreateLengthLimitResult();
            }

            return new BerLengthWriteResult
            {
                Success = true,
                Data = [(byte)length],
                WrittenCount = 1
            };
        }

        long remaining = length;
        int valueOctets = 0;
        while (remaining > 0)
        {
            valueOctets = valueOctets + 1;
            remaining = remaining >> 8;
        }

        int totalOctets = valueOctets + 1;
        if (!this.Resources.AllowsLengthOctets(totalOctets))
        {
            return this.CreateLengthLimitResult();
        }

        byte[] data = new byte[totalOctets];
        data[0] = (byte)(128 | valueOctets);
        remaining = length;
        int index = data.Length - 1;
        while (index > 0)
        {
            data[index] = (byte)(remaining & 255);
            remaining = remaining >> 8;
            index = index - 1;
        }

        return new BerLengthWriteResult
        {
            Success = true,
            Data = data,
            WrittenCount = data.Length
        };
    }

    internal BerLengthWriteResult WriteEndOfContents()
    {
        if (!this.Resources.AllowsTagOctets(1)
            || !this.Resources.AllowsLengthOctets(1))
        {
            return new BerLengthWriteResult
            {
                LimitExceeded = true,
                ErrorCode = "ASNR0003",
                ErrorMessage = "The end-of-contents marker exceeds the configured identifier or length limit."
            };
        }

        return new BerLengthWriteResult
        {
            Success = true,
            Data = [0, 0],
            WrittenCount = 2
        };
    }

    internal BerLengthWriteResult CreateLengthLimitResult()
    {
        return new BerLengthWriteResult
        {
            LimitExceeded = true,
            ErrorCode = "ASNR0003",
            ErrorMessage = "The length determinant exceeds the configured length-octet limit."
        };
    }
}
