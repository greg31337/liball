using System.Numerics;
using xml;

namespace xml.Tests;

[TestFixture]
public class XdmDurationDatatypeTests
{
    [TestCase(XdmAtomicType.Duration, "P1Y13M1DT25H61M61.500S", "P2Y1M2DT2H2M1.5S")]
    [TestCase(XdmAtomicType.Duration, "-P0Y0M0DT0H0M0.0S", "PT0S")]
    [TestCase(XdmAtomicType.Duration, "  P1Y\n", "P1Y")]
    [TestCase(XdmAtomicType.YearMonthDuration, "P20Y15M", "P21Y3M")]
    [TestCase(XdmAtomicType.YearMonthDuration, "-P15M", "-P1Y3M")]
    [TestCase(XdmAtomicType.YearMonthDuration, "P0Y", "P0M")]
    [TestCase(XdmAtomicType.DayTimeDuration, "PT240H", "P10D")]
    [TestCase(XdmAtomicType.DayTimeDuration, "P0D", "PT0S")]
    [TestCase(XdmAtomicType.DayTimeDuration, "-PT256S", "-PT4M16S")]
    public void ParsesNormalizesAndCanonicalizesDurationValues(XdmAtomicType type, string lexical, string canonical)
    {
        XdmAtomicValue value = new XdmDurationDatatype().ParseXdmDuration(lexical, type);

        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(type));
            Assert.That(value.LexicalValue, Is.EqualTo(lexical));
            Assert.That(value.CanonicalValue, Is.EqualTo(canonical));
            Assert.That(value.StringValue, Is.EqualTo(canonical));
            Assert.That(value.ExtendedValue, Is.TypeOf<XdmDurationStorage>());
            Assert.That(value.IsImmutable, Is.True);
        });
    }

    [TestCase(XdmAtomicType.Duration, "")]
    [TestCase(XdmAtomicType.Duration, "P")]
    [TestCase(XdmAtomicType.Duration, "PT")]
    [TestCase(XdmAtomicType.Duration, "+P1D")]
    [TestCase(XdmAtomicType.Duration, "P-1D")]
    [TestCase(XdmAtomicType.Duration, "P1D1Y")]
    [TestCase(XdmAtomicType.Duration, "P1Y2Y")]
    [TestCase(XdmAtomicType.Duration, "P1.5D")]
    [TestCase(XdmAtomicType.Duration, "PT.5S")]
    [TestCase(XdmAtomicType.Duration, "PT1.S")]
    [TestCase(XdmAtomicType.Duration, "PT1M2H")]
    [TestCase(XdmAtomicType.YearMonthDuration, "P1D")]
    [TestCase(XdmAtomicType.YearMonthDuration, "P1YT1H")]
    [TestCase(XdmAtomicType.DayTimeDuration, "P1Y")]
    [TestCase(XdmAtomicType.DayTimeDuration, "P1M")]
    public void RejectsInvalidOrSubtypeIncompatibleDurationLexicalForms(XdmAtomicType type, string lexical)
    {
        XdmDurationDatatype datatype = new XdmDurationDatatype();
        XdmNumericParseResult result = datatype.TryParseXdmDuration(lexical, type);
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmDuration(lexical, type))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
        });
    }

    [Test]
    public void StoresArbitraryPrecisionMonthsAndFractionalSecondsExactly()
    {
        XdmDurationDatatype datatype = new XdmDurationDatatype();
        string years = new string('9', 200);
        string fraction = new string('1', 200);
        XdmAtomicValue value = datatype.ParseXdmDuration("P" + years + "YT0." + fraction + "S", XdmAtomicType.Duration);
        XdmDurationStorage storage = (XdmDurationStorage)value.ExtendedValue!;

        Assert.Multiple(() =>
        {
            Assert.That(storage.Months, Is.EqualTo(BigInteger.Parse(years) * 12));
            Assert.That(storage.SecondsScale, Is.EqualTo(200));
            Assert.That(storage.SecondsCoefficient, Is.EqualTo(BigInteger.Parse(fraction)));
            Assert.That(value.CanonicalValue, Is.EqualTo("P" + years + "YT0." + fraction + "S"));
        });
    }

    [Test]
    public void ComparesNormalizedDurationsAndOnlyOrdersMatchingSubtypes()
    {
        XdmDurationDatatype datatype = new XdmDurationDatatype();
        XdmAtomicValue year = datatype.ParseXdmDuration("P1Y", XdmAtomicType.Duration);
        XdmAtomicValue twelveMonths = datatype.ParseXdmDuration("P12M", XdmAtomicType.Duration);
        XdmAtomicValue day = datatype.ParseXdmDuration("P1D", XdmAtomicType.Duration);
        XdmAtomicValue hours = datatype.ParseXdmDuration("PT24H", XdmAtomicType.DayTimeDuration);
        XdmAtomicValue tenDays = datatype.ParseXdmDuration("P10D", XdmAtomicType.DayTimeDuration);
        XdmAtomicValue months = datatype.ParseXdmDuration("P11M", XdmAtomicType.YearMonthDuration);
        XdmAtomicValue nextYear = datatype.ParseXdmDuration("P1Y", XdmAtomicType.YearMonthDuration);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.EqualXdmDuration(year, twelveMonths), Is.True);
            Assert.That(datatype.EqualXdmDuration(day, hours), Is.True);
            Assert.That(datatype.EqualXdmDuration(year, datatype.ParseXdmDuration("P365D", XdmAtomicType.Duration)), Is.False);
            Assert.That(datatype.CompareXdmDuration(tenDays, hours), Is.GreaterThan(0));
            Assert.That(datatype.CompareXdmDuration(months, nextYear), Is.LessThan(0));
            Assert.That(Assert.Throws<ArgumentException>(() => datatype.CompareXdmDuration(year, day)), Is.Not.Null);
            Assert.That(Assert.Throws<ArgumentException>(() => datatype.CompareXdmDuration(months, hours)), Is.Not.Null);
        });
    }

    [Test]
    public void ExtractsSignedNormalizedDurationComponents()
    {
        XdmDurationDatatype datatype = new XdmDurationDatatype();
        XdmAtomicValue value = datatype.ParseXdmDuration("-P20Y15M3DT10H12M12.5S", XdmAtomicType.Duration);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.YearsFromXdmDuration(value).CanonicalValue, Is.EqualTo("-21"));
            Assert.That(datatype.MonthsFromXdmDuration(value).CanonicalValue, Is.EqualTo("-3"));
            Assert.That(datatype.DaysFromXdmDuration(value).CanonicalValue, Is.EqualTo("-3"));
            Assert.That(datatype.HoursFromXdmDuration(value).CanonicalValue, Is.EqualTo("-10"));
            Assert.That(datatype.MinutesFromXdmDuration(value).CanonicalValue, Is.EqualTo("-12"));
            Assert.That(datatype.SecondsFromXdmDuration(value).CanonicalValue, Is.EqualTo("-12.5"));
            Assert.That(datatype.SecondsFromXdmDuration(datatype.ParseXdmDuration("-PT256S", XdmAtomicType.DayTimeDuration)).CanonicalValue, Is.EqualTo("-16"));
        });
    }

    [Test]
    public void AddsSubtractsAndNegatesOrderedDurationSubtypes()
    {
        XdmDurationDatatype datatype = new XdmDurationDatatype();
        XdmAtomicValue firstMonths = datatype.ParseXdmDuration("P2Y11M", XdmAtomicType.YearMonthDuration);
        XdmAtomicValue secondMonths = datatype.ParseXdmDuration("P3Y3M", XdmAtomicType.YearMonthDuration);
        XdmAtomicValue firstTime = datatype.ParseXdmDuration("P2DT12H5M", XdmAtomicType.DayTimeDuration);
        XdmAtomicValue secondTime = datatype.ParseXdmDuration("P5DT12H", XdmAtomicType.DayTimeDuration);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.AddXdmDurations(firstMonths, secondMonths).CanonicalValue, Is.EqualTo("P6Y2M"));
            Assert.That(datatype.SubtractXdmDurations(firstMonths, secondMonths).CanonicalValue, Is.EqualTo("-P4M"));
            Assert.That(datatype.AddXdmDurations(firstTime, secondTime).CanonicalValue, Is.EqualTo("P8DT5M"));
            Assert.That(datatype.SubtractXdmDurations(datatype.ParseXdmDuration("P2DT12H", XdmAtomicType.DayTimeDuration), datatype.ParseXdmDuration("P1DT10H30M", XdmAtomicType.DayTimeDuration)).CanonicalValue, Is.EqualTo("P1DT1H30M"));
            Assert.That(datatype.NegateXdmDuration(firstTime).CanonicalValue, Is.EqualTo("-P2DT12H5M"));
            Assert.That(Assert.Throws<ArgumentException>(() => datatype.AddXdmDurations(firstMonths, firstTime)), Is.Not.Null);
        });
    }

    [Test]
    public void MultipliesAndDividesDurationsWithRequiredRounding()
    {
        XdmDurationDatatype datatype = new XdmDurationDatatype();
        XdmFloatingDatatype floating = new XdmFloatingDatatype();
        XdmAtomicValue months = datatype.ParseXdmDuration("P2Y11M", XdmAtomicType.YearMonthDuration);
        XdmAtomicValue time = datatype.ParseXdmDuration("PT2H10M", XdmAtomicType.DayTimeDuration);
        XdmAtomicValue detailedTime = datatype.ParseXdmDuration("P1DT2H30M10.5S", XdmAtomicType.DayTimeDuration);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.MultiplyXdmDuration(months, floating.ParseXdmFloating("2.3", XdmAtomicType.Double)).CanonicalValue, Is.EqualTo("P6Y9M"));
            Assert.That(datatype.DivideXdmDuration(months, floating.ParseXdmFloating("1.5", XdmAtomicType.Double)).CanonicalValue, Is.EqualTo("P1Y11M"));
            Assert.That(datatype.MultiplyXdmDuration(time, floating.ParseXdmFloating("2.1", XdmAtomicType.Double)).CanonicalValue, Is.EqualTo("PT4H33M"));
            Assert.That(datatype.DivideXdmDuration(detailedTime, floating.ParseXdmFloating("1.5", XdmAtomicType.Double)).CanonicalValue, Is.EqualTo("PT17H40M7S"));
            Assert.That(datatype.MultiplyXdmDuration(datatype.ParseXdmDuration("-P35M", XdmAtomicType.YearMonthDuration), floating.ParseXdmFloating("1.5", XdmAtomicType.Double)).CanonicalValue, Is.EqualTo("-P4Y4M"));
        });
    }

    [Test]
    public void DividesDurationsToExactDecimalRatios()
    {
        XdmDurationDatatype datatype = new XdmDurationDatatype();
        XdmAtomicValue fortyMonths = datatype.ParseXdmDuration("P3Y4M", XdmAtomicType.YearMonthDuration);
        XdmAtomicValue negativeSixteenMonths = datatype.ParseXdmDuration("-P1Y4M", XdmAtomicType.YearMonthDuration);
        XdmAtomicValue longTime = datatype.ParseXdmDuration("P2DT53M11S", XdmAtomicType.DayTimeDuration);
        XdmAtomicValue oneDayTenHours = datatype.ParseXdmDuration("P1DT10H", XdmAtomicType.DayTimeDuration);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.DivideXdmDurationByDuration(fortyMonths, negativeSixteenMonths).CanonicalValue, Is.EqualTo("-2.5"));
            Assert.That(datatype.DivideXdmDurationByDuration(fortyMonths, datatype.ParseXdmDuration("P1M", XdmAtomicType.YearMonthDuration)).CanonicalValue, Is.EqualTo("40"));
            Assert.That(datatype.DivideXdmDurationByDuration(longTime, oneDayTenHours).CanonicalValue, Is.EqualTo("1.437834967320261438"));
        });
    }

    [Test]
    public void RoundsDayTimeDurationSecondLengthsHalfToEven()
    {
        XdmDurationDatatype datatype = new XdmDurationDatatype();

        Assert.Multiple(() =>
        {
            Assert.That(datatype.RoundXdmDayTimeDuration(datatype.ParseXdmDuration("PT1.234S", XdmAtomicType.DayTimeDuration), 2).CanonicalValue, Is.EqualTo("PT1.23S"));
            Assert.That(datatype.RoundXdmDayTimeDuration(datatype.ParseXdmDuration("PT1.235S", XdmAtomicType.DayTimeDuration), 2).CanonicalValue, Is.EqualTo("PT1.24S"));
            Assert.That(datatype.RoundXdmDayTimeDuration(datatype.ParseXdmDuration("PT1.245S", XdmAtomicType.DayTimeDuration), 2).CanonicalValue, Is.EqualTo("PT1.24S"));
        });
    }

    [Test]
    public void ReportsDurationSpecificNanZeroAndOverflowErrors()
    {
        XdmDurationDatatype datatype = new XdmDurationDatatype();
        XdmFloatingDatatype floating = new XdmFloatingDatatype();
        XdmAtomicValue duration = datatype.ParseXdmDuration("P1M", XdmAtomicType.YearMonthDuration);
        XdmAtomicValue zeroDuration = datatype.ParseXdmDuration("P0M", XdmAtomicType.YearMonthDuration);
        XdmAtomicValue nan = floating.ParseXdmFloating("NaN", XdmAtomicType.Double);
        XdmAtomicValue infinity = floating.ParseXdmFloating("INF", XdmAtomicType.Double);
        XdmAtomicValue zero = floating.ParseXdmFloating("0", XdmAtomicType.Double);
        XdmAtomicValue maximum = datatype.ParseXdmDuration("P" + new string('9', 10000) + "M", XdmAtomicType.YearMonthDuration);

        XPathDynamicException nanError = Assert.Throws<XPathDynamicException>(() => datatype.MultiplyXdmDuration(duration, nan))!;
        XPathDynamicException infinityError = Assert.Throws<XPathDynamicException>(() => datatype.MultiplyXdmDuration(duration, infinity))!;
        XPathDynamicException zeroError = Assert.Throws<XPathDynamicException>(() => datatype.DivideXdmDuration(duration, zero))!;
        XPathDynamicException ratioError = Assert.Throws<XPathDynamicException>(() => datatype.DivideXdmDurationByDuration(duration, zeroDuration))!;
        XPathDynamicException additionError = Assert.Throws<XPathDynamicException>(() => datatype.AddXdmDurations(maximum, maximum))!;
        XdmNumericParseResult lexicalOverflow = datatype.TryParseXdmDuration("P" + new string('9', 10001) + "M", XdmAtomicType.YearMonthDuration);

        Assert.Multiple(() =>
        {
            Assert.That(nanError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCA0005));
            Assert.That(infinityError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODT0002));
            Assert.That(zeroError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODT0002));
            Assert.That(ratioError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
            Assert.That(additionError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODT0002));
            Assert.That(lexicalOverflow.ErrorCode, Is.EqualTo(XPathErrorCodes.FOCA0002));
            Assert.That(datatype.DivideXdmDuration(duration, infinity).CanonicalValue, Is.EqualTo("P0M"));
        });
    }

    [Test]
    public void FactoryCreatesSerializableCanonicalDurationValues()
    {
        XdmAtomicValue value = new XdmFactory().CreateXdmDurationAtomicValue("PT120S", XdmAtomicType.DayTimeDuration);
        XdmSequence sequence = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [value]
        };
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };
        XmlSerializationOptions options = new XmlSerializationOptions { Method = XmlSerializationMethod.Text };

        new XmlSerializer().SerializeXdmSequence(sequence, output, options);

        Assert.That(writer.ToString(), Is.EqualTo("PT2M"));
    }
}
