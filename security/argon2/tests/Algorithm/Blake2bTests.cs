using System.Text;
using argon2;

namespace argon2.Tests.Algorithm;

public class Blake2bTests
{
    [Test]
    public void Rfc7693AppendixESelfTestPasses()
    {
        int[] digestLengths = new int[] { 20, 32, 48, 64 };
        int[] inputLengths = new int[] { 0, 3, 128, 129, 255, 1024 };
        Blake2b aggregate = new Blake2b(32, ReadOnlySpan<byte>.Empty);

        foreach (int digestLength in digestLengths)
        {
            foreach (int inputLength in inputLengths)
            {
                byte[] input = CreateRfcSelfTestSequence(inputLength, (uint)inputLength);
                byte[] digest = new byte[digestLength];
                Blake2b.ComputeHash(input, digest);
                aggregate.Update(digest);

                byte[] key = CreateRfcSelfTestSequence(digestLength, (uint)digestLength);
                byte[] keyedDigest = new byte[digestLength];
                Blake2b.ComputeKeyedHash(input, key, keyedDigest);
                aggregate.Update(keyedDigest);
            }
        }

        byte[] aggregateDigest = new byte[32];
        aggregate.Finish(aggregateDigest);

        Assert.That(
            ToHex(aggregateDigest),
            Is.EqualTo("c23a7800d98123bd10f506c61e29da5603d763b8bbad2e737f5e765a7bccd475"));
    }

    [TestCase(1, "31")]
    [TestCase(4, "0f2e5c3b")]
    [TestCase(20, "2433af65183f411941345962733a8860df650139")]
    [TestCase(32, "39a7eb9fedc19aabc83425c6755dd90e6f9d0c804964a1f4aaeea3b9fb599835")]
    [TestCase(48, "9bd2b1bf7a89613fdcc76a3e02dabe81772a97bd5e6274fd9fe72e219bffe88c5e6f681a31481485dcb85dfa34bdc657")]
    [TestCase(63, "0464414f0a2e7456ff77883630b8c937b9ff3b90f37972b83866f86aea2a86351dd43979a61113943fc1eb5cf2171b3ae8f9f2b9f0d5fe7280870f7ff9684f")]
    [TestCase(64, "1ecc896f34d3f9cac484c73f75f6a5fb58ee6784be41b35f46067b9c65c63a6794d3d744112c653f73dd7deb6666204c5a9bfa5b46081fc10fdbe7884fa5cbf8")]
    public void SupportedDigestBoundariesMatchIndependentFixtures(
        int digestLength,
        string expectedHex)
    {
        byte[] input = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        byte[] digest = new byte[digestLength];

        Blake2b.ComputeHash(input, digest);

        Assert.That(ToHex(digest), Is.EqualTo(expectedHex));
    }

    [Test]
    public void EmptyInputMatchesKnownDigestLengths()
    {
        byte[] digest1 = new byte[1];
        byte[] digest32 = new byte[32];
        byte[] digest64 = new byte[64];

        Blake2b.ComputeHash(Array.Empty<byte>(), digest1);
        Blake2b.ComputeHash(Array.Empty<byte>(), digest32);
        Blake2b.ComputeHash(Array.Empty<byte>(), digest64);

        Assert.Multiple(() =>
        {
            Assert.That(ToHex(digest1), Is.EqualTo("2e"));
            Assert.That(
                ToHex(digest32),
                Is.EqualTo("0e5751c026e543b2e8ab2eb06099daa1d1e5df47778f7787faab45cdf12fe3a8"));
            Assert.That(
                ToHex(digest64),
                Is.EqualTo(
                    "786a02f742015903c6c6fd852552d272912f4740e15847618a86e217f71f5419" +
                    "d25e1031afee585313896444934eb04b903a685b1448b755d56f701afe9be2ce"));
        });
    }

    [Test]
    public void AbcMatchesRfc7693Digest()
    {
        byte[] digest = new byte[64];

        Blake2b.ComputeHash(Encoding.ASCII.GetBytes("abc"), digest);

        Assert.That(
            ToHex(digest),
            Is.EqualTo(
                "ba80a53f981c4d0d6a2797b69f12f6e94c212f14685ac4b74b12bb6fdbffa2d" +
                "17d87c5392aab792dc252d5de4533cc9518d38aa8dbf1925ab92386edd4009923"));
    }

    [Test]
    public void ExactAndMultipleBlocksMatchIndependentFixtures()
    {
        byte[] exactBlockInput = Enumerable.Range(0, 128).Select(value => (byte)value).ToArray();
        byte[] multipleBlockInput = Enumerable.Range(0, 129).Select(value => (byte)value).ToArray();
        byte[] exactBlockDigest = new byte[64];
        byte[] multipleBlockDigest = new byte[64];

        Blake2b.ComputeHash(exactBlockInput, exactBlockDigest);
        Blake2b.ComputeHash(multipleBlockInput, multipleBlockDigest);

        Assert.Multiple(() =>
        {
            Assert.That(
                ToHex(exactBlockDigest),
                Is.EqualTo(
                    "2319e3789c47e2daa5fe807f61bec2a1a6537fa03f19ff32e87eecbfd64b7e0" +
                    "e8ccff439ac333b040f19b0c4ddd11a61e24ac1fe0f10a039806c5dcc0da3d115"));
            Assert.That(
                ToHex(multipleBlockDigest),
                Is.EqualTo(
                    "f59711d44a031d5f97a9413c065d1e614c417ede998590325f49bad2fd444d3e" +
                    "4418be19aec4e11449ac1a57207898bc57d76a1bcf3566292c20c683a5c4648f"));
        });
    }

    [Test]
    public void IncrementalAbsorptionMatchesRfc7693Digest()
    {
        Blake2b hash = new Blake2b(64, ReadOnlySpan<byte>.Empty);
        byte[] digest = new byte[64];

        hash.Update(Encoding.ASCII.GetBytes("a"));
        hash.Update(ReadOnlySpan<byte>.Empty);
        hash.Update(Encoding.ASCII.GetBytes("bc"));
        hash.Finish(digest);

        Assert.That(
            ToHex(digest),
            Is.EqualTo(
                "ba80a53f981c4d0d6a2797b69f12f6e94c212f14685ac4b74b12bb6fdbffa2d" +
                "17d87c5392aab792dc252d5de4533cc9518d38aa8dbf1925ab92386edd4009923"));
    }

    [Test]
    public void KeyedHashingMatchesIndependentFixtures()
    {
        byte[] key = Enumerable.Range(0, 64).Select(value => (byte)value).ToArray();
        byte[] input = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        byte[] emptyDigest = new byte[32];
        byte[] multipleBlockDigest = new byte[64];

        Blake2b.ComputeKeyedHash(Array.Empty<byte>(), key, emptyDigest);
        Blake2b.ComputeKeyedHash(input, key, multipleBlockDigest);

        Assert.Multiple(() =>
        {
            Assert.That(
                ToHex(emptyDigest),
                Is.EqualTo("2fa9fbd9be36437de204e139e97d402bce68c828f43391608c891b5faed8a98a"));
            Assert.That(
                ToHex(multipleBlockDigest),
                Is.EqualTo(
                    "b72071e096277edebb8ee5134dd3714996307ba3a55aa4733d412abbe28e909e" +
                    "10e57e6fbfb4ef53b3b960518294ff889a90829254412e2a60b85add07a3674f"));
        });
    }

    [Test]
    public void InvalidDigestAndKeyLengthsAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => Blake2b.ComputeHash(Array.Empty<byte>(), Array.Empty<byte>()));
            Assert.Throws<Argon2ParameterException>(() => Blake2b.ComputeHash(Array.Empty<byte>(), new byte[65]));
            Assert.Throws<Argon2ParameterException>(
                () => Blake2b.ComputeKeyedHash(Array.Empty<byte>(), new byte[65], new byte[32]));
        });
    }

    static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }

    static byte[] CreateRfcSelfTestSequence(int length, uint seed)
    {
        byte[] output = new byte[length];
        uint first = 0xDEAD4BADU * seed;
        uint second = 1U;
        int index = 0;
        while (index < output.Length)
        {
            uint next = first + second;
            first = second;
            second = next;
            output[index] = (byte)(next >> 24);
            index = index + 1;
        }

        return output;
    }
}
