using System;
using System.Text;

namespace diameter.Tests;

public class ValidationResultBuilderTests
{
    [Test]
    public void ValidInternalResultsProduceNoIssuesOrErrorAnswer()
    {
        DiameterDictionarySnapshot snapshot = CreateBaseSnapshot();
        ValidationResultBuilder builder = new ValidationResultBuilder(snapshot);
        CommandValidationResult command = new CommandValidationResult();
        command.Status = DiameterValidationStatus.Valid;
        AvpValidationResult avp = new AvpValidationResult();
        avp.Status = DiameterValidationStatus.Valid;

        DiameterValidationResult result = builder.BuildValidationResult(
            new DiameterMessage(),
            command,
            avp);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.ErrorAnswer, Is.Null);
        });
    }

    [Test]
    public void MissingFixedSizeAvpCreatesZeroFilledMinimumPayload()
    {
        DiameterDictionarySnapshot snapshot = CreateBaseSnapshot();
        DiameterCommandDefinition commandDefinition = CreateCommandDefinition();
        commandDefinition.Rules.Add(CreateRule(DiameterAvpCodes.SessionTimeout, 0U, 1, 1));
        snapshot.Commands.Add(commandDefinition);
        DiameterMessage message = CreateRequest();
        CommandValidationResult command = new CommandValidator(snapshot).ValidateCommand(message);

        DiameterValidationResult result = new ValidationResultBuilder(snapshot)
            .BuildValidationResult(message, command, null);
        DiameterAvp child = DecodeOnlyChild(result.ErrorAnswer!.FailedAvp!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Invalid));
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo("missing-required-avp"));
            Assert.That(result.Issues[0].ResultCode, Is.EqualTo(DiameterResultCodes.MissingAvp));
            Assert.That(result.ErrorAnswer!.ResultCode, Is.EqualTo(DiameterResultCodes.MissingAvp));
            Assert.That(result.ErrorAnswer.Request, Is.SameAs(message));
            Assert.That(result.ErrorAnswer.FailedAvp!.Code, Is.EqualTo(DiameterAvpCodes.FailedAvp));
            Assert.That(result.ErrorAnswer.FailedAvp.Flags, Is.EqualTo(DiameterAvpFlags.Mandatory));
            Assert.That(child.Code, Is.EqualTo(DiameterAvpCodes.SessionTimeout));
            Assert.That(child.Flags, Is.EqualTo(DiameterAvpFlags.Mandatory));
            Assert.That(child.Data, Is.EqualTo(new byte[4]));
        });
    }

    [Test]
    public void MissingVendorAvpIncludesVendorHeaderAndMinimumPayload()
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        DiameterAvpDefinition definition = CreateAvpDefinition(
            900U,
            10415U,
            "Vendor-Value",
            DiameterAvpBaseType.Unsigned32,
            4);
        definition.RequiredFlags =
            DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory;
        snapshot.Avps.Add(definition);
        DiameterCommandDefinition commandDefinition = CreateCommandDefinition();
        commandDefinition.Rules.Add(CreateRule(900U, 10415U, 1, 1));
        snapshot.Commands.Add(commandDefinition);
        DiameterMessage message = CreateRequest();
        CommandValidationResult command = new CommandValidator(snapshot).ValidateCommand(message);

        DiameterValidationResult result = new ValidationResultBuilder(snapshot)
            .BuildValidationResult(message, command, null);
        DiameterAvp child = DecodeOnlyChild(result.ErrorAnswer!.FailedAvp!);

        Assert.Multiple(() =>
        {
            Assert.That(child.Code, Is.EqualTo(900U));
            Assert.That(child.VendorId, Is.EqualTo(10415U));
            Assert.That(
                child.Flags,
                Is.EqualTo(DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory));
            Assert.That(child.Data, Is.EqualTo(new byte[4]));
        });
    }

    [Test]
    public void MissingCustomDerivedAvpUsesInheritedMinimumPayloadLength()
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        DiameterTypeDefinition type = new DiameterTypeDefinition();
        type.Name = "Custom-Address";
        type.BaseType = DiameterAvpBaseType.OctetString;
        type.ParentTypeName = "Address";
        snapshot.Types.Add(type);
        DiameterAvpDefinition definition = CreateAvpDefinition(
            900U,
            0U,
            "Custom-Address-AVP",
            DiameterAvpBaseType.OctetString,
            -1);
        definition.TypeName = "Custom-Address";
        snapshot.Avps.Add(definition);
        DiameterCommandDefinition commandDefinition = CreateCommandDefinition();
        commandDefinition.Rules.Add(CreateRule(900U, 0U, 1, 1));
        snapshot.Commands.Add(commandDefinition);
        DiameterMessage message = CreateRequest();
        CommandValidationResult command = new CommandValidator(snapshot).ValidateCommand(message);

        DiameterValidationResult result = new ValidationResultBuilder(snapshot)
            .BuildValidationResult(message, command, null);
        DiameterAvp child = DecodeOnlyChild(result.ErrorAnswer!.FailedAvp!);

        Assert.That(child.Data, Is.EqualTo(new byte[6]));
    }

    [Test]
    public void UnsupportedMandatoryAvpIsCopiedIntoFailedAvp()
    {
        DiameterDictionarySnapshot snapshot = CreateBaseSnapshot();
        DiameterMessage message = new DiameterMessage();
        DiameterAvp offending = CreateAvp(
            999U,
            DiameterAvpFlags.Mandatory,
            new byte[] { 1, 2, 3 });
        message.Avps.Add(offending);
        AvpValidationResult avps = new AvpValidator(snapshot, null)
            .ValidateMessageAvps(message);

        DiameterValidationResult result = new ValidationResultBuilder(snapshot)
            .BuildValidationResult(message, null, avps);
        DiameterAvp child = DecodeOnlyChild(result.ErrorAnswer!.FailedAvp!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues[0].ResultCode, Is.EqualTo(DiameterResultCodes.AvpUnsupported));
            Assert.That(result.Issues[0].OffendingAvp, Is.SameAs(offending));
            Assert.That(child.Code, Is.EqualTo(999U));
            Assert.That(child.Flags, Is.EqualTo(DiameterAvpFlags.Mandatory));
            Assert.That(child.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void InvalidSizeMapsToInvalidLengthAndCopiesTheMalformedValue()
    {
        DiameterDictionarySnapshot snapshot = CreateBaseSnapshot();
        DiameterAvp offending = CreateAvp(
            DiameterAvpCodes.SessionTimeout,
            DiameterAvpFlags.Mandatory,
            new byte[] { 1, 2, 3 });
        AvpValidationResult avps = new AvpValidator(snapshot, null)
            .ValidateSingleAvp(offending);

        DiameterValidationResult result = new ValidationResultBuilder(snapshot)
            .BuildValidationResult(null, null, avps);
        DiameterAvp child = DecodeOnlyChild(result.ErrorAnswer!.FailedAvp!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues[0].ResultCode, Is.EqualTo(DiameterResultCodes.InvalidAvpLength));
            Assert.That(result.Issues[0].ExpectedLength, Is.EqualTo(4));
            Assert.That(result.Issues[0].ActualLength, Is.EqualTo(3));
            Assert.That(child.Code, Is.EqualTo(DiameterAvpCodes.SessionTimeout));
            Assert.That(child.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void InvalidDerivedValueMapsToInvalidAvpValue()
    {
        DiameterDictionarySnapshot snapshot = CreateBaseSnapshot();
        DiameterAvp offending = CreateAvp(
            DiameterAvpCodes.OriginHost,
            DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("bad host"));
        AvpValidationResult avps = new AvpValidator(snapshot, null)
            .ValidateSingleAvp(offending);

        DiameterValidationResult result = new ValidationResultBuilder(snapshot)
            .BuildValidationResult(null, null, avps);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues[0].Code, Is.EqualTo("invalid-derived-avp-value"));
            Assert.That(result.Issues[0].ResultCode, Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(result.ErrorAnswer!.ResultCode, Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(result.ErrorAnswer.FailedAvp, Is.Not.Null);
        });
    }

    [Test]
    public void ExcessOccurrenceCopiesFirstAvpBeyondMaximum()
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        DiameterCommandDefinition commandDefinition = CreateCommandDefinition();
        commandDefinition.Rules.Add(CreateRule(DiameterAvpCodes.UserName, 0U, 0, 1));
        snapshot.Commands.Add(commandDefinition);
        DiameterMessage message = CreateRequest();
        message.Avps.Add(CreateAvp(1U, DiameterAvpFlags.None, new byte[] { 1 }));
        message.Avps.Add(CreateAvp(1U, DiameterAvpFlags.None, new byte[] { 2 }));
        message.Avps.Add(CreateAvp(1U, DiameterAvpFlags.None, new byte[] { 3 }));
        CommandValidationResult command = new CommandValidator(snapshot).ValidateCommand(message);

        DiameterValidationResult result = new ValidationResultBuilder(snapshot)
            .BuildValidationResult(message, command, null);
        DiameterAvp child = DecodeOnlyChild(result.ErrorAnswer!.FailedAvp!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues[0].ResultCode, Is.EqualTo(DiameterResultCodes.AvpOccursTooManyTimes));
            Assert.That(result.Issues[0].AvpIndex, Is.EqualTo(1));
            Assert.That(result.Issues[0].ActualCount, Is.EqualTo(3));
            Assert.That(child.Data, Is.EqualTo(new byte[] { 2 }));
        });
    }

    [Test]
    public void MissingNestedAvpPreservesImmediateGroupedHierarchy()
    {
        DiameterDictionarySnapshot snapshot = CreateBaseSnapshot();
        DiameterAvp proxyHost = CreateAvp(
            DiameterAvpCodes.ProxyHost,
            DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("proxy.example"));
        DiameterAvp proxyInfo = CreateAvp(
            DiameterAvpCodes.ProxyInfo,
            DiameterAvpFlags.Mandatory,
            EncodeAvp(proxyHost));
        AvpValidationResult avps = new AvpValidator(snapshot, null)
            .ValidateSingleAvp(proxyInfo);

        DiameterValidationResult result = new ValidationResultBuilder(snapshot)
            .BuildValidationResult(null, null, avps);
        DiameterAvp parent = DecodeOnlyChild(result.ErrorAnswer!.FailedAvp!);
        DiameterValueResult nested = AvpDecoder.DecodeAvpSequence(parent.Data);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues[0].ResultCode, Is.EqualTo(DiameterResultCodes.MissingAvp));
            Assert.That(result.Issues[0].ParentAvpCode, Is.EqualTo(DiameterAvpCodes.ProxyInfo));
            Assert.That(parent.Code, Is.EqualTo(DiameterAvpCodes.ProxyInfo));
            Assert.That(nested.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(nested.Avps, Has.Count.EqualTo(1));
            Assert.That(nested.Avps[0].Code, Is.EqualTo(DiameterAvpCodes.ProxyState));
            Assert.That(nested.Avps[0].Data, Is.Empty);
        });
    }

    [Test]
    public void FirstIssueAloneDrivesTheErrorAnswerRequest()
    {
        DiameterDictionarySnapshot snapshot = CreateBaseSnapshot();
        CommandValidationResult command = new CommandValidationResult();
        CommandValidationFailure first = new CommandValidationFailure();
        first.Code = "unknown-command";
        command.Failures.Add(first);
        CommandValidationFailure second = new CommandValidationFailure();
        second.Code = "missing-required-avp";
        second.AvpCode = DiameterAvpCodes.SessionTimeout;
        command.Failures.Add(second);

        DiameterValidationResult result = new ValidationResultBuilder(snapshot)
            .BuildValidationResult(new DiameterMessage(), command, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues, Has.Count.EqualTo(2));
            Assert.That(result.ErrorAnswer!.Issue, Is.SameAs(result.Issues[0]));
            Assert.That(result.ErrorAnswer.ResultCode, Is.EqualTo(DiameterResultCodes.CommandUnsupported));
            Assert.That(result.ErrorAnswer.FailedAvp, Is.Null);
            Assert.That(result.Issues[1].FailedAvp, Is.Not.Null);
        });
    }

    [Test]
    public void PublicIssuesRetainExpectedAndActualHeaderAndAvpFlags()
    {
        DiameterDictionarySnapshot commandSnapshot =
            new DiameterDictionarySnapshot();
        DiameterCommandDefinition commandDefinition =
            CreateCommandDefinition();
        commandDefinition.RequiredFlags =
            DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        commandDefinition.ProhibitedFlags =
            DiameterMessageFlags.Error;
        commandSnapshot.Commands.Add(commandDefinition);
        DiameterMessage message = CreateRequest();
        message.Flags =
            DiameterMessageFlags.Request |
            DiameterMessageFlags.Error;
        CommandValidationResult command =
            new CommandValidator(commandSnapshot)
                .ValidateCommand(message);
        DiameterValidationResult commandResult =
            new ValidationResultBuilder(commandSnapshot)
                .BuildValidationResult(message, command, null);

        DiameterDictionarySnapshot avpSnapshot =
            new DiameterDictionarySnapshot();
        DiameterAvpDefinition avpDefinition = CreateAvpDefinition(
            900U,
            0U,
            "Flagged",
            DiameterAvpBaseType.OctetString,
            -1);
        avpDefinition.RequiredFlags = DiameterAvpFlags.Mandatory;
        avpDefinition.ProhibitedFlags = DiameterAvpFlags.Protected;
        avpSnapshot.Avps.Add(avpDefinition);
        DiameterAvp avp = CreateAvp(
            900U,
            DiameterAvpFlags.Protected,
            new byte[0]);
        AvpValidationResult avpValidation =
            new AvpValidator(avpSnapshot, new DiameterLimitConfiguration())
                .ValidateSingleAvp(avp);
        DiameterValidationResult avpResult =
            new ValidationResultBuilder(avpSnapshot)
                .BuildValidationResult(null, null, avpValidation);

        Assert.Multiple(() =>
        {
            Assert.That(commandResult.Issues, Has.Count.EqualTo(2));
            Assert.That(commandResult.Issues[0].ExpectedMessageFlags,
                Is.EqualTo(
                    DiameterMessageFlags.Request |
                    DiameterMessageFlags.Proxyable));
            Assert.That(commandResult.Issues[0].ActualMessageFlags,
                Is.EqualTo(message.Flags));
            Assert.That(commandResult.Issues[1].ExpectedMessageFlags,
                Is.EqualTo(DiameterMessageFlags.Error));
            Assert.That(commandResult.Issues[1].ActualMessageFlags,
                Is.EqualTo(message.Flags));
            Assert.That(avpResult.Issues, Has.Count.EqualTo(2));
            Assert.That(avpResult.Issues[0].ExpectedAvpFlags,
                Is.EqualTo(DiameterAvpFlags.Mandatory));
            Assert.That(avpResult.Issues[0].ActualAvpFlags,
                Is.EqualTo(DiameterAvpFlags.Protected));
            Assert.That(avpResult.Issues[1].ExpectedAvpFlags,
                Is.EqualTo(DiameterAvpFlags.Protected));
            Assert.That(avpResult.Issues[1].ActualAvpFlags,
                Is.EqualTo(DiameterAvpFlags.Protected));
        });
    }

    private static DiameterDictionarySnapshot CreateBaseSnapshot()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        Assert.That(
            builder.RegisterDictionaryModule(Rfc6733Dictionary.CreateModule()).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        return builder.ReadDictionarySnapshot();
    }

    private static DiameterCommandDefinition CreateCommandDefinition()
    {
        DiameterCommandDefinition command = new DiameterCommandDefinition();
        command.ApplicationId = 4U;
        command.Code = 272U;
        command.Name = "Test";
        command.IsRequest = true;
        command.RequiredFlags = DiameterMessageFlags.Request;
        return command;
    }

    private static DiameterAvpRuleDefinition CreateRule(
        uint code,
        uint vendorId,
        int minimum,
        int maximum)
    {
        DiameterAvpRuleDefinition rule = new DiameterAvpRuleDefinition();
        rule.AvpCode = code;
        rule.AvpVendorId = vendorId;
        rule.AvpName = "Test-AVP";
        rule.MinimumCount = minimum;
        rule.MaximumCount = maximum;
        return rule;
    }

    private static DiameterAvpDefinition CreateAvpDefinition(
        uint code,
        uint vendorId,
        string name,
        DiameterAvpBaseType baseType,
        int fixedLength)
    {
        DiameterAvpDefinition definition = new DiameterAvpDefinition();
        definition.Code = code;
        definition.VendorId = vendorId;
        definition.Name = name;
        definition.TypeName = baseType.ToString();
        definition.BaseType = baseType;
        definition.FixedDataLength = fixedLength;
        return definition;
    }

    private static DiameterMessage CreateRequest()
    {
        DiameterMessage message = new DiameterMessage();
        message.ApplicationId = 4U;
        message.CommandCode = 272U;
        message.Flags = DiameterMessageFlags.Request;
        return message;
    }

    private static DiameterAvp CreateAvp(
        uint code,
        DiameterAvpFlags flags,
        byte[] data)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = flags;
        avp.Data = data;
        return avp;
    }

    private static byte[] EncodeAvp(DiameterAvp avp)
    {
        DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avp);
        Assert.That(measurement.Status, Is.EqualTo(DiameterCodecStatus.Success));
        byte[] data = new byte[measurement.RequiredCount];
        DiameterEncodeResult encoded = AvpEncoder.EncodeAvp(data, avp);
        Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return data;
    }

    private static DiameterAvp DecodeOnlyChild(DiameterAvp failedAvp)
    {
        Assert.That(failedAvp.Code, Is.EqualTo(DiameterAvpCodes.FailedAvp));
        DiameterValueResult decoded = AvpDecoder.DecodeAvpSequence(failedAvp.Data);
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(decoded.Avps, Has.Count.EqualTo(1));
        return decoded.Avps[0];
    }
}
