using System;

namespace email;
/// <summary>
/// Represents a HTML mutation.
/// </summary>
public class EmailHtmlMutation : IEmailHtmlMutationContract
{
    EmailHtmlMutationKind kind;
    string tagName;
    string attributeName;
    string originalValue;
    string replacementValue;
    int sourceOffset;
    string description;
    /// <summary>
    /// Creates a new HTML mutation.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "tagName">The tag name used by the operation.</param>
    /// <param name = "attributeName">The attribute name used by the operation.</param>
    /// <param name = "originalValue">The original value used by the operation.</param>
    /// <param name = "replacementValue">The replacement value used by the operation.</param>
    /// <param name = "sourceOffset">The zero-based source offset.</param>
    /// <param name = "description">The description used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlMutation(EmailHtmlMutationKind kind, string tagName, string attributeName, string originalValue, string replacementValue, int sourceOffset, string description)
    {
        if (kind < EmailHtmlMutationKind.RemovedElement || kind > EmailHtmlMutationKind.RebalancedMarkup)
            throw new ArgumentOutOfRangeException("kind");
        if (tagName == null)
            throw new ArgumentNullException("tagName");
        if (attributeName == null)
            throw new ArgumentNullException("attributeName");
        if (originalValue == null)
            throw new ArgumentNullException("originalValue");
        if (replacementValue == null)
            throw new ArgumentNullException("replacementValue");
        if (sourceOffset < 0)
            throw new ArgumentOutOfRangeException("sourceOffset");
        if (description == null || description.Length == 0)
            throw new ArgumentException("A mutation description is required.", "description");
        this.kind = kind;
        this.tagName = tagName;
        this.attributeName = attributeName;
        this.originalValue = originalValue;
        this.replacementValue = replacementValue;
        this.sourceOffset = sourceOffset;
        this.description = description;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailHtmlMutationKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the tag name.
    /// </summary>
    /// <returns>The current tag name.</returns>
    public string GetTagName()
    {
        return tagName;
    }

    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    /// <returns>The current attribute name.</returns>
    public string GetAttributeName()
    {
        return attributeName;
    }

    /// <summary>
    /// Gets the original value.
    /// </summary>
    /// <returns>The current original value.</returns>
    public string GetOriginalValue()
    {
        return originalValue;
    }

    /// <summary>
    /// Gets the replacement value.
    /// </summary>
    /// <returns>The current replacement value.</returns>
    public string GetReplacementValue()
    {
        return replacementValue;
    }

    /// <summary>
    /// Gets the source offset.
    /// </summary>
    /// <returns>The current source offset.</returns>
    public int GetSourceOffset()
    {
        return sourceOffset;
    }

    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <returns>The current description.</returns>
    public string GetDescription()
    {
        return description;
    }
}
