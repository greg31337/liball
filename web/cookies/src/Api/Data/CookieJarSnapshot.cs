namespace cookies;

/// <summary>
/// Represents a versioned lossless snapshot of canonical cookie-jar state.
/// </summary>
public class CookieJarSnapshot
{
    /// <summary>
    /// The snapshot schema version.
    /// </summary>
    public int Version = CookieDefaults.SnapshotVersion;

    /// <summary>
    /// The stored cookies in deterministic snapshot order.
    /// </summary>
    public StoredCookie[] Cookies = [];

    /// <summary>
    /// The next stable sequence number available to the store.
    /// </summary>
    public long NextSequenceNumber;
}
