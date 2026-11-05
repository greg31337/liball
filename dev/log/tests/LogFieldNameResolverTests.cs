using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogFieldNameResolverTests
{
    [Test]
    public void Resolve_KeepsUniqueFieldNamesUnchanged()
    {
        LogField[] resolved = LogFieldNameResolver.Resolve(
            new LogField[]
            {
                LogField.String("User", "Ada"),
                LogField.Int("Count", 2)
            });

        Assert.That(resolved.Length, Is.EqualTo(2));
        Assert.That(resolved[0].Name, Is.EqualTo("User"));
        Assert.That(resolved[1].Name, Is.EqualTo("Count"));
    }

    [Test]
    public void Resolve_AddsStableSuffixesForDuplicateNames()
    {
        LogField[] resolved = LogFieldNameResolver.Resolve(
            new LogField[]
            {
                LogField.String("User", "first"),
                LogField.String("User", "second"),
                LogField.String("User", "third")
            });

        Assert.That(resolved.Length, Is.EqualTo(3));
        Assert.That(resolved[0].Name, Is.EqualTo("User"));
        Assert.That(resolved[1].Name, Is.EqualTo("User_1"));
        Assert.That(resolved[2].Name, Is.EqualTo("User_2"));
    }

    [Test]
    public void Resolve_AvoidsCollisionsWithExistingSuffixedNames()
    {
        LogField[] resolved = LogFieldNameResolver.Resolve(
            new LogField[]
            {
                LogField.String("User", "first"),
                LogField.String("User_1", "existing"),
                LogField.String("User", "second")
            });

        Assert.That(resolved.Length, Is.EqualTo(3));
        Assert.That(resolved[0].Name, Is.EqualTo("User"));
        Assert.That(resolved[1].Name, Is.EqualTo("User_1"));
        Assert.That(resolved[2].Name, Is.EqualTo("User_2"));
    }

    [Test]
    public void Resolve_SkipsInvalidAndNullFields()
    {
        LogField[] resolved = LogFieldNameResolver.Resolve(
            new LogField[]
            {
                LogField.String("Good", "value"),
                LogField.String("bad name", "ignored"),
                null!
            });

        Assert.That(resolved.Length, Is.EqualTo(1));
        Assert.That(resolved[0].Name, Is.EqualTo("Good"));
    }

    [Test]
    public void LogEvent_ResolvesDuplicateFieldNamesOnCapture()
    {
        LogEvent logEvent = LogEvent.CreateAt(
            DateTimeOffset.MinValue,
            LogLevel.Info,
            "",
            0,
            "",
            "",
            "",
            new LogField[]
            {
                LogField.String("User", "first"),
                LogField.String("User", "second")
            },
            null);

        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("User"));
        Assert.That(logEvent.Fields[1].Name, Is.EqualTo("User_1"));
    }
}
