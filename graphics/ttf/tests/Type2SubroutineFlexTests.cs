using ttf;

namespace ttf.Tests;

internal class Type2SubroutineFlexTests
{
    [Test]
    public void SelectsSubroutineBiasAtEveryBoundary()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Type2CharStringExecutor.GetSubroutineBias(0), Is.EqualTo(107));
            Assert.That(Type2CharStringExecutor.GetSubroutineBias(1239), Is.EqualTo(107));
            Assert.That(Type2CharStringExecutor.GetSubroutineBias(1240), Is.EqualTo(1131));
            Assert.That(Type2CharStringExecutor.GetSubroutineBias(33899), Is.EqualTo(1131));
            Assert.That(Type2CharStringExecutor.GetSubroutineBias(33900), Is.EqualTo(32768));
            Assert.Throws<ArgumentOutOfRangeException>(() => Type2CharStringExecutor.GetSubroutineBias(-1));
        });
    }

    [Test]
    public void ExecutesLocalAndGlobalSubroutinesOnTheSharedStack()
    {
        CffIndex local = CreateIndex(Join(Number(5), Number(6), new byte[] { 11 }));
        CffIndex global = CreateIndex(Join(Number(7), Number(8), new byte[] { 11 }));
        byte[] bytes = Join(
            Number(0), Number(0), new byte[] { 21 },
            Number(-107), new byte[] { 10, 5 },
            Number(-107), new byte[] { 29, 5, 14 });
        Type2CharStringResult result = Execute(bytes, local, global, 10, 100);

        Assert.Multiple(() =>
        {
            AssertCommand(result, 0, Type2DrawingCommandKind.Move, 0, 0);
            AssertCommand(result, 1, Type2DrawingCommandKind.Line, 5, 6);
            AssertCommand(result, 2, Type2DrawingCommandKind.Line, 12, 14);
        });
    }

    [Test]
    public void ExecutesNestedSubroutinesAndContinuesAfterReturn()
    {
        CffIndex local = CreateIndex(
            Join(
                Number(-106), new byte[] { 10 },
                Number(1), Number(1), new byte[] { 5, 11 }),
            Join(Number(2), Number(3), new byte[] { 5, 11 }));
        byte[] bytes = Join(
            Number(0), Number(0), new byte[] { 21 },
            Number(-107), new byte[] { 10, 14 });
        Type2CharStringResult result = Execute(bytes, local, null, 10, 100);

        Assert.Multiple(() =>
        {
            AssertCommand(result, 1, Type2DrawingCommandKind.Line, 2, 3);
            AssertCommand(result, 2, Type2DrawingCommandKind.Line, 3, 4);
        });
    }

    [Test]
    public void EnforcesSubroutineDepthAndOperationLimits()
    {
        CffIndex recursive = CreateIndex(Join(
            Number(-107), new byte[] { 10, 11 }));
        CffIndex nested = CreateIndex(
            Join(Number(-106), new byte[] { 10, 11 }),
            Join(Number(-105), new byte[] { 10, 11 }),
            new byte[] { 11 });
        byte[] recursiveMain = Join(
            Number(-107), new byte[] { 10, 14 });
        byte[] excessiveOperations = new byte[]
        {
            12, 0,
            12, 0,
            14
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Execute(
                recursiveMain,
                recursive,
                null,
                2,
                100));
            Assert.Throws<FontLimitException>(() => Execute(
                recursiveMain,
                nested,
                null,
                2,
                100));
            Assert.Throws<FontLimitException>(() => Execute(
                excessiveOperations,
                null,
                null,
                10,
                2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Execute(
                new byte[] { 14 },
                null,
                null,
                0,
                100));
            Assert.Throws<ArgumentOutOfRangeException>(() => Execute(
                new byte[] { 14 },
                null,
                null,
                10,
                0));
        });
    }

    [Test]
    public void AcceptsExactDepthAndRejectsLocalGlobalSubroutineCycle()
    {
        CffIndex exactDepth = CreateIndex(
            Join(Number(-106), new byte[] { 10, 11 }),
            new byte[] { 11 });
        CffIndex cyclicLocal = CreateIndex(Join(
            Number(-107), new byte[] { 29, 11 }));
        CffIndex cyclicGlobal = CreateIndex(Join(
            Number(-107), new byte[] { 10, 11 }));
        byte[] main = Join(Number(-107), new byte[] { 10, 14 });
        Type2CharStringResult accepted = Execute(
            main,
            exactDepth,
            null,
            2,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.GetCommandCount(), Is.Zero);
            Assert.Throws<FontFormatException>(() => Execute(
                main,
                cyclicLocal,
                cyclicGlobal,
                10,
                100));
        });
    }

    [Test]
    public void RejectsMalformedSubroutinePrograms()
    {
        CffIndex missingReturn = CreateIndex(Number(1));
        CffIndex endsCharString = CreateIndex(new byte[] { 14 });
        CffIndex returns = CreateIndex(new byte[] { 11 });
        CffIndex trailingReturnData = CreateIndex(new byte[] { 11, 11 });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Execute(
                Join(Number(-107), new byte[] { 10, 14 }),
                missingReturn,
                null,
                10,
                100));
            Assert.Throws<FontFormatException>(() => Execute(
                Join(Number(-107), new byte[] { 10, 14 }),
                endsCharString,
                null,
                10,
                100));
            Assert.Throws<FontFormatException>(() => Execute(
                Join(Number(-106), new byte[] { 10, 14 }),
                returns,
                null,
                10,
                100));
            Assert.Throws<FontFormatException>(() => Execute(
                new byte[] { 11 },
                null,
                null,
                10,
                100));
            Assert.Throws<FontFormatException>(() => Execute(
                Join(Number(-107), new byte[] { 10, 14 }),
                trailingReturnData,
                null,
                10,
                100));
        });
    }

    [Test]
    public void AllowsFinalSubroutineToEndTheCharString()
    {
        CffIndex local = CreateIndex(new byte[] { 14 });
        Type2CharStringResult result = Execute(
            Join(Number(-107), new byte[] { 10 }),
            local,
            null,
            10,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetWidth(), Is.Zero);
            Assert.That(result.GetCommandCount(), Is.Zero);
        });
    }

    [Test]
    public void ExecutesHorizontalFlexAndFlex()
    {
        Type2CharStringResult horizontal = ExecuteFlex(
            Numbers(10, 20, 5, 30, 40, 50, 60),
            34);
        Type2CharStringResult flex = ExecuteFlex(
            Numbers(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 50),
            35);

        Assert.Multiple(() =>
        {
            AssertCubic(horizontal, 1, 10, 0, 30, 5, 60, 5);
            AssertCubic(horizontal, 2, 100, 5, 150, 0, 210, 0);
            AssertCubic(flex, 1, 1, 2, 4, 6, 9, 12);
            AssertCubic(flex, 2, 16, 20, 25, 30, 36, 42);
        });
    }

    [Test]
    public void ExecutesHorizontalFlexOneAndBothFlexOneAxisBranches()
    {
        Type2CharStringResult horizontal = ExecuteFlex(
            Numbers(10, 2, 20, 3, 30, 40, 50, 4, 60),
            36);
        Type2CharStringResult horizontalDominant = ExecuteFlex(
            Numbers(10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 5),
            37);
        Type2CharStringResult verticalDominant = ExecuteFlex(
            Numbers(1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 5),
            37);

        Assert.Multiple(() =>
        {
            AssertCubic(horizontal, 1, 10, 2, 30, 5, 60, 5);
            AssertCubic(horizontal, 2, 100, 5, 150, 9, 210, 0);
            AssertCubic(horizontalDominant, 2, 40, 4, 50, 5, 55, 0);
            AssertCubic(verticalDominant, 2, 4, 40, 5, 50, 0, 55);
        });
    }

    [Test]
    public void ExecutesDeprecatedDotSectionAndCompatibilityComposition()
    {
        Type2CharStringResult dotSection = Execute(
            Join(Number(10), Number(20), Escape(0), new byte[] { 21, 14 }),
            null,
            null,
            10,
            100);
        Type2CharStringResult composite = Type2CharStringExecutor.Execute(
            CreateSlice(Join(Numbers(12, 34, 65, 194), new byte[] { 14 })),
            100,
            500,
            0);
        Type2CharStringResult explicitWidth = Type2CharStringExecutor.Execute(
            CreateSlice(Join(Number(20), Numbers(1, 2, 3, 4), new byte[] { 14 })),
            100,
            500,
            0);

        Assert.Multiple(() =>
        {
            AssertCommand(dotSection, 0, Type2DrawingCommandKind.Move, 10, 20);
            Assert.That(composite.HasCompatibilityComposite(), Is.True);
            Assert.That(composite.GetCompatibilityAccentX(), Is.EqualTo(12));
            Assert.That(composite.GetCompatibilityAccentY(), Is.EqualTo(34));
            Assert.That(composite.GetCompatibilityBaseCode(), Is.EqualTo(65));
            Assert.That(composite.GetCompatibilityAccentCode(), Is.EqualTo(194));
            Assert.That(composite.GetWidth(), Is.EqualTo(500));
            Assert.That(explicitWidth.GetWidth(), Is.EqualTo(120));
            Assert.That(explicitWidth.HasExplicitWidth(), Is.True);
            Assert.Throws<InvalidOperationException>(() => dotSection.GetCompatibilityBaseCode());
        });
    }

    [Test]
    public void RejectsMalformedFlexAndCompatibilityComposition()
    {
        byte[][] malformed = new byte[][]
        {
            Join(Number(0), Number(0), new byte[] { 21 }, Numbers(1, 2, 3), Escape(34), new byte[] { 14 }),
            Join(Numbers(1, 2, 3, 4, 5, 6, 7), Escape(34), new byte[] { 14 }),
            Join(Numbers(1, 2, 300, 4), new byte[] { 14 }),
            Join(Numbers(1, 2, 3.5, 4), new byte[] { 14 }),
            Join(Number(0), Number(0), new byte[] { 21 }, Numbers(1, 2, 3, 4), new byte[] { 14 })
        };

        Assert.Multiple(() =>
        {
            foreach (byte[] bytes in malformed)
            {
                Assert.Throws<FontFormatException>(() => Type2CharStringExecutor.Execute(
                    CreateSlice(bytes),
                    0,
                    0,
                    0));
            }
        });
    }

    static Type2CharStringResult ExecuteFlex(byte[] operands, int operatorCode)
    {
        return Execute(
            Join(
                Number(0), Number(0), new byte[] { 21 },
                operands, Escape(operatorCode), new byte[] { 14 }),
            null,
            null,
            10,
            100);
    }

    static Type2CharStringResult Execute(
        byte[] bytes,
        CffIndex? local,
        CffIndex? global,
        int maximumDepth,
        int maximumOperations)
    {
        return Type2CharStringExecutor.Execute(
            CreateSlice(bytes),
            0,
            0,
            0,
            local,
            global,
            maximumDepth,
            maximumOperations);
    }

    static CffIndex CreateIndex(params byte[][] objects)
    {
        return CffIndexParser.Parse(CreateSlice(BuildIndex(objects)), 0);
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

    static byte[] BuildIndex(params byte[][] objects)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)objects.Length);

        if (objects.Length == 0)
        {
            return builder.CopyBytes();
        }

        int dataLength = 0;

        foreach (byte[] value in objects)
        {
            dataLength += value.Length;
        }

        int offsetSize = dataLength + 1 <= 255 ? 1 : 2;
        builder.WriteUInt8((byte)offsetSize);
        int offset = 1;
        WriteOffset(builder, offsetSize, offset);

        foreach (byte[] value in objects)
        {
            offset += value.Length;
            WriteOffset(builder, offsetSize, offset);
        }

        foreach (byte[] value in objects)
        {
            builder.WriteBytes(value);
        }

        return builder.CopyBytes();
    }

    static void WriteOffset(
        TestBinaryBuilder builder,
        int offsetSize,
        int offset)
    {
        if (offsetSize == 1)
        {
            builder.WriteUInt8((byte)offset);
        }
        else
        {
            builder.WriteUInt16((ushort)offset);
        }
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

    static byte[] Number(double value)
    {
        if (value != System.Math.Truncate(value))
        {
            int fixedValue = (int)System.Math.Round(value * 65536.0);
            return new TestBinaryBuilder()
                .WriteUInt8(255)
                .WriteInt32(fixedValue)
                .CopyBytes();
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

        return new TestBinaryBuilder()
            .WriteUInt8(28)
            .WriteInt16((short)integer)
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
