using System;

namespace raw;

/// <summary>Provides content probing, document opening, generic raw decoding, complete rendering, and validation.</summary>
internal static class RawCodec
{
    /// <summary>Probes a bounded memory input without decoding image payloads.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="fileName">An optional filename used only for mismatch diagnostics.</param>
    /// <param name="expectedFormat">The caller-expected format, or <see cref="RawFormat.Unknown"/>.</param>
    /// <param name="genericDescriptorProvided">Whether an authoritative generic descriptor is available.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>The content-based probe result.</returns>
    public static RawProbe Probe(System.ReadOnlyMemory<byte> source, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        return FormatProbe.Probe(source, fileName, expectedFormat, genericDescriptorProvided, options.Limits, options.CancellationToken);
    }

    /// <summary>Probes a bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="fileName">An optional filename used only for mismatch diagnostics.</param>
    /// <param name="expectedFormat">The caller-expected format, or <see cref="RawFormat.Unknown"/>.</param>
    /// <param name="genericDescriptorProvided">Whether an authoritative generic descriptor is available.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>The content-based probe result.</returns>
    public static RawProbe Probe(System.IO.Stream source, long offset, long length, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        return FormatProbe.Probe(source, offset, length, fileName, expectedFormat, genericDescriptorProvided, options.Limits, options.CancellationToken);
    }

    /// <summary>Probes a file without decoding image payloads.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="expectedFormat">The caller-expected format, or <see cref="RawFormat.Unknown"/>.</param>
    /// <param name="genericDescriptorProvided">Whether an authoritative generic descriptor is available.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>The content-based probe result.</returns>
    public static RawProbe Probe(string path, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        RequirePath(path);
        using (System.IO.Stream source = System.IO.File.OpenRead(path))
        {
            return FormatProbe.Probe(source, 0, source.Length, path, expectedFormat, genericDescriptorProvided, options.Limits, options.CancellationToken);
        }
    }

    /// <summary>Opens a bounded memory input according to the requested read selections.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Read selections, limits, and cancellation state.</param>
    /// <returns>The inspected document.</returns>
    public static RawDocument Open(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        return OpenCore(source, genericDescriptor, options, false);
    }

    /// <summary>Opens a bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Read selections, limits, and cancellation state.</param>
    /// <returns>The inspected document backed by an owned bounded copy.</returns>
    public static RawDocument Open(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        byte[] bytes = ReadBoundedStream(source, offset, length, options.Limits, options.CancellationToken);
        return OpenCore(bytes, genericDescriptor, options, true);
    }

    /// <summary>Opens a file according to the requested read selections.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Read selections, limits, and cancellation state.</param>
    /// <returns>The inspected document backed by an owned bounded copy.</returns>
    public static RawDocument Open(string path, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        byte[] bytes = ReadFile(path, options.Limits, options.CancellationToken);
        RawOpenOptions selected = CopyOpenOptions(options, options.ReadMetadata, options.ReadPreviews, options.ReadRawPayload, path);
        return OpenCore(bytes, genericDescriptor, selected, true);
    }

    /// <summary>Opens only metadata and structural image descriptions from memory.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Limits and cancellation state.</param>
    /// <returns>A document that cannot decode raw payloads.</returns>
    public static RawDocument OpenMetadata(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        return OpenCore(source, genericDescriptor, CopyOpenOptions(options, true, false, false, options.FileNameHint), false);
    }

    /// <summary>Opens selected metadata from memory and applies the requested reconciliation policy.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Metadata selections, limits, and reconciliation policy.</param>
    /// <returns>A metadata-only document containing the selected representations.</returns>
    public static RawDocument OpenMetadata(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawMetadataOptions options)
    {
        RawOpenOptions openOptions = CreateMetadataOpenOptions(options, string.Empty);
        return OpenCore(source, genericDescriptor, openOptions, false, options);
    }

    /// <summary>Opens only metadata and structural image descriptions from a caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Limits and cancellation state.</param>
    /// <returns>A metadata-only document backed by an owned bounded copy.</returns>
    public static RawDocument OpenMetadata(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        byte[] bytes = ReadBoundedStream(source, offset, length, options.Limits, options.CancellationToken);
        return OpenCore(bytes, genericDescriptor, CopyOpenOptions(options, true, false, false, options.FileNameHint), true);
    }

    /// <summary>Opens selected metadata from a bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Metadata selections, limits, and reconciliation policy.</param>
    /// <returns>A metadata-only document backed by an owned bounded copy.</returns>
    public static RawDocument OpenMetadata(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawMetadataOptions options)
    {
        RawOpenOptions openOptions = CreateMetadataOpenOptions(options, string.Empty);
        byte[] bytes = ReadBoundedStream(source, offset, length, options.Limits, options.CancellationToken);
        return OpenCore(bytes, genericDescriptor, openOptions, true, options);
    }

    /// <summary>Opens only metadata and structural image descriptions from a file.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Limits and cancellation state.</param>
    /// <returns>A metadata-only document backed by an owned bounded copy.</returns>
    public static RawDocument OpenMetadata(string path, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        byte[] bytes = ReadFile(path, options.Limits, options.CancellationToken);
        return OpenCore(bytes, genericDescriptor, CopyOpenOptions(options, true, false, false, path), true);
    }

    /// <summary>Opens selected metadata from a file and applies the requested reconciliation policy.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Metadata selections, limits, and reconciliation policy.</param>
    /// <returns>A metadata-only document backed by an owned bounded copy.</returns>
    public static RawDocument OpenMetadata(string path, RawDescriptor? genericDescriptor, RawMetadataOptions options)
    {
        RawOpenOptions openOptions = CreateMetadataOpenOptions(options, path);
        byte[] bytes = ReadFile(path, options.Limits, options.CancellationToken);
        return OpenCore(bytes, genericDescriptor, openOptions, true, options);
    }

    /// <summary>Opens only embedded preview and thumbnail descriptions and bytes from memory.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Limits and cancellation state.</param>
    /// <returns>A preview-only document that cannot decode raw payloads.</returns>
    public static RawDocument OpenPreviews(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        return OpenCore(source, genericDescriptor, CopyOpenOptions(options, false, true, false, options.FileNameHint), false);
    }

    /// <summary>Opens only embedded preview and thumbnail descriptions and bytes from a caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Limits and cancellation state.</param>
    /// <returns>A preview-only document backed by an owned bounded copy.</returns>
    public static RawDocument OpenPreviews(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        byte[] bytes = ReadBoundedStream(source, offset, length, options.Limits, options.CancellationToken);
        return OpenCore(bytes, genericDescriptor, CopyOpenOptions(options, false, true, false, options.FileNameHint), true);
    }

    /// <summary>Opens only embedded preview and thumbnail descriptions and bytes from a file.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Limits and cancellation state.</param>
    /// <returns>A preview-only document backed by an owned bounded copy.</returns>
    public static RawDocument OpenPreviews(string path, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        ValidateOpenOptions(options);
        byte[] bytes = ReadFile(path, options.Limits, options.CancellationToken);
        return OpenCore(bytes, genericDescriptor, CopyOpenOptions(options, false, true, false, path), true);
    }

    internal static RawDocument OpenCore(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options, bool ownsSource)
    {
        return OpenCore(source, genericDescriptor, options, ownsSource, null);
    }

    internal static RawDocument OpenCore(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options, bool ownsSource, RawMetadataOptions? metadataOptions)
    {
        options.CheckCancellation();
        if (options.ReadRawPayload && !options.DeferPayloads && !ownsSource)
        {
            CheckedMath.FileSize(source.Length, options.Limits);
            CheckedMath.AllocationSize(source.Length, 1, options.Limits);
            source = source.ToArray();
            ownsSource = true;
        }
        RawProbe probe = FormatProbe.Probe(source, options.FileNameHint, RawFormat.Unknown, genericDescriptor != null, options.Limits, options.CancellationToken);
        if (probe.Format == RawFormat.Unknown)
        {
            RawDiagnosticCode code = probe.IsAmbiguous() ? RawDiagnosticCode.AmbiguousFormat : RawDiagnosticCode.UnknownFormat;
            throw new RawFormatException(code, "The bounded input does not have one authoritative supported interpretation.", -1);
        }
        if (probe.Format == RawFormat.GenericRaw)
        {
            return OpenGeneric(source, genericDescriptor, options, probe, ownsSource);
        }
        BoundedInput input = new BoundedInput(source, 0, source.Length, options.Limits);
        TiffIfdGraph graph = TiffParser.Parse(input);
        return OpenTiff(source, options, probe, graph, ownsSource, metadataOptions);
    }

    internal static RawOpenOptions CopyOpenOptions(RawOpenOptions source, bool readMetadata, bool readPreviews, bool readRawPayload, string fileName)
    {
        RawOpenOptions result = new RawOpenOptions();
        result.Limits = source.Limits;
        result.CancellationToken = source.CancellationToken;
        result.ReadMetadata = readMetadata;
        result.ReadPreviews = readPreviews;
        result.ReadRawPayload = readRawPayload;
        result.DeferPayloads = source.DeferPayloads;
        result.Tolerant = source.Tolerant;
        result.AllowExperimentalProfiles = source.AllowExperimentalProfiles;
        result.ProfileCatalogue = source.ProfileCatalogue;
        result.FileNameHint = fileName == null ? string.Empty : fileName;
        return result;
    }

    internal static RawOpenOptions CreateMetadataOpenOptions(RawMetadataOptions options, string fileName)
    {
        if (options == null || !options.Validate().IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid metadata options are required.", -1);
        }
        RawOpenOptions result = new RawOpenOptions();
        result.Limits = options.Limits;
        result.CancellationToken = options.CancellationToken;
        result.ReadMetadata = true;
        result.ReadPreviews = false;
        result.ReadRawPayload = false;
        result.DeferPayloads = true;
        result.Tolerant = false;
        result.AllowExperimentalProfiles = false;
        result.ProfileCatalogue = options.ProfileCatalogue;
        result.FileNameHint = fileName == null ? string.Empty : fileName;
        return result;
    }

    internal static void ValidateOpenOptions(RawOpenOptions options)
    {
        if (options == null || !options.Validate().IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid raw open options are required.", -1);
        }
    }

    internal static void RequirePath(string path)
    {
        if (path == null || path.Length == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A nonempty input path is required.", -1);
        }
    }

    internal static System.ReadOnlyMemory<byte> GetByteMemory(byte[]? source)
    {
        if (source == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A byte-array input is required.", -1);
        }
        return source;
    }

    internal static byte[] CopySpan(System.ReadOnlySpan<byte> source, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CheckedMath.FileSize(source.Length, limits);
        CheckedMath.AllocationSize(source.Length, 1, limits);
        byte[] bytes = new byte[source.Length];
        source.CopyTo(bytes);
        cancellationToken.ThrowIfCancellationRequested();
        return bytes;
    }

    internal static byte[] ReadFile(string path, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        RequirePath(path);
        using (System.IO.Stream source = System.IO.File.OpenRead(path))
        {
            return ReadBoundedStream(source, 0, source.Length, limits, cancellationToken);
        }
    }

    internal static byte[] ReadBoundedStream(System.IO.Stream source, long offset, long length, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BoundedInput input = BoundedInput.CreateStream(source, offset, length, limits);
        CheckedMath.AllocationSize(length, 1, limits);
        byte[] bytes = new byte[CheckedMath.ToInt32(length)];
        int filled = 0;
        while (filled < bytes.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = input.Read(bytes.AsSpan(filled));
            if (read == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The input ended before the bounded stream copy completed.", input.GetAbsolutePosition());
            }
            filled += read;
        }
        return bytes;
    }

    internal static RawDocument OpenGeneric(System.ReadOnlyMemory<byte> source, RawDescriptor? descriptor, RawOpenOptions options, RawProbe probe, bool ownsSource)
    {
        if (descriptor == null || !descriptor.ValidateSourceLength(source.Length, options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A valid exact-length generic raw descriptor is required.", -1);
        }
        RawDescriptor retainedDescriptor = CopyGenericDescriptor(descriptor);
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.RawImage, "GenericRaw/Data", retainedDescriptor.DataOffset, retainedDescriptor.DataLength, 0, 0, 0, RawCompression.None, true, true);
        RawImageInfo info = CreateGenericImageInfo(retainedDescriptor, payload);
        RawPayloadMap payloadMap = new RawPayloadMap(source.Length, new RawPayloadBlock[] { payload });
        RawCapabilities capabilities = CreateGenericCapabilities(retainedDescriptor, true);
        capabilities.Decoded = capabilities.Decoded && options.ReadRawPayload;
        capabilities.Rendered = capabilities.Rendered && options.ReadRawPayload;
        RawDocument document = new RawDocument(RawFormat.GenericRaw, string.Empty, retainedDescriptor, new RawImageInfo[] { info }, null, payloadMap, capabilities, probe.Diagnostics, options.ReadMetadata, 0, options.ReadPreviews, options.ReadRawPayload);
        document.AttachSource(source, ownsSource, null);
        return document;
    }

    internal static RawDescriptor CopyGenericDescriptor(RawDescriptor source)
    {
        RawDescriptor descriptor = new RawDescriptor();
        descriptor.Width = source.Width;
        descriptor.Height = source.Height;
        descriptor.BitDepth = source.BitDepth;
        descriptor.StoredWordBits = source.StoredWordBits;
        descriptor.SampleKind = source.SampleKind;
        descriptor.Packing = source.Packing;
        descriptor.ByteOrder = source.ByteOrder;
        descriptor.BitOrder = source.BitOrder;
        descriptor.Layout = source.Layout;
        descriptor.ChannelCount = source.ChannelCount;
        descriptor.Channels = new RawChannelDescription[source.Channels.Length];
        int index = 0;
        while (index < source.Channels.Length)
        {
            RawChannelDescription channel = source.Channels[index];
            descriptor.Channels[index] = channel == null ? throw new System.InvalidOperationException("A validated channel description cannot be null.") : new RawChannelDescription(channel.Name, channel.Kind);
            index++;
        }
        descriptor.Photometric = source.Photometric;
        if (source.Cfa != null)
        {
            RawCfaColor[] colors = new RawCfaColor[source.Cfa.Colors.Length];
            System.Array.Copy(source.Cfa.Colors, colors, colors.Length);
            descriptor.Cfa = new RawCfaPattern(source.Cfa.Layout, source.Cfa.RepeatWidth, source.Cfa.RepeatHeight, colors);
        }
        descriptor.Orientation = source.Orientation;
        descriptor.RowStrideBytes = source.RowStrideBytes;
        descriptor.PlaneStrideBytes = source.PlaneStrideBytes;
        descriptor.DataOffset = source.DataOffset;
        descriptor.DataLength = source.DataLength;
        descriptor.AllowTrailingBytes = source.AllowTrailingBytes;
        return descriptor;
    }

    internal static RawDocument OpenTiff(System.ReadOnlyMemory<byte> source, RawOpenOptions options, RawProbe probe, TiffIfdGraph graph, bool ownsSource, RawMetadataOptions? metadataOptions)
    {
        Cr2Structure? cr2 = probe.Format == RawFormat.Cr2 ? Cr2Parser.Parse(source, graph, options) : null;
        NefStructure? nef = probe.Format == RawFormat.Nef ? NefParser.Parse(graph) : null;
        System.Collections.Generic.List<RawImageInfo> images = new System.Collections.Generic.List<RawImageInfo>();
        System.Collections.Generic.List<RawPayloadBlock> payloads = new System.Collections.Generic.List<RawPayloadBlock>();
        System.Collections.Generic.List<RawThumbnail> thumbnails = new System.Collections.Generic.List<RawThumbnail>();
        int imageIndex = 0;
        while (imageIndex < graph.Images.Length)
        {
            options.CheckCancellation();
            TiffImageDescriptor descriptor = graph.Images[imageIndex];
            RawImageKind imageKind = cr2 != null ? Cr2Parser.GetImageKind(cr2, descriptor) : nef != null ? NefParser.GetImageKind(nef, descriptor) : GetTiffImageKind(probe.Format, descriptor);
            Cr2RawImage? cr2Raw = cr2 == null ? null : Cr2Parser.FindRawImage(cr2, descriptor);
            NefRawImage? nefRaw = nef == null ? null : NefParser.FindRawImage(nef, descriptor);
            int firstPayload = payloads.Count;
            AddTiffPayloads(payloads, descriptor, imageKind, graph, source.Length);
            if (cr2Raw != null)
            {
                Cr2Parser.MarkRawPayloads(payloads, firstPayload, cr2Raw.PlaneIndex);
            }
            if (nefRaw != null)
            {
                NefParser.MarkRawPayloads(payloads, firstPayload, nefRaw.PlaneIndex);
            }
            RawPayloadBlock? primaryPayload = payloads.Count == firstPayload + 1 ? payloads[firstPayload] : null;
            RawImageInfo info = CreateTiffImageInfo(descriptor, imageKind, primaryPayload, options.Limits);
            if (cr2Raw != null)
            {
                info.Compression = RawCompression.LosslessJpeg;
            }
            images.Add(info);
            if (options.ReadPreviews && imageKind != RawImageKind.Raw && imageKind != RawImageKind.Auxiliary)
            {
                RawThumbnail? thumbnail = CreateTiffThumbnail(source, descriptor, info, payloads, firstPayload, thumbnails.Count, options);
                if (thumbnail != null)
                {
                    thumbnails.Add(thumbnail);
                }
            }
            imageIndex++;
        }
        if (cr2 != null && options.ReadPreviews)
        {
            RawThumbnail? thumbnail = Cr2Parser.CreateThumbnail(source, cr2, payloads, thumbnails.Count);
            if (thumbnail != null)
            {
                thumbnails.Add(thumbnail);
            }
        }
        RawMetadata? metadata = null;
        CanonCr2Extraction? canon = null;
        NikonNefExtraction? nikon = null;
        if (options.ReadMetadata || options.ReadRawPayload)
        {
            metadata = metadataOptions == null ? ExifCodec.Decode(graph, options.Limits) : ExifCodec.Decode(graph, metadataOptions);
            if (cr2 != null || probe.Format == RawFormat.Tiff)
            {
                TiffImageDescriptor? rawDescriptor = cr2 == null ? null : cr2.RawImage;
                canon = CanonCr2MetadataParser.Parse(source, probe.Format, graph, rawDescriptor, metadata, options);
                if (canon.Metadata != null)
                {
                    int rawModelIndex = rawDescriptor == null ? -1 : rawDescriptor.ImageIndex;
                    if ((canon.Profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id == CanonCr2MetadataParser.Eos1dProfile)
                    {
                        rawModelIndex = AddEarlyCanonRawImage(images, payloads, canon);
                    }
                    if (rawModelIndex < 0 || rawModelIndex >= images.Count)
                    {
                        long errorOffset = cr2 == null ? (canon.EarlyHeader ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset : (cr2.Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawIfdOffset;
                        throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The selected Canon profile raw image index is invalid.", errorOffset);
                    }
                    if (cr2 != null && canon.Profile.Id != CanonCr2MetadataParser.Eos1dProfile)
                    {
                        ApplyCanonCr2Metadata(images, cr2, canon);
                    }
                    else
                    {
                        CanonCr2MetadataParser.ApplyToImage(images[rawModelIndex], canon.Metadata, canon.Profile);
                        if (images[rawModelIndex].Payload != null)
                        {
                            (images[rawModelIndex].Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Decodable = true;
                        }
                    }
                }
            }
            if (nef != null)
            {
                nikon = NikonNefMetadataParser.Parse(graph, nef.RawImages[0].Descriptor, metadata, options);
                if (nikon.Metadata != null)
                {
                    if (nikon.Profile == null && NikonHeRecognition.IsCompression(nikon.Metadata.Compression))
                    {
                        NikonHeRecognition.ApplyToImages(images, nef, nikon.Metadata);
                    }
                    else
                    {
                        ApplyNikonNefMetadata(images, nef, nikon);
                    }
                }
            }
        }
        if (options.ReadMetadata)
        {
            if (metadataOptions == null || metadataOptions.ReadXmp)
            {
                AddXmpPayloads(payloads, graph, source.Length);
            }
            if (metadataOptions == null || metadataOptions.ReadIptc)
            {
                AddIptcPayloads(payloads, graph, source.Length, options.Limits);
            }
            if (metadataOptions == null || metadataOptions.ReadIcc)
            {
                AddIccPayloads(payloads, graph, source.Length, options.Limits);
            }
            if (metadataOptions == null || metadataOptions.ReadMakerNotes)
            {
                AddMakerNotePayloads(payloads, graph, source.Length);
                if (nef != null)
                {
                    NefParser.AddCaptureEditPayloads(payloads, metadata, graph, source.Length);
                }
            }
            RawOpaquePreservation.AddAuthenticityBlocks(payloads, metadata, graph, source);
        }
        RawOpaquePreservation.AddOpaqueIslands(probe.Format, graph, source, payloads);
        RawPayloadMap payloadMap = new RawPayloadMap(source.Length, payloads.ToArray());
        RawCapabilities capabilities = GetCapabilities(probe.Format, null);
        bool decoderAvailable = false;
        if (canon != null && canon.Metadata != null)
        {
            decoderAvailable = true;
        }
        if (nikon != null && nikon.Metadata != null && nikon.Profile != null && (NefUncompressedDecoder.CanDecode(nikon.Profile, (nef ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawImages[0].Descriptor) || NikonCompressionCodec.CanDecode(nikon.Profile, nef.RawImages[0].Descriptor, nikon.Metadata) || NefVariantDecoder.CanDecode(nikon.Profile, nef.RawImages[0].Descriptor)))
        {
            decoderAvailable = true;
        }
        capabilities.Decoded = options.ReadRawPayload && decoderAvailable;
        capabilities.Rendered = capabilities.Decoded && HasBaselineRenderPath(images);
        bool unsupportedNikonCodec = nikon != null && nikon.Metadata != null && NikonHeRecognition.IsCompression(nikon.Metadata.Compression);
        capabilities.PreservingRewrite = capabilities.PreservingRewrite && options.ReadMetadata && options.ReadPreviews && !unsupportedNikonCodec;
        int directoryCount = options.ReadMetadata ? graph.Ifds.Length : 0;
        RawDiagnostic[] diagnostics = cr2 == null || cr2.Diagnostics.Length == 0 ? probe.Diagnostics : Cr2Parser.CombineDiagnostics(probe.Diagnostics, cr2.Diagnostics);
        if (canon != null && canon.Diagnostics.Length != 0)
        {
            diagnostics = Cr2Parser.CombineDiagnostics(diagnostics, canon.Diagnostics);
        }
        if (nikon != null && nikon.Diagnostics.Length != 0)
        {
            diagnostics = Cr2Parser.CombineDiagnostics(diagnostics, nikon.Diagnostics);
        }
        string profileName = canon != null && canon.Profile != null ? canon.Profile.Id : nikon != null && nikon.Profile != null ? nikon.Profile.Id : string.Empty;
        RawDocument document = new RawDocument(probe.Format, profileName, null, images.ToArray(), thumbnails.ToArray(), payloadMap, capabilities, diagnostics, options.ReadMetadata, directoryCount, options.ReadPreviews, options.ReadRawPayload);
        document.AttachSource(source, ownsSource, graph);
        if (cr2 != null)
        {
            document.AttachCr2Header(cr2.Header);
        }
        if (options.ReadMetadata)
        {
            document.AttachMetadata(metadata);
        }
        if (options.ReadMetadata || options.ReadRawPayload)
        {
            AttachDecoderDetails(document, canon, nikon);
        }
        return document;
    }

    internal static void AttachDecoderDetails(RawDocument document, CanonCr2Extraction? canon, NikonNefExtraction? nikon)
    {
        if (canon != null && canon.Metadata != null)
        {
            document.AttachCanonCr2Metadata(canon.Metadata);
            document.AttachReadProfile(canon.Profile);
        }
        if (nikon != null && nikon.Metadata != null)
        {
            document.AttachNikonNefMetadata(nikon.Metadata);
            document.AttachReadProfile(nikon.Profile);
        }
    }

    internal static void ApplyNikonNefMetadata(System.Collections.Generic.List<RawImageInfo> images, NefStructure structure, NikonNefExtraction extraction)
    {
        if (images == null || structure == null || extraction == null || extraction.Metadata == null || extraction.Profile == null)
        {
            return;
        }
        int applied = 0;
        int index = 0;
        while (index < structure.RawImages.Length)
        {
            NefRawImage raw = structure.RawImages[index];
            int modelIndex = raw.Descriptor.ImageIndex;
            if (modelIndex >= 0 && modelIndex < images.Count && raw.Descriptor.Width == (ulong)extraction.Metadata.StoredWidth && raw.Descriptor.Height == (ulong)extraction.Metadata.StoredHeight)
            {
                NikonNefMetadataParser.ApplyToImage(images[modelIndex], extraction.Metadata, extraction.Profile);
                applied++;
            }
            index++;
        }
        if (applied == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The selected Nikon profile does not identify an available raw image.", -1);
        }
    }

    internal static void ApplyCanonCr2Metadata(System.Collections.Generic.List<RawImageInfo> images, Cr2Structure structure, CanonCr2Extraction extraction)
    {
        if (images == null || structure == null || extraction == null || extraction.Metadata == null || extraction.Profile == null)
        {
            return;
        }
        int applied = 0;
        int index = 0;
        while (index < structure.RawImages.Length)
        {
            Cr2RawImage raw = structure.RawImages[index];
            int modelIndex = (raw.Descriptor ?? throw new System.InvalidOperationException("A required value is unavailable.")).ImageIndex;
            if (raw.ImageIndex == 0 && modelIndex >= 0 && modelIndex < images.Count && raw.Descriptor.Width == (ulong)extraction.Metadata.SensorWidth && raw.Descriptor.Height == (ulong)extraction.Metadata.SensorHeight)
            {
                RawImageInfo image = images[modelIndex];
                CanonCr2MetadataParser.ApplyToImage(image, extraction.Metadata, extraction.Profile);
                if (image.Payload != null)
                {
                    image.Payload.Decodable = true;
                }
                applied++;
            }
            index++;
        }
        if (applied == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The selected Canon profile does not identify an available raw image.", (structure.Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawIfdOffset);
        }
    }

    internal static int AddEarlyCanonRawImage(System.Collections.Generic.List<RawImageInfo> images, System.Collections.Generic.List<RawPayloadBlock> payloads, CanonCr2Extraction extraction)
    {
        CanonEarlyRawHeader? header = extraction.EarlyHeader;
        RawReadProfile? profile = extraction.Profile;
        if (header == null || profile == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "Complete early Canon raw profile and payload geometry are required.", -1);
        }
        int imageIndex = images.Count;
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.RawImage, "CanonMakerNote/RawDataOffset", header.Offset, header.Length, imageIndex, 0, 0, RawCompression.LosslessJpeg, true, true);
        RawArea stored = new RawArea(RawAreaKind.StoredImage, 0, 0, header.StoredWidth, header.StoredHeight);
        RawArea? active = CanonCr2MetadataParser.CopyArea(profile.Geometry.ActiveArea, RawAreaKind.Active);
        RawImageInfo info = new RawImageInfo(imageIndex, RawImageKind.Raw, header.StoredWidth, header.StoredHeight, header.Precision, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.LosslessJpeg, RawOrientation.Normal,
            new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) },
            new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, header.StoredWidth, header.StoredHeight, header.Precision, header.StoredWidth) },
            CanonCr2MetadataParser.CopyCfa(profile.Geometry.Cfa), stored, active, CanonCr2MetadataParser.CopyArea(active, RawAreaKind.DefaultCrop), payload);
        images.Add(info);
        payloads.Add(payload);
        return imageIndex;
    }

    /// <summary>Reports independently supported operations for a format and optional generic descriptor.</summary>
    /// <param name="format">The content format.</param>
    /// <param name="genericDescriptor">The generic descriptor when applicable, or <c>null</c>.</param>
    /// <returns>The independent capability flags.</returns>
    public static RawCapabilities GetCapabilities(RawFormat format, RawDescriptor? genericDescriptor)
    {
        if (format == RawFormat.GenericRaw)
        {
            bool valid = genericDescriptor != null && genericDescriptor.Validate(new RawLimits()).IsValid();
            return CreateGenericCapabilities(genericDescriptor, valid);
        }
        if (format == RawFormat.Cr2)
        {
            return new RawCapabilities(true, true, false, false, true, false, false);
        }
        if (format == RawFormat.Nef)
        {
            return new RawCapabilities(true, true, false, false, true, false, false);
        }
        if (format == RawFormat.Tiff)
        {
            return new RawCapabilities(true, true, false, false, false, false, false);
        }
        return new RawCapabilities(false, false, false, false, false, false, false);
    }

    internal static RawCapabilities CreateGenericCapabilities(RawDescriptor? descriptor, bool valid)
    {
        bool renderable = valid && descriptor != null && (descriptor.Photometric == RawPhotometricInterpretation.BlackIsZero || descriptor.Photometric == RawPhotometricInterpretation.WhiteIsZero || descriptor.Photometric == RawPhotometricInterpretation.Rgb || descriptor.Photometric == RawPhotometricInterpretation.LinearRaw || descriptor.Photometric == RawPhotometricInterpretation.ColorFilterArray) && (descriptor.ChannelCount == 1 || descriptor.ChannelCount == 3 || descriptor.ChannelCount == 4);
        return new RawCapabilities(valid, valid, valid, renderable, false, valid, false);
    }

    internal static bool HasBaselineRenderPath(System.Collections.Generic.List<RawImageInfo> images)
    {
        if (images == null)
        {
            return false;
        }
        int index = 0;
        while (index < images.Count)
        {
            RawImageInfo image = images[index];
            if (image != null && image.Kind == RawImageKind.Raw)
            {
                int channelCount = image.GetChannelCount();
                return channelCount == 1 || channelCount == 3 || channelCount == 4;
            }
            index++;
        }
        return false;
    }

    internal static RawImageInfo CreateGenericImageInfo(RawDescriptor descriptor, RawPayloadBlock payload)
    {
        RawPlaneInfo[] planes = new RawPlaneInfo[descriptor.ChannelCount];
        int index = 0;
        while (index < planes.Length)
        {
            RawChannelDescription channel = descriptor.Channels[index];
            planes[index] = new RawPlaneInfo(index, channel.Name, channel.Kind, descriptor.Width, descriptor.Height, descriptor.BitDepth, descriptor.Width);
            index++;
        }
        RawArea stored = new RawArea(RawAreaKind.StoredImage, 0, 0, descriptor.Width, descriptor.Height);
        RawArea active = new RawArea(RawAreaKind.Active, 0, 0, descriptor.Width, descriptor.Height);
        RawArea crop = new RawArea(RawAreaKind.DefaultCrop, 0, 0, descriptor.Width, descriptor.Height);
        return new RawImageInfo(0, RawImageKind.Raw, descriptor.Width, descriptor.Height, descriptor.BitDepth, descriptor.SampleKind, descriptor.Photometric, RawCompression.None, descriptor.Orientation, descriptor.Channels, planes, descriptor.Cfa, stored, active, crop, payload);
    }

    internal static RawImageKind GetTiffImageKind(RawFormat format, TiffImageDescriptor descriptor)
    {
        if (descriptor.TransparencyMask)
        {
            return RawImageKind.Mask;
        }
        if (descriptor.ReducedResolution)
        {
            return RawImageKind.Thumbnail;
        }
        if (descriptor.Photometric == RawPhotometricInterpretation.ColorFilterArray || descriptor.Photometric == RawPhotometricInterpretation.LinearRaw || format == RawFormat.Cr2)
        {
            return RawImageKind.Raw;
        }
        return RawImageKind.Preview;
    }

    internal static void AddTiffPayloads(System.Collections.Generic.List<RawPayloadBlock> payloads, TiffImageDescriptor descriptor, RawImageKind imageKind, TiffIfdGraph graph, long sourceLength)
    {
        int index = 0;
        while (index < descriptor.Blocks.Length)
        {
            TiffDataBlock block = descriptor.Blocks[index];
            long offset = RelativeTiffOffset(block.Offset, graph);
            long length = CheckedMath.ToInt64(block.ByteCount);
            if (offset < 0 || length <= 0 || offset > sourceLength || length > sourceLength - offset)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "A TIFF image payload block exceeds the bounded source.", offset);
            }
            RawPayloadKind payloadKind = imageKind == RawImageKind.Raw || imageKind == RawImageKind.Auxiliary ? RawPayloadKind.RawImage : imageKind == RawImageKind.Thumbnail ? RawPayloadKind.Thumbnail : RawPayloadKind.Preview;
            string blockName = block.Tiled ? "Tile" : "Strip";
            RawCompression compression = GetTiffCompression(descriptor);
            bool decodable = compression != RawCompression.Unknown;
            payloads.Add(new RawPayloadBlock(payloadKind, descriptor.Path + "/" + blockName + block.Index, offset, length, descriptor.ImageIndex, block.Plane, block.Index, compression, decodable, true));
            index++;
        }
    }

    internal static void AddXmpPayloads(System.Collections.Generic.List<RawPayloadBlock> payloads, TiffIfdGraph graph, long sourceLength)
    {
        int ifdIndex = 0;
        while (ifdIndex < graph.Ifds.Length)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            int entryIndex = 0;
            while (entryIndex < ifd.Entries.Length)
            {
                TiffEntry entry = ifd.Entries[entryIndex];
                if (entry.Tag == 700 && entry.RawBytes.Length > 0 && (entry.FieldType == TiffFieldType.Byte || entry.FieldType == TiffFieldType.Undefined))
                {
                    long absolute = XmpCodec.GetValueOffset(graph, entry);
                    long offset = absolute - graph.ContainerStart;
                    if (offset < 0 || offset > sourceLength || entry.RawBytes.Length > sourceLength - offset)
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "An XMP packet exceeds the bounded TIFF source.", absolute);
                    }
                    payloads.Add(new RawPayloadBlock(RawPayloadKind.Xmp, ifd.Path + "/Tag0x02BC", offset, entry.RawBytes.Length, -1, -1, -1, RawCompression.None, true, true));
                }
                entryIndex++;
            }
            ifdIndex++;
        }
    }

    internal static void AddIptcPayloads(System.Collections.Generic.List<RawPayloadBlock> payloads, TiffIfdGraph graph, long sourceLength, RawLimits? limits)
    {
        RawIptcRecord[] records = IptcCodec.Discover(graph, limits);
        int index = 0;
        while (index < records.Length)
        {
            RawIptcRecord record = records[index];
            long offset = record.Offset - graph.ContainerStart;
            long length = (record.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength;
            if (offset < 0 || offset > sourceLength || length > sourceLength - offset)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "An IPTC IIM payload exceeds the bounded TIFF source.", record.Offset);
            }
            payloads.Add(new RawPayloadBlock(RawPayloadKind.Iptc, record.IfdPath + "/IPTC" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), offset, length, -1, -1, -1, RawCompression.None, true, true));
            index++;
        }
    }

    internal static void AddIccPayloads(System.Collections.Generic.List<RawPayloadBlock> payloads, TiffIfdGraph graph, long sourceLength, RawLimits? limits)
    {
        RawIccProfile[] profiles = IccCodec.Discover(graph, limits);
        int index = 0;
        while (index < profiles.Length)
        {
            RawIccProfile profile = profiles[index];
            long offset = profile.Offset - graph.ContainerStart;
            long length = (profile.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength;
            if (offset < 0 || offset > sourceLength || length > sourceLength - offset)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "An ICC profile exceeds the bounded TIFF source.", profile.Offset);
            }
            payloads.Add(new RawPayloadBlock(RawPayloadKind.IccProfile, profile.IfdPath + "/ICC" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), offset, length, -1, -1, -1, RawCompression.None, true, true));
            index++;
        }
    }

    internal static void AddMakerNotePayloads(System.Collections.Generic.List<RawPayloadBlock> payloads, TiffIfdGraph graph, long sourceLength)
    {
        int ifdIndex = 0;
        int noteIndex = 0;
        while (ifdIndex < graph.Ifds.Length)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            int entryIndex = 0;
            while (entryIndex < ifd.Entries.Length)
            {
                TiffEntry entry = ifd.Entries[entryIndex];
                if (entry.Tag == 37500 && entry.RawBytes.Length > 0)
                {
                    long absolute = MakerNoteCodec.ValueOffset(graph, entry);
                    long offset = absolute - graph.ContainerStart;
                    long length = entry.RawBytes.LongLength;
                    if (offset < 0 || offset > sourceLength || length > sourceLength - offset)
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "A maker-note payload exceeds the bounded TIFF source.", absolute);
                    }
                    string path = ifd.Path + "/MakerNote" + noteIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    payloads.Add(new RawPayloadBlock(RawPayloadKind.MakerNote, path, offset, length, -1, -1, -1, RawCompression.None, false, true));
                    noteIndex++;
                }
                entryIndex++;
            }
            ifdIndex++;
        }
    }

    internal static long RelativeTiffOffset(ulong offset, TiffIfdGraph? graph)
    {
        long signedOffset = CheckedMath.ToInt64(offset);
        long absolute = CheckedMath.Add((graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.BaseOffset, signedOffset);
        if (absolute < graph.ContainerStart)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "A TIFF payload starts before its bounded container.", absolute);
        }
        return absolute - graph.ContainerStart;
    }

    internal static RawCompression GetTiffCompression(TiffImageDescriptor descriptor)
    {
        if (descriptor.CompressionCode == 6 || descriptor.CompressionCode == 7)
        {
            return RawCompression.BaselineJpeg;
        }
        return descriptor.Compression;
    }

    internal static RawImageInfo CreateTiffImageInfo(TiffImageDescriptor descriptor, RawImageKind imageKind, RawPayloadBlock? payload, RawLimits? limits)
    {
        int width = ToModelDimension(descriptor.Width, (limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxWidth, "TIFF image width");
        int height = ToModelDimension(descriptor.Height, limits.MaxHeight, "TIFF image height");
        int channelCount = ToModelDimension(descriptor.SamplesPerPixel, 64, "TIFF channel count");
        int bitDepth = 8;
        if (descriptor.BitsPerSample.Length != 0)
        {
            bitDepth = ToModelDimension(descriptor.BitsPerSample[0], 32, "TIFF sample precision");
        }
        RawSampleKind sampleKind = GetTiffSampleKind(descriptor, bitDepth);
        RawChannelDescription[] channels = CreateTiffChannels(descriptor.Photometric, channelCount);
        RawPlaneInfo[] planes = new RawPlaneInfo[channelCount];
        int index = 0;
        while (index < planes.Length)
        {
            int planeDepth = bitDepth;
            if (index < descriptor.BitsPerSample.Length)
            {
                planeDepth = ToModelDimension(descriptor.BitsPerSample[index], 32, "TIFF plane precision");
            }
            planes[index] = new RawPlaneInfo(index, channels[index].Name, channels[index].Kind, width, height, planeDepth, width);
            index++;
        }
        RawCfaPattern? cfa = CreateTiffCfa(descriptor);
        RawArea stored = new RawArea(RawAreaKind.StoredImage, 0, 0, width, height);
        RawArea active = new RawArea(RawAreaKind.Active, 0, 0, width, height);
        RawArea crop = new RawArea(RawAreaKind.DefaultCrop, 0, 0, width, height);
        return new RawImageInfo(descriptor.ImageIndex, imageKind, width, height, bitDepth, sampleKind, descriptor.Photometric, GetTiffCompression(descriptor), descriptor.Orientation, channels, planes, cfa, stored, active, crop, payload);
    }

    internal static int ToModelDimension(ulong value, int maximum, string context)
    {
        if (value == 0 || value > (ulong)maximum || value > int.MaxValue)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, context + " is zero or exceeds the supported model limit.", -1);
        }
        return (int)value;
    }

    internal static RawSampleKind GetTiffSampleKind(TiffImageDescriptor descriptor, int bitDepth)
    {
        TiffSampleFormat format = descriptor.SampleFormats.Length == 0 ? TiffSampleFormat.UnsignedInteger : descriptor.SampleFormats[0];
        if (format == TiffSampleFormat.SignedInteger)
        {
            return RawSampleKind.SignedInteger;
        }
        if (format == TiffSampleFormat.FloatingPoint)
        {
            return bitDepth == 16 ? RawSampleKind.Half : RawSampleKind.Single;
        }
        return RawSampleKind.UnsignedInteger;
    }

    internal static RawChannelDescription[] CreateTiffChannels(RawPhotometricInterpretation photometric, int count)
    {
        RawChannelDescription[] channels = new RawChannelDescription[count];
        int index = 0;
        while (index < count)
        {
            string name = "C" + index;
            RawChannelKind kind = RawChannelKind.Custom;
            if ((photometric == RawPhotometricInterpretation.WhiteIsZero || photometric == RawPhotometricInterpretation.BlackIsZero) && index == 0)
            {
                name = "Gray";
                kind = RawChannelKind.Gray;
            }
            else if (photometric == RawPhotometricInterpretation.Rgb)
            {
                if (index == 0)
                {
                    name = "R";
                    kind = RawChannelKind.Red;
                }
                else if (index == 1)
                {
                    name = "G";
                    kind = RawChannelKind.Green;
                }
                else if (index == 2)
                {
                    name = "B";
                    kind = RawChannelKind.Blue;
                }
                else if (index == 3)
                {
                    name = "A";
                    kind = RawChannelKind.Alpha;
                }
            }
            else if (photometric == RawPhotometricInterpretation.YCbCr)
            {
                name = index == 0 ? "Y" : index == 1 ? "Cb" : "Cr";
                kind = index == 0 ? RawChannelKind.Gray : RawChannelKind.Custom;
            }
            else if (photometric == RawPhotometricInterpretation.ColorFilterArray && index == 0)
            {
                name = "CFA";
                kind = RawChannelKind.Custom;
            }
            channels[index] = new RawChannelDescription(name, kind);
            index++;
        }
        return channels;
    }

    internal static RawCfaPattern? CreateTiffCfa(TiffImageDescriptor descriptor)
    {
        if (descriptor.Photometric != RawPhotometricInterpretation.ColorFilterArray)
        {
            return null;
        }
        TiffEntry? dimensionsEntry = descriptor.Ifd.FindEntry(33421);
        TiffEntry? patternEntry = descriptor.Ifd.FindEntry(33422);
        if (dimensionsEntry == null || patternEntry == null)
        {
            return null;
        }
        ulong[] dimensions = TiffImageData.GetUnsignedValues(dimensionsEntry);
        if (dimensions.Length != 2 || dimensions[0] == 0 || dimensions[1] == 0 || dimensions[0] > 64 || dimensions[1] > 64 || dimensions[0] * dimensions[1] != (ulong)patternEntry.RawBytes.Length)
        {
            return null;
        }
        RawCfaColor[] colors = new RawCfaColor[patternEntry.RawBytes.Length];
        int index = 0;
        while (index < colors.Length)
        {
            colors[index] = TiffCfaColor(patternEntry.RawBytes[index]);
            index++;
        }
        return RawCfaPattern.CreateArbitrary((int)dimensions[0], (int)dimensions[1], colors);
    }

    internal static RawCfaColor TiffCfaColor(byte value)
    {
        if (value == 0) return RawCfaColor.Red;
        if (value == 1) return RawCfaColor.Green;
        if (value == 2) return RawCfaColor.Blue;
        if (value == 3) return RawCfaColor.Cyan;
        if (value == 4) return RawCfaColor.Magenta;
        if (value == 5) return RawCfaColor.Yellow;
        if (value == 6) return RawCfaColor.White;
        return RawCfaColor.Unknown;
    }

    internal static RawThumbnail? CreateTiffThumbnail(System.ReadOnlyMemory<byte> source, TiffImageDescriptor descriptor, RawImageInfo info, System.Collections.Generic.List<RawPayloadBlock> payloads, int firstPayload, int thumbnailIndex, RawOpenOptions options)
    {
        int payloadCount = payloads.Count - firstPayload;
        if (payloadCount <= 0)
        {
            return null;
        }
        long totalLength = 0;
        int index = firstPayload;
        while (index < payloads.Count)
        {
            totalLength = CheckedMath.Add(totalLength, payloads[index].Length);
            index++;
        }
        if (totalLength > (options.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxPreviewBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The retained TIFF preview exceeds the configured preview limit.", payloads[firstPayload].Offset);
        }
        CheckedMath.AllocationSize(totalLength, 1, options.Limits);
        byte[] bytes = new byte[CheckedMath.ToInt32(totalLength)];
        int destinationOffset = 0;
        index = firstPayload;
        while (index < payloads.Count)
        {
            RawPayloadBlock block = payloads[index];
            source.Span.Slice((int)block.Offset, (int)block.Length).CopyTo(bytes.AsSpan(destinationOffset, (int)block.Length));
            destinationOffset += (int)block.Length;
            index++;
        }
        RawThumbnailKind kind = RawThumbnailKind.Opaque;
        string mimeType = "application/octet-stream";
        if (descriptor.CompressionCode == 6 || descriptor.CompressionCode == 7)
        {
            kind = RawThumbnailKind.Jpeg;
            mimeType = "image/jpeg";
        }
        else if (descriptor.CompressionCode == 1 && info.BitDepth == 8 && descriptor.PlanarConfiguration == TiffPlanarConfiguration.Chunky)
        {
            long expected = (long)info.Width * info.Height * info.GetChannelCount();
            if (bytes.LongLength == expected && info.Photometric == RawPhotometricInterpretation.Rgb && info.GetChannelCount() == 3)
            {
                kind = RawThumbnailKind.Rgb8;
                mimeType = "image/x-rgb";
            }
            else if (bytes.LongLength == expected && (info.Photometric == RawPhotometricInterpretation.BlackIsZero || info.Photometric == RawPhotometricInterpretation.WhiteIsZero) && info.GetChannelCount() == 1)
            {
                kind = RawThumbnailKind.Gray8;
                mimeType = "image/x-gray";
            }
        }
        RawPayloadBlock? payload = payloadCount == 1 ? payloads[firstPayload] : null;
        return new RawThumbnail(thumbnailIndex, kind, info.Width, info.Height, info.Orientation, mimeType, bytes, payload, null);
    }

    /// <summary>Decodes the selected image from an opened document.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="options">Decode selection, limits, and cancellation state.</param>
    /// <returns>The normalized sensor image.</returns>
    /// <exception cref="RawUnsupportedException">The document does not currently support raw decode.</exception>
    public static RawSensorImage Decode(RawDocument document, RawDecodeOptions options)
    {
        RawImageInfo info = ValidateDecodeRequest(document, options);
        RawFormatDetails? details = document.FormatDetails;
        if (details != null && details.Canon != null)
        {
            return Cr2LosslessJpegDecoder.Decode(document, info, options);
        }
        if (details != null && details.Nikon != null)
        {
            if (NefVariantDecoder.IsProfile(document.ReadProfile))
            {
                return NefVariantDecoder.Decode(document, info, options);
            }
            if (NikonCompressionCodec.IsProfile(document.ReadProfile))
            {
                return NikonCompressionCodec.Decode(document, info, options);
            }
            return NefUncompressedDecoder.Decode(document, info, options);
        }
        RawDescriptor? descriptor = document.GenericDescriptor;
        long rowSampleCount = CheckedMath.Multiply((descriptor ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, descriptor.ChannelCount);
        long totalSampleCount = CheckedMath.Multiply(rowSampleCount, descriptor.Height);
        int normalizedElementSize = descriptor.SampleKind == RawSampleKind.Half || descriptor.SampleKind == RawSampleKind.Single ? 4 : 2;
        CheckedMath.DecodedSize(totalSampleCount, normalizedElementSize, options.Limits);
        CheckedMath.AllocationSize(totalSampleCount, normalizedElementSize, options.Limits);
        int rowStride = CheckedMath.ToInt32(rowSampleCount);
        int sampleCount = CheckedMath.ToInt32(totalSampleCount);
        ushort[]? integerSamples = null;
        float[]? floatingSamples = null;
        if (descriptor.SampleKind == RawSampleKind.Half || descriptor.SampleKind == RawSampleKind.Single)
        {
            floatingSamples = new float[sampleCount];
            GenericRawCodec.DecodeNormalized(document.Source, descriptor, RawDecodeRegion.CreateWhole(descriptor), floatingSamples, options);
        }
        else
        {
            integerSamples = new ushort[sampleCount];
            GenericRawCodec.DecodeNormalized(document.Source, descriptor, RawDecodeRegion.CreateWhole(descriptor), integerSamples, options);
        }
        RawStoredImage? stored = options.PreserveStoredSamples ? DecodeStored(document, descriptor, info, rowStride, sampleCount, options) : null;
        double whiteLevel = descriptor.SampleKind == RawSampleKind.Half || descriptor.SampleKind == RawSampleKind.Single ? 1.0 : descriptor.BitDepth == 32 ? uint.MaxValue : (1UL << descriptor.BitDepth) - 1UL;
        RawBlackLevel blackLevel = RawBlackLevel.CreateScalar(0.0, descriptor.ChannelCount);
        return new RawSensorImage(info, integerSamples, floatingSamples, rowStride, true, stored, null, null, blackLevel, new double[] { whiteLevel }, null, null, document.Diagnostics);
    }

    /// <summary>Opens and decodes one descriptor-driven raw memory input.</summary>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative generic descriptor.</param>
    /// <param name="options">Decode selection, limits, and cancellation state.</param>
    /// <returns>The normalized sensor image.</returns>
    public static RawSensorImage Decode(System.ReadOnlyMemory<byte> source, RawDescriptor? descriptor, RawDecodeOptions options)
    {
        RawOpenOptions openOptions = DecodeOpenOptions(options, string.Empty);
        RawDocument document = Open(source, descriptor, openOptions);
        return Decode(document, options);
    }

    /// <summary>Opens and decodes one bounded descriptor-driven raw stream without closing it.</summary>
    /// <param name="source">The caller-owned readable stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="descriptor">The authoritative generic descriptor.</param>
    /// <param name="options">Decode selection, limits, and cancellation state.</param>
    /// <returns>The normalized sensor image.</returns>
    public static RawSensorImage Decode(System.IO.Stream source, long offset, long length, RawDescriptor? descriptor, RawDecodeOptions options)
    {
        RawOpenOptions openOptions = DecodeOpenOptions(options, string.Empty);
        RawDocument document = Open(source, offset, length, descriptor, openOptions);
        return Decode(document, options);
    }

    /// <summary>Opens and decodes one descriptor-driven raw file.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="descriptor">The authoritative generic descriptor.</param>
    /// <param name="options">Decode selection, limits, and cancellation state.</param>
    /// <returns>The normalized sensor image.</returns>
    public static RawSensorImage Decode(string path, RawDescriptor? descriptor, RawDecodeOptions options)
    {
        RawOpenOptions openOptions = DecodeOpenOptions(options, path);
        RawDocument document = Open(path, descriptor, openOptions);
        return Decode(document, options);
    }

    internal static RawOpenOptions DecodeOpenOptions(RawDecodeOptions options, string fileName)
    {
        if (options == null || !options.Validate().IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid raw decode options are required.", -1);
        }
        RawOpenOptions result = new RawOpenOptions();
        result.Limits = options.Limits;
        result.CancellationToken = options.CancellationToken;
        result.ReadMetadata = true;
        result.ReadPreviews = false;
        result.ReadRawPayload = true;
        result.DeferPayloads = true;
        result.AllowExperimentalProfiles = options.AllowExperimentalProfiles;
        result.FileNameHint = fileName == null ? string.Empty : fileName;
        return result;
    }

    internal static RawImageInfo ValidateDecodeRequest(RawDocument document, RawDecodeOptions options)
    {
        if (options == null || !options.Validate().IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid raw decode options are required.", -1);
        }
        options.CheckCancellation();
        if (document != null && document.FormatDetails != null && document.FormatDetails.Nikon != null && NikonHeRecognition.IsCompression(document.FormatDetails.Nikon.Compression))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, NikonHeRecognition.GetMessage(document.FormatDetails.Nikon.Compression), -1);
        }
        bool generic = document != null && document.Format == RawFormat.GenericRaw && document.GenericDescriptor != null;
        bool canon = document != null && document.FormatDetails != null && (document.Format == RawFormat.Cr2 || document.Format == RawFormat.Tiff) && document.FormatDetails.Canon != null;
        bool nikon = document != null && document.FormatDetails != null && document.Format == RawFormat.Nef && document.FormatDetails.Nikon != null && document.ReadProfile != null && (NefUncompressedDecoder.IsProfile(document.ReadProfile) || NikonCompressionCodec.IsProfile(document.ReadProfile) || NefVariantDecoder.IsProfile(document.ReadProfile));
        if (document == null || document.Capabilities == null || !document.Capabilities.Decoded || !generic && !canon && !nikon)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "The opened document does not have a supported raw decoder.", -1);
        }
        if (!document.SourceRetained || !document.RawPayloadAvailable)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The document was opened without retained raw-payload access.", -1);
        }
        if (!options.NormalizeSamples)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The public sensor-image facade currently requires normalized output.", -1);
        }
        if (canon || nikon)
        {
            RawImageInfo? selected = SelectCanonRawImage(document, options.ImageIndex, options.PlaneIndex);
            if (selected == null)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The requested raw image or acquisition plane is not available.", -1);
            }
            return selected;
        }
        if (options.ImageIndex < 0 || options.ImageIndex >= (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length || options.PlaneIndex != 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The requested image or plane index is not available.", -1);
        }
        return document.Images[options.ImageIndex];
    }

    internal static RawImageInfo? SelectCanonRawImage(RawDocument document, int requestedImage, int requestedPlane)
    {
        if (document == null || document.Images == null || requestedImage < 0 || requestedPlane < 0)
        {
            return null;
        }
        int imageIndex = -1;
        int planeIndex = 0;
        int index = 0;
        while (index < document.Images.Length)
        {
            RawImageInfo image = document.Images[index];
            if (image != null && image.Kind == RawImageKind.Raw)
            {
                imageIndex++;
                planeIndex = 0;
                if (imageIndex == requestedImage && requestedPlane == 0)
                {
                    return image;
                }
                if (imageIndex > requestedImage)
                {
                    return null;
                }
            }
            else if (image != null && image.Kind == RawImageKind.Auxiliary && imageIndex == requestedImage)
            {
                planeIndex++;
                if (planeIndex == requestedPlane)
                {
                    return image;
                }
            }
            index++;
        }
        return null;
    }

    internal static RawStoredImage DecodeStored(RawDocument document, RawDescriptor descriptor, RawImageInfo info, int rowStride, int sampleCount, RawDecodeOptions options)
    {
        if (descriptor.SampleKind == RawSampleKind.UnsignedInteger)
        {
            uint[] samples = new uint[sampleCount];
            GenericRawCodec.DecodeStored(document.Source, descriptor, RawDecodeRegion.CreateWhole(descriptor), samples, options);
            return new RawStoredImage(info, samples, null, null, rowStride, true, null);
        }
        if (descriptor.SampleKind == RawSampleKind.SignedInteger)
        {
            int[] samples = new int[sampleCount];
            GenericRawCodec.DecodeStored(document.Source, descriptor, RawDecodeRegion.CreateWhole(descriptor), samples, options);
            return new RawStoredImage(info, null, samples, null, rowStride, true, null);
        }
        float[] floating = new float[sampleCount];
        GenericRawCodec.DecodeStored(document.Source, descriptor, RawDecodeRegion.CreateWhole(descriptor), floating, options);
        return new RawStoredImage(info, null, null, floating, rowStride, true, null);
    }

    /// <summary>Decodes and renders the selected image and plane through the complete processing pipeline.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="options">Render controls and resource limits.</param>
    /// <returns>A tightly pixel-interleaved gray, RGB, or RGBA image.</returns>
    /// <exception cref="RawUnsupportedException">The document or requested processing combination is unsupported.</exception>
    public static RawRgbImage Render(RawDocument document, RawRenderOptions options)
    {
        ValidateRenderRequest(document, options);
        RawDecodeOptions decodeOptions = new RawDecodeOptions();
        decodeOptions.Limits = options.Limits;
        decodeOptions.CancellationToken = options.CancellationToken;
        decodeOptions.ImageIndex = options.ImageIndex;
        decodeOptions.PlaneIndex = options.PlaneIndex;
        decodeOptions.NormalizeSamples = true;
        decodeOptions.PreserveStoredSamples = CanPreprocessRender(document);
        decodeOptions.ApplyLinearization = true;
        RawSensorImage sensor = Decode(document, decodeOptions);
        if (decodeOptions.PreserveStoredSamples)
        {
            RawNormalizationOptions normalization = new RawNormalizationOptions();
            normalization.Limits = options.Limits;
            normalization.CancellationToken = options.CancellationToken;
            normalization.UserCrop = options.UserCrop;
            normalization.BlackLevel = sensor.BlackLevel;
            normalization.WhiteLevels = sensor.WhiteLevels;
            normalization.MaskedAreas = sensor.MaskedAreas;
            normalization.PreserveOriginalValues = false;
            if (document.FormatDetails != null && document.FormatDetails.Nikon != null)
            {
                normalization.LinearizationTable = document.FormatDetails.Nikon.LinearizationTable;
            }
            sensor = RawRenderer.Preprocess(sensor, normalization);
        }
        return RawRenderer.Render(sensor, options);
    }

    /// <summary>Opens and renders one descriptor-driven raw memory input.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="descriptor">The authoritative generic descriptor.</param>
    /// <param name="options">Baseline render controls.</param>
    /// <returns>The rendered image.</returns>
    public static RawRgbImage Render(System.ReadOnlyMemory<byte> source, RawDescriptor? descriptor, RawRenderOptions options)
    {
        RawOpenOptions openOptions = RenderOpenOptions(options, string.Empty);
        return Render(Open(source, descriptor, openOptions), options);
    }

    /// <summary>Opens and renders one bounded descriptor-driven raw stream without closing it.</summary>
    /// <param name="source">The caller-owned readable stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="descriptor">The authoritative generic descriptor.</param>
    /// <param name="options">Baseline render controls.</param>
    /// <returns>The rendered image.</returns>
    public static RawRgbImage Render(System.IO.Stream source, long offset, long length, RawDescriptor? descriptor, RawRenderOptions options)
    {
        RawOpenOptions openOptions = RenderOpenOptions(options, string.Empty);
        return Render(Open(source, offset, length, descriptor, openOptions), options);
    }

    /// <summary>Opens and renders one descriptor-driven raw file.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="descriptor">The authoritative generic descriptor.</param>
    /// <param name="options">Baseline render controls.</param>
    /// <returns>The rendered image.</returns>
    public static RawRgbImage Render(string path, RawDescriptor? descriptor, RawRenderOptions options)
    {
        RawOpenOptions openOptions = RenderOpenOptions(options, path);
        return Render(Open(path, descriptor, openOptions), options);
    }

    internal static RawOpenOptions RenderOpenOptions(RawRenderOptions options, string fileName)
    {
        if (options == null || !options.Validate().IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid raw render options are required.", -1);
        }
        RawOpenOptions result = new RawOpenOptions();
        result.Limits = options.Limits;
        result.CancellationToken = options.CancellationToken;
        result.ReadMetadata = false;
        result.ReadPreviews = false;
        result.ReadRawPayload = true;
        result.DeferPayloads = true;
        result.FileNameHint = fileName == null ? string.Empty : fileName;
        return result;
    }

    internal static void ValidateRenderRequest(RawDocument document, RawRenderOptions options)
    {
        if (options == null || !options.Validate().IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid raw render options are required.", -1);
        }
        options.CheckCancellation();
        if (document == null || document.Capabilities == null || !document.Capabilities.Rendered || document.Images == null || document.Images.Length == 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The document does not have a supported render path.", -1);
        }
    }

    internal static bool CanPreprocessRender(RawDocument document)
    {
        RawFormatDetails? details = document.FormatDetails;
        if (details != null && details.Canon != null && details.Canon.IsSubsampled)
        {
            return false;
        }
        if (details != null && details.Nikon != null && NefVariantDecoder.IsProfile(document.ReadProfile) && NefVariantDecoder.GetKind(document.ReadProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")) == NefVariantKind.SmallYCbCr)
        {
            return false;
        }
        return true;
    }

    /// <summary>Validates an opened document according to selected validation subsystems.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="options">Validation controls and limits.</param>
    /// <returns>All selected validation findings.</returns>
    public static RawValidationReport Validate(RawDocument document, RawValidationOptions options)
    {
        if (options == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Raw validation options are required.", -1);
        }
        RawValidationReport optionReport = options.Validate();
        if (!optionReport.IsValid())
        {
            return optionReport;
        }
        options.CheckCancellation();
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (document == null)
        {
            AddConformance(messages, FirstSelectedCategory(options), RawDiagnosticCode.InvalidArgument, RawSeverity.Error, RawFormat.Unknown, -1, "Document", "An opened document is required.", false);
            return CreateValidationReport(options, messages);
        }
        if (options.ValidateSemantic)
        {
            if (document.Format <= RawFormat.Unknown || document.Format > RawFormat.Nef || document.ProfileName == null || document.MetadataDirectoryCount < 0)
            {
                AddConformance(messages, RawValidationCategory.Semantic, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, document.Format, -1, "Document.Identity", "The document identity or metadata directory count is invalid.", false);
            }
            if (document.Capabilities == null || !document.Capabilities.Recognized)
            {
                AddConformance(messages, RawValidationCategory.Semantic, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, document.Format, -1, "Document.Capabilities", "An opened document must report recognition capability.", false);
            }
        }
        if (options.ValidateStructure)
        {
            if (document.PayloadMap == null)
            {
                AddConformance(messages, RawValidationCategory.Structural, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, document.Format, -1, "Document.PayloadMap", "An opened document requires a payload map.", false);
            }
            else
            {
                AddReportAsCategory(messages, document.PayloadMap.Validate(), RawValidationCategory.Structural, document.Format);
            }
            if (document.TiffGraph != null)
            {
                AddStructuralReport(messages, TiffValidator.Validate(document.TiffGraph, options.Strict), document, options);
            }
            if (document.Format == RawFormat.GenericRaw && (document.GenericDescriptor == null || !document.GenericDescriptor.ValidateSourceLength(document.Source.Length, options.Limits).IsValid()))
            {
                AddConformance(messages, RawValidationCategory.Structural, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, document.Format, -1, "Document.GenericDescriptor", "The generic descriptor does not match the retained bounded source.", false);
            }
        }
        if (options.ValidateSemantic)
        {
            ValidateDocumentModels(document, options, messages);
        }
        if (options.ValidateMetadata)
        {
            ValidateMetadataModels(document, options, messages);
        }
        if (options.ValidateProfile)
        {
            ValidateDocumentProfile(document, options, messages);
        }
        if (options.ValidateCodec && document.Format == RawFormat.GenericRaw && document.GenericDescriptor != null && document.SourceRetained)
        {
            ValidateGenericPayload(document, messages);
        }
        ValidateDocumentDiagnostics(document, options, messages);
        return CreateValidationReport(options, messages);
    }

    /// <summary>Validates one dry-run write plan according to selected profile and writer subsystems.</summary>
    /// <param name="plan">The dry-run write plan.</param>
    /// <param name="options">Validation controls and limits.</param>
    /// <returns>All selected profile and writer conformance findings.</returns>
    public static RawValidationReport Validate(RawWritePlan plan, RawValidationOptions options)
    {
        if (options == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Raw validation options are required.", -1);
        }
        RawValidationReport optionReport = options.Validate();
        if (!optionReport.IsValid())
        {
            return optionReport;
        }
        options.CheckCancellation();
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (plan == null)
        {
            AddConformance(messages, FirstSelectedCategory(options), RawDiagnosticCode.InvalidArgument, RawSeverity.Error, RawFormat.Unknown, -1, "WritePlan", "A dry-run write plan is required.", false);
            return CreateValidationReport(options, messages);
        }
        if (options.ValidateProfile)
        {
            ValidateWritePlanProfile(plan, messages);
        }
        if (options.ValidateWriter)
        {
            ValidateWritePlan(plan, options, messages);
        }
        return CreateValidationReport(options, messages);
    }

    /// <summary>Opens and validates one bounded memory input.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Validation controls and limits.</param>
    /// <returns>All selected validation findings.</returns>
    public static RawValidationReport Validate(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawValidationOptions options)
    {
        RawOpenOptions openOptions = ValidationOpenOptions(options, string.Empty);
        return Validate(Open(source, genericDescriptor, openOptions), options);
    }

    /// <summary>Opens and validates one bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Validation controls and limits.</param>
    /// <returns>All selected validation findings.</returns>
    public static RawValidationReport Validate(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawValidationOptions options)
    {
        RawOpenOptions openOptions = ValidationOpenOptions(options, string.Empty);
        return Validate(Open(source, offset, length, genericDescriptor, openOptions), options);
    }

    /// <summary>Opens and validates one file.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c>.</param>
    /// <param name="options">Validation controls and limits.</param>
    /// <returns>All selected validation findings.</returns>
    public static RawValidationReport Validate(string path, RawDescriptor? genericDescriptor, RawValidationOptions options)
    {
        RawOpenOptions openOptions = ValidationOpenOptions(options, path);
        return Validate(Open(path, genericDescriptor, openOptions), options);
    }

    internal static RawOpenOptions ValidationOpenOptions(RawValidationOptions options, string fileName)
    {
        if (options == null || !options.Validate().IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid raw validation options are required.", -1);
        }
        RawOpenOptions result = new RawOpenOptions();
        result.Limits = options.Limits;
        result.CancellationToken = options.CancellationToken;
        result.ReadMetadata = true;
        result.ReadPreviews = false;
        result.ReadRawPayload = false;
        result.DeferPayloads = true;
        result.Tolerant = options.AllowRecovery;
        result.FileNameHint = fileName == null ? string.Empty : fileName;
        return result;
    }

    internal static void ValidateDocumentModels(RawDocument document, RawValidationOptions options, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (document.FormatDetails == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "Document.FormatDetails", "The format-details model cannot be null.");
        }
        if (document.Images == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "Document.Images", "The image collection cannot be null.");
        }
        else
        {
            int imageIndex = 0;
            while (imageIndex < document.Images.Length)
            {
                if (document.Images[imageIndex] == null)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "Document.Images", "Every image description must be populated.");
                }
                else
                {
                    RawModelValidation.AddReport(messages, document.Images[imageIndex].Validate(options.Limits));
                }
                imageIndex++;
            }
        }
        if (document.Thumbnails == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "Document.Thumbnails", "The thumbnail collection cannot be null.");
        }
        else
        {
            int thumbnailIndex = 0;
            while (thumbnailIndex < document.Thumbnails.Length)
            {
                if (document.Thumbnails[thumbnailIndex] == null)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "Document.Thumbnails", "Every thumbnail must be populated.");
                }
                else
                {
                    RawModelValidation.AddReport(messages, document.Thumbnails[thumbnailIndex].Validate(options.Limits));
                }
                thumbnailIndex++;
            }
        }
        if (document.Diagnostics == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "Document.Diagnostics", "The document diagnostic collection cannot be null.");
        }
        if (document.PendingPreviewEdits == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "Document.PendingPreviewEdits", "The staged preview edit collection cannot be null.");
        }
    }

    internal static void ValidateMetadataModels(RawDocument document, RawValidationOptions options, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (!document.MetadataLoaded || document.Metadata == null)
        {
            return;
        }
        RawMetadata metadata = document.Metadata;
        if (metadata.UnifiedFields == null || metadata.SourceFields == null || metadata.Conflicts == null || metadata.PendingEdits == null || metadata.MakerNotes == null)
        {
            AddConformance(messages, RawValidationCategory.Metadata, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, document.Format, -1, "Metadata.Reconciliation", "Metadata reconciliation collections cannot be null.", false);
            return;
        }
        int index = 0;
        while (index < metadata.Conflicts.Length)
        {
            RawMetadataConflict conflict = metadata.Conflicts[index];
            if (conflict == null || conflict.Name == null || conflict.Name.Length == 0 || conflict.Candidates == null || conflict.Candidates.Length < 2 || conflict.Candidates[0] == null)
            {
                AddConformance(messages, RawValidationCategory.Metadata, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, document.Format, -1, "Metadata.Conflicts", "Every metadata conflict requires a field name and at least two candidates.", false);
            }
            else
            {
                string context = "Metadata." + conflict.Name;
                string message = conflict.Resolved ? "Parallel metadata representations differ; explicit precedence selected one value." : "Parallel metadata representations differ and no value was selected.";
                messages.Add(new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.MetadataConflict, RawSeverity.Warning, document.Format, conflict.Candidates[0].Offset, context, message, false));
            }
            index++;
        }
        index = 0;
        while (index < metadata.MakerNotes.Length)
        {
            RawMakerNote note = metadata.MakerNotes[index];
            if (note == null)
            {
                AddConformance(messages, RawValidationCategory.Metadata, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, document.Format, -1, "Metadata.MakerNotes", "Every maker-note model must be populated.", false);
            }
            else
            {
                AddReportAsCategory(messages, note.Validate(options.Limits), RawValidationCategory.Metadata, document.Format);
            }
            index++;
        }
    }

    internal static RawValidationReport CreateValidationReport(RawValidationOptions options, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        return RawValidationReport.CreateSelected(options.Strict, messages.ToArray(), options.ValidateStructure, options.ValidateCodec, options.ValidateSemantic, options.ValidateProfile, options.ValidateMetadata, options.ValidateWriter);
    }

    internal static RawValidationCategory FirstSelectedCategory(RawValidationOptions options)
    {
        if (options.ValidateStructure) return RawValidationCategory.Structural;
        if (options.ValidateCodec) return RawValidationCategory.Codec;
        if (options.ValidateSemantic) return RawValidationCategory.Semantic;
        if (options.ValidateProfile) return RawValidationCategory.Profile;
        if (options.ValidateMetadata) return RawValidationCategory.Metadata;
        return RawValidationCategory.Writer;
    }

    internal static bool CategorySelected(RawValidationOptions options, RawValidationCategory category)
    {
        if (category == RawValidationCategory.Structural) return options.ValidateStructure;
        if (category == RawValidationCategory.Codec) return options.ValidateCodec;
        if (category == RawValidationCategory.Semantic) return options.ValidateSemantic;
        if (category == RawValidationCategory.Profile) return options.ValidateProfile;
        if (category == RawValidationCategory.Metadata) return options.ValidateMetadata;
        if (category == RawValidationCategory.Writer) return options.ValidateWriter;
        return false;
    }

    internal static void AddConformance(System.Collections.Generic.List<RawValidationMessage> messages, RawValidationCategory category, RawDiagnosticCode code, RawSeverity severity, RawFormat format, long offset, string context, string message, bool recovered)
    {
        messages.Add(new RawValidationMessage(category, code, severity, format, offset, context, message, recovered));
    }

    internal static void AddReportAsCategory(System.Collections.Generic.List<RawValidationMessage> messages, RawValidationReport report, RawValidationCategory category, RawFormat format)
    {
        if (report == null || report.Messages == null)
        {
            return;
        }
        int index = 0;
        while (index < report.Messages.Length)
        {
            RawValidationMessage message = report.Messages[index];
            if (message != null)
            {
                RawFormat selectedFormat = message.Format == RawFormat.Unknown ? format : message.Format;
                AddConformance(messages, category, message.Code, message.Severity, selectedFormat, message.Offset, message.Context, message.Message, message.Recovered);
            }
            index++;
        }
    }

    internal static void AddStructuralReport(System.Collections.Generic.List<RawValidationMessage> messages, RawValidationReport report, RawDocument document, RawValidationOptions options)
    {
        if (report == null || report.Messages == null)
        {
            return;
        }
        int index = 0;
        while (index < report.Messages.Length)
        {
            RawValidationMessage message = report.Messages[index];
            if (message != null && !RecoveredStructuralMessage(document, options, message))
            {
                AddConformance(messages, RawValidationCategory.Structural, message.Code, message.Severity, document.Format, message.Offset, message.Context, message.Message, message.Recovered);
            }
            index++;
        }
    }

    internal static bool RecoveredStructuralMessage(RawDocument document, RawValidationOptions options, RawValidationMessage message)
    {
        if (options.Strict || !options.AllowRecovery || document.Diagnostics == null || message == null || message.Context == null)
        {
            return false;
        }
        int index = 0;
        while (index < document.Diagnostics.Length)
        {
            RawDiagnostic diagnostic = document.Diagnostics[index];
            if (diagnostic != null && diagnostic.Code == RawDiagnosticCode.RecoveryApplied && diagnostic.Recovered && RecoveryCoversContext(diagnostic, message.Context))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool RecoveryCoversContext(RawDiagnostic diagnostic, string context)
    {
        if (diagnostic.IfdPath.Length == 0 || context == null)
        {
            return false;
        }
        if (diagnostic.Stage == "Cr2Parser" && diagnostic.Tag == 513)
        {
            return context == TiffValidator.TagContext(diagnostic.IfdPath, 513) || context == TiffValidator.TagContext(diagnostic.IfdPath, 514);
        }
        if (diagnostic.Tag >= 0 && diagnostic.Tag <= ushort.MaxValue)
        {
            return context == TiffValidator.TagContext(diagnostic.IfdPath, (ushort)diagnostic.Tag);
        }
        return context == diagnostic.IfdPath;
    }

    internal static void ValidateDocumentProfile(RawDocument document, RawValidationOptions options, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (document.ReadProfile == null)
        {
            if (document.ProfileName != null && document.ProfileName.Length != 0)
            {
                AddConformance(messages, RawValidationCategory.Profile, RawDiagnosticCode.InvalidProfile, RawSeverity.Error, document.Format, -1, "Document.Profile", "The selected profile name has no attached read-profile definition.", false);
            }
            return;
        }
        RawReadProfile profile = document.ReadProfile;
        if (document.ProfileName == null || document.ProfileName != profile.Id)
        {
            AddConformance(messages, RawValidationCategory.Profile, RawDiagnosticCode.InvalidProfile, RawSeverity.Error, document.Format, -1, "Document.Profile.Id", "The selected profile name does not match the attached read-profile identifier.", false);
        }
        if (profile.Identity == null || profile.Identity.Format != document.Format)
        {
            AddConformance(messages, RawValidationCategory.Profile, RawDiagnosticCode.InvalidProfile, RawSeverity.Error, document.Format, -1, "Document.Profile.Identity", "The attached read profile does not match the detected format.", false);
        }
        RawProfileSchemaDocument schema = new RawProfileSchemaDocument();
        schema.ReadProfiles = new RawReadProfile[] { profile };
        schema.WriteProfiles = System.Array.Empty<RawWriteProfile>();
        RawModelValidation.AddReport(messages, schema.Validate(options.Limits));
    }

    internal static void ValidateDocumentDiagnostics(RawDocument document, RawValidationOptions options, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (document.Diagnostics == null)
        {
            return;
        }
        int index = 0;
        while (index < document.Diagnostics.Length)
        {
            RawDiagnostic diagnostic = document.Diagnostics[index];
            if (diagnostic != null)
            {
                RawValidationCategory category = DiagnosticCategory(diagnostic.Code);
                if (CategorySelected(options, category))
                {
                    RawDiagnosticCode code = diagnostic.Code;
                    RawSeverity severity = diagnostic.Severity;
                    string message = diagnostic.Message;
                    if (diagnostic.Recovered && diagnostic.Code != RawDiagnosticCode.RecoveryApplied)
                    {
                        code = RawDiagnosticCode.ValidationFailed;
                        severity = RawSeverity.Error;
                        message = "A recovery must use the named RecoveryApplied diagnostic code. " + message;
                    }
                    else if (diagnostic.Recovered && (options.Strict || !options.AllowRecovery))
                    {
                        severity = RawSeverity.Error;
                    }
                    string context = diagnostic.IfdPath.Length == 0 ? diagnostic.Stage : diagnostic.IfdPath;
                    AddConformance(messages, category, code, severity, diagnostic.Format, diagnostic.Offset, context, message, diagnostic.Recovered);
                }
            }
            index++;
        }
    }

    internal static RawValidationCategory DiagnosticCategory(RawDiagnosticCode code)
    {
        if (code == RawDiagnosticCode.InvalidCodecData || code == RawDiagnosticCode.UnsupportedCodec)
        {
            return RawValidationCategory.Codec;
        }
        if (code == RawDiagnosticCode.InvalidProfile || code == RawDiagnosticCode.UnsupportedProfile || code == RawDiagnosticCode.AmbiguousProfile)
        {
            return RawValidationCategory.Profile;
        }
        if (code == RawDiagnosticCode.InvalidMetadata || code == RawDiagnosticCode.MetadataConflict)
        {
            return RawValidationCategory.Metadata;
        }
        if (code >= RawDiagnosticCode.UnknownDataDropped && code <= RawDiagnosticCode.SignatureInvalidated)
        {
            return RawValidationCategory.Writer;
        }
        if (code >= RawDiagnosticCode.UnknownFormat && code <= RawDiagnosticCode.AmbiguousFormat || code == RawDiagnosticCode.RecoveryApplied)
        {
            return RawValidationCategory.Structural;
        }
        return RawValidationCategory.Semantic;
    }

    internal static void ValidateWritePlanProfile(RawWritePlan plan, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (plan.ProfileName == null)
        {
            AddConformance(messages, RawValidationCategory.Profile, RawDiagnosticCode.InvalidProfile, RawSeverity.Error, RawFormat.Unknown, -1, "WritePlan.ProfileName", "The selected write-profile name cannot be null.", false);
        }
        if (plan.Compatibility == null)
        {
            AddConformance(messages, RawValidationCategory.Profile, RawDiagnosticCode.InvalidProfile, RawSeverity.Error, RawFormat.Unknown, -1, "WritePlan.Compatibility", "The write-profile compatibility collection cannot be null.", false);
            return;
        }
        int index = 0;
        while (index < plan.Compatibility.Length)
        {
            RawWriteCompatibilityEntry entry = plan.Compatibility[index];
            if (entry == null || entry.ProfileId == null || entry.Format <= RawFormat.Unknown || entry.Format > RawFormat.Nef || entry.Application == null || entry.Version == null || entry.SampleId == null || entry.Check == null || entry.Details == null || entry.Result < RawProfileEvidenceResult.Unknown || entry.Result > RawProfileEvidenceResult.Failed)
            {
                AddConformance(messages, RawValidationCategory.Profile, RawDiagnosticCode.InvalidProfile, RawSeverity.Error, RawFormat.Unknown, -1, "WritePlan.Compatibility", "Every compatibility row requires a defined format, result, and non-null identity fields.", false);
            }
            else if (plan.ProfileName == null || entry.ProfileId != plan.ProfileName)
            {
                AddConformance(messages, RawValidationCategory.Profile, RawDiagnosticCode.InvalidProfile, RawSeverity.Error, entry.Format, -1, "WritePlan.Compatibility.ProfileId", "Compatibility evidence must identify the selected write profile.", false);
            }
            else
            {
                if (entry.Result != RawProfileEvidenceResult.Passed)
                {
                    AddConformance(messages, RawValidationCategory.Profile, RawDiagnosticCode.UnsupportedProfile, RawSeverity.Warning, entry.Format, -1, "WritePlan.Compatibility.Result", "The compatibility row does not record a passed result.", false);
                }
                if (entry.Experimental)
                {
                    AddConformance(messages, RawValidationCategory.Profile, RawDiagnosticCode.UnsupportedProfile, RawSeverity.Warning, entry.Format, -1, "WritePlan.Compatibility.Experimental", "The selected writer profile remains experimental.", false);
                }
            }
            index++;
        }
    }

    internal static void ValidateWritePlan(RawWritePlan plan, RawValidationOptions options, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        RawFormat format = WritePlanFormat(plan);
        if (plan.PredictedSize < -1 || plan.CanWrite && plan.PredictedSize < 0)
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.WritePlanningFailed, RawSeverity.Error, format, -1, "WritePlan.PredictedSize", "A writable plan requires a nonnegative predicted size.", false);
        }
        if (plan.Changes == null || plan.Diagnostics == null || plan.AuthenticityBlocks == null || plan.PixelValidation == null)
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.WritePlanningFailed, RawSeverity.Error, format, -1, "WritePlan.Collections", "Changes, diagnostics, authenticity blocks, and pixel validation are required.", false);
            return;
        }
        int index = 0;
        while (index < plan.Changes.Length)
        {
            options.CheckCancellation();
            ValidateWriteChange(plan, plan.Changes[index], index, format, messages);
            index++;
        }
        ValidateWriteChangeOverlaps(plan, format, messages);
        index = 0;
        while (index < plan.AuthenticityBlocks.Length)
        {
            RawPayloadBlock block = plan.AuthenticityBlocks[index];
            if (block == null || block.Kind != RawPayloadKind.Authenticity || block.Path == null || block.Offset < 0 || block.Length <= 0 || block.Offset > long.MaxValue - block.Length)
            {
                AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.WritePlanningFailed, RawSeverity.Error, format, -1, "WritePlan.AuthenticityBlocks", "Every authenticity block requires a valid bounded authenticity range.", false);
            }
            index++;
        }
        if (plan.AuthenticityInvalidated && plan.AuthenticityBlocks.Length == 0)
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.SignatureInvalidated, RawSeverity.Error, format, -1, "WritePlan.AuthenticityInvalidated", "Authenticity cannot be marked invalidated when no authenticity block is retained.", false);
        }
        ValidatePixelReport(plan.PixelValidation, format, messages);
        bool diagnosticError = false;
        index = 0;
        while (index < plan.Diagnostics.Length)
        {
            RawDiagnostic diagnostic = plan.Diagnostics[index];
            if (diagnostic == null)
            {
                AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.WritePlanningFailed, RawSeverity.Error, format, -1, "WritePlan.Diagnostics", "Every writer diagnostic must be populated.", false);
                diagnosticError = true;
            }
            else
            {
                if (diagnostic.Severity == RawSeverity.Error)
                {
                    diagnosticError = true;
                }
                string context = diagnostic.IfdPath.Length == 0 ? diagnostic.Stage : diagnostic.IfdPath;
                RawValidationCategory category = DiagnosticCategory(diagnostic.Code) == RawValidationCategory.Profile && options.ValidateProfile ? RawValidationCategory.Profile : RawValidationCategory.Writer;
                AddConformance(messages, category, diagnostic.Code, diagnostic.Severity, diagnostic.Format, diagnostic.Offset, context, diagnostic.Message, diagnostic.Recovered);
            }
            index++;
        }
        if (plan.CanWrite && diagnosticError || !plan.CanWrite && !diagnosticError)
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.WritePlanningFailed, RawSeverity.Error, format, -1, "WritePlan.CanWrite", "The CanWrite result must agree with error diagnostics.", false);
        }
    }

    internal static void ValidateWriteChange(RawWritePlan plan, RawWriteChange change, int index, RawFormat format, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        string context = "WritePlan.Changes[" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
        if (change == null || change.Kind < RawWriteChangeKind.Copied || change.Kind > RawWriteChangeKind.Added || change.Location == null || change.Location.Length == 0 || change.Description == null || change.OriginalOffset < -1 || change.OriginalLength < 0 || change.PlannedOffset < -1 || change.PlannedLength < 0)
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.WritePlanningFailed, RawSeverity.Error, format, -1, context, "Every write change requires a defined kind, location, description, and nonnegative bounded lengths.", false);
            return;
        }
        if (change.OriginalOffset >= 0 && change.OriginalOffset > long.MaxValue - change.OriginalLength || change.PlannedOffset >= 0 && change.PlannedOffset > long.MaxValue - change.PlannedLength)
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, format, -1, context, "A write-change byte range overflows.", false);
        }
        if (change.Kind == RawWriteChangeKind.Added && (change.OriginalOffset != -1 || change.OriginalLength != 0) || change.Kind == RawWriteChangeKind.Dropped && (change.PlannedOffset != -1 || change.PlannedLength != 0))
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.WritePlanningFailed, RawSeverity.Error, format, -1, context, "Added and dropped changes must use their canonical absent-range representation.", false);
        }
        if (plan.CanWrite && change.Kind != RawWriteChangeKind.Dropped && change.PlannedOffset < 0)
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.InvalidOffset, RawSeverity.Error, format, -1, context, "Every emitted change in a writable plan requires an assigned output offset.", false);
        }
        if (plan.PredictedSize >= 0 && change.PlannedOffset >= 0 && (change.PlannedOffset > long.MaxValue - change.PlannedLength || change.PlannedOffset + change.PlannedLength > plan.PredictedSize))
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.InvalidOffset, RawSeverity.Error, format, change.PlannedOffset, context, "The planned change exceeds the predicted output size.", false);
        }
    }

    internal static void ValidateWriteChangeOverlaps(RawWritePlan plan, RawFormat format, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        int left = 0;
        while (left < plan.Changes.Length)
        {
            RawWriteChange first = plan.Changes[left];
            int right = left + 1;
            while (first != null && right < plan.Changes.Length)
            {
                RawWriteChange second = plan.Changes[right];
                if (second != null && RangesOverlap(first.PlannedOffset, first.PlannedLength, second.PlannedOffset, second.PlannedLength))
                {
                    AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.InvalidOffset, RawSeverity.Error, format, second.PlannedOffset, "WritePlan.Changes", "Planned output change ranges overlap.", false);
                }
                right++;
            }
            left++;
        }
    }

    internal static bool RangesOverlap(long firstOffset, long firstLength, long secondOffset, long secondLength)
    {
        if (firstOffset < 0 || firstLength <= 0 || secondOffset < 0 || secondLength <= 0 || firstOffset > long.MaxValue - firstLength || secondOffset > long.MaxValue - secondLength)
        {
            return false;
        }
        return firstOffset < secondOffset + secondLength && secondOffset < firstOffset + firstLength;
    }

    internal static void ValidatePixelReport(RawPixelValidationReport report, RawFormat format, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (report.Kind < RawPixelValidationKind.None || report.Kind > RawPixelValidationKind.ErrorBound || report.Details == null || report.SampleCount < 0 || report.MaximumAbsoluteError < -1 || report.AllowedMaximumAbsoluteError < 0 || report.Required && report.Kind == RawPixelValidationKind.None || report.Performed && !report.Required || report.Passed && !report.Performed)
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.OutputValidationFailed, RawSeverity.Error, format, -1, "WritePlan.PixelValidation", "The decoded pixel-validation state is internally inconsistent.", false);
            return;
        }
        if (report.Performed && (!report.Passed || report.MaximumAbsoluteError > report.AllowedMaximumAbsoluteError))
        {
            AddConformance(messages, RawValidationCategory.Writer, RawDiagnosticCode.OutputValidationFailed, RawSeverity.Error, format, -1, "WritePlan.PixelValidation", "The performed decoded pixel-validation guarantee failed.", false);
        }
    }

    internal static RawFormat WritePlanFormat(RawWritePlan plan)
    {
        if (plan.Compatibility != null)
        {
            int index = 0;
            while (index < plan.Compatibility.Length)
            {
                if (plan.Compatibility[index] != null)
                {
                    return plan.Compatibility[index].Format;
                }
                index++;
            }
        }
        if (plan.Diagnostics != null)
        {
            int index = 0;
            while (index < plan.Diagnostics.Length)
            {
                if (plan.Diagnostics[index] != null)
                {
                    return plan.Diagnostics[index].Format;
                }
                index++;
            }
        }
        return RawFormat.Unknown;
    }

    internal static void ValidateGenericPayload(RawDocument document, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        RawDescriptor? descriptor = document.GenericDescriptor;
        if ((descriptor ?? throw new System.InvalidOperationException("A required value is unavailable.")).Packing != RawPacking.ByteAligned || descriptor.SampleKind == RawSampleKind.Half || descriptor.SampleKind == RawSampleKind.Single || descriptor.BitDepth == descriptor.StoredWordBits)
        {
            return;
        }
        int bytesPerWord = descriptor.StoredWordBits / 8;
        int planeCount = descriptor.Layout == RawSampleLayout.Planar ? descriptor.ChannelCount : 1;
        int samplesPerRow = descriptor.Layout == RawSampleLayout.Planar ? descriptor.Width : descriptor.Width * descriptor.ChannelCount;
        int plane = 0;
        while (plane < planeCount)
        {
            int row = 0;
            while (row < descriptor.Height)
            {
                long rowOffset = descriptor.DataOffset + (long)plane * descriptor.PlaneStrideBytes + (long)row * descriptor.RowStrideBytes;
                int sample = 0;
                while (sample < samplesPerRow)
                {
                    long sampleOffset = rowOffset + (long)sample * bytesPerWord;
                    System.ReadOnlySpan<byte> word = document.Source.Span.Slice((int)sampleOffset, bytesPerWord);
                    bool invalid = false;
                    if (descriptor.SampleKind == RawSampleKind.UnsignedInteger)
                    {
                        invalid = GenericRawCodec.ReadUnsignedWord(word, descriptor.StoredWordBits, descriptor.ByteOrder) > GenericRawCodec.MaximumUnsigned(descriptor.BitDepth);
                    }
                    else
                    {
                        int value = GenericRawCodec.ReadSignedWord(word, descriptor.StoredWordBits, descriptor.ByteOrder);
                        invalid = value < GenericRawCodec.MinimumSigned(descriptor.BitDepth) || value > GenericRawCodec.MaximumSigned(descriptor.BitDepth);
                    }
                    if (invalid)
                    {
                        messages.Add(new RawValidationMessage(RawValidationCategory.Codec, RawDiagnosticCode.InvalidCodecData, RawSeverity.Error, RawFormat.GenericRaw, sampleOffset, "GenericRaw/Samples", "A byte-aligned sample exceeds the declared meaningful precision.", false));
                        return;
                    }
                    sample++;
                }
                row++;
            }
            plane++;
        }
    }
}
