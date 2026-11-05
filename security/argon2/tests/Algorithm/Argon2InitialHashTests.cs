using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2InitialHashTests
{
    [Test]
    public void Rfc9106PrehashDigestsMatchAllVariants()
    {
        byte[] password = Enumerable.Repeat((byte)1, 32).ToArray();
        byte[] salt = Enumerable.Repeat((byte)2, 16).ToArray();
        byte[] secret = Enumerable.Repeat((byte)3, 8).ToArray();
        byte[] associatedData = Enumerable.Repeat((byte)4, 12).ToArray();

        byte[] argon2d = ComputeVectorHash(
            Argon2Variant.D,
            password,
            salt,
            secret,
            associatedData);
        byte[] argon2i = ComputeVectorHash(
            Argon2Variant.I,
            password,
            salt,
            secret,
            associatedData);
        byte[] argon2id = ComputeVectorHash(
            Argon2Variant.Id,
            password,
            salt,
            secret,
            associatedData);

        Assert.Multiple(() =>
        {
            Assert.That(
                ToHex(argon2d),
                Is.EqualTo(
                    "b8819791a0359660bb7709c85fa48f04d5d82c05c5f215ccdb885491717cf757" +
                    "082c28b951be381410b5fc2eb7274033b9fdc7ae672bcaac5d179097a4af3109"));
            Assert.That(
                ToHex(argon2i),
                Is.EqualTo(
                    "c46065815276a0b3e731731c902f1fd80cf776907fbb7b6a5ca72e7b56011fee" +
                    "ca446c86dd75b9469a5e6879dec4b72d0863fb939b982e5f397cc7d164fddaa9"));
            Assert.That(
                ToHex(argon2id),
                Is.EqualTo(
                    "2889de487eb42ae500c0007ed9252f1069eadec40d5765b485de6dc2437a67b8" +
                    "546a2f0acc1a0882db8fcf74714b472e94df421a5da1112ffa11434370a1e997"));
        });
    }

    [Test]
    public void EmptyOptionalInputsAreLengthPrefixedAndAccepted()
    {
        Argon2Parameters parameters = CreateVectorParameters(Argon2Variant.Id);
        byte[] output = new byte[64];

        Assert.DoesNotThrow(
            () => Argon2InitialHash.ComputeHash(
                ReadOnlySpan<byte>.Empty,
                new byte[8],
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                parameters,
                32,
                output));
        Assert.That(output, Has.Some.Not.EqualTo(0));
    }

    [Test]
    public void CallerOwnedInputsAreNotModified()
    {
        byte[] password = Enumerable.Repeat((byte)1, 32).ToArray();
        byte[] salt = Enumerable.Repeat((byte)2, 16).ToArray();
        byte[] secret = Enumerable.Repeat((byte)3, 8).ToArray();
        byte[] associatedData = Enumerable.Repeat((byte)4, 12).ToArray();
        byte[] originalPassword = (byte[])password.Clone();
        byte[] originalSalt = (byte[])salt.Clone();
        byte[] originalSecret = (byte[])secret.Clone();
        byte[] originalAssociatedData = (byte[])associatedData.Clone();

        ComputeVectorHash(Argon2Variant.Id, password, salt, secret, associatedData);

        Assert.Multiple(() =>
        {
            Assert.That(password, Is.EqualTo(originalPassword));
            Assert.That(salt, Is.EqualTo(originalSalt));
            Assert.That(secret, Is.EqualTo(originalSecret));
            Assert.That(associatedData, Is.EqualTo(originalAssociatedData));
        });
    }

    [Test]
    public void RequestedMemoryAndTagLengthParticipateInInitialHash()
    {
        byte[] password = Enumerable.Repeat((byte)1, 32).ToArray();
        byte[] salt = Enumerable.Repeat((byte)2, 16).ToArray();
        byte[] secret = Enumerable.Repeat((byte)3, 8).ToArray();
        byte[] associatedData = Enumerable.Repeat((byte)4, 12).ToArray();
        Argon2Parameters parameters = CreateVectorParameters(Argon2Variant.Id);
        byte[] baseline = new byte[64];
        byte[] changedMemory = new byte[64];
        byte[] changedTagLength = new byte[64];

        Argon2InitialHash.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            32,
            baseline);
        parameters.MemorySizeKiB = 33L;
        Argon2InitialHash.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            32,
            changedMemory);
        parameters.MemorySizeKiB = 32L;
        Argon2InitialHash.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            33,
            changedTagLength);

        Assert.Multiple(() =>
        {
            Assert.That(changedMemory, Is.Not.EqualTo(baseline));
            Assert.That(changedTagLength, Is.Not.EqualTo(baseline));
        });
    }

    [Test]
    public void DestinationMustBeExactlyPrehashDigestLength()
    {
        Argon2Parameters parameters = CreateVectorParameters(Argon2Variant.Id);

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => Argon2InitialHash.ComputeHash(
                    Array.Empty<byte>(),
                    new byte[8],
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    parameters,
                    32,
                    new byte[63]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2InitialHash.ComputeHash(
                    Array.Empty<byte>(),
                    new byte[8],
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    parameters,
                    32,
                    new byte[65]));
        });
    }

    static byte[] ComputeVectorHash(
        Argon2Variant variant,
        byte[] password,
        byte[] salt,
        byte[] secret,
        byte[] associatedData)
    {
        Argon2Parameters parameters = CreateVectorParameters(variant);
        byte[] output = new byte[64];
        Argon2InitialHash.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            32,
            output);
        return output;
    }

    static Argon2Parameters CreateVectorParameters(Argon2Variant variant)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = Argon2Version.Version13;
        parameters.Iterations = 3L;
        parameters.MemorySizeKiB = 32L;
        parameters.DegreeOfParallelism = 4;
        return parameters;
    }

    static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }
}
