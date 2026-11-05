namespace svg.Tests;

public class SvgScalarUnitsTests
{
    [TestCase("90", true, SvgAngleUnit.Number, "90")]
    [TestCase("180deg", false, SvgAngleUnit.Deg, "180deg")]
    [TestCase("200GRAD", false, SvgAngleUnit.Grad, "200grad")]
    [TestCase("3.141592653589793rad", false, SvgAngleUnit.Rad, "3.141592653589793rad")]
    [TestCase(".5TURN", false, SvgAngleUnit.Turn, "0.5turn")]
    public void AnglesParseEveryUnitAndSerializeCanonically(string input, bool allowUnitless, SvgAngleUnit unit, string expectedText)
    {
        SvgAngleParseResult result = SvgScalarUnitParser.ParseAngle(input, allowUnitless, 64);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgScalarUnitParseError.None));
            Assert.That(result.Angle.Unit, Is.EqualTo(unit));
            Assert.That(result.Angle.ToText(), Is.EqualTo(expectedText));
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        });
    }

    [Test]
    public void AngleConversionsCoverDegreesGradiansRadiansAndTurns()
    {
        SvgUnitConversionResult degreesToRadians = new SvgAngle(180, SvgAngleUnit.Deg).ConvertTo(SvgAngleUnit.Rad);
        SvgUnitConversionResult gradiansToDegrees = new SvgAngle(200, SvgAngleUnit.Grad).ConvertTo(SvgAngleUnit.Deg);
        SvgUnitConversionResult radiansToDegrees = new SvgAngle(Math.PI, SvgAngleUnit.Rad).ConvertTo(SvgAngleUnit.Deg);
        SvgUnitConversionResult turnsToDegrees = new SvgAngle(0.5, SvgAngleUnit.Turn).ConvertTo(SvgAngleUnit.Deg);
        SvgUnitConversionResult unitlessToDegrees = new SvgAngle(90, SvgAngleUnit.Number).ConvertTo(SvgAngleUnit.Deg);

        Assert.Multiple(() =>
        {
            Assert.That(degreesToRadians.Value, Is.EqualTo(Math.PI).Within(1e-14));
            Assert.That(gradiansToDegrees.Value, Is.EqualTo(180).Within(1e-12));
            Assert.That(radiansToDegrees.Value, Is.EqualTo(180).Within(1e-12));
            Assert.That(turnsToDegrees.Value, Is.EqualTo(180));
            Assert.That(unitlessToDegrees.Value, Is.EqualTo(90));
        });
    }

    [TestCase("1s", false, SvgTimeUnit.Seconds, "1s")]
    [TestCase("250MS", false, SvgTimeUnit.Milliseconds, "250ms")]
    [TestCase("-1.5", true, SvgTimeUnit.Seconds, "-1.5s")]
    public void TimesParseUnitsAndOptionalUnitlessSeconds(string input, bool allowUnitless, SvgTimeUnit unit, string expectedText)
    {
        SvgTimeParseResult result = SvgScalarUnitParser.ParseTime(input, allowUnitless, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Time.Unit, Is.EqualTo(unit));
            Assert.That(result.Time.ToText(), Is.EqualTo(expectedText));
        });
    }

    [Test]
    public void TimeConversionsRoundTripBetweenSecondsAndMilliseconds()
    {
        SvgUnitConversionResult milliseconds = new SvgTime(1.25, SvgTimeUnit.Seconds).ConvertTo(SvgTimeUnit.Milliseconds);
        SvgUnitConversionResult seconds = new SvgTime(250, SvgTimeUnit.Milliseconds).ConvertTo(SvgTimeUnit.Seconds);

        Assert.Multiple(() =>
        {
            Assert.That(milliseconds.Success, Is.True);
            Assert.That(milliseconds.Value, Is.EqualTo(1250));
            Assert.That(seconds.Success, Is.True);
            Assert.That(seconds.Value, Is.EqualTo(0.25));
        });
    }

    [TestCase("0Hz", SvgFrequencyUnit.Hz, "0Hz")]
    [TestCase("440hz", SvgFrequencyUnit.Hz, "440Hz")]
    [TestCase("2.5KHZ", SvgFrequencyUnit.KHz, "2.5kHz")]
    public void FrequenciesParseNonnegativeValuesAndCanonicalUnits(string input, SvgFrequencyUnit unit, string expectedText)
    {
        SvgFrequencyParseResult result = SvgScalarUnitParser.ParseFrequency(input, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Frequency.Unit, Is.EqualTo(unit));
            Assert.That(result.Frequency.ToText(), Is.EqualTo(expectedText));
        });
    }

    [Test]
    public void FrequencyConversionsUseOneThousandHertzPerKilohertz()
    {
        SvgUnitConversionResult hertz = new SvgFrequency(2.5, SvgFrequencyUnit.KHz).ConvertTo(SvgFrequencyUnit.Hz);
        SvgUnitConversionResult kilohertz = new SvgFrequency(500, SvgFrequencyUnit.Hz).ConvertTo(SvgFrequencyUnit.KHz);

        Assert.Multiple(() =>
        {
            Assert.That(hertz.Value, Is.EqualTo(2500));
            Assert.That(kilohertz.Value, Is.EqualTo(0.5));
        });
    }

    [TestCase("96dpi", SvgResolutionUnit.Dpi, "96dpi")]
    [TestCase("10DPCM", SvgResolutionUnit.Dpcm, "10dpcm")]
    [TestCase("2dppx", SvgResolutionUnit.Dppx, "2dppx")]
    [TestCase("2X", SvgResolutionUnit.Dppx, "2dppx")]
    public void ResolutionsParseEveryUnitAndCanonicalizeTheXAlias(string input, SvgResolutionUnit unit, string expectedText)
    {
        SvgResolutionParseResult result = SvgScalarUnitParser.ParseResolution(input, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Resolution.Unit, Is.EqualTo(unit));
            Assert.That(result.Resolution.ToText(), Is.EqualTo(expectedText));
        });
    }

    [Test]
    public void ResolutionConversionsUseCssReferenceRatios()
    {
        SvgUnitConversionResult dppx = new SvgResolution(96, SvgResolutionUnit.Dpi).ConvertTo(SvgResolutionUnit.Dppx);
        SvgUnitConversionResult dpiFromDpcm = new SvgResolution(1, SvgResolutionUnit.Dpcm).ConvertTo(SvgResolutionUnit.Dpi);
        SvgUnitConversionResult dpiFromDppx = new SvgResolution(2, SvgResolutionUnit.Dppx).ConvertTo(SvgResolutionUnit.Dpi);

        Assert.Multiple(() =>
        {
            Assert.That(dppx.Value, Is.EqualTo(1));
            Assert.That(dpiFromDpcm.Value, Is.EqualTo(2.54));
            Assert.That(dpiFromDppx.Value, Is.EqualTo(192));
        });
    }

    [Test]
    public void RatiosParseOptionalDenominatorsAndSerializeExplicitly()
    {
        SvgRatioParseResult fraction = SvgScalarUnitParser.ParseRatio(" 16 / 9 ", 16);
        SvgRatioParseResult single = SvgScalarUnitParser.ParseRatio("1.5", 16);
        SvgUnitConversionResult value = fraction.Ratio.ToValue();

        Assert.Multiple(() =>
        {
            Assert.That(fraction.Success, Is.True);
            Assert.That(fraction.Ratio.Numerator, Is.EqualTo(16));
            Assert.That(fraction.Ratio.Denominator, Is.EqualTo(9));
            Assert.That(fraction.Ratio.ToText(), Is.EqualTo("16/9"));
            Assert.That(value.Value, Is.EqualTo(16.0 / 9.0));
            Assert.That(single.Success, Is.True);
            Assert.That(single.Ratio.Denominator, Is.EqualTo(1));
            Assert.That(single.Ratio.ToText(), Is.EqualTo("1.5/1"));
        });
    }

    [TestCase("0", 0.0)]
    [TestCase("12.5", 12.5)]
    [TestCase("1e2", 100.0)]
    public void PathLengthParsesFiniteNonnegativeNumbers(string input, double expected)
    {
        SvgPathLengthParseResult result = SvgScalarUnitParser.ParsePathLength(input, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PathLength.Value, Is.EqualTo(expected));
            Assert.That(result.PathLength.ToText(), Is.EqualTo(SvgNumberFormatter.FormatShortest(expected).Text));
        });
    }

    [Test]
    public void PathLengthScaleHandlesPositiveZeroAndOverflowCases()
    {
        SvgUnitConversionResult scale = new SvgPathLength(100).GetScale(250);
        SvgUnitConversionResult zeroAuthorLength = new SvgPathLength(0).GetScale(10);
        SvgUnitConversionResult invalidActualLength = new SvgPathLength(10).GetScale(-1);
        SvgUnitConversionResult overflow = new SvgPathLength(double.Epsilon).GetScale(double.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(scale.Success, Is.True);
            Assert.That(scale.Value, Is.EqualTo(2.5));
            Assert.That(zeroAuthorLength.Error, Is.EqualTo(SvgUnitConversionError.ZeroDivisor));
            Assert.That(invalidActualLength.Error, Is.EqualTo(SvgUnitConversionError.InvalidArgument));
            Assert.That(overflow.Error, Is.EqualTo(SvgUnitConversionError.NonFiniteResult));
        });
    }

    [Test]
    public void RequiredUnitsAndUnknownUnitsReportExactOffsets()
    {
        SvgAngleParseResult angleUnit = SvgScalarUnitParser.ParseAngle("90", false, 32);
        SvgTimeParseResult timeUnit = SvgScalarUnitParser.ParseTime("1", false, 32);
        SvgFrequencyParseResult frequencyUnit = SvgScalarUnitParser.ParseFrequency("440", 32);
        SvgResolutionParseResult resolutionUnit = SvgScalarUnitParser.ParseResolution("96", 32);
        SvgAngleParseResult unknownAngle = SvgScalarUnitParser.ParseAngle("2degrees", false, 32);
        SvgFrequencyParseResult unknownFrequency = SvgScalarUnitParser.ParseFrequency("2rpm", 32);

        Assert.Multiple(() =>
        {
            Assert.That(angleUnit.Error, Is.EqualTo(SvgScalarUnitParseError.UnitRequired));
            Assert.That(angleUnit.ErrorOffset, Is.EqualTo(2));
            Assert.That(timeUnit.Error, Is.EqualTo(SvgScalarUnitParseError.UnitRequired));
            Assert.That(timeUnit.ErrorOffset, Is.EqualTo(1));
            Assert.That(frequencyUnit.Error, Is.EqualTo(SvgScalarUnitParseError.UnitRequired));
            Assert.That(frequencyUnit.ErrorOffset, Is.EqualTo(3));
            Assert.That(resolutionUnit.Error, Is.EqualTo(SvgScalarUnitParseError.UnitRequired));
            Assert.That(resolutionUnit.ErrorOffset, Is.EqualTo(2));
            Assert.That(unknownAngle.Error, Is.EqualTo(SvgScalarUnitParseError.UnknownUnit));
            Assert.That(unknownAngle.ErrorOffset, Is.EqualTo(1));
            Assert.That(unknownFrequency.Error, Is.EqualTo(SvgScalarUnitParseError.UnknownUnit));
            Assert.That(unknownFrequency.ErrorOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void ScalarRangeValidationReportsTheNumericComponentOffset()
    {
        SvgFrequencyParseResult negativeFrequency = SvgScalarUnitParser.ParseFrequency(" -1Hz", 32);
        SvgResolutionParseResult zeroResolution = SvgScalarUnitParser.ParseResolution(" 0dpi", 32);
        SvgResolutionParseResult negativeResolution = SvgScalarUnitParser.ParseResolution("-1dppx", 32);
        SvgRatioParseResult zeroNumerator = SvgScalarUnitParser.ParseRatio(" 0/1", 32);
        SvgRatioParseResult zeroDenominator = SvgScalarUnitParser.ParseRatio("1 / 0", 32);
        SvgPathLengthParseResult negativePathLength = SvgScalarUnitParser.ParsePathLength(" -1", 32);

        Assert.Multiple(() =>
        {
            Assert.That(negativeFrequency.Error, Is.EqualTo(SvgScalarUnitParseError.OutOfRange));
            Assert.That(negativeFrequency.ErrorOffset, Is.EqualTo(1));
            Assert.That(zeroResolution.Error, Is.EqualTo(SvgScalarUnitParseError.OutOfRange));
            Assert.That(zeroResolution.ErrorOffset, Is.EqualTo(1));
            Assert.That(negativeResolution.ErrorOffset, Is.Zero);
            Assert.That(zeroNumerator.Error, Is.EqualTo(SvgScalarUnitParseError.OutOfRange));
            Assert.That(zeroNumerator.ErrorOffset, Is.EqualTo(1));
            Assert.That(zeroDenominator.Error, Is.EqualTo(SvgScalarUnitParseError.OutOfRange));
            Assert.That(zeroDenominator.ErrorOffset, Is.EqualTo(4));
            Assert.That(negativePathLength.Error, Is.EqualTo(SvgScalarUnitParseError.OutOfRange));
            Assert.That(negativePathLength.ErrorOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void MalformedNumbersRatiosAndTrailingTextReturnStableFailures()
    {
        SvgAngleParseResult empty = SvgScalarUnitParser.ParseAngle(" ", true, 32);
        SvgAngleParseResult overflow = SvgScalarUnitParser.ParseAngle("1e309deg", false, 32);
        SvgAngleParseResult trailing = SvgScalarUnitParser.ParseAngle("2deg!", false, 32);
        SvgRatioParseResult missingDenominator = SvgScalarUnitParser.ParseRatio("16/", 32);
        SvgRatioParseResult repeatedSlash = SvgScalarUnitParser.ParseRatio("16//9", 32);
        SvgRatioParseResult trailingRatio = SvgScalarUnitParser.ParseRatio("1/2/3", 32);
        SvgPathLengthParseResult overflowPath = SvgScalarUnitParser.ParsePathLength("1e309", 32);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Error, Is.EqualTo(SvgScalarUnitParseError.ExpectedValue));
            Assert.That(empty.ErrorOffset, Is.EqualTo(1));
            Assert.That(overflow.Error, Is.EqualTo(SvgScalarUnitParseError.InvalidNumber));
            Assert.That(overflow.NumberError, Is.EqualTo(SvgNumberParseError.Overflow));
            Assert.That(overflow.ErrorOffset, Is.EqualTo(4));
            Assert.That(trailing.Error, Is.EqualTo(SvgScalarUnitParseError.TrailingCharacters));
            Assert.That(trailing.ErrorOffset, Is.EqualTo(4));
            Assert.That(missingDenominator.Error, Is.EqualTo(SvgScalarUnitParseError.ExpectedRatioComponent));
            Assert.That(missingDenominator.ErrorOffset, Is.EqualTo(3));
            Assert.That(repeatedSlash.Error, Is.EqualTo(SvgScalarUnitParseError.ExpectedRatioComponent));
            Assert.That(repeatedSlash.ErrorOffset, Is.EqualTo(3));
            Assert.That(trailingRatio.Error, Is.EqualTo(SvgScalarUnitParseError.TrailingCharacters));
            Assert.That(trailingRatio.ErrorOffset, Is.EqualTo(3));
            Assert.That(overflowPath.Error, Is.EqualTo(SvgScalarUnitParseError.InvalidNumber));
            Assert.That(overflowPath.NumberError, Is.EqualTo(SvgNumberParseError.Overflow));
        });
    }

    [Test]
    public void CompleteTokenBoundsAndInvalidArgumentsReturnFailures()
    {
        SvgAngleParseResult exact = SvgScalarUnitParser.ParseAngle("1deg", false, 4);
        SvgAngleParseResult longAngle = SvgScalarUnitParser.ParseAngle("1turn", false, 4);
        SvgRatioParseResult longRatio = SvgScalarUnitParser.ParseRatio("16 / 9", 5);
        SvgRatioParseResult trailingWhitespace = SvgScalarUnitParser.ParseRatio("16/9     ", 4);
        SvgTimeParseResult invalidLimit = SvgScalarUnitParser.ParseTime("1s", false, 0);
        SvgResolutionParseResult nullResolution = SvgScalarUnitParser.ParseResolution(null, 32);
        SvgRatioParseResult nullRatio = SvgScalarUnitParser.ParseRatio(null, 32);
        SvgPathLengthParseResult nullPath = SvgScalarUnitParser.ParsePathLength(null, 32);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(longAngle.Error, Is.EqualTo(SvgScalarUnitParseError.TokenTooLong));
            Assert.That(longAngle.ErrorOffset, Is.EqualTo(4));
            Assert.That(longRatio.Error, Is.EqualTo(SvgScalarUnitParseError.TokenTooLong));
            Assert.That(longRatio.ErrorOffset, Is.EqualTo(5));
            Assert.That(trailingWhitespace.Success, Is.True);
            Assert.That(invalidLimit.Error, Is.EqualTo(SvgScalarUnitParseError.InvalidArgument));
            Assert.That(nullResolution.Error, Is.EqualTo(SvgScalarUnitParseError.InvalidArgument));
            Assert.That(nullRatio.Error, Is.EqualTo(SvgScalarUnitParseError.InvalidArgument));
            Assert.That(nullPath.Error, Is.EqualTo(SvgScalarUnitParseError.InvalidArgument));
        });
    }

    [Test]
    public void ConstructorsEnforceEachTypedValueDomain()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgAngle(double.NaN, SvgAngleUnit.Deg));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgAngle(1, (SvgAngleUnit)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgTime(double.PositiveInfinity, SvgTimeUnit.Seconds));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgTime(1, (SvgTimeUnit)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgFrequency(-1, SvgFrequencyUnit.Hz));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgFrequency(1, (SvgFrequencyUnit)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgResolution(0, SvgResolutionUnit.Dpi));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgResolution(1, (SvgResolutionUnit)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgRatio(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgRatio(1, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgPathLength(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgPathLength(double.NaN));
        });
    }

    [Test]
    public void ConversionsRejectMutatedInvalidStateAndFiniteOverflow()
    {
        SvgAngle invalidAngle = new SvgAngle(1, SvgAngleUnit.Deg);
        invalidAngle.Unit = (SvgAngleUnit)999;
        SvgFrequency invalidFrequency = new SvgFrequency(1, SvgFrequencyUnit.Hz);
        invalidFrequency.Value = -1;
        SvgRatio invalidRatio = new SvgRatio(1, 1);
        invalidRatio.Denominator = 0;

        SvgUnitConversionResult angleState = invalidAngle.ConvertTo(SvgAngleUnit.Rad);
        SvgUnitConversionResult frequencyState = invalidFrequency.ConvertTo(SvgFrequencyUnit.KHz);
        SvgUnitConversionResult ratioState = invalidRatio.ToValue();
        SvgUnitConversionResult angleOverflow = new SvgAngle(double.MaxValue, SvgAngleUnit.Turn).ConvertTo(SvgAngleUnit.Rad);
        SvgUnitConversionResult timeOverflow = new SvgTime(double.MaxValue, SvgTimeUnit.Seconds).ConvertTo(SvgTimeUnit.Milliseconds);
        SvgUnitConversionResult frequencyOverflow = new SvgFrequency(double.MaxValue, SvgFrequencyUnit.KHz).ConvertTo(SvgFrequencyUnit.Hz);
        SvgUnitConversionResult resolutionOverflow = new SvgResolution(double.MaxValue, SvgResolutionUnit.Dppx).ConvertTo(SvgResolutionUnit.Dpi);
        SvgUnitConversionResult ratioOverflow = new SvgRatio(double.MaxValue, double.Epsilon).ToValue();

        Assert.Multiple(() =>
        {
            Assert.That(angleState.Error, Is.EqualTo(SvgUnitConversionError.InvalidArgument));
            Assert.That(frequencyState.Error, Is.EqualTo(SvgUnitConversionError.InvalidArgument));
            Assert.That(ratioState.Error, Is.EqualTo(SvgUnitConversionError.InvalidArgument));
            Assert.That(angleOverflow.Error, Is.EqualTo(SvgUnitConversionError.NonFiniteResult));
            Assert.That(timeOverflow.Error, Is.EqualTo(SvgUnitConversionError.NonFiniteResult));
            Assert.That(frequencyOverflow.Error, Is.EqualTo(SvgUnitConversionError.NonFiniteResult));
            Assert.That(resolutionOverflow.Error, Is.EqualTo(SvgUnitConversionError.NonFiniteResult));
            Assert.That(ratioOverflow.Error, Is.EqualTo(SvgUnitConversionError.NonFiniteResult));
        });
    }

    [Test]
    [NonParallelizable]
    public void ScalarParsingAndSerializationAreCultureInvariant()
    {
        System.Globalization.CultureInfo originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");

            SvgAngleParseResult angle = SvgScalarUnitParser.ParseAngle("1.5deg", false, 32);
            SvgTimeParseResult time = SvgScalarUnitParser.ParseTime("2.5s", false, 32);
            SvgRatioParseResult ratio = SvgScalarUnitParser.ParseRatio("1.5/2.5", 32);

            Assert.Multiple(() =>
            {
                Assert.That(angle.Angle.ToText(), Is.EqualTo("1.5deg"));
                Assert.That(time.Time.ToText(), Is.EqualTo("2.5s"));
                Assert.That(ratio.Ratio.ToText(), Is.EqualTo("1.5/2.5"));
            });
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
