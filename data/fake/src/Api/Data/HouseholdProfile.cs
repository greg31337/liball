namespace fake;

/// <summary>
/// Represents a correlated synthetic household and its members.
/// </summary>
public class HouseholdProfile
{
    /// <summary>The locale-provided household label consistent with the member count.</summary>
    public string HouseholdType = string.Empty;

    /// <summary>The number of people in <c>Members</c>.</summary>
    public int MemberCount;

    /// <summary>The independently generated household members.</summary>
    public PersonProfile[] Members = new PersonProfile[0];

    /// <summary>The fictional address shared by every member.</summary>
    public string Address = string.Empty;

    /// <summary>The synthetic phone number shared by every member.</summary>
    public string Phone = string.Empty;

    /// <summary>The canonical requested locale shared by every member.</summary>
    public string Locale = string.Empty;

    /// <summary>The IANA time-zone identifier shared by every member.</summary>
    public string TimeZoneId = string.Empty;

    internal HouseholdProfile()
    {
    }
}
