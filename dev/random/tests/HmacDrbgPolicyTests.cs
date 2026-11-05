using System.Reflection;

namespace random.Tests;

public class HmacDrbgPolicyTests
{
    [Test]
    public void Sha256ProfileExposesTheRequiredSecurityLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HmacDrbgSha256Engine.SecurityStrengthBits, Is.EqualTo(256));
            Assert.That(HmacDrbgSha256Engine.MinimumEntropyBytes, Is.EqualTo(32));
            Assert.That(HmacDrbgSha256Engine.MinimumNonceBytes, Is.EqualTo(16));
            Assert.That(HmacDrbgSha256Engine.MaximumInputBytes, Is.EqualTo(4294967296L));
            Assert.That(HmacDrbgSha256Engine.MaximumRequestBytes, Is.EqualTo(65536));
            Assert.That(HmacDrbgSha256Engine.ReseedInterval, Is.EqualTo(281474976710656UL));
        });
    }

    [Test]
    public void InstantiationEnforcesEntropyAndNonceStrengthBeforeCreatingState()
    {
        RandomArgumentException entropyError = Assert.Throws<RandomArgumentException>(() =>
            new HmacDrbgSha256Engine(new byte[31], new byte[16], new byte[0]));
        RandomArgumentException nonceError = Assert.Throws<RandomArgumentException>(() =>
            new HmacDrbgSha256Engine(new byte[32], new byte[15], new byte[0]));

        Assert.Multiple(() =>
        {
            Assert.That(entropyError.ArgumentName(), Is.EqualTo("entropy"));
            Assert.That(nonceError.ArgumentName(), Is.EqualTo("nonce"));
            Assert.DoesNotThrow(() => new HmacDrbgSha256Engine(new byte[32], new byte[16], new byte[0]));
        });
    }

    [Test]
    public void InputLengthValidatorsEnforceNistMaximumsWithoutLargeAllocations()
    {
        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => HmacDrbgSha256Engine.ValidateEntropyLength(4294967296L, "entropy"));
            Assert.DoesNotThrow(() => HmacDrbgSha256Engine.ValidateNonceLength(4294967296L, "nonce"));
            Assert.DoesNotThrow(() => HmacDrbgSha256Engine.ValidateOptionalInputLength(4294967296L, "additionalInput"));
            Assert.Throws<RandomArgumentException>(() => HmacDrbgSha256Engine.ValidateEntropyLength(4294967297L, "entropy"));
            Assert.Throws<RandomArgumentException>(() => HmacDrbgSha256Engine.ValidateNonceLength(4294967297L, "nonce"));
            Assert.Throws<RandomArgumentException>(() => HmacDrbgSha256Engine.ValidateOptionalInputLength(4294967297L, "additionalInput"));
        });
    }

    [Test]
    public void GenerateEnforcesRequestLimitBeforeMutatingStateOrDestination()
    {
        HmacDrbgSha256Engine engine = Create();
        byte[] key = engine.KeyBytes();
        byte[] value = engine.ValueBytes();
        byte[] destination = Enumerable.Repeat((byte)0xA5, 65537).ToArray();

        RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => engine.Generate(destination));

        Assert.Multiple(() =>
        {
            Assert.That(error.ArgumentName(), Is.EqualTo("destination"));
            Assert.That(destination, Is.All.EqualTo(0xA5));
            Assert.That(engine.KeyBytes(), Is.EqualTo(key));
            Assert.That(engine.ValueBytes(), Is.EqualTo(value));
            Assert.That(engine.ReseedCounter(), Is.EqualTo(1UL));
        });

        Assert.DoesNotThrow(() => engine.Generate(new byte[65536]));
    }

    [Test]
    public void InvalidReseedIsAtomicAndSuccessfulReseedPreservesCallerInputs()
    {
        HmacDrbgSha256Engine engine = Create();
        byte[] key = engine.KeyBytes();
        byte[] value = engine.ValueBytes();

        RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => engine.Reseed(new byte[31], new byte[0]));

        Assert.Multiple(() =>
        {
            Assert.That(error.ArgumentName(), Is.EqualTo("entropy"));
            Assert.That(engine.KeyBytes(), Is.EqualTo(key));
            Assert.That(engine.ValueBytes(), Is.EqualTo(value));
            Assert.That(engine.ReseedCounter(), Is.EqualTo(1UL));
        });

        byte[] entropy = Sequence(0x60, 32);
        byte[] additionalInput = Sequence(0x90, 19);
        byte[] expectedEntropy = (byte[])entropy.Clone();
        byte[] expectedAdditionalInput = (byte[])additionalInput.Clone();
        engine.Reseed(entropy, additionalInput);

        Assert.Multiple(() =>
        {
            Assert.That(entropy, Is.EqualTo(expectedEntropy));
            Assert.That(additionalInput, Is.EqualTo(expectedAdditionalInput));
        });
    }

    [Test]
    public void ReseedIntervalAllowsItsBoundaryThenFailsAtomically()
    {
        HmacDrbgSha256Engine engine = Create();
        SetCounter(engine, HmacDrbgSha256Engine.ReseedInterval);
        engine.Generate(new byte[1]);
        byte[] key = engine.KeyBytes();
        byte[] value = engine.ValueBytes();
        byte[] destination = new byte[] { 0xA5 };

        Assert.Throws<RandomReseedRequiredException>(() => engine.Generate(destination));

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new byte[] { 0xA5 }));
            Assert.That(engine.KeyBytes(), Is.EqualTo(key));
            Assert.That(engine.ValueBytes(), Is.EqualTo(value));
            Assert.That(engine.ReseedCounter(), Is.EqualTo(HmacDrbgSha256Engine.ReseedInterval + 1UL));
        });
    }

    [Test]
    public void PredictionResistanceReseedsAndConsumesAdditionalInputOnlyDuringReseed()
    {
        HmacDrbgSha256Engine actual = Create();
        HmacDrbgSha256Engine expected = Create();
        byte[] entropy = Sequence(0x40, 32);
        byte[] additionalInput = Sequence(0x80, 23);
        byte[] expectedEntropy = (byte[])entropy.Clone();
        byte[] expectedAdditionalInput = (byte[])additionalInput.Clone();
        byte[] actualOutput = new byte[73];
        byte[] expectedOutput = new byte[73];

        actual.GeneratePredictionResistant(actualOutput, additionalInput, entropy);
        expected.Reseed(entropy, additionalInput);
        expected.GenerateWithAdditionalInput(expectedOutput, new byte[0]);

        Assert.Multiple(() =>
        {
            Assert.That(actualOutput, Is.EqualTo(expectedOutput));
            Assert.That(actual.KeyBytes(), Is.EqualTo(expected.KeyBytes()));
            Assert.That(actual.ValueBytes(), Is.EqualTo(expected.ValueBytes()));
            Assert.That(actual.ReseedCounter(), Is.EqualTo(2UL));
            Assert.That(entropy, Is.EqualTo(expectedEntropy));
            Assert.That(additionalInput, Is.EqualTo(expectedAdditionalInput));
        });
    }

    [Test]
    public void PredictionResistanceCanUseOsEntropyAndFalseUsesOrdinaryGeneration()
    {
        HmacDrbgSha256Engine resistant = Create();
        HmacDrbgSha256Engine ordinary = Create();
        HmacDrbgSha256Engine expected = Create();
        byte[] resistantOutput = new byte[32];
        byte[] ordinaryOutput = new byte[32];
        byte[] expectedOutput = new byte[32];
        byte[] additionalInput = Sequence(0xB0, 9);

        resistant.GenerateWithOptions(resistantOutput, additionalInput, true);
        ordinary.GenerateWithOptions(ordinaryOutput, additionalInput, false);
        expected.GenerateWithAdditionalInput(expectedOutput, additionalInput);

        Assert.Multiple(() =>
        {
            Assert.That(resistant.ReseedCounter(), Is.EqualTo(2UL));
            Assert.That(resistantOutput, Is.Not.All.EqualTo(0));
            Assert.That(ordinaryOutput, Is.EqualTo(expectedOutput));
            Assert.That(ordinary.KeyBytes(), Is.EqualTo(expected.KeyBytes()));
            Assert.That(ordinary.ValueBytes(), Is.EqualTo(expected.ValueBytes()));
        });
    }

    [Test]
    public void ReplacedStateArraysAreZeroed()
    {
        HmacDrbgSha256Engine engine = Create();
        FieldInfo keyField = ReflectionInfo.Required<FieldInfo>(typeof(HmacDrbgSha256Engine).GetField("key", BindingFlags.Instance | BindingFlags.NonPublic));
        FieldInfo valueField = ReflectionInfo.Required<FieldInfo>(typeof(HmacDrbgSha256Engine).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic));
        byte[] retiredKey = ReflectionInfo.Required<byte[]>(keyField.GetValue(engine));
        byte[] retiredValue = ReflectionInfo.Required<byte[]>(valueField.GetValue(engine));

        engine.Update(Sequence(0xD0, 13));

        Assert.Multiple(() =>
        {
            Assert.That(retiredKey, Is.All.EqualTo(0));
            Assert.That(retiredValue, Is.All.EqualTo(0));
            Assert.That(engine.KeyBytes(), Is.Not.All.EqualTo(0));
            Assert.That(engine.ValueBytes(), Is.Not.All.EqualTo(0));
        });
    }

    static HmacDrbgSha256Engine Create()
    {
        return new HmacDrbgSha256Engine(Sequence(0x00, 32), Sequence(0x20, 16), new byte[0]);
    }

    static void SetCounter(HmacDrbgSha256Engine engine, ulong counter)
    {
        FieldInfo field = ReflectionInfo.Required<FieldInfo>(typeof(HmacDrbgSha256Engine).GetField("reseedCounter", BindingFlags.Instance | BindingFlags.NonPublic));
        field.SetValue(engine, counter);
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
