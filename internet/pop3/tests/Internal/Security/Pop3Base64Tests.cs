using System.Text;

namespace pop3.Tests;

public class Pop3Base64Tests
{
    [TestCase("", "")]
    [TestCase("f", "Zg==")]
    [TestCase("fo", "Zm8=")]
    [TestCase("foo", "Zm9v")]
    [TestCase("foob", "Zm9vYg==")]
    [TestCase("fooba", "Zm9vYmE=")]
    [TestCase("foobar", "Zm9vYmFy")]
    public void EncodeAndDecodeMatchRfc4648Vectors(string plainText, string encoded)
    {
        Pop3Base64 codec = new Pop3Base64();

        Assert.That(codec.Encode(Encoding.ASCII.GetBytes(plainText)), Is.EqualTo(encoded));
        Assert.That(Encoding.ASCII.GetString(codec.Decode(encoded)), Is.EqualTo(plainText));
    }

    [Test]
    public void BinaryAlphabetCharactersAndPaddingRoundTrip()
    {
        Pop3Base64 codec = new Pop3Base64();
        byte[] value = new byte[] { 251, 255 };

        Assert.That(codec.Encode(value), Is.EqualTo("+/8="));
        Assert.That(codec.Decode("+/8="), Is.EqualTo(value));
    }

    [Test]
    public void InitialResponseUsesThePop3EmptyValueMarker()
    {
        Pop3Base64 codec = new Pop3Base64();

        Assert.That(codec.EncodeInitialResponse(Array.Empty<byte>()), Is.EqualTo("="));
        Assert.That(codec.DecodeInitialResponse("="), Is.Empty);
        Assert.That(codec.Encode(Array.Empty<byte>()), Is.Empty);
        Assert.That(codec.Decode(""), Is.Empty);
        Assert.Throws<InvalidDataException>(() => codec.DecodeInitialResponse(""));
    }

    [TestCase("*")]
    public void CancellationMarkerIsNotBase64(string marker)
    {
        Pop3Base64 codec = new Pop3Base64();

        Assert.That(codec.IsCancellationMarker(marker), Is.True);
        Assert.Throws<InvalidDataException>(() => codec.Decode(marker));
        Assert.Throws<InvalidDataException>(() => codec.DecodeInitialResponse(marker));
    }

    [TestCase("")]
    [TestCase("**")]
    [TestCase(" *")]
    [TestCase("* ")]
    [TestCase("A*")]
    public void CancellationMarkerRecognitionRequiresAnExactMatch(string value)
    {
        Assert.That(new Pop3Base64().IsCancellationMarker(value), Is.False);
    }

    [TestCase("A")]
    [TestCase("AA")]
    [TestCase("AAA")]
    [TestCase("AAAAA")]
    [TestCase("Zg=")]
    [TestCase("Zg===")]
    [TestCase("A===")]
    [TestCase("AA===")]
    [TestCase("AAAA=")]
    [TestCase("====")]
    [TestCase("=AAA")]
    [TestCase("A=AA")]
    [TestCase("AA=A")]
    [TestCase("AAA=BBB=")]
    public void DecodeRejectsInvalidLengthsAndPadding(string encoded)
    {
        Assert.Throws<InvalidDataException>(() => new Pop3Base64().Decode(encoded));
    }

    [TestCase(" Zg==")]
    [TestCase("Zg== ")]
    [TestCase("Z g==")]
    [TestCase("Zg==\r\n")]
    [TestCase("Zg==\t")]
    [TestCase("-w==")]
    [TestCase("_w==")]
    [TestCase("Zg.=")]
    public void DecodeRejectsWhitespaceAndNonAlphabetCharacters(string encoded)
    {
        Assert.Throws<InvalidDataException>(() => new Pop3Base64().Decode(encoded));
    }

    [TestCase("Zh==")]
    [TestCase("Zm9=")]
    public void DecodeRejectsNoncanonicalPadBits(string encoded)
    {
        Assert.Throws<InvalidDataException>(() => new Pop3Base64().Decode(encoded));
    }

    [TestCase("AA==")]
    [TestCase("AAA=")]
    [TestCase("////")]
    public void CanonicalBase64ValidationAcceptsValidPaddingAndAlphabet(string encoded)
    {
        Pop3Base64 codec = new Pop3Base64();

        Assert.That(codec.IsValid(encoded), Is.True);
        Assert.DoesNotThrow(() => codec.Decode(encoded));
    }

    [TestCase("AB==")]
    [TestCase("AAB=")]
    [TestCase("AA=A")]
    [TestCase("AA\u00E9=")]
    public void CanonicalBase64ValidationRejectsPadBitsPlacementAndNonAscii(string encoded)
    {
        Pop3Base64 codec = new Pop3Base64();

        Assert.That(codec.IsValid(encoded), Is.False);
        Assert.Throws<InvalidDataException>(() => codec.Decode(encoded));
    }

    [Test]
    public void CanonicalBase64ValidationRejectsNull()
    {
        Assert.That(new Pop3Base64().IsValid(null!), Is.False);
    }

    [Test]
    public void ArbitrarilyLargeSaslPayloadRoundTripsWithoutANormalLineLimit()
    {
        byte[] value = new byte[1000003];
        int index = 0;
        while (index < value.Length)
        {
            value[index] = (byte)(index % 251);
            index++;
        }

        Pop3Base64 codec = new Pop3Base64();
        string encoded = codec.Encode(value);

        Assert.That(encoded.Length, Is.GreaterThan(Pop3Defaults.MaximumResponseLineOctets));
        Assert.That(codec.Decode(encoded), Is.EqualTo(value));
    }

    [Test]
    public void NullInputsAreRejected()
    {
        Pop3Base64 codec = new Pop3Base64();

        Assert.Throws<ArgumentNullException>(() => codec.Encode(null!));
        Assert.Throws<ArgumentNullException>(() => codec.EncodeInitialResponse(null!));
        Assert.Throws<ArgumentNullException>(() => codec.Decode(null!));
        Assert.Throws<ArgumentNullException>(() => codec.DecodeInitialResponse(null!));
    }
}
