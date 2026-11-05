using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailNormalizationOptions"/>.
/// </summary>
public interface IEmailNormalizationOptionsContract
{
    /// <summary>
    /// Gets the normalize line endings.
    /// </summary>
    /// <returns><c>true</c> when the normalize line endings setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetNormalizeLineEndings();
    /// <summary>
    /// Sets the normalize line endings used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the normalize line endings behavior.</param>
    void SetNormalizeLineEndings(bool value);
    /// <summary>
    /// Gets the refold headers.
    /// </summary>
    /// <returns><c>true</c> when the refold headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRefoldHeaders();
    /// <summary>
    /// Sets the refold headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the refold headers behavior.</param>
    void SetRefoldHeaders(bool value);
    /// <summary>
    /// Gets the repair MIME version.
    /// </summary>
    /// <returns><c>true</c> when the repair MIME version setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRepairMimeVersion();
    /// <summary>
    /// Sets the repair MIME version used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the repair MIME version behavior.</param>
    void SetRepairMimeVersion(bool value);
    /// <summary>
    /// Gets the repair content types.
    /// </summary>
    /// <returns><c>true</c> when the repair content types setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRepairContentTypes();
    /// <summary>
    /// Sets the repair content types used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the repair content types behavior.</param>
    void SetRepairContentTypes(bool value);
    /// <summary>
    /// Gets the repair transfer encodings.
    /// </summary>
    /// <returns><c>true</c> when the repair transfer encodings setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRepairTransferEncodings();
    /// <summary>
    /// Sets the repair transfer encodings used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the repair transfer encodings behavior.</param>
    void SetRepairTransferEncodings(bool value);
    /// <summary>
    /// Gets the repair boundaries.
    /// </summary>
    /// <returns><c>true</c> when the repair boundaries setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRepairBoundaries();
    /// <summary>
    /// Sets the repair boundaries used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the repair boundaries behavior.</param>
    void SetRepairBoundaries(bool value);
    /// <summary>
    /// Gets the normalize charsets.
    /// </summary>
    /// <returns><c>true</c> when the normalize charsets setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetNormalizeCharsets();
    /// <summary>
    /// Sets the normalize charsets used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the normalize charsets behavior.</param>
    void SetNormalizeCharsets(bool value);
    /// <summary>
    /// Gets the normalize filenames.
    /// </summary>
    /// <returns><c>true</c> when the normalize filenames setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetNormalizeFilenames();
    /// <summary>
    /// Sets the normalize filenames used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the normalize filenames behavior.</param>
    void SetNormalizeFilenames(bool value);
    /// <summary>
    /// Gets the preferred header line length.
    /// </summary>
    /// <returns>The current preferred header line length.</returns>
    int GetPreferredHeaderLineLength();
    /// <summary>
    /// Sets the preferred header line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetPreferredHeaderLineLength(int value);
    /// <summary>
    /// Gets the maximum content bytes.
    /// </summary>
    /// <returns>The current maximum content bytes.</returns>
    long GetMaximumContentBytes();
    /// <summary>
    /// Sets the maximum content bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumContentBytes(long value);
    /// <summary>
    /// Gets the deterministic boundary seed.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current deterministic boundary seed.</returns>
    byte[] GetDeterministicBoundarySeed();
    /// <summary>
    /// Sets the deterministic boundary seed used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetDeterministicBoundarySeed(byte[] value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailNormalizationOptions Clone();
}
