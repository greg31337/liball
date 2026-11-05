namespace asn1;

internal class OerPrimitiveCodec
{
    internal AsnResourceLimits Limits;

    internal OerPrimitiveCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerPrimitiveResult EncodePrimitive(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        bool canonical)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (!this.HasExpectedRuntimeKind(type.Kind, value))
        {
            return this.CreatePrimitiveError(
                "ASNO0021",
                "The runtime value does not match the requested OER type.",
                false);
        }

        if (type.Kind == AsnTypeKind.Boolean)
        {
            return this.CreateEncodedPrimitive(
                value,
                [value.BooleanValue ? (byte)255 : (byte)0],
                true);
        }

        if (type.Kind == AsnTypeKind.Integer)
        {
            OerIntegerResult integer = new OerIntegerCodec(this.Limits)
                .EncodeInteger(type, value.IntegerValue);
            return this.CopyIntegerEncode(integer, value);
        }

        if (type.Kind == AsnTypeKind.Enumerated)
        {
            return this.EncodeEnumerated(value);
        }

        if (type.Kind == AsnTypeKind.Null)
        {
            return this.CreateEncodedPrimitive(value, [], true);
        }

        if (type.Kind == AsnTypeKind.Real)
        {
            return this.EncodeReal(value);
        }

        if (type.Kind == AsnTypeKind.BitString)
        {
            return this.EncodeBitString(type, value);
        }

        if (type.Kind == AsnTypeKind.OctetString)
        {
            return this.EncodeOctetString(type, value);
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifier
            || type.Kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return this.EncodeObjectIdentifier(type.Kind, value);
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return this.EncodeTextContent(type, value, type.Kind);
        }

        if (this.IsTimeKind(type.Kind))
        {
            return new OerTimeCodec(this.Limits).EncodeTime(
                type,
                value,
                canonical);
        }

        if (this.IsCharacterStringKind(type.Kind))
        {
            AsnTypeKind contentKind = type.Kind == AsnTypeKind.ObjectDescriptor
                ? AsnTypeKind.GraphicString
                : type.Kind;
            return this.EncodeTextContent(type, value, contentKind);
        }

        return this.CreatePrimitiveError(
            "ASNO0022",
            "The requested type is not an OER primitive type.",
            false);
    }

    internal OerPrimitiveResult DecodePrimitive(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (type.Kind == AsnTypeKind.Boolean)
        {
            return this.DecodeBoolean(data, offset, count, canonical);
        }

        if (type.Kind == AsnTypeKind.Integer)
        {
            OerIntegerResult integer = new OerIntegerCodec(this.Limits)
                .DecodeInteger(type, data, offset, count, canonical);
            return this.CopyIntegerDecode(
                integer,
                AsnRuntimeValueKind.Integer,
                data,
                offset);
        }

        if (type.Kind == AsnTypeKind.Enumerated)
        {
            return this.DecodeEnumerated(data, offset, count, canonical);
        }

        if (type.Kind == AsnTypeKind.Null)
        {
            return this.CreateDecodedPrimitive(
                new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null },
                [],
                0,
                true);
        }

        if (type.Kind == AsnTypeKind.Real)
        {
            return this.DecodeReal(data, offset, count, canonical);
        }

        if (type.Kind == AsnTypeKind.BitString)
        {
            return this.DecodeBitString(type, data, offset, count, canonical);
        }

        if (type.Kind == AsnTypeKind.OctetString)
        {
            return this.DecodeOctetString(type, data, offset, count, canonical);
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifier
            || type.Kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return this.DecodeObjectIdentifier(
                type.Kind,
                data,
                offset,
                count,
                canonical);
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return this.DecodeTextContent(
                type,
                type.Kind,
                data,
                offset,
                count,
                canonical);
        }

        if (this.IsTimeKind(type.Kind))
        {
            return new OerTimeCodec(this.Limits).DecodeTime(
                type,
                data,
                offset,
                count,
                canonical);
        }

        if (this.IsCharacterStringKind(type.Kind))
        {
            AsnTypeKind contentKind = type.Kind == AsnTypeKind.ObjectDescriptor
                ? AsnTypeKind.GraphicString
                : type.Kind;
            return this.DecodeTextContent(
                type,
                contentKind,
                data,
                offset,
                count,
                canonical);
        }

        return this.CreatePrimitiveError(
            "ASNO0022",
            "The requested type is not an OER primitive type.",
            false);
    }

    internal OerPrimitiveResult DecodeBoolean(
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        if (count == 0)
        {
            return new OerPrimitiveResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                HasViolationOffset = true,
                ViolationOffset = 0
            };
        }

        int encoded = data[offset];
        if (canonical && encoded != 0 && encoded != 255)
        {
            return this.CreatePrimitiveError(
                "ASNO0023",
                "COER requires BOOLEAN TRUE to use the all-ones octet.",
                false);
        }

        return this.CreateDecodedPrimitive(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Boolean,
                HasBooleanValue = true,
                BooleanValue = encoded != 0
            },
            [(byte)encoded],
            1,
            encoded == 0 || encoded == 255);
    }

    internal OerPrimitiveResult EncodeEnumerated(AsnRuntimeValue value)
    {
        System.Numerics.BigInteger number = value.IntegerValue;
        if (number >= System.Numerics.BigInteger.Zero
            && number <= new System.Numerics.BigInteger(127))
        {
            return this.CreateEncodedPrimitive(
                value,
                [(byte)number],
                true);
        }

        byte[] content = BigIntegerConversions.ToSignedBigEndian(number);
        if (content.Length > 127
            || !new ResourceCounter(this.Limits).AllowsIntegerBits(
                content.Length * 8))
        {
            return this.CreatePrimitiveError(
                "ASNR0010",
                "The ENUMERATED value exceeds the OER long-form limit.",
                true);
        }

        byte[] data = new byte[content.Length + 1];
        data[0] = (byte)(128 | content.Length);
        System.Buffer.BlockCopy(content, 0, data, 1, content.Length);
        return this.CreateEncodedPrimitive(value, data, true);
    }

    internal OerPrimitiveResult DecodeEnumerated(
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        if (count == 0)
        {
            return new OerPrimitiveResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                HasViolationOffset = true,
                ViolationOffset = 0
            };
        }

        int first = data[offset];
        if (first < 128)
        {
            return this.CreateDecodedIntegerPrimitive(
                AsnRuntimeValueKind.Enumerated,
                new System.Numerics.BigInteger(first),
                [(byte)first],
                true);
        }

        int contentCount = first & 127;
        if (contentCount == 0)
        {
            return this.CreatePrimitiveError(
                "ASNO0024",
                "The OER ENUMERATED long form must contain value octets.",
                false);
        }

        if (count - 1 < contentCount)
        {
            return new OerPrimitiveResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = count,
                HasViolationOffset = true,
                ViolationOffset = count
            };
        }

        if (!new ResourceCounter(this.Limits).AllowsIntegerBits(
            contentCount * 8))
        {
            return this.CreatePrimitiveError(
                "ASNR0010",
                "The ENUMERATED value exceeds the integer-bit limit.",
                true);
        }

        byte[] content = new byte[contentCount];
        System.Buffer.BlockCopy(data, offset + 1, content, 0, contentCount);
        System.Numerics.BigInteger number =
            BigIntegerConversions.FromSignedBigEndian(content);
        bool minimalContent = new OerIntegerCodec(this.Limits)
            .IsMinimalContent(content, true);
        bool shortValue = number >= System.Numerics.BigInteger.Zero
            && number <= new System.Numerics.BigInteger(127);
        if (canonical && (shortValue || !minimalContent))
        {
            return this.CreatePrimitiveErrorAt(
                "ASNO0025",
                "The COER ENUMERATED value does not use its shortest form.",
                false,
                shortValue ? 0 : 1);
        }

        byte[] encoded = new byte[contentCount + 1];
        System.Buffer.BlockCopy(data, offset, encoded, 0, encoded.Length);
        return this.CreateDecodedIntegerPrimitive(
            AsnRuntimeValueKind.Enumerated,
            number,
            encoded,
            !shortValue && minimalContent);
    }

    internal OerPrimitiveResult EncodeReal(AsnRuntimeValue value)
    {
        if (value.RealValue == null)
        {
            return this.CreatePrimitiveError(
                "ASNO0021",
                "The runtime value does not contain a REAL value.",
                false);
        }

        BerPrimitiveValueResult content = new BerRealCodec(this.Limits)
            .EncodeReal(value.RealValue, AsnEncodingRules.Der);
        if (!content.Success)
        {
            return this.CopyBerFailure(content);
        }

        return this.CopyEncodedContent(
            new OerContentCodec(this.Limits).EncodeVariableContent(
                content.Content),
            value);
    }

    internal OerPrimitiveResult DecodeReal(
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        OerContentResult framed = new OerContentCodec(this.Limits)
            .DecodeVariableContent(data, offset, count, canonical);
        if (!framed.Success)
        {
            return this.CopyContentFailure(framed);
        }

        BerRealCodec reals = new BerRealCodec(this.Limits);
        BerPrimitiveValueResult decoded = reals.DecodeReal(framed.Content);
        if (!decoded.Success || decoded.Value == null
            || decoded.Value.RealValue == null)
        {
            return this.CopyBerFailure(decoded);
        }

        BerPrimitiveValueResult canonicalContent = reals.EncodeReal(
            decoded.Value.RealValue,
            AsnEncodingRules.Der);
        if (!canonicalContent.Success
            || !this.BytesEqual(canonicalContent.Content, framed.Content))
        {
            return this.CreatePrimitiveErrorAt(
                "ASNO0026",
                "OER REAL content must use the DER contents-octet form.",
                false,
                framed.ConsumedCount - framed.Content.Length);
        }

        return this.CreateDecodedPrimitive(
            decoded.Value,
            framed.Data,
            framed.ConsumedCount,
            framed.Minimal);
    }

    internal OerPrimitiveResult EncodeBitString(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnBitStringValue? bits = value.BitStringValue;
        if (bits == null || !bits.IsValid())
        {
            return this.CreatePrimitiveError(
                "ASNO0027",
                "The BIT STRING value is invalid.",
                false);
        }

        OerSizeResult size = this.GetFixedSize(type);
        if (size.Fixed)
        {
            if (bits.BitLength != size.Size)
            {
                return this.CreatePrimitiveError(
                    "ASNO0028",
                    "The BIT STRING does not match its fixed OER size.",
                    false);
            }

            return this.CopyEncodedContent(
                new OerContentCodec(this.Limits).EncodeFixedContent(
                    bits.Bytes,
                    bits.Bytes.Length),
                value);
        }

        byte[] content = new byte[bits.Bytes.Length + 1];
        content[0] = (byte)(bits.Bytes.Length * 8 - bits.BitLength);
        System.Buffer.BlockCopy(bits.Bytes, 0, content, 1, bits.Bytes.Length);
        return this.CopyEncodedContent(
            new OerContentCodec(this.Limits).EncodeVariableContent(content),
            value);
    }

    internal OerPrimitiveResult DecodeBitString(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        OerSizeResult size = this.GetFixedSize(type);
        OerContentResult framed;
        int unused;
        if (size.Fixed)
        {
            if (size.Size > int.MaxValue - 7)
            {
                return this.CreatePrimitiveError(
                    "ASNR0013",
                    "The fixed BIT STRING size exceeds the supported range.",
                    true);
            }

            int octets = (size.Size + 7) / 8;
            framed = new OerContentCodec(this.Limits).DecodeFixedContent(
                data,
                offset,
                count,
                octets);
            unused = octets * 8 - size.Size;
        }
        else
        {
            framed = new OerContentCodec(this.Limits).DecodeVariableContent(
                data,
                offset,
                count,
                canonical);
            if (!framed.Success)
            {
                return this.CopyContentFailure(framed);
            }

            if (framed.Content.Length == 0 || framed.Content[0] > 7)
            {
                return this.CreatePrimitiveErrorAt(
                    "ASNO0029",
                    "A variable BIT STRING requires a valid unused-bit octet.",
                    false,
                    framed.ConsumedCount - framed.Content.Length);
            }

            unused = framed.Content[0];
            byte[] payload = new byte[framed.Content.Length - 1];
            System.Buffer.BlockCopy(
                framed.Content,
                1,
                payload,
                0,
                payload.Length);
            framed.Content = payload;
        }

        if (!framed.Success)
        {
            return this.CopyContentFailure(framed);
        }

        if (framed.Content.Length == 0 && unused != 0)
        {
            return this.CreatePrimitiveError(
                "ASNO0030",
                "An empty BIT STRING cannot declare unused bits.",
                false);
        }

        if (unused != 0
            && (framed.Content[framed.Content.Length - 1]
                & (1 << unused) - 1) != 0)
        {
            return this.CreatePrimitiveErrorAt(
                "ASNO0031",
                "Unused OER BIT STRING bits must be zero.",
                false,
                framed.ConsumedCount - 1);
        }

        if (framed.Content.Length > int.MaxValue / 8)
        {
            return this.CreatePrimitiveError(
                "ASNR0013",
                "The BIT STRING bit length exceeds the supported range.",
                true);
        }

        return this.CreateDecodedPrimitive(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = framed.Content,
                    BitLength = framed.Content.Length * 8 - unused
                }
            },
            framed.Data,
            framed.ConsumedCount,
            framed.Minimal);
    }

    internal OerPrimitiveResult EncodeOctetString(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        OerSizeResult size = this.GetFixedSize(type);
        OerContentResult content = size.Fixed
            ? new OerContentCodec(this.Limits).EncodeFixedContent(
                value.Octets,
                size.Size)
            : new OerContentCodec(this.Limits).EncodeVariableContent(
                value.Octets);
        return this.CopyEncodedContent(content, value);
    }

    internal OerPrimitiveResult DecodeOctetString(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        OerSizeResult size = this.GetFixedSize(type);
        OerContentResult content = size.Fixed
            ? new OerContentCodec(this.Limits).DecodeFixedContent(
                data,
                offset,
                count,
                size.Size)
            : new OerContentCodec(this.Limits).DecodeVariableContent(
                data,
                offset,
                count,
                canonical);
        if (!content.Success)
        {
            return this.CopyContentFailure(content);
        }

        return this.CreateDecodedPrimitive(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = content.Content
            },
            content.Data,
            content.ConsumedCount,
            content.Minimal);
    }

    internal OerPrimitiveResult EncodeObjectIdentifier(
        AsnTypeKind kind,
        AsnRuntimeValue value)
    {
        if (value.ObjectIdentifierValue == null)
        {
            return this.CreatePrimitiveError(
                "ASNO0021",
                "The runtime value does not contain an object identifier.",
                false);
        }

        BerPrimitiveValueResult encoded = new BerObjectIdentifierCodec(
            this.Limits).EncodeObjectIdentifier(
                value.ObjectIdentifierValue,
                kind == AsnTypeKind.RelativeObjectIdentifier);
        if (!encoded.Success)
        {
            return this.CopyBerFailure(encoded);
        }

        return this.CopyEncodedContent(
            new OerContentCodec(this.Limits).EncodeVariableContent(
                encoded.Content),
            value);
    }

    internal OerPrimitiveResult DecodeObjectIdentifier(
        AsnTypeKind kind,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        OerContentResult content = new OerContentCodec(this.Limits)
            .DecodeVariableContent(data, offset, count, canonical);
        if (!content.Success)
        {
            return this.CopyContentFailure(content);
        }

        BerPrimitiveValueResult decoded = new BerObjectIdentifierCodec(
            this.Limits).DecodeObjectIdentifier(
                content.Content,
                kind == AsnTypeKind.RelativeObjectIdentifier);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CopyBerFailure(decoded);
        }

        return this.CreateDecodedPrimitive(
            decoded.Value,
            content.Data,
            content.ConsumedCount,
            content.Minimal);
    }

    internal OerPrimitiveResult EncodeTextContent(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        AsnTypeKind contentKind)
    {
        BerPrimitiveValueResult encoded = new BerCharacterStringCodec(
            this.Limits).EncodeText(contentKind, value.TextValue);
        if (!encoded.Success)
        {
            return this.CopyBerFailure(encoded);
        }

        OerSizeResult size = this.GetFixedSize(type);
        int width = this.GetKnownCharacterWidth(contentKind);
        OerContentResult content;
        if (size.Fixed && width > 0)
        {
            if (size.Size > int.MaxValue / width)
            {
                return this.CreatePrimitiveError(
                    "ASNR0009",
                    "The fixed character-string size exceeds the supported range.",
                    true);
            }

            content = new OerContentCodec(this.Limits).EncodeFixedContent(
                encoded.Content,
                size.Size * width);
        }
        else
        {
            content = new OerContentCodec(this.Limits).EncodeVariableContent(
                encoded.Content);
        }

        return this.CopyEncodedContent(content, value);
    }

    internal OerPrimitiveResult DecodeTextContent(
        AsnTypeDescriptor type,
        AsnTypeKind contentKind,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        OerSizeResult size = this.GetFixedSize(type);
        int width = this.GetKnownCharacterWidth(contentKind);
        OerContentResult content;
        if (size.Fixed && width > 0)
        {
            if (size.Size > int.MaxValue / width)
            {
                return this.CreatePrimitiveError(
                    "ASNR0009",
                    "The fixed character-string size exceeds the supported range.",
                    true);
            }

            content = new OerContentCodec(this.Limits).DecodeFixedContent(
                data,
                offset,
                count,
                size.Size * width);
        }
        else
        {
            content = new OerContentCodec(this.Limits).DecodeVariableContent(
                data,
                offset,
                count,
                canonical);
        }

        if (!content.Success)
        {
            return this.CopyContentFailure(content);
        }

        BerPrimitiveValueResult decoded = new BerCharacterStringCodec(
            this.Limits).DecodeText(contentKind, content.Content);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CopyBerFailure(decoded);
        }

        if (canonical)
        {
            BerPrimitiveValueResult canonicalContent =
                new BerCharacterStringCodec(this.Limits).EncodeText(
                    contentKind,
                    decoded.Value.TextValue);
            if (!canonicalContent.Success
                || !this.BytesEqual(
                    canonicalContent.Content,
                    content.Content))
            {
                return this.CreatePrimitiveError(
                    "ASNO0032",
                    "COER character-string content is not in canonical form.",
                    false);
            }
        }

        return this.CreateDecodedPrimitive(
            decoded.Value,
            content.Data,
            content.ConsumedCount,
            content.Minimal);
    }

    internal OerSizeResult GetFixedSize(AsnTypeDescriptor type)
    {
        if (type.Resolution != null)
        {
            SizeConstraintResult? resolved = type.Resolution
                .EncodingConstraints.Oer.SizeConstraints;
            if (resolved != null
                && resolved.HasRootLowerBound
                && resolved.HasRootUpperBound
                && resolved.RootLowerBound == resolved.RootUpperBound
                && resolved.RootLowerBound >= System.Numerics.BigInteger.Zero
                && resolved.RootLowerBound <= new System.Numerics.BigInteger(
                    int.MaxValue))
            {
                return new OerSizeResult
                {
                    Fixed = true,
                    Size = (int)resolved.RootLowerBound
                };
            }
        }

        int constraintIndex = 0;
        while (constraintIndex < type.Constraints.Length)
        {
            AsnConstraintDescriptor constraint = type.Constraints[constraintIndex];
            if (!constraint.Extensible && constraint.Root != null)
            {
                OerSizeResult size = this.FindFixedSizeElement(
                    constraint.Root);
                if (size.Fixed)
                {
                    return size;
                }
            }

            constraintIndex = constraintIndex + 1;
        }

        return new OerSizeResult();
    }

    internal OerSizeResult FindFixedSizeElement(AsnConstraintElement element)
    {
        if (element.Kind == AsnConstraintKind.Size)
        {
            if (element.HasLowerBound
                && element.HasUpperBound
                && element.LowerBound == element.UpperBound
                && element.LowerBound >= System.Numerics.BigInteger.Zero
                && element.LowerBound <= new System.Numerics.BigInteger(
                    int.MaxValue))
            {
                return new OerSizeResult
                {
                    Fixed = true,
                    Size = (int)element.LowerBound
                };
            }

            int sizeIndex = 0;
            while (sizeIndex < element.Children.Length)
            {
                AsnConstraintElement child = element.Children[sizeIndex];
                if (child.HasLowerBound
                    && child.HasUpperBound
                    && child.LowerBound == child.UpperBound
                    && child.LowerBound >= System.Numerics.BigInteger.Zero
                    && child.LowerBound <= new System.Numerics.BigInteger(
                        int.MaxValue))
                {
                    return new OerSizeResult
                    {
                        Fixed = true,
                        Size = (int)child.LowerBound
                    };
                }

                sizeIndex = sizeIndex + 1;
            }
        }

        int index = 0;
        while (index < element.Children.Length)
        {
            OerSizeResult nested = this.FindFixedSizeElement(
                element.Children[index]);
            if (nested.Fixed)
            {
                return nested;
            }

            index = index + 1;
        }

        return new OerSizeResult();
    }

    internal int GetKnownCharacterWidth(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.Ia5String
            || kind == AsnTypeKind.VisibleString
            || kind == AsnTypeKind.NumericString
            || kind == AsnTypeKind.PrintableString)
        {
            return 1;
        }

        if (kind == AsnTypeKind.BmpString)
        {
            return 2;
        }

        return kind == AsnTypeKind.UniversalString ? 4 : 0;
    }

    internal bool HasExpectedRuntimeKind(
        AsnTypeKind kind,
        AsnRuntimeValue value)
    {
        if (this.IsTimeKind(kind))
        {
            return value.Kind == AsnRuntimeValueKind.Time
                && value.TimeValue != null;
        }

        if (kind == AsnTypeKind.ObjectDescriptor)
        {
            return value.Kind == AsnRuntimeValueKind.CharacterString
                && value.TextValue != null;
        }

        return new BerPrimitiveCodec(this.Limits).HasExpectedRuntimeKind(
            kind,
            value);
    }

    internal bool IsCharacterStringKind(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.ObjectDescriptor
            || kind == AsnTypeKind.Utf8String
            || kind == AsnTypeKind.NumericString
            || kind == AsnTypeKind.PrintableString
            || kind == AsnTypeKind.TeletexString
            || kind == AsnTypeKind.VideotexString
            || kind == AsnTypeKind.Ia5String
            || kind == AsnTypeKind.GraphicString
            || kind == AsnTypeKind.VisibleString
            || kind == AsnTypeKind.GeneralString
            || kind == AsnTypeKind.UniversalString
            || kind == AsnTypeKind.BmpString;
    }

    internal bool IsTimeKind(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.UtcTime
            || kind == AsnTypeKind.GeneralizedTime
            || kind == AsnTypeKind.Time
            || kind == AsnTypeKind.Date
            || kind == AsnTypeKind.TimeOfDay
            || kind == AsnTypeKind.DateTime
            || kind == AsnTypeKind.Duration;
    }

    internal bool BytesEqual(byte[] left, byte[] right)
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

    internal OerPrimitiveResult CopyIntegerEncode(
        OerIntegerResult integer,
        AsnRuntimeValue value)
    {
        if (!integer.Success)
        {
            return new OerPrimitiveResult
            {
                Status = integer.Status,
                LimitExceeded = integer.LimitExceeded,
                ErrorCode = integer.ErrorCode,
                ErrorMessage = integer.ErrorMessage
            };
        }

        return this.CreateEncodedPrimitive(
            value,
            integer.Data,
            integer.Minimal);
    }

    internal OerPrimitiveResult CopyIntegerDecode(
        OerIntegerResult integer,
        AsnRuntimeValueKind kind,
        byte[] source,
        int offset)
    {
        if (!integer.Success)
        {
            return new OerPrimitiveResult
            {
                Status = integer.Status,
                ConsumedCount = integer.ConsumedCount,
                Minimal = integer.Minimal,
                HasViolationOffset = integer.HasViolationOffset,
                ViolationOffset = integer.ViolationOffset,
                LimitExceeded = integer.LimitExceeded,
                ErrorCode = integer.ErrorCode,
                ErrorMessage = integer.ErrorMessage
            };
        }

        byte[] data = new byte[integer.ConsumedCount];
        System.Buffer.BlockCopy(
            source,
            offset,
            data,
            0,
            integer.ConsumedCount);
        return this.CreateDecodedPrimitive(
            new AsnRuntimeValue
            {
                Kind = kind,
                HasIntegerValue = true,
                IntegerValue = integer.Value
            },
            data,
            integer.ConsumedCount,
            integer.Minimal);
    }

    internal OerPrimitiveResult CopyEncodedContent(
        OerContentResult content,
        AsnRuntimeValue value)
    {
        if (!content.Success)
        {
            return this.CopyContentFailure(content);
        }

        return this.CreateEncodedPrimitive(
            value,
            content.Data,
            content.Minimal);
    }

    internal OerPrimitiveResult CopyContentFailure(OerContentResult content)
    {
        return new OerPrimitiveResult
        {
            Status = content.Status,
            ConsumedCount = content.ConsumedCount,
            Minimal = content.Minimal,
            HasViolationOffset = content.HasViolationOffset,
            ViolationOffset = content.ViolationOffset,
            LimitExceeded = content.LimitExceeded,
            ErrorCode = content.ErrorCode,
            ErrorMessage = content.ErrorMessage
        };
    }

    internal OerPrimitiveResult CopyBerFailure(BerPrimitiveValueResult content)
    {
        return this.CreatePrimitiveError(
            content.ErrorCode,
            content.ErrorMessage,
            content.LimitExceeded);
    }

    internal OerPrimitiveResult CreateEncodedPrimitive(
        AsnRuntimeValue value,
        byte[] data,
        bool minimal)
    {
        return new OerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = data,
            ConsumedCount = data.Length,
            Minimal = minimal
        };
    }

    internal OerPrimitiveResult CreateDecodedPrimitive(
        AsnRuntimeValue value,
        byte[] data,
        int consumedCount,
        bool minimal)
    {
        return new OerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = data,
            ConsumedCount = consumedCount,
            Minimal = minimal
        };
    }

    internal OerPrimitiveResult CreateDecodedIntegerPrimitive(
        AsnRuntimeValueKind kind,
        System.Numerics.BigInteger number,
        byte[] data,
        bool minimal)
    {
        return this.CreateDecodedPrimitive(
            new AsnRuntimeValue
            {
                Kind = kind,
                HasIntegerValue = true,
                IntegerValue = number
            },
            data,
            data.Length,
            minimal);
    }

    internal OerPrimitiveResult CreatePrimitiveError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerPrimitiveResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal OerPrimitiveResult CreatePrimitiveErrorAt(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset)
    {
        OerPrimitiveResult result = this.CreatePrimitiveError(
            code,
            message,
            limitExceeded);
        result.HasViolationOffset = true;
        result.ViolationOffset = violationOffset;
        return result;
    }
}
