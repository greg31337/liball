namespace svg.Tests;

public class SvgMediaQueryTests
{
    [Test]
    public void ParserRetainsTypesModifiersLegacyFeaturesAndRangeComparisons()
    {
        string text = "only screen and (min-width: 40em) and (height < 900px), not print and (color)";

        SvgMediaQueryParseResult result = new SvgMediaQueryParser().ParseWithValueAndLimitsAndSource(text, new SvgCssLimits(), new SvgSourceUri("media.css"));
        SvgMediaQuery first = result.QueryList.Queries[0];
        SvgMediaQuery second = result.QueryList.Queries[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.QueryList.IsValid, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(first.Modifier, Is.EqualTo(SvgMediaQueryModifier.Only));
            Assert.That(first.MediaType, Is.EqualTo("screen"));
            Assert.That(first.Features.Select(feature => feature.Kind), Is.EqualTo(new[] { SvgMediaFeatureKind.Width, SvgMediaFeatureKind.Height }));
            Assert.That(first.Features.Select(feature => feature.Comparison), Is.EqualTo(new[] { SvgMediaComparison.MinimumInclusive, SvgMediaComparison.MaximumExclusive }));
            Assert.That(first.Features[0].NumericValue, Is.EqualTo(40));
            Assert.That(first.Features[0].Unit, Is.EqualTo("em"));
            Assert.That(first.Features[0].ValueText, Is.EqualTo("40em"));
            Assert.That(second.Modifier, Is.EqualTo(SvgMediaQueryModifier.Not));
            Assert.That(second.MediaType, Is.EqualTo("print"));
            Assert.That(second.Features.Single().Comparison, Is.EqualTo(SvgMediaComparison.Boolean));
            Assert.That(first.Source.Source.Uri, Is.EqualTo("media.css"));
            Assert.That(first.Source.StartOffset, Is.Zero);
            Assert.That(second.Source.StartOffset, Is.EqualTo(text.IndexOf("not")));
        });
    }

    [Test]
    public void MediaTypesListsAndNotModifiersEvaluateAsAlternatives()
    {
        SvgStyleEnvironment environment = Environment(800, 600);

        Assert.Multiple(() =>
        {
            Assert.That(environment.Matches("screen", new SvgCssLimits()), Is.True);
            Assert.That(environment.Matches("print", new SvgCssLimits()), Is.False);
            Assert.That(environment.Matches("print, screen and (width: 800px)", new SvgCssLimits()), Is.True);
            Assert.That(environment.Matches("not print and (color)", new SvgCssLimits()), Is.True);
            Assert.That(environment.Matches("not screen and (color)", new SvgCssLimits()), Is.False);
            Assert.That(environment.Matches("unknown", new SvgCssLimits()), Is.False);
        });
    }

    [Test]
    public void ViewportDimensionsOrientationAndLengthUnitsEvaluateFromEnvironment()
    {
        SvgStyleEnvironment landscape = Environment(800, 600);
        landscape.RootFontSize = 20;
        SvgStyleEnvironment square = Environment(400, 400);

        Assert.Multiple(() =>
        {
            Assert.That(landscape.Matches("(min-width: 40em) and (max-height: 6.25in)", new SvgCssLimits()), Is.True);
            Assert.That(landscape.Matches("(width >= 800px) and (600px >= height)", new SvgCssLimits()), Is.True);
            Assert.That(landscape.Matches("(width > 800px)", new SvgCssLimits()), Is.False);
            Assert.That(landscape.Matches("(orientation: landscape)", new SvgCssLimits()), Is.True);
            Assert.That(landscape.Matches("(orientation: portrait)", new SvgCssLimits()), Is.False);
            Assert.That(landscape.Matches("(width: 100vw) and (height: 100vh)", new SvgCssLimits()), Is.True);
            Assert.That(square.Matches("(orientation: portrait)", new SvgCssLimits()), Is.True);
        });
    }

    [Test]
    public void ResolutionColorMonochromeAndReducedMotionEvaluateFromEnvironment()
    {
        SvgStyleEnvironment environment = Environment(1024, 768);
        environment.ResolutionDpi = 192;
        environment.ColorBits = 10;
        environment.MonochromeBits = 0;
        environment.ReducedMotion = true;

        Assert.Multiple(() =>
        {
            Assert.That(environment.Matches("(resolution: 2dppx)", new SvgCssLimits()), Is.True);
            Assert.That(environment.Matches("(min-resolution: 75.59055118110236dpcm)", new SvgCssLimits()), Is.True);
            Assert.That(environment.Matches("(max-resolution: 191dpi)", new SvgCssLimits()), Is.False);
            Assert.That(environment.Matches("(color) and (min-color: 8) and (max-color: 10)", new SvgCssLimits()), Is.True);
            Assert.That(environment.Matches("(monochrome)", new SvgCssLimits()), Is.False);
            Assert.That(environment.Matches("(monochrome: 0)", new SvgCssLimits()), Is.True);
            Assert.That(environment.Matches("(prefers-reduced-motion: reduce)", new SvgCssLimits()), Is.True);
            Assert.That(environment.Matches("(prefers-reduced-motion: no-preference)", new SvgCssLimits()), Is.False);
            Assert.That(environment.Matches("(prefers-reduced-motion)", new SvgCssLimits()), Is.True);
        });
    }

    [Test]
    public void UnknownFeaturesAreValidButSafelyDoNotMatch()
    {
        SvgMediaQueryParseResult result = new SvgMediaQueryParser().ParseWithValueAndLimits("screen and (vendor-feature: enabled), (unknown)", new SvgCssLimits());
        SvgStyleEnvironment environment = Environment(100, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.QueryList.IsValid, Is.True);
            Assert.That(result.QueryList.Queries.SelectMany(query => query.Features).All(feature => feature.Kind == SvgMediaFeatureKind.Unknown), Is.True);
            Assert.That(new SvgMediaQueryEvaluator().Matches(result.QueryList, environment), Is.False);
        });
    }

    [TestCase("")]
    [TestCase("screen and width")]
    [TestCase("screen and (width: 50%)")]
    [TestCase("screen and (orientation: diagonal)")]
    [TestCase("screen,")]
    [TestCase("only (color)")]
    [TestCase("(width >> 10px)")]
    public void MalformedMediaQueriesProduceLocatedCssDiagnostics(string text)
    {
        SvgSourceUri source = new SvgSourceUri("bad.css");
        SvgMediaQueryParseResult result = new SvgMediaQueryParser().ParseWithValueAndLimitsAndSource(text, new SvgCssLimits(), source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.QueryList.IsValid, Is.False);
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidCss), Is.True);
            Assert.That(result.Diagnostics.Where(diagnostic => diagnostic.Source != null).All(diagnostic => diagnostic.Source.Source == source), Is.True);
            Assert.That(new SvgMediaQueryEvaluator().Matches(result.QueryList, Environment(800, 600)), Is.False);
        });
    }

    [Test]
    public void TokenLimitsFailWithoutCreatingAnUnboundedQueryList()
    {
        SvgCssLimits limits = new SvgCssLimits();
        limits.MaxTokens = 3;

        SvgMediaQueryParseResult result = new SvgMediaQueryParser().ParseWithValueAndLimits("screen and (color)", limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.QueryList, Is.Null);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(result.ErrorOffset, Is.GreaterThanOrEqualTo(0));
        });
    }

    [Test]
    public void QueryAndFeatureCountsUseExistingCssCollectionBounds()
    {
        SvgCssLimits queryLimits = new SvgCssLimits();
        queryLimits.MaxRules = 1;
        SvgCssLimits featureLimits = new SvgCssLimits();
        featureLimits.MaxDeclarations = 1;

        SvgMediaQueryParseResult queries = new SvgMediaQueryParser().ParseWithValueAndLimits("screen, print", queryLimits);
        SvgMediaQueryParseResult features = new SvgMediaQueryParser().ParseWithValueAndLimits("screen and (color) and (width)", featureLimits);

        Assert.Multiple(() =>
        {
            Assert.That(queries.Success, Is.False);
            Assert.That(queries.QueryList, Is.Null);
            Assert.That(queries.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(features.Success, Is.False);
            Assert.That(features.QueryList, Is.Null);
            Assert.That(features.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void CssMediaRulesAttachLocatedQueriesAndRequireBlocks()
    {
        string css = "@media screen and (min-width: 600px) { rect { fill:red; } } @media print;";
        SvgSourceUri source = new SvgSourceUri("sheet.css");

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, new SvgCssLimits(), SvgStyleOrigin.Author, source);
        SvgCssAtRule valid = result.StyleSheet.Rules[0].AtRule;
        SvgCssRule invalid = result.StyleSheet.Rules[1];

        Assert.Multiple(() =>
        {
            Assert.That(valid.MediaQueries, Is.Not.Null);
            Assert.That(valid.MediaQueries.IsValid, Is.True);
            Assert.That(valid.MediaQueries.Queries.Single().Features.Single().Kind, Is.EqualTo(SvgMediaFeatureKind.Width));
            Assert.That(valid.MediaQueries.Queries[0].Source.Source, Is.SameAs(source));
            Assert.That(valid.MediaQueries.Queries[0].Source.StartOffset, Is.EqualTo(css.IndexOf("screen")));
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.Diagnostics.Single().Message, Does.Contain("requires a block"));
            Assert.That(result.StyleSheet.IsValid, Is.False);
        });
    }

    [Test]
    public void CollectedEmbeddedAndImportedMediaUseTheSameStyleEnvironment()
    {
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.BaseUri = "https://example.test/root.svg";
        SvgDocument document = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg><style media='screen and (min-width: 600px)'>rect {}</style><style media='print'>circle {}</style></svg>", loadOptions).Document;
        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits(document, new List<SvgStyleSheet>(), new SvgCssLimits());
        SvgStyleEnvironment environment = Environment(800, 600);

        Assert.Multiple(() =>
        {
            Assert.That(collection.StyleSheets[0].MatchesMedia(environment, new SvgCssLimits()), Is.True);
            Assert.That(collection.StyleSheets[1].MatchesMedia(environment, new SvgCssLimits()), Is.False);
        });
    }

    [Test]
    public void InvalidStyleEnvironmentsNeverMatch()
    {
        SvgMediaQueryList query = new SvgMediaQueryParser().ParseWithValueAndLimits("screen and (color)", new SvgCssLimits()).QueryList;
        SvgStyleEnvironment invalid = Environment(100, 100);
        invalid.ResolutionDpi = double.NaN;

        Assert.That(new SvgMediaQueryEvaluator().Matches(query, invalid), Is.False);
    }

    static SvgStyleEnvironment Environment(double width, double height)
    {
        SvgStyleEnvironment environment = new SvgStyleEnvironment();
        environment.ViewportWidth = width;
        environment.ViewportHeight = height;
        return environment;
    }
}
