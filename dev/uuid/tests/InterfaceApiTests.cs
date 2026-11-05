using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class InterfaceApiTests
{
    [TestCase("550e8400-e29b-41d4-a716-446655440000", IdentifierKind.Uuid, typeof(UuidValue))]
    [TestCase("01ARZ3NDEKTSV4RRFFQ69G5FAV", IdentifierKind.Ulid, typeof(UlidValue))]
    [TestCase("0ujtsYcgvSTl8PAuAdqWYSMnLOv", IdentifierKind.Ksuid, typeof(KsuidValue))]
    [TestCase("507f1f77bcf86cd799439011", IdentifierKind.ObjectId, typeof(ObjectIdValue))]
    [TestCase("4194308096", IdentifierKind.Snowflake, typeof(SnowflakeValue))]
    public void Parse_ReturnsConcreteValueThroughCommonInterface(string text, IdentifierKind kind, Type valueType)
    {
        IdentifierParseResult result = new Identifier().Parse(text);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.InstanceOf(valueType));
        Assert.That(result.Value.Kind(), Is.EqualTo(kind));
        Assert.That(result.Value.ToCanonicalString(), Is.EqualTo(result.NormalizedText));
        Assert.That(new Identifier().NormalizeValue(result.Value), Is.EqualTo(result.NormalizedText));
    }

    [Test]
    public void Parse_ReturnsCommonValueForNanoIdAndNoValueOnFailure()
    {
        IdentifierParseResult nanoId = new Identifier().Parse("V1StGXR8_Z5jdHi6B-myT");
        IdentifierParseResult failure = new Identifier().Parse("not an identifier");

        Assert.That(nanoId.Ok, Is.True);
        Assert.That(nanoId.Value, Is.Not.Null);
        Assert.That(nanoId.Value.Kind(), Is.EqualTo(IdentifierKind.NanoId));
        Assert.That(nanoId.Value.ToCanonicalString(), Is.EqualTo("V1StGXR8_Z5jdHi6B-myT"));
        Assert.That(failure.Value, Is.Null);
    }

    [Test]
    public void CommonValueOverloads_CompareAndExtractTimestamps()
    {
        IdentifierParseResult earlierResult = new Identifier().Parse("000000000000000000000000");
        IdentifierParseResult laterResult = new Identifier().Parse("000000010000000000000000");
        Assert.That(earlierResult.Value, Is.Not.Null);
        Assert.That(laterResult.Value, Is.Not.Null);
        IIdentifierValue earlier = earlierResult.Value;
        IIdentifierValue later = laterResult.Value;

        IdentifierTimestampResult timestamp = new Identifier().TimestampValue(later);

        Assert.That(new Identifier().CompareValues(earlier, later), Is.LessThan(0));
        Assert.That(timestamp.Ok, Is.True);
        Assert.That(timestamp.Kind, Is.EqualTo(IdentifierKind.ObjectId));
        Assert.That(timestamp.UnixSeconds, Is.EqualTo(1));
    }

    [Test]
    public void CommonValueContract_CanBeImplementedByConsumers()
    {
        IIdentifierValue left = new TestIdentifierValue(IdentifierKind.Ulid, "01ARZ3NDEKTSV4RRFFQ69G5FAV");
        IIdentifierValue right = new TestIdentifierValue(IdentifierKind.Ulid, "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        Assert.That(new Identifier().NormalizeValue(left), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(new Identifier().CompareValues(left, right), Is.LessThan(0));
        Assert.That(new Identifier().TimestampValue(left).Ok, Is.True);
    }

    class TestIdentifierValue : IIdentifierValue
    {
        IdentifierKind IdentifierKind;
        string Text;

        public TestIdentifierValue(IdentifierKind kind, string text)
        {
            IdentifierKind = kind;
            Text = text;
        }

        public IdentifierKind Kind()
        {
            return IdentifierKind;
        }

        public string ToCanonicalString()
        {
            return Text;
        }
    }
}
