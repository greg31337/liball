namespace asn1;

internal class OerUsefulTypeCodec
{
    internal AsnResourceLimits Limits;

    internal OerUsefulTypeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerCollectionResult EncodeUseful(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        bool canonical)
    {
        return this.EncodeUsefulAtDepth(
            kind,
            value,
            canonical,
            0,
            AsnUnknownDataPolicy.Preserve);
    }

    internal OerCollectionResult DecodeUseful(
        AsnTypeKind kind,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        return this.DecodeUsefulAtDepth(
            kind,
            data,
            offset,
            count,
            canonical,
            0,
            AsnUnknownDataPolicy.Preserve);
    }

    internal OerCollectionResult EncodeUsefulAtDepth(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        if (kind == AsnTypeKind.InstanceOf)
        {
            return this.EncodeInstanceOf(value, canonical, policy);
        }

        AsnRuntimeValueKind expected = this.GetAssociatedRuntimeKind(kind);
        AsnTypeDescriptor? schema = this.CreateAssociatedSchema(kind);
        if (schema == null || value.Kind != expected)
        {
            return this.CreateUsefulError(
                "ASNO0082",
                "The requested OER useful type or runtime value is invalid.",
                false);
        }

        OerCollectionResult encoded = new OerCollectionCodec(this.Limits)
            .EncodeCollectionAtDepth(
                schema,
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Sequence,
                    Fields = value.Fields
                },
                canonical,
                depth,
                policy);
        if (encoded.Success)
        {
            encoded.Value = value;
        }

        return encoded;
    }

    internal OerCollectionResult DecodeUsefulAtDepth(
        AsnTypeKind kind,
        byte[] data,
        int offset,
        int count,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        if (kind == AsnTypeKind.InstanceOf)
        {
            return this.DecodeInstanceOf(
                data,
                offset,
                count,
                canonical,
                policy);
        }

        AsnTypeDescriptor? schema = this.CreateAssociatedSchema(kind);
        if (schema == null)
        {
            return this.CreateUsefulError(
                "ASNO0082",
                "The requested type has no OER associated type.",
                false);
        }

        OerCollectionResult decoded = new OerCollectionCodec(this.Limits)
            .DecodeCollectionAtDepth(
                schema,
                data,
                offset,
                count,
                canonical,
                depth,
                policy);
        if (decoded.Success && decoded.Value != null)
        {
            decoded.Value.Kind = this.GetAssociatedRuntimeKind(kind);
        }

        return decoded;
    }

    internal OerCollectionResult EncodeInstanceOf(
        AsnRuntimeValue value,
        bool canonical,
        AsnUnknownDataPolicy policy)
    {
        if (value.Kind != AsnRuntimeValueKind.InstanceOf)
        {
            return this.CreateUsefulError(
                "ASNO0083",
                "INSTANCE OF requires its structured runtime value.",
                false);
        }

        AsnRuntimeValue? identifier = this.GetPresentFieldValue(
            value,
            "type-id");
        AsnRuntimeValue? openValue = this.GetPresentFieldValue(
            value,
            "value");
        if (identifier == null
            || identifier.Kind != AsnRuntimeValueKind.ObjectIdentifier
            || identifier.ObjectIdentifierValue == null
            || openValue == null
            || openValue.Kind != AsnRuntimeValueKind.OpenType
            || openValue.OpenTypeValue == null
            || policy != AsnUnknownDataPolicy.Preserve
            || !new OpaquePayloadPolicy().CanReuseOpenType(
                openValue.OpenTypeValue,
                canonical
                    ? AsnEncodingRules.Coer
                    : AsnEncodingRules.Oer))
        {
            return this.CreateUsefulError(
                "ASNO0083",
                "INSTANCE OF requires type-id and a complete open encoding.",
                false);
        }

        OerPrimitiveResult encodedIdentifier = new OerPrimitiveCodec(
            this.Limits).EncodePrimitive(
                this.CreatePrimitiveType(AsnTypeKind.ObjectIdentifier),
                identifier,
                canonical);
        if (!encodedIdentifier.Success)
        {
            return new OerValueCodec(this.Limits).CopyPrimitiveValue(
                encodedIdentifier);
        }

        OerContentResult encodedOpen = new OerOpenTypeCodec(this.Limits)
            .EncodeOpenType(openValue.OpenTypeValue.OriginalData);
        if (!encodedOpen.Success)
        {
            return this.CopyContentFailure(encodedOpen);
        }

        byte[] data = new byte[
            encodedIdentifier.Data.Length + encodedOpen.Data.Length];
        System.Buffer.BlockCopy(
            encodedIdentifier.Data,
            0,
            data,
            0,
            encodedIdentifier.Data.Length);
        System.Buffer.BlockCopy(
            encodedOpen.Data,
            0,
            data,
            encodedIdentifier.Data.Length,
            encodedOpen.Data.Length);
        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = data,
            ConsumedCount = data.Length
        };
    }

    internal OerCollectionResult DecodeInstanceOf(
        byte[] data,
        int offset,
        int count,
        bool canonical,
        AsnUnknownDataPolicy policy)
    {
        OerPrimitiveResult identifier = new OerPrimitiveCodec(this.Limits)
            .DecodePrimitive(
                this.CreatePrimitiveType(AsnTypeKind.ObjectIdentifier),
                data,
                offset,
                count,
                canonical);
        if (!identifier.Success || identifier.Value == null)
        {
            return new OerValueCodec(this.Limits).CopyPrimitiveValue(
                identifier);
        }

        OerContentResult open = new OerOpenTypeCodec(this.Limits)
            .DecodeOpenType(
                data,
                offset + identifier.ConsumedCount,
                count - identifier.ConsumedCount,
                canonical);
        if (!open.Success)
        {
            return this.ShiftUsefulFailure(
                this.CopyContentFailure(open),
                identifier.ConsumedCount);
        }

        if (policy == AsnUnknownDataPolicy.Reject)
        {
            return this.CreateUsefulErrorAt(
                "ASNO0083",
                "An unresolved INSTANCE OF value is not permitted by policy.",
                false,
                identifier.ConsumedCount + open.ConsumedCount
                    - open.Content.Length);
        }

        int consumed = identifier.ConsumedCount + open.ConsumedCount;
        byte[] encoded = new byte[consumed];
        System.Buffer.BlockCopy(data, offset, encoded, 0, consumed);
        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.InstanceOf,
                Fields =
                [
                    this.CreatePresentField("type-id", identifier.Value),
                    this.CreatePresentField(
                        "value",
                        new AsnRuntimeValue
                        {
                            Kind = AsnRuntimeValueKind.OpenType,
                            OpenTypeValue = new AsnOpenTypeValue
                            {
                                Unresolved = true,
                                DispatchKey = identifier.Value,
                                HasEncodingRules = policy
                                    == AsnUnknownDataPolicy.Preserve,
                                EncodingRules = canonical
                                    ? AsnEncodingRules.Coer
                                    : AsnEncodingRules.Oer,
                                HasOriginalData = policy
                                    == AsnUnknownDataPolicy.Preserve,
                                OriginalData = policy
                                    == AsnUnknownDataPolicy.Preserve
                                    ? open.Content
                                    : []
                            }
                        })
                ]
            },
            Data = encoded,
            ConsumedCount = consumed
        };
    }

    internal AsnTypeDescriptor? CreateAssociatedSchema(AsnTypeKind kind)
    {
        string dataName = kind == AsnTypeKind.CharacterString
            ? "string-value"
            : kind == AsnTypeKind.External
                || kind == AsnTypeKind.EmbeddedPdv
                ? "data-value"
                : "";
        if (dataName.Length == 0)
        {
            return null;
        }

        AsnFieldDescriptor identification = this.CreateField(
            "identification",
            0,
            this.CreateIdentificationSchema());
        AsnFieldDescriptor data = this.CreateField(
            dataName,
            kind == AsnTypeKind.CharacterString ? 1 : 2,
            this.CreatePrimitiveType(AsnTypeKind.OctetString));
        AsnFieldDescriptor[] roots;
        if (kind != AsnTypeKind.External)
        {
            roots = [identification, data];
        }
        else
        {
            AsnFieldDescriptor descriptor = this.CreateField(
                "data-value-descriptor",
                1,
                this.CreatePrimitiveType(AsnTypeKind.GraphicString));
            descriptor.Optional = true;
            roots = [identification, descriptor, data];
        }

        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = roots,
            RootFields = roots
        };
    }

    internal AsnTypeDescriptor CreateIdentificationSchema()
    {
        AsnTypeDescriptor objectIdentifier = this.CreatePrimitiveType(
            AsnTypeKind.ObjectIdentifier);
        AsnTypeDescriptor integer = this.CreatePrimitiveType(
            AsnTypeKind.Integer);
        AsnTypeDescriptor nullType = this.CreatePrimitiveType(
            AsnTypeKind.Null);
        AsnTypeDescriptor syntaxes = this.CreatePairSchema(
            "abstract",
            objectIdentifier,
            "transfer",
            this.CreatePrimitiveType(AsnTypeKind.ObjectIdentifier));
        AsnTypeDescriptor negotiation = this.CreatePairSchema(
            "presentation-context-id",
            integer,
            "transfer-syntax",
            this.CreatePrimitiveType(AsnTypeKind.ObjectIdentifier));
        AsnFieldDescriptor[] fields =
        [
            this.CreateTaggedField("syntaxes", 0, syntaxes, 0),
            this.CreateTaggedField(
                "syntax",
                1,
                objectIdentifier,
                1),
            this.CreateTaggedField(
                "presentation-context-id",
                2,
                integer,
                2),
            this.CreateTaggedField(
                "context-negotiation",
                3,
                negotiation,
                3),
            this.CreateTaggedField(
                "transfer-syntax",
                4,
                this.CreatePrimitiveType(AsnTypeKind.ObjectIdentifier),
                4),
            this.CreateTaggedField("fixed", 5, nullType, 5)
        ];
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Fields = fields,
            RootFields = fields
        };
    }

    internal AsnTypeDescriptor CreatePairSchema(
        string firstName,
        AsnTypeDescriptor firstType,
        string secondName,
        AsnTypeDescriptor secondType)
    {
        AsnFieldDescriptor first = this.CreateField(firstName, 0, firstType);
        AsnFieldDescriptor second = this.CreateField(
            secondName,
            1,
            secondType);
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = [first, second],
            RootFields = [first, second]
        };
    }

    internal AsnTypeDescriptor CreatePrimitiveType(AsnTypeKind kind)
    {
        return new AsnTypeDescriptor { Kind = kind };
    }

    internal AsnFieldDescriptor CreateField(
        string name,
        int index,
        AsnTypeDescriptor type)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Index = index,
            Type = type
        };
    }

    internal AsnFieldDescriptor CreateTaggedField(
        string name,
        int index,
        AsnTypeDescriptor type,
        int tagNumber)
    {
        AsnFieldDescriptor field = this.CreateField(name, index, type);
        field.Tags =
        [
            new AsnTagDescriptor
            {
                Class = AsnTagClass.ContextSpecific,
                Number = tagNumber
            }
        ];
        return field;
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

    internal AsnRuntimeValue? GetPresentFieldValue(
        AsnRuntimeValue value,
        string name)
    {
        int index = 0;
        while (index < value.Fields.Length)
        {
            if (value.Fields[index] != null
                && value.Fields[index].Name == name
                && value.Fields[index].Present)
            {
                return value.Fields[index].Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnRuntimeValueKind GetAssociatedRuntimeKind(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.External)
        {
            return AsnRuntimeValueKind.External;
        }

        if (kind == AsnTypeKind.EmbeddedPdv)
        {
            return AsnRuntimeValueKind.EmbeddedPdv;
        }

        return AsnRuntimeValueKind.CharacterString;
    }

    internal OerCollectionResult CopyContentFailure(OerContentResult source)
    {
        return new OerCollectionResult
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

    internal OerCollectionResult CreateUsefulError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerCollectionResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal OerCollectionResult CreateUsefulErrorAt(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset)
    {
        OerCollectionResult result = this.CreateUsefulError(
            code,
            message,
            limitExceeded);
        result.HasViolationOffset = true;
        result.ViolationOffset = violationOffset;
        return result;
    }

    internal OerCollectionResult ShiftUsefulFailure(
        OerCollectionResult result,
        int prefixCount)
    {
        if (!result.Success)
        {
            result.ConsumedCount = prefixCount + result.ConsumedCount;
            result.ViolationOffset = prefixCount
                + (result.HasViolationOffset
                    ? result.ViolationOffset
                    : 0);
            result.HasViolationOffset = true;
        }

        return result;
    }
}
