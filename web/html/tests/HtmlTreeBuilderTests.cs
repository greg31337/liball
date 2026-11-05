
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeBuilderTests
{
    [Test]
    public void DispatcherSupportsReprocessingAndTemporaryModeRules()
    {
        HtmlTreeBuilder builder = CreateBuilder(CreateDocument(),
            HtmlLimits.CreateDefault(), null, null);
        List<string> calls = new List<string>();
        builder.SetModeHandler(HtmlInsertionMode.Initial, (current, token) =>
        {
            calls.Add("initial");
            current.SetInsertionMode(HtmlInsertionMode.BeforeHtml);
            return HtmlTreeBuilderAction.Reprocess();
        });
        builder.SetModeHandler(HtmlInsertionMode.BeforeHtml, (current, token) =>
        {
            calls.Add("before-html");
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        });
        builder.SetModeHandler(HtmlInsertionMode.InBody, (current, token) =>
        {
            calls.Add("in-body");
            return HtmlTreeBuilderAction.Consume();
        });

        bool processed = builder.ProcessToken(CreateStartTag("p", 0, 3));

        Assert.Multiple(() =>
        {
            Assert.That(processed, Is.True);
            Assert.That(calls, Is.EqualTo(new[]
            {
                "initial", "before-html", "in-body"
            }));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.BeforeHtml));
            Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(3));
            Assert.That(builder.DidLastDispatchUseForeignRules(), Is.False);
        });
    }

    [Test]
    public void DispatcherStopsNonConvergingReprocessingWithDiagnostic()
    {
        HtmlTreeBuilder builder = CreateBuilder(CreateDocument(),
            HtmlLimits.CreateDefault(), null, null);
        builder.SetModeHandler(HtmlInsertionMode.Initial, (current, token) =>
            HtmlTreeBuilderAction.Reprocess());
        HtmlToken token = CreateStartTag("p", 4, 7);

        Assert.That(builder.ProcessToken(token), Is.False);
        HtmlDiagnostic diagnostic = builder.GetDiagnostics().Single();
        Assert.Multiple(() =>
        {
            Assert.That(builder.HasTerminalLimitExceeded(), Is.True);
            Assert.That(builder.GetLastDispatchCount(),
                Is.EqualTo(HtmlTreeBuilder.MaximumTokenDispatches + 1));
            Assert.That(diagnostic.GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.OperationIncomplete));
            Assert.That(diagnostic.GetSpan(), Is.SameAs(token.GetSourceSpan()));
        });
    }

    [Test]
    public void CancellationBetweenTokenReprocessingStopsBeforeNextRule()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = new HtmlTreeBuilder(document,
            new HtmlBudget(HtmlLimits.CreateDefault(), source.Token), null,
            null);
        List<string> calls = new List<string>();
        builder.SetModeHandler(HtmlInsertionMode.Initial, (current, token) =>
        {
            calls.Add("initial");
            current.SetInsertionMode(HtmlInsertionMode.BeforeHtml);
            source.Cancel();
            return HtmlTreeBuilderAction.Reprocess();
        });
        builder.SetModeHandler(HtmlInsertionMode.BeforeHtml,
            (current, token) =>
            {
                calls.Add("before-html");
                return HtmlTreeBuilderAction.Consume();
            });

        Assert.Throws<OperationCanceledException>(() =>
            builder.ProcessToken(CreateStartTag("p", 0, 3)));
        Assert.That(calls, Is.EqualTo(new[] { "initial" }));
        Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(1));
    }

    [Test]
    public void DispatcherSelectsHtmlAndForeignRulesAtIntegrationPoints()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document,
            HtmlLimits.CreateDefault(), null, null);
        int htmlCalls = 0;
        int foreignCalls = 0;
        builder.SetModeHandler(HtmlInsertionMode.Initial, (current, token) =>
        {
            htmlCalls++;
            return HtmlTreeBuilderAction.Consume();
        });
        builder.SetForeignHandler((current, token) =>
        {
            foreignCalls++;
            return HtmlTreeBuilderAction.Consume();
        });
        HtmlNode mathMi = CreateForeign(document, HtmlNamespace.MathMl, "mi",
            Array.Empty<HtmlAttribute>());
        Assert.That(builder.GetOpenElements().Push(mathMi), Is.True);

        Assert.That(builder.ProcessToken(CreateStartTag("span", 0, 1)), Is.True);
        Assert.That(builder.ProcessToken(CreateStartTag("mglyph", 1, 2)), Is.True);
        Assert.That(builder.ProcessToken(CreateCharacter("x", 2, 3)), Is.True);
        Assert.That(builder.ProcessToken(CreateEndTag("mi", 3, 4)), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(htmlCalls, Is.EqualTo(2));
            Assert.That(foreignCalls, Is.EqualTo(2));
            Assert.That(builder.ShouldUseHtmlRules(
                HtmlToken.CreateEndOfFile(CreateSpan(4, 4))), Is.True);
        });

        HtmlAttribute encoding = HtmlAttribute.Create("encoding", "TEXT/HTML",
            CreateSpan(5, 6));
        HtmlNode annotation = CreateForeign(document, HtmlNamespace.MathMl,
            "annotation-xml", new[] { encoding });
        builder.GetOpenElements().Pop();
        Assert.That(builder.GetOpenElements().Push(annotation), Is.True);
        Assert.That(builder.ShouldUseHtmlRules(
            CreateStartTag("svg", 6, 7)), Is.True);
        Assert.That(builder.ShouldUseHtmlRules(
            CreateCharacter("x", 7, 8)), Is.True);
        Assert.That(builder.ShouldUseHtmlRules(
            CreateEndTag("annotation-xml", 8, 9)), Is.False);
    }

    [Test]
    public void ParseErrorsUseTokenSpansAndHonorTheDiagnosticBudget()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumDiagnostics(1);
        HtmlTreeBuilder builder = CreateBuilder(CreateDocument(), limits,
            null, null);
        HtmlToken first = CreateStartTag("first", 10, 15);
        HtmlToken second = CreateEndTag("second", 16, 22);

        Assert.That(builder.ReportParseError(first, "first error"), Is.True);
        Assert.That(builder.ReportParseError(second, "second error"), Is.False);

        HtmlDiagnostic diagnostic = builder.GetDiagnostics().Single();
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.TreeBuilderUnexpectedToken));
            Assert.That(diagnostic.GetSeverity(),
                Is.EqualTo(HtmlDiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Is.EqualTo("first error"));
            Assert.That(diagnostic.GetSpan(), Is.SameAs(first.GetSourceSpan()));
            Assert.That(builder.WereDiagnosticsTruncated(), Is.True);
            Assert.That(builder.GetDroppedDiagnosticCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void NodeCreationPreservesTokenDataAndMarksGeneratedSourcesUnknown()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document,
            HtmlLimits.CreateDefault(), null, null);
        HtmlAttribute attribute = HtmlAttribute.Create("id", "main",
            CreateSpan(1, 4));
        HtmlToken token = HtmlToken.CreateStartTag("section",
            new[] { attribute }, false, CreateSpan(0, 10));
        HtmlNode element = builder.CreateHtmlElement(token);
        HtmlNode generated = builder.CreateGeneratedHtmlElement("body");
        HtmlNode template = builder.CreateHtmlElement(
            CreateStartTag("template", 11, 21));
        HtmlToken foreignToken = CreateStartTag("circle", 22, 30);
        HtmlNode foreign = builder.CreateElement(foreignToken,
            HtmlNamespace.Svg, "http://www.w3.org/2000/svg", "circle",
            "circle", "");

        Assert.Multiple(() =>
        {
            Assert.That(element.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Html));
            Assert.That(element.GetAttribute("id"), Is.EqualTo("main"));
            Assert.That(element.GetSourceSpan(), Is.SameAs(token.GetSourceSpan()));
            Assert.That(generated.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(template.GetTemplateContent(), Is.Not.Null);
            Assert.That(foreign.GetNamespaceStorage(), Is.EqualTo(HtmlNamespace.Svg));
            Assert.That(foreign.GetSourceSpan(),
                Is.SameAs(foreignToken.GetSourceSpan()));
        });
    }

    [Test]
    public void SharedInsertionCoalescesTextAndPreservesParserSourceState()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document,
            HtmlLimits.CreateDefault(), null, null);
        HtmlNode html = builder.InsertHtmlElement(
            CreateStartTag("html", 0, 6));
        HtmlNode body = builder.InsertHtmlElement(
            CreateStartTag("body", 6, 12));
        HtmlNode firstText = builder.InsertCharacter(
            CreateCharacter("hello", 12, 17));
        HtmlNode secondText = builder.InsertCharacter(
            CreateCharacter(" world", 17, 23));
        HtmlToken commentToken = HtmlToken.CreateComment("note",
            CreateSpan(23, 34));
        HtmlNode comment = builder.InsertComment(commentToken);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetRoot().GetFirstChild(), Is.SameAs(html));
            Assert.That(html.GetFirstChild(), Is.SameAs(body));
            Assert.That(firstText, Is.SameAs(secondText));
            Assert.That(firstText.GetData(), Is.EqualTo("hello world"));
            Assert.That(firstText.GetSourceSpan().GetStart()
                .GetNormalizedCharacterOffset(), Is.EqualTo(12));
            Assert.That(firstText.GetSourceSpan().GetEnd()
                .GetNormalizedCharacterOffset(), Is.EqualTo(23));
            Assert.That(comment.GetData(), Is.EqualTo("note"));
            Assert.That(comment.GetSourceSpan(),
                Is.SameAs(commentToken.GetSourceSpan()));
            Assert.That(body.GetChildren(), Is.EqualTo(new[]
            {
                firstText, comment
            }));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    [Test]
    public void FosterParentingInsertsBeforeTheLastParentedTable()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document,
            HtmlLimits.CreateDefault(), null, null);
        HtmlNode html = document.CreateElement("html");
        HtmlNode body = document.CreateElement("body");
        HtmlNode before = document.CreateText("before");
        HtmlNode table = document.CreateElement("table");
        body.InitializeChildrenStorage(new[] { before, table });
        html.InitializeChildrenStorage(new[] { body });
        document.GetRoot().InitializeChildrenStorage(new[] { html });
        PushAll(builder.GetOpenElements(), new[] { html, body, table });
        builder.SetFosterParenting(true);

        HtmlInsertionLocation location = builder.GetAdjustedInsertionLocation();
        HtmlNode inserted = builder.InsertCharacter(
            CreateCharacter(" fostered", 30, 39));

        Assert.Multiple(() =>
        {
            Assert.That(location.GetParent(), Is.SameAs(body));
            Assert.That(location.GetBeforeChild(), Is.SameAs(table));
            Assert.That(inserted, Is.SameAs(before));
            Assert.That(before.GetData(), Is.EqualTo("before fostered"));
            Assert.That(body.GetChildren(), Is.EqualTo(new[] { before, table }));
        });
    }

    [Test]
    public void FosterParentingPrefersANewerTemplateAndHandlesDetachedTables()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document,
            HtmlLimits.CreateDefault(), null, null);
        HtmlNode html = document.CreateElement("html");
        HtmlNode table = document.CreateElement("table");
        HtmlNode template = document.CreateElement("template");
        PushAll(builder.GetOpenElements(), new[] { html, table, template });
        builder.SetFosterParenting(true);

        Assert.That(builder.GetAdjustedInsertionLocation().GetParent(),
            Is.SameAs(template.GetTemplateContent()));

        builder.GetOpenElements().Pop();
        HtmlInsertionLocation detached = builder.GetAdjustedInsertionLocation();
        Assert.Multiple(() =>
        {
            Assert.That(detached.GetParent(), Is.SameAs(html));
            Assert.That(detached.GetBeforeChild(), Is.Null);
        });
    }

    [Test]
    public void TemplateAndRootInsertionTargetsRedirectAdjustedLocations()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode context = document.CreateElement("div");
        HtmlTreeBuilder builder = CreateBuilder(document,
            HtmlLimits.CreateDefault(), context, fragment);
        HtmlNode html = document.CreateElement("html");
        Assert.That(builder.GetOpenElements().Push(html), Is.True);

        Assert.That(builder.GetAdjustedInsertionLocation().GetParent(),
            Is.SameAs(fragment));

        HtmlNode template = document.CreateElement("template");
        Assert.That(builder.GetOpenElements().Push(template), Is.True);
        Assert.That(builder.GetAdjustedInsertionLocation().GetParent(),
            Is.SameAs(template.GetTemplateContent()));
    }

    [Test]
    public void NodeAndDepthLimitsStopBeforeCreatingRejectedElements()
    {
        HtmlLimits depthLimits = HtmlLimits.CreateDefault();
        depthLimits.SetMaximumDepth(1);
        HtmlDocument depthDocument = CreateDocument();
        HtmlTreeBuilder depthBuilder = CreateBuilder(depthDocument,
            depthLimits, null, null);
        Assert.That(depthBuilder.InsertHtmlElement(
            CreateStartTag("html", 0, 1)), Is.Not.Null);
        Assert.That(depthBuilder.InsertHtmlElement(
            CreateStartTag("body", 1, 2)), Is.Null);

        Assert.Multiple(() =>
        {
            Assert.That(depthBuilder.GetOpenElements().GetCount(), Is.EqualTo(1));
            Assert.That(depthDocument.GetDocumentElement().GetChildren(), Is.Empty);
            Assert.That(depthBuilder.GetDiagnostics().Single().GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.TreeBuilderDepthExceeded));
        });

        HtmlLimits nodeLimits = HtmlLimits.CreateDefault();
        nodeLimits.SetMaximumNodes(2);
        HtmlDocument nodeDocument = CreateDocument();
        HtmlTreeBuilder nodeBuilder = CreateBuilder(nodeDocument,
            nodeLimits, null, null);
        Assert.That(nodeBuilder.InsertHtmlElement(
            CreateStartTag("html", 0, 1)), Is.Not.Null);
        Assert.That(nodeBuilder.InsertHtmlElement(
            CreateStartTag("body", 1, 2)), Is.Null);
        Assert.That(nodeBuilder.GetDiagnostics().Single().GetCode(),
            Is.EqualTo(HtmlDiagnosticCode.ResourceLimitExceeded));
    }

    [Test]
    public void DoctypeAndCommentCanBeAttributedDirectlyToTheDocument()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document,
            HtmlLimits.CreateDefault(), null, null);
        HtmlToken commentToken = HtmlToken.CreateComment("before",
            CreateSpan(0, 9));
        HtmlToken doctypeToken = HtmlToken.CreateDoctype("html", false,
            CreateSpan(9, 24));
        HtmlNode comment = builder.InsertCommentAtTarget(commentToken,
            document.GetRoot());
        HtmlNode doctype = builder.InsertDoctype(doctypeToken);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetRoot().GetChildren(),
                Is.EqualTo(new[] { comment, doctype }));
            Assert.That(comment.GetSourceSpan(),
                Is.SameAs(commentToken.GetSourceSpan()));
            Assert.That(doctype.GetSourceSpan(),
                Is.SameAs(doctypeToken.GetSourceSpan()));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    [Test]
    public void InvalidDocumentInsertionLocationsDropNodesWithoutCorruptingTree()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document,
            HtmlLimits.CreateDefault(), null, null);
        HtmlNode html = builder.CreateHtmlElement(
            CreateStartTag("html", 0, 1));
        HtmlInsertionLocation end = new HtmlInsertionLocation(
            document.GetRoot(), null);
        Assert.That(builder.InsertNodeAtLocation(html, end), Is.True);
        HtmlNode duplicate = builder.CreateHtmlElement(
            CreateStartTag("html", 1, 2));
        HtmlNode lateDoctype = builder.CreateDoctype(
            HtmlToken.CreateDoctype("html", false, CreateSpan(2, 3)));

        Assert.Multiple(() =>
        {
            Assert.That(builder.InsertNodeAtLocation(duplicate, end), Is.False);
            Assert.That(builder.InsertNodeAtLocation(lateDoctype, end), Is.False);
            Assert.That(document.GetRoot().GetChildren(),
                Is.EqualTo(new[] { html }));
            Assert.That(duplicate.GetParent(), Is.Null);
            Assert.That(lateDoctype.GetParent(), Is.Null);
        });
    }

    static HtmlTreeBuilder CreateBuilder(HtmlDocument document,
        HtmlLimits limits, HtmlNode context, HtmlNode rootInsertionTarget)
    {
        return new HtmlTreeBuilder(document,
            new HtmlBudget(limits, CancellationToken.None), context,
            rootInsertionTarget);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }

    static HtmlNode CreateForeign(HtmlDocument document,
        HtmlNamespace namespaceKind, string localName,
        HtmlAttribute[] attributes)
    {
        return document.CreateElementStorage(namespaceKind,
            HtmlNamespaceData.GetUri(namespaceKind), localName, localName, "",
            attributes, HtmlSourceSpan.CreateUnknown());
    }

    static HtmlToken CreateStartTag(string name, int start, int end)
    {
        return HtmlToken.CreateStartTag(name, Array.Empty<HtmlAttribute>(),
            false, CreateSpan(start, end));
    }

    static HtmlToken CreateEndTag(string name, int start, int end)
    {
        return HtmlToken.CreateEndTag(name, Array.Empty<HtmlAttribute>(),
            false, CreateSpan(start, end));
    }

    static HtmlToken CreateCharacter(string data, int start, int end)
    {
        return HtmlToken.CreateCharacter(data, CreateSpan(start, end));
    }

    static HtmlSourceSpan CreateSpan(int start, int end)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(start, 1, start + 1),
            HtmlSourcePosition.Create(end, 1, end + 1));
    }

    static void PushAll(HtmlOpenElementStack stack, HtmlNode[] nodes)
    {
        foreach (HtmlNode node in nodes)
        {
            Assert.That(stack.Push(node), Is.True);
        }
    }
}
