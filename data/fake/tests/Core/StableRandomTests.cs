using System.Text;
using fake;

namespace fake.Tests;

[TestFixture]
public class StableRandomTests
{
    [Test]
    public void WrappingArithmeticPreservesModuloSixtyFourBitResults()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WrappingArithmetic.Add(ulong.MaxValue, 1UL), Is.EqualTo(0UL));
            Assert.That(WrappingArithmetic.Add(ulong.MaxValue, ulong.MaxValue), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(WrappingArithmetic.Multiply(ulong.MaxValue, ulong.MaxValue), Is.EqualTo(1UL));
            Assert.That(WrappingArithmetic.Multiply(ulong.MaxValue, 2UL), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(WrappingArithmetic.Multiply(0x8000000000000000UL, 2UL), Is.EqualTo(0UL));
        });
    }

    [Test]
    public void EqualRootSeedsProduceEqualSequences()
    {
        FakeRandom first = Fake.Create(long.MinValue).Random();
        FakeRandom second = Fake.Create(long.MinValue).Random();

        AssertSequencesEqual(first, second, 32);
    }

    [TestCase(int.MinValue)]
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(int.MaxValue)]
    public void IntegerAndEquivalentLongSeedsProduceEqualSequences(int seed)
    {
        FakeRandom integerSeeded = Fake.Create(0).Random();
        FakeRandom longSeeded = Fake.Create(0).Random();
        integerSeeded.Reseed(seed);
        longSeeded.ReseedFromSeed((long)seed);

        AssertSequencesEqual(integerSeeded, longSeeded, 16);
    }

    [Test]
    public void EqualStringSeedsProduceEqualSequences()
    {
        FakeRandom first = Fake.Create().Random();
        FakeRandom second = Fake.Create().Random();
        first.ReseedFromSeedUsingString("Καλημέρα 🌍");
        second.ReseedFromSeedUsingString("Καλημέρα 🌍");

        AssertSequencesEqual(first, second, 16);
    }

    [Test]
    public void EqualByteArraySeedsProduceEqualSequences()
    {
        FakeRandom first = Fake.Create().Random();
        FakeRandom second = Fake.Create().Random();
        first.ReseedFromSeedUsingByteArray(new byte[] { 0, 1, 2, 127, 128, 255 });
        second.ReseedFromSeedUsingByteArray(new byte[] { 0, 1, 2, 127, 128, 255 });

        AssertSequencesEqual(first, second, 16);
    }

    [Test]
    public void StringSeedMatchesItsUtf8Bytes()
    {
        const string seed = "naïve/東京";
        FakeRandom fromString = Fake.Create().Random();
        FakeRandom fromBytes = Fake.Create().Random();
        fromString.ReseedFromSeedUsingString(seed);
        fromBytes.ReseedFromSeedUsingByteArray(Encoding.UTF8.GetBytes(seed));

        AssertSequencesEqual(fromString, fromBytes, 16);
    }

    [Test]
    public void EmptyStringAndEmptyBytesAreValidEquivalentSeeds()
    {
        FakeRandom fromString = Fake.Create().Random();
        FakeRandom fromBytes = Fake.Create().Random();
        fromString.ReseedFromSeedUsingString(string.Empty);
        fromBytes.ReseedFromSeedUsingByteArray(Array.Empty<byte>());

        AssertSequencesEqual(fromString, fromBytes, 8);
    }

    [Test]
    public void DifferentSeedsProduceDifferentSequences()
    {
        FakeRandom first = Fake.Create(1).Random();
        FakeRandom second = Fake.Create(2).Random();

        ulong firstValue = first.ULong();
        ulong secondValue = second.ULong();

        Assert.That(firstValue, Is.Not.EqualTo(secondValue));
    }

    [Test]
    public void ConsecutiveValuesAdvanceTheStream()
    {
        FakeRandom random = Fake.Create(123).Random();

        ulong first = random.ULong();
        ulong second = random.ULong();

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void ResetReplaysSequenceFromCurrentSeed()
    {
        FakeRandom random = Fake.Create().Random();
        random.ReseedFromSeedUsingString("reset-seed");
        ulong[] expected = Take(random, 20);
        Take(random, 7);

        random.Reset();

        Assert.That(Take(random, 20), Is.EqualTo(expected));
    }

    [Test]
    public void ReseedImmediatelyRestartsAtNewSeed()
    {
        FakeRandom random = Fake.Create(1).Random();
        FakeRandom expected = Fake.Create(987654321).Random();
        Take(random, 10);

        random.Reseed(987654321);

        AssertSequencesEqual(random, expected, 20);
    }

    [Test]
    public void ByteSeedIsCapturedBeforeCallerMutation()
    {
        byte[] seed = new byte[] { 10, 20, 30, 40 };
        FakeRandom random = Fake.Create().Random();
        FakeRandom expected = Fake.Create().Random();
        random.ReseedFromSeedUsingByteArray(seed);
        expected.ReseedFromSeedUsingByteArray(new byte[] { 10, 20, 30, 40 });

        seed[0] = 99;
        Take(random, 4);
        random.Reset();

        AssertSequencesEqual(random, expected, 20);
    }

    [Test]
    public void NullStringSeedIsRejected()
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => random.ReseedFromSeedUsingString((string)null));

        Assert.That(exception.ParamName, Is.EqualTo("seed"));
    }

    [Test]
    public void NullByteSeedIsRejected()
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => random.ReseedFromSeedUsingByteArray((byte[])null));

        Assert.That(exception.ParamName, Is.EqualTo("seed"));
    }

    [Test]
    public void OtherProviderAccessDoesNotAdvanceRootRandomStream()
    {
        Fake first = Fake.Create(44);
        Fake second = Fake.Create(44);

        first.Person();
        first.Date();
        first.Internet();

        AssertSequencesEqual(first.Random(), second.Random(), 16);
    }

    static ulong[] Take(FakeRandom random, int count)
    {
        ulong[] values = new ulong[count];
        int index = 0;
        while (index < count)
        {
            values[index] = random.ULong();
            index = index + 1;
        }
        return values;
    }

    static void AssertSequencesEqual(FakeRandom first, FakeRandom second, int count)
    {
        Assert.That(Take(first, count), Is.EqualTo(Take(second, count)));
    }
}
