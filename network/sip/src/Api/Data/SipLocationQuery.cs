namespace sip;

/// <summary>Describes a location-service binding query.</summary>
public class SipLocationQuery
{
    /// <summary>The address of record.</summary>
    public SipUri? AddressOfRecord;
    /// <summary>The optional instance identifier filter.</summary>
    public string? InstanceId;
    /// <summary>The optional registration identifier filter.</summary>
    public int RegistrationId = -1;
    /// <summary>The monotonic query timestamp.</summary>
    public long NowMilliseconds = -1;
}
