namespace icc;

/// <summary>Associates a Profile ID with localized descriptions.</summary>
public class IccProfileSequenceIdentifier
{
    /// <summary>Gets or sets the Profile ID.</summary>
    public IccProfileId ProfileId;
    /// <summary>Gets or sets localized profile descriptions.</summary>
    public IccLocalizedString[] Descriptions;

    /// <summary>Initializes an absent identifier.</summary>
    public IccProfileSequenceIdentifier()
    {
        ProfileId = new IccProfileId();
        Descriptions = new IccLocalizedString[0];
    }
}
