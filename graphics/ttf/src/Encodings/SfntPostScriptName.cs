namespace ttf;

internal class SfntPostScriptName
{
    SfntLocalizedName? selectedName;
    string originalName;
    string normalizedName;
    bool originalValid;
    bool truncated;
    int invalidCharacterCount;

    internal SfntPostScriptName(
        SfntLocalizedName? selectedName,
        string originalName,
        string normalizedName,
        bool originalValid,
        bool truncated,
        int invalidCharacterCount)
    {
        if (originalName == null)
        {
            throw new System.ArgumentNullException("originalName");
        }

        if (normalizedName == null)
        {
            throw new System.ArgumentNullException("normalizedName");
        }

        if (invalidCharacterCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("invalidCharacterCount");
        }

        this.selectedName = selectedName;
        this.originalName = originalName;
        this.normalizedName = normalizedName;
        this.originalValid = originalValid;
        this.truncated = truncated;
        this.invalidCharacterCount = invalidCharacterCount;
    }

    internal SfntLocalizedName? GetSelectedName()
    {
        return selectedName;
    }

    internal string GetOriginalName()
    {
        return originalName;
    }

    internal string GetNormalizedName()
    {
        return normalizedName;
    }

    internal bool IsOriginalValid()
    {
        return originalValid;
    }

    internal bool WasNormalized()
    {
        return !System.String.Equals(
            originalName,
            normalizedName,
            System.StringComparison.Ordinal);
    }

    internal bool WasTruncated()
    {
        return truncated;
    }

    internal int GetInvalidCharacterCount()
    {
        return invalidCharacterCount;
    }
}

internal static class SfntPostScriptNameNormalizer
{
    const int MaximumLength = 63;
    const string FallbackName = "UnnamedFont";

    internal static SfntPostScriptName? Find(SfntNameTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        SfntLocalizedName? selectedName = table.SelectDefaultName(6);

        if (selectedName == null)
        {
            return null;
        }

        return Normalize(selectedName.GetValue(), selectedName);
    }

    internal static SfntPostScriptName Normalize(string originalName)
    {
        return Normalize(originalName, null);
    }

    internal static bool IsValid(string name)
    {
        if (name == null || name.Length == 0 || name.Length > MaximumLength)
        {
            return false;
        }

        int index = 0;

        while (index < name.Length)
        {
            if (!IsValidCharacter(name[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static SfntPostScriptName Normalize(
        string originalName,
        SfntLocalizedName? selectedName)
    {
        if (originalName == null)
        {
            throw new System.ArgumentNullException("originalName");
        }

        char[] normalizedCharacters = new char[MaximumLength];
        int normalizedLength = 0;
        int invalidCharacterCount = 0;
        bool previousCharacterInvalid = false;
        bool truncated = false;
        int index = 0;

        while (index < originalName.Length)
        {
            char character = originalName[index];

            if (IsValidCharacter(character))
            {
                if (normalizedLength < MaximumLength)
                {
                    normalizedCharacters[normalizedLength] = character;
                    normalizedLength += 1;
                }
                else
                {
                    truncated = true;
                }

                previousCharacterInvalid = false;
            }
            else
            {
                invalidCharacterCount += 1;

                if (!previousCharacterInvalid)
                {
                    if (normalizedLength < MaximumLength)
                    {
                        normalizedCharacters[normalizedLength] = '-';
                        normalizedLength += 1;
                    }
                    else
                    {
                        truncated = true;
                    }
                }

                previousCharacterInvalid = true;
            }

            index += 1;
        }

        string normalizedName;

        if (normalizedLength == 0)
        {
            normalizedName = FallbackName;
        }
        else
        {
            normalizedName = new string(normalizedCharacters, 0, normalizedLength);
        }

        bool originalValid = originalName.Length != 0 &&
            originalName.Length <= MaximumLength &&
            invalidCharacterCount == 0;

        return new SfntPostScriptName(
            selectedName,
            originalName,
            normalizedName,
            originalValid,
            truncated,
            invalidCharacterCount);
    }

    static bool IsValidCharacter(char character)
    {
        if (character < 33 || character > 126)
        {
            return false;
        }

        return character != '[' && character != ']' &&
            character != '(' && character != ')' &&
            character != '{' && character != '}' &&
            character != '<' && character != '>' &&
            character != '/' && character != '%';
    }
}
