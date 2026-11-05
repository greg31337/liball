using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class IdentifierClassificationTests
{
    [Test]
    public void Classify_RecognizesUuidText()
    {
        IdentifierValidationResult canonical = new Identifier().Classify("550e8400-e29b-41d4-a716-446655440000");
        IdentifierValidationResult compact = new Identifier().Classify("550E8400E29B41D4A716446655440000");
        IdentifierValidationResult urn = new Identifier().Classify("urn:uuid:550e8400-e29b-41d4-a716-446655440000");

        AssertClassification(canonical, IdentifierKind.Uuid, "550e8400-e29b-41d4-a716-446655440000");
        AssertClassification(compact, IdentifierKind.Uuid, "550e8400-e29b-41d4-a716-446655440000");
        AssertClassification(urn, IdentifierKind.Uuid, "550e8400-e29b-41d4-a716-446655440000");
    }

    [Test]
    public void Classify_RecognizesUlidText()
    {
        IdentifierValidationResult result = new Identifier().Classify("01arz3ndektsv4rrffq69g5fav");

        AssertClassification(result, IdentifierKind.Ulid, "01ARZ3NDEKTSV4RRFFQ69G5FAV");
    }

    [Test]
    public void Classify_RecognizesKsuidText()
    {
        IdentifierValidationResult result = new Identifier().Classify("000000000000000000000000000");

        AssertClassification(result, IdentifierKind.Ksuid, "000000000000000000000000000");
    }

    [Test]
    public void Classify_RecognizesObjectIdText()
    {
        IdentifierValidationResult result = new Identifier().Classify("507F1F77BCF86CD799439011");

        AssertClassification(result, IdentifierKind.ObjectId, "507f1f77bcf86cd799439011");
    }

    [Test]
    public void Classify_RecognizesSnowflakeDecimalText()
    {
        IdentifierValidationResult result = new Identifier().Classify("000123456789");

        AssertClassification(result, IdentifierKind.Snowflake, "123456789");
    }

    [Test]
    public void Classify_RecognizesDefaultNanoIdText()
    {
        string text = NanoIdSettings.DefaultAlphabet.Substring(0, NanoIdSettings.DefaultSize);

        IdentifierValidationResult result = new Identifier().Classify(text);

        AssertClassification(result, IdentifierKind.NanoId, text);
    }

    [Test]
    public void Classify_ReturnsUnknownForUnsupportedText()
    {
        IdentifierValidationResult nullResult = new Identifier().Classify(null);
        IdentifierValidationResult empty = new Identifier().Classify("");
        IdentifierValidationResult unsupported = new Identifier().Classify("not an identifier");

        AssertFailure(nullResult, "identifier text is null");
        AssertFailure(empty, "identifier kind is unknown");
        AssertFailure(unsupported, "identifier kind is unknown");
        Assert.That(new Identifier().ClassifyKind("not an identifier"), Is.EqualTo(IdentifierKind.Unknown));
    }

    [Test]
    public void NanoIdValidate_ValidatesDefaultAndCustomText()
    {
        string defaultText = NanoIdSettings.DefaultAlphabet.Substring(0, NanoIdSettings.DefaultSize);
        NanoIdSettings custom = new NanoIdSettings("abc", 4);

        IdentifierValidationResult defaultResult = new NanoId().Validate(defaultText);
        IdentifierValidationResult customResult = new NanoId().ValidateWithSettings("abca", custom);

        AssertClassification(defaultResult, IdentifierKind.NanoId, defaultText);
        AssertClassification(customResult, IdentifierKind.NanoId, "abca");
    }

    [Test]
    public void NanoIdValidate_RejectsInvalidTextAndSettings()
    {
        NanoIdSettings invalidAlphabet = new NanoIdSettings("abc", 4);
        invalidAlphabet.Alphabet = "abca";
        NanoIdSettings invalidSize = new NanoIdSettings("abc", 4);
        invalidSize.Size = -1;

        AssertFailure(new NanoId().Validate(null), "Nano ID text is null");
        AssertFailure(new NanoId().Validate("abc"), "expected 21 Nano ID characters but found 3");
        AssertFailure(new NanoId().ValidateWithSettings("abc$", new NanoIdSettings("abc", 4)), "invalid Nano ID character at index 3");
        AssertFailure(new NanoId().ValidateWithSettings("abca", null), "Nano ID settings is null");
        AssertFailure(new NanoId().ValidateWithSettings("abca", invalidAlphabet), "duplicate Nano ID alphabet character at index 3");
        AssertFailure(new NanoId().ValidateWithSettings("abca", invalidSize), "Nano ID size cannot be negative");
    }

    static void AssertClassification(IdentifierValidationResult result, IdentifierKind kind, string normalizedText)
    {
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(kind));
        Assert.That(result.NormalizedText, Is.EqualTo(normalizedText));
        Assert.That(result.Error, Is.EqualTo(""));
    }

    static void AssertFailure(IdentifierValidationResult result, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
    }
}
