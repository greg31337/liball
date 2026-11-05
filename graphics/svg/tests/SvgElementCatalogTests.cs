namespace svg.Tests;

public class SvgElementCatalogTests
{
    [Test]
    public void CatalogCoversEveryCurrentElementExactlyOnce()
    {
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        List<SvgElementDefinition> entries = catalog.GetEntries();
        SvgElementKind[] currentKinds = Enum.GetValues<SvgElementKind>().Where(kind => new SvgNodeNames().IsSvg2Element(kind)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(currentKinds.Length));
            Assert.That(entries.Select(entry => entry.Kind), Is.EquivalentTo(currentKinds));
            Assert.That(entries.Select(entry => entry.Kind).Distinct().Count(), Is.EqualTo(entries.Count));
            Assert.That(entries.Select(entry => entry.LocalName).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(entries.Count));
            Assert.That(entries.All(entry => entry.Categories.Count > 0), Is.True);
            Assert.That(currentKinds.All(kind => catalog.Find(kind) != null), Is.True);
            Assert.That(entries.All(entry => ReferenceEquals(catalog.FindByName(SvgNamespaces.Svg, entry.LocalName), entry)), Is.True);
            Assert.That(catalog.Find(SvgElementKind.Unknown), Is.Null);
            Assert.That(catalog.Find(SvgElementKind.Font), Is.Null);
            Assert.That(catalog.FindByName("urn:vendor", "rect"), Is.Null);
            Assert.That(catalog.FindByName(SvgNamespaces.Svg, "Rect"), Is.Null);
        });
    }

    [Test]
    public void ContentModelsExpressGeneralTextFilterAndCardinalityRules()
    {
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        SvgElementContentModel circle = catalog.Find(SvgElementKind.Circle).ContentModel;
        SvgElementContentModel text = catalog.Find(SvgElementKind.Text).ContentModel;
        SvgElementContentModel filter = catalog.Find(SvgElementKind.Filter).ContentModel;
        SvgElementContentModel transfer = catalog.Find(SvgElementKind.FeComponentTransfer).ContentModel;
        SvgElementContentModel lighting = catalog.Find(SvgElementKind.FeDiffuseLighting).ContentModel;
        SvgElementContentModel merge = catalog.Find(SvgElementKind.FeMerge).ContentModel;
        SvgElementContentModel foreign = catalog.Find(SvgElementKind.ForeignObject).ContentModel;
        SvgElementContentModel anchor = catalog.Find(SvgElementKind.A).ContentModel;
        SvgElementContentModel description = catalog.Find(SvgElementKind.Desc).ContentModel;

        Assert.Multiple(() =>
        {
            Assert.That(circle.AllowsElement(SvgElementKind.Animate), Is.True);
            Assert.That(circle.AllowsElement(SvgElementKind.Title), Is.True);
            Assert.That(circle.AllowsElement(SvgElementKind.Rect), Is.False);
            Assert.That(circle.AllowsText, Is.False);
            Assert.That(text.AllowsText, Is.True);
            Assert.That(text.AllowsElement(SvgElementKind.TSpan), Is.True);
            Assert.That(text.AllowsElement(SvgElementKind.TextPath), Is.True);
            Assert.That(text.AllowsElement(SvgElementKind.Rect), Is.False);
            Assert.That(filter.AllowsElement(SvgElementKind.FeBlend), Is.True);
            Assert.That(filter.AllowsElement(SvgElementKind.Rect), Is.False);
            Assert.That(transfer.AllowsElementChildren(new List<SvgElementKind> { SvgElementKind.FeFuncR, SvgElementKind.FeFuncG, SvgElementKind.FeFuncB, SvgElementKind.FeFuncA }), Is.True);
            Assert.That(transfer.AllowsElementChildren(new List<SvgElementKind> { SvgElementKind.FeFuncR, SvgElementKind.FeFuncR }), Is.False);
            Assert.That(lighting.AllowsElementChildren(new List<SvgElementKind>()), Is.False);
            Assert.That(lighting.AllowsElementChildren(new List<SvgElementKind> { SvgElementKind.FeDistantLight }), Is.True);
            Assert.That(lighting.AllowsElementChildren(new List<SvgElementKind> { SvgElementKind.FeDistantLight, SvgElementKind.FePointLight }), Is.False);
            Assert.That(merge.AllowsElementChildren(new List<SvgElementKind>()), Is.False);
            Assert.That(merge.AllowsElementChildren(new List<SvgElementKind> { SvgElementKind.FeMergeNode, SvgElementKind.FeMergeNode }), Is.True);
            Assert.That(foreign.AllowsText, Is.True);
            Assert.That(foreign.AllowsForeignElements, Is.True);
            Assert.That(anchor.AllowsElement(SvgElementKind.A), Is.False);
            Assert.That(anchor.AllowsElement(SvgElementKind.Rect), Is.True);
            Assert.That(description.AllowsText, Is.True);
            Assert.That(description.AllowsForeignElements, Is.True);
            Assert.That(description.AllowsElement(SvgElementKind.Rect), Is.True);
            Assert.That(transfer.AllowsElementChildren(null), Is.False);
        });
    }

    [Test]
    public void AttributeDefinitionsCombineSpecificGlobalPresentationAndDependentDefaults()
    {
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        SvgElementDefinition rect = catalog.Find(SvgElementKind.Rect);
        SvgElementDefinition convolve = catalog.Find(SvgElementKind.FeConvolveMatrix);
        SvgElementDefinition radial = catalog.Find(SvgElementKind.RadialGradient);
        SvgElementDefinition svg = catalog.Find(SvgElementKind.Svg);
        SvgElementDefinition image = catalog.Find(SvgElementKind.Image);
        SvgElementDefinition path = catalog.Find(SvgElementKind.Path);

        Assert.Multiple(() =>
        {
            Assert.That(rect.FindAttribute("", "x").DefaultKind, Is.EqualTo(SvgAttributeDefaultKind.Literal));
            Assert.That(rect.FindAttribute("", "x").DefaultValue, Is.EqualTo("0"));
            Assert.That(rect.FindAttribute("", "width").DefaultValue, Is.EqualTo("auto"));
            Assert.That(rect.FindAttribute("", "rx").DefaultValue, Is.EqualTo("auto"));
            Assert.That(rect.FindAttribute("", "id").Global, Is.True);
            Assert.That(rect.FindAttribute(SvgNamespaces.Xml, "space").DefaultValue, Is.EqualTo("default"));
            Assert.That(rect.FindAttribute("", "aria-label").Global, Is.True);
            Assert.That(rect.FindAttribute("", "data-series").Global, Is.True);
            Assert.That(rect.FindAttribute("", "onclick").Global, Is.True);
            Assert.That(rect.FindAttribute("", "fill").Presentation, Is.True);
            Assert.That(rect.FindAttribute("", "fill").DefaultValue, Is.EqualTo("black"));
            Assert.That(rect.IsAttributeApplicable("", "requiredExtensions"), Is.True);
            Assert.That(rect.IsAttributeApplicable("", "kernelMatrix"), Is.False);
            Assert.That(convolve.FindAttribute("", "kernelMatrix").Required, Is.True);
            Assert.That(convolve.FindAttribute("", "divisor").DefaultKind, Is.EqualTo(SvgAttributeDefaultKind.ContextDependent));
            Assert.That(radial.FindAttribute("", "fx").DefaultKind, Is.EqualTo(SvgAttributeDefaultKind.OtherAttribute));
            Assert.That(radial.FindAttribute("", "fx").DefaultValue, Is.EqualTo("cx"));
            Assert.That(svg.FindAttribute("", "width").DefaultKind, Is.EqualTo(SvgAttributeDefaultKind.ContextDependent));
            Assert.That(image.FindAttribute("", "href").Required, Is.True);
            Assert.That(image.FindAttribute(SvgNamespaces.XLink, "href").Required, Is.True);
            Assert.That(path.FindAttribute("", "d").DefaultKind, Is.EqualTo(SvgAttributeDefaultKind.Literal));
            Assert.That(path.FindAttribute("", "d").DefaultValue, Is.Empty);
            Assert.That(path.FindAttribute(null, "d"), Is.Null);
            Assert.That(path.FindAttribute("", null), Is.Null);
        });
    }

    [Test]
    public void RenderModesDistinguishDefaultReferenceResourceAndHostContexts()
    {
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        SvgElementDefinition rect = catalog.Find(SvgElementKind.Rect);
        SvgElementDefinition group = catalog.Find(SvgElementKind.G);
        SvgElementDefinition symbol = catalog.Find(SvgElementKind.Symbol);
        SvgElementDefinition gradient = catalog.Find(SvgElementKind.LinearGradient);
        SvgElementDefinition foreign = catalog.Find(SvgElementKind.ForeignObject);
        SvgElementDefinition defs = catalog.Find(SvgElementKind.Defs);

        Assert.Multiple(() =>
        {
            Assert.That(rect.RenderMode, Is.EqualTo(SvgElementRenderMode.Direct));
            Assert.That(rect.IsRenderable(false, false), Is.True);
            Assert.That(group.RenderMode, Is.EqualTo(SvgElementRenderMode.Container));
            Assert.That(group.IsRenderable(false, false), Is.True);
            Assert.That(symbol.RenderMode, Is.EqualTo(SvgElementRenderMode.Referenced));
            Assert.That(symbol.IsRenderable(false, true), Is.False);
            Assert.That(symbol.IsRenderable(true, false), Is.True);
            Assert.That(gradient.RenderMode, Is.EqualTo(SvgElementRenderMode.Resource));
            Assert.That(gradient.IsRenderable(true, true), Is.False);
            Assert.That(foreign.RenderMode, Is.EqualTo(SvgElementRenderMode.HostCallback));
            Assert.That(foreign.IsRenderable(false, false), Is.False);
            Assert.That(foreign.IsRenderable(false, true), Is.True);
            Assert.That(defs.RenderMode, Is.EqualTo(SvgElementRenderMode.Never));
            Assert.That(defs.IsRenderable(true, true), Is.False);
        });
    }

    [Test]
    public void ModelConstructorsRejectInvalidKindsCardinalitiesAndDefaults()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgElementChildRule(SvgElementKind.Font, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgElementChildRule(SvgElementKind.Rect, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgElementChildRule(SvgElementKind.Rect, 2, 1));
            Assert.Throws<ArgumentNullException>(() => new SvgElementAttributeDefinition(null, "x", false, SvgAttributeDefaultKind.None, "", false, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgElementAttributeDefinition("", "", false, SvgAttributeDefaultKind.None, "", false, false));
            Assert.Throws<ArgumentException>(() => new SvgElementAttributeDefinition("", "x", false, SvgAttributeDefaultKind.None, "0", false, false));
            Assert.Throws<ArgumentException>(() => new SvgElementAttributeDefinition("", "fx", false, SvgAttributeDefaultKind.OtherAttribute, "", false, false));
        });
    }
}
