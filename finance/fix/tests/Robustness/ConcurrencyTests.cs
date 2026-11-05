using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fix;

namespace fix.Tests;

[TestFixture]
public class ConcurrencyTests
{
    private string ConcurrencyDirectory = "";

    [SetUp]
    public void CreateConcurrencyDirectory()
    {
        ConcurrencyDirectory = Path.Combine(Path.GetTempPath(), "fix-concurrency-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ConcurrencyDirectory);
    }

    [TearDown]
    public void RemoveConcurrencyDirectory()
    {
        if (Directory.Exists(ConcurrencyDirectory)) Directory.Delete(ConcurrencyDirectory, true);
    }

    [Test]
    [CancelAfter(15000)]
    public void SharedDictionarySupportsConcurrentReadOnlyConsumersWithoutMutation()
    {
        FixDictionary dictionary = SharedDictionary();
        string original = DictionaryText(dictionary);
        byte[] frame = BuildFrame("35=D\u000111=ORDER-1\u0001");
        string[] outcomes = new string[64];

        Parallel.For(0, outcomes.Length, index =>
        {
            DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(frame, dictionary, new DecodeOptions());
            FixFieldDefinition? field = new FixDictionaryQuery().ResolveFieldDefinition(dictionary, 11);
            ValidationResult validated = decoded.Message == null
                ? new ValidationResult()
                : new FixMessageValidator().ValidateMessage(decoded.Message, dictionary, new ValidationOptions(), new ValidationContext());
            GenerationResult generated = new CSharpModelGenerator().GenerateModels(dictionary, new ModelGenerationOptions { MessageNames = ["NewOrderSingle"] });
            outcomes[index] = decoded.Status + ":" + decoded.ErrorCode + ":" + (field == null ? "" : field.Name) + ":" + validated.IsValid + ":" + string.Join(",", validated.Issues.ConvertAll(item => item.Code)) + ":" + generated.Success + ":" + generated.Manifest;
        });

        Assert.Multiple(() =>
        {
            Assert.That(outcomes, Is.All.EqualTo(outcomes[0]));
            Assert.That(outcomes[0], Does.StartWith("Success::ClOrdID:True"));
            Assert.That(DictionaryText(dictionary), Is.EqualTo(original));
            Assert.That(dictionary.Diagnostics, Is.Empty);
        });
    }

    [Test]
    [CancelAfter(15000)]
    public void MemoryStoreSerializesConcurrentRecordsAndSequencePairs()
    {
        MemoryMessageStore store = new MemoryMessageStore(0);
        StoreRangeResult[] stored = new StoreRangeResult[128];
        Parallel.For(0, stored.Length, index =>
        {
            stored[index] = store.StoreMessage(Stored(index + 1));
        });

        StoreRangeResult[] sequenceUpdates = new StoreRangeResult[128];
        Parallel.For(0, sequenceUpdates.Length, index =>
        {
            int sender = index + 2;
            sequenceUpdates[index] = store.SaveSequenceState(sender, sender + 1000);
        });
        StoreRangeResult range = store.ReadMessageRange(1, stored.Length);
        StoreRangeResult state = store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.All.Matches<StoreRangeResult>(item => item.Success));
            Assert.That(sequenceUpdates, Is.All.Matches<StoreRangeResult>(item => item.Success && item.NextTargetSeqNum == item.NextSenderSeqNum + 1000));
            Assert.That(range.Success, Is.True, range.Code);
            Assert.That(range.HasGap, Is.False);
            Assert.That(range.Messages.ConvertAll(item => item.SequenceNumber), Is.EqualTo(Enumerable.Range(1, stored.Length)));
            Assert.That(state.NextTargetSeqNum, Is.EqualTo(state.NextSenderSeqNum + 1000));
        });
    }

    [Test]
    [CancelAfter(15000)]
    public void EngineSerializesDynamicAddRemovePollAndSnapshotOperations()
    {
        FixSessionEngine engine = MultiSessionTests.CreateEngine();
        SessionSettings anchor = MultiSessionTests.InactiveSettings("ANCHOR", "REMOTE", "ANCHOR");
        engine.AddSession(anchor);
        ManualResetEventSlim start = new ManualResetEventSlim(false);
        Task[] tasks = new Task[9];
        tasks[0] = Task.Run(() =>
        {
            start.Wait();
            for (int index = 0; index < 200; index++)
            {
                SessionPollResult poll = engine.PollSessions();
                Assert.That(poll.State, Is.AnyOf(SessionState.Disconnected, SessionState.Stopped));
                SessionSnapshot snapshot = engine.ResolveSessionSnapshot(anchor.Id);
                Assert.That(snapshot.Id.SenderCompID, Is.EqualTo("ANCHOR"));
            }
        });
        for (int worker = 1; worker < tasks.Length; worker++)
        {
            int workerIndex = worker;
            tasks[worker] = Task.Run(() =>
            {
                start.Wait();
                for (int iteration = 0; iteration < 20; iteration++)
                {
                    string suffix = workerIndex + "-" + iteration;
                    SessionSettings settings = MultiSessionTests.InactiveSettings("LOCAL-" + suffix, "REMOTE", "Q-" + suffix);
                    SessionPollResult added = engine.AddSession(settings);
                    SessionSnapshot snapshot = engine.ResolveSessionSnapshot(settings.Id);
                    SessionPollResult removed = engine.RemoveSession(settings.Id);
                    Assert.That(added.State, Is.EqualTo(SessionState.Disconnected));
                    Assert.That(snapshot.Id.SenderCompID, Is.EqualTo(settings.Id.SenderCompID));
                    Assert.That(removed.State, Is.EqualTo(SessionState.Stopped));
                }
            });
        }

        start.Set();
        Task.WaitAll(tasks);
        SessionSnapshot anchorSnapshot = engine.ResolveSessionSnapshot(anchor.Id);
        SessionPollResult anchorRemoved = engine.RemoveSession(anchor.Id);

        Assert.Multiple(() =>
        {
            Assert.That(anchorSnapshot.Id.SenderCompID, Is.EqualTo("ANCHOR"));
            Assert.That(anchorSnapshot.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(anchorRemoved.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(engine.PollSessions().State, Is.EqualTo(SessionState.Stopped));
        });
    }

    [Test]
    [CancelAfter(15000)]
    public void IndependentSessionsCanProgressSimultaneously()
    {
        FixSession[] sessions = new FixSession[32];
        SessionPollResult[] started = new SessionPollResult[sessions.Length];
        SessionPollResult[] stopped = new SessionPollResult[sessions.Length];
        for (int index = 0; index < sessions.Length; index++) sessions[index] = InactiveSession(index);

        Parallel.For(0, sessions.Length, index =>
        {
            started[index] = sessions[index].StartSession();
            for (int poll = 0; poll < 20; poll++)
            {
                SessionPollResult result = sessions[index].PollSession();
                SessionSnapshot snapshot = sessions[index].GetSessionSnapshot();
                Assert.That(result.State, Is.EqualTo(SessionState.Disconnected));
                Assert.That(snapshot.Id.SenderCompID, Is.EqualTo("LOCAL-" + index));
                Assert.That(snapshot.NextSenderSeqNum, Is.GreaterThanOrEqualTo(1));
                Assert.That(snapshot.NextTargetSeqNum, Is.GreaterThanOrEqualTo(1));
            }
            stopped[index] = sessions[index].StopSession();
        });

        Assert.Multiple(() =>
        {
            Assert.That(started, Is.All.Matches<SessionPollResult>(item => item.State == SessionState.Disconnected));
            Assert.That(stopped, Is.All.Matches<SessionPollResult>(item => item.State == SessionState.Stopped));
            Assert.That(sessions.Select(item => item.GetSessionSnapshot().State), Is.All.EqualTo(SessionState.Stopped));
        });
    }

    [Test]
    [CancelAfter(15000)]
    public void FileLogSerializesConcurrentWritersIntoCompleteRecords()
    {
        string path = Path.Combine(ConcurrencyDirectory, "concurrent.audit");
        FileFixLog first = new FileFixLog(path, false);
        FileFixLog second = new FileFixLog(path, false);
        Parallel.For(0, 128, index =>
        {
            FileFixLog selected = index % 2 == 0 ? first : second;
            selected.WriteLog(LogEntry(index));
        });

        string[] lines = File.ReadAllLines(path);
        string[] texts = lines.Select(line => line.Split('\t')[8]).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        string[] expected = Enumerable.Range(0, 128).Select(index => "entry-" + index).OrderBy(value => value, StringComparer.Ordinal).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Length.EqualTo(128));
            Assert.That(lines, Is.All.Matches<string>(line => line.Split('\t').Length == 10));
            Assert.That(texts, Is.EqualTo(expected));
        });
    }

    [Test]
    [CancelAfter(15000)]
    public void SnapshotsRemainDetachedAndConsistentDuringConcurrentLifecycleChanges()
    {
        FixSession session = InactiveSession(999);
        ConcurrentBag<SessionSnapshot> snapshots = new ConcurrentBag<SessionSnapshot>();
        Parallel.For(0, 512, index =>
        {
            if (index % 4 == 0) session.StartSession();
            else if (index % 4 == 1) session.PollSession();
            else if (index % 4 == 2) session.StopSession();
            SessionSnapshot snapshot = session.GetSessionSnapshot();
            snapshots.Add(snapshot);
            snapshot.Id.SenderCompID = "MUTATED";
            snapshot.NextSenderSeqNum = int.MaxValue;
        });
        session.StopSession();
        SessionSnapshot final = session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(snapshots, Has.Count.EqualTo(512));
            Assert.That(snapshots.Select(item => item.State), Is.All.Matches<SessionState>(state => state == SessionState.Stopped || state == SessionState.Disconnected));
            Assert.That(snapshots.Select(item => item.NextTargetSeqNum), Is.All.GreaterThanOrEqualTo(1));
            Assert.That(final.Id.SenderCompID, Is.EqualTo("LOCAL-999"));
            Assert.That(final.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(final.NextTargetSeqNum, Is.EqualTo(1));
            Assert.That(final.State, Is.EqualTo(SessionState.Stopped));
        });
    }

    private static FixSession InactiveSession(int index)
    {
        SessionSettings settings = MultiSessionTests.InactiveSettings("LOCAL-" + index, "REMOTE", "Q-" + index);
        FakeSessionClock clock = new FakeSessionClock(MultiSessionTests.Now);
        return new FixSession(settings, new FixSessionTests.FakeSessionTransport(), new MemoryMessageStore(0), new NullFixLog(), clock, new FixSessionTests.RecordingThrottle(), new FixSessionTests.RecordingSessionApplication(), new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixSessionTests.AcceptingMessageValidator(), new FixSessionTests.EmptyDictionaryProvider(), new FixSessionTests.AcceptingAuthenticator());
    }

    private static StoredMessage Stored(int sequenceNumber)
    {
        return new StoredMessage
        {
            SequenceNumber = sequenceNumber,
            SendingTimeUtc = MultiSessionTests.Now.AddSeconds(sequenceNumber),
            MsgType = "D",
            Bytes = [(byte)(sequenceNumber % 251), FixDefaults.Soh]
        };
    }

    private static LogEntry LogEntry(int index)
    {
        return new LogEntry
        {
            TimestampUtc = MultiSessionTests.Now.AddTicks(index),
            Id = new SessionId { BeginString = FixBeginStrings.Fix44, SenderCompID = "LOCAL", TargetCompID = "REMOTE", Qualifier = "CONCURRENT" },
            Direction = LogDirection.Event,
            EventKind = SessionEventKind.MessageReceived,
            Text = "entry-" + index,
            Bytes = Encoding.ASCII.GetBytes("8=FIX.4.4\u000135=D\u000111=" + index + "\u0001")
        };
    }

    private static FixDictionary SharedDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "SHARED", BeginString = FixBeginStrings.Fix44, ApplicationVersion = FixApplicationVersions.Fix44 },
            Datatypes = [new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String }],
            Fields =
            [
                Field(FixTags.BeginString, "BeginString", FixFieldType.String),
                Field(FixTags.BodyLength, "BodyLength", FixFieldType.Length),
                Field(FixTags.MsgType, "MsgType", FixFieldType.String),
                Field(FixTags.CheckSum, "CheckSum", FixFieldType.String),
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", DatatypeName = "String", FieldType = FixFieldType.String }
            ],
            Header = [Member(FixTags.BeginString, 0), Member(FixTags.BodyLength, 1), Member(FixTags.MsgType, 2)],
            Trailer = [Member(FixTags.CheckSum, 0)],
            Messages = [new FixMessageDefinition { Id = 1, Name = "NewOrderSingle", MsgType = "D", Category = "Orders", Members = [Member(11, 0)] }]
        };
    }

    private static FixFieldDefinition Field(int tag, string name, FixFieldType type)
    {
        return new FixFieldDefinition { Tag = tag, Name = name, FieldType = type };
    }

    private static FixMemberDefinition Member(int tag, int position)
    {
        return new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = tag, Position = position, Presence = FixPresence.Optional };
    }

    private static string DictionaryText(FixDictionary dictionary)
    {
        return dictionary.Identity.Name + ":" + string.Join(",", dictionary.Fields.ConvertAll(item => item.Tag + "=" + item.Name)) + ":" + string.Join(",", dictionary.Messages.ConvertAll(item => item.MsgType + "=" + item.Name)) + ":" + dictionary.Diagnostics.Count;
    }

    private static byte[] BuildFrame(string bodyText)
    {
        byte[] body = Encoding.ASCII.GetBytes(bodyText);
        byte[] header = Encoding.ASCII.GetBytes("8=FIX.4.4\u00019=" + body.Length + "\u0001");
        byte[] withoutChecksum = Combine(header, body);
        int checksum = ChecksumCalculator.CalculateChecksum(withoutChecksum);
        return Combine(withoutChecksum, Encoding.ASCII.GetBytes("10=" + checksum.ToString("000") + "\u0001"));
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }
}
