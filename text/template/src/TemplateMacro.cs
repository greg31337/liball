using System;
using System.Collections.Generic;

namespace template;

internal class TemplateMacroFunction
{
    internal TemplateRenderer Renderer;
    internal TemplateBlockSyntax Block;
    internal TemplateRenderContext Context;

    internal TemplateMacroFunction(TemplateRenderer renderer, TemplateBlockSyntax block, TemplateRenderContext context)
    {
        if (renderer == null)
        {
            throw new ArgumentNullException("renderer");
        }

        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Renderer = renderer;
        Block = block;
        Context = context;
    }

    internal TemplateValue Invoke(List<TemplateValue> arguments)
    {
        return Renderer.RenderMacroToValue(Block, Context, arguments);
    }
}
