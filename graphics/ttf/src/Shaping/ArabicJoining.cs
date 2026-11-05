namespace ttf;

internal enum ArabicShapingScript
{
    Arabic = 0,
    Syriac = 1,
    Mongolian = 2
}

internal enum ArabicJoiningType
{
    NonJoining = 0,
    LeftJoining = 1,
    RightJoining = 2,
    DualJoining = 3,
    JoinCausing = 4,
    Transparent = 5
}

internal enum ArabicJoiningAction
{
    Isolated = 0,
    Final = 1,
    Final2 = 2,
    Final3 = 3,
    Medial = 4,
    Medial2 = 5,
    Initial = 6,
    None = 7
}

internal class ArabicJoiningAnalysis
{
    ArabicJoiningType[] joiningTypes;
    string[] joiningGroups;
    ArabicJoiningAction[] actions;

    internal ArabicJoiningAnalysis(
        ArabicJoiningType[] joiningTypes,
        string[] joiningGroups,
        ArabicJoiningAction[] actions)
    {
        this.joiningTypes = joiningTypes;
        this.joiningGroups = joiningGroups;
        this.actions = actions;
    }

    internal int GetCount()
    {
        return actions.Length;
    }

    internal ArabicJoiningType GetJoiningType(int index)
    {
        ValidateIndex(index);
        return joiningTypes[index];
    }

    internal string GetJoiningGroup(int index)
    {
        ValidateIndex(index);
        return joiningGroups[index];
    }

    internal ArabicJoiningAction GetAction(int index)
    {
        ValidateIndex(index);
        return actions[index];
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= actions.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

internal static class ArabicJoiningAnalyzer
{
    const int ColumnCount = 6;
    const int NonJoiningColumn = 0;
    const int LeftJoiningColumn = 1;
    const int RightJoiningColumn = 2;
    const int DualJoiningColumn = 3;
    const int AlaphColumn = 4;
    const int DalathRishColumn = 5;
    const int MongolianVariationSelectorStart = 0x180B;
    const int MongolianVariationSelectorEnd = 0x180D;
    const int MongolianFreeVariationSelectorFour = 0x180F;

    static readonly int[] PreviousActions = new int[]
    {
        7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7,
        7, 7, 6, 6, 6, 6,
        7, 7, 4, 4, 4, 4,
        7, 7, 5, 5, 5, 5,
        7, 7, 0, 0, 0, 0,
        7, 7, 7, 7, 7, 7
    };

    static readonly int[] CurrentActions = new int[]
    {
        7, 0, 0, 0, 0, 0,
        7, 0, 0, 0, 2, 0,
        7, 0, 1, 1, 1, 1,
        7, 0, 1, 1, 1, 1,
        7, 0, 0, 0, 2, 0,
        7, 0, 0, 0, 2, 0,
        7, 0, 0, 0, 3, 0
    };

    static readonly int[] NextStates = new int[]
    {
        0, 2, 1, 2, 1, 6,
        0, 2, 1, 2, 5, 6,
        0, 2, 1, 3, 4, 6,
        0, 2, 1, 3, 4, 6,
        0, 2, 1, 2, 5, 6,
        0, 2, 1, 2, 5, 6,
        0, 2, 1, 2, 5, 6
    };

    internal static ArabicJoiningAnalysis Analyze(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        ArabicShapingScript script)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        ValidateScript(script);
        int count = buffer.GetCount();
        ArabicJoiningType[] joiningTypes =
            new ArabicJoiningType[count];
        string[] joiningGroups = new string[count];
        ArabicJoiningAction[] actions =
            new ArabicJoiningAction[count];
        int state = 0;
        int previousIndex = -1;
        int index = 0;

        while (index < count)
        {
            int codePoint = buffer.GetScalarValue(index);
            string joiningTypeName = unicodeData.GetPropertyName(
                UnicodeDataTags.JoiningType,
                codePoint);
            string joiningGroup = unicodeData.GetPropertyName(
                UnicodeDataTags.JoiningGroup,
                codePoint);
            ArabicJoiningType joiningType = ConvertJoiningType(
                joiningTypeName);
            joiningTypes[index] = joiningType;
            joiningGroups[index] = joiningGroup;
            actions[index] = ArabicJoiningAction.None;

            if (joiningType == ArabicJoiningType.Transparent)
            {
                index++;
                continue;
            }

            int column = GetColumn(joiningType, joiningGroup);
            int transitionIndex = state * ColumnCount + column;
            ArabicJoiningAction previousAction =
                (ArabicJoiningAction)PreviousActions[transitionIndex];
            if (previousIndex >= 0 &&
                previousAction != ArabicJoiningAction.None)
            {
                actions[previousIndex] = previousAction;
            }

            actions[index] = (ArabicJoiningAction)
                CurrentActions[transitionIndex];
            previousIndex = index;
            state = NextStates[transitionIndex];
            index++;
        }

        if (script == ArabicShapingScript.Mongolian)
        {
            PropagateMongolianVariationActions(buffer, actions);
        }

        return new ArabicJoiningAnalysis(
            joiningTypes,
            joiningGroups,
            actions);
    }

    static int GetColumn(
        ArabicJoiningType joiningType,
        string joiningGroup)
    {
        if (joiningGroup == "Alaph")
        {
            return AlaphColumn;
        }

        if (joiningGroup == "Dalath_Rish")
        {
            return DalathRishColumn;
        }

        if (joiningType == ArabicJoiningType.LeftJoining)
        {
            return LeftJoiningColumn;
        }

        if (joiningType == ArabicJoiningType.RightJoining)
        {
            return RightJoiningColumn;
        }

        if (joiningType == ArabicJoiningType.DualJoining ||
            joiningType == ArabicJoiningType.JoinCausing)
        {
            return DualJoiningColumn;
        }

        return NonJoiningColumn;
    }

    static ArabicJoiningType ConvertJoiningType(string value)
    {
        if (value == "U")
        {
            return ArabicJoiningType.NonJoining;
        }

        if (value == "L")
        {
            return ArabicJoiningType.LeftJoining;
        }

        if (value == "R")
        {
            return ArabicJoiningType.RightJoining;
        }

        if (value == "D")
        {
            return ArabicJoiningType.DualJoining;
        }

        if (value == "C")
        {
            return ArabicJoiningType.JoinCausing;
        }

        if (value == "T")
        {
            return ArabicJoiningType.Transparent;
        }

        throw FontFormatException.Create(
            "The Unicode joining type is not supported.");
    }

    static void PropagateMongolianVariationActions(
        ShapingBuffer buffer,
        ArabicJoiningAction[] actions)
    {
        int index = 1;
        while (index < buffer.GetCount())
        {
            int codePoint = buffer.GetScalarValue(index);
            if ((codePoint >= MongolianVariationSelectorStart &&
                codePoint <= MongolianVariationSelectorEnd) ||
                codePoint == MongolianFreeVariationSelectorFour)
            {
                actions[index] = actions[index - 1];
            }

            index++;
        }
    }

    static void ValidateScript(ArabicShapingScript script)
    {
        if (script != ArabicShapingScript.Arabic &&
            script != ArabicShapingScript.Syriac &&
            script != ArabicShapingScript.Mongolian)
        {
            throw new System.ArgumentOutOfRangeException("script");
        }
    }
}
