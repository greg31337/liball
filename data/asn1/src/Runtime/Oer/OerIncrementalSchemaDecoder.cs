namespace asn1;

internal class OerIncrementalSchemaDecoder : IAsnIncrementalDecoder
{
    internal AsnReflectionCatalog Catalog;
    internal string TypeName;
    internal AsnCodecOptions Options;
    internal AsnResourceLimits Limits;
    internal OerSchemaCodec Codec;
    internal ByteAccumulator Buffer = new ByteAccumulator(0);
    internal ResourceCounter Resources;
    internal bool Terminal;
    internal AsnIncrementalResult TerminalResult = new AsnIncrementalResult();

    internal OerIncrementalSchemaDecoder(
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
        this.Codec = new OerSchemaCodec(catalog, options.EncodingRules);
        this.Resources = new ResourceCounter(this.Limits);
    }

    public AsnIncrementalResult Consume(AsnIncrementalInput input)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        if (input.Data == null)
        {
            throw new System.ArgumentException(
                "The incremental OER input data cannot be null.",
                "input");
        }

        int count = input.HasCount
            ? input.Count
            : input.Data.Length - input.Offset;
        if (!CursorMath.IsRangeValid(input.Offset, count, input.Data.Length))
        {
            throw new System.ArgumentOutOfRangeException("input");
        }

        if (this.Terminal)
        {
            return this.CreateStateError(
                0,
                "ASNO0104",
                "The incremental OER decoder is terminal until Reset is called.",
                false);
        }

        if (!this.Resources.TryChargeAllocationBytes(count))
        {
            AsnIncrementalResult limited = this.SetTerminalError(
                0,
                "ASNR0005",
                "Buffering incremental OER input exceeds the configured allocation limit.",
                true);
            this.SetResultByteOffset(limited, this.Buffer.Count);
            return limited;
        }

        this.Buffer.AppendSegment(input.Data, input.Offset, count);
        return this.DecodeBufferedInput(count, input.IsFinalBlock);
    }

    public AsnIncrementalResult Complete()
    {
        if (this.Terminal)
        {
            return this.CopyTerminalResult();
        }

        return this.DecodeBufferedInput(0, true);
    }

    public void Reset()
    {
        this.Buffer.Clear();
        this.Resources = new ResourceCounter(this.Limits);
        this.Terminal = false;
        this.TerminalResult = new AsnIncrementalResult();
    }

    internal AsnIncrementalResult DecodeBufferedInput(
        int consumedCount,
        bool finalBlock)
    {
        AsnDecodeResult decoded = this.Codec.DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = this.TypeName,
                Data = this.Buffer.ToArray(),
                Options = this.Options
            });
        bool incomplete = decoded.Status == AsnDecodeStatus.NeedMoreData
            || decoded.Status == AsnDecodeStatus.Error
                && decoded.Diagnostics.Length > 0
                && decoded.Diagnostics[0].Code == "ASNO0003";
        if (incomplete && !finalBlock)
        {
            return new AsnIncrementalResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = consumedCount
            };
        }

        if (incomplete)
        {
            AsnIncrementalResult truncated = this.SetTerminalError(
                consumedCount,
                "ASNO0105",
                "The final OER input ends within an encoded value.",
                false);
            this.SetResultByteOffset(truncated, decoded.ConsumedCount);
            return truncated;
        }

        if (decoded.Status != AsnDecodeStatus.Done)
        {
            AsnIncrementalResult failure = new AsnIncrementalResult
            {
                Status = AsnDecodeStatus.Error,
                ConsumedCount = consumedCount,
                Diagnostics = decoded.Diagnostics
            };
            this.SetTerminal(failure);
            return failure;
        }

        if (!finalBlock)
        {
            return new AsnIncrementalResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = consumedCount
            };
        }

        AsnIncrementalResult result = new AsnIncrementalResult
        {
            Status = AsnDecodeStatus.Done,
            ConsumedCount = consumedCount,
            Value = decoded.Value
        };
        this.SetTerminal(result);
        return result;
    }

    internal AsnIncrementalResult SetTerminalError(
        int consumedCount,
        string code,
        string message,
        bool limitExceeded)
    {
        AsnIncrementalResult result = this.CreateStateError(
            consumedCount,
            code,
            message,
            limitExceeded);
        this.SetTerminal(result);
        return result;
    }

    internal AsnIncrementalResult CreateStateError(
        int consumedCount,
        string code,
        string message,
        bool limitExceeded)
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
                    Category = limitExceeded
                        ? AsnDiagnosticCategory.Resource
                        : AsnDiagnosticCategory.Decoding,
                    Code = code,
                    Message = message
                }
            ]
        };
    }

    internal void SetTerminal(AsnIncrementalResult result)
    {
        this.Terminal = true;
        this.TerminalResult = result;
    }

    internal void SetResultByteOffset(
        AsnIncrementalResult result,
        int byteOffset)
    {
        if (result.Diagnostics.Length > 0)
        {
            result.Diagnostics[0].HasBitOffset = true;
            result.Diagnostics[0].BitOffset = byteOffset > int.MaxValue / 8
                ? int.MaxValue
                : byteOffset * 8;
            result.Diagnostics[0].Message = result.Diagnostics[0].Message
                + " Byte offset: "
                + byteOffset.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                + ".";
        }
    }

    internal AsnIncrementalResult CopyTerminalResult()
    {
        return new AsnIncrementalResult
        {
            Status = this.TerminalResult.Status,
            Value = this.TerminalResult.Value,
            Diagnostics = this.TerminalResult.Diagnostics
        };
    }
}
