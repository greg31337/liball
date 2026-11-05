namespace fake;

/// <summary>Contains correlated synthetic identifiers used by a person profile.</summary>
/// <remarks>The values do not identify an actual person and the contact value is not claimed to be reachable.</remarks>
public class PersonIdentifierRecord
{
    /// <summary>A random version 4 UUID used only as a synthetic profile identifier.</summary>
    public string ProfileId = string.Empty;

    /// <summary>The username derived from the profile's selected name.</summary>
    public string Username = string.Empty;

    /// <summary>The reserved-domain email address derived from <c>Username</c>.</summary>
    public string Email = string.Empty;

    /// <summary>Whether <c>Email</c> uses a reserved example domain, always <c>true</c>.</summary>
    public bool EmailIsReservedExample;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the record claims a reachable contact, always <c>false</c>.</summary>
    public bool RepresentsReachableContact;

    internal PersonIdentifierRecord()
    {
    }
}
