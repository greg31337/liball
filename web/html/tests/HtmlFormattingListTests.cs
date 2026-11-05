
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlFormattingListTests
{
    [Test]
    public void EntriesMarkersAndClearingPreserveChronologicalOrder()
    {
        HtmlDocument document = CreateDocument();
        HtmlFormattingList list = new HtmlFormattingList();
        HtmlToken boldToken = CreateToken("b", Array.Empty<HtmlAttribute>());
        HtmlNode bold = CreateElement(document, boldToken);
        HtmlToken italicToken = CreateToken("i", Array.Empty<HtmlAttribute>());
        HtmlNode italic = CreateElement(document, italicToken);

        list.Add(bold, boldToken);
        list.AddMarker();
        list.Add(italic, italicToken);
        HtmlFormattingEntry[] snapshot = list.GetEntries();
        snapshot[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(list.GetCount(), Is.EqualTo(3));
            Assert.That(list.GetElement(0), Is.SameAs(bold));
            Assert.That(list.GetToken(0), Is.SameAs(boldToken));
            Assert.That(list.IsMarker(1), Is.True);
            Assert.That(list.GetElement(1), Is.Null);
            Assert.That(list.GetToken(1), Is.Null);
            Assert.That(list.GetElement(2), Is.SameAs(italic));
            Assert.That(list.GetEntries()[0].GetElement(), Is.SameAs(bold));
            Assert.That(list.GetLastHtmlElement("i"), Is.SameAs(italic));
            Assert.That(list.GetLastHtmlElement("b"), Is.Null);
        });

        Assert.That(list.ClearToLastMarker(), Is.EqualTo(2));
        Assert.That(list.GetEntries().Select(value => value.GetElement()),
            Is.EqualTo(new[] { bold }));
        Assert.That(list.ClearToLastMarker(), Is.EqualTo(1));
        Assert.That(list.GetEntries(), Is.Empty);
    }

    [Test]
    public void NoahsArkClauseUsesOriginalParsedAttributesAndDropsEarliest()
    {
        HtmlDocument document = CreateDocument();
        HtmlFormattingList list = new HtmlFormattingList();
        HtmlAttribute attribute = CreateAttribute("class", "same");
        HtmlToken firstToken = CreateToken("b", new[] { attribute });
        HtmlNode first = CreateElement(document, firstToken);
        list.Add(first, firstToken);
        first.SetAttribute("class", "mutated after parsing");

        HtmlToken secondToken = CreateToken("b", new[]
        {
            CreateAttribute("class", "same")
        });
        HtmlToken thirdToken = CreateToken("b", new[]
        {
            CreateAttribute("class", "same")
        });
        HtmlToken fourthToken = CreateToken("b", new[]
        {
            CreateAttribute("class", "same")
        });
        HtmlNode second = CreateElement(document, secondToken);
        HtmlNode third = CreateElement(document, thirdToken);
        HtmlNode fourth = CreateElement(document, fourthToken);
        list.Add(second, secondToken);
        list.Add(third, thirdToken);
        list.Add(fourth, fourthToken);

        Assert.Multiple(() =>
        {
            Assert.That(list.GetCount(), Is.EqualTo(3));
            Assert.That(list.Contains(first), Is.False);
            Assert.That(list.GetEntries().Select(value => value.GetElement()),
                Is.EqualTo(new[] { second, third, fourth }));
        });

        list.AddMarker();
        HtmlToken afterMarkerToken = CreateToken("b", new[]
        {
            CreateAttribute("class", "same")
        });
        HtmlNode afterMarker = CreateElement(document, afterMarkerToken);
        list.Add(afterMarker, afterMarkerToken);
        Assert.That(list.GetCount(), Is.EqualTo(5));
    }

    [Test]
    public void NoahsArkAttributeComparisonIgnoresOrderButNotIdentityOrValue()
    {
        HtmlDocument document = CreateDocument();
        HtmlFormattingList list = new HtmlFormattingList();
        HtmlAttribute id = CreateAttribute("id", "one");
        HtmlAttribute language = HtmlAttribute.CreateNamespaced("xml:lang",
            "lang", "xml", "http://www.w3.org/XML/1998/namespace", "en",
            HtmlSourceSpan.CreateUnknown());
        HtmlToken firstToken = CreateToken("em", new[] { id, language });
        HtmlToken secondToken = CreateToken("em", new[] { language, id });
        HtmlToken thirdToken = CreateToken("em", new[]
        {
            CreateAttribute("id", "one"),
            HtmlAttribute.CreateNamespaced("xml:lang", "lang", "xml",
                "http://www.w3.org/XML/1998/namespace", "en",
                HtmlSourceSpan.CreateUnknown())
        });
        HtmlToken fourthToken = CreateToken("em", new[] { language, id });
        HtmlNode first = CreateElement(document, firstToken);
        HtmlNode second = CreateElement(document, secondToken);
        HtmlNode third = CreateElement(document, thirdToken);
        HtmlNode fourth = CreateElement(document, fourthToken);
        list.Add(first, firstToken);
        list.Add(second, secondToken);
        list.Add(third, thirdToken);
        list.Add(fourth, fourthToken);

        Assert.That(list.Contains(first), Is.False);
        Assert.That(list.GetCount(), Is.EqualTo(3));

        HtmlToken differentValueToken = CreateToken("em", new[]
        {
            CreateAttribute("id", "two"), language
        });
        list.Add(CreateElement(document, differentValueToken),
            differentValueToken);
        Assert.That(list.GetCount(), Is.EqualTo(4));
    }

    [Test]
    public void ListSupportsRemovalReplacementAndBookmarkInsertion()
    {
        HtmlDocument document = CreateDocument();
        HtmlFormattingList list = new HtmlFormattingList();
        HtmlToken boldToken = CreateToken("b", Array.Empty<HtmlAttribute>());
        HtmlToken italicToken = CreateToken("i", Array.Empty<HtmlAttribute>());
        HtmlNode bold = CreateElement(document, boldToken);
        HtmlNode italic = CreateElement(document, italicToken);
        HtmlNode replacement = CreateElement(document, boldToken);
        list.Add(bold, boldToken);
        list.Add(italic, italicToken);

        Assert.That(list.Replace(bold, replacement), Is.True);
        Assert.That(list.GetElement(0), Is.SameAs(replacement));
        Assert.That(list.GetToken(0), Is.SameAs(boldToken));
        Assert.That(list.Remove(italic), Is.True);
        Assert.That(list.Remove(italic), Is.False);
        list.InsertAt(0, italic, italicToken);

        Assert.Multiple(() =>
        {
            Assert.That(list.GetEntries().Select(value => value.GetElement()),
                Is.EqualTo(new[] { italic, replacement }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.GetElement(2));
            Assert.Throws<InvalidOperationException>(() => list.InsertAt(
                0, italic, italicToken));
            Assert.Throws<InvalidOperationException>(() => list.Add(
                italic, italicToken));
            Assert.Throws<ArgumentException>(() => list.Add(
                document.CreateElement("div"),
                CreateToken("div", Array.Empty<HtmlAttribute>())));
            Assert.Throws<ArgumentException>(() => list.Add(
                document.CreateElement("b"), italicToken));
        });
    }

    [Test]
    public void ReconstructionReopensMissingEntriesFromTheirOriginalTokens()
    {
        HtmlDocument document = CreateDocument();
        HtmlOpenElementStack stack = CreateStack(8);
        HtmlNode html = document.CreateElement("html");
        HtmlNode body = document.CreateElement("body");
        html.InitializeChildrenStorage(new[] { body });
        Assert.That(stack.Push(html), Is.True);
        Assert.That(stack.Push(body), Is.True);

        HtmlAttribute boldAttribute = CreateAttribute("class", "strong");
        HtmlToken boldToken = CreateToken("b", new[] { boldAttribute });
        HtmlToken italicToken = CreateToken("i", Array.Empty<HtmlAttribute>());
        HtmlNode oldBold = CreateElement(document, boldToken);
        HtmlNode oldItalic = CreateElement(document, italicToken);
        HtmlFormattingList list = new HtmlFormattingList();
        list.Add(oldBold, boldToken);
        list.Add(oldItalic, italicToken);
        List<HtmlNode> created = new List<HtmlNode>();

        bool result = list.Reconstruct(stack, token =>
        {
            HtmlNode element = CreateElement(document, token);
            stack.GetCurrentNode().Append(element);
            created.Add(element);
            return element;
        });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(created, Has.Count.EqualTo(2));
            Assert.That(list.GetElement(0), Is.SameAs(created[0]));
            Assert.That(list.GetElement(1), Is.SameAs(created[1]));
            Assert.That(list.GetToken(0), Is.SameAs(boldToken));
            Assert.That(stack.GetNodes(),
                Is.EqualTo(new[] { html, body, created[0], created[1] }));
            Assert.That(body.GetFirstChild(), Is.SameAs(created[0]));
            Assert.That(created[0].GetFirstChild(), Is.SameAs(created[1]));
            Assert.That(created[0].GetAttribute("class"), Is.EqualTo("strong"));
            Assert.That(oldBold.GetParent(), Is.Null);
            Assert.That(oldItalic.GetParent(), Is.Null);
        });

        int callbackCount = 0;
        Assert.That(list.Reconstruct(stack, token =>
        {
            callbackCount++;
            return CreateElement(document, token);
        }), Is.True);
        Assert.That(callbackCount, Is.Zero);
    }

    [Test]
    public void MarkerAndDepthLimitStopReconstructionWithoutCreatingElements()
    {
        HtmlDocument document = CreateDocument();
        HtmlOpenElementStack stack = CreateStack(2);
        Assert.That(stack.Push(document.CreateElement("html")), Is.True);
        Assert.That(stack.Push(document.CreateElement("body")), Is.True);
        HtmlToken token = CreateToken("b", Array.Empty<HtmlAttribute>());
        HtmlNode bold = CreateElement(document, token);
        HtmlFormattingList list = new HtmlFormattingList();
        list.Add(bold, token);
        int callbackCount = 0;

        Assert.That(list.Reconstruct(stack, value =>
        {
            callbackCount++;
            return CreateElement(document, value);
        }), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(callbackCount, Is.Zero);
            Assert.That(list.GetElement(0), Is.SameAs(bold));
            Assert.That(stack.GetCount(), Is.EqualTo(2));
            Assert.That(stack.HasExceededDepth(), Is.True);
        });

        HtmlFormattingList marked = new HtmlFormattingList();
        marked.Add(bold, token);
        marked.AddMarker();
        Assert.That(marked.Reconstruct(stack, value =>
        {
            callbackCount++;
            return CreateElement(document, value);
        }), Is.True);
        Assert.That(callbackCount, Is.Zero);
    }

    [Test]
    public void SynchronizationHelpersUpdateFormattingListAndOpenStackTogether()
    {
        HtmlDocument document = CreateDocument();
        HtmlOpenElementStack stack = CreateStack(8);
        HtmlNode html = document.CreateElement("html");
        HtmlToken boldToken = CreateToken("b", Array.Empty<HtmlAttribute>());
        HtmlToken italicToken = CreateToken("i", Array.Empty<HtmlAttribute>());
        HtmlNode bold = CreateElement(document, boldToken);
        HtmlNode italic = CreateElement(document, italicToken);
        PushAll(stack, new[] { html, bold, italic });
        HtmlFormattingList list = new HtmlFormattingList();
        list.Add(bold, boldToken);
        list.Add(italic, italicToken);
        HtmlNode replacement = CreateElement(document, boldToken);

        Assert.That(list.ReplaceInListAndStack(bold, replacement, stack),
            Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(list.Contains(bold), Is.False);
            Assert.That(list.Contains(replacement), Is.True);
            Assert.That(stack.Contains(bold), Is.False);
            Assert.That(stack.Contains(replacement), Is.True);
            Assert.That(list.GetToken(0), Is.SameAs(boldToken));
        });

        Assert.That(list.RemoveFromListAndStack(italic, stack), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(list.Contains(italic), Is.False);
            Assert.That(stack.Contains(italic), Is.False);
            Assert.That(list.RemoveFromListAndStack(italic, stack), Is.False);
        });
    }

    static HtmlOpenElementStack CreateStack(int maximumDepth)
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumDepth(maximumDepth);
        return new HtmlOpenElementStack(
            new HtmlBudget(limits, CancellationToken.None), null);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }

    static HtmlAttribute CreateAttribute(string name, string value)
    {
        return HtmlAttribute.Create(name, value, HtmlSourceSpan.CreateUnknown());
    }

    static HtmlToken CreateToken(string name, HtmlAttribute[] attributes)
    {
        return HtmlToken.CreateStartTag(name, attributes, false,
            HtmlSourceSpan.CreateUnknown());
    }

    static HtmlNode CreateElement(HtmlDocument document, HtmlToken token)
    {
        return document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", token.GetName(), token.GetName(),
            "", token.GetAttributes(), token.GetSourceSpan());
    }

    static void PushAll(HtmlOpenElementStack stack, HtmlNode[] nodes)
    {
        foreach (HtmlNode node in nodes)
        {
            Assert.That(stack.Push(node), Is.True);
        }
    }
}
