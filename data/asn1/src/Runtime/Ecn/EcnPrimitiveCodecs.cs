namespace asn1;

internal class EcnPrimitiveCodecs
{
    internal AsnResourceLimits Limits;
    internal EcnBitFieldOperations Bits = new EcnBitFieldOperations();
    internal EcnPrimitiveSettingsResolver Settings =
        new EcnPrimitiveSettingsResolver();

    internal EcnPrimitiveCodecs(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal EcnPrimitiveResult EncodePrimitive(
        EcnClassCategory category,
        EcnEncodingProperty[] properties,
        AsnRuntimeValue value)
    {
        if (properties == null)
        {
            throw new System.ArgumentNullException("properties");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        EcnPrimitiveSettingsResult resolved = this.Settings.ResolveSettings(
            properties);
        if (!resolved.Success || resolved.Settings == null)
        {
            return this.CreateError(
                "ASNECN0100",
                resolved.ErrorMessage,
                false);
        }

        EcnPrimitiveSettings settings = resolved.Settings;
        if (category == EcnClassCategory.Boolean)
        {
            return this.EncodeBooleanValue(value, settings);
        }

        if (category == EcnClassCategory.Integer)
        {
            return this.EncodeIntegerValue(value, settings);
        }

        if (category == EcnClassCategory.Real)
        {
            return this.EncodeRealValue(value, settings);
        }

        if (category == EcnClassCategory.BitString)
        {
            return this.EncodeBitStringValue(value, settings);
        }

        if (category == EcnClassCategory.OctetString)
        {
            return this.EncodeOctetStringValue(value, settings);
        }

        if (category == EcnClassCategory.CharacterString)
        {
            return this.EncodeCharacterStringValue(value, settings);
        }

        if (category == EcnClassCategory.ObjectIdentifier)
        {
            return this.EncodeObjectIdentifierValue(value, settings);
        }

        if (category == EcnClassCategory.Null)
        {
            return this.EncodeNullValue(value, settings);
        }

        if (category == EcnClassCategory.OpenType)
        {
            return this.EncodeOpenTypeValue(value, settings);
        }

        if (category == EcnClassCategory.Tag)
        {
            return this.EncodeTagValue(value, settings);
        }

        if (category == EcnClassCategory.Padding)
        {
            return this.EncodePaddingValue(settings);
        }

        return this.CreateError(
            "ASNECN0101",
            "The encoding class category is not an ECN primitive bit-field category.",
            false);
    }

    internal EcnPrimitiveResult DecodePrimitive(
        EcnClassCategory category,
        EcnEncodingProperty[] properties,
        byte[] data,
        int bitCount)
    {
        if (properties == null)
        {
            throw new System.ArgumentNullException("properties");
        }

        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (bitCount < 0 || bitCount > data.Length * 8)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        EcnPrimitiveSettingsResult resolved = this.Settings.ResolveSettings(
            properties);
        if (!resolved.Success || resolved.Settings == null)
        {
            return this.CreateError(
                "ASNECN0100",
                resolved.ErrorMessage,
                false);
        }

        EcnPrimitiveSettings settings = resolved.Settings;
        if (settings.SizeBits >= 0 && bitCount != settings.SizeBits)
        {
            return this.CreateError(
                "ASNECN0102",
                "The primitive field size does not match ENCODING-SPACE SIZE.",
                false);
        }

        EcnPrimitiveResult limit = this.CheckFieldLimit(bitCount);
        if (!limit.Success)
        {
            return limit;
        }

        EcnBitField encoded = this.Bits.CreateFromBytes(data, bitCount);
        EcnBitTransformResult transformed = this.Bits.ApplyDecodingTransforms(
            encoded,
            settings);
        if (!transformed.Success || transformed.Field == null)
        {
            return this.CreateError(
                "ASNECN0103",
                transformed.ErrorMessage,
                false);
        }

        EcnBitField field = transformed.Field;
        if (category == EcnClassCategory.Boolean)
        {
            return this.DecodeBooleanValue(field, settings);
        }

        if (category == EcnClassCategory.Integer)
        {
            return this.DecodeIntegerValue(field, settings);
        }

        if (category == EcnClassCategory.Real)
        {
            return this.DecodeRealValue(field, settings);
        }

        if (category == EcnClassCategory.BitString)
        {
            return this.DecodeBitStringValue(field, settings);
        }

        if (category == EcnClassCategory.OctetString)
        {
            return this.DecodeOctetStringValue(field, settings);
        }

        if (category == EcnClassCategory.CharacterString)
        {
            return this.DecodeCharacterStringValue(field, settings);
        }

        if (category == EcnClassCategory.ObjectIdentifier)
        {
            return this.DecodeObjectIdentifierValue(field, settings);
        }

        if (category == EcnClassCategory.Null)
        {
            return this.DecodeNullValue(field, settings);
        }

        if (category == EcnClassCategory.OpenType)
        {
            return this.DecodeOpenTypeValue(field, settings);
        }

        if (category == EcnClassCategory.Tag)
        {
            return this.DecodeTagValue(field, settings);
        }

        if (category == EcnClassCategory.Padding)
        {
            return this.DecodePaddingValue(field, settings);
        }

        return this.CreateError(
            "ASNECN0101",
            "The encoding class category is not an ECN primitive bit-field category.",
            false);
    }

    internal EcnPrimitiveResult EncodeBooleanValue(
        AsnRuntimeValue value,
        EcnPrimitiveSettings settings)
    {
        if (value.Kind != AsnRuntimeValueKind.Boolean
            || !value.HasBooleanValue)
        {
            return this.CreateValueKindError("BOOLEAN");
        }

        EcnBitField pattern = value.BooleanValue
            ? settings.TruePattern
            : settings.FalsePattern;
        return this.EncodeField(pattern, settings, false, true);
    }

    internal EcnPrimitiveResult DecodeBooleanValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnBitTransformResult trueField = this.Bits.FitField(
            settings.TruePattern,
            settings,
            false);
        EcnBitTransformResult falseField = this.Bits.FitField(
            settings.FalsePattern,
            settings,
            false);
        if (!trueField.Success || trueField.Field == null
            || !falseField.Success || falseField.Field == null)
        {
            return this.CreateError(
                "ASNECN0104",
                "The Boolean patterns do not fit the ECN encoding space.",
                false);
        }

        bool isTrue = this.Bits.FieldsEqual(field, trueField.Field);
        bool isFalse = this.Bits.FieldsEqual(field, falseField.Field);
        if (isTrue == isFalse)
        {
            return this.CreateError(
                "ASNECN0105",
                "The ECN Boolean field matches neither or both value patterns.",
                false);
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = isTrue
        }, field);
    }

    internal EcnPrimitiveResult EncodeIntegerValue(
        AsnRuntimeValue value,
        EcnPrimitiveSettings settings)
    {
        if ((value.Kind != AsnRuntimeValueKind.Integer
                && value.Kind != AsnRuntimeValueKind.Enumerated)
            || !value.HasIntegerValue)
        {
            return this.CreateValueKindError("INTEGER");
        }

        if (!this.IntegerWithinRange(value.IntegerValue, settings))
        {
            return this.CreateError(
                "ASNECN0106",
                "The integer value is outside the ECN RANGE.",
                false);
        }

        bool unsigned = this.IsUnsignedInteger(settings);
        if (unsigned && value.IntegerValue < System.Numerics.BigInteger.Zero)
        {
            return this.CreateError(
                "ASNECN0107",
                "A positive or unsigned ECN integer encoding cannot encode a negative value.",
                false);
        }

        int minimum = this.GetIntegerBitCount(value.IntegerValue, unsigned);
        if (!new ResourceCounter(this.Limits).AllowsIntegerBits(minimum))
        {
            return this.CreateError(
                "ASNR0010",
                "The ECN integer exceeds the configured integer-bit limit.",
                true);
        }

        EcnBitField initial = this.EncodeIntegerBits(
            value.IntegerValue,
            minimum);
        EcnBitTransformResult fitted = this.Bits.FitField(
            initial,
            settings,
            !unsigned && value.IntegerValue < System.Numerics.BigInteger.Zero);
        if (!fitted.Success || fitted.Field == null)
        {
            return this.CreateError(
                "ASNECN0108",
                fitted.ErrorMessage,
                false);
        }

        EcnBitField field = fitted.Field;
        if (this.IsReversedInteger(settings))
        {
            field = this.Bits.ReverseField(field);
        }

        return this.TransformEncodedField(field, settings);
    }

    internal EcnPrimitiveResult DecodeIntegerValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnBitTransformResult unpadded = this.Bits.RemoveUnusedBits(
            field,
            settings);
        if (!unpadded.Success || unpadded.Field == null)
        {
            return this.CreateError(
                "ASNECN0109",
                unpadded.ErrorMessage,
                false);
        }

        EcnBitField valueField = unpadded.Field;
        if (this.IsReversedInteger(settings))
        {
            valueField = this.Bits.ReverseField(valueField);
        }

        if (valueField.BitCount == 0)
        {
            return this.CreateError(
                "ASNECN0110",
                "An ECN integer field cannot be empty.",
                false);
        }

        bool unsigned = this.IsUnsignedInteger(settings);
        System.Numerics.BigInteger integer = this.DecodeIntegerBits(
            valueField,
            unsigned);
        if (!this.IntegerWithinRange(integer, settings))
        {
            return this.CreateError(
                "ASNECN0106",
                "The decoded integer is outside the ECN RANGE.",
                false);
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = integer
        }, field);
    }

    internal EcnPrimitiveResult EncodeRealValue(
        AsnRuntimeValue value,
        EcnPrimitiveSettings settings)
    {
        if (value.Kind != AsnRuntimeValueKind.Real
            || value.RealValue == null)
        {
            return this.CreateValueKindError("REAL");
        }

        BerPrimitiveValueResult encoded = new BerRealCodec(this.Limits)
            .EncodeReal(value.RealValue, AsnEncodingRules.Der);
        if (!encoded.Success)
        {
            return this.CreateError(
                encoded.ErrorCode,
                encoded.ErrorMessage,
                encoded.LimitExceeded);
        }

        return this.EncodeField(
            this.Bits.CreateFromBytes(
                encoded.Content,
                encoded.Content.Length * 8),
            settings,
            false,
            false);
    }

    internal EcnPrimitiveResult DecodeRealValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnPrimitiveResult payload = this.DecodeOctetPayload(field, settings);
        if (!payload.Success)
        {
            return payload;
        }

        BerPrimitiveValueResult decoded = new BerRealCodec(this.Limits)
            .DecodeReal(payload.Data);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CreateError(
                decoded.ErrorCode,
                decoded.ErrorMessage,
                decoded.LimitExceeded);
        }

        return this.CreateDecodedValue(decoded.Value, field);
    }

    internal EcnPrimitiveResult EncodeBitStringValue(
        AsnRuntimeValue value,
        EcnPrimitiveSettings settings)
    {
        if (value.Kind != AsnRuntimeValueKind.BitString
            || value.BitStringValue == null
            || !value.BitStringValue.IsValid())
        {
            return this.CreateValueKindError("BIT STRING");
        }

        return this.EncodeField(
            this.Bits.CreateFromBytes(
                value.BitStringValue.Bytes,
                value.BitStringValue.BitLength),
            settings,
            false,
            false);
    }

    internal EcnPrimitiveResult DecodeBitStringValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnBitTransformResult payload = this.ExtractPayload(field, settings);
        if (!payload.Success || payload.Field == null)
        {
            return this.CreateError(
                "ASNECN0111",
                payload.ErrorMessage,
                false);
        }

        EcnBitField bits = payload.Field;
        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.BitString,
            BitStringValue = new AsnBitStringValue
            {
                Bytes = bits.Data,
                BitLength = bits.BitCount
            }
        }, field);
    }

    internal EcnPrimitiveResult EncodeOctetStringValue(
        AsnRuntimeValue value,
        EcnPrimitiveSettings settings)
    {
        if (value.Kind != AsnRuntimeValueKind.OctetString)
        {
            return this.CreateValueKindError("OCTET STRING");
        }

        return this.EncodeOctets(value.Octets, settings, false);
    }

    internal EcnPrimitiveResult DecodeOctetStringValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnPrimitiveResult payload = this.DecodeOctetPayload(field, settings);
        if (!payload.Success)
        {
            return payload;
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = payload.Data
        }, field);
    }

    internal EcnPrimitiveResult EncodeCharacterStringValue(
        AsnRuntimeValue value,
        EcnPrimitiveSettings settings)
    {
        if (value.Kind != AsnRuntimeValueKind.CharacterString
            || value.TextValue == null)
        {
            return this.CreateValueKindError("character string");
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(
            value.TextValue.Length))
        {
            return this.CreateError(
                "ASNR0009",
                "The ECN character string exceeds the configured text limit.",
                true);
        }

        if (!this.TextUsesAlphabet(value.TextValue, settings))
        {
            return this.CreateError(
                "ASNECN0119",
                "The character string contains a character outside the ECN ALPHABET.",
                false);
        }

        return this.EncodeOctets(
            System.Text.Encoding.UTF8.GetBytes(value.TextValue),
            settings,
            false);
    }

    internal EcnPrimitiveResult DecodeCharacterStringValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnPrimitiveResult payload = this.DecodeOctetPayload(field, settings);
        if (!payload.Success)
        {
            return payload;
        }

        string text = System.Text.Encoding.UTF8.GetString(payload.Data);
        byte[] roundTrip = System.Text.Encoding.UTF8.GetBytes(text);
        if (!this.ByteArraysEqual(payload.Data, roundTrip)
            || !new ResourceCounter(this.Limits).AllowsTextCharacters(
                text.Length))
        {
            return this.CreateError(
                "ASNECN0112",
                "The ECN character-string field is not valid bounded UTF-8.",
                false);
        }

        if (!this.TextUsesAlphabet(text, settings))
        {
            return this.CreateError(
                "ASNECN0119",
                "The decoded character string contains a character outside the ECN ALPHABET.",
                false);
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = text
        }, field);
    }

    internal EcnPrimitiveResult EncodeObjectIdentifierValue(
        AsnRuntimeValue value,
        EcnPrimitiveSettings settings)
    {
        if ((value.Kind != AsnRuntimeValueKind.ObjectIdentifier
                && value.Kind
                    != AsnRuntimeValueKind.RelativeObjectIdentifier)
            || value.ObjectIdentifierValue == null)
        {
            return this.CreateValueKindError("OBJECT IDENTIFIER");
        }

        BerPrimitiveValueResult encoded = new BerObjectIdentifierCodec(
            this.Limits).EncodeObjectIdentifier(
                value.ObjectIdentifierValue,
                value.ObjectIdentifierValue.Relative);
        if (!encoded.Success)
        {
            return this.CreateError(
                encoded.ErrorCode,
                encoded.ErrorMessage,
                encoded.LimitExceeded);
        }

        return this.EncodeOctets(encoded.Content, settings, false);
    }

    internal EcnPrimitiveResult DecodeObjectIdentifierValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnPrimitiveResult payload = this.DecodeOctetPayload(field, settings);
        if (!payload.Success)
        {
            return payload;
        }

        bool relative = settings.Representation == "relative";
        BerPrimitiveValueResult decoded = new BerObjectIdentifierCodec(
            this.Limits).DecodeObjectIdentifier(payload.Data, relative);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CreateError(
                decoded.ErrorCode,
                decoded.ErrorMessage,
                decoded.LimitExceeded);
        }

        return this.CreateDecodedValue(decoded.Value, field);
    }

    internal EcnPrimitiveResult EncodeNullValue(
        AsnRuntimeValue value,
        EcnPrimitiveSettings settings)
    {
        if (value.Kind != AsnRuntimeValueKind.Null)
        {
            return this.CreateValueKindError("NULL");
        }

        return this.EncodeField(
            new EcnBitField(),
            settings,
            false,
            true);
    }

    internal EcnPrimitiveResult DecodeNullValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnBitTransformResult expected = this.Bits.FitField(
            new EcnBitField(),
            settings,
            false);
        if (!expected.Success || expected.Field == null
            || !this.Bits.FieldsEqual(field, expected.Field))
        {
            return this.CreateError(
                "ASNECN0113",
                "The ECN NULL field does not match its padding pattern.",
                false);
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Null
        }, field);
    }

    internal EcnPrimitiveResult EncodeOpenTypeValue(
        AsnRuntimeValue value,
        EcnPrimitiveSettings settings)
    {
        if (value.Kind != AsnRuntimeValueKind.OpenType
            || value.OpenTypeValue == null
            || !new OpaquePayloadPolicy().CanReuseOpenType(
                value.OpenTypeValue,
                AsnEncodingRules.Ecn))
        {
            return this.CreateValueKindError("open type with original data");
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
            value.OpenTypeValue.OriginalData.Length))
        {
            return this.CreateError(
                "ASNR0006",
                "The ECN open type exceeds the configured open-type limit.",
                true);
        }

        return this.EncodeOctets(
            value.OpenTypeValue.OriginalData,
            settings,
            false);
    }

    internal EcnPrimitiveResult DecodeOpenTypeValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnPrimitiveResult payload = this.DecodeOctetPayload(field, settings);
        if (!payload.Success)
        {
            return payload;
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
            payload.Data.Length))
        {
            return this.CreateError(
                "ASNR0006",
                "The ECN open type exceeds the configured open-type limit.",
                true);
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = true,
                EncodingRules = AsnEncodingRules.Ecn,
                HasOriginalData = true,
                OriginalData = payload.Data
            }
        }, field);
    }

    internal EcnPrimitiveResult EncodeTagValue(
        AsnRuntimeValue value,
        EcnPrimitiveSettings settings)
    {
        if (value.Kind != AsnRuntimeValueKind.Integer
            || !value.HasIntegerValue
            || value.IntegerValue < System.Numerics.BigInteger.Zero)
        {
            return this.CreateValueKindError(
                "tag represented by a non-negative integer");
        }

        AsnTag tag = new AsnTag
        {
            Class = this.GetTagClass(value.TextValue),
            Number = value.IntegerValue
        };
        OerTagResult encoded = new OerTagCodec(this.Limits).EncodeTag(tag);
        if (!encoded.Success)
        {
            return this.CreateError(
                encoded.ErrorCode,
                encoded.ErrorMessage,
                encoded.LimitExceeded);
        }

        EcnPrimitiveResult result = this.EncodeOctets(
            encoded.Data,
            settings,
            false);
        result.Tag = tag;
        return result;
    }

    internal EcnPrimitiveResult DecodeTagValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnPrimitiveResult payload = this.DecodeOctetPayload(field, settings);
        if (!payload.Success)
        {
            return payload;
        }

        OerTagResult decoded = new OerTagCodec(this.Limits).DecodeTag(
            payload.Data,
            0,
            payload.Data.Length);
        if (!decoded.Success || decoded.Tag == null
            || decoded.ConsumedCount != payload.Data.Length)
        {
            return this.CreateError(
                decoded.ErrorCode.Length == 0
                    ? "ASNECN0114"
                    : decoded.ErrorCode,
                decoded.ErrorMessage.Length == 0
                    ? "The ECN tag field contains trailing or incomplete data."
                    : decoded.ErrorMessage,
                decoded.LimitExceeded);
        }

        EcnPrimitiveResult result = this.CreateDecodedValue(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = decoded.Tag.Number,
                TextValue = this.GetTagClassName(decoded.Tag.Class)
            },
            field);
        result.Tag = decoded.Tag;
        return result;
    }

    internal EcnPrimitiveResult EncodePaddingValue(
        EcnPrimitiveSettings settings)
    {
        int bitCount = settings.SizeBits >= 0
            ? settings.SizeBits
            : settings.Pattern.BitCount;
        if (bitCount < 0 || settings.Pattern.BitCount == 0)
        {
            return this.CreateError(
                "ASNECN0115",
                "The ECN padding category requires a size or non-empty pattern.",
                false);
        }

        bool[] pattern = this.Bits.ToBits(settings.Pattern);
        bool[] repeated = new bool[bitCount];
        int index = 0;
        while (index < repeated.Length)
        {
            repeated[index] = pattern[index % pattern.Length];
            index = index + 1;
        }

        return this.TransformEncodedField(
            this.Bits.FromBits(repeated),
            settings);
    }

    internal EcnPrimitiveResult DecodePaddingValue(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        bool[] pattern = this.Bits.ToBits(settings.Pattern);
        bool[] actual = this.Bits.ToBits(field);
        if (pattern.Length == 0)
        {
            return this.CreateError(
                "ASNECN0115",
                "The ECN padding category requires a non-empty pattern.",
                false);
        }

        int index = 0;
        while (index < actual.Length)
        {
            if (actual[index] != pattern[index % pattern.Length])
            {
                return this.CreateError(
                    "ASNECN0116",
                    "The ECN padding field does not match PAD-PATTERN.",
                    false);
            }

            index = index + 1;
        }

        return this.CreateDecodedValue(new AsnRuntimeValue(), field);
    }

    internal EcnPrimitiveResult EncodeOctets(
        byte[] data,
        EcnPrimitiveSettings settings,
        bool signExtension)
    {
        return this.EncodeField(
            this.Bits.CreateFromBytes(data, data.Length * 8),
            settings,
            signExtension,
            false);
    }

    internal EcnPrimitiveResult EncodeField(
        EcnBitField field,
        EcnPrimitiveSettings settings,
        bool signExtension,
        bool ignoreTerminator)
    {
        EcnBitField payload = field;
        if (!ignoreTerminator && settings.Terminator.BitCount > 0)
        {
            payload = this.Bits.AppendFields(payload, settings.Terminator);
        }

        EcnBitTransformResult fitted = this.Bits.FitField(
            payload,
            settings,
            signExtension);
        if (!fitted.Success || fitted.Field == null)
        {
            return this.CreateError(
                "ASNECN0108",
                fitted.ErrorMessage,
                false);
        }

        return this.TransformEncodedField(fitted.Field, settings);
    }

    internal EcnPrimitiveResult TransformEncodedField(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnPrimitiveResult limit = this.CheckFieldLimit(field.BitCount);
        if (!limit.Success)
        {
            return limit;
        }

        EcnBitTransformResult transformed = this.Bits.ApplyEncodingTransforms(
            field,
            settings);
        if (!transformed.Success || transformed.Field == null)
        {
            return this.CreateError(
                "ASNECN0103",
                transformed.ErrorMessage,
                false);
        }

        return this.CreateEncodedField(transformed.Field);
    }

    internal EcnBitTransformResult ExtractPayload(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnBitTransformResult unpadded = this.Bits.RemoveUnusedBits(
            field,
            settings);
        if (!unpadded.Success || unpadded.Field == null)
        {
            return unpadded;
        }

        return this.Bits.RemoveTerminator(
            unpadded.Field,
            settings.Terminator);
    }

    internal EcnPrimitiveResult DecodeOctetPayload(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnBitTransformResult extracted = this.ExtractPayload(
            field,
            settings);
        if (!extracted.Success || extracted.Field == null)
        {
            return this.CreateError(
                "ASNECN0111",
                extracted.ErrorMessage,
                false);
        }

        if (extracted.Field.BitCount % 8 != 0)
        {
            return this.CreateError(
                "ASNECN0117",
                "The decoded ECN primitive payload is not octet-sized.",
                false);
        }

        return new EcnPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = extracted.Field.Data,
            BitCount = extracted.Field.BitCount
        };
    }

    internal EcnBitField EncodeIntegerBits(
        System.Numerics.BigInteger value,
        int bitCount)
    {
        System.Numerics.BigInteger encoded = value;
        if (encoded < System.Numerics.BigInteger.Zero)
        {
            encoded = (System.Numerics.BigInteger.One << bitCount) + encoded;
        }

        bool[] bits = new bool[bitCount];
        int index = bitCount - 1;
        while (index >= 0)
        {
            bits[index] = (encoded & System.Numerics.BigInteger.One)
                != System.Numerics.BigInteger.Zero;
            encoded = encoded >> 1;
            index = index - 1;
        }

        return this.Bits.FromBits(bits);
    }

    internal System.Numerics.BigInteger DecodeIntegerBits(
        EcnBitField field,
        bool unsigned)
    {
        bool[] bits = this.Bits.ToBits(field);
        System.Numerics.BigInteger value = System.Numerics.BigInteger.Zero;
        int index = 0;
        while (index < bits.Length)
        {
            value = value << 1;
            if (bits[index])
            {
                value = value | System.Numerics.BigInteger.One;
            }

            index = index + 1;
        }

        if (!unsigned && bits[0])
        {
            value = value - (System.Numerics.BigInteger.One
                << bits.Length);
        }

        return value;
    }

    internal int GetIntegerBitCount(
        System.Numerics.BigInteger value,
        bool unsigned)
    {
        System.Numerics.BigInteger measured = unsigned
            || value >= System.Numerics.BigInteger.Zero
                ? value
                : ~value;
        int count = 0;
        while (measured > System.Numerics.BigInteger.Zero)
        {
            measured = measured >> 1;
            count = count + 1;
        }

        if (unsigned)
        {
            return count == 0 ? 1 : count;
        }

        return count + 1;
    }

    internal bool IntegerWithinRange(
        System.Numerics.BigInteger value,
        EcnPrimitiveSettings settings)
    {
        return !settings.HasRange
            || value >= settings.LowerBound
                && value <= settings.UpperBound;
    }

    internal bool IsUnsignedInteger(EcnPrimitiveSettings settings)
    {
        return settings.Signedness == "unsigned"
            || settings.Representation == "positive-int"
            || settings.Representation == "reverse-positive-int";
    }

    internal bool IsReversedInteger(EcnPrimitiveSettings settings)
    {
        return settings.Representation == "reverse-positive-int"
            || settings.Representation == "reverse-twos-complement";
    }

    internal AsnTagClass GetTagClass(string text)
    {
        string normalized = text == null ? "" : text.ToLowerInvariant();
        if (normalized == "application")
        {
            return AsnTagClass.Application;
        }

        if (normalized == "context-specific" || normalized == "context")
        {
            return AsnTagClass.ContextSpecific;
        }

        if (normalized == "private")
        {
            return AsnTagClass.Private;
        }

        return AsnTagClass.Universal;
    }

    internal string GetTagClassName(AsnTagClass tagClass)
    {
        if (tagClass == AsnTagClass.Application)
        {
            return "application";
        }

        if (tagClass == AsnTagClass.ContextSpecific)
        {
            return "context-specific";
        }

        if (tagClass == AsnTagClass.Private)
        {
            return "private";
        }

        return "universal";
    }

    internal bool ByteArraysEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool TextUsesAlphabet(
        string text,
        EcnPrimitiveSettings settings)
    {
        if (!settings.HasAlphabet)
        {
            return true;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (settings.Alphabet.IndexOf(text[index]) < 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal EcnPrimitiveResult CheckFieldLimit(int bitCount)
    {
        int byteCount = (bitCount + 7) / 8;
        ResourceCounter resources = new ResourceCounter(this.Limits);
        if (!resources.AllowsIntegerBits(bitCount)
            || !resources.TryChargeAllocationBytes(byteCount))
        {
            return this.CreateError(
                "ASNR0010",
                "The ECN bit field exceeds the configured resource limits.",
                true);
        }

        return new EcnPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal EcnPrimitiveResult CreateEncodedField(EcnBitField field)
    {
        return new EcnPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = field.Data,
            BitCount = field.BitCount
        };
    }

    internal EcnPrimitiveResult CreateDecodedValue(
        AsnRuntimeValue value,
        EcnBitField field)
    {
        return new EcnPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = field.Data,
            BitCount = field.BitCount
        };
    }

    internal EcnPrimitiveResult CreateValueKindError(string expected)
    {
        return this.CreateError(
            "ASNECN0118",
            "The runtime value does not contain the expected " + expected
                + " ECN primitive value.",
            false);
    }

    internal EcnPrimitiveResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new EcnPrimitiveResult
        {
            Status = AsnDecodeStatus.Error,
            ErrorCode = code,
            ErrorMessage = message,
            LimitExceeded = limitExceeded
        };
    }
}
