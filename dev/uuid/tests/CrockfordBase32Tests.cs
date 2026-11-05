using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class CrockfordBase32Tests
{
    [Test]
    public void OversizedEncodingShape_IsRejectedWithoutArithmeticOverflow()
    {
        Assert.That(() => CrockfordBase32.ValidateEncodingShape(int.MaxValue, int.MaxValue), Throws.ArgumentException);
    }

    [Test]
    public void DecodeValue_AcceptsUlidAlphabetCaseInsensitively()
    {
        Assert.That(CrockfordBase32.DecodeValue('0'), Is.EqualTo(0));
        Assert.That(CrockfordBase32.DecodeValue('9'), Is.EqualTo(9));
        Assert.That(CrockfordBase32.DecodeValue('A'), Is.EqualTo(10));
        Assert.That(CrockfordBase32.DecodeValue('z'), Is.EqualTo(31));
        Assert.That(CrockfordBase32.DecodeValue('I'), Is.EqualTo(-1));
        Assert.That(CrockfordBase32.DecodeValue('O'), Is.EqualTo(-1));
        Assert.That(CrockfordBase32.DecodeValue('U'), Is.EqualTo(-1));
    }

    [Test]
    public void EncodeAndDecodeUlidBytes_MatchCanonicalSpecExample()
    {
        byte[] bytes = new byte[16];
        BigEndianTools.WriteUInt48(bytes, 0, 1469922850259);
        byte[] randomness = CrockfordBase32.DecodeRandomness("TSV4RRFFQ69G5FAV");
        int index = 0;
        while (index < randomness.Length)
        {
            bytes[index + 6] = randomness[index];
            index = index + 1;
        }

        string text = CrockfordBase32.EncodeUlidBytes(bytes);

        Assert.That(text, Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(CrockfordBase32.DecodeUlidBytes(text), Is.EqualTo(bytes));
        Assert.That(CrockfordBase32.DecodeUlidBytes("01arz3ndektsv4rrffq69g5fav"), Is.EqualTo(bytes));
    }

    [Test]
    public void TimestampHelpers_EncodeAndDecodeFortyEightBitTimestamp()
    {
        string text = CrockfordBase32.EncodeTimestamp(1469922850259);

        Assert.That(text, Is.EqualTo("01ARZ3NDEK"));
        Assert.That(CrockfordBase32.DecodeTimestamp(text), Is.EqualTo(1469922850259));
    }

    [Test]
    public void RandomnessHelpers_EncodeAndDecodeEightyBitRandomness()
    {
        byte[] bytes = new byte[] { 0xD6, 0x76, 0x4C, 0x61, 0xEF, 0xB9, 0x93, 0x02, 0xBD, 0x5B };

        string text = CrockfordBase32.EncodeRandomness(bytes);

        Assert.That(text, Is.EqualTo("TSV4RRFFQ69G5FAV"));
        Assert.That(CrockfordBase32.DecodeRandomness(text), Is.EqualTo(bytes));
    }

    [Test]
    public void DecodeUlidBytes_RejectsInvalidLengthCharactersAndOverflow()
    {
        Assert.That(() => CrockfordBase32.DecodeUlidBytes("01ARZ3NDEKTSV4RRFFQ69G5FA"), Throws.ArgumentException);
        Assert.That(() => CrockfordBase32.DecodeUlidBytes("01ARZ3NDEKTSV4RRFFQ69G5FAI"), Throws.ArgumentException);
        Assert.That(() => CrockfordBase32.DecodeUlidBytes("81ARZ3NDEKTSV4RRFFQ69G5FAV"), Throws.ArgumentException);
        Assert.That(() => CrockfordBase32.DecodeUlidBytes(null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void ValidateUlidText_ReturnsNormalizedValidationResult()
    {
        IdentifierValidationResult valid = CrockfordBase32.ValidateUlidText("01arz3ndektsv4rrffq69g5fav");
        IdentifierValidationResult wrongLength = CrockfordBase32.ValidateUlidText("01ARZ3NDEK");
        IdentifierValidationResult invalid = CrockfordBase32.ValidateUlidText("01ARZ3NDEKTSV4RRFFQ69G5FAI");
        IdentifierValidationResult overflow = CrockfordBase32.ValidateUlidText("81ARZ3NDEKTSV4RRFFQ69G5FAV");

        Assert.That(valid.Ok, Is.True);
        Assert.That(valid.Kind, Is.EqualTo(IdentifierKind.Ulid));
        Assert.That(valid.NormalizedText, Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(wrongLength.Ok, Is.False);
        Assert.That(wrongLength.Error, Is.EqualTo("expected 26 Crockford Base32 characters but found 10"));
        Assert.That(invalid.Ok, Is.False);
        Assert.That(invalid.Error, Is.EqualTo("invalid Crockford Base32 character at index 25"));
        Assert.That(overflow.Ok, Is.False);
        Assert.That(overflow.Error, Is.EqualTo("ULID text overflows 128 bits"));
    }

    [Test]
    public void EncodeBytesAndDecodeBytes_ValidateRepresentableShape()
    {
        byte[] oneByte = new byte[] { 255 };

        Assert.That(CrockfordBase32.EncodeBytes(oneByte, 2), Is.EqualTo("7Z"));
        Assert.That(CrockfordBase32.DecodeBytes("7Z", 1), Is.EqualTo(oneByte));
        Assert.That(() => CrockfordBase32.EncodeBytes(oneByte, 1), Throws.ArgumentException);
        Assert.That(() => CrockfordBase32.DecodeBytes("80", 1), Throws.ArgumentException);
    }
}
