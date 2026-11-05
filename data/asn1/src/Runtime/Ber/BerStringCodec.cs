namespace asn1;

internal class BerStringCodec
{
    internal AsnResourceLimits Limits;

    internal BerStringCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerStringResult EncodeConstructedString(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        AsnEncodingRules rules,
        int segmentSize,
        bool indefinite)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (!this.IsSupportedStringKind(kind))
        {
            return this.CreateStringError(
                "ASNE0065",
                "The requested type does not support constructed BER string encoding.",
                false);
        }

        if (rules == AsnEncodingRules.Der)
        {
            return this.CreateStringError(
                "ASNE0066",
                "DER requires primitive string encoding.",
                false);
        }

        if (rules == AsnEncodingRules.Cer)
        {
            return this.EncodeCerString(kind, value);
        }

        if (rules != AsnEncodingRules.Ber || segmentSize <= 0)
        {
            return this.CreateStringError(
                "ASNE0067",
                "Constructed string encoding requires BER or CER and a positive segment size.",
                false);
        }

        BerPrimitiveEncodeResult primitive = new BerPrimitiveCodec(this.Limits)
            .EncodePrimitive(kind, value, rules);
        if (!primitive.Success || primitive.Tlv == null)
        {
            return this.CopyEncodeFailure(primitive);
        }

        BerStringResult segmented = kind == AsnTypeKind.BitString
            ? this.SegmentBitString(
                primitive.Tlv.Content,
                primitive.Tlv.Tag.Number,
                segmentSize)
            : this.SegmentOctetString(
                kind,
                primitive.Tlv.Content,
                primitive.Tlv.Tag.Number,
                segmentSize);
        if (!segmented.Success || segmented.Tlv == null)
        {
            return segmented;
        }

        segmented.Tlv.IndefiniteLength = indefinite;
        return segmented;
    }

    internal BerStringResult EncodeCerString(
        AsnTypeKind kind,
        AsnRuntimeValue value)
    {
        if (!this.IsSupportedStringKind(kind))
        {
            return this.CreateStringError(
                "ASNE0065",
                "The requested type does not support CER string encoding.",
                false);
        }

        BerPrimitiveEncodeResult primitive = new BerPrimitiveCodec(this.Limits)
            .EncodePrimitive(kind, value, AsnEncodingRules.Cer);
        if (!primitive.Success || primitive.Tlv == null)
        {
            return this.CopyEncodeFailure(primitive);
        }

        return this.SelectCerStringForm(kind, primitive.Tlv);
    }

    internal BerStringResult SelectCerStringForm(
        AsnTypeKind kind,
        BerTlv primitive)
    {
        if (primitive.Content.Length <= 1000)
        {
            return new BerStringResult
            {
                Success = true,
                Tlv = primitive
            };
        }

        BerStringResult segmented = kind == AsnTypeKind.BitString
            ? this.SegmentBitString(
                primitive.Content,
                primitive.Tag.Number,
                1000)
            : this.SegmentOctetString(
                kind,
                primitive.Content,
                primitive.Tag.Number,
                1000);
        if (segmented.Success && segmented.Tlv != null)
        {
            segmented.Tlv.IndefiniteLength = true;
        }

        return segmented;
    }

    internal BerStringResult DecodeConstructedString(
        AsnTypeKind kind,
        BerTlv tlv,
        AsnEncodingRules rules)
    {
        int tagNumber = new BerPrimitiveCodec(this.Limits)
            .GetUniversalTagNumber(kind);
        return this.DecodeTaggedConstructedString(
            kind,
            tlv,
            rules,
            tagNumber);
    }

    internal BerStringResult DecodeTaggedConstructedString(
        AsnTypeKind kind,
        BerTlv tlv,
        AsnEncodingRules rules,
        int tagNumber)
    {
        if (tlv == null)
        {
            throw new System.ArgumentNullException("tlv");
        }

        if (!this.IsSupportedStringKind(kind)
            || (rules != AsnEncodingRules.Ber
                && rules != AsnEncodingRules.Cer
                && rules != AsnEncodingRules.Der))
        {
            return this.CreateStringError(
                "ASND0079",
                "The requested type or encoding rules do not support BER string decoding.",
                false);
        }

        if (!tlv.IsValid())
        {
            return this.CreateStringError(
                "ASND0080",
                "The BER string TLV tree is internally inconsistent.",
                false);
        }

        if (tlv.Tag.Class != AsnTagClass.Universal
            || tlv.Tag.Number != new System.Numerics.BigInteger(tagNumber))
        {
            return this.CreateStringError(
                "ASND0081",
                "The BER string has the wrong universal tag.",
                false);
        }

        if (!tlv.Constructed)
        {
            int primitiveTag = new BerPrimitiveCodec(this.Limits)
                .GetUniversalTagNumber(kind);
            BerTlv primitiveInput = new BerTlv
            {
                Tag = new AsnTag
                {
                    Class = AsnTagClass.Universal,
                    Number = new System.Numerics.BigInteger(primitiveTag)
                },
                Content = tlv.Content
            };
            BerPrimitiveDecodeResult primitive = new BerPrimitiveCodec(
                this.Limits).DecodePrimitive(kind, primitiveInput, rules);
            return this.CopyDecodeResult(primitive);
        }

        if (rules == AsnEncodingRules.Der)
        {
            return this.CreateStringError(
                "ASND0082",
                "DER does not permit constructed string encoding.",
                false);
        }

        if (rules == AsnEncodingRules.Cer
            && !this.IsCanonicalCerConstructedForm(kind, tlv, tagNumber))
        {
            return this.CreateStringError(
                "ASNCAN0011",
                "The constructed string does not use canonical CER fragments.",
                false);
        }

        BerStringDecodeState state = new BerStringDecodeState();
        ResourceCounter resources = new ResourceCounter(this.Limits);
        this.AppendStringFragments(
            kind,
            tlv,
            rules,
            tagNumber,
            state,
            resources);
        if (state.Failed)
        {
            return this.CreateStringError(
                state.ErrorCode,
                state.ErrorMessage,
                state.LimitExceeded);
        }

        byte[] content;
        if (kind == AsnTypeKind.BitString)
        {
            content = new byte[state.Bytes.Count + 1];
            content[0] = (byte)state.LastUnusedBits;
            System.Buffer.BlockCopy(
                state.Bytes.Buffer,
                0,
                content,
                1,
                state.Bytes.Count);
        }
        else
        {
            content = state.Bytes.ToArray();
        }

        int decodedTag = new BerPrimitiveCodec(this.Limits)
            .GetUniversalTagNumber(kind);
        BerTlv primitiveTlv = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = new System.Numerics.BigInteger(decodedTag)
            },
            Content = content
        };
        AsnEncodingRules primitiveRules = rules == AsnEncodingRules.Cer
            ? AsnEncodingRules.Ber
            : rules;
        BerPrimitiveDecodeResult decoded = new BerPrimitiveCodec(this.Limits)
            .DecodePrimitive(kind, primitiveTlv, primitiveRules);
        return this.CopyDecodeResult(decoded);
    }

    internal BerStringResult SegmentBitString(
        byte[] content,
        System.Numerics.BigInteger tagNumber,
        int segmentSize)
    {
        int payloadLength = content.Length - 1;
        if (payloadLength > 0 && segmentSize < 2)
        {
            return this.CreateStringError(
                "ASNE0068",
                "A BIT STRING segment needs room for unused-bit metadata and data.",
                false);
        }

        int maximumPayload = segmentSize - 1;
        System.Collections.Generic.List<BerTlv> fragments =
            new System.Collections.Generic.List<BerTlv>();
        int offset = 0;
        while (offset < payloadLength)
        {
            int count = payloadLength - offset;
            if (count > maximumPayload)
            {
                count = maximumPayload;
            }

            bool last = offset + count == payloadLength;
            byte[] fragment = new byte[count + 1];
            fragment[0] = last ? content[0] : (byte)0;
            System.Buffer.BlockCopy(content, offset + 1, fragment, 1, count);
            fragments.Add(this.CreatePrimitiveFragment(tagNumber, fragment));
            offset = offset + count;
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            fragments.Count))
        {
            return this.CreateStringError(
                "ASNR0011",
                "The constructed BIT STRING exceeds the fragment-count limit.",
                true);
        }

        return new BerStringResult
        {
            Success = true,
            Tlv = this.CreateConstructedStringRoot(
                tagNumber,
                fragments.ToArray())
        };
    }

    internal BerStringResult SegmentOctetString(
        AsnTypeKind kind,
        byte[] content,
        System.Numerics.BigInteger tagNumber,
        int segmentSize)
    {
        System.Collections.Generic.List<BerTlv> fragments =
            new System.Collections.Generic.List<BerTlv>();
        int offset = 0;
        while (offset < content.Length)
        {
            int count = this.FindFragmentLength(
                kind,
                content,
                offset,
                segmentSize);
            if (count <= 0)
            {
                return this.CreateStringError(
                    "ASNE0069",
                    "The segment size cannot contain one complete encoded character.",
                    false);
            }

            byte[] fragment = new byte[count];
            System.Buffer.BlockCopy(content, offset, fragment, 0, count);
            fragments.Add(this.CreatePrimitiveFragment(tagNumber, fragment));
            offset = offset + count;
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            fragments.Count))
        {
            return this.CreateStringError(
                "ASNR0011",
                "The constructed string exceeds the fragment-count limit.",
                true);
        }

        return new BerStringResult
        {
            Success = true,
            Tlv = this.CreateConstructedStringRoot(
                tagNumber,
                fragments.ToArray())
        };
    }

    internal int FindFragmentLength(
        AsnTypeKind kind,
        byte[] content,
        int offset,
        int maximum)
    {
        int remaining = content.Length - offset;
        if (kind == AsnTypeKind.UniversalString
            || kind == AsnTypeKind.BmpString)
        {
            int unit = kind == AsnTypeKind.UniversalString ? 4 : 2;
            int count = remaining < maximum ? remaining : maximum;
            return count - count % unit;
        }

        if (kind != AsnTypeKind.Utf8String)
        {
            return remaining < maximum ? remaining : maximum;
        }

        Utf8ChunkDecoder decoder = new Utf8ChunkDecoder();
        int countBytes = 0;
        while (countBytes < remaining)
        {
            int length = decoder.GetSequenceLength(content[offset + countBytes]);
            if (length <= 0
                || length > remaining - countBytes
                || countBytes + length > maximum)
            {
                break;
            }

            countBytes = countBytes + length;
        }

        return countBytes;
    }

    internal bool IsCanonicalCerConstructedForm(
        AsnTypeKind kind,
        BerTlv tlv,
        int tagNumber)
    {
        if (!tlv.IndefiniteLength || tlv.Children.Length == 0)
        {
            return false;
        }

        long logicalLength = kind == AsnTypeKind.BitString ? 1 : 0;
        int index = 0;
        while (index < tlv.Children.Length)
        {
            BerTlv child = tlv.Children[index];
            if (child.Constructed
                || child.IndefiniteLength
                || child.Tag.Class != AsnTagClass.Universal
                || child.Tag.Number
                    != new System.Numerics.BigInteger(tagNumber))
            {
                return false;
            }

            int contentLength = child.Content.Length;
            bool final = index == tlv.Children.Length - 1;
            if (final)
            {
                if (contentLength == 0 || contentLength > 1000)
                {
                    return false;
                }
            }
            else if (!this.IsCanonicalCerNonFinalFragment(
                kind,
                child,
                tlv.Children[index + 1]))
            {
                return false;
            }

            if (kind == AsnTypeKind.BitString)
            {
                if (contentLength == 0
                    || !final && child.Content[0] != 0)
                {
                    return false;
                }

                logicalLength = logicalLength + contentLength - 1;
            }
            else
            {
                logicalLength = logicalLength + contentLength;
            }

            index = index + 1;
        }

        return logicalLength > 1000;
    }

    internal bool IsCanonicalCerNonFinalFragment(
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

    internal void AppendStringFragments(
        AsnTypeKind kind,
        BerTlv current,
        AsnEncodingRules rules,
        int tagNumber,
        BerStringDecodeState state,
        ResourceCounter resources)
    {
        if (state.Failed)
        {
            return;
        }

        if (current.Tag.Class != AsnTagClass.Universal
            || current.Tag.Number != new System.Numerics.BigInteger(tagNumber))
        {
            this.SetStateError(
                state,
                "ASND0083",
                "Every constructed string fragment must use the same universal tag.",
                false);
            return;
        }

        if (!current.Constructed)
        {
            int primitiveTag = new BerPrimitiveCodec(this.Limits)
                .GetUniversalTagNumber(kind);
            BerTlv primitiveFragment = new BerTlv
            {
                Tag = new AsnTag
                {
                    Class = AsnTagClass.Universal,
                    Number = new System.Numerics.BigInteger(primitiveTag)
                },
                Content = current.Content
            };
            BerPrimitiveDecodeResult validation = new BerPrimitiveCodec(
                this.Limits).DecodePrimitive(kind, primitiveFragment, rules);
            if (!validation.Success)
            {
                AsnDiagnostic diagnostic = validation.Diagnostics[0];
                this.SetStateError(
                    state,
                    diagnostic.Code,
                    diagnostic.Message,
                    diagnostic.Category == AsnDiagnosticCategory.Resource);
                return;
            }

            int appendOffset = 0;
            int appendCount = current.Content.Length;
            if (kind == AsnTypeKind.BitString)
            {
                if (state.SawFragment && state.LastUnusedBits != 0)
                {
                    this.SetStateError(
                        state,
                        "ASND0084",
                        "Only the final BIT STRING fragment may contain unused bits.",
                        false);
                    return;
                }

                state.LastUnusedBits = current.Content[0];
                appendOffset = 1;
                appendCount = appendCount - 1;
            }

            if (appendCount > int.MaxValue - state.Bytes.Count
                || !new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
                    state.Bytes.Count + appendCount))
            {
                this.SetStateError(
                    state,
                    "ASNR0001",
                    "The flattened string exceeds the configured byte limit.",
                    true);
                return;
            }

            state.Bytes.AppendSegment(
                current.Content,
                appendOffset,
                appendCount);
            state.SawFragment = true;
            return;
        }

        if (!resources.TryEnterNesting())
        {
            this.SetStateError(
                state,
                "ASNR0002",
                "The constructed string exceeds the configured nesting limit.",
                true);
            return;
        }

        if (!resources.AllowsCollectionElements(current.Children.Length))
        {
            resources.LeaveNesting();
            this.SetStateError(
                state,
                "ASNR0011",
                "The constructed string exceeds the fragment-count limit.",
                true);
            return;
        }

        int index = 0;
        while (index < current.Children.Length && !state.Failed)
        {
            this.AppendStringFragments(
                kind,
                current.Children[index],
                rules,
                tagNumber,
                state,
                resources);
            index = index + 1;
        }

        resources.LeaveNesting();
    }

    internal bool IsSupportedStringKind(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.BitString
            || kind == AsnTypeKind.OctetString
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

    internal BerTlv CreatePrimitiveFragment(
        System.Numerics.BigInteger tagNumber,
        byte[] content)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = tagNumber
            },
            Content = content
        };
    }

    internal BerTlv CreateConstructedStringRoot(
        System.Numerics.BigInteger tagNumber,
        BerTlv[] children)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = tagNumber
            },
            Constructed = true,
            Children = children
        };
    }

    internal BerStringResult CopyEncodeFailure(
        BerPrimitiveEncodeResult result)
    {
        if (result.Diagnostics.Length == 0)
        {
            return this.CreateStringError(
                "ASNE0070",
                "The primitive string value could not be encoded.",
                false);
        }

        AsnDiagnostic diagnostic = result.Diagnostics[0];
        return this.CreateStringError(
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Category == AsnDiagnosticCategory.Resource);
    }

    internal BerStringResult CopyDecodeResult(
        BerPrimitiveDecodeResult result)
    {
        if (result.Success && result.Value != null)
        {
            return new BerStringResult
            {
                Success = true,
                Value = result.Value
            };
        }

        if (result.Diagnostics.Length == 0)
        {
            return this.CreateStringError(
                "ASND0085",
                "The primitive string content could not be decoded.",
                false);
        }

        AsnDiagnostic diagnostic = result.Diagnostics[0];
        return this.CreateStringError(
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Category == AsnDiagnosticCategory.Resource);
    }

    internal void SetStateError(
        BerStringDecodeState state,
        string code,
        string message,
        bool resource)
    {
        state.Failed = true;
        state.LimitExceeded = resource;
        state.ErrorCode = code;
        state.ErrorMessage = message;
    }

    internal BerStringResult CreateStringError(
        string code,
        string message,
        bool resource)
    {
        return new BerStringResult
        {
            LimitExceeded = resource,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
