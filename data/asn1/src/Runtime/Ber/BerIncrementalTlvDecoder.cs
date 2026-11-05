namespace asn1;

internal class BerIncrementalTlvDecoder
{
    internal AsnEncodingRules Rules;
    internal bool PreserveEncodedData;
    internal AsnResourceLimits Limits;
    internal ByteAccumulator Input = new ByteAccumulator(0);
    internal bool Terminal;
    internal BerReadResult TerminalResult = new BerReadResult();

    internal BerIncrementalTlvDecoder(
        AsnEncodingRules rules,
        bool preserveEncodedData,
        AsnResourceLimits limits)
    {
        this.Rules = rules;
        this.PreserveEncodedData = preserveEncodedData;
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerReadResult ConsumeChunk(AsnIncrementalInput input)
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

        if (this.Terminal)
        {
            return this.CreateStateError(
                "ASND0110",
                "The incremental BER decoder is terminal until Reset is called.");
        }

        BerReadResult latest = new BerReadResult
        {
            Status = AsnDecodeStatus.NeedMoreData
        };
        int consumed = 0;
        while (consumed < count)
        {
            if (!this.AllowsBufferedByte())
            {
                BerReadResult resource = this.CreateStateError(
                    "ASNR0005",
                    "Incremental BER buffering exceeds the configured byte or allocation limit.");
                resource.ConsumedCount = consumed;
                this.SetTerminal(resource);
                return resource;
            }

            this.Input.AppendByte(input.Data[input.Offset + consumed]);
            consumed = consumed + 1;
            latest = this.ReadBufferedInput();
            if (latest.Status == AsnDecodeStatus.Done)
            {
                BerReadResult canonical = this.CheckCanonicalResult(latest);
                canonical.ConsumedCount = consumed;
                this.SetTerminal(canonical);
                return canonical;
            }

            if (latest.Status == AsnDecodeStatus.Error)
            {
                latest.ConsumedCount = consumed;
                this.SetTerminal(latest);
                return latest;
            }
        }

        latest.ConsumedCount = consumed;
        if (input.IsFinalBlock)
        {
            BerReadResult completed = this.CreateIncompleteError();
            completed.ConsumedCount = consumed;
            this.SetTerminal(completed);
            return completed;
        }

        return latest;
    }

    internal BerReadResult CompleteInput()
    {
        if (this.Terminal)
        {
            return this.CopyTerminalResult();
        }

        BerReadResult latest = this.ReadBufferedInput();
        if (latest.Status == AsnDecodeStatus.Done)
        {
            BerReadResult canonical = this.CheckCanonicalResult(latest);
            canonical.ConsumedCount = 0;
            this.SetTerminal(canonical);
            return canonical;
        }

        if (latest.Status == AsnDecodeStatus.Error)
        {
            latest.ConsumedCount = 0;
            this.SetTerminal(latest);
            return latest;
        }

        BerReadResult incomplete = this.CreateIncompleteError();
        this.SetTerminal(incomplete);
        return incomplete;
    }

    internal void ResetDecoder()
    {
        this.Input.Clear();
        this.Terminal = false;
        this.TerminalResult = new BerReadResult();
    }

    internal BerReadResult ReadBufferedInput()
    {
        return new BerTlvReaderImplementation(this.Limits).Read(
            new BerReadRequest
            {
                Data = this.Input.Buffer,
                HasCount = true,
                Count = this.Input.Count,
                EncodingRules = this.Rules,
                PreserveEncodedData = this.PreserveEncodedData
            });
    }

    internal BerReadResult CheckCanonicalResult(BerReadResult read)
    {
        if (this.Rules == AsnEncodingRules.Ber)
        {
            return read;
        }

        AsnCanonicalityResult canonical = new BerCanonicalValidator(
            this.Limits).ValidateInput(
                this.Input.Buffer,
                0,
                this.Input.Count,
                this.Rules);
        if (canonical.Status == AsnCanonicalityStatus.Canonical)
        {
            return read;
        }

        return new BerReadResult
        {
            Status = AsnDecodeStatus.Error,
            Tlv = read.Tlv,
            Events = read.Events,
            Diagnostics = canonical.Diagnostics
        };
    }

    internal bool AllowsBufferedByte()
    {
        int required = this.Input.Count + 1;
        ResourceCounter resources = new ResourceCounter(this.Limits);
        return resources.AllowsOpenTypeBytes(required)
            && resources.TryChargeAllocationBytes(required);
    }

    internal void SetTerminal(BerReadResult result)
    {
        this.Terminal = true;
        this.TerminalResult = result;
    }

    internal BerReadResult CopyTerminalResult()
    {
        return new BerReadResult
        {
            Status = this.TerminalResult.Status,
            Tlv = this.TerminalResult.Tlv,
            Events = this.TerminalResult.Events,
            Diagnostics = this.TerminalResult.Diagnostics
        };
    }

    internal BerReadResult CreateIncompleteError()
    {
        return this.CreateStateError(
            "ASND0111",
            "The final incremental BER block ends before one complete value.");
    }

    internal BerReadResult CreateStateError(string code, string message)
    {
        return new BerReadResult
        {
            Status = AsnDecodeStatus.Error,
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = code.StartsWith(
                        "ASNR",
                        System.StringComparison.Ordinal)
                        ? AsnDiagnosticCategory.Resource
                        : AsnDiagnosticCategory.Decoding,
                    Code = code,
                    Message = message
                }
            ]
        };
    }
}
