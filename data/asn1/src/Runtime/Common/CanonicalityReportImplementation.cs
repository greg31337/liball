namespace asn1;

internal class CanonicalityReportImplementation
{
    internal ConfiguredCodec Codec;

    internal CanonicalityReportImplementation(ConfiguredCodec codec)
    {
        this.Codec = codec == null
            ? throw new System.ArgumentNullException("codec")
            : codec;
    }

    internal AsnCanonicalityResult CheckCanonicality(
        AsnDecodeRequest request)
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
            Options = this.Codec.CreateEffectiveOptions(request.Options)
        };
        if (this.Codec.Rules == AsnEncodingRules.Der
            || this.Codec.Rules == AsnEncodingRules.Cer)
        {
            AsnResourceLimits limits = effective.Options == null
                    || effective.Options.ResourceLimits == null
                ? new AsnResourceLimits()
                : effective.Options.ResourceLimits;
            AsnCanonicalityResult result = new BerCanonicalValidator(limits)
                .ValidateRequest(effective);
            return this.EnrichBerResult(result, effective);
        }

        if (!this.IsCanonicalRule())
        {
            return this.CreateUnsupportedResult();
        }

        return this.CheckSchemaCanonicality(effective);
    }

    internal AsnCanonicalityResult CheckSchemaCanonicality(
        AsnDecodeRequest request)
    {
        int count = this.GetInputCount(request);
        AsnDecodeResult canonical = this.Codec.Decode(request);
        if (canonical.Status == AsnDecodeStatus.Done
            && canonical.ConsumedCount == count)
        {
            return this.CreateCanonicalResult();
        }
        if (canonical.Status == AsnDecodeStatus.Done)
        {
            return this.CreateTrailingInputResult(canonical.ConsumedCount);
        }

        ConfiguredCodec basicCodec = this.CreateBasicCodec(request);
        AsnDecodeResult basic = basicCodec.Decode(request);
        bool basicComplete = basic.Status == AsnDecodeStatus.Done
            && basic.ConsumedCount == count
            && basic.Value != null;
        AsnCanonicalityStatus status = basicComplete
            ? AsnCanonicalityStatus.NonCanonical
            : AsnCanonicalityStatus.Invalid;
        bool repairable = basicComplete
            && this.CanRepairValue(
                basic.Value,
                request.TypeName,
                request.Options);
        return this.CreateDecodeReport(
            canonical,
            status,
            repairable);
    }

    internal AsnCanonicalityResult EnrichBerResult(
        AsnCanonicalityResult result,
        AsnDecodeRequest request)
    {
        result.HasEncodingRules = true;
        result.EncodingRules = this.Codec.Rules;
        result.ViolationOffsetIsBit = false;
        result.Path = result.Path.Length == 0 ? "$" : result.Path;
        if (result.Status == AsnCanonicalityStatus.Canonical
            || result.Status == AsnCanonicalityStatus.NonCanonical)
        {
            ConfiguredCodec schemaCodec = result.Status
                    == AsnCanonicalityStatus.NonCanonical
                ? this.CreateBasicCodec(request)
                : this.Codec;
            AsnDecodeResult decoded = schemaCodec.Decode(request);
            int count = this.GetInputCount(request);
            bool complete = decoded.Status == AsnDecodeStatus.Done
                && decoded.ConsumedCount == count
                && decoded.Value != null;
            if (!complete)
            {
                return this.CreateDecodeReport(
                    decoded,
                    AsnCanonicalityStatus.Invalid,
                    false);
            }

            if (result.Status == AsnCanonicalityStatus.NonCanonical)
            {
                result.Repairable = this.CanRepairValue(
                    decoded.Value,
                    request.TypeName,
                    request.Options);
            }
        }
        return result;
    }

    internal ConfiguredCodec CreateBasicCodec(AsnDecodeRequest request)
    {
        AsnCodecOptions source = request.Options == null
            ? this.Codec.Options
            : request.Options;
        AsnCodecOptions options = new CodecProviderImplementation()
            .CloneOptions(source);
        options.EncodingRules = this.GetBasicRule();
        return (ConfiguredCodec)new CodecProviderImplementation()
            .CreateCodec(this.Codec.Catalog, options);
    }

    internal bool CanRepairValue(
        AsnRuntimeValue? value,
        string typeName,
        AsnCodecOptions? options)
    {
        if (value == null)
        {
            return false;
        }
        AsnEncodeResult encoded = this.Codec.Encode(new AsnEncodeRequest
        {
            TypeName = typeName,
            Value = value,
            Options = options
        });
        return encoded.Success;
    }

    internal AsnCanonicalityResult CreateDecodeReport(
        AsnDecodeResult decoded,
        AsnCanonicalityStatus status,
        bool repairable)
    {
        AsnDiagnostic diagnostic = decoded.Diagnostics.Length == 0
            ? this.CreateDiagnostic(
                status == AsnCanonicalityStatus.NonCanonical
                    ? "ASNCAN0013"
                    : "ASNCAN0014",
                status == AsnCanonicalityStatus.NonCanonical
                    ? "The encoded value is valid but not canonical."
                    : "The encoded value is not a valid value of the requested type.",
                status == AsnCanonicalityStatus.NonCanonical
                    ? AsnDiagnosticCategory.Canonicality
                    : AsnDiagnosticCategory.Decoding)
            : decoded.Diagnostics[0];
        if (status == AsnCanonicalityStatus.NonCanonical)
        {
            diagnostic.Category = AsnDiagnosticCategory.Canonicality;
        }

        bool bitOffset = diagnostic.HasBitOffset
            && this.IsPerCanonicalRule();
        int offset = diagnostic.HasBitOffset
            ? bitOffset
                ? diagnostic.BitOffset
                : diagnostic.BitOffset / 8
            : decoded.ConsumedCount;
        return new AsnCanonicalityResult
        {
            Status = status,
            HasEncodingRules = true,
            EncodingRules = this.Codec.Rules,
            HasViolationOffset = true,
            ViolationOffset = offset,
            ViolationOffsetIsBit = bitOffset,
            Repairable = repairable,
            Path = diagnostic.ValuePath.Length == 0
                ? "$"
                : diagnostic.ValuePath,
            Diagnostics = [diagnostic]
        };
    }

    internal AsnCanonicalityResult CreateCanonicalResult()
    {
        return new AsnCanonicalityResult
        {
            Status = AsnCanonicalityStatus.Canonical,
            HasEncodingRules = true,
            EncodingRules = this.Codec.Rules,
            Path = "$"
        };
    }

    internal AsnCanonicalityResult CreateTrailingInputResult(
        int consumedCount)
    {
        return new AsnCanonicalityResult
        {
            Status = AsnCanonicalityStatus.Invalid,
            HasEncodingRules = true,
            EncodingRules = this.Codec.Rules,
            HasViolationOffset = true,
            ViolationOffset = consumedCount,
            Path = "$",
            Diagnostics = [this.CreateDiagnostic(
                "ASNCAN0012",
                "Canonical input must contain exactly one framed value.",
                AsnDiagnosticCategory.Decoding)]
        };
    }

    internal AsnCanonicalityResult CreateUnsupportedResult()
    {
        return new AsnCanonicalityResult
        {
            Status = AsnCanonicalityStatus.Invalid,
            HasEncodingRules = true,
            EncodingRules = this.Codec.Rules,
            Path = "$",
            Diagnostics = [this.CreateDiagnostic(
                "ASNCODEC0005",
                "Canonicality checking requires canonical encoding rules.",
                AsnDiagnosticCategory.Canonicality)]
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

    internal int GetInputCount(AsnDecodeRequest request)
    {
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
        return count;
    }

    internal bool IsCanonicalRule()
    {
        return this.Codec.Rules == AsnEncodingRules.CanonicalAlignedPer
            || this.Codec.Rules
                == AsnEncodingRules.CanonicalUnalignedPer
            || this.Codec.Rules == AsnEncodingRules.Coer
            || this.Codec.Rules == AsnEncodingRules.CanonicalXer;
    }

    internal bool IsPerCanonicalRule()
    {
        return this.Codec.Rules == AsnEncodingRules.CanonicalAlignedPer
            || this.Codec.Rules
                == AsnEncodingRules.CanonicalUnalignedPer;
    }

    internal AsnEncodingRules GetBasicRule()
    {
        if (this.Codec.Rules == AsnEncodingRules.Der
            || this.Codec.Rules == AsnEncodingRules.Cer)
        {
            return AsnEncodingRules.Ber;
        }
        if (this.Codec.Rules == AsnEncodingRules.CanonicalAlignedPer)
        {
            return AsnEncodingRules.Aper;
        }
        if (this.Codec.Rules
            == AsnEncodingRules.CanonicalUnalignedPer)
        {
            return AsnEncodingRules.Uper;
        }
        if (this.Codec.Rules == AsnEncodingRules.Coer)
        {
            return AsnEncodingRules.Oer;
        }
        return AsnEncodingRules.BasicXer;
    }
}
