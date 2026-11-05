namespace argon2;

/// <summary>
/// Represents a malformed or unsupported Argon2 encoded hash.
/// </summary>
public class Argon2FormatException : System.Exception
{
    /// <summary>
    /// Initializes an Argon2 format exception with an error message.
    /// </summary>
    /// <param name="message">The message that describes the encoded-hash error.</param>
    public Argon2FormatException(string message) : base(message)
    {
    }
}
