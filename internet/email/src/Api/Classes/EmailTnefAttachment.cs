using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a TNEF attachment.
/// </summary>
public class EmailTnefAttachment : IEmailTnefAttachmentContract
{
    int index;
    List<EmailTnefAttribute> attributes;
    List<EmailTnefProperty> properties;
    string? filename;
    string? contentType;
    string? contentId;
    string? contentLocation;
    byte[] content;
    bool hasContent;
    int attachMethod;
    int renderingPosition;
    bool hidden;
    EmailTnefParseResult? embeddedMessage;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new TNEF attachment.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "attributes">The attributes used by the operation.</param>
    /// <param name = "properties">The properties used by the operation.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "contentType">The content type used by the operation.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "hasContent">Whether to enable the has content behavior.</param>
    /// <param name = "attachMethod">The attach method used by the operation.</param>
    /// <param name = "renderingPosition">The rendering position used by the operation.</param>
    /// <param name = "hidden">Whether to enable the hidden behavior.</param>
    /// <param name = "embeddedMessage">The embedded message used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTnefAttachment(int index, List<EmailTnefAttribute> attributes, List<EmailTnefProperty> properties, string? filename, string? contentType, string? contentId, string? contentLocation, byte[] content, bool hasContent, int attachMethod, int renderingPosition, bool hidden, EmailTnefParseResult? embeddedMessage, EmailIssueList issues)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException("index");
        if (attributes == null)
            throw new ArgumentNullException("attributes");
        if (properties == null)
            throw new ArgumentNullException("properties");
        if (content == null)
            throw new ArgumentNullException("content");
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.index = index;
        this.attributes = new List<EmailTnefAttribute>(attributes);
        this.properties = new List<EmailTnefProperty>(properties);
        this.filename = filename;
        this.contentType = contentType;
        this.contentId = contentId;
        this.contentLocation = contentLocation;
        this.content = Copy(content);
        this.hasContent = hasContent;
        this.attachMethod = attachMethod;
        this.renderingPosition = renderingPosition;
        this.hidden = hidden;
        this.embeddedMessage = embeddedMessage;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    public int GetIndex()
    {
        return index;
    }

    /// <summary>
    /// Gets the attributes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attributes.</returns>
    public List<EmailTnefAttribute> GetAttributes()
    {
        return new List<EmailTnefAttribute>(attributes);
    }

    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current properties.</returns>
    public List<EmailTnefProperty> GetProperties()
    {
        return new List<EmailTnefProperty>(properties);
    }

    /// <summary>
    /// Determines whether the filename is present or available.
    /// </summary>
    /// <returns><c>true</c> when the filename is available; otherwise, <c>false</c>.</returns>
    public bool HasFilename()
    {
        return filename != null;
    }

    /// <summary>
    /// Gets the filename.
    /// </summary>
    /// <returns>The current filename.</returns>
    public string? GetFilename()
    {
        return filename;
    }

    /// <summary>
    /// Determines whether the content type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content type is available; otherwise, <c>false</c>.</returns>
    public bool HasContentType()
    {
        return contentType != null;
    }

    /// <summary>
    /// Gets the content type.
    /// </summary>
    /// <returns>The current content type.</returns>
    public string? GetContentType()
    {
        return contentType;
    }

    /// <summary>
    /// Determines whether the content ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content ID is available; otherwise, <c>false</c>.</returns>
    public bool HasContentId()
    {
        return contentId != null;
    }

    /// <summary>
    /// Gets the content ID.
    /// </summary>
    /// <returns>The current content ID.</returns>
    public string? GetContentId()
    {
        return contentId;
    }

    /// <summary>
    /// Determines whether the content location is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content location is available; otherwise, <c>false</c>.</returns>
    public bool HasContentLocation()
    {
        return contentLocation != null;
    }

    /// <summary>
    /// Gets the content location.
    /// </summary>
    /// <returns>The current content location.</returns>
    public string? GetContentLocation()
    {
        return contentLocation;
    }

    /// <summary>
    /// Determines whether the content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content is available; otherwise, <c>false</c>.</returns>
    public bool HasContent()
    {
        return hasContent;
    }

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content.</returns>
    public byte[] GetContent()
    {
        return Copy(content);
    }

    /// <summary>
    /// Gets the attach method.
    /// </summary>
    /// <returns>The current attach method.</returns>
    public int GetAttachMethod()
    {
        return attachMethod;
    }

    /// <summary>
    /// Gets the rendering position.
    /// </summary>
    /// <returns>The current rendering position.</returns>
    public int GetRenderingPosition()
    {
        return renderingPosition;
    }

    /// <summary>
    /// Determines whether the current value or state is hidden.
    /// </summary>
    /// <returns><c>true</c> when the value or state is hidden; otherwise, <c>false</c>.</returns>
    public bool IsHidden()
    {
        return hidden;
    }

    /// <summary>
    /// Determines whether the embedded message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the embedded message is available; otherwise, <c>false</c>.</returns>
    public bool HasEmbeddedMessage()
    {
        return embeddedMessage != null && embeddedMessage.HasMessage();
    }

    /// <summary>
    /// Gets the embedded parse result.
    /// </summary>
    /// <returns>The current embedded parse result.</returns>
    public EmailTnefParseResult? GetEmbeddedParseResult()
    {
        return embeddedMessage;
    }

    /// <summary>
    /// Gets the embedded message.
    /// </summary>
    /// <returns>The current embedded message.</returns>
    public EmailTnefMessage? GetEmbeddedMessage()
    {
        return HasEmbeddedMessage() ? (embeddedMessage ?? throw new InvalidOperationException()).GetMessage() : null;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    static byte[] Copy(byte[] source)
    {
        byte[] result = new byte[source.Length];
        source.CopyTo(result, 0);
        return result;
    }
}
