using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Supplies an HTML rendering hook with its writer and effective pipeline settings.
/// </summary>
public class HtmlRenderContext
{
    /// <summary>
    /// The escaping, size-limited writer used by the active rendering operation.
    /// </summary>
    public HtmlWriter Writer;
    /// <summary>
    /// A snapshot of the pipeline used by the active rendering operation.
    /// </summary>
    public MarkdownPipeline Pipeline;
    /// <summary>
    /// A detached copy of the effective rendering options.
    /// </summary>
    public MarkdownOptions Options;
    internal List<string> FootnoteOrder;
    internal MarkdownRuleList RenderRules;

    internal HtmlRenderContext(HtmlWriter writer, MarkdownPipeline pipeline)
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
        FootnoteOrder = new List<string>();
        RenderRules = pipeline.GetHtmlRenderRules();
    }
}
