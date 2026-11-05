using System;
using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class AvpValidatorTests
{
    [Test]
    public void KnownAndUnknownOptionalAvpsValidateWithoutFailures()
    {
        AvpValidator validator = CreateBaseValidator();
        DiameterMessage message = new DiameterMessage();
        message.Avps.Add(CreateAvp(
            DiameterAvpCodes.OriginHost,
            0U,
            DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("host.example")));
        message.Avps.Add(CreateAvp(999U, 0U, DiameterAvpFlags.None, new byte[] { 1 }));

        AvpValidationResult result = validator.ValidateMessageAvps(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(result.Failures, Is.Empty);
        });
    }

    [Test]
    public void UnknownMandatoryAvpIsRejectedAtItsMessageIndex()
    {
        AvpValidator validator = CreateBaseValidator();
        DiameterMessage message = new DiameterMessage();
        message.Avps.Add(CreateAvp(998U, 0U, DiameterAvpFlags.None, new byte[0]));
        message.Avps.Add(CreateAvp(
            999U,
            0U,
            DiameterAvpFlags.Mandatory,
            new byte[0]));

        AvpValidationResult result = validator.ValidateMessageAvps(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Count.EqualTo(1));
            AssertFailure(result, 0, "unsupported-mandatory-avp", 999U, 1, 0);
        });
    }

    [Test]
    public void VendorHeaderAndDefinitionIdentityAreValidatedIndependently()
    {
        AvpValidator validator = CreateBaseValidator();
        DiameterMessage message = new DiameterMessage();
        message.Avps.Add(CreateAvp(
            DiameterAvpCodes.OriginHost,
            10415U,
            DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("host.example")));
        message.Avps.Add(CreateAvp(
            DiameterAvpCodes.OriginRealm,
            10415U,
            DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("example")));
        message.Avps.Add(CreateAvp(
            DiameterAvpCodes.DestinationHost,
            0U,
            DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("host.example")));

        AvpValidationResult result = validator.ValidateMessageAvps(message);

        Assert.Multiple(() =>
        {
            Assert.That(CountFailures(result, "vendor-flag-required"), Is.EqualTo(1));
            Assert.That(CountFailures(result, "vendor-id-required"), Is.EqualTo(1));
            Assert.That(CountFailures(result, "avp-vendor-mismatch"), Is.EqualTo(2));
            Assert.That(CountFailures(result, "prohibited-avp-flags-present"), Is.EqualTo(1));
        });
    }

    [Test]
    public void RequiredAndProhibitedVmpMasksAreEnforced()
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        DiameterAvpDefinition definition = CreateDefinition(
            900U,
            0U,
            "Flagged",
            "OctetString",
            DiameterAvpBaseType.OctetString);
        definition.RequiredFlags = DiameterAvpFlags.Mandatory;
        definition.ProhibitedFlags =
            DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Protected;
        snapshot.Avps.Add(definition);
        AvpValidator validator = new AvpValidator(snapshot, new DiameterLimitConfiguration());
        DiameterAvp avp = CreateAvp(
            900U,
            0U,
            DiameterAvpFlags.Protected,
            new byte[0]);

        AvpValidationResult result = validator.ValidateSingleAvp(avp);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Count.EqualTo(2));
            AssertFailure(result, 0, "required-avp-flags-missing", 900U, 0, 0);
            Assert.That(result.Failures[0].ExpectedFlags, Is.EqualTo(DiameterAvpFlags.Mandatory));
            AssertFailure(result, 1, "prohibited-avp-flags-present", 900U, 0, 0);
            Assert.That(
                result.Failures[1].ExpectedFlags,
                Is.EqualTo(DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Protected));
        });
    }

    [Test]
    public void FixedWireSizesAreEnforcedBeforeValueDecoding()
    {
        AvpValidator validator = CreateBaseValidator();
        DiameterAvp avp = CreateAvp(
            DiameterAvpCodes.SessionTimeout,
            0U,
            DiameterAvpFlags.Mandatory,
            new byte[] { 0, 0, 1 });

        AvpValidationResult result = validator.ValidateSingleAvp(avp);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Count.EqualTo(1));
            AssertFailure(result, 0, "invalid-avp-size", DiameterAvpCodes.SessionTimeout, 0, 0);
            Assert.That(result.Failures[0].ExpectedLength, Is.EqualTo(4));
            Assert.That(result.Failures[0].ActualLength, Is.EqualTo(3));
        });
    }

    [Test]
    public void StandardDerivedTypesUseTheirStrictValueCodecs()
    {
        AvpValidator validator = CreateBaseValidator();
        DiameterMessage message = new DiameterMessage();
        message.Avps.Add(CreateAvp(
            DiameterAvpCodes.HostIpAddress,
            0U,
            DiameterAvpFlags.Mandatory,
            new byte[] { 0, 3 }));
        message.Avps.Add(CreateAvp(
            DiameterAvpCodes.UserName,
            0U,
            DiameterAvpFlags.Mandatory,
            new byte[] { 255 }));
        message.Avps.Add(CreateAvp(
            DiameterAvpCodes.OriginHost,
            0U,
            DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("bad host")));
        message.Avps.Add(CreateAvp(
            DiameterAvpCodes.RedirectHost,
            0U,
            DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("http://host.example")));

        AvpValidationResult result = validator.ValidateMessageAvps(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Count.EqualTo(4));
            AssertValueFailure(result, 0, DiameterAvpCodes.HostIpAddress, "unsupported-address-family");
            AssertValueFailure(result, 1, DiameterAvpCodes.UserName, "invalid-utf8");
            AssertValueFailure(result, 2, DiameterAvpCodes.OriginHost, "invalid-diameter-identity");
            AssertValueFailure(result, 3, DiameterAvpCodes.RedirectHost, "invalid-uri-scheme");
        });
    }

    [Test]
    public void CustomDerivedTypeTraversesItsParentChain()
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        DiameterTypeDefinition type = new DiameterTypeDefinition();
        type.Name = "Custom-Identity";
        type.BaseType = DiameterAvpBaseType.OctetString;
        type.ParentTypeName = "DiameterIdentity";
        type.ParentTypeVendorId = 0U;
        snapshot.Types.Add(type);
        snapshot.Avps.Add(CreateDefinition(
            900U,
            0U,
            "Custom-Identity-AVP",
            "Custom-Identity",
            DiameterAvpBaseType.OctetString));
        AvpValidator validator = new AvpValidator(snapshot, null);

        AvpValidationResult result = validator.ValidateSingleAvp(CreateAvp(
            900U,
            0U,
            DiameterAvpFlags.None,
            Encoding.ASCII.GetBytes("bad host")));

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Count.EqualTo(1));
            AssertValueFailure(result, 0, 900U, "invalid-diameter-identity");
        });
    }

    [Test]
    public void EnumeratedDomainAcceptsAssignedAndRejectsUnknownValues()
    {
        AvpValidator validator = CreateBaseValidator();
        DiameterAvp assigned = CreateAvp(
            DiameterAvpCodes.DisconnectCause,
            0U,
            DiameterAvpFlags.Mandatory,
            EncodeInt32(0));
        DiameterAvp unknown = CreateAvp(
            DiameterAvpCodes.DisconnectCause,
            0U,
            DiameterAvpFlags.Mandatory,
            EncodeInt32(99));

        AvpValidationResult valid = validator.ValidateSingleAvp(assigned);
        AvpValidationResult invalid = validator.ValidateSingleAvp(unknown);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(invalid.Failures, Has.Count.EqualTo(1));
            AssertFailure(
                invalid,
                0,
                "unknown-enumerated-value",
                DiameterAvpCodes.DisconnectCause,
                0,
                0);
            Assert.That(invalid.Failures[0].ActualCount, Is.EqualTo(99));
        });
    }

    [Test]
    public void CompleteGroupedAvpValidatesItsChildrenRecursively()
    {
        AvpValidator validator = CreateBaseValidator();
        DiameterAvp proxyHost = CreateAvp(
            DiameterAvpCodes.ProxyHost,
            0U,
            DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("proxy.example"));
        DiameterAvp proxyState = CreateAvp(
            DiameterAvpCodes.ProxyState,
            0U,
            DiameterAvpFlags.Mandatory,
            new byte[] { 1, 2, 3 });
        DiameterAvp grouped = CreateAvp(
            DiameterAvpCodes.ProxyInfo,
            0U,
            DiameterAvpFlags.Mandatory,
            EncodeAvps(new DiameterAvp[] { proxyHost, proxyState }));

        AvpValidationResult result = validator.ValidateSingleAvp(grouped);

        Assert.That(result.Status, Is.EqualTo(DiameterValidationStatus.Valid));
    }

    [Test]
    public void GroupedGrammarReportsMissingChildAndUnknownMandatoryExtension()
    {
        AvpValidator validator = CreateBaseValidator();
        DiameterAvp proxyHost = CreateAvp(
            DiameterAvpCodes.ProxyHost,
            0U,
            DiameterAvpFlags.Mandatory,
            Encoding.ASCII.GetBytes("proxy.example"));
        DiameterAvp unknown = CreateAvp(
            999U,
            0U,
            DiameterAvpFlags.Mandatory,
            new byte[0]);
        DiameterAvp grouped = CreateAvp(
            DiameterAvpCodes.ProxyInfo,
            0U,
            DiameterAvpFlags.Mandatory,
            EncodeAvps(new DiameterAvp[] { proxyHost, unknown }));

        AvpValidationResult result = validator.ValidateSingleAvp(grouped);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Count.EqualTo(2));
            AssertFailure(
                result,
                0,
                "missing-required-grouped-avp",
                DiameterAvpCodes.ProxyState,
                -1,
                1);
            Assert.That(result.Failures[0].ParentAvpCode, Is.EqualTo(DiameterAvpCodes.ProxyInfo));
            AssertFailure(result, 1, "unsupported-mandatory-avp", 999U, 1, 1);
        });
    }

    [Test]
    public void MalformedGroupedPayloadReturnsItsUnderlyingCodecFailure()
    {
        AvpValidator validator = CreateBaseValidator();
        DiameterAvp grouped = CreateAvp(
            DiameterAvpCodes.ProxyInfo,
            0U,
            DiameterAvpFlags.Mandatory,
            new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 });

        AvpValidationResult result = validator.ValidateSingleAvp(grouped);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Count.EqualTo(1));
            AssertFailure(result, 0, "invalid-grouped-value", DiameterAvpCodes.ProxyInfo, 0, 0);
            Assert.That(result.Failures[0].ValueCode, Is.EqualTo("invalid-avp-length"));
        });
    }

    [Test]
    public void ClosedGroupedGrammarEnforcesCountsFixedHeadOrderingAndUnlistedChildren()
    {
        AvpValidator validator = CreateCustomGroupedValidator(new DiameterLimitConfiguration());
        DiameterAvp second = CreateAvp(902U, 0U, DiameterAvpFlags.None, new byte[0]);
        DiameterAvp first = CreateAvp(901U, 0U, DiameterAvpFlags.None, new byte[0]);
        DiameterAvp duplicate = CreateAvp(901U, 0U, DiameterAvpFlags.None, new byte[0]);
        DiameterAvp extension = CreateAvp(903U, 0U, DiameterAvpFlags.None, new byte[0]);
        DiameterAvp grouped = CreateAvp(
            900U,
            0U,
            DiameterAvpFlags.None,
            EncodeAvps(new DiameterAvp[] { second, first, duplicate, extension }));

        AvpValidationResult result = validator.ValidateSingleAvp(grouped);

        Assert.Multiple(() =>
        {
            Assert.That(CountFailures(result, "too-many-grouped-avp-occurrences"), Is.EqualTo(1));
            Assert.That(CountFailures(result, "fixed-head-grouped-avp-misordered"), Is.EqualTo(1));
            Assert.That(CountFailures(result, "grouped-avp-ordering-violation"), Is.EqualTo(2));
            Assert.That(CountFailures(result, "grouped-avp-not-allowed"), Is.EqualTo(1));
        });
    }

    [Test]
    public void RecursiveGroupedValidationHonorsConfiguredDepthLimit()
    {
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        limits.MaximumGroupedDepth = 2;
        AvpValidator validator = CreateRecursiveGroupedValidator(limits);
        DiameterAvp innermost = CreateAvp(900U, 0U, DiameterAvpFlags.None, new byte[0]);
        DiameterAvp middle = CreateAvp(
            900U,
            0U,
            DiameterAvpFlags.None,
            EncodeAvps(new DiameterAvp[] { innermost }));
        DiameterAvp outer = CreateAvp(
            900U,
            0U,
            DiameterAvpFlags.None,
            EncodeAvps(new DiameterAvp[] { middle }));

        AvpValidationResult result = validator.ValidateSingleAvp(outer);

        Assert.Multiple(() =>
        {
            Assert.That(CountFailures(result, "grouped-depth-limit"), Is.EqualTo(1));
            Assert.That(FindFailure(result, "grouped-depth-limit").Depth, Is.EqualTo(2));
        });
    }

    private static AvpValidator CreateBaseValidator()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        Assert.That(
            builder.RegisterDictionaryModule(Rfc6733Dictionary.CreateModule()).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        return new AvpValidator(
            builder.ReadDictionarySnapshot(),
            new DiameterLimitConfiguration());
    }

    private static AvpValidator CreateCustomGroupedValidator(
        DiameterLimitConfiguration limits)
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        DiameterAvpDefinition grouped = CreateDefinition(
            900U,
            0U,
            "Custom-Group",
            "Grouped",
            DiameterAvpBaseType.Grouped);
        grouped.AllowUnlistedAvps = false;
        grouped.GroupedRules.Add(CreateRule(901U, "First", 1, 1, 0, 0));
        grouped.GroupedRules.Add(CreateRule(902U, "Second", 0, 1, -1, 1));
        snapshot.Avps.Add(grouped);
        snapshot.Avps.Add(CreateDefinition(
            901U,
            0U,
            "First",
            "OctetString",
            DiameterAvpBaseType.OctetString));
        snapshot.Avps.Add(CreateDefinition(
            902U,
            0U,
            "Second",
            "OctetString",
            DiameterAvpBaseType.OctetString));
        return new AvpValidator(snapshot, limits);
    }

    private static AvpValidator CreateRecursiveGroupedValidator(
        DiameterLimitConfiguration limits)
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        snapshot.Avps.Add(CreateDefinition(
            900U,
            0U,
            "Recursive-Group",
            "Grouped",
            DiameterAvpBaseType.Grouped));
        return new AvpValidator(snapshot, limits);
    }

    private static DiameterAvpDefinition CreateDefinition(
        uint code,
        uint vendorId,
        string name,
        string typeName,
        DiameterAvpBaseType baseType)
    {
        DiameterAvpDefinition definition = new DiameterAvpDefinition();
        definition.Code = code;
        definition.VendorId = vendorId;
        definition.Name = name;
        definition.TypeName = typeName;
        definition.BaseType = baseType;
        return definition;
    }

    private static DiameterAvpRuleDefinition CreateRule(
        uint code,
        string name,
        int minimum,
        int maximum,
        int fixedPosition,
        int orderingGroup)
    {
        DiameterAvpRuleDefinition rule = new DiameterAvpRuleDefinition();
        rule.AvpCode = code;
        rule.AvpName = name;
        rule.MinimumCount = minimum;
        rule.MaximumCount = maximum;
        rule.FixedHeadPosition = fixedPosition;
        rule.OrderingGroup = orderingGroup;
        return rule;
    }

    private static DiameterAvp CreateAvp(
        uint code,
        uint vendorId,
        DiameterAvpFlags flags,
        byte[] data)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.VendorId = vendorId;
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

    private static byte[] EncodeAvps(DiameterAvp[] avps)
    {
        int requiredCount = 0;
        int index = 0;
        while (index < avps.Length)
        {
            DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avps[index]);
            Assert.That(measurement.Status, Is.EqualTo(DiameterCodecStatus.Success));
            requiredCount = requiredCount + measurement.RequiredCount;
            index = index + 1;
        }

        byte[] data = new byte[requiredCount];
        int offset = 0;
        index = 0;
        while (index < avps.Length)
        {
            DiameterEncodeResult encoded = AvpEncoder.EncodeAvp(
                data.AsSpan(offset),
                avps[index]);
            Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
            offset = offset + encoded.WrittenCount;
            index = index + 1;
        }

        return data;
    }

    private static void AssertValueFailure(
        AvpValidationResult result,
        int index,
        uint avpCode,
        string valueCode)
    {
        AssertFailure(result, index, "invalid-derived-avp-value", avpCode, index, 0);
        Assert.That(result.Failures[index].ValueCode, Is.EqualTo(valueCode));
    }

    private static void AssertFailure(
        AvpValidationResult result,
        int index,
        string code,
        uint avpCode,
        int avpIndex,
        int depth)
    {
        Assert.That(result.Failures[index].Code, Is.EqualTo(code));
        Assert.That(result.Failures[index].AvpCode, Is.EqualTo(avpCode));
        Assert.That(result.Failures[index].AvpIndex, Is.EqualTo(avpIndex));
        Assert.That(result.Failures[index].Depth, Is.EqualTo(depth));
    }

    private static int CountFailures(AvpValidationResult result, string code)
    {
        int count = 0;
        int index = 0;
        while (index < result.Failures.Count)
        {
            if (result.Failures[index].Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    private static AvpValidationFailure FindFailure(
        AvpValidationResult result,
        string code)
    {
        int index = 0;
        while (index < result.Failures.Count)
        {
            if (result.Failures[index].Code == code)
            {
                return result.Failures[index];
            }

            index = index + 1;
        }

        Assert.Fail("Missing AVP validation failure: " + code);
        return new AvpValidationFailure();
    }
}
