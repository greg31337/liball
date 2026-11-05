using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a keyword list.
/// </summary>
public class EmailKeywordList : IEmailKeywordListContract
{
    List<EmailKeyword> keywords;
    byte[] rawBytes;
    /// <summary>
    /// Creates a new keyword list.
    /// </summary>
    /// <param name = "keywords">The keywords used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailKeywordList(List<EmailKeyword> keywords, byte[] rawBytes)
    {
        if (keywords == null)
        {
            throw new ArgumentNullException("keywords");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        this.keywords = new List<EmailKeyword>(keywords);
        this.rawBytes = Copy(rawBytes);
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    public int GetCount()
    {
        return keywords.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailKeyword GetAt(int index)
    {
        if (index < 0 || index >= keywords.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return keywords[index];
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    public List<EmailKeyword> GetAll()
    {
        return new List<EmailKeyword>(keywords);
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

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
