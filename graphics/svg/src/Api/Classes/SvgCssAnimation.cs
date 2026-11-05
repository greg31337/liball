namespace svg;

/// <summary>Selects CSS keyframes and samples ordered animation effects at caller-controlled time.</summary>
public class SvgCssAnimation : ISvgCssAnimation
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCssAnimation()
    {
    }

    /// <summary>Builds a CSS animation plan using deterministic defaults.</summary>
    /// <param name="cascade">The target element's completed cascade.</param>
    /// <param name="styleSheets">The ordered stylesheets containing keyframes rules.</param>
    /// <returns>The resolved animation list.</returns>
    public SvgCssAnimationPlan ResolveWithCascadeAndStyleSheets(SvgCascadeResult cascade, System.Collections.Generic.List<SvgStyleSheet> styleSheets)
    {
        return new SvgCssAnimation().ResolveWithCascadeAndStyleSheetsAndOptions(cascade, styleSheets, new SvgCssAnimationOptions());
    }

    /// <summary>Builds a bounded CSS animation plan.</summary>
    /// <param name="cascade">The target element's completed cascade.</param>
    /// <param name="styleSheets">The ordered stylesheets containing keyframes rules.</param>
    /// <param name="options">The property, parsing, and interpolation options.</param>
    /// <returns>The resolved animation list.</returns>
    public SvgCssAnimationPlan ResolveWithCascadeAndStyleSheetsAndOptions(SvgCascadeResult cascade, System.Collections.Generic.List<SvgStyleSheet> styleSheets, SvgCssAnimationOptions options)
    {
        SvgCssAnimationPlan plan = new SvgCssAnimationPlan(cascade);
        if (cascade == null || !cascade.Success || styleSheets == null || !new SvgCssAnimation().ValidOptions(options))
        {
            new SvgCssAnimation().Fail(plan, SvgCssAnimationError.InvalidArgument, SvgDiagnosticCode.InvalidArgument, "CSS animation arguments are invalid.", null);
            return plan;
        }
        System.Collections.Generic.List<string>? names = new SvgCssAnimation().ListValue(cascade, SvgCssPropertyNames.AnimationName, SvgInitialValues.AnimationName, options.MaxAnimations);
        if (names == null)
        {
            new SvgCssAnimation().Fail(plan, SvgCssAnimationError.LimitExceeded, SvgDiagnosticCode.InputLimitExceeded, "The animation-name list is invalid or exceeds its bound.", new SvgCssAnimation().Source(cascade, SvgCssPropertyNames.AnimationName));
            return plan;
        }
        int index = 0;
        while (index < names.Count)
        {
            string? name = new SvgCssAnimation().DecodeName(names[index], options);
            if (name == null)
            {
                new SvgCssAnimation().Fail(plan, SvgCssAnimationError.InvalidAnimation, SvgDiagnosticCode.InvalidCss, "An animation name is invalid.", new SvgCssAnimation().Source(cascade, SvgCssPropertyNames.AnimationName));
                return plan;
            }
            if (name != "none")
            {
                SvgCssAnimationItem item = new SvgCssAnimationItem(index);
                item.Name = name;
                if (!new SvgCssAnimation().ReadSettings(item, cascade, index, options, plan)) return plan;
                item.Keyframes = new SvgCssAnimation().FindKeyframes(styleSheets, name, options, plan);
                if (item.Keyframes == null)
                {
                    new SvgCssAnimation().Fail(plan, SvgCssAnimationError.MissingKeyframes, SvgDiagnosticCode.InvalidCss, "A named CSS keyframes rule was not found.", new SvgCssAnimation().Source(cascade, SvgCssPropertyNames.AnimationName));
                    return plan;
                }
                new SvgCssAnimation().BuildTracks(item, options, plan);
                if (plan.Error != SvgCssAnimationError.None) return plan;
                plan.Animations.Add(item);
            }
            index = index + 1;
        }
        plan.Success = true;
        return plan;
    }

    /// <summary>Samples and composes a CSS animation plan at caller-provided seconds.</summary>
    /// <param name="plan">The resolved animation plan.</param>
    /// <param name="timeSeconds">The finite caller time in seconds.</param>
    /// <param name="underlying">The typed underlying cascade values.</param>
    /// <param name="options">The interpolation options.</param>
    /// <returns>The timing phases and final affected property values.</returns>
    public SvgCssAnimationSampleResult Sample(SvgCssAnimationPlan? plan, double timeSeconds, System.Collections.Generic.List<SvgCssAnimationUnderlyingValue>? underlying, SvgCssAnimationOptions options)
    {
        System.Collections.Generic.List<SvgCssAnimationItemSample> timing = new System.Collections.Generic.List<SvgCssAnimationItemSample>();
        System.Collections.Generic.List<SvgCssAnimationSampleValue> values = new System.Collections.Generic.List<SvgCssAnimationSampleValue>();
        if (plan == null || !plan.Success || !SvgFiniteMath.IsFinite(timeSeconds) || underlying == null || !new SvgCssAnimation().ValidOptions(options)) return new SvgCssAnimation().SampleFailure(SvgCssAnimationError.InvalidArgument, timing, values);
        int index = 0;
        while (index < plan.Animations.Count)
        {
            SvgCssAnimationItem item = plan.Animations[index];
            SvgCssAnimationItemSample itemSample = new SvgCssAnimation().Timing(item, timeSeconds);
            timing.Add(itemSample);
            if (itemSample.Phase != SvgCssAnimationPhase.Idle)
            {
                int trackIndex = 0;
                while (trackIndex < item.Tracks.Count)
                {
                    SvgCssAnimationTrack track = item.Tracks[trackIndex];
                    SvgAnimationValue? baseValue = new SvgCssAnimation().CurrentValue(track.PropertyName, track.ValueKind, underlying, values);
                    SvgAnimationValue? sampled = new SvgCssAnimation().SampleTrack(track, itemSample.Progress, baseValue, options);
                    if (sampled == null) return new SvgCssAnimation().SampleFailure(baseValue == null ? SvgCssAnimationError.MissingUnderlyingValue : SvgCssAnimationError.IncompatibleValues, timing, values);
                    new SvgCssAnimation().SetValue(values, track.PropertyName, sampled, item.Index);
                    trackIndex = trackIndex + 1;
                }
            }
            index = index + 1;
        }
        return new SvgCssAnimationSampleResult(true, SvgCssAnimationError.None, timing, values);
    }
bool ReadSettings(SvgCssAnimationItem item, SvgCascadeResult cascade, int index, SvgCssAnimationOptions options, SvgCssAnimationPlan plan)
    {
        string duration = new SvgCssAnimation().IndexedValue(cascade, SvgCssPropertyNames.AnimationDuration, SvgInitialValues.AnimationDuration, index, options.MaxAnimations);
        string delay = new SvgCssAnimation().IndexedValue(cascade, SvgCssPropertyNames.AnimationDelay, SvgInitialValues.AnimationDelay, index, options.MaxAnimations);
        string iterations = new SvgCssAnimation().IndexedValue(cascade, SvgCssPropertyNames.AnimationIterationCount, SvgInitialValues.AnimationIterationCount, index, options.MaxAnimations);
        string direction = new SvgCssAnimation().IndexedValue(cascade, SvgCssPropertyNames.AnimationDirection, SvgInitialValues.AnimationDirection, index, options.MaxAnimations);
        string fill = new SvgCssAnimation().IndexedValue(cascade, SvgCssPropertyNames.AnimationFillMode, SvgInitialValues.AnimationFillMode, index, options.MaxAnimations);
        string state = new SvgCssAnimation().IndexedValue(cascade, SvgCssPropertyNames.AnimationPlayState, SvgInitialValues.AnimationPlayState, index, options.MaxAnimations);
        string easing = new SvgCssAnimation().IndexedValue(cascade, SvgCssPropertyNames.AnimationTimingFunction, SvgInitialValues.AnimationTimingFunction, index, options.MaxAnimations);
        SvgFiniteResult parsedDuration = new SvgCssAnimation().Time(duration, false, options.MaxTokenCharacters);
        SvgFiniteResult parsedDelay = new SvgCssAnimation().Time(delay, true, options.MaxTokenCharacters);
        SvgFiniteResult parsedIterations = new SvgCssAnimation().Iterations(iterations, options.MaxTokenCharacters);
        SvgCssEasing? parsedEasing = new SvgCssAnimation().ParseEasing(easing, options.MaxTokenCharacters);
        if (!parsedDuration.Success || !parsedDelay.Success || !parsedIterations.Success || parsedEasing == null || !new SvgCssAnimation().ParseDirection(direction, item) || !new SvgCssAnimation().ParseFill(fill, item) || !new SvgCssAnimation().ParseState(state, item))
        {
            new SvgCssAnimation().Fail(plan, parsedEasing == null ? SvgCssAnimationError.InvalidEasing : SvgCssAnimationError.InvalidAnimation, SvgDiagnosticCode.InvalidCss, "A CSS animation longhand value is invalid.", new SvgCssAnimation().Source(cascade, SvgCssPropertyNames.AnimationName));
            return false;
        }
        item.Easing = parsedEasing;
        item.DurationSeconds = parsedDuration.Value;
        item.DelaySeconds = parsedDelay.Value;
        item.IterationCount = parsedIterations.Value;
        return true;
    }
SvgCssKeyframes? FindKeyframes(System.Collections.Generic.List<SvgStyleSheet> sheets, string name, SvgCssAnimationOptions options, SvgCssAnimationPlan plan)
    {
        SvgCssKeyframes? found = null;
        int[] inspected = new int[] { 0 };
        int sheetIndex = 0;
        while (sheetIndex < sheets.Count)
        {
            SvgStyleSheet sheet = sheets[sheetIndex];
            if (sheet != null) found = new SvgCssAnimation().FindKeyframesRules(sheet.Rules, name, found, options, plan, inspected);
            if (plan.Error != SvgCssAnimationError.None) return null;
            sheetIndex = sheetIndex + 1;
        }
        return found;
    }
SvgCssKeyframes? FindKeyframesRules(System.Collections.Generic.List<SvgCssRule> rules, string name, SvgCssKeyframes? found, SvgCssAnimationOptions options, SvgCssAnimationPlan plan, int[] inspected)
    {
        int index = 0;
        while (index < rules.Count)
        {
            SvgCssRule rule = rules[index];
            inspected[0] = inspected[0] + 1;
            if (inspected[0] > options.MaxKeyframes)
            {
                new SvgCssAnimation().Fail(plan, SvgCssAnimationError.LimitExceeded, SvgDiagnosticCode.InputLimitExceeded, "The keyframes inspection bound was exceeded.", rule == null ? null : rule.Source);
                return found;
            }
            if (rule != null && rule.IsValid && rule.AtRule != null && rule.AtRule.Keyframes != null && rule.AtRule.Keyframes.IsValid && string.Equals(rule.AtRule.Keyframes.Name, name, System.StringComparison.Ordinal)) found = rule.AtRule.Keyframes;
            if (rule != null && rule.ChildRules != null && rule.ChildRules.Count != 0) found = new SvgCssAnimation().FindKeyframesRules(rule.ChildRules, name, found, options, plan, inspected);
            if (plan.Error != SvgCssAnimationError.None) return found;
            index = index + 1;
        }
        return found;
    }
void BuildTracks(SvgCssAnimationItem item, SvgCssAnimationOptions options, SvgCssAnimationPlan plan)
    {
        int blockIndex = 0;
        while (blockIndex < (item.Keyframes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Keyframes.Count)
        {
            SvgCssKeyframe block = item.Keyframes.Keyframes[blockIndex];
            int offsetIndex = 0;
            while (offsetIndex < block.Offsets.Count)
            {
                double offset = block.Offsets[offsetIndex];
                SvgCssEasing outgoing = item.Easing;
                SvgCssDeclaration? easingDeclaration = new SvgCssAnimation().LastDeclaration(block.Declarations, SvgCssPropertyNames.AnimationTimingFunction);
                if (easingDeclaration != null)
                {
                    outgoing = (new SvgCssAnimation().ParseEasing(easingDeclaration.ValueText, options.MaxTokenCharacters) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                    if (outgoing == null)
                    {
                        new SvgCssAnimation().Fail(plan, SvgCssAnimationError.InvalidEasing, SvgDiagnosticCode.InvalidCss, "A keyframe timing function is invalid.", easingDeclaration.Source);
                        return;
                    }
                }
                int declarationIndex = 0;
                while (declarationIndex < block.Declarations.Count)
                {
                    SvgCssDeclaration declaration = block.Declarations[declarationIndex];
                    if (declaration != null && declaration.IsValid && !declaration.Important && !string.Equals(declaration.Name, SvgCssPropertyNames.AnimationTimingFunction, System.StringComparison.OrdinalIgnoreCase)) new SvgCssAnimation().AddTrackKey(item, declaration, offset, outgoing, options, plan);
                    if (plan.Error != SvgCssAnimationError.None) return;
                    declarationIndex = declarationIndex + 1;
                }
                offsetIndex = offsetIndex + 1;
            }
            blockIndex = blockIndex + 1;
        }
        int trackIndex = 0;
        while (trackIndex < item.Tracks.Count)
        {
            SvgCssAnimationTrack track = item.Tracks[trackIndex];
            track.Keys.Sort((left, right) => left.Offset.CompareTo(right.Offset));
            if (track.Keys.Count == 0 || track.Keys[0].Offset != 0) track.Keys.Insert(0, new SvgCssAnimationKey(0, null, item.Easing));
            if (track.Keys[track.Keys.Count - 1].Offset != 1) track.Keys.Add(new SvgCssAnimationKey(1, null, item.Easing));
            trackIndex = trackIndex + 1;
        }
    }
void AddTrackKey(SvgCssAnimationItem item, SvgCssDeclaration declaration, double offset, SvgCssEasing easing, SvgCssAnimationOptions options, SvgCssAnimationPlan plan)
    {
        SvgPropertySupportEntry? property = options.Properties.Find(declaration.Name);
        if (property == null || property.IsShorthand || property.AnimationType == SvgPropertyAnimationType.NotAnimatable) return;
        SvgAnimationValueKind kind = new SvgCssAnimation().ValueKind(property);
        SvgAnimationValueParseResult parsed = new SvgAnimationInterpolator().ParseWithKindAndTextAndOptions(kind, declaration.ValueText.Trim(), options.Interpolation);
        if (!parsed.Success)
        {
            new SvgCssAnimation().Fail(plan, parsed.Error == SvgAnimationInterpolationError.LimitExceeded ? SvgCssAnimationError.LimitExceeded : SvgCssAnimationError.InvalidKeyframeValue, parsed.Error == SvgAnimationInterpolationError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidCss, "A CSS keyframe property value is invalid.", declaration.Source);
            return;
        }
        SvgCssAnimationTrack? track = new SvgCssAnimation().FindTrack(item.Tracks, property.Name);
        if (track == null)
        {
            if (item.Tracks.Count >= options.MaxProperties)
            {
                new SvgCssAnimation().Fail(plan, SvgCssAnimationError.LimitExceeded, SvgDiagnosticCode.InputLimitExceeded, "The animated property track bound was exceeded.", declaration.Source);
                return;
            }
            track = new SvgCssAnimationTrack(property.Name, kind);
            item.Tracks.Add(track);
        }
        int index = 0;
        while (index < track.Keys.Count)
        {
            if (track.Keys[index].Offset == offset)
            {
                track.Keys[index] = new SvgCssAnimationKey(offset, parsed.Value, easing);
                return;
            }
            index = index + 1;
        }
        track.Keys.Add(new SvgCssAnimationKey(offset, parsed.Value, easing));
    }
SvgCssAnimationItemSample Timing(SvgCssAnimationItem item, double time)
    {
        double effective = item.PlayState == SvgCssAnimationPlayState.Paused ? 0 : time;
        double local = effective - item.DelaySeconds;
        if (local < 0)
        {
            if (item.FillMode != SvgCssAnimationFillMode.Backwards && item.FillMode != SvgCssAnimationFillMode.Both) return new SvgCssAnimationItemSample(item.Index, SvgCssAnimationPhase.Idle, 0, 0);
            return new SvgCssAnimationItemSample(item.Index, SvgCssAnimationPhase.Before, 0, new SvgCssAnimation().Directed(item, 0, 0));
        }
        double activeDuration = item.DurationSeconds * item.IterationCount;
        if (double.IsPositiveInfinity(item.IterationCount)) activeDuration = double.PositiveInfinity;
        if (item.DurationSeconds > 0 && local < activeDuration)
        {
            int iteration = (int)System.Math.Floor(local / item.DurationSeconds);
            double progress = local / item.DurationSeconds - iteration;
            return new SvgCssAnimationItemSample(item.Index, SvgCssAnimationPhase.Active, iteration, new SvgCssAnimation().Directed(item, iteration, progress));
        }
        if (item.FillMode != SvgCssAnimationFillMode.Forwards && item.FillMode != SvgCssAnimationFillMode.Both) return new SvgCssAnimationItemSample(item.Index, SvgCssAnimationPhase.Idle, 0, 0);
        int finalIteration = item.IterationCount <= 0 ? 0 : (int)System.Math.Ceiling(item.IterationCount) - 1;
        double finalProgress = item.IterationCount <= 0 ? 0 : item.IterationCount - System.Math.Floor(item.IterationCount);
        if (finalProgress == 0 && item.IterationCount > 0) finalProgress = 1;
        return new SvgCssAnimationItemSample(item.Index, SvgCssAnimationPhase.After, finalIteration, new SvgCssAnimation().Directed(item, finalIteration, finalProgress));
    }
double Directed(SvgCssAnimationItem item, int iteration, double progress)
    {
        bool reverse = item.Direction == SvgCssAnimationDirection.Reverse || item.Direction == SvgCssAnimationDirection.Alternate && iteration % 2 == 1 || item.Direction == SvgCssAnimationDirection.AlternateReverse && iteration % 2 == 0;
        return reverse ? 1 - progress : progress;
    }
SvgAnimationValue? SampleTrack(SvgCssAnimationTrack track, double progress, SvgAnimationValue? underlying, SvgCssAnimationOptions options)
    {
        int segment = 0;
        if (progress >= 1) segment = track.Keys.Count - 2;
        else
        {
            while (segment + 1 < track.Keys.Count && progress >= track.Keys[segment + 1].Offset) segment = segment + 1;
            if (segment >= track.Keys.Count - 1) segment = track.Keys.Count - 2;
        }
        SvgCssAnimationKey first = track.Keys[segment];
        SvgCssAnimationKey second = track.Keys[segment + 1];
        SvgAnimationValue? start = first.Value == null ? underlying : first.Value;
        SvgAnimationValue? end = second.Value == null ? underlying : second.Value;
        if (start == null || end == null || start.Kind != track.ValueKind || end.Kind != track.ValueKind) return null;
        double local = second.Offset == first.Offset ? 0 : (progress - first.Offset) / (second.Offset - first.Offset);
        if (local < 0) local = 0;
        if (local > 1) local = 1;
        double eased = first.Easing.Apply(local);
        if (!SvgFiniteMath.IsFinite(eased)) return null;
        SvgAnimationCalcMode mode = track.ValueKind == SvgAnimationValueKind.String ? SvgAnimationCalcMode.Discrete : SvgAnimationCalcMode.Linear;
        SvgAnimationInterpolationResult result = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndModeAndOptions(start, end, eased, mode, options.Interpolation);
        return result.Success ? result.Value : null;
    }
SvgAnimationValue? CurrentValue(string name, SvgAnimationValueKind kind, System.Collections.Generic.List<SvgCssAnimationUnderlyingValue> underlying, System.Collections.Generic.List<SvgCssAnimationSampleValue> current)
    {
        int index = current.Count - 1;
        while (index >= 0)
        {
            if (string.Equals(current[index].PropertyName, name, System.StringComparison.OrdinalIgnoreCase)) return current[index].Value.Kind == kind ? current[index].Value : null;
            index = index - 1;
        }
        index = 0;
        while (index < underlying.Count)
        {
            if (underlying[index] != null && string.Equals(underlying[index].PropertyName, name, System.StringComparison.OrdinalIgnoreCase)) return underlying[index].Value.Kind == kind ? underlying[index].Value : null;
            index = index + 1;
        }
        return null;
    }
void SetValue(System.Collections.Generic.List<SvgCssAnimationSampleValue> values, string name, SvgAnimationValue value, int animationIndex)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index].PropertyName, name, System.StringComparison.OrdinalIgnoreCase))
            {
                values[index] = new SvgCssAnimationSampleValue(name, value, animationIndex);
                return;
            }
            index = index + 1;
        }
        values.Add(new SvgCssAnimationSampleValue(name, value, animationIndex));
    }
SvgCssAnimationTrack? FindTrack(System.Collections.Generic.List<SvgCssAnimationTrack> tracks, string name)
    {
        int index = 0;
        while (index < tracks.Count)
        {
            if (string.Equals(tracks[index].PropertyName, name, System.StringComparison.OrdinalIgnoreCase)) return tracks[index];
            index = index + 1;
        }
        return null;
    }
SvgCssDeclaration? LastDeclaration(System.Collections.Generic.List<SvgCssDeclaration> declarations, string name)
    {
        int index = declarations.Count - 1;
        while (index >= 0)
        {
            if (declarations[index] != null && declarations[index].IsValid && string.Equals(declarations[index].Name, name, System.StringComparison.OrdinalIgnoreCase)) return declarations[index];
            index = index - 1;
        }
        return null;
    }
SvgAnimationValueKind ValueKind(SvgPropertySupportEntry property)
    {
        if (property.Name == SvgCssPropertyNames.D) return SvgAnimationValueKind.Path;
        if (property.AnimationType == SvgPropertyAnimationType.Number) return SvgAnimationValueKind.Number;
        if (property.AnimationType == SvgPropertyAnimationType.Length) return SvgAnimationValueKind.Length;
        if (property.AnimationType == SvgPropertyAnimationType.Color) return SvgAnimationValueKind.Color;
        if (property.AnimationType == SvgPropertyAnimationType.Paint) return SvgAnimationValueKind.Paint;
        if (property.AnimationType == SvgPropertyAnimationType.Transform) return SvgAnimationValueKind.Transform;
        return SvgAnimationValueKind.String;
    }
SvgFiniteResult Time(string? text, bool negative, int maximum)
    {
        string value = text == null ? "" : text.Trim();
        if (value == "0") return new SvgFiniteResult(true, 0, "");
        SvgTimeParseResult parsed = SvgScalarUnitParser.ParseTime(value, false, maximum);
        if (!parsed.Success) return new SvgFiniteResult(false, 0, "Invalid time.");
        SvgUnitConversionResult seconds = (parsed.Time ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ConvertTo(SvgTimeUnit.Seconds);
        if (!seconds.Success || !negative && seconds.Value < 0) return new SvgFiniteResult(false, 0, "Invalid time.");
        return new SvgFiniteResult(true, seconds.Value, "");
    }
SvgFiniteResult Iterations(string? text, int maximum)
    {
        string value = text == null ? "" : text.Trim();
        if (string.Equals(value, "infinite", System.StringComparison.OrdinalIgnoreCase)) return new SvgFiniteResult(true, double.PositiveInfinity, "");
        SvgNumberParseResult parsed = SvgNumberParser.ParseCssNumber(value, maximum);
        return parsed.Success && parsed.Value >= 0 ? new SvgFiniteResult(true, parsed.Value, "") : new SvgFiniteResult(false, 0, "Invalid iteration count.");
    }
SvgCssEasing? ParseEasing(string? text, int maximum)
    {
        string value = text == null ? "" : text.Trim().ToLowerInvariant();
        SvgCssEasing result = new SvgCssEasing();
        if (value == "linear") return result;
        if (value == "ease") return new SvgCssAnimation().Bezier(0.25, 0.1, 0.25, 1);
        if (value == "ease-in") return new SvgCssAnimation().Bezier(0.42, 0, 1, 1);
        if (value == "ease-out") return new SvgCssAnimation().Bezier(0, 0, 0.58, 1);
        if (value == "ease-in-out") return new SvgCssAnimation().Bezier(0.42, 0, 0.58, 1);
        if (value == "step-start") return new SvgCssAnimation().Step(1, true);
        if (value == "step-end") return new SvgCssAnimation().Step(1, false);
        if (value.StartsWith("cubic-bezier(", System.StringComparison.Ordinal) && value.EndsWith(")", System.StringComparison.Ordinal))
        {
            string inner = value.Substring(13, value.Length - 14);
            SvgNumberListResult numbers = SvgListParser.ParseNumberList(inner, SvgListSeparatorMode.Comma, 5, maximum, false);
            if (!numbers.Success || numbers.Values.Length != 4 || numbers.Values[0] < 0 || numbers.Values[0] > 1 || numbers.Values[2] < 0 || numbers.Values[2] > 1) return null;
            return new SvgCssAnimation().Bezier(numbers.Values[0], numbers.Values[1], numbers.Values[2], numbers.Values[3]);
        }
        if (value.StartsWith("steps(", System.StringComparison.Ordinal) && value.EndsWith(")", System.StringComparison.Ordinal))
        {
            string inner = value.Substring(6, value.Length - 7);
            int comma = inner.IndexOf(',');
            string countText = comma < 0 ? inner.Trim() : inner.Substring(0, comma).Trim();
            SvgNumberParseResult count = SvgNumberParser.ParseCssNumber(countText, maximum);
            if (!count.Success || count.Value < 1 || count.Value > System.Int32.MaxValue || count.Value != System.Math.Floor(count.Value)) return null;
            bool start = false;
            if (comma >= 0)
            {
                string position = inner.Substring(comma + 1).Trim();
                if (position == "start" || position == "jump-start") start = true;
                else if (position != "end" && position != "jump-end") return null;
            }
            return new SvgCssAnimation().Step((int)count.Value, start);
        }
        return null;
    }
SvgCssEasing Bezier(double x1, double y1, double x2, double y2)
    {
        SvgCssEasing result = new SvgCssEasing(); result.Kind = SvgCssEasingKind.CubicBezier; result.X1 = x1; result.Y1 = y1; result.X2 = x2; result.Y2 = y2; return result;
    }
SvgCssEasing Step(int count, bool start)
    {
        SvgCssEasing result = new SvgCssEasing(); result.Kind = SvgCssEasingKind.Steps; result.Steps = count; result.StepStart = start; return result;
    }
bool ParseDirection(string text, SvgCssAnimationItem item)
    {
        string value = text.Trim().ToLowerInvariant();
        if (value == "normal") item.Direction = SvgCssAnimationDirection.Normal;
        else if (value == "reverse") item.Direction = SvgCssAnimationDirection.Reverse;
        else if (value == "alternate") item.Direction = SvgCssAnimationDirection.Alternate;
        else if (value == "alternate-reverse") item.Direction = SvgCssAnimationDirection.AlternateReverse;
        else return false;
        return true;
    }
bool ParseFill(string text, SvgCssAnimationItem item)
    {
        string value = text.Trim().ToLowerInvariant();
        if (value == "none") item.FillMode = SvgCssAnimationFillMode.None;
        else if (value == "forwards") item.FillMode = SvgCssAnimationFillMode.Forwards;
        else if (value == "backwards") item.FillMode = SvgCssAnimationFillMode.Backwards;
        else if (value == "both") item.FillMode = SvgCssAnimationFillMode.Both;
        else return false;
        return true;
    }
bool ParseState(string text, SvgCssAnimationItem item)
    {
        string value = text.Trim().ToLowerInvariant();
        if (value == "running") item.PlayState = SvgCssAnimationPlayState.Running;
        else if (value == "paused") item.PlayState = SvgCssAnimationPlayState.Paused;
        else return false;
        return true;
    }
System.Collections.Generic.List<string>? ListValue(SvgCascadeResult cascade, string property, string initial, int maximum)
    {
        SvgCascadedValue? value = cascade.Find(property);
        return new SvgCssAnimation().SplitTopLevel(value == null ? initial : value.ValueText, maximum);
    }
string IndexedValue(SvgCascadeResult cascade, string property, string initial, int index, int maximum)
    {
        System.Collections.Generic.List<string>? values = new SvgCssAnimation().ListValue(cascade, property, initial, maximum);
        return values == null || values.Count == 0 ? "" : values[index % values.Count];
    }
System.Collections.Generic.List<string>? SplitTopLevel(string text, int maximum)
    {
        if (text == null || maximum < 1) return null;
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        int start = 0; int depth = 0; char quote = '\0'; int index = 0;
        while (index <= text.Length)
        {
            char current = index == text.Length ? ',' : text[index];
            if (quote != '\0')
            {
                if (current == '\\') index = index + 1;
                else if (current == quote) quote = '\0';
            }
            else if (current == '\'' || current == '"') quote = current;
            else if (current == '(' || current == '[') depth = depth + 1;
            else if (current == ')' || current == ']') { if (depth == 0) return null; depth = depth - 1; }
            else if (current == ',' && depth == 0)
            {
                if (result.Count >= maximum) return null;
                string item = text.Substring(start, index - start).Trim();
                if (item.Length == 0) return null;
                result.Add(item); start = index + 1;
            }
            index = index + 1;
        }
        return depth == 0 && quote == '\0' ? result : null;
    }
string? DecodeName(string text, SvgCssAnimationOptions options)
    {
        string value = text.Trim();
        if (string.Equals(value, "none", System.StringComparison.OrdinalIgnoreCase)) return "none";
        SvgCssTokenizeResult tokens = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(value, 4, options.MaxTokenCharacters);
        if (!tokens.Success) return null;
        int index = 0; SvgCssToken? selected = null;
        while (index < tokens.Tokens.Count)
        {
            SvgCssToken token = tokens.Tokens[index];
            if (token.Kind != SvgCssTokenKind.Whitespace && token.Kind != SvgCssTokenKind.Comment)
            {
                if (selected != null || token.Kind != SvgCssTokenKind.Ident && token.Kind != SvgCssTokenKind.String) return null;
                selected = token;
            }
            index = index + 1;
        }
        return selected == null ? null : selected.Value;
    }
SvgSourceSpan? Source(SvgCascadeResult cascade, string property)
    {
        SvgCascadedValue? value = cascade.Find(property);
        return value == null || value.Winner == null ? null : value.Winner.Source;
    }
bool ValidOptions(SvgCssAnimationOptions options)
    {
        return options != null && options.Properties != null && options.Interpolation != null && options.MaxAnimations > 0 && options.MaxKeyframes > 0 && options.MaxProperties > 0 && options.MaxTokenCharacters > 0;
    }
void Fail(SvgCssAnimationPlan plan, SvgCssAnimationError error, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        plan.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source)); plan.Error = error; plan.Success = false;
    }
SvgCssAnimationSampleResult SampleFailure(SvgCssAnimationError error, System.Collections.Generic.List<SvgCssAnimationItemSample> animations, System.Collections.Generic.List<SvgCssAnimationSampleValue> values)
    {
        return new SvgCssAnimationSampleResult(false, error, animations, values);
    }
}
