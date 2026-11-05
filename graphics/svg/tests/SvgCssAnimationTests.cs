namespace svg.Tests;

public class SvgCssAnimationTests
{
    [Test]
    public void AnimationShorthandExpandsThroughTheCascadeAndSelectsKeyframes()
    {
        string css = "rect { animation: 2s linear .5s 3 alternate both paused fade; } @keyframes fade { from { opacity:0 } to { opacity:1 } }";
        CssContext context = Context(css);

        SvgCssAnimationPlan plan = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(context.Cascade, context.Sheets);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cascade.Find(SvgCssPropertyNames.AnimationName).ValueText, Is.EqualTo("fade"));
            Assert.That(context.Cascade.Find(SvgCssPropertyNames.AnimationDuration).ValueText, Is.EqualTo("2s"));
            Assert.That(context.Cascade.Find(SvgCssPropertyNames.AnimationDelay).ValueText, Is.EqualTo(".5s"));
            Assert.That(plan.Success, Is.True, Message(plan));
            Assert.That(plan.Animations, Has.Count.EqualTo(1));
            Assert.That(plan.Animations[0].Name, Is.EqualTo("fade"));
            Assert.That(plan.Animations[0].DurationSeconds, Is.EqualTo(2));
            Assert.That(plan.Animations[0].DelaySeconds, Is.EqualTo(0.5));
            Assert.That(plan.Animations[0].IterationCount, Is.EqualTo(3));
            Assert.That(plan.Animations[0].Direction, Is.EqualTo(SvgCssAnimationDirection.Alternate));
            Assert.That(plan.Animations[0].FillMode, Is.EqualTo(SvgCssAnimationFillMode.Both));
            Assert.That(plan.Animations[0].PlayState, Is.EqualTo(SvgCssAnimationPlayState.Paused));
        });
    }

    [Test]
    public void DelayDurationAndLinearEasingUseCallerControlledTime()
    {
        string css = "rect { animation-name:fade; animation-duration:2s; animation-delay:.5s; animation-timing-function:linear } @keyframes fade { from { opacity:0 } to { opacity:1 } }";
        CssContext context = Context(css);
        SvgCssAnimationPlan plan = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(context.Cascade, context.Sheets);

        SvgCssAnimationSampleResult sample = new SvgCssAnimation().Sample(plan, 1.5, Underlying("opacity", SvgAnimationValue.FromNumber(0.25)), new SvgCssAnimationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(sample.Success, Is.True);
            Assert.That(sample.Animations[0].Phase, Is.EqualTo(SvgCssAnimationPhase.Active));
            Assert.That(sample.Animations[0].Progress, Is.EqualTo(0.5));
            Assert.That(sample.Find("opacity").Value.Number, Is.EqualTo(0.5).Within(0.00001));
        });
    }

    [Test]
    public void KeyframeTimingFunctionOverridesAnimationEasingForItsInterval()
    {
        string css = "rect { animation: 2s linear fade } @keyframes fade { 0% { opacity:0; animation-timing-function:steps(2,end) } 100% { opacity:1 } }";
        CssContext context = Context(css);
        SvgCssAnimationPlan plan = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(context.Cascade, context.Sheets);

        SvgCssAnimationSampleResult first = new SvgCssAnimation().Sample(plan, 0.5, Underlying("opacity", SvgAnimationValue.FromNumber(0)), new SvgCssAnimationOptions());
        SvgCssAnimationSampleResult second = new SvgCssAnimation().Sample(plan, 1.5, Underlying("opacity", SvgAnimationValue.FromNumber(0)), new SvgCssAnimationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Find("opacity").Value.Number, Is.EqualTo(0));
            Assert.That(second.Find("opacity").Value.Number, Is.EqualTo(0.5));
        });
    }

    [Test]
    public void DirectionIterationsAndFillModesSelectBeforeActiveAndAfterValues()
    {
        string css = "rect { animation: 1s linear 1s 2 alternate both fade } @keyframes fade { from { opacity:0 } to { opacity:1 } }";
        CssContext context = Context(css);
        SvgCssAnimationPlan plan = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(context.Cascade, context.Sheets);
        List<SvgCssAnimationUnderlyingValue> underlying = Underlying("opacity", SvgAnimationValue.FromNumber(0.4));

        SvgCssAnimationSampleResult before = new SvgCssAnimation().Sample(plan, 0.5, underlying, new SvgCssAnimationOptions());
        SvgCssAnimationSampleResult first = new SvgCssAnimation().Sample(plan, 1.25, underlying, new SvgCssAnimationOptions());
        SvgCssAnimationSampleResult second = new SvgCssAnimation().Sample(plan, 2.25, underlying, new SvgCssAnimationOptions());
        SvgCssAnimationSampleResult after = new SvgCssAnimation().Sample(plan, 4, underlying, new SvgCssAnimationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(before.Animations[0].Phase, Is.EqualTo(SvgCssAnimationPhase.Before));
            Assert.That(before.Find("opacity").Value.Number, Is.EqualTo(0));
            Assert.That(first.Animations[0].Progress, Is.EqualTo(0.25));
            Assert.That(first.Find("opacity").Value.Number, Is.EqualTo(0.25));
            Assert.That(second.Animations[0].IterationIndex, Is.EqualTo(1));
            Assert.That(second.Animations[0].Progress, Is.EqualTo(0.75));
            Assert.That(after.Animations[0].Phase, Is.EqualTo(SvgCssAnimationPhase.After));
            Assert.That(after.Find("opacity").Value.Number, Is.EqualTo(0));
        });
    }

    [Test]
    public void PausedAnimationHoldsItsInitialActiveTime()
    {
        string css = "rect { animation: 2s linear both paused fade } @keyframes fade { from { opacity:.2 } to { opacity:1 } }";
        CssContext context = Context(css);
        SvgCssAnimationPlan plan = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(context.Cascade, context.Sheets);

        SvgCssAnimationSampleResult sample = new SvgCssAnimation().Sample(plan, 100, Underlying("opacity", SvgAnimationValue.FromNumber(0)), new SvgCssAnimationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(sample.Animations[0].Phase, Is.EqualTo(SvgCssAnimationPhase.Active));
            Assert.That(sample.Animations[0].Progress, Is.EqualTo(0));
            Assert.That(sample.Find("opacity").Value.Number, Is.EqualTo(0.2));
        });
    }

    [Test]
    public void MultipleAnimationsRepeatShortListsAndLaterEffectsWin()
    {
        string css = "rect { animation-name:first,second; animation-duration:2s; animation-timing-function:linear } @keyframes first { from { opacity:0 } to { opacity:1 } } @keyframes second { from { opacity:0 } to { opacity:2 } }";
        CssContext context = Context(css);
        SvgCssAnimationPlan plan = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(context.Cascade, context.Sheets);

        SvgCssAnimationSampleResult sample = new SvgCssAnimation().Sample(plan, 1, Underlying("opacity", SvgAnimationValue.FromNumber(0.5)), new SvgCssAnimationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Animations, Has.Count.EqualTo(2));
            Assert.That(plan.Animations.Select(item => item.DurationSeconds), Is.EqualTo(new[] { 2.0, 2.0 }));
            Assert.That(sample.Find("opacity").AnimationIndex, Is.EqualTo(1));
            Assert.That(sample.Find("opacity").Value.Number, Is.EqualTo(1));
        });
    }

    [Test]
    public void ImplicitEndpointsUseTheCurrentUnderlyingCascadeValue()
    {
        string css = "rect { animation: 2s linear fade } @keyframes fade { 50% { opacity:1 } }";
        CssContext context = Context(css);
        SvgCssAnimationPlan plan = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(context.Cascade, context.Sheets);
        List<SvgCssAnimationUnderlyingValue> underlying = Underlying("opacity", SvgAnimationValue.FromNumber(0));

        SvgCssAnimationSampleResult first = new SvgCssAnimation().Sample(plan, 0.5, underlying, new SvgCssAnimationOptions());
        SvgCssAnimationSampleResult second = new SvgCssAnimation().Sample(plan, 1.5, underlying, new SvgCssAnimationOptions());
        SvgCssAnimationSampleResult missing = new SvgCssAnimation().Sample(plan, 0.5, new List<SvgCssAnimationUnderlyingValue>(), new SvgCssAnimationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Animations[0].Tracks[0].Keys.Select(key => key.Offset), Is.EqualTo(new[] { 0.0, 0.5, 1.0 }));
            Assert.That(first.Find("opacity").Value.Number, Is.EqualTo(0.5));
            Assert.That(second.Find("opacity").Value.Number, Is.EqualTo(0.5));
            Assert.That(missing.Error, Is.EqualTo(SvgCssAnimationError.MissingUnderlyingValue));
        });
    }

    [Test]
    public void LaterKeyframesRuleWithTheSameCaseSensitiveNameWins()
    {
        string css = "rect { animation: 1s linear forwards Fade } @keyframes Fade { to { opacity:.5 } } @keyframes fade { to { opacity:.7 } } @keyframes Fade { to { opacity:1 } }";
        CssContext context = Context(css);
        SvgCssAnimationPlan plan = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(context.Cascade, context.Sheets);

        SvgCssAnimationSampleResult sample = new SvgCssAnimation().Sample(plan, 2, Underlying("opacity", SvgAnimationValue.FromNumber(0)), new SvgCssAnimationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Animations[0].Keyframes, Is.SameAs(context.Sheets[0].Rules[3].AtRule.Keyframes));
            Assert.That(sample.Find("opacity").Value.Number, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidTimingAndEasingReturnStablePlanFailures()
    {
        CssContext durationContext = Context("rect { animation-name:fade; animation-duration:-1s } @keyframes fade { to { opacity:1 } }");
        CssContext easingContext = Context("rect { animation-name:fade; animation-duration:1s; animation-timing-function:cubic-bezier(2,0,1,1) } @keyframes fade { to { opacity:1 } }");

        SvgCssAnimationPlan duration = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(durationContext.Cascade, durationContext.Sheets);
        SvgCssAnimationPlan easing = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(easingContext.Cascade, easingContext.Sheets);

        Assert.Multiple(() =>
        {
            Assert.That(duration.Error, Is.EqualTo(SvgCssAnimationError.InvalidAnimation));
            Assert.That(easing.Error, Is.EqualTo(SvgCssAnimationError.InvalidEasing));
        });
    }

    static CssContext Context(string css)
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>");
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits());
        Assert.That(parsed.StyleSheet, Is.Not.Null);
        List<SvgStyleSheet> sheets = new List<SvgStyleSheet> { parsed.StyleSheet };
        SvgCascadeResult cascade = new SvgCascade().ResolveWithNodeAndStyleSheets(document.DocumentElement.Children[0], sheets);
        Assert.That(cascade.Success, Is.True, string.Join(" | ", cascade.Diagnostics.Select(item => item.Message)));
        return new CssContext(sheets, cascade);
    }

    static List<SvgCssAnimationUnderlyingValue> Underlying(string property, SvgAnimationValue value)
    {
        return new List<SvgCssAnimationUnderlyingValue> { new SvgCssAnimationUnderlyingValue(property, value) };
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static string Message(SvgCssAnimationPlan plan)
    {
        return plan.Error + ": " + string.Join(" | ", plan.Diagnostics.Select(item => item.Message));
    }

    class CssContext
    {
        public List<SvgStyleSheet> Sheets;
        public SvgCascadeResult Cascade;

        public CssContext(List<SvgStyleSheet> sheets, SvgCascadeResult cascade)
        {
            Sheets = sheets;
            Cascade = cascade;
        }
    }
}
