using System.Reflection;
using icc;

namespace icc.Tests;

public class CheckedSizeTests
{
    [Test]
    public void CheckedArithmeticReturnsExactBoundaryValues()
    {
        Func<long, long, long, long> addOffsets = CheckedSizeMethod("AddOffsets").CreateDelegate<Func<long, long, long, long>>();
        Func<uint, uint, uint> addUnsigned = CheckedSizeMethod("AddUnsigned").CreateDelegate<Func<uint, uint, uint>>();
        Func<int, int, int, int> multiplyCount = CheckedSizeMethod("MultiplyCount").CreateDelegate<Func<int, int, int, int>>();
        Func<uint, int, int> convertUnsignedSize = CheckedSizeMethod("ConvertUnsignedSize").CreateDelegate<Func<uint, int, int>>();
        Func<int, int, int, int> alignSize = CheckedSizeMethod("AlignSize").CreateDelegate<Func<int, int, int, int>>();
        Func<long, long, long, long> validateRange = CheckedSizeMethod("ValidateRange").CreateDelegate<Func<long, long, long, long>>();
        Func<int, int, int, int> accumulateBytes = CheckedSizeMethod("AccumulateBytes").CreateDelegate<Func<int, int, int, int>>();
        Func<int, int, int, byte[]> createByteArray = CheckedSizeMethod("CreateByteArray").CreateDelegate<Func<int, int, int, byte[]>>();

        Assert.Multiple(() =>
        {
            Assert.That(addOffsets(128, 12, 140), Is.EqualTo(140));
            Assert.That(addUnsigned(uint.MaxValue - 1, 1), Is.EqualTo(uint.MaxValue));
            Assert.That(multiplyCount(4, 12, 48), Is.EqualTo(48));
            Assert.That(convertUnsignedSize(128, 128), Is.EqualTo(128));
            Assert.That(alignSize(129, 4, 132), Is.EqualTo(132));
            Assert.That(validateRange(128, 12, 140), Is.EqualTo(140));
            Assert.That(accumulateBytes(40, 8, 48), Is.EqualTo(48));
            Assert.That(createByteArray(3, 4, 12), Has.Length.EqualTo(12));
        });
    }

    [Test]
    public void CheckedArithmeticRejectsSignedOverflowAndUnsignedWrap()
    {
        Func<long, long, long, long> addOffsets = CheckedSizeMethod("AddOffsets").CreateDelegate<Func<long, long, long, long>>();
        Func<uint, uint, uint> addUnsigned = CheckedSizeMethod("AddUnsigned").CreateDelegate<Func<uint, uint, uint>>();
        Func<uint, int, int> convertUnsignedSize = CheckedSizeMethod("ConvertUnsignedSize").CreateDelegate<Func<uint, int, int>>();
        Func<int, int, int, int> accumulateBytes = CheckedSizeMethod("AccumulateBytes").CreateDelegate<Func<int, int, int, int>>();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => addOffsets(long.MaxValue, 1, long.MaxValue));
            Assert.Throws<IccProfileFormatException>(() => addOffsets(-1, 1, 100));
            Assert.Throws<IccProfileFormatException>(() => addUnsigned(uint.MaxValue, 1));
            Assert.Throws<IccProfileFormatException>(() => convertUnsignedSize(uint.MaxValue, int.MaxValue));
            Assert.Throws<IccProfileFormatException>(() => accumulateBytes(int.MaxValue, 1, int.MaxValue));
        });
    }

    [Test]
    public void ArraySizingRejectsImpossibleAndResourceLimitedAllocations()
    {
        Func<int, int, int, int> multiplyCount = CheckedSizeMethod("MultiplyCount").CreateDelegate<Func<int, int, int, int>>();
        Func<int, int, int, byte[]> createByteArray = CheckedSizeMethod("CreateByteArray").CreateDelegate<Func<int, int, int, byte[]>>();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => multiplyCount(int.MaxValue, 2, int.MaxValue));
            Assert.Throws<IccProfileFormatException>(() => multiplyCount(1_500_000_000, 2, int.MaxValue));
            Assert.Throws<IccProfileFormatException>(() => multiplyCount(11, 10, 100));
            Assert.Throws<IccProfileFormatException>(() => multiplyCount(-1, 4, 100));
            Assert.Throws<IccProfileFormatException>(() => multiplyCount(4, -1, 100));
            Assert.Throws<IccProfileFormatException>(() => createByteArray(101, 1, 100));
        });
    }

    [Test]
    public void RangeAndAlignmentChecksRejectOutOfBoundsResults()
    {
        Func<int, int, int, int> alignSize = CheckedSizeMethod("AlignSize").CreateDelegate<Func<int, int, int, int>>();
        Func<long, long, long, long> validateRange = CheckedSizeMethod("ValidateRange").CreateDelegate<Func<long, long, long, long>>();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => validateRange(90, 11, 100));
            Assert.Throws<IccProfileFormatException>(() => validateRange(0, -1, 100));
            Assert.Throws<IccProfileFormatException>(() => validateRange(0, 0, -1));
            Assert.Throws<IccProfileFormatException>(() => alignSize(int.MaxValue, 4, int.MaxValue));
            Assert.Throws<IccProfileFormatException>(() => alignSize(12, 0, 100));
            Assert.Throws<IccProfileFormatException>(() => alignSize(101, 4, 100));
        });
    }

    private static MethodInfo CheckedSizeMethod(string name)
    {
        Type checkedSize = typeof(IccProfile).Assembly.GetType("icc.CheckedSize")
            ?? throw new InvalidOperationException("CheckedSize was not found in the production assembly.");
        return checkedSize.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"CheckedSize.{name} was not found.");
    }
}
