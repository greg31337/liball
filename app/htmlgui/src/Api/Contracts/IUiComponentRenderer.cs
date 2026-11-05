using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Renders copied component requests without requiring public delegates.</summary>
public interface IUiComponentRenderer
{
    /// <summary>Renders one component generation.</summary>
    /// <param name="request">The copied render request.</param>
    /// <returns>A task containing rendered content or diagnostics.</returns>
    Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request);
}
