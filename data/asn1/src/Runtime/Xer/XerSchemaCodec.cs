namespace asn1;

internal class XerSchemaCodec
{
    internal AsnReflectionCatalog Catalog;
    internal AsnEncodingRules Rules;
    internal bool Canonical;
    internal bool Extended;

    internal XerSchemaCodec(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules)
    {
        this.Catalog = catalog == null
            ? throw new System.ArgumentNullException("catalog")
            : catalog;
        if (rules != AsnEncodingRules.BasicXer
            && rules != AsnEncodingRules.CanonicalXer
            && rules != AsnEncodingRules.ExtendedXer)
        {
            throw new System.ArgumentOutOfRangeException("rules");
        }

        this.Rules = rules;
        this.Canonical = rules == AsnEncodingRules.CanonicalXer;
        this.Extended = rules == AsnEncodingRules.ExtendedXer;
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
                "The XER encode request contains a null type name.",
                "request");
        }

        if (request.Value == null)
        {
            return this.CreatePublicEncodeError(
                "ASNXER0400",
                "An XER encode request requires a runtime value.",
                false);
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = this.Rules }
            : request.Options;
        if (options.EncodingRules != this.Rules)
        {
            return this.CreatePublicEncodeError(
                "ASNXER0401",
                "The requested XER rules do not match this codec.",
                false);
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreatePublicEncodeError(
                "ASNXER0402",
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
        XerValueCodec values = new XerValueCodec(limits);
        values.Canonical = this.Canonical;
        values.Extended = this.Extended;
        values.UnknownDataPolicy = options.UnknownDataPolicy;
        XerValueResult encoded = values.EncodeValue(type, request.Value);
        if (!encoded.Success)
        {
            return this.CopyPublicEncodeFailure(encoded);
        }

        System.IO.MemoryStream output = new System.IO.MemoryStream();
        XerXmlTokenWriter writer = new XerXmlTokenWriter(output, limits);
        int tokenIndex = 0;
        while (tokenIndex < encoded.Tokens.Length)
        {
            XerXmlWriteResult written = writer.WriteToken(
                encoded.Tokens[tokenIndex]);
            if (!written.Success)
            {
                return this.CopyPublicWriteFailure(written);
            }

            tokenIndex = tokenIndex + 1;
        }

        XerXmlWriteResult complete = writer.CompleteWriter();
        if (!complete.Success)
        {
            return this.CopyPublicWriteFailure(complete);
        }

        byte[] data = output.ToArray();
        return new AsnEncodeResult
        {
            Success = true,
            Data = data,
            WrittenCount = data.Length
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

        if (request.TypeName == null)
        {
            throw new System.ArgumentException(
                "The XER encode request contains a null type name.",
                "request");
        }

        if (request.Value == null)
        {
            return this.CreateStreamingFailure(
                this.CreatePublicEncodeError(
                    "ASNXER0400",
                    "An XER encode request requires a runtime value.",
                    false),
                0);
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = this.Rules }
            : request.Options;
        if (options.EncodingRules != this.Rules)
        {
            return this.CreateStreamingFailure(
                this.CreatePublicEncodeError(
                    "ASNXER0401",
                    "The requested XER rules do not match this codec.",
                    false),
                0);
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreateStreamingFailure(
                this.CreatePublicEncodeError(
                    "ASNXER0402",
                    "The requested schema type is missing or ambiguous.",
                    false),
                0);
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
                    new AsnEncodeResult { Diagnostics = diagnostics },
                    0);
            }
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        XerValueCodec values = new XerValueCodec(limits);
        values.Canonical = this.Canonical;
        values.Extended = this.Extended;
        values.UnknownDataPolicy = options.UnknownDataPolicy;
        XerValueResult encoded = values.EncodeValue(type, request.Value);
        if (!encoded.Success)
        {
            return this.CreateStreamingFailure(
                this.CopyPublicEncodeFailure(encoded),
                0);
        }

        XerXmlTokenWriter writer = new XerXmlTokenWriter(output, limits);
        int tokenIndex = 0;
        while (tokenIndex < encoded.Tokens.Length)
        {
            XerXmlWriteResult written = writer.WriteToken(
                encoded.Tokens[tokenIndex]);
            if (!written.Success)
            {
                return this.CreateStreamingFailure(
                    this.CopyPublicWriteFailure(written),
                    writer.WrittenCount);
            }

            tokenIndex = tokenIndex + 1;
        }

        XerXmlWriteResult complete = writer.CompleteWriter();
        if (!complete.Success)
        {
            return this.CreateStreamingFailure(
                this.CopyPublicWriteFailure(complete),
                writer.WrittenCount);
        }

        return new AsnEncodeResult
        {
            Success = true,
            WrittenCount = writer.WrittenCount,
            StreamProgress = new AsnStreamProgress
            {
                WrittenCount = writer.WrittenCount,
                HasExpectedCount = true,
                ExpectedCount = writer.WrittenCount,
                Complete = true
            }
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
                "The XER decode request contains null input.",
                "request");
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = this.Rules }
            : request.Options;
        if (options.EncodingRules != this.Rules)
        {
            return this.CreatePublicDecodeError(
                "ASNXER0401",
                "The requested XER rules do not match this codec.",
                false,
                0);
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreatePublicDecodeError(
                "ASNXER0402",
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
        XerValueResult decoded;
        if (this.Canonical)
        {
            decoded = new XerCanonicalCodec(limits).DecodeCanonical(
                type,
                request.Data,
                request.Offset,
                count);
        }
        else
        {
            decoded = this.DecodeNoncanonical(
                type,
                request.Data,
                request.Offset,
                count,
                limits,
                options.UnknownDataPolicy);
        }

        if (!decoded.Success || decoded.Value == null)
        {
            return this.CopyPublicDecodeFailure(decoded);
        }

        if (options.ValidateValues)
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

    internal XerValueResult DecodeNoncanonical(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        AsnResourceLimits limits,
        AsnUnknownDataPolicy unknownDataPolicy)
    {
        byte[] input = new byte[count];
        System.Buffer.BlockCopy(data, offset, input, 0, count);
        XerXmlReadResult read = new XerXmlTokenReader(limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = input,
                IsFinalBlock = true
            });
        if (read.Status != AsnDecodeStatus.Done)
        {
            return new XerValueResult
            {
                Status = read.Status,
                ConsumedCount = read.ConsumedCount,
                LimitExceeded = read.LimitExceeded,
                HasViolationOffset = true,
                ViolationOffset = read.ViolationOffset,
                ValuePath = "$",
                ErrorCode = read.ErrorCode,
                ErrorMessage = read.ErrorMessage
            };
        }

        XerValueCodec values = new XerValueCodec(limits);
        values.Extended = this.Extended;
        values.UnknownDataPolicy = unknownDataPolicy;
        XerValueResult decoded = values.DecodeValue(
            type,
            read.Tokens,
            0,
            read.Tokens.Length);
        if (!decoded.Success)
        {
            return decoded;
        }

        if (decoded.ConsumedCount != read.Tokens.Length)
        {
            return new XerValueResult
            {
                Status = AsnDecodeStatus.Error,
                ConsumedCount = decoded.ConsumedCount,
                ErrorCode = "ASNXER0403",
                ErrorMessage = "The XER input contains trailing content."
            };
        }

        decoded.ConsumedCount = count;
        return decoded;
    }

    internal AsnEncodeResult CopyPublicEncodeFailure(XerValueResult source)
    {
        AsnEncodeResult result = this.CreatePublicEncodeError(
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
        this.SetDiagnosticContext(
            result.Diagnostics,
            source.ValuePath,
            false,
            0);
        return result;
    }

    internal AsnEncodeResult CreateStreamingFailure(
        AsnEncodeResult source,
        int writtenCount)
    {
        return new AsnEncodeResult
        {
            WrittenCount = writtenCount,
            Diagnostics = source.Diagnostics,
            StreamProgress = new AsnStreamProgress
            {
                WrittenCount = writtenCount,
                Complete = false
            }
        };
    }

    internal AsnEncodeResult CopyPublicWriteFailure(XerXmlWriteResult source)
    {
        return this.CreatePublicEncodeError(
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal AsnDecodeResult CopyPublicDecodeFailure(XerValueResult source)
    {
        AsnDecodeResult result = this.CreatePublicDecodeError(
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded,
            source.ConsumedCount);
        if (source.ErrorCode.Length == 0)
        {
            result.Diagnostics = [];
            result.Status = source.Status;
        }

        this.SetDiagnosticContext(
            result.Diagnostics,
            source.ValuePath,
            source.HasViolationOffset,
            source.ViolationOffset);

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

    internal void SetDiagnosticContext(
        AsnDiagnostic[] diagnostics,
        string valuePath,
        bool hasViolationOffset,
        int violationOffset)
    {
        if (diagnostics.Length == 0)
        {
            return;
        }

        diagnostics[0].ValuePath = valuePath;
        if (hasViolationOffset)
        {
            diagnostics[0].HasBitOffset = true;
            diagnostics[0].BitOffset = violationOffset > int.MaxValue / 8
                ? int.MaxValue
                : violationOffset * 8;
            diagnostics[0].Message = diagnostics[0].Message
                + " Byte offset: "
                + violationOffset.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                + ".";
        }
    }
}
