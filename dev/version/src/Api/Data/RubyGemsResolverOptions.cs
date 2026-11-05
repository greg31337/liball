namespace version;

/// <summary>
/// Configures rubygems resolver behavior.
/// </summary>
/// <remarks>
/// Configure behavior through the public fields before passing the value to an operation.
/// </remarks>
public class RubyGemsResolverOptions
{
    /// <summary>
    /// Stores the <c>prereleasePolicy</c> value.
    /// </summary>
    public PrereleasePolicy prereleasePolicy;
    /// <summary>
    /// Stores the <c>includeDevelopmentDependencies</c> value.
    /// </summary>
    public bool includeDevelopmentDependencies;
    /// <summary>
    /// Stores the <c>maxBacktrackingSteps</c> value.
    /// </summary>
    public int maxBacktrackingSteps;
    /// <summary>
    /// Stores the <c>platforms</c> value.
    /// </summary>
    public RubyGemsPlatform[] platforms;
    /// <summary>
    /// Stores the <c>sources</c> value.
    /// </summary>
    public RubyGemsSource[] sources;
    /// <summary>
    /// Stores the <c>exclusions</c> value.
    /// </summary>
    public RubyGemsDependencyExclusion[] exclusions;

    /// <summary>
    /// Initializes a new RubyGems Resolver Options instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    public RubyGemsResolverOptions()
    {
        prereleasePolicy = PrereleasePolicy.Default;
        includeDevelopmentDependencies = false;
        maxBacktrackingSteps = 1000;
        platforms = new RubyGemsPlatform[] { RubyGemsPlatform.CreateRuby() };
        sources = new RubyGemsSource[0];
        exclusions = new RubyGemsDependencyExclusion[0];
    }

    /// <summary>
    /// Creates a default.
    /// </summary>
    /// <returns>The resulting rubygems resolver options.</returns>
    internal static RubyGemsResolverOptions CreateDefault()
    {
        return new RubyGemsResolverOptions();
    }

    /// <summary>
    /// Gets a prerelease policy.
    /// </summary>
    /// <returns>The resulting prerelease policy.</returns>
    internal PrereleasePolicy GetPrereleasePolicy()
    {
        return prereleasePolicy;
    }

    /// <summary>
    /// Updates the prerelease policy setting.
    /// </summary>
    /// <param name="prereleasePolicyValue">The prerelease policy value used by the operation.</param>
    internal void SetPrereleasePolicy(PrereleasePolicy prereleasePolicyValue)
    {
        ValidatePrereleasePolicy(prereleasePolicyValue);
        prereleasePolicy = prereleasePolicyValue;
    }

    /// <summary>
    /// Reports whether development dependencies.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IncludesDevelopmentDependencies()
    {
        return includeDevelopmentDependencies;
    }

    /// <summary>
    /// Updates the include development dependencies setting.
    /// </summary>
    /// <param name="includeDevelopmentDependenciesValue">Whether to enable include development dependencies value.</param>
    internal void SetIncludeDevelopmentDependencies(bool includeDevelopmentDependenciesValue)
    {
        includeDevelopmentDependencies = includeDevelopmentDependenciesValue;
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
    /// Gets a platform count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetPlatformCount()
    {
        return platforms.Length;
    }

    /// <summary>
    /// Gets a platform.
    /// </summary>
    /// <param name="platformIndex">The zero-based platform index.</param>
    /// <returns>The resulting rubygems platform.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal RubyGemsPlatform GetPlatform(int platformIndex)
    {
        if (platformIndex < 0 || platformIndex >= platforms.Length)
        {
            throw new System.ArgumentOutOfRangeException("platformIndex");
        }

        return platforms[platformIndex];
    }

    /// <summary>
    /// Gets a platforms.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal RubyGemsPlatform[] GetPlatforms()
    {
        return CopyPlatforms(platforms);
    }

    /// <summary>
    /// Updates the platforms setting.
    /// </summary>
    /// <param name="platformValues">The platform values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal void SetPlatforms(RubyGemsPlatform[] platformValues)
    {
        System.ArgumentNullException.ThrowIfNull(platformValues);

        platforms = CopyPlatforms(platformValues);
    }

    /// <summary>
    /// Gets a source count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetSourceCount()
    {
        return sources.Length;
    }

    /// <summary>
    /// Gets a source.
    /// </summary>
    /// <param name="sourceIndex">The zero-based source index.</param>
    /// <returns>The resulting rubygems source.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal RubyGemsSource GetSource(int sourceIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= sources.Length)
        {
            throw new System.ArgumentOutOfRangeException("sourceIndex");
        }

        return sources[sourceIndex];
    }

    /// <summary>
    /// Gets a sources.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal RubyGemsSource[] GetSources()
    {
        return CopySources(sources);
    }

    /// <summary>
    /// Updates the sources setting.
    /// </summary>
    /// <param name="sourceValues">The source values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal void SetSources(RubyGemsSource[] sourceValues)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValues);

        sources = CopySources(sourceValues);
    }

    /// <summary>
    /// Gets an exclusion count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetExclusionCount()
    {
        return exclusions.Length;
    }

    /// <summary>
    /// Gets an exclusion.
    /// </summary>
    /// <param name="exclusionIndex">The zero-based exclusion index.</param>
    /// <returns>The resulting rubygems dependency exclusion.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal RubyGemsDependencyExclusion GetExclusion(int exclusionIndex)
    {
        if (exclusionIndex < 0 || exclusionIndex >= exclusions.Length)
        {
            throw new System.ArgumentOutOfRangeException("exclusionIndex");
        }

        return exclusions[exclusionIndex];
    }

    /// <summary>
    /// Gets an exclusions.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal RubyGemsDependencyExclusion[] GetExclusions()
    {
        return CopyExclusions(exclusions);
    }

    /// <summary>
    /// Updates the exclusions setting.
    /// </summary>
    /// <param name="exclusionValues">The exclusion values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal void SetExclusions(RubyGemsDependencyExclusion[] exclusionValues)
    {
        System.ArgumentNullException.ThrowIfNull(exclusionValues);

        exclusions = CopyExclusions(exclusionValues);
    }

    internal static void ValidatePrereleasePolicy(PrereleasePolicy policy)
    {
        if (policy == PrereleasePolicy.Default)
        {
            return;
        }

        if (policy == PrereleasePolicy.Include)
        {
            return;
        }

        if (policy == PrereleasePolicy.Exclude)
        {
            return;
        }

        if (policy == PrereleasePolicy.ExplicitOnly)
        {
            return;
        }

        if (policy == PrereleasePolicy.AutoIfNoStableCandidate)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("policy");
    }

    internal static RubyGemsPlatform[] CopyPlatforms(RubyGemsPlatform[] platformValues)
    {
        RubyGemsPlatform[] copy = new RubyGemsPlatform[platformValues.Length];
        int index = 0;

        while (index < platformValues.Length)
        {
            if (platformValues[index] == null)
            {
                throw new System.ArgumentException("RubyGems platform arrays cannot contain null values.");
            }

            copy[index] = platformValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static RubyGemsSource[] CopySources(RubyGemsSource[] sourceValues)
    {
        RubyGemsSource[] copy = new RubyGemsSource[sourceValues.Length];
        int index = 0;

        while (index < sourceValues.Length)
        {
            if (sourceValues[index] == null)
            {
                throw new System.ArgumentException("RubyGems source arrays cannot contain null values.");
            }

            copy[index] = sourceValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static RubyGemsDependencyExclusion[] CopyExclusions(RubyGemsDependencyExclusion[] exclusionValues)
    {
        RubyGemsDependencyExclusion[] copy = new RubyGemsDependencyExclusion[exclusionValues.Length];
        int index = 0;

        while (index < exclusionValues.Length)
        {
            if (exclusionValues[index] == null)
            {
                throw new System.ArgumentException("RubyGems dependency exclusion arrays cannot contain null values.");
            }

            copy[index] = exclusionValues[index];
            index = index + 1;
        }

        return copy;
    }
}
