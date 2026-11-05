namespace svg;

/// <summary>Reads bounded SFNT, WOFF, and WOFF2 fonts and required OpenType tables.</summary>
public class SvgOpenTypeReader : ISvgOpenTypeReader
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgOpenTypeReader()
    {
    }

    /// <summary>Creates an empty font database with explicit limits.</summary>
    /// <param name="limits">The database font count and byte limits.</param>
    /// <returns>A new empty font database.</returns>
    public SvgFontDatabase CreateFontDatabaseWithLimits(SvgFontLimits limits)
    {
        return SvgFontDatabase.Create(limits);
    }

    /// <summary>Reads caller-owned SFNT or web-font bytes using secure bounded defaults.</summary>
    /// <param name="bytes">The complete SFNT, WOFF, or WOFF2 font bytes.</param>
    /// <returns>The completed or failed font read.</returns>
    public SvgOpenTypeReadResult ReadWithBytes(byte[] bytes)
    {
        return new SvgOpenTypeReader().ReadWithBytesAndOptions(bytes, new SvgOpenTypeReadOptions());
    }

    /// <summary>Reads SFNT or web-font bytes with explicit limits, validation, and ownership.</summary>
    /// <param name="bytes">The complete SFNT, WOFF, or WOFF2 font bytes.</param>
    /// <param name="options">The bounded read options.</param>
    /// <returns>The completed or failed font read.</returns>
    public SvgOpenTypeReadResult ReadWithBytesAndOptions(byte[] bytes, SvgOpenTypeReadOptions? options)
    {
        if (!new SvgOpenTypeReader().ValidOptions(options) || bytes == null)
        {
            SvgOpenTypeReadResult invalid = new SvgOpenTypeReadResult(null);
            invalid.Error = SvgOpenTypeError.InvalidArgument;
            invalid.Diagnostics.Add(new SvgOpenTypeReader().Diagnostic(SvgOpenTypeError.InvalidArgument, "The OpenType bytes or read options are invalid.", options == null ? null : options.Source));
            return invalid;
        }
        if (bytes.LongLength > (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Limits.MaxBytesPerFont)
        {
            SvgOpenTypeReadResult limited = new SvgOpenTypeReadResult(null);
            limited.Error = SvgOpenTypeError.LimitExceeded;
            limited.Diagnostics.Add(new SvgOpenTypeReader().Diagnostic(SvgOpenTypeError.LimitExceeded, "The font exceeds MaxBytesPerFont.", options.Source));
            return limited;
        }
        byte[] retained = bytes;
        bool reconstructed = bytes.Length >= 4 && (new SvgOpenTypeReader().ReadUInt32(bytes, 0) == 2001684038 || new SvgOpenTypeReader().ReadUInt32(bytes, 0) == 2001684018);
        if (reconstructed)
        {
            SvgWebFontDecodeResult decoded = new SvgWebFontDecoder().DecodeWithBytesAndOptions(bytes, options);
            if (!decoded.Success)
            {
                SvgOpenTypeReadResult failed = new SvgOpenTypeReadResult(null);
                failed.Error = decoded.Error;
                failed.Diagnostics.Add(new SvgOpenTypeReader().Diagnostic(decoded.Error, decoded.Message, options.Source));
                return failed;
            }
            retained = (decoded.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }
        else if (options.CopyInputData)
        {
            retained = new byte[bytes.Length];
            System.Array.Copy(bytes, 0, retained, 0, bytes.Length);
        }
        SvgFont font = new SvgFont(retained, reconstructed || options.CopyInputData);
        SvgOpenTypeReadResult result = new SvgOpenTypeReadResult(font);
        SvgOpenTypeReadState state = new SvgOpenTypeReadState(retained, options, result);
        if (new SvgOpenTypeReader().Cancelled(state)) return result;
        if (!new SvgOpenTypeReader().ReadDirectory(state)) return result;
        if (!new SvgOpenTypeReader().ValidateRequiredTables(state)) return result;
        if (!new SvgOpenTypeReader().ParseHead(state)) return result;
        if (!new SvgOpenTypeReader().ParseMaximumProfile(state)) return result;
        if (!new SvgOpenTypeReader().ParseHorizontalHeader(state)) return result;
        if (!new SvgOpenTypeReader().ParseHorizontalMetrics(state)) return result;
        if (!new SvgOpenTypeReader().ParseVerticalMetrics(state)) return result;
        if (!new SvgOpenTypeReader().ParseOs2(state)) return result;
        if (!new SvgOpenTypeReader().ParseNames(state)) return result;
        if (!new SvgOpenTypeReader().ParseCharacterMaps(state)) return result;
        if (!new SvgOpenTypeReader().ParsePost(state)) return result;
        font.FamilyName = font.FindName(16);
        if (font.FamilyName.Length == 0) font.FamilyName = font.FindName(1);
        font.SubfamilyName = font.FindName(17);
        if (font.SubfamilyName.Length == 0) font.SubfamilyName = font.FindName(2);
        font.FullName = font.FindName(4);
        font.PostScriptName = font.FindName(6);
        font.VersionName = font.FindName(5);
        font.Success = true;
        result.Success = true;
        return result;
    }
bool ReadDirectory(SvgOpenTypeReadState state)
    {
        if ((state.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 12) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "The SFNT table directory is truncated.");
        uint version = new SvgOpenTypeReader().ReadUInt32(state.Bytes, 0);
        if (version != 65536 && version != 1330926671) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "The SFNT version is neither TrueType 1.0 nor OTTO.");
        int tableCount = new SvgOpenTypeReader().ReadUInt16(state.Bytes, 4);
        if (tableCount < 1) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "The SFNT table directory is empty.");
        if (tableCount > state.Options.Limits.MaxTablesPerFont) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.LimitExceeded, "The font exceeds MaxTablesPerFont.");
        long directoryLengthValue = 12L + 16L * tableCount;
        if (directoryLengthValue > state.Bytes.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "The SFNT table records are truncated.");
        int directoryLength = (int)directoryLengthValue;
        int expectedSelector = 0;
        int power = 1;
        while (power <= tableCount / 2)
        {
            power = power * 2;
            expectedSelector = expectedSelector + 1;
        }
        int expectedSearchRange = power * 16;
        int expectedRangeShift = tableCount * 16 - expectedSearchRange;
        if (new SvgOpenTypeReader().ReadUInt16(state.Bytes, 6) != expectedSearchRange || new SvgOpenTypeReader().ReadUInt16(state.Bytes, 8) != expectedSelector || new SvgOpenTypeReader().ReadUInt16(state.Bytes, 10) != expectedRangeShift) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "The SFNT binary-search directory fields are inconsistent.");
        (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SfntVersion = version;
        string precedingTag = "";
        int index = 0;
        while (index < tableCount)
        {
            if (new SvgOpenTypeReader().Cancelled(state)) return false;
            int recordOffset = 12 + index * 16;
            string tag = new SvgOpenTypeReader().ReadTag(state.Bytes, recordOffset);
            if (!new SvgOpenTypeReader().ValidTag(tag)) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "An SFNT table tag contains a non-printing byte.");
            if (precedingTag.Length != 0 && string.CompareOrdinal(precedingTag, tag) >= 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "SFNT table records are duplicated or not sorted by tag.");
            uint checksum = new SvgOpenTypeReader().ReadUInt32(state.Bytes, recordOffset + 4);
            uint unsignedOffset = new SvgOpenTypeReader().ReadUInt32(state.Bytes, recordOffset + 8);
            uint unsignedLength = new SvgOpenTypeReader().ReadUInt32(state.Bytes, recordOffset + 12);
            if (unsignedOffset > System.Int32.MaxValue || unsignedLength > System.Int32.MaxValue) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "An SFNT table range exceeds the supported byte address space.");
            int offset = (int)unsignedOffset;
            int length = (int)unsignedLength;
            if (offset % 4 != 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "Every top-level SFNT table must begin on a four-byte boundary.");
            if (offset < directoryLength || (long)offset + length > state.Bytes.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "An SFNT table range is outside the font bytes or overlaps the directory.");
            SvgOpenTypeTable table = new SvgOpenTypeTable(tag, checksum, offset, length);
            state.Result.Font.Tables.Add(table);
            precedingTag = tag;
            index = index + 1;
        }
        if (!new SvgOpenTypeReader().ValidateTableRanges(state)) return false;
        if (!new SvgOpenTypeReader().ValidateChecksums(state)) return false;
        return true;
    }
bool ValidateTableRanges(SvgOpenTypeReadState state)
    {
        int index = 0;
        while (index < (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Tables.Count)
        {
            SvgOpenTypeTable table = state.Result.Font.Tables[index];
            int otherIndex = index + 1;
            while (otherIndex < state.Result.Font.Tables.Count)
            {
                SvgOpenTypeTable other = state.Result.Font.Tables[otherIndex];
                long tableEnd = (long)table.Offset + table.Length;
                long otherEnd = (long)other.Offset + other.Length;
                if (table.Length != 0 && other.Length != 0 && table.Offset < otherEnd && other.Offset < tableEnd) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "Distinct top-level SFNT table ranges overlap.");
                otherIndex = otherIndex + 1;
            }
            int paddedLength = new SvgOpenTypeReader().PaddedLength(table.Length);
            if (paddedLength < 0 || (long)table.Offset + paddedLength > (state.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "An SFNT table is missing required four-byte padding.");
            int padding = table.Offset + table.Length;
            while (padding < table.Offset + paddedLength)
            {
                if (state.Bytes[padding] != 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidDirectory, "SFNT table padding bytes must be zero.");
                padding = padding + 1;
            }
            index = index + 1;
        }
        return true;
    }
bool ValidateChecksums(SvgOpenTypeReadState state)
    {
        int index = 0;
        while (index < (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Tables.Count)
        {
            if (new SvgOpenTypeReader().Cancelled(state)) return false;
            SvgOpenTypeTable table = state.Result.Font.Tables[index];
            bool head = table.Tag == "head";
            table.ComputedChecksum = new SvgOpenTypeReader().Checksum(state.Bytes, table.Offset, table.Length, head);
            table.ChecksumValid = table.ComputedChecksum == table.Checksum;
            if (state.Options.ValidateChecksums && !table.ChecksumValid) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidChecksum, "The checksum for table " + table.Tag + " is invalid.");
            index = index + 1;
        }
        if (state.Options.ValidateChecksums && new SvgOpenTypeReader().Checksum(state.Bytes, 0, (state.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length, false) != 2981146554) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidChecksum, "The whole-font checksum adjustment is invalid.");
        return true;
    }
bool ValidateRequiredTables(SvgOpenTypeReadState state)
    {
        string[] required = new string[] { "OS/2", "cmap", "head", "hhea", "hmtx", "maxp", "name", "post" };
        int index = 0;
        while (index < required.Length)
        {
            if ((state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable(required[index]) == null) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.MissingRequiredTable, "The required " + required[index] + " table is missing.");
            index = index + 1;
        }
        bool glyf = (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("glyf") != null;
        bool loca = state.Result.Font.FindTable("loca") != null;
        bool cff = state.Result.Font.FindTable("CFF ") != null;
        bool cff2 = state.Result.Font.FindTable("CFF2") != null;
        if (state.Result.Font.SfntVersion == 65536)
        {
            if (!glyf || !loca || cff || cff2) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.MissingRequiredTable, "A TrueType SFNT requires matching glyf and loca tables without CFF outlines.");
            state.Result.Font.OutlineKind = SvgOpenTypeOutlineKind.TrueType;
        }
        else
        {
            if (glyf || loca || cff == cff2) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.MissingRequiredTable, "An OTTO font requires exactly one CFF or CFF2 outline table family.");
            state.Result.Font.OutlineKind = cff ? SvgOpenTypeOutlineKind.Cff : SvgOpenTypeOutlineKind.Cff2;
        }
        return true;
    }
bool ParseHead(SvgOpenTypeReadState state)
    {
        SvgOpenTypeTable? table = (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("head");
        if ((table ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 54) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The head table is truncated.");
        SvgOpenTypeHeadTable result = new SvgOpenTypeHeadTable();
        result.MajorVersion = new SvgOpenTypeReader().TableUInt16(state, table, 0);
        result.MinorVersion = new SvgOpenTypeReader().TableUInt16(state, table, 2);
        result.FontRevision = new SvgOpenTypeReader().TableFixed(state, table, 4);
        result.ChecksumAdjustment = new SvgOpenTypeReader().TableUInt32(state, table, 8);
        uint magic = new SvgOpenTypeReader().TableUInt32(state, table, 12);
        result.Flags = new SvgOpenTypeReader().TableUInt16(state, table, 16);
        result.UnitsPerEm = new SvgOpenTypeReader().TableUInt16(state, table, 18);
        result.Created = new SvgOpenTypeReader().TableInt64(state, table, 20);
        result.Modified = new SvgOpenTypeReader().TableInt64(state, table, 28);
        result.XMin = new SvgOpenTypeReader().TableInt16(state, table, 36);
        result.YMin = new SvgOpenTypeReader().TableInt16(state, table, 38);
        result.XMax = new SvgOpenTypeReader().TableInt16(state, table, 40);
        result.YMax = new SvgOpenTypeReader().TableInt16(state, table, 42);
        result.MacStyle = new SvgOpenTypeReader().TableUInt16(state, table, 44);
        result.LowestRecommendedPixelsPerEm = new SvgOpenTypeReader().TableUInt16(state, table, 46);
        result.FontDirectionHint = new SvgOpenTypeReader().TableInt16(state, table, 48);
        result.IndexToLocationFormat = new SvgOpenTypeReader().TableInt16(state, table, 50);
        result.GlyphDataFormat = new SvgOpenTypeReader().TableInt16(state, table, 52);
        if (result.MajorVersion != 1 || magic != 1594834165) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The head table version or magic number is invalid.");
        if (result.UnitsPerEm < 16 || result.UnitsPerEm > 16384) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The head unitsPerEm value is outside 16 through 16384.");
        if (result.XMin > result.XMax || result.YMin > result.YMax) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The head font bounding box is inverted.");
        if (result.IndexToLocationFormat != 0 && result.IndexToLocationFormat != 1) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The head indexToLocFormat value is invalid.");
        if (result.GlyphDataFormat != 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "The head glyphDataFormat is unsupported.");
        state.Result.Font.Head = result;
        return true;
    }
bool ParseMaximumProfile(SvgOpenTypeReadState state)
    {
        SvgOpenTypeTable? table = (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("maxp");
        if ((table ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 6) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The maxp table is truncated.");
        SvgOpenTypeMaximumProfileTable result = new SvgOpenTypeMaximumProfileTable();
        result.Version = new SvgOpenTypeReader().TableUInt32(state, table, 0);
        result.GlyphCount = new SvgOpenTypeReader().TableUInt16(state, table, 4);
        if (result.GlyphCount < 1) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The maxp glyph count must be positive.");
        if (result.GlyphCount > state.Options.Limits.MaxGlyphsPerFont) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.LimitExceeded, "The font exceeds MaxGlyphsPerFont.");
        if (state.Result.Font.OutlineKind == SvgOpenTypeOutlineKind.TrueType)
        {
            if (result.Version != 65536 || table.Length < 32) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "TrueType outlines require maxp version 1.0.");
            result.MaximumPoints = new SvgOpenTypeReader().TableUInt16(state, table, 6);
            result.MaximumContours = new SvgOpenTypeReader().TableUInt16(state, table, 8);
            result.MaximumCompositePoints = new SvgOpenTypeReader().TableUInt16(state, table, 10);
            result.MaximumCompositeContours = new SvgOpenTypeReader().TableUInt16(state, table, 12);
            result.MaximumComponentElements = new SvgOpenTypeReader().TableUInt16(state, table, 28);
            result.MaximumComponentDepth = new SvgOpenTypeReader().TableUInt16(state, table, 30);
            if (result.MaximumPoints > state.Options.Limits.MaxPointsPerGlyph || result.MaximumCompositePoints > state.Options.Limits.MaxPointsPerGlyph) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.LimitExceeded, "The maxp point requirements exceed MaxPointsPerGlyph.");
        }
        else if (result.Version != 20480) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "CFF outlines require maxp version 0.5.");
        state.Result.Font.MaximumProfile = result;
        return true;
    }
bool ParseHorizontalHeader(SvgOpenTypeReadState state)
    {
        SvgOpenTypeTable? table = (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("hhea");
        if ((table ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 36) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The hhea table is truncated.");
        if (new SvgOpenTypeReader().TableUInt16(state, table, 0) != 1) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "The hhea major version is unsupported.");
        int reservedOffset = 24;
        while (reservedOffset <= 30)
        {
            if (new SvgOpenTypeReader().TableInt16(state, table, reservedOffset) != 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A reserved hhea field is nonzero.");
            reservedOffset = reservedOffset + 2;
        }
        SvgOpenTypeHorizontalHeaderTable result = new SvgOpenTypeHorizontalHeaderTable();
        result.Ascender = new SvgOpenTypeReader().TableInt16(state, table, 4);
        result.Descender = new SvgOpenTypeReader().TableInt16(state, table, 6);
        result.LineGap = new SvgOpenTypeReader().TableInt16(state, table, 8);
        result.AdvanceWidthMaximum = new SvgOpenTypeReader().TableUInt16(state, table, 10);
        result.MinimumLeftSideBearing = new SvgOpenTypeReader().TableInt16(state, table, 12);
        result.MinimumRightSideBearing = new SvgOpenTypeReader().TableInt16(state, table, 14);
        result.MaximumExtent = new SvgOpenTypeReader().TableInt16(state, table, 16);
        result.CaretSlopeRise = new SvgOpenTypeReader().TableInt16(state, table, 18);
        result.CaretSlopeRun = new SvgOpenTypeReader().TableInt16(state, table, 20);
        result.CaretOffset = new SvgOpenTypeReader().TableInt16(state, table, 22);
        result.MetricDataFormat = new SvgOpenTypeReader().TableInt16(state, table, 32);
        result.NumberOfHorizontalMetrics = new SvgOpenTypeReader().TableUInt16(state, table, 34);
        if (result.MetricDataFormat != 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "The hhea metricDataFormat is unsupported.");
        if (result.NumberOfHorizontalMetrics < 1 || result.NumberOfHorizontalMetrics > (state.Result.Font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The hhea numberOfHMetrics is outside the glyph count.");
        state.Result.Font.HorizontalHeader = result;
        return true;
    }
bool ParseHorizontalMetrics(SvgOpenTypeReadState state)
    {
        SvgOpenTypeTable? table = (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("hmtx");
        int glyphCount = (state.Result.Font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount;
        int metricCount = (state.Result.Font.HorizontalHeader ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).NumberOfHorizontalMetrics;
        long required = 4L * metricCount + 2L * (glyphCount - metricCount);
        if (required > (table ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The hmtx table is truncated for the declared glyph and metric counts.");
        int glyph = 0;
        int lastAdvance = 0;
        while (glyph < glyphCount)
        {
            if (new SvgOpenTypeReader().Cancelled(state)) return false;
            int bearing;
            if (glyph < metricCount)
            {
                lastAdvance = new SvgOpenTypeReader().TableUInt16(state, table, glyph * 4);
                bearing = new SvgOpenTypeReader().TableInt16(state, table, glyph * 4 + 2);
            }
            else bearing = new SvgOpenTypeReader().TableInt16(state, table, metricCount * 4 + (glyph - metricCount) * 2);
            state.Result.Font.HorizontalMetrics.Add(new SvgOpenTypeHorizontalMetric(glyph, lastAdvance, bearing));
            glyph = glyph + 1;
        }
        return true;
    }
bool ParseVerticalMetrics(SvgOpenTypeReadState state)
    {
        SvgOpenTypeTable? headerTable = (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("vhea");
        SvgOpenTypeTable? metricTable = state.Result.Font.FindTable("vmtx");
        if (headerTable == null && metricTable == null) return true;
        if (headerTable == null || metricTable == null) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "Vertical fonts require both vhea and vmtx tables.");
        if (headerTable.Length < 36) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The vhea table is truncated.");
        uint version = new SvgOpenTypeReader().TableUInt32(state, headerTable, 0);
        if (version != 65536 && version != 69632) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "The vhea version is unsupported.");
        int reservedOffset = 24;
        while (reservedOffset <= 30)
        {
            if (new SvgOpenTypeReader().TableInt16(state, headerTable, reservedOffset) != 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A reserved vhea field is nonzero.");
            reservedOffset = reservedOffset + 2;
        }
        SvgOpenTypeVerticalHeaderTable header = new SvgOpenTypeVerticalHeaderTable();
        header.Version = version;
        header.Ascender = new SvgOpenTypeReader().TableInt16(state, headerTable, 4);
        header.Descender = new SvgOpenTypeReader().TableInt16(state, headerTable, 6);
        header.LineGap = new SvgOpenTypeReader().TableInt16(state, headerTable, 8);
        header.AdvanceHeightMaximum = new SvgOpenTypeReader().TableUInt16(state, headerTable, 10);
        header.MinimumTopSideBearing = new SvgOpenTypeReader().TableInt16(state, headerTable, 12);
        header.MinimumBottomSideBearing = new SvgOpenTypeReader().TableInt16(state, headerTable, 14);
        header.MaximumExtent = new SvgOpenTypeReader().TableInt16(state, headerTable, 16);
        header.CaretSlopeRise = new SvgOpenTypeReader().TableInt16(state, headerTable, 18);
        header.CaretSlopeRun = new SvgOpenTypeReader().TableInt16(state, headerTable, 20);
        header.CaretOffset = new SvgOpenTypeReader().TableInt16(state, headerTable, 22);
        header.MetricDataFormat = new SvgOpenTypeReader().TableInt16(state, headerTable, 32);
        header.NumberOfVerticalMetrics = new SvgOpenTypeReader().TableUInt16(state, headerTable, 34);
        int glyphCount = (state.Result.Font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount;
        if (header.MetricDataFormat != 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "The vhea metricDataFormat is unsupported.");
        if (header.NumberOfVerticalMetrics < 1 || header.NumberOfVerticalMetrics > glyphCount) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The vhea numberOfLongVerMetrics is outside the glyph count.");
        int metricCount = header.NumberOfVerticalMetrics;
        long required = 4L * metricCount + 2L * (glyphCount - metricCount);
        if (required > metricTable.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The vmtx table is truncated for the declared glyph and metric counts.");
        int glyph = 0;
        int lastAdvance = 0;
        while (glyph < glyphCount)
        {
            if (new SvgOpenTypeReader().Cancelled(state)) return false;
            int bearing;
            if (glyph < metricCount)
            {
                lastAdvance = new SvgOpenTypeReader().TableUInt16(state, metricTable, glyph * 4);
                bearing = new SvgOpenTypeReader().TableInt16(state, metricTable, glyph * 4 + 2);
            }
            else bearing = new SvgOpenTypeReader().TableInt16(state, metricTable, metricCount * 4 + (glyph - metricCount) * 2);
            state.Result.Font.VerticalMetrics.Add(new SvgOpenTypeVerticalMetric(glyph, lastAdvance, bearing));
            glyph = glyph + 1;
        }
        state.Result.Font.VerticalHeader = header;
        return true;
    }
bool ParseOs2(SvgOpenTypeReadState state)
    {
        SvgOpenTypeTable? table = (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("OS/2");
        if ((table ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 2) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The OS/2 table is truncated.");
        int version = new SvgOpenTypeReader().TableUInt16(state, table, 0);
        if (version < 0 || version > 5) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "The OS/2 table version is unsupported.");
        int required = version == 0 ? 78 : version == 1 ? 86 : version == 5 ? 100 : 96;
        if (table.Length < required) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The OS/2 table is truncated for its declared version.");
        SvgOpenTypeOs2Table result = new SvgOpenTypeOs2Table();
        result.Version = version;
        result.AverageCharacterWidth = new SvgOpenTypeReader().TableInt16(state, table, 2);
        result.WeightClass = new SvgOpenTypeReader().TableUInt16(state, table, 4);
        result.WidthClass = new SvgOpenTypeReader().TableUInt16(state, table, 6);
        result.EmbeddingFlags = new SvgOpenTypeReader().TableUInt16(state, table, 8);
        result.FamilyClass = new SvgOpenTypeReader().TableInt16(state, table, 30);
        result.VendorIdentifier = new SvgOpenTypeReader().TableTag(state, table, 58);
        result.SelectionFlags = new SvgOpenTypeReader().TableUInt16(state, table, 62);
        result.FirstCharacterIndex = new SvgOpenTypeReader().TableUInt16(state, table, 64);
        result.LastCharacterIndex = new SvgOpenTypeReader().TableUInt16(state, table, 66);
        result.TypographicAscender = new SvgOpenTypeReader().TableInt16(state, table, 68);
        result.TypographicDescender = new SvgOpenTypeReader().TableInt16(state, table, 70);
        result.TypographicLineGap = new SvgOpenTypeReader().TableInt16(state, table, 72);
        result.WindowsAscent = new SvgOpenTypeReader().TableUInt16(state, table, 74);
        result.WindowsDescent = new SvgOpenTypeReader().TableUInt16(state, table, 76);
        result.HasHeightMetrics = version >= 2;
        if (result.HasHeightMetrics)
        {
            result.XHeight = new SvgOpenTypeReader().TableInt16(state, table, 86);
            result.CapitalHeight = new SvgOpenTypeReader().TableInt16(state, table, 88);
        }
        if (result.WeightClass < 1 || result.WeightClass > 1000) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The OS/2 weight class is outside 1 through 1000.");
        if (result.WidthClass < 1 || result.WidthClass > 9) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The OS/2 width class is outside 1 through 9.");
        if (!new SvgOpenTypeReader().ValidTag(result.VendorIdentifier)) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The OS/2 vendor identifier contains a non-printing byte.");
        state.Result.Font.Os2 = result;
        return true;
    }
bool ParseNames(SvgOpenTypeReadState state)
    {
        SvgOpenTypeTable? table = (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("name");
        if ((table ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 6) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The name table is truncated.");
        int version = new SvgOpenTypeReader().TableUInt16(state, table, 0);
        int count = new SvgOpenTypeReader().TableUInt16(state, table, 2);
        int storageOffset = new SvgOpenTypeReader().TableUInt16(state, table, 4);
        if (version != 0 && version != 1) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "The name table version is unsupported.");
        long recordsEnd = 6L + 12L * count;
        if (recordsEnd > table.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The name records are truncated.");
        int languageCount = 0;
        int languageOffset = (int)recordsEnd;
        if (version == 1)
        {
            if (languageOffset + 2 > table.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The name language-tag count is truncated.");
            languageCount = new SvgOpenTypeReader().TableUInt16(state, table, languageOffset);
            languageOffset = languageOffset + 2;
            if ((long)languageOffset + 4L * languageCount > table.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The name language-tag records are truncated.");
        }
        int headerEnd = languageOffset + 4 * languageCount;
        if (storageOffset < headerEnd || storageOffset > table.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The name string storage offset overlaps records or exceeds the table.");
        System.Collections.Generic.List<string> languageTags = new System.Collections.Generic.List<string>();
        int languageIndex = 0;
        while (languageIndex < languageCount)
        {
            int length = new SvgOpenTypeReader().TableUInt16(state, table, languageOffset + languageIndex * 4);
            int offset = new SvgOpenTypeReader().TableUInt16(state, table, languageOffset + languageIndex * 4 + 2);
            if (!new SvgOpenTypeReader().StorageRange(table, storageOffset, offset, length)) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A name language-tag string is outside the string storage.");
            string? languageTag = new SvgOpenTypeReader().DecodeUtf16BigEndian(state.Bytes, table.Offset + storageOffset + offset, length);
            if (languageTag == null) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A name language-tag string is not valid UTF-16BE.");
            languageTags.Add(languageTag);
            languageIndex = languageIndex + 1;
        }
        SvgOpenTypeNameRecord? preceding = null;
        int index = 0;
        while (index < count)
        {
            if (new SvgOpenTypeReader().Cancelled(state)) return false;
            int offset = 6 + index * 12;
            SvgOpenTypeNameRecord record = new SvgOpenTypeNameRecord();
            record.PlatformIdentifier = new SvgOpenTypeReader().TableUInt16(state, table, offset);
            record.EncodingIdentifier = new SvgOpenTypeReader().TableUInt16(state, table, offset + 2);
            record.LanguageIdentifier = new SvgOpenTypeReader().TableUInt16(state, table, offset + 4);
            record.NameIdentifier = new SvgOpenTypeReader().TableUInt16(state, table, offset + 6);
            record.Length = new SvgOpenTypeReader().TableUInt16(state, table, offset + 8);
            record.Offset = new SvgOpenTypeReader().TableUInt16(state, table, offset + 10);
            if (preceding != null && new SvgOpenTypeReader().CompareName(preceding, record) >= 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "Name records are duplicated or not sorted by their identifiers.");
            if (!new SvgOpenTypeReader().StorageRange(table, storageOffset, record.Offset, record.Length)) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A name string is outside the string storage.");
            int absolute = table.Offset + storageOffset + record.Offset;
            if (record.PlatformIdentifier == 0 || record.PlatformIdentifier == 3)
            {
                record.Value = (new SvgOpenTypeReader().DecodeUtf16BigEndian(state.Bytes, absolute, record.Length) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                if (record.Value == null) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A Unicode or Windows name string is not valid UTF-16BE.");
                record.Decoded = true;
            }
            else if (record.PlatformIdentifier == 1 && record.EncodingIdentifier == 0)
            {
                string? ascii = new SvgOpenTypeReader().DecodeAscii(state.Bytes, absolute, record.Length);
                if (ascii != null)
                {
                    record.Value = ascii;
                    record.Decoded = true;
                }
            }
            if (record.LanguageIdentifier >= 32768)
            {
                int languageRecord = record.LanguageIdentifier - 32768;
                if (version != 1 || languageRecord < 0 || languageRecord >= languageTags.Count) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A name record references an unavailable language tag.");
                record.LanguageTag = languageTags[languageRecord];
            }
            state.Result.Font.Names.Add(record);
            preceding = record;
            index = index + 1;
        }
        return true;
    }
bool ParseCharacterMaps(SvgOpenTypeReadState state)
    {
        SvgOpenTypeTable? table = (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("cmap");
        if ((table ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 4) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The cmap table is truncated.");
        if (new SvgOpenTypeReader().TableUInt16(state, table, 0) != 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "The cmap table version is unsupported.");
        int count = new SvgOpenTypeReader().TableUInt16(state, table, 2);
        int recordsEnd = 4 + 8 * count;
        if (count < 1 || recordsEnd > table.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The cmap encoding records are empty or truncated.");
        SvgOpenTypeCmapRecord? preceding = null;
        int index = 0;
        while (index < count)
        {
            if (new SvgOpenTypeReader().Cancelled(state)) return false;
            int recordOffset = 4 + index * 8;
            SvgOpenTypeCmapRecord record = new SvgOpenTypeCmapRecord();
            record.PlatformIdentifier = new SvgOpenTypeReader().TableUInt16(state, table, recordOffset);
            record.EncodingIdentifier = new SvgOpenTypeReader().TableUInt16(state, table, recordOffset + 2);
            uint offsetValue = new SvgOpenTypeReader().TableUInt32(state, table, recordOffset + 4);
            if (offsetValue > System.Int32.MaxValue) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A cmap subtable offset exceeds the supported address space.");
            record.Offset = (int)offsetValue;
            if (record.Offset < recordsEnd || (long)record.Offset + 2 > table.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A cmap subtable offset overlaps records or exceeds the table.");
            if (!new SvgOpenTypeReader().ParseCmapSubtable(state, table, record)) return false;
            if (preceding != null && new SvgOpenTypeReader().CompareCmap(preceding, record) >= 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "Cmap encoding records are duplicated or not sorted by platform, encoding, and language.");
            state.Result.Font.CharacterMaps.Add(record);
            preceding = record;
            index = index + 1;
        }
        return true;
    }
bool ParseCmapSubtable(SvgOpenTypeReadState state, SvgOpenTypeTable table, SvgOpenTypeCmapRecord record)
    {
        int offset = record.Offset;
        record.Format = new SvgOpenTypeReader().TableUInt16(state, table, offset);
        record.Language = 0;
        record.HasLanguage = false;
        long length;
        if (record.Format == 0 || record.Format == 2 || record.Format == 4 || record.Format == 6)
        {
            if (offset + 6 > table.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A cmap subtable header is truncated.");
            length = new SvgOpenTypeReader().TableUInt16(state, table, offset + 2);
            record.Language = (uint)new SvgOpenTypeReader().TableUInt16(state, table, offset + 4);
            record.HasLanguage = true;
        }
        else if (record.Format == 8 || record.Format == 10 || record.Format == 12 || record.Format == 13)
        {
            if (offset + 12 > table.Length || new SvgOpenTypeReader().TableUInt16(state, table, offset + 2) != 0) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A long cmap subtable header is truncated or has a nonzero reserved field.");
            length = new SvgOpenTypeReader().TableUInt32(state, table, offset + 4);
            record.Language = new SvgOpenTypeReader().TableUInt32(state, table, offset + 8);
            record.HasLanguage = true;
        }
        else if (record.Format == 14)
        {
            if (offset + 10 > table.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 14 cmap header is truncated.");
            length = new SvgOpenTypeReader().TableUInt32(state, table, offset + 2);
        }
        else return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "A cmap subtable format is unsupported.");
        if (length > System.Int32.MaxValue || length < 1 || (long)offset + length > table.Length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A cmap subtable length exceeds the cmap table.");
        record.Length = (int)length;
        if (!new SvgOpenTypeReader().ValidateCmapBody(state, table, record)) return false;
        return true;
    }
bool ValidateCmapBody(SvgOpenTypeReadState state, SvgOpenTypeTable table, SvgOpenTypeCmapRecord record)
    {
        int offset = record.Offset;
        int length = record.Length;
        if (record.Format == 0 && length != 262) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 0 cmap subtable must contain 256 glyph bytes.");
        if (record.Format == 2 && length < 518) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 2 cmap subtable is truncated.");
        if (record.Format == 4)
        {
            if (length < 24) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 4 cmap subtable is truncated.");
            int segmentBytes = new SvgOpenTypeReader().TableUInt16(state, table, offset + 6);
            if (segmentBytes < 2 || segmentBytes % 2 != 0 || 16L + 4L * segmentBytes > length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 4 cmap segment count exceeds the subtable.");
        }
        if (record.Format == 6)
        {
            if (length < 10) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 6 cmap subtable is truncated.");
            int entries = new SvgOpenTypeReader().TableUInt16(state, table, offset + 8);
            if (10L + 2L * entries > length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 6 cmap glyph array exceeds the subtable.");
        }
        if (record.Format == 8)
        {
            if (length < 8208) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 8 cmap subtable is truncated.");
            uint groups = new SvgOpenTypeReader().TableUInt32(state, table, offset + 8204);
            if (8208L + 12L * groups > length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 8 cmap group array exceeds the subtable.");
        }
        if (record.Format == 10)
        {
            if (length < 20) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 10 cmap subtable is truncated.");
            uint characters = new SvgOpenTypeReader().TableUInt32(state, table, offset + 16);
            if (20L + 2L * characters > length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 10 cmap glyph array exceeds the subtable.");
        }
        if (record.Format == 12 || record.Format == 13)
        {
            if (length < 16) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A grouped cmap subtable is truncated.");
            uint groups = new SvgOpenTypeReader().TableUInt32(state, table, offset + 12);
            if (16L + 12L * groups > length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A grouped cmap array exceeds the subtable.");
        }
        if (record.Format == 14)
        {
            uint selectors = new SvgOpenTypeReader().TableUInt32(state, table, offset + 6);
            if (10L + 11L * selectors > length) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "A format 14 variation-selector array exceeds the subtable.");
        }
        return true;
    }
bool ParsePost(SvgOpenTypeReadState state)
    {
        SvgOpenTypeTable? table = (state.Result.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("post");
        if ((table ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 32) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.InvalidTable, "The post table header is truncated.");
        SvgOpenTypePostTable result = new SvgOpenTypePostTable();
        result.Version = new SvgOpenTypeReader().TableUInt32(state, table, 0);
        result.ItalicAngle = new SvgOpenTypeReader().TableFixed(state, table, 4);
        result.UnderlinePosition = new SvgOpenTypeReader().TableInt16(state, table, 8);
        result.UnderlineThickness = new SvgOpenTypeReader().TableInt16(state, table, 10);
        result.FixedPitch = new SvgOpenTypeReader().TableUInt32(state, table, 12) != 0;
        if (result.Version != 65536 && result.Version != 131072 && result.Version != 151552 && result.Version != 196608 && result.Version != 262144) return new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.UnsupportedVersion, "The post table version is unsupported.");
        state.Result.Font.Post = result;
        return true;
    }
int CompareName(SvgOpenTypeNameRecord first, SvgOpenTypeNameRecord second)
    {
        if (first.PlatformIdentifier != second.PlatformIdentifier) return first.PlatformIdentifier < second.PlatformIdentifier ? -1 : 1;
        if (first.EncodingIdentifier != second.EncodingIdentifier) return first.EncodingIdentifier < second.EncodingIdentifier ? -1 : 1;
        if (first.LanguageIdentifier != second.LanguageIdentifier) return first.LanguageIdentifier < second.LanguageIdentifier ? -1 : 1;
        if (first.NameIdentifier != second.NameIdentifier) return first.NameIdentifier < second.NameIdentifier ? -1 : 1;
        return 0;
    }
int CompareCmap(SvgOpenTypeCmapRecord first, SvgOpenTypeCmapRecord second)
    {
        if (first.PlatformIdentifier != second.PlatformIdentifier) return first.PlatformIdentifier < second.PlatformIdentifier ? -1 : 1;
        if (first.EncodingIdentifier != second.EncodingIdentifier) return first.EncodingIdentifier < second.EncodingIdentifier ? -1 : 1;
        if (first.Language != second.Language) return first.Language < second.Language ? -1 : 1;
        return 0;
    }
bool StorageRange(SvgOpenTypeTable table, int storageOffset, int offset, int length)
    {
        return offset >= 0 && length >= 0 && (long)storageOffset + offset + length <= table.Length;
    }
string? DecodeUtf16BigEndian(byte[]? bytes, int offset, int length)
    {
        if (length % 2 != 0) return null;
        System.Text.StringBuilder result = new System.Text.StringBuilder(length / 2);
        int index = 0;
        while (index < length)
        {
            int value = (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset + index] << 8 | bytes[offset + index + 1];
            if (value >= 55296 && value <= 56319)
            {
                if (index + 3 >= length) return null;
                int low = bytes[offset + index + 2] << 8 | bytes[offset + index + 3];
                if (low < 56320 || low > 57343) return null;
                result.Append((char)value);
                result.Append((char)low);
                index = index + 4;
            }
            else
            {
                if (value >= 56320 && value <= 57343) return null;
                result.Append((char)value);
                index = index + 2;
            }
        }
        return result.ToString();
    }
string? DecodeAscii(byte[]? bytes, int offset, int length)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder(length);
        int index = 0;
        while (index < length)
        {
            if ((bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset + index] > 127) return null;
            result.Append((char)bytes[offset + index]);
            index = index + 1;
        }
        return result.ToString();
    }
bool ValidOptions(SvgOpenTypeReadOptions? options)
    {
        if (options == null || options.Limits == null) return false;
        SvgFontLimits limits = options.Limits;
        return limits.MaxFonts > 0 && limits.MaxBytesPerFont > 0 && limits.MaxTotalBytes > 0 && limits.MaxTablesPerFont > 0 && limits.MaxGlyphsPerFont > 0 && limits.MaxPointsPerGlyph > 0 && limits.MaxShapingGlyphs > 0 && limits.MaxVariationAxes > 0;
    }
bool Cancelled(SvgOpenTypeReadState state)
    {
        if (!state.Options.CancellationToken.IsCancellationRequested) return false;
        new SvgOpenTypeReader().Fail(state, SvgOpenTypeError.Cancelled, "OpenType parsing was cancelled.");
        return true;
    }
bool Fail(SvgOpenTypeReadState state, SvgOpenTypeError error, string message)
    {
        if (state.Result.Error == SvgOpenTypeError.None)
        {
            state.Result.Error = error;
            state.Result.Diagnostics.Add(new SvgOpenTypeReader().Diagnostic(error, message, state.Options.Source));
        }
        return false;
    }
SvgDiagnostic Diagnostic(SvgOpenTypeError error, string message, SvgSourceSpan? source)
    {
        SvgDiagnosticCode code = SvgDiagnosticCode.InvalidFont;
        if (error == SvgOpenTypeError.InvalidArgument) code = SvgDiagnosticCode.InvalidArgument;
        else if (error == SvgOpenTypeError.LimitExceeded) code = SvgDiagnosticCode.InputLimitExceeded;
        else if (error == SvgOpenTypeError.Cancelled) code = SvgDiagnosticCode.Cancelled;
        return new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source);
    }
int PaddedLength(int length)
    {
        if (length < 0 || length > System.Int32.MaxValue - 3) return -1;
        return (length + 3) & -4;
    }
uint Checksum(byte[]? bytes, int offset, int length, bool zeroHeadAdjustment)
    {
        ulong sum = 0;
        int padded = new SvgOpenTypeReader().PaddedLength(length);
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
string ReadTag(byte[]? bytes, int offset)
    {
        char[] value = new char[4];
        value[0] = (char)(bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset];
        value[1] = (char)bytes[offset + 1];
        value[2] = (char)bytes[offset + 2];
        value[3] = (char)bytes[offset + 3];
        return new string(value);
    }
int ReadUInt16(byte[]? bytes, int offset)
    {
        return (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset] << 8 | bytes[offset + 1];
    }
int ReadInt16(byte[]? bytes, int offset)
    {
        int value = new SvgOpenTypeReader().ReadUInt16(bytes, offset);
        return value >= 32768 ? value - 65536 : value;
    }
uint ReadUInt32(byte[]? bytes, int offset)
    {
        return (uint)(bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }
int ReadInt32(byte[]? bytes, int offset)
    {
        uint value = new SvgOpenTypeReader().ReadUInt32(bytes, offset);
        return value <= 2147483647u ? (int)value : (int)((long)value - 4294967296L);
    }
long ReadInt64(byte[]? bytes, int offset)
    {
        long high = new SvgOpenTypeReader().ReadInt32(bytes, offset);
        uint low = new SvgOpenTypeReader().ReadUInt32(bytes, offset + 4);
        return high << 32 | low;
    }
int TableUInt16(SvgOpenTypeReadState state, SvgOpenTypeTable table, int offset)
    {
        return new SvgOpenTypeReader().ReadUInt16(state.Bytes, table.Offset + offset);
    }
int TableInt16(SvgOpenTypeReadState state, SvgOpenTypeTable table, int offset)
    {
        return new SvgOpenTypeReader().ReadInt16(state.Bytes, table.Offset + offset);
    }
uint TableUInt32(SvgOpenTypeReadState state, SvgOpenTypeTable table, int offset)
    {
        return new SvgOpenTypeReader().ReadUInt32(state.Bytes, table.Offset + offset);
    }
long TableInt64(SvgOpenTypeReadState state, SvgOpenTypeTable table, int offset)
    {
        return new SvgOpenTypeReader().ReadInt64(state.Bytes, table.Offset + offset);
    }
double TableFixed(SvgOpenTypeReadState state, SvgOpenTypeTable table, int offset)
    {
        return new SvgOpenTypeReader().ReadInt32(state.Bytes, table.Offset + offset) / 65536.0;
    }
string TableTag(SvgOpenTypeReadState state, SvgOpenTypeTable table, int offset)
    {
        return new SvgOpenTypeReader().ReadTag(state.Bytes, table.Offset + offset);
    }
}
