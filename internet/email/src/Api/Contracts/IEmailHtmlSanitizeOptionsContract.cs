using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlSanitizeOptions"/>.
/// </summary>
public interface IEmailHtmlSanitizeOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailHtmlLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailHtmlLimits value);
    /// <summary>
    /// Gets the allow remote resources.
    /// </summary>
    /// <returns><c>true</c> when the allow remote resources setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowRemoteResources();
    /// <summary>
    /// Sets the allow remote resources used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow remote resources behavior.</param>
    void SetAllowRemoteResources(bool value);
    /// <summary>
    /// Gets the allow cid resources.
    /// </summary>
    /// <returns><c>true</c> when the allow cid resources setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowCidResources();
    /// <summary>
    /// Sets the allow cid resources used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow cid resources behavior.</param>
    void SetAllowCidResources(bool value);
    /// <summary>
    /// Gets the allow safe data images.
    /// </summary>
    /// <returns><c>true</c> when the allow safe data images setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowSafeDataImages();
    /// <summary>
    /// Sets the allow safe data images used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow safe data images behavior.</param>
    void SetAllowSafeDataImages(bool value);
    /// <summary>
    /// Gets the preserve comments.
    /// </summary>
    /// <returns><c>true</c> when the preserve comments setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetPreserveComments();
    /// <summary>
    /// Sets the preserve comments used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the preserve comments behavior.</param>
    void SetPreserveComments(bool value);
    /// <summary>
    /// Gets the maximum mutation count.
    /// </summary>
    /// <returns>The current maximum mutation count.</returns>
    int GetMaximumMutationCount();
    /// <summary>
    /// Sets the maximum mutation count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumMutationCount(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailHtmlSanitizeOptions Clone();
}
