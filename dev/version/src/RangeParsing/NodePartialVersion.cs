namespace version;

internal class NodePartialVersion
{
    internal bool valid;
    internal bool range;
    internal bool any;
    internal int componentCount;
    internal string major;
    internal string minor;
    internal string patch;
    internal bool majorWildcard;
    internal bool minorWildcard;
    internal bool patchWildcard;

    internal NodePartialVersion(
        bool validValue,
        bool rangeValue,
        bool anyValue,
        int componentCountValue,
        string majorValue,
        string minorValue,
        string patchValue,
        bool majorWildcardValue,
        bool minorWildcardValue,
        bool patchWildcardValue)
    {
        System.ArgumentNullException.ThrowIfNull(majorValue);
        System.ArgumentNullException.ThrowIfNull(minorValue);
        System.ArgumentNullException.ThrowIfNull(patchValue);

        valid = validValue;
        range = rangeValue;
        any = anyValue;
        componentCount = componentCountValue;
        major = majorValue;
        minor = minorValue;
        patch = patchValue;
        majorWildcard = majorWildcardValue;
        minorWildcard = minorWildcardValue;
        patchWildcard = patchWildcardValue;
    }

    internal static NodePartialVersion Parse(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        string normalized = StripLeadingV(text);
        if (normalized.Length == 0)
        {
            return CreateInvalid(false);
        }

        if (ContainsVersionSuffix(normalized))
        {
            return CreateInvalid(false);
        }

        string[] components = SplitComponents(normalized);
        if (components.Length == 0 || components.Length > 3)
        {
            return CreateInvalid(ContainsWildcard(normalized));
        }

        if (components.Length == 1 && IsWildcard(components[0]))
        {
            return new NodePartialVersion(true, true, true, 1, "0", "0", "0", true, true, true);
        }

        bool hasWildcard = false;
        bool invalid = false;
        string major = "0";
        string minor = "0";
        string patch = "0";
        bool majorWildcard = false;
        bool minorWildcard = false;
        bool patchWildcard = false;

        if (components.Length > 0)
        {
            majorWildcard = IsWildcard(components[0]);
            major = NormalizeComponent(components[0]);
            hasWildcard = hasWildcard || majorWildcard;
            invalid = invalid || !IsValidComponent(components[0]);
        }

        if (components.Length > 1)
        {
            minorWildcard = IsWildcard(components[1]);
            minor = NormalizeComponent(components[1]);
            hasWildcard = hasWildcard || minorWildcard;
            invalid = invalid || !IsValidComponent(components[1]);
        }

        if (components.Length > 2)
        {
            patchWildcard = IsWildcard(components[2]);
            patch = NormalizeComponent(components[2]);
            hasWildcard = hasWildcard || patchWildcard;
            invalid = invalid || !IsValidComponent(components[2]);
        }

        if (invalid)
        {
            return CreateInvalid(hasWildcard || components.Length < 3);
        }

        bool range = hasWildcard || components.Length < 3;
        return new NodePartialVersion(true, range, false, components.Length, major, minor, patch, majorWildcard, minorWildcard, patchWildcard);
    }

    internal bool IsValid()
    {
        return valid;
    }

    internal bool IsRange()
    {
        return range;
    }

    internal bool IsAny()
    {
        return any || majorWildcard;
    }

    internal int GetComponentCount()
    {
        return componentCount;
    }

    internal string GetMajor()
    {
        return major;
    }

    internal string GetMinor()
    {
        return minor;
    }

    internal string GetPatch()
    {
        return patch;
    }

    internal bool HasMajorWildcard()
    {
        return majorWildcard;
    }

    internal bool HasMinorWildcard()
    {
        return minorWildcard;
    }

    internal bool HasPatchWildcard()
    {
        return patchWildcard;
    }

    internal bool IsMajorOnly()
    {
        return componentCount == 1 || minorWildcard;
    }

    internal bool IsMinorOnly()
    {
        return componentCount == 2 || patchWildcard;
    }

    internal static NodePartialVersion CreateInvalid(bool rangeValue)
    {
        return new NodePartialVersion(false, rangeValue, false, 0, "0", "0", "0", false, false, false);
    }

    internal static string StripLeadingV(string text)
    {
        if (text.Length > 1 && VersionChar.IsV(text[0]))
        {
            return text.Substring(1);
        }

        return text;
    }

    internal static bool ContainsVersionSuffix(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '-' || text[index] == '+')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string[] SplitComponents(string text)
    {
        System.Collections.Generic.List<string> components = new System.Collections.Generic.List<string>();
        int startIndex = 0;
        int index = 0;

        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == '.')
            {
                components.Add(text.Substring(startIndex, index - startIndex));
                startIndex = index + 1;
            }

            index = index + 1;
        }

        return components.ToArray();
    }

    internal static bool ContainsWildcard(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (IsWildcardCharacter(text[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsValidComponent(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        if (IsWildcard(text))
        {
            return true;
        }

        return ContainsOnlyDigits(text);
    }

    internal static string NormalizeComponent(string text)
    {
        if (IsWildcard(text))
        {
            return "0";
        }

        return text;
    }

    internal static bool IsWildcard(string text)
    {
        if (text == "x")
        {
            return true;
        }

        if (text == "X")
        {
            return true;
        }

        if (text == "*")
        {
            return true;
        }

        return false;
    }

    internal static bool IsWildcardCharacter(char value)
    {
        if (value == 'x')
        {
            return true;
        }

        if (value == 'X')
        {
            return true;
        }

        if (value == '*')
        {
            return true;
        }

        return false;
    }

    internal static bool ContainsOnlyDigits(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (!VersionChar.IsDigit(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
