namespace dns;

/// <summary>Calculates and verifies canonical ZONEMD zone digests.</summary>
public interface IDnsZoneDigestService
{
    /// <summary>Calculates a digest over canonical zone data.</summary>
    /// <param name="zone">The complete zone to digest.</param>
    /// <param name="scheme">The ZONEMD digest scheme number.</param>
    /// <param name="hashAlgorithm">The ZONEMD hash algorithm number.</param>
    /// <returns>The calculated digest data and diagnostics.</returns>
    DnsZoneDigestResult CalculateZoneDigest(DnsZone zone, int scheme, int hashAlgorithm);

    /// <summary>Verifies a supplied ZONEMD value against canonical zone data.</summary>
    /// <param name="zone">The complete zone to verify.</param>
    /// <param name="expectedDigest">The expected ZONEMD data.</param>
    /// <returns>The verification state, calculated digest, and diagnostics.</returns>
    DnsZoneDigestResult VerifyZoneDigest(DnsZone zone, DnsZonemdData expectedDigest);
}
