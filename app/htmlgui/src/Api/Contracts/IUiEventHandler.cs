namespace htmlgui;

/// <summary>Receives normalized UI event callbacks without requiring public delegates or C# events.</summary>
public interface IUiEventHandler
{
    /// <summary>Handles one normalized event invocation.</summary>
    /// <param name="context">The event and current propagation context.</param>
    /// <returns>The handler outcome and propagation requests.</returns>
    UiEventHandlerResult HandleUiEvent(UiEventHandlerContext context);
}
