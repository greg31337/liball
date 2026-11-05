using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlMutation"/>.
/// </summary>
public interface IEmailHtmlMutationContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailHtmlMutationKind GetKind();
    /// <summary>
    /// Gets the tag name.
    /// </summary>
    /// <returns>The current tag name.</returns>
    string GetTagName();
    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    /// <returns>The current attribute name.</returns>
    string GetAttributeName();
    /// <summary>
    /// Gets the original value.
    /// </summary>
    /// <returns>The current original value.</returns>
    string GetOriginalValue();
    /// <summary>
    /// Gets the replacement value.
    /// </summary>
    /// <returns>The current replacement value.</returns>
    string GetReplacementValue();
    /// <summary>
    /// Gets the source offset.
    /// </summary>
    /// <returns>The current source offset.</returns>
    int GetSourceOffset();
    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <returns>The current description.</returns>
    string GetDescription();
}
