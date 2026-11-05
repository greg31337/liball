using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Contains the aggregate results of analyzing a Markdown document.
/// </summary>
public class MarkdownDocumentAnalysis
{
    /// <summary>
    /// Stores analyzed headings in document order.
    /// </summary>
    public List<MarkdownHeadingInfo> Headings;
    /// <summary>
    /// Stores analyzed links in document order.
    /// </summary>
    public List<MarkdownLinkInfo> Links;
    /// <summary>
    /// Stores analyzed images in document order.
    /// </summary>
    public List<MarkdownImageInfo> Images;
    /// <summary>
    /// Stores the hierarchical heading outline in document order.
    /// </summary>
    public List<MarkdownHeadingOutlineItem> HeadingOutline;
    /// <summary>
    /// Stores link-reference definitions in document order.
    /// </summary>
    public List<MarkdownLinkReferenceInfo> LinkReferenceDefinitions;
    /// <summary>
    /// Stores captured front-matter blocks in document order.
    /// </summary>
    public List<MarkdownFrontMatterInfo> FrontMatter;
    /// <summary>
    /// Stores the generated Markdown table of contents.
    /// </summary>
    public string TableOfContents;
    /// <summary>
    /// Stores the plain text value used by this instance.
    /// </summary>
    public string PlainText;
    /// <summary>
    /// Stores the node count value used by this instance.
    /// </summary>
    public int NodeCount;
    /// <summary>
    /// Stores the document count value used by this instance.
    /// </summary>
    public int DocumentCount;
    /// <summary>
    /// Stores the block count value used by this instance.
    /// </summary>
    public int BlockCount;
    /// <summary>
    /// Stores the inline count value used by this instance.
    /// </summary>
    public int InlineCount;
    /// <summary>
    /// Stores the extension count value used by this instance.
    /// </summary>
    public int ExtensionCount;
    /// <summary>
    /// Stores the text node count value used by this instance.
    /// </summary>
    public int TextNodeCount;
    /// <summary>
    /// Stores the word count value used by this instance.
    /// </summary>
    public int WordCount;
    /// <summary>
    /// Stores the character count value used by this instance.
    /// </summary>
    public int CharacterCount;
    /// <summary>
    /// Stores the estimated reading time in seconds.
    /// </summary>
    public int ReadingTimeSeconds;
    /// <summary>
    /// Stores the estimated reading time rounded up to whole minutes.
    /// </summary>
    public int ReadingTimeMinutes;

    /// <summary>
    /// Initializes a new <see cref="MarkdownDocumentAnalysis"/> instance.
    /// </summary>
    public MarkdownDocumentAnalysis()
    {
        Headings = new List<MarkdownHeadingInfo>();
        Links = new List<MarkdownLinkInfo>();
        Images = new List<MarkdownImageInfo>();
        HeadingOutline = new List<MarkdownHeadingOutlineItem>();
        LinkReferenceDefinitions = new List<MarkdownLinkReferenceInfo>();
        FrontMatter = new List<MarkdownFrontMatterInfo>();
        TableOfContents = "";
        PlainText = "";
        NodeCount = 0;
        DocumentCount = 0;
        BlockCount = 0;
        InlineCount = 0;
        ExtensionCount = 0;
        TextNodeCount = 0;
        WordCount = 0;
        CharacterCount = 0;
        ReadingTimeSeconds = 0;
        ReadingTimeMinutes = 0;
    }

    /// <summary>
    /// Returns heading count.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetHeadingCount()
    {
        return Headings.Count;
    }

    /// <summary>
    /// Returns link count.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetLinkCount()
    {
        return Links.Count;
    }

    /// <summary>
    /// Returns image count.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetImageCount()
    {
        return Images.Count;
    }

    /// <summary>
    /// Returns headings.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownHeadingInfo[] GetHeadings()
    {
        MarkdownHeadingInfo[] headings = new MarkdownHeadingInfo[Headings.Count];
        int index = 0;
        while (index < Headings.Count)
        {
            headings[index] = Headings[index].Copy();
            index = index + 1;
        }

        return headings;
    }

    /// <summary>
    /// Returns links.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownLinkInfo[] GetLinks()
    {
        MarkdownLinkInfo[] links = new MarkdownLinkInfo[Links.Count];
        int index = 0;
        while (index < Links.Count)
        {
            links[index] = Links[index].Copy();
            index = index + 1;
        }

        return links;
    }

    /// <summary>
    /// Returns images.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownImageInfo[] GetImages()
    {
        MarkdownImageInfo[] images = new MarkdownImageInfo[Images.Count];
        int index = 0;
        while (index < Images.Count)
        {
            images[index] = Images[index].Copy();
            index = index + 1;
        }

        return images;
    }
}
