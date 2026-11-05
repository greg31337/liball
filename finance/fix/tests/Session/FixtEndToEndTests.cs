using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class FixtEndToEndTests
{
    [Test]
    public void NegotiatedDefaultAndBidirectionalApplVerOverridesUseIndependentDictionaries()
    {
        FixtPair pair = new FixtPair();
        string acceptorConfiguredDictionary = pair.Acceptor.Core.ApplicationDictionary.Identity.Name;

        pair.StartAndActivate();
        pair.InitiatorProvider.Clear();
        pair.AcceptorProvider.Clear();

        AssertAccepted(pair.Initiator.SendMessage(Order("DEFAULT-SP2", "")), pair.InitiatorValidator);
        pair.PumpUntil(() => pair.AcceptorApplication.ApplicationContexts.Count == 1);
        Assert.That(pair.InitiatorProvider.ResolvedNames, Is.Empty);
        Assert.That(pair.AcceptorProvider.ResolvedNames, Is.Empty);

        AssertAccepted(pair.Initiator.SendMessage(Order("OVERRIDE-FIX50", FixApplicationVersions.Fix50)), pair.InitiatorValidator);
        pair.PumpUntil(() => pair.AcceptorApplication.ApplicationContexts.Count == 2);
        AssertAccepted(pair.Acceptor.SendMessage(Order("OVERRIDE-SP1", FixApplicationVersions.Fix50Sp1)), pair.AcceptorValidator);
        pair.PumpUntil(() => pair.InitiatorApplication.ApplicationContexts.Count == 1);

        SessionPollResult logout = pair.Initiator.LogoutSession("fixt-routing-complete");
        pair.InitiatorEvents.AddRange(logout.Events);
        pair.PumpUntil(() => pair.Initiator.GetSessionSnapshot().State == SessionState.Disconnected && pair.Acceptor.GetSessionSnapshot().State == SessionState.Disconnected);

        Assert.Multiple(() =>
        {
            Assert.That(acceptorConfiguredDictionary, Is.EqualTo("FIX50"));
            Assert.That(pair.Initiator.Core.Negotiation.DefaultApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(pair.Acceptor.Core.Negotiation.DefaultApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(pair.Initiator.Core.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(pair.Acceptor.Core.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(pair.Initiator.Core.ApplicationDictionary.Identity.Name, Is.EqualTo("FIX50SP2"));
            Assert.That(pair.Acceptor.Core.ApplicationDictionary.Identity.Name, Is.EqualTo("FIX50SP2"));
            Assert.That(pair.AcceptorApplication.ApplicationContexts.Select(item => Text(item.Message, 11)), Is.EqualTo(new[] { "DEFAULT-SP2", "OVERRIDE-FIX50" }));
            Assert.That(pair.InitiatorApplication.ApplicationContexts.Select(item => Text(item.Message, 11)), Is.EqualTo(new[] { "OVERRIDE-SP1" }));
            Assert.That(Text(pair.AcceptorApplication.ApplicationContexts[0].Message, FixTags.ApplVerID), Is.Empty);
            Assert.That(Text(pair.AcceptorApplication.ApplicationContexts[1].Message, FixTags.ApplVerID), Is.EqualTo(FixApplicationVersions.Fix50));
            Assert.That(Text(pair.InitiatorApplication.ApplicationContexts[0].Message, FixTags.ApplVerID), Is.EqualTo(FixApplicationVersions.Fix50Sp1));
            Assert.That(pair.InitiatorProvider.ResolvedVersions, Is.EqualTo(new[] { FixApplicationVersions.Fix50, FixApplicationVersions.Fix50Sp1 }));
            Assert.That(pair.AcceptorProvider.ResolvedVersions, Is.EqualTo(new[] { FixApplicationVersions.Fix50, FixApplicationVersions.Fix50Sp1 }));
            Assert.That(pair.InitiatorProvider.ResolvedNames, Is.EqualTo(new[] { "FIX50", "FIX50SP1" }));
            Assert.That(pair.AcceptorProvider.ResolvedNames, Is.EqualTo(new[] { "FIX50", "FIX50SP1" }));
        });
    }

    private static FixMessage Order(string id, string applicationVersion)
    {
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            BodyFields =
            [
                FixSessionTests.Field(11, id),
                FixSessionTests.Field(55, "IBM"),
                FixSessionTests.Field(54, "1"),
                FixSessionTests.Field(60, "20260830-18:00:00.000"),
                FixSessionTests.Field(40, "1")
            ]
        };
        if (applicationVersion.Length != 0) message.HeaderFields.Add(FixSessionTests.Field(FixTags.ApplVerID, applicationVersion));
        return message;
    }

    private static string Text(FixMessage message, int tag)
    {
        FixField? field = EnvelopeValidator.FindFirstMessageField(message, tag);
        return field == null ? "" : Encoding.Latin1.GetString(field.Value);
    }

    private static void AssertAccepted(SessionSendResult result, RecordingValidator validator)
    {
        Assert.That(result.Accepted, Is.True, result.Code + ": " + result.Text + ": " + validator.IssueText());
    }

    private class FixtPair
    {
        internal InMemoryDuplexTransportPair Transport = new InMemoryDuplexTransportPair();
        internal FakeSessionClock Clock = new FakeSessionClock(FixSessionTests.Now);
        internal RecordingDictionaryProvider InitiatorProvider = new RecordingDictionaryProvider();
        internal RecordingDictionaryProvider AcceptorProvider = new RecordingDictionaryProvider();
        internal RecordingValidator InitiatorValidator = new RecordingValidator();
        internal RecordingValidator AcceptorValidator = new RecordingValidator();
        internal FixSessionTests.RecordingSessionApplication InitiatorApplication = new FixSessionTests.RecordingSessionApplication();
        internal FixSessionTests.RecordingSessionApplication AcceptorApplication = new FixSessionTests.RecordingSessionApplication();
        internal List<SessionEvent> InitiatorEvents = [];
        internal List<SessionEvent> AcceptorEvents = [];
        internal FixSession Initiator;
        internal FixSession Acceptor;

        internal FixtPair()
        {
            SessionSettings initiatorSettings = Settings(SessionRole.Initiator, "INITIATOR", "ACCEPTOR", "FIX50SP2", FixApplicationVersions.Fix50Sp2);
            SessionSettings acceptorSettings = Settings(SessionRole.Acceptor, "ACCEPTOR", "INITIATOR", "FIX50", FixApplicationVersions.Fix50);
            Initiator = CreateSession(initiatorSettings, Transport.First, InitiatorApplication, InitiatorProvider, InitiatorValidator);
            Acceptor = CreateSession(acceptorSettings, Transport.Second, AcceptorApplication, AcceptorProvider, AcceptorValidator);
        }

        internal void StartAndActivate()
        {
            AcceptorEvents.AddRange(Acceptor.StartSession().Events);
            InitiatorEvents.AddRange(Initiator.StartSession().Events);
            PumpUntil(() => Initiator.GetSessionSnapshot().State == SessionState.Active && Acceptor.GetSessionSnapshot().State == SessionState.Active);
        }

        internal void PumpUntil(Func<bool> completed)
        {
            int polls = 0;
            while (!completed() && polls < 500)
            {
                InitiatorEvents.AddRange(Initiator.PollSession().Events);
                AcceptorEvents.AddRange(Acceptor.PollSession().Events);
                polls++;
            }
            Assert.That(completed(), Is.True, StateText());
        }

        internal string StateText()
        {
            SessionSnapshot initiator = Initiator.GetSessionSnapshot();
            SessionSnapshot acceptor = Acceptor.GetSessionSnapshot();
            return "initiator=" + initiator.State + "/" + initiator.NextSenderSeqNum + "/" + initiator.NextTargetSeqNum + ", acceptor=" + acceptor.State + "/" + acceptor.NextSenderSeqNum + "/" + acceptor.NextTargetSeqNum + ", initiator-events=" + string.Join(",", InitiatorEvents.ConvertAll(item => item.Code)) + ", acceptor-events=" + string.Join(",", AcceptorEvents.ConvertAll(item => item.Code));
        }

        private FixSession CreateSession(SessionSettings settings, ISessionTransport transport, FixSessionTests.RecordingSessionApplication application, RecordingDictionaryProvider provider, RecordingValidator validator)
        {
            return new FixSession(settings, transport, new MemoryMessageStore(0), new FixSessionTests.RecordingFixLog(), Clock, new TokenBucketMessageThrottle(Clock), application, new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), validator, provider, new BasicSessionAuthenticator(settings, null));
        }

        private static SessionSettings Settings(SessionRole role, string sender, string target, string applicationName, string applicationVersion)
        {
            SessionSettings settings = FixSessionTests.Settings(false);
            settings.Id.BeginString = FixBeginStrings.Fixt11;
            settings.Id.SenderCompID = sender;
            settings.Id.TargetCompID = target;
            settings.Role = role;
            settings.CheckLatency = false;
            settings.DefaultApplicationVersion = applicationVersion;
            settings.TransportDictionary = new FixDictionaryIdentity { Name = "FIXT11", BeginString = FixBeginStrings.Fixt11 };
            settings.ApplicationDictionary = new FixDictionaryIdentity { Name = applicationName, ApplicationVersion = applicationVersion };
            return settings;
        }
    }

    private class RecordingDictionaryProvider : IDictionaryProvider
    {
        internal EmbeddedDictionaryProvider Provider = new EmbeddedDictionaryProvider(null);
        internal List<string> ResolvedVersions = [];
        internal List<string> ResolvedNames = [];

        public DictionaryLoadResult ResolveDictionary(FixDictionaryIdentity identity)
        {
            DictionaryLoadResult result = Provider.ResolveDictionary(identity);
            ResolvedVersions.Add(identity.ApplicationVersion);
            ResolvedNames.Add(result.Dictionary == null ? "" : result.Dictionary.Identity.Name);
            return result;
        }

        internal void Clear()
        {
            ResolvedVersions.Clear();
            ResolvedNames.Clear();
        }
    }

    private class RecordingValidator : IMessageValidator
    {
        internal FixMessageValidator Validator = new FixMessageValidator();
        internal ValidationResult LastResult = new ValidationResult { IsValid = true };

        public ValidationResult ValidateMessage(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
        {
            LastResult = Validator.ValidateMessage(message, dictionary, options, context);
            return LastResult;
        }

        internal string IssueText()
        {
            return string.Join(",", LastResult.Issues.ConvertAll(item => item.Code + ":" + item.Tag));
        }
    }
}
