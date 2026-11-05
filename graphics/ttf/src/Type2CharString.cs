namespace ttf;

internal enum Type2DrawingCommandKind
{
    Move = 0,
    Line = 1,
    Cubic = 2
}

internal class Type2DrawingCommand
{
    Type2DrawingCommandKind kind;
    double controlX;
    double controlY;
    double secondControlX;
    double secondControlY;
    double endX;
    double endY;

    internal static Type2DrawingCommand Create(
        Type2DrawingCommandKind kind,
        double endX,
        double endY)
    {
        return new Type2DrawingCommand(kind, 0, 0, 0, 0, endX, endY);
    }

    internal Type2DrawingCommand(
        Type2DrawingCommandKind kind,
        double controlX,
        double controlY,
        double secondControlX,
        double secondControlY,
        double endX,
        double endY)
    {
        this.kind = kind;
        this.controlX = controlX;
        this.controlY = controlY;
        this.secondControlX = secondControlX;
        this.secondControlY = secondControlY;
        this.endX = endX;
        this.endY = endY;
    }

    internal static Type2DrawingCommand Create(
        double controlX,
        double controlY,
        double secondControlX,
        double secondControlY,
        double endX,
        double endY)
    {
        return new Type2DrawingCommand(
            Type2DrawingCommandKind.Cubic,
            controlX,
            controlY,
            secondControlX,
            secondControlY,
            endX,
            endY);
    }

    internal Type2DrawingCommandKind GetKind()
    {
        return kind;
    }

    internal double GetControlX()
    {
        EnsureCubic();
        return controlX;
    }

    internal double GetControlY()
    {
        EnsureCubic();
        return controlY;
    }

    internal double GetSecondControlX()
    {
        EnsureCubic();
        return secondControlX;
    }

    internal double GetSecondControlY()
    {
        EnsureCubic();
        return secondControlY;
    }

    internal double GetEndX()
    {
        return endX;
    }

    internal double GetEndY()
    {
        return endY;
    }

    void EnsureCubic()
    {
        if (kind != Type2DrawingCommandKind.Cubic)
        {
            throw new System.InvalidOperationException(
                "Only a Type 2 cubic command has control points.");
        }
    }
}

internal class Type2StemHint
{
    bool vertical;
    double positionDelta;
    double width;

    internal Type2StemHint(
        bool vertical,
        double positionDelta,
        double width)
    {
        this.vertical = vertical;
        this.positionDelta = positionDelta;
        this.width = width;
    }

    internal bool IsVertical()
    {
        return vertical;
    }

    internal double GetPositionDelta()
    {
        return positionDelta;
    }

    internal double GetWidth()
    {
        return width;
    }
}

internal class Type2HintMask
{
    bool counterMask;
    byte[] bytes;
    int commandIndex;

    internal Type2HintMask(
        bool counterMask,
        byte[] bytes,
        int commandIndex)
    {
        this.counterMask = counterMask;
        this.bytes = bytes;
        this.commandIndex = commandIndex;
    }

    internal bool IsCounterMask()
    {
        return counterMask;
    }

    internal int GetLength()
    {
        return bytes.Length;
    }

    internal int GetCommandIndex()
    {
        return commandIndex;
    }

    internal byte[] CopyBytes()
    {
        return bytes.AsSpan().ToArray();
    }
}

internal class Type2CharStringResult
{
    double width;
    bool explicitWidth;
    Type2DrawingCommand[] commands;
    Type2StemHint[] stems;
    Type2HintMask[] masks;
    bool compatibilityComposite;
    double compatibilityAccentX;
    double compatibilityAccentY;
    int compatibilityBaseCode;
    int compatibilityAccentCode;
    bool deprecatedOperator;

    internal Type2CharStringResult(
        double width,
        bool explicitWidth,
        Type2DrawingCommand[] commands,
        Type2StemHint[] stems,
        Type2HintMask[] masks,
        bool compatibilityComposite,
        double compatibilityAccentX,
        double compatibilityAccentY,
        int compatibilityBaseCode,
        int compatibilityAccentCode,
        bool deprecatedOperator)
    {
        this.width = width;
        this.explicitWidth = explicitWidth;
        this.commands = commands;
        this.stems = stems;
        this.masks = masks;
        this.compatibilityComposite = compatibilityComposite;
        this.compatibilityAccentX = compatibilityAccentX;
        this.compatibilityAccentY = compatibilityAccentY;
        this.compatibilityBaseCode = compatibilityBaseCode;
        this.compatibilityAccentCode = compatibilityAccentCode;
        this.deprecatedOperator = deprecatedOperator;
    }

    internal double GetWidth()
    {
        return width;
    }

    internal bool HasExplicitWidth()
    {
        return explicitWidth;
    }

    internal int GetCommandCount()
    {
        return commands.Length;
    }

    internal Type2DrawingCommand GetCommand(int index)
    {
        if (index < 0 || index >= commands.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return commands[index];
    }

    internal int GetStemCount()
    {
        return stems.Length;
    }

    internal int GetHorizontalStemCount()
    {
        int count = 0;
        int index = 0;

        while (index < stems.Length)
        {
            if (!stems[index].IsVertical())
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    internal int GetVerticalStemCount()
    {
        return stems.Length - GetHorizontalStemCount();
    }

    internal Type2StemHint GetStem(int index)
    {
        if (index < 0 || index >= stems.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stems[index];
    }

    internal int GetHintMaskCount()
    {
        return masks.Length;
    }

    internal Type2HintMask GetHintMask(int index)
    {
        if (index < 0 || index >= masks.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return masks[index];
    }

    internal bool HasCompatibilityComposite()
    {
        return compatibilityComposite;
    }

    internal bool UsesDeprecatedOperator()
    {
        return deprecatedOperator;
    }

    internal double GetCompatibilityAccentX()
    {
        EnsureCompatibilityComposite();
        return compatibilityAccentX;
    }

    internal double GetCompatibilityAccentY()
    {
        EnsureCompatibilityComposite();
        return compatibilityAccentY;
    }

    internal int GetCompatibilityBaseCode()
    {
        EnsureCompatibilityComposite();
        return compatibilityBaseCode;
    }

    internal int GetCompatibilityAccentCode()
    {
        EnsureCompatibilityComposite();
        return compatibilityAccentCode;
    }

    void EnsureCompatibilityComposite()
    {
        if (!compatibilityComposite)
        {
            throw new System.InvalidOperationException(
                "The Type 2 charstring does not use compatibility composition.");
        }
    }
}

internal class Type2OperandStack
{
    double[] values;
    int count;

    internal Type2OperandStack(int maximumCount)
    {
        if (maximumCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumCount");
        }

        values = new double[maximumCount];
    }

    internal static Type2OperandStack Create()
    {
        return new Type2OperandStack(48);
    }

    internal static Type2OperandStack Create(int maximumCount)
    {
        return new Type2OperandStack(maximumCount);
    }

    internal int GetCount()
    {
        return count;
    }

    internal double Get(int index)
    {
        if (index < 0 || index >= count)
        {
            throw FontFormatException.Create(
                "A Type 2 operator accessed an unavailable operand.");
        }

        return values[index];
    }

    internal void Set(int index, double value)
    {
        if (index < 0 || index >= count)
        {
            throw FontFormatException.Create(
                "A Type 2 operator accessed an unavailable operand.");
        }

        values[index] = value;
    }

    internal void Push(double value)
    {
        if (!double.IsFinite(value))
        {
            throw FontFormatException.Create(
                "A Type 2 operand is outside the supported numeric range.");
        }

        if (count >= values.Length)
        {
            throw FontLimitException.Create(
                "A Type 2 operand stack exceeds its format limit.");
        }

        values[count] = value;
        count += 1;
    }

    internal double Pop()
    {
        if (count == 0)
        {
            throw FontFormatException.Create(
                "A Type 2 operand stack underflowed.");
        }

        count -= 1;
        return values[count];
    }

    internal double Peek()
    {
        return Get(count - 1);
    }

    internal double RemoveFirst()
    {
        double value = Get(0);
        int index = 1;

        while (index < count)
        {
            values[index - 1] = values[index];
            index += 1;
        }

        count -= 1;
        return value;
    }

    internal void Clear()
    {
        count = 0;
    }

    internal void RemoveTop(int removeCount)
    {
        if (removeCount < 0 || removeCount > count)
        {
            throw FontFormatException.Create(
                "A Type 2 operator removes unavailable operands.");
        }

        count -= removeCount;
    }
}

internal class Type2CharStringExecutor
{
    const int MaximumStemCount = 96;
    const int DefaultMaximumOperationCount = 1000000;
    Type2OperandStack stack;
    double[] transientValues;
    System.Collections.Generic.List<Type2DrawingCommand> commands;
    System.Collections.Generic.List<Type2StemHint> stems;
    System.Collections.Generic.List<Type2HintMask> masks;
    double nominalWidth;
    double defaultWidth;
    double width;
    bool widthResolved;
    bool explicitWidth;
    bool ended;
    bool hasCurrentContour;
    double currentX;
    double currentY;
    uint randomState;
    CffIndex? localSubroutines;
    CffIndex? globalSubroutines;
    Cff2Index? cff2LocalSubroutines;
    Cff2Index? cff2GlobalSubroutines;
    Cff2VariationStore? variationStore;
    Cff2VariationLocation? variationLocation;
    int variationDataIndex;
    bool charStringVariationDataIndexSeen;
    bool blendSeen;
    bool cff2;
    bool[] activeLocalSubroutines;
    bool[] activeGlobalSubroutines;
    int maximumSubroutineDepth;
    int maximumOperationCount;
    int operationCount;
    bool compatibilityComposite;
    double compatibilityAccentX;
    double compatibilityAccentY;
    int compatibilityBaseCode;
    int compatibilityAccentCode;
    bool deprecatedOperator;

    Type2CharStringExecutor(int operandStackSize)
    {
        stack = Type2OperandStack.Create(operandStackSize);
        transientValues = new double[32];
        commands = new System.Collections.Generic.List<Type2DrawingCommand>();
        stems = new System.Collections.Generic.List<Type2StemHint>();
        masks = new System.Collections.Generic.List<Type2HintMask>();
        activeLocalSubroutines = new bool[0];
        activeGlobalSubroutines = new bool[0];
    }

    void InitializeCff1(
        double nominalWidth,
        double defaultWidth,
        int initialRandomSeed,
        CffIndex? localSubroutines,
        CffIndex? globalSubroutines,
        int maximumSubroutineDepth,
        int maximumOperationCount)
    {
        if (!double.IsFinite(nominalWidth) ||
            !double.IsFinite(defaultWidth))
        {
            throw new System.ArgumentOutOfRangeException("nominalWidth");
        }

        if (maximumSubroutineDepth <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumSubroutineDepth");
        }

        if (maximumOperationCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumOperationCount");
        }

        this.nominalWidth = nominalWidth;
        this.defaultWidth = defaultWidth;
        this.localSubroutines = localSubroutines;
        this.globalSubroutines = globalSubroutines;
        activeLocalSubroutines = new bool[
            localSubroutines == null ? 0 : localSubroutines.GetCount()];
        activeGlobalSubroutines = new bool[
            globalSubroutines == null ? 0 : globalSubroutines.GetCount()];
        this.maximumSubroutineDepth = maximumSubroutineDepth;
        this.maximumOperationCount = maximumOperationCount;
        width = defaultWidth;
        randomState = CheckedFontMath.ReinterpretInt32AsUInt32(
            initialRandomSeed);

        if (randomState == 0)
        {
            randomState = 0x12345678U;
        }
    }

    void InitializeCff2(
        int initialVariationDataIndex,
        Cff2Index? localSubroutines,
        Cff2Index globalSubroutines,
        Cff2VariationStore? variationStore,
        double[]? normalizedCoordinates,
        int maximumSubroutineDepth,
        int maximumOperationCount)
    {
        if (initialVariationDataIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "initialVariationDataIndex");
        }

        if (maximumSubroutineDepth <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumSubroutineDepth");
        }

        if (maximumOperationCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumOperationCount");
        }

        if (variationStore == null)
        {
            if (normalizedCoordinates == null ||
                normalizedCoordinates.Length != 0 ||
                initialVariationDataIndex != 0)
            {
                throw new System.ArgumentException(
                    "CFF2 variation inputs require a VariationStore.",
                    "normalizedCoordinates");
            }
        }
        else
        {
            variationLocation = variationStore.CreateLocation(
                normalizedCoordinates);
            variationStore.GetVariationData(initialVariationDataIndex);
        }

        this.cff2LocalSubroutines = localSubroutines;
        this.cff2GlobalSubroutines = globalSubroutines;
        this.variationStore = variationStore;
        variationDataIndex = initialVariationDataIndex;
        activeLocalSubroutines = new bool[
            localSubroutines == null ? 0 : localSubroutines.GetCount()];
        activeGlobalSubroutines = new bool[
            globalSubroutines == null ? 0 : globalSubroutines.GetCount()];
        this.maximumSubroutineDepth = maximumSubroutineDepth;
        this.maximumOperationCount = maximumOperationCount;
        cff2 = true;
        widthResolved = true;
    }

    static Type2CharStringExecutor Create(
        double nominalWidth,
        double defaultWidth,
        int initialRandomSeed,
        CffIndex? localSubroutines,
        CffIndex? globalSubroutines,
        int maximumSubroutineDepth,
        int maximumOperationCount)
    {
        Type2CharStringExecutor executor =
            new Type2CharStringExecutor(48);
        executor.InitializeCff1(
            nominalWidth,
            defaultWidth,
            initialRandomSeed,
            localSubroutines,
            globalSubroutines,
            maximumSubroutineDepth,
            maximumOperationCount);
        return executor;
    }

    static Type2CharStringExecutor Create(
        int initialVariationDataIndex,
        Cff2Index? localSubroutines,
        Cff2Index globalSubroutines,
        Cff2VariationStore? variationStore,
        double[]? normalizedCoordinates,
        int maximumSubroutineDepth,
        int maximumOperationCount)
    {
        Type2CharStringExecutor executor =
            new Type2CharStringExecutor(513);
        executor.InitializeCff2(
            initialVariationDataIndex,
            localSubroutines,
            globalSubroutines,
            variationStore,
            normalizedCoordinates,
            maximumSubroutineDepth,
            maximumOperationCount);
        return executor;
    }

    internal static Type2CharStringResult Execute(
        FontDataSlice data,
        double nominalWidth,
        double defaultWidth,
        int initialRandomSeed)
    {
        return Execute(
            data,
            nominalWidth,
            defaultWidth,
            initialRandomSeed,
            null,
            null,
            10,
            DefaultMaximumOperationCount);
    }

    internal static Type2CharStringResult Execute(
        FontDataSlice data,
        double nominalWidth,
        double defaultWidth,
        int initialRandomSeed,
        CffIndex? localSubroutines,
        CffIndex? globalSubroutines,
        int maximumSubroutineDepth,
        int maximumOperationCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        Type2CharStringExecutor executor = Type2CharStringExecutor.Create(
            nominalWidth,
            defaultWidth,
            initialRandomSeed,
            localSubroutines,
            globalSubroutines,
            maximumSubroutineDepth,
            maximumOperationCount);
        return executor.Run(data);
    }

    internal static Type2CharStringResult ExecuteCff2(
        FontDataSlice data,
        int initialVariationDataIndex,
        Cff2Index? localSubroutines,
        Cff2Index globalSubroutines,
        Cff2VariationStore? variationStore,
        double[]? normalizedCoordinates,
        int maximumSubroutineDepth,
        int maximumOperationCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        Type2CharStringExecutor executor = Type2CharStringExecutor.Create(
            initialVariationDataIndex,
            localSubroutines,
            globalSubroutines,
            variationStore,
            normalizedCoordinates,
            maximumSubroutineDepth,
            maximumOperationCount);
        return executor.Run(data);
    }

    internal static int GetSubroutineBias(int subroutineCount)
    {
        if (subroutineCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("subroutineCount");
        }

        if (subroutineCount < 1240)
        {
            return 107;
        }

        if (subroutineCount < 33900)
        {
            return 1131;
        }

        return 32768;
    }

    internal static int GetDefaultMaximumOperationCount()
    {
        return DefaultMaximumOperationCount;
    }

    Type2CharStringResult Run(FontDataSlice data)
    {
        ExecuteProgram(data, false, 0);

        if (!ended && !cff2)
        {
            throw FontFormatException.Create(
                "A Type 2 charstring does not end with endchar.");
        }

        return new Type2CharStringResult(
            width,
            explicitWidth,
            commands.ToArray(),
            stems.ToArray(),
            masks.ToArray(),
            compatibilityComposite,
            compatibilityAccentX,
            compatibilityAccentY,
            compatibilityBaseCode,
            compatibilityAccentCode,
            deprecatedOperator);
    }

    void ExecuteProgram(
        FontDataSlice data,
        bool subroutine,
        int subroutineDepth)
    {
        BigEndianReader reader = new BigEndianReader(data);
        bool returned = false;

        while (reader.GetRemainingLength() != 0 && !ended && !returned)
        {
            int firstByte = reader.ReadUInt8();

            if (firstByte == 28)
            {
                stack.Push(reader.ReadInt16());
            }
            else if (firstByte >= 32 && firstByte <= 246)
            {
                stack.Push(firstByte - 139);
            }
            else if (firstByte >= 247 && firstByte <= 250)
            {
                stack.Push((firstByte - 247) * 256 +
                    reader.ReadUInt8() + 108);
            }
            else if (firstByte >= 251 && firstByte <= 254)
            {
                stack.Push(-((firstByte - 251) * 256) -
                    reader.ReadUInt8() - 108);
            }
            else if (firstByte == 255)
            {
                stack.Push(reader.ReadInt32() / 65536.0);
            }
            else
            {
                returned = ExecuteOperator(
                    firstByte,
                    reader,
                    subroutine,
                    subroutineDepth);
            }
        }

        if (cff2)
        {
            if (returned)
            {
                throw FontFormatException.Create(
                    "A CFF2 charstring contains the reserved return operator.");
            }

            if (!subroutine)
            {
                if (stack.GetCount() != 0)
                {
                    throw FontFormatException.Create(
                        "A CFF2 charstring ends with unused operands.");
                }

                ended = true;
            }

            return;
        }

        if (subroutine)
        {
            if (ended)
            {
                if (reader.GetRemainingLength() != 0)
                {
                    throw FontFormatException.Create(
                        "A Type 2 subroutine contains data after endchar.");
                }

                return;
            }

            if (!returned)
            {
                throw FontFormatException.Create(
                    "A Type 2 subroutine does not end with return.");
            }

            if (reader.GetRemainingLength() != 0)
            {
                throw FontFormatException.Create(
                    "A Type 2 subroutine contains data after return.");
            }

            return;
        }

        if (reader.GetRemainingLength() != 0)
        {
            throw FontFormatException.Create(
                "A Type 2 charstring contains data after endchar.");
        }
    }

    bool ExecuteOperator(
        int operatorCode,
        BigEndianReader reader,
        bool subroutine,
        int subroutineDepth)
    {
        CountOperation();

        if (operatorCode == 12)
        {
            ExecuteEscapedOperator(reader.ReadUInt8());
            return false;
        }

        if (cff2 && operatorCode == 15)
        {
            SelectVariationData();
            return false;
        }

        if (cff2 && operatorCode == 16)
        {
            BlendOperands();
            return false;
        }

        if (operatorCode == 1 || operatorCode == 18)
        {
            AddStems(false);
            return false;
        }

        if (operatorCode == 3 || operatorCode == 23)
        {
            AddStems(true);
            return false;
        }

        if (operatorCode == 19 || operatorCode == 20)
        {
            PrepareHintMaskStems();
            ReadHintMask(reader, operatorCode == 20);
            return false;
        }

        if (operatorCode == 4)
        {
            MoveVertical();
            return false;
        }

        if (operatorCode == 5)
        {
            AddRelativeLines();
            return false;
        }

        if (operatorCode == 6 || operatorCode == 7)
        {
            AddAlternatingLines(operatorCode == 6);
            return false;
        }

        if (operatorCode == 8)
        {
            AddRelativeCurves();
            return false;
        }

        if (operatorCode == 21)
        {
            MoveRelative();
            return false;
        }

        if (operatorCode == 22)
        {
            MoveHorizontal();
            return false;
        }

        if (operatorCode == 24)
        {
            AddCurveLine();
            return false;
        }

        if (operatorCode == 25)
        {
            AddLineCurve();
            return false;
        }

        if (operatorCode == 26 || operatorCode == 27)
        {
            AddSingleAxisCurves(operatorCode == 27);
            return false;
        }

        if (operatorCode == 30 || operatorCode == 31)
        {
            AddAlternatingCurves(operatorCode == 31);
            return false;
        }

        if (operatorCode == 14)
        {
            if (cff2)
            {
                throw FontFormatException.Create(
                    "A CFF2 charstring contains the reserved endchar operator.");
            }

            EndCharString();
            return false;
        }

        if (operatorCode == 10 || operatorCode == 29)
        {
            CallSubroutine(
                operatorCode == 29,
                subroutineDepth);
            return false;
        }

        if (operatorCode == 11)
        {
            if (cff2)
            {
                throw FontFormatException.Create(
                    "A CFF2 charstring contains the reserved return operator.");
            }

            if (!subroutine)
            {
                throw FontFormatException.Create(
                    "A Type 2 return operator occurs outside a subroutine.");
            }

            return true;
        }

        if (cff2)
        {
            stack.Clear();
            return false;
        }

        throw FontFormatException.Create(
            "A Type 2 charstring contains a reserved operator.");
    }

    void ExecuteEscapedOperator(int operatorCode)
    {
        if (cff2 && operatorCode != 34 && operatorCode != 35 &&
            operatorCode != 36 && operatorCode != 37)
        {
            stack.Clear();
            return;
        }

        if (operatorCode == 3)
        {
            bool second = PopBoolean();
            bool first = PopBoolean();
            PushBoolean(first && second);
            return;
        }

        if (operatorCode == 4)
        {
            bool second = PopBoolean();
            bool first = PopBoolean();
            PushBoolean(first || second);
            return;
        }

        if (operatorCode == 5)
        {
            PushBoolean(!PopBoolean());
            return;
        }

        if (operatorCode == 9)
        {
            stack.Push(System.Math.Abs(stack.Pop()));
            return;
        }

        if (operatorCode == 10)
        {
            AddNumbers();
            return;
        }

        if (operatorCode == 11)
        {
            SubtractNumbers();
            return;
        }

        if (operatorCode == 12)
        {
            DivideNumbers();
            return;
        }

        if (operatorCode == 14)
        {
            stack.Push(-stack.Pop());
            return;
        }

        if (operatorCode == 15)
        {
            double second = stack.Pop();
            double first = stack.Pop();
            PushBoolean(first == second);
            return;
        }

        if (operatorCode == 18)
        {
            stack.Pop();
            return;
        }

        if (operatorCode == 20)
        {
            PutTransient();
            return;
        }

        if (operatorCode == 21)
        {
            GetTransient();
            return;
        }

        if (operatorCode == 22)
        {
            IfElse();
            return;
        }

        if (operatorCode == 23)
        {
            PushRandom();
            return;
        }

        if (operatorCode == 24)
        {
            MultiplyNumbers();
            return;
        }

        if (operatorCode == 26)
        {
            SquareRoot();
            return;
        }

        if (operatorCode == 27)
        {
            stack.Push(stack.Peek());
            return;
        }

        if (operatorCode == 28)
        {
            Exchange();
            return;
        }

        if (operatorCode == 29)
        {
            Index();
            return;
        }

        if (operatorCode == 30)
        {
            Roll();
            return;
        }

        if (operatorCode == 0)
        {
            deprecatedOperator = true;
            return;
        }

        if (operatorCode == 34)
        {
            AddHorizontalFlex();
            return;
        }

        if (operatorCode == 35)
        {
            AddFlex();
            return;
        }

        if (operatorCode == 36)
        {
            AddHorizontalFlexOne();
            return;
        }

        if (operatorCode == 37)
        {
            AddFlexOne();
            return;
        }

        throw FontFormatException.Create(
            "A Type 2 charstring contains a reserved escaped operator.");
    }

    void ResolveWidth(int requiredOperandCount)
    {
        if (cff2)
        {
            if (stack.GetCount() != requiredOperandCount)
            {
                throw FontFormatException.Create(
                    "A CFF2 drawing operator has an invalid operand count.");
            }

            return;
        }

        if (widthResolved)
        {
            if (stack.GetCount() != requiredOperandCount)
            {
                throw FontFormatException.Create(
                    "A Type 2 drawing operator has an invalid operand count.");
            }

            return;
        }

        if (stack.GetCount() == requiredOperandCount + 1)
        {
            width = nominalWidth + stack.RemoveFirst();
            explicitWidth = true;
        }
        else if (stack.GetCount() == requiredOperandCount)
        {
            width = defaultWidth;
        }
        else
        {
            throw FontFormatException.Create(
                "A Type 2 width or drawing operator has an invalid operand count.");
        }

        EnsureFinite(width);
        widthResolved = true;
    }

    void ResolveStemWidth()
    {
        if (cff2)
        {
            if (stack.GetCount() == 0 || stack.GetCount() % 2 != 0)
            {
                throw FontFormatException.Create(
                    "A CFF2 stem operator requires operand pairs.");
            }

            return;
        }

        if (!widthResolved)
        {
            if (stack.GetCount() % 2 != 0)
            {
                width = nominalWidth + stack.RemoveFirst();
                explicitWidth = true;
            }
            else
            {
                width = defaultWidth;
            }

            EnsureFinite(width);
            widthResolved = true;
        }

        if (stack.GetCount() == 0 || stack.GetCount() % 2 != 0)
        {
            throw FontFormatException.Create(
                "A Type 2 stem operator requires operand pairs.");
        }
    }

    void AddStems(bool vertical)
    {
        ResolveStemWidth();
        int index = 0;

        while (index < stack.GetCount())
        {
            if (stems.Count >= MaximumStemCount)
            {
                throw FontLimitException.Create(
                    "A Type 2 charstring exceeds 96 stem hints.");
            }

            stems.Add(new Type2StemHint(
                vertical,
                stack.Get(index),
                stack.Get(index + 1)));
            index += 2;
        }

        stack.Clear();
    }

    void PrepareHintMaskStems()
    {
        if (stack.GetCount() != 0)
        {
            AddStems(true);
            return;
        }

        if (!widthResolved)
        {
            width = defaultWidth;
            widthResolved = true;
        }
    }

    void ReadHintMask(BigEndianReader reader, bool counterMask)
    {
        int byteCount = (stems.Count + 7) / 8;
        byte[] bytes = new byte[byteCount];
        int index = 0;

        while (index < bytes.Length)
        {
            bytes[index] = reader.ReadUInt8();
            index += 1;
        }

        masks.Add(new Type2HintMask(
            counterMask,
            bytes,
            commands.Count));
    }

    void MoveVertical()
    {
        ResolveWidth(1);
        currentY += stack.Get(0);
        AddMove();
        stack.Clear();
    }

    void MoveRelative()
    {
        ResolveWidth(2);
        currentX += stack.Get(0);
        currentY += stack.Get(1);
        AddMove();
        stack.Clear();
    }

    void MoveHorizontal()
    {
        ResolveWidth(1);
        currentX += stack.Get(0);
        AddMove();
        stack.Clear();
    }

    void AddMove()
    {
        commands.Add(Type2DrawingCommand.Create(
            Type2DrawingCommandKind.Move,
            currentX,
            currentY));
        hasCurrentContour = true;
    }

    void AddRelativeLines()
    {
        EnsureDrawingStarted();

        if (stack.GetCount() < 2 || stack.GetCount() % 2 != 0)
        {
            throw FontFormatException.Create(
                "Type 2 rlineto requires one or more coordinate pairs.");
        }

        int index = 0;

        while (index < stack.GetCount())
        {
            AddLine(stack.Get(index), stack.Get(index + 1));
            index += 2;
        }

        stack.Clear();
    }

    void AddAlternatingLines(bool horizontal)
    {
        EnsureDrawingStarted();

        if (stack.GetCount() == 0)
        {
            throw FontFormatException.Create(
                "A Type 2 alternating line operator requires operands.");
        }

        int index = 0;

        while (index < stack.GetCount())
        {
            if (horizontal)
            {
                AddLine(stack.Get(index), 0);
            }
            else
            {
                AddLine(0, stack.Get(index));
            }

            horizontal = !horizontal;
            index += 1;
        }

        stack.Clear();
    }

    void AddRelativeCurves()
    {
        EnsureDrawingStarted();

        if (stack.GetCount() < 6 || stack.GetCount() % 6 != 0)
        {
            throw FontFormatException.Create(
                "Type 2 rrcurveto requires groups of six operands.");
        }

        int index = 0;

        while (index < stack.GetCount())
        {
            AddCurve(
                stack.Get(index),
                stack.Get(index + 1),
                stack.Get(index + 2),
                stack.Get(index + 3),
                stack.Get(index + 4),
                stack.Get(index + 5));
            index += 6;
        }

        stack.Clear();
    }

    void AddCurveLine()
    {
        EnsureDrawingStarted();
        int count = stack.GetCount();

        if (count < 8 || (count - 2) % 6 != 0)
        {
            throw FontFormatException.Create(
                "Type 2 rcurveline has an invalid operand count.");
        }

        int index = 0;

        while (index < count - 2)
        {
            AddCurve(
                stack.Get(index),
                stack.Get(index + 1),
                stack.Get(index + 2),
                stack.Get(index + 3),
                stack.Get(index + 4),
                stack.Get(index + 5));
            index += 6;
        }

        AddLine(stack.Get(count - 2), stack.Get(count - 1));
        stack.Clear();
    }

    void AddLineCurve()
    {
        EnsureDrawingStarted();
        int count = stack.GetCount();

        if (count < 8 || (count - 6) % 2 != 0)
        {
            throw FontFormatException.Create(
                "Type 2 rlinecurve has an invalid operand count.");
        }

        int index = 0;

        while (index < count - 6)
        {
            AddLine(stack.Get(index), stack.Get(index + 1));
            index += 2;
        }

        AddCurve(
            stack.Get(count - 6),
            stack.Get(count - 5),
            stack.Get(count - 4),
            stack.Get(count - 3),
            stack.Get(count - 2),
            stack.Get(count - 1));
        stack.Clear();
    }

    void AddSingleAxisCurves(bool horizontal)
    {
        EnsureDrawingStarted();
        int count = stack.GetCount();

        if (count < 4 || (count % 4 != 0 && count % 4 != 1))
        {
            throw FontFormatException.Create(
                "A Type 2 single-axis curve operator has an invalid operand count.");
        }

        int index = 0;
        double optional = 0;

        if (count % 4 == 1)
        {
            optional = stack.Get(0);
            index = 1;
        }

        while (index < count)
        {
            if (horizontal)
            {
                AddCurve(
                    stack.Get(index),
                    optional,
                    stack.Get(index + 1),
                    stack.Get(index + 2),
                    stack.Get(index + 3),
                    0);
            }
            else
            {
                AddCurve(
                    optional,
                    stack.Get(index),
                    stack.Get(index + 1),
                    stack.Get(index + 2),
                    0,
                    stack.Get(index + 3));
            }

            optional = 0;
            index += 4;
        }

        stack.Clear();
    }

    void AddAlternatingCurves(bool horizontal)
    {
        EnsureDrawingStarted();
        int count = stack.GetCount();

        if (count < 4 || (count % 4 != 0 && count % 4 != 1))
        {
            throw FontFormatException.Create(
                "A Type 2 alternating curve operator has an invalid operand count.");
        }

        int index = 0;

        while (index + 3 < count)
        {
            bool finalCurve = count - index <= 5;
            double optional = finalCurve && count - index == 5 ?
                stack.Get(index + 4) : 0;

            if (horizontal)
            {
                AddCurve(
                    stack.Get(index),
                    0,
                    stack.Get(index + 1),
                    stack.Get(index + 2),
                    optional,
                    stack.Get(index + 3));
            }
            else
            {
                AddCurve(
                    0,
                    stack.Get(index),
                    stack.Get(index + 1),
                    stack.Get(index + 2),
                    stack.Get(index + 3),
                    optional);
            }

            index += 4;

            if (finalCurve && count - index == 1)
            {
                index += 1;
            }

            horizontal = !horizontal;
        }

        if (index != count)
        {
            throw FontFormatException.Create(
                "A Type 2 alternating curve has unused operands.");
        }

        stack.Clear();
    }

    void AddHorizontalFlex()
    {
        EnsureDrawingStarted();

        if (stack.GetCount() != 7)
        {
            throw FontFormatException.Create(
                "Type 2 hflex requires seven operands.");
        }

        double vertical = stack.Get(2);
        AddCurve(
            stack.Get(0),
            0,
            stack.Get(1),
            vertical,
            stack.Get(3),
            0);
        AddCurve(
            stack.Get(4),
            0,
            stack.Get(5),
            -vertical,
            stack.Get(6),
            0);
        stack.Clear();
    }

    void AddFlex()
    {
        EnsureDrawingStarted();

        if (stack.GetCount() != 13)
        {
            throw FontFormatException.Create(
                "Type 2 flex requires thirteen operands.");
        }

        AddCurve(
            stack.Get(0),
            stack.Get(1),
            stack.Get(2),
            stack.Get(3),
            stack.Get(4),
            stack.Get(5));
        AddCurve(
            stack.Get(6),
            stack.Get(7),
            stack.Get(8),
            stack.Get(9),
            stack.Get(10),
            stack.Get(11));
        stack.Clear();
    }

    void AddHorizontalFlexOne()
    {
        EnsureDrawingStarted();

        if (stack.GetCount() != 9)
        {
            throw FontFormatException.Create(
                "Type 2 hflex1 requires nine operands.");
        }

        double finalVertical = -(
            stack.Get(1) +
            stack.Get(3) +
            stack.Get(7));
        AddCurve(
            stack.Get(0),
            stack.Get(1),
            stack.Get(2),
            stack.Get(3),
            stack.Get(4),
            0);
        AddCurve(
            stack.Get(5),
            0,
            stack.Get(6),
            stack.Get(7),
            stack.Get(8),
            finalVertical);
        stack.Clear();
    }

    void AddFlexOne()
    {
        EnsureDrawingStarted();

        if (stack.GetCount() != 11)
        {
            throw FontFormatException.Create(
                "Type 2 flex1 requires eleven operands.");
        }

        double horizontalSum = stack.Get(0) +
            stack.Get(2) +
            stack.Get(4) +
            stack.Get(6) +
            stack.Get(8);
        double verticalSum = stack.Get(1) +
            stack.Get(3) +
            stack.Get(5) +
            stack.Get(7) +
            stack.Get(9);
        double finalHorizontal;
        double finalVertical;

        if (System.Math.Abs(horizontalSum) > System.Math.Abs(verticalSum))
        {
            finalHorizontal = stack.Get(10);
            finalVertical = -verticalSum;
        }
        else
        {
            finalHorizontal = -horizontalSum;
            finalVertical = stack.Get(10);
        }

        AddCurve(
            stack.Get(0),
            stack.Get(1),
            stack.Get(2),
            stack.Get(3),
            stack.Get(4),
            stack.Get(5));
        AddCurve(
            stack.Get(6),
            stack.Get(7),
            stack.Get(8),
            stack.Get(9),
            finalHorizontal,
            finalVertical);
        stack.Clear();
    }

    void AddLine(double deltaX, double deltaY)
    {
        currentX += deltaX;
        currentY += deltaY;
        EnsureFinite(currentX);
        EnsureFinite(currentY);
        commands.Add(Type2DrawingCommand.Create(
            Type2DrawingCommandKind.Line,
            currentX,
            currentY));
    }

    void AddCurve(
        double firstDeltaX,
        double firstDeltaY,
        double secondDeltaX,
        double secondDeltaY,
        double thirdDeltaX,
        double thirdDeltaY)
    {
        double firstX = currentX + firstDeltaX;
        double firstY = currentY + firstDeltaY;
        double secondX = firstX + secondDeltaX;
        double secondY = firstY + secondDeltaY;
        double endX = secondX + thirdDeltaX;
        double endY = secondY + thirdDeltaY;
        EnsureFinite(firstX);
        EnsureFinite(firstY);
        EnsureFinite(secondX);
        EnsureFinite(secondY);
        EnsureFinite(endX);
        EnsureFinite(endY);
        commands.Add(Type2DrawingCommand.Create(
            firstX,
            firstY,
            secondX,
            secondY,
            endX,
            endY));
        currentX = endX;
        currentY = endY;
    }

    void EnsureDrawingStarted()
    {
        if (!hasCurrentContour)
        {
            throw FontFormatException.Create(
                "A Type 2 path operator occurs before a moveto.");
        }
    }

    void EndCharString()
    {
        bool hasCompatibilityOperands = stack.GetCount() == 4 ||
            (!widthResolved && stack.GetCount() == 5);

        if (hasCompatibilityOperands)
        {
            ResolveWidth(4);
            SetCompatibilityComposite();
        }
        else if (!widthResolved)
        {
            ResolveWidth(0);
        }
        else if (stack.GetCount() != 0)
        {
            throw FontFormatException.Create(
                "A Type 2 endchar operator has unexpected operands.");
        }

        stack.Clear();
        ended = true;
    }

    void SetCompatibilityComposite()
    {
        if (commands.Count != 0)
        {
            throw FontFormatException.Create(
                "A Type 2 compatibility composite must not also draw a path.");
        }

        int baseCode = ReadInteger(stack.Get(2));
        int accentCode = ReadInteger(stack.Get(3));

        if (baseCode < 0 || baseCode > 255 ||
            accentCode < 0 || accentCode > 255)
        {
            throw FontFormatException.Create(
                "A Type 2 compatibility composite uses an invalid character code.");
        }

        compatibilityComposite = true;
        deprecatedOperator = true;
        compatibilityAccentX = stack.Get(0);
        compatibilityAccentY = stack.Get(1);
        compatibilityBaseCode = baseCode;
        compatibilityAccentCode = accentCode;
    }

    void AddNumbers()
    {
        double second = stack.Pop();
        double first = stack.Pop();
        double result = first + second;
        EnsureFinite(result);
        stack.Push(result);
    }

    void SubtractNumbers()
    {
        double second = stack.Pop();
        double first = stack.Pop();
        double result = first - second;
        EnsureFinite(result);
        stack.Push(result);
    }

    void DivideNumbers()
    {
        double divisor = stack.Pop();
        double dividend = stack.Pop();

        if (divisor == 0)
        {
            throw FontFormatException.Create(
                "A Type 2 div operator divides by zero.");
        }

        double result = dividend / divisor;
        EnsureFinite(result);
        stack.Push(result);
    }

    void MultiplyNumbers()
    {
        double second = stack.Pop();
        double first = stack.Pop();
        double result = first * second;
        EnsureFinite(result);
        stack.Push(result);
    }

    void SquareRoot()
    {
        double value = stack.Pop();

        if (value < 0)
        {
            throw FontFormatException.Create(
                "A Type 2 sqrt operator has a negative operand.");
        }

        stack.Push(System.Math.Sqrt(value));
    }

    bool PopBoolean()
    {
        return stack.Pop() != 0;
    }

    void PushBoolean(bool value)
    {
        stack.Push(value ? 1 : 0);
    }

    void PutTransient()
    {
        int index = ReadInteger(stack.Pop());
        double value = stack.Pop();
        ValidateTransientIndex(index);
        transientValues[index] = value;
    }

    void GetTransient()
    {
        int index = ReadInteger(stack.Pop());
        ValidateTransientIndex(index);
        stack.Push(transientValues[index]);
    }

    void IfElse()
    {
        double secondComparison = stack.Pop();
        double firstComparison = stack.Pop();
        double secondValue = stack.Pop();
        double firstValue = stack.Pop();
        stack.Push(firstComparison <= secondComparison ?
            firstValue : secondValue);
    }

    void PushRandom()
    {
        ulong nextState = (ulong)randomState * 1664525UL + 1013904223UL;
        randomState = (uint)(nextState & 0xFFFFFFFFUL);
        stack.Push((randomState + 1.0) / 4294967296.0);
    }

    void Exchange()
    {
        double top = stack.Pop();
        double second = stack.Pop();
        stack.Push(top);
        stack.Push(second);
    }

    void Index()
    {
        int index = ReadInteger(stack.Pop());

        if (stack.GetCount() == 0)
        {
            throw FontFormatException.Create(
                "A Type 2 index operator has no value to copy.");
        }

        if (index < 0)
        {
            index = 0;
        }

        if (index >= stack.GetCount())
        {
            index = stack.GetCount() - 1;
        }

        stack.Push(stack.Get(stack.GetCount() - 1 - index));
    }

    void Roll()
    {
        int amount = ReadInteger(stack.Pop());
        int count = ReadInteger(stack.Pop());

        if (count <= 0 || count > stack.GetCount())
        {
            throw FontFormatException.Create(
                "A Type 2 roll operator has an invalid element count.");
        }

        int shift = amount % count;

        if (shift < 0)
        {
            shift += count;
        }

        double[] values = new double[count];
        int start = stack.GetCount() - count;
        int index = 0;

        while (index < count)
        {
            values[(index + shift) % count] = stack.Get(start + index);
            index += 1;
        }

        index = 0;

        while (index < count)
        {
            stack.Set(start + index, values[index]);
            index += 1;
        }
    }

    void SelectVariationData()
    {
        if (variationStore == null ||
            charStringVariationDataIndexSeen ||
            blendSeen ||
            stack.GetCount() != 1)
        {
            throw FontFormatException.Create(
                "A CFF2 charstring has an invalid vsindex operator.");
        }

        int selectedIndex = ReadInteger(stack.Pop());

        if (selectedIndex < 0)
        {
            throw FontFormatException.Create(
                "A CFF2 charstring has a negative vsindex operand.");
        }

        variationStore.GetVariationData(selectedIndex);
        variationDataIndex = selectedIndex;
        charStringVariationDataIndexSeen = true;
    }

    void BlendOperands()
    {
        if (variationStore == null || stack.GetCount() == 0)
        {
            throw FontFormatException.Create(
                "A CFF2 blend operator requires variation operands.");
        }

        int blendCount = ReadInteger(stack.Peek());

        if (blendCount <= 0)
        {
            throw FontFormatException.Create(
                "A CFF2 blend operator has an invalid result count.");
        }

        System.ArgumentNullException.ThrowIfNull(variationLocation);
        double[] scalars = variationLocation.CopyRegionScalars(
            variationDataIndex);
        long required = (long)blendCount * (scalars.Length + 1) + 1;

        if (required > stack.GetCount())
        {
            throw FontFormatException.Create(
                "A CFF2 blend operator does not have enough operands.");
        }

        int start = stack.GetCount() - (int)required;
        double[] results = new double[blendCount];
        int valueIndex = 0;

        while (valueIndex < results.Length)
        {
            double value = stack.Get(start + valueIndex);
            int scalarIndex = 0;

            while (scalarIndex < scalars.Length)
            {
                int deltaIndex = start + blendCount +
                    valueIndex * scalars.Length + scalarIndex;
                value += stack.Get(deltaIndex) * scalars[scalarIndex];
                scalarIndex += 1;
            }

            EnsureFinite(value);
            results[valueIndex] = value;
            valueIndex += 1;
        }

        stack.RemoveTop((int)required);
        valueIndex = 0;

        while (valueIndex < results.Length)
        {
            stack.Push(results[valueIndex]);
            valueIndex += 1;
        }

        blendSeen = true;
    }

    void CallSubroutine(bool global, int subroutineDepth)
    {
        if (subroutineDepth >= maximumSubroutineDepth)
        {
            throw FontLimitException.Create(
                "A Type 2 charstring exceeds the configured subroutine depth.");
        }

        int operand = ReadInteger(stack.Pop());
        bool[] active = global ?
            activeGlobalSubroutines : activeLocalSubroutines;
        CffIndex? index = global ? globalSubroutines : localSubroutines;
        Cff2Index? cff2Index = global ?
            cff2GlobalSubroutines : cff2LocalSubroutines;
        int count = cff2 ?
            (cff2Index == null ? 0 : cff2Index.GetCount()) :
            (index == null ? 0 : index.GetCount());
        long biasedIndex = (long)operand + GetSubroutineBias(count);

        if ((!cff2 && index == null) ||
            (cff2 && cff2Index == null) ||
            biasedIndex < 0 ||
            biasedIndex >= count)
        {
            throw FontFormatException.Create(
                "A Type 2 charstring calls a subroutine outside its INDEX.");
        }

        int resolvedIndex = (int)biasedIndex;

        if (active[resolvedIndex])
        {
            throw FontFormatException.Create(
                "A Type 2 charstring contains recursive subroutine calls.");
        }

        active[resolvedIndex] = true;
        FontDataSlice subroutine;
        if (cff2)
        {
            System.ArgumentNullException.ThrowIfNull(cff2Index);
            subroutine = cff2Index.GetObject(resolvedIndex);
        }
        else
        {
            System.ArgumentNullException.ThrowIfNull(index);
            subroutine = index.GetObject(resolvedIndex);
        }
        ExecuteProgram(subroutine, true, subroutineDepth + 1);
        active[resolvedIndex] = false;
    }

    void CountOperation()
    {
        if (operationCount >= maximumOperationCount)
        {
            throw FontLimitException.Create(
                "A Type 2 charstring exceeds its operation limit.");
        }

        operationCount += 1;
    }

    int ReadInteger(double value)
    {
        if (value < int.MinValue ||
            value > int.MaxValue ||
            value != (int)value)
        {
            throw FontFormatException.Create(
                "A Type 2 operator requires an integer operand.");
        }

        return (int)value;
    }

    void ValidateTransientIndex(int index)
    {
        if (index < 0 || index >= transientValues.Length)
        {
            throw FontFormatException.Create(
                "A Type 2 transient-array index is outside 0 through 31.");
        }
    }

    void EnsureFinite(double value)
    {
        if (!double.IsFinite(value))
        {
            throw FontFormatException.Create(
                "A Type 2 calculation is outside the supported numeric range.");
        }
    }
}
