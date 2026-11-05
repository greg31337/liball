namespace svg;

/// <summary>Reconstructs bounded WOFF and WOFF2 containers into standard SFNT bytes.</summary>
public class SvgWebFontDecoder : ISvgWebFontDecoder
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgWebFontDecoder()
    {
    }
    static readonly string[] KnownTags = new string[]
    {
        "cmap", "head", "hhea", "hmtx", "maxp", "name", "OS/2", "post",
        "cvt ", "fpgm", "glyf", "loca", "prep", "CFF ", "VORG", "EBDT",
        "EBLC", "gasp", "hdmx", "kern", "LTSH", "PCLT", "VDMX", "vhea",
        "vmtx", "BASE", "GDEF", "GPOS", "GSUB", "EBSC", "JSTF", "MATH",
        "CBDT", "CBLC", "COLR", "CPAL", "SVG ", "sbix", "acnt", "avar",
        "bdat", "bloc", "bsln", "cvar", "fdsc", "feat", "fmtx", "fvar",
        "gvar", "hsty", "just", "lcar", "mort", "morx", "opbd", "prop",
        "trak", "Zapf", "Silf", "Glat", "Gloc", "Feat", "Sill"
    };

    /// <summary>Reconstructs one web font using bounded defaults.</summary>
    /// <param name="bytes">The complete WOFF or WOFF2 bytes.</param>
    /// <returns>The reconstructed SFNT bytes or a stable failure.</returns>
    public SvgWebFontDecodeResult DecodeWithBytes(byte[] bytes)
    {
        return new SvgWebFontDecoder().DecodeWithBytesAndOptions(bytes, new SvgOpenTypeReadOptions());
    }

    /// <summary>Reconstructs one web font using explicit font limits and cancellation.</summary>
    /// <param name="bytes">The complete WOFF or WOFF2 bytes.</param>
    /// <param name="options">The bounded OpenType read options.</param>
    /// <returns>The reconstructed SFNT bytes or a stable failure.</returns>
    public SvgWebFontDecodeResult DecodeWithBytesAndOptions(byte[]? bytes, SvgOpenTypeReadOptions options)
    {
        if (bytes == null || !new SvgWebFontDecoder().ValidOptions(options)) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidArgument, SvgWebFontFormat.Unknown, "The web-font bytes or options are invalid.");
        if (bytes.LongLength > options.Limits.MaxBytesPerFont) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.LimitExceeded, SvgWebFontFormat.Unknown, "The web-font container exceeds MaxBytesPerFont.");
        SvgWebFontFormat format = new SvgWebFontDecoder().Detect(bytes);
        if (format == SvgWebFontFormat.Unknown) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The input is not a WOFF or WOFF2 container.");
        System.Threading.Tasks.Task<SvgWebFontDecodeResult> task = System.Threading.Tasks.Task.Run(() => new SvgWebFontDecoder().DecodeUnsafe(bytes, options, format));
        System.IAsyncResult pending = task;
        pending.AsyncWaitHandle.WaitOne();
        if (task.IsFaulted)
        {
            System.AggregateException observed = task.Exception;
            if (observed != null) observed.GetBaseException();
            return new SvgWebFontDecoder().Failure(SvgOpenTypeError.DecompressionFailed, format, "The web-font compressed data is malformed or truncated.");
        }
        if (task.IsCanceled || options.CancellationToken.IsCancellationRequested) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.Cancelled, format, "Web-font decoding was cancelled.");
        return task.Result;
    }
SvgWebFontDecodeResult DecodeUnsafe(byte[] bytes, SvgOpenTypeReadOptions options, SvgWebFontFormat format)
    {
        if (options.CancellationToken.IsCancellationRequested) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.Cancelled, format, "Web-font decoding was cancelled.");
        if (format == SvgWebFontFormat.Woff) return new SvgWebFontDecoder().DecodeWoff(bytes, options);
        return new SvgWebFontDecoder().DecodeWoff2(bytes, options);
    }
SvgWebFontDecodeResult DecodeWoff(byte[] bytes, SvgOpenTypeReadOptions options)
    {
        SvgWebFontFormat format = SvgWebFontFormat.Woff;
        if (bytes.Length < 44) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The WOFF header is truncated.");
        uint flavor = new SvgWebFontDecoder().ReadUInt32(bytes, 4);
        uint declaredLength = new SvgWebFontDecoder().ReadUInt32(bytes, 8);
        int tableCount = new SvgWebFontDecoder().ReadUInt16(bytes, 12);
        int reserved = new SvgWebFontDecoder().ReadUInt16(bytes, 14);
        uint totalSfntSize = new SvgWebFontDecoder().ReadUInt32(bytes, 16);
        uint metadataOffset = new SvgWebFontDecoder().ReadUInt32(bytes, 24);
        uint metadataLength = new SvgWebFontDecoder().ReadUInt32(bytes, 28);
        uint metadataOriginalLength = new SvgWebFontDecoder().ReadUInt32(bytes, 32);
        uint privateOffset = new SvgWebFontDecoder().ReadUInt32(bytes, 36);
        uint privateLength = new SvgWebFontDecoder().ReadUInt32(bytes, 40);
        if (!new SvgWebFontDecoder().ValidFlavor(flavor)) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.UnsupportedVersion, format, "The WOFF flavor is unsupported.");
        if (declaredLength != bytes.Length || reserved != 0) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The WOFF length or reserved header field is invalid.");
        if (tableCount < 1) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "The WOFF table directory is empty.");
        if (tableCount > options.Limits.MaxTablesPerFont) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.LimitExceeded, format, "The WOFF table count exceeds MaxTablesPerFont.");
        long directoryEndValue = 44L + 20L * tableCount;
        if (directoryEndValue > bytes.Length) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "The WOFF table directory is truncated.");
        int directoryEnd = (int)directoryEndValue;
        System.Collections.Generic.List<SvgWebFontTable> tables = new System.Collections.Generic.List<SvgWebFontTable>();
        string preceding = "";
        long expectedSfnt = 12L + 16L * tableCount;
        int index = 0;
        while (index < tableCount)
        {
            int record = 44 + index * 20;
            string tag = new SvgWebFontDecoder().ReadTag(bytes, record);
            if (!new SvgWebFontDecoder().ValidTag(tag) || preceding.Length != 0 && string.CompareOrdinal(preceding, tag) >= 0) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "WOFF table tags are invalid, duplicated, or unsorted.");
            uint sourceOffset = new SvgWebFontDecoder().ReadUInt32(bytes, record + 4);
            uint compressedLength = new SvgWebFontDecoder().ReadUInt32(bytes, record + 8);
            uint originalLength = new SvgWebFontDecoder().ReadUInt32(bytes, record + 12);
            if (sourceOffset > System.Int32.MaxValue || compressedLength > System.Int32.MaxValue || originalLength > System.Int32.MaxValue || compressedLength > originalLength) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "A WOFF table length or offset is invalid.");
            SvgWebFontTable table = new SvgWebFontTable(tag);
            table.SourceOffset = (int)sourceOffset;
            table.SourceLength = (int)compressedLength;
            table.OriginalLength = (int)originalLength;
            table.OriginalChecksum = new SvgWebFontDecoder().ReadUInt32(bytes, record + 16);
            table.Order = index;
            tables.Add(table);
            int padded = new SvgWebFontDecoder().Align4(table.OriginalLength);
            if (padded < 0) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.LimitExceeded, format, "The reconstructed WOFF size exceeds the supported address space.");
            expectedSfnt = expectedSfnt + padded;
            if (expectedSfnt > options.Limits.MaxBytesPerFont || expectedSfnt > System.Int32.MaxValue) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.LimitExceeded, format, "The reconstructed WOFF font exceeds MaxBytesPerFont.");
            preceding = tag;
            index = index + 1;
        }
        if (totalSfntSize != expectedSfnt || totalSfntSize % 4 != 0) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The WOFF totalSfntSize is inconsistent.");
        System.Collections.Generic.List<SvgWebFontTable> physical = new System.Collections.Generic.List<SvgWebFontTable>(tables);
        physical.Sort((first, second) => first.SourceOffset.CompareTo(second.SourceOffset));
        int cursor = directoryEnd;
        index = 0;
        while (index < physical.Count)
        {
            SvgWebFontTable table = physical[index];
            if (table.SourceOffset != cursor || (long)table.SourceOffset + table.SourceLength > bytes.Length) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "WOFF table ranges contain a gap, overlap, or out-of-range value.");
            if (!new SvgWebFontDecoder().ZeroPadding(bytes, table.SourceOffset + table.SourceLength, new SvgWebFontDecoder().Align4(table.SourceOffset + table.SourceLength))) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "WOFF table padding is invalid.");
            cursor = new SvgWebFontDecoder().Align4(table.SourceOffset + table.SourceLength);
            table.Order = index;
            index = index + 1;
        }
        SvgWebFontDecodeResult? blockResult = new SvgWebFontDecoder().ValidateOptionalBlocks(bytes, cursor, metadataOffset, metadataLength, metadataOriginalLength, privateOffset, privateLength, format);
        if (blockResult != null) return blockResult;
        index = 0;
        while (index < tables.Count)
        {
            if (options.CancellationToken.IsCancellationRequested) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.Cancelled, format, "Web-font decoding was cancelled.");
            SvgWebFontTable table = tables[index];
            if (table.SourceLength == table.OriginalLength)
            {
                table.Data = new byte[table.OriginalLength];
                System.Array.Copy(bytes, table.SourceOffset, table.Data, 0, table.OriginalLength);
            }
            else
            {
                table.Data = new SvgWebFontDecoder().Decompress(bytes, table.SourceOffset, table.SourceLength, table.OriginalLength, false, options);
                if (table.Data == null)
                {
                    if (options.CancellationToken.IsCancellationRequested) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.Cancelled, format, "Web-font decoding was cancelled.");
                    return new SvgWebFontDecoder().Failure(SvgOpenTypeError.DecompressionFailed, format, "A WOFF table could not be decompressed to its declared length.");
                }
            }
            uint checksum = new SvgWebFontDecoder().Checksum(table.Data, 0, table.Data.Length, table.Tag == "head");
            if (checksum != table.OriginalChecksum) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidChecksum, format, "A reconstructed WOFF table checksum is invalid.");
            index = index + 1;
        }
        return new SvgWebFontDecoder().BuildSfnt(tables, flavor, options, format);
    }
SvgWebFontDecodeResult DecodeWoff2(byte[] bytes, SvgOpenTypeReadOptions options)
    {
        SvgWebFontFormat format = SvgWebFontFormat.Woff2;
        if (bytes.Length < 48) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The WOFF2 header is truncated.");
        uint flavor = new SvgWebFontDecoder().ReadUInt32(bytes, 4);
        uint declaredLength = new SvgWebFontDecoder().ReadUInt32(bytes, 8);
        int tableCount = new SvgWebFontDecoder().ReadUInt16(bytes, 12);
        uint compressedLength = new SvgWebFontDecoder().ReadUInt32(bytes, 20);
        uint metadataOffset = new SvgWebFontDecoder().ReadUInt32(bytes, 28);
        uint metadataLength = new SvgWebFontDecoder().ReadUInt32(bytes, 32);
        uint metadataOriginalLength = new SvgWebFontDecoder().ReadUInt32(bytes, 36);
        uint privateOffset = new SvgWebFontDecoder().ReadUInt32(bytes, 40);
        uint privateLength = new SvgWebFontDecoder().ReadUInt32(bytes, 44);
        if (flavor == 1953784678) return new SvgWebFontDecoder().Unsupported(format, "", "WOFF2 font collections require caller fallback.");
        if (!new SvgWebFontDecoder().ValidFlavor(flavor)) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.UnsupportedVersion, format, "The WOFF2 flavor is unsupported.");
        if (declaredLength != bytes.Length) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The WOFF2 declared length is invalid.");
        if (tableCount < 1) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "The WOFF2 table directory is empty.");
        if (tableCount > options.Limits.MaxTablesPerFont) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.LimitExceeded, format, "The WOFF2 table count exceeds MaxTablesPerFont.");
        SvgWebFontCursor cursor = new SvgWebFontCursor(bytes, 48, bytes.Length - 48);
        System.Collections.Generic.List<SvgWebFontTable> tables = new System.Collections.Generic.List<SvgWebFontTable>();
        long transformedSize = 0;
        int index = 0;
        while (index < tableCount)
        {
            int flags = cursor.ReadByte();
            if (flags < 0) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "The WOFF2 table directory is truncated.");
            int tagIndex = flags & 63;
            string? tag = tagIndex == 63 ? cursor.ReadTag() : KnownTags[tagIndex];
            if (!new SvgWebFontDecoder().ValidTag(tag) || new SvgWebFontDecoder().FindTable(tables, tag) != null) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "A WOFF2 table tag is invalid or duplicated.");
            int version = flags >> 6;
            bool transformed;
            if (tag == "glyf" || tag == "loca")
            {
                if (version != 0 && version != 3) return new SvgWebFontDecoder().Unsupported(format, tag, "The WOFF2 glyph transform version is unsupported.");
                transformed = version == 0;
            }
            else if (tag == "hmtx")
            {
                if (version != 0 && version != 1) return new SvgWebFontDecoder().Unsupported(format, tag, "The WOFF2 horizontal-metrics transform version is unsupported.");
                transformed = version == 1;
            }
            else
            {
                if (version != 0) return new SvgWebFontDecoder().Unsupported(format, tag, "The WOFF2 table transform is unsupported.");
                transformed = false;
            }
            SvgWebFontNumber original = new SvgWebFontDecoder().ReadBase128(cursor);
            if (!original.Valid) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "A WOFF2 original table length is malformed.");
            SvgWebFontNumber source = transformed ? new SvgWebFontDecoder().ReadBase128(cursor) : original;
            if (!source.Valid) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "A WOFF2 transformed table length is malformed.");
            if (tag == "loca" && transformed && source.Value != 0) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "A transformed WOFF2 loca table must consume zero bytes.");
            SvgWebFontTable table = new SvgWebFontTable(tag);
            table.OriginalLength = original.Value;
            table.SourceLength = source.Value;
            table.TransformVersion = version;
            table.Transformed = transformed;
            table.SourceOffset = (int)transformedSize;
            table.Order = index;
            transformedSize = transformedSize + source.Value;
            if (transformedSize > options.Limits.MaxBytesPerFont || transformedSize > System.Int32.MaxValue) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.LimitExceeded, format, "The WOFF2 decompressed data exceeds MaxBytesPerFont.");
            tables.Add(table);
            index = index + 1;
        }
        if (compressedLength > System.Int32.MaxValue || (long)cursor.Position + compressedLength > bytes.Length) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The WOFF2 compressed data range is invalid.");
        int compressedOffset = cursor.Position;
        int afterCompressed = new SvgWebFontDecoder().Align4(compressedOffset + (int)compressedLength);
        if (afterCompressed < 0 || !new SvgWebFontDecoder().ZeroPadding(bytes, compressedOffset + (int)compressedLength, afterCompressed)) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The WOFF2 compressed-data padding is invalid.");
        SvgWebFontDecodeResult? blockResult = new SvgWebFontDecoder().ValidateOptionalBlocks(bytes, afterCompressed, metadataOffset, metadataLength, metadataOriginalLength, privateOffset, privateLength, format);
        if (blockResult != null) return blockResult;
        byte[]? transformedBytes = new SvgWebFontDecoder().Decompress(bytes, compressedOffset, (int)compressedLength, (int)transformedSize, true, options);
        if (transformedBytes == null)
        {
            if (options.CancellationToken.IsCancellationRequested) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.Cancelled, format, "Web-font decoding was cancelled.");
            return new SvgWebFontDecoder().Failure(SvgOpenTypeError.DecompressionFailed, format, "The WOFF2 Brotli stream did not decode to the directory size.");
        }
        index = 0;
        while (index < tables.Count)
        {
            SvgWebFontTable table = tables[index];
            table.Data = new byte[table.SourceLength];
            if (table.SourceLength != 0) System.Array.Copy(transformedBytes, table.SourceOffset, table.Data, 0, table.SourceLength);
            index = index + 1;
        }
        SvgWebFontDecodeResult? transformResult = new SvgWebFontDecoder().ReconstructWoff2Transforms(tables, options, format);
        if (transformResult != null) return transformResult;
        return new SvgWebFontDecoder().BuildSfnt(tables, flavor, options, format);
    }
SvgWebFontDecodeResult? ReconstructWoff2Transforms(System.Collections.Generic.List<SvgWebFontTable> tables, SvgOpenTypeReadOptions options, SvgWebFontFormat format)
    {
        SvgWebFontTable? glyf = new SvgWebFontDecoder().FindTable(tables, "glyf");
        SvgWebFontTable? loca = new SvgWebFontDecoder().FindTable(tables, "loca");
        SvgWoff2GlyphResult? glyphResult = null;
        if (glyf != null || loca != null)
        {
            if (glyf == null || loca == null || glyf.Transformed != loca.Transformed) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "WOFF2 glyf and loca tables must form a matching pair.");
            if (glyf.Transformed)
            {
                if (loca.Order != glyf.Order + 1) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "Transformed WOFF2 loca must immediately follow glyf.");
                glyphResult = SvgWoff2GlyphDecoder.Decode(glyf.Data, loca.OriginalLength, options);
                if (!glyphResult.Success) return new SvgWebFontDecoder().Failure(glyphResult.Error, format, glyphResult.Message);
                SvgWebFontTable? head = new SvgWebFontDecoder().FindTable(tables, "head");
                SvgWebFontTable? maxp = new SvgWebFontDecoder().FindTable(tables, "maxp");
                if (head == null || maxp == null || (head.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 54 || (maxp.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 6) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidTable, format, "Transformed WOFF2 glyphs require valid head and maxp tables.");
                int declaredGlyphCount = maxp.Data[4] << 8 | maxp.Data[5];
                int declaredIndexFormat = head.Data[50] << 8 | head.Data[51];
                if (declaredIndexFormat >= 32768) declaredIndexFormat = declaredIndexFormat - 65536;
                if (glyphResult.GlyphCount != declaredGlyphCount || glyphResult.IndexFormat != declaredIndexFormat) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidTable, format, "Transformed WOFF2 glyph metadata disagrees with maxp or head.");
                glyf.Data = glyphResult.Glyf;
                loca.Data = glyphResult.Loca;
                glyf.OriginalLength = (glyf.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length;
                loca.OriginalLength = (loca.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length;
            }
            else if (glyf.TransformVersion != 3 || loca.TransformVersion != 3) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidDirectory, format, "Null WOFF2 glyf and loca tables require transform version three.");
        }
        SvgWebFontTable? hmtx = new SvgWebFontDecoder().FindTable(tables, "hmtx");
        if (hmtx != null && hmtx.Transformed)
        {
            if (glyphResult == null) glyphResult = SvgWoff2GlyphDecoder.ReadMinimums(tables, options);
            if (!glyphResult.Success) return new SvgWebFontDecoder().Failure(glyphResult.Error, format, glyphResult.Message);
            byte[]? metrics = SvgWoff2GlyphDecoder.ReconstructMetrics(hmtx.Data, tables, glyphResult, options);
            if (metrics == null || metrics.Length != hmtx.OriginalLength) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidTable, format, "The transformed WOFF2 hmtx table is invalid.");
            hmtx.Data = metrics;
            hmtx.OriginalLength = metrics.Length;
        }
        int index = 0;
        while (index < tables.Count)
        {
            if (tables[index].Data == null || (tables[index].Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).LongLength > options.Limits.MaxBytesPerFont) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.LimitExceeded, format, "A reconstructed WOFF2 table exceeds MaxBytesPerFont.");
            index = index + 1;
        }
        return null;
    }
SvgWebFontDecodeResult BuildSfnt(System.Collections.Generic.List<SvgWebFontTable> tables, uint flavor, SvgOpenTypeReadOptions options, SvgWebFontFormat format)
    {
        SvgWebFontTable? head = new SvgWebFontDecoder().FindTable(tables, "head");
        if (head != null)
        {
            if ((head.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 12) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidTable, format, "The reconstructed head table is truncated.");
            byte[] copy = new byte[head.Data.Length];
            System.Array.Copy(head.Data, 0, copy, 0, copy.Length);
            copy[8] = 0;
            copy[9] = 0;
            copy[10] = 0;
            copy[11] = 0;
            head.Data = copy;
        }
        System.Collections.Generic.List<SvgWebFontTable> physical = new System.Collections.Generic.List<SvgWebFontTable>(tables);
        physical.Sort((first, second) => first.Order.CompareTo(second.Order));
        long outputLength = 12L + 16L * tables.Count;
        int index = 0;
        while (index < physical.Count)
        {
            outputLength = new SvgWebFontDecoder().Align4Long(outputLength);
            physical[index].OutputOffset = (int)outputLength;
            outputLength = outputLength + (physical[index].Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length;
            if (outputLength > options.Limits.MaxBytesPerFont || outputLength > System.Int32.MaxValue) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.LimitExceeded, format, "The reconstructed SFNT font exceeds MaxBytesPerFont.");
            index = index + 1;
        }
        outputLength = new SvgWebFontDecoder().Align4Long(outputLength);
        byte[] output = new byte[(int)outputLength];
        new SvgWebFontDecoder().WriteUInt32(output, 0, flavor);
        new SvgWebFontDecoder().WriteUInt16(output, 4, tables.Count);
        int selector = 0;
        int power = 1;
        while (power <= tables.Count / 2)
        {
            power = power * 2;
            selector = selector + 1;
        }
        new SvgWebFontDecoder().WriteUInt16(output, 6, power * 16);
        new SvgWebFontDecoder().WriteUInt16(output, 8, selector);
        new SvgWebFontDecoder().WriteUInt16(output, 10, tables.Count * 16 - power * 16);
        System.Collections.Generic.List<SvgWebFontTable> records = new System.Collections.Generic.List<SvgWebFontTable>(tables);
        records.Sort((first, second) => string.CompareOrdinal(first.Tag, second.Tag));
        index = 0;
        while (index < records.Count)
        {
            SvgWebFontTable table = records[index];
            int record = 12 + index * 16;
            new SvgWebFontDecoder().WriteTag(output, record, table.Tag);
            new SvgWebFontDecoder().WriteUInt32(output, record + 4, new SvgWebFontDecoder().Checksum(table.Data, 0, (table.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length, table.Tag == "head"));
            new SvgWebFontDecoder().WriteUInt32(output, record + 8, (uint)table.OutputOffset);
            new SvgWebFontDecoder().WriteUInt32(output, record + 12, (uint)table.Data.Length);
            System.Array.Copy(table.Data, 0, output, table.OutputOffset, table.Data.Length);
            index = index + 1;
        }
        if (head != null)
        {
            uint adjustment = (uint)(((ulong)2981146554u + 4294967296UL - new SvgWebFontDecoder().Checksum(output, 0, output.Length, false)) & 4294967295UL);
            new SvgWebFontDecoder().WriteUInt32(output, head.OutputOffset + 8, adjustment);
        }
        SvgWebFontDecodeResult result = new SvgWebFontDecodeResult();
        result.Success = true;
        result.Format = format;
        result.Bytes = output;
        result.Message = "The web font was reconstructed successfully.";
        return result;
    }
byte[]? Decompress(byte[] bytes, int offset, int length, int expectedLength, bool brotli, SvgOpenTypeReadOptions options)
    {
        byte[] output = new byte[expectedLength];
        using (System.IO.MemoryStream input = new System.IO.MemoryStream(bytes, offset, length, false))
        using (System.IO.Stream decoder = brotli ? new System.IO.Compression.BrotliStream(input, System.IO.Compression.CompressionMode.Decompress, false) : new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress, false))
        {
            int written = 0;
            while (written < output.Length)
            {
                if (options.CancellationToken.IsCancellationRequested) return null;
                int read = decoder.Read(output, written, output.Length - written);
                if (read == 0) return null;
                written = written + read;
            }
            if (decoder.ReadByte() != -1) return null;
        }
        return output;
    }
SvgWebFontDecodeResult? ValidateOptionalBlocks(byte[] bytes, int start, uint metadataOffset, uint metadataLength, uint metadataOriginalLength, uint privateOffset, uint privateLength, SvgWebFontFormat format)
    {
        int cursor = start;
        if (metadataOffset == 0 || metadataLength == 0 || metadataOriginalLength == 0)
        {
            if (metadataOffset != 0 || metadataLength != 0 || metadataOriginalLength != 0) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The web-font metadata fields are inconsistent.");
        }
        else
        {
            if (metadataOffset > System.Int32.MaxValue || metadataLength > System.Int32.MaxValue || metadataOffset != cursor || (long)metadataOffset + metadataLength > bytes.Length) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The web-font metadata range is invalid.");
            int metadataEnd = (int)metadataOffset + (int)metadataLength;
            int paddedEnd = new SvgWebFontDecoder().Align4(metadataEnd);
            if (paddedEnd < 0 || !new SvgWebFontDecoder().ZeroPadding(bytes, metadataEnd, paddedEnd)) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The web-font metadata padding is invalid.");
            cursor = paddedEnd;
        }
        if (privateOffset == 0 || privateLength == 0)
        {
            if (privateOffset != 0 || privateLength != 0) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The web-font private-data fields are inconsistent.");
        }
        else
        {
            if (privateOffset > System.Int32.MaxValue || privateLength > System.Int32.MaxValue || privateOffset != cursor || (long)privateOffset + privateLength > bytes.Length) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The web-font private-data range is invalid.");
            cursor = (int)privateOffset + (int)privateLength;
        }
        if (cursor != bytes.Length) return new SvgWebFontDecoder().Failure(SvgOpenTypeError.InvalidContainer, format, "The web-font container has a gap or trailing data.");
        return null;
    }
SvgWebFontNumber ReadBase128(SvgWebFontCursor cursor)
    {
        uint result = 0;
        int index = 0;
        while (index < 5)
        {
            int value = cursor.ReadByte();
            if (value < 0 || index == 0 && value == 128) return new SvgWebFontNumber(false, 0);
            if ((result & 4261412864u) != 0) return new SvgWebFontNumber(false, 0);
            result = result << 7 | (uint)(value & 127);
            if ((value & 128) == 0)
            {
                if (result > System.Int32.MaxValue) return new SvgWebFontNumber(false, 0);
                return new SvgWebFontNumber(true, (int)result);
            }
            index = index + 1;
        }
        return new SvgWebFontNumber(false, 0);
    }
SvgWebFontTable? FindTable(System.Collections.Generic.List<SvgWebFontTable> tables, string? tag)
    {
        int index = 0;
        while (index < tables.Count)
        {
            if (tables[index].Tag == tag) return tables[index];
            index = index + 1;
        }
        return null;
    }
bool ValidOptions(SvgOpenTypeReadOptions? options)
    {
        if (options == null || options.Limits == null) return false;
        SvgFontLimits limits = options.Limits;
        return limits.MaxFonts > 0 && limits.MaxBytesPerFont > 0 && limits.MaxTotalBytes > 0 && limits.MaxTablesPerFont > 0 && limits.MaxGlyphsPerFont > 0 && limits.MaxPointsPerGlyph > 0 && limits.MaxShapingGlyphs > 0 && limits.MaxVariationAxes > 0;
    }
SvgWebFontFormat Detect(byte[] bytes)
    {
        if (bytes.Length < 4) return SvgWebFontFormat.Unknown;
        uint signature = new SvgWebFontDecoder().ReadUInt32(bytes, 0);
        if (signature == 2001684038) return SvgWebFontFormat.Woff;
        if (signature == 2001684018) return SvgWebFontFormat.Woff2;
        return SvgWebFontFormat.Unknown;
    }
bool ValidFlavor(uint flavor)
    {
        return flavor == 65536 || flavor == 1330926671;
    }
bool ValidTag(string? tag)
    {
        if (tag == null || tag.Length != 4) return false;
        int index = 0;
        while (index < 4)
        {
            if (tag[index] < 32 || tag[index] > 126) return false;
            index = index + 1;
        }
        return true;
    }
bool ZeroPadding(byte[] bytes, int start, int end)
    {
        if (start < 0 || end < start || end > bytes.Length) return false;
        int index = start;
        while (index < end)
        {
            if (bytes[index] != 0) return false;
            index = index + 1;
        }
        return true;
    }
int Align4(int value)
    {
        if (value < 0 || value > System.Int32.MaxValue - 3) return -1;
        return (value + 3) & -4;
    }
long Align4Long(long value)
    {
        return (value + 3) & -4L;
    }
uint Checksum(byte[]? bytes, int offset, int length, bool zeroHeadAdjustment)
    {
        ulong sum = 0;
        int padded = new SvgWebFontDecoder().Align4(length);
        int index = 0;
        while (index < padded)
        {
            uint word = 0;
            int part = 0;
            while (part < 4)
            {
                int relative = index + part;
                byte value = relative < length ? (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset + relative] : (byte)0;
                if (zeroHeadAdjustment && relative >= 8 && relative < 12) value = 0;
                word = word | (uint)value << (24 - part * 8);
                part = part + 1;
            }
            sum = sum + word;
            index = index + 4;
        }
        return (uint)(sum & 4294967295u);
    }
string ReadTag(byte[] bytes, int offset)
    {
        char[] value = new char[4];
        value[0] = (char)bytes[offset];
        value[1] = (char)bytes[offset + 1];
        value[2] = (char)bytes[offset + 2];
        value[3] = (char)bytes[offset + 3];
        return new string(value);
    }
int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }
uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }
void WriteTag(byte[] bytes, int offset, string? value)
    {
        bytes[offset] = (byte)(value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[0];
        bytes[offset + 1] = (byte)value[1];
        bytes[offset + 2] = (byte)value[2];
        bytes[offset + 3] = (byte)value[3];
    }
void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 8) & 255);
        bytes[offset + 1] = (byte)(value & 255);
    }
void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)((value >> 24) & 255);
        bytes[offset + 1] = (byte)((value >> 16) & 255);
        bytes[offset + 2] = (byte)((value >> 8) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
SvgWebFontDecodeResult Failure(SvgOpenTypeError error, SvgWebFontFormat format, string message)
    {
        SvgWebFontDecodeResult result = new SvgWebFontDecodeResult();
        result.Error = error;
        result.Format = format;
        result.Message = message;
        return result;
    }
SvgWebFontDecodeResult Unsupported(SvgWebFontFormat format, string? tag, string message)
    {
        SvgWebFontDecodeResult result = new SvgWebFontDecoder().Failure(SvgOpenTypeError.UnsupportedTransform, format, message);
        result.RequiresFallback = true;
        result.UnsupportedTable = (tag ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        return result;
    }
}
