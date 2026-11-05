namespace asn1;

internal class OerSchemaCodec
{
    internal AsnReflectionCatalog Catalog;
    internal AsnEncodingRules Rules;
    internal bool Canonical;

    internal OerSchemaCodec(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules)
    {
        this.Catalog = catalog == null
            ? throw new System.ArgumentNullException("catalog")
            : catalog;
        if (rules != AsnEncodingRules.Oer
            && rules != AsnEncodingRules.Coer)
        {
            throw new System.ArgumentOutOfRangeException("rules");
        }

        this.Rules = rules;
        this.Canonical = rules == AsnEncodingRules.Coer;
    }

    internal AsnEncodeResult EncodeRequest(AsnEncodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.TypeName == null)
        {
            throw new System.ArgumentException(
                "The BASIC-OER encode request contains a null type name.",
                "request");
        }

        if (request.Value == null)
        {
            return this.CreatePublicEncodeError(
                "ASNO0100",
                "A BASIC-OER encode request requires a runtime value.",
                false);
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = this.Rules }
            : request.Options;
        if (options.EncodingRules != this.Rules)
        {
            return this.CreatePublicEncodeError(
                "ASNO0101",
                "The requested OER rules do not match this codec.",
                false);
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreatePublicEncodeError(
                "ASNO0102",
                "The requested schema type is missing or ambiguous.",
                false);
        }

        if (options.ValidateValues)
        {
            AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
                .ValidateRuntimeValue(
                    request.Value,
                    type,
                    options.ResourceLimits == null
                        ? new AsnResourceLimits()
                        : options.ResourceLimits);
            if (diagnostics.Length > 0)
            {
                return new AsnEncodeResult { Diagnostics = diagnostics };
            }
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        OerCollectionResult encoded = new OerValueCodec(limits)
            .EncodeValueAtDepth(
                type,
                request.Value,
                this.Canonical,
                0,
                options.UnknownDataPolicy);
        if (!encoded.Success)
        {
            return this.CreatePublicEncodeError(
                encoded.ErrorCode,
                encoded.ErrorMessage,
                encoded.LimitExceeded);
        }

        return new AsnEncodeResult
        {
            Success = true,
            Data = encoded.Data,
            WrittenCount = encoded.Data.Length
        };
    }

    internal AsnDecodeResult DecodeRequest(AsnDecodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Data == null || request.TypeName == null)
        {
            throw new System.ArgumentException(
                "The BASIC-OER decode request contains null input.",
                "request");
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = this.Rules }
            : request.Options;
        if (options.EncodingRules != this.Rules)
        {
            return this.CreatePublicDecodeError(
                "ASNO0101",
                "The requested OER rules do not match this codec.",
                false,
                0);
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreatePublicDecodeError(
                "ASNO0102",
                "The requested schema type is missing or ambiguous.",
                false,
                0);
        }

        int count = request.HasCount
            ? request.Count
            : request.Data.Length - request.Offset;
        if (!CursorMath.IsRangeValid(request.Offset, count, request.Data.Length))
        {
            throw new System.ArgumentOutOfRangeException("request");
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        OerCollectionResult decoded = new OerValueCodec(limits)
            .DecodeValueAtDepth(
                type,
                request.Data,
                request.Offset,
                count,
                this.Canonical,
                0,
                options.UnknownDataPolicy);
        if (!decoded.Success)
        {
            return this.CopyPublicDecodeFailure(decoded);
        }

        if (decoded.ConsumedCount != count)
        {
            return this.CreatePublicDecodeErrorAt(
                "ASNO0103",
                "The BASIC-OER input contains trailing data.",
                false,
                decoded.ConsumedCount,
                decoded.ConsumedCount);
        }

        if (decoded.Value != null && options.ValidateValues)
        {
            AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
                .ValidateRuntimeValue(
                    decoded.Value,
                    type,
                    options.ResourceLimits == null
                        ? new AsnResourceLimits()
                        : options.ResourceLimits);
            if (diagnostics.Length > 0)
            {
                return new AsnDecodeResult
                {
                    Status = AsnDecodeStatus.Error,
                    ConsumedCount = decoded.ConsumedCount,
                    Diagnostics = diagnostics
                };
            }
        }

        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Done,
            Value = decoded.Value,
            ConsumedCount = decoded.ConsumedCount
        };
    }

    internal AsnEncodeResult EncodeStream(
        AsnEncodeRequest request,
        System.IO.Stream output)
    {
        if (output == null)
        {
            throw new System.ArgumentNullException("output");
        }

        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = this.Rules }
            : request.Options;
        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        bool streamOctets = options.EncodingRules == this.Rules
            && type != null
            && type.Kind == AsnTypeKind.OctetString
            && request.Value != null
            && request.Value.Kind == AsnRuntimeValueKind.OctetString;
        if (!streamOctets || type == null || request.Value == null)
        {
            return this.WriteBufferedEncoding(
                this.EncodeRequest(request),
                output);
        }

        if (options.ValidateValues)
        {
            AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
                .ValidateRuntimeValue(
                    request.Value,
                    type,
                    options.ResourceLimits == null
                        ? new AsnResourceLimits()
                        : options.ResourceLimits);
            if (diagnostics.Length > 0)
            {
                return this.CreateStreamingFailure(
                    new AsnEncodeResult { Diagnostics = diagnostics });
            }
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        OerSizeResult size = new OerPrimitiveCodec(limits).GetFixedSize(type);
        byte[] prefix = [];
        if (size.Fixed)
        {
            if (request.Value.Octets.Length != size.Size)
            {
                return this.WriteBufferedEncoding(
                    this.EncodeRequest(request),
                    output);
            }
        }
        else
        {
            OerLengthResult length = new OerLengthCodec(limits).WriteLength(
                request.Value.Octets.Length);
            if (!length.Success)
            {
                return this.CreateStreamingFailure(
                    this.CreatePublicEncodeError(
                        length.ErrorCode,
                        length.ErrorMessage,
                        length.LimitExceeded));
            }

            prefix = length.Data;
        }

        this.WriteStreamBytes(output, prefix);
        this.WriteStreamBytes(output, request.Value.Octets);
        int writtenCount = prefix.Length + request.Value.Octets.Length;
        return new AsnEncodeResult
        {
            Success = true,
            WrittenCount = writtenCount,
            StreamProgress = new AsnStreamProgress
            {
                WrittenCount = writtenCount,
                HasExpectedCount = true,
                ExpectedCount = writtenCount,
                Complete = true
            }
        };
    }

    internal AsnEncodeResult WriteBufferedEncoding(
        AsnEncodeResult encoded,
        System.IO.Stream output)
    {
        if (!encoded.Success)
        {
            return this.CreateStreamingFailure(encoded);
        }

        this.WriteStreamBytes(output, encoded.Data);
        int writtenCount = encoded.WrittenCount;
        return new AsnEncodeResult
        {
            Success = true,
            WrittenCount = writtenCount,
            StreamProgress = new AsnStreamProgress
            {
                WrittenCount = writtenCount,
                HasExpectedCount = true,
                ExpectedCount = writtenCount,
                Complete = true
            }
        };
    }

    internal AsnEncodeResult CreateStreamingFailure(AsnEncodeResult source)
    {
        return new AsnEncodeResult
        {
            Diagnostics = source.Diagnostics,
            StreamProgress = new AsnStreamProgress { Complete = false }
        };
    }

    internal void WriteStreamBytes(
        System.IO.Stream output,
        byte[] data)
    {
        int index = 0;
        while (index < data.Length)
        {
            output.Write(data, index, 1);
            index = index + 1;
        }
    }

    internal AsnDecodeResult CopyPublicDecodeFailure(
        OerCollectionResult source)
    {
        AsnDiagnostic[] diagnostics = source.ErrorCode.Length == 0
            ? []
            : [this.CreateDiagnostic(
                source.ErrorCode,
                source.ErrorMessage,
                source.LimitExceeded,
                false)];
        AsnDecodeResult result = new AsnDecodeResult
        {
            Status = source.Status,
            ConsumedCount = source.ConsumedCount,
            Diagnostics = diagnostics
        };
        if (diagnostics.Length > 0)
        {
            this.SetDiagnosticByteOffset(
                diagnostics[0],
                source.HasViolationOffset
                    ? source.ViolationOffset
                    : source.ConsumedCount);
        }

        return result;
    }

    internal AsnEncodeResult CreatePublicEncodeError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new AsnEncodeResult
        {
            Diagnostics = [this.CreateDiagnostic(
                code,
                message,
                limitExceeded,
                true)]
        };
    }

    internal AsnDecodeResult CreatePublicDecodeError(
        string code,
        string message,
        bool limitExceeded,
        int consumedCount)
    {
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            Diagnostics = [this.CreateDiagnostic(
                code,
                message,
                limitExceeded,
                false)]
        };
    }

    internal AsnDecodeResult CreatePublicDecodeErrorAt(
        string code,
        string message,
        bool limitExceeded,
        int consumedCount,
        int violationOffset)
    {
        AsnDecodeResult result = this.CreatePublicDecodeError(
            code,
            message,
            limitExceeded,
            consumedCount);
        this.SetDiagnosticByteOffset(
            result.Diagnostics[0],
            violationOffset);
        return result;
    }

    internal void SetDiagnosticByteOffset(
        AsnDiagnostic diagnostic,
        int byteOffset)
    {
        diagnostic.HasBitOffset = true;
        diagnostic.BitOffset = byteOffset > int.MaxValue / 8
            ? int.MaxValue
            : byteOffset * 8;
        diagnostic.Message = diagnostic.Message + " Byte offset: "
            + byteOffset.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + ".";
    }

    internal AsnDiagnostic CreateDiagnostic(
        string code,
        string message,
        bool limitExceeded,
        bool encoding)
    {
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = limitExceeded
                ? AsnDiagnosticCategory.Resource
                : encoding
                    ? AsnDiagnosticCategory.Encoding
                    : AsnDiagnosticCategory.Decoding,
            Code = code,
            Message = message
        };
    }
}
