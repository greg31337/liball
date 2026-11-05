namespace random.Tests;

public class RandomStringsTests
{
    [Test]
    public void ByteHelpersMatchGeneratorByteMethodsExactly()
    {
        RandomGenerator allocatingGenerator = Create(101UL);
        RandomGenerator allocatingReference = Create(101UL);
        RandomGenerator fillGenerator = Create(202UL);
        RandomGenerator fillReference = Create(202UL);
        byte[] filled = new byte[259];
        byte[] expectedFill = new byte[259];

        byte[] allocated = RandomStrings.Bytes(allocatingGenerator, 259);
        byte[] expectedAllocated = allocatingReference.NextBytes(259);
        RandomStrings.FillBytes(fillGenerator, filled);
        fillReference.FillBytes(expectedFill);

        Assert.Multiple(() =>
        {
            Assert.That(allocated, Is.EqualTo(expectedAllocated));
            Assert.That(filled, Is.EqualTo(expectedFill));
            Assert.That(allocatingGenerator.ExportState().ToBytes(), Is.EqualTo(allocatingReference.ExportState().ToBytes()));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(fillReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void EncodedHelpersEncodeExactlyTheGeneratedBytes()
    {
        const int byteCount = 37;
        RandomGenerator hexGenerator = Create(303UL);
        RandomGenerator hexReference = Create(303UL);
        RandomGenerator base32Generator = Create(404UL);
        RandomGenerator base32Reference = Create(404UL);
        RandomGenerator base64Generator = Create(505UL);
        RandomGenerator base64Reference = Create(505UL);
        RandomGenerator urlGenerator = Create(606UL);
        RandomGenerator urlReference = Create(606UL);

        string hex = RandomStrings.Hex(hexGenerator, byteCount);
        string base32 = RandomStrings.Base32(base32Generator, byteCount);
        string base64 = RandomStrings.Base64(base64Generator, byteCount);
        string base64Url = RandomStrings.Base64Url(urlGenerator, byteCount);

        Assert.Multiple(() =>
        {
            Assert.That(hex, Is.EqualTo(SecureToken.EncodeHex(hexReference.NextBytes(byteCount))));
            Assert.That(base32, Is.EqualTo(SecureToken.EncodeBase32(base32Reference.NextBytes(byteCount))));
            Assert.That(base64, Is.EqualTo(SecureToken.EncodeBase64(base64Reference.NextBytes(byteCount))));
            Assert.That(base64Url, Is.EqualTo(SecureToken.EncodeBase64Url(urlReference.NextBytes(byteCount))));
            Assert.That(hexGenerator.ExportState().ToBytes(), Is.EqualTo(hexReference.ExportState().ToBytes()));
            Assert.That(base32Generator.ExportState().ToBytes(), Is.EqualTo(base32Reference.ExportState().ToBytes()));
            Assert.That(base64Generator.ExportState().ToBytes(), Is.EqualTo(base64Reference.ExportState().ToBytes()));
            Assert.That(urlGenerator.ExportState().ToBytes(), Is.EqualTo(urlReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void EncodedLengthsAndCharacterSetsMatchTheirForms()
    {
        int[] expectedBase32 = [0, 2, 4, 5, 7, 8, 10];
        int[] expectedBase64 = [0, 4, 4, 4, 8, 8, 8];
        int[] expectedBase64Url = [0, 2, 3, 4, 6, 7, 8];
        RandomGenerator generator = Create(707UL);

        for (int byteCount = 0; byteCount <= 6; byteCount++)
        {
            string hex = RandomStrings.Hex(generator, byteCount);
            string base32 = RandomStrings.Base32(generator, byteCount);
            string base64 = RandomStrings.Base64(generator, byteCount);
            string base64Url = RandomStrings.Base64Url(generator, byteCount);
            Assert.Multiple(() =>
            {
                Assert.That(hex, Has.Length.EqualTo(byteCount * 2));
                Assert.That(hex.All(character => "0123456789ABCDEF".Contains(character)), Is.True);
                Assert.That(base32, Has.Length.EqualTo(expectedBase32[byteCount]));
                Assert.That(base32.All(SecureToken.Base32Alphabet.Contains), Is.True);
                Assert.That(base64, Has.Length.EqualTo(expectedBase64[byteCount]));
                Assert.That(base64Url, Has.Length.EqualTo(expectedBase64Url[byteCount]));
                Assert.That(base64Url.All(character => char.IsAsciiLetterOrDigit(character) || character == '-' || character == '_'), Is.True);
                Assert.That(base64Url, Does.Not.Contain("="));
            });
        }
    }

    [Test]
    public void StringMatchesIndependentUnbiasedSelectionsExactly()
    {
        const string alphabet = "A7-z";
        RandomGenerator actualGenerator = Create(808UL);
        RandomGenerator referenceGenerator = Create(808UL);
        char[] expected = new char[401];
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = alphabet[(int)referenceGenerator.NextUInt32Bounded((uint)alphabet.Length)];
        }

        string actual = RandomStrings.String(actualGenerator, alphabet, expected.Length);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(new string(expected)));
            Assert.That(actual.All(alphabet.Contains), Is.True);
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void CharacterFillMatchesAllocatedStringExactly()
    {
        const string alphabet = "ABCDE";
        RandomGenerator fillGenerator = Create(909UL);
        RandomGenerator stringGenerator = Create(909UL);
        char[] destination = new char[211];

        RandomStrings.FillCharacters(fillGenerator, alphabet, destination);
        string expected = RandomStrings.String(stringGenerator, alphabet, destination.Length);

        Assert.Multiple(() =>
        {
            Assert.That(new string(destination), Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(stringGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void EmptyResultsAndSingletonAlphabetsConsumeNoState()
    {
        RandomGenerator generator = Create(1001UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        byte[] emptyBytes = RandomStrings.Bytes(generator, 0);
        RandomStrings.FillBytes(generator, Span<byte>.Empty);
        string hex = RandomStrings.Hex(generator, 0);
        string base32 = RandomStrings.Base32(generator, 0);
        string base64 = RandomStrings.Base64(generator, 0);
        string base64Url = RandomStrings.Base64Url(generator, 0);
        string emptyString = RandomStrings.String(generator, "AB", 0);
        string repeated = RandomStrings.String(generator, "x", 31);
        char[] repeatedFill = new char[31];
        RandomStrings.FillCharacters(generator, "y", repeatedFill);

        Assert.Multiple(() =>
        {
            Assert.That(emptyBytes, Is.Empty);
            Assert.That(hex, Is.Empty);
            Assert.That(base32, Is.Empty);
            Assert.That(base64, Is.Empty);
            Assert.That(base64Url, Is.Empty);
            Assert.That(emptyString, Is.Empty);
            Assert.That(repeated, Is.EqualTo(new string('x', 31)));
            Assert.That(repeatedFill, Is.All.EqualTo('y'));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidArgumentsFailBeforeStateOrDestinationMutation()
    {
        RandomGenerator generator = Create(1101UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        char[] destination = ['q', 'q', 'q'];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => RandomStrings.Bytes(NullReference.Value<global::random.IRandomSource>(), 1)), "generator" },
            new object[] { (TestDelegate)(() => RandomStrings.Bytes(generator, -1)), "length" },
            new object[] { (TestDelegate)(() => RandomStrings.Hex(generator, -1)), "byteCount" },
            new object[] { (TestDelegate)(() => RandomStrings.Base32(generator, int.MaxValue)), "byteCount" },
            new object[] { (TestDelegate)(() => RandomStrings.Base64(generator, int.MaxValue)), "byteCount" },
            new object[] { (TestDelegate)(() => RandomStrings.Base64Url(generator, int.MaxValue)), "byteCount" },
            new object[] { (TestDelegate)(() => RandomStrings.String(NullReference.Value<global::random.IRandomSource>(), "AB", 1)), "generator" },
            new object[] { (TestDelegate)(() => RandomStrings.String(generator, NullReference.Value<string>(), 1)), "alphabet" },
            new object[] { (TestDelegate)(() => RandomStrings.String(generator, "", 1)), "alphabet" },
            new object[] { (TestDelegate)(() => RandomStrings.String(generator, "AAB", 1)), "alphabet" },
            new object[] { (TestDelegate)(() => RandomStrings.String(generator, "A\uD800B", 1)), "alphabet" },
            new object[] { (TestDelegate)(() => RandomStrings.String(generator, "AB", -1)), "length" },
            new object[] { (TestDelegate)(() => RandomStrings.FillCharacters(generator, "AAB", destination)), "alphabet" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new char[] { 'q', 'q', 'q' }));
        }
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
