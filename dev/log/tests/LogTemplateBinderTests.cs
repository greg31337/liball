using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogTemplateBinderTests
{
    [Test]
    public void Bind_MapsPlaceholdersToValuesByPosition()
    {
        LogTemplate template = LogTemplateParser.ParseTemplate("Hello {User}, count {Count}");

        LogField[] fields = LogTemplateBinder.BindTemplate(
            template,
            new object[]
            {
                "Ada",
                3
            });

        Assert.That(fields.Length, Is.EqualTo(2));
        Assert.That(fields[0].Name, Is.EqualTo("User"));
        Assert.That(fields[0].Value.Kind, Is.EqualTo(LogValueKind.String));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo("Ada"));
        Assert.That(fields[1].Name, Is.EqualTo("Count"));
        Assert.That(fields[1].Value.Kind, Is.EqualTo(LogValueKind.Integer));
        Assert.That(fields[1].Value.IntegerValue, Is.EqualTo(3));
    }

    [Test]
    public void Bind_UsesNullForMissingValuesAndIgnoresExtraValues()
    {
        LogField[] missing = LogTemplateBinder.BindParsedTemplate("Hello {User} {Region}", new object[] { "Ada" });
        LogField[] extra = LogTemplateBinder.BindParsedTemplate("Hello {User}", new object[] { "Ada", "Ignored" });

        Assert.That(missing.Length, Is.EqualTo(2));
        Assert.That(missing[0].Value.TextValue, Is.EqualTo("Ada"));
        Assert.That(missing[1].Value.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(extra.Length, Is.EqualTo(1));
        Assert.That(extra[0].Value.TextValue, Is.EqualTo("Ada"));
    }

    [Test]
    public void Bind_SkipsInvalidPlaceholders()
    {
        LogTemplate template = new LogTemplate(
            "{Good} {bad name}",
            new LogTemplateToken[]
            {
                LogTemplateToken.Placeholder("Good", 0, 0, 6),
                LogTemplateToken.Placeholder("bad name", 1, 7, 10)
            });

        LogField[] fields = LogTemplateBinder.BindTemplate(template, new object[] { "ok", "ignored" });

        Assert.That(fields.Length, Is.EqualTo(1));
        Assert.That(fields[0].Name, Is.EqualTo("Good"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo("ok"));
    }

    [Test]
    public void Bind_ResolvesDuplicatePlaceholderNames()
    {
        LogField[] fields = LogTemplateBinder.BindParsedTemplate("{User} {User}", new object[] { "first", "second" });

        Assert.That(fields.Length, Is.EqualTo(2));
        Assert.That(fields[0].Name, Is.EqualTo("User"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo("first"));
        Assert.That(fields[1].Name, Is.EqualTo("User_1"));
        Assert.That(fields[1].Value.TextValue, Is.EqualTo("second"));
    }

    [Test]
    public void ToLogValue_ConvertsCommonScalarTypes()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 18, 0, 0, TimeSpan.Zero);
        TimeSpan duration = TimeSpan.FromSeconds(5);

        Assert.That(LogTemplateBinder.ToLogValue(null).Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(LogTemplateBinder.ToLogValue(true).Kind, Is.EqualTo(LogValueKind.Bool));
        Assert.That(LogTemplateBinder.ToLogValue(5).IntegerValue, Is.EqualTo(5));
        Assert.That(LogTemplateBinder.ToLogValue(6L).IntegerValue, Is.EqualTo(6));
        Assert.That(LogTemplateBinder.ToLogValue(7.5m).DecimalValue, Is.EqualTo(7.5m));
        Assert.That(LogTemplateBinder.ToLogValue(8.5d).FloatingPointValue, Is.EqualTo(8.5d));
        Assert.That(LogTemplateBinder.ToLogValue(timestamp).DateTimeValue, Is.EqualTo(timestamp));
        Assert.That(LogTemplateBinder.ToLogValue(duration).DurationValue, Is.EqualTo(duration));
    }

    [Test]
    public void ToLogValue_ConvertsExceptionArraysDictionariesAndObjectText()
    {
        InvalidOperationException exception = new InvalidOperationException("failed");
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary["name"] = "Ada";
        dictionary["count"] = 2;

        LogValue exceptionValue = LogTemplateBinder.ToLogValue(exception);
        LogValue arrayValue = LogTemplateBinder.ToLogValue(new object?[] { "a", 1, null });
        LogValue dictionaryValue = LogTemplateBinder.ToLogValue(dictionary);
        LogValue objectTextValue = LogTemplateBinder.ToLogValue(new Uri("https://example.test/path"));

        Assert.That(exceptionValue.Kind, Is.EqualTo(LogValueKind.Exception));
        Assert.That(arrayValue.Kind, Is.EqualTo(LogValueKind.Array));
        Assert.That(arrayValue.ArrayValue[2].Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(dictionaryValue.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(dictionaryValue.DictionaryValue["name"].TextValue, Is.EqualTo("Ada"));
        Assert.That(dictionaryValue.DictionaryValue["count"].IntegerValue, Is.EqualTo(2));
        Assert.That(objectTextValue.Kind, Is.EqualTo(LogValueKind.ObjectText));
    }

    [Test]
    public void ToLogValueLimit_ConvertsNestedObjectGraphsWithinLimits()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 8, 12, 30, 15, TimeSpan.Zero);
        Dictionary<string, object> user = new Dictionary<string, object>();
        user["name"] = "Ada";
        user["roles"] = new object[] { "admin", "operator" };
        Dictionary<string, object> payload = new Dictionary<string, object>();
        payload["user"] = user;
        payload["attempts"] = new object[] { 1, 2L, 3.5d };
        payload["createdAt"] = timestamp;
        payload["elapsed"] = TimeSpan.FromMilliseconds(250);

        LogValue value = LogTemplateBinder.ToLogValueLimit(payload, 100, 10, 4);

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(value.IsTruncated, Is.False);
        Assert.That(value.CapturedDepth, Is.EqualTo(4));
        Assert.That(value.DictionaryValue.Count, Is.EqualTo(4));
        Assert.That(value.DictionaryValue["createdAt"].DateTimeValue, Is.EqualTo(timestamp));
        Assert.That(value.DictionaryValue["elapsed"].DurationValue, Is.EqualTo(TimeSpan.FromMilliseconds(250)));

        LogValue userValue = value.DictionaryValue["user"];
        Assert.That(userValue.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(userValue.DictionaryValue["name"].TextValue, Is.EqualTo("Ada"));
        Assert.That(userValue.DictionaryValue["roles"].ArrayValue[0].TextValue, Is.EqualTo("admin"));
        Assert.That(userValue.DictionaryValue["roles"].ArrayValue[1].TextValue, Is.EqualTo("operator"));

        LogValue attempts = value.DictionaryValue["attempts"];
        Assert.That(attempts.Kind, Is.EqualTo(LogValueKind.Array));
        Assert.That(attempts.ArrayValue[0].IntegerValue, Is.EqualTo(1));
        Assert.That(attempts.ArrayValue[1].IntegerValue, Is.EqualTo(2));
        Assert.That(attempts.ArrayValue[2].FloatingPointValue, Is.EqualTo(3.5d));
    }
}
