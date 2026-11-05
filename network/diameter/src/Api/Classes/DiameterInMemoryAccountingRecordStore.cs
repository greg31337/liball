using System.Collections.Generic;

namespace diameter;

/// <summary>Stores accounting records atomically in process memory.</summary>
public class DiameterInMemoryAccountingRecordStore :
    IAccountingRecordStore
{
    internal AccountingRecordStoreEngine Engine =
        new AccountingRecordStoreEngine();
    internal object StoreLock = new object();

    /// <summary>Initializes an empty in-memory accounting store.</summary>
    public DiameterInMemoryAccountingRecordStore()
    {
    }

    /// <summary>Loads one independently owned accounting record.</summary>
    /// <param name="recordIdentifier">The positive local record identifier.</param>
    /// <returns>The found or missing store outcome.</returns>
    public DiameterAccountingStoreResult LoadDiameterAccountingRecord(
        long recordIdentifier)
    {
        lock (StoreLock)
        {
            return Engine.LoadAccountingStoreRecord(recordIdentifier);
        }
    }

    /// <summary>Atomically creates or replaces one accounting record.</summary>
    /// <param name="record">The complete owned accounting record.</param>
    /// <param name="expectedVersion">Zero for creation or the current version for replacement.</param>
    /// <returns>The save or conflict outcome.</returns>
    public DiameterAccountingStoreResult SaveDiameterAccountingRecord(
        DiameterAccountingRecord? record,
        long expectedVersion)
    {
        lock (StoreLock)
        {
            AccountingStoreMutation mutation =
                Engine.PrepareAccountingStoreSave(
                    record,
                    expectedVersion);
            Engine.ApplyAccountingStoreMutation(mutation);
            return mutation.Result;
        }
    }

    /// <summary>Atomically removes one version-matched accounting record.</summary>
    /// <param name="recordIdentifier">The positive local record identifier.</param>
    /// <param name="expectedVersion">The version that must still be current.</param>
    /// <returns>The removal, missing, or conflict outcome.</returns>
    public DiameterAccountingStoreResult RemoveDiameterAccountingRecord(
        long recordIdentifier,
        long expectedVersion)
    {
        lock (StoreLock)
        {
            AccountingStoreMutation mutation =
                Engine.PrepareAccountingStoreRemove(
                    recordIdentifier,
                    expectedVersion);
            Engine.ApplyAccountingStoreMutation(mutation);
            return mutation.Result;
        }
    }

    /// <summary>Reads independent records in deterministic creation order.</summary>
    /// <returns>All retained accounting records.</returns>
    public List<DiameterAccountingRecord>
        ReadStoredDiameterAccountingRecords()
    {
        lock (StoreLock)
        {
            return Engine.ReadAccountingStoreRecords();
        }
    }
}
