using System;

namespace html;
/// <summary>Represents one tokenizer output token together with attributes, flags, and source location.</summary>
public class HtmlToken : IHtmlToken
{
    internal HtmlTokenKind kind;
    internal bool hasName;
    internal string name;
    internal string data;
    internal HtmlAttribute[] attributes;
    internal bool selfClosing;
    internal bool selfClosingAcknowledged;
    internal bool hasPublicIdentifier;
    internal string publicIdentifier;
    internal bool hasSystemIdentifier;
    internal string systemIdentifier;
    internal bool forceQuirks;
    internal HtmlSourceSpan sourceSpan;
    internal HtmlToken(HtmlTokenKind kind, bool hasName, string name, string data, HtmlAttribute[] attributes, bool selfClosing, bool hasPublicIdentifier, string publicIdentifier, bool hasSystemIdentifier, string systemIdentifier, bool forceQuirks, HtmlSourceSpan sourceSpan)
    {
        this.kind = kind;
        this.hasName = hasName;
        this.name = name;
        this.data = data;
        this.attributes = CopyAttributes(attributes);
        this.selfClosing = selfClosing;
        this.selfClosingAcknowledged = false;
        this.hasPublicIdentifier = hasPublicIdentifier;
        this.publicIdentifier = publicIdentifier;
        this.hasSystemIdentifier = hasSystemIdentifier;
        this.systemIdentifier = systemIdentifier;
        this.forceQuirks = forceQuirks;
        this.sourceSpan = sourceSpan;
    }

    internal static HtmlToken CreateCharacter(string data, HtmlSourceSpan sourceSpan)
    {
        ValidateData(data);
        ValidateSpan(sourceSpan);
        return new HtmlToken(HtmlTokenKind.Character, false, "", data, new HtmlAttribute[0], false, false, "", false, "", false, sourceSpan);
    }

    internal static HtmlToken CreateComment(string data, HtmlSourceSpan sourceSpan)
    {
        ValidateData(data);
        ValidateSpan(sourceSpan);
        return new HtmlToken(HtmlTokenKind.Comment, false, "", data, new HtmlAttribute[0], false, false, "", false, "", false, sourceSpan);
    }

    internal static HtmlToken CreateStartTag(string name, HtmlAttribute[] attributes, bool selfClosing, HtmlSourceSpan sourceSpan)
    {
        ValidateTag(name, attributes, sourceSpan);
        return new HtmlToken(HtmlTokenKind.StartTag, true, name, "", attributes, selfClosing, false, "", false, "", false, sourceSpan);
    }

    internal static HtmlToken CreateEndTag(string name, HtmlAttribute[] attributes, bool selfClosing, HtmlSourceSpan sourceSpan)
    {
        ValidateTag(name, attributes, sourceSpan);
        return new HtmlToken(HtmlTokenKind.EndTag, true, name, "", attributes, selfClosing, false, "", false, "", false, sourceSpan);
    }

    internal static HtmlToken CreateDoctype(string name, bool forceQuirks, HtmlSourceSpan sourceSpan)
    {
        return CreateDoctype(true, name, false, "", false, "", forceQuirks, sourceSpan);
    }

    internal static HtmlToken CreateDoctype(bool hasName, string name, bool hasPublicIdentifier, string publicIdentifier, bool hasSystemIdentifier, string systemIdentifier, bool forceQuirks, HtmlSourceSpan sourceSpan)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (publicIdentifier == null)
        {
            throw new ArgumentNullException("publicIdentifier");
        }

        if (systemIdentifier == null)
        {
            throw new ArgumentNullException("systemIdentifier");
        }

        if (!hasName && name.Length != 0)
        {
            throw new ArgumentException("A missing DOCTYPE name must use an empty value.", "name");
        }

        if (!hasPublicIdentifier && publicIdentifier.Length != 0)
        {
            throw new ArgumentException("A missing public identifier must use an empty value.", "publicIdentifier");
        }

        if (!hasSystemIdentifier && systemIdentifier.Length != 0)
        {
            throw new ArgumentException("A missing system identifier must use an empty value.", "systemIdentifier");
        }

        ValidateSpan(sourceSpan);
        return new HtmlToken(HtmlTokenKind.Doctype, hasName, name, "", new HtmlAttribute[0], false, hasPublicIdentifier, publicIdentifier, hasSystemIdentifier, systemIdentifier, forceQuirks, sourceSpan);
    }

    internal static HtmlToken CreateEndOfFile(HtmlSourceSpan sourceSpan)
    {
        ValidateSpan(sourceSpan);
        return new HtmlToken(HtmlTokenKind.EndOfFile, false, "", "", new HtmlAttribute[0], false, false, "", false, "", false, sourceSpan);
    }

    /// <summary>Gets which tokenizer output form this token represents.</summary>
    /// <returns>Document type, start tag, end tag, comment, character data, or end of file.</returns>
    public HtmlTokenKind GetKind()
    {
        return this.kind;
    }

    /// <summary>Reports whether this token kind carries a tag or document-type name.</summary>
    /// <returns><c>true</c> when the token contains a name; otherwise, <c>false</c>.</returns>
    public bool HasName()
    {
        return this.hasName;
    }

    /// <summary>Gets the tag or document-type name carried by this token.</summary>
    /// <returns>The parsed name, or an empty string when this token has no name.</returns>
    public string GetName()
    {
        return this.name;
    }

    /// <summary>Gets the character or comment data carried by this token.</summary>
    /// <returns>The parsed data, or an empty string when this token has no character data.</returns>
    public string GetData()
    {
        return this.data;
    }

    /// <summary>Gets the ordered, duplicate-filtered attributes carried by a start-tag token.</summary>
    /// <returns>A snapshot of the attributes; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlAttribute[] GetAttributes()
    {
        return CopyAttributes(this.attributes);
    }

    /// <summary>Reports whether the token used self-closing start-tag syntax.</summary>
    /// <returns><c>true</c> when the token uses self-closing syntax; otherwise, <c>false</c>.</returns>
    public bool IsSelfClosing()
    {
        return this.selfClosing;
    }

    /// <summary>Reports whether the tree builder acknowledged the self-closing start tag.</summary>
    /// <returns><c>true</c> when the tree builder acknowledged the self-closing syntax; otherwise, <c>false</c>.</returns>
    public bool IsSelfClosingAcknowledged()
    {
        return this.selfClosingAcknowledged;
    }

    /// <summary>Reports whether a self-closing start tag still needs tree-builder acknowledgement.</summary>
    /// <returns><c>true</c> when a start tag requires tree-builder acknowledgement; otherwise, <c>false</c>.</returns>
    public bool RequiresSelfClosingAcknowledgement()
    {
        return this.kind == HtmlTokenKind.StartTag && this.selfClosing && !this.selfClosingAcknowledged;
    }

    internal void AcknowledgeSelfClosing()
    {
        if (this.kind != HtmlTokenKind.StartTag || !this.selfClosing)
        {
            throw new InvalidOperationException("Only a self-closing start tag can be acknowledged.");
        }

        this.selfClosingAcknowledged = true;
    }

    /// <summary>Indicates whether this object has the public identifier.</summary>
    /// <returns><c>true</c> when the document type contains a public identifier; otherwise, <c>false</c>.</returns>
    public bool HasPublicIdentifier()
    {
        return this.hasPublicIdentifier;
    }

    /// <summary>Gets the decoded public identifier carried by a document type token.</summary>
    /// <returns>The identifier when <see cref = "HasPublicIdentifier"/> is <c>true</c>; otherwise, an empty string.</returns>
    public string GetPublicIdentifier()
    {
        return this.publicIdentifier;
    }

    /// <summary>Indicates whether this object has the system identifier.</summary>
    /// <returns><c>true</c> when the document type contains a system identifier; otherwise, <c>false</c>.</returns>
    public bool HasSystemIdentifier()
    {
        return this.hasSystemIdentifier;
    }

    /// <summary>Gets the decoded system identifier carried by a document type token.</summary>
    /// <returns>The identifier when <see cref = "HasSystemIdentifier"/> is <c>true</c>; otherwise, an empty string.</returns>
    public string GetSystemIdentifier()
    {
        return this.systemIdentifier;
    }

    /// <summary>Reports whether this document type token forces quirks mode.</summary>
    /// <returns><c>true</c> when the document type forces quirks mode; otherwise, <c>false</c>.</returns>
    public bool IsForceQuirks()
    {
        return this.forceQuirks;
    }

    /// <summary>Gets the input range consumed to produce this token.</summary>
    /// <returns>The half-open source span, or an explicitly unknown span when source tracking was unavailable.</returns>
    public HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }

    internal static void ValidateData(string data)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }
    }

    internal static void ValidateTag(string name, HtmlAttribute[] attributes, HtmlSourceSpan sourceSpan)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("A tag name cannot be empty.", "name");
        }

        if (attributes == null)
        {
            throw new ArgumentNullException("attributes");
        }

        int index = 0;
        while (index < attributes.Length)
        {
            if (attributes[index] == null)
            {
                throw new ArgumentException("A tag attribute cannot be null.", "attributes");
            }

            index++;
        }

        ValidateSpan(sourceSpan);
    }

    internal static void ValidateSpan(HtmlSourceSpan sourceSpan)
    {
        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }
    }

    internal static HtmlAttribute[] CopyAttributes(HtmlAttribute[] values)
    {
        HtmlAttribute[] result = new HtmlAttribute[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}
