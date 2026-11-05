using System;

namespace email;
/// <summary>
/// Represents a text search match.
/// </summary>
public class EmailTextSearchMatch : IEmailTextSearchMatchContract
{
    MimeEntity entity;
    EmailPath path;
    int characterOffset;
    int characterLength;
    string context;
    bool htmlSource;
    /// <summary>
    /// Creates a new text search match.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "characterOffset">The zero-based character offset.</param>
    /// <param name = "characterLength">The character length used by the operation.</param>
    /// <param name = "context">The context used by the operation.</param>
    /// <param name = "htmlSource">Whether to enable the html source behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTextSearchMatch(MimeEntity entity, EmailPath path, int characterOffset, int characterLength, string context, bool htmlSource)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (path == null)
            throw new ArgumentNullException("path");
        if (characterOffset < 0)
            throw new ArgumentOutOfRangeException("characterOffset");
        if (characterLength < 1)
            throw new ArgumentOutOfRangeException("characterLength");
        if (context == null)
            throw new ArgumentNullException("context");
        this.entity = entity;
        this.path = path;
        this.characterOffset = characterOffset;
        this.characterLength = characterLength;
        this.context = context;
        this.htmlSource = htmlSource;
    }

    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    public MimeEntity GetEntity()
    {
        return entity;
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    public EmailPath GetPath()
    {
        return path;
    }

    /// <summary>
    /// Gets the character offset.
    /// </summary>
    /// <returns>The current character offset.</returns>
    public int GetCharacterOffset()
    {
        return characterOffset;
    }

    /// <summary>
    /// Gets the character length.
    /// </summary>
    /// <returns>The current character length.</returns>
    public int GetCharacterLength()
    {
        return characterLength;
    }

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <returns>The current context.</returns>
    public string GetContext()
    {
        return context;
    }

    /// <summary>
    /// Determines whether the current value or state is from HTML.
    /// </summary>
    /// <returns><c>true</c> when the value or state is from HTML; otherwise, <c>false</c>.</returns>
    public bool IsFromHtml()
    {
        return htmlSource;
    }
}
