namespace cookies;

/// <summary>
/// Encodes and decodes versioned lossless cookie-jar snapshots.
/// </summary>
public class CookieSnapshotCodec : ICookieSnapshotCodec
{
    /// <summary>
    /// Creates a lossless cookie snapshot codec.
    /// </summary>
    public CookieSnapshotCodec()
    {
    }

    /// <summary>
    /// Serializes a validated snapshot with deterministic entry and field order.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <returns>The versioned lossless JSON snapshot.</returns>
    /// <exception cref="CookieSnapshotException">The snapshot model is invalid.</exception>
    public string SerializeJarSnapshot(CookieJarSnapshot snapshot)
    {
        return CookieSnapshotWriter.WriteSnapshot(snapshot);
    }

    /// <summary>
    /// Parses snapshot text or throws for corrupt input.
    /// </summary>
    /// <param name="text">The snapshot text.</param>
    /// <returns>The validated snapshot.</returns>
    /// <exception cref="CookieSnapshotException">The snapshot is malformed or unsupported.</exception>
    public CookieJarSnapshot ParseJarSnapshot(string text)
    {
        CookieSnapshotParseResult result = CookieSnapshotReader.ReadSnapshot(text);
        if (!result.Success || result.Snapshot == null)
        {
            throw new CookieSnapshotException(
                CookieSnapshotWriter.CreateSnapshotFailureMessage(
                    result.ErrorCode));
        }

        return CookieGraphCloner.CloneJarSnapshot(result.Snapshot);
    }

    /// <summary>
    /// Parses snapshot text without throwing for corrupt input.
    /// </summary>
    /// <param name="text">The snapshot text.</param>
    /// <returns>The validated snapshot result.</returns>
    public CookieSnapshotParseResult TryParseJarSnapshot(string text)
    {
        return CookieGraphCloner.CloneSnapshotParseResult(
            CookieSnapshotReader.ReadSnapshot(text));
    }
}
