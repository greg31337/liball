using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Stores an ordered, repeatable collection of message header fields.
/// </summary>
public class EmailHeaders : IEmailHeadersContract
{
    List<EmailHeader> headers;
    Action? mutationGuard;
    /// <summary>
    /// Creates a header collection from preserved fields.
    /// </summary>
    public EmailHeaders()
    {
        headers = new List<EmailHeader>();
    }

    internal static EmailHeaders CreateImplementationWith()
    {
        return new EmailHeaders();
    }

    internal static EmailHeaders CreateImplementationWithPreservedHeaders(List<EmailHeader> preservedHeaders)
    {
        if (preservedHeaders == null)
        {
            throw new ArgumentNullException("preservedHeaders");
        }

        EmailHeaders value = new EmailHeaders();
        int index = 0;
        while (index < preservedHeaders.Count)
        {
            if (preservedHeaders[index] == null)
            {
                throw new ArgumentException("A header collection cannot contain null entries.", "preservedHeaders");
            }

            value.headers.Add(preservedHeaders[index]);
            index++;
        }

        return value;
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    public int GetCount()
    {
        return headers.Count;
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current count.</returns>
    public int GetCountNamed(string name)
    {
        ValidateLookupName(name);
        int count = 0;
        int index = 0;
        while (index < headers.Count)
        {
            if (NameEquals(headers[index], name))
            {
                count++;
            }

            index++;
        }

        return count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    public EmailHeader GetAt(int index)
    {
        ValidateExistingIndex(index);
        return headers[index];
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    public List<EmailHeader> GetAll()
    {
        return new List<EmailHeader>(headers);
    }

    /// <summary>
    /// Gets the first.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first.</returns>
    public EmailHeader? GetFirst(string name)
    {
        int index = IndexOfFirst(name);
        return index < 0 ? null : headers[index];
    }

    /// <summary>
    /// Gets the last.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current last.</returns>
    public EmailHeader? GetLast(string name)
    {
        int index = IndexOfLast(name);
        return index < 0 ? null : headers[index];
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current all.</returns>
    public List<EmailHeader> GetAllNamed(string name)
    {
        ValidateLookupName(name);
        List<EmailHeader> matches = new List<EmailHeader>();
        int index = 0;
        while (index < headers.Count)
        {
            if (NameEquals(headers[index], name))
            {
                matches.Add(headers[index]);
            }

            index++;
        }

        return matches;
    }

    /// <summary>
    /// Determines whether the collection contains the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool Contains(string name)
    {
        return IndexOfFirst(name) >= 0;
    }

    /// <summary>
    /// Determines whether the collection contains the supplied value.
    /// </summary>
    /// <param name = "header">The header used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool ContainsHeader(EmailHeader header)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        return IndexOf(header) >= 0;
    }

    /// <summary>
    /// Determines whether the duplicate is present or available.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the duplicate is available; otherwise, <c>false</c>.</returns>
    public bool HasDuplicate(string name)
    {
        return GetCountNamed(name) > 1;
    }

    /// <summary>
    /// Finds the zero-based index of the first, or reports that it is absent.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public int IndexOfFirst(string name)
    {
        ValidateLookupName(name);
        int index = 0;
        while (index < headers.Count)
        {
            if (NameEquals(headers[index], name))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Finds the zero-based index of the last, or reports that it is absent.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public int IndexOfLast(string name)
    {
        ValidateLookupName(name);
        int index = headers.Count - 1;
        while (index >= 0)
        {
            if (NameEquals(headers[index], name))
            {
                return index;
            }

            index--;
        }

        return -1;
    }

    /// <summary>
    /// Finds the zero-based index of the supplied value, or reports that it is absent.
    /// </summary>
    /// <param name = "header">The header used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public int IndexOf(EmailHeader header)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        int index = 0;
        while (index < headers.Count)
        {
            if (object.ReferenceEquals(headers[index], header))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Adds a value to the current collection or model.
    /// </summary>
    /// <param name = "header">The header used by the operation.</param>
    public void Add(EmailHeader header)
    {
        ValidateMutationHeader(header);
        EnsureMutationAllowed();
        headers.Add(header);
    }

    /// <summary>
    /// Inserts a value at the requested position.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "header">The header used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void Insert(int index, EmailHeader header)
    {
        if (index < 0 || index > headers.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        ValidateMutationHeader(header);
        EnsureMutationAllowed();
        headers.Insert(index, header);
    }

    /// <summary>
    /// Replaces the matching at value or values.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "replacement">The replacement used by the operation.</param>
    /// <returns>The header produced by the operation.</returns>
    public EmailHeader ReplaceAt(int index, EmailHeader replacement)
    {
        ValidateExistingIndex(index);
        ValidateMutationHeader(replacement);
        EnsureMutationAllowed();
        EmailHeader previous = headers[index];
        headers[index] = replacement;
        return previous;
    }

    /// <summary>
    /// Replaces the matching first value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "replacement">The replacement used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ReplaceFirst(string name, EmailHeader replacement)
    {
        ValidateMutationHeader(replacement);
        int index = IndexOfFirst(name);
        if (index < 0)
        {
            return false;
        }

        EnsureMutationAllowed();
        headers[index] = replacement;
        return true;
    }

    /// <summary>
    /// Replaces the matching all value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "replacement">The replacement used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public int ReplaceAll(string name, EmailHeader replacement)
    {
        ValidateMutationHeader(replacement);
        List<EmailHeader> replacements = new List<EmailHeader>();
        replacements.Add(replacement);
        return ReplaceAllWithHeaders(name, replacements);
    }

    /// <summary>
    /// Replaces the matching all value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "replacements">The replacements used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public int ReplaceAllWithHeaders(string name, List<EmailHeader> replacements)
    {
        ValidateLookupName(name);
        ValidateReplacements(replacements);
        int first = IndexOfFirst(name);
        if (first < 0)
        {
            return 0;
        }

        EnsureMutationAllowed();
        List<EmailHeader> updated = new List<EmailHeader>();
        int removed = 0;
        int index = 0;
        while (index < headers.Count)
        {
            if (NameEquals(headers[index], name))
            {
                if (removed == 0)
                {
                    int replacementIndex = 0;
                    while (replacementIndex < replacements.Count)
                    {
                        updated.Add(replacements[replacementIndex]);
                        replacementIndex++;
                    }
                }

                removed++;
            }
            else
            {
                updated.Add(headers[index]);
            }

            index++;
        }

        headers = updated;
        return removed;
    }

    /// <summary>
    /// Removes the matching at value or values.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The header produced by the operation.</returns>
    public EmailHeader RemoveAt(int index)
    {
        ValidateExistingIndex(index);
        EnsureMutationAllowed();
        EmailHeader removed = headers[index];
        headers.RemoveAt(index);
        return removed;
    }

    /// <summary>
    /// Removes the matching first value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The header produced by the operation.</returns>
    public EmailHeader? RemoveFirst(string name)
    {
        int index = IndexOfFirst(name);
        if (index < 0)
        {
            return null;
        }

        return RemoveAt(index);
    }

    /// <summary>
    /// Removes the matching all value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailHeader> RemoveAll(string name)
    {
        ValidateLookupName(name);
        List<EmailHeader> removed = new List<EmailHeader>();
        List<EmailHeader> retained = new List<EmailHeader>();
        int index = 0;
        while (index < headers.Count)
        {
            if (NameEquals(headers[index], name))
            {
                removed.Add(headers[index]);
            }
            else
            {
                retained.Add(headers[index]);
            }

            index++;
        }

        if (removed.Count > 0)
        {
            EnsureMutationAllowed();
            headers = retained;
        }

        return removed;
    }

    /// <summary>
    /// Moves the supplied value.
    /// </summary>
    /// <param name = "fromIndex">The zero-based from index.</param>
    /// <param name = "toIndex">The zero-based to index.</param>
    public void Move(int fromIndex, int toIndex)
    {
        ValidateExistingIndex(fromIndex);
        ValidateExistingIndex(toIndex);
        if (fromIndex == toIndex)
        {
            return;
        }

        EnsureMutationAllowed();
        EmailHeader moved = headers[fromIndex];
        headers.RemoveAt(fromIndex);
        headers.Insert(toIndex, moved);
    }

    /// <summary>
    /// Moves the first.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "toIndex">The zero-based to index.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool MoveFirst(string name, int toIndex)
    {
        ValidateExistingIndex(toIndex);
        int fromIndex = IndexOfFirst(name);
        if (fromIndex < 0)
        {
            return false;
        }

        Move(fromIndex, toIndex);
        return true;
    }

    /// <summary>
    /// Swaps the supplied value.
    /// </summary>
    /// <param name = "firstIndex">The zero-based first index.</param>
    /// <param name = "secondIndex">The zero-based second index.</param>
    public void Swap(int firstIndex, int secondIndex)
    {
        ValidateExistingIndex(firstIndex);
        ValidateExistingIndex(secondIndex);
        if (firstIndex == secondIndex)
        {
            return;
        }

        EnsureMutationAllowed();
        EmailHeader first = headers[firstIndex];
        headers[firstIndex] = headers[secondIndex];
        headers[secondIndex] = first;
    }

    /// <summary>
    /// Clears all stored items or state.
    /// </summary>
    public void Clear()
    {
        if (headers.Count == 0)
            return;
        EnsureMutationAllowed();
        headers.Clear();
    }

    internal void SetMutationGuard(Action? value)
    {
        mutationGuard = value;
        int index = 0;
        while (index < headers.Count)
        {
            headers[index].SetMutationGuard(value);
            index++;
        }
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailHeaders Clone()
    {
        List<EmailHeader> copies = new List<EmailHeader>();
        int index = 0;
        while (index < headers.Count)
        {
            copies.Add(headers[index].Clone());
            index++;
        }

        return CreateImplementationWithPreservedHeaders(copies);
    }

    static void ValidateReplacements(List<EmailHeader> replacements)
    {
        if (replacements == null)
        {
            throw new ArgumentNullException("replacements");
        }

        int index = 0;
        while (index < replacements.Count)
        {
            ValidateMutationHeader(replacements[index]);
            index++;
        }
    }

    void EnsureMutationAllowed()
    {
        if (mutationGuard != null)
            mutationGuard();
    }

    static void ValidateMutationHeader(EmailHeader header)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        byte[] name = header.GetNameBytes();
        if (name.Length == 0)
        {
            throw new ArgumentException("Headers added through mutation must have a field name.", "header");
        }

        int index = 0;
        while (index < name.Length)
        {
            if (name[index] < 33 || name[index] > 126 || name[index] == 58)
            {
                throw new ArgumentException("A header name contains an unsafe or invalid byte.", "header");
            }

            index++;
        }

        byte[] value = header.GetRawValue();
        index = 0;
        while (index < value.Length)
        {
            if (value[index] == 0)
            {
                throw new ArgumentException("A header value contains a NUL byte.", "header");
            }

            if (value[index] == 13)
            {
                if (index + 2 >= value.Length || value[index + 1] != 10 || value[index + 2] != 32 && value[index + 2] != 9)
                {
                    throw new ArgumentException("A header value contains an unsafe carriage return.", "header");
                }

                index += 2;
            }
            else if (value[index] == 10)
            {
                throw new ArgumentException("A header value contains an unsafe line feed.", "header");
            }

            index++;
        }
    }

    static bool NameEquals(EmailHeader header, string name)
    {
        byte[] headerName = header.GetNameBytes();
        if (headerName.Length != name.Length)
        {
            return false;
        }

        int index = 0;
        while (index < headerName.Length)
        {
            int headerByte = headerName[index];
            int nameCharacter = name[index];
            if (nameCharacter > 127)
            {
                return false;
            }

            if (headerByte >= 65 && headerByte <= 90)
            {
                headerByte += 32;
            }

            if (nameCharacter >= 65 && nameCharacter <= 90)
            {
                nameCharacter += 32;
            }

            if (headerByte != nameCharacter)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static void ValidateLookupName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }
    }

    void ValidateExistingIndex(int index)
    {
        if (index < 0 || index >= headers.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }
    }
}
