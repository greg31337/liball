namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifReadResult"/>.</summary>
public interface IAvifReadResult
{
    /// <summary>Returns the inspection outcome.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the parsed document, including findings on failure.</summary>
    /// <returns>The document, or null when no source could be read.</returns>
    AvifDocument? GetDocument();

    /// <summary>Returns detected document features and their supported operations.</summary>
    /// <returns>The capability report, which is empty when no document is available.</returns>
    AvifCapabilityReport GetCapabilities();
}
