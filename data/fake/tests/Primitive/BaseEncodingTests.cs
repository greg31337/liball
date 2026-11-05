using System.Text;
using fake;

namespace fake.Tests;

[TestFixture]
public class BaseEncodingTests
{
    [TestCase("", "", "", "")]
    [TestCase("f", "66", "MY======", "Zg==")]
    [TestCase("fo", "666F", "MZXQ====", "Zm8=")]
    [TestCase("foo", "666F6F", "MZXW6===", "Zm9v")]
    [TestCase("foob", "666F6F62", "MZXW6YQ=", "Zm9vYg==")]
    [TestCase("fooba", "666F6F6261", "MZXW6YTB", "Zm9vYmE=")]
    [TestCase("foobar", "666F6F626172", "MZXW6YTBOI======", "Zm9vYmFy")]
    public void Rfc4648TestVectorsMatch(string input, string base16, string base32, string base64)
    {
        PrimitiveProvider primitive = Fake.Create(3201).Primitive();
        byte[] source = Encoding.ASCII.GetBytes(input);

        Assert.Multiple(() =>
        {
            Assert.That(primitive.Base16FromSource(source), Is.EqualTo(base16));
            Assert.That(primitive.Base32FromSource(source), Is.EqualTo(base32));
            Assert.That(primitive.Base64FromSource(source), Is.EqualTo(base64));
        });
    }

    [TestCase(new byte[] { 0x14, 0xfb, 0x9c, 0x03, 0xd9, 0x7e }, "FPucA9l+")]
    [TestCase(new byte[] { 0x14, 0xfb, 0x9c, 0x03, 0xd9 }, "FPucA9k=")]
    [TestCase(new byte[] { 0x14, 0xfb, 0x9c, 0x03 }, "FPucAw==")]
    public void Rfc4648Base64IllustrationsMatch(byte[] source, string expected)
    {
        Assert.That(Fake.Create(3202).Primitive().Base64FromSource(source), Is.EqualTo(expected));
    }

    [Test]
    public void Base16AndBase64MatchPlatformCanonicalEncoders()
    {
        PrimitiveProvider primitive = Fake.Create(3203).Primitive();
        Random random = new Random(3203);

        for (int length = 0; length <= 256; length++)
        {
            byte[] source = new byte[length];
            random.NextBytes(source);

            Assert.That(primitive.Base16FromSource(source), Is.EqualTo(Convert.ToHexString(source)));
            Assert.That(primitive.Base64FromSource(source), Is.EqualTo(Convert.ToBase64String(source)));
        }
    }

    [Test]
    public void Base32RoundTripsLongInputsAndUsesCanonicalPadding()
    {
        PrimitiveProvider primitive = Fake.Create(3204).Primitive();
        Random random = new Random(3204);

        for (int length = 0; length <= 128; length++)
        {
            byte[] source = new byte[length];
            random.NextBytes(source);
            string encoded = primitive.Base32FromSource(source);

            Assert.That(DecodeBase32(encoded), Is.EqualTo(source));
            Assert.That(encoded.Length % 8, Is.Zero);
            Assert.That(encoded, Does.Match("^[A-Z2-7]*={0,6}$"));
        }
    }

    [Test]
    public void ReadOnlySpanOverloadsEncodeOnlyTheSelectedSlice()
    {
        PrimitiveProvider primitive = Fake.Create(3205).Primitive();
        byte[] source = Encoding.ASCII.GetBytes("!foobar?");
        ReadOnlySpan<byte> slice = source.AsSpan(1, 6);

        Assert.That(primitive.Base16FromSourceUsingReadOnlySpanOfByte(slice), Is.EqualTo("666F6F626172"));
        Assert.That(primitive.Base32FromSourceUsingReadOnlySpanOfByte(slice), Is.EqualTo("MZXW6YTBOI======"));
        Assert.That(primitive.Base64FromSourceUsingReadOnlySpanOfByte(slice), Is.EqualTo("Zm9vYmFy"));
    }

    [Test]
    public void SpanEncodersReturnLengthAndLeaveDestinationSuffixUntouched()
    {
        PrimitiveProvider primitive = Fake.Create(3206).Primitive();
        byte[] source = Encoding.ASCII.GetBytes("foobar");
        char[] base16 = Enumerable.Repeat('#', 16).ToArray();
        char[] base32 = Enumerable.Repeat('#', 20).ToArray();
        char[] base64 = Enumerable.Repeat('#', 12).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(primitive.WriteBase16(source.AsSpan(), base16.AsSpan()), Is.EqualTo(12));
            Assert.That(primitive.WriteBase32(source.AsSpan(), base32.AsSpan()), Is.EqualTo(16));
            Assert.That(primitive.WriteBase64(source.AsSpan(), base64.AsSpan()), Is.EqualTo(8));
        });

        Assert.Multiple(() =>
        {
            Assert.That(new string(base16), Is.EqualTo("666F6F626172####"));
            Assert.That(new string(base32), Is.EqualTo("MZXW6YTBOI======####"));
            Assert.That(new string(base64), Is.EqualTo("Zm9vYmFy####"));
        });
    }

    [Test]
    public void TooSmallSpanDestinationsFailWithoutModification()
    {
        PrimitiveProvider primitive = Fake.Create(3207).Primitive();
        byte[] source = Encoding.ASCII.GetBytes("foobar");
        char[] base16 = Enumerable.Repeat('#', 11).ToArray();
        char[] base32 = Enumerable.Repeat('#', 15).ToArray();
        char[] base64 = Enumerable.Repeat('#', 7).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentException>(() => primitive.WriteBase16(source.AsSpan(), base16.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentException>(() => primitive.WriteBase32(source.AsSpan(), base32.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentException>(() => primitive.WriteBase64(source.AsSpan(), base64.AsSpan())).ParamName, Is.EqualTo("destination"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(base16, Has.All.EqualTo('#'));
            Assert.That(base32, Has.All.EqualTo('#'));
            Assert.That(base64, Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void RandomRfcEncodingsAreDeterministicCanonicalAndDecodable()
    {
        PrimitiveProvider first = Fake.Create(3208).Primitive();
        PrimitiveProvider second = Fake.Create(3208).Primitive();

        string base16 = first.Base16(17);
        string base32 = first.Base32(17);
        string base64 = first.Base64(17);

        Assert.Multiple(() =>
        {
            Assert.That(base16, Is.EqualTo(second.Base16(17)));
            Assert.That(base32, Is.EqualTo(second.Base32(17)));
            Assert.That(base64, Is.EqualTo(second.Base64(17)));
            Assert.That(Convert.FromHexString(base16), Has.Length.EqualTo(17));
            Assert.That(DecodeBase32(base32), Has.Length.EqualTo(17));
            Assert.That(Convert.FromBase64String(base64), Has.Length.EqualTo(17));
            Assert.That(base16, Does.Match("^[0-9A-F]{34}$"));
            Assert.That(base32, Does.Match("^[A-Z2-7]{28}={4}$"));
            Assert.That(base64, Does.Match("^[A-Za-z0-9+/]{23}=$"));
        });
    }

    [Test]
    public void EncodingCallerBytesDoesNotConsumePrimitiveRandomness()
    {
        PrimitiveProvider actual = Fake.Create(3209).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3209).Primitive();
        byte[] source = Encoding.ASCII.GetBytes("caller supplied bytes");
        char[] destination = new char[64];

        actual.Base16FromSource(source);
        actual.Base32FromSourceUsingReadOnlySpanOfByte(source.AsSpan());
        actual.Base64FromSource(source);
        actual.WriteBase16(source.AsSpan(), destination.AsSpan());
        actual.WriteBase32(source.AsSpan(), destination.AsSpan());
        actual.WriteBase64(source.AsSpan(), destination.AsSpan());

        Assert.That(actual.Base58(50), Is.EqualTo(expectedPosition.Base58(50)));
    }

    [Test]
    public void Base58GenerationUsesTheExactBitcoinAlphabet()
    {
        PrimitiveProvider first = Fake.Create(3210).Primitive();
        PrimitiveProvider second = Fake.Create(3210).Primitive();
        const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        string actual = first.Base58(10000);

        Assert.That(actual, Is.EqualTo(second.Base58(10000)));
        Assert.That(actual, Has.Length.EqualTo(10000));
        Assert.That(actual, Does.Not.Contain("0"));
        Assert.That(actual, Does.Not.Contain("O"));
        Assert.That(actual, Does.Not.Contain("I"));
        Assert.That(actual, Does.Not.Contain("l"));
        foreach (char character in alphabet)
        {
            Assert.That(actual, Does.Contain(character.ToString()));
        }
        Assert.That(actual.All(character => alphabet.Contains(character)), Is.True);
    }

    [Test]
    public void Base58SpanWriterFillsTheDestinationDeterministically()
    {
        PrimitiveProvider first = Fake.Create(3211).Primitive();
        PrimitiveProvider second = Fake.Create(3211).Primitive();
        char[] destination = new char[128];

        int written = first.WriteBase58(destination.AsSpan());

        Assert.That(written, Is.EqualTo(destination.Length));
        Assert.That(new string(destination), Is.EqualTo(second.Base58(destination.Length)));
    }

    [Test]
    public void EmptyEncodingAndGenerationOperationsAreSupportedWithoutEntropy()
    {
        PrimitiveProvider actual = Fake.Create(3212).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3212).Primitive();
        char[] untouched = new char[] { '#', '#' };

        Assert.Multiple(() =>
        {
            Assert.That(actual.Base16FromSource(Array.Empty<byte>()), Is.Empty);
            Assert.That(actual.Base32FromSource(Array.Empty<byte>()), Is.Empty);
            Assert.That(actual.Base64FromSource(Array.Empty<byte>()), Is.Empty);
            Assert.That(actual.Base16(0), Is.Empty);
            Assert.That(actual.Base32(0), Is.Empty);
            Assert.That(actual.Base64(0), Is.Empty);
            Assert.That(actual.Base58(0), Is.Empty);
            Assert.That(actual.WriteBase16(ReadOnlySpan<byte>.Empty, untouched.AsSpan()), Is.Zero);
            Assert.That(actual.WriteBase32(ReadOnlySpan<byte>.Empty, untouched.AsSpan()), Is.Zero);
            Assert.That(actual.WriteBase64(ReadOnlySpan<byte>.Empty, untouched.AsSpan()), Is.Zero);
            Assert.That(actual.WriteBase58(Span<char>.Empty), Is.Zero);
        });

        Assert.That(untouched, Is.EqualTo(new char[] { '#', '#' }));
        Assert.That(actual.Base58(50), Is.EqualTo(expectedPosition.Base58(50)));
    }

    [Test]
    public void ArrayEncodingOverloadsRejectNullSources()
    {
        PrimitiveProvider primitive = Fake.Create(3213).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => primitive.Base16FromSource((byte[])null)).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => primitive.Base32FromSource((byte[])null)).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => primitive.Base64FromSource((byte[])null)).ParamName, Is.EqualTo("source"));
        });
    }

    [Test]
    public void GeneratedEncodingsRejectNegativeByteCountsBeforeEntropy()
    {
        PrimitiveProvider actual = Fake.Create(3214).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3214).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base16(-1)).ParamName, Is.EqualTo("byteCount"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base32(-1)).ParamName, Is.EqualTo("byteCount"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base64(-1)).ParamName, Is.EqualTo("byteCount"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base58(-1)).ParamName, Is.EqualTo("count"));
        });

        Assert.That(actual.Base58(50), Is.EqualTo(expectedPosition.Base58(50)));
    }

    [Test]
    public void EncodingOperationsHonorConfiguredOutputLimitBeforeEntropy()
    {
        FakeOptions options = FakeOptions.Create(3215L);
        options.MaximumOutputSize = 7;
        PrimitiveProvider actual = Fake.Create(options).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(options).Primitive();
        byte[] fourBytes = new byte[4];
        char[] eightCharacters = new char[8];

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base16FromSource(fourBytes)).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base32FromSource(new byte[1])).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base64FromSource(fourBytes)).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base16(4)).ParamName, Is.EqualTo("byteCount"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base32(1)).ParamName, Is.EqualTo("byteCount"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base64(4)).ParamName, Is.EqualTo("byteCount"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteBase16(fourBytes.AsSpan(), eightCharacters.AsSpan())).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteBase32(new byte[1].AsSpan(), eightCharacters.AsSpan())).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteBase64(fourBytes.AsSpan(), eightCharacters.AsSpan())).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Base58(8)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteBase58(eightCharacters.AsSpan())).ParamName, Is.EqualTo("destination"));
        });

        Assert.That(actual.Base58(7), Is.EqualTo(expectedPosition.Base58(7)));
    }

    static byte[] DecodeBase32(string encoded)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        List<byte> result = new List<byte>();
        int accumulator = 0;
        int availableBits = 0;
        foreach (char character in encoded)
        {
            if (character == '=')
            {
                break;
            }
            int digit = alphabet.IndexOf(character);
            Assert.That(digit, Is.GreaterThanOrEqualTo(0));
            accumulator = (accumulator << 5) | digit;
            availableBits += 5;
            if (availableBits >= 8)
            {
                availableBits -= 8;
                result.Add((byte)((accumulator >> availableBits) & 255));
            }
        }
        return result.ToArray();
    }
}
