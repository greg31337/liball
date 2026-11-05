using System;
using System.IO;

namespace markdown;

internal class DebugTreeRenderContext
{
    internal TextWriter Writer;
    internal MarkdownOutputSizeLimiter OutputSizeLimiter;
    internal MarkdownPipeline Pipeline;
    internal MarkdownOptions Options;

    internal DebugTreeRenderContext(TextWriter writer, MarkdownPipeline pipeline)
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
        OutputSizeLimiter = new MarkdownOutputSizeLimiter(
            pipeline.GetOptions().MaximumOutputSize);
        Pipeline = pipeline.Copy();
        Options = pipeline.GetOptions();
    }

    internal void Write(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        OutputSizeLimiter.Add(text);
        Writer.Write(text);
    }
}
