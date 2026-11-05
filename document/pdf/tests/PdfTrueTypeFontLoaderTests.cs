using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfTrueTypeFontLoaderTests
{
    [Test]
    public void Load_ParsesNamesMetricsFormat4CmapAndInstallablePermissions()
    {
        byte[] data = TestSfntFont.Create(0x00010000U, 0, false);
        PdfTrueTypeFontLoader loader = new PdfTrueTypeFontLoader();

        PdfTrueTypeFont font = loader.Load(data);

        Assert.Multiple(() =>
        {
            Assert.That(font.FamilyName, Is.EqualTo("Test Sans"));
            Assert.That(font.SubfamilyName, Is.EqualTo("Regular"));
            Assert.That(font.FullName, Is.EqualTo("Test Sans Regular"));
            Assert.That(font.PostScriptName, Is.EqualTo("TestSans-Regular"));
            Assert.That(font.IsOpenType, Is.False);
            Assert.That(font.UnitsPerEm, Is.EqualTo(1000));
            Assert.That(font.XMin, Is.EqualTo(-50));
            Assert.That(font.YMin, Is.EqualTo(-200));
            Assert.That(font.XMax, Is.EqualTo(900));
            Assert.That(font.YMax, Is.EqualTo(800));
            Assert.That(font.Ascent, Is.EqualTo(800));
            Assert.That(font.Descent, Is.EqualTo(-200));
            Assert.That(font.LineGap, Is.EqualTo(200));
            Assert.That(font.XHeight, Is.EqualTo(450));
            Assert.That(font.CapHeight, Is.EqualTo(700));
            Assert.That(font.GlyphCount, Is.EqualTo(4));
            Assert.That(font.AdvanceWidths, Is.EqualTo(new[] { 500, 600, 700, 700 }));
            Assert.That(font.CmapPlatformId, Is.EqualTo(3));
            Assert.That(font.CmapEncodingId, Is.EqualTo(1));
            Assert.That(font.CmapFormat, Is.EqualTo(4));
            Assert.That(font.UnicodeCodePoints, Is.EqualTo(new[] { 65, 66, 67 }));
            Assert.That(font.GlyphIndices, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(font.EmbeddingPermission, Is.EqualTo(PdfFontEmbeddingPermission.Installable));
            Assert.That(font.EmbeddingAllowed, Is.True);
            Assert.That(font.SubsettingAllowed, Is.True);
        });

        string expectedIdentity = Convert.ToHexString(SHA256.HashData(data));
        Assert.That(font.Identity, Is.EqualTo(expectedIdentity));
        Assert.That(font.ResourceName, Is.EqualTo("TT" + expectedIdentity));
    }

    [Test]
    public void Load_SelectsFullUnicodeFormat12CmapFromOpenTypeFont()
    {
        byte[] data = TestSfntFont.Create(0x4F54544FU, 0, true);

        PdfTrueTypeFont font = new PdfTrueTypeFontLoader().Load(data);

        Assert.Multiple(() =>
        {
            Assert.That(font.IsOpenType, Is.True);
            Assert.That(font.CmapPlatformId, Is.EqualTo(3));
            Assert.That(font.CmapEncodingId, Is.EqualTo(10));
            Assert.That(font.CmapFormat, Is.EqualTo(12));
            Assert.That(font.UnicodeCodePoints, Is.EqualTo(new[] { 0x1F600 }));
            Assert.That(font.GlyphIndices, Is.EqualTo(new[] { 3 }));
        });
    }

    [TestCase(0x0002, PdfFontEmbeddingPermission.Restricted, false, false)]
    [TestCase(0x0004, PdfFontEmbeddingPermission.PreviewAndPrint, true, true)]
    [TestCase(0x0008 | 0x0100, PdfFontEmbeddingPermission.Editable, true, false)]
    [TestCase(0x0200, PdfFontEmbeddingPermission.BitmapOnly, false, false)]
    public void Load_InterpretsEmbeddingPermissions(int fsType, PdfFontEmbeddingPermission permission, bool embeddingAllowed, bool subsettingAllowed)
    {
        byte[] data = TestSfntFont.Create(0x00010000U, (ushort)fsType, false);

        PdfTrueTypeFont font = new PdfTrueTypeFontLoader().Load(data);

        Assert.That(font.EmbeddingPermission, Is.EqualTo(permission));
        Assert.That(font.EmbeddingAllowed, Is.EqualTo(embeddingAllowed));
        Assert.That(font.SubsettingAllowed, Is.EqualTo(subsettingAllowed));
    }

    [Test]
    public void Registry_DeduplicatesFontDataByDeterministicIdentity()
    {
        byte[] data = TestSfntFont.Create(0x00010000U, 0, false);
        IPdfFontRegistry registry = new PdfFontRegistry();

        PdfTrueTypeFont first = registry.RegisterTrueTypeFont(data);
        PdfTrueTypeFont second = registry.RegisterTrueTypeFont((byte[])data.Clone());

        Assert.That(second, Is.SameAs(first));
        Assert.That(registry.GetTrueTypeFontCount(), Is.EqualTo(1));
        Assert.That(registry.HasTrueTypeFont(first.Identity), Is.True);
        Assert.That(registry.GetTrueTypeFont(first.Identity), Is.SameAs(first));
        Assert.Throws<ArgumentException>(() => registry.GetTrueTypeFont("not-registered"));
    }

    [Test]
    public void Document_RegistersFontsFromByteArrayAndStreamInItsRegistry()
    {
        byte[] firstData = TestSfntFont.Create(0x00010000U, 0, false);
        byte[] secondData = TestSfntFont.Create(0x00010000U, 0x0004, false);
        IPdfDocument document = new PdfDocument();

        PdfTrueTypeFont first = document.RegisterTrueTypeFont(firstData);
        using MemoryStream stream = new MemoryStream(secondData);
        PdfTrueTypeFont second = document.RegisterTrueTypeFontFromStream(stream);

        Assert.That(document.GetFontRegistry().GetTrueTypeFontCount(), Is.EqualTo(2));
        Assert.That(document.GetFontRegistry().GetTrueTypeFont(first.Identity), Is.SameAs(first));
        Assert.That(document.GetFontRegistry().GetTrueTypeFont(second.Identity), Is.SameAs(second));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void Loader_LoadsFromFileAndDoesNotRetainCallerArray()
    {
        byte[] data = TestSfntFont.Create(0x00010000U, 0, false);
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "font-loader-test-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, data);

        try
        {
            PdfTrueTypeFontLoader loader = new PdfTrueTypeFontLoader();
            PdfTrueTypeFont fromArray = loader.Load(data);
            PdfTrueTypeFont fromFile = loader.LoadFromFile(path);
            data[1] = 0;

            Assert.That(fromFile.Identity, Is.EqualTo(fromArray.Identity));
            Assert.That(fromArray.FontData[1], Is.EqualTo(1));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Load_RejectsUnsupportedSignatureAndTruncatedDirectory()
    {
        PdfTrueTypeFontLoader loader = new PdfTrueTypeFontLoader();
        byte[] unsupported = new byte[12];
        byte[] truncated = TestSfntFont.Create(0x00010000U, 0, false)[..20];

        Assert.Throws<InvalidDataException>(() => loader.Load(unsupported));
        Assert.Throws<InvalidDataException>(() => loader.Load(truncated));
    }

    [Test]
    public void Load_RejectsDuplicateAndOutOfBoundsTables()
    {
        PdfTrueTypeFontLoader loader = new PdfTrueTypeFontLoader();
        byte[] duplicate = TestSfntFont.Create(0x00010000U, 0, false);
        Array.Copy(duplicate, 12, duplicate, 28, 4);
        byte[] outOfBounds = TestSfntFont.Create(0x00010000U, 0, false);
        BinaryPrimitives.WriteUInt32BigEndian(outOfBounds.AsSpan(20, 4), uint.MaxValue);

        Assert.Throws<InvalidDataException>(() => loader.Load(duplicate));
        Assert.Throws<InvalidDataException>(() => loader.Load(outOfBounds));
    }

    [Test]
    public void Load_RejectsFontWithoutSupportedUnicodeCmap()
    {
        byte[] data = TestSfntFont.Create(0x00010000U, 0, false);
        int cmapOffset = TestSfntFont.FindTableOffset(data, "cmap");
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(cmapOffset + 4, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(cmapOffset + 6, 2), 0);

        Assert.Throws<InvalidDataException>(() => new PdfTrueTypeFontLoader().Load(data));
    }

    [Test]
    public void Loader_ValidatesNullAndUnreadableInputs()
    {
        PdfTrueTypeFontLoader loader = new PdfTrueTypeFontLoader();
        using MemoryStream unreadable = new MemoryStream();
        unreadable.Dispose();

        Assert.Throws<ArgumentNullException>(() => loader.Load(null!));
        Assert.Throws<ArgumentNullException>(() => loader.LoadFromStream(null!));
        Assert.Throws<ArgumentException>(() => loader.LoadFromStream(unreadable));
        Assert.Throws<ArgumentNullException>(() => loader.LoadFromFile(null!));
    }
}

static class TestSfntFont
{
    public static byte[] Create(uint signature, ushort fsType, bool includeFormat12)
    {
        Dictionary<string, byte[]> tables = new Dictionary<string, byte[]>();
        tables.Add("OS/2", CreateOs2(fsType));
        tables.Add("cmap", CreateCmap(includeFormat12));
        tables.Add("glyf", CreateGlyf());
        tables.Add("head", CreateHead());
        tables.Add("hhea", CreateHhea());
        tables.Add("hmtx", CreateHmtx());
        tables.Add("loca", CreateLoca());
        tables.Add("maxp", CreateMaxp());
        tables.Add("name", CreateName());

        string[] tags = tables.Keys.OrderBy(tag => tag, StringComparer.Ordinal).ToArray();
        int tableCount = tags.Length;
        int directoryLength = 12 + tableCount * 16;
        int length = directoryLength;
        foreach (string tag in tags)
        {
            length = Align4(length);
            length += Align4(tables[tag].Length);
        }

        byte[] font = new byte[length];
        WriteUInt32(font, 0, signature);
        WriteUInt16(font, 4, (ushort)tableCount);
        int greatestPower = 1;
        int selector = 0;
        while (greatestPower <= tableCount / 2)
        {
            greatestPower *= 2;
            selector++;
        }

        WriteUInt16(font, 6, (ushort)(greatestPower * 16));
        WriteUInt16(font, 8, (ushort)selector);
        WriteUInt16(font, 10, (ushort)(tableCount * 16 - greatestPower * 16));

        int dataOffset = directoryLength;
        for (int index = 0; index < tableCount; index++)
        {
            string tag = tags[index];
            byte[] table = tables[tag];
            dataOffset = Align4(dataOffset);
            int record = 12 + index * 16;
            Encoding.ASCII.GetBytes(tag).CopyTo(font, record);
            WriteUInt32(font, record + 8, (uint)dataOffset);
            WriteUInt32(font, record + 12, (uint)table.Length);
            table.CopyTo(font, dataOffset);
            dataOffset += Align4(table.Length);
        }

        return font;
    }

    public static int FindTableOffset(byte[] font, string requestedTag)
    {
        int tableCount = ReadUInt16(font, 4);
        for (int index = 0; index < tableCount; index++)
        {
            int record = 12 + index * 16;
            string tag = Encoding.ASCII.GetString(font, record, 4);
            if (tag == requestedTag)
            {
                return (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(record + 8, 4));
            }
        }

        throw new InvalidOperationException("Fixture table was not found.");
    }

    static byte[] CreateHead()
    {
        byte[] table = new byte[54];
        WriteUInt32(table, 0, 0x00010000U);
        WriteUInt32(table, 12, 0x5F0F3CF5U);
        WriteUInt16(table, 18, 1000);
        WriteInt16(table, 36, -50);
        WriteInt16(table, 38, -200);
        WriteInt16(table, 40, 900);
        WriteInt16(table, 42, 800);
        WriteInt16(table, 50, 1);
        return table;
    }

    static byte[] CreateHhea()
    {
        byte[] table = new byte[36];
        WriteUInt32(table, 0, 0x00010000U);
        WriteInt16(table, 4, 800);
        WriteInt16(table, 6, -200);
        WriteInt16(table, 8, 200);
        WriteUInt16(table, 34, 3);
        return table;
    }

    static byte[] CreateHmtx()
    {
        byte[] table = new byte[14];
        WriteUInt16(table, 0, 500);
        WriteUInt16(table, 4, 600);
        WriteUInt16(table, 8, 700);
        return table;
    }

    static byte[] CreateMaxp()
    {
        byte[] table = new byte[6];
        WriteUInt32(table, 0, 0x00010000U);
        WriteUInt16(table, 4, 4);
        return table;
    }

    static byte[] CreateGlyf()
    {
        byte[] table = new byte[42];
        WriteInt16(table, 0, 0);
        WriteInt16(table, 2, 0);
        WriteInt16(table, 4, 0);
        WriteInt16(table, 6, 500);
        WriteInt16(table, 8, 700);
        WriteUInt16(table, 10, 0);

        WriteInt16(table, 12, 0);
        WriteInt16(table, 14, 0);
        WriteInt16(table, 16, 0);
        WriteInt16(table, 18, 500);
        WriteInt16(table, 20, 700);
        WriteUInt16(table, 22, 0);

        WriteInt16(table, 24, -1);
        WriteInt16(table, 26, 0);
        WriteInt16(table, 28, 0);
        WriteInt16(table, 30, 500);
        WriteInt16(table, 32, 700);
        WriteUInt16(table, 34, 1);
        WriteUInt16(table, 36, 1);
        WriteInt16(table, 38, 0);
        WriteInt16(table, 40, 0);
        return table;
    }

    static byte[] CreateLoca()
    {
        byte[] table = new byte[20];
        WriteUInt32(table, 0, 0);
        WriteUInt32(table, 4, 0);
        WriteUInt32(table, 8, 12);
        WriteUInt32(table, 12, 24);
        WriteUInt32(table, 16, 42);
        return table;
    }

    static byte[] CreateOs2(ushort fsType)
    {
        byte[] table = new byte[90];
        WriteUInt16(table, 0, 2);
        WriteUInt16(table, 8, fsType);
        WriteInt16(table, 86, 450);
        WriteInt16(table, 88, 700);
        return table;
    }

    static byte[] CreateName()
    {
        (ushort Id, string Value)[] values =
        {
            (1, "Test Sans"),
            (2, "Regular"),
            (4, "Test Sans Regular"),
            (6, "TestSans-Regular")
        };
        List<byte> strings = new List<byte>();
        byte[] table = new byte[6 + values.Length * 12];
        WriteUInt16(table, 0, 0);
        WriteUInt16(table, 2, (ushort)values.Length);
        WriteUInt16(table, 4, (ushort)table.Length);
        for (int index = 0; index < values.Length; index++)
        {
            byte[] value = Encoding.BigEndianUnicode.GetBytes(values[index].Value);
            int record = 6 + index * 12;
            WriteUInt16(table, record, 3);
            WriteUInt16(table, record + 2, 1);
            WriteUInt16(table, record + 4, 0x0409);
            WriteUInt16(table, record + 6, values[index].Id);
            WriteUInt16(table, record + 8, (ushort)value.Length);
            WriteUInt16(table, record + 10, (ushort)strings.Count);
            strings.AddRange(value);
        }

        return table.Concat(strings).ToArray();
    }

    static byte[] CreateCmap(bool includeFormat12)
    {
        byte[] format4 = CreateFormat4();
        if (!includeFormat12)
        {
            byte[] cmap = new byte[12 + format4.Length];
            WriteUInt16(cmap, 2, 1);
            WriteUInt16(cmap, 4, 3);
            WriteUInt16(cmap, 6, 1);
            WriteUInt32(cmap, 8, 12);
            format4.CopyTo(cmap, 12);
            return cmap;
        }

        byte[] format12 = CreateFormat12();
        byte[] combined = new byte[20 + format4.Length + format12.Length];
        WriteUInt16(combined, 2, 2);
        WriteUInt16(combined, 4, 3);
        WriteUInt16(combined, 6, 1);
        WriteUInt32(combined, 8, 20);
        WriteUInt16(combined, 12, 3);
        WriteUInt16(combined, 14, 10);
        WriteUInt32(combined, 16, (uint)(20 + format4.Length));
        format4.CopyTo(combined, 20);
        format12.CopyTo(combined, 20 + format4.Length);
        return combined;
    }

    static byte[] CreateFormat4()
    {
        byte[] table = new byte[32];
        WriteUInt16(table, 0, 4);
        WriteUInt16(table, 2, (ushort)table.Length);
        WriteUInt16(table, 6, 4);
        WriteUInt16(table, 8, 4);
        WriteUInt16(table, 10, 1);
        WriteUInt16(table, 14, 67);
        WriteUInt16(table, 16, 0xFFFF);
        WriteUInt16(table, 20, 65);
        WriteUInt16(table, 22, 0xFFFF);
        WriteInt16(table, 24, -64);
        WriteInt16(table, 26, 1);
        return table;
    }

    static byte[] CreateFormat12()
    {
        byte[] table = new byte[28];
        WriteUInt16(table, 0, 12);
        WriteUInt32(table, 4, (uint)table.Length);
        WriteUInt32(table, 12, 1);
        WriteUInt32(table, 16, 0x1F600);
        WriteUInt32(table, 20, 0x1F600);
        WriteUInt32(table, 24, 3);
        return table;
    }

    static int Align4(int value)
    {
        return (value + 3) & -4;
    }

    static int ReadUInt16(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));
    }

    static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset, 2), value);
    }

    static void WriteInt16(byte[] data, int offset, short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(offset, 2), value);
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset, 4), value);
    }
}
