using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace diameter.Tests;

public class Rfc8506CoverageMatrixTests
{
    [Test]
    public void EveryAvpDefinitionMatchesTheRfc8506Matrix()
    {
        DiameterDictionaryModule module = Rfc8506Dictionary.CreateModule();
        string[] expected = new string[]
        {
            "11|Filter-Id|UTF8String|OctetString|-1|Mandatory",
            "411|CC-Correlation-Id|OctetString|OctetString|-1|None",
            "412|CC-Input-Octets|Unsigned64|Unsigned64|8|Mandatory",
            "413|CC-Money|Grouped|Grouped|-1|Mandatory",
            "414|CC-Output-Octets|Unsigned64|Unsigned64|8|Mandatory",
            "415|CC-Request-Number|Unsigned32|Unsigned32|4|Mandatory",
            "416|CC-Request-Type|CC-Request-Type|Integer32|4|Mandatory",
            "417|CC-Service-Specific-Units|Unsigned64|Unsigned64|8|Mandatory",
            "418|CC-Session-Failover|CC-Session-Failover|Integer32|4|Mandatory",
            "419|CC-Sub-Session-Id|Unsigned64|Unsigned64|8|Mandatory",
            "420|CC-Time|Unsigned32|Unsigned32|4|Mandatory",
            "421|CC-Total-Octets|Unsigned64|Unsigned64|8|Mandatory",
            "422|Check-Balance-Result|Check-Balance-Result|Integer32|4|Mandatory",
            "423|Cost-Information|Grouped|Grouped|-1|Mandatory",
            "424|Cost-Unit|UTF8String|OctetString|-1|Mandatory",
            "425|Currency-Code|Unsigned32|Unsigned32|4|Mandatory",
            "426|Credit-Control|Credit-Control|Integer32|4|Mandatory",
            "427|Credit-Control-Failure-Handling|Credit-Control-Failure-Handling|Integer32|4|Mandatory",
            "428|Direct-Debiting-Failure-Handling|Direct-Debiting-Failure-Handling|Integer32|4|Mandatory",
            "429|Exponent|Integer32|Integer32|4|Mandatory",
            "430|Final-Unit-Indication|Grouped|Grouped|-1|Mandatory",
            "431|Granted-Service-Unit|Grouped|Grouped|-1|Mandatory",
            "432|Rating-Group|Unsigned32|Unsigned32|4|Mandatory",
            "433|Redirect-Address-Type|Redirect-Address-Type|Integer32|4|Mandatory",
            "434|Redirect-Server|Grouped|Grouped|-1|Mandatory",
            "435|Redirect-Server-Address|UTF8String|OctetString|-1|Mandatory",
            "436|Requested-Action|Requested-Action|Integer32|4|Mandatory",
            "437|Requested-Service-Unit|Grouped|Grouped|-1|Mandatory",
            "438|Restriction-Filter-Rule|IPFilterRule|OctetString|-1|Mandatory",
            "439|Service-Identifier|Unsigned32|Unsigned32|4|Mandatory",
            "440|Service-Parameter-Info|Grouped|Grouped|-1|None",
            "441|Service-Parameter-Type|Unsigned32|Unsigned32|4|None",
            "442|Service-Parameter-Value|OctetString|OctetString|-1|None",
            "443|Subscription-Id|Grouped|Grouped|-1|Mandatory",
            "444|Subscription-Id-Data|UTF8String|OctetString|-1|Mandatory",
            "445|Unit-Value|Grouped|Grouped|-1|Mandatory",
            "446|Used-Service-Unit|Grouped|Grouped|-1|Mandatory",
            "447|Value-Digits|Integer64|Integer64|8|Mandatory",
            "448|Validity-Time|Unsigned32|Unsigned32|4|Mandatory",
            "449|Final-Unit-Action|Final-Unit-Action|Integer32|4|Mandatory",
            "450|Subscription-Id-Type|Subscription-Id-Type|Integer32|4|Mandatory",
            "451|Tariff-Time-Change|Time|OctetString|4|Mandatory",
            "452|Tariff-Change-Usage|Tariff-Change-Usage|Integer32|4|Mandatory",
            "453|G-S-U-Pool-Identifier|Unsigned32|Unsigned32|4|Mandatory",
            "454|CC-Unit-Type|CC-Unit-Type|Integer32|4|Mandatory",
            "455|Multiple-Services-Indicator|Multiple-Services-Indicator|Integer32|4|Mandatory",
            "456|Multiple-Services-Credit-Control|Grouped|Grouped|-1|Mandatory",
            "457|G-S-U-Pool-Reference|Grouped|Grouped|-1|Mandatory",
            "458|User-Equipment-Info|Grouped|Grouped|-1|None",
            "459|User-Equipment-Info-Type|User-Equipment-Info-Type|Integer32|4|None",
            "460|User-Equipment-Info-Value|OctetString|OctetString|-1|None",
            "461|Service-Context-Id|UTF8String|OctetString|-1|Mandatory",
            "509|Filter-Rule|QoSFilterRule|OctetString|-1|Mandatory",
            "653|User-Equipment-Info-Extension|Grouped|Grouped|-1|None",
            "654|User-Equipment-Info-IMEISV|OctetString|OctetString|-1|None",
            "655|User-Equipment-Info-MAC|OctetString|OctetString|-1|None",
            "656|User-Equipment-Info-EUI64|OctetString|OctetString|-1|None",
            "657|User-Equipment-Info-ModifiedEUI64|OctetString|OctetString|-1|None",
            "658|User-Equipment-Info-IMEI|OctetString|OctetString|-1|None",
            "659|Subscription-Id-Extension|Grouped|Grouped|-1|None",
            "660|Subscription-Id-E164|UTF8String|OctetString|-1|None",
            "661|Subscription-Id-IMSI|UTF8String|OctetString|-1|None",
            "662|Subscription-Id-SIP-URI|UTF8String|OctetString|-1|None",
            "663|Subscription-Id-NAI|UTF8String|OctetString|-1|None",
            "664|Subscription-Id-Private|UTF8String|OctetString|-1|None",
            "665|Redirect-Server-Extension|Grouped|Grouped|-1|None",
            "666|Redirect-Address-IPAddress|Address|OctetString|-1|None",
            "667|Redirect-Address-URL|UTF8String|OctetString|-1|None",
            "668|Redirect-Address-SIP-URI|UTF8String|OctetString|-1|None",
            "669|QoS-Final-Unit-Indication|Grouped|Grouped|-1|None"
        };

        string[] actual = module.Avps
            .Select(avp => string.Join(
                "|",
                avp.Code,
                avp.Name,
                avp.TypeName,
                avp.BaseType,
                avp.FixedDataLength,
                avp.RequiredFlags))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected.OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(module.Avps, Has.All.Matches<DiameterAvpDefinition>(avp =>
                avp.VendorId == DiameterVendorIds.Ietf &&
                avp.ProhibitedFlags == DiameterAvpFlags.VendorSpecific));
        });
    }

    [Test]
    public void EveryEnumeratedValueAndResultCodeMatchesTheRfc8506Matrix()
    {
        string[] expected = new string[]
        {
            "268||DIAMETER_END_USER_SERVICE_DENIED|4010",
            "268||DIAMETER_CREDIT_CONTROL_NOT_APPLICABLE|4011",
            "268||DIAMETER_CREDIT_LIMIT_REACHED|4012",
            "268||DIAMETER_USER_UNKNOWN|5030",
            "268||DIAMETER_RATING_FAILED|5031",
            "416|CC-Request-Type|INITIAL_REQUEST|1",
            "416|CC-Request-Type|UPDATE_REQUEST|2",
            "416|CC-Request-Type|TERMINATION_REQUEST|3",
            "416|CC-Request-Type|EVENT_REQUEST|4",
            "418|CC-Session-Failover|FAILOVER_NOT_SUPPORTED|0",
            "418|CC-Session-Failover|FAILOVER_SUPPORTED|1",
            "422|Check-Balance-Result|ENOUGH_CREDIT|0",
            "422|Check-Balance-Result|NO_CREDIT|1",
            "426|Credit-Control|CREDIT_AUTHORIZATION|0",
            "426|Credit-Control|RE_AUTHORIZATION|1",
            "427|Credit-Control-Failure-Handling|TERMINATE|0",
            "427|Credit-Control-Failure-Handling|CONTINUE|1",
            "427|Credit-Control-Failure-Handling|RETRY_AND_TERMINATE|2",
            "428|Direct-Debiting-Failure-Handling|TERMINATE_OR_BUFFER|0",
            "428|Direct-Debiting-Failure-Handling|CONTINUE|1",
            "433|Redirect-Address-Type|IPV4_ADDRESS|0",
            "433|Redirect-Address-Type|IPV6_ADDRESS|1",
            "433|Redirect-Address-Type|URL|2",
            "433|Redirect-Address-Type|SIP_URI|3",
            "436|Requested-Action|DIRECT_DEBITING|0",
            "436|Requested-Action|REFUND_ACCOUNT|1",
            "436|Requested-Action|CHECK_BALANCE|2",
            "436|Requested-Action|PRICE_ENQUIRY|3",
            "449|Final-Unit-Action|TERMINATE|0",
            "449|Final-Unit-Action|REDIRECT|1",
            "449|Final-Unit-Action|RESTRICT_ACCESS|2",
            "450|Subscription-Id-Type|END_USER_E164|0",
            "450|Subscription-Id-Type|END_USER_IMSI|1",
            "450|Subscription-Id-Type|END_USER_SIP_URI|2",
            "450|Subscription-Id-Type|END_USER_NAI|3",
            "450|Subscription-Id-Type|END_USER_PRIVATE|4",
            "452|Tariff-Change-Usage|UNIT_BEFORE_TARIFF_CHANGE|0",
            "452|Tariff-Change-Usage|UNIT_AFTER_TARIFF_CHANGE|1",
            "452|Tariff-Change-Usage|UNIT_INDETERMINATE|2",
            "454|CC-Unit-Type|TIME|0",
            "454|CC-Unit-Type|MONEY|1",
            "454|CC-Unit-Type|TOTAL-OCTETS|2",
            "454|CC-Unit-Type|INPUT-OCTETS|3",
            "454|CC-Unit-Type|OUTPUT-OCTETS|4",
            "454|CC-Unit-Type|SERVICE-SPECIFIC-UNITS|5",
            "455|Multiple-Services-Indicator|MULTIPLE_SERVICES_NOT_SUPPORTED|0",
            "455|Multiple-Services-Indicator|MULTIPLE_SERVICES_SUPPORTED|1",
            "459|User-Equipment-Info-Type|IMEISV|0",
            "459|User-Equipment-Info-Type|MAC|1",
            "459|User-Equipment-Info-Type|EUI64|2",
            "459|User-Equipment-Info-Type|MODIFIED_EUI64|3"
        };
        DiameterDictionaryModule module = Rfc8506Dictionary.CreateModule();
        string[] actual = module.EnumValues
            .Select(value => string.Join(
                "|", value.AvpCode, value.TypeName, value.Name, value.Value))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected.OrderBy(value => value, StringComparer.Ordinal)));
    }

    [Test]
    public void EveryGroupedChildRuleMatchesTheRfc8506Grammar()
    {
        string[] expected = new string[]
        {
            "413|425|0|1", "413|445|1|1",
            "423|424|0|1", "423|425|1|1", "423|445|1|1",
            "430|11|0|-1", "430|434|0|1", "430|438|0|-1", "430|449|1|1",
            "431|412|0|1", "431|413|0|1", "431|414|0|1", "431|417|0|1",
            "431|420|0|1", "431|421|0|1", "431|451|0|1",
            "434|433|1|1", "434|435|1|1",
            "437|412|0|1", "437|413|0|1", "437|414|0|1", "437|417|0|1",
            "437|420|0|1", "437|421|0|1",
            "440|441|1|1", "440|442|1|1",
            "443|444|1|1", "443|450|1|1",
            "445|429|0|1", "445|447|1|1",
            "446|412|0|1", "446|413|0|1", "446|414|0|1", "446|417|0|1",
            "446|420|0|1", "446|421|0|1", "446|452|0|1",
            "456|268|0|1", "456|430|0|1", "456|431|0|1", "456|432|0|1",
            "456|437|0|1", "456|439|0|-1", "456|446|0|-1", "456|448|0|1",
            "456|452|0|1", "456|457|0|-1", "456|669|0|1",
            "457|445|1|1", "457|453|1|1", "457|454|1|1",
            "458|459|1|1", "458|460|1|1",
            "653|654|0|1", "653|655|0|1", "653|656|0|1", "653|657|0|1",
            "653|658|0|1",
            "659|660|0|1", "659|661|0|1", "659|662|0|1", "659|663|0|1",
            "659|664|0|1",
            "665|666|0|1", "665|667|0|1", "665|668|0|1",
            "669|11|0|-1", "669|449|1|1", "669|509|0|-1", "669|665|0|1"
        };
        DiameterDictionaryModule module = Rfc8506Dictionary.CreateModule();
        string[] actual = module.Avps
            .SelectMany(parent => parent.GroupedRules.Select(rule => string.Join(
                "|", parent.Code, rule.AvpCode, rule.MinimumCount, rule.MaximumCount)))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected.OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(FindAvp(module, 431U).AllowUnlistedAvps, Is.True);
            Assert.That(FindAvp(module, 437U).AllowUnlistedAvps, Is.True);
            Assert.That(FindAvp(module, 446U).AllowUnlistedAvps, Is.True);
            Assert.That(FindAvp(module, 456U).AllowUnlistedAvps, Is.True);
            Assert.That(FindAvp(module, 653U).AllowUnlistedAvps, Is.True);
            Assert.That(FindAvp(module, 659U).AllowUnlistedAvps, Is.True);
            Assert.That(FindAvp(module, 665U).AllowUnlistedAvps, Is.True);
            Assert.That(FindAvp(module, 669U).AllowUnlistedAvps, Is.True);
        });
    }

    [Test]
    public void EveryCcrAndCcaOccurrenceRuleMatchesTheRfc8506Abnf()
    {
        string[] expected = new string[]
        {
            "CCA|1|0|1|-1", "CCA|50|0|1|-1", "CCA|55|0|1|-1",
            "CCA|258|1|1|-1", "CCA|261|0|1|-1", "CCA|262|0|1|-1",
            "CCA|263|1|1|0", "CCA|264|1|1|-1", "CCA|268|1|1|-1",
            "CCA|278|0|1|-1", "CCA|279|0|-1|-1", "CCA|282|0|-1|-1",
            "CCA|284|0|-1|-1", "CCA|292|0|-1|-1", "CCA|296|1|1|-1",
            "CCA|415|1|1|-1", "CCA|416|1|1|-1", "CCA|418|0|1|-1",
            "CCA|419|0|1|-1", "CCA|422|0|1|-1", "CCA|423|0|1|-1",
            "CCA|427|0|1|-1", "CCA|428|0|1|-1", "CCA|430|0|1|-1",
            "CCA|431|0|1|-1", "CCA|448|0|1|-1", "CCA|456|0|-1|-1",
            "CCA|669|0|1|-1",
            "CCR|1|0|1|-1", "CCR|50|0|1|-1", "CCR|55|0|1|-1",
            "CCR|258|1|1|-1", "CCR|263|1|1|0", "CCR|264|1|1|-1",
            "CCR|278|0|1|-1", "CCR|282|0|-1|-1", "CCR|283|1|1|-1",
            "CCR|284|0|-1|-1", "CCR|293|0|1|-1", "CCR|295|0|1|-1",
            "CCR|296|1|1|-1", "CCR|411|0|1|-1", "CCR|415|1|1|-1",
            "CCR|416|1|1|-1", "CCR|419|0|1|-1", "CCR|436|0|1|-1",
            "CCR|437|0|1|-1", "CCR|439|0|1|-1", "CCR|440|0|-1|-1",
            "CCR|443|0|-1|-1", "CCR|446|0|-1|-1", "CCR|455|0|1|-1",
            "CCR|456|0|-1|-1", "CCR|458|0|1|-1", "CCR|461|1|1|-1",
            "CCR|653|0|1|-1", "CCR|659|0|-1|-1"
        };
        DiameterDictionaryModule module = Rfc8506Dictionary.CreateModule();
        string[] actual = module.Commands
            .Where(command => command.Code == DiameterCommandCodes.CreditControl)
            .SelectMany(command => command.Rules.Select(rule => string.Join(
                "|",
                command.IsRequest ? "CCR" : "CCA",
                rule.AvpCode,
                rule.MinimumCount,
                rule.MaximumCount,
                rule.FixedHeadPosition)))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected.OrderBy(value => value, StringComparer.Ordinal)));
    }

    [Test]
    public void EveryRfc8506AvpRoundTripsThroughItsCodecAndPassesValidation()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionarySnapshot snapshot = dictionary.ReadDictionarySnapshot();
        DiameterDictionaryModule module = dictionary.FindDictionaryModuleByName("rfc8506")!;
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        IDiameterApplicationAdapter adapter = new DiameterApplicationAdapter(
            dictionary,
            codec,
            new DiameterLimitConfiguration());
        IDiameterValidator validator = new DiameterValidator();
        List<DiameterAvp> groupedTypes = CreateGroupedTypes(snapshot);

        int index = 0;
        while (index < module.Avps.Count)
        {
            DiameterAvpDefinition definition = module.Avps[index];
            DiameterAvp avp = CreateValidAvp(definition, snapshot, codec, groupedTypes);
            DiameterTypedAvp typed = adapter.DecodeDiameterTypedAvp(
                avp,
                new DateTimeOffset(2036, 2, 7, 6, 28, 16, TimeSpan.Zero));
            DiameterAvp encoded = adapter.EncodeDiameterTypedAvp(typed);
            DiameterValidationResult validation = validator.ValidateGroupedAvp(
                avp,
                snapshot,
                new DiameterLimitConfiguration());

            Assert.Multiple(() =>
            {
                Assert.That(typed.Known, Is.True, definition.Name);
                Assert.That(typed.Decoded, Is.True, definition.Name);
                Assert.That(encoded.Data, Is.EqualTo(avp.Data), definition.Name);
                Assert.That(validation.Status, Is.EqualTo(DiameterValidationStatus.Valid),
                    definition.Name + ": " + string.Join(",", validation.Issues.Select(issue => issue.Code)));
            });
            index = index + 1;
        }
    }

    private static DiameterAvp CreateValidAvp(
        DiameterAvpDefinition definition,
        DiameterDictionarySnapshot snapshot,
        IDiameterAvpCodec codec,
        List<DiameterAvp> groupedTypes)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = definition.Code;
        avp.VendorId = definition.VendorId;
        avp.Flags = definition.RequiredFlags;
        if (definition.BaseType == DiameterAvpBaseType.Grouped)
        {
            List<DiameterAvp> children = new List<DiameterAvp>();
            int ruleIndex = 0;
            while (ruleIndex < definition.GroupedRules.Count)
            {
                DiameterAvpRuleDefinition rule = definition.GroupedRules[ruleIndex];
                DiameterAvpDefinition child = snapshot.Avps.Single(item =>
                    item.Code == rule.AvpCode && item.VendorId == rule.AvpVendorId);
                int count = 0;
                while (count < rule.MinimumCount)
                {
                    children.Add(CreateValidAvp(child, snapshot, codec, groupedTypes));
                    count = count + 1;
                }
                ruleIndex = ruleIndex + 1;
            }

            DiameterValueResult measured = codec.MeasureGrouped(
                children,
                groupedTypes,
                new DiameterLimitConfiguration());
            byte[] data = new byte[measured.RequiredCount];
            DiameterValueResult encoded = codec.EncodeGrouped(
                data,
                children,
                groupedTypes,
                new DiameterLimitConfiguration());
            Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success), definition.Name);
            avp.Data = data;
            return avp;
        }

        DiameterEnumValueDefinition? enumValue = snapshot.EnumValues.FirstOrDefault(value =>
            value.AvpCode == definition.Code && value.AvpVendorId == definition.VendorId);
        if (enumValue != null)
        {
            avp.Data = EncodeInteger32(codec, enumValue.Value);
        }
        else if (definition.TypeName == "Address")
        {
            avp.Data = new byte[] { 0, 1, 192, 0, 2, 1 };
        }
        else if (definition.TypeName == "Time")
        {
            avp.Data = new byte[] { 0, 0, 0, 0 };
        }
        else if (definition.TypeName == "IPFilterRule")
        {
            avp.Data = Encoding.ASCII.GetBytes("permit out ip from any to assigned");
        }
        else if (definition.TypeName == "QoSFilterRule")
        {
            avp.Data = Encoding.ASCII.GetBytes("tag in ip from any to assigned DSCP 46");
        }
        else if (definition.TypeName == "UTF8String")
        {
            avp.Data = Encoding.UTF8.GetBytes("value");
        }
        else if (definition.BaseType == DiameterAvpBaseType.Integer32 ||
            definition.BaseType == DiameterAvpBaseType.Unsigned32)
        {
            avp.Data = new byte[4];
        }
        else if (definition.BaseType == DiameterAvpBaseType.Integer64 ||
            definition.BaseType == DiameterAvpBaseType.Unsigned64)
        {
            avp.Data = new byte[8];
        }
        else
        {
            avp.Data = new byte[] { 1, 2, 3 };
        }

        return avp;
    }

    private static byte[] EncodeInteger32(IDiameterAvpCodec codec, int value)
    {
        byte[] data = new byte[4];
        DiameterValueResult result = codec.EncodeInteger32(data, value);
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return data;
    }

    private static List<DiameterAvp> CreateGroupedTypes(DiameterDictionarySnapshot snapshot)
    {
        return snapshot.Avps
            .Where(definition => definition.BaseType == DiameterAvpBaseType.Grouped)
            .Select(definition => new DiameterAvp
            {
                Code = definition.Code,
                VendorId = definition.VendorId
            })
            .ToList();
    }

    private static DiameterAvpDefinition FindAvp(
        DiameterDictionaryModule module,
        uint code)
    {
        return module.Avps.Single(avp => avp.Code == code);
    }
}
