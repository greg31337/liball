namespace svg.Tests;

public class SvgSvgFontTests
{
    [Test]
    public void ReadsFontAndGlyphDefaultsOverridesOutlinesAndChildContent()
    {
        SvgSvgFontReadResult result = Read(
            "<font horiz-adv-x='600' horiz-origin-x='10' horiz-origin-y='20'>" +
            "<font-face font-family='Legacy Sans' units-per-em='1000' ascent='800' descent='200'/>" +
            "<missing-glyph horiz-adv-x='500' d='M0 0L20 0L20 20Z'/>" +
            "<glyph unicode='A' glyph-name='A.alt, A' horiz-adv-x='650' vert-origin-x='310' vert-origin-y='790' vert-adv-y='900' d='M0 0L100 0L50 100Z'><path d='M1 1L2 2'/></glyph>" +
            "<glyph unicode='B' glyph-name='B' d='M0 0L10 10'/>" +
            "</font>");
        SvgSvgFont font = result.Font;
        SvgSvgGlyph a = font.FindGlyphByName("A.alt");
        SvgSvgGlyph b = font.FindGlyphByName("B");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(font.Family, Is.EqualTo("Legacy Sans"));
            Assert.That(font.UnitsPerEm, Is.EqualTo(1000));
            Assert.That(font.Ascent, Is.EqualTo(800));
            Assert.That(font.Descent, Is.EqualTo(200));
            Assert.That(font.HorizontalOriginX, Is.EqualTo(10));
            Assert.That(font.HorizontalOriginY, Is.EqualTo(20));
            Assert.That(font.HorizontalAdvance, Is.EqualTo(600));
            Assert.That(font.VerticalOriginX, Is.EqualTo(300));
            Assert.That(font.VerticalOriginY, Is.EqualTo(800));
            Assert.That(font.VerticalAdvance, Is.EqualTo(1000));
            Assert.That(a.Names, Is.EqualTo(new[] { "A.alt", "A" }));
            Assert.That(a.HorizontalAdvance, Is.EqualTo(650));
            Assert.That(a.HorizontalOriginX, Is.EqualTo(10));
            Assert.That(a.HorizontalOriginY, Is.EqualTo(20));
            Assert.That(a.VerticalOriginX, Is.EqualTo(310));
            Assert.That(a.VerticalOriginY, Is.EqualTo(790));
            Assert.That(a.VerticalAdvance, Is.EqualTo(900));
            Assert.That(a.Outline.IsValid, Is.True);
            Assert.That(a.Outline.Commands, Has.Count.EqualTo(4));
            Assert.That(a.HasChildContent, Is.True);
            Assert.That(font.FindOutline("A.alt"), Is.SameAs(a.Outline));
            Assert.That(b.HorizontalAdvance, Is.EqualTo(600));
            Assert.That(b.VerticalOriginX, Is.EqualTo(300));
            Assert.That(b.VerticalOriginY, Is.EqualTo(800));
            Assert.That(b.VerticalAdvance, Is.EqualTo(1000));
            Assert.That(font.MissingGlyph.IsMissingGlyph, Is.True);
            Assert.That(font.MissingGlyph.HorizontalAdvance, Is.EqualTo(500));
            Assert.That(font.MissingGlyph.Outline.Commands, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void SelectsGlyphsInDocumentOrderWithLigatureLanguageOrientationAndArabicFilters()
    {
        SvgSvgFont font = Read(
            "<font horiz-adv-x='600'><font-face units-per-em='1000'/>" +
            "<missing-glyph glyph-name='missing' d='M0 0Z'/>" +
            "<glyph unicode='fi' glyph-name='fi' d='M0 0L2 2'/>" +
            "<glyph unicode='f' glyph-name='f-horizontal' orientation='h' lang='en-US,fr' d='M0 0L3 3'/>" +
            "<glyph unicode='f' glyph-name='f-vertical' orientation='v' d='M0 0L4 4'/>" +
            "<glyph unicode='ا' glyph-name='arabic-initial' arabic-form='initial' d='M0 0L5 5'/>" +
            "<glyph unicode='ا' glyph-name='arabic-isolated' arabic-form='isolated' d='M0 0L6 6'/>" +
            "</font>").Font;

        SvgSvgGlyphMatch ligature = font.MatchGlyph("file", 0, SvgSvgFontOrientation.Horizontal, "en", SvgSvgArabicForm.None);
        SvgSvgGlyphMatch horizontal = font.MatchGlyph("fox", 0, SvgSvgFontOrientation.Horizontal, "en", SvgSvgArabicForm.None);
        SvgSvgGlyphMatch vertical = font.MatchGlyph("fox", 0, SvgSvgFontOrientation.Vertical, "", SvgSvgArabicForm.None);
        SvgSvgGlyphMatch initial = font.MatchGlyph("ا", 0, SvgSvgFontOrientation.Horizontal, "", SvgSvgArabicForm.Initial);
        SvgSvgGlyphMatch isolated = font.MatchGlyph("ا", 0, SvgSvgFontOrientation.Horizontal, "", SvgSvgArabicForm.Isolated);
        SvgSvgGlyphMatch noForm = font.MatchGlyph("ا", 0, SvgSvgFontOrientation.Horizontal, "", SvgSvgArabicForm.None);
        SvgSvgGlyphMatch missing = font.MatchGlyph("😀", 0, SvgSvgFontOrientation.Horizontal, "", SvgSvgArabicForm.None);
        SvgSvgGlyphMatch invalid = font.MatchGlyph("A", 1, SvgSvgFontOrientation.Horizontal, "", SvgSvgArabicForm.None);

        Assert.Multiple(() =>
        {
            Assert.That(ligature.Found, Is.True);
            Assert.That(ligature.Glyph.Names, Does.Contain("fi"));
            Assert.That(ligature.ConsumedLength, Is.EqualTo(2));
            Assert.That(horizontal.Glyph.Names, Does.Contain("f-horizontal"));
            Assert.That(horizontal.ConsumedLength, Is.EqualTo(1));
            Assert.That(vertical.Glyph.Names, Does.Contain("f-vertical"));
            Assert.That(initial.Glyph.Names, Does.Contain("arabic-initial"));
            Assert.That(isolated.Glyph.Names, Does.Contain("arabic-isolated"));
            Assert.That(noForm.Found, Is.False);
            Assert.That(noForm.UsedMissingGlyph, Is.True);
            Assert.That(noForm.Glyph, Is.SameAs(font.MissingGlyph));
            Assert.That(missing.Success, Is.True);
            Assert.That(missing.UsedMissingGlyph, Is.True);
            Assert.That(missing.ConsumedLength, Is.EqualTo(2));
            Assert.That(invalid.Success, Is.False);
        });
    }

    [Test]
    public void ResolvesHorizontalAndVerticalKerningFromUnicodeRangesAndGlyphNames()
    {
        SvgSvgFont font = Read(
            "<font horiz-adv-x='600'><font-face/>" +
            "<glyph unicode='A' glyph-name='A.alt'/>" +
            "<glyph unicode='a' glyph-name='a'/>" +
            "<glyph unicode='f' glyph-name='f'/>" +
            "<glyph unicode='i' glyph-name='i'/>" +
            "<glyph unicode='А' glyph-name='cyrillic-a'/>" +
            "<hkern u1='U+0041-005A' g1='f' u2='a' g2='i' k='40'/>" +
            "<hkern u1='U+4??' u2='U+0061' k='12'/>" +
            "<vkern g1='A.alt' g2='a' k='-10'/>" +
            "</font>").Font;
        SvgSvgGlyph aUpper = font.FindGlyphByName("A.alt");
        SvgSvgGlyph aLower = font.FindGlyphByName("a");
        SvgSvgGlyph f = font.FindGlyphByName("f");
        SvgSvgGlyph i = font.FindGlyphByName("i");
        SvgSvgGlyph cyrillic = font.FindGlyphByName("cyrillic-a");

        SvgSvgKerningResult unicode = font.GetKerning(aUpper, aLower, false);
        SvgSvgKerningResult names = font.GetKerning(f, i, false);
        SvgSvgKerningResult wildcard = font.GetKerning(cyrillic, aLower, false);
        SvgSvgKerningResult vertical = font.GetKerning(aUpper, aLower, true);
        SvgSvgKerningResult none = font.GetKerning(cyrillic, i, false);
        SvgSvgGlyph foreign = Read("<font horiz-adv-x='10'><font-face/><glyph unicode='x'/></font>").Font.Glyphs[0];
        SvgSvgKerningResult invalid = font.GetKerning(foreign, i, false);

        Assert.Multiple(() =>
        {
            Assert.That(unicode.Success && unicode.Found, Is.True);
            Assert.That(unicode.Adjustment, Is.EqualTo(40));
            Assert.That(names.Adjustment, Is.EqualTo(40));
            Assert.That(wildcard.Adjustment, Is.EqualTo(12));
            Assert.That(vertical.Adjustment, Is.EqualTo(-10));
            Assert.That(vertical.Rule.Vertical, Is.True);
            Assert.That(none.Success, Is.True);
            Assert.That(none.Found, Is.False);
            Assert.That(none.Adjustment, Is.Zero);
            Assert.That(invalid.Success, Is.False);
        });
    }

    [Test]
    public void DerivesCircularMetricDefaultsDeterministically()
    {
        SvgSvgFont allDefault = Read("<font horiz-adv-x='500'><font-face units-per-em='1200'/><glyph unicode='A'/></font>").Font;
        SvgSvgFont authoredVerticalOrigin = Read("<font horiz-adv-x='500' vert-origin-y='300'><font-face units-per-em='1200'/><glyph unicode='A'/></font>").Font;

        Assert.Multiple(() =>
        {
            Assert.That(allDefault.Ascent, Is.EqualTo(1200));
            Assert.That(allDefault.VerticalOriginY, Is.EqualTo(1200));
            Assert.That(allDefault.Descent, Is.EqualTo(1200));
            Assert.That(allDefault.VerticalOriginX, Is.EqualTo(250));
            Assert.That(allDefault.VerticalAdvance, Is.EqualTo(1200));
            Assert.That(authoredVerticalOrigin.Ascent, Is.EqualTo(900));
            Assert.That(authoredVerticalOrigin.VerticalOriginY, Is.EqualTo(300));
            Assert.That(authoredVerticalOrigin.Descent, Is.EqualTo(300));
        });
    }

    [Test]
    public void RejectsInvalidStructureMetricsGlyphsKerningAndConfiguredLimits()
    {
        SvgSvgFontReadResult notFont = new SvgSvgFontReader().ReadWithFontNode(FontNode("<g/>"));
        SvgSvgFontReadResult missingFace = Read("<font horiz-adv-x='10'/>");
        SvgSvgFontReadResult missingAdvance = Read("<font><font-face/></font>");
        SvgSvgFontReadResult negativeAdvance = Read("<font horiz-adv-x='10'><font-face/><glyph unicode='A' horiz-adv-x='-1'/></font>");
        SvgSvgFontReadResult invalidPath = Read("<font horiz-adv-x='10'><font-face/><glyph unicode='A' d='M0 0 Q'/></font>");
        SvgSvgFontReadResult invalidKerning = Read("<font horiz-adv-x='10'><font-face/><glyph unicode='A'/><hkern u1='U+41' k='1'/></font>");
        SvgSvgFontOptions options = new SvgSvgFontOptions();
        options.MaxGlyphs = 1;
        SvgSvgFontReadResult limited = Read("<font horiz-adv-x='10'><font-face/><glyph unicode='A'/><glyph unicode='B'/></font>", options);
        SvgSvgFontOptions cancelledOptions = new SvgSvgFontOptions();
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        cancellation.Cancel();
        cancelledOptions.CancellationToken = cancellation.Token;
        SvgSvgFontReadResult cancelled = Read("<font horiz-adv-x='10'><font-face/><glyph unicode='A'/></font>", cancelledOptions);

        Assert.Multiple(() =>
        {
            Assert.That(notFont.Error, Is.EqualTo(SvgSvgFontError.NotFontElement));
            Assert.That(missingFace.Error, Is.EqualTo(SvgSvgFontError.MissingFontFace));
            Assert.That(missingAdvance.Error, Is.EqualTo(SvgSvgFontError.InvalidMetric));
            Assert.That(negativeAdvance.Error, Is.EqualTo(SvgSvgFontError.InvalidMetric));
            Assert.That(invalidPath.Error, Is.EqualTo(SvgSvgFontError.InvalidGlyph));
            Assert.That(invalidKerning.Error, Is.EqualTo(SvgSvgFontError.InvalidKerning));
            Assert.That(limited.Error, Is.EqualTo(SvgSvgFontError.LimitExceeded));
            Assert.That(cancelled.Error, Is.EqualTo(SvgSvgFontError.Cancelled));
            Assert.That(new[] { notFont, missingFace, missingAdvance, negativeAdvance, invalidPath, invalidKerning, limited, cancelled }, Has.All.Matches<SvgSvgFontReadResult>(item => !item.Success && item.Diagnostics.Count == 1));
        });
    }

    [Test]
    public void LegacyCatalogReportsImplementedFontSemanticsAndPreservedMetadataSeparately()
    {
        SvgLegacyCatalog catalog = new SvgCatalogFactory().CreateLegacyCatalog();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.FindElement(SvgElementKind.Font).Handling, Is.EqualTo(SvgLegacyHandling.Implemented));
            Assert.That(catalog.FindElement(SvgElementKind.Glyph).Handling, Is.EqualTo(SvgLegacyHandling.Implemented));
            Assert.That(catalog.FindElement(SvgElementKind.MissingGlyph).Handling, Is.EqualTo(SvgLegacyHandling.Implemented));
            Assert.That(catalog.FindElement(SvgElementKind.HKern).Handling, Is.EqualTo(SvgLegacyHandling.Implemented));
            Assert.That(catalog.FindAttribute("", "glyph-name").Handling, Is.EqualTo(SvgLegacyHandling.Implemented));
            Assert.That(catalog.FindAttribute("", "unicode-range").Handling, Is.EqualTo(SvgLegacyHandling.PreserveOnly));
        });
    }

    static SvgSvgFontReadResult Read(string fontMarkup)
    {
        return Read(fontMarkup, new SvgSvgFontOptions());
    }

    static SvgSvgFontReadResult Read(string fontMarkup, SvgSvgFontOptions options)
    {
        return new SvgSvgFontReader().ReadWithFontNodeAndOptions(FontNode(fontMarkup), options);
    }

    static SvgNode FontNode(string fontMarkup)
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs>" + fontMarkup + "</defs></svg>";
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document.DocumentElement.Children[0].Children[0];
    }
}
