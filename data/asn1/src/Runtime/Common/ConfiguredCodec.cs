namespace asn1;

internal class ConfiguredCodec : IAsnCodec
{
    internal AsnReflectionCatalog Catalog;
    internal AsnCodecOptions Options;
    internal AsnEncodingRules Rules;

    internal ConfiguredCodec(
        AsnReflectionCatalog catalog,
        AsnCodecOptions options)
    {
        this.Catalog = catalog == null
            ? throw new System.ArgumentNullException("catalog")
            : catalog;
        this.Options = options == null
            ? throw new System.ArgumentNullException("options")
            : options;
        this.Rules = options.EncodingRules;
    }

    public AsnEncodeResult Encode(AsnEncodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        AsnEncodeRequest effective = new AsnEncodeRequest
        {
            TypeName = request.TypeName,
            Value = request.Value,
            Options = this.CreateEffectiveOptions(request.Options)
        };
        AsnEncodeResult result;
        if (this.IsBerRule())
        {
            result = new BerSchemaEncoder(this.Catalog)
                .EncodeRequest(effective);
        }
        else if (this.IsPerRule())
        {
            result = new PerSchemaCodec(this.Catalog, this.Rules)
                .EncodeRequest(effective);
        }
        else if (this.IsOerRule())
        {
            result = new OerSchemaCodec(this.Catalog, this.Rules)
                .EncodeRequest(effective);
        }
        else if (this.IsXerRule())
        {
            result = new XerSchemaCodec(this.Catalog, this.Rules)
                .EncodeRequest(effective);
        }
        else
        {
            result = this.EncodeEcn(effective);
        }

        return this.CompleteEncodeResult(result);
    }

    public AsnDecodeResult Decode(AsnDecodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        AsnDecodeRequest effective = new AsnDecodeRequest
        {
            TypeName = request.TypeName,
            Data = request.Data,
            Offset = request.Offset,
            HasCount = request.HasCount,
            Count = request.Count,
            Options = this.CreateEffectiveOptions(request.Options)
        };
        AsnDecodeResult result;
        if (this.IsBerRule())
        {
            result = new BerSchemaDecoder(this.Catalog)
                .DecodeRequest(effective);
        }
        else if (this.IsPerRule())
        {
            result = new PerSchemaCodec(this.Catalog, this.Rules)
                .DecodeRequest(effective);
        }
        else if (this.IsOerRule())
        {
            result = new OerSchemaCodec(this.Catalog, this.Rules)
                .DecodeRequest(effective);
        }
        else if (this.IsXerRule())
        {
            result = new XerSchemaCodec(this.Catalog, this.Rules)
                .DecodeRequest(effective);
        }
        else
        {
            result = this.DecodeEcn(effective);
        }

        return result;
    }

    public AsnDecodeResult TryDecodeSpan(
        System.ReadOnlySpan<byte> input,
        string typeName,
        AsnCodecOptions options)
    {
        if (typeName == null)
        {
            throw new System.ArgumentNullException("typeName");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        AsnCodecOptions effective = this.CreateEffectiveOptions(options);
        AsnResourceLimits limits = effective.ResourceLimits == null
            ? new AsnResourceLimits()
            : effective.ResourceLimits;
        if (!new ResourceCounter(limits).TryChargeAllocationBytes(
            input.Length))
        {
            return this.CreateResourceDecodeFailure(
                "The input span exceeds the configured allocation limit.");
        }

        byte[] data = input.ToArray();
        AsnDecodeResult decoded = this.Decode(new AsnDecodeRequest
        {
            TypeName = typeName,
            Data = data,
            Options = effective
        });
        if (this.IsXerRule()
            && decoded.Status == AsnDecodeStatus.Error)
        {
            AsnIncrementalResult incremental =
                new XerIncrementalSchemaDecoder(
                    this.Catalog,
                    typeName,
                    effective).Consume(new AsnIncrementalInput
                    {
                        Data = data,
                        IsFinalBlock = false
                    });
            if (incremental.Status == AsnDecodeStatus.NeedMoreData)
            {
                return this.ConvertIncrementalDecode(incremental);
            }
        }
        if (decoded.Status == AsnDecodeStatus.Error
            && decoded.Diagnostics.Length > 0
            && this.IsIncompleteDiagnostic(decoded.Diagnostics[0].Code))
        {
            return new AsnDecodeResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = input.Length
            };
        }
        return decoded;
    }

    public AsnEncodeResult EncodeToSpan(
        AsnEncodeRequest request,
        System.Span<byte> destination)
    {
        AsnEncodeResult encoded = this.Encode(request);
        if (!encoded.Success)
        {
            return encoded;
        }
        if (destination.Length < encoded.WrittenCount)
        {
            AsnEncodeResult failure = this.CreateEncodeFailure(
                "ASNCODEC0004",
                "The destination span is too small for the encoded value.");
            failure.StreamProgress = new AsnStreamProgress
            {
                HasExpectedCount = true,
                ExpectedCount = encoded.WrittenCount,
                Complete = false
            };
            return failure;
        }

        new System.ReadOnlySpan<byte>(
            encoded.Data,
            0,
            encoded.WrittenCount).CopyTo(destination);
        encoded.StreamProgress = new AsnStreamProgress
        {
            WrittenCount = encoded.WrittenCount,
            HasExpectedCount = true,
            ExpectedCount = encoded.WrittenCount,
            Complete = true
        };
        return encoded;
    }

    public AsnCanonicalityResult CheckCanonicality(AsnDecodeRequest request)
    {
        return new CanonicalityReportImplementation(this)
            .CheckCanonicality(request);
    }

    public AsnTranscodeResult Transcode(AsnTranscodeRequest request)
    {
        return new TranscodeImplementation(this).Transcode(request);
    }

    public IAsnStreamingCodec CreateStreamingCodec()
    {
        return new StreamingCodecImplementation(this);
    }

    public IAsnIncrementalDecoder CreateIncrementalDecoder(
        string typeName,
        AsnCodecOptions options)
    {
        return new IncrementalDecoderImplementation(
            this,
            typeName,
            options);
    }

    internal AsnEncodeResult EncodeEcn(AsnEncodeRequest request)
    {
        if (request.Value == null)
        {
            return this.CreateEncodeFailure(
                "ASNCODEC0001",
                "An ECN encode request requires a runtime value.");
        }

        EcnCodecPlanSelection? selection = this.FindEcnSelection(
            request.TypeName);
        if (selection == null)
        {
            return this.CreateEncodeFailure(
                "ASNCODEC0002",
                "The requested schema type has no executable ECN plan.");
        }

        return new EcnIntegratedPlanCodec(
            selection.Plan,
            selection.Category,
            selection.Properties,
            selection.Type,
            request.Options ?? this.Options).EncodeIntegrated(
                request.Value,
                []);
    }

    internal AsnDecodeResult DecodeEcn(AsnDecodeRequest request)
    {
        if (request.Data == null)
        {
            throw new System.ArgumentException(
                "The ECN decode request contains null data.",
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

        EcnCodecPlanSelection? selection = this.FindEcnSelection(
            request.TypeName);
        if (selection == null)
        {
            return this.CreateDecodeFailure(
                "ASNCODEC0002",
                "The requested schema type has no executable ECN plan.");
        }

        byte[] data;
        if (request.Offset == 0 && count == request.Data.Length)
        {
            data = request.Data;
        }
        else
        {
            AsnResourceLimits limits = request.Options == null
                    || request.Options.ResourceLimits == null
                ? new AsnResourceLimits()
                : request.Options.ResourceLimits;
            if (!new ResourceCounter(limits).TryChargeAllocationBytes(count))
            {
                return this.CreateResourceDecodeFailure(
                    "The ECN input range exceeds the configured allocation limit.");
            }

            data = new byte[count];
            System.Buffer.BlockCopy(
                request.Data,
                request.Offset,
                data,
                0,
                count);
        }
        return new EcnIntegratedPlanCodec(
            selection.Plan,
            selection.Category,
            selection.Properties,
            selection.Type,
            request.Options ?? this.Options).DecodeIntegrated(
                data,
                count * 8,
                []);
    }

    internal AsnEncodeResult CompleteEncodeResult(AsnEncodeResult result)
    {
        if (result.Data == null)
        {
            return this.CreateEncodeFailure(
                "ASNCODEC0006",
                "The selected codec returned a null output buffer.");
        }

        result.WrittenCount = result.Data.Length;
        return result;
    }

    internal AsnDecodeResult ConvertIncrementalDecode(
        AsnIncrementalResult source)
    {
        return new AsnDecodeResult
        {
            Status = source.Status,
            Value = source.Value,
            ConsumedCount = source.ConsumedCount,
            Diagnostics = source.Diagnostics
        };
    }

    internal bool IsIncompleteDiagnostic(string code)
    {
        return code == "ASNP0004"
            || code == "ASNP0006"
            || code == "ASNO0003"
            || code == "ASNECN0206"
            || code == "ASNECN0207";
    }

    internal EcnCodecPlanSelection? FindEcnSelection(string typeName)
    {
        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(typeName);
        if (type == null)
        {
            return null;
        }

        EcnCodecPlanSelection? result = null;
        int index = 0;
        while (index < this.Catalog.EcnCodecPlans.Length)
        {
            EcnCodecPlanSelection candidate =
                this.Catalog.EcnCodecPlans[index];
            if (candidate.Type == type)
            {
                if (result != null)
                {
                    return null;
                }
                result = candidate;
            }
            index = index + 1;
        }
        return result;
    }

    internal AsnCodecOptions CreateEffectiveOptions(AsnCodecOptions? request)
    {
        AsnCodecOptions source = request == null ? this.Options : request;
        AsnResourceLimits? limits = source.ResourceLimits == null
            ? this.Options.ResourceLimits
            : source.ResourceLimits;
        return new AsnCodecOptions
        {
            EncodingRules = this.Rules,
            UnknownDataPolicy = source.UnknownDataPolicy,
            ValidateValues = source.ValidateValues,
            UseIndefiniteLengths = source.UseIndefiniteLengths,
            ResourceLimits = limits,
            ObjectSetRegistry = source.ObjectSetRegistry == null
                ? this.Options.ObjectSetRegistry
                : source.ObjectSetRegistry,
            PerInstructionRegistry = new CodecProviderImplementation()
                .SnapshotPerInstructionRegistry(
                    source.PerInstructionRegistry == null
                        ? this.Options.PerInstructionRegistry
                        : source.PerInstructionRegistry)
        };
    }

    internal bool IsBerRule()
    {
        return this.Rules == AsnEncodingRules.Ber
            || this.Rules == AsnEncodingRules.Der
            || this.Rules == AsnEncodingRules.Cer;
    }

    internal bool IsPerRule()
    {
        return this.Rules == AsnEncodingRules.Aper
            || this.Rules == AsnEncodingRules.Uper
            || this.Rules == AsnEncodingRules.CanonicalAlignedPer
            || this.Rules == AsnEncodingRules.CanonicalUnalignedPer;
    }

    internal bool IsOerRule()
    {
        return this.Rules == AsnEncodingRules.Oer
            || this.Rules == AsnEncodingRules.Coer;
    }

    internal bool IsXerRule()
    {
        return this.Rules == AsnEncodingRules.BasicXer
            || this.Rules == AsnEncodingRules.CanonicalXer
            || this.Rules == AsnEncodingRules.ExtendedXer;
    }

    internal AsnEncodeResult CreateEncodeFailure(
        string code,
        string message)
    {
        return new AsnEncodeResult
        {
            Diagnostics = [this.CreateDiagnostic(
                code,
                message,
                AsnDiagnosticCategory.Encoding)]
        };
    }

    internal AsnDecodeResult CreateDecodeFailure(
        string code,
        string message)
    {
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Error,
            Diagnostics = [this.CreateDiagnostic(
                code,
                message,
                AsnDiagnosticCategory.Decoding)]
        };
    }

    internal AsnDecodeResult CreateResourceDecodeFailure(string message)
    {
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Error,
            Diagnostics = [this.CreateDiagnostic(
                "ASNR0005",
                message,
                AsnDiagnosticCategory.Resource)]
        };
    }

    internal AsnDiagnostic CreateDiagnostic(
        string code,
        string message,
        AsnDiagnosticCategory category)
    {
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = category,
            Code = code,
            Message = message
        };
    }
}
