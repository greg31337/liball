using System;
using System.Collections.Generic;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class ReplayTests
{
    internal static readonly DateTime OriginalTime = new DateTime(2026, 8, 30, 12, 0, 0, 123, DateTimeKind.Utc);
    internal static readonly DateTime ReplayTime = new DateTime(2026, 8, 30, 13, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ApplicationReplayPreservesOriginalTimeAndRewritesDuplicateHeaders()
    {
        ReplayFixture fixture = new ReplayFixture(2);
        byte[] originalBytes = fixture.StoreApplication(1, "ORDER-1", "20260830-12:00:00.123456");
        fixture.Application.MutateReplayText = "reviewed";

        ReplayProcessingResult result = fixture.Processor.ProcessReplayRange(1, 0);
        FixMessage replay = fixture.Decode(result.Messages[0].Bytes);
        StoreRangeResult stored = fixture.Store.ReadMessageRange(1, 1);
        StoreRangeResult sequenceState = fixture.Store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True, result.Code + ":" + result.Text);
            Assert.That(result.Messages, Has.Count.EqualTo(1));
            Assert.That(result.ReplayedApplicationCount, Is.EqualTo(1));
            Assert.That(result.GapFillCount, Is.Zero);
            Assert.That(result.Messages[0].IsGapFill, Is.False);
            Assert.That(Text(replay, FixTags.MsgSeqNum), Is.EqualTo("1"));
            Assert.That(Text(replay, FixTags.PossDupFlag), Is.EqualTo("Y"));
            Assert.That(Text(replay, FixTags.OrigSendingTime), Is.EqualTo("20260830-12:00:00.123456"));
            Assert.That(Text(replay, FixTags.SendingTime), Is.EqualTo("20260830-13:00:00.000"));
            Assert.That(Text(replay, FixTags.Text), Is.EqualTo("reviewed"));
            Assert.That(fixture.Application.Contexts, Has.Count.EqualTo(1));
            Assert.That(fixture.Application.Contexts[0].IsReplay, Is.True);
            Assert.That(fixture.Application.Contexts[0].IsPossibleDuplicate, Is.True);
            Assert.That(fixture.Application.Contexts[0].SequenceNumber, Is.EqualTo(1));
            Assert.That(fixture.Application.SendingTimesAtCallback[0], Is.EqualTo("20260830-12:00:00.123456"));
            Assert.That(fixture.Application.PossDupAtCallback[0], Is.EqualTo("Y"));
            Assert.That(stored.Messages[0].Bytes, Is.EqualTo(originalBytes));
            Assert.That(sequenceState.NextSenderSeqNum, Is.EqualTo(2));
            Assert.That(fixture.Sequences.NextSenderSeqNum, Is.EqualTo(2));
        });
    }

    [Test]
    public void AdministrativeAndUnavailableSequencesCoalesceIntoOneGapFill()
    {
        ReplayFixture fixture = new ReplayFixture(6);
        fixture.StoreAdministrative(2, FixAdminMessageTypes.Heartbeat);
        fixture.StoreAdministrative(4, FixAdminMessageTypes.Logout);
        fixture.StoreApplication(5, "ORDER-5", "20260830-12:00:05.000");

        ReplayProcessingResult result = fixture.Processor.ProcessReplayRange(2, 5);
        FixMessage gapFill = fixture.Decode(result.Messages[0].Bytes);
        FixMessage replay = fixture.Decode(result.Messages[1].Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Messages, Has.Count.EqualTo(2));
            Assert.That(result.Messages[0].IsGapFill, Is.True);
            Assert.That(result.Messages[0].BeginSeqNo, Is.EqualTo(2));
            Assert.That(result.Messages[0].EndSeqNo, Is.EqualTo(4));
            Assert.That(Text(gapFill, FixTags.MsgType), Is.EqualTo(FixAdminMessageTypes.SequenceReset));
            Assert.That(Text(gapFill, FixTags.MsgSeqNum), Is.EqualTo("2"));
            Assert.That(Text(gapFill, FixTags.PossDupFlag), Is.EqualTo("Y"));
            Assert.That(Text(gapFill, FixTags.OrigSendingTime), Is.EqualTo("20260830-13:00:00.000"));
            Assert.That(Text(gapFill, FixTags.SendingTime), Is.EqualTo("20260830-13:00:00.000"));
            Assert.That(Text(gapFill, FixTags.GapFillFlag), Is.EqualTo("Y"));
            Assert.That(Text(gapFill, FixTags.NewSeqNo), Is.EqualTo("5"));
            Assert.That(Text(replay, FixTags.MsgSeqNum), Is.EqualTo("5"));
            Assert.That(result.AdministrativeSequenceCount, Is.EqualTo(2));
            Assert.That(result.UnavailableSequenceCount, Is.EqualTo(1));
            Assert.That(result.UsedUnavailableGapFill, Is.True);
            Assert.That(result.GapFillCount, Is.EqualTo(1));
            Assert.That(fixture.Application.Contexts, Has.Count.EqualTo(1));
            Assert.That(fixture.Sequences.NextSenderSeqNum, Is.EqualTo(6));
        });
    }

    [Test]
    public void DroppedAndRejectedApplicationReplaysBecomeGapFillBeforeAcceptedReplay()
    {
        ReplayFixture fixture = new ReplayFixture(4);
        fixture.StoreApplication(1, "DROP", "20260830-12:00:01.000");
        fixture.StoreApplication(2, "REJECT", "20260830-12:00:02.000");
        fixture.StoreApplication(3, "ACCEPT", "20260830-12:00:03.000");
        fixture.Application.Decisions[1] = Decision(MessageDisposition.Drop, "drop");
        fixture.Application.Decisions[2] = Decision(MessageDisposition.Reject, "reject");

        ReplayProcessingResult result = fixture.Processor.ProcessReplayRange(1, 3);
        FixMessage gapFill = fixture.Decode(result.Messages[0].Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Messages, Has.Count.EqualTo(2));
            Assert.That(result.Messages[0].IsGapFill, Is.True);
            Assert.That(result.Messages[0].BeginSeqNo, Is.EqualTo(1));
            Assert.That(result.Messages[0].EndSeqNo, Is.EqualTo(2));
            Assert.That(Text(gapFill, FixTags.NewSeqNo), Is.EqualTo("3"));
            Assert.That(result.Messages[1].OriginalSequenceNumber, Is.EqualTo(3));
            Assert.That(result.SkippedApplicationCount, Is.EqualTo(2));
            Assert.That(result.ReplayedApplicationCount, Is.EqualTo(1));
            Assert.That(fixture.Application.Contexts, Has.Count.EqualTo(3));
            Assert.That(fixture.Sequences.NextSenderSeqNum, Is.EqualTo(4));
        });
    }

    [Test]
    public void ExistingOrigSendingTimeRemainsTheReplayOrigin()
    {
        ReplayFixture fixture = new ReplayFixture(2);
        FixMessage message = fixture.ApplicationMessage(1, "ORDER-1", "20260830-12:00:00.000");
        message.HeaderFields.Add(Field(FixTags.OrigSendingTime, "20260830-11:59:00.000"));
        byte[] bytes = fixture.Encode(message);
        fixture.Store.StoreMessage(Stored(1, "D", false, bytes));

        ReplayProcessingResult result = fixture.Processor.ProcessReplayRange(1, 1);
        FixMessage replay = fixture.Decode(result.Messages[0].Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(Text(replay, FixTags.OrigSendingTime), Is.EqualTo("20260830-11:59:00.000"));
            Assert.That(Text(replay, FixTags.SendingTime), Is.EqualTo("20260830-13:00:00.000"));
        });
    }

    [TestCase(MessageDisposition.Disconnect, "application-stop")]
    [TestCase((MessageDisposition)99, "invalid-replay-disposition")]
    public void TerminalApplicationReplayDecisionsReturnNoOutboundFrames(MessageDisposition disposition, string expectedCode)
    {
        ReplayFixture fixture = new ReplayFixture(2);
        fixture.StoreApplication(1, "ORDER-1", "20260830-12:00:00.000");
        fixture.Application.Decisions[1] = Decision(disposition, disposition == MessageDisposition.Disconnect ? expectedCode : "invalid");

        ReplayProcessingResult result = fixture.Processor.ProcessReplayRange(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Disconnect, Is.True);
            Assert.That(result.Code, Is.EqualTo(expectedCode));
            Assert.That(result.Messages, Is.Empty);
            Assert.That(fixture.Sequences.NextSenderSeqNum, Is.EqualTo(2));
        });
    }

    [Test]
    public void MissingApplicationDecisionIsTerminal()
    {
        ReplayFixture fixture = new ReplayFixture(2);
        fixture.StoreApplication(1, "ORDER-1", "20260830-12:00:00.000");
        fixture.Application.ReturnMissingDecision = true;

        ReplayProcessingResult result = fixture.Processor.ProcessReplayRange(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Disconnect, Is.True);
            Assert.That(result.Code, Is.EqualTo("missing-replay-decision"));
            Assert.That(result.Messages, Is.Empty);
        });
    }

    [Test]
    public void CorruptStoreAndInvalidStoredApplicationHaveExplicitTerminalPolicies()
    {
        ReplayFixture corruptFixture = new ReplayFixture(2, new CorruptReplayStore());
        ReplayProcessingResult corrupt = corruptFixture.Processor.ProcessReplayRange(1, 1);
        ReplayFixture invalidFixture = new ReplayFixture(2);
        invalidFixture.Store.StoreMessage(Stored(1, "D", false, [1, 2, 3]));
        ReplayProcessingResult invalid = invalidFixture.Processor.ProcessReplayRange(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(corrupt.Accepted, Is.False);
            Assert.That(corrupt.Disconnect, Is.True);
            Assert.That(corrupt.StoreCorrupt, Is.True);
            Assert.That(corrupt.Code, Is.EqualTo("corrupt-replay-store-range"));
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Disconnect, Is.True);
            Assert.That(invalid.StoreCorrupt, Is.True);
            Assert.That(invalid.Messages, Is.Empty);
            Assert.That(invalidFixture.Application.Contexts, Is.Empty);
        });
    }

    [Test]
    public void OpenAndExplicitRangesCapAtLastSentAndFutureRangeIsEmpty()
    {
        ReplayFixture fixture = new ReplayFixture(4);
        fixture.StoreApplication(2, "ORDER-2", "20260830-12:00:02.000");
        fixture.StoreApplication(3, "ORDER-3", "20260830-12:00:03.000");

        ReplayProcessingResult capped = fixture.Processor.ProcessReplayRange(2, 99);
        ReplayProcessingResult open = fixture.Processor.ProcessReplayRange(3, 0);
        ReplayProcessingResult empty = fixture.Processor.ProcessReplayRange(4, 0);
        ReplayProcessingResult invalid = fixture.Processor.ProcessReplayRange(3, 2);

        Assert.Multiple(() =>
        {
            Assert.That(capped.Accepted, Is.True);
            Assert.That(capped.EndSeqNo, Is.EqualTo(3));
            Assert.That(capped.Messages, Has.Count.EqualTo(2));
            Assert.That(open.Accepted, Is.True);
            Assert.That(open.EndSeqNo, Is.EqualTo(3));
            Assert.That(open.Messages, Has.Count.EqualTo(1));
            Assert.That(empty.Accepted, Is.True);
            Assert.That(empty.Code, Is.EqualTo("replay-range-empty"));
            Assert.That(empty.Messages, Is.Empty);
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Code, Is.EqualTo("invalid-replay-range"));
            Assert.That(fixture.Sequences.NextSenderSeqNum, Is.EqualTo(4));
        });
    }

    [Test]
    public void ConstructorInitializationAndClockContractsAreEnforced()
    {
        ReplayFixture fixture = new ReplayFixture(2);
        SessionSequenceProcessor uninitialized = new SessionSequenceProcessor(fixture.Store);
        ReplayProcessor notReady = new ReplayProcessor(fixture.Store, uninitialized, fixture.Builder, fixture.Application);
        ReplayProcessingResult result = notReady.ProcessReplayRange(1, 1);
        ReplayBuilder localClock = new ReplayBuilder(fixture.Id, fixture.Dictionary, new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixMessageOperations(), new FakeSessionClock(DateTime.SpecifyKind(ReplayTime, DateTimeKind.Local)));

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => new ReplayBuilder(null!, fixture.Dictionary, new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixMessageOperations(), fixture.Clock));
            Assert.Throws<FixSessionException>(() => new ReplayProcessor(null!, fixture.Sequences, fixture.Builder, fixture.Application));
            Assert.Throws<FixSessionException>(() => localClock.CreateGapFillReplay(1, 1));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Disconnect, Is.True);
            Assert.That(result.Code, Is.EqualTo("sequence-state-not-initialized"));
        });
    }

    private static ApplicationDecision Decision(MessageDisposition disposition, string code)
    {
        return new ApplicationDecision { Disposition = disposition, Code = code, Text = code };
    }

    private static StoredMessage Stored(int sequenceNumber, string messageType, bool administrative, byte[] bytes)
    {
        return new StoredMessage
        {
            SequenceNumber = sequenceNumber,
            SendingTimeUtc = OriginalTime.AddSeconds(sequenceNumber),
            MsgType = messageType,
            IsAdministrative = administrative,
            Bytes = bytes
        };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    private static string Text(FixMessage message, int tag)
    {
        FixField? field = EnvelopeValidator.FindFirstMessageField(message, tag);
        return field == null ? "" : Encoding.Latin1.GetString(field.Value);
    }

    private class ReplayFixture
    {
        internal SessionId Id = new SessionId { BeginString = FixBeginStrings.Fix44, SenderCompID = "SENDER", TargetCompID = "TARGET", Qualifier = "PRIMARY" };
        internal FixDictionary Dictionary = CreateDictionary();
        internal IMessageStore Store;
        internal SessionSequenceProcessor Sequences;
        internal FakeSessionClock Clock = new FakeSessionClock(ReplayTime);
        internal ReplayApplication Application = new ReplayApplication();
        internal ReplayBuilder Builder;
        internal ReplayProcessor Processor;

        internal ReplayFixture(int nextSenderSeqNum, IMessageStore? store = null)
        {
            Store = store ?? new MemoryMessageStore(0);
            StoreRangeResult saved = Store.SaveSequenceState(nextSenderSeqNum, 1);
            Assert.That(saved.Success, Is.True);
            Sequences = new SessionSequenceProcessor(Store);
            Assert.That(Sequences.RefreshSequences().Accepted, Is.True);
            Builder = new ReplayBuilder(Id, Dictionary, new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixMessageOperations(), Clock);
            Processor = new ReplayProcessor(Store, Sequences, Builder, Application);
        }

        internal byte[] StoreApplication(int sequenceNumber, string orderId, string sendingTime)
        {
            byte[] bytes = Encode(ApplicationMessage(sequenceNumber, orderId, sendingTime));
            StoreRangeResult stored = Store.StoreMessage(Stored(sequenceNumber, "D", false, bytes));
            Assert.That(stored.Success, Is.True);
            return bytes;
        }

        internal void StoreAdministrative(int sequenceNumber, string messageType)
        {
            StoreRangeResult stored = Store.StoreMessage(Stored(sequenceNumber, messageType, true, [1]));
            Assert.That(stored.Success, Is.True);
        }

        internal FixMessage ApplicationMessage(int sequenceNumber, string orderId, string sendingTime)
        {
            FixMessage message = new FixMessage { BeginString = FixBeginStrings.Fix44, MsgType = "D" };
            message.HeaderFields.Add(Field(FixTags.MsgSeqNum, sequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            message.HeaderFields.Add(Field(FixTags.SenderCompID, Id.SenderCompID));
            message.HeaderFields.Add(Field(FixTags.TargetCompID, Id.TargetCompID));
            message.HeaderFields.Add(Field(FixTags.SendingTime, sendingTime));
            message.BodyFields.Add(Field(11, orderId));
            return message;
        }

        internal byte[] Encode(FixMessage message)
        {
            TagValueMessageEncoder encoder = new TagValueMessageEncoder();
            EncodeResult measured = encoder.EncodeMessage(message, Dictionary, Span<byte>.Empty, new EncodeOptions());
            Assert.That(measured.RequiredSize, Is.GreaterThan(0), measured.ErrorCode);
            byte[] bytes = new byte[measured.RequiredSize];
            EncodeResult encoded = encoder.EncodeMessage(message, Dictionary, bytes, new EncodeOptions());
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success), encoded.ErrorCode);
            return bytes;
        }

        internal FixMessage Decode(byte[] bytes)
        {
            DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(bytes, Dictionary, new DecodeOptions());
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            return decoded.Message!;
        }

        internal static FixDictionary CreateDictionary()
        {
            FixDictionary dictionary = new FixDictionary();
            dictionary.Identity.BeginString = FixBeginStrings.Fix44;
            dictionary.Fields =
            [
                Definition(FixTags.BeginString, FixFieldType.String),
                Definition(FixTags.BodyLength, FixFieldType.Length),
                Definition(FixTags.CheckSum, FixFieldType.String),
                Definition(FixTags.MsgSeqNum, FixFieldType.SeqNum),
                Definition(FixTags.MsgType, FixFieldType.String),
                Definition(FixTags.PossDupFlag, FixFieldType.Boolean),
                Definition(FixTags.SenderCompID, FixFieldType.String),
                Definition(FixTags.SendingTime, FixFieldType.UtcTimestamp),
                Definition(FixTags.TargetCompID, FixFieldType.String),
                Definition(FixTags.OrigSendingTime, FixFieldType.UtcTimestamp),
                Definition(FixTags.GapFillFlag, FixFieldType.Boolean),
                Definition(FixTags.NewSeqNo, FixFieldType.SeqNum),
                Definition(11, FixFieldType.String),
                Definition(FixTags.Text, FixFieldType.String)
            ];
            dictionary.Header =
            [
                Member(FixTags.MsgSeqNum),
                Member(FixTags.SenderCompID),
                Member(FixTags.TargetCompID),
                Member(FixTags.SendingTime),
                Member(FixTags.PossDupFlag),
                Member(FixTags.OrigSendingTime)
            ];
            dictionary.Trailer = [Member(FixTags.CheckSum)];
            dictionary.Messages =
            [
                new FixMessageDefinition { Id = 1, MsgType = "D", Members = [Member(11), Member(FixTags.Text)] },
                new FixMessageDefinition { Id = 2, MsgType = FixAdminMessageTypes.SequenceReset, Members = [Member(FixTags.GapFillFlag), Member(FixTags.NewSeqNo)] }
            ];
            return dictionary;
        }

        internal static FixFieldDefinition Definition(int tag, FixFieldType type)
        {
            return new FixFieldDefinition { Tag = tag, FieldType = type };
        }

        internal static FixMemberDefinition Member(int tag)
        {
            return new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = tag };
        }
    }

    private class ReplayApplication : ISessionApplication
    {
        internal Dictionary<int, ApplicationDecision> Decisions = new Dictionary<int, ApplicationDecision>();
        internal List<ApplicationMessageContext> Contexts = [];
        internal List<string> SendingTimesAtCallback = [];
        internal List<string> PossDupAtCallback = [];
        internal bool ReturnMissingDecision;
        internal string MutateReplayText = "";

        public ApplicationDecision OnSessionCreated(SessionId id)
        {
            return Decision(MessageDisposition.Accept, "created");
        }

        public ApplicationDecision OnSessionLogon(ApplicationMessageContext context)
        {
            return Decision(MessageDisposition.Accept, "logon");
        }

        public ApplicationDecision OnSessionLogout(ApplicationMessageContext context)
        {
            return Decision(MessageDisposition.Accept, "logout");
        }

        public ApplicationDecision OnAdministrativeMessage(ApplicationMessageContext context)
        {
            return Decision(MessageDisposition.Accept, "admin");
        }

        public ApplicationDecision OnApplicationMessage(ApplicationMessageContext context)
        {
            return Decision(MessageDisposition.Accept, "application");
        }

        public ApplicationDecision OnReplayMessage(ApplicationMessageContext context)
        {
            Contexts.Add(context);
            SendingTimesAtCallback.Add(Text(context.Message, FixTags.SendingTime));
            PossDupAtCallback.Add(Text(context.Message, FixTags.PossDupFlag));
            if (MutateReplayText.Length != 0)
            {
                FixField? text = EnvelopeValidator.FindFirstMessageField(context.Message, FixTags.Text);
                if (text == null)
                {
                    context.Message.BodyFields.Add(Field(FixTags.Text, MutateReplayText));
                }
                else
                {
                    text.Value = Encoding.Latin1.GetBytes(MutateReplayText);
                }
            }
            if (ReturnMissingDecision)
            {
                return null!;
            }
            if (Decisions.ContainsKey(context.SequenceNumber))
            {
                return Decisions[context.SequenceNumber];
            }
            return Decision(MessageDisposition.Accept, "accept");
        }
    }

    private class CorruptReplayStore : IMessageStore
    {
        internal int Sender = 1;
        internal int Target = 1;

        public StoreRangeResult StoreMessage(StoredMessage message)
        {
            return State();
        }

        public StoreRangeResult ReadMessageRange(int beginSeqNo, int endSeqNo)
        {
            StoreRangeResult result = State();
            result.IsCorrupt = true;
            return result;
        }

        public StoreRangeResult SaveSequenceState(int nextSenderSeqNum, int nextTargetSeqNum)
        {
            Sender = nextSenderSeqNum;
            Target = nextTargetSeqNum;
            return State();
        }

        public StoreRangeResult RefreshStore()
        {
            return State();
        }

        public StoreRangeResult ResetStore()
        {
            Sender = 1;
            Target = 1;
            return State();
        }

        private StoreRangeResult State()
        {
            return new StoreRangeResult { Success = true, HasSequenceState = true, NextSenderSeqNum = Sender, NextTargetSeqNum = Target };
        }
    }
}
