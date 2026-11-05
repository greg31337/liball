namespace svg.Tests;

public class SvgAnimationTimelineTests
{
    [Test]
    public void OffsetDurationRepeatAndFreezeProduceCurrentSimpleState()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='opacity' begin='1s' dur='2s' repeatCount='2' fill='freeze'/></rect></svg>");

        SvgAnimationTimelineEntry active = new SvgAnimationTimeline().EvaluateWithDocumentAndTime(document, 4.5).Animations.Single();
        SvgAnimationTimelineEntry frozen = new SvgAnimationTimeline().EvaluateWithDocumentAndTime(document, 5).Animations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(active.Active, Is.True);
            Assert.That(active.Frozen, Is.False);
            Assert.That(active.Intervals.Single().Begin, Is.EqualTo(1));
            Assert.That(active.Intervals.Single().End, Is.EqualTo(5));
            Assert.That(active.RepeatIteration, Is.EqualTo(1));
            Assert.That(active.SimpleTime, Is.EqualTo(1.5));
            Assert.That(active.SimpleProgress, Is.EqualTo(0.75));
            Assert.That(frozen.Active, Is.False);
            Assert.That(frozen.Frozen, Is.True);
            Assert.That(frozen.RepeatIteration, Is.EqualTo(1));
            Assert.That(frozen.SimpleTime, Is.EqualTo(2));
            Assert.That(frozen.SimpleProgress, Is.EqualTo(1));
        });
    }

    [Test]
    public void EndMinMaxAndRepeatDurationBoundActiveIntervals()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect>" +
            "<animate attributeName='x' begin='0s' dur='10s' min='2s' max='4s' end='3s'/>" +
            "<animate attributeName='y' begin='1s' dur='1s' repeatCount='indefinite' repeatDur='2.5s'/>" +
            "</rect></svg>";

        SvgAnimationTimelineResult result = new SvgAnimationTimeline().EvaluateWithDocumentAndTime(Load(source), 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Message(result));
            Assert.That(result.Animations[0].Intervals.Single().End, Is.EqualTo(3));
            Assert.That(result.Animations[0].Animation.MinimumSeconds, Is.EqualTo(2));
            Assert.That(result.Animations[0].Animation.MaximumSeconds, Is.EqualTo(4));
            Assert.That(result.Animations[1].Intervals.Single().Begin, Is.EqualTo(1));
            Assert.That(result.Animations[1].Intervals.Single().End, Is.EqualTo(3.5));
            Assert.That(result.Animations[1].Animation.RepeatCount, Is.EqualTo(double.PositiveInfinity));
            Assert.That(result.Animations[1].Active, Is.True);
        });
    }

    [Test]
    public void SyncbaseEndAndRepeatTriggersResolveAcrossAnimations()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect>" +
            "<animate id='base' attributeName='opacity' begin='1s' dur='2s' repeatCount='3'/>" +
            "<animate attributeName='x' begin='base.begin+500ms' dur='1s'/>" +
            "<animate attributeName='y' begin='base.end' dur='1s'/>" +
            "<animate attributeName='width' begin='base.repeat(2)' dur='1s'/>" +
            "</rect></svg>";

        SvgAnimationTimelineResult result = new SvgAnimationTimeline().EvaluateWithDocumentAndTime(Load(source), 5.25);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Message(result));
            Assert.That(result.Animations[0].Intervals.Single().End, Is.EqualTo(7));
            Assert.That(result.Animations[1].BeginTimes, Is.EqualTo(new[] { 1.5 }));
            Assert.That(result.Animations[2].BeginTimes, Is.EqualTo(new[] { 7.0 }));
            Assert.That(result.Animations[3].BeginTimes, Is.EqualTo(new[] { 5.0 }));
            Assert.That(result.Animations[3].Active, Is.True);
            Assert.That(result.SyncPasses, Is.GreaterThanOrEqualTo(1));
        });
    }

    [Test]
    public void InjectedElementAccessKeyAndWallclockEventsCreateInstanceTimes()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><g id='button'>" +
            "<animate attributeName='opacity' begin='click+1s' dur='1s'/>" +
            "<animate attributeName='x' begin='button.focus+250ms' dur='1s'/>" +
            "<animate attributeName='y' begin='accessKey(x)-500ms' dur='1s'/>" +
            "<animate attributeName='width' begin='wallclock(2026-08-19T12:00:00Z)' dur='1s'/>" +
            "</g></svg>";
        SvgDocument document = Load(source);
        SvgNode target = document.DocumentElement.Children[0];
        SvgAnimationTimelineOptions options = new SvgAnimationTimelineOptions();
        options.Events.InjectNodeEvent(target, "click", 2);
        options.Events.InjectEvent("button", "focus", 4);
        options.Events.InjectAccessKey("x", 6);
        options.Events.InjectWallclock("2026-08-19T12:00:00Z", 8);

        SvgAnimationTimelineResult result = new SvgAnimationTimeline().EvaluateWithDocumentAndTimeAndOptions(document, 8.5, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Message(result));
            Assert.That(result.Animations[0].BeginTimes.Single(), Is.EqualTo(3));
            Assert.That(result.Animations[1].BeginTimes.Single(), Is.EqualTo(4.25));
            Assert.That(result.Animations[2].BeginTimes.Single(), Is.EqualTo(5.5));
            Assert.That(result.Animations[3].BeginTimes.Single(), Is.EqualTo(8));
            Assert.That(result.Animations[3].Active, Is.True);
            Assert.That(options.Events.Version, Is.EqualTo(4));
            Assert.That(options.Events.GetEvents(), Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void RestartModesAcceptPreemptIgnoreOrForbidLaterBegins()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect>" +
            "<animate attributeName='x' begin='0s;1s;4s' dur='3s' restart='always'/>" +
            "<animate attributeName='y' begin='0s;1s;4s' dur='3s' restart='whenNotActive'/>" +
            "<animate attributeName='width' begin='0s;1s;4s' dur='3s' restart='never'/>" +
            "</rect></svg>";

        SvgAnimationTimelineResult result = new SvgAnimationTimeline().EvaluateWithDocumentAndTime(Load(source), 4.5);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Message(result));
            Assert.That(result.Animations[0].Intervals.Select(item => (item.Begin, item.End)), Is.EqualTo(new[] { (0.0, 1.0), (1.0, 4.0), (4.0, 7.0) }));
            Assert.That(result.Animations[1].Intervals.Select(item => (item.Begin, item.End)), Is.EqualTo(new[] { (0.0, 3.0), (4.0, 7.0) }));
            Assert.That(result.Animations[2].Intervals.Select(item => (item.Begin, item.End)), Is.EqualTo(new[] { (0.0, 3.0) }));
            Assert.That(result.Animations[0].Active, Is.True);
            Assert.That(result.Animations[1].Active, Is.True);
            Assert.That(result.Animations[2].Active, Is.False);
        });
    }

    [Test]
    public void EventBasedEndsTerminateOtherwiseLongIntervals()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='box'><animate attributeName='opacity' begin='0s' dur='10s' end='box.click+1s'/></rect></svg>";
        SvgDocument document = Load(source);
        SvgAnimationTimelineOptions options = new SvgAnimationTimelineOptions();
        options.Events.InjectEvent("box", "click", 2);

        SvgAnimationTimelineEntry entry = new SvgAnimationTimeline().EvaluateWithDocumentAndTimeAndOptions(document, 3, options).Animations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(entry.EndTimes, Is.EqualTo(new[] { 3.0 }));
            Assert.That(entry.Intervals.Single().End, Is.EqualTo(3));
            Assert.That(entry.Active, Is.False);
        });
    }

    [Test]
    public void DiscardActivatesAtBeginAndReturnsUniqueRemovedTargets()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='gone'/><g><discard href='#gone' begin='2s'/><discard href='#gone' begin='3s'/></g></svg>";
        SvgDocument document = Load(source);

        SvgAnimationTimelineResult before = new SvgAnimationTimeline().EvaluateWithDocumentAndTime(document, 1.9);
        SvgAnimationTimelineResult after = new SvgAnimationTimeline().EvaluateWithDocumentAndTime(document, 3);

        Assert.Multiple(() =>
        {
            Assert.That(before.DiscardedNodes, Is.Empty);
            Assert.That(after.Success, Is.True, Message(after));
            Assert.That(after.Animations.All(item => item.Animation.Kind == SvgAnimationKind.Discard), Is.True);
            Assert.That(after.Animations.All(item => item.Discarded), Is.True);
            Assert.That(after.DiscardedNodes, Has.Count.EqualTo(1));
            Assert.That(after.DiscardedNodes.Single(), Is.SameAs(document.DocumentElement.Children[0]));
        });
    }

    [Test]
    public void InvalidTimingValuesReturnLocatedErrors()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect>" +
            "<animate attributeName='x' begin='0s;'/>" +
            "<animate attributeName='y' dur='-1s'/>" +
            "<animate attributeName='width' repeatCount='0'/>" +
            "<animate attributeName='height' restart='sometimes'/>" +
            "</rect></svg>";

        SvgAnimationTimelineResult result = new SvgAnimationTimeline().EvaluateWithDocumentAndTime(Load(source), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(4));
            Assert.That(result.Diagnostics.All(item => item.Code == SvgDiagnosticCode.InvalidValue), Is.True);
            Assert.That(result.Animations.All(item => !item.Success), Is.True);
        });
    }

    [Test]
    public void TimelineLimitsBoundAnimationsEventsSpecifiersInstancesAndPasses()
    {
        SvgDocument animations = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='x'/><animate attributeName='y'/></rect></svg>");
        SvgAnimationTimelineOptions animationLimit = new SvgAnimationTimelineOptions();
        animationLimit.MaxAnimations = 1;
        SvgAnimationTimelineResult tooManyAnimations = new SvgAnimationTimeline().EvaluateWithDocumentAndTimeAndOptions(animations, 0, animationLimit);

        SvgDocument instances = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='x' begin='0s;1s'/></rect></svg>");
        SvgAnimationTimelineOptions instanceLimit = new SvgAnimationTimelineOptions();
        instanceLimit.MaxInstances = 1;
        SvgAnimationTimelineResult tooManyInstances = new SvgAnimationTimeline().EvaluateWithDocumentAndTimeAndOptions(instances, 0, instanceLimit);

        SvgAnimationTimelineOptions eventLimit = new SvgAnimationTimelineOptions();
        eventLimit.MaxEvents = 1;
        eventLimit.Events.InjectEvent("a", "click", 0);
        eventLimit.Events.InjectEvent("a", "click", 1);
        SvgAnimationTimelineResult tooManyEvents = new SvgAnimationTimeline().EvaluateWithDocumentAndTimeAndOptions(instances, 0, eventLimit);

        Assert.Multiple(() =>
        {
            Assert.That(tooManyAnimations.Success, Is.False);
            Assert.That(tooManyInstances.Success, Is.False);
            Assert.That(tooManyEvents.Success, Is.False);
            Assert.That(tooManyAnimations.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(tooManyInstances.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(tooManyEvents.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void EventQueueCanBeClearedAndDetachedSubtreesCanBeEvaluated()
    {
        SvgAnimationEventQueue events = new SvgAnimationEventQueue();
        events.InjectEvent("box", "click", 1);
        events.Clear();
        SvgDocument document = Load("<svg><g><rect><set attributeName='opacity' begin='2s' to='0'/></rect></g></svg>");
        SvgNode subtree = document.DocumentElement.Children[0];

        SvgAnimationTimelineOptions options = new SvgAnimationTimelineOptions();
        options.Events = events;
        SvgAnimationTimelineResult result = new SvgAnimationTimeline().EvaluateWithRootAndTimeAndOptions(subtree, 2, options);

        Assert.Multiple(() =>
        {
            Assert.That(events.Events, Is.Empty);
            Assert.That(events.Version, Is.EqualTo(2));
            Assert.That(result.Success, Is.True, Message(result));
            Assert.That(result.Animations.Single().Active, Is.True);
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Message(SvgAnimationTimelineResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Code + ": " + item.Message));
    }
}
