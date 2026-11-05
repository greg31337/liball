namespace fake;

/// <summary>Represents a correlated fictional flight fixture.</summary>
/// <remarks>Codes, flight numbers, seats, and record locators are synthetic shapes and do not claim assignment, booking, operation, or airport service.</remarks>
public class FlightProfile
{
    /// <summary>The project-fictional airline name.</summary>
    public string AirlineName = string.Empty;

    /// <summary>The synthetic two-character airline-designator shape.</summary>
    public string AirlineCode = string.Empty;

    /// <summary>The fictional origin airport name.</summary>
    public string OriginAirportName = string.Empty;

    /// <summary>The synthetic three-letter origin airport-code shape.</summary>
    public string OriginAirportCode = string.Empty;

    /// <summary>The fictional destination airport name.</summary>
    public string DestinationAirportName = string.Empty;

    /// <summary>The synthetic three-letter destination airport-code shape.</summary>
    public string DestinationAirportCode = string.Empty;

    /// <summary>The project-fictional aircraft name.</summary>
    public string AircraftName = string.Empty;

    /// <summary>The synthetic aircraft-designator shape.</summary>
    public string AircraftCode = string.Empty;

    /// <summary>The flight number beginning with <c>AirlineCode</c>.</summary>
    public string FlightNumber = string.Empty;

    /// <summary>The seat correlated with <c>Cabin</c>.</summary>
    public string Seat = string.Empty;

    /// <summary>The synthetic reservation record-locator shape.</summary>
    public string RecordLocator = string.Empty;

    /// <summary>The passenger cabin category.</summary>
    public string Cabin = string.Empty;

    /// <summary>The route composed from the distinct airport codes.</summary>
    public string Route = string.Empty;

    /// <summary>The departure instant expressed in UTC.</summary>
    public System.DateTimeOffset Departure;

    /// <summary>The arrival instant expressed in UTC.</summary>
    public System.DateTimeOffset Arrival;

    /// <summary>The positive elapsed flight time in minutes.</summary>
    public int DurationMinutes;

    /// <summary>Whether the profile is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the airline designator claims assignment, always <c>false</c>.</summary>
    public bool AirlineCodeIsAssigned;

    /// <summary>Whether the airport codes claim assignment, always <c>false</c>.</summary>
    public bool AirportCodesAreAssigned;

    /// <summary>Whether the record locator represents a booking, always <c>false</c>.</summary>
    public bool RecordLocatorIsBooked;

    internal FlightProfile()
    {
    }
}
