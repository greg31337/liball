namespace htmlgui;

/// <summary>Reports an unrecoverable data-binding operation failure.</summary>
public class UiBindingException : System.Exception
{
    /// <summary>The stable binding identifier.</summary>
    public string BindingId;
    /// <summary>The stable target node identifier.</summary>
    public string TargetNodeId;

    /// <summary>Creates a binding exception with binding and target context.</summary>
    /// <param name="message">The failure description.</param>
    /// <param name="bindingId">The stable binding identifier.</param>
    /// <param name="targetNodeId">The stable target node identifier.</param>
    public UiBindingException(string message, string bindingId, string targetNodeId)
        : base($"{Guard.ArgumentNotWhiteSpace(message, "message")} (binding '{Guard.ArgumentNotWhiteSpace(bindingId, "bindingId")}', target '{Guard.ArgumentNotWhiteSpace(targetNodeId, "targetNodeId")}').")
    {
        BindingId = bindingId;
        TargetNodeId = targetNodeId;
    }
}
