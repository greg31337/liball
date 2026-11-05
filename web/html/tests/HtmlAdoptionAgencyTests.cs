
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlAdoptionAgencyTests
{
    [TestCase("a")]
    [TestCase("b")]
    [TestCase("big")]
    [TestCase("code")]
    [TestCase("em")]
    [TestCase("font")]
    [TestCase("i")]
    [TestCase("nobr")]
    [TestCase("s")]
    [TestCase("small")]
    [TestCase("strike")]
    [TestCase("strong")]
    [TestCase("tt")]
    [TestCase("u")]
    public void EveryFormattingSubjectRepairsCrossedInlineEndTags(
        string subject)
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);
        string nestedName = subject == "i" ? "b" : "i";
        HtmlToken subjectToken = Start(subject, 30, 35);
        HtmlToken nestedToken = Start(nestedName, 35, 40);

        Process(builder, new[]
        {
            subjectToken,
            nestedToken,
            Character("one", 40, 43),
            End(subject, 43, 50),
            Character("two", 50, 53),
            End(nestedName, 53, 60),
            Character("three", 60, 65)
        });

        HtmlNode[] children = body.GetChildren();
        HtmlNode originalSubject = children[0];
        HtmlNode reconstructedNested = children[1];
        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(3));
            Assert.That(originalSubject.GetLocalName(), Is.EqualTo(subject));
            Assert.That(originalSubject.GetFirstChild().GetLocalName(),
                Is.EqualTo(nestedName));
            Assert.That(originalSubject.GetTextContent(), Is.EqualTo("one"));
            Assert.That(reconstructedNested.GetLocalName(),
                Is.EqualTo(nestedName));
            Assert.That(reconstructedNested.GetTextContent(),
                Is.EqualTo("two"));
            Assert.That(reconstructedNested.GetSourceSpan(),
                Is.SameAs(nestedToken.GetSourceSpan()));
            Assert.That(children[2].GetData(), Is.EqualTo("three"));
            Assert.That(builder.GetFormattingElements().GetCount(), Is.Zero);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [TestCase("p")]
    [TestCase("button")]
    [TestCase("div")]
    public void FurthestBlockIsAdoptedIntoAReplacementFormattingElement(
        string blockName)
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);
        HtmlToken anchorToken = Start("a", 30, 33);

        Process(builder, new[]
        {
            anchorToken,
            Character("one", 33, 36),
            Start(blockName, 36, 45),
            Character("two", 45, 48),
            End("a", 48, 52),
            Character("three", 52, 57),
            End(blockName, 57, 68)
        });

        HtmlNode originalAnchor = body.GetFirstChild();
        HtmlNode block = body.GetLastChild();
        HtmlNode replacementAnchor = block.GetFirstChild();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetChildElements().Select(node =>
                node.GetLocalName()), Is.EqualTo(new[] { "a", blockName }));
            Assert.That(originalAnchor.GetTextContent(), Is.EqualTo("one"));
            Assert.That(replacementAnchor.GetLocalName(), Is.EqualTo("a"));
            Assert.That(replacementAnchor.GetTextContent(), Is.EqualTo("two"));
            Assert.That(replacementAnchor.GetSourceSpan(),
                Is.SameAs(anchorToken.GetSourceSpan()));
            Assert.That(block.GetLastChild().GetData(), Is.EqualTo("three"));
            Assert.That(builder.GetFormattingElements().GetCount(), Is.Zero);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    [Test]
    public void RepeatedFurthestBlocksAreRepairedAcrossOuterIterations()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, new[]
        {
            Start("a", 30, 33),
            Character("one", 33, 36),
            Start("div", 36, 41),
            Character("two", 41, 44),
            Start("div", 44, 49),
            Character("three", 49, 54),
            End("a", 54, 58),
            Character("four", 58, 62),
            End("div", 62, 68),
            Character("five", 68, 72),
            End("div", 72, 78)
        });

        HtmlNode originalAnchor = body.GetFirstChild();
        HtmlNode outerDiv = body.GetLastChild();
        HtmlNode outerReplacement = outerDiv.GetFirstChild();
        HtmlNode innerDiv = outerDiv.GetChildElements()[1];
        HtmlNode innerReplacement = innerDiv.GetFirstChild();
        Assert.Multiple(() =>
        {
            Assert.That(originalAnchor.GetTextContent(), Is.EqualTo("one"));
            Assert.That(outerReplacement.GetLocalName(), Is.EqualTo("a"));
            Assert.That(outerReplacement.GetTextContent(), Is.EqualTo("two"));
            Assert.That(innerReplacement.GetLocalName(), Is.EqualTo("a"));
            Assert.That(innerReplacement.GetTextContent(),
                Is.EqualTo("three"));
            Assert.That(innerDiv.GetLastChild().GetData(), Is.EqualTo("four"));
            Assert.That(outerDiv.GetLastChild().GetData(), Is.EqualTo("five"));
            Assert.That(builder.GetFormattingElements().GetCount(), Is.Zero);
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void FourthInnerFormattingEntryIsPrunedWhileThreeAreRecreated()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        EnterBody(builder, document);

        Process(builder, new[]
        {
            Start("div", 30, 35),
            Start("a", 35, 38),
            Start("b", 38, 41),
            Start("u", 41, 44),
            Start("i", 44, 47),
            Start("code", 47, 53),
            Start("div", 53, 58),
            End("a", 58, 62)
        });

        HtmlFormattingList formatting = builder.GetFormattingElements();
        Assert.Multiple(() =>
        {
            Assert.That(HtmlAdoptionAgency.MaximumInnerActiveEntries,
                Is.EqualTo(3));
            Assert.That(formatting.GetEntries().Select(entry =>
                entry.GetElement().GetLocalName()),
                Is.EqualTo(new[] { "u", "i", "code" }));
            Assert.That(builder.GetOpenElements().GetLastHtmlElement("b"),
                Is.Null);
            Assert.That(builder.GetOpenElements().GetCurrentNode()
                .GetLocalName(), Is.EqualTo("div"));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    [Test]
    public void OuterLoopStopsAfterEightRepairsOnAdversarialNesting()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        EnterBody(builder, document);
        List<HtmlToken> tokens = new List<HtmlToken>
        {
            Start("div", 30, 35),
            Start("a", 35, 38),
            Start("b", 38, 41)
        };
        int index = 0;
        while (index < 10)
        {
            tokens.Add(Start("div", 41 + index * 5, 46 + index * 5));
            index++;
        }

        tokens.Add(End("a", 91, 95));
        Process(builder, tokens.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(HtmlAdoptionAgency.MaximumOuterIterations,
                Is.EqualTo(8));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(8));
            Assert.That(builder.HasTerminalLimitExceeded(), Is.False);
            Assert.That(builder.GetFormattingElements()
                .GetLastHtmlElement("a"), Is.Not.Null);
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    [Test]
    public void MissingOrDetachedFormattingEntriesFollowStandardFallbacks()
    {
        HtmlDocument missingDocument = CreateDocument();
        HtmlTreeBuilder missing = CreateBuilder(missingDocument);
        HtmlNode missingBody = EnterBody(missing, missingDocument);
        Assert.That(missing.ProcessToken(Start("b", 30, 33)), Is.True);
        HtmlNode bold = missing.GetOpenElements().GetCurrentNode();
        Assert.That(missing.GetFormattingElements().Remove(bold), Is.True);
        Assert.That(missing.ProcessToken(End("b", 33, 37)), Is.True);
        Assert.That(missing.GetOpenElements().GetCurrentNode(),
            Is.SameAs(missingBody));

        HtmlDocument detachedDocument = CreateDocument();
        HtmlTreeBuilder detached = CreateBuilder(detachedDocument);
        EnterBody(detached, detachedDocument);
        Assert.That(detached.ProcessToken(Start("i", 30, 33)), Is.True);
        HtmlNode italic = detached.GetOpenElements().Pop();
        Assert.That(detached.GetFormattingElements().Contains(italic), Is.True);
        Assert.That(detached.ProcessToken(End("i", 33, 37)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(detached.GetFormattingElements().Contains(italic),
                Is.False);
            Assert.That(detached.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void AdoptionAgencyElementCreationHonorsNodeLimits()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumNodes(8);
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = new HtmlTreeBuilder(document,
            new HtmlBudget(limits, CancellationToken.None), null, null);
        EnterBody(builder, document);
        Process(builder, new[]
        {
            Start("a", 30, 33),
            Start("p", 33, 36),
            Character("text", 36, 40)
        });

        Assert.That(builder.ProcessToken(End("a", 40, 44)), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(builder.HasTerminalLimitExceeded(), Is.True);
            Assert.That(builder.GetDiagnostics().Last().GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.ResourceLimitExceeded));
            Assert.That(builder.GetDiagnostics().Last().GetSpan()
                .GetStart().GetNormalizedCharacterOffset(), Is.EqualTo(30));
        });
    }

    static HtmlNode EnterBody(HtmlTreeBuilder builder, HtmlDocument document)
    {
        Process(builder, new[]
        {
            HtmlToken.CreateDoctype("html", false, Span(0, 15)),
            Start("html", 15, 21),
            Start("head", 21, 27),
            End("head", 27, 34),
            Start("body", 34, 40)
        });
        return document.GetBody();
    }

    static void Process(HtmlTreeBuilder builder, HtmlToken[] tokens)
    {
        foreach (HtmlToken token in tokens)
        {
            Assert.That(builder.ProcessToken(token), Is.True);
        }
    }

    static HtmlTreeBuilder CreateBuilder(HtmlDocument document)
    {
        return new HtmlTreeBuilder(document,
            new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None),
            null, null);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }

    static HtmlToken Start(string name, int start, int end)
    {
        return HtmlToken.CreateStartTag(name, Array.Empty<HtmlAttribute>(),
            false, Span(start, end));
    }

    static HtmlToken End(string name, int start, int end)
    {
        return HtmlToken.CreateEndTag(name, Array.Empty<HtmlAttribute>(),
            false, Span(start, end));
    }

    static HtmlToken Character(string data, int start, int end)
    {
        return HtmlToken.CreateCharacter(data, Span(start, end));
    }

    static HtmlSourceSpan Span(int start, int end)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(start, 1, start + 1),
            HtmlSourcePosition.Create(end, 1, end + 1));
    }
}
