namespace version;

internal static class RubyGemsDependencyResolver
{
    internal static RubyGemsDependencyResolutionResult Resolve(
        RubyGemsDependency[] rootDependencies,
        RubyGemsSpecification[] availableSpecifications)
    {
        return Resolve(rootDependencies, availableSpecifications, RubyGemsResolverOptions.CreateDefault());
    }

    internal static RubyGemsDependencyResolutionResult Resolve(
        RubyGemsDependency[] rootDependencies,
        RubyGemsSpecification[] availableSpecifications,
        RubyGemsResolverOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(rootDependencies);
        System.ArgumentNullException.ThrowIfNull(availableSpecifications);
        System.ArgumentNullException.ThrowIfNull(options);
        ValidateDependencies(rootDependencies);
        ValidateSpecifications(availableSpecifications);

        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states = new System.Collections.Generic.List<RubyGemsDependencyResolutionState>();
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> edges = new System.Collections.Generic.List<RubyGemsDependencyGraphEdge>();
        int index = 0;

        while (index < rootDependencies.Length)
        {
            edges.Add(RubyGemsDependencyGraphEdge.CreateRootEdge(rootDependencies[index]));
            AddConstraint(rootDependencies[index], 0, true, states);
            index = index + 1;
        }

        RubyGemsBacktrackingBudget budget = new RubyGemsBacktrackingBudget(options.GetMaxBacktrackingSteps());
        return Search(states, edges, availableSpecifications, options, budget);
    }

    internal static RubyGemsDependencyResolutionResult Search(
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states,
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> edges,
        RubyGemsSpecification[] availableSpecifications,
        RubyGemsResolverOptions options,
        RubyGemsBacktrackingBudget budget)
    {
        RubyGemsDependencyResolutionState? pendingSelection = FindStateNeedingSelection(states, options);
        if (pendingSelection != null)
        {
            return SearchCandidates(pendingSelection, states, edges, availableSpecifications, options, budget);
        }

        RubyGemsDependencyResolutionState? pendingExpansion = FindUnexpandedSelectedState(states);
        if (pendingExpansion != null)
        {
            return ExpandSelectedSpecification(pendingExpansion, states, edges, availableSpecifications, options, budget);
        }

        return CreateResult(states, edges, new RubyGemsDependencyConflict[0]);
    }

    internal static RubyGemsDependencyResolutionResult SearchCandidates(
        RubyGemsDependencyResolutionState pendingSelection,
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states,
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> edges,
        RubyGemsSpecification[] availableSpecifications,
        RubyGemsResolverOptions options,
        RubyGemsBacktrackingBudget budget)
    {
        RubyGemsSpecification[] candidates = FindMatchingCandidates(pendingSelection, availableSpecifications, options);
        if (candidates.Length == 0)
        {
            return CreateResult(
                states,
                edges,
                new RubyGemsDependencyConflict[] { CreateSelectionConflict(pendingSelection, availableSpecifications) });
        }

        RubyGemsDependencyResolutionResult? lastFailure = null;
        int index = 0;

        while (index < candidates.Length)
        {
            if (!budget.TryConsumeStep())
            {
                return CreateResult(
                    states,
                    edges,
                    new RubyGemsDependencyConflict[]
                    {
                        new RubyGemsDependencyConflict(
                            pendingSelection.GetName(),
                            pendingSelection.GetDepth(),
                            RubyGemsResolverDiagnosticCodes.BacktrackingLimitExceeded,
                            "RubyGems dependency resolution exceeded the configured backtracking step limit.")
                    });
            }

            System.Collections.Generic.List<RubyGemsDependencyResolutionState> branchStates = CopyStates(states);
            System.Collections.Generic.List<RubyGemsDependencyGraphEdge> branchEdges = CopyEdges(edges);
            RubyGemsDependencyResolutionState branchState = GetState(branchStates, pendingSelection.GetName());
            branchState.SetSelectedSpecification(candidates[index]);

            RubyGemsDependencyResolutionResult branchResult = Search(
                branchStates,
                branchEdges,
                availableSpecifications,
                options,
                budget);
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
            new RubyGemsDependencyConflict[] { CreateSelectionConflict(pendingSelection, availableSpecifications) });
    }

    internal static RubyGemsDependencyResolutionResult ExpandSelectedSpecification(
        RubyGemsDependencyResolutionState pendingExpansion,
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states,
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> edges,
        RubyGemsSpecification[] availableSpecifications,
        RubyGemsResolverOptions options,
        RubyGemsBacktrackingBudget budget)
    {
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> branchStates = CopyStates(states);
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> branchEdges = CopyEdges(edges);
        RubyGemsDependencyResolutionState branchState = GetState(branchStates, pendingExpansion.GetName());
        branchState.MarkExpanded();

        RubyGemsDependencyConflict? conflict = AddSelectedSpecificationDependencies(branchState, branchStates, branchEdges, options);
        if (conflict != null)
        {
            return CreateResult(branchStates, branchEdges, new RubyGemsDependencyConflict[] { conflict });
        }

        return Search(branchStates, branchEdges, availableSpecifications, options, budget);
    }

    internal static RubyGemsDependencyConflict? AddSelectedSpecificationDependencies(
        RubyGemsDependencyResolutionState state,
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states,
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> edges,
        RubyGemsResolverOptions options)
    {
        RubyGemsSpecification specification = state.GetSelectedSpecification();
        int index = 0;

        while (index < specification.GetDependencyCount())
        {
            RubyGemsDependency dependency = specification.GetDependency(index);
            if (dependency.IsRuntime() || options.IncludesDevelopmentDependencies())
            {
                if (WouldCreateCycle(specification.GetName(), dependency.GetName(), edges))
                {
                    return new RubyGemsDependencyConflict(
                        dependency.GetName(),
                        state.GetDepth() + 1,
                        RubyGemsResolverDiagnosticCodes.CyclicDependency,
                        "RubyGems dependency graph contains a cyclic dependency.");
                }

                RubyGemsDependencyGraphEdge edge = RubyGemsDependencyGraphEdge.CreateSpecificationEdge(
                    specification,
                    dependency,
                    state.GetDepth() + 1);
                AddEdgeIfMissing(edges, edge);
                AddConstraint(dependency, state.GetDepth() + 1, false, states);
            }

            index = index + 1;
        }

        return null;
    }

    internal static void AddConstraint(
        RubyGemsDependency dependency,
        int depth,
        bool direct,
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states)
    {
        RubyGemsDependencyResolutionState? state = FindState(states, dependency.GetName());
        if (state == null)
        {
            state = new RubyGemsDependencyResolutionState(
                dependency.GetName(),
                depth,
                direct,
                null,
                false,
                new RubyGemsDependency[0]);
            states.Add(state);
        }

        state.AddConstraint(dependency, depth, direct);
    }

    internal static RubyGemsSpecification[] FindMatchingCandidates(
        RubyGemsDependencyResolutionState state,
        RubyGemsSpecification[] availableSpecifications,
        RubyGemsResolverOptions options)
    {
        System.Collections.Generic.List<RubyGemsSpecification> candidates = new System.Collections.Generic.List<RubyGemsSpecification>();
        bool stableCandidateExists = false;
        int index = 0;

        while (index < availableSpecifications.Length)
        {
            if (StateAllowsCandidateIgnoringPrereleasePolicy(state, availableSpecifications[index], options))
            {
                if (!availableSpecifications[index].IsPrerelease())
                {
                    stableCandidateExists = true;
                }
            }

            index = index + 1;
        }

        index = 0;
        while (index < availableSpecifications.Length)
        {
            if (StateAllowsCandidate(state, availableSpecifications[index], options, stableCandidateExists))
            {
                candidates.Add(availableSpecifications[index]);
            }

            index = index + 1;
        }

        RubyGemsSpecification[] values = candidates.ToArray();
        SortCandidatesDescending(values, options);
        return values;
    }

    internal static bool StateAllowsCandidate(
        RubyGemsDependencyResolutionState state,
        RubyGemsSpecification candidate,
        RubyGemsResolverOptions options,
        bool stableCandidateExists)
    {
        if (!StateAllowsCandidateIgnoringPrereleasePolicy(state, candidate, options))
        {
            return false;
        }

        return PrereleasePolicyAllowsCandidate(state, candidate, options, stableCandidateExists);
    }

    internal static bool StateAllowsCandidateIgnoringPrereleasePolicy(
        RubyGemsDependencyResolutionState state,
        RubyGemsSpecification candidate,
        RubyGemsResolverOptions options)
    {
        if (!candidate.MatchesName(state.GetName()))
        {
            return false;
        }

        if (!RubyGemsPlatformSelector.SpecificationMatchesPlatform(candidate, options.GetPlatforms()))
        {
            return false;
        }

        if (!SourceAllowed(candidate, options))
        {
            return false;
        }

        if (IsExcluded(candidate, options))
        {
            return false;
        }

        int index = 0;
        while (index < state.GetConstraintCount())
        {
            if (!DependencyAllowsCandidate(state.GetConstraint(index), candidate, true))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool DependencyAllowsCandidate(
        RubyGemsDependency dependency,
        RubyGemsSpecification candidate,
        bool allowDefaultPrerelease)
    {
        if (!candidate.MatchesName(dependency.GetName()))
        {
            return false;
        }

        if (dependency.GetRequirements().AllowsVersion(candidate.GetVersion()))
        {
            return true;
        }

        return allowDefaultPrerelease
            && candidate.IsPrerelease()
            && dependency.GetRequirements().IsDefault();
    }

    internal static bool PrereleasePolicyAllowsCandidate(
        RubyGemsDependencyResolutionState state,
        RubyGemsSpecification candidate,
        RubyGemsResolverOptions options,
        bool stableCandidateExists)
    {
        if (!candidate.IsPrerelease())
        {
            return true;
        }

        if (options.GetPrereleasePolicy() == PrereleasePolicy.Exclude)
        {
            return false;
        }

        if (options.GetPrereleasePolicy() == PrereleasePolicy.Include)
        {
            return true;
        }

        if (options.GetPrereleasePolicy() == PrereleasePolicy.AutoIfNoStableCandidate && !stableCandidateExists)
        {
            return true;
        }

        return state.HasPrereleaseRequirement();
    }

    internal static bool SourceAllowed(RubyGemsSpecification candidate, RubyGemsResolverOptions options)
    {
        if (options.GetSourceCount() == 0)
        {
            return true;
        }

        return GetSourceIndex(candidate, options) < options.GetSourceCount();
    }

    internal static int GetSourceIndex(RubyGemsSpecification candidate, RubyGemsResolverOptions options)
    {
        int index = 0;

        while (index < options.GetSourceCount())
        {
            RubyGemsSource source = options.GetSource(index);
            if (candidate.GetSource().GetName() == source.GetName()
                && candidate.GetSource().GetUri() == source.GetUri())
            {
                return index;
            }

            index = index + 1;
        }

        return options.GetSourceCount();
    }

    internal static bool IsExcluded(RubyGemsSpecification candidate, RubyGemsResolverOptions options)
    {
        int index = 0;

        while (index < options.GetExclusionCount())
        {
            if (options.GetExclusion(index).ExcludesSpecification(candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static RubyGemsDependencyResolutionState? FindStateNeedingSelection(
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states,
        RubyGemsResolverOptions options)
    {
        int index = 0;

        while (index < states.Count)
        {
            if (!states[index].HasSelectedSpecification())
            {
                return states[index];
            }

            if (!StateAllowsCandidate(states[index], states[index].GetSelectedSpecification(), options, HasStableAlternative(states[index], options)))
            {
                return states[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool HasStableAlternative(RubyGemsDependencyResolutionState state, RubyGemsResolverOptions options)
    {
        if (!state.HasSelectedSpecification())
        {
            return false;
        }

        return !state.GetSelectedSpecification().IsPrerelease();
    }

    internal static RubyGemsDependencyResolutionState? FindUnexpandedSelectedState(
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states)
    {
        int index = 0;

        while (index < states.Count)
        {
            if (states[index].HasSelectedSpecification() && !states[index].IsExpanded())
            {
                return states[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static RubyGemsDependencyConflict CreateSelectionConflict(
        RubyGemsDependencyResolutionState state,
        RubyGemsSpecification[] availableSpecifications)
    {
        if (HasSpecificationMetadata(state.GetName(), availableSpecifications))
        {
            return new RubyGemsDependencyConflict(
                state.GetName(),
                state.GetDepth(),
                RubyGemsResolverDiagnosticCodes.UnsatisfiedConstraints,
                "No available RubyGems specification satisfies all dependency constraints.");
        }

        return new RubyGemsDependencyConflict(
            state.GetName(),
            state.GetDepth(),
            RubyGemsResolverDiagnosticCodes.MissingSpecificationMetadata,
            "No RubyGems specification metadata is available for the requested gem.");
    }

    internal static bool HasSpecificationMetadata(string name, RubyGemsSpecification[] availableSpecifications)
    {
        int index = 0;

        while (index < availableSpecifications.Length)
        {
            if (availableSpecifications[index].MatchesName(name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool WouldCreateCycle(
        string parentName,
        string dependencyName,
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> edges)
    {
        if (parentName == dependencyName)
        {
            return true;
        }

        return HasDependencyPath(dependencyName, parentName, edges, 0);
    }

    internal static bool HasDependencyPath(
        string currentName,
        string targetName,
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> edges,
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
                && edges[index].GetParentSpecification().MatchesName(currentName))
            {
                string childName = edges[index].GetDependency().GetName();
                if (childName == targetName)
                {
                    return true;
                }

                if (HasDependencyPath(childName, targetName, edges, depth + 1))
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal static void AddEdgeIfMissing(
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> edges,
        RubyGemsDependencyGraphEdge edge)
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

    internal static bool EdgesMatch(RubyGemsDependencyGraphEdge left, RubyGemsDependencyGraphEdge right)
    {
        if (left.HasParent() != right.HasParent())
        {
            return false;
        }

        if (left.HasParent() && !left.GetParentSpecification().HasSameIdentity(right.GetParentSpecification()))
        {
            return false;
        }

        return left.GetDependency().GetName() == right.GetDependency().GetName()
            && left.GetDependency().GetRequirements().ToNormalizedRequirementText() == right.GetDependency().GetRequirements().ToNormalizedRequirementText()
            && left.GetDepth() == right.GetDepth();
    }

    internal static RubyGemsDependencyResolutionState? FindState(
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states,
        string name)
    {
        int index = 0;

        while (index < states.Count)
        {
            if (states[index].GetName() == name)
            {
                return states[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static RubyGemsDependencyResolutionState GetState(
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states,
        string name)
    {
        RubyGemsDependencyResolutionState? state = FindState(states, name);
        if (state == null)
        {
            throw new System.InvalidOperationException("RubyGems dependency resolution state was not found.");
        }

        return state;
    }

    internal static RubyGemsDependencyResolutionResult CreateResult(
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states,
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> edges,
        RubyGemsDependencyConflict[] conflicts)
    {
        return new RubyGemsDependencyResolutionResult(CreateNodes(states), edges.ToArray(), conflicts);
    }

    internal static RubyGemsDependencyGraphNode[] CreateNodes(System.Collections.Generic.List<RubyGemsDependencyResolutionState> states)
    {
        System.Collections.Generic.List<RubyGemsDependencyGraphNode> nodes = new System.Collections.Generic.List<RubyGemsDependencyGraphNode>();
        int index = 0;

        while (index < states.Count)
        {
            if (states[index].HasSelectedSpecification())
            {
                nodes.Add(new RubyGemsDependencyGraphNode(
                    states[index].GetSelectedSpecification(),
                    states[index].GetDepth(),
                    states[index].IsDirect()));
            }

            index = index + 1;
        }

        return nodes.ToArray();
    }

    internal static System.Collections.Generic.List<RubyGemsDependencyResolutionState> CopyStates(
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> states)
    {
        System.Collections.Generic.List<RubyGemsDependencyResolutionState> copy = new System.Collections.Generic.List<RubyGemsDependencyResolutionState>();
        int index = 0;

        while (index < states.Count)
        {
            copy.Add(states[index].Copy());
            index = index + 1;
        }

        return copy;
    }

    internal static System.Collections.Generic.List<RubyGemsDependencyGraphEdge> CopyEdges(
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> edges)
    {
        System.Collections.Generic.List<RubyGemsDependencyGraphEdge> copy = new System.Collections.Generic.List<RubyGemsDependencyGraphEdge>();
        int index = 0;

        while (index < edges.Count)
        {
            copy.Add(edges[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static void SortCandidatesDescending(RubyGemsSpecification[] candidates, RubyGemsResolverOptions options)
    {
        int index = 1;

        while (index < candidates.Length)
        {
            RubyGemsSpecification candidate = candidates[index];
            int sortedIndex = index - 1;

            while (sortedIndex >= 0 && CompareCandidates(candidate, candidates[sortedIndex], options) < 0)
            {
                candidates[sortedIndex + 1] = candidates[sortedIndex];
                sortedIndex = sortedIndex - 1;
            }

            candidates[sortedIndex + 1] = candidate;
            index = index + 1;
        }
    }

    internal static int CompareCandidates(RubyGemsSpecification left, RubyGemsSpecification right, RubyGemsResolverOptions options)
    {
        int versionComparison = RubyGemsVersions.Compare(left.GetVersionText(), right.GetVersionText());
        if (versionComparison > 0)
        {
            return -1;
        }

        if (versionComparison < 0)
        {
            return 1;
        }

        int leftPlatformScore = RubyGemsPlatformSelector.GetSpecificationPlatformScore(left, options.GetPlatforms());
        int rightPlatformScore = RubyGemsPlatformSelector.GetSpecificationPlatformScore(right, options.GetPlatforms());
        if (leftPlatformScore > rightPlatformScore)
        {
            return -1;
        }

        if (leftPlatformScore < rightPlatformScore)
        {
            return 1;
        }

        int leftSourceIndex = GetSourceIndex(left, options);
        int rightSourceIndex = GetSourceIndex(right, options);
        if (leftSourceIndex < rightSourceIndex)
        {
            return -1;
        }

        if (leftSourceIndex > rightSourceIndex)
        {
            return 1;
        }

        return 0;
    }

    internal static void ValidateDependencies(RubyGemsDependency[] dependencies)
    {
        int index = 0;

        while (index < dependencies.Length)
        {
            if (dependencies[index] == null)
            {
                throw new System.ArgumentException("RubyGems root dependency arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateSpecifications(RubyGemsSpecification[] specifications)
    {
        int index = 0;

        while (index < specifications.Length)
        {
            if (specifications[index] == null)
            {
                throw new System.ArgumentException("RubyGems specification arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }
}

internal class RubyGemsDependencyResolutionState
{
    internal string name;
    internal int depth;
    internal bool direct;
    internal RubyGemsSpecification? selectedSpecification;
    internal bool expanded;
    internal System.Collections.Generic.List<RubyGemsDependency> constraints;

    internal RubyGemsDependencyResolutionState(
        string nameValue,
        int depthValue,
        bool directValue,
        RubyGemsSpecification? selectedSpecificationValue,
        bool expandedValue,
        RubyGemsDependency[] constraintValues)
    {
        RubyGemsDependency.ValidateName(nameValue, "RubyGems dependency resolution state name cannot be empty or contain invalid characters.");
        System.ArgumentNullException.ThrowIfNull(constraintValues);

        name = nameValue;
        depth = depthValue;
        direct = directValue;
        selectedSpecification = selectedSpecificationValue;
        expanded = expandedValue;
        constraints = CopyConstraintList(constraintValues);
    }

    internal string GetName()
    {
        return name;
    }

    internal int GetDepth()
    {
        return depth;
    }

    internal bool IsDirect()
    {
        return direct;
    }

    internal bool HasSelectedSpecification()
    {
        return selectedSpecification != null;
    }

    internal RubyGemsSpecification GetSelectedSpecification()
    {
        if (selectedSpecification == null)
        {
            throw new System.InvalidOperationException("RubyGems dependency resolution state has no selected specification.");
        }

        return selectedSpecification;
    }

    internal void SetSelectedSpecification(RubyGemsSpecification specification)
    {
        System.ArgumentNullException.ThrowIfNull(specification);

        selectedSpecification = specification;
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

    internal int GetConstraintCount()
    {
        return constraints.Count;
    }

    internal RubyGemsDependency GetConstraint(int constraintIndex)
    {
        if (constraintIndex < 0 || constraintIndex >= constraints.Count)
        {
            throw new System.ArgumentOutOfRangeException("constraintIndex");
        }

        return constraints[constraintIndex];
    }

    internal bool HasPrereleaseRequirement()
    {
        int index = 0;

        while (index < constraints.Count)
        {
            if (constraints[index].GetRequirements().HasPrereleaseRequirement()
                || constraints[index].GetRequirements().IsDefaultPrerelease())
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddConstraint(RubyGemsDependency dependency, int depthValue, bool directValue)
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

        if (AddConstraintIfMissing(dependency) && selectedSpecification != null)
        {
            expanded = false;
        }
    }

    internal RubyGemsDependencyResolutionState Copy()
    {
        return new RubyGemsDependencyResolutionState(
            name,
            depth,
            direct,
            selectedSpecification,
            expanded,
            constraints.ToArray());
    }

    internal bool AddConstraintIfMissing(RubyGemsDependency dependency)
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

    internal bool DependenciesMatch(RubyGemsDependency left, RubyGemsDependency right)
    {
        return left.GetName() == right.GetName()
            && left.GetDependencyType() == right.GetDependencyType()
            && left.GetRequirements().ToNormalizedRequirementText() == right.GetRequirements().ToNormalizedRequirementText();
    }

    internal static System.Collections.Generic.List<RubyGemsDependency> CopyConstraintList(RubyGemsDependency[] values)
    {
        System.Collections.Generic.List<RubyGemsDependency> copy = new System.Collections.Generic.List<RubyGemsDependency>();
        int index = 0;

        while (index < values.Length)
        {
            copy.Add(values[index]);
            index = index + 1;
        }

        return copy;
    }
}

internal class RubyGemsBacktrackingBudget
{
    internal int maxSteps;
    internal int usedSteps;

    internal RubyGemsBacktrackingBudget(int maxStepsValue)
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
