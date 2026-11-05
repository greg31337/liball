namespace svg.Tests;

public class SvgCascadeVariableTests
{
    [Test]
    public void CustomPropertiesPreserveTokensAndTrackTransitiveDependencies()
    {
        string css = "rect { --base: red; --tone:var(--base); --family:  \"A B\" , serif ; " +
            "fill:var(--tone); font-family:var(--family); color:var(--missing,  blue); }";
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { Sheet(css) });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Find("--family").AuthoredValueText, Is.EqualTo("  \"A B\" , serif "));
            Assert.That(result.CustomProperties.Get("--family").RawText, Is.EqualTo("  \"A B\" , serif "));
            Assert.That(result.Find("font-family").ValueText, Is.EqualTo("  \"A B\" , serif "));
            Assert.That(result.Find("fill").Dependencies, Is.EqualTo(new[] { "--tone", "--base" }));
            Assert.That(result.Find("color").ValueText, Is.EqualTo("  blue"));
            Assert.That(result.Find("color").Dependencies, Is.EqualTo(new[] { "--missing" }));
            Assert.That(result.FindDependents("--base"), Does.Contain("--tone"));
            Assert.That(result.FindDependents("--base"), Does.Contain("fill"));
        });
    }

    [Test]
    public void CustomPropertiesInheritCaseSensitivelyAndCssWideValuesApply()
    {
        string css = "g { --Tone: rgb(1 2 3); --gone:green; } " +
            "rect { --gone:initial; --kept:unset; fill:var(--Tone); stroke:var(--tone, blue); }";
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode rect = group.Children[0];
        SvgStyleSheet sheet = Sheet(css);
        SvgCascadeResult parent = new SvgCascade().ResolveWithNodeAndStyleSheets(group, new List<SvgStyleSheet> { sheet });
        SvgCascadeOptions options = new SvgCascadeOptions { Parent = parent };

        SvgCascadeResult child = new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(rect, new List<SvgStyleSheet> { sheet }, options);

        Assert.Multiple(() =>
        {
            Assert.That(child.Find("--Tone").Kind, Is.EqualTo(SvgCascadedValueKind.Inherited));
            Assert.That(child.CustomProperties.Get("--Tone"), Is.Not.Null);
            Assert.That(child.Find("fill").ValueText, Is.EqualTo(" rgb(1 2 3)"));
            Assert.That(child.Find("stroke").ValueText, Is.EqualTo(" blue"));
            Assert.That(child.Find("--gone").Kind, Is.EqualTo(SvgCascadedValueKind.Initial));
            Assert.That(child.CustomProperties.Get("--gone"), Is.Null);
            Assert.That(child.Find("--kept").Kind, Is.EqualTo(SvgCascadedValueKind.Initial));
        });
    }

    [Test]
    public void CyclesAllowConsumerFallbackButOtherwiseInvalidateTheWinningValue()
    {
        string css = "rect { opacity:.25; --bad:not-a-number; opacity:var(--bad); " +
            "--a:var(--b); --b:var(--a); fill:var(--a, blue); stroke:var(--a); }";
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { Sheet(css) });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Find("opacity").ValueText, Is.EqualTo("1"));
            Assert.That(result.Find("opacity").Kind, Is.EqualTo(SvgCascadedValueKind.Initial));
            Assert.That(result.Find("opacity").VariableError, Is.EqualTo(SvgCssVariableError.InvalidComputedValue));
            Assert.That(result.Find("opacity").InvalidAtComputedValue, Is.True);
            Assert.That(result.Find("fill").ValueText, Is.EqualTo(" blue"));
            Assert.That(result.Find("fill").Dependencies, Is.EqualTo(new[] { "--a", "--b" }));
            Assert.That(result.Find("stroke").ValueText, Is.EqualTo("none"));
            Assert.That(result.Find("stroke").VariableError, Is.EqualTo(SvgCssVariableError.CyclicVariable));
            Assert.That(result.Find("--a").InvalidAtComputedValue, Is.True);
            Assert.That(result.CustomProperties.Get("--a"), Is.Null);
            Assert.That(result.Diagnostics.Count(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidCss), Is.GreaterThanOrEqualTo(3));
        });
    }

    [Test]
    public void VariableBackedShorthandsExpandAfterSubstitution()
    {
        string css = "rect { --font-spec:italic 700 12px serif; --mark:url(#tip); " +
            "font:var(--font-spec); marker:var(--mark); }";
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { Sheet(css) });

        Assert.Multiple(() =>
        {
            Assert.That(result.Find("font-style").ValueText, Is.EqualTo("italic"));
            Assert.That(result.Find("font-weight").ValueText, Is.EqualTo("700"));
            Assert.That(result.Find("font-size").ValueText, Is.EqualTo("12px"));
            Assert.That(result.Find("font-family").ValueText, Is.EqualTo("serif"));
            Assert.That(result.Find("marker-start").ValueText, Is.EqualTo("url(#tip)"));
            Assert.That(result.Find("marker-mid").ValueText, Is.EqualTo("url(#tip)"));
            Assert.That(result.Find("marker-end").ValueText, Is.EqualTo("url(#tip)"));
            Assert.That(result.Find("font-size").Dependencies, Is.EqualTo(new[] { "--font-spec" }));
        });
    }

    [Test]
    public void VariableExpansionLimitsFailTheCascadeWithBoundedDiagnostics()
    {
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];
        SvgCascadeOptions options = new SvgCascadeOptions();
        options.Limits.MaxVariableExpansionTokens = 2;

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(rect, new List<SvgStyleSheet> { Sheet("rect { --many:1 2 3; fill:var(--many); }") }, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Find("--many").VariableError, Is.EqualTo(SvgCssVariableError.TokenLimitExceeded));
            Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
        });
    }

    static SvgStyleSheet Sheet(string css)
    {
        return new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits()).StyleSheet;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
