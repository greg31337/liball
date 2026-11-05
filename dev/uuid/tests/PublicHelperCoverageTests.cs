using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class PublicHelperCoverageTests
{
    [Test]
    public void UuidParserHelpers_RecognizeUrnPrefixesAndCanonicalHyphenLayout()
    {
        Assert.That(UuidParser.IsUrn("urn:uuid:00112233-4455-6677-8899-aabbccddeeff"), Is.True);
        Assert.That(UuidParser.IsUrn("URN:UUID:00112233-4455-6677-8899-aabbccddeeff"), Is.True);
        Assert.That(UuidParser.IsUrn("uuid:00112233-4455-6677-8899-aabbccddeeff"), Is.False);
        Assert.That(UuidParser.IsUrn(null), Is.False);

        Assert.That(UuidParser.HasCanonicalHyphens("00112233-4455-6677-8899-aabbccddeeff"), Is.True);
        Assert.That(UuidParser.HasCanonicalHyphens("001122334455-6677-8899-aabbccddeeff"), Is.False);
        Assert.That(UuidParser.HasCanonicalHyphens("00112233-4455-6677-8899-aabbccddeef"), Is.False);
    }

    [Test]
    public void ByteArrayTools_UncoveredBranchesHandleNullAndDefaultValidationKind()
    {
        IdentifierValidationResult valid = ByteArrayTools.ValidateLength(new byte[] { 1, 2, 3 }, 3);
        IdentifierValidationResult nullResult = ByteArrayTools.ValidateLength(null, 3);
        IdentifierValidationResult negativeLength = ByteArrayTools.ValidateLength(new byte[] { 1 }, -1);

        Assert.That(ByteArrayTools.Compare(null, new byte[] { 1 }), Is.LessThan(0));
        Assert.That(ByteArrayTools.Compare(new byte[] { 1 }, null), Is.GreaterThan(0));
        Assert.That(() => ByteArrayTools.Copy(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => ByteArrayTools.CopyLength(null, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => ByteArrayTools.CopyLength(new byte[] { 1 }, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(valid.Ok, Is.True);
        Assert.That(valid.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(nullResult.Ok, Is.False);
        Assert.That(nullResult.Error, Is.EqualTo("byte array is null"));
        Assert.That(negativeLength.Ok, Is.False);
        Assert.That(negativeLength.Error, Is.EqualTo("expected length cannot be negative"));
    }

    [Test]
    public void HexTools_UncoveredValidationBranchesHandleNullAndNegativeLengths()
    {
        IdentifierValidationResult nullText = HexTools.ValidateFixedHex(null, 2, IdentifierKind.ObjectId);
        IdentifierValidationResult negativeLength = HexTools.ValidateFixedHex("00", -1, IdentifierKind.ObjectId);

        Assert.That(HexTools.IsHex('0'), Is.True);
        Assert.That(HexTools.IsHex('F'), Is.True);
        Assert.That(HexTools.IsHex('x'), Is.False);
        Assert.That(() => HexTools.ParseFixedLength(null, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HexTools.FormatLower(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(nullText.Ok, Is.False);
        Assert.That(nullText.Error, Is.EqualTo("hex text is null"));
        Assert.That(negativeLength.Ok, Is.False);
        Assert.That(negativeLength.Error, Is.EqualTo("expected byte length cannot be negative"));
    }

    [Test]
    public void Base62PublicHelpers_ValidateCharactersLengthAndNonZeroBytes()
    {
        Assert.That(Base62.IsBase62('0'), Is.True);
        Assert.That(Base62.IsBase62('z'), Is.True);
        Assert.That(Base62.IsBase62('-'), Is.False);
        Assert.That(Base62.RequireLength("abc", 3), Is.EqualTo("abc"));
        Assert.That(() => Base62.RequireLength(null, 3), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Base62.RequireLength("abc", 2), Throws.ArgumentException);
        Assert.That(Base62.HasNonZeroByte(new byte[] { 0, 0 }), Is.False);
        Assert.That(Base62.HasNonZeroByte(new byte[] { 0, 1 }), Is.True);
    }

    [Test]
    public void CrockfordBase32PublicHelpers_ValidateCharactersLengthAndEncodingShape()
    {
        Assert.That(CrockfordBase32.IsCrockfordBase32('A'), Is.True);
        Assert.That(CrockfordBase32.IsCrockfordBase32('z'), Is.True);
        Assert.That(CrockfordBase32.IsCrockfordBase32('u'), Is.False);
        Assert.That(CrockfordBase32.RequireLength("01AR", 4), Is.EqualTo("01AR"));
        Assert.That(() => CrockfordBase32.RequireLength(null, 4), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CrockfordBase32.RequireLength("01AR", 5), Throws.ArgumentException);
        Assert.That(CrockfordBase32.ValidateEncodingShape(1, 2), Is.EqualTo(2));
        Assert.That(() => CrockfordBase32.ValidateEncodingShape(-1, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CrockfordBase32.ValidateEncodingShape(1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CrockfordBase32.ValidateEncodingShape(2, 1), Throws.ArgumentException);
    }
}
