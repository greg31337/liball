namespace uri.Tests;

public class AsciiTests
{
    [Test]
    public void CoreClassificationMatchesEveryAsciiBoundary()
    {
        for (int code = 0; code <= 255; code++)
        {
            char value = (char)code;
            bool alphabetic = value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
            bool digit = value >= '0' && value <= '9';
            bool hexadecimal = digit || value >= 'A' && value <= 'F' || value >= 'a' && value <= 'f';

            Assert.That(Ascii.IsAscii(value), Is.EqualTo(code <= 127), $"ASCII U+{code:X4}");
            Assert.That(Ascii.IsAlphabetic(value), Is.EqualTo(alphabetic), $"alphabetic U+{code:X4}");
            Assert.That(Ascii.IsUppercaseAlphabetic(value), Is.EqualTo(value >= 'A' && value <= 'Z'));
            Assert.That(Ascii.IsLowercaseAlphabetic(value), Is.EqualTo(value >= 'a' && value <= 'z'));
            Assert.That(Ascii.IsDigit(value), Is.EqualTo(digit), $"digit U+{code:X4}");
            Assert.That(Ascii.IsAlphaNumeric(value), Is.EqualTo(alphabetic || digit));
            Assert.That(Ascii.IsHexDigit(value), Is.EqualTo(hexadecimal), $"hexadecimal U+{code:X4}");
        }
    }

    [Test]
    public void UriClassificationMatchesPublishedCharacterSets()
    {
        for (int code = 0; code <= 127; code++)
        {
            char value = (char)code;

            Assert.That(
                Ascii.IsUnreserved(value),
                Is.EqualTo(UriCharacterSets.Unreserved.Contains(value)),
                $"unreserved U+{code:X4}");
            Assert.That(
                Ascii.IsGeneralDelimiter(value),
                Is.EqualTo(UriCharacterSets.GeneralDelimiters.Contains(value)),
                $"general delimiter U+{code:X4}");
            Assert.That(
                Ascii.IsSubDelimiter(value),
                Is.EqualTo(UriCharacterSets.SubDelimiters.Contains(value)),
                $"sub-delimiter U+{code:X4}");
            Assert.That(
                Ascii.IsReserved(value),
                Is.EqualTo(UriCharacterSets.Reserved.Contains(value)),
                $"reserved U+{code:X4}");
        }
    }

    [Test]
    public void ControlAndVisibleClassificationUseAsciiDefinitions()
    {
        for (int code = 0; code <= 255; code++)
        {
            char value = (char)code;

            Assert.That(Ascii.IsControl(value), Is.EqualTo(code <= 31 || code == 127));
            Assert.That(Ascii.IsVisible(value), Is.EqualTo(code >= 33 && code <= 126));
        }
    }

    [TestCase('0', 0)]
    [TestCase('9', 9)]
    [TestCase('A', 10)]
    [TestCase('F', 15)]
    [TestCase('a', 10)]
    [TestCase('f', 15)]
    [TestCase('/', -1)]
    [TestCase('G', -1)]
    [TestCase('g', -1)]
    [TestCase('\u0080', -1)]
    public void HexValueConversionHandlesBothCasesAndInvalidInput(char value, int expected)
    {
        Assert.That(Ascii.GetHexValue(value), Is.EqualTo(expected));
    }

    [TestCase('0', '0', 0)]
    [TestCase('0', 'f', 15)]
    [TestCase('A', '0', 160)]
    [TestCase('f', 'F', 255)]
    [TestCase('G', '0', -1)]
    [TestCase('0', 'x', -1)]
    public void HexByteConversionRequiresTwoHexadecimalDigits(char high, char low, int expected)
    {
        Assert.That(Ascii.GetHexByte(high, low), Is.EqualTo(expected));
    }

    [Test]
    public void UpperHexConversionProducesCanonicalDigits()
    {
        string expected = "0123456789ABCDEF";

        for (int value = 0; value < expected.Length; value++)
        {
            Assert.That(Ascii.GetUpperHexCharacter(value), Is.EqualTo(expected[value]));
        }
    }

    [Test]
    public void UpperHexConversionRejectsValuesOutsideOneNibble()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Ascii.GetUpperHexCharacter(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Ascii.GetUpperHexCharacter(16));
        });
    }

    [Test]
    public void EveryByteRoundTripsThroughCanonicalHexCharacters()
    {
        for (int value = 0; value <= 255; value++)
        {
            char high = Ascii.GetUpperHexCharacter(value >> 4);
            char low = Ascii.GetUpperHexCharacter(value & 15);

            Assert.That(Ascii.GetHexByte(high, low), Is.EqualTo(value), $"byte {value}");
        }
    }
}
