using fake;

namespace fake.Tests;

[TestFixture]
public class SystemDeveloperTextTests
{
    [Test]
    public void CronKindsProduceConventionalFiveFieldSchedules()
    {
        SystemProvider system = Fake.Create(11600).System();

        Assert.Multiple(() =>
        {
            Assert.That(system.CronFromKind(CronExpressionKind.EveryMinute),
                Is.EqualTo("* * * * *"));
            Assert.That(system.CronFromKind(CronExpressionKind.Hourly),
                Is.EqualTo("0 * * * *"));
            Assert.That(system.CronFromKind(CronExpressionKind.Daily),
                Is.EqualTo("0 0 * * *"));
            Assert.That(system.CronFromKind(CronExpressionKind.Weekly),
                Is.EqualTo("0 0 * * 0"));
            Assert.That(system.CronFromKind(CronExpressionKind.Monthly),
                Is.EqualTo("0 0 1 * *"));
        });

        int index = 0;
        while (index < 128)
        {
            AssertCronShape(system.Cron());
            AssertCronShape(system.CronFromKind(CronExpressionKind.SpecificTime));
            AssertCronShape(system.CronFromKind(CronExpressionKind.MinuteInterval));
            index++;
        }
    }

    [Test]
    public void CronGenerationIsDeterministic()
    {
        SystemProvider first = Fake.Create(11601).System();
        SystemProvider second = Fake.Create(11601).System();

        int index = 0;
        while (index < 128)
        {
            Assert.Multiple(() =>
            {
                Assert.That(first.Cron(), Is.EqualTo(second.Cron()));
                Assert.That(first.CronFromKind(CronExpressionKind.SpecificTime),
                    Is.EqualTo(second.CronFromKind(CronExpressionKind.SpecificTime)));
                Assert.That(first.CronFromKind(CronExpressionKind.MinuteInterval),
                    Is.EqualTo(second.CronFromKind(CronExpressionKind.MinuteInterval)));
            });
            index++;
        }
    }

    [Test]
    public void EnvironmentNamesArePortableAndValidatorHandlesBoundaries()
    {
        const long seed = 11602;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();

        int index = 0;
        while (index < 128)
        {
            string value = first.EnvironmentName();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.EnvironmentName()));
                Assert.That(value, Does.Match("^[A-Z][A-Z0-9_]*$"));
                Assert.That(first.IsEnvironmentName(value), Is.True);
            });
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(first.IsEnvironmentName("_"), Is.True);
            Assert.That(first.IsEnvironmentName("app_2"), Is.True);
            Assert.That(first.IsEnvironmentName(""), Is.False);
            Assert.That(first.IsEnvironmentName("2APP"), Is.False);
            Assert.That(first.IsEnvironmentName("APP-NAME"), Is.False);
            Assert.That(first.IsEnvironmentName("APP.NAME"), Is.False);
            Assert.That(() => first.IsEnvironmentName(null),
                Throws.TypeOf<ArgumentNullException>());
        });
    }

    [Test]
    public void CommandArgumentsAreDeterministicInertSingleTokens()
    {
        const long seed = 11603;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();
        string[] reviewed = new string[]
        {
            "--config=example.json", "--dry-run", "--format=json",
            "--mode=sample", "--output=report.txt", "--profile=example",
            "--verbose", "--workers=4"
        };

        int index = 0;
        while (index < 128)
        {
            string value = first.CommandArgument();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.CommandArgument()));
                Assert.That(value, Is.AnyOf(reviewed));
                Assert.That(value, Does.Match(
                    "^--[a-z-]+(?:=[a-z0-9.-]+)?$"));
                Assert.That(value, Does.Not.Contain(" "));
                Assert.That(value, Does.Not.Contain(";"));
                Assert.That(value, Does.Not.Contain("&"));
                Assert.That(value, Does.Not.Contain("|"));
                Assert.That(value, Does.Not.Contain("`"));
                Assert.That(value, Does.Not.Contain("$"));
            });
            index++;
        }
    }

    [Test]
    public void LogEntriesUseCapturedReferenceTimeAndRequestedSeverity()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2032,
            6,
            7,
            8,
            9,
            10,
            123,
            TimeSpan.FromHours(5.5));
        FakeOptions options = FakeOptions.Create(11604);
        options.ReferenceTime = reference;
        SystemProvider system = Fake.Create(options).System();
        string timestamp = reference.ToString("O");
        LogSeverity[] severities = Enum.GetValues<LogSeverity>();
        string[] labels = new string[]
        {
            "TRACE", "DEBUG", "INFORMATION", "WARNING", "ERROR", "CRITICAL"
        };

        int index = 0;
        while (index < severities.Length)
        {
            string value = system.LogEntryFromSeverity(severities[index]);
            Assert.Multiple(() =>
            {
                Assert.That(value, Does.StartWith(
                    timestamp + " [" + labels[index] + "] Example."));
                Assert.That(value, Does.Match(
                    " Example\\.[A-Za-z]+ [0-9]{4}: Synthetic "));
                Assert.That(value, Does.EndWith("."));
                Assert.That(value, Does.Not.Contain("\n"));
            });
            index++;
        }

        SystemProvider first = Fake.Create(options).System();
        SystemProvider second = Fake.Create(options).System();
        index = 0;
        while (index < 64)
        {
            Assert.That(first.LogEntry(), Is.EqualTo(second.LogEntry()));
            index++;
        }
    }

    [Test]
    public void ExceptionAndStackTextUseOnlySyntheticAuthoredContent()
    {
        const long seed = 11605;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();
        string message = first.ExceptionMessage();
        string sameMessage = second.ExceptionMessage();
        string defaultTrace = first.StackTrace();
        string sameDefaultTrace = second.StackTrace();
        string exactTrace = first.StackTraceFromFrameCount(5);
        string sameExactTrace = second.StackTraceFromFrameCount(5);
        string exception = first.ExceptionText();
        string sameException = second.ExceptionText();

        Assert.Multiple(() =>
        {
            Assert.That(message, Is.EqualTo(sameMessage));
            Assert.That(message, Does.StartWith("Synthetic "));
            Assert.That(defaultTrace, Is.EqualTo(sameDefaultTrace));
            Assert.That(defaultTrace.Split('\n'), Has.Length.InRange(2, 4));
            Assert.That(exactTrace, Is.EqualTo(sameExactTrace));
            Assert.That(exactTrace.Split('\n'), Has.Length.EqualTo(5));
            Assert.That(exception, Is.EqualTo(sameException));
            Assert.That(exception, Does.Match(
                "^System\\.(?:IO\\.)?[A-Za-z]+Exception: Synthetic "));
            Assert.That(exception.Split('\n'), Has.Length.InRange(3, 5));
        });

        string[] frames = exactTrace.Split('\n');
        int index = 0;
        while (index < frames.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(frames[index], Does.StartWith("   at Example."));
                Assert.That(frames[index], Does.Contain(
                    " in /workspace/src/"));
                Assert.That(frames[index], Does.Match(":line [0-9]+$"));
            });
            index++;
        }
    }

    [Test]
    public void MaximumSizeAndArgumentGuardsRunBeforeEntropy()
    {
        const long seed = 11606;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        SystemProvider inspected = Fake.Create(tinyOptions).System();
        SystemProvider untouched = Fake.Create(tinyOptions).System();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 7;
        SystemProvider countInspected = Fake.Create(countOptions).System();
        SystemProvider countUntouched = Fake.Create(countOptions).System();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.Cron(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.EnvironmentName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.CommandArgument(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.LogEntry(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.ExceptionMessage(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.StackTrace(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.ExceptionText(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.CommandArgument(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.StackTraceFromFrameCount(8),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.StackTraceFromFrameCount(0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.CronFromKind((CronExpressionKind)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.LogEntryFromSeverity((LogSeverity)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(),
                Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void DeveloperTextGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11607);
        Fake untouched = Fake.Create(11607);

        int index = 0;
        while (index < 32)
        {
            inspected.System().Cron();
            inspected.System().EnvironmentName();
            inspected.System().CommandArgument();
            inspected.System().LogEntry();
            inspected.System().ExceptionMessage();
            inspected.System().StackTrace();
            inspected.System().ExceptionText();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertCronShape(string value)
    {
        string[] fields = value.Split(' ');
        Assert.That(fields, Has.Length.EqualTo(5));
        Assert.Multiple(() =>
        {
            Assert.That(ValidMinute(fields[0]), Is.True, value);
            Assert.That(ValidHour(fields[1]), Is.True, value);
            Assert.That(fields[2], Is.AnyOf("*", "1"));
            Assert.That(fields[3], Is.EqualTo("*"));
            Assert.That(fields[4], Is.AnyOf("*", "0"));
        });
    }

    static bool ValidMinute(string value)
    {
        if (value == "*")
        {
            return true;
        }
        if (value.StartsWith("*/"))
        {
            int step = int.Parse(value.Substring(2));
            return step >= 2 && step <= 30;
        }
        int minute = int.Parse(value);
        return minute >= 0 && minute <= 59;
    }

    static bool ValidHour(string value)
    {
        if (value == "*")
        {
            return true;
        }
        int hour = int.Parse(value);
        return hour >= 0 && hour <= 23;
    }
}
