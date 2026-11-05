namespace asn1;

internal class BerCharacterStringCodec
{
    internal AsnResourceLimits Limits;

    internal BerCharacterStringCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerPrimitiveValueResult EncodeText(
        AsnTypeKind kind,
        string text)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(text.Length))
        {
            return this.CreateTextError(
                "ASNR0009",
                "The character string exceeds the configured text limit.",
                true);
        }

        if (kind == AsnTypeKind.Utf8String
            || kind == AsnTypeKind.ObjectIdentifierIri
            || kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            if (!this.ValidateUnicodeText(text))
            {
                return this.CreateTextError(
                    "ASNE0024",
                    "The character string contains an invalid Unicode scalar sequence.",
                    false);
            }

            if ((kind == AsnTypeKind.ObjectIdentifierIri
                    || kind == AsnTypeKind.RelativeObjectIdentifierIri)
                && !this.ValidateIriText(text, kind))
            {
                return this.CreateTextError(
                    "ASNE0025",
                    "The object identifier IRI text is not valid.",
                    false);
            }

            return new BerPrimitiveValueResult
            {
                Success = true,
                Content = System.Text.Encoding.UTF8.GetBytes(text)
            };
        }

        if (kind == AsnTypeKind.UniversalString)
        {
            return this.EncodeUniversalText(text);
        }

        if (kind == AsnTypeKind.BmpString)
        {
            return this.EncodeBmpText(text);
        }

        ByteAccumulator bytes = new ByteAccumulator(text.Length);
        int index = 0;
        while (index < text.Length)
        {
            UnicodeScalarReadResult scalar = this.ReadTextScalar(text, index);
            if (!scalar.Success || !this.IsAllowedScalar(scalar.Scalar, kind))
            {
                return this.CreateTextError(
                    "ASNE0026",
                    "The character string contains a scalar outside the type's permitted alphabet.",
                    false);
            }

            if (scalar.Scalar > 255)
            {
                return this.CreateTextError(
                    "ASNE0027",
                    "The selected one-octet character string cannot represent a scalar above 255.",
                    false);
            }

            bytes.AppendByte((byte)scalar.Scalar);
            index = scalar.NextIndex;
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Content = bytes.ToArray()
        };
    }

    internal BerPrimitiveValueResult DecodeText(
        AsnTypeKind kind,
        byte[] content)
    {
        if (content == null)
        {
            throw new System.ArgumentNullException("content");
        }

        string text;
        if (kind == AsnTypeKind.Utf8String
            || kind == AsnTypeKind.ObjectIdentifierIri
            || kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            BerPrimitiveValueResult utf8 = this.DecodeUtf8Text(content);
            if (!utf8.Success || utf8.Value == null)
            {
                return utf8;
            }

            text = utf8.Value.TextValue;
            if ((kind == AsnTypeKind.ObjectIdentifierIri
                    || kind == AsnTypeKind.RelativeObjectIdentifierIri)
                && !this.ValidateIriText(text, kind))
            {
                return this.CreateTextError(
                    "ASND0025",
                    "The object identifier IRI text is not valid.",
                    false);
            }
        }
        else if (kind == AsnTypeKind.UniversalString)
        {
            BerPrimitiveValueResult universal = this.DecodeUniversalText(content);
            if (!universal.Success || universal.Value == null)
            {
                return universal;
            }

            text = universal.Value.TextValue;
        }
        else if (kind == AsnTypeKind.BmpString)
        {
            BerPrimitiveValueResult bmp = this.DecodeBmpText(content);
            if (!bmp.Success || bmp.Value == null)
            {
                return bmp;
            }

            text = bmp.Value.TextValue;
        }
        else
        {
            TextAccumulator decoded = new TextAccumulator(content.Length);
            int index = 0;
            while (index < content.Length)
            {
                int scalar = content[index];
                if (!this.IsAllowedScalar(scalar, kind))
                {
                    return this.CreateTextError(
                        "ASND0026",
                        "The encoded character is outside the type's permitted alphabet.",
                        false);
                }

                decoded.AppendCharacter((char)scalar);
                index = index + 1;
            }

            text = decoded.ToText();
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(text.Length))
        {
            return this.CreateTextError(
                "ASNR0009",
                "The character string exceeds the configured text limit.",
                true);
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = kind == AsnTypeKind.ObjectIdentifierIri
                    ? AsnRuntimeValueKind.ObjectIdentifierIri
                    : kind == AsnTypeKind.RelativeObjectIdentifierIri
                        ? AsnRuntimeValueKind.RelativeObjectIdentifierIri
                        : AsnRuntimeValueKind.CharacterString,
                TextValue = text
            }
        };
    }

    internal BerPrimitiveValueResult EncodeUniversalText(string text)
    {
        ByteAccumulator bytes = new ByteAccumulator(0);
        int index = 0;
        while (index < text.Length)
        {
            UnicodeScalarReadResult scalar = this.ReadTextScalar(text, index);
            if (!scalar.Success)
            {
                return this.CreateTextError(
                    "ASNE0024",
                    "UniversalString contains an invalid Unicode scalar sequence.",
                    false);
            }

            bytes.AppendByte((byte)((scalar.Scalar >> 24) & 255));
            bytes.AppendByte((byte)((scalar.Scalar >> 16) & 255));
            bytes.AppendByte((byte)((scalar.Scalar >> 8) & 255));
            bytes.AppendByte((byte)(scalar.Scalar & 255));
            index = scalar.NextIndex;
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Content = bytes.ToArray()
        };
    }

    internal BerPrimitiveValueResult EncodeBmpText(string text)
    {
        ByteAccumulator bytes = new ByteAccumulator(0);
        int index = 0;
        while (index < text.Length)
        {
            UnicodeScalarReadResult scalar = this.ReadTextScalar(text, index);
            if (!scalar.Success || scalar.Scalar > 65535)
            {
                return this.CreateTextError(
                    "ASNE0028",
                    "BMPString requires Basic Multilingual Plane scalar values.",
                    false);
            }

            bytes.AppendByte((byte)((scalar.Scalar >> 8) & 255));
            bytes.AppendByte((byte)(scalar.Scalar & 255));
            index = scalar.NextIndex;
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Content = bytes.ToArray()
        };
    }

    internal BerPrimitiveValueResult DecodeUtf8Text(byte[] content)
    {
        TextAccumulator text = new TextAccumulator(content.Length);
        Utf8ChunkDecoder decoder = new Utf8ChunkDecoder();
        int index = 0;
        while (index < content.Length)
        {
            int length = decoder.GetSequenceLength(content[index]);
            if (length == 0
                || length > content.Length - index
                || !decoder.IsValidSequence(content, index, length))
            {
                return this.CreateTextError(
                    "ASND0027",
                    "The UTF-8 content contains an invalid or truncated sequence.",
                    false);
            }

            decoder.AppendScalar(
                text,
                decoder.DecodeScalar(content, index, length));
            index = index + length;
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue { TextValue = text.ToText() }
        };
    }

    internal BerPrimitiveValueResult DecodeUniversalText(byte[] content)
    {
        if (content.Length % 4 != 0)
        {
            return this.CreateTextError(
                "ASND0028",
                "UniversalString content must contain complete four-octet scalar values.",
                false);
        }

        TextAccumulator text = new TextAccumulator(content.Length / 2);
        Utf8ChunkDecoder scalarWriter = new Utf8ChunkDecoder();
        int index = 0;
        while (index < content.Length)
        {
            int scalar = (content[index] << 24)
                | (content[index + 1] << 16)
                | (content[index + 2] << 8)
                | content[index + 3];
            if (!this.IsUnicodeScalar(scalar))
            {
                return this.CreateTextError(
                    "ASND0029",
                    "UniversalString content identifies a non-scalar value.",
                    false);
            }

            scalarWriter.AppendScalar(text, scalar);
            index = index + 4;
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue { TextValue = text.ToText() }
        };
    }

    internal BerPrimitiveValueResult DecodeBmpText(byte[] content)
    {
        if (content.Length % 2 != 0)
        {
            return this.CreateTextError(
                "ASND0030",
                "BMPString content must contain complete two-octet scalar values.",
                false);
        }

        TextAccumulator text = new TextAccumulator(content.Length / 2);
        int index = 0;
        while (index < content.Length)
        {
            int scalar = (content[index] << 8) | content[index + 1];
            if (!this.IsUnicodeScalar(scalar))
            {
                return this.CreateTextError(
                    "ASND0031",
                    "BMPString content identifies a surrogate value.",
                    false);
            }

            text.AppendCharacter((char)scalar);
            index = index + 2;
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue { TextValue = text.ToText() }
        };
    }

    internal bool ValidateUnicodeText(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            UnicodeScalarReadResult scalar = this.ReadTextScalar(text, index);
            if (!scalar.Success)
            {
                return false;
            }

            index = scalar.NextIndex;
        }

        return true;
    }

    internal bool ValidateIriText(string text, AsnTypeKind kind)
    {
        bool relative = kind == AsnTypeKind.RelativeObjectIdentifierIri;
        if (text.Length == 0
            || relative && text[0] == '/'
            || !relative && text[0] != '/')
        {
            return false;
        }

        int index = relative ? 0 : 1;
        bool segment = false;
        while (index < text.Length)
        {
            if (text[index] == '/')
            {
                if (!segment)
                {
                    return false;
                }

                segment = false;
                index = index + 1;
                continue;
            }

            if (text[index] == '%')
            {
                if (index + 2 >= text.Length
                    || !this.IsHexDigit(text[index + 1])
                    || !this.IsHexDigit(text[index + 2]))
                {
                    return false;
                }

                segment = true;
                index = index + 3;
                continue;
            }

            UnicodeScalarReadResult scalar = this.ReadTextScalar(text, index);
            if (!scalar.Success || !this.IsIriScalar(scalar.Scalar))
            {
                return false;
            }

            segment = true;
            index = scalar.NextIndex;
        }

        return segment;
    }

    internal UnicodeScalarReadResult ReadTextScalar(string text, int index)
    {
        if (index < 0 || index >= text.Length)
        {
            return new UnicodeScalarReadResult();
        }

        int first = text[index];
        if (first >= 55296 && first <= 56319)
        {
            if (index + 1 >= text.Length)
            {
                return new UnicodeScalarReadResult();
            }

            int second = text[index + 1];
            if (second < 56320 || second > 57343)
            {
                return new UnicodeScalarReadResult();
            }

            return new UnicodeScalarReadResult
            {
                Success = true,
                Scalar = 65536 + (first - 55296) * 1024 + second - 56320,
                NextIndex = index + 2
            };
        }

        if (first >= 56320 && first <= 57343)
        {
            return new UnicodeScalarReadResult();
        }

        return new UnicodeScalarReadResult
        {
            Success = true,
            Scalar = first,
            NextIndex = index + 1
        };
    }

    internal bool IsAllowedScalar(int scalar, AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.NumericString)
        {
            return scalar == 32 || scalar >= 48 && scalar <= 57;
        }

        if (kind == AsnTypeKind.PrintableString)
        {
            return scalar >= 65 && scalar <= 90
                || scalar >= 97 && scalar <= 122
                || scalar >= 48 && scalar <= 57
                || scalar == 32 || scalar == 39 || scalar == 40
                || scalar == 41 || scalar == 43 || scalar == 44
                || scalar == 45 || scalar == 46 || scalar == 47
                || scalar == 58 || scalar == 61 || scalar == 63;
        }

        if (kind == AsnTypeKind.Ia5String)
        {
            return scalar <= 127;
        }

        if (kind == AsnTypeKind.VisibleString)
        {
            return scalar >= 32 && scalar <= 126;
        }

        if (kind == AsnTypeKind.GraphicString)
        {
            return scalar >= 32 && (scalar < 127 || scalar > 159);
        }

        return (kind == AsnTypeKind.TeletexString
                || kind == AsnTypeKind.VideotexString
                || kind == AsnTypeKind.GeneralString)
            && scalar <= 255;
    }

    internal bool IsUnicodeScalar(int scalar)
    {
        return scalar >= 0 && scalar <= 1114111
            && (scalar < 55296 || scalar > 57343);
    }

    internal bool IsIriScalar(int scalar)
    {
        return scalar >= 160
            || scalar >= 65 && scalar <= 90
            || scalar >= 97 && scalar <= 122
            || scalar >= 48 && scalar <= 57
            || scalar == 45 || scalar == 46 || scalar == 95
            || scalar == 126 || scalar == 33 || scalar == 36
            || scalar == 38 || scalar == 39 || scalar == 40
            || scalar == 41 || scalar == 42 || scalar == 43
            || scalar == 44 || scalar == 59 || scalar == 61
            || scalar == 58 || scalar == 64;
    }

    internal bool IsHexDigit(char value)
    {
        return value >= '0' && value <= '9'
            || value >= 'A' && value <= 'F'
            || value >= 'a' && value <= 'f';
    }

    internal BerPrimitiveValueResult CreateTextError(
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
