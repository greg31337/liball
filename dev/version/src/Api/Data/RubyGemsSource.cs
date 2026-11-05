namespace version;

/// <summary>
/// Represents a rubygems source.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class RubyGemsSource
{
    /// <summary>
    /// Stores the <c>name</c> value.
    /// </summary>
    public string name;
    /// <summary>
    /// Stores the <c>uri</c> value.
    /// </summary>
    public string uri;

    /// <summary>
    /// Initializes a new RubyGems Source instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="nameValue">The name value used by the operation.</param>
    /// <param name="uriValue">The uri value used by the operation.</param>
    public RubyGemsSource(string nameValue, string uriValue)
    {
        RubyGemsDependency.ValidateName(nameValue, "RubyGems source name cannot be empty or contain invalid characters.");
        ValidateNonEmpty(uriValue, "RubyGems source URI cannot be empty.");

        name = nameValue;
        uri = uriValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="name">The name used by the operation.</param>
    /// <param name="uri">The uri used by the operation.</param>
    /// <returns>The resulting rubygems source.</returns>
    internal static RubyGemsSource Create(string name, string uri)
    {
        return new RubyGemsSource(name, uri);
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
    /// Gets an uri.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetUri()
    {
        return uri;
    }

    /// <summary>
    /// Reports whether this value matches the supplied normalized name.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool MatchesName(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return name == text;
    }

    internal static void ValidateNonEmpty(string text, string message)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            throw new System.ArgumentException(message);
        }
    }
}
