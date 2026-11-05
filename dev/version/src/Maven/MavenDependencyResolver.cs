namespace version;

internal static class MavenDependencyResolver
{
    internal static MavenDependencyResolutionResult Resolve(
        MavenDependency[] rootDependencies,
        MavenDependencyModel[] availableModels)
    {
        return Resolve(rootDependencies, MavenDependencyManagement.CreateEmpty(), availableModels);
    }

    internal static MavenDependencyResolutionResult Resolve(
        MavenDependency[] rootDependencies,
        MavenDependencyManagement rootDependencyManagement,
        MavenDependencyModel[] availableModels)
    {
        System.ArgumentNullException.ThrowIfNull(rootDependencies);
        System.ArgumentNullException.ThrowIfNull(rootDependencyManagement);
        System.ArgumentNullException.ThrowIfNull(availableModels);
        ValidateDependencies(rootDependencies);
        ValidateModels(availableModels);

        System.Collections.Generic.List<MavenDependencyResolutionState> states = new System.Collections.Generic.List<MavenDependencyResolutionState>();
        System.Collections.Generic.List<MavenDependencyGraphEdge> edges = new System.Collections.Generic.List<MavenDependencyGraphEdge>();
        System.Collections.Generic.List<MavenDependencyConflict> conflicts = new System.Collections.Generic.List<MavenDependencyConflict>();
        System.Collections.Generic.List<MavenDependencyResolutionState> queue = new System.Collections.Generic.List<MavenDependencyResolutionState>();
        int index = 0;

        while (index < rootDependencies.Length)
        {
            MavenDependency dependency = ApplyDependencyManagement(rootDependencies[index], rootDependencyManagement);
            MavenDependencyGraphEdge edge = MavenDependencyGraphEdge.CreateRootEdge(dependency);
            edges.Add(edge);

            if (IsUnsupportedRootDependency(dependency))
            {
                conflicts.Add(new MavenDependencyConflict(
                    dependency.GetCoordinate(),
                    0,
                    MavenResolverDiagnosticCodes.UnsupportedFeature,
                    "Maven system and import scoped root dependencies are not supported by the finite metadata resolver."));
            }
            else
            {
                AddDependency(dependency, 0, true, dependency.GetScope(), availableModels, states, conflicts, queue);
            }

            index = index + 1;
        }

        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            AddPackageDependencies(queue[queueIndex], availableModels, states, edges, conflicts, queue);
            queueIndex = queueIndex + 1;
        }

        return new MavenDependencyResolutionResult(CreateNodes(states), edges.ToArray(), conflicts.ToArray());
    }

    internal static void AddPackageDependencies(
        MavenDependencyResolutionState parentState,
        MavenDependencyModel[] availableModels,
        System.Collections.Generic.List<MavenDependencyResolutionState> states,
        System.Collections.Generic.List<MavenDependencyGraphEdge> edges,
        System.Collections.Generic.List<MavenDependencyConflict> conflicts,
        System.Collections.Generic.List<MavenDependencyResolutionState> queue)
    {
        MavenDependencyModel parentModel = parentState.GetSelectedModel();
        MavenDependencyManagement management = CreateEffectiveDependencyManagement(parentModel, availableModels);
        int index = 0;

        while (index < parentModel.GetDependencyCount())
        {
            MavenDependency dependency = ApplyDependencyManagement(parentModel.GetDependency(index), management);
            MavenDependencyScope effectiveScope = CombineScopes(parentState.GetEffectiveScope(), dependency.GetScope());

            if (effectiveScope != MavenDependencyScope.Import && !dependency.IsOptional() && !IsExcluded(parentState, dependency))
            {
                MavenDependencyGraphEdge edge = MavenDependencyGraphEdge.CreatePackageEdge(
                    parentModel.GetIdentity(),
                    dependency,
                    parentState.GetDepth() + 1,
                    effectiveScope);
                edges.Add(edge);
                AddDependency(
                    dependency,
                    parentState.GetDepth() + 1,
                    false,
                    effectiveScope,
                    availableModels,
                    states,
                    conflicts,
                    queue);
            }

            index = index + 1;
        }
    }

    internal static void AddDependency(
        MavenDependency dependency,
        int depth,
        bool direct,
        MavenDependencyScope effectiveScope,
        MavenDependencyModel[] availableModels,
        System.Collections.Generic.List<MavenDependencyResolutionState> states,
        System.Collections.Generic.List<MavenDependencyConflict> conflicts,
        System.Collections.Generic.List<MavenDependencyResolutionState> queue)
    {
        MavenDependencyResolutionState? state = FindState(states, dependency.GetCoordinate());
        if (state != null && depth >= state.GetDepth())
        {
            return;
        }

        MavenDependencyModel? selectedModel = SelectModel(dependency, availableModels);
        if (selectedModel == null)
        {
            string diagnosticCode = GetSelectionConflictCode(dependency, availableModels);
            conflicts.Add(new MavenDependencyConflict(
                dependency.GetCoordinate(),
                depth,
                diagnosticCode,
                "No available Maven artifact satisfies the dependency."));
            return;
        }

        if (state == null)
        {
            state = new MavenDependencyResolutionState(dependency.GetCoordinate(), selectedModel, dependency, depth, direct, effectiveScope);
            states.Add(state);
        }
        else
        {
            state.SetSelectedModel(selectedModel, dependency, depth, direct, effectiveScope);
        }

        queue.Add(state);
    }

    internal static MavenDependency ApplyDependencyManagement(MavenDependency dependency, MavenDependencyManagement dependencyManagement)
    {
        MavenDependency? managed = FindManagedDependency(dependency, dependencyManagement);
        if (managed == null || dependency.HasVersionRange())
        {
            return dependency;
        }

        if (!managed.HasVersionRange())
        {
            return dependency;
        }

        return new MavenDependency(
            dependency.GetCoordinate(),
            managed.GetVersionRangeText(),
            dependency.GetScope(),
            dependency.IsOptional(),
            dependency.GetExclusions());
    }

    internal static MavenDependency? FindManagedDependency(MavenDependency dependency, MavenDependencyManagement dependencyManagement)
    {
        int index = 0;

        while (index < dependencyManagement.GetManagedDependencyCount())
        {
            MavenDependency managed = dependencyManagement.GetManagedDependency(index);
            if (CoordinatesMatch(dependency.GetCoordinate(), managed.GetCoordinate()))
            {
                return managed;
            }

            index = index + 1;
        }

        return null;
    }

    internal static MavenDependencyManagement CreateEffectiveDependencyManagement(
        MavenDependencyModel model,
        MavenDependencyModel[] availableModels)
    {
        System.Collections.Generic.List<MavenDependency> managedDependencies = new System.Collections.Generic.List<MavenDependency>();
        System.Collections.Generic.List<MavenImportedBomConstraint> importedBoms = new System.Collections.Generic.List<MavenImportedBomConstraint>();

        AddParentManagement(model, availableModels, managedDependencies, importedBoms);
        AddManagement(model.GetDependencyManagement(), availableModels, managedDependencies, importedBoms);

        return new MavenDependencyManagement(managedDependencies.ToArray(), importedBoms.ToArray());
    }

    internal static void AddParentManagement(
        MavenDependencyModel model,
        MavenDependencyModel[] availableModels,
        System.Collections.Generic.List<MavenDependency> managedDependencies,
        System.Collections.Generic.List<MavenImportedBomConstraint> importedBoms)
    {
        if (!model.HasParentReference())
        {
            return;
        }

        MavenDependencyModel? parent = FindModelByIdentity(model.GetParentReference().GetIdentity(), availableModels);
        if (parent == null)
        {
            return;
        }

        AddParentManagement(parent, availableModels, managedDependencies, importedBoms);
        AddManagement(parent.GetDependencyManagement(), availableModels, managedDependencies, importedBoms);
    }

    internal static void AddManagement(
        MavenDependencyManagement dependencyManagement,
        MavenDependencyModel[] availableModels,
        System.Collections.Generic.List<MavenDependency> managedDependencies,
        System.Collections.Generic.List<MavenImportedBomConstraint> importedBoms)
    {
        int bomIndex = 0;
        while (bomIndex < dependencyManagement.GetImportedBomCount())
        {
            MavenImportedBomConstraint bom = dependencyManagement.GetImportedBom(bomIndex);
            MavenDependencyModel? bomModel = FindModelByIdentity(bom.GetIdentity(), availableModels);
            if (bomModel != null)
            {
                AddManagement(bomModel.GetDependencyManagement(), availableModels, managedDependencies, importedBoms);
            }

            importedBoms.Add(bom);
            bomIndex = bomIndex + 1;
        }

        int dependencyIndex = 0;
        while (dependencyIndex < dependencyManagement.GetManagedDependencyCount())
        {
            AddOrReplaceManagedDependency(managedDependencies, dependencyManagement.GetManagedDependency(dependencyIndex));
            dependencyIndex = dependencyIndex + 1;
        }
    }

    internal static void AddOrReplaceManagedDependency(
        System.Collections.Generic.List<MavenDependency> managedDependencies,
        MavenDependency dependency)
    {
        int index = 0;

        while (index < managedDependencies.Count)
        {
            if (CoordinatesMatch(managedDependencies[index].GetCoordinate(), dependency.GetCoordinate()))
            {
                managedDependencies[index] = dependency;
                return;
            }

            index = index + 1;
        }

        managedDependencies.Add(dependency);
    }

    internal static MavenDependencyModel? SelectModel(MavenDependency dependency, MavenDependencyModel[] availableModels)
    {
        MavenDependencyModel? selected = null;
        int index = 0;

        while (index < availableModels.Length)
        {
            if (CoordinatesMatch(dependency.GetCoordinate(), availableModels[index].GetIdentity().GetCoordinate())
                && DependencyAllowsModel(dependency, availableModels[index]))
            {
                if (selected == null || MavenVersionComparer.Compare(
                    availableModels[index].GetIdentity().GetVersionText(),
                    selected.GetIdentity().GetVersionText()) < 0)
                {
                    selected = availableModels[index];
                }
            }

            index = index + 1;
        }

        return selected;
    }

    internal static bool DependencyAllowsModel(MavenDependency dependency, MavenDependencyModel model)
    {
        if (!dependency.HasVersionRange())
        {
            return true;
        }

        if (!IsRangeText(dependency.GetVersionRangeText()))
        {
            return MavenVersionComparer.Compare(dependency.GetVersionRangeText(), model.GetIdentity().GetVersionText()) == 0;
        }

        return dependency.AllowsVersionText(model.GetIdentity().GetVersionText());
    }

    internal static MavenDependencyModel? FindModelByIdentity(MavenArtifactVersionIdentity identity, MavenDependencyModel[] availableModels)
    {
        int index = 0;

        while (index < availableModels.Length)
        {
            if (availableModels[index].GetIdentity().HasSameIdentity(identity))
            {
                return availableModels[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static MavenDependencyResolutionState? FindState(
        System.Collections.Generic.List<MavenDependencyResolutionState> states,
        MavenArtifactCoordinate coordinate)
    {
        int index = 0;

        while (index < states.Count)
        {
            if (CoordinatesMatch(states[index].GetCoordinate(), coordinate))
            {
                return states[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static MavenDependencyGraphNode[] CreateNodes(System.Collections.Generic.List<MavenDependencyResolutionState> states)
    {
        MavenDependencyGraphNode[] nodes = new MavenDependencyGraphNode[states.Count];
        int index = 0;

        while (index < states.Count)
        {
            nodes[index] = new MavenDependencyGraphNode(
                states[index].GetSelectedModel(),
                states[index].GetDepth(),
                states[index].IsDirect(),
                states[index].GetEffectiveScope());
            index = index + 1;
        }

        return nodes;
    }

    internal static bool IsExcluded(MavenDependencyResolutionState parentState, MavenDependency dependency)
    {
        return parentState.GetSelectedByDependency().Excludes(dependency.GetCoordinate());
    }

    internal static bool IsUnsupportedRootDependency(MavenDependency dependency)
    {
        return dependency.GetScope() == MavenDependencyScope.System || dependency.GetScope() == MavenDependencyScope.Import;
    }

    internal static MavenDependencyScope CombineScopes(MavenDependencyScope parentScope, MavenDependencyScope childScope)
    {
        if (childScope == MavenDependencyScope.Test
            || childScope == MavenDependencyScope.Provided
            || childScope == MavenDependencyScope.System
            || childScope == MavenDependencyScope.Import)
        {
            return MavenDependencyScope.Import;
        }

        if (parentScope == MavenDependencyScope.Runtime)
        {
            return MavenDependencyScope.Runtime;
        }

        if (parentScope == MavenDependencyScope.Test)
        {
            return MavenDependencyScope.Test;
        }

        if (childScope == MavenDependencyScope.Runtime)
        {
            return MavenDependencyScope.Runtime;
        }

        return MavenDependencyScope.Compile;
    }

    internal static bool CoordinatesMatch(MavenArtifactCoordinate left, MavenArtifactCoordinate right)
    {
        return left.HasSameCoordinate(right);
    }

    internal static string GetSelectionConflictCode(MavenDependency dependency, MavenDependencyModel[] availableModels)
    {
        if (HasMatchingCoordinate(dependency, availableModels))
        {
            return MavenResolverDiagnosticCodes.ConflictingRanges;
        }

        return MavenResolverDiagnosticCodes.UnresolvedArtifactCoordinate;
    }

    internal static bool HasMatchingCoordinate(MavenDependency dependency, MavenDependencyModel[] availableModels)
    {
        int index = 0;

        while (index < availableModels.Length)
        {
            if (CoordinatesMatch(dependency.GetCoordinate(), availableModels[index].GetIdentity().GetCoordinate()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsRangeText(string versionRangeText)
    {
        if (versionRangeText.Length == 0)
        {
            return false;
        }

        return versionRangeText[0] == '[' || versionRangeText[0] == '(';
    }

    internal static void ValidateDependencies(MavenDependency[] dependencies)
    {
        int index = 0;

        while (index < dependencies.Length)
        {
            if (dependencies[index] == null)
            {
                throw new System.ArgumentException("Maven root dependency arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateModels(MavenDependencyModel[] models)
    {
        int index = 0;

        while (index < models.Length)
        {
            if (models[index] == null)
            {
                throw new System.ArgumentException("Maven dependency model arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }
}

internal class MavenDependencyResolutionState
{
    internal MavenArtifactCoordinate coordinate;
    internal MavenDependencyModel selectedModel;
    internal MavenDependency selectedByDependency;
    internal int depth;
    internal bool direct;
    internal MavenDependencyScope effectiveScope;

    internal MavenDependencyResolutionState(
        MavenArtifactCoordinate coordinateValue,
        MavenDependencyModel selectedModelValue,
        MavenDependency selectedByDependencyValue,
        int depthValue,
        bool directValue,
        MavenDependencyScope effectiveScopeValue)
    {
        System.ArgumentNullException.ThrowIfNull(coordinateValue);
        System.ArgumentNullException.ThrowIfNull(selectedModelValue);
        System.ArgumentNullException.ThrowIfNull(selectedByDependencyValue);

        coordinate = coordinateValue;
        selectedModel = selectedModelValue;
        selectedByDependency = selectedByDependencyValue;
        depth = depthValue;
        direct = directValue;
        effectiveScope = effectiveScopeValue;
    }

    internal MavenArtifactCoordinate GetCoordinate()
    {
        return coordinate;
    }

    internal MavenDependencyModel GetSelectedModel()
    {
        return selectedModel;
    }

    internal MavenDependency GetSelectedByDependency()
    {
        return selectedByDependency;
    }

    internal int GetDepth()
    {
        return depth;
    }

    internal bool IsDirect()
    {
        return direct;
    }

    internal MavenDependencyScope GetEffectiveScope()
    {
        return effectiveScope;
    }

    internal void SetSelectedModel(
        MavenDependencyModel selectedModelValue,
        MavenDependency selectedByDependencyValue,
        int depthValue,
        bool directValue,
        MavenDependencyScope effectiveScopeValue)
    {
        System.ArgumentNullException.ThrowIfNull(selectedModelValue);
        System.ArgumentNullException.ThrowIfNull(selectedByDependencyValue);

        selectedModel = selectedModelValue;
        selectedByDependency = selectedByDependencyValue;
        depth = depthValue;
        direct = directValue;
        effectiveScope = effectiveScopeValue;
    }
}
