namespace version;

/// <summary>
/// Represents a maven snapshot version metadata.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenSnapshotVersionMetadata
{
    /// <summary>
    /// Stores the <c>extension</c> value.
    /// </summary>
    public string extension;
    /// <summary>
    /// Stores the <c>classifier</c> value.
    /// </summary>
    public string classifier;
    /// <summary>
    /// Stores the <c>classifierSpecified</c> value.
    /// </summary>
    public bool classifierSpecified;
    /// <summary>
    /// Stores the <c>value</c> value.
    /// </summary>
    public string value;
    /// <summary>
    /// Stores the <c>updated</c> value.
    /// </summary>
    public string updated;

    /// <summary>
    /// Initializes a new Maven Snapshot Version Metadata instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="extensionValue">The extension value used by the operation.</param>
    /// <param name="classifierValue">The classifier value used by the operation.</param>
    /// <param name="valueText">The value text to process.</param>
    /// <param name="updatedValue">The updated value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public MavenSnapshotVersionMetadata(string extensionValue, string? classifierValue, string valueText, string updatedValue)
    {
        MavenCoordinateValidation.ValidateToken(extensionValue, "Maven snapshot version extension cannot be empty or contain invalid characters.");
        System.ArgumentNullException.ThrowIfNull(valueText);
        System.ArgumentNullException.ThrowIfNull(updatedValue);

        VersionParseResult result = MavenVersionParser.Parse(valueText);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid Maven snapshot version value.");
        }

        extension = extensionValue;
        value = valueText;
        updated = updatedValue;

        if (classifierValue == null || classifierValue.Length == 0)
        {
            classifier = "";
            classifierSpecified = false;
        }
        else
        {
            MavenCoordinateValidation.ValidateToken(classifierValue, "Maven snapshot version classifier cannot contain invalid characters.");
            classifier = classifierValue;
            classifierSpecified = true;
        }
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="extension">The extension used by the operation.</param>
    /// <param name="classifier">The classifier used by the operation.</param>
    /// <param name="value">The value used by the operation.</param>
    /// <param name="updated">The updated used by the operation.</param>
    /// <returns>The resulting maven snapshot version metadata.</returns>
    internal static MavenSnapshotVersionMetadata Create(
        string extension,
        string? classifier,
        string value,
        string updated)
    {
        return new MavenSnapshotVersionMetadata(extension, classifier, value, updated);
    }

    /// <summary>
    /// Gets an extension.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetExtension()
    {
        return extension;
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
    /// Gets a value.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetValue()
    {
        return value;
    }

    /// <summary>
    /// Reports whether this value has updated.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasUpdated()
    {
        return updated.Length > 0;
    }

    /// <summary>
    /// Gets an updated.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetUpdated()
    {
        return updated;
    }
}
