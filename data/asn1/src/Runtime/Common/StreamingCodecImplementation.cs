namespace asn1;

internal class StreamingCodecImplementation : IAsnStreamingCodec
{
    internal ConfiguredCodec Codec;

    internal StreamingCodecImplementation(ConfiguredCodec codec)
    {
        this.Codec = codec == null
            ? throw new System.ArgumentNullException("codec")
            : codec;
    }

    public AsnEncodeResult EncodeStream(
        AsnEncodeRequest request,
        System.IO.Stream output)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (output == null)
        {
            throw new System.ArgumentNullException("output");
        }

        AsnEncodeRequest effective = new AsnEncodeRequest
        {
            TypeName = request.TypeName,
            Value = request.Value,
            Options = this.Codec.CreateEffectiveOptions(request.Options)
        };
        AsnEncodeResult result;
        if (this.Codec.IsBerRule())
        {
            result = new BerSchemaEncoder(this.Codec.Catalog)
                .EncodeStream(effective, output);
        }
        else if (this.Codec.IsPerRule())
        {
            result = new PerSchemaCodec(
                this.Codec.Catalog,
                this.Codec.Rules).EncodeStream(effective, output);
        }
        else if (this.Codec.IsOerRule())
        {
            result = new OerSchemaCodec(
                this.Codec.Catalog,
                this.Codec.Rules).EncodeStream(effective, output);
        }
        else if (this.Codec.IsXerRule())
        {
            result = new XerSchemaCodec(
                this.Codec.Catalog,
                this.Codec.Rules).EncodeStream(effective, output);
        }
        else
        {
            result = this.EncodeBufferedEcn(effective, output);
        }

        result.Data = [];
        return result;
    }

    public AsnDecodeResult DecodeStream(
        System.IO.Stream input,
        string typeName,
        AsnCodecOptions options)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }
        if (typeName == null)
        {
            throw new System.ArgumentNullException("typeName");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        AsnCodecOptions effective = this.Codec.CreateEffectiveOptions(options);
        AsnResourceLimits limits = effective.ResourceLimits == null
            ? new AsnResourceLimits()
            : effective.ResourceLimits;
        ResourceCounter resources = new ResourceCounter(limits);
        ByteAccumulator buffer = new ByteAccumulator(0);
        byte[] unit = new byte[1];
        AsnDecodeResult latest = this.Codec.Rules == AsnEncodingRules.Ecn
            ? new AsnDecodeResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            }
            : this.Codec.TryDecodeSpan(
                System.ReadOnlySpan<byte>.Empty,
                typeName,
                effective);
        if (latest.Status != AsnDecodeStatus.NeedMoreData)
        {
            return this.SetDecodeProgress(
                latest,
                0,
                latest.Status == AsnDecodeStatus.Done);
        }
        bool terminal = false;
        while (!terminal)
        {
            if (!resources.TryChargeAllocationBytes(1))
            {
                return this.CreateStreamResourceFailure(buffer.Count);
            }
            int read = input.Read(unit, 0, 1);
            if (read == 0)
            {
                terminal = true;
            }
            else
            {
                if (read != 1)
                {
                    throw new System.IO.IOException(
                        "The input stream returned an invalid read count.");
                }
                buffer.AppendByte(unit[0]);
                latest = this.Codec.TryDecodeSpan(
                    new System.ReadOnlySpan<byte>(
                        buffer.Buffer,
                        0,
                        buffer.Count),
                    typeName,
                    effective);
                if (latest.Status != AsnDecodeStatus.NeedMoreData)
                {
                    return this.SetDecodeProgress(
                        latest,
                        buffer.Count,
                        latest.Status == AsnDecodeStatus.Done);
                }
            }
        }

        if (latest.Status == AsnDecodeStatus.NeedMoreData)
        {
            latest = this.CreateIncompleteStreamFailure(buffer.Count);
        }
        return this.SetDecodeProgress(latest, buffer.Count, false);
    }

    internal AsnEncodeResult EncodeBufferedEcn(
        AsnEncodeRequest request,
        System.IO.Stream output)
    {
        AsnEncodeResult encoded = this.Codec.Encode(request);
        if (!encoded.Success)
        {
            encoded.StreamProgress = new AsnStreamProgress
            {
                Complete = false
            };
            return encoded;
        }

        this.WriteBytes(output, encoded.Data);
        return new AsnEncodeResult
        {
            Success = true,
            WrittenCount = encoded.WrittenCount,
            StreamProgress = new AsnStreamProgress
            {
                WrittenCount = encoded.WrittenCount,
                HasExpectedCount = true,
                ExpectedCount = encoded.WrittenCount,
                Complete = true
            }
        };
    }

    internal void WriteBytes(System.IO.Stream output, byte[] data)
    {
        int index = 0;
        while (index < data.Length)
        {
            output.Write(data, index, 1);
            index = index + 1;
        }
    }

    internal AsnDecodeResult SetDecodeProgress(
        AsnDecodeResult result,
        int consumedCount,
        bool complete)
    {
        result.ConsumedCount = consumedCount;
        result.StreamProgress = new AsnStreamProgress
        {
            ConsumedCount = consumedCount,
            Complete = complete
        };
        return result;
    }

    internal AsnDecodeResult CreateIncompleteStreamFailure(
        int consumedCount)
    {
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Decoding,
                    Code = "ASNCODEC0007",
                    Message = "The input stream ended before one complete encoded value was available."
                }
            ]
        };
    }

    internal AsnDecodeResult CreateStreamResourceFailure(
        int consumedCount)
    {
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            StreamProgress = new AsnStreamProgress
            {
                ConsumedCount = consumedCount,
                Complete = false
            },
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Resource,
                    Code = "ASNR0005",
                    Message = "Buffering stream input exceeds the configured allocation limit."
                }
            ]
        };
    }
}
