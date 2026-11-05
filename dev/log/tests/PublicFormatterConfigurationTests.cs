using System.Globalization;
using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicFormatterConfigurationTests
{
    [Test]
    public void TimestampMode_AppliesUtcAndLocalTimeAcrossBuiltInFormatters()
    {
        ILogFactory factory = new LogFactory();
        DateTimeOffset timestamp = new DateTimeOffset(2026, 1, 15, 18, 30, 45, TimeSpan.FromHours(2));
        LogEvent logEvent = CreateEvent(timestamp, factory);
        LogFormatter[] formatters = CreateFormatters(factory, "{Timestamp}|{UtcTimestamp}|{Fields}");
        string utcTimestamp = timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        string localTimestamp = timestamp.ToLocalTime().ToString("O", CultureInfo.InvariantCulture);

        foreach (LogFormatter formatter in formatters)
        {
            ILogFormatterConfiguration configuration = formatter;
            Assert.That(configuration.GetTimestampMode(), Is.EqualTo(LogTimestampMode.Utc));
            Assert.That(formatter.Format(logEvent), Does.Contain(utcTimestamp));

            configuration.SetTimestampMode(LogTimestampMode.Local);

            Assert.That(configuration.GetTimestampMode(), Is.EqualTo(LogTimestampMode.Local));
            Assert.That(formatter.Format(logEvent), Does.Contain(localTimestamp));

            configuration.SetTimestampMode(LogTimestampMode.Utc);

            Assert.That(configuration.GetTimestampMode(), Is.EqualTo(LogTimestampMode.Utc));
            Assert.That(formatter.Format(logEvent), Does.Contain(utcTimestamp));
        }
    }

    [Test]
    public void NameFieldOrder_IsStableAcrossBuiltInFormatters()
    {
        ILogFactory factory = new LogFactory();
        LogEvent logEvent = CreateEvent(DateTimeOffset.UnixEpoch, factory);
        LogFormatter[] formatters = CreateFormatters(factory, "{Timestamp}{Fields}");

        foreach (LogFormatter formatter in formatters)
        {
            ILogFormatterConfiguration configuration = formatter;
            Assert.That(configuration.GetFieldOrder(), Is.EqualTo(LogFieldOrder.Captured));

            configuration.SetFieldOrder(LogFieldOrder.Name);
            string ordered = formatter.Format(logEvent);

            Assert.That(configuration.GetFieldOrder(), Is.EqualTo(LogFieldOrder.Name));
            AssertNamesAppearInOrder(ordered, "Alpha", "Middle", "Zeta");

            configuration.SetFieldOrder(LogFieldOrder.Captured);
            string captured = formatter.Format(logEvent);

            Assert.That(configuration.GetFieldOrder(), Is.EqualTo(LogFieldOrder.Captured));
            AssertNamesAppearInOrder(captured, "Zeta", "Alpha", "Middle");
        }
    }

    [Test]
    public void BuiltInFormatter_CustomActionCanOverrideAndRestoreConfiguredFormatting()
    {
        ILogFactory factory = new LogFactory();
        LogFormatter formatter = factory.CreateTextFormatter();
        ILogFormatterConfiguration configuration = formatter;
        LogEvent logEvent = CreateEvent(DateTimeOffset.UnixEpoch, factory);
        configuration.SetFieldOrder(LogFieldOrder.Name);

        configuration.SetFormatAction(currentEvent => "custom:" + currentEvent.RenderedMessage);
        string custom = formatter.Format(logEvent);
        configuration.ClearFormatAction();
        string restored = formatter.Format(logEvent);

        Assert.That(custom, Is.EqualTo("custom:message"));
        AssertNamesAppearInOrder(restored, "Alpha", "Middle", "Zeta");
    }

    static LogFormatter[] CreateFormatters(ILogFactory factory, string outputTemplate)
    {
        return new LogFormatter[]
        {
            factory.CreateCompactJsonFormatter(),
            factory.CreateFullJsonFormatter(),
            factory.CreateTextFormatter(),
            factory.CreatePlainConsoleFormatter(),
            factory.CreateColorConsoleFormatter(),
            factory.CreateKeyValueFormatter(),
            factory.CreateOutputTemplateFormatter(outputTemplate)
        };
    }

    static LogEvent CreateEvent(DateTimeOffset timestamp, ILogFactory factory)
    {
        return new LogEvent(
            timestamp,
            LogLevel.Info,
            "Application",
            7,
            "Processed",
            "message",
            "message",
            new LogField[]
            {
                factory.CreateField("Zeta", 3),
                factory.CreateField("Alpha", 1),
                factory.CreateField("Middle", 2)
            },
            null,
            "",
            "",
            "",
            1,
            2,
            "machine",
            "application",
            "",
            "",
            0);
    }

    static void AssertNamesAppearInOrder(string output, string first, string second, string third)
    {
        int firstIndex = output.IndexOf(first, StringComparison.Ordinal);
        int secondIndex = output.IndexOf(second, StringComparison.Ordinal);
        int thirdIndex = output.IndexOf(third, StringComparison.Ordinal);

        Assert.That(firstIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(secondIndex, Is.GreaterThan(firstIndex));
        Assert.That(thirdIndex, Is.GreaterThan(secondIndex));
    }
}
