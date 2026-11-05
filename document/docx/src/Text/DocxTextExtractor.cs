namespace docx;

internal class DocxTextExtractor
{
    internal static string Extract(IDocxTextContent content)
    {
        DocxTextOptions options = new DocxTextOptions();
        return Extract(content, options);
    }

    internal static string Extract(IDocxTextContent content, DocxTextOptions options)
    {
        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return content.GetTextWithOptions(options);
    }

    internal static string ExtractTable(DocxTable table)
    {
        DocxTextOptions options = new DocxTextOptions();
        return ExtractTable(table, options);
    }

    internal static string ExtractTable(DocxTable table, DocxTextOptions options)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return table.GetTextWithOptions(options);
    }

    internal static string ExtractHeader(DocxHeader header)
    {
        DocxTextOptions options = new DocxTextOptions();
        return ExtractHeader(header, options);
    }

    internal static string ExtractHeader(DocxHeader header, DocxTextOptions options)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ExtractBlocks(header.Blocks, options);
    }

    internal static string ExtractFooter(DocxFooter footer)
    {
        DocxTextOptions options = new DocxTextOptions();
        return ExtractFooter(footer, options);
    }

    internal static string ExtractFooter(DocxFooter footer, DocxTextOptions options)
    {
        if (footer == null)
        {
            throw new ArgumentNullException("footer");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ExtractBlocks(footer.Blocks, options);
    }

    internal static string ExtractFootnotes(DocxFootnotes footnotes)
    {
        DocxTextOptions options = new DocxTextOptions();
        return ExtractFootnotes(footnotes, options);
    }

    internal static string ExtractFootnotes(DocxFootnotes footnotes, DocxTextOptions options)
    {
        if (footnotes == null)
        {
            throw new ArgumentNullException("footnotes");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = "";
        bool hasFootnote = false;

        foreach (DocxFootnote footnote in footnotes.Footnotes)
        {
            if (hasFootnote)
            {
                text = text + options.ParagraphSeparator;
            }

            text = text + ExtractFootnote(footnote, options);
            hasFootnote = true;
        }

        return text;
    }

    internal static string ExtractFootnote(DocxFootnote footnote)
    {
        DocxTextOptions options = new DocxTextOptions();
        return ExtractFootnote(footnote, options);
    }

    internal static string ExtractFootnote(DocxFootnote footnote, DocxTextOptions options)
    {
        if (footnote == null)
        {
            throw new ArgumentNullException("footnote");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ExtractBlocks(footnote.Blocks, options);
    }

    internal static string ExtractEndnotes(DocxEndnotes endnotes)
    {
        DocxTextOptions options = new DocxTextOptions();
        return ExtractEndnotes(endnotes, options);
    }

    internal static string ExtractEndnotes(DocxEndnotes endnotes, DocxTextOptions options)
    {
        if (endnotes == null)
        {
            throw new ArgumentNullException("endnotes");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = "";
        bool hasEndnote = false;

        foreach (DocxEndnote endnote in endnotes.Endnotes)
        {
            if (hasEndnote)
            {
                text = text + options.ParagraphSeparator;
            }

            text = text + ExtractEndnote(endnote, options);
            hasEndnote = true;
        }

        return text;
    }

    internal static string ExtractEndnote(DocxEndnote endnote)
    {
        DocxTextOptions options = new DocxTextOptions();
        return ExtractEndnote(endnote, options);
    }

    internal static string ExtractEndnote(DocxEndnote endnote, DocxTextOptions options)
    {
        if (endnote == null)
        {
            throw new ArgumentNullException("endnote");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ExtractBlocks(endnote.Blocks, options);
    }

    internal static string ExtractComments(DocxComments comments)
    {
        DocxTextOptions options = new DocxTextOptions();
        return ExtractComments(comments, options);
    }

    internal static string ExtractComments(DocxComments comments, DocxTextOptions options)
    {
        if (comments == null)
        {
            throw new ArgumentNullException("comments");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = "";
        bool hasComment = false;

        foreach (DocxComment comment in comments.Comments)
        {
            if (hasComment)
            {
                text = text + options.ParagraphSeparator;
            }

            text = text + ExtractComment(comment, options);
            hasComment = true;
        }

        return text;
    }

    internal static string ExtractComment(DocxComment comment)
    {
        DocxTextOptions options = new DocxTextOptions();
        return ExtractComment(comment, options);
    }

    internal static string ExtractComment(DocxComment comment, DocxTextOptions options)
    {
        if (comment == null)
        {
            throw new ArgumentNullException("comment");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ExtractBlocks(comment.Blocks, options);
    }

    internal static string ExtractBlocks(List<DocxBlock> blocks, DocxTextOptions options)
    {
        if (blocks == null)
        {
            throw new ArgumentNullException("blocks");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = "";
        bool hasBlock = false;

        foreach (DocxBlock block in blocks)
        {
            if (hasBlock)
            {
                text = text + options.ParagraphSeparator;
            }

            text = text + block.GetTextWithOptions(options);
            hasBlock = true;
        }

        return text;
    }
}
