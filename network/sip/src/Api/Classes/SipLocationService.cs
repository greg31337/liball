namespace sip;

/// <summary>Stores and queries SIP registration bindings in memory.</summary>
public class SipLocationService : ISipLocationService
{
    SipLocationServiceCore core;

    /// <summary>Initializes an empty in-memory location service.</summary>
    public SipLocationService()
    {
        core = new SipLocationServiceCore();
    }

    /// <summary>Configures application-provided location storage.</summary>
    /// <param name="storage">The durable storage provider.</param>
    public void ConfigureLocationStorage(ISipLocationStorage storage)
    {
        core.ConfigureStorage(storage);
    }

    /// <summary>Queries matching bindings.</summary>
    /// <param name="query">The location query.</param>
    /// <returns>The ordered query result.</returns>
    public SipLocationResult QueryLocation(SipLocationQuery query)
    {
        return core.Query(query);
    }

    /// <summary>Stores or replaces one binding.</summary>
    /// <param name="binding">The binding to store.</param>
    /// <returns>The resulting location data.</returns>
    public SipLocationResult UpdateLocation(SipRegistrationBinding binding)
    {
        return core.Update(binding);
    }

    /// <summary>Atomically replaces all bindings for one address of record.</summary>
    /// <param name="update">The complete replacement binding set.</param>
    /// <returns>The resulting location data.</returns>
    public SipLocationResult ReplaceLocations(SipLocationUpdate update)
    {
        return core.Replace(update);
    }

    /// <summary>Removes matching bindings.</summary>
    /// <param name="query">The removal query.</param>
    /// <returns>The resulting location data.</returns>
    public SipLocationResult RemoveLocation(SipLocationQuery query)
    {
        return core.Remove(query);
    }

    /// <summary>Expires bindings at a monotonic timestamp.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    /// <returns>The resulting location data.</returns>
    public SipLocationResult ExpireLocations(long nowMilliseconds)
    {
        return core.Expire(nowMilliseconds);
    }
}
