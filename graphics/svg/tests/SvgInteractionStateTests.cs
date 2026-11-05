namespace svg.Tests;

public class SvgInteractionStateTests
{
    [Test]
    public void TargetAndRecognizedLinkStateDriveDynamicSelectors()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><a href='page.svg'><rect/></a><g id='section'/><a/></svg>");
        SvgNode link = document.DocumentElement.Children[0];
        SvgNode section = document.DocumentElement.Children[1];
        SvgNode noHref = document.DocumentElement.Children[2];
        SvgInteractionState state = new SvgInteractionState(document);

        bool targeted = state.SetTargetFragment("#section");
        bool visited = state.SetLinkState(link, SvgSelectorLinkState.Visited);

        Assert.Multiple(() =>
        {
            Assert.That(targeted, Is.True);
            Assert.That(visited, Is.True);
            Assert.That(state.Target, Is.SameAs(section));
            Assert.That(Matches(":target", section, state), Is.True);
            Assert.That(Matches(":target", link, state), Is.False);
            Assert.That(Matches(":visited", link, state), Is.True);
            Assert.That(Matches(":link", link, state), Is.False);
            Assert.That(state.GetLinkState(noHref), Is.EqualTo(SvgSelectorLinkState.None));
            Assert.That(state.SetLinkState(noHref, SvgSelectorLinkState.Visited), Is.False);
        });

        state.SetLinkState(link, SvgSelectorLinkState.None);
        Assert.That(Matches(":link", link, state), Is.True);
    }

    [Test]
    public void HoverIncludesAncestorsWhileActiveAndFocusUseExactIdentity()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/></g><circle/></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode rectangle = group.Children[0];
        SvgNode circle = document.DocumentElement.Children[1];
        SvgInteractionState state = new SvgInteractionState(document);

        state.SetHover(rectangle);
        state.SetActive(rectangle);
        state.SetFocus(circle);

        Assert.Multiple(() =>
        {
            Assert.That(Matches(":hover", rectangle, state), Is.True);
            Assert.That(Matches(":hover", group, state), Is.True);
            Assert.That(Matches(":hover", circle, state), Is.False);
            Assert.That(Matches(":active", rectangle, state), Is.True);
            Assert.That(Matches(":active", group, state), Is.False);
            Assert.That(Matches(":focus", circle, state), Is.True);
            Assert.That(Matches(":focus", rectangle, state), Is.False);
        });
    }

    [Test]
    public void PointerUpdateIsAtomicValidatedAndCallerControlled()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>");
        SvgNode rectangle = document.DocumentElement.Children[0];
        SvgInteractionState state = new SvgInteractionState(document);
        SvgPointerButtons buttons = SvgPointerButtons.Primary | SvgPointerButtons.Auxiliary;

        bool accepted = state.SetPointer(rectangle, 12.5, -3, buttons);
        long version = state.Version;
        bool unchanged = state.SetPointer(rectangle, 12.5, -3, buttons);
        bool rejectedCoordinate = state.SetPointer(rectangle, double.NaN, 0, SvgPointerButtons.None);
        bool rejectedButtons = state.SetPointer(rectangle, 0, 0, (SvgPointerButtons)32);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(unchanged, Is.True);
            Assert.That(rejectedCoordinate, Is.False);
            Assert.That(rejectedButtons, Is.False);
            Assert.That(state.Version, Is.EqualTo(version));
            Assert.That(state.HasPointer, Is.True);
            Assert.That(state.PointerX, Is.EqualTo(12.5));
            Assert.That(state.PointerY, Is.EqualTo(-3));
            Assert.That(state.PointerTarget, Is.SameAs(rectangle));
            Assert.That(state.PointerButtons, Is.EqualTo(buttons));
            Assert.That(Matches(":hover", rectangle, state), Is.True);
        });

        state.ClearPointer();
        Assert.Multiple(() =>
        {
            Assert.That(state.HasPointer, Is.False);
            Assert.That(state.PointerTarget, Is.Null);
            Assert.That(state.PointerButtons, Is.EqualTo(SvgPointerButtons.None));
            Assert.That(Matches(":hover", rectangle, state), Is.False);
        });
    }

    [Test]
    public void HostLanguageProvidesLangFallbackWithoutOverridingXmlLang()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/><g xml:lang='de'><circle/></g></svg>");
        SvgNode rectangle = document.DocumentElement.Children[0];
        SvgNode circle = document.DocumentElement.Children[1].Children[0];
        SvgInteractionState state = new SvgInteractionState(document);

        bool accepted = state.SetLanguage("FR-ca");
        bool rejected = state.SetLanguage("en_US");

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(rejected, Is.False);
            Assert.That(state.Language, Is.EqualTo("fr-ca"));
            Assert.That(Matches(":lang(fr)", rectangle, state), Is.True);
            Assert.That(Matches(":lang(fr)", circle, state), Is.False);
            Assert.That(Matches(":lang(de)", circle, state), Is.True);
        });
    }

    [Test]
    public void InteractionRevisionRefreshesStylesWithoutAnEventLoop()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode rectangle = group.Children[0];
        SvgStyleSheet sheet = Sheet("rect { fill:blue } g:hover rect { fill:red }");
        SvgStyleResolver resolver = new SvgStyleResolver();
        SvgStyleResolverOptions options = new SvgStyleResolverOptions();
        SvgInteractionState state = new SvgInteractionState(document);
        state.Apply(options);

        SvgComputedStyle idle = resolver.ResolveWithOptions(rectangle, new List<SvgStyleSheet> { sheet }, options);
        state.SetHover(rectangle);
        SvgComputedStyle hovered = resolver.ResolveWithOptions(rectangle, new List<SvgStyleSheet> { sheet }, options);

        Assert.Multiple(() =>
        {
            Assert.That(state.Version, Is.EqualTo(state.DynamicSelectors.Version));
            Assert.That(idle.GetPaint("fill").Color.Blue, Is.EqualTo(1));
            Assert.That(hovered.GetPaint("fill").Color.Red, Is.EqualTo(1));
            Assert.That(hovered, Is.Not.SameAs(idle));
        });
    }

    [Test]
    public void LanguageAndReducedMotionPropagateToHostInputs()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgInteractionState state = new SvgInteractionState(document);
        state.SetLanguage("en-GB");
        state.SetReducedMotion(true);
        SvgResolveOptions resolve = new SvgResolveOptions();
        SvgConditionalProcessingOptions conditional = new SvgConditionalProcessingOptions();
        SvgAnimationState animation = new SvgAnimationState(document);

        state.ApplyResolveOptions(resolve);
        long environmentVersion = resolve.StyleOptions.StyleEnvironment.Version;
        state.Apply(resolve.StyleOptions);
        state.ApplyConditionalProcessingOptions(conditional);
        state.ApplyAnimationState(animation);

        Assert.Multiple(() =>
        {
            Assert.That(resolve.StyleOptions.SelectorOptions.DynamicState, Is.SameAs(state.DynamicSelectors));
            Assert.That(resolve.StyleOptions.StyleEnvironment.ReducedMotion, Is.True);
            Assert.That(resolve.StyleOptions.StyleEnvironment.Version, Is.EqualTo(environmentVersion));
            Assert.That(resolve.LanguagePreferences, Is.EqualTo(new[] { "en-gb" }));
            Assert.That(conditional.LanguagePreferences, Is.EqualTo(new[] { "en-gb" }));
            Assert.That(animation.ReducedMotion, Is.True);
        });
    }

    [Test]
    public void ForeignDocumentIdentitiesAreRejectedWithoutChangingState()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='local'/></svg>");
        SvgDocument foreignDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><a href='x'/></svg>");
        SvgNode foreign = foreignDocument.DocumentElement.Children[0];
        SvgInteractionState state = new SvgInteractionState(document);

        Assert.Multiple(() =>
        {
            Assert.That(state.SetTarget(foreign), Is.False);
            Assert.That(state.SetHover(foreign), Is.False);
            Assert.That(state.SetActive(foreign), Is.False);
            Assert.That(state.SetFocus(foreign), Is.False);
            Assert.That(state.SetPointer(foreign, 1, 2, SvgPointerButtons.None), Is.False);
            Assert.That(state.SetLinkState(foreign, SvgSelectorLinkState.Visited), Is.False);
            Assert.That(state.SetTargetFragment("#missing"), Is.False);
            Assert.That(state.Version, Is.EqualTo(0));
        });
    }

    static bool Matches(string selectorText, SvgNode node, SvgInteractionState state)
    {
        SvgSelectorParseResult parsed = new SvgSelectorParser().ParseWithValueAndLimits(selectorText, new SvgCssLimits());
        SvgSelectorMatchOptions options = new SvgSelectorMatchOptions();
        options.DynamicState = state.DynamicSelectors;
        return new SvgSelectorMatcher().Match(parsed.Selector, node, options).Matches;
    }

    static SvgStyleSheet Sheet(string source)
    {
        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits(source, new SvgCssLimits());
        Assert.That(result.Success, Is.True);
        return result.StyleSheet;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
