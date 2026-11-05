using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class ObjectIdParserTests
{
    [Test]
    public void Parse_AcceptsCanonicalObjectId()
    {
        ObjectIdParseResult result = new ObjectId().Parse("507f1f77bcf86cd799439011");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.NormalizedText, Is.EqualTo("507f1f77bcf86cd799439011"));
        Assert.That(result.Value.UnixSeconds(), Is.EqualTo(1350508407));
        Assert.That(result.Value.ToBytes(), Is.EqualTo(new byte[] { 0x50, 0x7F, 0x1F, 0x77, 0xBC, 0xF8, 0x6C, 0xD7, 0x99, 0x43, 0x90, 0x11 }));
    }

    [Test]
    public void Parse_AcceptsUppercaseAndNormalizesToLowercase()
    {
        ObjectIdParseResult result = new ObjectId().Parse("507F1F77BCF86CD799439011");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.NormalizedText, Is.EqualTo("507f1f77bcf86cd799439011"));
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.ToHexString(), Is.EqualTo("507f1f77bcf86cd799439011"));
    }

    [Test]
    public void Validate_ReturnsNormalizedValidationResult()
    {
        IdentifierValidationResult result = new ObjectId().Validate("507F1F77BCF86CD799439011");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.ObjectId));
        Assert.That(result.NormalizedText, Is.EqualTo("507f1f77bcf86cd799439011"));
        Assert.That(result.Error, Is.EqualTo(""));
    }

    [Test]
    public void Parse_RejectsInvalidLengthAndCharacters()
    {
        AssertParseFailure(null, "ObjectId text is null");
        AssertParseFailure("507f", "expected 24 hex characters but found 4");
        AssertParseFailure("507f1f77bcf86cd79943901z", "invalid hex character at index 23");
    }

    [Test]
    public void TryParse_DelegatesToParse()
    {
        ObjectIdParseResult result = new ObjectId().TryParse("507f1f77bcf86cd799439011");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.NormalizedText, Is.EqualTo("507f1f77bcf86cd799439011"));
    }

    [Test]
    public void ParseValue_ReturnsValueOrThrows()
    {
        ObjectIdValue value = new ObjectId().ParseValue("507f1f77bcf86cd799439011");

        Assert.That(value.UnixSeconds(), Is.EqualTo(1350508407));
        Assert.That(() => new ObjectId().ParseValue("not-an-objectid"), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void Format_ReturnsCanonicalTextOrThrowsForNull()
    {
        ObjectIdValue value = new ObjectId().ParseValue("507F1F77BCF86CD799439011");

        Assert.That(new ObjectId().Format(value), Is.EqualTo("507f1f77bcf86cd799439011"));
        Assert.That(new ObjectId().FormatUpper(value), Is.EqualTo("507F1F77BCF86CD799439011"));
        Assert.That(() => new ObjectId().Format(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new ObjectId().FormatUpper(null), Throws.TypeOf<ArgumentNullException>());
    }

    static void AssertParseFailure(string? text, string error)
    {
        ObjectIdParseResult result = new ObjectId().Parse(text);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
    }
}
