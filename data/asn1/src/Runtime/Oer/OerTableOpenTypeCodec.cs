namespace asn1;

internal class OerTableOpenTypeCodec
{
    internal AsnResourceLimits Limits;
    internal RuntimeDispatchKeyConverter KeyConverter =
        new RuntimeDispatchKeyConverter();
    internal DispatchKeyNormalizer KeyNormalizer =
        new DispatchKeyNormalizer();

    internal OerTableOpenTypeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerOpenValueResult DecodeTableOpenType(
        AsnObjectTableDescriptor table,
        AsnRuntimeValue dispatchKey,
        byte[] data,
        int offset,
        int count,
        AsnUnknownDataPolicy policy,
        bool canonical)
    {
        OerOpenValueResult entry = this.ResolveEntry(table, dispatchKey);
        if (!entry.Success && entry.ErrorCode != "ASNO0095")
        {
            return entry;
        }

        AsnTypeDescriptor? resolvedType = entry.Success
            ? entry.ResolvedType
            : null;
        OerOpenValueResult decoded = new OerOpenValueCodec(this.Limits)
            .DecodeOpenValue(
                resolvedType,
                dispatchKey,
                data,
                offset,
                count,
                policy,
                canonical);
        if (!decoded.Success && resolvedType == null
            && decoded.ErrorCode == "ASNO0091")
        {
            decoded.ErrorCode = "ASNO0095";
            decoded.ErrorMessage =
                "The open-type dispatch key is not present in the governing table.";
        }

        return decoded;
    }

    internal OerOpenValueResult ResolveTablePayload(
        AsnObjectTableDescriptor table,
        AsnRuntimeValue dispatchKey,
        byte[] payload,
        AsnUnknownDataPolicy policy,
        bool canonical)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        OerContentResult wrapped = new OerOpenTypeCodec(this.Limits)
            .EncodeOpenType(payload);
        if (!wrapped.Success)
        {
            return new OerOpenValueCodec(this.Limits).CopyContentFailure(
                wrapped);
        }

        return this.DecodeTableOpenType(
            table,
            dispatchKey,
            wrapped.Data,
            0,
            wrapped.Data.Length,
            policy,
            canonical);
    }

    internal OerOpenValueResult EncodeTableOpenType(
        AsnObjectTableDescriptor table,
        AsnOpenTypeValue open,
        AsnUnknownDataPolicy policy,
        bool canonical)
    {
        if (open == null || !open.IsValid())
        {
            return this.CreateTableError(
                "ASNO0096",
                "The table-constrained OER open value is inconsistent.",
                false);
        }

        AsnTypeDescriptor? resolvedType = null;
        if (open.Resolved)
        {
            if (open.DispatchKey == null)
            {
                return this.CreateTableError(
                    "ASNO0096",
                    "A resolved table-constrained value requires its dispatch key.",
                    false);
            }

            OerOpenValueResult entry = this.ResolveEntry(
                table,
                open.DispatchKey);
            if (!entry.Success || entry.ResolvedType == null)
            {
                return this.CreateTableError(
                    "ASNO0097",
                    "The resolved OER open value has no matching table entry.",
                    false);
            }

            resolvedType = entry.ResolvedType;
        }

        return new OerOpenValueCodec(this.Limits).EncodeOpenValue(
            open,
            resolvedType,
            policy,
            canonical);
    }

    internal OerOpenValueResult ResolveEntry(
        AsnObjectTableDescriptor table,
        AsnRuntimeValue dispatchKey)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (dispatchKey == null)
        {
            throw new System.ArgumentNullException("dispatchKey");
        }

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
                    return this.CreateTableError(
                        "ASNO0098",
                        "The OER dispatch key identifies multiple table entries.",
                        false);
                }

                match = entry.Type;
            }

            index = index + 1;
        }

        if (match != null)
        {
            return new OerOpenValueResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                ResolvedType = match
            };
        }

        return this.CreateTableError(
            "ASNO0095",
            "The open-type dispatch key is absent.",
            false);
    }

    internal OerOpenValueResult CreateTableError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerOpenValueResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
