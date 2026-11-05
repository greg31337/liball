namespace svg.Tests;

public class SvgExtendedValueTests
{
    [Test]
    public void ScalarUnitGrammarsParseFromStringsAndSpans()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions options = new SvgExtendedValueParseOptions();
        SvgExtendedValueParseResult length = service.ParseExtendedWithString(SvgExtendedValueKind.Length, "25%", options);
        SvgExtendedValueParseResult angle = service.ParseExtendedWithReadOnlySpanOfChar(SvgExtendedValueKind.Angle, ".5turn".AsSpan(), options);
        SvgExtendedValueParseResult time = service.ParseExtendedWithString(SvgExtendedValueKind.Time, "250ms", options);
        SvgExtendedValueParseResult frequency = service.ParseExtendedWithString(SvgExtendedValueKind.Frequency, "2kHz", options);
        SvgExtendedValueParseResult resolution = service.ParseExtendedWithString(SvgExtendedValueKind.Resolution, "96dpi", options);
        SvgExtendedValueParseResult ratio = service.ParseExtendedWithString(SvgExtendedValueKind.Ratio, "16 / 9", options);
        SvgExtendedValueParseResult pathLength = service.ParseExtendedWithString(SvgExtendedValueKind.PathLength, "12.5", options);

        Assert.Multiple(() =>
        {
            Assert.That(length.Success, Is.True);
            Assert.That(length.Value!.Length!.Unit, Is.EqualTo(SvgLengthUnit.Percentage));
            Assert.That(angle.Success, Is.True);
            Assert.That(angle.Value!.Angle!.Unit, Is.EqualTo(SvgAngleUnit.Turn));
            Assert.That(time.Value!.Time!.Value, Is.EqualTo(250));
            Assert.That(frequency.Value!.Frequency!.Unit, Is.EqualTo(SvgFrequencyUnit.KHz));
            Assert.That(resolution.Value!.Resolution!.Unit, Is.EqualTo(SvgResolutionUnit.Dpi));
            Assert.That(ratio.Value!.Ratio!.Denominator, Is.EqualTo(9));
            Assert.That(pathLength.Value!.PathLength!.Value, Is.EqualTo(12.5));
        });
    }

    [Test]
    public void StructuralAndPaintGrammarsNoLongerRequireDocumentAttributes()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions options = new SvgExtendedValueParseOptions();
        SvgExtendedValueParseResult viewBox = service.ParseExtendedWithString(SvgExtendedValueKind.ViewBox, "0 0 100 50", options);
        SvgExtendedValueParseResult aspect = service.ParseExtendedWithString(SvgExtendedValueKind.PreserveAspectRatio, "defer xMidYMax slice", options);
        SvgExtendedValueParseResult svgTransform = service.ParseExtendedWithString(SvgExtendedValueKind.SvgTransform, "translate(10 20) scale(2)", options);
        SvgExtendedValueParseResult cssTransform = service.ParseExtendedWithString(SvgExtendedValueKind.CssTransform, "translate(50%, 25%) rotate(90deg)", options);
        SvgExtendedValueParseResult origin = service.ParseExtendedWithString(SvgExtendedValueKind.TransformOrigin, "left top 2px", options);
        SvgExtendedValueParseResult box = service.ParseExtendedWithString(SvgExtendedValueKind.TransformBox, "fill-box", options);
        SvgExtendedValueParseResult color = service.ParseExtendedWithReadOnlySpanOfChar(SvgExtendedValueKind.Color, "oklch(60% 0.2 30)".AsSpan(), options);
        SvgExtendedValueParseResult paint = service.ParseExtendedWithString(SvgExtendedValueKind.Paint, "url(#paint) rgb(10 20 30)", options);

        Assert.Multiple(() =>
        {
            Assert.That(viewBox.Value!.ViewBox!.Width, Is.EqualTo(100));
            Assert.That(aspect.Value!.AspectRatio!.Scale, Is.EqualTo(SvgAspectRatioScale.Slice));
            Assert.That(svgTransform.Value!.SvgTransform!.Items.Count, Is.EqualTo(2));
            Assert.That(cssTransform.Value!.CssTransform!.Items.Count, Is.EqualTo(2));
            Assert.That(origin.Value!.TransformOrigin!.HasZ, Is.True);
            Assert.That(box.Value!.TransformBox, Is.EqualTo(SvgTransformBox.FillBox));
            Assert.That(color.Success, Is.True);
            Assert.That(color.Value!.Color!.Space, Is.EqualTo(SvgColorSpace.Oklch));
            Assert.That(paint.Success, Is.True);
            Assert.That(paint.Value!.Paint!.Kind, Is.EqualTo(SvgPaintKind.Url));
        });
    }

    [Test]
    public void ScalarEvaluationAndConversionUseExplicitContexts()
    {
        SvgValueService service = new SvgValueService();
        SvgLengthContext context = new SvgLengthContext();
        context.HasViewport = true;
        context.ViewportWidth = 200;
        context.ViewportHeight = 100;
        SvgLengthEvaluationResult length = service.EvaluateLength(new SvgLength(25, SvgLengthUnit.Percentage), context, SvgLengthPercentageBasis.ViewportWidth);
        SvgUnitConversionResult angle = service.ConvertAngle(new SvgAngle(0.5, SvgAngleUnit.Turn), SvgAngleUnit.Deg);
        SvgUnitConversionResult time = service.ConvertTime(new SvgTime(250, SvgTimeUnit.Milliseconds), SvgTimeUnit.Seconds);
        SvgUnitConversionResult frequency = service.ConvertFrequency(new SvgFrequency(2, SvgFrequencyUnit.KHz), SvgFrequencyUnit.Hz);
        SvgUnitConversionResult resolution = service.ConvertResolution(new SvgResolution(96, SvgResolutionUnit.Dpi), SvgResolutionUnit.Dppx);
        SvgUnitConversionResult ratio = service.EvaluateRatio(new SvgRatio(16, 8));
        SvgUnitConversionResult scale = service.EvaluatePathLengthScale(new SvgPathLength(12.5), 25);

        Assert.Multiple(() =>
        {
            Assert.That(length.Value, Is.EqualTo(50));
            Assert.That(angle.Value, Is.EqualTo(180).Within(1e-12));
            Assert.That(time.Value, Is.EqualTo(0.25));
            Assert.That(frequency.Value, Is.EqualTo(2000));
            Assert.That(resolution.Value, Is.EqualTo(1));
            Assert.That(ratio.Value, Is.EqualTo(2));
            Assert.That(scale.Value, Is.EqualTo(2));
        });
    }

    [Test]
    public void CalcTreesParseEvaluateAndSerializeCanonically()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions options = new SvgExtendedValueParseOptions();
        options.ExpectedCalcDimension = SvgCalcDimension.LengthPercentage;
        SvgExtendedValueParseResult parsed = service.ParseExtendedWithString(SvgExtendedValueKind.Calc, "clamp(10px, 25%, 80px)", options);
        SvgLengthContext context = new SvgLengthContext();
        context.HasViewport = true;
        context.ViewportWidth = 200;
        context.ViewportHeight = 100;
        SvgCalcEvaluationResult evaluated = service.EvaluateCalc(parsed.Value!.CalcExpression!, context, SvgLengthPercentageBasis.ViewportWidth);
        SvgExtendedValueParseResult ending = service.ParseExtendedWithString(SvgExtendedValueKind.Calc, "clamp(20px, 50%, 160px)", options);
        SvgCalcEvaluationResult interpolated = service.InterpolateCalc(parsed.Value.CalcExpression, ending.Value!.CalcExpression!, 0.5, context, SvgLengthPercentageBasis.ViewportWidth);
        SvgValueFormatResult formatted = service.SerializeExtended(parsed.Value);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(evaluated.Success, Is.True);
            Assert.That(evaluated.Dimension, Is.EqualTo(SvgCalcDimension.LengthPercentage));
            Assert.That(evaluated.Value, Is.EqualTo(50));
            Assert.That(interpolated.Success, Is.True);
            Assert.That(interpolated.Value, Is.EqualTo(75));
            Assert.That(formatted.Text, Is.EqualTo("clamp(10px, 25%, 80px)"));
        });
    }

    [Test]
    public void TransformAndViewBoxEvaluationProduceFiniteMatrices()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions options = new SvgExtendedValueParseOptions();
        SvgExtendedValueParseResult svg = service.ParseExtendedWithString(SvgExtendedValueKind.SvgTransform, "translate(10 20) scale(2)", options);
        SvgTransformMatrixResult svgMatrix = service.ComposeSvgTransform(svg.Value!.SvgTransform!);
        SvgExtendedValueParseResult css = service.ParseExtendedWithString(SvgExtendedValueKind.CssTransform, "translate(50%, 25%)", options);
        SvgCssTransformContext cssContext = new SvgCssTransformContext();
        cssContext.FillBox = new SvgCssTransformReferenceBox(true, 0, 0, 200, 100);
        SvgTransformOrigin origin = new SvgTransformOrigin(new SvgLength(0, SvgLengthUnit.Px), new SvgLength(0, SvgLengthUnit.Px), null, false);
        SvgCssTransformMatrixResult cssMatrix = service.ComposeCssTransform(css.Value!.CssTransform!, cssContext, origin, SvgTransformBox.FillBox);
        SvgExtendedValueParseResult cssStart = service.ParseExtendedWithString(SvgExtendedValueKind.CssTransform, "translate(0, 0)", options);
        SvgMatrixOperationResult cssInterpolated = service.InterpolateCssTransform(cssStart.Value!.CssTransform!, css.Value.CssTransform, 0.5, cssContext, origin, SvgTransformBox.FillBox);
        SvgViewBoxFitResult fit = service.FitViewBox(new SvgViewBox(0, 0, 100, 50), new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false), 0, 0, 200, 200);

        Assert.Multiple(() =>
        {
            Assert.That(svgMatrix.Success, Is.True);
            Assert.That(svgMatrix.Matrix!.A, Is.EqualTo(2));
            Assert.That(cssMatrix.Success, Is.True);
            Assert.That(cssMatrix.Matrix!.E, Is.EqualTo(100));
            Assert.That(cssMatrix.Matrix.F, Is.EqualTo(25));
            Assert.That(cssInterpolated.Success, Is.True);
            Assert.That(cssInterpolated.Matrix!.E, Is.EqualTo(50));
            Assert.That(cssInterpolated.Matrix.F, Is.EqualTo(12.5));
            Assert.That(fit.Success, Is.True);
            Assert.That(fit.Matrix!.A, Is.EqualTo(2));
            Assert.That(fit.Matrix.F, Is.EqualTo(50));
        });
    }

    [Test]
    public void ColorConversionAndTypedInterpolationAreStandalone()
    {
        SvgValueService service = new SvgValueService();
        SvgColor red = new SvgColor(SvgColorSpace.Srgb, 1, 0, 0, 1);
        SvgColor blue = new SvgColor(SvgColorSpace.Srgb, 0, 0, 1, 1);
        SvgColorOperationResult converted = service.ConvertColor(red, SvgColorSpace.Oklab);
        SvgColorOperationResult midpoint = service.InterpolateColor(red, blue, 0.5, SvgColorSpace.Srgb);
        SvgExtendedValueParseOptions parseOptions = new SvgExtendedValueParseOptions();
        SvgExtendedValue from = service.ParseExtendedWithString(SvgExtendedValueKind.Angle, "0deg", parseOptions).Value!;
        SvgExtendedValue to = service.ParseExtendedWithString(SvgExtendedValueKind.Angle, "1turn", parseOptions).Value!;
        SvgExtendedValueOperationResult angle = service.InterpolateExtended(from, to, 0.25, new SvgAnimationInterpolationOptions());
        SvgExtendedValue firstBox = service.ParseExtendedWithString(SvgExtendedValueKind.ViewBox, "0 0 10 20", parseOptions).Value!;
        SvgExtendedValue secondBox = service.ParseExtendedWithString(SvgExtendedValueKind.ViewBox, "10 20 30 40", parseOptions).Value!;
        SvgExtendedValueOperationResult box = service.InterpolateExtended(firstBox, secondBox, 0.5, new SvgAnimationInterpolationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(converted.Success, Is.True);
            Assert.That(converted.Color!.Space, Is.EqualTo(SvgColorSpace.Oklab));
            Assert.That(midpoint.Color!.Red, Is.EqualTo(0.5).Within(1e-12));
            Assert.That(midpoint.Color.Blue, Is.EqualTo(0.5).Within(1e-12));
            Assert.That(angle.Success, Is.True);
            Assert.That(angle.Value!.Angle!.Value, Is.EqualTo(90).Within(1e-12));
            Assert.That(box.Success, Is.True);
            Assert.That(box.Value!.ViewBox!.X, Is.EqualTo(5));
            Assert.That(box.Value.ViewBox.Width, Is.EqualTo(20));
        });
    }

    [Test]
    public void SpecificListsParseAndSerializeThroughTheValueService()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions options = new SvgExtendedValueParseOptions();
        SvgExtendedValueParseResult dash = service.ParseExtendedWithString(SvgExtendedValueKind.DashArray, "2px, 3%", options);
        SvgExtendedValueParseResult positions = service.ParseExtendedWithString(SvgExtendedValueKind.TextPositionList, "1em 2px", options);
        SvgExtendedValueParseResult rotate = service.ParseExtendedWithString(SvgExtendedValueKind.RotateList, "0, 45 90", options);
        SvgExtendedValueParseResult families = service.ParseExtendedWithString(SvgExtendedValueKind.FontFamilyList, "\"Open Sans\", serif", options);
        SvgExtendedValueParseResult order = service.ParseExtendedWithString(SvgExtendedValueKind.PaintOrder, "stroke fill markers", options);

        Assert.Multiple(() =>
        {
            Assert.That(dash.Success, Is.True);
            Assert.That(dash.Value!.LengthList!.Value!.Items.Count, Is.EqualTo(2));
            Assert.That(positions.Value!.LengthList!.Value!.Items.Count, Is.EqualTo(2));
            Assert.That(rotate.Value!.RotateList!.Items.Count, Is.EqualTo(3));
            Assert.That(families.Value!.FontFamilyList!.Families[0].Name, Is.EqualTo("Open Sans"));
            Assert.That(order.Value!.PaintOrder!.Items[0], Is.EqualTo(SvgPaintOrderComponent.Stroke));
            Assert.That(service.SerializeExtended(dash.Value).Text, Is.EqualTo("2px 3%"));
            Assert.That(service.SerializeExtended(rotate.Value).Text, Is.EqualTo("0 45 90"));
            Assert.That(service.SerializeExtended(families.Value).Text, Is.EqualTo("\"Open Sans\", serif"));
            Assert.That(service.SerializeExtended(order.Value).Text, Is.EqualTo("stroke fill markers"));
        });
    }

    [Test]
    public void ExtendedSerializationSupportsAtomicSpanDestinations()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions options = new SvgExtendedValueParseOptions();
        SvgExtendedValue value = service.ParseExtendedWithString(SvgExtendedValueKind.PreserveAspectRatio, "xMidYMid meet", options).Value!;
        char[] tooSmall = new[] { 'x', 'x' };
        SvgValueFormatResult failed = service.SerializeExtendedToSpan(value, tooSmall.AsSpan());
        char[] destination = new char[13];
        SvgValueFormatResult written = service.SerializeExtendedToSpan(value, destination.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(failed.Error, Is.EqualTo(SvgValueFormatError.DestinationTooSmall));
            Assert.That(failed.RequiredLength, Is.EqualTo(13));
            Assert.That(tooSmall, Is.EqualTo(new[] { 'x', 'x' }));
            Assert.That(written.Success, Is.True);
            Assert.That(new string(destination), Is.EqualTo("xMidYMid meet"));
        });
    }

    [Test]
    public void ExtendedFailuresPreserveExactOffsetsAndControlledRecovery()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions strict = new SvgExtendedValueParseOptions();
        SvgExtendedValueParseResult unit = service.ParseExtendedWithString(SvgExtendedValueKind.Length, "12wat", strict);
        SvgExtendedValueParseResult viewBox = service.ParseExtendedWithString(SvgExtendedValueKind.ViewBox, "0 0 -1 2", strict);
        SvgExtendedValueParseResult strictTransform = service.ParseExtendedWithString(SvgExtendedValueKind.CssTransform, "translate(10px) bogus(1) scale(2)", strict);
        SvgExtendedValueParseOptions compatible = new SvgExtendedValueParseOptions();
        compatible.RecoverInvalidItems = true;
        SvgExtendedValueParseResult recovered = service.ParseExtendedWithString(SvgExtendedValueKind.CssTransform, "translate(10px) bogus(1) scale(2)", compatible);
        SvgExtendedValueParseResult invalidKind = service.ParseExtendedWithString((SvgExtendedValueKind)999, "1", strict);
        SvgExtendedValueParseResult svgDecimal = service.ParseExtendedWithString(SvgExtendedValueKind.Length, "1.px", strict);
        SvgExtendedValueParseOptions cssOptions = new SvgExtendedValueParseOptions();
        cssOptions.UseCssNumberGrammar = true;
        SvgExtendedValueParseResult cssDecimal = service.ParseExtendedWithString(SvgExtendedValueKind.Length, "1.px", cssOptions);

        Assert.Multiple(() =>
        {
            Assert.That(unit.Error, Is.EqualTo(SvgExtendedValueError.InvalidUnitOrKeyword));
            Assert.That(unit.ErrorOffset, Is.EqualTo(2));
            Assert.That(viewBox.Error, Is.EqualTo(SvgExtendedValueError.OutOfRange));
            Assert.That(viewBox.ErrorOffset, Is.EqualTo(4));
            Assert.That(strictTransform.Success, Is.False);
            Assert.That(strictTransform.ErrorOffset, Is.EqualTo(16));
            Assert.That(recovered.Success, Is.True);
            Assert.That(recovered.Recovered, Is.True);
            Assert.That(recovered.Value!.CssTransform!.Items.Count, Is.EqualTo(2));
            Assert.That(invalidKind.Error, Is.EqualTo(SvgExtendedValueError.InvalidArgument));
            Assert.That(svgDecimal.Success, Is.True);
            Assert.That(cssDecimal.Success, Is.False);
        });
    }
}
