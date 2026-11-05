using System.Text;
using email;

namespace email.Tests;

public class EmailByteScannerTests
{
    [Test]
    public void BasicAsciiClassifiersCoverEveryByteValue()
    {
        int value = 0;
        while (value <= 255)
        {
            byte current = (byte)value;
            Assert.That(new EmailByteScanner().IsAscii(current), Is.EqualTo(value <= 127), "ASCII " + value);
            Assert.That(new EmailByteScanner().IsAsciiLetter(current), Is.EqualTo(value >= 65 && value <= 90 || value >= 97 && value <= 122), "letter " + value);
            Assert.That(new EmailByteScanner().IsAsciiDigit(current), Is.EqualTo(value >= 48 && value <= 57), "digit " + value);
            Assert.That(new EmailByteScanner().IsAsciiLetterOrDigit(current), Is.EqualTo(value >= 48 && value <= 57 || value >= 65 && value <= 90 || value >= 97 && value <= 122), "letter or digit " + value);
            Assert.That(new EmailByteScanner().IsWsp(current), Is.EqualTo(value == 9 || value == 32), "WSP " + value);
            Assert.That(new EmailByteScanner().IsCr(current), Is.EqualTo(value == 13), "CR " + value);
            Assert.That(new EmailByteScanner().IsLf(current), Is.EqualTo(value == 10), "LF " + value);
            Assert.That(new EmailByteScanner().IsControl(current), Is.EqualTo(value <= 31 || value == 127), "control " + value);
            Assert.That(new EmailByteScanner().IsVisibleAscii(current), Is.EqualTo(value >= 33 && value <= 126), "visible " + value);
            Assert.That(new EmailByteScanner().IsFieldNameCharacter(current), Is.EqualTo(value >= 33 && value <= 126 && value != 58), "field name " + value);
            value++;
        }
    }

    [Test]
    public void RfcAtextClassifierMatchesAllowedPunctuation()
    {
        string allowedPunctuation = "!#$%&'*+-/=?^_`{|}~";
        int value = 0;
        while (value <= 255)
        {
            bool expected = value >= 48 && value <= 57 || value >= 65 && value <= 90 || value >= 97 && value <= 122;
            if (value <= 127 && allowedPunctuation.Contains((char)value))
            {
                expected = true;
            }

            Assert.That(new EmailByteScanner().IsAtext((byte)value), Is.EqualTo(expected), "atext " + value);
            value++;
        }

        Assert.That(new EmailByteScanner().IsAtext((byte)'.'), Is.False);
        Assert.That(new EmailByteScanner().IsAtext((byte)'@'), Is.False);
    }

    [Test]
    public void MimeTokenClassifierExcludesControlsSpaceAndTspecials()
    {
        string special = "()<>@,;:\\\"/[]?=";
        int value = 0;
        while (value <= 255)
        {
            bool expected = value >= 33 && value <= 126 && !special.Contains((char)value);
            Assert.That(new EmailByteScanner().IsMimeTokenCharacter((byte)value), Is.EqualTo(expected), "MIME token " + value);
            value++;
        }
    }

    [Test]
    public void AsciiCaseConversionChangesLettersOnly()
    {
        int value = 0;
        while (value <= 255)
        {
            byte expectedLower = value >= 65 && value <= 90 ? (byte)(value + 32) : (byte)value;
            byte expectedUpper = value >= 97 && value <= 122 ? (byte)(value - 32) : (byte)value;
            Assert.That(new EmailByteScanner().ToLowerAscii((byte)value), Is.EqualTo(expectedLower), "lower " + value);
            Assert.That(new EmailByteScanner().ToUpperAscii((byte)value), Is.EqualTo(expectedUpper), "upper " + value);
            value++;
        }
    }

    [Test]
    public void AsciiComparisonFoldsLettersButNotNonAsciiBytes()
    {
        byte[] mixed = Encoding.ASCII.GetBytes("Content-Type");
        byte[] lower = Encoding.ASCII.GetBytes("content-type");
        byte[] greater = Encoding.ASCII.GetBytes("content-types");

        Assert.That(new EmailByteScanner().EqualsAsciiIgnoreCase(mixed, lower), Is.True);
        Assert.That(new EmailByteScanner().EqualsAsciiIgnoreCase(mixed, greater), Is.False);
        Assert.That(new EmailByteScanner().CompareAsciiIgnoreCase(mixed, lower), Is.Zero);
        Assert.That(new EmailByteScanner().CompareAsciiIgnoreCase(mixed, greater), Is.LessThan(0));
        Assert.That(new EmailByteScanner().CompareAsciiIgnoreCase(greater, mixed), Is.GreaterThan(0));
        Assert.That(new EmailByteScanner().CompareAsciiIgnoreCase(new byte[] { 66 }, new byte[] { 97 }), Is.GreaterThan(0));
        Assert.That(new EmailByteScanner().EqualsAsciiIgnoreCase(new byte[] { 192 }, new byte[] { 224 }), Is.False);
        Assert.That(new EmailByteScanner().StartsWithAsciiIgnoreCase(mixed, Encoding.ASCII.GetBytes("CONTENT")), Is.True);
        Assert.That(new EmailByteScanner().StartsWithAsciiIgnoreCase(mixed, greater), Is.False);
        Assert.That(new EmailByteScanner().StartsWithAsciiIgnoreCase(mixed, Encoding.ASCII.GetBytes("contest")), Is.False);
    }

    [Test]
    public void HexHelpersRoundTripEveryNibbleAndRejectInvalidValues()
    {
        int nibble = 0;
        while (nibble < 16)
        {
            byte upper = new EmailByteScanner().ToUpperHex(nibble);
            byte lower = new EmailByteScanner().ToLowerHex(nibble);
            Assert.That(new EmailByteScanner().IsHexDigit(upper), Is.True);
            Assert.That(new EmailByteScanner().IsHexDigit(lower), Is.True);
            Assert.That(new EmailByteScanner().GetHexValue(upper), Is.EqualTo(nibble));
            Assert.That(new EmailByteScanner().GetHexValue(lower), Is.EqualTo(nibble));
            nibble++;
        }

        Assert.That(new EmailByteScanner().DecodeHexPair((byte)'0', (byte)'0'), Is.Zero);
        Assert.That(new EmailByteScanner().DecodeHexPair((byte)'A', (byte)'f'), Is.EqualTo(175));
        Assert.That(new EmailByteScanner().DecodeHexPair((byte)'G', (byte)'0'), Is.EqualTo(-1));
        Assert.That(new EmailByteScanner().DecodeHexPair((byte)'0', (byte)'x'), Is.EqualTo(-1));
        Assert.That(new EmailByteScanner().GetHexValue((byte)'/'), Is.EqualTo(-1));
        Assert.That(new EmailByteScanner().GetHexValue((byte)'g'), Is.EqualTo(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailByteScanner().ToUpperHex(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailByteScanner().ToLowerHex(16));
    }

    [Test]
    public void FindByteHonorsStartAndMissingResults()
    {
        byte[] value = Encoding.ASCII.GetBytes("a:b:c");

        Assert.That(new EmailByteScanner().FindByte(value, (byte)':', 0), Is.EqualTo(1));
        Assert.That(new EmailByteScanner().FindByte(value, (byte)':', 2), Is.EqualTo(3));
        Assert.That(new EmailByteScanner().FindByte(value, (byte)'x', 0), Is.EqualTo(-1));
        Assert.That(new EmailByteScanner().FindByte(value, (byte)':', value.Length), Is.EqualTo(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailByteScanner().FindByte(value, (byte)':', -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailByteScanner().FindByte(value, (byte)':', value.Length + 1));
    }

    [Test]
    public void FindSequenceHandlesEmptyOverlappingAndCaseInsensitiveDelimiters()
    {
        byte[] value = Encoding.ASCII.GetBytes("aaab--Boundary--tail");

        Assert.That(new EmailByteScanner().FindSequence(value, Encoding.ASCII.GetBytes("aab"), 0), Is.EqualTo(1));
        Assert.That(new EmailByteScanner().FindSequence(value, Encoding.ASCII.GetBytes("--Boundary"), 0), Is.EqualTo(4));
        Assert.That(new EmailByteScanner().FindSequence(value, Encoding.ASCII.GetBytes("--boundary"), 0), Is.EqualTo(-1));
        Assert.That(new EmailByteScanner().FindSequenceAsciiIgnoreCase(value, Encoding.ASCII.GetBytes("--boundary"), 0), Is.EqualTo(4));
        Assert.That(new EmailByteScanner().FindSequence(value, Array.Empty<byte>(), 3), Is.EqualTo(3));
        Assert.That(new EmailByteScanner().FindSequence(value, Encoding.ASCII.GetBytes("tail"), value.Length), Is.EqualTo(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailByteScanner().FindSequence(value, Array.Empty<byte>(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailByteScanner().FindSequenceAsciiIgnoreCase(value, Array.Empty<byte>(), value.Length + 1));
    }

    [Test]
    public void FindAnyReturnsFirstMatchingPosition()
    {
        byte[] value = Encoding.ASCII.GetBytes("name=value;next");
        byte[] delimiters = new byte[] { (byte)';', (byte)'=' };

        Assert.That(new EmailByteScanner().FindAny(value, delimiters, 0), Is.EqualTo(4));
        Assert.That(new EmailByteScanner().FindAny(value, delimiters, 5), Is.EqualTo(10));
        Assert.That(new EmailByteScanner().FindAny(value, Array.Empty<byte>(), 0), Is.EqualTo(-1));
        Assert.That(new EmailByteScanner().FindAny(value, delimiters, value.Length), Is.EqualTo(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailByteScanner().FindAny(value, delimiters, -1));
    }
}
