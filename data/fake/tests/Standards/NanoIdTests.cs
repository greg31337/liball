using fake;
using System.Collections.Concurrent;

namespace fake.Tests;

[TestFixture]
public class NanoIdTests
{
    [Test]
    public void DefaultNanoIdUsesReferenceLengthAlphabetAndDeterministicStream()
    {
        IdentifierProvider first = Fake.Create(4801).Identifier();
        IdentifierProvider second = Fake.Create(4801).Identifier();

        string actual = first.NanoId();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Has.Length.EqualTo(21));
            Assert.That(actual, Does.Match("^[_\\-0-9A-Za-z]{21}$"));
            Assert.That(actual, Is.EqualTo(second.NanoId()));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(21)]
    [TestCase(257)]
    public void ExplicitLengthsAreExactAndDeterministic(int length)
    {
        IdentifierProvider first = Fake.Create(4802).Identifier();
        IdentifierProvider second = Fake.Create(4802).Identifier();

        string actual = first.NanoIdFromLength(length);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Has.Length.EqualTo(length));
            Assert.That(actual, Is.EqualTo(second.NanoIdFromLength(length)));
        });
    }

    [Test]
    public void CustomAlphabetSelectionIsUniformAndUsesEverySymbol()
    {
        IdentifierProvider identifiers = Fake.Create(4803).Identifier();
        const string alphabet = "ABC";
        string value = identifiers.NanoIdFromLengthAndAlphabet(120000, alphabet);
        int[] counts = new int[alphabet.Length];

        foreach (char character in value)
        {
            counts[alphabet.IndexOf(character)]++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(value, Has.All.Matches<char>(character => alphabet.Contains(character)));
            Assert.That(counts[0], Is.InRange(39000, 41000));
            Assert.That(counts[1], Is.InRange(39000, 41000));
            Assert.That(counts[2], Is.InRange(39000, 41000));
        });
    }

    [Test]
    public void SingleCharacterAlphabetSupportsDeterministicDegenerateValues()
    {
        IdentifierProvider identifiers = Fake.Create(4804).Identifier();

        Assert.That(identifiers.NanoIdFromLengthAndAlphabet(12, "x"), Is.EqualTo("xxxxxxxxxxxx"));
    }

    [Test]
    public void SpanGenerationUsesDestinationLengthAndLeavesNoExternalState()
    {
        IdentifierProvider actual = Fake.Create(4805).Identifier();
        IdentifierProvider expected = Fake.Create(4805).Identifier();
        char[] destination = Enumerable.Repeat('#', 32).ToArray();

        int written = actual.WriteNanoIdFromDestinationAndAlphabet(destination.AsSpan(3, 25), "0123456789abcdef");

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(25));
            Assert.That(destination.Take(3), Has.All.EqualTo('#'));
            Assert.That(new string(destination, 3, 25), Is.EqualTo(expected.NanoIdFromLengthAndAlphabet(25, "0123456789abcdef")));
            Assert.That(destination.Skip(28), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void AlphabetValidationRejectsNullEmptyDuplicateSurrogateAndOversizeValues()
    {
        IdentifierProvider identifiers = Fake.Create(4806).Identifier();
        string oversized = new string(Enumerable.Range(0, 257).Select(value => (char)(value + 1)).ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.NanoIdFromLengthAndAlphabet(4, null)).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.NanoIdFromLengthAndAlphabet(4, "")).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.NanoIdFromLengthAndAlphabet(4, "abca")).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.NanoIdFromLengthAndAlphabet(4, "a\ud800b")).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.NanoIdFromLengthAndAlphabet(4, "a\udc00b")).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.NanoIdFromLengthAndAlphabet(4, oversized)).ParamName, Is.EqualTo("alphabet"));
        });
    }

    [Test]
    public void ConfiguredAlphabetAndOutputLimitsAreEnforcedBeforeEntropyUse()
    {
        FakeOptions options = FakeOptions.Create(4807L);
        options.MaximumCount = 4;
        options.MaximumOutputSize = 5;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(options).Identifier();

        ArgumentOutOfRangeException alphabetException = Assert.Throws<ArgumentOutOfRangeException>(() => actual.NanoIdFromLengthAndAlphabet(4, "abcde"));
        ArgumentOutOfRangeException lengthException = Assert.Throws<ArgumentOutOfRangeException>(() => actual.NanoIdFromLengthAndAlphabet(6, "abcd"));

        Assert.Multiple(() =>
        {
            Assert.That(alphabetException.ParamName, Is.EqualTo("alphabet"));
            Assert.That(lengthException.ParamName, Is.EqualTo("length"));
            Assert.That(actual.NanoIdFromLengthAndAlphabet(5, "abcd"), Is.EqualTo(expectedPosition.NanoIdFromLengthAndAlphabet(5, "abcd")));
        });
    }

    [Test]
    public void NegativeLengthAndOversizeDestinationAreRejectedAtomically()
    {
        FakeOptions options = FakeOptions.Create(4808L);
        options.MaximumOutputSize = 5;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(options).Identifier();
        char[] destination = Enumerable.Repeat('#', 6).ToArray();

        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(() => actual.NanoIdFromLength(-1));
        ArgumentOutOfRangeException oversized = Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteNanoId(destination));

        Assert.Multiple(() =>
        {
            Assert.That(negative.ParamName, Is.EqualTo("length"));
            Assert.That(oversized.ParamName, Is.EqualTo("destination"));
            Assert.That(destination, Has.All.EqualTo('#'));
            Assert.That(actual.NanoIdFromLength(5), Is.EqualTo(expectedPosition.NanoIdFromLength(5)));
        });
    }

    [Test]
    public void ShortIdIsTheTenCharacterNanoIdConvenience()
    {
        IdentifierProvider shortProvider = Fake.Create(4809).Identifier();
        IdentifierProvider nanoProvider = Fake.Create(4809).Identifier();
        char[] destination = new char[7];

        string value = shortProvider.ShortId();

        Assert.Multiple(() =>
        {
            Assert.That(value, Has.Length.EqualTo(10));
            Assert.That(value, Is.EqualTo(nanoProvider.NanoIdFromLength(10)));
            Assert.That(shortProvider.ShortIdFromLength(4), Has.Length.EqualTo(4));
            Assert.That(shortProvider.WriteShortId(destination), Is.EqualTo(7));
            Assert.That(new string(destination), Has.Length.EqualTo(7));
        });
    }

    [Test]
    public void CryptographicModeProducesValuesFromTheRequestedAlphabet()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();
        const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

        string value = identifiers.NanoIdFromLengthAndAlphabet(4096, alphabet);

        Assert.That(value, Has.All.Matches<char>(character => alphabet.Contains(character)));
    }

    [Test]
    public void DefaultDecimalSequenceStartsAtZeroAndAdvancesByOne()
    {
        IdentifierProvider identifiers = Fake.Create(4810).Identifier();

        Assert.That(
            new string[]
            {
                identifiers.SequenceId(),
                identifiers.SequenceId(),
                identifiers.SequenceId(),
                identifiers.SequenceId()
            },
            Is.EqualTo(new string[] { "0", "1", "2", "3" }));
    }

    [Test]
    public void NamedDecimalSequencesHaveIndependentPositions()
    {
        IdentifierProvider identifiers = Fake.Create(4811).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.SequenceIdFromName("orders"), Is.EqualTo("0"));
            Assert.That(identifiers.SequenceIdFromName("customers"), Is.EqualTo("0"));
            Assert.That(identifiers.SequenceIdFromName("orders"), Is.EqualTo("1"));
            Assert.That(identifiers.SequenceIdFromName("customers"), Is.EqualTo("1"));
        });
    }

    [Test]
    public void ConfiguredSequenceSupportsStartIncrementAndZeroPadding()
    {
        IdentifierProvider identifiers = Fake.Create(4812).Identifier();

        Assert.That(
            new string[]
            {
                identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("invoice", 41UL, 7UL, 5),
                identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("invoice", 41UL, 7UL, 5),
                identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("invoice", 41UL, 7UL, 5)
            },
            Is.EqualTo(new string[] { "00041", "00048", "00055" }));
    }

    [Test]
    public void DefaultConfiguredSequenceOverloadRetainsItsSettings()
    {
        IdentifierProvider identifiers = Fake.Create(4813).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.SequenceIdFromStartAndIncrementAndMinimumDigits(9UL, 10UL, 3), Is.EqualTo("009"));
            Assert.That(identifiers.SequenceIdFromStartAndIncrementAndMinimumDigits(9UL, 10UL, 3), Is.EqualTo("019"));
        });
    }

    [Test]
    public void SequenceConfigurationCannotChangeAfterFirstUse()
    {
        IdentifierProvider identifiers = Fake.Create(4814).Identifier();
        Assert.That(identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("fixed", 10UL, 2UL, 3), Is.EqualTo("010"));

        ArgumentException start = Assert.Throws<ArgumentException>(() => identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("fixed", 11UL, 2UL, 3));
        ArgumentException increment = Assert.Throws<ArgumentException>(() => identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("fixed", 10UL, 3UL, 3));
        ArgumentException width = Assert.Throws<ArgumentException>(() => identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("fixed", 10UL, 2UL, 4));

        Assert.Multiple(() =>
        {
            Assert.That(start.ParamName, Is.EqualTo("name"));
            Assert.That(increment.ParamName, Is.EqualTo("name"));
            Assert.That(width.ParamName, Is.EqualTo("name"));
            Assert.That(identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("fixed", 10UL, 2UL, 3), Is.EqualTo("012"));
        });
    }

    [Test]
    public void SequenceArgumentsAreValidatedBeforeCreatingOrLookingUpState()
    {
        IdentifierProvider identifiers = Fake.Create(4815).Identifier();
        identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("fixed", 0UL, 1UL, 0);

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.SequenceIdFromName((string)null)).ParamName, Is.EqualTo("name"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.SequenceIdFromName(" ")).ParamName, Is.EqualTo("name"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("fixed", 0UL, 0UL, 0)).ParamName, Is.EqualTo("increment"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("fixed", 0UL, 1UL, -1)).ParamName, Is.EqualTo("minimumDigits"));
        });
    }

    [Test]
    public void SequenceReturnsMaximumThenReportsUnsignedOverflowAtomically()
    {
        IdentifierProvider identifiers = Fake.Create(4816).Identifier();

        string maximum = identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("maximum", ulong.MaxValue, 1UL, 0);
        FakeException first = Assert.Throws<FakeException>(() => identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("maximum", ulong.MaxValue, 1UL, 0));
        FakeException second = Assert.Throws<FakeException>(() => identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("maximum", ulong.MaxValue, 1UL, 0));

        Assert.Multiple(() =>
        {
            Assert.That(maximum, Is.EqualTo("18446744073709551615"));
            Assert.That(first.Message, Does.Contain("overflowed"));
            Assert.That(second.Message, Is.EqualTo(first.Message));
        });
    }

    [Test]
    public void SequenceOutputGrowthStopsAtomicallyAtConfiguredLimit()
    {
        FakeOptions options = FakeOptions.Create(4817L);
        options.MaximumOutputSize = 2;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();

        Assert.That(identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("tiny", 99UL, 1UL, 0), Is.EqualTo("99"));
        ArgumentOutOfRangeException first = Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("tiny", 99UL, 1UL, 0));
        ArgumentOutOfRangeException second = Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("tiny", 99UL, 1UL, 0));

        Assert.Multiple(() =>
        {
            Assert.That(first.ParamName, Is.EqualTo("value"));
            Assert.That(second.ActualValue, Is.EqualTo(first.ActualValue));
        });
    }

    [Test]
    public void ConcurrentSequenceCallsReturnEveryPositionExactlyOnce()
    {
        IdentifierProvider identifiers = Fake.Create(4818).Identifier();
        ConcurrentBag<ulong> values = new ConcurrentBag<ulong>();

        Parallel.For(0, 1000, index => values.Add(ulong.Parse(identifiers.SequenceIdFromName("concurrent"))));

        Assert.That(values.Order(), Is.EqualTo(Enumerable.Range(0, 1000).Select(value => (ulong)value)));
    }

    [Test]
    public void DecimalSequencesConsumeNoRandomEntropy()
    {
        IdentifierProvider actual = Fake.Create(4819).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(4819).Identifier();

        actual.SequenceId();
        actual.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("orders", 100UL, 5UL, 6);
        actual.SequenceIdFromNameAndStartAndIncrementAndMinimumDigits("orders", 100UL, 5UL, 6);

        Assert.That(actual.NanoId(), Is.EqualTo(expectedPosition.NanoId()));
    }
}
