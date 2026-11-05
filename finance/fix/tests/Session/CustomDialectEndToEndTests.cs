using System;
using System.IO;
using System.Linq;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class CustomDialectEndToEndTests
{
    [Test]
    public void QuickFixStructureAndOrchestraRulesGenerateAndExchangeNestedVenueModels()
    {
        DictionaryLoadResult quickFix = LoadQuickFixExtension();
        DictionaryLoadResult orchestra = LoadOrchestraExtension();
        Assert.That(quickFix.Dictionary, Is.Not.Null);
        Assert.That(orchestra.Dictionary, Is.Not.Null);
        DictionaryLoadResult composed = new FixDictionaryComposer().ComposeDictionary(quickFix.Dictionary!, orchestra.Dictionary!, new FixDictionaryOptions());
        Assert.That(composed.Dictionary, Is.Not.Null);
        FixDictionary dialect = composed.Dictionary!;
        dialect.Identity.Name = "VenueDialect";
        dialect.Identity.BeginString = "";
        dialect.Identity.ApplicationVersion = "99";
        dialect.Identity.CustomVersion = "VENUE-1";

        GenerationResult generated = new CSharpModelGenerator().GenerateModels(dialect, new ModelGenerationOptions
        {
            Namespace = "Venue.Custom.Generated",
            MessageNames = ["VenueOrder"]
        });
        DialectPair pair = new DialectPair(dialect);
        pair.StartAndActivate();

        SessionSendResult invalid = pair.Initiator.SendMessage(VenueOrder("INVALID", false));
        AssertAccepted(pair.Initiator.SendMessage(VenueOrder("ORDER-1", true)), pair.InitiatorValidator);
        pair.PumpUntil(() => pair.AcceptorApplication.ApplicationContexts.Count == 1);
        FixMessage received = pair.AcceptorApplication.ApplicationContexts[0].Message;
        Assert.That(received.BodyGroups.Select(item => item.CountTag), Does.Contain(9010), MessageShape(received));
        FixGroup outer = received.BodyGroups.Single(item => item.CountTag == 9010);
        FixGroup inner = outer.Entries[0].Groups.Single(item => item.CountTag == 9012);

        SessionPollResult logout = pair.Initiator.LogoutSession("custom-dialect-complete");
        pair.InitiatorEvents.AddRange(logout.Events);
        pair.PumpUntil(() => pair.Initiator.GetSessionSnapshot().State == SessionState.Disconnected && pair.Acceptor.GetSessionSnapshot().State == SessionState.Disconnected);

        FixMemberDefinition approval = dialect.Messages.Single(item => item.MsgType == "U99").Members.Single(item => item.ReferenceId == 9030);
        Assert.Multiple(() =>
        {
            Assert.That(quickFix.Success, Is.True, DictionaryText(quickFix));
            Assert.That(orchestra.Success, Is.True, DictionaryText(orchestra));
            Assert.That(composed.Success, Is.True, DictionaryText(composed));
            Assert.That(dialect.Messages.Single(item => item.MsgType == "U99").Name, Is.EqualTo("VenueOrder"));
            Assert.That(dialect.Groups.Select(item => item.CountTag), Does.Contain(9010));
            Assert.That(dialect.Groups.Select(item => item.CountTag), Does.Contain(9012));
            Assert.That(approval.Presence, Is.EqualTo(FixPresence.Conditional));
            Assert.That(approval.PresenceRule, Is.EqualTo("exists(9002)"));
            Assert.That(approval.RulePresence, Is.EqualTo(FixPresence.Required));
            Assert.That(generated.Success, Is.True, GenerationText(generated));
            Assert.That(generated.Files.Select(item => item.Path), Does.Contain("Messages/VenueOrder.cs"));
            Assert.That(generated.Files.Select(item => item.Path), Does.Contain("Groups/NoVenueParties.cs"));
            Assert.That(generated.Files.Select(item => item.Path), Does.Contain("Groups/NoVenuePartyRoles.cs"));
            Assert.That(generated.Files.Single(item => item.Path == "Messages/VenueOrder.cs").Content, Does.Contain("VenueApprovalCode"));
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Code, Is.EqualTo("conditionally-required-member-missing"));
            Assert.That(pair.Initiator.GetSessionSnapshot().NextSenderSeqNum, Is.EqualTo(4));
            Assert.That(pair.AcceptorApplication.ApplicationContexts[0].SequenceNumber, Is.EqualTo(2));
            Assert.That(Text(received, 9002), Is.EqualTo("ORDER-1"));
            Assert.That(Text(received, 9030), Is.EqualTo("APPROVED"));
            Assert.That(outer.DeclaredCount, Is.EqualTo(1));
            Assert.That(outer.Entries, Has.Count.EqualTo(1));
            Assert.That(Text(outer.Entries[0].Fields.Single(item => item.Tag == 9011)), Is.EqualTo("PARTY-1"));
            Assert.That(inner.DeclaredCount, Is.EqualTo(1));
            Assert.That(Text(inner.Entries[0].Fields.Single(item => item.Tag == 9013)), Is.EqualTo("7"));
            Assert.That(pair.InitiatorProvider.CustomResolutionCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(pair.AcceptorProvider.CustomResolutionCount, Is.GreaterThanOrEqualTo(1));
        });
    }

    private static DictionaryLoadResult LoadQuickFixExtension()
    {
        string path = Path.Combine(RepositoryRoot(), "tests", "Fixtures", "QuickFix", "Venue44.xml");
        using FileStream source = File.OpenRead(path);
        return new QuickFixDictionaryLoader().LoadDictionary(source, new FixDictionaryOptions());
    }

    private static DictionaryLoadResult LoadOrchestraExtension()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2020/orchestra/repository" name="VenueRules" version="FIX.4.4">
              <fixr:metadata/>
              <fixr:datatypes>
                <fixr:datatype name="String"/>
              </fixr:datatypes>
              <fixr:fields>
                <fixr:field id="9030" name="VenueApprovalCode" type="String"/>
              </fixr:fields>
              <fixr:messages>
                <fixr:message id="129" name="VenueOrder" msgType="U99" category="app">
                  <fixr:structure>
                    <fixr:fieldRef id="9030">
                      <fixr:rule presence="required"><fixr:when>exists(9002)</fixr:when></fixr:rule>
                    </fixr:fieldRef>
                  </fixr:structure>
                </fixr:message>
              </fixr:messages>
            </fixr:repository>
            """;
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new OrchestraDictionaryLoader().LoadDictionary(source, new FixDictionaryOptions());
    }

    private static FixMessage VenueOrder(string orderId, bool includeApproval)
    {
        FixGroup nested = new FixGroup
        {
            CountTag = 9012,
            DelimiterTag = 9013,
            Entries =
            [
                new FixGroupEntry { EntryIndex = 0, Fields = [FixSessionTests.Field(9013, "7")] }
            ]
        };
        FixGroup parties = new FixGroup
        {
            CountTag = 9010,
            DelimiterTag = 9011,
            Entries =
            [
                new FixGroupEntry
                {
                    EntryIndex = 0,
                    Fields = [FixSessionTests.Field(9011, "PARTY-1")],
                    Groups = [nested]
                }
            ]
        };
        FixMessage message = new FixMessage
        {
            MsgType = "U99",
            BodyFields =
            [
                FixSessionTests.Field(9002, orderId),
                FixSessionTests.Field(9004, "A")
            ],
            BodyGroups = [parties]
        };
        if (includeApproval) message.BodyFields.Add(FixSessionTests.Field(9030, "APPROVED"));
        return message;
    }

    private static string Text(FixMessage message, int tag)
    {
        FixField? field = EnvelopeValidator.FindFirstMessageField(message, tag);
        return field == null ? "" : Text(field);
    }

    private static string Text(FixField field)
    {
        return Encoding.Latin1.GetString(field.Value);
    }

    private static string DictionaryText(DictionaryLoadResult result)
    {
        return string.Join(",", result.Diagnostics.ConvertAll(item => item.Code + ":" + item.Message));
    }

    private static string GenerationText(GenerationResult result)
    {
        return string.Join(",", result.Diagnostics.ConvertAll(item => item.Code + ":" + item.Message));
    }

    private static string MessageShape(FixMessage message)
    {
        return "header-fields=" + string.Join(",", message.HeaderFields.ConvertAll(item => item.Tag)) + "; body-fields=" + string.Join(",", message.BodyFields.ConvertAll(item => item.Tag)) + "; trailer-fields=" + string.Join(",", message.TrailerFields.ConvertAll(item => item.Tag)) + "; header-groups=" + string.Join(",", message.HeaderGroups.ConvertAll(item => item.CountTag)) + "; body-groups=" + string.Join(",", message.BodyGroups.ConvertAll(item => item.CountTag)) + "; trailer-groups=" + string.Join(",", message.TrailerGroups.ConvertAll(item => item.CountTag)) + "; wire=" + Encoding.Latin1.GetString(message.OriginalBytes);
    }

    private static void AssertAccepted(SessionSendResult result, RecordingValidator validator)
    {
        Assert.That(result.Accepted, Is.True, result.Code + ": " + result.Text + ": " + validator.IssueText());
    }

    private static string RepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
    }

    private class DialectPair
    {
        internal InMemoryDuplexTransportPair Transport = new InMemoryDuplexTransportPair();
        internal FakeSessionClock Clock = new FakeSessionClock(FixSessionTests.Now);
        internal CustomDictionaryProvider InitiatorProvider;
        internal CustomDictionaryProvider AcceptorProvider;
        internal RecordingValidator InitiatorValidator = new RecordingValidator();
        internal RecordingValidator AcceptorValidator = new RecordingValidator();
        internal FixSessionTests.RecordingSessionApplication InitiatorApplication = new FixSessionTests.RecordingSessionApplication();
        internal FixSessionTests.RecordingSessionApplication AcceptorApplication = new FixSessionTests.RecordingSessionApplication();
        internal System.Collections.Generic.List<SessionEvent> InitiatorEvents = [];
        internal System.Collections.Generic.List<SessionEvent> AcceptorEvents = [];
        internal FixSession Initiator;
        internal FixSession Acceptor;

        internal DialectPair(FixDictionary dictionary)
        {
            InitiatorProvider = new CustomDictionaryProvider(dictionary);
            AcceptorProvider = new CustomDictionaryProvider(dictionary);
            SessionSettings initiatorSettings = Settings(SessionRole.Initiator, "INITIATOR", "ACCEPTOR");
            SessionSettings acceptorSettings = Settings(SessionRole.Acceptor, "ACCEPTOR", "INITIATOR");
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

        private FixSession CreateSession(SessionSettings settings, ISessionTransport transport, FixSessionTests.RecordingSessionApplication application, CustomDictionaryProvider provider, RecordingValidator validator)
        {
            return new FixSession(settings, transport, new MemoryMessageStore(0), new FixSessionTests.RecordingFixLog(), Clock, new TokenBucketMessageThrottle(Clock), application, new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), validator, provider, new BasicSessionAuthenticator(settings, null));
        }

        private static SessionSettings Settings(SessionRole role, string sender, string target)
        {
            SessionSettings settings = FixSessionTests.Settings(false);
            settings.Id.BeginString = FixBeginStrings.Fixt11;
            settings.Id.SenderCompID = sender;
            settings.Id.TargetCompID = target;
            settings.Role = role;
            settings.CheckLatency = false;
            settings.DefaultApplicationVersion = "99";
            settings.TransportDictionary = new FixDictionaryIdentity { Name = "FIXT11", BeginString = FixBeginStrings.Fixt11 };
            settings.ApplicationDictionary = new FixDictionaryIdentity { Name = "VenueDialect", ApplicationVersion = "99" };
            return settings;
        }
    }

    private class CustomDictionaryProvider : IDictionaryProvider
    {
        internal EmbeddedDictionaryProvider Embedded = new EmbeddedDictionaryProvider(null);
        internal FixDictionary CustomDictionary;
        internal int CustomResolutionCount;

        internal CustomDictionaryProvider(FixDictionary customDictionary)
        {
            CustomDictionary = customDictionary;
        }

        public DictionaryLoadResult ResolveDictionary(FixDictionaryIdentity identity)
        {
            if (identity.ApplicationVersion == "99")
            {
                CustomResolutionCount++;
                return new DictionaryLoadResult { Success = true, Dictionary = CustomDictionary };
            }
            return Embedded.ResolveDictionary(identity);
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
