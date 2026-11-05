namespace fake;

/// <summary>Represents a correlated synthetic animal and nature fixture.</summary>
/// <remarks>The fixture combines generic vocabulary and a fictional mountain name. It does not claim an observation, ownership, breed certification, or real place.</remarks>
public class AnimalRecord
{
    /// <summary>The broad animal name correlated with <c>Species</c>.</summary>
    public string Animal = string.Empty;

    /// <summary>The species label correlated with <c>Animal</c> and <c>Habitat</c>.</summary>
    public string Species = string.Empty;

    /// <summary>The broad habitat correlated with <c>Species</c>.</summary>
    public string Habitat = string.Empty;

    /// <summary>The pet type correlated with <c>Breed</c>.</summary>
    public string Pet = string.Empty;

    /// <summary>The breed label correlated with <c>Pet</c>.</summary>
    public string Breed = string.Empty;

    /// <summary>A generic plant name.</summary>
    public string Plant = string.Empty;

    /// <summary>A generic tree name.</summary>
    public string Tree = string.Empty;

    /// <summary>A generic flower name.</summary>
    public string Flower = string.Empty;

    /// <summary>A compositional project-fictional mountain name.</summary>
    public string Mountain = string.Empty;

    /// <summary>A generic weather condition.</summary>
    public string Weather = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the record represents an observation, always <c>false</c>.</summary>
    public bool RepresentsObservation;

    /// <summary>Whether the mountain name claims a real place, always <c>false</c>.</summary>
    public bool MountainIsReal;

    internal AnimalRecord()
    {
    }
}
