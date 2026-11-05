namespace asn1;

internal class OpenTypeRuntimeCodec
{
    internal IAsnCodec Codec;
    internal IAsnObjectSetRegistry CatalogRegistry;
    internal RuntimeDispatchKeyConverter KeyConverter = new RuntimeDispatchKeyConverter();

    internal OpenTypeRuntimeCodec(
        IAsnCodec codec,
        IAsnObjectSetRegistry catalogRegistry)
    {
        this.Codec = codec == null
            ? throw new System.ArgumentNullException("codec")
            : codec;
        this.CatalogRegistry = catalogRegistry == null
            ? throw new System.ArgumentNullException("catalogRegistry")
            : catalogRegistry;
    }

    internal AsnDecodeResult DecodePayload(
        AsnInformationObjectSet objectSet,
        AsnRuntimeValue dispatchKey,
        byte[] payload,
        AsnCodecOptions options)
    {
        if (objectSet == null)
        {
            throw new System.ArgumentNullException("objectSet");
        }

        if (dispatchKey == null)
        {
            throw new System.ArgumentNullException("dispatchKey");
        }

        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (!this.SupportsRules(options.EncodingRules))
        {
            return this.CreateDecodeError(
                "ASND0004",
                "The selected encoding rules do not define this open-type payload path.");
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        if (!new ResourceCounter(limits).AllowsOpenTypeBytes(payload.Length))
        {
            return this.CreateDecodeErrorWithDiagnostics(
                "ASNR0001",
                "The open-type payload exceeds the configured byte limit.",
                [this.CreateDiagnostic(
                    AsnDiagnosticCategory.Resource,
                    "ASNR0001",
                    "The open-type payload exceeds the configured byte limit.")]);
        }

        IAsnObjectSetRegistry registry = options.ObjectSetRegistry == null
            ? this.CatalogRegistry
            : options.ObjectSetRegistry;
        if (!this.AllowsObjectTableEntries(registry, objectSet, limits))
        {
            return this.CreateDecodeErrorWithDiagnostics(
                "ASNR0011",
                "The governing object table exceeds the configured collection limit.",
                [this.CreateDiagnostic(
                    AsnDiagnosticCategory.Resource,
                    "ASNR0011",
                    "The governing object table exceeds the configured collection limit.")]);
        }

        AsnOpenTypeDispatchEntry? entry = this.ResolveEntry(
            registry,
            objectSet,
            dispatchKey);
        if (entry == null
            || entry.Type == null
            || entry.Type.Name == null
            || entry.Type.Name.Length == 0)
        {
            return this.DecodeUnknown(
                objectSet,
                dispatchKey,
                payload,
                options);
        }

        AsnDecodeResult decoded = this.Codec.Decode(new AsnDecodeRequest
        {
            TypeName = entry.Type.Name,
            Data = this.CopyBytes(payload),
            HasCount = true,
            Count = payload.Length,
            Options = options
        });
        if (decoded.Status != AsnDecodeStatus.Done
            || decoded.Value == null
            || decoded.ConsumedCount != payload.Length)
        {
            return this.CreateDecodeErrorWithDiagnostics(
                "ASND0002",
                "The resolved open-type payload could not be decoded completely.",
                decoded.Diagnostics);
        }

        AsnOpenTypeValue open = new AsnOpenTypeValue
        {
            Resolved = true,
            ResolvedTypeId = entry.Type.Name,
            TypeName = entry.Type.Name,
            Value = decoded.Value,
            DispatchKey = dispatchKey,
            EncodingRules = options.EncodingRules,
            HasEncodingRules = true,
            HasOriginalData = true,
            OriginalData = this.CopyBytes(payload)
        };
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OpenType,
                OpenTypeValue = open
            },
            ConsumedCount = payload.Length
        };
    }

    internal AsnEncodeResult EncodePayload(
        AsnInformationObjectSet objectSet,
        AsnOpenTypeValue open,
        AsnCodecOptions options)
    {
        if (objectSet == null)
        {
            throw new System.ArgumentNullException("objectSet");
        }

        if (open == null)
        {
            throw new System.ArgumentNullException("open");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (!this.SupportsRules(options.EncodingRules))
        {
            return this.CreateEncodeError(
                "ASNE0004",
                "The selected encoding rules do not define this open-type payload path.");
        }

        if (!open.IsValid())
        {
            return this.CreateEncodeError(
                "ASNE0001",
                "The open-type value has an inconsistent resolution or payload state.");
        }

        if (open.Unresolved)
        {
            return this.EncodeUnresolved(objectSet, open, options);
        }

        if (!open.Resolved || open.Value == null || open.DispatchKey == null)
        {
            return this.CreateEncodeError(
                "ASNE0001",
                "A resolved open value requires a decoded value and dispatch key.");
        }

        IAsnObjectSetRegistry registry = options.ObjectSetRegistry == null
            ? this.CatalogRegistry
            : options.ObjectSetRegistry;
        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        if (!this.AllowsObjectTableEntries(registry, objectSet, limits))
        {
            return this.CreateEncodeErrorWithDiagnostics(
                "ASNR0011",
                "The governing object table exceeds the configured collection limit.",
                [this.CreateDiagnostic(
                    AsnDiagnosticCategory.Resource,
                    "ASNR0011",
                    "The governing object table exceeds the configured collection limit.")]);
        }

        AsnOpenTypeDispatchEntry? entry = this.ResolveEntry(
            registry,
            objectSet,
            open.DispatchKey);
        if (entry == null
            || entry.Type == null
            || entry.Type.Name == null
            || entry.Type.Name.Length == 0)
        {
            return this.CreateEncodeError(
                "ASNE0002",
                "The open value dispatch key is not present in the governing table.");
        }

        if (!this.TypeMatches(open, entry.Type.Name))
        {
            return this.CreateEncodeError(
                "ASNE0003",
                "The open value type identity does not match the table dispatch entry.");
        }

        AsnEncodeResult encoded = this.Codec.Encode(new AsnEncodeRequest
        {
            TypeName = entry.Type.Name,
            Value = open.Value,
            Options = options
        });
        if (!encoded.Success
            || encoded.Data == null
            || encoded.WrittenCount < 0
            || encoded.WrittenCount > encoded.Data.Length)
        {
            return this.CreateEncodeErrorWithDiagnostics(
                "ASNE0005",
                "The resolved open value could not be encoded.",
                encoded.Diagnostics);
        }

        if (!new ResourceCounter(limits).AllowsOpenTypeBytes(encoded.WrittenCount))
        {
            return this.CreateEncodeErrorWithDiagnostics(
                "ASNR0001",
                "The encoded open-type payload exceeds the configured byte limit.",
                [this.CreateDiagnostic(
                    AsnDiagnosticCategory.Resource,
                    "ASNR0001",
                    "The encoded open-type payload exceeds the configured byte limit.")]);
        }

        byte[] payload = this.CopyByteCount(encoded.Data, encoded.WrittenCount);
        return new AsnEncodeResult
        {
            Success = true,
            Data = payload,
            WrittenCount = payload.Length
        };
    }

    internal AsnDecodeResult DecodeUnknown(
        AsnInformationObjectSet objectSet,
        AsnRuntimeValue dispatchKey,
        byte[] payload,
        AsnCodecOptions options)
    {
        if (!objectSet.Extensible
            || (options.UnknownDataPolicy != AsnUnknownDataPolicy.Preserve
                && options.UnknownDataPolicy != AsnUnknownDataPolicy.Skip))
        {
            return this.CreateDecodeError(
                "ASND0001",
                "The open-type dispatch key is not present in the governing table.");
        }

        bool preserve = options.UnknownDataPolicy == AsnUnknownDataPolicy.Preserve;
        AsnOpenTypeValue open = new AsnOpenTypeValue
        {
            Unresolved = true,
            DispatchKey = dispatchKey,
            EncodingRules = options.EncodingRules,
            HasEncodingRules = preserve,
            HasOriginalData = preserve,
            OriginalData = preserve ? this.CopyBytes(payload) : []
        };
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OpenType,
                OpenTypeValue = open
            },
            ConsumedCount = payload.Length
        };
    }

    internal AsnEncodeResult EncodeUnresolved(
        AsnInformationObjectSet objectSet,
        AsnOpenTypeValue open,
        AsnCodecOptions options)
    {
        if (!objectSet.Extensible
            || options.UnknownDataPolicy != AsnUnknownDataPolicy.Preserve
            || open == null
            || !open.IsValid()
            || !open.Unresolved
            || !open.HasOriginalData
            || !open.HasEncodingRules)
        {
            return this.CreateEncodeError(
                "ASNE0006",
                "The unresolved open value has no preservable payload.");
        }

        if (!new OpaquePayloadPolicy().CanReuseOpenType(
            open,
            options.EncodingRules))
        {
            return this.CreateEncodeError(
                "ASNE0007",
                "An unresolved open payload cannot be transcoded to different encoding rules.");
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        if (!new ResourceCounter(limits).AllowsOpenTypeBytes(open.OriginalData.Length))
        {
            return this.CreateEncodeErrorWithDiagnostics(
                "ASNR0001",
                "The preserved open-type payload exceeds the configured byte limit.",
                [this.CreateDiagnostic(
                    AsnDiagnosticCategory.Resource,
                    "ASNR0001",
                    "The preserved open-type payload exceeds the configured byte limit.")]);
        }

        byte[] payload = this.CopyBytes(open.OriginalData);
        return new AsnEncodeResult
        {
            Success = true,
            Data = payload,
            WrittenCount = payload.Length
        };
    }

    internal AsnOpenTypeDispatchEntry? ResolveEntry(
        IAsnObjectSetRegistry registry,
        AsnInformationObjectSet objectSet,
        AsnRuntimeValue dispatchKey)
    {
        AsnValue key = this.KeyConverter.ConvertKey(dispatchKey);
        return registry.ResolveDispatchEntry(objectSet, key);
    }

    internal bool AllowsObjectTableEntries(
        IAsnObjectSetRegistry registry,
        AsnInformationObjectSet objectSet,
        AsnResourceLimits limits)
    {
        ResourceCounter resources = new ResourceCounter(limits);
        if (!resources.AllowsCollectionElements(
            objectSet.DispatchEntries.Length))
        {
            return false;
        }

        AsnOpenTypeDispatchEntry[] entries = registry.GetEffectiveDispatchEntries(
            objectSet);
        return resources.AllowsCollectionElements(entries.Length);
    }

    internal bool TypeMatches(AsnOpenTypeValue open, string typeName)
    {
        string identity = open.GetResolvedTypeIdentity();
        return identity == typeName
            || identity.EndsWith(
                "." + typeName,
                System.StringComparison.Ordinal)
            || open.TypeName == typeName
            || open.TypeName.EndsWith(
                "." + typeName,
                System.StringComparison.Ordinal);
    }

    internal bool SupportsRules(AsnEncodingRules rules)
    {
        return rules == AsnEncodingRules.Ber
            || rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer
            || rules == AsnEncodingRules.Aper
            || rules == AsnEncodingRules.Uper
            || rules == AsnEncodingRules.CanonicalAlignedPer
            || rules == AsnEncodingRules.CanonicalUnalignedPer
            || rules == AsnEncodingRules.Oer
            || rules == AsnEncodingRules.Coer
            || rules == AsnEncodingRules.BasicXer
            || rules == AsnEncodingRules.CanonicalXer
            || rules == AsnEncodingRules.ExtendedXer;
    }

    internal byte[] CopyBytes(byte[] source)
    {
        return this.CopyByteCount(source, source.Length);
    }

    internal byte[] CopyByteCount(byte[] source, int count)
    {
        byte[] result = new byte[count];
        int index = 0;
        while (index < count)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    internal AsnDecodeResult CreateDecodeError(string code, string message)
    {
        return this.CreateDecodeErrorWithDiagnostics(
            code,
            message,
            [this.CreateDiagnostic(AsnDiagnosticCategory.Decoding, code, message)]);
    }

    internal AsnDecodeResult CreateDecodeErrorWithDiagnostics(
        string code,
        string message,
        AsnDiagnostic[] diagnostics)
    {
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Error,
            Diagnostics = diagnostics == null || diagnostics.Length == 0
                ? [this.CreateDiagnostic(AsnDiagnosticCategory.Decoding, code, message)]
                : diagnostics
        };
    }

    internal AsnEncodeResult CreateEncodeError(string code, string message)
    {
        return this.CreateEncodeErrorWithDiagnostics(
            code,
            message,
            [this.CreateDiagnostic(AsnDiagnosticCategory.Encoding, code, message)]);
    }

    internal AsnEncodeResult CreateEncodeErrorWithDiagnostics(
        string code,
        string message,
        AsnDiagnostic[] diagnostics)
    {
        return new AsnEncodeResult
        {
            Diagnostics = diagnostics == null || diagnostics.Length == 0
                ? [this.CreateDiagnostic(AsnDiagnosticCategory.Encoding, code, message)]
                : diagnostics
        };
    }

    internal AsnDiagnostic CreateDiagnostic(
        AsnDiagnosticCategory category,
        string code,
        string message)
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
