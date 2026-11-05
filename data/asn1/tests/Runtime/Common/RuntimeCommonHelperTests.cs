using System.Numerics;
using asn1;

namespace asn1.Tests;

public class RuntimeCommonHelperTests
{
    [Test]
    public void ByteAccumulatorGrowsAppendsSegmentsAndClears()
    {
        ByteAccumulator accumulator = new ByteAccumulator(1);

        accumulator.AppendByte(1);
        accumulator.AppendBytes([2, 3]);
        accumulator.AppendSegment([9, 4, 5, 8], 1, 2);

        Assert.That(accumulator.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
        Assert.That(accumulator.Count, Is.EqualTo(5));

        accumulator.Clear();
        accumulator.AppendByte(7);

        Assert.That(accumulator.ToArray(), Is.EqualTo(new byte[] { 7 }));
        Assert.That(() => accumulator.AppendSegment([1], 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new ByteAccumulator(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void TextAccumulatorGrowsAppendsSegmentsAndClears()
    {
        TextAccumulator accumulator = new TextAccumulator(0);

        accumulator.AppendCharacter('A');
        accumulator.AppendText("BC");
        accumulator.AppendTextSegment("-DEF-", 1, 3);

        Assert.That(accumulator.ToText(), Is.EqualTo("ABCDEF"));
        Assert.That(accumulator.Count, Is.EqualTo(6));

        accumulator.Clear();
        accumulator.AppendText("next");

        Assert.That(accumulator.ToText(), Is.EqualTo("next"));
        Assert.That(() => accumulator.AppendTextSegment("x", 0, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new TextAccumulator(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void CursorMathValidatesRangesWithoutOverflow()
    {
        Assert.That(CursorMath.IsOffsetValid(0, 0), Is.True);
        Assert.That(CursorMath.IsOffsetValid(6, 5), Is.False);
        Assert.That(CursorMath.IsRangeValid(2, 3, 5), Is.True);
        Assert.That(CursorMath.IsRangeValid(2, 4, 5), Is.False);
        Assert.That(CursorMath.IsRangeValid(int.MaxValue - 1, 1, int.MaxValue), Is.True);
        Assert.That(CursorMath.IsRangeValid(int.MaxValue - 1, 2, int.MaxValue), Is.False);
        Assert.That(CursorMath.AdvanceOrNegativeOne(2, 3, 5), Is.EqualTo(5));
        Assert.That(CursorMath.AdvanceOrNegativeOne(int.MaxValue - 1, 2, int.MaxValue), Is.EqualTo(-1));
        Assert.That(CursorMath.RemainingOrNegativeOne(2, 5), Is.EqualTo(3));
        Assert.That(CursorMath.RemainingOrNegativeOne(-1, 5), Is.EqualTo(-1));
    }

    [Test]
    public void StableOrderUsesOrdinalAndLexicographicComparisonWithoutReorderingEquals()
    {
        byte[][] byteValues =
        [
            [2],
            [1, 5],
            [1, 5],
            [1, 4],
            [1, 5, 0]
        ];
        string[] textValues = ["b", "a", "a", "A"];

        Assert.That(StableOrder.CompareByteArrays([1, 2], [1, 3]), Is.LessThan(0));
        Assert.That(StableOrder.CompareByteArrays([1, 2], [1, 2]), Is.Zero);
        Assert.That(StableOrder.CompareStringsOrdinal("A", "a"), Is.LessThan(0));
        Assert.That(StableOrder.GetByteArrayOrder(byteValues), Is.EqualTo(new int[] { 3, 1, 2, 4, 0 }));
        Assert.That(StableOrder.GetStringOrder(textValues), Is.EqualTo(new int[] { 3, 1, 2, 0 }));
    }

    [Test]
    public void BigIntegerConversionsPreserveUnsignedAndSignedBigEndianValues()
    {
        Assert.That(BigIntegerConversions.FromUnsignedBigEndian([]), Is.EqualTo(BigInteger.Zero));
        Assert.That(BigIntegerConversions.FromUnsignedBigEndian([255]), Is.EqualTo(new BigInteger(255)));
        Assert.That(BigIntegerConversions.FromUnsignedBigEndian([1, 0]), Is.EqualTo(new BigInteger(256)));
        Assert.That(BigIntegerConversions.ToUnsignedBigEndian(BigInteger.Zero), Is.EqualTo(new byte[] { 0 }));
        Assert.That(BigIntegerConversions.ToUnsignedBigEndian(new BigInteger(255)), Is.EqualTo(new byte[] { 255 }));
        Assert.That(BigIntegerConversions.ToUnsignedBigEndian(new BigInteger(256)), Is.EqualTo(new byte[] { 1, 0 }));
        Assert.That(() => BigIntegerConversions.ToUnsignedBigEndian(new BigInteger(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());

        Assert.That(BigIntegerConversions.FromSignedBigEndian([255]), Is.EqualTo(new BigInteger(-1)));
        Assert.That(BigIntegerConversions.FromSignedBigEndian([128]), Is.EqualTo(new BigInteger(-128)));
        Assert.That(BigIntegerConversions.FromSignedBigEndian([0, 128]), Is.EqualTo(new BigInteger(128)));
        Assert.That(BigIntegerConversions.ToSignedBigEndian(new BigInteger(-129)), Is.EqualTo(new byte[] { 255, 127 }));
        Assert.That(BigIntegerConversions.ToSignedBigEndian(new BigInteger(-128)), Is.EqualTo(new byte[] { 128 }));
        Assert.That(BigIntegerConversions.ToSignedBigEndian(new BigInteger(128)), Is.EqualTo(new byte[] { 0, 128 }));
    }

    [Test]
    public void ResourceCounterEnforcesPerValueAndCumulativeBounds()
    {
        AsnResourceLimits limits = new AsnResourceLimits
        {
            MaximumNestingDepth = 2,
            MaximumCollectionElements = 3,
            MaximumTagOctets = 4,
            MaximumLengthOctets = 5,
            MaximumIntegerBits = 6,
            MaximumTextCharacters = 7,
            MaximumOpenTypeBytes = 8,
            MaximumTotalAllocationBytes = 10,
            MaximumDiagnostics = 2
        };
        ResourceCounter counter = new ResourceCounter(limits);

        Assert.That(counter.TryEnterNesting(), Is.True);
        Assert.That(counter.TryEnterNesting(), Is.True);
        Assert.That(counter.TryEnterNesting(), Is.False);
        counter.LeaveNesting();
        Assert.That(counter.TryEnterNesting(), Is.True);

        Assert.That(counter.AllowsCollectionElements(3), Is.True);
        Assert.That(counter.AllowsCollectionElements(4), Is.False);
        Assert.That(counter.AllowsTagOctets(4), Is.True);
        Assert.That(counter.AllowsTagOctets(5), Is.False);
        Assert.That(counter.AllowsLengthOctets(5), Is.True);
        Assert.That(counter.AllowsLengthOctets(6), Is.False);
        Assert.That(counter.AllowsIntegerBits(6), Is.True);
        Assert.That(counter.AllowsIntegerBits(7), Is.False);
        Assert.That(counter.AllowsTextCharacters(7), Is.True);
        Assert.That(counter.AllowsTextCharacters(8), Is.False);
        Assert.That(counter.AllowsOpenTypeBytes(8), Is.True);
        Assert.That(counter.AllowsOpenTypeBytes(9), Is.False);

        Assert.That(counter.TryChargeAllocationBytes(6), Is.True);
        Assert.That(counter.TryChargeAllocationBytes(4), Is.True);
        Assert.That(counter.TryChargeAllocationBytes(1), Is.False);
        Assert.That(counter.AllocatedBytes, Is.EqualTo(10));
        Assert.That(counter.TryChargeDiagnostic(), Is.True);
        Assert.That(counter.TryChargeDiagnostic(), Is.True);
        Assert.That(counter.TryChargeDiagnostic(), Is.False);
        Assert.That(() => counter.AllowsCollectionElements(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => counter.TryChargeAllocationBytes(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ResourceCounterBypassesBoundsOnlyWhenUnlimitedIsExplicit()
    {
        AsnResourceLimits boundedLimits = new AsnResourceLimits
        {
            MaximumNestingDepth = 0,
            MaximumCollectionElements = 0,
            MaximumTotalAllocationBytes = 0,
            MaximumDiagnostics = 0
        };
        AsnResourceLimits unlimitedLimits = new AsnResourceLimits
        {
            Unlimited = true,
            MaximumNestingDepth = 0,
            MaximumCollectionElements = 0,
            MaximumTotalAllocationBytes = 0,
            MaximumDiagnostics = 0
        };
        ResourceCounter bounded = new ResourceCounter(boundedLimits);
        ResourceCounter unlimited = new ResourceCounter(unlimitedLimits);

        Assert.That(bounded.TryEnterNesting(), Is.False);
        Assert.That(bounded.AllowsCollectionElements(1), Is.False);
        Assert.That(bounded.TryChargeAllocationBytes(1), Is.False);
        Assert.That(bounded.TryChargeDiagnostic(), Is.False);

        Assert.That(unlimited.TryEnterNesting(), Is.True);
        Assert.That(unlimited.AllowsCollectionElements(100), Is.True);
        Assert.That(unlimited.TryChargeAllocationBytes(100), Is.True);
        Assert.That(unlimited.TryChargeDiagnostic(), Is.True);
    }
}
