namespace version;

/// <summary>
/// Represents a maven dependency.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenDependency
{
    /// <summary>
    /// Stores the <c>coordinate</c> value.
    /// </summary>
    public MavenArtifactCoordinate coordinate;
    /// <summary>
    /// Stores the <c>versionRangeText</c> value.
    /// </summary>
    public string versionRangeText;
    /// <summary>
    /// Stores the <c>versionRangeSpecified</c> value.
    /// </summary>
    public bool versionRangeSpecified;
    /// <summary>
    /// Stores the <c>versionRange</c> value.
    /// </summary>
    public VersionRange? versionRange;
    /// <summary>
    /// Stores the <c>scope</c> value.
    /// </summary>
    public MavenDependencyScope scope;
    /// <summary>
    /// Stores the <c>optional</c> value.
    /// </summary>
    public bool optional;
    /// <summary>
    /// Stores the <c>exclusions</c> value.
    /// </summary>
    public MavenDependencyExclusion[] exclusions;

    /// <summary>
    /// Initializes a new Maven Dependency instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="coordinateValue">The coordinate value used by the operation.</param>
    /// <param name="versionRangeTextValue">The version range text value to process.</param>
    /// <param name="scopeValue">The scope value used by the operation.</param>
    /// <param name="optionalValue">Whether to enable optional value.</param>
    /// <param name="exclusionValues">The exclusion values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public MavenDependency(
        MavenArtifactCoordinate coordinateValue,
        string? versionRangeTextValue,
        MavenDependencyScope scopeValue,
        bool optionalValue,
        MavenDependencyExclusion[] exclusionValues)
    {
        System.ArgumentNullException.ThrowIfNull(coordinateValue);
        System.ArgumentNullException.ThrowIfNull(exclusionValues);
        ValidateScope(scopeValue);

        coordinate = coordinateValue;
        scope = scopeValue;
        optional = optionalValue;
        exclusions = CopyExclusions(exclusionValues);

        if (versionRangeTextValue == null || versionRangeTextValue.Length == 0)
        {
            versionRangeText = "";
            versionRangeSpecified = false;
            versionRange = null;
        }
        else
        {
            VersionRangeParseResult result = MavenRangeParser.Parse(versionRangeTextValue);
            if (result.IsFailure())
            {
                throw new System.ArgumentException("Invalid Maven dependency version range.");
            }

            versionRangeText = versionRangeTextValue;
            versionRangeSpecified = true;
            versionRange = result.GetRange();
        }
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <param name="versionRangeText">The version range text to process.</param>
    /// <returns>The resulting maven dependency.</returns>
    internal static MavenDependency Create(string groupIdText, string artifactIdText, string versionRangeText)
    {
        return new MavenDependency(
            MavenArtifactCoordinate.CreateJar(groupIdText, artifactIdText),
            versionRangeText,
            MavenDependencyScope.Compile,
            false,
            new MavenDependencyExclusion[0]);
    }

    /// <summary>
    /// Creates an unversioned.
    /// </summary>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <returns>The resulting maven dependency.</returns>
    internal static MavenDependency CreateUnversioned(string groupIdText, string artifactIdText)
    {
        return new MavenDependency(
            MavenArtifactCoordinate.CreateJar(groupIdText, artifactIdText),
            null,
            MavenDependencyScope.Compile,
            false,
            new MavenDependencyExclusion[0]);
    }

    /// <summary>
    /// Gets a coordinate.
    /// </summary>
    /// <returns>The resulting maven artifact coordinate.</returns>
    internal MavenArtifactCoordinate GetCoordinate()
    {
        return coordinate;
    }

    /// <summary>
    /// Gets a group id.
    /// </summary>
    /// <returns>The resulting maven group id.</returns>
    internal MavenGroupId GetGroupId()
    {
        return coordinate.GetGroupId();
    }

    /// <summary>
    /// Gets an artifact id.
    /// </summary>
    /// <returns>The resulting maven artifact id.</returns>
    internal MavenArtifactId GetArtifactId()
    {
        return coordinate.GetArtifactId();
    }

    /// <summary>
    /// Gets a packaging.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetPackaging()
    {
        return coordinate.GetPackaging();
    }

    /// <summary>
    /// Reports whether this value has classifier.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasClassifier()
    {
        return coordinate.HasClassifier();
    }

    /// <summary>
    /// Gets a classifier.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetClassifier()
    {
        return coordinate.GetClassifier();
    }

    /// <summary>
    /// Reports whether this value has version range.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasVersionRange()
    {
        return versionRangeSpecified;
    }

    /// <summary>
    /// Gets a version range text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionRangeText()
    {
        return versionRangeText;
    }

    /// <summary>
    /// Gets a version range.
    /// </summary>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal VersionRange GetVersionRange()
    {
        if (versionRange == null)
        {
            throw new System.InvalidOperationException("Maven dependency does not contain a version range.");
        }

        return versionRange;
    }

    /// <summary>
    /// Gets a scope.
    /// </summary>
    /// <returns>The resulting maven dependency scope.</returns>
    internal MavenDependencyScope GetScope()
    {
        return scope;
    }

    /// <summary>
    /// Reports whether optional.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsOptional()
    {
        return optional;
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
    /// <returns>The resulting maven dependency exclusion.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal MavenDependencyExclusion GetExclusion(int exclusionIndex)
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
    internal MavenDependencyExclusion[] GetExclusions()
    {
        return CopyExclusions(exclusions);
    }

    /// <summary>
    /// Reports whether version text.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsVersionText(string versionText)
    {
        if (versionRange == null)
        {
            return true;
        }

        return versionRange.ContainsText(versionText, VersionDialect.Maven);
    }

    /// <summary>
    /// Reports whether this dependency or exclusion removes the supplied candidate.
    /// </summary>
    /// <param name="dependencyCoordinate">The dependency coordinate used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool Excludes(MavenArtifactCoordinate dependencyCoordinate)
    {
        int index = 0;

        while (index < exclusions.Length)
        {
            if (exclusions[index].Matches(dependencyCoordinate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static MavenDependencyExclusion[] CopyExclusions(MavenDependencyExclusion[] exclusionValues)
    {
        MavenDependencyExclusion[] copy = new MavenDependencyExclusion[exclusionValues.Length];
        int index = 0;

        while (index < exclusionValues.Length)
        {
            if (exclusionValues[index] == null)
            {
                throw new System.ArgumentException("Maven dependency exclusion arrays cannot contain null values.");
            }

            copy[index] = exclusionValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateScope(MavenDependencyScope scopeValue)
    {
        if (scopeValue == MavenDependencyScope.Compile)
        {
            return;
        }

        if (scopeValue == MavenDependencyScope.Provided)
        {
            return;
        }

        if (scopeValue == MavenDependencyScope.Runtime)
        {
            return;
        }

        if (scopeValue == MavenDependencyScope.Test)
        {
            return;
        }

        if (scopeValue == MavenDependencyScope.System)
        {
            return;
        }

        if (scopeValue == MavenDependencyScope.Import)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("scopeValue");
    }
}
