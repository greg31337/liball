using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a signature tag list.
/// </summary>
public class EmailSignatureTagList : IEmailSignatureTagListContract
{
    List<EmailSignatureTag> tags;
    byte[] rawBytes;
    /// <summary>
    /// Creates a new signature tag list.
    /// </summary>
    /// <param name = "tags">The tags used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSignatureTagList(List<EmailSignatureTag> tags, byte[] rawBytes)
    {
        if (tags == null || rawBytes == null)
        {
            throw new ArgumentNullException("tags");
        }

        this.tags = new List<EmailSignatureTag>(tags);
        this.rawBytes = Copy(rawBytes);
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    public int GetCount()
    {
        return tags.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSignatureTag GetAt(int index)
    {
        if (index < 0 || index >= tags.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return tags[index];
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    public List<EmailSignatureTag> GetAll()
    {
        return new List<EmailSignatureTag>(tags);
    }

    /// <summary>
    /// Determines whether the collection contains the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool Contains(string name)
    {
        return GetFirst(name) != null;
    }

    /// <summary>
    /// Gets the first.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSignatureTag? GetFirst(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int index = 0;
        while (index < tags.Count)
        {
            if (string.Equals(tags[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                return tags[index];
            }

            index++;
        }

        return null;
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current all.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public List<EmailSignatureTag> GetAllNamed(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        List<EmailSignatureTag> matches = new List<EmailSignatureTag>();
        int index = 0;
        while (index < tags.Count)
        {
            if (string.Equals(tags[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(tags[index]);
            }

            index++;
        }

        return matches;
    }

    /// <summary>
    /// Determines whether the duplicate is present or available.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the duplicate is available; otherwise, <c>false</c>.</returns>
    public bool HasDuplicate(string name)
    {
        return GetAllNamed(name).Count > 1;
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

    static byte[] Copy(byte[] input)
    {
        byte[] copy = new byte[input.Length];
        input.CopyTo(copy, 0);
        return copy;
    }
}
