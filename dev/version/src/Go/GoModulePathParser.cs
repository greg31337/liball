namespace version;

internal class GoModulePathParser
{
    internal string source;

    internal GoModulePathParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
    }

    internal static GoModulePathParseResult Parse(string text)
    {
        if (text == null)
        {
            return Failure(
                "",
                GoDiagnosticCodes.MissingModulePath,
                "Go module path cannot be null.",
                0);
        }

        GoModulePathParser parser = new GoModulePathParser(text);
        return parser.ParseModulePath();
    }

    internal static GoModulePathParseResult ParseEscaped(string text)
    {
        if (text == null)
        {
            return Failure(
                "",
                GoDiagnosticCodes.MissingModulePath,
                "Escaped Go module path cannot be null.",
                0);
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '!')
            {
                if (index + 1 >= text.Length || !VersionChar.IsLowercaseLetter(text[index + 1]))
                {
                    return Failure(
                        text,
                        GoDiagnosticCodes.InvalidEscapedPathElement,
                        "Escaped Go module paths use ! followed by a lowercase ASCII letter.",
                        index);
                }

                builder.Append(ToUppercaseAscii(text[index + 1]));
                index = index + 2;
            }
            else
            {
                builder.Append(text[index]);
                index = index + 1;
            }
        }

        GoModulePathParseResult result = Parse(builder.ToString());
        if (result.IsFailure())
        {
            VersionDiagnostic diagnostic = result.GetDiagnostic(0);
            return Failure(text, diagnostic.GetCode(), diagnostic.GetMessage(), 0);
        }

        return GoModulePathParseResult.CreateSuccess(text, result.GetModulePath());
    }

    internal GoModulePathParseResult ParseModulePath()
    {
        if (source.Length == 0)
        {
            return Failure(
                source,
                GoDiagnosticCodes.MissingModulePath,
                "Go module path cannot be empty.",
                0);
        }

        if (source[0] == '/' || source[source.Length - 1] == '/')
        {
            return Failure(
                source,
                GoDiagnosticCodes.InvalidModulePath,
                "Go module path cannot begin or end with a slash.",
                0);
        }

        int elementStartIndex = 0;
        int elementIndex = 0;
        int finalElementStartIndex = 0;
        int finalElementEndIndex = 0;

        while (elementStartIndex < source.Length)
        {
            int elementEndIndex = FindSlashOrEnd(elementStartIndex);
            if (elementEndIndex == elementStartIndex)
            {
                return Failure(
                    source,
                    GoDiagnosticCodes.InvalidPathElement,
                    "Go module path cannot contain empty path elements.",
                    elementStartIndex);
            }

            string diagnosticCode = ValidateElement(elementStartIndex, elementEndIndex, elementIndex);
            if (diagnosticCode.Length > 0)
            {
                return Failure(
                    source,
                    diagnosticCode,
                    "Invalid Go module path element.",
                    elementStartIndex);
            }

            finalElementStartIndex = elementStartIndex;
            finalElementEndIndex = elementEndIndex;
            elementIndex = elementIndex + 1;
            elementStartIndex = elementEndIndex + 1;
        }

        string majorVersionSuffix = "";
        bool gopkgInPath = IsGopkgInPath();

        if (gopkgInPath)
        {
            majorVersionSuffix = ReadGopkgInMajorVersionSuffix(finalElementStartIndex, finalElementEndIndex);
            if (majorVersionSuffix.Length == 0)
            {
                return Failure(
                    source,
                    GoDiagnosticCodes.InvalidGopkgInPath,
                    "gopkg.in module paths must end with a dot major version suffix.",
                    finalElementStartIndex);
            }
        }
        else
        {
            string suffixCode = ValidateSlashMajorVersionSuffix(finalElementStartIndex, finalElementEndIndex);
            if (suffixCode.Length > 0)
            {
                return Failure(
                    source,
                    suffixCode,
                    "Invalid Go module path major version suffix.",
                    finalElementStartIndex);
            }

            majorVersionSuffix = ReadSlashMajorVersionSuffix(finalElementStartIndex, finalElementEndIndex);
        }

        return GoModulePathParseResult.CreateSuccess(source, new GoModulePath(source, majorVersionSuffix, gopkgInPath));
    }

    internal string ValidateElement(int startIndex, int endIndex, int elementIndex)
    {
        if (source[startIndex] == '.' || source[endIndex - 1] == '.')
        {
            return GoDiagnosticCodes.InvalidPathElement;
        }

        int index = startIndex;
        while (index < endIndex)
        {
            if (source[index] == '!')
            {
                return GoDiagnosticCodes.InvalidEscapedPathElement;
            }

            if (!IsAllowedPathElementCharacter(source[index]))
            {
                return GoDiagnosticCodes.InvalidPathElement;
            }

            index = index + 1;
        }

        if (IsReservedWindowsNamePrefix(startIndex, endIndex) || HasShortNameSuffix(startIndex, endIndex))
        {
            return GoDiagnosticCodes.InvalidPathElement;
        }

        if (elementIndex == 0 && !IsValidFirstElement(startIndex, endIndex))
        {
            return GoDiagnosticCodes.InvalidFirstPathElement;
        }

        return "";
    }

    internal bool IsValidFirstElement(int startIndex, int endIndex)
    {
        if (source[startIndex] == '-')
        {
            return false;
        }

        bool hasDot = false;
        int index = startIndex;

        while (index < endIndex)
        {
            if (source[index] == '.')
            {
                hasDot = true;
            }
            else if (!VersionChar.IsLowercaseLetter(source[index]) && !VersionChar.IsDigit(source[index]) && source[index] != '-')
            {
                return false;
            }

            index = index + 1;
        }

        return hasDot;
    }

    internal string ValidateSlashMajorVersionSuffix(int startIndex, int endIndex)
    {
        if (!LooksLikeSlashMajorVersionSuffix(startIndex, endIndex))
        {
            return "";
        }

        if (ContainsDot(startIndex + 1, endIndex))
        {
            return GoDiagnosticCodes.InvalidMajorVersionSuffix;
        }

        if (endIndex - startIndex > 2 && source[startIndex + 1] == '0')
        {
            return GoDiagnosticCodes.InvalidMajorVersionSuffix;
        }

        if (IsMajorVersionZeroOrOne(startIndex + 1, endIndex))
        {
            return GoDiagnosticCodes.InvalidMajorVersionSuffix;
        }

        return "";
    }

    internal string ReadSlashMajorVersionSuffix(int startIndex, int endIndex)
    {
        if (!LooksLikeSlashMajorVersionSuffix(startIndex, endIndex))
        {
            return "";
        }

        if (ValidateSlashMajorVersionSuffix(startIndex, endIndex).Length > 0)
        {
            return "";
        }

        return source.Substring(startIndex, endIndex - startIndex);
    }

    internal bool LooksLikeSlashMajorVersionSuffix(int startIndex, int endIndex)
    {
        if (endIndex - startIndex < 2 || source[startIndex] != 'v')
        {
            return false;
        }

        int index = startIndex + 1;
        bool hasNumberCharacter = false;

        while (index < endIndex)
        {
            if (!VersionChar.IsDigit(source[index]) && source[index] != '.')
            {
                return false;
            }

            hasNumberCharacter = true;
            index = index + 1;
        }

        return hasNumberCharacter;
    }

    internal string ReadGopkgInMajorVersionSuffix(int startIndex, int endIndex)
    {
        int index = endIndex - 1;

        while (index >= startIndex)
        {
            if (source[index] == '.' && index + 2 < endIndex && source[index + 1] == 'v')
            {
                if (AllDigits(index + 2, endIndex) && !HasLeadingZero(index + 2, endIndex))
                {
                    return source.Substring(index + 1, endIndex - index - 1);
                }

                return "";
            }

            index = index - 1;
        }

        return "";
    }

    internal bool IsGopkgInPath()
    {
        string prefix = "gopkg.in/";

        if (source == "gopkg.in")
        {
            return true;
        }

        if (source.Length <= prefix.Length)
        {
            return false;
        }

        return source.Substring(0, prefix.Length) == prefix;
    }

    internal bool IsReservedWindowsNamePrefix(int startIndex, int endIndex)
    {
        int prefixEndIndex = FindElementDotOrEnd(startIndex, endIndex);
        int prefixLength = prefixEndIndex - startIndex;

        if (EqualsIgnoreCase(startIndex, prefixEndIndex, "CON"))
        {
            return true;
        }

        if (EqualsIgnoreCase(startIndex, prefixEndIndex, "PRN"))
        {
            return true;
        }

        if (EqualsIgnoreCase(startIndex, prefixEndIndex, "AUX"))
        {
            return true;
        }

        if (EqualsIgnoreCase(startIndex, prefixEndIndex, "NUL"))
        {
            return true;
        }

        if (prefixLength == 4 && IsComOrLptPrefix(startIndex) && source[startIndex + 3] >= '1' && source[startIndex + 3] <= '9')
        {
            return true;
        }

        return false;
    }

    internal bool IsComOrLptPrefix(int startIndex)
    {
        if (EqualsIgnoreCase(startIndex, startIndex + 3, "COM"))
        {
            return true;
        }

        return EqualsIgnoreCase(startIndex, startIndex + 3, "LPT");
    }

    internal bool HasShortNameSuffix(int startIndex, int endIndex)
    {
        int prefixEndIndex = FindElementDotOrEnd(startIndex, endIndex);
        int index = prefixEndIndex - 1;

        while (index >= startIndex && VersionChar.IsDigit(source[index]))
        {
            index = index - 1;
        }

        return index >= startIndex && index < prefixEndIndex - 1 && source[index] == '~';
    }

    internal int FindElementDotOrEnd(int startIndex, int endIndex)
    {
        int index = startIndex;

        while (index < endIndex)
        {
            if (source[index] == '.')
            {
                return index;
            }

            index = index + 1;
        }

        return endIndex;
    }

    internal int FindSlashOrEnd(int startIndex)
    {
        int index = startIndex;

        while (index < source.Length)
        {
            if (source[index] == '/')
            {
                return index;
            }

            index = index + 1;
        }

        return source.Length;
    }

    internal bool IsAllowedPathElementCharacter(char value)
    {
        return VersionChar.IsLetter(value)
            || VersionChar.IsDigit(value)
            || value == '-'
            || value == '.'
            || value == '_'
            || value == '~';
    }

    internal bool ContainsDot(int startIndex, int endIndex)
    {
        int index = startIndex;

        while (index < endIndex)
        {
            if (source[index] == '.')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool AllDigits(int startIndex, int endIndex)
    {
        if (startIndex >= endIndex)
        {
            return false;
        }

        int index = startIndex;
        while (index < endIndex)
        {
            if (!VersionChar.IsDigit(source[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool HasLeadingZero(int startIndex, int endIndex)
    {
        return endIndex - startIndex > 1 && source[startIndex] == '0';
    }

    internal bool IsMajorVersionZeroOrOne(int startIndex, int endIndex)
    {
        if (endIndex - startIndex != 1)
        {
            return false;
        }

        return source[startIndex] == '0' || source[startIndex] == '1';
    }

    internal bool EqualsIgnoreCase(int startIndex, int endIndex, string value)
    {
        if (endIndex - startIndex != value.Length)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (ToLowercaseAscii(source[startIndex + index]) != ToLowercaseAscii(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static char ToUppercaseAscii(char value)
    {
        return (char)(value - 'a' + 'A');
    }

    internal static char ToLowercaseAscii(char value)
    {
        if (VersionChar.IsUppercaseLetter(value))
        {
            return (char)(value - 'A' + 'a');
        }

        return value;
    }

    internal static GoModulePathParseResult Failure(string sourceText, string code, string message, int diagnosticIndex)
    {
        return GoModulePathParseResult.CreateFailure(
            sourceText,
            VersionDiagnostic.FromIndex(code, message, sourceText, diagnosticIndex));
    }
}
