using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogFieldTests
{
    [Test]
    public void Constructor_AcceptsValidNameAndNormalizesNullValue()
    {
        LogField field = new LogField("request.id", null);

        Assert.That(field.IsValid, Is.True);
        Assert.That(field.Name, Is.EqualTo("request.id"));
        Assert.That(field.ErrorMessage, Is.EqualTo(""));
        Assert.That(field.Value.Kind, Is.EqualTo(LogValueKind.Null));
    }

    [Test]
    public void Constructor_RejectsInvalidNamesWithoutThrowing()
    {
        LogField missing = LogField.String("", "value");
        LogField padded = LogField.String(" user", "value");
        LogField numeric = LogField.String("1user", "value");
        LogField unsafeCharacter = LogField.String("user-name", "value");

        Assert.That(missing.IsValid, Is.False);
        Assert.That(padded.IsValid, Is.False);
        Assert.That(numeric.IsValid, Is.False);
        Assert.That(unsafeCharacter.IsValid, Is.False);
        Assert.That(missing.Name, Is.EqualTo(""));
        Assert.That(missing.Value.Kind, Is.EqualTo(LogValueKind.Null));
    }

    [Test]
    public void IsValidName_ReportsValidationResult()
    {
        Assert.That(LogField.IsValidName("user_id"), Is.True);
        Assert.That(LogField.IsValidName("http.status"), Is.True);
        Assert.That(LogField.IsValidName("_internal"), Is.True);
        Assert.That(LogField.IsValidName(""), Is.False);
        Assert.That(LogField.IsValidName("bad name"), Is.False);
    }

    [Test]
    public void TypedFactories_CreateExpectedValueKinds()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 14, 0, 0, TimeSpan.Zero);
        TimeSpan duration = TimeSpan.FromMilliseconds(15);

        Assert.That(LogField.String("text", "hello").Value.Kind, Is.EqualTo(LogValueKind.String));
        Assert.That(LogField.Bool("flag", true).Value.Kind, Is.EqualTo(LogValueKind.Bool));
        Assert.That(LogField.Int("count", 2).Value.Kind, Is.EqualTo(LogValueKind.Integer));
        Assert.That(LogField.Long("total", 3).Value.IntegerValue, Is.EqualTo(3));
        Assert.That(LogField.Decimal("price", 4.5m).Value.Kind, Is.EqualTo(LogValueKind.Decimal));
        Assert.That(LogField.Double("ratio", 0.75d).Value.Kind, Is.EqualTo(LogValueKind.FloatingPoint));
        Assert.That(LogField.DateTime("created", timestamp).Value.DateTimeValue, Is.EqualTo(timestamp));
        Assert.That(LogField.Duration("elapsed", duration).Value.DurationValue, Is.EqualTo(duration));
    }

    [Test]
    public void ComplexFactories_CreateExpectedValueKinds()
    {
        Dictionary<string, LogValue> dictionary = new Dictionary<string, LogValue>();
        dictionary["count"] = LogValue.Integer(1);

        Assert.That(LogField.Exception(new InvalidOperationException("bad")).Name, Is.EqualTo("exception"));
        Assert.That(LogField.ExceptionNamed("error", new InvalidOperationException("bad")).Value.Kind, Is.EqualTo(LogValueKind.Exception));
        Assert.That(LogField.ObjectText("uri", new Uri("https://example.test")).Value.Kind, Is.EqualTo(LogValueKind.ObjectText));
        Assert.That(LogField.Array("items", new LogValue[] { LogValue.String("a") }).Value.Kind, Is.EqualTo(LogValueKind.Array));
        Assert.That(LogField.Dictionary("data", dictionary).Value.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(LogField.Raw("raw", "{\"a\":1}").Value.Kind, Is.EqualTo(LogValueKind.Raw));
    }
}
