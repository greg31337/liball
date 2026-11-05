namespace svg;

/// <summary>Selects, validates, samples, accumulates, and composes SMIL animation values.</summary>
public class SvgAnimationComposer : ISvgAnimationComposer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgAnimationComposer()
    {
    }

    /// <summary>Builds a value plan using the animation target's interpolation category.</summary>
    /// <param name="animation">The target-resolved animation.</param>
    /// <returns>The validated value plan.</returns>
    public SvgAnimationValuePlan ResolveWithAnimation(SvgAnimation animation)
    {
        return new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(animation, new SvgAnimationComposer().InferKind(animation), new SvgAnimationCompositionOptions());
    }

    /// <summary>Builds a value plan for an explicit value family and bounded options.</summary>
    /// <param name="animation">The target-resolved animation.</param>
    /// <param name="kind">The requested typed value family.</param>
    /// <param name="options">The parsing and composition options.</param>
    /// <returns>The validated value plan.</returns>
    public SvgAnimationValuePlan ResolveWithAnimationAndKindAndOptions(SvgAnimation? animation, SvgAnimationValueKind kind, SvgAnimationCompositionOptions options)
    {
        SvgAnimation stableAnimation = animation == null ? new SvgAnimation(null) : animation;
        SvgAnimationValuePlan plan = new SvgAnimationValuePlan(stableAnimation, kind);
        if (animation == null || !animation.Success || !new SvgAnimationComposer().KnownKind(kind) || !new SvgAnimationComposer().ValidOptions(options))
        {
            new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.InvalidArgument, SvgDiagnosticCode.InvalidArgument, "Animation value composition arguments are invalid.", animation == null ? null : (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return plan;
        }
        new SvgAnimationComposer().ResetAnimationValues(animation, kind);
        new SvgAnimationComposer().SelectValues(plan, options);
        if (plan.Error != SvgAnimationCompositionError.None) return plan;
        new SvgAnimationComposer().ParseSettings(plan, options);
        if (plan.Error != SvgAnimationCompositionError.None) return plan;
        new SvgAnimationComposer().CopyPlanToAnimation(plan);
        plan.Success = true;
        return plan;
    }

    /// <summary>Samples and composes a validated plan at normalized simple progress.</summary>
    /// <param name="plan">The validated value plan.</param>
    /// <param name="underlying">The current underlying typed value, or <c>null</c> when unnecessary.</param>
    /// <param name="progress">The finite simple progress in the unit interval.</param>
    /// <param name="repeatIteration">The nonnegative zero-based repeat iteration.</param>
    /// <param name="options">The composition options.</param>
    /// <returns>The effect and final composed value.</returns>
    public SvgAnimationCompositionResult SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(SvgAnimationValuePlan? plan, SvgAnimationValue? underlying, double progress, int repeatIteration, SvgAnimationCompositionOptions options)
    {
        if (plan == null || !plan.Success || !SvgFiniteMath.IsFinite(progress) || progress < 0 || progress > 1 || repeatIteration < 0 || !new SvgAnimationComposer().ValidOptions(options)) return new SvgAnimationComposer().CompositionFailure(SvgAnimationCompositionError.InvalidArgument);
        if (repeatIteration > options.MaxRepeatAccumulations) return new SvgAnimationComposer().CompositionFailure(SvgAnimationCompositionError.LimitExceeded);
        System.Collections.Generic.List<SvgAnimationValue>? values = new SvgAnimationComposer().EffectiveValues(plan, underlying, options);
        if (values == null) return new SvgAnimationComposer().CompositionFailure(new SvgAnimationComposer().UnderlyingRequired(plan) ? SvgAnimationCompositionError.MissingUnderlyingValue : SvgAnimationCompositionError.IncompatibleValues);
        SvgAnimationInterpolationResult sampled = new SvgAnimationComposer().SampleValues(plan, values, progress, options);
        if (!sampled.Success) return new SvgAnimationComposer().CompositionFailure(new SvgAnimationComposer().MapInterpolationError(sampled.Error));
        SvgAnimationValue? effect = sampled.Value;
        if (plan.Accumulate == SvgAnimationAccumulate.Sum && repeatIteration > 0)
        {
            SvgAnimationValue? accumulated = new SvgAnimationComposer().Accumulate(effect, values[values.Count - 1], repeatIteration, options);
            if (accumulated == null) return new SvgAnimationComposer().CompositionFailure(SvgAnimationCompositionError.IncompatibleValues);
            effect = accumulated;
        }
        SvgAnimationValue? composed = effect;
        bool additive = plan.SourceKind == SvgAnimationValueSourceKind.By || plan.Additive == SvgAnimationAdditive.Sum && plan.SourceKind != SvgAnimationValueSourceKind.To && plan.Animation.Kind != SvgAnimationKind.Set;
        if (additive)
        {
            if (underlying == null) return new SvgAnimationComposer().CompositionFailure(SvgAnimationCompositionError.MissingUnderlyingValue);
            composed = new SvgAnimationComposer().AddValues(underlying, effect, true, options);
            if (composed == null) return new SvgAnimationComposer().CompositionFailure(SvgAnimationCompositionError.IncompatibleValues);
        }
        return new SvgAnimationCompositionResult(true, SvgAnimationCompositionError.None, effect, composed, sampled.SegmentIndex, sampled.EffectiveProgress, repeatIteration);
    }

    /// <summary>Samples using the current simple progress and repeat iteration of a timeline entry.</summary>
    /// <param name="plan">The validated value plan.</param>
    /// <param name="underlying">The current underlying typed value, or <c>null</c>.</param>
    /// <param name="timeline">The active or frozen timeline entry.</param>
    /// <param name="options">The composition options.</param>
    /// <returns>The effect and final composed value.</returns>
    public SvgAnimationCompositionResult SampleWithPlanAndUnderlyingAndTimelineAndOptions(SvgAnimationValuePlan plan, SvgAnimationValue? underlying, SvgAnimationTimelineEntry? timeline, SvgAnimationCompositionOptions options)
    {
        if (timeline == null || !timeline.Active && !timeline.Frozen) return new SvgAnimationComposer().CompositionFailure(SvgAnimationCompositionError.InvalidArgument);
        return new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(plan, underlying, timeline.SimpleProgress, timeline.RepeatIteration, options);
    }
void SelectValues(SvgAnimationValuePlan plan, SvgAnimationCompositionOptions options)
    {
        SvgNode source = (plan.Animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgAttribute? values = new SvgAnimationComposer().FindAttribute((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), "values");
        SvgAttribute? from = new SvgAnimationComposer().FindAttribute(source, SvgAttributeNames.From);
        SvgAttribute? to = new SvgAnimationComposer().FindAttribute(source, SvgAttributeNames.To);
        SvgAttribute? by = new SvgAnimationComposer().FindAttribute(source, SvgAttributeNames.By);
        if (plan.Animation.Kind == SvgAnimationKind.Set)
        {
            if (to == null)
            {
                new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.MissingValues, SvgDiagnosticCode.InvalidValue, "A set animation requires a to value.", source.SourceSpan);
                return;
            }
            plan.SourceKind = SvgAnimationValueSourceKind.To;
            SvgAnimationValue? parsedSet = new SvgAnimationComposer().ParseValue(plan, to, options);
            if (parsedSet != null) plan.Values.Add(parsedSet);
            return;
        }
        if (values != null)
        {
            plan.SourceKind = SvgAnimationValueSourceKind.Values;
            new SvgAnimationComposer().ParseValuesList(plan, values, options);
            return;
        }
        if (from != null && to != null)
        {
            plan.SourceKind = SvgAnimationValueSourceKind.FromTo;
            new SvgAnimationComposer().AddParsedPair(plan, from, to, options);
            return;
        }
        if (from != null && by != null)
        {
            plan.SourceKind = SvgAnimationValueSourceKind.FromBy;
            new SvgAnimationComposer().AddParsedPair(plan, from, by, options);
            return;
        }
        if (to != null)
        {
            plan.SourceKind = SvgAnimationValueSourceKind.To;
            SvgAnimationValue? parsedTo = new SvgAnimationComposer().ParseValue(plan, to, options);
            if (parsedTo != null) plan.Values.Add(parsedTo);
            return;
        }
        if (by != null)
        {
            plan.SourceKind = SvgAnimationValueSourceKind.By;
            SvgAnimationValue? parsedBy = new SvgAnimationComposer().ParseValue(plan, by, options);
            if (parsedBy != null) plan.Values.Add(parsedBy);
            return;
        }
        new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.MissingValues, SvgDiagnosticCode.InvalidValue, "The animation requires values, from/to, from/by, to, or by.", source.SourceSpan);
    }
void ParseValuesList(SvgAnimationValuePlan plan, SvgAttribute attribute, SvgAnimationCompositionOptions options)
    {
        System.Collections.Generic.List<string>? items = new SvgAnimationComposer().SplitValues(attribute.Value, options.MaxValues);
        if (items == null)
        {
            new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.LimitExceeded, SvgDiagnosticCode.InputLimitExceeded, "The values list is empty, malformed, or exceeds MaxValues.", attribute.SourceSpan);
            return;
        }
        int index = 0;
        while (index < items.Count)
        {
            SvgAnimationValueParseResult parsed = new SvgAnimationInterpolator().ParseWithKindAndTextAndOptions(plan.ValueKind, items[index], options.Interpolation);
            if (!parsed.Success)
            {
                SvgAnimationCompositionError error = parsed.Error == SvgAnimationInterpolationError.LimitExceeded ? SvgAnimationCompositionError.LimitExceeded : SvgAnimationCompositionError.InvalidValue;
                SvgDiagnosticCode code = error == SvgAnimationCompositionError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
                new SvgAnimationComposer().Fail(plan, error, code, "One values-list item is invalid for the selected value family.", attribute.SourceSpan);
                return;
            }
            plan.Values.Add((parsed.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            index = index + 1;
        }
    }
void AddParsedPair(SvgAnimationValuePlan plan, SvgAttribute first, SvgAttribute second, SvgAnimationCompositionOptions options)
    {
        SvgAnimationValue? firstValue = new SvgAnimationComposer().ParseValue(plan, first, options);
        if (firstValue == null) return;
        SvgAnimationValue? secondValue = new SvgAnimationComposer().ParseValue(plan, second, options);
        if (secondValue == null) return;
        plan.Values.Add(firstValue);
        plan.Values.Add(secondValue);
    }
SvgAnimationValue? ParseValue(SvgAnimationValuePlan plan, SvgAttribute attribute, SvgAnimationCompositionOptions options)
    {
        SvgAnimationValueParseResult parsed = new SvgAnimationInterpolator().ParseWithKindAndTextAndOptions(plan.ValueKind, attribute.Value, options.Interpolation);
        if (parsed.Success) return parsed.Value;
        SvgAnimationCompositionError error = parsed.Error == SvgAnimationInterpolationError.LimitExceeded ? SvgAnimationCompositionError.LimitExceeded : SvgAnimationCompositionError.InvalidValue;
        SvgDiagnosticCode code = error == SvgAnimationCompositionError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
        new SvgAnimationComposer().Fail(plan, error, code, "An animation value is invalid for the selected value family.", attribute.SourceSpan);
        return null;
    }
void ParseSettings(SvgAnimationValuePlan plan, SvgAnimationCompositionOptions options)
    {
        SvgNode source = (plan.Animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        plan.CalcMode = new SvgAnimationComposer().DefaultCalcMode(plan.Animation.Kind);
        SvgAttribute? calcMode = new SvgAnimationComposer().FindAttribute((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.CalcMode);
        if (calcMode != null)
        {
            SvgAnimationKeywordParseResult parsedMode = new SvgAnimationKeywordParser().ParseCalcMode(calcMode.Value);
            if (!parsedMode.Success)
            {
                new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.InvalidValue, SvgDiagnosticCode.InvalidValue, "calcMode is invalid.", calcMode.SourceSpan);
                return;
            }
            plan.CalcMode = (SvgAnimationCalcMode)parsedMode.Value;
        }
        if (plan.Animation.Kind == SvgAnimationKind.Set) plan.CalcMode = SvgAnimationCalcMode.Discrete;
        plan.Additive = new SvgAnimationComposer().ParseAdditive(plan, new SvgAnimationComposer().FindAttribute(source, SvgAttributeNames.Additive));
        if (plan.Error != SvgAnimationCompositionError.None) return;
        plan.Accumulate = new SvgAnimationComposer().ParseAccumulate(plan, new SvgAnimationComposer().FindAttribute(source, SvgAttributeNames.Accumulate));
        if (plan.Error != SvgAnimationCompositionError.None) return;
        if ((plan.SourceKind == SvgAnimationValueSourceKind.FromBy || plan.SourceKind == SvgAnimationValueSourceKind.By || plan.Additive == SvgAnimationAdditive.Sum || plan.Accumulate == SvgAnimationAccumulate.Sum) && !new SvgAnimationComposer().PlanSupportsAddition(plan))
        {
            new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.InvalidComposition, SvgDiagnosticCode.UnsupportedFeature, "The selected value family does not support additive or accumulated animation.", source.SourceSpan);
            return;
        }
        int keyframeCount = new SvgAnimationComposer().EffectiveKeyframeCount(plan);
        new SvgAnimationComposer().ParseKeyTimes(plan, new SvgAnimationComposer().FindAttribute(source, SvgAttributeNames.KeyTimes), keyframeCount, options);
        if (plan.Error != SvgAnimationCompositionError.None) return;
        new SvgAnimationComposer().ParseKeySplines(plan, new SvgAnimationComposer().FindAttribute(source, SvgAttributeNames.KeySplines), keyframeCount, options);
        if (plan.Error != SvgAnimationCompositionError.None) return;
        new SvgAnimationComposer().ParseKeyPoints(plan, new SvgAnimationComposer().FindAttribute(source, SvgAttributeNames.KeyPoints), new SvgAnimationComposer().FindAttribute(source, SvgAttributeNames.KeyTimes) != null, options);
    }
SvgAnimationAdditive ParseAdditive(SvgAnimationValuePlan plan, SvgAttribute? attribute)
    {
        if (attribute == null) return SvgAnimationAdditive.Replace;
        SvgAnimationKeywordParseResult parsed = new SvgAnimationKeywordParser().ParseAdditive(attribute.Value);
        if (parsed.Success) return (SvgAnimationAdditive)parsed.Value;
        new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.InvalidComposition, SvgDiagnosticCode.InvalidValue, "additive must be replace or sum.", attribute.SourceSpan);
        return SvgAnimationAdditive.Replace;
    }
SvgAnimationAccumulate ParseAccumulate(SvgAnimationValuePlan plan, SvgAttribute? attribute)
    {
        if (attribute == null) return SvgAnimationAccumulate.None;
        SvgAnimationKeywordParseResult parsed = new SvgAnimationKeywordParser().ParseAccumulate(attribute.Value);
        if (parsed.Success) return (SvgAnimationAccumulate)parsed.Value;
        new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.InvalidComposition, SvgDiagnosticCode.InvalidValue, "accumulate must be none or sum.", attribute.SourceSpan);
        return SvgAnimationAccumulate.None;
    }
void ParseKeyTimes(SvgAnimationValuePlan plan, SvgAttribute? attribute, int count, SvgAnimationCompositionOptions options)
    {
        if (attribute == null)
        {
            plan.KeyTimes = new SvgAnimationComposer().DefaultKeyTimes(count, plan.CalcMode);
            return;
        }
        SvgKeyValueListParseResult parsed = new SvgAnimationValueParser().ParseKeyTimes(attribute.Value, options.MaxValues, options.Interpolation.MaxTokenCharacters);
        if (!parsed.Success)
        {
            new SvgAnimationComposer().Fail(plan, parsed.Error == SvgTimingParseError.ItemLimitExceeded || parsed.Error == SvgTimingParseError.TokenTooLong ? SvgAnimationCompositionError.LimitExceeded : SvgAnimationCompositionError.InvalidKeyTimes, parsed.Error == SvgTimingParseError.ItemLimitExceeded || parsed.Error == SvgTimingParseError.TokenTooLong ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue, "keyTimes is invalid.", attribute.SourceSpan);
            return;
        }
        if (plan.CalcMode != SvgAnimationCalcMode.Paced)
        {
            bool endpointValid = parsed.Values.Length > 0 && parsed.Values[0] == 0;
            if (plan.CalcMode != SvgAnimationCalcMode.Discrete && count > 1) endpointValid = endpointValid && parsed.Values[parsed.Values.Length - 1] == 1;
            if (parsed.Values.Length != count || !endpointValid)
            {
                new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.InvalidKeyTimes, SvgDiagnosticCode.InvalidValue, "keyTimes must match the effective keyframe count and required endpoints.", attribute.SourceSpan);
                return;
            }
        }
        plan.KeyTimes = parsed.Values;
    }
void ParseKeySplines(SvgAnimationValuePlan plan, SvgAttribute? attribute, int count, SvgAnimationCompositionOptions options)
    {
        if (attribute == null)
        {
            if (plan.CalcMode == SvgAnimationCalcMode.Spline && count > 1) new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.InvalidKeySplines, SvgDiagnosticCode.InvalidValue, "Spline interpolation requires keySplines.", (plan.Animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return;
        }
        SvgKeySplineListParseResult parsed = new SvgAnimationValueParser().ParseKeySplines(attribute.Value, options.MaxValues, options.Interpolation.MaxTokenCharacters);
        if (!parsed.Success)
        {
            new SvgAnimationComposer().Fail(plan, parsed.Error == SvgTimingParseError.ItemLimitExceeded || parsed.Error == SvgTimingParseError.TokenTooLong ? SvgAnimationCompositionError.LimitExceeded : SvgAnimationCompositionError.InvalidKeySplines, parsed.Error == SvgTimingParseError.ItemLimitExceeded || parsed.Error == SvgTimingParseError.TokenTooLong ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue, "keySplines is invalid.", attribute.SourceSpan);
            return;
        }
        if (plan.CalcMode == SvgAnimationCalcMode.Spline && parsed.Splines.Count != count - 1)
        {
            new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.InvalidKeySplines, SvgDiagnosticCode.InvalidValue, "keySplines must contain one spline per keyframe segment.", attribute.SourceSpan);
            return;
        }
        int index = 0;
        while (index < parsed.Splines.Count)
        {
            plan.KeySplines.Add(parsed.Splines[index]);
            index = index + 1;
        }
    }
void ParseKeyPoints(SvgAnimationValuePlan plan, SvgAttribute? attribute, bool hasAuthoredKeyTimes, SvgAnimationCompositionOptions options)
    {
        if (attribute == null) return;
        SvgKeyValueListParseResult parsed = new SvgAnimationValueParser().ParseKeyPoints(attribute.Value, options.MaxValues, options.Interpolation.MaxTokenCharacters);
        if (!parsed.Success)
        {
            new SvgAnimationComposer().Fail(plan, parsed.Error == SvgTimingParseError.ItemLimitExceeded || parsed.Error == SvgTimingParseError.TokenTooLong ? SvgAnimationCompositionError.LimitExceeded : SvgAnimationCompositionError.InvalidKeyPoints, parsed.Error == SvgTimingParseError.ItemLimitExceeded || parsed.Error == SvgTimingParseError.TokenTooLong ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue, "keyPoints is invalid.", attribute.SourceSpan);
            return;
        }
        if (plan.Animation.Kind != SvgAnimationKind.AnimateMotion || !hasAuthoredKeyTimes || plan.KeyTimes.Length == 0 || parsed.Values.Length != plan.KeyTimes.Length)
        {
            new SvgAnimationComposer().Fail(plan, SvgAnimationCompositionError.InvalidKeyPoints, SvgDiagnosticCode.InvalidValue, "keyPoints applies only to motion animation and must match keyTimes.", attribute.SourceSpan);
            return;
        }
        plan.KeyPoints = parsed.Values;
    }
SvgAnimationInterpolationResult SampleValues(SvgAnimationValuePlan plan, System.Collections.Generic.List<SvgAnimationValue> values, double progress, SvgAnimationCompositionOptions options)
    {
        if (values.Count == 1) return new SvgAnimationInterpolationResult(true, SvgAnimationInterpolationError.None, values[0], 0, 0, 0);
        if (plan.CalcMode == SvgAnimationCalcMode.Paced) return new SvgAnimationInterpolator().InterpolateSequence(values, progress, SvgAnimationCalcMode.Paced, new System.Collections.Generic.List<SvgKeySpline>(), options.Interpolation);
        if (plan.CalcMode == SvgAnimationCalcMode.Discrete)
        {
            int selected = new SvgAnimationComposer().DiscreteIndex(plan.KeyTimes, progress, values.Count);
            return new SvgAnimationInterpolationResult(true, SvgAnimationInterpolationError.None, values[selected], 0, selected, 0);
        }
        int segment = new SvgAnimationComposer().SegmentIndex(plan.KeyTimes, progress);
        double start = plan.KeyTimes[segment];
        double end = plan.KeyTimes[segment + 1];
        double local = end == start ? 0 : (progress - start) / (end - start);
        if (local < 0) local = 0;
        if (local > 1) local = 1;
        SvgAnimationInterpolationOptions interpolation = new SvgAnimationComposer().CopyInterpolationOptions(options.Interpolation);
        SvgAnimationCalcMode mode = SvgAnimationCalcMode.Linear;
        if (plan.CalcMode == SvgAnimationCalcMode.Spline)
        {
            interpolation.Spline = plan.KeySplines[segment];
            mode = SvgAnimationCalcMode.Spline;
        }
        SvgAnimationInterpolationResult result = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndModeAndOptions(values[segment], values[segment + 1], local, mode, interpolation);
        if (result.Success) result.SegmentIndex = segment;
        return result;
    }
System.Collections.Generic.List<SvgAnimationValue>? EffectiveValues(SvgAnimationValuePlan plan, SvgAnimationValue? underlying, SvgAnimationCompositionOptions options)
    {
        System.Collections.Generic.List<SvgAnimationValue> result = new System.Collections.Generic.List<SvgAnimationValue>();
        if (plan.SourceKind == SvgAnimationValueSourceKind.Values || plan.SourceKind == SvgAnimationValueSourceKind.FromTo)
        {
            new SvgAnimationComposer().CopyValues(plan.Values, result);
            return result;
        }
        if (plan.SourceKind == SvgAnimationValueSourceKind.FromBy)
        {
            SvgAnimationValue? endpoint = new SvgAnimationComposer().AddValues(plan.Values[0], plan.Values[1], false, options);
            if (endpoint == null) return null;
            result.Add(plan.Values[0]);
            result.Add(endpoint);
            return result;
        }
        if (plan.SourceKind == SvgAnimationValueSourceKind.To)
        {
            if (plan.Animation.Kind == SvgAnimationKind.Set)
            {
                result.Add(plan.Values[0]);
                return result;
            }
            if (underlying == null || underlying.Kind != plan.ValueKind) return null;
            result.Add(underlying);
            result.Add(plan.Values[0]);
            return result;
        }
        if (plan.SourceKind == SvgAnimationValueSourceKind.By)
        {
            if (underlying == null || underlying.Kind != plan.ValueKind) return null;
            SvgAnimationValue? zero = new SvgAnimationComposer().ZeroLike(plan.Values[0]);
            if (zero == null) return null;
            result.Add(zero);
            result.Add(plan.Values[0]);
            return result;
        }
        return null;
    }
SvgAnimationValue? Accumulate(SvgAnimationValue? effect, SvgAnimationValue finalValue, int repeat, SvgAnimationCompositionOptions options)
    {
        SvgAnimationValue? scaled = new SvgAnimationComposer().ScaleValue(finalValue, repeat);
        return scaled == null ? null : new SvgAnimationComposer().AddValues(effect, scaled, false, options);
    }
SvgAnimationValue? AddValues(SvgAnimationValue? first, SvgAnimationValue? second, bool composeTransform, SvgAnimationCompositionOptions options)
    {
        if (first == null || second == null || first.Kind != second.Kind) return null;
        if (first.Kind == SvgAnimationValueKind.Number) return new SvgAnimationComposer().FiniteNumber(first.Number + second.Number);
        if (first.Kind == SvgAnimationValueKind.Length) return new SvgAnimationComposer().AddLengths(first.Length, second.Length, options);
        if (first.Kind == SvgAnimationValueKind.Color) return new SvgAnimationComposer().AddColors(first.Color, second.Color, options.Interpolation.ColorSpace, false);
        if (first.Kind == SvgAnimationValueKind.Points) return new SvgAnimationComposer().AddPoints(first.Points, second.Points);
        if (first.Kind == SvgAnimationValueKind.ViewBox) return new SvgAnimationComposer().AddViewBoxes(first.ViewBox, second.ViewBox);
        if (first.Kind == SvgAnimationValueKind.Transform) return new SvgAnimationComposer().AddTransforms(first.Transform, second.Transform, composeTransform);
        if (first.Kind == SvgAnimationValueKind.Paint && (first.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind == SvgPaintKind.Color && (second.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind == SvgPaintKind.Color) return new SvgAnimationComposer().AddColors(first.Paint.Color, second.Paint.Color, options.Interpolation.ColorSpace, true);
        return null;
    }
SvgAnimationValue? AddLengths(SvgLength? first, SvgLength? second, SvgAnimationCompositionOptions options)
    {
        if (first == null || second == null) return null;
        if (first.Unit == second.Unit)
        {
            double sum = first.Value + second.Value;
            return SvgFiniteMath.IsFinite(sum) ? SvgAnimationValue.FromLength(new SvgLength(sum, first.Unit)) : null;
        }
        SvgLengthEvaluationResult a = first.Evaluate(options.Interpolation.LengthContext, options.Interpolation.LengthPercentageBasis);
        SvgLengthEvaluationResult b = second.Evaluate(options.Interpolation.LengthContext, options.Interpolation.LengthPercentageBasis);
        double resolved = a.Value + b.Value;
        return a.Success && b.Success && SvgFiniteMath.IsFinite(resolved) ? SvgAnimationValue.FromLength(new SvgLength(resolved, SvgLengthUnit.Number)) : null;
    }
SvgAnimationValue? AddColors(SvgColor? first, SvgColor? second, SvgColorSpace space, bool paint)
    {
        if (first == null || second == null || first.IsCurrentColor || second.IsCurrentColor) return null;
        SvgColorOperationResult a = first.ConvertTo(space);
        SvgColorOperationResult b = second.ConvertTo(space);
        if (!a.Success || !b.Success) return null;
        double red = (a.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red + (b.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red;
        double green = a.Color.Green + b.Color.Green;
        double blue = a.Color.Blue + b.Color.Blue;
        double alpha = a.Color.Alpha + b.Color.Alpha;
        if (!SvgFiniteMath.IsFinite(red) || !SvgFiniteMath.IsFinite(green) || !SvgFiniteMath.IsFinite(blue) || !SvgFiniteMath.IsFinite(alpha)) return null;
        if (alpha > 1) alpha = 1;
        SvgColor color = new SvgColor(space, red, green, blue, alpha);
        return paint ? SvgAnimationValue.FromPaint(SvgPaint.Solid(color)) : SvgAnimationValue.FromColor(color);
    }
SvgAnimationValue? AddPoints(System.Collections.Generic.List<SvgPoint>? first, System.Collections.Generic.List<SvgPoint>? second)
    {
        if (first == null || second == null || first.Count != second.Count) return null;
        System.Collections.Generic.List<SvgPoint> result = new System.Collections.Generic.List<SvgPoint>();
        int index = 0;
        while (index < first.Count)
        {
            if (first[index] == null || second[index] == null) return null;
            double x = first[index].X + second[index].X;
            double y = first[index].Y + second[index].Y;
            if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return null;
            result.Add(new SvgPoint(x, y));
            index = index + 1;
        }
        return SvgAnimationValue.FromPoints(result);
    }
SvgAnimationValue? AddViewBoxes(SvgViewBox? first, SvgViewBox? second)
    {
        if (first == null || second == null) return null;
        double x = first.X + second.X;
        double y = first.Y + second.Y;
        double width = first.Width + second.Width;
        double height = first.Height + second.Height;
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y) || !SvgFiniteMath.IsFinite(width) || !SvgFiniteMath.IsFinite(height)) return null;
        return SvgAnimationValue.FromViewBox(new SvgViewBox(x, y, width, height));
    }
SvgAnimationValue? AddTransforms(SvgTransformList? first, SvgTransformList? second, bool compose)
    {
        if (first == null || second == null) return null;
        System.Collections.Generic.List<SvgTransform> result = new System.Collections.Generic.List<SvgTransform>();
        if (compose)
        {
            new SvgAnimationComposer().CopyTransforms(first.Items, result);
            new SvgAnimationComposer().CopyTransforms(second.Items, result);
            return SvgAnimationValue.FromTransform(new SvgTransformList(result));
        }
        if (first.Items.Count != second.Items.Count) return null;
        int index = 0;
        while (index < first.Items.Count)
        {
            SvgTransform a = first.Items[index];
            SvgTransform b = second.Items[index];
            if (a.Kind != b.Kind || a.Values.Length != b.Values.Length) return null;
            double[] values = new double[a.Values.Length];
            int valueIndex = 0;
            while (valueIndex < values.Length)
            {
                values[valueIndex] = a.Values[valueIndex] + b.Values[valueIndex];
                if (!SvgFiniteMath.IsFinite(values[valueIndex])) return null;
                valueIndex = valueIndex + 1;
            }
            result.Add(new SvgTransform(a.Kind, values));
            index = index + 1;
        }
        return SvgAnimationValue.FromTransform(new SvgTransformList(result));
    }
SvgAnimationValue? ScaleValue(SvgAnimationValue value, int factor)
    {
        if (value.Kind == SvgAnimationValueKind.Number) return new SvgAnimationComposer().FiniteNumber(value.Number * factor);
        if (value.Kind == SvgAnimationValueKind.Length)
        {
            double length = (value.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value * factor;
            return SvgFiniteMath.IsFinite(length) ? SvgAnimationValue.FromLength(new SvgLength(length, value.Length.Unit)) : null;
        }
        if (value.Kind == SvgAnimationValueKind.Color)
        {
            double red = (value.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red * factor;
            double green = value.Color.Green * factor;
            double blue = value.Color.Blue * factor;
            double alpha = value.Color.Alpha * factor;
            if (!SvgFiniteMath.IsFinite(red) || !SvgFiniteMath.IsFinite(green) || !SvgFiniteMath.IsFinite(blue) || !SvgFiniteMath.IsFinite(alpha)) return null;
            if (alpha > 1) alpha = 1;
            return SvgAnimationValue.FromColor(new SvgColor(value.Color.Space, red, green, blue, alpha));
        }
        if (value.Kind == SvgAnimationValueKind.Points)
        {
            System.Collections.Generic.List<SvgPoint> points = new System.Collections.Generic.List<SvgPoint>();
            int index = 0;
            while (index < value.Points.Count)
            {
                double x = value.Points[index].X * factor;
                double y = value.Points[index].Y * factor;
                if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return null;
                points.Add(new SvgPoint(x, y));
                index = index + 1;
            }
            return SvgAnimationValue.FromPoints(points);
        }
        if (value.Kind == SvgAnimationValueKind.ViewBox)
        {
            double x = (value.ViewBox ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X * factor;
            double y = value.ViewBox.Y * factor;
            double width = value.ViewBox.Width * factor;
            double height = value.ViewBox.Height * factor;
            if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y) || !SvgFiniteMath.IsFinite(width) || !SvgFiniteMath.IsFinite(height)) return null;
            return SvgAnimationValue.FromViewBox(new SvgViewBox(x, y, width, height));
        }
        if (value.Kind == SvgAnimationValueKind.Transform)
        {
            System.Collections.Generic.List<SvgTransform> transforms = new System.Collections.Generic.List<SvgTransform>();
            int itemIndex = 0;
            while (itemIndex < (value.Transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Items.Count)
            {
                double[] parameters = new double[value.Transform.Items[itemIndex].Values.Length];
                int parameterIndex = 0;
                while (parameterIndex < parameters.Length)
                {
                    parameters[parameterIndex] = value.Transform.Items[itemIndex].Values[parameterIndex] * factor;
                    if (!SvgFiniteMath.IsFinite(parameters[parameterIndex])) return null;
                    parameterIndex = parameterIndex + 1;
                }
                transforms.Add(new SvgTransform(value.Transform.Items[itemIndex].Kind, parameters));
                itemIndex = itemIndex + 1;
            }
            return SvgAnimationValue.FromTransform(new SvgTransformList(transforms));
        }
        if (value.Kind == SvgAnimationValueKind.Paint && (value.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind == SvgPaintKind.Color)
        {
            double red = (value.Paint.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red * factor;
            double green = value.Paint.Color.Green * factor;
            double blue = value.Paint.Color.Blue * factor;
            double alpha = value.Paint.Color.Alpha * factor;
            if (!SvgFiniteMath.IsFinite(red) || !SvgFiniteMath.IsFinite(green) || !SvgFiniteMath.IsFinite(blue) || !SvgFiniteMath.IsFinite(alpha)) return null;
            if (alpha > 1) alpha = 1;
            SvgColor color = new SvgColor(value.Paint.Color.Space, red, green, blue, alpha);
            return SvgAnimationValue.FromPaint(SvgPaint.Solid(color));
        }
        return null;
    }
SvgAnimationValue? ZeroLike(SvgAnimationValue value)
    {
        if (value.Kind == SvgAnimationValueKind.Number) return SvgAnimationValue.FromNumber(0);
        if (value.Kind == SvgAnimationValueKind.Length) return SvgAnimationValue.FromLength(new SvgLength(0, (value.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Unit));
        if (value.Kind == SvgAnimationValueKind.Color) return SvgAnimationValue.FromColor(new SvgColor((value.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Space, 0, 0, 0, 0));
        if (value.Kind == SvgAnimationValueKind.Points)
        {
            System.Collections.Generic.List<SvgPoint> points = new System.Collections.Generic.List<SvgPoint>();
            int index = 0;
            while (index < value.Points.Count)
            {
                points.Add(new SvgPoint(0, 0));
                index = index + 1;
            }
            return SvgAnimationValue.FromPoints(points);
        }
        if (value.Kind == SvgAnimationValueKind.ViewBox) return SvgAnimationValue.FromViewBox(new SvgViewBox(0, 0, 0, 0));
        if (value.Kind == SvgAnimationValueKind.Transform) return new SvgAnimationComposer().ZeroTransform(value.Transform);
        if (value.Kind == SvgAnimationValueKind.Paint && (value.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind == SvgPaintKind.Color) return SvgAnimationValue.FromPaint(SvgPaint.Solid(new SvgColor((value.Paint.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Space, 0, 0, 0, 0)));
        return null;
    }
SvgAnimationValue? ZeroTransform(SvgTransformList? source)
    {
        if (source == null) return null;
        System.Collections.Generic.List<SvgTransform> result = new System.Collections.Generic.List<SvgTransform>();
        int index = 0;
        while (index < source.Items.Count)
        {
            SvgTransform item = source.Items[index];
            double[] values = new double[item.Values.Length];
            if (item.Kind == SvgTransformKind.Matrix)
            {
                values[0] = 1;
                values[3] = 1;
            }
            else if (item.Kind == SvgTransformKind.Scale)
            {
                values[0] = 1;
                if (values.Length > 1) values[1] = 1;
            }
            else if (item.Kind == SvgTransformKind.Rotate && values.Length == 3)
            {
                values[1] = item.Values[1];
                values[2] = item.Values[2];
            }
            result.Add(new SvgTransform(item.Kind, values));
            index = index + 1;
        }
        return SvgAnimationValue.FromTransform(new SvgTransformList(result));
    }
int DiscreteIndex(double[] times, double progress, int count)
    {
        int selected = 0;
        int index = 0;
        while (index < times.Length && index < count)
        {
            if (progress >= times[index]) selected = index;
            index = index + 1;
        }
        if (selected >= count) selected = count - 1;
        return selected;
    }
int SegmentIndex(double[] times, double progress)
    {
        if (progress >= 1) return times.Length - 2;
        int index = 0;
        while (index + 1 < times.Length)
        {
            if (progress >= times[index] && progress < times[index + 1]) return index;
            index = index + 1;
        }
        return times.Length - 2;
    }
double[] DefaultKeyTimes(int count, SvgAnimationCalcMode mode)
    {
        double[] result = new double[count];
        if (count == 1)
        {
            result[0] = 0;
            return result;
        }
        int index = 0;
        while (index < count)
        {
            result[index] = mode == SvgAnimationCalcMode.Discrete ? (double)index / count : (double)index / (count - 1);
            index = index + 1;
        }
        return result;
    }
int EffectiveKeyframeCount(SvgAnimationValuePlan plan)
    {
        if (plan.SourceKind == SvgAnimationValueSourceKind.Values) return plan.Values.Count;
        if (plan.Animation.Kind == SvgAnimationKind.Set) return 1;
        return 2;
    }
SvgAnimationCalcMode DefaultCalcMode(SvgAnimationKind kind)
    {
        if (kind == SvgAnimationKind.AnimateMotion) return SvgAnimationCalcMode.Paced;
        if (kind == SvgAnimationKind.Set) return SvgAnimationCalcMode.Discrete;
        return SvgAnimationCalcMode.Linear;
    }
SvgAnimationValueKind InferKind(SvgAnimation? animation)
    {
        if (animation == null) return SvgAnimationValueKind.String;
        if (animation.TargetKind == SvgAnimationTargetKind.Motion) return SvgAnimationValueKind.Points;
        if (animation.AnimationType == SvgPropertyAnimationType.Transform) return SvgAnimationValueKind.Transform;
        if (animation.AnimationType == SvgPropertyAnimationType.Number) return SvgAnimationValueKind.Number;
        if (animation.AnimationType == SvgPropertyAnimationType.Length) return SvgAnimationValueKind.Length;
        if (animation.AnimationType == SvgPropertyAnimationType.Color) return SvgAnimationValueKind.Color;
        if (animation.AnimationType == SvgPropertyAnimationType.Paint) return SvgAnimationValueKind.Paint;
        if (animation.LocalName == SvgAttributeNames.Points) return SvgAnimationValueKind.Points;
        if (animation.LocalName == SvgAttributeNames.ViewBox) return SvgAnimationValueKind.ViewBox;
        if (animation.LocalName == SvgAttributeNames.D || animation.LocalName == SvgAttributeNames.Path) return SvgAnimationValueKind.Path;
        return SvgAnimationValueKind.String;
    }
bool AddableKind(SvgAnimationValueKind kind)
    {
        return kind == SvgAnimationValueKind.Number || kind == SvgAnimationValueKind.Length || kind == SvgAnimationValueKind.Color || kind == SvgAnimationValueKind.Points || kind == SvgAnimationValueKind.Transform || kind == SvgAnimationValueKind.ViewBox || kind == SvgAnimationValueKind.Paint;
    }
bool PlanSupportsAddition(SvgAnimationValuePlan plan)
    {
        if (!new SvgAnimationComposer().AddableKind(plan.ValueKind)) return false;
        int index = 0;
        while (index < plan.Values.Count)
        {
            SvgAnimationValue value = plan.Values[index];
            if (plan.ValueKind == SvgAnimationValueKind.Color && (value.Color == null || value.Color.IsCurrentColor)) return false;
            if (plan.ValueKind == SvgAnimationValueKind.Paint && (value.Paint == null || value.Paint.Kind != SvgPaintKind.Color || (value.Paint.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IsCurrentColor)) return false;
            index = index + 1;
        }
        return true;
    }
bool UnderlyingRequired(SvgAnimationValuePlan plan)
    {
        return plan.SourceKind == SvgAnimationValueSourceKind.To || plan.SourceKind == SvgAnimationValueSourceKind.By || plan.Additive == SvgAnimationAdditive.Sum;
    }
System.Collections.Generic.List<string>? SplitValues(string text, int maximum)
    {
        if (text == null || maximum < 1) return null;
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        int start = 0;
        int index = 0;
        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == ';')
            {
                if (result.Count >= maximum) return null;
                string item = text.Substring(start, index - start).Trim();
                if (item.Length == 0) return null;
                result.Add(item);
                start = index + 1;
            }
            index = index + 1;
        }
        return result.Count == 0 ? null : result;
    }
SvgAnimationInterpolationOptions CopyInterpolationOptions(SvgAnimationInterpolationOptions source)
    {
        SvgAnimationInterpolationOptions result = new SvgAnimationInterpolationOptions();
        result.LengthContext = source.LengthContext;
        result.LengthPercentageBasis = source.LengthPercentageBasis;
        result.ColorSpace = source.ColorSpace;
        result.Spline = source.Spline;
        result.MaxItems = source.MaxItems;
        result.MaxTokenCharacters = source.MaxTokenCharacters;
        return result;
    }
void CopyValues(System.Collections.Generic.List<SvgAnimationValue> source, System.Collections.Generic.List<SvgAnimationValue> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
void CopyTransforms(System.Collections.Generic.List<SvgTransform> source, System.Collections.Generic.List<SvgTransform> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
void ResetAnimationValues(SvgAnimation animation, SvgAnimationValueKind kind)
    {
        animation.ValueKind = kind;
        animation.Values.Clear();
        animation.FromValue = null;
        animation.ToValue = null;
        animation.ByValue = null;
        animation.KeyTimes = new double[0];
        animation.KeySplines.Clear();
        animation.KeyPoints = new double[0];
    }
void CopyPlanToAnimation(SvgAnimationValuePlan plan)
    {
        SvgAnimation animation = plan.Animation;
        animation.ValueKind = plan.ValueKind;
        new SvgAnimationComposer().CopyValues(plan.Values, animation.Values);
        if (plan.SourceKind == SvgAnimationValueSourceKind.FromTo || plan.SourceKind == SvgAnimationValueSourceKind.FromBy) animation.FromValue = plan.Values[0];
        if (plan.SourceKind == SvgAnimationValueSourceKind.FromTo || plan.SourceKind == SvgAnimationValueSourceKind.To) animation.ToValue = plan.Values[plan.Values.Count - 1];
        if (plan.SourceKind == SvgAnimationValueSourceKind.FromBy || plan.SourceKind == SvgAnimationValueSourceKind.By) animation.ByValue = plan.Values[plan.Values.Count - 1];
        animation.CalcMode = plan.CalcMode;
        animation.KeyTimes = (double[])plan.KeyTimes.Clone();
        int index = 0;
        while (index < plan.KeySplines.Count)
        {
            animation.KeySplines.Add(plan.KeySplines[index]);
            index = index + 1;
        }
        animation.KeyPoints = (double[])plan.KeyPoints.Clone();
        animation.Additive = plan.Additive;
        animation.Accumulate = plan.Accumulate;
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
bool ValidOptions(SvgAnimationCompositionOptions options)
    {
        return options != null && options.Interpolation != null && options.MaxValues > 0 && options.MaxRepeatAccumulations >= 0;
    }
bool KnownKind(SvgAnimationValueKind kind)
    {
        return kind >= SvgAnimationValueKind.Number && kind <= SvgAnimationValueKind.String;
    }
SvgAnimationCompositionError MapInterpolationError(SvgAnimationInterpolationError error)
    {
        if (error == SvgAnimationInterpolationError.LimitExceeded) return SvgAnimationCompositionError.LimitExceeded;
        if (error == SvgAnimationInterpolationError.IncompatibleValues || error == SvgAnimationInterpolationError.UnsupportedInterpolation) return SvgAnimationCompositionError.IncompatibleValues;
        return SvgAnimationCompositionError.InvalidValue;
    }
void Fail(SvgAnimationValuePlan plan, SvgAnimationCompositionError error, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source);
        plan.Diagnostics.Add(diagnostic);
        if (plan.Animation != null) plan.Animation.Diagnostics.Add(diagnostic);
        plan.Error = error;
        plan.Success = false;
    }
SvgAnimationCompositionResult CompositionFailure(SvgAnimationCompositionError error)
    {
        return new SvgAnimationCompositionResult(false, error, null, null, -1, 0, 0);
    }
SvgAnimationValue? FiniteNumber(double value)
    {
        return SvgFiniteMath.IsFinite(value) ? SvgAnimationValue.FromNumber(value) : null;
    }
}
