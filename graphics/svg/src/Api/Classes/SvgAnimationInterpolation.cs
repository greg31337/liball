namespace svg;

/// <summary>Parses and interpolates typed SVG animation values without string coercion.</summary>
public class SvgAnimationInterpolator : ISvgAnimationInterpolator
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgAnimationInterpolator()
    {
    }

    /// <summary>Parses authored text as one typed animation value.</summary>
    /// <param name="kind">The requested value family.</param>
    /// <param name="text">The authored value.</param>
    /// <returns>The parsed typed value.</returns>
    public SvgAnimationValueParseResult ParseWithKindAndText(SvgAnimationValueKind kind, string text)
    {
        return new SvgAnimationInterpolator().ParseWithKindAndTextAndOptions(kind, text, new SvgAnimationInterpolationOptions());
    }

    /// <summary>Parses authored text with explicit bounds.</summary>
    /// <param name="kind">The requested value family.</param>
    /// <param name="text">The authored value.</param>
    /// <param name="options">The parsing and interpolation options.</param>
    /// <returns>The parsed typed value.</returns>
    public SvgAnimationValueParseResult ParseWithKindAndTextAndOptions(SvgAnimationValueKind kind, string? text, SvgAnimationInterpolationOptions options)
    {
        if (!new SvgAnimationInterpolator().KnownKind(kind) || text == null || !new SvgAnimationInterpolator().ValidOptions(options)) return new SvgAnimationInterpolator().ParseFailure(SvgAnimationInterpolationError.InvalidArgument, 0);
        if (kind == SvgAnimationValueKind.String)
        {
            if (text.Length > options.MaxTokenCharacters) return new SvgAnimationInterpolator().ParseFailure(SvgAnimationInterpolationError.LimitExceeded, options.MaxTokenCharacters);
            return new SvgAnimationInterpolator().ParseSuccess(SvgAnimationValue.FromString(text));
        }
        if (kind == SvgAnimationValueKind.Number)
        {
            SvgNumberParseResult number = SvgNumberParser.ParseNumber(text, options.MaxTokenCharacters);
            return number.Success ? new SvgAnimationInterpolator().ParseSuccess(SvgAnimationValue.FromNumber(number.Value)) : new SvgAnimationInterpolator().ParseFailure(new SvgAnimationInterpolator().NumberError(number.Error), number.ErrorOffset);
        }
        if (kind == SvgAnimationValueKind.Length)
        {
            SvgLengthParseResult length = SvgLengthParser.ParseLength(text, true, true, options.MaxTokenCharacters);
            return length.Success ? new SvgAnimationInterpolator().ParseSuccess(SvgAnimationValue.FromLength((length.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")))) : new SvgAnimationInterpolator().ParseFailure(new SvgAnimationInterpolator().LengthError(length.Error), length.ErrorOffset);
        }
        if (kind == SvgAnimationValueKind.Color)
        {
            SvgColorParseResult color = new SvgColorParser().Parse(text, options.MaxTokenCharacters);
            return color.Success ? new SvgAnimationInterpolator().ParseSuccess(SvgAnimationValue.FromColor((color.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")))) : new SvgAnimationInterpolator().ParseFailure(color.Error == SvgColorParseError.TokenTooLong ? SvgAnimationInterpolationError.LimitExceeded : SvgAnimationInterpolationError.InvalidValue, color.ErrorOffset);
        }
        if (kind == SvgAnimationValueKind.Path)
        {
            SvgPathParseResult path = new SvgPathParser().Parse(text, options.MaxItems, options.MaxTokenCharacters);
            if (!path.Success) return new SvgAnimationInterpolator().ParseFailure(path.Error == SvgPathParseError.SegmentLimitExceeded || path.Error == SvgPathParseError.TokenTooLong ? SvgAnimationInterpolationError.LimitExceeded : SvgAnimationInterpolationError.InvalidValue, path.ErrorOffset);
            SvgPathNormalizeResult normalized = new SvgPathNormalizer().NormalizeWithPathAndOptions(path.PathData, new SvgAnimationInterpolator().NormalizeOptions(options.MaxItems));
            return normalized.Success ? new SvgAnimationInterpolator().ParseSuccess(SvgAnimationValue.FromPath((normalized.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")))) : new SvgAnimationInterpolator().ParseFailure(normalized.Error == SvgPathNormalizeError.CommandLimitExceeded ? SvgAnimationInterpolationError.LimitExceeded : SvgAnimationInterpolationError.InvalidValue, 0);
        }
        if (kind == SvgAnimationValueKind.Points)
        {
            SvgPointListResult points = SvgListParser.ParsePoints(text, options.MaxItems, options.MaxTokenCharacters);
            if (!points.Success) return new SvgAnimationInterpolator().ParseFailure(points.Error == SvgListParseError.ItemLimitExceeded || points.NumberError == SvgNumberParseError.TokenTooLong ? SvgAnimationInterpolationError.LimitExceeded : SvgAnimationInterpolationError.InvalidValue, points.ErrorOffset);
            return new SvgAnimationInterpolator().ParseSuccess(SvgAnimationValue.FromPoints(new SvgAnimationInterpolator().Points(points.Coordinates)));
        }
        if (kind == SvgAnimationValueKind.Transform)
        {
            string transformText = text.Trim() == "none" ? "" : text;
            SvgTransformParseResult transform = SvgTransformParser.Parse(transformText, options.MaxItems, options.MaxTokenCharacters);
            return transform.Success ? new SvgAnimationInterpolator().ParseSuccess(SvgAnimationValue.FromTransform(transform.TransformList)) : new SvgAnimationInterpolator().ParseFailure(transform.Error == SvgTransformParseError.TransformLimitExceeded || transform.Error == SvgTransformParseError.TokenTooLong ? SvgAnimationInterpolationError.LimitExceeded : SvgAnimationInterpolationError.InvalidValue, transform.ErrorOffset);
        }
        if (kind == SvgAnimationValueKind.ViewBox)
        {
            SvgViewBoxParseResult viewBox = SvgViewBoxParser.ParseViewBox(text, options.MaxTokenCharacters);
            return viewBox.Success ? new SvgAnimationInterpolator().ParseSuccess(SvgAnimationValue.FromViewBox((viewBox.ViewBox ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")))) : new SvgAnimationInterpolator().ParseFailure(viewBox.Error == SvgViewBoxParseError.TokenTooLong ? SvgAnimationInterpolationError.LimitExceeded : SvgAnimationInterpolationError.InvalidValue, viewBox.ErrorOffset);
        }
        SvgPaintParseResult paint = new SvgPaintParser().Parse(text, options.MaxItems, options.MaxTokenCharacters, false);
        return paint.Success ? new SvgAnimationInterpolator().ParseSuccess(SvgAnimationValue.FromPaint((paint.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")))) : new SvgAnimationInterpolator().ParseFailure(paint.Error == SvgPaintParseError.FallbackLimitExceeded || paint.Error == SvgPaintParseError.TokenTooLong ? SvgAnimationInterpolationError.LimitExceeded : SvgAnimationInterpolationError.InvalidValue, paint.ErrorOffset);
    }

    /// <summary>Interpolates two typed values with default conversion options.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="progress">The finite progress in the unit interval.</param>
    /// <param name="mode">The interpolation mode.</param>
    /// <returns>The typed interpolated value.</returns>
    public SvgAnimationInterpolationResult InterpolateWithFromAndToAndProgressAndMode(SvgAnimationValue from, SvgAnimationValue to, double progress, SvgAnimationCalcMode mode)
    {
        return new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndModeAndOptions(from, to, progress, mode, new SvgAnimationInterpolationOptions());
    }

    /// <summary>Interpolates two typed values with explicit conversion, color-space, spline, and bounds.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="progress">The finite progress in the unit interval.</param>
    /// <param name="mode">The interpolation mode.</param>
    /// <param name="options">The interpolation options.</param>
    /// <returns>The typed interpolated value.</returns>
    public SvgAnimationInterpolationResult InterpolateWithFromAndToAndProgressAndModeAndOptions(SvgAnimationValue from, SvgAnimationValue to, double progress, SvgAnimationCalcMode mode, SvgAnimationInterpolationOptions options)
    {
        if (!new SvgAnimationInterpolator().ValidPairArguments(from, to, progress, mode, options)) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.InvalidArgument);
        double mapped = progress;
        if (mode == SvgAnimationCalcMode.Discrete) mapped = progress < 0.5 ? 0 : 1;
        else if (mode == SvgAnimationCalcMode.Spline)
        {
            if (options.Spline == null) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.InvalidSplines);
            mapped = new SvgAnimationInterpolator().Ease(progress, options.Spline);
        }
        SvgAnimationInterpolationResult result = new SvgAnimationInterpolator().InterpolatePair(from, to, mapped, options);
        if (!result.Success) return result;
        result.EffectiveProgress = mapped;
        SvgAnimationInterpolationResult distance = new SvgAnimationInterpolator().Distance(from, to, options);
        if (!distance.Success && mode == SvgAnimationCalcMode.Paced) return distance;
        if (distance.Success) result.Distance = distance.Distance;
        return result;
    }

    /// <summary>Interpolates an evenly spaced or paced sequence of typed values.</summary>
    /// <param name="values">At least two compatible typed values.</param>
    /// <param name="progress">The finite overall progress in the unit interval.</param>
    /// <param name="mode">The interpolation mode.</param>
    /// <param name="splines">One spline per segment for spline mode, otherwise an empty list.</param>
    /// <param name="options">The interpolation options.</param>
    /// <returns>The selected segment and typed interpolated value.</returns>
    public SvgAnimationInterpolationResult InterpolateSequence(System.Collections.Generic.List<SvgAnimationValue>? values, double progress, SvgAnimationCalcMode mode, System.Collections.Generic.List<SvgKeySpline>? splines, SvgAnimationInterpolationOptions options)
    {
        if (values == null || splines == null || values.Count < 2 || values.Count > 1000000 || !SvgFiniteMath.IsFinite(progress) || progress < 0 || progress > 1 || !new SvgAnimationInterpolator().KnownMode(mode) || !new SvgAnimationInterpolator().ValidOptions(options)) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.InvalidArgument);
        if (values.Count > options.MaxItems) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.LimitExceeded);
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null || values[index].Kind != values[0].Kind) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.IncompatibleValues);
            index = index + 1;
        }
        if (mode == SvgAnimationCalcMode.Discrete)
        {
            int selected = progress >= 1 ? values.Count - 1 : (int)System.Math.Floor(progress * values.Count);
            SvgAnimationInterpolationResult discrete = new SvgAnimationInterpolator().InterpolatePair(values[selected], values[selected], 0, options);
            if (discrete.Success)
            {
                discrete.SegmentIndex = selected;
                discrete.EffectiveProgress = 0;
            }
            return discrete;
        }
        if (mode == SvgAnimationCalcMode.Paced) return new SvgAnimationInterpolator().InterpolatePaced(values, progress, options);
        int segment = progress >= 1 ? values.Count - 2 : (int)System.Math.Floor(progress * (values.Count - 1));
        double local = progress >= 1 ? 1 : progress * (values.Count - 1) - segment;
        if (mode == SvgAnimationCalcMode.Spline)
        {
            if (splines.Count != values.Count - 1) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.InvalidSplines);
            local = new SvgAnimationInterpolator().Ease(local, splines[segment]);
        }
        SvgAnimationInterpolationResult result = new SvgAnimationInterpolator().InterpolatePair(values[segment], values[segment + 1], local, options);
        if (result.Success)
        {
            result.SegmentIndex = segment;
            result.EffectiveProgress = local;
            SvgAnimationInterpolationResult distance = new SvgAnimationInterpolator().Distance(values[segment], values[segment + 1], options);
            if (distance.Success) result.Distance = distance.Distance;
        }
        return result;
    }

    /// <summary>Computes the finite typed distance used by paced interpolation.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="options">The conversion and color-space options.</param>
    /// <returns>A successful result whose Distance field contains the typed distance.</returns>
    public SvgAnimationInterpolationResult Distance(SvgAnimationValue? from, SvgAnimationValue? to, SvgAnimationInterpolationOptions options)
    {
        if (from == null || to == null || from.Kind != to.Kind || !new SvgAnimationInterpolator().ValidOptions(options)) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.InvalidArgument);
        double distance;
        if (from.Kind == SvgAnimationValueKind.Number) distance = System.Math.Abs(to.Number - from.Number);
        else if (from.Kind == SvgAnimationValueKind.Length)
        {
            double[]? lengths = new SvgAnimationInterpolator().LengthEndpoints(from.Length, to.Length, options);
            if (lengths == null) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.IncompatibleValues);
            distance = System.Math.Abs(lengths[1] - lengths[0]);
        }
        else if (from.Kind == SvgAnimationValueKind.Color)
        {
            double[]? colors = new SvgAnimationInterpolator().ColorEndpoints(from.Color, to.Color, options.ColorSpace);
            if (colors == null) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.IncompatibleValues);
            distance = new SvgAnimationInterpolator().Euclidean(colors);
        }
        else if (from.Kind == SvgAnimationValueKind.Points) distance = new SvgAnimationInterpolator().PointDistance(from.Points, to.Points);
        else if (from.Kind == SvgAnimationValueKind.Transform) distance = new SvgAnimationInterpolator().TransformDistance(from.Transform, to.Transform);
        else if (from.Kind == SvgAnimationValueKind.ViewBox) distance = new SvgAnimationInterpolator().ViewBoxDistance(from.ViewBox, to.ViewBox);
        else if (from.Kind == SvgAnimationValueKind.Path) distance = new SvgAnimationInterpolator().PathDistance(from.Path, to.Path, options.MaxItems);
        else if (from.Kind == SvgAnimationValueKind.Paint)
        {
            if (from.Paint == null || to.Paint == null || from.Paint.Kind != SvgPaintKind.Color || to.Paint.Kind != SvgPaintKind.Color) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.UnsupportedInterpolation);
            double[]? paints = new SvgAnimationInterpolator().ColorEndpoints(from.Paint.Color, to.Paint.Color, options.ColorSpace);
            if (paints == null) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.IncompatibleValues);
            distance = new SvgAnimationInterpolator().Euclidean(paints);
        }
        else return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.UnsupportedInterpolation);
        if (!SvgFiniteMath.IsFinite(distance) || distance < 0) return new SvgAnimationInterpolator().Failure(distance < 0 ? SvgAnimationInterpolationError.IncompatibleValues : SvgAnimationInterpolationError.NonFiniteResult);
        return new SvgAnimationInterpolationResult(true, SvgAnimationInterpolationError.None, null, 0, 0, distance);
    }
SvgAnimationInterpolationResult InterpolatePaced(System.Collections.Generic.List<SvgAnimationValue> values, double progress, SvgAnimationInterpolationOptions options)
    {
        double[] distances = new double[values.Count - 1];
        double total = 0;
        int index = 0;
        while (index < distances.Length)
        {
            SvgAnimationInterpolationResult measured = new SvgAnimationInterpolator().Distance(values[index], values[index + 1], options);
            if (!measured.Success) return measured;
            distances[index] = measured.Distance;
            total = total + measured.Distance;
            if (!SvgFiniteMath.IsFinite(total)) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.NonFiniteResult);
            index = index + 1;
        }
        if (total == 0)
        {
            SvgAnimationInterpolationResult same = new SvgAnimationInterpolator().InterpolatePair(values[0], values[values.Count - 1], progress, options);
            if (same.Success) same.SegmentIndex = progress >= 1 ? values.Count - 2 : 0;
            return same;
        }
        double target = progress * total;
        double traversed = 0;
        int segment = distances.Length - 1;
        index = 0;
        while (index < distances.Length)
        {
            if (target <= traversed + distances[index])
            {
                segment = index;
                break;
            }
            traversed = traversed + distances[index];
            index = index + 1;
        }
        double local = distances[segment] == 0 ? 0 : (target - traversed) / distances[segment];
        if (local < 0) local = 0;
        if (local > 1) local = 1;
        SvgAnimationInterpolationResult result = new SvgAnimationInterpolator().InterpolatePair(values[segment], values[segment + 1], local, options);
        if (result.Success)
        {
            result.SegmentIndex = segment;
            result.EffectiveProgress = local;
            result.Distance = distances[segment];
        }
        return result;
    }
SvgAnimationInterpolationResult InterpolatePair(SvgAnimationValue from, SvgAnimationValue to, double progress, SvgAnimationInterpolationOptions options)
    {
        if (from.Kind == SvgAnimationValueKind.String) return new SvgAnimationInterpolator().Success(progress < 0.5 ? SvgAnimationValue.FromString(from.Text) : SvgAnimationValue.FromString(to.Text));
        if (from.Kind == SvgAnimationValueKind.Number) return new SvgAnimationInterpolator().FiniteNumber(new SvgAnimationInterpolator().Lerp(from.Number, to.Number, progress));
        if (from.Kind == SvgAnimationValueKind.Length) return new SvgAnimationInterpolator().InterpolateLength(from.Length, to.Length, progress, options);
        if (from.Kind == SvgAnimationValueKind.Color) return new SvgAnimationInterpolator().InterpolateColor(from.Color, to.Color, progress, options.ColorSpace, false);
        if (from.Kind == SvgAnimationValueKind.Points) return new SvgAnimationInterpolator().InterpolatePoints(from.Points, to.Points, progress);
        if (from.Kind == SvgAnimationValueKind.Transform) return new SvgAnimationInterpolator().InterpolateTransform(from.Transform, to.Transform, progress);
        if (from.Kind == SvgAnimationValueKind.ViewBox) return new SvgAnimationInterpolator().InterpolateViewBox(from.ViewBox, to.ViewBox, progress);
        if (from.Kind == SvgAnimationValueKind.Path) return new SvgAnimationInterpolator().InterpolatePath(from.Path, to.Path, progress, options.MaxItems);
        if (from.Kind == SvgAnimationValueKind.Paint) return new SvgAnimationInterpolator().InterpolatePaint(from.Paint, to.Paint, progress, options.ColorSpace);
        return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.InvalidArgument);
    }
SvgAnimationInterpolationResult InterpolateLength(SvgLength? from, SvgLength? to, double progress, SvgAnimationInterpolationOptions options)
    {
        if (from == null || to == null) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.InvalidArgument);
        if (from.Unit == to.Unit)
        {
            double value = new SvgAnimationInterpolator().Lerp(from.Value, to.Value, progress);
            return SvgFiniteMath.IsFinite(value) ? new SvgAnimationInterpolator().Success(SvgAnimationValue.FromLength(new SvgLength(value, from.Unit))) : new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.NonFiniteResult);
        }
        double[]? endpoints = new SvgAnimationInterpolator().LengthEndpoints(from, to, options);
        if (endpoints == null) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.IncompatibleValues);
        double resolved = new SvgAnimationInterpolator().Lerp(endpoints[0], endpoints[1], progress);
        return SvgFiniteMath.IsFinite(resolved) ? new SvgAnimationInterpolator().Success(SvgAnimationValue.FromLength(new SvgLength(resolved, SvgLengthUnit.Number))) : new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.NonFiniteResult);
    }
double[]? LengthEndpoints(SvgLength? from, SvgLength? to, SvgAnimationInterpolationOptions options)
    {
        if (from == null || to == null) return null;
        if (from.Unit == to.Unit) return new double[] { from.Value, to.Value };
        SvgLengthEvaluationResult first = from.Evaluate(options.LengthContext, options.LengthPercentageBasis);
        SvgLengthEvaluationResult second = to.Evaluate(options.LengthContext, options.LengthPercentageBasis);
        return first.Success && second.Success ? new double[] { first.Value, second.Value } : null;
    }
SvgAnimationInterpolationResult InterpolateColor(SvgColor? from, SvgColor? to, double progress, SvgColorSpace space, bool paint)
    {
        SvgColorOperationResult color = SvgColorMath.Interpolate((from ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (to ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), progress, space);
        if (!color.Success) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.IncompatibleValues);
        return paint ? new SvgAnimationInterpolator().Success(SvgAnimationValue.FromPaint(SvgPaint.Solid((color.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))))) : new SvgAnimationInterpolator().Success(SvgAnimationValue.FromColor((color.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))));
    }
double[]? ColorEndpoints(SvgColor? from, SvgColor? to, SvgColorSpace space)
    {
        if (from == null || to == null || from.IsCurrentColor || to.IsCurrentColor) return null;
        SvgColorOperationResult first = from.ConvertTo(space);
        SvgColorOperationResult second = to.ConvertTo(space);
        if (!first.Success || !second.Success) return null;
        return new double[] { (second.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red - (first.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red, second.Color.Green - first.Color.Green, second.Color.Blue - first.Color.Blue, second.Color.Alpha - first.Color.Alpha };
    }
SvgAnimationInterpolationResult InterpolatePoints(System.Collections.Generic.List<SvgPoint> from, System.Collections.Generic.List<SvgPoint> to, double progress)
    {
        if (!new SvgAnimationInterpolator().CompatiblePoints(from, to)) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.IncompatibleValues);
        System.Collections.Generic.List<SvgPoint> points = new System.Collections.Generic.List<SvgPoint>();
        int index = 0;
        while (index < from.Count)
        {
            double x = new SvgAnimationInterpolator().Lerp(from[index].X, to[index].X, progress);
            double y = new SvgAnimationInterpolator().Lerp(from[index].Y, to[index].Y, progress);
            if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.NonFiniteResult);
            points.Add(new SvgPoint(x, y));
            index = index + 1;
        }
        return new SvgAnimationInterpolator().Success(SvgAnimationValue.FromPoints(points));
    }
double PointDistance(System.Collections.Generic.List<SvgPoint> from, System.Collections.Generic.List<SvgPoint> to)
    {
        if (!new SvgAnimationInterpolator().CompatiblePoints(from, to)) return -1;
        double sum = 0;
        int index = 0;
        while (index < from.Count)
        {
            double x = to[index].X - from[index].X;
            double y = to[index].Y - from[index].Y;
            sum = sum + x * x + y * y;
            index = index + 1;
        }
        return System.Math.Sqrt(sum);
    }
bool CompatiblePoints(System.Collections.Generic.List<SvgPoint>? from, System.Collections.Generic.List<SvgPoint>? to)
    {
        if (from == null || to == null || from.Count != to.Count) return false;
        int index = 0;
        while (index < from.Count)
        {
            if (from[index] == null || to[index] == null) return false;
            index = index + 1;
        }
        return true;
    }
SvgAnimationInterpolationResult InterpolateTransform(SvgTransformList? from, SvgTransformList? to, double progress)
    {
        if (!new SvgAnimationInterpolator().CompatibleTransforms(from, to)) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.IncompatibleValues);
        System.Collections.Generic.List<SvgTransform> items = new System.Collections.Generic.List<SvgTransform>();
        int index = 0;
        while (index < (from ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Items.Count)
        {
            SvgTransform first = from.Items[index];
            SvgTransform second = (to ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Items[index];
            int count = new SvgAnimationInterpolator().ExpandedTransformCount(first.Kind, first.Values.Length, second.Values.Length);
            double[] firstValues = new SvgAnimationInterpolator().ExpandedTransformValues(first, count);
            double[] secondValues = new SvgAnimationInterpolator().ExpandedTransformValues(second, count);
            double[] values = new double[count];
            int valueIndex = 0;
            while (valueIndex < count)
            {
                values[valueIndex] = new SvgAnimationInterpolator().Lerp(firstValues[valueIndex], secondValues[valueIndex], progress);
                if (!SvgFiniteMath.IsFinite(values[valueIndex])) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.NonFiniteResult);
                valueIndex = valueIndex + 1;
            }
            items.Add(new SvgTransform(first.Kind, values));
            index = index + 1;
        }
        return new SvgAnimationInterpolator().Success(SvgAnimationValue.FromTransform(new SvgTransformList(items)));
    }
double TransformDistance(SvgTransformList? from, SvgTransformList? to)
    {
        if (!new SvgAnimationInterpolator().CompatibleTransforms(from, to)) return -1;
        double sum = 0;
        int index = 0;
        while (index < (from ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Items.Count)
        {
            int count = new SvgAnimationInterpolator().ExpandedTransformCount(from.Items[index].Kind, from.Items[index].Values.Length, (to ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Items[index].Values.Length);
            double[] first = new SvgAnimationInterpolator().ExpandedTransformValues(from.Items[index], count);
            double[] second = new SvgAnimationInterpolator().ExpandedTransformValues(to.Items[index], count);
            int valueIndex = 0;
            while (valueIndex < count)
            {
                double difference = second[valueIndex] - first[valueIndex];
                sum = sum + difference * difference;
                valueIndex = valueIndex + 1;
            }
            index = index + 1;
        }
        return System.Math.Sqrt(sum);
    }
bool CompatibleTransforms(SvgTransformList? from, SvgTransformList? to)
    {
        if (from == null || to == null || from.Items == null || to.Items == null || from.Items.Count != to.Items.Count) return false;
        int index = 0;
        while (index < from.Items.Count)
        {
            if (from.Items[index] == null || to.Items[index] == null || from.Items[index].Kind != to.Items[index].Kind) return false;
            index = index + 1;
        }
        return true;
    }
int ExpandedTransformCount(SvgTransformKind kind, int first, int second)
    {
        if (kind == SvgTransformKind.Translate || kind == SvgTransformKind.Scale) return first > second ? first : second;
        if (kind == SvgTransformKind.Rotate) return first > second ? first : second;
        return first;
    }
double[] ExpandedTransformValues(SvgTransform transform, int count)
    {
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            if (index < transform.Values.Length) values[index] = transform.Values[index];
            else if (transform.Kind == SvgTransformKind.Scale && index == 1) values[index] = transform.Values[0];
            else values[index] = 0;
            index = index + 1;
        }
        return values;
    }
SvgAnimationInterpolationResult InterpolateViewBox(SvgViewBox? from, SvgViewBox? to, double progress)
    {
        if (from == null || to == null) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.InvalidArgument);
        double x = new SvgAnimationInterpolator().Lerp(from.X, to.X, progress);
        double y = new SvgAnimationInterpolator().Lerp(from.Y, to.Y, progress);
        double width = new SvgAnimationInterpolator().Lerp(from.Width, to.Width, progress);
        double height = new SvgAnimationInterpolator().Lerp(from.Height, to.Height, progress);
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y) || !SvgFiniteMath.IsFinite(width) || !SvgFiniteMath.IsFinite(height) || width < 0 || height < 0) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.NonFiniteResult);
        return new SvgAnimationInterpolator().Success(SvgAnimationValue.FromViewBox(new SvgViewBox(x, y, width, height)));
    }
double ViewBoxDistance(SvgViewBox? from, SvgViewBox? to)
    {
        if (from == null || to == null) return -1;
        return new SvgAnimationInterpolator().Euclidean(new double[] { to.X - from.X, to.Y - from.Y, to.Width - from.Width, to.Height - from.Height });
    }
SvgAnimationInterpolationResult InterpolatePaint(SvgPaint? from, SvgPaint? to, double progress, SvgColorSpace space)
    {
        if (from == null || to == null) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.InvalidArgument);
        if (from.Kind == SvgPaintKind.Color && to.Kind == SvgPaintKind.Color) return new SvgAnimationInterpolator().InterpolateColor(from.Color, to.Color, progress, space, true);
        if (new SvgAnimationInterpolator().SamePaint(from, to)) return new SvgAnimationInterpolator().Success(SvgAnimationValue.FromPaint(progress < 0.5 ? from : to));
        return new SvgAnimationInterpolator().Success(SvgAnimationValue.FromPaint(progress < 0.5 ? from : to));
    }
bool SamePaint(SvgPaint first, SvgPaint second)
    {
        if (first.Kind != second.Kind) return false;
        if (first.Kind == SvgPaintKind.Url) return first.Url == second.Url;
        return first.Kind != SvgPaintKind.Color;
    }
SvgAnimationInterpolationResult InterpolatePath(SvgPathData? from, SvgPathData? to, double progress, int maximum)
    {
        SvgPathData[]? paths = new SvgAnimationInterpolator().CompatiblePaths(from, to, maximum);
        if (paths == null) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.IncompatibleValues);
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        int index = 0;
        while (index < paths[0].Commands.Count)
        {
            SvgPathCommand first = paths[0].Commands[index];
            SvgPathCommand second = paths[1].Commands[index];
            double[] values = new double[first.Values.Length];
            int valueIndex = 0;
            while (valueIndex < values.Length)
            {
                if (first.Kind == SvgPathCommandKind.ArcTo && (valueIndex == 3 || valueIndex == 4)) values[valueIndex] = progress < 0.5 ? first.Values[valueIndex] : second.Values[valueIndex];
                else values[valueIndex] = new SvgAnimationInterpolator().Lerp(first.Values[valueIndex], second.Values[valueIndex], progress);
                if (!SvgFiniteMath.IsFinite(values[valueIndex])) return new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.NonFiniteResult);
                valueIndex = valueIndex + 1;
            }
            new SvgAnimationInterpolator().AddPathCommand(builder, first.Kind, values);
            index = index + 1;
        }
        return new SvgAnimationInterpolator().Success(SvgAnimationValue.FromPath(builder.Build()));
    }
double PathDistance(SvgPathData? from, SvgPathData? to, int maximum)
    {
        SvgPathData[]? paths = new SvgAnimationInterpolator().CompatiblePaths(from, to, maximum);
        if (paths == null) return -1;
        double sum = 0;
        int index = 0;
        while (index < paths[0].Commands.Count)
        {
            SvgPathCommand first = paths[0].Commands[index];
            SvgPathCommand second = paths[1].Commands[index];
            int valueIndex = 0;
            while (valueIndex < first.Values.Length)
            {
                if (first.Kind != SvgPathCommandKind.ArcTo || valueIndex != 3 && valueIndex != 4)
                {
                    double difference = second.Values[valueIndex] - first.Values[valueIndex];
                    sum = sum + difference * difference;
                }
                valueIndex = valueIndex + 1;
            }
            index = index + 1;
        }
        return System.Math.Sqrt(sum);
    }
SvgPathData[]? CompatiblePaths(SvgPathData? from, SvgPathData? to, int maximum)
    {
        if (from == null || to == null) return null;
        SvgPathNormalizeResult first = new SvgPathNormalizer().NormalizeWithPathAndOptions(from, new SvgAnimationInterpolator().NormalizeOptions(maximum));
        SvgPathNormalizeResult second = new SvgPathNormalizer().NormalizeWithPathAndOptions(to, new SvgAnimationInterpolator().NormalizeOptions(maximum));
        if (!first.Success || !second.Success || (first.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count != (second.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count) return null;
        int index = 0;
        while (index < first.PathData.Commands.Count)
        {
            SvgPathCommand a = first.PathData.Commands[index];
            SvgPathCommand b = second.PathData.Commands[index];
            if (a.Kind != b.Kind || a.Values.Length != b.Values.Length) return null;
            index = index + 1;
        }
        return new SvgPathData[] { first.PathData, second.PathData };
    }
void AddPathCommand(SvgGeneratedPathBuilder builder, SvgPathCommandKind kind, double[] values)
    {
        if (kind == SvgPathCommandKind.MoveTo) builder.MoveTo(values[0], values[1]);
        else if (kind == SvgPathCommandKind.LineTo) builder.LineTo(values[0], values[1]);
        else if (kind == SvgPathCommandKind.CubicCurveTo) builder.CubicTo(values[0], values[1], values[2], values[3], values[4], values[5]);
        else if (kind == SvgPathCommandKind.QuadraticCurveTo) builder.QuadraticTo(values[0], values[1], values[2], values[3]);
        else if (kind == SvgPathCommandKind.ArcTo) builder.ArcToWithParameters(values[0], values[1], values[2], values[3] != 0, values[4] != 0, values[5], values[6]);
        else if (kind == SvgPathCommandKind.ClosePath) builder.Close();
    }
double Ease(double progress, SvgKeySpline spline)
    {
        double low = 0;
        double high = 1;
        double parameter = progress;
        int index = 0;
        while (index < 24)
        {
            double x = new SvgAnimationInterpolator().Cubic(parameter, spline.X1, spline.X2);
            if (x < progress) low = parameter;
            else high = parameter;
            parameter = (low + high) * 0.5;
            index = index + 1;
        }
        double value = new SvgAnimationInterpolator().Cubic(parameter, spline.Y1, spline.Y2);
        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }
double Cubic(double parameter, double first, double second)
    {
        double inverse = 1 - parameter;
        return 3 * inverse * inverse * parameter * first + 3 * inverse * parameter * parameter * second + parameter * parameter * parameter;
    }
double Lerp(double first, double second, double progress)
    {
        return first + (second - first) * progress;
    }
double Euclidean(double[] differences)
    {
        double sum = 0;
        int index = 0;
        while (index < differences.Length)
        {
            sum = sum + differences[index] * differences[index];
            index = index + 1;
        }
        return System.Math.Sqrt(sum);
    }
System.Collections.Generic.List<SvgPoint> Points(double[] coordinates)
    {
        System.Collections.Generic.List<SvgPoint> result = new System.Collections.Generic.List<SvgPoint>();
        int index = 0;
        while (index < coordinates.Length)
        {
            result.Add(new SvgPoint(coordinates[index], coordinates[index + 1]));
            index = index + 2;
        }
        return result;
    }
SvgPathNormalizeOptions NormalizeOptions(int maximum)
    {
        SvgPathNormalizeOptions options = new SvgPathNormalizeOptions();
        options.MaximumCommands = maximum;
        return options;
    }
SvgAnimationInterpolationError NumberError(SvgNumberParseError error)
    {
        return error == SvgNumberParseError.TokenTooLong ? SvgAnimationInterpolationError.LimitExceeded : SvgAnimationInterpolationError.InvalidValue;
    }
SvgAnimationInterpolationError LengthError(SvgLengthParseError error)
    {
        return error == SvgLengthParseError.TokenTooLong ? SvgAnimationInterpolationError.LimitExceeded : SvgAnimationInterpolationError.InvalidValue;
    }
bool ValidPairArguments(SvgAnimationValue from, SvgAnimationValue to, double progress, SvgAnimationCalcMode mode, SvgAnimationInterpolationOptions options)
    {
        return from != null && to != null && from.Kind == to.Kind && new SvgAnimationInterpolator().KnownKind(from.Kind) && SvgFiniteMath.IsFinite(progress) && progress >= 0 && progress <= 1 && new SvgAnimationInterpolator().KnownMode(mode) && new SvgAnimationInterpolator().ValidOptions(options);
    }
bool ValidOptions(SvgAnimationInterpolationOptions options)
    {
        return options != null && options.LengthContext != null && SvgColorMath.IsKnownSpace(options.ColorSpace) && options.MaxItems > 0 && options.MaxTokenCharacters > 0;
    }
bool KnownKind(SvgAnimationValueKind kind)
    {
        return kind >= SvgAnimationValueKind.Number && kind <= SvgAnimationValueKind.String;
    }
bool KnownMode(SvgAnimationCalcMode mode)
    {
        return mode >= SvgAnimationCalcMode.Discrete && mode <= SvgAnimationCalcMode.Spline;
    }
SvgAnimationValueParseResult ParseSuccess(SvgAnimationValue value)
    {
        return new SvgAnimationValueParseResult(true, SvgAnimationInterpolationError.None, value, -1);
    }
SvgAnimationValueParseResult ParseFailure(SvgAnimationInterpolationError error, int offset)
    {
        return new SvgAnimationValueParseResult(false, error, null, offset);
    }
SvgAnimationInterpolationResult FiniteNumber(double value)
    {
        return SvgFiniteMath.IsFinite(value) ? new SvgAnimationInterpolator().Success(SvgAnimationValue.FromNumber(value)) : new SvgAnimationInterpolator().Failure(SvgAnimationInterpolationError.NonFiniteResult);
    }
SvgAnimationInterpolationResult Success(SvgAnimationValue value)
    {
        return new SvgAnimationInterpolationResult(true, SvgAnimationInterpolationError.None, value, 0, 0, 0);
    }
SvgAnimationInterpolationResult Failure(SvgAnimationInterpolationError error)
    {
        return new SvgAnimationInterpolationResult(false, error, null, 0, -1, 0);
    }
}
