namespace sip;

/// <summary>Persists location bindings for a SIP location service.</summary>
/// <remarks>
/// Implementations may use any application-managed durable store. Each mutation
/// must atomically commit its affected address-of-record data and return the
/// resulting bindings.
/// </remarks>
public interface ISipLocationStorage
{
    /// <summary>Reads matching stored bindings.</summary>
    /// <param name="query">The location query.</param>
    /// <returns>The matching stored bindings.</returns>
    SipLocationResult QueryStoredLocations(SipLocationQuery query);
    /// <summary>Atomically stores or replaces one binding.</summary>
    /// <param name="binding">The binding to store.</param>
    /// <returns>The resulting bindings for the address of record.</returns>
    SipLocationResult UpdateStoredLocation(SipRegistrationBinding binding);
    /// <summary>Atomically replaces one address-of-record binding set.</summary>
    /// <param name="update">The complete replacement binding set.</param>
    /// <returns>The resulting bindings for the address of record.</returns>
    SipLocationResult ReplaceStoredLocations(SipLocationUpdate update);
    /// <summary>Atomically removes matching bindings.</summary>
    /// <param name="query">The removal query.</param>
    /// <returns>The resulting bindings for the address of record.</returns>
    SipLocationResult RemoveStoredLocations(SipLocationQuery query);
    /// <summary>Atomically removes bindings expired at a timestamp.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    /// <returns>
    /// The resulting stored bindings, with removed bindings in
    /// <see cref="SipLocationResult.ExpiredBindings"/>.
    /// </returns>
    SipLocationResult ExpireStoredLocations(long nowMilliseconds);
}
