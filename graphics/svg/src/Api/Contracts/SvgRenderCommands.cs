namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgRenderCommandEnumerator"/>.</summary>
public interface ISvgRenderCommandEnumerator
{
    /// <summary>Enumerates and attaches commands using bounded defaults.</summary>
    /// <param name="scene">The current normalized and planned scene.</param>
    /// <returns>The complete balanced command list.</returns>
    SvgRenderCommandList EnumerateWithScene(SvgScene scene);
    /// <summary>Enumerates commands using explicit bounds and attachment behavior.</summary>
    /// <param name="scene">The current normalized and planned scene.</param>
    /// <param name="options">The command enumeration options.</param>
    /// <returns>The complete balanced or failed command list.</returns>
    SvgRenderCommandList EnumerateWithSceneAndOptions(SvgScene scene, SvgRenderCommandOptions options);
}
