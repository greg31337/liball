namespace sip;

/// <summary>Queries and mutates registration location bindings.</summary>
public interface ISipLocationService
{
    /// <summary>Configures application-provided location storage.</summary>
    /// <param name="storage">The durable storage provider.</param>
    void ConfigureLocationStorage(ISipLocationStorage storage);
    /// <summary>Queries matching bindings.</summary>
    /// <param name="query">The location query.</param>
    /// <returns>The ordered query result.</returns>
    SipLocationResult QueryLocation(SipLocationQuery query);
    /// <summary>Stores or replaces one binding.</summary>
    /// <param name="binding">The binding to store.</param>
    /// <returns>The resulting location data.</returns>
    SipLocationResult UpdateLocation(SipRegistrationBinding binding);
    /// <summary>Atomically replaces all bindings for one address of record.</summary>
    /// <param name="update">The complete replacement binding set.</param>
    /// <returns>The resulting location data.</returns>
    SipLocationResult ReplaceLocations(SipLocationUpdate update);
    /// <summary>Removes matching bindings.</summary>
    /// <param name="query">The removal query.</param>
    /// <returns>The resulting location data.</returns>
    SipLocationResult RemoveLocation(SipLocationQuery query);
    /// <summary>Expires bindings at a monotonic timestamp.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    /// <returns>The resulting location data.</returns>
    SipLocationResult ExpireLocations(long nowMilliseconds);
}
