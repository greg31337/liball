namespace asn1;

internal class BerRealCodec
{
    internal AsnResourceLimits Limits;

    internal BerRealCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerPrimitiveValueResult EncodeReal(
        AsnRealValue value,
        AsnEncodingRules rules)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        ResourceCounter resources = new ResourceCounter(this.Limits);
        if (value.Kind == AsnRealKind.FiniteDecimal
            && (value.DecimalText == null
                || !resources.AllowsTextCharacters(value.DecimalText.Length)))
        {
            return this.CreateRealError(
                "ASNR0009",
                "The REAL decimal text exceeds the configured text limit.",
                true);
        }

        BerIntegerCodec integers = new BerIntegerCodec(this.Limits);
        if (value.Kind == AsnRealKind.FiniteBinary
            && (!integers.FitsIntegerLimit(value.Mantissa)
                || !integers.FitsIntegerLimit(value.Exponent)))
        {
            return this.CreateRealError(
                "ASNR0010",
                "The REAL binary tuple exceeds the configured integer-bit limit.",
                true);
        }

        RealNormalizationResult normalized = new RealValueNormalizer()
            .NormalizeRealValue(value);
        if (!normalized.Success || normalized.Value == null)
        {
            return this.CreateRealError(
                "ASNE0031",
                normalized.ErrorMessage,
                false);
        }

        AsnRealValue source = normalized.Value;
        if (source.Kind == AsnRealKind.Zero)
        {
            return new BerPrimitiveValueResult
            {
                Success = true
            };
        }

        if (source.Kind == AsnRealKind.PositiveInfinity
            || source.Kind == AsnRealKind.NegativeInfinity
            || source.Kind == AsnRealKind.NotANumber
            || source.Kind == AsnRealKind.MinusZero)
        {
            byte special = source.Kind == AsnRealKind.PositiveInfinity
                ? (byte)64
                : source.Kind == AsnRealKind.NegativeInfinity
                    ? (byte)65
                    : source.Kind == AsnRealKind.NotANumber
                        ? (byte)66
                        : (byte)67;
            return new BerPrimitiveValueResult
            {
                Success = true,
                Content = [special]
            };
        }

        if (source.Kind == AsnRealKind.FiniteDecimal)
        {
            byte[] text = System.Text.Encoding.ASCII.GetBytes(
                rules == AsnEncodingRules.Der
                    || rules == AsnEncodingRules.Cer
                    ? this.FormatDerDecimal(source)
                    : source.DecimalText);
            byte[] content = new byte[text.Length + 1];
            content[0] = 3;
            System.Buffer.BlockCopy(text, 0, content, 1, text.Length);
            return new BerPrimitiveValueResult
            {
                Success = true,
                Content = content
            };
        }

        System.Numerics.BigInteger mantissa = source.Mantissa;
        bool negative = mantissa < System.Numerics.BigInteger.Zero;
        byte[] exponent = BigIntegerConversions.ToSignedBigEndian(
            source.Exponent);
        byte[] magnitude = BigIntegerConversions.ToUnsignedBigEndian(
            System.Numerics.BigInteger.Abs(mantissa));
        if (exponent.Length > 255)
        {
            return this.CreateRealError(
                "ASNR0012",
                "The REAL exponent requires more than 255 octets.",
                true);
        }

        int prefixLength = exponent.Length <= 3 ? 1 : 2;
        byte[] binary = new byte[
            prefixLength + exponent.Length + magnitude.Length];
        int first = 128;
        if (negative)
        {
            first = first | 64;
        }

        if (exponent.Length <= 3)
        {
            first = first | exponent.Length - 1;
        }
        else
        {
            first = first | 3;
            binary[1] = (byte)exponent.Length;
        }

        binary[0] = (byte)first;
        System.Buffer.BlockCopy(
            exponent,
            0,
            binary,
            prefixLength,
            exponent.Length);
        System.Buffer.BlockCopy(
            magnitude,
            0,
            binary,
            prefixLength + exponent.Length,
            magnitude.Length);
        return new BerPrimitiveValueResult
        {
            Success = true,
            Content = binary
        };
    }

    internal string FormatDerDecimal(AsnRealValue value)
    {
        string coefficient = value.DecimalCoefficient.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        string exponent = value.DecimalExponent == System.Numerics.BigInteger.Zero
            ? "+0"
            : value.DecimalExponent.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        return coefficient + ".E" + exponent;
    }

    internal BerPrimitiveValueResult DecodeReal(byte[] content)
    {
        if (content == null)
        {
            throw new System.ArgumentNullException("content");
        }

        if (content.Length == 0)
        {
            return this.CreateDecodedReal(new AsnRealValue
            {
                Kind = AsnRealKind.Zero,
                Normalized = true
            });
        }

        if (content.Length == 1 && content[0] >= 64 && content[0] <= 67)
        {
            AsnRealKind kind = content[0] == 64
                ? AsnRealKind.PositiveInfinity
                : content[0] == 65
                    ? AsnRealKind.NegativeInfinity
                    : content[0] == 66
                        ? AsnRealKind.NotANumber
                        : AsnRealKind.MinusZero;
            return this.CreateDecodedReal(new AsnRealValue
            {
                Kind = kind,
                Normalized = true
            });
        }

        if ((content[0] & 128) == 0)
        {
            if (content[0] < 1 || content[0] > 3 || content.Length == 1)
            {
                return this.CreateRealError(
                    "ASND0037",
                    "The decimal REAL content uses an unsupported NR form.",
                    false);
            }

            int index = 1;
            while (index < content.Length)
            {
                if (content[index] > 127)
                {
                    return this.CreateRealError(
                        "ASND0038",
                        "The decimal REAL content is not ASCII text.",
                        false);
                }

                index = index + 1;
            }

            string text = System.Text.Encoding.ASCII.GetString(
                content,
                1,
                content.Length - 1);
            if (!new ResourceCounter(this.Limits).AllowsTextCharacters(
                text.Length))
            {
                return this.CreateRealError(
                    "ASNR0009",
                    "The REAL decimal text exceeds the configured text limit.",
                    true);
            }

            RealNormalizationResult normalized = new RealValueNormalizer()
                .NormalizeRealValue(new AsnRealValue
                {
                    Kind = AsnRealKind.FiniteDecimal,
                    HasDecimalValue = true,
                    DecimalText = text
                });
            if (!normalized.Success || normalized.Value == null)
            {
                return this.CreateRealError(
                    "ASND0039",
                    "The decimal REAL content is not valid decimal notation.",
                    false);
            }

            return this.CreateDecodedReal(normalized.Value);
        }

        int exponentLengthCode = content[0] & 3;
        int exponentLength = exponentLengthCode + 1;
        int exponentOffset = 1;
        if (exponentLengthCode == 3)
        {
            if (content.Length < 2 || content[1] == 0)
            {
                return this.CreateRealError(
                    "ASND0040",
                    "The binary REAL exponent-length form is invalid.",
                    false);
            }

            exponentLength = content[1];
            exponentOffset = 2;
        }

        if (exponentLength > content.Length - exponentOffset
            || exponentLength == content.Length - exponentOffset)
        {
            return this.CreateRealError(
                "ASND0041",
                "The binary REAL exponent or mantissa is truncated.",
                false);
        }

        byte[] exponentBytes = new byte[exponentLength];
        System.Buffer.BlockCopy(
            content,
            exponentOffset,
            exponentBytes,
            0,
            exponentLength);
        if (exponentBytes.Length > 1
            && (exponentBytes[0] == 0 && (exponentBytes[1] & 128) == 0
                || exponentBytes[0] == 255
                    && (exponentBytes[1] & 128) != 0))
        {
            return this.CreateRealError(
                "ASND0042",
                "The binary REAL exponent has a redundant leading octet.",
                false);
        }

        int mantissaOffset = exponentOffset + exponentLength;
        int mantissaLength = content.Length - mantissaOffset;
        if (mantissaLength > 1 && content[mantissaOffset] == 0)
        {
            return this.CreateRealError(
                "ASND0043",
                "The binary REAL mantissa has a redundant leading octet.",
                false);
        }

        byte[] mantissaBytes = new byte[mantissaLength];
        System.Buffer.BlockCopy(
            content,
            mantissaOffset,
            mantissaBytes,
            0,
            mantissaLength);
        System.Numerics.BigInteger mantissa =
            BigIntegerConversions.FromUnsignedBigEndian(mantissaBytes);
        if (mantissa == System.Numerics.BigInteger.Zero)
        {
            return this.CreateRealError(
                "ASND0044",
                "A binary REAL mantissa cannot be zero.",
                false);
        }

        if ((content[0] & 64) != 0)
        {
            mantissa = -mantissa;
        }

        int baseCode = (content[0] >> 4) & 3;
        if (baseCode == 3)
        {
            return this.CreateRealError(
                "ASND0045",
                "The binary REAL base selector is reserved.",
                false);
        }

        int exponentFactor = baseCode == 0 ? 1 : baseCode == 1 ? 3 : 4;
        int scaling = (content[0] >> 2) & 3;
        System.Numerics.BigInteger exponent =
            BigIntegerConversions.FromSignedBigEndian(exponentBytes)
            * new System.Numerics.BigInteger(exponentFactor)
            + new System.Numerics.BigInteger(scaling);
        BerIntegerCodec integers = new BerIntegerCodec(this.Limits);
        if (!integers.FitsIntegerLimit(mantissa)
            || !integers.FitsIntegerLimit(exponent))
        {
            return this.CreateRealError(
                "ASNR0010",
                "The REAL binary tuple exceeds the configured integer-bit limit.",
                true);
        }

        RealNormalizationResult binary = new RealValueNormalizer()
            .NormalizeRealValue(new AsnRealValue
            {
                Kind = AsnRealKind.FiniteBinary,
                HasBinaryValue = true,
                Mantissa = mantissa,
                Radix = 2,
                Exponent = exponent
            });
        if (!binary.Success || binary.Value == null)
        {
            return this.CreateRealError(
                "ASND0046",
                "The binary REAL content is invalid.",
                false);
        }

        return this.CreateDecodedReal(binary.Value);
    }

    internal BerPrimitiveValueResult CreateDecodedReal(AsnRealValue value)
    {
        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Real,
                RealValue = value
            }
        };
    }

    internal BerPrimitiveValueResult CreateRealError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new BerPrimitiveValueResult
        {
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
