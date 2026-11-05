namespace odf.Tests;

public class OdfUnitAndGeometryTests
{
    [TestCase("-1.5mm", -1.5, OdfUnitKind.Millimetre, "-1.5mm")]
    [TestCase("001.250cm", 1.25, OdfUnitKind.Centimetre, "1.25cm")]
    [TestCase("2in", 2.0, OdfUnitKind.Inch, "2in")]
    [TestCase("12pt", 12.0, OdfUnitKind.Point, "12pt")]
    [TestCase("3pc", 3.0, OdfUnitKind.Pica, "3pc")]
    [TestCase("96px", 96.0, OdfUnitKind.Pixel, "96px")]
    public void LengthsPreserveTheirUnitsAndFormatCanonically(
        string lexical,
        double expected,
        OdfUnitKind unit,
        string canonical)
    {
        OdfUnitValue result = OdfLexicalParser.ParseLength(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.NumericValue, Is.EqualTo(expected));
            Assert.That(result.UnitKind, Is.EqualTo(unit));
            Assert.That(result.LexicalValue, Is.EqualTo(lexical));
            Assert.That(OdfLexicalFormatter.FormatMeasurement(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("1")]
    [TestCase("+1cm")]
    [TestCase("1.2.3cm")]
    [TestCase("1e2cm")]
    [TestCase("1CM")]
    [TestCase("1 cm")]
    [TestCase("1em")]
    [TestCase("10%")]
    public void InvalidOrNonLengthValuesArePreserved(string? lexical)
    {
        OdfUnitValue result = OdfLexicalParser.ParseLength(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.LexicalValue, Is.EqualTo(lexical ?? ""));
            Assert.That(OdfLexicalFormatter.FormatMeasurement(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("0cm", true, false)]
    [TestCase(".0px", true, false)]
    [TestCase("0.01pt", true, true)]
    [TestCase("-0cm", false, false)]
    [TestCase("-1mm", false, false)]
    public void RestrictedLengthFormsEnforceTheirSignRules(
        string lexical,
        bool nonNegative,
        bool positive)
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfLexicalParser.ParseNonNegativeLength(lexical).IsValid, Is.EqualTo(nonNegative));
            Assert.That(OdfLexicalParser.ParsePositiveLength(lexical).IsValid, Is.EqualTo(positive));
        });
    }

    [TestCase("0px", true)]
    [TestCase("12.5px", true)]
    [TestCase("-1px", false)]
    [TestCase("1pt", false)]
    [TestCase("1PX", false)]
    public void PixelLengthsRequireNonNegativePixelUnits(string lexical, bool valid)
    {
        OdfUnitValue result = OdfLexicalParser.ParsePixelLength(lexical);

        Assert.That(result.IsValid, Is.EqualTo(valid));
    }

    [TestCase("2em", 2.0, OdfUnitKind.Em)]
    [TestCase("1.5ex", 1.5, OdfUnitKind.Ex)]
    [TestCase("-25%", -25.0, OdfUnitKind.Percentage)]
    public void GenericMeasurementsRetainRelativeUnits(string lexical, double expected, OdfUnitKind unit)
    {
        OdfUnitValue result = OdfLexicalParser.ParseMeasurement(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.NumericValue, Is.EqualTo(expected));
            Assert.That(result.UnitKind, Is.EqualTo(unit));
            Assert.That(OdfLexicalFormatter.FormatMeasurement(result), Is.EqualTo(lexical));
        });
    }

    [Test]
    public void CoordinatesUseLengthSyntaxAndPreserveUnits()
    {
        OdfUnitValue coordinate = OdfLexicalParser.ParseCoordinate("-2.50cm");

        Assert.Multiple(() =>
        {
            Assert.That(coordinate.IsValid, Is.True);
            Assert.That(coordinate.NumericValue, Is.EqualTo(-2.5));
            Assert.That(coordinate.UnitKind, Is.EqualTo(OdfUnitKind.Centimetre));
            Assert.That(OdfLexicalFormatter.FormatMeasurement(coordinate), Is.EqualTo("-2.5cm"));
            Assert.That(OdfLexicalParser.ParseCoordinate("12").IsValid, Is.False);
        });
    }

    [TestCase("45", 45.0, OdfUnitKind.Unitless, "45")]
    [TestCase("+90deg", 90.0, OdfUnitKind.Degree, "90deg")]
    [TestCase("-1.5rad", -1.5, OdfUnitKind.Radian, "-1.5rad")]
    [TestCase("200grad", 200.0, OdfUnitKind.Grad, "200grad")]
    [TestCase("1e2deg", 100.0, OdfUnitKind.Degree, "100deg")]
    public void AnglesSupportUnitlessAndExplicitUnits(
        string lexical,
        double expected,
        OdfUnitKind unit,
        string canonical)
    {
        OdfUnitValue result = OdfLexicalParser.ParseAngle(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.NumericValue, Is.EqualTo(expected));
            Assert.That(result.UnitKind, Is.EqualTo(unit));
            Assert.That(OdfLexicalFormatter.FormatAngle(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("INF")]
    [TestCase("NaNrad")]
    [TestCase("1degree")]
    [TestCase("1 deg")]
    [TestCase("1turn")]
    public void InvalidAnglesArePreserved(string? lexical)
    {
        OdfUnitValue result = OdfLexicalParser.ParseAngle(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfLexicalFormatter.FormatAngle(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("0,0", 0L, 0L, "0,0")]
    [TestCase("-12,0007", -12L, 7L, "-12,7")]
    [TestCase("9223372036854775807,-9223372036854775808", long.MaxValue, long.MinValue, "9223372036854775807,-9223372036854775808")]
    public void PointsParseIntegerCoordinatePairs(
        string lexical,
        long x,
        long y,
        string canonical)
    {
        OdfPointValue result = OdfLexicalParser.ParsePoint(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.X, Is.EqualTo(x));
            Assert.That(result.Y, Is.EqualTo(y));
            Assert.That(OdfLexicalFormatter.FormatPoint(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("1")]
    [TestCase("1,2,3")]
    [TestCase("1, 2")]
    [TestCase("+1,2")]
    [TestCase("1.0,2")]
    [TestCase("9223372036854775808,0")]
    public void InvalidPointsArePreserved(string? lexical)
    {
        OdfPointValue result = OdfLexicalParser.ParsePoint(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfLexicalFormatter.FormatPoint(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("(1 2 3)", 1.0, 2.0, 3.0, "(1 2 3)")]
    [TestCase("( 1.0\t-2\n.5 )", 1.0, -2.0, 0.5, "(1 -2 0.5)")]
    public void VectorsParseThreeWhitespaceSeparatedComponents(
        string lexical,
        double x,
        double y,
        double z,
        string canonical)
    {
        OdfVectorValue result = OdfLexicalParser.ParseVector(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.X, Is.EqualTo(x));
            Assert.That(result.Y, Is.EqualTo(y));
            Assert.That(result.Z, Is.EqualTo(z));
            Assert.That(OdfLexicalFormatter.FormatVector(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("1 2 3")]
    [TestCase("(1 2)")]
    [TestCase("(1 2 3 4)")]
    [TestCase("(+1 2 3)")]
    [TestCase("(1,2,3)")]
    [TestCase("(1e0 2 3)")]
    public void InvalidVectorsArePreserved(string? lexical)
    {
        OdfVectorValue result = OdfLexicalParser.ParseVector(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfLexicalFormatter.FormatVector(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("0 0 1000 500", 0L, 0L, 1000L, 500L, "0 0 1000 500")]
    [TestCase(" -10\t+20 00100\n200 ", -10L, 20L, 100L, 200L, "-10 20 100 200")]
    public void ViewBoxesParseFourIntegerComponents(
        string lexical,
        long left,
        long top,
        long width,
        long height,
        string canonical)
    {
        OdfViewBoxValue result = OdfLexicalParser.ParseViewBox(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Left, Is.EqualTo(left));
            Assert.That(result.Top, Is.EqualTo(top));
            Assert.That(result.Width, Is.EqualTo(width));
            Assert.That(result.Height, Is.EqualTo(height));
            Assert.That(OdfLexicalFormatter.FormatViewBox(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("0 0 100")]
    [TestCase("0 0 100 100 1")]
    [TestCase("0,0,100,100")]
    [TestCase("0 0 1.5 2")]
    [TestCase("0 0 9223372036854775808 2")]
    public void InvalidViewBoxesArePreserved(string? lexical)
    {
        OdfViewBoxValue result = OdfLexicalParser.ParseViewBox(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfLexicalFormatter.FormatViewBox(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [Test]
    public void PhysicalLengthConversionsUseExactStandardRatios()
    {
        OdfUnitValue centimetres = OdfLexicalParser.ParseLength("2.54cm");
        OdfUnitValue millimetres = OdfLexicalParser.ParseLength("25.4mm");
        OdfUnitValue picas = OdfLexicalParser.ParseLength("6pc");

        OdfUnitValue centimetresInPoints = OdfUnitConverter.ConvertLength(centimetres, OdfUnitKind.Point, null);
        OdfUnitValue millimetresInInches = OdfUnitConverter.ConvertLength(millimetres, OdfUnitKind.Inch, null);
        OdfUnitValue picasInPoints = OdfUnitConverter.ConvertLength(picas, OdfUnitKind.Point, null);

        Assert.Multiple(() =>
        {
            Assert.That(centimetresInPoints.IsValid, Is.True);
            Assert.That(centimetresInPoints.NumericValue, Is.EqualTo(72).Within(1e-12));
            Assert.That(millimetresInInches.NumericValue, Is.EqualTo(1).Within(1e-12));
            Assert.That(picasInPoints.NumericValue, Is.EqualTo(72).Within(1e-12));
            Assert.That(centimetres.UnitKind, Is.EqualTo(OdfUnitKind.Centimetre));
            Assert.That(centimetres.LexicalValue, Is.EqualTo("2.54cm"));
        });
    }

    [Test]
    public void ContextualLengthConversionsRequireExplicitScales()
    {
        OdfUnitConversionContext context = new OdfUnitConversionContext
        {
            PixelsPerInch = 96,
            EmSizeInPoints = 12,
            ExSizeInPoints = 6,
            PercentageBasisInPoints = 200
        };

        OdfUnitValue pixels = OdfUnitConverter.ConvertLength(
            OdfLexicalParser.ParseMeasurement("96px"), OdfUnitKind.Point, context);
        OdfUnitValue ems = OdfUnitConverter.ConvertLength(
            OdfLexicalParser.ParseMeasurement("2em"), OdfUnitKind.Point, context);
        OdfUnitValue exs = OdfUnitConverter.ConvertLength(
            OdfLexicalParser.ParseMeasurement("2ex"), OdfUnitKind.Point, context);
        OdfUnitValue percentage = OdfUnitConverter.ConvertLength(
            OdfLexicalParser.ParseMeasurement("50%"), OdfUnitKind.Point, context);
        OdfUnitValue pointsToPixels = OdfUnitConverter.ConvertLength(
            OdfLexicalParser.ParseLength("72pt"), OdfUnitKind.Pixel, context);

        Assert.Multiple(() =>
        {
            Assert.That(pixels.NumericValue, Is.EqualTo(72).Within(1e-12));
            Assert.That(ems.NumericValue, Is.EqualTo(24).Within(1e-12));
            Assert.That(exs.NumericValue, Is.EqualTo(12).Within(1e-12));
            Assert.That(percentage.NumericValue, Is.EqualTo(100).Within(1e-12));
            Assert.That(pointsToPixels.NumericValue, Is.EqualTo(96).Within(1e-12));
            Assert.That(OdfLexicalFormatter.FormatMeasurement(pointsToPixels), Is.EqualTo("96px"));
        });
    }

    [Test]
    public void MissingOrInvalidConversionContextProducesPreservedInvalidResult()
    {
        OdfUnitValue pixels = OdfLexicalParser.ParseMeasurement("96px");
        OdfUnitConversionContext invalidContext = new OdfUnitConversionContext
        {
            PixelsPerInch = 0
        };

        OdfUnitValue missing = OdfUnitConverter.ConvertLength(pixels, OdfUnitKind.Point, null);
        OdfUnitValue invalid = OdfUnitConverter.ConvertLength(pixels, OdfUnitKind.Point, invalidContext);
        OdfUnitValue unsupportedTarget = OdfUnitConverter.ConvertLength(
            OdfLexicalParser.ParseLength("1cm"), OdfUnitKind.Degree, null);

        Assert.Multiple(() =>
        {
            Assert.That(missing.IsValid, Is.False);
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(unsupportedTarget.IsValid, Is.False);
            Assert.That(OdfLexicalFormatter.FormatMeasurement(missing), Is.EqualTo("96px"));
            Assert.That(OdfLexicalFormatter.FormatMeasurement(invalid), Is.EqualTo("96px"));
            Assert.That(OdfLexicalFormatter.FormatMeasurement(unsupportedTarget), Is.EqualTo("1cm"));
        });
    }

    [Test]
    public void PercentageConversionAcceptsAnExplicitZeroLengthBasis()
    {
        OdfUnitConversionContext context = new OdfUnitConversionContext
        {
            PercentageBasisInPoints = 0
        };

        OdfUnitValue toPoints = OdfUnitConverter.ConvertLength(
            OdfLexicalParser.ParseMeasurement("50%"), OdfUnitKind.Point, context);
        OdfUnitValue toPercentage = OdfUnitConverter.ConvertLength(
            OdfLexicalParser.ParseLength("0pt"), OdfUnitKind.Percentage, context);

        Assert.Multiple(() =>
        {
            Assert.That(toPoints.IsValid, Is.True);
            Assert.That(toPoints.NumericValue, Is.Zero);
            Assert.That(toPercentage.IsValid, Is.False);
        });
    }

    [Test]
    public void AngleConversionsSupportDegreesRadiansAndGradians()
    {
        OdfUnitValue grads = OdfUnitConverter.ConvertAngle(
            OdfLexicalParser.ParseAngle("200grad"), OdfUnitKind.Degree);
        OdfUnitValue radians = OdfUnitConverter.ConvertAngle(
            OdfLexicalParser.ParseAngle("180deg"), OdfUnitKind.Radian);
        OdfUnitValue degrees = OdfUnitConverter.ConvertAngle(
            OdfLexicalParser.ParseAngle("3.141592653589793rad"), OdfUnitKind.Unitless);

        Assert.Multiple(() =>
        {
            Assert.That(grads.IsValid, Is.True);
            Assert.That(grads.NumericValue, Is.EqualTo(180).Within(1e-12));
            Assert.That(radians.NumericValue, Is.EqualTo(global::System.Math.PI).Within(1e-12));
            Assert.That(degrees.NumericValue, Is.EqualTo(180).Within(1e-12));
            Assert.That(OdfLexicalFormatter.FormatAngle(grads), Is.EqualTo("180deg"));
        });
    }

    [Test]
    public void NullGeometryAndUnitFormatInputsProduceEmptyText()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfLexicalFormatter.FormatMeasurement(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatAngle(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatPoint(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatVector(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatViewBox(null), Is.Empty);
        });
    }
}
