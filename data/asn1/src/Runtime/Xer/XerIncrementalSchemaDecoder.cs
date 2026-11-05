namespace asn1;

internal class XerIncrementalSchemaDecoder : IAsnIncrementalDecoder
{
    internal AsnReflectionCatalog Catalog;
    internal string TypeName;
    internal AsnCodecOptions Options;
    internal AsnResourceLimits Limits;
    internal AsnTypeDescriptor? Type;
    internal XerXmlTokenReader Reader;
    internal System.Collections.Generic.List<XerXmlToken> Tokens =
        new System.Collections.Generic.List<XerXmlToken>();
    internal bool Terminal;
    internal AsnIncrementalResult TerminalResult = new AsnIncrementalResult();

    internal XerIncrementalSchemaDecoder(
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
        if (options.EncodingRules != AsnEncodingRules.BasicXer
            && options.EncodingRules != AsnEncodingRules.CanonicalXer
            && options.EncodingRules != AsnEncodingRules.ExtendedXer)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        this.Type = new BerSchemaDecoder(catalog).ResolveRequestedType(
            typeName);
        this.Reader = new XerXmlTokenReader(this.Limits);
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
                "The incremental XER input data cannot be null.",
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
                "ASNXER0404",
                "The incremental XER decoder is terminal until Reset is called.",
                false);
        }

        XerXmlReadResult read = this.Reader.ConsumeXml(input);
        return this.ProcessReadResult(read, count);
    }

    public AsnIncrementalResult Complete()
    {
        if (this.Terminal)
        {
            return this.CopyTerminalResult();
        }

        XerXmlReadResult read = this.Reader.CompleteXml();
        return this.ProcessReadResult(read, 0);
    }

    public void Reset()
    {
        this.Reader.ResetXml();
        this.Tokens.Clear();
        this.Terminal = false;
        this.TerminalResult = new AsnIncrementalResult();
    }

    internal AsnIncrementalResult ProcessReadResult(
        XerXmlReadResult read,
        int consumedCount)
    {
        int index = 0;
        while (index < read.Tokens.Length)
        {
            this.Tokens.Add(read.Tokens[index]);
            index = index + 1;
        }

        if (read.Status == AsnDecodeStatus.NeedMoreData)
        {
            return new AsnIncrementalResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = consumedCount
            };
        }

        if (read.Status == AsnDecodeStatus.Error)
        {
            AsnIncrementalResult failure = this.SetTerminalError(
                consumedCount,
                read.ErrorCode,
                read.ErrorMessage,
                read.LimitExceeded);
            this.SetResultByteOffset(failure, read.ViolationOffset);
            return failure;
        }

        return this.DecodeCompletedTokens(consumedCount);
    }

    internal AsnIncrementalResult DecodeCompletedTokens(int consumedCount)
    {
        if (this.Type == null)
        {
            return this.SetTerminalError(
                consumedCount,
                "ASNXER0402",
                "The requested schema type is missing or ambiguous.",
                false);
        }

        XerValueCodec values = new XerValueCodec(this.Limits);
        values.Extended = this.Options.EncodingRules
            == AsnEncodingRules.ExtendedXer;
        values.UnknownDataPolicy = this.Options.UnknownDataPolicy;
        XerXmlToken[] tokens = this.Tokens.ToArray();
        XerValueResult decoded = values.DecodeValue(
            this.Type,
            tokens,
            0,
            tokens.Length);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.SetValueFailure(consumedCount, decoded);
        }

        if (decoded.ConsumedCount != tokens.Length)
        {
            return this.SetTerminalError(
                consumedCount,
                "ASNXER0403",
                "The XER input contains trailing content.",
                false);
        }

        if (this.Options.EncodingRules == AsnEncodingRules.CanonicalXer)
        {
            XerCanonicalCodec canonical = new XerCanonicalCodec(this.Limits);
            XerValueResult encoded = canonical.EncodeCanonical(
                this.Type,
                decoded.Value);
            if (!encoded.Success)
            {
                return this.SetValueFailure(consumedCount, encoded);
            }

            byte[] expected = canonical.RenderTokens(encoded.Tokens);
            byte[] input = this.Reader.Input.ToArray();
            if (expected.Length == 0
                || !canonical.BytesEqual(input, expected))
            {
                return this.SetTerminalError(
                    consumedCount,
                    "ASNXER0200",
                    "The XER document is valid BASIC-XER but is not in canonical form.",
                    false);
            }
        }

        if (this.Options.ValidateValues)
        {
            AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
                .ValidateRuntimeValue(decoded.Value, this.Type, this.Limits);
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

    internal AsnIncrementalResult SetValueFailure(
        int consumedCount,
        XerValueResult source)
    {
        string code = source.ErrorCode.Length == 0
            ? "ASNXER0405"
            : source.ErrorCode;
        string message = source.ErrorMessage.Length == 0
            ? "The completed XML document is not a value of the requested XER type."
            : source.ErrorMessage;
        AsnIncrementalResult result = this.SetTerminalError(
            consumedCount,
            code,
            message,
            source.LimitExceeded);
        if (result.Diagnostics.Length > 0)
        {
            result.Diagnostics[0].ValuePath = source.ValuePath;
        }

        if (source.HasViolationOffset)
        {
            this.SetResultByteOffset(result, source.ViolationOffset);
        }

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
