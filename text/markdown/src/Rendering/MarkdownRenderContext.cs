using System;

namespace markdown;

internal class MarkdownRenderContext
{
    internal MarkdownWriter Writer;
    internal MarkdownPipeline Pipeline;
    internal MarkdownOptions Options;

    internal MarkdownRenderContext(MarkdownWriter writer, MarkdownPipeline pipeline)
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
