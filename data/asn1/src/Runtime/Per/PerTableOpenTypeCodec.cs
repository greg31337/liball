namespace asn1;

internal class PerTableOpenTypeCodec
{
    internal AsnResourceLimits Limits;
    internal PerOpenTypeCodec OpenTypes;
    internal RuntimeDispatchKeyConverter KeyConverter =
        new RuntimeDispatchKeyConverter();
    internal DispatchKeyNormalizer KeyNormalizer =
        new DispatchKeyNormalizer();

    internal PerTableOpenTypeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.OpenTypes = new PerOpenTypeCodec(limits);
    }

    internal PerTableOpenTypeResult ReadTableOpenType(
        PerBitReader reader,
        AsnObjectTableDescriptor table,
        AsnRuntimeValue dispatchKey,
        AsnUnknownDataPolicy policy,
        AsnEncodingRules rules,
        AsnRuntimeValue? decodedValue,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        int start = reader.Position;
        PerOpenTypeResult payload = this.OpenTypes.ReadOpenType(
            reader,
            aligned);
        if (!payload.Success)
        {
            return this.CopyOpenTypeFailure(payload);
        }

        PerTableOpenTypeResult resolved = this.ResolveTablePayload(
            table,
            dispatchKey,
            payload.Data,
            policy,
            rules,
            decodedValue);
        if (!resolved.Success)
        {
            return resolved;
        }

        resolved.BitCount = reader.Position - start;
        return resolved;
    }

    internal PerTableOpenTypeResult ResolveTablePayload(
        AsnObjectTableDescriptor table,
        AsnRuntimeValue dispatchKey,
        byte[] payload,
        AsnUnknownDataPolicy policy,
        AsnEncodingRules rules,
        AsnRuntimeValue? decodedValue)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (dispatchKey == null)
        {
            throw new System.ArgumentNullException("dispatchKey");
        }

        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
            payload.Length))
        {
            return this.CreateError(
                "ASNR0006",
                "The table-constrained open payload exceeds the configured limit.",
                true);
        }

        PerTableOpenTypeResult entry = this.ResolveEntry(table, dispatchKey);
        if (entry.Success && entry.ResolvedType != null)
        {
            if (decodedValue == null)
            {
                entry.Data = this.CopyBytes(payload);
                return entry;
            }

            string identity = entry.ResolvedType.StableId.Length > 0
                ? entry.ResolvedType.StableId
                : entry.ResolvedType.Name;
            entry.OpenValue = new AsnOpenTypeValue
            {
                Resolved = true,
                ResolvedTypeId = identity,
                TypeName = entry.ResolvedType.Name,
                Value = decodedValue,
                DispatchKey = dispatchKey,
                HasEncodingRules = true,
                EncodingRules = rules,
                HasOriginalData = true,
                OriginalData = this.CopyBytes(payload)
            };
            entry.Data = this.CopyBytes(payload);
            return entry;
        }

        if (entry.ErrorCode != "ASNP0080")
        {
            return entry;
        }

        if (policy == AsnUnknownDataPolicy.Reject)
        {
            return this.CreateError(
                "ASNP0080",
                "The open-type dispatch key is not present in the governing table.",
                false);
        }

        bool preserve = policy == AsnUnknownDataPolicy.Preserve;
        return new PerTableOpenTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = this.CopyBytes(payload),
            OpenValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                DispatchKey = dispatchKey,
                HasEncodingRules = preserve,
                EncodingRules = rules,
                HasOriginalData = preserve,
                OriginalData = preserve ? this.CopyBytes(payload) : []
            }
        };
    }

    internal PerTableOpenTypeResult WriteTableOpenType(
        PerBitWriter writer,
        AsnObjectTableDescriptor table,
        AsnOpenTypeValue open,
        byte[] resolvedEncoding,
        int resolvedBitCount,
        AsnUnknownDataPolicy policy,
        AsnEncodingRules rules,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (open == null || !open.IsValid())
        {
            return this.CreateError(
                "ASNP0081",
                "The table-constrained open value is inconsistent.",
                false);
        }

        byte[] encoding;
        int bitCount;
        if (open.Unresolved)
        {
            if (policy != AsnUnknownDataPolicy.Preserve
                || !new OpaquePayloadPolicy().CanReuseOpenType(
                    open,
                    rules))
            {
                return this.CreateError(
                    "ASNP0082",
                    "The unresolved open value has no compatible preserved payload.",
                    false);
            }

            encoding = open.OriginalData;
            bitCount = encoding.Length * 8;
        }
        else
        {
            if (open.DispatchKey == null)
            {
                return this.CreateError(
                    "ASNP0081",
                    "A resolved open value requires its dispatch key.",
                    false);
            }

            PerTableOpenTypeResult entry = this.ResolveEntry(
                table,
                open.DispatchKey);
            if (!entry.Success || entry.ResolvedType == null
                || !this.TypeMatches(open, entry.ResolvedType))
            {
                return this.CreateError(
                    "ASNP0083",
                    "The resolved open value does not match its table entry.",
                    false);
            }

            encoding = resolvedEncoding;
            bitCount = resolvedBitCount;
        }

        int start = writer.GetWrittenBitCount();
        PerOpenTypeResult written = this.OpenTypes.WriteOpenType(
            writer,
            encoding,
            bitCount,
            aligned);
        if (!written.Success)
        {
            return this.CopyOpenTypeFailure(written);
        }

        return new PerTableOpenTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            OpenValue = open,
            Data = written.Data,
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerTableOpenTypeResult ResolveEntry(
        AsnObjectTableDescriptor table,
        AsnRuntimeValue dispatchKey)
    {
        NormalizedDispatchKey runtime = this.KeyNormalizer.NormalizeKey(
            this.KeyConverter.ConvertKey(dispatchKey));
        AsnTypeDescriptor? match = null;
        int index = 0;
        while (index < table.Entries.Length)
        {
            AsnObjectTableEntryDescriptor entry = table.Entries[index];
            AsnValue? key = entry.Key == null ? null : entry.Key.Value;
            NormalizedDispatchKey candidate = this.KeyNormalizer.NormalizeKey(
                key);
            if (runtime.Success && candidate.Success
                && runtime.Identity == candidate.Identity
                && entry.Type != null)
            {
                if (match != null)
                {
                    return this.CreateError(
                        "ASNP0084",
                        "The open-type dispatch key identifies multiple table entries.",
                        false);
                }

                match = entry.Type;
            }

            index = index + 1;
        }

        if (match != null)
        {
            return new PerTableOpenTypeResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                ResolvedType = match
            };
        }

        return new PerTableOpenTypeResult
        {
            Status = AsnDecodeStatus.Error,
            ErrorCode = "ASNP0080",
            ErrorMessage = "The open-type dispatch key is absent."
        };
    }

    internal bool TypeMatches(
        AsnOpenTypeValue open,
        AsnTypeDescriptor type)
    {
        string identity = open.GetResolvedTypeIdentity();
        return identity == type.StableId
            || identity == type.Name
            || identity.EndsWith(
                "." + type.Name,
                System.StringComparison.Ordinal)
            || open.TypeName == type.Name;
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        if (source.Length > 0)
        {
            System.Buffer.BlockCopy(source, 0, result, 0, source.Length);
        }

        return result;
    }

    internal PerTableOpenTypeResult CopyOpenTypeFailure(
        PerOpenTypeResult source)
    {
        return new PerTableOpenTypeResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerTableOpenTypeResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerTableOpenTypeResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
