using System.Text;
using ttf;

namespace ttf.Tests;

internal class FontColorPalettesTests
{
    [Test]
    public void EnumeratesTypedPalettesColorsAndDefaultLabels()
    {
        FontFace face = CreateFace(true, true);
        FontColorPalettes palettes = face.GetColorPalettes();
        ColorPalette light = palettes.GetDefaultPalette();
        ColorPalette dark = palettes.GetPalette(1);
        ColorGlyphColor first = light.GetColor(0);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetColorPalettes(), Is.SameAs(palettes));
            Assert.That(palettes.GetRequestedLanguage(), Is.Null);
            Assert.That(palettes.GetVersion(), Is.EqualTo(1));
            Assert.That(palettes.GetPaletteCount(), Is.EqualTo(2));
            Assert.That(palettes.GetPaletteEntryCount(), Is.EqualTo(2));
            Assert.That(palettes.HasPaletteTypes(), Is.True);
            Assert.That(palettes.HasPaletteLabels(), Is.True);
            Assert.That(palettes.HasPaletteEntryLabels(), Is.True);
            Assert.That(light.GetIndex(), Is.Zero);
            Assert.That(light.GetEntryCount(), Is.EqualTo(2));
            Assert.That(light.HasType(), Is.True);
            Assert.That(light.GetPaletteType(),
                Is.EqualTo(ColorPaletteType.LightBackground));
            Assert.That(light.IsUsableWithLightBackground(), Is.True);
            Assert.That(light.IsUsableWithDarkBackground(), Is.False);
            Assert.That(dark.GetPaletteType(),
                Is.EqualTo(ColorPaletteType.DarkBackground));
            Assert.That(dark.IsUsableWithLightBackground(), Is.False);
            Assert.That(dark.IsUsableWithDarkBackground(), Is.True);
            Assert.That(first.GetRed(), Is.EqualTo(3));
            Assert.That(first.GetGreen(), Is.EqualTo(2));
            Assert.That(first.GetBlue(), Is.EqualTo(1));
            Assert.That(first.GetAlpha(), Is.EqualTo(255));
            Assert.That(light.HasLabel(), Is.True);
            Assert.That(light.GetLabelNameId(), Is.EqualTo(256));
            Assert.That(light.GetLabel()!.GetValue(), Is.EqualTo("Light"));
            Assert.That(dark.GetLabel()!.GetValue(), Is.EqualTo("Dark"));
            Assert.That(palettes.HasPaletteEntryLabel(0), Is.True);
            Assert.That(palettes.GetPaletteEntryLabelNameId(0), Is.EqualTo(258));
            Assert.That(palettes.GetPaletteEntryLabel(0)!.GetValue(),
                Is.EqualTo("Outline"));
            Assert.That(palettes.HasPaletteEntryLabel(1), Is.False);
            Assert.That(palettes.GetPaletteEntryLabel(1), Is.Null);
        });
    }

    [Test]
    public void SelectsLocalizedPaletteAndEntryLabels()
    {
        FontFace face = CreateFace(true, false);
        FontColorPalettes french = face.GetLocalizedColorPalettes("fr-FR");

        Assert.Multiple(() =>
        {
            Assert.That(french.GetRequestedLanguage(), Is.EqualTo("fr-FR"));
            Assert.That(french.GetPalette(0).GetLabel()!.GetValue(),
                Is.EqualTo("Clair"));
            Assert.That(french.GetPalette(0).GetLabel()!.GetMatchKind(),
                Is.EqualTo(FontNameMatchKind.ExactLanguage));
            Assert.That(french.GetPalette(1).GetLabel()!.GetValue(),
                Is.EqualTo("Sombre"));
            Assert.That(french.GetPaletteEntryLabel(0)!.GetValue(),
                Is.EqualTo("Contour"));
            Assert.Throws<ArgumentNullException>(() =>
                face.GetLocalizedColorPalettes(null!));
            Assert.Throws<ArgumentException>(() =>
                face.GetLocalizedColorPalettes("fr_FR"));
        });
    }

    [Test]
    public void ResolvesOrdinaryAndForegroundColors()
    {
        FontColorPalettes palettes = CreateFace(true, false).GetColorPalettes();
        ColorGlyphColor foreground = new ColorGlyphColor(90, 80, 70, 60);
        ColorGlyphColor ordinary = palettes.ResolveColor(1, 1, foreground);
        ColorGlyphColor resolvedForeground = palettes.ResolveColor(
            1,
            palettes.GetForegroundPaletteEntryIndex(),
            foreground);

        Assert.Multiple(() =>
        {
            Assert.That(palettes.GetForegroundPaletteEntryIndex(),
                Is.EqualTo(0xFFFF));
            Assert.That(palettes.IsForegroundPaletteEntry(0xFFFF), Is.True);
            Assert.That(palettes.IsForegroundPaletteEntry(1), Is.False);
            Assert.That(resolvedForeground, Is.SameAs(foreground));
            Assert.That(ordinary.GetRed(), Is.EqualTo(12));
            Assert.That(ordinary.GetGreen(), Is.EqualTo(11));
            Assert.That(ordinary.GetBlue(), Is.EqualTo(10));
            Assert.That(resolvedForeground.GetRed(), Is.EqualTo(90));
            Assert.That(resolvedForeground.GetGreen(), Is.EqualTo(80));
            Assert.That(resolvedForeground.GetBlue(), Is.EqualTo(70));
            Assert.That(resolvedForeground.GetAlpha(), Is.EqualTo(60));
            Assert.Throws<ArgumentNullException>(() =>
                palettes.ResolveColor(0, 0, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                palettes.ResolveColor(2, 0, foreground));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                palettes.ResolveColor(0, 2, foreground));
        });
    }

    [Test]
    public void SelectedPaletteMatchesResolvedInstanceColorGlyph()
    {
        FontFace face = CreateFace(true, true);
        FontInstance instance = face.CreateDefaultInstance();
        FontColorPalettes palettes = face.GetColorPalettes();
        ColorGlyphOptions options = new ColorGlyphOptions();
        options.SetPaletteIndex(1);
        options.SetForegroundColor(new ColorGlyphColor(90, 80, 70, 60));

        ColorGlyph glyph = new ColorGlyphResolver().ResolveGlyphWithOptions(
            instance,
            0,
            options);
        ColorPaint root = glyph.GetPaintGraph()!.GetRoot();
        ColorGlyphColor exposed = palettes.GetPalette(1).GetColor(0);
        ColorGlyphColor resolved = root.GetChild(0).GetChild(0).GetColor();
        ColorGlyphColor resolvedForeground = root.GetChild(1).GetChild(0).GetColor();

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetPaletteIndex(), Is.EqualTo(1));
            Assert.That(resolved.GetRed(), Is.EqualTo(exposed.GetRed()));
            Assert.That(resolved.GetGreen(), Is.EqualTo(exposed.GetGreen()));
            Assert.That(resolved.GetBlue(), Is.EqualTo(exposed.GetBlue()));
            Assert.That(resolved.GetAlpha(), Is.EqualTo(exposed.GetAlpha()));
            Assert.That(resolvedForeground.GetRed(), Is.EqualTo(90));
            Assert.That(resolvedForeground.GetAlpha(), Is.EqualTo(60));
        });
    }

    [Test]
    public void RepresentsAbsentPalettesAndValidatesAccessors()
    {
        FontColorPalettes palettes = CreateFace(false, false).GetColorPalettes();

        Assert.Multiple(() =>
        {
            Assert.That(palettes.GetPaletteCount(), Is.Zero);
            Assert.That(palettes.GetPaletteEntryCount(), Is.Zero);
            Assert.That(palettes.HasPaletteTypes(), Is.False);
            Assert.That(palettes.HasPaletteLabels(), Is.False);
            Assert.That(palettes.HasPaletteEntryLabels(), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                palettes.GetPalette(0));
            Assert.Throws<InvalidOperationException>(() =>
                palettes.GetDefaultPalette());
        });

        FontColorPalettes present = CreateFace(true, false).GetColorPalettes();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                present.GetPalette(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                present.GetPalette(0).GetColor(2));
            Assert.Throws<InvalidOperationException>(() =>
                present.GetPaletteEntryLabelNameId(1));
        });
    }

    static FontFace CreateFace(bool includeCpal, bool includeColr)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        tables.Add(CreateTable(FontTags.Maxp, BuildMaxp()));

        if (includeCpal)
        {
            tables.Add(CreateTable(FontTags.Cpal, BuildCpal()));
            tables.Add(CreateTable(FontTags.Name, BuildNameTable()));
        }
        if (includeColr)
        {
            tables.Add(CreateTable(FontTags.Colr, BuildColr()));
        }

        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntTableData CreateTable(int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static byte[] BuildMaxp()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16(1)
            .CopyBytes();
    }

    static byte[] BuildCpal()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt16(4)
            .WriteUInt32(44)
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt32(28)
            .WriteUInt32(36)
            .WriteUInt32(40)
            .WriteUInt32(1)
            .WriteUInt32(2)
            .WriteUInt16(256)
            .WriteUInt16(257)
            .WriteUInt16(258)
            .WriteUInt16(0xFFFF)
            .WriteUInt8(1).WriteUInt8(2).WriteUInt8(3).WriteUInt8(255)
            .WriteUInt8(4).WriteUInt8(5).WriteUInt8(6).WriteUInt8(255)
            .WriteUInt8(7).WriteUInt8(8).WriteUInt8(9).WriteUInt8(255)
            .WriteUInt8(10).WriteUInt8(11).WriteUInt8(12).WriteUInt8(255)
            .CopyBytes();
    }

    static byte[] BuildColr()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt32(14)
            .WriteUInt32(20)
            .WriteUInt16(2)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(2)
            .WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(0xFFFF)
            .CopyBytes();
    }

    static byte[] BuildNameTable()
    {
        int[] nameIds = new int[] { 256, 257, 258, 256, 257, 258 };
        int[] languageIds = new int[]
        {
            0x0409, 0x0409, 0x0409, 0x040C, 0x040C, 0x040C
        };
        string[] values = new string[]
        {
            "Light", "Dark", "Outline", "Clair", "Sombre", "Contour"
        };
        byte[][] encoded = new byte[values.Length][];
        int[] offsets = new int[values.Length];
        int nextOffset = 0;
        int index = 0;

        while (index < values.Length)
        {
            encoded[index] = Encoding.BigEndianUnicode.GetBytes(values[index]);
            offsets[index] = nextOffset;
            nextOffset += encoded[index].Length;
            index += 1;
        }

        int storageOffset = 6 + values.Length * 12;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)values.Length)
            .WriteUInt16((ushort)storageOffset);
        index = 0;

        while (index < values.Length)
        {
            builder
                .WriteUInt16(3)
                .WriteUInt16(1)
                .WriteUInt16((ushort)languageIds[index])
                .WriteUInt16((ushort)nameIds[index])
                .WriteUInt16((ushort)encoded[index].Length)
                .WriteUInt16((ushort)offsets[index]);
            index += 1;
        }

        index = 0;

        while (index < encoded.Length)
        {
            builder.WriteBytes(encoded[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }
}
