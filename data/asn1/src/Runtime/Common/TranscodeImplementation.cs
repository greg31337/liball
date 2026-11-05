namespace asn1;

internal class TranscodeImplementation
{
    internal ConfiguredCodec Codec;

    internal TranscodeImplementation(ConfiguredCodec codec)
    {
        this.Codec = codec == null
            ? throw new System.ArgumentNullException("codec")
            : codec;
    }

    internal AsnTranscodeResult Transcode(AsnTranscodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.TargetOptions == null)
        {
            return this.CreateFailure(
                request,
                null,
                [this.CreateDiagnostic(
                    "ASNTRANS0001",
                    "A transcode request requires target encoding options.",
                    "$")]);
        }

        CodecProviderImplementation provider =
            new CodecProviderImplementation();
        AsnEncodingRules targetRules =
            request.TargetOptions.EncodingRules;
        if (!provider.IsSupportedRule(targetRules))
        {
            return this.CreateFailure(
                request,
                null,
                [this.CreateDiagnostic(
                    "ASNTRANS0002",
                    "The requested target encoding rules are not supported.",
                    "$")]);
        }

        int count = this.GetInputCount(request);
        AsnCodecOptions sourceOptions = this.Codec.CreateEffectiveOptions(
            request.SourceOptions);
        sourceOptions.UnknownDataPolicy = AsnUnknownDataPolicy.Preserve;
        AsnDecodeResult decoded = this.Codec.Decode(new AsnDecodeRequest
        {
            TypeName = request.TypeName,
            Data = request.Data,
            Offset = request.Offset,
            HasCount = request.HasCount,
            Count = request.Count,
            Options = sourceOptions
        });
        if (decoded.Status != AsnDecodeStatus.Done
            || decoded.Value == null)
        {
            AsnDiagnostic[] diagnostics = decoded.Diagnostics.Length == 0
                ? [this.CreateDiagnostic(
                    "ASNTRANS0003",
                    "The source value could not be decoded for transcoding.",
                    "$")]
                : decoded.Diagnostics;
            return this.CreateFailure(request, decoded.Value, diagnostics);
        }
        if (decoded.ConsumedCount != count)
        {
            AsnDiagnostic trailing = this.CreateDiagnostic(
                "ASNTRANS0004",
                "A transcode source must contain exactly one framed value.",
                "$");
            trailing.HasBitOffset = true;
            trailing.BitOffset = decoded.ConsumedCount > int.MaxValue / 8
                ? int.MaxValue
                : decoded.ConsumedCount * 8;
            return this.CreateFailure(request, decoded.Value, [trailing]);
        }

        AsnDiagnostic? issue = this.FindTransferIssue(
            decoded.Value,
            targetRules,
            "$");
        if (issue != null)
        {
            return this.CreateFailure(request, decoded.Value, [issue]);
        }

        AsnCodecOptions targetOptions = provider.CloneOptions(
            request.TargetOptions);
        targetOptions.UnknownDataPolicy = AsnUnknownDataPolicy.Preserve;
        IAsnCodec target = provider.CreateCodec(
            this.Codec.Catalog,
            targetOptions);
        AsnEncodeResult encoded = target.Encode(new AsnEncodeRequest
        {
            TypeName = request.TypeName,
            Value = decoded.Value,
            Options = targetOptions
        });
        if (!encoded.Success)
        {
            AsnDiagnostic[] diagnostics = encoded.Diagnostics.Length == 0
                ? [this.CreateDiagnostic(
                    "ASNTRANS0008",
                    "The normalized value could not be encoded under the target rules.",
                    "$")]
                : encoded.Diagnostics;
            return this.CreateFailure(request, decoded.Value, diagnostics);
        }

        return new AsnTranscodeResult
        {
            Success = true,
            SourceEncodingRules = this.Codec.Rules,
            TargetEncodingRules = targetRules,
            Value = decoded.Value,
            Data = encoded.Data,
            WrittenCount = encoded.WrittenCount
        };
    }

    internal AsnDiagnostic? FindTransferIssue(
        AsnRuntimeValue value,
        AsnEncodingRules targetRules,
        string path)
    {
        OpaquePayloadPolicy policy = new OpaquePayloadPolicy();
        if (value.OpenTypeValue != null)
        {
            AsnOpenTypeValue open = value.OpenTypeValue;
            if (open.Unresolved
                && !policy.CanReuseOpenType(open, targetRules))
            {
                return this.CreateDiagnostic(
                    "ASNTRANS0005",
                    "An unresolved open payload cannot be transferred to different encoding rules without loss.",
                    path);
            }
            if (open.Resolved && open.Value != null)
            {
                AsnDiagnostic? nestedOpen = this.FindTransferIssue(
                    open.Value,
                    targetRules,
                    path + ".<open>");
                if (nestedOpen != null)
                {
                    return nestedOpen;
                }
            }
        }

        int extensionIndex = 0;
        while (extensionIndex < value.UnknownExtensions.Length)
        {
            AsnUnknownExtension extension =
                value.UnknownExtensions[extensionIndex];
            if (!policy.CanReuseUnknownExtension(
                extension,
                targetRules))
            {
                return this.CreateDiagnostic(
                    "ASNTRANS0006",
                    "An unknown extension payload cannot be transferred to different encoding rules without loss.",
                    path + ".<extension["
                        + extension.Index.ToString(
                            System.Globalization.CultureInfo.InvariantCulture)
                        + "]>");
            }
            extensionIndex = extensionIndex + 1;
        }

        int unknownIndex = 0;
        while (unknownIndex < value.UnknownFields.Length)
        {
            AsnUnknownField unknown = value.UnknownFields[unknownIndex];
            if (!policy.CanReuseUnknownField(unknown, targetRules))
            {
                return this.CreateDiagnostic(
                    "ASNTRANS0007",
                    "An unknown field payload cannot be transferred to different encoding rules without loss.",
                    path + ".<unknown[" + unknown.Name + "]>");
            }
            unknownIndex = unknownIndex + 1;
        }

        int fieldIndex = 0;
        while (fieldIndex < value.Fields.Length)
        {
            AsnRuntimeFieldValue field = value.Fields[fieldIndex];
            if (field.Present && field.Value != null)
            {
                AsnDiagnostic? fieldIssue = this.FindTransferIssue(
                    field.Value,
                    targetRules,
                    path + "." + field.Name);
                if (fieldIssue != null)
                {
                    return fieldIssue;
                }
            }
            fieldIndex = fieldIndex + 1;
        }

        int itemIndex = 0;
        while (itemIndex < value.Items.Length)
        {
            AsnDiagnostic? itemIssue = this.FindTransferIssue(
                value.Items[itemIndex],
                targetRules,
                path + "[" + itemIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + "]");
            if (itemIssue != null)
            {
                return itemIssue;
            }
            itemIndex = itemIndex + 1;
        }

        return null;
    }

    internal int GetInputCount(AsnTranscodeRequest request)
    {
        if (request.Data == null)
        {
            throw new System.ArgumentException(
                "The transcode source data cannot be null.",
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

    internal AsnTranscodeResult CreateFailure(
        AsnTranscodeRequest request,
        AsnRuntimeValue? value,
        AsnDiagnostic[] diagnostics)
    {
        return new AsnTranscodeResult
        {
            SourceEncodingRules = this.Codec.Rules,
            TargetEncodingRules = request.TargetOptions == null
                ? this.Codec.Rules
                : request.TargetOptions.EncodingRules,
            Value = value,
            Diagnostics = diagnostics
        };
    }

    internal AsnDiagnostic CreateDiagnostic(
        string code,
        string message,
        string path)
    {
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = AsnDiagnosticCategory.Encoding,
            Code = code,
            Message = message,
            ValuePath = path
        };
    }
}
