using System.Text.Json;
using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicTypedValueFactoryTests
{
    [Test]
    public void TypedValueMethods_CreateEverySupportedKind()
    {
        ILogValueFactory factory = new LogFactory();
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 25, 16, 0, 0, TimeSpan.Zero);
        TimeSpan duration = TimeSpan.FromMilliseconds(125);
        InvalidOperationException exceptionValue = new InvalidOperationException("failure");
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary["enabled"] = true;

        LogValue nullValue = factory.CreateNullValue();
        LogValue stringValue = factory.CreateStringValue("text");
        LogValue boolValue = factory.CreateBoolValue(true);
        LogValue intValue = factory.CreateIntValue(12);
        LogValue longValue = factory.CreateLongValue(5000000000L);
        LogValue decimalValue = factory.CreateDecimalValue(12.5m);
        LogValue doubleValue = factory.CreateDoubleValue(2.5d);
        LogValue dateTimeValue = factory.CreateDateTimeValue(timestamp);
        LogValue durationValue = factory.CreateDurationValue(duration);
        LogValue exception = factory.CreateExceptionValue(exceptionValue);
        LogValue objectText = factory.CreateObjectTextValue(new Uri("https://example.test/path"));
        LogValue array = factory.CreateArrayValue(new object?[] { 1, "two" });
        LogValue dictionaryValue = factory.CreateDictionaryValue(dictionary);
        LogValue rawJson = factory.CreateRawJsonValue("{\"ok\":true}");

        Assert.That(nullValue.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(stringValue.TextValue, Is.EqualTo("text"));
        Assert.That(boolValue.BoolValue, Is.True);
        Assert.That(intValue.IntegerValue, Is.EqualTo(12));
        Assert.That(longValue.IntegerValue, Is.EqualTo(5000000000L));
        Assert.That(decimalValue.DecimalValue, Is.EqualTo(12.5m));
        Assert.That(doubleValue.FloatingPointValue, Is.EqualTo(2.5d));
        Assert.That(dateTimeValue.DateTimeValue, Is.EqualTo(timestamp));
        Assert.That(durationValue.DurationValue, Is.EqualTo(duration));
        Assert.That(exception.ExceptionValue, Is.SameAs(exceptionValue));
        Assert.That(exception.TextValue, Does.Contain("failure"));
        Assert.That(objectText.TextValue, Is.EqualTo("https://example.test/path"));
        Assert.That(array.Kind, Is.EqualTo(LogValueKind.Array));
        Assert.That(array.ArrayValue[0].IntegerValue, Is.EqualTo(1));
        Assert.That(array.ArrayValue[1].TextValue, Is.EqualTo("two"));
        Assert.That(dictionaryValue.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(dictionaryValue.DictionaryValue["enabled"].BoolValue, Is.True);
        Assert.That(rawJson.Kind, Is.EqualTo(LogValueKind.Raw));
        Assert.That(JsonDocument.Parse(rawJson.RawValue).RootElement.GetProperty("ok").GetBoolean(), Is.True);
    }

    [Test]
    public void TypedFieldMethods_CreateEverySupportedKindAndValidateNames()
    {
        ILogValueFactory factory = new LogFactory();
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 25, 16, 30, 0, TimeSpan.Zero);
        TimeSpan duration = TimeSpan.FromSeconds(3);
        InvalidOperationException exceptionValue = new InvalidOperationException("failure");
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary["count"] = 2;

        LogField[] fields = new LogField[]
        {
            factory.CreateNullField("nullValue"),
            factory.CreateStringField("stringValue", "text"),
            factory.CreateBoolField("boolValue", true),
            factory.CreateIntField("intValue", 1),
            factory.CreateLongField("longValue", 2L),
            factory.CreateDecimalField("decimalValue", 3m),
            factory.CreateDoubleField("doubleValue", 4d),
            factory.CreateDateTimeField("dateTimeValue", timestamp),
            factory.CreateDurationField("durationValue", duration),
            factory.CreateExceptionField("exceptionValue", exceptionValue),
            factory.CreateObjectTextField("objectText", new Uri("https://example.test")),
            factory.CreateArrayField("arrayValue", new object?[] { "item" }),
            factory.CreateDictionaryField("dictionaryValue", dictionary),
            factory.CreateRawJsonField("rawValue", "[1,2]")
        };

        LogValueKind[] kinds = new LogValueKind[]
        {
            LogValueKind.Null,
            LogValueKind.String,
            LogValueKind.Bool,
            LogValueKind.Integer,
            LogValueKind.Integer,
            LogValueKind.Decimal,
            LogValueKind.FloatingPoint,
            LogValueKind.DateTime,
            LogValueKind.Duration,
            LogValueKind.Exception,
            LogValueKind.ObjectText,
            LogValueKind.Array,
            LogValueKind.Dictionary,
            LogValueKind.Raw
        };

        int index = 0;
        while (index < fields.Length)
        {
            Assert.That(fields[index].IsValid, Is.True);
            Assert.That(fields[index].Value.Kind, Is.EqualTo(kinds[index]));
            index++;
        }

        LogField invalid = factory.CreateIntField("bad name", 1);
        Assert.That(invalid.IsValid, Is.False);
        Assert.That(invalid.Value.Kind, Is.EqualTo(LogValueKind.Null));
    }

    [Test]
    public void LimitedMethods_ApplyStringCollectionAndNestingLimits()
    {
        ILogValueFactory factory = new LogFactory();
        object?[] nested = new object?[]
        {
            "abcdef",
            new object?[] { "deep" },
            3
        };
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary["first"] = "abcdef";
        dictionary["second"] = new object?[] { "deep" };
        dictionary["third"] = 3;

        LogValue stringValue = factory.CreateStringValueWithLimit("abcdef", 3);
        LogValue exceptionValue = factory.CreateExceptionValueWithLimit(new InvalidOperationException("abcdef"), 8);
        LogValue objectText = factory.CreateObjectTextValueWithLimit(new Uri("https://example.test"), 5);
        LogValue array = factory.CreateArrayValueWithLimits(nested, 3, 2, 1);
        LogValue dictionaryValue = factory.CreateDictionaryValueWithLimits(dictionary, 3, 2, 1);
        LogValue limitedValue = factory.CreateValueWithLimits(nested, 4, 1, 1);
        LogField limitedField = factory.CreateFieldWithLimits("payload", nested, 4, 1, 1);
        LogField stringField = factory.CreateStringFieldWithLimit("text", "abcdef", 2);
        LogField exceptionField = factory.CreateExceptionFieldWithLimit("error", new InvalidOperationException("abcdef"), 7);
        LogField objectField = factory.CreateObjectTextFieldWithLimit("object", new Uri("https://example.test"), 6);
        LogField arrayField = factory.CreateArrayFieldWithLimits("items", nested, 3, 2, 1);
        LogField dictionaryField = factory.CreateDictionaryFieldWithLimits("data", dictionary, 3, 2, 1);

        Assert.That(stringValue.TextValue, Is.EqualTo("abc"));
        Assert.That(stringValue.IsTruncated, Is.True);
        Assert.That(exceptionValue.TextValue.Length, Is.EqualTo(8));
        Assert.That(exceptionValue.IsTruncated, Is.True);
        Assert.That(objectText.TextValue, Is.EqualTo("https"));
        Assert.That(array.ArrayValue.Length, Is.EqualTo(2));
        Assert.That(array.IsTruncated, Is.True);
        Assert.That(array.ArrayValue[0].TextValue, Is.EqualTo("abc"));
        Assert.That(array.ArrayValue[1].Kind, Is.EqualTo(LogValueKind.ObjectText));
        Assert.That(array.ArrayValue[1].TextValue, Is.EqualTo("[de"));
        Assert.That(array.CapturedDepth, Is.EqualTo(1));
        Assert.That(dictionaryValue.DictionaryValue.Count, Is.EqualTo(2));
        Assert.That(dictionaryValue.DictionaryValue["first"].TextValue, Is.EqualTo("abc"));
        Assert.That(dictionaryValue.DictionaryValue["second"].Kind, Is.EqualTo(LogValueKind.ObjectText));
        Assert.That(limitedValue.ArrayValue.Length, Is.EqualTo(1));
        Assert.That(limitedValue.ArrayValue[0].TextValue, Is.EqualTo("abcd"));
        Assert.That(limitedField.Value.ArrayValue.Length, Is.EqualTo(1));
        Assert.That(stringField.Value.TextValue, Is.EqualTo("ab"));
        Assert.That(exceptionField.Value.IsTruncated, Is.True);
        Assert.That(objectField.Value.TextValue, Is.EqualTo("https:"));
        Assert.That(arrayField.Value.ArrayValue.Length, Is.EqualTo(2));
        Assert.That(dictionaryField.Value.DictionaryValue.Count, Is.EqualTo(2));
    }

    [Test]
    public void RawJsonMethods_ValidateSyntaxAndConfiguredLimits()
    {
        ILogValueFactory factory = new LogFactory();

        LogValue value = factory.CreateRawJsonValueWithLimits("{\"item\":{\"id\":1}}", 64, 2);
        LogField field = factory.CreateRawJsonFieldWithLimits("payload", "[true,false]", 32, 1);

        Assert.That(value.RawValue, Is.EqualTo("{\"item\":{\"id\":1}}"));
        Assert.That(value.CapturedDepth, Is.EqualTo(2));
        Assert.That(field.Value.RawValue, Is.EqualTo("[true,false]"));
        Assert.That(field.Value.CapturedDepth, Is.EqualTo(1));
        Assert.Catch<JsonException>(() => factory.CreateRawJsonValue("not-json"));
        Assert.Catch<JsonException>(() => factory.CreateRawJsonField("payload", null));
        Assert.Throws<ArgumentException>(() => factory.CreateRawJsonValueWithLimits("{\"value\":1}", 5, 1));
        Assert.Throws<ArgumentException>(() => factory.CreateRawJsonFieldWithLimits("payload", "{\"nested\":{\"value\":1}}", 64, 1));
    }

    [Test]
    public void TypedMethods_NormalizeNullCollectionsExceptionsAndObjects()
    {
        ILogValueFactory factory = new LogFactory();

        Assert.That(factory.CreateValueWithLimits(null, 1, 1, 1).Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(factory.CreateStringValue(null).Kind, Is.EqualTo(LogValueKind.String));
        Assert.That(factory.CreateStringValue(null).TextValue, Is.Empty);
        Assert.That(factory.CreateArrayValue(null).Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(factory.CreateDictionaryValue(null).Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(factory.CreateExceptionValue(null).Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(factory.CreateObjectTextValue(null).Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(factory.CreateArrayField("items", null).Value.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(factory.CreateDictionaryField("data", null).Value.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(factory.CreateExceptionField("error", null).Value.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(factory.CreateObjectTextField("object", null).Value.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(factory.CreateFieldWithLimits("value", null, 1, 1, 1).Value.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(factory.CreateStringField("text", null).Value.Kind, Is.EqualTo(LogValueKind.String));
        Assert.That(factory.CreateStringField("text", null).Value.TextValue, Is.Empty);
    }

    [Test]
    public void TypedMethods_PreserveNumericExtremesAndNormalizeZeroLimits()
    {
        ILogValueFactory factory = new LogFactory();
        LogValue minimumLong = factory.CreateLongValue(long.MinValue);
        LogValue maximumLong = factory.CreateLongValue(long.MaxValue);
        LogValue minimumDecimal = factory.CreateDecimalValue(decimal.MinValue);
        LogValue maximumDecimal = factory.CreateDecimalValue(decimal.MaxValue);
        LogValue notANumber = factory.CreateDoubleValue(double.NaN);
        LogValue positiveInfinity = factory.CreateDoubleValue(double.PositiveInfinity);
        LogValue negativeInfinity = factory.CreateDoubleValue(double.NegativeInfinity);
        LogValue zeroText = factory.CreateStringValueWithLimit("abc", -1);
        LogValue zeroCollection = factory.CreateArrayValueWithLimits(new object?[] { 1, 2 }, int.MaxValue, -1, int.MaxValue);
        LogValue zeroDepth = factory.CreateValueWithLimits(new object?[] { 1 }, 64, 64, 0);
        LogField maximumField = factory.CreateLongField("Maximum", long.MaxValue);

        Assert.That(minimumLong.IntegerValue, Is.EqualTo(long.MinValue));
        Assert.That(maximumLong.IntegerValue, Is.EqualTo(long.MaxValue));
        Assert.That(minimumDecimal.DecimalValue, Is.EqualTo(decimal.MinValue));
        Assert.That(maximumDecimal.DecimalValue, Is.EqualTo(decimal.MaxValue));
        Assert.That(double.IsNaN(notANumber.FloatingPointValue), Is.True);
        Assert.That(double.IsPositiveInfinity(positiveInfinity.FloatingPointValue), Is.True);
        Assert.That(double.IsNegativeInfinity(negativeInfinity.FloatingPointValue), Is.True);
        Assert.That(zeroText.TextValue, Is.Empty);
        Assert.That(zeroText.IsTruncated, Is.True);
        Assert.That(zeroText.OriginalLength, Is.EqualTo(3));
        Assert.That(zeroText.CapturedLength, Is.Zero);
        Assert.That(zeroText.MaximumLength, Is.Zero);
        Assert.That(zeroCollection.ArrayValue, Is.Empty);
        Assert.That(zeroCollection.IsTruncated, Is.True);
        Assert.That(zeroCollection.OriginalLength, Is.EqualTo(2));
        Assert.That(zeroCollection.MaximumLength, Is.Zero);
        Assert.That(zeroDepth.Kind, Is.EqualTo(LogValueKind.ObjectText));
        Assert.That(zeroDepth.TextValue, Is.EqualTo("[depth limit]"));
        Assert.That(maximumField.Value.IntegerValue, Is.EqualTo(long.MaxValue));
    }
}
