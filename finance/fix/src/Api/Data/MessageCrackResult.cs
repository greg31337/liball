namespace fix;

/// <summary>Contains the explicit outcome of dictionary-driven message dispatch.</summary>
public class MessageCrackResult
{
    /// <summary>Whether the message was identified and delivered to the selected handler method.</summary>
    public bool Success;
    /// <summary>The dictionary-identified dispatch context.</summary>
    public MessageCrackContext? Context;
    /// <summary>The handler's processing decision.</summary>
    public ApplicationDecision? Decision;
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
}
