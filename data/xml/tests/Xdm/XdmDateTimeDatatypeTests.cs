using xml;

namespace xml.Tests;

[TestFixture]
public class XdmDateTimeDatatypeTests
{
    [TestCase(XdmAtomicType.DateTime, "2002-10-10T12:00:00-05:00", "2002-10-10T12:00:00-05:00")]
    [TestCase(XdmAtomicType.DateTime, "2000-12-31T24:00:00+00:00", "2001-01-01T00:00:00Z")]
    [TestCase(XdmAtomicType.DateTimeStamp, "2000-02-29T01:02:03.450Z", "2000-02-29T01:02:03.45Z")]
    [TestCase(XdmAtomicType.Date, "0000-02-29Z", "0000-02-29Z")]
    [TestCase(XdmAtomicType.Date, "-0004-02-29", "-0004-02-29")]
    [TestCase(XdmAtomicType.Time, "24:00:00", "00:00:00")]
    [TestCase(XdmAtomicType.Time, "12:34:56.780+02:30", "12:34:56.78+02:30")]
    [TestCase(XdmAtomicType.GYearMonth, "2024-02Z", "2024-02Z")]
    [TestCase(XdmAtomicType.GYear, "2024-05:00", "2024-05:00")]
    [TestCase(XdmAtomicType.GMonthDay, "--02-29", "--02-29")]
    [TestCase(XdmAtomicType.GMonth, "--12+14:00", "--12+14:00")]
    [TestCase(XdmAtomicType.GDay, "---31Z", "---31Z")]
    public void ParsesAndCanonicalizesEveryDateTimeType(XdmAtomicType type, string lexical, string canonical)
    {
        XdmAtomicValue value = new XdmDateTimeDatatype().ParseXdmDateTime(lexical, type);

        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(type));
            Assert.That(value.LexicalValue, Is.EqualTo(lexical));
            Assert.That(value.CanonicalValue, Is.EqualTo(canonical));
            Assert.That(value.StringValue, Is.EqualTo(canonical));
            Assert.That(value.ExtendedValue, Is.TypeOf<XdmDateTimeStorage>());
            Assert.That(value.IsImmutable, Is.True);
        });
    }

    [TestCase(XdmAtomicType.DateTime, "2000-01-01 00:00:00")]
    [TestCase(XdmAtomicType.DateTime, "2001-02-29T00:00:00")]
    [TestCase(XdmAtomicType.DateTime, "2000-01-01T24:00:01")]
    [TestCase(XdmAtomicType.DateTime, "2000-01-01T23:60:00")]
    [TestCase(XdmAtomicType.DateTime, "2000-01-01T23:59:60")]
    [TestCase(XdmAtomicType.DateTime, "2000-01-01T00:00:00+14:01")]
    [TestCase(XdmAtomicType.DateTime, "2000-01-01T00:00:00+15:00")]
    [TestCase(XdmAtomicType.DateTime, "02000-01-01T00:00:00")]
    [TestCase(XdmAtomicType.DateTime, "-0000-01-01T00:00:00")]
    [TestCase(XdmAtomicType.DateTimeStamp, "2000-01-01T00:00:00")]
    [TestCase(XdmAtomicType.Date, "2000-04-31")]
    [TestCase(XdmAtomicType.Time, "1:00:00")]
    [TestCase(XdmAtomicType.Time, "00:00:00.")]
    [TestCase(XdmAtomicType.GMonthDay, "--02-30")]
    [TestCase(XdmAtomicType.GMonth, "--00")]
    [TestCase(XdmAtomicType.GDay, "---32")]
    public void RejectsInvalidDateTimeLexicalForms(XdmAtomicType type, string lexical)
    {
        XdmDateTimeDatatype datatype = new XdmDateTimeDatatype();
        XdmNumericParseResult result = datatype.TryParseXdmDateTime(lexical, type);
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmDateTime(lexical, type))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
        });
    }

    [Test]
    public void SupportsArbitraryYearsAndProlepticGregorianDayArithmetic()
    {
        XdmDateTimeDatatype datatype = new XdmDateTimeDatatype();
        XdmAtomicValue farFuture = datatype.ParseXdmDateTime("12345678901234567890-12-31T23:59:59Z", XdmAtomicType.DateTime);
        XdmAtomicValue oneSecond = new XdmDurationDatatype().ParseXdmDuration("PT1S", XdmAtomicType.DayTimeDuration);
        XdmAtomicValue next = datatype.AddXdmDurationToDateTime(farFuture, oneSecond);
        XdmAtomicValue beforeYearZero = datatype.ParseXdmDateTime("-0001-12-31T23:59:59Z", XdmAtomicType.DateTime);
        XdmAtomicValue yearZero = datatype.AddXdmDurationToDateTime(beforeYearZero, oneSecond);

        Assert.Multiple(() =>
        {
            Assert.That(next.CanonicalValue, Is.EqualTo("12345678901234567891-01-01T00:00:00Z"));
            Assert.That(yearZero.CanonicalValue, Is.EqualTo("0000-01-01T00:00:00Z"));
        });
    }

    [Test]
    public void ComparesInstantsUsingExplicitAndImplicitTimezones()
    {
        XdmDateTimeDatatype datatype = new XdmDateTimeDatatype();
        XdmAtomicValue eastern = datatype.ParseXdmDateTime("2002-10-10T12:00:00-05:00", XdmAtomicType.DateTime);
        XdmAtomicValue utc = datatype.ParseXdmDateTime("2002-10-10T17:00:00Z", XdmAtomicType.DateTimeStamp);
        XdmAtomicValue local = datatype.ParseXdmDateTime("2002-10-10T12:00:00", XdmAtomicType.DateTime);
        XdmAtomicValue earlier = datatype.ParseXdmDateTime("2002-10-10T16:59:59Z", XdmAtomicType.DateTime);
        XdmAtomicValue zero = datatype.ParseXdmDateTime("2000Z", XdmAtomicType.GYear);
        XdmAtomicValue offsetZero = datatype.ParseXdmDateTime("2000-01-01+00:00", XdmAtomicType.Date);
        XdmAtomicValue firstDay = datatype.ParseXdmDateTime("---15-11:00", XdmAtomicType.GDay);
        XdmAtomicValue nextDay = datatype.ParseXdmDateTime("---16+13:00", XdmAtomicType.GDay);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.CompareXdmDateTime(eastern, utc, 0), Is.Zero);
            Assert.That(datatype.CompareXdmDateTime(local, utc, -300), Is.Zero);
            Assert.That(datatype.CompareXdmDateTime(earlier, utc, 0), Is.LessThan(0));
            Assert.That(datatype.EqualXdmDateTime(eastern, utc, 0), Is.True);
            Assert.That(datatype.EqualXdmDateTime(zero, datatype.ParseXdmDateTime("2000Z", XdmAtomicType.GYear), 0), Is.True);
            Assert.That(datatype.EqualXdmDateTime(firstDay, nextDay, 0), Is.True);
            Assert.That(datatype.EqualXdmDateTime(offsetZero, offsetZero, 0), Is.True);
            Assert.That(Assert.Throws<ArgumentException>(() => datatype.CompareXdmDateTime(zero, zero, 0)), Is.Not.Null);
        });
    }

    [Test]
    public void ExtractsDateTimeComponentsAndOptionalTimezone()
    {
        XdmDateTimeDatatype datatype = new XdmDateTimeDatatype();
        XdmAtomicValue value = datatype.ParseXdmDateTime("-12345-06-07T08:09:10.25-05:30", XdmAtomicType.DateTime);
        XdmAtomicValue withoutTimezone = datatype.ParseXdmDateTime("2000-01-01", XdmAtomicType.Date);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.YearFromXdmDateTime(value).CanonicalValue, Is.EqualTo("-12345"));
            Assert.That(datatype.MonthFromXdmDateTime(value).CanonicalValue, Is.EqualTo("6"));
            Assert.That(datatype.DayFromXdmDateTime(value).CanonicalValue, Is.EqualTo("7"));
            Assert.That(datatype.HoursFromXdmDateTime(value).CanonicalValue, Is.EqualTo("8"));
            Assert.That(datatype.MinutesFromXdmDateTime(value).CanonicalValue, Is.EqualTo("9"));
            Assert.That(datatype.SecondsFromXdmDateTime(value).CanonicalValue, Is.EqualTo("10.25"));
            Assert.That(datatype.TimezoneFromXdmDateTime(value)!.CanonicalValue, Is.EqualTo("-PT5H30M"));
            Assert.That(datatype.TimezoneFromXdmDateTime(withoutTimezone), Is.Null);
        });
    }

    [Test]
    public void AdjustsTimezonesWhilePreservingInstantsAndAttachesImplicitZones()
    {
        XdmDateTimeDatatype datatype = new XdmDateTimeDatatype();
        XdmAtomicValue source = datatype.ParseXdmDateTime("2002-03-07T10:00:00-07:00", XdmAtomicType.DateTime);
        XdmAtomicValue local = datatype.ParseXdmDateTime("2002-03-07T10:00:00", XdmAtomicType.DateTime);
        XdmAtomicValue shifted = datatype.AdjustXdmDateTimeTimezone(source, true, -600);
        XdmAtomicValue nextDay = datatype.AdjustXdmDateTimeTimezone(source, true, 600);
        XdmAtomicValue removed = datatype.AdjustXdmDateTimeTimezone(source, false, 0);
        XdmAtomicValue attached = datatype.AdjustXdmDateTimeTimezone(local, true, -300);
        XdmAtomicValue implicitValue = datatype.ApplyXdmImplicitTimezone(local, 330);

        Assert.Multiple(() =>
        {
            Assert.That(shifted.CanonicalValue, Is.EqualTo("2002-03-07T07:00:00-10:00"));
            Assert.That(nextDay.CanonicalValue, Is.EqualTo("2002-03-08T03:00:00+10:00"));
            Assert.That(removed.CanonicalValue, Is.EqualTo("2002-03-07T10:00:00"));
            Assert.That(attached.CanonicalValue, Is.EqualTo("2002-03-07T10:00:00-05:00"));
            Assert.That(implicitValue.CanonicalValue, Is.EqualTo("2002-03-07T10:00:00+05:30"));
            Assert.That(Assert.Throws<XPathDynamicException>(() => datatype.AdjustXdmDateTimeTimezone(source, true, 841))!.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODT0003));
        });
    }

    [Test]
    public void SubtractsDateTimesDatesAndTimesUsingTheImplicitTimezone()
    {
        XdmDateTimeDatatype datatype = new XdmDateTimeDatatype();
        XdmAtomicValue dateTimeLeft = datatype.ParseXdmDateTime("2000-10-30T06:12:00", XdmAtomicType.DateTime);
        XdmAtomicValue dateTimeRight = datatype.ParseXdmDateTime("1999-11-28T09:00:00Z", XdmAtomicType.DateTime);
        XdmAtomicValue dateLeft = datatype.ParseXdmDateTime("2000-10-30", XdmAtomicType.Date);
        XdmAtomicValue dateRight = datatype.ParseXdmDateTime("1999-11-28Z", XdmAtomicType.Date);
        XdmAtomicValue timeLeft = datatype.ParseXdmDateTime("11:12:00Z", XdmAtomicType.Time);
        XdmAtomicValue timeRight = datatype.ParseXdmDateTime("04:00:00", XdmAtomicType.Time);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.SubtractXdmDateTimeValues(dateTimeLeft, dateTimeRight, -300).CanonicalValue, Is.EqualTo("P337DT2H12M"));
            Assert.That(datatype.SubtractXdmDateTimeValues(dateLeft, dateRight, 300).CanonicalValue, Is.EqualTo("P336DT19H"));
            Assert.That(datatype.SubtractXdmDateTimeValues(timeLeft, timeRight, -300).CanonicalValue, Is.EqualTo("PT2H12M"));
        });
    }

    [Test]
    public void AddsAndSubtractsBothDurationSubtypesFromCalendarValues()
    {
        XdmDateTimeDatatype datatype = new XdmDateTimeDatatype();
        XdmDurationDatatype durations = new XdmDurationDatatype();
        XdmAtomicValue dateTime = datatype.ParseXdmDateTime("2000-10-30T11:12:00", XdmAtomicType.DateTime);
        XdmAtomicValue date = datatype.ParseXdmDateTime("2000-02-29Z", XdmAtomicType.Date);
        XdmAtomicValue time = datatype.ParseXdmDateTime("23:30:00Z", XdmAtomicType.Time);
        XdmAtomicValue yearMonths = durations.ParseXdmDuration("P1Y2M", XdmAtomicType.YearMonthDuration);
        XdmAtomicValue oneYear = durations.ParseXdmDuration("P1Y", XdmAtomicType.YearMonthDuration);
        XdmAtomicValue dayTime = durations.ParseXdmDuration("P3DT1H15M", XdmAtomicType.DayTimeDuration);
        XdmAtomicValue twoHours = durations.ParseXdmDuration("PT2H", XdmAtomicType.DayTimeDuration);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.AddXdmDurationToDateTime(dateTime, yearMonths).CanonicalValue, Is.EqualTo("2001-12-30T11:12:00"));
            Assert.That(datatype.AddXdmDurationToDateTime(dateTime, dayTime).CanonicalValue, Is.EqualTo("2000-11-02T12:27:00"));
            Assert.That(datatype.SubtractXdmDurationFromDateTime(dateTime, dayTime).CanonicalValue, Is.EqualTo("2000-10-27T09:57:00"));
            Assert.That(datatype.SubtractXdmDurationFromDateTime(date, oneYear).CanonicalValue, Is.EqualTo("1999-02-28Z"));
            Assert.That(datatype.AddXdmDurationToDateTime(time, twoHours).CanonicalValue, Is.EqualTo("01:30:00Z"));
            Assert.That(Assert.Throws<ArgumentException>(() => datatype.AddXdmDurationToDateTime(time, yearMonths)), Is.Not.Null);
        });
    }

    [Test]
    public void ReportsCalendarOverflowWithTheStandardError()
    {
        XdmDateTimeDatatype datatype = new XdmDateTimeDatatype();
        XdmDurationDatatype durations = new XdmDurationDatatype();
        XdmAtomicValue date = datatype.ParseXdmDateTime(new string('9', 10000) + "-12-31", XdmAtomicType.Date);
        XdmAtomicValue months = durations.ParseXdmDuration("P1M", XdmAtomicType.YearMonthDuration);
        XdmNumericParseResult lexicalOverflow = datatype.TryParseXdmDateTime(new string('9', 10001) + "-01-01", XdmAtomicType.Date);

        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.AddXdmDurationToDateTime(date, months))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODT0001));
            Assert.That(lexicalOverflow.ErrorCode, Is.EqualTo(XPathErrorCodes.FOCA0002));
        });
    }

    [Test]
    public void FactoryCreatesSerializableCanonicalDateTimeValues()
    {
        XdmAtomicValue value = new XdmFactory().CreateXdmDateTimeAtomicValue("2000-01-01T24:00:00+00:00", XdmAtomicType.DateTime);
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

        Assert.That(writer.ToString(), Is.EqualTo("2000-01-02T00:00:00Z"));
    }
}
