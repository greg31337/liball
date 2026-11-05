namespace random;

/// <summary>
/// Describes the identity, storage, security classification, and optional
/// operations of a random-number algorithm.
/// </summary>
/// <remarks>
/// Instances copy their capability input and return a copy from
/// <see cref="Capabilities"/>, so metadata cannot be changed through a shared
/// array. The class describes an algorithm; it does not create a generator. A true
/// <see cref="IsCryptographic"/> value is a usage classification, not a claim of FIPS
/// validation, independent certification, or suitability of caller-supplied entropy.
/// </remarks>
public class RandomAlgorithmInfo : IRandomAlgorithmInfo
{
    RandomAlgorithm algorithm;
    string name;
    int version;
    int transformVersion;
    int wordBits;
    int stateBytes;
    bool cryptographic;
    string periodDescription;
    RandomCapability[] capabilities;

    /// <summary>
    /// Creates algorithm metadata.
    /// </summary>
    /// <param name="algorithm">The stable algorithm identifier.</param>
    /// <param name="name">The nonempty display name.</param>
    /// <param name="version">The positive semantic algorithm version.</param>
    /// <param name="transformVersion">The positive version of the shared output transforms.</param>
    /// <param name="wordBits">The positive native output word size in bits.</param>
    /// <param name="stateBytes">The nonnegative serialized engine-state size in bytes.</param>
    /// <param name="cryptographic">Whether the algorithm is intended for cryptographic use.</param>
    /// <param name="periodDescription">The stable nonempty description of the period or exhaustion behavior.</param>
    /// <param name="capabilities">The optional operations implemented by the algorithm.</param>
    /// <exception cref="RandomArgumentException">An argument is outside its documented domain.</exception>
    public RandomAlgorithmInfo(RandomAlgorithm algorithm, string name, int version, int transformVersion, int wordBits, int stateBytes, bool cryptographic, string periodDescription, RandomCapability[] capabilities)
    {
        Guard.Argument(algorithm != RandomAlgorithm.None, "algorithm", "An algorithm must be specified.");
        Guard.NotEmpty(name, "name");
        Guard.Positive(version, "version");
        Guard.Positive(transformVersion, "transformVersion");
        Guard.Positive(wordBits, "wordBits");
        Guard.NonNegative(stateBytes, "stateBytes");
        Guard.NotEmpty(periodDescription, "periodDescription");
        Guard.NotNull(capabilities, "capabilities");

        this.algorithm = algorithm;
        this.name = name;
        this.version = version;
        this.transformVersion = transformVersion;
        this.wordBits = wordBits;
        this.stateBytes = stateBytes;
        this.cryptographic = cryptographic;
        this.periodDescription = periodDescription;
        this.capabilities = (RandomCapability[])capabilities.Clone();
    }

    /// <summary>
    /// Returns the stable algorithm identifier.
    /// </summary>
    /// <returns>The algorithm identifier.</returns>
    public RandomAlgorithm Algorithm()
    {
        return algorithm;
    }

    /// <summary>
    /// Returns the display name of the algorithm.
    /// </summary>
    /// <returns>The nonempty algorithm name.</returns>
    public string Name()
    {
        return name;
    }

    /// <summary>
    /// Returns the semantic version of the named algorithm.
    /// </summary>
    /// <returns>A positive algorithm version.</returns>
    public int Version()
    {
        return version;
    }

    /// <summary>
    /// Returns the version of the shared output transforms.
    /// </summary>
    /// <returns>A positive transform version.</returns>
    public int TransformVersion()
    {
        return transformVersion;
    }

    /// <summary>
    /// Returns the native output word size.
    /// </summary>
    /// <returns>The positive word size in bits.</returns>
    public int WordBits()
    {
        return wordBits;
    }

    /// <summary>
    /// Returns the serialized engine-state payload size.
    /// </summary>
    /// <returns>The state size in bytes, or zero when state is not exportable.</returns>
    public int StateBytes()
    {
        return stateBytes;
    }

    /// <summary>
    /// Reports whether the algorithm is intended for cryptographic use.
    /// </summary>
    /// <returns><c>true</c> for a cryptographic algorithm; otherwise, <c>false</c>.</returns>
    public bool IsCryptographic()
    {
        return cryptographic;
    }

    /// <summary>
    /// Returns the stable description of the algorithm period or exhaustion behavior.
    /// </summary>
    /// <returns>A nonempty period or exhaustion description.</returns>
    public string PeriodDescription()
    {
        return periodDescription;
    }

    /// <summary>
    /// Returns a copy of the supported optional operations.
    /// </summary>
    /// <returns>A new capability array owned by the caller.</returns>
    public RandomCapability[] Capabilities()
    {
        return (RandomCapability[])capabilities.Clone();
    }

    /// <summary>
    /// Reports whether the algorithm supports an optional operation.
    /// </summary>
    /// <param name="capability">The individual capability to query.</param>
    /// <returns><c>true</c> when the capability is supported; otherwise, <c>false</c>.</returns>
    public bool HasCapability(RandomCapability capability)
    {
        if (capability == RandomCapability.None)
        {
            return false;
        }

        int index = 0;
        while (index < capabilities.Length)
        {
            if (capabilities[index] == capability)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
