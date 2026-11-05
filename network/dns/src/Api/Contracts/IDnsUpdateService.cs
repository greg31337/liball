namespace dns;

/// <summary>Validates and atomically applies DNS dynamic updates.</summary>
public interface IDnsUpdateService
{
    /// <summary>Applies one authenticated and authorized update request.</summary>
    /// <param name="request">The zone, prerequisites, mutations, and security policy.</param>
    /// <returns>The response code, committed transaction, and diagnostics.</returns>
    DnsUpdateResult ApplyUpdate(DnsUpdateRequest request);
}
