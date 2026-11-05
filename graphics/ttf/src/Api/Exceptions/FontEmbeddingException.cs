namespace ttf;

/// <summary>Represents an embedding operation prohibited by rights or policy.</summary>
public class FontEmbeddingException : System.Exception
{
    /// <summary>Creates an exception with a message and underlying exception.</summary>
    /// <param name="message">The message that describes the embedding failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public FontEmbeddingException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates an exception with a deterministic default message.</summary>
    /// <returns>A new embedding exception.</returns>
    internal static FontEmbeddingException Create()
    {
        return Create("The font cannot be embedded under the selected policy.");
    }

    /// <summary>Creates an exception with a caller-supplied message.</summary>
    /// <param name="message">The message that describes the embedding failure.</param>
    /// <returns>A new embedding exception.</returns>
    internal static FontEmbeddingException Create(string message)
    {
        return new FontEmbeddingException(message, null);
    }

    /// <summary>Creates an exception with a message and underlying exception.</summary>
    /// <param name="message">The message that describes the embedding failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    /// <returns>A new embedding exception.</returns>
    internal static FontEmbeddingException Create(
        string message,
        System.Exception innerException)
    {
        return new FontEmbeddingException(message, innerException);
    }
}
