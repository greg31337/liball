using System.Buffers.Binary;
using System.Numerics;
using System.Reflection;

namespace random.Tests;

public class SecureRandomGeneratorTests
{
    [Test]
    public void BothSecureModesCoverEveryPublicIntegerAndTokenFamily()
    {
        ISecureRandomGenerator[] generators =
        [
            SecureRandomGenerator.CreateSystem(),
            Create()
        ];

        foreach (ISecureRandomGenerator generator in generators)
        {
            byte[] generated = new byte[13];
            byte[] filled = new byte[17];
            generator.Generate(generated);
            generator.FillBytes(filled);
            byte[] allocated = generator.NextBytes(19);
            byte boundedByte = generator.NextByteBounded(17);
            byte rangedByte = generator.NextByteInRange(200, 251);
            generator.NextUInt32();
            uint boundedUInt32 = generator.NextUInt32Bounded(1000U);
            uint rangedUInt32 = generator.NextUInt32InRange(1000U, 2000U);
            generator.NextUInt64();
            ulong boundedUInt64 = generator.NextUInt64Bounded(100000UL);
            ulong rangedUInt64 = generator.NextUInt64InRange(100000UL, 200000UL);
            generator.NextInt32();
            int boundedInt32 = generator.NextInt32Bounded(1000);
            int rangedInt32 = generator.NextInt32InRange(-1000, 1000);
            generator.NextInt64();
            long boundedInt64 = generator.NextInt64Bounded(100000L);
            long rangedInt64 = generator.NextInt64InRange(-100000L, 100000L);
            BigInteger boundedBigInteger = generator.NextBigInteger(BigInteger.One << 130);
            BigInteger rangedBigInteger = generator.NextBigIntegerInRange(-(BigInteger.One << 129), BigInteger.One << 129);
            generator.NextBoolean();
            string hex = generator.TokenHex(7);
            string base32 = generator.TokenBase32(7);
            string base64 = generator.TokenBase64(7);
            string base64Url = generator.TokenBase64Url(7);
            string text = generator.String("ABCxyz09", 23);
            PasswordPolicy policy = new PasswordPolicy(16, "ABCxyz09", ["ABC", "09"], [2, 2], true, true);
            string password = generator.Password(policy);
            Guid uuid4 = generator.Uuid4();
            Guid uuid7 = generator.Uuid7();
            byte[] uuid4Bytes = SecureUuid.NetworkBytes(uuid4);
            byte[] uuid7Bytes = SecureUuid.NetworkBytes(uuid7);

            Assert.Multiple(() =>
            {
                Assert.That(generator.Info().IsCryptographic(), Is.True);
                Assert.That(allocated, Has.Length.EqualTo(19));
                Assert.That(boundedByte, Is.LessThan((byte)17));
                Assert.That(rangedByte, Is.InRange((byte)200, (byte)250));
                Assert.That(boundedUInt32, Is.LessThan(1000U));
                Assert.That(rangedUInt32, Is.InRange(1000U, 1999U));
                Assert.That(boundedUInt64, Is.LessThan(100000UL));
                Assert.That(rangedUInt64, Is.InRange(100000UL, 199999UL));
                Assert.That(boundedInt32, Is.InRange(0, 999));
                Assert.That(rangedInt32, Is.InRange(-1000, 999));
                Assert.That(boundedInt64, Is.InRange(0L, 99999L));
                Assert.That(rangedInt64, Is.InRange(-100000L, 99999L));
                Assert.That(boundedBigInteger, Is.InRange(BigInteger.Zero, (BigInteger.One << 130) - BigInteger.One));
                Assert.That(rangedBigInteger, Is.InRange(-(BigInteger.One << 129), (BigInteger.One << 129) - BigInteger.One));
                Assert.That(hex, Does.Match("^[0-9A-F]{14}$"));
                Assert.That(base32, Does.Match("^[A-Z2-7]{12}$"));
                Assert.That(Convert.FromBase64String(base64), Has.Length.EqualTo(7));
                Assert.That(base64Url, Does.Match("^[A-Za-z0-9_-]{10}$"));
                Assert.That(text, Has.Length.EqualTo(23).And.All.Matches<char>(character => "ABCxyz09".Contains(character)));
                Assert.That(password, Has.Length.EqualTo(16));
                Assert.That(password.Count("ABC".Contains), Is.GreaterThanOrEqualTo(2));
                Assert.That(password.Count("09".Contains), Is.GreaterThanOrEqualTo(2));
                Assert.That(uuid4Bytes[6] >> 4, Is.EqualTo(4));
                Assert.That(uuid4Bytes[8] >> 6, Is.EqualTo(2));
                Assert.That(uuid7Bytes[6] >> 4, Is.EqualTo(7));
                Assert.That(uuid7Bytes[8] >> 6, Is.EqualTo(2));
            });
        }
    }

    [Test]
    public void PublicUninstantiateZeroesStateAndRejectsEveryOutputFamily()
    {
        SecureRandomGenerator generator = Create();
        HmacDrbgSha256Engine engine = HmacEngine(generator);
        FieldInfo keyField = ReflectionInfo.Required<FieldInfo>(typeof(HmacDrbgSha256Engine).GetField("key", BindingFlags.Instance | BindingFlags.NonPublic));
        FieldInfo valueField = ReflectionInfo.Required<FieldInfo>(typeof(HmacDrbgSha256Engine).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic));
        byte[] retiredKey = ReflectionInfo.Required<byte[]>(keyField.GetValue(engine));
        byte[] retiredValue = ReflectionInfo.Required<byte[]>(valueField.GetValue(engine));

        generator.Uninstantiate();

        TestDelegate[] operations =
        [
            () => generator.Generate(new byte[1]),
            () => generator.FillBytes(new byte[1]),
            () => generator.NextBytes(1),
            () => generator.NextByteBounded(2),
            () => generator.NextByteInRange(0, 2),
            () => generator.NextUInt32(),
            () => generator.NextUInt32Bounded(2U),
            () => generator.NextUInt32InRange(0U, 2U),
            () => generator.NextUInt64(),
            () => generator.NextUInt64Bounded(2UL),
            () => generator.NextUInt64InRange(0UL, 2UL),
            () => generator.NextInt32(),
            () => generator.NextInt32Bounded(2),
            () => generator.NextInt32InRange(0, 2),
            () => generator.NextInt64(),
            () => generator.NextInt64Bounded(2L),
            () => generator.NextInt64InRange(0L, 2L),
            () => generator.NextBigInteger(new BigInteger(2)),
            () => generator.NextBigIntegerInRange(BigInteger.Zero, new BigInteger(2)),
            () => generator.NextBoolean(),
            () => generator.TokenHex(1),
            () => generator.TokenBase32(1),
            () => generator.TokenBase64(1),
            () => generator.TokenBase64Url(1),
            () => generator.String("AB", 1),
            () => generator.Password(new PasswordPolicy(1, "AB", [], [], true, true)),
            () => generator.Uuid4(),
            () => generator.Uuid7()
        ];

        Assert.Multiple(() =>
        {
            Assert.That(retiredKey, Is.All.EqualTo(0));
            Assert.That(retiredValue, Is.All.EqualTo(0));
            Assert.That(engine.KeyBytes(), Is.Empty);
            Assert.That(engine.ValueBytes(), Is.Empty);
            Assert.That(engine.ReseedCounter(), Is.EqualTo(0UL));
            Assert.That(engine.IsInstantiated(), Is.False);
        });
        foreach (TestDelegate operation in operations)
        {
            Assert.Throws<RandomSecurityException>(operation);
        }
    }

    [Test]
    public void FactoriesSelectOnlyCryptographicAlgorithms()
    {
        SecureRandomGenerator system = SecureRandomGenerator.CreateSystem();
        SecureRandomGenerator hmac = SecureRandomGenerator.CreateHmacDrbg();

        Assert.Multiple(() =>
        {
            Assert.That(system.Info().Algorithm(), Is.EqualTo(RandomAlgorithm.SystemCryptoV1));
            Assert.That(system.Info().IsCryptographic(), Is.True);
            Assert.That(system.Info().StateBytes(), Is.EqualTo(0));
            Assert.That(hmac.Info().Algorithm(), Is.EqualTo(RandomAlgorithm.HmacDrbgSha256V1));
            Assert.That(hmac.Info().IsCryptographic(), Is.True);
            Assert.That(hmac.Info().StateBytes(), Is.EqualTo(0));
            Assert.That(system.NextBytes(17), Has.Length.EqualTo(17));
            Assert.That(hmac.NextBytes(17), Has.Length.EqualTo(17));
        });
    }

    [Test]
    public void ExplicitHmacFactoryValidatesAndPreservesCallerInputs()
    {
        byte[] entropy = Sequence(0x00, 32);
        byte[] nonce = Sequence(0x20, 16);
        byte[] personalization = Sequence(0x30, 21);
        byte[] expectedEntropy = (byte[])entropy.Clone();
        byte[] expectedNonce = (byte[])nonce.Clone();
        byte[] expectedPersonalization = (byte[])personalization.Clone();

        SecureRandomGenerator generator = SecureRandomGenerator.CreateHmacDrbg(entropy, nonce, personalization);

        Assert.Multiple(() =>
        {
            Assert.That(generator.Info().Algorithm(), Is.EqualTo(RandomAlgorithm.HmacDrbgSha256V1));
            Assert.That(entropy, Is.EqualTo(expectedEntropy));
            Assert.That(nonce, Is.EqualTo(expectedNonce));
            Assert.That(personalization, Is.EqualTo(expectedPersonalization));
            Assert.Throws<RandomArgumentException>(() => SecureRandomGenerator.CreateHmacDrbg(new byte[31], nonce, personalization));
            Assert.Throws<RandomArgumentException>(() => SecureRandomGenerator.CreateHmacDrbg(entropy, new byte[15], personalization));
            Assert.Throws<RandomArgumentException>(() => SecureRandomGenerator.CreateHmacDrbg(NullReference.Value<byte[]>(), nonce, personalization));
        });
    }

    [Test]
    public void RawIntegersDecodeIndependentRequestsInLittleEndianOrder()
    {
        SecureRandomGenerator unsigned = Create();
        HmacDrbgSha256Engine unsignedReference = CreateEngine();
        byte[] expected32Bytes = new byte[4];
        byte[] expected64Bytes = new byte[8];
        unsignedReference.Generate(expected32Bytes);
        unsignedReference.Generate(expected64Bytes);

        SecureRandomGenerator signed = Create();
        HmacDrbgSha256Engine signedReference = CreateEngine();
        byte[] expectedSigned32Bytes = new byte[4];
        byte[] expectedSigned64Bytes = new byte[8];
        signedReference.Generate(expectedSigned32Bytes);
        signedReference.Generate(expectedSigned64Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(unsigned.NextUInt32(), Is.EqualTo(BinaryPrimitives.ReadUInt32LittleEndian(expected32Bytes)));
            Assert.That(unsigned.NextUInt64(), Is.EqualTo(BinaryPrimitives.ReadUInt64LittleEndian(expected64Bytes)));
            Assert.That(signed.NextInt32(), Is.EqualTo(int.CreateTruncating(BinaryPrimitives.ReadUInt32LittleEndian(expectedSigned32Bytes))));
            Assert.That(signed.NextInt64(), Is.EqualTo(long.CreateTruncating(BinaryPrimitives.ReadUInt64LittleEndian(expectedSigned64Bytes))));
        });
    }

    [Test]
    public void GenerateSupportsAdditionalInputAndEnforcesOneRequestLimitAtomically()
    {
        SecureRandomGenerator actual = Create();
        HmacDrbgSha256Engine reference = CreateEngine();
        byte[] additionalInput = Sequence(0x80, 25);
        byte[] actualOutput = new byte[129];
        byte[] expectedOutput = new byte[129];

        actual.GenerateWithOptions(actualOutput, additionalInput, false);
        reference.GenerateWithAdditionalInput(expectedOutput, additionalInput);

        Assert.That(actualOutput, Is.EqualTo(expectedOutput));

        SecureRandomGenerator invalid = Create();
        SecureRandomGenerator untouched = Create();
        byte[] destination = Enumerable.Repeat((byte)0xA5, 65537).ToArray();
        Assert.Throws<RandomArgumentException>(() => invalid.Generate(destination));
        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.All.EqualTo(0xA5));
            Assert.That(invalid.NextUInt64(), Is.EqualTo(untouched.NextUInt64()));
        });
    }

    [Test]
    public void FillBytesSplitsLargeHmacOutputIntoCompliantRequests()
    {
        SecureRandomGenerator actual = Create();
        HmacDrbgSha256Engine reference = CreateEngine();
        byte[] actualOutput = new byte[70000];
        byte[] expectedOutput = new byte[70000];

        actual.FillBytes(actualOutput);
        reference.Generate(expectedOutput.AsSpan(0, 65536));
        reference.Generate(expectedOutput.AsSpan(65536));

        Assert.That(actualOutput, Is.EqualTo(expectedOutput));
    }

    [Test]
    public void HmacLifecycleReseedsPredictablyAndUninstantiateIsTerminal()
    {
        SecureRandomGenerator generator = Create();
        byte[] additionalInput = Sequence(0xA0, 19);
        byte[] expectedAdditionalInput = (byte[])additionalInput.Clone();

        generator.ReseedWithAdditionalInput(additionalInput);
        Assert.Multiple(() =>
        {
            Assert.That(HmacEngine(generator).ReseedCounter(), Is.EqualTo(1UL));
            Assert.That(additionalInput, Is.EqualTo(expectedAdditionalInput));
        });

        generator.GenerateWithOptions(new byte[32], new byte[0], true);
        Assert.That(HmacEngine(generator).ReseedCounter(), Is.EqualTo(2UL));

        generator.Uninstantiate();
        Assert.Multiple(() =>
        {
            Assert.Throws<RandomSecurityException>(() => generator.Generate(Span<byte>.Empty));
            Assert.Throws<RandomSecurityException>(() => generator.FillBytes(Span<byte>.Empty));
            Assert.Throws<RandomSecurityException>(() => generator.NextUInt32());
            Assert.Throws<RandomSecurityException>(() => generator.Reseed());
            Assert.DoesNotThrow(() => generator.Uninstantiate());
        });
    }

    [Test]
    public void PublicReseedStatusTracksRequestsAndAutomaticReseeding()
    {
        SecureRandomGenerator generator = Create();
        ISecureRandomGenerator contract = generator;

        Assert.Multiple(() =>
        {
            Assert.That(contract.ReseedCounter(), Is.EqualTo(1UL));
            Assert.That(contract.IsReseedRequired(), Is.False);
        });

        contract.Generate(Span<byte>.Empty);
        Assert.That(contract.ReseedCounter(), Is.EqualTo(1UL));

        contract.Generate(new byte[1]);
        Assert.That(contract.ReseedCounter(), Is.EqualTo(2UL));

        contract.FillBytes(new byte[65537]);
        Assert.Multiple(() =>
        {
            Assert.That(contract.ReseedCounter(), Is.EqualTo(4UL));
            Assert.That(contract.IsReseedRequired(), Is.False);
        });

        FieldInfo counterField = ReflectionInfo.Required<FieldInfo>(typeof(HmacDrbgSha256Engine).GetField("reseedCounter", BindingFlags.Instance | BindingFlags.NonPublic));
        counterField.SetValue(HmacEngine(generator), HmacDrbgSha256Engine.ReseedInterval);
        Assert.Multiple(() =>
        {
            Assert.That(contract.ReseedCounter(), Is.EqualTo(HmacDrbgSha256Engine.ReseedInterval));
            Assert.That(contract.IsReseedRequired(), Is.False);
        });

        contract.Generate(new byte[1]);
        Assert.Multiple(() =>
        {
            Assert.That(contract.ReseedCounter(), Is.EqualTo(HmacDrbgSha256Engine.ReseedInterval + 1UL));
            Assert.That(contract.IsReseedRequired(), Is.True);
        });

        contract.Generate(new byte[1]);
        Assert.Multiple(() =>
        {
            Assert.That(contract.ReseedCounter(), Is.EqualTo(2UL));
            Assert.That(contract.IsReseedRequired(), Is.False);
        });

        contract.Reseed();
        Assert.Multiple(() =>
        {
            Assert.That(contract.ReseedCounter(), Is.EqualTo(1UL));
            Assert.That(contract.IsReseedRequired(), Is.False);
        });
    }

    [Test]
    public void ReseedStatusIsUnsupportedForSystemAndTerminalAfterUninstantiation()
    {
        ISecureRandomGenerator system = SecureRandomGenerator.CreateSystem();
        RandomCapabilityException systemCounter = Assert.Throws<RandomCapabilityException>(() => system.ReseedCounter());
        RandomCapabilityException systemStatus = Assert.Throws<RandomCapabilityException>(() => system.IsReseedRequired());

        SecureRandomGenerator hmac = Create();
        ISecureRandomGenerator uninstantiated = hmac;
        hmac.Uninstantiate();

        Assert.Multiple(() =>
        {
            Assert.That(systemCounter.Algorithm(), Is.EqualTo(RandomAlgorithm.SystemCryptoV1));
            Assert.That(systemCounter.Capability(), Is.EqualTo(RandomCapability.Reseed));
            Assert.That(systemStatus.Algorithm(), Is.EqualTo(RandomAlgorithm.SystemCryptoV1));
            Assert.That(systemStatus.Capability(), Is.EqualTo(RandomCapability.Reseed));
            Assert.Throws<RandomSecurityException>(() => uninstantiated.ReseedCounter());
            Assert.Throws<RandomSecurityException>(() => uninstantiated.IsReseedRequired());
        });
    }

    [Test]
    public void ExpiredHmacGeneratorAutomaticallyReseedsFromTheSystemSource()
    {
        SecureRandomGenerator generator = Create();
        HmacDrbgSha256Engine engine = HmacEngine(generator);
        FieldInfo counterField = ReflectionInfo.Required<FieldInfo>(typeof(HmacDrbgSha256Engine).GetField("reseedCounter", BindingFlags.Instance | BindingFlags.NonPublic));
        counterField.SetValue(engine, HmacDrbgSha256Engine.ReseedInterval + 1UL);

        Assert.DoesNotThrow(() => generator.Generate(new byte[8]));
        Assert.That(engine.ReseedCounter(), Is.EqualTo(2UL));
    }

    [Test]
    public void SystemSourceRejectsStatefulDrbgOptionsBeforeFilling()
    {
        SecureRandomGenerator generator = SecureRandomGenerator.CreateSystem();
        byte[] destination = new byte[] { 0xA5 };

        RandomCapabilityException reseed = Assert.Throws<RandomCapabilityException>(() => generator.Reseed());
        RandomCapabilityException additional = Assert.Throws<RandomCapabilityException>(() => generator.GenerateWithOptions(destination, new byte[] { 1 }, false));
        RandomCapabilityException resistance = Assert.Throws<RandomCapabilityException>(() => generator.GenerateWithOptions(destination, new byte[0], true));
        RandomCapabilityException uninstantiate = Assert.Throws<RandomCapabilityException>(() => generator.Uninstantiate());

        Assert.Multiple(() =>
        {
            Assert.That(reseed.Capability(), Is.EqualTo(RandomCapability.Reseed));
            Assert.That(additional.Capability(), Is.EqualTo(RandomCapability.AdditionalInput));
            Assert.That(resistance.Capability(), Is.EqualTo(RandomCapability.PredictionResistance));
            Assert.That(uninstantiate.Capability(), Is.EqualTo(RandomCapability.Uninstantiate));
            Assert.That(destination, Is.EqualTo(new byte[] { 0xA5 }));
        });
    }

    [Test]
    public void SecureSurfaceHasNoReplayOrWeakScalarSeedApi()
    {
        Type type = typeof(SecureRandomGenerator);
        string[] forbiddenMethods = ["Clone", "ExportState", "RestoreState", "Reset", "Jump", "LongJump", "Advance", "Seek", "Spawn", "SpawnMany", "MathMode"];

        Assert.That(type.GetConstructors(), Is.Empty);
        foreach (string methodName in forbiddenMethods)
        {
            Assert.That(type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Any(method => method.Name == methodName), Is.False, methodName);
        }

        MethodInfo[] factories = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(method => method.Name.StartsWith("Create", StringComparison.Ordinal))
            .ToArray();
        Assert.That(factories.SelectMany(method => method.GetParameters()).Any(parameter =>
            parameter.ParameterType == typeof(byte) ||
            parameter.ParameterType == typeof(short) ||
            parameter.ParameterType == typeof(int) ||
            parameter.ParameterType == typeof(long) ||
            parameter.ParameterType == typeof(ushort) ||
            parameter.ParameterType == typeof(uint) ||
            parameter.ParameterType == typeof(ulong) ||
            parameter.ParameterType == typeof(string)), Is.False);
    }

    static SecureRandomGenerator Create()
    {
        return SecureRandomGenerator.CreateHmacDrbg(Sequence(0x00, 32), Sequence(0x20, 16), Sequence(0x30, 11));
    }

    static HmacDrbgSha256Engine CreateEngine()
    {
        return new HmacDrbgSha256Engine(Sequence(0x00, 32), Sequence(0x20, 16), Sequence(0x30, 11));
    }

    static HmacDrbgSha256Engine HmacEngine(SecureRandomGenerator generator)
    {
        FieldInfo field = ReflectionInfo.Required<FieldInfo>(typeof(SecureRandomGenerator).GetField("hmacDrbg", BindingFlags.Instance | BindingFlags.NonPublic));
        return ReflectionInfo.Required<HmacDrbgSha256Engine>(field.GetValue(generator));
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
