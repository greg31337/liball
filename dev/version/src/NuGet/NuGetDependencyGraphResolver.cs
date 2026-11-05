namespace version;

internal static class NuGetDependencyGraphResolver
{
    internal static NuGetDependencyResolutionResult Resolve(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages)
    {
        return Resolve(rootDependencies, availablePackages, NuGetPackageRejectionPolicy.CreateDefault());
    }

    internal static NuGetDependencyResolutionResult Resolve(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        return ResolveInternal(rootDependencies, availablePackages, null, rejectionPolicy);
    }

    internal static NuGetDependencyResolutionResult Resolve(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        string projectFrameworkText)
    {
        return Resolve(rootDependencies, availablePackages, projectFrameworkText, NuGetPackageRejectionPolicy.CreateDefault());
    }

    internal static NuGetDependencyResolutionResult Resolve(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        string projectFrameworkText,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        NuGetTargetFrameworkParseResult projectResult = NuGetTargetFrameworkParser.Parse(projectFrameworkText);
        if (projectResult.IsFailure())
        {
            throw new System.ArgumentException("Invalid NuGet project target framework.");
        }

        return Resolve(rootDependencies, availablePackages, projectResult.GetFramework(), rejectionPolicy);
    }

    internal static NuGetDependencyResolutionResult Resolve(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetTargetFramework projectFramework)
    {
        return Resolve(rootDependencies, availablePackages, projectFramework, NuGetPackageRejectionPolicy.CreateDefault());
    }

    internal static NuGetDependencyResolutionResult Resolve(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetTargetFramework projectFramework,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        System.ArgumentNullException.ThrowIfNull(projectFramework);

        return ResolveInternal(rootDependencies, availablePackages, projectFramework, rejectionPolicy);
    }

    internal static NuGetDependencyResolutionResult ResolveInternal(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetTargetFramework? projectFramework,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        System.ArgumentNullException.ThrowIfNull(rootDependencies);
        System.ArgumentNullException.ThrowIfNull(availablePackages);
        System.ArgumentNullException.ThrowIfNull(rejectionPolicy);
        ValidateDependencies(rootDependencies);
        ValidateAvailablePackages(availablePackages);

        System.Collections.Generic.List<NuGetDependencyResolutionState> states = new System.Collections.Generic.List<NuGetDependencyResolutionState>();
        System.Collections.Generic.List<NuGetDependencyGraphEdge> edges = new System.Collections.Generic.List<NuGetDependencyGraphEdge>();
        System.Collections.Generic.List<NuGetDependencyConflict> conflicts = new System.Collections.Generic.List<NuGetDependencyConflict>();
        System.Collections.Generic.List<NuGetSourcePackageMetadata> queue = new System.Collections.Generic.List<NuGetSourcePackageMetadata>();
        System.Collections.Generic.List<int> queueDepths = new System.Collections.Generic.List<int>();
        int index = 0;

        while (index < rootDependencies.Length)
        {
            NuGetDependencyGraphEdge edge = NuGetDependencyGraphEdge.CreateRootEdge(rootDependencies[index]);
            edges.Add(edge);
            AddConstraint(edge.GetConstraint(), availablePackages, rejectionPolicy, states, conflicts, queue, queueDepths);
            index = index + 1;
        }

        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            AddPackageDependencies(
                queue[queueIndex],
                queueDepths[queueIndex],
                projectFramework,
                availablePackages,
                rejectionPolicy,
                states,
                edges,
                conflicts,
                queue,
                queueDepths);
            queueIndex = queueIndex + 1;
        }

        return new NuGetDependencyResolutionResult(CreateNodes(states), edges.ToArray(), conflicts.ToArray());
    }

    internal static void AddPackageDependencies(
        NuGetSourcePackageMetadata packageMetadata,
        int parentDepth,
        NuGetTargetFramework? projectFramework,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageRejectionPolicy rejectionPolicy,
        System.Collections.Generic.List<NuGetDependencyResolutionState> states,
        System.Collections.Generic.List<NuGetDependencyGraphEdge> edges,
        System.Collections.Generic.List<NuGetDependencyConflict> conflicts,
        System.Collections.Generic.List<NuGetSourcePackageMetadata> queue,
        System.Collections.Generic.List<int> queueDepths)
    {
        if (projectFramework != null)
        {
            AddPackageDependenciesForFramework(
                packageMetadata,
                parentDepth,
                projectFramework,
                availablePackages,
                rejectionPolicy,
                states,
                edges,
                conflicts,
                queue,
                queueDepths);
            return;
        }

        int groupIndex = 0;

        while (groupIndex < packageMetadata.GetPackageMetadata().GetDependencyGroupCount())
        {
            NuGetDependencyGroup group = packageMetadata.GetPackageMetadata().GetDependencyGroup(groupIndex);
            int dependencyIndex = 0;

            while (dependencyIndex < group.GetDependencyCount())
            {
                NuGetDependency dependency = group.GetDependency(dependencyIndex);
                AddDependencyConstraintFromPackage(
                    packageMetadata,
                    parentDepth,
                    dependency,
                    availablePackages,
                    rejectionPolicy,
                    states,
                    edges,
                    conflicts,
                    queue,
                    queueDepths);
                dependencyIndex = dependencyIndex + 1;
            }

            groupIndex = groupIndex + 1;
        }
    }

    internal static void AddPackageDependenciesForFramework(
        NuGetSourcePackageMetadata packageMetadata,
        int parentDepth,
        NuGetTargetFramework projectFramework,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageRejectionPolicy rejectionPolicy,
        System.Collections.Generic.List<NuGetDependencyResolutionState> states,
        System.Collections.Generic.List<NuGetDependencyGraphEdge> edges,
        System.Collections.Generic.List<NuGetDependencyConflict> conflicts,
        System.Collections.Generic.List<NuGetSourcePackageMetadata> queue,
        System.Collections.Generic.List<int> queueDepths)
    {
        NuGetDependencyGroup? group = NuGetTargetFrameworkCompatibility.SelectBestDependencyGroup(
            projectFramework,
            packageMetadata.GetPackageMetadata());

        if (group == null)
        {
            if (packageMetadata.GetPackageMetadata().GetDependencyGroupCount() > 0)
            {
                conflicts.Add(new NuGetDependencyConflict(
                    packageMetadata.GetIdentity().GetPackageId(),
                    parentDepth,
                    NuGetResolverDiagnosticCodes.IncompatibleFramework,
                    "No NuGet dependency group is compatible with the project target framework."));
            }

            return;
        }

        int dependencyIndex = 0;

        while (dependencyIndex < group.GetDependencyCount())
        {
            NuGetDependency dependency = group.GetDependency(dependencyIndex);
            AddDependencyConstraintFromPackage(
                packageMetadata,
                parentDepth,
                dependency,
                availablePackages,
                rejectionPolicy,
                states,
                edges,
                conflicts,
                queue,
                queueDepths);
            dependencyIndex = dependencyIndex + 1;
        }
    }

    internal static void AddDependencyConstraintFromPackage(
        NuGetSourcePackageMetadata packageMetadata,
        int parentDepth,
        NuGetDependency dependency,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageRejectionPolicy rejectionPolicy,
        System.Collections.Generic.List<NuGetDependencyResolutionState> states,
        System.Collections.Generic.List<NuGetDependencyGraphEdge> edges,
        System.Collections.Generic.List<NuGetDependencyConflict> conflicts,
        System.Collections.Generic.List<NuGetSourcePackageMetadata> queue,
        System.Collections.Generic.List<int> queueDepths)
    {
        if (WouldCreateCycle(packageMetadata.GetIdentity().GetPackageId(), dependency.GetPackageId(), edges))
        {
            conflicts.Add(new NuGetDependencyConflict(
                dependency.GetPackageId(),
                parentDepth + 1,
                NuGetResolverDiagnosticCodes.CyclicDependency,
                "NuGet dependency graph contains a cyclic dependency."));
            return;
        }

        NuGetDependencyGraphEdge edge = NuGetDependencyGraphEdge.CreatePackageEdge(
            packageMetadata.GetIdentity(),
            dependency,
            parentDepth + 1);
        edges.Add(edge);
        AddConstraint(edge.GetConstraint(), availablePackages, rejectionPolicy, states, conflicts, queue, queueDepths);
    }

    internal static void AddConstraint(
        NuGetDependencyRangeConstraint constraint,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageRejectionPolicy rejectionPolicy,
        System.Collections.Generic.List<NuGetDependencyResolutionState> states,
        System.Collections.Generic.List<NuGetDependencyConflict> conflicts,
        System.Collections.Generic.List<NuGetSourcePackageMetadata> queue,
        System.Collections.Generic.List<int> queueDepths)
    {
        NuGetDependencyResolutionState? state = FindState(states, constraint.GetPackageId());

        if (state == null)
        {
            state = new NuGetDependencyResolutionState(constraint.GetPackageId(), constraint.GetDepth(), constraint.IsDirect());
            states.Add(state);
            state.AddConstraint(constraint);
            SelectPackageForState(state, availablePackages, rejectionPolicy, conflicts, queue, queueDepths);
            return;
        }

        if (constraint.GetDepth() < state.GetDepth())
        {
            state.ResetForNearerConstraint(constraint.GetDepth(), constraint.IsDirect());
            state.AddConstraint(constraint);
            SelectPackageForState(state, availablePackages, rejectionPolicy, conflicts, queue, queueDepths);
            return;
        }

        if (constraint.GetDepth() == state.GetDepth())
        {
            state.AddConstraint(constraint);
            SelectPackageForState(state, availablePackages, rejectionPolicy, conflicts, queue, queueDepths);
        }
    }

    internal static void SelectPackageForState(
        NuGetDependencyResolutionState state,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageRejectionPolicy rejectionPolicy,
        System.Collections.Generic.List<NuGetDependencyConflict> conflicts,
        System.Collections.Generic.List<NuGetSourcePackageMetadata> queue,
        System.Collections.Generic.List<int> queueDepths)
    {
        NuGetSourcePackageMetadata? selected = SelectLowestMatchingPackage(state, availablePackages, rejectionPolicy);
        if (selected == null)
        {
            state.ClearSelectedPackage();
            conflicts.Add(CreatePackageSelectionConflict(state, availablePackages));
            return;
        }

        NuGetDependencyConflict? ambiguousSourceConflict = CreateAmbiguousSourceConflict(
            state,
            selected,
            availablePackages,
            rejectionPolicy);
        if (ambiguousSourceConflict != null)
        {
            state.ClearSelectedPackage();
            conflicts.Add(ambiguousSourceConflict);
            return;
        }

        if (!state.HasSelectedPackage() || !state.GetSelectedPackage().GetIdentity().HasSameIdentity(selected.GetIdentity()))
        {
            state.SetSelectedPackage(selected);
            queue.Add(selected);
            queueDepths.Add(state.GetDepth());
        }
    }

    internal static NuGetSourcePackageMetadata? SelectLowestMatchingPackage(
        NuGetDependencyResolutionState state,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        NuGetSourcePackageMetadata? selected = null;
        int index = 0;

        while (index < availablePackages.Length)
        {
            if (CanSelectPackage(state, availablePackages[index], rejectionPolicy))
            {
                if (selected == null || NuGetVersionComparer.Compare(
                    availablePackages[index].GetIdentity().GetVersion(),
                    selected.GetIdentity().GetVersion()) < 0)
                {
                    selected = availablePackages[index];
                }
            }

            index = index + 1;
        }

        return selected;
    }

    internal static bool CanSelectPackage(
        NuGetDependencyResolutionState state,
        NuGetSourcePackageMetadata packageMetadata,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        if (!packageMetadata.GetIdentity().GetPackageId().EqualsId(state.GetPackageId()))
        {
            return false;
        }

        if (rejectionPolicy.ShouldReject(packageMetadata))
        {
            return false;
        }

        int index = 0;
        while (index < state.GetConstraintCount())
        {
            if (!state.GetConstraint(index).Allows(packageMetadata.GetIdentity()))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static NuGetDependencyConflict CreatePackageSelectionConflict(
        NuGetDependencyResolutionState state,
        NuGetSourcePackageMetadata[] availablePackages)
    {
        if (HasPackageMetadata(state.GetPackageId(), availablePackages))
        {
            return new NuGetDependencyConflict(
                state.GetPackageId(),
                state.GetDepth(),
                NuGetResolverDiagnosticCodes.UnsatisfiedConstraints,
                "No available NuGet package satisfies all nearest dependency constraints.");
        }

        return new NuGetDependencyConflict(
            state.GetPackageId(),
            state.GetDepth(),
            NuGetResolverDiagnosticCodes.MissingPackageMetadata,
            "No NuGet package metadata is available for the requested package.");
    }

    internal static NuGetDependencyConflict? CreateAmbiguousSourceConflict(
        NuGetDependencyResolutionState state,
        NuGetSourcePackageMetadata selected,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        int index = 0;

        while (index < availablePackages.Length)
        {
            if (CanSelectPackage(state, availablePackages[index], rejectionPolicy)
                && selected.GetIdentity().HasSameIdentity(availablePackages[index].GetIdentity())
                && !selected.GetSource().EqualsSource(availablePackages[index].GetSource()))
            {
                return new NuGetDependencyConflict(
                    state.GetPackageId(),
                    state.GetDepth(),
                    NuGetResolverDiagnosticCodes.AmbiguousSourceMatches,
                    "Multiple NuGet sources contain the same selected package version.");
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool HasPackageMetadata(NuGetPackageId packageId, NuGetSourcePackageMetadata[] availablePackages)
    {
        int index = 0;

        while (index < availablePackages.Length)
        {
            if (availablePackages[index].GetIdentity().GetPackageId().EqualsId(packageId))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool WouldCreateCycle(
        NuGetPackageId parentPackageId,
        NuGetPackageId dependencyPackageId,
        System.Collections.Generic.List<NuGetDependencyGraphEdge> edges)
    {
        if (parentPackageId.EqualsId(dependencyPackageId))
        {
            return true;
        }

        return HasDependencyPath(dependencyPackageId, parentPackageId, edges, 0);
    }

    internal static bool HasDependencyPath(
        NuGetPackageId currentPackageId,
        NuGetPackageId targetPackageId,
        System.Collections.Generic.List<NuGetDependencyGraphEdge> edges,
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
                && edges[index].GetParentIdentity().GetPackageId().EqualsId(currentPackageId))
            {
                NuGetPackageId childPackageId = edges[index].GetConstraint().GetPackageId();
                if (childPackageId.EqualsId(targetPackageId))
                {
                    return true;
                }

                if (HasDependencyPath(childPackageId, targetPackageId, edges, depth + 1))
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal static NuGetDependencyResolutionState? FindState(
        System.Collections.Generic.List<NuGetDependencyResolutionState> states,
        NuGetPackageId packageId)
    {
        int index = 0;

        while (index < states.Count)
        {
            if (states[index].GetPackageId().EqualsId(packageId))
            {
                return states[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static NuGetDependencyGraphNode[] CreateNodes(System.Collections.Generic.List<NuGetDependencyResolutionState> states)
    {
        System.Collections.Generic.List<NuGetDependencyGraphNode> nodes = new System.Collections.Generic.List<NuGetDependencyGraphNode>();
        int index = 0;

        while (index < states.Count)
        {
            if (states[index].HasSelectedPackage())
            {
                nodes.Add(new NuGetDependencyGraphNode(
                    states[index].GetSelectedPackage(),
                    states[index].GetDepth(),
                    states[index].IsDirect()));
            }

            index = index + 1;
        }

        return nodes.ToArray();
    }

    internal static void ValidateDependencies(NuGetDependency[] dependencies)
    {
        int index = 0;

        while (index < dependencies.Length)
        {
            if (dependencies[index] == null)
            {
                throw new System.ArgumentException("NuGet root dependency arrays cannot contain null values.");
            }

            index = index + 1;
        }
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
}

internal class NuGetDependencyResolutionState
{
    internal NuGetPackageId packageId;
    internal int depth;
    internal bool direct;
    internal NuGetSourcePackageMetadata? selectedPackage;
    internal System.Collections.Generic.List<NuGetDependencyRangeConstraint> constraints;

    internal NuGetDependencyResolutionState(NuGetPackageId packageIdValue, int depthValue, bool directValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageIdValue);

        packageId = packageIdValue;
        depth = depthValue;
        direct = directValue;
        selectedPackage = null;
        constraints = new System.Collections.Generic.List<NuGetDependencyRangeConstraint>();
    }

    internal NuGetPackageId GetPackageId()
    {
        return packageId;
    }

    internal int GetDepth()
    {
        return depth;
    }

    internal bool IsDirect()
    {
        return direct;
    }

    internal void ResetForNearerConstraint(int depthValue, bool directValue)
    {
        depth = depthValue;
        direct = directValue;
        selectedPackage = null;
        constraints.Clear();
    }

    internal void AddConstraint(NuGetDependencyRangeConstraint constraint)
    {
        System.ArgumentNullException.ThrowIfNull(constraint);

        constraints.Add(constraint);
    }

    internal int GetConstraintCount()
    {
        return constraints.Count;
    }

    internal NuGetDependencyRangeConstraint GetConstraint(int constraintIndex)
    {
        if (constraintIndex < 0 || constraintIndex >= constraints.Count)
        {
            throw new System.ArgumentOutOfRangeException("constraintIndex");
        }

        return constraints[constraintIndex];
    }

    internal bool HasSelectedPackage()
    {
        return selectedPackage != null;
    }

    internal NuGetSourcePackageMetadata GetSelectedPackage()
    {
        if (selectedPackage == null)
        {
            throw new System.InvalidOperationException("Dependency state does not contain a selected package.");
        }

        return selectedPackage;
    }

    internal void SetSelectedPackage(NuGetSourcePackageMetadata packageMetadata)
    {
        System.ArgumentNullException.ThrowIfNull(packageMetadata);

        selectedPackage = packageMetadata;
    }

    internal void ClearSelectedPackage()
    {
        selectedPackage = null;
    }
}
