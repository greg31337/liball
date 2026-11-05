namespace svg.Tests;

public class SvgDomCoverageTests
{
    [Test]
    public void LoaderConnectsEveryNodeKindWithRetainedSourceData()
    {
        string input = "<!DOCTYPE svg><!--before--><?edit keep?><svg>text<![CDATA[cdata]]><g/></svg>";
        SvgLoadOptions options = new SvgLoadOptions();
        options.DtdPolicy = SvgDtdPolicy.PreserveOnly;

        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);
        SvgDocument document = loaded.Document;
        SvgNode root = document.DocumentElement;
        List<SvgNode> nodes = new List<SvgNode>
        {
            document.DocumentNode,
            document.DocumentNode.Children[0],
            document.DocumentNode.Children[1],
            document.DocumentNode.Children[2],
            root,
            root.Children[0],
            root.Children[1]
        };

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Success, Is.True);
            Assert.That(nodes.Select(node => node.Kind), Is.EqualTo(new[]
            {
                SvgNodeKind.Document,
                SvgNodeKind.Doctype,
                SvgNodeKind.Comment,
                SvgNodeKind.ProcessingInstruction,
                SvgNodeKind.Element,
                SvgNodeKind.Text,
                SvgNodeKind.CData
            }));
            Assert.That(nodes, Has.All.Matches<SvgNode>(node => ReferenceEquals(node.OwnerDocument, document)));
            Assert.That(nodes, Has.All.Matches<SvgNode>(node => node.SourceSpan != null));
            Assert.That(document.DocumentNode.Children, Has.All.Matches<SvgNode>(node => ReferenceEquals(node.Parent, document.DocumentNode)));
            Assert.That(root.Children, Has.All.Matches<SvgNode>(node => ReferenceEquals(node.Parent, root)));
            Assert.That(document.DocumentNode.Children[0].RawToken.RawText, Is.EqualTo("<!DOCTYPE svg>"));
            Assert.That(document.DocumentNode.Children[1].RawToken.RawText, Is.EqualTo("<!--before-->"));
            Assert.That(document.DocumentNode.Children[2].RawToken.RawText, Is.EqualTo("<?edit keep?>"));
            Assert.That(root.Children[0].RawToken.RawText, Is.EqualTo("text"));
            Assert.That(root.Children[1].RawToken.RawText, Is.EqualTo("<![CDATA[cdata]]>"));
        });
    }

    [Test]
    public void NamespaceScopesKeepSameLocalNamesDistinctAcrossElementsAndAttributes()
    {
        string input = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:a='urn:a' xmlns:b='urn:b' a:key='one' b:key='two'>" +
            "<a:item key='plain'/><b:item/><item xmlns=''/><item/></svg>";
        SvgDocument document = Load(input);
        SvgNode root = document.DocumentElement;
        SvgNode first = root.Children[0];
        SvgNode second = root.Children[1];
        SvgNode reset = root.Children[2];
        SvgNode inherited = root.Children[3];

        Assert.Multiple(() =>
        {
            Assert.That(first.NamespaceUri, Is.EqualTo("urn:a"));
            Assert.That(second.NamespaceUri, Is.EqualTo("urn:b"));
            Assert.That(reset.NamespaceUri, Is.Empty);
            Assert.That(inherited.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(first.Attributes[0].NamespaceUri, Is.Empty);
            Assert.That(root.Attributes.Single(attribute => attribute.Prefix == "a" && attribute.LocalName == "key").Value, Is.EqualTo("one"));
            Assert.That(root.Attributes.Single(attribute => attribute.Prefix == "b" && attribute.LocalName == "key").Value, Is.EqualTo("two"));
            Assert.That(document.Index.GetByExpandedName("urn:a", "item"), Is.EqualTo(new[] { first }));
            Assert.That(document.Index.GetByExpandedName("urn:b", "item"), Is.EqualTo(new[] { second }));
            Assert.That(document.Index.GetByExpandedName("", "item"), Is.EqualTo(new[] { reset }));
            Assert.That(document.Index.GetByExpandedName(SvgNamespaces.Svg, "item"), Is.EqualTo(new[] { inherited }));
        });
    }

    [Test]
    public void CrossDocumentMoveInvalidatesBothIndexesAndRefreshesDescendantOwnership()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument source = Load("<svg><g id='moved' class='old'><rect id='inside'/></g><circle/></svg>");
        SvgDocument target = Load("<svg><path id='target'/></svg>");
        SvgNode group = source.DocumentElement.Children[0];
        SvgNode rectangle = group.Children[0];

        Assert.That(source.Index.GetById("moved"), Is.SameAs(group));
        Assert.That(target.Index.GetById("moved"), Is.Null);
        SvgMutationResult moved = mutations.MoveChild(group, target.DocumentElement, 1);
        SvgAttribute oldClass = group.Attributes.Single(attribute => attribute.LocalName == "class");
        SvgAttribute newClass = documents.CreateAttribute("class", "", "class", "", "new shared");
        SvgMutationResult replaced = mutations.ReplaceAttribute(group, oldClass, newClass);

        Assert.Multiple(() =>
        {
            Assert.That(moved.Success, Is.True);
            Assert.That(replaced.Success, Is.True);
            Assert.That(source.Index.GetById("moved"), Is.Null);
            Assert.That(source.Index.GetById("inside"), Is.Null);
            Assert.That(source.Index.GetByQualifiedName("g"), Is.Empty);
            Assert.That(target.Index.GetById("moved"), Is.SameAs(group));
            Assert.That(target.Index.GetById("inside"), Is.SameAs(rectangle));
            Assert.That(target.Index.GetByClass("old"), Is.Empty);
            Assert.That(target.Index.GetByClass("new"), Is.EqualTo(new[] { group }));
            Assert.That(target.Index.GetByClass("shared"), Is.EqualTo(new[] { group }));
            Assert.That(group.OwnerDocument, Is.SameAs(target));
            Assert.That(rectangle.OwnerDocument, Is.SameAs(target));
            Assert.That(group.Parent, Is.SameAs(target.DocumentElement));
        });
    }

    [TestCase(SvgNodeKind.Document)]
    [TestCase(SvgNodeKind.Element)]
    [TestCase(SvgNodeKind.Text)]
    [TestCase(SvgNodeKind.CData)]
    [TestCase(SvgNodeKind.Comment)]
    [TestCase(SvgNodeKind.ProcessingInstruction)]
    [TestCase(SvgNodeKind.Doctype)]
    public void DeepClonePreservesEveryNodeTagButDropsSourceAndOwnership(SvgNodeKind kind)
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgSourceUri source = new SvgSourceUri("memory:clone");
        SvgNode node = new SvgNode(kind, kind == SvgNodeKind.Element ? "v:item" : "", kind == SvgNodeKind.Element ? "v" : "", kind == SvgNodeKind.Element ? "item" : "", kind == SvgNodeKind.Element ? "urn:vendor" : "", "value");
        SvgNode child = new SvgNode(SvgNodeKind.Comment, "", "", "", "", "child");
        node.SourceSpan = new SvgSourceSpan(source, 0, 1, 1, 5, 1, 6);
        child.SourceSpan = new SvgSourceSpan(source, 1, 1, 2, 4, 1, 5);
        child.Parent = node;
        node.Children.Add(child);
        if (kind == SvgNodeKind.Element)
        {
            SvgAttribute attribute = new SvgAttribute("v:key", "v", "key", "urn:vendor", "one");
            attribute.OwnerElement = node;
            attribute.SourceSpan = new SvgSourceSpan(source, 1, 1, 2, 2, 1, 3);
            node.Attributes.Add(attribute);
        }

        SvgNode clone = api.CloneNode(node, true);

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(node));
            Assert.That(clone.Kind, Is.EqualTo(kind));
            Assert.That(clone.Value, Is.EqualTo("value"));
            Assert.That(clone.SourceSpan, Is.Null);
            Assert.That(clone.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Generated));
            Assert.That(clone.Parent, Is.Null);
            Assert.That(clone.OwnerDocument, Is.Null);
            Assert.That(clone.Children.Single(), Is.Not.SameAs(child));
            Assert.That(clone.Children.Single().Kind, Is.EqualTo(SvgNodeKind.Comment));
            Assert.That(clone.Children.Single().Parent, Is.SameAs(clone));
            Assert.That(clone.Children.Single().SourceSpan, Is.Null);
            Assert.That(clone.Attributes, Has.Count.EqualTo(kind == SvgNodeKind.Element ? 1 : 0));
            if (kind == SvgNodeKind.Element)
            {
                Assert.That(clone.Attributes[0].OwnerElement, Is.SameAs(clone));
                Assert.That(clone.Attributes[0].NamespaceUri, Is.EqualTo("urn:vendor"));
                Assert.That(clone.Attributes[0].SourceSpan, Is.Null);
            }
        });
    }

    [Test]
    public void DiffAndInversePatchCommentsInstructionsCDataAndUnknownElements()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi nodes = new SvgNodeApi();
        SvgDocument document = Load("<svg><!--old--><?edit before?><![CDATA[first]]><v:old xmlns:v='urn:vendor'/></svg>");
        SvgDocument original = Load("<svg><!--old--><?edit before?><![CDATA[first]]><v:old xmlns:v='urn:vendor'/></svg>");
        SvgDocument desired = Load("<svg><!--new--><?edit after?><![CDATA[second]]><v:new xmlns:v='urn:vendor'/><future/></svg>");

        SvgPatch patch = documents.Diff(document, desired);
        SvgPatchResult applied = documents.ApplyPatch(document, patch);

        Assert.Multiple(() =>
        {
            Assert.That(applied.Success, Is.True);
            Assert.That(nodes.DeepEquals(document.DocumentNode, desired.DocumentNode), Is.True);
            Assert.That(patch.Operations.Count(operation => operation.Kind == SvgPatchOperationKind.NodeValue), Is.EqualTo(3));
            Assert.That(patch.Operations.Any(operation => operation.Kind == SvgPatchOperationKind.NodeReplace), Is.True);
            Assert.That(patch.Operations.Any(operation => operation.Kind == SvgPatchOperationKind.NodeInsert), Is.True);
            Assert.That(document.DocumentElement.Children.Select(node => node.Kind), Is.EqualTo(new[]
            {
                SvgNodeKind.Comment,
                SvgNodeKind.ProcessingInstruction,
                SvgNodeKind.CData,
                SvgNodeKind.Element,
                SvgNodeKind.Element
            }));
        });

        SvgPatchResult reversed = applied.Inverse.Apply(document);

        Assert.Multiple(() =>
        {
            Assert.That(reversed.Success, Is.True);
            Assert.That(nodes.DeepEquals(document.DocumentNode, original.DocumentNode), Is.True);
        });
    }

    [Test]
    public void UnknownVendorContentRetainsPrefixesAttributeOrderRawValuesAndSpans()
    {
        string input = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:v='urn:vendor' v:root='A&amp;B'>" +
            "<v:widget v:mode='raw' plain='two' data-extra='three'><v:child/></v:widget></svg>";
        SvgDocument document = Load(input);
        SvgNode root = document.DocumentElement;
        SvgNode widget = root.Children[0];
        SvgNode child = widget.Children[0];
        SvgAttribute rootVendor = root.Attributes[2];

        Assert.Multiple(() =>
        {
            Assert.That(widget.ElementKind, Is.EqualTo(SvgElementKind.Unknown));
            Assert.That(widget.QualifiedName, Is.EqualTo("v:widget"));
            Assert.That(widget.Prefix, Is.EqualTo("v"));
            Assert.That(widget.NamespaceUri, Is.EqualTo("urn:vendor"));
            Assert.That(widget.Attributes.Select(attribute => attribute.QualifiedName), Is.EqualTo(new[] { "v:mode", "plain", "data-extra" }));
            Assert.That(widget.Attributes.Select(attribute => attribute.Value), Is.EqualTo(new[] { "raw", "two", "three" }));
            Assert.That(child.ElementKind, Is.EqualTo(SvgElementKind.Unknown));
            Assert.That(child.NamespaceUri, Is.EqualTo("urn:vendor"));
            Assert.That(rootVendor.Value, Is.EqualTo("A&B"));
            Assert.That(rootVendor.RawToken.Value, Is.EqualTo("A&amp;B"));
            Assert.That(rootVendor.SourceSpan, Is.Not.Null);
            Assert.That(widget.RawToken.RawText, Does.StartWith("<v:widget"));
            Assert.That(widget.EndToken.RawText, Is.EqualTo("</v:widget>"));
            Assert.That(widget.SourceSpan.StartOffset, Is.EqualTo(input.IndexOf("<v:widget", StringComparison.Ordinal)));
            Assert.That(widget.SourceSpan.EndOffset, Is.EqualTo(input.IndexOf("</v:widget>", StringComparison.Ordinal) + "</v:widget>".Length));
            Assert.That(child.SourceSpan.StartOffset, Is.EqualTo(input.IndexOf("<v:child", StringComparison.Ordinal)));
            Assert.That(child.SourceSpan.EndOffset, Is.EqualTo(input.IndexOf("<v:child", StringComparison.Ordinal) + "<v:child/>".Length));
        });
    }

    [Test]
    public void UntouchedNodeAndAttributeSpansSurviveSiblingAndAttributeMutations()
    {
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg id='root'><g data-key='keep'/><rect/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgAttribute keptAttribute = group.Attributes[0];
        SvgNode rectangle = root.Children[1];
        SvgSourceSpan groupSpan = group.SourceSpan;
        SvgSourceSpan attributeSpan = keptAttribute.SourceSpan;
        SvgSourceSpan rectangleSpan = rectangle.SourceSpan;
        SvgAttribute oldId = root.Attributes[0];

        mutations.ReplaceAttribute(root, oldId, documents.CreateAttribute("id", "", "id", "", "changed"));
        mutations.InsertChild(root, 1, documents.CreateComment("generated"));

        Assert.Multiple(() =>
        {
            Assert.That(group.SourceSpan, Is.SameAs(groupSpan));
            Assert.That(keptAttribute.SourceSpan, Is.SameAs(attributeSpan));
            Assert.That(rectangle.SourceSpan, Is.SameAs(rectangleSpan));
            Assert.That(group.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(keptAttribute.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(rectangle.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Original));
            Assert.That(root.SourceMapKind, Is.EqualTo(SvgSourceMapKind.Rewritten));
            Assert.That(root.Children[1].SourceMapKind, Is.EqualTo(SvgSourceMapKind.Generated));
            Assert.That(root.Children[1].SourceSpan, Is.Null);
        });
    }

    static SvgDocument Load(string input)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }
}
