using System.Collections.Generic;

namespace diameter.Tests;

public class CommandValidatorTests
{
    [Test]
    public void ValidCommandAcceptsCountsOrderingAndUnconstrainedExtensions()
    {
        CommandValidator validator = CreateValidator(true);
        DiameterMessage message = CreateValidMessage();
        message.Avps.Insert(2, CreateAvp(999U, 0U));

        CommandValidationResult result = validator.ValidateCommand(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(result.Definition, Is.Not.Null);
            Assert.That(result.Definition!.Name, Is.EqualTo("Test-Command"));
            Assert.That(result.Failures, Is.Empty);
        });
    }

    [Test]
    public void HeaderValidationReportsMissingAndProhibitedFlagsTogether()
    {
        CommandValidator validator = CreateValidator(true);
        DiameterMessage message = CreateValidMessage();
        message.Flags = DiameterMessageFlags.Request | DiameterMessageFlags.Error;

        CommandValidationResult result = validator.ValidateCommand(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Invalid));
            Assert.That(result.Failures, Has.Count.EqualTo(2));
            AssertFailure(result, 0, "required-header-flags-missing", 0U, -1);
            Assert.That(
                result.Failures[0].ExpectedFlags,
                Is.EqualTo(DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable));
            Assert.That(result.Failures[0].ActualFlags, Is.EqualTo(message.Flags));
            AssertFailure(result, 1, "prohibited-header-flags-present", 0U, -1);
            Assert.That(
                result.Failures[1].ExpectedFlags,
                Is.EqualTo(
                    DiameterMessageFlags.Error |
                    DiameterMessageFlags.PotentiallyRetransmitted));
        });
    }

    [Test]
    public void WrongDirectionUsesKnownDefinitionToReportRequestFlagFailure()
    {
        CommandValidator validator = CreateValidator(true);
        DiameterMessage message = CreateValidMessage();
        message.Flags = DiameterMessageFlags.Proxyable;

        CommandValidationResult result = validator.ValidateCommand(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Definition, Is.Not.Null);
            Assert.That(result.Failures, Has.Count.EqualTo(1));
            AssertFailure(result, 0, "required-header-flags-missing", 0U, -1);
        });
    }

    [Test]
    public void CommandLookupDistinguishesApplicationMismatchFromUnknownCommand()
    {
        CommandValidator validator = CreateValidator(true);
        DiameterMessage wrongApplication = CreateValidMessage();
        wrongApplication.ApplicationId = 0U;
        DiameterMessage unknown = CreateValidMessage();
        unknown.CommandCode = 999U;

        CommandValidationResult mismatch = validator.ValidateCommand(wrongApplication);
        CommandValidationResult missing = validator.ValidateCommand(unknown);

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Definition, Is.Null);
            Assert.That(mismatch.Failures, Has.Count.EqualTo(1));
            AssertFailure(mismatch, 0, "application-id-mismatch", 0U, -1);
            Assert.That(missing.Definition, Is.Null);
            Assert.That(missing.Failures, Has.Count.EqualTo(1));
            AssertFailure(missing, 0, "unknown-command", 0U, -1);
        });
    }

    [Test]
    public void OccurrenceValidationReportsMissingAndFirstExcessOccurrence()
    {
        CommandValidator validator = CreateValidator(true);
        DiameterMessage message = CreateValidMessage();
        RemoveAvp(message.Avps, 283U, 0U);
        message.Avps.Add(CreateAvp(1U, 0U));

        CommandValidationResult result = validator.ValidateCommand(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Count.EqualTo(2));
            AssertFailure(result, 0, "missing-required-avp", 283U, -1);
            Assert.That(result.Failures[0].ExpectedMinimum, Is.EqualTo(1));
            Assert.That(result.Failures[0].ExpectedMaximum, Is.EqualTo(1));
            Assert.That(result.Failures[0].ActualCount, Is.Zero);
            AssertFailure(result, 1, "too-many-avp-occurrences", 1U, 3);
            Assert.That(result.Failures[1].ExpectedMaximum, Is.EqualTo(1));
            Assert.That(result.Failures[1].ActualCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void FixedHeadValidationReportsTheActualAndExpectedPositions()
    {
        CommandValidator validator = CreateValidator(true);
        DiameterMessage message = CreateValidMessage();
        DiameterAvp session = message.Avps[0];
        message.Avps.RemoveAt(0);
        message.Avps.Insert(1, session);

        CommandValidationResult result = validator.ValidateCommand(message);
        CommandValidationFailure failure = FindFailure(
            result.Failures,
            "fixed-head-avp-misordered");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Invalid));
            Assert.That(failure.AvpCode, Is.EqualTo(263U));
            Assert.That(failure.AvpIndex, Is.EqualTo(1));
            Assert.That(failure.ExpectedPosition, Is.Zero);
        });
    }

    [Test]
    public void OrderingValidationIgnoresExtensionsButRejectsDescendingGroups()
    {
        CommandValidator validator = CreateValidator(true);
        DiameterMessage message = CreateValidMessage();
        DiameterAvp userName = message.Avps[3];
        message.Avps.RemoveAt(3);
        message.Avps.Insert(2, userName);
        message.Avps.Insert(2, CreateAvp(999U, 0U));

        CommandValidationResult result = validator.ValidateCommand(message);
        CommandValidationFailure failure = FindFailure(
            result.Failures,
            "avp-ordering-violation");

        Assert.Multiple(() =>
        {
            Assert.That(failure.AvpCode, Is.EqualTo(283U));
            Assert.That(failure.AvpIndex, Is.EqualTo(4));
            Assert.That(
                CountFailures(result.Failures, "avp-ordering-violation"),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ClosedGrammarRejectsEveryUnlistedAvpByVendorAndCode()
    {
        CommandValidator validator = CreateValidator(false);
        DiameterMessage message = CreateValidMessage();
        message.Avps.Add(CreateAvp(999U, 0U));
        message.Avps.Add(CreateAvp(700U, 0U));

        CommandValidationResult result = validator.ValidateCommand(message);

        Assert.Multiple(() =>
        {
            Assert.That(CountFailures(result.Failures, "avp-not-allowed"), Is.EqualTo(2));
            AssertFailure(result, 0, "avp-not-allowed", 999U, 4);
            AssertFailure(result, 1, "avp-not-allowed", 700U, 5);
            Assert.That(result.Failures[1].AvpVendorId, Is.Zero);
        });
    }

    [Test]
    public void VendorScopedRuleMatchesOnlyTheAssignedVendorIdentity()
    {
        CommandValidator validator = CreateValidator(false);
        DiameterMessage message = CreateValidMessage();
        message.Avps.Insert(3, CreateAvp(700U, 10415U));

        CommandValidationResult accepted = validator.ValidateCommand(message);
        message.Avps[3].VendorId = 0U;
        CommandValidationResult rejected = validator.ValidateCommand(message);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(rejected.Status, Is.EqualTo(DiameterValidationStatus.Invalid));
            Assert.That(rejected.Failures, Has.Count.EqualTo(1));
            AssertFailure(rejected, 0, "avp-not-allowed", 700U, 3);
        });
    }

    [Test]
    public void BuiltInCommandRulesValidateACompleteCapabilitiesExchangeRequest()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        Assert.That(
            builder.RegisterDictionaryModule(Rfc6733Dictionary.CreateModule()).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        CommandValidator validator = new CommandValidator(builder.ReadDictionarySnapshot());
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request;
        message.ApplicationId = DiameterApplicationIds.Common;
        message.CommandCode = DiameterCommandCodes.CapabilitiesExchange;
        message.Avps.Add(CreateAvp(DiameterAvpCodes.OriginHost, 0U));
        message.Avps.Add(CreateAvp(DiameterAvpCodes.OriginRealm, 0U));
        message.Avps.Add(CreateAvp(DiameterAvpCodes.HostIpAddress, 0U));
        message.Avps.Add(CreateAvp(DiameterAvpCodes.VendorId, 0U));
        message.Avps.Add(CreateAvp(DiameterAvpCodes.ProductName, 0U));
        message.Avps.Add(CreateAvp(999U, 0U));

        CommandValidationResult result = validator.ValidateCommand(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(result.Definition, Is.Not.Null);
            Assert.That(result.Definition!.Name, Is.EqualTo("Capabilities-Exchange"));
        });
    }

    [Test]
    public void NullMessageAndEmptyDictionaryReturnStableFailures()
    {
        CommandValidator validator = new CommandValidator(null);

        CommandValidationResult nullMessage = validator.ValidateCommand(null);
        CommandValidationResult unknown = validator.ValidateCommand(new DiameterMessage());

        Assert.Multiple(() =>
        {
            AssertFailure(nullMessage, 0, "message-required", 0U, -1);
            AssertFailure(unknown, 0, "unknown-command", 0U, -1);
        });
    }

    private static CommandValidator CreateValidator(bool allowUnlisted)
    {
        DiameterCommandDefinition command = new DiameterCommandDefinition();
        command.ApplicationId = 4U;
        command.Code = 272U;
        command.Name = "Test-Command";
        command.IsRequest = true;
        command.RequiredFlags =
            DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable;
        command.ProhibitedFlags =
            DiameterMessageFlags.Error | DiameterMessageFlags.PotentiallyRetransmitted;
        command.AllowUnlistedAvps = allowUnlisted;
        command.Rules.Add(CreateRule(263U, 0U, "Session-Id", 1, 1, 0, 0));
        command.Rules.Add(CreateRule(264U, 0U, "Origin-Host", 1, 1, -1, 0));
        command.Rules.Add(CreateRule(283U, 0U, "Destination-Realm", 1, 1, -1, 1));
        command.Rules.Add(CreateRule(700U, 10415U, "Vendor-Extension", 0, 1, -1, 1));
        command.Rules.Add(CreateRule(1U, 0U, "User-Name", 0, 1, -1, 2));
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        snapshot.Commands.Add(command);
        return new CommandValidator(snapshot);
    }

    private static DiameterMessage CreateValidMessage()
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable;
        message.ApplicationId = 4U;
        message.CommandCode = 272U;
        message.Avps.Add(CreateAvp(263U, 0U));
        message.Avps.Add(CreateAvp(264U, 0U));
        message.Avps.Add(CreateAvp(283U, 0U));
        message.Avps.Add(CreateAvp(1U, 0U));
        return message;
    }

    private static DiameterAvpRuleDefinition CreateRule(
        uint code,
        uint vendorId,
        string name,
        int minimum,
        int maximum,
        int fixedPosition,
        int orderingGroup)
    {
        DiameterAvpRuleDefinition rule = new DiameterAvpRuleDefinition();
        rule.AvpCode = code;
        rule.AvpVendorId = vendorId;
        rule.AvpName = name;
        rule.MinimumCount = minimum;
        rule.MaximumCount = maximum;
        rule.FixedHeadPosition = fixedPosition;
        rule.OrderingGroup = orderingGroup;
        return rule;
    }

    private static DiameterAvp CreateAvp(uint code, uint vendorId)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.VendorId = vendorId;
        return avp;
    }

    private static void RemoveAvp(List<DiameterAvp> avps, uint code, uint vendorId)
    {
        int index = 0;
        while (index < avps.Count)
        {
            if (avps[index].Code == code && avps[index].VendorId == vendorId)
            {
                avps.RemoveAt(index);
                return;
            }

            index = index + 1;
        }
    }

    private static CommandValidationFailure FindFailure(
        List<CommandValidationFailure> failures,
        string code)
    {
        int index = 0;
        while (index < failures.Count)
        {
            if (failures[index].Code == code)
            {
                return failures[index];
            }

            index = index + 1;
        }

        Assert.Fail("Missing validation failure: " + code);
        return new CommandValidationFailure();
    }

    private static int CountFailures(
        List<CommandValidationFailure> failures,
        string code)
    {
        int count = 0;
        int index = 0;
        while (index < failures.Count)
        {
            if (failures[index].Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    private static void AssertFailure(
        CommandValidationResult result,
        int index,
        string code,
        uint avpCode,
        int avpIndex)
    {
        Assert.That(result.Failures[index].Code, Is.EqualTo(code));
        Assert.That(result.Failures[index].AvpCode, Is.EqualTo(avpCode));
        Assert.That(result.Failures[index].AvpIndex, Is.EqualTo(avpIndex));
    }
}
