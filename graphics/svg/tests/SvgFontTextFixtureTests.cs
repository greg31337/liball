namespace svg.Tests;

public class SvgFontTextFixtureTests
{
    [Test]
    public void ReadsRealCmapsOutlinesAndScaledMetrics()
    {
        SvgOpenTypeReadResult sans = Read("NotoSans-Regular.ttf");
        SvgOpenTypeReadResult arabic = Read("NotoSansArabic-Regular.ttf");
        SvgOpenTypeGlyphMapping capitalA = sans.Success ? sans.Font.MapCodePoint('A') : null;
        SvgOpenTypeGlyphMapping arabicSheen = arabic.Success ? arabic.Font.MapCodePoint(0x0634) : null;
        SvgTrueTypeGlyphResult outline = capitalA != null && capitalA.Success ? sans.Font.DecodeTrueTypeGlyph(capitalA.GlyphIndex) : null;
        SvgOpenTypeGlyphMetrics metrics = capitalA != null && capitalA.Success ? sans.Font.GetGlyphMetrics(capitalA.GlyphIndex, 20) : null;

        Assert.Multiple(() =>
        {
            Assert.That(sans.Success, Is.True);
            Assert.That(sans.Font.FamilyName, Is.EqualTo("Noto Sans"));
            Assert.That(sans.Font.OutlineKind, Is.EqualTo(SvgOpenTypeOutlineKind.TrueType));
            Assert.That(sans.Font.Head.UnitsPerEm, Is.EqualTo(1000));
            Assert.That(capitalA.Success, Is.True);
            Assert.That(capitalA.Found, Is.True);
            Assert.That(capitalA.GlyphIndex, Is.GreaterThan(0));
            Assert.That(outline.Success, Is.True);
            Assert.That(outline.Outline.Empty, Is.False);
            Assert.That(outline.Outline.Path.Commands, Is.Not.Empty);
            Assert.That(outline.Outline.Contours, Is.Not.Empty);
            Assert.That(metrics.Success, Is.True);
            Assert.That(metrics.Scale, Is.EqualTo(0.02));
            Assert.That(metrics.ScaledAdvanceWidth, Is.GreaterThan(0));
            Assert.That(arabic.Success, Is.True);
            Assert.That(arabic.Font.FamilyName, Is.EqualTo("Noto Sans Arabic"));
            Assert.That(arabicSheen.Success, Is.True);
            Assert.That(arabicSheen.Found, Is.True);
            Assert.That(arabicSheen.GlyphIndex, Is.GreaterThan(0));
        });
    }

    [Test]
    public void RejectsMalformedRealTablesAndEnforcesReadAndOutlineLimits()
    {
        byte[] bytes = Fixture("NotoSans-Regular.ttf");
        SvgOpenTypeReadResult valid = new SvgOpenTypeReader().ReadWithBytes(bytes);
        byte[] malformed = (byte[])bytes.Clone();
        SvgOpenTypeTable head = valid.Font.FindTable("head");
        malformed[head.Offset + 18] = 0;
        malformed[head.Offset + 19] = 0;
        SvgOpenTypeReadOptions malformedOptions = new SvgOpenTypeReadOptions();
        malformedOptions.ValidateChecksums = false;

        SvgOpenTypeReadOptions byteLimit = new SvgOpenTypeReadOptions();
        byteLimit.Limits.MaxBytesPerFont = bytes.Length - 1;
        SvgOpenTypeReadOptions tableLimit = new SvgOpenTypeReadOptions();
        tableLimit.Limits.MaxTablesPerFont = 1;
        SvgOpenTypeReadOptions glyphLimit = new SvgOpenTypeReadOptions();
        glyphLimit.Limits.MaxGlyphsPerFont = 1;
        SvgTrueTypeGlyphOptions pointLimit = new SvgTrueTypeGlyphOptions();
        pointLimit.Limits.MaxPointsPerGlyph = 3;
        SvgOpenTypeGlyphMapping capitalA = valid.Font.MapCodePoint('A');

        SvgOpenTypeReadResult badTable = new SvgOpenTypeReader().ReadWithBytesAndOptions(malformed, malformedOptions);
        SvgOpenTypeReadResult tooManyBytes = new SvgOpenTypeReader().ReadWithBytesAndOptions(bytes, byteLimit);
        SvgOpenTypeReadResult tooManyTables = new SvgOpenTypeReader().ReadWithBytesAndOptions(bytes, tableLimit);
        SvgOpenTypeReadResult tooManyGlyphs = new SvgOpenTypeReader().ReadWithBytesAndOptions(bytes, glyphLimit);
        SvgTrueTypeGlyphResult tooManyPoints = valid.Font.DecodeTrueTypeGlyphWithOptions(capitalA.GlyphIndex, pointLimit);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(badTable.Error, Is.EqualTo(SvgOpenTypeError.InvalidTable));
            Assert.That(tooManyBytes.Error, Is.EqualTo(SvgOpenTypeError.LimitExceeded));
            Assert.That(tooManyTables.Error, Is.EqualTo(SvgOpenTypeError.LimitExceeded));
            Assert.That(tooManyGlyphs.Error, Is.EqualTo(SvgOpenTypeError.LimitExceeded));
            Assert.That(tooManyPoints.Error, Is.EqualTo(SvgTrueTypeGlyphError.PointLimitExceeded));
        });
    }

    [Test]
    public void ReordersSupportedMixedDirectionTextAndBuildsRealFontFallbackRuns()
    {
        byte[] sansBytes = Fixture("NotoSans-Regular.ttf");
        byte[] arabicBytes = Fixture("NotoSansArabic-Regular.ttf");
        SvgFontDatabase database = new SvgFontDatabase();
        SvgFontFaceResult sans = database.AddFont(sansBytes);
        SvgFontFaceResult arabic = database.AddFont(arabicBytes);
        SvgFontMatchRequest request = new SvgFontMatchRequest();
        request.Families.Add("Noto Sans");
        request.Families.Add("Noto Sans Arabic");
        request.AllowAnyFamilyFallback = false;

        SvgFontFallbackResult fallback = database.CreateFallbackRuns("A\u0634A", request);
        SvgBidiResult bidi = new SvgBidiAnalyzer().AnalyzeWithText("A \u0634\u0628 B");

        Assert.Multiple(() =>
        {
            Assert.That(sans.Success, Is.True);
            Assert.That(arabic.Success, Is.True);
            Assert.That(sans.Face.Covers('A'), Is.True);
            Assert.That(sans.Face.Covers(0x0634), Is.False);
            Assert.That(arabic.Face.Covers(0x0634), Is.True);
            Assert.That(fallback.Success, Is.True);
            Assert.That(fallback.MissingCodePoints, Is.Zero);
            Assert.That(fallback.Runs, Has.Count.EqualTo(3));
            Assert.That(fallback.Runs[0].Face, Is.SameAs(sans.Face));
            Assert.That(fallback.Runs[1].Face, Is.SameAs(arabic.Face));
            Assert.That(fallback.Runs[2].Face, Is.SameAs(sans.Face));
            Assert.That(bidi.Success, Is.True);
            Assert.That(bidi.VisualText, Is.EqualTo("A \u0628\u0634 B"));
            Assert.That(bidi.VisualToLogical, Is.EqualTo(new[] { 0, 1, 3, 2, 4, 5 }));
            Assert.That(bidi.Scalars[2].Level % 2, Is.EqualTo(1));
            Assert.That(bidi.Scalars[3].Level % 2, Is.EqualTo(1));
        });
    }

    [Test]
    public void ShapesRealLigatureClustersAndPreservesUtf16Coverage()
    {
        SvgFont font = Read("NotoSans-Regular.ttf").Font;
        SvgShapingOptions options = new SvgShapingOptions();
        options.ScriptTag = "latn";
        options.LanguageTag = "dflt";
        options.EmSize = 20;

        SvgShapingResult result = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "office", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Glyphs, Is.Not.Empty);
            Assert.That(result.Glyphs.Count, Is.LessThan(6));
            Assert.That(result.Glyphs.Any(glyph => glyph.ScalarCount > 1), Is.True);
            Assert.That(result.Glyphs.All(glyph => glyph.FontIdentifier == 0), Is.True);
            Assert.That(result.Glyphs.All(glyph => !glyph.Missing), Is.True);
            Assert.That(result.Glyphs.Sum(glyph => glyph.ScalarCount), Is.EqualTo(6));
            Assert.That(result.Glyphs.Sum(glyph => glyph.Utf16Length), Is.EqualTo(6));
            Assert.That(result.AppliedFeatures, Does.Contain("liga"));
        });
    }

    [Test]
    public void PositionsRealVerticalGlyphsAndAppliesTextLengthScaling()
    {
        SvgFont font = Read("NotoSans-Regular.ttf").Font;
        SvgShapingOptions verticalOptions = new SvgShapingOptions();
        verticalOptions.Vertical = true;
        verticalOptions.EmSize = 20;
        SvgShapingResult verticalShaping = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "ABC", verticalOptions);
        SvgTextLayoutOptions positioning = new SvgTextLayoutOptions();
        positioning.WritingMode = SvgWritingMode.VerticalRightToLeft;
        positioning.FontSize = 20;
        positioning.Ascent = 16;
        positioning.Descent = 4;
        positioning.Y.Add(10);
        positioning.Dx.AddRange(new[] { 2.0, 4.0, 6.0 });
        positioning.Dy.AddRange(new[] { 1.0, 2.0, 3.0 });
        positioning.Rotate.AddRange(new[] { 10.0, 20.0 });
        SvgTextLayoutResult vertical = new SvgTextLayout().LayoutWithShapingAndOptions(verticalShaping, positioning);

        SvgShapingOptions horizontalOptions = new SvgShapingOptions();
        horizontalOptions.EmSize = 20;
        SvgShapingResult horizontalShaping = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "ABC", horizontalOptions);
        SvgTextLayoutResult natural = new SvgTextLayout().LayoutWithShapingAndOptions(horizontalShaping, new SvgTextLayoutOptions());
        SvgTextLayoutOptions lengthOptions = new SvgTextLayoutOptions();
        lengthOptions.HasTextLength = true;
        lengthOptions.TextLength = natural.ActualTextLength * 0.5;
        lengthOptions.LengthAdjust = SvgTextLengthAdjust.SpacingAndGlyphs;
        SvgTextLayoutResult shortened = new SvgTextLayout().LayoutWithShapingAndOptions(horizontalShaping, lengthOptions);

        Assert.Multiple(() =>
        {
            Assert.That(verticalShaping.Success, Is.True);
            Assert.That(verticalShaping.Glyphs.Select(glyph => glyph.XAdvance), Is.All.Zero);
            Assert.That(verticalShaping.Glyphs.Select(glyph => glyph.YAdvance), Is.All.EqualTo(20));
            Assert.That(vertical.Success, Is.True);
            Assert.That(vertical.Glyphs.Select(glyph => glyph.X), Is.EqualTo(new[] { 2.0, 6.0, 12.0 }));
            Assert.That(vertical.Glyphs.Select(glyph => glyph.Y), Is.EqualTo(new[] { 11.0, 33.0, 56.0 }));
            Assert.That(vertical.Glyphs.Select(glyph => glyph.Rotation), Is.EqualTo(new[] { 10.0, 20.0, 20.0 }));
            Assert.That(natural.Success, Is.True);
            Assert.That(shortened.Success, Is.True);
            Assert.That(shortened.ActualTextLength, Is.EqualTo(natural.ActualTextLength * 0.5).Within(0.000001));
            Assert.That(shortened.GlyphScale, Is.EqualTo(0.5).Within(0.000001));
            Assert.That(shortened.Glyphs.Select(glyph => glyph.InlineScale), Is.All.EqualTo(0.5).Within(0.000001));
        });
    }

    [Test]
    public void ProjectsAndOutlinesRealGlyphsOnCalibratedTextPath()
    {
        SvgFont font = Read("NotoSans-Regular.ttf").Font;
        SvgShapingOptions shapingOptions = new SvgShapingOptions();
        shapingOptions.EmSize = 20;
        shapingOptions.FontIdentifier = 17;
        SvgShapingResult shaping = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "SVG", shapingOptions);
        SvgTextLayoutOptions positioning = new SvgTextLayoutOptions();
        positioning.FontSize = 20;
        positioning.Ascent = 16;
        positioning.Descent = 4;
        SvgTextLayoutResult text = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, positioning);
        SvgTextPathOptions pathOptions = new SvgTextPathOptions();
        pathOptions.HasPathLength = true;
        pathOptions.PathLength = 100;
        pathOptions.StartOffset = 10;
        SvgTextPathResult path = new SvgTextPathLayout().LayoutInline(text, "M0 0 L200 0", pathOptions);
        SvgTextGeometryOptions geometryOptions = new SvgTextGeometryOptions();
        geometryOptions.Fonts.Add(new SvgTextFontResource(17, font, 20));
        SvgTextGeometryResult geometry = new SvgTextGeometry().BuildWithSvgTextPathResultAndSvgTextGeometryOptions(path, geometryOptions);

        Assert.Multiple(() =>
        {
            Assert.That(shaping.Success, Is.True);
            Assert.That(text.Success, Is.True);
            Assert.That(path.Success, Is.True);
            Assert.That(path.PathLengthScale, Is.EqualTo(2));
            Assert.That(path.Glyphs[0].Point.X, Is.EqualTo(20));
            Assert.That(path.Glyphs.Select(glyph => glyph.Tangent.X), Is.All.EqualTo(1));
            Assert.That(path.Glyphs.Select(glyph => glyph.Visible), Is.All.True);
            Assert.That(geometry.Success, Is.True);
            Assert.That(geometry.Glyphs, Has.Count.EqualTo(3));
            Assert.That(geometry.Glyphs.Select(glyph => glyph.InkBounds.IsEmpty), Is.All.False);
            Assert.That(geometry.Path.Commands, Is.Not.Empty);
            Assert.That(geometry.InkBounds.IsEmpty, Is.False);
            Assert.That(geometry.AccessibleText, Is.EqualTo("SVG"));
        });
    }

    static SvgOpenTypeReadResult Read(string name)
    {
        return new SvgOpenTypeReader().ReadWithBytes(Fixture(name));
    }

    static byte[] Fixture(string name)
    {
        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "fixtures", "fonts", name));
        Assert.That(System.IO.File.Exists(path), Is.True, path);
        return System.IO.File.ReadAllBytes(path);
    }
}
