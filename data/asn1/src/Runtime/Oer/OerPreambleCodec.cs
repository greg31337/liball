namespace asn1;

internal class OerPreambleCodec
{
    internal AsnResourceLimits Limits;

    internal OerPreambleCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerPreambleResult EncodeRootPreamble(
        bool extensible,
        bool extensionPresent,
        bool[] rootPresence)
    {
        if (rootPresence == null)
        {
            throw new System.ArgumentNullException("rootPresence");
        }

        if (!extensible && extensionPresent)
        {
            return this.CreatePreambleError(
                "ASNO0050",
                "A non-extensible OER collection cannot contain extension additions.",
                false);
        }

        int bitCount = rootPresence.Length + (extensible ? 1 : 0);
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            bitCount))
        {
            return this.CreatePreambleError(
                "ASNR0011",
                "The OER collection preamble exceeds the configured element limit.",
                true);
        }

        int octetCount = (bitCount + 7) / 8;
        if (!new ResourceCounter(this.Limits).TryChargeAllocationBytes(
            octetCount))
        {
            return this.CreatePreambleError(
                "ASNR0005",
                "Allocating the OER collection preamble exceeds the configured limit.",
                true);
        }

        byte[] data = new byte[octetCount];
        int bitIndex = 0;
        if (extensible)
        {
            this.SetBit(data, bitIndex, extensionPresent);
            bitIndex = bitIndex + 1;
        }

        int index = 0;
        while (index < rootPresence.Length)
        {
            this.SetBit(data, bitIndex, rootPresence[index]);
            bitIndex = bitIndex + 1;
            index = index + 1;
        }

        return new OerPreambleResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresent = extensionPresent,
            Presence = this.CopyPresence(rootPresence),
            Data = data,
            ConsumedCount = octetCount
        };
    }

    internal OerPreambleResult DecodeRootPreamble(
        byte[] data,
        int offset,
        int count,
        bool extensible,
        int rootPresenceCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (rootPresenceCount < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "rootPresenceCount");
        }

        int bitCount = rootPresenceCount + (extensible ? 1 : 0);
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            bitCount))
        {
            return this.CreatePreambleError(
                "ASNR0011",
                "The OER collection preamble exceeds the configured element limit.",
                true);
        }

        int octetCount = (bitCount + 7) / 8;
        if (count < octetCount)
        {
            return new OerPreambleResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = count,
                HasViolationOffset = true,
                ViolationOffset = count
            };
        }

        int bitIndex = 0;
        bool extensionPresent = false;
        if (extensible)
        {
            extensionPresent = this.GetBit(data, offset, bitIndex);
            bitIndex = bitIndex + 1;
        }

        bool[] presence = new bool[rootPresenceCount];
        int index = 0;
        while (index < presence.Length)
        {
            presence[index] = this.GetBit(data, offset, bitIndex);
            bitIndex = bitIndex + 1;
            index = index + 1;
        }

        while (bitIndex < octetCount * 8)
        {
            if (this.GetBit(data, offset, bitIndex))
            {
                return this.CreatePreambleErrorAt(
                    "ASNO0051",
                    "Unused OER preamble bits must be zero.",
                    false,
                    bitIndex / 8);
            }

            bitIndex = bitIndex + 1;
        }

        byte[] encoded = new byte[octetCount];
        System.Buffer.BlockCopy(data, offset, encoded, 0, octetCount);
        return new OerPreambleResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresent = extensionPresent,
            Presence = presence,
            Data = encoded,
            ConsumedCount = octetCount
        };
    }

    internal OerPreambleResult EncodeExtensionBitmap(bool[] presence)
    {
        if (presence == null)
        {
            throw new System.ArgumentNullException("presence");
        }

        if (presence.Length == 0 || !this.HasPresentBit(presence))
        {
            return this.CreatePreambleError(
                "ASNO0052",
                "An OER extension bitmap requires at least one present addition.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            presence.Length))
        {
            return this.CreatePreambleError(
                "ASNR0011",
                "The OER extension bitmap exceeds the configured element limit.",
                true);
        }

        int bitmapOctets = (presence.Length + 7) / 8;
        byte[] content = new byte[bitmapOctets + 1];
        content[0] = (byte)(bitmapOctets * 8 - presence.Length);
        int index = 0;
        while (index < presence.Length)
        {
            if (presence[index])
            {
                content[1 + index / 8] = (byte)(content[1 + index / 8]
                    | 1 << (7 - index % 8));
            }

            index = index + 1;
        }

        OerContentResult wrapped = new OerContentCodec(this.Limits)
            .EncodeVariableContent(content);
        if (!wrapped.Success)
        {
            return this.CopyContentFailure(wrapped);
        }

        return new OerPreambleResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresent = true,
            Presence = this.CopyPresence(presence),
            Data = wrapped.Data,
            ConsumedCount = wrapped.ConsumedCount
        };
    }

    internal OerPreambleResult DecodeExtensionBitmap(
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        OerContentResult wrapped = new OerContentCodec(this.Limits)
            .DecodeVariableContent(data, offset, count, canonical);
        if (!wrapped.Success)
        {
            return this.CopyContentFailure(wrapped);
        }

        if (wrapped.Content.Length < 2 || wrapped.Content[0] > 7)
        {
            return this.CreatePreambleErrorAt(
                "ASNO0053",
                "An OER extension bitmap requires an unused-bit count and at least one bitmap octet.",
                false,
                wrapped.ConsumedCount - wrapped.Content.Length);
        }

        int unusedBits = wrapped.Content[0];
        int presenceCount = (wrapped.Content.Length - 1) * 8 - unusedBits;
        if (presenceCount <= 0
            || !new ResourceCounter(this.Limits).AllowsCollectionElements(
                presenceCount))
        {
            return this.CreatePreambleErrorAt(
                presenceCount <= 0 ? "ASNO0053" : "ASNR0011",
                presenceCount <= 0
                    ? "An OER extension bitmap must contain at least one presence bit."
                    : "The OER extension bitmap exceeds the configured element limit.",
                presenceCount > 0,
                wrapped.ConsumedCount - wrapped.Content.Length);
        }

        int trailing = 0;
        while (trailing < unusedBits)
        {
            int bit = presenceCount + trailing;
            if (this.GetBit(wrapped.Content, 1, bit))
            {
                return this.CreatePreambleErrorAt(
                    "ASNO0054",
                    "Unused OER extension bitmap bits must be zero.",
                    false,
                    wrapped.ConsumedCount - wrapped.Content.Length
                        + 1 + bit / 8);
            }

            trailing = trailing + 1;
        }

        bool[] presence = new bool[presenceCount];
        int index = 0;
        while (index < presence.Length)
        {
            presence[index] = this.GetBit(wrapped.Content, 1, index);
            index = index + 1;
        }

        if (!this.HasPresentBit(presence))
        {
            return this.CreatePreambleErrorAt(
                "ASNO0052",
                "An OER extension bitmap requires at least one present addition.",
                false,
                wrapped.ConsumedCount - wrapped.Content.Length + 1);
        }

        if (canonical && !presence[presence.Length - 1])
        {
            return this.CreatePreambleErrorAt(
                "ASNO0058",
                "A COER extension bitmap must end at its last present addition.",
                false,
                wrapped.ConsumedCount - 1);
        }

        return new OerPreambleResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresent = true,
            Presence = presence,
            Data = wrapped.Data,
            ConsumedCount = wrapped.ConsumedCount
        };
    }

    internal bool GetBit(byte[] data, int offset, int bitIndex)
    {
        return (data[offset + bitIndex / 8]
            & 1 << (7 - bitIndex % 8)) != 0;
    }

    internal void SetBit(byte[] data, int bitIndex, bool value)
    {
        if (value)
        {
            data[bitIndex / 8] = (byte)(data[bitIndex / 8]
                | 1 << (7 - bitIndex % 8));
        }
    }

    internal bool HasPresentBit(bool[] presence)
    {
        int index = 0;
        while (index < presence.Length)
        {
            if (presence[index])
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool[] CopyPresence(bool[] presence)
    {
        bool[] result = new bool[presence.Length];
        int index = 0;
        while (index < presence.Length)
        {
            result[index] = presence[index];
            index = index + 1;
        }

        return result;
    }

    internal OerPreambleResult CopyContentFailure(OerContentResult source)
    {
        return new OerPreambleResult
        {
            Status = source.Status,
            ConsumedCount = source.ConsumedCount,
            HasViolationOffset = source.HasViolationOffset,
            ViolationOffset = source.ViolationOffset,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal OerPreambleResult CreatePreambleError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerPreambleResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal OerPreambleResult CreatePreambleErrorAt(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset)
    {
        OerPreambleResult result = this.CreatePreambleError(
            code,
            message,
            limitExceeded);
        result.HasViolationOffset = true;
        result.ViolationOffset = violationOffset;
        return result;
    }
}
