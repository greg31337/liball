namespace ttf;

internal enum ThaiLegacyPuaPolicy
{
    Disabled = 0,
    WhenOpenTypeSubstitutionUnavailable = 1
}

internal class ThaiLegacyPuaCoverage
{
    int[] codePoints;

    internal ThaiLegacyPuaCoverage(int[] codePoints)
    {
        if (codePoints == null)
        {
            throw new System.ArgumentNullException("codePoints");
        }

        this.codePoints = new int[codePoints.Length];
        int index = 0;
        while (index < codePoints.Length)
        {
            if (!System.Text.Rune.IsValid(codePoints[index]))
            {
                throw new System.ArgumentOutOfRangeException("codePoints");
            }

            this.codePoints[index] = codePoints[index];
            index++;
        }
    }

    internal bool HasCodePoint(int codePoint)
    {
        int index = 0;
        while (index < codePoints.Length)
        {
            if (codePoints[index] == codePoint)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}

internal class ThaiLegacyPuaResult
{
    int[] replacements;
    int[] unsafeStarts;
    int replacementCount;

    internal ThaiLegacyPuaResult(
        int[] replacements,
        int[] unsafeStarts,
        int replacementCount)
    {
        this.replacements = replacements;
        this.unsafeStarts = unsafeStarts;
        this.replacementCount = replacementCount;
    }

    internal int GetCount()
    {
        return replacements.Length;
    }

    internal int GetReplacementCount()
    {
        return replacementCount;
    }

    internal bool HasReplacement(int index)
    {
        ValidateIndex(index);
        return replacements[index] >= 0;
    }

    internal int GetReplacement(int index)
    {
        ValidateIndex(index);
        if (replacements[index] < 0)
        {
            throw new System.InvalidOperationException(
                "No legacy Thai replacement is available at this index.");
        }

        return replacements[index];
    }

    internal int GetUnsafeStart(int index)
    {
        ValidateIndex(index);
        return unsafeStarts[index];
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= replacements.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

internal enum ThaiLegacyConsonantType
{
    Normal = 0,
    Ascending = 1,
    RemovableDescender = 2,
    StrictDescender = 3,
    NotConsonant = 4
}

internal enum ThaiLegacyMarkType
{
    AboveVowel = 0,
    BelowVowel = 1,
    Tone = 2,
    NotMark = 3
}

internal enum ThaiLegacyAction
{
    None = 0,
    ShiftDown = 1,
    ShiftLeft = 2,
    ShiftDownLeft = 3,
    RemoveDescender = 4
}

internal enum ThaiLegacyAboveState
{
    Level0 = 0,
    Level1 = 1,
    Level2 = 2,
    Level3 = 3
}

internal enum ThaiLegacyBelowState
{
    NoDescender = 0,
    RemovableDescender = 1,
    StrictDescender = 2
}

internal static class ThaiLegacyPuaShaper
{
    internal static ThaiLegacyPuaResult Prepare(
        ShapingBuffer buffer,
        int[] sourceIndexes,
        ThaiLegacyPuaCoverage coverage)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (sourceIndexes == null)
        {
            throw new System.ArgumentNullException("sourceIndexes");
        }

        if (coverage == null)
        {
            throw new System.ArgumentNullException("coverage");
        }

        if (sourceIndexes.Length != buffer.GetCount())
        {
            throw new System.ArgumentException(
                "The Thai fallback order must describe every buffer item.");
        }

        int count = sourceIndexes.Length;
        int[] replacements = new int[count];
        int[] unsafeStarts = new int[count];
        int index = 0;
        while (index < count)
        {
            replacements[index] = -1;
            unsafeStarts[index] = -1;
            index++;
        }

        ThaiLegacyAboveState aboveState = ThaiLegacyAboveState.Level3;
        ThaiLegacyBelowState belowState =
            ThaiLegacyBelowState.StrictDescender;
        int baseIndex = 0;
        index = 0;
        while (index < count)
        {
            int codePoint = buffer.GetScalarValue(sourceIndexes[index]);
            ThaiLegacyMarkType markType = GetMarkType(codePoint);
            if (markType == ThaiLegacyMarkType.NotMark)
            {
                ThaiLegacyConsonantType consonantType =
                    GetConsonantType(codePoint);
                aboveState = GetAboveStartState(consonantType);
                belowState = GetBelowStartState(consonantType);
                baseIndex = index;
                index++;
                continue;
            }

            ThaiLegacyAction aboveAction = GetAboveAction(
                aboveState,
                markType);
            ThaiLegacyAction belowAction = GetBelowAction(
                belowState,
                markType);
            aboveState = GetNextAboveState(aboveState, markType);
            belowState = GetNextBelowState(belowState, markType);
            ThaiLegacyAction action = aboveAction != ThaiLegacyAction.None ?
                aboveAction : belowAction;
            unsafeStarts[index] = baseIndex;
            if (action == ThaiLegacyAction.RemoveDescender)
            {
                int baseCodePoint = buffer.GetScalarValue(
                    sourceIndexes[baseIndex]);
                int replacement = GetReplacement(
                    baseCodePoint,
                    action,
                    coverage);
                if (replacement != baseCodePoint)
                {
                    replacements[baseIndex] = replacement;
                }
            }
            else
            {
                int replacement = GetReplacement(
                    codePoint,
                    action,
                    coverage);
                if (replacement != codePoint)
                {
                    replacements[index] = replacement;
                }
            }

            index++;
        }

        int replacementCount = 0;
        index = 0;
        while (index < replacements.Length)
        {
            if (replacements[index] >= 0)
            {
                replacementCount++;
            }

            index++;
        }

        return new ThaiLegacyPuaResult(
            replacements,
            unsafeStarts,
            replacementCount);
    }

    static ThaiLegacyConsonantType GetConsonantType(int codePoint)
    {
        if (codePoint == 0x0E1B || codePoint == 0x0E1D ||
            codePoint == 0x0E1F)
        {
            return ThaiLegacyConsonantType.Ascending;
        }

        if (codePoint == 0x0E0D || codePoint == 0x0E10)
        {
            return ThaiLegacyConsonantType.RemovableDescender;
        }

        if (codePoint == 0x0E0E || codePoint == 0x0E0F)
        {
            return ThaiLegacyConsonantType.StrictDescender;
        }

        if (codePoint >= 0x0E01 && codePoint <= 0x0E2E)
        {
            return ThaiLegacyConsonantType.Normal;
        }

        return ThaiLegacyConsonantType.NotConsonant;
    }

    static ThaiLegacyMarkType GetMarkType(int codePoint)
    {
        if (codePoint == 0x0E31 ||
            (codePoint >= 0x0E34 && codePoint <= 0x0E37) ||
            codePoint == 0x0E47 ||
            (codePoint >= 0x0E4D && codePoint <= 0x0E4E))
        {
            return ThaiLegacyMarkType.AboveVowel;
        }

        if (codePoint >= 0x0E38 && codePoint <= 0x0E3A)
        {
            return ThaiLegacyMarkType.BelowVowel;
        }

        if (codePoint >= 0x0E48 && codePoint <= 0x0E4C)
        {
            return ThaiLegacyMarkType.Tone;
        }

        return ThaiLegacyMarkType.NotMark;
    }

    static ThaiLegacyAboveState GetAboveStartState(
        ThaiLegacyConsonantType type)
    {
        if (type == ThaiLegacyConsonantType.Ascending)
        {
            return ThaiLegacyAboveState.Level1;
        }

        if (type == ThaiLegacyConsonantType.NotConsonant)
        {
            return ThaiLegacyAboveState.Level3;
        }

        return ThaiLegacyAboveState.Level0;
    }

    static ThaiLegacyBelowState GetBelowStartState(
        ThaiLegacyConsonantType type)
    {
        if (type == ThaiLegacyConsonantType.RemovableDescender)
        {
            return ThaiLegacyBelowState.RemovableDescender;
        }

        if (type == ThaiLegacyConsonantType.StrictDescender ||
            type == ThaiLegacyConsonantType.NotConsonant)
        {
            return ThaiLegacyBelowState.StrictDescender;
        }

        return ThaiLegacyBelowState.NoDescender;
    }

    static ThaiLegacyAction GetAboveAction(
        ThaiLegacyAboveState state,
        ThaiLegacyMarkType mark)
    {
        if (state == ThaiLegacyAboveState.Level0 &&
            mark == ThaiLegacyMarkType.Tone)
        {
            return ThaiLegacyAction.ShiftDown;
        }

        if (state == ThaiLegacyAboveState.Level1 &&
            mark == ThaiLegacyMarkType.AboveVowel)
        {
            return ThaiLegacyAction.ShiftLeft;
        }

        if (state == ThaiLegacyAboveState.Level1 &&
            mark == ThaiLegacyMarkType.Tone)
        {
            return ThaiLegacyAction.ShiftDownLeft;
        }

        if (state == ThaiLegacyAboveState.Level2 &&
            mark == ThaiLegacyMarkType.Tone)
        {
            return ThaiLegacyAction.ShiftLeft;
        }

        return ThaiLegacyAction.None;
    }

    static ThaiLegacyAction GetBelowAction(
        ThaiLegacyBelowState state,
        ThaiLegacyMarkType mark)
    {
        if (state == ThaiLegacyBelowState.RemovableDescender &&
            mark == ThaiLegacyMarkType.BelowVowel)
        {
            return ThaiLegacyAction.RemoveDescender;
        }

        if (state == ThaiLegacyBelowState.StrictDescender &&
            mark == ThaiLegacyMarkType.BelowVowel)
        {
            return ThaiLegacyAction.ShiftDown;
        }

        return ThaiLegacyAction.None;
    }

    static ThaiLegacyAboveState GetNextAboveState(
        ThaiLegacyAboveState state,
        ThaiLegacyMarkType mark)
    {
        if (state == ThaiLegacyAboveState.Level0)
        {
            return mark == ThaiLegacyMarkType.BelowVowel ?
                ThaiLegacyAboveState.Level0 :
                ThaiLegacyAboveState.Level3;
        }

        if (state == ThaiLegacyAboveState.Level1)
        {
            return mark == ThaiLegacyMarkType.BelowVowel ?
                ThaiLegacyAboveState.Level1 :
                ThaiLegacyAboveState.Level2;
        }

        if (state == ThaiLegacyAboveState.Level2)
        {
            return mark == ThaiLegacyMarkType.BelowVowel ?
                ThaiLegacyAboveState.Level2 :
                ThaiLegacyAboveState.Level3;
        }

        return ThaiLegacyAboveState.Level3;
    }

    static ThaiLegacyBelowState GetNextBelowState(
        ThaiLegacyBelowState state,
        ThaiLegacyMarkType mark)
    {
        if (state == ThaiLegacyBelowState.NoDescender)
        {
            return mark == ThaiLegacyMarkType.BelowVowel ?
                ThaiLegacyBelowState.StrictDescender :
                ThaiLegacyBelowState.NoDescender;
        }

        if (state == ThaiLegacyBelowState.RemovableDescender)
        {
            return mark == ThaiLegacyMarkType.BelowVowel ?
                ThaiLegacyBelowState.StrictDescender :
                ThaiLegacyBelowState.RemovableDescender;
        }

        return ThaiLegacyBelowState.StrictDescender;
    }

    static int GetReplacement(
        int codePoint,
        ThaiLegacyAction action,
        ThaiLegacyPuaCoverage coverage)
    {
        if (action == ThaiLegacyAction.None)
        {
            return codePoint;
        }

        int windows = GetWindowsMapping(codePoint, action);
        if (windows != 0 && coverage.HasCodePoint(windows))
        {
            return windows;
        }

        int mac = GetMacMapping(codePoint, action);
        if (mac != 0 && coverage.HasCodePoint(mac))
        {
            return mac;
        }

        return codePoint;
    }

    static int GetWindowsMapping(int codePoint, ThaiLegacyAction action)
    {
        if (action == ThaiLegacyAction.ShiftDown)
        {
            return GetShiftDownWindows(codePoint);
        }

        if (action == ThaiLegacyAction.ShiftDownLeft)
        {
            return GetShiftDownLeftWindows(codePoint);
        }

        if (action == ThaiLegacyAction.ShiftLeft)
        {
            return GetShiftLeftWindows(codePoint);
        }

        if (action == ThaiLegacyAction.RemoveDescender)
        {
            if (codePoint == 0x0E0D)
            {
                return 0xF70F;
            }

            if (codePoint == 0x0E10)
            {
                return 0xF700;
            }
        }

        return 0;
    }

    static int GetMacMapping(int codePoint, ThaiLegacyAction action)
    {
        if (action == ThaiLegacyAction.ShiftDown)
        {
            return GetShiftDownMac(codePoint);
        }

        if (action == ThaiLegacyAction.ShiftDownLeft)
        {
            return GetShiftDownLeftMac(codePoint);
        }

        if (action == ThaiLegacyAction.ShiftLeft)
        {
            return GetShiftLeftMac(codePoint);
        }

        if (action == ThaiLegacyAction.RemoveDescender)
        {
            if (codePoint == 0x0E0D)
            {
                return 0xF89A;
            }

            if (codePoint == 0x0E10)
            {
                return 0xF89E;
            }
        }

        return 0;
    }

    static int GetShiftDownWindows(int codePoint)
    {
        if (codePoint >= 0x0E48 && codePoint <= 0x0E4C)
        {
            return 0xF70A + codePoint - 0x0E48;
        }

        if (codePoint >= 0x0E38 && codePoint <= 0x0E3A)
        {
            return 0xF718 + codePoint - 0x0E38;
        }

        return 0;
    }

    static int GetShiftDownMac(int codePoint)
    {
        if (codePoint >= 0x0E48 && codePoint <= 0x0E4C)
        {
            return 0xF88B + (codePoint - 0x0E48) * 3;
        }

        if (codePoint >= 0x0E38 && codePoint <= 0x0E3A)
        {
            return 0xF89B + codePoint - 0x0E38;
        }

        return 0;
    }

    static int GetShiftDownLeftWindows(int codePoint)
    {
        if (codePoint >= 0x0E48 && codePoint <= 0x0E4C)
        {
            return 0xF705 + codePoint - 0x0E48;
        }

        return 0;
    }

    static int GetShiftDownLeftMac(int codePoint)
    {
        if (codePoint >= 0x0E48 && codePoint <= 0x0E4C)
        {
            return 0xF88C + (codePoint - 0x0E48) * 3;
        }

        return 0;
    }

    static int GetShiftLeftWindows(int codePoint)
    {
        if (codePoint >= 0x0E48 && codePoint <= 0x0E4C)
        {
            return 0xF713 + codePoint - 0x0E48;
        }

        if (codePoint == 0x0E31)
        {
            return 0xF710;
        }

        if (codePoint >= 0x0E34 && codePoint <= 0x0E37)
        {
            return 0xF701 + codePoint - 0x0E34;
        }

        if (codePoint == 0x0E47)
        {
            return 0xF712;
        }

        if (codePoint == 0x0E4D)
        {
            return 0xF711;
        }

        return 0;
    }

    static int GetShiftLeftMac(int codePoint)
    {
        if (codePoint >= 0x0E48 && codePoint <= 0x0E4C)
        {
            return 0xF88A + (codePoint - 0x0E48) * 3;
        }

        if (codePoint == 0x0E31)
        {
            return 0xF884;
        }

        if (codePoint >= 0x0E34 && codePoint <= 0x0E37)
        {
            return 0xF885 + codePoint - 0x0E34;
        }

        if (codePoint == 0x0E47)
        {
            return 0xF889;
        }

        if (codePoint == 0x0E4D)
        {
            return 0xF899;
        }

        return 0;
    }
}
