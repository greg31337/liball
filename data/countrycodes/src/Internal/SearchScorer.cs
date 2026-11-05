namespace countrycodes;

internal static class SearchScorer
{
    internal static bool AreOptionsValid(SearchOptions? options)
    {
        if (options == null || options.MaximumResults <= 0)
        {
            return false;
        }

        if (!IsSearchModeValid(options.Mode) ||
            !IsValidationPolicyValid(options.ValidationPolicy))
        {
            return false;
        }

        if (options.Locale != null &&
            LocaleTagUtility.NormalizeLocaleTag(options.Locale) == null)
        {
            return false;
        }

        return true;
    }

    internal static bool IsSearchModeValid(SearchMode mode)
    {
        return mode == SearchMode.ExactNormalized ||
               mode == SearchMode.Prefix ||
               mode == SearchMode.Token ||
               mode == SearchMode.Substring ||
               mode == SearchMode.Fuzzy;
    }

    internal static bool IsValidationPolicyValid(ValidationPolicy policy)
    {
        return policy == ValidationPolicy.SyntaxOnly ||
               policy == ValidationPolicy.AssignedOnly ||
               policy == ValidationPolicy.AnyKnownIso ||
               policy == ValidationPolicy.AllowHistorical ||
               policy == ValidationPolicy.AllowUserAssigned;
    }

    internal static SearchEvaluation EvaluateBroadMatch(
        string queryKey,
        string sourceKey,
        SearchMode mode,
        int prefixScore,
        int tokenScore,
        int substringScore,
        int fuzzyScore)
    {
        if ((int)mode >= (int)SearchMode.Prefix &&
            sourceKey.StartsWith(
                queryKey,
                global::System.StringComparison.Ordinal))
        {
            return new SearchEvaluation(true, SearchMatchKind.Prefix, prefixScore);
        }

        if ((int)mode >= (int)SearchMode.Token &&
            ContainsToken(sourceKey, queryKey))
        {
            return new SearchEvaluation(true, SearchMatchKind.Token, tokenScore);
        }

        if ((int)mode >= (int)SearchMode.Substring &&
            sourceKey.IndexOf(
                queryKey,
                global::System.StringComparison.Ordinal) >= 0)
        {
            return new SearchEvaluation(
                true,
                SearchMatchKind.Substring,
                substringScore);
        }

        if (mode == SearchMode.Fuzzy)
        {
            int maximumDistance = GetMaximumDistance(queryKey.Length);
            int distance = CalculateDistance(queryKey, sourceKey, maximumDistance);
            if (distance <= maximumDistance)
            {
                return new SearchEvaluation(
                    true,
                    SearchMatchKind.Fuzzy,
                    fuzzyScore - distance);
            }
        }

        return new SearchEvaluation(false, SearchMatchKind.Fuzzy, 0);
    }

    internal static bool ContainsToken(string sourceKey, string queryKey)
    {
        if (queryKey.Length == 0 || queryKey.IndexOf(' ') >= 0)
        {
            return false;
        }

        int index = 0;
        while (index < sourceKey.Length)
        {
            while (index < sourceKey.Length && sourceKey[index] == ' ')
            {
                index += 1;
            }

            int start = index;
            while (index < sourceKey.Length && sourceKey[index] != ' ')
            {
                index += 1;
            }

            if (index - start == queryKey.Length &&
                global::System.String.CompareOrdinal(
                    sourceKey,
                    start,
                    queryKey,
                    0,
                    queryKey.Length) == 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static int GetMaximumDistance(int queryLength)
    {
        if (queryLength <= 4)
        {
            return 1;
        }

        if (queryLength <= 8)
        {
            return 2;
        }

        return 3;
    }

    internal static int CalculateDistance(
        string left,
        string right,
        int maximumDistance)
    {
        int boundedMaximum = maximumDistance;
        if (boundedMaximum < 0)
        {
            boundedMaximum = 0;
        }

        if (boundedMaximum > 3)
        {
            boundedMaximum = 3;
        }

        int beyondMaximum = boundedMaximum + 1;
        int lengthDifference = left.Length - right.Length;
        if (lengthDifference < 0)
        {
            lengthDifference = -lengthDifference;
        }

        if (lengthDifference > boundedMaximum)
        {
            return beyondMaximum;
        }

        int[] previous = new int[right.Length + 1];
        int[] current = new int[right.Length + 1];
        int column = 0;
        while (column <= right.Length)
        {
            if (column <= boundedMaximum)
            {
                previous[column] = column;
            }
            else
            {
                previous[column] = beyondMaximum;
            }

            column += 1;
        }

        int row = 1;
        while (row <= left.Length)
        {
            if (row <= boundedMaximum)
            {
                current[0] = row;
            }
            else
            {
                current[0] = beyondMaximum;
            }

            int firstColumn = row - boundedMaximum;
            if (firstColumn < 1)
            {
                firstColumn = 1;
            }

            int lastColumn = row + boundedMaximum;
            if (lastColumn > right.Length)
            {
                lastColumn = right.Length;
            }

            if (firstColumn > 1)
            {
                current[firstColumn - 1] = beyondMaximum;
            }

            int rowMinimum = current[0];
            column = firstColumn;
            while (column <= lastColumn)
            {
                int substitutionCost = 1;
                if (left[row - 1] == right[column - 1])
                {
                    substitutionCost = 0;
                }

                int deletion = previous[column] + 1;
                int insertion = current[column - 1] + 1;
                int substitution = previous[column - 1] + substitutionCost;
                int value = deletion;
                if (insertion < value)
                {
                    value = insertion;
                }

                if (substitution < value)
                {
                    value = substitution;
                }

                if (value > beyondMaximum)
                {
                    value = beyondMaximum;
                }

                current[column] = value;
                if (value < rowMinimum)
                {
                    rowMinimum = value;
                }

                column += 1;
            }

            if (lastColumn < right.Length)
            {
                current[lastColumn + 1] = beyondMaximum;
            }

            if (rowMinimum > boundedMaximum)
            {
                return beyondMaximum;
            }

            int[] swap = previous;
            previous = current;
            current = swap;
            row += 1;
        }

        int result = previous[right.Length];
        if (result > boundedMaximum)
        {
            return beyondMaximum;
        }

        return result;
    }

    internal static int GetResultCount(int availableCount, int maximumResults)
    {
        if (availableCount <= 0 || maximumResults <= 0)
        {
            return 0;
        }

        if (availableCount < maximumResults)
        {
            return availableCount;
        }

        return maximumResults;
    }

    internal static int CompareCandidates(
        int leftScore,
        SearchMatchKind leftKind,
        string leftSourceText,
        string leftNormalizedKey,
        string leftIdentity,
        int rightScore,
        SearchMatchKind rightKind,
        string rightSourceText,
        string rightNormalizedKey,
        string rightIdentity)
    {
        if (leftScore != rightScore)
        {
            if (leftScore > rightScore)
            {
                return -1;
            }

            return 1;
        }

        if (leftKind != rightKind)
        {
            if ((int)leftKind < (int)rightKind)
            {
                return -1;
            }

            return 1;
        }

        int comparison = AsciiCodeUtility.CompareOrdinal(
            leftSourceText,
            rightSourceText);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = AsciiCodeUtility.CompareOrdinal(
            leftNormalizedKey,
            rightNormalizedKey);
        if (comparison != 0)
        {
            return comparison;
        }

        return AsciiCodeUtility.CompareOrdinal(leftIdentity, rightIdentity);
    }
}
