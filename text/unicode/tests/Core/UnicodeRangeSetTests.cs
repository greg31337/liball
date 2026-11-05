using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeRangeSetTests
{
    [Test]
    public void Constructor_NormalizesAndDefensivelyCopiesRanges()
    {
        UnicodeRange[] input = new UnicodeRange[]
        {
            new UnicodeRange(30, 30),
            new UnicodeRange(10, 12),
            new UnicodeRange(20, 22),
            new UnicodeRange(12, 19),
        };

        UnicodeRangeSet set = new UnicodeRangeSet(input);
        input[1].Start = 0;
        input[2] = new UnicodeRange(100, 100);

        UnicodeRange[] ranges = set.GetRanges();
        Assert.That(set.GetRangeCount(), Is.EqualTo(2));
        Assert.That(ranges[0].Start, Is.EqualTo(10));
        Assert.That(ranges[0].End, Is.EqualTo(22));
        Assert.That(ranges[1].Start, Is.EqualTo(30));
        Assert.That(ranges[1].End, Is.EqualTo(30));
        Assert.That(set.Contains(10), Is.True);
        Assert.That(set.Contains(22), Is.True);
        Assert.That(set.Contains(23), Is.False);
    }

    [Test]
    public void Constructor_MergesDuplicateContainedAndAdjacentRanges()
    {
        UnicodeRangeSet set = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(8, 10),
            new UnicodeRange(1, 3),
            new UnicodeRange(1, 3),
            new UnicodeRange(2, 2),
            new UnicodeRange(4, 7),
        });

        AssertRanges(set, new int[] { 1 }, new int[] { 10 });
        Assert.That(set.Contains(0), Is.False);
        Assert.That(set.Contains(1), Is.True);
        Assert.That(set.Contains(10), Is.True);
        Assert.That(set.Contains(11), Is.False);
    }

    [Test]
    public void GetRangesAndNormalize_ReturnIndependentSets()
    {
        UnicodeRangeSet set = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(5, 9),
        });

        UnicodeRange[] ranges = set.GetRanges();
        ranges[0].Start = 0;
        UnicodeRangeSet normalized = set.Normalize();
        UnicodeRange[] normalizedRanges = normalized.GetRanges();
        normalizedRanges[0].End = 100;

        Assert.That(set.Contains(0), Is.False);
        Assert.That(set.Contains(5), Is.True);
        Assert.That(set.Contains(10), Is.False);
        Assert.That(normalized.Contains(5), Is.True);
        Assert.That(normalized.Contains(10), Is.False);
    }

    [Test]
    public void SetOperations_ReturnNormalizedMathematicalResults()
    {
        UnicodeRangeSet left = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(0, 4),
            new UnicodeRange(10, 14),
        });
        UnicodeRangeSet right = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(3, 11),
            new UnicodeRange(20, 20),
        });

        AssertRanges(left.Union(right), new int[] { 0, 20 }, new int[] { 14, 20 });
        AssertRanges(left.Intersect(right), new int[] { 3, 10 }, new int[] { 4, 11 });
        AssertRanges(left.Difference(right), new int[] { 0, 12 }, new int[] { 2, 14 });
        AssertRanges(right.Difference(left), new int[] { 5, 20 }, new int[] { 9, 20 });
    }

    [Test]
    public void SetOperations_HandleEmptyDisjointAndCompleteInputs()
    {
        UnicodeRangeSet empty = new UnicodeRangeSet(new UnicodeRange[] { });
        UnicodeRangeSet left = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(10, 20),
        });
        UnicodeRangeSet disjoint = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(30, 40),
        });
        UnicodeRangeSet covering = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(0, 0x10FFFF),
        });

        AssertRanges(left.Union(empty), new int[] { 10 }, new int[] { 20 });
        AssertRanges(empty.Union(left), new int[] { 10 }, new int[] { 20 });
        AssertRanges(left.Union(disjoint), new int[] { 10, 30 }, new int[] { 20, 40 });
        AssertRanges(left.Intersect(empty), new int[] { }, new int[] { });
        AssertRanges(left.Intersect(disjoint), new int[] { }, new int[] { });
        AssertRanges(left.Difference(empty), new int[] { 10 }, new int[] { 20 });
        AssertRanges(empty.Difference(left), new int[] { }, new int[] { });
        AssertRanges(left.Difference(covering), new int[] { }, new int[] { });
    }

    [Test]
    public void Difference_SplitsRangesAcrossMultipleRemovals()
    {
        UnicodeRangeSet source = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(0, 20),
            new UnicodeRange(30, 40),
        });
        UnicodeRangeSet removed = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(0, 2),
            new UnicodeRange(5, 7),
            new UnicodeRange(12, 35),
            new UnicodeRange(40, 40),
        });

        AssertRanges(source.Difference(removed), new int[] { 3, 8, 36 }, new int[] { 4, 11, 39 });
    }

    [Test]
    public void Complement_UsesTheEntireUnicodeCodePointDomain()
    {
        UnicodeRangeSet set = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(0, 1),
            new UnicodeRange(0x10FFFF, 0x10FFFF),
        });

        AssertRanges(set.Complement(), new int[] { 2 }, new int[] { 0x10FFFE });
        AssertRanges(
            new UnicodeRangeSet(new UnicodeRange[] { }).Complement(),
            new int[] { 0 },
            new int[] { 0x10FFFF });
        Assert.That(set.Complement().Complement().GetRanges()[0].Start, Is.EqualTo(0));
        Assert.That(set.Complement().Complement().GetRanges()[1].End, Is.EqualTo(0x10FFFF));
    }

    [Test]
    public void ComplementOfFullDomain_IsEmpty()
    {
        UnicodeRangeSet full = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(0, 0x10FFFF),
        });

        AssertRanges(full.Complement(), new int[] { }, new int[] { });
        Assert.That(full.Contains(-1), Is.False);
        Assert.That(full.Contains(0), Is.True);
        Assert.That(full.Contains(0x10FFFF), Is.True);
        Assert.That(full.Contains(0x110000), Is.False);
    }

    [Test]
    public void SetOperations_DoNotMutateEitherOperand()
    {
        UnicodeRangeSet left = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(0, 10),
        });
        UnicodeRangeSet right = new UnicodeRangeSet(new UnicodeRange[]
        {
            new UnicodeRange(5, 15),
        });

        left.Union(right).GetRanges()[0].Start = 100;
        left.Intersect(right).GetRanges()[0].End = 100;
        left.Difference(right).GetRanges()[0].End = 100;

        AssertRanges(left, new int[] { 0 }, new int[] { 10 });
        AssertRanges(right, new int[] { 5 }, new int[] { 15 });
    }

    [Test]
    public void RangeSets_ValidateInputsAndUnicodeBounds()
    {
        UnicodeRangeSet set = new UnicodeRangeSet(new UnicodeRange[] { });

        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeRange(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeRange(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeRange(0x110000, 0x110000));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeRange(0, 0x110000));
        Assert.Throws<ArgumentException>(() => new UnicodeRange(2, 1));
        Assert.Throws<ArgumentNullException>(() => new UnicodeRangeSet(null!));
        Assert.Throws<ArgumentException>(() => new UnicodeRangeSet(new UnicodeRange[] { null! }));
        Assert.Throws<ArgumentNullException>(() => set.Union(null!));
        Assert.Throws<ArgumentNullException>(() => set.Intersect(null!));
        Assert.Throws<ArgumentNullException>(() => set.Difference(null!));
    }

    private static void AssertRanges(UnicodeRangeSet set, int[] starts, int[] ends)
    {
        UnicodeRange[] ranges = set.GetRanges();
        Assert.That(ranges, Has.Length.EqualTo(starts.Length));
        Assert.That(ends, Has.Length.EqualTo(starts.Length));
        int index = 0;
        while (index < ranges.Length)
        {
            Assert.That(ranges[index].Start, Is.EqualTo(starts[index]));
            Assert.That(ranges[index].End, Is.EqualTo(ends[index]));
            index = index + 1;
        }
    }
}
