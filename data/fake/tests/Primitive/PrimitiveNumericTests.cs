using fake;

namespace fake.Tests;

[TestFixture]
public class PrimitiveNumericTests
{
    [Test]
    public void DigitsAreDeterministicAndRemainWithinTheirRadix()
    {
        PrimitiveProvider first = Fake.Create(3101).Primitive();
        PrimitiveProvider second = Fake.Create(3101).Primitive();
        bool[] decimalSeen = new bool[10];
        bool[] base36Seen = new bool[36];

        for (int index = 0; index < 1000; index++)
        {
            int firstDecimal = first.Digit();
            int secondDecimal = second.Digit();
            int firstBase36 = first.DigitFromRadix(36);
            int secondBase36 = second.DigitFromRadix(36);

            Assert.That(firstDecimal, Is.EqualTo(secondDecimal));
            Assert.That(firstBase36, Is.EqualTo(secondBase36));
            Assert.That(firstDecimal, Is.InRange(0, 9));
            Assert.That(firstBase36, Is.InRange(0, 35));
            decimalSeen[firstDecimal] = true;
            base36Seen[firstBase36] = true;
        }

        Assert.That(decimalSeen, Has.All.True);
        Assert.That(base36Seen, Has.All.True);
    }

    [Test]
    public void DigitStringsHaveExactLengthAndRetainLeadingZeroes()
    {
        PrimitiveProvider first = Fake.Create(3102).Primitive();
        PrimitiveProvider second = Fake.Create(3102).Primitive();
        bool foundLeadingZero = false;

        for (int index = 0; index < 200; index++)
        {
            string actual = first.Digits(12);

            Assert.That(actual, Is.EqualTo(second.Digits(12)));
            Assert.That(actual, Has.Length.EqualTo(12));
            Assert.That(actual, Does.Match("^[0-9]{12}$"));
            if (actual[0] == '0')
            {
                foundLeadingZero = true;
            }
        }

        Assert.That(foundLeadingZero, Is.True);
    }

    [TestCase(2)]
    [TestCase(8)]
    [TestCase(10)]
    [TestCase(16)]
    [TestCase(36)]
    public void NumericStringsHonorExclusiveSignedBounds(int radix)
    {
        PrimitiveProvider primitive = Fake.Create(3103 + radix).Primitive();

        for (int index = 0; index < 500; index++)
        {
            string text = primitive.NumberStringFromRadixAndMinimumAndMaximum(radix, -5000L, 7000L);
            long value = Parse(text, radix);

            Assert.That(value, Is.GreaterThanOrEqualTo(-5000L));
            Assert.That(value, Is.LessThan(7000L));
            Assert.That(text, Does.Match(radix == 36 ? "^-?[0-9a-z]+$" : "^-?[0-9a-f]+$"));
        }
    }

    [Test]
    public void UnsignedNumericStringsCoverTheFullUlongRange()
    {
        PrimitiveProvider primitive = Fake.Create(3115).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(primitive.BinaryFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong.MaxValue, ulong.MaxValue, true, false, 0), Is.EqualTo(new string('1', 64)));
            Assert.That(primitive.OctalFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong.MaxValue, ulong.MaxValue, true, false, 0), Is.EqualTo("1777777777777777777777"));
            Assert.That(primitive.NumberStringFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong.MaxValue, ulong.MaxValue, true, true, 20), Is.EqualTo("+18446744073709551615"));
            Assert.That(primitive.HexFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong.MaxValue, ulong.MaxValue, true, false, 0), Is.EqualTo("ffffffffffffffff"));
            Assert.That(primitive.Base36FromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong.MaxValue, ulong.MaxValue, true, false, 0), Is.EqualTo("3w5e11264sgsf"));
            Assert.That(primitive.NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(36, ulong.MaxValue, ulong.MaxValue, true, true, 14), Is.EqualTo("+03w5e11264sgsf"));
        });

        for (int index = 0; index < 200; index++)
        {
            string text = primitive.NumberStringFromRadixAndMinimumAndMaximumUsingInt32AndUInt64AndUInt64(36, ulong.MaxValue - 1000UL, ulong.MaxValue);
            ulong value = ParseUnsigned(text, 36);
            Assert.That(value, Is.GreaterThanOrEqualTo(ulong.MaxValue - 1000UL));
            Assert.That(value, Is.LessThan(ulong.MaxValue));
        }
    }

    [Test]
    public void UnsignedNumericSpanWriterHandlesMaximumValueAndOptionalSign()
    {
        PrimitiveProvider primitive = Fake.Create(3116).Primitive();
        char[] destination = Enumerable.Repeat('#', 20).ToArray();

        int written = primitive.WriteHexFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(destination.AsSpan(), ulong.MaxValue, ulong.MaxValue, true, true, 16);

        Assert.That(written, Is.EqualTo(17));
        Assert.That(new string(destination), Is.EqualTo("+ffffffffffffffff###"));
    }

    [Test]
    public void NamedRadixMethodsUseExpectedDigitsSignsAndPadding()
    {
        PrimitiveProvider primitive = Fake.Create(3104).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(primitive.BinaryFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(-5L, -5L, true, false, 8), Is.EqualTo("-00000101"));
            Assert.That(primitive.OctalFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(8L, 8L, true, true, 4), Is.EqualTo("+0010"));
            Assert.That(primitive.NumberStringFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(-42L, -42L, true, false, 5), Is.EqualTo("-00042"));
            Assert.That(primitive.HexFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(255L, 255L, true, false, 0), Is.EqualTo("ff"));
            Assert.That(primitive.Base36FromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(35L, 35L, true, false, 0), Is.EqualTo("z"));
            Assert.That(primitive.NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(36, 1295L, 1295L, true, true, 4), Is.EqualTo("+00zz"));
            Assert.That(primitive.NumberStringFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(0L, 0L, true, false, 0), Is.EqualTo("0"));
            Assert.That(primitive.NumberStringFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(0L, 0L, true, true, 0), Is.EqualTo("+0"));
        });
    }

    [Test]
    public void FullLongRangeAndMinimumValueFormatWithoutOverflow()
    {
        PrimitiveProvider primitive = Fake.Create(3105).Primitive();

        Assert.That(primitive.HexFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long.MinValue, long.MinValue, true, false, 0), Is.EqualTo("-8000000000000000"));
        Assert.That(
            primitive.BinaryFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long.MinValue, long.MinValue, true, false, 0),
            Is.EqualTo("-1" + new string('0', 63)));
        Assert.That(primitive.Base36FromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long.MinValue, long.MinValue, true, false, 0), Is.EqualTo("-1y2p0ij32e8e8"));

        for (int index = 0; index < 200; index++)
        {
            string text = primitive.NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(36, long.MinValue, long.MaxValue, true, false, 0);
            Assert.That(Parse(text, 36), Is.InRange(long.MinValue, long.MaxValue));
        }
    }

    [Test]
    public void ConvenienceMethodsMatchGeneralRadixFormatting()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Fake.Create(3106).Primitive().Binary(10L, 1000L), Is.EqualTo(Fake.Create(3106).Primitive().NumberStringFromRadixAndMinimumAndMaximum(2, 10L, 1000L)));
            Assert.That(Fake.Create(3106).Primitive().Octal(10L, 1000L), Is.EqualTo(Fake.Create(3106).Primitive().NumberStringFromRadixAndMinimumAndMaximum(8, 10L, 1000L)));
            Assert.That(Fake.Create(3106).Primitive().Hex(10L, 1000L), Is.EqualTo(Fake.Create(3106).Primitive().NumberStringFromRadixAndMinimumAndMaximum(16, 10L, 1000L)));
            Assert.That(Fake.Create(3106).Primitive().Base36(10L, 1000L), Is.EqualTo(Fake.Create(3106).Primitive().NumberStringFromRadixAndMinimumAndMaximum(36, 10L, 1000L)));
            Assert.That(Fake.Create(3106).Primitive().NumberString(10L, 1000L), Is.EqualTo(Fake.Create(3106).Primitive().NumberStringFromRadixAndMinimumAndMaximum(10, 10L, 1000L)));
        });
    }

    [Test]
    public void DigitSpanWriterFillsTheWholeDestination()
    {
        PrimitiveProvider first = Fake.Create(3107).Primitive();
        PrimitiveProvider second = Fake.Create(3107).Primitive();
        char[] destination = new char[32];

        int written = first.WriteDigits(destination.AsSpan());

        Assert.That(written, Is.EqualTo(destination.Length));
        Assert.That(new string(destination), Is.EqualTo(second.Digits(destination.Length)));
        Assert.That(new string(destination), Does.Match("^[0-9]{32}$"));
    }

    [Test]
    public void NumericSpanWritersReturnLengthAndLeaveTheSuffixUntouched()
    {
        PrimitiveProvider primitive = Fake.Create(3108).Primitive();
        char[] binary = Enumerable.Repeat('#', 12).ToArray();
        char[] octal = Enumerable.Repeat('#', 12).ToArray();
        char[] number = Enumerable.Repeat('#', 12).ToArray();
        char[] hex = Enumerable.Repeat('#', 12).ToArray();
        char[] base36 = Enumerable.Repeat('#', 12).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(primitive.WriteBinary(binary.AsSpan(), -5L, -5L, true, false, 8), Is.EqualTo(9));
            Assert.That(primitive.WriteOctal(octal.AsSpan(), 8L, 8L, true, true, 4), Is.EqualTo(5));
            Assert.That(primitive.WriteNumberString(number.AsSpan(), -42L, -42L, true, false, 5), Is.EqualTo(6));
            Assert.That(primitive.WriteHex(hex.AsSpan(), 255L, 255L, true, false, 4), Is.EqualTo(4));
            Assert.That(primitive.WriteBase36(base36.AsSpan(), 1295L, 1295L, true, true, 4), Is.EqualTo(5));
        });

        Assert.Multiple(() =>
        {
            Assert.That(new string(binary), Is.EqualTo("-00000101###"));
            Assert.That(new string(octal), Is.EqualTo("+0010#######"));
            Assert.That(new string(number), Is.EqualTo("-00042######"));
            Assert.That(new string(hex), Is.EqualTo("00ff########"));
            Assert.That(new string(base36), Is.EqualTo("+00zz#######"));
        });
    }

    [Test]
    public void GeneralRadixSpanWriterSupportsLowercaseBase36()
    {
        PrimitiveProvider primitive = Fake.Create(3109).Primitive();
        char[] destination = Enumerable.Repeat('*', 6).ToArray();

        int written = primitive.WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(destination.AsSpan(), 36, 35L, 35L, true, false, 3);

        Assert.That(written, Is.EqualTo(3));
        Assert.That(new string(destination), Is.EqualTo("00z***"));
    }

    [Test]
    public void TooSmallDestinationFailsAtomicallyForWorstCaseRange()
    {
        PrimitiveProvider actual = Fake.Create(3110).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3110).Primitive();
        char[] destination = new char[] { '#', '#' };

        ArgumentException exception = Assert.Throws<ArgumentException>(() => actual.WriteHex(destination.AsSpan(), 0L, 4096L, false, false, 0));

        Assert.That(exception.ParamName, Is.EqualTo("destination"));
        Assert.That(destination, Is.EqualTo(new char[] { '#', '#' }));
        Assert.That(actual.Digits(20), Is.EqualTo(expectedPosition.Digits(20)));
    }

    [Test]
    public void EmptyDigitOperationsDoNotAdvanceThePrimitiveStream()
    {
        PrimitiveProvider actual = Fake.Create(3111).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3111).Primitive();

        Assert.That(actual.Digits(0), Is.Empty);
        Assert.That(actual.WriteDigits(Span<char>.Empty), Is.Zero);
        Assert.That(actual.Digits(20), Is.EqualTo(expectedPosition.Digits(20)));
    }

    [Test]
    public void PrimitiveStreamIsIndependentFromRootRandomCalls()
    {
        Fake actual = Fake.Create(3112);
        Fake expected = Fake.Create(3112);

        actual.Random().ULong();
        actual.Random().ULong();

        Assert.That(actual.Primitive().Digits(30), Is.EqualTo(expected.Primitive().Digits(30)));
    }

    [Test]
    public void NumericOperationsValidateRadixBoundsAndPaddingBeforeEntropy()
    {
        PrimitiveProvider actual = Fake.Create(3113).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3113).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.DigitFromRadix(1)).ParamName, Is.EqualTo("radix"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.NumberStringFromRadixAndMinimumAndMaximum(37, 0L, 10L)).ParamName, Is.EqualTo("radix"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.NumberString(10L, 10L)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.NumberStringFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(11L, 10L, true, false, 0)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.NumberStringFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(0L, 10L, false, false, -1)).ParamName, Is.EqualTo("minimumDigits"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Digits(-1)).ParamName, Is.EqualTo("count"));
        });

        Assert.That(actual.Digits(20), Is.EqualTo(expectedPosition.Digits(20)));
    }

    [Test]
    public void NumericOperationsHonorConfiguredOutputLimitBeforeEntropy()
    {
        FakeOptions options = FakeOptions.Create(3114L);
        options.MaximumOutputSize = 4;
        PrimitiveProvider actual = Fake.Create(options).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(options).Primitive();
        char[] destination = new char[5];

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Digits(5)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteDigits(destination.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.NumberString(0L, 100000L)).ParamName, Is.EqualTo("minimumDigits"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.HexFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(1L, 1L, true, true, 4)).ParamName, Is.EqualTo("minimumDigits"));
        });

        Assert.That(actual.Digits(4), Is.EqualTo(expectedPosition.Digits(4)));
    }

    static long Parse(string text, int radix)
    {
        bool negative = text[0] == '-';
        bool positive = text[0] == '+';
        int index = negative || positive ? 1 : 0;
        ulong magnitude = 0UL;
        while (index < text.Length)
        {
            char character = text[index];
            int digit = character >= '0' && character <= '9'
                ? character - '0'
                : character - 'a' + 10;
            Assert.That(digit, Is.LessThan(radix));
            magnitude = magnitude * (ulong)radix + (ulong)digit;
            index++;
        }
        if (negative)
        {
            if (magnitude == 0x8000000000000000UL)
            {
                return long.MinValue;
            }
            return -(long)magnitude;
        }
        return (long)magnitude;
    }


    static ulong ParseUnsigned(string text, int radix)
    {
        int index = text[0] == '+' ? 1 : 0;
        ulong value = 0UL;
        while (index < text.Length)
        {
            char character = text[index];
            int digit = character >= '0' && character <= '9'
                ? character - '0'
                : character - 'a' + 10;
            Assert.That(digit, Is.LessThan(radix));
            value = value * (ulong)radix + (ulong)digit;
            index++;
        }
        return value;
    }
}
