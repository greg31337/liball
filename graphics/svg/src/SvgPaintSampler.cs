namespace svg;

internal class SvgNormalizedPaintStop
{
    internal double Offset;
    internal SvgColor Color;

    internal SvgNormalizedPaintStop(double offset, SvgColor color)
    {
        Offset = offset;
        Color = color;
    }
}

internal class SvgGradientSampleState
{
    SvgSceneGradientPaintData? gradient;
    SvgPoint? point;
    SvgPaintSampleOptions options;
    System.Collections.Generic.List<SvgNormalizedPaintStop> stops;

    internal SvgGradientSampleState(SvgSceneGradientPaintData? paint, SvgPoint? samplePoint, SvgPaintSampleOptions sampleOptions)
    {
        gradient = paint;
        point = samplePoint;
        options = sampleOptions;
        stops = new System.Collections.Generic.List<SvgNormalizedPaintStop>();
    }

    internal SvgPaintSampleResult Run()
    {
        if ((gradient ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind != SvgPaintServerKind.LinearGradient && gradient.Kind != SvgPaintServerKind.RadialGradient) return SvgPaintSampleMath.Failure(SvgPaintSampleError.InvalidArgument);
        if (gradient.SpreadMethod != SvgGradientSpreadMethod.Pad && gradient.SpreadMethod != SvgGradientSpreadMethod.Reflect && gradient.SpreadMethod != SvgGradientSpreadMethod.Repeat) return SvgPaintSampleMath.Failure(SvgPaintSampleError.InvalidArgument);
        if (gradient.InterpolationSpace != SvgColorSpace.Srgb && gradient.InterpolationSpace != SvgColorSpace.SrgbLinear) return SvgPaintSampleMath.Failure(SvgPaintSampleError.InvalidArgument);
        SvgPaintSampleError validation = NormalizeStops();
        if (validation != SvgPaintSampleError.None) return SvgPaintSampleMath.Failure(validation);
        if (stops.Count == 0) return SvgPaintSampleMath.NoPaint(0);
        if (stops.Count == 1) return Color(stops[0].Color, 0, 0, 0, 1);
        SvgMatrixOperationResult inverse = gradient.Transform.Invert();
        if (!inverse.Success)
        {
            SvgPaintSampleError error;
            if (inverse.Error == SvgGeometryError.SingularMatrix) error = SvgPaintSampleError.SingularTransform;
            else if (inverse.Error == SvgGeometryError.InvalidArgument) error = SvgPaintSampleError.InvalidArgument;
            else error = SvgPaintSampleError.NonFiniteResult;
            return SvgPaintSampleMath.Failure(error);
        }
        SvgPointTransformResult local = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint((point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!local.Success) return SvgPaintSampleMath.Failure(SvgPaintSampleError.NonFiniteResult);
        SvgPaintOffsetResult offset;
        if (gradient.Kind == SvgPaintServerKind.LinearGradient) offset = LinearOffset(local.Point);
        else if (gradient.Kind == SvgPaintServerKind.RadialGradient) offset = RadialOffset(local.Point);
        else return SvgPaintSampleMath.Failure(SvgPaintSampleError.InvalidArgument);
        if (!offset.Success) return SvgPaintSampleMath.Failure(offset.Error);
        if (offset.UseLastStop) return Color(stops[stops.Count - 1].Color, 1, stops.Count - 1, stops.Count - 1, stops.Count);
        if (offset.UseAverage) return Average();
        if (!offset.HasPaint) return SvgPaintSampleMath.NoPaint(stops.Count);
        double spread = SvgPaintSampleMath.Spread(offset.Offset, gradient.SpreadMethod);
        if (!SvgFiniteMath.IsFinite(spread)) return SvgPaintSampleMath.Failure(SvgPaintSampleError.NonFiniteResult);
        return Interpolate(spread);
    }

    SvgPaintSampleError NormalizeStops()
    {
        if ((gradient ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Stops == null) return SvgPaintSampleError.InvalidArgument;
        if (gradient.Stops.Count > options.MaximumStops) return SvgPaintSampleError.StopLimitExceeded;
        double previous = 0;
        int index = 0;
        while (index < gradient.Stops.Count)
        {
            SvgSceneGradientStopData source = gradient.Stops[index];
            if (source == null || source.Color == null || !SvgFiniteMath.IsFinite(source.Offset) || !SvgFiniteMath.IsFinite(source.Alpha) || source.Alpha < 0 || source.Alpha > 1) return SvgPaintSampleError.InvalidArgument;
            double offset = source.Offset;
            if (offset < 0) offset = 0;
            if (offset > 1) offset = 1;
            if (offset < previous) offset = previous;
            SvgColor? color = SvgPaintSampleMath.ColorWithAlpha(source.Color, source.Alpha);
            if (color == null) return SvgPaintSampleError.InvalidArgument;
            stops.Add(new SvgNormalizedPaintStop(offset, color));
            previous = offset;
            index = index + 1;
        }
        return SvgPaintSampleError.None;
    }

    SvgPaintOffsetResult LinearOffset(SvgPoint? local)
    {
        if (!SvgPaintSampleMath.ValidPoint((gradient ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).StartOrCenter) || !SvgPaintSampleMath.ValidPoint(gradient.EndOrFocus)) return SvgPaintOffsetResult.Failure(SvgPaintSampleError.InvalidArgument);
        double dx = gradient.EndOrFocus.X - gradient.StartOrCenter.X;
        double dy = gradient.EndOrFocus.Y - gradient.StartOrCenter.Y;
        if (!SvgFiniteMath.IsFinite(dx) || !SvgFiniteMath.IsFinite(dy)) return SvgPaintOffsetResult.Failure(SvgPaintSampleError.NonFiniteResult);
        if (dx == 0 && dy == 0) return SvgPaintOffsetResult.Last();
        double px = (local ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X - gradient.StartOrCenter.X;
        double py = local.Y - gradient.StartOrCenter.Y;
        double scale = SvgPaintSampleMath.Maximum(dx, dy, px, py, 0, 0);
        if (scale == 0) return SvgPaintOffsetResult.Last();
        dx = dx / scale;
        dy = dy / scale;
        px = px / scale;
        py = py / scale;
        double denominator = dx * dx + dy * dy;
        double offset = (px * dx + py * dy) / denominator;
        if (!SvgFiniteMath.IsFinite(offset)) return SvgPaintOffsetResult.Failure(SvgPaintSampleError.NonFiniteResult);
        return SvgPaintOffsetResult.Paint(offset);
    }

    SvgPaintOffsetResult RadialOffset(SvgPoint? local)
    {
        if (!SvgPaintSampleMath.ValidPoint((gradient ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).StartOrCenter) || !SvgPaintSampleMath.ValidPoint(gradient.EndOrFocus)) return SvgPaintOffsetResult.Failure(SvgPaintSampleError.InvalidArgument);
        if (!SvgFiniteMath.IsFinite(gradient.Radius) || !SvgFiniteMath.IsFinite(gradient.FocalRadius) || gradient.Radius < 0 || gradient.FocalRadius < 0) return SvgPaintOffsetResult.Failure(SvgPaintSampleError.InvalidArgument);
        if (gradient.Radius == 0) return SvgPaintOffsetResult.Last();
        double dx = gradient.StartOrCenter.X - gradient.EndOrFocus.X;
        double dy = gradient.StartOrCenter.Y - gradient.EndOrFocus.Y;
        double dr = gradient.Radius - gradient.FocalRadius;
        if (dx == 0 && dy == 0 && dr == 0) return SvgPaintOffsetResult.None();
        double qx = (local ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X - gradient.EndOrFocus.X;
        double qy = local.Y - gradient.EndOrFocus.Y;
        double scale = SvgPaintSampleMath.Maximum(dx, dy, qx, qy, gradient.Radius, gradient.FocalRadius);
        if (scale == 0) return SvgPaintOffsetResult.Last();
        dx = dx / scale;
        dy = dy / scale;
        qx = qx / scale;
        qy = qy / scale;
        double focalRadius = gradient.FocalRadius / scale;
        dr = dr / scale;
        double a = dx * dx + dy * dy - dr * dr;
        double b = -2 * (qx * dx + qy * dy + focalRadius * dr);
        double c = qx * qx + qy * qy - focalRadius * focalRadius;
        SvgPaintOffsetResult solved = SolveRadial(a, b, c, focalRadius, dr);
        if (solved.HasPaint || solved.Error != SvgPaintSampleError.None) return solved;
        if (gradient.SpreadMethod == SvgGradientSpreadMethod.Repeat && gradient.FocalRadius == 0 && SvgPaintSampleMath.NearlyEqual(dx * dx + dy * dy, dr * dr)) return SvgPaintOffsetResult.Average();
        return solved;
    }

    SvgPaintOffsetResult SolveRadial(double a, double b, double c, double focalRadius, double radiusDelta)
    {
        double epsilon = 0.00000000000001;
        if (System.Math.Abs(a) <= epsilon)
        {
            if (System.Math.Abs(b) <= epsilon)
            {
                if (System.Math.Abs(c) <= epsilon) return SvgPaintOffsetResult.Paint(0);
                return SvgPaintOffsetResult.None();
            }
            return ValidRoot(-c / b, focalRadius, radiusDelta);
        }
        double discriminant = b * b - 4 * a * c;
        if (!SvgFiniteMath.IsFinite(discriminant)) return SvgPaintOffsetResult.Failure(SvgPaintSampleError.NonFiniteResult);
        if (discriminant < -epsilon) return SvgPaintOffsetResult.None();
        if (discriminant < 0) discriminant = 0;
        double root = System.Math.Sqrt(discriminant);
        double first = (-b - root) / (2 * a);
        double second = (-b + root) / (2 * a);
        bool firstValid = SvgPaintSampleMath.ValidRadius(first, focalRadius, radiusDelta);
        bool secondValid = SvgPaintSampleMath.ValidRadius(second, focalRadius, radiusDelta);
        if (!firstValid && !secondValid) return SvgPaintOffsetResult.None();
        double selected = firstValid && secondValid ? System.Math.Max(first, second) : firstValid ? first : second;
        if (!SvgFiniteMath.IsFinite(selected)) return SvgPaintOffsetResult.Failure(SvgPaintSampleError.NonFiniteResult);
        return SvgPaintOffsetResult.Paint(selected);
    }

    SvgPaintOffsetResult ValidRoot(double root, double focalRadius, double radiusDelta)
    {
        if (!SvgFiniteMath.IsFinite(root)) return SvgPaintOffsetResult.Failure(SvgPaintSampleError.NonFiniteResult);
        if (!SvgPaintSampleMath.ValidRadius(root, focalRadius, radiusDelta)) return SvgPaintOffsetResult.None();
        return SvgPaintOffsetResult.Paint(root);
    }

    SvgPaintSampleResult Interpolate(double offset)
    {
        if (offset < stops[0].Offset) return Color(stops[0].Color, offset, 0, 0, stops.Count);
        int lower = 0;
        while (lower + 1 < stops.Count && stops[lower + 1].Offset <= offset) lower = lower + 1;
        if (lower == stops.Count - 1 || stops[lower].Offset == offset) return Color(stops[lower].Color, offset, lower, lower, stops.Count);
        int upper = lower + 1;
        double span = stops[upper].Offset - stops[lower].Offset;
        if (span <= 0) return Color(stops[upper].Color, offset, upper, upper, stops.Count);
        double amount = (offset - stops[lower].Offset) / span;
        SvgColorOperationResult interpolated = stops[lower].Color.Interpolate(stops[upper].Color, amount, (gradient ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).InterpolationSpace);
        if (!interpolated.Success) return SvgPaintSampleMath.Failure(SvgPaintSampleError.ColorConversionFailed);
        return Color(interpolated.Color, offset, lower, upper, stops.Count);
    }

    SvgPaintSampleResult Average()
    {
        SvgColorOperationResult firstResult = stops[0].Color.ConvertTo((gradient ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).InterpolationSpace);
        if (!firstResult.Success) return SvgPaintSampleMath.Failure(SvgPaintSampleError.ColorConversionFailed);
        SvgColor? first = firstResult.Color;
        double alpha = (first ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Alpha * stops[0].Offset;
        double red = first.Red * first.Alpha * stops[0].Offset;
        double green = first.Green * first.Alpha * stops[0].Offset;
        double blue = first.Blue * first.Alpha * stops[0].Offset;
        int index = 0;
        while (index + 1 < stops.Count)
        {
            SvgColorOperationResult leftResult = stops[index].Color.ConvertTo(gradient.InterpolationSpace);
            SvgColorOperationResult rightResult = stops[index + 1].Color.ConvertTo(gradient.InterpolationSpace);
            if (!leftResult.Success || !rightResult.Success) return SvgPaintSampleMath.Failure(SvgPaintSampleError.ColorConversionFailed);
            SvgColor? left = leftResult.Color;
            SvgColor? right = rightResult.Color;
            double width = stops[index + 1].Offset - stops[index].Offset;
            alpha = alpha + width * ((left ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Alpha + (right ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Alpha) / 2;
            red = red + width * (left.Red * left.Alpha + right.Red * right.Alpha) / 2;
            green = green + width * (left.Green * left.Alpha + right.Green * right.Alpha) / 2;
            blue = blue + width * (left.Blue * left.Alpha + right.Blue * right.Alpha) / 2;
            index = index + 1;
        }
        SvgColorOperationResult lastResult = stops[stops.Count - 1].Color.ConvertTo(gradient.InterpolationSpace);
        if (!lastResult.Success) return SvgPaintSampleMath.Failure(SvgPaintSampleError.ColorConversionFailed);
        SvgColor? last = lastResult.Color;
        double tail = 1 - stops[stops.Count - 1].Offset;
        alpha = alpha + (last ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Alpha * tail;
        red = red + last.Red * last.Alpha * tail;
        green = green + last.Green * last.Alpha * tail;
        blue = blue + last.Blue * last.Alpha * tail;
        if (alpha > 0)
        {
            red = red / alpha;
            green = green / alpha;
            blue = blue / alpha;
        }
        else
        {
            red = 0;
            green = 0;
            blue = 0;
        }
        if (!SvgFiniteMath.IsFinite(red) || !SvgFiniteMath.IsFinite(green) || !SvgFiniteMath.IsFinite(blue) || !SvgFiniteMath.IsFinite(alpha) || alpha < 0 || alpha > 1) return SvgPaintSampleMath.Failure(SvgPaintSampleError.NonFiniteResult);
        SvgColor average = new SvgColor(gradient.InterpolationSpace, red, green, blue, alpha);
        return Color(average, 0, 0, stops.Count - 1, stops.Count);
    }

    SvgPaintSampleResult Color(SvgColor? color, double offset, int lower, int upper, int count)
    {
        SvgPaintSampleResult result = SvgPaintSampleMath.Pixel(color, options);
        if (!result.Success) return result;
        result.Offset = offset;
        result.LowerStopIndex = lower;
        result.UpperStopIndex = upper;
        result.StopCount = count;
        return result;
    }
}

internal class SvgPaintOffsetResult
{
    internal bool Success;
    internal SvgPaintSampleError Error;
    internal bool HasPaint;
    internal bool UseLastStop;
    internal bool UseAverage;
    internal double Offset;

    internal SvgPaintOffsetResult(bool success, SvgPaintSampleError error, bool hasPaint, bool useLastStop, bool useAverage, double offset)
    {
        Success = success;
        Error = error;
        HasPaint = hasPaint;
        UseLastStop = useLastStop;
        UseAverage = useAverage;
        Offset = offset;
    }

    internal static SvgPaintOffsetResult Paint(double offset) { return new SvgPaintOffsetResult(true, SvgPaintSampleError.None, true, false, false, offset); }
    internal static SvgPaintOffsetResult None() { return new SvgPaintOffsetResult(true, SvgPaintSampleError.None, false, false, false, 0); }
    internal static SvgPaintOffsetResult Last() { return new SvgPaintOffsetResult(true, SvgPaintSampleError.None, true, true, false, 1); }
    internal static SvgPaintOffsetResult Average() { return new SvgPaintOffsetResult(true, SvgPaintSampleError.None, true, false, true, 0); }
    internal static SvgPaintOffsetResult Failure(SvgPaintSampleError error) { return new SvgPaintOffsetResult(false, error, false, false, false, 0); }
}

internal static class SvgPaintSampleMath
{
    internal static bool Valid(SvgSceneResource? resource, SvgPoint? point, SvgPaintSampleOptions? options)
    {
        if (resource == null || !resource.HasValidTag() || point == null || options == null) return false;
        if (!SvgFiniteMath.IsFinite(point.X) || !SvgFiniteMath.IsFinite(point.Y) || !SvgFiniteMath.IsFinite(options.Opacity) || options.Opacity < 0 || options.Opacity > 1 || options.MaximumStops < 1) return false;
        return options.OutputColorSpace == SvgPixelColorSpace.Srgb || options.OutputColorSpace == SvgPixelColorSpace.SrgbLinear;
    }

    internal static SvgPaintSampleResult Solid(SvgSceneSolidPaintData? solid, SvgPaintSampleOptions options)
    {
        if (solid == null || solid.Color == null || !SvgFiniteMath.IsFinite(solid.Opacity) || solid.Opacity < 0 || solid.Opacity > 1) return Failure(SvgPaintSampleError.InvalidArgument);
        SvgColor? color = ColorWithAlpha(solid.Color, solid.Opacity);
        if (color == null) return Failure(SvgPaintSampleError.InvalidArgument);
        return Pixel(color, options);
    }

    internal static SvgColor? ColorWithAlpha(SvgColor? source, double alpha)
    {
        if (source == null || !source.IsValid || source.IsCurrentColor || !SvgColorMath.IsKnownSpace(source.Space) || !SvgFiniteMath.IsFinite(source.Red) || !SvgFiniteMath.IsFinite(source.Green) || !SvgFiniteMath.IsFinite(source.Blue) || !SvgFiniteMath.IsFinite(source.Alpha) || source.Alpha < 0 || source.Alpha > 1 || !SvgFiniteMath.IsFinite(alpha) || alpha < 0 || alpha > 1) return null;
        return new SvgColor(source.Space, source.Red, source.Green, source.Blue, alpha);
    }

    internal static SvgPaintSampleResult Pixel(SvgColor? color, SvgPaintSampleOptions options)
    {
        SvgColorSpace output = options.OutputColorSpace == SvgPixelColorSpace.Srgb ? SvgColorSpace.Srgb : SvgColorSpace.SrgbLinear;
        SvgColorOperationResult converted = (color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ConvertTo(output);
        if (!converted.Success) return Failure(SvgPaintSampleError.ColorConversionFailed);
        double alpha = Clamp((converted.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Alpha * options.Opacity);
        double red = Clamp(converted.Color.Red) * alpha;
        double green = Clamp(converted.Color.Green) * alpha;
        double blue = Clamp(converted.Color.Blue) * alpha;
        SvgPaintSampleResult result = new SvgPaintSampleResult();
        result.Success = true;
        result.HasPaint = true;
        result.Pixel = new SvgPremultipliedRgba8(Byte(red), Byte(green), Byte(blue), Byte(alpha));
        return result;
    }

    internal static SvgPaintSampleResult Failure(SvgPaintSampleError error)
    {
        SvgPaintSampleResult result = new SvgPaintSampleResult();
        result.Error = error;
        return result;
    }

    internal static SvgPaintSampleResult NoPaint(int stopCount)
    {
        SvgPaintSampleResult result = new SvgPaintSampleResult();
        result.Success = true;
        result.StopCount = stopCount;
        return result;
    }

    internal static bool ValidPoint(SvgPoint point)
    {
        return point != null && SvgFiniteMath.IsFinite(point.X) && SvgFiniteMath.IsFinite(point.Y);
    }

    internal static double Spread(double offset, SvgGradientSpreadMethod spread)
    {
        if (spread == SvgGradientSpreadMethod.Pad) return Clamp(offset);
        if (spread == SvgGradientSpreadMethod.Repeat)
        {
            double repeated = offset - System.Math.Floor(offset);
            return repeated == 0 ? 0 : repeated;
        }
        if (spread == SvgGradientSpreadMethod.Reflect)
        {
            double repeated = offset % 2;
            if (repeated < 0) repeated = repeated + 2;
            return repeated <= 1 ? repeated : 2 - repeated;
        }
        return double.NaN;
    }

    internal static double Maximum(double first, double second, double third, double fourth, double fifth, double sixth)
    {
        double result = System.Math.Abs(first);
        result = System.Math.Max(result, System.Math.Abs(second));
        result = System.Math.Max(result, System.Math.Abs(third));
        result = System.Math.Max(result, System.Math.Abs(fourth));
        result = System.Math.Max(result, System.Math.Abs(fifth));
        return System.Math.Max(result, System.Math.Abs(sixth));
    }

    internal static bool ValidRadius(double offset, double focalRadius, double radiusDelta)
    {
        return SvgFiniteMath.IsFinite(offset) && focalRadius + offset * radiusDelta >= -0.00000000000001;
    }

    internal static bool NearlyEqual(double first, double second)
    {
        double scale = System.Math.Max(1, System.Math.Max(System.Math.Abs(first), System.Math.Abs(second)));
        return System.Math.Abs(first - second) <= scale * 0.00000000000001;
    }

    static double Clamp(double value)
    {
        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }

    static byte Byte(double value)
    {
        return (byte)System.Math.Floor(value * 255 + 0.5);
    }
}
