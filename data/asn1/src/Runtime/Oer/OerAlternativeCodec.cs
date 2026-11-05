namespace asn1;

internal class OerAlternativeCodec
{
    internal AsnResourceLimits Limits;

    internal OerAlternativeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerCollectionResult EncodeChoice(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        bool canonical)
    {
        return this.EncodeChoiceAtDepth(
            type,
            value,
            canonical,
            0,
            AsnUnknownDataPolicy.Preserve);
    }

    internal OerCollectionResult DecodeChoice(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        return this.DecodeChoiceAtDepth(
            type,
            data,
            offset,
            count,
            canonical,
            0,
            AsnUnknownDataPolicy.Preserve);
    }

    internal OerCollectionResult EncodeChoiceAtDepth(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        if (!this.AllowsDepth(depth)
            || type.Kind != AsnTypeKind.Choice
            || value.Kind != AsnRuntimeValueKind.Choice)
        {
            return this.CreateAlternativeError(
                !this.AllowsDepth(depth) ? "ASNR0001" : "ASNO0073",
                !this.AllowsDepth(depth)
                    ? "The OER CHOICE exceeds the configured nesting depth."
                    : "The runtime value does not match an OER CHOICE.",
                !this.AllowsDepth(depth));
        }

        AsnRuntimeFieldValue? selected = this.FindSelection(value.Fields);
        if (selected == null || selected.Value == null)
        {
            return this.CreateAlternativeError(
                "ASNO0074",
                "An OER CHOICE must select exactly one valued alternative.",
                false);
        }

        AsnFieldDescriptor? field = this.FindField(type.Fields, selected.Name);
        if (field == null || field.Type == null)
        {
            return this.CreateAlternativeError(
                "ASNO0075",
                "The selected OER CHOICE alternative is not defined.",
                false);
        }

        AsnTag? tag = this.GetSelectedTag(field, selected.Value);
        if (tag == null)
        {
            return this.CreateAlternativeError(
                "ASNO0076",
                "The selected OER CHOICE alternative has no effective tag.",
                false);
        }

        OerTagResult encodedTag = new OerTagCodec(this.Limits).EncodeTag(tag);
        if (!encodedTag.Success)
        {
            return this.CopyTagFailure(encodedTag);
        }

        OerCollectionResult payload = new OerValueCodec(this.Limits)
            .EncodeValueAtDepth(
                field.Type,
                selected.Value,
                canonical,
                depth + 1,
                policy);
        if (!payload.Success)
        {
            return payload;
        }

        byte[] data = new byte[encodedTag.Data.Length + payload.Data.Length];
        System.Buffer.BlockCopy(
            encodedTag.Data,
            0,
            data,
            0,
            encodedTag.Data.Length);
        System.Buffer.BlockCopy(
            payload.Data,
            0,
            data,
            encodedTag.Data.Length,
            payload.Data.Length);
        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = data,
            ConsumedCount = data.Length
        };
    }

    internal OerCollectionResult DecodeChoiceAtDepth(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        if (!this.AllowsDepth(depth) || type.Kind != AsnTypeKind.Choice)
        {
            return this.CreateAlternativeError(
                !this.AllowsDepth(depth) ? "ASNR0001" : "ASNO0073",
                !this.AllowsDepth(depth)
                    ? "The OER CHOICE exceeds the configured nesting depth."
                    : "The requested type is not an OER CHOICE.",
                !this.AllowsDepth(depth));
        }

        OerTagResult decodedTag = new OerTagCodec(this.Limits).DecodeTag(
            data,
            offset,
            count);
        if (!decodedTag.Success || decodedTag.Tag == null)
        {
            return this.CopyTagFailure(decodedTag);
        }

        AsnFieldDescriptor? field = this.FindTaggedField(
            type.Fields,
            decodedTag.Tag);
        if (field == null || field.Type == null)
        {
            return this.CreateAlternativeError(
                "ASNO0077",
                "The OER CHOICE tag does not identify an alternative.",
                false);
        }

        OerCollectionResult payload = new OerValueCodec(this.Limits)
            .DecodeValueAtDepth(
                field.Type,
                data,
                offset + decodedTag.ConsumedCount,
                count - decodedTag.ConsumedCount,
                canonical,
                depth + 1,
                policy);
        if (!payload.Success)
        {
            return this.ShiftAlternativeFailure(
                payload,
                decodedTag.ConsumedCount);
        }

        int consumed = decodedTag.ConsumedCount + payload.ConsumedCount;
        byte[] encoded = new byte[consumed];
        System.Buffer.BlockCopy(data, offset, encoded, 0, consumed);
        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Choice,
                Fields =
                [
                    new AsnRuntimeFieldValue
                    {
                        Name = field.Name,
                        Present = true,
                        Value = payload.Value
                    }
                ]
            },
            Data = encoded,
            ConsumedCount = consumed
        };
    }

    internal AsnRuntimeFieldValue? FindSelection(
        AsnRuntimeFieldValue[] fields)
    {
        AsnRuntimeFieldValue? result = null;
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index] != null && fields[index].Present)
            {
                if (result != null)
                {
                    return null;
                }

                result = fields[index];
            }

            index = index + 1;
        }

        return result;
    }

    internal AsnFieldDescriptor? FindField(
        AsnFieldDescriptor[] fields,
        string name)
    {
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index] != null && fields[index].Name == name)
            {
                return fields[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnFieldDescriptor? FindTaggedField(
        AsnFieldDescriptor[] fields,
        AsnTag tag)
    {
        int index = 0;
        while (index < fields.Length)
        {
            AsnTypeDescriptor? fieldType = fields[index].Type;
            AsnTag? effective = this.GetDirectTag(fields[index]);
            if (effective != null && effective.Class == tag.Class
                && effective.Number == tag.Number)
            {
                return fields[index];
            }

            if (effective == null && fieldType != null
                && fieldType.Kind == AsnTypeKind.Choice
                && this.ChoiceContainsTag(fieldType, tag))
            {
                return fields[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnTag? GetSelectedTag(
        AsnFieldDescriptor field,
        AsnRuntimeValue selectedValue)
    {
        AsnTag? direct = this.GetDirectTag(field);
        if (direct != null)
        {
            return direct;
        }

        if (field.Type != null && field.Type.Kind == AsnTypeKind.Choice)
        {
            AsnRuntimeFieldValue? nested = this.FindSelection(
                selectedValue.Fields);
            AsnFieldDescriptor? nestedField = nested == null
                ? null
                : this.FindField(field.Type.Fields, nested.Name);
            return nested == null || nested.Value == null
                || nestedField == null
                ? null
                : this.GetSelectedTag(nestedField, nested.Value);
        }

        return null;
    }

    internal AsnTag? GetDirectTag(AsnFieldDescriptor field)
    {
        AsnTagDescriptor? descriptor = field.Tags.Length > 0
            ? field.Tags[0]
            : field.Type != null && field.Type.Tags.Length > 0
                ? field.Type.Tags[0]
                : null;
        if (descriptor != null)
        {
            return new AsnTag
            {
                Class = descriptor.Class,
                Mode = descriptor.Mode,
                Number = descriptor.Number
            };
        }

        int universal = field.Type == null
            ? -1
            : this.GetUniversalTagNumber(field.Type.Kind);
        return universal < 0
            ? null
            : new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = universal
            };
    }

    internal bool ChoiceContainsTag(AsnTypeDescriptor type, AsnTag tag)
    {
        int index = 0;
        while (index < type.Fields.Length)
        {
            AsnTypeDescriptor? fieldType = type.Fields[index].Type;
            AsnTag? direct = this.GetDirectTag(type.Fields[index]);
            if (direct != null && direct.Class == tag.Class
                && direct.Number == tag.Number)
            {
                return true;
            }

            if (direct == null && fieldType != null
                && fieldType.Kind == AsnTypeKind.Choice
                && this.ChoiceContainsTag(fieldType, tag))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
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

        if (kind == AsnTypeKind.ObjectDescriptor)
        {
            return AsnUniversalTags.ObjectDescriptor;
        }

        if (kind == AsnTypeKind.Real)
        {
            return AsnUniversalTags.Real;
        }

        if (kind == AsnTypeKind.Enumerated)
        {
            return AsnUniversalTags.Enumerated;
        }

        if (kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return AsnUniversalTags.RelativeObjectIdentifier;
        }

        if (kind == AsnTypeKind.Sequence
            || kind == AsnTypeKind.SequenceOf)
        {
            return AsnUniversalTags.Sequence;
        }

        if (kind == AsnTypeKind.Set || kind == AsnTypeKind.SetOf)
        {
            return AsnUniversalTags.Set;
        }

        if (kind == AsnTypeKind.External
            || kind == AsnTypeKind.InstanceOf)
        {
            return AsnUniversalTags.External;
        }

        if (kind == AsnTypeKind.EmbeddedPdv)
        {
            return AsnUniversalTags.EmbeddedPdv;
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

        if (kind == AsnTypeKind.UtcTime)
        {
            return AsnUniversalTags.UtcTime;
        }

        if (kind == AsnTypeKind.GeneralizedTime)
        {
            return AsnUniversalTags.GeneralizedTime;
        }

        if (kind == AsnTypeKind.Time)
        {
            return AsnUniversalTags.Time;
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

        if (kind == AsnTypeKind.CharacterString)
        {
            return AsnUniversalTags.CharacterString;
        }

        if (kind == AsnTypeKind.BmpString)
        {
            return AsnUniversalTags.BmpString;
        }

        if (kind == AsnTypeKind.Date)
        {
            return AsnUniversalTags.Date;
        }

        if (kind == AsnTypeKind.TimeOfDay)
        {
            return AsnUniversalTags.TimeOfDay;
        }

        if (kind == AsnTypeKind.DateTime)
        {
            return AsnUniversalTags.DateTime;
        }

        if (kind == AsnTypeKind.Duration)
        {
            return AsnUniversalTags.Duration;
        }

        if (kind == AsnTypeKind.ObjectIdentifierIri)
        {
            return AsnUniversalTags.ObjectIdentifierIri;
        }

        return kind == AsnTypeKind.RelativeObjectIdentifierIri
            ? AsnUniversalTags.RelativeObjectIdentifierIri
            : -1;
    }

    internal bool AllowsDepth(int depth)
    {
        return this.Limits.Unlimited
            || depth <= this.Limits.MaximumNestingDepth;
    }

    internal OerCollectionResult CopyTagFailure(OerTagResult source)
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

    internal OerCollectionResult CreateAlternativeError(
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

    internal OerCollectionResult ShiftAlternativeFailure(
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
