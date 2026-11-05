namespace svg.Tests;

public class SvgDocumentIndexTests
{
    [Test]
    public void IndexesIdsClassesNamesAndNamespacesInDocumentOrder()
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg xmlns='http://www.w3.org/2000/svg' xmlns:x='urn:x' id='root' class='a b a'><g id='same' class='b c'/><g id='same' class='c'/><x:item class='a'/></svg>", new SvgLoadOptions());
        SvgDocument document = loaded.Document;
        SvgNode root = document.DocumentElement;
        SvgNode firstGroup = root.Children[0];
        SvgNode secondGroup = root.Children[1];
        SvgNode foreign = root.Children[2];

        List<SvgNode> classA = document.Index.GetByClass("a");
        List<SvgNode> groups = document.Index.GetByQualifiedName("g");
        List<SvgNode> svgElements = document.Index.GetByNamespace(SvgNamespaces.Svg);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Success, Is.True);
            Assert.That(document.Index.IndexedVersion, Is.EqualTo(document.Version));
            Assert.That(document.Index.GetById("root"), Is.SameAs(root));
            Assert.That(document.Index.GetById("same"), Is.SameAs(firstGroup));
            Assert.That(document.Index.GetById("missing"), Is.Null);
            Assert.That(classA, Is.EqualTo(new[] { root, foreign }));
            Assert.That(document.Index.GetByClass("b"), Is.EqualTo(new[] { root, firstGroup }));
            Assert.That(document.Index.GetByClass("c"), Is.EqualTo(new[] { firstGroup, secondGroup }));
            Assert.That(groups, Is.EqualTo(new[] { firstGroup, secondGroup }));
            Assert.That(document.Index.GetByQualifiedName("x:item"), Is.EqualTo(new[] { foreign }));
            Assert.That(svgElements, Is.EqualTo(new[] { root, firstGroup, secondGroup }));
            Assert.That(document.Index.GetByNamespace("urn:x"), Is.EqualTo(new[] { foreign }));
            Assert.That(document.Index.GetByExpandedName(SvgNamespaces.Svg, "g"), Is.EqualTo(new[] { firstGroup, secondGroup }));
        });

        classA.Clear();
        groups.Reverse();
        svgElements.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(document.Index.GetByClass("a"), Is.EqualTo(new[] { root, foreign }));
            Assert.That(document.Index.GetByQualifiedName("g"), Is.EqualTo(new[] { firstGroup, secondGroup }));
            Assert.That(document.Index.GetByNamespace(SvgNamespaces.Svg), Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void DuplicateIdsProduceLocatedFirstWinsWarningsInDocumentOrder()
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg><g id='same'/><rect id='same'/><circle id='same'/></svg>", new SvgLoadOptions());
        SvgDocument document = loaded.Document;
        SvgNode first = document.DocumentElement.Children[0];
        List<SvgDiagnostic> diagnostics = document.Index.GetDuplicateIdDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Success, Is.True);
            Assert.That(document.Index.GetById("same"), Is.SameAs(first));
            Assert.That(diagnostics, Has.Count.EqualTo(2));
            Assert.That(diagnostics, Has.All.Matches<SvgDiagnostic>(item => item.Severity == SvgDiagnosticSeverity.Warning && item.Code == SvgDiagnosticCode.DuplicateId));
            Assert.That(diagnostics[0].Source, Is.SameAs(document.DocumentElement.Children[1].Attributes[0].SourceSpan));
            Assert.That(diagnostics[1].Source, Is.SameAs(document.DocumentElement.Children[2].Attributes[0].SourceSpan));
            Assert.That(diagnostics[0].RelatedSpans, Is.EqualTo(new[] { first.Attributes[0].SourceSpan }));
            Assert.That(loaded.Diagnostics.Where(item => item.Code == SvgDiagnosticCode.DuplicateId), Is.EqualTo(diagnostics));
            Assert.That(document.Diagnostics.Where(item => item.Code == SvgDiagnosticCode.DuplicateId), Is.EqualTo(diagnostics));
        });
    }

    [Test]
    public void MutationsInvalidateAndRefreshKeysWithoutAccumulatingDiagnostics()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg><g id='one' class='old'/></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgAttribute oldId = group.Attributes[0];
        SvgAttribute oldClass = group.Attributes[1];
        SvgAttribute newId = factory.CreateAttribute("id", "", "id", "", "two");
        SvgAttribute newClass = factory.CreateAttribute("class", "", "class", "", "new shared");

        mutations.ReplaceAttribute(group, oldId, newId);
        mutations.ReplaceAttribute(group, oldClass, newClass);

        Assert.That(document.Index.IndexedVersion, Is.EqualTo(-1));
        Assert.Multiple(() =>
        {
            Assert.That(document.Index.GetById("one"), Is.Null);
            Assert.That(document.Index.GetById("two"), Is.SameAs(group));
            Assert.That(document.Index.GetByClass("old"), Is.Empty);
            Assert.That(document.Index.GetByClass("new"), Is.EqualTo(new[] { group }));
            Assert.That(document.Index.IndexedVersion, Is.EqualTo(document.Version));
        });

        SvgNode duplicate = factory.CreateElement("rect", "", "rect", SvgNamespaces.Svg);
        mutations.AppendAttribute(duplicate, factory.CreateAttribute("id", "", "id", "", "two"));
        mutations.AppendAttribute(duplicate, factory.CreateAttribute("class", "", "class", "", "shared"));
        mutations.AppendChild(document.DocumentElement, duplicate);

        Assert.That(document.Index.GetById("two"), Is.SameAs(group));
        Assert.Multiple(() =>
        {
            Assert.That(document.Index.GetByClass("shared"), Is.EqualTo(new[] { group, duplicate }));
            Assert.That(document.Index.GetDuplicateIdDiagnostics(), Has.Count.EqualTo(1));
            Assert.That(document.Diagnostics.Count(item => item.Code == SvgDiagnosticCode.DuplicateId), Is.EqualTo(1));
        });

        mutations.RemoveChild(document.DocumentElement, duplicate);

        Assert.Multiple(() =>
        {
            Assert.That(document.Index.IndexedVersion, Is.EqualTo(-1));
            Assert.That(document.Diagnostics.Count(item => item.Code == SvgDiagnosticCode.DuplicateId), Is.Zero);
            Assert.That(document.Index.GetDuplicateIdDiagnostics(), Is.Empty);
            Assert.That(document.Index.GetByClass("shared"), Is.EqualTo(new[] { group }));
            Assert.That(document.Diagnostics.Count(item => item.Code == SvgDiagnosticCode.DuplicateId), Is.Zero);
        });
    }

    [Test]
    public void ClassIndexUsesSvgWhitespaceAndIndexesEachElementOncePerToken()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = factory.CreateDocument();
        SvgNode root = factory.CreateElement("svg", "", "svg", SvgNamespaces.Svg);
        SvgAttribute classes = factory.CreateAttribute("class", "", "class", "", "one\ttwo\nthree\rfour\ffive one");
        mutations.AppendAttribute(root, classes);
        mutations.AppendChild(document.DocumentNode, root);

        Assert.Multiple(() =>
        {
            Assert.That(document.Index.GetByClass("one"), Is.EqualTo(new[] { root }));
            Assert.That(document.Index.GetByClass("two"), Is.EqualTo(new[] { root }));
            Assert.That(document.Index.GetByClass("three"), Is.EqualTo(new[] { root }));
            Assert.That(document.Index.GetByClass("four"), Is.EqualTo(new[] { root }));
            Assert.That(document.Index.GetByClass("five"), Is.EqualTo(new[] { root }));
            Assert.That(document.Index.GetByClass("one two"), Is.Empty);
        });
    }

    [Test]
    public void SourceIndexSupportsExactContainingAndOverlappingQueries()
    {
        SvgDocument document = Load("<svg><g><rect/></g><circle/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode rect = group.Children[0];
        SvgNode circle = root.Children[1];
        List<SvgNode> sourceOrder = document.Index.GetSourceOrderedNodes();

        Assert.Multiple(() =>
        {
            Assert.That(sourceOrder, Is.EqualTo(new[] { root, group, rect, circle }));
            Assert.That(document.Index.GetBySourceSpan(rect.SourceSpan), Is.EqualTo(new[] { rect }));
            Assert.That(document.Index.FindContainingSourceSpan(rect.SourceSpan), Is.EqualTo(new[] { root, group, rect }));
            Assert.That(document.Index.FindOverlappingSourceSpan(rect.SourceSpan), Is.EqualTo(new[] { root, group, rect }));
        });
    }

    [Test]
    public void SourceIndexOrdersNestedResourcesByStableResourcePath()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = factory.CreateDocument();
        SvgSourceUri source = new SvgSourceUri("root.svg");
        SvgSourceSpan firstReference = new SvgSourceSpan(source, 10, 1, 11, 15, 1, 16);
        SvgSourceSpan secondReference = new SvgSourceSpan(source, 20, 1, 21, 25, 1, 26);
        SvgSourceUri resourceZero = SvgSourceUri.CreateNested("zero.svg", source, firstReference, 0);
        SvgSourceUri resourceOne = SvgSourceUri.CreateNested("one.svg", source, secondReference, 1);
        SvgNode root = factory.CreateElement("svg", "", "svg", SvgNamespaces.Svg);
        SvgNode laterResource = factory.CreateElement("one", "", "one", "urn:test");
        SvgNode earlierResource = factory.CreateElement("zero", "", "zero", "urn:test");
        root.SourceSpan = new SvgSourceSpan(source, 0, 1, 1, 100, 1, 101);
        laterResource.SourceSpan = new SvgSourceSpan(resourceOne, 0, 1, 1, 5, 1, 6);
        earlierResource.SourceSpan = new SvgSourceSpan(resourceZero, 0, 1, 1, 5, 1, 6);
        mutations.AppendChild(document.DocumentNode, root);
        mutations.AppendChild(root, laterResource);
        mutations.AppendChild(root, earlierResource);

        Assert.That(document.Index.GetSourceOrderedNodes(), Is.EqualTo(new[] { root, earlierResource, laterResource }));
    }

    static SvgDocument Load(string input)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }
}
