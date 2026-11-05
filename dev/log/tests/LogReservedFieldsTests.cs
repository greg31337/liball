using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogReservedFieldsTests
{
    [Test]
    public void IsReservedFieldName_DetectsEventFieldNamesCaseInsensitively()
    {
        Assert.That(LogReservedFields.IsReservedFieldName("timestamp"), Is.True);
        Assert.That(LogReservedFields.IsReservedFieldName("Level"), Is.True);
        Assert.That(LogReservedFields.IsReservedFieldName("loggerName"), Is.True);
        Assert.That(LogReservedFields.IsReservedFieldName("traceId"), Is.True);
        Assert.That(LogReservedFields.IsReservedFieldName("custom"), Is.False);
    }

    [Test]
    public void Resolve_PrefixesReservedFieldNames()
    {
        LogField[] resolved = LogReservedFields.Resolve(
            new LogField[]
            {
                LogField.String("level", "user-level"),
                LogField.String("custom", "value")
            });

        Assert.That(resolved.Length, Is.EqualTo(2));
        Assert.That(resolved[0].Name, Is.EqualTo("field_level"));
        Assert.That(resolved[1].Name, Is.EqualTo("custom"));
    }

    [Test]
    public void Resolve_AvoidsExistingPrefixedNames()
    {
        LogField[] resolved = LogReservedFields.Resolve(
            new LogField[]
            {
                LogField.String("field_level", "existing"),
                LogField.String("level", "reserved")
            });

        Assert.That(resolved.Length, Is.EqualTo(2));
        Assert.That(resolved[0].Name, Is.EqualTo("field_level"));
        Assert.That(resolved[1].Name, Is.EqualTo("field_level_1"));
    }

    [Test]
    public void Resolve_CombinesReservedHandlingWithDuplicateResolution()
    {
        LogField[] resolved = LogReservedFields.Resolve(
            new LogField[]
            {
                LogField.String("message", "first"),
                LogField.String("message", "second")
            });

        Assert.That(resolved.Length, Is.EqualTo(2));
        Assert.That(resolved[0].Name, Is.EqualTo("field_message"));
        Assert.That(resolved[1].Name, Is.EqualTo("field_message_1"));
    }

    [Test]
    public void LogEvent_ResolvesReservedFieldsOnCapture()
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
                LogField.String("level", "user-level")
            },
            null);

        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("field_level"));
    }

    [Test]
    public void Binder_ResolvesReservedPlaceholderNames()
    {
        LogField[] fields = LogTemplateBinder.BindParsedTemplate("{level}", new object[] { "user-level" });

        Assert.That(fields.Length, Is.EqualTo(1));
        Assert.That(fields[0].Name, Is.EqualTo("field_level"));
    }
}
