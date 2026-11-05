namespace jpeg;

/// <summary>Represents JPEG data that is malformed, truncated, or structurally inconsistent.</summary>
public class JpegInvalidDataException : System.Exception
{
    /// <summary>Creates an exception that explains the invalid JPEG condition.</summary>
    /// <param name="message">A plain-language description of the problem.</param>
    /// <returns>An exception carrying the supplied message.</returns>
    internal static JpegInvalidDataException Create(string message)
    {
        return new JpegInvalidDataException(message, null);
    }

    /// <summary>Creates an exception with a message and an optional lower-level failure.</summary>
    /// <param name="message">A plain-language description of the problem.</param>
    /// <param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    public JpegInvalidDataException(string message, System.Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>Represents valid JPEG features or requested operations that this library does not implement.</summary>
public class JpegUnsupportedException : System.Exception
{
    /// <summary>Creates an exception that identifies the unsupported feature or operation.</summary>
    /// <param name="message">A plain-language description of the unsupported behavior.</param>
    /// <returns>An exception carrying the supplied message.</returns>
    internal static JpegUnsupportedException Create(string message)
    {
        return new JpegUnsupportedException(message, null);
    }

    /// <summary>Creates an exception with a message and an optional lower-level failure.</summary>
    /// <param name="message">A plain-language description of the unsupported behavior.</param>
    /// <param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    public JpegUnsupportedException(string message, System.Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>Represents JPEG input that exceeds a configured size, count, or allocation limit.</summary>
public class JpegResourceLimitException : System.Exception
{
    /// <summary>Creates an exception that identifies the exceeded resource limit.</summary>
    /// <param name="message">A plain-language description of the exceeded limit.</param>
    /// <returns>An exception carrying the supplied message.</returns>
    internal static JpegResourceLimitException Create(string message)
    {
        return new JpegResourceLimitException(message, null);
    }

    /// <summary>Creates an exception with a message and an optional lower-level failure.</summary>
    /// <param name="message">A plain-language description of the exceeded limit.</param>
    /// <param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    public JpegResourceLimitException(string message, System.Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>Represents image data or write options that cannot produce a valid JPEG stream.</summary>
public class JpegWriteValidationException : System.Exception
{
    /// <summary>Creates an exception that explains why the image or write options are invalid.</summary>
    /// <param name="message">A plain-language description of the validation failure.</param>
    /// <returns>An exception carrying the supplied message.</returns>
    internal static JpegWriteValidationException Create(string message)
    {
        return new JpegWriteValidationException(message, null);
    }

    /// <summary>Creates an exception with a message and an optional lower-level failure.</summary>
    /// <param name="message">A plain-language description of the validation failure.</param>
    /// <param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    public JpegWriteValidationException(string message, System.Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>Represents a failure while reading or writing JPEG data through a file or stream.</summary>
public class JpegIOException : System.Exception
{
    /// <summary>Creates an exception that explains the failed JPEG I/O operation.</summary>
    /// <param name="message">A deterministic description of the failed operation.</param>
    /// <param name="innerException">The platform I/O exception that caused the failure, or <c>null</c>.</param>
    public JpegIOException(string message, System.Exception? innerException) : base(message, innerException)
    {
    }
}
