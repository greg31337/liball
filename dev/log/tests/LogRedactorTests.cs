using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogRedactorTests
{
    [Test]
    public void RedactFields_ReplacesMatchingFieldValues()
    {
        LogField[] fields = LogRedactor.RedactFields(
            new LogField[]
            {
                LogField.String("password", "secret"),
                LogField.String("user", "Ada")
            },
            new string[]
            {
                "password"
            });

        Assert.That(fields[0].Name, Is.EqualTo("password"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo(LogRedactor.RedactedText));
        Assert.That(fields[1].Value.TextValue, Is.EqualTo("Ada"));
    }

    [Test]
    public void RedactFields_MatchesCaseAndDuplicateSuffixes()
    {
        LogField[] fields = LogRedactor.RedactFields(
            new LogField[]
            {
                LogField.String("Password", "first"),
                LogField.String("password_1", "second")
            },
            new string[]
            {
                "password"
            });

        Assert.That(fields[0].Value.TextValue, Is.EqualTo(LogRedactor.RedactedText));
        Assert.That(fields[1].Value.TextValue, Is.EqualTo(LogRedactor.RedactedText));
    }

    [Test]
    public void RedactFields_MatchesReservedFieldRenames()
    {
        LogField[] reservedFields = LogReservedFields.Resolve(
            new LogField[]
            {
                LogField.String("level", "user-level")
            });

        LogField[] fields = LogRedactor.RedactFields(reservedFields, new string[] { "level" });

        Assert.That(fields[0].Name, Is.EqualTo("field_level"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo(LogRedactor.RedactedText));
    }

    [Test]
    public void RedactEvent_ReturnsEventSnapshotWithRedactedFields()
    {
        LogEvent logEvent = LogEvent.CreateAt(
            DateTimeOffset.MinValue,
            LogLevel.Info,
            "Application.Worker",
            5,
            "Event",
            "template",
            "rendered",
            new LogField[]
            {
                LogField.String("token", "secret")
            },
            null);

        LogEvent redacted = LogRedactor.RedactEvent(logEvent, new string[] { "token" });

        Assert.That(redacted.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(redacted.EventId, Is.EqualTo(5));
        Assert.That(redacted.Fields[0].Name, Is.EqualTo("token"));
        Assert.That(redacted.Fields[0].Value.TextValue, Is.EqualTo(LogRedactor.RedactedText));
    }

    [Test]
    public void Builder_RedactsConfiguredFieldNamesBeforeBuild()
    {
        LogEvent logEvent = LogEventBuilder.Create(LogClock.Fixed(DateTimeOffset.MinValue))
            .AddRedactedFieldName("password")
            .AddField(LogField.String("password", "secret"))
            .AddField(LogField.String("user", "Ada"))
            .Build();

        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("password"));
        Assert.That(logEvent.Fields[0].Value.TextValue, Is.EqualTo(LogRedactor.RedactedText));
        Assert.That(logEvent.Fields[1].Value.TextValue, Is.EqualTo("Ada"));
    }
}
