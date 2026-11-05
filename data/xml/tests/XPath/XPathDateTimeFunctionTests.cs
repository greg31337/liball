using xml;

namespace xml.Tests;

[TestFixture]
public class XPathDateTimeFunctionTests
{
    [Test]
    public void AppliesWidthsToFractionalSecondsAndIgnoresThemForRomanNumerals()
    {
        XdmSequence result = Evaluate("(format-time(xs:time('09:15:06.456'), '[f,4-4]'), format-time(xs:time('09:15:06.456'), '[f,2-2]'), format-date(xs:date('0817-01-01'), '[Yi,4-4]'), format-date(xs:date('1004-01-01'), '[Yi,4-4]'))", null);

        Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "4560", "45", "dcccxvii", "miv " }));
    }

    [Test]
    public void UsesCultureAbbreviationsAndPresentationCaseForNames()
    {
        XdmSequence result = Evaluate("(format-date(xs:date('2003-12-08'), '[FNn,3-4]', 'en', (), ()), format-time(xs:time('09:00:00'), '[PNn]'))", null);

        Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "Mon", "Am" }));
    }

    [Test]
    public void ExtractsSignedDurationComponentsAndHandlesEmptyValues()
    {
        XdmSequence result = Evaluate("(years-from-duration(xs:duration('-P20Y15M3DT10H12M12.5S')), months-from-duration(xs:duration('-P20Y15M3DT10H12M12.5S')), days-from-duration(xs:duration('-P20Y15M3DT10H12M12.5S')), hours-from-duration(xs:duration('-P20Y15M3DT10H12M12.5S')), minutes-from-duration(xs:duration('-P20Y15M3DT10H12M12.5S')), seconds-from-duration(xs:duration('-P20Y15M3DT10H12M12.5S')), years-from-duration(()))", null);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "-21", "-3", "-3", "-10", "-12", "-12.5" }));
    }

    [Test]
    public void ExtractsDateTimeDateAndTimeComponentsWithOptionalTimezones()
    {
        XdmSequence result = Evaluate("(year-from-dateTime(xs:dateTime('-12345-06-07T08:09:10.25-05:30')), month-from-dateTime(xs:dateTime('2001-06-07T08:09:10.25-05:30')), day-from-dateTime(xs:dateTime('2001-06-07T08:09:10.25-05:30')), hours-from-dateTime(xs:dateTime('2001-06-07T08:09:10.25-05:30')), minutes-from-dateTime(xs:dateTime('2001-06-07T08:09:10.25-05:30')), seconds-from-dateTime(xs:dateTime('2001-06-07T08:09:10.25-05:30')), timezone-from-dateTime(xs:dateTime('2001-06-07T08:09:10.25-05:30')), year-from-date(xs:date('2026-09-13Z')), timezone-from-date(xs:date('2026-09-13')), hours-from-time(xs:time('23:58:57.5Z')), timezone-from-time(xs:time('23:58:57.5Z'))) ", null);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "-12345", "6", "7", "8", "9", "10.25", "-PT5H30M", "2026", "23", "PT0S" }));
    }

    [Test]
    public void ConstructsDateTimesAndRejectsConflictingTimezones()
    {
        XdmSequence local = Evaluate("dateTime(xs:date('1999-12-31'), xs:time('12:00:00'))", null);
        XdmSequence zoned = Evaluate("dateTime(xs:date('1999-12-31-05:00'), xs:time('12:00:00'))", null);
        XdmSequence empty = Evaluate("dateTime((), xs:time('12:00:00'))", null);
        XPathDynamicException conflict = Assert.Throws<XPathDynamicException>(() => Evaluate("dateTime(xs:date('1999-12-31-05:00'), xs:time('12:00:00+01:00'))", null))!;

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(local), Is.EqualTo(new[] { "1999-12-31T12:00:00" }));
            Assert.That(StringValues(zoned), Is.EqualTo(new[] { "1999-12-31T12:00:00-05:00" }));
            Assert.That(empty.Count, Is.Zero);
            Assert.That(conflict.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0008));
        });
    }

    [Test]
    public void AdjustsDateTimesDatesAndTimesToExplicitAndImplicitTimezones()
    {
        XPathDynamicContext context = new XPathDynamicContext { ImplicitTimezoneMinutes = -300 };
        XdmSequence result = Evaluate("(adjust-dateTime-to-timezone(xs:dateTime('2002-03-07T10:00:00-07:00')), adjust-dateTime-to-timezone(xs:dateTime('2002-03-07T10:00:00'), xs:dayTimeDuration('-PT10H')), adjust-date-to-timezone(xs:date('2002-03-07-07:00'), xs:dayTimeDuration('-PT10H')), adjust-time-to-timezone(xs:time('10:00:00-07:00'), xs:dayTimeDuration('PT10H')), adjust-time-to-timezone(xs:time('10:00:00Z'), ()))", context);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "2002-03-07T12:00:00-05:00", "2002-03-07T10:00:00-10:00", "2002-03-06-10:00", "03:00:00+10:00", "10:00:00" }));
    }

    [Test]
    public void RejectsInvalidTimezoneDurations()
    {
        XPathDynamicException fractional = Assert.Throws<XPathDynamicException>(() => Evaluate("adjust-time-to-timezone(xs:time('10:00:00'), xs:dayTimeDuration('PT1M0.5S'))", null))!;
        XPathDynamicException excessive = Assert.Throws<XPathDynamicException>(() => Evaluate("adjust-date-to-timezone(xs:date('2000-01-01'), xs:dayTimeDuration('PT14H1M'))", null))!;

        Assert.Multiple(() =>
        {
            Assert.That(fractional.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODT0003));
            Assert.That(excessive.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODT0003));
        });
    }

    [Test]
    public void EvaluatesTemporalAndDurationArithmetic()
    {
        XdmSequence result = Evaluate("(xs:yearMonthDuration('P2Y11M') + xs:yearMonthDuration('P3Y3M'), xs:dayTimeDuration('P2DT12H') - xs:dayTimeDuration('P1DT10H30M'), xs:date('2000-10-31') + xs:yearMonthDuration('P1M'), xs:dayTimeDuration('P3D') + xs:dateTime('2000-01-01T00:00:00Z'), xs:time('23:00:00') + xs:dayTimeDuration('PT2H'), xs:dateTime('2000-01-02T03:00:00Z') - xs:dateTime('2000-01-01T00:00:00Z'), xs:date('2000-01-03') - xs:dayTimeDuration('P2D'), xs:yearMonthDuration('P10M') * 2.5, 2 * xs:dayTimeDuration('PT1.5S'), xs:yearMonthDuration('P10M') div 2, xs:dayTimeDuration('P3D') div xs:dayTimeDuration('P1D'))", null);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "P6Y2M", "P1DT1H30M", "2000-11-30", "2000-01-04T00:00:00Z", "01:00:00", "P1DT3H", "2000-01-01", "P2Y1M", "PT3S", "P5M", "3" }));
    }

    [Test]
    public void ComparesTemporalValuesUsingTheImplicitTimezone()
    {
        XPathDynamicContext context = new XPathDynamicContext { ImplicitTimezoneMinutes = -300 };
        XdmSequence result = Evaluate("(xs:dateTime('2002-10-10T12:00:00') eq xs:dateTime('2002-10-10T17:00:00Z'), xs:date('2000-01-01Z') lt xs:date('2000-01-02Z'), xs:yearMonthDuration('P11M') lt xs:yearMonthDuration('P1Y'))", context);

        Assert.That(result.AtomicValues!.Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { true, true, true }));
    }

    [Test]
    public void FormatsDateAndTimePicturesIncludingNamesWidthsAndEscapedBrackets()
    {
        XdmSequence result = Evaluate("(format-date(xs:date('2002-12-31'), '[Y0001]-[M01]-[D01]'), format-date(xs:date('2002-12-31'), '[[[D1o] [MNn], [Y0001]]]','en',(),()), format-date(xs:date('2002-12-31'), '[D01] [MN,*-3] [Y0001]','en',(),()), format-time(xs:time('15:58:45.762+02:00'), '[h]:[m01]:[s01].[f001] [PN] [z]','en',(),()))", null);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "2002-12-31", "[31st December, 2002]", "31 DEC 2002", "3:58:45.762 PM GMT+02:00" }));
    }

    [Test]
    public void FormatsCalendarNumbersWordsWeeksAndTimezonePresentations()
    {
        XdmSequence result = Evaluate("(format-date(xs:date('2002-12-31'), '[MI] [Dwo] [YWw] [Y01]'), format-date(xs:date('2013-02-01'), '[F] [d] [W] [w]'), format-date(xs:date('2006-01-30'), '[w]'), format-time(xs:time('12:00:00Z'), '[Z] [Z00:00t] [ZZ]'), format-time(xs:time('12:00:00'), '[ZZ]'), format-date(xs:date('2002-12-31'), '[C] [Y]', (), 'Q{}AD', ()), format-date(xs:date('2002-12-31'), '[Y٠٠٠١]'))", null);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "XII thirty-first Two Thousand and Two 02", "friday 32 5 5", "5", "+00:00 Z Z", "J", "gregorian 2002", "٢٠٠٢" }));
    }

    [Test]
    public void UsesRequestedLanguagesPlacesAndDocumentedFallbackMarkers()
    {
        XdmSequence result = Evaluate("(format-date(xs:date('2002-12-31'), '[D] [MNn] [Y]', 'de', (), ()), format-date(xs:date('2002-12-31'), '[Y]', 'not_a_language', (), ()), format-date(xs:date('2002-12-31'), '[Y]', (), 'AH', ()), format-dateTime(xs:dateTime('2010-02-15T12:00:00Z'), '[H01]:[m01] [Z]', 'en', (), 'America/New_York'))", null);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "31 Dezember 2002", "[Language: en]2002", "[Calendar: AD]2002", "07:00 -05:00" }));
    }

    [Test]
    public void ReportsDatePictureSyntaxCalendarAndUnavailableComponentErrors()
    {
        XPathDynamicException picture = Assert.Throws<XPathDynamicException>(() => Evaluate("format-date(xs:date('2000-01-01'), '[Y')", null))!;
        XPathDynamicException marker = Assert.Throws<XPathDynamicException>(() => Evaluate("format-date(xs:date('2000-01-01'), '[Q]')", null))!;
        XPathDynamicException calendar = Assert.Throws<XPathDynamicException>(() => Evaluate("format-date(xs:date('2000-01-01'), '[Y]', (), 'unknown', ())", null))!;
        XPathDynamicException component = Assert.Throws<XPathDynamicException>(() => Evaluate("format-date(xs:date('2000-01-01'), '[H]')", null))!;

        Assert.Multiple(() =>
        {
            Assert.That(picture.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOFD1340));
            Assert.That(marker.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOFD1340));
            Assert.That(calendar.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOFD1340));
            Assert.That(component.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOFD1350));
        });
    }

    [TestCase("years-from-duration()")]
    [TestCase("dateTime(xs:date('2000-01-01'))")]
    [TestCase("adjust-date-to-timezone(xs:date('2000-01-01'), (), ())")]
    [TestCase("format-time(xs:time('12:00:00'), '[H]', 'en')")]
    public void RejectsUnsupportedDateTimeFunctionArities(string source)
    {
        XPathStaticException error = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath(source, new XPathStaticContext()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
    }

    private static XdmSequence Evaluate(string source, XPathDynamicContext? context)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, new XPathStaticContext());
        return new XPathEvaluator().EvaluateXPath(expression, context == null ? new XPathDynamicContext() : context).Value!;
    }

    private static string[] StringValues(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue!).ToArray();
    }
}
