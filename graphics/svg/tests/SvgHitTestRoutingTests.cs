namespace svg.Tests;

public class SvgHitTestRoutingTests
{
    [Test]
    public void HitRouteRunsTargetFirstThroughResolvedAncestors()
    {
        SvgResolvedDocument document = Resolve("<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><g id='group'><rect id='target' width='10' height='10'/></g></svg>");

        SvgDocumentHitTestResult result = document.HitTest(new SvgPoint(5, 5));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Topmost.Route.Select(item => Id(item.SourceNode)), Is.EqualTo(new[] { "target", "group", "" }));
            Assert.That(result.Topmost.Route[0].Node, Is.SameAs(result.Topmost.Node));
            Assert.That(result.Topmost.Route.Last().Node, Is.SameAs(document.Root));
            Assert.That(result.Topmost.Route.All(item => item.InstancePath.Length == 0), Is.True);
        });
    }

    [Test]
    public void UseInstanceRoutePreservesSourceAndInstanceIdentity()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='30' height='20'><defs><g id='art'><rect id='shape' width='6' height='6'/></g></defs><use id='copy' href='#art' x='10'/></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgHitTestMatch match = document.HitTest(new SvgPoint(12, 2)).Topmost;

        Assert.Multiple(() =>
        {
            Assert.That(match, Is.Not.Null);
            Assert.That(match.IsInstance, Is.True);
            Assert.That(Id(match.SourceNode), Is.EqualTo("shape"));
            Assert.That(match.Route[0].SourceNode, Is.SameAs(match.SourceNode));
            Assert.That(match.Route[0].InstancePath, Is.EqualTo(match.InstancePath));
            Assert.That(match.Route.Any(item => item.IsInstance && Id(item.SourceNode) == "art"), Is.True);
            Assert.That(match.Route.Any(item => Id(item.SourceNode) == "copy"), Is.True);
            Assert.That(match.Route.Last().Node, Is.SameAs(document.Root));
        });
    }

    [Test]
    public void TopmostHitExposesInheritedKeywordAndCustomCursorFallbacks()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='30' height='20'><g cursor='url(cursor.cur) 4 5, crosshair'><rect id='custom' width='10' height='10'/></g><rect id='automatic' x='15' width='10' height='10'/></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgDocumentHitTestResult custom = document.HitTest(new SvgPoint(5, 5));
        SvgDocumentHitTestResult automatic = document.HitTest(new SvgPoint(20, 5));
        SvgDocumentHitTestResult miss = document.HitTest(new SvgPoint(29, 19));

        Assert.Multiple(() =>
        {
            Assert.That(custom.Cursor, Is.SameAs(custom.Topmost.Cursor));
            Assert.That(custom.Cursor.Keyword, Is.EqualTo("crosshair"));
            Assert.That(custom.Cursor.Urls.Select(item => item.Value), Is.EqualTo(new[] { "cursor.cur" }));
            Assert.That(custom.Cursor.CssText, Is.EqualTo("url(cursor.cur) 4 5, crosshair"));
            Assert.That(custom.Cursor.SourceNode, Is.SameAs(custom.Topmost.SourceNode));
            Assert.That(automatic.Cursor.Keyword, Is.EqualTo("auto"));
            Assert.That(automatic.Cursor.Urls, Is.Empty);
            Assert.That(miss.Cursor, Is.Null);
        });
    }

    [Test]
    public void AllHitsRetainTheirOwnCursorInTopmostFirstOrder()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><rect id='bottom' width='10' height='10' cursor='move'/><rect id='top' width='10' height='10' cursor='pointer'/></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgDocumentHitTestResult result = document.HitTestAll(new SvgPoint(5, 5));

        Assert.Multiple(() =>
        {
            Assert.That(result.Matches.Select(item => Id(item.SourceNode)), Is.EqualTo(new[] { "top", "bottom" }));
            Assert.That(result.Matches.Select(item => item.Cursor.Keyword), Is.EqualTo(new[] { "pointer", "move" }));
            Assert.That(result.Cursor, Is.SameAs(result.Matches[0].Cursor));
        });
    }

    static SvgResolvedDocument Resolve(string source)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
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
}
