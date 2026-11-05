using ttf;

namespace ttf.Tests;

internal class FontClusterFallbackTests
{
    [Test]
    public void PrefersCompleteCombiningGraphemeCoverage()
    {
        FontFace primary = Face("Primary", new int[] { 0x0061 });
        FontFace fallback = Face(
            "Grapheme Fallback",
            new int[] { 0x0061, 0x0301 });
        FontSet set = Set(primary, fallback);

        FontMatch? match = set.ResolveCluster(
            new FontRequest("Primary"),
            "Latn",
            "a\u0301");

        Assert.That(match!.GetFace(), Is.SameAs(fallback));
    }

    [Test]
    public void PrefersCompleteEmojiSequenceCoverageWithoutRequiringZwjGlyph()
    {
        FontFace primary = Face("Primary", new int[] { 0x1F469 });
        FontFace fallback = Face(
            "Emoji Fallback",
            new int[] { 0x1F469, 0x1F4BB });
        FontSet set = Set(primary, fallback);

        FontMatch? match = set.ResolveCluster(
            new FontRequest("Primary"),
            "Zyyy",
            "\U0001F469\u200D\U0001F4BB");

        Assert.That(match!.GetFace(), Is.SameAs(fallback));
    }

    [Test]
    public void RequiresCoverageForTheExactVariationSequence()
    {
        FontFace nominal = Face("Primary", new int[] { 0x2764 });
        FontFace variation = VariationFace(
            "Variation Fallback",
            0x2764,
            0xFE0F);
        FontSet set = Set(nominal, variation);

        FontMatch? match = set.ResolveCluster(
            new FontRequest("Primary"),
            "Zyyy",
            "\u2764\uFE0F");

        Assert.That(match!.GetFace(), Is.SameAs(variation));
    }

    [Test]
    public void KeepsAnUnsafeSpanOnTheBestSinglePartialFace()
    {
        FontFace primary = Face("Primary", new int[] { 0x0061 });
        FontFace fallback = Face(
            "Partial Fallback",
            new int[] { 0x0062, 0x0301 });
        FontSet set = Set(primary, fallback);

        FontMatch? match = set.ResolveCluster(
            new FontRequest("Primary"),
            "Latn",
            "a\u0301b");

        Assert.That(match!.GetFace(), Is.SameAs(fallback));
    }

    [Test]
    public void PreservesFallbackOrderWhenCoverageIsEqual()
    {
        FontFace primary = Face("Primary", new int[] { 0x0061, 0x0301 });
        FontFace fallback = Face(
            "Equal Fallback",
            new int[] { 0x0061, 0x0301 });
        FontSet set = Set(primary, fallback);

        FontMatch? match = set.ResolveCluster(
            new FontRequest("Primary"),
            "Latn",
            "a\u0301");

        Assert.That(match!.GetFace(), Is.SameAs(primary));
    }

    [Test]
    public void ReturnsNullWithoutMatchesAndValidatesClusterText()
    {
        FontSet empty = new FontSet();
        FontSet populated = new FontSet();
        populated.Add(Face("Primary", new int[] { 0x0061 }));
        FontRequest request = new FontRequest("Primary");

        Assert.Multiple(() =>
        {
            Assert.That(empty.ResolveCluster(request, "Latn", "a"), Is.Null);
            Assert.Throws<ArgumentNullException>(
                () => populated.ResolveCluster(request, "Latn", null!));
            Assert.Throws<ArgumentException>(
                () => populated.ResolveCluster(request, "Latn", string.Empty));
            Assert.Throws<System.Text.DecoderFallbackException>(
                () => populated.ResolveCluster(request, "Latn", "\uD800"));
        });
    }

    [Test]
    public void ShapesEveryResolvedFontTransitionAsAnExplicitRun()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = Clone(source, "Primary", new int[] { 0x0041 });
        FontFace fallback = Clone(source, "Latin Fallback", new int[] { 0x0042 });
        FontSet set = Set(primary, fallback);

        TextShapingResult result = TextShaper.Shape(
            "ABBA",
            set,
            new FontRequest("Primary"));

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.EqualTo(3));
            Assert.That(result.GetRun(0).GetFontFace(), Is.SameAs(primary));
            Assert.That(result.GetRun(1).GetFontFace(), Is.SameAs(fallback));
            Assert.That(result.GetRun(2).GetFontFace(), Is.SameAs(primary));
            Assert.That(result.GetRun(0).GetFontInstance().GetFace(),
                Is.SameAs(primary));
            Assert.That(result.GetRun(1).GetFontInstance().GetFace(),
                Is.SameAs(fallback));
            Assert.That(result.GetRun(0).GetUtf16Start(), Is.Zero);
            Assert.That(result.GetRun(1).GetUtf16Start(), Is.EqualTo(1));
            Assert.That(result.GetRun(1).GetUtf16Length(), Is.EqualTo(2));
            Assert.That(result.GetRun(2).GetUtf16Start(), Is.EqualTo(3));
            Assert.That(result.GetDiagnosticCount(), Is.Zero);
        });
    }

    [Test]
    public void ReportsMissingGlyphAfterConfiguredFallbacksAreExhausted()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = Clone(source, "Primary", new int[] { 0x0041 });
        FontFace fallback = Clone(source, "Latin Fallback", new int[] { 0x0042 });
        FontSet set = Set(primary, fallback);

        TextShapingResult result = TextShaper.Shape(
            "AC",
            set,
            new FontRequest("Primary"));

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.EqualTo(1));
            Assert.That(result.GetRun(0).GetFontFace(), Is.SameAs(primary));
            Assert.That(result.GetRun(0).GetShapedRun().GetGlyph(1)
                .GetGlyphId(), Is.Zero);
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            Assert.That(result.GetDiagnostic(0).GetMessage(),
                Does.Contain("U+0043"));
        });
    }

    [Test]
    public void ReportsClustersThatHaveNoConfiguredFontMatch()
    {
        TextShapingResult result = TextShaper.Shape(
            "A",
            new FontSet(),
            new FontRequest("Unavailable"));

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.Zero);
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            Assert.That(result.GetDiagnostic(0).GetLogicalPath(),
                Is.EqualTo("Shaping.Fallback[0:1]"));
        });
    }

    [Test]
    public void ReportsUnsupportedVariationSequenceAfterFallbackExhaustion()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = Clone(source, "Primary", new int[] { 0x2764 });
        FontSet set = new FontSet();
        set.Add(primary);

        TextShapingResult result = TextShaper.Shape(
            "\u2764\uFE0F",
            set,
            new FontRequest("Primary"));

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.EqualTo(1));
            Assert.That(result.GetRun(0).GetFontFace(), Is.SameAs(primary));
            Assert.That(result.GetRun(0).GetShapedRun().GetGlyph(0)
                .GetGlyphId(), Is.EqualTo(1));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            Assert.That(result.GetDiagnostic(0).GetMessage(),
                Does.Contain("U+2764 U+FE0F"));
        });
    }

    [Test]
    public void ValidatesFallbackShapingInputs()
    {
        FontSet set = new FontSet();
        FontRequest request = new FontRequest("Primary");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                TextShaper.Shape(null!, set, request));
            Assert.Throws<ArgumentNullException>(() =>
                TextShaper.Shape("A", (FontSet?)null!, request));
            Assert.Throws<ArgumentNullException>(() =>
                TextShaper.Shape("A", set, (FontRequest?)null!));
            Assert.Throws<ArgumentNullException>(() =>
                TextShaper.Shape(
                    "A",
                    set,
                    request,
                    (TextShapingOptions?)null!));
        });
    }

    internal static FontSet Set(FontFace primary, FontFace fallback)
    {
        FontSet set = new FontSet();
        set.Add(primary);
        set.Add(fallback);
        set.AddFallbackFamily(
            fallback.GetNames().GetTypographicFamilyName()!.GetValue());
        return set;
    }

    static FontFace Face(string family, int[] codePoints)
    {
        return FontMatchingTests.CreateStaticFaceWithCmap(
            family,
            BuildCmap(BuildFormat12(codePoints), null),
            codePoints.Length + 1);
    }

    internal static FontFace Clone(
        FontFace source,
        string family,
        int[] codePoints)
    {
        return FontMatchingTests.CloneFaceWithCmap(
            source,
            family,
            BuildCmap(BuildFormat12(codePoints), null));
    }

    static FontFace VariationFace(
        string family,
        int codePoint,
        int variationSelector)
    {
        return FontMatchingTests.CreateStaticFaceWithCmap(
            family,
            BuildCmap(
                BuildFormat12(new int[] { codePoint }),
                BuildFormat14(codePoint, variationSelector, 2)),
            3);
    }

    static byte[] BuildFormat12(int[] codePoints)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32((uint)(16 + codePoints.Length * 12))
            .WriteUInt32(0)
            .WriteUInt32((uint)codePoints.Length);
        int index = 0;

        while (index < codePoints.Length)
        {
            builder.WriteUInt32((uint)codePoints[index])
                .WriteUInt32((uint)codePoints[index])
                .WriteUInt32((uint)(index + 1));
            index += 1;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildFormat14(
        int codePoint,
        int variationSelector,
        int glyphId)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(14)
            .WriteUInt32(30)
            .WriteUInt32(1)
            .WriteUInt24((uint)variationSelector)
            .WriteUInt32(0)
            .WriteUInt32(21)
            .WriteUInt32(1)
            .WriteUInt24((uint)codePoint)
            .WriteUInt16((ushort)glyphId)
            .CopyBytes();
    }

    static byte[] BuildCmap(byte[] nominal, byte[]? variation)
    {
        int tableCount = variation == null ? 1 : 2;
        int nominalOffset = 4 + tableCount * 8;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)tableCount)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt32((uint)nominalOffset);

        if (variation != null)
        {
            builder.WriteUInt16(0)
                .WriteUInt16(5)
                .WriteUInt32((uint)(nominalOffset + nominal.Length));
        }

        builder.WriteBytes(nominal);

        if (variation != null)
        {
            builder.WriteBytes(variation);
        }

        return builder.CopyBytes();
    }
}
