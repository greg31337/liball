namespace svg;

/// <summary>Parses and formats basic SVG numeric values without requiring a document.</summary>
public class SvgValueService : ISvgValueService
{
    /// <summary>Creates a stateless standalone value service.</summary>
    public SvgValueService()
    {
    }

    /// <summary>Parses one complete finite SVG number from a string.</summary>
    /// <param name="value">The authored value.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed number or exact failure.</returns>
    public SvgValueNumberResult ParseNumberWithString(string? value, SvgValueParseOptions options)
    {
        if (value == null) return new SvgValueNumberResult(false, SvgValueParseError.InvalidArgument, 0, 0, 0, 0, false);
        return new SvgValueService().ParseNumberCore(value, options);
    }

    /// <summary>Parses one complete finite SVG number from a character span.</summary>
    /// <param name="value">The authored value.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed number or exact failure.</returns>
    public SvgValueNumberResult ParseNumberWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueParseOptions options)
    {
        return new SvgValueService().ParseNumberCore(value.ToString(), options);
    }

    /// <summary>Parses one complete signed decimal integer from a string.</summary>
    /// <param name="value">The authored value.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed integer or exact failure.</returns>
    public SvgValueIntegerResult ParseIntegerWithString(string? value, SvgValueParseOptions options)
    {
        if (value == null) return new SvgValueIntegerResult(false, SvgValueParseError.InvalidArgument, 0, 0, 0, 0, false);
        return new SvgValueService().ParseIntegerCore(value, options);
    }

    /// <summary>Parses one complete signed decimal integer from a character span.</summary>
    /// <param name="value">The authored value.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed integer or exact failure.</returns>
    public SvgValueIntegerResult ParseIntegerWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueParseOptions options)
    {
        return new SvgValueService().ParseIntegerCore(value.ToString(), options);
    }

    /// <summary>Parses one complete finite SVG coordinate from a string.</summary>
    /// <param name="value">The authored coordinate.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed coordinate or exact failure.</returns>
    public SvgValueNumberResult ParseCoordinateWithString(string? value, SvgValueParseOptions options)
    {
        if (value == null) return new SvgValueNumberResult(false, SvgValueParseError.InvalidArgument, 0, 0, 0, 0, false);
        return new SvgValueService().ParseNumberCore(value, options);
    }

    /// <summary>Parses one complete finite SVG coordinate from a character span.</summary>
    /// <param name="value">The authored coordinate.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed coordinate or exact failure.</returns>
    public SvgValueNumberResult ParseCoordinateWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueParseOptions options)
    {
        return new SvgValueService().ParseNumberCore(value.ToString(), options);
    }

    /// <summary>Parses one required number and one optional number from a string.</summary>
    /// <param name="value">The authored pair.</param><param name="options">The bounds and recovery behavior.</param><returns>The pair or exact failure.</returns>
    public SvgValueOptionalPairResult ParseOptionalPairWithString(string? value, SvgValueParseOptions options)
    {
        if (value == null) return new SvgValueOptionalPairResult(false, SvgValueParseError.InvalidArgument, 0, 0, false, 0, false);
        return new SvgValueService().ParseOptionalPairCore(value, options);
    }

    /// <summary>Parses one required number and one optional number from a character span.</summary>
    /// <param name="value">The authored pair.</param><param name="options">The bounds and recovery behavior.</param><returns>The pair or exact failure.</returns>
    public SvgValueOptionalPairResult ParseOptionalPairWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueParseOptions options)
    {
        return new SvgValueService().ParseOptionalPairCore(value.ToString(), options);
    }

    /// <summary>Parses a bounded finite-number list from a string.</summary>
    /// <param name="value">The authored list.</param><param name="separator">The separator grammar.</param><param name="options">The bounds and recovery behavior.</param><returns>The values or exact failure.</returns>
    public SvgValueNumberListResult ParseNumberListWithString(string? value, SvgValueListSeparator separator, SvgValueParseOptions options)
    {
        if (value == null) return new SvgValueNumberListResult(false, SvgValueParseError.InvalidArgument, new double[0], 0, 0, 0, false);
        return new SvgValueService().ParseNumberListCore(value, separator, options);
    }

    /// <summary>Parses a bounded finite-number list from a character span.</summary>
    /// <param name="value">The authored list.</param><param name="separator">The separator grammar.</param><param name="options">The bounds and recovery behavior.</param><returns>The values or exact failure.</returns>
    public SvgValueNumberListResult ParseNumberListWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueListSeparator separator, SvgValueParseOptions options)
    {
        return new SvgValueService().ParseNumberListCore(value.ToString(), separator, options);
    }

    /// <summary>Parses a bounded signed integer list from a string.</summary>
    /// <param name="value">The authored list.</param><param name="separator">The separator grammar.</param><param name="options">The bounds and recovery behavior.</param><returns>The values or exact failure.</returns>
    public SvgValueIntegerListResult ParseIntegerListWithString(string? value, SvgValueListSeparator separator, SvgValueParseOptions options)
    {
        if (value == null) return new SvgValueIntegerListResult(false, SvgValueParseError.InvalidArgument, new int[0], 0, 0, 0, false);
        return new SvgValueService().ParseIntegerListCore(value, separator, options);
    }

    /// <summary>Parses a bounded signed integer list from a character span.</summary>
    /// <param name="value">The authored list.</param><param name="separator">The separator grammar.</param><param name="options">The bounds and recovery behavior.</param><returns>The values or exact failure.</returns>
    public SvgValueIntegerListResult ParseIntegerListWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueListSeparator separator, SvgValueParseOptions options)
    {
        return new SvgValueService().ParseIntegerListCore(value.ToString(), separator, options);
    }

    /// <summary>Parses a bounded SVG points list from a string.</summary>
    /// <param name="value">The authored points.</param><param name="options">The point bound and recovery behavior.</param><returns>The points or exact failure.</returns>
    public SvgValuePointListResult ParsePointListWithString(string? value, SvgValueParseOptions options)
    {
        if (value == null) return new SvgValuePointListResult(false, SvgValueParseError.InvalidArgument, new System.Collections.Generic.List<SvgPoint>(), 0, false);
        return new SvgValueService().ParsePointListCore(value, options);
    }

    /// <summary>Parses a bounded SVG points list from a character span.</summary>
    /// <param name="value">The authored points.</param><param name="options">The point bound and recovery behavior.</param><returns>The points or exact failure.</returns>
    public SvgValuePointListResult ParsePointListWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueParseOptions options)
    {
        return new SvgValueService().ParsePointListCore(value.ToString(), options);
    }

    /// <summary>Formats one finite number.</summary>
    /// <param name="value">The finite number.</param><param name="mode">The deterministic output mode.</param><returns>The formatted text or stable failure.</returns>
    public SvgValueFormatResult FormatNumber(double value, SvgStandaloneValueFormatMode mode)
    {
        return new SvgValueService().FormatNumberCore(value, mode);
    }

    /// <summary>Formats one finite number atomically into a caller destination.</summary>
    /// <param name="value">The finite number.</param><param name="mode">The deterministic output mode.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    public SvgValueFormatResult FormatNumberToSpan(double value, SvgStandaloneValueFormatMode mode, System.Span<char> destination)
    {
        SvgValueFormatResult formatted = new SvgValueService().FormatNumberCore(value, mode);
        return new SvgValueService().CopyCore(formatted, destination);
    }

    /// <summary>Formats one finite coordinate.</summary>
    /// <param name="value">The finite coordinate.</param><param name="mode">The deterministic output mode.</param><returns>The formatted text or stable failure.</returns>
    public SvgValueFormatResult FormatCoordinate(double value, SvgStandaloneValueFormatMode mode)
    {
        return new SvgValueService().FormatNumberCore(value, mode);
    }

    /// <summary>Formats one finite coordinate atomically into a caller destination.</summary>
    /// <param name="value">The finite coordinate.</param><param name="mode">The deterministic output mode.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    public SvgValueFormatResult FormatCoordinateToSpan(double value, SvgStandaloneValueFormatMode mode, System.Span<char> destination)
    {
        return new SvgValueService().CopyCore(new SvgValueService().FormatNumberCore(value, mode), destination);
    }

    /// <summary>Formats one signed integer.</summary>
    /// <param name="value">The integer.</param><returns>The invariant decimal text.</returns>
    public SvgValueFormatResult FormatInteger(int value)
    {
        string text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new SvgValueFormatResult(true, SvgValueFormatError.None, text, text.Length, 0);
    }

    /// <summary>Formats one signed integer atomically into a caller destination.</summary>
    /// <param name="value">The integer.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    public SvgValueFormatResult FormatIntegerToSpan(int value, System.Span<char> destination)
    {
        string text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new SvgValueService().CopyCore(new SvgValueService().FormatSuccess(text), destination);
    }

    /// <summary>Formats a finite-number list.</summary>
    /// <param name="values">The finite numbers.</param><param name="separator">The output separator.</param><param name="mode">The deterministic output mode.</param><returns>The formatted text or stable failure.</returns>
    public SvgValueFormatResult FormatNumberList(double[]? values, SvgValueListSeparator separator, SvgStandaloneValueFormatMode mode)
    {
        if (values == null || !new SvgValueService().ValidSeparator(separator)) return new SvgValueService().FormatFailure(SvgValueFormatError.InvalidArgument);
        string delimiter = new SvgValueService().Delimiter(separator, mode);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            SvgValueFormatResult item = new SvgValueService().FormatNumberCore(values[index], mode);
            if (!item.Success) return item;
            if (index > 0) builder.Append(delimiter);
            builder.Append(item.Text);
            index = index + 1;
        }
        return new SvgValueService().FormatSuccess(builder.ToString());
    }

    /// <summary>Formats a finite-number list atomically into a caller destination.</summary>
    /// <param name="values">The finite numbers.</param><param name="separator">The output separator.</param><param name="mode">The deterministic output mode.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    public SvgValueFormatResult FormatNumberListToSpan(double[]? values, SvgValueListSeparator separator, SvgStandaloneValueFormatMode mode, System.Span<char> destination)
    {
        SvgValueFormatResult formatted = new SvgValueService().FormatNumberList(values, separator, mode);
        return new SvgValueService().CopyCore(formatted, destination);
    }

    /// <summary>Formats a signed integer list.</summary>
    /// <param name="values">The signed integers.</param><param name="separator">The output separator.</param><returns>The formatted text or stable failure.</returns>
    public SvgValueFormatResult FormatIntegerList(int[]? values, SvgValueListSeparator separator)
    {
        if (values == null || !new SvgValueService().ValidSeparator(separator)) return new SvgValueService().FormatFailure(SvgValueFormatError.InvalidArgument);
        string delimiter = new SvgValueService().Delimiter(separator, SvgStandaloneValueFormatMode.Canonical);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0) builder.Append(delimiter);
            builder.Append(values[index].ToString(System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }
        return new SvgValueService().FormatSuccess(builder.ToString());
    }

    /// <summary>Formats a signed integer list atomically into a caller destination.</summary>
    /// <param name="values">The signed integers.</param><param name="separator">The output separator.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    public SvgValueFormatResult FormatIntegerListToSpan(int[]? values, SvgValueListSeparator separator, System.Span<char> destination)
    {
        SvgValueFormatResult formatted = new SvgValueService().FormatIntegerList(values, separator);
        return new SvgValueService().CopyCore(formatted, destination);
    }

    /// <summary>Formats a point list.</summary>
    /// <param name="points">The finite points.</param><param name="mode">The deterministic output mode.</param><returns>The formatted text or stable failure.</returns>
    public SvgValueFormatResult FormatPointList(System.Collections.Generic.List<SvgPoint>? points, SvgStandaloneValueFormatMode mode)
    {
        if (points == null || !new SvgValueService().ValidFormatMode(mode)) return new SvgValueService().FormatFailure(SvgValueFormatError.InvalidArgument);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < points.Count)
        {
            SvgPoint? point = points[index];
            if (point == null) return new SvgValueService().FormatFailure(SvgValueFormatError.InvalidArgument);
            SvgValueFormatResult x = new SvgValueService().FormatNumberCore(point.X, mode);
            SvgValueFormatResult y = new SvgValueService().FormatNumberCore(point.Y, mode);
            if (!x.Success || !y.Success) return new SvgValueService().FormatFailure(SvgValueFormatError.NonFinite);
            if (index > 0) builder.Append(mode == SvgStandaloneValueFormatMode.Compact ? ',' : ' ');
            builder.Append(x.Text);
            builder.Append(',');
            builder.Append(y.Text);
            index = index + 1;
        }
        return new SvgValueService().FormatSuccess(builder.ToString());
    }

    /// <summary>Formats a point list atomically into a caller destination.</summary>
    /// <param name="points">The finite points.</param><param name="mode">The deterministic output mode.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    public SvgValueFormatResult FormatPointListToSpan(System.Collections.Generic.List<SvgPoint>? points, SvgStandaloneValueFormatMode mode, System.Span<char> destination)
    {
        SvgValueFormatResult formatted = new SvgValueService().FormatPointList(points, mode);
        return new SvgValueService().CopyCore(formatted, destination);
    }

    /// <summary>Formats an optional pair.</summary>
    /// <param name="first">The finite first value.</param><param name="second">The finite optional value.</param><param name="hasSecond">Whether to emit the second value.</param><param name="mode">The deterministic output mode.</param><returns>The formatted text or stable failure.</returns>
    public SvgValueFormatResult FormatOptionalPair(double first, double second, bool hasSecond, SvgStandaloneValueFormatMode mode)
    {
        SvgValueFormatResult firstResult = new SvgValueService().FormatNumberCore(first, mode);
        if (!firstResult.Success) return firstResult;
        if (!hasSecond) return firstResult;
        SvgValueFormatResult secondResult = new SvgValueService().FormatNumberCore(second, mode);
        if (!secondResult.Success) return secondResult;
        return new SvgValueService().FormatSuccess(firstResult.Text + (mode == SvgStandaloneValueFormatMode.Compact ? "," : " ") + secondResult.Text);
    }

    /// <summary>Formats an optional pair atomically into a caller destination.</summary>
    /// <param name="first">The finite first value.</param><param name="second">The finite optional value.</param><param name="hasSecond">Whether to emit the second value.</param><param name="mode">The deterministic output mode.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    public SvgValueFormatResult FormatOptionalPairToSpan(double first, double second, bool hasSecond, SvgStandaloneValueFormatMode mode, System.Span<char> destination)
    {
        SvgValueFormatResult formatted = new SvgValueService().FormatOptionalPair(first, second, hasSecond, mode);
        return new SvgValueService().CopyCore(formatted, destination);
    }

    /// <summary>Parses any extended standalone SVG or CSS value from a string.</summary>
    /// <param name="kind">The requested grammar.</param><param name="value">The authored text.</param><param name="options">The grammar flags and bounds.</param><returns>The typed value or exact failure.</returns>
    public SvgExtendedValueParseResult ParseExtendedWithString(SvgExtendedValueKind kind, string? value, SvgExtendedValueParseOptions options)
    {
        if (value == null) return new SvgValueService().ExtendedFailure(SvgExtendedValueError.InvalidArgument, 0);
        return new SvgValueService().ParseExtendedCore(kind, value, options);
    }

    /// <summary>Parses any extended standalone SVG or CSS value from a character span.</summary>
    /// <param name="kind">The requested grammar.</param><param name="value">The authored text.</param><param name="options">The grammar flags and bounds.</param><returns>The typed value or exact failure.</returns>
    public SvgExtendedValueParseResult ParseExtendedWithReadOnlySpanOfChar(SvgExtendedValueKind kind, System.ReadOnlySpan<char> value, SvgExtendedValueParseOptions options)
    {
        return new SvgValueService().ParseExtendedCore(kind, value.ToString(), options);
    }

    /// <summary>Evaluates a typed length or percentage to SVG user units.</summary>
    /// <param name="length">The typed length.</param><param name="context">The viewport, font, and object-bounds context.</param><param name="basis">The percentage basis.</param><returns>The finite user-unit result.</returns>
    public SvgLengthEvaluationResult EvaluateLength(SvgLength length, SvgLengthContext context, SvgLengthPercentageBasis basis)
    {
        return SvgLengthEvaluator.Evaluate(length, context, basis);
    }

    /// <summary>Converts an angle to another angle unit.</summary>
    /// <param name="angle">The source angle.</param><param name="targetUnit">The target unit.</param><returns>The converted numeric component.</returns>
    public SvgUnitConversionResult ConvertAngle(SvgAngle angle, SvgAngleUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertAngle(angle, targetUnit);
    }

    /// <summary>Converts a time to another time unit.</summary>
    /// <param name="time">The source time.</param><param name="targetUnit">The target unit.</param><returns>The converted numeric component.</returns>
    public SvgUnitConversionResult ConvertTime(SvgTime time, SvgTimeUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertTime(time, targetUnit);
    }

    /// <summary>Converts a frequency to another frequency unit.</summary>
    /// <param name="frequency">The source frequency.</param><param name="targetUnit">The target unit.</param><returns>The converted numeric component.</returns>
    public SvgUnitConversionResult ConvertFrequency(SvgFrequency frequency, SvgFrequencyUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertFrequency(frequency, targetUnit);
    }

    /// <summary>Converts a resolution to another resolution unit.</summary>
    /// <param name="resolution">The source resolution.</param><param name="targetUnit">The target unit.</param><returns>The converted numeric component.</returns>
    public SvgUnitConversionResult ConvertResolution(SvgResolution resolution, SvgResolutionUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertResolution(resolution, targetUnit);
    }

    /// <summary>Evaluates a positive ratio.</summary>
    /// <param name="ratio">The positive ratio.</param><returns>The numerator divided by the denominator.</returns>
    public SvgUnitConversionResult EvaluateRatio(SvgRatio ratio)
    {
        return ratio == null ? SvgScalarUnitConverter.Failure(SvgUnitConversionError.InvalidArgument) : ratio.ToValue();
    }

    /// <summary>Computes measured length divided by author pathLength.</summary>
    /// <param name="pathLength">The author pathLength.</param><param name="actualLength">The measured nonnegative length.</param><returns>The finite scale or stable failure.</returns>
    public SvgUnitConversionResult EvaluatePathLengthScale(SvgPathLength pathLength, double actualLength)
    {
        return pathLength == null ? SvgScalarUnitConverter.Failure(SvgUnitConversionError.InvalidArgument) : pathLength.GetScale(actualLength);
    }

    /// <summary>Evaluates a typed CSS math tree in canonical units.</summary>
    /// <param name="expression">The typed CSS math tree.</param><param name="context">The viewport, font, and object-bounds context.</param><param name="basis">The percentage basis.</param><returns>The finite canonical value.</returns>
    public SvgCalcEvaluationResult EvaluateCalc(SvgCalcExpression expression, SvgLengthContext context, SvgLengthPercentageBasis basis)
    {
        if (expression == null || context == null) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, SvgCalcDimension.Any, 0);
        return new SvgValueService().EvaluateCalcCore(expression, context, basis);
    }

    /// <summary>Composes an SVG transform list into one matrix.</summary>
    /// <param name="transforms">The SVG transform list.</param><returns>The finite matrix or stable failure.</returns>
    public SvgTransformMatrixResult ComposeSvgTransform(SvgTransformList transforms)
    {
        return SvgTransformMatrixBuilder.Compose(transforms);
    }

    /// <summary>Composes a CSS transform list around a resolved transform origin.</summary>
    /// <param name="transforms">The CSS transform list.</param><param name="context">The length and reference-box context.</param><param name="origin">The transform origin.</param><param name="box">The selected transform box.</param><returns>The finite matrix or stable failure.</returns>
    public SvgCssTransformMatrixResult ComposeCssTransform(SvgCssTransformList transforms, SvgCssTransformContext context, SvgTransformOrigin origin, SvgTransformBox box)
    {
        return SvgCssTransformMatrixBuilder.Compose(transforms, context, origin, box);
    }

    /// <summary>Interpolates two evaluated CSS transform matrices.</summary>
    /// <param name="from">The starting CSS transform list.</param><param name="to">The ending CSS transform list.</param><param name="amount">The unit-interval interpolation amount.</param><param name="context">The shared length and reference-box context.</param><param name="origin">The shared transform origin.</param><param name="box">The selected transform box.</param><returns>The interpolated finite matrix.</returns>
    public SvgMatrixOperationResult InterpolateCssTransform(SvgCssTransformList from, SvgCssTransformList to, double amount, SvgCssTransformContext context, SvgTransformOrigin origin, SvgTransformBox box)
    {
        if (!SvgFiniteMath.IsFinite(amount) || amount < 0 || amount > 1) return new SvgMatrixOperationResult(false, SvgGeometryError.InvalidArgument, null);
        SvgCssTransformMatrixResult first = SvgCssTransformMatrixBuilder.Compose(from, context, origin, box);
        SvgCssTransformMatrixResult second = SvgCssTransformMatrixBuilder.Compose(to, context, origin, box);
        if (!first.Success || !second.Success || first.Matrix == null || second.Matrix == null) return new SvgMatrixOperationResult(false, SvgGeometryError.InvalidArgument, null);
        return SvgMatrixMath.Interpolate(first.Matrix, second.Matrix, amount);
    }

    /// <summary>Fits a viewBox into a viewport using preserveAspectRatio.</summary>
    /// <param name="viewBox">The source viewBox.</param><param name="aspectRatio">The fitting policy.</param><param name="viewportX">The viewport x coordinate.</param><param name="viewportY">The viewport y coordinate.</param><param name="viewportWidth">The viewport width.</param><param name="viewportHeight">The viewport height.</param><returns>The fitting matrix.</returns>
    public SvgViewBoxFitResult FitViewBox(SvgViewBox viewBox, SvgAspectRatio aspectRatio, double viewportX, double viewportY, double viewportWidth, double viewportHeight)
    {
        return SvgViewBoxFitter.Fit(viewBox, aspectRatio, viewportX, viewportY, viewportWidth, viewportHeight);
    }

    /// <summary>Converts a color to another supported color space.</summary>
    /// <param name="color">The source color.</param><param name="targetSpace">The target color space.</param><returns>The converted color.</returns>
    public SvgColorOperationResult ConvertColor(SvgColor color, SvgColorSpace targetSpace)
    {
        return SvgColorMath.Convert(color, targetSpace);
    }

    /// <summary>Interpolates two colors in a selected color space.</summary>
    /// <param name="from">The starting color.</param><param name="to">The ending color.</param><param name="amount">The finite interpolation amount.</param><param name="space">The interpolation color space.</param><returns>The interpolated color.</returns>
    public SvgColorOperationResult InterpolateColor(SvgColor from, SvgColor to, double amount, SvgColorSpace space)
    {
        return SvgColorMath.Interpolate(from, to, amount, space);
    }

    /// <summary>Evaluates and interpolates compatible CSS math trees in canonical units.</summary>
    /// <param name="from">The starting expression.</param><param name="to">The ending expression.</param><param name="amount">The unit-interval interpolation amount.</param><param name="context">The shared length context.</param><param name="basis">The shared percentage basis.</param><returns>The finite interpolated canonical value.</returns>
    public SvgCalcEvaluationResult InterpolateCalc(SvgCalcExpression from, SvgCalcExpression to, double amount, SvgLengthContext context, SvgLengthPercentageBasis basis)
    {
        if (from == null || to == null || context == null || !SvgFiniteMath.IsFinite(amount) || amount < 0 || amount > 1) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, SvgCalcDimension.Any, 0);
        SvgCalcEvaluationResult first = new SvgValueService().EvaluateCalcCore(from, context, basis); if (!first.Success) return first;
        SvgCalcEvaluationResult second = new SvgValueService().EvaluateCalcCore(to, context, basis); if (!second.Success) return second;
        if (!new SvgValueService().CompatibleCalcDimensions(first.Dimension, second.Dimension)) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.IncompatibleDimensions, SvgCalcDimension.Any, 0);
        SvgCalcDimension dimension = first.Dimension == second.Dimension ? first.Dimension : SvgCalcDimension.LengthPercentage;
        return new SvgValueService().CalcFinite(dimension, new SvgValueService().Lerp(first.Value, second.Value, amount));
    }

    /// <summary>Interpolates compatible extended typed values.</summary>
    /// <param name="from">The starting typed value.</param><param name="to">The ending typed value.</param><param name="amount">The unit-interval interpolation amount.</param><param name="options">The length, color, and bound options.</param><returns>The interpolated typed value.</returns>
    public SvgExtendedValueOperationResult InterpolateExtended(SvgExtendedValue from, SvgExtendedValue to, double amount, SvgAnimationInterpolationOptions options)
    {
        if (from == null || to == null || options == null || from.Kind != to.Kind || !SvgFiniteMath.IsFinite(amount) || amount < 0 || amount > 1) return new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.InvalidArgument, null);
        return new SvgValueService().InterpolateExtendedCore(from, to, amount, options);
    }

    /// <summary>Serializes an extended typed value canonically.</summary>
    /// <param name="value">The typed value.</param><returns>The canonical text or stable failure.</returns>
    public SvgValueFormatResult SerializeExtended(SvgExtendedValue value)
    {
        if (value == null) return new SvgValueService().FormatFailure(SvgValueFormatError.InvalidArgument);
        string? text = new SvgValueService().SerializeExtendedCore(value);
        return text == null ? new SvgValueService().FormatFailure(SvgValueFormatError.InvalidArgument) : new SvgValueService().FormatSuccess(text);
    }

    /// <summary>Serializes an extended typed value atomically into a caller destination.</summary>
    /// <param name="value">The typed value.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    public SvgValueFormatResult SerializeExtendedToSpan(SvgExtendedValue value, System.Span<char> destination)
    {
        SvgValueFormatResult formatted = new SvgValueService().SerializeExtended(value);
        return new SvgValueService().CopyCore(formatted, destination);
    }

    SvgValueNumberResult ParseNumberCore(string value, SvgValueParseOptions options)
    {
        if (!new SvgValueService().ValidOptions(options)) return new SvgValueNumberResult(false, SvgValueParseError.InvalidArgument, 0, 0, 0, 0, false);
        SvgSpanReader reader = new SvgSpanReader(value, options.MaxTokenCharacters);
        reader.SkipWhitespace();
        SvgNumberParseResult parsed = SvgNumberParser.ReadNumber(reader);
        if (!parsed.Success) return new SvgValueNumberResult(false, new SvgValueService().MapNumberError(parsed.Error), 0, parsed.Start, parsed.Start, parsed.ErrorOffset, false);
        int next = reader.GetPosition();
        reader.SkipWhitespace();
        if (reader.IsEnd()) return new SvgValueNumberResult(true, SvgValueParseError.None, parsed.Value, parsed.Start, reader.GetPosition(), -1, false);
        bool recover = options.RecoveryMode == SvgValueRecoveryMode.KeepValidPrefix;
        return new SvgValueNumberResult(false, SvgValueParseError.TrailingCharacters, recover ? parsed.Value : 0, parsed.Start, recover ? next : parsed.Start, reader.GetPosition(), recover);
    }

    SvgValueIntegerResult ParseIntegerCore(string value, SvgValueParseOptions options)
    {
        if (!new SvgValueService().ValidOptions(options)) return new SvgValueIntegerResult(false, SvgValueParseError.InvalidArgument, 0, 0, 0, 0, false);
        SvgSpanReader reader = new SvgSpanReader(value, options.MaxTokenCharacters);
        reader.SkipWhitespace();
        SvgIntegerParseResult parsed = SvgNumberParser.ReadInteger(reader);
        if (!parsed.Success) return new SvgValueIntegerResult(false, new SvgValueService().MapNumberError(parsed.Error), 0, parsed.Start, parsed.Start, parsed.ErrorOffset, false);
        int next = reader.GetPosition();
        reader.SkipWhitespace();
        if (reader.IsEnd()) return new SvgValueIntegerResult(true, SvgValueParseError.None, parsed.Value, parsed.Start, reader.GetPosition(), -1, false);
        bool recover = options.RecoveryMode == SvgValueRecoveryMode.KeepValidPrefix;
        return new SvgValueIntegerResult(false, SvgValueParseError.TrailingCharacters, recover ? parsed.Value : 0, parsed.Start, recover ? next : parsed.Start, reader.GetPosition(), recover);
    }

    SvgValueOptionalPairResult ParseOptionalPairCore(string value, SvgValueParseOptions options)
    {
        if (!new SvgValueService().ValidOptions(options)) return new SvgValueOptionalPairResult(false, SvgValueParseError.InvalidArgument, 0, 0, false, 0, false);
        SvgNumberListResult parsed = SvgListParser.ParseNumberList(value, SvgListSeparatorMode.CommaOrWhitespace, 2, options.MaxTokenCharacters, false);
        bool recover = !parsed.Success && options.RecoveryMode == SvgValueRecoveryMode.KeepValidPrefix && parsed.Values.Length > 0;
        if (!parsed.Success && !recover) return new SvgValueOptionalPairResult(false, new SvgValueService().MapListError(parsed.Error, parsed.NumberError), 0, 0, false, parsed.ErrorOffset, false);
        double first = parsed.Values.Length > 0 ? parsed.Values[0] : 0;
        double second = parsed.Values.Length > 1 ? parsed.Values[1] : 0;
        return new SvgValueOptionalPairResult(parsed.Success, new SvgValueService().MapListError(parsed.Error, parsed.NumberError), first, second, parsed.Values.Length > 1, parsed.ErrorOffset, recover);
    }

    SvgValueNumberListResult ParseNumberListCore(string value, SvgValueListSeparator separator, SvgValueParseOptions options)
    {
        if (!new SvgValueService().ValidOptions(options) || !new SvgValueService().ValidSeparator(separator)) return new SvgValueNumberListResult(false, SvgValueParseError.InvalidArgument, new double[0], 0, 0, 0, false);
        SvgNumberListResult parsed = SvgListParser.ParseNumberList(value, new SvgValueService().InternalSeparator(separator), options.MaxItems, options.MaxTokenCharacters, options.AllowEmpty);
        bool recover = !parsed.Success && options.RecoveryMode == SvgValueRecoveryMode.KeepValidPrefix && parsed.Values.Length > 0;
        double[] values = parsed.Success || recover ? parsed.Values : new double[0];
        return new SvgValueNumberListResult(parsed.Success, new SvgValueService().MapListError(parsed.Error, parsed.NumberError), values, parsed.Start, parsed.NextOffset, parsed.ErrorOffset, recover);
    }

    SvgValueIntegerListResult ParseIntegerListCore(string value, SvgValueListSeparator separator, SvgValueParseOptions options)
    {
        if (!new SvgValueService().ValidOptions(options) || !new SvgValueService().ValidSeparator(separator)) return new SvgValueIntegerListResult(false, SvgValueParseError.InvalidArgument, new int[0], 0, 0, 0, false);
        SvgIntegerListResult parsed = SvgListParser.ParseIntegerList(value, new SvgValueService().InternalSeparator(separator), options.MaxItems, options.MaxTokenCharacters, options.AllowEmpty);
        bool recover = !parsed.Success && options.RecoveryMode == SvgValueRecoveryMode.KeepValidPrefix && parsed.Values.Length > 0;
        int[] values = parsed.Success || recover ? parsed.Values : new int[0];
        return new SvgValueIntegerListResult(parsed.Success, new SvgValueService().MapListError(parsed.Error, parsed.NumberError), values, parsed.Start, parsed.NextOffset, parsed.ErrorOffset, recover);
    }

    SvgValuePointListResult ParsePointListCore(string value, SvgValueParseOptions options)
    {
        if (!new SvgValueService().ValidOptions(options)) return new SvgValuePointListResult(false, SvgValueParseError.InvalidArgument, new System.Collections.Generic.List<SvgPoint>(), 0, false);
        int maximumCoordinates = options.MaxItems > 1073741823 ? 2147483647 : options.MaxItems * 2;
        SvgNumberListResult parsed = SvgListParser.ParseNumberList(value, SvgListSeparatorMode.CommaOrWhitespace, maximumCoordinates, options.MaxTokenCharacters, options.AllowEmpty);
        SvgValueParseError error = new SvgValueService().MapListError(parsed.Error, parsed.NumberError);
        int errorOffset = parsed.ErrorOffset;
        bool success = parsed.Success;
        if (success && parsed.Values.Length % 2 != 0)
        {
            success = false;
            error = SvgValueParseError.OddCoordinateCount;
            errorOffset = parsed.NextOffset;
        }
        bool recover = !success && options.RecoveryMode == SvgValueRecoveryMode.KeepValidPrefix && parsed.Values.Length >= 2;
        System.Collections.Generic.List<SvgPoint> points = new System.Collections.Generic.List<SvgPoint>();
        if (success || recover)
        {
            int index = 0;
            while (index + 1 < parsed.Values.Length)
            {
                points.Add(new SvgPoint(parsed.Values[index], parsed.Values[index + 1]));
                index = index + 2;
            }
        }
        return new SvgValuePointListResult(success, error, points, errorOffset, recover);
    }

    SvgValueFormatResult FormatNumberCore(double value, SvgStandaloneValueFormatMode mode)
    {
        if (!new SvgValueService().ValidFormatMode(mode)) return new SvgValueService().FormatFailure(SvgValueFormatError.InvalidArgument);
        SvgNumberFormatResult formatted;
        if (mode == SvgStandaloneValueFormatMode.ShortestRoundTrip) formatted = SvgNumberFormatter.FormatShortest(value);
        else if (mode == SvgStandaloneValueFormatMode.Canonical) formatted = SvgNumberFormatter.FormatCanonical(value);
        else formatted = SvgNumberFormatter.FormatCompact(value);
        if (!formatted.Success) return new SvgValueService().FormatFailure(SvgValueFormatError.NonFinite);
        return new SvgValueService().FormatSuccess(formatted.Text);
    }

    SvgValueFormatResult CopyCore(SvgValueFormatResult formatted, System.Span<char> destination)
    {
        if (!formatted.Success) return formatted;
        if (destination.Length < formatted.Text.Length) return new SvgValueFormatResult(false, SvgValueFormatError.DestinationTooSmall, formatted.Text, formatted.Text.Length, 0);
        int index = 0;
        while (index < formatted.Text.Length)
        {
            destination[index] = formatted.Text[index];
            index = index + 1;
        }
        return new SvgValueFormatResult(true, SvgValueFormatError.None, formatted.Text, formatted.Text.Length, formatted.Text.Length);
    }

    bool ValidOptions(SvgValueParseOptions options)
    {
        return options != null && options.MaxItems > 0 && options.MaxTokenCharacters > 0 && (options.RecoveryMode == SvgValueRecoveryMode.Strict || options.RecoveryMode == SvgValueRecoveryMode.KeepValidPrefix);
    }

    bool ValidSeparator(SvgValueListSeparator separator)
    {
        return separator == SvgValueListSeparator.CommaOrWhitespace || separator == SvgValueListSeparator.Comma || separator == SvgValueListSeparator.Whitespace || separator == SvgValueListSeparator.Semicolon;
    }

    bool ValidFormatMode(SvgStandaloneValueFormatMode mode)
    {
        return mode == SvgStandaloneValueFormatMode.ShortestRoundTrip || mode == SvgStandaloneValueFormatMode.Canonical || mode == SvgStandaloneValueFormatMode.Compact;
    }

    SvgListSeparatorMode InternalSeparator(SvgValueListSeparator separator)
    {
        if (separator == SvgValueListSeparator.Comma) return SvgListSeparatorMode.Comma;
        if (separator == SvgValueListSeparator.Whitespace) return SvgListSeparatorMode.Whitespace;
        if (separator == SvgValueListSeparator.Semicolon) return SvgListSeparatorMode.Semicolon;
        return SvgListSeparatorMode.CommaOrWhitespace;
    }

    string Delimiter(SvgValueListSeparator separator, SvgStandaloneValueFormatMode mode)
    {
        if (separator == SvgValueListSeparator.Whitespace) return " ";
        if (separator == SvgValueListSeparator.Semicolon) return mode == SvgStandaloneValueFormatMode.Compact ? ";" : "; ";
        if (separator == SvgValueListSeparator.Comma) return mode == SvgStandaloneValueFormatMode.Compact ? "," : ", ";
        return mode == SvgStandaloneValueFormatMode.Compact ? "," : " ";
    }

    SvgValueParseError MapNumberError(SvgNumberParseError error)
    {
        if (error == SvgNumberParseError.ExpectedNumber) return SvgValueParseError.ExpectedNumber;
        if (error == SvgNumberParseError.ExpectedInteger) return SvgValueParseError.ExpectedInteger;
        if (error == SvgNumberParseError.InvalidExponent) return SvgValueParseError.InvalidExponent;
        if (error == SvgNumberParseError.TokenTooLong) return SvgValueParseError.TokenTooLong;
        if (error == SvgNumberParseError.Overflow) return SvgValueParseError.Overflow;
        if (error == SvgNumberParseError.TrailingCharacters) return SvgValueParseError.TrailingCharacters;
        if (error == SvgNumberParseError.None) return SvgValueParseError.None;
        return SvgValueParseError.InvalidArgument;
    }

    SvgValueParseError MapListError(SvgListParseError error, SvgNumberParseError numberError)
    {
        if (numberError != SvgNumberParseError.None) return new SvgValueService().MapNumberError(numberError);
        if (error == SvgListParseError.ExpectedItem) return SvgValueParseError.ExpectedItem;
        if (error == SvgListParseError.InvalidItem) return SvgValueParseError.InvalidItem;
        if (error == SvgListParseError.MissingSeparator) return SvgValueParseError.MissingSeparator;
        if (error == SvgListParseError.TrailingSeparator) return SvgValueParseError.TrailingSeparator;
        if (error == SvgListParseError.ItemLimitExceeded) return SvgValueParseError.ItemLimitExceeded;
        if (error == SvgListParseError.OddCoordinateCount) return SvgValueParseError.OddCoordinateCount;
        if (error == SvgListParseError.None) return SvgValueParseError.None;
        return SvgValueParseError.InvalidArgument;
    }

    SvgExtendedValueParseResult ParseExtendedCore(SvgExtendedValueKind kind, string value, SvgExtendedValueParseOptions options)
    {
        if (!new SvgValueService().ValidExtendedOptions(options) || !new SvgValueService().ValidExtendedKind(kind)) return new SvgValueService().ExtendedFailure(SvgExtendedValueError.InvalidArgument, 0);
        if (kind == SvgExtendedValueKind.Length) return new SvgValueService().ParseExtendedLength(value, options);
        if (kind == SvgExtendedValueKind.Angle) return new SvgValueService().ParseExtendedAngle(value, options);
        if (kind == SvgExtendedValueKind.Time) return new SvgValueService().ParseExtendedTime(value, options);
        if (kind == SvgExtendedValueKind.Frequency) return new SvgValueService().ParseExtendedFrequency(value, options);
        if (kind == SvgExtendedValueKind.Resolution) return new SvgValueService().ParseExtendedResolution(value, options);
        if (kind == SvgExtendedValueKind.Ratio) return new SvgValueService().ParseExtendedRatio(value, options);
        if (kind == SvgExtendedValueKind.PathLength) return new SvgValueService().ParseExtendedPathLength(value, options);
        if (kind == SvgExtendedValueKind.ViewBox) return new SvgValueService().ParseExtendedViewBox(value, options);
        if (kind == SvgExtendedValueKind.PreserveAspectRatio) return new SvgValueService().ParseExtendedAspectRatio(value, options);
        if (kind == SvgExtendedValueKind.SvgTransform) return new SvgValueService().ParseExtendedSvgTransform(value, options);
        if (kind == SvgExtendedValueKind.CssTransform) return new SvgValueService().ParseExtendedCssTransform(value, options);
        if (kind == SvgExtendedValueKind.TransformOrigin) return new SvgValueService().ParseExtendedOrigin(value, options);
        if (kind == SvgExtendedValueKind.TransformBox) return new SvgValueService().ParseExtendedBox(value, options);
        if (kind == SvgExtendedValueKind.Color) return new SvgValueService().ParseExtendedColor(value, options);
        if (kind == SvgExtendedValueKind.Paint) return new SvgValueService().ParseExtendedPaint(value, options);
        if (kind == SvgExtendedValueKind.Calc) return new SvgValueService().ParseExtendedCalc(value, options);
        return new SvgValueService().ParseExtendedSpecificList(kind, value, options);
    }

    SvgExtendedValueParseResult ParseExtendedLength(string value, SvgExtendedValueParseOptions options)
    {
        SvgLengthParseResult parsed = options.UseCssNumberGrammar ? SvgLengthParser.ParseCssLength(value, options.AllowPercentage, options.AllowUnitless, options.MaxTokenCharacters) : SvgLengthParser.ParseLength(value, options.AllowPercentage, options.AllowUnitless, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapLengthError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Length); result.Length = parsed.Length;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedAngle(string value, SvgExtendedValueParseOptions options)
    {
        SvgAngleParseResult parsed = SvgScalarUnitParser.ParseAngle(value, options.AllowUnitless, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapScalarError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Angle); result.Angle = parsed.Angle;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedTime(string value, SvgExtendedValueParseOptions options)
    {
        SvgTimeParseResult parsed = SvgScalarUnitParser.ParseTime(value, options.AllowUnitless, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapScalarError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Time); result.Time = parsed.Time;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedFrequency(string value, SvgExtendedValueParseOptions options)
    {
        SvgFrequencyParseResult parsed = SvgScalarUnitParser.ParseFrequency(value, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapScalarError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Frequency); result.Frequency = parsed.Frequency;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedResolution(string value, SvgExtendedValueParseOptions options)
    {
        SvgResolutionParseResult parsed = SvgScalarUnitParser.ParseResolution(value, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapScalarError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Resolution); result.Resolution = parsed.Resolution;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedRatio(string value, SvgExtendedValueParseOptions options)
    {
        SvgRatioParseResult parsed = SvgScalarUnitParser.ParseRatio(value, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapScalarError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Ratio); result.Ratio = parsed.Ratio;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedPathLength(string value, SvgExtendedValueParseOptions options)
    {
        SvgPathLengthParseResult parsed = SvgScalarUnitParser.ParsePathLength(value, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapScalarError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.PathLength); result.PathLength = parsed.PathLength;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedViewBox(string value, SvgExtendedValueParseOptions options)
    {
        SvgViewBoxParseResult parsed = SvgViewBoxParser.ParseViewBox(value, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapViewBoxError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.ViewBox); result.ViewBox = parsed.ViewBox;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedAspectRatio(string value, SvgExtendedValueParseOptions options)
    {
        SvgAspectRatioParseResult parsed = SvgViewBoxParser.ParseAspectRatio(value, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapViewBoxError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.PreserveAspectRatio); result.AspectRatio = parsed.AspectRatio;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedSvgTransform(string value, SvgExtendedValueParseOptions options)
    {
        SvgTransformParseResult parsed = SvgTransformParser.Parse(value, options.MaxItems, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapTransformError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.SvgTransform); result.SvgTransform = parsed.TransformList;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedCssTransform(string value, SvgExtendedValueParseOptions options)
    {
        SvgCssTransformParseResult parsed = SvgCssTransformParser.ParseTransform(value, options.MaxItems, options.MaxTokenCharacters, options.RecoverInvalidItems);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapCssTransformError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.CssTransform); result.CssTransform = parsed.TransformList;
        SvgExtendedValueError error = parsed.Recovered ? new SvgValueService().MapCssTransformError(parsed.Error) : SvgExtendedValueError.None;
        return new SvgValueService().ExtendedSuccess(result, parsed.Recovered, error, parsed.ErrorOffset);
    }

    SvgExtendedValueParseResult ParseExtendedOrigin(string value, SvgExtendedValueParseOptions options)
    {
        SvgTransformOriginParseResult parsed = SvgCssTransformParser.ParseOrigin(value, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapCssTransformError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.TransformOrigin); result.TransformOrigin = parsed.Origin;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedBox(string value, SvgExtendedValueParseOptions options)
    {
        SvgTransformBoxParseResult parsed = SvgCssTransformParser.ParseTransformBox(value, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapCssTransformError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.TransformBox); result.TransformBox = parsed.TransformBox;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedColor(string value, SvgExtendedValueParseOptions options)
    {
        SvgColorParseResult parsed = new SvgColorParser().Parse(value, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapColorError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Color); result.Color = parsed.Color;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedPaint(string value, SvgExtendedValueParseOptions options)
    {
        SvgPaintParseResult parsed = new SvgPaintParser().Parse(value, options.MaxItems, options.MaxTokenCharacters, options.RecoverInvalidItems);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapPaintError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Paint); result.Paint = parsed.Paint;
        SvgExtendedValueError error = parsed.Recovered ? new SvgValueService().MapPaintError(parsed.Error) : SvgExtendedValueError.None;
        return new SvgValueService().ExtendedSuccess(result, parsed.Recovered, error, parsed.ErrorOffset);
    }

    SvgExtendedValueParseResult ParseExtendedCalc(string value, SvgExtendedValueParseOptions options)
    {
        SvgCustomPropertySet properties = options.CustomProperties == null ? new SvgCustomPropertySet() : options.CustomProperties;
        SvgCalcParseResult parsed = new SvgCalcParser().ParseWithValueAndPropertiesAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters(value, properties, options.ExpectedCalcDimension, options.MaxDepth, options.MaxNodes, options.MaxTokenCharacters);
        if (!parsed.Success) return new SvgValueService().ExtendedFailure(new SvgValueService().MapCalcError(parsed.Error), parsed.ErrorOffset);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Calc); result.CalcExpression = parsed.Expression;
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult ParseExtendedSpecificList(SvgExtendedValueKind kind, string value, SvgExtendedValueParseOptions options)
    {
        SvgExtendedValue result = new SvgExtendedValue(kind);
        if (kind == SvgExtendedValueKind.DashArray || kind == SvgExtendedValueKind.TextPositionList)
        {
            SvgLengthListParseResult parsed = kind == SvgExtendedValueKind.DashArray ? new SvgLengthListParser().ParseDashArray(value, options.MaxItems, options.MaxTokenCharacters) : new SvgLengthListParser().ParseTextPosition(value, options.MaxItems, options.MaxTokenCharacters);
            result.LengthList = parsed;
            if (!parsed.Success) return new SvgValueService().SpecificListOutcome(result, parsed.Error, parsed.ErrorOffset, options.RecoverInvalidItems && parsed.Value != null);
            return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
        }
        if (kind == SvgExtendedValueKind.RotateList)
        {
            SvgRotateListParseResult parsed = new SvgRotateListParser().Parse(value, options.MaxItems, options.MaxTokenCharacters); result.RotateList = parsed;
            if (!parsed.Success) return new SvgValueService().SpecificListOutcome(result, parsed.Error, parsed.ErrorOffset, options.RecoverInvalidItems && parsed.Items.Count > 0);
            return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
        }
        if (kind == SvgExtendedValueKind.FontFamilyList)
        {
            SvgFontFamilyParseResult parsed = new SvgFontFamilyParser().Parse(value, options.MaxItems, options.MaxTokenCharacters); result.FontFamilyList = parsed;
            if (!parsed.Success) return new SvgValueService().SpecificListOutcome(result, parsed.Error, parsed.ErrorOffset, options.RecoverInvalidItems && parsed.Families.Count > 0);
            return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
        }
        SvgPaintOrderParseResult order = new SvgPaintOrderParser().Parse(value); result.PaintOrder = order;
        if (!order.Success) return new SvgValueService().SpecificListOutcome(result, order.Error, order.ErrorOffset, options.RecoverInvalidItems && order.Items.Count > 0);
        return new SvgValueService().ExtendedSuccess(result, false, SvgExtendedValueError.None, -1);
    }

    SvgExtendedValueParseResult SpecificListOutcome(SvgExtendedValue value, SvgSpecificListParseError error, int offset, bool recovered)
    {
        SvgExtendedValueError mapped = error == SvgSpecificListParseError.ItemLimitExceeded ? SvgExtendedValueError.LimitExceeded : error == SvgSpecificListParseError.TokenTooLong ? SvgExtendedValueError.TokenTooLong : error == SvgSpecificListParseError.ExpectedValue ? SvgExtendedValueError.ExpectedValue : SvgExtendedValueError.InvalidSyntax;
        return recovered ? new SvgValueService().ExtendedSuccess(value, true, mapped, offset) : new SvgValueService().ExtendedFailure(mapped, offset);
    }

    SvgExtendedValueOperationResult InterpolateExtendedCore(SvgExtendedValue from, SvgExtendedValue to, double amount, SvgAnimationInterpolationOptions options)
    {
        if (from.Kind == SvgExtendedValueKind.Angle) return new SvgValueService().InterpolateAngleValues(from, to, amount);
        if (from.Kind == SvgExtendedValueKind.Time) return new SvgValueService().InterpolateTimeValues(from, to, amount);
        if (from.Kind == SvgExtendedValueKind.Frequency) return new SvgValueService().InterpolateFrequencyValues(from, to, amount);
        if (from.Kind == SvgExtendedValueKind.Resolution) return new SvgValueService().InterpolateResolutionValues(from, to, amount);
        if (from.Kind == SvgExtendedValueKind.Ratio) return new SvgValueService().InterpolateRatioValues(from, to, amount);
        if (from.Kind == SvgExtendedValueKind.PathLength) return new SvgValueService().InterpolatePathLengthValues(from, to, amount);
        SvgAnimationValue? first = new SvgValueService().AnimationValue(from);
        SvgAnimationValue? second = new SvgValueService().AnimationValue(to);
        if (first == null || second == null) return new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.UnsupportedInterpolation, null);
        SvgAnimationInterpolationResult interpolated = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndModeAndOptions(first, second, amount, SvgAnimationCalcMode.Linear, options);
        if (!interpolated.Success || interpolated.Value == null) return new SvgExtendedValueOperationResult(false, interpolated.Error, null);
        SvgExtendedValue? result = new SvgValueService().ExtendedFromAnimation(from.Kind, interpolated.Value);
        return result == null ? new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.IncompatibleValues, null) : new SvgExtendedValueOperationResult(true, SvgAnimationInterpolationError.None, result);
    }

    SvgExtendedValueOperationResult InterpolateAngleValues(SvgExtendedValue from, SvgExtendedValue to, double amount)
    {
        SvgUnitConversionResult first = SvgScalarUnitConverter.ConvertAngle(from.Angle, SvgAngleUnit.Deg); SvgUnitConversionResult second = SvgScalarUnitConverter.ConvertAngle(to.Angle, SvgAngleUnit.Deg);
        if (!first.Success || !second.Success) return new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.IncompatibleValues, null);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Angle); result.Angle = new SvgAngle(new SvgValueService().Lerp(first.Value, second.Value, amount), SvgAngleUnit.Deg);
        return new SvgExtendedValueOperationResult(true, SvgAnimationInterpolationError.None, result);
    }

    SvgExtendedValueOperationResult InterpolateTimeValues(SvgExtendedValue from, SvgExtendedValue to, double amount)
    {
        SvgUnitConversionResult first = SvgScalarUnitConverter.ConvertTime(from.Time, SvgTimeUnit.Seconds); SvgUnitConversionResult second = SvgScalarUnitConverter.ConvertTime(to.Time, SvgTimeUnit.Seconds);
        if (!first.Success || !second.Success) return new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.IncompatibleValues, null);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Time); result.Time = new SvgTime(new SvgValueService().Lerp(first.Value, second.Value, amount), SvgTimeUnit.Seconds);
        return new SvgExtendedValueOperationResult(true, SvgAnimationInterpolationError.None, result);
    }

    SvgExtendedValueOperationResult InterpolateFrequencyValues(SvgExtendedValue from, SvgExtendedValue to, double amount)
    {
        SvgUnitConversionResult first = SvgScalarUnitConverter.ConvertFrequency(from.Frequency, SvgFrequencyUnit.Hz); SvgUnitConversionResult second = SvgScalarUnitConverter.ConvertFrequency(to.Frequency, SvgFrequencyUnit.Hz);
        if (!first.Success || !second.Success) return new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.IncompatibleValues, null);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Frequency); result.Frequency = new SvgFrequency(new SvgValueService().Lerp(first.Value, second.Value, amount), SvgFrequencyUnit.Hz);
        return new SvgExtendedValueOperationResult(true, SvgAnimationInterpolationError.None, result);
    }

    SvgExtendedValueOperationResult InterpolateResolutionValues(SvgExtendedValue from, SvgExtendedValue to, double amount)
    {
        SvgUnitConversionResult first = SvgScalarUnitConverter.ConvertResolution(from.Resolution, SvgResolutionUnit.Dppx); SvgUnitConversionResult second = SvgScalarUnitConverter.ConvertResolution(to.Resolution, SvgResolutionUnit.Dppx);
        if (!first.Success || !second.Success) return new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.IncompatibleValues, null);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Resolution); result.Resolution = new SvgResolution(new SvgValueService().Lerp(first.Value, second.Value, amount), SvgResolutionUnit.Dppx);
        return new SvgExtendedValueOperationResult(true, SvgAnimationInterpolationError.None, result);
    }

    SvgExtendedValueOperationResult InterpolateRatioValues(SvgExtendedValue from, SvgExtendedValue to, double amount)
    {
        if (from.Ratio == null || to.Ratio == null || !new SvgValueService().ValidRatioValue(from.Ratio) || !new SvgValueService().ValidRatioValue(to.Ratio)) return new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.IncompatibleValues, null);
        double numerator = new SvgValueService().Lerp(from.Ratio.Numerator, to.Ratio.Numerator, amount);
        double denominator = new SvgValueService().Lerp(from.Ratio.Denominator, to.Ratio.Denominator, amount);
        if (!SvgFiniteMath.IsFinite(numerator) || numerator <= 0 || !SvgFiniteMath.IsFinite(denominator) || denominator <= 0) return new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.IncompatibleValues, null);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.Ratio); result.Ratio = new SvgRatio(numerator, denominator);
        return new SvgExtendedValueOperationResult(true, SvgAnimationInterpolationError.None, result);
    }

    SvgExtendedValueOperationResult InterpolatePathLengthValues(SvgExtendedValue from, SvgExtendedValue to, double amount)
    {
        if (from.PathLength == null || to.PathLength == null || !new SvgValueService().ValidPathLengthValue(from.PathLength) || !new SvgValueService().ValidPathLengthValue(to.PathLength)) return new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.IncompatibleValues, null);
        double value = new SvgValueService().Lerp(from.PathLength.Value, to.PathLength.Value, amount);
        if (!SvgFiniteMath.IsFinite(value) || value < 0) return new SvgExtendedValueOperationResult(false, SvgAnimationInterpolationError.IncompatibleValues, null);
        SvgExtendedValue result = new SvgExtendedValue(SvgExtendedValueKind.PathLength); result.PathLength = new SvgPathLength(value);
        return new SvgExtendedValueOperationResult(true, SvgAnimationInterpolationError.None, result);
    }

    SvgAnimationValue? AnimationValue(SvgExtendedValue value)
    {
        if (value.Kind == SvgExtendedValueKind.Length && value.Length != null) return SvgAnimationValue.FromLength(value.Length);
        if (value.Kind == SvgExtendedValueKind.Color && value.Color != null) return SvgAnimationValue.FromColor(value.Color);
        if (value.Kind == SvgExtendedValueKind.SvgTransform && value.SvgTransform != null) return SvgAnimationValue.FromTransform(value.SvgTransform);
        if (value.Kind == SvgExtendedValueKind.ViewBox && value.ViewBox != null) return SvgAnimationValue.FromViewBox(value.ViewBox);
        if (value.Kind == SvgExtendedValueKind.Paint && value.Paint != null) return SvgAnimationValue.FromPaint(value.Paint);
        return null;
    }

    SvgExtendedValue? ExtendedFromAnimation(SvgExtendedValueKind kind, SvgAnimationValue value)
    {
        SvgExtendedValue result = new SvgExtendedValue(kind);
        if (kind == SvgExtendedValueKind.Length) result.Length = value.Length;
        else if (kind == SvgExtendedValueKind.Color) result.Color = value.Color;
        else if (kind == SvgExtendedValueKind.SvgTransform) result.SvgTransform = value.Transform;
        else if (kind == SvgExtendedValueKind.ViewBox) result.ViewBox = value.ViewBox;
        else if (kind == SvgExtendedValueKind.Paint) result.Paint = value.Paint;
        else return null;
        return result;
    }

    double Lerp(double first, double second, double amount)
    {
        if (amount == 0) return first;
        if (amount == 1) return second;
        return first * (1 - amount) + second * amount;
    }

    SvgCalcEvaluationResult EvaluateCalcCore(SvgCalcExpression expression, SvgLengthContext context, SvgLengthPercentageBasis basis)
    {
        if (!expression.IsValid || expression.Children == null || !new SvgValueService().ValidCalcKind(expression.Kind) || !new SvgValueService().ValidCalcDimension(expression.Dimension)) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, SvgCalcDimension.Any, 0);
        if (expression.Kind == SvgCalcExpressionKind.Value) return new SvgValueService().EvaluateCalcLeaf(expression, context, basis);
        if (expression.Kind == SvgCalcExpressionKind.Calc)
        {
            if (expression.Children.Count != 1) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
            return new SvgValueService().EvaluateCalcCore(expression.Children[0], context, basis);
        }
        if (expression.Kind == SvgCalcExpressionKind.Negate)
        {
            if (expression.Children.Count != 1) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
            SvgCalcEvaluationResult operand = new SvgValueService().EvaluateCalcCore(expression.Children[0], context, basis);
            if (!operand.Success) return operand;
            return new SvgValueService().CalcFinite(expression.Dimension, -operand.Value);
        }
        if (expression.Kind == SvgCalcExpressionKind.Min || expression.Kind == SvgCalcExpressionKind.Max || expression.Kind == SvgCalcExpressionKind.Clamp) return new SvgValueService().EvaluateCalcFunction(expression, context, basis);
        if (expression.Children.Count != 2) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
        SvgCalcEvaluationResult left = new SvgValueService().EvaluateCalcCore(expression.Children[0], context, basis);
        if (!left.Success) return left;
        SvgCalcEvaluationResult right = new SvgValueService().EvaluateCalcCore(expression.Children[1], context, basis);
        if (!right.Success) return right;
        double value;
        if (expression.Kind == SvgCalcExpressionKind.Add) value = left.Value + right.Value;
        else if (expression.Kind == SvgCalcExpressionKind.Subtract) value = left.Value - right.Value;
        else if (expression.Kind == SvgCalcExpressionKind.Multiply) value = left.Value * right.Value;
        else
        {
            if (right.Value == 0) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.DivisionByZero, expression.Dimension, 0);
            value = left.Value / right.Value;
        }
        return new SvgValueService().CalcFinite(expression.Dimension, value);
    }

    SvgCalcEvaluationResult EvaluateCalcLeaf(SvgCalcExpression expression, SvgLengthContext context, SvgLengthPercentageBasis basis)
    {
        if (!SvgFiniteMath.IsFinite(expression.Value) || expression.Unit == null) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
        if (expression.Dimension == SvgCalcDimension.Number && expression.Unit.Length == 0) return new SvgCalcEvaluationResult(true, SvgCalcEvaluationError.None, expression.Dimension, expression.Value);
        string text = SvgNumberFormatter.FormatShortest(expression.Value).Text + expression.Unit;
        if (expression.Dimension == SvgCalcDimension.Length || expression.Dimension == SvgCalcDimension.Percentage || expression.Dimension == SvgCalcDimension.LengthPercentage)
        {
            SvgLengthParseResult parsed = SvgLengthParser.ParseCssLength(text, true, true, 4096);
            if (!parsed.Success || parsed.Length == null) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
            SvgLengthEvaluationResult evaluated = SvgLengthEvaluator.Evaluate(parsed.Length, context, basis);
            if (!evaluated.Success)
            {
                SvgCalcEvaluationError error = evaluated.Error == SvgLengthEvaluationError.NonFiniteResult ? SvgCalcEvaluationError.NonFiniteResult : SvgCalcEvaluationError.MissingContext;
                return new SvgCalcEvaluationResult(false, error, expression.Dimension, 0);
            }
            return new SvgCalcEvaluationResult(true, SvgCalcEvaluationError.None, expression.Dimension, evaluated.Value);
        }
        if (expression.Dimension == SvgCalcDimension.Angle)
        {
            SvgAngleParseResult parsed = SvgScalarUnitParser.ParseAngle(text, true, 4096); SvgUnitConversionResult converted = SvgScalarUnitConverter.ConvertAngle(parsed.Angle, SvgAngleUnit.Deg);
            return converted.Success ? new SvgCalcEvaluationResult(true, SvgCalcEvaluationError.None, expression.Dimension, converted.Value) : new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
        }
        if (expression.Dimension == SvgCalcDimension.Time)
        {
            SvgTimeParseResult parsed = SvgScalarUnitParser.ParseTime(text, true, 4096); SvgUnitConversionResult converted = SvgScalarUnitConverter.ConvertTime(parsed.Time, SvgTimeUnit.Seconds);
            return converted.Success ? new SvgCalcEvaluationResult(true, SvgCalcEvaluationError.None, expression.Dimension, converted.Value) : new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
        }
        if (expression.Dimension == SvgCalcDimension.Frequency)
        {
            SvgFrequencyParseResult parsed = SvgScalarUnitParser.ParseFrequency(text, 4096); SvgUnitConversionResult converted = SvgScalarUnitConverter.ConvertFrequency(parsed.Frequency, SvgFrequencyUnit.Hz);
            return converted.Success ? new SvgCalcEvaluationResult(true, SvgCalcEvaluationError.None, expression.Dimension, converted.Value) : new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
        }
        if (expression.Dimension == SvgCalcDimension.Resolution)
        {
            SvgResolutionParseResult parsed = SvgScalarUnitParser.ParseResolution(text, 4096); SvgUnitConversionResult converted = SvgScalarUnitConverter.ConvertResolution(parsed.Resolution, SvgResolutionUnit.Dppx);
            return converted.Success ? new SvgCalcEvaluationResult(true, SvgCalcEvaluationError.None, expression.Dimension, converted.Value) : new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
        }
        return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
    }

    SvgCalcEvaluationResult EvaluateCalcFunction(SvgCalcExpression expression, SvgLengthContext context, SvgLengthPercentageBasis basis)
    {
        int required = expression.Kind == SvgCalcExpressionKind.Clamp ? 3 : 1;
        if (expression.Children.Count < required || expression.Kind == SvgCalcExpressionKind.Clamp && expression.Children.Count != 3) return new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.InvalidArgument, expression.Dimension, 0);
        SvgCalcEvaluationResult first = new SvgValueService().EvaluateCalcCore(expression.Children[0], context, basis);
        if (!first.Success) return first;
        if (expression.Kind == SvgCalcExpressionKind.Clamp)
        {
            SvgCalcEvaluationResult preferred = new SvgValueService().EvaluateCalcCore(expression.Children[1], context, basis); if (!preferred.Success) return preferred;
            SvgCalcEvaluationResult maximum = new SvgValueService().EvaluateCalcCore(expression.Children[2], context, basis); if (!maximum.Success) return maximum;
            double upperBounded = preferred.Value > maximum.Value ? maximum.Value : preferred.Value;
            double clamped = upperBounded < first.Value ? first.Value : upperBounded;
            return new SvgValueService().CalcFinite(expression.Dimension, clamped);
        }
        double selected = first.Value;
        int index = 1;
        while (index < expression.Children.Count)
        {
            SvgCalcEvaluationResult item = new SvgValueService().EvaluateCalcCore(expression.Children[index], context, basis); if (!item.Success) return item;
            if (expression.Kind == SvgCalcExpressionKind.Min && item.Value < selected || expression.Kind == SvgCalcExpressionKind.Max && item.Value > selected) selected = item.Value;
            index = index + 1;
        }
        return new SvgValueService().CalcFinite(expression.Dimension, selected);
    }

    SvgCalcEvaluationResult CalcFinite(SvgCalcDimension dimension, double value)
    {
        return SvgFiniteMath.IsFinite(value) ? new SvgCalcEvaluationResult(true, SvgCalcEvaluationError.None, dimension, SvgFiniteMath.NormalizeZero(value)) : new SvgCalcEvaluationResult(false, SvgCalcEvaluationError.NonFiniteResult, dimension, 0);
    }

    string? SerializeExtendedCore(SvgExtendedValue value)
    {
        if (!new SvgValueService().ValidExtendedKind(value.Kind)) return null;
        if (value.Kind == SvgExtendedValueKind.Length) return value.Length == null || !new SvgValueService().ValidLengthValue(value.Length) ? null : value.Length.ToText();
        if (value.Kind == SvgExtendedValueKind.Angle) return value.Angle == null || !new SvgValueService().ValidAngleValue(value.Angle) ? null : value.Angle.ToText();
        if (value.Kind == SvgExtendedValueKind.Time) return value.Time == null || !new SvgValueService().ValidTimeValue(value.Time) ? null : value.Time.ToText();
        if (value.Kind == SvgExtendedValueKind.Frequency) return value.Frequency == null || !new SvgValueService().ValidFrequencyValue(value.Frequency) ? null : value.Frequency.ToText();
        if (value.Kind == SvgExtendedValueKind.Resolution) return value.Resolution == null || !new SvgValueService().ValidResolutionValue(value.Resolution) ? null : value.Resolution.ToText();
        if (value.Kind == SvgExtendedValueKind.Ratio) return value.Ratio == null || !new SvgValueService().ValidRatioValue(value.Ratio) ? null : value.Ratio.ToText();
        if (value.Kind == SvgExtendedValueKind.PathLength) return value.PathLength == null || !new SvgValueService().ValidPathLengthValue(value.PathLength) ? null : value.PathLength.ToText();
        if (value.Kind == SvgExtendedValueKind.ViewBox) return value.ViewBox == null ? null : value.ViewBox.ToText();
        if (value.Kind == SvgExtendedValueKind.PreserveAspectRatio) return value.AspectRatio == null ? null : value.AspectRatio.ToText();
        if (value.Kind == SvgExtendedValueKind.SvgTransform) return value.SvgTransform == null ? null : value.SvgTransform.ToText();
        if (value.Kind == SvgExtendedValueKind.CssTransform) return value.CssTransform == null ? null : value.CssTransform.ToText();
        if (value.Kind == SvgExtendedValueKind.TransformOrigin) return value.TransformOrigin == null ? null : value.TransformOrigin.ToText();
        if (value.Kind == SvgExtendedValueKind.TransformBox) return SvgCssTransformRules.IsBox(value.TransformBox) ? SvgCssTransformRules.BoxText(value.TransformBox) : null;
        if (value.Kind == SvgExtendedValueKind.Color) return value.Color == null ? null : value.Color.ToText();
        if (value.Kind == SvgExtendedValueKind.Paint) return value.Paint == null ? null : value.Paint.ToText();
        if (value.Kind == SvgExtendedValueKind.Calc) return value.CalcExpression == null ? null : new SvgValueService().SerializeCalc(value.CalcExpression);
        if (value.Kind == SvgExtendedValueKind.DashArray || value.Kind == SvgExtendedValueKind.TextPositionList) return value.LengthList == null || value.LengthList.Value == null ? null : value.LengthList.Value.ToText();
        if (value.Kind == SvgExtendedValueKind.RotateList) return value.RotateList == null ? null : new SvgValueService().SerializeRotateList(value.RotateList);
        if (value.Kind == SvgExtendedValueKind.FontFamilyList) return value.FontFamilyList == null ? null : new SvgValueService().SerializeFontFamilies(value.FontFamilyList);
        return value.PaintOrder == null ? null : new SvgValueService().SerializePaintOrder(value.PaintOrder);
    }

    string? SerializeCalc(SvgCalcExpression expression)
    {
        if (expression == null || !expression.IsValid || expression.Children == null || !new SvgValueService().ValidCalcKind(expression.Kind) || !new SvgValueService().ValidCalcDimension(expression.Dimension)) return null;
        if (expression.Kind == SvgCalcExpressionKind.Value)
        {
            if (expression.Children.Count != 0 || expression.Unit == null || !SvgFiniteMath.IsFinite(expression.Value)) return null;
            SvgNumberFormatResult formatted = SvgNumberFormatter.FormatShortest(expression.Value);
            return formatted.Success ? formatted.Text + expression.Unit : null;
        }
        if (expression.Kind == SvgCalcExpressionKind.Calc)
        {
            if (expression.Children.Count != 1) return null; string? child = new SvgValueService().SerializeCalc(expression.Children[0]); return child == null ? null : "calc(" + child + ")";
        }
        if (expression.Kind == SvgCalcExpressionKind.Negate)
        {
            if (expression.Children.Count != 1) return null; string? child = new SvgValueService().SerializeCalc(expression.Children[0]); return child == null ? null : "-(" + child + ")";
        }
        if (expression.Kind == SvgCalcExpressionKind.Min || expression.Kind == SvgCalcExpressionKind.Max || expression.Kind == SvgCalcExpressionKind.Clamp)
        {
            string name = expression.Kind == SvgCalcExpressionKind.Min ? "min" : expression.Kind == SvgCalcExpressionKind.Max ? "max" : "clamp";
            System.Text.StringBuilder builder = new System.Text.StringBuilder(); builder.Append(name); builder.Append('(');
            int index = 0; while (index < expression.Children.Count) { string? item = new SvgValueService().SerializeCalc(expression.Children[index]); if (item == null) return null; if (index > 0) builder.Append(", "); builder.Append(item); index = index + 1; }
            builder.Append(')'); return builder.ToString();
        }
        if (expression.Children.Count != 2) return null;
        string? left = new SvgValueService().SerializeCalc(expression.Children[0]); string? right = new SvgValueService().SerializeCalc(expression.Children[1]); if (left == null || right == null) return null;
        string operation = expression.Kind == SvgCalcExpressionKind.Add ? " + " : expression.Kind == SvgCalcExpressionKind.Subtract ? " - " : expression.Kind == SvgCalcExpressionKind.Multiply ? " * " : " / ";
        return "(" + left + operation + right + ")";
    }

    string? SerializeRotateList(SvgRotateListParseResult result)
    {
        if (result.Keyword != SvgListKeyword.Values) return SvgSpecificListRules.KeywordText(result.Keyword);
        System.Text.StringBuilder builder = new System.Text.StringBuilder(); int index = 0;
        while (index < result.Items.Count) { if (result.Items[index] == null || result.Items[index].Angle == null) return null; if (index > 0) builder.Append(' '); builder.Append(result.Items[index].Angle.ToText()); index = index + 1; }
        return builder.ToString();
    }

    string? SerializeFontFamilies(SvgFontFamilyParseResult result)
    {
        if (result.Keyword != SvgListKeyword.Values) return SvgSpecificListRules.KeywordText(result.Keyword);
        System.Text.StringBuilder builder = new System.Text.StringBuilder(); int index = 0;
        while (index < result.Families.Count)
        {
            SvgFontFamily family = result.Families[index]; if (family == null || family.Name == null) return null; if (index > 0) builder.Append(", ");
            if (family.WasQuoted || family.Name.IndexOf(' ') >= 0) { builder.Append('"'); builder.Append(family.Name.Replace("\\", "\\\\").Replace("\"", "\\\"")); builder.Append('"'); } else builder.Append(family.Name);
            index = index + 1;
        }
        return builder.ToString();
    }

    string? SerializePaintOrder(SvgPaintOrderParseResult result)
    {
        if (result.Keyword != SvgListKeyword.Values) return SvgSpecificListRules.KeywordText(result.Keyword);
        System.Text.StringBuilder builder = new System.Text.StringBuilder(); int index = 0;
        while (index < result.Items.Count) { if (index > 0) builder.Append(' '); SvgPaintOrderComponent item = result.Items[index]; builder.Append(item == SvgPaintOrderComponent.Fill ? "fill" : item == SvgPaintOrderComponent.Stroke ? "stroke" : item == SvgPaintOrderComponent.Markers ? "markers" : ""); index = index + 1; }
        return builder.ToString();
    }

    bool ValidExtendedOptions(SvgExtendedValueParseOptions options)
    {
        return options != null && options.MaxItems > 0 && options.MaxTokenCharacters > 0 && options.MaxDepth > 0 && options.MaxNodes > 0 && new SvgValueService().ValidCalcDimension(options.ExpectedCalcDimension);
    }

    bool ValidExtendedKind(SvgExtendedValueKind kind)
    {
        return kind >= SvgExtendedValueKind.Length && kind <= SvgExtendedValueKind.PaintOrder;
    }

    bool ValidLengthValue(SvgLength? value)
    {
        return value != null && SvgFiniteMath.IsFinite(value.Value) && SvgLengthUnits.IsKnown(value.Unit);
    }

    bool ValidAngleValue(SvgAngle? value)
    {
        return value != null && SvgFiniteMath.IsFinite(value.Value) && SvgScalarUnitNames.IsAngleUnit(value.Unit);
    }

    bool ValidTimeValue(SvgTime? value)
    {
        return value != null && SvgFiniteMath.IsFinite(value.Value) && SvgScalarUnitNames.IsTimeUnit(value.Unit);
    }

    bool ValidFrequencyValue(SvgFrequency? value)
    {
        return value != null && SvgFiniteMath.IsFinite(value.Value) && value.Value >= 0 && SvgScalarUnitNames.IsFrequencyUnit(value.Unit);
    }

    bool ValidResolutionValue(SvgResolution? value)
    {
        return value != null && SvgFiniteMath.IsFinite(value.Value) && value.Value > 0 && SvgScalarUnitNames.IsResolutionUnit(value.Unit);
    }

    bool ValidRatioValue(SvgRatio? value)
    {
        return value != null && SvgFiniteMath.IsFinite(value.Numerator) && value.Numerator > 0 && SvgFiniteMath.IsFinite(value.Denominator) && value.Denominator > 0;
    }

    bool ValidPathLengthValue(SvgPathLength? value)
    {
        return value != null && SvgFiniteMath.IsFinite(value.Value) && value.Value >= 0;
    }

    bool ValidCalcKind(SvgCalcExpressionKind kind)
    {
        return kind >= SvgCalcExpressionKind.Value && kind <= SvgCalcExpressionKind.Clamp;
    }

    bool ValidCalcDimension(SvgCalcDimension dimension)
    {
        return dimension >= SvgCalcDimension.Any && dimension <= SvgCalcDimension.Resolution;
    }

    bool CompatibleCalcDimensions(SvgCalcDimension first, SvgCalcDimension second)
    {
        if (first == second) return true;
        bool firstLength = first == SvgCalcDimension.Length || first == SvgCalcDimension.Percentage || first == SvgCalcDimension.LengthPercentage;
        bool secondLength = second == SvgCalcDimension.Length || second == SvgCalcDimension.Percentage || second == SvgCalcDimension.LengthPercentage;
        return firstLength && secondLength;
    }

    SvgExtendedValueError MapLengthError(SvgLengthParseError error)
    {
        if (error == SvgLengthParseError.ExpectedLength) return SvgExtendedValueError.ExpectedValue; if (error == SvgLengthParseError.InvalidNumber) return SvgExtendedValueError.InvalidNumber;
        if (error == SvgLengthParseError.UnknownUnit || error == SvgLengthParseError.UnitRequired || error == SvgLengthParseError.PercentageNotAllowed) return SvgExtendedValueError.InvalidUnitOrKeyword;
        if (error == SvgLengthParseError.TokenTooLong) return SvgExtendedValueError.TokenTooLong; if (error == SvgLengthParseError.TrailingCharacters) return SvgExtendedValueError.TrailingCharacters; return SvgExtendedValueError.InvalidArgument;
    }

    SvgExtendedValueError MapScalarError(SvgScalarUnitParseError error)
    {
        if (error == SvgScalarUnitParseError.ExpectedValue || error == SvgScalarUnitParseError.ExpectedRatioComponent) return SvgExtendedValueError.ExpectedValue; if (error == SvgScalarUnitParseError.InvalidNumber) return SvgExtendedValueError.InvalidNumber;
        if (error == SvgScalarUnitParseError.UnknownUnit || error == SvgScalarUnitParseError.UnitRequired) return SvgExtendedValueError.InvalidUnitOrKeyword; if (error == SvgScalarUnitParseError.OutOfRange) return SvgExtendedValueError.OutOfRange;
        if (error == SvgScalarUnitParseError.TokenTooLong) return SvgExtendedValueError.TokenTooLong; if (error == SvgScalarUnitParseError.TrailingCharacters) return SvgExtendedValueError.TrailingCharacters; return SvgExtendedValueError.InvalidArgument;
    }

    SvgExtendedValueError MapViewBoxError(SvgViewBoxParseError error)
    {
        if (error == SvgViewBoxParseError.ExpectedValue || error == SvgViewBoxParseError.ComponentCount) return SvgExtendedValueError.ExpectedValue; if (error == SvgViewBoxParseError.InvalidNumber) return SvgExtendedValueError.InvalidNumber;
        if (error == SvgViewBoxParseError.NegativeDimension) return SvgExtendedValueError.OutOfRange; if (error == SvgViewBoxParseError.UnknownAlignment || error == SvgViewBoxParseError.UnknownScale) return SvgExtendedValueError.InvalidUnitOrKeyword;
        if (error == SvgViewBoxParseError.TokenTooLong) return SvgExtendedValueError.TokenTooLong; if (error == SvgViewBoxParseError.TrailingCharacters) return SvgExtendedValueError.TrailingCharacters; return SvgExtendedValueError.InvalidArgument;
    }

    SvgExtendedValueError MapTransformError(SvgTransformParseError error)
    {
        if (error == SvgTransformParseError.TransformLimitExceeded) return SvgExtendedValueError.LimitExceeded; if (error == SvgTransformParseError.TokenTooLong) return SvgExtendedValueError.TokenTooLong;
        if (error == SvgTransformParseError.ExpectedTransform || error == SvgTransformParseError.ExpectedParameter) return SvgExtendedValueError.ExpectedValue; if (error == SvgTransformParseError.InvalidNumber) return SvgExtendedValueError.InvalidNumber;
        if (error == SvgTransformParseError.InvalidArgument) return SvgExtendedValueError.InvalidArgument; return SvgExtendedValueError.InvalidSyntax;
    }

    SvgExtendedValueError MapCssTransformError(SvgCssTransformParseError error)
    {
        if (error == SvgCssTransformParseError.FunctionLimitExceeded) return SvgExtendedValueError.LimitExceeded; if (error == SvgCssTransformParseError.TokenTooLong) return SvgExtendedValueError.TokenTooLong;
        if (error == SvgCssTransformParseError.ExpectedValue) return SvgExtendedValueError.ExpectedValue; if (error == SvgCssTransformParseError.TrailingCharacters) return SvgExtendedValueError.TrailingCharacters;
        if (error == SvgCssTransformParseError.InvalidParserArgument) return SvgExtendedValueError.InvalidArgument; return SvgExtendedValueError.InvalidSyntax;
    }

    SvgExtendedValueError MapColorError(SvgColorParseError error)
    {
        if (error == SvgColorParseError.ExpectedColor || error == SvgColorParseError.ExpectedComponent) return SvgExtendedValueError.ExpectedValue; if (error == SvgColorParseError.InvalidNumber || error == SvgColorParseError.NonFiniteResult) return SvgExtendedValueError.InvalidNumber;
        if (error == SvgColorParseError.InvalidUnit || error == SvgColorParseError.UnknownColor) return SvgExtendedValueError.InvalidUnitOrKeyword; if (error == SvgColorParseError.TokenTooLong) return SvgExtendedValueError.TokenTooLong;
        if (error == SvgColorParseError.TrailingCharacters) return SvgExtendedValueError.TrailingCharacters; if (error == SvgColorParseError.InvalidArgument) return SvgExtendedValueError.InvalidArgument; return SvgExtendedValueError.InvalidSyntax;
    }

    SvgExtendedValueError MapPaintError(SvgPaintParseError error)
    {
        if (error == SvgPaintParseError.ExpectedPaint) return SvgExtendedValueError.ExpectedValue; if (error == SvgPaintParseError.FallbackLimitExceeded) return SvgExtendedValueError.LimitExceeded;
        if (error == SvgPaintParseError.TokenTooLong) return SvgExtendedValueError.TokenTooLong; if (error == SvgPaintParseError.TrailingCharacters) return SvgExtendedValueError.TrailingCharacters; if (error == SvgPaintParseError.InvalidArgument) return SvgExtendedValueError.InvalidArgument;
        return SvgExtendedValueError.InvalidSyntax;
    }

    SvgExtendedValueError MapCalcError(SvgCalcParseError error)
    {
        if (error == SvgCalcParseError.ExpectedExpression) return SvgExtendedValueError.ExpectedValue; if (error == SvgCalcParseError.TokenTooLong) return SvgExtendedValueError.TokenTooLong;
        if (error == SvgCalcParseError.DepthExceeded || error == SvgCalcParseError.NodeLimitExceeded) return SvgExtendedValueError.LimitExceeded; if (error == SvgCalcParseError.UnknownUnit) return SvgExtendedValueError.InvalidUnitOrKeyword;
        if (error == SvgCalcParseError.InvalidArgument) return SvgExtendedValueError.InvalidArgument; return SvgExtendedValueError.InvalidSyntax;
    }

    SvgExtendedValueParseResult ExtendedSuccess(SvgExtendedValue value, bool recovered, SvgExtendedValueError error, int offset)
    {
        return new SvgExtendedValueParseResult(true, recovered, error, value, offset);
    }

    SvgExtendedValueParseResult ExtendedFailure(SvgExtendedValueError error, int offset)
    {
        return new SvgExtendedValueParseResult(false, false, error, null, offset);
    }

    SvgValueFormatResult FormatSuccess(string text)
    {
        return new SvgValueFormatResult(true, SvgValueFormatError.None, text, text.Length, 0);
    }

    SvgValueFormatResult FormatFailure(SvgValueFormatError error)
    {
        return new SvgValueFormatResult(false, error, "", 0, 0);
    }
}
