namespace version;

internal static class CargoCandidateFilter
{
    internal static CargoPackageMetadata[] Filter(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options)
    {
        return Filter(dependency, candidates, options, null);
    }

    internal static CargoPackageMetadata[] Filter(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options,
        CargoResolvedDependencyFeatures? selectedFeatures)
    {
        System.ArgumentNullException.ThrowIfNull(dependency);
        System.ArgumentNullException.ThrowIfNull(candidates);
        System.ArgumentNullException.ThrowIfNull(options);
        ValidateCandidates(candidates);
        ValidateSelectedFeatures(dependency, selectedFeatures);

        System.Collections.Generic.List<CargoPackageMetadata> filtered = new System.Collections.Generic.List<CargoPackageMetadata>();
        int index = 0;

        while (index < candidates.Length)
        {
            if (MatchesCandidate(dependency, candidates[index], options, selectedFeatures))
            {
                filtered.Add(candidates[index]);
            }

            index = index + 1;
        }

        return filtered.ToArray();
    }

    internal static CargoPackageMetadata? SelectHighest(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options)
    {
        return SelectHighest(dependency, candidates, options, null);
    }

    internal static CargoPackageMetadata? SelectHighest(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options,
        CargoResolvedDependencyFeatures? selectedFeatures)
    {
        CargoPackageMetadata[] filtered = Filter(dependency, candidates, options, selectedFeatures);
        CargoPackageMetadata? selected = null;
        int index = 0;

        while (index < filtered.Length)
        {
            if (selected == null || CargoVersions.Compare(filtered[index].GetIdentity().GetVersionText(), selected.GetIdentity().GetVersionText()) > 0)
            {
                selected = filtered[index];
            }

            index = index + 1;
        }

        return selected;
    }

    internal static bool MatchesCandidate(
        CargoDependency dependency,
        CargoPackageMetadata candidate,
        CargoResolverOptions options,
        CargoResolvedDependencyFeatures? selectedFeatures)
    {
        if (!candidate.GetIdentity().GetPackageName().EqualsName(dependency.GetPackageName()))
        {
            return false;
        }

        if (candidate.IsYanked() && !options.IncludesYankedVersions())
        {
            return false;
        }

        if (!MatchesVersionRequirement(dependency, candidate, options))
        {
            return false;
        }

        if (!MatchesRegistry(dependency, candidate, options))
        {
            return false;
        }

        return SupportsSelectedFeatures(candidate, selectedFeatures);
    }

    internal static bool MatchesVersionRequirement(
        CargoDependency dependency,
        CargoPackageMetadata candidate,
        CargoResolverOptions options)
    {
        UniversalVersion version = candidate.GetIdentity().GetVersion();

        if (!dependency.HasVersionRequirement())
        {
            return options.IncludesPrereleaseVersions() || !version.IsPrerelease();
        }

        return dependency.GetVersionRequirement().ContainsVersionWithPrerelease(version, options.IncludesPrereleaseVersions());
    }

    internal static bool MatchesRegistry(
        CargoDependency dependency,
        CargoPackageMetadata candidate,
        CargoResolverOptions options)
    {
        string sourceName = "crates-io";
        if (dependency.HasRegistryName())
        {
            sourceName = dependency.GetRegistryName();
        }

        CargoRegistrySource? expectedSource = ResolveExpectedSource(sourceName, options);
        if (expectedSource == null)
        {
            return candidate.GetRegistrySource().MatchesName(sourceName);
        }

        return RegistrySourcesEqual(candidate.GetRegistrySource(), expectedSource);
    }

    internal static CargoRegistrySource? ResolveExpectedSource(string sourceName, CargoResolverOptions options)
    {
        int replacementIndex = 0;

        while (replacementIndex < options.GetSourceReplacementCount())
        {
            CargoSourceReplacement replacement = options.GetSourceReplacement(replacementIndex);
            if (replacement.ReplacesSource(sourceName))
            {
                return replacement.GetReplacementSource();
            }

            replacementIndex = replacementIndex + 1;
        }

        int sourceIndex = 0;

        while (sourceIndex < options.GetRegistrySourceCount())
        {
            CargoRegistrySource source = options.GetRegistrySource(sourceIndex);
            if (source.MatchesName(sourceName))
            {
                return source;
            }

            sourceIndex = sourceIndex + 1;
        }

        return null;
    }

    internal static bool SupportsSelectedFeatures(CargoPackageMetadata candidate, CargoResolvedDependencyFeatures? selectedFeatures)
    {
        if (selectedFeatures == null)
        {
            return true;
        }

        int index = 0;

        while (index < selectedFeatures.GetFeatureCount())
        {
            if (!CandidateHasFeature(candidate, selectedFeatures.GetFeature(index)))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool CandidateHasFeature(CargoPackageMetadata candidate, string featureName)
    {
        int index = 0;

        while (index < candidate.GetFeatureCount())
        {
            if (candidate.GetFeature(index).GetName() == featureName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool RegistrySourcesEqual(CargoRegistrySource left, CargoRegistrySource right)
    {
        return left.MatchesName(right.GetName()) && left.GetIndexUrl() == right.GetIndexUrl();
    }

    internal static void ValidateSelectedFeatures(CargoDependency dependency, CargoResolvedDependencyFeatures? selectedFeatures)
    {
        if (selectedFeatures == null)
        {
            return;
        }

        if (!selectedFeatures.GetPackageName().EqualsName(dependency.GetPackageName()))
        {
            throw new System.ArgumentException("Cargo selected dependency features must belong to the dependency package.");
        }
    }

    internal static void ValidateCandidates(CargoPackageMetadata[] candidates)
    {
        int index = 0;

        while (index < candidates.Length)
        {
            if (candidates[index] == null)
            {
                throw new System.ArgumentException("Cargo candidate arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }
}
