namespace svg.Tests;

public class SvgOpenTypeFontProcessorTests
{
    [Test]
    public void ExposesAxesPalettesAndSupportedColorLayers()
    {
        SvgFont font = VariableTrueTypeFont();

        SvgOpenTypeFontProfile profile = new SvgOpenTypeFontProcessor().ReadProfileWithFontAndOptions(font, new SvgOpenTypeProfileOptions());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Success, Is.True);
            Assert.That(profile.Status, Is.EqualTo(SvgOpenTypeInstanceStatus.Success));
            Assert.That(profile.Axes, Has.Count.EqualTo(1));
            Assert.That(profile.Axes[0].Tag, Is.EqualTo("wght"));
            Assert.That(profile.Axes[0].Minimum, Is.EqualTo(100));
            Assert.That(profile.Axes[0].Default, Is.EqualTo(400));
            Assert.That(profile.Axes[0].Maximum, Is.EqualTo(900));
            Assert.That(profile.Palettes, Has.Count.EqualTo(1));
            Assert.That(profile.Palettes[0].Colors.Select(color => new[] { color.Red, color.Green, color.Blue, color.Alpha }), Is.EqualTo(new[] { new byte[] { 255, 0, 0, 255 }, new byte[] { 0, 255, 0, 128 } }));
            Assert.That(profile.ColorVersion, Is.EqualTo(0));
            Assert.That(profile.ColorGlyphs.Single().GlyphIndex, Is.EqualTo(1));
            Assert.That(profile.ColorGlyphs.Single().Layers.Select(layer => layer.GlyphIndex), Is.EqualTo(new[] { 1, 0 }));
            Assert.That(profile.ColorGlyphs.Single().Layers.Select(layer => layer.PaletteEntryIndex), Is.EqualTo(new[] { 0, 1 }));
        });
    }

    [Test]
    public void InstantiatesAvarHvarGvarMetricsOutlinesAndColorPaints()
    {
        SvgFont font = VariableTrueTypeFont();
        SvgOpenTypeInstanceOptions options = new SvgOpenTypeInstanceOptions();
        options.EmSize = 20;
        options.Coordinates.Add(new SvgOpenTypeVariationCoordinate("wght", 900));

        SvgOpenTypeGlyphInstance result = new SvgOpenTypeFontProcessor().InstantiateGlyphWithFontAndGlyphIndexAndOptions(font, 1, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
            Assert.That(result.Coordinates.Single().UserValue, Is.EqualTo(900));
            Assert.That(result.Coordinates.Single().NormalizedValue, Is.EqualTo(.5));
            Assert.That(result.Metrics.AdvanceWidth, Is.EqualTo(625));
            Assert.That(result.Metrics.LeftSideBearing, Is.EqualTo(10));
            Assert.That(result.Metrics.ScaledAdvanceWidth, Is.EqualTo(12.5));
            Assert.That(result.TrueTypeOutline.Points.Select(point => point.X), Is.EqualTo(new[] { 5, 105, 105, 5 }));
            Assert.That(result.TrueTypeOutline.Path.Commands[0].Values, Is.EqualTo(new[] { 5d, 0d }));
            Assert.That(result.ColorGlyph.Layers, Has.Count.EqualTo(2));
            Assert.That(result.ColorGlyph.Layers[0].Color.Red, Is.EqualTo(255));
            Assert.That(result.ColorGlyph.Layers[1].Color.Green, Is.EqualTo(255));
            Assert.That(result.ColorGlyph.Layers[1].Color.Alpha, Is.EqualTo(128));
        });
    }

    [Test]
    public void EvaluatesCff2VariationStoreBlendOperators()
    {
        SvgFont font = Cff2Font([239, 189, 140, 16, 239, 21], true);
        SvgOpenTypeInstanceOptions options = new SvgOpenTypeInstanceOptions();
        options.Coordinates.Add(new SvgOpenTypeVariationCoordinate("wght", 900));

        SvgOpenTypeGlyphInstance result = new SvgOpenTypeFontProcessor().InstantiateGlyphWithFontAndGlyphIndexAndOptions(font, 0, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
            Assert.That(result.CffOutline.Cff2, Is.True);
            Assert.That(result.CffOutline.Path.Commands[0].Kind, Is.EqualTo(SvgPathCommandKind.MoveTo));
            Assert.That(result.CffOutline.Path.Commands[0].Values, Is.EqualTo(new[] { 150d, 100d }));
            Assert.That(options.CffOptions.VariationCoordinates, Is.Empty);
        });
    }

    [Test]
    public void UsesGvarPhantomPointsForMetricsWhenHvarIsAbsent()
    {
        SvgFont font = SvgTrueTypeGlyphDecoderTests.CreateFont(false, Array.Empty<byte>(), SvgTrueTypeGlyphDecoderTests.BuildRepeatedRectangleGlyph());
        Attach(font, "fvar", Fvar());
        Attach(font, "gvar", GvarPhantomAdvance());
        SvgOpenTypeInstanceOptions options = new SvgOpenTypeInstanceOptions();
        options.EmSize = 20;
        options.Coordinates.Add(new SvgOpenTypeVariationCoordinate("wght", 900));

        SvgOpenTypeGlyphInstance result = new SvgOpenTypeFontProcessor().InstantiateGlyphWithFontAndGlyphIndexAndOptions(font, 1, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Metrics.AdvanceWidth, Is.EqualTo(630));
            Assert.That(result.Metrics.LeftSideBearing, Is.EqualTo(10));
            Assert.That(result.Metrics.ScaledAdvanceWidth, Is.EqualTo(12.6));
            Assert.That(result.TrueTypeOutline.XMin, Is.EqualTo(0));
            Assert.That(result.TrueTypeOutline.XMax, Is.EqualTo(100));
        });
    }

    [Test]
    public void ReturnsPreciseCallbackRequirementsForUnsupportedPaintsAndOperators()
    {
        SvgFont colorFont = SvgTrueTypeGlyphDecoderTests.CreateFont(false, Array.Empty<byte>(), SvgTrueTypeGlyphDecoderTests.BuildRepeatedRectangleGlyph());
        Attach(colorFont, "CPAL", Cpal());
        Attach(colorFont, "COLR", ColrVersionOne());
        SvgOpenTypeFontProfile color = new SvgOpenTypeFontProcessor().ReadProfileWithFontAndOptions(colorFont, new SvgOpenTypeProfileOptions());

        SvgFont operatorFont = Cff2Font([12, 0], false);
        SvgOpenTypeGlyphInstance operation = new SvgOpenTypeFontProcessor().InstantiateGlyphWithFontAndGlyphIndexAndOptions(operatorFont, 0, new SvgOpenTypeInstanceOptions());

        Assert.Multiple(() =>
        {
            Assert.That(color.Success, Is.False);
            Assert.That(color.Status, Is.EqualTo(SvgOpenTypeInstanceStatus.CallbackRequired));
            Assert.That(color.RequiredTableTag, Is.EqualTo("COLR"));
            Assert.That(color.UnsupportedOperator, Is.EqualTo(1));
            Assert.That(color.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.CallbackRequired));
            Assert.That(operation.Success, Is.False);
            Assert.That(operation.Status, Is.EqualTo(SvgOpenTypeInstanceStatus.CallbackRequired));
            Assert.That(operation.RequiredTableTag, Is.EqualTo("CFF2"));
            Assert.That(operation.UnsupportedOperator, Is.EqualTo(3072));
            Assert.That(operation.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.CallbackRequired));
        });
    }

    [Test]
    public void EnforcesAxisPaletteAndCancellationBounds()
    {
        SvgFont font = VariableTrueTypeFont();
        SvgOpenTypeProfileOptions axes = new SvgOpenTypeProfileOptions();
        axes.Limits.MaxVariationAxes = 0;
        SvgOpenTypeFontProfile invalid = new SvgOpenTypeFontProcessor().ReadProfileWithFontAndOptions(font, axes);
        SvgOpenTypeProfileOptions palettes = new SvgOpenTypeProfileOptions();
        palettes.MaximumPaletteColors = 1;
        SvgOpenTypeFontProfile limited = new SvgOpenTypeFontProcessor().ReadProfileWithFontAndOptions(font, palettes);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        SvgOpenTypeInstanceOptions cancelledOptions = new SvgOpenTypeInstanceOptions();
        cancelledOptions.CancellationToken = cancellation.Token;
        SvgOpenTypeGlyphInstance cancelled = new SvgOpenTypeFontProcessor().InstantiateGlyphWithFontAndGlyphIndexAndOptions(font, 1, cancelledOptions);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(SvgOpenTypeInstanceStatus.InvalidArgument));
            Assert.That(limited.Status, Is.EqualTo(SvgOpenTypeInstanceStatus.LimitExceeded));
            Assert.That(cancelled.Status, Is.EqualTo(SvgOpenTypeInstanceStatus.Cancelled));
        });
    }

    static SvgFont VariableTrueTypeFont()
    {
        SvgFont font = SvgTrueTypeGlyphDecoderTests.CreateFont(false, Array.Empty<byte>(), SvgTrueTypeGlyphDecoderTests.BuildRepeatedRectangleGlyph());
        Attach(font, "fvar", Fvar());
        Attach(font, "avar", Avar());
        Attach(font, "HVAR", Hvar());
        Attach(font, "gvar", Gvar());
        Attach(font, "CPAL", Cpal());
        Attach(font, "COLR", Colr());
        return font;
    }

    static SvgFont Cff2Font(byte[] charString, bool variation)
    {
        byte[] cff = Cff2(charString, variation);
        SvgFont font = new SvgFont(cff, false);
        font.OutlineKind = SvgOpenTypeOutlineKind.Cff2;
        font.Tables.Add(new SvgOpenTypeTable("CFF2", 0, 0, cff.Length));
        font.Head = new SvgOpenTypeHeadTable();
        font.Head.UnitsPerEm = 1000;
        font.MaximumProfile = new SvgOpenTypeMaximumProfileTable();
        font.MaximumProfile.GlyphCount = 1;
        font.HorizontalMetrics.Add(new SvgOpenTypeHorizontalMetric(0, 500, 0));
        font.Success = true;
        if (variation) Attach(font, "fvar", Fvar());
        return font;
    }

    static void Attach(SvgFont font, string tag, byte[] table)
    {
        byte[] previous = font.Bytes;
        byte[] combined = new byte[previous.Length + table.Length];
        Array.Copy(previous, combined, previous.Length);
        Array.Copy(table, 0, combined, previous.Length, table.Length);
        font.Bytes = combined;
        font.Tables.Add(new SvgOpenTypeTable(tag, 0, previous.Length, table.Length));
    }

    static byte[] Fvar()
    {
        byte[] result = new byte[36];
        U16(result, 0, 1);
        U16(result, 4, 16);
        U16(result, 8, 1);
        U16(result, 10, 20);
        U16(result, 12, 0);
        U16(result, 14, 8);
        Tag(result, 16, "wght");
        Fixed(result, 20, 100);
        Fixed(result, 24, 400);
        Fixed(result, 28, 900);
        U16(result, 32, 0);
        U16(result, 34, 256);
        return result;
    }

    static byte[] Avar()
    {
        byte[] result = new byte[22];
        U16(result, 0, 1);
        U16(result, 6, 1);
        U16(result, 8, 3);
        F2(result, 10, -1);
        F2(result, 12, -1);
        F2(result, 14, 0);
        F2(result, 16, 0);
        F2(result, 18, 1);
        F2(result, 20, .5);
        return result;
    }

    static byte[] Hvar()
    {
        byte[] result = new byte[54];
        U16(result, 0, 1);
        U32(result, 4, 20);
        U16(result, 20, 1);
        U32(result, 22, 12);
        U16(result, 26, 1);
        U32(result, 28, 22);
        U16(result, 32, 1);
        U16(result, 34, 1);
        F2(result, 36, 0);
        F2(result, 38, 1);
        F2(result, 40, 1);
        U16(result, 42, 2);
        U16(result, 44, 1);
        U16(result, 46, 1);
        U16(result, 48, 0);
        I16(result, 50, 0);
        I16(result, 52, 50);
        return result;
    }

    static byte[] Gvar()
    {
        byte[] result = new byte[46];
        U16(result, 0, 1);
        U16(result, 4, 1);
        U16(result, 6, 0);
        U32(result, 8, 26);
        U16(result, 12, 2);
        U16(result, 14, 0);
        U32(result, 16, 26);
        U16(result, 20, 0);
        U16(result, 22, 0);
        U16(result, 24, 10);
        U16(result, 26, 1);
        U16(result, 28, 10);
        U16(result, 30, 10);
        U16(result, 32, 32768);
        F2(result, 34, 1);
        result[36] = 7;
        int index = 0;
        while (index < 8)
        {
            result[37 + index] = 10;
            index++;
        }
        result[45] = 135;
        return result;
    }

    static byte[] GvarPhantomAdvance()
    {
        byte[] result = Gvar();
        int index = 0;
        while (index < 8)
        {
            result[37 + index] = 0;
            index++;
        }
        result[42] = 30;
        return result;
    }

    static byte[] Cpal()
    {
        byte[] result = new byte[22];
        U16(result, 2, 2);
        U16(result, 4, 1);
        U16(result, 6, 2);
        U32(result, 8, 14);
        U16(result, 12, 0);
        result[14] = 0;
        result[15] = 0;
        result[16] = 255;
        result[17] = 255;
        result[18] = 0;
        result[19] = 255;
        result[20] = 0;
        result[21] = 128;
        return result;
    }

    static byte[] Colr()
    {
        byte[] result = new byte[28];
        U16(result, 2, 1);
        U32(result, 4, 14);
        U32(result, 8, 20);
        U16(result, 12, 2);
        U16(result, 14, 1);
        U16(result, 16, 0);
        U16(result, 18, 2);
        U16(result, 20, 1);
        U16(result, 22, 0);
        U16(result, 24, 0);
        U16(result, 26, 1);
        return result;
    }

    static byte[] ColrVersionOne()
    {
        byte[] result = new byte[38];
        U16(result, 0, 1);
        U32(result, 4, 34);
        U32(result, 8, 34);
        U32(result, 14, 34);
        return result;
    }

    static byte[] Cff2(byte[] charString, bool variation)
    {
        byte[] global = Index(Array.Empty<byte[]>());
        byte[] characters = Index([charString]);
        byte[] dictionaries = Index([[139, 139, 18]]);
        int topSize = variation ? 19 : 13;
        int characterOffset = 5 + topSize + global.Length;
        int dictionaryOffset = characterOffset + characters.Length;
        int variationOffset = dictionaryOffset + dictionaries.Length;
        List<byte> top = new List<byte>();
        DictionaryInteger(top, characterOffset);
        top.Add(17);
        DictionaryInteger(top, dictionaryOffset);
        top.Add(12);
        top.Add(36);
        if (variation)
        {
            DictionaryInteger(top, variationOffset);
            top.Add(24);
        }
        List<byte> result = new List<byte>();
        result.AddRange([2, 0, 5, 0, (byte)top.Count]);
        result.AddRange(top);
        result.AddRange(global);
        result.AddRange(characters);
        result.AddRange(dictionaries);
        if (variation) result.AddRange(CffVariationStore());
        return result.ToArray();
    }

    static byte[] CffVariationStore()
    {
        byte[] result = new byte[32];
        U16(result, 0, 30);
        U16(result, 2, 1);
        U32(result, 4, 12);
        U16(result, 8, 1);
        U32(result, 10, 22);
        U16(result, 14, 1);
        U16(result, 16, 1);
        F2(result, 18, 0);
        F2(result, 20, 1);
        F2(result, 22, 1);
        U16(result, 24, 1);
        U16(result, 26, 0);
        U16(result, 28, 1);
        U16(result, 30, 0);
        return result;
    }

    static byte[] Index(byte[][] objects)
    {
        List<byte> result = new List<byte>();
        U32(result, (uint)objects.Length);
        if (objects.Length == 0) return result.ToArray();
        result.Add(1);
        int offset = 1;
        result.Add((byte)offset);
        int index = 0;
        while (index < objects.Length)
        {
            offset += objects[index].Length;
            result.Add((byte)offset);
            index++;
        }
        index = 0;
        while (index < objects.Length)
        {
            result.AddRange(objects[index]);
            index++;
        }
        return result.ToArray();
    }

    static void DictionaryInteger(List<byte> target, int value)
    {
        target.Add(29);
        target.Add((byte)(value >> 24));
        target.Add((byte)(value >> 16));
        target.Add((byte)(value >> 8));
        target.Add((byte)value);
    }

    static void Tag(byte[] bytes, int offset, string tag)
    {
        int index = 0;
        while (index < 4)
        {
            bytes[offset + index] = (byte)tag[index];
            index++;
        }
    }

    static void Fixed(byte[] bytes, int offset, double value)
    {
        U32(bytes, offset, (uint)(int)(value * 65536));
    }

    static void F2(byte[] bytes, int offset, double value)
    {
        I16(bytes, offset, (int)(value * 16384));
    }

    static void I16(byte[] bytes, int offset, int value)
    {
        U16(bytes, offset, value & 65535);
    }

    static void U16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void U32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    static void U32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }
}
