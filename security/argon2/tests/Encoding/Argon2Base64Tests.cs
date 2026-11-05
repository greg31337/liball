using argon2;

namespace argon2.Tests.Codecs;

public class Argon2Base64Tests
{
    [TestCase("", "")]
    [TestCase("00", "AA")]
    [TestCase("ff", "/w")]
    [TestCase("0001", "AAE")]
    [TestCase("ffee", "/+4")]
    [TestCase("000102", "AAEC")]
    [TestCase("666f6f626172", "Zm9vYmFy")]
    public void EncodingMatchesCanonicalUnpaddedFixtures(string sourceHex, string expected)
    {
        byte[] source = Convert.FromHexString(sourceHex);
        char[] destination = new char[Argon2Base64.GetEncodedLength(source.Length)];

        Argon2Base64.Encode(source, destination);

        Assert.That(new string(destination), Is.EqualTo(expected));
    }

    [Test]
    public void EverySmallLengthRoundTripsWithoutPadding()
    {
        int length = 0;
        while (length <= 64)
        {
            byte[] source = new byte[length];
            int index = 0;
            while (index < source.Length)
            {
                source[index] = (byte)(index * 37 + length);
                index = index + 1;
            }

            char[] encoded = new char[Argon2Base64.GetEncodedLength(source.Length)];
            Argon2Base64.Encode(source, encoded);
            int decodedLength = Argon2Base64.GetDecodedLength(encoded.Length);
            byte[] decoded = new byte[decodedLength];
            Argon2Base64.Decode(encoded, decoded);

            Assert.Multiple(() =>
            {
                Assert.That(encoded, Has.None.EqualTo('='));
                Assert.That(decodedLength, Is.EqualTo(source.Length));
                Assert.That(decoded, Is.EqualTo(source));
            });
            length = length + 1;
        }
    }

    [TestCase("A")]
    [TestCase("AAAAA")]
    [TestCase("=A")]
    [TestCase("A=")]
    [TestCase("AA=")]
    [TestCase("AA==")]
    [TestCase("A A")]
    [TestCase("\tAA")]
    [TestCase("AA\r")]
    [TestCase("AA\n")]
    [TestCase("AA\0")]
    [TestCase("AA\u00A0")]
    [TestCase("AA-")]
    [TestCase("AA_")]
    [TestCase("AA*")]
    [TestCase("AAé")]
    [TestCase(".A")]
    public void InvalidLengthsAlphabetPaddingAndWhitespaceAreRejected(string encoded)
    {
        int decodedLength = Argon2Base64.GetDecodedLengthWithoutException(encoded.Length);
        byte[] destination = decodedLength < 0
            ? Array.Empty<byte>()
            : new byte[decodedLength];

        bool result = Argon2Base64.DecodeWithoutException(encoded, destination);

        Assert.That(result, Is.False);
    }

    [Test]
    public void EveryNonAlphabetAsciiCharacterIsRejectedInEveryPosition()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        int characterValue = 0;
        while (characterValue <= 127)
        {
            char character = (char)characterValue;
            if (!alphabet.Contains(character))
            {
                int position = 0;
                while (position < 4)
                {
                    char[] encoded = new char[] { 'A', 'A', 'A', 'A' };
                    encoded[position] = character;
                    byte[] destination = new byte[] { 0xA5, 0xA5, 0xA5 };
                    byte[] original = (byte[])destination.Clone();

                    bool result = Argon2Base64.DecodeWithoutException(encoded, destination);

                    Assert.Multiple(() =>
                    {
                        Assert.That(result, Is.False);
                        Assert.That(Argon2Base64.IsCanonical(encoded), Is.False);
                        Assert.That(destination, Is.EqualTo(original));
                    });
                    position = position + 1;
                }
            }

            characterValue = characterValue + 1;
        }
    }

    [Test]
    public void EveryPossibleTrailingSextetEnforcesCanonicalUnusedBits()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        int value = 0;
        while (value < alphabet.Length)
        {
            string oneByteEncoding = "A" + alphabet[value];
            string twoByteEncoding = "AA" + alphabet[value];
            bool oneByteExpected = (value & 0x0F) == 0;
            bool twoByteExpected = (value & 0x03) == 0;
            byte[] oneByteDestination = new byte[] { 0xA5 };
            byte[] twoByteDestination = new byte[] { 0xA5, 0xA5 };
            byte[] originalOneByteDestination = (byte[])oneByteDestination.Clone();
            byte[] originalTwoByteDestination = (byte[])twoByteDestination.Clone();

            bool oneByteResult = Argon2Base64.DecodeWithoutException(
                oneByteEncoding,
                oneByteDestination);
            bool twoByteResult = Argon2Base64.DecodeWithoutException(
                twoByteEncoding,
                twoByteDestination);

            Assert.Multiple(() =>
            {
                Assert.That(
                    Argon2Base64.IsCanonical(oneByteEncoding),
                    Is.EqualTo(oneByteExpected),
                    "one-byte final sextet " + value);
                Assert.That(
                    oneByteResult,
                    Is.EqualTo(oneByteExpected),
                    "one-byte decode final sextet " + value);
                Assert.That(
                    Argon2Base64.IsCanonical(twoByteEncoding),
                    Is.EqualTo(twoByteExpected),
                    "two-byte final sextet " + value);
                Assert.That(
                    twoByteResult,
                    Is.EqualTo(twoByteExpected),
                    "two-byte decode final sextet " + value);
                if (!oneByteExpected)
                {
                    Assert.That(oneByteDestination, Is.EqualTo(originalOneByteDestination));
                }

                if (!twoByteExpected)
                {
                    Assert.That(twoByteDestination, Is.EqualTo(originalTwoByteDestination));
                }
            });
            value = value + 1;
        }
    }

    [TestCase("AB")]
    [TestCase("AAB")]
    [TestCase("//")]
    [TestCase("///")]
    public void NonzeroUnusedTrailingBitsAreRejected(string encoded)
    {
        int decodedLength = Argon2Base64.GetDecodedLength(encoded.Length);
        byte[] destination = new byte[decodedLength];
        Array.Fill(destination, (byte)0xA5);
        byte[] original = (byte[])destination.Clone();

        bool result = Argon2Base64.DecodeWithoutException(encoded, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(destination, Is.EqualTo(original));
            Assert.Throws<Argon2FormatException>(() => Argon2Base64.Decode(encoded, destination));
        });
    }

    [TestCase("")]
    [TestCase("00")]
    [TestCase("0001")]
    [TestCase("000102")]
    [TestCase("00010203")]
    public void ExactDestinationBoundsAreEnforcedForEveryRemainder(string sourceHex)
    {
        byte[] source = Convert.FromHexString(sourceHex);
        char[] validEncodingDestination = new char[Argon2Base64.GetEncodedLength(source.Length)];
        Argon2Base64.Encode(source, validEncodingDestination);
        byte[] validDecodingDestination = new byte[source.Length];
        Argon2Base64.Decode(validEncodingDestination, validDecodingDestination);

        Assert.Multiple(() =>
        {
            Assert.That(validDecodingDestination, Is.EqualTo(source));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Base64.Encode(source, new char[validEncodingDestination.Length + 1]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Base64.Decode(validEncodingDestination, new byte[source.Length + 1]));
            Assert.That(
                Argon2Base64.DecodeWithoutException(
                    validEncodingDestination,
                    new byte[source.Length + 1]),
                Is.False);
            if (validEncodingDestination.Length > 0)
            {
                Assert.Throws<Argon2ParameterException>(
                    () => Argon2Base64.Encode(
                        source,
                        new char[validEncodingDestination.Length - 1]));
            }

            if (source.Length > 0)
            {
                Assert.Throws<Argon2ParameterException>(
                    () => Argon2Base64.Decode(
                        validEncodingDestination,
                        new byte[source.Length - 1]));
                Assert.That(
                    Argon2Base64.DecodeWithoutException(
                        validEncodingDestination,
                        new byte[source.Length - 1]),
                    Is.False);
            }
        });
    }

    [Test]
    public void LengthCalculationsRejectInvalidAndUnaddressableValues()
    {
        int maximumByteLength = Array.MaxLength / 4 * 3
            + (Array.MaxLength % 4 == 0 ? 0 : Array.MaxLength % 4 - 1);

        Assert.Multiple(() =>
        {
            Assert.That(Argon2Base64.GetEncodedLength(0), Is.Zero);
            Assert.That(Argon2Base64.GetEncodedLength(1), Is.EqualTo(2));
            Assert.That(Argon2Base64.GetEncodedLength(2), Is.EqualTo(3));
            Assert.That(Argon2Base64.GetEncodedLength(3), Is.EqualTo(4));
            Assert.That(Argon2Base64.GetEncodedLength(maximumByteLength), Is.EqualTo(Array.MaxLength));
            Assert.That(Argon2Base64.GetDecodedLength(Array.MaxLength), Is.EqualTo(maximumByteLength));
            Assert.That(
                Argon2Base64.GetDecodedLengthWithoutException(Array.MaxLength),
                Is.EqualTo(maximumByteLength));
            Assert.That(Argon2Base64.GetDecodedLengthWithoutException(1), Is.EqualTo(-1));
            Assert.Throws<Argon2ParameterException>(() => Argon2Base64.GetEncodedLength(-1));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Base64.GetEncodedLength(maximumByteLength + 1));
            Assert.Throws<Argon2ParameterException>(() => Argon2Base64.GetDecodedLength(-1));
            Assert.Throws<Argon2FormatException>(() => Argon2Base64.GetDecodedLength(5));
        });
    }
}
