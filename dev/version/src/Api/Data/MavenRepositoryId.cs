namespace version;

/// <summary>
/// Represents a maven repository id.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenRepositoryId
{
    /// <summary>
    /// Stores the <c>value</c> value.
    /// </summary>
    public string value;

    /// <summary>
    /// Initializes a new Maven Repository Id instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="valueText">The value text to process.</param>
    public MavenRepositoryId(string valueText)
    {
        MavenCoordinateValidation.ValidateToken(valueText, "Maven repository id cannot be empty or contain invalid characters.");

        value = valueText;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="valueText">The value text to process.</param>
    /// <returns>The resulting maven repository id.</returns>
    internal static MavenRepositoryId Create(string valueText)
    {
        return new MavenRepositoryId(valueText);
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
    /// Reports whether both identifiers are equal under their ecosystem comparison rules.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool EqualsId(MavenRepositoryId other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return value == other.GetValue();
    }

    /// <summary>
    /// Reports whether this value matches the supplied text under its ecosystem comparison rules.
    /// </summary>
    /// <param name="valueText">The value text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool MatchesText(string valueText)
    {
        return value == valueText;
    }
}
