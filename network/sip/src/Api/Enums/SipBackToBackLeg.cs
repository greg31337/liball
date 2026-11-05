namespace sip;

/// <summary>Identifies one independently managed back-to-back session leg.</summary>
public enum SipBackToBackLeg
{
    /// <summary>No leg is selected.</summary>
    None = 0,
    /// <summary>The incoming or left session leg.</summary>
    Left = 1,
    /// <summary>The outgoing or right session leg.</summary>
    Right = 2
}
