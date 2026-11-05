namespace version;

internal static class NuGetTargetFrameworkCompatibility
{
    internal const int MaximumVersionScore = (int.MaxValue - 805000) / 2;

    internal static bool IsCompatible(string projectFrameworkText, string candidateFrameworkText)
    {
        NuGetTargetFrameworkParseResult projectResult = NuGetTargetFrameworkParser.Parse(projectFrameworkText);
        NuGetTargetFrameworkParseResult candidateResult = NuGetTargetFrameworkParser.Parse(candidateFrameworkText);

        if (projectResult.IsFailure() || candidateResult.IsFailure())
        {
            return false;
        }

        return IsCompatible(projectResult.GetFramework(), candidateResult.GetFramework());
    }

    internal static bool IsCompatible(
        string projectFrameworkText,
        string candidateFrameworkText,
        string fallbackFrameworkListText)
    {
        NuGetTargetFrameworkParseResult projectResult = NuGetTargetFrameworkParser.Parse(projectFrameworkText);
        NuGetTargetFrameworkParseResult candidateResult = NuGetTargetFrameworkParser.Parse(candidateFrameworkText);

        if (projectResult.IsFailure() || candidateResult.IsFailure())
        {
            return false;
        }

        return IsCompatible(projectResult.GetFramework(), candidateResult.GetFramework(), fallbackFrameworkListText);
    }

    internal static bool IsCompatible(NuGetTargetFramework projectFramework, NuGetTargetFramework candidateFramework)
    {
        System.ArgumentNullException.ThrowIfNull(projectFramework);
        System.ArgumentNullException.ThrowIfNull(candidateFramework);

        return GetCompatibilityScore(projectFramework, candidateFramework) >= 0;
    }

    internal static bool IsCompatible(
        NuGetTargetFramework projectFramework,
        NuGetTargetFramework candidateFramework,
        string fallbackFrameworkListText)
    {
        System.ArgumentNullException.ThrowIfNull(projectFramework);
        System.ArgumentNullException.ThrowIfNull(candidateFramework);

        if (IsCompatible(projectFramework, candidateFramework))
        {
            return true;
        }

        NuGetTargetFrameworkFallbackParseResult fallbackResult = NuGetTargetFrameworkParser.ParseFallbackList(fallbackFrameworkListText);
        if (fallbackResult.IsFailure())
        {
            return false;
        }

        NuGetTargetFrameworkFallbackList fallbackList = fallbackResult.GetFallbackList();
        int index = 0;
        while (index < fallbackList.GetFrameworkCount())
        {
            if (IsCompatible(fallbackList.GetFramework(index), candidateFramework))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static NuGetDependencyGroup? SelectBestDependencyGroup(
        string projectFrameworkText,
        NuGetDependencyGroup[] dependencyGroups)
    {
        NuGetTargetFrameworkParseResult projectResult = NuGetTargetFrameworkParser.Parse(projectFrameworkText);
        if (projectResult.IsFailure())
        {
            return null;
        }

        return SelectBestDependencyGroup(projectResult.GetFramework(), dependencyGroups);
    }

    internal static NuGetDependencyGroup? SelectBestDependencyGroup(
        string projectFrameworkText,
        NuGetPackageMetadata packageMetadata)
    {
        System.ArgumentNullException.ThrowIfNull(packageMetadata);

        return SelectBestDependencyGroup(projectFrameworkText, packageMetadata.GetDependencyGroups());
    }

    internal static NuGetDependencyGroup? SelectBestDependencyGroup(
        NuGetTargetFramework projectFramework,
        NuGetPackageMetadata packageMetadata)
    {
        System.ArgumentNullException.ThrowIfNull(packageMetadata);

        return SelectBestDependencyGroup(projectFramework, packageMetadata.GetDependencyGroups());
    }

    internal static NuGetDependencyGroup? SelectBestDependencyGroup(
        NuGetTargetFramework projectFramework,
        NuGetDependencyGroup[] dependencyGroups)
    {
        System.ArgumentNullException.ThrowIfNull(projectFramework);
        System.ArgumentNullException.ThrowIfNull(dependencyGroups);

        NuGetDependencyGroup? bestGroup = null;
        int bestScore = -1;
        int index = 0;

        while (index < dependencyGroups.Length)
        {
            NuGetDependencyGroup group = dependencyGroups[index];
            if (group == null)
            {
                throw new System.ArgumentException("NuGet dependency group arrays cannot contain null values.");
            }

            int score = GetDependencyGroupScore(projectFramework, group);
            if (score > bestScore)
            {
                bestScore = score;
                bestGroup = group;
            }

            index = index + 1;
        }

        return bestGroup;
    }

    internal static int GetDependencyGroupScore(NuGetTargetFramework projectFramework, NuGetDependencyGroup dependencyGroup)
    {
        if (!dependencyGroup.HasTargetFramework())
        {
            return 0;
        }

        NuGetTargetFrameworkParseResult candidateResult = NuGetTargetFrameworkParser.Parse(dependencyGroup.GetTargetFramework());
        if (candidateResult.IsFailure())
        {
            return -1;
        }

        return GetCompatibilityScore(projectFramework, candidateResult.GetFramework());
    }

    internal static int GetCompatibilityScore(NuGetTargetFramework projectFramework, NuGetTargetFramework candidateFramework)
    {
        if (projectFramework.GetNormalizedText() == candidateFramework.GetNormalizedText())
        {
            return 1000000 + GetVersionScore(candidateFramework);
        }

        if (projectFramework.IsPortable() || candidateFramework.IsPortable())
        {
            return GetPortableCompatibilityScore(projectFramework, candidateFramework);
        }

        if (projectFramework.GetKind() == candidateFramework.GetKind())
        {
            if (CompareVersionTexts(candidateFramework.GetVersionText(), projectFramework.GetVersionText()) <= 0)
            {
                if (IsPlatformCompatible(projectFramework, candidateFramework))
                {
                    return AddScores(
                        800000,
                        GetVersionScore(candidateFramework),
                        GetPlatformScore(candidateFramework),
                        int.MaxValue - 1);
                }
            }

            return -1;
        }

        if (candidateFramework.GetKind() == NuGetTargetFrameworkKind.NetStandard)
        {
            return GetNetStandardCompatibilityScore(projectFramework, candidateFramework);
        }

        return -1;
    }

    internal static bool IsPlatformCompatible(NuGetTargetFramework projectFramework, NuGetTargetFramework candidateFramework)
    {
        if (candidateFramework.GetKind() != NuGetTargetFrameworkKind.Net)
        {
            return true;
        }

        if (!candidateFramework.HasPlatform())
        {
            return true;
        }

        if (!projectFramework.HasPlatform())
        {
            return false;
        }

        if (projectFramework.GetPlatformText() != candidateFramework.GetPlatformText())
        {
            return false;
        }

        if (!candidateFramework.HasPlatformVersion())
        {
            return true;
        }

        if (!projectFramework.HasPlatformVersion())
        {
            return false;
        }

        return CompareVersionTexts(candidateFramework.GetPlatformVersionText(), projectFramework.GetPlatformVersionText()) <= 0;
    }

    internal static int GetPortableCompatibilityScore(NuGetTargetFramework projectFramework, NuGetTargetFramework candidateFramework)
    {
        if (!projectFramework.IsPortable() || !candidateFramework.IsPortable())
        {
            return -1;
        }

        if (PortableProfileContainsAll(projectFramework, candidateFramework))
        {
            int frameworkCount = candidateFramework.GetPortableFrameworkCount();
            if (frameworkCount > int.MaxValue - 500000)
            {
                return int.MaxValue;
            }

            return 500000 + frameworkCount;
        }

        return -1;
    }

    internal static int GetNetStandardCompatibilityScore(NuGetTargetFramework projectFramework, NuGetTargetFramework candidateFramework)
    {
        string maximumVersion = GetMaximumNetStandardVersion(projectFramework);
        if (maximumVersion.Length == 0)
        {
            return -1;
        }

        if (CompareVersionTexts(candidateFramework.GetVersionText(), maximumVersion) <= 0)
        {
            return AddScores(600000, GetVersionScore(candidateFramework), 0, int.MaxValue);
        }

        return -1;
    }

    internal static string GetMaximumNetStandardVersion(NuGetTargetFramework projectFramework)
    {
        if (projectFramework.GetKind() == NuGetTargetFrameworkKind.NetStandard)
        {
            return projectFramework.GetVersionText();
        }

        if (projectFramework.GetKind() == NuGetTargetFrameworkKind.Net)
        {
            if (CompareVersionTexts(projectFramework.GetVersionText(), "5.0") >= 0)
            {
                return "2.1";
            }
        }

        if (projectFramework.GetKind() == NuGetTargetFrameworkKind.NetCoreApp)
        {
            if (CompareVersionTexts(projectFramework.GetVersionText(), "3.0") >= 0)
            {
                return "2.1";
            }

            if (CompareVersionTexts(projectFramework.GetVersionText(), "2.0") >= 0)
            {
                return "2.0";
            }

            if (CompareVersionTexts(projectFramework.GetVersionText(), "1.0") >= 0)
            {
                return "1.6";
            }
        }

        if (projectFramework.GetKind() == NuGetTargetFrameworkKind.NetFramework)
        {
            if (CompareVersionTexts(projectFramework.GetVersionText(), "4.6.1") >= 0)
            {
                return "2.0";
            }

            if (CompareVersionTexts(projectFramework.GetVersionText(), "4.5") >= 0)
            {
                return "1.1";
            }
        }

        return "";
    }

    internal static bool PortableProfileContainsAll(NuGetTargetFramework projectFramework, NuGetTargetFramework candidateFramework)
    {
        int candidateIndex = 0;
        while (candidateIndex < candidateFramework.GetPortableFrameworkCount())
        {
            if (!PortableProfileContains(projectFramework, candidateFramework.GetPortableFramework(candidateIndex)))
            {
                return false;
            }

            candidateIndex = candidateIndex + 1;
        }

        return true;
    }

    internal static bool PortableProfileContains(NuGetTargetFramework framework, string portableFrameworkText)
    {
        int index = 0;
        while (index < framework.GetPortableFrameworkCount())
        {
            if (framework.GetPortableFramework(index) == portableFrameworkText)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int GetVersionScore(NuGetTargetFramework framework)
    {
        if (framework.GetVersionText().Length == 0)
        {
            return 0;
        }

        return GetVersionScore(framework.GetVersionText());
    }

    internal static int GetPlatformScore(NuGetTargetFramework framework)
    {
        if (!framework.HasPlatform())
        {
            return 0;
        }

        return 5000 + GetVersionScore(framework.GetPlatformVersionText());
    }

    internal static int AddScores(int baseScore, int firstScore, int secondScore, int maximumScore)
    {
        if (firstScore > maximumScore - baseScore)
        {
            return maximumScore;
        }

        int score = baseScore + firstScore;
        if (secondScore > maximumScore - score)
        {
            return maximumScore;
        }

        return score + secondScore;
    }

    internal static int GetVersionScore(string versionText)
    {
        int score = 0;
        int multiplier = 10000;
        int startIndex = 0;
        int index = 0;

        while (index <= versionText.Length && multiplier > 0)
        {
            if (index == versionText.Length || versionText[index] == '.')
            {
                int versionPart = ParseVersionPart(versionText, startIndex, index);
                if (versionPart > (MaximumVersionScore - score) / multiplier)
                {
                    return MaximumVersionScore;
                }

                score = score + (versionPart * multiplier);
                multiplier = multiplier / 100;
                startIndex = index + 1;
            }

            index = index + 1;
        }

        return score;
    }

    internal static int CompareVersionTexts(string leftText, string rightText)
    {
        int leftStart = 0;
        int rightStart = 0;

        while (leftStart < leftText.Length || rightStart < rightText.Length)
        {
            int leftEnd = FindVersionPartEnd(leftText, leftStart);
            int rightEnd = FindVersionPartEnd(rightText, rightStart);
            int comparison = CompareVersionParts(leftText, leftStart, leftEnd, rightText, rightStart, rightEnd);
            if (comparison < 0)
            {
                return -1;
            }

            if (comparison > 0)
            {
                return 1;
            }

            leftStart = leftEnd + 1;
            rightStart = rightEnd + 1;
        }

        return 0;
    }

    internal static int CompareVersionParts(
        string leftText,
        int leftStart,
        int leftEnd,
        string rightText,
        int rightStart,
        int rightEnd)
    {
        while (leftStart < leftEnd && leftText[leftStart] == '0')
        {
            leftStart = leftStart + 1;
        }

        while (rightStart < rightEnd && rightText[rightStart] == '0')
        {
            rightStart = rightStart + 1;
        }

        int leftLength = leftEnd - leftStart;
        int rightLength = rightEnd - rightStart;
        if (leftLength < rightLength)
        {
            return -1;
        }

        if (leftLength > rightLength)
        {
            return 1;
        }

        int index = 0;
        while (index < leftLength)
        {
            if (leftText[leftStart + index] < rightText[rightStart + index])
            {
                return -1;
            }

            if (leftText[leftStart + index] > rightText[rightStart + index])
            {
                return 1;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static int FindVersionPartEnd(string text, int startIndex)
    {
        int index = startIndex;
        while (index < text.Length && text[index] != '.')
        {
            index = index + 1;
        }

        return index;
    }

    internal static int ParseVersionPart(string text, int startIndex, int endIndex)
    {
        int value = 0;
        int index = startIndex;

        while (index < endIndex)
        {
            int digit = text[index] - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                return int.MaxValue;
            }

            value = (value * 10) + digit;
            index = index + 1;
        }

        return value;
    }
}
