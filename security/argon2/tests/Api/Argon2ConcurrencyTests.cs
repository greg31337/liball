using argon2;

namespace argon2.Tests.Api;

public class Argon2ConcurrencyTests
{
    const int OperationCount = 24;

    [Test]
    public void SharedPrimitiveProducesDeterministicIsolatedResults()
    {
        IArgon2Primitive primitive = new Argon2Primitive();
        Argon2Parameters parameters = CreateParameters();
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions();
        byte[] salt = new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 };
        byte[] firstPassword = new byte[] { 1, 2, 3, 4 };
        byte[] secondPassword = new byte[] { 5, 6, 7, 8 };
        byte[] firstExpected = new byte[16];
        byte[] secondExpected = new byte[16];
        primitive.ComputeHash(
            firstPassword,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            firstExpected);
        primitive.ComputeHash(
            secondPassword,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            secondExpected);
        byte[][] actual = new byte[OperationCount][];

        Parallel.For(0, actual.Length, index =>
        {
            byte[] destination = new byte[16];
            byte[] password = index % 2 == 0 ? firstPassword : secondPassword;
            primitive.ComputeHash(
                password,
                salt,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                parameters,
                executionOptions,
                destination);
            actual[index] = destination;
        });

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < actual.Length)
            {
                byte[] expected = index % 2 == 0 ? firstExpected : secondExpected;
                Assert.That(actual[index], Is.EqualTo(expected));
                index = index + 1;
            }
        });
    }

    [Test]
    public void SharedKeyDeriverProducesDeterministicIsolatedResults()
    {
        IArgon2KeyDeriver keyDeriver = new Argon2KeyDeriver();
        Argon2Parameters parameters = CreateParameters();
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions();
        byte[] salt = new byte[] { 16, 17, 18, 19, 20, 21, 22, 23 };
        byte[] firstSecret = new byte[] { 1, 3, 5, 7 };
        byte[] secondSecret = new byte[] { 2, 4, 6, 8 };
        byte[] firstExpected = keyDeriver.DeriveKey(
            new byte[] { 9, 8, 7, 6 },
            salt,
            firstSecret,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            16);
        byte[] secondExpected = keyDeriver.DeriveKey(
            new byte[] { 9, 8, 7, 6 },
            salt,
            secondSecret,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            16);
        byte[][] actual = new byte[OperationCount][];

        Parallel.For(0, actual.Length, index =>
        {
            byte[] secret = index % 2 == 0 ? firstSecret : secondSecret;
            actual[index] = keyDeriver.DeriveKey(
                new byte[] { 9, 8, 7, 6 },
                salt,
                secret,
                ReadOnlySpan<byte>.Empty,
                parameters,
                executionOptions,
                16);
        });

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < actual.Length)
            {
                byte[] expected = index % 2 == 0 ? firstExpected : secondExpected;
                Assert.That(actual[index], Is.EqualTo(expected));
                index = index + 1;
            }
        });
    }

    [Test]
    public void SharedCodecProducesDeterministicIndependentResults()
    {
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        Argon2EncodedHash firstHash = CreateEncodedHashData(1);
        Argon2EncodedHash secondHash = CreateEncodedHashData(33);
        string firstExpected = codec.EncodeHash(firstHash);
        string secondExpected = codec.EncodeHash(secondHash);
        string[] encoded = new string[OperationCount];
        Argon2EncodedHash[] decoded = new Argon2EncodedHash[OperationCount];

        Parallel.For(0, encoded.Length, index =>
        {
            Argon2EncodedHash source = index % 2 == 0 ? firstHash : secondHash;
            encoded[index] = codec.EncodeHash(source);
            decoded[index] = codec.DecodeHash(encoded[index]);
        });

        decoded[0].Parameters.MemorySizeKiB = 64L;
        decoded[0].Salt[0] = 0xFF;
        decoded[0].Tag[0] = 0xFF;

        Assert.Multiple(() =>
        {
            int index = 1;
            while (index < encoded.Length)
            {
                string expected = index % 2 == 0 ? firstExpected : secondExpected;
                Argon2EncodedHash expectedHash = index % 2 == 0 ? firstHash : secondHash;
                Assert.That(encoded[index], Is.EqualTo(expected));
                Assert.That(decoded[index].Parameters.MemorySizeKiB, Is.EqualTo(8L));
                Assert.That(decoded[index].Salt, Is.EqualTo(expectedHash.Salt));
                Assert.That(decoded[index].Tag, Is.EqualTo(expectedHash.Tag));
                index = index + 1;
            }

            Assert.That(decoded.Distinct().Count(), Is.EqualTo(OperationCount));
            Assert.That(decoded.Select(hash => hash.Parameters).Distinct().Count(), Is.EqualTo(OperationCount));
            Assert.That(decoded.Select(hash => hash.Salt).Distinct().Count(), Is.EqualTo(OperationCount));
            Assert.That(decoded.Select(hash => hash.Tag).Distinct().Count(), Is.EqualTo(OperationCount));
            Assert.That(firstHash.Parameters.MemorySizeKiB, Is.EqualTo(8L));
            Assert.That(firstHash.Salt[0], Is.EqualTo(1));
            Assert.That(firstHash.Tag[0], Is.EqualTo(17));
        });
    }

    [Test]
    public void SharedPasswordHasherUsesUniqueSaltsAndIsolatesPerCallContext()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateHasherOptions());
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        byte[] password = new byte[] { 4, 3, 2, 1 };
        byte[][] secrets = new byte[OperationCount][];
        byte[][] associatedData = new byte[OperationCount][];
        string[] encoded = new string[OperationCount];
        int index = 0;
        while (index < OperationCount)
        {
            secrets[index] = new byte[] { 0xA0, (byte)index };
            associatedData[index] = new byte[] { 0xB0, (byte)(OperationCount - index) };
            index = index + 1;
        }

        Parallel.For(0, encoded.Length, operationIndex =>
        {
            encoded[operationIndex] = hasher.HashPasswordBytesWithContext(
                password,
                secrets[operationIndex],
                associatedData[operationIndex]);
        });

        string[] salts = encoded
            .Select(value => Convert.ToHexString(codec.DecodeHash(value).Salt))
            .ToArray();
        Argon2VerificationResult[] matching = new Argon2VerificationResult[OperationCount];
        Argon2VerificationResult[] wrongContext = new Argon2VerificationResult[OperationCount];
        Parallel.For(0, encoded.Length, operationIndex =>
        {
            matching[operationIndex] = hasher.VerifyPasswordBytesWithContext(
                password,
                encoded[operationIndex],
                secrets[operationIndex],
                associatedData[operationIndex]);
            int adjacentIndex = (operationIndex + 1) % OperationCount;
            wrongContext[operationIndex] = hasher.VerifyPasswordBytesWithContext(
                password,
                encoded[operationIndex],
                secrets[adjacentIndex],
                associatedData[operationIndex]);
        });

        Assert.Multiple(() =>
        {
            Assert.That(salts.Distinct().Count(), Is.EqualTo(OperationCount));
            int resultIndex = 0;
            while (resultIndex < OperationCount)
            {
                Assert.That(
                    matching[resultIndex].Status,
                    Is.EqualTo(Argon2VerificationStatus.Match));
                Assert.That(matching[resultIndex].NeedsRehash, Is.False);
                Assert.That(
                    wrongContext[resultIndex].Status,
                    Is.EqualTo(Argon2VerificationStatus.Mismatch));
                Assert.That(wrongContext[resultIndex].NeedsRehash, Is.False);
                resultIndex = resultIndex + 1;
            }
        });
    }

    static Argon2EncodedHash CreateEncodedHashData(int offset)
    {
        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Parameters = CreateParameters();
        hash.Salt = CreateSequence(8, offset);
        hash.Tag = CreateSequence(16, offset + 16);
        return hash;
    }

    static Argon2PasswordHasherOptions CreateHasherOptions()
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.Parameters = CreateParameters();
        options.SaltLength = 8;
        options.TagLength = 16;
        options.ExecutionOptions = CreateExecutionOptions();
        options.VerificationPolicy.MaximumMemorySizeKiB = 8L;
        options.VerificationPolicy.MaximumIterations = 1L;
        options.VerificationPolicy.MaximumDegreeOfParallelism = 1;
        options.VerificationPolicy.MaximumSaltLength = 8;
        options.VerificationPolicy.MaximumTagLength = 16;
        return options;
    }

    static Argon2Parameters CreateParameters()
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = Argon2Variant.Id;
        parameters.Version = Argon2Version.Version13;
        parameters.MemorySizeKiB = 8L;
        parameters.Iterations = 1L;
        parameters.DegreeOfParallelism = 1;
        return parameters;
    }

    static Argon2ExecutionOptions CreateExecutionOptions()
    {
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 1;
        return executionOptions;
    }

    static byte[] CreateSequence(int length, int offset)
    {
        byte[] value = new byte[length];
        int index = 0;
        while (index < value.Length)
        {
            value[index] = (byte)(offset + index);
            index = index + 1;
        }

        return value;
    }
}
