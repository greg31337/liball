namespace cbor;

internal class CddlValidationBudget
{
    internal int MaxNestingDepth;
    internal int MaxSteps;
    internal int MaxAlternatives;
    internal int MaxMemoEntries;
    internal int MaxEmbeddedBytes;
    internal int MaxPatternStates;
    internal int NestingDepth;
    internal int Steps;
    internal int Alternatives;
    internal int MemoEntries;
    internal int EmbeddedBytes;
    internal int PatternStates;

    internal CddlValidationBudget(CddlValidationOptions options)
    {
        MaxNestingDepth = options.MaxNestingDepth;
        MaxSteps = options.MaxSteps;
        MaxAlternatives = options.MaxAlternatives;
        MaxMemoEntries = options.MaxMemoEntries;
        MaxEmbeddedBytes = options.MaxEmbeddedBytes;
        MaxPatternStates = options.MaxPatternStates;
    }

    internal bool TakeStep()
    {
        if (!CanTake(Steps, 1, MaxSteps))
        {
            return false;
        }
        Steps = Steps + 1;
        return true;
    }

    internal bool TakeAlternative()
    {
        if (!CanTake(Alternatives, 1, MaxAlternatives))
        {
            return false;
        }
        Alternatives = Alternatives + 1;
        return true;
    }

    internal bool EnterNesting()
    {
        if (!CanTake(NestingDepth, 1, MaxNestingDepth))
        {
            return false;
        }
        NestingDepth = NestingDepth + 1;
        return true;
    }

    internal void LeaveNesting()
    {
        if (NestingDepth > 0)
        {
            NestingDepth = NestingDepth - 1;
        }
    }

    internal bool TakeMemoEntry()
    {
        if (!CanTake(MemoEntries, 1, MaxMemoEntries))
        {
            return false;
        }
        MemoEntries = MemoEntries + 1;
        return true;
    }

    internal bool TakeEmbeddedBytes(int count)
    {
        if (!CanTake(EmbeddedBytes, count, MaxEmbeddedBytes))
        {
            return false;
        }
        EmbeddedBytes = EmbeddedBytes + count;
        return true;
    }

    internal bool TakePatternStates(int count)
    {
        if (!CanTake(PatternStates, count, MaxPatternStates))
        {
            return false;
        }
        PatternStates = PatternStates + count;
        return true;
    }

    internal bool CanTake(int current, int count, int maximum)
    {
        if (count < 0 || maximum < 0 || current > maximum)
        {
            return false;
        }
        return count <= maximum - current;
    }
}
