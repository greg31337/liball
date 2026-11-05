namespace iso20022;

/// <summary>Represents the four preserved components of an ISO 20022 message identifier.</summary>
public class MessageIdentifier
{
    /// <summary>The business-area component.</summary>
    public string BusinessArea = string.Empty;
    /// <summary>The message-functionality component with leading zeroes preserved.</summary>
    public string MessageFunctionality = string.Empty;
    /// <summary>The variant component with leading zeroes preserved.</summary>
    public string Variant = string.Empty;
    /// <summary>The version component with leading zeroes preserved.</summary>
    public string Version = string.Empty;
    /// <summary>The complete source identifier.</summary>
    public string Value = string.Empty;
}
