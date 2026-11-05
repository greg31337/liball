using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides airline and flight generation operations.
/// </summary>
public class AirlineProvider : IAirlineProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal AirlineProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("airline");
    }

    /// <summary>Selects a project-fictional airline name.</summary>
    /// <returns>A fictional carrier name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Airline()
    {
        return AirlineGenerator.AirlineName(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional airline name.</summary>
    /// <returns>A fictional carrier name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string AirlineName()
    {
        return AirlineGenerator.AirlineName(Context, RandomValue);
    }

    /// <summary>Selects a synthetic two-character airline-designator shape.</summary>
    /// <remarks>The value does not claim assignment by an aviation authority or industry organization.</remarks>
    /// <returns>An uppercase letter followed by a digit.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string AirlineCode()
    {
        return AirlineGenerator.AirlineCode(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional airport name.</summary>
    /// <returns>A fictional airport name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Airport()
    {
        return AirlineGenerator.AirportName(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional airport name.</summary>
    /// <returns>A fictional airport name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string AirportName()
    {
        return AirlineGenerator.AirportName(Context, RandomValue);
    }

    /// <summary>Selects a synthetic three-letter airport-code shape.</summary>
    /// <remarks>The value does not claim assignment or airport service.</remarks>
    /// <returns>A three-letter uppercase code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string AirportCode()
    {
        return AirlineGenerator.AirportCode(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional aircraft name.</summary>
    /// <returns>A fictional aircraft model name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Aircraft()
    {
        return AirlineGenerator.AircraftName(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional aircraft name.</summary>
    /// <returns>A fictional aircraft model name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string AircraftName()
    {
        return AirlineGenerator.AircraftName(Context, RandomValue);
    }

    /// <summary>Selects a synthetic aircraft-designator shape.</summary>
    /// <remarks>The value does not claim assignment by an aviation authority.</remarks>
    /// <returns>One uppercase letter followed by three digits.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string AircraftCode()
    {
        return AirlineGenerator.AircraftCode(Context, RandomValue);
    }

    /// <summary>Generates a synthetic flight-number shape.</summary>
    /// <remarks>The airline designator is fictional and the number does not represent an operated flight.</remarks>
    /// <returns>An airline code, a space, and a positive number.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string FlightNumber()
    {
        return AirlineGenerator.FlightNumber(Context, RandomValue);
    }

    /// <summary>Generates a seat in a randomly selected cabin.</summary>
    /// <returns>A cabin-consistent row number and seat letter.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Seat()
    {
        return AirlineGenerator.Seat(Context, RandomValue);
    }

    /// <summary>Generates a seat in the requested cabin.</summary>
    /// <param name="cabin">Economy, premium economy, business, or first.</param>
    /// <returns>A cabin-consistent row number and seat letter.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when cabin is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when cabin is unsupported.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SeatFromCabin(string cabin)
    {
        return AirlineGenerator.Seat(Context, RandomValue, cabin);
    }

    /// <summary>Generates a synthetic reservation record-locator shape.</summary>
    /// <remarks>The value is not booked or registered with a reservation system.</remarks>
    /// <returns>Six unambiguous uppercase letters or digits.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string RecordLocator()
    {
        return AirlineGenerator.RecordLocator(Context, RandomValue);
    }

    /// <summary>Selects a passenger cabin category.</summary>
    /// <returns>Economy, premium economy, business, or first.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Cabin()
    {
        return AirlineGenerator.Cabin(Context, RandomValue);
    }

    /// <summary>Generates a route between distinct fictional airport codes.</summary>
    /// <returns>Origin and destination codes separated by a hyphen.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Route()
    {
        return AirlineGenerator.Route(Context, RandomValue);
    }

    /// <summary>Generates a correlated fictional flight profile.</summary>
    /// <remarks>No code, number, seat, or locator claims assignment, booking, or actual operation.</remarks>
    /// <returns>Correlated airline, airport, aircraft, cabin, seat, route, and schedule data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public FlightProfile Record()
    {
        return AirlineGenerator.Record(Context, RandomValue);
    }

    /// <summary>Generates a correlated fictional flight profile.</summary>
    /// <remarks>This is an alias for <see cref="Record()"/>.</remarks>
    /// <returns>Correlated airline, airport, aircraft, cabin, seat, route, and schedule data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public FlightProfile Flight()
    {
        return Record();
    }
}
