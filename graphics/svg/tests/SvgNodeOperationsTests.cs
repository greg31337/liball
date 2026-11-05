namespace svg.Tests;

public class SvgNodeOperationsTests
{
    [Test]
    public void CloneNodeCreatesIndependentShallowAndDeepGeneratedCopies()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument source = Load("<svg id='root'><g>value</g></svg>");
        SvgNode root = source.DocumentElement;

        SvgNode shallow = api.CloneNode(root, false);
        SvgNode deep = api.CloneNode(root, true);

        Assert.Multiple(() =>
        {
            Assert.That(shallow, Is.Not.SameAs(root));
            Assert.That(shallow.Children, Is.Empty);
            Assert.That(shallow.Attributes.Count, Is.EqualTo(1));
            Assert.That(shallow.Attributes[0], Is.Not.SameAs(root.Attributes[0]));
            Assert.That(shallow.Attributes[0].OwnerElement, Is.SameAs(shallow));
            Assert.That(shallow.OwnerDocument, Is.Null);
            Assert.That(shallow.SourceSpan, Is.Null);
            Assert.That(deep.Children[0].Parent, Is.SameAs(deep));
            Assert.That(deep.Children[0].Children[0].Parent, Is.SameAs(deep.Children[0]));
            Assert.That(deep.OwnerDocument, Is.Null);
            Assert.That(deep.Children[0].OwnerDocument, Is.Null);
            Assert.That(api.DeepEquals(root, deep), Is.True);
        });

        deep.Children[0].Children[0].Value = "changed";
        Assert.That(root.Children[0].Children[0].Value, Is.EqualTo("value"));
    }

    [Test]
    public void ImportCopiesIntoTargetOwnershipWithoutMutatingEitherDocument()
    {
        SvgDocumentApi api = new SvgDocumentApi();
        SvgDocument source = Load("<svg><g data-key='one'><rect/></g></svg>");
        SvgDocument target = Load("<svg/>");
        SvgNode group = source.DocumentElement.Children[0];

        SvgNode imported = api.ImportNode(target, group, true);

        Assert.Multiple(() =>
        {
            Assert.That(source.Version, Is.Zero);
            Assert.That(target.Version, Is.Zero);
            Assert.That(imported.Parent, Is.Null);
            Assert.That(imported.OwnerDocument, Is.SameAs(target));
            Assert.That(imported.Children[0].OwnerDocument, Is.SameAs(target));
            Assert.That(imported.Attributes[0].OwnerDocument, Is.SameAs(target));
            Assert.That(imported.Attributes[0].OwnerElement, Is.SameAs(imported));
            Assert.That(imported.IsDirty, Is.False);
            Assert.That(group.Parent, Is.SameAs(source.DocumentElement));
            Assert.That(group.OwnerDocument, Is.SameAs(source));
        });
    }

    [Test]
    public void AdoptDetachesAndReassignsAWholeSubtreeWhileInvalidatingOnce()
    {
        SvgDocumentApi api = new SvgDocumentApi();
        SvgDocument source = Load("<svg><g data-key='one'><rect/></g></svg>");
        SvgDocument target = Load("<svg/>");
        SvgNode group = source.DocumentElement.Children[0];
        SvgNode rect = group.Children[0];
        SvgAttribute attribute = group.Attributes[0];

        SvgMutationResult adopted = api.AdoptNode(target, group);
        SvgMutationResult adoptedAgain = api.AdoptNode(target, group);

        Assert.Multiple(() =>
        {
            Assert.That(adopted.Success, Is.True);
            Assert.That(adopted.Node, Is.SameAs(group));
            Assert.That(adoptedAgain.Success, Is.True);
            Assert.That(source.Version, Is.EqualTo(1));
            Assert.That(target.Version, Is.EqualTo(1));
            Assert.That(source.DocumentElement.Children, Is.Empty);
            Assert.That(group.Parent, Is.Null);
            Assert.That(new[] { group.OwnerDocument, rect.OwnerDocument, attribute.OwnerDocument }, Has.All.SameAs(target));
            Assert.That(attribute.OwnerElement, Is.SameAs(group));
            Assert.That(group.IsDirty, Is.True);
        });
    }

    [Test]
    public void TextContentReadsRelevantDescendantsAndWritesAtomically()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg>one<!--skip--><g><![CDATA[two]]><?edit skip?>three</g></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode oldGroup = root.Children[2];

        Assert.That(api.GetTextContent(root), Is.EqualTo("onetwothree"));
        long before = document.Version;
        SvgMutationResult changed = api.SetTextContent(root, "replacement");
        SvgMutationResult noOp = api.SetTextContent(root, "replacement");

        Assert.Multiple(() =>
        {
            Assert.That(changed.Success, Is.True);
            Assert.That(noOp.Success, Is.True);
            Assert.That(document.Version, Is.EqualTo(before + 1));
            Assert.That(root.Children.Count, Is.EqualTo(1));
            Assert.That(root.Children[0].Kind, Is.EqualTo(SvgNodeKind.Text));
            Assert.That(root.Children[0].Value, Is.EqualTo("replacement"));
            Assert.That(root.Children[0].OwnerDocument, Is.SameAs(document));
            Assert.That(oldGroup.Parent, Is.Null);
            Assert.That(oldGroup.OwnerDocument, Is.Null);
        });
    }

    [Test]
    public void NormalizeTextRecursesMergesAdjacentTextAndPreservesCDataBoundaries()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = factory.CreateDocument();
        SvgNode root = factory.CreateElement("svg", "", "svg", SvgNamespaces.Svg);
        SvgNode group = factory.CreateElement("g", "", "g", SvgNamespaces.Svg);
        api.AppendChild(document.DocumentNode, root);
        api.AppendChild(root, factory.CreateText(""));
        api.AppendChild(root, factory.CreateText("a"));
        SvgNode removed = factory.CreateText("b");
        api.AppendChild(root, removed);
        api.AppendChild(root, factory.CreateCData("C"));
        api.AppendChild(root, factory.CreateText("c"));
        api.AppendChild(root, factory.CreateText("d"));
        api.AppendChild(root, group);
        api.AppendChild(group, factory.CreateText("x"));
        api.AppendChild(group, factory.CreateText("y"));
        long before = document.Version;

        SvgMutationResult normalized = api.NormalizeText(root);
        SvgMutationResult noOp = api.NormalizeText(root);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.Success, Is.True);
            Assert.That(noOp.Success, Is.True);
            Assert.That(document.Version, Is.EqualTo(before + 1));
            Assert.That(root.Children.Select(node => node.Kind), Is.EqualTo(new[] { SvgNodeKind.Text, SvgNodeKind.CData, SvgNodeKind.Text, SvgNodeKind.Element }));
            Assert.That(root.Children[0].Value, Is.EqualTo("ab"));
            Assert.That(root.Children[2].Value, Is.EqualTo("cd"));
            Assert.That(group.Children.Count, Is.EqualTo(1));
            Assert.That(group.Children[0].Value, Is.EqualTo("xy"));
            Assert.That(removed.Parent, Is.Null);
            Assert.That(removed.OwnerDocument, Is.Null);
        });
    }

    [Test]
    public void NamespaceLookupHonorsNearestDeclarationsShadowingAndBuiltIns()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:a='urn:outer'><g xmlns:a='urn:inner'><a:item><text/></a:item></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode item = group.Children[0];
        SvgNode text = item.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(api.LookupNamespaceUri(text, "a"), Is.EqualTo("urn:inner"));
            Assert.That(api.LookupNamespaceUri(text, ""), Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(api.LookupNamespaceUri(text, "xml"), Is.EqualTo(SvgNamespaces.Xml));
            Assert.That(api.LookupPrefix(text, "urn:inner"), Is.EqualTo("a"));
            Assert.That(api.LookupPrefix(text, "urn:outer"), Is.EqualTo(""));
            Assert.That(api.IsDefaultNamespace(text, SvgNamespaces.Svg), Is.True);
        });
    }

    [Test]
    public void TraversalHelpersReturnStableRelationshipSnapshotsInDocumentOrder()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg><rect/><g><path/></g><circle/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode rect = root.Children[0];
        SvgNode group = root.Children[1];
        SvgNode path = group.Children[0];
        SvgNode circle = root.Children[2];

        List<SvgNode> descendants = api.GetDescendants(root);
        List<SvgNode> ancestors = api.GetAncestors(path);

        Assert.Multiple(() =>
        {
            Assert.That(descendants, Is.EqualTo(new[] { rect, group, path, circle }));
            Assert.That(ancestors, Is.EqualTo(new[] { group, root, document.DocumentNode }));
            Assert.That(api.GetPreviousSibling(group), Is.SameAs(rect));
            Assert.That(api.GetNextSibling(group), Is.SameAs(circle));
            Assert.That(api.GetPreviousSiblings(circle), Is.EqualTo(new[] { rect, group }));
            Assert.That(api.GetNextSiblings(rect), Is.EqualTo(new[] { group, circle }));
            Assert.That(api.IsAncestor(root, path), Is.True);
            Assert.That(api.IsDescendant(path, root), Is.True);
            Assert.That(api.IsAncestor(path, path), Is.False);
        });
    }

    [Test]
    public void DeepEqualityUsesExpandedNamesUnorderedAttributesAndOrderedChildren()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi api = new SvgNodeApi();
        SvgNode first = factory.CreateElement("a:item", "a", "item", "urn:test");
        SvgNode second = factory.CreateElement("b:item", "b", "item", "urn:test");
        api.AppendAttribute(first, factory.CreateAttribute("id", "", "id", "", "one"));
        api.AppendAttribute(first, factory.CreateAttribute("a:key", "a", "key", "urn:attr", "two"));
        api.AppendAttribute(second, factory.CreateAttribute("b:key", "b", "key", "urn:attr", "two"));
        api.AppendAttribute(second, factory.CreateAttribute("id", "", "id", "", "one"));
        api.AppendChild(first, factory.CreateText("value"));
        api.AppendChild(second, factory.CreateText("value"));

        Assert.That(api.DeepEquals(first, second), Is.True);

        second.Attributes[0].Value = "changed";
        Assert.That(api.DeepEquals(first, second), Is.False);
    }

    static SvgDocument Load(string input)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }
}
