namespace sip;

/// <summary>Holds REFER receiving, authentication, and subscription policy.</summary>
public class SipReferServerOptions
{
    /// <summary>The maximum REFER body size in bytes.</summary>
    public int MaxBodyBytes = SipDefaults.MaxBodyBytes;
    /// <summary>Requires configured authentication.</summary>
    public bool RequireAuthentication;
    /// <summary>Allows REFER requests within an existing dialog.</summary>
    public bool AllowInDialog = true;
    /// <summary>Allows REFER requests outside an existing dialog.</summary>
    public bool AllowOutOfDialog = true;
    /// <summary>Allows Refer-Sub false to suppress the implicit subscription.</summary>
    public bool AllowNoSubscription = true;
    /// <summary>The implicit subscription lifetime in seconds.</summary>
    public int SubscriptionExpiresSeconds = SipDefaults.SubscriptionExpiresSeconds;
    /// <summary>The URI schemes allowed in Refer-To, or an empty array to allow any valid scheme.</summary>
    public string[] AllowedReferToSchemes = new string[0];
}
