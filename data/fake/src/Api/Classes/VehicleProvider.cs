using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides vehicle and transport generation operations.
/// </summary>
public class VehicleProvider : IVehicleProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal VehicleProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("vehicle");
    }

    /// <summary>Selects a project-fictional vehicle make.</summary>
    /// <returns>A fictional manufacturer name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Make()
    {
        return VehicleGenerator.Make(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional vehicle model.</summary>
    /// <returns>A fictional model name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Model()
    {
        return VehicleGenerator.Model(Context, RandomValue);
    }

    /// <summary>Selects a broad motor-vehicle body type.</summary>
    /// <returns>A body-type name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Type()
    {
        return VehicleGenerator.Type(Context, RandomValue);
    }

    /// <summary>Selects a fuel or propulsion type.</summary>
    /// <returns>A conventional propulsion category.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Fuel()
    {
        return VehicleGenerator.Fuel(Context, RandomValue);
    }

    /// <summary>Selects a broad bicycle type.</summary>
    /// <returns>A conventional bicycle category.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Bicycle()
    {
        return VehicleGenerator.Bicycle(Context, RandomValue);
    }

    /// <summary>Generates a conspicuously synthetic registration-plate shape.</summary>
    /// <remarks>The value begins with <c>TEST-</c> and does not claim assignment by a jurisdiction.</remarks>
    /// <returns>A synthetic registration string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string RegistrationPlate()
    {
        return VehicleGenerator.RegistrationPlate(Context, RandomValue);
    }

    /// <summary>Generates a conspicuously synthetic registration-plate shape.</summary>
    /// <remarks>This is an alias for <see cref="RegistrationPlate()"/>.</remarks>
    /// <returns>A synthetic registration string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string Plate()
    {
        return RegistrationPlate();
    }

    /// <summary>Generates a checksum-valid synthetic VIN shape.</summary>
    /// <remarks>The captured reference year is clamped to the unambiguous 1980 through 2039 VIN model-year window. The WMI is random and no assignment is claimed.</remarks>
    /// <returns>A 17-character VIN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Vin()
    {
        return VehicleGenerator.Vin(Context, RandomValue);
    }

    /// <summary>Generates a checksum-valid synthetic VIN shape for a model year.</summary>
    /// <remarks>The WMI is random and no assignment, manufacture, or vehicle existence is claimed.</remarks>
    /// <param name="modelYear">A model year from 1980 through 2039.</param>
    /// <returns>A 17-character VIN shape encoding the selected model year.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when modelYear or a configured limit is invalid.</exception>
    public string VinFromModelYear(int modelYear)
    {
        return VehicleGenerator.Vin(Context, RandomValue, modelYear);
    }

    /// <summary>Selects a project-fictional route name.</summary>
    /// <returns>A generic route name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Route()
    {
        return VehicleGenerator.Route(Context, RandomValue);
    }

    /// <summary>Selects a broad public-transit mode.</summary>
    /// <returns>A conventional transit category.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string TransitMode()
    {
        return VehicleGenerator.TransitMode(Context, RandomValue);
    }

    /// <summary>Generates a correlated fictional motor-vehicle profile.</summary>
    /// <remarks>The VIN and registration are synthetic shapes and explicitly do not claim external assignment.</remarks>
    /// <returns>Correlated make, model, type, fuel, year, VIN, registration, color, route, and transit data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public VehicleProfile Record()
    {
        return VehicleGenerator.Record(Context, RandomValue);
    }
}
