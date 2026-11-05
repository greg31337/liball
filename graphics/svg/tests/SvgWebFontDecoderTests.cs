namespace svg.Tests;

public class SvgWebFontDecoderTests
{
    [Test]
    public void ReconstructsCompressedWoffAndReadsItAsOpenType()
    {
        byte[] sfnt = SvgOpenTypeReaderTests.BuildFont(false, true);
        byte[] woff = BuildWoff(sfnt);

        SvgWebFontDecodeResult decoded = new SvgWebFontDecoder().DecodeWithBytes(woff);
        SvgOpenTypeReadResult read = new SvgOpenTypeReader().ReadWithBytes(woff);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Format, Is.EqualTo(SvgWebFontFormat.Woff));
            Assert.That(decoded.Bytes, Is.EqualTo(sfnt));
            Assert.That(HasCompressedWoffTable(woff), Is.True);
            Assert.That(read.Success, Is.True);
            Assert.That(read.Font.OwnsBytes, Is.True);
            Assert.That(read.Font.FamilyName, Is.EqualTo("Fixture"));
            Assert.That(read.Font.MapCodePoint(65).GlyphIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void DecodesNullTransformWoff2BrotliData()
    {
        byte[] sfnt = SvgOpenTypeReaderTests.BuildFont(false, true);
        byte[] woff2 = BuildWoff2(sfnt, false, false);

        SvgWebFontDecodeResult decoded = new SvgWebFontDecoder().DecodeWithBytes(woff2);
        SvgOpenTypeReadResult read = new SvgOpenTypeReader().ReadWithBytes(woff2);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Format, Is.EqualTo(SvgWebFontFormat.Woff2));
            Assert.That(decoded.Bytes.Length, Is.EqualTo(sfnt.Length));
            Assert.That(read.Success, Is.True);
            Assert.That(read.Font.FamilyName, Is.EqualTo("Fixture"));
            Assert.That(read.Font.OutlineKind, Is.EqualTo(SvgOpenTypeOutlineKind.TrueType));
        });
    }

    [Test]
    public void ReconstructsTransformedWoff2GlyphLocaAndHorizontalMetrics()
    {
        byte[] sfnt = SvgOpenTypeReaderTests.BuildFont(false, true);
        byte[] woff2 = BuildWoff2(sfnt, true, true);

        SvgOpenTypeReadResult read = new SvgOpenTypeReader().ReadWithBytes(woff2);
        SvgTrueTypeGlyphResult glyph = read.Success ? read.Font.DecodeTrueTypeGlyph(1) : null;

        Assert.Multiple(() =>
        {
            Assert.That(read.Success, Is.True);
            Assert.That(read.Font.FindTable("glyf").Length, Is.GreaterThan(0));
            Assert.That(read.Font.FindTable("loca").Length, Is.EqualTo(8));
            Assert.That(read.Font.HorizontalMetrics.Select(metric => metric.LeftSideBearing), Is.EqualTo(new[] { 0, 0, -20 }));
            Assert.That(glyph.Success, Is.True);
            Assert.That(glyph.Outline.Points.Select(point => point.X), Is.EqualTo(new[] { 0, 100, 100, 0 }));
            Assert.That(glyph.Outline.Points.Select(point => point.Y), Is.EqualTo(new[] { 0, 0, 100, 100 }));
            Assert.That(glyph.Outline.Path.Commands.Select(command => command.Kind), Is.EqualTo(new[]
            {
                SvgPathCommandKind.MoveTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.ClosePath
            }));
        });
    }

    [Test]
    public void RejectsMalformedWoffDirectoriesCompressionAndSizeLimits()
    {
        byte[] sfnt = SvgOpenTypeReaderTests.BuildFont(false, true);
        byte[] invalidLength = BuildWoff(sfnt);
        int compressedRecord = FindCompressedWoffRecord(invalidLength);
        uint originalLength = ReadUInt32(invalidLength, compressedRecord + 12);
        WriteUInt32(invalidLength, compressedRecord + 8, originalLength + 1);

        byte[] invalidCompression = BuildWoff(sfnt);
        compressedRecord = FindCompressedWoffRecord(invalidCompression);
        int compressedOffset = (int)ReadUInt32(invalidCompression, compressedRecord + 4);
        int compressedLength = (int)ReadUInt32(invalidCompression, compressedRecord + 8);
        Array.Clear(invalidCompression, compressedOffset, compressedLength);

        SvgOpenTypeReadOptions options = new SvgOpenTypeReadOptions();
        options.Limits.MaxBytesPerFont = sfnt.Length - 1;

        SvgWebFontDecodeResult directory = new SvgWebFontDecoder().DecodeWithBytes(invalidLength);
        SvgWebFontDecodeResult compression = new SvgWebFontDecoder().DecodeWithBytes(invalidCompression);
        SvgWebFontDecodeResult limited = new SvgWebFontDecoder().DecodeWithBytesAndOptions(BuildWoff(sfnt), options);

        Assert.Multiple(() =>
        {
            Assert.That(directory.Error, Is.EqualTo(SvgOpenTypeError.InvalidDirectory));
            Assert.That(compression.Error, Is.EqualTo(SvgOpenTypeError.DecompressionFailed));
            Assert.That(limited.Error, Is.EqualTo(SvgOpenTypeError.LimitExceeded));
        });
    }

    [Test]
    public void ClassifiesUnsupportedWoff2TransformsAndCollectionsAndRejectsBadBrotli()
    {
        byte[] sfnt = SvgOpenTypeReaderTests.BuildFont(false, true);
        byte[] transform = BuildWoff2(sfnt, false, false);
        transform[48] = (byte)(transform[48] | 64);

        byte[] collection = BuildWoff2(sfnt, false, false);
        WriteUInt32(collection, 4, 0x74746366u);

        byte[] badBrotli = BuildWoff2(sfnt, false, false);
        int compressedOffset = FindWoff2CompressedOffset(badBrotli);
        badBrotli[compressedOffset] = (byte)(badBrotli[compressedOffset] ^ 255);

        SvgWebFontDecodeResult unsupported = new SvgWebFontDecoder().DecodeWithBytes(transform);
        SvgWebFontDecodeResult unsupportedCollection = new SvgWebFontDecoder().DecodeWithBytes(collection);
        SvgWebFontDecodeResult compression = new SvgWebFontDecoder().DecodeWithBytes(badBrotli);

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Error, Is.EqualTo(SvgOpenTypeError.UnsupportedTransform));
            Assert.That(unsupported.RequiresFallback, Is.True);
            Assert.That(unsupported.UnsupportedTable, Is.EqualTo("OS/2"));
            Assert.That(unsupportedCollection.Error, Is.EqualTo(SvgOpenTypeError.UnsupportedTransform));
            Assert.That(unsupportedCollection.RequiresFallback, Is.True);
            Assert.That(compression.Error, Is.EqualTo(SvgOpenTypeError.DecompressionFailed));
        });
    }

    static byte[] BuildWoff(byte[] sfnt)
    {
        List<WebTable> tables = ReadTables(sfnt);
        int offset = 44 + tables.Count * 20;
        int index = 0;
        while (index < tables.Count)
        {
            byte[] compressed = CompressZlib(tables[index].Data);
            tables[index].Encoded = compressed.Length < tables[index].Data.Length ? compressed : tables[index].Data;
            tables[index].Offset = offset;
            offset = Align4(offset + tables[index].Encoded.Length);
            index = index + 1;
        }
        byte[] output = new byte[offset];
        WriteUInt32(output, 0, 0x774F4646u);
        WriteUInt32(output, 4, ReadUInt32(sfnt, 0));
        WriteUInt32(output, 8, (uint)output.Length);
        WriteUInt16(output, 12, tables.Count);
        WriteUInt16(output, 14, 0);
        WriteUInt32(output, 16, (uint)sfnt.Length);
        WriteUInt16(output, 20, 1);
        WriteUInt16(output, 22, 0);
        index = 0;
        while (index < tables.Count)
        {
            WebTable table = tables[index];
            int record = 44 + index * 20;
            WriteTag(output, record, table.Tag);
            WriteUInt32(output, record + 4, (uint)table.Offset);
            WriteUInt32(output, record + 8, (uint)table.Encoded.Length);
            WriteUInt32(output, record + 12, (uint)table.Data.Length);
            WriteUInt32(output, record + 16, table.Checksum);
            Array.Copy(table.Encoded, 0, output, table.Offset, table.Encoded.Length);
            index = index + 1;
        }
        return output;
    }

    static byte[] BuildWoff2(byte[] sfnt, bool transformGlyphs, bool transformMetrics)
    {
        List<WebTable> sourceTables = ReadTables(sfnt);
        if (transformGlyphs)
        {
            int glyfIndex = sourceTables.FindIndex(table => table.Tag == "glyf");
            int locaIndex = sourceTables.FindIndex(table => table.Tag == "loca");
            WebTable loca = sourceTables[locaIndex];
            sourceTables.RemoveAt(locaIndex);
            glyfIndex = sourceTables.FindIndex(table => table.Tag == "glyf");
            sourceTables.Insert(glyfIndex + 1, loca);
        }
        List<byte> directory = new List<byte>();
        List<byte> transformed = new List<byte>();
        int index = 0;
        while (index < sourceTables.Count)
        {
            WebTable table = sourceTables[index];
            int tagIndex = KnownTagIndex(table.Tag);
            bool glyphTransform = transformGlyphs && table.Tag == "glyf";
            bool locaTransform = transformGlyphs && table.Tag == "loca";
            bool metricTransform = transformMetrics && table.Tag == "hmtx";
            int version = glyphTransform || locaTransform ? 0 : metricTransform ? 1 : table.Tag == "glyf" || table.Tag == "loca" ? 3 : 0;
            directory.Add((byte)(tagIndex | version << 6));
            AddBase128(directory, table.Data.Length);
            byte[] data = table.Data;
            if (glyphTransform) data = BuildTransformedGlyf();
            else if (locaTransform) data = Array.Empty<byte>();
            else if (metricTransform) data = new byte[] { 1, 1, 244, 2, 88, 255, 236 };
            if (glyphTransform || locaTransform || metricTransform) AddBase128(directory, data.Length);
            transformed.AddRange(data);
            index = index + 1;
        }
        byte[] compressed = CompressBrotli(transformed.ToArray());
        int compressedOffset = 48 + directory.Count;
        int length = Align4(compressedOffset + compressed.Length);
        byte[] output = new byte[length];
        WriteUInt32(output, 0, 0x774F4632u);
        WriteUInt32(output, 4, ReadUInt32(sfnt, 0));
        WriteUInt32(output, 8, (uint)output.Length);
        WriteUInt16(output, 12, sourceTables.Count);
        WriteUInt16(output, 14, 0);
        WriteUInt32(output, 16, (uint)sfnt.Length);
        WriteUInt32(output, 20, (uint)compressed.Length);
        WriteUInt16(output, 24, 1);
        WriteUInt16(output, 26, 0);
        directory.CopyTo(output, 48);
        Array.Copy(compressed, 0, output, compressedOffset, compressed.Length);
        return output;
    }

    static byte[] BuildTransformedGlyf()
    {
        byte[] output = new byte[56];
        WriteUInt16(output, 0, 0);
        WriteUInt16(output, 2, 0);
        WriteUInt16(output, 4, 3);
        WriteUInt16(output, 6, 0);
        WriteUInt32(output, 8, 6);
        WriteUInt32(output, 12, 1);
        WriteUInt32(output, 16, 4);
        WriteUInt32(output, 20, 5);
        WriteUInt32(output, 24, 0);
        WriteUInt32(output, 28, 4);
        WriteUInt32(output, 32, 0);
        output[36] = 0;
        output[37] = 0;
        output[38] = 0;
        output[39] = 1;
        output[40] = 0;
        output[41] = 0;
        output[42] = 4;
        output[43] = 0;
        output[44] = 11;
        output[45] = 1;
        output[46] = 10;
        output[47] = 0;
        output[48] = 100;
        output[49] = 100;
        output[50] = 100;
        output[51] = 0;
        return output;
    }

    static List<WebTable> ReadTables(byte[] sfnt)
    {
        int count = ReadUInt16(sfnt, 4);
        List<WebTable> tables = new List<WebTable>();
        int index = 0;
        while (index < count)
        {
            int record = 12 + index * 16;
            string tag = ReadTag(sfnt, record);
            uint checksum = ReadUInt32(sfnt, record + 4);
            int offset = (int)ReadUInt32(sfnt, record + 8);
            int length = (int)ReadUInt32(sfnt, record + 12);
            byte[] data = new byte[length];
            Array.Copy(sfnt, offset, data, 0, length);
            tables.Add(new WebTable(tag, checksum, data));
            index = index + 1;
        }
        return tables;
    }

    static byte[] CompressZlib(byte[] bytes)
    {
        using MemoryStream output = new MemoryStream();
        using (System.IO.Compression.ZLibStream stream = new System.IO.Compression.ZLibStream(output, System.IO.Compression.CompressionLevel.SmallestSize, true))
        {
            stream.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }

    static byte[] CompressBrotli(byte[] bytes)
    {
        using MemoryStream output = new MemoryStream();
        using (System.IO.Compression.BrotliStream stream = new System.IO.Compression.BrotliStream(output, System.IO.Compression.CompressionLevel.SmallestSize, true))
        {
            stream.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }

    static bool HasCompressedWoffTable(byte[] bytes)
    {
        return FindCompressedWoffRecord(bytes) >= 0;
    }

    static int FindCompressedWoffRecord(byte[] bytes)
    {
        int count = ReadUInt16(bytes, 12);
        int index = 0;
        while (index < count)
        {
            int record = 44 + index * 20;
            if (ReadUInt32(bytes, record + 8) < ReadUInt32(bytes, record + 12)) return record;
            index = index + 1;
        }
        return -1;
    }

    static int FindWoff2CompressedOffset(byte[] bytes)
    {
        int count = ReadUInt16(bytes, 12);
        int position = 48;
        int index = 0;
        while (index < count)
        {
            int flags = bytes[position];
            position = position + 1;
            if ((flags & 63) == 63) position = position + 4;
            position = SkipBase128(bytes, position);
            int tagIndex = flags & 63;
            int version = flags >> 6;
            bool transformed = (tagIndex == 10 || tagIndex == 11) ? version == 0 : tagIndex == 3 ? version != 0 : version != 0;
            if (transformed) position = SkipBase128(bytes, position);
            index = index + 1;
        }
        return position;
    }

    static int SkipBase128(byte[] bytes, int position)
    {
        while ((bytes[position] & 128) != 0) position = position + 1;
        return position + 1;
    }

    static void AddBase128(List<byte> bytes, int value)
    {
        int shift = 28;
        bool started = false;
        while (shift > 0)
        {
            int part = value >> shift & 127;
            if (part != 0 || started)
            {
                bytes.Add((byte)(part | 128));
                started = true;
            }
            shift = shift - 7;
        }
        bytes.Add((byte)(value & 127));
    }

    static int KnownTagIndex(string tag)
    {
        string[] tags = new string[]
        {
            "cmap", "head", "hhea", "hmtx", "maxp", "name", "OS/2", "post", "cvt ", "fpgm", "glyf", "loca", "prep", "CFF ", "VORG", "EBDT",
            "EBLC", "gasp", "hdmx", "kern", "LTSH", "PCLT", "VDMX", "vhea", "vmtx", "BASE", "GDEF", "GPOS", "GSUB", "EBSC", "JSTF", "MATH",
            "CBDT", "CBLC", "COLR", "CPAL", "SVG ", "sbix", "acnt", "avar", "bdat", "bloc", "bsln", "cvar", "fdsc", "feat", "fmtx", "fvar",
            "gvar", "hsty", "just", "lcar", "mort", "morx", "opbd", "prop", "trak", "Zapf", "Silf", "Glat", "Gloc", "Feat", "Sill"
        };
        return Array.IndexOf(tags, tag);
    }

    static int Align4(int value)
    {
        return (value + 3) & -4;
    }

    static string ReadTag(byte[] bytes, int offset)
    {
        return new string(new[] { (char)bytes[offset], (char)bytes[offset + 1], (char)bytes[offset + 2], (char)bytes[offset + 3] });
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    static uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    static void WriteTag(byte[] bytes, int offset, string value)
    {
        bytes[offset] = (byte)value[0];
        bytes[offset + 1] = (byte)value[1];
        bytes[offset + 2] = (byte)value[2];
        bytes[offset + 3] = (byte)value[3];
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    class WebTable
    {
        public string Tag;
        public uint Checksum;
        public byte[] Data;
        public byte[] Encoded;
        public int Offset;

        public WebTable(string tag, uint checksum, byte[] data)
        {
            Tag = tag;
            Checksum = checksum;
            Data = data;
            Encoded = data;
            Offset = 0;
        }
    }
}
