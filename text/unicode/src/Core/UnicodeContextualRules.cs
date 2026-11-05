namespace unicode;

internal class UnicodeContextualRules
{
    internal static bool RequiresContext(int codePoint)
    {
        if (codePoint == 0x200C || codePoint == 0x200D)
        {
            return true;
        }

        if (codePoint == 0x00B7 || codePoint == 0x0375)
        {
            return true;
        }

        if (codePoint == 0x05F3 || codePoint == 0x05F4 || codePoint == 0x30FB)
        {
            return true;
        }

        if (codePoint >= 0x0660 && codePoint <= 0x0669)
        {
            return true;
        }

        return codePoint >= 0x06F0 && codePoint <= 0x06F9;
    }

    internal static bool IsValidContext(UnicodeScalarRecord[] scalars, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int codePoint = scalars[index].CodePoint;
        if (codePoint == 0x200D)
        {
            return HasViramaBefore(scalars, index);
        }

        if (codePoint == 0x200C)
        {
            if (HasViramaBefore(scalars, index))
            {
                return true;
            }

            return HasJoiningContext(scalars, index);
        }

        if (codePoint == 0x00B7)
        {
            if (index == 0 || index + 1 >= scalars.Length)
            {
                return false;
            }

            return scalars[index - 1].CodePoint == 0x006C && scalars[index + 1].CodePoint == 0x006C;
        }

        if (codePoint == 0x0375)
        {
            if (index + 1 >= scalars.Length)
            {
                return false;
            }

            return UnicodeProperties.Instance.GetScript(scalars[index + 1].CodePoint) == UnicodeScript.Greek;
        }

        if (codePoint == 0x05F3 || codePoint == 0x05F4)
        {
            if (index == 0)
            {
                return false;
            }

            return UnicodeProperties.Instance.GetScript(scalars[index - 1].CodePoint) == UnicodeScript.Hebrew;
        }

        if (codePoint == 0x30FB)
        {
            return ContainsJapaneseScript(scalars);
        }

        if (codePoint >= 0x0660 && codePoint <= 0x0669)
        {
            return !ContainsCodePointRange(scalars, 0x06F0, 0x06F9);
        }

        if (codePoint >= 0x06F0 && codePoint <= 0x06F9)
        {
            return !ContainsCodePointRange(scalars, 0x0660, 0x0669);
        }

        return true;
    }

    internal static bool HasViramaBefore(UnicodeScalarRecord[] scalars, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        if (index == 0)
        {
            return false;
        }

        return UnicodeProperties.Instance.GetCombiningClass(scalars[index - 1].CodePoint) == 9;
    }

    internal static bool HasJoiningContext(UnicodeScalarRecord[] scalars, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int before = index - 1;
        while (before >= 0)
        {
            UnicodeJoiningType beforeType = UnicodeProperties.Instance.GetJoiningType(scalars[before].CodePoint);
            if (beforeType != UnicodeJoiningType.Transparent)
            {
                bool joinsBefore = beforeType == UnicodeJoiningType.LeftJoining;
                if (beforeType == UnicodeJoiningType.DualJoining)
                {
                    joinsBefore = true;
                }

                if (!joinsBefore)
                {
                    return false;
                }

                break;
            }

            before = before - 1;
        }

        if (before < 0)
        {
            return false;
        }

        int after = index + 1;
        while (after < scalars.Length)
        {
            UnicodeJoiningType afterType = UnicodeProperties.Instance.GetJoiningType(scalars[after].CodePoint);
            if (afterType != UnicodeJoiningType.Transparent)
            {
                if (afterType == UnicodeJoiningType.RightJoining)
                {
                    return true;
                }

                return afterType == UnicodeJoiningType.DualJoining;
            }

            after = after + 1;
        }

        return false;
    }

    internal static bool ContainsJapaneseScript(UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int index = 0;
        while (index < scalars.Length)
        {
            UnicodeScript script = UnicodeProperties.Instance.GetScript(scalars[index].CodePoint);
            if (script == UnicodeScript.Hiragana || script == UnicodeScript.Katakana || script == UnicodeScript.Han)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsCodePointRange(UnicodeScalarRecord[] scalars, int start, int end)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index].CodePoint >= start && scalars[index].CodePoint <= end)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
