using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCanonicalComparisonOptions"/>.
/// </summary>
public interface IEmailCanonicalComparisonOptionsContract
{
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
    /// Gets the ignore received headers.
    /// </summary>
    /// <returns><c>true</c> when the ignore received headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIgnoreReceivedHeaders();
    /// <summary>
    /// Sets the ignore received headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore received headers behavior.</param>
    void SetIgnoreReceivedHeaders(bool value);
    /// <summary>
    /// Gets the ignore authentication headers.
    /// </summary>
    /// <returns><c>true</c> when the ignore authentication headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIgnoreAuthenticationHeaders();
    /// <summary>
    /// Sets the ignore authentication headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore authentication headers behavior.</param>
    void SetIgnoreAuthenticationHeaders(bool value);
    /// <summary>
    /// Gets the ignore date headers.
    /// </summary>
    /// <returns><c>true</c> when the ignore date headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIgnoreDateHeaders();
    /// <summary>
    /// Sets the ignore date headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore date headers behavior.</param>
    void SetIgnoreDateHeaders(bool value);
    /// <summary>
    /// Gets the ignore message identifiers.
    /// </summary>
    /// <returns><c>true</c> when the ignore message identifiers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIgnoreMessageIdentifiers();
    /// <summary>
    /// Sets the ignore message identifiers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore message identifiers behavior.</param>
    void SetIgnoreMessageIdentifiers(bool value);
    /// <summary>
    /// Gets the ignore subject headers.
    /// </summary>
    /// <returns><c>true</c> when the ignore subject headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIgnoreSubjectHeaders();
    /// <summary>
    /// Sets the ignore subject headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore subject headers behavior.</param>
    void SetIgnoreSubjectHeaders(bool value);
    /// <summary>
    /// Gets the ignore header order.
    /// </summary>
    /// <returns><c>true</c> when the ignore header order setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIgnoreHeaderOrder();
    /// <summary>
    /// Sets the ignore header order used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the ignore header order behavior.</param>
    void SetIgnoreHeaderOrder(bool value);
    /// <summary>
    /// Adds an ignored header name to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddIgnoredHeaderName(string value);
    /// <summary>
    /// Removes the matching ignored header name value or values.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool RemoveIgnoredHeaderName(string value);
    /// <summary>
    /// Clears the stored ignored header names value or values.
    /// </summary>
    void ClearIgnoredHeaderNames();
    /// <summary>
    /// Gets the ignored header name count.
    /// </summary>
    /// <returns>The current ignored header name count.</returns>
    int GetIgnoredHeaderNameCount();
    /// <summary>
    /// Gets the ignored header name at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested ignored header name.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    string GetIgnoredHeaderNameAt(int index);
    /// <summary>
    /// Gets the ignored header names.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current ignored header names.</returns>
    List<string> GetIgnoredHeaderNames();
    /// <summary>
    /// Gets the deterministic seed.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current deterministic seed.</returns>
    byte[] GetDeterministicSeed();
    /// <summary>
    /// Sets the deterministic seed used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetDeterministicSeed(byte[] value);
    /// <summary>
    /// Gets the maximum output bytes.
    /// </summary>
    /// <returns>The current maximum output bytes.</returns>
    long GetMaximumOutputBytes();
    /// <summary>
    /// Sets the maximum output bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumOutputBytes(long value);
    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    int GetMaximumDepth();
    /// <summary>
    /// Sets the maximum depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumDepth(int value);
    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    int GetMaximumEntityCount();
    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumEntityCount(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailCanonicalComparisonOptions Clone();
    /// <summary>
    /// Determines whether the collection contains ignored header name.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ContainsIgnoredHeaderName(string name);
}
