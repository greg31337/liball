using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace diameter.Tests;

public class ParserInteroperabilityCorpusTests
{
    private const uint ThreeGppVendorId = 10415;

    [Test]
    public void FreeDiameterCapabilitiesRequestParsesAndReencodesExactly()
    {
        byte[] wire = ReadHexCorpus("freeDiameter-cer.hex");
        DiameterMessageCodec codec = new DiameterMessageCodec();

        DiameterDecodeResult decoded = codec.DecodeMessage(
            wire,
            false,
            new DiameterLimitConfiguration());
        DiameterEncodeResult encoded = codec.EncodeMessageToArray(
            decoded.Message);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoded.ConsumedCount, Is.EqualTo(wire.Length));
            Assert.That(decoded.Message!.CommandCode,
                Is.EqualTo(DiameterCommandCodes.CapabilitiesExchange));
            Assert.That(decoded.Message.Flags,
                Is.EqualTo(DiameterMessageFlags.Request));
            Assert.That(decoded.Message.ApplicationId,
                Is.EqualTo(DiameterApplicationIds.Common));
            Assert.That(ReadText(decoded.Message,
                    DiameterAvpCodes.OriginHost,
                    0),
                Is.EqualTo("peer.freediameter.test"));
            Assert.That(ReadText(decoded.Message,
                    DiameterAvpCodes.ProductName,
                    0),
                Is.EqualTo("freeDiameter"));
            Assert.That(ReadUnsigned32(decoded.Message,
                    DiameterAvpCodes.AuthApplicationId,
                    0),
                Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(ReadUnsigned32(decoded.Message,
                    DiameterAvpCodes.AcctApplicationId,
                    0),
                Is.EqualTo(DiameterApplicationIds.BaseAccounting));
            Assert.That(encoded.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encoded.Data, Is.EqualTo(wire));
        });

        DiameterAvp hostAddress = FindAvp(
            decoded.Message!,
            DiameterAvpCodes.HostIpAddress,
            0);
        DiameterValueResult address = new DiameterAvpCodec()
            .DecodeAddress(hostAddress.Data);
        Assert.Multiple(() =>
        {
            Assert.That(address.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(address.Address, Is.EqualTo(IPAddress.Parse("192.0.2.10")));
        });
    }

    [Test]
    public void JDiameterCompatibleDictionaryLoadsEnumsGroupedRulesAndCommandGrammar()
    {
        string json = File.ReadAllText(FindCorpusFile(
            "jdiameter-rfc4006.json"));
        IDiameterDictionaryLoader loader =
            new DiameterJsonDictionaryLoader();

        DiameterDictionaryModule module = loader.LoadDictionaryModule(json);
        DiameterAvpDefinition requestType = module.Avps.Single(
            item => item.Code == DiameterAvpCodes.CcRequestType);
        DiameterAvpDefinition grantedUnits = module.Avps.Single(
            item => item.Code == DiameterAvpCodes.GrantedServiceUnit);
        DiameterCommandDefinition request = module.Commands.Single();

        Assert.Multiple(() =>
        {
            Assert.That(module.Name,
                Is.EqualTo("jdiameter-rfc4006-subset"));
            Assert.That(module.Version,
                Is.EqualTo("7f3909342cca30ffc7e69b7d8b4111f5fde52ece"));
            Assert.That(module.Applications.Single().Id,
                Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(module.EnumValues.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    "INITIAL_REQUEST",
                    "UPDATE_REQUEST",
                    "TERMINATION_REQUEST",
                    "EVENT_REQUEST"
                }));
            Assert.That(requestType.RequiredFlags,
                Is.EqualTo(DiameterAvpFlags.Mandatory));
            Assert.That(grantedUnits.BaseType,
                Is.EqualTo(DiameterAvpBaseType.Grouped));
            Assert.That(grantedUnits.GroupedRules.Single().AvpCode,
                Is.EqualTo(DiameterAvpCodes.CcTime));
            Assert.That(grantedUnits.GroupedRules.Single().MaximumCount,
                Is.EqualTo(1));
            Assert.That(request.Code,
                Is.EqualTo(DiameterCommandCodes.CreditControl));
            Assert.That(request.RequiredFlags,
                Is.EqualTo(
                    DiameterMessageFlags.Request |
                    DiameterMessageFlags.Proxyable));
            Assert.That(request.Rules.Select(item => item.AvpCode),
                Is.EqualTo(new uint[]
                {
                    DiameterAvpCodes.CcRequestType,
                    DiameterAvpCodes.CcRequestNumber,
                    DiameterAvpCodes.GrantedServiceUnit
                }));
        });
    }

    [Test]
    public void Open5gsS6aRequestParsesNestedVendorGroupsAndReencodesExactly()
    {
        byte[] wire = ReadHexCorpus("open5gs-s6a-ulr.hex");
        DiameterMessageCodec codec = new DiameterMessageCodec();

        DiameterDecodeResult decoded = codec.DecodeMessage(
            wire,
            false,
            new DiameterLimitConfiguration());
        DiameterMessage message = decoded.Message!;
        DiameterAvp supportedFeatures = FindAvp(message, 628, ThreeGppVendorId);
        DiameterAvp terminalInformation = FindAvp(message, 1401, ThreeGppVendorId);
        DiameterValueResult supportedChildren = ValueCodecs.DecodeGrouped(
            supportedFeatures.Data,
            CreateGroupedTypes(),
            new DiameterLimitConfiguration());
        DiameterValueResult terminalChildren = ValueCodecs.DecodeGrouped(
            terminalInformation.Data,
            CreateGroupedTypes(),
            new DiameterLimitConfiguration());
        DiameterEncodeResult encoded = codec.EncodeMessageToArray(message);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoded.ConsumedCount, Is.EqualTo(wire.Length));
            Assert.That(message.CommandCode, Is.EqualTo(316));
            Assert.That(message.ApplicationId, Is.EqualTo(16777251));
            Assert.That(message.Flags,
                Is.EqualTo(
                    DiameterMessageFlags.Request |
                    DiameterMessageFlags.Proxyable));
            Assert.That(ReadText(message, DiameterAvpCodes.UserName, 0),
                Is.EqualTo("001010123456789"));
            Assert.That(ReadUnsigned32(message, 1405, ThreeGppVendorId),
                Is.EqualTo(1));
            Assert.That(ReadUnsigned32(message, 1032, ThreeGppVendorId),
                Is.EqualTo(1004));
            Assert.That(FindAvp(message, 1407, ThreeGppVendorId).Data,
                Is.EqualTo(new byte[] { 0x00, 0xf1, 0x10 }));
            Assert.That(supportedChildren.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(supportedChildren.Avps.Select(item =>
                    item.VendorId + ":" + item.Code),
                Is.EqualTo(new[] { "0:266", "10415:629", "10415:630" }));
            Assert.That(ReadUnsigned32(
                    supportedChildren.Avps.Single(item => item.Code == 630)),
                Is.EqualTo(3));
            Assert.That(terminalChildren.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(Encoding.UTF8.GetString(
                    terminalChildren.Avps.Single(item => item.Code == 1402).Data),
                Is.EqualTo("356789012345678"));
            Assert.That(Encoding.UTF8.GetString(
                    terminalChildren.Avps.Single(item => item.Code == 1403).Data),
                Is.EqualTo("01"));
            Assert.That(encoded.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encoded.Data, Is.EqualTo(wire));
        });
    }

    private static List<GroupedAvpType> CreateGroupedTypes()
    {
        List<GroupedAvpType> result = new List<GroupedAvpType>();
        GroupedAvpType supportedFeatures = new GroupedAvpType();
        supportedFeatures.Code = 628;
        supportedFeatures.VendorId = ThreeGppVendorId;
        result.Add(supportedFeatures);
        GroupedAvpType terminalInformation = new GroupedAvpType();
        terminalInformation.Code = 1401;
        terminalInformation.VendorId = ThreeGppVendorId;
        result.Add(terminalInformation);
        return result;
    }

    private static string ReadText(
        DiameterMessage message,
        uint code,
        uint vendorId)
    {
        return Encoding.UTF8.GetString(FindAvp(message, code, vendorId).Data);
    }

    private static uint ReadUnsigned32(
        DiameterMessage message,
        uint code,
        uint vendorId)
    {
        return ReadUnsigned32(FindAvp(message, code, vendorId));
    }

    private static uint ReadUnsigned32(DiameterAvp avp)
    {
        Assert.That(avp.Data.Length, Is.EqualTo(4));
        return BinaryPrimitives.ReadUInt32BigEndian(avp.Data);
    }

    private static DiameterAvp FindAvp(
        DiameterMessage message,
        uint code,
        uint vendorId)
    {
        DiameterAvp? result = message.Avps.Find(
            item => item.Code == code && item.VendorId == vendorId);
        Assert.That(result, Is.Not.Null,
            "Expected AVP " + vendorId + ":" + code + ".");
        return result!;
    }

    private static byte[] ReadHexCorpus(string name)
    {
        string[] lines = File.ReadAllLines(FindCorpusFile(name));
        List<byte> bytes = new List<byte>();
        int lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            string line = lines[lineIndex];
            int comment = line.IndexOf('#');
            if (comment >= 0)
            {
                line = line.Substring(0, comment);
            }

            string[] tokens = line.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);
            int tokenIndex = 0;
            while (tokenIndex < tokens.Length)
            {
                bytes.Add(Convert.ToByte(tokens[tokenIndex], 16));
                tokenIndex += 1;
            }

            lineIndex += 1;
        }

        return bytes.ToArray();
    }

    private static string FindCorpusFile(string name)
    {
        DirectoryInfo? current = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (current != null &&
            !File.Exists(Path.Combine(current.FullName, "todo.md")))
        {
            current = current.Parent;
        }

        Assert.That(current, Is.Not.Null,
            "Could not locate the repository root.");
        return Path.Combine(
            current!.FullName,
            "tests",
            "Interoperability",
            "Corpus",
            name);
    }
}
