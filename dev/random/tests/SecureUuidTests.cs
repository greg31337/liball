using System.Reflection;

namespace random.Tests;

public class SecureUuidTests
{
    [Test]
    public void Version4MatchesRfc9562AppendixVectorInNetworkByteOrder()
    {
        byte[] randomBytes = Convert.FromHexString("919108F752D133205BACF847DB4148A8");
        byte[] expectedRandomBytes = (byte[])randomBytes.Clone();

        Guid value = SecureUuid.CreateVersion4(randomBytes);

        Assert.Multiple(() =>
        {
            Assert.That(value.ToString("D"), Is.EqualTo("919108f7-52d1-4320-9bac-f847db4148a8"));
            Assert.That(Convert.ToHexString(SecureUuid.NetworkBytes(value)), Is.EqualTo("919108F752D143209BACF847DB4148A8"));
            Assert.That(randomBytes, Is.EqualTo(expectedRandomBytes));
        });
    }

    [Test]
    public void Version7MatchesRfc9562AppendixVectorInNetworkByteOrder()
    {
        byte[] randomBytes = Convert.FromHexString("0000000000000CC358C4DC0C0C07398F");

        Guid value = SecureUuid.CreateVersion7(randomBytes, 1645557742000L, 0xCC3);

        Assert.Multiple(() =>
        {
            Assert.That(value.ToString("D"), Is.EqualTo("017f22e2-79b0-7cc3-98c4-dc0c0c07398f"));
            Assert.That(Convert.ToHexString(SecureUuid.NetworkBytes(value)), Is.EqualTo("017F22E279B07CC398C4DC0C0C07398F"));
        });
    }

    [Test]
    public void Version4ConsumesExactlyOneSixteenByteSecureRequest()
    {
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();
        byte[] randomBytes = reference.NextBytes(16);
        Guid expected = SecureUuid.CreateVersion4(randomBytes);

        Guid value = actual.Uuid4();
        byte[] bytes = SecureUuid.NetworkBytes(value);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(expected));
            Assert.That(bytes[6] >> 4, Is.EqualTo(4));
            Assert.That(bytes[8] >> 6, Is.EqualTo(2));
        });
    }

    [Test]
    public void Version7SeedsThenIncrementsCounterAcrossSameTimeAndRollback()
    {
        const long timestamp = 1700000000123L;
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();

        byte[] firstRandom = reference.NextBytes(16);
        int firstCounter = SecureUuid.SeedCounter(firstRandom);
        Guid expectedFirst = SecureUuid.CreateVersion7(firstRandom, timestamp, firstCounter);
        Guid first = actual.Uuid7AtUnixMilliseconds(timestamp);

        byte[] secondRandom = reference.NextBytes(16);
        Guid expectedSecond = SecureUuid.CreateVersion7(secondRandom, timestamp, firstCounter + 1);
        Guid second = actual.Uuid7AtUnixMilliseconds(timestamp);

        byte[] rollbackRandom = reference.NextBytes(16);
        Guid expectedRollback = SecureUuid.CreateVersion7(rollbackRandom, timestamp, firstCounter + 2);
        Guid rollback = actual.Uuid7AtUnixMilliseconds(timestamp - 1000L);

        byte[] advancedRandom = reference.NextBytes(16);
        int advancedCounter = SecureUuid.SeedCounter(advancedRandom);
        Guid expectedAdvanced = SecureUuid.CreateVersion7(advancedRandom, timestamp + 1L, advancedCounter);
        Guid advanced = actual.Uuid7AtUnixMilliseconds(timestamp + 1L);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(expectedFirst));
            Assert.That(second, Is.EqualTo(expectedSecond));
            Assert.That(rollback, Is.EqualTo(expectedRollback));
            Assert.That(advanced, Is.EqualTo(expectedAdvanced));
            Assert.That(Compare(first, second), Is.LessThan(0));
            Assert.That(Compare(second, rollback), Is.LessThan(0));
            Assert.That(Compare(rollback, advanced), Is.LessThan(0));
        });
    }

    [Test]
    public void SameMillisecondBatchIsStrictlyMonotonicWithSequentialCounters()
    {
        const long timestamp = 1700000000123L;
        SecureRandomGenerator generator = Create();
        Guid previous = generator.Uuid7AtUnixMilliseconds(timestamp);
        int previousCounter = Counter(previous);

        for (int index = 0; index < 100; index++)
        {
            Guid current = generator.Uuid7AtUnixMilliseconds(timestamp);
            Assert.Multiple(() =>
            {
                Assert.That(Timestamp(current), Is.EqualTo(timestamp));
                Assert.That(Counter(current), Is.EqualTo(previousCounter + 1));
                Assert.That(Compare(previous, current), Is.LessThan(0));
                Assert.That(Version(current), Is.EqualTo(7));
                Assert.That(Variant(current), Is.EqualTo(2));
            });
            previous = current;
            previousCounter++;
        }
    }

    [Test]
    public void CounterRolloverAdvancesLogicalTimestampAndReseedsCounter()
    {
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();
        SetUuid7State(actual, 100L, SecureUuid.MaximumCounter);
        byte[] randomBytes = reference.NextBytes(16);
        int expectedCounter = SecureUuid.SeedCounter(randomBytes);
        Guid expected = SecureUuid.CreateVersion7(randomBytes, 101L, expectedCounter);

        Guid value = actual.Uuid7AtUnixMilliseconds(100L);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(expected));
            Assert.That(Timestamp(value), Is.EqualTo(101L));
            Assert.That(Counter(value), Is.EqualTo(expectedCounter));
        });

        Guid next = actual.Uuid7AtUnixMilliseconds(100L);
        Assert.Multiple(() =>
        {
            Assert.That(Timestamp(next), Is.EqualTo(101L));
            Assert.That(Counter(next), Is.EqualTo(expectedCounter + 1));
            Assert.That(Compare(value, next), Is.LessThan(0));
        });
    }

    [Test]
    public void InvalidOrFullyExhaustedTimestampsFailWithoutConsumingEntropy()
    {
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();

        RandomArgumentException negative = Assert.Throws<RandomArgumentException>(() => actual.Uuid7AtUnixMilliseconds(-1L));
        RandomArgumentException excessive = Assert.Throws<RandomArgumentException>(() => actual.Uuid7AtUnixMilliseconds(SecureUuid.MaximumUnixMilliseconds + 1L));
        SetUuid7State(actual, SecureUuid.MaximumUnixMilliseconds, SecureUuid.MaximumCounter);
        Assert.Throws<RandomExhaustedException>(() => actual.Uuid7AtUnixMilliseconds(SecureUuid.MaximumUnixMilliseconds));

        Assert.Multiple(() =>
        {
            Assert.That(negative.ArgumentName(), Is.EqualTo("unixMilliseconds"));
            Assert.That(excessive.ArgumentName(), Is.EqualTo("unixMilliseconds"));
            Assert.That(actual.Uuid4(), Is.EqualTo(reference.Uuid4()));
        });
    }

    [Test]
    public void PublicVersion7UsesCurrentUnixMillisecondsAndRequiredMasks()
    {
        SecureRandomGenerator generator = SecureRandomGenerator.CreateSystem();
        long before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Guid value = generator.Uuid7();
        long after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Assert.Multiple(() =>
        {
            Assert.That(Timestamp(value), Is.InRange(before, after));
            Assert.That(Version(value), Is.EqualTo(7));
            Assert.That(Variant(value), Is.EqualTo(2));
        });
    }

    [Test]
    public void LayoutHelpersRejectInvalidInputsWithoutChangingCallerBytes()
    {
        byte[] bytes = new byte[16];
        byte[] expected = (byte[])bytes.Clone();

        Assert.Multiple(() =>
        {
            Assert.Throws<RandomArgumentException>(() => SecureUuid.CreateVersion4(NullReference.Value<byte[]>()));
            Assert.Throws<RandomArgumentException>(() => SecureUuid.CreateVersion4(new byte[15]));
            Assert.Throws<RandomArgumentException>(() => SecureUuid.CreateVersion7(bytes, 0L, -1));
            Assert.Throws<RandomArgumentException>(() => SecureUuid.CreateVersion7(bytes, 0L, 4096));
            Assert.That(bytes, Is.EqualTo(expected));
        });
    }

    static int Version(Guid value)
    {
        return SecureUuid.NetworkBytes(value)[6] >> 4;
    }

    static int Variant(Guid value)
    {
        return SecureUuid.NetworkBytes(value)[8] >> 6;
    }

    static int Counter(Guid value)
    {
        byte[] bytes = SecureUuid.NetworkBytes(value);
        return ((bytes[6] & 0x0F) << 8) | bytes[7];
    }

    static long Timestamp(Guid value)
    {
        byte[] bytes = SecureUuid.NetworkBytes(value);
        long result = 0L;
        for (int index = 0; index < 6; index++)
        {
            result = (result << 8) | bytes[index];
        }

        return result;
    }

    static int Compare(Guid first, Guid second)
    {
        byte[] firstBytes = SecureUuid.NetworkBytes(first);
        byte[] secondBytes = SecureUuid.NetworkBytes(second);
        for (int index = 0; index < firstBytes.Length; index++)
        {
            int comparison = firstBytes[index].CompareTo(secondBytes[index]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    static void SetUuid7State(SecureRandomGenerator generator, long timestamp, int counter)
    {
        Type type = typeof(SecureRandomGenerator);
        ReflectionInfo.Required<FieldInfo>(type.GetField("uuid7Timestamp", BindingFlags.Instance | BindingFlags.NonPublic)).SetValue(generator, timestamp);
        ReflectionInfo.Required<FieldInfo>(type.GetField("uuid7Counter", BindingFlags.Instance | BindingFlags.NonPublic)).SetValue(generator, counter);
        ReflectionInfo.Required<FieldInfo>(type.GetField("uuid7Initialized", BindingFlags.Instance | BindingFlags.NonPublic)).SetValue(generator, true);
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
