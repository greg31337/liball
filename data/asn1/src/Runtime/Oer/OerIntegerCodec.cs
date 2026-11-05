namespace asn1;

internal class OerIntegerCodec
{
    internal AsnResourceLimits Limits;
    internal ResourceCounter Resources;

    internal OerIntegerCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Resources = new ResourceCounter(limits);
    }

    internal OerIntegerFormat DeriveFormat(AsnTypeDescriptor type)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (type.Kind != AsnTypeKind.Integer
            && type.Kind != AsnTypeKind.Enumerated)
        {
            return this.CreateFormatError(
                "ASNO0010",
                "OER integer formatting requires INTEGER or ENUMERATED metadata.");
        }

        OerIntegerBounds bounds = new OerIntegerBounds();
        this.ApplyConstraints(type.Constraints, bounds);
        if (bounds.Visible && bounds.Intervals.Length == 0)
        {
            return this.CreateFormatError(
                "ASNO0011",
                "The effective OER integer bounds are empty or reversed.");
        }

        if (bounds.Visible)
        {
            this.ComputeHull(bounds);
        }

        OerIntegerFormat result = new OerIntegerFormat
        {
            Success = true,
            HasLowerBound = bounds.HasLowerBound,
            LowerBound = bounds.LowerBound,
            HasUpperBound = bounds.HasUpperBound,
            UpperBound = bounds.UpperBound,
            Signed = !bounds.HasLowerBound
                || bounds.LowerBound < System.Numerics.BigInteger.Zero
        };
        if (bounds.HasLowerBound && bounds.HasUpperBound)
        {
            result.FixedOctetCount = result.Signed
                ? this.GetSignedFixedOctets(
                    bounds.LowerBound,
                    bounds.UpperBound)
                : this.GetUnsignedFixedOctets(bounds.UpperBound);
        }

        return result;
    }

    internal OerIntegerResult EncodeInteger(
        AsnTypeDescriptor type,
        System.Numerics.BigInteger value)
    {
        OerIntegerFormat format = this.DeriveFormat(type);
        if (!format.Success)
        {
            return this.CopyFormatError(format);
        }

        if (!this.IsWithinBounds(format, value))
        {
            return this.CreateError(
                "ASNO0012",
                "The integer value is outside the effective OER bounds.",
                false);
        }

        byte[] content;
        if (format.FixedOctetCount > 0)
        {
            content = this.EncodeFixedContent(format, value);
            if (content.Length == 0)
            {
                return this.CreateError(
                    "ASNO0013",
                    "The integer value does not fit its fixed OER width.",
                    false);
            }

            if (!this.AllowsContent(content.Length)
                || !this.Resources.TryChargeAllocationBytes(content.Length))
            {
                return this.CreateError(
                    "ASNR0010",
                    "The fixed OER integer exceeds the configured limits.",
                    true);
            }

            return this.CreateEncodeSuccess(
                value,
                content,
                content.Length,
                format);
        }

        if (!format.Signed && value < System.Numerics.BigInteger.Zero)
        {
            return this.CreateError(
                "ASNO0012",
                "A non-negative OER integer cannot encode a negative value.",
                false);
        }

        content = format.Signed
            ? BigIntegerConversions.ToSignedBigEndian(value)
            : BigIntegerConversions.ToUnsignedBigEndian(value);
        if (!this.AllowsContent(content.Length))
        {
            return this.CreateError(
                "ASNR0010",
                "The variable OER integer exceeds the configured integer-bit limit.",
                true);
        }

        OerLengthResult length = new OerLengthCodec(this.Limits).WriteLength(
            content.Length);
        if (!length.Success)
        {
            return this.CopyLengthError(length);
        }

        int total = length.Data.Length + content.Length;
        if (!this.Resources.TryChargeAllocationBytes(total))
        {
            return this.CreateError(
                "ASNR0005",
                "Allocating the variable OER integer exceeds the configured limit.",
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
        return this.CreateEncodeSuccess(
            value,
            data,
            content.Length,
            format);
    }

    internal OerIntegerResult DecodeInteger(
        AsnTypeDescriptor type,
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

        OerIntegerFormat format = this.DeriveFormat(type);
        if (!format.Success)
        {
            return this.CopyFormatError(format);
        }

        int contentOffset = offset;
        int contentCount = format.FixedOctetCount;
        int determinantCount = 0;
        bool determinantMinimal = true;
        if (format.FixedOctetCount == 0)
        {
            OerLengthResult length = new OerLengthCodec(
                this.Limits).ConsumeLength(
                    data,
                    offset,
                    count,
                    true,
                    canonical);
            if (!length.Success)
            {
                return this.CopyLengthError(length);
            }

            if (length.Length > int.MaxValue)
            {
                return this.CreateErrorAt(
                    "ASNR0008",
                    "The variable OER integer length exceeds the supported range.",
                    true,
                    0);
            }

            determinantCount = length.ConsumedCount;
            determinantMinimal = length.Minimal;
            contentOffset = offset + determinantCount;
            contentCount = (int)length.Length;
        }

        if (contentCount == 0)
        {
            return this.CreateErrorAt(
                "ASNO0014",
                "An OER integer encoding must contain at least one content octet.",
                false,
                0);
        }

        if (contentCount > count - determinantCount)
        {
            return new OerIntegerResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = count,
                HasViolationOffset = true,
                ViolationOffset = count
            };
        }

        if (!this.AllowsContent(contentCount)
            || !this.Resources.TryChargeAllocationBytes(contentCount))
        {
            return this.CreateErrorAt(
                "ASNR0010",
                "The decoded OER integer exceeds the configured limits.",
                true,
                0);
        }

        byte[] content = new byte[contentCount];
        System.Buffer.BlockCopy(
            data,
            contentOffset,
            content,
            0,
            contentCount);
        bool minimal = determinantMinimal
            && (format.FixedOctetCount > 0
                || this.IsMinimalContent(content, format.Signed));
        if (canonical && !minimal)
        {
            return this.CreateErrorAt(
                "ASNO0015",
                "The COER integer content does not use the shortest form.",
                false,
                determinantCount);
        }

        System.Numerics.BigInteger value = format.Signed
            ? BigIntegerConversions.FromSignedBigEndian(content)
            : BigIntegerConversions.FromUnsignedBigEndian(content);
        if (!this.IsWithinBounds(format, value))
        {
            return this.CreateErrorAt(
                "ASNO0012",
                "The decoded integer is outside the effective OER bounds.",
                false,
                determinantCount);
        }

        return new OerIntegerResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = content,
            ContentOctetCount = contentCount,
            ConsumedCount = determinantCount + contentCount,
            FixedOctetCount = format.FixedOctetCount,
            Signed = format.Signed,
            Minimal = minimal
        };
    }

    internal void ApplyConstraints(
        AsnConstraintDescriptor[] constraints,
        OerIntegerBounds bounds)
    {
        int index = 0;
        while (index < constraints.Length)
        {
            AsnConstraintDescriptor constraint = constraints[index];
            if (constraint.Applications.Length == 0)
            {
                this.ApplyConstraintApplication(constraint, bounds);
            }
            else
            {
                int applicationIndex = 0;
                while (applicationIndex < constraint.Applications.Length)
                {
                    this.ApplyConstraintApplication(
                        constraint.Applications[applicationIndex],
                        bounds);
                    applicationIndex = applicationIndex + 1;
                }
            }

            index = index + 1;
        }
    }

    internal void ApplyConstraintApplication(
        AsnConstraintDescriptor constraint,
        OerIntegerBounds bounds)
    {
        if (constraint.DomainKind != AsnTypeKind.Integer
            && constraint.DomainKind != AsnTypeKind.Enumerated)
        {
            return;
        }

        if (constraint.Extensible || constraint.RootIntervals.Length == 0)
        {
            return;
        }

        this.ApplyIntervals(constraint.RootIntervals, bounds);
    }

    internal void ApplyIntervals(
        AsnIntervalDescriptor[] intervals,
        OerIntegerBounds bounds)
    {
        if (!bounds.Visible)
        {
            bounds.Visible = true;
            bounds.Intervals = intervals;
            return;
        }

        System.Collections.Generic.List<AsnIntervalDescriptor> intersection =
            new System.Collections.Generic.List<AsnIntervalDescriptor>();
        int leftIndex = 0;
        while (leftIndex < bounds.Intervals.Length)
        {
            int rightIndex = 0;
            while (rightIndex < intervals.Length)
            {
                AsnIntervalDescriptor? item = this.IntersectIntervals(
                    bounds.Intervals[leftIndex],
                    intervals[rightIndex]);
                if (item != null)
                {
                    intersection.Add(item);
                }

                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }

        bounds.Intervals = intersection.ToArray();
    }

    internal AsnIntervalDescriptor? IntersectIntervals(
        AsnIntervalDescriptor left,
        AsnIntervalDescriptor right)
    {
        bool hasLower = left.HasLowerBound || right.HasLowerBound;
        System.Numerics.BigInteger lower = left.HasLowerBound
            ? left.LowerBound
            : right.LowerBound;
        if (left.HasLowerBound && right.HasLowerBound
            && right.LowerBound > lower)
        {
            lower = right.LowerBound;
        }

        bool hasUpper = left.HasUpperBound || right.HasUpperBound;
        System.Numerics.BigInteger upper = left.HasUpperBound
            ? left.UpperBound
            : right.UpperBound;
        if (left.HasUpperBound && right.HasUpperBound
            && right.UpperBound < upper)
        {
            upper = right.UpperBound;
        }

        if (hasLower && hasUpper && lower > upper)
        {
            return null;
        }

        return new AsnIntervalDescriptor
        {
            HasLowerBound = hasLower,
            LowerBound = lower,
            HasUpperBound = hasUpper,
            UpperBound = upper
        };
    }

    internal void ComputeHull(OerIntegerBounds bounds)
    {
        bool allLowerBounded = true;
        bool allUpperBounded = true;
        bool firstLower = true;
        bool firstUpper = true;
        int index = 0;
        while (index < bounds.Intervals.Length)
        {
            AsnIntervalDescriptor interval = bounds.Intervals[index];
            if (!interval.HasLowerBound)
            {
                allLowerBounded = false;
            }
            else if (firstLower || interval.LowerBound < bounds.LowerBound)
            {
                bounds.LowerBound = interval.LowerBound;
                firstLower = false;
            }

            if (!interval.HasUpperBound)
            {
                allUpperBounded = false;
            }
            else if (firstUpper || interval.UpperBound > bounds.UpperBound)
            {
                bounds.UpperBound = interval.UpperBound;
                firstUpper = false;
            }

            index = index + 1;
        }

        bounds.HasLowerBound = allLowerBounded;
        bounds.HasUpperBound = allUpperBounded;
    }

    internal int GetUnsignedFixedOctets(
        System.Numerics.BigInteger upperBound)
    {
        if (upperBound <= new System.Numerics.BigInteger(255))
        {
            return 1;
        }

        if (upperBound <= new System.Numerics.BigInteger(65535))
        {
            return 2;
        }

        System.Numerics.BigInteger maximum32 =
            (System.Numerics.BigInteger.One << 32) - 1;
        if (upperBound <= maximum32)
        {
            return 4;
        }

        System.Numerics.BigInteger maximum64 =
            (System.Numerics.BigInteger.One << 64) - 1;
        return upperBound <= maximum64 ? 8 : 0;
    }

    internal int GetSignedFixedOctets(
        System.Numerics.BigInteger lowerBound,
        System.Numerics.BigInteger upperBound)
    {
        int octets = 1;
        while (octets <= 8)
        {
            int bits = octets * 8 - 1;
            System.Numerics.BigInteger minimum =
                -(System.Numerics.BigInteger.One << bits);
            System.Numerics.BigInteger maximum =
                (System.Numerics.BigInteger.One << bits) - 1;
            if (lowerBound >= minimum && upperBound <= maximum)
            {
                return octets;
            }

            octets = octets == 1 ? 2 : octets * 2;
        }

        return 0;
    }

    internal byte[] EncodeFixedContent(
        OerIntegerFormat format,
        System.Numerics.BigInteger value)
    {
        byte[] minimal = format.Signed
            ? BigIntegerConversions.ToSignedBigEndian(value)
            : BigIntegerConversions.ToUnsignedBigEndian(value);
        if (minimal.Length > format.FixedOctetCount)
        {
            return [];
        }

        byte[] result = new byte[format.FixedOctetCount];
        byte fill = format.Signed && value < System.Numerics.BigInteger.Zero
            ? (byte)255
            : (byte)0;
        int index = 0;
        while (index < result.Length - minimal.Length)
        {
            result[index] = fill;
            index = index + 1;
        }

        System.Buffer.BlockCopy(
            minimal,
            0,
            result,
            result.Length - minimal.Length,
            minimal.Length);
        return result;
    }

    internal bool IsMinimalContent(byte[] content, bool signed)
    {
        if (content.Length == 0)
        {
            return false;
        }

        if (content.Length == 1)
        {
            return true;
        }

        if (!signed)
        {
            return content[0] != 0;
        }

        return !(content[0] == 0 && (content[1] & 128) == 0)
            && !(content[0] == 255 && (content[1] & 128) != 0);
    }

    internal bool IsWithinBounds(
        OerIntegerFormat format,
        System.Numerics.BigInteger value)
    {
        return (!format.HasLowerBound || value >= format.LowerBound)
            && (!format.HasUpperBound || value <= format.UpperBound);
    }

    internal bool AllowsContent(int octetCount)
    {
        if (octetCount > int.MaxValue / 8)
        {
            return false;
        }

        return new ResourceCounter(this.Limits).AllowsIntegerBits(
            octetCount * 8);
    }

    internal OerIntegerResult CreateEncodeSuccess(
        System.Numerics.BigInteger value,
        byte[] data,
        int contentOctets,
        OerIntegerFormat format)
    {
        return new OerIntegerResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = data,
            ContentOctetCount = contentOctets,
            ConsumedCount = data.Length,
            FixedOctetCount = format.FixedOctetCount,
            Signed = format.Signed
        };
    }

    internal OerIntegerFormat CreateFormatError(string code, string message)
    {
        return new OerIntegerFormat
        {
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal OerIntegerResult CopyFormatError(OerIntegerFormat format)
    {
        return this.CreateError(
            format.ErrorCode,
            format.ErrorMessage,
            false);
    }

    internal OerIntegerResult CopyLengthError(OerLengthResult length)
    {
        OerIntegerResult result = this.CreateError(
            length.ErrorCode,
            length.ErrorMessage,
            length.LimitExceeded);
        result.ConsumedCount = length.ConsumedCount;
        result.HasViolationOffset = length.HasViolationOffset;
        result.ViolationOffset = length.ViolationOffset;
        return result;
    }

    internal OerIntegerResult CreateErrorAt(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset)
    {
        OerIntegerResult result = this.CreateError(
            code,
            message,
            limitExceeded);
        result.HasViolationOffset = true;
        result.ViolationOffset = violationOffset;
        return result;
    }

    internal OerIntegerResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerIntegerResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
