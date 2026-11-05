using asn1;

namespace asn1.Tests;

public class TimeSettingsCompilerTests
{
    [Test]
    public void CompilerBuildsTypedDateAndTimeSelections()
    {
        ResolutionFixture fixture = Resolve(
            "Times DEFINITIONS ::= BEGIN "
                + "Calendar ::= TIME (SETTINGS \"Basic=Date Date=YWD Year=L6\"); "
                + "Clock ::= TIME (SETTINGS \"Basic=Time Time=HMSF3 "
                + "Local-or-UTC=LD Midnight=End\") END");
        TimePropertySettings calendar = RequireSettings(fixture, "Calendar").Single();
        TimePropertySettings clock = RequireSettings(fixture, "Clock").Single();

        Assert.That(calendar.Basic, Is.EqualTo(TimeBasicSettingKind.Date));
        Assert.That(calendar.Date, Is.EqualTo(TimeDateSettingKind.YearWeekDay));
        Assert.That(calendar.Year, Is.EqualTo(TimeYearSettingKind.Large));
        Assert.That(calendar.LargeYearDigits, Is.EqualTo(6));
        Assert.That(clock.Basic, Is.EqualTo(TimeBasicSettingKind.Time));
        Assert.That(clock.Time, Is.EqualTo(TimePrecisionSettingKind.HourMinuteSecond));
        Assert.That(clock.FractionalDigits, Is.EqualTo(3));
        Assert.That(clock.Zone, Is.EqualTo(TimeZoneSettingKind.LocalDifference));
        Assert.That(clock.Midnight, Is.EqualTo(TimeMidnightSettingKind.End));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerBuildsIntervalRecurrenceAndDurationSelections()
    {
        ResolutionFixture fixture = Resolve(
            "Times DEFINITIONS ::= BEGIN "
                + "Window ::= TIME (SETTINGS \"Basic=Interval Interval-type=SE "
                + "SE-point=Date-Time Date=YD Year=Proleptic Time=HMF2 "
                + "Local-or-UTC=Z Midnight=Start\"); "
                + "Repeating ::= TIME (SETTINGS \"Basic=Rec-Interval Interval-type=SD "
                + "SE-point=Date Recurrence=R12\"); "
                + "Forever ::= TIME (SETTINGS \"Basic=Rec-Interval Interval-type=D "
                + "Recurrence=Unlimited\"); "
                + "Span ::= DURATION (SETTINGS \"Basic=Interval Interval-type=D\") END");
        TimePropertySettings window = RequireSettings(fixture, "Window").Single();
        TimePropertySettings repeating = RequireSettings(fixture, "Repeating").Single();
        TimePropertySettings forever = RequireSettings(fixture, "Forever").Single();
        TimePropertySettings span = RequireSettings(fixture, "Span").Single();

        Assert.That(window.Interval, Is.EqualTo(TimeIntervalSettingKind.StartEnd));
        Assert.That(window.Endpoint, Is.EqualTo(TimeEndpointSettingKind.DateTime));
        Assert.That(window.Date, Is.EqualTo(TimeDateSettingKind.YearDay));
        Assert.That(window.Time, Is.EqualTo(TimePrecisionSettingKind.HourMinute));
        Assert.That(window.FractionalDigits, Is.EqualTo(2));
        Assert.That(window.Zone, Is.EqualTo(TimeZoneSettingKind.Utc));
        Assert.That(repeating.Basic, Is.EqualTo(TimeBasicSettingKind.RecurringInterval));
        Assert.That(repeating.Interval, Is.EqualTo(TimeIntervalSettingKind.StartDuration));
        Assert.That(repeating.Endpoint, Is.EqualTo(TimeEndpointSettingKind.Date));
        Assert.That(repeating.Recurrence, Is.EqualTo(TimeRecurrenceSettingKind.DigitCount));
        Assert.That(repeating.RecurrenceDigits, Is.EqualTo(12));
        Assert.That(forever.Recurrence, Is.EqualTo(TimeRecurrenceSettingKind.Unlimited));
        Assert.That(span.Basic, Is.EqualTo(TimeBasicSettingKind.Interval));
        Assert.That(span.Interval, Is.EqualTo(TimeIntervalSettingKind.Duration));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParserRecognizesEveryDatePrecisionAndYearForm()
    {
        string[] dateNames = ["C", "Y", "YM", "YMD", "YD", "YW", "YWD"];
        TimeDateSettingKind[] dateKinds =
        [
            TimeDateSettingKind.Century,
            TimeDateSettingKind.Year,
            TimeDateSettingKind.YearMonth,
            TimeDateSettingKind.YearMonthDay,
            TimeDateSettingKind.YearDay,
            TimeDateSettingKind.YearWeek,
            TimeDateSettingKind.YearWeekDay
        ];

        int index = 0;
        while (index < dateNames.Length)
        {
            TimeSettingsParseResult parsed = new TimeSettingsParser(
                "Date=" + dateNames[index]).ParseSettings();
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Settings?.Date, Is.EqualTo(dateKinds[index]));
            index = index + 1;
        }

        Assert.That(new TimeSettingsParser("Year=Basic").ParseSettings().Settings?.Year,
            Is.EqualTo(TimeYearSettingKind.Basic));
        Assert.That(new TimeSettingsParser("Year=Proleptic").ParseSettings().Settings?.Year,
            Is.EqualTo(TimeYearSettingKind.Proleptic));
        Assert.That(new TimeSettingsParser("Year=Negative").ParseSettings().Settings?.Year,
            Is.EqualTo(TimeYearSettingKind.Negative));
        TimePropertySettings large = new TimeSettingsParser("Year=L17")
            .ParseSettings().Settings ?? throw new InvalidOperationException();
        Assert.That(large.Year, Is.EqualTo(TimeYearSettingKind.Large));
        Assert.That(large.LargeYearDigits, Is.EqualTo(17));
    }

    [Test]
    public void ParserRecognizesEveryTimePrecisionAndIntervalForm()
    {
        TimePropertySettings hours = ParseSettings("Time=H Local-or-UTC=L");
        TimePropertySettings minutes = ParseSettings("Time=HM Interval-type=DE");
        TimePropertySettings seconds = ParseSettings("Time=HMS SE-point=Time");
        TimePropertySettings hourFraction = ParseSettings("Time=HF1");
        TimePropertySettings minuteFraction = ParseSettings("Time=HMF2");
        TimePropertySettings secondFraction = ParseSettings("Time=HMSF9");

        Assert.That(hours.Time, Is.EqualTo(TimePrecisionSettingKind.Hour));
        Assert.That(hours.Zone, Is.EqualTo(TimeZoneSettingKind.Local));
        Assert.That(minutes.Time, Is.EqualTo(TimePrecisionSettingKind.HourMinute));
        Assert.That(minutes.Interval, Is.EqualTo(TimeIntervalSettingKind.DurationEnd));
        Assert.That(seconds.Time, Is.EqualTo(TimePrecisionSettingKind.HourMinuteSecond));
        Assert.That(seconds.Endpoint, Is.EqualTo(TimeEndpointSettingKind.Time));
        Assert.That(hourFraction.FractionalDigits, Is.EqualTo(1));
        Assert.That(minuteFraction.FractionalDigits, Is.EqualTo(2));
        Assert.That(secondFraction.FractionalDigits, Is.EqualTo(9));
    }

    [Test]
    public void CompilerRetainsPartialSetExpressionsAndInheritedSelections()
    {
        ResolutionFixture fixture = Resolve(
            "Times DEFINITIONS ::= BEGIN "
                + "Base ::= TIME ((SETTINGS \"Basic=Time\") "
                + "EXCEPT (SETTINGS \"Midnight=End\")); "
                + "Derived ::= Base (SETTINGS \"Time=HM Local-or-UTC=Z\") END");
        TimePropertySettings[] baseSettings = RequireSettings(fixture, "Base");
        TimePropertySettings[] derivedSettings = RequireSettings(fixture, "Derived");

        Assert.That(baseSettings.Select(settings => settings.Source),
            Is.EqualTo(new[] { "Basic=Time", "Midnight=End" }));
        Assert.That(derivedSettings.Select(settings => settings.Source),
            Is.EqualTo(new[]
            {
                "Basic=Time",
                "Midnight=End",
                "Time=HM Local-or-UTC=Z"
            }));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerDiagnosesMalformedAndIncompatibleSettings()
    {
        ResolutionFixture fixture = Resolve(
            "Times DEFINITIONS ::= BEGIN "
                + "Unknown ::= TIME (SETTINGS \"Calendar=YMD\"); "
                + "Fraction ::= TIME (SETTINGS \"Time=HMF0\"); "
                + "Large ::= TIME (SETTINGS \"Year=L4\"); "
                + "Repeated ::= TIME (SETTINGS \"Date=Y Date=YM\"); "
                + "DateConflict ::= TIME (SETTINGS \"Basic=Date Time=H\"); "
                + "IntervalConflict ::= TIME "
                + "(SETTINGS \"Basic=Interval Recurrence=R1\") END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0004"),
            Is.EqualTo(4));
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0005"),
            Is.EqualTo(2));
        Assert.That(diagnostics.Where(diagnostic => diagnostic.Code == "ASNC0004"
                || diagnostic.Code == "ASNC0005")
            .All(diagnostic => diagnostic.Span != null), Is.True);
        Assert.That(RequireSettings(fixture, "Unknown"), Is.Empty);
        Assert.That(RequireSettings(fixture, "DateConflict"), Is.Empty);
    }

    private static TimePropertySettings ParseSettings(string source)
    {
        TimeSettingsParseResult parsed = new TimeSettingsParser(source).ParseSettings();
        Assert.That(parsed.Success, Is.True, parsed.ErrorMessage);
        return parsed.Settings ?? throw new InvalidOperationException();
    }

    private static TimePropertySettings[] RequireSettings(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)
            ?.ResolvedType?.TimeSettings
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "time-settings.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(
            [parsed.Module ?? throw new InvalidOperationException()]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True);
        Assert.That(types.Success, Is.True);
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
