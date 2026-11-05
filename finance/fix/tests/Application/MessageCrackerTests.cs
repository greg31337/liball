using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class MessageCrackerTests
{
    [Test]
    public void CrackDispatchesEveryAdministrativeTypeSeparatelyFromCustomApplicationTypes()
    {
        string[] administrativeTypes =
        [
            FixAdminMessageTypes.Heartbeat,
            FixAdminMessageTypes.TestRequest,
            FixAdminMessageTypes.ResendRequest,
            FixAdminMessageTypes.Reject,
            FixAdminMessageTypes.SequenceReset,
            FixAdminMessageTypes.Logout,
            FixAdminMessageTypes.Logon,
            FixAdminMessageTypes.BusinessMessageReject
        ];
        FixDictionary transport = new FixDictionary { Identity = new FixDictionaryIdentity { BeginString = FixBeginStrings.Fixt11 } };
        for (int index = 0; index < administrativeTypes.Length; index++)
        {
            transport.Messages.Add(new FixMessageDefinition
            {
                Id = index + 1,
                Name = "Admin" + index,
                MsgType = administrativeTypes[index],
                Category = "Session"
            });
        }
        FixMessageDefinition applicationDefinition = new FixMessageDefinition
        {
            Id = 100,
            Name = "VenueOrder",
            MsgType = "U100",
            Category = "VenueOrders",
            Scenario = "base"
        };
        FixDictionary application = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.FixLatest, Scenario = "base" },
            Messages = [applicationDefinition]
        };
        RecordingCrackHandler handler = new RecordingCrackHandler();
        IMessageCracker cracker = new FixMessageCracker();

        List<MessageCrackResult> administrativeResults = [];
        for (int index = 0; index < administrativeTypes.Length; index++)
        {
            administrativeResults.Add(cracker.CrackMessage(new FixMessage { MsgType = administrativeTypes[index] }, transport, application, "auction", handler));
        }
        MessageCrackResult applicationResult = cracker.CrackMessage(new FixMessage { MsgType = "U100" }, transport, application, "base", handler);

        Assert.Multiple(() =>
        {
            Assert.That(administrativeResults, Is.All.Matches<MessageCrackResult>(item => item.Success));
            Assert.That(administrativeResults, Is.All.Matches<MessageCrackResult>(item => item.Context!.IsAdministrative));
            Assert.That(administrativeResults.Select(item => item.Context!.Category), Is.All.EqualTo("Session"));
            Assert.That(handler.AdministrativeCalls, Is.EqualTo(administrativeTypes.Length));
            Assert.That(handler.ApplicationCalls, Is.EqualTo(1));
            Assert.That(applicationResult.Success, Is.True, applicationResult.ErrorMessage);
            Assert.That(applicationResult.Context!.Message.MsgType, Is.EqualTo("U100"));
            Assert.That(applicationResult.Context.Definition, Is.SameAs(applicationDefinition));
            Assert.That(applicationResult.Context.Name, Is.EqualTo("VenueOrder"));
            Assert.That(applicationResult.Context.Category, Is.EqualTo("VenueOrders"));
            Assert.That(applicationResult.Context.IsAdministrative, Is.False);
            Assert.That(applicationResult.Context.Identity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.FixLatest));
            Assert.That(applicationResult.Decision, Is.SameAs(handler.ApplicationDecision));
        });
    }

    [Test]
    public void CrackIdentifiesHeaderOnlyMsgTypeAndCarriesPerMessageVersionMetadata()
    {
        FixMessageDefinition definition = new FixMessageDefinition { Id = 1, Name = "Order", MsgType = "D", Category = "Orders", Scenario = "base" };
        FixDictionary application = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.Fix50, Scenario = "base" },
            Messages = [definition]
        };
        FixMessage message = new FixMessage
        {
            BeginString = FixBeginStrings.Fixt11,
            HeaderFields =
            [
                Field(FixTags.MsgType, "D"),
                Field(FixTags.ApplVerID, FixApplicationVersions.Fix50Sp2),
                Field(FixTags.ApplExtID, "309"),
                Field(FixTags.CstmApplVerID, "VENUE")
            ]
        };
        RecordingCrackHandler handler = new RecordingCrackHandler();

        MessageCrackResult result = new FixMessageCracker().CrackMessage(message, new FixDictionary(), application, "base", handler);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.Multiple(() =>
        {
            Assert.That(handler.ApplicationCalls, Is.EqualTo(1));
            Assert.That(handler.AdministrativeCalls, Is.Zero);
            Assert.That(result.Context!.Message, Is.SameAs(message));
            Assert.That(result.Context.Definition, Is.SameAs(definition));
            Assert.That(result.Context.Identity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(result.Context.Identity.ExtensionPack, Is.EqualTo(309));
            Assert.That(result.Context.Identity.CustomVersion, Is.EqualTo("VENUE"));
            Assert.That(message.MsgType, Is.Empty);
            Assert.That(message.HeaderFields, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void NameAndCategoryLookupHonorScenarioInheritanceAndStableTypeSelection()
    {
        FixMessageDefinition baseOrder = Definition(1, "Order", "D", "Orders", "base");
        FixMessageDefinition baseCancel = Definition(2, "Cancel", "F", "Orders", "base");
        FixMessageDefinition auctionOrder = Definition(3, "AuctionOrder", "D", "Orders", "auction");
        FixMessageDefinition quote = Definition(4, "Quote", "S", "MarketData", "base");
        FixDictionary dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.FixLatest, Scenario = "base" },
            Options = new FixDictionaryOptions { NormalizeNames = true },
            Messages = [baseOrder, baseCancel, auctionOrder, quote],
            Scenarios =
            [
                new FixScenarioDefinition { Name = "base" },
                new FixScenarioDefinition { Name = "auction", ParentName = "base" }
            ]
        };
        IMessageCracker cracker = new FixMessageCracker();

        MessageLookupResult byName = cracker.LookupMessageName(dictionary, " auctionorder ", " AUCTION ");
        MessageLookupResult inheritedName = cracker.LookupMessageName(dictionary, "cancel", "auction");
        MessageLookupResult byCategory = cracker.LookupMessageCategory(dictionary, " orders ", "auction");

        Assert.Multiple(() =>
        {
            Assert.That(byName.Success, Is.True, byName.ErrorMessage);
            Assert.That(byName.Definitions, Is.EqualTo(new[] { auctionOrder }));
            Assert.That(byName.Identity.Scenario, Is.EqualTo("AUCTION"));
            Assert.That(byName.Identity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.FixLatest));
            Assert.That(inheritedName.Definitions, Is.EqualTo(new[] { baseCancel }));
            Assert.That(byCategory.Success, Is.True, byCategory.ErrorMessage);
            Assert.That(byCategory.Definitions, Is.EqualTo(new[] { auctionOrder, baseCancel }));
            Assert.That(byCategory.Identity.Scenario, Is.EqualTo("auction"));
            Assert.That(dictionary.Messages, Is.EqualTo(new[] { baseOrder, baseCancel, auctionOrder, quote }));
            Assert.That(dictionary.Identity.Scenario, Is.EqualTo("base"));
        });
    }

    [Test]
    public void CrackAndLookupFailuresAreExplicitAndDoNotInvokeHandlers()
    {
        FixMessageDefinition order = Definition(1, "Order", "D", "Orders", "base");
        FixDictionary application = new FixDictionary { Messages = [order] };
        RecordingCrackHandler handler = new RecordingCrackHandler();
        FixMessageCracker cracker = new FixMessageCracker();

        MessageCrackResult unknown = cracker.CrackMessage(new FixMessage { MsgType = "Z" }, new FixDictionary(), application, "base", handler);
        MessageCrackResult missingHandler = cracker.CrackMessage(new FixMessage { MsgType = "D" }, new FixDictionary(), application, "base", null!);
        handler.ReturnNullDecision = true;
        MessageCrackResult missingDecision = cracker.CrackMessage(new FixMessage { MsgType = "D" }, new FixDictionary(), application, "base", handler);
        MessageLookupResult unknownName = cracker.LookupMessageName(application, "Missing", "base");
        MessageLookupResult unknownCategory = cracker.LookupMessageCategory(application, "Missing", "base");

        Assert.Multiple(() =>
        {
            Assert.That(unknown.ErrorCode, Is.EqualTo("unknown-application-message-type"));
            Assert.That(missingHandler.ErrorCode, Is.EqualTo("missing-crack-handler"));
            Assert.That(missingDecision.ErrorCode, Is.EqualTo("missing-crack-decision"));
            Assert.That(unknownName.ErrorCode, Is.EqualTo("unknown-message-name"));
            Assert.That(unknownCategory.ErrorCode, Is.EqualTo("unknown-message-category"));
            Assert.That(unknown.Success || missingHandler.Success || missingDecision.Success || unknownName.Success || unknownCategory.Success, Is.False);
            Assert.That(handler.AdministrativeCalls, Is.Zero);
            Assert.That(handler.ApplicationCalls, Is.EqualTo(1));
            Assert.That(application.Messages, Is.EqualTo(new[] { order }));
        });
    }

    private static FixMessageDefinition Definition(int id, string name, string msgType, string category, string scenario)
    {
        return new FixMessageDefinition { Id = id, Name = name, MsgType = msgType, Category = category, Scenario = scenario };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    private class RecordingCrackHandler : IMessageCrackHandler
    {
        public int AdministrativeCalls;
        public int ApplicationCalls;
        public bool ReturnNullDecision;
        public ApplicationDecision AdministrativeDecision = new ApplicationDecision { Disposition = MessageDisposition.Accept, Code = "admin" };
        public ApplicationDecision ApplicationDecision = new ApplicationDecision { Disposition = MessageDisposition.Accept, Code = "application" };

        public ApplicationDecision HandleAdministrativeMessage(MessageCrackContext context)
        {
            AdministrativeCalls++;
            return ReturnNullDecision ? null! : AdministrativeDecision;
        }

        public ApplicationDecision HandleApplicationMessage(MessageCrackContext context)
        {
            ApplicationCalls++;
            return ReturnNullDecision ? null! : ApplicationDecision;
        }
    }
}
