using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class Base62Tests
{
    [Test]
    public void DecodeValue_UsesCaseSensitiveBase62Alphabet()
    {
        Assert.That(Base62.DecodeValue('0'), Is.EqualTo(0));
        Assert.That(Base62.DecodeValue('9'), Is.EqualTo(9));
        Assert.That(Base62.DecodeValue('A'), Is.EqualTo(10));
        Assert.That(Base62.DecodeValue('Z'), Is.EqualTo(35));
        Assert.That(Base62.DecodeValue('a'), Is.EqualTo(36));
        Assert.That(Base62.DecodeValue('z'), Is.EqualTo(61));
        Assert.That(Base62.DecodeValue('_'), Is.EqualTo(-1));
    }

    [Test]
    public void EncodeBytes_PreservesLeadingZerosForFixedWidthOutput()
    {
        Assert.That(Base62.EncodeBytes(new byte[] { 0 }, 2), Is.EqualTo("00"));
        Assert.That(Base62.EncodeBytes(new byte[] { 61 }, 2), Is.EqualTo("0z"));
        Assert.That(Base62.EncodeBytes(new byte[] { 62 }, 2), Is.EqualTo("10"));
        Assert.That(Base62.EncodeBytes(new byte[] { 255 }, 2), Is.EqualTo("47"));
    }

    [Test]
    public void DecodeBytes_DecodesBase62ValuesIntoFixedWidthBytes()
    {
        Assert.That(Base62.DecodeBytes("00", 1), Is.EqualTo(new byte[] { 0 }));
        Assert.That(Base62.DecodeBytes("0z", 1), Is.EqualTo(new byte[] { 61 }));
        Assert.That(Base62.DecodeBytes("10", 1), Is.EqualTo(new byte[] { 62 }));
        Assert.That(Base62.DecodeBytes("47", 1), Is.EqualTo(new byte[] { 255 }));
    }

    [Test]
    public void DecodeBytes_RejectsInvalidCharactersAndOverflow()
    {
        Assert.That(() => Base62.DecodeBytes("_", 1), Throws.ArgumentException);
        Assert.That(() => Base62.DecodeBytes("48", 1), Throws.ArgumentException);
        Assert.That(() => Base62.DecodeBytes(null, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Base62.DecodeBytes("0", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void EncodeBytes_RejectsOutputTooShortForValue()
    {
        Assert.That(() => Base62.EncodeBytes(new byte[] { 62 }, 1), Throws.ArgumentException);
        Assert.That(() => Base62.EncodeBytes(null, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Base62.EncodeBytes(new byte[] { 0 }, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void KsuidHelpers_EncodeAndDecodeTwentyByteValues()
    {
        byte[] bytes = new byte[]
        {
            0x15,
            0x60,
            0xE7,
            0x5F,
            0x1D,
            0xEE,
            0xAA,
            0xD7,
            0x13,
            0xF8,
            0xA5,
            0x9E,
            0x5C,
            0xA5,
            0x7C,
            0x2B,
            0x47,
            0x72,
            0x6F,
            0xC7
        };

        string text = Base62.EncodeKsuidBytes(bytes);

        Assert.That(text, Has.Length.EqualTo(27));
        Assert.That(Base62.DecodeKsuidBytes(text), Is.EqualTo(bytes));
    }

    [Test]
    public void KsuidHelpers_UseTwentyByteTwentySevenCharacterCanonicalShape()
    {
        Assert.That(Base62.EncodeKsuidBytes(new byte[20]), Is.EqualTo("000000000000000000000000000"));
        Assert.That(Base62.DecodeKsuidBytes("000000000000000000000000000"), Is.EqualTo(new byte[20]));
        Assert.That(() => Base62.EncodeKsuidBytes(new byte[19]), Throws.ArgumentException);
        Assert.That(() => Base62.DecodeKsuidBytes("00000000000000000000000000"), Throws.ArgumentException);
    }

    [Test]
    public void ValidateKsuidText_ReturnsValidationResult()
    {
        string zero = "000000000000000000000000000";
        IdentifierValidationResult valid = Base62.ValidateKsuidText(zero);
        IdentifierValidationResult wrongLength = Base62.ValidateKsuidText("000");
        IdentifierValidationResult invalid = Base62.ValidateKsuidText("00000000000000000000000000_");
        IdentifierValidationResult overflow = Base62.ValidateKsuidText("zzzzzzzzzzzzzzzzzzzzzzzzzzz");

        Assert.That(valid.Ok, Is.True);
        Assert.That(valid.Kind, Is.EqualTo(IdentifierKind.Ksuid));
        Assert.That(valid.NormalizedText, Is.EqualTo(zero));
        Assert.That(wrongLength.Ok, Is.False);
        Assert.That(wrongLength.Error, Is.EqualTo("expected 27 Base62 characters but found 3"));
        Assert.That(invalid.Ok, Is.False);
        Assert.That(invalid.Error, Is.EqualTo("invalid Base62 character at index 26"));
        Assert.That(overflow.Ok, Is.False);
        Assert.That(overflow.Error, Is.EqualTo("KSUID text overflows 160 bits"));
    }
}
