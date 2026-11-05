namespace asn1;

internal class PerIncrementalSchemaDecoder : IAsnIncrementalDecoder
{
    internal AsnReflectionCatalog Catalog;
    internal string TypeName;
    internal AsnCodecOptions Options;
    internal AsnResourceLimits Limits;
    internal PerSchemaCodec Codec;
    internal PerBitReader Reader;
    internal bool Terminal;
    internal AsnIncrementalResult TerminalResult = new AsnIncrementalResult();

    internal PerIncrementalSchemaDecoder(
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
        this.Codec = new PerSchemaCodec(catalog, options.EncodingRules);
        this.Reader = new PerBitReader(this.Limits);
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
                "The incremental input data cannot be null.",
                "input");
        }

        int count = input.HasCount
            ? input.Count
            : input.Data.Length - input.Offset;
        if (!CursorMath.IsRangeValid(input.Offset, count, input.Data.Length))
        {
            throw new System.ArgumentOutOfRangeException("input");
        }

        if (count > int.MaxValue / 8)
        {
            return this.SetTerminalError(
                0,
                "ASNR0008",
                "The incremental PER chunk exceeds the supported bit-count range.",
                true);
        }

        return this.ConsumeInputBits(
            input.Data,
            input.Offset,
            count * 8,
            input.IsFinalBlock,
            count);
    }

    public AsnIncrementalResult Complete()
    {
        if (this.Terminal)
        {
            return this.CopyTerminalResult();
        }

        PerBitReadResult appended = this.Reader.AppendInputBits(
            [],
            0,
            0,
            true);
        if (appended.Status != AsnDecodeStatus.Done)
        {
            return this.SetReaderError(0);
        }

        return this.DecodeBufferedInput(0, true);
    }

    public void Reset()
    {
        this.Reader.ResetReader();
        this.Terminal = false;
        this.TerminalResult = new AsnIncrementalResult();
    }

    internal AsnIncrementalResult ConsumeBits(
        byte[] data,
        int offset,
        int bitCount,
        bool finalBlock)
    {
        int consumedCount = bitCount / 8;
        if (bitCount % 8 != 0)
        {
            consumedCount = consumedCount + 1;
        }

        return this.ConsumeInputBits(
            data,
            offset,
            bitCount,
            finalBlock,
            consumedCount);
    }

    internal AsnIncrementalResult ConsumeInputBits(
        byte[] data,
        int offset,
        int bitCount,
        bool finalBlock,
        int consumedCount)
    {
        if (this.Terminal)
        {
            return this.CreateStateError(
                0,
                "ASNP0097",
                "The incremental PER decoder is terminal until Reset is called.",
                false);
        }

        PerBitReadResult appended = this.Reader.AppendInputBits(
            data,
            offset,
            bitCount,
            finalBlock);
        if (appended.Status != AsnDecodeStatus.Done)
        {
            return this.SetReaderError(0);
        }

        return this.DecodeBufferedInput(consumedCount, finalBlock);
    }

    internal AsnIncrementalResult DecodeBufferedInput(
        int consumedCount,
        bool finalBlock)
    {
        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(this.TypeName);
        if (type == null)
        {
            return this.SetTerminalError(
                consumedCount,
                "ASNP0082",
                "The requested schema type is missing or ambiguous.",
                false);
        }

        this.Reader.Position = 0;
        this.Reader.Resources = new ResourceCounter(this.Limits);
        if (!this.Reader.Resources.TryChargeAllocationBytes(
            this.Reader.Buffer.Length))
        {
            return this.SetTerminalError(
                consumedCount,
                "ASNR0005",
                "Incremental PER decoding exceeds the configured allocation limit.",
                true);
        }

        PerSchemaResult decoded = this.Codec.DecodeType(
            type,
            this.Reader,
            this.Options,
            0);
        if (!decoded.Success)
        {
            if (decoded.Status == AsnDecodeStatus.NeedMoreData
                && !finalBlock)
            {
                return new AsnIncrementalResult
                {
                    Status = AsnDecodeStatus.NeedMoreData,
                    ConsumedCount = consumedCount
                };
            }

            return this.SetDecodedError(
                consumedCount,
                decoded.ErrorCode,
                decoded.ErrorMessage,
                decoded.LimitExceeded);
        }

        if (!finalBlock)
        {
            return new AsnIncrementalResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = consumedCount
            };
        }

        PerBitReadResult complete = this.Reader.CompleteInput(true);
        if (complete.Status != AsnDecodeStatus.Done)
        {
            return this.SetReaderError(consumedCount);
        }

        if (decoded.Value != null && this.Options.ValidateValues)
        {
            AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
                .ValidateRuntimeValue(decoded.Value, type, this.Limits);
            if (diagnostics.Length > 0)
            {
                AsnIncrementalResult invalid = new AsnIncrementalResult
                {
                    Status = AsnDecodeStatus.Error,
                    ConsumedCount = consumedCount,
                    Diagnostics = diagnostics
                };
                this.SetTerminal(invalid);
                return invalid;
            }
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

    internal AsnIncrementalResult SetReaderError(int consumedCount)
    {
        AsnIncrementalResult result = this.SetTerminalError(
            consumedCount,
            this.Reader.ErrorCode,
            this.Reader.ErrorMessage,
            this.Reader.LimitExceeded);
        this.SetResultBitOffset(result);
        return result;
    }

    internal AsnIncrementalResult SetDecodedError(
        int consumedCount,
        string code,
        string message,
        bool limitExceeded)
    {
        AsnIncrementalResult result = this.SetTerminalError(
            consumedCount,
            code,
            message,
            limitExceeded);
        this.SetResultBitOffset(result);
        return result;
    }

    internal void SetResultBitOffset(AsnIncrementalResult result)
    {
        if (result.Diagnostics.Length > 0)
        {
            result.Diagnostics[0].HasBitOffset = true;
            result.Diagnostics[0].BitOffset =
                this.Reader.HasViolationBitOffset
                    ? this.Reader.ViolationBitOffset
                    : this.Reader.Position;
        }
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
