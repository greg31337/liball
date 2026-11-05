using ttf;

namespace ttf.Tests;

internal class FontInstanceShapingPublicTests
{
    [Test]
    public void ShapesStringsThroughGsubAndGpos()
    {
        FontInstance instance = LoadInstance();
        TextShapingResult ligatureResult = instance.ShapeText("fi");
        TextShapingResult kerningResult = instance.ShapeText("AV");
        TextShapingRun ligatureRun = ligatureResult.GetRun(0);
        ShapedRun ligature = ligatureRun.GetShapedRun();
        ShapedRun kerning = kerningResult.GetRun(0).GetShapedRun();

        Assert.Multiple(() =>
        {
            Assert.That(ligatureResult.GetRunCount(), Is.EqualTo(1));
            Assert.That(ligatureRun.GetFontInstance(), Is.SameAs(instance));
            Assert.That(ligatureRun.GetFontFace(), Is.SameAs(instance.GetFace()));
            Assert.That(ligatureRun.GetScript(), Is.EqualTo("Latn"));
            Assert.That(ligatureRun.GetScriptTag(), Is.EqualTo(Tag("latn")));
            Assert.That(ligatureRun.GetUtf16Start(), Is.Zero);
            Assert.That(ligatureRun.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(ligature.GetGlyphCount(), Is.EqualTo(1));
            Assert.That(ligature.GetGlyph(0).GetGlyphId(), Is.EqualTo(6));
            Assert.That(ligature.GetGlyph(0).IsLigature(), Is.True);
            Assert.That(kerning.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(kerning.GetGlyph(0).GetXAdvance(), Is.EqualTo(520));
            Assert.That(kerning.GetGlyph(1).GetXAdvance(), Is.EqualTo(600));
        });
    }

    [Test]
    public void ExplicitOptionsControlFeaturesDirectionAndDecoding()
    {
        FontInstance instance = LoadInstance();
        TextShapingOptions options = new TextShapingOptions();
        options.SetFeatures(new FontLayoutFeatureSetting[]
        {
            new FontLayoutFeatureSetting(Tag("liga"), 0, 0, 2)
        });
        TextShapingResult unligated = instance.ShapeTextWithOptions(
            "fi",
            options);

        TextShapingOptions kerningOptions = new TextShapingOptions();
        kerningOptions.SetFeatures(new FontLayoutFeatureSetting[]
        {
            new FontLayoutFeatureSetting(Tag("kern"), 0, 0, 2)
        });
        TextShapingResult unkerned = instance.ShapeTextWithOptions(
            "AV",
            kerningOptions);

        options.ClearFeatures();
        options.SetDirection(TextShapingDirection.TopToBottom);
        TextShapingResult vertical = instance.ShapeTextWithOptions(
            "A",
            options);

        options.SetDirection(TextShapingDirection.LeftToRight);
        options.SetDecodingMode(Utf16DecodingMode.Replacement);
        TextShapingResult replaced = instance.ShapeTextWithOptions(
            "\uD800",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(unligated.GetRun(0).GetShapedRun().GetGlyphCount(),
                Is.EqualTo(2));
            Assert.That(unkerned.GetRun(0).GetShapedRun().GetGlyph(0)
                .GetXAdvance(), Is.EqualTo(600));
            Assert.That(vertical.GetRun(0).GetShapedRun().GetGlyph(0)
                .GetXAdvance(), Is.Zero);
            Assert.That(vertical.GetRun(0).GetShapedRun().GetGlyph(0)
                .GetYAdvance(), Is.LessThan(0));
            Assert.That(replaced.GetSourceRuneCount(), Is.EqualTo(1));
            Assert.That(replaced.GetDiagnosticCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void PreservesScriptAndBidiRunBoundaries()
    {
        FontInstance instance = LoadInstance();
        TextShapingResult result = instance.ShapeText("A\u05D0\u05D1");
        TextShapingRun latin = result.GetRun(0);
        TextShapingRun hebrew = result.GetRun(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.EqualTo(2));
            Assert.That(result.GetScriptRunCount(), Is.EqualTo(2));
            Assert.That(result.GetBidiRunCount(), Is.EqualTo(2));
            Assert.That(latin.GetScript(), Is.EqualTo("Latn"));
            Assert.That(latin.GetBidiLevel(), Is.Zero);
            Assert.That(latin.GetUtf16Start(), Is.Zero);
            Assert.That(latin.GetUtf16Length(), Is.EqualTo(1));
            Assert.That(hebrew.GetScript(), Is.EqualTo("Hebr"));
            Assert.That(hebrew.GetBidiLevel() & 1, Is.EqualTo(1));
            Assert.That(hebrew.GetUtf16Start(), Is.EqualTo(1));
            Assert.That(hebrew.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(hebrew.GetFontInstance(), Is.SameAs(instance));
        });
    }

    [Test]
    public void ShapesCopiedCharacterSpansWithAndWithoutOptions()
    {
        FontInstance instance = LoadInstance();
        char[] source = "XfiY".ToCharArray();
        TextShapingResult defaults = instance.ShapeSpan(
            source.AsSpan(1, 2));
        source[1] = 'A';
        source[2] = 'V';

        TextShapingOptions options = new TextShapingOptions();
        options.SetFeatures(new FontLayoutFeatureSetting[]
        {
            new FontLayoutFeatureSetting(Tag("liga"), 0, 0, 2)
        });
        TextShapingResult explicitResult = instance.ShapeSpanWithOptions(
            "fi".AsSpan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(defaults.GetRun(0).GetShapedRun().GetGlyphCount(),
                Is.EqualTo(1));
            Assert.That(defaults.GetRun(0).GetShapedRun().GetGlyph(0)
                .GetGlyphId(), Is.EqualTo(6));
            Assert.That(defaults.GetRun(0).GetUtf16Start(), Is.Zero);
            Assert.That(defaults.GetRun(0).GetUtf16Length(), Is.EqualTo(2));
            Assert.That(explicitResult.GetRun(0).GetShapedRun().GetGlyphCount(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void SupportsEmptyInputsAndValidatesReferences()
    {
        FontInstance instance = LoadInstance();

        Assert.Multiple(() =>
        {
            Assert.That(instance.ShapeText("").GetRunCount(), Is.Zero);
            Assert.That(instance.ShapeSpan(ReadOnlySpan<char>.Empty)
                .GetRunCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                instance.ShapeText(null!));
            Assert.Throws<ArgumentNullException>(() =>
                instance.ShapeTextWithOptions("A", null!));
            Assert.Throws<ArgumentNullException>(() =>
                ShapeSpanWithNullOptions(instance));
            Assert.Throws<System.Text.DecoderFallbackException>(() =>
                instance.ShapeText("\uD800"));
        });
    }

    static void ShapeSpanWithNullOptions(FontInstance instance)
    {
        instance.ShapeSpanWithOptions("A".AsSpan(), null!);
    }

    static FontInstance LoadInstance()
    {
        return HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
