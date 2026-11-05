using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailQueryOptions"/>.
/// </summary>
public interface IEmailQueryOptionsContract
{
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
    void SetMaximumEntityCount(int value);
    /// <summary>
    /// Gets the maximum query characters.
    /// </summary>
    /// <returns>The current maximum query characters.</returns>
    int GetMaximumQueryCharacters();
    /// <summary>
    /// Sets the maximum query characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumQueryCharacters(int value);
    /// <summary>
    /// Gets the maximum header count.
    /// </summary>
    /// <returns>The current maximum header count.</returns>
    int GetMaximumHeaderCount();
    /// <summary>
    /// Sets the maximum header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHeaderCount(int value);
    /// <summary>
    /// Gets the maximum field characters.
    /// </summary>
    /// <returns>The current maximum field characters.</returns>
    int GetMaximumFieldCharacters();
    /// <summary>
    /// Sets the maximum field characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumFieldCharacters(int value);
    /// <summary>
    /// Gets the maximum body characters.
    /// </summary>
    /// <returns>The current maximum body characters.</returns>
    int GetMaximumBodyCharacters();
    /// <summary>
    /// Sets the maximum body characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumBodyCharacters(int value);
    /// <summary>
    /// Gets the maximum body bytes.
    /// </summary>
    /// <returns>The current maximum body bytes.</returns>
    long GetMaximumBodyBytes();
    /// <summary>
    /// Sets the maximum body bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumBodyBytes(long value);
    /// <summary>
    /// Gets the maximum query depth.
    /// </summary>
    /// <returns>The current maximum query depth.</returns>
    int GetMaximumQueryDepth();
    /// <summary>
    /// Sets the maximum query depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumQueryDepth(int value);
    /// <summary>
    /// Gets the maximum query node count.
    /// </summary>
    /// <returns>The current maximum query node count.</returns>
    int GetMaximumQueryNodeCount();
    /// <summary>
    /// Sets the maximum query node count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumQueryNodeCount(int value);
    /// <summary>
    /// Gets the maximum children per node.
    /// </summary>
    /// <returns>The current maximum children per node.</returns>
    int GetMaximumChildrenPerNode();
    /// <summary>
    /// Sets the maximum children per node used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumChildrenPerNode(int value);
    /// <summary>
    /// Gets the case sensitive.
    /// </summary>
    /// <returns><c>true</c> when the case sensitive setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetCaseSensitive();
    /// <summary>
    /// Sets the case sensitive used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the case sensitive behavior.</param>
    void SetCaseSensitive(bool value);
    /// <summary>
    /// Gets the include text attachments.
    /// </summary>
    /// <returns><c>true</c> when the include text attachments setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeTextAttachments();
    /// <summary>
    /// Sets the include text attachments used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include text attachments behavior.</param>
    void SetIncludeTextAttachments(bool value);
    /// <summary>
    /// Gets the include HTML.
    /// </summary>
    /// <returns><c>true</c> when the include HTML setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeHtml();
    /// <summary>
    /// Sets the include HTML used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include HTML behavior.</param>
    void SetIncludeHtml(bool value);
    /// <summary>
    /// Gets the require known date zone.
    /// </summary>
    /// <returns><c>true</c> when the require known date zone setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireKnownDateZone();
    /// <summary>
    /// Sets the require known date zone used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require known date zone behavior.</param>
    void SetRequireKnownDateZone(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailQueryOptions Clone();
}
