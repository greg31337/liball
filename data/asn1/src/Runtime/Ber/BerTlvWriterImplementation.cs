namespace asn1;

internal class BerTlvWriterImplementation : IBerTlvWriter
{
    internal AsnResourceLimits Limits;

    internal BerTlvWriterImplementation(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    public BerWriteResult Write(BerWriteRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (!this.IsBerFamilyRule(request.EncodingRules))
        {
            return this.CreateWriteError(
                "ASNE0004",
                "Schema-less TLV writing requires BER, DER, or CER rules.",
                false);
        }

        if (request.Events == null)
        {
            throw new System.ArgumentException(
                "The write request events cannot be null.",
                "request");
        }

        if (request.Tlv != null && request.Events.Length > 0)
        {
            return this.CreateWriteError(
                "ASNE0005",
                "A write request cannot supply both a TLV tree and events.",
                false);
        }

        BerTlv? root = request.Tlv;
        System.Collections.Generic.Dictionary<BerTlv, long> expectedLengths =
            new System.Collections.Generic.Dictionary<BerTlv, long>();
        if (root == null)
        {
            BerTlvEventBuildResult built = this.BuildTreeFromEvents(
                request.Events);
            if (!built.Success || built.Root == null)
            {
                return this.CreateWriteError(
                    built.ErrorCode,
                    built.ErrorMessage,
                    false);
            }

            root = built.Root;
            expectedLengths = built.ExpectedLengths;
        }

        BerTlvSizeResult sizes = this.CalculateTreeSizes(
            root,
            request.EncodingRules,
            expectedLengths);
        if (!sizes.Success)
        {
            return this.CreateWriteError(
                sizes.ErrorCode,
                sizes.ErrorMessage,
                sizes.LimitExceeded);
        }

        if (sizes.EncodedLength > int.MaxValue)
        {
            return this.CreateWriteError(
                "ASNR0008",
                "The encoded TLV exceeds the supported output-array range.",
                true);
        }

        ResourceCounter resources = new ResourceCounter(this.Limits);
        if (!resources.TryChargeAllocationBytes(sizes.EncodedLength))
        {
            return this.CreateWriteError(
                "ASNR0005",
                "Writing the TLV exceeds the configured allocation limit.",
                true);
        }

        byte[] data = new byte[(int)sizes.EncodedLength];
        BerWriteResult emitted = this.EmitTree(
            root,
            request.EncodingRules,
            sizes.Sizes,
            data);
        if (!emitted.Success)
        {
            return emitted;
        }

        emitted.Data = data;
        emitted.WrittenCount = data.Length;
        return emitted;
    }

    public BerWriteResult WriteSpan(
        BerWriteRequest request,
        System.Span<byte> destination)
    {
        BerWriteResult result = this.Write(request);
        if (!result.Success)
        {
            return result;
        }

        if (destination.Length < result.Data.Length)
        {
            return this.CreateWriteError(
                "ASNE0006",
                "The destination span is too small for the encoded TLV.",
                false);
        }

        new System.ReadOnlySpan<byte>(result.Data).CopyTo(destination);
        return result;
    }

    public BerWriteResult WriteStream(
        BerWriteRequest request,
        System.IO.Stream output)
    {
        if (output == null)
        {
            throw new System.ArgumentNullException("output");
        }

        BerWriteResult result = this.Write(request);
        if (result.Success)
        {
            int index = 0;
            while (index < result.Data.Length)
            {
                output.Write(result.Data, index, 1);
                index = index + 1;
            }
        }

        return result;
    }

    internal BerTlvEventBuildResult BuildTreeFromEvents(
        BerTlvEvent[] events)
    {
        System.Collections.Generic.List<BerTlvEventFrame> frames =
            new System.Collections.Generic.List<BerTlvEventFrame>();
        System.Collections.Generic.Dictionary<BerTlv, long> expectedLengths =
            new System.Collections.Generic.Dictionary<BerTlv, long>();
        BerTlv? root = null;
        bool ended = false;
        int index = 0;
        while (index < events.Length)
        {
            BerTlvEvent current = events[index];
            if (current == null)
            {
                return this.CreateEventBuildError(
                    "ASNE0007",
                    "The event sequence contains a null event.");
            }

            if (ended)
            {
                return this.CreateEventBuildError(
                    "ASNE0008",
                    "No events can follow the end-of-input event.");
            }

            if (current.Kind == AsnTlvEventKind.EndOfInput)
            {
                if (frames.Count != 0 || root == null)
                {
                    return this.CreateEventBuildError(
                        "ASNE0009",
                        "The end-of-input event appears before the root value is complete.");
                }

                ended = true;
            }
            else if (current.Kind == AsnTlvEventKind.Primitive)
            {
                if (current.Tag == null
                    || current.Data == null
                    || !current.HasLength
                    || current.Length != current.Data.Length)
                {
                    return this.CreateEventBuildError(
                        "ASNE0010",
                        "A primitive event requires a tag and an exact definite data length.");
                }

                BerTlv primitive = new BerTlv
                {
                    Tag = current.Tag,
                    Content = current.Data
                };
                BerTlvEventBuildResult? attached = this.AttachEventNode(
                    primitive,
                    frames,
                    root,
                    expectedLengths);
                if (attached != null)
                {
                    return attached;
                }

                if (root == null)
                {
                    root = primitive;
                }
            }
            else if (current.Kind == AsnTlvEventKind.StartConstructed)
            {
                if (current.Tag == null
                    || (current.HasLength && current.Length < 0))
                {
                    return this.CreateEventBuildError(
                        "ASNE0011",
                        "A constructed-start event requires a tag and a valid length form.");
                }

                BerTlv constructed = new BerTlv
                {
                    Tag = current.Tag,
                    Constructed = true,
                    IndefiniteLength = !current.HasLength
                };
                BerTlvEventBuildResult? attached = this.AttachEventNode(
                    constructed,
                    frames,
                    root,
                    expectedLengths);
                if (attached != null)
                {
                    return attached;
                }

                if (root == null)
                {
                    root = constructed;
                }

                if (current.HasLength)
                {
                    expectedLengths.Add(constructed, current.Length);
                }

                frames.Add(new BerTlvEventFrame
                {
                    Node = constructed
                });
            }
            else if (current.Kind == AsnTlvEventKind.EndConstructed)
            {
                if (frames.Count == 0)
                {
                    return this.CreateEventBuildError(
                        "ASNE0012",
                        "A constructed-end event has no matching start event.");
                }

                BerTlvEventFrame frame = frames[frames.Count - 1];
                if (current.Tag != null
                    && !this.TagsEqual(current.Tag, frame.Node.Tag))
                {
                    return this.CreateEventBuildError(
                        "ASNE0013",
                        "The constructed-end tag does not match its start tag.");
                }

                frame.Node.Children = frame.Children.ToArray();
                frames.RemoveAt(frames.Count - 1);
            }
            else
            {
                return this.CreateEventBuildError(
                    "ASNE0014",
                    "The event sequence contains an unsupported event kind.");
            }

            index = index + 1;
        }

        if (root == null || frames.Count != 0)
        {
            return this.CreateEventBuildError(
                "ASNE0015",
                "The event sequence does not contain one complete root value.");
        }

        return new BerTlvEventBuildResult
        {
            Success = true,
            Root = root,
            ExpectedLengths = expectedLengths
        };
    }

    internal BerTlvEventBuildResult? AttachEventNode(
        BerTlv node,
        System.Collections.Generic.List<BerTlvEventFrame> frames,
        BerTlv? root,
        System.Collections.Generic.Dictionary<BerTlv, long> expectedLengths)
    {
        if (frames.Count == 0)
        {
            if (root != null)
            {
                return this.CreateEventBuildError(
                    "ASNE0016",
                    "The event sequence contains more than one root value.");
            }

            return null;
        }

        BerTlvEventFrame parent = frames[frames.Count - 1];
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            parent.Children.Count + 1))
        {
            return this.CreateEventBuildError(
                "ASNR0007",
                "The event sequence exceeds the configured child-count limit.");
        }

        parent.Children.Add(node);
        return null;
    }

    internal BerTlvSizeResult CalculateTreeSizes(
        BerTlv root,
        AsnEncodingRules rules,
        System.Collections.Generic.Dictionary<BerTlv, long> expectedLengths)
    {
        BerTlvSizeResult result = new BerTlvSizeResult();
        ResourceCounter resources = new ResourceCounter(this.Limits);
        System.Collections.Generic.List<BerTlvSizeFrame> frames =
            new System.Collections.Generic.List<BerTlvSizeFrame>();
        frames.Add(new BerTlvSizeFrame { Node = root });
        while (frames.Count > 0)
        {
            BerTlvSizeFrame frame = frames[frames.Count - 1];
            BerTlv node = frame.Node;
            if (node == null
                || node.Tag == null
                || node.Content == null
                || node.Children == null
                || node.EncodedData == null)
            {
                return this.CreateSizeError(
                    "ASNE0017",
                    "The TLV tree contains a null node or field.",
                    false);
            }

            if (node.UseEncodedData)
            {
                if (rules != AsnEncodingRules.Ber
                    || node.EncodedData.Length == 0)
                {
                    return this.CreateSizeError(
                        "ASNE0098",
                        "Exact TLV slices can only be re-emitted as BER.",
                        false);
                }

                if (!resources.AllowsOpenTypeBytes(node.EncodedData.Length))
                {
                    return this.CreateSizeError(
                        "ASNR0001",
                        "The exact TLV slice exceeds the configured byte limit.",
                        true);
                }

                BerTlvSizeInfo exactInfo = new BerTlvSizeInfo
                {
                    EncodedLength = node.EncodedData.Length
                };
                result.Sizes.Add(node, exactInfo);
                frames.RemoveAt(frames.Count - 1);
                this.AddCompletedSizeToParent(
                    frames,
                    exactInfo.EncodedLength);
                continue;
            }

            if (!node.Constructed)
            {
                if (node.IndefiniteLength || node.Children.Length != 0)
                {
                    return this.CreateSizeError(
                        "ASNE0018",
                        "A primitive TLV cannot be indefinite or contain child nodes.",
                        false);
                }

                if (!resources.AllowsOpenTypeBytes(node.Content.Length))
                {
                    return this.CreateSizeError(
                        "ASNR0006",
                        "Primitive TLV content exceeds the configured byte limit.",
                        true);
                }

                BerTlvSizeResult primitive = this.CalculateNodeSize(
                    node,
                    node.Content.Length,
                    rules);
                if (!primitive.Success)
                {
                    return primitive;
                }

                result.Sizes.Add(node, primitive.Sizes[node]);
                frames.RemoveAt(frames.Count - 1);
                this.AddCompletedSizeToParent(frames, primitive.EncodedLength);
                continue;
            }

            if (node.Content.Length != 0)
            {
                return this.CreateSizeError(
                    "ASNE0019",
                    "A constructed TLV cannot contain primitive content bytes.",
                    false);
            }

            if (node.IndefiniteLength && rules == AsnEncodingRules.Der)
            {
                return this.CreateSizeError(
                    "ASNE0020",
                    "DER does not permit indefinite length.",
                    false);
            }

            if (!resources.AllowsCollectionElements(node.Children.Length))
            {
                return this.CreateSizeError(
                    "ASNR0007",
                    "The constructed TLV exceeds the configured child-count limit.",
                    true);
            }

            if (!frame.NestingCharged)
            {
                if (!resources.TryEnterNesting())
                {
                    return this.CreateSizeError(
                        "ASNR0004",
                        "The BER nesting depth exceeds the configured limit.",
                        true);
                }

                frame.NestingCharged = true;
            }

            if (frame.NextChildIndex < node.Children.Length)
            {
                BerTlv child = node.Children[frame.NextChildIndex];
                frame.NextChildIndex = frame.NextChildIndex + 1;
                if (child == null
                    || result.Sizes.ContainsKey(child)
                    || this.ContainsSizeFrameNode(frames, child))
                {
                    return this.CreateSizeError(
                        "ASNE0021",
                        "The TLV tree contains a null, shared, or cyclic child node.",
                        false);
                }

                frames.Add(new BerTlvSizeFrame { Node = child });
                continue;
            }

            BerTlvSizeResult constructed = this.CalculateNodeSize(
                node,
                frame.ContentLength,
                rules);
            if (!constructed.Success)
            {
                return constructed;
            }

            if (expectedLengths.ContainsKey(node)
                && expectedLengths[node] != frame.ContentLength)
            {
                return this.CreateSizeError(
                    "ASNE0022",
                    "A constructed event length does not match its encoded children.",
                    false);
            }

            result.Sizes.Add(node, constructed.Sizes[node]);
            resources.LeaveNesting();
            frames.RemoveAt(frames.Count - 1);
            this.AddCompletedSizeToParent(frames, constructed.EncodedLength);
        }

        result.Success = true;
        result.EncodedLength = result.Sizes[root].EncodedLength;
        return result;
    }

    internal BerTlvSizeResult CalculateNodeSize(
        BerTlv node,
        long contentLength,
        AsnEncodingRules rules)
    {
        BerIdentifierWriteResult identifier = new BerIdentifierWriter(
            this.Limits).WriteIdentifier(node.Tag, node.Constructed);
        if (!identifier.Success)
        {
            return this.CreateSizeError(
                identifier.ErrorCode,
                identifier.ErrorMessage,
                identifier.LimitExceeded);
        }

        BerLengthWriteResult length = new BerLengthWriter(this.Limits)
            .WriteLength(contentLength, node.IndefiniteLength);
        if (!length.Success)
        {
            return this.CreateSizeError(
                length.ErrorCode,
                length.ErrorMessage,
                length.LimitExceeded);
        }

        long overhead = identifier.WrittenCount + length.WrittenCount;
        if (node.IndefiniteLength)
        {
            overhead = overhead + 2;
        }

        if (contentLength > long.MaxValue - overhead)
        {
            return this.CreateSizeError(
                "ASNR0008",
                "The encoded TLV size overflows the supported Int64 range.",
                true);
        }

        BerTlvSizeInfo info = new BerTlvSizeInfo
        {
            ContentLength = contentLength,
            EncodedLength = contentLength + overhead
        };
        BerTlvSizeResult result = new BerTlvSizeResult
        {
            Success = true,
            EncodedLength = info.EncodedLength
        };
        result.Sizes.Add(node, info);
        return result;
    }

    internal void AddCompletedSizeToParent(
        System.Collections.Generic.List<BerTlvSizeFrame> frames,
        long encodedLength)
    {
        if (frames.Count == 0)
        {
            return;
        }

        BerTlvSizeFrame parent = frames[frames.Count - 1];
        if (parent.ContentLength > long.MaxValue - encodedLength)
        {
            parent.ContentLength = long.MaxValue;
        }
        else
        {
            parent.ContentLength = parent.ContentLength + encodedLength;
        }
    }

    internal bool ContainsSizeFrameNode(
        System.Collections.Generic.List<BerTlvSizeFrame> frames,
        BerTlv candidate)
    {
        int index = 0;
        while (index < frames.Count)
        {
            if (frames[index].Node == candidate)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal BerWriteResult EmitTree(
        BerTlv root,
        AsnEncodingRules rules,
        System.Collections.Generic.Dictionary<BerTlv, BerTlvSizeInfo> sizes,
        byte[] destination)
    {
        System.Collections.Generic.List<BerTlvEmitFrame> frames =
            new System.Collections.Generic.List<BerTlvEmitFrame>();
        frames.Add(new BerTlvEmitFrame { Node = root });
        int offset = 0;
        while (frames.Count > 0)
        {
            BerTlvEmitFrame frame = frames[frames.Count - 1];
            BerTlv node = frame.Node;
            if (!frame.HeaderWritten && node.UseEncodedData)
            {
                System.Buffer.BlockCopy(
                    node.EncodedData,
                    0,
                    destination,
                    offset,
                    node.EncodedData.Length);
                offset = offset + node.EncodedData.Length;
                frames.RemoveAt(frames.Count - 1);
                continue;
            }

            if (!frame.HeaderWritten)
            {
                BerIdentifierWriteResult identifier = new BerIdentifierWriter(
                    this.Limits).WriteIdentifier(node.Tag, node.Constructed);
                BerLengthWriteResult length = new BerLengthWriter(this.Limits)
                    .WriteLength(
                        sizes[node].ContentLength,
                        node.IndefiniteLength);
                System.Buffer.BlockCopy(
                    identifier.Data,
                    0,
                    destination,
                    offset,
                    identifier.Data.Length);
                offset = offset + identifier.Data.Length;
                System.Buffer.BlockCopy(
                    length.Data,
                    0,
                    destination,
                    offset,
                    length.Data.Length);
                offset = offset + length.Data.Length;
                frame.HeaderWritten = true;
                if (!node.Constructed)
                {
                    System.Buffer.BlockCopy(
                        node.Content,
                        0,
                        destination,
                        offset,
                        node.Content.Length);
                    offset = offset + node.Content.Length;
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
                destination[offset] = 0;
                destination[offset + 1] = 0;
                offset = offset + 2;
            }

            frames.RemoveAt(frames.Count - 1);
        }

        if (offset != destination.Length)
        {
            return this.CreateWriteError(
                "ASNE0023",
                "The emitted TLV size differs from the validated size.",
                false);
        }

        return new BerWriteResult
        {
            Success = true
        };
    }

    internal bool IsBerFamilyRule(AsnEncodingRules rules)
    {
        return rules == AsnEncodingRules.Ber
            || rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer;
    }

    internal bool TagsEqual(AsnTag left, AsnTag right)
    {
        return left.Class == right.Class && left.Number == right.Number;
    }

    internal BerTlvEventBuildResult CreateEventBuildError(
        string code,
        string message)
    {
        return new BerTlvEventBuildResult
        {
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal BerTlvSizeResult CreateSizeError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new BerTlvSizeResult
        {
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal BerWriteResult CreateWriteError(
        string code,
        string message,
        bool resource)
    {
        return new BerWriteResult
        {
            Diagnostics =
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
            ]
        };
    }
}
