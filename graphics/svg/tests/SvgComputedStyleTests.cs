namespace svg.Tests;

public class SvgComputedStyleTests
{
    [Test]
    public void ConvertsRepresentativePropertyFamiliesToTypedValues()
    {
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];
        SvgStyleSheet sheet = Sheet(
            "rect {" +
            "display:block; visibility:hidden; opacity:1.5; color:rgb(255 0 0 / 50%);" +
            "fill:url(#gradient) red; stroke:none; stroke-width:2px; stroke-dasharray:1px, 2%;" +
            "marker-start:url(#tip); paint-order:stroke; clip-path:url(#clip); mask:url(#mask); filter:blur(2px);" +
            "mix-blend-mode:multiply; shape-rendering:crispEdges;" +
            "transform:translate(10px, 20%) rotate(45deg); transform-origin:left top; transform-box:fill-box;" +
            "font-family:\"Open Sans\", serif; font-size:12px; glyph-orientation-horizontal:90deg; text-anchor:middle;" +
            "pointer-events:visibleStroke; cursor:url(cursor.cur) 4 5, pointer;" +
            "cx:25%; width:40px; d:path(\"M0 0L1 1\");" +
            "}");

        SvgCascadeResult cascade = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { sheet });
        SvgComputedStyle style = new SvgComputedStyleConverter().ConvertWithNodeAndCascade(rect, cascade);

        Assert.Multiple(() =>
        {
            Assert.That(style.Success, Is.True);
            Assert.That(style.GetKeyword("display"), Is.EqualTo("block"));
            Assert.That(style.GetKeyword("visibility"), Is.EqualTo("hidden"));
            Assert.That(style.GetNumber("opacity").Success, Is.True);
            Assert.That(style.GetNumber("opacity").Value, Is.EqualTo(1));
            Assert.That(style.GetColor("color").Alpha, Is.EqualTo(0.5));
            Assert.That(style.GetPaint("fill").Kind, Is.EqualTo(SvgPaintKind.Url));
            Assert.That(style.GetPaint("fill").Url, Is.EqualTo("#gradient"));
            Assert.That(style.GetPaint("stroke").Kind, Is.EqualTo(SvgPaintKind.None));
            Assert.That(style.GetLength("stroke-width").ToText(), Is.EqualTo("2px"));
            Assert.That(style.Find("stroke-dasharray").LengthList.Items.Count, Is.EqualTo(2));
            Assert.That(style.Find("marker-start").Reference.Fragment, Is.EqualTo("tip"));
            Assert.That(style.Find("paint-order").PaintOrder, Is.EqualTo(new[] { SvgPaintOrderComponent.Stroke, SvgPaintOrderComponent.Fill, SvgPaintOrderComponent.Markers }));
            Assert.That(style.Find("clip-path").Reference.Fragment, Is.EqualTo("clip"));
            Assert.That(style.Find("mask").Kind, Is.EqualTo(SvgComputedValueKind.Text));
            Assert.That(style.Find("filter").CssText, Is.EqualTo("blur(2px)"));
            Assert.That(style.GetKeyword("mix-blend-mode"), Is.EqualTo("multiply"));
            Assert.That(style.GetKeyword("shape-rendering"), Is.EqualTo("crispedges"));
            Assert.That(style.GetTransformList().Items.Count, Is.EqualTo(2));
            Assert.That(style.Find("transform-origin").TransformOrigin.X.ToText(), Is.EqualTo("0%"));
            Assert.That(style.Find("transform-box").TransformBox, Is.EqualTo(SvgTransformBox.FillBox));
            Assert.That(style.Find("font-family").FontFamilies.Select(family => family.Name), Is.EqualTo(new[] { "Open Sans", "serif" }));
            Assert.That(style.GetLength("font-size").ToText(), Is.EqualTo("12px"));
            Assert.That(style.Find("glyph-orientation-horizontal").Angle.ToText(), Is.EqualTo("90deg"));
            Assert.That(style.GetKeyword("text-anchor"), Is.EqualTo("middle"));
            Assert.That(style.GetKeyword("pointer-events"), Is.EqualTo("visiblestroke"));
            Assert.That(style.Find("cursor").Cursor.Urls[0].Value, Is.EqualTo("cursor.cur"));
            Assert.That(style.Find("cursor").Cursor.Keyword, Is.EqualTo("pointer"));
            Assert.That(style.GetLength("cx").ToText(), Is.EqualTo("25%"));
            Assert.That(style.GetLength("width").ToText(), Is.EqualTo("40px"));
            Assert.That(style.Find("d").CssText, Is.EqualTo("path(\"M0 0L1 1\")"));
        });
    }

    [Test]
    public void ProducesEveryLonghandInCatalogOrderWithTypedDefaults()
    {
        SvgNode circle = Load("<svg xmlns='http://www.w3.org/2000/svg'><circle/></svg>").DocumentElement.Children[0];
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();
        SvgCascadeResult cascade = new SvgCascade().ResolveWithNodeAndStyleSheets(circle, new List<SvgStyleSheet>());

        SvgComputedStyle style = new SvgComputedStyleConverter().ConvertWithNodeAndCascade(circle, cascade);
        List<SvgPropertySupportEntry> longhands = catalog.Entries.Where(entry => !entry.IsShorthand).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(style.Success, Is.True);
            Assert.That(style.Values.Count, Is.EqualTo(longhands.Count));
            Assert.That(style.Values.Select(value => value.PropertyName), Is.EqualTo(longhands.Select(entry => entry.Name)));
            Assert.That(style.Values.All(value => value.CssText.Length != 0), Is.True);
            Assert.That(style.Find("fill").Kind, Is.EqualTo(SvgComputedValueKind.Paint));
            Assert.That(style.Find("color").Kind, Is.EqualTo(SvgComputedValueKind.Color));
            Assert.That(style.Find("opacity").Kind, Is.EqualTo(SvgComputedValueKind.Number));
            Assert.That(style.Find("transform").Kind, Is.EqualTo(SvgComputedValueKind.TransformList));
            Assert.That(style.Find("font-family").Kind, Is.EqualTo(SvgComputedValueKind.FontFamilyList));
            Assert.That(style.Find("cursor").Kind, Is.EqualTo(SvgComputedValueKind.Cursor));
        });
    }

    [Test]
    public void ConvertsLegacyUnitlessSvgTransformsWithoutLosingCssTransforms()
    {
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];
        SvgStyleSheet sheet = Sheet("rect { transform:translate(10 20) rotate(30 5 6); }");

        SvgComputedStyle style = new SvgComputedStyleConverter().ConvertWithNodeAndCascade(rect, new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { sheet }));

        Assert.Multiple(() =>
        {
            Assert.That(style.Success, Is.True);
            Assert.That(style.GetTransformList().Items.Count, Is.EqualTo(2));
            Assert.That(style.GetTransformList().Items[0].Kind, Is.EqualTo(SvgCssTransformKind.Translate));
            Assert.That(style.GetTransformList().Items[0].Lengths[0].ToText(), Is.EqualTo("10px"));
            Assert.That(style.GetTransformList().Items[1].Kind, Is.EqualTo(SvgCssTransformKind.Matrix));
        });
    }

    [Test]
    public void InvalidComputedGrammarRecoversToInheritedOrInitialValue()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode rect = group.Children[0];
        SvgStyleSheet sheet = Sheet("g { font-family:monospace; } rect { font-family:serif,,sans-serif; paint-order:fill fill; }");
        SvgCascadeResult parentCascade = new SvgCascade().ResolveWithNodeAndStyleSheets(group, new List<SvgStyleSheet> { sheet });
        SvgComputedStyle parentStyle = new SvgComputedStyleConverter().ConvertWithNodeAndCascade(group, parentCascade);
        SvgCascadeOptions cascadeOptions = new SvgCascadeOptions { Parent = parentCascade };
        SvgCascadeResult childCascade = new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(rect, new List<SvgStyleSheet> { sheet }, cascadeOptions);
        SvgComputedStyleOptions computedOptions = new SvgComputedStyleOptions { Parent = parentStyle };

        SvgComputedStyle childStyle = new SvgComputedStyleConverter().ConvertWithNodeAndCascadeAndOptions(rect, childCascade, computedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(childStyle.Success, Is.True);
            Assert.That(childStyle.Find("font-family").Recovered, Is.True);
            Assert.That(childStyle.Find("font-family").FontFamilies.Single().Name, Is.EqualTo("monospace"));
            Assert.That(childStyle.Find("paint-order").Recovered, Is.True);
            Assert.That(childStyle.Find("paint-order").CssText, Is.EqualTo("fill stroke markers"));
            Assert.That(childStyle.Diagnostics.Count(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidCss), Is.EqualTo(2));
        });
    }

    [Test]
    public void RetainsCustomPropertySnapshotsDependenciesAndWinnerSource()
    {
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];
        SvgStyleSheet sheet = Sheet("rect { --brand: rgb(0 128 0); fill:var(--brand); }");
        SvgCascadeResult cascade = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { sheet });

        SvgComputedStyle style = new SvgComputedStyleConverter().ConvertWithNodeAndCascade(rect, cascade);

        Assert.Multiple(() =>
        {
            Assert.That(style.CustomProperties, Is.Not.SameAs(cascade.CustomProperties));
            Assert.That(style.CustomProperties.Get("--brand").RawText.Trim(), Is.EqualTo("rgb(0 128 0)"));
            Assert.That(style.Find("fill").Dependencies, Is.EqualTo(new[] { "--brand" }));
            Assert.That(style.Find("fill").Source, Is.SameAs(cascade.Find("fill").Winner.Source));
            Assert.That(style.SourceVersion, Is.EqualTo(rect.Version));
        });
    }

    [Test]
    public void ConversionRejectsInvalidCrossNodeAndStaleCascadeInputsWithDiagnostics()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/><circle/></svg>");
        SvgNode rect = document.DocumentElement.Children[0];
        SvgNode circle = document.DocumentElement.Children[1];
        SvgCascade cascadeService = new SvgCascade();
        SvgComputedStyleConverter converter = new SvgComputedStyleConverter();
        SvgComputedStyleOptions options = new SvgComputedStyleOptions();
        SvgCascadeResult rectCascade = cascadeService.ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet>());

        SvgComputedStyle nullNode = converter.ConvertWithNodeAndCascadeAndOptions(null, rectCascade, options);
        SvgComputedStyle nullCascade = converter.ConvertWithNodeAndCascadeAndOptions(rect, null, options);
        SvgComputedStyle nullOptions = converter.ConvertWithNodeAndCascadeAndOptions(rect, rectCascade, null);
        SvgComputedStyle crossNode = converter.ConvertWithNodeAndCascadeAndOptions(circle, rectCascade, options);

        Assert.That(new SvgQuery().GetByQualifiedName(document, "rect"), Is.EqualTo(new[] { rect }));
        Assert.That(document.Index.IndexedVersion, Is.EqualTo(document.Version));
        SvgAttribute classes = new SvgDocumentApi().CreateAttribute("class", "", "class", "", "changed");
        SvgMutationResult mutation = new SvgNodeApi().AppendAttribute(rect, classes);
        SvgComputedStyle stale = converter.ConvertWithNodeAndCascadeAndOptions(rect, rectCascade, options);
        SvgCascadeResult refreshedCascade = cascadeService.ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet>());
        SvgComputedStyle refreshed = converter.ConvertWithNodeAndCascadeAndOptions(rect, refreshedCascade, options);

        Assert.Multiple(() =>
        {
            Assert.That(rectCascade.SourceNode, Is.SameAs(rect));
            Assert.That(rectCascade.SourceVersion, Is.LessThan(rect.Version));
            Assert.That(nullNode.Success, Is.False);
            Assert.That(nullCascade.Success, Is.False);
            Assert.That(nullOptions.Success, Is.False);
            Assert.That(crossNode.Success, Is.False);
            Assert.That(stale.Success, Is.False);
            Assert.That(new[] { nullNode, nullCascade, nullOptions, crossNode, stale }, Has.All.Matches<SvgComputedStyle>(style => style.Diagnostics.Single().Code == SvgDiagnosticCode.InvalidArgument));
            Assert.That(mutation.Success, Is.True);
            Assert.That(document.Index.IndexedVersion, Is.EqualTo(-1));
            Assert.That(refreshedCascade.SourceNode, Is.SameAs(rect));
            Assert.That(refreshedCascade.SourceVersion, Is.EqualTo(rect.Version));
            Assert.That(refreshed.Success, Is.True);
            Assert.That(refreshed.SourceVersion, Is.EqualTo(rect.Version));
        });
    }

    static SvgStyleSheet Sheet(string css)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, new SvgCssLimits(), SvgStyleOrigin.Author, new SvgSourceUri("computed.css"));
        Assert.That(parsed.Success, Is.True);
        return parsed.StyleSheet;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
