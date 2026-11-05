namespace countrycodes;

/// <summary>
/// Reports that no subdivision matches a valid subdivision code.
/// </summary>
public class SubdivisionNotFoundException : global::System.Exception
{
    /// <summary>
    /// The valid subdivision code that was not found.
    /// </summary>
    public string Input;

    /// <summary>
    /// Creates a subdivision-not-found exception with stable lookup context.
    /// </summary>
    /// <param name="input">The valid subdivision code that was not found.</param>
    public SubdivisionNotFoundException(string input)
        : base($"No subdivision was found for code '{input}'.")
    {
        Input = input;
    }
}
