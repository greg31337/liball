using System;

namespace markdown;

/// <summary>
/// Provides typed, feature-specific data for a Markdown AST node.
/// </summary>
/// <remarks>
/// <see cref="Kind"/> is available for every node. The feature field matching that kind is populated when the kind has additional structured metadata.
/// </remarks>
public class MarkdownNodeData
{
    /// <summary>The node kind described by this data.</summary>
    public MarkdownNodeKind Kind;
    /// <summary>Heading metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownHeadingData? Heading;
    /// <summary>List metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownListData? List;
    /// <summary>List-item metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownListItemData? ListItem;
    /// <summary>Code-block metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownCodeBlockData? CodeBlock;
    /// <summary>Link metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownLinkData? Link;
    /// <summary>Image metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownImageData? Image;
    /// <summary>Link-reference-definition metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownReferenceData? Reference;
    /// <summary>Table metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownTableData? Table;
    /// <summary>Table-cell metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownTableCellData? TableCell;
    /// <summary>Task-list-item metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownTaskListItemData? TaskListItem;
    /// <summary>Footnote metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownFootnoteData? Footnote;
    /// <summary>Front-matter metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownFrontMatterData? FrontMatter;
    /// <summary>Math metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownMathData? Math;
    /// <summary>Diagram metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownDiagramData? Diagram;
    /// <summary>Admonition metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownAdmonitionData? Admonition;
    /// <summary>Custom-container metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownContainerData? Container;
    /// <summary>Custom fenced or inline metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownCustomNodeData? Custom;
    /// <summary>Abbreviation metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownAbbreviationData? Abbreviation;
    /// <summary>Emoji metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownEmojiData? Emoji;
    /// <summary>Attribute-node metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownAttributeNodeData? Attribute;
    /// <summary>Generic extension metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownExtensionData? Extension;
    /// <summary>Details-block metadata, or <c>null</c> for other node kinds.</summary>
    public MarkdownDetailsData? Details;
    /// <summary>Names of caller-authored generic attributes attached by source syntax.</summary>
    public MarkdownAttachedAttributeData AttachedAttributes;

    /// <summary>Initializes data for an unknown node.</summary>
    public MarkdownNodeData()
    {
        Kind = MarkdownNodeKind.Unknown;
        Heading = null;
        List = null;
        ListItem = null;
        CodeBlock = null;
        Link = null;
        Image = null;
        Reference = null;
        Table = null;
        TableCell = null;
        TaskListItem = null;
        Footnote = null;
        FrontMatter = null;
        Math = null;
        Diagram = null;
        Admonition = null;
        Container = null;
        Custom = null;
        Abbreviation = null;
        Emoji = null;
        Attribute = null;
        Extension = null;
        Details = null;
        AttachedAttributes = new MarkdownAttachedAttributeData();
    }

    internal static MarkdownNodeData CreateForKind(MarkdownNodeKind kind)
    {
        MarkdownNodeData data = new MarkdownNodeData();
        data.Kind = kind;

        switch (kind)
        {
            case MarkdownNodeKind.Heading:
                data.Heading = new MarkdownHeadingData();
                break;
            case MarkdownNodeKind.List:
                data.List = new MarkdownListData();
                break;
            case MarkdownNodeKind.ListItem:
                data.ListItem = new MarkdownListItemData();
                break;
            case MarkdownNodeKind.IndentedCodeBlock:
                data.CodeBlock = new MarkdownCodeBlockData();
                break;
            case MarkdownNodeKind.FencedCodeBlock:
                data.CodeBlock = new MarkdownCodeBlockData();
                data.CodeBlock.IsFenced = true;
                break;
            case MarkdownNodeKind.Link:
                data.Link = new MarkdownLinkData();
                break;
            case MarkdownNodeKind.Image:
                data.Image = new MarkdownImageData();
                break;
            case MarkdownNodeKind.LinkReferenceDefinition:
                data.Reference = new MarkdownReferenceData();
                break;
            case MarkdownNodeKind.Table:
                data.Table = new MarkdownTableData();
                break;
            case MarkdownNodeKind.TableCell:
                data.TableCell = new MarkdownTableCellData();
                break;
            case MarkdownNodeKind.TaskListItem:
                data.ListItem = new MarkdownListItemData();
                data.TaskListItem = new MarkdownTaskListItemData();
                break;
            case MarkdownNodeKind.FootnoteDefinition:
            case MarkdownNodeKind.FootnoteReference:
                data.Footnote = new MarkdownFootnoteData();
                data.Footnote.IsDefinition = kind == MarkdownNodeKind.FootnoteDefinition;
                break;
            case MarkdownNodeKind.FrontMatter:
                data.FrontMatter = new MarkdownFrontMatterData();
                break;
            case MarkdownNodeKind.MathSpan:
            case MarkdownNodeKind.MathBlock:
                data.Math = new MarkdownMathData();
                data.Math.IsBlock = kind == MarkdownNodeKind.MathBlock;
                break;
            case MarkdownNodeKind.DiagramBlock:
                data.Diagram = new MarkdownDiagramData();
                break;
            case MarkdownNodeKind.AdmonitionBlock:
                data.Admonition = new MarkdownAdmonitionData();
                break;
            case MarkdownNodeKind.CustomContainerBlock:
                data.Container = new MarkdownContainerData();
                break;
            case MarkdownNodeKind.CustomFencedBlock:
            case MarkdownNodeKind.CustomInline:
                data.Custom = new MarkdownCustomNodeData();
                data.Custom.IsBlock = kind == MarkdownNodeKind.CustomFencedBlock;
                break;
            case MarkdownNodeKind.AbbreviationDefinition:
            case MarkdownNodeKind.Abbreviation:
                data.Abbreviation = new MarkdownAbbreviationData();
                data.Abbreviation.IsDefinition = kind == MarkdownNodeKind.AbbreviationDefinition;
                break;
            case MarkdownNodeKind.Emoji:
                data.Emoji = new MarkdownEmojiData();
                break;
            case MarkdownNodeKind.Attribute:
                data.Attribute = new MarkdownAttributeNodeData();
                break;
            case MarkdownNodeKind.ExtensionBlock:
            case MarkdownNodeKind.ExtensionInline:
                data.Extension = new MarkdownExtensionData();
                data.Extension.IsBlock = kind == MarkdownNodeKind.ExtensionBlock;
                break;
            case MarkdownNodeKind.DetailsBlock:
            case MarkdownNodeKind.DetailsSummary:
                data.Details = new MarkdownDetailsData();
                data.Details.IsSummary = kind == MarkdownNodeKind.DetailsSummary;
                break;
        }

        return data;
    }

    internal MarkdownNodeData CopyData()
    {
        MarkdownNodeData copy = CreateForKind(Kind);
        if (Heading != null && copy.Heading != null) copy.Heading.CopyValues(Heading);
        if (List != null && copy.List != null) copy.List.CopyValues(List);
        if (ListItem != null && copy.ListItem != null) copy.ListItem.CopyValues(ListItem);
        if (CodeBlock != null && copy.CodeBlock != null) copy.CodeBlock.CopyValues(CodeBlock);
        if (Link != null && copy.Link != null) copy.Link.CopyValues(Link);
        if (Image != null && copy.Image != null) copy.Image.CopyValues(Image);
        if (Reference != null && copy.Reference != null) copy.Reference.CopyValues(Reference);
        if (Table != null && copy.Table != null) copy.Table.CopyValues(Table);
        if (TableCell != null && copy.TableCell != null) copy.TableCell.CopyValues(TableCell);
        if (TaskListItem != null && copy.TaskListItem != null) copy.TaskListItem.CopyValues(TaskListItem);
        if (Footnote != null && copy.Footnote != null) copy.Footnote.CopyValues(Footnote);
        if (FrontMatter != null && copy.FrontMatter != null) copy.FrontMatter.CopyValues(FrontMatter);
        if (Math != null && copy.Math != null) copy.Math.CopyValues(Math);
        if (Diagram != null && copy.Diagram != null) copy.Diagram.CopyValues(Diagram);
        if (Admonition != null && copy.Admonition != null) copy.Admonition.CopyValues(Admonition);
        if (Container != null && copy.Container != null) copy.Container.CopyValues(Container);
        if (Custom != null && copy.Custom != null) copy.Custom.CopyValues(Custom);
        if (Abbreviation != null && copy.Abbreviation != null) copy.Abbreviation.CopyValues(Abbreviation);
        if (Emoji != null && copy.Emoji != null) copy.Emoji.CopyValues(Emoji);
        if (Attribute != null && copy.Attribute != null) copy.Attribute.CopyValues(Attribute);
        if (Extension != null && copy.Extension != null) copy.Extension.CopyValues(Extension);
        if (Details != null && copy.Details != null) copy.Details.CopyValues(Details);
        copy.AttachedAttributes.CopyValues(AttachedAttributes);
        return copy;
    }

    internal void ApplyAttribute(string name, string value)
    {
        int numericValue = ParseNonNegativeInteger(value);
        bool booleanValue = value == "true";

        if (Heading != null)
        {
            if (name == "level") Heading.Level = numericValue;
            else if (name == "id") Heading.Id = value;
            else if (name == "explicitAttributes") Heading.HasExplicitAttributes = booleanValue;
        }
        if (List != null)
        {
            if (name == "type") List.IsOrdered = value == "ordered";
            else if (name == "start") List.Start = numericValue;
            else if (name == "marker") List.Marker = value;
            else if (name == "tight") List.IsTight = booleanValue;
        }
        if (ListItem != null)
        {
            if (name == "number") ListItem.Number = numericValue;
            else if (name == "marker") ListItem.Marker = value;
        }
        if (CodeBlock != null)
        {
            if (name == "info") CodeBlock.Info = value;
            else if (name == "fenceMarker") CodeBlock.FenceMarker = value;
            else if (name == "fenceLength") CodeBlock.FenceLength = numericValue;
        }
        if (Link != null)
        {
            if (name == "destination") Link.Destination = value;
            else if (name == "title") Link.Title = value;
            else if (name == "referenceLabel") Link.ReferenceLabel = value;
            else if (name == "autolink") Link.IsAutolink = booleanValue;
        }
        if (Image != null)
        {
            if (name == "destination") Image.Destination = value;
            else if (name == "title") Image.Title = value;
            else if (name == "referenceLabel") Image.ReferenceLabel = value;
        }
        if (Reference != null)
        {
            if (name == "label") Reference.Label = value;
            else if (name == "normalizedLabel") Reference.NormalizedLabel = value;
            else if (name == "destination") Reference.Destination = value;
            else if (name == "title") Reference.Title = value;
        }
        if (Table != null && name == "columns") Table.ColumnCount = numericValue;
        if (TableCell != null && name == "align") TableCell.Alignment = value;
        if (TaskListItem != null && name == "checked") TaskListItem.IsChecked = booleanValue;
        if (Footnote != null)
        {
            if (name == "label") Footnote.Label = value;
            else if (name == "normalizedLabel") Footnote.NormalizedLabel = value;
            else if (name == "resolved") Footnote.IsResolved = booleanValue;
        }
        if (FrontMatter != null)
        {
            if (name == "format") FrontMatter.Format = value;
            else if (name == "delimiter") FrontMatter.Delimiter = value;
        }
        if (Math != null && name == "delimiter") Math.Delimiter = value;
        if (Diagram != null)
        {
            if (name == "diagramType") Diagram.DiagramType = value;
            else if (name == "info") Diagram.Info = value;
        }
        if (Admonition != null)
        {
            if (name == "style") Admonition.Style = value;
            else if (name == "type") Admonition.Type = value;
            else if (name == "title") Admonition.Title = value;
        }
        if (Container != null)
        {
            if (name == "name") Container.Name = value;
            else if (name == "title") Container.Title = value;
        }
        if (Custom != null)
        {
            if (name == "name") Custom.Name = value;
            else if (name == "info") Custom.Info = value;
            else if (name == "infoKey") Custom.Name = value;
            else if (name == "openingDelimiter") Custom.OpeningDelimiter = value;
            else if (name == "closingDelimiter") Custom.ClosingDelimiter = value;
        }
        if (Abbreviation != null)
        {
            if (name == "label") Abbreviation.Label = value;
            else if (name == "title") Abbreviation.Title = value;
        }
        if (Emoji != null)
        {
            if (name == "name") Emoji.Name = value;
            else if (name == "replacement") Emoji.Replacement = value;
        }
        if (Attribute != null)
        {
            if (name == "name") Attribute.Name = value;
            else if (name == "value") Attribute.Value = value;
        }
        if (Extension != null && name == "name") Extension.Name = value;
        if (Details != null)
        {
            if (name == "summary") Details.Summary = value;
            else if (name == "open") Details.IsOpen = booleanValue;
        }
    }

    internal static int ParseNonNegativeInteger(string value)
    {
        int result = 0;
        int index = 0;
        if (value.Length == 0) return 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9') return 0;
            int digit = character - '0';
            if (result > (int.MaxValue - digit) / 10) return 0;
            result = result * 10 + digit;
            index = index + 1;
        }
        return result;
    }
}

/// <summary>Describes heading level and identifier metadata.</summary>
public class MarkdownHeadingData
{
    /// <summary>The heading level from one through six.</summary>
    public int Level;
    /// <summary>The explicit or generated heading identifier.</summary>
    public string Id;
    /// <summary>Whether an explicit attribute list was present.</summary>
    public bool HasExplicitAttributes;
    /// <summary>Initializes empty heading metadata.</summary>
    public MarkdownHeadingData() { Level = 0; Id = ""; HasExplicitAttributes = false; }
    internal void CopyValues(MarkdownHeadingData other) { Level = other.Level; Id = other.Id; HasExplicitAttributes = other.HasExplicitAttributes; }
}

/// <summary>Describes ordered or bullet list metadata.</summary>
public class MarkdownListData
{
    /// <summary>Whether the list uses ordered markers.</summary>
    public bool IsOrdered;
    /// <summary>The first ordered-list number, or zero for a bullet list.</summary>
    public int Start;
    /// <summary>The list marker text.</summary>
    public string Marker;
    /// <summary>Whether the list is tight.</summary>
    public bool IsTight;
    /// <summary>Initializes empty list metadata.</summary>
    public MarkdownListData() { IsOrdered = false; Start = 0; Marker = ""; IsTight = false; }
    internal void CopyValues(MarkdownListData other) { IsOrdered = other.IsOrdered; Start = other.Start; Marker = other.Marker; IsTight = other.IsTight; }
}

/// <summary>Describes a list item's marker and ordinal.</summary>
public class MarkdownListItemData
{
    /// <summary>The ordered item number, or zero for a bullet item.</summary>
    public int Number;
    /// <summary>The item marker text.</summary>
    public string Marker;
    /// <summary>Initializes empty list-item metadata.</summary>
    public MarkdownListItemData() { Number = 0; Marker = ""; }
    internal void CopyValues(MarkdownListItemData other) { Number = other.Number; Marker = other.Marker; }
}

/// <summary>Describes indented or fenced code-block metadata.</summary>
public class MarkdownCodeBlockData
{
    /// <summary>Whether the code block uses a fence.</summary>
    public bool IsFenced;
    /// <summary>The complete fence info string.</summary>
    public string Info;
    /// <summary>The backtick or tilde fence marker.</summary>
    public string FenceMarker;
    /// <summary>The opening fence length.</summary>
    public int FenceLength;
    /// <summary>Initializes empty code-block metadata.</summary>
    public MarkdownCodeBlockData() { IsFenced = false; Info = ""; FenceMarker = ""; FenceLength = 0; }
    internal void CopyValues(MarkdownCodeBlockData other) { IsFenced = other.IsFenced; Info = other.Info; FenceMarker = other.FenceMarker; FenceLength = other.FenceLength; }
}

/// <summary>Describes a link destination and its source form.</summary>
public class MarkdownLinkData
{
    /// <summary>The link destination.</summary>
    public string Destination;
    /// <summary>The optional link title.</summary>
    public string Title;
    /// <summary>The source reference label, or an empty string for inline links.</summary>
    public string ReferenceLabel;
    /// <summary>Whether the link originated as an autolink.</summary>
    public bool IsAutolink;
    /// <summary>Initializes empty link metadata.</summary>
    public MarkdownLinkData() { Destination = ""; Title = ""; ReferenceLabel = ""; IsAutolink = false; }
    internal void CopyValues(MarkdownLinkData other) { Destination = other.Destination; Title = other.Title; ReferenceLabel = other.ReferenceLabel; IsAutolink = other.IsAutolink; }
}

/// <summary>Describes an image destination and its source form.</summary>
public class MarkdownImageData
{
    /// <summary>The image destination.</summary>
    public string Destination;
    /// <summary>The optional image title.</summary>
    public string Title;
    /// <summary>The source reference label, or an empty string for inline images.</summary>
    public string ReferenceLabel;
    /// <summary>Initializes empty image metadata.</summary>
    public MarkdownImageData() { Destination = ""; Title = ""; ReferenceLabel = ""; }
    internal void CopyValues(MarkdownImageData other) { Destination = other.Destination; Title = other.Title; ReferenceLabel = other.ReferenceLabel; }
}

/// <summary>Describes a link reference definition.</summary>
public class MarkdownReferenceData
{
    /// <summary>The original reference label.</summary>
    public string Label;
    /// <summary>The normalized reference label.</summary>
    public string NormalizedLabel;
    /// <summary>The reference destination.</summary>
    public string Destination;
    /// <summary>The optional reference title.</summary>
    public string Title;
    /// <summary>Initializes empty reference metadata.</summary>
    public MarkdownReferenceData() { Label = ""; NormalizedLabel = ""; Destination = ""; Title = ""; }
    internal void CopyValues(MarkdownReferenceData other) { Label = other.Label; NormalizedLabel = other.NormalizedLabel; Destination = other.Destination; Title = other.Title; }
}

/// <summary>Describes table dimensions.</summary>
public class MarkdownTableData
{
    /// <summary>The number of table columns.</summary>
    public int ColumnCount;
    /// <summary>Initializes empty table metadata.</summary>
    public MarkdownTableData() { ColumnCount = 0; }
    internal void CopyValues(MarkdownTableData other) { ColumnCount = other.ColumnCount; }
}

/// <summary>Describes a table cell's alignment.</summary>
public class MarkdownTableCellData
{
    /// <summary>The alignment name: empty, <c>left</c>, <c>center</c>, or <c>right</c>.</summary>
    public string Alignment;
    /// <summary>Initializes empty table-cell metadata.</summary>
    public MarkdownTableCellData() { Alignment = ""; }
    internal void CopyValues(MarkdownTableCellData other) { Alignment = other.Alignment; }
}

/// <summary>Describes a task list item's checked state.</summary>
public class MarkdownTaskListItemData
{
    /// <summary>Whether the task marker is checked.</summary>
    public bool IsChecked;
    /// <summary>Initializes unchecked task metadata.</summary>
    public MarkdownTaskListItemData() { IsChecked = false; }
    internal void CopyValues(MarkdownTaskListItemData other) { IsChecked = other.IsChecked; }
}

/// <summary>Describes a footnote definition or reference.</summary>
public class MarkdownFootnoteData
{
    /// <summary>The original footnote label.</summary>
    public string Label;
    /// <summary>The normalized footnote label.</summary>
    public string NormalizedLabel;
    /// <summary>Whether a reference resolved to a definition.</summary>
    public bool IsResolved;
    /// <summary>Whether the node is a definition rather than a reference.</summary>
    public bool IsDefinition;
    /// <summary>Initializes empty footnote metadata.</summary>
    public MarkdownFootnoteData() { Label = ""; NormalizedLabel = ""; IsResolved = false; IsDefinition = false; }
    internal void CopyValues(MarkdownFootnoteData other) { Label = other.Label; NormalizedLabel = other.NormalizedLabel; IsResolved = other.IsResolved; IsDefinition = other.IsDefinition; }
}

/// <summary>Describes captured front matter.</summary>
public class MarkdownFrontMatterData
{
    /// <summary>The format name such as <c>yaml</c>, <c>toml</c>, or <c>json</c>.</summary>
    public string Format;
    /// <summary>The opening and closing delimiter text.</summary>
    public string Delimiter;
    /// <summary>Initializes empty front-matter metadata.</summary>
    public MarkdownFrontMatterData() { Format = ""; Delimiter = ""; }
    internal void CopyValues(MarkdownFrontMatterData other) { Format = other.Format; Delimiter = other.Delimiter; }
}

/// <summary>Describes inline or block math.</summary>
public class MarkdownMathData
{
    /// <summary>The math delimiter text.</summary>
    public string Delimiter;
    /// <summary>Whether the node is block math.</summary>
    public bool IsBlock;
    /// <summary>Initializes empty inline-math metadata.</summary>
    public MarkdownMathData() { Delimiter = ""; IsBlock = false; }
    internal void CopyValues(MarkdownMathData other) { Delimiter = other.Delimiter; IsBlock = other.IsBlock; }
}

/// <summary>Describes a diagram block.</summary>
public class MarkdownDiagramData
{
    /// <summary>The registered diagram type.</summary>
    public string DiagramType;
    /// <summary>The complete fence info string.</summary>
    public string Info;
    /// <summary>Initializes empty diagram metadata.</summary>
    public MarkdownDiagramData() { DiagramType = ""; Info = ""; }
    internal void CopyValues(MarkdownDiagramData other) { DiagramType = other.DiagramType; Info = other.Info; }
}

/// <summary>Describes an admonition or alert block.</summary>
public class MarkdownAdmonitionData
{
    /// <summary>The source style, such as admonition or alert.</summary>
    public string Style;
    /// <summary>The admonition type.</summary>
    public string Type;
    /// <summary>The optional title.</summary>
    public string Title;
    /// <summary>Initializes empty admonition metadata.</summary>
    public MarkdownAdmonitionData() { Style = ""; Type = ""; Title = ""; }
    internal void CopyValues(MarkdownAdmonitionData other) { Style = other.Style; Type = other.Type; Title = other.Title; }
}

/// <summary>Describes a custom container block.</summary>
public class MarkdownContainerData
{
    /// <summary>The registered container name.</summary>
    public string Name;
    /// <summary>The optional container title.</summary>
    public string Title;
    /// <summary>Initializes empty container metadata.</summary>
    public MarkdownContainerData() { Name = ""; Title = ""; }
    internal void CopyValues(MarkdownContainerData other) { Name = other.Name; Title = other.Title; }
}

/// <summary>Describes a custom fenced or inline node.</summary>
public class MarkdownCustomNodeData
{
    /// <summary>The registered rule or info-key name.</summary>
    public string Name;
    /// <summary>The complete fence info string.</summary>
    public string Info;
    /// <summary>The custom inline opening delimiter.</summary>
    public string OpeningDelimiter;
    /// <summary>The custom inline closing delimiter.</summary>
    public string ClosingDelimiter;
    /// <summary>Whether the custom node is a block.</summary>
    public bool IsBlock;
    /// <summary>Initializes empty custom-node metadata.</summary>
    public MarkdownCustomNodeData() { Name = ""; Info = ""; OpeningDelimiter = ""; ClosingDelimiter = ""; IsBlock = false; }
    internal void CopyValues(MarkdownCustomNodeData other) { Name = other.Name; Info = other.Info; OpeningDelimiter = other.OpeningDelimiter; ClosingDelimiter = other.ClosingDelimiter; IsBlock = other.IsBlock; }
}

/// <summary>Describes an abbreviation definition or use.</summary>
public class MarkdownAbbreviationData
{
    /// <summary>The abbreviation label.</summary>
    public string Label;
    /// <summary>The expanded abbreviation title.</summary>
    public string Title;
    /// <summary>Whether the node is a definition.</summary>
    public bool IsDefinition;
    /// <summary>Initializes empty abbreviation metadata.</summary>
    public MarkdownAbbreviationData() { Label = ""; Title = ""; IsDefinition = false; }
    internal void CopyValues(MarkdownAbbreviationData other) { Label = other.Label; Title = other.Title; IsDefinition = other.IsDefinition; }
}

/// <summary>Describes an emoji shortcode and replacement.</summary>
public class MarkdownEmojiData
{
    /// <summary>The shortcode name without colons.</summary>
    public string Name;
    /// <summary>The configured replacement text.</summary>
    public string Replacement;
    /// <summary>Initializes empty emoji metadata.</summary>
    public MarkdownEmojiData() { Name = ""; Replacement = ""; }
    internal void CopyValues(MarkdownEmojiData other) { Name = other.Name; Replacement = other.Replacement; }
}

/// <summary>Describes a standalone attribute node.</summary>
public class MarkdownAttributeNodeData
{
    /// <summary>The attribute name.</summary>
    public string Name;
    /// <summary>The attribute value.</summary>
    public string Value;
    /// <summary>Initializes empty attribute-node metadata.</summary>
    public MarkdownAttributeNodeData() { Name = ""; Value = ""; }
    internal void CopyValues(MarkdownAttributeNodeData other) { Name = other.Name; Value = other.Value; }
}

/// <summary>Describes a generic extension block or inline.</summary>
public class MarkdownExtensionData
{
    /// <summary>The extension-defined name.</summary>
    public string Name;
    /// <summary>Whether the extension node is a block.</summary>
    public bool IsBlock;
    /// <summary>Initializes empty extension metadata.</summary>
    public MarkdownExtensionData() { Name = ""; IsBlock = false; }
    internal void CopyValues(MarkdownExtensionData other) { Name = other.Name; IsBlock = other.IsBlock; }
}

/// <summary>Describes a collapsible details block or its summary node.</summary>
public class MarkdownDetailsData
{
    /// <summary>The summary source text.</summary>
    public string Summary;
    /// <summary>Whether the details block is initially open.</summary>
    public bool IsOpen;
    /// <summary>Whether this data belongs to the summary child rather than the details block.</summary>
    public bool IsSummary;
    /// <summary>Initializes empty details metadata.</summary>
    public MarkdownDetailsData() { Summary = ""; IsOpen = false; IsSummary = false; }
    internal void CopyValues(MarkdownDetailsData other) { Summary = other.Summary; IsOpen = other.IsOpen; IsSummary = other.IsSummary; }
}

/// <summary>Tracks caller-authored generic attribute names in source order.</summary>
public class MarkdownAttachedAttributeData
{
    /// <summary>The attached attribute names in first-seen order.</summary>
    public System.Collections.Generic.List<string> Names;
    /// <summary>Whether the attribute list used block attachment syntax.</summary>
    public bool IsBlock;
    /// <summary>Initializes empty attached-attribute metadata.</summary>
    public MarkdownAttachedAttributeData()
    {
        Names = new System.Collections.Generic.List<string>();
        IsBlock = false;
    }
    internal void CopyValues(MarkdownAttachedAttributeData other)
    {
        Names.Clear();
        int index = 0;
        while (index < other.Names.Count)
        {
            Names.Add(other.Names[index]);
            index = index + 1;
        }
        IsBlock = other.IsBlock;
    }
}
