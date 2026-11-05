using System;

namespace fake;

/// <summary>
/// Contains a portable snapshot of a deterministic random stream.
/// </summary>
/// <remarks>
/// All fields form part of the snapshot. Changing a snapshot after it has been
/// restored does not change the restored stream.
/// </remarks>
public class FakeState
{
    /// <summary>
    /// Identifies the stable algorithm and seed-conversion contract.
    /// </summary>
    public const string StableAlgorithm = "xoshiro256**-splitmix64-fnv1a64-v1";

    /// <summary>
    /// The algorithm identifier for this snapshot.
    /// </summary>
    public string Algorithm = string.Empty;

    /// <summary>
    /// The seed used when the restored stream is reset.
    /// </summary>
    public ulong InitialSeed;

    /// <summary>
    /// The first random-state word.
    /// </summary>
    public ulong State0;

    /// <summary>
    /// The second random-state word.
    /// </summary>
    public ulong State1;

    /// <summary>
    /// The third random-state word.
    /// </summary>
    public ulong State2;

    /// <summary>
    /// The fourth random-state word.
    /// </summary>
    public ulong State3;

    /// <summary>
    /// Initializes a snapshot from an algorithm identifier, reset seed, and state words.
    /// </summary>
    /// <param name="algorithm">The algorithm identifier.</param>
    /// <param name="initialSeed">The seed used by reset.</param>
    /// <param name="state0">The first random-state word.</param>
    /// <param name="state1">The second random-state word.</param>
    /// <param name="state2">The third random-state word.</param>
    /// <param name="state3">The fourth random-state word.</param>
    public FakeState(string algorithm, ulong initialSeed, ulong state0, ulong state1, ulong state2, ulong state3)
    {
        Algorithm = algorithm;
        InitialSeed = initialSeed;
        State0 = state0;
        State1 = state1;
        State2 = state2;
        State3 = state3;
    }

    /// <summary>
    /// Creates an independent field-for-field copy of this snapshot.
    /// </summary>
    /// <returns>A new snapshot containing the same state.</returns>
    internal FakeState Copy()
    {
        return new FakeState(Algorithm, InitialSeed, State0, State1, State2, State3);
    }

    internal void Validate()
    {
        Guard.NotNullOrEmpty(Algorithm, "Algorithm");
        if (Algorithm != StableAlgorithm)
        {
            throw new ArgumentException($"Algorithm must be '{StableAlgorithm}'.", "Algorithm");
        }
        if (State0 == 0UL && State1 == 0UL && State2 == 0UL && State3 == 0UL)
        {
            throw new ArgumentException("At least one random-state word must be nonzero.", "state");
        }
    }
}
