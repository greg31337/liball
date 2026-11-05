using System.Reflection;
using System.Security.Cryptography;

namespace random.Tests;

public class HmacDrbgSha256EngineTests
{
    [Test]
    public void InstantiateMatchesIndependentUpdateCalculation()
    {
        byte[] entropy = Sequence(0x00, 32);
        byte[] nonce = Sequence(0x20, 16);
        byte[] personalization = Sequence(0x30, 11);
        byte[] key = new byte[32];
        byte[] value = Enumerable.Repeat((byte)0x01, 32).ToArray();
        byte[][] expected = ReferenceUpdate(key, value, Combine(entropy, nonce, personalization));

        HmacDrbgSha256Engine engine = new HmacDrbgSha256Engine(entropy, nonce, personalization);

        Assert.Multiple(() =>
        {
            Assert.That(engine.KeyBytes(), Is.EqualTo(expected[0]));
            Assert.That(engine.ValueBytes(), Is.EqualTo(expected[1]));
            Assert.That(engine.ReseedCounter(), Is.EqualTo(1UL));
            Assert.That(engine.IsInstantiated(), Is.True);
            Assert.That(entropy, Is.EqualTo(Sequence(0x00, 32)));
            Assert.That(nonce, Is.EqualTo(Sequence(0x20, 16)));
            Assert.That(personalization, Is.EqualTo(Sequence(0x30, 11)));
        });
    }

    [Test]
    public void UpdateUsesOneRoundForEmptyDataAndTwoForProvidedData()
    {
        HmacDrbgSha256Engine emptyEngine = Create();
        HmacDrbgSha256Engine dataEngine = Create();
        byte[] emptyKey = emptyEngine.KeyBytes();
        byte[] emptyValue = emptyEngine.ValueBytes();
        byte[] dataKey = dataEngine.KeyBytes();
        byte[] dataValue = dataEngine.ValueBytes();
        byte[] data = Sequence(0xA0, 19);
        byte[][] expectedEmpty = ReferenceUpdate(emptyKey, emptyValue, new byte[0]);
        byte[][] expectedData = ReferenceUpdate(dataKey, dataValue, data);

        emptyEngine.Update(new byte[0]);
        dataEngine.Update(data);

        Assert.Multiple(() =>
        {
            Assert.That(emptyEngine.KeyBytes(), Is.EqualTo(expectedEmpty[0]));
            Assert.That(emptyEngine.ValueBytes(), Is.EqualTo(expectedEmpty[1]));
            Assert.That(dataEngine.KeyBytes(), Is.EqualTo(expectedData[0]));
            Assert.That(dataEngine.ValueBytes(), Is.EqualTo(expectedData[1]));
        });
    }

    [Test]
    public void GenerateMatchesIndependentMultiBlockCalculation()
    {
        HmacDrbgSha256Engine engine = Create();
        byte[] key = engine.KeyBytes();
        byte[] value = engine.ValueBytes();
        byte[] expectedOutput = new byte[77];
        int offset = 0;
        while (offset < expectedOutput.Length)
        {
            value = HMACSHA256.HashData(key, value);
            int count = Math.Min(32, expectedOutput.Length - offset);
            Array.Copy(value, 0, expectedOutput, offset, count);
            offset += count;
        }

        byte[][] expectedState = ReferenceUpdate(key, value, new byte[0]);
        byte[] actual = new byte[77];

        engine.Generate(actual);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expectedOutput));
            Assert.That(engine.KeyBytes(), Is.EqualTo(expectedState[0]));
            Assert.That(engine.ValueBytes(), Is.EqualTo(expectedState[1]));
            Assert.That(engine.ReseedCounter(), Is.EqualTo(2UL));
        });
    }

    [Test]
    public void GenerateAppliesAdditionalInputBeforeAndAfterOutput()
    {
        HmacDrbgSha256Engine engine = Create();
        byte[] additionalInput = Sequence(0xC0, 23);
        byte[][] preState = ReferenceUpdate(engine.KeyBytes(), engine.ValueBytes(), additionalInput);
        byte[] expectedOutput = HMACSHA256.HashData(preState[0], preState[1]);
        byte[][] expectedState = ReferenceUpdate(preState[0], expectedOutput, additionalInput);
        byte[] actual = new byte[32];

        engine.GenerateWithAdditionalInput(actual, additionalInput);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expectedOutput));
            Assert.That(engine.KeyBytes(), Is.EqualTo(expectedState[0]));
            Assert.That(engine.ValueBytes(), Is.EqualTo(expectedState[1]));
            Assert.That(engine.ReseedCounter(), Is.EqualTo(2UL));
        });
    }

    [Test]
    public void ReseedUpdatesStateAndResetsCounter()
    {
        HmacDrbgSha256Engine engine = Create();
        byte[] discarded = new byte[9];
        engine.Generate(discarded);
        byte[] entropy = Sequence(0x50, 32);
        byte[] additionalInput = Sequence(0x80, 17);
        byte[][] expected = ReferenceUpdate(engine.KeyBytes(), engine.ValueBytes(), Combine(entropy, additionalInput));

        engine.Reseed(entropy, additionalInput);

        Assert.Multiple(() =>
        {
            Assert.That(engine.KeyBytes(), Is.EqualTo(expected[0]));
            Assert.That(engine.ValueBytes(), Is.EqualTo(expected[1]));
            Assert.That(engine.ReseedCounter(), Is.EqualTo(1UL));
        });
    }

    [Test]
    public void EmptyGenerateDoesNotChangeState()
    {
        HmacDrbgSha256Engine engine = Create();
        byte[] key = engine.KeyBytes();
        byte[] value = engine.ValueBytes();
        ulong counter = engine.ReseedCounter();

        engine.GenerateWithAdditionalInput(Span<byte>.Empty, Sequence(0x90, 7));

        Assert.Multiple(() =>
        {
            Assert.That(engine.KeyBytes(), Is.EqualTo(key));
            Assert.That(engine.ValueBytes(), Is.EqualTo(value));
            Assert.That(engine.ReseedCounter(), Is.EqualTo(counter));
        });
    }

    [Test]
    public void UninstantiateZeroesRetiredStateAndDisablesOperations()
    {
        HmacDrbgSha256Engine engine = Create();
        FieldInfo keyField = ReflectionInfo.Required<FieldInfo>(typeof(HmacDrbgSha256Engine).GetField("key", BindingFlags.Instance | BindingFlags.NonPublic));
        FieldInfo valueField = ReflectionInfo.Required<FieldInfo>(typeof(HmacDrbgSha256Engine).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic));
        byte[] retiredKey = ReflectionInfo.Required<byte[]>(keyField.GetValue(engine));
        byte[] retiredValue = ReflectionInfo.Required<byte[]>(valueField.GetValue(engine));

        engine.Uninstantiate();

        Assert.Multiple(() =>
        {
            Assert.That(retiredKey, Is.All.EqualTo(0));
            Assert.That(retiredValue, Is.All.EqualTo(0));
            Assert.That(engine.KeyBytes(), Is.Empty);
            Assert.That(engine.ValueBytes(), Is.Empty);
            Assert.That(engine.ReseedCounter(), Is.EqualTo(0UL));
            Assert.That(engine.IsInstantiated(), Is.False);
            Assert.Throws<RandomSecurityException>(() => engine.Generate(new byte[1]));
            Assert.Throws<RandomSecurityException>(() => engine.Reseed(new byte[1], new byte[0]));
            Assert.DoesNotThrow(() => engine.Uninstantiate());
        });
    }

    [Test]
    public void MetadataExposesOnlySecureStateMachineCapabilities()
    {
        RandomAlgorithmInfo info = Create().Info();

        Assert.Multiple(() =>
        {
            Assert.That(info.Algorithm(), Is.EqualTo(RandomAlgorithm.HmacDrbgSha256V1));
            Assert.That(info.Version(), Is.EqualTo(HmacDrbgSha256Engine.AlgorithmVersion));
            Assert.That(info.StateBytes(), Is.EqualTo(0));
            Assert.That(info.IsCryptographic(), Is.True);
            Assert.That(info.Capabilities(), Is.EqualTo(new[]
            {
                RandomCapability.Reseed,
                RandomCapability.AdditionalInput,
                RandomCapability.PredictionResistance,
                RandomCapability.Uninstantiate
            }));
        });
    }

    static HmacDrbgSha256Engine Create()
    {
        return new HmacDrbgSha256Engine(Sequence(0x00, 32), Sequence(0x20, 16), new byte[0]);
    }

    static byte[][] ReferenceUpdate(byte[] key, byte[] value, byte[] providedData)
    {
        byte[] nextKey = HMACSHA256.HashData(key, Combine(value, new byte[] { 0x00 }, providedData));
        byte[] nextValue = HMACSHA256.HashData(nextKey, value);
        if (providedData.Length == 0)
        {
            return [nextKey, nextValue];
        }

        nextKey = HMACSHA256.HashData(nextKey, Combine(nextValue, new byte[] { 0x01 }, providedData));
        nextValue = HMACSHA256.HashData(nextKey, nextValue);
        return [nextKey, nextValue];
    }

    static byte[] Combine(params byte[][] values)
    {
        int length = values.Sum(value => value.Length);
        byte[] result = new byte[length];
        int offset = 0;
        foreach (byte[] value in values)
        {
            Array.Copy(value, 0, result, offset, value.Length);
            offset += value.Length;
        }

        return result;
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
