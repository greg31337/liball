namespace asn1;

internal class BerTlvReaderImplementation : IBerTlvReader
{
    internal AsnResourceLimits Limits;

    internal BerTlvReaderImplementation(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    public BerReadResult Read(BerReadRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Data == null)
        {
            throw new System.ArgumentException(
                "The read request data cannot be null.",
                "request");
        }

        int count = request.HasCount
            ? request.Count
            : request.Data.Length - request.Offset;
        if (!CursorMath.IsRangeValid(
            request.Offset,
            count,
            request.Data.Length))
        {
            throw new System.ArgumentOutOfRangeException("request");
        }

        return this.ReadRegion(
            new System.ReadOnlySpan<byte>(
                request.Data,
                request.Offset,
                count),
            request.EncodingRules,
            request.PreserveEncodedData,
            request.Offset);
    }

    public BerReadResult ReadSpan(
        System.ReadOnlySpan<byte> input,
        AsnEncodingRules rules)
    {
        return this.ReadRegion(input, rules, false, 0);
    }

    public BerReadResult ReadStream(
        System.IO.Stream input,
        AsnEncodingRules rules)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        ByteAccumulator bytes = new ByteAccumulator(8192);
        ResourceCounter resources = new ResourceCounter(this.Limits);
        byte[] chunk = new byte[8192];
        int count = input.Read(chunk, 0, chunk.Length);
        while (count > 0)
        {
            if (!resources.TryChargeAllocationBytes(count))
            {
                return this.CreateReadError(
                    0,
                    "ASNR0005",
                    "Reading the BER stream exceeds the configured allocation limit.");
            }

            bytes.AppendSegment(chunk, 0, count);
            count = input.Read(chunk, 0, chunk.Length);
        }

        return this.ReadRegion(
            new System.ReadOnlySpan<byte>(bytes.Buffer, 0, bytes.Count),
            rules,
            false,
            0);
    }

    internal BerReadResult ReadRegion(
        System.ReadOnlySpan<byte> input,
        AsnEncodingRules rules,
        bool preserveEncodedData,
        int baseOffset)
    {
        if (!this.IsBerFamilyRule(rules))
        {
            return this.CreateReadError(
                0,
                "ASND0013",
                "Schema-less TLV reading requires BER, DER, or CER rules.");
        }

        bool requireMinimal = rules != AsnEncodingRules.Ber;
        BerIdentifierReader identifiers = new BerIdentifierReader(this.Limits);
        BerLengthReader lengths = new BerLengthReader(this.Limits);
        ResourceCounter resources = new ResourceCounter(this.Limits);
        System.Collections.Generic.List<BerTlvReadFrame> frames =
            new System.Collections.Generic.List<BerTlvReadFrame>();
        System.Collections.Generic.List<BerTlvEvent> events =
            new System.Collections.Generic.List<BerTlvEvent>();
        BerTlv? root = null;
        int position = 0;
        while (true)
        {
            if (frames.Count > 0)
            {
                BerTlvReadFrame top = frames[frames.Count - 1];
                if (!top.Indefinite && position == top.EndOffset)
                {
                    if (!this.CompleteReadFrame(
                        frames,
                        events,
                        input,
                        position,
                        preserveEncodedData,
                        baseOffset,
                        resources))
                    {
                        return this.CreateLowLevelError(
                            root,
                            frames,
                            events,
                            position,
                            baseOffset,
                            "ASNR0005",
                            "Retaining encoded TLV data exceeds the configured byte or allocation limit.",
                            true);
                    }
                    if (frames.Count == 0 && root != null)
                    {
                        return this.CreateReadSuccess(
                            root,
                            events,
                            position,
                            baseOffset);
                    }

                    continue;
                }

                if (!top.Indefinite && position > top.EndOffset)
                {
                    return this.CreateStructuralError(
                        root,
                        frames,
                        events,
                        position,
                        baseOffset,
                        "ASND0014",
                        "Constructed content exceeds its definite boundary.");
                }

                if (top.Indefinite
                    && !this.CanFitEndOfContents(frames, position))
                {
                    return this.CreateStructuralError(
                        root,
                        frames,
                        events,
                        position,
                        baseOffset,
                        "ASND0024",
                        "An indefinite value has no room for end-of-contents within an enclosing definite length.");
                }

                if (top.Indefinite && position < input.Length
                    && input[position] == 0)
                {
                    BerEndOfContentsResult eoc = lengths.ReadEndOfContents(
                        input.Slice(position));
                    if (eoc.Status == AsnDecodeStatus.NeedMoreData)
                    {
                        return this.CreateNeedMoreResult(
                            root,
                            frames,
                            events,
                            position,
                            baseOffset);
                    }

                    if (eoc.Status == AsnDecodeStatus.Error)
                    {
                        return this.CreateLowLevelError(
                            root,
                            frames,
                            events,
                            position + eoc.ConsumedCount,
                            baseOffset,
                            eoc.ErrorCode,
                            eoc.ErrorMessage,
                            eoc.LimitExceeded);
                    }

                    position = position + eoc.ConsumedCount;
                    if (!this.CompleteReadFrame(
                        frames,
                        events,
                        input,
                        position,
                        preserveEncodedData,
                        baseOffset,
                        resources))
                    {
                        return this.CreateLowLevelError(
                            root,
                            frames,
                            events,
                            position,
                            baseOffset,
                            "ASNR0005",
                            "Retaining encoded TLV data exceeds the configured byte or allocation limit.",
                            true);
                    }
                    if (frames.Count == 0 && root != null)
                    {
                        return this.CreateReadSuccess(
                            root,
                            events,
                            position,
                            baseOffset);
                    }

                    continue;
                }
            }

            if (position >= input.Length)
            {
                return this.CreateNeedMoreResult(
                    root,
                    frames,
                    events,
                    position,
                    baseOffset);
            }

            int availableEnd = input.Length;
            if (frames.Count > 0)
            {
                BerTlvReadFrame parent = frames[frames.Count - 1];
                if (!parent.Indefinite && parent.EndOffset < availableEnd)
                {
                    availableEnd = (int)parent.EndOffset;
                }
            }

            BerIdentifierReadResult identifier = identifiers.ReadIdentifier(
                input.Slice(position, availableEnd - position),
                requireMinimal);
            if (identifier.Status == AsnDecodeStatus.NeedMoreData)
            {
                if (this.IsDefiniteBoundary(frames, availableEnd))
                {
                    return this.CreateStructuralError(
                        root,
                        frames,
                        events,
                        position,
                        baseOffset,
                        "ASND0015",
                        "An identifier crosses an enclosing definite boundary.");
                }

                return this.CreateNeedMoreResult(
                    root,
                    frames,
                    events,
                    position,
                    baseOffset);
            }

            if (identifier.Status == AsnDecodeStatus.Error
                || identifier.Tag == null)
            {
                return this.CreateLowLevelError(
                    root,
                    frames,
                    events,
                    position + identifier.ConsumedCount,
                    baseOffset,
                    identifier.ErrorCode,
                    identifier.ErrorMessage,
                    identifier.LimitExceeded);
            }

            int lengthOffset = position + identifier.ConsumedCount;
            BerLengthReadResult length = lengths.ReadLength(
                input.Slice(lengthOffset, availableEnd - lengthOffset),
                requireMinimal);
            if (length.Status == AsnDecodeStatus.NeedMoreData)
            {
                if (this.IsDefiniteBoundary(frames, availableEnd))
                {
                    return this.CreateStructuralError(
                        root,
                        frames,
                        events,
                        lengthOffset,
                        baseOffset,
                        "ASND0016",
                        "A length determinant crosses an enclosing definite boundary.");
                }

                return this.CreateNeedMoreResult(
                    root,
                    frames,
                    events,
                    position,
                    baseOffset);
            }

            if (length.Status == AsnDecodeStatus.Error)
            {
                return this.CreateLowLevelError(
                    root,
                    frames,
                    events,
                    lengthOffset + length.ConsumedCount,
                    baseOffset,
                    length.ErrorCode,
                    length.ErrorMessage,
                    length.LimitExceeded);
            }

            int headerEnd = lengthOffset + length.ConsumedCount;
            if (input[position] == 0)
            {
                return this.CreateStructuralError(
                    root,
                    frames,
                    events,
                    headerEnd,
                    baseOffset,
                    "ASND0017",
                    "An end-of-contents marker appears outside an indefinite scope.");
            }

            if (length.Indefinite && !identifier.Constructed)
            {
                return this.CreateStructuralError(
                    root,
                    frames,
                    events,
                    headerEnd,
                    baseOffset,
                    "ASND0018",
                    "Primitive values cannot use indefinite length.");
            }

            if (length.Indefinite && rules == AsnEncodingRules.Der)
            {
                return this.CreateStructuralError(
                    root,
                    frames,
                    events,
                    headerEnd,
                    baseOffset,
                    "ASND0019",
                    "DER does not permit indefinite length.");
            }

            if (!this.CanFitParentBoundary(frames, headerEnd, length))
            {
                return this.CreateStructuralError(
                    root,
                    frames,
                    events,
                    headerEnd,
                    baseOffset,
                    "ASND0020",
                    "The child value exceeds an enclosing definite length.");
            }

            BerTlv node = new BerTlv
            {
                Tag = identifier.Tag,
                Constructed = identifier.Constructed,
                IndefiniteLength = length.Indefinite,
                EncodedOffset = baseOffset + position
            };
            if (!resources.TryChargeAllocationBytes(64))
            {
                return this.CreateLowLevelError(
                    root,
                    frames,
                    events,
                    position,
                    baseOffset,
                    "ASNR0005",
                    "Reading the TLV tree exceeds the configured allocation limit.",
                    true);
            }

            BerReadResult? attachmentError = this.AttachReadNode(
                node,
                frames,
                root,
                position,
                baseOffset);
            if (attachmentError != null)
            {
                return attachmentError;
            }

            if (root == null)
            {
                root = node;
            }

            if (!identifier.Constructed)
            {
                if (length.Length > int.MaxValue
                    || length.Length > input.Length - headerEnd)
                {
                    return this.CreateNeedMoreResult(
                        root,
                        frames,
                        events,
                        position,
                        baseOffset);
                }

                int contentLength = (int)length.Length;
                if (!resources.AllowsOpenTypeBytes(contentLength)
                    || !resources.TryChargeAllocationBytes(contentLength))
                {
                    return this.CreateLowLevelError(
                        root,
                        frames,
                        events,
                        position,
                        baseOffset,
                        "ASNR0006",
                        "Primitive TLV content exceeds the configured byte limit.",
                        true);
                }

                node.Content = input.Slice(headerEnd, contentLength).ToArray();
                int nodeEnd = headerEnd + contentLength;
                if (preserveEncodedData)
                {
                    int encodedLength = nodeEnd - position;
                    if (!resources.AllowsOpenTypeBytes(encodedLength)
                        || !resources.TryChargeAllocationBytes(encodedLength))
                    {
                        return this.CreateLowLevelError(
                            root,
                            frames,
                            events,
                            position,
                            baseOffset,
                            "ASNR0005",
                            "Retaining encoded TLV data exceeds the configured allocation limit.",
                            true);
                    }

                    node.EncodedData = input.Slice(
                        position,
                        encodedLength).ToArray();
                }

                events.Add(new BerTlvEvent
                {
                    Kind = AsnTlvEventKind.Primitive,
                    Tag = node.Tag,
                    Offset = baseOffset + position,
                    HasLength = true,
                    Length = contentLength,
                    Data = node.Content
                });
                position = nodeEnd;
                if (frames.Count == 0)
                {
                    return this.CreateReadSuccess(
                        root,
                        events,
                        position,
                        baseOffset);
                }

                continue;
            }

            if (!resources.TryEnterNesting())
            {
                return this.CreateLowLevelError(
                    root,
                    frames,
                    events,
                    position,
                    baseOffset,
                    "ASNR0004",
                    "The BER nesting depth exceeds the configured limit.",
                    true);
            }

            long endOffset = length.Indefinite
                ? 0
                : headerEnd + length.Length;
            frames.Add(new BerTlvReadFrame
            {
                Node = node,
                StartOffset = position,
                EndOffset = endOffset,
                DeclaredLength = length.Length,
                Indefinite = length.Indefinite
            });
            events.Add(new BerTlvEvent
            {
                Kind = AsnTlvEventKind.StartConstructed,
                Tag = node.Tag,
                Offset = baseOffset + position,
                HasLength = !length.Indefinite,
                Length = length.Length
            });
            position = headerEnd;
        }
    }

    internal bool IsBerFamilyRule(AsnEncodingRules rules)
    {
        return rules == AsnEncodingRules.Ber
            || rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer;
    }

    internal bool CanFitParentBoundary(
        System.Collections.Generic.List<BerTlvReadFrame> frames,
        int headerEnd,
        BerLengthReadResult length)
    {
        if (frames.Count == 0)
        {
            return true;
        }

        BerTlvReadFrame parent = frames[frames.Count - 1];
        if (parent.Indefinite)
        {
            return true;
        }

        if (headerEnd > parent.EndOffset)
        {
            return false;
        }

        if (length.Indefinite)
        {
            return parent.EndOffset - headerEnd >= 2;
        }

        return length.Length <= parent.EndOffset - headerEnd;
    }

    internal bool CanFitEndOfContents(
        System.Collections.Generic.List<BerTlvReadFrame> frames,
        int position)
    {
        int index = 0;
        while (index < frames.Count - 1)
        {
            BerTlvReadFrame frame = frames[index];
            if (!frame.Indefinite && frame.EndOffset - position < 2)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsDefiniteBoundary(
        System.Collections.Generic.List<BerTlvReadFrame> frames,
        int availableEnd)
    {
        if (frames.Count == 0)
        {
            return false;
        }

        BerTlvReadFrame frame = frames[frames.Count - 1];
        return !frame.Indefinite && frame.EndOffset == availableEnd;
    }

    internal BerReadResult? AttachReadNode(
        BerTlv node,
        System.Collections.Generic.List<BerTlvReadFrame> frames,
        BerTlv? root,
        int position,
        int baseOffset)
    {
        if (frames.Count == 0)
        {
            if (root != null)
            {
                return this.CreateReadError(
                    position,
                    "ASND0021",
                    "Only one root TLV value can be read at a time.");
            }

            return null;
        }

        BerTlvReadFrame parent = frames[frames.Count - 1];
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            parent.Children.Count + 1))
        {
            return this.CreateReadError(
                position,
                "ASNR0007",
                "The constructed value exceeds the configured child-count limit.");
        }

        parent.Children.Add(node);
        return null;
    }

    internal bool CompleteReadFrame(
        System.Collections.Generic.List<BerTlvReadFrame> frames,
        System.Collections.Generic.List<BerTlvEvent> events,
        System.ReadOnlySpan<byte> input,
        int position,
        bool preserveEncodedData,
        int baseOffset,
        ResourceCounter resources)
    {
        BerTlvReadFrame frame = frames[frames.Count - 1];
        if (preserveEncodedData)
        {
            int encodedLength = position - frame.StartOffset;
            if (!resources.AllowsOpenTypeBytes(encodedLength)
                || !resources.TryChargeAllocationBytes(encodedLength))
            {
                return false;
            }

            frame.Node.EncodedData = input.Slice(
                frame.StartOffset,
                encodedLength).ToArray();
        }

        frame.Node.Children = frame.Children.ToArray();
        events.Add(new BerTlvEvent
        {
            Kind = AsnTlvEventKind.EndConstructed,
            Tag = frame.Node.Tag,
            Offset = baseOffset + position,
            HasLength = !frame.Indefinite,
            Length = frame.DeclaredLength
        });
        frames.RemoveAt(frames.Count - 1);
        resources.LeaveNesting();
        return true;
    }

    internal BerReadResult CreateReadSuccess(
        BerTlv root,
        System.Collections.Generic.List<BerTlvEvent> events,
        int consumedCount,
        int baseOffset)
    {
        events.Add(new BerTlvEvent
        {
            Kind = AsnTlvEventKind.EndOfInput,
            Offset = baseOffset + consumedCount
        });
        return new BerReadResult
        {
            Status = AsnDecodeStatus.Done,
            ConsumedCount = consumedCount,
            Tlv = root,
            Events = events.ToArray()
        };
    }

    internal BerReadResult CreateNeedMoreResult(
        BerTlv? root,
        System.Collections.Generic.List<BerTlvReadFrame> frames,
        System.Collections.Generic.List<BerTlvEvent> events,
        int consumedCount,
        int baseOffset)
    {
        this.SnapshotReadFrames(frames);
        return new BerReadResult
        {
            Status = AsnDecodeStatus.NeedMoreData,
            ConsumedCount = consumedCount,
            Tlv = root,
            Events = events.ToArray()
        };
    }

    internal BerReadResult CreateStructuralError(
        BerTlv? root,
        System.Collections.Generic.List<BerTlvReadFrame> frames,
        System.Collections.Generic.List<BerTlvEvent> events,
        int consumedCount,
        int baseOffset,
        string code,
        string message)
    {
        this.SnapshotReadFrames(frames);
        return new BerReadResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            Tlv = root,
            Events = events.ToArray(),
            Diagnostics = [this.CreateDecodeDiagnostic(
                code,
                message,
                baseOffset + consumedCount,
                false)]
        };
    }

    internal BerReadResult CreateLowLevelError(
        BerTlv? root,
        System.Collections.Generic.List<BerTlvReadFrame> frames,
        System.Collections.Generic.List<BerTlvEvent> events,
        int consumedCount,
        int baseOffset,
        string code,
        string message,
        bool limitExceeded)
    {
        this.SnapshotReadFrames(frames);
        return new BerReadResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            Tlv = root,
            Events = events.ToArray(),
            Diagnostics = [this.CreateDecodeDiagnostic(
                code,
                message,
                baseOffset + consumedCount,
                limitExceeded)]
        };
    }

    internal BerReadResult CreateReadError(
        int consumedCount,
        string code,
        string message)
    {
        return new BerReadResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            Diagnostics = [this.CreateDecodeDiagnostic(
                code,
                message,
                consumedCount,
                code.StartsWith("ASNR", System.StringComparison.Ordinal))]
        };
    }

    internal AsnDiagnostic CreateDecodeDiagnostic(
        string code,
        string message,
        int offset,
        bool resource)
    {
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = resource
                ? AsnDiagnosticCategory.Resource
                : AsnDiagnosticCategory.Decoding,
            Code = code,
            Message = message + " Byte offset: "
                + offset.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "."
        };
    }

    internal void SnapshotReadFrames(
        System.Collections.Generic.List<BerTlvReadFrame> frames)
    {
        int index = 0;
        while (index < frames.Count)
        {
            frames[index].Node.Children = frames[index].Children.ToArray();
            index = index + 1;
        }
    }
}
