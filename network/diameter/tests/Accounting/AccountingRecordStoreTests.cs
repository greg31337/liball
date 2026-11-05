using System;
using System.Collections.Generic;
using System.IO;

namespace diameter.Tests;

public class AccountingRecordStoreTests
{
    [Test]
    public void FileStoreRejectsMissingPaths()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new DiameterFileAccountingRecordStore(null));
            Assert.Throws<ArgumentException>(() =>
                new DiameterFileAccountingRecordStore(string.Empty));
        });
    }

    [Test]
    public void InMemoryStoreOwnsRecordsAndEnforcesOptimisticVersions()
    {
        DiameterInMemoryAccountingRecordStore store =
            new DiameterInMemoryAccountingRecordStore();
        DiameterAccountingRecord first = CreateStoreRecord(
            10,
            "memory;1");

        DiameterAccountingStoreResult created =
            store.SaveDiameterAccountingRecord(first, 0);
        first.Request.SessionId = "mutated";
        first.Request.ApplicationState = new byte[] { 9 };
        DiameterAccountingStoreResult loaded =
            store.LoadDiameterAccountingRecord(10);
        loaded.Record!.Code = "updated";
        DiameterAccountingStoreResult updated =
            store.SaveDiameterAccountingRecord(loaded.Record, 1);
        DiameterAccountingStoreResult stale =
            store.SaveDiameterAccountingRecord(loaded.Record, 1);
        loaded.Record.Request.SessionId = "changed-after-load";
        DiameterAccountingStoreResult retained =
            store.LoadDiameterAccountingRecord(10);

        Assert.Multiple(() =>
        {
            Assert.That(created.Saved, Is.True);
            Assert.That(created.Record!.Version, Is.EqualTo(1));
            Assert.That(loaded.Found, Is.True);
            Assert.That(updated.Saved, Is.True);
            Assert.That(updated.Record!.Version, Is.EqualTo(2));
            Assert.That(stale.Conflict, Is.True);
            Assert.That(stale.Record!.Version, Is.EqualTo(2));
            Assert.That(retained.Record!.Request.SessionId,
                Is.EqualTo("memory;1"));
            Assert.That(retained.Record.Code, Is.EqualTo("updated"));
        });
    }

    [Test]
    public void InMemoryStorePreservesCreationOrderAndAtomicRemoval()
    {
        DiameterInMemoryAccountingRecordStore store =
            new DiameterInMemoryAccountingRecordStore();
        store.SaveDiameterAccountingRecord(
            CreateStoreRecord(20, "memory;20"),
            0);
        store.SaveDiameterAccountingRecord(
            CreateStoreRecord(21, "memory;21"),
            0);

        DiameterAccountingStoreResult conflict =
            store.RemoveDiameterAccountingRecord(20, 2);
        DiameterAccountingStoreResult removed =
            store.RemoveDiameterAccountingRecord(20, 1);
        DiameterAccountingStoreResult missing =
            store.RemoveDiameterAccountingRecord(20, 1);
        List<DiameterAccountingRecord> records =
            store.ReadStoredDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(conflict.Found, Is.True);
            Assert.That(conflict.Conflict, Is.True);
            Assert.That(removed.Removed, Is.True);
            Assert.That(missing.Found, Is.False);
            Assert.That(records.Count, Is.EqualTo(1));
            Assert.That(records[0].RecordIdentifier, Is.EqualTo(21));
        });
    }

    [Test]
    public void FileStoreSurvivesRestartAndAtomicReplacement()
    {
        string directory = CreateStoreTestDirectory();
        string path = Path.Combine(directory, "records.dat");
        try
        {
            DiameterFileAccountingRecordStore first =
                new DiameterFileAccountingRecordStore(path);
            first.SaveDiameterAccountingRecord(
                CreateStoreRecord(30, "file;30"),
                0);
            first.SaveDiameterAccountingRecord(
                CreateStoreRecord(31, "file;31"),
                0);

            DiameterFileAccountingRecordStore restarted =
                new DiameterFileAccountingRecordStore(path);
            List<DiameterAccountingRecord> records =
                restarted.ReadStoredDiameterAccountingRecords();
            records[0].Request.SessionId = "mutated";
            DiameterAccountingStoreResult loaded =
                restarted.LoadDiameterAccountingRecord(30);
            loaded.Record!.Message = "updated after restart";
            DiameterAccountingStoreResult updated =
                restarted.SaveDiameterAccountingRecord(
                    loaded.Record,
                    1);
            restarted.RemoveDiameterAccountingRecord(31, 1);

            DiameterFileAccountingRecordStore final =
                new DiameterFileAccountingRecordStore(path);
            List<DiameterAccountingRecord> retained =
                final.ReadStoredDiameterAccountingRecords();
            string[] temporaryFiles = Directory.GetFiles(
                directory,
                "*.tmp");

            Assert.Multiple(() =>
            {
                Assert.That(records.Count, Is.EqualTo(2));
                Assert.That(loaded.Record.Request.SessionId,
                    Is.EqualTo("file;30"));
                Assert.That(updated.Record!.Version, Is.EqualTo(2));
                Assert.That(retained.Count, Is.EqualTo(1));
                Assert.That(retained[0].RecordIdentifier, Is.EqualTo(30));
                Assert.That(retained[0].Version, Is.EqualTo(2));
                Assert.That(retained[0].Message,
                    Is.EqualTo("updated after restart"));
                Assert.That(temporaryFiles, Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FileStoreDetectsChecksumCorruptionWithoutOverwritingIt()
    {
        string directory = CreateStoreTestDirectory();
        string path = Path.Combine(directory, "corrupt.dat");
        try
        {
            DiameterFileAccountingRecordStore first =
                new DiameterFileAccountingRecordStore(path);
            first.SaveDiameterAccountingRecord(
                CreateStoreRecord(40, "corrupt;40"),
                0);
            byte[] corrupted = File.ReadAllBytes(path);
            corrupted[corrupted.Length - 1] =
                (byte)(corrupted[corrupted.Length - 1] ^ 255);
            File.WriteAllBytes(path, corrupted);
            DiameterFileAccountingRecordStore detected =
                new DiameterFileAccountingRecordStore(path);

            DiameterAccountingStoreResult loaded =
                detected.LoadDiameterAccountingRecord(40);
            DiameterAccountingStoreResult save =
                detected.SaveDiameterAccountingRecord(
                    CreateStoreRecord(41, "corrupt;41"),
                    0);
            byte[] afterSave = File.ReadAllBytes(path);

            Assert.Multiple(() =>
            {
                Assert.That(loaded.Found, Is.False);
                Assert.That(loaded.Code,
                    Is.EqualTo("accounting-store-checksum-invalid"));
                Assert.That(save.Saved, Is.False);
                Assert.That(save.Code,
                    Is.EqualTo("accounting-store-checksum-invalid"));
                Assert.That(afterSave, Is.EqualTo(corrupted));
                Assert.Throws<InvalidDataException>(() =>
                    detected.ReadStoredDiameterAccountingRecords());
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FileBackedPendingRecordReplaysAfterProcessRestart()
    {
        string directory = CreateStoreTestDirectory();
        string path = Path.Combine(directory, "recovery.dat");
        try
        {
            DiameterFileAccountingRecordStore firstStore =
                new DiameterFileAccountingRecordStore(path);
            PolicyAccountingRequestClient firstRequests =
                new PolicyAccountingRequestClient();
            DiameterAccountingClient firstClient = CreateStoreTestClient(
                firstStore,
                firstRequests);
            DiameterAccountingRequest request =
                CreateStoreAccountingRequest("file-recovery;1");
            firstClient.SendDiameterAccountingEvent(request);
            firstRequests.FailAccountingRequest(0, 1, false);

            DiameterFileAccountingRecordStore restartedStore =
                new DiameterFileAccountingRecordStore(path);
            PolicyAccountingRequestClient recoveredRequests =
                new PolicyAccountingRequestClient();
            DiameterAccountingClient recoveredClient =
                CreateStoreTestClient(
                    restartedStore,
                    recoveredRequests);
            List<DiameterAccountingRecord> replaying =
                recoveredClient.ReadDiameterAccountingRecords();
            recoveredRequests.CompleteAccountingRequest(0);

            DiameterFileAccountingRecordStore finalStore =
                new DiameterFileAccountingRecordStore(path);

            Assert.Multiple(() =>
            {
                Assert.That(recoveredRequests.Requests.Count,
                    Is.EqualTo(1));
                Assert.That(recoveredRequests.Requests[0].Request.Flags &
                    DiameterMessageFlags.PotentiallyRetransmitted,
                    Is.Not.EqualTo(DiameterMessageFlags.None));
                Assert.That(replaying[0].Request.SessionId,
                    Is.EqualTo("file-recovery;1"));
                Assert.That(replaying[0].AttemptCount, Is.EqualTo(2));
                Assert.That(finalStore.ReadStoredDiameterAccountingRecords(),
                    Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PublicStoresReportMissingConflictsAndRejectInvalidKeys()
    {
        DiameterInMemoryAccountingRecordStore memory =
            new DiameterInMemoryAccountingRecordStore();
        DiameterAccountingStoreResult memoryMissing =
            memory.LoadDiameterAccountingRecord(50);
        DiameterAccountingStoreResult memoryConflict =
            memory.SaveDiameterAccountingRecord(
                CreateStoreRecord(50, "memory-conflict;50"),
                1);

        string directory = CreateStoreTestDirectory();
        string path = Path.Combine(directory, "outcomes.dat");
        try
        {
            DiameterFileAccountingRecordStore file =
                new DiameterFileAccountingRecordStore(path);
            DiameterAccountingStoreResult fileMissing =
                file.LoadDiameterAccountingRecord(60);
            DiameterAccountingStoreResult fileConflict =
                file.SaveDiameterAccountingRecord(
                    CreateStoreRecord(60, "file-conflict;60"),
                    1);

            Assert.Multiple(() =>
            {
                Assert.That(memoryMissing.Found, Is.False);
                Assert.That(memoryMissing.Code,
                    Is.EqualTo("accounting-record-not-found"));
                Assert.That(memoryConflict.Conflict, Is.True);
                Assert.That(memoryConflict.Found, Is.False);
                Assert.That(memoryConflict.Code,
                    Is.EqualTo("accounting-version-conflict"));
                Assert.That(fileMissing.Code,
                    Is.EqualTo("accounting-record-not-found"));
                Assert.That(fileConflict.Conflict, Is.True);
                Assert.That(File.Exists(path), Is.False);
                Assert.That(
                    () => memory.LoadDiameterAccountingRecord(0),
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(
                    () => file.LoadDiameterAccountingRecord(-1),
                    Throws.TypeOf<ArgumentOutOfRangeException>());
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FileStoreClassifiesEnvelopeCorruptionOutcomes()
    {
        string directory = CreateStoreTestDirectory();
        string truncatedPath = Path.Combine(directory, "truncated.dat");
        string headerPath = Path.Combine(directory, "header.dat");
        string lengthPath = Path.Combine(directory, "length.dat");
        try
        {
            File.WriteAllBytes(
                truncatedPath,
                new byte[] { 1, 2, 3 });
            File.WriteAllBytes(
                headerPath,
                new byte[
                    AccountingRecordStoreFileCodec.HeaderLength +
                    AccountingRecordStoreFileCodec.HashLength]);
            byte[] invalidLength =
                AccountingRecordStoreFileCodec.
                    EncodeAccountingStoreFile(
                        new List<DiameterAccountingRecord>());
            uint payloadLength = NetworkByteOrder.ReadUInt32(
                invalidLength.AsSpan(8));
            NetworkByteOrder.WriteUInt32(
                invalidLength.AsSpan(8),
                payloadLength + 1U);
            File.WriteAllBytes(lengthPath, invalidLength);

            DiameterFileAccountingRecordStore truncated =
                new DiameterFileAccountingRecordStore(truncatedPath);
            DiameterFileAccountingRecordStore header =
                new DiameterFileAccountingRecordStore(headerPath);
            DiameterFileAccountingRecordStore length =
                new DiameterFileAccountingRecordStore(lengthPath);

            Assert.Multiple(() =>
            {
                Assert.That(truncated.LoadDiameterAccountingRecord(1).Code,
                    Is.EqualTo("accounting-store-truncated"));
                Assert.That(header.LoadDiameterAccountingRecord(1).Code,
                    Is.EqualTo("accounting-store-header-invalid"));
                Assert.That(length.LoadDiameterAccountingRecord(1).Code,
                    Is.EqualTo("accounting-store-length-invalid"));
                Assert.That(
                    () => truncated.ReadStoredDiameterAccountingRecords(),
                    Throws.TypeOf<InvalidDataException>());
                Assert.That(
                    () => header.RemoveDiameterAccountingRecord(1, 0),
                    Throws.Nothing);
                Assert.That(header.RemoveDiameterAccountingRecord(1, 0).Code,
                    Is.EqualTo("accounting-store-header-invalid"));
                Assert.That(
                    truncated.SaveDiameterAccountingRecord(null, 0)
                        .RecordIdentifier,
                    Is.Zero);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static DiameterAccountingRecord CreateStoreRecord(
        long recordIdentifier,
        string sessionId)
    {
        DateTimeOffset now = new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);
        DiameterAccountingRecord result =
            new DiameterAccountingRecord();
        result.RecordIdentifier = recordIdentifier;
        result.Request = CreateStoreAccountingRequest(sessionId);
        result.OutboundMessage = new DiameterMessage();
        result.OutboundMessage.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.OutboundMessage.CommandCode =
            DiameterCommandCodes.Accounting;
        result.OutboundMessage.ApplicationId =
            DiameterApplicationIds.BaseAccounting;
        result.State = DiameterAccountingState.PendingEvent;
        result.CreatedAt = now;
        result.UpdatedAt = now;
        result.LastAttemptAt = now;
        result.AttemptCount = 1;
        result.Durable = true;
        result.Code = "pending";
        result.Message = "Pending durable accounting record.";
        return result;
    }

    private static DiameterAccountingRequest CreateStoreAccountingRequest(
        string sessionId)
    {
        DiameterAccountingRequest result =
            new DiameterAccountingRequest();
        result.SessionId = sessionId;
        result.ApplicationId = DiameterApplicationIds.BaseAccounting;
        result.DestinationRealm.Value = "remote.example";
        result.RealtimeRequired =
            DiameterAccountingRealtimeRequired.GrantAndStore;
        return result;
    }

    private static DiameterAccountingClient CreateStoreTestClient(
        IAccountingRecordStore store,
        PolicyAccountingRequestClient requests)
    {
        DiameterIdentity originHost = new DiameterIdentity();
        originHost.Value = "client.example";
        DiameterIdentity originRealm = new DiameterIdentity();
        originRealm.Value = "example";
        return new DiameterAccountingClient(
            originHost,
            originRealm,
            new DiameterBaseAvpFactory(),
            requests,
            store,
            new TestDiameterClock(
                new DateTimeOffset(
                    2026,
                    8,
                    29,
                    12,
                    0,
                    0,
                    TimeSpan.Zero),
                0));
    }

    private static string CreateStoreTestDirectory()
    {
        string result = Path.Combine(
            Path.GetTempPath(),
            "diameter-accounting-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(result);
        return result;
    }
}
