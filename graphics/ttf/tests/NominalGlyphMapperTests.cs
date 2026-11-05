namespace ttf.Tests;

public class NominalGlyphMapperTests
{
    [Test]
    public void MapsNominalAndVariationGlyphsAndReportsMissingGlyphs()
    {
        ShapingBuffer buffer = CreateBuffer(
            "A\uFE0F\u2764\uFE0FB\u200D\u200BC");
        buffer.SetMask(2, 0x20);
        buffer.SetSyllable(4, 6);

        ShapingMappingResult result = Map(buffer, 2);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetCount(), Is.EqualTo(6));
            Assert.That(buffer.GetSourceLength(), Is.EqualTo(8));
            Assert.That(buffer.GetScalarValue(0), Is.EqualTo(0x41));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(4));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(2));
            Assert.That(buffer.GetScalarValue(1), Is.EqualTo(0x2764));
            Assert.That(buffer.GetGlyphId(1), Is.EqualTo(9));
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(2));
            Assert.That(buffer.GetClusterLength(1), Is.EqualTo(2));
            Assert.That(buffer.GetMask(1), Is.EqualTo(0x20));
            Assert.That(buffer.GetGlyphId(2), Is.Zero);
            Assert.That(buffer.GetSyllable(2), Is.EqualTo(6));
            Assert.That(buffer.GetGlyphId(3), Is.EqualTo(7));
            Assert.That(buffer.IsDefaultIgnorable(3), Is.True);
            Assert.That(buffer.GetGlyphId(4), Is.Zero);
            Assert.That(buffer.IsDefaultIgnorable(4), Is.True);
            Assert.That(buffer.GetGlyphId(5), Is.Zero);
            Assert.That(result.GetConsumedVariationSelectorCount(),
                Is.EqualTo(2));
            Assert.That(result.GetDefaultIgnorableCount(), Is.EqualTo(4));
            Assert.That(result.GetMissingGlyphCount(), Is.EqualTo(2));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(2));
        });

        AssertMissing(result, 0, 0x42, 4, 1);
        AssertMissing(result, 1, 0x43, 7, 1);
        FontDiagnostic diagnostic = result.GetDiagnostic(0);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Warning));
            Assert.That(diagnostic.GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            Assert.That(diagnostic.GetMessage(),
                Is.EqualTo("No glyph mapping is available for U+0042."));
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(0x636D6170));
            Assert.That(diagnostic.GetLogicalPath(),
                Is.EqualTo("Shaping.Cmap[4]"));
        });
    }

    [Test]
    public void UsesVariationMappingWhenNominalGlyphIsMissing()
    {
        ShapingBuffer buffer = CreateBuffer("\u2603\uFE0F");

        ShapingMappingResult result = Map(buffer, 0);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetCount(), Is.EqualTo(1));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(10));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(2));
            Assert.That(result.GetConsumedVariationSelectorCount(),
                Is.EqualTo(1));
            Assert.That(result.GetMissingGlyphCount(), Is.Zero);
        });
    }

    [Test]
    public void ConsumesUnsupportedSelectorAfterBaseUsingNominalFallback()
    {
        ShapingBuffer buffer = CreateBuffer("A\uFE0E");
        ShapingBuffer supplementary = CreateBuffer("A\U000E0100");

        ShapingMappingResult result = Map(buffer, 0);
        ShapingMappingResult supplementaryResult = Map(supplementary, 0);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetCount(), Is.EqualTo(1));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(4));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(2));
            Assert.That(result.GetConsumedVariationSelectorCount(),
                Is.EqualTo(1));
            Assert.That(result.GetDefaultIgnorableCount(), Is.EqualTo(1));
            Assert.That(result.GetMissingGlyphCount(), Is.Zero);
            Assert.That(supplementary.GetCount(), Is.EqualTo(1));
            Assert.That(supplementary.GetGlyphId(0), Is.EqualTo(4));
            Assert.That(supplementary.GetClusterLength(0), Is.EqualTo(3));
            Assert.That(supplementary.GetSourceLength(), Is.EqualTo(3));
            Assert.That(supplementaryResult
                .GetConsumedVariationSelectorCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void KeepsOrphanSelectorAsAnIgnorableWithoutMissingReport()
    {
        ShapingBuffer buffer = CreateBuffer("\uFE0FA");

        ShapingMappingResult result = Map(buffer, 0);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetCount(), Is.EqualTo(2));
            Assert.That(buffer.GetScalarValue(0), Is.EqualTo(0xFE0F));
            Assert.That(buffer.GetGlyphId(0), Is.Zero);
            Assert.That(buffer.IsDefaultIgnorable(0), Is.True);
            Assert.That(buffer.GetGlyphId(1), Is.EqualTo(4));
            Assert.That(result.GetConsumedVariationSelectorCount(), Is.Zero);
            Assert.That(result.GetDefaultIgnorableCount(), Is.EqualTo(1));
            Assert.That(result.GetMissingGlyphCount(), Is.Zero);
        });
    }

    [Test]
    public void ReportsSupplementaryMissingGlyphSourceRange()
    {
        ShapingBuffer buffer = CreateBuffer("\U0001F600");

        ShapingMappingResult result = Map(buffer, 1);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetGlyphId(0), Is.Zero);
            Assert.That(result.GetMissingCodePoint(0), Is.EqualTo(0x1F600));
            Assert.That(result.GetMissingClusterStart(0), Is.Zero);
            Assert.That(result.GetMissingClusterLength(0), Is.EqualTo(2));
            Assert.That(result.GetDiagnostic(0).GetMessage(), Is.EqualTo(
                "No glyph mapping is available for U+1F600."));
        });
    }

    [Test]
    public void SuppressesLayoutControlsWithoutMissingGlyphDiagnostics()
    {
        ShapingBuffer buffer = CreateBuffer(
            "A\t\u000B\u000C\r\n\u0085\u00AD\u2028\u2029\u2060\uFEFFB");

        ShapingMappingResult result = Map(buffer, 1);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(4));
            Assert.That(result.GetMissingGlyphCount(), Is.EqualTo(1));
            Assert.That(result.GetMissingCodePoint(0), Is.EqualTo('B'));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.GetDefaultIgnorableCount(), Is.EqualTo(3));
        });

        int index = 1;
        while (index < buffer.GetCount() - 1)
        {
            Assert.That(buffer.GetGlyphId(index), Is.Zero);
            Assert.That(buffer.IsDefaultIgnorable(index), Is.True);
            index++;
        }
    }

    [Test]
    public void EnforcesDiagnosticLimitAndValidatesArguments()
    {
        SfntCmapMapping mapping = CreateMapping();
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        ShapingBuffer buffer = CreateBuffer("BC");
        ShapingMappingResult empty = Map(CreateBuffer(""), 0);

        FontLimitException? exception = Assert.Throws<FontLimitException>(() =>
            NominalGlyphMapper.Map(buffer, mapping, unicodeData, 1));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo(
                "Glyph mapping exceeds the configured diagnostic limit."));
            Assert.That(buffer.GetCount(), Is.EqualTo(2));
            Assert.That(buffer.HasGlyphId(0), Is.False);
            Assert.That(buffer.HasGlyphId(1), Is.False);
            Assert.That(empty.GetDiagnosticCount(), Is.Zero);
            Assert.That(empty.GetMissingGlyphCount(), Is.Zero);
            Assert.That(empty.GetConsumedVariationSelectorCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                NominalGlyphMapper.Map(null!, mapping, unicodeData, 0));
            Assert.Throws<ArgumentNullException>(() =>
                NominalGlyphMapper.Map(CreateBuffer("A"), null, unicodeData, 0));
            Assert.Throws<ArgumentNullException>(() => NominalGlyphMapper.Map(
                CreateBuffer("A"),
                mapping,
                null!,
                0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NominalGlyphMapper.Map(
                    CreateBuffer("A"),
                    mapping,
                    unicodeData,
                    -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetDiagnostic(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetMissingCodePoint(-1));
        });
    }

    static ShapingMappingResult Map(
        ShapingBuffer buffer,
        int maximumDiagnosticCount)
    {
        return NominalGlyphMapper.Map(
            buffer,
            CreateMapping(),
            UnicodeData.LoadDefault(),
            maximumDiagnosticCount);
    }

    static ShapingBuffer CreateBuffer(string text)
    {
        RuneBuffer runes = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        return new ShapingBuffer(runes, ShapingDirection.LeftToRight);
    }

    static SfntCmapMapping CreateMapping()
    {
        SfntCmapGroupedData groups = new SfntCmapGroupedData(
            false,
            new SfntCmapGroup[]
            {
                new SfntCmapGroup(0x41, 0x41, 4),
                new SfntCmapGroup(0x200D, 0x200D, 7),
                new SfntCmapGroup(0x2764, 0x2764, 8)
            });
        SfntCmapSubtable nominal = new SfntCmapSubtable(
            12,
            0,
            0,
            null,
            null,
            null,
            null,
            null,
            null,
            groups,
            null,
            null);
        SfntCmapVariationSelectorRecord selector =
            new SfntCmapVariationSelectorRecord(
                0xFE0F,
                new SfntCmapVariationDefaultRange[]
                {
                    new SfntCmapVariationDefaultRange(0x41, 0)
                },
                new SfntCmapVariationMapping[]
                {
                    new SfntCmapVariationMapping(0x2603, 10),
                    new SfntCmapVariationMapping(0x2764, 9)
                });
        return new SfntCmapMapping(
            new SfntCmapEncodingRecord[]
            {
                new SfntCmapEncodingRecord(0, 4, 0, nominal)
            },
            new int[] { 120 },
            false,
            new SfntCmapFormat14Data(
                new SfntCmapVariationSelectorRecord[] { selector }),
            new SfntCmapCoverageRange[0],
            16,
            16);
    }

    static void AssertMissing(
        ShapingMappingResult result,
        int index,
        int codePoint,
        int clusterStart,
        int clusterLength)
    {
        Assert.That(result.GetMissingCodePoint(index), Is.EqualTo(codePoint));
        Assert.That(result.GetMissingClusterStart(index),
            Is.EqualTo(clusterStart));
        Assert.That(result.GetMissingClusterLength(index),
            Is.EqualTo(clusterLength));
    }
}
