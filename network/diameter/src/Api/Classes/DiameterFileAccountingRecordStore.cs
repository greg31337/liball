using System;
using System.Collections.Generic;
using System.IO;

namespace diameter;

/// <summary>
/// Stores accounting records in a checksum-protected atomically replaced file.
/// </summary>
public class DiameterFileAccountingRecordStore : IAccountingRecordStore
{
    internal AccountingRecordStoreEngine Engine =
        new AccountingRecordStoreEngine();
    internal object StoreLock = new object();
    internal string FilePath = string.Empty;
    internal bool Corrupt;
    internal string CorruptionCode = string.Empty;
    internal string CorruptionMessage = string.Empty;

    /// <summary>Initializes or loads an atomic accounting record file.</summary>
    /// <param name="filePath">The accounting store file path.</param>
    /// <exception cref="System.ArgumentException">Thrown when the path is empty.</exception>
    public DiameterFileAccountingRecordStore(string? filePath)
    {
        if (filePath == null || filePath.Length == 0)
        {
            throw new ArgumentException(
                "An accounting store file path is required.");
        }

        FilePath = Path.GetFullPath(filePath);
        string? directory = Path.GetDirectoryName(FilePath);
        if (directory == null || directory.Length == 0)
        {
            throw new ArgumentException(
                "The accounting store directory is invalid.");
        }

        Directory.CreateDirectory(directory);
        if (File.Exists(FilePath))
        {
            AccountingStoreDecodeResult decoded =
                AccountingRecordStoreFileCodec.DecodeAccountingStoreFile(
                    File.ReadAllBytes(FilePath));
            if (decoded.Valid)
            {
                Engine.ReplaceAccountingStoreRecords(decoded.Records);
            }
            else
            {
                Corrupt = true;
                CorruptionCode = decoded.Code;
                CorruptionMessage = decoded.Message;
            }
        }
    }

    /// <summary>Loads one independently owned file-backed accounting record.</summary>
    /// <param name="recordIdentifier">The positive local record identifier.</param>
    /// <returns>The found, missing, or corrupt-store outcome.</returns>
    public DiameterAccountingStoreResult LoadDiameterAccountingRecord(
        long recordIdentifier)
    {
        if (recordIdentifier <= 0)
        {
            throw new ArgumentOutOfRangeException("recordIdentifier");
        }

        lock (StoreLock)
        {
            if (Corrupt)
            {
                return CreateCorruptAccountingStoreResult(
                    recordIdentifier);
            }

            return Engine.LoadAccountingStoreRecord(recordIdentifier);
        }
    }

    /// <summary>Atomically writes a complete versioned accounting snapshot.</summary>
    /// <param name="record">The complete owned accounting record.</param>
    /// <param name="expectedVersion">Zero for creation or the current version for replacement.</param>
    /// <returns>The save, conflict, or corrupt-store outcome.</returns>
    public DiameterAccountingStoreResult SaveDiameterAccountingRecord(
        DiameterAccountingRecord? record,
        long expectedVersion)
    {
        lock (StoreLock)
        {
            if (Corrupt)
            {
                return CreateCorruptAccountingStoreResult(
                    record == null ? 0 : record.RecordIdentifier);
            }

            AccountingStoreMutation mutation =
                Engine.PrepareAccountingStoreSave(
                    record,
                    expectedVersion);
            if (mutation.Changed)
            {
                WriteAtomicAccountingStoreFile(mutation.Records);
                Engine.ApplyAccountingStoreMutation(mutation);
            }

            return mutation.Result;
        }
    }

    /// <summary>Atomically removes one version-matched accounting record.</summary>
    /// <param name="recordIdentifier">The positive local record identifier.</param>
    /// <param name="expectedVersion">The version that must still be current.</param>
    /// <returns>The removal, missing, conflict, or corrupt-store outcome.</returns>
    public DiameterAccountingStoreResult RemoveDiameterAccountingRecord(
        long recordIdentifier,
        long expectedVersion)
    {
        lock (StoreLock)
        {
            if (Corrupt)
            {
                return CreateCorruptAccountingStoreResult(
                    recordIdentifier);
            }

            AccountingStoreMutation mutation =
                Engine.PrepareAccountingStoreRemove(
                    recordIdentifier,
                    expectedVersion);
            if (mutation.Changed)
            {
                WriteAtomicAccountingStoreFile(mutation.Records);
                Engine.ApplyAccountingStoreMutation(mutation);
            }

            return mutation.Result;
        }
    }

    /// <summary>Reads independent file-backed records in creation order.</summary>
    /// <returns>All retained accounting records.</returns>
    /// <exception cref="System.IO.InvalidDataException">
    /// Thrown when file corruption was detected while loading the store.
    /// </exception>
    public List<DiameterAccountingRecord>
        ReadStoredDiameterAccountingRecords()
    {
        lock (StoreLock)
        {
            if (Corrupt)
            {
                throw new InvalidDataException(CorruptionMessage);
            }

            return Engine.ReadAccountingStoreRecords();
        }
    }

    internal void WriteAtomicAccountingStoreFile(
        List<DiameterAccountingRecord> records)
    {
        byte[] data =
            AccountingRecordStoreFileCodec.EncodeAccountingStoreFile(
                records);
        string temporaryPath = FilePath + "." +
            Guid.NewGuid().ToString("N") + ".tmp";
        using (FileStream stream = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            stream.Write(data, 0, data.Length);
            stream.Flush(true);
        }

        File.Move(temporaryPath, FilePath, true);
    }

    internal DiameterAccountingStoreResult
        CreateCorruptAccountingStoreResult(long recordIdentifier)
    {
        DiameterAccountingStoreResult result =
            new DiameterAccountingStoreResult();
        result.RecordIdentifier = recordIdentifier;
        result.Code = CorruptionCode;
        result.Message = CorruptionMessage;
        return result;
    }
}
