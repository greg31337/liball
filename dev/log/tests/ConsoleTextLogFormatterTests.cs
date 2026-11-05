using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class ConsoleTextLogFormatterTests
{
    [Test]
    public void Plain_UsesTextFormatterWithoutAnsiCodes()
    {
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        string output = ConsoleTextLogFormatter.FormatPlain(logEvent);

        Assert.That(output, Is.EqualTo(TextLogFormatter.FormatPlain(logEvent)));
        Assert.That(ConsoleTextLogFormatter.HasAnsiColor(output), Is.False);
    }

    [Test]
    public void AnsiColor_WrapsLineWithLevelColorAndReset()
    {
        LogEvent logEvent = LogEvent.Create(LogLevel.Warn, "Application", "message", "rendered", new LogField[0]);

        string output = ConsoleTextLogFormatter.FormatAnsi(logEvent);

        Assert.That(output, Does.StartWith(ConsoleTextLogFormatter.WarnColor));
        Assert.That(output, Does.EndWith(ConsoleTextLogFormatter.Reset));
        Assert.That(output, Does.Contain("[Warn] Application: rendered"));
        Assert.That(ConsoleTextLogFormatter.HasAnsiColor(output), Is.True);
    }

    [Test]
    public void WithAnsiColor_SelectsPlainOrColoredFormatter()
    {
        LogEvent logEvent = LogEvent.Create(LogLevel.Error, "Application", "message", "rendered", new LogField[0]);
        LogFormatter plain = ConsoleTextLogFormatter.WithAnsiColor(false);
        LogFormatter colored = ConsoleTextLogFormatter.WithAnsiColor(true);

        string plainOutput = plain.Format(logEvent);
        string coloredOutput = colored.Format(logEvent);

        Assert.That(ConsoleTextLogFormatter.HasAnsiColor(plainOutput), Is.False);
        Assert.That(coloredOutput, Does.StartWith(ConsoleTextLogFormatter.ErrorColor));
        Assert.That(coloredOutput, Does.EndWith(ConsoleTextLogFormatter.Reset));
    }

    [Test]
    public void ColorForLevel_MapsEveryLogLevel()
    {
        Assert.That(ConsoleTextLogFormatter.ColorForLevel(LogLevel.Trace), Is.EqualTo(ConsoleTextLogFormatter.TraceColor));
        Assert.That(ConsoleTextLogFormatter.ColorForLevel(LogLevel.Debug), Is.EqualTo(ConsoleTextLogFormatter.DebugColor));
        Assert.That(ConsoleTextLogFormatter.ColorForLevel(LogLevel.Info), Is.EqualTo(ConsoleTextLogFormatter.InfoColor));
        Assert.That(ConsoleTextLogFormatter.ColorForLevel(LogLevel.Warn), Is.EqualTo(ConsoleTextLogFormatter.WarnColor));
        Assert.That(ConsoleTextLogFormatter.ColorForLevel(LogLevel.Error), Is.EqualTo(ConsoleTextLogFormatter.ErrorColor));
        Assert.That(ConsoleTextLogFormatter.ColorForLevel(LogLevel.Fatal), Is.EqualTo(ConsoleTextLogFormatter.FatalColor));
        Assert.That(ConsoleTextLogFormatter.ColorForLevel(LogLevel.Off), Is.EqualTo(ConsoleTextLogFormatter.Reset));
    }

    [Test]
    public void ConsoleFormatter_FormatLine_AppendsConfiguredNewLine()
    {
        LogFormatter formatter = ConsoleTextLogFormatter.AnsiColor();
        LogEvent logEvent = LogEvent.Create(LogLevel.Fatal, "Application", "message", "rendered", new LogField[0]);

        formatter.NewLine = "\n";
        string output = formatter.FormatLine(logEvent);

        Assert.That(output, Does.EndWith(ConsoleTextLogFormatter.Reset + "\n"));
        Assert.That(output, Does.StartWith(ConsoleTextLogFormatter.FatalColor));
    }

    [Test]
    public void FormatterDispatch_UsesBuiltInConsoleTextFormatter()
    {
        LogFormatter formatter = new LogFormatter();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        string output = formatter.Dispatch("console", logEvent);

        Assert.That(formatter.CanFormat("console-text"), Is.True);
        Assert.That(output, Does.EndWith("[Info] Application: rendered"));
        Assert.That(ConsoleTextLogFormatter.HasAnsiColor(output), Is.False);
    }
}
