namespace svg.Tests;

public class SvgAnimationInterpolationTests
{
    [Test]
    public void ParsesAndFormatsEveryTypedAnimationValueFamily()
    {
        SvgAnimationValueParseResult number = Parse(SvgAnimationValueKind.Number, "2.5");
        SvgAnimationValueParseResult length = Parse(SvgAnimationValueKind.Length, "25%");
        SvgAnimationValueParseResult color = Parse(SvgAnimationValueKind.Color, "#ff0000");
        SvgAnimationValueParseResult path = Parse(SvgAnimationValueKind.Path, "M0 0h10v5z");
        SvgAnimationValueParseResult points = Parse(SvgAnimationValueKind.Points, "0,0 10,5");
        SvgAnimationValueParseResult transform = Parse(SvgAnimationValueKind.Transform, "translate(2,3) scale(2)");
        SvgAnimationValueParseResult viewBox = Parse(SvgAnimationValueKind.ViewBox, "0 1 20 10");
        SvgAnimationValueParseResult paint = Parse(SvgAnimationValueKind.Paint, "url(#g) red");
        SvgAnimationValueParseResult text = Parse(SvgAnimationValueKind.String, "visible");

        Assert.Multiple(() =>
        {
            Assert.That(new[] { number, length, color, path, points, transform, viewBox, paint, text }.All(item => item.Success), Is.True);
            Assert.That(number.Value.ToText(), Is.EqualTo("2.5"));
            Assert.That(length.Value.ToText(), Is.EqualTo("25%"));
            Assert.That(color.Value.Color.Red, Is.EqualTo(1));
            Assert.That(path.Value.Path.Commands.Select(item => item.Kind), Is.EqualTo(new[] { SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.ClosePath }));
            Assert.That(points.Value.ToText(), Is.EqualTo("0,0 10,5"));
            Assert.That(transform.Value.ToText(), Is.EqualTo("translate(2 3) scale(2)"));
            Assert.That(viewBox.Value.ToText(), Is.EqualTo("0 1 20 10"));
            Assert.That(paint.Value.Paint.Kind, Is.EqualTo(SvgPaintKind.Url));
            Assert.That(text.Value.ToText(), Is.EqualTo("visible"));
        });
    }

    [Test]
    public void NumberInterpolationSupportsDiscreteLinearAndSplineModes()
    {
        SvgAnimationValue from = SvgAnimationValue.FromNumber(0);
        SvgAnimationValue to = SvgAnimationValue.FromNumber(10);
        SvgAnimationInterpolationOptions options = new SvgAnimationInterpolationOptions();
        options.Spline = new SvgKeySpline(0.25, 0.1, 0.25, 1, 0, 1);

        SvgAnimationInterpolationResult earlyDiscrete = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(from, to, 0.49, SvgAnimationCalcMode.Discrete);
        SvgAnimationInterpolationResult lateDiscrete = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(from, to, 0.5, SvgAnimationCalcMode.Discrete);
        SvgAnimationInterpolationResult linear = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(from, to, 0.25, SvgAnimationCalcMode.Linear);
        SvgAnimationInterpolationResult spline = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndModeAndOptions(from, to, 0.5, SvgAnimationCalcMode.Spline, options);

        Assert.Multiple(() =>
        {
            Assert.That(earlyDiscrete.Value.Number, Is.EqualTo(0));
            Assert.That(lateDiscrete.Value.Number, Is.EqualTo(10));
            Assert.That(linear.Value.Number, Is.EqualTo(2.5));
            Assert.That(spline.Success, Is.True);
            Assert.That(spline.EffectiveProgress, Is.GreaterThan(0.75).And.LessThan(0.85));
            Assert.That(spline.Value.Number, Is.EqualTo(spline.EffectiveProgress * 10).Within(0.000001));
        });
    }

    [Test]
    public void LengthInterpolationPreservesMatchingUnitsAndResolvesMixedUnits()
    {
        SvgAnimationValue percentA = Parse(SvgAnimationValueKind.Length, "10%").Value;
        SvgAnimationValue percentB = Parse(SvgAnimationValueKind.Length, "30%").Value;
        SvgAnimationValue pixels = Parse(SvgAnimationValueKind.Length, "0px").Value;
        SvgAnimationValue centimeters = Parse(SvgAnimationValueKind.Length, "2.54cm").Value;
        SvgAnimationInterpolationOptions options = new SvgAnimationInterpolationOptions();

        SvgAnimationInterpolationResult matching = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndModeAndOptions(percentA, percentB, 0.5, SvgAnimationCalcMode.Linear, options);
        SvgAnimationInterpolationResult absolute = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndModeAndOptions(pixels, centimeters, 0.5, SvgAnimationCalcMode.Linear, options);

        options.LengthContext.HasViewport = true;
        options.LengthContext.ViewportWidth = 200;
        options.LengthContext.ViewportHeight = 100;
        options.LengthPercentageBasis = SvgLengthPercentageBasis.ViewportWidth;
        SvgAnimationInterpolationResult resolvedPercent = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndModeAndOptions(pixels, Parse(SvgAnimationValueKind.Length, "50%").Value, 0.5, SvgAnimationCalcMode.Linear, options);

        Assert.Multiple(() =>
        {
            Assert.That(matching.Value.Length.Unit, Is.EqualTo(SvgLengthUnit.Percentage));
            Assert.That(matching.Value.Length.Value, Is.EqualTo(20));
            Assert.That(absolute.Success, Is.True);
            Assert.That(absolute.Value.Length.Unit, Is.EqualTo(SvgLengthUnit.Number));
            Assert.That(absolute.Value.Length.Value, Is.EqualTo(48).Within(0.000001));
            Assert.That(resolvedPercent.Value.Length.Value, Is.EqualTo(50));
        });
    }

    [Test]
    public void ColorsAndColorPaintsUsePremultipliedInterpolation()
    {
        SvgAnimationValue transparentRed = Parse(SvgAnimationValueKind.Color, "rgb(255 0 0 / 0)").Value;
        SvgAnimationValue blue = Parse(SvgAnimationValueKind.Color, "blue").Value;
        SvgAnimationValue redPaint = Parse(SvgAnimationValueKind.Paint, "red").Value;
        SvgAnimationValue bluePaint = Parse(SvgAnimationValueKind.Paint, "blue").Value;

        SvgAnimationInterpolationResult color = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(transparentRed, blue, 0.5, SvgAnimationCalcMode.Linear);
        SvgAnimationInterpolationResult paint = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(redPaint, bluePaint, 0.5, SvgAnimationCalcMode.Linear);

        Assert.Multiple(() =>
        {
            Assert.That(color.Success, Is.True);
            Assert.That(color.Value.Color.Red, Is.EqualTo(0).Within(0.000001));
            Assert.That(color.Value.Color.Blue, Is.EqualTo(1).Within(0.000001));
            Assert.That(color.Value.Color.Alpha, Is.EqualTo(0.5));
            Assert.That(paint.Success, Is.True);
            Assert.That(paint.Value.Paint.Kind, Is.EqualTo(SvgPaintKind.Color));
            Assert.That(paint.Value.Paint.Color.Red, Is.EqualTo(0.5).Within(0.000001));
            Assert.That(paint.Value.Paint.Color.Blue, Is.EqualTo(0.5).Within(0.000001));
        });
    }

    [Test]
    public void PointsTransformsAndViewBoxesInterpolateComponentWise()
    {
        SvgAnimationValue pointsA = Parse(SvgAnimationValueKind.Points, "0,0 10,20").Value;
        SvgAnimationValue pointsB = Parse(SvgAnimationValueKind.Points, "10,20 30,40").Value;
        SvgAnimationValue transformA = Parse(SvgAnimationValueKind.Transform, "translate(10) scale(2)").Value;
        SvgAnimationValue transformB = Parse(SvgAnimationValueKind.Transform, "translate(20 40) scale(4 8)").Value;
        SvgAnimationValue viewBoxA = Parse(SvgAnimationValueKind.ViewBox, "0 0 10 20").Value;
        SvgAnimationValue viewBoxB = Parse(SvgAnimationValueKind.ViewBox, "10 20 30 40").Value;

        SvgAnimationInterpolationResult points = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(pointsA, pointsB, 0.5, SvgAnimationCalcMode.Linear);
        SvgAnimationInterpolationResult transform = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(transformA, transformB, 0.5, SvgAnimationCalcMode.Linear);
        SvgAnimationInterpolationResult viewBox = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(viewBoxA, viewBoxB, 0.5, SvgAnimationCalcMode.Linear);

        Assert.Multiple(() =>
        {
            Assert.That(points.Value.ToText(), Is.EqualTo("5,10 20,30"));
            Assert.That(transform.Value.ToText(), Is.EqualTo("translate(15 20) scale(3 5)"));
            Assert.That(viewBox.Value.ToText(), Is.EqualTo("5 10 20 30"));
            Assert.That(points.Distance, Is.GreaterThan(0));
            Assert.That(transform.Distance, Is.GreaterThan(0));
            Assert.That(viewBox.Distance, Is.GreaterThan(0));
        });
    }

    [Test]
    public void CompatiblePathsMorphAndIncompatiblePathsFailStably()
    {
        SvgAnimationValue first = Parse(SvgAnimationValueKind.Path, "M0 0 L10 0 C10 0 10 10 20 10 Z").Value;
        SvgAnimationValue second = Parse(SvgAnimationValueKind.Path, "M10 10 L20 20 C20 20 30 30 40 40 Z").Value;
        SvgAnimationValue incompatible = Parse(SvgAnimationValueKind.Path, "M0 0 Q10 10 20 0").Value;

        SvgAnimationInterpolationResult result = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(first, second, 0.5, SvgAnimationCalcMode.Linear);
        SvgAnimationInterpolationResult failure = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(first, incompatible, 0.5, SvgAnimationCalcMode.Linear);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value.Path.Commands[0].Values, Is.EqualTo(new[] { 5.0, 5.0 }));
            Assert.That(result.Value.Path.Commands[1].Values, Is.EqualTo(new[] { 15.0, 10.0 }));
            Assert.That(result.Value.Path.Commands[2].Values, Is.EqualTo(new[] { 15.0, 10.0, 20.0, 20.0, 30.0, 25.0 }));
            Assert.That(result.Value.Path.Commands.Last().Kind, Is.EqualTo(SvgPathCommandKind.ClosePath));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Error, Is.EqualTo(SvgAnimationInterpolationError.IncompatibleValues));
        });
    }

    [Test]
    public void PacedSequencesAllocateTimeByTypedDistance()
    {
        List<SvgAnimationValue> values = new()
        {
            SvgAnimationValue.FromNumber(0),
            SvgAnimationValue.FromNumber(10),
            SvgAnimationValue.FromNumber(30)
        };

        SvgAnimationInterpolationResult quarter = new SvgAnimationInterpolator().InterpolateSequence(values, 0.25, SvgAnimationCalcMode.Paced, new List<SvgKeySpline>(), new SvgAnimationInterpolationOptions());
        SvgAnimationInterpolationResult half = new SvgAnimationInterpolator().InterpolateSequence(values, 0.5, SvgAnimationCalcMode.Paced, new List<SvgKeySpline>(), new SvgAnimationInterpolationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(quarter.Success, Is.True);
            Assert.That(quarter.SegmentIndex, Is.EqualTo(0));
            Assert.That(quarter.Value.Number, Is.EqualTo(7.5));
            Assert.That(half.SegmentIndex, Is.EqualTo(1));
            Assert.That(half.EffectiveProgress, Is.EqualTo(0.25));
            Assert.That(half.Value.Number, Is.EqualTo(15));
        });
    }

    [Test]
    public void SequenceModesUseDiscreteSlotsAndPerSegmentSplines()
    {
        List<SvgAnimationValue> strings = new()
        {
            SvgAnimationValue.FromString("a"),
            SvgAnimationValue.FromString("b"),
            SvgAnimationValue.FromString("c")
        };
        List<SvgAnimationValue> numbers = new()
        {
            SvgAnimationValue.FromNumber(0),
            SvgAnimationValue.FromNumber(10),
            SvgAnimationValue.FromNumber(20)
        };
        List<SvgKeySpline> splines = new()
        {
            new SvgKeySpline(0, 0, 1, 1, 0, 1),
            new SvgKeySpline(0, 0, 1, 1, 0, 1)
        };

        SvgAnimationInterpolationResult discrete = new SvgAnimationInterpolator().InterpolateSequence(strings, 0.34, SvgAnimationCalcMode.Discrete, new List<SvgKeySpline>(), new SvgAnimationInterpolationOptions());
        SvgAnimationInterpolationResult spline = new SvgAnimationInterpolator().InterpolateSequence(numbers, 0.75, SvgAnimationCalcMode.Spline, splines, new SvgAnimationInterpolationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(discrete.Value.Text, Is.EqualTo("b"));
            Assert.That(discrete.SegmentIndex, Is.EqualTo(1));
            Assert.That(spline.Success, Is.True);
            Assert.That(spline.SegmentIndex, Is.EqualTo(1));
            Assert.That(spline.Value.Number, Is.EqualTo(15).Within(0.00001));
        });
    }

    [Test]
    public void NonColorPaintAndStringValuesRemainDiscrete()
    {
        SvgAnimationValue none = Parse(SvgAnimationValueKind.Paint, "none").Value;
        SvgAnimationValue context = Parse(SvgAnimationValueKind.Paint, "context-fill").Value;
        SvgAnimationValue hidden = SvgAnimationValue.FromString("hidden");
        SvgAnimationValue visible = SvgAnimationValue.FromString("visible");

        SvgAnimationInterpolationResult earlyPaint = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(none, context, 0.25, SvgAnimationCalcMode.Linear);
        SvgAnimationInterpolationResult latePaint = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(none, context, 0.75, SvgAnimationCalcMode.Linear);
        SvgAnimationInterpolationResult stringValue = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(hidden, visible, 0.75, SvgAnimationCalcMode.Linear);
        SvgAnimationInterpolationResult pacedString = new SvgAnimationInterpolator().Distance(hidden, visible, new SvgAnimationInterpolationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(earlyPaint.Value.Paint.Kind, Is.EqualTo(SvgPaintKind.None));
            Assert.That(latePaint.Value.Paint.Kind, Is.EqualTo(SvgPaintKind.ContextFill));
            Assert.That(stringValue.Value.Text, Is.EqualTo("visible"));
            Assert.That(pacedString.Success, Is.False);
            Assert.That(pacedString.Error, Is.EqualTo(SvgAnimationInterpolationError.UnsupportedInterpolation));
        });
    }

    [Test]
    public void InvalidValuesStructuresSplinesAndLimitsReturnStableFailures()
    {
        SvgAnimationInterpolationOptions limited = new SvgAnimationInterpolationOptions();
        limited.MaxItems = 1;
        limited.MaxTokenCharacters = 3;
        SvgAnimationValueParseResult token = new SvgAnimationInterpolator().ParseWithKindAndTextAndOptions(SvgAnimationValueKind.Number, "1234", limited);
        SvgAnimationValueParseResult points = new SvgAnimationInterpolator().ParseWithKindAndTextAndOptions(SvgAnimationValueKind.Points, "0,0 1,1", limited);
        SvgAnimationInterpolationResult pointMismatch = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(Parse(SvgAnimationValueKind.Points, "0,0").Value, Parse(SvgAnimationValueKind.Points, "0,0 1,1").Value, 0.5, SvgAnimationCalcMode.Linear);
        SvgAnimationInterpolationResult missingSpline = new SvgAnimationInterpolator().InterpolateWithFromAndToAndProgressAndMode(SvgAnimationValue.FromNumber(0), SvgAnimationValue.FromNumber(1), 0.5, SvgAnimationCalcMode.Spline);

        Assert.Multiple(() =>
        {
            Assert.That(token.Error, Is.EqualTo(SvgAnimationInterpolationError.LimitExceeded));
            Assert.That(points.Error, Is.EqualTo(SvgAnimationInterpolationError.LimitExceeded));
            Assert.That(pointMismatch.Error, Is.EqualTo(SvgAnimationInterpolationError.IncompatibleValues));
            Assert.That(missingSpline.Error, Is.EqualTo(SvgAnimationInterpolationError.InvalidSplines));
        });
    }

    static SvgAnimationValueParseResult Parse(SvgAnimationValueKind kind, string text)
    {
        SvgAnimationValueParseResult result = new SvgAnimationInterpolator().ParseWithKindAndText(kind, text);
        Assert.That(result.Success, Is.True, kind + ": " + result.Error + " at " + result.ErrorOffset);
        return result;
    }
}
