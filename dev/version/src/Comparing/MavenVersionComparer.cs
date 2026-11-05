namespace version;

internal static class MavenVersionComparer
{
    internal static int Compare(string leftText, string rightText)
    {
        MavenVersionToken[] leftTokens = MavenVersionParser.ParseTokens(leftText);
        MavenVersionToken[] rightTokens = MavenVersionParser.ParseTokens(rightText);
        int leftLength = TrimTrailingNullItems(leftTokens);
        int rightLength = TrimTrailingNullItems(rightTokens);
        int maxLength = leftLength;
        int separatorComparison = 0;

        if (rightLength > maxLength)
        {
            maxLength = rightLength;
        }

        int index = 0;
        while (index < maxLength)
        {
            int comparison = 0;

            if (index >= leftLength)
            {
                comparison = 0 - CompareTokenToNull(rightTokens[index]);
            }
            else if (index >= rightLength)
            {
                comparison = CompareTokenToNull(leftTokens[index]);
            }
            else
            {
                comparison = CompareTokens(leftTokens[index], rightTokens[index]);

                if (comparison == 0 && separatorComparison == 0)
                {
                    separatorComparison = CompareSeparators(leftTokens[index], rightTokens[index]);
                }
            }

            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        return separatorComparison;
    }

    internal static int TrimTrailingNullItems(MavenVersionToken[] tokens)
    {
        int length = tokens.Length;

        while (length > 0 && IsNullItem(tokens[length - 1]))
        {
            length = length - 1;
        }

        return length;
    }

    internal static bool IsNullItem(MavenVersionToken token)
    {
        if (token.IsNumeric())
        {
            return CompareNumericText(token.GetText(), "0") == 0;
        }

        return MavenQualifierComparer.CompareQualifiers(token.GetText(), "") == 0;
    }

    internal static int CompareTokenToNull(MavenVersionToken token)
    {
        if (token.IsNumeric())
        {
            return CompareNumericText(token.GetText(), "0");
        }

        return MavenQualifierComparer.CompareQualifiers(token.GetText(), "");
    }

    internal static int CompareTokens(MavenVersionToken left, MavenVersionToken right)
    {
        if (left.IsNumeric() && right.IsNumeric())
        {
            return CompareNumericText(left.GetText(), right.GetText());
        }

        if (left.IsText() && right.IsText())
        {
            return MavenQualifierComparer.CompareQualifiers(left.GetText(), right.GetText());
        }

        if (left.IsNumeric())
        {
            return 1;
        }

        return -1;
    }

    internal static int CompareSeparators(MavenVersionToken left, MavenVersionToken right)
    {
        char leftSeparator = left.GetSeparatorBefore();
        char rightSeparator = right.GetSeparatorBefore();

        if (leftSeparator == rightSeparator)
        {
            return 0;
        }

        if (leftSeparator == '-' && rightSeparator == '.')
        {
            return 1;
        }

        if (leftSeparator == '.' && rightSeparator == '-')
        {
            return -1;
        }

        return 0;
    }

    internal static int CompareNumericText(string left, string right)
    {
        string normalizedLeft = VersionFormatter.NormalizeNumericText(left);
        string normalizedRight = VersionFormatter.NormalizeNumericText(right);

        if (normalizedLeft.Length < normalizedRight.Length)
        {
            return -1;
        }

        if (normalizedLeft.Length > normalizedRight.Length)
        {
            return 1;
        }

        return NormalizeComparison(System.String.CompareOrdinal(normalizedLeft, normalizedRight));
    }

    internal static int NormalizeComparison(int comparison)
    {
        if (comparison < 0)
        {
            return -1;
        }

        if (comparison > 0)
        {
            return 1;
        }

        return 0;
    }
}
