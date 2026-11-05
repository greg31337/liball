namespace version;

internal static class RubyGemsPlatformSelector
{
    internal static bool PlatformMatches(RubyGemsPlatform gemPlatform, RubyGemsPlatform localPlatform)
    {
        System.ArgumentNullException.ThrowIfNull(gemPlatform);
        System.ArgumentNullException.ThrowIfNull(localPlatform);

        return gemPlatform.MatchesPlatform(localPlatform);
    }

    internal static bool SpecificationMatchesPlatform(RubyGemsSpecification specification, RubyGemsPlatform[] localPlatforms)
    {
        System.ArgumentNullException.ThrowIfNull(specification);
        System.ArgumentNullException.ThrowIfNull(localPlatforms);
        ValidatePlatforms(localPlatforms);

        if (specification.GetPlatform().IsRuby())
        {
            return ContainsRubyPlatform(localPlatforms);
        }

        int index = 0;

        while (index < localPlatforms.Length)
        {
            if (!localPlatforms[index].IsRuby() && specification.GetPlatform().MatchesPlatform(localPlatforms[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static RubyGemsSpecification[] FilterCompatibleSpecifications(
        RubyGemsSpecification[] specifications,
        RubyGemsPlatform[] localPlatforms)
    {
        System.ArgumentNullException.ThrowIfNull(specifications);
        System.ArgumentNullException.ThrowIfNull(localPlatforms);
        ValidateSpecifications(specifications);
        ValidatePlatforms(localPlatforms);

        System.Collections.Generic.List<RubyGemsSpecification> matches = new System.Collections.Generic.List<RubyGemsSpecification>();
        int index = 0;

        while (index < specifications.Length)
        {
            if (SpecificationMatchesPlatform(specifications[index], localPlatforms))
            {
                matches.Add(specifications[index]);
            }

            index = index + 1;
        }

        return matches.ToArray();
    }

    internal static RubyGemsSpecification? SelectBestSpecification(
        RubyGemsSpecification[] specifications,
        RubyGemsPlatform[] localPlatforms)
    {
        RubyGemsSpecification[] matches = FilterCompatibleSpecifications(specifications, localPlatforms);
        RubyGemsSpecification? selected = null;
        int selectedScore = -1;
        int index = 0;

        while (index < matches.Length)
        {
            int score = GetPlatformScore(matches[index], localPlatforms);
            if (selected == null
                || score > selectedScore
                || (score == selectedScore && RubyGemsVersions.Compare(matches[index].GetVersionText(), selected.GetVersionText()) > 0))
            {
                selected = matches[index];
                selectedScore = score;
            }

            index = index + 1;
        }

        return selected;
    }

    internal static int GetSpecificationPlatformScore(RubyGemsSpecification specification, RubyGemsPlatform[] localPlatforms)
    {
        System.ArgumentNullException.ThrowIfNull(specification);
        System.ArgumentNullException.ThrowIfNull(localPlatforms);
        ValidatePlatforms(localPlatforms);

        return GetPlatformScore(specification, localPlatforms);
    }

    internal static int GetPlatformScore(RubyGemsSpecification specification, RubyGemsPlatform[] localPlatforms)
    {
        if (specification.GetPlatform().IsRuby())
        {
            return 1;
        }

        int index = 0;

        while (index < localPlatforms.Length)
        {
            if (specification.GetPlatform().EqualsPlatform(localPlatforms[index]))
            {
                return 3;
            }

            index = index + 1;
        }

        return 2;
    }

    internal static bool ContainsRubyPlatform(RubyGemsPlatform[] localPlatforms)
    {
        int index = 0;

        while (index < localPlatforms.Length)
        {
            if (localPlatforms[index].IsRuby())
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidateSpecifications(RubyGemsSpecification[] specifications)
    {
        int index = 0;

        while (index < specifications.Length)
        {
            if (specifications[index] == null)
            {
                throw new System.ArgumentException("RubyGems specification arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidatePlatforms(RubyGemsPlatform[] platforms)
    {
        int index = 0;

        while (index < platforms.Length)
        {
            if (platforms[index] == null)
            {
                throw new System.ArgumentException("RubyGems platform arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }
}
