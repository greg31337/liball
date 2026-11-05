using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class HexToolsTests
{
    [Test]
    public void OversizedExpectedLength_IsRejectedWithoutArithmeticOverflow()
    {
        Assert.That(() => HexTools.ParseFixedLength("", int.MaxValue), Throws.ArgumentException);
        Assert.That(HexTools.ValidateFixedHex("", int.MaxValue, IdentifierKind.Unknown).Ok, Is.False);
    }

    [Test]
    public void HexValue_AcceptsUppercaseAndLowercaseHex()
    {
        Assert.That(HexTools.HexValue('0'), Is.EqualTo(0));
        Assert.That(HexTools.HexValue('9'), Is.EqualTo(9));
        Assert.That(HexTools.HexValue('a'), Is.EqualTo(10));
        Assert.That(HexTools.HexValue('f'), Is.EqualTo(15));
        Assert.That(HexTools.HexValue('A'), Is.EqualTo(10));
        Assert.That(HexTools.HexValue('F'), Is.EqualTo(15));
        Assert.That(HexTools.HexValue('g'), Is.EqualTo(-1));
    }

    [Test]
    public void ParseHex_ParsesMixedCaseHexToBytes()
    {
        byte[] bytes = HexTools.ParseHex("0012aBfF");

        Assert.That(bytes, Is.EqualTo(new byte[] { 0x00, 0x12, 0xAB, 0xFF }));
    }

    [Test]
    public void ParseHex_RejectsOddLengthAndInvalidCharacters()
    {
        Assert.That(() => HexTools.ParseHex("123"), Throws.ArgumentException);
        Assert.That(() => HexTools.ParseHex("12xz"), Throws.ArgumentException);
        Assert.That(() => HexTools.ParseHex(null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void ParseFixedLength_RequiresExactByteLength()
    {
        Assert.That(HexTools.ParseFixedLength("abcdef", 3), Is.EqualTo(new byte[] { 0xAB, 0xCD, 0xEF }));
        Assert.That(() => HexTools.ParseFixedLength("abcdef", 2), Throws.ArgumentException);
        Assert.That(() => HexTools.ParseFixedLength("abcdef", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void FormatMethods_FormatLowercaseAndUppercaseHex()
    {
        byte[] bytes = new byte[] { 0x00, 0x12, 0xAB, 0xFF };

        Assert.That(HexTools.FormatLower(bytes), Is.EqualTo("0012abff"));
        Assert.That(HexTools.FormatUpper(bytes), Is.EqualTo("0012ABFF"));
    }

    [Test]
    public void UuidCompactHelpers_RequireSixteenBytesOrThirtyTwoHexCharacters()
    {
        string text = "00112233445566778899aabbccddeeff";
        byte[] bytes = HexTools.ParseUuidCompact(text);

        Assert.That(bytes, Is.EqualTo(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }));
        Assert.That(HexTools.FormatUuidCompactLower(bytes), Is.EqualTo(text));
        Assert.That(HexTools.FormatUuidCompactUpper(bytes), Is.EqualTo("00112233445566778899AABBCCDDEEFF"));
        Assert.That(() => HexTools.ParseUuidCompact("001122"), Throws.ArgumentException);
        Assert.That(() => HexTools.FormatUuidCompactLower(new byte[15]), Throws.ArgumentException);
    }

    [Test]
    public void ObjectIdHelpers_RequireTwelveBytesOrTwentyFourHexCharacters()
    {
        string text = "507f1f77bcf86cd799439011";
        byte[] bytes = HexTools.ParseObjectId(text);

        Assert.That(bytes, Is.EqualTo(new byte[] { 0x50, 0x7F, 0x1F, 0x77, 0xBC, 0xF8, 0x6C, 0xD7, 0x99, 0x43, 0x90, 0x11 }));
        Assert.That(HexTools.FormatObjectIdLower(bytes), Is.EqualTo(text));
        Assert.That(HexTools.FormatObjectIdUpper(bytes), Is.EqualTo("507F1F77BCF86CD799439011"));
        Assert.That(() => HexTools.ParseObjectId("507f"), Throws.ArgumentException);
        Assert.That(() => HexTools.FormatObjectIdLower(new byte[13]), Throws.ArgumentException);
    }

    [Test]
    public void ValidateFixedHex_ReturnsNormalizedValidationResult()
    {
        IdentifierValidationResult valid = HexTools.ValidateFixedHex("AABB", 2, IdentifierKind.ObjectId);
        IdentifierValidationResult wrongLength = HexTools.ValidateFixedHex("AABB", 3, IdentifierKind.ObjectId);
        IdentifierValidationResult invalid = HexTools.ValidateFixedHex("AAZG", 2, IdentifierKind.ObjectId);

        Assert.That(valid.Ok, Is.True);
        Assert.That(valid.Kind, Is.EqualTo(IdentifierKind.ObjectId));
        Assert.That(valid.NormalizedText, Is.EqualTo("aabb"));
        Assert.That(valid.Error, Is.EqualTo(""));
        Assert.That(wrongLength.Ok, Is.False);
        Assert.That(wrongLength.Error, Is.EqualTo("expected 6 hex characters but found 4"));
        Assert.That(invalid.Ok, Is.False);
        Assert.That(invalid.Error, Is.EqualTo("invalid hex character at index 2"));
    }
}
