namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgRenderer"/>.</summary>
public interface ISvgRenderer
{
    /// <summary>Resolves and renders an editable document using deterministic defaults.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <returns>The complete raster result.</returns>
    SvgRenderResult RenderWithSvgDocument(SvgDocument document);
    /// <summary>Resolves and renders an editable document at the requested viewport, scale, background, and time.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The complete raster result.</returns>
    SvgRenderResult RenderWithSvgDocumentAndSvgRenderOptions(SvgDocument? document, SvgRenderOptions options);
    /// <summary>Renders a current resolved document using deterministic defaults.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <returns>The complete raster result.</returns>
    SvgRenderResult RenderWithSvgResolvedDocument(SvgResolvedDocument document);
    /// <summary>Renders a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The complete raster result.</returns>
    SvgRenderResult RenderWithSvgResolvedDocumentAndSvgRenderOptions(SvgResolvedDocument document, SvgRenderOptions options);
    /// <summary>Renders a complete planned scene using deterministic defaults.</summary>
    /// <param name="scene">The current successful planned scene.</param>
    /// <returns>The complete raster result.</returns>
    SvgRenderResult RenderWithScene(SvgScene scene);
    /// <summary>Renders a complete current planned scene.</summary>
    /// <param name="scene">The current successful planned scene.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The complete raster result.</returns>
    SvgRenderResult RenderWithSceneAndOptions(SvgScene? scene, SvgRenderOptions options);
    /// <summary>Renders one fragment ID from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="fragment">The fragment identifier with or without a leading number sign.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderFragmentWithSvgResolvedDocumentAndString(SvgResolvedDocument document, string fragment);
    /// <summary>Resolves an editable document and renders one fragment ID using deterministic defaults.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="fragment">The fragment identifier with or without a leading number sign.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderFragmentWithSvgDocumentAndString(SvgDocument document, string fragment);
    /// <summary>Resolves an editable document and renders one fragment ID.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="fragment">The fragment identifier with or without a leading number sign.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderFragmentWithSvgDocumentAndStringAndSvgRenderOptions(SvgDocument document, string fragment, SvgRenderOptions options);
    /// <summary>Renders one fragment ID from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="fragment">The fragment identifier with or without a leading number sign.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderFragmentWithSvgResolvedDocumentAndStringAndSvgRenderOptions(SvgResolvedDocument document, string fragment, SvgRenderOptions options);
    /// <summary>Renders one authored source node from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderNodeWithSvgResolvedDocumentAndSvgNode(SvgResolvedDocument document, SvgNode source);
    /// <summary>Resolves an editable document and renders one authored source node using deterministic defaults.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderNodeWithSvgDocumentAndSvgNode(SvgDocument document, SvgNode source);
    /// <summary>Resolves an editable document and renders one authored source node.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderNodeWithSvgDocumentAndSvgNodeAndSvgRenderOptions(SvgDocument document, SvgNode source, SvgRenderOptions options);
    /// <summary>Renders one authored source node from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderNodeWithSvgResolvedDocumentAndSvgNodeAndSvgRenderOptions(SvgResolvedDocument document, SvgNode source, SvgRenderOptions options);
    /// <summary>Renders one exact use-instance path from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderInstanceWithSvgResolvedDocumentAndString(SvgResolvedDocument document, string instancePath);
    /// <summary>Resolves an editable document and renders one exact use-instance path using deterministic defaults.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderInstanceWithSvgDocumentAndString(SvgDocument document, string instancePath);
    /// <summary>Resolves an editable document and renders one exact use-instance path.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderInstanceWithSvgDocumentAndStringAndSvgRenderOptions(SvgDocument document, string instancePath, SvgRenderOptions options);
    /// <summary>Renders one exact use-instance path from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    SvgRenderResult RenderInstanceWithSvgResolvedDocumentAndStringAndSvgRenderOptions(SvgResolvedDocument document, string instancePath, SvgRenderOptions options);
}
