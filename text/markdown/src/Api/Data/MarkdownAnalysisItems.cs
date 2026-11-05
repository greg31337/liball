using System;

namespace markdown;

/// <summary>
/// Describes a heading found during document analysis.
/// </summary>
public class MarkdownHeadingInfo
{
    /// <summary>
    /// Stores the node value used by this instance.
    /// </summary>
    public MarkdownNode Node;
    /// <summary>
    /// Stores the level value used by this instance.
    /// </summary>
    public int Level;
    /// <summary>
    /// Stores the text value used by this instance.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the ID value used by this instance.
    /// </summary>
    public string Id;

    /// <summary>
    /// Initializes a new <see cref="MarkdownHeadingInfo"/> instance.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <param name="level">The heading level from 1 through 6.</param>
    /// <param name="text">The text to process.</param>
    /// <param name="id">The id value.</param>
    public MarkdownHeadingInfo(MarkdownNode node, int level, string text, string id)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (level < 1)
        {
            throw new ArgumentOutOfRangeException("level");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        Node = node;
        Level = level;
        Text = text;
        Id = id;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownHeadingInfo"/> value.</returns>
    internal MarkdownHeadingInfo Copy()
    {
        return new MarkdownHeadingInfo(Node, Level, Text, Id);
    }
}

/// <summary>
/// Describes a link found during document analysis.
/// </summary>
public class MarkdownLinkInfo
{
    /// <summary>
    /// Stores the node value used by this instance.
    /// </summary>
    public MarkdownNode Node;
    /// <summary>
    /// Stores the destination value used by this instance.
    /// </summary>
    public string Destination;
    /// <summary>
    /// Stores the title value used by this instance.
    /// </summary>
    public string Title;
    /// <summary>
    /// Stores the text value used by this instance.
    /// </summary>
    public string Text;

    /// <summary>
    /// Initializes a new <see cref="MarkdownLinkInfo"/> instance.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="title">The title value.</param>
    /// <param name="text">The text to process.</param>
    public MarkdownLinkInfo(MarkdownNode node, string destination, string title, string text)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Node = node;
        Destination = destination;
        Title = title;
        Text = text;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownLinkInfo"/> value.</returns>
    internal MarkdownLinkInfo Copy()
    {
        return new MarkdownLinkInfo(Node, Destination, Title, Text);
    }
}

/// <summary>
/// Describes an image found during document analysis.
/// </summary>
public class MarkdownImageInfo
{
    /// <summary>
    /// Stores the node value used by this instance.
    /// </summary>
    public MarkdownNode Node;
    /// <summary>
    /// Stores the destination value used by this instance.
    /// </summary>
    public string Destination;
    /// <summary>
    /// Stores the title value used by this instance.
    /// </summary>
    public string Title;
    /// <summary>
    /// Stores the alt text value used by this instance.
    /// </summary>
    public string AltText;

    /// <summary>
    /// Initializes a new <see cref="MarkdownImageInfo"/> instance.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="title">The title value.</param>
    /// <param name="altText">The alt text value.</param>
    public MarkdownImageInfo(MarkdownNode node, string destination, string title, string altText)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (altText == null)
        {
            throw new ArgumentNullException("altText");
        }

        Node = node;
        Destination = destination;
        Title = title;
        AltText = altText;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownImageInfo"/> value.</returns>
    internal MarkdownImageInfo Copy()
    {
        return new MarkdownImageInfo(Node, Destination, Title, AltText);
    }
}

/// <summary>
/// Describes one heading in the generated document outline.
/// </summary>
public class MarkdownHeadingOutlineItem
{
    /// <summary>The analyzed heading.</summary>
    public MarkdownHeadingInfo Heading;
    /// <summary>The outline depth, starting at zero.</summary>
    public int Depth;
    /// <summary>The parent outline index, or minus one for a root item.</summary>
    public int ParentIndex;

    /// <summary>Initializes an empty outline item.</summary>
    public MarkdownHeadingOutlineItem()
    {
        Heading = new MarkdownHeadingInfo(new MarkdownNode(), 1, "", "");
        Depth = 0;
        ParentIndex = -1;
    }

    internal MarkdownHeadingOutlineItem Copy()
    {
        MarkdownHeadingOutlineItem copy = new MarkdownHeadingOutlineItem();
        copy.Heading = Heading.Copy();
        copy.Depth = Depth;
        copy.ParentIndex = ParentIndex;
        return copy;
    }
}

/// <summary>
/// Describes a link-reference definition found during analysis.
/// </summary>
public class MarkdownLinkReferenceInfo
{
    /// <summary>The definition node.</summary>
    public MarkdownNode Node;
    /// <summary>The source label.</summary>
    public string Label;
    /// <summary>The normalized label.</summary>
    public string NormalizedLabel;
    /// <summary>The link destination.</summary>
    public string Destination;
    /// <summary>The optional title.</summary>
    public string Title;

    /// <summary>Initializes an empty reference description.</summary>
    public MarkdownLinkReferenceInfo()
    {
        Node = new MarkdownNode();
        Label = "";
        NormalizedLabel = "";
        Destination = "";
        Title = "";
    }

    internal MarkdownLinkReferenceInfo Copy()
    {
        MarkdownLinkReferenceInfo copy = new MarkdownLinkReferenceInfo();
        copy.Node = Node;
        copy.Label = Label;
        copy.NormalizedLabel = NormalizedLabel;
        copy.Destination = Destination;
        copy.Title = Title;
        return copy;
    }
}

/// <summary>
/// Describes a captured front-matter block.
/// </summary>
public class MarkdownFrontMatterInfo
{
    /// <summary>The front-matter node.</summary>
    public MarkdownNode Node;
    /// <summary>The format name.</summary>
    public string Format;
    /// <summary>The opening and closing delimiter.</summary>
    public string Delimiter;
    /// <summary>The captured content between delimiters.</summary>
    public string Content;

    /// <summary>Initializes an empty front-matter description.</summary>
    public MarkdownFrontMatterInfo()
    {
        Node = new MarkdownNode();
        Format = "";
        Delimiter = "";
        Content = "";
    }

    internal MarkdownFrontMatterInfo Copy()
    {
        MarkdownFrontMatterInfo copy = new MarkdownFrontMatterInfo();
        copy.Node = Node;
        copy.Format = Format;
        copy.Delimiter = Delimiter;
        copy.Content = Content;
        return copy;
    }
}
