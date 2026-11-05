namespace asn1;

internal class OerContentCodec
{
    internal AsnResourceLimits Limits;
    internal ResourceCounter Resources;

    internal OerContentCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Resources = new ResourceCounter(limits);
    }

    internal OerContentResult EncodeVariableContent(byte[] content)
    {
        if (content == null)
        {
            throw new System.ArgumentNullException("content");
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
            content.Length))
        {
            return this.CreateContentError(
                "ASNR0020",
                "The OER content exceeds the configured byte limit.",
                true);
        }

        OerLengthResult length = new OerLengthCodec(this.Limits).WriteLength(
            content.Length);
        if (!length.Success)
        {
            return this.CopyLengthFailure(length);
        }

        if (content.Length > int.MaxValue - length.Data.Length)
        {
            return this.CreateContentError(
                "ASNR0021",
                "The OER value exceeds the supported array range.",
                true);
        }

        int total = length.Data.Length + content.Length;
        if (!this.Resources.TryChargeAllocationBytes(total))
        {
            return this.CreateContentError(
                "ASNR0005",
                "Allocating the OER value exceeds the configured limit.",
                true);
        }

        byte[] data = new byte[total];
        System.Buffer.BlockCopy(
            length.Data,
            0,
            data,
            0,
            length.Data.Length);
        System.Buffer.BlockCopy(
            content,
            0,
            data,
            length.Data.Length,
            content.Length);
        return new OerContentResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Content = content,
            Data = data,
            ConsumedCount = total
        };
    }

    internal OerContentResult DecodeVariableContent(
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        OerLengthResult length = new OerLengthCodec(this.Limits).ConsumeLength(
            data,
            offset,
            count,
            true,
            canonical);
        if (!length.Success)
        {
            return this.CopyLengthFailure(length);
        }

        if (length.Length > int.MaxValue)
        {
            return this.CreateContentErrorAt(
                "ASNR0021",
                "The OER content length exceeds the supported array range.",
                true,
                0);
        }

        int contentCount = (int)length.Length;
        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
                contentCount))
        {
            return this.CreateContentErrorAt(
                "ASNR0020",
                "The decoded OER content exceeds the configured limits.",
                true,
                0);
        }

        if (contentCount > count - length.ConsumedCount)
        {
            return new OerContentResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = count,
                HasViolationOffset = true,
                ViolationOffset = count,
                Minimal = length.Minimal
            };
        }

        if (!this.Resources.TryChargeAllocationBytes(contentCount))
        {
            return this.CreateContentErrorAt(
                "ASNR0020",
                "The decoded OER content exceeds the configured limits.",
                true,
                0);
        }

        byte[] content = new byte[contentCount];
        System.Buffer.BlockCopy(
            data,
            offset + length.ConsumedCount,
            content,
            0,
            contentCount);
        int consumed = length.ConsumedCount + contentCount;
        byte[] encoded = new byte[consumed];
        System.Buffer.BlockCopy(data, offset, encoded, 0, consumed);
        return new OerContentResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Content = content,
            Data = encoded,
            ConsumedCount = consumed,
            Minimal = length.Minimal
        };
    }

    internal OerContentResult EncodeFixedContent(
        byte[] content,
        int expectedCount)
    {
        if (content == null)
        {
            throw new System.ArgumentNullException("content");
        }

        if (content.Length != expectedCount)
        {
            return this.CreateContentError(
                "ASNO0020",
                "The OER content does not match its fixed size.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
                content.Length)
            || !this.Resources.TryChargeAllocationBytes(content.Length))
        {
            return this.CreateContentError(
                "ASNR0020",
                "The fixed OER content exceeds the configured limits.",
                true);
        }

        return new OerContentResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Content = content,
            Data = content,
            ConsumedCount = content.Length
        };
    }

    internal OerContentResult DecodeFixedContent(
        byte[] data,
        int offset,
        int count,
        int expectedCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (expectedCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("expectedCount");
        }

        if (count < expectedCount)
        {
            return new OerContentResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = count,
                HasViolationOffset = true,
                ViolationOffset = count
            };
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
                expectedCount)
            || !this.Resources.TryChargeAllocationBytes(expectedCount))
        {
            return this.CreateContentError(
                "ASNR0020",
                "The fixed OER content exceeds the configured limits.",
                true);
        }

        byte[] content = new byte[expectedCount];
        System.Buffer.BlockCopy(data, offset, content, 0, expectedCount);
        return new OerContentResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Content = content,
            Data = content,
            ConsumedCount = expectedCount
        };
    }

    internal OerContentResult CopyLengthFailure(OerLengthResult length)
    {
        return new OerContentResult
        {
            Status = length.Status,
            ConsumedCount = length.ConsumedCount,
            Minimal = length.Minimal,
            HasViolationOffset = length.HasViolationOffset,
            ViolationOffset = length.ViolationOffset,
            LimitExceeded = length.LimitExceeded,
            ErrorCode = length.ErrorCode,
            ErrorMessage = length.ErrorMessage
        };
    }

    internal OerContentResult CreateContentError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerContentResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal OerContentResult CreateContentErrorAt(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset)
    {
        OerContentResult result = this.CreateContentError(
            code,
            message,
            limitExceeded);
        result.HasViolationOffset = true;
        result.ViolationOffset = violationOffset;
        return result;
    }
}
