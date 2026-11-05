namespace markdown;

/// <summary>
/// Identifies the syntactic role of a Markdown AST node.
/// </summary>
public enum MarkdownNodeKind
{
    /// <summary>
    /// Identifies a unknown node.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Identifies a document node.
    /// </summary>
    Document = 1,
    /// <summary>
    /// Identifies a block quote node.
    /// </summary>
    BlockQuote = 2,
    /// <summary>
    /// Identifies a list node.
    /// </summary>
    List = 3,
    /// <summary>
    /// Identifies a list item node.
    /// </summary>
    ListItem = 4,
    /// <summary>
    /// Identifies a paragraph node.
    /// </summary>
    Paragraph = 5,
    /// <summary>
    /// Identifies a heading node.
    /// </summary>
    Heading = 6,
    /// <summary>
    /// Identifies a thematic break node.
    /// </summary>
    ThematicBreak = 7,
    /// <summary>
    /// Identifies a indented code block node.
    /// </summary>
    IndentedCodeBlock = 8,
    /// <summary>
    /// Identifies a fenced code block node.
    /// </summary>
    FencedCodeBlock = 9,
    /// <summary>
    /// Identifies a HTML block node.
    /// </summary>
    HtmlBlock = 10,
    /// <summary>
    /// Identifies a link reference definition node.
    /// </summary>
    LinkReferenceDefinition = 11,
    /// <summary>
    /// Identifies a text node.
    /// </summary>
    Text = 12,
    /// <summary>
    /// Identifies a soft line break node.
    /// </summary>
    SoftLineBreak = 13,
    /// <summary>
    /// Identifies a hard line break node.
    /// </summary>
    HardLineBreak = 14,
    /// <summary>
    /// Identifies a code span node.
    /// </summary>
    CodeSpan = 15,
    /// <summary>
    /// Identifies a emphasis node.
    /// </summary>
    Emphasis = 16,
    /// <summary>
    /// Identifies a strong emphasis node.
    /// </summary>
    StrongEmphasis = 17,
    /// <summary>
    /// Identifies a link node.
    /// </summary>
    Link = 18,
    /// <summary>
    /// Identifies a image node.
    /// </summary>
    Image = 19,
    /// <summary>
    /// Identifies a HTML inline node.
    /// </summary>
    HtmlInline = 20,
    /// <summary>
    /// Identifies a table node.
    /// </summary>
    Table = 21,
    /// <summary>
    /// Identifies a table head node.
    /// </summary>
    TableHead = 22,
    /// <summary>
    /// Identifies a table body node.
    /// </summary>
    TableBody = 23,
    /// <summary>
    /// Identifies a table row node.
    /// </summary>
    TableRow = 24,
    /// <summary>
    /// Identifies a table cell node.
    /// </summary>
    TableCell = 25,
    /// <summary>
    /// Identifies a strikethrough node.
    /// </summary>
    Strikethrough = 26,
    /// <summary>
    /// Identifies a task list item node.
    /// </summary>
    TaskListItem = 27,
    /// <summary>
    /// Identifies a footnote definition node.
    /// </summary>
    FootnoteDefinition = 28,
    /// <summary>
    /// Identifies a footnote reference node.
    /// </summary>
    FootnoteReference = 29,
    /// <summary>
    /// Identifies a definition list node.
    /// </summary>
    DefinitionList = 30,
    /// <summary>
    /// Identifies a definition term node.
    /// </summary>
    DefinitionTerm = 31,
    /// <summary>
    /// Identifies a definition description node.
    /// </summary>
    DefinitionDescription = 32,
    /// <summary>
    /// Identifies a front matter node.
    /// </summary>
    FrontMatter = 33,
    /// <summary>
    /// Identifies a attribute node.
    /// </summary>
    Attribute = 34,
    /// <summary>
    /// Identifies a math span node.
    /// </summary>
    MathSpan = 35,
    /// <summary>
    /// Identifies a math block node.
    /// </summary>
    MathBlock = 36,
    /// <summary>
    /// Identifies a extension block node.
    /// </summary>
    ExtensionBlock = 37,
    /// <summary>
    /// Identifies a extension inline node.
    /// </summary>
    ExtensionInline = 38,
    /// <summary>
    /// Identifies a abbreviation definition node.
    /// </summary>
    AbbreviationDefinition = 39,
    /// <summary>
    /// Identifies a abbreviation node.
    /// </summary>
    Abbreviation = 40,
    /// <summary>
    /// Identifies a superscript node.
    /// </summary>
    Superscript = 41,
    /// <summary>
    /// Identifies a subscript node.
    /// </summary>
    Subscript = 42,
    /// <summary>
    /// Identifies a inserted text node.
    /// </summary>
    InsertedText = 43,
    /// <summary>
    /// Identifies a marked text node.
    /// </summary>
    MarkedText = 44,
    /// <summary>
    /// Identifies a diagram block node.
    /// </summary>
    DiagramBlock = 45,
    /// <summary>
    /// Identifies a emoji node.
    /// </summary>
    Emoji = 46,
    /// <summary>
    /// Identifies a admonition block node.
    /// </summary>
    AdmonitionBlock = 47,
    /// <summary>
    /// Identifies a custom container block node.
    /// </summary>
    CustomContainerBlock = 48,
    /// <summary>
    /// Identifies a custom fenced block node.
    /// </summary>
    CustomFencedBlock = 49,
    /// <summary>
    /// Identifies a custom inline node.
    /// </summary>
    CustomInline = 50,
    /// <summary>
    /// Identifies a collapsible details block node.
    /// </summary>
    DetailsBlock = 51,
    /// <summary>
    /// Identifies the summary node of a details block.
    /// </summary>
    DetailsSummary = 52
}
