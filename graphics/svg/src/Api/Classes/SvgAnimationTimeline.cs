namespace svg;

/// <summary>Evaluates bounded SMIL instance times and animation intervals at caller-supplied time.</summary>
public class SvgAnimationTimeline : ISvgAnimationTimeline
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgAnimationTimeline()
    {
    }

    /// <summary>Evaluates every animation in a document using default options.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="time">The finite caller time in seconds.</param>
    /// <returns>The resolved timeline state.</returns>
    public SvgAnimationTimelineResult EvaluateWithDocumentAndTime(SvgDocument document, double time)
    {
        return new SvgAnimationTimeline().EvaluateWithDocumentAndTimeAndOptions(document, time, new SvgAnimationTimelineOptions());
    }

    /// <summary>Evaluates every animation in a document with explicit events and limits.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="time">The finite caller time in seconds.</param>
    /// <param name="options">The timeline options.</param>
    /// <returns>The resolved timeline state.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when time is not finite.</exception>
    public SvgAnimationTimelineResult EvaluateWithDocumentAndTimeAndOptions(SvgDocument document, double time, SvgAnimationTimelineOptions options)
    {
        if (document == null) throw new System.ArgumentNullException("document");
        if (options == null) throw new System.ArgumentNullException("options");
        if (!SvgFiniteMath.IsFinite(time)) throw new System.ArgumentOutOfRangeException("time");
        SvgNode? root = document.DocumentElement;
        SvgAnimationTimelineResult result = new SvgAnimationTimelineResult(time, document.Version);
        if (root == null)
        {
            result.Success = true;
            return result;
        }
        return new SvgAnimationTimeline().EvaluateRoot(root, time, options, result);
    }

    /// <summary>Evaluates animations in one connected or detached subtree.</summary>
    /// <param name="root">The subtree root.</param>
    /// <param name="time">The finite caller time in seconds.</param>
    /// <param name="options">The timeline options.</param>
    /// <returns>The resolved timeline state.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when time is not finite.</exception>
    public SvgAnimationTimelineResult EvaluateWithRootAndTimeAndOptions(SvgNode root, double time, SvgAnimationTimelineOptions options)
    {
        if (root == null) throw new System.ArgumentNullException("root");
        if (options == null) throw new System.ArgumentNullException("options");
        if (!SvgFiniteMath.IsFinite(time)) throw new System.ArgumentOutOfRangeException("time");
        return new SvgAnimationTimeline().EvaluateRoot(root, time, options, new SvgAnimationTimelineResult(time, root.Version));
    }
SvgAnimationTimelineResult EvaluateRoot(SvgNode root, double time, SvgAnimationTimelineOptions options, SvgAnimationTimelineResult result)
    {
        if (!new SvgAnimationTimeline().ValidOptions(options))
        {
            new SvgAnimationTimeline().AddResultError(result, SvgDiagnosticCode.InvalidArgument, "Animation timeline options are invalid.", root.SourceSpan);
            return result;
        }
        if (options.Events.Events.Count > options.MaxEvents)
        {
            new SvgAnimationTimeline().AddResultError(result, SvgDiagnosticCode.InputLimitExceeded, "The injected animation event count exceeds MaxEvents.", root.SourceSpan);
            return result;
        }
        SvgAnimationTimelineWork work = new SvgAnimationTimelineWork(options, result);
        new SvgAnimationTimeline().Collect(root, work);
        if (work.LimitFailed) return result;
        new SvgAnimationTimeline().SeedInstanceTimes(work);
        new SvgAnimationTimeline().BuildAllIntervals(result.Animations, work);
        new SvgAnimationTimeline().ResolveDependencies(work);
        new SvgAnimationTimeline().SetCurrentStates(time, work);
        result.InstanceCount = work.InstanceCount;
        result.Success = !new SvgAnimationTimeline().HasErrors(result.Diagnostics);
        return result;
    }
bool ValidOptions(SvgAnimationTimelineOptions options)
    {
        return options.TargetOptions != null && options.Events != null && options.MaxAnimations > 0 && options.MaxTimingSpecifiers > 0 && options.MaxInstances > 0 && options.MaxSyncPasses > 0 && options.MaxEvents > 0 && options.MaxTokenCharacters > 0;
    }
void Collect(SvgNode root, SvgAnimationTimelineWork work)
    {
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        stack.Add(root);
        while (stack.Count != 0)
        {
            int last = stack.Count - 1;
            SvgNode node = stack[last];
            stack.RemoveAt(last);
            if (new SvgAnimationTimeline().IsAnimationElement(node))
            {
                if (work.Result.Animations.Count >= work.Options.MaxAnimations)
                {
                    new SvgAnimationTimeline().FailLimit(work, "The animation element count exceeds MaxAnimations.", node.SourceSpan);
                    return;
                }
                SvgAnimation animation = new SvgAnimationTargetResolver().ResolveWithSourceAndOptions(node, work.Options.TargetOptions);
                SvgAnimationTimelineEntry entry = new SvgAnimationTimelineEntry(animation);
                work.Result.Animations.Add(entry);
                new SvgAnimationTimeline().AppendDiagnostics(work.Result.Diagnostics, animation.Diagnostics);
                if (animation.Success) new SvgAnimationTimeline().ParseTiming(entry, work);
                entry.Success = animation.Success && !new SvgAnimationTimeline().HasErrors(animation.Diagnostics);
                if (work.LimitFailed) return;
            }
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                stack.Add(node.Children[index]);
                index = index - 1;
            }
        }
    }
bool IsAnimationElement(SvgNode node)
    {
        if (node.Kind != SvgNodeKind.Element) return false;
        return node.ElementKind == SvgElementKind.Animate || node.ElementKind == SvgElementKind.AnimateColor || node.ElementKind == SvgElementKind.AnimateMotion || node.ElementKind == SvgElementKind.AnimateTransform || node.ElementKind == SvgElementKind.Set || node.ElementKind == SvgElementKind.Discard;
    }
void ParseTiming(SvgAnimationTimelineEntry entry, SvgAnimationTimelineWork work)
    {
        SvgAnimation animation = entry.Animation;
        SvgNode source = (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgAttribute? begin = new SvgAnimationTimeline().FindAttribute((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.Begin);
        SvgAttribute? end = new SvgAnimationTimeline().FindAttribute(source, SvgAttributeNames.End);
        if (!new SvgAnimationTimeline().ParseTimingAttribute(animation, begin, true, work)) entry.Success = false;
        if (end != null && !new SvgAnimationTimeline().ParseTimingAttribute(animation, end, false, work)) entry.Success = false;
        if (work.LimitFailed) return;
        animation.DurationSeconds = new SvgAnimationTimeline().ParseClockAttribute(animation, new SvgAnimationTimeline().FindAttribute(source, SvgAttributeNames.Dur), double.PositiveInfinity, true, work);
        animation.MinimumSeconds = new SvgAnimationTimeline().ParseClockAttribute(animation, new SvgAnimationTimeline().FindAttribute(source, SvgAttributeNames.Min), 0, false, work);
        animation.MaximumSeconds = new SvgAnimationTimeline().ParseClockAttribute(animation, new SvgAnimationTimeline().FindAttribute(source, SvgAttributeNames.Max), double.PositiveInfinity, true, work);
        animation.RepeatDurationSeconds = new SvgAnimationTimeline().ParseClockAttribute(animation, new SvgAnimationTimeline().FindAttribute(source, SvgAttributeNames.RepeatDur), double.PositiveInfinity, true, work);
        animation.RepeatCount = new SvgAnimationTimeline().ParseRepeatCount(animation, new SvgAnimationTimeline().FindAttribute(source, SvgAttributeNames.RepeatCount), work);
        animation.Restart = new SvgAnimationTimeline().ParseRestart(animation, new SvgAnimationTimeline().FindAttribute(source, SvgAttributeNames.Restart), work);
        animation.Fill = new SvgAnimationTimeline().ParseFill(animation, new SvgAnimationTimeline().FindAttribute(source, SvgAttributeNames.Fill), work);
        if (animation.MinimumSeconds > animation.MaximumSeconds)
        {
            new SvgAnimationTimeline().AddAnimationError(animation, work.Result, SvgDiagnosticCode.InvalidValue, "Animation min cannot exceed max.", source.SourceSpan);
            entry.Success = false;
        }
    }
bool ParseTimingAttribute(SvgAnimation animation, SvgAttribute? attribute, bool begin, SvgAnimationTimelineWork work)
    {
        string value = attribute == null ? "0s" : attribute.Value;
        int remaining = work.Options.MaxTimingSpecifiers - work.TimingCount;
        if (remaining < 1)
        {
            new SvgAnimationTimeline().FailLimit(work, "The timing specifier count exceeds MaxTimingSpecifiers.", attribute == null ? (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan : attribute.SourceSpan);
            return false;
        }
        SvgTimingListParseResult parsed = begin ? new SvgTimingParser().ParseBeginList(value, remaining, work.Options.MaxTokenCharacters) : new SvgTimingParser().ParseEndList(value, remaining, work.Options.MaxTokenCharacters);
        if (!parsed.Success)
        {
            SvgDiagnosticCode code = parsed.Error == SvgTimingParseError.ItemLimitExceeded || parsed.Error == SvgTimingParseError.TokenTooLong ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgAnimationTimeline().AddAnimationError(animation, work.Result, code, begin ? "The animation begin list is invalid." : "The animation end list is invalid.", attribute == null ? (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan : attribute.SourceSpan);
            return false;
        }
        work.TimingCount = work.TimingCount + parsed.Items.Count;
        System.Collections.Generic.List<SvgTiming> destination = begin ? animation.Begin : animation.End;
        int index = 0;
        while (index < parsed.Items.Count)
        {
            destination.Add(parsed.Items[index]);
            index = index + 1;
        }
        return true;
    }
double ParseClockAttribute(SvgAnimation animation, SvgAttribute? attribute, double defaultValue, bool allowIndefinite, SvgAnimationTimelineWork work)
    {
        if (attribute == null) return defaultValue;
        string value = attribute.Value.Trim();
        if (allowIndefinite && value == "indefinite") return double.PositiveInfinity;
        SvgClockParseResult parsed = new SvgTimingParser().ParseClock(value, work.Options.MaxTokenCharacters);
        if (!parsed.Success || (parsed.Clock ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Seconds < 0)
        {
            SvgDiagnosticCode code = parsed.Error == SvgTimingParseError.TokenTooLong ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgAnimationTimeline().AddAnimationError(animation, work.Result, code, "The animation clock attribute must be a nonnegative clock value or permitted indefinite keyword.", attribute.SourceSpan);
            return defaultValue;
        }
        return parsed.Clock.Seconds;
    }
double ParseRepeatCount(SvgAnimation animation, SvgAttribute? attribute, SvgAnimationTimelineWork work)
    {
        if (attribute == null) return 1;
        string value = attribute.Value.Trim();
        if (value == "indefinite") return double.PositiveInfinity;
        SvgNumberParseResult parsed = SvgNumberParser.ParseNumber(value, work.Options.MaxTokenCharacters);
        if (!parsed.Success || parsed.Value <= 0)
        {
            SvgDiagnosticCode code = parsed.Error == SvgNumberParseError.TokenTooLong ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgAnimationTimeline().AddAnimationError(animation, work.Result, code, "repeatCount must be positive or indefinite.", attribute.SourceSpan);
            return 1;
        }
        return parsed.Value;
    }
SvgAnimationRestart ParseRestart(SvgAnimation animation, SvgAttribute? attribute, SvgAnimationTimelineWork work)
    {
        if (attribute == null) return SvgAnimationRestart.Always;
        SvgAnimationKeywordParseResult parsed = new SvgAnimationKeywordParser().ParseRestart(attribute.Value);
        if (parsed.Success) return (SvgAnimationRestart)parsed.Value;
        new SvgAnimationTimeline().AddAnimationError(animation, work.Result, SvgDiagnosticCode.InvalidValue, "restart must be always, whenNotActive, or never.", attribute.SourceSpan);
        return SvgAnimationRestart.Always;
    }
SvgAnimationFill ParseFill(SvgAnimation animation, SvgAttribute? attribute, SvgAnimationTimelineWork work)
    {
        if (attribute == null) return SvgAnimationFill.Remove;
        SvgAnimationKeywordParseResult parsed = new SvgAnimationKeywordParser().ParseFill(attribute.Value);
        if (parsed.Success) return (SvgAnimationFill)parsed.Value;
        new SvgAnimationTimeline().AddAnimationError(animation, work.Result, SvgDiagnosticCode.InvalidValue, "fill must be remove or freeze.", attribute.SourceSpan);
        return SvgAnimationFill.Remove;
    }
void SeedInstanceTimes(SvgAnimationTimelineWork work)
    {
        int entryIndex = 0;
        while (entryIndex < work.Result.Animations.Count)
        {
            SvgAnimationTimelineEntry entry = work.Result.Animations[entryIndex];
            new SvgAnimationTimeline().SeedList(entry, entry.Animation.Begin, true, work);
            new SvgAnimationTimeline().SeedList(entry, entry.Animation.End, false, work);
            new SvgAnimationTimeline().SortTimes(entry.BeginTimes);
            new SvgAnimationTimeline().SortTimes(entry.EndTimes);
            entryIndex = entryIndex + 1;
        }
    }
void SeedList(SvgAnimationTimelineEntry entry, System.Collections.Generic.List<SvgTiming> specs, bool begin, SvgAnimationTimelineWork work)
    {
        int index = 0;
        while (index < specs.Count)
        {
            SvgTiming timing = specs[index];
            if (timing.Kind == SvgTimingKind.Offset) new SvgAnimationTimeline().AddTime(entry, begin, timing.OffsetSeconds, work);
            else if (timing.Kind == SvgTimingKind.EventBase) new SvgAnimationTimeline().AddEventTimes(entry, timing, begin, work);
            else if (timing.Kind == SvgTimingKind.AccessKey) new SvgAnimationTimeline().AddSpecialEventTimes(entry, timing, begin, SvgAnimationEventKind.AccessKey, work);
            else if (timing.Kind == SvgTimingKind.Wallclock) new SvgAnimationTimeline().AddSpecialEventTimes(entry, timing, begin, SvgAnimationEventKind.Wallclock, work);
            index = index + 1;
        }
    }
void AddEventTimes(SvgAnimationTimelineEntry entry, SvgTiming timing, bool begin, SvgAnimationTimelineWork work)
    {
        SvgNode? defaultTarget = entry.Animation.Target;
        SvgNode? explicitTarget = timing.BaseId.Length == 0 || (entry.Animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).OwnerDocument == null ? null : (entry.Animation.Source.OwnerDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Index.GetById(timing.BaseId);
        int index = 0;
        while (index < work.Options.Events.Events.Count)
        {
            SvgAnimationEvent item = work.Options.Events.Events[index];
            if (item.Kind == SvgAnimationEventKind.Event && item.Symbol == timing.Symbol && new SvgAnimationTimeline().EventTargetMatches(item, timing.BaseId, defaultTarget, explicitTarget)) new SvgAnimationTimeline().AddTime(entry, begin, item.Time + timing.OffsetSeconds, work);
            index = index + 1;
        }
    }
bool EventTargetMatches(SvgAnimationEvent item, string baseId, SvgNode? defaultTarget, SvgNode? explicitTarget)
    {
        SvgNode? expected = baseId.Length == 0 ? defaultTarget : explicitTarget;
        if (item.Target != null) return expected != null && object.ReferenceEquals(item.Target, expected);
        string expectedId = baseId.Length == 0 ? new SvgAnimationTimeline().NodeId(defaultTarget) : baseId;
        return expectedId.Length != 0 && item.BaseId == expectedId;
    }
void AddSpecialEventTimes(SvgAnimationTimelineEntry entry, SvgTiming timing, bool begin, SvgAnimationEventKind kind, SvgAnimationTimelineWork work)
    {
        string symbol = kind == SvgAnimationEventKind.AccessKey ? timing.AccessKey : timing.Wallclock;
        int index = 0;
        while (index < work.Options.Events.Events.Count)
        {
            SvgAnimationEvent item = work.Options.Events.Events[index];
            if (item.Kind == kind && item.Symbol == symbol) new SvgAnimationTimeline().AddTime(entry, begin, item.Time + timing.OffsetSeconds, work);
            index = index + 1;
        }
    }
void ResolveDependencies(SvgAnimationTimelineWork work)
    {
        bool changed = true;
        int pass = 0;
        while (changed && pass < work.Options.MaxSyncPasses && !work.LimitFailed)
        {
            changed = false;
            int entryIndex = 0;
            while (entryIndex < work.Result.Animations.Count)
            {
                SvgAnimationTimelineEntry entry = work.Result.Animations[entryIndex];
                if (new SvgAnimationTimeline().ResolveDependencyList(entry, entry.Animation.Begin, true, work)) changed = true;
                if (new SvgAnimationTimeline().ResolveDependencyList(entry, entry.Animation.End, false, work)) changed = true;
                entryIndex = entryIndex + 1;
            }
            if (changed)
            {
                entryIndex = 0;
                while (entryIndex < work.Result.Animations.Count)
                {
                    new SvgAnimationTimeline().SortTimes(work.Result.Animations[entryIndex].BeginTimes);
                    new SvgAnimationTimeline().SortTimes(work.Result.Animations[entryIndex].EndTimes);
                    entryIndex = entryIndex + 1;
                }
                new SvgAnimationTimeline().BuildAllIntervals(work.Result.Animations, work);
            }
            pass = pass + 1;
        }
        work.Result.SyncPasses = pass;
        if (changed && !work.LimitFailed) new SvgAnimationTimeline().FailLimit(work, "Animation synchronization exceeded MaxSyncPasses.", null);
    }
bool ResolveDependencyList(SvgAnimationTimelineEntry entry, System.Collections.Generic.List<SvgTiming> specs, bool begin, SvgAnimationTimelineWork work)
    {
        bool changed = false;
        int index = 0;
        while (index < specs.Count)
        {
            SvgTiming timing = specs[index];
            if (timing.Kind == SvgTimingKind.SyncBase)
            {
                SvgAnimationTimelineEntry? dependency = new SvgAnimationTimeline().FindAnimationById(work.Result.Animations, timing.BaseId);
                if (dependency != null)
                {
                    if (timing.Symbol == "begin" && new SvgAnimationTimeline().CopyOffsetTimes(entry, dependency.BeginTimes, timing.OffsetSeconds, begin, work)) changed = true;
                    if (timing.Symbol == "end" && new SvgAnimationTimeline().CopyIntervalEnds(entry, dependency.Intervals, timing.OffsetSeconds, begin, work)) changed = true;
                }
            }
            else if (timing.Kind == SvgTimingKind.Repeat)
            {
                SvgAnimationTimelineEntry? dependency = timing.BaseId.Length == 0 ? entry : new SvgAnimationTimeline().FindAnimationById(work.Result.Animations, timing.BaseId);
                if (dependency != null && new SvgAnimationTimeline().CopyRepeatTimes(entry, dependency, timing, begin, work)) changed = true;
            }
            index = index + 1;
        }
        return changed;
    }
bool CopyOffsetTimes(SvgAnimationTimelineEntry entry, System.Collections.Generic.List<double> values, double offset, bool begin, SvgAnimationTimelineWork work)
    {
        bool changed = false;
        int index = 0;
        while (index < values.Count)
        {
            if (new SvgAnimationTimeline().AddTime(entry, begin, values[index] + offset, work)) changed = true;
            index = index + 1;
        }
        return changed;
    }
bool CopyIntervalEnds(SvgAnimationTimelineEntry entry, System.Collections.Generic.List<SvgAnimationInterval> intervals, double offset, bool begin, SvgAnimationTimelineWork work)
    {
        bool changed = false;
        int index = 0;
        while (index < intervals.Count)
        {
            if (!double.IsPositiveInfinity(intervals[index].End) && new SvgAnimationTimeline().AddTime(entry, begin, intervals[index].End + offset, work)) changed = true;
            index = index + 1;
        }
        return changed;
    }
bool CopyRepeatTimes(SvgAnimationTimelineEntry entry, SvgAnimationTimelineEntry dependency, SvgTiming timing, bool begin, SvgAnimationTimelineWork work)
    {
        double duration = dependency.Animation.DurationSeconds;
        if (!SvgFiniteMath.IsFinite(duration) || duration <= 0) return false;
        bool changed = false;
        int index = 0;
        while (index < dependency.Intervals.Count)
        {
            SvgAnimationInterval interval = dependency.Intervals[index];
            double time = interval.Begin + duration * timing.RepeatIteration;
            if (SvgFiniteMath.IsFinite(time) && time < interval.End && new SvgAnimationTimeline().AddTime(entry, begin, time + timing.OffsetSeconds, work)) changed = true;
            index = index + 1;
        }
        return changed;
    }
SvgAnimationTimelineEntry? FindAnimationById(System.Collections.Generic.List<SvgAnimationTimelineEntry> entries, string id)
    {
        if (id.Length == 0) return null;
        int index = 0;
        while (index < entries.Count)
        {
            if (new SvgAnimationTimeline().NodeId(entries[index].Animation.Source) == id) return entries[index];
            index = index + 1;
        }
        return null;
    }
void BuildAllIntervals(System.Collections.Generic.List<SvgAnimationTimelineEntry> entries, SvgAnimationTimelineWork work)
    {
        int index = 0;
        while (index < entries.Count && !work.LimitFailed)
        {
            new SvgAnimationTimeline().BuildIntervals(entries[index], work);
            index = index + 1;
        }
    }
void BuildIntervals(SvgAnimationTimelineEntry entry, SvgAnimationTimelineWork work)
    {
        entry.Intervals.Clear();
        if (!entry.Success || entry.Animation.Kind == SvgAnimationKind.Discard) return;
        SvgAnimation animation = entry.Animation;
        int index = 0;
        while (index < entry.BeginTimes.Count)
        {
            double begin = entry.BeginTimes[index];
            if (animation.Restart == SvgAnimationRestart.Never && entry.Intervals.Count != 0) break;
            SvgAnimationInterval? previous = entry.Intervals.Count == 0 ? null : entry.Intervals[entry.Intervals.Count - 1];
            if (previous != null && begin < previous.End)
            {
                if (animation.Restart == SvgAnimationRestart.WhenNotActive)
                {
                    index = index + 1;
                    continue;
                }
                previous.End = begin;
            }
            double end = new SvgAnimationTimeline().CalculatedEnd(animation, begin);
            double explicitEnd = new SvgAnimationTimeline().FirstEndAtOrAfter(entry.EndTimes, begin);
            if (explicitEnd < end) end = explicitEnd;
            if (entry.Intervals.Count >= work.Options.MaxInstances)
            {
                new SvgAnimationTimeline().FailLimit(work, "The animation interval count exceeds MaxInstances.", (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
                return;
            }
            entry.Intervals.Add(new SvgAnimationInterval(begin, end, animation.DurationSeconds));
            index = index + 1;
        }
    }
double CalculatedEnd(SvgAnimation animation, double begin)
    {
        double active = double.PositiveInfinity;
        if (SvgFiniteMath.IsFinite(animation.DurationSeconds))
        {
            if (animation.DurationSeconds == 0) active = 0;
            else if (double.IsPositiveInfinity(animation.RepeatCount)) active = double.PositiveInfinity;
            else
            {
                active = animation.DurationSeconds * animation.RepeatCount;
                if (!SvgFiniteMath.IsFinite(active)) active = double.PositiveInfinity;
            }
        }
        if (animation.RepeatDurationSeconds < active) active = animation.RepeatDurationSeconds;
        if (animation.MaximumSeconds < active) active = animation.MaximumSeconds;
        if (active < animation.MinimumSeconds) active = animation.MinimumSeconds;
        double end = begin + active;
        return SvgFiniteMath.IsFinite(end) ? end : double.PositiveInfinity;
    }
double FirstEndAtOrAfter(System.Collections.Generic.List<double> ends, double begin)
    {
        int index = 0;
        while (index < ends.Count)
        {
            if (ends[index] >= begin) return ends[index];
            index = index + 1;
        }
        return double.PositiveInfinity;
    }
void SetCurrentStates(double time, SvgAnimationTimelineWork work)
    {
        int index = 0;
        while (index < work.Result.Animations.Count)
        {
            SvgAnimationTimelineEntry entry = work.Result.Animations[index];
            if (entry.Animation.Kind == SvgAnimationKind.Discard) new SvgAnimationTimeline().SetDiscardState(entry, time, work.Result);
            else new SvgAnimationTimeline().SetAnimationState(entry, time);
            index = index + 1;
        }
    }
void SetDiscardState(SvgAnimationTimelineEntry entry, double time, SvgAnimationTimelineResult result)
    {
        int index = 0;
        while (index < entry.BeginTimes.Count)
        {
            if (entry.BeginTimes[index] <= time)
            {
                entry.Discarded = true;
                entry.ActiveBegin = entry.BeginTimes[index];
                new SvgAnimationTimeline().AddDiscarded(result.DiscardedNodes, entry.Animation.Target);
                return;
            }
            index = index + 1;
        }
    }
void SetAnimationState(SvgAnimationTimelineEntry entry, double time)
    {
        SvgAnimationInterval? selected = null;
        int index = 0;
        while (index < entry.Intervals.Count)
        {
            SvgAnimationInterval interval = entry.Intervals[index];
            if (interval.Contains(time)) selected = interval;
            index = index + 1;
        }
        if (selected != null) entry.Active = true;
        else if (entry.Animation.Fill == SvgAnimationFill.Freeze)
        {
            index = entry.Intervals.Count - 1;
            while (index >= 0)
            {
                SvgAnimationInterval interval = entry.Intervals[index];
                if (!double.IsPositiveInfinity(interval.End) && time >= interval.End)
                {
                    selected = interval;
                    entry.Frozen = true;
                    break;
                }
                index = index - 1;
            }
        }
        if (selected == null) return;
        entry.ActiveBegin = selected.Begin;
        entry.ActiveEnd = selected.End;
        new SvgAnimationTimeline().SetSimpleState(entry, selected, time);
    }
void SetSimpleState(SvgAnimationTimelineEntry entry, SvgAnimationInterval interval, double time)
    {
        double duration = interval.SimpleDuration;
        double elapsed = entry.Active ? time - interval.Begin : interval.End - interval.Begin;
        if (!SvgFiniteMath.IsFinite(duration) || duration <= 0)
        {
            entry.SimpleTime = elapsed;
            entry.SimpleProgress = duration == 0 && entry.Frozen ? 1 : 0;
            return;
        }
        double quotient = elapsed / duration;
        double iterationValue = entry.Frozen ? System.Math.Ceiling(quotient) - 1 : System.Math.Floor(quotient);
        if (iterationValue < 0) iterationValue = 0;
        if (iterationValue > int.MaxValue) entry.RepeatIteration = int.MaxValue;
        else entry.RepeatIteration = (int)iterationValue;
        entry.SimpleTime = elapsed - entry.RepeatIteration * duration;
        if (entry.Frozen && entry.SimpleTime <= 0) entry.SimpleTime = duration;
        if (entry.SimpleTime > duration) entry.SimpleTime = duration;
        entry.SimpleProgress = entry.SimpleTime / duration;
    }
bool AddTime(SvgAnimationTimelineEntry entry, bool begin, double time, SvgAnimationTimelineWork work)
    {
        if (!SvgFiniteMath.IsFinite(time)) return false;
        System.Collections.Generic.List<double> list = begin ? entry.BeginTimes : entry.EndTimes;
        int index = 0;
        while (index < list.Count)
        {
            if (list[index] == time) return false;
            index = index + 1;
        }
        if (work.InstanceCount >= work.Options.MaxInstances)
        {
            new SvgAnimationTimeline().FailLimit(work, "The resolved instance-time count exceeds MaxInstances.", (entry.Animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return false;
        }
        list.Add(SvgFiniteMath.NormalizeZero(time));
        work.InstanceCount = work.InstanceCount + 1;
        return true;
    }
void SortTimes(System.Collections.Generic.List<double> values)
    {
        values.Sort();
    }
void AddDiscarded(System.Collections.Generic.List<SvgNode> nodes, SvgNode? node)
    {
        if (node == null) return;
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], node)) return;
            index = index + 1;
        }
        nodes.Add(node);
    }
string NodeId(SvgNode? node)
    {
        if (node == null) return "";
        SvgAttribute? attribute = new SvgAnimationTimeline().FindAttribute(node, SvgAttributeNames.Id);
        return attribute == null ? "" : attribute.Value;
    }
SvgAttribute? FindAttribute(SvgNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name) return attribute;
            index = index + 1;
        }
        return null;
    }
void FailLimit(SvgAnimationTimelineWork work, string message, SvgSourceSpan? source)
    {
        if (!work.LimitFailed) new SvgAnimationTimeline().AddResultError(work.Result, SvgDiagnosticCode.InputLimitExceeded, message, source);
        work.LimitFailed = true;
    }
void AddAnimationError(SvgAnimation animation, SvgAnimationTimelineResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source);
        animation.Diagnostics.Add(diagnostic);
        result.Diagnostics.Add(diagnostic);
        animation.Success = false;
    }
void AddResultError(SvgAnimationTimelineResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
        result.Success = false;
    }
void AppendDiagnostics(System.Collections.Generic.List<SvgDiagnostic> destination, System.Collections.Generic.List<SvgDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
bool HasErrors(System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == SvgDiagnosticSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }
}
