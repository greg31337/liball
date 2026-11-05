using System.Collections.Generic;

namespace dns;

internal class DnsNameOperationResult
{
    internal bool Success;
    internal bool BooleanValue;
    internal int Comparison;
    internal int LabelCount;
    internal DnsName? Name;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class DnsNameOperations
{
    internal static DnsNameOperationResult NamesEqual(DnsName left, DnsName right)
    {
        RawDnsNameResult leftResult = RawDnsName.ValidateName(left);
        if (!leftResult.Success)
        {
            return FromRawFailure(leftResult);
        }

        RawDnsNameResult rightResult = RawDnsName.ValidateName(right);
        if (!rightResult.Success)
        {
            return FromRawFailure(rightResult);
        }

        DnsName? optionalLeft = leftResult.Name;
        DnsName? optionalRight = rightResult.Name;
        if (optionalLeft == null || optionalRight == null)
        {
            return OperationFailure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.");
        }

        bool equal = optionalLeft.IsAbsolute == optionalRight.IsAbsolute &&
            optionalLeft.Labels.Count == optionalRight.Labels.Count;
        int index = 0;
        while (equal && index < optionalLeft.Labels.Count)
        {
            equal = LabelsEqual(
                optionalLeft.Labels[index],
                optionalRight.Labels[index]);
            index = index + 1;
        }

        DnsNameOperationResult result = new DnsNameOperationResult();
        result.Success = true;
        result.BooleanValue = equal;
        return result;
    }

    internal static DnsNameOperationResult CompareNames(DnsName left, DnsName right)
    {
        RawDnsNameResult leftResult = RawDnsName.ValidateName(left);
        if (!leftResult.Success)
        {
            return FromRawFailure(leftResult);
        }

        RawDnsNameResult rightResult = RawDnsName.ValidateName(right);
        if (!rightResult.Success)
        {
            return FromRawFailure(rightResult);
        }

        DnsName? optionalLeft = leftResult.Name;
        DnsName? optionalRight = rightResult.Name;
        if (optionalLeft == null || optionalRight == null)
        {
            return OperationFailure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.");
        }

        int leftIndex = optionalLeft.Labels.Count - 1;
        int rightIndex = optionalRight.Labels.Count - 1;
        int comparison = 0;
        while (comparison == 0 && leftIndex >= 0 && rightIndex >= 0)
        {
            comparison = CompareLabels(
                optionalLeft.Labels[leftIndex],
                optionalRight.Labels[rightIndex]);
            leftIndex = leftIndex - 1;
            rightIndex = rightIndex - 1;
        }

        if (comparison == 0)
        {
            if (optionalLeft.Labels.Count < optionalRight.Labels.Count)
            {
                comparison = -1;
            }
            else if (optionalLeft.Labels.Count > optionalRight.Labels.Count)
            {
                comparison = 1;
            }
            else if (optionalLeft.IsAbsolute != optionalRight.IsAbsolute)
            {
                if (optionalLeft.IsAbsolute)
                {
                    comparison = 1;
                }
                else
                {
                    comparison = -1;
                }
            }
        }

        DnsNameOperationResult result = new DnsNameOperationResult();
        result.Success = true;
        result.Comparison = comparison;
        result.BooleanValue = comparison == 0;
        return result;
    }

    internal static DnsNameOperationResult HasNameSuffix(
        DnsName name,
        DnsName suffix)
    {
        RawDnsNameResult nameResult = RawDnsName.ValidateName(name);
        if (!nameResult.Success)
        {
            return FromRawFailure(nameResult);
        }

        RawDnsNameResult suffixResult = RawDnsName.ValidateName(suffix);
        if (!suffixResult.Success)
        {
            return FromRawFailure(suffixResult);
        }

        DnsName? optionalName = nameResult.Name;
        DnsName? optionalSuffix = suffixResult.Name;
        if (optionalName == null || optionalSuffix == null)
        {
            return OperationFailure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.");
        }

        DnsNameOperationResult result = new DnsNameOperationResult();
        result.Success = true;
        result.BooleanValue = SuffixMatches(optionalName, optionalSuffix);
        return result;
    }

    internal static DnsNameOperationResult IsAncestorName(
        DnsName ancestor,
        DnsName name)
    {
        RawDnsNameResult ancestorResult = RawDnsName.ValidateName(ancestor);
        if (!ancestorResult.Success)
        {
            return FromRawFailure(ancestorResult);
        }

        RawDnsNameResult nameResult = RawDnsName.ValidateName(name);
        if (!nameResult.Success)
        {
            return FromRawFailure(nameResult);
        }

        DnsName? optionalAncestor = ancestorResult.Name;
        DnsName? optionalName = nameResult.Name;
        if (optionalAncestor == null || optionalName == null)
        {
            return OperationFailure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.");
        }

        DnsNameOperationResult result = new DnsNameOperationResult();
        result.Success = true;
        result.BooleanValue = optionalAncestor.Labels.Count < optionalName.Labels.Count &&
            SuffixMatches(optionalName, optionalAncestor);
        return result;
    }

    internal static DnsNameOperationResult CountNameLabels(DnsName name)
    {
        RawDnsNameResult nameResult = RawDnsName.ValidateName(name);
        if (!nameResult.Success)
        {
            return FromRawFailure(nameResult);
        }

        DnsNameOperationResult result = new DnsNameOperationResult();
        result.Success = true;
        result.LabelCount = nameResult.LabelCount;
        return result;
    }

    internal static DnsNameOperationResult GetParentName(DnsName name)
    {
        RawDnsNameResult nameResult = RawDnsName.ValidateName(name);
        if (!nameResult.Success)
        {
            return FromRawFailure(nameResult);
        }

        DnsName? optionalName = nameResult.Name;
        if (optionalName == null)
        {
            return OperationFailure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.");
        }

        if (optionalName.Labels.Count == 0)
        {
            return OperationFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "name",
                "A zero-label name has no parent.");
        }

        List<byte[]> parentLabels = new List<byte[]>();
        int index = 1;
        while (index < optionalName.Labels.Count)
        {
            parentLabels.Add(optionalName.Labels[index]);
            index = index + 1;
        }

        RawDnsNameResult parentResult = RawDnsName.BuildName(
            parentLabels,
            optionalName.IsAbsolute);
        return FromRawName(parentResult);
    }

    internal static DnsNameOperationResult ConcatenateNames(
        DnsName prefix,
        DnsName suffix)
    {
        RawDnsNameResult prefixResult = RawDnsName.ValidateName(prefix);
        if (!prefixResult.Success)
        {
            return FromRawFailure(prefixResult);
        }

        RawDnsNameResult suffixResult = RawDnsName.ValidateName(suffix);
        if (!suffixResult.Success)
        {
            return FromRawFailure(suffixResult);
        }

        DnsName? optionalPrefix = prefixResult.Name;
        DnsName? optionalSuffix = suffixResult.Name;
        if (optionalPrefix == null || optionalSuffix == null)
        {
            return OperationFailure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.");
        }

        if (optionalPrefix.IsAbsolute)
        {
            return OperationFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "prefix",
                "An absolute name cannot be used as a concatenation prefix.");
        }

        List<byte[]> labels = new List<byte[]>();
        int prefixIndex = 0;
        while (prefixIndex < optionalPrefix.Labels.Count)
        {
            labels.Add(optionalPrefix.Labels[prefixIndex]);
            prefixIndex = prefixIndex + 1;
        }

        int suffixIndex = 0;
        while (suffixIndex < optionalSuffix.Labels.Count)
        {
            labels.Add(optionalSuffix.Labels[suffixIndex]);
            suffixIndex = suffixIndex + 1;
        }

        RawDnsNameResult combinedResult = RawDnsName.BuildName(
            labels,
            optionalSuffix.IsAbsolute);
        return FromRawName(combinedResult);
    }

    internal static DnsNameOperationResult RelativizeName(
        DnsName name,
        DnsName origin)
    {
        RawDnsNameResult nameResult = RawDnsName.ValidateName(name);
        if (!nameResult.Success)
        {
            return FromRawFailure(nameResult);
        }

        RawDnsNameResult originResult = RawDnsName.ValidateName(origin);
        if (!originResult.Success)
        {
            return FromRawFailure(originResult);
        }

        DnsName? optionalName = nameResult.Name;
        DnsName? optionalOrigin = originResult.Name;
        if (optionalName == null || optionalOrigin == null)
        {
            return OperationFailure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.");
        }

        if (!optionalName.IsAbsolute)
        {
            return OperationFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "name",
                "Only an absolute DNS name can be relativized.");
        }

        if (!optionalOrigin.IsAbsolute)
        {
            return OperationFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "origin",
                "A relativization origin must be absolute.");
        }

        if (!SuffixMatches(optionalName, optionalOrigin))
        {
            return OperationFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "origin",
                "The origin is not a suffix of the DNS name.");
        }

        int relativeLabelCount = optionalName.Labels.Count - optionalOrigin.Labels.Count;
        List<byte[]> labels = new List<byte[]>();
        int index = 0;
        while (index < relativeLabelCount)
        {
            labels.Add(optionalName.Labels[index]);
            index = index + 1;
        }

        RawDnsNameResult relativeResult = RawDnsName.CreateRelativeName(labels);
        return FromRawName(relativeResult);
    }

    internal static DnsNameOperationResult FindCommonAncestor(
        DnsName first,
        DnsName second)
    {
        RawDnsNameResult firstResult = RawDnsName.ValidateName(first);
        if (!firstResult.Success)
        {
            return FromRawFailure(firstResult);
        }

        RawDnsNameResult secondResult = RawDnsName.ValidateName(second);
        if (!secondResult.Success)
        {
            return FromRawFailure(secondResult);
        }

        DnsName? optionalFirst = firstResult.Name;
        DnsName? optionalSecond = secondResult.Name;
        if (optionalFirst == null || optionalSecond == null)
        {
            return OperationFailure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.");
        }

        if (optionalFirst.IsAbsolute != optionalSecond.IsAbsolute)
        {
            return OperationFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "name",
                "Names with different terminal states have no common ancestor.");
        }

        int firstIndex = optionalFirst.Labels.Count - 1;
        int secondIndex = optionalSecond.Labels.Count - 1;
        int matchCount = 0;
        while (firstIndex >= 0 && secondIndex >= 0 &&
            LabelsEqual(
                optionalFirst.Labels[firstIndex],
                optionalSecond.Labels[secondIndex]))
        {
            matchCount = matchCount + 1;
            firstIndex = firstIndex - 1;
            secondIndex = secondIndex - 1;
        }

        List<byte[]> labels = new List<byte[]>();
        int copyIndex = optionalFirst.Labels.Count - matchCount;
        while (copyIndex < optionalFirst.Labels.Count)
        {
            labels.Add(optionalFirst.Labels[copyIndex]);
            copyIndex = copyIndex + 1;
        }

        RawDnsNameResult ancestorResult = RawDnsName.BuildName(
            labels,
            optionalFirst.IsAbsolute);
        return FromRawName(ancestorResult);
    }

    internal static bool SuffixMatches(DnsName name, DnsName suffix)
    {
        if (name.IsAbsolute != suffix.IsAbsolute ||
            suffix.Labels.Count > name.Labels.Count)
        {
            return false;
        }

        int nameIndex = name.Labels.Count - 1;
        int suffixIndex = suffix.Labels.Count - 1;
        while (suffixIndex >= 0)
        {
            if (!LabelsEqual(name.Labels[nameIndex], suffix.Labels[suffixIndex]))
            {
                return false;
            }

            nameIndex = nameIndex - 1;
            suffixIndex = suffixIndex - 1;
        }

        return true;
    }

    internal static bool LabelsEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (FoldAscii(left[index]) != FoldAscii(right[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int CompareLabels(byte[] left, byte[] right)
    {
        int commonLength = left.Length;
        if (right.Length < commonLength)
        {
            commonLength = right.Length;
        }

        int index = 0;
        while (index < commonLength)
        {
            int leftValue = FoldAscii(left[index]);
            int rightValue = FoldAscii(right[index]);
            if (leftValue < rightValue)
            {
                return -1;
            }

            if (leftValue > rightValue)
            {
                return 1;
            }

            index = index + 1;
        }

        if (left.Length < right.Length)
        {
            return -1;
        }

        if (left.Length > right.Length)
        {
            return 1;
        }

        return 0;
    }

    internal static int FoldAscii(byte value)
    {
        if (value >= 65 && value <= 90)
        {
            return value + 32;
        }

        return value;
    }

    internal static DnsNameOperationResult FromRawName(RawDnsNameResult rawResult)
    {
        if (!rawResult.Success)
        {
            return FromRawFailure(rawResult);
        }

        DnsNameOperationResult result = new DnsNameOperationResult();
        result.Success = true;
        result.Name = rawResult.Name;
        return result;
    }

    internal static DnsNameOperationResult FromRawFailure(RawDnsNameResult rawResult)
    {
        DnsNameOperationResult result = new DnsNameOperationResult();
        int index = 0;
        while (index < rawResult.Diagnostics.Count)
        {
            result.Diagnostics.Add(rawResult.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    internal static DnsNameOperationResult OperationFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        DnsNameOperationResult result = new DnsNameOperationResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
