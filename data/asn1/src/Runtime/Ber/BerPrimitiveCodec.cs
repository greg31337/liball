namespace asn1;

internal class BerPrimitiveCodec
{
    internal AsnResourceLimits Limits;

    internal BerPrimitiveCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerPrimitiveEncodeResult EncodePrimitive(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (!this.IsBerFamilyRule(rules))
        {
            return this.CreateEncodeError(
                "ASNE0032",
                "Primitive BER encoding requires BER, DER, or CER rules.",
                false);
        }

        int tagNumber = this.GetUniversalTagNumber(kind);
        if (tagNumber < 0)
        {
            return this.CreateEncodeError(
                "ASNE0033",
                "The requested ASN.1 type is not a primitive BER type.",
                false);
        }

        if (!this.HasExpectedRuntimeKind(kind, value))
        {
            return this.CreateEncodeError(
                "ASNE0034",
                "The runtime value does not match the requested ASN.1 type.",
                false);
        }

        BerPrimitiveValueResult encoded = this.EncodePrimitiveContent(
            kind,
            value,
            rules);
        if (!encoded.Success)
        {
            return this.CreateEncodeError(
                encoded.ErrorCode,
                encoded.ErrorMessage,
                encoded.LimitExceeded);
        }

        if (!this.AllowsContent(encoded.Content.Length))
        {
            return this.CreateEncodeError(
                "ASNR0005",
                "The primitive content exceeds the configured byte or allocation limit.",
                true);
        }

        BerTlv tlv = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = new System.Numerics.BigInteger(tagNumber)
            },
            Content = encoded.Content
        };
        BerStringCodec strings = new BerStringCodec(this.Limits);
        if (rules == AsnEncodingRules.Cer
            && strings.IsSupportedStringKind(kind))
        {
            BerStringResult canonical = strings.SelectCerStringForm(kind, tlv);
            if (!canonical.Success || canonical.Tlv == null)
            {
                return this.CreateEncodeError(
                    canonical.ErrorCode,
                    canonical.ErrorMessage,
                    canonical.LimitExceeded);
            }

            tlv = canonical.Tlv;
        }

        return new BerPrimitiveEncodeResult
        {
            Success = true,
            Tlv = tlv
        };
    }

    internal BerPrimitiveDecodeResult DecodePrimitive(
        AsnTypeKind kind,
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        if (tlv == null)
        {
            throw new System.ArgumentNullException("tlv");
        }

        if (!this.IsBerFamilyRule(rules))
        {
            return this.CreateDecodeError(
                "ASND0047",
                "Primitive BER decoding requires BER, DER, or CER rules.",
                false);
        }

        int tagNumber = this.GetUniversalTagNumber(kind);
        if (tagNumber < 0)
        {
            return this.CreateDecodeError(
                "ASND0048",
                "The requested ASN.1 type is not a primitive BER type.",
                false);
        }

        if (tlv.Tag == null
            || tlv.Tag.Class != AsnTagClass.Universal
            || tlv.Tag.Number != new System.Numerics.BigInteger(tagNumber))
        {
            return this.CreateDecodeError(
                "ASND0049",
                "The BER value does not have the expected universal tag.",
                false);
        }

        if (tlv.Constructed
            || tlv.IndefiniteLength
            || tlv.Content == null
            || tlv.Children == null
            || tlv.Children.Length != 0)
        {
            return this.CreateDecodeError(
                "ASND0050",
                "The BER primitive must use definite primitive content.",
                false);
        }

        if (!this.AllowsContent(tlv.Content.Length))
        {
            return this.CreateDecodeError(
                "ASNR0005",
                "The primitive content exceeds the configured byte or allocation limit.",
                true);
        }

        if (rules == AsnEncodingRules.Cer
            && new BerStringCodec(this.Limits).IsSupportedStringKind(kind)
            && tlv.Content.Length > 1000)
        {
            return this.CreateDecodeError(
                "ASNCAN0011",
                "CER requires long string values to use constructed fragments.",
                false);
        }

        BerPrimitiveValueResult decoded = this.DecodePrimitiveContent(
            kind,
            tlv.Content,
            rules);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CreateDecodeError(
                decoded.ErrorCode,
                decoded.ErrorMessage,
                decoded.LimitExceeded);
        }

        return new BerPrimitiveDecodeResult
        {
            Success = true,
            Value = decoded.Value
        };
    }

    internal BerPrimitiveValueResult EncodePrimitiveContent(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        if (kind == AsnTypeKind.Boolean)
        {
            return new BerPrimitiveValueResult
            {
                Success = true,
                Content = [value.BooleanValue ? (byte)255 : (byte)0]
            };
        }

        if (kind == AsnTypeKind.Integer
            || kind == AsnTypeKind.Enumerated)
        {
            return new BerIntegerCodec(this.Limits).EncodeInteger(
                value.IntegerValue);
        }

        if (kind == AsnTypeKind.Null)
        {
            return new BerPrimitiveValueResult { Success = true };
        }

        if (kind == AsnTypeKind.BitString)
        {
            return this.EncodeBitString(value.BitStringValue);
        }

        if (kind == AsnTypeKind.OctetString)
        {
            return new BerPrimitiveValueResult
            {
                Success = true,
                Content = this.CopyBytes(value.Octets)
            };
        }

        if (kind == AsnTypeKind.ObjectIdentifier
            || kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            if (value.ObjectIdentifierValue == null)
            {
                return this.CreateValueError(
                    "ASNE0034",
                    "The runtime value does not contain an object identifier.",
                    false);
            }

            return new BerObjectIdentifierCodec(this.Limits)
                .EncodeObjectIdentifier(
                    value.ObjectIdentifierValue,
                    kind == AsnTypeKind.RelativeObjectIdentifier);
        }

        if (kind == AsnTypeKind.Real)
        {
            if (value.RealValue == null)
            {
                return this.CreateValueError(
                    "ASNE0034",
                    "The runtime value does not contain a REAL value.",
                    false);
            }

            return new BerRealCodec(this.Limits).EncodeReal(
                value.RealValue,
                rules);
        }

        return new BerCharacterStringCodec(this.Limits).EncodeText(
            kind,
            value.TextValue);
    }

    internal BerPrimitiveValueResult DecodePrimitiveContent(
        AsnTypeKind kind,
        byte[] content,
        AsnEncodingRules rules)
    {
        if (kind == AsnTypeKind.Boolean)
        {
            return this.DecodeBoolean(content, rules);
        }

        if (kind == AsnTypeKind.Integer
            || kind == AsnTypeKind.Enumerated)
        {
            BerPrimitiveValueResult integer = new BerIntegerCodec(this.Limits)
                .DecodeInteger(content);
            if (integer.Success && integer.Value != null)
            {
                integer.Value.Kind = kind == AsnTypeKind.Integer
                    ? AsnRuntimeValueKind.Integer
                    : AsnRuntimeValueKind.Enumerated;
            }

            return integer;
        }

        if (kind == AsnTypeKind.Null)
        {
            return this.DecodeNull(content);
        }

        if (kind == AsnTypeKind.BitString)
        {
            return this.DecodeBitString(content);
        }

        if (kind == AsnTypeKind.OctetString)
        {
            return new BerPrimitiveValueResult
            {
                Success = true,
                Value = new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OctetString,
                    Octets = this.CopyBytes(content)
                }
            };
        }

        if (kind == AsnTypeKind.ObjectIdentifier
            || kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return new BerObjectIdentifierCodec(this.Limits)
                .DecodeObjectIdentifier(
                    content,
                    kind == AsnTypeKind.RelativeObjectIdentifier);
        }

        if (kind == AsnTypeKind.Real)
        {
            return new BerRealCodec(this.Limits).DecodeReal(content);
        }

        return new BerCharacterStringCodec(this.Limits).DecodeText(
            kind,
            content);
    }

    internal BerPrimitiveValueResult EncodeBitString(
        AsnBitStringValue? value)
    {
        if (value == null || !value.IsValid())
        {
            return this.CreateValueError(
                "ASNE0035",
                "The bit string value is invalid.",
                false);
        }

        byte[] content = new byte[value.Bytes.Length + 1];
        int unused = value.Bytes.Length * 8 - value.BitLength;
        content[0] = (byte)unused;
        System.Buffer.BlockCopy(
            value.Bytes,
            0,
            content,
            1,
            value.Bytes.Length);
        return new BerPrimitiveValueResult
        {
            Success = true,
            Content = content
        };
    }

    internal BerPrimitiveValueResult DecodeBoolean(
        byte[] content,
        AsnEncodingRules rules)
    {
        if (content.Length != 1)
        {
            return this.CreateValueError(
                "ASND0051",
                "BOOLEAN content must contain exactly one octet.",
                false);
        }

        if (rules != AsnEncodingRules.Ber
            && content[0] != 0
            && content[0] != 255)
        {
            return this.CreateValueError(
                "ASND0052",
                "DER and CER require BOOLEAN TRUE to use the all-ones octet.",
                false);
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Boolean,
                HasBooleanValue = true,
                BooleanValue = content[0] != 0
            }
        };
    }

    internal BerPrimitiveValueResult DecodeNull(byte[] content)
    {
        if (content.Length != 0)
        {
            return this.CreateValueError(
                "ASND0053",
                "NULL content must be empty.",
                false);
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null }
        };
    }

    internal BerPrimitiveValueResult DecodeBitString(byte[] content)
    {
        if (content.Length == 0 || content[0] > 7)
        {
            return this.CreateValueError(
                "ASND0054",
                "BIT STRING content requires a valid unused-bit count.",
                false);
        }

        int byteCount = content.Length - 1;
        int unused = content[0];
        if (byteCount == 0 && unused != 0)
        {
            return this.CreateValueError(
                "ASND0055",
                "An empty BIT STRING cannot declare unused bits.",
                false);
        }

        if (unused != 0)
        {
            int mask = (1 << unused) - 1;
            if ((content[content.Length - 1] & mask) != 0)
            {
                return this.CreateValueError(
                    "ASND0056",
                    "Unused BIT STRING bits must be zero.",
                    false);
            }
        }

        if (byteCount > int.MaxValue / 8)
        {
            return this.CreateValueError(
                "ASNR0013",
                "The BIT STRING bit length exceeds the supported range.",
                true);
        }

        byte[] bytes = new byte[byteCount];
        System.Buffer.BlockCopy(content, 1, bytes, 0, byteCount);
        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = bytes,
                    BitLength = byteCount * 8 - unused
                }
            }
        };
    }

    internal bool HasExpectedRuntimeKind(
        AsnTypeKind kind,
        AsnRuntimeValue value)
    {
        if (kind == AsnTypeKind.Boolean)
        {
            return value.Kind == AsnRuntimeValueKind.Boolean
                && value.HasBooleanValue;
        }

        if (kind == AsnTypeKind.Integer)
        {
            return value.Kind == AsnRuntimeValueKind.Integer
                && value.HasIntegerValue;
        }

        if (kind == AsnTypeKind.Enumerated)
        {
            return value.Kind == AsnRuntimeValueKind.Enumerated
                && value.HasIntegerValue;
        }

        if (kind == AsnTypeKind.Real)
        {
            return value.Kind == AsnRuntimeValueKind.Real
                && value.RealValue != null;
        }

        if (kind == AsnTypeKind.BitString)
        {
            return value.Kind == AsnRuntimeValueKind.BitString
                && value.BitStringValue != null;
        }

        if (kind == AsnTypeKind.OctetString)
        {
            return value.Kind == AsnRuntimeValueKind.OctetString
                && value.Octets != null;
        }

        if (kind == AsnTypeKind.Null)
        {
            return value.Kind == AsnRuntimeValueKind.Null;
        }

        if (kind == AsnTypeKind.ObjectIdentifier)
        {
            return value.Kind == AsnRuntimeValueKind.ObjectIdentifier
                && value.ObjectIdentifierValue != null;
        }

        if (kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return value.Kind == AsnRuntimeValueKind.RelativeObjectIdentifier
                && value.ObjectIdentifierValue != null;
        }

        if (kind == AsnTypeKind.ObjectIdentifierIri)
        {
            return value.Kind == AsnRuntimeValueKind.ObjectIdentifierIri
                && value.TextValue != null;
        }

        if (kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return value.Kind == AsnRuntimeValueKind.RelativeObjectIdentifierIri
                && value.TextValue != null;
        }

        return value.Kind == AsnRuntimeValueKind.CharacterString
            && value.TextValue != null;
    }

    internal int GetUniversalTagNumber(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.Boolean)
        {
            return AsnUniversalTags.Boolean;
        }

        if (kind == AsnTypeKind.Integer)
        {
            return AsnUniversalTags.Integer;
        }

        if (kind == AsnTypeKind.Enumerated)
        {
            return AsnUniversalTags.Enumerated;
        }

        if (kind == AsnTypeKind.Real)
        {
            return AsnUniversalTags.Real;
        }

        if (kind == AsnTypeKind.BitString)
        {
            return AsnUniversalTags.BitString;
        }

        if (kind == AsnTypeKind.OctetString)
        {
            return AsnUniversalTags.OctetString;
        }

        if (kind == AsnTypeKind.Null)
        {
            return AsnUniversalTags.Null;
        }

        if (kind == AsnTypeKind.ObjectIdentifier)
        {
            return AsnUniversalTags.ObjectIdentifier;
        }

        if (kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return AsnUniversalTags.RelativeObjectIdentifier;
        }

        if (kind == AsnTypeKind.ObjectIdentifierIri)
        {
            return AsnUniversalTags.ObjectIdentifierIri;
        }

        if (kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return AsnUniversalTags.RelativeObjectIdentifierIri;
        }

        if (kind == AsnTypeKind.Utf8String)
        {
            return AsnUniversalTags.Utf8String;
        }

        if (kind == AsnTypeKind.NumericString)
        {
            return AsnUniversalTags.NumericString;
        }

        if (kind == AsnTypeKind.PrintableString)
        {
            return AsnUniversalTags.PrintableString;
        }

        if (kind == AsnTypeKind.TeletexString)
        {
            return AsnUniversalTags.TeletexString;
        }

        if (kind == AsnTypeKind.VideotexString)
        {
            return AsnUniversalTags.VideotexString;
        }

        if (kind == AsnTypeKind.Ia5String)
        {
            return AsnUniversalTags.Ia5String;
        }

        if (kind == AsnTypeKind.GraphicString)
        {
            return AsnUniversalTags.GraphicString;
        }

        if (kind == AsnTypeKind.VisibleString)
        {
            return AsnUniversalTags.VisibleString;
        }

        if (kind == AsnTypeKind.GeneralString)
        {
            return AsnUniversalTags.GeneralString;
        }

        if (kind == AsnTypeKind.UniversalString)
        {
            return AsnUniversalTags.UniversalString;
        }

        if (kind == AsnTypeKind.BmpString)
        {
            return AsnUniversalTags.BmpString;
        }

        return -1;
    }

    internal bool IsBerFamilyRule(AsnEncodingRules rules)
    {
        return rules == AsnEncodingRules.Ber
            || rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer;
    }

    internal bool AllowsContent(int count)
    {
        ResourceCounter resources = new ResourceCounter(this.Limits);
        return resources.AllowsOpenTypeBytes(count)
            && resources.TryChargeAllocationBytes(count);
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        System.Buffer.BlockCopy(source, 0, copy, 0, source.Length);
        return copy;
    }

    internal BerPrimitiveValueResult CreateValueError(
        string code,
        string message,
        bool resource)
    {
        return new BerPrimitiveValueResult
        {
            LimitExceeded = resource,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal BerPrimitiveEncodeResult CreateEncodeError(
        string code,
        string message,
        bool resource)
    {
        return new BerPrimitiveEncodeResult
        {
            Diagnostics = [this.CreatePrimitiveDiagnostic(
                code,
                message,
                resource,
                true)]
        };
    }

    internal BerPrimitiveDecodeResult CreateDecodeError(
        string code,
        string message,
        bool resource)
    {
        return new BerPrimitiveDecodeResult
        {
            Diagnostics = [this.CreatePrimitiveDiagnostic(
                code,
                message,
                resource,
                false)]
        };
    }

    internal AsnDiagnostic CreatePrimitiveDiagnostic(
        string code,
        string message,
        bool resource,
        bool encoding)
    {
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = resource
                ? AsnDiagnosticCategory.Resource
                : code == "ASNCAN0011"
                    ? AsnDiagnosticCategory.Canonicality
                : encoding
                    ? AsnDiagnosticCategory.Encoding
                    : AsnDiagnosticCategory.Decoding,
            Code = code,
            Message = message
        };
    }
}
