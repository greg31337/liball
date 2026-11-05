namespace ttf;

internal class FontFallbackConfiguration
{
    System.Collections.Generic.Dictionary<FontGenericFamily, string> genericFamilies;
    System.Collections.Generic.List<string> fallbackFamilies;
    System.Collections.Generic.Dictionary<
        string,
        System.Collections.Generic.List<string>> scriptFallbackFamilies;

    internal FontFallbackConfiguration()
    {
        genericFamilies = new System.Collections.Generic.Dictionary<
            FontGenericFamily,
            string>();
        fallbackFamilies = new System.Collections.Generic.List<string>();
        scriptFallbackFamilies = new System.Collections.Generic.Dictionary<
            string,
            System.Collections.Generic.List<string>>(System.StringComparer.Ordinal);
    }

    internal void SetGenericFamily(FontGenericFamily genericFamily, string familyName)
    {
        ValidateGenericFamily(genericFamily);
        ValidateFamilyName(familyName);
        genericFamilies[genericFamily] = familyName;
    }

    internal void ClearGenericFamily(FontGenericFamily genericFamily)
    {
        ValidateGenericFamily(genericFamily);
        genericFamilies.Remove(genericFamily);
    }

    internal string? GetGenericFamily(FontGenericFamily genericFamily)
    {
        ValidateGenericFamily(genericFamily);
        return genericFamilies.ContainsKey(genericFamily)
            ? genericFamilies[genericFamily]
            : null;
    }

    internal bool AddFallbackFamily(string familyName)
    {
        ValidateFamilyName(familyName);

        if (ContainsFamily(fallbackFamilies, familyName))
        {
            return false;
        }

        fallbackFamilies.Add(familyName);
        return true;
    }

    internal void ClearFallbackFamilies()
    {
        fallbackFamilies.Clear();
    }

    internal int GetFallbackFamilyCount()
    {
        return fallbackFamilies.Count;
    }

    internal string GetFallbackFamily(int index)
    {
        ValidateIndex(index, fallbackFamilies.Count);
        return fallbackFamilies[index];
    }

    internal bool AddScriptFallbackFamily(string script, string familyName)
    {
        string normalizedScript = NormalizeScript(script);
        ValidateFamilyName(familyName);

        if (!scriptFallbackFamilies.ContainsKey(normalizedScript))
        {
            scriptFallbackFamilies.Add(
                normalizedScript,
                new System.Collections.Generic.List<string>());
        }

        System.Collections.Generic.List<string> families =
            scriptFallbackFamilies[normalizedScript];

        if (ContainsFamily(families, familyName))
        {
            return false;
        }

        families.Add(familyName);
        return true;
    }

    internal void ClearScriptFallbackFamilies(string script)
    {
        scriptFallbackFamilies.Remove(NormalizeScript(script));
    }

    internal int GetScriptFallbackFamilyCount(string script)
    {
        string normalizedScript = NormalizeScript(script);
        return scriptFallbackFamilies.ContainsKey(normalizedScript)
            ? scriptFallbackFamilies[normalizedScript].Count
            : 0;
    }

    internal string GetScriptFallbackFamily(string script, int index)
    {
        string normalizedScript = NormalizeScript(script);
        int count = GetScriptFallbackFamilyCount(normalizedScript);
        ValidateIndex(index, count);
        return scriptFallbackFamilies[normalizedScript][index];
    }

    internal string? ResolveGenericFamily(FontRequest request)
    {
        if (!request.HasGenericFamily())
        {
            return request.GetFamilyName();
        }

        FontGenericFamily genericFamily = request.GetGenericFamily();
        return genericFamilies.ContainsKey(genericFamily)
            ? genericFamilies[genericFamily]
            : null;
    }

    internal string[] CopyFallbackFamilies()
    {
        return fallbackFamilies.ToArray();
    }

    internal string[] CopyScriptFallbackFamilies(string script)
    {
        string normalizedScript = NormalizeScript(script);

        if (!scriptFallbackFamilies.ContainsKey(normalizedScript))
        {
            return new string[0];
        }

        return scriptFallbackFamilies[normalizedScript].ToArray();
    }

    internal static void ValidateGenericFamily(FontGenericFamily genericFamily)
    {
        if (genericFamily < FontGenericFamily.Serif ||
            genericFamily > FontGenericFamily.FangSong)
        {
            throw new System.ArgumentOutOfRangeException("genericFamily");
        }
    }

    internal static string GetGenericFamilyName(FontGenericFamily genericFamily)
    {
        ValidateGenericFamily(genericFamily);

        if (genericFamily == FontGenericFamily.SansSerif) { return "sans-serif"; }
        if (genericFamily == FontGenericFamily.Monospace) { return "monospace"; }
        if (genericFamily == FontGenericFamily.Cursive) { return "cursive"; }
        if (genericFamily == FontGenericFamily.Fantasy) { return "fantasy"; }
        if (genericFamily == FontGenericFamily.SystemUi) { return "system-ui"; }
        if (genericFamily == FontGenericFamily.Emoji) { return "emoji"; }
        if (genericFamily == FontGenericFamily.Math) { return "math"; }
        if (genericFamily == FontGenericFamily.FangSong) { return "fangsong"; }
        return "serif";
    }

    internal static string NormalizeScript(string script)
    {
        if (script == null)
        {
            throw new System.ArgumentNullException("script");
        }

        if (script.Length != 4)
        {
            throw new System.ArgumentException(
                "A script code must contain four ASCII letters.",
                "script");
        }

        char[] characters = new char[4];
        int index = 0;

        while (index < characters.Length)
        {
            char character = script[index];
            bool upper = character >= 'A' && character <= 'Z';
            bool lower = character >= 'a' && character <= 'z';

            if (!upper && !lower)
            {
                throw new System.ArgumentException(
                    "A script code must contain four ASCII letters.",
                    "script");
            }

            characters[index] = index == 0
                ? char.ToUpperInvariant(character)
                : char.ToLowerInvariant(character);
            index += 1;
        }

        return new string(characters);
    }

    static void ValidateFamilyName(string familyName)
    {
        if (familyName == null)
        {
            throw new System.ArgumentNullException("familyName");
        }

        if (FontFamilyNameResolver.NormalizeFamilyName(familyName).Length == 0)
        {
            throw new System.ArgumentException(
                "A font family name cannot be empty or whitespace.",
                "familyName");
        }
    }

    static bool ContainsFamily(
        System.Collections.Generic.List<string> families,
        string familyName)
    {
        string normalized = FontFamilyNameResolver.NormalizeFamilyName(familyName);
        int index = 0;

        while (index < families.Count)
        {
            if (string.Equals(
                FontFamilyNameResolver.NormalizeFamilyName(families[index]),
                normalized,
                System.StringComparison.Ordinal))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static void ValidateIndex(int index, int count)
    {
        if (index < 0 || index >= count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

internal static class FontFallbackResolver
{
    internal static FontFallbackList Resolve(
        FontFamily[]? families,
        FontRequest request,
        string? primaryFamily,
        string[] scriptFamilies,
        string[] fallbackFamilies)
    {
        System.Collections.Generic.List<FontMatch> matches =
            new System.Collections.Generic.List<FontMatch>();
        FontFallbackMatchKind primaryKind = request.HasGenericFamily()
            ? FontFallbackMatchKind.GenericFamily
            : FontFallbackMatchKind.RequestedFamily;
        AddMatch(families, request, primaryFamily, primaryKind, matches);
        AddMatches(
            families,
            request,
            scriptFamilies,
            FontFallbackMatchKind.ScriptFallback,
            matches);
        AddMatches(
            families,
            request,
            fallbackFamilies,
            FontFallbackMatchKind.FallbackFamily,
            matches);
        return new FontFallbackList(matches.ToArray());
    }

    static void AddMatches(
        FontFamily[]? families,
        FontRequest request,
        string[] familyNames,
        FontFallbackMatchKind kind,
        System.Collections.Generic.List<FontMatch> matches)
    {
        int index = 0;

        while (index < familyNames.Length)
        {
            AddMatch(
                families,
                request,
                familyNames[index],
                kind,
                matches);
            index += 1;
        }
    }

    static void AddMatch(
        FontFamily[]? families,
        FontRequest request,
        string? familyName,
        FontFallbackMatchKind kind,
        System.Collections.Generic.List<FontMatch> matches)
    {
        if (familyName == null)
        {
            return;
        }

        FontRequest concreteRequest = request.CopyForFamily(familyName);
        FontMatch? match = FontMatcher.Match(families, concreteRequest);

        if (match != null && !Contains(matches, match))
        {
            match.SetFallbackDetails(kind, familyName);
            matches.Add(match);
        }
    }

    static bool Contains(
        System.Collections.Generic.List<FontMatch> matches,
        FontMatch candidate)
    {
        int index = 0;

        while (index < matches.Count)
        {
            FontMatch current = matches[index];

            if (object.ReferenceEquals(current.GetFace(), candidate.GetFace()) &&
                string.Equals(
                    current.GetCoordinates().GetKey(),
                    candidate.GetCoordinates().GetKey(),
                    System.StringComparison.Ordinal))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}

internal static class FontClusterFallbackResolver
{
    internal static FontMatch? Resolve(
        FontFallbackList fallbacks,
        RuneBuffer runes,
        UnicodeData unicodeData)
    {
        if (fallbacks == null)
        {
            throw new System.ArgumentNullException("fallbacks");
        }

        if (runes == null)
        {
            throw new System.ArgumentNullException("runes");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        int requiredCount = CountRequiredScalars(runes, unicodeData);
        FontMatch? bestMatch = null;
        int bestCoveredCount = -1;
        int index = 0;

        while (index < fallbacks.GetCount())
        {
            FontMatch match = fallbacks.GetMatch(index);
            int coveredCount = CountCoveredScalars(
                match.GetFace().GetCmapMapping(),
                runes,
                unicodeData);

            if (coveredCount == requiredCount)
            {
                return match;
            }

            if (coveredCount > bestCoveredCount)
            {
                bestMatch = match;
                bestCoveredCount = coveredCount;
            }

            index += 1;
        }

        return bestMatch;
    }

    internal static bool HasCompleteCoverage(
        FontMatch match,
        RuneBuffer runes,
        UnicodeData unicodeData)
    {
        int requiredCount = CountRequiredScalars(runes, unicodeData);
        int coveredCount = CountCoveredScalars(
            match.GetFace().GetCmapMapping(),
            runes,
            unicodeData);
        return coveredCount == requiredCount;
    }

    internal static int GetFirstMissingCodePoint(
        FontMatch match,
        RuneBuffer runes,
        UnicodeData unicodeData)
    {
        SfntCmapMapping? mapping = match.GetFace().GetCmapMapping();
        int index = 0;
        while (index < runes.GetCount())
        {
            int codePoint = runes.GetScalarValue(index);
            if (IsVariationSelector(unicodeData, codePoint))
            {
                index += 1;
                continue;
            }

            if (index + 1 < runes.GetCount() &&
                IsVariationSelector(
                    unicodeData,
                    runes.GetScalarValue(index + 1)))
            {
                if (mapping == null || mapping.MapVariation(
                    codePoint,
                    runes.GetScalarValue(index + 1)) == 0)
                {
                    return codePoint;
                }
                index += 2;
                continue;
            }

            if (!IsDefaultIgnorable(unicodeData, codePoint) &&
                (mapping == null || mapping.MapUnicode(codePoint) == 0))
            {
                return codePoint;
            }
            index += 1;
        }
        return -1;
    }

    static int CountRequiredScalars(
        RuneBuffer runes,
        UnicodeData unicodeData)
    {
        int requiredCount = 0;
        int index = 0;

        while (index < runes.GetCount())
        {
            int codePoint = runes.GetScalarValue(index);

            if (IsVariationSelector(unicodeData, codePoint))
            {
                index += 1;
                continue;
            }

            if (index + 1 < runes.GetCount() &&
                IsVariationSelector(
                    unicodeData,
                    runes.GetScalarValue(index + 1)))
            {
                requiredCount += 1;
                index += 2;
                continue;
            }

            if (!IsDefaultIgnorable(unicodeData, codePoint))
            {
                requiredCount += 1;
            }

            index += 1;
        }

        return requiredCount;
    }

    static int CountCoveredScalars(
        SfntCmapMapping? mapping,
        RuneBuffer runes,
        UnicodeData unicodeData)
    {
        int coveredCount = 0;
        int index = 0;

        while (index < runes.GetCount())
        {
            int codePoint = runes.GetScalarValue(index);

            if (IsVariationSelector(unicodeData, codePoint))
            {
                index += 1;
                continue;
            }

            if (index + 1 < runes.GetCount() &&
                IsVariationSelector(
                    unicodeData,
                    runes.GetScalarValue(index + 1)))
            {
                if (mapping != null && mapping.MapVariation(
                    codePoint,
                    runes.GetScalarValue(index + 1)) != 0)
                {
                    coveredCount += 1;
                }

                index += 2;
                continue;
            }

            if (!IsDefaultIgnorable(unicodeData, codePoint) &&
                mapping != null && mapping.MapUnicode(codePoint) != 0)
            {
                coveredCount += 1;
            }

            index += 1;
        }

        return coveredCount;
    }

    static bool IsVariationSelector(
        UnicodeData unicodeData,
        int codePoint)
    {
        return unicodeData.GetPropertyName(
            UnicodeDataTags.VariationSelector,
            codePoint) == "Y";
    }

    static bool IsDefaultIgnorable(
        UnicodeData unicodeData,
        int codePoint)
    {
        return unicodeData.GetPropertyName(
            UnicodeDataTags.DefaultIgnorable,
            codePoint) == "Y";
    }
}
