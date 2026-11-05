namespace svg;

/// <summary>Configures animation-state timelines, interpolation, motion, CSS, and reduced motion.</summary>
public class SvgAnimationStateOptions
{
    /// <summary>The SMIL timeline options and injected events.</summary>
    public SvgAnimationTimelineOptions Timeline;
    /// <summary>The SMIL typed composition options.</summary>
    public SvgAnimationCompositionOptions Composition;
    /// <summary>The motion path sampling options.</summary>
    public SvgMotionAnimationOptions Motion;
    /// <summary>The CSS animation sampling options.</summary>
    public SvgCssAnimationOptions Css;
    /// <summary>The policy applied when state reduced motion is enabled.</summary>
    public SvgAnimationReducedMotionBehavior ReducedMotionBehavior;
    /// <summary>The maximum cached SMIL plans.</summary>
    public int MaxCachedPlans;

    /// <summary>Creates secure deterministic state defaults.</summary>
    public SvgAnimationStateOptions()
    {
        Timeline = new SvgAnimationTimelineOptions();
        Composition = new SvgAnimationCompositionOptions();
        Motion = new SvgMotionAnimationOptions();
        Css = new SvgCssAnimationOptions();
        ReducedMotionBehavior = SvgAnimationReducedMotionBehavior.Disable;
        MaxCachedPlans = 4096;
    }
}

/// <summary>Associates one animation value slot with its caller-supplied base value.</summary>
public class SvgAnimationBaseValue
{
    /// <summary>The source element receiving animation.</summary>
    public SvgNode Target;
    /// <summary>The animation value slot kind.</summary>
    public SvgAnimationTargetKind TargetKind;
    /// <summary>The CSS property or XML attribute local name.</summary>
    public string PropertyName;
    /// <summary>The typed base value.</summary>
    public SvgAnimationValue Value;

    /// <summary>Creates one typed base value binding.</summary>
    /// <param name="target">The target SVG element.</param>
    /// <param name="targetKind">The target value slot kind.</param>
    /// <param name="propertyName">The local property name, or an empty string for motion.</param>
    /// <param name="value">The typed base value.</param>
    public SvgAnimationBaseValue(SvgNode target, SvgAnimationTargetKind targetKind, string propertyName, SvgAnimationValue value)
    {
        if (target == null || target.Kind != SvgNodeKind.Element) throw new System.ArgumentOutOfRangeException("target");
        if (targetKind <= SvgAnimationTargetKind.Unknown || targetKind > SvgAnimationTargetKind.Motion) throw new System.ArgumentOutOfRangeException("targetKind");
        if (propertyName == null) throw new System.ArgumentNullException("propertyName");
        if (value == null) throw new System.ArgumentNullException("value");
        Target = target; TargetKind = targetKind; PropertyName = Normalize(propertyName); Value = value;
    }

    internal static string Normalize(string value)
    {
        return value.ToLowerInvariant();
    }
}

/// <summary>Associates one target element with a resolved CSS animation plan.</summary>
public class SvgCssAnimationRegistration
{
    /// <summary>The target SVG element.</summary>
    public SvgNode Target;
    /// <summary>The target version captured at registration.</summary>
    public long TargetVersion;
    /// <summary>The resolved CSS animation plan.</summary>
    public SvgCssAnimationPlan Plan;

    /// <summary>Creates one CSS animation registration.</summary>
    /// <param name="target">The target element.</param>
    /// <param name="plan">The successful CSS animation plan.</param>
    public SvgCssAnimationRegistration(SvgNode target, SvgCssAnimationPlan plan)
    {
        if (target == null || target.Kind != SvgNodeKind.Element) throw new System.ArgumentOutOfRangeException("target");
        if (plan == null || !plan.Success) throw new System.ArgumentOutOfRangeException("plan");
        Target = target; TargetVersion = target.Version; Plan = plan;
    }
}

/// <summary>Contains one base-versus-animated value slot in an animation sample.</summary>
public class SvgAnimationSampleValue
{
    /// <summary>The animated target element.</summary>
    public SvgNode Target;
    /// <summary>The animated target slot kind.</summary>
    public SvgAnimationTargetKind TargetKind;
    /// <summary>The normalized property or attribute name.</summary>
    public string PropertyName;
    /// <summary>The registered base value.</summary>
    public SvgAnimationValue BaseValue;
    /// <summary>The final animation-composed value.</summary>
    public SvgAnimationValue AnimatedValue;
    /// <summary>Whether an active or filling animation contributed.</summary>
    public bool HasAnimation;

    /// <summary>Creates one sampled base-versus-animated value.</summary>
    /// <param name="target">The target element.</param>
    /// <param name="targetKind">The target slot kind.</param>
    /// <param name="propertyName">The normalized property name.</param>
    /// <param name="baseValue">The base value.</param>
    public SvgAnimationSampleValue(SvgNode target, SvgAnimationTargetKind targetKind, string propertyName, SvgAnimationValue baseValue)
    {
        Target = target; TargetKind = targetKind; PropertyName = propertyName; BaseValue = baseValue; AnimatedValue = baseValue; HasAnimation = false;
    }
}

/// <summary>Contains one complete caller-time animation sample and dirty-node report.</summary>
public class SvgAnimationSample
{
    /// <summary>The finite caller time in seconds.</summary>
    public double TimeSeconds;
    /// <summary>Whether reduced-motion suppression was applied.</summary>
    public bool ReducedMotionApplied;
    /// <summary>The sampled base-versus-animated values.</summary>
    public System.Collections.Generic.List<SvgAnimationSampleValue> Values;
    /// <summary>The source nodes whose effective animated state changed.</summary>
    public System.Collections.Generic.List<SvgNode> DirtySourceNodes;
    /// <summary>The scene nodes corresponding to dirty source identities.</summary>
    public System.Collections.Generic.List<SvgSceneNode> DirtySceneNodes;
    /// <summary>The targets discarded by the SMIL timeline.</summary>
    public System.Collections.Generic.List<SvgNode> DiscardedNodes;
    /// <summary>Located timeline and sampling diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The number of stale cached plans removed during this sample.</summary>
    public int InvalidatedCacheEntries;
    /// <summary>The stable sample failure.</summary>
    public SvgAnimationStateError Error;
    /// <summary>Whether the complete sample succeeded.</summary>
    public bool Success;

    internal SvgAnimationSample(double time)
    {
        TimeSeconds = time; ReducedMotionApplied = false; Values = new System.Collections.Generic.List<SvgAnimationSampleValue>(); DirtySourceNodes = new System.Collections.Generic.List<SvgNode>(); DirtySceneNodes = new System.Collections.Generic.List<SvgSceneNode>(); DiscardedNodes = new System.Collections.Generic.List<SvgNode>(); Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(); InvalidatedCacheEntries = 0; Error = SvgAnimationStateError.None; Success = false;
    }

    /// <summary>Finds one sampled slot by target identity, kind, and local name.</summary>
    /// <param name="target">The target element.</param>
    /// <param name="targetKind">The target slot kind.</param>
    /// <param name="propertyName">The local property name.</param>
    /// <returns>The sampled value, or <c>null</c>.</returns>
    internal SvgAnimationSampleValue? Find(SvgNode target, SvgAnimationTargetKind targetKind, string propertyName)
    {
        int index = 0;
        while (index < Values.Count)
        {
            SvgAnimationSampleValue value = Values[index];
            if (object.ReferenceEquals(value.Target, target) && value.TargetKind == targetKind && string.Equals(value.PropertyName, propertyName, System.StringComparison.OrdinalIgnoreCase)) return value;
            index = index + 1;
        }
        return null;
    }
}

/// <summary>Owns caller-controlled animation inputs, plan caches, and dirty-state comparison.</summary>
public class SvgAnimationState
{
    /// <summary>The source SVG document.</summary>
    public SvgDocument? Document;
    /// <summary>The optional normalized scene used for dirty scene-node reporting.</summary>
    public SvgScene? Scene;
    /// <summary>The state sampling options.</summary>
    public SvgAnimationStateOptions? Options;
    /// <summary>Whether the caller currently requests reduced motion.</summary>
    public bool ReducedMotion;
    /// <summary>The registered typed base values.</summary>
    public System.Collections.Generic.List<SvgAnimationBaseValue>? BaseValues;
    /// <summary>The registered per-target CSS animation plans.</summary>
    public System.Collections.Generic.List<SvgCssAnimationRegistration>? CssAnimations;
    /// <summary>The most recent successful sample, or <c>null</c>.</summary>
    public SvgAnimationSample? LastSample;
    /// <summary>The current cached SMIL plan count.</summary>
    public int CachedPlanCount;
    /// <summary>The cumulative explicit and automatic cache invalidation count.</summary>
    public long CacheInvalidations;

    System.Collections.Generic.List<SvgAnimationStateCacheEntry>? cache;

    /// <summary>Creates animation state for one document without a scene.</summary>
    /// <param name="document">The source document.</param>
    public SvgAnimationState(SvgDocument document)
    {
        Initialize(document, null, new SvgAnimationStateOptions());
    }

    /// <summary>Creates animation state for one document and optional scene.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="scene">The optional normalized scene.</param>
    /// <param name="options">The timeline, interpolation, cache, and reduced-motion options.</param>
    /// <returns>A new animation state.</returns>
    internal static SvgAnimationState Create(SvgDocument document, SvgScene? scene, SvgAnimationStateOptions options)
    {
        SvgAnimationState result = new SvgAnimationState(document);
        result.Initialize(document, scene, options);
        return result;
    }

    /// <summary>Adds or replaces one typed base value binding.</summary>
    /// <param name="value">The base value binding.</param>
    internal void SetBaseValue(SvgAnimationBaseValue value)
    {
        if (value == null) throw new System.ArgumentNullException("value");
        int index = FindBase(value.Target, value.TargetKind, value.PropertyName);
        if (index < 0) (BaseValues ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Add(value);
        else (BaseValues ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[index] = value;
    }

    /// <summary>Adds or replaces a target's resolved CSS animation plan.</summary>
    /// <param name="registration">The CSS animation registration.</param>
    internal void SetCssAnimation(SvgCssAnimationRegistration registration)
    {
        if (registration == null) throw new System.ArgumentNullException("registration");
        int index = 0;
        while (index < (CssAnimations ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            if (object.ReferenceEquals(CssAnimations[index].Target, registration.Target)) { CssAnimations[index] = registration; return; }
            index = index + 1;
        }
        CssAnimations.Add(registration);
    }

    /// <summary>Removes every cached SMIL value and motion plan.</summary>
    /// <returns>The removed entry count.</returns>
    internal int InvalidateAll()
    {
        int removed = (cache ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count; cache.Clear(); CachedPlanCount = 0; CacheInvalidations = CacheInvalidations + removed; return removed;
    }

    /// <summary>Removes cached plans whose animation source or target is the supplied node.</summary>
    /// <param name="node">The source or target node.</param>
    /// <returns>The removed entry count.</returns>
    internal int Invalidate(SvgNode? node)
    {
        if (node == null) return 0;
        int removed = 0; int index = (cache ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count - 1;
        while (index >= 0)
        {
            if (object.ReferenceEquals(cache[index].Source, node) || object.ReferenceEquals(cache[index].Target, node)) { cache.RemoveAt(index); removed = removed + 1; }
            index = index - 1;
        }
        CachedPlanCount = cache.Count; CacheInvalidations = CacheInvalidations + removed; return removed;
    }

    /// <summary>Samples SMIL and registered CSS animations at caller-provided seconds.</summary>
    /// <param name="timeSeconds">The finite caller time in seconds.</param>
    /// <returns>The complete value and dirty-node sample.</returns>
    internal SvgAnimationSample Sample(double timeSeconds)
    {
        SvgAnimationSample sample = new SvgAnimationSample(timeSeconds);
        if (!SvgFiniteMath.IsFinite(timeSeconds) || !ValidState()) { StateFail(sample, SvgAnimationStateError.InvalidArgument, "Animation state or caller time is invalid.", Document == null ? null : Document.SourceSpan); return sample; }
        sample.InvalidatedCacheEntries = RemoveStaleCache();
        CopyBaseValues(sample.Values);
        bool suppress = ReducedMotion && (Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ReducedMotionBehavior == SvgAnimationReducedMotionBehavior.Disable;
        sample.ReducedMotionApplied = suppress;
        if (!suppress)
        {
            if (!SampleSmil(sample, timeSeconds)) return sample;
            if (!SampleCss(sample, timeSeconds)) return sample;
        }
        ReportDirty(sample);
        sample.Success = true;
        LastSample = sample;
        return sample;
    }

    internal void Initialize(SvgDocument document, SvgScene? scene, SvgAnimationStateOptions options)
    {
        if (document == null) throw new System.ArgumentNullException("document");
        if (options == null) throw new System.ArgumentNullException("options");
        Document = document; Scene = scene; Options = options; ReducedMotion = false; BaseValues = new System.Collections.Generic.List<SvgAnimationBaseValue>(); CssAnimations = new System.Collections.Generic.List<SvgCssAnimationRegistration>(); LastSample = null; CachedPlanCount = 0; CacheInvalidations = 0; cache = new System.Collections.Generic.List<SvgAnimationStateCacheEntry>();
    }

    internal bool SampleSmil(SvgAnimationSample sample, double time)
    {
        SvgAnimationTimelineResult timeline = new SvgAnimationTimeline().EvaluateWithDocumentAndTimeAndOptions((Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), time, (Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Timeline);
        AppendDiagnostics(sample.Diagnostics, timeline.Diagnostics);
        CopyNodes(timeline.DiscardedNodes, sample.DiscardedNodes);
        if (!timeline.Success) { StateFail(sample, SvgAnimationStateError.TimelineFailure, "The SMIL timeline could not be evaluated.", Document.SourceSpan); return false; }
        int index = 0;
        while (index < timeline.Animations.Count)
        {
            SvgAnimationTimelineEntry entry = timeline.Animations[index];
            if (entry.Success && (entry.Active || entry.Frozen) && entry.Animation.Kind != SvgAnimationKind.Discard)
            {
                SvgAnimation animation = entry.Animation;
                string name = animation.TargetKind == SvgAnimationTargetKind.Motion ? "" : animation.LocalName;
                SvgAnimationSampleValue? value = FindValue(sample.Values, animation.Target, animation.TargetKind, name);
                if (value == null) { StateFail(sample, SvgAnimationStateError.MissingBaseValue, "An active SMIL animation requires a registered compatible base value.", (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan); return false; }
                SvgAnimationStateCacheEntry? cached = GetPlan(animation, sample);
                if (cached == null) return false;
                if (animation.Kind == SvgAnimationKind.AnimateMotion)
                {
                    if (value.AnimatedValue.Kind != SvgAnimationValueKind.Transform) { StateFail(sample, SvgAnimationStateError.MissingBaseValue, "Motion animation requires a transform base value.", (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan); return false; }
                    SvgMotionAnimationResult motion = new SvgMotionAnimation().SampleWithPlanAndTimelineAndOriginAndBaseTransformAndOptions((cached.MotionPlan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), entry, new SvgPoint(0, 0), value.AnimatedValue.Transform, Options.Motion);
                    if (!motion.Success) { StateFail(sample, SvgAnimationStateError.InvalidSmilAnimation, "Motion animation sampling failed.", (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan); return false; }
                    value.AnimatedValue = SvgAnimationValue.FromTransform((motion.ComposedTransform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
                }
                else
                {
                    SvgAnimationCompositionResult composed = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndTimelineAndOptions((cached.ValuePlan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), value.AnimatedValue, entry, Options.Composition);
                    if (!composed.Success) { StateFail(sample, SvgAnimationStateError.InvalidSmilAnimation, "SMIL value composition failed.", (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan); return false; }
                    value.AnimatedValue = (composed.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                }
                value.HasAnimation = true;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool SampleCss(SvgAnimationSample sample, double time)
    {
        int registrationIndex = 0;
        while (registrationIndex < (CssAnimations ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            SvgCssAnimationRegistration registration = CssAnimations[registrationIndex];
            if (registration.Target.Version != registration.TargetVersion) { StateFail(sample, SvgAnimationStateError.StaleSnapshot, "A registered CSS animation target is stale.", registration.Target.SourceSpan); return false; }
            System.Collections.Generic.List<SvgCssAnimationUnderlyingValue> underlying = new System.Collections.Generic.List<SvgCssAnimationUnderlyingValue>();
            int valueIndex = 0;
            while (valueIndex < sample.Values.Count)
            {
                SvgAnimationSampleValue value = sample.Values[valueIndex];
                if (object.ReferenceEquals(value.Target, registration.Target) && value.TargetKind == SvgAnimationTargetKind.CssProperty) underlying.Add(new SvgCssAnimationUnderlyingValue(value.PropertyName, value.AnimatedValue));
                valueIndex = valueIndex + 1;
            }
            SvgCssAnimationSampleResult css = new SvgCssAnimation().Sample(registration.Plan, time, underlying, (Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Css);
            if (!css.Success) { StateFail(sample, SvgAnimationStateError.InvalidCssAnimation, "A registered CSS animation could not be sampled.", registration.Target.SourceSpan); return false; }
            valueIndex = 0;
            while (valueIndex < css.Values.Count)
            {
                SvgCssAnimationSampleValue cssValue = css.Values[valueIndex];
                SvgAnimationSampleValue? value = FindValue(sample.Values, registration.Target, SvgAnimationTargetKind.CssProperty, cssValue.PropertyName);
                if (value == null) { StateFail(sample, SvgAnimationStateError.MissingBaseValue, "A CSS animation requires a registered base property value.", registration.Target.SourceSpan); return false; }
                value.AnimatedValue = cssValue.Value; value.HasAnimation = true; valueIndex = valueIndex + 1;
            }
            registrationIndex = registrationIndex + 1;
        }
        return true;
    }

    internal SvgAnimationStateCacheEntry? GetPlan(SvgAnimation animation, SvgAnimationSample sample)
    {
        int index = 0;
        while (index < (cache ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            if (object.ReferenceEquals(cache[index].Source, animation.Source)) return cache[index];
            index = index + 1;
        }
        if (cache.Count >= (Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaxCachedPlans) { StateFail(sample, SvgAnimationStateError.InvalidArgument, "The animation plan cache bound was exceeded.", (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan); return null; }
        SvgAnimationStateCacheEntry entry = new SvgAnimationStateCacheEntry(animation);
        if (animation.Kind == SvgAnimationKind.AnimateMotion)
        {
            entry.MotionPlan = new SvgMotionAnimation().ResolveWithAnimationAndOptions(animation, Options.Motion);
            AppendDiagnostics(sample.Diagnostics, entry.MotionPlan.Diagnostics);
            if (!entry.MotionPlan.Success) { StateFail(sample, SvgAnimationStateError.InvalidSmilAnimation, "A motion animation plan is invalid.", (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan); return null; }
        }
        else
        {
            entry.ValuePlan = new SvgAnimationComposer().ResolveWithAnimation(animation);
            AppendDiagnostics(sample.Diagnostics, entry.ValuePlan.Diagnostics);
            if (!entry.ValuePlan.Success) { StateFail(sample, SvgAnimationStateError.InvalidSmilAnimation, "A SMIL value plan is invalid.", (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan); return null; }
        }
        cache.Add(entry); CachedPlanCount = cache.Count; return entry;
    }

    internal int RemoveStaleCache()
    {
        int removed = 0; int index = (cache ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count - 1;
        while (index >= 0)
        {
            if (!cache[index].IsCurrent()) { cache.RemoveAt(index); removed = removed + 1; }
            index = index - 1;
        }
        CachedPlanCount = cache.Count; CacheInvalidations = CacheInvalidations + removed; return removed;
    }

    internal void CopyBaseValues(System.Collections.Generic.List<SvgAnimationSampleValue> destination)
    {
        int index = 0;
        while (index < (BaseValues ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            SvgAnimationBaseValue value = BaseValues[index]; destination.Add(new SvgAnimationSampleValue(value.Target, value.TargetKind, value.PropertyName, value.Value)); index = index + 1;
        }
    }

    internal void ReportDirty(SvgAnimationSample sample)
    {
        int index = 0;
        while (index < sample.Values.Count)
        {
            SvgAnimationSampleValue current = sample.Values[index];
            SvgAnimationSampleValue? previous = LastSample == null ? null : FindValue(LastSample.Values, current.Target, current.TargetKind, current.PropertyName);
            bool changed = previous == null ? current.HasAnimation : !SameValue(previous.AnimatedValue, current.AnimatedValue) || previous.HasAnimation != current.HasAnimation;
            if (changed) AddDirty(sample, current.Target);
            index = index + 1;
        }
        if (LastSample != null)
        {
            index = 0;
            while (index < LastSample.Values.Count)
            {
                SvgAnimationSampleValue previous = LastSample.Values[index];
                if (FindValue(sample.Values, previous.Target, previous.TargetKind, previous.PropertyName) == null) AddDirty(sample, previous.Target);
                index = index + 1;
            }
        }
        index = 0;
        while (index < sample.DiscardedNodes.Count) { AddDirty(sample, sample.DiscardedNodes[index]); index = index + 1; }
    }

    internal void AddDirty(SvgAnimationSample sample, SvgNode source)
    {
        if (!ContainsNode(sample.DirtySourceNodes, source)) sample.DirtySourceNodes.Add(source);
        if (Scene == null) return;
        int index = 0;
        while (index < (Scene.Nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            SvgSceneNode sceneNode = Scene.Nodes[index];
            if (object.ReferenceEquals(sceneNode.SourceNode, source) && !ContainsSceneNode(sample.DirtySceneNodes, sceneNode)) sample.DirtySceneNodes.Add(sceneNode);
            index = index + 1;
        }
    }

    internal int FindBase(SvgNode target, SvgAnimationTargetKind kind, string name)
    {
        int index = 0;
        while (index < (BaseValues ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            SvgAnimationBaseValue value = BaseValues[index];
            if (object.ReferenceEquals(value.Target, target) && value.TargetKind == kind && string.Equals(value.PropertyName, name, System.StringComparison.OrdinalIgnoreCase)) return index;
            index = index + 1;
        }
        return -1;
    }

    internal static SvgAnimationSampleValue? FindValue(System.Collections.Generic.List<SvgAnimationSampleValue> values, SvgNode? target, SvgAnimationTargetKind kind, string name)
    {
        int index = 0;
        while (index < values.Count)
        {
            SvgAnimationSampleValue value = values[index];
            if (object.ReferenceEquals(value.Target, target) && value.TargetKind == kind && string.Equals(value.PropertyName, name, System.StringComparison.OrdinalIgnoreCase)) return value;
            index = index + 1;
        }
        return null;
    }

    internal bool ValidState()
    {
        if (Document == null || Options == null || Options.Timeline == null || Options.Composition == null || Options.Motion == null || Options.Css == null || Options.MaxCachedPlans < 1) return false;
        return Options.ReducedMotionBehavior >= SvgAnimationReducedMotionBehavior.Preserve && Options.ReducedMotionBehavior <= SvgAnimationReducedMotionBehavior.Disable;
    }

    internal static bool SameValue(SvgAnimationValue first, SvgAnimationValue second)
    {
        return first != null && second != null && first.Kind == second.Kind && first.ToText() == second.ToText();
    }

    internal static bool ContainsNode(System.Collections.Generic.List<SvgNode> nodes, SvgNode target)
    {
        int index = 0; while (index < nodes.Count) { if (object.ReferenceEquals(nodes[index], target)) return true; index = index + 1; } return false;
    }

    internal static bool ContainsSceneNode(System.Collections.Generic.List<SvgSceneNode> nodes, SvgSceneNode target)
    {
        int index = 0; while (index < nodes.Count) { if (object.ReferenceEquals(nodes[index], target)) return true; index = index + 1; } return false;
    }

    internal static void CopyNodes(System.Collections.Generic.List<SvgNode> source, System.Collections.Generic.List<SvgNode> destination)
    {
        int index = 0; while (index < source.Count) { destination.Add(source[index]); index = index + 1; }
    }

    internal static void AppendDiagnostics(System.Collections.Generic.List<SvgDiagnostic> destination, System.Collections.Generic.List<SvgDiagnostic> source)
    {
        int index = 0; while (index < source.Count) { destination.Add(source[index]); index = index + 1; }
    }

    internal static void StateFail(SvgAnimationSample sample, SvgAnimationStateError error, string message, SvgSourceSpan? source)
    {
        sample.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, message, source)); sample.Error = error; sample.Success = false;
    }
}
