using ttf;

namespace ttf.Tests;

internal class Type2CharStringTests
{
    [Test]
    public void DecodesEveryType2NumberEncoding()
    {
        byte[] bytes = Join(
            new byte[] { 32, 246, 21 },
            new byte[] { 247, 0, 251, 0, 5 },
            new byte[] { 28, 0x7F, 0xFF, 28, 0x80, 0x00, 5 },
            Fixed(1.5), Fixed(-2.25), new byte[] { 5, 14 });
        Type2CharStringResult result = Execute(bytes);

        Assert.Multiple(() =>
        {
            AssertCommand(result, 0, Type2DrawingCommandKind.Move, -107, 107);
            AssertCommand(result, 1, Type2DrawingCommandKind.Line, 1, -1);
            AssertCommand(result, 2, Type2DrawingCommandKind.Line, 32768, -32769);
            AssertCommand(result, 3, Type2DrawingCommandKind.Line, 32769.5, -32771.25);
            Assert.That(result.GetWidth(), Is.Zero);
            Assert.That(result.HasExplicitWidth(), Is.False);
        });
    }

    [Test]
    public void ResolvesDefaultAndExplicitWidthsAtEveryLegalPosition()
    {
        Type2CharStringResult defaultWidth = Execute(
            new byte[] { 14 },
            100,
            500);
        Type2CharStringResult endWidth = Execute(
            Join(Number(20), new byte[] { 14 }),
            100,
            500);
        Type2CharStringResult moveWidth = Execute(
            Join(Number(20), Number(10), Number(30), new byte[] { 21, 14 }),
            100,
            500);
        Type2CharStringResult verticalMoveWidth = Execute(
            Join(Number(20), Number(7), new byte[] { 4, 14 }),
            100,
            500);
        Type2CharStringResult horizontalMoveWidth = Execute(
            Join(Number(20), Number(9), new byte[] { 22, 14 }),
            100,
            500);
        Type2CharStringResult stemWidth = Execute(
            Join(
                Number(20), Number(5), Number(10), new byte[] { 1 },
                Number(2), Number(3), new byte[] { 21, 14 }),
            100,
            500);

        Assert.Multiple(() =>
        {
            Assert.That(defaultWidth.GetWidth(), Is.EqualTo(500));
            Assert.That(defaultWidth.HasExplicitWidth(), Is.False);
            Assert.That(endWidth.GetWidth(), Is.EqualTo(120));
            Assert.That(endWidth.HasExplicitWidth(), Is.True);
            Assert.That(moveWidth.GetWidth(), Is.EqualTo(120));
            AssertCommand(moveWidth, 0, Type2DrawingCommandKind.Move, 10, 30);
            Assert.That(verticalMoveWidth.GetWidth(), Is.EqualTo(120));
            AssertCommand(verticalMoveWidth, 0, Type2DrawingCommandKind.Move, 0, 7);
            Assert.That(horizontalMoveWidth.GetWidth(), Is.EqualTo(120));
            AssertCommand(horizontalMoveWidth, 0, Type2DrawingCommandKind.Move, 9, 0);
            Assert.That(stemWidth.GetWidth(), Is.EqualTo(120));
            Assert.That(stemWidth.GetStemCount(), Is.EqualTo(1));
            AssertCommand(stemWidth, 0, Type2DrawingCommandKind.Move, 2, 3);
        });
    }

    [Test]
    public void AccountsForStemsAndReadsHintAndCounterMasks()
    {
        byte[] bytes = Join(
            Number(20), Number(10), Number(5), Number(20), Number(6),
            new byte[] { 18 },
            Number(30), Number(4), new byte[] { 3 },
            Number(40), Number(3), new byte[] { 23 },
            Number(50), Number(2), new byte[] { 19, 0xF8 },
            new byte[] { 20, 0xA0 },
            Number(0), Number(0), new byte[] { 21, 14 });
        Type2CharStringResult result = Execute(bytes, 100, 500);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetWidth(), Is.EqualTo(120));
            Assert.That(result.GetStemCount(), Is.EqualTo(5));
            Assert.That(result.GetHorizontalStemCount(), Is.EqualTo(2));
            Assert.That(result.GetVerticalStemCount(), Is.EqualTo(3));
            Assert.That(result.GetStem(0).IsVertical(), Is.False);
            Assert.That(result.GetStem(0).GetPositionDelta(), Is.EqualTo(10));
            Assert.That(result.GetStem(0).GetWidth(), Is.EqualTo(5));
            Assert.That(result.GetStem(2).IsVertical(), Is.True);
            Assert.That(result.GetStem(4).GetPositionDelta(), Is.EqualTo(50));
            Assert.That(result.GetHintMaskCount(), Is.EqualTo(2));
            Assert.That(result.GetHintMask(0).IsCounterMask(), Is.False);
            Assert.That(result.GetHintMask(0).CopyBytes(), Is.EqualTo(new byte[] { 0xF8 }));
            Assert.That(result.GetHintMask(1).IsCounterMask(), Is.True);
            Assert.That(result.GetHintMask(1).CopyBytes(), Is.EqualTo(new byte[] { 0xA0 }));
            Assert.That(result.GetHintMask(0).GetCommandIndex(), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetStem(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetHintMask(2));
        });
    }

    [Test]
    public void MatchesAdobeType2EdgeAndHintMaskExamples()
    {
        Type2CharStringResult edgeHints = Execute(Join(
            Numbers(121, -21, 400, -20), new byte[] { 1, 14 }));
        Type2CharStringResult hintMask = Execute(Join(
            Numbers(280, 100, -70, 40), new byte[] { 18 },
            Numbers(400, 50), new byte[] { 19, 0xA0, 14 }));

        Assert.Multiple(() =>
        {
            Assert.That(edgeHints.GetStemCount(), Is.EqualTo(2));
            Assert.That(edgeHints.GetStem(0).GetPositionDelta(), Is.EqualTo(121));
            Assert.That(edgeHints.GetStem(0).GetWidth(), Is.EqualTo(-21));
            Assert.That(edgeHints.GetStem(1).GetPositionDelta(), Is.EqualTo(400));
            Assert.That(edgeHints.GetStem(1).GetWidth(), Is.EqualTo(-20));
            Assert.That(hintMask.GetStemCount(), Is.EqualTo(3));
            Assert.That(hintMask.GetHorizontalStemCount(), Is.EqualTo(2));
            Assert.That(hintMask.GetVerticalStemCount(), Is.EqualTo(1));
            Assert.That(hintMask.GetHintMaskCount(), Is.EqualTo(1));
            Assert.That(hintMask.GetHintMask(0).CopyBytes(),
                Is.EqualTo(new byte[] { 0xA0 }));
        });
    }

    [Test]
    public void ExecutesRelativeAndAlternatingLineOperators()
    {
        byte[] bytes = Join(
            Number(10), Number(20), new byte[] { 21 },
            Number(5), Number(6), Number(-2), Number(3), new byte[] { 5 },
            Number(10), Number(4), Number(-3), new byte[] { 6 },
            Number(7), Number(2), new byte[] { 7, 14 });
        Type2CharStringResult result = Execute(bytes);

        Assert.Multiple(() =>
        {
            AssertCommand(result, 0, Type2DrawingCommandKind.Move, 10, 20);
            AssertCommand(result, 1, Type2DrawingCommandKind.Line, 15, 26);
            AssertCommand(result, 2, Type2DrawingCommandKind.Line, 13, 29);
            AssertCommand(result, 3, Type2DrawingCommandKind.Line, 23, 29);
            AssertCommand(result, 4, Type2DrawingCommandKind.Line, 23, 33);
            AssertCommand(result, 5, Type2DrawingCommandKind.Line, 20, 33);
            AssertCommand(result, 6, Type2DrawingCommandKind.Line, 20, 40);
            AssertCommand(result, 7, Type2DrawingCommandKind.Line, 22, 40);
        });
    }

    [Test]
    public void ExecutesRelativeCurveCurveLineAndLineCurveOperators()
    {
        byte[] bytes = Join(
            Number(0), Number(0), new byte[] { 21 },
            Numbers(10, 0, 10, 10, 10, 0), new byte[] { 8 },
            Numbers(0, 10, -10, 10, -10, 0, -5, -5), new byte[] { 24 },
            Numbers(5, -5, 2, 0, 2, 2, 2, 0), new byte[] { 25, 14 });
        Type2CharStringResult result = Execute(bytes);

        Assert.Multiple(() =>
        {
            AssertCubic(result, 1, 10, 0, 20, 10, 30, 10);
            AssertCubic(result, 2, 30, 20, 20, 30, 10, 30);
            AssertCommand(result, 3, Type2DrawingCommandKind.Line, 5, 25);
            AssertCommand(result, 4, Type2DrawingCommandKind.Line, 10, 20);
            AssertCubic(result, 5, 12, 20, 14, 22, 16, 22);
        });
    }

    [Test]
    public void ExecutesHorizontalAndVerticalCurveOperators()
    {
        byte[] bytes = Join(
            Number(0), Number(0), new byte[] { 21 },
            Numbers(5, 10, 20, 3, 30), new byte[] { 27 },
            Numbers(4, 10, 5, 20, 30), new byte[] { 26, 14 });
        Type2CharStringResult result = Execute(bytes);

        Assert.Multiple(() =>
        {
            AssertCubic(result, 1, 10, 5, 30, 8, 60, 8);
            AssertCubic(result, 2, 64, 18, 69, 38, 69, 68);
        });
    }

    [Test]
    public void ExecutesAlternatingHvAndVhCurvesWithFinalDeltas()
    {
        byte[] alternating = Join(
            Number(0), Number(0), new byte[] { 21 },
            Numbers(10, 20, 5, 30, 7, 8, 9, 10), new byte[] { 31, 14 });
        Type2CharStringResult hvOptional = Execute(Join(
            Number(0), Number(0), new byte[] { 21 },
            Numbers(10, 20, 5, 30, 4), new byte[] { 31, 14 }));
        Type2CharStringResult vhOptional = Execute(Join(
            Number(0), Number(0), new byte[] { 21 },
            Numbers(7, 8, 9, 10, 4), new byte[] { 30, 14 }));
        Type2CharStringResult result = Execute(alternating);

        Assert.Multiple(() =>
        {
            AssertCubic(result, 1, 10, 0, 30, 5, 30, 35);
            AssertCubic(result, 2, 30, 42, 38, 51, 48, 51);
            AssertCubic(hvOptional, 1, 10, 0, 30, 5, 34, 35);
            AssertCubic(vhOptional, 1, 0, 7, 8, 16, 18, 20);
        });
    }

    [Test]
    public void ExecutesArithmeticAndNumericOperators()
    {
        byte[] bytes = Join(
            Number(10), Number(5), Escape(10),
            Number(9), Escape(26), new byte[] { 21 },
            Number(-4), Escape(9), Number(5), Escape(14), new byte[] { 5 },
            Number(6), Number(7), Escape(24), Number(20), Number(4), Escape(12), new byte[] { 5 },
            Number(10), Number(3), Escape(11), Number(2), new byte[] { 5, 14 });
        Type2CharStringResult result = Execute(bytes);

        Assert.Multiple(() =>
        {
            AssertCommand(result, 0, Type2DrawingCommandKind.Move, 15, 3);
            AssertCommand(result, 1, Type2DrawingCommandKind.Line, 19, -2);
            AssertCommand(result, 2, Type2DrawingCommandKind.Line, 61, 3);
            AssertCommand(result, 3, Type2DrawingCommandKind.Line, 68, 5);
        });
    }

    [Test]
    public void ExecutesLogicConditionalAndTransientOperators()
    {
        byte[] bytes = Join(
            Number(25), Number(3), Escape(20),
            Number(3), Escape(21),
            Number(4), Escape(21), new byte[] { 21 },
            Number(1), Number(0), Escape(3),
            Number(0), Number(1), Escape(4), new byte[] { 5 },
            Number(0), Escape(5),
            Number(3), Number(3), Escape(15), new byte[] { 5 },
            Numbers(10, 20, 1, 2), Escape(22),
            Numbers(30, 40, 3, 2), Escape(22), new byte[] { 5, 14 });
        Type2CharStringResult result = Execute(bytes);

        Assert.Multiple(() =>
        {
            AssertCommand(result, 0, Type2DrawingCommandKind.Move, 25, 0);
            AssertCommand(result, 1, Type2DrawingCommandKind.Line, 25, 1);
            AssertCommand(result, 2, Type2DrawingCommandKind.Line, 26, 2);
            AssertCommand(result, 3, Type2DrawingCommandKind.Line, 36, 42);
        });
    }

    [Test]
    public void ExecutesDupDropExchangeIndexAndRollOperators()
    {
        byte[] bytes = Join(
            Number(0), Number(0), new byte[] { 21 },
            Number(10), Number(20), Escape(27), Escape(18), Escape(28), new byte[] { 5 },
            Number(1), Number(2), Number(3), Number(2), Escape(29), new byte[] { 5 },
            Number(4), Number(5), Number(6), Number(3), Number(1), Escape(30), Escape(18), new byte[] { 5, 14 });
        Type2CharStringResult result = Execute(bytes);

        Assert.Multiple(() =>
        {
            AssertCommand(result, 1, Type2DrawingCommandKind.Line, 20, 10);
            AssertCommand(result, 2, Type2DrawingCommandKind.Line, 21, 12);
            AssertCommand(result, 3, Type2DrawingCommandKind.Line, 24, 13);
            AssertCommand(result, 4, Type2DrawingCommandKind.Line, 30, 17);
        });
    }

    [Test]
    public void RandomOperatorIsSeededAndDeterministic()
    {
        byte[] bytes = Join(
            Escape(23), Escape(23), new byte[] { 21, 14 });
        Type2CharStringResult first = Type2CharStringExecutor.Execute(
            CreateSlice(bytes), 0, 0, 1234);
        Type2CharStringResult second = Type2CharStringExecutor.Execute(
            CreateSlice(bytes), 0, 0, 1234);
        double firstValue = first.GetCommand(0).GetEndX();
        double secondValue = first.GetCommand(0).GetEndY();

        Assert.Multiple(() =>
        {
            Assert.That(firstValue,
                Is.GreaterThan(0).And.LessThanOrEqualTo(1));
            Assert.That(secondValue,
                Is.GreaterThan(0).And.LessThanOrEqualTo(1));
            Assert.That(second.GetCommand(0).GetEndX(),
                Is.EqualTo(firstValue));
            Assert.That(second.GetCommand(0).GetEndY(),
                Is.EqualTo(secondValue));
        });
    }

    [Test]
    public void RejectsMalformedOperandsStacksAndCalculations()
    {
        byte[][] malformed = new byte[][]
        {
            Array.Empty<byte>(),
            new byte[] { 14, 14 },
            new byte[] { 28, 0 },
            new byte[] { 247 },
            new byte[] { 255, 0, 0, 0 },
            new byte[] { 12 },
            Join(Escape(10), new byte[] { 14 }),
            Join(Number(4), Number(0), Escape(12), new byte[] { 14 }),
            Join(Number(-1), Escape(26), new byte[] { 14 }),
            Join(Number(1.5), Escape(21), new byte[] { 14 }),
            Join(Number(32), Escape(21), new byte[] { 14 })
        };
        byte[] overflow = Join(RepeatNumber(0, 49), new byte[] { 14 });

        Assert.Multiple(() =>
        {
            foreach (byte[] value in malformed)
            {
                Assert.Throws<FontFormatException>(() => Execute(value));
            }

            Assert.Throws<FontLimitException>(() => Execute(overflow));
            Assert.Throws<ArgumentNullException>(() => Type2CharStringExecutor.Execute(null!, 0, 0, 0));
        });
    }

    [Test]
    public void RejectsReservedDeferredAndMisplacedDrawingOperators()
    {
        byte[][] malformed = new byte[][]
        {
            new byte[] { 0, 14 },
            new byte[] { 12, 1, 14 },
            new byte[] { 10, 14 },
            new byte[] { 29, 14 },
            new byte[] { 12, 34, 14 },
            Join(Number(1), Number(2), new byte[] { 5, 14 }),
            Join(Number(0), Number(0), new byte[] { 21 }, Number(1), new byte[] { 5, 14 }),
            Join(Number(0), Number(0), new byte[] { 21 }, Numbers(1, 2, 3, 4, 5), new byte[] { 8, 14 }),
            Join(Number(1), Number(2), new byte[] { 1, 19 })
        };

        Assert.Multiple(() =>
        {
            foreach (byte[] value in malformed)
            {
                Assert.Throws<FontFormatException>(() => Execute(value));
            }
        });
    }

    [Test]
    public void EnforcesStemLimitAndHintMaskLength()
    {
        byte[] maximumMask = RepeatByte(0xFF, 12);
        byte[] maximumHintSet = Join(
            RepeatNumber(0, 48), new byte[] { 1 },
            RepeatNumber(0, 48), new byte[] { 1 },
            RepeatNumber(0, 48), new byte[] { 1 },
            RepeatNumber(0, 48), new byte[] { 1, 19 },
            maximumMask,
            new byte[] { 14 });
        byte[] tooManyStems = Join(
            RepeatNumber(0, 48), new byte[] { 1 },
            RepeatNumber(0, 48), new byte[] { 1 },
            RepeatNumber(0, 48), new byte[] { 1 },
            RepeatNumber(0, 48), new byte[] { 1 },
            Number(0), Number(0), new byte[] { 1, 14 });
        byte[] twoByteMask = Join(
            RepeatNumber(0, 18), new byte[] { 1, 19, 0xFF, 0x80, 14 });
        byte[] truncatedMask = Join(
            RepeatNumber(0, 18), new byte[] { 1, 19, 0xFF });
        Type2CharStringResult valid = Execute(twoByteMask);
        Type2CharStringResult maximum = Execute(maximumHintSet);

        Assert.Multiple(() =>
        {
            Assert.That(maximum.GetStemCount(), Is.EqualTo(96));
            Assert.That(maximum.GetHintMaskCount(), Is.EqualTo(1));
            Assert.That(maximum.GetHintMask(0).GetLength(), Is.EqualTo(12));
            Assert.That(maximum.GetHintMask(0).CopyBytes(),
                Is.EqualTo(maximumMask));
            Assert.Throws<FontLimitException>(() => Execute(tooManyStems));
            Assert.That(valid.GetHintMask(0).GetLength(), Is.EqualTo(2));
            Assert.That(valid.GetHintMask(0).CopyBytes(), Is.EqualTo(new byte[] { 0xFF, 0x80 }));
            Assert.Throws<FontFormatException>(() => Execute(truncatedMask));
        });
    }

    [Test]
    public void EnforcesCff1AndCff2OperandStackBoundaries()
    {
        Type2OperandStack cff1 = Type2OperandStack.Create();
        Type2OperandStack cff2 = Type2OperandStack.Create(513);

        for (int index = 0; index < 48; index += 1)
        {
            cff1.Push(index);
        }

        for (int index = 0; index < 513; index += 1)
        {
            cff2.Push(index);
        }

        Assert.Multiple(() =>
        {
            Assert.That(cff1.GetCount(), Is.EqualTo(48));
            Assert.That(cff1.Peek(), Is.EqualTo(47));
            Assert.That(cff2.GetCount(), Is.EqualTo(513));
            Assert.That(cff2.Peek(), Is.EqualTo(512));
            Assert.Throws<FontLimitException>(() => cff1.Push(48));
            Assert.Throws<FontLimitException>(() => cff2.Push(513));
        });
    }

    static Type2CharStringResult Execute(byte[] bytes)
    {
        return Execute(bytes, 0, 0);
    }

    static Type2CharStringResult Execute(
        byte[] bytes,
        double nominalWidth,
        double defaultWidth)
    {
        return Type2CharStringExecutor.Execute(
            CreateSlice(bytes),
            nominalWidth,
            defaultWidth,
            0);
    }

    static void AssertCommand(
        Type2CharStringResult result,
        int index,
        Type2DrawingCommandKind kind,
        double endX,
        double endY)
    {
        Type2DrawingCommand command = result.GetCommand(index);

        Assert.Multiple(() =>
        {
            Assert.That(command.GetKind(), Is.EqualTo(kind));
            Assert.That(command.GetEndX(), Is.EqualTo(endX).Within(0.000001));
            Assert.That(command.GetEndY(), Is.EqualTo(endY).Within(0.000001));
        });
    }

    static void AssertCubic(
        Type2CharStringResult result,
        int index,
        double firstX,
        double firstY,
        double secondX,
        double secondY,
        double endX,
        double endY)
    {
        Type2DrawingCommand command = result.GetCommand(index);

        Assert.Multiple(() =>
        {
            Assert.That(command.GetKind(), Is.EqualTo(Type2DrawingCommandKind.Cubic));
            Assert.That(command.GetControlX(), Is.EqualTo(firstX).Within(0.000001));
            Assert.That(command.GetControlY(), Is.EqualTo(firstY).Within(0.000001));
            Assert.That(command.GetSecondControlX(), Is.EqualTo(secondX).Within(0.000001));
            Assert.That(command.GetSecondControlY(), Is.EqualTo(secondY).Within(0.000001));
            Assert.That(command.GetEndX(), Is.EqualTo(endX).Within(0.000001));
            Assert.That(command.GetEndY(), Is.EqualTo(endY).Within(0.000001));
        });
    }

    static byte[] Escape(int operatorCode)
    {
        return new byte[] { 12, (byte)operatorCode };
    }

    static byte[] Numbers(params double[] values)
    {
        byte[][] encoded = new byte[values.Length][];

        for (int index = 0; index < values.Length; index += 1)
        {
            encoded[index] = Number(values[index]);
        }

        return Join(encoded);
    }

    static byte[] RepeatNumber(double value, int count)
    {
        byte[][] encoded = new byte[count][];

        for (int index = 0; index < count; index += 1)
        {
            encoded[index] = Number(value);
        }

        return Join(encoded);
    }

    static byte[] RepeatByte(byte value, int count)
    {
        byte[] bytes = new byte[count];

        for (int index = 0; index < bytes.Length; index += 1)
        {
            bytes[index] = value;
        }

        return bytes;
    }

    static byte[] Number(double value)
    {
        if (value != System.Math.Truncate(value))
        {
            return Fixed(value);
        }

        int integer = (int)value;

        if (integer >= -107 && integer <= 107)
        {
            return new byte[] { (byte)(integer + 139) };
        }

        if (integer >= 108 && integer <= 1131)
        {
            int adjusted = integer - 108;
            return new byte[]
            {
                (byte)(247 + adjusted / 256),
                (byte)(adjusted % 256)
            };
        }

        if (integer >= -1131 && integer <= -108)
        {
            int adjusted = -integer - 108;
            return new byte[]
            {
                (byte)(251 + adjusted / 256),
                (byte)(adjusted % 256)
            };
        }

        return new TestBinaryBuilder()
            .WriteUInt8(28)
            .WriteInt16((short)integer)
            .CopyBytes();
    }

    static byte[] Fixed(double value)
    {
        int encoded = (int)System.Math.Round(value * 65536.0);
        return new TestBinaryBuilder()
            .WriteUInt8(255)
            .WriteInt32(encoded)
            .CopyBytes();
    }

    static FontDataSlice CreateSlice(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return data.GetSlice(0, data.GetLength());
    }

    static byte[] Join(params byte[][] values)
    {
        int length = 0;

        foreach (byte[] value in values)
        {
            length += value.Length;
        }

        byte[] result = new byte[length];
        int offset = 0;

        foreach (byte[] value in values)
        {
            value.CopyTo(result, offset);
            offset += value.Length;
        }

        return result;
    }
}
