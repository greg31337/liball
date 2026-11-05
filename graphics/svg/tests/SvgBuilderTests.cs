namespace svg.Tests;

public class SvgBuilderTests
{
    [Test]
    public void BuildsSvgAttributesStylesStylesheetsAndTextFluently()
    {
        SvgBuilder builder = new SvgBuilder();

        SvgBuilder returned = builder
            .SetAttribute("viewBox", "0 0 20 10")
            .BeginElement(SvgElementKind.G)
            .SetAttribute("id", "layer")
            .BeginElement(SvgElementKind.Text)
            .SetNamespacedAttribute("xml:lang", "xml", "lang", SvgNamespaces.Xml, "fr")
            .SetStyle("fill", "red")
            .SetStyleWithPriority("font-size", "12px", true)
            .AddText("Héllo <&>")
            .EndElement()
            .BeginElement(SvgElementKind.Rect)
            .SetAttribute("width", "20")
            .SetAttribute("height", "10")
            .SetStyle("fill", "blue")
            .SetStyle("stroke", "black")
            .SetStyle("fill", "lime")
            .RemoveStyle("stroke")
            .EndElement()
            .AddStyleSheet("#layer{opacity:.5}")
            .AddComment("generated")
            .EndElement();

        SvgDocument document = builder.Build();
        SvgNode group = document.Index.GetById("layer");
        SvgNode text = group.Children.Single(item => item.ElementKind == SvgElementKind.Text);
        SvgNode rect = group.Children.Single(item => item.ElementKind == SvgElementKind.Rect);
        SvgNode style = group.Children.Single(item => item.ElementKind == SvgElementKind.Style);

        Assert.Multiple(() =>
        {
            Assert.That(returned, Is.SameAs(builder));
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
            Assert.That(document.DocumentElement, Is.SameAs(builder.Root));
            Assert.That(builder.Current, Is.SameAs(builder.Root));
            Assert.That(builder.Root.ElementKind, Is.EqualTo(SvgElementKind.Svg));
            Assert.That(builder.Root.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(Attribute(builder.Root, SvgNamespaces.Xmlns, "xmlns"), Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(Attribute(builder.Root, "", "viewBox"), Is.EqualTo("0 0 20 10"));
            Assert.That(Attribute(text, SvgNamespaces.Xml, "lang"), Is.EqualTo("fr"));
            Assert.That(new SvgNodeApi().GetTextContent(text), Is.EqualTo("Héllo <&>"));
            Assert.That(Attribute(text, "", "style"), Is.EqualTo("fill:red;font-size:12px!important;"));
            Assert.That(Attribute(rect, "", "style"), Is.EqualTo("fill:lime;"));
            Assert.That(new SvgNodeApi().GetTextContent(style), Is.EqualTo("#layer{opacity:.5}"));
            Assert.That(group.Children.Last().Kind, Is.EqualTo(SvgNodeKind.Comment));
            Assert.That(document.IsDirty, Is.True);
            Assert.That(document.Version, Is.GreaterThan(0));
        });
    }

    [Test]
    public void CreatesEveryRecognizedElementThroughOneDataDrivenOperation()
    {
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        List<SvgElementDefinition> definitions = catalog.GetEntries();
        List<SvgLegacyFeatureDefinition> legacyDefinitions = new SvgCatalogFactory().CreateLegacyCatalog().GetElementEntries();
        SvgBuilder builder = new SvgBuilder();
        builder.BeginElement(SvgElementKind.Defs);
        SvgNode parent = builder.Current;

        foreach (SvgElementDefinition definition in definitions)
        {
            builder.BeginElement(definition.Kind);
            Assert.That(builder.Succeeded(), Is.True, definition.Kind + ": " + builder.Message);
            Assert.That(builder.Current.ElementKind, Is.EqualTo(definition.Kind));
            Assert.That(builder.Current.LocalName, Is.EqualTo(definition.LocalName));
            Assert.That(builder.Current.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
            builder.EndElement();
        }
        foreach (SvgLegacyFeatureDefinition definition in legacyDefinitions)
        {
            builder.BeginElement(definition.ElementKind);
            Assert.That(builder.Succeeded(), Is.True, definition.ElementKind + ": " + builder.Message);
            Assert.That(builder.Current.ElementKind, Is.EqualTo(definition.ElementKind));
            Assert.That(builder.Current.LocalName, Is.EqualTo(definition.LocalName));
            Assert.That(builder.Current.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
            builder.EndElement();
        }

        Assert.Multiple(() =>
        {
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
            Assert.That(builder.Current, Is.SameAs(parent));
            Assert.That(parent.Children, Has.Count.EqualTo(definitions.Count + legacyDefinitions.Count));
            Assert.That(parent.Children.Take(definitions.Count).Select(item => item.ElementKind), Is.EqualTo(definitions.Select(item => item.Kind)));
            Assert.That(parent.Children.Skip(definitions.Count).Select(item => item.ElementKind), Is.EqualTo(legacyDefinitions.Select(item => item.ElementKind)));
        });
    }

    [Test]
    public void EditsExistingDocumentsAndSupportsForeignNodesCDataAndReplacementText()
    {
        SvgDocument existing = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='target'><text>old</text></g></svg>");
        SvgNode target = existing.Index.GetById("target");
        SvgBuilder builder = SvgBuilder.Create(existing);
        builder.Select(target)
            .BeginElement(SvgElementKind.Desc)
            .AddText("first")
            .SetText("replacement")
            .EndElement()
            .BeginElementWithNamespace("html:span", "html", "span", SvgNamespaces.XHtml)
            .SetAttribute("class", "caption")
            .AddCData("foreign text")
            .EndElement();

        SvgNode description = target.Children.Single(item => item.ElementKind == SvgElementKind.Desc);
        SvgNode foreign = target.Children.Single(item => item.NamespaceUri == SvgNamespaces.XHtml);
        SvgDocument replacement = new SvgDocument();
        SvgBuilder switched = builder.UseDocument(replacement).AddElement(SvgElementKind.Circle);

        Assert.Multiple(() =>
        {
            Assert.That(new SvgNodeApi().GetTextContent(description), Is.EqualTo("replacement"));
            Assert.That(description.Children, Has.Count.EqualTo(1));
            Assert.That(foreign.QualifiedName, Is.EqualTo("html:span"));
            Assert.That(foreign.Children.Single().Kind, Is.EqualTo(SvgNodeKind.CData));
            Assert.That(Attribute(foreign, "", "class"), Is.EqualTo("caption"));
            Assert.That(switched, Is.SameAs(builder));
            Assert.That(builder.Document, Is.SameAs(replacement));
            Assert.That(builder.Root, Is.SameAs(replacement.DocumentElement));
            Assert.That(builder.Root.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.Circle));
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
        });
    }

    [Test]
    public void ReportsStickyErrorsWithoutApplyingLaterOperationsAndCanRecoverExplicitly()
    {
        SvgBuilder builder = new SvgBuilder();
        int originalChildren = builder.Root.Children.Count;

        builder.BeginElement(SvgElementKind.Unknown).BeginElement(SvgElementKind.Rect);

        Assert.Multiple(() =>
        {
            Assert.That(builder.Error, Is.EqualTo(SvgBuilderError.UnsupportedElement));
            Assert.That(builder.Root.Children, Has.Count.EqualTo(originalChildren));
            Assert.That(builder.Message, Is.Not.Empty);
        });

        builder.ClearError().EndElement();
        Assert.That(builder.Error, Is.EqualTo(SvgBuilderError.InvalidState));

        builder.ClearError().SetNamespacedAttribute("bad:name", "", "bad:name", "", "value");
        Assert.That(builder.Error, Is.EqualTo(SvgBuilderError.InvalidArgument));
        Assert.That(builder.Root.Attributes.Any(item => item.Value == "value"), Is.False);

        builder.ClearError().SetStyle("fill", "red;stroke:blue");
        Assert.That(builder.Error, Is.EqualTo(SvgBuilderError.InvalidStyle));
        Assert.That(builder.Root.Attributes.Any(item => item.LocalName == "style"), Is.False);

        builder.ClearError().AddStyleSheet("broken");
        Assert.That(builder.Error, Is.EqualTo(SvgBuilderError.InvalidStyle));
        Assert.That(builder.Root.Children, Is.Empty);

        builder.ClearError().Append(builder.Root);
        Assert.Multiple(() =>
        {
            Assert.That(builder.Error, Is.EqualTo(SvgBuilderError.MutationFailed));
            Assert.That(builder.MutationError, Is.EqualTo(SvgMutationError.NodeAlreadyAttached));
        });

        builder.ClearError().BeginElement(SvgElementKind.Circle).EndElement();
        Assert.Multiple(() =>
        {
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
            Assert.That(builder.Root.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.Circle));
            Assert.Throws<ArgumentNullException>(() => SvgBuilder.Create(null));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Attribute(SvgNode element, string namespaceUri, string localName)
    {
        return element.Attributes.Single(item => item.NamespaceUri == namespaceUri && item.LocalName == localName).Value;
    }
}
