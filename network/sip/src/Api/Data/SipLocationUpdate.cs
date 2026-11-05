namespace sip;

/// <summary>Describes an atomic replacement of one address-of-record binding set.</summary>
public class SipLocationUpdate
{
    /// <summary>The address of record whose bindings are replaced.</summary>
    public SipUri? AddressOfRecord;
    /// <summary>The complete replacement binding set.</summary>
    public SipRegistrationBinding[] Bindings = new SipRegistrationBinding[0];
}
