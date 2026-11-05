namespace pop3.Tests;

public class Pop3MailboxTransactionTests
{
    [Test]
    public void OpenCreatesADefensiveStableSnapshot()
    {
        MailboxSession session = ValidSession();
        Pop3MailboxTransaction transaction = OpenTransaction(session);

        session.Status!.MessageCount = 99;
        session.Status.TotalOctets = 99;
        session.Messages![0].MessageNumber = 99;
        session.Messages[0].OctetCount = 99;
        session.Messages[0].IsDeleted = true;
        session.UniqueIds![0].Value = "changed";
        Pop3MessageInfo[] firstMessages = transaction.GetMessages();
        Pop3UniqueId[] firstUniqueIds = transaction.GetUniqueIds();
        firstMessages[1].MessageNumber = 99;
        firstMessages[1].OctetCount = 99;
        firstMessages[1].IsDeleted = true;
        firstUniqueIds[1].Value = "changed-again";

        Pop3MailboxStatus status = transaction.GetStatus();
        Pop3MessageInfo[] messages = transaction.GetMessages();
        Pop3UniqueId[] uniqueIds = transaction.GetUniqueIds();

        Assert.That(session.Events,
            Is.EqualTo(new[] { "status", "messages", "unique-ids" }));
        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets, Is.EqualTo(30));
        Assert.That(messages.Length, Is.EqualTo(2));
        Assert.That(messages[0].MessageNumber, Is.EqualTo(1));
        Assert.That(messages[0].OctetCount, Is.EqualTo(10));
        Assert.That(messages[0].IsDeleted, Is.False);
        Assert.That(messages[0].IsUtf8Representation, Is.False);
        Assert.That(messages[1].MessageNumber, Is.EqualTo(2));
        Assert.That(messages[1].OctetCount, Is.EqualTo(20));
        Assert.That(messages[1].IsDeleted, Is.False);
        Assert.That(messages[1].IsUtf8Representation, Is.True);
        Assert.That(uniqueIds.Length, Is.EqualTo(2));
        Assert.That(uniqueIds[0].MessageNumber, Is.EqualTo(1));
        Assert.That(uniqueIds[0].Value, Is.EqualTo("uid-1"));
        Assert.That(uniqueIds[1].MessageNumber, Is.EqualTo(2));
        Assert.That(uniqueIds[1].Value, Is.EqualTo("UID-2"));
        Assert.That(transaction.HasSession(), Is.True);
        Assert.That(session.ReleaseCount, Is.Zero);
    }

    [Test]
    public void EmptySnapshotHasZeroVisibleStatus()
    {
        MailboxSession session = new MailboxSession();
        session.Status = Status(0, 0);
        session.Messages = Array.Empty<Pop3MessageInfo>();
        session.UniqueIds = Array.Empty<Pop3UniqueId>();

        Pop3MailboxTransaction transaction = OpenTransaction(session);
        Pop3MailboxStatus status = transaction.GetStatus();

        Assert.That(status.MessageCount, Is.Zero);
        Assert.That(status.TotalOctets, Is.Zero);
        Assert.That(transaction.GetMessages(), Is.Empty);
        Assert.That(transaction.GetUniqueIds(), Is.Empty);
    }

    [Test]
    public void DeletionMarksPreserveNumbersAndRecomputeVisibleStatus()
    {
        MailboxSession session = Session(
            new long[] { 10, 20, 30 },
            new string[] { "one", "two", "three" });
        Pop3MailboxTransaction transaction = OpenTransaction(session);

        transaction.MarkDeleted(2);
        Pop3MailboxStatus afterSecond = transaction.GetStatus();
        transaction.MarkDeleted(1);
        Pop3MailboxStatus afterFirst = transaction.GetStatus();
        Pop3MessageInfo[] markedMessages = transaction.GetMessages();

        Assert.That(afterSecond.MessageCount, Is.EqualTo(2));
        Assert.That(afterSecond.TotalOctets, Is.EqualTo(40));
        Assert.That(afterFirst.MessageCount, Is.EqualTo(1));
        Assert.That(afterFirst.TotalOctets, Is.EqualTo(30));
        Assert.That(markedMessages.Select(item => item.MessageNumber),
            Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(markedMessages.Select(item => item.IsDeleted),
            Is.EqualTo(new[] { true, true, false }));
        Assert.That(transaction.GetUniqueIds().Select(item => item.Value),
            Is.EqualTo(new[] { "one", "two", "three" }));
        Assert.That(session.MarkedNumbers, Is.EqualTo(new[] { 2, 1 }));
        Assert.Throws<InvalidOperationException>(() =>
            transaction.MarkDeleted(2));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            transaction.MarkDeleted(0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            transaction.MarkDeleted(4));
        Assert.That(session.MarkedNumbers, Is.EqualTo(new[] { 2, 1 }));

        Pop3MailboxStatus restored = transaction.ResetDeletions();
        Pop3MessageInfo[] resetMessages = transaction.GetMessages();

        Assert.That(restored.MessageCount, Is.EqualTo(3));
        Assert.That(restored.TotalOctets, Is.EqualTo(60));
        Assert.That(transaction.GetStatus().MessageCount, Is.EqualTo(3));
        Assert.That(transaction.GetStatus().TotalOctets, Is.EqualTo(60));
        Assert.That(resetMessages.Select(item => item.IsDeleted),
            Is.EqualTo(new[] { false, false, false }));
        Assert.That(session.ResetCount, Is.EqualTo(1));
    }

    [Test]
    public void ProviderMarkFailureDoesNotChangeTheLocalSnapshot()
    {
        MailboxSession session = ValidSession();
        session.ThrowOnMark = true;
        Pop3MailboxTransaction transaction = OpenTransaction(session);

        Assert.Throws<InvalidOperationException>(() =>
            transaction.MarkDeleted(1));

        Assert.That(transaction.GetMessages()[0].IsDeleted, Is.False);
        Assert.That(transaction.GetStatus().MessageCount, Is.EqualTo(2));
        Assert.That(transaction.GetStatus().TotalOctets, Is.EqualTo(30));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void InvalidResetStatusDoesNotClearLocalDeletionMarks(int scenario)
    {
        MailboxSession session = ValidSession();
        Pop3MailboxTransaction transaction = OpenTransaction(session);
        transaction.MarkDeleted(1);
        if (scenario == 0)
        {
            session.ResetStatus = null;
        }
        else if (scenario == 1)
        {
            session.ResetStatus = Status(1, 30);
        }
        else
        {
            session.ResetStatus = Status(2, 29);
        }

        Assert.Throws<InvalidDataException>(() =>
            transaction.ResetDeletions());

        Assert.That(transaction.GetMessages()[0].IsDeleted, Is.True);
        Assert.That(transaction.GetStatus().MessageCount, Is.EqualTo(1));
        Assert.That(transaction.GetStatus().TotalOctets, Is.EqualTo(20));
    }

    [Test]
    public void LongMaximumTotalRemainsValidAndRecomputesAfterDeletion()
    {
        MailboxSession session = Session(
            new long[] { long.MaxValue - 1, 1 },
            new string[] { "one", "two" });
        Pop3MailboxTransaction transaction = OpenTransaction(session);

        Assert.That(transaction.GetStatus().TotalOctets,
            Is.EqualTo(long.MaxValue));

        transaction.MarkDeleted(1);

        Assert.That(transaction.GetStatus().MessageCount, Is.EqualTo(1));
        Assert.That(transaction.GetStatus().TotalOctets, Is.EqualTo(1));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    [TestCase(16)]
    [TestCase(17)]
    [TestCase(18)]
    [TestCase(19)]
    [TestCase(20)]
    [TestCase(21)]
    public void InvalidSnapshotsReleaseTheirAcquiredSession(int scenario)
    {
        MailboxSession session = ValidSession();
        MakeSnapshotInvalid(session, scenario);
        Pop3MailboxTransaction transaction = new Pop3MailboxTransaction();

        Assert.Throws<InvalidDataException>(() => Open(transaction, session));

        Assert.That(transaction.HasSession(), Is.False);
        Assert.That(session.ReleaseCount, Is.EqualTo(1));
    }

    [Test]
    public void SnapshotOperationsRequireAnActiveSession()
    {
        Pop3MailboxTransaction transaction = new Pop3MailboxTransaction();

        Assert.Throws<Pop3StateException>(() => transaction.GetStatus());
        Assert.Throws<Pop3StateException>(() => transaction.GetMessages());
        Assert.Throws<Pop3StateException>(() => transaction.GetUniqueIds());
        Assert.Throws<Pop3StateException>(() => transaction.MarkDeleted(1));
        Assert.Throws<Pop3StateException>(() => transaction.ResetDeletions());
    }

    [Test]
    public void OpeningAMailboxValidatesAuthenticationAndConnectionInputs()
    {
        Pop3MailboxTransaction transaction = new Pop3MailboxTransaction();
        Pop3AuthenticationResult authentication = new Pop3AuthenticationResult();
        Pop3ServerConnection connection = new Pop3ServerConnection();

        Assert.Throws<ArgumentNullException>(() => transaction.OpenMailbox(
            null!, null, connection, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => transaction.OpenMailbox(
            authentication, null, null!, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => transaction.OpenMailbox(
            authentication, null, connection, CancellationToken.None));

        authentication.Status = Pop3AuthenticationStatus.Success;
        authentication.AuthenticatedIdentity = null;
        authentication.MaildropKey = "mail";
        Assert.Throws<ArgumentException>(() => transaction.OpenMailbox(
            authentication, null, connection, CancellationToken.None));
        authentication.AuthenticatedIdentity = "alice";
        authentication.MaildropKey = null;
        Assert.Throws<ArgumentException>(() => transaction.OpenMailbox(
            authentication, null, connection, CancellationToken.None));
        authentication.MaildropKey = "mail";

        Pop3MailboxOpenResult result = transaction.OpenMailbox(
            authentication, null, connection, CancellationToken.None);

        Assert.That(result.Status,
            Is.EqualTo(Pop3AuthenticationStatus.PermanentFailure));
        Assert.That(transaction.HasSession(), Is.False);
    }

    static void MakeSnapshotInvalid(MailboxSession session, int scenario)
    {
        if (scenario == 0)
        {
            session.Status = null;
        }
        else if (scenario == 1)
        {
            session.Status!.MessageCount = -1;
        }
        else if (scenario == 2)
        {
            session.Status!.TotalOctets = -1;
        }
        else if (scenario == 3)
        {
            session.Status!.MessageCount = 1;
        }
        else if (scenario == 4)
        {
            session.Status!.TotalOctets = 29;
        }
        else if (scenario == 5)
        {
            session.Messages = null;
        }
        else if (scenario == 6)
        {
            session.Messages![0] = null!;
        }
        else if (scenario == 7)
        {
            session.Messages![0].MessageNumber = 0;
        }
        else if (scenario == 8)
        {
            session.Messages![1].MessageNumber = 1;
        }
        else if (scenario == 9)
        {
            session.Messages![0].OctetCount = -1;
        }
        else if (scenario == 10)
        {
            session.Messages![0].IsDeleted = true;
        }
        else if (scenario == 11)
        {
            session.Messages![0].OctetCount = long.MaxValue;
            session.Messages[1].OctetCount = 1;
            session.Status!.TotalOctets = long.MaxValue;
        }
        else if (scenario == 12)
        {
            session.UniqueIds = null;
        }
        else if (scenario == 13)
        {
            session.UniqueIds = new Pop3UniqueId[] { UniqueId(1, "only") };
        }
        else if (scenario == 14)
        {
            session.UniqueIds![0] = null!;
        }
        else if (scenario == 15)
        {
            session.UniqueIds![0].MessageNumber = 0;
        }
        else if (scenario == 16)
        {
            session.UniqueIds![0].Value = null!;
        }
        else if (scenario == 17)
        {
            session.UniqueIds![0].Value = "";
        }
        else if (scenario == 18)
        {
            session.UniqueIds![0].Value = new string('a', 71);
        }
        else if (scenario == 19)
        {
            session.UniqueIds![0].Value = "has space";
        }
        else if (scenario == 20)
        {
            session.UniqueIds![0].Value = "café";
        }
        else
        {
            session.UniqueIds![1].Value = "uid-1";
        }
    }

    static Pop3MailboxTransaction OpenTransaction(MailboxSession session)
    {
        Pop3MailboxTransaction transaction = new Pop3MailboxTransaction();
        Open(transaction, session);
        return transaction;
    }

    static void Open(
        Pop3MailboxTransaction transaction,
        MailboxSession session)
    {
        MailboxProvider provider = new MailboxProvider(session);
        Pop3AuthenticationResult authentication = new Pop3AuthenticationResult();
        authentication.Status = Pop3AuthenticationStatus.Success;
        authentication.AuthenticatedIdentity = "alice";
        authentication.MaildropKey = "mail";
        transaction.OpenMailbox(
            authentication,
            provider,
            new Pop3ServerConnection(),
            CancellationToken.None);
    }

    static MailboxSession ValidSession()
    {
        MailboxSession session = Session(
            new long[] { 10, 20 },
            new string[] { "uid-1", "UID-2" });
        session.Messages![1].IsUtf8Representation = true;
        return session;
    }

    static MailboxSession Session(long[] sizes, string[] uniqueIdValues)
    {
        MailboxSession session = new MailboxSession();
        session.Messages = new Pop3MessageInfo[sizes.Length];
        session.UniqueIds = new Pop3UniqueId[uniqueIdValues.Length];
        long totalOctets = 0;
        int index = 0;
        while (index < sizes.Length)
        {
            Pop3MessageInfo message = new Pop3MessageInfo();
            message.MessageNumber = index + 1;
            message.OctetCount = sizes[index];
            session.Messages[index] = message;
            totalOctets += sizes[index];
            index++;
        }
        index = 0;
        while (index < uniqueIdValues.Length)
        {
            session.UniqueIds[index] = UniqueId(index + 1, uniqueIdValues[index]);
            index++;
        }
        session.Status = Status(sizes.Length, totalOctets);
        session.ResetStatus = Status(sizes.Length, totalOctets);
        return session;
    }

    static Pop3MailboxStatus Status(int count, long totalOctets)
    {
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        status.MessageCount = count;
        status.TotalOctets = totalOctets;
        return status;
    }

    static Pop3UniqueId UniqueId(int messageNumber, string value)
    {
        Pop3UniqueId uniqueId = new Pop3UniqueId();
        uniqueId.MessageNumber = messageNumber;
        uniqueId.Value = value;
        return uniqueId;
    }

    sealed class MailboxProvider : IPop3MailboxProvider
    {
        MailboxSession Session;

        internal MailboxProvider(MailboxSession session)
        {
            Session = session;
        }

        public Pop3MailboxOpenResult OpenMailbox(
            Pop3MailboxOpenRequest request,
            CancellationToken cancellationToken)
        {
            Pop3MailboxOpenResult result = new Pop3MailboxOpenResult();
            result.Status = Pop3AuthenticationStatus.Success;
            result.Session = Session;
            return result;
        }
    }

    sealed class MailboxSession : IPop3MailboxSession
    {
        internal Pop3MailboxStatus? Status;
        internal Pop3MessageInfo[]? Messages;
        internal Pop3UniqueId[]? UniqueIds;
        internal Pop3MailboxStatus? ResetStatus;
        internal List<string> Events = new List<string>();
        internal List<int> MarkedNumbers = new List<int>();
        internal int ResetCount;
        internal int ReleaseCount;
        internal bool ThrowOnMark;

        public Pop3MailboxStatus GetStatus()
        {
            Events.Add("status");
            return Status!;
        }

        public Pop3MessageInfo[] GetMessages()
        {
            Events.Add("messages");
            return Messages!;
        }

        public Pop3UniqueId[] GetUniqueIds()
        {
            Events.Add("unique-ids");
            return UniqueIds!;
        }

        public void WriteMessage(
            int messageNumber,
            bool useUtf8Representation,
            Stream destination,
            CancellationToken cancellationToken)
        {
        }

        public void WriteTop(
            int messageNumber,
            int bodyLineCount,
            bool useUtf8Representation,
            Stream destination,
            CancellationToken cancellationToken)
        {
        }

        public void MarkDeleted(int messageNumber)
        {
            if (ThrowOnMark)
            {
                throw new InvalidOperationException("mark failed");
            }
            MarkedNumbers.Add(messageNumber);
        }

        public Pop3MailboxStatus ResetDeletions()
        {
            ResetCount++;
            return ResetStatus!;
        }

        public void CommitUpdate(CancellationToken cancellationToken)
        {
        }

        public void RollbackChanges()
        {
        }

        public void ReleaseSession()
        {
            ReleaseCount++;
        }
    }
}
