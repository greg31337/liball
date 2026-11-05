using System.Text;
using iso20022;

namespace iso20022.Tests;

public class SupplementaryDataFaqTests
{
    private const string CoreNamespace =
        "urn:iso:std:iso:20022:tech:xsd:seev.031.001.03";
    private const string ExtensionNamespace =
        "urn:iso:std:iso:20022:tech:xsd:supl.001.001.01";

    [Test]
    public void OfficialGlobalExampleResolvesQualifiedLocationAndPreservesPayload()
    {
        string xml =
            "<core:Document xmlns:core=\"" + CoreNamespace + "\" " +
            "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\">" +
            "<core:CorpActnNtfctn><core:CorpActnOptnDtls>" +
            "<core:CshMvmntDtls>first</core:CshMvmntDtls>" +
            "<core:CshMvmntDtls>second</core:CshMvmntDtls>" +
            "</core:CorpActnOptnDtls></core:CorpActnNtfctn>" +
            "<core:SplmtryData><core:PlcAndNm>" +
            "core:CorpActnNtfctn/core:CorpActnOptnDtls[1]/" +
            "core:CshMvmntDtls[2]</core:PlcAndNm><core:Envlp>" +
            "<ext:Document><ext:CashMvmntDtls>" +
            "<ext:MyExtensionData>This extends the second occurrence" +
            "</ext:MyExtensionData></ext:CashMvmntDtls></ext:Document>" +
            "</core:Envlp></core:SplmtryData></core:Document>";

        SupplementaryValidationResult result = Validate(xml);
        SupplementaryDataItem item = result.Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Valid, Is.True);
            Assert.That(item.NamespaceUri, Is.EqualTo(ExtensionNamespace));
            Assert.That(item.PlaceAndName,
                Is.EqualTo("core:CorpActnNtfctn/" +
                    "core:CorpActnOptnDtls[1]/core:CshMvmntDtls[2]"));
            Assert.That(item.LocationPaths, Has.Count.EqualTo(1));
            Assert.That(item.LocationPath.Value,
                Does.Contain("CorpActnOptnDtls").And
                    .EndWith("CshMvmntDtls[2]"));
            Assert.That(item.Payload, Is.Not.Null);
            Assert.That(item.Payload!.Xml,
                Does.Contain("This extends the second occurrence"));
            Assert.That(item.Payload.Xml,
                Does.Contain("xmlns:core=\"" + CoreNamespace + "\""));
            Assert.That(item.Payload.Xml,
                Does.Contain("xmlns:ext=\"" + ExtensionNamespace + "\""));
            Assert.That(item.Payload.Utf8Bytes,
                Is.EqualTo(Encoding.UTF8.GetBytes(item.Payload.Xml)));
        });
    }

    [Test]
    public void OfficialLegacyExtensionOneExampleRetainsRepeatedTextParts()
    {
        string path =
            "CorpActnNtfctn/CorpActnOptnDtls/CshMvmntDtls";
        string xml =
            "<Document xmlns=\"" + CoreNamespace + "\">" +
            "<CorpActnNtfctn><CorpActnOptnDtls>" +
            "<CshMvmntDtls>Information that is extended</CshMvmntDtls>" +
            "</CorpActnOptnDtls></CorpActnNtfctn>" +
            "<Xtsn><PlcAndNm>" + path + "</PlcAndNm>" +
            "<Txt>part 1 of the extension</Txt></Xtsn>" +
            "<Xtsn><PlcAndNm>" + path + "</PlcAndNm>" +
            "<Txt>part 2 of the extension</Txt></Xtsn></Document>";

        SupplementaryValidationResult result = Validate(xml);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.Items.Select(value => value.IsLegacyForm),
                Is.All.True);
            Assert.That(result.Items.Select(value => value.OccurrenceIndex),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(result.Items[0].LocationPath.Value,
                Is.EqualTo(result.Items[1].LocationPath.Value));
            Assert.That(result.Items[0].Payload!.Xml,
                Does.Contain("part 1 of the extension"));
            Assert.That(result.Items[1].Payload!.Xml,
                Does.Contain("part 2 of the extension"));
        });
    }

    [Test]
    public void OfficialRepetitionExampleSupportsExactAndAllOccurrences()
    {
        string xml =
            "<core:Document xmlns:core=\"" + CoreNamespace + "\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\">" +
            "<core:CorpActnNtfctn>" +
            "<core:CorpActnOptnDtls>" +
            "<core:CshMvmntDtls>one</core:CshMvmntDtls>" +
            "<core:CshMvmntDtls>two</core:CshMvmntDtls>" +
            "<core:CshMvmntDtls>three</core:CshMvmntDtls>" +
            "</core:CorpActnOptnDtls><core:CorpActnOptnDtls>" +
            "<core:CshMvmntDtls>four</core:CshMvmntDtls>" +
            "</core:CorpActnOptnDtls></core:CorpActnNtfctn>" +
            SupplementaryXml(
                "core:CorpActnNtfctn/core:CorpActnOptnDtls[1]/" +
                "core:CshMvmntDtls[2]",
                "exact") +
            SupplementaryXml(
                "core:CorpActnNtfctn/core:CorpActnOptnDtls/" +
                "core:CshMvmntDtls",
                "all") +
            "</core:Document>";

        SupplementaryValidationResult result = Validate(xml);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.Items[0].LocationPaths, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].LocationPath.Value,
                Does.Contain("CorpActnOptnDtls[1]").And
                    .EndWith("CshMvmntDtls[2]"));
            Assert.That(result.Items[1].LocationPaths, Has.Count.EqualTo(4));
            Assert.That(result.Items[1].LocationPaths[0].Value,
                Does.Contain("CorpActnOptnDtls[1]").And
                    .EndWith("CshMvmntDtls[1]"));
            Assert.That(result.Items[1].LocationPaths[3].Value,
                Does.Contain("CorpActnOptnDtls[2]").And
                    .EndWith("CshMvmntDtls"));
        });
    }

    [Test]
    public void OfficialCanDoPolicyDistinguishesUnknownIgnoredAndRequiredExtensions()
    {
        string xml = PolicyMessage();
        ProfileDefinition unknownProfile = new ProfileDefinition();
        unknownProfile.SupplementaryDataPolicies.Add(
            new SupplementaryDataPolicy
            {
                NamespaceUri = "urn:example:another-extension"
            });
        ProfileDefinition ignoredProfile = new ProfileDefinition();
        ignoredProfile.SupplementaryDataPolicies.Add(
            new SupplementaryDataPolicy
            {
                NamespaceUri = ExtensionNamespace,
                CanIgnore = true
            });
        ProfileDefinition requiredProfile = new ProfileDefinition();
        requiredProfile.SupplementaryDataPolicies.Add(
            new SupplementaryDataPolicy
            {
                NamespaceUri = ExtensionNamespace,
                MustUnderstand = true
            });
        ISupplementaryDataValidator service = new SupplementaryDataValidator();

        SupplementaryValidationResult unknown =
            service.ValidateSupplementaryData(Request(xml, unknownProfile));
        SupplementaryValidationResult ignored =
            service.ValidateSupplementaryData(Request(xml, ignoredProfile));
        SupplementaryValidationResult required =
            service.ValidateSupplementaryData(Request(xml, requiredProfile));

        Assert.Multiple(() =>
        {
            Assert.That(unknown.Valid, Is.False);
            Assert.That(unknown.Items.Single().Status,
                Is.EqualTo(SupplementaryValidationStatus.UnknownNamespace));
            Assert.That(unknown.Items.Single().Payload!.Xml,
                Does.Contain("<ext:MyNewElement>abc</ext:MyNewElement>"));
            Assert.That(ignored.Valid, Is.True);
            Assert.That(ignored.Status,
                Is.EqualTo(SupplementaryValidationStatus.Ignored));
            Assert.That(ignored.Items.Single().CanIgnore, Is.True);
            Assert.That(required.Valid, Is.False);
            Assert.That(required.Items.Single().Status,
                Is.EqualTo(
                    SupplementaryValidationStatus.MustUnderstandRejected));
            Assert.That(required.Diagnostics.Single().Code,
                Is.EqualTo("supplementary.must_understand_rejected"));
        });
    }

    [Test]
    public void FaqNamespaceAndXPathRulesRejectUnqualifiedPayloadAndInvalidPath()
    {
        string unqualified =
            "<core:Document xmlns:core=\"" + CoreNamespace + "\">" +
            "<core:Target/><core:SplmtryData>" +
            "<core:PlcAndNm>core:Target</core:PlcAndNm>" +
            "<core:Envlp><Document><Value>extension</Value></Document>" +
            "</core:Envlp></core:SplmtryData></core:Document>";
        string invalidPath =
            "<core:Document xmlns:core=\"" + CoreNamespace + "\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\">" +
            "<core:Target/>" + SupplementaryXml("core:Target[", "invalid") +
            "</core:Document>";

        SupplementaryValidationResult namespaceResult = Validate(unqualified);
        SupplementaryValidationResult xpathResult = Validate(invalidPath);

        Assert.Multiple(() =>
        {
            Assert.That(namespaceResult.Success, Is.True,
                namespaceResult.ErrorMessage);
            Assert.That(namespaceResult.Valid, Is.False);
            Assert.That(namespaceResult.Items.Single().Status,
                Is.EqualTo(SupplementaryValidationStatus.Invalid));
            Assert.That(namespaceResult.Diagnostics.Single().Code,
                Is.EqualTo("supplementary.payload_namespace_missing"));
            Assert.That(namespaceResult.Items.Single().Payload!.Xml,
                Does.Contain("<Document"));
            Assert.That(xpathResult.Success, Is.True, xpathResult.ErrorMessage);
            Assert.That(xpathResult.Valid, Is.False);
            Assert.That(xpathResult.Items.Single().Status,
                Is.EqualTo(SupplementaryValidationStatus.InvalidLocation));
            Assert.That(xpathResult.Diagnostics.Single().Code,
                Is.EqualTo("supplementary.place_and_name_invalid"));
        });
    }

    private static SupplementaryValidationResult Validate(string xml)
    {
        return new SupplementaryDataValidator().ValidateSupplementaryData(
            new SupplementaryValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes(xml)
            });
    }

    private static SupplementaryValidationRequest Request(
        string xml,
        ProfileDefinition profile)
    {
        return new SupplementaryValidationRequest
        {
            Xml = Encoding.UTF8.GetBytes(xml),
            Profile = profile
        };
    }

    private static string PolicyMessage()
    {
        return "<core:Document xmlns:core=\"" + CoreNamespace + "\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\">" +
            "<core:Target/><core:SplmtryData>" +
            "<core:PlcAndNm>core:Target</core:PlcAndNm>" +
            "<core:Envlp><ext:Document>" +
            "<ext:MyNewElement>abc</ext:MyNewElement>" +
            "</ext:Document></core:Envlp></core:SplmtryData>" +
            "</core:Document>";
    }

    private static string SupplementaryXml(string path, string value)
    {
        return "<core:SplmtryData><core:PlcAndNm>" + path +
            "</core:PlcAndNm><core:Envlp><ext:Document>" +
            "<ext:MyNewElement>" + value + "</ext:MyNewElement>" +
            "</ext:Document></core:Envlp></core:SplmtryData>";
    }
}
