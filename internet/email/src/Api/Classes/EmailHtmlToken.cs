using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a HTML token.
/// </summary>
public class EmailHtmlToken : IEmailHtmlTokenContract
{
    EmailHtmlTokenKind kind;
    string name;
    string data;
    List<EmailHtmlAttribute> attributes;
    bool selfClosing;
    bool rawText;
    bool malformed;
    int sourceOffset;
    int sourceLength;
    /// <summary>
    /// Creates a new HTML token.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "attributes">The attributes used by the operation.</param>
    /// <param name = "selfClosing">Whether to enable the self closing behavior.</param>
    /// <param name = "rawText">Whether to enable the raw text behavior.</param>
    /// <param name = "malformed">Whether to enable the malformed behavior.</param>
    /// <param name = "sourceOffset">The zero-based source offset.</param>
    /// <param name = "sourceLength">The source length used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlToken(EmailHtmlTokenKind kind, string name, string data, List<EmailHtmlAttribute> attributes, bool selfClosing, bool rawText, bool malformed, int sourceOffset, int sourceLength)
    {
        if (kind < EmailHtmlTokenKind.Text || kind > EmailHtmlTokenKind.Doctype)
            throw new ArgumentOutOfRangeException("kind");
        if (name == null)
            throw new ArgumentNullException("name");
        if (data == null)
            throw new ArgumentNullException("data");
        if (attributes == null)
            throw new ArgumentNullException("attributes");
        if (sourceOffset < 0)
            throw new ArgumentOutOfRangeException("sourceOffset");
        if (sourceLength < 1)
            throw new ArgumentOutOfRangeException("sourceLength");
        if ((kind == EmailHtmlTokenKind.StartTag || kind == EmailHtmlTokenKind.EndTag) && name.Length == 0)
            throw new ArgumentException("Tag tokens require a name.", "name");
        this.kind = kind;
        this.name = name;
        this.data = data;
        this.attributes = new List<EmailHtmlAttribute>(attributes);
        this.selfClosing = selfClosing;
        this.rawText = rawText;
        this.malformed = malformed;
        this.sourceOffset = sourceOffset;
        this.sourceLength = sourceLength;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailHtmlTokenKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <returns>The current data.</returns>
    public string GetData()
    {
        return data;
    }

    /// <summary>
    /// Gets the attribute count.
    /// </summary>
    /// <returns>The current attribute count.</returns>
    public int GetAttributeCount()
    {
        return attributes.Count;
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current attribute.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlAttribute GetAttribute(int index)
    {
        if (index < 0 || index >= attributes.Count)
            throw new ArgumentOutOfRangeException("index");
        return attributes[index];
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    /// <param name = "expectedName">The expected name used by the operation.</param>
    /// <returns>The current attribute.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHtmlAttribute? GetAttributeNamed(string expectedName)
    {
        if (expectedName == null)
            throw new ArgumentNullException("expectedName");
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].GetName(), expectedName, StringComparison.OrdinalIgnoreCase))
                return attributes[index];
            index++;
        }

        return null;
    }

    /// <summary>
    /// Gets the attributes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attributes.</returns>
    public List<EmailHtmlAttribute> GetAttributes()
    {
        return new List<EmailHtmlAttribute>(attributes);
    }

    /// <summary>
    /// Determines whether the current value or state is self closing.
    /// </summary>
    /// <returns><c>true</c> when the value or state is self closing; otherwise, <c>false</c>.</returns>
    public bool IsSelfClosing()
    {
        return selfClosing;
    }

    /// <summary>
    /// Determines whether the current value or state is raw text.
    /// </summary>
    /// <returns><c>true</c> when the value or state is raw text; otherwise, <c>false</c>.</returns>
    public bool IsRawText()
    {
        return rawText;
    }

    /// <summary>
    /// Determines whether the current value or state is malformed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is malformed; otherwise, <c>false</c>.</returns>
    public bool IsMalformed()
    {
        return malformed;
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
    /// Gets the source length.
    /// </summary>
    /// <returns>The current source length.</returns>
    public int GetSourceLength()
    {
        return sourceLength;
    }
}
