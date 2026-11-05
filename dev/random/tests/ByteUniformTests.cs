namespace random.Tests;

public class ByteUniformTests
{
    [Test]
    public void DeterministicByteMethodsMatchUnbiasedUInt32Mappings()
    {
        RandomGenerator[] boundedActual = CreateDeterministicGenerators();
        RandomGenerator[] boundedReference = CreateDeterministicGenerators();
        byte[] bounds = [1, 2, 3, 10, 127, 128, 254, 255];
        for (int generatorIndex = 0; generatorIndex < boundedActual.Length; generatorIndex++)
        {
            foreach (byte bound in bounds)
            {
                Assert.That(boundedActual[generatorIndex].NextByteBounded(bound), Is.EqualTo((byte)boundedReference[generatorIndex].NextUInt32Bounded((uint)bound)));
            }

            Assert.That(boundedActual[generatorIndex].ExportState().ToBytes(), Is.EqualTo(boundedReference[generatorIndex].ExportState().ToBytes()));
        }

        RandomGenerator[] rangedActual = CreateDeterministicGenerators();
        RandomGenerator[] rangedReference = CreateDeterministicGenerators();
        byte[][] ranges = [[0, 1], [5, 6], [1, 255], [127, 255], [200, 255]];
        for (int generatorIndex = 0; generatorIndex < rangedActual.Length; generatorIndex++)
        {
            foreach (byte[] range in ranges)
            {
                Assert.That(rangedActual[generatorIndex].NextByteInRange(range[0], range[1]), Is.EqualTo((byte)rangedReference[generatorIndex].NextUInt32InRange((uint)range[0], (uint)range[1])));
            }

            Assert.That(rangedActual[generatorIndex].ExportState().ToBytes(), Is.EqualTo(rangedReference[generatorIndex].ExportState().ToBytes()));
        }
    }

    [Test]
    public void DeterministicByteDegenerateAndInvalidDomainsPreserveState()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        Assert.Multiple(() =>
        {
            Assert.That(generator.NextByteBounded(1), Is.EqualTo((byte)0));
            Assert.That(generator.NextByteInRange(254, 255), Is.EqualTo((byte)254));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });

        object[] cases =
        [
            new object[] { (TestDelegate)(() => generator.NextByteBounded(0)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextByteInRange(5, 5)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextByteInRange(6, 5)), "maximum" }
        ];
        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.Multiple(() =>
            {
                Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            });
        }
    }

    [Test]
    public void SecureByteMethodsMatchUnbiasedUInt32Mappings()
    {
        SecureRandomGenerator boundedActual = CreateSecureGenerator();
        SecureRandomGenerator boundedReference = CreateSecureGenerator();
        byte[] bounds = [1, 2, 3, 10, 127, 128, 254, 255];
        foreach (byte bound in bounds)
        {
            Assert.That(boundedActual.NextByteBounded(bound), Is.EqualTo((byte)boundedReference.NextUInt32Bounded((uint)bound)));
        }

        Assert.That(boundedActual.NextUInt64(), Is.EqualTo(boundedReference.NextUInt64()));

        SecureRandomGenerator rangedActual = CreateSecureGenerator();
        SecureRandomGenerator rangedReference = CreateSecureGenerator();
        byte[][] ranges = [[0, 1], [5, 6], [1, 255], [127, 255], [200, 255]];
        foreach (byte[] range in ranges)
        {
            Assert.That(rangedActual.NextByteInRange(range[0], range[1]), Is.EqualTo((byte)rangedReference.NextUInt32InRange((uint)range[0], (uint)range[1])));
        }

        Assert.That(rangedActual.NextUInt64(), Is.EqualTo(rangedReference.NextUInt64()));
    }

    [Test]
    public void SecureByteDegenerateAndInvalidDomainsDoNotRequestBytes()
    {
        SecureRandomGenerator actual = CreateSecureGenerator();
        SecureRandomGenerator reference = CreateSecureGenerator();

        Assert.Multiple(() =>
        {
            Assert.That(actual.NextByteBounded(1), Is.EqualTo((byte)0));
            Assert.That(actual.NextByteInRange(254, 255), Is.EqualTo((byte)254));
        });

        object[] cases =
        [
            new object[] { (TestDelegate)(() => actual.NextByteBounded(0)), "bound" },
            new object[] { (TestDelegate)(() => actual.NextByteInRange(5, 5)), "maximum" },
            new object[] { (TestDelegate)(() => actual.NextByteInRange(6, 5)), "maximum" }
        ];
        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        Assert.That(actual.NextUInt64(), Is.EqualTo(reference.NextUInt64()));
    }

    [Test]
    public void DirectSystemSourceByteMethodsStayInsideRequestedDomains()
    {
        SecureRandomGenerator generator = SecureRandomGenerator.CreateSystem();
        for (int index = 0; index < 256; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(generator.NextByteBounded(17), Is.LessThan((byte)17));
                Assert.That(generator.NextByteInRange(200, 251), Is.InRange((byte)200, (byte)250));
            });
        }
    }

    static RandomGenerator[] CreateDeterministicGenerators()
    {
        return
        [
            RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable),
            RandomGenerator.CreateMt19937(42U, RandomMathMode.Portable)
        ];
    }

    static SecureRandomGenerator CreateSecureGenerator()
    {
        return SecureRandomGenerator.CreateHmacDrbg(Sequence(0x00, 32), Sequence(0x20, 16), Sequence(0x30, 11));
    }

    static byte[] Sequence(int start, int length)
    {
        byte[] result = new byte[length];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = (byte)(start + index);
        }

        return result;
    }
}
