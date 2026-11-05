namespace asn1;

internal class BerIncrementalTlvEncoder
{
    internal AsnReflectionCatalog? Catalog;
    internal AsnResourceLimits Limits;
    internal System.Collections.Generic.List<byte[]> Segments =
        new System.Collections.Generic.List<byte[]>();
    internal int SegmentIndex;
    internal int SegmentOffset;
    internal long ExpectedCount;
    internal bool Started;
    internal bool Done;

    internal BerIncrementalTlvEncoder(
        AsnReflectionCatalog? catalog,
        AsnResourceLimits limits)
    {
        this.Catalog = catalog;
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerIncrementalWriteResult StartTlv(BerWriteRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (this.Started && !this.Done)
        {
            return this.CreateTransientWriteError(
                "ASNE0105",
                "The incremental BER encoder already has an active value.",
                false);
        }

        if (request.Events == null)
        {
            throw new System.ArgumentException(
                "The write request events cannot be null.",
                "request");
        }

        if (request.Tlv == null || request.Events.Length > 0)
        {
            return this.CreateWriteError(
                "ASNE0106",
                "Incremental TLV encoding requires one TLV tree.",
                false);
        }

        if (!new BerTlvWriterImplementation(this.Limits).IsBerFamilyRule(
            request.EncodingRules))
        {
            return this.CreateWriteError(
                "ASNE0004",
                "Schema-less TLV writing requires BER, DER, or CER rules.",
                false);
        }

        return this.PrepareTree(request.Tlv, request.EncodingRules);
    }

    internal BerIncrementalWriteResult StartSchema(AsnEncodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (this.Started && !this.Done)
        {
            return this.CreateTransientWriteError(
                "ASNE0105",
                "The incremental BER encoder already has an active value.",
                false);
        }

        if (this.Catalog == null)
        {
            return this.CreateWriteError(
                "ASNE0107",
                "Incremental schema encoding requires a reflection catalog.",
                false);
        }

        if (request.TypeName == null || request.Value == null)
        {
            return this.CreateWriteError(
                "ASNE0108",
                "Incremental schema encoding requires a type name and value.",
                false);
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = AsnEncodingRules.Ber }
            : request.Options;
        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreateWriteError(
                "ASNE0072",
                "The requested schema type is missing or ambiguous.",
                false);
        }

        BerSchemaEncodeResult encoded = new BerSchemaEncoder(this.Catalog)
            .EncodeTlv(type, request.Value, options);
        if (!encoded.Success || encoded.Tlv == null)
        {
            return new BerIncrementalWriteResult
            {
                Diagnostics = encoded.Diagnostics
            };
        }

        return this.PrepareTree(encoded.Tlv, options.EncodingRules);
    }

    internal BerIncrementalWriteResult PrepareTree(
        BerTlv root,
        AsnEncodingRules rules)
    {
        this.ResetEncoder();
        BerTlvWriterImplementation writer = new BerTlvWriterImplementation(
            this.Limits);
        BerTlvSizeResult sizes = writer.CalculateTreeSizes(
            root,
            rules,
            new System.Collections.Generic.Dictionary<BerTlv, long>());
        if (!sizes.Success)
        {
            return this.CreateWriteError(
                sizes.ErrorCode,
                sizes.ErrorMessage,
                sizes.LimitExceeded);
        }

        System.Collections.Generic.List<BerTlvEmitFrame> frames =
            new System.Collections.Generic.List<BerTlvEmitFrame>();
        frames.Add(new BerTlvEmitFrame { Node = root });
        while (frames.Count > 0)
        {
            BerTlvEmitFrame frame = frames[frames.Count - 1];
            BerTlv node = frame.Node;
            if (!frame.HeaderWritten && node.UseEncodedData)
            {
                this.AddSegment(node.EncodedData);
                frames.RemoveAt(frames.Count - 1);
                continue;
            }

            if (!frame.HeaderWritten)
            {
                BerIdentifierWriteResult identifier = new BerIdentifierWriter(
                    this.Limits).WriteIdentifier(node.Tag, node.Constructed);
                BerLengthWriteResult length = new BerLengthWriter(this.Limits)
                    .WriteLength(
                        sizes.Sizes[node].ContentLength,
                        node.IndefiniteLength);
                if (!identifier.Success || !length.Success)
                {
                    return this.CreateWriteError(
                        !identifier.Success
                            ? identifier.ErrorCode
                            : length.ErrorCode,
                        !identifier.Success
                            ? identifier.ErrorMessage
                            : length.ErrorMessage,
                        !identifier.Success
                            ? identifier.LimitExceeded
                            : length.LimitExceeded);
                }

                this.AddSegment(identifier.Data);
                this.AddSegment(length.Data);
                frame.HeaderWritten = true;
                if (!node.Constructed)
                {
                    this.AddSegment(node.Content);
                    frames.RemoveAt(frames.Count - 1);
                    continue;
                }
            }

            if (frame.NextChildIndex < node.Children.Length)
            {
                BerTlv child = node.Children[frame.NextChildIndex];
                frame.NextChildIndex = frame.NextChildIndex + 1;
                frames.Add(new BerTlvEmitFrame { Node = child });
                continue;
            }

            if (node.IndefiniteLength)
            {
                this.AddSegment([0, 0]);
            }

            frames.RemoveAt(frames.Count - 1);
        }

        this.ExpectedCount = sizes.EncodedLength;
        this.Started = true;
        this.Done = this.Segments.Count == 0;
        return new BerIncrementalWriteResult
        {
            Success = true,
            Done = this.Done,
            ExpectedCount = this.ExpectedCount
        };
    }

    internal BerIncrementalWriteResult WriteChunk(System.Span<byte> destination)
    {
        if (!this.Started)
        {
            return this.CreateWriteError(
                "ASNE0109",
                "The incremental BER encoder has not been started.",
                false);
        }

        int written = 0;
        while (written < destination.Length
            && this.SegmentIndex < this.Segments.Count)
        {
            byte[] segment = this.Segments[this.SegmentIndex];
            int available = segment.Length - this.SegmentOffset;
            int count = destination.Length - written;
            if (count > available)
            {
                count = available;
            }

            if (count > 0)
            {
                new System.ReadOnlySpan<byte>(
                    segment,
                    this.SegmentOffset,
                    count).CopyTo(destination.Slice(written, count));
                written = written + count;
                this.SegmentOffset = this.SegmentOffset + count;
            }

            if (this.SegmentOffset == segment.Length)
            {
                this.SegmentIndex = this.SegmentIndex + 1;
                this.SegmentOffset = 0;
            }
        }

        this.Done = this.SegmentIndex == this.Segments.Count;
        return new BerIncrementalWriteResult
        {
            Success = true,
            Done = this.Done,
            WrittenCount = written,
            ExpectedCount = this.ExpectedCount
        };
    }

    internal void ResetEncoder()
    {
        this.Segments.Clear();
        this.SegmentIndex = 0;
        this.SegmentOffset = 0;
        this.ExpectedCount = 0;
        this.Started = false;
        this.Done = false;
    }

    internal void AddSegment(byte[] data)
    {
        if (data.Length > 0)
        {
            this.Segments.Add(data);
        }
    }

    internal BerIncrementalWriteResult CreateWriteError(
        string code,
        string message,
        bool resource)
    {
        return this.CreateTransientWriteError(code, message, resource);
    }

    internal BerIncrementalWriteResult CreateTransientWriteError(
        string code,
        string message,
        bool resource)
    {
        AsnDiagnostic[] diagnostics =
        [
            new AsnDiagnostic
            {
                Severity = AsnDiagnosticSeverity.Error,
                Category = resource
                    ? AsnDiagnosticCategory.Resource
                    : AsnDiagnosticCategory.Encoding,
                Code = code,
                Message = message
            }
        ];
        return new BerIncrementalWriteResult
        {
            Diagnostics = diagnostics
        };
    }
}
