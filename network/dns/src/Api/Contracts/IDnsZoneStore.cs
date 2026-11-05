using System.Collections.Generic;

namespace dns;

/// <summary>Stores DNS zones and performs authoritative name and journal lookups.</summary>
public interface IDnsZoneStore
{
    /// <summary>Finds the longest hosted zone matching a name and class.</summary>
    /// <param name="name">The queried DNS name.</param>
    /// <param name="recordClass">The recognized DNS class.</param>
    /// <param name="classValue">The raw DNS class integer.</param>
    /// <returns>The best matching zone, or null when none is hosted.</returns>
    DnsZone? FindBestZone(DnsName name, DnsClass recordClass, int classValue);

    /// <summary>Reads one hosted zone by exact apex and class.</summary>
    /// <param name="apex">The exact zone apex.</param>
    /// <param name="recordClass">The recognized DNS class.</param>
    /// <param name="classValue">The raw DNS class integer.</param>
    /// <returns>The zone snapshot, or null when it is not hosted.</returns>
    DnsZone? ReadZone(DnsName apex, DnsClass recordClass, int classValue);

    /// <summary>Adds a new validated zone.</summary>
    /// <param name="zone">The zone to add.</param>
    /// <returns>The resulting zone-validation state.</returns>
    DnsZoneValidationResult AddZone(DnsZone zone);

    /// <summary>Atomically replaces one hosted zone.</summary>
    /// <param name="zone">The complete replacement zone.</param>
    /// <returns>The resulting zone-validation state.</returns>
    DnsZoneValidationResult ReplaceZone(DnsZone zone);

    /// <summary>Deletes one hosted zone by exact apex and class.</summary>
    /// <param name="apex">The exact zone apex.</param>
    /// <param name="recordClass">The recognized DNS class.</param>
    /// <param name="classValue">The raw DNS class integer.</param>
    /// <returns>True when a zone was removed.</returns>
    bool DeleteZone(DnsName apex, DnsClass recordClass, int classValue);

    /// <summary>Finds an exact node in one hosted zone.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The exact owner name.</param>
    /// <returns>The exact node, or null when the name does not exist.</returns>
    DnsZoneNode? FindExactNode(DnsZone zone, DnsName name);

    /// <summary>Finds the closest existing ancestor node in one hosted zone.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The queried owner name.</param>
    /// <returns>The closest existing node, or null when none exists.</returns>
    DnsZoneNode? FindClosestNode(DnsZone zone, DnsName name);

    /// <summary>Finds the nearest enclosing delegation node.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The queried owner name.</param>
    /// <returns>The delegation node, or null when the name remains authoritative.</returns>
    DnsZoneNode? FindDelegationNode(DnsZone zone, DnsName name);

    /// <summary>Finds the applicable wildcard node below a closest encloser.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The nonexistent queried name.</param>
    /// <returns>The wildcard node, or null when none applies.</returns>
    DnsZoneNode? FindWildcardNode(DnsZone zone, DnsName name);

    /// <summary>Finds the canonical predecessor node for denial processing.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The canonical lookup name.</param>
    /// <returns>The predecessor node, or null for an empty zone.</returns>
    DnsZoneNode? FindPrecedingNode(DnsZone zone, DnsName name);

    /// <summary>Finds the canonical successor node for denial processing.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The canonical lookup name.</param>
    /// <returns>The successor node, or null for an empty zone.</returns>
    DnsZoneNode? FindFollowingNode(DnsZone zone, DnsName name);

    /// <summary>Validates and atomically applies a zone transaction.</summary>
    /// <param name="apex">The exact hosted zone apex.</param>
    /// <param name="recordClass">The recognized DNS class.</param>
    /// <param name="classValue">The raw DNS class integer.</param>
    /// <param name="transaction">The expected serial and ordered changes.</param>
    /// <returns>The validation state after applying or rejecting the transaction.</returns>
    DnsZoneValidationResult ApplyZoneTransaction(DnsName apex, DnsClass recordClass, int classValue, DnsZoneTransaction transaction);

    /// <summary>Reads committed journal entries after one SOA serial.</summary>
    /// <param name="apex">The exact hosted zone apex.</param>
    /// <param name="recordClass">The recognized DNS class.</param>
    /// <param name="classValue">The raw DNS class integer.</param>
    /// <param name="fromSerial">The first prior serial requested by IXFR.</param>
    /// <returns>The ordered available journal entries.</returns>
    List<DnsZoneJournalEntry> ReadZoneJournal(DnsName apex, DnsClass recordClass, int classValue, uint fromSerial);
}
