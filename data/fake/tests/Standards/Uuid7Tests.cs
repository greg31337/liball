using fake;

namespace fake.Tests;

[TestFixture]
public class Uuid7Tests
{
    [Test]
    public void Rfc9562AppendixA6VectorMatchesPublishedFields()
    {
        byte[] value = Convert.FromHexString("0000000000000CC358C4DC0C0C07398F");
        IdentifierProvider identifiers = Fake.Create(4601).Identifier();

        UuidGenerator.ApplyVersion7(value, 1645557742000L);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(value), Is.EqualTo("017F22E279B07CC398C4DC0C0C07398F"));
            Assert.That(identifiers.FormatUuid(value), Is.EqualTo("017f22e2-79b0-7cc3-98c4-dc0c0c07398f"));
            Assert.That(identifiers.ValidateUuid7FromValueUsingReadOnlySpanOfByte(value), Is.True);
            Assert.That(identifiers.Uuid7UnixMilliseconds(value), Is.EqualTo(1645557742000L));
        });
    }

    [Test]
    public void DefaultGenerationUsesCapturedReferenceTimeAtMillisecondPrecision()
    {
        FakeOptions options = FakeOptions.Create(4602L);
        options.ReferenceTime = new DateTimeOffset(2034, 5, 6, 7, 8, 9, TimeSpan.FromHours(-7)).AddTicks(9876L);
        IdentifierProvider identifiers = Fake.Create(options).Identifier();

        byte[] value = identifiers.Uuid7Bytes();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.Uuid7UnixMilliseconds(value), Is.EqualTo(options.ReferenceTime.ToUnixTimeMilliseconds()));
            Assert.That(identifiers.ValidateUuid7FromValueUsingReadOnlySpanOfByte(value), Is.True);
            Assert.That(value[6] & 0xf0, Is.EqualTo(0x70));
            Assert.That(value[6] & 0x08, Is.Zero);
            Assert.That(value[8] & 0xc0, Is.EqualTo(0x80));
        });
    }

    [Test]
    public void EqualMillisecondGenerationIsStrictlyMonotonicAndDeterministic()
    {
        IdentifierProvider first = Fake.Create(4603).Identifier();
        IdentifierProvider second = Fake.Create(4603).Identifier();
        string previous = "";

        int index = 0;
        while (index < 4096)
        {
            string actual = first.Uuid7();
            string expected = second.Uuid7();
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(first.ValidateUuid7(actual), Is.True);
            if (index > 0)
            {
                Assert.That(string.CompareOrdinal(previous, actual), Is.LessThan(0));
            }
            previous = actual;
            index++;
        }
    }

    [Test]
    public void SameMillisecondCounterIncrementsBeforeRandomTail()
    {
        IdentifierProvider identifiers = Fake.Create(4604).Identifier();
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123L);

        byte[] first = identifiers.Uuid7BytesFromTimestamp(timestamp);
        byte[] second = identifiers.Uuid7BytesFromTimestamp(timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(Counter(second), Is.EqualTo(Counter(first) + 1UL));
            Assert.That(Compare(first, second), Is.LessThan(0));
            Assert.That(identifiers.Uuid7UnixMilliseconds(second), Is.EqualTo(1700000000123L));
        });
    }

    [Test]
    public void RegressedTimestampsClampToLastValueAndContinueMonotonically()
    {
        IdentifierProvider identifiers = Fake.Create(4605).Identifier();
        DateTimeOffset baseline = DateTimeOffset.FromUnixTimeMilliseconds(1700000000000L);

        byte[] first = identifiers.Uuid7BytesFromTimestamp(baseline.AddSeconds(10));
        byte[] rollback = identifiers.Uuid7BytesFromTimestamp(baseline);
        byte[] repeated = identifiers.Uuid7BytesFromTimestamp(baseline.AddSeconds(10));
        byte[] advanced = identifiers.Uuid7BytesFromTimestamp(baseline.AddSeconds(20));

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.Uuid7UnixMilliseconds(first), Is.EqualTo(1700000010000L));
            Assert.That(identifiers.Uuid7UnixMilliseconds(rollback), Is.EqualTo(1700000010000L));
            Assert.That(identifiers.Uuid7UnixMilliseconds(repeated), Is.EqualTo(1700000010000L));
            Assert.That(identifiers.Uuid7UnixMilliseconds(advanced), Is.EqualTo(1700000020000L));
            Assert.That(Compare(first, rollback), Is.LessThan(0));
            Assert.That(Compare(rollback, repeated), Is.LessThan(0));
            Assert.That(Compare(repeated, advanced), Is.LessThan(0));
        });
    }

    [Test]
    public void EquivalentOffsetTimestampsProduceTheSameDeterministicSequence()
    {
        DateTimeOffset utc = new DateTimeOffset(2035, 4, 5, 12, 30, 45, TimeSpan.Zero);
        DateTimeOffset offset = utc.ToOffset(TimeSpan.FromHours(5.5));
        IdentifierProvider first = Fake.Create(4606).Identifier();
        IdentifierProvider second = Fake.Create(4606).Identifier();

        Assert.That(first.Uuid7FromTimestamp(utc), Is.EqualTo(second.Uuid7FromTimestamp(offset)));
    }

    [Test]
    public void VersionSevenTextAndByteApisShareOneMonotonicState()
    {
        IdentifierProvider identifiers = Fake.Create(4607).Identifier();
        char[] textDestination = Enumerable.Repeat('#', 40).ToArray();
        byte[] byteDestination = Enumerable.Repeat((byte)0xee, 20).ToArray();

        string first = identifiers.Uuid7();
        byte[] secondBytes = identifiers.Uuid7Bytes();
        int textWritten = identifiers.WriteUuid7(textDestination);
        int bytesWritten = identifiers.WriteUuid7Bytes(byteDestination);
        string second = identifiers.FormatUuid(secondBytes);
        string third = new string(textDestination, 0, textWritten);
        string fourth = identifiers.FormatUuidFromValue(byteDestination.AsSpan(0, bytesWritten));

        Assert.Multiple(() =>
        {
            Assert.That(string.CompareOrdinal(first, second), Is.LessThan(0));
            Assert.That(string.CompareOrdinal(second, third), Is.LessThan(0));
            Assert.That(string.CompareOrdinal(third, fourth), Is.LessThan(0));
            Assert.That(textWritten, Is.EqualTo(36));
            Assert.That(bytesWritten, Is.EqualTo(16));
            Assert.That(textDestination.Skip(36), Has.All.EqualTo('#'));
            Assert.That(byteDestination.Skip(16), Has.All.EqualTo(0xee));
        });
    }

    [Test]
    public void ExplicitTimestampSpanOverloadsWriteOnlyRequiredSlices()
    {
        IdentifierProvider textProvider = Fake.Create(4608).Identifier();
        IdentifierProvider expectedTextProvider = Fake.Create(4608).Identifier();
        IdentifierProvider byteProvider = Fake.Create(4609).Identifier();
        IdentifierProvider expectedByteProvider = Fake.Create(4609).Identifier();
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1800000000123L);
        char[] textDestination = Enumerable.Repeat('#', 40).ToArray();
        byte[] byteDestination = Enumerable.Repeat((byte)0xee, 20).ToArray();

        int textWritten = textProvider.WriteUuid7FromDestinationAndTimestamp(textDestination, timestamp);
        int bytesWritten = byteProvider.WriteUuid7BytesFromDestinationAndTimestamp(byteDestination, timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(textWritten, Is.EqualTo(36));
            Assert.That(new string(textDestination, 0, textWritten), Is.EqualTo(expectedTextProvider.Uuid7FromTimestamp(timestamp)));
            Assert.That(textDestination.Skip(textWritten), Has.All.EqualTo('#'));
            Assert.That(bytesWritten, Is.EqualTo(16));
            Assert.That(byteDestination.Take(bytesWritten), Is.EqualTo(expectedByteProvider.Uuid7BytesFromTimestamp(timestamp)));
            Assert.That(byteDestination.Skip(bytesWritten), Has.All.EqualTo(0xee));
        });
    }

    [Test]
    public void TooSmallDestinationsDoNotAdvanceStateOrConsumeEntropy()
    {
        IdentifierProvider actual = Fake.Create(4610).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(4610).Identifier();
        char[] textDestination = Enumerable.Repeat('#', 35).ToArray();
        byte[] byteDestination = Enumerable.Repeat((byte)0xee, 15).ToArray();

        ArgumentException textException = Assert.Throws<ArgumentException>(() => actual.WriteUuid7(textDestination));
        ArgumentException byteException = Assert.Throws<ArgumentException>(() => actual.WriteUuid7Bytes(byteDestination));

        Assert.Multiple(() =>
        {
            Assert.That(textException.ParamName, Is.EqualTo("destination"));
            Assert.That(byteException.ParamName, Is.EqualTo("destination"));
            Assert.That(textDestination, Has.All.EqualTo('#'));
            Assert.That(byteDestination, Has.All.EqualTo(0xee));
            Assert.That(actual.Uuid7Bytes(), Is.EqualTo(expectedPosition.Uuid7Bytes()));
        });
    }

    [Test]
    public void TimestampValidationRejectsPreEpochValuesWithoutAdvancingState()
    {
        IdentifierProvider actual = Fake.Create(4611).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(4611).Identifier();
        DateTimeOffset invalid = DateTimeOffset.UnixEpoch.AddTicks(-1L);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => actual.Uuid7FromTimestamp(invalid));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ParamName, Is.EqualTo("timestamp"));
            Assert.That(actual.Uuid7Bytes(), Is.EqualTo(expectedPosition.Uuid7Bytes()));
        });
    }

    [Test]
    public void FullUnsignedFortyEightBitTimestampBoundaryIsPreserved()
    {
        byte[] maximum = new byte[16];
        byte[] overflow = new byte[16];

        UuidGenerator.ApplyVersion7(maximum, UuidVersion7State.MaximumUnixMilliseconds);
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => UuidGenerator.ApplyVersion7(overflow, UuidVersion7State.MaximumUnixMilliseconds + 1L));

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(maximum.AsSpan(0, 6)), Is.EqualTo("FFFFFFFFFFFF"));
            Assert.That(UuidGenerator.Version7UnixMilliseconds(maximum), Is.EqualTo(UuidVersion7State.MaximumUnixMilliseconds));
            Assert.That(exception.ParamName, Is.EqualTo("timestamp"));
        });
    }

    [Test]
    public void CounterRolloverIsDetectedInsteadOfReturningADuplicate()
    {
        byte[] value = Convert.FromHexString("0000000000007FFFBFFFFFFFFFFFFFFF");

        bool incremented = UuidGenerator.IncrementVersion7Counter(value);

        Assert.Multiple(() =>
        {
            Assert.That(incremented, Is.False);
            Assert.That(Convert.ToHexString(value.AsSpan(6, 6)), Is.EqualTo("700080000000"));
        });
    }

    [Test]
    public void ValidationRejectsWrongVersionVariantLengthAndNull()
    {
        IdentifierProvider identifiers = Fake.Create(4612).Identifier();
        string valid = "017f22e2-79b0-7cc3-98c4-dc0c0c07398f";
        string wrongVersion = "017f22e2-79b0-4cc3-98c4-dc0c0c07398f";
        string wrongVariant = "017f22e2-79b0-7cc3-78c4-dc0c0c07398f";

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.ValidateUuid7(valid), Is.True);
            Assert.That(identifiers.ValidateUuid7(wrongVersion), Is.False);
            Assert.That(identifiers.ValidateUuid7(wrongVariant), Is.False);
            Assert.That(identifiers.ValidateUuid7("not-a-uuid"), Is.False);
            Assert.That(identifiers.ValidateUuid7FromValueUsingReadOnlySpanOfByte(new byte[15]), Is.False);
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateUuid7((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.Uuid7UnixMilliseconds(identifiers.ParseUuid(wrongVersion))).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void OutputLimitFailuresDoNotAdvanceVersionSevenState()
    {
        FakeOptions options = FakeOptions.Create(4613L);
        options.MaximumOutputSize = 20;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(options).Identifier();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => actual.Uuid7());

        Assert.Multiple(() =>
        {
            Assert.That(exception.ParamName, Is.EqualTo("value"));
            Assert.That(actual.Uuid7Bytes(), Is.EqualTo(expectedPosition.Uuid7Bytes()));
        });
    }

    [Test]
    public void CryptographicModeRetainsStructureAndMonotonicOrdering()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();
        byte[] previous = identifiers.Uuid7Bytes();

        int index = 0;
        while (index < 128)
        {
            byte[] current = identifiers.Uuid7Bytes();
            Assert.That(identifiers.ValidateUuid7FromValueUsingReadOnlySpanOfByte(current), Is.True);
            Assert.That(Compare(previous, current), Is.LessThan(0));
            previous = current;
            index++;
        }
    }

    static ulong Counter(byte[] value)
    {
        return ((ulong)(value[6] & 15) << 38)
            | ((ulong)value[7] << 30)
            | ((ulong)(value[8] & 63) << 24)
            | ((ulong)value[9] << 16)
            | ((ulong)value[10] << 8)
            | value[11];
    }

    static int Compare(byte[] left, byte[] right)
    {
        int index = 0;
        while (index < left.Length)
        {
            int comparison = left[index].CompareTo(right[index]);
            if (comparison != 0)
            {
                return comparison;
            }
            index++;
        }
        return 0;
    }
}
