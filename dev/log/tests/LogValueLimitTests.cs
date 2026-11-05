using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogValueLimitTests
{
    [Test]
    public void StringLimit_TruncatesTextAndRecordsMetadata()
    {
        LogValue value = LogValue.StringLimit("abcdef", 3);

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.String));
        Assert.That(value.TextValue, Is.EqualTo("abc"));
        Assert.That(value.IsTruncated, Is.True);
        Assert.That(value.OriginalLength, Is.EqualTo(6));
        Assert.That(value.CapturedLength, Is.EqualTo(3));
        Assert.That(value.MaximumLength, Is.EqualTo(3));
    }

    [Test]
    public void RawLimitAndObjectTextLimit_TruncateText()
    {
        LogValue raw = LogValue.RawLimit("123456", 4);
        LogValue text = LogValue.ObjectTextLimit(new Uri("https://example.test/path"), 12);

        Assert.That(raw.RawValue, Is.EqualTo("1234"));
        Assert.That(raw.IsTruncated, Is.True);
        Assert.That(text.TextValue, Is.EqualTo("https://exam"));
        Assert.That(text.IsTruncated, Is.True);
    }

    [Test]
    public void ArrayLimit_CapturesOnlyMaximumEntries()
    {
        LogValue value = LogValue.ArrayLimit(
            new LogValue[]
            {
                LogValue.Integer(1),
                LogValue.Integer(2),
                LogValue.Integer(3)
            },
            2);

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.Array));
        Assert.That(value.ArrayValue.Length, Is.EqualTo(2));
        Assert.That(value.IsTruncated, Is.True);
        Assert.That(value.OriginalLength, Is.EqualTo(3));
        Assert.That(value.CapturedLength, Is.EqualTo(2));
    }

    [Test]
    public void DictionaryLimit_CapturesOnlyMaximumEntries()
    {
        Dictionary<string, LogValue> values = new Dictionary<string, LogValue>();
        values["a"] = LogValue.Integer(1);
        values["b"] = LogValue.Integer(2);
        values["c"] = LogValue.Integer(3);

        LogValue value = LogValue.DictionaryLimit(values, 2);

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(value.DictionaryValue.Count, Is.EqualTo(2));
        Assert.That(value.IsTruncated, Is.True);
        Assert.That(value.OriginalLength, Is.EqualTo(3));
        Assert.That(value.CapturedLength, Is.EqualTo(2));
    }

    [Test]
    public void BinderLimit_AppliesStringAndCollectionLimits()
    {
        object[] values = new object[]
        {
            "abcdef",
            new object[] { 1, 2, 3 }
        };

        LogValue array = LogTemplateBinder.ToArrayValueLimit(values, 3, 1, 4);

        Assert.That(array.ArrayValue.Length, Is.EqualTo(1));
        Assert.That(array.IsTruncated, Is.True);
        Assert.That(array.ArrayValue[0].TextValue, Is.EqualTo("abc"));
        Assert.That(array.ArrayValue[0].IsTruncated, Is.True);
    }

    [Test]
    public void BinderLimit_StopsAtMaximumDepth()
    {
        object[] nested = new object[]
        {
            new object[] { "deep" }
        };

        LogValue value = LogTemplateBinder.ToLogValueLimit(nested, 100, 10, 1);

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.Array));
        Assert.That(value.ArrayValue[0].Kind, Is.EqualTo(LogValueKind.ObjectText));
        Assert.That(value.ArrayValue[0].TextValue, Is.EqualTo("[depth limit]"));
    }
}
