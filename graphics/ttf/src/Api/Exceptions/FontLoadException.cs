namespace ttf;

/// <summary>Represents a failure to acquire or load a font source.</summary>
public class FontLoadException : System.Exception
{
    /// <summary>Creates an exception with a message and underlying exception.</summary>
    /// <param name="message">The message that describes the load failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public FontLoadException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates an exception with a deterministic default message.</summary>
    /// <returns>A new load exception.</returns>
    internal static FontLoadException Create()
    {
        return Create("The font source could not be loaded.");
    }

    /// <summary>Creates an exception with a caller-supplied message.</summary>
    /// <param name="message">The message that describes the load failure.</param>
    /// <returns>A new load exception.</returns>
    internal static FontLoadException Create(string message)
    {
        return new FontLoadException(message, null);
    }

    /// <summary>Creates an exception with a message and underlying exception.</summary>
    /// <param name="message">The message that describes the load failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    /// <returns>A new load exception.</returns>
    internal static FontLoadException Create(
        string message,
        System.Exception innerException)
    {
        return new FontLoadException(message, innerException);
    }
}
