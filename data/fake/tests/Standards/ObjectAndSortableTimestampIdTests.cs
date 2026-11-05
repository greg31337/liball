using fake;

namespace fake.Tests;

[TestFixture]
public class ObjectAndSortableTimestampIdTests
{
    [TestCase(0L, "00000000")]
    [TestCase(2147483647L, "7fffffff")]
    [TestCase(2147483648L, "80000000")]
    [TestCase(4294967295L, "ffffffff")]
    public void ObjectIdTimestampUsesUnsignedBigEndianSeconds(long seconds, string expected)
    {
        byte[] value = new byte[ObjectIdGenerator.ByteLength];

        ObjectIdGenerator.ApplyTimestamp(value, (uint)seconds);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(value.AsSpan(0, 4)).ToLowerInvariant(), Is.EqualTo(expected));
            Assert.That(ObjectIdGenerator.UnixSeconds(value), Is.EqualTo(seconds));
        });
    }

    [Test]
    public void ObjectIdFieldsAndCounterWrapMatchTheDocumentedShape()
    {
        ObjectIdState state = new ObjectIdState();
        state.Initialized = true;
        state.ProcessValue = Convert.FromHexString("1122334455");
        state.CounterValue = 16777215;
        FakeRandom random = Fake.Create(4901).Identifier().RandomValue;
        byte[] first = new byte[12];
        byte[] second = new byte[12];

        state.Next(random, 0x01020304U, first);
        state.Next(random, 0x01020304U, second);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(first).ToLowerInvariant(), Is.EqualTo("010203041122334455ffffff"));
            Assert.That(Convert.ToHexString(second).ToLowerInvariant(), Is.EqualTo("010203041122334455000000"));
            Assert.That(state.CounterValue, Is.EqualTo(1));
        });
    }

    [Test]
    public void ObjectIdGenerationIsDeterministicAndProviderStable()
    {
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(1700000000L).AddTicks(9999999L);
        IdentifierProvider first = Fake.Create(4902).Identifier();
        IdentifierProvider second = Fake.Create(4902).Identifier();

        byte[] firstValue = first.ObjectIdBytesFromTimestamp(timestamp);
        byte[] nextValue = first.ObjectIdBytesFromTimestamp(timestamp);
        byte[] repeated = second.ObjectIdBytesFromTimestamp(timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(firstValue, Is.EqualTo(repeated));
            Assert.That(firstValue.AsSpan(4, 5).ToArray(), Is.EqualTo(nextValue.AsSpan(4, 5).ToArray()));
            Assert.That(ReadCounter(nextValue), Is.EqualTo((ReadCounter(firstValue) + 1) & 16777215));
            Assert.That(first.ObjectIdUnixSecondsFromValueUsingReadOnlySpanOfByte(firstValue), Is.EqualTo(1700000000L));
        });
    }

    [Test]
    public void ObjectIdTextRoundTripsAndCanonicalizesHexadecimalCase()
    {
        IdentifierProvider identifiers = Fake.Create(4903).Identifier();
        string uppercase = "010203041122334455AABBCC";

        byte[] parsed = identifiers.ParseObjectId(uppercase);

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.FormatObjectId(parsed), Is.EqualTo("010203041122334455aabbcc"));
            Assert.That(identifiers.ValidateObjectId(uppercase), Is.True);
            Assert.That(identifiers.ValidateObjectIdFromValueUsingReadOnlySpanOfByte(parsed), Is.True);
            Assert.That(identifiers.ObjectIdUnixSeconds(uppercase), Is.EqualTo(0x01020304L));
        });
    }

    [TestCase("")]
    [TestCase("010203041122334455aabbc")]
    [TestCase("010203041122334455aabbcc0")]
    [TestCase("010203041122334455aabbcg")]
    public void InvalidObjectIdTextIsRejected(string value)
    {
        IdentifierProvider identifiers = Fake.Create(4904).Identifier();

        Assert.That(identifiers.ValidateObjectId(value), Is.False);
        Assert.That(Assert.Throws<ArgumentException>(() => identifiers.ParseObjectId(value)).ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void ObjectIdSpanOperationsPreserveDestinationSuffixes()
    {
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(1700000001L);
        IdentifierProvider actual = Fake.Create(4905).Identifier();
        IdentifierProvider expected = Fake.Create(4905).Identifier();
        char[] text = Enumerable.Repeat('#', 28).ToArray();
        byte[] bytes = Enumerable.Repeat((byte)0xee, 16).ToArray();

        int textWritten = actual.WriteObjectIdFromDestinationAndTimestamp(text, timestamp);
        int bytesWritten = actual.WriteObjectIdBytesFromDestinationAndTimestamp(bytes, timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(new string(text, 0, textWritten), Is.EqualTo(expected.ObjectIdFromTimestamp(timestamp)));
            Assert.That(bytes.Take(bytesWritten), Is.EqualTo(expected.ObjectIdBytesFromTimestamp(timestamp)));
            Assert.That(text.Skip(textWritten), Has.All.EqualTo('#'));
            Assert.That(bytes.Skip(bytesWritten), Has.All.EqualTo(0xee));
        });
    }

    [Test]
    public void ObjectIdRangeAndDestinationFailuresDoNotAdvanceState()
    {
        IdentifierProvider actual = Fake.Create(4906).Identifier();
        IdentifierProvider expected = Fake.Create(4906).Identifier();
        DateTimeOffset beforeEpoch = DateTimeOffset.UnixEpoch.AddTicks(-1L);
        DateTimeOffset afterMaximum = DateTimeOffset.FromUnixTimeSeconds(ObjectIdGenerator.MaximumUnixSeconds).AddSeconds(1L);

        Assert.Throws<ArgumentOutOfRangeException>(() => actual.ObjectIdFromTimestamp(beforeEpoch));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.ObjectIdFromTimestamp(afterMaximum));
        Assert.Throws<ArgumentException>(() => actual.WriteObjectId(new char[23]));
        Assert.Throws<ArgumentException>(() => actual.WriteObjectIdBytes(new byte[11]));

        Assert.That(actual.ObjectIdBytes(), Is.EqualTo(expected.ObjectIdBytes()));
    }

    [Test]
    public void SortableTimestampIdUsesBigEndianMillisecondsAndHexRoundTrips()
    {
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(0x010203040506L);
        IdentifierProvider identifiers = Fake.Create(4907).Identifier();

        byte[] bytes = identifiers.SortableTimestampIdBytesFromTimestamp(timestamp);
        string text = identifiers.FormatSortableTimestampId(bytes);
        byte[] parsed = identifiers.ParseSortableTimestampId(text.ToUpperInvariant());

        Assert.Multiple(() =>
        {
            Assert.That(text[..12], Is.EqualTo("010203040506"));
            Assert.That(text, Does.Match("^[0-9a-f]{32}$"));
            Assert.That(parsed, Is.EqualTo(bytes));
            Assert.That(identifiers.ValidateSortableTimestampId(text), Is.True);
            Assert.That(identifiers.SortableTimestampIdUnixMilliseconds(text), Is.EqualTo(0x010203040506L));
        });
    }

    [Test]
    public void SortableTimestampIdsAreStrictlyOrderedAndDeterministicWithinOneMillisecond()
    {
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1800000000123L);
        IdentifierProvider first = Fake.Create(4908).Identifier();
        IdentifierProvider second = Fake.Create(4908).Identifier();
        string previous = "";

        int index = 0;
        while (index < 4096)
        {
            string actual = first.SortableTimestampIdFromTimestamp(timestamp);
            string repeated = second.SortableTimestampIdFromTimestamp(timestamp);
            Assert.That(actual, Is.EqualTo(repeated));
            if (index > 0)
            {
                Assert.That(string.CompareOrdinal(previous, actual), Is.LessThan(0));
            }
            previous = actual;
            index++;
        }
    }

    [Test]
    public void SortableTimestampIdClampsClockRollbackAndReseedsAfterAdvance()
    {
        IdentifierProvider identifiers = Fake.Create(4909).Identifier();
        DateTimeOffset baseline = DateTimeOffset.FromUnixTimeMilliseconds(1900000000000L);

        string first = identifiers.SortableTimestampIdFromTimestamp(baseline.AddSeconds(10));
        string rollback = identifiers.SortableTimestampIdFromTimestamp(baseline);
        string advanced = identifiers.SortableTimestampIdFromTimestamp(baseline.AddSeconds(20));

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.SortableTimestampIdUnixMilliseconds(first), Is.EqualTo(1900000010000L));
            Assert.That(identifiers.SortableTimestampIdUnixMilliseconds(rollback), Is.EqualTo(1900000010000L));
            Assert.That(identifiers.SortableTimestampIdUnixMilliseconds(advanced), Is.EqualTo(1900000020000L));
            Assert.That(string.CompareOrdinal(first, rollback), Is.LessThan(0));
            Assert.That(string.CompareOrdinal(rollback, advanced), Is.LessThan(0));
        });
    }

    [Test]
    public void SortableTimestampTailOverflowIsAtomic()
    {
        SortableTimestampIdState state = new SortableTimestampIdState();
        state.Initialized = true;
        state.LastUnixMilliseconds = 123456L;
        state.LastValue = Enumerable.Repeat(byte.MaxValue, 16).ToArray();
        SortableTimestampIdGenerator.ApplyTimestamp(state.LastValue, state.LastUnixMilliseconds);
        byte[] original = (byte[])state.LastValue.Clone();
        byte[] destination = Enumerable.Repeat((byte)0xee, 16).ToArray();
        FakeRandom random = Fake.Create(4910).Identifier().RandomValue;

        FakeException exception = Assert.Throws<FakeException>(() => state.Next(random, 123456L, destination));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("overflowed"));
            Assert.That(state.LastValue, Is.EqualTo(original));
            Assert.That(destination, Has.All.EqualTo(0xee));
        });
    }

    [Test]
    public void SortableTimestampSpanOperationsAndFailuresAreAtomic()
    {
        IdentifierProvider actual = Fake.Create(4911).Identifier();
        IdentifierProvider expected = Fake.Create(4911).Identifier();
        char[] tooSmallText = Enumerable.Repeat('#', 31).ToArray();
        byte[] tooSmallBytes = Enumerable.Repeat((byte)0xee, 15).ToArray();

        Assert.Throws<ArgumentException>(() => actual.WriteSortableTimestampId(tooSmallText));
        Assert.Throws<ArgumentException>(() => actual.WriteSortableTimestampIdBytes(tooSmallBytes));

        char[] text = Enumerable.Repeat('#', 36).ToArray();
        byte[] bytes = Enumerable.Repeat((byte)0xee, 20).ToArray();
        int textWritten = actual.WriteSortableTimestampId(text);
        int bytesWritten = actual.WriteSortableTimestampIdBytes(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(new string(text, 0, textWritten), Is.EqualTo(expected.SortableTimestampId()));
            Assert.That(bytes.Take(bytesWritten), Is.EqualTo(expected.SortableTimestampIdBytes()));
            Assert.That(text.Skip(textWritten), Has.All.EqualTo('#'));
            Assert.That(bytes.Skip(bytesWritten), Has.All.EqualTo(0xee));
        });
    }

    [Test]
    public void OutputLimitsAreCheckedBeforeIdentifierStateAdvances()
    {
        FakeOptions options = FakeOptions.Create(4912L);
        options.MaximumOutputSize = 20;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expected = Fake.Create(options).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => actual.ObjectId());
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.SortableTimestampId());
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.FormatObjectId(new byte[12]));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.FormatSortableTimestampId(new byte[16]));

        Assert.Multiple(() =>
        {
            Assert.That(actual.ObjectIdBytes(), Is.EqualTo(expected.ObjectIdBytes()));
            Assert.That(actual.SortableTimestampIdBytes(), Is.EqualTo(expected.SortableTimestampIdBytes()));
        });
    }

    [Test]
    public void CryptographicModeProducesValidIdentifiersAndMaintainsOrdering()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();
        string previous = identifiers.SortableTimestampId();

        int index = 0;
        while (index < 128)
        {
            Assert.That(identifiers.ValidateObjectId(identifiers.ObjectId()), Is.True);
            string current = identifiers.SortableTimestampId();
            Assert.That(identifiers.ValidateSortableTimestampId(current), Is.True);
            Assert.That(string.CompareOrdinal(previous, current), Is.LessThan(0));
            previous = current;
            index++;
        }
    }

    static int ReadCounter(byte[] value)
    {
        return (value[9] << 16) | (value[10] << 8) | value[11];
    }
}
