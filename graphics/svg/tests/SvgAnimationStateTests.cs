namespace svg.Tests;

public class SvgAnimationStateTests
{
    [Test]
    public void CallerTimeProducesBaseAndAnimatedValuesAndTracksChanges()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='opacity' values='0;1' dur='2s'/></rect></svg>");
        SvgNode target = document.DocumentElement.Children[0];
        SvgAnimationState state = StateWithOpacity(document, target, 0.25);

        SvgAnimationSample first = state.Sample(1);
        SvgAnimationSample repeated = state.Sample(1);
        SvgAnimationSample later = state.Sample(1.5);
        SvgAnimationSampleValue value = first.Find(target, SvgAnimationTargetKind.CssProperty, "opacity");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Message(first));
            Assert.That(first.TimeSeconds, Is.EqualTo(1));
            Assert.That(value.BaseValue.Number, Is.EqualTo(0.25));
            Assert.That(value.AnimatedValue.Number, Is.EqualTo(0.5).Within(0.00001));
            Assert.That(value.HasAnimation, Is.True);
            Assert.That(first.DirtySourceNodes, Is.EqualTo(new[] { target }));
            Assert.That(repeated.DirtySourceNodes, Is.Empty);
            Assert.That(later.Find(target, SvgAnimationTargetKind.CssProperty, "opacity").AnimatedValue.Number, Is.EqualTo(0.75).Within(0.00001));
            Assert.That(later.DirtySourceNodes, Is.EqualTo(new[] { target }));
        });
    }

    [Test]
    public void ReducedMotionReturnsBaseValuesAndPreservePolicyKeepsAnimation()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='opacity' values='0;1' dur='2s'/></rect></svg>");
        SvgNode target = document.DocumentElement.Children[0];
        SvgAnimationState state = StateWithOpacity(document, target, 0.25);
        SvgAnimationSample animated = state.Sample(1);

        state.ReducedMotion = true;
        SvgAnimationSample reduced = state.Sample(1);
        state.Options.ReducedMotionBehavior = SvgAnimationReducedMotionBehavior.Preserve;
        SvgAnimationSample preserved = state.Sample(1);

        Assert.Multiple(() =>
        {
            Assert.That(animated.Find(target, SvgAnimationTargetKind.CssProperty, "opacity").AnimatedValue.Number, Is.EqualTo(0.5).Within(0.00001));
            Assert.That(reduced.Success, Is.True, Message(reduced));
            Assert.That(reduced.ReducedMotionApplied, Is.True);
            Assert.That(reduced.Find(target, SvgAnimationTargetKind.CssProperty, "opacity").AnimatedValue.Number, Is.EqualTo(0.25));
            Assert.That(reduced.Find(target, SvgAnimationTargetKind.CssProperty, "opacity").HasAnimation, Is.False);
            Assert.That(reduced.DirtySourceNodes, Is.EqualTo(new[] { target }));
            Assert.That(preserved.ReducedMotionApplied, Is.False);
            Assert.That(preserved.Find(target, SvgAnimationTargetKind.CssProperty, "opacity").AnimatedValue.Number, Is.EqualTo(0.5).Within(0.00001));
        });
    }

    [Test]
    public void SmilPlansAreCachedAndInvalidatedExplicitlyOrByNodeVersion()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='opacity' values='0;1' dur='2s'/></rect></svg>");
        SvgNode target = document.DocumentElement.Children[0];
        SvgNode animation = target.Children[0];
        SvgAnimationState state = StateWithOpacity(document, target, 0);

        SvgAnimationSample first = state.Sample(1);
        SvgAnimationSample repeated = state.Sample(1);
        int explicitlyRemoved = state.Invalidate(target);
        SvgAnimationSample rebuilt = state.Sample(1);
        animation.MarkDirty();
        SvgAnimationSample automaticallyRebuilt = state.Sample(1);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Message(first));
            Assert.That(repeated.Success, Is.True, Message(repeated));
            Assert.That(explicitlyRemoved, Is.EqualTo(1));
            Assert.That(rebuilt.Success, Is.True, Message(rebuilt));
            Assert.That(automaticallyRebuilt.Success, Is.True, Message(automaticallyRebuilt));
            Assert.That(automaticallyRebuilt.InvalidatedCacheEntries, Is.EqualTo(1));
            Assert.That(state.CachedPlanCount, Is.EqualTo(1));
            Assert.That(state.CacheInvalidations, Is.EqualTo(2));
        });
    }

    [Test]
    public void DirtySourceIdentityReportsEveryCorrespondingSceneNode()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='opacity' values='0;1' dur='2s'/></rect></svg>");
        SvgNode target = document.DocumentElement.Children[0];
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgResolvedNode resolvedTarget = resolved.FindSource(target);
        SvgScene scene = SvgScene.Create(document);
        SvgSceneNode root = new SvgSceneNode(SvgSceneNodeKind.Group);
        root.SetIdentity(resolved.Root);
        SvgSceneNode authored = new SvgSceneNode(SvgSceneNodeKind.Path);
        authored.SetIdentity(resolvedTarget);
        SvgSceneNode instance = new SvgSceneNode(SvgSceneNodeKind.Path);
        instance.SetIdentity(resolvedTarget);
        root.AddChild(authored);
        root.AddChild(instance);
        scene.SetRoot(root);
        SvgAnimationState state = SvgAnimationState.Create(document, scene, new SvgAnimationStateOptions());
        state.SetBaseValue(new SvgAnimationBaseValue(target, SvgAnimationTargetKind.CssProperty, "opacity", SvgAnimationValue.FromNumber(0)));

        SvgAnimationSample sample = state.Sample(1);

        Assert.Multiple(() =>
        {
            Assert.That(sample.Success, Is.True, Message(sample));
            Assert.That(sample.DirtySourceNodes, Is.EqualTo(new[] { target }));
            Assert.That(sample.DirtySceneNodes, Is.EqualTo(new[] { authored, instance }));
        });
    }

    [Test]
    public void RegisteredCssPlanUsesBasePropertyAtCallerTime()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>");
        SvgNode target = document.DocumentElement.Children[0];
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits("rect { animation: 2s linear fade } @keyframes fade { from { opacity:0 } to { opacity:1 } }", new SvgCssLimits());
        List<SvgStyleSheet> sheets = new List<SvgStyleSheet> { parsed.StyleSheet };
        SvgCascadeResult cascade = new SvgCascade().ResolveWithNodeAndStyleSheets(target, sheets);
        SvgCssAnimationPlan plan = new SvgCssAnimation().ResolveWithCascadeAndStyleSheets(cascade, sheets);
        SvgAnimationState state = new SvgAnimationState(document);
        state.SetBaseValue(new SvgAnimationBaseValue(target, SvgAnimationTargetKind.CssProperty, "opacity", SvgAnimationValue.FromNumber(0.25)));
        state.SetCssAnimation(new SvgCssAnimationRegistration(target, plan));

        SvgAnimationSample sample = state.Sample(1);
        SvgAnimationSampleValue value = sample.Find(target, SvgAnimationTargetKind.CssProperty, "opacity");

        Assert.Multiple(() =>
        {
            Assert.That(cascade.Success, Is.True);
            Assert.That(plan.Success, Is.True);
            Assert.That(sample.Success, Is.True, Message(sample));
            Assert.That(value.BaseValue.Number, Is.EqualTo(0.25));
            Assert.That(value.AnimatedValue.Number, Is.EqualTo(0.5).Within(0.00001));
            Assert.That(value.HasAnimation, Is.True);
            Assert.That(sample.DirtySourceNodes, Is.EqualTo(new[] { target }));
        });
    }

    [Test]
    public void MissingActiveBaseValueReturnsStableFailure()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='opacity' values='0;1' dur='2s'/></rect></svg>");

        SvgAnimationSample sample = new SvgAnimationState(document).Sample(1);

        Assert.Multiple(() =>
        {
            Assert.That(sample.Success, Is.False);
            Assert.That(sample.Error, Is.EqualTo(SvgAnimationStateError.MissingBaseValue));
            Assert.That(sample.Diagnostics, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void EndingAnimationAndDiscardReportDirtyTargets()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='animated'><animate attributeName='opacity' values='0;1' dur='1s'/></rect><circle id='gone'/><discard href='#gone' begin='2s'/></svg>");
        SvgNode animatedTarget = document.DocumentElement.Children[0];
        SvgNode discardedTarget = document.DocumentElement.Children[1];
        SvgAnimationState state = StateWithOpacity(document, animatedTarget, 0.25);
        state.Sample(0.5);

        SvgAnimationSample ended = state.Sample(2);
        SvgAnimationSampleValue value = ended.Find(animatedTarget, SvgAnimationTargetKind.CssProperty, "opacity");

        Assert.Multiple(() =>
        {
            Assert.That(ended.Success, Is.True, Message(ended));
            Assert.That(value.AnimatedValue.Number, Is.EqualTo(0.25));
            Assert.That(value.HasAnimation, Is.False);
            Assert.That(ended.DiscardedNodes, Is.EqualTo(new[] { discardedTarget }));
            Assert.That(ended.DirtySourceNodes, Is.EqualTo(new[] { animatedTarget, discardedTarget }));
        });
    }

    static SvgAnimationState StateWithOpacity(SvgDocument document, SvgNode target, double baseValue)
    {
        SvgAnimationState state = new SvgAnimationState(document);
        state.SetBaseValue(new SvgAnimationBaseValue(target, SvgAnimationTargetKind.CssProperty, "opacity", SvgAnimationValue.FromNumber(baseValue)));
        return state;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static string Message(SvgAnimationSample sample)
    {
        return sample.Error + ": " + string.Join(" | ", sample.Diagnostics.Select(item => item.Message));
    }
}
