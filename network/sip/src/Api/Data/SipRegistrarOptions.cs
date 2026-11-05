namespace sip;

/// <summary>Holds registrar validation, expiry, and capacity policy.</summary>
public class SipRegistrarOptions
{
    /// <summary>The default binding expiry in seconds.</summary>
    public int DefaultExpiresSeconds = SipDefaults.RegistrationExpiresSeconds;
    /// <summary>The minimum binding expiry in seconds.</summary>
    public int MinExpiresSeconds = 60;
    /// <summary>The maximum binding expiry in seconds.</summary>
    public int MaxExpiresSeconds = 86400;
    /// <summary>The maximum bindings for one address of record.</summary>
    public int MaxBindingCount = SipDefaults.MaxRegistrationBindingCount;
    /// <summary>Requires authentication.</summary>
    public bool RequireAuthentication = true;
    /// <summary>Allows third-party registrations.</summary>
    public bool AllowThirdPartyRegistration;
    /// <summary>The served domains.</summary>
    public string[] Domains = new string[0];
}
