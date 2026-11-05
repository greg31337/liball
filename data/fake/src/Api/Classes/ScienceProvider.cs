using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides science and measurement generation operations.
/// </summary>
public class ScienceProvider : IScienceProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal ScienceProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("science");
    }

    /// <summary>Selects a broad scientific field.</summary>
    /// <returns>A scientific field name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Field()
    {
        return ScienceGenerator.Field(Context, RandomValue);
    }

    /// <summary>Selects a familiar chemical element.</summary>
    /// <returns>An element name from the correlated element fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Element()
    {
        return ScienceGenerator.Element(Context, RandomValue);
    }

    /// <summary>Selects a conventional scientific unit.</summary>
    /// <returns>A unit name from the correlated quantity and symbol fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Unit()
    {
        return ScienceGenerator.Unit(Context, RandomValue);
    }

    /// <summary>Selects an SI prefix.</summary>
    /// <returns>An SI prefix name from the correlated symbol and exponent fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SiPrefix()
    {
        return ScienceGenerator.SiPrefix(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic scientific measurement.</summary>
    /// <remarks>The value does not claim calibration, observation, or measurement.</remarks>
    /// <returns>Correlated quantity, unit, prefix, value, and formatted text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public MeasurementRecord Measurement()
    {
        return ScienceGenerator.Measurement(Context, RandomValue);
    }

    /// <summary>Selects a planet in the Solar System.</summary>
    /// <returns>A planet name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Planet()
    {
        return ScienceGenerator.Planet(Context, RandomValue);
    }

    /// <summary>Selects a generic space term.</summary>
    /// <returns>A space-related noun.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Space()
    {
        return ScienceGenerator.Space(Context, RandomValue);
    }

    /// <summary>Generates a conspicuously synthetic experiment label.</summary>
    /// <remarks>The label begins with <c>TEST-</c> and does not represent conducted research.</remarks>
    /// <returns>A numbered experiment-shaped label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Experiment()
    {
        return ScienceGenerator.Experiment(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic science fixture.</summary>
    /// <remarks>The measurement is unmeasured and the experiment does not represent conducted research.</remarks>
    /// <returns>Correlated field, element, measurement, planet, space, and experiment data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public ScienceRecord Record()
    {
        return ScienceGenerator.Record(Context, RandomValue);
    }
}
