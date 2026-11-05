namespace version;

/// <summary>
/// Represents a maven dependency exclusion.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenDependencyExclusion
{
    /// <summary>
    /// Stores the <c>groupId</c> value.
    /// </summary>
    public MavenGroupId groupId;
    /// <summary>
    /// Stores the <c>artifactId</c> value.
    /// </summary>
    public MavenArtifactId artifactId;

    /// <summary>
    /// Initializes a new Maven Dependency Exclusion instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="groupIdValue">The group id value used by the operation.</param>
    /// <param name="artifactIdValue">The artifact id value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public MavenDependencyExclusion(MavenGroupId groupIdValue, MavenArtifactId artifactIdValue)
    {
        System.ArgumentNullException.ThrowIfNull(groupIdValue);
        System.ArgumentNullException.ThrowIfNull(artifactIdValue);

        groupId = groupIdValue;
        artifactId = artifactIdValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <returns>The resulting maven dependency exclusion.</returns>
    internal static MavenDependencyExclusion Create(string groupIdText, string artifactIdText)
    {
        return new MavenDependencyExclusion(new MavenGroupId(groupIdText), new MavenArtifactId(artifactIdText));
    }

    /// <summary>
    /// Gets a group id.
    /// </summary>
    /// <returns>The resulting maven group id.</returns>
    internal MavenGroupId GetGroupId()
    {
        return groupId;
    }

    /// <summary>
    /// Gets an artifact id.
    /// </summary>
    /// <returns>The resulting maven artifact id.</returns>
    internal MavenArtifactId GetArtifactId()
    {
        return artifactId;
    }

    /// <summary>
    /// Reports whether this value matches the supplied candidate.
    /// </summary>
    /// <param name="coordinate">The coordinate used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool Matches(MavenArtifactCoordinate coordinate)
    {
        System.ArgumentNullException.ThrowIfNull(coordinate);

        return groupId.EqualsId(coordinate.GetGroupId())
            && artifactId.EqualsId(coordinate.GetArtifactId());
    }
}
