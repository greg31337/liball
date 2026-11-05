using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefAttachment"/>.
/// </summary>
public interface IEmailTnefAttachmentContract
{
    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    int GetIndex();
    /// <summary>
    /// Gets the attributes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attributes.</returns>
    List<EmailTnefAttribute> GetAttributes();
    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current properties.</returns>
    List<EmailTnefProperty> GetProperties();
    /// <summary>
    /// Determines whether the filename is present or available.
    /// </summary>
    /// <returns><c>true</c> when the filename is available; otherwise, <c>false</c>.</returns>
    bool HasFilename();
    /// <summary>
    /// Gets the filename.
    /// </summary>
    /// <returns>The current filename.</returns>
    string? GetFilename();
    /// <summary>
    /// Determines whether the content type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content type is available; otherwise, <c>false</c>.</returns>
    bool HasContentType();
    /// <summary>
    /// Gets the content type.
    /// </summary>
    /// <returns>The current content type.</returns>
    string? GetContentType();
    /// <summary>
    /// Determines whether the content ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content ID is available; otherwise, <c>false</c>.</returns>
    bool HasContentId();
    /// <summary>
    /// Gets the content ID.
    /// </summary>
    /// <returns>The current content ID.</returns>
    string? GetContentId();
    /// <summary>
    /// Determines whether the content location is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content location is available; otherwise, <c>false</c>.</returns>
    bool HasContentLocation();
    /// <summary>
    /// Gets the content location.
    /// </summary>
    /// <returns>The current content location.</returns>
    string? GetContentLocation();
    /// <summary>
    /// Determines whether the content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content is available; otherwise, <c>false</c>.</returns>
    bool HasContent();
    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content.</returns>
    byte[] GetContent();
    /// <summary>
    /// Gets the attach method.
    /// </summary>
    /// <returns>The current attach method.</returns>
    int GetAttachMethod();
    /// <summary>
    /// Gets the rendering position.
    /// </summary>
    /// <returns>The current rendering position.</returns>
    int GetRenderingPosition();
    /// <summary>
    /// Determines whether the current value or state is hidden.
    /// </summary>
    /// <returns><c>true</c> when the value or state is hidden; otherwise, <c>false</c>.</returns>
    bool IsHidden();
    /// <summary>
    /// Determines whether the embedded message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the embedded message is available; otherwise, <c>false</c>.</returns>
    bool HasEmbeddedMessage();
    /// <summary>
    /// Gets the embedded parse result.
    /// </summary>
    /// <returns>The current embedded parse result.</returns>
    EmailTnefParseResult? GetEmbeddedParseResult();
    /// <summary>
    /// Gets the embedded message.
    /// </summary>
    /// <returns>The current embedded message.</returns>
    EmailTnefMessage? GetEmbeddedMessage();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
