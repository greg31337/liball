using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="AirlineProvider"/>.</summary>
public interface IAirlineProvider
{
    /// <summary>Selects a project-fictional airline name.</summary>
    /// <returns>A fictional carrier name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Airline();
    /// <summary>Selects a project-fictional airline name.</summary>
    /// <returns>A fictional carrier name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string AirlineName();
    /// <summary>Selects a synthetic two-character airline-designator shape.</summary>
    /// <remarks>The value does not claim assignment by an aviation authority or industry organization.</remarks>
    /// <returns>An uppercase letter followed by a digit.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string AirlineCode();
    /// <summary>Selects a project-fictional airport name.</summary>
    /// <returns>A fictional airport name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Airport();
    /// <summary>Selects a project-fictional airport name.</summary>
    /// <returns>A fictional airport name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string AirportName();
    /// <summary>Selects a synthetic three-letter airport-code shape.</summary>
    /// <remarks>The value does not claim assignment or airport service.</remarks>
    /// <returns>A three-letter uppercase code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string AirportCode();
    /// <summary>Selects a project-fictional aircraft name.</summary>
    /// <returns>A fictional aircraft model name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Aircraft();
    /// <summary>Selects a project-fictional aircraft name.</summary>
    /// <returns>A fictional aircraft model name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string AircraftName();
    /// <summary>Selects a synthetic aircraft-designator shape.</summary>
    /// <remarks>The value does not claim assignment by an aviation authority.</remarks>
    /// <returns>One uppercase letter followed by three digits.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string AircraftCode();
    /// <summary>Generates a synthetic flight-number shape.</summary>
    /// <remarks>The airline designator is fictional and the number does not represent an operated flight.</remarks>
    /// <returns>An airline code, a space, and a positive number.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string FlightNumber();
    /// <summary>Generates a seat in a randomly selected cabin.</summary>
    /// <returns>A cabin-consistent row number and seat letter.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Seat();
    /// <summary>Generates a seat in the requested cabin.</summary>
    /// <param name="cabin">Economy, premium economy, business, or first.</param>
    /// <returns>A cabin-consistent row number and seat letter.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when cabin is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when cabin is unsupported.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string SeatFromCabin(string cabin);
    /// <summary>Generates a synthetic reservation record-locator shape.</summary>
    /// <remarks>The value is not booked or registered with a reservation system.</remarks>
    /// <returns>Six unambiguous uppercase letters or digits.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    string RecordLocator();
    /// <summary>Selects a passenger cabin category.</summary>
    /// <returns>Economy, premium economy, business, or first.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Cabin();
    /// <summary>Generates a route between distinct fictional airport codes.</summary>
    /// <returns>Origin and destination codes separated by a hyphen.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Route();
    /// <summary>Generates a correlated fictional flight profile.</summary>
    /// <remarks>No code, number, seat, or locator claims assignment, booking, or actual operation.</remarks>
    /// <returns>Correlated airline, airport, aircraft, cabin, seat, route, and schedule data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    FlightProfile Record();
    /// <summary>Generates a correlated fictional flight profile.</summary>
    /// <remarks>This is an alias for <see cref="Record()"/>.</remarks>
    /// <returns>Correlated airline, airport, aircraft, cabin, seat, route, and schedule data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    FlightProfile Flight();
}
