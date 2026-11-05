namespace fix;

/// <summary>Describes one dictionary-identified message delivered to a crack handler.</summary>
public class MessageCrackContext
{
    /// <summary>The message being dispatched.</summary>
    public FixMessage Message = new FixMessage();
    /// <summary>The matched dictionary definition.</summary>
    public FixMessageDefinition Definition = new FixMessageDefinition();
    /// <summary>The selected dictionary identity, scenario, and application version metadata.</summary>
    public FixDictionaryIdentity Identity = new FixDictionaryIdentity();
    /// <summary>The canonical dictionary message name.</summary>
    public string Name = "";
    /// <summary>The dictionary message category.</summary>
    public string Category = "";
    /// <summary>Whether the message is administrative.</summary>
    public bool IsAdministrative;
}
