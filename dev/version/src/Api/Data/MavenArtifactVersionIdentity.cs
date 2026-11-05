namespace version;

/// <summary>
/// Represents a maven artifact version identity.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenArtifactVersionIdentity
{
    /// <summary>
    /// Stores the <c>coordinate</c> value.
    /// </summary>
    public MavenArtifactCoordinate coordinate;
    /// <summary>
    /// Stores the <c>version</c> value.
    /// </summary>
    public UniversalVersion version;

    /// <summary>
    /// Initializes a new Maven Artifact Version Identity instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="coordinateValue">The coordinate value used by the operation.</param>
    /// <param name="versionValue">The version value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public MavenArtifactVersionIdentity(MavenArtifactCoordinate coordinateValue, UniversalVersion versionValue)
    {
        System.ArgumentNullException.ThrowIfNull(coordinateValue);
        System.ArgumentNullException.ThrowIfNull(versionValue);

        if (versionValue.GetDialect() != VersionDialect.Maven)
        {
            throw new System.ArgumentException("Maven artifact version identity requires a Maven version.");
        }

        coordinate = coordinateValue;
        version = versionValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>The resulting maven artifact version identity.</returns>
    internal static MavenArtifactVersionIdentity Create(string groupIdText, string artifactIdText, string versionText)
    {
        return Create(groupIdText, artifactIdText, "jar", null, versionText);
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <param name="packagingText">The packaging text to process.</param>
    /// <param name="classifierText">The classifier text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>The resulting maven artifact version identity.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static MavenArtifactVersionIdentity Create(
        string groupIdText,
        string artifactIdText,
        string packagingText,
        string? classifierText,
        string versionText)
    {
        System.ArgumentNullException.ThrowIfNull(versionText);

        VersionParseResult result = MavenVersionParser.Parse(versionText);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid Maven artifact version.");
        }

        return new MavenArtifactVersionIdentity(
            MavenArtifactCoordinate.Create(groupIdText, artifactIdText, packagingText, classifierText),
            result.GetVersion());
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
    /// Gets a version.
    /// </summary>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion GetVersion()
    {
        return version;
    }

    /// <summary>
    /// Gets a version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionText()
    {
        return version.GetOriginalText();
    }

    /// <summary>
    /// Gets an identity text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetIdentityText()
    {
        return coordinate.GetCoordinateText() + ":" + version.GetOriginalText();
    }

    /// <summary>
    /// Reports whether this value has same identity.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool HasSameIdentity(MavenArtifactVersionIdentity other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return coordinate.HasSameCoordinate(other.GetCoordinate())
            && MavenVersionComparer.Compare(version.GetOriginalText(), other.GetVersionText()) == 0;
    }
}
