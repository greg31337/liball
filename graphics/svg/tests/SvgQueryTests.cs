namespace svg.Tests;

public class SvgQueryTests
{
    [Test]
    public void IndexedQueryHelpersReturnIndependentDocumentOrderedResults()
    {
        SvgQuery query = new SvgQuery();
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:x='urn:x' id='root' class='common'><g class='common shape'/><rect class='shape'/><x:item/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode rect = root.Children[1];
        SvgNode foreign = root.Children[2];

        List<SvgNode> common = query.GetByClass(document, "common");
        List<SvgNode> shapes = query.GetByClass(document, "shape");

        Assert.Multiple(() =>
        {
            Assert.That(query.GetById(document, "root"), Is.SameAs(root));
            Assert.That(query.GetById(document, "missing"), Is.Null);
            Assert.That(common, Is.EqualTo(new[] { root, group }));
            Assert.That(shapes, Is.EqualTo(new[] { group, rect }));
            Assert.That(query.GetByQualifiedName(document, "x:item"), Is.EqualTo(new[] { foreign }));
            Assert.That(query.GetByNamespace(document, SvgNamespaces.Svg), Is.EqualTo(new[] { root, group, rect }));
            Assert.That(query.GetByNamespace(document, "urn:x"), Is.EqualTo(new[] { foreign }));
            Assert.That(query.GetByExpandedName(document, SvgNamespaces.Svg, "rect"), Is.EqualTo(new[] { rect }));
        });

        common.Clear();
        shapes.Reverse();

        Assert.Multiple(() =>
        {
            Assert.That(query.GetByClass(document, "common"), Is.EqualTo(new[] { root, group }));
            Assert.That(query.GetByClass(document, "shape"), Is.EqualTo(new[] { group, rect }));
        });
    }

    [Test]
    public void DocumentPredicateQueriesUsePreorderWithoutSyntheticDocumentNode()
    {
        SvgQuery query = new SvgQuery();
        SvgDocument document = Load("<svg>text<g><!--note--><rect/></g><circle/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode text = root.Children[0];
        SvgNode group = root.Children[1];
        SvgNode comment = group.Children[0];
        SvgNode rect = group.Children[1];
        SvgNode circle = root.Children[2];
        List<SvgNode> callbackOrder = new List<SvgNode>();

        List<SvgNode> elements = query.FindAll(document, node => node.Kind == SvgNodeKind.Element);
        SvgNode firstComment = query.FindFirst(document, node => node.Kind == SvgNodeKind.Comment);
        int count = query.Traverse(document, node => callbackOrder.Add(node));

        Assert.Multiple(() =>
        {
            Assert.That(elements, Is.EqualTo(new[] { root, group, rect, circle }));
            Assert.That(firstComment, Is.SameAs(comment));
            Assert.That(callbackOrder, Is.EqualTo(new[] { root, text, group, comment, rect, circle }));
            Assert.That(callbackOrder, Does.Not.Contain(document.DocumentNode));
            Assert.That(count, Is.EqualTo(6));
        });
    }

    [Test]
    public void NodePredicateQueriesIncludeTheScopeAndExcludeOutsideBranches()
    {
        SvgQuery query = new SvgQuery();
        SvgDocument document = Load("<svg><rect/><g><path/><circle/></g><ellipse/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[1];
        SvgNode path = group.Children[0];
        SvgNode circle = group.Children[1];
        List<SvgNode> visited = new List<SvgNode>();

        List<SvgNode> elements = query.FindAllFromNode(group, node => node.Kind == SvgNodeKind.Element);
        SvgNode first = query.FindFirstFromNode(group, node => node.LocalName == "path");
        int count = query.TraverseFromNode(group, node => visited.Add(node));

        Assert.Multiple(() =>
        {
            Assert.That(elements, Is.EqualTo(new[] { group, path, circle }));
            Assert.That(first, Is.SameAs(path));
            Assert.That(visited, Is.EqualTo(new[] { group, path, circle }));
            Assert.That(count, Is.EqualTo(3));
            Assert.That(visited, Does.Not.Contain(root.Children[0]));
            Assert.That(visited, Does.Not.Contain(root.Children[2]));
        });
    }

    [Test]
    public void TraversalSnapshotIsStableWhenVisitorEditsTheLiveTree()
    {
        SvgQuery query = new SvgQuery();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocument document = Load("<svg><rect/><circle/><path/></svg>");
        SvgNode root = document.DocumentElement;
        SvgNode first = root.Children[0];
        SvgNode removed = root.Children[1];
        SvgNode last = root.Children[2];
        List<SvgNode> visited = new List<SvgNode>();

        int count = query.Traverse(document, node =>
        {
            visited.Add(node);
            if (ReferenceEquals(node, first)) mutations.RemoveChild(root, removed);
        });

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(4));
            Assert.That(visited, Is.EqualTo(new[] { root, first, removed, last }));
            Assert.That(root.Children, Is.EqualTo(new[] { first, last }));
            Assert.That(removed.Parent, Is.Null);
            Assert.That(removed.OwnerDocument, Is.Null);
            Assert.That(document.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public void SourceQueriesConvertSourceIndexOrderBackToDocumentOrder()
    {
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNodeApi mutations = new SvgNodeApi();
        SvgQuery query = new SvgQuery();
        SvgDocument document = factory.CreateDocument();
        SvgSourceUri source = new SvgSourceUri("memory:query");
        SvgNode root = factory.CreateElement("svg", "", "svg", SvgNamespaces.Svg);
        SvgNode laterInSource = factory.CreateElement("rect", "", "rect", SvgNamespaces.Svg);
        SvgNode earlierInSource = factory.CreateElement("circle", "", "circle", SvgNamespaces.Svg);
        root.SourceSpan = new SvgSourceSpan(source, 0, 1, 1, 100, 1, 101);
        laterInSource.SourceSpan = new SvgSourceSpan(source, 50, 1, 51, 60, 1, 61);
        earlierInSource.SourceSpan = new SvgSourceSpan(source, 10, 1, 11, 20, 1, 21);
        mutations.AppendChild(document.DocumentNode, root);
        mutations.AppendChild(root, laterInSource);
        mutations.AppendChild(root, earlierInSource);
        SvgSourceSpan whole = new SvgSourceSpan(source, 0, 1, 1, 100, 1, 101);

        Assert.Multiple(() =>
        {
            Assert.That(document.Index.FindOverlappingSourceSpan(whole), Is.EqualTo(new[] { root, earlierInSource, laterInSource }));
            Assert.That(query.FindOverlappingSourceSpan(document, whole), Is.EqualTo(new[] { root, laterInSource, earlierInSource }));
            Assert.That(query.GetBySourceSpan(document, laterInSource.SourceSpan), Is.EqualTo(new[] { laterInSource }));
            Assert.That(query.FindContainingSourceSpan(document, earlierInSource.SourceSpan), Is.EqualTo(new[] { root, earlierInSource }));
        });
    }

    [Test]
    public void QueryCallbacksAndRequiredScopesRejectNull()
    {
        SvgQuery query = new SvgQuery();
        SvgDocument document = Load("<svg/>");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => query.GetById(null, "id"));
            Assert.Throws<ArgumentNullException>(() => query.FindFirst(document, null));
            Assert.Throws<ArgumentNullException>(() => query.FindAllFromNode((SvgNode)null, node => true));
            Assert.Throws<ArgumentNullException>(() => query.Traverse(document, null));
        });
    }

    static SvgDocument Load(string input)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }
}
