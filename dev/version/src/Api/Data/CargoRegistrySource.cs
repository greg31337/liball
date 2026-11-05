namespace version;

/// <summary>
/// Represents a cargo registry source.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoRegistrySource
{
    /// <summary>
    /// Stores the <c>name</c> value.
    /// </summary>
    public string name;
    /// <summary>
    /// Stores the <c>indexUrl</c> value.
    /// </summary>
    public string indexUrl;

    /// <summary>
    /// Initializes a new Cargo Registry Source instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="nameValue">The name value used by the operation.</param>
    /// <param name="indexUrlValue">The index url value used by the operation.</param>
    public CargoRegistrySource(string nameValue, string indexUrlValue)
    {
        CargoPackageName.ValidateName(nameValue, "Cargo registry name cannot be empty or contain invalid characters.");
        ValidateNonEmpty(indexUrlValue, "Cargo registry index URL cannot be empty.");

        name = nameValue;
        indexUrl = indexUrlValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="name">The name used by the operation.</param>
    /// <param name="indexUrl">The index url used by the operation.</param>
    /// <returns>The resulting cargo registry source.</returns>
    internal static CargoRegistrySource Create(string name, string indexUrl)
    {
        return new CargoRegistrySource(name, indexUrl);
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
    /// Gets an index url.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetIndexUrl()
    {
        return indexUrl;
    }

    /// <summary>
    /// Reports whether this value matches the supplied normalized name.
    /// </summary>
    /// <param name="nameText">The name text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool MatchesName(string nameText)
    {
        System.ArgumentNullException.ThrowIfNull(nameText);

        return name == nameText;
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
