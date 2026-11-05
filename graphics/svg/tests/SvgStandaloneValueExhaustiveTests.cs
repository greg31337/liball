namespace svg.Tests;

public class SvgStandaloneValueExhaustiveTests
{
    [Test]
    public void EveryExtendedGrammarHasStringSpanParityAndRoundTrips()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions options = new SvgExtendedValueParseOptions();
        SvgExtendedValueKind[] kinds = new[]
        {
            SvgExtendedValueKind.Length, SvgExtendedValueKind.Angle, SvgExtendedValueKind.Time,
            SvgExtendedValueKind.Frequency, SvgExtendedValueKind.Resolution, SvgExtendedValueKind.Ratio,
            SvgExtendedValueKind.PathLength, SvgExtendedValueKind.ViewBox, SvgExtendedValueKind.PreserveAspectRatio,
            SvgExtendedValueKind.SvgTransform, SvgExtendedValueKind.CssTransform, SvgExtendedValueKind.TransformOrigin,
            SvgExtendedValueKind.TransformBox, SvgExtendedValueKind.Color, SvgExtendedValueKind.Paint,
            SvgExtendedValueKind.Calc, SvgExtendedValueKind.DashArray, SvgExtendedValueKind.TextPositionList,
            SvgExtendedValueKind.RotateList, SvgExtendedValueKind.FontFamilyList, SvgExtendedValueKind.PaintOrder
        };
        string[] inputs = new[]
        {
            "1Q", "1rad", "1ms", "1kHz", "1dpcm", "4 / 3", "2",
            "0 0 10 20", "defer xMaxYMin slice", "translate(1 2) rotate(30)",
            "translateX(1px) scale(2)", "left top 1px", "fill-box",
            "rgb(10 20 30 / 50%)", "url(#p) none", "calc(1px + 2px)",
            "1px, 2%", "1em 2px", "0, 45 90", "\"Open Sans\", serif",
            "stroke fill markers"
        };

        int index = 0;
        while (index < kinds.Length)
        {
            SvgExtendedValueParseResult fromString = service.ParseExtendedWithString(kinds[index], inputs[index], options);
            SvgExtendedValueParseResult fromSpan = service.ParseExtendedWithReadOnlySpanOfChar(kinds[index], inputs[index].AsSpan(), options);
            Assert.That(fromString.Success, Is.True, kinds[index] + " string parse");
            Assert.That(fromSpan.Success, Is.EqualTo(fromString.Success), kinds[index] + " span success");
            Assert.That(fromSpan.Error, Is.EqualTo(fromString.Error), kinds[index] + " span error");
            Assert.That(fromSpan.ErrorOffset, Is.EqualTo(fromString.ErrorOffset), kinds[index] + " span offset");
            Assert.That(fromSpan.Recovered, Is.EqualTo(fromString.Recovered), kinds[index] + " span recovery");

            SvgValueFormatResult formatted = service.SerializeExtended(fromString.Value!);
            Assert.That(formatted.Success, Is.True, kinds[index] + " serialization");
            char[] exact = new char[formatted.RequiredLength];
            SvgValueFormatResult copied = service.SerializeExtendedToSpan(fromString.Value!, exact.AsSpan());
            Assert.That(copied.Success, Is.True, kinds[index] + " exact destination");
            Assert.That(new string(exact), Is.EqualTo(formatted.Text), kinds[index] + " destination text");
            SvgExtendedValueParseResult reparsed = service.ParseExtendedWithString(kinds[index], formatted.Text, options);
            Assert.That(reparsed.Success, Is.True, kinds[index] + " parse-format-parse");
            Assert.That(service.SerializeExtended(reparsed.Value!).Text, Is.EqualTo(formatted.Text), kinds[index] + " canonical stability");
            index = index + 1;
        }
    }

    [Test]
    public void EveryLengthUnitParsesAndEvaluatesWithItsRequiredContext()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions options = new SvgExtendedValueParseOptions();
        SvgLengthContext context = new SvgLengthContext();
        context.HasViewport = true;
        context.ViewportWidth = 200;
        context.ViewportHeight = 100;
        context.HasFontSize = true;
        context.FontSize = 10;
        context.HasXHeight = true;
        context.XHeight = 4;
        context.HasZeroAdvance = true;
        context.ZeroAdvance = 5;
        context.HasRootFontSize = true;
        context.RootFontSize = 12;
        string[] suffixes = new[] { "", "%", "px", "em", "ex", "ch", "rem", "vw", "vh", "vmin", "vmax", "cm", "mm", "Q", "in", "pt", "pc" };
        SvgLengthUnit[] units = new[]
        {
            SvgLengthUnit.Number, SvgLengthUnit.Percentage, SvgLengthUnit.Px, SvgLengthUnit.Em,
            SvgLengthUnit.Ex, SvgLengthUnit.Ch, SvgLengthUnit.Rem, SvgLengthUnit.Vw,
            SvgLengthUnit.Vh, SvgLengthUnit.Vmin, SvgLengthUnit.Vmax, SvgLengthUnit.Cm,
            SvgLengthUnit.Mm, SvgLengthUnit.Q, SvgLengthUnit.In, SvgLengthUnit.Pt, SvgLengthUnit.Pc
        };
        double[] expected = new[]
        {
            2, 4, 2, 20, 8, 10, 24, 4, 2, 2, 4,
            192.0 / 2.54, 192.0 / 25.4, 192.0 / 101.6, 192, 192.0 / 72, 32
        };

        int index = 0;
        while (index < suffixes.Length)
        {
            SvgExtendedValueParseResult parsed = service.ParseExtendedWithString(SvgExtendedValueKind.Length, "2" + suffixes[index], options);
            Assert.That(parsed.Success, Is.True, suffixes[index]);
            Assert.That(parsed.Value!.Length!.Unit, Is.EqualTo(units[index]), suffixes[index]);
            SvgLengthEvaluationResult evaluated = service.EvaluateLength(parsed.Value.Length, context, SvgLengthPercentageBasis.ViewportWidth);
            Assert.That(evaluated.Success, Is.True, suffixes[index]);
            Assert.That(evaluated.Value, Is.EqualTo(expected[index]).Within(1e-12), suffixes[index]);
            index = index + 1;
        }
    }

    [Test]
    public void EveryScalarUnitParsesSerializesAndConvertsToCanonicalUnits()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions options = new SvgExtendedValueParseOptions();
        string[] angleTexts = new[] { "2", "2deg", "2grad", "2rad", "2turn" };
        double[] angleDegrees = new[] { 2, 2, 1.8, 360 / System.Math.PI, 720 };
        int index = 0;
        while (index < angleTexts.Length)
        {
            SvgExtendedValueParseResult parsed = service.ParseExtendedWithString(SvgExtendedValueKind.Angle, angleTexts[index], options);
            Assert.That(parsed.Success, Is.True, angleTexts[index]);
            Assert.That(service.SerializeExtended(parsed.Value!).Success, Is.True, angleTexts[index]);
            Assert.That(service.ConvertAngle(parsed.Value!.Angle!, SvgAngleUnit.Deg).Value, Is.EqualTo(angleDegrees[index]).Within(1e-12), angleTexts[index]);
            index = index + 1;
        }

        SvgExtendedValueParseResult seconds = service.ParseExtendedWithString(SvgExtendedValueKind.Time, "2s", options);
        SvgExtendedValueParseResult milliseconds = service.ParseExtendedWithString(SvgExtendedValueKind.Time, "2ms", options);
        SvgExtendedValueParseResult hertz = service.ParseExtendedWithString(SvgExtendedValueKind.Frequency, "2Hz", options);
        SvgExtendedValueParseResult kilohertz = service.ParseExtendedWithString(SvgExtendedValueKind.Frequency, "2kHz", options);
        SvgExtendedValueParseResult dpi = service.ParseExtendedWithString(SvgExtendedValueKind.Resolution, "96dpi", options);
        SvgExtendedValueParseResult dpcm = service.ParseExtendedWithString(SvgExtendedValueKind.Resolution, "37.79527559055118dpcm", options);
        SvgExtendedValueParseResult dppx = service.ParseExtendedWithString(SvgExtendedValueKind.Resolution, "1dppx", options);
        SvgExtendedValueParseResult alias = service.ParseExtendedWithString(SvgExtendedValueKind.Resolution, "1x", options);

        Assert.Multiple(() =>
        {
            Assert.That(service.ConvertTime(seconds.Value!.Time!, SvgTimeUnit.Milliseconds).Value, Is.EqualTo(2000));
            Assert.That(service.ConvertTime(milliseconds.Value!.Time!, SvgTimeUnit.Seconds).Value, Is.EqualTo(0.002));
            Assert.That(service.ConvertFrequency(hertz.Value!.Frequency!, SvgFrequencyUnit.KHz).Value, Is.EqualTo(0.002));
            Assert.That(service.ConvertFrequency(kilohertz.Value!.Frequency!, SvgFrequencyUnit.Hz).Value, Is.EqualTo(2000));
            Assert.That(service.ConvertResolution(dpi.Value!.Resolution!, SvgResolutionUnit.Dppx).Value, Is.EqualTo(1));
            Assert.That(service.ConvertResolution(dpcm.Value!.Resolution!, SvgResolutionUnit.Dppx).Value, Is.EqualTo(1).Within(1e-12));
            Assert.That(service.ConvertResolution(dppx.Value!.Resolution!, SvgResolutionUnit.Dpi).Value, Is.EqualTo(96));
            Assert.That(service.SerializeExtended(alias.Value!).Text, Is.EqualTo("1dppx"));
        });
    }

    [Test]
    public void ParsingAndFormattingAreIndependentOfCurrentCulture()
    {
        System.Globalization.CultureInfo originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            SvgValueService service = new SvgValueService();
            SvgValueNumberResult number = service.ParseNumberWithString("1234.5", new SvgValueParseOptions());
            SvgValueFormatResult compact = service.FormatNumber(1234.5, SvgStandaloneValueFormatMode.Compact);
            SvgExtendedValueParseResult length = service.ParseExtendedWithString(SvgExtendedValueKind.Length, "1.5cm", new SvgExtendedValueParseOptions());
            SvgValueFormatResult serialized = service.SerializeExtended(length.Value!);

            Assert.Multiple(() =>
            {
                Assert.That(number.Value, Is.EqualTo(1234.5));
                Assert.That(compact.Text, Is.EqualTo("1234.5"));
                Assert.That(serialized.Text, Is.EqualTo("1.5cm"));
            });
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public void FiniteExtremeInterpolationDoesNotOverflowAndPreservesEndpoints()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions parseOptions = new SvgExtendedValueParseOptions();
        SvgAnimationInterpolationOptions interpolationOptions = new SvgAnimationInterpolationOptions();
        SvgExtendedValue from = service.ParseExtendedWithString(SvgExtendedValueKind.Angle, "1.7976931348623157e308deg", parseOptions).Value!;
        SvgExtendedValue to = service.ParseExtendedWithString(SvgExtendedValueKind.Angle, "-1.7976931348623157e308deg", parseOptions).Value!;

        SvgExtendedValueOperationResult start = service.InterpolateExtended(from, to, 0, interpolationOptions);
        SvgExtendedValueOperationResult middle = service.InterpolateExtended(from, to, 0.5, interpolationOptions);
        SvgExtendedValueOperationResult end = service.InterpolateExtended(from, to, 1, interpolationOptions);

        Assert.Multiple(() =>
        {
            Assert.That(start.Success, Is.True);
            Assert.That(start.Value!.Angle!.Value, Is.EqualTo(double.MaxValue));
            Assert.That(middle.Success, Is.True);
            Assert.That(middle.Value!.Angle!.Value, Is.Zero);
            Assert.That(end.Success, Is.True);
            Assert.That(end.Value!.Angle!.Value, Is.EqualTo(-double.MaxValue));
        });
    }

    [Test]
    public void MutableInvalidExtendedPayloadsFailWithoutThrowingOrEmittingText()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions options = new SvgExtendedValueParseOptions();
        SvgExtendedValue length = service.ParseExtendedWithString(SvgExtendedValueKind.Length, "1px", options).Value!;
        SvgExtendedValue angle = service.ParseExtendedWithString(SvgExtendedValueKind.Angle, "1deg", options).Value!;
        SvgExtendedValue ratio = service.ParseExtendedWithString(SvgExtendedValueKind.Ratio, "1/2", options).Value!;
        SvgExtendedValue pathLength = service.ParseExtendedWithString(SvgExtendedValueKind.PathLength, "1", options).Value!;
        SvgExtendedValue box = service.ParseExtendedWithString(SvgExtendedValueKind.TransformBox, "view-box", options).Value!;
        SvgExtendedValue calc = service.ParseExtendedWithString(SvgExtendedValueKind.Calc, "calc(1px + 2px)", options).Value!;
        length.Length!.Value = double.NaN;
        angle.Angle!.Unit = (SvgAngleUnit)999;
        ratio.Ratio!.Numerator = 0;
        pathLength.PathLength!.Value = double.PositiveInfinity;
        box.TransformBox = (SvgTransformBox)999;
        calc.CalcExpression!.Kind = (SvgCalcExpressionKind)999;

        SvgValueFormatResult badLength = service.SerializeExtended(length);
        SvgValueFormatResult badAngle = service.SerializeExtended(angle);
        SvgValueFormatResult badRatio = service.SerializeExtended(ratio);
        SvgValueFormatResult badPath = service.SerializeExtended(pathLength);
        SvgValueFormatResult badBox = service.SerializeExtended(box);
        SvgValueFormatResult badCalc = service.SerializeExtended(calc);
        SvgExtendedValue validRatio = service.ParseExtendedWithString(SvgExtendedValueKind.Ratio, "1/2", options).Value!;
        SvgExtendedValueOperationResult interpolation = service.InterpolateExtended(ratio, validRatio, 0.5, new SvgAnimationInterpolationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(badLength.Error, Is.EqualTo(SvgValueFormatError.InvalidArgument));
            Assert.That(badAngle.Error, Is.EqualTo(SvgValueFormatError.InvalidArgument));
            Assert.That(badRatio.Error, Is.EqualTo(SvgValueFormatError.InvalidArgument));
            Assert.That(badPath.Error, Is.EqualTo(SvgValueFormatError.InvalidArgument));
            Assert.That(badBox.Error, Is.EqualTo(SvgValueFormatError.InvalidArgument));
            Assert.That(badCalc.Error, Is.EqualTo(SvgValueFormatError.InvalidArgument));
            Assert.That(interpolation.Success, Is.False);
            Assert.That(interpolation.Error, Is.EqualTo(SvgAnimationInterpolationError.IncompatibleValues));
        });
    }

    [Test]
    public void ErrorOffsetsLimitsRecoveryAndDestinationSizingRemainExact()
    {
        SvgValueService service = new SvgValueService();
        SvgExtendedValueParseOptions strict = new SvgExtendedValueParseOptions();
        SvgExtendedValueParseOptions compatible = new SvgExtendedValueParseOptions();
        compatible.RecoverInvalidItems = true;
        SvgExtendedValueParseResult exponent = service.ParseExtendedWithString(SvgExtendedValueKind.Length, "  1e+px", strict);
        SvgExtendedValueParseResult unit = service.ParseExtendedWithReadOnlySpanOfChar(SvgExtendedValueKind.Angle, "  2degrees".AsSpan(), strict);
        SvgExtendedValueParseResult strictPaint = service.ParseExtendedWithString(SvgExtendedValueKind.Paint, "url(#a) bogus", strict);
        SvgExtendedValueParseResult recoveredPaint = service.ParseExtendedWithString(SvgExtendedValueKind.Paint, "url(#a) bogus", compatible);
        SvgExtendedValueParseOptions bounded = new SvgExtendedValueParseOptions();
        bounded.MaxItems = 1;
        SvgExtendedValueParseResult limited = service.ParseExtendedWithString(SvgExtendedValueKind.DashArray, "1px 2px", bounded);
        SvgExtendedValue value = service.ParseExtendedWithString(SvgExtendedValueKind.TransformBox, "content-box", strict).Value!;
        char[] shortDestination = new[] { 'x', 'x', 'x' };
        SvgValueFormatResult shortResult = service.SerializeExtendedToSpan(value, shortDestination.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(exponent.Error, Is.EqualTo(SvgExtendedValueError.InvalidNumber));
            Assert.That(exponent.ErrorOffset, Is.EqualTo(5));
            Assert.That(unit.Error, Is.EqualTo(SvgExtendedValueError.InvalidUnitOrKeyword));
            Assert.That(unit.ErrorOffset, Is.EqualTo(3));
            Assert.That(strictPaint.Success, Is.False);
            Assert.That(recoveredPaint.Success, Is.True);
            Assert.That(recoveredPaint.Recovered, Is.True);
            Assert.That(limited.Error, Is.EqualTo(SvgExtendedValueError.LimitExceeded));
            Assert.That(limited.ErrorOffset, Is.EqualTo(4));
            Assert.That(shortResult.Error, Is.EqualTo(SvgValueFormatError.DestinationTooSmall));
            Assert.That(shortResult.RequiredLength, Is.EqualTo(11));
            Assert.That(shortResult.WrittenLength, Is.Zero);
            Assert.That(shortDestination, Is.EqualTo(new[] { 'x', 'x', 'x' }));
        });
    }
}
