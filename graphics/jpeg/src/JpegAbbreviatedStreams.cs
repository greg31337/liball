namespace jpeg;

internal class JpegTableSetCodec
{
    internal JpegTableSet ReadTableStream(byte[] data)
    {
        JpegInfo info = JpegIdentifier.Identify(data);
        if (!info.IsAbbreviatedTableStream)
        {
            throw JpegInvalidDataException.Create("The JPEG data is not an abbreviated table-only stream.");
        }

        JpegTableSet tables = new JpegTableSet();
        tables.QuantizationTables = info.QuantizationTables;
        tables.HuffmanTables = info.HuffmanTables;
        tables.ArithmeticConditioningTables = info.ArithmeticConditioningTables;
        return tables;
    }

    internal byte[] WriteTableStream(JpegTableSet tables)
    {
        JpegTableStore store = CreateTableStore(tables);
        if (CountTables(store) == 0)
        {
            throw JpegWriteValidationException.Create("An abbreviated table stream must contain at least one table.");
        }

        JpegMarkerSegmentWriter writer = new JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        if (store.QuantizationTables.Count > 0)
        {
            new JpegQuantizationTableWriter().WriteMarkerSegment(writer, store.QuantizationTables);
        }

        if (store.HuffmanTables.Count > 0)
        {
            new JpegHuffmanTableWriter().WriteMarkerSegment(writer, store.HuffmanTables);
        }

        if (store.ArithmeticConditioningTables.Count > 0)
        {
            new JpegArithmeticConditioningTableWriter().WriteMarkerSegment(writer, store.ArithmeticConditioningTables);
        }

        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    internal JpegTableStore CreateTableStore(JpegTableSet tables)
    {
        ValidateTableSet(tables);
        JpegTableStore store = new JpegTableStore();
        if (tables.QuantizationTables.Count > 0)
        {
            byte[] payload = new JpegQuantizationTableWriter().WritePayloads(tables.QuantizationTables);
            store.QuantizationTables = new JpegQuantizationTableParser().Parse(payload);
        }

        if (tables.HuffmanTables.Count > 0)
        {
            byte[] payload = new JpegHuffmanTableWriter().WritePayloads(tables.HuffmanTables);
            store.HuffmanTables = new JpegHuffmanTableParser().Parse(payload);
        }

        if (tables.ArithmeticConditioningTables.Count > 0)
        {
            byte[] payload = new JpegArithmeticConditioningTableWriter().WritePayloads(tables.ArithmeticConditioningTables);
            store.ArithmeticConditioningTables = new JpegArithmeticConditioningTableParser().Parse(payload);
        }

        return store;
    }

    internal JpegReadOptions CreateReadOptions(JpegReadOptions source, JpegTableSet tables)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("options");
        }

        JpegReadOptions copy = new JpegReadOptions();
        copy.MetadataPolicy = source.MetadataPolicy;
        copy.ColorPolicy = source.ColorPolicy;
        copy.IccBehaviorPolicy = source.IccBehaviorPolicy;
        copy.ErrorPolicy = source.ErrorPolicy;
        copy.OutputPixelFormat = source.OutputPixelFormat;
        copy.ChromaUpsamplingMode = source.ChromaUpsamplingMode;
        copy.RawComponentOutput = source.RawComponentOutput;
        copy.UnknownColorHandling = source.UnknownColorHandling;
        copy.AdobeInversionMode = source.AdobeInversionMode;
        copy.YCbCrRange = source.YCbCrRange;
        copy.InverseDctMethod = source.InverseDctMethod;
        copy.IdctScaleNumerator = source.IdctScaleNumerator;
        copy.IdctScaleDenominator = source.IdctScaleDenominator;
        copy.IdctScaleRatio = source.IdctScaleRatio;
        copy.TargetWidth = source.TargetWidth;
        copy.TargetHeight = source.TargetHeight;
        copy.DecodeRegion = source.DecodeRegion;
        copy.FastMcuCrop = source.FastMcuCrop;
        copy.MaxWidth = source.MaxWidth;
        copy.MaxHeight = source.MaxHeight;
        copy.MaxComponents = source.MaxComponents;
        copy.MaxScans = source.MaxScans;
        copy.MaxMarkerPayloadBytes = source.MaxMarkerPayloadBytes;
        copy.MaxRetainedMarkers = source.MaxRetainedMarkers;
        copy.MaxTables = source.MaxTables;
        copy.MaxMetadataBytes = source.MaxMetadataBytes;
        copy.MaxEntropyBytes = source.MaxEntropyBytes;
        copy.MaxOutputPixels = source.MaxOutputPixels;
        copy.MaxOutputBytes = source.MaxOutputBytes;
        copy.MaxComponentBufferBytes = source.MaxComponentBufferBytes;
        copy.MaxProgressiveStateBytes = source.MaxProgressiveStateBytes;
        copy.MaxCallbackOutputBytes = source.MaxCallbackOutputBytes;
        copy.AutoOrient = source.AutoOrient;
        copy.SkipMetadata = source.SkipMetadata;
        copy.TableStore = CreateTableStore(tables);
        return copy;
    }

    internal int CountTables(JpegTableStore store)
    {
        return store.QuantizationTables.Count + store.HuffmanTables.Count + store.ArithmeticConditioningTables.Count;
    }

    internal void ValidateTableSet(JpegTableSet tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (tables.QuantizationTables == null)
        {
            throw new System.ArgumentException("Quantization table collection cannot be null.", "tables");
        }

        if (tables.HuffmanTables == null)
        {
            throw new System.ArgumentException("Huffman table collection cannot be null.", "tables");
        }

        if (tables.ArithmeticConditioningTables == null)
        {
            throw new System.ArgumentException("Arithmetic-conditioning table collection cannot be null.", "tables");
        }
    }
}

internal class JpegTableOperations
{
    internal JpegTableSet ExtractFromBytes(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JpegInfo info = JpegIdentifier.Identify(data);
        if (!info.SawStartOfImage)
        {
            throw JpegInvalidDataException.Create("Table extraction requires marker-based JPEG data.");
        }

        JpegTableSet source = new JpegTableSet();
        source.QuantizationTables = info.QuantizationTables;
        source.HuffmanTables = info.HuffmanTables;
        source.ArithmeticConditioningTables = info.ArithmeticConditioningTables;
        return CopyTableSet(source);
    }

    internal byte[] InjectIntoBytes(byte[] data, JpegTableSet tables)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JpegInspectionResult inspection = new JpegMarkerInspectionReader().InspectBytes(
            data,
            new JpegReadOptions(),
            default(System.Threading.CancellationToken));
        JpegTableSetCodec codec = new JpegTableSetCodec();
        JpegTableStore store = codec.CreateTableStore(tables);
        if (codec.CountTables(store) == 0)
        {
            throw JpegWriteValidationException.Create("Table injection requires at least one replacement table.");
        }

        byte[] definitions = CreateDefinitionBytes(store);
        int insertionOffset = FindInsertionOffset(inspection, data.Length);
        return ReplaceDefinitionSegments(data, inspection, definitions, insertionOffset);
    }

    internal JpegTableSet ExtractFromCoefficientImage(JpegCoefficientImage image)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        JpegTableSet source = new JpegTableSet();
        source.QuantizationTables = image.QuantizationTables;
        source.HuffmanTables = image.HuffmanTables;
        source.ArithmeticConditioningTables = image.ArithmeticConditioningTables;
        return CopyTableSet(source);
    }

    internal JpegCoefficientImage InjectIntoCoefficientImage(JpegCoefficientImage image, JpegTableSet tables)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        JpegTableSet copy = CopyTableSet(tables);
        image.QuantizationTables = copy.QuantizationTables;
        image.HuffmanTables = copy.HuffmanTables;
        image.ArithmeticConditioningTables = copy.ArithmeticConditioningTables;
        return image;
    }

    internal JpegTableSet CopyTableSet(JpegTableSet source)
    {
        JpegTableStore store = new JpegTableSetCodec().CreateTableStore(source);
        JpegTableSet copy = new JpegTableSet();
        copy.QuantizationTables = store.QuantizationTables;
        copy.HuffmanTables = store.HuffmanTables;
        copy.ArithmeticConditioningTables = store.ArithmeticConditioningTables;
        return copy;
    }

    internal byte[] CreateDefinitionBytes(JpegTableStore store)
    {
        JpegMarkerSegmentWriter writer = new JpegMarkerSegmentWriter();
        if (store.QuantizationTables.Count > 0)
        {
            new JpegQuantizationTableWriter().WriteMarkerSegment(writer, store.QuantizationTables);
        }

        if (store.HuffmanTables.Count > 0)
        {
            new JpegHuffmanTableWriter().WriteMarkerSegment(writer, store.HuffmanTables);
        }

        if (store.ArithmeticConditioningTables.Count > 0)
        {
            new JpegArithmeticConditioningTableWriter().WriteMarkerSegment(writer, store.ArithmeticConditioningTables);
        }

        return writer.ToArray();
    }

    internal int FindInsertionOffset(JpegInspectionResult inspection, int dataLength)
    {
        foreach (JpegMarkerSegment segment in inspection.Markers)
        {
            if (IsTableDefinition(segment))
            {
                return ValidateOffset(segment.MarkerOffset, dataLength);
            }
        }

        foreach (JpegMarkerSegment segment in inspection.Markers)
        {
            if (IsFrameOrScan(segment) || segment.Marker == JpegMarker.EndOfImage)
            {
                return ValidateOffset(segment.MarkerOffset, dataLength);
            }
        }

        foreach (JpegMarkerSegment segment in inspection.Markers)
        {
            if (segment.Marker == JpegMarker.StartOfImage)
            {
                return ValidateOffset(segment.EndOffset, dataLength);
            }
        }

        throw JpegInvalidDataException.Create("JPEG start-of-image marker was not found.");
    }

    internal byte[] ReplaceDefinitionSegments(
        byte[] data,
        JpegInspectionResult inspection,
        byte[] definitions,
        int insertionOffset)
    {
        JpegByteWriter writer = JpegByteWriter.WithCapacity(data.Length + definitions.Length);
        writer.WriteBytesRange(data, 0, insertionOffset);
        writer.WriteBytes(definitions);
        int sourceOffset = insertionOffset;
        foreach (JpegMarkerSegment segment in inspection.Markers)
        {
            if (!IsTableDefinition(segment))
            {
                continue;
            }

            int markerOffset = ValidateOffset(segment.MarkerOffset, data.Length);
            int endOffset = ValidateOffset(segment.EndOffset, data.Length);
            if (markerOffset < sourceOffset)
            {
                continue;
            }

            writer.WriteBytesRange(data, sourceOffset, markerOffset - sourceOffset);
            sourceOffset = endOffset;
        }

        writer.WriteBytesRange(data, sourceOffset, data.Length - sourceOffset);
        return writer.ToArray();
    }

    internal int ValidateOffset(long offset, int dataLength)
    {
        if (offset < 0 || offset > dataLength)
        {
            throw JpegInvalidDataException.Create("JPEG marker offset is outside the source data.");
        }

        return (int)offset;
    }

    internal bool IsTableDefinition(JpegMarkerSegment segment)
    {
        return segment.Marker == JpegMarker.DefineQuantizationTable
            || segment.Marker == JpegMarker.DefineHuffmanTable
            || segment.Marker == JpegMarker.DefineArithmeticConditioning;
    }

    internal bool IsFrameOrScan(JpegMarkerSegment segment)
    {
        int value = segment.Value;
        if (value >= JpegConstants.MarkerStartOfFrame0 && value <= JpegConstants.MarkerStartOfFrame3)
        {
            return true;
        }

        if (value >= JpegConstants.MarkerStartOfFrame5 && value <= JpegConstants.MarkerStartOfFrame7)
        {
            return true;
        }

        if (value >= JpegConstants.MarkerStartOfFrame9 && value <= JpegConstants.MarkerStartOfFrame11)
        {
            return true;
        }

        if (value >= JpegConstants.MarkerStartOfFrame13 && value <= JpegConstants.MarkerStartOfFrame15)
        {
            return true;
        }

        return segment.Marker == JpegMarker.StartOfScan;
    }
}
