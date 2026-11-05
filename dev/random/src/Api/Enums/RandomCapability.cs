namespace random;

/// <summary>
/// Identifies an optional operation supported by a random-number algorithm.
/// </summary>
/// <remarks>
/// Capability values are queried individually through
/// <see cref="RandomAlgorithmInfo.HasCapability(RandomCapability)"/>.
/// </remarks>
public enum RandomCapability
{
    /// <summary>
    /// Represents the absence of an optional capability.
    /// </summary>
    None = 0,

    /// <summary>
    /// Supports deterministic state export and import.
    /// </summary>
    State = 1,

    /// <summary>
    /// Supports cloning at the exact current output position.
    /// </summary>
    Clone = 2,

    /// <summary>
    /// Supports resetting to its documented initial state.
    /// </summary>
    Reset = 3,

    /// <summary>
    /// Supports a fixed-distance jump operation.
    /// </summary>
    Jump = 4,

    /// <summary>
    /// Supports a fixed-distance long-jump operation.
    /// </summary>
    LongJump = 5,

    /// <summary>
    /// Supports advancing by a caller-selected distance.
    /// </summary>
    Advance = 6,

    /// <summary>
    /// Supports seeking to an absolute counter or block position.
    /// </summary>
    Seek = 7,

    /// <summary>
    /// Supports deterministic indexed child-stream creation.
    /// </summary>
    Spawn = 8,

    /// <summary>
    /// Supports reserving a non-overlapping range of counter blocks.
    /// </summary>
    BlockReservation = 9,

    /// <summary>
    /// Supports reseeding with fresh cryptographic entropy.
    /// </summary>
    Reseed = 10,

    /// <summary>
    /// Supports caller-provided additional input during secure generation.
    /// </summary>
    AdditionalInput = 11,

    /// <summary>
    /// Supports a prediction-resistance generation request.
    /// </summary>
    PredictionResistance = 12,

    /// <summary>
    /// Supports securely erasing and permanently uninstantiating the current state.
    /// </summary>
    Uninstantiate = 13
}
