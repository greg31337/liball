using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArchiveIndexOptions"/>.
/// </summary>
public interface IEmailArchiveIndexOptionsContract
{
    /// <summary>
    /// Gets the selected header name count.
    /// </summary>
    /// <returns>The current selected header name count.</returns>
    int GetSelectedHeaderNameCount();
    /// <summary>
    /// Gets the selected header name at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested selected header name.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    string GetSelectedHeaderNameAt(int index);
    /// <summary>
    /// Gets the selected header names.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current selected header names.</returns>
    List<string> GetSelectedHeaderNames();
    /// <summary>
    /// Adds a selected header name to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddSelectedHeaderName(string value);
    /// <summary>
    /// Removes the matching selected header name value or values.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool RemoveSelectedHeaderName(string value);
    /// <summary>
    /// Clears the stored selected header names value or values.
    /// </summary>
    void ClearSelectedHeaderNames();
    /// <summary>
    /// Determines whether the collection contains selected header name.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ContainsSelectedHeaderName(string value);
    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>The current hash algorithm.</returns>
    EmailHashAlgorithm GetHashAlgorithm();
    /// <summary>
    /// Sets the hash algorithm used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetHashAlgorithm(EmailHashAlgorithm value);
    /// <summary>
    /// Gets the maximum hash bytes.
    /// </summary>
    /// <returns>The current maximum hash bytes.</returns>
    long GetMaximumHashBytes();
    /// <summary>
    /// Sets the maximum hash bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHashBytes(long value);
    /// <summary>
    /// Gets the include semantic hash.
    /// </summary>
    /// <returns><c>true</c> when the include semantic hash setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeSemanticHash();
    /// <summary>
    /// Sets the include semantic hash used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include semantic hash behavior.</param>
    void SetIncludeSemanticHash(bool value);
    /// <summary>
    /// Gets the maximum selected header count.
    /// </summary>
    /// <returns>The current maximum selected header count.</returns>
    int GetMaximumSelectedHeaderCount();
    /// <summary>
    /// Sets the maximum selected header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumSelectedHeaderCount(int value);
    /// <summary>
    /// Gets the maximum selected header characters.
    /// </summary>
    /// <returns>The current maximum selected header characters.</returns>
    int GetMaximumSelectedHeaderCharacters();
    /// <summary>
    /// Sets the maximum selected header characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumSelectedHeaderCharacters(int value);
    /// <summary>
    /// Gets the analysis options.
    /// </summary>
    /// <returns>The current analysis options.</returns>
    EmailMessageAnalysisOptions GetAnalysisOptions();
    /// <summary>
    /// Sets the analysis options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetAnalysisOptions(EmailMessageAnalysisOptions value);
    /// <summary>
    /// Gets the semantic options.
    /// </summary>
    /// <returns>The current semantic options.</returns>
    EmailCanonicalComparisonOptions GetSemanticOptions();
    /// <summary>
    /// Sets the semantic options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetSemanticOptions(EmailCanonicalComparisonOptions value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailArchiveIndexOptions Clone();
}
