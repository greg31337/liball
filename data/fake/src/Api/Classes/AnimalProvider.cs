using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides animal and nature generation operations.
/// </summary>
public class AnimalProvider : IAnimalProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal AnimalProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("animal");
    }

    /// <summary>Selects a broad animal name.</summary>
    /// <returns>An animal category correlated with the species and habitat fixture tables.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Animal()
    {
        return AnimalGenerator.Animal(Context, RandomValue);
    }

    /// <summary>Selects a common species label.</summary>
    /// <returns>A species label correlated with the animal and habitat fixture tables.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Species()
    {
        return AnimalGenerator.Species(Context, RandomValue);
    }

    /// <summary>Selects a broad habitat.</summary>
    /// <returns>A habitat correlated with the animal and species fixture tables.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Habitat()
    {
        return AnimalGenerator.Habitat(Context, RandomValue);
    }

    /// <summary>Selects a broad pet type.</summary>
    /// <returns>A pet type correlated with the breed fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Pet()
    {
        return AnimalGenerator.Pet(Context, RandomValue);
    }

    /// <summary>Selects a breed label.</summary>
    /// <returns>A breed correlated with the pet fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Breed()
    {
        return AnimalGenerator.Breed(Context, RandomValue);
    }

    /// <summary>Selects a generic plant name.</summary>
    /// <returns>A plant name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Plant()
    {
        return AnimalGenerator.Plant(Context, RandomValue);
    }

    /// <summary>Selects a generic tree name.</summary>
    /// <returns>A tree name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Tree()
    {
        return AnimalGenerator.Tree(Context, RandomValue);
    }

    /// <summary>Selects a generic flower name.</summary>
    /// <returns>A flower name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Flower()
    {
        return AnimalGenerator.Flower(Context, RandomValue);
    }

    /// <summary>Generates a compositional project-fictional mountain name.</summary>
    /// <remarks>The generated name does not claim a real geographic feature.</remarks>
    /// <returns>A fictional mountain name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Mountain()
    {
        return AnimalGenerator.Mountain(Context, RandomValue);
    }

    /// <summary>Selects a generic weather condition.</summary>
    /// <remarks>The value is vocabulary, not a forecast or observation.</remarks>
    /// <returns>A weather condition.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Weather()
    {
        return AnimalGenerator.Weather(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic animal and nature fixture.</summary>
    /// <remarks>The record does not represent an observation, owned pet, certified breed, or real mountain.</remarks>
    /// <returns>Correlated animal, species, habitat, pet, breed, plant, mountain, and weather data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public AnimalRecord Record()
    {
        return AnimalGenerator.Record(Context, RandomValue);
    }
}
