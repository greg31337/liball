using System;

namespace markdown;

internal class MarkdownNodeKindFacts
{
    internal static bool IsBlockKind(MarkdownNodeKind kind)
    {
        switch (kind)
        {
            case MarkdownNodeKind.BlockQuote:
            case MarkdownNodeKind.List:
            case MarkdownNodeKind.ListItem:
            case MarkdownNodeKind.Paragraph:
            case MarkdownNodeKind.Heading:
            case MarkdownNodeKind.ThematicBreak:
            case MarkdownNodeKind.IndentedCodeBlock:
            case MarkdownNodeKind.FencedCodeBlock:
            case MarkdownNodeKind.HtmlBlock:
            case MarkdownNodeKind.LinkReferenceDefinition:
            case MarkdownNodeKind.Table:
            case MarkdownNodeKind.TableHead:
            case MarkdownNodeKind.TableBody:
            case MarkdownNodeKind.TableRow:
            case MarkdownNodeKind.TableCell:
            case MarkdownNodeKind.TaskListItem:
            case MarkdownNodeKind.FootnoteDefinition:
            case MarkdownNodeKind.DefinitionList:
            case MarkdownNodeKind.DefinitionTerm:
            case MarkdownNodeKind.DefinitionDescription:
            case MarkdownNodeKind.FrontMatter:
            case MarkdownNodeKind.AbbreviationDefinition:
            case MarkdownNodeKind.MathBlock:
            case MarkdownNodeKind.DiagramBlock:
            case MarkdownNodeKind.AdmonitionBlock:
            case MarkdownNodeKind.CustomContainerBlock:
            case MarkdownNodeKind.CustomFencedBlock:
            case MarkdownNodeKind.DetailsBlock:
            case MarkdownNodeKind.ExtensionBlock:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsInlineKind(MarkdownNodeKind kind)
    {
        switch (kind)
        {
            case MarkdownNodeKind.Text:
            case MarkdownNodeKind.SoftLineBreak:
            case MarkdownNodeKind.HardLineBreak:
            case MarkdownNodeKind.CodeSpan:
            case MarkdownNodeKind.Emphasis:
            case MarkdownNodeKind.StrongEmphasis:
            case MarkdownNodeKind.Link:
            case MarkdownNodeKind.Image:
            case MarkdownNodeKind.HtmlInline:
            case MarkdownNodeKind.Strikethrough:
            case MarkdownNodeKind.FootnoteReference:
            case MarkdownNodeKind.Attribute:
            case MarkdownNodeKind.MathSpan:
            case MarkdownNodeKind.Abbreviation:
            case MarkdownNodeKind.Superscript:
            case MarkdownNodeKind.Subscript:
            case MarkdownNodeKind.InsertedText:
            case MarkdownNodeKind.MarkedText:
            case MarkdownNodeKind.Emoji:
            case MarkdownNodeKind.CustomInline:
            case MarkdownNodeKind.DetailsSummary:
            case MarkdownNodeKind.ExtensionInline:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsExtensionKind(MarkdownNodeKind kind)
    {
        switch (kind)
        {
            case MarkdownNodeKind.Table:
            case MarkdownNodeKind.TableHead:
            case MarkdownNodeKind.TableBody:
            case MarkdownNodeKind.TableRow:
            case MarkdownNodeKind.TableCell:
            case MarkdownNodeKind.Strikethrough:
            case MarkdownNodeKind.TaskListItem:
            case MarkdownNodeKind.FootnoteDefinition:
            case MarkdownNodeKind.FootnoteReference:
            case MarkdownNodeKind.DefinitionList:
            case MarkdownNodeKind.DefinitionTerm:
            case MarkdownNodeKind.DefinitionDescription:
            case MarkdownNodeKind.FrontMatter:
            case MarkdownNodeKind.AbbreviationDefinition:
            case MarkdownNodeKind.Attribute:
            case MarkdownNodeKind.MathSpan:
            case MarkdownNodeKind.MathBlock:
            case MarkdownNodeKind.DiagramBlock:
            case MarkdownNodeKind.AdmonitionBlock:
            case MarkdownNodeKind.CustomContainerBlock:
            case MarkdownNodeKind.CustomFencedBlock:
            case MarkdownNodeKind.Abbreviation:
            case MarkdownNodeKind.Superscript:
            case MarkdownNodeKind.Subscript:
            case MarkdownNodeKind.InsertedText:
            case MarkdownNodeKind.MarkedText:
            case MarkdownNodeKind.Emoji:
            case MarkdownNodeKind.CustomInline:
            case MarkdownNodeKind.DetailsBlock:
            case MarkdownNodeKind.DetailsSummary:
            case MarkdownNodeKind.ExtensionBlock:
            case MarkdownNodeKind.ExtensionInline:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsDocumentKind(MarkdownNodeKind kind)
    {
        return kind == MarkdownNodeKind.Document;
    }

    internal static void ValidateInlineExtensionKind(MarkdownNodeKind kind)
    {
        if (!IsInlineKind(kind) || !IsExtensionKind(kind))
        {
            throw new ArgumentException("Node kind is not an inline extension kind.");
        }
    }
}
