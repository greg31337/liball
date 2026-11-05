using System.Globalization;
using System.Text;

namespace svg.Tests;

public class SvgUtilitiesTests
{
    [TestCase(0.0, true)]
    [TestCase(-12.5, true)]
    [TestCase(double.MaxValue, true)]
    [TestCase(double.PositiveInfinity, false)]
    [TestCase(double.NegativeInfinity, false)]
    [TestCase(double.NaN, false)]
    public void FiniteTestRejectsNaNAndInfinities(double value, bool expected)
    {
        Assert.That(SvgFiniteMath.IsFinite(value), Is.EqualTo(expected));
    }

    [Test]
    public void FiniteArithmeticProducesNormalizedResults()
    {
        SvgFiniteResult add = SvgFiniteMath.Add(1.25, 2.75);
        SvgFiniteResult subtract = SvgFiniteMath.Subtract(10, 3);
        SvgFiniteResult multiply = SvgFiniteMath.Multiply(4, -2.5);
        SvgFiniteResult divide = SvgFiniteMath.Divide(9, 4);
        SvgFiniteResult clampLow = SvgFiniteMath.Clamp(-2, 0, 10);
        SvgFiniteResult clampHigh = SvgFiniteMath.Clamp(12, 0, 10);
        SvgFiniteResult lerp = SvgFiniteMath.Lerp(double.MaxValue, -double.MaxValue, 0.5);
        double normalizedZero = SvgFiniteMath.NormalizeZero(-0.0);

        Assert.Multiple(() =>
        {
            Assert.That(add.Success, Is.True);
            Assert.That(add.Value, Is.EqualTo(4));
            Assert.That(subtract.Value, Is.EqualTo(7));
            Assert.That(multiply.Value, Is.EqualTo(-10));
            Assert.That(divide.Value, Is.EqualTo(2.25));
            Assert.That(clampLow.Value, Is.Zero);
            Assert.That(clampHigh.Value, Is.EqualTo(10));
            Assert.That(lerp.Success, Is.True);
            Assert.That(lerp.Value, Is.Zero);
            Assert.That(BitConverter.DoubleToInt64Bits(normalizedZero), Is.Zero);
        });
    }

    [Test]
    public void FiniteArithmeticReturnsFailuresWithoutLeakingNonFiniteValues()
    {
        SvgFiniteResult operand = SvgFiniteMath.Add(double.NaN, 1);
        SvgFiniteResult overflow = SvgFiniteMath.Multiply(double.MaxValue, 2);
        SvgFiniteResult divideByZero = SvgFiniteMath.Divide(1, -0.0);
        SvgFiniteResult reversedClamp = SvgFiniteMath.Clamp(1, 2, -2);
        SvgFiniteResult nonFiniteLerp = SvgFiniteMath.Lerp(1, 2, double.PositiveInfinity);

        Assert.Multiple(() =>
        {
            Assert.That(operand.Success, Is.False);
            Assert.That(operand.Value, Is.Zero);
            Assert.That(operand.Error, Is.EqualTo("Arithmetic operands must be finite."));
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.Value, Is.Zero);
            Assert.That(overflow.Error, Is.EqualTo("Arithmetic result is not finite."));
            Assert.That(divideByZero.Success, Is.False);
            Assert.That(divideByZero.Error, Is.EqualTo("Division by zero is not allowed."));
            Assert.That(reversedClamp.Success, Is.False);
            Assert.That(reversedClamp.Error, Is.EqualTo("The minimum must not exceed the maximum."));
            Assert.That(nonFiniteLerp.Success, Is.False);
        });
    }

    [Test]
    public void FiniteAdditionSubtractionAndExtrapolationRejectOverflow()
    {
        SvgFiniteResult add = SvgFiniteMath.Add(double.MaxValue, double.MaxValue);
        SvgFiniteResult subtract = SvgFiniteMath.Subtract(-double.MaxValue, double.MaxValue);
        SvgFiniteResult extrapolate = SvgFiniteMath.Lerp(double.MaxValue, -double.MaxValue, 2);

        Assert.Multiple(() =>
        {
            Assert.That(add.Success, Is.False);
            Assert.That(add.Value, Is.Zero);
            Assert.That(subtract.Success, Is.False);
            Assert.That(subtract.Value, Is.Zero);
            Assert.That(extrapolate.Success, Is.False);
            Assert.That(extrapolate.Value, Is.Zero);
        });
    }

    [Test]
    [NonParallelizable]
    public void OrdinalNamesRemainCaseSensitiveUnderTurkishCulture()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");

            string[] unsorted = ["image", "I", "ı", "svg"];
            string[] sorted = ["A", "I", "a", "aa", "b", "ı"];

            Assert.Multiple(() =>
            {
                Assert.That(SvgOrdinalNames.Equal("I", "ı"), Is.False);
                Assert.That(SvgOrdinalNames.Equal("svg", "svg"), Is.True);
                Assert.That(SvgOrdinalNames.Equal(null, null), Is.True);
                Assert.That(SvgOrdinalNames.Compare("I", "ı"), Is.LessThan(0));
                Assert.That(SvgOrdinalNames.Find(unsorted, "I"), Is.EqualTo(1));
                Assert.That(SvgOrdinalNames.Find(unsorted, "i"), Is.EqualTo(-1));
                Assert.That(SvgOrdinalNames.FindSorted(sorted, "aa"), Is.EqualTo(3));
                Assert.That(SvgOrdinalNames.FindSorted(sorted, "z"), Is.EqualTo(-1));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Test]
    public void OrdinalLookupRejectsNullArraysAndNames()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => SvgOrdinalNames.Find(null, "svg"));
            Assert.Throws<ArgumentNullException>(() => SvgOrdinalNames.Find(["svg"], null));
            Assert.Throws<ArgumentNullException>(() => SvgOrdinalNames.FindSorted(null, "svg"));
            Assert.Throws<ArgumentNullException>(() => SvgOrdinalNames.FindSorted(["svg"], null));
        });
    }

    [Test]
    public void CollectionScansUseExactValueAndReferenceSemantics()
    {
        object first = new object();
        object equalButDifferent = new string(['x']);
        object sameValue = new string(['x']);
        object[] references = [first, equalButDifferent, null];
        byte[] bytes = [10, 20, 30, 20];
        char[] characters = ['s', 'v', 'g'];

        Assert.Multiple(() =>
        {
            Assert.That(SvgCollectionScan.IndexOfInt32([4, 8, 4], 4), Is.Zero);
            Assert.That(SvgCollectionScan.IndexOfInt32([4, 8, 4], 5), Is.EqualTo(-1));
            Assert.That(SvgCollectionScan.IndexOfByte(bytes, 20), Is.EqualTo(1));
            Assert.That(SvgCollectionScan.IndexOfByte(bytes, 99), Is.EqualTo(-1));
            Assert.That(SvgCollectionScan.IndexOfCharacter(characters, 'g'), Is.EqualTo(2));
            Assert.That(SvgCollectionScan.IndexOfReference(references, first), Is.Zero);
            Assert.That(SvgCollectionScan.IndexOfReference(references, equalButDifferent), Is.EqualTo(1));
            Assert.That(SvgCollectionScan.IndexOfReference(references, sameValue), Is.EqualTo(-1));
            Assert.That(SvgCollectionScan.IndexOfReference(references, null), Is.EqualTo(2));
            Assert.That(SvgCollectionScan.EqualBytes(bytes, [10, 20, 30, 20]), Is.True);
            Assert.That(SvgCollectionScan.EqualBytes(bytes, [10, 20, 30]), Is.False);
            Assert.That(SvgCollectionScan.EqualInt32([1, 2], [1, 2]), Is.True);
            Assert.That(SvgCollectionScan.EqualInt32([1, 2], [2, 1]), Is.False);
        });
    }

    [Test]
    public void CollectionArrayScansRejectNullArrays()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => SvgCollectionScan.IndexOfInt32(null, 0));
            Assert.Throws<ArgumentNullException>(() => SvgCollectionScan.IndexOfReference(null, null));
            Assert.Throws<ArgumentNullException>(() => SvgCollectionScan.EqualInt32(null, []));
            Assert.Throws<ArgumentNullException>(() => SvgCollectionScan.EqualInt32([], null));
        });
    }

    [Test]
    public void StableByteHashMatchesPublishedFnv1aVector()
    {
        byte[] hello = Encoding.ASCII.GetBytes("hello");

        ulong hash = SvgStableHash.HashBytes(hello);

        Assert.That(hash, Is.EqualTo(0xA430D84680AABD0BUL));
    }

    [Test]
    public void StableHashesDefineEncodingCombinationAndNumericCanonicalization()
    {
        string text = "A😀z";
        byte[] utf16LittleEndian = Encoding.Unicode.GetBytes(text);
        byte[] prefix = [1, 2, 3];
        ulong value = 0x0102030405060708UL;
        byte[] combinedBytes = [1, 2, 3, 8, 7, 6, 5, 4, 3, 2, 1];

        Assert.Multiple(() =>
        {
            Assert.That(SvgStableHash.HashString(text), Is.EqualTo(SvgStableHash.HashBytes(utf16LittleEndian)));
            Assert.That(SvgStableHash.Combine(SvgStableHash.HashBytes(prefix), value), Is.EqualTo(SvgStableHash.HashBytes(combinedBytes)));
            Assert.That(SvgStableHash.HashInt64(0x0102030405060708L), Is.EqualTo(SvgStableHash.HashBytes([8, 7, 6, 5, 4, 3, 2, 1])));
            Assert.That(SvgStableHash.HashDouble(0.0), Is.EqualTo(SvgStableHash.HashDouble(-0.0)));
            Assert.That(SvgStableHash.HashDouble(double.NaN), Is.EqualTo(SvgStableHash.HashDouble(BitConverter.Int64BitsToDouble(0x7FF0000000000001L))));
            Assert.That(SvgStableHash.HashString("A"), Is.Not.EqualTo(SvgStableHash.HashString("a")));
        });
    }

    [Test]
    public void StableStringHashRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SvgStableHash.HashString(null));
    }

    [Test]
    public void CancellationCheckRunsAtExactWorkIntervalAndCanReset()
    {
        SvgCancellationCheck check = new SvgCancellationCheck(CancellationToken.None, 3);

        bool first = check.Tick();
        bool second = check.Tick();
        bool third = check.Tick();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.False);
            Assert.That(second, Is.False);
            Assert.That(third, Is.True);
            Assert.That(check.Remaining, Is.EqualTo(3));
            Assert.That(check.WorkCount, Is.EqualTo(3));
        });

        check.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(check.Remaining, Is.EqualTo(3));
            Assert.That(check.WorkCount, Is.Zero);
        });
    }

    [Test]
    public void CancellationIsObservedNoLaterThanConfiguredInterval()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        SvgCancellationCheck check = new SvgCancellationCheck(source.Token, 3);
        source.Cancel();

        Assert.That(check.Tick(), Is.False);
        Assert.That(check.Tick(), Is.False);
        Assert.Throws<OperationCanceledException>(() => check.Tick());
        Assert.Throws<OperationCanceledException>(() => check.CheckNow());
    }

    [Test]
    public void CancellationCheckRejectsNonpositiveIntervals()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgCancellationCheck(CancellationToken.None, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgCancellationCheck(CancellationToken.None, -1));
        });
    }
}
