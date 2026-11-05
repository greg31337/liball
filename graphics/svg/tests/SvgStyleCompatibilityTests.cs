namespace svg.Tests;

public class SvgStyleCompatibilityTests
{
    [Test]
    public void BuiltInUserAgentDefaultsApplyWithSvgCascadePrecedence()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><svg/><image/><defs style='display:inline !important'/><a/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode nested = root.Children[0];
        SvgNode image = root.Children[1];
        SvgNode defs = root.Children[2];
        SvgNode link = root.Children[3];
        SvgCascadeOptions linkOptions = new SvgCascadeOptions();
        SvgDynamicSelectorState dynamicState = new SvgDynamicSelectorState(new SvgDynamicSelectorStateOptions());
        dynamicState.LinkState = node => node == link ? SvgSelectorLinkState.Unvisited : SvgSelectorLinkState.None;
        linkOptions.SelectorOptions.DynamicState = dynamicState;

        SvgCascadeResult rootStyle = new SvgCascade().ResolveWithNodeAndStyleSheets(root, new List<SvgStyleSheet>());
        SvgCascadeResult nestedStyle = new SvgCascade().ResolveWithNodeAndStyleSheets(nested, new List<SvgStyleSheet>());
        SvgCascadeResult imageStyle = new SvgCascade().ResolveWithNodeAndStyleSheets(image, new List<SvgStyleSheet>());
        SvgCascadeResult defsStyle = new SvgCascade().ResolveWithNodeAndStyleSheets(defs, new List<SvgStyleSheet>());
        SvgCascadeResult linkStyle = new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(link, new List<SvgStyleSheet>(), linkOptions);
        SvgCascadeOptions disabled = new SvgCascadeOptions { IncludeUserAgentDefaults = false };
        SvgCascadeResult disabledDefs = new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(defs, new List<SvgStyleSheet>(), disabled);

        Assert.Multiple(() =>
        {
            Assert.That(rootStyle.Find("overflow-x").ValueText, Is.EqualTo("visible"));
            Assert.That(nestedStyle.Find("overflow-x").ValueText, Is.EqualTo("hidden"));
            Assert.That(imageStyle.Find("overflow-y").ValueText, Is.EqualTo("hidden"));
            Assert.That(nestedStyle.Find("overflow-x").Winner.SourceKind, Is.EqualTo(SvgCascadeSourceKind.UserAgentDefault));
            Assert.That(defsStyle.Find("display").ValueText, Is.EqualTo("none"));
            Assert.That(defsStyle.Find("display").Winner.Origin, Is.EqualTo(SvgStyleOrigin.UserAgent));
            Assert.That(defsStyle.Find("display").Winner.Important, Is.True);
            Assert.That(linkStyle.Find("cursor").ValueText, Is.EqualTo("pointer"));
            Assert.That(disabledDefs.Find("display").ValueText, Is.EqualTo("inline"));
        });
    }

    [Test]
    public void CurrentColorResolvesAgainstInheritedColorAndContextPaintRemainsDeferred()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g style='color:rgb(25% 50% 75%)'><rect style='color:currentColor; fill:url(#paint) currentColor; stroke:currentColor; flood-color:currentColor'/><circle style='fill:context-fill; stroke:context-stroke'/></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode rect = group.Children[0];
        SvgNode circle = group.Children[1];
        SvgComputedStyle parent = new SvgComputedStyleConverter().ConvertWithNodeAndCascade(group, new SvgCascade().ResolveWithNodeAndStyleSheets(group, new List<SvgStyleSheet>()));
        SvgComputedStyleOptions childOptions = new SvgComputedStyleOptions { Parent = parent };

        SvgComputedStyle rectStyle = new SvgComputedStyleConverter().ConvertWithNodeAndCascadeAndOptions(rect, new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet>()), childOptions);
        SvgComputedStyle circleStyle = new SvgComputedStyleConverter().ConvertWithNodeAndCascadeAndOptions(circle, new SvgCascade().ResolveWithNodeAndStyleSheets(circle, new List<SvgStyleSheet>()), childOptions);

        Assert.Multiple(() =>
        {
            Assert.That(rectStyle.GetColor("color").IsCurrentColor, Is.False);
            Assert.That(rectStyle.GetColor("color").ToText(), Is.EqualTo(parent.GetColor("color").ToText()));
            Assert.That(rectStyle.GetPaint("stroke").Color.ToText(), Is.EqualTo(parent.GetColor("color").ToText()));
            Assert.That(rectStyle.GetColor("flood-color").ToText(), Is.EqualTo(parent.GetColor("color").ToText()));
            Assert.That(rectStyle.GetPaint("fill").Kind, Is.EqualTo(SvgPaintKind.Url));
            Assert.That(rectStyle.GetPaint("fill").Fallbacks.Single().Color.ToText(), Is.EqualTo(parent.GetColor("color").ToText()));
            Assert.That(circleStyle.GetPaint("fill").Kind, Is.EqualTo(SvgPaintKind.ContextFill));
            Assert.That(circleStyle.GetPaint("stroke").Kind, Is.EqualTo(SvgPaintKind.ContextStroke));
            Assert.That(circleStyle.GetPaint("fill").ToText(), Is.EqualTo("context-fill"));
        });
    }

    [Test]
    public void ComputedParentSuppliesSvgInheritanceWithoutAParentCascade()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g style='fill:orange; visibility:hidden; stroke-width:3px; opacity:.4; clip-path:url(#clip)'><rect style='fill:inherit; visibility:unset'/></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode rect = group.Children[0];
        SvgComputedStyle parent = new SvgComputedStyleConverter().ConvertWithNodeAndCascade(group, new SvgCascade().ResolveWithNodeAndStyleSheets(group, new List<SvgStyleSheet>()));
        SvgComputedStyleOptions options = new SvgComputedStyleOptions { Parent = parent };

        SvgComputedStyle child = new SvgComputedStyleConverter().ConvertWithNodeAndCascadeAndOptions(rect, new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet>()), options);

        Assert.Multiple(() =>
        {
            Assert.That(child.GetPaint("fill").ToText(), Is.EqualTo(parent.GetPaint("fill").ToText()));
            Assert.That(child.GetKeyword("visibility"), Is.EqualTo("hidden"));
            Assert.That(child.GetLength("stroke-width").ToText(), Is.EqualTo("3px"));
            Assert.That(child.GetNumber("opacity").Value, Is.EqualTo(1));
            Assert.That(child.GetKeyword("clip-path"), Is.EqualTo("none"));
        });
    }

    [Test]
    public void PresentationAttributesAcceptSvgUnitsAndInvalidValuesOverrideUaDefaultsWithInitials()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect x='12' y='-3.5' width='20' stroke-width='2' font-size='11' transform-origin='10 20' glyph-orientation-horizontal='45' kerning='3'/><image overflow='bogus'/></svg>");
        SvgNode rect = document.DocumentElement.Children[0];
        SvgNode image = document.DocumentElement.Children[1];
        SvgCascadeResult rectCascade = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet>());
        SvgComputedStyle rectStyle = new SvgComputedStyleConverter().ConvertWithNodeAndCascade(rect, rectCascade);
        SvgCascadeResult imageCascade = new SvgCascade().ResolveWithNodeAndStyleSheets(image, new List<SvgStyleSheet>());

        Assert.Multiple(() =>
        {
            Assert.That(rectStyle.GetLength("x").Unit, Is.EqualTo(SvgLengthUnit.Number));
            Assert.That(rectStyle.GetLength("x").Value, Is.EqualTo(12));
            Assert.That(rectStyle.GetLength("y").Value, Is.EqualTo(-3.5));
            Assert.That(rectStyle.GetLength("width").Unit, Is.EqualTo(SvgLengthUnit.Number));
            Assert.That(rectStyle.GetLength("stroke-width").Value, Is.EqualTo(2));
            Assert.That(rectStyle.GetLength("font-size").Value, Is.EqualTo(11));
            Assert.That(rectStyle.Find("transform-origin").TransformOrigin.X.ToText(), Is.EqualTo("10px"));
            Assert.That(rectStyle.Find("glyph-orientation-horizontal").Angle.Unit, Is.EqualTo(SvgAngleUnit.Number));
            Assert.That(rectStyle.Find("kerning").IsLegacy, Is.True);
            Assert.That(rectCascade.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.PreservedFeature), Is.True);
            Assert.That(imageCascade.Find("overflow-x").ValueText, Is.EqualTo("visible"));
            Assert.That(imageCascade.Find("overflow-x").Winner.SourceKind, Is.EqualTo(SvgCascadeSourceKind.PresentationAttribute));
            Assert.That(imageCascade.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.CompatibilityRecovery), Is.True);
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
