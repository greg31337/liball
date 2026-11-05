namespace cbor;

/// <summary>Associates a CDDL control-operator name with an application handler.</summary>
public class CddlControlRegistration
{
    /// <summary>The control name including its leading period.</summary>
    public string Name = "";
    /// <summary>The application control handler.</summary>
    public ICddlControlHandler? Handler;
    /// <summary>Indicates whether the registration is enabled.</summary>
    public bool IsEnabled = true;
}
