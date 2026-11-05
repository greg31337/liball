using System;

namespace uri;

internal static class UriComparerCore
{
    internal static UriComparisonCoreResult CompareExact(
        UriReference? left,
        UriReference? right)
    {
        ValidateComparisonInputs(left, right);
        if (left == null || right == null)
        {
            throw new ArgumentException("Two URI references are required.");
        }

        UriComponent difference = FindFirstDifferentComponent(left, right);
        return new UriComparisonCoreResult(
            difference == UriComponent.None,
            difference);
    }

    internal static UriComparisonCoreResult CompareSyntax(
        UriReference? left,
        UriReference? right)
    {
        ValidateComparisonInputs(left, right);
        UriReference normalizedLeft = UriNormalizerCore.NormalizeSyntaxReference(left);
        UriReference normalizedRight = UriNormalizerCore.NormalizeSyntaxReference(right);
        return CompareExact(normalizedLeft, normalizedRight);
    }

    internal static UriComparisonCoreResult CompareWithSchemeProfile(
        UriReference? left,
        UriReference? right,
        UriSchemeProfile? profile)
    {
        ValidateComparisonInputs(left, right);
        if (profile == null)
        {
            throw new ArgumentException("A URI scheme profile is required.");
        }

        UriReference normalizedLeft = UriNormalizerCore.NormalizeWithSchemeProfile(
            left,
            profile);
        UriReference normalizedRight = UriNormalizerCore.NormalizeWithSchemeProfile(
            right,
            profile);
        return CompareExact(normalizedLeft, normalizedRight);
    }

    internal static UriComparisonCoreResult CompareWithBuiltInSchemeProfiles(
        UriReference? left,
        UriReference? right)
    {
        ValidateComparisonInputs(left, right);
        UriReference normalizedLeft = UriNormalizerCore.NormalizeWithBuiltInSchemeProfile(
            left);
        UriReference normalizedRight = UriNormalizerCore.NormalizeWithBuiltInSchemeProfile(
            right);
        return CompareExact(normalizedLeft, normalizedRight);
    }

    internal static UriComponent FindFirstDifferentComponent(
        UriReference left,
        UriReference right)
    {
        if (left.HasScheme != right.HasScheme)
        {
            return UriComponent.Scheme;
        }

        if (left.HasScheme && left.Scheme != right.Scheme)
        {
            return UriComponent.Scheme;
        }

        if (left.HasAuthority != right.HasAuthority)
        {
            return UriComponent.Authority;
        }

        if (left.HasAuthority)
        {
            UriAuthority leftAuthority = left.Authority;
            UriAuthority rightAuthority = right.Authority;
            if (leftAuthority.HasUserInfo != rightAuthority.HasUserInfo)
            {
                return UriComponent.UserInfo;
            }

            if (leftAuthority.HasUserInfo &&
                leftAuthority.UserInfo != rightAuthority.UserInfo)
            {
                return UriComponent.UserInfo;
            }

            if (leftAuthority.HostKind != rightAuthority.HostKind ||
                leftAuthority.Host != rightAuthority.Host)
            {
                return UriComponent.Host;
            }

            if (leftAuthority.HasPort != rightAuthority.HasPort)
            {
                return UriComponent.Port;
            }

            if (leftAuthority.HasPort &&
                leftAuthority.Port != rightAuthority.Port)
            {
                return UriComponent.Port;
            }
        }

        if (left.Path != right.Path)
        {
            return UriComponent.Path;
        }

        if (left.HasQuery != right.HasQuery)
        {
            return UriComponent.Query;
        }

        if (left.HasQuery && left.Query != right.Query)
        {
            return UriComponent.Query;
        }

        if (left.HasFragment != right.HasFragment)
        {
            return UriComponent.Fragment;
        }

        if (left.HasFragment && left.Fragment != right.Fragment)
        {
            return UriComponent.Fragment;
        }

        return UriComponent.None;
    }

    internal static void ValidateComparisonInputs(
        UriReference? left,
        UriReference? right)
    {
        if (left == null ||
            right == null ||
            left.Authority == null ||
            right.Authority == null)
        {
            throw new ArgumentException("Two URI references are required.");
        }
    }
}
