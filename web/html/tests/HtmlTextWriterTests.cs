
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTextWriterTests
{
    [Test]
    public void CollapsesHtmlWhitespaceAcrossWritesAndTrimsEdges()
    {
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteText(" \talpha\r\n");
        writer.WriteText("  beta\f");
        writer.WriteText("\u2003gamma\u00A0delta  ");

        Assert.That(writer.Complete(),
            Is.EqualTo("alpha beta \u2003gamma\u00A0delta"));
    }

    [Test]
    public void BlockAndLineBreakRequestsCompactToConfiguredBlankLines()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetMaximumBlankLines(1);
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteBlockBoundary();
        writer.WriteText("first ");
        writer.WriteBlockBoundary();
        writer.WriteBlockBoundary();
        writer.WriteLineBreak();
        writer.WriteText(" second");
        writer.WriteBlockBoundary();

        Assert.That(writer.Complete(), Is.EqualTo("first\n\nsecond"));

        options.SetMaximumBlankLines(0);
        HtmlTextWriter compact = CreateWriter(options);
        compact.WriteText("first");
        compact.WriteBlockBoundary();
        compact.WriteLineBreak();
        compact.WriteText("second");

        Assert.That(compact.Complete(), Is.EqualTo("first\nsecond"));
    }

    [Test]
    public void PreformattedTextNormalizesLineEndingsWithoutCollapsingContent()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetNewLine("\r\n");
        HtmlTextWriter writer = CreateWriter(options);

        writer.WritePreformatted("  alpha\r\n\tbeta\rgamma\n  ");

        Assert.That(writer.Complete(),
            Is.EqualTo("alpha\r\n\tbeta\r\ngamma"));

        options.SetTrim(false);
        HtmlTextWriter untrimmed = CreateWriter(options);
        untrimmed.WriteText(" alpha ");

        Assert.That(untrimmed.Complete(), Is.EqualTo(" alpha "));
    }

    [Test]
    public void TraversalExcludesHiddenAndInertSubtrees()
    {
        HtmlNode fragment = ParseFragment(
            "visible "
            + "<span hidden>hidden</span> "
            + "<span inert>inert <b>nested</b></span> "
            + "<span aria-hidden=' TRUE '>aria</span> "
            + "<input type=' hidden ' value='secret'> "
            + "<span aria-hidden='false'>kept</span> "
            + "<svg><g hidden>foreign kept</g></svg>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(),
            Is.EqualTo("visible kept foreign kept"));
    }

    [Test]
    public void TraversalUsesLastMatchingRuleAndDefaultExclusions()
    {
        HtmlNode fragment = ParseFragment(
            "before <span class='skip'>ignored</span> "
            + "<span class='replace'>secret</span> "
            + "<script>included()</script> after");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.AddExcludedSelector(".skip, .replace");
        options.AddReplacementSelector(".replace", "[value]");
        options.AddIncludedSelector("script");
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(),
            Is.EqualTo("before [value] included() after"));
    }

    [Test]
    public void SemanticBlocksProduceCompactedParagraphBoundaries()
    {
        HtmlNode fragment = ParseFragment(
            "<main><h1> Title </h1><section>"
            + "<p>First <strong>paragraph</strong>.</p>"
            + "<div>Second</div></section>"
            + "<blockquote>Quoted</blockquote>"
            + "<address>Place</address></main>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "Title\n\nFirst paragraph.\n\nSecond\n\nQuoted\n\nPlace"));
    }

    [Test]
    public void BreaksThematicRulesAndPreformattedRegionsRemainDistinct()
    {
        HtmlNode fragment = ParseFragment(
            "<p>before <code>a   b</code><br>next</p><hr>"
            + "<pre>  one\r\n\ttwo\n</pre><p>after</p>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "before a b\nnext\n\n----------\n\n"
            + "  one\n\ttwo\n\nafter"));
    }

    [Test]
    public void ClosedDetailsExposeOnlySummaryWhileOpenDetailsExposeContent()
    {
        HtmlNode fragment = ParseFragment(
            "<details><span>before</span><summary>Closed</summary>"
            + "<p>secret</p><summary>other</summary></details>"
            + "<details open><summary>Open</summary><p>shown</p></details>"
            + "<figure>art<figcaption>Caption</figcaption></figure>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "Closed\n\nOpen\n\nshown\n\nart\nCaption"));
    }

    [Test]
    public void DefinitionListsUseLineBoundariesWithinTheList()
    {
        HtmlNode fragment = ParseFragment(
            "<dl><dt>Term one</dt><dd>Definition one</dd>"
            + "<dt>Term two</dt><dd><code>Definition   two</code></dd></dl>"
            + "<p>after</p>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "Term one\nDefinition one\nTerm two\nDefinition two\n\nafter"));
    }

    [Test]
    public void RubySubscriptAndSuperscriptRetainReadableSemantics()
    {
        HtmlNode fragment = ParseFragment(
            "<p>H<sub>2</sub>O and x<sup>2</sup>. "
            + "<ruby>漢<rp>(</rp><rt>kan</rt><rp>)</rp></ruby> "
            + "<ruby>語<rtc>go</rtc></ruby> "
            + "<ruby>字<rtc><rt>ji</rt><rt>letter</rt></rtc></ruby></p>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "H_2O and x^2. 漢 (kan) 語 (go) 字 (ji) (letter)"));
    }

    [Test]
    public void BidiElementsEmitUnicodeIsolationAndOverrideControls()
    {
        HtmlNode fragment = ParseFragment(
            "<p><bdi>auto</bdi> <bdi dir=rtl>rtl</bdi> "
            + "<bdo dir=ltr>abc</bdo> <span dir=auto>mix</span> "
            + "<span dir=invalid>plain</span></p>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "\u2068auto\u2069 \u2067rtl\u2069 "
            + "\u202Dabc\u202C \u2068mix\u2069 plain"));
    }

    [Test]
    public void UnorderedListsNestBulletsAndHangContinuationLines()
    {
        HtmlNode fragment = ParseFragment(
            "<ul><li>Alpha<br>continued<ul>"
            + "<li>Nested one</li><li>Nested two<br>tail</li>"
            + "</ul></li><li>Beta</li></ul>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "- Alpha\n  continued\n  * Nested one\n"
            + "  * Nested two\n    tail\n- Beta"));
    }

    [Test]
    public void OrderedListsHonorStartValueReversedAndType()
    {
        HtmlNode fragment = ParseFragment(
            "<ol start=3 type=A><li>three</li>"
            + "<li value=27>twenty-seven</li><li>next</li></ol>"
            + "<ol reversed type=i><li>third</li>"
            + "<li value=5>five</li><li>four</li></ol>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "C. three\nAA. twenty-seven\nAB. next\n\n"
            + "iii. third\nv. five\niv. four"));
    }

    [Test]
    public void LeadingCheckboxesProduceTaskMarkers()
    {
        HtmlNode fragment = ParseFragment(
            "<ul><li><input type=checkbox checked>Done</li>"
            + "<li><label><input type=checkbox> Todo</label></li>"
            + "<li>Normal <input type=checkbox></li></ul>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "- [x] Done\n- [ ] Todo\n- Normal"));
    }

    [Test]
    public void CustomBulletsCycleAndFormattingDepthIsCapped()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetUnorderedListBullets(["•", "◦"]);
        options.SetMaximumListDepth(2);
        HtmlNode fragment = ParseFragment(
            "<ul><li>one<ul><li>two<ul><li>three<ul>"
            + "<li>four</li></ul></li></ul></li></ul></li></ul>");
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "• one\n  ◦ two\n  ◦ three\n  ◦ four"));
    }

    [Test]
    public void ListRulesPreserveOwnedOrdinalsAndReplacementMarkers()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.AddExcludedSelector("li.skip");
        options.AddReplacementSelector("li.replace", "replacement");
        HtmlNode fragment = ParseFragment(
            "<ol><li class=skip>hidden</li><li>visible</li>"
            + "<li class=replace>secret</li></ol>");
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(),
            Is.EqualTo("2. visible\n3. replacement"));
    }

    [Test]
    public void LinkModesResolveSafeTargetsAgainstTheDocumentBase()
    {
        HtmlNode body = ParseBodyWithBase(
            "<p><a href=guide>Guide</a> "
            + "<a href='mailto:test@example.com'>Mail</a></p>",
            "https://example.test/docs/page.html");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
        HtmlTextWriter textAndUrl = CreateWriter(options);
        textAndUrl.WriteNode(body);

        options.SetLinkStyle(HtmlTextLinkStyle.UrlOnly);
        HtmlTextWriter urlOnly = CreateWriter(options);
        urlOnly.WriteNode(body);

        options.SetLinkStyle(HtmlTextLinkStyle.TextOnly);
        HtmlTextWriter textOnly = CreateWriter(options);
        textOnly.WriteNode(body);

        Assert.Multiple(() =>
        {
            Assert.That(textAndUrl.Complete(), Is.EqualTo(
                "Guide (https://example.test/docs/guide) "
                + "Mail (mailto:test@example.com)"));
            Assert.That(urlOnly.Complete(), Is.EqualTo(
                "https://example.test/docs/guide mailto:test@example.com"));
            Assert.That(textOnly.Complete(), Is.EqualTo("Guide Mail"));
        });
    }

    [Test]
    public void UnsafeAndCredentialBearingTargetsAreOmitted()
    {
        HtmlNode fragment = ParseFragment(
            "<p><a href='java&#x0A;script:alert(1)'>Script</a> "
            + "<a href='data:text/html,bad'>Data</a> "
            + "<a href='https://user:secret@example.test/'>Credentials</a> "
            + "<a href='https://safe.test/path'>Safe</a></p>");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "Script Data Credentials Safe (https://safe.test/path)"));
    }

    [Test]
    public void FootnoteLinksCoalesceResolvedDuplicateTargets()
    {
        HtmlNode body = ParseBodyWithBase(
            "<p><a href=guide>One</a> <a href='./guide'>Two</a> "
            + "<a href='tel:+15551234'>Call</a></p>",
            "https://example.test/docs/page.html");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetLinkStyle(HtmlTextLinkStyle.Footnotes);
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(body);
        string first = writer.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(
                "One[1] Two[1] Call[2]\n\n"
                + "[1] https://example.test/docs/guide\n"
                + "[2] tel:+15551234"));
            Assert.That(writer.Complete(), Is.EqualTo(first));
        });
    }

    [Test]
    public void ImageModesUseAltTitleSafeSourceAndDecorativeSemantics()
    {
        HtmlNode body = ParseBodyWithBase(
            "<p>Before <img alt=Diagram src=images/chart.png> "
            + "<img title=Titled src='javascript:bad'> "
            + "<img alt='' src=decorative.png> after</p>",
            "https://example.test/docs/page.html");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetImageStyle(HtmlTextImageStyle.AltTextAndUrl);
        HtmlTextWriter combined = CreateWriter(options);
        combined.WriteNode(body);

        options.SetImageStyle(HtmlTextImageStyle.UrlOnly);
        HtmlTextWriter urls = CreateWriter(options);
        urls.WriteNode(body);

        options.SetImageStyle(HtmlTextImageStyle.Placeholder);
        HtmlTextWriter placeholders = CreateWriter(options);
        placeholders.WriteNode(body);

        options.SetImageStyle(HtmlTextImageStyle.Omit);
        HtmlTextWriter omitted = CreateWriter(options);
        omitted.WriteNode(body);

        Assert.Multiple(() =>
        {
            Assert.That(combined.Complete(), Is.EqualTo(
                "Before Diagram "
                + "(https://example.test/docs/images/chart.png) "
                + "Titled after"));
            Assert.That(urls.Complete(), Is.EqualTo(
                "Before https://example.test/docs/images/chart.png "
                + "https://example.test/docs/decorative.png after"));
            Assert.That(placeholders.Complete(), Is.EqualTo(
                "Before [image] [image] [image] after"));
            Assert.That(omitted.Complete(), Is.EqualTo("Before after"));
        });
    }

    [Test]
    public void MediaAndFiguresUseTitlesSourcesAndSpecificPlaceholders()
    {
        HtmlNode body = ParseBodyWithBase(
            "<figure><img alt=Chart src=chart.png>"
            + "<figcaption>Quarterly results</figcaption></figure>"
            + "<p><video title=Demo><source src=movie.mp4></video> "
            + "<audio title=Sound src='javascript:bad'></audio> "
            + "<iframe title=Frame src='https://frame.test/'></iframe></p>",
            "https://example.test/media/page.html");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetImageStyle(HtmlTextImageStyle.AltTextAndUrl);
        HtmlTextWriter combined = CreateWriter(options);
        combined.WriteNode(body);

        options.SetImageStyle(HtmlTextImageStyle.Placeholder);
        HtmlTextWriter placeholders = CreateWriter(options);
        placeholders.WriteNode(body);

        Assert.Multiple(() =>
        {
            Assert.That(combined.Complete(), Is.EqualTo(
                "Chart (https://example.test/media/chart.png)\n"
                + "Quarterly results\n\n"
                + "Demo (https://example.test/media/movie.mp4) "
                + "Sound Frame (https://frame.test/)"));
            Assert.That(placeholders.Complete(), Is.EqualTo(
                "[image]\nQuarterly results\n\n"
                + "[video] [audio] [frame]"));
        });
    }

    [Test]
    public void FormControlExtractionIsOptInAndLabelsRemainByDefault()
    {
        HtmlNode fragment = ParseFragment(
            "<form><label>Name <input value=Alice></label> "
            + "<select><option selected>Choice</option></select> "
            + "<textarea>private note</textarea> <button>Go</button>"
            + "</form><p>after</p>");
        HtmlTextWriter writer = CreateWriter(HtmlTextOptions.CreateDefault());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo("Name\n\nafter"));
    }

    [Test]
    public void EnabledControlsPairExplicitWrappingAndAriaLabelsWithValues()
    {
        HtmlNode fragment = ParseFragment(
            "<form><label for=name>Name</label> "
            + "<input id=name value=Alice> "
            + "<input aria-label=Search value=query> "
            + "<label>Subscribe <input type=checkbox checked></label> "
            + "<label for=choice>Choice</label> "
            + "<input id=choice type=radio> "
            + "<button aria-label=Action>Save</button></form>");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetIncludeFormControls(true);
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "Name: Alice Search: query [x] Subscribe "
            + "( ) Choice Action: Save"));
    }

    [Test]
    public void SensitiveInputValuesAreNeverExtracted()
    {
        HtmlNode fragment = ParseFragment(
            "<form><label>Password <input type=password value=secret></label> "
            + "<label>File <input type=file value='/private/file'></label> "
            + "<label for=hidden>Hidden</label>"
            + "<input id=hidden type=hidden value=token> "
            + "<input value=public></form>");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetIncludeFormControls(true);
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(),
            Is.EqualTo("Password File Hidden public"));
    }

    [Test]
    public void SelectsUseOnlyExplicitSelectedMarkupAndTextareasUseSourceText()
    {
        HtmlNode fragment = ParseFragment(
            "<form><label>Colors <select multiple>"
            + "<option selected>Red</option><option>Green</option>"
            + "<optgroup><option label=Azure selected>Blue</option></optgroup>"
            + "</select></label> "
            + "<label>Empty <select><option>Default</option></select></label> "
            + "<label>Notes <textarea>hello\n world</textarea></label> "
            + "<label>Progress <meter value=0.5></meter></label></form>");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetIncludeFormControls(true);
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "Colors: Red, Azure Empty Notes: hello world Progress: 0.5"));
    }

    [Test]
    public void TaskCheckboxesAreNotDuplicatedWhenControlsAreEnabled()
    {
        HtmlNode fragment = ParseFragment(
            "<ul><li><input type=checkbox checked>Done</li>"
            + "<li><label><input type=checkbox> Todo</label></li></ul>");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetIncludeFormControls(true);
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(),
            Is.EqualTo("- [x] Done\n- [ ] Todo"));
    }

    [Test]
    public void WriterStopsAtSharedOutputAndVisitLimits()
    {
        HtmlLimits outputLimits = HtmlLimits.CreateDefault();
        outputLimits.SetMaximumOutputCharacters(4);
        HtmlTextOptions outputOptions = HtmlTextOptions.CreateDefault();
        outputOptions.SetLimits(outputLimits);
        HtmlCompiledTextOptions outputCompiled = outputOptions.Compile();
        HtmlTextOperationContext outputContext = new HtmlTextOperationContext(
            outputCompiled, CancellationToken.None, null);
        HtmlTextWriter outputWriter = new HtmlTextWriter(outputCompiled,
            outputContext, CancellationToken.None);

        outputWriter.WriteText("abcdef");

        Assert.Multiple(() =>
        {
            Assert.That(outputWriter.Complete(), Is.EqualTo("abcd"));
            Assert.That(outputContext.WasTruncated(), Is.True);
        });

        HtmlLimits visitLimits = HtmlLimits.CreateDefault();
        visitLimits.SetMaximumVisits(2);
        HtmlTextOptions visitOptions = HtmlTextOptions.CreateDefault();
        visitOptions.SetLimits(visitLimits);
        HtmlCompiledTextOptions visitCompiled = visitOptions.Compile();
        HtmlTextOperationContext visitContext = new HtmlTextOperationContext(
            visitCompiled, CancellationToken.None, null);
        HtmlTextWriter visitWriter = new HtmlTextWriter(visitCompiled,
            visitContext, CancellationToken.None);
        visitWriter.WriteNode(ParseFragment("<span>one</span><b>two</b>"));

        Assert.Multiple(() =>
        {
            Assert.That(visitWriter.Complete(), Is.Empty);
            Assert.That(visitContext.WasTruncated(), Is.True);
        });
    }

    [Test]
    public void WrappingUsesWordBoundariesAndPreservesLongTokens()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetWrapColumn(10);
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(ParseFragment(
            "<p>alpha beta gamma extraordinary delta</p>"));

        Assert.That(writer.Complete(), Is.EqualTo(
            "alpha beta\ngamma\nextraordinary\ndelta"));
    }

    [Test]
    public void WrappingCountsUnicodeDisplayClustersDeterministically()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetWrapColumn(6);
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(ParseFragment(
            "<p>猫猫 aa e\u0301e\u0301 bb 🙂🙂 x</p>"));

        Assert.That(writer.Complete(), Is.EqualTo(
            "猫猫\naa e\u0301e\u0301\nbb\n🙂🙂 x"));
    }

    [Test]
    public void WrappingPreservesHardBreaksAndPreformattedRegions()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetWrapColumn(6);
        options.SetNewLine("\r\n");
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(ParseFragment(
            "<p>one two<br>three four</p>"
            + "<pre>alpha beta gamma\nwide line</pre>"
            + "<p>end now</p>"));

        Assert.That(writer.Complete(), Is.EqualTo(
            "one\r\ntwo\r\nthree\r\nfour\r\n\r\n"
            + "alpha beta gamma\r\nwide line\r\n\r\nend\r\nnow"));
    }

    [Test]
    public void WrappedListItemsUseHangingIndentation()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetWrapColumn(10);
        HtmlTextWriter writer = CreateWriter(options);

        writer.WriteNode(ParseFragment(
            "<ul><li>alpha beta gamma</li>"
            + "<li>extraordinary token</li></ul>"));

        Assert.That(writer.Complete(), Is.EqualTo(
            "- alpha\n  beta\n  gamma\n-\n  extraordinary\n  token"));
    }

    static HtmlTextWriter CreateWriter(HtmlTextOptions options)
    {
        HtmlCompiledTextOptions compiled = options.Compile();
        HtmlTextOperationContext context = new HtmlTextOperationContext(
            compiled, CancellationToken.None, null);
        return new HtmlTextWriter(compiled, context, CancellationToken.None);
    }

    static HtmlNode ParseFragment(string html)
    {
        return Html.ParseFragment(html, "body").GetFragment();
    }

    static HtmlNode ParseBodyWithBase(string html, string baseUrl)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl(baseUrl);
        HtmlDocument document = Html.Parse(html, options,
            CancellationToken.None).GetDocument();
        return document.GetBody();
    }
}
