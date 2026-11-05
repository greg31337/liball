namespace version;

internal class CargoPartialVersion
{
    internal bool valid;
    internal bool wildcard;
    internal bool any;
    internal int componentCount;
    internal int missingComponentCount;
    internal string major;
    internal string minor;
    internal string patch;
    internal VersionIdentifier[] prereleaseIdentifiers;
    internal VersionIdentifier[] buildIdentifiers;

    internal CargoPartialVersion(
        bool validValue,
        bool wildcardValue,
        bool anyValue,
        int componentCountValue,
        int missingComponentCountValue,
        string majorValue,
        string minorValue,
        string patchValue,
        VersionIdentifier[] prereleaseIdentifierValues,
        VersionIdentifier[] buildIdentifierValues)
    {
        System.ArgumentNullException.ThrowIfNull(majorValue);
        System.ArgumentNullException.ThrowIfNull(minorValue);
        System.ArgumentNullException.ThrowIfNull(patchValue);
        System.ArgumentNullException.ThrowIfNull(prereleaseIdentifierValues);
        System.ArgumentNullException.ThrowIfNull(buildIdentifierValues);

        valid = validValue;
        wildcard = wildcardValue;
        any = anyValue;
        componentCount = componentCountValue;
        missingComponentCount = missingComponentCountValue;
        major = majorValue;
        minor = minorValue;
        patch = patchValue;
        prereleaseIdentifiers = CopyIdentifiers(prereleaseIdentifierValues);
        buildIdentifiers = CopyIdentifiers(buildIdentifierValues);
    }

    internal static CargoPartialVersion Parse(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        int suffixIndex = FindSuffixIndex(text);
        string coreText = text;
        if (suffixIndex >= 0)
        {
            coreText = text.Substring(0, suffixIndex);
        }

        string[] components = SplitComponents(coreText);
        if (components.Length == 0 || components.Length > 3)
        {
            return CreateInvalid();
        }

        bool hasWildcard = ContainsWildcard(components);

        if (suffixIndex >= 0 && (components.Length < 3 || hasWildcard))
        {
            return CreateInvalid();
        }

        if (IsWildcardComponent(components[0]))
        {
            if (components.Length != 1)
            {
                return CreateInvalid();
            }

            return new CargoPartialVersion(
                true,
                true,
                true,
                1,
                3,
                "0",
                "0",
                "0",
                new VersionIdentifier[0],
                new VersionIdentifier[0]);
        }

        if (!IsValidNumericComponent(components[0]))
        {
            return CreateInvalid();
        }

        string major = components[0];
        string minor = "0";
        string patch = "0";

        if (components.Length > 1)
        {
            if (IsWildcardComponent(components[1]))
            {
                if (components.Length != 2)
                {
                    return CreateInvalid();
                }

                return new CargoPartialVersion(
                    true,
                    true,
                    false,
                    2,
                    2,
                    major,
                    "0",
                    "0",
                    new VersionIdentifier[0],
                    new VersionIdentifier[0]);
            }

            if (!IsValidNumericComponent(components[1]))
            {
                return CreateInvalid();
            }

            minor = components[1];
        }

        if (components.Length > 2)
        {
            if (IsWildcardComponent(components[2]))
            {
                return new CargoPartialVersion(
                    true,
                    true,
                    false,
                    3,
                    1,
                    major,
                    minor,
                    "0",
                    new VersionIdentifier[0],
                    new VersionIdentifier[0]);
            }

            if (!IsValidNumericComponent(components[2]))
            {
                return CreateInvalid();
            }

            patch = components[2];
        }

        VersionIdentifier[] prereleaseIdentifiers = new VersionIdentifier[0];
        VersionIdentifier[] buildIdentifiers = new VersionIdentifier[0];

        if (suffixIndex >= 0)
        {
            VersionParseResult result = SemVerParser.ParseStrict(text);
            if (result.IsFailure())
            {
                return CreateInvalid();
            }

            prereleaseIdentifiers = result.GetVersion().GetPrereleaseIdentifiers();
            buildIdentifiers = result.GetVersion().GetBuildIdentifiers();
        }

        return new CargoPartialVersion(
            true,
            false,
            false,
            components.Length,
            3 - components.Length,
            major,
            minor,
            patch,
            prereleaseIdentifiers,
            buildIdentifiers);
    }

    internal bool IsValid()
    {
        return valid;
    }

    internal bool IsWildcard()
    {
        return wildcard;
    }

    internal bool IsAny()
    {
        return any;
    }

    internal int GetComponentCount()
    {
        return componentCount;
    }

    internal int GetMissingComponentCount()
    {
        return missingComponentCount;
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

    internal VersionIdentifier[] GetPrereleaseIdentifiers()
    {
        return CopyIdentifiers(prereleaseIdentifiers);
    }

    internal VersionIdentifier[] GetBuildIdentifiers()
    {
        return CopyIdentifiers(buildIdentifiers);
    }

    internal static CargoPartialVersion CreateInvalid()
    {
        return new CargoPartialVersion(false, false, false, 0, 0, "0", "0", "0", new VersionIdentifier[0], new VersionIdentifier[0]);
    }

    internal static int FindSuffixIndex(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '-' || text[index] == '+')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static string[] SplitComponents(string text)
    {
        System.Collections.Generic.List<string> components = new System.Collections.Generic.List<string>();
        int startIndex = 0;
        int index = 0;

        if (text.Length == 0)
        {
            return new string[0];
        }

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

    internal static bool ContainsWildcard(string[] components)
    {
        int index = 0;

        while (index < components.Length)
        {
            if (IsWildcardComponent(components[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsWildcardComponent(string text)
    {
        if (text == "*")
        {
            return true;
        }

        if (text == "x")
        {
            return true;
        }

        if (text == "X")
        {
            return true;
        }

        return false;
    }

    internal static bool IsValidNumericComponent(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        if (text.Length > 1 && text[0] == '0')
        {
            return false;
        }

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

    internal VersionIdentifier[] CopyIdentifiers(VersionIdentifier[] source)
    {
        VersionIdentifier[] copy = new VersionIdentifier[source.Length];
        int index = 0;

        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }
}
