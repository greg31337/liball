namespace svg;

/// <summary>Defines public standalone parsing and deterministic formatting for basic SVG numeric values.</summary>
public interface ISvgValueService
{
    /// <summary>Parses one complete finite SVG number from a string.</summary>
    /// <param name="value">The authored value.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed number or exact failure.</returns>
    SvgValueNumberResult ParseNumberWithString(string? value, SvgValueParseOptions options);
    /// <summary>Parses one complete finite SVG number from a character span.</summary>
    /// <param name="value">The authored value.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed number or exact failure.</returns>
    SvgValueNumberResult ParseNumberWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueParseOptions options);
    /// <summary>Parses one complete signed decimal integer from a string.</summary>
    /// <param name="value">The authored value.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed integer or exact failure.</returns>
    SvgValueIntegerResult ParseIntegerWithString(string? value, SvgValueParseOptions options);
    /// <summary>Parses one complete signed decimal integer from a character span.</summary>
    /// <param name="value">The authored value.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed integer or exact failure.</returns>
    SvgValueIntegerResult ParseIntegerWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueParseOptions options);
    /// <summary>Parses one complete finite SVG coordinate from a string.</summary>
    /// <param name="value">The authored coordinate.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed coordinate or exact failure.</returns>
    SvgValueNumberResult ParseCoordinateWithString(string? value, SvgValueParseOptions options);
    /// <summary>Parses one complete finite SVG coordinate from a character span.</summary>
    /// <param name="value">The authored coordinate.</param><param name="options">The bounds and recovery behavior.</param><returns>The parsed coordinate or exact failure.</returns>
    SvgValueNumberResult ParseCoordinateWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueParseOptions options);
    /// <summary>Parses one required number and one optional number from a string.</summary>
    /// <param name="value">The authored pair.</param><param name="options">The bounds and recovery behavior.</param><returns>The pair or exact failure.</returns>
    SvgValueOptionalPairResult ParseOptionalPairWithString(string? value, SvgValueParseOptions options);
    /// <summary>Parses one required number and one optional number from a character span.</summary>
    /// <param name="value">The authored pair.</param><param name="options">The bounds and recovery behavior.</param><returns>The pair or exact failure.</returns>
    SvgValueOptionalPairResult ParseOptionalPairWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueParseOptions options);
    /// <summary>Parses a bounded finite-number list from a string.</summary>
    /// <param name="value">The authored list.</param><param name="separator">The separator grammar.</param><param name="options">The bounds and recovery behavior.</param><returns>The values or exact failure.</returns>
    SvgValueNumberListResult ParseNumberListWithString(string? value, SvgValueListSeparator separator, SvgValueParseOptions options);
    /// <summary>Parses a bounded finite-number list from a character span.</summary>
    /// <param name="value">The authored list.</param><param name="separator">The separator grammar.</param><param name="options">The bounds and recovery behavior.</param><returns>The values or exact failure.</returns>
    SvgValueNumberListResult ParseNumberListWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueListSeparator separator, SvgValueParseOptions options);
    /// <summary>Parses a bounded signed integer list from a string.</summary>
    /// <param name="value">The authored list.</param><param name="separator">The separator grammar.</param><param name="options">The bounds and recovery behavior.</param><returns>The values or exact failure.</returns>
    SvgValueIntegerListResult ParseIntegerListWithString(string? value, SvgValueListSeparator separator, SvgValueParseOptions options);
    /// <summary>Parses a bounded signed integer list from a character span.</summary>
    /// <param name="value">The authored list.</param><param name="separator">The separator grammar.</param><param name="options">The bounds and recovery behavior.</param><returns>The values or exact failure.</returns>
    SvgValueIntegerListResult ParseIntegerListWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueListSeparator separator, SvgValueParseOptions options);
    /// <summary>Parses a bounded SVG points list from a string.</summary>
    /// <param name="value">The authored points.</param><param name="options">The point bound and recovery behavior.</param><returns>The points or exact failure.</returns>
    SvgValuePointListResult ParsePointListWithString(string? value, SvgValueParseOptions options);
    /// <summary>Parses a bounded SVG points list from a character span.</summary>
    /// <param name="value">The authored points.</param><param name="options">The point bound and recovery behavior.</param><returns>The points or exact failure.</returns>
    SvgValuePointListResult ParsePointListWithReadOnlySpanOfChar(System.ReadOnlySpan<char> value, SvgValueParseOptions options);
    /// <summary>Formats one finite number.</summary>
    /// <param name="value">The finite number.</param><param name="mode">The deterministic output mode.</param><returns>The formatted text or stable failure.</returns>
    SvgValueFormatResult FormatNumber(double value, SvgStandaloneValueFormatMode mode);
    /// <summary>Formats one finite number atomically into a caller destination.</summary>
    /// <param name="value">The finite number.</param><param name="mode">The deterministic output mode.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    SvgValueFormatResult FormatNumberToSpan(double value, SvgStandaloneValueFormatMode mode, System.Span<char> destination);
    /// <summary>Formats one finite coordinate.</summary>
    /// <param name="value">The finite coordinate.</param><param name="mode">The deterministic output mode.</param><returns>The formatted text or stable failure.</returns>
    SvgValueFormatResult FormatCoordinate(double value, SvgStandaloneValueFormatMode mode);
    /// <summary>Formats one finite coordinate atomically into a caller destination.</summary>
    /// <param name="value">The finite coordinate.</param><param name="mode">The deterministic output mode.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    SvgValueFormatResult FormatCoordinateToSpan(double value, SvgStandaloneValueFormatMode mode, System.Span<char> destination);
    /// <summary>Formats one signed integer.</summary>
    /// <param name="value">The integer.</param><returns>The invariant decimal text.</returns>
    SvgValueFormatResult FormatInteger(int value);
    /// <summary>Formats one signed integer atomically into a caller destination.</summary>
    /// <param name="value">The integer.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    SvgValueFormatResult FormatIntegerToSpan(int value, System.Span<char> destination);
    /// <summary>Formats a finite-number list.</summary>
    /// <param name="values">The finite numbers.</param><param name="separator">The output separator.</param><param name="mode">The deterministic output mode.</param><returns>The formatted text or stable failure.</returns>
    SvgValueFormatResult FormatNumberList(double[]? values, SvgValueListSeparator separator, SvgStandaloneValueFormatMode mode);
    /// <summary>Formats a finite-number list atomically into a caller destination.</summary>
    /// <param name="values">The finite numbers.</param><param name="separator">The output separator.</param><param name="mode">The deterministic output mode.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    SvgValueFormatResult FormatNumberListToSpan(double[]? values, SvgValueListSeparator separator, SvgStandaloneValueFormatMode mode, System.Span<char> destination);
    /// <summary>Formats a signed integer list.</summary>
    /// <param name="values">The signed integers.</param><param name="separator">The output separator.</param><returns>The formatted text or stable failure.</returns>
    SvgValueFormatResult FormatIntegerList(int[]? values, SvgValueListSeparator separator);
    /// <summary>Formats a signed integer list atomically into a caller destination.</summary>
    /// <param name="values">The signed integers.</param><param name="separator">The output separator.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    SvgValueFormatResult FormatIntegerListToSpan(int[]? values, SvgValueListSeparator separator, System.Span<char> destination);
    /// <summary>Formats a point list.</summary>
    /// <param name="points">The finite points.</param><param name="mode">The deterministic output mode.</param><returns>The formatted text or stable failure.</returns>
    SvgValueFormatResult FormatPointList(System.Collections.Generic.List<SvgPoint>? points, SvgStandaloneValueFormatMode mode);
    /// <summary>Formats a point list atomically into a caller destination.</summary>
    /// <param name="points">The finite points.</param><param name="mode">The deterministic output mode.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    SvgValueFormatResult FormatPointListToSpan(System.Collections.Generic.List<SvgPoint>? points, SvgStandaloneValueFormatMode mode, System.Span<char> destination);
    /// <summary>Formats an optional pair.</summary>
    /// <param name="first">The finite first value.</param><param name="second">The finite optional value.</param><param name="hasSecond">Whether to emit the second value.</param><param name="mode">The deterministic output mode.</param><returns>The formatted text or stable failure.</returns>
    SvgValueFormatResult FormatOptionalPair(double first, double second, bool hasSecond, SvgStandaloneValueFormatMode mode);
    /// <summary>Formats an optional pair atomically into a caller destination.</summary>
    /// <param name="first">The finite first value.</param><param name="second">The finite optional value.</param><param name="hasSecond">Whether to emit the second value.</param><param name="mode">The deterministic output mode.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    SvgValueFormatResult FormatOptionalPairToSpan(double first, double second, bool hasSecond, SvgStandaloneValueFormatMode mode, System.Span<char> destination);
    /// <summary>Parses any extended standalone SVG or CSS value from a string.</summary>
    /// <param name="kind">The requested grammar.</param><param name="value">The authored text.</param><param name="options">The grammar flags and bounds.</param><returns>The typed value or exact failure.</returns>
    SvgExtendedValueParseResult ParseExtendedWithString(SvgExtendedValueKind kind, string? value, SvgExtendedValueParseOptions options);
    /// <summary>Parses any extended standalone SVG or CSS value from a character span.</summary>
    /// <param name="kind">The requested grammar.</param><param name="value">The authored text.</param><param name="options">The grammar flags and bounds.</param><returns>The typed value or exact failure.</returns>
    SvgExtendedValueParseResult ParseExtendedWithReadOnlySpanOfChar(SvgExtendedValueKind kind, System.ReadOnlySpan<char> value, SvgExtendedValueParseOptions options);
    /// <summary>Evaluates a typed length or percentage to SVG user units.</summary>
    /// <param name="length">The typed length.</param><param name="context">The viewport, font, and object-bounds context.</param><param name="basis">The percentage basis.</param><returns>The finite user-unit result.</returns>
    SvgLengthEvaluationResult EvaluateLength(SvgLength length, SvgLengthContext context, SvgLengthPercentageBasis basis);
    /// <summary>Converts an angle to another angle unit.</summary>
    /// <param name="angle">The source angle.</param><param name="targetUnit">The target unit.</param><returns>The converted numeric component.</returns>
    SvgUnitConversionResult ConvertAngle(SvgAngle angle, SvgAngleUnit targetUnit);
    /// <summary>Converts a time to another time unit.</summary>
    /// <param name="time">The source time.</param><param name="targetUnit">The target unit.</param><returns>The converted numeric component.</returns>
    SvgUnitConversionResult ConvertTime(SvgTime time, SvgTimeUnit targetUnit);
    /// <summary>Converts a frequency to another frequency unit.</summary>
    /// <param name="frequency">The source frequency.</param><param name="targetUnit">The target unit.</param><returns>The converted numeric component.</returns>
    SvgUnitConversionResult ConvertFrequency(SvgFrequency frequency, SvgFrequencyUnit targetUnit);
    /// <summary>Converts a resolution to another resolution unit.</summary>
    /// <param name="resolution">The source resolution.</param><param name="targetUnit">The target unit.</param><returns>The converted numeric component.</returns>
    SvgUnitConversionResult ConvertResolution(SvgResolution resolution, SvgResolutionUnit targetUnit);
    /// <summary>Evaluates a positive ratio.</summary>
    /// <param name="ratio">The positive ratio.</param><returns>The numerator divided by the denominator.</returns>
    SvgUnitConversionResult EvaluateRatio(SvgRatio ratio);
    /// <summary>Computes measured length divided by author pathLength.</summary>
    /// <param name="pathLength">The author pathLength.</param><param name="actualLength">The measured nonnegative length.</param><returns>The finite scale or stable failure.</returns>
    SvgUnitConversionResult EvaluatePathLengthScale(SvgPathLength pathLength, double actualLength);
    /// <summary>Evaluates a typed CSS math tree in canonical units.</summary>
    /// <param name="expression">The typed CSS math tree.</param><param name="context">The viewport, font, and object-bounds context.</param><param name="basis">The percentage basis.</param><returns>The finite canonical value.</returns>
    SvgCalcEvaluationResult EvaluateCalc(SvgCalcExpression expression, SvgLengthContext context, SvgLengthPercentageBasis basis);
    /// <summary>Composes an SVG transform list into one matrix.</summary>
    /// <param name="transforms">The SVG transform list.</param><returns>The finite matrix or stable failure.</returns>
    SvgTransformMatrixResult ComposeSvgTransform(SvgTransformList transforms);
    /// <summary>Composes a CSS transform list around a resolved transform origin.</summary>
    /// <param name="transforms">The CSS transform list.</param><param name="context">The length and reference-box context.</param><param name="origin">The transform origin.</param><param name="box">The selected transform box.</param><returns>The finite matrix or stable failure.</returns>
    SvgCssTransformMatrixResult ComposeCssTransform(SvgCssTransformList transforms, SvgCssTransformContext context, SvgTransformOrigin origin, SvgTransformBox box);
    /// <summary>Interpolates two evaluated CSS transform matrices.</summary>
    /// <param name="from">The starting CSS transform list.</param><param name="to">The ending CSS transform list.</param><param name="amount">The unit-interval interpolation amount.</param><param name="context">The shared length and reference-box context.</param><param name="origin">The shared transform origin.</param><param name="box">The selected transform box.</param><returns>The interpolated finite matrix.</returns>
    SvgMatrixOperationResult InterpolateCssTransform(SvgCssTransformList from, SvgCssTransformList to, double amount, SvgCssTransformContext context, SvgTransformOrigin origin, SvgTransformBox box);
    /// <summary>Fits a viewBox into a viewport using preserveAspectRatio.</summary>
    /// <param name="viewBox">The source viewBox.</param><param name="aspectRatio">The fitting policy.</param><param name="viewportX">The viewport x coordinate.</param><param name="viewportY">The viewport y coordinate.</param><param name="viewportWidth">The viewport width.</param><param name="viewportHeight">The viewport height.</param><returns>The fitting matrix.</returns>
    SvgViewBoxFitResult FitViewBox(SvgViewBox viewBox, SvgAspectRatio aspectRatio, double viewportX, double viewportY, double viewportWidth, double viewportHeight);
    /// <summary>Converts a color to another supported color space.</summary>
    /// <param name="color">The source color.</param><param name="targetSpace">The target color space.</param><returns>The converted color.</returns>
    SvgColorOperationResult ConvertColor(SvgColor color, SvgColorSpace targetSpace);
    /// <summary>Interpolates two colors in a selected color space.</summary>
    /// <param name="from">The starting color.</param><param name="to">The ending color.</param><param name="amount">The finite interpolation amount.</param><param name="space">The interpolation color space.</param><returns>The interpolated color.</returns>
    SvgColorOperationResult InterpolateColor(SvgColor from, SvgColor to, double amount, SvgColorSpace space);
    /// <summary>Evaluates and interpolates compatible CSS math trees in canonical units.</summary>
    /// <param name="from">The starting expression.</param><param name="to">The ending expression.</param><param name="amount">The unit-interval interpolation amount.</param><param name="context">The shared length context.</param><param name="basis">The shared percentage basis.</param><returns>The finite interpolated canonical value.</returns>
    SvgCalcEvaluationResult InterpolateCalc(SvgCalcExpression from, SvgCalcExpression to, double amount, SvgLengthContext context, SvgLengthPercentageBasis basis);
    /// <summary>Interpolates compatible extended typed values.</summary>
    /// <param name="from">The starting typed value.</param><param name="to">The ending typed value.</param><param name="amount">The unit-interval interpolation amount.</param><param name="options">The length, color, and bound options.</param><returns>The interpolated typed value.</returns>
    SvgExtendedValueOperationResult InterpolateExtended(SvgExtendedValue from, SvgExtendedValue to, double amount, SvgAnimationInterpolationOptions options);
    /// <summary>Serializes an extended typed value canonically.</summary>
    /// <param name="value">The typed value.</param><returns>The canonical text or stable failure.</returns>
    SvgValueFormatResult SerializeExtended(SvgExtendedValue value);
    /// <summary>Serializes an extended typed value atomically into a caller destination.</summary>
    /// <param name="value">The typed value.</param><param name="destination">The caller destination.</param><returns>The required and written counts.</returns>
    SvgValueFormatResult SerializeExtendedToSpan(SvgExtendedValue value, System.Span<char> destination);
}
