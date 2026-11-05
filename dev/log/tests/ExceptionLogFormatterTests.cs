using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class ExceptionLogFormatterTests
{
    [Test]
    public void FormatEventException_ReturnsEmptyTextWhenEventHasNoException()
    {
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        string output = ExceptionLogFormatter.FormatEventException(logEvent);

        Assert.That(output, Is.EqualTo(""));
    }

    [Test]
    public void FormatExceptionSummary_FormatsTypeAndMessage()
    {
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");

        string output = ExceptionLogFormatter.FormatExceptionSummary(exceptionValue);

        Assert.That(output, Is.EqualTo("System.InvalidOperationException: failed"));
    }

    [Test]
    public void FormatException_IncludesInnerExceptionChain()
    {
        ArgumentException inner = new ArgumentException("inner failed");
        InvalidOperationException outer = new InvalidOperationException("outer failed", inner);

        string output = ExceptionLogFormatter.FormatException(outer);

        Assert.That(output, Does.StartWith("System.InvalidOperationException: outer failed"));
        Assert.That(output, Does.Contain("Caused by: System.ArgumentException: inner failed"));
    }

    [Test]
    public void FormatException_IncludesStackTraceWhenPresent()
    {
        Exception exceptionValue = CreateThrownException();

        string output = ExceptionLogFormatter.FormatException(exceptionValue);

        Assert.That(output, Does.Contain("System.ApplicationException: thrown"));
        Assert.That(output, Does.Contain(nameof(CreateThrownException)));
    }

    [Test]
    public void SummaryFormatter_FormatsEventExceptionAsSingleHeader()
    {
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        LogEvent logEvent = LogEvent.CreateAt(DateTimeOffset.UnixEpoch, LogLevel.Error, "Application", 0, "", "failed", "failed", new LogField[0], exceptionValue);
        LogFormatter formatter = ExceptionLogFormatter.Summary();

        string output = formatter.Format(logEvent);

        Assert.That(output, Is.EqualTo("System.InvalidOperationException: failed"));
    }

    [Test]
    public void FormatterDispatch_UsesBuiltInExceptionFormatter()
    {
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        LogEvent logEvent = LogEvent.CreateAt(DateTimeOffset.UnixEpoch, LogLevel.Error, "Application", 0, "", "failed", "failed", new LogField[0], exceptionValue);
        LogFormatter formatter = new LogFormatter();

        string output = formatter.Dispatch("exception", logEvent);

        Assert.That(formatter.CanFormat("exception"), Is.True);
        Assert.That(output, Does.StartWith("System.InvalidOperationException: failed"));
    }

    static Exception CreateThrownException()
    {
        try
        {
            throw new ApplicationException("thrown");
        }
        catch (Exception exceptionValue)
        {
            return exceptionValue;
        }
    }
}
