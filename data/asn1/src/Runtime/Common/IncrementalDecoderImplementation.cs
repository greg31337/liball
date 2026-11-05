namespace asn1;

internal enum IncrementalDecoderState
{
    Active = 0,
    Done = 1,
    Error = 2
}

internal class IncrementalDecoderImplementation : IAsnIncrementalDecoder
{
    internal ConfiguredCodec Codec;
    internal string TypeName;
    internal AsnCodecOptions Options;
    internal AsnResourceLimits Limits;
    internal ByteAccumulator Buffer = new ByteAccumulator(0);
    internal ResourceCounter Resources;
    internal IncrementalDecoderState State;
    internal AsnIncrementalResult TerminalResult = new AsnIncrementalResult();

    internal IncrementalDecoderImplementation(
        ConfiguredCodec codec,
        string typeName,
        AsnCodecOptions options)
    {
        this.Codec = codec == null
            ? throw new System.ArgumentNullException("codec")
            : codec;
        this.TypeName = typeName == null
            ? throw new System.ArgumentNullException("typeName")
            : typeName;
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        this.Options = new CodecProviderImplementation().CloneOptions(
            codec.CreateEffectiveOptions(options));
        this.Limits = this.Options.ResourceLimits == null
            ? new AsnResourceLimits()
            : this.Options.ResourceLimits;
        this.Resources = new ResourceCounter(this.Limits);
        this.State = IncrementalDecoderState.Active;
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
        if (this.State != IncrementalDecoderState.Active)
        {
            return this.CreateTerminalStateError();
        }

        int consumedCount = 0;
        AsnIncrementalResult latest = this.EvaluateBufferedInput(
            0,
            input.IsFinalBlock && count == 0);
        if (latest.Status != AsnDecodeStatus.NeedMoreData)
        {
            return latest;
        }

        while (consumedCount < count)
        {
            if (!this.Resources.TryChargeAllocationBytes(1))
            {
                return this.SetTerminalResourceError(consumedCount);
            }

            this.Buffer.AppendByte(input.Data[input.Offset + consumedCount]);
            consumedCount = consumedCount + 1;
            latest = this.EvaluateBufferedInput(
                consumedCount,
                input.IsFinalBlock && consumedCount == count);
            if (latest.Status != AsnDecodeStatus.NeedMoreData)
            {
                return latest;
            }
        }

        return latest;
    }

    public AsnIncrementalResult Complete()
    {
        if (this.State != IncrementalDecoderState.Active)
        {
            return this.CopyTerminalResult();
        }

        return this.EvaluateBufferedInput(0, true);
    }

    public void Reset()
    {
        this.Buffer.Clear();
        this.Resources = new ResourceCounter(this.Limits);
        this.State = IncrementalDecoderState.Active;
        this.TerminalResult = new AsnIncrementalResult();
    }

    internal AsnIncrementalResult EvaluateBufferedInput(
        int consumedCount,
        bool finalBlock)
    {
        AsnDecodeResult decoded = this.Codec.TryDecodeSpan(
            new System.ReadOnlySpan<byte>(
                this.Buffer.Buffer,
                0,
                this.Buffer.Count),
            this.TypeName,
            this.Options);
        if (decoded.Status == AsnDecodeStatus.NeedMoreData)
        {
            if (finalBlock)
            {
                return this.SetTerminalIncompleteError(consumedCount);
            }

            return new AsnIncrementalResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = consumedCount
            };
        }

        AsnIncrementalResult result = new AsnIncrementalResult
        {
            Status = decoded.Status,
            ConsumedCount = consumedCount,
            Value = decoded.Value,
            Diagnostics = decoded.Diagnostics
        };
        this.SetTerminal(result);
        return result;
    }

    internal AsnIncrementalResult SetTerminalIncompleteError(
        int consumedCount)
    {
        AsnIncrementalResult result = new AsnIncrementalResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Decoding,
                    Code = "ASNCODEC0008",
                    Message = "The final incremental input ends before one complete encoded value is available."
                }
            ]
        };
        this.SetTerminal(result);
        return result;
    }

    internal AsnIncrementalResult SetTerminalResourceError(
        int consumedCount)
    {
        AsnIncrementalResult result = new AsnIncrementalResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Resource,
                    Code = "ASNR0005",
                    Message = "Buffering incremental input exceeds the configured allocation limit."
                }
            ]
        };
        this.SetTerminal(result);
        return result;
    }

    internal AsnIncrementalResult CreateTerminalStateError()
    {
        return new AsnIncrementalResult
        {
            Status = AsnDecodeStatus.Error,
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Decoding,
                    Code = "ASNCODEC0009",
                    Message = "The incremental decoder is terminal until Reset is called."
                }
            ]
        };
    }

    internal void SetTerminal(AsnIncrementalResult result)
    {
        this.State = result.Status == AsnDecodeStatus.Done
            ? IncrementalDecoderState.Done
            : IncrementalDecoderState.Error;
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
