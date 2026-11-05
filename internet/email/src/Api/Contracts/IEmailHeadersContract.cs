using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaders"/>.
/// </summary>
public interface IEmailHeadersContract
{
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    int GetCount();
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current count.</returns>
    int GetCountNamed(string name);
    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    EmailHeader GetAt(int index);
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    List<EmailHeader> GetAll();
    /// <summary>
    /// Gets the first.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first.</returns>
    EmailHeader? GetFirst(string name);
    /// <summary>
    /// Gets the last.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current last.</returns>
    EmailHeader? GetLast(string name);
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current all.</returns>
    List<EmailHeader> GetAllNamed(string name);
    /// <summary>
    /// Determines whether the collection contains the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool Contains(string name);
    /// <summary>
    /// Determines whether the collection contains the supplied value.
    /// </summary>
    /// <param name = "header">The header used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool ContainsHeader(EmailHeader header);
    /// <summary>
    /// Determines whether the duplicate is present or available.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the duplicate is available; otherwise, <c>false</c>.</returns>
    bool HasDuplicate(string name);
    /// <summary>
    /// Finds the zero-based index of the first, or reports that it is absent.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    int IndexOfFirst(string name);
    /// <summary>
    /// Finds the zero-based index of the last, or reports that it is absent.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    int IndexOfLast(string name);
    /// <summary>
    /// Finds the zero-based index of the supplied value, or reports that it is absent.
    /// </summary>
    /// <param name = "header">The header used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    int IndexOf(EmailHeader header);
    /// <summary>
    /// Adds a value to the current collection or model.
    /// </summary>
    /// <param name = "header">The header used by the operation.</param>
    void Add(EmailHeader header);
    /// <summary>
    /// Inserts a value at the requested position.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "header">The header used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void Insert(int index, EmailHeader header);
    /// <summary>
    /// Replaces the matching at value or values.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "replacement">The replacement used by the operation.</param>
    /// <returns>The header produced by the operation.</returns>
    EmailHeader ReplaceAt(int index, EmailHeader replacement);
    /// <summary>
    /// Replaces the matching first value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "replacement">The replacement used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ReplaceFirst(string name, EmailHeader replacement);
    /// <summary>
    /// Replaces the matching all value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "replacement">The replacement used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    int ReplaceAll(string name, EmailHeader replacement);
    /// <summary>
    /// Replaces the matching all value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "replacements">The replacements used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    int ReplaceAllWithHeaders(string name, List<EmailHeader> replacements);
    /// <summary>
    /// Removes the matching at value or values.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The header produced by the operation.</returns>
    EmailHeader RemoveAt(int index);
    /// <summary>
    /// Removes the matching first value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The header produced by the operation.</returns>
    EmailHeader? RemoveFirst(string name);
    /// <summary>
    /// Removes the matching all value or values.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailHeader> RemoveAll(string name);
    /// <summary>
    /// Moves the supplied value.
    /// </summary>
    /// <param name = "fromIndex">The zero-based from index.</param>
    /// <param name = "toIndex">The zero-based to index.</param>
    void Move(int fromIndex, int toIndex);
    /// <summary>
    /// Moves the first.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "toIndex">The zero-based to index.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool MoveFirst(string name, int toIndex);
    /// <summary>
    /// Swaps the supplied value.
    /// </summary>
    /// <param name = "firstIndex">The zero-based first index.</param>
    /// <param name = "secondIndex">The zero-based second index.</param>
    void Swap(int firstIndex, int secondIndex);
    /// <summary>
    /// Clears all stored items or state.
    /// </summary>
    void Clear();
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailHeaders Clone();
}
