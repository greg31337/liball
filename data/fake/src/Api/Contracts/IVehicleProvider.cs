using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="VehicleProvider"/>.</summary>
public interface IVehicleProvider
{
    /// <summary>Selects a project-fictional vehicle make.</summary>
    /// <returns>A fictional manufacturer name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Make();
    /// <summary>Selects a project-fictional vehicle model.</summary>
    /// <returns>A fictional model name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Model();
    /// <summary>Selects a broad motor-vehicle body type.</summary>
    /// <returns>A body-type name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Type();
    /// <summary>Selects a fuel or propulsion type.</summary>
    /// <returns>A conventional propulsion category.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Fuel();
    /// <summary>Selects a broad bicycle type.</summary>
    /// <returns>A conventional bicycle category.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Bicycle();
    /// <summary>Generates a conspicuously synthetic registration-plate shape.</summary>
    /// <remarks>The value begins with <c>TEST-</c> and does not claim assignment by a jurisdiction.</remarks>
    /// <returns>A synthetic registration string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    string RegistrationPlate();
    /// <summary>Generates a conspicuously synthetic registration-plate shape.</summary>
    /// <remarks>This is an alias for <see cref="RegistrationPlate()"/>.</remarks>
    /// <returns>A synthetic registration string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    string Plate();
    /// <summary>Generates a checksum-valid synthetic VIN shape.</summary>
    /// <remarks>The captured reference year is clamped to the unambiguous 1980 through 2039 VIN model-year window. The WMI is random and no assignment is claimed.</remarks>
    /// <returns>A 17-character VIN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Vin();
    /// <summary>Generates a checksum-valid synthetic VIN shape for a model year.</summary>
    /// <remarks>The WMI is random and no assignment, manufacture, or vehicle existence is claimed.</remarks>
    /// <param name="modelYear">A model year from 1980 through 2039.</param>
    /// <returns>A 17-character VIN shape encoding the selected model year.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when modelYear or a configured limit is invalid.</exception>
    string VinFromModelYear(int modelYear);
    /// <summary>Selects a project-fictional route name.</summary>
    /// <returns>A generic route name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Route();
    /// <summary>Selects a broad public-transit mode.</summary>
    /// <returns>A conventional transit category.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string TransitMode();
    /// <summary>Generates a correlated fictional motor-vehicle profile.</summary>
    /// <remarks>The VIN and registration are synthetic shapes and explicitly do not claim external assignment.</remarks>
    /// <returns>Correlated make, model, type, fuel, year, VIN, registration, color, route, and transit data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    VehicleProfile Record();
}
