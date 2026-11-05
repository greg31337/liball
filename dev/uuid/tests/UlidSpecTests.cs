using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UlidSpecTests
{
    [Test]
    public void CanonicalSpecExample_ParsesFormatsAndExposesTimestampParts()
    {
        UlidValue value = new Ulid().ParseValue("01ARZ3NDEKTSV4RRFFQ69G5FAV");

        Assert.That(value.ToCanonicalString(), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(value.ToCanonicalString().Substring(0, 10), Is.EqualTo("01ARZ3NDEK"));
        Assert.That(value.ToCanonicalString().Substring(10, 16), Is.EqualTo("TSV4RRFFQ69G5FAV"));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(1469922850259));
    }

    [Test]
    public void CanonicalStructureExample_UsesTenTimestampAndSixteenRandomnessCharacters()
    {
        UlidParseResult result = new Ulid().Parse("01AN4Z07BY79KA1307SR9X4MV3");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.NormalizedText.Substring(0, 10), Is.EqualTo("01AN4Z07BY"));
        Assert.That(result.NormalizedText.Substring(10, 16), Is.EqualTo("79KA1307SR9X4MV3"));
    }

    [Test]
    public void LowercaseSpecInput_NormalizesToCanonicalUppercase()
    {
        UlidParseResult result = new Ulid().Parse("01arz3ndektsv4rrffq69g5fav");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.NormalizedText, Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.ToCanonicalString(), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
    }

    [Test]
    public void SpecMonotonicExamples_SortLexicographicallyAndByBinaryBytes()
    {
        UlidValue first = new Ulid().ParseValue("01BX5ZZKBKACTAV9WEVGEMMVRY");
        UlidValue second = new Ulid().ParseValue("01BX5ZZKBKACTAV9WEVGEMMVRZ");
        UlidValue third = new Ulid().ParseValue("01BX5ZZKBKACTAV9WEVGEMMVS0");
        UlidValue fourth = new Ulid().ParseValue("01BX5ZZKBKACTAV9WEVGEMMVS1");

        Assert.That(string.CompareOrdinal(first.ToCanonicalString(), second.ToCanonicalString()), Is.LessThan(0));
        Assert.That(string.CompareOrdinal(second.ToCanonicalString(), third.ToCanonicalString()), Is.LessThan(0));
        Assert.That(string.CompareOrdinal(third.ToCanonicalString(), fourth.ToCanonicalString()), Is.LessThan(0));
        Assert.That(first.CompareTo(second), Is.LessThan(0));
        Assert.That(second.CompareTo(third), Is.LessThan(0));
        Assert.That(third.CompareTo(fourth), Is.LessThan(0));
    }

    [Test]
    public void SpecMaximumUlid_IsAcceptedAndMinimumOverflowIsRejected()
    {
        UlidValue max = new Ulid().ParseValue("7ZZZZZZZZZZZZZZZZZZZZZZZZZ");
        UlidParseResult overflow = new Ulid().Parse("80000000000000000000000000");
        UlidParseResult allZ = new Ulid().Parse("ZZZZZZZZZZZZZZZZZZZZZZZZZZ");

        Assert.That(max.UnixMilliseconds(), Is.EqualTo(BigEndianTools.MaxUInt48));
        Assert.That(max.RandomnessBytes(), Is.EqualTo(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 }));
        Assert.That(overflow.Ok, Is.False);
        Assert.That(overflow.Error, Is.EqualTo("ULID text overflows 128 bits"));
        Assert.That(allZ.Ok, Is.False);
        Assert.That(allZ.Error, Is.EqualTo("ULID text overflows 128 bits"));
    }

    [Test]
    public void GeneratedMonotonicUlids_SortWithinSameMillisecond()
    {
        UlidGeneratorState state = new UlidGeneratorState();

        UlidValue first = state.NextAtUnixMilliseconds(1645557742000);
        UlidValue second = state.NextAtUnixMilliseconds(1645557742000);
        UlidValue third = state.NextAtUnixMilliseconds(1645557742000);

        Assert.That(string.CompareOrdinal(first.ToCanonicalString(), second.ToCanonicalString()), Is.LessThan(0));
        Assert.That(string.CompareOrdinal(second.ToCanonicalString(), third.ToCanonicalString()), Is.LessThan(0));
        Assert.That(first.CompareTo(second), Is.LessThan(0));
        Assert.That(second.CompareTo(third), Is.LessThan(0));
    }
}
