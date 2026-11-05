namespace svg.Tests;

public class SvgCssDeclarationExpansionTests
{
    [Test]
    public void MarkerExpansionPreservesImportanceOrderAndSourceProvenance()
    {
        SvgSourceUri source = new SvgSourceUri("style.css");
        SvgCssParseResult result = new SvgCssParser().ParseDeclarationListWithValueAndLimitsAndSource("marker: url(#tip) !important; fill:red", new SvgCssLimits(), source);
        SvgCssDeclaration marker = result.Declarations[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(marker.IsShorthand, Is.True);
            Assert.That(marker.RawText, Is.EqualTo("marker: url(#tip) !important;"));
            Assert.That(marker.ExpandedDeclarations.Select(value => value.Name), Is.EqualTo(new[] { "marker-start", "marker-mid", "marker-end" }));
            Assert.That(marker.ExpandedDeclarations.Select(value => value.ValueText), Is.All.EqualTo("url(#tip)"));
            Assert.That(marker.ExpandedDeclarations.Select(value => value.Important), Is.All.True);
            Assert.That(marker.ExpandedDeclarations.Select(value => value.SourceOrder), Is.All.EqualTo(marker.SourceOrder));
            Assert.That(marker.ExpandedDeclarations.Select(value => value.ShorthandName), Is.All.EqualTo("marker"));
            Assert.That(marker.ExpandedDeclarations.Select(value => value.Source.Source), Is.All.SameAs(source));
            Assert.That(result.Declarations[1].ExpandedDeclarations, Is.Empty);
        });
    }

    [Test]
    public void OverflowExpandsOneOrTwoAxesAndCssWideValues()
    {
        SvgCssParseResult result = new SvgCssParser().ParseDeclarationListWithValueAndLimits("overflow:hidden; overflow:clip auto; overflow:inherit", new SvgCssLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(Values(result.Declarations[0]), Is.EqualTo(new[] { "overflow-x:hidden", "overflow-y:hidden" }));
            Assert.That(Values(result.Declarations[1]), Is.EqualTo(new[] { "overflow-x:clip", "overflow-y:auto" }));
            Assert.That(Values(result.Declarations[2]), Is.EqualTo(new[] { "overflow-x:inherit", "overflow-y:inherit" }));
        });
    }

    [Test]
    public void TextDecorationExpandsUnorderedComponentsAndLonghandInitials()
    {
        SvgCssParseResult result = new SvgCssParser().ParseDeclarationListWithValueAndLimits("text-decoration: wavy overline rgb(1 2 3) underline; text-decoration:red", new SvgCssLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(Values(result.Declarations[0]), Is.EqualTo(new[]
            {
                "text-decoration-line:overline underline",
                "text-decoration-style:wavy",
                "text-decoration-color:rgb(1 2 3)"
            }));
            Assert.That(Values(result.Declarations[1]), Is.EqualTo(new[]
            {
                "text-decoration-line:none",
                "text-decoration-style:solid",
                "text-decoration-color:red"
            }));
        });
    }

    [Test]
    public void FontExpansionParsesModifiersSizeLineHeightAndFamilyList()
    {
        string css = "font: italic small-caps 700 condensed 16px/1.5 \"Open Sans\", serif; font:12px monospace";

        SvgCssParseResult result = new SvgCssParser().ParseDeclarationListWithValueAndLimits(css, new SvgCssLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(Values(result.Declarations[0]), Is.EqualTo(new[]
            {
                "font-style:italic",
                "font-variant:small-caps",
                "font-weight:700",
                "font-stretch:condensed",
                "font-size:16px",
                "font-family:\"Open Sans\", serif"
            }));
            Assert.That(Values(result.Declarations[1]), Is.EqualTo(new[]
            {
                "font-style:normal",
                "font-variant:normal",
                "font-weight:normal",
                "font-stretch:normal",
                "font-size:12px",
                "font-family:monospace"
            }));
        });
    }

    [Test]
    public void VariablesRemainPendingValuesOnEveryExpandedLonghand()
    {
        SvgCssParseResult result = new SvgCssParser().ParseDeclarationListWithValueAndLimits("font:var(--font); text-decoration:var(--decoration); font:12px \"var(fake)\"; font:menu", new SvgCssLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Declarations[0].ExpandedDeclarations, Has.Count.EqualTo(6));
            Assert.That(result.Declarations[0].ExpandedDeclarations.Select(value => value.ValueText), Is.All.EqualTo("var(--font)"));
            Assert.That(result.Declarations[1].ExpandedDeclarations, Has.Count.EqualTo(3));
            Assert.That(result.Declarations[1].ExpandedDeclarations.Select(value => value.ValueText), Is.All.EqualTo("var(--decoration)"));
            Assert.That(result.Declarations[2].ExpandedDeclarations.Last().ValueText, Is.EqualTo("\"var(fake)\""));
            Assert.That(result.Declarations[3].ExpandedDeclarations.Select(value => value.ValueText), Is.All.EqualTo("menu"));
        });
    }

    [Test]
    public void InvalidShorthandsAreLocatedAndRecoveryContinues()
    {
        string css = "marker:red; overflow:hidden visible auto; text-decoration:none underline; font:bold serif; fill:red";

        SvgCssParseResult result = new SvgCssParser().ParseDeclarationListWithValueAndLimitsAndSource(css, new SvgCssLimits(), new SvgSourceUri("bad.css"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Declarations, Has.Count.EqualTo(5));
            Assert.That(result.Declarations.Take(4).All(declaration => !declaration.IsValid), Is.True);
            Assert.That(result.Declarations.Take(4).All(declaration => declaration.IsShorthand), Is.True);
            Assert.That(result.Declarations.Take(4).All(declaration => declaration.ExpandedDeclarations.Count == 0), Is.True);
            Assert.That(result.Declarations[4].IsValid, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(4));
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidCss), Is.True);
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Source.Source.Uri == "bad.css"), Is.True);
        });
    }

    [Test]
    public void ShorthandExpansionUsesTheExistingDeclarationSecurityLimit()
    {
        SvgCssLimits tooSmall = new SvgCssLimits();
        tooSmall.MaxDeclarations = 5;
        SvgCssParseResult direct = new SvgCssParser().ParseDeclarationListWithValueAndLimits("font:12px serif", tooSmall);
        SvgCssLimits exhausted = new SvgCssLimits();
        exhausted.MaxDeclarations = 6;
        SvgCssParseResult following = new SvgCssParser().ParseDeclarationListWithValueAndLimits("font:12px serif; color:red", exhausted);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Success, Is.False);
            Assert.That(direct.Error, Is.EqualTo(SvgCssParseError.DeclarationLimitExceeded));
            Assert.That(following.Success, Is.False);
            Assert.That(following.Error, Is.EqualTo(SvgCssParseError.DeclarationLimitExceeded));
            Assert.That(direct.Diagnostics.Last().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void CollectionBudgetsCountGeneratedLonghands()
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg xmlns='http://www.w3.org/2000/svg'/>", new SvgLoadOptions());
        SvgStyleSheet caller = new SvgCssParser().ParseStyleSheetWithValueAndLimits("text { font:12px serif; }", new SvgCssLimits()).StyleSheet;
        SvgCssLimits limits = new SvgCssLimits();
        limits.MaxDeclarations = 5;

        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits(loaded.Document, new List<SvgStyleSheet> { caller }, limits);

        Assert.Multiple(() =>
        {
            Assert.That(collection.StyleSheets, Is.Empty);
            Assert.That(collection.IsValid, Is.False);
            Assert.That(collection.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void DirectExpansionHandlesLonghandsCallerShorthandsAndInvalidArguments()
    {
        SvgCssDeclaration fill = new SvgCssParser().ParseDeclarationListWithValueAndLimits("fill:red", new SvgCssLimits()).Declarations[0];
        SvgCssDeclaration custom = new SvgCssParser().ParseDeclarationListWithValueAndLimits("host-pair:a b", new SvgCssLimits()).Declarations[0];
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();
        catalog.Add(SvgPropertySupportEntry.CreateMetadata("host-pair", SvgPropertyValueSupportKind.Any, Array.Empty<string>(), "", "a b", false, "<ident>{2}", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.NotAnimatable, "", true, false, new[] { "host-first", "host-second" }));

        SvgCssDeclarationExpansionResult longhand = new SvgCssDeclarationParser().Expand(fill, catalog, new SvgCssLimits());
        SvgCssDeclarationExpansionResult unsupported = new SvgCssDeclarationParser().Expand(custom, catalog, new SvgCssLimits());
        SvgCssDeclarationExpansionResult invalid = new SvgCssDeclarationParser().Expand(null, catalog, new SvgCssLimits());

        Assert.Multiple(() =>
        {
            Assert.That(longhand.Success, Is.True);
            Assert.That(longhand.Declarations, Is.Empty);
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.Error, Is.EqualTo(SvgCssDeclarationExpansionError.UnsupportedShorthand));
            Assert.That(custom.IsValid, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(SvgCssDeclarationExpansionError.InvalidArgument));
        });
    }

    [Test]
    public void SupportsConditionsUseShorthandGrammarValidation()
    {
        SvgSupportsEnvironment environment = new SvgSupportsEnvironment();
        SvgCssLimits limits = new SvgCssLimits();

        Assert.Multiple(() =>
        {
            Assert.That(environment.Matches("(overflow: hidden auto)", limits), Is.True);
            Assert.That(environment.Matches("(overflow: hidden visible auto)", limits), Is.False);
            Assert.That(environment.Matches("(font: italic 12px serif)", limits), Is.True);
            Assert.That(environment.Matches("(font: bold serif)", limits), Is.False);
            Assert.That(environment.Matches("(marker: url(#tip))", limits), Is.True);
            Assert.That(environment.Matches("(marker: red)", limits), Is.False);
        });
    }

    static string[] Values(SvgCssDeclaration declaration)
    {
        return declaration.ExpandedDeclarations.Select(value => value.Name + ":" + value.ValueText).ToArray();
    }
}
