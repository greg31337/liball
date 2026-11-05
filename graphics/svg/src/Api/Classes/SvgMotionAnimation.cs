namespace svg;

/// <summary>Resolves and samples animateMotion paths, key points, rotation, and origin composition.</summary>
public class SvgMotionAnimation : ISvgMotionAnimation
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgMotionAnimation()
    {
    }

    /// <summary>Resolves one target-resolved animateMotion element with secure defaults.</summary>
    /// <param name="animation">The target-resolved motion animation.</param>
    /// <returns>The resolved bounded motion plan.</returns>
    public SvgMotionAnimationPlan ResolveWithAnimation(SvgAnimation animation)
    {
        return new SvgMotionAnimation().ResolveWithAnimationAndOptions(animation, new SvgMotionAnimationOptions());
    }

    /// <summary>Resolves one target-resolved animateMotion element.</summary>
    /// <param name="animation">The target-resolved motion animation.</param>
    /// <param name="options">The reference, geometry, interpolation, and security options.</param>
    /// <returns>The resolved bounded motion plan.</returns>
    public SvgMotionAnimationPlan ResolveWithAnimationAndOptions(SvgAnimation animation, SvgMotionAnimationOptions options)
    {
        SvgMotionAnimationPlan plan = new SvgMotionAnimationPlan(animation);
        if (!new SvgMotionAnimation().ValidAnimation(animation) || !new SvgMotionAnimation().ValidOptions(options))
        {
            new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidArgument, SvgDiagnosticCode.InvalidArgument, "Motion animation arguments are invalid.", animation == null || animation.Source == null ? null : animation.Source.SourceSpan);
            return plan;
        }
        SvgNode? mpath = new SvgMotionAnimation().FindMPath((animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), plan);
        if (plan.Error != SvgMotionAnimationError.None) return plan;
        if (mpath != null) new SvgMotionAnimation().ResolveMPath(plan, mpath, options);
        else
        {
            SvgAttribute? path = new SvgMotionAnimation().FindAttribute(animation.Source, SvgAttributeNames.Path);
            if (path != null) new SvgMotionAnimation().ResolveInlinePath(plan, path, options);
            else new SvgMotionAnimation().ResolveValues(plan, options);
        }
        if (plan.Error != SvgMotionAnimationError.None) return plan;
        new SvgMotionAnimation().ParseRotate(plan, new SvgMotionAnimation().FindAttribute(animation.Source, SvgAttributeNames.Rotate), options);
        if (plan.Error != SvgMotionAnimationError.None) return plan;
        if (plan.SourceKind != SvgMotionPathSourceKind.Values) new SvgMotionAnimation().ParsePathSettings(plan, options);
        if (plan.Error != SvgMotionAnimationError.None) return plan;
        new SvgMotionAnimation().CopyPlanToAnimation(plan);
        plan.Success = true;
        return plan;
    }

    /// <summary>Samples a resolved motion plan around the coordinate origin.</summary>
    /// <param name="plan">The resolved motion plan.</param>
    /// <param name="progress">The finite simple progress in the unit interval.</param>
    /// <returns>The sampled supplemental motion transform.</returns>
    public SvgMotionAnimationResult SampleWithPlanAndProgress(SvgMotionAnimationPlan plan, double progress)
    {
        return new SvgMotionAnimation().SampleWithPlanAndProgressAndRepeatIterationAndOriginAndBaseTransformAndOptions(plan, progress, 0, new SvgPoint(0, 0), null, new SvgMotionAnimationOptions());
    }

    /// <summary>Samples a resolved motion plan and composes it after a base transform.</summary>
    /// <param name="plan">The resolved motion plan.</param>
    /// <param name="progress">The finite simple progress in the unit interval.</param>
    /// <param name="repeatIteration">The nonnegative zero-based repeat iteration.</param>
    /// <param name="origin">The finite target point mapped onto the motion path.</param>
    /// <param name="baseTransform">The base target transform, or <c>null</c>.</param>
    /// <param name="options">The sampling and repeat bounds.</param>
    /// <returns>The sampled and composed motion transform.</returns>
    public SvgMotionAnimationResult SampleWithPlanAndProgressAndRepeatIterationAndOriginAndBaseTransformAndOptions(SvgMotionAnimationPlan? plan, double progress, int repeatIteration, SvgPoint? origin, SvgTransformList? baseTransform, SvgMotionAnimationOptions options)
    {
        if (plan == null || !plan.Success || !SvgFiniteMath.IsFinite(progress) || progress < 0 || progress > 1 || repeatIteration < 0 || origin == null || !new SvgMotionAnimation().ValidPoint(origin) || !new SvgMotionAnimation().ValidOptions(options)) return new SvgMotionAnimation().SampleFailure(SvgMotionAnimationError.InvalidArgument);
        if (repeatIteration > options.CompositionOptions.MaxRepeatAccumulations) return new SvgMotionAnimation().SampleFailure(SvgMotionAnimationError.LimitExceeded);
        if (plan.SourceKind == SvgMotionPathSourceKind.Values) return new SvgMotionAnimation().SampleValues(plan, progress, repeatIteration, origin, baseTransform, options);
        double pathProgress = new SvgMotionAnimation().MapProgress(plan, progress, options);
        if (!SvgFiniteMath.IsFinite(pathProgress)) return new SvgMotionAnimation().SampleFailure(SvgMotionAnimationError.NonFiniteResult);
        double calibratedDistance = (plan.Metrics ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).CalibratedLength * pathProgress;
        if (!SvgFiniteMath.IsFinite(calibratedDistance)) return new SvgMotionAnimation().SampleFailure(SvgMotionAnimationError.NonFiniteResult);
        SvgPathLocationResult location = new SvgPathMetrics().GetLocationWithPathAndDistanceAndOptions((plan.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), calibratedDistance, (plan.Measurement ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!location.Success) return new SvgMotionAnimation().SampleFailure(new SvgMotionAnimation().MapMetricError(location.Error));
        SvgPoint? point = location.Point;
        if (plan.Accumulate == SvgAnimationAccumulate.Sum && repeatIteration > 0)
        {
            SvgPathLocationResult endpoint = new SvgPathMetrics().GetLocationWithPathAndDistanceAndOptions(plan.PathData, plan.Metrics.CalibratedLength, plan.Measurement);
            if (!endpoint.Success) return new SvgMotionAnimation().SampleFailure(new SvgMotionAnimation().MapMetricError(endpoint.Error));
            double x = (point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X + (endpoint.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X * repeatIteration;
            double y = point.Y + endpoint.Point.Y * repeatIteration;
            if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return new SvgMotionAnimation().SampleFailure(SvgMotionAnimationError.NonFiniteResult);
            point = new SvgPoint(x, y);
        }
        return new SvgMotionAnimation().ComposeSample(plan, point, location.Tangent, pathProgress, calibratedDistance, location.ActualDistance, repeatIteration, origin, baseTransform);
    }

    /// <summary>Samples using a timeline entry's simple progress and repeat iteration.</summary>
    /// <param name="plan">The resolved motion plan.</param>
    /// <param name="timeline">The active or frozen timeline entry.</param>
    /// <param name="origin">The finite target point mapped onto the motion path.</param>
    /// <param name="baseTransform">The base target transform, or <c>null</c>.</param>
    /// <param name="options">The sampling and repeat bounds.</param>
    /// <returns>The sampled and composed motion transform.</returns>
    public SvgMotionAnimationResult SampleWithPlanAndTimelineAndOriginAndBaseTransformAndOptions(SvgMotionAnimationPlan plan, SvgAnimationTimelineEntry? timeline, SvgPoint origin, SvgTransformList? baseTransform, SvgMotionAnimationOptions options)
    {
        if (timeline == null || !timeline.Active && !timeline.Frozen) return new SvgMotionAnimation().SampleFailure(SvgMotionAnimationError.InvalidArgument);
        return new SvgMotionAnimation().SampleWithPlanAndProgressAndRepeatIterationAndOriginAndBaseTransformAndOptions(plan, timeline.SimpleProgress, timeline.RepeatIteration, origin, baseTransform, options);
    }
void ResolveMPath(SvgMotionAnimationPlan plan, SvgNode mpath, SvgMotionAnimationOptions options)
    {
        plan.SourceKind = SvgMotionPathSourceKind.MPath;
        plan.MPath = mpath;
        SvgReferenceResolveOptions referenceOptions = new SvgMotionAnimation().CopyReferenceOptions(options.ReferenceOptions);
        referenceOptions.RequireReference = true;
        plan.Reference = new SvgReferenceResolver().ResolveWithNodeAndOptions(mpath, referenceOptions);
        new SvgMotionAnimation().AppendDiagnostics(plan.Diagnostics, plan.Reference.Diagnostics);
        if (!plan.Reference.Success || plan.Reference.Target == null || plan.Reference.Target.ElementKind != SvgElementKind.Path)
        {
            new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidMPath, SvgDiagnosticCode.InvalidReference, "The mpath reference must resolve to an SVG path element.", plan.Reference.ReferenceSource == null ? mpath.SourceSpan : plan.Reference.ReferenceSource);
            return;
        }
        plan.PathNode = plan.Reference.Target;
        SvgShapeResolveOptions shapeOptions = new SvgMotionAnimation().CopyShapeOptions(options.ShapeOptions);
        if (shapeOptions.ComputedStyle != null && !object.ReferenceEquals(shapeOptions.ComputedStyle.Node, plan.PathNode)) shapeOptions.ComputedStyle = null;
        SvgShapeResult shape = new SvgShapeResolver().ResolveWithNodeAndOptions(plan.PathNode, shapeOptions);
        new SvgMotionAnimation().AppendDiagnostics(plan.Diagnostics, shape.Diagnostics);
        if (!shape.Success || shape.PathData == null)
        {
            new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidReferencedPath, SvgDiagnosticCode.InvalidValue, "The referenced path geometry is invalid.", plan.PathNode.SourceSpan);
            return;
        }
        plan.PathData = shape.PathData;
        plan.PathLength = shape.PathLength;
        new SvgMotionAnimation().Measure(plan, options, plan.PathNode.SourceSpan);
    }
void ResolveInlinePath(SvgMotionAnimationPlan plan, SvgAttribute attribute, SvgMotionAnimationOptions options)
    {
        plan.SourceKind = SvgMotionPathSourceKind.Path;
        SvgPathParseResult parsed = new SvgPathParser().Parse(attribute.Value, options.MaxPathSegments, options.MaxTokenCharacters);
        if (!parsed.Success || parsed.PathData == null)
        {
            SvgMotionAnimationError error = parsed.Error == SvgPathParseError.SegmentLimitExceeded || parsed.Error == SvgPathParseError.TokenTooLong ? SvgMotionAnimationError.LimitExceeded : SvgMotionAnimationError.InvalidPath;
            SvgDiagnosticCode code = error == SvgMotionAnimationError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgMotionAnimation().Fail(plan, error, code, "The animateMotion path attribute is invalid or exceeds its configured bound.", attribute.SourceSpan);
            return;
        }
        plan.PathData = parsed.PathData;
        new SvgMotionAnimation().Measure(plan, options, attribute.SourceSpan);
    }
void ResolveValues(SvgMotionAnimationPlan plan, SvgMotionAnimationOptions options)
    {
        plan.SourceKind = SvgMotionPathSourceKind.Values;
        plan.ValuePlan = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(plan.Animation, SvgAnimationValueKind.Points, options.CompositionOptions);
        new SvgMotionAnimation().AppendDiagnostics(plan.Diagnostics, plan.ValuePlan.Diagnostics);
        if (!plan.ValuePlan.Success || !new SvgMotionAnimation().SinglePointValues(plan.ValuePlan))
        {
            SvgMotionAnimationError error = plan.ValuePlan.Error == SvgAnimationCompositionError.LimitExceeded ? SvgMotionAnimationError.LimitExceeded : plan.ValuePlan.Error == SvgAnimationCompositionError.MissingValues ? SvgMotionAnimationError.MissingPath : SvgMotionAnimationError.InvalidValues;
            new SvgMotionAnimation().Fail(plan, error, error == SvgMotionAnimationError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue, "Point-value motion requires one coordinate pair in every selected value.", (plan.Animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return;
        }
        plan.CalcMode = plan.ValuePlan.CalcMode;
        plan.KeyTimes = (double[])plan.ValuePlan.KeyTimes.Clone();
        plan.KeyPoints = (double[])plan.ValuePlan.KeyPoints.Clone();
        new SvgMotionAnimation().CopySplines(plan.ValuePlan.KeySplines, plan.KeySplines);
        plan.Additive = plan.ValuePlan.Additive;
        plan.Accumulate = plan.ValuePlan.Accumulate;
    }
void Measure(SvgMotionAnimationPlan plan, SvgMotionAnimationOptions options, SvgSourceSpan? source)
    {
        plan.Measurement = new SvgMotionAnimation().MeasureOptions(options.MeasureOptions, plan.PathLength);
        plan.Metrics = new SvgPathMetrics().MeasureWithPathAndOptions((plan.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), plan.Measurement);
        if (!plan.Metrics.Success)
        {
            new SvgMotionAnimation().Fail(plan, new SvgMotionAnimation().MapMetricError(plan.Metrics.Error), SvgDiagnosticCode.InvalidValue, "The motion path or pathLength calibration could not be measured.", source);
        }
    }
void ParseRotate(SvgMotionAnimationPlan plan, SvgAttribute? attribute, SvgMotionAnimationOptions options)
    {
        if (attribute == null) return;
        string text = attribute.Value.Trim();
        if (text == "auto")
        {
            plan.RotateKind = SvgMotionRotateKind.Auto;
            return;
        }
        if (text == "auto-reverse")
        {
            plan.RotateKind = SvgMotionRotateKind.AutoReverse;
            return;
        }
        SvgAngleParseResult parsed = SvgScalarUnitParser.ParseAngle(text, true, options.MaxTokenCharacters);
        if (!parsed.Success)
        {
            new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidRotate, SvgDiagnosticCode.InvalidValue, "The animateMotion rotate value must be auto, auto-reverse, or an angle.", attribute.SourceSpan);
            return;
        }
        SvgUnitConversionResult degrees = (parsed.Angle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ConvertTo(SvgAngleUnit.Deg);
        if (!degrees.Success)
        {
            new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidRotate, SvgDiagnosticCode.InvalidValue, "The animateMotion rotate angle is not finite.", attribute.SourceSpan);
            return;
        }
        plan.RotateAngle = degrees.Value;
    }
void ParsePathSettings(SvgMotionAnimationPlan plan, SvgMotionAnimationOptions options)
    {
        SvgNode source = (plan.Animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgAttribute? calcMode = new SvgMotionAnimation().FindAttribute((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.CalcMode);
        if (calcMode != null)
        {
            SvgAnimationKeywordParseResult parsed = new SvgAnimationKeywordParser().ParseCalcMode(calcMode.Value);
            if (!parsed.Success)
            {
                new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidKeys, SvgDiagnosticCode.InvalidValue, "The motion calcMode is invalid.", calcMode.SourceSpan);
                return;
            }
            plan.CalcMode = (SvgAnimationCalcMode)parsed.Value;
        }
        SvgAttribute? additive = new SvgMotionAnimation().FindAttribute(source, SvgAttributeNames.Additive);
        if (additive != null)
        {
            SvgAnimationKeywordParseResult parsed = new SvgAnimationKeywordParser().ParseAdditive(additive.Value);
            if (!parsed.Success)
            {
                new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidValues, SvgDiagnosticCode.InvalidValue, "The motion additive value is invalid.", additive.SourceSpan);
                return;
            }
            plan.Additive = (SvgAnimationAdditive)parsed.Value;
        }
        SvgAttribute? accumulate = new SvgMotionAnimation().FindAttribute(source, SvgAttributeNames.Accumulate);
        if (accumulate != null)
        {
            SvgAnimationKeywordParseResult parsed = new SvgAnimationKeywordParser().ParseAccumulate(accumulate.Value);
            if (!parsed.Success)
            {
                new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidValues, SvgDiagnosticCode.InvalidValue, "The motion accumulate value is invalid.", accumulate.SourceSpan);
                return;
            }
            plan.Accumulate = (SvgAnimationAccumulate)parsed.Value;
        }
        SvgAttribute? keyTimes = new SvgMotionAnimation().FindAttribute(source, SvgAttributeNames.KeyTimes);
        SvgAttribute? keyPoints = new SvgMotionAnimation().FindAttribute(source, SvgAttributeNames.KeyPoints);
        if (keyTimes == null && keyPoints != null)
        {
            new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidKeys, SvgDiagnosticCode.InvalidValue, "Motion keyPoints require authored keyTimes.", keyPoints.SourceSpan);
            return;
        }
        if (keyTimes == null)
        {
            plan.KeyTimes = new double[] { 0, 1 };
            plan.KeyPoints = new double[] { 0, 1 };
        }
        else
        {
            SvgKeyValueListParseResult parsedTimes = new SvgAnimationValueParser().ParseKeyTimes(keyTimes.Value, options.MaxKeyItems, options.MaxTokenCharacters);
            if (!parsedTimes.Success || parsedTimes.Values.Length < 2 || parsedTimes.Values[0] != 0 || plan.CalcMode != SvgAnimationCalcMode.Discrete && parsedTimes.Values[parsedTimes.Values.Length - 1] != 1)
            {
                new SvgMotionAnimation().Fail(plan, new SvgMotionAnimation().KeyLimit(parsedTimes.Error) ? SvgMotionAnimationError.LimitExceeded : SvgMotionAnimationError.InvalidKeys, new SvgMotionAnimation().KeyLimit(parsedTimes.Error) ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue, "Motion keyTimes require at least two values and valid endpoints.", keyTimes.SourceSpan);
                return;
            }
            plan.KeyTimes = parsedTimes.Values;
            if (keyPoints == null) plan.KeyPoints = new SvgMotionAnimation().EvenPoints(plan.KeyTimes.Length);
            else
            {
                SvgKeyValueListParseResult parsedPoints = new SvgAnimationValueParser().ParseKeyPoints(keyPoints.Value, options.MaxKeyItems, options.MaxTokenCharacters);
                if (!parsedPoints.Success || parsedPoints.Values.Length != plan.KeyTimes.Length)
                {
                    new SvgMotionAnimation().Fail(plan, new SvgMotionAnimation().KeyLimit(parsedPoints.Error) ? SvgMotionAnimationError.LimitExceeded : SvgMotionAnimationError.InvalidKeys, new SvgMotionAnimation().KeyLimit(parsedPoints.Error) ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue, "Motion keyPoints must match keyTimes.", keyPoints.SourceSpan);
                    return;
                }
                plan.KeyPoints = parsedPoints.Values;
            }
        }
        SvgAttribute? keySplines = new SvgMotionAnimation().FindAttribute(source, SvgAttributeNames.KeySplines);
        if (keySplines == null)
        {
            if (plan.CalcMode == SvgAnimationCalcMode.Spline) new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidKeys, SvgDiagnosticCode.InvalidValue, "Spline motion requires keySplines.", source.SourceSpan);
            return;
        }
        SvgKeySplineListParseResult parsedSplines = new SvgAnimationValueParser().ParseKeySplines(keySplines.Value, options.MaxKeyItems, options.MaxTokenCharacters);
        if (!parsedSplines.Success || plan.CalcMode == SvgAnimationCalcMode.Spline && parsedSplines.Splines.Count != plan.KeyTimes.Length - 1)
        {
            new SvgMotionAnimation().Fail(plan, new SvgMotionAnimation().KeyLimit(parsedSplines.Error) ? SvgMotionAnimationError.LimitExceeded : SvgMotionAnimationError.InvalidKeys, new SvgMotionAnimation().KeyLimit(parsedSplines.Error) ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue, "Motion keySplines must match the key-time intervals.", keySplines.SourceSpan);
            return;
        }
        new SvgMotionAnimation().CopySplines(parsedSplines.Splines, plan.KeySplines);
    }
SvgMotionAnimationResult SampleValues(SvgMotionAnimationPlan plan, double progress, int repeatIteration, SvgPoint origin, SvgTransformList? baseTransform, SvgMotionAnimationOptions options)
    {
        System.Collections.Generic.List<SvgPoint> zeroPoints = new System.Collections.Generic.List<SvgPoint>();
        zeroPoints.Add(new SvgPoint(0, 0));
        SvgAnimationValue underlying = SvgAnimationValue.FromPoints(zeroPoints);
        SvgAnimationCompositionResult sampled = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(plan.ValuePlan, underlying, progress, repeatIteration, options.CompositionOptions);
        if (!sampled.Success || sampled.Value == null || sampled.Value.Points == null || sampled.Value.Points.Count != 1) return new SvgMotionAnimation().SampleFailure(new SvgMotionAnimation().MapCompositionError(sampled.Error));
        SvgVector tangent = new SvgMotionAnimation().ValueTangent(plan.ValuePlan, sampled.SegmentIndex);
        return new SvgMotionAnimation().ComposeSample(plan, sampled.Value.Points[0], tangent, progress, 0, 0, repeatIteration, origin, baseTransform);
    }
SvgMotionAnimationResult ComposeSample(SvgMotionAnimationPlan plan, SvgPoint? point, SvgVector? tangent, double pathProgress, double calibratedDistance, double actualDistance, int repeatIteration, SvgPoint origin, SvgTransformList? baseTransform)
    {
        double angle = plan.RotateAngle;
        if (plan.RotateKind == SvgMotionRotateKind.Auto || plan.RotateKind == SvgMotionRotateKind.AutoReverse)
        {
            angle = (tangent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X == 0 && tangent.Y == 0 ? 0 : System.Math.Atan2(tangent.Y, tangent.X) * 180 / System.Math.PI;
            if (plan.RotateKind == SvgMotionRotateKind.AutoReverse) angle = angle + 180;
        }
        if (!SvgFiniteMath.IsFinite(angle)) return new SvgMotionAnimation().SampleFailure(SvgMotionAnimationError.NonFiniteResult);
        System.Collections.Generic.List<SvgTransform> motionItems = new System.Collections.Generic.List<SvgTransform>();
        motionItems.Add(new SvgTransform(SvgTransformKind.Translate, new double[] { (point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, point.Y }));
        if (angle != 0) motionItems.Add(new SvgTransform(SvgTransformKind.Rotate, new double[] { angle }));
        if (origin.X != 0 || origin.Y != 0) motionItems.Add(new SvgTransform(SvgTransformKind.Translate, new double[] { -origin.X, -origin.Y }));
        SvgTransformList motion = new SvgTransformList(motionItems);
        System.Collections.Generic.List<SvgTransform> composedItems = new System.Collections.Generic.List<SvgTransform>();
        if (baseTransform != null)
        {
            int baseIndex = 0;
            while (baseIndex < baseTransform.Items.Count)
            {
                if (baseTransform.Items[baseIndex] == null) return new SvgMotionAnimation().SampleFailure(SvgMotionAnimationError.InvalidArgument);
                composedItems.Add(baseTransform.Items[baseIndex]);
                baseIndex = baseIndex + 1;
            }
        }
        int motionIndex = 0;
        while (motionIndex < motion.Items.Count)
        {
            composedItems.Add(motion.Items[motionIndex]);
            motionIndex = motionIndex + 1;
        }
        SvgTransformList composed = new SvgTransformList(composedItems);
        if (!motion.ToMatrix().Success || !composed.ToMatrix().Success) return new SvgMotionAnimation().SampleFailure(SvgMotionAnimationError.NonFiniteResult);
        return new SvgMotionAnimationResult(true, SvgMotionAnimationError.None, point, tangent, pathProgress, calibratedDistance, actualDistance, angle, motion, composed, repeatIteration);
    }
double MapProgress(SvgMotionAnimationPlan plan, double progress, SvgMotionAnimationOptions options)
    {
        if (plan.CalcMode == SvgAnimationCalcMode.Paced) return progress;
        if (plan.CalcMode == SvgAnimationCalcMode.Discrete)
        {
            int selected = 0;
            int index = 0;
            while (index < plan.KeyTimes.Length)
            {
                if (progress >= plan.KeyTimes[index]) selected = index;
                index = index + 1;
            }
            if (selected >= plan.KeyPoints.Length) selected = plan.KeyPoints.Length - 1;
            return plan.KeyPoints[selected];
        }
        int segment = new SvgMotionAnimation().Segment(plan.KeyTimes, progress);
        double startTime = plan.KeyTimes[segment];
        double endTime = plan.KeyTimes[segment + 1];
        double local = endTime == startTime ? 0 : (progress - startTime) / (endTime - startTime);
        if (local < 0) local = 0;
        if (local > 1) local = 1;
        if (plan.CalcMode == SvgAnimationCalcMode.Spline)
        {
            SvgAnimationInterpolationOptions interpolation = new SvgMotionAnimation().CopyInterpolationOptions(options.CompositionOptions.Interpolation);
            interpolation.Spline = plan.KeySplines[segment];
            SvgAnimationInterpolationResult eased = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndModeAndOptions(SvgAnimationValue.FromNumber(0), SvgAnimationValue.FromNumber(1), local, SvgAnimationCalcMode.Spline, interpolation);
            if (!eased.Success) return double.NaN;
            local = (eased.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Number;
        }
        return plan.KeyPoints[segment] + (plan.KeyPoints[segment + 1] - plan.KeyPoints[segment]) * local;
    }
SvgVector ValueTangent(SvgAnimationValuePlan? plan, int segment)
    {
        if (plan == null || plan.Values == null || plan.Values.Count == 0) return new SvgVector(0, 0);
        if (plan.Values.Count == 1)
        {
            SvgPoint delta = plan.Values[0].Points[0];
            return new SvgMotionAnimation().UnitVector(delta.X, delta.Y);
        }
        int firstIndex = segment;
        if (firstIndex < 0) firstIndex = 0;
        if (firstIndex >= plan.Values.Count - 1) firstIndex = plan.Values.Count - 2;
        SvgPoint first = plan.Values[firstIndex].Points[0];
        SvgPoint second = plan.Values[firstIndex + 1].Points[0];
        double x = second.X - first.X;
        double y = second.Y - first.Y;
        if (plan.SourceKind == SvgAnimationValueSourceKind.FromBy && firstIndex == 0)
        {
            x = second.X;
            y = second.Y;
        }
        return new SvgMotionAnimation().UnitVector(x, y);
    }
SvgVector UnitVector(double x, double y)
    {
        double length = System.Math.Sqrt(x * x + y * y);
        if (!SvgFiniteMath.IsFinite(length) || length == 0) return new SvgVector(0, 0);
        return new SvgVector(x / length, y / length);
    }
SvgNode? FindMPath(SvgNode source, SvgMotionAnimationPlan plan)
    {
        SvgNode? result = null;
        int index = 0;
        while (index < source.Children.Count)
        {
            SvgNode child = source.Children[index];
            if (child.Kind == SvgNodeKind.Element && child.ElementKind == SvgElementKind.MPath)
            {
                if (result != null)
                {
                    new SvgMotionAnimation().Fail(plan, SvgMotionAnimationError.InvalidMPath, SvgDiagnosticCode.InvalidValue, "An animateMotion element cannot contain multiple mpath children.", child.SourceSpan);
                    return result;
                }
                result = child;
            }
            index = index + 1;
        }
        return result;
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
bool SinglePointValues(SvgAnimationValuePlan plan)
    {
        int index = 0;
        while (index < plan.Values.Count)
        {
            if (plan.Values[index] == null || plan.Values[index].Points == null || plan.Values[index].Points.Count != 1) return false;
            index = index + 1;
        }
        return plan.Values.Count > 0;
    }
int Segment(double[] times, double progress)
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
double[] EvenPoints(int count)
    {
        double[] result = new double[count];
        int index = 0;
        while (index < count)
        {
            result[index] = (double)index / (count - 1);
            index = index + 1;
        }
        return result;
    }
SvgPathMeasureOptions MeasureOptions(SvgPathMeasureOptions source, SvgPathLength? pathLength)
    {
        SvgPathMeasureOptions result = new SvgPathMeasureOptions();
        result.Tolerance = source.Tolerance;
        result.MaximumSubdivisionDepth = source.MaximumSubdivisionDepth;
        result.MaximumFlattenedSegments = source.MaximumFlattenedSegments;
        result.PathLength = pathLength == null ? source.PathLength : pathLength;
        return result;
    }
SvgReferenceResolveOptions CopyReferenceOptions(SvgReferenceResolveOptions source)
    {
        SvgReferenceResolveOptions result = new SvgReferenceResolveOptions();
        result.RequireReference = source.RequireReference;
        result.ExternalDocument = source.ExternalDocument;
        result.MaxUriCharacters = source.MaxUriCharacters;
        result.MaxBaseDepth = source.MaxBaseDepth;
        result.NestedDocumentOptions = source.NestedDocumentOptions;
        return result;
    }
SvgShapeResolveOptions CopyShapeOptions(SvgShapeResolveOptions source)
    {
        SvgShapeResolveOptions result = new SvgShapeResolveOptions();
        result.Attributes = source.Attributes;
        result.ComputedStyle = source.ComputedStyle;
        result.LengthContext = source.LengthContext;
        result.GeometryLimits = source.GeometryLimits;
        result.MaxTokenCharacters = source.MaxTokenCharacters;
        result.MaxCssPathCharacters = source.MaxCssPathCharacters;
        result.MaxCssPathTokens = source.MaxCssPathTokens;
        result.AncestorRendered = source.AncestorRendered;
        return result;
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
void CopySplines(System.Collections.Generic.List<SvgKeySpline> source, System.Collections.Generic.List<SvgKeySpline> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
void CopyPlanToAnimation(SvgMotionAnimationPlan plan)
    {
        SvgAnimation animation = plan.Animation;
        animation.ValueKind = SvgAnimationValueKind.Points;
        if (plan.SourceKind != SvgMotionPathSourceKind.Values)
        {
            animation.Values.Clear();
            animation.FromValue = null;
            animation.ToValue = null;
            animation.ByValue = null;
        }
        animation.CalcMode = plan.CalcMode;
        animation.KeyTimes = (double[])plan.KeyTimes.Clone();
        animation.KeyPoints = (double[])plan.KeyPoints.Clone();
        animation.KeySplines.Clear();
        new SvgMotionAnimation().CopySplines(plan.KeySplines, animation.KeySplines);
        animation.Additive = plan.Additive;
        animation.Accumulate = plan.Accumulate;
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
bool ValidAnimation(SvgAnimation animation)
    {
        return animation != null && animation.Success && animation.Kind == SvgAnimationKind.AnimateMotion && animation.TargetKind == SvgAnimationTargetKind.Motion && animation.Source != null && animation.IsCurrent();
    }
bool ValidOptions(SvgMotionAnimationOptions? options)
    {
        if (options == null || options.ReferenceOptions == null || options.ShapeOptions == null || options.MeasureOptions == null || options.CompositionOptions == null || options.CompositionOptions.Interpolation == null) return false;
        if (options.MaxPathSegments < 1 || options.MaxTokenCharacters < 1 || options.MaxKeyItems < 1) return false;
        if (!SvgFiniteMath.IsFinite(options.MeasureOptions.Tolerance) || options.MeasureOptions.Tolerance <= 0 || options.MeasureOptions.MaximumSubdivisionDepth < 0 || options.MeasureOptions.MaximumFlattenedSegments < 1) return false;
        return options.CompositionOptions.MaxValues > 0 && options.CompositionOptions.MaxRepeatAccumulations >= 0;
    }
bool ValidPoint(SvgPoint point)
    {
        return SvgFiniteMath.IsFinite(point.X) && SvgFiniteMath.IsFinite(point.Y);
    }
bool KeyLimit(SvgTimingParseError error)
    {
        return error == SvgTimingParseError.ItemLimitExceeded || error == SvgTimingParseError.TokenTooLong;
    }
SvgMotionAnimationError MapMetricError(SvgPathMetricError error)
    {
        if (error == SvgPathMetricError.SegmentLimitExceeded || error == SvgPathMetricError.SubdivisionLimitExceeded) return SvgMotionAnimationError.LimitExceeded;
        return SvgMotionAnimationError.NonFiniteResult;
    }
SvgMotionAnimationError MapCompositionError(SvgAnimationCompositionError error)
    {
        if (error == SvgAnimationCompositionError.LimitExceeded) return SvgMotionAnimationError.LimitExceeded;
        if (error == SvgAnimationCompositionError.InvalidArgument) return SvgMotionAnimationError.InvalidArgument;
        return SvgMotionAnimationError.InvalidValues;
    }
void Fail(SvgMotionAnimationPlan plan, SvgMotionAnimationError error, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source);
        plan.Diagnostics.Add(diagnostic);
        if (plan.Animation != null) plan.Animation.Diagnostics.Add(diagnostic);
        plan.Error = error;
        plan.Success = false;
    }
SvgMotionAnimationResult SampleFailure(SvgMotionAnimationError error)
    {
        return new SvgMotionAnimationResult(false, error, null, null, 0, 0, 0, 0, null, null, 0);
    }
}
