namespace svg.Tests;

public class SvgStructuralResolverTests
{
    [Test]
    public void GroupPreservesAllChildrenButTraversesOnlyRenderingContent()
    {
        SvgNode group = Load("<svg xmlns='http://www.w3.org/2000/svg'><g transform='translate(2 3)'><title>Primary <tspan>title</tspan></title><desc>A description</desc><metadata><item xmlns='urn:test'>data</item></metadata><rect/><defs><circle/></defs><symbol><circle/></symbol><view/><text>visible</text></g></svg>").DocumentElement.Children[0];

        SvgStructuralResult result = new SvgStructuralResolver().ResolveWithNode(group);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(SvgStructuralElementKind.Group));
            Assert.That(result.RenderMode, Is.EqualTo(SvgElementRenderMode.Container));
            Assert.That(result.Rendered, Is.True);
            Assert.That(result.RendersChildren, Is.True);
            Assert.That(result.ElementChildren, Has.Count.EqualTo(8));
            Assert.That(result.RenderingChildren.Select(node => node.ElementKind), Is.EqualTo(new[] { SvgElementKind.Rect, SvgElementKind.Text }));
            Assert.That(result.TitleElements, Has.Count.EqualTo(1));
            Assert.That(result.DescriptionElements, Has.Count.EqualTo(1));
            Assert.That(result.MetadataElements, Has.Count.EqualTo(1));
            Assert.That(result.TitleText, Is.EqualTo("Primary title"));
            Assert.That(result.DescriptionText, Is.EqualTo("A description"));
            Assert.That(result.Transform.ToText(), Is.EqualTo("translate(2 3)"));
        });
    }

    [Test]
    public void DefinitionsViewsAndDescriptiveElementsNeverRender()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><rect/><title>definitions</title></defs><view viewBox='1 2 30 40' preserveAspectRatio='none'><rect/></view><title>Hello <tspan>world</tspan></title><desc>details</desc><metadata><data xmlns='urn:test'>value</data></metadata></svg>");
        SvgStructuralResolveOptions referenced = new SvgStructuralResolveOptions();
        referenced.Referenced = true;

        SvgStructuralResult definitions = new SvgStructuralResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], referenced);
        SvgStructuralResult view = new SvgStructuralResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], referenced);
        SvgStructuralResult title = new SvgStructuralResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], referenced);
        SvgStructuralResult description = new SvgStructuralResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[3], referenced);
        SvgStructuralResult metadata = new SvgStructuralResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[4], referenced);

        Assert.Multiple(() =>
        {
            Assert.That(definitions.DefinitionContainer, Is.True);
            Assert.That(definitions.Rendered, Is.False);
            Assert.That(definitions.RendersChildren, Is.False);
            Assert.That(definitions.ElementChildren, Has.Count.EqualTo(2));
            Assert.That(definitions.RenderingChildren, Is.Empty);
            Assert.That(view.Rendered, Is.False);
            Assert.That(view.RendersChildren, Is.False);
            Assert.That(view.ViewBox.ToText(), Is.EqualTo("1 2 30 40"));
            Assert.That(view.AspectRatio.Alignment, Is.EqualTo(SvgAspectRatioAlignment.None));
            Assert.That(title.Descriptive, Is.True);
            Assert.That(title.TextContent, Is.EqualTo("Hello world"));
            Assert.That(description.TextContent, Is.EqualTo("details"));
            Assert.That(metadata.TextContent, Is.EqualTo("value"));
            Assert.That(new[] { definitions, view, title, description, metadata }.All(item => item.Success), Is.True);
        });
    }

    [Test]
    public void SymbolRendersOnlyWhenReferencedAndRetainsViewportInputs()
    {
        SvgNode symbol = Load("<svg xmlns='http://www.w3.org/2000/svg'><symbol viewBox='0 0 20 10' preserveAspectRatio='xMaxYMin slice' refX='25%' refY='2'><title>icon</title><rect/><defs><circle/></defs></symbol></svg>").DocumentElement.Children[0];
        SvgStructuralResult ordinary = new SvgStructuralResolver().ResolveWithNode(symbol);
        SvgStructuralResolveOptions options = new SvgStructuralResolveOptions();
        options.Referenced = true;
        SvgStructuralResult referenced = new SvgStructuralResolver().ResolveWithNodeAndOptions(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(ordinary.ReferenceOnly, Is.True);
            Assert.That(ordinary.Rendered, Is.False);
            Assert.That(ordinary.RendersChildren, Is.False);
            Assert.That(ordinary.RenderingChildren, Is.Empty);
            Assert.That(referenced.Success, Is.True);
            Assert.That(referenced.Rendered, Is.True);
            Assert.That(referenced.RendersChildren, Is.True);
            Assert.That(referenced.RenderingChildren.Select(node => node.ElementKind), Is.EqualTo(new[] { SvgElementKind.Rect }));
            Assert.That(referenced.ViewBox.ToText(), Is.EqualTo("0 0 20 10"));
            Assert.That(referenced.AspectRatio.Alignment, Is.EqualTo(SvgAspectRatioAlignment.XMaxYMin));
            Assert.That(referenced.AspectRatio.Scale, Is.EqualTo(SvgAspectRatioScale.Slice));
            Assert.That(referenced.RefX.ToText(), Is.EqualTo("25%"));
            Assert.That(referenced.RefY.ToText(), Is.EqualTo("2"));
            Assert.That(referenced.TitleText, Is.EqualTo("icon"));
        });
    }

    [Test]
    public void LinkRetainsBothReferencesAndParsesNavigationMetadata()
    {
        SvgNode link = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><a href='#new' xlink:href='#old' target='_blank' download='' hreflang='EN-us' type='image/svg+xml; charset=UTF-8' rel='NoOpener external'><title>Go</title><rect/></a></svg>").DocumentElement.Children[0];

        SvgStructuralResult result = new SvgStructuralResolver().ResolveWithNode(link);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(SvgStructuralElementKind.Link));
            Assert.That(result.Rendered, Is.True);
            Assert.That(result.RenderingChildren.Select(node => node.ElementKind), Is.EqualTo(new[] { SvgElementKind.Rect }));
            Assert.That(result.Href.Value, Is.EqualTo("#new"));
            Assert.That(result.XLinkHref.Value, Is.EqualTo("#old"));
            Assert.That(result.HrefText, Is.EqualTo("#new"));
            Assert.That(result.XLinkHrefText, Is.EqualTo("#old"));
            Assert.That(result.Target, Is.EqualTo("_blank"));
            Assert.That(result.HasDownload, Is.True);
            Assert.That(result.Download, Is.Empty);
            Assert.That(result.HrefLanguage, Is.EqualTo("en-us"));
            Assert.That(result.MediaType.ToText(), Is.EqualTo("image/svg+xml; charset=UTF-8"));
            Assert.That(result.Relations.Select(item => item.Value), Is.EqualTo(new[] { "noopener", "external" }));
            Assert.That(result.Relations.Select(item => item.Source), Is.EqualTo(new[] { "NoOpener", "external" }));
            Assert.That(result.TitleText, Is.EqualTo("Go"));
        });
    }

    [Test]
    public void DisplayAndAncestorStateSuppressContainerTraversal()
    {
        SvgNode hiddenNode = Load("<svg xmlns='http://www.w3.org/2000/svg'><g style='display:none'><rect/></g></svg>").DocumentElement.Children[0];
        SvgComputedStyle style = new SvgStyleResolver().Resolve(hiddenNode, new List<SvgStyleSheet>());
        SvgStructuralResolveOptions hiddenOptions = new SvgStructuralResolveOptions();
        hiddenOptions.ComputedStyle = style;
        SvgStructuralResult hidden = new SvgStructuralResolver().ResolveWithNodeAndOptions(hiddenNode, hiddenOptions);
        SvgNode ancestorNode = Load("<svg xmlns='http://www.w3.org/2000/svg'><a><circle/></a></svg>").DocumentElement.Children[0];
        SvgStructuralResolveOptions ancestorOptions = new SvgStructuralResolveOptions();
        ancestorOptions.AncestorRendered = false;
        SvgStructuralResult ancestorSuppressed = new SvgStructuralResolver().ResolveWithNodeAndOptions(ancestorNode, ancestorOptions);

        Assert.Multiple(() =>
        {
            Assert.That(style.Success, Is.True);
            Assert.That(hidden.Success, Is.True);
            Assert.That(hidden.Rendered, Is.False);
            Assert.That(hidden.RenderingChildren, Is.Empty);
            Assert.That(ancestorSuppressed.Success, Is.True);
            Assert.That(ancestorSuppressed.Rendered, Is.False);
            Assert.That(ancestorSuppressed.RenderingChildren, Is.Empty);
            Assert.That(ancestorSuppressed.Target, Is.EqualTo("_self"));
        });
    }

    [Test]
    public void InvalidLinkMetadataAndTextLimitsProduceLocatedFailures()
    {
        SvgNode link = Load("<svg xmlns='http://www.w3.org/2000/svg'><a hreflang='en_us' type='not-a-type' rel='one two'><rect/></a></svg>").DocumentElement.Children[0];
        SvgStructuralResolveOptions linkOptions = new SvgStructuralResolveOptions();
        linkOptions.MaxRelationTokens = 1;
        SvgStructuralResult invalidLink = new SvgStructuralResolver().ResolveWithNodeAndOptions(link, linkOptions);
        SvgNode title = Load("<svg xmlns='http://www.w3.org/2000/svg'><title>abcdef</title></svg>").DocumentElement.Children[0];
        SvgStructuralResolveOptions textOptions = new SvgStructuralResolveOptions();
        textOptions.TreeLimits.MaxTextCharacters = 3;
        SvgStructuralResult limitedText = new SvgStructuralResolver().ResolveWithNodeAndOptions(title, textOptions);

        Assert.Multiple(() =>
        {
            Assert.That(invalidLink.Success, Is.False);
            Assert.That(invalidLink.Diagnostics.Count(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidValue), Is.EqualTo(2));
            Assert.That(invalidLink.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
            Assert.That(invalidLink.Diagnostics.All(diagnostic => diagnostic.Source != null), Is.True);
            Assert.That(limitedText.Success, Is.False);
            Assert.That(limitedText.TextContent, Is.EqualTo("abc"));
            Assert.That(limitedText.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(limitedText.Diagnostics.Single().Source, Is.SameAs(title.SourceSpan));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
