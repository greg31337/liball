namespace version;

/// <summary>
/// Represents a maven artifact coordinate.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenArtifactCoordinate
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
    /// Stores the <c>packaging</c> value.
    /// </summary>
    public string packaging;
    /// <summary>
    /// Stores the <c>classifier</c> value.
    /// </summary>
    public string classifier;
    /// <summary>
    /// Stores the <c>classifierSpecified</c> value.
    /// </summary>
    public bool classifierSpecified;

    /// <summary>
    /// Initializes a new Maven Artifact Coordinate instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="groupIdValue">The group id value used by the operation.</param>
    /// <param name="artifactIdValue">The artifact id value used by the operation.</param>
    /// <param name="packagingValue">The packaging value used by the operation.</param>
    /// <param name="classifierValue">The classifier value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public MavenArtifactCoordinate(
        MavenGroupId groupIdValue,
        MavenArtifactId artifactIdValue,
        string packagingValue,
        string? classifierValue)
    {
        System.ArgumentNullException.ThrowIfNull(groupIdValue);
        System.ArgumentNullException.ThrowIfNull(artifactIdValue);
        MavenCoordinateValidation.ValidateToken(packagingValue, "Maven packaging cannot be empty or contain invalid characters.");

        groupId = groupIdValue;
        artifactId = artifactIdValue;
        packaging = packagingValue;

        if (classifierValue == null || classifierValue.Length == 0)
        {
            classifier = "";
            classifierSpecified = false;
        }
        else
        {
            MavenCoordinateValidation.ValidateToken(classifierValue, "Maven classifier cannot contain invalid characters.");
            classifier = classifierValue;
            classifierSpecified = true;
        }
    }

    /// <summary>
    /// Creates a jar.
    /// </summary>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <returns>The resulting maven artifact coordinate.</returns>
    internal static MavenArtifactCoordinate CreateJar(string groupIdText, string artifactIdText)
    {
        return new MavenArtifactCoordinate(new MavenGroupId(groupIdText), new MavenArtifactId(artifactIdText), "jar", null);
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <param name="packagingText">The packaging text to process.</param>
    /// <param name="classifierText">The classifier text to process.</param>
    /// <returns>The resulting maven artifact coordinate.</returns>
    internal static MavenArtifactCoordinate Create(
        string groupIdText,
        string artifactIdText,
        string packagingText,
        string? classifierText)
    {
        return new MavenArtifactCoordinate(
            new MavenGroupId(groupIdText),
            new MavenArtifactId(artifactIdText),
            packagingText,
            classifierText);
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
    /// Gets a packaging.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetPackaging()
    {
        return packaging;
    }

    /// <summary>
    /// Reports whether this value has classifier.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasClassifier()
    {
        return classifierSpecified;
    }

    /// <summary>
    /// Gets a classifier.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetClassifier()
    {
        return classifier;
    }

    /// <summary>
    /// Gets a coordinate text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetCoordinateText()
    {
        if (classifierSpecified)
        {
            return groupId.GetValue() + ":" + artifactId.GetValue() + ":" + packaging + ":" + classifier;
        }

        return groupId.GetValue() + ":" + artifactId.GetValue() + ":" + packaging;
    }

    /// <summary>
    /// Reports whether this value has same coordinate.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool HasSameCoordinate(MavenArtifactCoordinate other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return groupId.EqualsId(other.GetGroupId())
            && artifactId.EqualsId(other.GetArtifactId())
            && packaging == other.GetPackaging()
            && classifier == other.GetClassifier()
            && classifierSpecified == other.HasClassifier();
    }
}
