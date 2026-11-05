namespace asn1;

internal class BerCanonicalValidator
{
    internal AsnResourceLimits Limits;
    internal AsnEncodingRules ActiveRules = AsnEncodingRules.Der;

    internal BerCanonicalValidator(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal AsnCanonicalityResult ValidateRequest(AsnDecodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Data == null)
        {
            throw new System.ArgumentException(
                "The canonicality request data cannot be null.",
                "request");
        }

        int count = request.HasCount
            ? request.Count
            : request.Data.Length - request.Offset;
        if (!CursorMath.IsRangeValid(
            request.Offset,
            count,
            request.Data.Length))
        {
            throw new System.ArgumentOutOfRangeException("request");
        }

        AsnEncodingRules rules = request.Options == null
            ? AsnEncodingRules.Der
            : request.Options.EncodingRules;
        AsnResourceLimits limits = request.Options == null
                || request.Options.ResourceLimits == null
            ? this.Limits
            : request.Options.ResourceLimits;
        return new BerCanonicalValidator(limits).ValidateInput(
            request.Data,
            request.Offset,
            count,
            rules);
    }

    internal AsnCanonicalityResult ValidateInput(
        byte[] data,
        int offset,
        int count,
        AsnEncodingRules rules)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        this.ActiveRules = rules;
        if (rules != AsnEncodingRules.Der
            && rules != AsnEncodingRules.Cer)
        {
            return this.CreateResult(
                AsnCanonicalityStatus.Invalid,
                offset,
                "ASNCAN0001",
                "BER canonicality validation requires DER or CER rules.");
        }

        BerReadResult read = new BerTlvReaderImplementation(this.Limits).Read(
            new BerReadRequest
            {
                Data = data,
                Offset = offset,
                HasCount = true,
                Count = count,
                EncodingRules = AsnEncodingRules.Ber,
                PreserveEncodedData = true
            });
        if (read.Status != AsnDecodeStatus.Done || read.Tlv == null)
        {
            string code = read.Diagnostics.Length == 0
                ? "ASNCAN0010"
                : read.Diagnostics[0].Code;
            string message = read.Diagnostics.Length == 0
                ? "The input does not contain one complete BER value."
                : read.Diagnostics[0].Message;
            if (code == "ASND0017")
            {
                int markerOffset = read.ConsumedCount < 2
                    ? offset
                    : offset + read.ConsumedCount - 2;
                return this.CreateResult(
                    AsnCanonicalityStatus.Invalid,
                    markerOffset,
                    "ASNCAN0010",
                    "An end-of-contents marker is not a standalone value.");
            }

            return this.CreateResult(
                AsnCanonicalityStatus.Invalid,
                offset + read.ConsumedCount,
                code,
                message);
        }

        if (read.ConsumedCount != count)
        {
            return this.CreateResult(
                AsnCanonicalityStatus.Invalid,
                offset + read.ConsumedCount,
                "ASNCAN0009",
                "Canonical input must contain exactly one value.");
        }

        BerCanonicalValidationState state =
            new BerCanonicalValidationState();
        this.ValidateNode(read.Tlv, rules, state);
        if (!state.HasViolation)
        {
            return new AsnCanonicalityResult
            {
                Status = AsnCanonicalityStatus.Canonical,
                HasEncodingRules = true,
                EncodingRules = rules,
                Path = "$"
            };
        }

        return this.CreateResult(
            state.Invalid
                ? AsnCanonicalityStatus.Invalid
                : AsnCanonicalityStatus.NonCanonical,
            state.Offset,
            state.Code,
            state.Message);
    }

    internal void ValidateNode(
        BerTlv node,
        AsnEncodingRules rules,
        BerCanonicalValidationState state)
    {
        int contentOffset = this.ValidateHeader(node, rules, state);
        if (node.Tag.Class == AsnTagClass.Universal
            && node.Tag.Number == System.Numerics.BigInteger.Zero)
        {
            state.RecordViolation(
                node.EncodedOffset,
                "ASNCAN0010",
                "An end-of-contents marker is not a standalone value.",
                true);
        }
        if (!node.Constructed
            && node.Tag.Class == AsnTagClass.Universal
            && node.Tag.Number <= new System.Numerics.BigInteger(int.MaxValue)
            && this.RequiresConstructedUniversalForm((int)node.Tag.Number))
        {
            state.RecordViolation(
                node.EncodedOffset,
                "ASNCAN0010",
                "This universal value must use constructed encoding.",
                true);
        }

        AsnTypeKind stringKind = this.GetStringKind(node);
        if (stringKind != AsnTypeKind.Unknown)
        {
            this.ValidateStringForm(
                node,
                stringKind,
                rules,
                contentOffset,
                state);
        }
        else if (!node.Constructed)
        {
            this.ValidateScalar(node, rules, contentOffset, state);
        }
        else if (node.Tag.Class == AsnTagClass.Universal
            && node.Tag.Number <= new System.Numerics.BigInteger(int.MaxValue)
            && this.RequiresPrimitiveUniversalForm((int)node.Tag.Number))
        {
            state.RecordViolation(
                node.EncodedOffset,
                "ASNCAN0004",
                "This universal value must use primitive encoding.",
                true);
        }
        if (node.Constructed)
        {
            int index = 0;
            while (index < node.Children.Length)
            {
                this.ValidateNode(node.Children[index], rules, state);
                index = index + 1;
            }

            if (node.Tag.Class == AsnTagClass.Universal
                && node.Tag.Number
                    == new System.Numerics.BigInteger(AsnUniversalTags.Set))
            {
                this.ValidateSetOrder(node, state);
            }
        }
    }

    internal int ValidateHeader(
        BerTlv node,
        AsnEncodingRules rules,
        BerCanonicalValidationState state)
    {
        BerIdentifierReadResult identifier = new BerIdentifierReader(
            this.Limits).ReadIdentifier(node.EncodedData, false);
        int identifierLength = identifier.ConsumedCount;
        BerIdentifierWriteResult canonicalIdentifier =
            new BerIdentifierWriter(this.Limits).WriteIdentifier(
                node.Tag,
                node.Constructed);
        if (!identifier.Minimal && canonicalIdentifier.Success)
        {
            int difference = this.FindDifference(
                node.EncodedData,
                0,
                identifierLength,
                canonicalIdentifier.Data);
            state.RecordViolation(
                node.EncodedOffset + difference,
                "ASNCAN0002",
                "The identifier does not use the shortest form.",
                false);
        }

        BerLengthReadResult length = new BerLengthReader(this.Limits)
            .ReadLength(
                new System.ReadOnlySpan<byte>(
                    node.EncodedData,
                    identifierLength,
                    node.EncodedData.Length - identifierLength),
                false);
        int lengthOffset = node.EncodedOffset + identifierLength;
        if (!length.Minimal)
        {
            BerLengthWriteResult canonicalLength = new BerLengthWriter(
                this.Limits).WriteLength(length.Length, false);
            if (canonicalLength.Success)
            {
                int difference = this.FindDifference(
                    node.EncodedData,
                    identifierLength,
                    length.ConsumedCount,
                    canonicalLength.Data);
                state.RecordViolation(
                    node.EncodedOffset + identifierLength + difference,
                    "ASNCAN0003",
                    "The length determinant does not use the shortest form.",
                    false);
            }
        }

        if (rules == AsnEncodingRules.Der && length.Indefinite)
        {
            state.RecordViolation(
                lengthOffset,
                "ASNCAN0003",
                "DER requires definite lengths.",
                false);
        }
        else if (rules == AsnEncodingRules.Cer
            && node.Constructed
            && !length.Indefinite)
        {
            state.RecordViolation(
                lengthOffset,
                "ASNCAN0003",
                "CER requires indefinite length for constructed values.",
                false);
        }

        return node.EncodedOffset + identifierLength + length.ConsumedCount;
    }

    internal void ValidateStringForm(
        BerTlv node,
        AsnTypeKind kind,
        AsnEncodingRules rules,
        int contentOffset,
        BerCanonicalValidationState state)
    {
        if (rules == AsnEncodingRules.Der)
        {
            if (node.Constructed)
            {
                state.RecordViolation(
                    node.EncodedOffset,
                    "ASNCAN0004",
                    "DER requires primitive string encoding.",
                    false);
                return;
            }

            this.ValidateStringScalar(node, kind, rules, contentOffset, state);
            return;
        }

        if (!node.Constructed)
        {
            if (node.Content.Length > 1000)
            {
                state.RecordViolation(
                    node.EncodedOffset,
                    "ASNCAN0007",
                    "CER requires strings longer than 1000 content octets to be segmented.",
                    false);
            }

            this.ValidateStringScalar(node, kind, rules, contentOffset, state);
            return;
        }

        this.ValidateCerSegments(node, kind, state);
    }

    internal void ValidateCerSegments(
        BerTlv node,
        AsnTypeKind kind,
        BerCanonicalValidationState state)
    {
        int logicalLength = kind == AsnTypeKind.BitString ? 1 : 0;
        int index = 0;
        while (index < node.Children.Length)
        {
            BerTlv child = node.Children[index];
            if (child.Tag.Class != node.Tag.Class
                || child.Tag.Number != node.Tag.Number
                || child.Constructed)
            {
                state.RecordViolation(
                    child.EncodedOffset,
                    "ASNCAN0007",
                    "CER string fragments must be primitive values with the enclosing tag.",
                    false);
            }

            int contentLength = child.Content.Length;
            bool final = index == node.Children.Length - 1;
            if (!final
                    && !this.IsValidNonFinalStringLength(
                        kind,
                        child,
                        node.Children[index + 1])
                || final && (contentLength == 0 || contentLength > 1000))
            {
                state.RecordViolation(
                    child.EncodedOffset,
                    "ASNCAN0007",
                    "CER string fragments must contain 1000 octets except for the final shorter fragment.",
                    false);
            }

            if (kind == AsnTypeKind.BitString)
            {
                if (contentLength == 0)
                {
                    state.RecordViolation(
                        child.EncodedOffset,
                        "ASNCAN0007",
                        "A CER BIT STRING fragment requires an unused-bit octet.",
                        true);
                }
                else
                {
                    logicalLength = logicalLength + contentLength - 1;
                    if (!final && child.Content[0] != 0)
                    {
                        int childContentOffset = this.GetContentOffset(child);
                        state.RecordViolation(
                            childContentOffset,
                            "ASNCAN0006",
                            "Only the final CER BIT STRING fragment may declare unused bits.",
                            false);
                    }
                }
            }
            else
            {
                logicalLength = logicalLength + contentLength;
            }

            index = index + 1;
        }

        if (logicalLength <= 1000)
        {
            state.RecordViolation(
                node.EncodedOffset,
                "ASNCAN0007",
                "CER requires primitive encoding for strings of at most 1000 content octets.",
                false);
        }
    }

    internal void ValidateStringScalar(
        BerTlv node,
        AsnTypeKind kind,
        AsnEncodingRules rules,
        int contentOffset,
        BerCanonicalValidationState state)
    {
        if (kind == AsnTypeKind.BitString
            && this.ValidateBitPadding(node.Content, contentOffset, state))
        {
            return;
        }

        BerPrimitiveValueResult decoded;
        BerPrimitiveValueResult encoded;
        if (node.Tag.Class == AsnTagClass.Universal
            && node.Tag.Number
                == new System.Numerics.BigInteger(
                    AsnUniversalTags.ObjectDescriptor))
        {
            BerCharacterStringCodec characters =
                new BerCharacterStringCodec(this.Limits);
            decoded = characters.DecodeText(
                AsnTypeKind.GraphicString,
                node.Content);
            encoded = decoded.Success && decoded.Value != null
                ? characters.EncodeText(
                    AsnTypeKind.GraphicString,
                    decoded.Value.TextValue)
                : decoded;
        }
        else
        {
            BerPrimitiveCodec primitives = new BerPrimitiveCodec(this.Limits);
            decoded = primitives.DecodePrimitiveContent(
                kind,
                node.Content,
                AsnEncodingRules.Ber);
            encoded = decoded.Success && decoded.Value != null
                ? primitives.EncodePrimitiveContent(
                    kind,
                    decoded.Value,
                    rules)
                : decoded;
        }

        this.CompareScalarResult(
            node,
            decoded,
            encoded,
            contentOffset,
            state);
    }

    internal void ValidateScalar(
        BerTlv node,
        AsnEncodingRules rules,
        int contentOffset,
        BerCanonicalValidationState state)
    {
        if (node.Tag.Class != AsnTagClass.Universal)
        {
            return;
        }

        if (node.Tag.Number > new System.Numerics.BigInteger(int.MaxValue))
        {
            return;
        }

        int tag = (int)node.Tag.Number;
        AsnTypeKind timeKind = this.GetTimeKind(tag);
        if (timeKind != AsnTypeKind.Unknown)
        {
            this.ValidateTimeScalar(
                node,
                timeKind,
                rules,
                contentOffset,
                state);
            return;
        }

        AsnTypeKind kind = this.GetPrimitiveKind(tag);
        if (kind == AsnTypeKind.Unknown)
        {
            return;
        }

        if ((kind == AsnTypeKind.Integer
                || kind == AsnTypeKind.Enumerated)
            && this.HasRedundantIntegerOctet(node.Content))
        {
            state.RecordViolation(
                contentOffset,
                "ASNCAN0005",
                "INTEGER and ENUMERATED content must use the shortest two's-complement form.",
                false);
            return;
        }

        if ((kind == AsnTypeKind.ObjectIdentifier
                || kind == AsnTypeKind.RelativeObjectIdentifier)
            && this.FindRedundantOidGroup(node.Content) >= 0)
        {
            int redundant = this.FindRedundantOidGroup(node.Content);
            state.RecordViolation(
                contentOffset + redundant,
                "ASNCAN0005",
                "Object identifier subidentifiers must use the shortest base-128 form.",
                false);
            return;
        }

        if (kind == AsnTypeKind.BitString
            && this.ValidateBitPadding(node.Content, contentOffset, state))
        {
            return;
        }

        if (kind == AsnTypeKind.Real
            && this.ValidateRealRedundancy(
                node.Content,
                contentOffset,
                state))
        {
            return;
        }

        BerPrimitiveCodec primitives = new BerPrimitiveCodec(this.Limits);
        BerPrimitiveValueResult decoded = primitives.DecodePrimitiveContent(
            kind,
            node.Content,
            AsnEncodingRules.Ber);
        BerPrimitiveValueResult encoded = decoded.Success
                && decoded.Value != null
            ? primitives.EncodePrimitiveContent(
                kind,
                decoded.Value,
                rules)
            : decoded;
        this.CompareScalarResult(
            node,
            decoded,
            encoded,
            contentOffset,
            state);
    }

    internal void ValidateTimeScalar(
        BerTlv node,
        AsnTypeKind kind,
        AsnEncodingRules rules,
        int contentOffset,
        BerCanonicalValidationState state)
    {
        BerTimeCodec times = new BerTimeCodec(this.Limits);
        BerPrimitiveValueResult decoded = times.DecodeTime(kind, node.Content);
        BerPrimitiveValueResult encoded = decoded.Success
                && decoded.Value != null
                && decoded.Value.TimeValue != null
            ? times.EncodeTime(kind, decoded.Value.TimeValue, rules)
            : decoded;
        this.CompareScalarResult(
            node,
            decoded,
            encoded,
            contentOffset,
            state);
    }

    internal void CompareScalarResult(
        BerTlv node,
        BerPrimitiveValueResult decoded,
        BerPrimitiveValueResult encoded,
        int contentOffset,
        BerCanonicalValidationState state)
    {
        if (!decoded.Success || decoded.Value == null)
        {
            state.RecordViolation(
                contentOffset,
                decoded.ErrorCode.Length == 0
                    ? "ASNCAN0010"
                    : decoded.ErrorCode,
                decoded.ErrorMessage.Length == 0
                    ? "The primitive content is invalid."
                    : decoded.ErrorMessage,
                true);
            return;
        }

        if (!encoded.Success)
        {
            state.RecordViolation(
                contentOffset,
                encoded.ErrorCode.Length == 0
                    ? "ASNCAN0010"
                    : encoded.ErrorCode,
                encoded.ErrorMessage.Length == 0
                    ? "The primitive content cannot be canonicalized."
                    : encoded.ErrorMessage,
                true);
            return;
        }

        int difference = this.FindDifference(
            node.Content,
            0,
            node.Content.Length,
            encoded.Content);
        if (difference >= 0)
        {
            state.RecordViolation(
                contentOffset + difference,
                "ASNCAN0005",
                "The primitive content is not in canonical form.",
                false);
        }
    }

    internal bool ValidateBitPadding(
        byte[] content,
        int contentOffset,
        BerCanonicalValidationState state)
    {
        if (content.Length == 0 || content[0] > 7
            || content.Length == 1 && content[0] != 0)
        {
            state.RecordViolation(
                contentOffset,
                "ASNCAN0010",
                "BIT STRING content has an invalid unused-bit count.",
                true);
            return true;
        }

        int unused = content[0];
        if (unused != 0)
        {
            int mask = (1 << unused) - 1;
            if ((content[content.Length - 1] & mask) != 0)
            {
                state.RecordViolation(
                    contentOffset + content.Length - 1,
                    "ASNCAN0006",
                    "Unused BIT STRING padding bits must be zero.",
                    false);
                return true;
            }
        }

        return false;
    }

    internal bool ValidateRealRedundancy(
        byte[] content,
        int contentOffset,
        BerCanonicalValidationState state)
    {
        if (content.Length == 0 || (content[0] & 128) == 0)
        {
            return false;
        }

        int exponentLengthCode = content[0] & 3;
        int exponentLength = exponentLengthCode + 1;
        int exponentOffset = 1;
        if (exponentLengthCode == 3)
        {
            if (content.Length < 2)
            {
                return false;
            }

            exponentLength = content[1];
            exponentOffset = 2;
        }

        if (exponentLength > 1
            && exponentOffset + exponentLength <= content.Length
            && (content[exponentOffset] == 0
                    && (content[exponentOffset + 1] & 128) == 0
                || content[exponentOffset] == 255
                    && (content[exponentOffset + 1] & 128) != 0))
        {
            state.RecordViolation(
                contentOffset + exponentOffset,
                "ASNCAN0005",
                "A canonical binary REAL exponent has no redundant leading octet.",
                false);
            return true;
        }

        int mantissaOffset = exponentOffset + exponentLength;
        if (mantissaOffset < content.Length - 1
            && content[mantissaOffset] == 0)
        {
            state.RecordViolation(
                contentOffset + mantissaOffset,
                "ASNCAN0005",
                "A canonical binary REAL mantissa has no redundant leading octet.",
                false);
            return true;
        }

        return false;
    }

    internal void ValidateSetOrder(
        BerTlv node,
        BerCanonicalValidationState state)
    {
        int index = 1;
        while (index < node.Children.Length)
        {
            if (this.CompareEncoded(
                node.Children[index - 1].EncodedData,
                node.Children[index].EncodedData) > 0)
            {
                state.RecordViolation(
                    node.Children[index].EncodedOffset,
                    "ASNCAN0008",
                    "SET and SET OF elements are not in canonical encoded order.",
                    false);
                return;
            }

            index = index + 1;
        }
    }

    internal int CompareEncoded(byte[] left, byte[] right)
    {
        int count = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < count)
        {
            if (left[index] != right[index])
            {
                return left[index] < right[index] ? -1 : 1;
            }

            index = index + 1;
        }

        if (left.Length == right.Length)
        {
            return 0;
        }

        return left.Length < right.Length ? -1 : 1;
    }

    internal bool IsValidNonFinalStringLength(
        AsnTypeKind kind,
        BerTlv current,
        BerTlv next)
    {
        if (current.Content.Length == 1000)
        {
            return true;
        }

        if (kind != AsnTypeKind.Utf8String
            || current.Content.Length == 0
            || current.Content.Length > 1000
            || next.Content.Length == 0)
        {
            return false;
        }

        int nextCharacterLength = new Utf8ChunkDecoder()
            .GetSequenceLength(next.Content[0]);
        return nextCharacterLength > 0
            && current.Content.Length + nextCharacterLength > 1000;
    }

    internal int FindDifference(
        byte[] actual,
        int actualOffset,
        int actualCount,
        byte[] expected)
    {
        int count = actualCount < expected.Length
            ? actualCount
            : expected.Length;
        int index = 0;
        while (index < count)
        {
            if (actual[actualOffset + index] != expected[index])
            {
                return index;
            }

            index = index + 1;
        }

        return actualCount == expected.Length ? -1 : count;
    }

    internal int GetContentOffset(BerTlv node)
    {
        BerIdentifierReadResult identifier = new BerIdentifierReader(
            this.Limits).ReadIdentifier(node.EncodedData, false);
        BerLengthReadResult length = new BerLengthReader(this.Limits)
            .ReadLength(
                new System.ReadOnlySpan<byte>(
                    node.EncodedData,
                    identifier.ConsumedCount,
                    node.EncodedData.Length - identifier.ConsumedCount),
                false);
        return node.EncodedOffset
            + identifier.ConsumedCount
            + length.ConsumedCount;
    }

    internal bool HasRedundantIntegerOctet(byte[] content)
    {
        return content.Length > 1
            && (content[0] == 0 && (content[1] & 128) == 0
                || content[0] == 255 && (content[1] & 128) != 0);
    }

    internal int FindRedundantOidGroup(byte[] content)
    {
        int index = 0;
        bool start = true;
        while (index < content.Length)
        {
            if (start && content[index] == 128)
            {
                return index;
            }

            start = (content[index] & 128) == 0;
            index = index + 1;
        }

        return -1;
    }

    internal AsnTypeKind GetStringKind(BerTlv node)
    {
        if (node.Tag.Class != AsnTagClass.Universal)
        {
            return AsnTypeKind.Unknown;
        }

        if (node.Tag.Number > new System.Numerics.BigInteger(int.MaxValue))
        {
            return AsnTypeKind.Unknown;
        }

        int tag = (int)node.Tag.Number;
        if (tag == AsnUniversalTags.ObjectDescriptor)
        {
            return AsnTypeKind.GraphicString;
        }

        AsnTypeKind kind = this.GetPrimitiveKind(tag);
        return new BerStringCodec(this.Limits).IsSupportedStringKind(kind)
            ? kind
            : AsnTypeKind.Unknown;
    }

    internal AsnTypeKind GetPrimitiveKind(int tag)
    {
        switch (tag)
        {
            case AsnUniversalTags.Boolean:
                return AsnTypeKind.Boolean;
            case AsnUniversalTags.Integer:
                return AsnTypeKind.Integer;
            case AsnUniversalTags.BitString:
                return AsnTypeKind.BitString;
            case AsnUniversalTags.OctetString:
                return AsnTypeKind.OctetString;
            case AsnUniversalTags.Null:
                return AsnTypeKind.Null;
            case AsnUniversalTags.ObjectIdentifier:
                return AsnTypeKind.ObjectIdentifier;
            case AsnUniversalTags.Real:
                return AsnTypeKind.Real;
            case AsnUniversalTags.Enumerated:
                return AsnTypeKind.Enumerated;
            case AsnUniversalTags.Utf8String:
                return AsnTypeKind.Utf8String;
            case AsnUniversalTags.RelativeObjectIdentifier:
                return AsnTypeKind.RelativeObjectIdentifier;
            case AsnUniversalTags.NumericString:
                return AsnTypeKind.NumericString;
            case AsnUniversalTags.PrintableString:
                return AsnTypeKind.PrintableString;
            case AsnUniversalTags.TeletexString:
                return AsnTypeKind.TeletexString;
            case AsnUniversalTags.VideotexString:
                return AsnTypeKind.VideotexString;
            case AsnUniversalTags.Ia5String:
                return AsnTypeKind.Ia5String;
            case AsnUniversalTags.GraphicString:
                return AsnTypeKind.GraphicString;
            case AsnUniversalTags.VisibleString:
                return AsnTypeKind.VisibleString;
            case AsnUniversalTags.GeneralString:
                return AsnTypeKind.GeneralString;
            case AsnUniversalTags.UniversalString:
                return AsnTypeKind.UniversalString;
            case AsnUniversalTags.BmpString:
                return AsnTypeKind.BmpString;
            case AsnUniversalTags.ObjectIdentifierIri:
                return AsnTypeKind.ObjectIdentifierIri;
            case AsnUniversalTags.RelativeObjectIdentifierIri:
                return AsnTypeKind.RelativeObjectIdentifierIri;
            default:
                return AsnTypeKind.Unknown;
        }
    }

    internal AsnTypeKind GetTimeKind(int tag)
    {
        switch (tag)
        {
            case AsnUniversalTags.UtcTime:
                return AsnTypeKind.UtcTime;
            case AsnUniversalTags.GeneralizedTime:
                return AsnTypeKind.GeneralizedTime;
            case AsnUniversalTags.Time:
                return AsnTypeKind.Time;
            case AsnUniversalTags.Date:
                return AsnTypeKind.Date;
            case AsnUniversalTags.TimeOfDay:
                return AsnTypeKind.TimeOfDay;
            case AsnUniversalTags.DateTime:
                return AsnTypeKind.DateTime;
            case AsnUniversalTags.Duration:
                return AsnTypeKind.Duration;
            default:
                return AsnTypeKind.Unknown;
        }
    }

    internal bool RequiresPrimitiveUniversalForm(int tag)
    {
        return this.GetPrimitiveKind(tag) != AsnTypeKind.Unknown
            || this.GetTimeKind(tag) != AsnTypeKind.Unknown
            || tag == AsnUniversalTags.ObjectDescriptor;
    }

    internal bool RequiresConstructedUniversalForm(int tag)
    {
        return tag == AsnUniversalTags.External
            || tag == AsnUniversalTags.EmbeddedPdv
            || tag == AsnUniversalTags.Sequence
            || tag == AsnUniversalTags.Set
            || tag == AsnUniversalTags.CharacterString;
    }

    internal AsnCanonicalityResult CreateResult(
        AsnCanonicalityStatus status,
        int offset,
        string code,
        string message)
    {
        return new AsnCanonicalityResult
        {
            Status = status,
            HasEncodingRules = true,
            EncodingRules = this.ActiveRules,
            HasViolationOffset = true,
            ViolationOffset = offset,
            Repairable = status == AsnCanonicalityStatus.NonCanonical,
            Path = "$",
            Diagnostics = [new AsnDiagnostic
            {
                Severity = AsnDiagnosticSeverity.Error,
                Category = status == AsnCanonicalityStatus.Invalid
                    ? AsnDiagnosticCategory.Decoding
                    : AsnDiagnosticCategory.Canonicality,
                Code = code,
                Message = message + " Byte offset: "
                    + offset.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + "."
            }]
        };
    }
}
