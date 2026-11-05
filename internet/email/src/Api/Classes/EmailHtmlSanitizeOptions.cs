using System;

namespace email;
/// <summary>
/// Controls how HTML sanitize operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailHtmlSanitizeOptions : IEmailHtmlSanitizeOptionsContract
{
    /// <summary>
    /// The default maximum mutation count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumMutationCount = 100000;
    EmailHtmlLimits limits;
    bool allowRemoteResources;
    bool allowCidResources;
    bool allowSafeDataImages;
    bool preserveComments;
    int maximumMutationCount;
    /// <summary>
    /// Creates a new HTML sanitize options object.
    /// </summary>
    public EmailHtmlSanitizeOptions()
    {
        limits = new EmailHtmlLimits();
        allowRemoteResources = false;
        allowCidResources = true;
        allowSafeDataImages = false;
        preserveComments = false;
        maximumMutationCount = DefaultMaximumMutationCount;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailHtmlLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailHtmlLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the allow remote resources.
    /// </summary>
    /// <returns><c>true</c> when the allow remote resources setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowRemoteResources()
    {
        return allowRemoteResources;
    }

    /// <summary>
    /// Sets the allow remote resources used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow remote resources behavior.</param>
    public void SetAllowRemoteResources(bool value)
    {
        allowRemoteResources = value;
    }

    /// <summary>
    /// Gets the allow cid resources.
    /// </summary>
    /// <returns><c>true</c> when the allow cid resources setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowCidResources()
    {
        return allowCidResources;
    }

    /// <summary>
    /// Sets the allow cid resources used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow cid resources behavior.</param>
    public void SetAllowCidResources(bool value)
    {
        allowCidResources = value;
    }

    /// <summary>
    /// Gets the allow safe data images.
    /// </summary>
    /// <returns><c>true</c> when the allow safe data images setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowSafeDataImages()
    {
        return allowSafeDataImages;
    }

    /// <summary>
    /// Sets the allow safe data images used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow safe data images behavior.</param>
    public void SetAllowSafeDataImages(bool value)
    {
        allowSafeDataImages = value;
    }

    /// <summary>
    /// Gets the preserve comments.
    /// </summary>
    /// <returns><c>true</c> when the preserve comments setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetPreserveComments()
    {
        return preserveComments;
    }

    /// <summary>
    /// Sets the preserve comments used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the preserve comments behavior.</param>
    public void SetPreserveComments(bool value)
    {
        preserveComments = value;
    }

    /// <summary>
    /// Gets the maximum mutation count.
    /// </summary>
    /// <returns>The current maximum mutation count.</returns>
    public int GetMaximumMutationCount()
    {
        return maximumMutationCount;
    }

    /// <summary>
    /// Sets the maximum mutation count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumMutationCount(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumMutationCount = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailHtmlSanitizeOptions Clone()
    {
        EmailHtmlSanitizeOptions copy = new EmailHtmlSanitizeOptions();
        copy.SetLimits(limits);
        copy.SetAllowRemoteResources(allowRemoteResources);
        copy.SetAllowCidResources(allowCidResources);
        copy.SetAllowSafeDataImages(allowSafeDataImages);
        copy.SetPreserveComments(preserveComments);
        copy.SetMaximumMutationCount(maximumMutationCount);
        return copy;
    }
}
