namespace sip;

/// <summary>Holds one ICE candidate signaling attribute without connectivity state.</summary>
public class SipIceCandidate
{
    /// <summary>The candidate foundation.</summary>
    public string? Foundation;
    /// <summary>The component identifier.</summary>
    public int Component = -1;
    /// <summary>The transport token.</summary>
    public string? Transport;
    /// <summary>The unsigned 32-bit candidate priority represented in a signed 64-bit value.</summary>
    public long Priority = -1;
    /// <summary>The candidate connection address.</summary>
    public string? Address;
    /// <summary>The candidate port.</summary>
    public int Port = -1;
    /// <summary>The candidate type token.</summary>
    public string? Type;
    /// <summary>The ordered extension name/value pairs.</summary>
    public string[] Extensions = new string[0];
}
