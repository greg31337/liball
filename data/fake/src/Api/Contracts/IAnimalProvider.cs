using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="AnimalProvider"/>.</summary>
public interface IAnimalProvider
{
    /// <summary>Selects a broad animal name.</summary>
    /// <returns>An animal category correlated with the species and habitat fixture tables.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Animal();
    /// <summary>Selects a common species label.</summary>
    /// <returns>A species label correlated with the animal and habitat fixture tables.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Species();
    /// <summary>Selects a broad habitat.</summary>
    /// <returns>A habitat correlated with the animal and species fixture tables.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Habitat();
    /// <summary>Selects a broad pet type.</summary>
    /// <returns>A pet type correlated with the breed fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Pet();
    /// <summary>Selects a breed label.</summary>
    /// <returns>A breed correlated with the pet fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Breed();
    /// <summary>Selects a generic plant name.</summary>
    /// <returns>A plant name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Plant();
    /// <summary>Selects a generic tree name.</summary>
    /// <returns>A tree name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Tree();
    /// <summary>Selects a generic flower name.</summary>
    /// <returns>A flower name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Flower();
    /// <summary>Generates a compositional project-fictional mountain name.</summary>
    /// <remarks>The generated name does not claim a real geographic feature.</remarks>
    /// <returns>A fictional mountain name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Mountain();
    /// <summary>Selects a generic weather condition.</summary>
    /// <remarks>The value is vocabulary, not a forecast or observation.</remarks>
    /// <returns>A weather condition.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Weather();
    /// <summary>Generates a correlated synthetic animal and nature fixture.</summary>
    /// <remarks>The record does not represent an observation, owned pet, certified breed, or real mountain.</remarks>
    /// <returns>Correlated animal, species, habitat, pet, breed, plant, mountain, and weather data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    AnimalRecord Record();
}
