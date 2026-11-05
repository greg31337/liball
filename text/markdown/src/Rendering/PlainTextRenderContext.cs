using System;

namespace markdown;

internal class PlainTextRenderContext
{
    internal PlainTextWriter Writer;
    internal MarkdownPipeline Pipeline;
    internal MarkdownOptions Options;

    internal PlainTextRenderContext(PlainTextWriter writer, MarkdownPipeline pipeline)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        Writer = writer;
        Pipeline = pipeline.Copy();
        Options = pipeline.GetOptions();
    }
}
