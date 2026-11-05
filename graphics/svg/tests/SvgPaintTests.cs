namespace svg.Tests;

public class SvgPaintTests
{
    [Test]
    public void ProgrammaticPaintsValidateKindsPayloadsAndFlatFallbackOrder()
    {
        SvgPaint none = SvgPaint.NoPaint();
        SvgPaint color = SvgPaint.Solid(new SvgColor(SvgColorSpace.Srgb, 1, 0, 0, 1));
        SvgPaint context = SvgPaint.Context(SvgPaintKind.ContextStroke);
        List<SvgPaint> fallbacks = new List<SvgPaint> { SvgPaint.UrlReference("#second", new List<SvgPaint>()), color };
        SvgPaint url = SvgPaint.UrlReference("#first", fallbacks);

        fallbacks.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(none.Kind, Is.EqualTo(SvgPaintKind.None));
            Assert.That(color.Color.Red, Is.EqualTo(1));
            Assert.That(context.Kind, Is.EqualTo(SvgPaintKind.ContextStroke));
            Assert.That(url.Fallbacks, Has.Count.EqualTo(2));
            Assert.That(url.Source, Is.Empty);
            Assert.That(url.IsValid, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgPaint.Solid(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgPaint.Context(SvgPaintKind.Color));
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgPaint.UrlReference("", new List<SvgPaint>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgPaint.UrlReference("#x", new List<SvgPaint> { color, SvgPaint.UrlReference("#late", new List<SvgPaint>()) }));
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgPaint.UrlReference("#x", new List<SvgPaint> { context }));
        });
    }

    [Test]
    public void NoneSolidColorsAndContextPaintsParseCaseInsensitively()
    {
        SvgPaintParseResult none = Parse(" NoNe ");
        SvgPaintParseResult named = Parse("rebeccapurple");
        SvgPaintParseResult modern = Parse("oklch(54% 25% 20deg / .5)");
        SvgPaintParseResult fill = Parse("CONTEXT-FILL");
        SvgPaintParseResult escapedStroke = Parse("context\\2d stroke");

        Assert.Multiple(() =>
        {
            AssertSuccess(none, SvgPaintKind.None);
            Assert.That(none.Paint.Source, Is.EqualTo(" NoNe "));
            AssertSuccess(named, SvgPaintKind.Color);
            Assert.That(named.Paint.Color.Red, Is.EqualTo(102 / 255.0));
            AssertSuccess(modern, SvgPaintKind.Color);
            Assert.That(modern.Paint.Color.Space, Is.EqualTo(SvgColorSpace.Oklch));
            Assert.That(modern.Paint.Color.Alpha, Is.EqualTo(0.5));
            AssertSuccess(fill, SvgPaintKind.ContextFill);
            AssertSuccess(escapedStroke, SvgPaintKind.ContextStroke);
            Assert.That(fill.Paint.ToText(), Is.EqualTo("context-fill"));
        });
    }

    [Test]
    public void UrlReferencesSupportUnquotedQuotedAndEscapedContent()
    {
        SvgPaintParseResult fragment = Parse("url(#gradient)");
        SvgPaintParseResult external = Parse("URL( \"images/pattern.svg#tile\" )");
        SvgPaintParseResult spaced = Parse("url('folder/my pattern.svg#p')");
        SvgPaintParseResult escaped = Parse("url(#grad\\69 ent)");

        Assert.Multiple(() =>
        {
            AssertSuccess(fragment, SvgPaintKind.Url);
            Assert.That(fragment.Paint.Url, Is.EqualTo("#gradient"));
            Assert.That(fragment.Paint.ToText(), Is.EqualTo("url(\"#gradient\")"));
            Assert.That(external.Paint.Url, Is.EqualTo("images/pattern.svg#tile"));
            Assert.That(spaced.Paint.Url, Is.EqualTo("folder/my pattern.svg#p"));
            Assert.That(escaped.Paint.Url, Is.EqualTo("#gradient"));
            Assert.That(fragment.Paint.Source, Is.EqualTo("url(#gradient)"));
        });
    }

    [Test]
    public void UrlPaintsRetainOrderedUrlAndTerminalFallbackChains()
    {
        SvgPaintParseResult color = Parse("url(#first) url(\"#second\") rgb(255 0 0 / 50%)");
        SvgPaintParseResult none = Parse("url(#first) none");

        Assert.Multiple(() =>
        {
            AssertSuccess(color, SvgPaintKind.Url);
            Assert.That(color.Paint.Url, Is.EqualTo("#first"));
            Assert.That(color.Paint.Fallbacks, Has.Count.EqualTo(2));
            Assert.That(color.Paint.Fallbacks[0].Kind, Is.EqualTo(SvgPaintKind.Url));
            Assert.That(color.Paint.Fallbacks[0].Url, Is.EqualTo("#second"));
            Assert.That(color.Paint.Fallbacks[1].Kind, Is.EqualTo(SvgPaintKind.Color));
            Assert.That(color.Paint.Fallbacks[1].Color.Alpha, Is.EqualTo(0.5));
            Assert.That(color.Paint.ToText(), Is.EqualTo("url(\"#first\") url(\"#second\") color(srgb 1 0 0 / 0.5)"));
            Assert.That(none.Paint.Fallbacks, Has.Count.EqualTo(1));
            Assert.That(none.Paint.Fallbacks[0].Kind, Is.EqualTo(SvgPaintKind.None));
        });
    }

    [Test]
    public void StrictFallbackParsingStopsAtTheFirstFailureAndReturnsTheValidPrefix()
    {
        string input = "url(#first) url(#second) bogus(1) red";
        SvgPaintParseResult result = new SvgPaintParser().Parse(input, 8, 64, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgPaintParseError.UnknownPaint));
            Assert.That(result.ErrorOffset, Is.EqualTo(input.IndexOf("bogus", StringComparison.Ordinal)));
            Assert.That(result.Paint.Kind, Is.EqualTo(SvgPaintKind.Url));
            Assert.That(result.Paint.Fallbacks, Has.Count.EqualTo(1));
            Assert.That(result.Paint.Fallbacks[0].Url, Is.EqualTo("#second"));
        });
    }

    [Test]
    public void CompatibleFallbackParsingSkipsMalformedAtomsAndKeepsLaterCandidates()
    {
        string input = "url(#first) url(#second) bogus(1) url() context-fill url(#third) red";
        SvgPaintParseResult result = new SvgPaintParser().Parse(input, 10, 64, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgPaintParseError.UnknownPaint));
            Assert.That(result.ErrorOffset, Is.EqualTo(input.IndexOf("bogus", StringComparison.Ordinal)));
            Assert.That(result.Paint.Fallbacks, Has.Count.EqualTo(3));
            Assert.That(result.Paint.Fallbacks[0].Url, Is.EqualTo("#second"));
            Assert.That(result.Paint.Fallbacks[1].Url, Is.EqualTo("#third"));
            Assert.That(result.Paint.Fallbacks[2].Kind, Is.EqualTo(SvgPaintKind.Color));
            Assert.That(result.Paint.Fallbacks[2].Color.Red, Is.EqualTo(1));
        });
    }

    [Test]
    public void TerminalFallbacksRejectOrRecoverLaterCandidates()
    {
        string input = "url(#first) red url(#late)";
        SvgPaintParseResult strict = new SvgPaintParser().Parse(input, 4, 32, false);
        SvgPaintParseResult recovered = new SvgPaintParser().Parse(input, 4, 32, true);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Error, Is.EqualTo(SvgPaintParseError.InvalidFallback));
            Assert.That(strict.ErrorOffset, Is.EqualTo(input.IndexOf("url(#late)", StringComparison.Ordinal)));
            Assert.That(strict.Paint.Fallbacks, Has.Count.EqualTo(1));
            Assert.That(recovered.Success, Is.True);
            Assert.That(recovered.Recovered, Is.True);
            Assert.That(recovered.Error, Is.EqualTo(SvgPaintParseError.InvalidFallback));
            Assert.That(recovered.Paint.Fallbacks, Has.Count.EqualTo(1));
            Assert.That(recovered.Paint.Fallbacks[0].Kind, Is.EqualTo(SvgPaintKind.Color));
        });
    }

    [Test]
    public void InvalidPrimaryPaintAndUrlSyntaxNeverRecoverAsFallbacks()
    {
        SvgPaintParseResult emptyUrl = new SvgPaintParser().Parse("url() red", 4, 32, true);
        SvgPaintParseResult missingClose = new SvgPaintParser().Parse("url(#paint", 4, 32, true);
        SvgPaintParseResult escapedClose = new SvgPaintParser().Parse("url(#paint\\)", 4, 32, true);
        SvgPaintParseResult unquotedSpace = new SvgPaintParser().Parse("url(folder/my file.svg)", 4, 32, true);
        SvgPaintParseResult unknown = new SvgPaintParser().Parse("not-a-paint", 4, 32, true);

        Assert.Multiple(() =>
        {
            Assert.That(emptyUrl.Success, Is.False);
            Assert.That(emptyUrl.Error, Is.EqualTo(SvgPaintParseError.InvalidUrl));
            Assert.That(missingClose.Error, Is.EqualTo(SvgPaintParseError.InvalidUrl));
            Assert.That(escapedClose.Error, Is.EqualTo(SvgPaintParseError.InvalidUrl));
            Assert.That(unquotedSpace.Error, Is.EqualTo(SvgPaintParseError.InvalidUrl));
            Assert.That(unknown.Error, Is.EqualTo(SvgPaintParseError.UnknownPaint));
        });
    }

    [Test]
    public void ParserReportsBoundsEmptyInputAndRequiredSeparators()
    {
        SvgPaintParseResult nullInput = new SvgPaintParser().Parse(null, 2, 32, false);
        SvgPaintParseResult invalidFallbackBound = new SvgPaintParser().Parse("red", -1, 32, false);
        SvgPaintParseResult invalidTokenBound = new SvgPaintParser().Parse("red", 2, 0, false);
        SvgPaintParseResult empty = new SvgPaintParser().Parse(" ", 2, 32, false);
        SvgPaintParseResult fallbackLimit = new SvgPaintParser().Parse("url(#a) url(#b) red", 1, 32, false);
        SvgPaintParseResult urlLimit = new SvgPaintParser().Parse("url(#abcdef)", 2, 4, false);
        SvgPaintParseResult colorLimit = new SvgPaintParser().Parse("rebeccapurple", 2, 5, false);
        SvgPaintParseResult adjacent = new SvgPaintParser().Parse("url(#a)red", 2, 32, false);
        SvgPaintParseResult colorTrailing = new SvgPaintParser().Parse("red blue", 2, 32, false);

        Assert.Multiple(() =>
        {
            Assert.That(nullInput.Error, Is.EqualTo(SvgPaintParseError.InvalidArgument));
            Assert.That(invalidFallbackBound.Error, Is.EqualTo(SvgPaintParseError.InvalidArgument));
            Assert.That(invalidTokenBound.Error, Is.EqualTo(SvgPaintParseError.InvalidArgument));
            Assert.That(empty.Error, Is.EqualTo(SvgPaintParseError.ExpectedPaint));
            Assert.That(fallbackLimit.Error, Is.EqualTo(SvgPaintParseError.FallbackLimitExceeded));
            Assert.That(urlLimit.Error, Is.EqualTo(SvgPaintParseError.TokenTooLong));
            Assert.That(colorLimit.Error, Is.EqualTo(SvgPaintParseError.TokenTooLong));
            Assert.That(adjacent.Error, Is.EqualTo(SvgPaintParseError.InvalidFallback));
            Assert.That(colorTrailing.Error, Is.EqualTo(SvgPaintParseError.TrailingCharacters));
        });
    }

    [Test]
    public void InvalidMutablePaintStateFormatsAsEmptyText()
    {
        SvgPaint paint = SvgPaint.UrlReference("#paint", new List<SvgPaint>());
        paint.Fallbacks.Add(SvgPaint.Context(SvgPaintKind.ContextFill));

        Assert.That(paint.ToText(), Is.Empty);
    }

    static SvgPaintParseResult Parse(string value)
    {
        return new SvgPaintParser().Parse(value, 8, 128, false);
    }

    static void AssertSuccess(SvgPaintParseResult result, SvgPaintKind kind)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Recovered, Is.False);
        Assert.That(result.Error, Is.EqualTo(SvgPaintParseError.None));
        Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        Assert.That(result.Paint, Is.Not.Null);
        Assert.That(result.Paint.Kind, Is.EqualTo(kind));
        Assert.That(result.Paint.IsValid, Is.True);
    }
}
