namespace version;

/// <summary>
/// Represents a cargo source replacement.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoSourceReplacement
{
    /// <summary>
    /// Stores the <c>sourceName</c> value.
    /// </summary>
    public string sourceName;
    /// <summary>
    /// Stores the <c>replacementSource</c> value.
    /// </summary>
    public CargoRegistrySource replacementSource;

    /// <summary>
    /// Initializes a new Cargo Source Replacement instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="sourceNameValue">The source name value used by the operation.</param>
    /// <param name="replacementSourceValue">The replacement source value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public CargoSourceReplacement(string sourceNameValue, CargoRegistrySource replacementSourceValue)
    {
        CargoPackageName.ValidateName(sourceNameValue, "Cargo source replacement name cannot be empty or contain invalid characters.");
        System.ArgumentNullException.ThrowIfNull(replacementSourceValue);

        sourceName = sourceNameValue;
        replacementSource = replacementSourceValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="sourceName">The source name used by the operation.</param>
    /// <param name="replacementSource">The replacement source used by the operation.</param>
    /// <returns>The resulting cargo source replacement.</returns>
    internal static CargoSourceReplacement Create(string sourceName, CargoRegistrySource replacementSource)
    {
        return new CargoSourceReplacement(sourceName, replacementSource);
    }

    /// <summary>
    /// Gets a source name.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetSourceName()
    {
        return sourceName;
    }

    /// <summary>
    /// Gets a replacement source.
    /// </summary>
    /// <returns>The resulting cargo registry source.</returns>
    internal CargoRegistrySource GetReplacementSource()
    {
        return replacementSource;
    }

    /// <summary>
    /// Reports whether this source replacement applies to the supplied registry name.
    /// </summary>
    /// <param name="sourceNameText">The source name text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ReplacesSource(string sourceNameText)
    {
        System.ArgumentNullException.ThrowIfNull(sourceNameText);

        return sourceName == sourceNameText;
    }
}
