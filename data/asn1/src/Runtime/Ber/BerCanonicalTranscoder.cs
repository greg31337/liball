namespace asn1;

internal class BerCanonicalTranscoder
{
    internal AsnReflectionCatalog? Catalog;
    internal AsnResourceLimits Limits;

    internal BerCanonicalTranscoder(
        AsnReflectionCatalog? catalog,
        AsnResourceLimits limits)
    {
        this.Catalog = catalog;
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal AsnEncodeResult TranscodeSchema(
        AsnDecodeRequest request,
        AsnEncodingRules targetRules)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (this.Catalog == null)
        {
            return this.CreateError(
                "ASNT0001",
                "Schema-aware BER transcoding requires a reflection catalog.",
                false);
        }

        if (!this.IsCanonicalTarget(targetRules))
        {
            return this.CreateError(
                "ASNT0001",
                "BER transcoding requires DER or CER as the target rules.",
                false);
        }

        if (request.Data == null || request.TypeName == null)
        {
            throw new System.ArgumentException(
                "The schema transcode request contains null data.",
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

        AsnCodecOptions requested = request.Options == null
            ? new AsnCodecOptions()
            : request.Options;
        AsnResourceLimits limits = requested.ResourceLimits == null
            ? this.Limits
            : requested.ResourceLimits;
        AsnCodecOptions sourceOptions = new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.Ber,
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve,
            ValidateValues = requested.ValidateValues,
            ResourceLimits = limits,
            ObjectSetRegistry = requested.ObjectSetRegistry
        };
        AsnDecodeResult decoded = new BerSchemaDecoder(this.Catalog)
            .DecodeRequest(new AsnDecodeRequest
            {
                TypeName = request.TypeName,
                Data = request.Data,
                Offset = request.Offset,
                HasCount = true,
                Count = count,
                Options = sourceOptions
            });
        if (decoded.Status != AsnDecodeStatus.Done || decoded.Value == null)
        {
            return new AsnEncodeResult
            {
                Diagnostics = decoded.Diagnostics.Length == 0
                    ? [this.CreateDiagnostic(
                        "ASNT0003",
                        "The BER input could not be decoded with the supplied schema.",
                        false)]
                    : decoded.Diagnostics
            };
        }

        if (decoded.ConsumedCount != count)
        {
            return this.CreateError(
                "ASNT0003",
                "Schema-aware transcoding requires exactly one complete BER value.",
                false);
        }

        string unavailablePath = this.FindUnavailablePath(
            decoded.Value,
            request.TypeName);
        if (unavailablePath.Length > 0)
        {
            return this.CreateError(
                "ASNT0002",
                "Canonical transcoding requires unavailable type information at "
                    + unavailablePath
                    + ".",
                false);
        }

        AsnCodecOptions targetOptions = new AsnCodecOptions
        {
            EncodingRules = targetRules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
            ValidateValues = requested.ValidateValues,
            UseIndefiniteLengths = targetRules == AsnEncodingRules.Cer,
            ResourceLimits = limits,
            ObjectSetRegistry = requested.ObjectSetRegistry
        };
        return new BerSchemaEncoder(this.Catalog).EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = request.TypeName,
                Value = decoded.Value,
                Options = targetOptions
            });
    }

    internal AsnEncodeResult TranscodeTlv(
        byte[] data,
        int offset,
        int count,
        AsnEncodingRules targetRules)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (!this.IsCanonicalTarget(targetRules))
        {
            return this.CreateError(
                "ASNT0001",
                "BER transcoding requires DER or CER as the target rules.",
                false);
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
            return new AsnEncodeResult
            {
                Diagnostics = read.Diagnostics.Length == 0
                    ? [this.CreateDiagnostic(
                        "ASNT0003",
                        "The input does not contain one complete BER value.",
                        false)]
                    : read.Diagnostics
            };
        }

        if (read.ConsumedCount != count)
        {
            return this.CreateError(
                "ASNT0003",
                "Schema-less transcoding requires exactly one complete BER value.",
                false);
        }

        BerSchemaEncodeResult transcoded = this.TranscodeNode(
            read.Tlv,
            targetRules);
        if (!transcoded.Success || transcoded.Tlv == null)
        {
            return new AsnEncodeResult
            {
                Diagnostics = transcoded.Diagnostics
            };
        }

        BerWriteResult written = new BerTlvWriterImplementation(
            this.Limits).Write(new BerWriteRequest
            {
                Tlv = transcoded.Tlv,
                EncodingRules = targetRules
            });
        if (!written.Success)
        {
            return new AsnEncodeResult
            {
                Diagnostics = written.Diagnostics
            };
        }

        return new AsnEncodeResult
        {
            Success = true,
            Data = written.Data,
            WrittenCount = written.WrittenCount
        };
    }

    internal BerSchemaEncodeResult TranscodeNode(
        BerTlv source,
        AsnEncodingRules targetRules)
    {
        if (source.Tag.Class != AsnTagClass.Universal)
        {
            return this.CreateNodeError(
                "ASNT0002",
                "Schema-less canonical transcoding cannot determine the type of tag "
                    + this.FormatTag(source.Tag)
                    + " at byte offset "
                    + source.EncodedOffset
                    + ".");
        }

        if (source.Tag.Number > new System.Numerics.BigInteger(int.MaxValue))
        {
            return this.CreateNodeError(
                "ASNT0002",
                "Schema-less canonical transcoding cannot determine the type of universal tag "
                    + source.Tag.Number
                    + ".");
        }

        int tag = (int)source.Tag.Number;
        if (tag == AsnUniversalTags.Sequence)
        {
            return this.TranscodeCollection(source, targetRules, false);
        }

        if (tag == AsnUniversalTags.Set)
        {
            return this.TranscodeCollection(source, targetRules, true);
        }

        BerCanonicalValidator typeLookup = new BerCanonicalValidator(
            this.Limits);
        AsnTypeKind stringKind = typeLookup.GetStringKind(source);
        if (stringKind != AsnTypeKind.Unknown)
        {
            return this.TranscodeString(
                source,
                stringKind,
                targetRules);
        }

        AsnTypeKind primitiveKind = typeLookup.GetPrimitiveKind(tag);
        if (primitiveKind != AsnTypeKind.Unknown)
        {
            return this.TranscodePrimitive(
                source,
                primitiveKind,
                targetRules);
        }

        AsnTypeKind usefulKind = this.GetUsefulKind(tag);
        if (usefulKind != AsnTypeKind.Unknown)
        {
            return this.TranscodeUseful(
                source,
                usefulKind,
                targetRules);
        }

        return this.CreateNodeError(
            "ASNT0002",
            "Schema-less canonical transcoding does not know the semantics of universal tag "
                + tag
                + ".");
    }

    internal BerSchemaEncodeResult TranscodeCollection(
        BerTlv source,
        AsnEncodingRules targetRules,
        bool set)
    {
        if (!source.Constructed)
        {
            return this.CreateNodeError(
                "ASNT0003",
                "A universal collection must use constructed encoding.");
        }

        BerTlv[] children = new BerTlv[source.Children.Length];
        int index = 0;
        while (index < source.Children.Length)
        {
            BerSchemaEncodeResult child = this.TranscodeNode(
                source.Children[index],
                targetRules);
            if (!child.Success || child.Tlv == null)
            {
                return child;
            }

            children[index] = child.Tlv;
            index = index + 1;
        }

        BerConstructedResult encoded = set
            ? new BerConstructedCodec(this.Limits).EncodeSetOfValue(
                children,
                targetRules,
                false)
            : new BerConstructedCodec(this.Limits).EncodeSequenceOfValue(
                children,
                targetRules,
                false);
        if (!encoded.Success || encoded.Tlv == null)
        {
            return this.CopyConstructedFailure(encoded);
        }

        return new BerSchemaEncodeResult
        {
            Success = true,
            Tlv = encoded.Tlv
        };
    }

    internal BerSchemaEncodeResult TranscodePrimitive(
        BerTlv source,
        AsnTypeKind kind,
        AsnEncodingRules targetRules)
    {
        BerPrimitiveDecodeResult decoded = new BerPrimitiveCodec(this.Limits)
            .DecodePrimitive(kind, source, AsnEncodingRules.Ber);
        if (!decoded.Success || decoded.Value == null)
        {
            return new BerSchemaEncodeResult
            {
                Diagnostics = decoded.Diagnostics
            };
        }

        BerPrimitiveEncodeResult encoded = new BerPrimitiveCodec(this.Limits)
            .EncodePrimitive(kind, decoded.Value, targetRules);
        return this.CopyPrimitiveResult(encoded);
    }

    internal BerSchemaEncodeResult TranscodeString(
        BerTlv source,
        AsnTypeKind kind,
        AsnEncodingRules targetRules)
    {
        if (source.Tag.Number
            == new System.Numerics.BigInteger(
                AsnUniversalTags.ObjectDescriptor))
        {
            return this.TranscodeUseful(
                source,
                AsnTypeKind.ObjectDescriptor,
                targetRules);
        }

        BerStringResult decoded = new BerStringCodec(this.Limits)
            .DecodeConstructedString(kind, source, AsnEncodingRules.Ber);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CreateNodeError(
                decoded.ErrorCode,
                decoded.ErrorMessage);
        }

        BerPrimitiveEncodeResult encoded = new BerPrimitiveCodec(this.Limits)
            .EncodePrimitive(kind, decoded.Value, targetRules);
        return this.CopyPrimitiveResult(encoded);
    }

    internal BerSchemaEncodeResult TranscodeUseful(
        BerTlv source,
        AsnTypeKind kind,
        AsnEncodingRules targetRules)
    {
        BerPrimitiveDecodeResult decoded = new BerUsefulTypeCodec(this.Limits)
            .DecodeUsefulType(kind, source, AsnEncodingRules.Ber);
        if (!decoded.Success || decoded.Value == null)
        {
            return new BerSchemaEncodeResult
            {
                Diagnostics = decoded.Diagnostics
            };
        }

        string unavailablePath = this.FindUnavailablePath(decoded.Value, kind.ToString());
        if (unavailablePath.Length > 0)
        {
            return this.CreateNodeError(
                "ASNT0002",
                "Canonical transcoding requires unavailable type information at "
                    + unavailablePath
                    + ".");
        }

        BerPrimitiveEncodeResult encoded = new BerUsefulTypeCodec(this.Limits)
            .EncodeUsefulType(kind, decoded.Value, targetRules);
        return this.CopyPrimitiveResult(encoded);
    }

    internal string FindUnavailablePath(
        AsnRuntimeValue value,
        string path)
    {
        if (value.UnknownExtensions.Length > 0
            || value.UnknownFields.Length > 0)
        {
            return path;
        }

        if (value.OpenTypeValue != null)
        {
            if (value.OpenTypeValue.Unresolved)
            {
                return path;
            }

            if (value.OpenTypeValue.Value != null)
            {
                string openPath = this.FindUnavailablePath(
                    value.OpenTypeValue.Value,
                    path + ".open");
                if (openPath.Length > 0)
                {
                    return openPath;
                }
            }
        }

        int fieldIndex = 0;
        while (fieldIndex < value.Fields.Length)
        {
            AsnRuntimeFieldValue field = value.Fields[fieldIndex];
            if (field.Present && field.Value != null)
            {
                string fieldPath = this.FindUnavailablePath(
                    field.Value,
                    path + "." + field.Name);
                if (fieldPath.Length > 0)
                {
                    return fieldPath;
                }
            }

            fieldIndex = fieldIndex + 1;
        }

        int itemIndex = 0;
        while (itemIndex < value.Items.Length)
        {
            string itemPath = this.FindUnavailablePath(
                value.Items[itemIndex],
                path + "[" + itemIndex + "]");
            if (itemPath.Length > 0)
            {
                return itemPath;
            }

            itemIndex = itemIndex + 1;
        }

        return "";
    }

    internal AsnTypeKind GetUsefulKind(int tag)
    {
        switch (tag)
        {
            case AsnUniversalTags.External:
                return AsnTypeKind.External;
            case AsnUniversalTags.EmbeddedPdv:
                return AsnTypeKind.EmbeddedPdv;
            case AsnUniversalTags.CharacterString:
                return AsnTypeKind.CharacterString;
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

    internal bool IsCanonicalTarget(AsnEncodingRules rules)
    {
        return rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer;
    }

    internal BerSchemaEncodeResult CopyPrimitiveResult(
        BerPrimitiveEncodeResult result)
    {
        return new BerSchemaEncodeResult
        {
            Success = result.Success,
            Tlv = result.Tlv,
            Diagnostics = result.Diagnostics
        };
    }

    internal BerSchemaEncodeResult CopyConstructedFailure(
        BerConstructedResult result)
    {
        return new BerSchemaEncodeResult
        {
            Diagnostics = [this.CreateDiagnostic(
                result.ErrorCode,
                result.ErrorMessage,
                result.LimitExceeded)]
        };
    }

    internal BerSchemaEncodeResult CreateNodeError(
        string code,
        string message)
    {
        return new BerSchemaEncodeResult
        {
            Diagnostics = [this.CreateDiagnostic(code, message, false)]
        };
    }

    internal AsnEncodeResult CreateError(
        string code,
        string message,
        bool resource)
    {
        return new AsnEncodeResult
        {
            Diagnostics = [this.CreateDiagnostic(code, message, resource)]
        };
    }

    internal AsnDiagnostic CreateDiagnostic(
        string code,
        string message,
        bool resource)
    {
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = resource
                ? AsnDiagnosticCategory.Resource
                : AsnDiagnosticCategory.Encoding,
            Code = code,
            Message = message
        };
    }

    internal string FormatTag(AsnTag tag)
    {
        return "[" + tag.Class + " " + tag.Number + "]";
    }
}
