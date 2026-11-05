namespace version;

internal class CargoFeatureResolver
{
    internal CargoManifest manifest;
    internal CargoResolverOptions options;
    internal string[] requestedFeatures;
    internal bool includeDefaultFeatures;
    internal bool includeDevelopmentDependencies;
    internal System.Collections.Generic.List<string> pendingRootFeatures;
    internal System.Collections.Generic.List<string> enabledRootFeatures;
    internal System.Collections.Generic.List<string> activatedOptionalDependencies;
    internal System.Collections.Generic.List<CargoDependencyFeatureRequest> dependencyFeatureRequests;

    internal CargoFeatureResolver(
        CargoManifest manifestValue,
        CargoResolverOptions optionsValue,
        string[] requestedFeatureValues,
        bool includeDefaultFeaturesValue,
        bool includeDevelopmentDependenciesValue)
    {
        System.ArgumentNullException.ThrowIfNull(manifestValue);
        System.ArgumentNullException.ThrowIfNull(optionsValue);
        System.ArgumentNullException.ThrowIfNull(requestedFeatureValues);

        manifest = manifestValue;
        options = optionsValue;
        requestedFeatures = CopyRequestedFeatures(requestedFeatureValues);
        includeDefaultFeatures = includeDefaultFeaturesValue;
        includeDevelopmentDependencies = includeDevelopmentDependenciesValue;
        pendingRootFeatures = new System.Collections.Generic.List<string>();
        enabledRootFeatures = new System.Collections.Generic.List<string>();
        activatedOptionalDependencies = new System.Collections.Generic.List<string>();
        dependencyFeatureRequests = new System.Collections.Generic.List<CargoDependencyFeatureRequest>();
    }

    internal static CargoFeatureResolution Resolve(
        CargoManifest manifest,
        CargoResolverOptions options,
        string[] requestedFeatures,
        bool includeDefaultFeatures,
        bool includeDevelopmentDependencies)
    {
        CargoFeatureResolver resolver = new CargoFeatureResolver(
            manifest,
            options,
            requestedFeatures,
            includeDefaultFeatures,
            includeDevelopmentDependencies);

        return resolver.ResolveFeatures();
    }

    internal CargoFeatureResolution ResolveFeatures()
    {
        AddInitialRootFeatures();
        ProcessPendingRootFeatures();

        CargoDependency[] activeDependencies = SelectActiveDependencies();
        CargoResolvedDependencyFeatures[] dependencyFeatures = ResolveDependencyFeatures(activeDependencies);

        return new CargoFeatureResolution(
            enabledRootFeatures.ToArray(),
            activeDependencies,
            dependencyFeatures);
    }

    internal void AddInitialRootFeatures()
    {
        if (includeDefaultFeatures && FindFeature("default") != null)
        {
            AddPendingRootFeature("default");
        }

        int index = 0;
        while (index < requestedFeatures.Length)
        {
            ActivateFeatureToken(requestedFeatures[index]);
            index = index + 1;
        }
    }

    internal void ProcessPendingRootFeatures()
    {
        int index = 0;

        while (index < pendingRootFeatures.Count)
        {
            CargoFeature? feature = FindFeature(pendingRootFeatures[index]);
            if (feature != null)
            {
                AddEnabledRootFeature(feature.GetName());
                int memberIndex = 0;

                while (memberIndex < feature.GetMemberCount())
                {
                    ActivateFeatureToken(feature.GetMember(memberIndex));
                    memberIndex = memberIndex + 1;
                }
            }

            index = index + 1;
        }
    }

    internal void ActivateFeatureToken(string featureToken)
    {
        CargoFeature.ValidateFeatureMemberToken(featureToken, "Cargo requested feature cannot be empty or contain invalid characters.");

        if (StartsWithText(featureToken, "dep:"))
        {
            ActivateOptionalDependency(featureToken.Substring("dep:".Length));
            return;
        }

        int slashIndex = FindSlash(featureToken);
        if (slashIndex >= 0)
        {
            ActivateDependencyFeature(featureToken, slashIndex);
            return;
        }

        if (FindFeature(featureToken) != null)
        {
            AddPendingRootFeature(featureToken);
            return;
        }

        if (FindOptionalDependency(featureToken) != null)
        {
            AddEnabledRootFeature(featureToken);
            ActivateOptionalDependency(featureToken);
            return;
        }

        AddEnabledRootFeature(featureToken);
    }

    internal void ActivateDependencyFeature(string featureToken, int slashIndex)
    {
        string dependencyName = featureToken.Substring(0, slashIndex);
        string featureName = featureToken.Substring(slashIndex + 1);
        bool conditional = false;

        if (dependencyName.Length > 0 && dependencyName[dependencyName.Length - 1] == '?')
        {
            dependencyName = dependencyName.Substring(0, dependencyName.Length - 1);
            conditional = true;
        }

        CargoPackageName.ValidateName(dependencyName, "Cargo dependency feature package name cannot contain invalid characters.");
        CargoFeature.ValidateFeatureToken(featureName, "Cargo dependency feature name cannot be empty or contain invalid characters.");

        if (!conditional)
        {
            ActivateOptionalDependency(dependencyName);
        }

        dependencyFeatureRequests.Add(new CargoDependencyFeatureRequest(dependencyName, featureName, conditional));
    }

    internal void AddPendingRootFeature(string featureName)
    {
        if (ContainsText(pendingRootFeatures, featureName))
        {
            return;
        }

        pendingRootFeatures.Add(featureName);
    }

    internal void AddEnabledRootFeature(string featureName)
    {
        if (ContainsText(enabledRootFeatures, featureName))
        {
            return;
        }

        enabledRootFeatures.Add(featureName);
    }

    internal void ActivateOptionalDependency(string packageName)
    {
        CargoDependency? dependency = FindOptionalDependency(packageName);
        if (dependency == null)
        {
            return;
        }

        if (ContainsText(activatedOptionalDependencies, dependency.GetPackageName().GetNormalizedValue()))
        {
            return;
        }

        activatedOptionalDependencies.Add(dependency.GetPackageName().GetNormalizedValue());
    }

    internal CargoDependency[] SelectActiveDependencies()
    {
        System.Collections.Generic.List<CargoDependency> activeDependencies = new System.Collections.Generic.List<CargoDependency>();
        int index = 0;

        while (index < manifest.GetDependencyCount())
        {
            CargoDependency dependency = manifest.GetDependency(index);
            if (IsDependencySelected(dependency))
            {
                activeDependencies.Add(dependency);
            }

            index = index + 1;
        }

        return activeDependencies.ToArray();
    }

    internal bool IsDependencySelected(CargoDependency dependency)
    {
        if (!IsDependencyTargetSelected(dependency))
        {
            return false;
        }

        if (dependency.GetDependencyKind() == CargoDependencyKind.Development && !includeDevelopmentDependencies)
        {
            return false;
        }

        if (dependency.IsOptional())
        {
            return ContainsText(activatedOptionalDependencies, dependency.GetPackageName().GetNormalizedValue());
        }

        return true;
    }

    internal CargoResolvedDependencyFeatures[] ResolveDependencyFeatures(CargoDependency[] activeDependencies)
    {
        System.Collections.Generic.List<CargoResolvedDependencyFeaturesBuilder> builders = new System.Collections.Generic.List<CargoResolvedDependencyFeaturesBuilder>();
        int index = 0;

        while (index < activeDependencies.Length)
        {
            CargoDependency dependency = activeDependencies[index];
            CargoResolvedDependencyFeaturesBuilder builder = FindOrAddBuilder(builders, dependency);
            AddFeaturesForDependency(builder, dependency, activeDependencies);
            AddRequestedDependencyFeatures(builder);
            index = index + 1;
        }

        return CreateResolvedDependencyFeatures(builders);
    }

    internal CargoResolvedDependencyFeaturesBuilder FindOrAddBuilder(
        System.Collections.Generic.List<CargoResolvedDependencyFeaturesBuilder> builders,
        CargoDependency dependency)
    {
        int index = 0;

        while (index < builders.Count)
        {
            if (builders[index].MatchesDependency(dependency, options.GetResolverVersion()))
            {
                return builders[index];
            }

            index = index + 1;
        }

        CargoResolvedDependencyFeaturesBuilder builder = new CargoResolvedDependencyFeaturesBuilder(
            dependency.GetPackageName(),
            dependency.GetDependencyKind());
        builders.Add(builder);
        return builder;
    }

    internal void AddFeaturesForDependency(
        CargoResolvedDependencyFeaturesBuilder builder,
        CargoDependency dependency,
        CargoDependency[] activeDependencies)
    {
        if (options.GetResolverVersion() == CargoResolverVersion.Version1)
        {
            AddVersionOneUnifiedFeatures(builder, dependency, activeDependencies);
            return;
        }

        AddDeclaredDependencyFeatures(builder, dependency);
    }

    internal void AddVersionOneUnifiedFeatures(
        CargoResolvedDependencyFeaturesBuilder builder,
        CargoDependency dependency,
        CargoDependency[] activeDependencies)
    {
        int index = 0;

        while (index < manifest.GetDependencyCount())
        {
            CargoDependency candidate = manifest.GetDependency(index);
            if (candidate.GetPackageName().EqualsName(dependency.GetPackageName()) && IsVersionOneFeatureContributor(candidate, activeDependencies))
            {
                AddDeclaredDependencyFeatures(builder, candidate);
            }

            index = index + 1;
        }
    }

    internal bool IsVersionOneFeatureContributor(CargoDependency dependency, CargoDependency[] activeDependencies)
    {
        if (dependency.IsOptional() && !ContainsText(activatedOptionalDependencies, dependency.GetPackageName().GetNormalizedValue()))
        {
            return false;
        }

        if (dependency.GetDependencyKind() == CargoDependencyKind.Development && !includeDevelopmentDependencies)
        {
            return HasActiveDependencyForPackage(activeDependencies, dependency.GetPackageName());
        }

        return true;
    }

    internal bool HasActiveDependencyForPackage(CargoDependency[] activeDependencies, CargoPackageName packageName)
    {
        int index = 0;

        while (index < activeDependencies.Length)
        {
            if (activeDependencies[index].GetPackageName().EqualsName(packageName))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddDeclaredDependencyFeatures(CargoResolvedDependencyFeaturesBuilder builder, CargoDependency dependency)
    {
        if (dependency.UsesDefaultFeatures())
        {
            builder.SetDefaultFeatures();
        }

        int index = 0;

        while (index < dependency.GetFeatureCount())
        {
            builder.AddFeature(dependency.GetFeature(index));
            index = index + 1;
        }
    }

    internal void AddRequestedDependencyFeatures(CargoResolvedDependencyFeaturesBuilder builder)
    {
        int index = 0;

        while (index < dependencyFeatureRequests.Count)
        {
            CargoDependencyFeatureRequest request = dependencyFeatureRequests[index];
            if (builder.GetPackageName().MatchesText(request.GetPackageName()))
            {
                if (!request.IsConditional() || ContainsText(activatedOptionalDependencies, builder.GetPackageName().GetNormalizedValue()))
                {
                    builder.AddFeature(request.GetFeatureName());
                }
            }

            index = index + 1;
        }
    }

    internal CargoResolvedDependencyFeatures[] CreateResolvedDependencyFeatures(System.Collections.Generic.List<CargoResolvedDependencyFeaturesBuilder> builders)
    {
        CargoResolvedDependencyFeatures[] values = new CargoResolvedDependencyFeatures[builders.Count];
        int index = 0;

        while (index < builders.Count)
        {
            values[index] = builders[index].Create();
            index = index + 1;
        }

        return values;
    }

    internal bool IsDependencyTargetSelected(CargoDependency dependency)
    {
        if (!dependency.HasTargetTriple())
        {
            return true;
        }

        if (options.GetResolverVersion() == CargoResolverVersion.Version1)
        {
            return true;
        }

        if (!options.HasTargetTriple())
        {
            return true;
        }

        return CargoTargetMatcher.Matches(dependency.GetTargetTriple(), options.GetTargetTriple());
    }

    internal CargoFeature? FindFeature(string featureName)
    {
        int index = 0;

        while (index < manifest.GetFeatureCount())
        {
            CargoFeature feature = manifest.GetFeature(index);
            if (feature.GetName() == featureName)
            {
                return feature;
            }

            index = index + 1;
        }

        return null;
    }

    internal CargoDependency? FindOptionalDependency(string packageName)
    {
        int index = 0;

        while (index < manifest.GetDependencyCount())
        {
            CargoDependency dependency = manifest.GetDependency(index);
            if (dependency.IsOptional() && dependency.GetPackageName().MatchesText(packageName))
            {
                return dependency;
            }

            index = index + 1;
        }

        return null;
    }

    internal int FindSlash(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '/')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool ContainsText(System.Collections.Generic.List<string> values, string value)
    {
        int index = 0;

        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsTextInArray(string[] values, string value)
    {
        int index = 0;

        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool StartsWithText(string text, string value)
    {
        if (text.Length < value.Length)
        {
            return false;
        }

        return text.Substring(0, value.Length) == value;
    }

    internal static string[] CopyRequestedFeatures(string[] values)
    {
        string[] copy = new string[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            CargoFeature.ValidateFeatureToken(values[index], "Cargo requested feature arrays cannot contain empty or invalid values.");
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }
}

internal class CargoResolvedDependencyFeaturesBuilder
{
    internal CargoPackageName packageName;
    internal CargoDependencyKind dependencyKind;
    internal bool defaultFeatures;
    internal System.Collections.Generic.List<string> features;

    internal CargoResolvedDependencyFeaturesBuilder(CargoPackageName packageNameValue, CargoDependencyKind dependencyKindValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageNameValue);

        packageName = packageNameValue;
        dependencyKind = dependencyKindValue;
        defaultFeatures = false;
        features = new System.Collections.Generic.List<string>();
    }

    internal CargoPackageName GetPackageName()
    {
        return packageName;
    }

    internal bool MatchesDependency(CargoDependency dependency, CargoResolverVersion resolverVersion)
    {
        if (!packageName.EqualsName(dependency.GetPackageName()))
        {
            return false;
        }

        if (resolverVersion == CargoResolverVersion.Version1)
        {
            return true;
        }

        return dependencyKind == dependency.GetDependencyKind();
    }

    internal void SetDefaultFeatures()
    {
        defaultFeatures = true;
    }

    internal void AddFeature(string featureName)
    {
        CargoFeature.ValidateFeatureToken(featureName, "Cargo resolved dependency feature cannot be empty or contain invalid characters.");

        if (ContainsFeature(featureName))
        {
            return;
        }

        features.Add(featureName);
    }

    internal CargoResolvedDependencyFeatures Create()
    {
        return new CargoResolvedDependencyFeatures(
            packageName,
            dependencyKind,
            defaultFeatures,
            features.ToArray());
    }

    internal bool ContainsFeature(string featureName)
    {
        int index = 0;

        while (index < features.Count)
        {
            if (features[index] == featureName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}

internal class CargoDependencyFeatureRequest
{
    internal string packageName;
    internal string featureName;
    internal bool conditional;

    internal CargoDependencyFeatureRequest(string packageNameValue, string featureNameValue, bool conditionalValue)
    {
        CargoPackageName.ValidateName(packageNameValue, "Cargo dependency feature package name cannot contain invalid characters.");
        CargoFeature.ValidateFeatureToken(featureNameValue, "Cargo dependency feature name cannot be empty or contain invalid characters.");

        packageName = packageNameValue;
        featureName = featureNameValue;
        conditional = conditionalValue;
    }

    internal string GetPackageName()
    {
        return packageName;
    }

    internal string GetFeatureName()
    {
        return featureName;
    }

    internal bool IsConditional()
    {
        return conditional;
    }
}

internal static class CargoTargetMatcher
{
    internal static bool Matches(string dependencyTarget, string activeTarget)
    {
        System.ArgumentNullException.ThrowIfNull(dependencyTarget);
        System.ArgumentNullException.ThrowIfNull(activeTarget);

        if (dependencyTarget == activeTarget)
        {
            return true;
        }

        if (dependencyTarget == "cfg(unix)")
        {
            return IsUnixTarget(activeTarget);
        }

        if (dependencyTarget == "cfg(windows)")
        {
            return IsWindowsTarget(activeTarget);
        }

        if (StartsWithText(dependencyTarget, "cfg(target_os"))
        {
            return MatchesTargetOs(dependencyTarget, activeTarget);
        }

        if (StartsWithText(dependencyTarget, "cfg(target_arch"))
        {
            return MatchesTargetArch(dependencyTarget, activeTarget);
        }

        return false;
    }

    internal static bool MatchesTargetOs(string dependencyTarget, string activeTarget)
    {
        string expected = ReadCfgStringValue(dependencyTarget);
        if (expected.Length == 0)
        {
            return false;
        }

        return InferTargetOs(activeTarget) == expected;
    }

    internal static bool MatchesTargetArch(string dependencyTarget, string activeTarget)
    {
        string expected = ReadCfgStringValue(dependencyTarget);
        if (expected.Length == 0)
        {
            return false;
        }

        return InferTargetArch(activeTarget) == expected;
    }

    internal static string ReadCfgStringValue(string dependencyTarget)
    {
        int firstQuote = dependencyTarget.IndexOf('"');
        if (firstQuote < 0)
        {
            firstQuote = dependencyTarget.IndexOf('\'');
        }

        if (firstQuote < 0)
        {
            return "";
        }

        char quote = dependencyTarget[firstQuote];
        int endQuote = firstQuote + 1;

        while (endQuote < dependencyTarget.Length)
        {
            if (dependencyTarget[endQuote] == quote)
            {
                return dependencyTarget.Substring(firstQuote + 1, endQuote - firstQuote - 1);
            }

            endQuote = endQuote + 1;
        }

        return "";
    }

    internal static string InferTargetArch(string activeTarget)
    {
        int dashIndex = activeTarget.IndexOf('-');
        if (dashIndex < 0)
        {
            return activeTarget;
        }

        return activeTarget.Substring(0, dashIndex);
    }

    internal static string InferTargetOs(string activeTarget)
    {
        if (IsWindowsTarget(activeTarget))
        {
            return "windows";
        }

        if (ContainsText(activeTarget, "apple-darwin") || ContainsText(activeTarget, "darwin"))
        {
            return "macos";
        }

        if (ContainsText(activeTarget, "linux"))
        {
            return "linux";
        }

        if (ContainsText(activeTarget, "freebsd"))
        {
            return "freebsd";
        }

        if (ContainsText(activeTarget, "android"))
        {
            return "android";
        }

        return "";
    }

    internal static bool IsUnixTarget(string activeTarget)
    {
        if (IsWindowsTarget(activeTarget))
        {
            return false;
        }

        if (ContainsText(activeTarget, "linux"))
        {
            return true;
        }

        if (ContainsText(activeTarget, "darwin"))
        {
            return true;
        }

        if (ContainsText(activeTarget, "freebsd"))
        {
            return true;
        }

        if (ContainsText(activeTarget, "android"))
        {
            return true;
        }

        return false;
    }

    internal static bool IsWindowsTarget(string activeTarget)
    {
        return ContainsText(activeTarget, "windows") || ContainsText(activeTarget, "win32");
    }

    internal static bool ContainsText(string text, string value)
    {
        return text.IndexOf(value) >= 0;
    }

    internal static bool StartsWithText(string text, string value)
    {
        if (text.Length < value.Length)
        {
            return false;
        }

        return text.Substring(0, value.Length) == value;
    }
}

