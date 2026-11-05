namespace sip;

/// <summary>Provides standard SIP event package names supported by the package registry.</summary>
public static class SipEventPackages
{
    /// <summary>The presence event package.</summary>
    public const string Presence = "presence";
    /// <summary>The presence watcher-information event package.</summary>
    public const string WatcherInformation = "presence.winfo";
    /// <summary>The dialog event package.</summary>
    public const string Dialog = "dialog";
    /// <summary>The registration-state event package.</summary>
    public const string Registration = "reg";
    /// <summary>The message-summary event package.</summary>
    public const string MessageSummary = "message-summary";
    /// <summary>The generic resource-list event package.</summary>
    public const string ResourceList = "resource-list";
}
