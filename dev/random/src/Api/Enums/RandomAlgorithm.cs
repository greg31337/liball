namespace random;

/// <summary>
/// Identifies a concrete random-number algorithm and its compatibility version.
/// </summary>
/// <remarks>
/// The numeric values are stable identifiers intended for metadata and state
/// interchange. They do not describe relative quality or performance.
/// </remarks>
public enum RandomAlgorithm
{
    /// <summary>
    /// No random-number algorithm has been selected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Version 1 of the PCG 64-bit DXSM deterministic engine.
    /// </summary>
    Pcg64DxsmV1 = 1,

    /// <summary>
    /// Version 1 of the xoshiro256** deterministic engine.
    /// </summary>
    Xoshiro256StarStarV1 = 2,

    /// <summary>
    /// Version 1 of the ten-round Philox 4x64 counter-based engine.
    /// </summary>
    Philox4x64TenV1 = 3,

    /// <summary>
    /// Version 1 of MT19937 with reference initialization.
    /// </summary>
    Mt19937V1 = 4,

    /// <summary>
    /// Version 1 of the separately versioned NumPy-compatible MT19937 mode.
    /// </summary>
    Mt19937NumpyV1 = 5,

    /// <summary>
    /// Version 1 of the SplitMix64 deterministic engine.
    /// </summary>
    SplitMix64V1 = 6,

    /// <summary>
    /// Version 1 of the direct operating-system cryptographic random source.
    /// </summary>
    SystemCryptoV1 = 100,

    /// <summary>
    /// Version 1 of the HMAC-SHA-256 deterministic random bit generator.
    /// </summary>
    HmacDrbgSha256V1 = 101
}
