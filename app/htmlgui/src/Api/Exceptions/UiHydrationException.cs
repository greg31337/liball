namespace htmlgui;

/// <summary>Reports an unrecoverable hydration comparison or repair failure.</summary>
public class UiHydrationException : System.Exception
{
    /// <summary>The stable identifier of the mismatched node.</summary>
    public string NodeId;
    /// <summary>The requested hydration mode.</summary>
    public UiHydrationMode Mode;
    /// <summary>The hydration status at failure.</summary>
    public UiHydrationStatus Status;

    /// <summary>Creates a hydration exception with node, mode, and status context.</summary>
    /// <param name="message">The failure description.</param>
    /// <param name="nodeId">The stable node identifier.</param>
    /// <param name="mode">The hydration mode.</param>
    /// <param name="status">The hydration status.</param>
    public UiHydrationException(string message, string nodeId, UiHydrationMode mode, UiHydrationStatus status)
        : base($"{Guard.ArgumentNotWhiteSpace(message, "message")} (node '{Guard.ArgumentNotWhiteSpace(nodeId, "nodeId")}', mode '{mode}', status '{status}').")
    {
        NodeId = nodeId;
        Mode = mode;
        Status = status;
    }
}
