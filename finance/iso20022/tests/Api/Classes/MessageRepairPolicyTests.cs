using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class MessageRepairPolicyTests
{
    [Test]
    public void MalformedDeclarationsAreRejectedStrictlyAndRemovedWithDiagnosticsLeniently()
    {
        string xml = "\n<?XML version='1.0'?><Document xmlns='" + Namespace("pacs.008.001.10") + "'/>";
        string duplicate = "<?xml version='1.0'?><?xml version='1.0'?>" + Document("pacs.008.001.10");
        string invalidValue = "<?xml version='1.0' standalone='maybe'?>" + Document("pacs.008.001.10");
        IXmlMessageReader reader = new XmlMessageReader();
        IMessageDetector detector = new MessageDetector();

        Assert.Throws<XmlException>(() => reader.ReadAutoText(xml, new MessageReadOptions()));
        MessageReadResult textResult = reader.ReadAutoText(xml, LenientOptions());
        MessageReadResult byteResult = reader.ReadAutoBytes(Encoding.UTF8.GetBytes(xml), LenientOptions());
        MessageReadResult duplicateResult = reader.ReadAutoText(duplicate, LenientOptions());
        MessageReadResult invalidValueResult = reader.ReadAutoText(invalidValue, LenientOptions());
        MessageDetectionResult detection = detector.DetectText(xml, LenientOptions());

        Assert.Multiple(() =>
        {
            Assert.That(textResult.Success, Is.True);
            Assert.That(byteResult.Success, Is.True);
            Assert.That(duplicateResult.Success, Is.True);
            Assert.That(invalidValueResult.Success, Is.True);
            Assert.That(detection.Success, Is.True);
            Assert.That(textResult.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "xml.malformed_declaration_removed" }));
            Assert.That(byteResult.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "xml.malformed_declaration_removed" }));
            Assert.That(detection.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "xml.malformed_declaration_removed" }));
            Assert.That(duplicateResult.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "xml.malformed_declaration_removed" }));
            Assert.That(invalidValueResult.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "xml.malformed_declaration_removed" }));
            Assert.That(textResult.Repairs[0].OriginalValue, Is.EqualTo("<?XML version='1.0'?>"));
            Assert.That(textResult.Repairs[0].RepairedValue, Is.Empty);
            Assert.That(textResult.Repairs[0].Location?.LineNumber, Is.EqualTo(2));
            Assert.That(textResult.Repairs[0].Location?.ColumnNumber, Is.EqualTo(1));
            Assert.That(textResult.BytesConsumed, Is.EqualTo(Encoding.UTF8.GetByteCount(xml)));
        });
    }

    [Test]
    public void SiblingRootsWithDeclarationRetainTheDeclarationAndAreDiagnosedLeniently()
    {
        string xml = "<?xml version='1.0'?>" + Header("pacs.008.001.10") + Document("pacs.008.001.10");
        IMessageDetector detector = new MessageDetector();
        IXmlMessageReader reader = new XmlMessageReader();

        MessageDetectionResult strict = detector.DetectText(xml, new MessageReadOptions());
        MessageDetectionResult detection = detector.DetectText(xml, LenientOptions());
        MessageReadResult result = reader.ReadAutoText(xml, LenientOptions());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.True);
            Assert.That(strict.Repairs, Is.Empty);
            Assert.That(detection.Success, Is.True);
            Assert.That(detection.Layout, Is.EqualTo(EnvelopeKind.SiblingHeaderAndDocument));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Document?.Layout, Is.EqualTo(EnvelopeKind.SiblingHeaderAndDocument));
            Assert.That(detection.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "xml.sibling_roots_wrapped" }));
            Assert.That(result.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "xml.sibling_roots_wrapped" }));
            Assert.That(result.Repairs[0].Path?.Value, Does.Contain("AppHdr"));
            Assert.That(result.Repairs[0].Location?.HasLineInformation, Is.True);
        });
    }

    [Test]
    public void CleanSiblingLayoutRemainsSupportedStrictlyAndIsExplicitlyDiagnosedLeniently()
    {
        string xml = Header("pacs.008.001.10") + Document("pacs.008.001.10");
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult strict = detector.DetectText(xml, new MessageReadOptions());
        MessageDetectionResult lenient = detector.DetectText(xml, LenientOptions());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.True);
            Assert.That(strict.Repairs, Is.Empty);
            Assert.That(lenient.Success, Is.True);
            Assert.That(lenient.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "xml.sibling_roots_wrapped" }));
        });
    }

    [Test]
    public void NamespacePrefixRepairsHaveSpecificCodesPathsLocationsAndValues()
    {
        string originalNamespace = "URN:ISO:STD:ISO:20022:TECH:XSD:PACS.008.001.10";
        string xml = "<Document xmlns='" + originalNamespace + "'/>";
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult strict = detector.DetectText(xml, new MessageReadOptions());
        MessageDetectionResult lenient = detector.DetectText(xml, LenientOptions());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(lenient.Success, Is.True);
            Assert.That(lenient.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(lenient.Repairs.Select(value => value.Code), Is.EqualTo(new[]
            {
                "namespace.technical_prefix_case_normalized",
                "namespace.business_area_case_normalized"
            }));
            Assert.That(lenient.Repairs[0].OriginalValue, Is.EqualTo(originalNamespace));
            Assert.That(lenient.Repairs[1].RepairedValue, Is.EqualTo(Namespace("pacs.008.001.10")));
            Assert.That(lenient.Repairs[0].Path?.Value, Does.Contain("Document"));
            Assert.That(lenient.Repairs[0].Location?.LineNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void WrapperInferenceAndConfiguredNamespaceMismatchAreLenientAndFullyDiagnosed()
    {
        string xml = "<w:Batch xmlns:w='urn:source:batch'>" + Document("pacs.008.001.10") + "</w:Batch>";
        MessageReadOptions configured = LenientOptions();
        configured.CustomWrapperNames.Add(new QualifiedNameData { LocalName = "Batch", NamespaceUri = "urn:configured:batch", Prefix = "batch" });
        IMessageDetector detector = new MessageDetector();
        IXmlMessageReader reader = new XmlMessageReader();

        MessageDetectionResult strict = detector.DetectText(xml, new MessageReadOptions());
        MessageDetectionResult inferred = detector.DetectText(xml, LenientOptions());
        MessageDetectionResult namespaceRepair = detector.DetectText(xml, configured);
        MessageReadResult read = reader.ReadAutoText(xml, configured);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(inferred.Success, Is.True);
            Assert.That(inferred.Layout, Is.EqualTo(EnvelopeKind.CustomWrapper));
            Assert.That(inferred.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "wrapper.inferred" }));
            Assert.That(namespaceRepair.Success, Is.True);
            Assert.That(namespaceRepair.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "wrapper.namespace_repaired" }));
            Assert.That(namespaceRepair.Repairs[0].OriginalValue, Is.EqualTo("urn:source:batch"));
            Assert.That(namespaceRepair.Repairs[0].RepairedValue, Is.EqualTo("urn:configured:batch"));
            Assert.That(namespaceRepair.Repairs[0].Path?.Value, Does.Contain("Batch"));
            Assert.That(namespaceRepair.Repairs[0].Location?.HasLineInformation, Is.True);
            Assert.That(read.Success, Is.True);
            Assert.That(read.Document?.Envelope?.Name.NamespaceUri, Is.EqualTo("urn:source:batch"));
            Assert.That(read.Repairs.Select(value => value.Code), Is.EqualTo(new[] { "wrapper.namespace_repaired" }));
        });
    }

    [Test]
    public void ValidSingleRootDeclarationDoesNotProduceARepairAndDiagnosticLimitIsEnforced()
    {
        string single = "<?xml version='1.0'?>" + Document("pacs.008.001.10");
        string siblings = "<?XML version='1.0'?>" + Header("pacs.008.001.10") + Document("pacs.008.001.10");
        string cdata = "<Root><![CDATA[<?xml version='broken'?>]]></Root>";
        string utf16Text = "<?xml version='1.0' encoding='utf-16'?>" + Document("pacs.008.001.10");
        byte[] utf16 = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(utf16Text)).ToArray();
        MessageReadOptions limited = LenientOptions();
        limited.Limits.MaxDiagnosticCount = 1;
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult result = reader.ReadAutoText(single, LenientOptions());
        MessageReadResult cdataResult = reader.ReadGenericText(cdata, LenientOptions());
        MessageReadResult utf16Result = reader.ReadAutoBytes(utf16, LenientOptions());
        XmlException? exception = Assert.Throws<XmlException>(() => reader.ReadAutoText(siblings, limited));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Repairs, Is.Empty);
            Assert.That(cdataResult.Success, Is.True);
            Assert.That(cdataResult.Repairs, Is.Empty);
            Assert.That(cdataResult.Document?.Payloads[0].GenericContent?.Children[0].Value, Is.EqualTo("<?xml version='broken'?>"));
            Assert.That(utf16Result.Success, Is.True);
            Assert.That(utf16Result.Repairs, Is.Empty);
            Assert.That(utf16Result.BytesConsumed, Is.EqualTo(utf16.Length));
            Assert.That(exception?.Message, Does.Contain("MaxDiagnosticCount"));
        });
    }

    internal static MessageReadOptions LenientOptions()
    {
        return new MessageReadOptions { NamespaceMode = NamespaceMode.Lenient };
    }

    internal static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    internal static string Document(string identifier)
    {
        return "<Document xmlns='" + Namespace(identifier) + "'/>";
    }

    internal static string Header(string identifier)
    {
        return "<h:AppHdr xmlns:h='" + Namespace("head.001.001.04") + "'><h:MsgDefIdr>" + identifier + "</h:MsgDefIdr></h:AppHdr>";
    }
}
