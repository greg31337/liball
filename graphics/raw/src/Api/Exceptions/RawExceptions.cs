namespace raw;

/// <summary>Represents a general failure reported by the raw library.</summary>
public class RawException : System.Exception
{
    /// <summary>The machine-readable failure identifier.</summary>
    public RawDiagnosticCode Code;
    /// <summary>The associated byte offset, or <c>-1</c> when no offset applies.</summary>
    public long Offset;

    /// <summary>Creates a general raw failure with an optional underlying exception.</summary>
    /// <param name="code">The machine-readable identifier.</param>
    /// <param name="message">The failure description.</param>
    /// <param name="offset">The byte offset, or <c>-1</c>.</param>
    /// <param name="innerException">The underlying exception, or <c>null</c>.</param>
    public RawException(RawDiagnosticCode code, string message, long offset, System.Exception? innerException) : base(message, innerException)
    {
        Code = code;
        Offset = offset;
    }

    /// <summary>Creates a general raw failure without an underlying exception.</summary>
    /// <param name="code">The machine-readable identifier.</param>
    /// <param name="message">The failure description.</param>
    /// <param name="offset">The byte offset, or <c>-1</c>.</param>
    /// <returns>The created failure.</returns>
    internal static RawException Create(RawDiagnosticCode code, string message, long offset)
    {
        return new RawException(code, message, offset, null);
    }
}

/// <summary>Represents malformed, truncated, or inconsistent input.</summary>
public class RawFormatException : System.Exception
{
    /// <summary>The machine-readable failure identifier.</summary>
    public RawDiagnosticCode Code;
    /// <summary>The associated byte offset, or <c>-1</c> when no offset applies.</summary>
    public long Offset;

    /// <summary>Creates an input-format failure.</summary>
    /// <param name="code">The machine-readable identifier.</param>
    /// <param name="message">The failure description.</param>
    /// <param name="offset">The byte offset, or <c>-1</c>.</param>
    public RawFormatException(RawDiagnosticCode code, string message, long offset) : base(message)
    {
        Code = code;
        Offset = offset;
    }

}

/// <summary>Represents an operation stopped by a configured resource limit.</summary>
public class RawLimitException : System.Exception
{
    /// <summary>The machine-readable failure identifier.</summary>
    public RawDiagnosticCode Code;
    /// <summary>The associated byte offset, or <c>-1</c> when no offset applies.</summary>
    public long Offset;

    /// <summary>Creates a resource-limit failure.</summary>
    /// <param name="code">The machine-readable identifier.</param>
    /// <param name="message">The failure description.</param>
    /// <param name="offset">The byte offset, or <c>-1</c>.</param>
    public RawLimitException(RawDiagnosticCode code, string message, long offset) : base(message)
    {
        Code = code;
        Offset = offset;
    }

}

/// <summary>Represents a recognized operation that is not supported.</summary>
public class RawUnsupportedException : System.Exception
{
    /// <summary>The machine-readable failure identifier.</summary>
    public RawDiagnosticCode Code;
    /// <summary>The associated byte offset, or <c>-1</c> when no offset applies.</summary>
    public long Offset;

    /// <summary>Creates an unsupported-operation failure.</summary>
    /// <param name="code">The machine-readable identifier.</param>
    /// <param name="message">The failure description.</param>
    /// <param name="offset">The byte offset, or <c>-1</c>.</param>
    public RawUnsupportedException(RawDiagnosticCode code, string message, long offset) : base(message)
    {
        Code = code;
        Offset = offset;
    }

}

/// <summary>Represents a missing, ambiguous, disabled, or invalid profile.</summary>
public class RawProfileException : System.Exception
{
    /// <summary>The machine-readable failure identifier.</summary>
    public RawDiagnosticCode Code;
    /// <summary>The associated byte offset, or <c>-1</c> when no offset applies.</summary>
    public long Offset;

    /// <summary>Creates a profile failure.</summary>
    /// <param name="code">The machine-readable identifier.</param>
    /// <param name="message">The failure description.</param>
    /// <param name="offset">The byte offset, or <c>-1</c>.</param>
    public RawProfileException(RawDiagnosticCode code, string message, long offset) : base(message)
    {
        Code = code;
        Offset = offset;
    }

}

/// <summary>Represents a planning, serialization, or output-validation failure.</summary>
public class RawWriteException : System.Exception
{
    /// <summary>The machine-readable failure identifier.</summary>
    public RawDiagnosticCode Code;
    /// <summary>The associated byte offset, or <c>-1</c> when no offset applies.</summary>
    public long Offset;

    /// <summary>Creates a write failure.</summary>
    /// <param name="code">The machine-readable identifier.</param>
    /// <param name="message">The failure description.</param>
    /// <param name="offset">The byte offset, or <c>-1</c>.</param>
    public RawWriteException(RawDiagnosticCode code, string message, long offset) : base(message)
    {
        Code = code;
        Offset = offset;
    }

}
