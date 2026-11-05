using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicLoggerFactoryTests
{
    [Test]
    public void RootFactoryMethods_CreateDefaultAndFixedLevelLoggers()
    {
        ILoggerFactory factory = new LogFactory();

        Logger defaultLogger = factory.CreateRootLogger();
        Logger fixedLogger = factory.CreateRootLoggerWithLevel(LogLevel.Warn);

        Assert.That(defaultLogger.GetName(), Is.Empty);
        Assert.That(defaultLogger.GetIsValid(), Is.True);
        Assert.That(defaultLogger.GetMinimumLevel(), Is.EqualTo(LogLevel.Info));
        Assert.That(defaultLogger.IsEnabled(LogLevel.Debug), Is.False);
        Assert.That(defaultLogger.IsEnabled(LogLevel.Info), Is.True);
        Assert.That(fixedLogger.GetName(), Is.Empty);
        Assert.That(fixedLogger.GetMinimumLevel(), Is.EqualTo(LogLevel.Warn));
        Assert.That(fixedLogger.IsEnabled(LogLevel.Info), Is.False);
        Assert.That(fixedLogger.IsEnabled(LogLevel.Warn), Is.True);
    }

    [Test]
    public void NamedFactoryMethods_CreateNamedDefaultAndFixedLevelLoggers()
    {
        ILoggerFactory factory = new LogFactory();

        Logger defaultLogger = factory.CreateNamedLogger("Application.Worker");
        Logger fixedLogger = factory.CreateNamedLoggerWithLevel("Application.Database", LogLevel.Error);
        LogResult result = defaultLogger.WriteWithValues(LogLevel.Info, "Processed {Count}", new object?[] { 3 });

        Assert.That(defaultLogger.GetName(), Is.EqualTo("Application.Worker"));
        Assert.That(defaultLogger.GetMinimumLevel(), Is.EqualTo(LogLevel.Info));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(defaultLogger.GetLastEvent(), Is.Not.Null);
        Assert.That(defaultLogger.GetLastEvent()!.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(defaultLogger.GetLastEvent()!.RenderedMessage, Is.EqualTo("Processed 3"));
        Assert.That(fixedLogger.GetName(), Is.EqualTo("Application.Database"));
        Assert.That(fixedLogger.GetMinimumLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(fixedLogger.IsEnabled(LogLevel.Warn), Is.False);
        Assert.That(fixedLogger.IsEnabled(LogLevel.Error), Is.True);
    }

    [Test]
    public void NamedFactoryMethods_PreserveLoggerValidationResults()
    {
        ILoggerFactory factory = new LogFactory();

        Logger invalid = factory.CreateNamedLogger("bad name");

        Assert.That(invalid.GetIsValid(), Is.False);
        Assert.That(invalid.GetErrorMessage(), Is.EqualTo("logger name can contain only letters, digits, underscores, and dots"));
        Assert.That(invalid.IsEnabled(LogLevel.Fatal), Is.False);
    }

    [Test]
    public void RuntimeSwitchFactoryMethods_ShareSubsequentLevelChanges()
    {
        ILoggerFactory factory = new LogFactory();
        LogLevelSwitch levelSwitch = new LogLevelSwitch();
        levelSwitch.SetLevel(LogLevel.Error);
        Logger root = factory.CreateRootLoggerWithSwitch(levelSwitch);
        Logger named = factory.CreateNamedLoggerWithSwitch("Application.Worker", levelSwitch);

        Assert.That(root.IsEnabled(LogLevel.Warn), Is.False);
        Assert.That(named.IsEnabled(LogLevel.Warn), Is.False);

        levelSwitch.SetLevel(LogLevel.Debug);

        Assert.That(root.IsEnabled(LogLevel.Warn), Is.True);
        Assert.That(named.IsEnabled(LogLevel.Warn), Is.True);
        Assert.That(named.GetName(), Is.EqualTo("Application.Worker"));
    }

    [Test]
    public void RuntimeSwitchFactoryMethods_NormalizeNullSwitchesToDefaultLevel()
    {
        ILoggerFactory factory = new LogFactory();

        Logger root = factory.CreateRootLoggerWithSwitch(null);
        Logger named = factory.CreateNamedLoggerWithSwitch("Application.Worker", null);

        Assert.That(root.GetMinimumLevel(), Is.EqualTo(LogLevel.Info));
        Assert.That(named.GetMinimumLevel(), Is.EqualTo(LogLevel.Info));
        Assert.That(named.GetName(), Is.EqualTo("Application.Worker"));
    }

    [Test]
    public void NoOpFactoryMethod_CreatesExplicitlyDisabledLogger()
    {
        ILoggerFactory factory = new LogFactory();

        Logger logger = factory.CreateNoOpLogger();
        LogResult result = logger.WriteWithValues(LogLevel.Fatal, "ignored", null);

        Assert.That(logger.GetName(), Is.Empty);
        Assert.That(logger.GetIsNoOp(), Is.True);
        Assert.That(logger.GetMinimumLevel(), Is.EqualTo(LogLevel.Off));
        Assert.That(logger.IsEnabled(LogLevel.Trace), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Fatal), Is.False);
        Assert.That(result.IsDropped, Is.True);
        Assert.That(logger.GetWrittenCount(), Is.Zero);
        Assert.That(logger.GetLastEvent(), Is.Null);
    }
}
