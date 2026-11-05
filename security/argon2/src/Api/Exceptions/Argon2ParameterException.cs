namespace argon2;

/// <summary>
/// Represents an invalid Argon2 caller parameter or parameter combination.
/// </summary>
public class Argon2ParameterException : System.Exception
{
    /// <summary>
    /// Initializes an Argon2 parameter exception with an error message.
    /// </summary>
    /// <param name="message">The message that describes the invalid parameter.</param>
    public Argon2ParameterException(string message) : base(message)
    {
    }
}
