namespace svg;

/// <summary>Configures bounded render-command enumeration and scene attachment.</summary>
public class SvgRenderCommandOptions
{
    /// <summary>The maximum generated command count.</summary>
    public int MaximumCommands;
    /// <summary>Whether a successful list is attached to the source scene.</summary>
    public bool AttachToScene;

    /// <summary>Creates deterministic bounded command-enumeration defaults.</summary>
    public SvgRenderCommandOptions()
    {
        MaximumCommands = 1000000;
        AttachToScene = true;
    }
}

/// <summary>Contains one tagged backend-neutral render command.</summary>
public class SvgRenderCommand
{
    /// <summary>The zero-based deterministic sequence number.</summary>
    public int Sequence;
    /// <summary>The operation kind.</summary>
    public SvgRenderCommandKind Kind;
    /// <summary>The corresponding scene node.</summary>
    public SvgSceneNode Node;
    /// <summary>The corresponding layer-plan record.</summary>
    public SvgSceneLayer Layer;
    /// <summary>The absolute transform for a transform push, or <c>null</c>.</summary>
    public SvgMatrix? Transform;
    /// <summary>The world-space operation bounds, or <c>null</c>.</summary>
    public SvgRect? Bounds;
    /// <summary>The shared paint, image, clip, mask, or filter resource, or <c>null</c>.</summary>
    public SvgSceneResource? Resource;
    /// <summary>The shared start-marker resource, or <c>null</c>.</summary>
    public SvgSceneResource? MarkerStart;
    /// <summary>The shared middle-marker resource, or <c>null</c>.</summary>
    public SvgSceneResource? MarkerMid;
    /// <summary>The shared end-marker resource, or <c>null</c>.</summary>
    public SvgSceneResource? MarkerEnd;
    /// <summary>The operation opacity.</summary>
    public double Opacity;
    /// <summary>The layer blend mode.</summary>
    public string BlendMode;
    /// <summary>Whether a layer begins an isolated stacking context.</summary>
    public bool Isolated;

    internal SvgRenderCommand(SvgRenderCommandKind kind, SvgSceneNode node, SvgSceneLayer layer)
    {
        Sequence = 0;
        Kind = kind;
        Node = node;
        Layer = layer;
        Transform = null;
        Bounds = null;
        Resource = null;
        MarkerStart = null;
        MarkerMid = null;
        MarkerEnd = null;
        Opacity = 1;
        BlendMode = "normal";
        Isolated = false;
    }
}

/// <summary>Contains deterministic backend-neutral commands and structural validation state.</summary>
public class SvgRenderCommandList
{
    /// <summary>The source scene.</summary>
    public SvgScene Scene;
    /// <summary>The commands in deterministic execution order.</summary>
    public System.Collections.Generic.List<SvgRenderCommand> Commands;
    /// <summary>Whether complete balanced enumeration succeeded.</summary>
    public bool Success;
    /// <summary>The stable enumeration failure.</summary>
    public SvgRenderCommandError Error;

    internal SvgRenderCommandList(SvgScene scene)
    {
        Scene = scene;
        Commands = new System.Collections.Generic.List<SvgRenderCommand>();
        Success = false;
        Error = SvgRenderCommandError.None;
    }

    /// <summary>Returns commands belonging to one exact scene node.</summary>
    /// <param name="node">The exact scene node.</param>
    /// <returns>The matching commands in execution order.</returns>
    internal System.Collections.Generic.List<SvgRenderCommand> Find(SvgSceneNode node)
    {
        System.Collections.Generic.List<SvgRenderCommand> result = new System.Collections.Generic.List<SvgRenderCommand>();
        int index = 0;
        while (index < Commands.Count)
        {
            if (object.ReferenceEquals(Commands[index].Node, node)) result.Add(Commands[index]);
            index = index + 1;
        }
        return result;
    }
}
