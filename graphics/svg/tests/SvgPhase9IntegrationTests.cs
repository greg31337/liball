namespace svg.Tests;

public class SvgPhase9IntegrationTests
{
    [Test]
    public void InjectedEventsDriveDeterministicFixedTimeInterpolation()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='box'><animate attributeName='opacity' values='0;10' begin='box.click+1s' dur='2s' fill='freeze'/></rect></svg>";
        SvgDocument document = Load(source);
        SvgNode target = document.SourceDocumentNode("box");
        SvgAnimationStateOptions options = new SvgAnimationStateOptions();
        options.Timeline.Events.InjectEvent("box", "click", 2);
        SvgAnimationState state = SvgAnimationState.Create(document, null, options);
        state.SetBaseValue(new SvgAnimationBaseValue(target, SvgAnimationTargetKind.CssProperty, "opacity", SvgAnimationValue.FromNumber(2)));

        SvgAnimationSample before = state.Sample(2.5);
        SvgAnimationSample middle = state.Sample(4);
        SvgAnimationSample repeated = state.Sample(4);
        SvgAnimationSample frozen = state.Sample(6);

        Assert.Multiple(() =>
        {
            Assert.That(before.Success, Is.True, Message(before));
            Assert.That(Value(before, target, SvgAnimationTargetKind.CssProperty, "opacity").AnimatedValue.Number, Is.EqualTo(2));
            Assert.That(Value(before, target, SvgAnimationTargetKind.CssProperty, "opacity").HasAnimation, Is.False);
            Assert.That(middle.Success, Is.True, Message(middle));
            Assert.That(Value(middle, target, SvgAnimationTargetKind.CssProperty, "opacity").AnimatedValue.Number, Is.EqualTo(5).Within(0.00001));
            Assert.That(middle.DirtySourceNodes, Is.EqualTo(new[] { target }));
            Assert.That(repeated.DirtySourceNodes, Is.Empty);
            Assert.That(Value(frozen, target, SvgAnimationTargetKind.CssProperty, "opacity").AnimatedValue.Number, Is.EqualTo(10));
            Assert.That(Value(frozen, target, SvgAnimationTargetKind.CssProperty, "opacity").HasAnimation, Is.True);
            Assert.That(options.Timeline.Events.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public void MotionSamplingComposesAPathTransformAtCallerTime()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='mover'><animateMotion path='M0 0L100 0' dur='2s' rotate='auto'/></rect></svg>";
        SvgDocument document = Load(source);
        SvgNode target = document.SourceDocumentNode("mover");
        SvgAnimationState state = new SvgAnimationState(document);
        SvgTransformList identity = new SvgTransformList(new List<SvgTransform>());
        state.SetBaseValue(new SvgAnimationBaseValue(target, SvgAnimationTargetKind.Motion, "", SvgAnimationValue.FromTransform(identity)));

        SvgAnimationSample sample = state.Sample(1);
        SvgAnimationSampleValue value = Value(sample, target, SvgAnimationTargetKind.Motion, "");
        SvgTransformMatrixResult matrix = value.AnimatedValue.Transform.ToMatrix();

        Assert.Multiple(() =>
        {
            Assert.That(sample.Success, Is.True, Message(sample));
            Assert.That(value.HasAnimation, Is.True);
            Assert.That(matrix.Success, Is.True);
            Assert.That(matrix.Matrix.A, Is.EqualTo(1).Within(0.00001));
            Assert.That(matrix.Matrix.D, Is.EqualTo(1).Within(0.00001));
            Assert.That(matrix.Matrix.E, Is.EqualTo(50).Within(0.00001));
            Assert.That(matrix.Matrix.F, Is.EqualTo(0).Within(0.00001));
        });
    }

    [Test]
    public void CssAnimationPlansComposeThroughSharedAnimationState()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='box'/></svg>");
        SvgNode target = document.SourceDocumentNode("box");
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits("rect { animation: 4s linear both fade } @keyframes fade { from { opacity:0 } to { opacity:1 } }", new SvgCssLimits());
        List<SvgStyleSheet> sheets = new List<SvgStyleSheet> { parsed.StyleSheet };
        SvgCascadeResult cascade = new SvgCascade().ResolveWithNodeAndStyleSheets(target, sheets);
        SvgCssAnimationPlan plan = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(cascade, sheets);
        SvgAnimationState state = new SvgAnimationState(document);
        state.SetBaseValue(new SvgAnimationBaseValue(target, SvgAnimationTargetKind.CssProperty, "opacity", SvgAnimationValue.FromNumber(0.25)));
        state.SetCssAnimation(new SvgCssAnimationRegistration(target, plan));

        SvgAnimationSample first = state.Sample(1);
        SvgAnimationSample middle = state.Sample(2);
        SvgAnimationSample after = state.Sample(5);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(cascade.Success, Is.True);
            Assert.That(plan.Success, Is.True, string.Join(" | ", plan.Diagnostics.Select(item => item.Message)));
            Assert.That(Value(first, target, SvgAnimationTargetKind.CssProperty, "opacity").AnimatedValue.Number, Is.EqualTo(0.25).Within(0.00001));
            Assert.That(Value(middle, target, SvgAnimationTargetKind.CssProperty, "opacity").AnimatedValue.Number, Is.EqualTo(0.5).Within(0.00001));
            Assert.That(Value(after, target, SvgAnimationTargetKind.CssProperty, "opacity").AnimatedValue.Number, Is.EqualTo(1).Within(0.00001));
            Assert.That(Value(after, target, SvgAnimationTargetKind.CssProperty, "opacity").HasAnimation, Is.True);
        });
    }

    [Test]
    public void PointerEventsAndCursorResultsFeedCallerControlledInteraction()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><rect id='action' width='10' height='10' cursor='url(hand.cur), pointer' tabindex='0'/><rect id='overlay' width='10' height='10' pointer-events='none' cursor='wait'/></svg>";
        SvgResolvedDocument resolved = Resolve(source);
        SvgDocumentHitTestResult hit = resolved.HitTest(new SvgPoint(5, 5));
        SvgInteractionState interaction = new SvgInteractionState(resolved.SourceDocument);

        bool pointer = interaction.SetPointer(hit.Topmost.SourceNode, 5, 5, SvgPointerButtons.Primary);
        bool active = interaction.SetActive(hit.Topmost.SourceNode);
        bool focus = interaction.SetFocus(hit.Topmost.SourceNode);

        Assert.Multiple(() =>
        {
            Assert.That(hit.Success, Is.True);
            Assert.That(Id(hit.Topmost.SourceNode), Is.EqualTo("action"));
            Assert.That(hit.Cursor.Keyword, Is.EqualTo("pointer"));
            Assert.That(hit.Cursor.Urls.Select(item => item.Value), Is.EqualTo(new[] { "hand.cur" }));
            Assert.That(pointer, Is.True);
            Assert.That(active, Is.True);
            Assert.That(focus, Is.True);
            Assert.That(interaction.PointerButtons, Is.EqualTo(SvgPointerButtons.Primary));
            Assert.That(Matches(":hover", hit.Topmost.SourceNode, interaction), Is.True);
            Assert.That(Matches(":active", hit.Topmost.SourceNode, interaction), Is.True);
            Assert.That(Matches(":focus", hit.Topmost.SourceNode, interaction), Is.True);
        });
    }

    [Test]
    public void HitNavigationTabOrderAndAccessibilityTreeShareIdentity()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' id='root' width='30' height='20'><a id='link' href='../next.svg#part' tabindex='0' aria-label='Open destination'><rect id='button' width='10' height='10'/></a><circle id='later' cx='20' cy='5' r='4' tabindex='2' aria-label='Later'/><text id='caption' x='0' y='18'>Caption</text></svg>";
        SvgResolvedDocument resolved = Resolve(source);
        SvgDocumentHitTestResult hit = resolved.HitTest(new SvgPoint(5, 5));
        SvgActivationRecord activation = new SvgNavigation().ActivateWithDocumentAndMatch(resolved, hit.Topmost);
        SvgFocusOrderResult focus = new SvgNavigation().FocusOrderWithDocument(resolved);
        SvgAccessibilityTree tree = resolved.BuildAccessibilityTree();
        SvgNode linkSource = resolved.SourceDocument.Index.GetById("link");
        SvgAccessibilityTreeNode link = tree.Find(linkSource, "");

        Assert.Multiple(() =>
        {
            Assert.That(Id(hit.Topmost.SourceNode), Is.EqualTo("button"));
            Assert.That(activation.Success, Is.True);
            Assert.That(activation.LinkNode.SourceNode, Is.SameAs(linkSource));
            Assert.That(activation.Request.Kind, Is.EqualTo(SvgNavigationRequestKind.Navigate));
            Assert.That(activation.Request.NormalizedIri, Is.EqualTo("https://example.test/next.svg#part"));
            Assert.That(focus.TabOrder.Select(item => Id(item.SourceNode)), Is.EqualTo(new[] { "later", "link" }));
            Assert.That(tree.Success, Is.True, string.Join(" | ", tree.Diagnostics.Select(item => item.Message)));
            Assert.That(tree.FocusOrder.Select(item => Id(item.SourceNode)), Is.EqualTo(new[] { "later", "link" }));
            Assert.That(link, Is.Not.Null);
            Assert.That(link.ResolvedNode, Is.SameAs(activation.LinkNode));
            Assert.That(link.Semantics.Role, Is.EqualTo("link"));
            Assert.That(link.Semantics.Name, Is.EqualTo("Open destination"));
            Assert.That(link.Focusable, Is.True);
            Assert.That(link.HasGeometry, Is.True);
            Assert.That(tree.Text, Is.EqualTo("Caption"));
            Assert.That(tree.TextRanges.Single().SourceNode.Value, Is.EqualTo("Caption"));
        });
    }

    static SvgAnimationSampleValue Value(SvgAnimationSample sample, SvgNode target, SvgAnimationTargetKind kind, string name)
    {
        SvgAnimationSampleValue result = sample.Find(target, kind, name);
        Assert.That(result, Is.Not.Null, Message(sample));
        return result;
    }

    static bool Matches(string selectorText, SvgNode node, SvgInteractionState interaction)
    {
        SvgSelectorParseResult parsed = new SvgSelectorParser().ParseWithValueAndLimits(selectorText, new SvgCssLimits());
        SvgSelectorMatchOptions options = new SvgSelectorMatchOptions();
        options.DynamicState = interaction.DynamicSelectors;
        return new SvgSelectorMatcher().Match(parsed.Selector, node, options).Matches;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = "https://example.test/art/doc.svg";
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static SvgResolvedDocument Resolve(string source)
    {
        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(Load(source));
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

    static string Message(SvgAnimationSample sample)
    {
        return sample.Error + ": " + string.Join(" | ", sample.Diagnostics.Select(item => item.Message));
    }
}

static class SvgPhase9IntegrationDocumentExtensions
{
    public static SvgNode SourceDocumentNode(this SvgDocument document, string id)
    {
        SvgNode result = document.Index.GetById(id);
        Assert.That(result, Is.Not.Null);
        return result;
    }
}
