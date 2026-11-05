using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class ApplicationMessageOperationsTests
{
    [Test]
    public void CreateSupportsCustomMsgTypeScenarioAndApplicationVersionMetadata()
    {
        FixMessageDefinition baseDefinition = new FixMessageDefinition { Id = 1, Name = "VenueOrder", MsgType = "U100", Category = "Orders", Scenario = "base" };
        FixMessageDefinition auctionDefinition = new FixMessageDefinition { Id = 2, Name = "VenueAuctionOrder", MsgType = "U100", Category = "Auctions", Scenario = "auction" };
        FixDictionary dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "venue",
                BeginString = FixBeginStrings.Fixt11,
                ApplicationVersion = FixApplicationVersions.FixLatest,
                HasExtensionPack = true,
                ExtensionPack = 309,
                CustomVersion = "VENUE-2",
                Scenario = "base",
                Source = "venue.xml"
            },
            Messages = [baseDefinition, auctionDefinition],
            Scenarios =
            [
                new FixScenarioDefinition { Name = "base" },
                new FixScenarioDefinition { Name = "auction", ParentName = "base" }
            ]
        };
        IMessageOperations operations = new FixMessageOperations();

        ApplicationMessageResult result = operations.CreateApplicationMessage(dictionary, "U100", "auction");

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.Multiple(() =>
        {
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.Message!.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(result.Message.MsgType, Is.EqualTo("U100"));
            Assert.That(result.Definition, Is.SameAs(auctionDefinition));
            Assert.That(result.Identity, Is.Not.SameAs(dictionary.Identity));
            Assert.That(result.Identity.Name, Is.EqualTo("venue"));
            Assert.That(result.Identity.Scenario, Is.EqualTo("auction"));
            Assert.That(result.Identity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.FixLatest));
            Assert.That(result.Identity.HasExtensionPack, Is.True);
            Assert.That(result.Identity.ExtensionPack, Is.EqualTo(309));
            Assert.That(result.Identity.CustomVersion, Is.EqualTo("VENUE-2"));
            Assert.That(result.Identity.Source, Is.EqualTo("venue.xml"));
            Assert.That(result.Message.HeaderFields.Select(item => item.Tag), Is.EqualTo(new[]
            {
                FixTags.BeginString,
                FixTags.MsgType,
                FixTags.ApplVerID,
                FixTags.ApplExtID,
                FixTags.CstmApplVerID
            }));
            Assert.That(Text(result.Message, FixTags.MsgType), Is.EqualTo("U100"));
            Assert.That(Text(result.Message, FixTags.ApplVerID), Is.EqualTo(FixApplicationVersions.FixLatest));
            Assert.That(Text(result.Message, FixTags.ApplExtID), Is.EqualTo("309"));
            Assert.That(Text(result.Message, FixTags.CstmApplVerID), Is.EqualTo("VENUE-2"));
            Assert.That(dictionary.Identity.Scenario, Is.EqualTo("base"));
            Assert.That(dictionary.Messages, Is.EqualTo(new[] { baseDefinition, auctionDefinition }));
        });
    }

    [Test]
    public void IdentifyUsesScenarioInheritanceAndPerMessageVersionOverrides()
    {
        FixMessageDefinition definition = new FixMessageDefinition { Id = 1, Name = "Order", MsgType = "D", Category = "Orders", Scenario = "base" };
        FixDictionary dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                ApplicationVersion = FixApplicationVersions.Fix50,
                HasExtensionPack = true,
                ExtensionPack = 100,
                CustomVersion = "OLD",
                Scenario = "base"
            },
            Messages = [definition],
            Scenarios =
            [
                new FixScenarioDefinition { Name = "base" },
                new FixScenarioDefinition { Name = "auction", ParentName = "base" }
            ]
        };
        FixMessage message = new FixMessage
        {
            BeginString = FixBeginStrings.Fixt11,
            MsgType = "D",
            HeaderFields =
            [
                Field(FixTags.MsgType, "D"),
                Field(FixTags.ApplVerID, FixApplicationVersions.Fix50Sp2),
                Field(FixTags.ApplExtID, "309"),
                Field(FixTags.CstmApplVerID, "VENUE-3")
            ]
        };

        ApplicationMessageResult result = new FixMessageOperations().IdentifyApplicationMessage(message, dictionary, "auction");

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.Multiple(() =>
        {
            Assert.That(result.Message, Is.SameAs(message));
            Assert.That(result.Definition, Is.SameAs(definition));
            Assert.That(result.Identity.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(result.Identity.Scenario, Is.EqualTo("auction"));
            Assert.That(result.Identity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(result.Identity.HasExtensionPack, Is.True);
            Assert.That(result.Identity.ExtensionPack, Is.EqualTo(309));
            Assert.That(result.Identity.CustomVersion, Is.EqualTo("VENUE-3"));
            Assert.That(dictionary.Identity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50));
            Assert.That(dictionary.Identity.ExtensionPack, Is.EqualTo(100));
            Assert.That(dictionary.Identity.CustomVersion, Is.EqualTo("OLD"));
            Assert.That(message.HeaderFields, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void EmptyScenarioSupportsClassicAndScenarioNeutralDictionaries()
    {
        FixMessageDefinition definition = new FixMessageDefinition { Id = 1, Name = "NewOrderSingle", MsgType = "D", Category = "app" };
        FixDictionary dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { BeginString = FixBeginStrings.Fix44 },
            Messages = [definition]
        };
        FixMessageOperations operations = new FixMessageOperations();

        ApplicationMessageResult created = operations.CreateApplicationMessage(dictionary, "D", "");
        FixMessage headerOnly = new FixMessage { BeginString = FixBeginStrings.Fix44, HeaderFields = [Field(FixTags.MsgType, "D")] };
        ApplicationMessageResult identified = operations.IdentifyApplicationMessage(headerOnly, dictionary, "");

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True, created.ErrorMessage);
            Assert.That(created.Definition, Is.SameAs(definition));
            Assert.That(created.Identity.Scenario, Is.Empty);
            Assert.That(created.Message!.HeaderFields.Select(item => item.Tag), Is.EqualTo(new[] { FixTags.BeginString, FixTags.MsgType }));
            Assert.That(identified.Success, Is.True, identified.ErrorMessage);
            Assert.That(identified.Message, Is.SameAs(headerOnly));
            Assert.That(identified.Definition, Is.SameAs(definition));
            Assert.That(identified.Identity.ApplicationVersion, Is.Empty);
        });
    }

    [Test]
    public void InvalidRequestsReturnStableFailuresWithoutMutatingInputs()
    {
        FixMessageDefinition first = new FixMessageDefinition { Id = 1, Name = "First", MsgType = "D", Scenario = "alternate" };
        FixMessageDefinition baseDefinition = new FixMessageDefinition { Id = 2, Name = "Base", MsgType = "D", Scenario = "base" };
        FixDictionary dictionary = new FixDictionary { Messages = [first, baseDefinition] };
        FixMessageOperations operations = new FixMessageOperations();
        FixMessage conflicting = new FixMessage { MsgType = "D", HeaderFields = [Field(FixTags.MsgType, "F")] };
        FixMessage duplicateVersion = new FixMessage
        {
            MsgType = "D",
            HeaderFields =
            [
                Field(FixTags.ApplVerID, FixApplicationVersions.Fix50),
                Field(FixTags.ApplVerID, FixApplicationVersions.Fix50Sp2)
            ]
        };
        FixMessage invalidExtension = new FixMessage { MsgType = "D", HeaderFields = [Field(FixTags.ApplExtID, "30x")] };

        ApplicationMessageResult unknownType = operations.CreateApplicationMessage(dictionary, "Z", "alternate");
        ApplicationMessageResult unknownScenario = operations.CreateApplicationMessage(dictionary, "D", "missing");
        ApplicationMessageResult conflict = operations.IdentifyApplicationMessage(conflicting, dictionary, "alternate");
        ApplicationMessageResult duplicate = operations.IdentifyApplicationMessage(duplicateVersion, dictionary, "alternate");
        ApplicationMessageResult invalid = operations.IdentifyApplicationMessage(invalidExtension, dictionary, "alternate");

        Assert.Multiple(() =>
        {
            Assert.That(unknownType.ErrorCode, Is.EqualTo("unknown-application-message-type"));
            Assert.That(unknownScenario.ErrorCode, Is.EqualTo("unknown-application-message-scenario"));
            Assert.That(conflict.ErrorCode, Is.EqualTo("conflicting-message-type"));
            Assert.That(duplicate.ErrorCode, Is.EqualTo("duplicate-application-version-metadata"));
            Assert.That(invalid.ErrorCode, Is.EqualTo("invalid-application-extension"));
            Assert.That(unknownType.Success || unknownScenario.Success || conflict.Success || duplicate.Success || invalid.Success, Is.False);
            Assert.That(dictionary.Messages, Is.EqualTo(new[] { first, baseDefinition }));
            Assert.That(conflicting.MsgType, Is.EqualTo("D"));
            Assert.That(Text(conflicting, FixTags.MsgType), Is.EqualTo("F"));
            Assert.That(duplicateVersion.HeaderFields, Has.Count.EqualTo(2));
            Assert.That(Text(invalidExtension, FixTags.ApplExtID), Is.EqualTo("30x"));
        });
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    private static string Text(FixMessage message, int tag)
    {
        FixField field = message.HeaderFields.Single(item => item.Tag == tag);
        return Encoding.Latin1.GetString(field.Value);
    }
}
