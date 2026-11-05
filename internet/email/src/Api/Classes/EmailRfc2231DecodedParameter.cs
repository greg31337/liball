using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a rfc 2231 decoded parameter.
/// </summary>
public class EmailRfc2231DecodedParameter : IEmailRfc2231DecodedParameterContract
{
    string name;
    byte[] decodedBytes;
    string? charset;
    bool charsetPresent;
    string? language;
    bool languagePresent;
    bool extended;
    bool continued;
    int segmentCount;
    List<EmailRfc2231Parameter> sourceParameters;
    /// <summary>
    /// Creates a new rfc 2231 decoded parameter.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "decodedBytes">The source bytes or data to process.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    /// <param name = "charsetPresent">Whether to enable the charset present behavior.</param>
    /// <param name = "language">The language used by the operation.</param>
    /// <param name = "languagePresent">Whether to enable the language present behavior.</param>
    /// <param name = "extended">Whether to enable the extended behavior.</param>
    /// <param name = "continued">Whether to enable the continued behavior.</param>
    /// <param name = "segmentCount">The number of items or bytes to process.</param>
    /// <param name = "sourceParameters">The source parameters used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRfc2231DecodedParameter(string name, byte[] decodedBytes, string? charset, bool charsetPresent, string? language, bool languagePresent, bool extended, bool continued, int segmentCount, List<EmailRfc2231Parameter> sourceParameters)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Decoded parameter names cannot be empty.", "name");
        }

        if (decodedBytes == null)
        {
            throw new ArgumentNullException("decodedBytes");
        }

        if (charsetPresent && charset == null)
        {
            throw new ArgumentNullException("charset");
        }

        if (!charsetPresent && charset != null)
        {
            throw new ArgumentException("A charset value requires the charset-present flag.", "charset");
        }

        if (languagePresent && language == null)
        {
            throw new ArgumentNullException("language");
        }

        if (!languagePresent && language != null)
        {
            throw new ArgumentException("A language value requires the language-present flag.", "language");
        }

        if (segmentCount < 1)
        {
            throw new ArgumentOutOfRangeException("segmentCount");
        }

        if (sourceParameters == null)
        {
            throw new ArgumentNullException("sourceParameters");
        }

        this.name = name;
        this.decodedBytes = new byte[decodedBytes.Length];
        decodedBytes.CopyTo(this.decodedBytes, 0);
        this.charset = charset;
        this.charsetPresent = charsetPresent;
        this.language = language;
        this.languagePresent = languagePresent;
        this.extended = extended;
        this.continued = continued;
        this.segmentCount = segmentCount;
        this.sourceParameters = new List<EmailRfc2231Parameter>(sourceParameters);
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets the decoded bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current decoded bytes.</returns>
    public byte[] GetDecodedBytes()
    {
        byte[] copy = new byte[decodedBytes.Length];
        decodedBytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    public long GetDecodedByteCount()
    {
        return decodedBytes.Length;
    }

    /// <summary>
    /// Determines whether the charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the charset is available; otherwise, <c>false</c>.</returns>
    public bool HasCharset()
    {
        return charsetPresent;
    }

    /// <summary>
    /// Gets the charset.
    /// </summary>
    /// <returns>The current charset.</returns>
    public string? GetCharset()
    {
        return charset;
    }

    /// <summary>
    /// Determines whether the language is present or available.
    /// </summary>
    /// <returns><c>true</c> when the language is available; otherwise, <c>false</c>.</returns>
    public bool HasLanguage()
    {
        return languagePresent;
    }

    /// <summary>
    /// Gets the language.
    /// </summary>
    /// <returns>The current language.</returns>
    public string? GetLanguage()
    {
        return language;
    }

    /// <summary>
    /// Determines whether the current value or state is extended.
    /// </summary>
    /// <returns><c>true</c> when the value or state is extended; otherwise, <c>false</c>.</returns>
    public bool IsExtended()
    {
        return extended;
    }

    /// <summary>
    /// Determines whether the current value or state is continued.
    /// </summary>
    /// <returns><c>true</c> when the value or state is continued; otherwise, <c>false</c>.</returns>
    public bool IsContinued()
    {
        return continued;
    }

    /// <summary>
    /// Gets the segment count.
    /// </summary>
    /// <returns>The current segment count.</returns>
    public int GetSegmentCount()
    {
        return segmentCount;
    }

    /// <summary>
    /// Gets the source parameter count.
    /// </summary>
    /// <returns>The current source parameter count.</returns>
    public int GetSourceParameterCount()
    {
        return sourceParameters.Count;
    }

    /// <summary>
    /// Gets the source parameter.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current source parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRfc2231Parameter GetSourceParameter(int index)
    {
        if (index < 0 || index >= sourceParameters.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return sourceParameters[index];
    }

    /// <summary>
    /// Gets the source parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current source parameters.</returns>
    public List<EmailRfc2231Parameter> GetSourceParameters()
    {
        return new List<EmailRfc2231Parameter>(sourceParameters);
    }
}
