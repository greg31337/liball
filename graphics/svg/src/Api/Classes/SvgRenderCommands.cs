namespace svg;

/// <summary>Enumerates balanced deterministic backend-neutral commands from a planned scene.</summary>
public class SvgRenderCommandEnumerator : ISvgRenderCommandEnumerator
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgRenderCommandEnumerator()
    {
    }

    /// <summary>Enumerates and attaches commands using bounded defaults.</summary>
    /// <param name="scene">The current normalized and planned scene.</param>
    /// <returns>The complete balanced command list.</returns>
    public SvgRenderCommandList EnumerateWithScene(SvgScene scene)
    {
        return new SvgRenderCommandEnumerator().EnumerateWithSceneAndOptions(scene, new SvgRenderCommandOptions());
    }

    /// <summary>Enumerates commands using explicit bounds and attachment behavior.</summary>
    /// <param name="scene">The current normalized and planned scene.</param>
    /// <param name="options">The command enumeration options.</param>
    /// <returns>The complete balanced or failed command list.</returns>
    public SvgRenderCommandList EnumerateWithSceneAndOptions(SvgScene scene, SvgRenderCommandOptions options)
    {
        SvgRenderCommandList result = new SvgRenderCommandList(scene);
        if (!new SvgRenderCommandEnumerator().Valid(scene, options))
        {
            result.Error = SvgRenderCommandError.InvalidArgument;
            return result;
        }
        SvgRenderCommandState state = new SvgRenderCommandState(result, options);
        if (!new SvgRenderCommandEnumerator().Visit((scene.LayerPlan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Root, state))
        {
            result.Error = SvgRenderCommandError.LimitExceeded;
            return result;
        }
        if (!new SvgRenderCommandEnumerator().Balanced(result.Commands))
        {
            result.Error = SvgRenderCommandError.UnbalancedScopes;
            return result;
        }
        result.Success = true;
        if (options.AttachToScene) scene.RenderCommands = result;
        return result;
    }
bool Visit(SvgSceneLayer? layer, SvgRenderCommandState state)
    {
        if (layer == null || !layer.SubtreePaintable) return true;
        SvgSceneNode node = layer.Node;
        SvgRenderCommand transform = new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.PushTransform, layer);
        transform.Transform = node.AbsoluteTransform;
        if (!new SvgRenderCommandEnumerator().Emit(transform, state)) return false;
        int clipCount = 0;
        if (layer.OwnClipBounds != null)
        {
            SvgRenderCommand clip = new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.PushClip, layer);
            clip.Bounds = layer.OwnClipBounds;
            if (!new SvgRenderCommandEnumerator().Emit(clip, state)) return false;
            clipCount = clipCount + 1;
        }
        if (node.Clip != null)
        {
            SvgRenderCommand clip = new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.PushClip, layer);
            clip.Resource = node.Clip;
            clip.Bounds = node.WorldBounds;
            if (!new SvgRenderCommandEnumerator().Emit(clip, state)) return false;
            clipCount = clipCount + 1;
        }
        bool mask = node.Mask != null;
        if (mask)
        {
            SvgRenderCommand command = new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.PushMask, layer);
            command.Resource = node.Mask;
            command.Bounds = node.WorldBounds;
            if (!new SvgRenderCommandEnumerator().Emit(command, state)) return false;
        }
        if (layer.CreatesCompositingLayer)
        {
            SvgRenderCommand begin = new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.BeginLayer, layer);
            begin.Bounds = layer.PaintBounds;
            begin.Resource = node.Filter;
            begin.Opacity = layer.Opacity;
            begin.BlendMode = layer.BlendMode;
            begin.Isolated = layer.Isolated;
            if (!new SvgRenderCommandEnumerator().Emit(begin, state)) return false;
        }
        if (layer.Paintable && !new SvgRenderCommandEnumerator().Paint(node, layer, state)) return false;
        int childIndex = 0;
        while (childIndex < layer.Children.Count)
        {
            if (!new SvgRenderCommandEnumerator().Visit(layer.Children[childIndex], state)) return false;
            childIndex = childIndex + 1;
        }
        if (layer.CreatesCompositingLayer && !new SvgRenderCommandEnumerator().Emit(new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.EndLayer, layer), state)) return false;
        if (mask && !new SvgRenderCommandEnumerator().Emit(new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.PopMask, layer), state)) return false;
        while (clipCount > 0)
        {
            if (!new SvgRenderCommandEnumerator().Emit(new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.PopClip, layer), state)) return false;
            clipCount = clipCount - 1;
        }
        return new SvgRenderCommandEnumerator().Emit(new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.PopTransform, layer), state);
    }
bool Paint(SvgSceneNode node, SvgSceneLayer layer, SvgRenderCommandState state)
    {
        if (node.Kind == SvgSceneNodeKind.Path || node.Kind == SvgSceneNodeKind.Text)
        {
            int index = 0;
            while (index < layer.PaintOrder.Count)
            {
                SvgPaintOrderComponent component = layer.PaintOrder[index];
                if (component == SvgPaintOrderComponent.Fill && node.Fill != null && node.FillOpacity > 0)
                {
                    SvgRenderCommandKind kind = node.Kind == SvgSceneNodeKind.Path ? SvgRenderCommandKind.FillPath : SvgRenderCommandKind.FillText;
                    SvgRenderCommand command = new SvgRenderCommandEnumerator().Command(kind, layer);
                    command.Resource = node.Fill;
                    command.Opacity = node.FillOpacity;
                    command.Bounds = node.FillBounds;
                    if (!new SvgRenderCommandEnumerator().Emit(command, state)) return false;
                }
                if (component == SvgPaintOrderComponent.Stroke && node.Stroke != null && node.StrokeOpacity > 0)
                {
                    SvgRenderCommandKind kind = node.Kind == SvgSceneNodeKind.Path ? SvgRenderCommandKind.StrokePath : SvgRenderCommandKind.StrokeText;
                    SvgRenderCommand command = new SvgRenderCommandEnumerator().Command(kind, layer);
                    command.Resource = node.Stroke;
                    command.Opacity = node.StrokeOpacity;
                    command.Bounds = node.StrokeBounds;
                    if (!new SvgRenderCommandEnumerator().Emit(command, state)) return false;
                }
                if (component == SvgPaintOrderComponent.Markers && node.Kind == SvgSceneNodeKind.Path && (node.MarkerStart != null || node.MarkerMid != null || node.MarkerEnd != null))
                {
                    SvgRenderCommand command = new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.DrawMarkers, layer);
                    command.MarkerStart = node.MarkerStart;
                    command.MarkerMid = node.MarkerMid;
                    command.MarkerEnd = node.MarkerEnd;
                    command.Bounds = node.MarkerBounds;
                    if (!new SvgRenderCommandEnumerator().Emit(command, state)) return false;
                }
                index = index + 1;
            }
            return true;
        }
        if (node.Kind == SvgSceneNodeKind.Image)
        {
            SvgRenderCommand command = new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.DrawImage, layer);
            command.Resource = (node.Image ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Resource;
            command.Bounds = node.FillBounds;
            return new SvgRenderCommandEnumerator().Emit(command, state);
        }
        if (node.Kind == SvgSceneNodeKind.HostContent)
        {
            SvgRenderCommand command = new SvgRenderCommandEnumerator().Command(SvgRenderCommandKind.DrawHostContent, layer);
            command.Bounds = node.FillBounds;
            return new SvgRenderCommandEnumerator().Emit(command, state);
        }
        return true;
    }
SvgRenderCommand Command(SvgRenderCommandKind kind, SvgSceneLayer layer)
    {
        return new SvgRenderCommand(kind, layer.Node, layer);
    }
bool Emit(SvgRenderCommand command, SvgRenderCommandState state)
    {
        if (state.Result.Commands.Count >= state.Options.MaximumCommands) return false;
        command.Sequence = state.Result.Commands.Count;
        state.Result.Commands.Add(command);
        return true;
    }
bool Balanced(System.Collections.Generic.List<SvgRenderCommand> commands)
    {
        System.Collections.Generic.List<SvgRenderCommandKind> stack = new System.Collections.Generic.List<SvgRenderCommandKind>();
        int index = 0;
        while (index < commands.Count)
        {
            SvgRenderCommandKind kind = commands[index].Kind;
            if (kind == SvgRenderCommandKind.PushTransform || kind == SvgRenderCommandKind.PushClip || kind == SvgRenderCommandKind.PushMask || kind == SvgRenderCommandKind.BeginLayer) stack.Add(kind);
            else if (kind == SvgRenderCommandKind.PopTransform)
            {
                if (!new SvgRenderCommandEnumerator().Pop(stack, SvgRenderCommandKind.PushTransform)) return false;
            }
            else if (kind == SvgRenderCommandKind.PopClip)
            {
                if (!new SvgRenderCommandEnumerator().Pop(stack, SvgRenderCommandKind.PushClip)) return false;
            }
            else if (kind == SvgRenderCommandKind.PopMask)
            {
                if (!new SvgRenderCommandEnumerator().Pop(stack, SvgRenderCommandKind.PushMask)) return false;
            }
            else if (kind == SvgRenderCommandKind.EndLayer)
            {
                if (!new SvgRenderCommandEnumerator().Pop(stack, SvgRenderCommandKind.BeginLayer)) return false;
            }
            index = index + 1;
        }
        return stack.Count == 0;
    }
bool Pop(System.Collections.Generic.List<SvgRenderCommandKind> stack, SvgRenderCommandKind expected)
    {
        if (stack.Count == 0 || stack[stack.Count - 1] != expected) return false;
        stack.RemoveAt(stack.Count - 1);
        return true;
    }
bool Valid(SvgScene? scene, SvgRenderCommandOptions? options)
    {
        if (scene == null || options == null || options.MaximumCommands < 1 || scene.LayerPlan == null || !scene.LayerPlan.Success || !object.ReferenceEquals(scene.LayerPlan.Scene, scene)) return false;
        return scene.IsCurrent();
    }
}
