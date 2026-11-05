using System;
using System.Collections.Generic;

namespace template;

internal class TemplateBlockOverride
{
    internal string Name;
    internal TemplateBlockSyntax Block;
    internal TemplateBlockOverride? Parent;

    internal TemplateBlockOverride(string name, TemplateBlockSyntax block)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        Name = name;
        Block = block;
        Parent = null;
    }

    internal TemplateBlockOverride Clone()
    {
        TemplateBlockOverride clone = new TemplateBlockOverride(Name, Block);
        if (Parent != null)
        {
            clone.Parent = Parent.Clone();
        }

        return clone;
    }

    internal void AppendParent(TemplateBlockOverride parent)
    {
        if (parent == null)
        {
            throw new ArgumentNullException("parent");
        }

        TemplateBlockOverride current = this;
        while (current.Parent != null)
        {
            current = current.Parent;
        }

        current.Parent = parent;
    }
}

internal class TemplateSuperFunction
{
    internal TemplateRenderer Renderer;
    internal TemplateBlockOverride BlockOverride;
    internal TemplateBlockSyntax ParentBlock;
    internal TemplateRenderContext Context;

    internal TemplateSuperFunction(TemplateRenderer renderer, TemplateBlockOverride blockOverride, TemplateBlockSyntax parentBlock, TemplateRenderContext context)
    {
        if (renderer == null)
        {
            throw new ArgumentNullException("renderer");
        }

        if (blockOverride == null)
        {
            throw new ArgumentNullException("blockOverride");
        }

        if (parentBlock == null)
        {
            throw new ArgumentNullException("parentBlock");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Renderer = renderer;
        BlockOverride = blockOverride;
        ParentBlock = parentBlock;
        Context = context;
    }

    internal TemplateValue Invoke(List<TemplateValue> arguments)
    {
        return TemplateValue.FromSafeString(Renderer.RenderSuperBlockToString(BlockOverride, ParentBlock, Context));
    }
}
