namespace asn1;

internal class BerIncrementalSchemaDecoder : IAsnIncrementalDecoder
{
    internal AsnReflectionCatalog Catalog;
    internal string TypeName;
    internal AsnCodecOptions Options;
    internal AsnResourceLimits Limits;
    internal BerIncrementalTlvDecoder TlvDecoder;

    internal BerIncrementalSchemaDecoder(
        AsnReflectionCatalog catalog,
        string typeName,
        AsnCodecOptions options)
    {
        this.Catalog = catalog == null
            ? throw new System.ArgumentNullException("catalog")
            : catalog;
        this.TypeName = typeName == null
            ? throw new System.ArgumentNullException("typeName")
            : typeName;
        this.Options = options == null
            ? throw new System.ArgumentNullException("options")
            : options;
        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        this.TlvDecoder = new BerIncrementalTlvDecoder(
            options.EncodingRules,
            true,
            this.Limits);
    }

    public AsnIncrementalResult Consume(AsnIncrementalInput input)
    {
        BerReadResult read = this.TlvDecoder.ConsumeChunk(input);
        return this.ConvertReadResult(read);
    }

    public AsnIncrementalResult Complete()
    {
        return this.ConvertReadResult(this.TlvDecoder.CompleteInput());
    }

    public void Reset()
    {
        this.TlvDecoder.ResetDecoder();
    }

    internal AsnIncrementalResult ConvertReadResult(BerReadResult read)
    {
        if (read.Status != AsnDecodeStatus.Done || read.Tlv == null)
        {
            return new AsnIncrementalResult
            {
                Status = read.Status,
                ConsumedCount = read.ConsumedCount,
                Diagnostics = read.Diagnostics
            };
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(this.TypeName);
        if (type == null)
        {
            return this.CreateSchemaError(
                read.ConsumedCount,
                "ASND0090",
                "The requested schema type is missing or ambiguous.");
        }

        BerSchemaDecodeResult decoded = new BerSchemaDecoder(this.Catalog)
            .DecodeTlv(type, read.Tlv, this.Options);
        if (decoded.Success
            && decoded.Value != null
            && this.Options.EncodingRules != AsnEncodingRules.Ber)
        {
            AsnIncrementalResult? canonical =
                this.CheckSchemaCanonicalEncoding(
                    type,
                    decoded.Value,
                    read.ConsumedCount);
            if (canonical != null)
            {
                return canonical;
            }
        }

        return new AsnIncrementalResult
        {
            Status = decoded.Success
                ? AsnDecodeStatus.Done
                : AsnDecodeStatus.Error,
            ConsumedCount = read.ConsumedCount,
            Value = decoded.Value,
            Diagnostics = decoded.Diagnostics
        };
    }

    internal AsnIncrementalResult? CheckSchemaCanonicalEncoding(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        int consumedCount)
    {
        BerSchemaEncodeResult encoded = new BerSchemaEncoder(this.Catalog)
            .EncodeTlv(type, value, this.Options);
        if (!encoded.Success || encoded.Tlv == null)
        {
            return new AsnIncrementalResult
            {
                Status = AsnDecodeStatus.Error,
                ConsumedCount = consumedCount,
                Diagnostics = encoded.Diagnostics
            };
        }

        BerWriteResult written = new BerTlvWriterImplementation(this.Limits)
            .Write(new BerWriteRequest
            {
                Tlv = encoded.Tlv,
                EncodingRules = this.Options.EncodingRules
            });
        if (!written.Success)
        {
            return new AsnIncrementalResult
            {
                Status = AsnDecodeStatus.Error,
                ConsumedCount = consumedCount,
                Diagnostics = written.Diagnostics
            };
        }

        int difference = this.FindCanonicalDifference(written.Data);
        if (difference < 0)
        {
            return null;
        }

        return new AsnIncrementalResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Canonicality,
                    Code = "ASNCAN0011",
                    Message = "The schema-directed encoding is not canonical. Byte offset: "
                        + difference.ToString(
                            System.Globalization.CultureInfo.InvariantCulture)
                        + "."
                }
            ]
        };
    }

    internal int FindCanonicalDifference(byte[] canonical)
    {
        int count = this.TlvDecoder.Input.Count < canonical.Length
            ? this.TlvDecoder.Input.Count
            : canonical.Length;
        int index = 0;
        while (index < count)
        {
            if (this.TlvDecoder.Input.Buffer[index] != canonical[index])
            {
                return index;
            }

            index = index + 1;
        }

        return this.TlvDecoder.Input.Count == canonical.Length ? -1 : count;
    }

    internal AsnIncrementalResult CreateSchemaError(
        int consumedCount,
        string code,
        string message)
    {
        return new AsnIncrementalResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Decoding,
                    Code = code,
                    Message = message
                }
            ]
        };
    }
}
