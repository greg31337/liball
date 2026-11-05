namespace htmlgui;

/// <summary>Registers UI event handlers and dispatches normalized events through their propagation paths.</summary>
public interface IUiEventDispatcher
{
    /// <summary>Registers an event binding and its callback for one stable node.</summary>
    /// <param name="nodeId">The stable node identifier.</param>
    /// <param name="binding">The event binding.</param>
    /// <param name="handler">The callback handler.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiResult RegisterEventHandler(string? nodeId, UiEventBinding? binding, IUiEventHandler? handler);

    /// <summary>Unregisters an event binding and its callback.</summary>
    /// <param name="bindingId">The stable binding identifier.</param>
    /// <returns>The exceptionless unregistration result.</returns>
    UiResult UnregisterEventHandler(string? bindingId);

    /// <summary>Normalizes and dispatches an event to matching handlers.</summary>
    /// <param name="eventData">The event data and target-to-root path.</param>
    /// <returns>The ordered callback and propagation result.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the event or path is malformed.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when the event is null.</exception>
    UiEventDispatchResult DispatchUiEvent(UiEventData? eventData);
}
