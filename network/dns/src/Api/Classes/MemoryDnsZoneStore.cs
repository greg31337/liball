using System.Collections.Generic;

namespace dns;

/// <summary>Stores validated DNS zone snapshots in process memory.</summary>
public class MemoryDnsZoneStore : IDnsZoneStore
{
    internal object SyncRoot = new object();
    internal List<DnsStoredZone> Zones = new List<DnsStoredZone>();
    internal DnsHostedZoneIndex HostedIndex = new DnsHostedZoneIndex();
    internal long NextJournalSequence;

    /// <summary>Creates an empty in-memory DNS zone store.</summary>
    public MemoryDnsZoneStore()
    {
    }

    /// <summary>Finds the longest hosted zone matching a name and class.</summary>
    /// <param name="name">The queried DNS name.</param>
    /// <param name="recordClass">The recognized DNS class.</param>
    /// <param name="classValue">The raw DNS class integer.</param>
    /// <returns>The best matching zone, or null when none is hosted.</returns>
    public DnsZone? FindBestZone(
        DnsName name,
        DnsClass recordClass,
        int classValue)
    {
        lock (this.SyncRoot)
        {
            return DnsZoneStoreOperations.FindBestZone(
                this,
                name,
                recordClass,
                classValue);
        }
    }

    /// <summary>Reads one hosted zone by exact apex and class.</summary>
    /// <param name="apex">The exact zone apex.</param>
    /// <param name="recordClass">The recognized DNS class.</param>
    /// <param name="classValue">The raw DNS class integer.</param>
    /// <returns>The zone snapshot, or null when it is not hosted.</returns>
    public DnsZone? ReadZone(
        DnsName apex,
        DnsClass recordClass,
        int classValue)
    {
        lock (this.SyncRoot)
        {
            return DnsZoneStoreOperations.ReadZone(
                this,
                apex,
                recordClass,
                classValue);
        }
    }

    /// <summary>Adds a new validated zone.</summary>
    /// <param name="zone">The zone to add.</param>
    /// <returns>The resulting zone-validation state.</returns>
    public DnsZoneValidationResult AddZone(DnsZone zone)
    {
        lock (this.SyncRoot)
        {
            return DnsZoneStoreOperations.AddZone(this, zone);
        }
    }

    /// <summary>Atomically replaces one hosted zone.</summary>
    /// <param name="zone">The complete replacement zone.</param>
    /// <returns>The resulting zone-validation state.</returns>
    public DnsZoneValidationResult ReplaceZone(DnsZone zone)
    {
        lock (this.SyncRoot)
        {
            return DnsZoneStoreOperations.ReplaceZone(this, zone);
        }
    }

    /// <summary>Deletes one hosted zone by exact apex and class.</summary>
    /// <param name="apex">The exact zone apex.</param>
    /// <param name="recordClass">The recognized DNS class.</param>
    /// <param name="classValue">The raw DNS class integer.</param>
    /// <returns>True when a zone was removed.</returns>
    public bool DeleteZone(
        DnsName apex,
        DnsClass recordClass,
        int classValue)
    {
        lock (this.SyncRoot)
        {
            return DnsZoneStoreOperations.DeleteZone(
                this,
                apex,
                recordClass,
                classValue);
        }
    }

    /// <summary>Finds an exact node in one hosted zone.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The exact owner name.</param>
    /// <returns>The exact node, or null when the name does not exist.</returns>
    public DnsZoneNode? FindExactNode(DnsZone zone, DnsName name)
    {
        return ZoneIndexOperations.FindExact(
            ZoneIndexOperations.BuildZone(zone),
            name);
    }

    /// <summary>Finds the closest existing ancestor node in one hosted zone.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The queried owner name.</param>
    /// <returns>The closest existing node, or null when none exists.</returns>
    public DnsZoneNode? FindClosestNode(DnsZone zone, DnsName name)
    {
        return ZoneIndexOperations.FindClosestEncloser(
            ZoneIndexOperations.BuildZone(zone),
            name);
    }

    /// <summary>Finds the nearest enclosing delegation node.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The queried owner name.</param>
    /// <returns>The delegation node, or null when the name remains authoritative.</returns>
    public DnsZoneNode? FindDelegationNode(DnsZone zone, DnsName name)
    {
        return ZoneIndexOperations.FindDelegation(
            ZoneIndexOperations.BuildZone(zone),
            name);
    }

    /// <summary>Finds the applicable wildcard node below a closest encloser.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The nonexistent queried name.</param>
    /// <returns>The wildcard node, or null when none applies.</returns>
    public DnsZoneNode? FindWildcardNode(DnsZone zone, DnsName name)
    {
        return ZoneIndexOperations.FindWildcard(
            ZoneIndexOperations.BuildZone(zone),
            name);
    }

    /// <summary>Finds the canonical predecessor node for denial processing.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The canonical lookup name.</param>
    /// <returns>The predecessor node, or null for an empty zone.</returns>
    public DnsZoneNode? FindPrecedingNode(DnsZone zone, DnsName name)
    {
        return ZoneIndexOperations.FindPredecessor(
            ZoneIndexOperations.BuildZone(zone),
            name);
    }

    /// <summary>Finds the canonical successor node for denial processing.</summary>
    /// <param name="zone">The zone snapshot to search.</param>
    /// <param name="name">The canonical lookup name.</param>
    /// <returns>The successor node, or null for an empty zone.</returns>
    public DnsZoneNode? FindFollowingNode(DnsZone zone, DnsName name)
    {
        return ZoneIndexOperations.FindSuccessor(
            ZoneIndexOperations.BuildZone(zone),
            name);
    }

    /// <summary>Validates and atomically applies a zone transaction.</summary>
    /// <param name="apex">The exact hosted zone apex.</param>
    /// <param name="recordClass">The recognized DNS class.</param>
    /// <param name="classValue">The raw DNS class integer.</param>
    /// <param name="transaction">The expected serial and ordered changes.</param>
    /// <returns>The validation state after applying or rejecting the transaction.</returns>
    public DnsZoneValidationResult ApplyZoneTransaction(
        DnsName apex,
        DnsClass recordClass,
        int classValue,
        DnsZoneTransaction transaction)
    {
        lock (this.SyncRoot)
        {
            return DnsZoneStoreOperations.ApplyZoneTransaction(
                this,
                apex,
                recordClass,
                classValue,
                transaction);
        }
    }

    /// <summary>Reads committed journal entries after one SOA serial.</summary>
    /// <param name="apex">The exact hosted zone apex.</param>
    /// <param name="recordClass">The recognized DNS class.</param>
    /// <param name="classValue">The raw DNS class integer.</param>
    /// <param name="fromSerial">The first prior serial requested by IXFR.</param>
    /// <returns>The ordered available journal entries.</returns>
    public List<DnsZoneJournalEntry> ReadZoneJournal(
        DnsName apex,
        DnsClass recordClass,
        int classValue,
        uint fromSerial)
    {
        lock (this.SyncRoot)
        {
            return DnsZoneStoreOperations.ReadZoneJournal(
                this,
                apex,
                recordClass,
                classValue,
                fromSerial);
        }
    }
}
