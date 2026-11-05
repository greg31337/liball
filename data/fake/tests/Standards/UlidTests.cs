using fake;

namespace fake.Tests;

[TestFixture]
public class UlidTests
{
    [Test]
    public void CanonicalJavascriptVectorMatchesTimestampAndNetworkBytes()
    {
        IdentifierProvider identifiers = Fake.Create(4701).Identifier();
        string value = "01ARYZ6S41TSV4RRFFQ69G5FAV";
        byte[] expected = Convert.FromHexString("01563DF36481D6764C61EFB99302BD5B");

        byte[] parsed = identifiers.ParseUlid(value);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.EqualTo(expected));
            Assert.That(identifiers.FormatUlid(parsed), Is.EqualTo(value));
            Assert.That(identifiers.UlidUnixMilliseconds(value), Is.EqualTo(1469918176385L));
            Assert.That(identifiers.UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(parsed), Is.EqualTo(1469918176385L));
            Assert.That(identifiers.ValidateUlid(value), Is.True);
        });
    }

    [Test]
    public void CanonicalSpecificationExampleRoundTripsExactly()
    {
        IdentifierProvider identifiers = Fake.Create(4702).Identifier();
        string value = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

        byte[] parsed = identifiers.ParseUlid(value);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(parsed), Is.EqualTo("01563E3AB5D3D6764C61EFB99302BD5B"));
            Assert.That(identifiers.FormatUlid(parsed), Is.EqualTo(value));
            Assert.That(identifiers.UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(parsed), Is.EqualTo(1469922850259L));
        });
    }

    [Test]
    public void MaximumValueUsesOnlyTheAvailableOneHundredTwentyEightBits()
    {
        IdentifierProvider identifiers = Fake.Create(4703).Identifier();

        string text = identifiers.UlidMax();
        byte[] bytes = identifiers.UlidMaxBytes();

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("7ZZZZZZZZZZZZZZZZZZZZZZZZZ"));
            Assert.That(bytes, Has.All.EqualTo(byte.MaxValue));
            Assert.That(identifiers.ParseUlid(text), Is.EqualTo(bytes));
            Assert.That(identifiers.FormatUlid(bytes), Is.EqualTo(text));
            Assert.That(identifiers.UlidUnixMilliseconds(text), Is.EqualTo(UlidGenerator.MaximumUnixMilliseconds));
            Assert.That(identifiers.ValidateUlid(text), Is.True);
        });
    }

    [TestCase("80000000000000000000000000")]
    [TestCase("ZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    public void EncodedValuesAboveOneHundredTwentyEightBitsAreRejected(string value)
    {
        IdentifierProvider identifiers = Fake.Create(4704).Identifier();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => identifiers.ParseUlid(value));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ParamName, Is.EqualTo("value"));
            Assert.That(identifiers.ValidateUlid(value), Is.False);
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.UlidUnixMilliseconds(value)).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void ParsingIsCaseInsensitiveAndFormattingIsCanonicalUppercase()
    {
        IdentifierProvider identifiers = Fake.Create(4705).Identifier();
        string lowercase = "01aryz6s41tsv4rrffq69g5fav";

        byte[] parsed = identifiers.ParseUlid(lowercase);

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.ValidateUlid(lowercase), Is.True);
            Assert.That(identifiers.FormatUlid(parsed), Is.EqualTo("01ARYZ6S41TSV4RRFFQ69G5FAV"));
            Assert.That(identifiers.UlidUnixMilliseconds(lowercase), Is.EqualTo(1469918176385L));
        });
    }

    [TestCase('I')]
    [TestCase('L')]
    [TestCase('O')]
    [TestCase('U')]
    [TestCase('i')]
    [TestCase('l')]
    [TestCase('o')]
    [TestCase('u')]
    [TestCase('-')]
    public void ExcludedOrNonAlphabetCharactersAreRejected(char character)
    {
        IdentifierProvider identifiers = Fake.Create(4706).Identifier();
        char[] value = "01ARYZ6S41TSV4RRFFQ69G5FAV".ToCharArray();
        value[12] = character;
        string malformed = new string(value);

        Assert.That(identifiers.ValidateUlid(malformed), Is.False);
        Assert.That(Assert.Throws<ArgumentException>(() => identifiers.ParseUlid(malformed)).ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void RandomTextAndBinaryGenerationAreDeterministicAndEquivalent()
    {
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123L);
        IdentifierProvider textProvider = Fake.Create(4707).Identifier();
        IdentifierProvider binaryProvider = Fake.Create(4707).Identifier();

        string text = textProvider.UlidFromTimestamp(timestamp);
        byte[] bytes = binaryProvider.UlidBytesFromTimestamp(timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(binaryProvider.FormatUlid(bytes), Is.EqualTo(text));
            Assert.That(binaryProvider.ParseUlid(text), Is.EqualTo(bytes));
            Assert.That(binaryProvider.UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(bytes), Is.EqualTo(1700000000123L));
            Assert.That(text, Does.Match("^[0-7][0-9A-HJKMNP-TV-Z]{25}$"));
        });
    }

    [Test]
    public void RandomFormRefreshesAllRandomnessForEachCall()
    {
        IdentifierProvider first = Fake.Create(4708).Identifier();
        IdentifierProvider second = Fake.Create(4708).Identifier();

        byte[] firstValue = first.UlidBytes();
        byte[] secondValue = first.UlidBytes();

        Assert.Multiple(() =>
        {
            Assert.That(firstValue.AsSpan(6).ToArray(), Is.Not.EqualTo(secondValue.AsSpan(6).ToArray()));
            Assert.That(firstValue, Is.EqualTo(second.UlidBytes()));
            Assert.That(secondValue, Is.EqualTo(second.UlidBytes()));
        });
    }

    [Test]
    public void CapturedReferenceTimeIsTruncatedToMillisecondsAndStoredBigEndian()
    {
        FakeOptions options = FakeOptions.Create(4709L);
        options.ReferenceTime = new DateTimeOffset(2040, 6, 7, 8, 9, 10, TimeSpan.FromHours(-4)).AddTicks(9876L);
        IdentifierProvider identifiers = Fake.Create(options).Identifier();

        byte[] value = identifiers.UlidBytes();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(value), Is.EqualTo(options.ReferenceTime.ToUnixTimeMilliseconds()));
            Assert.That(Convert.ToHexString(value.AsSpan(0, 6)), Is.EqualTo(ToTimestampHex(options.ReferenceTime.ToUnixTimeMilliseconds())));
        });
    }

    [Test]
    public void ApplyingTimestampPreservesTheEightyRandomBits()
    {
        byte[] value = Convert.FromHexString("000000000000102030405060708090A0");

        UlidGenerator.ApplyTimestamp(value, 0x010203040506L);

        Assert.That(Convert.ToHexString(value), Is.EqualTo("010203040506102030405060708090A0"));
    }

    [Test]
    public void MonotonicFormIncrementsRandomnessByExactlyOne()
    {
        IdentifierProvider identifiers = Fake.Create(4710).Identifier();
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1800000000123L);

        byte[] first = identifiers.MonotonicUlidBytesFromTimestamp(timestamp);
        byte[] expected = (byte[])first.Clone();
        bool incremented = UlidGenerator.IncrementRandomness(expected);
        byte[] second = identifiers.MonotonicUlidBytesFromTimestamp(timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(incremented, Is.True);
            Assert.That(second, Is.EqualTo(expected));
            Assert.That(Compare(first, second), Is.LessThan(0));
        });
    }

    [Test]
    public void RandomnessIncrementCarriesAcrossOctetsWithoutChangingTimestamp()
    {
        byte[] value = Convert.FromHexString("0102030405060011223344556677FFFF");

        bool incremented = UlidGenerator.IncrementRandomness(value);

        Assert.Multiple(() =>
        {
            Assert.That(incremented, Is.True);
            Assert.That(Convert.ToHexString(value), Is.EqualTo("01020304050600112233445566780000"));
            Assert.That(UlidGenerator.UnixMilliseconds(value), Is.EqualTo(0x010203040506L));
        });
    }

    [Test]
    public void EqualMillisecondMonotonicGenerationIsStrictlyOrderedAndDeterministic()
    {
        IdentifierProvider first = Fake.Create(4711).Identifier();
        IdentifierProvider second = Fake.Create(4711).Identifier();
        string previous = "";

        int index = 0;
        while (index < 4096)
        {
            string actual = first.MonotonicUlid();
            string expected = second.MonotonicUlid();
            Assert.That(actual, Is.EqualTo(expected));
            if (index > 0)
            {
                Assert.That(string.CompareOrdinal(previous, actual), Is.LessThan(0));
            }
            previous = actual;
            index++;
        }
    }

    [Test]
    public void RegressedMonotonicTimestampsRetainLastTimeAndSortOrder()
    {
        IdentifierProvider identifiers = Fake.Create(4712).Identifier();
        DateTimeOffset baseline = DateTimeOffset.FromUnixTimeMilliseconds(1900000000000L);

        byte[] first = identifiers.MonotonicUlidBytesFromTimestamp(baseline.AddSeconds(10));
        byte[] rollback = identifiers.MonotonicUlidBytesFromTimestamp(baseline);
        byte[] repeated = identifiers.MonotonicUlidBytesFromTimestamp(baseline.AddSeconds(10));
        byte[] advanced = identifiers.MonotonicUlidBytesFromTimestamp(baseline.AddSeconds(20));

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(first), Is.EqualTo(1900000010000L));
            Assert.That(identifiers.UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(rollback), Is.EqualTo(1900000010000L));
            Assert.That(identifiers.UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(repeated), Is.EqualTo(1900000010000L));
            Assert.That(identifiers.UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(advanced), Is.EqualTo(1900000020000L));
            Assert.That(Compare(first, rollback), Is.LessThan(0));
            Assert.That(Compare(rollback, repeated), Is.LessThan(0));
            Assert.That(Compare(repeated, advanced), Is.LessThan(0));
        });
    }

    [Test]
    public void MonotonicRandomnessOverflowThrowsWithoutChangingStateOrDestination()
    {
        UlidMonotonicState state = new UlidMonotonicState();
        state.Initialized = true;
        state.LastUnixMilliseconds = 123456L;
        state.LastValue = Enumerable.Repeat(byte.MaxValue, 16).ToArray();
        UlidGenerator.ApplyTimestamp(state.LastValue, state.LastUnixMilliseconds);
        byte[] originalState = (byte[])state.LastValue.Clone();
        byte[] destination = Enumerable.Repeat((byte)0xee, 16).ToArray();
        FakeRandom random = Fake.Create(4713).Identifier().RandomValue;

        FakeException exception = Assert.Throws<FakeException>(() => state.Next(random, 123456L, destination));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("overflowed"));
            Assert.That(destination, Has.All.EqualTo(0xee));
            Assert.That(state.LastValue, Is.EqualTo(originalState));
            Assert.That(state.LastUnixMilliseconds, Is.EqualTo(123456L));
        });
    }

    [Test]
    public void DestinationSpanOperationsWriteOnlyRequiredSlices()
    {
        IdentifierProvider randomProvider = Fake.Create(4714).Identifier();
        IdentifierProvider expectedRandom = Fake.Create(4714).Identifier();
        IdentifierProvider monotonicProvider = Fake.Create(4715).Identifier();
        IdentifierProvider expectedMonotonic = Fake.Create(4715).Identifier();
        char[] randomText = Enumerable.Repeat('#', 30).ToArray();
        byte[] randomBytes = Enumerable.Repeat((byte)0xee, 20).ToArray();
        char[] monotonicText = Enumerable.Repeat('#', 30).ToArray();
        byte[] monotonicBytes = Enumerable.Repeat((byte)0xee, 20).ToArray();

        int randomTextWritten = randomProvider.WriteUlid(randomText);
        int randomBytesWritten = randomProvider.WriteUlidBytes(randomBytes);
        int monotonicTextWritten = monotonicProvider.WriteMonotonicUlid(monotonicText);
        int monotonicBytesWritten = monotonicProvider.WriteMonotonicUlidBytes(monotonicBytes);

        Assert.Multiple(() =>
        {
            Assert.That(new string(randomText, 0, randomTextWritten), Is.EqualTo(expectedRandom.Ulid()));
            Assert.That(randomBytes.Take(randomBytesWritten), Is.EqualTo(expectedRandom.UlidBytes()));
            Assert.That(new string(monotonicText, 0, monotonicTextWritten), Is.EqualTo(expectedMonotonic.MonotonicUlid()));
            Assert.That(monotonicBytes.Take(monotonicBytesWritten), Is.EqualTo(expectedMonotonic.MonotonicUlidBytes()));
            Assert.That(randomText.Skip(26), Has.All.EqualTo('#'));
            Assert.That(randomBytes.Skip(16), Has.All.EqualTo(0xee));
            Assert.That(monotonicText.Skip(26), Has.All.EqualTo('#'));
            Assert.That(monotonicBytes.Skip(16), Has.All.EqualTo(0xee));
        });
    }

    [Test]
    public void FormatAndParseSpansPreserveDestinationSuffixes()
    {
        IdentifierProvider identifiers = Fake.Create(4716).Identifier();
        byte[] value = Convert.FromHexString("01563DF36481D6764C61EFB99302BD5B");
        char[] textDestination = Enumerable.Repeat('#', 30).ToArray();
        byte[] byteDestination = Enumerable.Repeat((byte)0xee, 20).ToArray();

        int textWritten = identifiers.WriteUlidFromValueAndDestination(value, textDestination);
        int bytesWritten = identifiers.ParseUlidFromValueAndDestination("01aryz6s41tsv4rrffq69g5fav".AsSpan(), byteDestination);

        Assert.Multiple(() =>
        {
            Assert.That(textWritten, Is.EqualTo(26));
            Assert.That(new string(textDestination, 0, textWritten), Is.EqualTo("01ARYZ6S41TSV4RRFFQ69G5FAV"));
            Assert.That(textDestination.Skip(textWritten), Has.All.EqualTo('#'));
            Assert.That(bytesWritten, Is.EqualTo(16));
            Assert.That(byteDestination.Take(bytesWritten), Is.EqualTo(value));
            Assert.That(byteDestination.Skip(bytesWritten), Has.All.EqualTo(0xee));
        });
    }

    [Test]
    public void TooSmallDestinationsFailAtomicallyWithoutEntropyOrStateChanges()
    {
        IdentifierProvider actual = Fake.Create(4717).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(4717).Identifier();
        char[] textDestination = Enumerable.Repeat('#', 25).ToArray();
        byte[] byteDestination = Enumerable.Repeat((byte)0xee, 15).ToArray();

        Assert.Throws<ArgumentException>(() => actual.WriteUlid(textDestination));
        Assert.Throws<ArgumentException>(() => actual.WriteUlidBytes(byteDestination));
        Assert.Throws<ArgumentException>(() => actual.WriteMonotonicUlid(textDestination));
        Assert.Throws<ArgumentException>(() => actual.WriteMonotonicUlidBytes(byteDestination));
        Assert.Throws<ArgumentException>(() => actual.WriteUlidFromValueAndDestination(new byte[16], textDestination));
        Assert.Throws<ArgumentException>(() => actual.ParseUlidFromValueAndDestination("01ARYZ6S41TSV4RRFFQ69G5FAV".AsSpan(), byteDestination));

        Assert.Multiple(() =>
        {
            Assert.That(textDestination, Has.All.EqualTo('#'));
            Assert.That(byteDestination, Has.All.EqualTo(0xee));
            Assert.That(actual.UlidBytes(), Is.EqualTo(expectedPosition.UlidBytes()));
            Assert.That(actual.MonotonicUlidBytes(), Is.EqualTo(expectedPosition.MonotonicUlidBytes()));
        });
    }

    [Test]
    public void InvalidArgumentsAndLengthsUseConsistentParameterNames()
    {
        IdentifierProvider identifiers = Fake.Create(4718).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.FormatUlid((byte[])null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ParseUlid((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateUlid((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.UlidUnixMilliseconds((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.FormatUlid(new byte[15])).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.ParseUlid("01ARYZ6S41TSV4RRFFQ69G5FA")).ParamName, Is.EqualTo("value"));
            Assert.That(identifiers.ValidateUlidFromValueUsingReadOnlySpanOfByte(new byte[15]), Is.False);
        });
    }

    [Test]
    public void PreEpochTimestampFailuresDoNotConsumeEntropyOrAdvanceState()
    {
        IdentifierProvider actual = Fake.Create(4719).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(4719).Identifier();
        DateTimeOffset invalid = DateTimeOffset.UnixEpoch.AddTicks(-1L);

        ArgumentOutOfRangeException randomException = Assert.Throws<ArgumentOutOfRangeException>(() => actual.UlidFromTimestamp(invalid));
        ArgumentOutOfRangeException monotonicException = Assert.Throws<ArgumentOutOfRangeException>(() => actual.MonotonicUlidFromTimestamp(invalid));

        Assert.Multiple(() =>
        {
            Assert.That(randomException.ParamName, Is.EqualTo("timestamp"));
            Assert.That(monotonicException.ParamName, Is.EqualTo("timestamp"));
            Assert.That(actual.UlidBytes(), Is.EqualTo(expectedPosition.UlidBytes()));
            Assert.That(actual.MonotonicUlidBytes(), Is.EqualTo(expectedPosition.MonotonicUlidBytes()));
        });
    }

    [Test]
    public void OutputLimitFailuresDoNotConsumeEntropyOrAdvanceMonotonicState()
    {
        FakeOptions options = FakeOptions.Create(4720L);
        options.MaximumOutputSize = 20;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(options).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => actual.Ulid());
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.MonotonicUlid());
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.UlidMax());
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.FormatUlid(new byte[16]));

        Assert.Multiple(() =>
        {
            Assert.That(actual.UlidBytes(), Is.EqualTo(expectedPosition.UlidBytes()));
            Assert.That(actual.MonotonicUlidBytes(), Is.EqualTo(expectedPosition.MonotonicUlidBytes()));
        });
    }

    [Test]
    public void CryptographicModeProducesValidRandomAndMonotonicUlids()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();
        string previous = identifiers.MonotonicUlid();

        int index = 0;
        while (index < 128)
        {
            Assert.That(identifiers.ValidateUlid(identifiers.Ulid()), Is.True);
            string current = identifiers.MonotonicUlid();
            Assert.That(string.CompareOrdinal(previous, current), Is.LessThan(0));
            previous = current;
            index++;
        }
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

    static string ToTimestampHex(long value)
    {
        byte[] bytes = new byte[6];
        int index = 5;
        while (index >= 0)
        {
            bytes[index] = (byte)value;
            value >>= 8;
            index--;
        }
        return Convert.ToHexString(bytes);
    }
}
