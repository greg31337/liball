using System.Text;

namespace diameter.Tests;

public class BuiltInValidationMatrixTests
{
    [Test]
    public void EveryBuiltInCommandAcceptsMinimalAndMaximalValidPermutations()
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionary()
            .ReadDictionarySnapshot();
        IDiameterValidator validator = new DiameterValidator();
        int commandCount = 0;
        int moduleIndex = 0;
        while (moduleIndex < snapshot.Modules.Count)
        {
            DiameterDictionaryModule module = snapshot.Modules[moduleIndex];
            int commandIndex = 0;
            while (commandIndex < module.Commands.Count)
            {
                DiameterCommandDefinition command = module.Commands[commandIndex];
                DiameterMessage minimal = CreateValidMessage(snapshot, command, false);
                DiameterMessage maximal = CreateValidMessage(snapshot, command, true);
                DiameterValidationResult minimalResult = validator.ValidateMessage(
                    minimal,
                    snapshot,
                    new DiameterLimitConfiguration());
                DiameterValidationResult maximalResult = validator.ValidateMessage(
                    maximal,
                    snapshot,
                    new DiameterLimitConfiguration());

                Assert.Multiple(() =>
                {
                    Assert.That(minimalResult.Status,
                        Is.EqualTo(DiameterValidationStatus.Valid),
                        module.Name + "/" + command.Name + "/minimal");
                    Assert.That(minimalResult.Issues, Is.Empty,
                        module.Name + "/" + command.Name + "/minimal");
                    Assert.That(maximalResult.Status,
                        Is.EqualTo(DiameterValidationStatus.Valid),
                        module.Name + "/" + command.Name + "/maximal");
                    Assert.That(maximalResult.Issues, Is.Empty,
                        module.Name + "/" + command.Name + "/maximal");
                });
                commandCount = commandCount + 1;
                commandIndex = commandIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        Assert.That(commandCount, Is.EqualTo(20));
    }

    [Test]
    public void EveryBuiltInGroupedAvpAcceptsMinimalAndMaximalValidPermutations()
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionary()
            .ReadDictionarySnapshot();
        IDiameterValidator validator = new DiameterValidator();
        int groupedCount = 0;
        int moduleIndex = 0;
        while (moduleIndex < snapshot.Modules.Count)
        {
            DiameterDictionaryModule module = snapshot.Modules[moduleIndex];
            int avpIndex = 0;
            while (avpIndex < module.Avps.Count)
            {
                DiameterAvpDefinition definition = module.Avps[avpIndex];
                if (definition.BaseType == DiameterAvpBaseType.Grouped)
                {
                    DiameterAvp minimal = CreateValidAvp(snapshot, definition, false, 0);
                    DiameterAvp maximal = CreateValidAvp(snapshot, definition, true, 0);
                    DiameterValidationResult minimalResult = validator.ValidateGroupedAvp(
                        minimal,
                        snapshot,
                        new DiameterLimitConfiguration());
                    DiameterValidationResult maximalResult = validator.ValidateGroupedAvp(
                        maximal,
                        snapshot,
                        new DiameterLimitConfiguration());

                    Assert.Multiple(() =>
                    {
                        Assert.That(minimalResult.Status,
                            Is.EqualTo(DiameterValidationStatus.Valid),
                            module.Name + "/" + definition.Name + "/minimal");
                        Assert.That(minimalResult.Issues, Is.Empty,
                            module.Name + "/" + definition.Name + "/minimal");
                        Assert.That(maximalResult.Status,
                            Is.EqualTo(DiameterValidationStatus.Valid),
                            module.Name + "/" + definition.Name + "/maximal");
                        Assert.That(maximalResult.Issues, Is.Empty,
                            module.Name + "/" + definition.Name + "/maximal");
                    });
                    groupedCount = groupedCount + 1;
                }

                avpIndex = avpIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        Assert.That(groupedCount, Is.EqualTo(24));
    }

    [TestCase("unknown-command", 3001U, 0U)]
    [TestCase("application-id-mismatch", 3007U, 0U)]
    [TestCase("prohibited-header-flags-present", 5013U, 0U)]
    [TestCase("required-header-flags-missing", 5013U, 0U)]
    [TestCase("missing-required-avp", 5005U, 296U)]
    [TestCase("too-many-avp-occurrences", 5009U, 264U)]
    [TestCase("fixed-head-avp-misordered", 5008U, 263U)]
    [TestCase("avp-ordering-violation", 5008U, 901U)]
    [TestCase("avp-not-allowed", 5008U, 903U)]
    [TestCase("unsupported-mandatory-avp", 5001U, 999U)]
    [TestCase("invalid-derived-avp-value", 5004U, 264U)]
    [TestCase("unknown-enumerated-value", 5004U, 273U)]
    [TestCase("invalid-avp-size", 5014U, 278U)]
    [TestCase("required-avp-flags-missing", 5016U, 264U)]
    [TestCase("prohibited-avp-flags-present", 5016U, 281U)]
    public void CommandAndValueFailuresProduceExactResultCodeAndFailedAvp(
        string failureCode,
        uint expectedResultCode,
        uint expectedFailedCode)
    {
        ValidationCase validationCase = CreateValidationCase(failureCode);
        IDiameterValidator validator = new DiameterValidator();

        DiameterValidationResult result = validator.ValidateMessage(
            validationCase.Message,
            validationCase.Snapshot,
            new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Invalid));
            Assert.That(result.Issues[0].Code, Is.EqualTo(failureCode));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Error));
            Assert.That(result.Issues[0].ResultCode, Is.EqualTo(expectedResultCode));
            Assert.That(result.ErrorAnswer, Is.Not.Null);
            Assert.That(result.ErrorAnswer!.Issue, Is.SameAs(result.Issues[0]));
            Assert.That(result.ErrorAnswer.ResultCode, Is.EqualTo(expectedResultCode));
            Assert.That(result.ErrorAnswer.Request, Is.SameAs(validationCase.Message));
        });

        if (expectedFailedCode == 0U)
        {
            Assert.That(result.Issues[0].FailedAvp, Is.Null);
            Assert.That(result.ErrorAnswer!.FailedAvp, Is.Null);
        }
        else
        {
            DiameterAvp failed = DecodeFailedChild(result.ErrorAnswer!.FailedAvp!);
            Assert.Multiple(() =>
            {
                Assert.That(failed.Code, Is.EqualTo(expectedFailedCode));
                Assert.That(result.Issues[0].FailedAvp,
                    Is.SameAs(result.ErrorAnswer.FailedAvp));
                Assert.That(failed.Data, Is.EqualTo(validationCase.ExpectedFailedData));
            });
        }
    }

    [TestCase("missing-required-grouped-avp", 5005U, 902U)]
    [TestCase("too-many-grouped-avp-occurrences", 5009U, 902U)]
    [TestCase("fixed-head-grouped-avp-misordered", 5008U, 901U)]
    [TestCase("grouped-avp-ordering-violation", 5008U, 901U)]
    [TestCase("grouped-avp-not-allowed", 5008U, 903U)]
    public void GroupedGrammarFailuresProduceExactNestedFailedAvp(
        string failureCode,
        uint expectedResultCode,
        uint expectedChildCode)
    {
        GroupedValidationCase validationCase = CreateGroupedValidationCase(failureCode);
        IDiameterValidator validator = new DiameterValidator();

        DiameterValidationResult result = validator.ValidateGroupedAvp(
            validationCase.Avp,
            validationCase.Snapshot,
            new DiameterLimitConfiguration());
        DiameterAvp failedParent = DecodeFailedChild(result.ErrorAnswer!.FailedAvp!);
        DiameterAvp failedChild = DecodeOnlyAvp(failedParent.Data);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Invalid));
            Assert.That(result.Issues[0].Code, Is.EqualTo(failureCode));
            Assert.That(result.Issues[0].ResultCode, Is.EqualTo(expectedResultCode));
            Assert.That(result.Issues[0].ParentAvpCode, Is.EqualTo(900U));
            Assert.That(result.ErrorAnswer.ResultCode, Is.EqualTo(expectedResultCode));
            Assert.That(failedParent.Code, Is.EqualTo(900U));
            Assert.That(failedChild.Code, Is.EqualTo(expectedChildCode));
            Assert.That(failedChild.Data, Is.EqualTo(validationCase.ExpectedFailedData));
        });
    }

    [TestCase(257U, "Host-IP-Address", "unsupported-address-family")]
    [TestCase(1U, "User-Name", "invalid-utf8")]
    [TestCase(264U, "Origin-Host", "invalid-diameter-identity")]
    [TestCase(292U, "Redirect-Host", "invalid-uri-scheme")]
    [TestCase(438U, "Restriction-Filter-Rule", "invalid-ip-filter-rule")]
    [TestCase(509U, "Filter-Rule", "invalid-qos-filter-rule")]
    public void EveryDerivedInvalidValueMapsToInvalidAvpValueAndCopiesPayload(
        uint avpCode,
        string avpName,
        string expectedValueCode)
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionary()
            .ReadDictionarySnapshot();
        DiameterAvpDefinition definition = FindAvpDefinition(snapshot, 0U, avpCode);
        DiameterAvp avp = new DiameterAvp();
        avp.Code = avpCode;
        avp.Flags = definition.RequiredFlags;
        if (avpCode == DiameterAvpCodes.HostIpAddress)
        {
            avp.Data = new byte[] { 0, 3 };
        }
        else if (avpCode == DiameterAvpCodes.UserName)
        {
            avp.Data = new byte[] { 255 };
        }
        else if (avpCode == DiameterAvpCodes.OriginHost)
        {
            avp.Data = Encoding.ASCII.GetBytes("bad host");
        }
        else if (avpCode == DiameterAvpCodes.RedirectHost)
        {
            avp.Data = Encoding.ASCII.GetBytes("http://host.example");
        }
        else
        {
            avp.Data = Encoding.ASCII.GetBytes("invalid filter");
        }

        DiameterValidationResult result = new DiameterValidator().ValidateGroupedAvp(
            avp,
            snapshot,
            new DiameterLimitConfiguration());
        DiameterAvp failed = DecodeFailedChild(result.ErrorAnswer!.FailedAvp!);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Name, Is.EqualTo(avpName));
            Assert.That(result.Issues[0].Code, Is.EqualTo("invalid-derived-avp-value"));
            Assert.That(result.Issues[0].ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(result.Issues[0].Message,
                Is.EqualTo("A Diameter AVP contains an invalid value."));
            Assert.That(result.ErrorAnswer.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(result.Issues[0].OffendingAvp, Is.SameAs(avp));
            Assert.That(failed.Code, Is.EqualTo(avpCode));
            Assert.That(failed.Data, Is.EqualTo(avp.Data));
        });

        AvpValidationResult internalResult = new AvpValidator(snapshot, null)
            .ValidateSingleAvp(avp);
        Assert.That(internalResult.Failures[0].ValueCode, Is.EqualTo(expectedValueCode));
    }

    [Test]
    public void FailedAvpWildcardAcceptsAnyContainedStandardOrVendorAvp()
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionary()
            .ReadDictionarySnapshot();
        DiameterAvpDefinition failedDefinition = FindAvpDefinition(
            snapshot,
            0U,
            DiameterAvpCodes.FailedAvp);
        DiameterAvp standard = new DiameterAvp();
        standard.Code = 999U;
        standard.Data = new byte[] { 1 };
        DiameterAvp vendor = new DiameterAvp();
        vendor.Code = 1000U;
        vendor.VendorId = 424242U;
        vendor.Flags = DiameterAvpFlags.VendorSpecific;
        vendor.Data = new byte[] { 2 };
        DiameterAvp failed = new DiameterAvp();
        failed.Code = DiameterAvpCodes.FailedAvp;
        failed.Flags = failedDefinition.RequiredFlags;
        failed.Data = EncodeAvps(new List<DiameterAvp> { standard, vendor });

        DiameterValidationResult result = new DiameterValidator().ValidateGroupedAvp(
            failed,
            snapshot,
            new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.ErrorAnswer, Is.Null);
        });
    }

    private static ValidationCase CreateValidationCase(string failureCode)
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionary()
            .ReadDictionarySnapshot();
        if (failureCode == "unknown-command")
        {
            DiameterMessage message = CreateWatchdog(true);
            message.CommandCode = 999999U;
            return CreateCase(snapshot, message, new byte[0]);
        }

        if (failureCode == "application-id-mismatch")
        {
            DiameterMessage message = CreateWatchdog(true);
            message.ApplicationId = DiameterApplicationIds.CreditControl;
            return CreateCase(snapshot, message, new byte[0]);
        }

        if (failureCode == "prohibited-header-flags-present")
        {
            DiameterMessage message = CreateWatchdog(true);
            message.Flags = message.Flags | DiameterMessageFlags.Error;
            return CreateCase(snapshot, message, new byte[0]);
        }

        if (failureCode == "required-header-flags-missing")
        {
            DiameterMessage message = CreateReAuthRequest();
            message.Flags = DiameterMessageFlags.Request;
            return CreateCase(snapshot, message, new byte[0]);
        }

        if (failureCode == "missing-required-avp")
        {
            return CreateCase(snapshot, CreateWatchdog(false), new byte[0]);
        }

        if (failureCode == "too-many-avp-occurrences")
        {
            DiameterMessage message = CreateWatchdog(true);
            byte[] data = Encoding.ASCII.GetBytes("second.example");
            message.Avps.Add(CreateAvp(264U, DiameterAvpFlags.Mandatory, data));
            return CreateCase(snapshot, message, data);
        }

        if (failureCode == "fixed-head-avp-misordered")
        {
            DiameterMessage message = CreateReAuthRequest();
            DiameterAvp session = message.Avps[0];
            message.Avps.RemoveAt(0);
            message.Avps.Insert(1, session);
            return CreateCase(snapshot, message, session.Data);
        }

        if (failureCode == "avp-ordering-violation" ||
            failureCode == "avp-not-allowed")
        {
            return CreateCustomCommandCase(failureCode);
        }

        if (failureCode == "unsupported-mandatory-avp")
        {
            DiameterMessage message = CreateWatchdog(true);
            byte[] data = new byte[] { 9 };
            message.Avps.Add(CreateAvp(999U, DiameterAvpFlags.Mandatory, data));
            return CreateCase(snapshot, message, data);
        }

        if (failureCode == "invalid-derived-avp-value")
        {
            DiameterMessage message = CreateWatchdog(true);
            byte[] data = Encoding.ASCII.GetBytes("bad host");
            message.Avps[0].Data = data;
            return CreateCase(snapshot, message, data);
        }

        if (failureCode == "unknown-enumerated-value")
        {
            DiameterMessage message = CreateDisconnectRequest();
            byte[] data = EncodeInt32(99);
            message.Avps[2].Data = data;
            return CreateCase(snapshot, message, data);
        }

        if (failureCode == "invalid-avp-size")
        {
            DiameterMessage message = CreateWatchdog(true);
            byte[] data = new byte[] { 1, 2, 3 };
            message.Avps.Add(CreateAvp(278U, DiameterAvpFlags.Mandatory, data));
            return CreateCase(snapshot, message, data);
        }

        if (failureCode == "required-avp-flags-missing")
        {
            DiameterMessage message = CreateWatchdog(true);
            message.Avps[0].Flags = DiameterAvpFlags.None;
            return CreateCase(snapshot, message, message.Avps[0].Data);
        }

        DiameterMessage prohibited = CreateWatchdog(true);
        byte[] prohibitedData = Encoding.ASCII.GetBytes("error");
        prohibited.Avps.Add(CreateAvp(
            DiameterAvpCodes.ErrorMessage,
            DiameterAvpFlags.Mandatory,
            prohibitedData));
        return CreateCase(snapshot, prohibited, prohibitedData);
    }

    private static ValidationCase CreateCustomCommandCase(string failureCode)
    {
        DiameterDictionarySnapshot snapshot = CreateCustomSnapshot(false);
        DiameterMessage message = new DiameterMessage();
        message.CommandCode = 900U;
        message.ApplicationId = 900U;
        message.Flags = DiameterMessageFlags.Request;
        DiameterAvp first = CreateAvp(901U, DiameterAvpFlags.None, new byte[] { 1 });
        DiameterAvp second = CreateAvp(902U, DiameterAvpFlags.None, new byte[] { 2 });
        if (failureCode == "avp-ordering-violation")
        {
            message.Avps.Add(second);
            message.Avps.Add(first);
            return CreateCase(snapshot, message, first.Data);
        }

        message.Avps.Add(first);
        message.Avps.Add(second);
        DiameterAvp unlisted = CreateAvp(903U, DiameterAvpFlags.None, new byte[] { 3 });
        message.Avps.Add(unlisted);
        return CreateCase(snapshot, message, unlisted.Data);
    }

    private static GroupedValidationCase CreateGroupedValidationCase(string failureCode)
    {
        bool fixedHead = failureCode == "fixed-head-grouped-avp-misordered";
        DiameterDictionarySnapshot snapshot = CreateCustomSnapshot(fixedHead);
        DiameterAvp first = CreateAvp(901U, DiameterAvpFlags.None, new byte[] { 1 });
        DiameterAvp second = CreateAvp(902U, DiameterAvpFlags.None, new byte[] { 2 });
        List<DiameterAvp> children = new List<DiameterAvp>();
        byte[] expected = new byte[0];
        if (failureCode == "missing-required-grouped-avp")
        {
            children.Add(first);
        }
        else if (failureCode == "too-many-grouped-avp-occurrences")
        {
            children.Add(first);
            children.Add(second);
            DiameterAvp excess = CreateAvp(902U, DiameterAvpFlags.None, new byte[] { 9 });
            children.Add(excess);
            expected = excess.Data;
        }
        else if (failureCode == "fixed-head-grouped-avp-misordered" ||
            failureCode == "grouped-avp-ordering-violation")
        {
            children.Add(second);
            children.Add(first);
            expected = first.Data;
        }
        else
        {
            children.Add(first);
            children.Add(second);
            DiameterAvp unlisted = CreateAvp(903U, DiameterAvpFlags.None, new byte[] { 3 });
            children.Add(unlisted);
            expected = unlisted.Data;
        }

        DiameterAvp grouped = CreateAvp(900U, DiameterAvpFlags.None, EncodeAvps(children));
        GroupedValidationCase result = new GroupedValidationCase();
        result.Snapshot = snapshot;
        result.Avp = grouped;
        result.ExpectedFailedData = expected;
        return result;
    }

    private static DiameterDictionarySnapshot CreateCustomSnapshot(bool fixedHead)
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        snapshot.Avps.Add(CreateDefinition(900U, "Group", DiameterAvpBaseType.Grouped));
        snapshot.Avps.Add(CreateDefinition(901U, "First", DiameterAvpBaseType.OctetString));
        snapshot.Avps.Add(CreateDefinition(902U, "Second", DiameterAvpBaseType.OctetString));
        DiameterAvpDefinition grouped = snapshot.Avps[0];
        grouped.AllowUnlistedAvps = false;
        DiameterAvpRuleDefinition first = CreateRule(901U, "First", 1, 1);
        first.OrderingGroup = 0;
        if (fixedHead)
        {
            first.FixedHeadPosition = 0;
            first.OrderingGroup = -1;
        }

        DiameterAvpRuleDefinition second = CreateRule(902U, "Second", 1, 1);
        second.OrderingGroup = 1;
        if (fixedHead)
        {
            second.OrderingGroup = -1;
        }

        grouped.GroupedRules.Add(first);
        grouped.GroupedRules.Add(second);
        DiameterCommandDefinition command = new DiameterCommandDefinition();
        command.Code = 900U;
        command.ApplicationId = 900U;
        command.Name = "Test";
        command.IsRequest = true;
        command.RequiredFlags = DiameterMessageFlags.Request;
        command.AllowUnlistedAvps = false;
        command.Rules.Add(first);
        command.Rules.Add(second);
        snapshot.Commands.Add(command);
        return snapshot;
    }

    private static DiameterMessage CreateValidMessage(
        DiameterDictionarySnapshot snapshot,
        DiameterCommandDefinition command,
        bool maximal)
    {
        DiameterMessage message = new DiameterMessage();
        message.CommandCode = command.Code;
        message.ApplicationId = command.ApplicationId;
        message.Flags = command.RequiredFlags;
        int ruleIndex = 0;
        while (ruleIndex < command.Rules.Count)
        {
            DiameterAvpRuleDefinition rule = command.Rules[ruleIndex];
            int count = rule.MinimumCount;
            if (maximal && count == 0)
            {
                count = 1;
            }

            if (maximal && rule.MaximumCount < 0)
            {
                count = 2;
            }

            int occurrence = 0;
            while (occurrence < count)
            {
                DiameterAvpDefinition definition = FindAvpDefinition(
                    snapshot,
                    rule.AvpVendorId,
                    rule.AvpCode);
                message.Avps.Add(CreateValidAvp(snapshot, definition, maximal, 0));
                occurrence = occurrence + 1;
            }

            ruleIndex = ruleIndex + 1;
        }

        if (maximal && command.AllowUnlistedAvps)
        {
            message.Avps.Add(CreateAvp(16000000U, DiameterAvpFlags.None, new byte[] { 1 }));
        }

        return message;
    }

    private static DiameterAvp CreateValidAvp(
        DiameterDictionarySnapshot snapshot,
        DiameterAvpDefinition definition,
        bool maximal,
        int depth)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = definition.Code;
        avp.VendorId = definition.VendorId;
        avp.Flags = definition.RequiredFlags;
        if (definition.VendorId != 0U)
        {
            avp.Flags = avp.Flags | DiameterAvpFlags.VendorSpecific;
        }

        string valueType = ResolveValueType(snapshot, definition);
        if (definition.BaseType == DiameterAvpBaseType.Grouped || valueType == "Grouped")
        {
            List<DiameterAvp> children = new List<DiameterAvp>();
            int ruleIndex = 0;
            while (ruleIndex < definition.GroupedRules.Count)
            {
                DiameterAvpRuleDefinition rule = definition.GroupedRules[ruleIndex];
                int count = rule.MinimumCount;
                if (maximal && count == 0)
                {
                    count = 1;
                }

                if (maximal && rule.MaximumCount < 0)
                {
                    count = 2;
                }

                int occurrence = 0;
                while (occurrence < count)
                {
                    if (rule.AvpCode == 0U && rule.AvpName == "AVP")
                    {
                        children.Add(CreateAvp(
                            15000000U + (uint)depth,
                            DiameterAvpFlags.None,
                            new byte[] { 7 }));
                    }
                    else
                    {
                        DiameterAvpDefinition childDefinition = FindAvpDefinition(
                            snapshot,
                            rule.AvpVendorId,
                            rule.AvpCode);
                        children.Add(CreateValidAvp(
                            snapshot,
                            childDefinition,
                            maximal,
                            depth + 1));
                    }

                    occurrence = occurrence + 1;
                }

                ruleIndex = ruleIndex + 1;
            }

            if (maximal && definition.AllowUnlistedAvps &&
                !HasWildcardRule(definition))
            {
                children.Add(CreateAvp(
                    14000000U + (uint)depth,
                    DiameterAvpFlags.None,
                    new byte[] { 8 }));
            }

            avp.Data = EncodeAvps(children);
            return avp;
        }

        avp.Data = CreateValidValue(snapshot, definition, valueType);
        return avp;
    }

    private static byte[] CreateValidValue(
        DiameterDictionarySnapshot snapshot,
        DiameterAvpDefinition definition,
        string valueType)
    {
        if (valueType == "Address")
        {
            return new byte[] { 0, 1, 192, 0, 2, 1 };
        }

        if (valueType == "DiameterIdentity")
        {
            return Encoding.ASCII.GetBytes("node.example");
        }

        if (valueType == "DiameterURI")
        {
            return Encoding.ASCII.GetBytes("aaa://node.example");
        }

        if (valueType == "UTF8String")
        {
            return Encoding.UTF8.GetBytes("value");
        }

        if (valueType == "IPFilterRule")
        {
            return Encoding.ASCII.GetBytes("permit out ip from any to any");
        }

        if (valueType == "QoSFilterRule")
        {
            return Encoding.ASCII.GetBytes("tag in ip from any to assigned DSCP 46");
        }

        if (valueType == "Enumerated")
        {
            return EncodeInt32(FindFirstEnumValue(snapshot, definition));
        }

        if (definition.BaseType == DiameterAvpBaseType.Integer32 ||
            definition.BaseType == DiameterAvpBaseType.Unsigned32 ||
            definition.BaseType == DiameterAvpBaseType.Float32 || valueType == "Time")
        {
            return new byte[4];
        }

        if (definition.BaseType == DiameterAvpBaseType.Integer64 ||
            definition.BaseType == DiameterAvpBaseType.Unsigned64 ||
            definition.BaseType == DiameterAvpBaseType.Float64)
        {
            return new byte[8];
        }

        return new byte[] { 1 };
    }

    private static int FindFirstEnumValue(
        DiameterDictionarySnapshot snapshot,
        DiameterAvpDefinition definition)
    {
        int index = 0;
        while (index < snapshot.EnumValues.Count)
        {
            DiameterEnumValueDefinition value = snapshot.EnumValues[index];
            if (value.AvpCode == definition.Code &&
                value.AvpVendorId == definition.VendorId)
            {
                return value.Value;
            }

            index = index + 1;
        }

        index = 0;
        while (index < snapshot.EnumValues.Count)
        {
            DiameterEnumValueDefinition value = snapshot.EnumValues[index];
            if (value.TypeName == definition.TypeName &&
                value.TypeVendorId == definition.VendorId)
            {
                return value.Value;
            }

            index = index + 1;
        }

        Assert.Fail("Expected enum value for " + definition.Name + ".");
        return 0;
    }

    private static string ResolveValueType(
        DiameterDictionarySnapshot snapshot,
        DiameterAvpDefinition definition)
    {
        string name = definition.TypeName;
        uint vendorId = definition.VendorId;
        int remaining = snapshot.Types.Count + 1;
        while (remaining > 0)
        {
            if (IsKnownValueType(name))
            {
                return name;
            }

            DiameterTypeDefinition? type = FindType(snapshot, vendorId, name);
            if (type == null && vendorId != 0U)
            {
                type = FindType(snapshot, 0U, name);
            }

            if (type == null || string.IsNullOrEmpty(type.ParentTypeName))
            {
                return name;
            }

            name = type.ParentTypeName;
            vendorId = type.ParentTypeVendorId;
            remaining = remaining - 1;
        }

        return string.Empty;
    }

    private static bool IsKnownValueType(string name)
    {
        return name == "OctetString" || name == "Integer32" || name == "Integer64" ||
            name == "Unsigned32" || name == "Unsigned64" || name == "Float32" ||
            name == "Float64" || name == "Grouped" || name == "Address" ||
            name == "Time" || name == "UTF8String" || name == "DiameterIdentity" ||
            name == "DiameterURI" || name == "Enumerated" || name == "IPFilterRule" ||
            name == "QoSFilterRule";
    }

    private static DiameterTypeDefinition? FindType(
        DiameterDictionarySnapshot snapshot,
        uint vendorId,
        string name)
    {
        int index = 0;
        while (index < snapshot.Types.Count)
        {
            DiameterTypeDefinition type = snapshot.Types[index];
            if (type.VendorId == vendorId && type.Name == name)
            {
                return type;
            }

            index = index + 1;
        }

        return null;
    }

    private static bool HasWildcardRule(DiameterAvpDefinition definition)
    {
        int index = 0;
        while (index < definition.GroupedRules.Count)
        {
            if (definition.GroupedRules[index].AvpCode == 0U &&
                definition.GroupedRules[index].AvpName == "AVP")
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    private static DiameterMessage CreateWatchdog(bool includeRealm)
    {
        DiameterMessage message = new DiameterMessage();
        message.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        message.ApplicationId = DiameterApplicationIds.Common;
        message.Flags = DiameterMessageFlags.Request;
        message.Avps.Add(CreateAvp(
            DiameterAvpCodes.OriginHost,
            DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("peer.example")));
        if (includeRealm)
        {
            message.Avps.Add(CreateAvp(
                DiameterAvpCodes.OriginRealm,
                DiameterAvpFlags.Mandatory,
                Encoding.ASCII.GetBytes("example")));
        }

        return message;
    }

    private static DiameterMessage CreateDisconnectRequest()
    {
        DiameterMessage message = new DiameterMessage();
        message.CommandCode = DiameterCommandCodes.DisconnectPeer;
        message.ApplicationId = DiameterApplicationIds.Common;
        message.Flags = DiameterMessageFlags.Request;
        message.Avps.Add(CreateAvp(264U, DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("peer.example")));
        message.Avps.Add(CreateAvp(296U, DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("example")));
        message.Avps.Add(CreateAvp(273U, DiameterAvpFlags.Mandatory, EncodeInt32(0)));
        return message;
    }

    private static DiameterMessage CreateReAuthRequest()
    {
        DiameterMessage message = new DiameterMessage();
        message.CommandCode = DiameterCommandCodes.ReAuth;
        message.ApplicationId = DiameterApplicationIds.Common;
        message.Flags = DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable;
        message.Avps.Add(CreateAvp(263U, DiameterAvpFlags.Mandatory,
            Encoding.UTF8.GetBytes("session;1")));
        message.Avps.Add(CreateAvp(264U, DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("peer.example")));
        message.Avps.Add(CreateAvp(296U, DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("example")));
        message.Avps.Add(CreateAvp(283U, DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("destination.example")));
        message.Avps.Add(CreateAvp(293U, DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("host.destination.example")));
        message.Avps.Add(CreateAvp(258U, DiameterAvpFlags.Mandatory, new byte[4]));
        message.Avps.Add(CreateAvp(285U, DiameterAvpFlags.Mandatory, EncodeInt32(0)));
        return message;
    }

    private static DiameterAvpDefinition FindAvpDefinition(
        DiameterDictionarySnapshot snapshot,
        uint vendorId,
        uint code)
    {
        int index = 0;
        while (index < snapshot.Avps.Count)
        {
            DiameterAvpDefinition definition = snapshot.Avps[index];
            if (definition.VendorId == vendorId && definition.Code == code)
            {
                return definition;
            }

            index = index + 1;
        }

        Assert.Fail("Expected AVP definition " + vendorId + "/" + code + ".");
        return new DiameterAvpDefinition();
    }

    private static DiameterAvpDefinition CreateDefinition(
        uint code,
        string name,
        DiameterAvpBaseType type)
    {
        DiameterAvpDefinition definition = new DiameterAvpDefinition();
        definition.Code = code;
        definition.Name = name;
        definition.TypeName = type.ToString();
        definition.BaseType = type;
        return definition;
    }

    private static DiameterAvpRuleDefinition CreateRule(
        uint code,
        string name,
        int minimum,
        int maximum)
    {
        DiameterAvpRuleDefinition rule = new DiameterAvpRuleDefinition();
        rule.AvpCode = code;
        rule.AvpName = name;
        rule.MinimumCount = minimum;
        rule.MaximumCount = maximum;
        return rule;
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

    private static byte[] EncodeInt32(int value)
    {
        byte[] data = new byte[4];
        NetworkByteOrder.WriteInt32(data, value);
        return data;
    }

    private static byte[] EncodeAvps(List<DiameterAvp> avps)
    {
        int length = 0;
        int index = 0;
        while (index < avps.Count)
        {
            DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avps[index]);
            Assert.That(measurement.Status, Is.EqualTo(DiameterCodecStatus.Success));
            length = length + measurement.RequiredCount;
            index = index + 1;
        }

        byte[] data = new byte[length];
        int offset = 0;
        index = 0;
        while (index < avps.Count)
        {
            DiameterEncodeResult encoded = AvpEncoder.EncodeAvp(
                new Span<byte>(data, offset, data.Length - offset),
                avps[index]);
            Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
            offset = offset + encoded.WrittenCount;
            index = index + 1;
        }

        return data;
    }

    private static DiameterAvp DecodeFailedChild(DiameterAvp failed)
    {
        Assert.That(failed.Code, Is.EqualTo(DiameterAvpCodes.FailedAvp));
        return DecodeOnlyAvp(failed.Data);
    }

    private static DiameterAvp DecodeOnlyAvp(byte[] data)
    {
        DiameterValueResult decoded = AvpDecoder.DecodeAvpSequence(data);
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(decoded.Avps, Has.Count.EqualTo(1));
        return decoded.Avps[0];
    }

    private static ValidationCase CreateCase(
        DiameterDictionarySnapshot snapshot,
        DiameterMessage message,
        byte[] expectedData)
    {
        ValidationCase result = new ValidationCase();
        result.Snapshot = snapshot;
        result.Message = message;
        result.ExpectedFailedData = expectedData;
        return result;
    }
}

public class ValidationCase
{
    public DiameterDictionarySnapshot Snapshot = new DiameterDictionarySnapshot();
    public DiameterMessage Message = new DiameterMessage();
    public byte[] ExpectedFailedData = new byte[0];
}

public class GroupedValidationCase
{
    public DiameterDictionarySnapshot Snapshot = new DiameterDictionarySnapshot();
    public DiameterAvp Avp = new DiameterAvp();
    public byte[] ExpectedFailedData = new byte[0];
}
