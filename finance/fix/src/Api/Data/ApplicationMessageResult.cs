namespace fix;

/// <summary>Contains the outcome and dictionary metadata of creating or identifying an application message.</summary>
public class ApplicationMessageResult
{
    /// <summary>Whether the application message was created or identified successfully.</summary>
    public bool Success;
    /// <summary>The created or inspected message.</summary>
    public FixMessage? Message;
    /// <summary>The matched dictionary message definition.</summary>
    public FixMessageDefinition? Definition;
    /// <summary>The selected dictionary identity, scenario, and application version metadata.</summary>
    public FixDictionaryIdentity Identity = new FixDictionaryIdentity();
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
}
