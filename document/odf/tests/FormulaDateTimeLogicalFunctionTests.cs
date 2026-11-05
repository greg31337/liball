namespace odf.Tests;

public class FormulaDateTimeLogicalFunctionTests
{
    [Test]
    public void PublishesDateTimeAndLogicalConformanceMetadata()
    {
        OdfFormulaFunctionInfo date = FormulaFunctionCatalog.Find(
            OdfFormulaNames.DATE)!;
        OdfFormulaFunctionInfo dateDifference = FormulaFunctionCatalog.Find(
            OdfFormulaNames.DATEDIF)!;
        OdfFormulaFunctionInfo networkDays = FormulaFunctionCatalog.Find(
            OdfFormulaNames.NETWORKDAYS)!;
        OdfFormulaFunctionInfo easter = FormulaFunctionCatalog.Find(
            OdfFormulaNames.EASTERSUNDAY)!;
        OdfFormulaFunctionInfo ifError = FormulaFunctionCatalog.Find(
            OdfFormulaNames.IFERROR)!;
        OdfFormulaFunctionInfo now = FormulaFunctionCatalog.Find(
            OdfFormulaNames.NOW)!;
        OdfFormula parsed = FormulaParser.Parse("=IF(TRUE();TODAY();0)", 32);

        Assert.Multiple(() =>
        {
            Assert.That(date.Group, Is.EqualTo("DateTime"));
            Assert.That(date.Conformance,
                Is.EqualTo(OdfFormulaConformance.Small));
            Assert.That(date.IntroducedVersion,
                Is.EqualTo(OdfVersion.Version12));
            Assert.That(dateDifference.Conformance,
                Is.EqualTo(OdfFormulaConformance.Large));
            Assert.That(networkDays.Conformance,
                Is.EqualTo(OdfFormulaConformance.Medium));
            Assert.That(easter.IntroducedVersion,
                Is.EqualTo(OdfVersion.Version14));
            Assert.That(easter.Conformance,
                Is.EqualTo(OdfFormulaConformance.Unknown));
            Assert.That(easter.IsVolatile, Is.True);
            Assert.That(ifError.Group, Is.EqualTo("Logical"));
            Assert.That(ifError.Conformance,
                Is.EqualTo(OdfFormulaConformance.Large));
            Assert.That(now.IsVolatile, Is.True);
            Assert.That(parsed.Root!.Function!.Name,
                Is.EqualTo(OdfFormulaNames.IF));
            Assert.That(parsed.Root.Children[1].Function!.Name,
                Is.EqualTo(OdfFormulaNames.TODAY));
        });
    }

    [Test]
    public void EvaluatesEveryLogicalFunctionAndSelectedBranches()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult error = FormulaValueModel.CreateError(
            OdfFormulaError.DivisionByZero);
        OdfFormulaResult notAvailable = FormulaValueModel.CreateError(
            OdfFormulaError.NotAvailable);
        OdfFormulaResult values = FormulaValueModel.CreateArray(
            [
                FormulaValueModel.CreateBoolean(true),
                FormulaValueModel.CreateNumber(1),
                FormulaValueModel.CreateBoolean(false)
            ],
            1,
            3);

        OdfFormulaResult and = Evaluate(
            OdfFormulaNames.AND, [values], context);
        OdfFormulaResult or = Evaluate(
            OdfFormulaNames.OR, [values], context);
        OdfFormulaResult xor = Evaluate(
            OdfFormulaNames.XOR, [values], context);
        OdfFormulaResult trueValue = Evaluate(
            OdfFormulaNames.TRUE, [], context);
        OdfFormulaResult falseValue = Evaluate(
            OdfFormulaNames.FALSE, [], context);
        OdfFormulaResult not = Evaluate(
            OdfFormulaNames.NOT,
            [FormulaValueModel.CreateBoolean(true)],
            context);
        OdfFormulaResult selected = Evaluate(
            OdfFormulaNames.IF,
            [
                FormulaValueModel.CreateBoolean(true),
                FormulaValueModel.CreateText("chosen"),
                error
            ],
            context);
        OdfFormulaResult missingTrue = Evaluate(
            OdfFormulaNames.IF,
            [FormulaValueModel.CreateBoolean(true),
                FormulaValueModel.CreateEmpty()],
            context);
        OdfFormulaResult ifError = Evaluate(
            OdfFormulaNames.IFERROR,
            [error, FormulaValueModel.CreateText("fallback")],
            context);
        OdfFormulaResult ifNa = Evaluate(
            OdfFormulaNames.IFNA,
            [notAvailable, FormulaValueModel.CreateNumber(7)],
            context);
        OdfFormulaResult ifNaPreserves = Evaluate(
            OdfFormulaNames.IFNA,
            [error, FormulaValueModel.CreateNumber(7)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(and.BooleanValue, Is.False);
            Assert.That(or.BooleanValue, Is.True);
            Assert.That(xor.BooleanValue, Is.False);
            Assert.That(trueValue.BooleanValue, Is.True);
            Assert.That(falseValue.BooleanValue, Is.False);
            Assert.That(not.BooleanValue, Is.False);
            Assert.That(selected.StringValue, Is.EqualTo("chosen"));
            Assert.That(missingTrue.NumericValue, Is.Zero);
            Assert.That(ifError.StringValue, Is.EqualTo("fallback"));
            Assert.That(ifNa.NumericValue, Is.EqualTo(7));
            Assert.That(ifNaPreserves.Error,
                Is.EqualTo(OdfFormulaError.DivisionByZero));
        });
    }

    [Test]
    public void ConstructsRollsAndExtractsCalendarDates()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult leapDay = Evaluate(
            OdfFormulaNames.DATE,
            [Number(2024), Number(2), Number(29)],
            context);
        OdfFormulaResult rolled = Evaluate(
            OdfFormulaNames.DATE,
            [Number(2024), Number(13), Number(1)],
            context);
        OdfFormulaResult dateValue = Evaluate(
            OdfFormulaNames.DATEVALUE,
            [FormulaValueModel.CreateText("2024-03-01")],
            context);
        OdfFormulaResult day = Evaluate(
            OdfFormulaNames.DAY, [leapDay], context);
        OdfFormulaResult month = Evaluate(
            OdfFormulaNames.MONTH, [leapDay], context);
        OdfFormulaResult year = Evaluate(
            OdfFormulaNames.YEAR, [leapDay], context);
        OdfFormulaResult days = Evaluate(
            OdfFormulaNames.DAYS,
            [dateValue, Number(0)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(leapDay.Kind, Is.EqualTo(OdfValueKind.Date));
            Assert.That(leapDay.NumericValue, Is.EqualTo(59));
            Assert.That(leapDay.StringValue, Is.EqualTo("2024-02-29"));
            Assert.That(rolled.StringValue, Is.EqualTo("2025-01-01"));
            Assert.That(dateValue.NumericValue, Is.EqualTo(60));
            Assert.That(day.NumericValue, Is.EqualTo(29));
            Assert.That(month.NumericValue, Is.EqualTo(2));
            Assert.That(year.NumericValue, Is.EqualTo(2024));
            Assert.That(days.NumericValue, Is.EqualTo(60));
        });
    }

    [Test]
    public void EvaluatesDateDifferencesMonthEndsAndDayCountConventions()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult start = Date(2020, 1, 15, context);
        OdfFormulaResult end = Date(2022, 3, 20, context);
        OdfFormulaResult years = DateDif(
            start, end, "y", context);
        OdfFormulaResult months = DateDif(
            start, end, "m", context);
        OdfFormulaResult remainingMonths = DateDif(
            start, end, "ym", context);
        OdfFormulaResult remainingDays = DateDif(
            start, end, "md", context);
        OdfFormulaResult elapsedDays = DateDif(
            start, end, "d", context);
        OdfFormulaResult edate = Evaluate(
            OdfFormulaNames.EDATE,
            [Date(2024, 1, 31, context), Number(1)],
            context);
        OdfFormulaResult endOfMonth = Evaluate(
            OdfFormulaNames.EOMONTH,
            [Date(2024, 1, 15, context), Number(1)],
            context);
        OdfFormulaResult days360 = Evaluate(
            OdfFormulaNames.DAYS360,
            [Date(2024, 1, 31, context), Date(2024, 3, 31, context)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(years.NumericValue, Is.EqualTo(2));
            Assert.That(months.NumericValue, Is.EqualTo(26));
            Assert.That(remainingMonths.NumericValue, Is.EqualTo(2));
            Assert.That(remainingDays.NumericValue, Is.EqualTo(5));
            Assert.That(elapsedDays.NumericValue,
                Is.EqualTo(end.NumericValue - start.NumericValue));
            Assert.That(edate.StringValue, Is.EqualTo("2024-02-29"));
            Assert.That(endOfMonth.StringValue, Is.EqualTo("2024-02-29"));
            Assert.That(days360.NumericValue, Is.EqualTo(60));
        });
    }

    [Test]
    public void EvaluatesTimesWeekdaysAndIsoWeekNumbers()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult time = Evaluate(
            OdfFormulaNames.TIME,
            [Number(25), Number(30), Number(45)],
            context);
        OdfFormulaResult hour = Evaluate(
            OdfFormulaNames.HOUR, [time], context);
        OdfFormulaResult minute = Evaluate(
            OdfFormulaNames.MINUTE, [time], context);
        OdfFormulaResult second = Evaluate(
            OdfFormulaNames.SECOND, [time], context);
        OdfFormulaResult timeValue = Evaluate(
            OdfFormulaNames.TIMEVALUE,
            [FormulaValueModel.CreateText("12:30:00")],
            context);
        OdfFormulaResult januaryFirst = Date(2021, 1, 1, context);
        OdfFormulaResult weekday = Evaluate(
            OdfFormulaNames.WEEKDAY,
            [januaryFirst, Number(2)],
            context);
        OdfFormulaResult isoWeek = Evaluate(
            OdfFormulaNames.ISOWEEKNUM,
            [januaryFirst],
            context);
        OdfFormulaResult week = Evaluate(
            OdfFormulaNames.WEEKNUM,
            [januaryFirst, Number(21)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(time.NumericValue,
                Is.EqualTo((25 * 3600 + 30 * 60 + 45) / 86400.0));
            Assert.That(hour.NumericValue, Is.EqualTo(1));
            Assert.That(minute.NumericValue, Is.EqualTo(30));
            Assert.That(second.NumericValue, Is.EqualTo(45));
            Assert.That(timeValue.NumericValue, Is.EqualTo(0.5208333333333334)
                .Within(0.000000000001));
            Assert.That(weekday.NumericValue, Is.EqualTo(5));
            Assert.That(isoWeek.NumericValue, Is.EqualTo(53));
            Assert.That(week.NumericValue, Is.EqualTo(53));
        });
    }

    [Test]
    public void CountsAndOffsetsWorkdaysWithCallerSuppliedHolidays()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult start = Date(2024, 1, 1, context);
        OdfFormulaResult end = Date(2024, 1, 7, context);
        OdfFormulaResult holidayDate = Date(2024, 1, 3, context);
        OdfFormulaReference referenceData = new OdfFormulaReference();
        OdfFormulaResult holidays = FormulaValueModel.CreateReference(
            referenceData,
            [holidayDate],
            1,
            1);
        OdfFormulaResult normal = Evaluate(
            OdfFormulaNames.NETWORKDAYS,
            [start, end],
            context);
        OdfFormulaResult excludingHoliday = Evaluate(
            OdfFormulaNames.NETWORKDAYS,
            [start, end, holidays],
            context);
        OdfFormulaResult workday = Evaluate(
            OdfFormulaNames.WORKDAY,
            [start, Number(5), holidays],
            context);
        OdfFormulaResult noWorkingDays = Evaluate(
            OdfFormulaNames.WORKDAY,
            [
                start,
                Number(1),
                FormulaValueModel.CreateEmpty(),
                FormulaValueModel.CreateArray(
                    [Number(1), Number(1), Number(1), Number(1),
                        Number(1), Number(1), Number(1)],
                    1,
                    7)
            ],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(normal.NumericValue, Is.EqualTo(5));
            Assert.That(excludingHoliday.NumericValue, Is.EqualTo(4));
            Assert.That(workday.StringValue, Is.EqualTo("2024-01-09"));
            Assert.That(noWorkingDays.Error,
                Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void UsesCallerSuppliedClockAndCalculatesEasterAndYearFractions()
    {
        FormulaFunctionContext context = CreateContext();
        context.CurrentDateTime = Number(100.75);
        context.CurrentDateTime.Kind = OdfValueKind.DateTime;
        OdfFormulaResult now = Evaluate(OdfFormulaNames.NOW, [], context);
        OdfFormulaResult today = Evaluate(OdfFormulaNames.TODAY, [], context);
        OdfFormulaResult easter = Evaluate(
            OdfFormulaNames.EASTERSUNDAY,
            [Number(2024)],
            context);
        OdfFormulaResult actual = Evaluate(
            OdfFormulaNames.YEARFRAC,
            [Date(2024, 1, 1, context), Date(2024, 7, 1, context), Number(1)],
            context);
        OdfFormulaResult actual360 = Evaluate(
            OdfFormulaNames.YEARFRAC,
            [Date(2024, 1, 1, context), Date(2024, 7, 1, context), Number(2)],
            context);
        FormulaFunctionContext missingClock = CreateContext();
        OdfFormulaResult unavailable = Evaluate(
            OdfFormulaNames.NOW, [], missingClock);

        Assert.Multiple(() =>
        {
            Assert.That(now, Is.SameAs(context.CurrentDateTime));
            Assert.That(today.NumericValue, Is.EqualTo(100));
            Assert.That(today.Kind, Is.EqualTo(OdfValueKind.Date));
            Assert.That(easter.StringValue, Is.EqualTo("2024-03-31"));
            Assert.That(actual.NumericValue,
                Is.EqualTo(182.0 / 366.0).Within(0.000000000001));
            Assert.That(actual360.NumericValue,
                Is.EqualTo(182.0 / 360.0).Within(0.000000000001));
            Assert.That(unavailable.Error,
                Is.EqualTo(OdfFormulaError.NotAvailable));
        });
    }

    static FormulaFunctionContext CreateContext()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        context.SpreadsheetSettings.NullDate = "2024-01-01";
        return context;
    }

    static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        return FormulaFunctionEvaluator.Evaluate(name, arguments, context);
    }

    static OdfFormulaResult Number(double value)
    {
        return FormulaValueModel.CreateNumber(value);
    }

    static OdfFormulaResult Date(
        int year,
        int month,
        int day,
        FormulaFunctionContext context)
    {
        return Evaluate(
            OdfFormulaNames.DATE,
            [Number(year), Number(month), Number(day)],
            context);
    }

    static OdfFormulaResult DateDif(
        OdfFormulaResult start,
        OdfFormulaResult end,
        string format,
        FormulaFunctionContext context)
    {
        return Evaluate(
            OdfFormulaNames.DATEDIF,
            [start, end, FormulaValueModel.CreateText(format)],
            context);
    }
}
