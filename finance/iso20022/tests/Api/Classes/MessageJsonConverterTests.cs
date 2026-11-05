using System.Text.Json;
using iso20022;

namespace iso20022.Tests;

public class MessageJsonConverterTests
{
    private const string NamespaceUri =
        "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10";
    private const string SchemaIdentifier =
        "urn:iso:std:iso:20022:tech:json:pacs.008.001.10";

    [Test]
    public void GenericConversionCarriesIdentityFormsChoicesArraysAndAmountAttributes()
    {
        string xml =
            "<Document xmlns=\"" + NamespaceUri + "\"><PmtInf>" +
            "<CreDt>2026-08-31</CreDt><CutOffTm>12:34:56.120Z</CutOffTm>" +
            "<CreDtTm>2026-08-31T12:34:56.120Z</CreDtTm>" +
            "<SgntrData>AQI=</SgntrData><Acct><IBAN>GB82WEST</IBAN></Acct>" +
            "<Sts>ACCP</Sts><Sts>RJCT</Sts>" +
            "<Amt Ccy=\"USD\">12.30</Amt></PmtInf></Document>";
        MessageReadResult read = new XmlMessageReader().ReadGenericText(
            xml,
            new MessageReadOptions());
        IMessageJsonConverter converter = new MessageJsonConverter();

        MessageJsonResult written = converter.WriteGenericDocument(read.Document);
        using JsonDocument parsed = JsonDocument.Parse(written.Json);
        JsonElement top = parsed.RootElement;
        JsonElement payment = top.GetProperty("Document").GetProperty("PmtInf");
        MessageJsonResult restored = converter.ReadGenericText(written.Json);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True, written.ErrorMessage);
            Assert.That(written.SchemaIdentifier, Is.EqualTo(SchemaIdentifier));
            Assert.That(written.NamespaceUri, Is.EqualTo(NamespaceUri));
            Assert.That(written.IsLossless, Is.True);
            Assert.That(top.GetProperty("$schema").GetString(),
                Is.EqualTo(SchemaIdentifier));
            Assert.That(top.GetProperty("$namespace").GetString(),
                Is.EqualTo(NamespaceUri));
            Assert.That(payment.GetProperty("CreDt").GetString(),
                Is.EqualTo("2026-08-31"));
            Assert.That(payment.GetProperty("CutOffTm").GetString(),
                Is.EqualTo("12:34:56.120Z"));
            Assert.That(payment.GetProperty("CreDtTm").GetString(),
                Is.EqualTo("2026-08-31T12:34:56.120Z"));
            Assert.That(payment.GetProperty("SgntrData").GetString(),
                Is.EqualTo("AQI="));
            Assert.That(payment.GetProperty("Acct").GetProperty("IBAN").GetString(),
                Is.EqualTo("GB82WEST"));
            Assert.That(payment.GetProperty("Sts").GetArrayLength(), Is.EqualTo(2));
            Assert.That(payment.GetProperty("Amt").GetProperty("@Ccy").GetString(),
                Is.EqualTo("USD"));
            Assert.That(payment.GetProperty("Amt").GetProperty("$value").GetString(),
                Is.EqualTo("12.30"));
            Assert.That(restored.Success, Is.True, restored.ErrorMessage);
            Assert.That(restored.Xml, Does.Contain("<CreDt>2026-08-31</CreDt>")
                .And.Contain("<Sts>ACCP</Sts><Sts>RJCT</Sts>")
                .And.Contain("Ccy=\"USD\">12.30</Amt>"));
            Assert.That(restored.Document?.Metadata.NamespaceUri,
                Is.EqualTo(NamespaceUri));
        });
    }

    [Test]
    public void WildcardConversionReportsEveryKnownXmlInformationLoss()
    {
        string xml =
            "<Document xmlns=\"" + NamespaceUri + "\" xmlns:ext=\"urn:extension\" " +
            "xmlns:meta=\"urn:meta\"><SplmtryData><ext:Extension meta:id=\"17\">" +
            "<!--inside--><![CDATA[value<]]></ext:Extension></SplmtryData></Document>";
        MessageReadOptions options = new MessageReadOptions();
        options.PreserveComments = true;
        MessageReadResult read = new XmlMessageReader().ReadGenericText(xml, options);

        MessageJsonResult result = new MessageJsonConverter().WriteGenericDocument(
            read.Document);
        using JsonDocument parsed = JsonDocument.Parse(result.Json);
        JsonElement extension = parsed.RootElement.GetProperty("Document")
            .GetProperty("SplmtryData").GetProperty("Extension");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.IsLossless, Is.False);
            Assert.That(result.Diagnostics.Select(value => value.Code), Does.Contain(
                "json.wildcard_namespace_loss"));
            Assert.That(result.Diagnostics.Select(value => value.Code), Does.Contain(
                "json.wildcard_attribute_namespace_loss"));
            Assert.That(result.Diagnostics.Select(value => value.Code), Does.Contain(
                "json.wildcard_node_kind_loss"));
            Assert.That(result.Diagnostics, Has.Some.Matches<MessageJsonDiagnostic>(value =>
                value.Path == "/Document/SplmtryData/Extension/@id"));
            Assert.That(extension.GetProperty("@id").GetString(), Is.EqualTo("17"));
            Assert.That(extension.GetProperty("$value").GetString(),
                Is.EqualTo("value<"));
        });
    }

    [Test]
    public void JsonReadAcceptsNativeBooleanAndNumberButRejectsIdentityMismatch()
    {
        string json = "{\"$schema\":\"" + SchemaIdentifier + "\"," +
            "\"$namespace\":\"" + NamespaceUri + "\",\"Document\":{" +
            "\"Flag\":true,\"Rate\":12.30}}";
        IMessageJsonConverter converter = new MessageJsonConverter();

        MessageJsonResult valid = converter.ReadGenericText(json);
        MessageJsonResult mismatch = converter.ReadGenericText(json.Replace(
            SchemaIdentifier,
            "urn:iso:std:iso:20022:tech:json:pacs.009.001.10",
            StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True, valid.ErrorMessage);
            Assert.That(valid.Xml, Does.Contain("<Flag>true</Flag>")
                .And.Contain("<Rate>12.30</Rate>"));
            Assert.That(mismatch.Success, Is.False);
            Assert.That(mismatch.ErrorCode,
                Is.EqualTo("json.schema_namespace_mismatch"));
        });
    }

    [Test]
    public void JsonBoundaryShapesAndIdentityHelpersAreDeterministic()
    {
        MessageJsonConverter converter = new MessageJsonConverter();
        Assert.Multiple(() =>
        {
            Assert.That(converter.WriteGenericDocument(new MessageDocument()).ErrorCode,
                Is.EqualTo("json.content_missing"));
            Assert.That(converter.ReadGenericText("[]").ErrorCode,
                Is.EqualTo("json.root_invalid"));
            Assert.That(converter.ReadGenericText("{\"$schema\":\"x\",\"$namespace\":\"n\",\"a\":{},\"b\":{}}")
                .ErrorCode, Is.EqualTo("json.root_ambiguous"));
            Assert.That(converter.ReadGenericText("{\"a\":{}}").ErrorCode,
                Is.EqualTo("json.identity_missing"));
            Assert.That(converter.ReadGenericText("{\"$schema\":\"n#json\",\"$namespace\":\"n\"}")
                .ErrorCode, Is.EqualTo("json.content_missing"));
            Assert.That(converter.ReadGenericText("{\"$schema\":\"n#json\",\"$namespace\":\"n\",\"a\":[]}")
                .ErrorCode, Is.EqualTo("json.root_invalid"));
            Assert.That(MessageJsonConverter.FindFirstElementNode(null), Is.Null);
            Assert.That(MessageJsonConverter.FindFirstElementNode(
                new DynamicXmlNode { Kind = XmlNodeKind.Comment }), Is.Null);
            Assert.That(MessageJsonConverter.CreateSchemaIdentifier("urn:x", "pacs.008.001.10"),
                Is.EqualTo("urn:iso:std:iso:20022:tech:json:pacs.008.001.10"));
            Assert.That(MessageJsonConverter.CreateSchemaIdentifier("urn:x", ""),
                Is.EqualTo("urn:x#json"));
        });

        MessageDocument payloadIdentity = new MessageDocument();
        payloadIdentity.Payloads.Add(new MessagePayload());
        payloadIdentity.Payloads.Add(new MessagePayload
        {
            Identifier = new MessageIdentifier { Value = "pacs.008.001.10" }
        });
        Assert.That(MessageJsonConverter.FindMessageIdentifierValue(payloadIdentity),
            Is.EqualTo("pacs.008.001.10"));
        Assert.That(MessageJsonConverter.FindMessageIdentifierValue(new MessageDocument()),
            Is.Empty);

        using JsonDocument values = JsonDocument.Parse(
            "{\"nil\":null,\"true\":true,\"false\":false,\"number\":12,\"reserved\":{\"$unknown\":1}}");
        MessageJsonResult result = new MessageJsonResult { IsLossless = true };
        DynamicXmlNode nil = MessageJsonConverter.CreateElementFromJsonValue(
            "Nil", "urn:test", values.RootElement.GetProperty("nil"), "/Nil", result);
        DynamicXmlNode reserved = MessageJsonConverter.CreateElementFromJsonValue(
            "Reserved", "urn:test", values.RootElement.GetProperty("reserved"),
            "/Reserved", result);
        Assert.Multiple(() =>
        {
            Assert.That(nil.IsNil, Is.True);
            Assert.That(reserved.Children, Is.Empty);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain("json.reserved_property_loss"));
            Assert.That(MessageJsonConverter.ReadScalarText(
                values.RootElement.GetProperty("true")), Is.EqualTo("true"));
            Assert.That(MessageJsonConverter.ReadScalarText(
                values.RootElement.GetProperty("false")), Is.EqualTo("false"));
            Assert.That(MessageJsonConverter.ReadScalarText(
                values.RootElement.GetProperty("nil")), Is.Empty);
            Assert.That(MessageJsonConverter.ReadScalarText(
                values.RootElement.GetProperty("number")), Is.EqualTo("12"));
        });

        DynamicXmlNode nested = new DynamicXmlNode { Kind = XmlNodeKind.Document };
        nested.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Comment });
        Assert.That(MessageJsonConverter.FindFirstElementNode(nested), Is.Null);

        DynamicXmlNode complex = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "Root", NamespaceUri = "urn:test" }
        };
        complex.Attributes.Add(new XmlAttributeData
        {
            Name = new QualifiedNameData
            {
                LocalName = "nil",
                NamespaceUri = Iso20022Namespaces.XmlSchemaInstance
            },
            Value = "false"
        });
        complex.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = "before" });
        complex.Children.Add(new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "Child", NamespaceUri = "urn:other" },
            IsNil = true
        });
        complex.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = "after" });
        using MemoryStream output = new MemoryStream();
        using (Utf8JsonWriter writer = new Utf8JsonWriter(output))
        {
            MessageJsonConverter.WriteElementJsonValue(
                writer, complex, "urn:test", "/Root", result);
        }
        string written = System.Text.Encoding.UTF8.GetString(output.ToArray());
        Assert.Multiple(() =>
        {
            Assert.That(written, Does.Contain("\"Child\":null"));
            Assert.That(written, Does.Contain("\"$value\":\"beforeafter\""));
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain("json.wildcard_namespace_loss")
                    .And.Contain("json.wildcard_mixed_content_loss"));
        });
    }
}
