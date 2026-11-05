using System.IO;
using System.Text;
using iso20022;

namespace iso20022.Tests;

public class SupplementaryDataValidatorTests
{
    private const string CoreNamespace =
        "urn:iso:std:iso:20022:tech:xsd:seev.031.001.03";
    private const string ExtensionNamespace =
        "urn:iso:std:iso:20022:tech:xsd:supl.001.001.01";

    [Test]
    public void RawDiscoveryPreservesGlobalLocalRepeatingNamespaceCompletePayloads()
    {
        string xml =
            "<core:Document xmlns:core=\"" + CoreNamespace + "\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\" " +
            "xmlns:meta=\"urn:example:metadata\">" +
            "<core:Tx><core:Value>local target</core:Value>" +
            "<core:SplmtryData><core:Envlp>" +
            "<ext:Document meta:id=\"local\"><ext:Value>one</ext:Value>" +
            "</ext:Document></core:Envlp></core:SplmtryData></core:Tx>" +
            "<core:SplmtryData><core:PlcAndNm> core:Tx/core:Value </core:PlcAndNm>" +
            "<core:Envlp><ext:Document meta:id=\"first\">" +
            "<!--inside--><ext:Value><![CDATA[two<]]></ext:Value>" +
            "<ext:SplmtryData><ext:Envlp><ext:Document/></ext:Envlp>" +
            "</ext:SplmtryData></ext:Document></core:Envlp></core:SplmtryData>" +
            "<core:SplmtryData><core:PlcAndNm>core:Tx[1]</core:PlcAndNm>" +
            "<core:Envlp><ext:Document meta:id=\"second\"><ext:Value>three" +
            "</ext:Value></ext:Document></core:Envlp></core:SplmtryData>" +
            "</core:Document>";
        SupplementaryValidationRequest request = new SupplementaryValidationRequest
        {
            Xml = Encoding.UTF8.GetBytes(xml)
        };
        ISupplementaryDataValidator service = new SupplementaryDataValidator();

        SupplementaryValidationResult result =
            service.ValidateSupplementaryData(request);
        SupplementaryDataItem local = result.Items[0];
        SupplementaryDataItem firstGlobal = result.Items[1];
        SupplementaryDataItem secondGlobal = result.Items[2];
        MessageReadResult preservedRead = new XmlMessageReader().ReadGenericText(
            firstGlobal.Payload!.Xml,
            PreserveReadOptions());
        DynamicXmlNode preservedRoot = preservedRead.Document!.Payloads[0].GenericContent!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Status, Is.EqualTo(SupplementaryValidationStatus.Valid));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Items, Has.Count.EqualTo(3));
            Assert.That(result.Items.Select(item => item.IsLocal),
                Is.EqualTo(new[] { true, false, false }));
            Assert.That(result.Items.Select(item => item.OccurrenceIndex),
                Is.EqualTo(new[] { 0, 0, 1 }));
            Assert.That(local.PlaceAndName, Is.Empty);
            Assert.That(firstGlobal.PlaceAndName, Is.EqualTo("core:Tx/core:Value"));
            Assert.That(secondGlobal.PlaceAndName, Is.EqualTo("core:Tx[1]"));
            Assert.That(local.ContainerPath.Value,
                Does.EndWith("/Q{" + CoreNamespace + "}SplmtryData"));
            Assert.That(local.ContainerPath.Segments.Select(value => value.LocalName),
                Is.EqualTo(new[] { "Document", "Tx", "SplmtryData" }));
            Assert.That(local.LocationPath.Value,
                Does.EndWith("/Q{" + CoreNamespace + "}Tx"));
            Assert.That(firstGlobal.LocationPath.Value,
                Does.EndWith("/Q{" + CoreNamespace + "}Value"));
            Assert.That(firstGlobal.LocationPaths, Has.Count.EqualTo(1));
            Assert.That(secondGlobal.LocationPath.Value,
                Does.EndWith("/Q{" + CoreNamespace + "}Tx"));
            Assert.That(firstGlobal.EnvelopeName.LocalName, Is.EqualTo("Envlp"));
            Assert.That(firstGlobal.PayloadName.LocalName, Is.EqualTo("Document"));
            Assert.That(firstGlobal.NamespaceUri, Is.EqualTo(ExtensionNamespace));
            Assert.That(firstGlobal.Envelope!.Xml, Does.Contain("<core:Envlp"));
            Assert.That(firstGlobal.Payload.Xml, Does.Contain("xmlns:core="));
            Assert.That(firstGlobal.Payload.Xml, Does.Contain("xmlns:ext="));
            Assert.That(firstGlobal.Payload.Xml, Does.Contain("xmlns:meta="));
            Assert.That(firstGlobal.Payload.Xml, Does.Contain("meta:id=\"first\""));
            Assert.That(firstGlobal.Payload.Xml, Does.Contain("<!--inside-->"));
            Assert.That(firstGlobal.Payload.Xml, Does.Contain("<![CDATA[two<]]>"));
            Assert.That(firstGlobal.Payload.NamespaceDeclarations.Select(value =>
                    value.Prefix),
                Is.EqualTo(new[] { "core", "ext", "meta" }));
            Assert.That(firstGlobal.Payload.Utf8Bytes,
                Is.EqualTo(Encoding.UTF8.GetBytes(firstGlobal.Payload.Xml)));
            Assert.That(preservedRead.Success, Is.True, preservedRead.ErrorMessage);
            Assert.That(preservedRoot.Name.NamespaceUri,
                Is.EqualTo(ExtensionNamespace));
            Assert.That(FindElement(preservedRoot, "Value")!.Children[0].Kind,
                Is.EqualTo(XmlNodeKind.CData));
        });
    }

    [Test]
    public void GenericDocumentDiscoveryPreservesLegacyExtension1AndExtension2WithoutMutation()
    {
        string xml =
            "<Document xmlns=\"" + CoreNamespace + "\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\">" +
            "<CorpActnNtfctn><Value>target</Value></CorpActnNtfctn>" +
            "<Xtsn><PlcAndNm>CorpActnNtfctn/Value</PlcAndNm>" +
            "<Txt>part 1 of the extension</Txt></Xtsn>" +
            "<Xtsn><PlcAndNm>CorpActnNtfctn/Value</PlcAndNm>" +
            "<Txt><![CDATA[part 2 <of> the extension]]></Txt></Xtsn>" +
            "<Xtsn><PlcAndNm>CorpActnNtfctn</PlcAndNm><Envlp>" +
            "<ext:Document><ext:Structured>value</ext:Structured></ext:Document>" +
            "</Envlp></Xtsn></Document>";
        MessageReadResult read = new XmlMessageReader().ReadGenericText(
            xml,
            PreserveReadOptions());
        MessageDocument document = read.Document!;
        MessageWriteOptions writeOptions = PreserveWriteOptions();
        string before = new XmlMessageWriter().WriteGenericString(
            document,
            writeOptions).Text;
        SupplementaryValidationRequest request = new SupplementaryValidationRequest
        {
            Document = document
        };
        ISupplementaryDataValidator service = new SupplementaryDataValidator();

        SupplementaryValidationResult result =
            service.ValidateSupplementaryData(request);
        string after = new XmlMessageWriter().WriteGenericString(
            document,
            writeOptions).Text;
        SupplementaryDataItem first = result.Items[0];
        SupplementaryDataItem second = result.Items[1];
        SupplementaryDataItem structured = result.Items[2];
        SupplementaryValidationRequest metadataOnly =
            new SupplementaryValidationRequest
            {
                Document = document,
                PreserveContent = false
            };
        SupplementaryValidationResult withoutContent =
            service.ValidateSupplementaryData(metadataOnly);

        Assert.Multiple(() =>
        {
            Assert.That(read.Success, Is.True, read.ErrorMessage);
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Items, Has.Count.EqualTo(3));
            Assert.That(result.Items.Select(value => value.IsLegacyForm),
                Is.All.True);
            Assert.That(result.Items.Select(value => value.IsLocal),
                Is.All.False);
            Assert.That(result.Items.Select(value => value.OccurrenceIndex),
                Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(first.LocationPath.Value,
                Does.EndWith("/Q{" + CoreNamespace + "}Value"));
            Assert.That(second.LocationPath.Value,
                Is.EqualTo(first.LocationPath.Value));
            Assert.That(structured.LocationPath.Value,
                Does.EndWith("/Q{" + CoreNamespace + "}CorpActnNtfctn"));
            Assert.That(first.PayloadName.LocalName, Is.EqualTo("Txt"));
            Assert.That(first.NamespaceUri, Is.Empty);
            Assert.That(first.Envelope, Is.Null);
            Assert.That(first.EnvelopeName.LocalName, Is.Empty);
            Assert.That(first.Payload!.Xml,
                Does.Contain("part 1 of the extension"));
            Assert.That(second.Payload!.Xml,
                Does.Contain("<![CDATA[part 2 <of> the extension]]>"));
            Assert.That(structured.EnvelopeName.LocalName, Is.EqualTo("Envlp"));
            Assert.That(structured.Envelope, Is.Not.Null);
            Assert.That(structured.PayloadName.LocalName, Is.EqualTo("Document"));
            Assert.That(structured.NamespaceUri, Is.EqualTo(ExtensionNamespace));
            Assert.That(structured.Payload!.Xml,
                Does.Contain("<ext:Structured>value</ext:Structured>"));
            Assert.That(before, Is.EqualTo(after));
            Assert.That(withoutContent.Success, Is.True,
                withoutContent.ErrorMessage);
            Assert.That(withoutContent.Items, Has.Count.EqualTo(3));
            Assert.That(withoutContent.Items.Select(value => value.Envelope),
                Is.All.Null);
            Assert.That(withoutContent.Items.Select(value => value.Payload),
                Is.All.Null);
            Assert.That(withoutContent.Items[2].PayloadName.LocalName,
                Is.EqualTo("Document"));
        });
    }

    [Test]
    public void DiscoveryReportsAbsentMalformedAndBoundedOutcomesDeterministically()
    {
        ISupplementaryDataValidator service = new SupplementaryDataValidator();
        SupplementaryValidationResult absent = service.ValidateSupplementaryData(
            new SupplementaryValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes(
                    "<Document xmlns=\"" + CoreNamespace + "\"><Value/></Document>")
            });
        SupplementaryValidationResult missingEnvelope =
            service.ValidateSupplementaryData(
                new SupplementaryValidationRequest
                {
                    Xml = Encoding.UTF8.GetBytes(
                        "<Document xmlns=\"" + CoreNamespace + "\">" +
                        "<Value/><SplmtryData><PlcAndNm>Value</PlcAndNm>" +
                        "</SplmtryData></Document>")
                });
        SupplementaryValidationResult ambiguousPayload =
            service.ValidateSupplementaryData(
                new SupplementaryValidationRequest
                {
                    Xml = Encoding.UTF8.GetBytes(
                        "<Document xmlns=\"" + CoreNamespace + "\" " +
                        "xmlns:ext=\"" + ExtensionNamespace + "\">" +
                        "<SplmtryData><Envlp><ext:One/><ext:Two/>" +
                        "</Envlp></SplmtryData></Document>")
                });
        SupplementaryValidationResult nullRequest =
            service.ValidateSupplementaryData(null);
        SupplementaryValidationResult noInput =
            service.ValidateSupplementaryData(new SupplementaryValidationRequest());
        SupplementaryValidationRequest invalidLimitsRequest =
            new SupplementaryValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes("<Document/>")
            };
        invalidLimitsRequest.Limits.MaxDepth = 0;
        SupplementaryValidationResult invalidLimits =
            service.ValidateSupplementaryData(invalidLimitsRequest);
        MessageDocument boundedDocument = new XmlMessageReader().ReadGenericText(
            "<Document xmlns=\"" + CoreNamespace + "\"><One/><Two/></Document>",
            PreserveReadOptions()).Document!;
        SupplementaryValidationRequest boundedRequest =
            new SupplementaryValidationRequest
            {
                Document = boundedDocument
            };
        boundedRequest.Limits.MaxCollectionCount = 1;
        SupplementaryValidationResult bounded =
            service.ValidateSupplementaryData(boundedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Success, Is.True, absent.ErrorMessage);
            Assert.That(absent.Valid, Is.True);
            Assert.That(absent.Status,
                Is.EqualTo(SupplementaryValidationStatus.NotPresent));
            Assert.That(absent.Items, Is.Empty);
            Assert.That(missingEnvelope.Success, Is.True,
                missingEnvelope.ErrorMessage);
            Assert.That(missingEnvelope.Valid, Is.False);
            Assert.That(missingEnvelope.Status,
                Is.EqualTo(SupplementaryValidationStatus.Invalid));
            Assert.That(missingEnvelope.Items.Single().Status,
                Is.EqualTo(SupplementaryValidationStatus.Invalid));
            Assert.That(missingEnvelope.Diagnostics.Single().Code,
                Is.EqualTo("supplementary.envelope_missing"));
            Assert.That(missingEnvelope.Diagnostics.Single().Stage,
                Is.EqualTo(ValidationStage.SupplementaryData));
            Assert.That(missingEnvelope.Diagnostics.Single().Path.Value,
                Does.EndWith("SplmtryData"));
            Assert.That(ambiguousPayload.Success, Is.True,
                ambiguousPayload.ErrorMessage);
            Assert.That(ambiguousPayload.Valid, Is.False);
            Assert.That(ambiguousPayload.Diagnostics.Single().Code,
                Is.EqualTo("supplementary.payload_ambiguous"));
            Assert.That(nullRequest.Success, Is.False);
            Assert.That(nullRequest.ErrorCode,
                Is.EqualTo("supplementary.request_missing"));
            Assert.That(noInput.Success, Is.False);
            Assert.That(noInput.ErrorCode,
                Is.EqualTo("supplementary.input_missing"));
            Assert.That(invalidLimits.Success, Is.False);
            Assert.That(invalidLimits.ErrorCode,
                Is.EqualTo("supplementary.limits_invalid"));
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.Valid, Is.False);
            Assert.That(bounded.Status,
                Is.EqualTo(SupplementaryValidationStatus.ResourceLimitReached));
            Assert.That(bounded.ErrorCode,
                Is.EqualTo("supplementary.collection_limit"));
        });
    }

    [Test]
    public void PlaceAndNameResolvesQualifiedRelativeAbsoluteAndRepeatedTargets()
    {
        string xml =
            "<core:Document xmlns:core=\"" + CoreNamespace + "\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\">" +
            "<core:Group><core:Item><core:Value>one</core:Value></core:Item>" +
            "<core:Item><core:Value>two</core:Value></core:Item></core:Group>" +
            SupplementaryXml("core:Group/core:Item[2]/core:Value") +
            SupplementaryXml("core:Group/core:Item/core:Value") +
            SupplementaryXml("/core:Document/core:Group/core:Item[1]") +
            "</core:Document>";
        SupplementaryValidationResult result =
            new SupplementaryDataValidator().ValidateSupplementaryData(
                new SupplementaryValidationRequest
                {
                    Xml = Encoding.UTF8.GetBytes(xml)
                });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Items, Has.Count.EqualTo(3));
            Assert.That(result.Items[0].LocationPaths, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].LocationPath.Value,
                Does.Contain("Item[2]").And.EndsWith("Value"));
            Assert.That(result.Items[1].LocationPaths, Has.Count.EqualTo(2));
            Assert.That(result.Items[1].LocationPaths[0].Value,
                Does.Contain("Item[1]").And.EndsWith("Value"));
            Assert.That(result.Items[1].LocationPaths[1].Value,
                Does.Contain("Item[2]").And.EndsWith("Value"));
            Assert.That(result.Items[2].LocationPaths, Has.Count.EqualTo(1));
            Assert.That(result.Items[2].LocationPath.Value,
                Does.StartWith("/Q{" + CoreNamespace + "}Document").And
                    .Contain("Item[1]"));
        });
    }

    [Test]
    public void PlaceAndNameReportsInvalidUnsupportedPrefixAndNoMatchSeparately()
    {
        string xml =
            "<core:Document xmlns:core=\"" + CoreNamespace + "\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\">" +
            "<core:Group><core:Value/></core:Group>" +
            SupplementaryXml("core:Group[") +
            SupplementaryXml("core:Group//core:Value") +
            SupplementaryXml("missing:Group") +
            SupplementaryXml("core:Absent") +
            "</core:Document>";
        SupplementaryValidationResult result =
            new SupplementaryDataValidator().ValidateSupplementaryData(
                new SupplementaryValidationRequest
                {
                    Xml = Encoding.UTF8.GetBytes(xml)
                });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Items.Select(value => value.Status),
                Is.EqualTo(new[]
                {
                    SupplementaryValidationStatus.InvalidLocation,
                    SupplementaryValidationStatus.UnsupportedLocation,
                    SupplementaryValidationStatus.InvalidLocation,
                    SupplementaryValidationStatus.InvalidLocation
                }));
            Assert.That(result.Diagnostics.Select(value => value.Code),
                Is.EqualTo(new[]
                {
                    "supplementary.place_and_name_invalid",
                    "supplementary.place_and_name_unsupported",
                    "supplementary.place_and_name_prefix_unresolved",
                    "supplementary.place_and_name_not_found"
                }));
            Assert.That(result.Diagnostics[0].Kind,
                Is.EqualTo(DiagnosticKind.InvalidValue));
            Assert.That(result.Diagnostics[1].Kind,
                Is.EqualTo(DiagnosticKind.UnsupportedConstraint));
        });
    }

    [Test]
    public void PlaceAndNameStopsWhenResolvedTargetsExceedTheCollectionLimit()
    {
        string xml =
            "<core:Document xmlns:core=\"" + CoreNamespace + "\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\">" +
            "<core:Group><core:Branch><core:Value/><core:Value/></core:Branch>" +
            "<core:Branch><core:Value/></core:Branch></core:Group>" +
            SupplementaryXml("core:Group/core:Branch/core:Value") +
            "</core:Document>";
        SupplementaryValidationRequest request =
            new SupplementaryValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes(xml)
            };
        request.Limits.MaxCollectionCount = 2;

        SupplementaryValidationResult result =
            new SupplementaryDataValidator().ValidateSupplementaryData(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Status,
                Is.EqualTo(SupplementaryValidationStatus.ResourceLimitReached));
            Assert.That(result.ErrorCode,
                Is.EqualTo("supplementary.location_match_limit"));
        });
    }

    [Test]
    public void ProfilePolicyResolvesRegisteredSchemaAndValidatesPayload()
    {
        ISchemaRegistry registry = new SchemaRegistry();
        SchemaArtifact schema = RegisterExtensionSchema(registry);
        ProfileDefinition profile = new ProfileDefinition
        {
            Identifier = "profile.supplementary",
            Version = "2026"
        };
        profile.SupplementaryDataPolicies.Add(new SupplementaryDataPolicy
        {
            NamespaceUri = ExtensionNamespace,
            Schema = schema,
            MustUnderstand = true
        });
        ISupplementaryDataValidator service = new SupplementaryDataValidator();
        SupplementaryValidationResult valid =
            service.ValidateSupplementaryData(
                new SupplementaryValidationRequest
                {
                    Xml = Encoding.UTF8.GetBytes(PolicyMessage("OK")),
                    Profile = profile,
                    SchemaRegistry = registry,
                    XsdValidator = new XsdValidator(registry)
                });
        SupplementaryValidationResult invalid =
            service.ValidateSupplementaryData(
                new SupplementaryValidationRequest
                {
                    Xml = Encoding.UTF8.GetBytes(PolicyMessage("TOO-LONG")),
                    Profile = profile,
                    SchemaRegistry = registry,
                    XsdValidator = new XsdValidator(registry)
                });

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True, valid.ErrorMessage);
            Assert.That(valid.Valid, Is.True);
            Assert.That(valid.Diagnostics, Is.Empty);
            Assert.That(valid.Items.Single().Schema, Is.SameAs(schema));
            Assert.That(valid.Items.Single().MustUnderstand, Is.True);
            Assert.That(valid.Items.Single().LocationPath.Value,
                Does.EndWith("/Q{" + CoreNamespace + "}Target"));
            Assert.That(invalid.Success, Is.True, invalid.ErrorMessage);
            Assert.That(invalid.Valid, Is.False);
            Assert.That(invalid.Status,
                Is.EqualTo(SupplementaryValidationStatus.SchemaInvalid));
            Assert.That(invalid.Items.Single().Status,
                Is.EqualTo(SupplementaryValidationStatus.SchemaInvalid));
            Assert.That(invalid.Diagnostics.Single().Code,
                Is.EqualTo("supplementary.schema_invalid"));
            Assert.That(invalid.Diagnostics.Single().SourceArtifact,
                Is.SameAs(schema.Artifact));
            Assert.That(invalid.Diagnostics.Single().ProfileIdentifier,
                Is.EqualTo(profile.Identifier));
        });
    }

    [Test]
    public void CallerPolicyOverridesProfileIgnoreAndMustUnderstandOutcomes()
    {
        ProfileDefinition profile = new ProfileDefinition
        {
            Identifier = "profile.ignore",
            Version = "1"
        };
        profile.SupplementaryDataPolicies.Add(new SupplementaryDataPolicy
        {
            NamespaceUri = ExtensionNamespace,
            CanIgnore = true
        });
        SupplementaryValidationRequest ignoredRequest =
            new SupplementaryValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes(PolicyMessage("OK")),
                Profile = profile,
                PreserveContent = false
            };
        SupplementaryValidationRequest requiredRequest =
            new SupplementaryValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes(PolicyMessage("OK")),
                Profile = profile
            };
        requiredRequest.Policies.Add(new SupplementaryDataPolicy
        {
            NamespaceUri = ExtensionNamespace,
            MustUnderstand = true
        });
        SupplementaryValidationRequest unavailableRequest =
            new SupplementaryValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes(PolicyMessage("OK"))
            };
        unavailableRequest.Policies.Add(new SupplementaryDataPolicy
        {
            NamespaceUri = ExtensionNamespace
        });
        ISupplementaryDataValidator service = new SupplementaryDataValidator();

        SupplementaryValidationResult ignored =
            service.ValidateSupplementaryData(ignoredRequest);
        SupplementaryValidationResult required =
            service.ValidateSupplementaryData(requiredRequest);
        SupplementaryValidationResult unavailable =
            service.ValidateSupplementaryData(unavailableRequest);

        Assert.Multiple(() =>
        {
            Assert.That(ignored.Success, Is.True, ignored.ErrorMessage);
            Assert.That(ignored.Valid, Is.True);
            Assert.That(ignored.Status,
                Is.EqualTo(SupplementaryValidationStatus.Ignored));
            Assert.That(ignored.Items.Single().Status,
                Is.EqualTo(SupplementaryValidationStatus.Ignored));
            Assert.That(ignored.Items.Single().CanIgnore, Is.True);
            Assert.That(ignored.Items.Single().Payload, Is.Null);
            Assert.That(required.Success, Is.True, required.ErrorMessage);
            Assert.That(required.Valid, Is.False);
            Assert.That(required.Items.Single().MustUnderstand, Is.True);
            Assert.That(required.Items.Single().CanIgnore, Is.False);
            Assert.That(required.Items.Single().Status,
                Is.EqualTo(SupplementaryValidationStatus.MustUnderstandRejected));
            Assert.That(required.Diagnostics.Single().Code,
                Is.EqualTo("supplementary.must_understand_rejected"));
            Assert.That(required.Diagnostics.Single().ProfileIdentifier, Is.Empty);
            Assert.That(unavailable.Success, Is.True, unavailable.ErrorMessage);
            Assert.That(unavailable.Valid, Is.False);
            Assert.That(unavailable.Items.Single().Status,
                Is.EqualTo(SupplementaryValidationStatus.SchemaUnavailable));
            Assert.That(unavailable.Diagnostics.Single().Code,
                Is.EqualTo("supplementary.schema_unavailable"));
        });
    }

    [Test]
    public void ConfiguredUnknownNamespaceAndDisabledSchemaValidationAreDistinct()
    {
        ProfileDefinition unknownProfile = new ProfileDefinition
        {
            Identifier = "profile.closed"
        };
        unknownProfile.SupplementaryDataPolicies.Add(new SupplementaryDataPolicy
        {
            NamespaceUri = "urn:example:different-extension",
            ValidateSchema = false
        });
        SupplementaryValidationResult unknown =
            new SupplementaryDataValidator().ValidateSupplementaryData(
                new SupplementaryValidationRequest
                {
                    Xml = Encoding.UTF8.GetBytes(PolicyMessage("OK")),
                    Profile = unknownProfile
                });
        SchemaArtifact selected = new SchemaArtifact
        {
            NamespaceUri = ExtensionNamespace
        };
        ProfileDefinition optionalProfile = new ProfileDefinition();
        optionalProfile.SupplementaryDataPolicies.Add(
            new SupplementaryDataPolicy
            {
                NamespaceUri = ExtensionNamespace,
                Schema = selected,
                MustUnderstand = true
            });
        SupplementaryValidationResult disabled =
            new SupplementaryDataValidator().ValidateSupplementaryData(
                new SupplementaryValidationRequest
                {
                    Xml = Encoding.UTF8.GetBytes(PolicyMessage("OK")),
                    Profile = optionalProfile,
                    ValidateSchemas = false,
                    PreserveContent = false
                });

        Assert.Multiple(() =>
        {
            Assert.That(unknown.Success, Is.True, unknown.ErrorMessage);
            Assert.That(unknown.Valid, Is.False);
            Assert.That(unknown.Items.Single().Status,
                Is.EqualTo(SupplementaryValidationStatus.UnknownNamespace));
            Assert.That(unknown.Diagnostics.Single().Code,
                Is.EqualTo("supplementary.namespace_unknown"));
            Assert.That(disabled.Success, Is.True, disabled.ErrorMessage);
            Assert.That(disabled.Valid, Is.True);
            Assert.That(disabled.Items.Single().Status,
                Is.EqualTo(SupplementaryValidationStatus.Valid));
            Assert.That(disabled.Items.Single().Schema, Is.SameAs(selected));
            Assert.That(disabled.Items.Single().MustUnderstand, Is.True);
            Assert.That(disabled.Items.Single().Payload, Is.Null);
        });
    }

    [Test]
    public void InvalidConfigurationAndResourceBoundariesProduceStableDiagnostics()
    {
        SupplementaryDataValidator validator = new SupplementaryDataValidator();
        SupplementaryValidationRequest noLimits = new SupplementaryValidationRequest
            { Limits = null! };
        SupplementaryValidationRequest noPolicies = new SupplementaryValidationRequest
            { Policies = null! };
        SupplementaryValidationRequest badProfile = new SupplementaryValidationRequest
        {
            Profile = new ProfileDefinition { SupplementaryDataPolicies = null! }
        };
        SupplementaryValidationRequest nullXml = new SupplementaryValidationRequest
            { Xml = null! };
        SupplementaryValidationRequest tooLarge = new SupplementaryValidationRequest
        {
            Xml = "<x/>"u8.ToArray(),
            Limits = new ResourceLimits { MaxTotalBytes = 1 }
        };
        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateSupplementaryData(noLimits).ErrorCode,
                Is.EqualTo("supplementary.limits_invalid"));
            Assert.That(validator.ValidateSupplementaryData(noPolicies).ErrorCode,
                Is.EqualTo("supplementary.policy_invalid"));
            Assert.That(validator.ValidateSupplementaryData(badProfile).ErrorCode,
                Is.EqualTo("supplementary.policy_invalid"));
            Assert.That(validator.ValidateSupplementaryData(nullXml).ErrorCode,
                Is.EqualTo("supplementary.xml_invalid"));
            Assert.That(validator.ValidateSupplementaryData(tooLarge).ErrorCode,
                Is.EqualTo("supplementary.total_bytes_limit"));
            Assert.That(validator.ValidateSupplementaryData(
                new SupplementaryValidationRequest()).ErrorCode,
                Is.EqualTo("supplementary.input_missing"));
        });

        DynamicXmlNode node = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "x" }
        };
        SupplementaryDiscoveryState depth = CreateDiscoveryState();
        depth.Request.Limits.MaxDepth = 1;
        SupplementaryDiscoveryState elements = CreateDiscoveryState();
        elements.Request.Limits.MaxElementCount = 0;
        SupplementaryDiscoveryState attributes = CreateDiscoveryState();
        attributes.Request.Limits.MaxAttributesPerElement = 0;
        node.Attributes.Add(new XmlAttributeData { Value = "x" });
        SupplementaryDiscoveryState collection = CreateDiscoveryState();
        collection.Request.Limits.MaxCollectionCount = 1;
        node.Children.Add(new DynamicXmlNode());
        node.Children.Add(new DynamicXmlNode());
        SupplementaryDiscoveryState attributeText = CreateDiscoveryState();
        attributeText.Request.Limits.MaxTextLength = 0;

        Assert.Multiple(() =>
        {
            Assert.That(SupplementaryDataValidator.RecordElement(node, depth, 2), Is.False);
            Assert.That(SupplementaryDataValidator.RecordElement(
                new DynamicXmlNode(), elements, 1), Is.False);
            Assert.That(SupplementaryDataValidator.RecordElement(node, attributes, 1), Is.False);
            Assert.That(SupplementaryDataValidator.RecordElement(node, collection, 1), Is.False);
            Assert.That(SupplementaryDataValidator.RecordElement(
                new DynamicXmlNode
                {
                    Attributes = new List<XmlAttributeData>
                        { new XmlAttributeData { Value = "x" } }
                }, attributeText, 1), Is.False);
        });
        SupplementaryDiscoveryState valueText = CreateDiscoveryState();
        valueText.Request.Limits.MaxTextLength = 0;
        SupplementaryDataValidator.ValidateValueNode(
            new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = "x" }, valueText);
        Assert.That(valueText.Result.ErrorCode, Is.EqualTo("supplementary.text_limit"));

        DynamicXmlNode siblings = new DynamicXmlNode();
        DynamicXmlNode first = new DynamicXmlNode
            { Name = new QualifiedNameData { LocalName = "SplmtryData", NamespaceUri = "n" } };
        DynamicXmlNode second = new DynamicXmlNode
            { Name = new QualifiedNameData { LocalName = "SplmtryData", NamespaceUri = "n" } };
        siblings.Children.Add(first);
        siblings.Children.Add(second);
        Assert.Multiple(() =>
        {
            Assert.That(SupplementaryDataValidator.CountEarlierSupplementarySiblings(
                siblings, -1), Is.Zero);
            Assert.That(SupplementaryDataValidator.CountEarlierSupplementarySiblings(
                siblings, 1), Is.EqualTo(1));
        });
    }

    private static SupplementaryDiscoveryState CreateDiscoveryState()
    {
        SupplementaryValidationRequest request = new SupplementaryValidationRequest();
        DynamicXmlNode tree = new DynamicXmlNode { Kind = XmlNodeKind.Document };
        return new SupplementaryDiscoveryState(
            request,
            new SupplementaryValidationResult { Success = true, Valid = true },
            tree);
    }

    private static MessageReadOptions PreserveReadOptions()
    {
        return new MessageReadOptions
        {
            NamespaceMode = NamespaceMode.Strict,
            UnknownContentMode = UnknownContentMode.Preserve,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveWhitespace = true,
            PreserveLexicalValues = true
        };
    }

    private static MessageWriteOptions PreserveWriteOptions()
    {
        return new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Compact,
            UnknownContentMode = UnknownContentMode.Preserve,
            WriteXmlDeclaration = false,
            PreservePrefixes = true,
            PreserveLexicalValues = true
        };
    }

    private static string SupplementaryXml(string expression)
    {
        return "<core:SplmtryData><core:PlcAndNm>" + expression +
            "</core:PlcAndNm><core:Envlp><ext:Document/>" +
            "</core:Envlp></core:SplmtryData>";
    }

    private static string PolicyMessage(string code)
    {
        return "<core:Document xmlns:core=\"" + CoreNamespace + "\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\">" +
            "<core:Target/><core:SplmtryData>" +
            "<core:PlcAndNm>core:Target</core:PlcAndNm>" +
            "<core:Envlp><ext:Document><ext:Code>" + code +
            "</ext:Code></ext:Document></core:Envlp>" +
            "</core:SplmtryData></core:Document>";
    }

    private static SchemaArtifact RegisterExtensionSchema(ISchemaRegistry registry)
    {
        string schemaText =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" " +
            "targetNamespace=\"" + ExtensionNamespace + "\" " +
            "xmlns:ext=\"" + ExtensionNamespace + "\" " +
            "elementFormDefault=\"qualified\">" +
            "<xs:element name=\"Document\"><xs:complexType><xs:sequence>" +
            "<xs:element name=\"Code\"><xs:simpleType><xs:restriction " +
            "base=\"xs:string\"><xs:maxLength value=\"3\"/>" +
            "</xs:restriction></xs:simpleType></xs:element>" +
            "</xs:sequence></xs:complexType></xs:element></xs:schema>";
        SchemaArtifact identity = new SchemaArtifact
        {
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Schema,
                    Edition = "2026",
                    Identifier = "supplementary-test",
                    Version = "1"
                },
                OriginUri = "file:///supplementary-test.xsd",
                IsApproved = true
            },
            SourceName = "supplementary-test.xsd",
            BaseUri = "file:///supplementary-test.xsd",
            NamespaceUri = ExtensionNamespace,
            Version = "1",
            IsEntryPoint = true
        };
        SchemaRegistryOptions options = new SchemaRegistryOptions
        {
            ResolveIncludes = false,
            ResolveImports = false
        };
        SchemaCompilationResult imported =
            new SchemaImporter().ImportSchemaStream(
                new MemoryStream(Encoding.UTF8.GetBytes(schemaText)),
                identity,
                options);
        Assert.That(imported.Success, Is.True, imported.ErrorMessage);
        SchemaArtifact schema = imported.RootSchema ??
            throw new AssertionException(imported.ErrorMessage);
        SchemaCompilationResult registered =
            registry.RegisterSchema(schema, options);
        Assert.That(registered.Success, Is.True, registered.ErrorMessage);
        return schema;
    }

    private static DynamicXmlNode? FindElement(
        DynamicXmlNode node,
        string localName)
    {
        if (node.Kind == XmlNodeKind.Element &&
            node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            DynamicXmlNode? found = FindElement(node.Children[index], localName);
            if (found != null)
            {
                return found;
            }
            index++;
        }
        return null;
    }
}
