using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="ScienceProvider"/>.</summary>
public interface IScienceProvider
{
    /// <summary>Selects a broad scientific field.</summary>
    /// <returns>A scientific field name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Field();
    /// <summary>Selects a familiar chemical element.</summary>
    /// <returns>An element name from the correlated element fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Element();
    /// <summary>Selects a conventional scientific unit.</summary>
    /// <returns>A unit name from the correlated quantity and symbol fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Unit();
    /// <summary>Selects an SI prefix.</summary>
    /// <returns>An SI prefix name from the correlated symbol and exponent fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string SiPrefix();
    /// <summary>Generates a correlated synthetic scientific measurement.</summary>
    /// <remarks>The value does not claim calibration, observation, or measurement.</remarks>
    /// <returns>Correlated quantity, unit, prefix, value, and formatted text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    MeasurementRecord Measurement();
    /// <summary>Selects a planet in the Solar System.</summary>
    /// <returns>A planet name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Planet();
    /// <summary>Selects a generic space term.</summary>
    /// <returns>A space-related noun.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Space();
    /// <summary>Generates a conspicuously synthetic experiment label.</summary>
    /// <remarks>The label begins with <c>TEST-</c> and does not represent conducted research.</remarks>
    /// <returns>A numbered experiment-shaped label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Experiment();
    /// <summary>Generates a correlated synthetic science fixture.</summary>
    /// <remarks>The measurement is unmeasured and the experiment does not represent conducted research.</remarks>
    /// <returns>Correlated field, element, measurement, planet, space, and experiment data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    ScienceRecord Record();
}
