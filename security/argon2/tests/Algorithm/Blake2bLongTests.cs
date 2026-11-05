using System.Security.Cryptography;
using argon2;

namespace argon2.Tests.Algorithm;

public class Blake2bLongTests
{
    [Test]
    public void ShortOutputsMatchIndependentFixtures()
    {
        byte[] output4 = ComputeHash(4);
        byte[] output32 = ComputeHash(32);
        byte[] output64 = ComputeHash(64);

        Assert.Multiple(() =>
        {
            Assert.That(ToHex(output4), Is.EqualTo("ca513e0a"));
            Assert.That(
                ToHex(output32),
                Is.EqualTo("36592a3c3e0dfcff6e0efcf362b2a8e68eb0a1563c041ba4272309536ae39b47"));
            Assert.That(
                ToHex(output64),
                Is.EqualTo(
                    "2c6f5fa62d9b0549bfaae2b39e99afca0e624754e43f71bf8b2df8ead7151e36" +
                    "94fb51c7b4ec6de9b4f66426863ce4a520d7f84db5051250f5b4181f04aa4949"));
        });
    }

    [Test]
    public void LongOutputBranchBoundaryMatchesIndependentFixtures()
    {
        byte[] output65 = ComputeHash(65);
        byte[] output96 = ComputeHash(96);

        Assert.Multiple(() =>
        {
            Assert.That(
                ToHex(output65),
                Is.EqualTo(
                    "9321f69a406e6ab17f116b5bdc619b9e794806601069888795e1e36eb382839f" +
                    "6189ffa17b35028daabbc9bf1db409643b9981fb4bb1764fb33325cdb6deafbad6"));
            Assert.That(
                ToHex(output96),
                Is.EqualTo(
                    "fb57485ec7a6d6c983353a051998d1ab68014821cca42c53e4c99ebe828f880f" +
                    "155105341c88adc44ff28118362581b73c5d4a65cee3a2d7dfb0f74623a04512" +
                    "20f27107948a281513191a0aa327bfd816d6b46be21d3237b7377cfe7174b11b"));
        });
    }

    [Test]
    public void EmptyInputMatchesIndependentShortAndLongBoundaryFixtures()
    {
        byte[] output64 = new byte[64];
        byte[] output65 = new byte[65];

        Blake2bLong.ComputeHash(ReadOnlySpan<byte>.Empty, output64);
        Blake2bLong.ComputeHash(ReadOnlySpan<byte>.Empty, output65);

        Assert.Multiple(() =>
        {
            Assert.That(
                ToHex(output64),
                Is.EqualTo(
                    "7fedd5af05184f3700cb1986bf39663bc06501e6455da2b643d47bc1c01302be" +
                    "a32e4e9ec6b29f4d151c6348788b59d4e02e69e4199a886d5b36fc3e5200ab04"));
            Assert.That(
                ToHex(output65),
                Is.EqualTo(
                    "8e8b922d272035d878f074418dd8fadac41f5865ae0dc5066383237e85104c77" +
                    "6bcad5ae3396429e5048b46920bc0d7ad39ccbf77d99aeca1fe4702407b8603cb0"));
        });
    }

    [Test]
    public void NonMultipleTailMatchesIndependentFixture()
    {
        byte[] output = ComputeHash(100);

        Assert.That(
            ToHex(output),
            Is.EqualTo(
                "a6a4f85de6c6002f987ac446963978bfbee889ede3ae0294be1285c0b0fc116c" +
                "98ccb83336a570db0417d4177b247e2fc0634ffaca7db6be891e265c40b16700" +
                "527240b966318e78c479189553dedb9d31e76f1dae0f1d4e3f44a68b05466e1" +
                "bceb8737e"));
    }

    [Test]
    public void BlockSizedExpansionMatchesIndependentChecksum()
    {
        byte[] input = CreateInput();
        byte[] originalInput = (byte[])input.Clone();
        byte[] output = new byte[1024];

        Blake2bLong.ComputeHash(input, output);
        byte[] checksum = SHA256.HashData(output);

        Assert.Multiple(() =>
        {
            Assert.That(
                ToHex(checksum),
                Is.EqualTo("ed834eb64e2bfda8250cd1be358ff06c98839de98305bdeab294887c039727ba"));
            Assert.That(
                ToHex(output[..64]),
                Is.EqualTo(
                    "d5f4c304b4be42af53855c080628693f4c741bfe3378766e5dd6b78097ba90b7" +
                    "140c2f3dc0960ae214ae1820dd8b008839ba2ebce72d123662eef7bd75204b3f"));
            Assert.That(
                ToHex(output[^64..]),
                Is.EqualTo(
                    "8907c99865757af2055714c4c695cd3c8bbe1cb4028d0bcb444f3561445356eb" +
                    "d40c3ba6acc9446c5942112fd8911f41fbd98ce10f4a9415476b9336122c9127"));
            Assert.That(input, Is.EqualTo(originalInput));
        });
    }

    [Test]
    public void OutputsShorterThanArgon2MinimumAreRejected()
    {
        byte[] input = CreateInput();

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => Blake2bLong.ComputeHash(input, Array.Empty<byte>()));
            Assert.Throws<Argon2ParameterException>(() => Blake2bLong.ComputeHash(input, new byte[1]));
            Assert.Throws<Argon2ParameterException>(() => Blake2bLong.ComputeHash(input, new byte[2]));
            Assert.Throws<Argon2ParameterException>(() => Blake2bLong.ComputeHash(input, new byte[3]));
        });
    }

    static byte[] ComputeHash(int outputLength)
    {
        byte[] output = new byte[outputLength];
        Blake2bLong.ComputeHash(CreateInput(), output);
        return output;
    }

    static byte[] CreateInput()
    {
        return Enumerable.Range(0, 72).Select(value => (byte)value).ToArray();
    }

    static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }
}
