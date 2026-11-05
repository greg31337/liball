namespace version;

/// <summary>
/// Represents a nuget source identity.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetSourceIdentity
{
    /// <summary>
    /// Stores the <c>name</c> value.
    /// </summary>
    public string name;
    /// <summary>
    /// Stores the <c>source</c> value.
    /// </summary>
    public string source;
    /// <summary>
    /// Stores the <c>normalizedName</c> value.
    /// </summary>
    public string normalizedName;
    /// <summary>
    /// Stores the <c>normalizedSource</c> value.
    /// </summary>
    public string normalizedSource;

    /// <summary>
    /// Initializes a new NuGet Source Identity instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="nameText">The name text to process.</param>
    /// <param name="sourceText">The source text to process.</param>
    public NuGetSourceIdentity(string nameText, string sourceText)
    {
        ValidateText(nameText, "NuGet source name cannot be empty.");
        ValidateText(sourceText, "NuGet source location cannot be empty.");

        name = nameText;
        source = sourceText;
        normalizedName = Normalize(nameText);
        normalizedSource = Normalize(sourceText);
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="nameText">The name text to process.</param>
    /// <param name="sourceText">The source text to process.</param>
    /// <returns>The resulting nuget source identity.</returns>
    internal static NuGetSourceIdentity Create(string nameText, string sourceText)
    {
        return new NuGetSourceIdentity(nameText, sourceText);
    }

    /// <summary>
    /// Gets a name.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets a source.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetSource()
    {
        return source;
    }

    /// <summary>
    /// Gets a normalized name.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetNormalizedName()
    {
        return normalizedName;
    }

    /// <summary>
    /// Gets a normalized source.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetNormalizedSource()
    {
        return normalizedSource;
    }

    /// <summary>
    /// Reports whether both package sources have the same identity.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool EqualsSource(NuGetSourceIdentity other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return normalizedName == other.GetNormalizedName()
            && normalizedSource == other.GetNormalizedSource();
    }

    internal static void ValidateText(string text, string message)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            throw new System.ArgumentException(message);
        }
    }

    internal static string Normalize(string text)
    {
        return text.ToLowerInvariant();
    }
}
