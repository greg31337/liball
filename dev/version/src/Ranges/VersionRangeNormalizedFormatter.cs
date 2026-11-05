namespace version;

internal static class VersionRangeNormalizedFormatter
{
    internal static string Format(VersionRange range, RangeSyntax syntax)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        ValidateSyntax(syntax);

        if (syntax == RangeSyntax.NuGet && range.nuGetFloatingVersion != null)
        {
            return FormatNuGetFloating(range.nuGetFloatingVersion);
        }

        VersionRange simplified = VersionRangeAlgebra.Simplify(range);
        if (simplified.IsEmpty())
        {
            return FormatEmpty(syntax);
        }

        if (simplified.IsUniversal())
        {
            return FormatUniversal(syntax);
        }

        if (syntax == RangeSyntax.NuGet)
        {
            return FormatNuGetRange(simplified);
        }

        if (syntax == RangeSyntax.Maven)
        {
            return FormatMavenRange(simplified);
        }

        if (syntax == RangeSyntax.Python && simplified.GetSetCount() > 1)
        {
            return FormatPythonMultipleSets(simplified);
        }

        if (syntax != RangeSyntax.NodeSemVer && simplified.GetSetCount() > 1)
        {
            throw new System.ArgumentException("The requested range syntax cannot represent disjunctions.");
        }

        return FormatComparatorRange(simplified, syntax);
    }

    internal static string FormatEmpty(RangeSyntax syntax)
    {
        if (syntax == RangeSyntax.NodeSemVer)
        {
            return ">=0.0.0 <0.0.0";
        }

        if (syntax == RangeSyntax.Cargo)
        {
            return ">=0.0.0, <0.0.0";
        }

        if (syntax == RangeSyntax.NuGet)
        {
            return "[0.0.0,0.0.0)";
        }

        if (syntax == RangeSyntax.Maven)
        {
            return "[0,0)";
        }

        if (syntax == RangeSyntax.RubyGems)
        {
            return ">= 0, < 0";
        }

        return ">=0,<0";
    }

    internal static string FormatUniversal(RangeSyntax syntax)
    {
        if (syntax == RangeSyntax.Maven)
        {
            return "0";
        }

        if (syntax == RangeSyntax.RubyGems)
        {
            return ">= 0";
        }

        if (syntax == RangeSyntax.Python)
        {
            return ">=0";
        }

        return "*";
    }

    internal static string FormatComparatorRange(VersionRange range, RangeSyntax syntax)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int setIndex = 0;

        while (setIndex < range.GetSetCount())
        {
            if (setIndex > 0)
            {
                builder.Append(" || ");
            }

            VersionComparatorSet set = range.GetSet(setIndex);
            int comparatorIndex = 0;
            while (comparatorIndex < set.GetComparatorCount())
            {
                if (comparatorIndex > 0)
                {
                    if (syntax == RangeSyntax.NodeSemVer)
                    {
                        builder.Append(' ');
                    }
                    else
                    {
                        builder.Append(", ");
                    }
                }

                builder.Append(FormatComparator(set.GetComparator(comparatorIndex), syntax));
                comparatorIndex = comparatorIndex + 1;
            }

            setIndex = setIndex + 1;
        }

        return builder.ToString();
    }

    internal static string FormatComparator(VersionComparator comparator, RangeSyntax syntax)
    {
        RangeOperator rangeOperator = comparator.GetOperator();
        if ((syntax == RangeSyntax.NodeSemVer || syntax == RangeSyntax.Cargo)
            && rangeOperator == RangeOperator.NotEqual)
        {
            throw new System.ArgumentException("The requested range syntax cannot represent exclusions in one comparator set.");
        }

        string operatorText = comparator.GetOperatorText();
        if (syntax == RangeSyntax.Python && rangeOperator == RangeOperator.Equal)
        {
            operatorText = "==";
        }

        if (syntax == RangeSyntax.RubyGems)
        {
            operatorText = operatorText + " ";
        }

        return operatorText + VersionFormatter.FormatNormalized(comparator.GetVersion());
    }

    internal static string FormatNuGetRange(VersionRange range)
    {
        if (range.GetSetCount() != 1)
        {
            throw new System.ArgumentException("NuGet range syntax cannot represent disjunctions.");
        }

        return FormatIntervalSet(range.GetSet(0));
    }

    internal static string FormatMavenRange(VersionRange range)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < range.GetSetCount())
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(FormatIntervalSet(range.GetSet(index)));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string FormatIntervalSet(VersionComparatorSet set)
    {
        VersionComparator? equalComparator = VersionRangeAlgebra.GetEqualComparator(set);
        if (equalComparator != null)
        {
            return "[" + VersionFormatter.FormatNormalized(equalComparator.GetVersion()) + "]";
        }

        int comparatorIndex = 0;
        while (comparatorIndex < set.GetComparatorCount())
        {
            if (set.GetComparator(comparatorIndex).GetOperator() == RangeOperator.NotEqual)
            {
                throw new System.ArgumentException("Interval range syntax cannot represent a comparator exclusion.");
            }

            comparatorIndex = comparatorIndex + 1;
        }

        VersionRangeAlgebraBounds bounds = VersionRangeAlgebra.GetBounds(set);
        if (bounds.lowerVersion == null && bounds.upperVersion == null)
        {
            throw new System.ArgumentException("An interval comparator set must contain a bound.");
        }

        if (bounds.lowerVersion != null
            && bounds.upperVersion != null
            && bounds.lowerInclusive
            && bounds.upperInclusive
            && bounds.lowerVersion.CompareToVersion(bounds.upperVersion, VersionComparisonMode.Precedence) == 0)
        {
            return "[" + VersionFormatter.FormatNormalized(bounds.lowerVersion) + "]";
        }

        string openDelimiter = "(";
        if (bounds.lowerVersion != null && bounds.lowerInclusive)
        {
            openDelimiter = "[";
        }

        string closeDelimiter = ")";
        if (bounds.upperVersion != null && bounds.upperInclusive)
        {
            closeDelimiter = "]";
        }

        string lowerText = "";
        if (bounds.lowerVersion != null)
        {
            lowerText = VersionFormatter.FormatNormalized(bounds.lowerVersion);
        }

        string upperText = "";
        if (bounds.upperVersion != null)
        {
            upperText = VersionFormatter.FormatNormalized(bounds.upperVersion);
        }

        return openDelimiter + lowerText + "," + upperText + closeDelimiter;
    }

    internal static string FormatNuGetFloating(NuGetFloatingVersion floating)
    {
        string text = "*";
        if (floating.numericPrecision == 1)
        {
            text = VersionFormatter.NormalizeNumericText(floating.major) + ".*";
        }
        else if (floating.numericPrecision == 2)
        {
            text = VersionFormatter.NormalizeNumericText(floating.major)
                + "."
                + VersionFormatter.NormalizeNumericText(floating.minor)
                + ".*";
        }
        else if (floating.numericPrecision == 3)
        {
            text = VersionFormatter.NormalizeNumericText(floating.major)
                + "."
                + VersionFormatter.NormalizeNumericText(floating.minor)
                + "."
                + VersionFormatter.NormalizeNumericText(floating.patch);
        }

        if (!floating.includePrerelease)
        {
            return text;
        }

        if (floating.prereleaseAny || floating.prereleasePrefix.Length == 0)
        {
            return text + "-*";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(text);
        builder.Append('-');
        int index = 0;
        while (index < floating.prereleasePrefix.Length)
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            builder.Append(floating.prereleasePrefix[index]);
            index = index + 1;
        }

        builder.Append(".*");
        return builder.ToString();
    }

    internal static string FormatPythonMultipleSets(VersionRange range)
    {
        if (range.GetSetCount() == 2
            && range.GetSet(0).GetComparatorCount() == 1
            && range.GetSet(1).GetComparatorCount() == 1
            && range.GetSet(0).GetComparator(0).GetOperator() == RangeOperator.LessThan
            && range.GetSet(1).GetComparator(0).GetOperator() == RangeOperator.GreaterThanOrEqual)
        {
            string prefix = VersionFormatter.FormatNormalized(range.GetSet(0).GetComparator(0).GetVersion());
            return "!=" + prefix + ".*";
        }

        throw new System.ArgumentException("Python range syntax cannot represent this disjunction.");
    }

    internal static void ValidateSyntax(RangeSyntax syntax)
    {
        if (syntax != RangeSyntax.NodeSemVer
            && syntax != RangeSyntax.Cargo
            && syntax != RangeSyntax.NuGet
            && syntax != RangeSyntax.Maven
            && syntax != RangeSyntax.RubyGems
            && syntax != RangeSyntax.Python)
        {
            throw new System.ArgumentOutOfRangeException("syntax");
        }
    }
}
