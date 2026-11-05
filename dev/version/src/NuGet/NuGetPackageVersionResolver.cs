namespace version;

internal static class NuGetPackageVersionResolver
{
    internal static NuGetSourcePackageMetadata? ResolveLowestApplicableVersion(
        NuGetDependency dependency,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        return ResolveLowestApplicableVersion(
            dependency,
            availablePackages,
            sourcePriorities,
            NuGetPackageRejectionPolicy.CreateDefault(),
            VersionSelectionOptions.CreateDefault());
    }

    internal static NuGetSourcePackageMetadata? ResolveLowestApplicableVersion(
        NuGetDependency dependency,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        VersionSelectionOptions selectionOptions)
    {
        return ResolveLowestApplicableVersion(
            dependency,
            availablePackages,
            sourcePriorities,
            NuGetPackageRejectionPolicy.CreateDefault(),
            selectionOptions);
    }

    internal static NuGetSourcePackageMetadata? ResolveLowestApplicableVersion(
        NuGetDependency dependency,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        NuGetPackageRejectionPolicy rejectionPolicy,
        VersionSelectionOptions selectionOptions)
    {
        System.ArgumentNullException.ThrowIfNull(dependency);
        System.ArgumentNullException.ThrowIfNull(availablePackages);
        System.ArgumentNullException.ThrowIfNull(sourcePriorities);
        System.ArgumentNullException.ThrowIfNull(rejectionPolicy);
        System.ArgumentNullException.ThrowIfNull(selectionOptions);
        ValidateAvailablePackages(availablePackages);
        ValidateSourcePriorities(sourcePriorities);

        if (selectionOptions.GetPrereleasePolicy() == PrereleasePolicy.AutoIfNoStableCandidate)
        {
            NuGetSourcePackageMetadata? stable = SelectLowest(dependency, availablePackages, sourcePriorities, rejectionPolicy, PrereleasePolicy.Exclude);
            if (stable != null)
            {
                return stable;
            }

            return SelectLowest(dependency, availablePackages, sourcePriorities, rejectionPolicy, PrereleasePolicy.Include);
        }

        return SelectLowest(dependency, availablePackages, sourcePriorities, rejectionPolicy, selectionOptions.GetPrereleasePolicy());
    }

    internal static NuGetSourcePackageMetadata? ResolveFloatingVersion(
        string packageIdText,
        string floatingVersionText,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        return ResolveFloatingVersion(
            packageIdText,
            floatingVersionText,
            availablePackages,
            sourcePriorities,
            NuGetPackageRejectionPolicy.CreateDefault());
    }

    internal static NuGetSourcePackageMetadata? ResolveFloatingVersion(
        string packageIdText,
        string floatingVersionText,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        System.ArgumentNullException.ThrowIfNull(floatingVersionText);
        System.ArgumentNullException.ThrowIfNull(availablePackages);
        System.ArgumentNullException.ThrowIfNull(sourcePriorities);
        System.ArgumentNullException.ThrowIfNull(rejectionPolicy);
        ValidateAvailablePackages(availablePackages);
        ValidateSourcePriorities(sourcePriorities);

        NuGetPackageId packageId = new NuGetPackageId(packageIdText);
        NuGetFloatingVersion floatingVersion = NuGetFloatingVersion.Parse(floatingVersionText);
        if (!floatingVersion.IsValid())
        {
            throw new System.ArgumentException("Invalid NuGet floating version pattern.");
        }

        NuGetSourcePackageMetadata? selected = null;
        int index = 0;

        while (index < availablePackages.Length)
        {
            if (CanUsePackage(availablePackages[index], sourcePriorities, rejectionPolicy)
                && availablePackages[index].GetIdentity().GetPackageId().EqualsId(packageId)
                && floatingVersion.ContainsVersion(availablePackages[index].GetIdentity().GetVersion()))
            {
                if (selected == null || CompareFloatingCandidates(availablePackages[index], selected, sourcePriorities) > 0)
                {
                    selected = availablePackages[index];
                }
            }

            index = index + 1;
        }

        return selected;
    }

    internal static NuGetSourcePackageMetadata? SelectLowest(
        NuGetDependency dependency,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        NuGetPackageRejectionPolicy rejectionPolicy,
        PrereleasePolicy prereleasePolicy)
    {
        NuGetSourcePackageMetadata? selected = null;
        int index = 0;

        while (index < availablePackages.Length)
        {
            if (CanUsePackage(availablePackages[index], sourcePriorities, rejectionPolicy)
                && MatchesDependency(dependency, availablePackages[index], prereleasePolicy))
            {
                if (selected == null || CompareLowestCandidates(availablePackages[index], selected, sourcePriorities) < 0)
                {
                    selected = availablePackages[index];
                }
            }

            index = index + 1;
        }

        return selected;
    }

    internal static bool MatchesDependency(
        NuGetDependency dependency,
        NuGetSourcePackageMetadata packageMetadata,
        PrereleasePolicy prereleasePolicy)
    {
        if (!packageMetadata.GetIdentity().GetPackageId().EqualsId(dependency.GetPackageId()))
        {
            return false;
        }

        if (prereleasePolicy == PrereleasePolicy.Include)
        {
            return dependency.GetVersionRange().ContainsVersionWithPrerelease(packageMetadata.GetIdentity().GetVersion(), true);
        }

        if (prereleasePolicy == PrereleasePolicy.Exclude)
        {
            return !packageMetadata.GetIdentity().GetVersion().IsPrerelease()
                && dependency.GetVersionRange().ContainsVersionWithPrerelease(packageMetadata.GetIdentity().GetVersion(), true);
        }

        return dependency.GetVersionRange().ContainsVersionWithPrerelease(packageMetadata.GetIdentity().GetVersion(), false);
    }

    internal static int CompareLowestCandidates(
        NuGetSourcePackageMetadata left,
        NuGetSourcePackageMetadata right,
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        int versionComparison = NuGetVersionComparer.Compare(left.GetIdentity().GetVersion(), right.GetIdentity().GetVersion());
        if (versionComparison != 0)
        {
            return versionComparison;
        }

        return CompareSourcePriority(left, right, sourcePriorities);
    }

    internal static int CompareFloatingCandidates(
        NuGetSourcePackageMetadata left,
        NuGetSourcePackageMetadata right,
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        int versionComparison = NuGetVersionComparer.Compare(left.GetIdentity().GetVersion(), right.GetIdentity().GetVersion());
        if (versionComparison != 0)
        {
            return versionComparison;
        }

        return 0 - CompareSourcePriority(left, right, sourcePriorities);
    }

    internal static int CompareSourcePriority(
        NuGetSourcePackageMetadata left,
        NuGetSourcePackageMetadata right,
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        NuGetPackageSourcePriority? leftPriority = FindSourcePriority(sourcePriorities, left.GetSource());
        NuGetPackageSourcePriority? rightPriority = FindSourcePriority(sourcePriorities, right.GetSource());

        if (leftPriority == null && rightPriority == null)
        {
            return 0;
        }

        if (leftPriority == null)
        {
            return 1;
        }

        if (rightPriority == null)
        {
            return -1;
        }

        if (leftPriority.ComesBefore(rightPriority))
        {
            return -1;
        }

        if (rightPriority.ComesBefore(leftPriority))
        {
            return 1;
        }

        return 0;
    }

    internal static bool CanUsePackage(
        NuGetSourcePackageMetadata packageMetadata,
        NuGetPackageSourcePriority[] sourcePriorities,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        if (rejectionPolicy.ShouldReject(packageMetadata))
        {
            return false;
        }

        if (sourcePriorities.Length == 0)
        {
            return true;
        }

        NuGetPackageSourcePriority? priority = FindSourcePriority(sourcePriorities, packageMetadata.GetSource());
        if (priority == null)
        {
            return false;
        }

        return priority.IsEnabled();
    }

    internal static NuGetPackageSourcePriority? FindSourcePriority(
        NuGetPackageSourcePriority[] sourcePriorities,
        NuGetSourceIdentity source)
    {
        int index = 0;

        while (index < sourcePriorities.Length)
        {
            if (sourcePriorities[index].GetSource().EqualsSource(source))
            {
                return sourcePriorities[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static void ValidateAvailablePackages(NuGetSourcePackageMetadata[] availablePackages)
    {
        int index = 0;

        while (index < availablePackages.Length)
        {
            if (availablePackages[index] == null)
            {
                throw new System.ArgumentException("NuGet source package metadata arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateSourcePriorities(NuGetPackageSourcePriority[] sourcePriorities)
    {
        int index = 0;

        while (index < sourcePriorities.Length)
        {
            if (sourcePriorities[index] == null)
            {
                throw new System.ArgumentException("NuGet source priority arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }
}
