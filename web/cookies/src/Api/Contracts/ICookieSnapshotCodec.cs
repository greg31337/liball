namespace cookies;

/// <summary>
/// Encodes and decodes versioned lossless cookie-jar snapshots.
/// </summary>
public interface ICookieSnapshotCodec
{
    /// <summary>
    /// Serializes a validated cookie-jar snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <returns>The serialized snapshot text.</returns>
    /// <exception cref="CookieSnapshotException">The snapshot model is invalid.</exception>
    string SerializeJarSnapshot(CookieJarSnapshot snapshot);

    /// <summary>
    /// Parses snapshot text or throws for corrupt input.
    /// </summary>
    /// <param name="text">The snapshot text.</param>
    /// <returns>The validated snapshot.</returns>
    /// <exception cref="CookieSnapshotException">The snapshot text is corrupt or unsupported.</exception>
    CookieJarSnapshot ParseJarSnapshot(string text);

    /// <summary>
    /// Parses snapshot text without throwing for corrupt input.
    /// </summary>
    /// <param name="text">The snapshot text.</param>
    /// <returns>The snapshot parse result.</returns>
    CookieSnapshotParseResult TryParseJarSnapshot(string text);
}
