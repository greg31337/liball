using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogValueTests
{
    [Test]
    public void PrimitiveFactories_StoreTypedValues()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 12, 30, 0, TimeSpan.Zero);
        TimeSpan duration = TimeSpan.FromSeconds(42);

        Assert.That(LogValue.Null().Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(LogValue.String("hello").TextValue, Is.EqualTo("hello"));
        Assert.That(LogValue.Bool(true).BoolValue, Is.True);
        Assert.That(LogValue.Integer(123).IntegerValue, Is.EqualTo(123));
        Assert.That(LogValue.Decimal(12.5m).DecimalValue, Is.EqualTo(12.5m));
        Assert.That(LogValue.FloatingPoint(1.25d).FloatingPointValue, Is.EqualTo(1.25d));
        Assert.That(LogValue.DateTime(timestamp).DateTimeValue, Is.EqualTo(timestamp));
        Assert.That(LogValue.Duration(duration).DurationValue, Is.EqualTo(duration));
    }

    [Test]
    public void ExceptionFactory_StoresExceptionAndText()
    {
        InvalidOperationException exception = new InvalidOperationException("bad state");

        LogValue value = LogValue.Exception(exception);

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.Exception));
        Assert.That(value.ExceptionValue, Is.SameAs(exception));
        Assert.That(value.TextValue, Does.Contain("bad state"));
    }

    [Test]
    public void ObjectTextFactory_UsesToStringWithoutReflection()
    {
        Uri uri = new Uri("https://example.test/path");

        LogValue value = LogValue.ObjectText(uri);

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.ObjectText));
        Assert.That(value.TextValue, Is.EqualTo("https://example.test/path"));
    }

    [Test]
    public void NullInputs_NormalizeToNullOrEmptyText()
    {
        Assert.That(LogValue.String(null).TextValue, Is.EqualTo(""));
        Assert.That(LogValue.Exception(null).Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(LogValue.ObjectText(null).Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(LogValue.Array(null).Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(LogValue.Dictionary(null).Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(LogValue.Raw(null).RawValue, Is.EqualTo(""));
    }

    [Test]
    public void ArrayFactory_CopiesArrayAndNormalizesNullElements()
    {
        LogValue[] original = new LogValue[]
        {
            LogValue.String("first"),
            null!
        };

        LogValue value = LogValue.Array(original);
        original[0] = LogValue.String("changed");

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.Array));
        Assert.That(value.ArrayValue.Length, Is.EqualTo(2));
        Assert.That(value.ArrayValue[0].TextValue, Is.EqualTo("first"));
        Assert.That(value.ArrayValue[1].Kind, Is.EqualTo(LogValueKind.Null));
    }

    [Test]
    public void DictionaryFactory_CopiesDictionaryAndNormalizesNullValues()
    {
        Dictionary<string, LogValue> original = new Dictionary<string, LogValue>();
        original["name"] = LogValue.String("worker");
        original["missing"] = null!;

        LogValue value = LogValue.Dictionary(original);
        original["name"] = LogValue.String("changed");

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(value.DictionaryValue["name"].TextValue, Is.EqualTo("worker"));
        Assert.That(value.DictionaryValue["missing"].Kind, Is.EqualTo(LogValueKind.Null));
    }

    [Test]
    public void RawFactory_StoresRawJsonCompatibleText()
    {
        LogValue value = LogValue.Raw("{\"ok\":true}");

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.Raw));
        Assert.That(value.RawValue, Is.EqualTo("{\"ok\":true}"));
    }
}
