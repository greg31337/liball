using System.Collections.Generic;

namespace jpeg;

internal class JpegIdentifier
{
    internal JpegMarkerSegmentReader SegmentReader = new JpegMarkerSegmentReader(new byte[0]);
    internal JpegRawMarkerCollection RawMarkers = new JpegRawMarkerCollection();
    internal JpegInfo Info = new JpegInfo();
    internal JpegReadOptions Options = new JpegReadOptions();
    internal JpegDifferentialProcess DifferentialProcess = new JpegDifferentialProcess();
    internal JpegHierarchicalMode HierarchicalMode = new JpegHierarchicalMode();
    internal JpegLsMode JpegLsMode = new JpegLsMode();
    internal JpegSmartScaleMode SmartScaleMode = new JpegSmartScaleMode();
    internal JpegFormatDetector FormatDetector = new JpegFormatDetector();
    internal List<JpegMarkerSegment> InspectedMarkers = new List<JpegMarkerSegment>();
    internal long EntropyBytesRead;
    internal System.Threading.CancellationToken CancellationTokenValue;

    internal JpegIdentifier(byte[] data)
    {
        SegmentReader = new JpegMarkerSegmentReader(data);
        RawMarkers = new JpegRawMarkerCollection();
        Info = new JpegInfo();
        Options = new JpegReadOptions();
        DifferentialProcess = new JpegDifferentialProcess();
        HierarchicalMode = new JpegHierarchicalMode();
        JpegLsMode = new JpegLsMode();
        SmartScaleMode = new JpegSmartScaleMode();
        FormatDetector = new JpegFormatDetector();
        InspectedMarkers = new List<JpegMarkerSegment>();
        EntropyBytesRead = 0;
        CancellationTokenValue = default(System.Threading.CancellationToken);
    }

    internal static JpegIdentifier Create(byte[] data, JpegReadOptions options)
    {
        new JpegReadOptionsValidator().Validate(options);

        JpegIdentifier identifier = new JpegIdentifier(data);
        identifier.Options = options;
        return identifier;
    }

    internal static JpegIdentifier FromStream(System.IO.Stream stream)
    {
        JpegIdentifier identifier = new JpegIdentifier(new byte[0]);
        identifier.SegmentReader = JpegMarkerSegmentReader.FromStream(stream);
        return identifier;
    }

    internal static JpegIdentifier FromStream(System.IO.Stream stream, JpegReadOptions options)
    {
        new JpegReadOptionsValidator().Validate(options);

        JpegIdentifier identifier = FromStream(stream);
        identifier.Options = options;
        return identifier;
    }

    internal static JpegInfo Identify(byte[] data)
    {
        JpegIdentifier identifier = new JpegIdentifier(data);
        return identifier.Read();
    }

    internal static JpegInfo Identify(byte[] data, JpegReadOptions options)
    {
        JpegIdentifier identifier = Create(data, options);
        return identifier.Read();
    }

    internal static JpegInfo Identify(System.IO.Stream stream)
    {
        JpegIdentifier identifier = FromStream(stream);
        return identifier.Read();
    }

    internal static JpegInfo Identify(System.IO.Stream stream, JpegReadOptions options)
    {
        JpegIdentifier identifier = FromStream(stream, options);
        return identifier.Read();
    }

    internal static JpegInfo IdentifyBytesWithCancellation(byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        JpegIdentifier identifier = new JpegIdentifier(data);
        identifier.CancellationTokenValue = cancellationToken;
        return identifier.Read();
    }

    internal static JpegInfo IdentifyBytesWithOptionsAndCancellation(byte[] data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        JpegIdentifier identifier = Create(data, options);
        identifier.CancellationTokenValue = cancellationToken;
        return identifier.Read();
    }

    internal static JpegInfo IdentifyStreamWithCancellation(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        JpegIdentifier identifier = FromStream(stream);
        identifier.CancellationTokenValue = cancellationToken;
        return identifier.Read();
    }

    internal static JpegInfo IdentifyStreamWithOptionsAndCancellation(System.IO.Stream stream, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        JpegIdentifier identifier = FromStream(stream, options);
        identifier.CancellationTokenValue = cancellationToken;
        return identifier.Read();
    }

    internal JpegInfo Read()
    {
        CheckCancellation();
        SegmentReader.MaxMarkerPayloadBytes = Options.MaxMarkerPayloadBytes;
        Info.FormatFamily = FormatDetector.Detect(SegmentReader.Scanner.Reader);
        CheckCancellation();
        if (Info.FormatFamily != JpegFormatFamily.ClassicJpeg)
        {
            if (Info.FormatFamily == JpegFormatFamily.Unknown)
            {
                Info.FormatSupport = JpegSupportStatus.Unknown;
                Info.CodingProcessSupport = JpegSupportStatus.Unknown;
            }
            else
            {
                Info.FormatSupport = JpegSupportStatus.RecognizedButUnsupported;
                Info.CodingProcessSupport = JpegSupportStatus.Unknown;
            }

            return Info;
        }

        Info.FormatSupport = JpegSupportStatus.Supported;
        Info.CodingProcessSupport = JpegSupportStatus.Unknown;
        JpegMarkerSegment soi = SegmentReader.ReadStartOfImage();
        InspectedMarkers.Add(soi);
        Info.SawStartOfImage = true;
        ProcessSegment(soi);

        bool inEntropyData = false;
        while (!Info.SawEndOfImage)
        {
            CheckCancellation();
            JpegMarkerSegment segment;
            if (inEntropyData)
            {
                segment = SegmentReader.ReadEntropyMarkerSegment();
                RecordEntropyBytes(segment);
            }
            else
            {
                segment = SegmentReader.ReadNextSegment();
            }

            InspectedMarkers.Add(segment);
            ValidateMarkerOrder(segment, inEntropyData);
            ProcessSegment(segment);
            CheckCancellation();
            if (segment.Value == JpegConstants.MarkerEndOfImage)
            {
                break;
            }

            if (segment.Value == JpegConstants.MarkerStartOfScan)
            {
                inEntropyData = true;
                continue;
            }

            if (segment.Value >= JpegConstants.MarkerRestart0 && segment.Value <= JpegConstants.MarkerRestart7)
            {
                if (inEntropyData)
                {
                    inEntropyData = true;
                }
                else
                {
                    inEntropyData = false;
                }

                continue;
            }

            if (segment.Value == JpegConstants.MarkerDefineNumberOfLines && segment.FoundAfterEntropyData)
            {
                inEntropyData = true;
                continue;
            }

            inEntropyData = false;
        }

        CheckCancellation();
        ResolveColorHints();
        ApplyMetadataPolicy();
        return Info;
    }

    internal void ResolveColorHints()
    {
        JpegColorHintResolver resolver = new JpegColorHintResolver();
        Info.ColorHints = resolver.Resolve(Info);
        Info.ColorSpace = Info.ColorHints.EncodedColorSpace;
        Info.ColorSpaceSource = Info.ColorHints.EncodedColorSpaceSource;
    }

    internal void CheckCancellation()
    {
        CancellationTokenValue.ThrowIfCancellationRequested();
    }

    internal void ProcessSegment(JpegMarkerSegment segment)
    {
        if (ShouldPreserveRawMarkerSegment(segment))
        {
            EnforceRetainedMarkerLimit();
            JpegRawMarker raw = RawMarkers.CreateRawMarker(segment);
            RawMarkers.Markers.Add(raw);
            RawMarkers.TotalPayloadBytes = RawMarkers.TotalPayloadBytes + raw.PayloadLength;
            Info.Metadata.RawMarkers.Add(raw);
            Info.MetadataBytes = Info.MetadataBytes + raw.PayloadLength;
            EnforceMetadataLimit();
        }

        if (segment.Value >= JpegConstants.MarkerApp0 && segment.Value <= JpegConstants.MarkerApp15)
        {
            Info.AppSegmentCount = Info.AppSegmentCount + 1;
        }

        bool parseMetadata = ShouldParseMetadata();
        if (parseMetadata && segment.Value == JpegConstants.MarkerApp0)
        {
            ParseJfif(segment.Payload);
            ParseJfxx(segment.Payload);
        }

        if (parseMetadata && segment.Value == JpegConstants.MarkerApp1)
        {
            ParseExif(segment.Payload);
            ParseXmp(segment.Payload);
            ParseExtendedXmp(segment.Payload);
        }

        if (segment.Value == JpegConstants.MarkerApp14)
        {
            ParseAdobe(segment.Payload);
        }

        if (parseMetadata && segment.Value == JpegConstants.MarkerApp13)
        {
            ParsePhotoshop(segment.Payload);
        }

        if (parseMetadata && segment.Value == JpegConstants.MarkerApp2)
        {
            ParseIccProfileChunk(segment.Payload);
            ParseMpf(segment.Payload);
        }

        if (segment.Value == JpegConstants.MarkerComment)
        {
            Info.CommentCount = Info.CommentCount + 1;
            if (parseMetadata)
            {
                ParseComment(segment.Payload);
            }
        }

        if (segment.Value == JpegConstants.MarkerDefineQuantizationTable)
        {
            Info.QuantizationTableCount = Info.QuantizationTableCount + CountQuantizationTables(segment.Payload);
            EnforceTableLimit();
        }

        if (segment.Value == JpegConstants.MarkerDefineHuffmanTable)
        {
            Info.HuffmanTableCount = Info.HuffmanTableCount + CountHuffmanTables(segment.Payload);
            EnforceTableLimit();
        }

        if (segment.Value == JpegConstants.MarkerDefineArithmeticConditioning)
        {
            Info.ArithmeticConditioningTableCount = Info.ArithmeticConditioningTableCount + CountArithmeticConditioningTables(segment.Payload);
            EnforceTableLimit();
        }

        if (segment.Value == JpegConstants.MarkerDefineRestartInterval)
        {
            ParseRestartInterval(segment.Payload);
        }

        if (segment.Value == JpegConstants.MarkerDefineNumberOfLines)
        {
            ParseDefinedNumberOfLines(segment.Payload);
        }

        if (segment.Value == JpegConstants.MarkerDefineHierarchicalProgression)
        {
            ParseDefineHierarchicalProgression(segment);
        }

        if (segment.Value == JpegConstants.MarkerExpandReferenceComponents)
        {
            ParseExpandReferenceComponents(segment);
        }

        if (segment.Value == JpegConstants.MarkerJpegExtension)
        {
            ParseSmartScaleMarker(segment);
        }

        if (segment.Value >= JpegConstants.MarkerRestart0 && segment.Value <= JpegConstants.MarkerRestart7)
        {
            RecordRestartMarker(segment);
        }

        if (IsStartOfFrameMarker(segment.Value))
        {
            ParseFrameHeader(segment);
        }

        if (segment.Value == JpegConstants.MarkerStartOfScan)
        {
            ParseScanHeader(segment);
        }

        if (segment.Value == JpegConstants.MarkerEndOfImage)
        {
            if (IsAbbreviatedTableEnd())
            {
                Info.IsAbbreviatedTableStream = true;
            }

            if (parseMetadata)
            {
                ReassembleIccProfileChunks();
                ReassembleExtendedXmpChunks();
            }

            ApplySuppliedTablesForAbbreviatedImage();
            Info.SawEndOfImage = true;
        }
    }

    internal bool ShouldParseMetadata()
    {
        if (Options.SkipMetadata)
        {
            return false;
        }

        JpegMetadataPolicyFilter filter = new JpegMetadataPolicyFilter();
        return filter.ShouldParseMetadata(Options.MetadataPolicy);
    }

    internal bool ShouldPreserveRawMarkerSegment(JpegMarkerSegment segment)
    {
        if (segment == null)
        {
            throw new System.ArgumentNullException("segment");
        }

        if (Options.SkipMetadata)
        {
            return false;
        }

        if (!RawMarkers.ShouldPreserve(segment))
        {
            return false;
        }

        JpegMetadataPolicyFilter filter = new JpegMetadataPolicyFilter();
        return filter.ShouldKeepPayload(segment.Value, segment.Payload, Options.MetadataPolicy);
    }

    internal void ApplyMetadataPolicy()
    {
        if (Options.SkipMetadata)
        {
            Info.Metadata = new JpegMetadata();
            Info.MetadataBytes = 0;
            return;
        }

        JpegMetadataPolicyFilter filter = new JpegMetadataPolicyFilter();
        Info.Metadata = filter.CreateMetadataForPolicy(Info.Metadata, Options.MetadataPolicy);
        Info.MetadataBytes = filter.CalculateRawPayloadBytes(Info.Metadata);
    }

    internal void ValidateMarkerOrder(JpegMarkerSegment segment, bool inEntropyData)
    {
        if (Options.ErrorPolicy != JpegErrorPolicy.Strict && Options.ErrorPolicy != JpegErrorPolicy.Tolerant)
        {
            return;
        }

        if (segment.Value == JpegConstants.MarkerStartOfImage)
        {
            ReportMarkerOrderIssue("SOI marker appeared after the start of the JPEG stream.", segment);
        }

        if (segment.Value >= JpegConstants.MarkerRestart0 && segment.Value <= JpegConstants.MarkerRestart7)
        {
            if (!inEntropyData)
            {
                ReportMarkerOrderIssue("Restart marker appeared outside entropy-coded data.", segment);
            }
        }

        if (IsStartOfFrameMarker(segment.Value))
        {
            if (Info.SawStartOfFrame)
            {
                ReportMarkerOrderIssue("Multiple SOF markers are not allowed in strict mode.", segment);
            }

            if (Info.SawStartOfScan)
            {
                ReportMarkerOrderIssue("SOF marker appeared after SOS in strict mode.", segment);
            }
        }

        if (segment.Value == JpegConstants.MarkerStartOfScan && !Info.SawStartOfFrame)
        {
            ReportMarkerOrderIssue("SOS marker appeared before a SOF marker.", segment);
        }

        if (segment.Value == JpegConstants.MarkerEndOfImage && !Info.SawStartOfScan)
        {
            if (!IsAbbreviatedTableEnd())
            {
                ReportMarkerOrderIssue("EOI marker appeared before image scan data.", segment);
            }
        }
    }

    internal void ReportMarkerOrderIssue(string message, JpegMarkerSegment segment)
    {
        if (Options.ErrorPolicy == JpegErrorPolicy.Strict)
        {
            throw JpegInvalidDataException.Create(message);
        }

        if (Options.ErrorPolicy == JpegErrorPolicy.Tolerant)
        {
            AddWarning(JpegDiagnosticCode.InvalidMarkerOrder, message, segment);
        }
    }

    internal void AddWarning(JpegDiagnosticCode code, string message, JpegMarkerSegment segment)
    {
        if (code == JpegDiagnosticCode.UnsupportedProcess)
        {
            Info.CodingProcessSupport = JpegSupportStatus.RecognizedButUnsupported;
        }

        JpegDiagnostic diagnostic = Info.DiagnosticCollection.AddWarning(code, message, segment.MarkerOffset, segment.Marker);
        Info.Diagnostics.Add(diagnostic);
    }

    internal bool IsAbbreviatedTableEnd()
    {
        if (Info.SawStartOfFrame)
        {
            return false;
        }

        if (Info.SawStartOfScan)
        {
            return false;
        }

        return HasTableDefinitions();
    }

    internal bool HasTableDefinitions()
    {
        if (Info.QuantizationTableCount > 0)
        {
            return true;
        }

        if (Info.HuffmanTableCount > 0)
        {
            return true;
        }

        if (Info.ArithmeticConditioningTableCount > 0)
        {
            return true;
        }

        return false;
    }

    internal void ParseJfif(byte[] payload)
    {
        JpegJfifMetadataParser parser = new JpegJfifMetadataParser();
        if (!parser.IsJfif(payload))
        {
            return;
        }

        JpegJfifMetadata jfif = parser.Parse(payload);
        Info.Metadata.Jfif = jfif;
        Info.Metadata.HasJfif = true;
        Info.Metadata.JfifMajorVersion = jfif.MajorVersion;
        Info.Metadata.JfifMinorVersion = jfif.MinorVersion;
        Info.Metadata.DensityUnit = jfif.DensityUnit;
        Info.Metadata.XDensity = jfif.XDensity;
        Info.Metadata.YDensity = jfif.YDensity;
        Info.Metadata.HasDensityDpi = jfif.HasDensityDpi;
        Info.Metadata.XDpi = jfif.XDpi;
        Info.Metadata.YDpi = jfif.YDpi;
    }

    internal void ParseJfxx(byte[] payload)
    {
        JpegJfxxMetadataParser parser = new JpegJfxxMetadataParser();
        if (!parser.IsJfxx(payload))
        {
            return;
        }

        JpegJfxxMetadata jfxx = parser.Parse(payload);
        Info.Metadata.Jfxx.Add(jfxx);
        Info.Metadata.HasJfxx = true;
    }

    internal void ParseAdobe(byte[] payload)
    {
        JpegAdobeMetadataParser parser = new JpegAdobeMetadataParser();
        if (!parser.IsAdobe(payload))
        {
            return;
        }

        JpegAdobeMetadata adobe = parser.Parse(payload);
        Info.Metadata.Adobe = adobe;
        Info.Metadata.HasAdobe = true;
        Info.Metadata.AdobeVersion = adobe.Version;
        Info.Metadata.AdobeTransform = adobe.Transform;
        if (Info.SawStartOfFrame)
        {
            ApplyAdobeInterpretation();
        }
    }

    internal void ParseExif(byte[] payload)
    {
        JpegExifMetadataParser parser = new JpegExifMetadataParser();
        if (!parser.IsExif(payload))
        {
            return;
        }

        JpegExifMetadata exif = parser.Parse(payload);
        Info.Metadata.ExifMetadata = exif;
        Info.Metadata.HasExif = true;
        Info.Metadata.Exif = exif.RawPayload;
    }

    internal void ParseXmp(byte[] payload)
    {
        JpegXmpMetadataParser parser = new JpegXmpMetadataParser();
        if (!parser.IsXmp(payload))
        {
            return;
        }

        JpegXmpMetadata xmp = parser.Parse(payload);
        Info.Metadata.XmpMetadata = xmp;
        Info.Metadata.HasXmp = true;
        Info.Metadata.Xmp = xmp.RawPayload;
    }

    internal void ParseExtendedXmp(byte[] payload)
    {
        JpegExtendedXmpChunkParser parser = new JpegExtendedXmpChunkParser();
        if (!parser.IsExtendedXmp(payload))
        {
            return;
        }

        JpegExtendedXmpChunk chunk = parser.Parse(payload);
        Info.Metadata.ExtendedXmpChunks.Add(chunk);
        Info.Metadata.HasExtendedXmpChunks = true;
    }

    internal void ReassembleExtendedXmpChunks()
    {
        if (!Info.Metadata.HasExtendedXmpChunks)
        {
            return;
        }

        JpegExtendedXmpAssembler assembler = new JpegExtendedXmpAssembler();
        JpegExtendedXmpMetadata metadata = assembler.Inspect(Info.Metadata.ExtendedXmpChunks);
        Info.Metadata.ExtendedXmpMetadata = metadata;
        if (metadata.IsComplete)
        {
            Info.Metadata.ExtendedXmp = metadata.PacketData;
            Info.Metadata.HasExtendedXmp = true;
        }
    }

    internal void ParseMpf(byte[] payload)
    {
        JpegMpfMetadataParser parser = new JpegMpfMetadataParser();
        if (!parser.IsMpf(payload))
        {
            return;
        }

        JpegMpfMetadata mpf = parser.Parse(payload);
        Info.Metadata.MpfMetadata = mpf;
        Info.Metadata.HasMpf = true;
        Info.Metadata.Mpf = mpf.RawPayload;
    }

    internal void ParsePhotoshop(byte[] payload)
    {
        JpegPhotoshopMetadataParser parser = new JpegPhotoshopMetadataParser();
        if (!parser.IsPhotoshop(payload))
        {
            return;
        }

        JpegPhotoshopMetadata photoshop = parser.Parse(payload);
        Info.Metadata.Photoshop = photoshop;
        Info.Metadata.HasPhotoshop = true;
        if (photoshop.HasIptc)
        {
            Info.Metadata.HasIptc = true;
            Info.Metadata.Iptc = photoshop.IptcData;
        }
    }

    internal void ApplyAdobeInterpretation()
    {
        JpegAdobeMetadataParser parser = new JpegAdobeMetadataParser();
        parser.ApplyComponentInterpretation(Info.Metadata.Adobe, Info.ComponentCount);
        Info.ColorSpace = Info.Metadata.Adobe.ColorSpace;
        Info.AdobeInvertsCmyk = Info.Metadata.Adobe.InvertsCmyk;
    }

    internal void ParseComment(byte[] payload)
    {
        JpegCommentParser parser = new JpegCommentParser();
        string comment = parser.Parse(payload);
        Info.Metadata.Comments.Add(comment);
    }

    internal void ParseIccProfileChunk(byte[] payload)
    {
        JpegIccProfileChunkParser parser = new JpegIccProfileChunkParser();
        if (!parser.IsIccProfile(payload))
        {
            return;
        }

        JpegIccProfileChunk chunk = parser.Parse(payload);
        Info.Metadata.IccProfileChunks.Add(chunk);
        Info.Metadata.HasIccProfileChunks = true;
    }

    internal void ValidateIccProfileChunks()
    {
        if (!Info.Metadata.HasIccProfileChunks)
        {
            return;
        }

        JpegIccProfileChunkParser parser = new JpegIccProfileChunkParser();
        parser.ValidateSequence(Info.Metadata.IccProfileChunks);
    }

    internal void ReassembleIccProfileChunks()
    {
        if (!Info.Metadata.HasIccProfileChunks)
        {
            return;
        }

        JpegIccProfileAssembler assembler = new JpegIccProfileAssembler();
        Info.Metadata.IccProfile = assembler.Reassemble(Info.Metadata.IccProfileChunks);
        Info.Metadata.HasIccProfile = true;
        JpegIccProfileParser profileParser = new JpegIccProfileParser();
        if (profileParser.IsProfile(Info.Metadata.IccProfile))
        {
            Info.Metadata.Icc = profileParser.Parse(Info.Metadata.IccProfile);
            if (Info.Metadata.Icc.HasCicp)
            {
                Info.Metadata.HasCicp = true;
                Info.Metadata.Cicp = Info.Metadata.Icc.Cicp;
            }
        }
    }

    internal bool IsAbbreviatedImageCandidate()
    {
        if (!Info.SawStartOfFrame)
        {
            return false;
        }

        if (!Info.SawStartOfScan)
        {
            return false;
        }

        return !HasTableDefinitions();
    }

    internal void ApplySuppliedTablesForAbbreviatedImage()
    {
        if (!IsAbbreviatedImageCandidate())
        {
            return;
        }

        JpegTableStore store = Options.TableStore;
        if (store == null)
        {
            return;
        }

        if (!HasSuppliedTables(store))
        {
            return;
        }

        CopySuppliedQuantizationTables(store);
        CopySuppliedHuffmanTables(store);
        CopySuppliedArithmeticConditioningTables(store);
        EnforceTableLimit();
        Info.IsAbbreviatedImageStream = true;
        Info.UsedSuppliedTables = true;
    }

    internal bool HasSuppliedTables(JpegTableStore store)
    {
        if (store.QuantizationTables != null && store.QuantizationTables.Count > 0)
        {
            return true;
        }

        if (store.HuffmanTables != null && store.HuffmanTables.Count > 0)
        {
            return true;
        }

        if (store.ArithmeticConditioningTables != null && store.ArithmeticConditioningTables.Count > 0)
        {
            return true;
        }

        return false;
    }

    internal void CopySuppliedQuantizationTables(JpegTableStore store)
    {
        if (store.QuantizationTables == null)
        {
            return;
        }

        foreach (JpegQuantizationTable table in store.QuantizationTables)
        {
            Info.QuantizationTables.Add(table);
        }

        Info.QuantizationTableCount = Info.QuantizationTables.Count;
    }

    internal void CopySuppliedHuffmanTables(JpegTableStore store)
    {
        if (store.HuffmanTables == null)
        {
            return;
        }

        foreach (JpegHuffmanTable table in store.HuffmanTables)
        {
            Info.HuffmanTables.Add(table);
        }

        Info.HuffmanTableCount = Info.HuffmanTables.Count;
    }

    internal void CopySuppliedArithmeticConditioningTables(JpegTableStore store)
    {
        if (store.ArithmeticConditioningTables == null)
        {
            return;
        }

        foreach (JpegArithmeticConditioningTable table in store.ArithmeticConditioningTables)
        {
            Info.ArithmeticConditioningTables.Add(table);
        }

        Info.ArithmeticConditioningTableCount = Info.ArithmeticConditioningTables.Count;
    }

    internal int CountQuantizationTables(byte[] payload)
    {
        JpegQuantizationTableParser parser = new JpegQuantizationTableParser();
        List<JpegQuantizationTable> tables = parser.Parse(payload);
        foreach (JpegQuantizationTable table in tables)
        {
            StoreQuantizationTable(table);
        }

        return tables.Count;
    }

    internal void StoreQuantizationTable(JpegQuantizationTable table)
    {
        int index = 0;
        while (index < Info.QuantizationTables.Count)
        {
            if (Info.QuantizationTables[index].Identifier == table.Identifier)
            {
                Info.QuantizationTables[index] = table;
                return;
            }

            index = index + 1;
        }

        Info.QuantizationTables.Add(table);
    }

    internal int CountHuffmanTables(byte[] payload)
    {
        JpegHuffmanTableParser parser = new JpegHuffmanTableParser();
        List<JpegHuffmanTable> tables = parser.Parse(payload);
        foreach (JpegHuffmanTable table in tables)
        {
            StoreHuffmanTable(table);
        }

        return tables.Count;
    }

    internal void StoreHuffmanTable(JpegHuffmanTable table)
    {
        int index = 0;
        while (index < Info.HuffmanTables.Count)
        {
            JpegHuffmanTable current = Info.HuffmanTables[index];
            if (current.TableClass == table.TableClass && current.Identifier == table.Identifier)
            {
                Info.HuffmanTables[index] = table;
                return;
            }

            index = index + 1;
        }

        Info.HuffmanTables.Add(table);
    }

    internal int CountArithmeticConditioningTables(byte[] payload)
    {
        JpegArithmeticConditioningTableParser parser = new JpegArithmeticConditioningTableParser();
        List<JpegArithmeticConditioningTable> tables = parser.Parse(payload);
        foreach (JpegArithmeticConditioningTable table in tables)
        {
            StoreArithmeticConditioningTable(table);
        }

        return tables.Count;
    }

    internal void StoreArithmeticConditioningTable(JpegArithmeticConditioningTable table)
    {
        int index = 0;
        while (index < Info.ArithmeticConditioningTables.Count)
        {
            JpegArithmeticConditioningTable current = Info.ArithmeticConditioningTables[index];
            if (current.TableClass == table.TableClass && current.Identifier == table.Identifier)
            {
                Info.ArithmeticConditioningTables[index] = table;
                return;
            }

            index = index + 1;
        }

        Info.ArithmeticConditioningTables.Add(table);
    }

    internal void ParseRestartInterval(byte[] payload)
    {
        JpegRestartIntervalParser parser = new JpegRestartIntervalParser();
        Info.RestartInterval = parser.Parse(payload);
        Info.ResolvedRestartInterval = Info.RestartInterval;
        Info.HasRestartInterval = true;
        Info.ExpectedRestartMarkerIndex = 0;
    }

    internal void RecordRestartMarker(JpegMarkerSegment segment)
    {
        int markerIndex = segment.Value - JpegConstants.MarkerRestart0;
        Info.RestartMarkerCount = Info.RestartMarkerCount + 1;
        Info.LastRestartMarkerIndex = markerIndex;
        Info.ExpectedRestartMarkerIndex = (markerIndex + 1) & 7;
    }

    internal void ParseDefinedNumberOfLines(byte[] payload)
    {
        JpegNumberOfLinesParser parser = new JpegNumberOfLinesParser();
        Info.DefinedNumberOfLines = parser.Parse(payload);
        Info.HasDefinedNumberOfLines = true;
        if (Info.Height == 0)
        {
            Info.Height = Info.DefinedNumberOfLines;
            EstimateOutputSize();
            EnforceFrameLimits();
        }
    }

    internal void ParseDefineHierarchicalProgression(JpegMarkerSegment segment)
    {
        Info.HasHierarchicalProgression = true;
        Info.HierarchicalProgressionMarkerCount = Info.HierarchicalProgressionMarkerCount + 1;
        ReportUnsupportedHierarchicalMarker(segment);
    }

    internal void ParseExpandReferenceComponents(JpegMarkerSegment segment)
    {
        Info.ExpandReferenceComponentMarkerCount = Info.ExpandReferenceComponentMarkerCount + 1;
        ReportUnsupportedHierarchicalMarker(segment);
    }

    internal void ReportUnsupportedHierarchicalMarker(JpegMarkerSegment segment)
    {
        string message = HierarchicalMode.GetUnsupportedMessage(segment.Value);
        AddWarning(JpegDiagnosticCode.UnsupportedProcess, message, segment);
    }

    internal void ParseSmartScaleMarker(JpegMarkerSegment segment)
    {
        Info.HasSmartScale = true;
        Info.SmartScaleMarkerCount = Info.SmartScaleMarkerCount + 1;
        AddWarning(JpegDiagnosticCode.UnsupportedProcess, SmartScaleMode.GetUnsupportedMessage(), segment);
    }

    internal void ParseFrameHeader(JpegMarkerSegment segment)
    {
        JpegFrameHeaderParser parser = new JpegFrameHeaderParser();
        JpegFrameHeader frame = parser.Parse(segment.Value, segment.Payload);
        Info.Frame = frame;
        Info.Components = frame.Components;
        Info.Precision = frame.Precision;
        Info.Height = frame.Height;
        Info.Width = frame.Width;
        Info.ComponentCount = frame.ComponentCount;
        Info.Process = frame.Process;
        if (Info.CodingProcessSupport == JpegSupportStatus.Unknown)
        {
            Info.CodingProcessSupport = JpegSupportStatus.Supported;
        }
        Info.Progressive = frame.Progressive;
        Info.ArithmeticCoding = frame.ArithmeticCoding;
        ReportUnsupportedFrameProcess(frame, segment);
        ReportUnsupportedJpegLsFrame(frame, segment);
        Info.ColorSpace = InferColorSpace(frame.ComponentCount);
        if (Info.Metadata.HasAdobe)
        {
            ApplyAdobeInterpretation();
        }

        Info.Subsampling = InferSubsamplingFromFrame(frame);
        Info.SawStartOfFrame = true;
        EstimateOutputSize();
        EnforceFrameLimits();
    }

    internal void ReportUnsupportedJpegLsFrame(JpegFrameHeader frame, JpegMarkerSegment segment)
    {
        if (!JpegLsMode.IsJpegLsMarker(frame.Value))
        {
            return;
        }

        Info.IsJpegLs = true;
        Info.FormatFamily = JpegFormatFamily.JpegLs;
        Info.FormatSupport = JpegSupportStatus.RecognizedButUnsupported;
        Info.JpegLsMarkerCount = Info.JpegLsMarkerCount + 1;
        AddWarning(JpegDiagnosticCode.UnsupportedProcess, JpegLsMode.GetUnsupportedMessage(), segment);
    }

    internal void ReportUnsupportedFrameProcess(JpegFrameHeader frame, JpegMarkerSegment segment)
    {
        if (!DifferentialProcess.RequiresCorpusBeforeDecoderSupport(frame.Process))
        {
            return;
        }

        string message = DifferentialProcess.GetUnsupportedMessage(frame.Process);
        AddWarning(JpegDiagnosticCode.UnsupportedProcess, message, segment);
    }

    internal void ParseScanHeader(JpegMarkerSegment segment)
    {
        JpegScanHeaderParser parser = new JpegScanHeaderParser();
        JpegScanHeader scan = parser.Parse(segment.Payload);
        ValidateScanComponents(scan);
        Info.LastScanHeader = scan;
        Info.ScanHeaders.Add(scan);
        Info.ScanCount = Info.ScanCount + 1;
        Info.LastScanComponentCount = scan.ComponentCount;
        Info.LastScanSpectralStart = scan.SpectralStart;
        Info.LastScanSpectralEnd = scan.SpectralEnd;
        Info.LastScanSuccessiveHigh = scan.SuccessiveHigh;
        Info.LastScanSuccessiveLow = scan.SuccessiveLow;
        Info.SawStartOfScan = true;
        EnforceScanLimit();
    }

    internal void ValidateScanComponents(JpegScanHeader scan)
    {
        if (!Info.SawStartOfFrame)
        {
            return;
        }

        foreach (JpegScanComponent component in scan.Components)
        {
            if (!FrameComponentExists(component.Selector))
            {
                throw JpegInvalidDataException.Create("SOS component selector does not match any SOF component.");
            }
        }
    }

    internal bool FrameComponentExists(int selector)
    {
        foreach (JpegFrameComponent component in Info.Components)
        {
            if (component.Identifier == selector)
            {
                return true;
            }
        }

        return false;
    }

    internal void EstimateOutputSize()
    {
        long pixels = (long)Info.Width * (long)Info.Height;
        long bytesPerSample = 1;
        if (Info.Precision > 8)
        {
            bytesPerSample = 2;
        }

        Info.EstimatedOutputPixels = pixels;
        Info.EstimatedOutputBytes = pixels * (long)Info.ComponentCount * bytesPerSample;
    }

    internal void EnforceFrameLimits()
    {
        if (Options.MaxWidth >= 0 && Info.Width > Options.MaxWidth)
        {
            throw JpegResourceLimitException.Create("JPEG image width exceeds the configured maximum width.");
        }

        if (Options.MaxHeight >= 0 && Info.Height > Options.MaxHeight)
        {
            throw JpegResourceLimitException.Create("JPEG image height exceeds the configured maximum height.");
        }

        if (Options.MaxComponents >= 0 && Info.ComponentCount > Options.MaxComponents)
        {
            throw JpegResourceLimitException.Create("JPEG component count exceeds the configured maximum component count.");
        }

        if (Options.MaxOutputPixels >= 0 && Info.EstimatedOutputPixels > Options.MaxOutputPixels)
        {
            throw JpegResourceLimitException.Create("JPEG output pixel count exceeds the configured maximum output pixels.");
        }

        if (Options.MaxOutputBytes >= 0 && Info.EstimatedOutputBytes > Options.MaxOutputBytes)
        {
            throw JpegResourceLimitException.Create("JPEG output byte estimate exceeds the configured maximum output bytes.");
        }
    }

    internal void EnforceScanLimit()
    {
        if (Options.MaxScans >= 0 && Info.ScanCount > Options.MaxScans)
        {
            throw JpegResourceLimitException.Create("JPEG scan count exceeds the configured maximum scan count.");
        }
    }

    internal void EnforceMetadataLimit()
    {
        if (Options.MaxMetadataBytes >= 0 && Info.MetadataBytes > Options.MaxMetadataBytes)
        {
            throw JpegResourceLimitException.Create("JPEG metadata bytes exceed the configured maximum metadata bytes.");
        }
    }

    internal void RecordEntropyBytes(JpegMarkerSegment segment)
    {
        if (segment == null)
        {
            throw new System.ArgumentNullException("segment");
        }

        EntropyBytesRead = EntropyBytesRead + segment.EntropyBytesSkipped;
        if (Options.MaxEntropyBytes >= 0 && EntropyBytesRead > Options.MaxEntropyBytes)
        {
            throw JpegResourceLimitException.Create("JPEG entropy bytes exceed the configured maximum entropy bytes.");
        }
    }

    internal void EnforceRetainedMarkerLimit()
    {
        if (Options.MaxRetainedMarkers >= 0 && Info.Metadata.RawMarkers.Count >= Options.MaxRetainedMarkers)
        {
            throw JpegResourceLimitException.Create("JPEG retained marker count exceeds the configured maximum retained markers.");
        }
    }

    internal void EnforceTableLimit()
    {
        long tableCount = (long)Info.QuantizationTables.Count + (long)Info.HuffmanTables.Count + (long)Info.ArithmeticConditioningTables.Count;
        if (Options.MaxTables >= 0 && tableCount > Options.MaxTables)
        {
            throw JpegResourceLimitException.Create("JPEG table count exceeds the configured maximum tables.");
        }
    }

    internal bool IsStartOfFrameMarker(int markerValue)
    {
        if (markerValue >= JpegConstants.MarkerStartOfFrame0 && markerValue <= JpegConstants.MarkerStartOfFrame3)
        {
            return true;
        }

        if (markerValue >= JpegConstants.MarkerStartOfFrame5 && markerValue <= JpegConstants.MarkerStartOfFrame7)
        {
            return true;
        }

        if (markerValue >= JpegConstants.MarkerStartOfFrame9 && markerValue <= JpegConstants.MarkerStartOfFrame11)
        {
            return true;
        }

        if (markerValue >= JpegConstants.MarkerStartOfFrame13 && markerValue <= JpegConstants.MarkerStartOfFrame15)
        {
            return true;
        }

        if (markerValue == JpegConstants.MarkerJpegLs)
        {
            return true;
        }

        return false;
    }

    internal bool IsProgressiveProcess(int markerValue)
    {
        return markerValue == JpegConstants.MarkerStartOfFrame2
            || markerValue == JpegConstants.MarkerStartOfFrame6
            || markerValue == JpegConstants.MarkerStartOfFrame10
            || markerValue == JpegConstants.MarkerStartOfFrame14;
    }

    internal bool IsDifferentialProcess(int markerValue)
    {
        return DifferentialProcess.IsDifferentialMarker(markerValue);
    }

    internal bool IsArithmeticProcess(int markerValue)
    {
        return markerValue >= JpegConstants.MarkerStartOfFrame9 && markerValue <= JpegConstants.MarkerStartOfFrame15;
    }

    internal JpegColorSpace InferColorSpace(int componentCount)
    {
        if (componentCount == 1)
        {
            return JpegColorSpace.Grayscale;
        }

        if (componentCount == 3)
        {
            return JpegColorSpace.YCbCr;
        }

        if (componentCount == 4)
        {
            return JpegColorSpace.CMYK;
        }

        return JpegColorSpace.Unknown;
    }

    internal JpegSubsampling InferSubsampling(byte[] framePayload, int componentCount)
    {
        if (componentCount == 1)
        {
            return JpegSubsampling.Grayscale;
        }

        if (componentCount < 3)
        {
            return JpegSubsampling.Unknown;
        }

        int sampling = framePayload[7];
        int horizontal = sampling >> 4;
        int vertical = sampling & 15;
        if (horizontal == 1 && vertical == 1)
        {
            return JpegSubsampling.Ratio444;
        }

        if (horizontal == 2 && vertical == 1)
        {
            return JpegSubsampling.Ratio422;
        }

        if (horizontal == 2 && vertical == 2)
        {
            return JpegSubsampling.Ratio420;
        }

        if (horizontal == 1 && vertical == 2)
        {
            return JpegSubsampling.Ratio440;
        }

        if (horizontal == 4 && vertical == 1)
        {
            return JpegSubsampling.Ratio411;
        }

        return JpegSubsampling.Custom;
    }

    internal JpegSubsampling InferSubsamplingFromFrame(JpegFrameHeader frame)
    {
        if (frame.ComponentCount == 1)
        {
            return JpegSubsampling.Grayscale;
        }

        if (frame.ComponentCount < 3 || frame.Components.Count == 0)
        {
            return JpegSubsampling.Unknown;
        }

        int componentIndex = 1;
        while (componentIndex < frame.Components.Count)
        {
            JpegFrameComponent remaining = frame.Components[componentIndex];
            if (remaining.HorizontalSamplingFactor != 1 || remaining.VerticalSamplingFactor != 1)
            {
                return JpegSubsampling.Custom;
            }

            componentIndex = componentIndex + 1;
        }

        JpegFrameComponent component = frame.Components[0];
        int horizontal = component.HorizontalSamplingFactor;
        int vertical = component.VerticalSamplingFactor;
        if (horizontal == 1 && vertical == 1)
        {
            return JpegSubsampling.Ratio444;
        }

        if (horizontal == 2 && vertical == 1)
        {
            return JpegSubsampling.Ratio422;
        }

        if (horizontal == 2 && vertical == 2)
        {
            return JpegSubsampling.Ratio420;
        }

        if (horizontal == 1 && vertical == 2)
        {
            return JpegSubsampling.Ratio440;
        }

        if (horizontal == 4 && vertical == 1)
        {
            return JpegSubsampling.Ratio411;
        }

        return JpegSubsampling.Custom;
    }
}
