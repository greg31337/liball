namespace html;
/// <summary>Identifies the kind of token emitted by the HTML tokenizer.</summary>
public enum HtmlTokenKind
{
    /// <summary>Identifies a document-type token.</summary>
    Doctype = 0,
    /// <summary>Identifies a start-tag token.</summary>
    StartTag = 1,
    /// <summary>Identifies an end-tag token.</summary>
    EndTag = 2,
    /// <summary>Identifies a comment token.</summary>
    Comment = 3,
    /// <summary>Identifies a character-data token.</summary>
    Character = 4,
    /// <summary>Identifies the final end-of-file token.</summary>
    EndOfFile = 5
}
