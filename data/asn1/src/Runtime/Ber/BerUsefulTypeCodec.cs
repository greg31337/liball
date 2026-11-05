namespace asn1;

internal class BerUsefulTypeCodec
{
    internal AsnResourceLimits Limits;

    internal BerUsefulTypeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerPrimitiveEncodeResult EncodeUsefulType(
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
            return this.CreateUsefulEncodeError(
                "ASNE0038",
                "BER useful-type encoding requires BER, DER, or CER rules.",
                false);
        }

        BerUsefulValueResult encoded;
        if (this.IsTimeType(kind))
        {
            encoded = this.EncodeTimeType(kind, value, rules);
        }
        else if (kind == AsnTypeKind.ObjectDescriptor)
        {
            encoded = this.EncodeObjectDescriptor(value, rules);
        }
        else if (kind == AsnTypeKind.External)
        {
            encoded = this.EncodeExternalValue(value, rules);
        }
        else if (kind == AsnTypeKind.EmbeddedPdv)
        {
            encoded = this.EncodeAssociatedValue(
                value,
                rules,
                AsnTypeKind.EmbeddedPdv,
                "data-value");
        }
        else if (kind == AsnTypeKind.CharacterString)
        {
            encoded = this.EncodeAssociatedValue(
                value,
                rules,
                AsnTypeKind.CharacterString,
                "string-value");
        }
        else if (kind == AsnTypeKind.InstanceOf)
        {
            encoded = this.EncodeInstanceOfValue(value, rules);
        }
        else if (kind == AsnTypeKind.OpenType)
        {
            encoded = this.EncodeOpenTypeValue(value, rules);
        }
        else
        {
            encoded = this.CreateUsefulValueError(
                "ASNE0039",
                "The requested ASN.1 type is not a BER useful type.",
                false);
        }

        if (!encoded.Success || encoded.Tlv == null)
        {
            return this.CreateUsefulEncodeError(
                encoded.ErrorCode,
                encoded.ErrorMessage,
                encoded.LimitExceeded);
        }

        if (rules == AsnEncodingRules.Cer && encoded.Tlv.Constructed)
        {
            BerConstructedResult canonical = new BerConstructedCodec(
                this.Limits).MakeIndefiniteRecursively(
                    encoded.Tlv,
                    AsnEncodingRules.Cer);
            if (!canonical.Success || canonical.Tlv == null)
            {
                return this.CreateUsefulEncodeError(
                    canonical.ErrorCode,
                    canonical.ErrorMessage,
                    canonical.LimitExceeded);
            }

            encoded.Tlv = canonical.Tlv;
        }

        return new BerPrimitiveEncodeResult
        {
            Success = true,
            Tlv = encoded.Tlv
        };
    }

    internal BerPrimitiveDecodeResult DecodeUsefulType(
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
            return this.CreateUsefulDecodeError(
                "ASND0059",
                "BER useful-type decoding requires BER, DER, or CER rules.",
                false);
        }

        if (!tlv.IsValid())
        {
            return this.CreateUsefulDecodeError(
                "ASND0076",
                "The useful-type TLV tree is internally inconsistent.",
                false);
        }

        BerUsefulValueResult decoded;
        if (this.IsTimeType(kind))
        {
            decoded = this.DecodeTimeType(kind, tlv);
        }
        else if (kind == AsnTypeKind.ObjectDescriptor)
        {
            decoded = this.DecodeObjectDescriptor(tlv, rules);
        }
        else if (kind == AsnTypeKind.External)
        {
            decoded = this.DecodeExternalValue(tlv, rules);
        }
        else if (kind == AsnTypeKind.EmbeddedPdv)
        {
            decoded = this.DecodeAssociatedValue(
                tlv,
                rules,
                AsnTypeKind.EmbeddedPdv,
                "data-value");
        }
        else if (kind == AsnTypeKind.CharacterString)
        {
            decoded = this.DecodeAssociatedValue(
                tlv,
                rules,
                AsnTypeKind.CharacterString,
                "string-value");
        }
        else if (kind == AsnTypeKind.InstanceOf)
        {
            decoded = this.DecodeInstanceOfValue(tlv, rules);
        }
        else if (kind == AsnTypeKind.OpenType)
        {
            decoded = this.DecodeOpenTypeValue(tlv, rules);
        }
        else
        {
            decoded = this.CreateUsefulValueError(
                "ASND0060",
                "The requested ASN.1 type is not a BER useful type.",
                false);
        }

        if (!decoded.Success || decoded.Value == null)
        {
            return this.CreateUsefulDecodeError(
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

    internal BerUsefulValueResult EncodeTimeType(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        if (value.Kind != AsnRuntimeValueKind.Time || value.TimeValue == null)
        {
            return this.CreateUsefulValueError(
                "ASNE0040",
                "The runtime value does not contain the requested time value.",
                false);
        }

        BerTimeCodec times = new BerTimeCodec(this.Limits);
        BerPrimitiveValueResult encoded = times.EncodeTime(
            kind,
            value.TimeValue,
            rules);
        if (!encoded.Success)
        {
            return this.CopyPrimitiveFailure(encoded);
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Tlv = this.CreateUniversalPrimitive(
                times.GetUniversalTagNumber(kind),
                encoded.Content)
        };
    }

    internal BerUsefulValueResult DecodeTimeType(
        AsnTypeKind kind,
        BerTlv tlv)
    {
        BerTimeCodec times = new BerTimeCodec(this.Limits);
        if (!this.IsPrimitiveWithTag(
            tlv,
            AsnTagClass.Universal,
            times.GetUniversalTagNumber(kind)))
        {
            return this.CreateUsefulValueError(
                "ASND0061",
                "The encoded time value has the wrong tag or form.",
                false);
        }

        BerPrimitiveValueResult decoded = times.DecodeTime(kind, tlv.Content);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CopyPrimitiveFailure(decoded);
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Value = decoded.Value
        };
    }

    internal BerUsefulValueResult EncodeObjectDescriptor(
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        if (value.Kind != AsnRuntimeValueKind.CharacterString
            || value.TextValue == null)
        {
            return this.CreateUsefulValueError(
                "ASNE0041",
                "ObjectDescriptor requires a character-string runtime value.",
                false);
        }

        BerPrimitiveValueResult encoded = new BerCharacterStringCodec(
            this.Limits).EncodeText(
                AsnTypeKind.GraphicString,
                value.TextValue);
        if (!encoded.Success)
        {
            return this.CopyPrimitiveFailure(encoded);
        }

        BerTlv descriptor = this.CreateUniversalPrimitive(
            AsnUniversalTags.ObjectDescriptor,
            encoded.Content);
        if (rules == AsnEncodingRules.Cer)
        {
            BerStringResult canonical = new BerStringCodec(this.Limits)
                .SelectCerStringForm(
                    AsnTypeKind.GraphicString,
                    descriptor);
            if (!canonical.Success || canonical.Tlv == null)
            {
                return this.CreateUsefulValueError(
                    canonical.ErrorCode,
                    canonical.ErrorMessage,
                    canonical.LimitExceeded);
            }

            descriptor = canonical.Tlv;
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Tlv = descriptor
        };
    }

    internal BerUsefulValueResult DecodeObjectDescriptor(
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        if (!this.HasTag(
            tlv,
            AsnTagClass.Universal,
            AsnUniversalTags.ObjectDescriptor))
        {
            return this.CreateUsefulValueError(
                "ASND0062",
                "ObjectDescriptor requires primitive universal tag 7.",
                false);
        }

        BerStringResult decoded = new BerStringCodec(this.Limits)
            .DecodeTaggedConstructedString(
                AsnTypeKind.GraphicString,
                tlv,
                rules,
                AsnUniversalTags.ObjectDescriptor);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CreateUsefulValueError(
                decoded.ErrorCode,
                decoded.ErrorMessage,
                decoded.LimitExceeded);
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Value = decoded.Value
        };
    }

    internal BerUsefulValueResult EncodeOpenTypeValue(
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        if (value.Kind != AsnRuntimeValueKind.OpenType
            || value.OpenTypeValue == null
            || !new OpaquePayloadPolicy().CanReuseOpenType(
                value.OpenTypeValue,
                rules))
        {
            return this.CreateUsefulValueError(
                "ASNE0042",
                "An open type requires a valid complete encoding under the selected rules.",
                false);
        }

        byte[] data = value.OpenTypeValue.OriginalData;
        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(data.Length))
        {
            return this.CreateUsefulValueError(
                "ASNR0001",
                "The open-type encoding exceeds the configured byte limit.",
                true);
        }

        BerReadResult read = new BerTlvReaderImplementation(this.Limits).Read(
            new BerReadRequest
            {
                Data = data,
                HasCount = true,
                Count = data.Length,
                EncodingRules = rules,
                PreserveEncodedData = true
            });
        if (read.Status != AsnDecodeStatus.Done
            || read.Tlv == null
            || read.ConsumedCount != data.Length)
        {
            return this.CreateUsefulValueError(
                "ASNE0043",
                "The open-type data is not one complete BER value.",
                false);
        }

        if (rules == AsnEncodingRules.Ber)
        {
            read.Tlv.UseEncodedData = true;
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Tlv = read.Tlv
        };
    }

    internal BerUsefulValueResult DecodeOpenTypeValue(
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        byte[] data;
        if (tlv.EncodedData != null && tlv.EncodedData.Length > 0)
        {
            data = this.CopyBytes(tlv.EncodedData);
            BerReadResult retained = new BerTlvReaderImplementation(
                this.Limits).Read(new BerReadRequest
                {
                    Data = data,
                    HasCount = true,
                    Count = data.Length,
                    EncodingRules = rules
                });
            if (retained.Status != AsnDecodeStatus.Done
                || retained.Tlv == null
                || retained.ConsumedCount != data.Length
                || retained.Tlv.Tag.Class != tlv.Tag.Class
                || retained.Tlv.Tag.Number != tlv.Tag.Number
                || retained.Tlv.Constructed != tlv.Constructed)
            {
                return this.CreateUsefulValueError(
                    "ASND0077",
                    "The retained open-type bytes do not encode the supplied TLV.",
                    false);
            }
        }
        else
        {
            BerWriteResult written = new BerTlvWriterImplementation(
                this.Limits).Write(new BerWriteRequest
                {
                    Tlv = tlv,
                    EncodingRules = rules
                });
            if (!written.Success)
            {
                return this.CreateUsefulValueError(
                    "ASND0063",
                    "The open-type TLV cannot be serialized under the selected rules.",
                    false);
            }

            data = written.Data;
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(data.Length))
        {
            return this.CreateUsefulValueError(
                "ASNR0001",
                "The open-type encoding exceeds the configured byte limit.",
                true);
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OpenType,
                OpenTypeValue = new AsnOpenTypeValue
                {
                    Unresolved = true,
                    HasEncodingRules = true,
                    EncodingRules = rules,
                    HasOriginalData = true,
                    OriginalData = data,
                    OriginalTag = new AsnTag
                    {
                        Class = tlv.Tag.Class,
                        Number = tlv.Tag.Number
                    },
                    HasEncodedLocation = tlv.EncodedOffset >= 0,
                    EncodedOffset = tlv.EncodedOffset,
                    EncodedLength = data.Length
                }
            }
        };
    }

    internal BerUsefulValueResult EncodeAssociatedValue(
        AsnRuntimeValue value,
        AsnEncodingRules rules,
        AsnTypeKind kind,
        string dataFieldName)
    {
        AsnRuntimeValueKind expected = kind == AsnTypeKind.EmbeddedPdv
            ? AsnRuntimeValueKind.EmbeddedPdv
            : AsnRuntimeValueKind.CharacterString;
        if (value.Kind != expected
            || !this.HasOnlyPresentFields(
                value,
                ["identification", dataFieldName])
            || this.CountPresentField(value, "identification") != 1
            || this.CountPresentField(value, dataFieldName) != 1)
        {
            return this.CreateUsefulValueError(
                "ASNE0044",
                "The associated useful-type value has invalid or duplicate fields.",
                false);
        }

        AsnRuntimeValue? identification = this.GetPresentFieldValue(
            value,
            "identification");
        AsnRuntimeValue? data = this.GetPresentFieldValue(
            value,
            dataFieldName);
        if (identification == null || data == null)
        {
            return this.CreateUsefulValueError(
                "ASNE0045",
                "The associated useful type requires identification and data fields.",
                false);
        }

        BerUsefulValueResult encodedIdentification =
            this.EncodeIdentificationValue(identification, rules);
        if (!encodedIdentification.Success
            || encodedIdentification.Tlv == null)
        {
            return encodedIdentification;
        }

        System.Collections.Generic.List<BerTlv> children =
            new System.Collections.Generic.List<BerTlv>();
        children.Add(this.CreateContextConstructed(
            0,
            [encodedIdentification.Tlv]));

        BerUsefulValueResult encodedData = this.EncodePrimitiveChild(
            AsnTypeKind.OctetString,
            data,
            rules);
        if (!encodedData.Success || encodedData.Tlv == null)
        {
            return encodedData;
        }

        children.Add(this.CreateContextImplicit(2, encodedData.Tlv));
        int tag = kind == AsnTypeKind.EmbeddedPdv
            ? AsnUniversalTags.EmbeddedPdv
            : AsnUniversalTags.CharacterString;
        return new BerUsefulValueResult
        {
            Success = true,
            Tlv = this.CreateUniversalConstructed(tag, children.ToArray())
        };
    }

    internal BerUsefulValueResult DecodeAssociatedValue(
        BerTlv tlv,
        AsnEncodingRules rules,
        AsnTypeKind kind,
        string dataFieldName)
    {
        int tag = kind == AsnTypeKind.EmbeddedPdv
            ? AsnUniversalTags.EmbeddedPdv
            : AsnUniversalTags.CharacterString;
        if (!this.IsConstructedWithTag(tlv, AsnTagClass.Universal, tag)
            || tlv.Children.Length != 2)
        {
            return this.CreateUsefulValueError(
                "ASND0064",
                "The associated useful type has the wrong tag or component count.",
                false);
        }

        BerTlv identificationWrapper = tlv.Children[0];
        if (!this.IsConstructedWithTag(
            identificationWrapper,
            AsnTagClass.ContextSpecific,
            0)
            || identificationWrapper.Children.Length != 1)
        {
            return this.CreateUsefulValueError(
                "ASND0065",
                "The associated useful type requires one explicitly wrapped identification value.",
                false);
        }

        BerUsefulValueResult identification =
            this.DecodeIdentificationValue(
                identificationWrapper.Children[0],
                rules);
        if (!identification.Success || identification.Value == null)
        {
            return identification;
        }

        System.Collections.Generic.List<AsnRuntimeFieldValue> fields =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        fields.Add(this.CreatePresentField(
            "identification",
            identification.Value));
        BerTlv encodedData = tlv.Children[1];
        if (!this.HasTag(encodedData, AsnTagClass.ContextSpecific, 2))
        {
            return this.CreateUsefulValueError(
                "ASND0065",
                "The associated useful type requires implicitly tagged [2] data.",
                false);
        }

        BerUsefulValueResult data = this.DecodeImplicitPrimitive(
            AsnTypeKind.OctetString,
            encodedData,
            rules);
        if (!data.Success || data.Value == null)
        {
            return data;
        }

        fields.Add(this.CreatePresentField(dataFieldName, data.Value));
        return new BerUsefulValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = kind == AsnTypeKind.EmbeddedPdv
                    ? AsnRuntimeValueKind.EmbeddedPdv
                    : AsnRuntimeValueKind.CharacterString,
                Fields = fields.ToArray()
            }
        };
    }

    internal BerUsefulValueResult EncodeIdentificationValue(
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        string[] alternatives =
        [
            "syntaxes",
            "syntax",
            "presentation-context-id",
            "context-negotiation",
            "transfer-syntax",
            "fixed"
        ];
        if (value.Kind != AsnRuntimeValueKind.Choice
            || !this.HasOnlyPresentFields(value, alternatives))
        {
            return this.CreateUsefulValueError(
                "ASNE0046",
                "Identification must select one recognized alternative.",
                false);
        }

        int selected = -1;
        int index = 0;
        while (index < alternatives.Length)
        {
            if (this.CountPresentField(value, alternatives[index]) == 1)
            {
                if (selected >= 0)
                {
                    return this.CreateUsefulValueError(
                        "ASNE0046",
                        "Identification must select exactly one alternative.",
                        false);
                }

                selected = index;
            }

            index = index + 1;
        }

        if (selected < 0)
        {
            return this.CreateUsefulValueError(
                "ASNE0046",
                "Identification must select exactly one alternative.",
                false);
        }

        AsnRuntimeValue? selectedValue = this.GetPresentFieldValue(
            value,
            alternatives[selected]);
        if (selectedValue == null)
        {
            return this.CreateUsefulValueError(
                "ASNE0047",
                "The selected identification alternative has no value.",
                false);
        }

        if (selected == 0)
        {
            return this.EncodeIdentificationPair(
                selectedValue,
                rules,
                0,
                "abstract",
                "transfer");
        }

        if (selected == 3)
        {
            return this.EncodeIdentificationPair(
                selectedValue,
                rules,
                3,
                "presentation-context-id",
                "transfer-syntax");
        }

        AsnTypeKind primitiveKind = selected == 1 || selected == 4
            ? AsnTypeKind.ObjectIdentifier
            : selected == 2
                ? AsnTypeKind.Integer
                : AsnTypeKind.Null;
        BerUsefulValueResult primitive = this.EncodePrimitiveChild(
            primitiveKind,
            selectedValue,
            rules);
        if (!primitive.Success || primitive.Tlv == null)
        {
            return primitive;
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Tlv = this.CreateContextPrimitive(
                selected,
                primitive.Tlv.Content)
        };
    }

    internal BerUsefulValueResult DecodeIdentificationValue(
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        if (tlv.Tag == null
            || tlv.Tag.Class != AsnTagClass.ContextSpecific
            || tlv.Tag.Number < System.Numerics.BigInteger.Zero
            || tlv.Tag.Number > new System.Numerics.BigInteger(5))
        {
            return this.CreateUsefulValueError(
                "ASND0066",
                "Identification requires a context-specific alternative from 0 through 5.",
                false);
        }

        int alternative = (int)tlv.Tag.Number;
        string name = alternative == 0
            ? "syntaxes"
            : alternative == 1
                ? "syntax"
                : alternative == 2
                    ? "presentation-context-id"
                    : alternative == 3
                        ? "context-negotiation"
                        : alternative == 4
                            ? "transfer-syntax"
                            : "fixed";
        BerUsefulValueResult selected;
        if (alternative == 0)
        {
            selected = this.DecodeIdentificationPair(
                tlv,
                rules,
                "abstract",
                "transfer");
        }
        else if (alternative == 3)
        {
            selected = this.DecodeIdentificationPair(
                tlv,
                rules,
                "presentation-context-id",
                "transfer-syntax");
        }
        else
        {
            AsnTypeKind primitiveKind = alternative == 1 || alternative == 4
                ? AsnTypeKind.ObjectIdentifier
                : alternative == 2
                    ? AsnTypeKind.Integer
                    : AsnTypeKind.Null;
            selected = this.DecodeImplicitPrimitive(
                primitiveKind,
                tlv,
                rules);
        }

        if (!selected.Success || selected.Value == null)
        {
            return selected;
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Choice,
                Fields = [this.CreatePresentField(name, selected.Value)]
            }
        };
    }

    internal BerUsefulValueResult EncodeIdentificationPair(
        AsnRuntimeValue value,
        AsnEncodingRules rules,
        int tag,
        string firstName,
        string secondName)
    {
        if (value.Kind != AsnRuntimeValueKind.Sequence
            || !this.HasOnlyPresentFields(value, [firstName, secondName])
            || this.CountPresentField(value, firstName) != 1
            || this.CountPresentField(value, secondName) != 1)
        {
            return this.CreateUsefulValueError(
                "ASNE0048",
                "The identification sequence requires both fields exactly once.",
                false);
        }

        AsnRuntimeValue? first = this.GetPresentFieldValue(value, firstName);
        AsnRuntimeValue? second = this.GetPresentFieldValue(value, secondName);
        if (first == null || second == null)
        {
            return this.CreateUsefulValueError(
                "ASNE0048",
                "The identification sequence requires values for both fields.",
                false);
        }

        AsnTypeKind firstKind = tag == 0
            ? AsnTypeKind.ObjectIdentifier
            : AsnTypeKind.Integer;
        BerUsefulValueResult encodedFirst = this.EncodePrimitiveChild(
            firstKind,
            first,
            rules);
        BerUsefulValueResult encodedSecond = this.EncodePrimitiveChild(
            AsnTypeKind.ObjectIdentifier,
            second,
            rules);
        if (!encodedFirst.Success || encodedFirst.Tlv == null)
        {
            return encodedFirst;
        }

        if (!encodedSecond.Success || encodedSecond.Tlv == null)
        {
            return encodedSecond;
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Tlv = this.CreateContextConstructed(
                tag,
                [
                    this.CreateContextPrimitive(0, encodedFirst.Tlv.Content),
                    this.CreateContextPrimitive(1, encodedSecond.Tlv.Content)
                ])
        };
    }

    internal BerUsefulValueResult DecodeIdentificationPair(
        BerTlv tlv,
        AsnEncodingRules rules,
        string firstName,
        string secondName)
    {
        if (!tlv.Constructed
            || tlv.IndefiniteLength && tlv.Children == null
            || tlv.Content == null
            || tlv.Content.Length != 0
            || tlv.Children == null
            || tlv.Children.Length != 2)
        {
            return this.CreateUsefulValueError(
                "ASND0067",
                "The identification sequence requires two constructed components.",
                false);
        }

        AsnTypeKind firstKind = firstName == "abstract"
            ? AsnTypeKind.ObjectIdentifier
            : AsnTypeKind.Integer;
        if (!this.HasTag(
            tlv.Children[0],
            AsnTagClass.ContextSpecific,
            0)
            || !this.HasTag(
                tlv.Children[1],
                AsnTagClass.ContextSpecific,
                1))
        {
            return this.CreateUsefulValueError(
                "ASND0067",
                "The identification sequence components require automatic tags [0] and [1].",
                false);
        }

        BerUsefulValueResult first = this.DecodeImplicitPrimitive(
            firstKind,
            tlv.Children[0],
            rules);
        BerUsefulValueResult second = this.DecodeImplicitPrimitive(
            AsnTypeKind.ObjectIdentifier,
            tlv.Children[1],
            rules);
        if (!first.Success || first.Value == null)
        {
            return first;
        }

        if (!second.Success || second.Value == null)
        {
            return second;
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    this.CreatePresentField(firstName, first.Value),
                    this.CreatePresentField(secondName, second.Value)
                ]
            }
        };
    }

    internal BerUsefulValueResult EncodeExternalValue(
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        string[] fields =
        [
            "direct-reference",
            "indirect-reference",
            "data-value-descriptor",
            "single-ASN1-type",
            "octet-aligned",
            "arbitrary"
        ];
        if (value.Kind != AsnRuntimeValueKind.External
            || !this.HasOnlyPresentFields(value, fields)
            || this.CountPresentField(value, "direct-reference") > 1
            || this.CountPresentField(value, "indirect-reference") > 1
            || this.CountPresentField(value, "data-value-descriptor") > 1)
        {
            return this.CreateUsefulValueError(
                "ASNE0049",
                "The EXTERNAL value has invalid or duplicate fields.",
                false);
        }

        int references = this.CountPresentField(value, "direct-reference")
            + this.CountPresentField(value, "indirect-reference");
        int encodings = this.CountPresentField(value, "single-ASN1-type")
            + this.CountPresentField(value, "octet-aligned")
            + this.CountPresentField(value, "arbitrary");
        if (references == 0 || encodings != 1)
        {
            return this.CreateUsefulValueError(
                "ASNE0050",
                "EXTERNAL requires identification and exactly one encoding alternative.",
                false);
        }

        System.Collections.Generic.List<BerTlv> children =
            new System.Collections.Generic.List<BerTlv>();
        AsnRuntimeValue? direct = this.GetPresentFieldValue(
            value,
            "direct-reference");
        if (direct != null)
        {
            BerUsefulValueResult encodedDirect = this.EncodePrimitiveChild(
                AsnTypeKind.ObjectIdentifier,
                direct,
                rules);
            if (!encodedDirect.Success || encodedDirect.Tlv == null)
            {
                return encodedDirect;
            }

            children.Add(encodedDirect.Tlv);
        }

        AsnRuntimeValue? indirect = this.GetPresentFieldValue(
            value,
            "indirect-reference");
        if (indirect != null)
        {
            BerUsefulValueResult encodedIndirect = this.EncodePrimitiveChild(
                AsnTypeKind.Integer,
                indirect,
                rules);
            if (!encodedIndirect.Success || encodedIndirect.Tlv == null)
            {
                return encodedIndirect;
            }

            children.Add(encodedIndirect.Tlv);
        }

        AsnRuntimeValue? descriptor = this.GetPresentFieldValue(
            value,
            "data-value-descriptor");
        if (descriptor != null)
        {
            BerUsefulValueResult encodedDescriptor =
                this.EncodeObjectDescriptor(descriptor, rules);
            if (!encodedDescriptor.Success || encodedDescriptor.Tlv == null)
            {
                return encodedDescriptor;
            }

            children.Add(encodedDescriptor.Tlv);
        }

        BerUsefulValueResult encoding = this.EncodeExternalEncoding(
            value,
            rules);
        if (!encoding.Success || encoding.Tlv == null)
        {
            return encoding;
        }

        children.Add(encoding.Tlv);
        return new BerUsefulValueResult
        {
            Success = true,
            Tlv = this.CreateUniversalConstructed(
                AsnUniversalTags.External,
                children.ToArray())
        };
    }

    internal BerUsefulValueResult EncodeExternalEncoding(
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        AsnRuntimeValue? single = this.GetPresentFieldValue(
            value,
            "single-ASN1-type");
        if (single != null)
        {
            BerUsefulValueResult open = this.EncodeOpenTypeValue(single, rules);
            if (!open.Success || open.Tlv == null)
            {
                return open;
            }

            return new BerUsefulValueResult
            {
                Success = true,
                Tlv = this.CreateContextConstructed(0, [open.Tlv])
            };
        }

        AsnRuntimeValue? octets = this.GetPresentFieldValue(
            value,
            "octet-aligned");
        if (octets != null)
        {
            BerUsefulValueResult encoded = this.EncodePrimitiveChild(
                AsnTypeKind.OctetString,
                octets,
                rules);
            if (!encoded.Success || encoded.Tlv == null)
            {
                return encoded;
            }

            return new BerUsefulValueResult
            {
                Success = true,
                Tlv = this.CreateContextImplicit(1, encoded.Tlv)
            };
        }

        AsnRuntimeValue? bits = this.GetPresentFieldValue(value, "arbitrary");
        if (bits == null)
        {
            return this.CreateUsefulValueError(
                "ASNE0051",
                "The EXTERNAL encoding alternative has no value.",
                false);
        }

        BerUsefulValueResult encodedBits = this.EncodePrimitiveChild(
            AsnTypeKind.BitString,
            bits,
            rules);
        if (!encodedBits.Success || encodedBits.Tlv == null)
        {
            return encodedBits;
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Tlv = this.CreateContextImplicit(2, encodedBits.Tlv)
        };
    }

    internal BerUsefulValueResult DecodeExternalValue(
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        if (!this.IsConstructedWithTag(
            tlv,
            AsnTagClass.Universal,
            AsnUniversalTags.External)
            || tlv.Children.Length < 2
            || tlv.Children.Length > 4)
        {
            return this.CreateUsefulValueError(
                "ASND0068",
                "EXTERNAL requires universal tag 8 and its historical component sequence.",
                false);
        }

        int index = 0;
        int references = 0;
        System.Collections.Generic.List<AsnRuntimeFieldValue> fields =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        if (index < tlv.Children.Length
            && this.HasTag(
                tlv.Children[index],
                AsnTagClass.Universal,
                AsnUniversalTags.ObjectIdentifier))
        {
            BerUsefulValueResult direct = this.DecodePrimitiveChild(
                AsnTypeKind.ObjectIdentifier,
                tlv.Children[index],
                rules);
            if (!direct.Success || direct.Value == null)
            {
                return direct;
            }

            fields.Add(this.CreatePresentField(
                "direct-reference",
                direct.Value));
            references = references + 1;
            index = index + 1;
        }

        if (index < tlv.Children.Length
            && this.HasTag(
                tlv.Children[index],
                AsnTagClass.Universal,
                AsnUniversalTags.Integer))
        {
            BerUsefulValueResult indirect = this.DecodePrimitiveChild(
                AsnTypeKind.Integer,
                tlv.Children[index],
                rules);
            if (!indirect.Success || indirect.Value == null)
            {
                return indirect;
            }

            fields.Add(this.CreatePresentField(
                "indirect-reference",
                indirect.Value));
            references = references + 1;
            index = index + 1;
        }

        if (index < tlv.Children.Length
            && this.HasTag(
                tlv.Children[index],
                AsnTagClass.Universal,
                AsnUniversalTags.ObjectDescriptor))
        {
            BerUsefulValueResult descriptor = this.DecodeObjectDescriptor(
                tlv.Children[index],
                rules);
            if (!descriptor.Success || descriptor.Value == null)
            {
                return descriptor;
            }

            fields.Add(this.CreatePresentField(
                "data-value-descriptor",
                descriptor.Value));
            index = index + 1;
        }

        if (references == 0 || index != tlv.Children.Length - 1)
        {
            return this.CreateUsefulValueError(
                "ASND0069",
                "EXTERNAL identification is absent or its components are out of order.",
                false);
        }

        BerUsefulValueResult encoding = this.DecodeExternalEncoding(
            tlv.Children[index],
            rules);
        if (!encoding.Success || encoding.Value == null)
        {
            return encoding;
        }

        fields.Add(encoding.Value.Fields[0]);
        return new BerUsefulValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.External,
                Fields = fields.ToArray()
            }
        };
    }

    internal BerUsefulValueResult DecodeExternalEncoding(
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        if (tlv.Tag == null
            || tlv.Tag.Class != AsnTagClass.ContextSpecific
            || tlv.Tag.Number < System.Numerics.BigInteger.Zero
            || tlv.Tag.Number > new System.Numerics.BigInteger(2))
        {
            return this.CreateUsefulValueError(
                "ASND0070",
                "EXTERNAL requires encoding alternative [0], [1], or [2].",
                false);
        }

        int alternative = (int)tlv.Tag.Number;
        BerUsefulValueResult decoded;
        string name;
        if (alternative == 0)
        {
            if (!tlv.Constructed
                || tlv.Content == null
                || tlv.Content.Length != 0
                || tlv.Children == null
                || tlv.Children.Length != 1)
            {
                return this.CreateUsefulValueError(
                    "ASND0071",
                    "EXTERNAL single-ASN1-type requires one explicitly wrapped value.",
                    false);
            }

            decoded = this.DecodeOpenTypeValue(tlv.Children[0], rules);
            name = "single-ASN1-type";
        }
        else
        {
            decoded = this.DecodeImplicitPrimitive(
                alternative == 1
                    ? AsnTypeKind.OctetString
                    : AsnTypeKind.BitString,
                tlv,
                rules);
            name = alternative == 1 ? "octet-aligned" : "arbitrary";
        }

        if (!decoded.Success || decoded.Value == null)
        {
            return decoded;
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Choice,
                Fields = [this.CreatePresentField(name, decoded.Value)]
            }
        };
    }

    internal BerUsefulValueResult EncodeInstanceOfValue(
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        if (value.Kind != AsnRuntimeValueKind.InstanceOf
            || !this.HasOnlyPresentFields(value, ["type-id", "value"])
            || this.CountPresentField(value, "type-id") != 1
            || this.CountPresentField(value, "value") != 1)
        {
            return this.CreateUsefulValueError(
                "ASNE0052",
                "INSTANCE OF requires type-id and value fields exactly once.",
                false);
        }

        AsnRuntimeValue? typeId = this.GetPresentFieldValue(value, "type-id");
        AsnRuntimeValue? openValue = this.GetPresentFieldValue(value, "value");
        if (typeId == null || openValue == null)
        {
            return this.CreateUsefulValueError(
                "ASNE0052",
                "INSTANCE OF fields require values.",
                false);
        }

        BerUsefulValueResult encodedId = this.EncodePrimitiveChild(
            AsnTypeKind.ObjectIdentifier,
            typeId,
            rules);
        BerUsefulValueResult encodedOpen = this.EncodeOpenTypeValue(
            openValue,
            rules);
        if (!encodedId.Success || encodedId.Tlv == null)
        {
            return encodedId;
        }

        if (!encodedOpen.Success || encodedOpen.Tlv == null)
        {
            return encodedOpen;
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Tlv = this.CreateUniversalConstructed(
                AsnUniversalTags.InstanceOf,
                [
                    encodedId.Tlv,
                    this.CreateContextConstructed(0, [encodedOpen.Tlv])
                ])
        };
    }

    internal BerUsefulValueResult DecodeInstanceOfValue(
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        if (!this.IsConstructedWithTag(
            tlv,
            AsnTagClass.Universal,
            AsnUniversalTags.InstanceOf)
            || tlv.Children.Length != 2)
        {
            return this.CreateUsefulValueError(
                "ASND0072",
                "INSTANCE OF requires universal tag 8 and two components.",
                false);
        }

        BerUsefulValueResult typeId = this.DecodePrimitiveChild(
            AsnTypeKind.ObjectIdentifier,
            tlv.Children[0],
            rules);
        BerTlv wrapper = tlv.Children[1];
        if (!typeId.Success || typeId.Value == null)
        {
            return typeId;
        }

        if (!this.IsConstructedWithTag(
            wrapper,
            AsnTagClass.ContextSpecific,
            0)
            || wrapper.Children.Length != 1)
        {
            return this.CreateUsefulValueError(
                "ASND0073",
                "INSTANCE OF value requires one explicitly wrapped [0] value.",
                false);
        }

        BerUsefulValueResult open = this.DecodeOpenTypeValue(
            wrapper.Children[0],
            rules);
        if (!open.Success || open.Value == null)
        {
            return open;
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.InstanceOf,
                Fields =
                [
                    this.CreatePresentField("type-id", typeId.Value),
                    this.CreatePresentField("value", open.Value)
                ]
            }
        };
    }

    internal BerUsefulValueResult EncodePrimitiveChild(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        BerPrimitiveEncodeResult encoded = new BerPrimitiveCodec(this.Limits)
            .EncodePrimitive(kind, value, rules);
        if (!encoded.Success || encoded.Tlv == null)
        {
            if (encoded.Diagnostics.Length == 0)
            {
                return this.CreateUsefulValueError(
                    "ASNE0053",
                    "A useful-type component could not be encoded.",
                    false);
            }

            AsnDiagnostic diagnostic = encoded.Diagnostics[0];
            return this.CreateUsefulValueError(
                diagnostic.Code,
                diagnostic.Message,
                diagnostic.Category == AsnDiagnosticCategory.Resource);
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Tlv = encoded.Tlv
        };
    }

    internal BerUsefulValueResult DecodePrimitiveChild(
        AsnTypeKind kind,
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        BerPrimitiveDecodeResult decoded = new BerPrimitiveCodec(this.Limits)
            .DecodePrimitive(kind, tlv, rules);
        if (!decoded.Success || decoded.Value == null)
        {
            if (decoded.Diagnostics.Length == 0)
            {
                return this.CreateUsefulValueError(
                    "ASND0074",
                    "A useful-type component could not be decoded.",
                    false);
            }

            AsnDiagnostic diagnostic = decoded.Diagnostics[0];
            return this.CreateUsefulValueError(
                diagnostic.Code,
                diagnostic.Message,
                diagnostic.Category == AsnDiagnosticCategory.Resource);
        }

        return new BerUsefulValueResult
        {
            Success = true,
            Value = decoded.Value
        };
    }

    internal BerUsefulValueResult DecodeImplicitPrimitive(
        AsnTypeKind kind,
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        BerStringCodec strings = new BerStringCodec(this.Limits);
        if (tlv.Constructed
            && rules == AsnEncodingRules.Cer
            && strings.IsSupportedStringKind(kind))
        {
            int stringTag = new BerPrimitiveCodec(this.Limits)
                .GetUniversalTagNumber(kind);
            BerTlv stringTlv = this.CreateUniversalConstructed(
                stringTag,
                tlv.Children);
            stringTlv.IndefiniteLength = tlv.IndefiniteLength;
            BerStringResult decodedString = strings.DecodeConstructedString(
                kind,
                stringTlv,
                rules);
            if (!decodedString.Success || decodedString.Value == null)
            {
                return this.CreateUsefulValueError(
                    decodedString.ErrorCode,
                    decodedString.ErrorMessage,
                    decodedString.LimitExceeded);
            }

            return new BerUsefulValueResult
            {
                Success = true,
                Value = decodedString.Value
            };
        }

        if (tlv.Constructed
            || tlv.IndefiniteLength
            || tlv.Content == null
            || tlv.Children == null
            || tlv.Children.Length != 0)
        {
            return this.CreateUsefulValueError(
                "ASND0075",
                "The implicitly tagged useful-type component must be primitive.",
                false);
        }

        int universalTag = new BerPrimitiveCodec(this.Limits)
            .GetUniversalTagNumber(kind);
        BerTlv universal = this.CreateUniversalPrimitive(
            universalTag,
            tlv.Content);
        return this.DecodePrimitiveChild(kind, universal, rules);
    }

    internal BerUsefulValueResult CopyPrimitiveFailure(
        BerPrimitiveValueResult result)
    {
        return this.CreateUsefulValueError(
            result.ErrorCode,
            result.ErrorMessage,
            result.LimitExceeded);
    }

    internal AsnRuntimeValue? GetPresentFieldValue(
        AsnRuntimeValue source,
        string name)
    {
        if (source.Fields == null)
        {
            return null;
        }

        int index = 0;
        while (index < source.Fields.Length)
        {
            AsnRuntimeFieldValue field = source.Fields[index];
            if (field != null
                && field.Present
                && field.Name == name
                && field.Value != null)
            {
                return field.Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal int CountPresentField(AsnRuntimeValue source, string name)
    {
        if (source.Fields == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < source.Fields.Length)
        {
            AsnRuntimeFieldValue field = source.Fields[index];
            if (field != null && field.Present && field.Name == name)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool HasOnlyPresentFields(
        AsnRuntimeValue source,
        string[] allowedNames)
    {
        if (source.Fields == null
            || !new ResourceCounter(this.Limits).AllowsCollectionElements(
                source.Fields.Length))
        {
            return false;
        }

        int index = 0;
        while (index < source.Fields.Length)
        {
            AsnRuntimeFieldValue field = source.Fields[index];
            if (field == null || field.Name == null)
            {
                return false;
            }

            if (field.Present)
            {
                bool allowed = false;
                int nameIndex = 0;
                while (nameIndex < allowedNames.Length)
                {
                    if (field.Name == allowedNames[nameIndex])
                    {
                        allowed = true;
                    }

                    nameIndex = nameIndex + 1;
                }

                if (!allowed || field.Value == null)
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return true;
    }

    internal AsnRuntimeFieldValue CreatePresentField(
        string name,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeFieldValue
        {
            Name = name,
            Present = true,
            Value = value
        };
    }

    internal BerTlv CreateUniversalPrimitive(int tag, byte[] content)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = new System.Numerics.BigInteger(tag)
            },
            Content = this.CopyBytes(content)
        };
    }

    internal BerTlv CreateUniversalConstructed(int tag, BerTlv[] children)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = new System.Numerics.BigInteger(tag)
            },
            Constructed = true,
            Children = children
        };
    }

    internal BerTlv CreateContextPrimitive(int tag, byte[] content)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.ContextSpecific,
                Number = new System.Numerics.BigInteger(tag)
            },
            Content = this.CopyBytes(content)
        };
    }

    internal BerTlv CreateContextConstructed(int tag, BerTlv[] children)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.ContextSpecific,
                Number = new System.Numerics.BigInteger(tag)
            },
            Constructed = true,
            Children = children
        };
    }

    internal BerTlv CreateContextImplicit(int tag, BerTlv value)
    {
        value.Tag = new AsnTag
        {
            Class = AsnTagClass.ContextSpecific,
            Number = new System.Numerics.BigInteger(tag)
        };
        return value;
    }

    internal bool HasTag(BerTlv tlv, AsnTagClass tagClass, int number)
    {
        return tlv != null
            && tlv.Tag != null
            && tlv.Tag.Class == tagClass
            && tlv.Tag.Number == new System.Numerics.BigInteger(number);
    }

    internal bool IsPrimitiveWithTag(
        BerTlv tlv,
        AsnTagClass tagClass,
        int number)
    {
        return this.HasTag(tlv, tagClass, number)
            && !tlv.Constructed
            && !tlv.IndefiniteLength
            && tlv.Content != null
            && tlv.Children != null
            && tlv.Children.Length == 0;
    }

    internal bool IsConstructedWithTag(
        BerTlv tlv,
        AsnTagClass tagClass,
        int number)
    {
        return this.HasTag(tlv, tagClass, number)
            && tlv.Constructed
            && tlv.Content != null
            && tlv.Content.Length == 0
            && tlv.Children != null
            && new ResourceCounter(this.Limits).AllowsCollectionElements(
                tlv.Children.Length);
    }

    internal bool IsTimeType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.UtcTime
            || kind == AsnTypeKind.GeneralizedTime
            || kind == AsnTypeKind.Time
            || kind == AsnTypeKind.Date
            || kind == AsnTypeKind.TimeOfDay
            || kind == AsnTypeKind.DateTime
            || kind == AsnTypeKind.Duration;
    }

    internal bool IsBerFamilyRule(AsnEncodingRules rules)
    {
        return rules == AsnEncodingRules.Ber
            || rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer;
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        System.Buffer.BlockCopy(source, 0, copy, 0, source.Length);
        return copy;
    }

    internal BerUsefulValueResult CreateUsefulValueError(
        string code,
        string message,
        bool resource)
    {
        return new BerUsefulValueResult
        {
            LimitExceeded = resource,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal BerPrimitiveEncodeResult CreateUsefulEncodeError(
        string code,
        string message,
        bool resource)
    {
        return new BerPrimitiveEncodeResult
        {
            Diagnostics = [this.CreateUsefulDiagnostic(
                code,
                message,
                resource,
                true)]
        };
    }

    internal BerPrimitiveDecodeResult CreateUsefulDecodeError(
        string code,
        string message,
        bool resource)
    {
        return new BerPrimitiveDecodeResult
        {
            Diagnostics = [this.CreateUsefulDiagnostic(
                code,
                message,
                resource,
                false)]
        };
    }

    internal AsnDiagnostic CreateUsefulDiagnostic(
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
                : encoding
                    ? AsnDiagnosticCategory.Encoding
                    : AsnDiagnosticCategory.Decoding,
            Code = code,
            Message = message
        };
    }
}
