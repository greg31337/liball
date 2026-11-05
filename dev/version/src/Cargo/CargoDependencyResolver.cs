namespace version;

internal static class CargoDependencyResolver
{
    internal static CargoDependencyResolutionResult Resolve(
        CargoManifest manifest,
        CargoPackageMetadata[] availablePackages,
        CargoResolverOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(manifest);

        CargoFeatureResolution rootFeatures = CargoFeatureResolver.Resolve(
            manifest,
            options,
            new string[0],
            true,
            false);

        return Resolve(CreateResolvedDependencies(rootFeatures), availablePackages, options);
    }

    internal static CargoDependencyResolutionResult Resolve(
        CargoDependency[] rootDependencies,
        CargoPackageMetadata[] availablePackages,
        CargoResolverOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(rootDependencies);
        System.ArgumentNullException.ThrowIfNull(availablePackages);
        System.ArgumentNullException.ThrowIfNull(options);
        ValidateDependencies(rootDependencies);
        ValidateAvailablePackages(availablePackages);

        System.Collections.Generic.List<CargoDependencyResolutionState> states = new System.Collections.Generic.List<CargoDependencyResolutionState>();
        System.Collections.Generic.List<CargoDependencyGraphEdge> edges = new System.Collections.Generic.List<CargoDependencyGraphEdge>();
        int index = 0;

        while (index < rootDependencies.Length)
        {
            edges.Add(CargoDependencyGraphEdge.CreateRootEdge(rootDependencies[index]));
            AddConstraint(rootDependencies[index], 0, true, states);
            index = index + 1;
        }

        CargoBacktrackingBudget budget = new CargoBacktrackingBudget(options.GetMaxBacktrackingSteps());
        return Search(states, edges, availablePackages, options, budget);
    }

    internal static CargoDependencyResolutionResult Search(
        System.Collections.Generic.List<CargoDependencyResolutionState> states,
        System.Collections.Generic.List<CargoDependencyGraphEdge> edges,
        CargoPackageMetadata[] availablePackages,
        CargoResolverOptions options,
        CargoBacktrackingBudget budget)
    {
        CargoDependencyResolutionState? pendingSelection = FindStateNeedingSelection(states, options);
        if (pendingSelection != null)
        {
            return SearchCandidates(pendingSelection, states, edges, availablePackages, options, budget);
        }

        CargoDependencyResolutionState? pendingExpansion = FindUnexpandedSelectedState(states);
        if (pendingExpansion != null)
        {
            return ExpandSelectedPackage(pendingExpansion, states, edges, availablePackages, options, budget);
        }

        return CreateResult(states, edges, new CargoDependencyConflict[0]);
    }

    internal static CargoDependencyResolutionResult SearchCandidates(
        CargoDependencyResolutionState pendingSelection,
        System.Collections.Generic.List<CargoDependencyResolutionState> states,
        System.Collections.Generic.List<CargoDependencyGraphEdge> edges,
        CargoPackageMetadata[] availablePackages,
        CargoResolverOptions options,
        CargoBacktrackingBudget budget)
    {
        CargoPackageMetadata[] candidates = FindMatchingCandidates(pendingSelection, availablePackages, options);
        if (candidates.Length == 0)
        {
            return CreateResult(
                states,
                edges,
                new CargoDependencyConflict[] { CreateSelectionConflict(pendingSelection, availablePackages) });
        }

        CargoDependencyResolutionResult? lastFailure = null;
        int index = 0;

        while (index < candidates.Length)
        {
            if (!budget.TryConsumeStep())
            {
                return CreateResult(
                    states,
                    edges,
                    new CargoDependencyConflict[]
                    {
                        new CargoDependencyConflict(
                            pendingSelection.GetPackageName(),
                            pendingSelection.GetDepth(),
                            CargoResolverDiagnosticCodes.BacktrackingLimitExceeded,
                            "Cargo dependency resolution exceeded the configured backtracking step limit.")
                    });
            }

            System.Collections.Generic.List<CargoDependencyResolutionState> branchStates = CopyStates(states);
            System.Collections.Generic.List<CargoDependencyGraphEdge> branchEdges = CopyEdges(edges);
            CargoDependencyResolutionState branchState = GetState(branchStates, pendingSelection.GetPackageName());
            branchState.SetSelectedPackage(candidates[index]);

            CargoDependencyResolutionResult branchResult = Search(branchStates, branchEdges, availablePackages, options, budget);
            if (branchResult.IsSuccess())
            {
                return branchResult;
            }

            lastFailure = branchResult;
            index = index + 1;
        }

        if (lastFailure != null)
        {
            return lastFailure;
        }

        return CreateResult(
            states,
            edges,
            new CargoDependencyConflict[] { CreateSelectionConflict(pendingSelection, availablePackages) });
    }

    internal static CargoDependencyResolutionResult ExpandSelectedPackage(
        CargoDependencyResolutionState pendingExpansion,
        System.Collections.Generic.List<CargoDependencyResolutionState> states,
        System.Collections.Generic.List<CargoDependencyGraphEdge> edges,
        CargoPackageMetadata[] availablePackages,
        CargoResolverOptions options,
        CargoBacktrackingBudget budget)
    {
        System.Collections.Generic.List<CargoDependencyResolutionState> branchStates = CopyStates(states);
        System.Collections.Generic.List<CargoDependencyGraphEdge> branchEdges = CopyEdges(edges);
        CargoDependencyResolutionState branchState = GetState(branchStates, pendingExpansion.GetPackageName());
        branchState.MarkExpanded();

        CargoDependencyConflict? conflict = AddSelectedPackageDependencies(branchState, branchStates, branchEdges, options);
        if (conflict != null)
        {
            return CreateResult(branchStates, branchEdges, new CargoDependencyConflict[] { conflict });
        }

        return Search(branchStates, branchEdges, availablePackages, options, budget);
    }

    internal static CargoDependencyConflict? AddSelectedPackageDependencies(
        CargoDependencyResolutionState state,
        System.Collections.Generic.List<CargoDependencyResolutionState> states,
        System.Collections.Generic.List<CargoDependencyGraphEdge> edges,
        CargoResolverOptions options)
    {
        CargoPackageMetadata packageMetadata = state.GetSelectedPackage();
        CargoManifest packageManifest = new CargoManifest(
            packageMetadata.GetDependencies(),
            packageMetadata.GetFeatures(),
            new CargoRegistrySource[0],
            new CargoSourceReplacement[0]);
        CargoFeatureResolution featureResolution = CargoFeatureResolver.Resolve(
            packageManifest,
            options,
            state.GetFeatureNames(),
            state.UsesDefaultFeatures(),
            false);
        int index = 0;

        while (index < featureResolution.GetActiveDependencyCount())
        {
            CargoDependency dependency = CreateResolvedDependency(
                featureResolution.GetActiveDependency(index),
                featureResolution);
            if (WouldCreateCycle(packageMetadata.GetIdentity().GetPackageName(), dependency.GetPackageName(), edges))
            {
                return new CargoDependencyConflict(
                    dependency.GetPackageName(),
                    state.GetDepth() + 1,
                    CargoResolverDiagnosticCodes.CyclicDependency,
                    "Cargo dependency graph contains a cyclic dependency.");
            }

            CargoDependencyGraphEdge edge = CargoDependencyGraphEdge.CreatePackageEdge(
                packageMetadata.GetIdentity(),
                dependency,
                state.GetDepth() + 1);
            AddEdgeIfMissing(edges, edge);
            AddConstraint(dependency, state.GetDepth() + 1, false, states);
            index = index + 1;
        }

        return null;
    }

    internal static CargoDependency[] CreateResolvedDependencies(CargoFeatureResolution featureResolution)
    {
        CargoDependency[] dependencies = new CargoDependency[featureResolution.GetActiveDependencyCount()];
        int index = 0;

        while (index < featureResolution.GetActiveDependencyCount())
        {
            dependencies[index] = CreateResolvedDependency(featureResolution.GetActiveDependency(index), featureResolution);
            index = index + 1;
        }

        return dependencies;
    }

    internal static CargoDependency CreateResolvedDependency(CargoDependency dependency, CargoFeatureResolution featureResolution)
    {
        CargoResolvedDependencyFeatures? dependencyFeatures = featureResolution.FindDependencyFeatures(dependency.GetPackageName().GetValue());
        if (dependencyFeatures == null)
        {
            return dependency;
        }

        string? versionRequirementText = null;
        if (dependency.HasVersionRequirement())
        {
            versionRequirementText = dependency.GetVersionRequirementText();
        }

        string? registryName = null;
        if (dependency.HasRegistryName())
        {
            registryName = dependency.GetRegistryName();
        }

        string? targetTriple = null;
        if (dependency.HasTargetTriple())
        {
            targetTriple = dependency.GetTargetTriple();
        }

        return new CargoDependency(
            dependency.GetPackageName(),
            versionRequirementText,
            dependency.GetDependencyKind(),
            dependency.IsOptional(),
            dependencyFeatures.UsesDefaultFeatures(),
            dependencyFeatures.GetFeatures(),
            registryName,
            targetTriple);
    }

    internal static void AddConstraint(
        CargoDependency dependency,
        int depth,
        bool direct,
        System.Collections.Generic.List<CargoDependencyResolutionState> states)
    {
        CargoDependencyResolutionState? state = FindState(states, dependency.GetPackageName());
        if (state == null)
        {
            state = new CargoDependencyResolutionState(
                dependency.GetPackageName(),
                depth,
                direct,
                null,
                false,
                false,
                new string[0],
                new CargoDependency[0]);
            states.Add(state);
        }

        state.AddConstraint(dependency, depth, direct);
    }

    internal static CargoPackageMetadata[] FindMatchingCandidates(
        CargoDependencyResolutionState state,
        CargoPackageMetadata[] availablePackages,
        CargoResolverOptions options)
    {
        System.Collections.Generic.List<CargoPackageMetadata> candidates = new System.Collections.Generic.List<CargoPackageMetadata>();
        int index = 0;

        while (index < availablePackages.Length)
        {
            if (StateAllowsCandidate(state, availablePackages[index], options))
            {
                candidates.Add(availablePackages[index]);
            }

            index = index + 1;
        }

        CargoPackageMetadata[] values = candidates.ToArray();
        SortCandidatesDescending(values);
        return values;
    }

    internal static bool StateAllowsCandidate(
        CargoDependencyResolutionState state,
        CargoPackageMetadata candidate,
        CargoResolverOptions options)
    {
        CargoResolvedDependencyFeatures selectedFeatures = state.CreateSelectedFeatures();
        int index = 0;

        while (index < state.GetConstraintCount())
        {
            CargoPackageMetadata[] filtered = CargoCandidateFilter.Filter(
                state.GetConstraint(index),
                new CargoPackageMetadata[] { candidate },
                options,
                selectedFeatures);
            if (filtered.Length == 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static CargoDependencyResolutionState? FindStateNeedingSelection(
        System.Collections.Generic.List<CargoDependencyResolutionState> states,
        CargoResolverOptions options)
    {
        int index = 0;

        while (index < states.Count)
        {
            if (!states[index].HasSelectedPackage())
            {
                return states[index];
            }

            if (!StateAllowsCandidate(states[index], states[index].GetSelectedPackage(), options))
            {
                return states[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static CargoDependencyResolutionState? FindUnexpandedSelectedState(
        System.Collections.Generic.List<CargoDependencyResolutionState> states)
    {
        int index = 0;

        while (index < states.Count)
        {
            if (states[index].HasSelectedPackage() && !states[index].IsExpanded())
            {
                return states[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static CargoDependencyConflict CreateSelectionConflict(
        CargoDependencyResolutionState state,
        CargoPackageMetadata[] availablePackages)
    {
        if (HasPackageMetadata(state.GetPackageName(), availablePackages))
        {
            return new CargoDependencyConflict(
                state.GetPackageName(),
                state.GetDepth(),
                CargoResolverDiagnosticCodes.UnsatisfiedConstraints,
                "No available Cargo package satisfies all dependency constraints.");
        }

        return new CargoDependencyConflict(
            state.GetPackageName(),
            state.GetDepth(),
            CargoResolverDiagnosticCodes.MissingPackageMetadata,
            "No Cargo package metadata is available for the requested package.");
    }

    internal static bool HasPackageMetadata(CargoPackageName packageName, CargoPackageMetadata[] availablePackages)
    {
        int index = 0;

        while (index < availablePackages.Length)
        {
            if (availablePackages[index].GetIdentity().GetPackageName().EqualsName(packageName))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool WouldCreateCycle(
        CargoPackageName parentPackageName,
        CargoPackageName dependencyPackageName,
        System.Collections.Generic.List<CargoDependencyGraphEdge> edges)
    {
        if (parentPackageName.EqualsName(dependencyPackageName))
        {
            return true;
        }

        return HasDependencyPath(dependencyPackageName, parentPackageName, edges, 0);
    }

    internal static bool HasDependencyPath(
        CargoPackageName currentPackageName,
        CargoPackageName targetPackageName,
        System.Collections.Generic.List<CargoDependencyGraphEdge> edges,
        int depth)
    {
        if (depth > edges.Count)
        {
            return false;
        }

        int index = 0;
        while (index < edges.Count)
        {
            if (edges[index].HasParent()
                && edges[index].GetParentIdentity().GetPackageName().EqualsName(currentPackageName))
            {
                CargoPackageName childPackageName = edges[index].GetDependency().GetPackageName();
                if (childPackageName.EqualsName(targetPackageName))
                {
                    return true;
                }

                if (HasDependencyPath(childPackageName, targetPackageName, edges, depth + 1))
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal static void AddEdgeIfMissing(
        System.Collections.Generic.List<CargoDependencyGraphEdge> edges,
        CargoDependencyGraphEdge edge)
    {
        int index = 0;

        while (index < edges.Count)
        {
            if (EdgesMatch(edges[index], edge))
            {
                return;
            }

            index = index + 1;
        }

        edges.Add(edge);
    }

    internal static bool EdgesMatch(CargoDependencyGraphEdge left, CargoDependencyGraphEdge right)
    {
        if (left.HasParent() != right.HasParent())
        {
            return false;
        }

        if (left.HasParent() && !left.GetParentIdentity().HasSameIdentity(right.GetParentIdentity()))
        {
            return false;
        }

        return left.GetDependency().GetPackageName().EqualsName(right.GetDependency().GetPackageName())
            && left.GetDepth() == right.GetDepth();
    }

    internal static CargoDependencyResolutionState? FindState(
        System.Collections.Generic.List<CargoDependencyResolutionState> states,
        CargoPackageName packageName)
    {
        int index = 0;

        while (index < states.Count)
        {
            if (states[index].GetPackageName().EqualsName(packageName))
            {
                return states[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static CargoDependencyResolutionState GetState(
        System.Collections.Generic.List<CargoDependencyResolutionState> states,
        CargoPackageName packageName)
    {
        CargoDependencyResolutionState? state = FindState(states, packageName);
        if (state == null)
        {
            throw new System.InvalidOperationException("Cargo dependency resolution state was not found.");
        }

        return state;
    }

    internal static CargoDependencyResolutionResult CreateResult(
        System.Collections.Generic.List<CargoDependencyResolutionState> states,
        System.Collections.Generic.List<CargoDependencyGraphEdge> edges,
        CargoDependencyConflict[] conflicts)
    {
        return new CargoDependencyResolutionResult(CreateNodes(states), edges.ToArray(), conflicts);
    }

    internal static CargoDependencyGraphNode[] CreateNodes(System.Collections.Generic.List<CargoDependencyResolutionState> states)
    {
        System.Collections.Generic.List<CargoDependencyGraphNode> nodes = new System.Collections.Generic.List<CargoDependencyGraphNode>();
        int index = 0;

        while (index < states.Count)
        {
            if (states[index].HasSelectedPackage())
            {
                nodes.Add(new CargoDependencyGraphNode(
                    states[index].GetSelectedPackage(),
                    states[index].GetDepth(),
                    states[index].IsDirect(),
                    states[index].CreateSelectedFeatures()));
            }

            index = index + 1;
        }

        return nodes.ToArray();
    }

    internal static System.Collections.Generic.List<CargoDependencyResolutionState> CopyStates(
        System.Collections.Generic.List<CargoDependencyResolutionState> states)
    {
        System.Collections.Generic.List<CargoDependencyResolutionState> copy = new System.Collections.Generic.List<CargoDependencyResolutionState>();
        int index = 0;

        while (index < states.Count)
        {
            copy.Add(states[index].Copy());
            index = index + 1;
        }

        return copy;
    }

    internal static System.Collections.Generic.List<CargoDependencyGraphEdge> CopyEdges(
        System.Collections.Generic.List<CargoDependencyGraphEdge> edges)
    {
        System.Collections.Generic.List<CargoDependencyGraphEdge> copy = new System.Collections.Generic.List<CargoDependencyGraphEdge>();
        int index = 0;

        while (index < edges.Count)
        {
            copy.Add(edges[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static void SortCandidatesDescending(CargoPackageMetadata[] candidates)
    {
        int index = 1;

        while (index < candidates.Length)
        {
            CargoPackageMetadata candidate = candidates[index];
            int sortedIndex = index - 1;

            while (sortedIndex >= 0 && CargoVersions.Compare(candidate.GetIdentity().GetVersionText(), candidates[sortedIndex].GetIdentity().GetVersionText()) > 0)
            {
                candidates[sortedIndex + 1] = candidates[sortedIndex];
                sortedIndex = sortedIndex - 1;
            }

            candidates[sortedIndex + 1] = candidate;
            index = index + 1;
        }
    }

    internal static void ValidateDependencies(CargoDependency[] dependencies)
    {
        int index = 0;

        while (index < dependencies.Length)
        {
            if (dependencies[index] == null)
            {
                throw new System.ArgumentException("Cargo root dependency arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateAvailablePackages(CargoPackageMetadata[] availablePackages)
    {
        int index = 0;

        while (index < availablePackages.Length)
        {
            if (availablePackages[index] == null)
            {
                throw new System.ArgumentException("Cargo package metadata arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }
}

internal class CargoDependencyResolutionState
{
    internal CargoPackageName packageName;
    internal int depth;
    internal bool direct;
    internal CargoPackageMetadata? selectedPackage;
    internal bool expanded;
    internal bool defaultFeatures;
    internal System.Collections.Generic.List<string> features;
    internal System.Collections.Generic.List<CargoDependency> constraints;

    internal CargoDependencyResolutionState(
        CargoPackageName packageNameValue,
        int depthValue,
        bool directValue,
        CargoPackageMetadata? selectedPackageValue,
        bool expandedValue,
        bool defaultFeaturesValue,
        string[] featureValues,
        CargoDependency[] constraintValues)
    {
        System.ArgumentNullException.ThrowIfNull(packageNameValue);
        System.ArgumentNullException.ThrowIfNull(featureValues);
        System.ArgumentNullException.ThrowIfNull(constraintValues);

        packageName = packageNameValue;
        depth = depthValue;
        direct = directValue;
        selectedPackage = selectedPackageValue;
        expanded = expandedValue;
        defaultFeatures = defaultFeaturesValue;
        features = CopyFeatureList(featureValues);
        constraints = CopyConstraintList(constraintValues);
    }

    internal CargoPackageName GetPackageName()
    {
        return packageName;
    }

    internal int GetDepth()
    {
        return depth;
    }

    internal bool IsDirect()
    {
        return direct;
    }

    internal bool HasSelectedPackage()
    {
        return selectedPackage != null;
    }

    internal CargoPackageMetadata GetSelectedPackage()
    {
        if (selectedPackage == null)
        {
            throw new System.InvalidOperationException("Cargo dependency resolution state has no selected package.");
        }

        return selectedPackage;
    }

    internal void SetSelectedPackage(CargoPackageMetadata packageMetadata)
    {
        System.ArgumentNullException.ThrowIfNull(packageMetadata);

        selectedPackage = packageMetadata;
        expanded = false;
    }

    internal bool IsExpanded()
    {
        return expanded;
    }

    internal void MarkExpanded()
    {
        expanded = true;
    }

    internal bool UsesDefaultFeatures()
    {
        return defaultFeatures;
    }

    internal string[] GetFeatureNames()
    {
        return features.ToArray();
    }

    internal int GetConstraintCount()
    {
        return constraints.Count;
    }

    internal CargoDependency GetConstraint(int constraintIndex)
    {
        if (constraintIndex < 0 || constraintIndex >= constraints.Count)
        {
            throw new System.ArgumentOutOfRangeException("constraintIndex");
        }

        return constraints[constraintIndex];
    }

    internal void AddConstraint(CargoDependency dependency, int depthValue, bool directValue)
    {
        System.ArgumentNullException.ThrowIfNull(dependency);

        if (depthValue < depth)
        {
            depth = depthValue;
        }

        if (directValue)
        {
            direct = true;
        }

        bool changed = AddConstraintIfMissing(dependency);
        if (dependency.UsesDefaultFeatures() && !defaultFeatures)
        {
            defaultFeatures = true;
            changed = true;
        }

        int index = 0;
        while (index < dependency.GetFeatureCount())
        {
            if (AddFeatureIfMissing(dependency.GetFeature(index)))
            {
                changed = true;
            }

            index = index + 1;
        }

        if (changed && selectedPackage != null)
        {
            expanded = false;
        }
    }

    internal CargoResolvedDependencyFeatures CreateSelectedFeatures()
    {
        return new CargoResolvedDependencyFeatures(
            packageName,
            CargoDependencyKind.Normal,
            defaultFeatures,
            features.ToArray());
    }

    internal CargoDependencyResolutionState Copy()
    {
        return new CargoDependencyResolutionState(
            packageName,
            depth,
            direct,
            selectedPackage,
            expanded,
            defaultFeatures,
            features.ToArray(),
            constraints.ToArray());
    }

    internal bool AddConstraintIfMissing(CargoDependency dependency)
    {
        int index = 0;

        while (index < constraints.Count)
        {
            if (DependenciesMatch(constraints[index], dependency))
            {
                return false;
            }

            index = index + 1;
        }

        constraints.Add(dependency);
        return true;
    }

    internal bool AddFeatureIfMissing(string featureName)
    {
        int index = 0;

        while (index < features.Count)
        {
            if (features[index] == featureName)
            {
                return false;
            }

            index = index + 1;
        }

        features.Add(featureName);
        return true;
    }

    internal bool DependenciesMatch(CargoDependency left, CargoDependency right)
    {
        if (!left.GetPackageName().EqualsName(right.GetPackageName()))
        {
            return false;
        }

        if (left.HasVersionRequirement() != right.HasVersionRequirement())
        {
            return false;
        }

        if (left.HasVersionRequirement() && left.GetVersionRequirementText() != right.GetVersionRequirementText())
        {
            return false;
        }

        if (left.HasRegistryName() != right.HasRegistryName())
        {
            return false;
        }

        if (left.HasRegistryName() && left.GetRegistryName() != right.GetRegistryName())
        {
            return false;
        }

        return true;
    }

    internal static System.Collections.Generic.List<string> CopyFeatureList(string[] values)
    {
        System.Collections.Generic.List<string> copy = new System.Collections.Generic.List<string>();
        int index = 0;

        while (index < values.Length)
        {
            copy.Add(values[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static System.Collections.Generic.List<CargoDependency> CopyConstraintList(CargoDependency[] values)
    {
        System.Collections.Generic.List<CargoDependency> copy = new System.Collections.Generic.List<CargoDependency>();
        int index = 0;

        while (index < values.Length)
        {
            copy.Add(values[index]);
            index = index + 1;
        }

        return copy;
    }
}

internal class CargoBacktrackingBudget
{
    internal int maxSteps;
    internal int usedSteps;

    internal CargoBacktrackingBudget(int maxStepsValue)
    {
        if (maxStepsValue <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maxStepsValue");
        }

        maxSteps = maxStepsValue;
        usedSteps = 0;
    }

    internal bool TryConsumeStep()
    {
        if (usedSteps >= maxSteps)
        {
            return false;
        }

        usedSteps = usedSteps + 1;
        return true;
    }
}
