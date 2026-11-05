using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextSearchMatch"/>.
/// </summary>
public interface IEmailTextSearchMatchContract
{
    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    MimeEntity GetEntity();
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    EmailPath GetPath();
    /// <summary>
    /// Gets the character offset.
    /// </summary>
    /// <returns>The current character offset.</returns>
    int GetCharacterOffset();
    /// <summary>
    /// Gets the character length.
    /// </summary>
    /// <returns>The current character length.</returns>
    int GetCharacterLength();
    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <returns>The current context.</returns>
    string GetContext();
    /// <summary>
    /// Determines whether the current value or state is from HTML.
    /// </summary>
    /// <returns><c>true</c> when the value or state is from HTML; otherwise, <c>false</c>.</returns>
    bool IsFromHtml();
}
