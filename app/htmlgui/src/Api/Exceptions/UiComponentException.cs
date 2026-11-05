namespace htmlgui;

/// <summary>Reports an unrecoverable component runtime or lifecycle failure.</summary>
public class UiComponentException : System.Exception
{
    /// <summary>The stable component instance identifier.</summary>
    public string ComponentId;
    /// <summary>The lifecycle stage at which the failure occurred.</summary>
    public UiLifecycleStage Stage;

    /// <summary>Creates a component exception with component and lifecycle context.</summary>
    /// <param name="message">The failure description.</param>
    /// <param name="componentId">The stable component identifier.</param>
    /// <param name="stage">The lifecycle stage.</param>
    public UiComponentException(string message, string componentId, UiLifecycleStage stage)
        : base($"{Guard.ArgumentNotWhiteSpace(message, "message")} (component '{Guard.ArgumentNotWhiteSpace(componentId, "componentId")}', lifecycle '{stage}').")
    {
        ComponentId = componentId;
        Stage = stage;
    }
}
