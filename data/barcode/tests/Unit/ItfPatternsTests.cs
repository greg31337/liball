namespace barcode.Tests;

public class ItfPatternsTests
{
    static readonly string[] ExpectedDigitPatterns = new string[]
    {
        "nnwwn",
        "wnnnw",
        "nwnnw",
        "wwnnn",
        "nnwnw",
        "wnwnn",
        "nwwnn",
        "nnnww",
        "wnnwn",
        "nwnwn"
    };

    [Test]
    public void CompleteDigitTableMatchesIndependentWideNarrowOracle()
    {
        Assert.That(ExpectedDigitPatterns, Has.Length.EqualTo(10));
        int digit = 0;
        while (digit < ExpectedDigitPatterns.Length)
        {
            string actual = ToWideNarrowString(ItfPatterns.GetDigitWideElements(digit));
            Assert.That(actual, Is.EqualTo(ExpectedDigitPatterns[digit]), $"Digit {digit}");
            digit++;
        }
    }

    [Test]
    public void EveryDigitHasFiveElementsExactlyTwoWideAndAUniqueAssignment()
    {
        HashSet<int> observedPatterns = new HashSet<int>();
        int digit = 0;
        while (digit < ItfPatterns.DigitCount)
        {
            bool[] elements = ItfPatterns.GetDigitWideElements(digit);
            int[] widths = ItfPatterns.GetCanonicalDigitWidths(digit);

            Assert.Multiple(() =>
            {
                Assert.That(elements, Has.Length.EqualTo(5));
                Assert.That(elements.Count(value => value), Is.EqualTo(2), $"Digit {digit}");
                Assert.That(widths, Has.Length.EqualTo(5));
                Assert.That(widths.Count(width => width == 3), Is.EqualTo(2), $"Digit {digit}");
                Assert.That(widths.Count(width => width == 1), Is.EqualTo(3), $"Digit {digit}");
                Assert.That(widths.Sum(), Is.EqualTo(9), $"Digit {digit}");
                Assert.That(observedPatterns.Add(ItfPatterns.GetDigitPatternBits(digit)),
                    Is.True, $"Digit {digit}");
            });
            digit++;
        }

        Assert.That(observedPatterns, Has.Count.EqualTo(10));
    }

    [Test]
    public void PatternAssignmentsRoundTripEveryDigit()
    {
        int digit = 0;
        while (digit < ItfPatterns.DigitCount)
        {
            int pattern = ItfPatterns.GetDigitPatternBits(digit);
            Assert.That(ItfPatterns.FindDigitForPatternBits(pattern), Is.EqualTo(digit));
            digit++;
        }
    }

    [TestCase(0, 0x06)]
    [TestCase(1, 0x11)]
    [TestCase(3, 0x18)]
    [TestCase(7, 0x03)]
    [TestCase(9, 0x0A)]
    public void RepresentativeMasksUseMostSignificantElementFirst(int digit, int expectedPattern)
    {
        Assert.That(ItfPatterns.GetDigitPatternBits(digit), Is.EqualTo(expectedPattern));
    }

    [Test]
    public void StartAndStopTablesMatchTheStandardElementSequences()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ItfPatterns.GetCanonicalStartWidths(), Is.EqualTo(new int[] { 1, 1, 1, 1 }));
            Assert.That(ToWideNarrowString(ItfPatterns.GetStartWideElements()), Is.EqualTo("nnnn"));
            Assert.That(ItfPatterns.StartPatternBits, Is.EqualTo(0x00));
            Assert.That(ItfPatterns.GetCanonicalStopWidths(), Is.EqualTo(new int[] { 3, 1, 1 }));
            Assert.That(ToWideNarrowString(ItfPatterns.GetStopWideElements()), Is.EqualTo("wnn"));
            Assert.That(ItfPatterns.StopPatternBits, Is.EqualTo(0x04));
        });
    }

    [Test]
    public void PairWidthsInterleaveFirstDigitBarsAndSecondDigitSpaces()
    {
        int[] widths = ItfPatterns.GetCanonicalPairWidths(0, 1);

        Assert.That(widths, Is.EqualTo(new int[] { 1, 3, 1, 1, 3, 1, 3, 1, 1, 3 }));
        int firstDigit = 0;
        while (firstDigit < 10)
        {
            int secondDigit = 0;
            while (secondDigit < 10)
            {
                int[] pair = ItfPatterns.GetCanonicalPairWidths(firstDigit, secondDigit);
                int[] first = ItfPatterns.GetCanonicalDigitWidths(firstDigit);
                int[] second = ItfPatterns.GetCanonicalDigitWidths(secondDigit);
                int element = 0;
                while (element < 5)
                {
                    Assert.That(pair[element * 2], Is.EqualTo(first[element]),
                        $"Bar digit {firstDigit}, space digit {secondDigit}, element {element}");
                    Assert.That(pair[(element * 2) + 1], Is.EqualTo(second[element]),
                        $"Bar digit {firstDigit}, space digit {secondDigit}, element {element}");
                    element++;
                }

                secondDigit++;
            }

            firstDigit++;
        }
    }

    [Test]
    public void ReturnedArraysCannotMutateCanonicalTables()
    {
        bool[] digitElements = ItfPatterns.GetDigitWideElements(0);
        int[] digitWidths = ItfPatterns.GetCanonicalDigitWidths(0);
        int[] pairWidths = ItfPatterns.GetCanonicalPairWidths(0, 1);
        int[] startWidths = ItfPatterns.GetCanonicalStartWidths();
        int[] stopWidths = ItfPatterns.GetCanonicalStopWidths();
        digitElements[0] = true;
        digitWidths[0] = 9;
        pairWidths[0] = 9;
        startWidths[0] = 9;
        stopWidths[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(ToWideNarrowString(ItfPatterns.GetDigitWideElements(0)), Is.EqualTo("nnwwn"));
            Assert.That(ItfPatterns.GetCanonicalDigitWidths(0), Is.EqualTo(new int[] { 1, 1, 3, 3, 1 }));
            Assert.That(ItfPatterns.GetCanonicalPairWidths(0, 1),
                Is.EqualTo(new int[] { 1, 3, 1, 1, 3, 1, 3, 1, 1, 3 }));
            Assert.That(ItfPatterns.GetCanonicalStartWidths(), Is.EqualTo(new int[] { 1, 1, 1, 1 }));
            Assert.That(ItfPatterns.GetCanonicalStopWidths(), Is.EqualTo(new int[] { 3, 1, 1 }));
        });
    }

    [Test]
    public void TableConstantsDescribeAllPatternShapes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ItfPatterns.DigitCount, Is.EqualTo(10));
            Assert.That(ItfPatterns.DigitElementCount, Is.EqualTo(5));
            Assert.That(ItfPatterns.WideElementCount, Is.EqualTo(2));
            Assert.That(ItfPatterns.PairElementCount, Is.EqualTo(10));
            Assert.That(ItfPatterns.StartElementCount, Is.EqualTo(4));
            Assert.That(ItfPatterns.StopElementCount, Is.EqualTo(3));
            Assert.That(ItfPatterns.CanonicalNarrowWidth, Is.EqualTo(1));
            Assert.That(ItfPatterns.CanonicalWideWidth, Is.EqualTo(3));
        });
    }

    [Test]
    public void TablesRejectInvalidDigitsAndUnassignedPatterns()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => ItfPatterns.GetDigitPatternBits(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ItfPatterns.GetDigitPatternBits(10), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ItfPatterns.GetDigitWideElements(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ItfPatterns.GetCanonicalDigitWidths(10), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ItfPatterns.GetCanonicalPairWidths(-1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ItfPatterns.GetCanonicalPairWidths(0, 10), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ItfPatterns.FindDigitForPatternBits(0x00), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ItfPatterns.FindDigitForPatternBits(0x1F), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static string ToWideNarrowString(bool[] wideElements)
    {
        return new string(wideElements.Select(value => value ? 'w' : 'n').ToArray());
    }
}
