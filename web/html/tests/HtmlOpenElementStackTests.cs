
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlOpenElementStackTests
{
    [Test]
    public void StackOperationsExposeCurrentAdjustedAndSnapshotNodes()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode context = document.CreateElement("table");
        HtmlNode html = document.CreateElement("html");
        HtmlNode body = document.CreateElement("body");
        HtmlNode paragraph = document.CreateElement("p");
        HtmlOpenElementStack stack = CreateStack(8, context);

        Assert.That(stack.GetCurrentNode(), Is.Null);
        Assert.That(stack.GetAdjustedCurrentNode(), Is.Null);
        Assert.That(stack.Pop(), Is.Null);
        Assert.That(stack.Push(html), Is.True);
        Assert.That(stack.GetCurrentNode(), Is.SameAs(html));
        Assert.That(stack.GetAdjustedCurrentNode(), Is.SameAs(context));
        Assert.That(stack.Push(body), Is.True);
        Assert.That(stack.Push(paragraph), Is.True);

        HtmlNode[] snapshot = stack.GetNodes();
        snapshot[0] = paragraph;
        Assert.Multiple(() =>
        {
            Assert.That(stack.GetCount(), Is.EqualTo(3));
            Assert.That(stack.GetNode(0), Is.SameAs(html));
            Assert.That(stack.GetCurrentNode(), Is.SameAs(paragraph));
            Assert.That(stack.GetAdjustedCurrentNode(), Is.SameAs(paragraph));
            Assert.That(stack.Contains(body), Is.True);
            Assert.That(stack.GetIndex(paragraph), Is.EqualTo(2));
            Assert.That(stack.GetNodes(), Is.EqualTo(new[] { html, body, paragraph }));
        });

        HtmlNode replacement = document.CreateElement("section");
        HtmlNode inserted = document.CreateElement("div");
        Assert.That(stack.Replace(body, replacement), Is.True);
        Assert.That(stack.InsertAfter(replacement, inserted), Is.True);
        Assert.That(stack.Remove(replacement), Is.True);
        Assert.That(stack.PopUntil(inserted), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(stack.GetNodes(), Is.EqualTo(new[] { html }));
            Assert.That(stack.Replace(body, replacement), Is.False);
            Assert.That(stack.InsertAfter(body, replacement), Is.False);
            Assert.That(stack.Remove(body), Is.False);
            Assert.That(stack.PopUntil(body), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.GetNode(1));
            Assert.Throws<ArgumentException>(() => stack.Push(
                document.CreateText("not an element")));
            Assert.Throws<InvalidOperationException>(() => stack.Push(html));
            Assert.Throws<ArgumentException>(() => stack.HasInScope(
                document.CreateText("not an element")));
            Assert.Throws<ArgumentException>(() => CreateStack(8,
                document.CreateComment("not a context element")));
        });
    }

    [Test]
    public void StackGrowsAndCanPopThroughAnHtmlElementByName()
    {
        HtmlDocument document = CreateDocument();
        HtmlOpenElementStack stack = CreateStack(32, null);
        HtmlNode html = document.CreateElement("html");
        Assert.That(stack.Push(html), Is.True);
        int index = 0;
        while (index < 20)
        {
            Assert.That(stack.Push(document.CreateElement("div")), Is.True);
            index++;
        }

        Assert.That(stack.GetCount(), Is.EqualTo(21));
        Assert.That(stack.PopUntilHtmlElement("html"), Is.True);
        Assert.That(stack.GetCount(), Is.Zero);
        Assert.That(stack.PopUntilHtmlElement("body"), Is.False);
    }

    [Test]
    public void DepthLimitRejectsPushAndInsertionWithoutChangingTheStack()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode html = document.CreateElement("html");
        HtmlNode body = document.CreateElement("body");
        HtmlNode rejected = document.CreateElement("main");
        HtmlOpenElementStack stack = CreateStack(2, null);

        Assert.That(stack.Push(html), Is.True);
        Assert.That(stack.Push(body), Is.True);
        Assert.That(stack.Push(rejected), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(stack.GetNodes(), Is.EqualTo(new[] { html, body }));
            Assert.That(stack.HasExceededDepth(), Is.True);
        });

        Assert.That(stack.Pop(), Is.SameAs(body));
        Assert.That(stack.InsertAfter(html, rejected), Is.True);
        Assert.That(stack.GetNodes(), Is.EqualTo(new[] { html, rejected }));

        HtmlNode overflow = document.CreateElement("aside");
        Assert.That(stack.InsertAfter(html, overflow), Is.False);
        Assert.That(stack.GetNodes(), Is.EqualTo(new[] { html, rejected }));
    }

    [Test]
    public void OrdinaryScopeUsesHtmlMathMlAndSvgBoundaries()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode html = document.CreateElement("html");
        HtmlNode target = document.CreateElement("p");
        HtmlNode htmlMi = document.CreateElement("mi");
        HtmlNode mathMi = CreateForeign(document,
            "http://www.w3.org/1998/Math/MathML", "mi");
        HtmlNode span = document.CreateElement("span");
        HtmlOpenElementStack stack = CreateStack(12, null);
        PushAll(stack, new[] { html, target, htmlMi, mathMi, span });

        Assert.Multiple(() =>
        {
            Assert.That(stack.HasInScope(span), Is.True);
            Assert.That(stack.HasInScope(mathMi), Is.True);
            Assert.That(stack.HasInScope(target), Is.False);
            Assert.That(stack.HasHtmlElementInScope("p"), Is.False);
            Assert.That(stack.HasHtmlElementInScope("mi"), Is.False);
        });

        HtmlNode svgTitle = CreateForeign(document,
            "http://www.w3.org/2000/svg", "title");
        stack = CreateStack(12, null);
        PushAll(stack, new[] { html, target, svgTitle, span });
        Assert.That(stack.HasInScope(target), Is.False);

        HtmlNode otherTitle = CreateForeign(document, "urn:example", "title");
        stack = CreateStack(12, null);
        PushAll(stack, new[] { html, target, otherTitle, span });
        Assert.That(stack.HasInScope(target), Is.True);
    }

    [Test]
    public void ScopeVariantsStopAtTheirAdditionalBoundaries()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode html = document.CreateElement("html");
        HtmlNode listTarget = document.CreateElement("li");
        HtmlNode list = document.CreateElement("ul");
        HtmlNode paragraph = document.CreateElement("p");
        HtmlOpenElementStack stack = CreateStack(16, null);
        PushAll(stack, new[] { html, listTarget, list, paragraph });
        Assert.That(stack.HasInScope(listTarget), Is.True);
        Assert.That(stack.HasInListItemScope(listTarget), Is.False);

        HtmlNode button = document.CreateElement("button");
        stack = CreateStack(16, null);
        PushAll(stack, new[] { html, paragraph, button,
            document.CreateElement("span") });
        Assert.That(stack.HasHtmlElementInScope("p"), Is.True);
        Assert.That(stack.HasHtmlElementInButtonScope("p"), Is.False);

        HtmlNode table = document.CreateElement("table");
        HtmlNode tbody = document.CreateElement("tbody");
        stack = CreateStack(16, null);
        PushAll(stack, new[] { html, paragraph, table, tbody });
        Assert.That(stack.HasInTableScope(table), Is.True);
        Assert.That(stack.HasHtmlElementInTableScope("p"), Is.False);

        HtmlNode select = document.CreateElement("select");
        HtmlNode optgroup = document.CreateElement("optgroup");
        HtmlNode option = document.CreateElement("option");
        stack = CreateStack(16, null);
        PushAll(stack, new[] { html, select, optgroup, option });
        Assert.That(stack.HasInSelectScope(option), Is.True);
        Assert.That(stack.HasInSelectScope(optgroup), Is.True);
        Assert.That(stack.HasInSelectScope(select), Is.True);

        stack = CreateStack(16, null);
        PushAll(stack, new[]
        {
            html, select, document.CreateElement("span"), option
        });
        Assert.That(stack.HasInSelectScope(select), Is.False);
    }

    [Test]
    public void SpecialElementLookupIsNamespaceSensitiveAndOrdered()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode html = document.CreateElement("html");
        HtmlNode formatting = document.CreateElement("b");
        HtmlNode span = document.CreateElement("span");
        HtmlNode mathMi = CreateForeign(document,
            "http://www.w3.org/1998/Math/MathML", "mi");
        HtmlNode div = document.CreateElement("div");
        HtmlNode svgDesc = CreateForeign(document,
            "http://www.w3.org/2000/svg", "desc");
        HtmlOpenElementStack stack = CreateStack(16, null);
        PushAll(stack, new[]
        {
            html, formatting, span, mathMi, div, svgDesc
        });

        Assert.Multiple(() =>
        {
            Assert.That(HtmlOpenElementStack.IsSpecialElement(span), Is.False);
            Assert.That(HtmlOpenElementStack.IsSpecialElement(
                document.CreateElement("mi")), Is.False);
            Assert.That(HtmlOpenElementStack.IsSpecialElement(mathMi), Is.True);
            Assert.That(HtmlOpenElementStack.IsSpecialElement(div), Is.True);
            Assert.That(HtmlOpenElementStack.IsSpecialElement(svgDesc), Is.True);
            Assert.That(stack.GetFirstSpecialElementAfter(formatting),
                Is.SameAs(mathMi));
            Assert.That(stack.GetLastSpecialElement(), Is.SameAs(svgDesc));
            Assert.That(stack.GetLastHtmlElement("div"), Is.SameAs(div));
        });
    }

    [Test]
    public void ImpliedEndTagsPopOnlyCurrentEligibleHtmlElements()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode html = document.CreateElement("html");
        HtmlNode body = document.CreateElement("body");
        HtmlNode paragraph = document.CreateElement("p");
        HtmlNode listItem = document.CreateElement("li");
        HtmlNode option = document.CreateElement("option");
        HtmlOpenElementStack stack = CreateStack(16, null);
        PushAll(stack, new[] { html, body, paragraph, listItem, option });

        Assert.That(stack.GenerateImpliedEndTags(), Is.EqualTo(3));
        Assert.That(stack.GetNodes(), Is.EqualTo(new[] { html, body }));

        stack = CreateStack(16, null);
        PushAll(stack, new[] { html, body, paragraph, listItem, option });
        Assert.That(stack.GenerateImpliedEndTagsExcept("li"), Is.EqualTo(1));
        Assert.That(stack.GetCurrentNode(), Is.SameAs(listItem));

        HtmlNode foreignParagraph = CreateForeign(document,
            "http://www.w3.org/2000/svg", "p");
        stack = CreateStack(16, null);
        PushAll(stack, new[] { html, paragraph, foreignParagraph });
        Assert.That(stack.GenerateImpliedEndTags(), Is.Zero);
        Assert.That(stack.GetCurrentNode(), Is.SameAs(foreignParagraph));
    }

    [Test]
    public void ThoroughImpliedEndTagsAlsoCloseTableStructureElements()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode html = document.CreateElement("html");
        HtmlNode body = document.CreateElement("body");
        HtmlNode tbody = document.CreateElement("tbody");
        HtmlNode row = document.CreateElement("tr");
        HtmlNode cell = document.CreateElement("td");
        HtmlNode paragraph = document.CreateElement("p");
        HtmlOpenElementStack stack = CreateStack(16, null);
        PushAll(stack, new[] { html, body, tbody, row, cell, paragraph });

        Assert.That(stack.GenerateAllImpliedEndTagsThoroughly(), Is.EqualTo(4));
        Assert.That(stack.GetNodes(), Is.EqualTo(new[] { html, body }));
    }

    static HtmlOpenElementStack CreateStack(int maximumDepth,
        HtmlNode context)
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumDepth(maximumDepth);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        return new HtmlOpenElementStack(budget, context);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }

    static HtmlNode CreateForeign(HtmlDocument document, string namespaceUri,
        string localName)
    {
        return document.CreateElementInNamespace(namespaceUri, localName);
    }

    static void PushAll(HtmlOpenElementStack stack, HtmlNode[] nodes)
    {
        foreach (HtmlNode node in nodes)
        {
            Assert.That(stack.Push(node), Is.True);
        }
    }
}
