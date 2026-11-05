using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailWriteOptions"/>.
/// </summary>
public interface IEmailWriteOptionsContract
{
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    EmailWriteMode GetMode();
    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMode(EmailWriteMode value);
    /// <summary>
    /// Gets the transport constraint.
    /// </summary>
    /// <returns>The current transport constraint.</returns>
    EmailTransferEncodingConstraint GetTransportConstraint();
    /// <summary>
    /// Sets the transport constraint used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetTransportConstraint(EmailTransferEncodingConstraint value);
    /// <summary>
    /// Gets the allow smtp UTF-8.
    /// </summary>
    /// <returns><c>true</c> when the allow smtp UTF-8 setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowSmtpUtf8();
    /// <summary>
    /// Sets the allow smtp UTF-8 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow smtp UTF-8 behavior.</param>
    void SetAllowSmtpUtf8(bool value);
    /// <summary>
    /// Gets the header write options.
    /// </summary>
    /// <returns>The current header write options.</returns>
    EmailHeaderWriteOptions GetHeaderWriteOptions();
    /// <summary>
    /// Sets the header write options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetHeaderWriteOptions(EmailHeaderWriteOptions value);
    /// <summary>
    /// Gets the deterministic output.
    /// </summary>
    /// <returns><c>true</c> when the deterministic output setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetDeterministicOutput();
    /// <summary>
    /// Sets the deterministic output used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the deterministic output behavior.</param>
    void SetDeterministicOutput(bool value);
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
    /// Gets the include blind recipient headers.
    /// </summary>
    /// <returns><c>true</c> when the include blind recipient headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeBlindRecipientHeaders();
    /// <summary>
    /// Sets the include blind recipient headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include blind recipient headers behavior.</param>
    void SetIncludeBlindRecipientHeaders(bool value);
    /// <summary>
    /// Gets the maximum output bytes.
    /// </summary>
    /// <returns>The current maximum output bytes.</returns>
    long GetMaximumOutputBytes();
    /// <summary>
    /// Sets the maximum output bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumOutputBytes(long value);
    /// <summary>
    /// Gets the maximum nesting depth.
    /// </summary>
    /// <returns>The current maximum nesting depth.</returns>
    int GetMaximumNestingDepth();
    /// <summary>
    /// Sets the maximum nesting depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumNestingDepth(int value);
    /// <summary>
    /// Gets the maximum part count.
    /// </summary>
    /// <returns>The current maximum part count.</returns>
    int GetMaximumPartCount();
    /// <summary>
    /// Sets the maximum part count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumPartCount(int value);
    /// <summary>
    /// Determines whether the current value or state is exact preservation.
    /// </summary>
    /// <returns><c>true</c> when the value or state is exact preservation; otherwise, <c>false</c>.</returns>
    bool IsExactPreservation();
    /// <summary>
    /// Determines whether the current value or state is minimal regeneration.
    /// </summary>
    /// <returns><c>true</c> when the value or state is minimal regeneration; otherwise, <c>false</c>.</returns>
    bool IsMinimalRegeneration();
    /// <summary>
    /// Determines whether the current value or state is canonical.
    /// </summary>
    /// <returns><c>true</c> when the value or state is canonical; otherwise, <c>false</c>.</returns>
    bool IsCanonical();
    /// <summary>
    /// Gets the header write mode.
    /// </summary>
    /// <returns>The current header write mode.</returns>
    EmailHeaderWriteMode GetHeaderWriteMode();
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailWriteOptions Clone();
}
