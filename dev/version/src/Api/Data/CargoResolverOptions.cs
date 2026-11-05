namespace version;

/// <summary>
/// Configures cargo resolver behavior.
/// </summary>
/// <remarks>
/// Configure behavior through the public fields before passing the value to an operation.
/// </remarks>
public class CargoResolverOptions
{
    /// <summary>
    /// Stores the <c>resolverVersion</c> value.
    /// </summary>
    public CargoResolverVersion resolverVersion;
    /// <summary>
    /// Stores the <c>includeYankedVersions</c> value.
    /// </summary>
    public bool includeYankedVersions;
    /// <summary>
    /// Stores the <c>includePrereleaseVersions</c> value.
    /// </summary>
    public bool includePrereleaseVersions;
    /// <summary>
    /// Stores the <c>maxBacktrackingSteps</c> value.
    /// </summary>
    public int maxBacktrackingSteps;
    /// <summary>
    /// Stores the <c>targetTriple</c> value.
    /// </summary>
    public string targetTriple;
    /// <summary>
    /// Stores the <c>targetTripleSpecified</c> value.
    /// </summary>
    public bool targetTripleSpecified;
    /// <summary>
    /// Stores the <c>registrySources</c> value.
    /// </summary>
    public CargoRegistrySource[] registrySources;
    /// <summary>
    /// Stores the <c>sourceReplacements</c> value.
    /// </summary>
    public CargoSourceReplacement[] sourceReplacements;

    /// <summary>
    /// Initializes a new Cargo Resolver Options instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    public CargoResolverOptions()
    {
        resolverVersion = CargoResolverVersion.Version2;
        includeYankedVersions = false;
        includePrereleaseVersions = false;
        maxBacktrackingSteps = 1000;
        targetTriple = "";
        targetTripleSpecified = false;
        registrySources = new CargoRegistrySource[0];
        sourceReplacements = new CargoSourceReplacement[0];
    }

    /// <summary>
    /// Creates a default.
    /// </summary>
    /// <returns>The resulting cargo resolver options.</returns>
    internal static CargoResolverOptions CreateDefault()
    {
        return new CargoResolverOptions();
    }

    /// <summary>
    /// Gets a resolver version.
    /// </summary>
    /// <returns>The resulting cargo resolver version.</returns>
    internal CargoResolverVersion GetResolverVersion()
    {
        return resolverVersion;
    }

    /// <summary>
    /// Updates the resolver version setting.
    /// </summary>
    /// <param name="resolverVersionValue">The resolver version value used by the operation.</param>
    internal void SetResolverVersion(CargoResolverVersion resolverVersionValue)
    {
        ValidateResolverVersion(resolverVersionValue);
        resolverVersion = resolverVersionValue;
    }

    /// <summary>
    /// Reports whether yanked versions.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IncludesYankedVersions()
    {
        return includeYankedVersions;
    }

    /// <summary>
    /// Updates the include yanked versions setting.
    /// </summary>
    /// <param name="includeYankedVersionsValue">Whether to enable include yanked versions value.</param>
    internal void SetIncludeYankedVersions(bool includeYankedVersionsValue)
    {
        includeYankedVersions = includeYankedVersionsValue;
    }

    /// <summary>
    /// Reports whether prerelease versions.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IncludesPrereleaseVersions()
    {
        return includePrereleaseVersions;
    }

    /// <summary>
    /// Updates the include prerelease versions setting.
    /// </summary>
    /// <param name="includePrereleaseVersionsValue">Whether to enable include prerelease versions value.</param>
    internal void SetIncludePrereleaseVersions(bool includePrereleaseVersionsValue)
    {
        includePrereleaseVersions = includePrereleaseVersionsValue;
    }

    /// <summary>
    /// Gets a max backtracking steps.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetMaxBacktrackingSteps()
    {
        return maxBacktrackingSteps;
    }

    /// <summary>
    /// Updates the max backtracking steps setting.
    /// </summary>
    /// <param name="maxBacktrackingStepsValue">The max backtracking steps value used by the operation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal void SetMaxBacktrackingSteps(int maxBacktrackingStepsValue)
    {
        if (maxBacktrackingStepsValue <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maxBacktrackingStepsValue");
        }

        maxBacktrackingSteps = maxBacktrackingStepsValue;
    }

    /// <summary>
    /// Reports whether this value has target triple.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTargetTriple()
    {
        return targetTripleSpecified;
    }

    /// <summary>
    /// Gets a target triple.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetTargetTriple()
    {
        return targetTriple;
    }

    /// <summary>
    /// Updates the target triple setting.
    /// </summary>
    /// <param name="targetTripleValue">The target triple value used by the operation.</param>
    internal void SetTargetTriple(string targetTripleValue)
    {
        if (targetTripleValue == null || targetTripleValue.Length == 0)
        {
            targetTriple = "";
            targetTripleSpecified = false;
            return;
        }

        CargoDependency.ValidateTargetText(targetTripleValue, "Cargo resolver target triple cannot contain invalid characters.");
        targetTriple = targetTripleValue;
        targetTripleSpecified = true;
    }

    /// <summary>
    /// Gets a registry source count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetRegistrySourceCount()
    {
        return registrySources.Length;
    }

    /// <summary>
    /// Gets a registry source.
    /// </summary>
    /// <param name="registrySourceIndex">The zero-based registry source index.</param>
    /// <returns>The resulting cargo registry source.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal CargoRegistrySource GetRegistrySource(int registrySourceIndex)
    {
        if (registrySourceIndex < 0 || registrySourceIndex >= registrySources.Length)
        {
            throw new System.ArgumentOutOfRangeException("registrySourceIndex");
        }

        return registrySources[registrySourceIndex];
    }

    /// <summary>
    /// Gets a registry sources.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal CargoRegistrySource[] GetRegistrySources()
    {
        return CopyRegistrySources(registrySources);
    }

    /// <summary>
    /// Updates the registry sources setting.
    /// </summary>
    /// <param name="registrySourceValues">The registry source values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal void SetRegistrySources(CargoRegistrySource[] registrySourceValues)
    {
        System.ArgumentNullException.ThrowIfNull(registrySourceValues);

        registrySources = CopyRegistrySources(registrySourceValues);
    }

    /// <summary>
    /// Gets a source replacement count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetSourceReplacementCount()
    {
        return sourceReplacements.Length;
    }

    /// <summary>
    /// Gets a source replacement.
    /// </summary>
    /// <param name="sourceReplacementIndex">The zero-based source replacement index.</param>
    /// <returns>The resulting cargo source replacement.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal CargoSourceReplacement GetSourceReplacement(int sourceReplacementIndex)
    {
        if (sourceReplacementIndex < 0 || sourceReplacementIndex >= sourceReplacements.Length)
        {
            throw new System.ArgumentOutOfRangeException("sourceReplacementIndex");
        }

        return sourceReplacements[sourceReplacementIndex];
    }

    /// <summary>
    /// Gets a source replacements.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal CargoSourceReplacement[] GetSourceReplacements()
    {
        return CopySourceReplacements(sourceReplacements);
    }

    /// <summary>
    /// Updates the source replacements setting.
    /// </summary>
    /// <param name="sourceReplacementValues">The source replacement values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal void SetSourceReplacements(CargoSourceReplacement[] sourceReplacementValues)
    {
        System.ArgumentNullException.ThrowIfNull(sourceReplacementValues);

        sourceReplacements = CopySourceReplacements(sourceReplacementValues);
    }

    internal static void ValidateResolverVersion(CargoResolverVersion resolverVersionValue)
    {
        if (resolverVersionValue == CargoResolverVersion.Version1)
        {
            return;
        }

        if (resolverVersionValue == CargoResolverVersion.Version2)
        {
            return;
        }

        if (resolverVersionValue == CargoResolverVersion.Version3)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("resolverVersionValue");
    }

    internal static CargoRegistrySource[] CopyRegistrySources(CargoRegistrySource[] registrySourceValues)
    {
        CargoRegistrySource[] copy = new CargoRegistrySource[registrySourceValues.Length];
        int index = 0;

        while (index < registrySourceValues.Length)
        {
            if (registrySourceValues[index] == null)
            {
                throw new System.ArgumentException("Cargo registry source arrays cannot contain null values.");
            }

            copy[index] = registrySourceValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static CargoSourceReplacement[] CopySourceReplacements(CargoSourceReplacement[] sourceReplacementValues)
    {
        CargoSourceReplacement[] copy = new CargoSourceReplacement[sourceReplacementValues.Length];
        int index = 0;

        while (index < sourceReplacementValues.Length)
        {
            if (sourceReplacementValues[index] == null)
            {
                throw new System.ArgumentException("Cargo source replacement arrays cannot contain null values.");
            }

            copy[index] = sourceReplacementValues[index];
            index = index + 1;
        }

        return copy;
    }
}
