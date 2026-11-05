using ttf;

namespace ttf.Tests;

internal class TextLineControlsPublicTests
{
    [Test]
    public void AppliesConfiguredTabWidthAndReportsHardBreakAndWhitespaceOutcomes()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        TextLayoutOptions zeroTab = new TextLayoutOptions();
        zeroTab.SetTabWidth(0);
        TextLayoutOptions wideTab = new TextLayoutOptions();
        wideTab.SetTabWidth(25);
        string text = "A\tB  \n C";

        TextLayoutResult zero = Prepare(face, text, zeroTab).Reflow(1000);
        TextLayoutResult wide = Prepare(face, text, wideTab).Reflow(1000);
        TextLineResult first = wide.GetLine(0);
        PositionedGlyphResult zeroB = FindGlyphAt(zero.GetLine(0), 2);
        PositionedGlyphResult wideB = FindGlyphAt(first, 2);

        Assert.Multiple(() =>
        {
            Assert.That(wideTab.GetTabWidth(), Is.EqualTo(25));
            Assert.That(wide.GetLineCount(), Is.EqualTo(2));
            Assert.That(first.GetTabCount(), Is.EqualTo(1));
            Assert.That(first.EndedByHardBreak(), Is.True);
            Assert.That(first.WasWhitespaceTrimmed(), Is.True);
            Assert.That(first.GetVisibleUtf16Start(), Is.Zero);
            Assert.That(first.GetVisibleUtf16Length(), Is.EqualTo(3));
            Assert.That(first.GetAdvance() - zero.GetLine(0).GetAdvance(),
                Is.EqualTo(25).Within(0.000001));
            Assert.That(wideB.GetX() - zeroB.GetX(),
                Is.EqualTo(25).Within(0.000001));
            Assert.That(wide.GetLine(1).WasWhitespaceTrimmed(), Is.True);
            Assert.That(wide.GetLine(1).EndedByHardBreak(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                first.GetEllipsisText());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                wideTab.SetTabWidth(double.PositiveInfinity));
        });
    }

    [Test]
    public void UsesUax14BreaksAndMakesEmergencyBreakingOptional()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        TextLayoutOptions ordinaryOptions = new TextLayoutOptions();
        PreparedTextResult ordinaryPrepared = Prepare(
            face,
            "AA AA",
            ordinaryOptions);
        double ordinaryWidth = ordinaryPrepared.Reflow(1000)
            .GetLine(0).GetAdvance() * 0.6;

        TextLayoutOptions prohibitedOptions = new TextLayoutOptions();
        PreparedTextResult prohibitedPrepared = Prepare(
            face,
            "AAAA",
            prohibitedOptions);
        double unbrokenWidth = prohibitedPrepared.Reflow(1000)
            .GetLine(0).GetAdvance() * 0.4;
        TextLayoutOptions emergencyOptions = new TextLayoutOptions();
        emergencyOptions.SetAllowEmergencyBreaks(true);

        TextLayoutResult ordinary = ordinaryPrepared.Reflow(ordinaryWidth);
        TextLayoutResult prohibited = prohibitedPrepared.Reflow(unbrokenWidth);
        TextLayoutResult emergency = Prepare(
            face,
            "AAAA",
            emergencyOptions).Reflow(unbrokenWidth);

        Assert.Multiple(() =>
        {
            Assert.That(ordinary.GetLineCount(), Is.EqualTo(2));
            Assert.That(prohibited.GetLineCount(), Is.EqualTo(1));
            Assert.That(prohibited.GetLine(0).GetAdvance(),
                Is.GreaterThan(unbrokenWidth));
            Assert.That(emergency.GetLineCount(), Is.GreaterThan(1));
        });
    }

    [Test]
    public void InsertsHyphensForSoftHyphensAndCallerDictionaryPoints()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        TextLayoutOptions softOptions = new TextLayoutOptions();
        PreparedTextResult softPrepared = Prepare(
            face,
            "AB\u00ADCD",
            softOptions);
        double softWidth = softPrepared.Reflow(1000)
            .GetLine(0).GetAdvance() * 0.8;
        TextLayoutResult soft = softPrepared.Reflow(softWidth);

        HyphenationDictionary dictionary =
            new HyphenationDictionaryLoader().LoadFromBytes(
                System.Text.Encoding.UTF8.GetBytes(
                    "exception hy-phenation\n"));
        TextLayoutOptions dictionaryOptions = new TextLayoutOptions();
        dictionaryOptions.SetHyphenation(dictionary);
        PreparedTextResult dictionaryPrepared = Prepare(
            face,
            "hyphenation",
            dictionaryOptions);
        double dictionaryWidth = dictionaryPrepared.Reflow(1000)
            .GetLine(0).GetAdvance() * 0.45;
        TextLayoutResult matched = dictionaryPrepared.Reflow(dictionaryWidth);

        Assert.Multiple(() =>
        {
            Assert.That(soft.GetLineCount(), Is.EqualTo(2));
            Assert.That(soft.GetLine(0).HasInsertedHyphen(), Is.True);
            Assert.That(soft.GetLine(0).GetAdvance(),
                Is.GreaterThan(soft.GetLine(0).GetContentAdvance()));
            Assert.That(soft.GetLine(1).HasInsertedHyphen(), Is.False);
            Assert.That(matched.GetLineCount(), Is.EqualTo(2));
            Assert.That(matched.GetLine(0).GetVisibleUtf16Length(),
                Is.EqualTo(2));
            Assert.That(matched.GetLine(0).HasInsertedHyphen(), Is.True);
        });
    }

    [Test]
    public void ReportsEllipsisAndMaximumLineTruncation()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        TextLayoutOptions naturalOptions = new TextLayoutOptions();
        double naturalAdvance = Prepare(face, "AAAAAA", naturalOptions)
            .Reflow(1000).GetLine(0).GetAdvance();
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAllowEmergencyBreaks(true);
        options.SetMaximumLines(1);
        options.SetTrimmingMode(TextTrimmingMode.CharacterEllipsis);
        options.SetEllipsisText("...");

        TextLayoutResult layout = Prepare(face, "AAAAAA", options)
            .Reflow(naturalAdvance * 0.6);
        TextLineResult line = layout.GetLine(0);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetLineCount(), Is.EqualTo(1));
            Assert.That(layout.IsTruncated(), Is.True);
            Assert.That(layout.GetTruncationClusterIndex(), Is.LessThan(6));
            Assert.That(layout.GetTruncationUtf16Index(),
                Is.EqualTo(layout.GetTruncationClusterIndex()));
            Assert.That(line.HasEllipsis(), Is.True);
            Assert.That(line.GetEllipsisText(), Is.EqualTo("..."));
            Assert.That(line.GetAdvance(), Is.GreaterThan(
                line.GetContentAdvance()));
            Assert.That(line.HasInsertedHyphen(), Is.False);
            Assert.That(line.EndedByHardBreak(), Is.False);
        });
    }

    static PreparedTextResult Prepare(
        FontFace face,
        string text,
        TextLayoutOptions options)
    {
        FontSet set = new FontSet();
        set.Add(face);
        string family = face.GetNames().GetFamilyName()!.GetValue();
        return new TextLayoutEngine().Prepare(
            set,
            text,
            new TextLayoutStyle[]
            {
                new TextLayoutStyle(
                    0,
                    text.Length,
                    new FontRequest(family),
                    new TextStyle(12))
            },
            options);
    }

    static PositionedGlyphResult FindGlyphAt(
        TextLineResult line,
        int utf16Start)
    {
        int runIndex = 0;
        while (runIndex < line.GetRunCount())
        {
            PositionedRunResult run = line.GetVisualRun(runIndex);
            int glyphIndex = 0;
            while (glyphIndex < run.GetGlyphCount())
            {
                PositionedGlyphResult glyph = run.GetVisualGlyph(glyphIndex);
                if (glyph.GetUtf16Start() == utf16Start)
                {
                    return glyph;
                }
                glyphIndex += 1;
            }
            runIndex += 1;
        }
        throw new InvalidOperationException("The requested glyph was not found.");
    }
}
