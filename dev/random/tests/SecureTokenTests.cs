using System.Text;

namespace random.Tests;

public class SecureTokenTests
{
    [Test]
    public void EncodersMatchRfc4648Examples()
    {
        string[] inputs = ["", "f", "fo", "foo", "foob", "fooba", "foobar"];
        string[] base32 = ["", "MY", "MZXQ", "MZXW6", "MZXW6YQ", "MZXW6YTB", "MZXW6YTBOI"];

        for (int index = 0; index < inputs.Length; index++)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputs[index]);
            Assert.That(SecureToken.EncodeBase32(bytes), Is.EqualTo(base32[index]));
        }

        Assert.Multiple(() =>
        {
            Assert.That(SecureToken.EncodeHex(new byte[] { 0x00, 0xAB, 0xFF }), Is.EqualTo("00ABFF"));
            Assert.That(SecureToken.EncodeBase64(Encoding.ASCII.GetBytes("foobar")), Is.EqualTo("Zm9vYmFy"));
            Assert.That(SecureToken.EncodeBase64Url(new byte[] { 0xFB, 0xFF, 0xEF }), Is.EqualTo("-__v"));
        });
    }

    [Test]
    public void TokenMethodsEncodeExactlyTheRequestedRandomBytes()
    {
        const int byteCount = 37;
        SecureRandomGenerator hex = Create();
        SecureRandomGenerator hexReference = Create();
        SecureRandomGenerator base32 = Create();
        SecureRandomGenerator base32Reference = Create();
        SecureRandomGenerator base64 = Create();
        SecureRandomGenerator base64Reference = Create();
        SecureRandomGenerator base64Url = Create();
        SecureRandomGenerator base64UrlReference = Create();

        Assert.Multiple(() =>
        {
            Assert.That(Convert.FromHexString(hex.TokenHex(byteCount)), Is.EqualTo(hexReference.NextBytes(byteCount)));
            Assert.That(DecodeBase32(base32.TokenBase32(byteCount)), Is.EqualTo(base32Reference.NextBytes(byteCount)));
            Assert.That(Convert.FromBase64String(base64.TokenBase64(byteCount)), Is.EqualTo(base64Reference.NextBytes(byteCount)));
            Assert.That(DecodeBase64Url(base64Url.TokenBase64Url(byteCount)), Is.EqualTo(base64UrlReference.NextBytes(byteCount)));
        });
    }

    [Test]
    public void EncodedLengthsAndAlphabetsMatchTheirDocumentedForms()
    {
        int[] expectedBase32 = [0, 2, 4, 5, 7, 8, 10];
        int[] expectedBase64 = [0, 4, 4, 4, 8, 8, 8];
        int[] expectedBase64Url = [0, 2, 3, 4, 6, 7, 8];
        SecureRandomGenerator generator = Create();

        for (int byteCount = 0; byteCount <= 6; byteCount++)
        {
            string hex = generator.TokenHex(byteCount);
            string base32 = generator.TokenBase32(byteCount);
            string base64 = generator.TokenBase64(byteCount);
            string base64Url = generator.TokenBase64Url(byteCount);
            Assert.Multiple(() =>
            {
                Assert.That(hex, Has.Length.EqualTo(byteCount * 2));
                Assert.That(hex.All(character => "0123456789ABCDEF".Contains(character)), Is.True);
                Assert.That(base32, Has.Length.EqualTo(expectedBase32[byteCount]));
                Assert.That(base32.All(character => SecureToken.Base32Alphabet.Contains(character)), Is.True);
                Assert.That(base64, Has.Length.EqualTo(expectedBase64[byteCount]));
                Assert.That(base64Url, Has.Length.EqualTo(expectedBase64Url[byteCount]));
                Assert.That(base64Url.All(character => char.IsAsciiLetterOrDigit(character) || character == '-' || character == '_'), Is.True);
                Assert.That(base64Url, Does.Not.Contain("="));
            });
        }
    }

    [Test]
    public void EntropyCalculationsUseSourceBytesAndDistinctAlphabetSize()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SecureRandomGenerator.TokenEntropyBits(0), Is.EqualTo(0L));
            Assert.That(SecureRandomGenerator.TokenEntropyBits(32), Is.EqualTo(256L));
            Assert.That(SecureRandomGenerator.TokenEntropyBits(int.MaxValue), Is.EqualTo(17179869176L));
            Assert.That(SecureRandomGenerator.StringEntropyBits("01", 128), Is.EqualTo(128.0));
            Assert.That(SecureRandomGenerator.StringEntropyBits("0123456789ABCDEF", 20), Is.EqualTo(80.0));
            Assert.That(SecureRandomGenerator.StringEntropyBits("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 20),
                Is.EqualTo(20.0 * Math.Log2(62.0)).Within(1e-12));
        });
    }

    [Test]
    public void CustomAlphabetStringMatchesIndependentUnbiasedSelections()
    {
        const string alphabet = "ABC";
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();
        char[] expected = new char[200];
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = alphabet[(int)reference.NextUInt32Bounded((uint)alphabet.Length)];
        }

        string result = actual.String(alphabet, expected.Length);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new string(expected)));
            Assert.That(result.All(alphabet.Contains), Is.True);
            Assert.That(result.Distinct().Order().ToArray(), Is.EqualTo(alphabet.Order().ToArray()));
        });
    }

    [Test]
    public void InvalidTokenArgumentsFailWithoutConsumingEntropy()
    {
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => actual.TokenHex(-1)), "byteCount" },
            new object[] { (TestDelegate)(() => actual.TokenBase32(-1)), "byteCount" },
            new object[] { (TestDelegate)(() => actual.TokenBase64(-1)), "byteCount" },
            new object[] { (TestDelegate)(() => actual.TokenBase64Url(-1)), "byteCount" },
            new object[] { (TestDelegate)(() => actual.TokenHex(int.MaxValue)), "byteCount" },
            new object[] { (TestDelegate)(() => actual.TokenBase32(int.MaxValue)), "byteCount" },
            new object[] { (TestDelegate)(() => actual.TokenBase64(int.MaxValue)), "byteCount" },
            new object[] { (TestDelegate)(() => actual.TokenBase64Url(int.MaxValue)), "byteCount" },
            new object[] { (TestDelegate)(() => actual.String(NullReference.Value<string>(), 1)), "alphabet" },
            new object[] { (TestDelegate)(() => actual.String("", 1)), "alphabet" },
            new object[] { (TestDelegate)(() => actual.String("A", 1)), "alphabet" },
            new object[] { (TestDelegate)(() => actual.String("AAB", 1)), "alphabet" },
            new object[] { (TestDelegate)(() => actual.String("A\uD800B", 1)), "alphabet" },
            new object[] { (TestDelegate)(() => actual.String("AB", -1)), "length" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        Assert.That(actual.NextUInt64(), Is.EqualTo(reference.NextUInt64()));
    }

    [Test]
    public void EmptyTokensConsumeNothingButStillHonorTerminalHmacState()
    {
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();

        Assert.Multiple(() =>
        {
            Assert.That(actual.TokenHex(0), Is.Empty);
            Assert.That(actual.TokenBase32(0), Is.Empty);
            Assert.That(actual.TokenBase64(0), Is.Empty);
            Assert.That(actual.TokenBase64Url(0), Is.Empty);
            Assert.That(actual.String("AB", 0), Is.Empty);
        });
        Assert.That(actual.NextUInt64(), Is.EqualTo(reference.NextUInt64()));

        actual.Uninstantiate();
        Assert.Multiple(() =>
        {
            Assert.Throws<RandomSecurityException>(() => actual.TokenHex(0));
            Assert.Throws<RandomSecurityException>(() => actual.String("AB", 0));
        });
    }

    static byte[] DecodeBase32(string value)
    {
        List<byte> result = new List<byte>();
        int bitBuffer = 0;
        int bufferedBitCount = 0;
        foreach (char character in value)
        {
            int alphabetIndex = SecureToken.Base32Alphabet.IndexOf(character);
            Assert.That(alphabetIndex, Is.GreaterThanOrEqualTo(0));
            bitBuffer = (bitBuffer << 5) | alphabetIndex;
            bufferedBitCount += 5;
            if (bufferedBitCount >= 8)
            {
                bufferedBitCount -= 8;
                result.Add((byte)(bitBuffer >> bufferedBitCount));
                bitBuffer &= (1 << bufferedBitCount) - 1;
            }
        }

        return result.ToArray();
    }

    static byte[] DecodeBase64Url(string value)
    {
        string standard = value.Replace('-', '+').Replace('_', '/');
        int paddingLength = (4 - standard.Length % 4) % 4;
        standard += new string('=', paddingLength);
        return Convert.FromBase64String(standard);
    }

    static SecureRandomGenerator Create()
    {
        return SecureRandomGenerator.CreateHmacDrbg(Sequence(0x00, 32), Sequence(0x20, 16), Sequence(0x30, 11));
    }

    static byte[] Sequence(int start, int length)
    {
        byte[] result = new byte[length];
        for (int index = 0; index < length; index++)
        {
            result[index] = (byte)(start + index);
        }

        return result;
    }
}
