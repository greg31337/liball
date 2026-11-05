using System;
using System.Collections.Generic;
using System.IO;

namespace fix;

/// <summary>Persists replayable messages and session sequence state in a file-backed store.</summary>
public class FileMessageStore : IMessageStore
{
    internal static readonly object ProcessLock = new object();
    internal string DirectoryPath;
    internal string AppendPath;
    internal string IndexPath;
    internal string SequencePath;
    internal string SequenceTemporaryPath;
    internal string LockPath;
    internal string AppendTemporaryPath;
    internal string IndexTemporaryPath;
    internal int MaximumMessages;
    internal int NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
    internal int NextTargetSeqNum = FixDefaults.InitialSequenceNumber;
    internal long SequenceGeneration;
    internal List<FileStoreRecord> Records = [];

    /// <summary>Creates a named file store with count-based message retention.</summary>
    /// <param name="directoryPath">The directory containing the store files.</param>
    /// <param name="storeName">The safe file-name prefix for this store.</param>
    /// <param name="maximumMessages">The maximum retained message count, or zero for no count limit.</param>
    /// <exception cref="FixStoreException">Thrown when a constructor argument is invalid.</exception>
    public FileMessageStore(string directoryPath, string storeName, int maximumMessages)
    {
        if (directoryPath == null || directoryPath.Length == 0)
        {
            throw new FixStoreException("The file store directory path is missing.");
        }
        if (!IsValidFileStoreName(storeName))
        {
            throw new FixStoreException("The file store name contains invalid characters.");
        }
        if (maximumMessages < 0)
        {
            throw new FixStoreException("The maximum retained message count cannot be negative.");
        }
        DirectoryPath = Path.GetFullPath(directoryPath);
        MaximumMessages = maximumMessages;
        AppendPath = Path.Combine(DirectoryPath, storeName + ".messages");
        IndexPath = Path.Combine(DirectoryPath, storeName + ".index");
        SequencePath = Path.Combine(DirectoryPath, storeName + ".sequence");
        SequenceTemporaryPath = Path.Combine(DirectoryPath, storeName + ".sequence.tmp");
        LockPath = Path.Combine(DirectoryPath, storeName + ".lock");
        AppendTemporaryPath = Path.Combine(DirectoryPath, storeName + ".messages.compact.tmp");
        IndexTemporaryPath = Path.Combine(DirectoryPath, storeName + ".index.compact.tmp");
        Directory.CreateDirectory(DirectoryPath);
    }

    /// <summary>Appends and flushes one message before reporting storage success.</summary>
    /// <param name="message">The outbound record to persist.</param>
    /// <returns>The explicit durable store outcome.</returns>
    public StoreRangeResult StoreMessage(StoredMessage message)
    {
        lock (ProcessLock)
        {
            using (FileStream storeLock = OpenFileStoreLock())
            {
                StoreRangeResult refreshed = RefreshFileStoreState();
                if (!refreshed.Success)
                {
                    return refreshed;
                }
                string validationCode = ValidateFileStoredMessage(message);
                if (validationCode.Length != 0)
                {
                    return CreateFileFailureResult(validationCode, "The outbound record is invalid for durable storage.", false);
                }
                int latest = FindLatestFileSequenceNumber();
                if (message.SequenceNumber <= latest)
                {
                    return CreateFileFailureResult("message-sequence-not-increasing", "Durable message sequence numbers must increase.", false);
                }

                FileStoreRecordReadResult appended;
                using (FileStream append = OpenAppendFile())
                {
                    appended = FileStoreRecordFormat.WriteAppendRecord(append, message);
                    append.Flush(true);
                }
                if (!appended.Success || appended.Record == null)
                {
                    return CreateFileFailureResult(appended.Code, "The durable message record could not be encoded.", appended.IsCorrupt);
                }
                using (FileStream index = OpenIndexFile())
                {
                    if (!FileStoreIndexFormat.AppendIndexEntry(index, appended.Record))
                    {
                        return CreateFileFailureResult("index-append-failed", "The durable message index entry could not be encoded.", true);
                    }
                    index.Flush(true);
                }
                Records.Add(appended.Record);
                ApplyFileMessageRetention();
                StoreRangeResult result = CreateFileSuccessResult();
                result.BeginSeqNo = message.SequenceNumber;
                result.EndSeqNo = message.SequenceNumber;
                result.Code = refreshed.Code;
                result.Text = refreshed.Text;
                return result;
            }
        }
    }

    /// <summary>Reads detached records for an inclusive durable sequence range.</summary>
    /// <param name="beginSeqNo">The first sequence number.</param>
    /// <param name="endSeqNo">The final sequence number, or zero for the latest retained record.</param>
    /// <returns>The available ordered records, gap status, and sequence state.</returns>
    public StoreRangeResult ReadMessageRange(int beginSeqNo, int endSeqNo)
    {
        lock (ProcessLock)
        {
            using (FileStream storeLock = OpenFileStoreLock())
            {
                StoreRangeResult refreshed = RefreshFileStoreState();
                if (!refreshed.Success)
                {
                    return refreshed;
                }
                if (beginSeqNo < FixDefaults.InitialSequenceNumber || endSeqNo < 0)
                {
                    return CreateFileFailureResult("invalid-message-range", "Message range sequence numbers are invalid.", false);
                }
                if (endSeqNo != 0 && endSeqNo < beginSeqNo)
                {
                    return CreateFileFailureResult("inverted-message-range", "The message range ends before it begins.", false);
                }

                StoreRangeResult result = CreateFileSuccessResult();
                result.BeginSeqNo = beginSeqNo;
                int resolvedEnd = endSeqNo == 0 ? FindLatestFileSequenceNumber() : endSeqNo;
                result.EndSeqNo = resolvedEnd;
                if (resolvedEnd < beginSeqNo)
                {
                    result.HasGap = true;
                    return result;
                }
                for (int index = 0; index < Records.Count; index++)
                {
                    FileStoreRecord record = Records[index];
                    if (record.Message.SequenceNumber < beginSeqNo) continue;
                    if (record.Message.SequenceNumber > resolvedEnd) break;
                    result.Messages.Add(FileStoreRecordFormat.CloneRecordMessage(record.Message));
                }
                long requestedCount = (long)resolvedEnd - beginSeqNo + 1L;
                result.HasGap = result.Messages.Count != requestedCount;
                return result;
            }
        }
    }

    /// <summary>Atomically replaces the durable sender and target sequence snapshot.</summary>
    /// <param name="nextSenderSeqNum">The next sender sequence number.</param>
    /// <param name="nextTargetSeqNum">The next target sequence number.</param>
    /// <returns>The explicit durable store outcome.</returns>
    public StoreRangeResult SaveSequenceState(int nextSenderSeqNum, int nextTargetSeqNum)
    {
        lock (ProcessLock)
        {
            using (FileStream storeLock = OpenFileStoreLock())
            {
                StoreRangeResult refreshed = RefreshFileStoreState();
                if (!refreshed.Success)
                {
                    return refreshed;
                }
                if (nextSenderSeqNum < FixDefaults.InitialSequenceNumber || nextTargetSeqNum < FixDefaults.InitialSequenceNumber)
                {
                    return CreateFileFailureResult("invalid-sequence-state", "Next sender and target sequence numbers must be positive.", false);
                }
                long nextGeneration = SequenceGeneration + 1;
                if (!WriteAtomicSequenceSnapshot(nextGeneration, nextSenderSeqNum, nextTargetSeqNum))
                {
                    return CreateFileFailureResult("sequence-write-failed", "The sequence snapshot could not be encoded.", true);
                }
                SequenceGeneration = nextGeneration;
                NextSenderSeqNum = nextSenderSeqNum;
                NextTargetSeqNum = nextTargetSeqNum;
                return CreateFileSuccessResult();
            }
        }
    }

    /// <summary>Reloads durable message and sequence state and performs safe restart recovery.</summary>
    /// <returns>The explicit refresh outcome and current sequence state.</returns>
    public StoreRangeResult RefreshStore()
    {
        lock (ProcessLock)
        {
            using (FileStream storeLock = OpenFileStoreLock())
            {
                return RefreshFileStoreState();
            }
        }
    }

    /// <summary>Atomically clears durable records and restores initial sequence state.</summary>
    /// <returns>The explicit reset outcome.</returns>
    public StoreRangeResult ResetStore()
    {
        lock (ProcessLock)
        {
            using (FileStream storeLock = OpenFileStoreLock())
            {
                using (FileStream append = new FileStream(AppendPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    append.Flush(true);
                }
                using (FileStream index = new FileStream(IndexPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    FileStoreIndexFormat.InitializeIndexFile(index);
                    index.Flush(true);
                }
                if (!WriteAtomicSequenceSnapshot(0, FixDefaults.InitialSequenceNumber, FixDefaults.InitialSequenceNumber))
                {
                    return CreateFileFailureResult("sequence-write-failed", "The reset sequence snapshot could not be encoded.", true);
                }
                Records.Clear();
                SequenceGeneration = 0;
                NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
                NextTargetSeqNum = FixDefaults.InitialSequenceNumber;
                DeleteFileStoreTemporaryFiles();
                return CreateFileSuccessResult();
            }
        }
    }

    internal StoreRangeResult RefreshFileStoreState()
    {
        EnsureFileStoreFiles();
        string recoveryCode = "";
        string recoveryText = "";
        FileStoreSequenceResult sequence = LoadFileSequenceState();
        if (!sequence.Success)
        {
            StoreRangeResult failure = CreateFileFailureResult(sequence.Code, "The durable sequence snapshot is corrupt or incomplete.", true);
            failure.HasSequenceState = false;
            return failure;
        }
        SequenceGeneration = sequence.Generation;
        NextSenderSeqNum = sequence.NextSenderSeqNum;
        NextTargetSeqNum = sequence.NextTargetSeqNum;

        FileStoreScanResult scan;
        using (FileStream append = OpenAppendFile())
        {
            scan = FileStoreRecordFormat.ScanAppendFile(append);
            if (scan.IsCorrupt)
            {
                return CreateFileFailureResult(scan.Code, "Corrupt durable message data was detected.", true);
            }
            if (scan.IsPartialTail)
            {
                append.SetLength(scan.ValidLength);
                append.Flush(true);
                recoveryCode = "recovered-partial-message-tail";
                recoveryText = "A partial durable message tail was removed during refresh.";
            }
        }
        Records = scan.Records;

        bool rebuildIndex = false;
        using (FileStream index = OpenIndexFile())
        {
            FileStoreIndexReadResult indexed = FileStoreIndexFormat.ReadIndexFile(index, scan.ValidLength);
            rebuildIndex = !indexed.Success || !IndexMatchesFileRecords(indexed.Entries);
        }
        if (rebuildIndex)
        {
            RebuildFileStoreIndex();
            if (recoveryCode.Length == 0)
            {
                recoveryCode = "rebuilt-message-index";
                recoveryText = "The durable message index was rebuilt from append records.";
            }
        }
        ApplyFileMessageRetention();
        StoreRangeResult result = CreateFileSuccessResult();
        result.Code = recoveryCode;
        result.Text = recoveryText;
        return result;
    }

    internal void EnsureFileStoreFiles()
    {
        Directory.CreateDirectory(DirectoryPath);
        if (!File.Exists(AppendPath))
        {
            using (FileStream append = new FileStream(AppendPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                append.Flush(true);
            }
        }
        if (!File.Exists(IndexPath))
        {
            using (FileStream index = new FileStream(IndexPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                FileStoreIndexFormat.InitializeIndexFile(index);
                index.Flush(true);
            }
        }
        if (!File.Exists(SequencePath) && !File.Exists(SequenceTemporaryPath))
        {
            WriteAtomicSequenceSnapshot(0, FixDefaults.InitialSequenceNumber, FixDefaults.InitialSequenceNumber);
        }
    }

    internal FileStoreSequenceResult LoadFileSequenceState()
    {
        bool hasMain = File.Exists(SequencePath);
        bool hasTemporary = File.Exists(SequenceTemporaryPath);
        FileStoreSequenceResult main = new FileStoreSequenceResult();
        FileStoreSequenceResult temporary = new FileStoreSequenceResult();
        if (hasMain) main = ReadFileSequencePath(SequencePath);
        if (hasTemporary) temporary = ReadFileSequencePath(SequenceTemporaryPath);

        if (hasMain && main.Success && hasTemporary && temporary.Success)
        {
            if (temporary.Generation > main.Generation)
            {
                File.Move(SequenceTemporaryPath, SequencePath, true);
                return temporary;
            }
            File.Delete(SequenceTemporaryPath);
            return main;
        }
        if (hasMain && main.Success)
        {
            if (hasTemporary) File.Delete(SequenceTemporaryPath);
            return main;
        }
        if (hasTemporary && temporary.Success)
        {
            File.Move(SequenceTemporaryPath, SequencePath, true);
            return temporary;
        }
        if (hasMain) return main;
        if (hasTemporary) return temporary;
        FileStoreSequenceResult missing = new FileStoreSequenceResult();
        missing.IsCorrupt = true;
        missing.Code = "missing-sequence-state";
        return missing;
    }

    internal FileStoreSequenceResult ReadFileSequencePath(string path)
    {
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            return FileStoreIndexFormat.ReadSequenceFile(stream);
        }
    }

    internal bool WriteAtomicSequenceSnapshot(long generation, int nextSenderSeqNum, int nextTargetSeqNum)
    {
        bool written;
        using (FileStream temporary = new FileStream(SequenceTemporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            written = FileStoreIndexFormat.WriteSequenceFile(temporary, generation, nextSenderSeqNum, nextTargetSeqNum);
            temporary.Flush(true);
        }
        if (!written) return false;
        File.Move(SequenceTemporaryPath, SequencePath, true);
        return true;
    }

    internal void RebuildFileStoreIndex()
    {
        using (FileStream index = new FileStream(IndexPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            FileStoreIndexFormat.InitializeIndexFile(index);
            for (int recordIndex = 0; recordIndex < Records.Count; recordIndex++)
            {
                FileStoreIndexFormat.AppendIndexEntry(index, Records[recordIndex]);
            }
            index.Flush(true);
        }
    }

    internal bool IndexMatchesFileRecords(List<FileStoreIndexEntry> entries)
    {
        if (entries.Count != Records.Count) return false;
        for (int index = 0; index < Records.Count; index++)
        {
            FileStoreRecord record = Records[index];
            FileStoreIndexEntry entry = entries[index];
            if (entry.SequenceNumber != record.Message.SequenceNumber || entry.RecordOffset != record.Offset || entry.RecordLength != record.Length || entry.RecordChecksum != record.Checksum)
            {
                return false;
            }
        }
        return true;
    }

    internal void ApplyFileMessageRetention()
    {
        if (MaximumMessages == 0 || Records.Count <= MaximumMessages) return;
        int firstRetained = Records.Count - MaximumMessages;
        List<FileStoreRecord> retained = [];
        using (FileStream append = new FileStream(AppendTemporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        using (FileStream index = new FileStream(IndexTemporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            FileStoreIndexFormat.InitializeIndexFile(index);
            for (int recordIndex = firstRetained; recordIndex < Records.Count; recordIndex++)
            {
                FileStoreRecordReadResult written = FileStoreRecordFormat.WriteAppendRecord(append, Records[recordIndex].Message);
                if (written.Record != null)
                {
                    FileStoreIndexFormat.AppendIndexEntry(index, written.Record);
                    retained.Add(written.Record);
                }
            }
            append.Flush(true);
            index.Flush(true);
        }
        File.Move(AppendTemporaryPath, AppendPath, true);
        File.Move(IndexTemporaryPath, IndexPath, true);
        Records = retained;
    }

    internal FileStream OpenFileStoreLock()
    {
        Directory.CreateDirectory(DirectoryPath);
        return new FileStream(LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    internal FileStream OpenAppendFile()
    {
        return new FileStream(AppendPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
    }

    internal FileStream OpenIndexFile()
    {
        return new FileStream(IndexPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
    }

    internal int FindLatestFileSequenceNumber()
    {
        if (Records.Count == 0) return 0;
        return Records[Records.Count - 1].Message.SequenceNumber;
    }

    internal StoreRangeResult CreateFileSuccessResult()
    {
        StoreRangeResult result = new StoreRangeResult();
        result.Success = true;
        CopyFileSequenceState(result);
        return result;
    }

    internal StoreRangeResult CreateFileFailureResult(string code, string text, bool corrupt)
    {
        StoreRangeResult result = new StoreRangeResult();
        result.Code = code;
        result.Text = text;
        result.IsCorrupt = corrupt;
        CopyFileSequenceState(result);
        return result;
    }

    internal void CopyFileSequenceState(StoreRangeResult result)
    {
        result.HasSequenceState = true;
        result.NextSenderSeqNum = NextSenderSeqNum;
        result.NextTargetSeqNum = NextTargetSeqNum;
    }

    internal string ValidateFileStoredMessage(StoredMessage message)
    {
        return StoredMessageValidation.ValidateStoredMessage(message);
    }

    internal static bool IsValidFileStoreName(string storeName)
    {
        if (storeName == null || storeName.Length == 0 || storeName == "." || storeName == "..") return false;
        for (int index = 0; index < storeName.Length; index++)
        {
            char value = storeName[index];
            bool valid = value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z' || value >= '0' && value <= '9' || value == '-' || value == '_' || value == '.';
            if (!valid) return false;
        }
        return true;
    }

    internal void DeleteFileStoreTemporaryFiles()
    {
        if (File.Exists(SequenceTemporaryPath)) File.Delete(SequenceTemporaryPath);
        if (File.Exists(AppendTemporaryPath)) File.Delete(AppendTemporaryPath);
        if (File.Exists(IndexTemporaryPath)) File.Delete(IndexTemporaryPath);
    }
}
