using ttf;

namespace ttf.Tests;

internal class FontInstanceMvarTests
{
    [Test]
    public void AppliesAllRecognizedMvarTargetsToGlobalInstanceMetrics()
    {
        FontFace face = CreateFace(true);
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        FontDesignMetrics defaults = face.CreateDefaultInstance()
            .GetGlobalMetrics();
        FontDesignMetrics varied = face.CreateInstance(selectedCoordinates)
            .GetGlobalMetrics();
        FontScaledMetrics scaled = varied.Scale(20);

        Assert.Multiple(() =>
        {
            Assert.That(defaults.GetAscender(), Is.EqualTo(800));
            Assert.That(defaults.GetSubscriptXSize(), Is.EqualTo(650));
            Assert.That(defaults.GetGaspRangeMaximumPpem(0), Is.EqualTo(10));
            Assert.That(varied.GetLineMetricSource(),
                Is.EqualTo(FontLineMetricSource.Typographic));
            Assert.That(varied.GetAscender(), Is.EqualTo(810));
            Assert.That(varied.GetDescender(), Is.EqualTo(-190));
            Assert.That(varied.GetLineGap(), Is.EqualTo(30));
            Assert.That(varied.GetCapHeight(), Is.EqualTo(710));
            Assert.That(varied.GetXHeight(), Is.EqualTo(460));
            Assert.That(varied.GetUnderlinePosition(), Is.EqualTo(-90));
            Assert.That(varied.GetUnderlineThickness(), Is.EqualTo(60));
            Assert.That(varied.GetStrikeoutPosition(), Is.EqualTo(310));
            Assert.That(varied.GetStrikeoutThickness(), Is.EqualTo(60));
            Assert.That(varied.HasScriptMetrics(), Is.True);
            Assert.That(varied.GetSubscriptXSize(), Is.EqualTo(660));
            Assert.That(varied.GetSubscriptYSize(), Is.EqualTo(610));
            Assert.That(varied.GetSubscriptXOffset(), Is.EqualTo(20));
            Assert.That(varied.GetSubscriptYOffset(), Is.EqualTo(150));
            Assert.That(varied.GetSuperscriptXSize(), Is.EqualTo(710));
            Assert.That(varied.GetSuperscriptYSize(), Is.EqualTo(660));
            Assert.That(varied.GetSuperscriptXOffset(), Is.EqualTo(30));
            Assert.That(varied.GetSuperscriptYOffset(), Is.EqualTo(490));
            Assert.That(varied.GetCaretSlopeRise(), Is.EqualTo(11));
            Assert.That(varied.GetCaretSlopeRun(), Is.EqualTo(10));
            Assert.That(varied.GetCaretOffset(), Is.EqualTo(13));
            Assert.That(varied.GetVerticalAscender(), Is.EqualTo(510));
            Assert.That(varied.GetVerticalDescender(), Is.EqualTo(-490));
            Assert.That(varied.GetVerticalLineGap(), Is.EqualTo(50));
            Assert.That(varied.GetVerticalCaretSlopeRise(), Is.EqualTo(8));
            Assert.That(varied.GetVerticalCaretSlopeRun(), Is.EqualTo(11));
            Assert.That(varied.GetVerticalCaretOffset(), Is.EqualTo(13));
            Assert.That(varied.GetGaspRangeCount(), Is.EqualTo(11));
            Assert.That(varied.GetGaspRangeMaximumPpem(0), Is.EqualTo(20));
            Assert.That(varied.GetGaspRangeMaximumPpem(9), Is.EqualTo(110));
            Assert.That(varied.GetGaspRangeMaximumPpem(10), Is.EqualTo(0xFFFF));
            Assert.That(varied.GetGaspRangeBehavior(0), Is.EqualTo(0x000F));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                varied.GetGaspRangeMaximumPpem(11));
            Assert.That(scaled.HasScriptMetrics(), Is.True);
            Assert.That(scaled.GetSubscriptXSize(), Is.EqualTo(13.2));
            Assert.That(scaled.GetSuperscriptYOffset(), Is.EqualTo(9.8));
            Assert.That(scaled.GetGaspRangeCount(), Is.EqualTo(11));
            Assert.That(scaled.GetGaspRangeMaximumPpem(0), Is.EqualTo(20));
            Assert.That(scaled.GetGaspRangeBehavior(0), Is.EqualTo(0x000F));
        });
    }

    [Test]
    public void AppliesWindowsClippingDeltasWithDescenderSignConversion()
    {
        FontFace face = CreateFace(false);
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        FontDesignMetrics varied = face
            .CreateInstance(selectedCoordinates)
            .GetGlobalMetrics();

        Assert.Multiple(() =>
        {
            Assert.That(varied.GetLineMetricSource(),
                Is.EqualTo(FontLineMetricSource.Windows));
            Assert.That(varied.GetAscender(), Is.EqualTo(910));
            Assert.That(varied.GetDescender(), Is.EqualTo(-260));
            Assert.That(varied.GetLineGap(), Is.Zero);
        });
    }

    internal static FontFace CreateFace(bool useTypographicMetrics)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Head, BuildHead());
        AddTable(tables, FontTags.Maxp, BuildMaxp());
        AddTable(tables, FontTags.Hhea, BuildHhea());
        AddTable(tables, FontTags.Vhea, BuildVhea());
        AddTable(tables, FontTags.Os2, BuildOs2(useTypographicMetrics));
        AddTable(tables, FontTags.Post, BuildPost());
        AddTable(tables, FontTags.Gasp, BuildGasp());
        AddTable(tables, FontTags.Name, BuildName());
        AddTable(tables, FontTags.Fvar, BuildFvar());
        AddTable(tables, FontTags.Mvar, BuildMvar());
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x4F54544FU,
            new SfntValidationResult(
                FontOutlineKind.Cff1,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static byte[] BuildMvar()
    {
        int[] tags = GetRecognizedTags();
        int storeOffset = 12 + tags.Length * 8;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(0).WriteUInt16(8)
            .WriteUInt16((ushort)tags.Length)
            .WriteUInt16((ushort)storeOffset);
        int index = 0;

        while (index < tags.Length)
        {
            builder.WriteUInt32((uint)tags[index])
                .WriteUInt16(0).WriteUInt16((ushort)index);
            index += 1;
        }

        builder
            .WriteUInt16(1).WriteUInt32(12).WriteUInt16(1).WriteUInt32(22)
            .WriteUInt16(1).WriteUInt16(1)
            .WriteInt16(0).WriteInt16(0x4000).WriteInt16(0x4000)
            .WriteUInt16((ushort)tags.Length)
            .WriteUInt16(1).WriteUInt16(1).WriteUInt16(0);
        index = 0;

        while (index < tags.Length)
        {
            builder.WriteInt16(20);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static int[] GetRecognizedTags()
    {
        int[] tags = new int[]
        {
            SfntMvarValueTags.CapHeight,
            SfntMvarValueTags.GaspRange0,
            SfntMvarValueTags.GaspRange1,
            SfntMvarValueTags.GaspRange2,
            SfntMvarValueTags.GaspRange3,
            SfntMvarValueTags.GaspRange4,
            SfntMvarValueTags.GaspRange5,
            SfntMvarValueTags.GaspRange6,
            SfntMvarValueTags.GaspRange7,
            SfntMvarValueTags.GaspRange8,
            SfntMvarValueTags.GaspRange9,
            SfntMvarValueTags.HorizontalAscender,
            SfntMvarValueTags.HorizontalClippingAscent,
            SfntMvarValueTags.HorizontalClippingDescent,
            SfntMvarValueTags.HorizontalCaretOffset,
            SfntMvarValueTags.HorizontalCaretRun,
            SfntMvarValueTags.HorizontalCaretRise,
            SfntMvarValueTags.HorizontalDescender,
            SfntMvarValueTags.HorizontalLineGap,
            SfntMvarValueTags.SubscriptXOffset,
            SfntMvarValueTags.SubscriptXSize,
            SfntMvarValueTags.SubscriptYOffset,
            SfntMvarValueTags.SubscriptYSize,
            SfntMvarValueTags.SuperscriptXOffset,
            SfntMvarValueTags.SuperscriptXSize,
            SfntMvarValueTags.SuperscriptYOffset,
            SfntMvarValueTags.SuperscriptYSize,
            SfntMvarValueTags.StrikeoutPosition,
            SfntMvarValueTags.StrikeoutSize,
            SfntMvarValueTags.UnderlinePosition,
            SfntMvarValueTags.UnderlineSize,
            SfntMvarValueTags.VerticalAscender,
            SfntMvarValueTags.VerticalCaretOffset,
            SfntMvarValueTags.VerticalCaretRun,
            SfntMvarValueTags.VerticalCaretRise,
            SfntMvarValueTags.VerticalDescender,
            SfntMvarValueTags.VerticalLineGap,
            SfntMvarValueTags.XHeight
        };
        Array.Sort(tags);
        return tags;
    }

    static byte[] BuildHead()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt32(0x00010000).WriteUInt32(0).WriteUInt32(0x5F0F3CF5)
            .WriteUInt16(0).WriteUInt16(1000).WriteInt64(0).WriteInt64(0)
            .WriteInt16(-50).WriteInt16(-200).WriteInt16(900).WriteInt16(800)
            .WriteUInt16(0).WriteUInt16(8).WriteInt16(2)
            .WriteInt16(0).WriteInt16(0)
            .CopyBytes();
    }

    static byte[] BuildMaxp()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000)
            .WriteUInt16(1)
            .CopyBytes();
    }

    static byte[] BuildHhea()
    {
        return BuildMetricHeader(750, -250, 50, 1, 0, 3);
    }

    static byte[] BuildVhea()
    {
        return BuildMetricHeader(500, -500, 40, -2, 1, 3);
    }

    static byte[] BuildMetricHeader(
        int ascender,
        int descender,
        int lineGap,
        int caretRise,
        int caretRun,
        int caretOffset)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16((short)ascender).WriteInt16((short)descender)
            .WriteInt16((short)lineGap)
            .WriteUInt16(1000).WriteInt16(0).WriteInt16(0).WriteInt16(1000)
            .WriteInt16((short)caretRise).WriteInt16((short)caretRun)
            .WriteInt16((short)caretOffset)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16(1)
            .CopyBytes();
    }

    static byte[] BuildOs2(bool useTypographicMetrics)
    {
        int selectionFlags = useTypographicMetrics ? 0x80 : 0;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(5).WriteInt16(500).WriteUInt16(400).WriteUInt16(5)
            .WriteUInt16(0)
            .WriteInt16(650).WriteInt16(600).WriteInt16(10).WriteInt16(140)
            .WriteInt16(700).WriteInt16(650).WriteInt16(20).WriteInt16(480)
            .WriteInt16(50).WriteInt16(300).WriteInt16(0);
        builder.WriteZeros(10);
        return builder
            .WriteUInt32(0).WriteUInt32(0).WriteUInt32(0).WriteUInt32(0)
            .WriteUInt32(0x54455354).WriteUInt16((ushort)selectionFlags)
            .WriteUInt16(0).WriteUInt16(0xFFFF)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(20)
            .WriteUInt16(900).WriteUInt16(250)
            .WriteUInt32(0).WriteUInt32(0)
            .WriteInt16(450).WriteInt16(700)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(160).WriteUInt16(720)
            .CopyBytes();
    }

    static byte[] BuildPost()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00030000).WriteInt32(0)
            .WriteInt16(-100).WriteInt16(50).WriteUInt32(0)
            .WriteUInt32(0).WriteUInt32(0).WriteUInt32(0).WriteUInt32(0)
            .CopyBytes();
    }

    static byte[] BuildGasp()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(11);
        int maximum = 10;

        while (maximum <= 100)
        {
            builder.WriteUInt16((ushort)maximum).WriteUInt16(0x000F);
            maximum += 10;
        }

        return builder.WriteUInt16(0xFFFF).WriteUInt16(0x000F).CopyBytes();
    }

    static byte[] BuildName()
    {
        byte[] value = System.Text.Encoding.BigEndianUnicode.GetBytes("Weight");
        return new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(18)
            .WriteUInt16(3).WriteUInt16(1).WriteUInt16(0x0409)
            .WriteUInt16(256).WriteUInt16((ushort)value.Length)
            .WriteUInt16(0).WriteBytes(value)
            .CopyBytes();
    }

    static byte[] BuildFvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(16).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(20).WriteUInt16(0).WriteUInt16(8)
            .WriteTag("wght")
            .WriteInt32(0).WriteInt32(0).WriteInt32(0x00010000)
            .WriteUInt16(0).WriteUInt16(256)
            .CopyBytes();
    }

    static void AddTable(
        List<SfntTableData> tables,
        int tag,
        byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }
}
