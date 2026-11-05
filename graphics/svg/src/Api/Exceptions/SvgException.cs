namespace svg;

/// <summary>Represents an unrecoverable public API failure.</summary>
/// <remarks>This class derives directly from <see cref="System.Exception"/>.</remarks>
public class SvgException : System.Exception
{
    /// <summary>The primary diagnostic, or <c>null</c> when none was attached.</summary>
    public SvgDiagnostic? PrimaryDiagnostic;

    /// <summary>The attached diagnostics in insertion order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    /// <summary>Creates an API exception with a message, inner exception, and optional primary diagnostic.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    /// <param name="diagnostic">The primary diagnostic, or <c>null</c>.</param>
    public SvgException(string? message, System.Exception? innerException, SvgDiagnostic? diagnostic) : base(message, innerException)
    {
        PrimaryDiagnostic = diagnostic;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        if (diagnostic != null) Diagnostics.Add(diagnostic);
    }

    /// <summary>Creates an API exception with the default exception message.</summary>
    /// <returns>A new API exception.</returns>
    internal static SvgException Create() { return new SvgException(null, null, null); }

    /// <summary>Creates an API exception with a message.</summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new API exception.</returns>
    internal static SvgException Create(string message) { return new SvgException(message, null, null); }

    /// <summary>Creates an API exception with a message and inner exception.</summary>
    /// <param name="message">The failure message.</param><param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    /// <returns>A new API exception.</returns>
    internal static SvgException Create(string message, System.Exception? innerException) { return new SvgException(message, innerException, null); }

    /// <summary>Attaches another diagnostic to the exception.</summary>
    /// <param name="diagnostic">The diagnostic to attach.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>diagnostic</c> is <c>null</c>.</exception>
    public void AddDiagnostic(SvgDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new System.ArgumentNullException("diagnostic");
        }
        if (PrimaryDiagnostic == null)
        {
            PrimaryDiagnostic = diagnostic;
        }
        Diagnostics.Add(diagnostic);
    }
}

/// <summary>Represents an unrecoverable XML loading or writing failure.</summary>
/// <remarks>This class derives directly from <see cref="System.Exception"/>.</remarks>
public class SvgXmlException : System.Exception
{
    /// <summary>The primary diagnostic, or <c>null</c> when none was attached.</summary>
    public SvgDiagnostic? PrimaryDiagnostic;

    /// <summary>The attached diagnostics in insertion order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    /// <summary>Creates an XML exception with a message, inner exception, and optional primary diagnostic.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    /// <param name="diagnostic">The primary diagnostic, or <c>null</c>.</param>
    public SvgXmlException(string? message, System.Exception? innerException, SvgDiagnostic? diagnostic) : base(message, innerException)
    {
        PrimaryDiagnostic = diagnostic;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        if (diagnostic != null) Diagnostics.Add(diagnostic);
    }

    /// <summary>Creates an XML exception with the default exception message.</summary>
    /// <returns>A new XML exception.</returns>
    internal static SvgXmlException Create() { return new SvgXmlException(null, null, null); }

    /// <summary>Creates an XML exception with a message.</summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new XML exception.</returns>
    internal static SvgXmlException Create(string message) { return new SvgXmlException(message, null, null); }

    /// <summary>Creates an XML exception with a message and inner exception.</summary>
    /// <param name="message">The failure message.</param><param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    /// <returns>A new XML exception.</returns>
    internal static SvgXmlException Create(string message, System.Exception? innerException) { return new SvgXmlException(message, innerException, null); }

    /// <summary>Attaches another diagnostic to the exception.</summary>
    /// <param name="diagnostic">The diagnostic to attach.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>diagnostic</c> is <c>null</c>.</exception>
    public void AddDiagnostic(SvgDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new System.ArgumentNullException("diagnostic");
        }
        if (PrimaryDiagnostic == null)
        {
            PrimaryDiagnostic = diagnostic;
        }
        Diagnostics.Add(diagnostic);
    }
}

/// <summary>Represents an unrecoverable external resource failure.</summary>
/// <remarks>This class derives directly from <see cref="System.Exception"/>.</remarks>
public class SvgResourceException : System.Exception
{
    /// <summary>The primary diagnostic, or <c>null</c> when none was attached.</summary>
    public SvgDiagnostic? PrimaryDiagnostic;

    /// <summary>The attached diagnostics in insertion order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    /// <summary>Creates a resource exception with a message, inner exception, and optional primary diagnostic.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    /// <param name="diagnostic">The primary diagnostic, or <c>null</c>.</param>
    public SvgResourceException(string? message, System.Exception? innerException, SvgDiagnostic? diagnostic) : base(message, innerException)
    {
        PrimaryDiagnostic = diagnostic;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        if (diagnostic != null) Diagnostics.Add(diagnostic);
    }

    /// <summary>Creates a resource exception with the default exception message.</summary>
    /// <returns>A new resource exception.</returns>
    internal static SvgResourceException Create() { return new SvgResourceException(null, null, null); }

    /// <summary>Creates a resource exception with a message.</summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new resource exception.</returns>
    internal static SvgResourceException Create(string message) { return new SvgResourceException(message, null, null); }

    /// <summary>Creates a resource exception with a message and inner exception.</summary>
    /// <param name="message">The failure message.</param><param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    /// <returns>A new resource exception.</returns>
    internal static SvgResourceException Create(string message, System.Exception? innerException) { return new SvgResourceException(message, innerException, null); }

    /// <summary>Attaches another diagnostic to the exception.</summary>
    /// <param name="diagnostic">The diagnostic to attach.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>diagnostic</c> is <c>null</c>.</exception>
    public void AddDiagnostic(SvgDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new System.ArgumentNullException("diagnostic");
        }
        if (PrimaryDiagnostic == null)
        {
            PrimaryDiagnostic = diagnostic;
        }
        Diagnostics.Add(diagnostic);
    }
}

/// <summary>Represents an unrecoverable rendering failure.</summary>
/// <remarks>This class derives directly from <see cref="System.Exception"/>.</remarks>
public class SvgRenderingException : System.Exception
{
    /// <summary>The primary diagnostic, or <c>null</c> when none was attached.</summary>
    public SvgDiagnostic? PrimaryDiagnostic;

    /// <summary>The attached diagnostics in insertion order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    /// <summary>Creates a rendering exception with a message, inner exception, and optional primary diagnostic.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    /// <param name="diagnostic">The primary diagnostic, or <c>null</c>.</param>
    public SvgRenderingException(string? message, System.Exception? innerException, SvgDiagnostic? diagnostic) : base(message, innerException)
    {
        PrimaryDiagnostic = diagnostic;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        if (diagnostic != null) Diagnostics.Add(diagnostic);
    }

    /// <summary>Creates a rendering exception with the default exception message.</summary>
    /// <returns>A new rendering exception.</returns>
    internal static SvgRenderingException Create() { return new SvgRenderingException(null, null, null); }

    /// <summary>Creates a rendering exception with a message.</summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new rendering exception.</returns>
    internal static SvgRenderingException Create(string message) { return new SvgRenderingException(message, null, null); }

    /// <summary>Creates a rendering exception with a message and inner exception.</summary>
    /// <param name="message">The failure message.</param><param name="innerException">The exception that caused this failure, or <c>null</c>.</param>
    /// <returns>A new rendering exception.</returns>
    internal static SvgRenderingException Create(string message, System.Exception? innerException) { return new SvgRenderingException(message, innerException, null); }

    /// <summary>Attaches another diagnostic to the exception.</summary>
    /// <param name="diagnostic">The diagnostic to attach.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>diagnostic</c> is <c>null</c>.</exception>
    public void AddDiagnostic(SvgDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new System.ArgumentNullException("diagnostic");
        }
        if (PrimaryDiagnostic == null)
        {
            PrimaryDiagnostic = diagnostic;
        }
        Diagnostics.Add(diagnostic);
    }
}
