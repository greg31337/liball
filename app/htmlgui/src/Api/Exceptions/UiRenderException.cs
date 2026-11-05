namespace htmlgui;

/// <summary>Reports an unrecoverable deterministic rendering failure.</summary>
public class UiRenderException : System.Exception
{
    /// <summary>The stable identifier of the node being rendered.</summary>
    public string NodeId;
    /// <summary>The output kind being produced.</summary>
    public string OutputKind;

    /// <summary>Creates a rendering exception with node and output context.</summary>
    /// <param name="message">The failure description.</param>
    /// <param name="nodeId">The stable node identifier.</param>
    /// <param name="outputKind">The output kind.</param>
    public UiRenderException(string message, string nodeId, string outputKind)
        : base($"{Guard.ArgumentNotWhiteSpace(message, "message")} (node '{Guard.ArgumentNotWhiteSpace(nodeId, "nodeId")}', output '{Guard.ArgumentNotWhiteSpace(outputKind, "outputKind")}').")
    {
        NodeId = nodeId;
        OutputKind = outputKind;
    }
}
