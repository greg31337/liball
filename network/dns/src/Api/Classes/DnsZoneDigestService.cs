namespace dns;

/// <summary>Calculates and verifies canonical ZONEMD zone digests.</summary>
public class DnsZoneDigestService : IDnsZoneDigestService
{
    /// <summary>Creates a stateless zone-digest service.</summary>
    public DnsZoneDigestService()
    {
    }

    /// <summary>Calculates a digest over canonical zone data.</summary>
    /// <param name="zone">The complete zone to digest.</param>
    /// <param name="scheme">The ZONEMD digest scheme number.</param>
    /// <param name="hashAlgorithm">The ZONEMD hash algorithm number.</param>
    /// <returns>The calculated digest data and diagnostics.</returns>
    public DnsZoneDigestResult CalculateZoneDigest(
        DnsZone zone,
        int scheme,
        int hashAlgorithm)
    {
        return ZoneDigestOperations.Calculate(
            zone,
            scheme,
            hashAlgorithm);
    }

    /// <summary>Verifies a supplied ZONEMD value against canonical zone data.</summary>
    /// <param name="zone">The complete zone to verify.</param>
    /// <param name="expectedDigest">The expected ZONEMD data.</param>
    /// <returns>The verification state, calculated digest, and diagnostics.</returns>
    public DnsZoneDigestResult VerifyZoneDigest(
        DnsZone zone,
        DnsZonemdData expectedDigest)
    {
        return ZoneDigestOperations.Verify(zone, expectedDigest);
    }
}
