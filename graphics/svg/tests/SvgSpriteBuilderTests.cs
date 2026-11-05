namespace svg.Tests;

public class SvgSpriteBuilderTests
{
    [Test]
    public void BuildsSafelyOptimizedNamedSymbolsWithCompleteIsolationAndManifestMappings()
    {
        SvgDocument source = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 10' preserveAspectRatio='xMinYMin meet' aria-label='Painted icon'><title>Source title</title><desc>Source description</desc><!--remove--><defs><linearGradient id='paint'><stop offset='0' stop-color='red'/></linearGradient></defs><style>.tone,#shape{fill:url(#paint)}</style><rect id='shape' class='tone' width='20' height='10' fill='url(#paint)'/></svg>");
        long sourceVersion = source.Version;
        SvgSpriteBuilder builder = new SvgSpriteBuilder();

        SvgSpriteBuilder returned = builder.AddNamedDocument(source, "icon").AddNamed(source, "icon");
        SvgSpriteBuildResult result = builder.Build();
        List<SvgNode> symbols = result.Document.DocumentElement.Children.Single(item => item.ElementKind == SvgElementKind.Defs).Children.Where(item => item.ElementKind == SvgElementKind.Symbol).ToList();
        SvgNode first = symbols[0];
        SvgNode style = Descendants(first).Single(item => item.ElementKind == SvgElementKind.Style);

        Assert.Multiple(() =>
        {
            Assert.That(returned, Is.SameAs(builder));
            Assert.That(result.Success, Is.True, result.Message + Diagnostics(result.Diagnostics));
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
            Assert.That(symbols.Select(item => Attribute(item, "id")), Is.EqualTo(new[] { "icon", "icon-2" }));
            Assert.That(Attribute(first, "viewBox"), Is.EqualTo("0 0 20 10"));
            Assert.That(Attribute(first, "preserveAspectRatio"), Is.EqualTo("xMinYMin meet"));
            Assert.That(Attribute(first, "aria-label"), Is.EqualTo("Painted icon"));
            Assert.That(Attribute(first, "role"), Is.EqualTo("img"));
            Assert.That(Descendants(first).Single(item => item.ElementKind == SvgElementKind.Title).Children.Single().Value, Is.EqualTo("Source title"));
            Assert.That(Descendants(first).Single(item => item.ElementKind == SvgElementKind.Desc).Children.Single().Value, Is.EqualTo("Source description"));
            Assert.That(Descendants(first).Single(item => Attribute(item, "id") == "icon-shape"), Is.Not.Null);
            Assert.That(Descendants(first).Single(item => Attribute(item, "id") == "icon-paint"), Is.Not.Null);
            Assert.That(new SvgNodeApi().GetTextContent(style), Does.Contain("#icon .tone").And.Contain("#icon #icon-shape").And.Contain("url(#icon-paint)"));
            Assert.That(new SvgNodeApi().GetTextContent(Descendants(symbols[1]).Single(item => item.ElementKind == SvgElementKind.Style)), Does.Contain("#icon-2 .tone").And.Contain("#icon-2 #icon-2-shape"));
            Assert.That(Descendants(first).Single(item => Attribute(item, "id") == "icon-shape").Attributes.Single(item => item.LocalName == "fill").Value, Is.EqualTo("url(#icon-paint)"));
            Assert.That(Descendants(first).Any(item => item.Kind == SvgNodeKind.Comment), Is.False);
            Assert.That(result.Manifest, Has.Count.EqualTo(2));
            Assert.That(result.Manifest[0].SymbolIdentifier, Is.EqualTo("icon"));
            Assert.That(result.Manifest[0].ViewBox, Is.EqualTo("0 0 20 10"));
            Assert.That(result.Manifest[0].SafelyOptimized, Is.True);
            Assert.That(result.Manifest[0].IdentifierMappings.Any(item => item.SourceIdentifier == "paint" && item.SpriteIdentifier == "icon-paint"), Is.True);
            Assert.That(result.Manifest[0].IdentifierMappings.Any(item => item.SourceIdentifier == "shape" && item.SpriteIdentifier == "icon-shape"), Is.True);
            Assert.That(source.Version, Is.EqualTo(sourceVersion));
            Assert.That(source.Index.GetById("shape"), Is.Not.Null);
            Assert.That(source.Index.GetById("icon-shape"), Is.Null);
        });
    }

    [Test]
    public void ExtractsFragmentsInfersDrawingViewBoxAndAppliesDecorativeAccessibility()
    {
        SvgDocument source = Load("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><rect id='drop' width='90' height='90'/><g id='target' transform='translate(5 6)'><rect x='2' y='3' width='10' height='20'/></g></svg>");
        SvgSpriteAccessibilityOptions accessibility = new SvgSpriteAccessibilityOptions();
        accessibility.Decorative = true;
        SvgSpriteBuilder builder = new SvgSpriteBuilder();

        SvgSpriteBuildResult result = builder.AddFragmentWithAccessibility(source, "#target", " target badge ", accessibility).Build();
        SvgNode symbol = result.Document.Index.GetById("target-badge");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message + Diagnostics(result.Diagnostics));
            Assert.That(symbol, Is.Not.Null);
            Assert.That(Attribute(symbol, "viewBox"), Is.EqualTo("7 9 10 20"));
            Assert.That(Attribute(symbol, "aria-hidden"), Is.EqualTo("true"));
            Assert.That(Attribute(symbol, "role"), Is.EqualTo("presentation"));
            Assert.That(Descendants(symbol).Any(item => item.ElementKind == SvgElementKind.Title || item.ElementKind == SvgElementKind.Desc), Is.False);
            Assert.That(Descendants(symbol).Any(item => Attribute(item, "id") == "target-badge-target"), Is.True);
            Assert.That(Descendants(symbol).Any(item => Attribute(item, "id").EndsWith("drop", StringComparison.Ordinal)), Is.False);
            Assert.That(result.Manifest.Single().RequestedName, Is.EqualTo(" target badge "));
            Assert.That(result.Manifest.Single().FragmentIdentifier, Is.EqualTo("target"));
            Assert.That(result.Manifest.Single().ViewBox, Is.EqualTo("7 9 10 20"));
            Assert.That(result.Manifest.Single().Decorative, Is.True);
            Assert.That(source.Index.GetById("drop"), Is.Not.Null);
        });
    }

    [Test]
    public void ReportsMissingFragmentsAndSecurityBoundsWithoutPublishingPartialOutput()
    {
        SvgDocument source = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='one'><rect/></g></svg>");
        SvgSpriteBuildResult missing = new SvgSpriteBuilder().AddNamedFragment(source, "missing", "icon").Build();
        SvgSpriteBuilderOptions limitedOptions = new SvgSpriteBuilderOptions();
        limitedOptions.MaximumNodes = 2;
        SvgSpriteBuilder limitedBuilder = SvgSpriteBuilder.Create(limitedOptions);
        SvgSpriteBuildResult limited = limitedBuilder.AddNamedDocument(source, "icon").Build();

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Error, Is.EqualTo(SvgSpriteBuilderError.FragmentNotFound));
            Assert.That(missing.Document, Is.Null);
            Assert.That(missing.Manifest, Is.Empty);
            Assert.That(missing.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InvalidReference), Is.True);
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Error, Is.EqualTo(SvgSpriteBuilderError.NodeLimitExceeded));
            Assert.That(limited.Document, Is.Null);
            Assert.That(limited.Manifest, Is.Empty);
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static List<SvgNode> Descendants(SvgNode root)
    {
        List<SvgNode> result = new List<SvgNode>();
        List<SvgNode> pending = new List<SvgNode> { root };
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            result.Add(node);
            pending.AddRange(node.Children);
        }
        return result;
    }

    static string Attribute(SvgNode node, string localName)
    {
        if (node == null) return "";
        SvgAttribute attribute = node.Attributes.SingleOrDefault(item => item.NamespaceUri.Length == 0 && item.LocalName == localName);
        return attribute == null ? "" : attribute.Value;
    }

    static string Diagnostics(List<SvgDiagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item => item.Message));
    }
}
