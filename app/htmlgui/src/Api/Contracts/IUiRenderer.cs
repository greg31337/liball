namespace htmlgui;

/// <summary>Produces deterministic documents, fragments, styles, and browser bootstrap output.</summary>
public interface IUiRenderer
{
    /// <summary>Renders the active window and current page as a complete HTML document.</summary>
    /// <param name="application">The optional application model to render.</param>
    /// <returns>The complete deterministic render result or validation diagnostics.</returns>
    UiRenderResult RenderDocument(UiApplication? application);

    /// <summary>Renders the active window and current page without document chrome.</summary>
    /// <param name="application">The optional application model to render.</param>
    /// <returns>The deterministic fragment render result or validation diagnostics.</returns>
    UiRenderResult RenderFragment(UiApplication? application);

    /// <summary>Renders only the styles associated with the active window and current page.</summary>
    /// <param name="application">The optional application model whose styles are rendered.</param>
    /// <returns>The deterministic style result or validation diagnostics.</returns>
    UiRenderResult RenderStyles(UiApplication? application);

    /// <summary>Loads the deterministic browser bootstrap source.</summary>
    /// <returns>A render result containing only browser bootstrap output and its hash.</returns>
    UiRenderResult RenderBootstrap();
}
