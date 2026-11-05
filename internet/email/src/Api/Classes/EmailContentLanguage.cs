using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a content language.
/// </summary>
/// <remarks>
/// Callers should observe the ownership and lifetime methods when the instance retains streams, temporary storage, or source buffers.
/// </remarks>
public class EmailContentLanguage : IEmailContentLanguageContract
{
    List<string> languages;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new content language.
    /// </summary>
    /// <param name = "languages">The languages used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailContentLanguage(List<string> languages, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (languages == null)
        {
            throw new ArgumentNullException("languages");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.languages = new List<string>(languages);
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    public int GetCount()
    {
        return languages.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetAt(int index)
    {
        if (index < 0 || index >= languages.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return languages[index];
    }

    /// <summary>
    /// Gets the normalized at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested normalized.</returns>
    public string GetNormalizedAt(int index)
    {
        return GetAt(index).ToLowerInvariant();
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    public List<string> GetAll()
    {
        return new List<string>(languages);
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
