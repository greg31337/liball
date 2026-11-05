namespace htmlgui;

/// <summary>Renders validated portable UI models into deterministic HTML, CSS, hydration, and snapshot output.</summary>
public class UiRenderer : IUiRenderer
{
    internal RendererPipeline Pipeline;

    /// <summary>Initializes a UI renderer.</summary>
    public UiRenderer()
    {
        Pipeline = new RendererPipeline();
    }

    /// <summary>Renders the active window and current page as a complete HTML document.</summary>
    public UiRenderResult RenderDocument(UiApplication? application)
    {
        return Pipeline.RenderPage(application, true);
    }

    /// <summary>Renders the active window and current page without document chrome.</summary>
    public UiRenderResult RenderFragment(UiApplication? application)
    {
        return Pipeline.RenderPage(application, false);
    }

    /// <summary>Renders only the styles associated with the active window and current page.</summary>
    public UiRenderResult RenderStyles(UiApplication? application)
    {
        return Pipeline.RenderStylesOnly(application);
    }

    /// <summary>Loads the deterministic browser bootstrap source.</summary>
    public UiRenderResult RenderBootstrap()
    {
        return Pipeline.RenderBootstrapOnly();
    }
}
