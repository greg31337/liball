using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a TNEF body.
/// </summary>
public class EmailTnefBody : IEmailTnefBodyContract
{
    EmailTnefBodyKind kind;
    string? text;
    byte[] content;
    byte[] originalContent;
    string charset;
    EmailTnefProperty? sourceProperty;
    EmailTnefAttribute? sourceAttribute;
    /// <summary>
    /// Creates a new TNEF body.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "text">The text used by the operation.</param>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "originalContent">The original content used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    /// <param name = "sourceProperty">The source property used by the operation.</param>
    /// <param name = "sourceAttribute">The source attribute used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTnefBody(EmailTnefBodyKind kind, string? text, byte[] content, byte[] originalContent, string charset, EmailTnefProperty? sourceProperty, EmailTnefAttribute? sourceAttribute)
    {
        if (kind < EmailTnefBodyKind.PlainText || kind > EmailTnefBodyKind.RichText)
            throw new ArgumentOutOfRangeException("kind");
        if (content == null)
            throw new ArgumentNullException("content");
        if (originalContent == null)
            throw new ArgumentNullException("originalContent");
        this.kind = kind;
        this.text = text;
        this.content = Copy(content);
        this.originalContent = Copy(originalContent);
        this.charset = charset;
        this.sourceProperty = sourceProperty;
        this.sourceAttribute = sourceAttribute;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailTnefBodyKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Determines whether the text is present or available.
    /// </summary>
    /// <returns><c>true</c> when the text is available; otherwise, <c>false</c>.</returns>
    public bool HasText()
    {
        return text != null;
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    public string? GetText()
    {
        return text;
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
    /// Gets the original content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original content.</returns>
    public byte[] GetOriginalContent()
    {
        return Copy(originalContent);
    }

    /// <summary>
    /// Gets the charset.
    /// </summary>
    /// <returns>The current charset.</returns>
    public string GetCharset()
    {
        return charset;
    }

    /// <summary>
    /// Determines whether the source property is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source property is available; otherwise, <c>false</c>.</returns>
    public bool HasSourceProperty()
    {
        return sourceProperty != null;
    }

    /// <summary>
    /// Gets the source property.
    /// </summary>
    /// <returns>The current source property.</returns>
    public EmailTnefProperty? GetSourceProperty()
    {
        return sourceProperty;
    }

    /// <summary>
    /// Determines whether the source attribute is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source attribute is available; otherwise, <c>false</c>.</returns>
    public bool HasSourceAttribute()
    {
        return sourceAttribute != null;
    }

    /// <summary>
    /// Gets the source attribute.
    /// </summary>
    /// <returns>The current source attribute.</returns>
    public EmailTnefAttribute? GetSourceAttribute()
    {
        return sourceAttribute;
    }

    static byte[] Copy(byte[] source)
    {
        byte[] result = new byte[source.Length];
        source.CopyTo(result, 0);
        return result;
    }
}
