namespace svg.Tests;

public class SvgAccessibilityTreeTests
{
    [Test]
    public void LogicalTreeFlattensPresentationAndOmitsHiddenSubtrees()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' id='root'><g id='flat' role='presentation'><rect id='first' aria-label='First'/></g><circle id='hidden' aria-hidden='true'/><text id='text'>Hello<tspan id='span'> world</tspan></text><a id='link' href='next.svg'/></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgAccessibilityTree tree = document.BuildAccessibilityTree();

        Assert.Multiple(() =>
        {
            Assert.That(tree.Success, Is.True, Messages(tree));
            Assert.That(tree.Nodes.Select(item => Id(item.SourceNode)), Is.EqualTo(new[] { "root", "first", "text", "span", "link" }));
            Assert.That(tree.ReadingOrder.Select(item => item.ReadingOrder), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(tree.Find(document.SourceDocument.Index.GetById("flat"), ""), Is.Null);
            Assert.That(tree.Find(document.SourceDocument.Index.GetById("hidden"), ""), Is.Null);
            Assert.That(Find(tree, "first").Parent, Is.SameAs(tree.Root));
            Assert.That(Find(tree, "span").Parent, Is.SameAs(Find(tree, "text")));
            Assert.That(Find(tree, "first").Semantics.Name, Is.EqualTo("First"));
        });
    }

    [Test]
    public void GeometryIsWorldSpaceAndUnionsIntoAccessibleAncestors()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' id='root' width='100' height='100'><g id='group' transform='translate(10 20)'><rect id='box' x='1' y='2' width='4' height='3'/></g></svg>";
        SvgAccessibilityTree tree = Resolve(source).BuildAccessibilityTree();

        SvgAccessibilityTreeNode box = Find(tree, "box");
        SvgAccessibilityTreeNode group = Find(tree, "group");

        Assert.Multiple(() =>
        {
            Assert.That(tree.Success, Is.True, Messages(tree));
            AssertRect(box.Bounds, 11, 22, 4, 3);
            AssertRect(group.Bounds, 11, 22, 4, 3);
            AssertRect(tree.Root.Bounds, 11, 22, 4, 3);
            Assert.That(box.HasGeometry, Is.True);
            Assert.That(group.HasGeometry, Is.True);
        });
    }

    [Test]
    public void FocusabilityAndSequentialOrderReuseNavigationSemantics()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' id='root'><rect id='zero' tabindex='0'/><a id='link' href='next.svg'/><circle id='two' tabindex='2'/><path id='one' tabindex='1'/><rect id='negative' tabindex='-1'/></svg>";
        SvgAccessibilityTree tree = Resolve(source).BuildAccessibilityTree();

        Assert.Multiple(() =>
        {
            Assert.That(tree.Success, Is.True, Messages(tree));
            Assert.That(tree.FocusOrder.Select(item => Id(item.SourceNode)), Is.EqualTo(new[] { "one", "two", "zero", "link" }));
            Assert.That(Find(tree, "link").Focusable, Is.True);
            Assert.That(Find(tree, "link").HasTabIndex, Is.False);
            Assert.That(Find(tree, "two").TabIndex, Is.EqualTo(2));
            Assert.That(Find(tree, "negative").Focusable, Is.True);
            Assert.That(Find(tree, "negative").Sequential, Is.False);
        });
    }

    [Test]
    public void LogicalTextRangesRetainOffsetsOwnersAndSubtreeSpans()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' id='root'><text id='text' x='10' y='20'>  Hello <tspan id='span'> world  </tspan></text></svg>";
        SvgAccessibilityTree tree = Resolve(source).BuildAccessibilityTree();
        SvgAccessibilityTreeNode text = Find(tree, "text");
        SvgAccessibilityTreeNode span = Find(tree, "span");

        Assert.Multiple(() =>
        {
            Assert.That(tree.Success, Is.True, Messages(tree));
            Assert.That(tree.Text, Is.EqualTo("Hello world"));
            Assert.That(tree.TextRanges.Select(item => item.Text), Is.EqualTo(new[] { "Hello", "world" }));
            Assert.That(tree.TextRanges.Select(item => item.Start), Is.EqualTo(new[] { 0, 6 }));
            Assert.That(tree.TextRanges.Select(item => item.Length), Is.EqualTo(new[] { 5, 5 }));
            Assert.That(tree.TextRanges[0].Owner, Is.SameAs(text));
            Assert.That(tree.TextRanges[1].Owner, Is.SameAs(span));
            Assert.That(text.TextStart, Is.Zero);
            Assert.That(text.TextLength, Is.EqualTo(11));
            Assert.That(span.TextStart, Is.EqualTo(6));
            Assert.That(span.TextLength, Is.EqualTo(5));
            Assert.That(text.HasGeometry, Is.True);
            Assert.That(tree.Root.TextStart, Is.Zero);
            Assert.That(tree.Root.TextLength, Is.EqualTo(11));
        });
    }

    [Test]
    public void UseInstancesRetainAuthoredAndGeneratedAssociations()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' id='root'><defs><g id='art'><rect id='box' width='3' height='4' aria-label='Box'/></g></defs><use id='copy' href='#art' x='20' y='10'/></svg>";
        SvgResolvedDocument document = Resolve(source);
        SvgNode boxSource = document.SourceDocument.Index.GetById("box");

        SvgAccessibilityTree tree = document.BuildAccessibilityTree();
        SvgAccessibilityTreeNode instance = tree.Find(boxSource, "0/1/instance/0/0");

        Assert.Multiple(() =>
        {
            Assert.That(tree.Success, Is.True, Messages(tree));
            Assert.That(tree.Find(boxSource, ""), Is.Null);
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.IsInstance, Is.True);
            Assert.That(instance.SourceNode, Is.SameAs(boxSource));
            Assert.That(instance.ResolvedNode.Instance, Is.Not.Null);
            Assert.That(instance.InstancePath, Is.EqualTo("0/1/instance/0/0"));
            Assert.That(instance.Semantics.Name, Is.EqualTo("Box"));
            AssertRect(instance.Bounds, 20, 10, 3, 4);
        });
    }

    [Test]
    public void HiddenFocusableObjectsDoNotLeakIntoFocusOrder()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' id='root'><rect id='hidden' tabindex='0' aria-hidden='true'/><rect id='visible' tabindex='0'/></svg>";
        SvgAccessibilityTree tree = Resolve(source).BuildAccessibilityTree();

        Assert.Multiple(() =>
        {
            Assert.That(tree.Success, Is.True, Messages(tree));
            Assert.That(tree.Find(tree.SourceDocument.Index.GetById("hidden"), ""), Is.Null);
            Assert.That(tree.FocusOrder.Select(item => Id(item.SourceNode)), Is.EqualTo(new[] { "visible" }));
        });
    }

    [Test]
    public void NodeAndTextLimitsReturnStableBoundedFailures()
    {
        SvgResolvedDocument document = Resolve("<svg xmlns='http://www.w3.org/2000/svg'><text>Long text</text><rect/></svg>");
        SvgAccessibilityTreeOptions nodeOptions = new SvgAccessibilityTreeOptions();
        nodeOptions.MaximumNodes = 1;
        SvgAccessibilityTreeOptions textOptions = new SvgAccessibilityTreeOptions();
        textOptions.MaximumTextCharacters = 4;

        SvgAccessibilityTree nodes = document.BuildAccessibilityTreeWithOptions(nodeOptions);
        SvgAccessibilityTree text = document.BuildAccessibilityTreeWithOptions(textOptions);

        Assert.Multiple(() =>
        {
            Assert.That(nodes.Success, Is.False);
            Assert.That(nodes.Error, Is.EqualTo(SvgAccessibilityTreeError.LimitExceeded));
            Assert.That(nodes.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
            Assert.That(text.Success, Is.False);
            Assert.That(text.Error, Is.EqualTo(SvgAccessibilityTreeError.LimitExceeded));
            Assert.That(text.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
        });
    }

    [Test]
    public void TreeSnapshotsBecomeStaleAfterAuthoredMutation()
    {
        SvgResolvedDocument document = Resolve("<svg xmlns='http://www.w3.org/2000/svg'><rect id='box'/></svg>");
        SvgAccessibilityTree tree = document.BuildAccessibilityTree();
        SvgNode box = document.SourceDocument.Index.GetById("box");

        SvgMutationResult mutation = new SvgNodeApi().AppendAttribute(box, new SvgAttribute("aria-label", "", "aria-label", "", "Changed"));

        Assert.Multiple(() =>
        {
            Assert.That(tree.Success, Is.True);
            Assert.That(mutation.Success, Is.True);
            Assert.That(tree.IsCurrent(), Is.False);
            Assert.That(Find(tree, "box").IsCurrent(), Is.False);
        });
    }

    static SvgAccessibilityTreeNode Find(SvgAccessibilityTree tree, string id)
    {
        SvgNode source = tree.SourceDocument.Index.GetById(id);
        Assert.That(source, Is.Not.Null);
        SvgAccessibilityTreeNode result = tree.Find(source, "");
        Assert.That(result, Is.Not.Null);
        return result;
    }

    static SvgResolvedDocument Resolve(string source)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = "https://example.test/image.svg";
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(loaded.Success, Is.True);
        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(loaded.Document);
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result;
    }

    static string Id(SvgNode node)
    {
        if (node == null) return "";
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == "id") return attribute.Value;
            index = index + 1;
        }
        return "";
    }

    static string Messages(SvgAccessibilityTree tree)
    {
        return string.Join(" | ", tree.Diagnostics.Select(item => item.Message));
    }

    static void AssertRect(SvgRect rectangle, double x, double y, double width, double height)
    {
        Assert.That(rectangle, Is.Not.Null);
        Assert.That(rectangle.IsEmpty, Is.False);
        Assert.That(rectangle.X, Is.EqualTo(x).Within(0.000000001));
        Assert.That(rectangle.Y, Is.EqualTo(y).Within(0.000000001));
        Assert.That(rectangle.Width, Is.EqualTo(width).Within(0.000000001));
        Assert.That(rectangle.Height, Is.EqualTo(height).Within(0.000000001));
    }
}
