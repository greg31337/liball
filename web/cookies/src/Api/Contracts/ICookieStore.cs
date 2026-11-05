namespace cookies;

/// <summary>
/// Provides synchronous access to canonical stored cookie state.
/// </summary>
public interface ICookieStore
{
    /// <summary>
    /// Finds a stored cookie by its exact identity.
    /// </summary>
    /// <param name="identity">The identity to locate.</param>
    /// <returns>The stored cookie when found; otherwise, null.</returns>
    /// <exception cref="CookieStoreException">The store lookup fails.</exception>
    StoredCookie? FindStoredCookie(CookieIdentity identity);

    /// <summary>
    /// Returns stored cookies selected by a candidate or management query.
    /// </summary>
    /// <param name="query">The query filters to apply.</param>
    /// <returns>Independent stored cookie models in deterministic store order.</returns>
    /// <exception cref="CookieStoreException">The store query fails.</exception>
    StoredCookie[] QueryStoredCookies(CookieStoreQuery query);

    /// <summary>
    /// Atomically applies a canonical cookie upsert or expired-identity deletion.
    /// </summary>
    /// <param name="cookie">The canonical cookie state to apply.</param>
    /// <returns>The storage mutation outcome.</returns>
    /// <exception cref="CookieStoreException">The atomic mutation fails.</exception>
    CookieSetResult ApplyStoredCookie(StoredCookie cookie);

    /// <summary>
    /// Atomically updates the last-access time of existing cookie identities.
    /// </summary>
    /// <param name="identities">The cookie identities to update in input order.</param>
    /// <param name="accessTime">The last-access instant to apply.</param>
    /// <returns>Independent updated cookies in input order, excluding absent identities.</returns>
    /// <exception cref="CookieStoreException">The identities or atomic update are invalid.</exception>
    StoredCookie[] UpdateStoredCookieAccess(
        CookieIdentity[] identities,
        global::System.DateTimeOffset accessTime);

    /// <summary>
    /// Removes stored cookies selected by removal filters.
    /// </summary>
    /// <param name="query">The removal filters to apply.</param>
    /// <returns>The number of stored cookies removed.</returns>
    /// <exception cref="CookieStoreException">The atomic removal fails.</exception>
    int RemoveStoredCookies(CookieRemovalQuery query);

    /// <summary>
    /// Captures a complete independent snapshot of stored state.
    /// </summary>
    /// <returns>The deterministic store snapshot.</returns>
    /// <exception cref="CookieStoreException">The store snapshot cannot be captured.</exception>
    CookieJarSnapshot CaptureStoreSnapshot();

    /// <summary>
    /// Atomically replaces all stored state with a validated snapshot.
    /// </summary>
    /// <param name="snapshot">The independent validated snapshot.</param>
    /// <exception cref="CookieStoreException">The atomic snapshot replacement fails.</exception>
    void ReplaceStoreSnapshot(CookieJarSnapshot snapshot);
}
