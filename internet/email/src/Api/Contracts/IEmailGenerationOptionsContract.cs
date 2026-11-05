using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailGenerationOptions"/>.
/// </summary>
public interface IEmailGenerationOptionsContract
{
    /// <summary>
    /// Gets the generate date.
    /// </summary>
    /// <returns><c>true</c> when the generate date setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetGenerateDate();
    /// <summary>
    /// Sets the generate date used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the generate date behavior.</param>
    void SetGenerateDate(bool value);
    /// <summary>
    /// Gets the generate message ID.
    /// </summary>
    /// <returns><c>true</c> when the generate message ID setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetGenerateMessageId();
    /// <summary>
    /// Sets the generate message ID used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the generate message ID behavior.</param>
    void SetGenerateMessageId(bool value);
    /// <summary>
    /// Gets the generate content ids.
    /// </summary>
    /// <returns><c>true</c> when the generate content ids setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetGenerateContentIds();
    /// <summary>
    /// Sets the generate content ids used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the generate content ids behavior.</param>
    void SetGenerateContentIds(bool value);
    /// <summary>
    /// Gets the generate MIME version.
    /// </summary>
    /// <returns><c>true</c> when the generate MIME version setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetGenerateMimeVersion();
    /// <summary>
    /// Sets the generate MIME version used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the generate MIME version behavior.</param>
    void SetGenerateMimeVersion(bool value);
    /// <summary>
    /// Gets the generate boundaries.
    /// </summary>
    /// <returns><c>true</c> when the generate boundaries setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetGenerateBoundaries();
    /// <summary>
    /// Sets the generate boundaries used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the generate boundaries behavior.</param>
    void SetGenerateBoundaries(bool value);
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
    /// Gets the identifier domain.
    /// </summary>
    /// <returns>The current identifier domain.</returns>
    string GetIdentifierDomain();
    /// <summary>
    /// Sets the identifier domain used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetIdentifierDomain(string value);
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
    /// Determines whether the generation date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the generation date is available; otherwise, <c>false</c>.</returns>
    bool HasGenerationDate();
    /// <summary>
    /// Gets the generation date.
    /// </summary>
    /// <returns>The current generation date.</returns>
    EmailDateTime? GetGenerationDate();
    /// <summary>
    /// Sets the generation date used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetGenerationDate(EmailDateTime value);
    /// <summary>
    /// Clears the stored generation date value or values.
    /// </summary>
    void ClearGenerationDate();
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
    EmailGenerationOptions Clone();
}
