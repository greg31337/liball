namespace svg.Tests;

public class SvgUnitsTests
{
    [TestCase("2", SvgLengthUnit.Number, "2")]
    [TestCase("2%", SvgLengthUnit.Percentage, "2%")]
    [TestCase("2px", SvgLengthUnit.Px, "2px")]
    [TestCase("2em", SvgLengthUnit.Em, "2em")]
    [TestCase("2ex", SvgLengthUnit.Ex, "2ex")]
    [TestCase("2ch", SvgLengthUnit.Ch, "2ch")]
    [TestCase("2rem", SvgLengthUnit.Rem, "2rem")]
    [TestCase("2vw", SvgLengthUnit.Vw, "2vw")]
    [TestCase("2vh", SvgLengthUnit.Vh, "2vh")]
    [TestCase("2vmin", SvgLengthUnit.Vmin, "2vmin")]
    [TestCase("2vmax", SvgLengthUnit.Vmax, "2vmax")]
    [TestCase("2cm", SvgLengthUnit.Cm, "2cm")]
    [TestCase("2mm", SvgLengthUnit.Mm, "2mm")]
    [TestCase("2Q", SvgLengthUnit.Q, "2Q")]
    [TestCase("2in", SvgLengthUnit.In, "2in")]
    [TestCase("2pt", SvgLengthUnit.Pt, "2pt")]
    [TestCase("2pc", SvgLengthUnit.Pc, "2pc")]
    public void ParserRecognizesAndSerializesEveryPlannedLengthUnit(string input, SvgLengthUnit unit, string expectedText)
    {
        SvgLengthParseResult result = SvgLengthParser.ParseLength(input, true, true, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgLengthParseError.None));
            Assert.That(result.NumberError, Is.EqualTo(SvgNumberParseError.None));
            Assert.That(result.Length.Value, Is.EqualTo(2));
            Assert.That(result.Length.Unit, Is.EqualTo(unit));
            Assert.That(result.Length.ToText(), Is.EqualTo(expectedText));
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        });
    }

    [Test]
    public void UnitsAreParsedCaseInsensitivelyAndSerializedCanonically()
    {
        SvgLengthParseResult pixels = SvgLengthParser.ParseCssLength(" 1.5PX ", true, true, 32);
        SvgLengthParseResult quarterMillimeters = SvgLengthParser.ParseCssLength("2q", true, true, 32);
        SvgLengthParseResult viewport = SvgLengthParser.ParseCssLength("3VMIN", true, true, 32);

        Assert.Multiple(() =>
        {
            Assert.That(pixels.Success, Is.True);
            Assert.That(pixels.Length.ToText(), Is.EqualTo("1.5px"));
            Assert.That(quarterMillimeters.Success, Is.True);
            Assert.That(quarterMillimeters.Length.Unit, Is.EqualTo(SvgLengthUnit.Q));
            Assert.That(quarterMillimeters.Length.ToText(), Is.EqualTo("2Q"));
            Assert.That(viewport.Success, Is.True);
            Assert.That(viewport.Length.ToText(), Is.EqualTo("3vmin"));
        });
    }

    [Test]
    public void SvgAndCssModesRespectTheirNumberTokenBoundaries()
    {
        SvgLengthParseResult svg = SvgLengthParser.ParseLength("1.px", false, false, 32);
        SvgLengthParseResult css = SvgLengthParser.ParseCssLength("1.px", false, false, 32);

        Assert.Multiple(() =>
        {
            Assert.That(svg.Success, Is.True);
            Assert.That(svg.Length.Value, Is.EqualTo(1));
            Assert.That(svg.Length.Unit, Is.EqualTo(SvgLengthUnit.Px));
            Assert.That(css.Success, Is.False);
            Assert.That(css.Error, Is.EqualTo(SvgLengthParseError.UnitRequired));
            Assert.That(css.ErrorOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void UnitlessAndPercentagePoliciesAreExplicit()
    {
        SvgLengthParseResult unitlessAllowed = SvgLengthParser.ParseLength("-2.5", false, true, 32);
        SvgLengthParseResult unitlessRejected = SvgLengthParser.ParseLength("0", false, false, 32);
        SvgLengthParseResult percentageAllowed = SvgLengthParser.ParseLength("25%", true, false, 32);
        SvgLengthParseResult percentageRejected = SvgLengthParser.ParseLength("25%", false, false, 32);

        Assert.Multiple(() =>
        {
            Assert.That(unitlessAllowed.Success, Is.True);
            Assert.That(unitlessAllowed.Length.Unit, Is.EqualTo(SvgLengthUnit.Number));
            Assert.That(unitlessRejected.Success, Is.False);
            Assert.That(unitlessRejected.Error, Is.EqualTo(SvgLengthParseError.UnitRequired));
            Assert.That(unitlessRejected.ErrorOffset, Is.EqualTo(1));
            Assert.That(percentageAllowed.Success, Is.True);
            Assert.That(percentageAllowed.Length.Unit, Is.EqualTo(SvgLengthUnit.Percentage));
            Assert.That(percentageRejected.Success, Is.False);
            Assert.That(percentageRejected.Error, Is.EqualTo(SvgLengthParseError.PercentageNotAllowed));
            Assert.That(percentageRejected.ErrorOffset, Is.EqualTo(2));
        });
    }

    [Test]
    public void MalformedLengthsReturnExactNonThrowingFailures()
    {
        SvgLengthParseResult empty = SvgLengthParser.ParseLength(" ", true, true, 32);
        SvgLengthParseResult exponent = SvgLengthParser.ParseLength("1e+px", true, true, 32);
        SvgLengthParseResult overflow = SvgLengthParser.ParseLength("1e309px", true, true, 32);
        SvgLengthParseResult unknown = SvgLengthParser.ParseLength("2furlong", true, true, 32);
        SvgLengthParseResult trailing = SvgLengthParser.ParseLength("2px!", true, true, 32);
        SvgLengthParseResult repeatedPercent = SvgLengthParser.ParseLength("2%%", true, true, 32);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Error, Is.EqualTo(SvgLengthParseError.ExpectedLength));
            Assert.That(empty.ErrorOffset, Is.EqualTo(1));
            Assert.That(exponent.Error, Is.EqualTo(SvgLengthParseError.InvalidNumber));
            Assert.That(exponent.NumberError, Is.EqualTo(SvgNumberParseError.InvalidExponent));
            Assert.That(exponent.ErrorOffset, Is.EqualTo(3));
            Assert.That(overflow.Error, Is.EqualTo(SvgLengthParseError.InvalidNumber));
            Assert.That(overflow.NumberError, Is.EqualTo(SvgNumberParseError.Overflow));
            Assert.That(overflow.ErrorOffset, Is.EqualTo(4));
            Assert.That(unknown.Error, Is.EqualTo(SvgLengthParseError.UnknownUnit));
            Assert.That(unknown.ErrorOffset, Is.EqualTo(1));
            Assert.That(trailing.Error, Is.EqualTo(SvgLengthParseError.TrailingCharacters));
            Assert.That(trailing.ErrorOffset, Is.EqualTo(3));
            Assert.That(repeatedPercent.Error, Is.EqualTo(SvgLengthParseError.TrailingCharacters));
            Assert.That(repeatedPercent.ErrorOffset, Is.EqualTo(2));
        });
    }

    [Test]
    public void CompleteDimensionTokenLimitIncludesTheUnitSuffix()
    {
        SvgLengthParseResult exact = SvgLengthParser.ParseLength("1px", true, true, 3);
        SvgLengthParseResult tooLong = SvgLengthParser.ParseLength("1rem", true, true, 3);
        SvgLengthParseResult invalidLimit = SvgLengthParser.ParseLength("1px", true, true, 0);
        SvgLengthParseResult nullValue = SvgLengthParser.ParseLength(null, true, true, 32);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(tooLong.Success, Is.False);
            Assert.That(tooLong.Error, Is.EqualTo(SvgLengthParseError.TokenTooLong));
            Assert.That(tooLong.ErrorOffset, Is.EqualTo(3));
            Assert.That(invalidLimit.Error, Is.EqualTo(SvgLengthParseError.InvalidArgument));
            Assert.That(nullValue.Error, Is.EqualTo(SvgLengthParseError.InvalidArgument));
        });
    }

    [TestCase(SvgLengthUnit.Number, 12.5, 12.5)]
    [TestCase(SvgLengthUnit.Px, 12.5, 12.5)]
    [TestCase(SvgLengthUnit.In, 1.0, 96.0)]
    [TestCase(SvgLengthUnit.Cm, 2.54, 96.0)]
    [TestCase(SvgLengthUnit.Mm, 25.4, 96.0)]
    [TestCase(SvgLengthUnit.Q, 101.6, 96.0)]
    [TestCase(SvgLengthUnit.Pt, 72.0, 96.0)]
    [TestCase(SvgLengthUnit.Pc, 6.0, 96.0)]
    public void AbsoluteAndUnitlessLengthsEvaluateWithoutContext(SvgLengthUnit unit, double value, double expected)
    {
        SvgLengthEvaluationResult result = new SvgLength(value, unit).Evaluate(null, SvgLengthPercentageBasis.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgLengthEvaluationError.None));
            Assert.That(result.Value, Is.EqualTo(expected).Within(1e-12));
        });
    }

    [Test]
    public void FontAndRootFontUnitsUseTheirSpecificMetrics()
    {
        SvgLengthContext context = CreateCompleteContext();

        SvgLengthEvaluationResult em = new SvgLength(2, SvgLengthUnit.Em).Evaluate(context, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult ex = new SvgLength(2, SvgLengthUnit.Ex).Evaluate(context, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult ch = new SvgLength(2, SvgLengthUnit.Ch).Evaluate(context, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult rem = new SvgLength(2, SvgLengthUnit.Rem).Evaluate(context, SvgLengthPercentageBasis.None);

        Assert.Multiple(() =>
        {
            Assert.That(em.Value, Is.EqualTo(32));
            Assert.That(ex.Value, Is.EqualTo(16));
            Assert.That(ch.Value, Is.EqualTo(18));
            Assert.That(rem.Value, Is.EqualTo(40));
        });
    }

    [Test]
    public void ViewportUnitsUseWidthHeightMinimumAndMaximum()
    {
        SvgLengthContext context = CreateCompleteContext();

        SvgLengthEvaluationResult vw = new SvgLength(10, SvgLengthUnit.Vw).Evaluate(context, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult vh = new SvgLength(10, SvgLengthUnit.Vh).Evaluate(context, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult vmin = new SvgLength(10, SvgLengthUnit.Vmin).Evaluate(context, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult vmax = new SvgLength(10, SvgLengthUnit.Vmax).Evaluate(context, SvgLengthPercentageBasis.None);

        Assert.Multiple(() =>
        {
            Assert.That(vw.Value, Is.EqualTo(20));
            Assert.That(vh.Value, Is.EqualTo(10));
            Assert.That(vmin.Value, Is.EqualTo(10));
            Assert.That(vmax.Value, Is.EqualTo(20));
        });
    }

    [Test]
    public void PercentagesUseExplicitViewportFontAndRootFontBases()
    {
        SvgLengthContext context = CreateCompleteContext();
        SvgLength percentage = new SvgLength(50, SvgLengthUnit.Percentage);

        SvgLengthEvaluationResult width = percentage.Evaluate(context, SvgLengthPercentageBasis.ViewportWidth);
        SvgLengthEvaluationResult height = percentage.Evaluate(context, SvgLengthPercentageBasis.ViewportHeight);
        SvgLengthEvaluationResult diagonal = percentage.Evaluate(context, SvgLengthPercentageBasis.ViewportDiagonal);
        SvgLengthEvaluationResult font = percentage.Evaluate(context, SvgLengthPercentageBasis.FontSize);
        SvgLengthEvaluationResult rootFont = percentage.Evaluate(context, SvgLengthPercentageBasis.RootFontSize);

        Assert.Multiple(() =>
        {
            Assert.That(width.Value, Is.EqualTo(100));
            Assert.That(height.Value, Is.EqualTo(50));
            Assert.That(diagonal.Value, Is.EqualTo(Math.Sqrt(25000) * 0.5).Within(1e-12));
            Assert.That(font.Value, Is.EqualTo(8));
            Assert.That(rootFont.Value, Is.EqualTo(10));
        });
    }

    [Test]
    public void ObjectBoundingBoxBasisScalesUnitlessFractionsAndPercentages()
    {
        SvgLengthContext context = CreateCompleteContext();

        SvgLengthEvaluationResult unitlessWidth = new SvgLength(0.5, SvgLengthUnit.Number).Evaluate(context, SvgLengthPercentageBasis.ObjectBoundingBoxWidth);
        SvgLengthEvaluationResult percentageHeight = new SvgLength(50, SvgLengthUnit.Percentage).Evaluate(context, SvgLengthPercentageBasis.ObjectBoundingBoxHeight);
        SvgLengthEvaluationResult unitlessDiagonal = new SvgLength(0.5, SvgLengthUnit.Number).Evaluate(context, SvgLengthPercentageBasis.ObjectBoundingBoxDiagonal);
        SvgLengthEvaluationResult pixels = new SvgLength(0.5, SvgLengthUnit.Px).Evaluate(context, SvgLengthPercentageBasis.ObjectBoundingBoxWidth);

        Assert.Multiple(() =>
        {
            Assert.That(unitlessWidth.Value, Is.EqualTo(20));
            Assert.That(percentageHeight.Value, Is.EqualTo(15));
            Assert.That(unitlessDiagonal.Value, Is.EqualTo(Math.Sqrt(1250) * 0.5).Within(1e-12));
            Assert.That(pixels.Value, Is.EqualTo(0.5));
        });
    }

    [Test]
    public void MissingContextsReturnSpecificFailures()
    {
        SvgLengthContext fontOnly = new SvgLengthContext();
        fontOnly.HasFontSize = true;
        fontOnly.FontSize = 16;

        SvgLengthEvaluationResult viewport = new SvgLength(1, SvgLengthUnit.Vw).Evaluate(null, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult font = new SvgLength(1, SvgLengthUnit.Em).Evaluate(null, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult metric = new SvgLength(1, SvgLengthUnit.Ex).Evaluate(fontOnly, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult root = new SvgLength(1, SvgLengthUnit.Rem).Evaluate(null, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult basis = new SvgLength(50, SvgLengthUnit.Percentage).Evaluate(null, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult boundingBox = new SvgLength(0.5, SvgLengthUnit.Number).Evaluate(null, SvgLengthPercentageBasis.ObjectBoundingBoxWidth);

        Assert.Multiple(() =>
        {
            Assert.That(viewport.Error, Is.EqualTo(SvgLengthEvaluationError.MissingViewport));
            Assert.That(font.Error, Is.EqualTo(SvgLengthEvaluationError.MissingFontSize));
            Assert.That(metric.Error, Is.EqualTo(SvgLengthEvaluationError.MissingFontMetric));
            Assert.That(root.Error, Is.EqualTo(SvgLengthEvaluationError.MissingRootFontSize));
            Assert.That(basis.Error, Is.EqualTo(SvgLengthEvaluationError.MissingPercentageBasis));
            Assert.That(boundingBox.Error, Is.EqualTo(SvgLengthEvaluationError.MissingObjectBoundingBox));
        });
    }

    [Test]
    public void InvalidContextsAndOverflowingConversionsFailWithoutReturningInfinity()
    {
        SvgLengthContext invalidViewport = new SvgLengthContext();
        invalidViewport.HasViewport = true;
        invalidViewport.ViewportWidth = double.NaN;
        invalidViewport.ViewportHeight = 100;
        SvgLengthContext hugeViewport = new SvgLengthContext();
        hugeViewport.HasViewport = true;
        hugeViewport.ViewportWidth = double.MaxValue;
        hugeViewport.ViewportHeight = double.MaxValue;

        SvgLengthEvaluationResult invalid = new SvgLength(1, SvgLengthUnit.Vw).Evaluate(invalidViewport, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult conversionOverflow = new SvgLength(double.MaxValue, SvgLengthUnit.Cm).Evaluate(null, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult stableDiagonal = new SvgLength(100, SvgLengthUnit.Percentage).Evaluate(hugeViewport, SvgLengthPercentageBasis.ViewportDiagonal);
        SvgLengthEvaluationResult percentageOverflow = new SvgLength(200, SvgLengthUnit.Percentage).Evaluate(hugeViewport, SvgLengthPercentageBasis.ViewportWidth);
        SvgLengthEvaluationResult invalidBasis = new SvgLength(50, SvgLengthUnit.Percentage).Evaluate(new SvgLengthContext(), (SvgLengthPercentageBasis)999);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(SvgLengthEvaluationError.InvalidContext));
            Assert.That(conversionOverflow.Success, Is.False);
            Assert.That(conversionOverflow.Error, Is.EqualTo(SvgLengthEvaluationError.NonFiniteResult));
            Assert.That(conversionOverflow.Value, Is.Zero);
            Assert.That(stableDiagonal.Success, Is.True);
            Assert.That(stableDiagonal.Value, Is.EqualTo(double.MaxValue));
            Assert.That(percentageOverflow.Error, Is.EqualTo(SvgLengthEvaluationError.NonFiniteResult));
            Assert.That(invalidBasis.Error, Is.EqualTo(SvgLengthEvaluationError.UnsupportedUnitOrBasis));
        });
    }

    [Test]
    public void LengthConstructorEnforcesFiniteKnownValuesAndNormalizesZero()
    {
        SvgLength negativeZero = new SvgLength(-0.0, SvgLengthUnit.Px);

        Assert.Multiple(() =>
        {
            Assert.That(BitConverter.DoubleToInt64Bits(negativeZero.Value), Is.Zero);
            Assert.That(negativeZero.ToText(), Is.EqualTo("0px"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgLength(double.NaN, SvgLengthUnit.Px));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgLength(double.PositiveInfinity, SvgLengthUnit.Px));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgLength(1, (SvgLengthUnit)999));
        });
    }

    [Test]
    public void EvaluationRejectsPubliclyMutatedInvalidLengthState()
    {
        SvgLength invalidValue = new SvgLength(1, SvgLengthUnit.Px);
        invalidValue.Value = double.NaN;
        SvgLength invalidUnit = new SvgLength(1, SvgLengthUnit.Px);
        invalidUnit.Unit = (SvgLengthUnit)999;

        SvgLengthEvaluationResult valueResult = invalidValue.Evaluate(null, SvgLengthPercentageBasis.None);
        SvgLengthEvaluationResult unitResult = invalidUnit.Evaluate(null, SvgLengthPercentageBasis.None);

        Assert.Multiple(() =>
        {
            Assert.That(valueResult.Error, Is.EqualTo(SvgLengthEvaluationError.InvalidArgument));
            Assert.That(unitResult.Error, Is.EqualTo(SvgLengthEvaluationError.InvalidArgument));
        });
    }

    static SvgLengthContext CreateCompleteContext()
    {
        return new SvgLengthContext
        {
            HasViewport = true,
            ViewportWidth = 200,
            ViewportHeight = 100,
            HasFontSize = true,
            FontSize = 16,
            HasXHeight = true,
            XHeight = 8,
            HasZeroAdvance = true,
            ZeroAdvance = 9,
            HasRootFontSize = true,
            RootFontSize = 20,
            HasObjectBoundingBox = true,
            ObjectBoundingBoxWidth = 40,
            ObjectBoundingBoxHeight = 30
        };
    }
}
