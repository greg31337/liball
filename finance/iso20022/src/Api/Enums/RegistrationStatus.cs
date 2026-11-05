namespace iso20022;

/// <summary>Identifies the registration lifecycle state of an ISO 20022 repository concept.</summary>
public enum RegistrationStatus
{
    /// <summary>The registration status is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>The concept is pending final registration approval.</summary>
    ProvisionallyRegistered = 1,
    /// <summary>The concept is registered and approved for use.</summary>
    Registered = 2,
    /// <summary>The concept remains registered historically but cannot be used in new developments.</summary>
    Obsolete = 3
}
