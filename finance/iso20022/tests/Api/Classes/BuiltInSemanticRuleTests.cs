using iso20022;

namespace iso20022.Tests;

public class BuiltInSemanticRuleTests
{
    private const string MessageNamespace =
        "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10";

    [Test]
    public void ConsistentGenericCrossFieldsPassEveryBuiltInSemanticRule()
    {
        MessageDocument document = ReadDocument(CreatePaymentXml(
            "2",
            "30.00",
            "2026-08-31T12:00:00Z",
            "UNIQUE-1",
            "UNIQUE-2",
            "USD",
            "USD",
            "BIZ-1",
            "BIZ-2"));
        document.Header = CreateHeader("pacs.008.001.10");
        ValidationRequest request = CreateRequest(document);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.True);
            Assert.That(report.Diagnostics, Is.Empty);
            Assert.That(report.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.Semantic }));
        });
    }

    [Test]
    public void InvalidGenericCrossFieldsReportEveryBuiltInRuleFamilyInStableOrder()
    {
        MessageDocument document = ReadDocument(CreatePaymentXml(
            "3",
            "25.00",
            "2026-09-03T12:00:00Z",
            "DUPLICATE",
            "DUPLICATE",
            "USD",
            "EUR",
            "BIZ-DUPLICATE",
            "BIZ-DUPLICATE"));
        document.Header = CreateHeader("pacs.009.001.10");
        ValidationRequest request = CreateRequest(document);
        request.Policy.RedactOffendingValues = false;

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.Diagnostics.Select(item => item.Code), Is.EqualTo(new[]
            {
                "validation.semantic.transaction_count",
                "validation.semantic.control_sum",
                "validation.semantic.amount_currency_mixed",
                "validation.semantic.chronology",
                "validation.semantic.reference_duplicate",
                "validation.semantic.header_body_identifier",
                "validation.semantic.business_identifier_duplicate"
            }));
            Assert.That(report.Diagnostics.Select(item => item.RuleIdentifier),
                Is.EqualTo(new[]
                {
                    "iso20022.semantic.transaction_count",
                    "iso20022.semantic.control_sum",
                    "iso20022.semantic.amount_currency",
                    "iso20022.semantic.chronology",
                    "iso20022.semantic.reference_unique",
                    "iso20022.semantic.header_body",
                    "iso20022.semantic.business_id_unique"
                }));
            Assert.That(report.Diagnostics.Select(item => item.SourceIdentifier),
                Has.All.EqualTo("iso20022.builtin"));
            Assert.That(report.Diagnostics.Select(item => item.Stage),
                Has.All.EqualTo(ValidationStage.Semantic));
            Assert.That(report.Diagnostics.Select(item => item.Path.Value),
                Has.All.Not.Empty);
            Assert.That(report.ErrorCount, Is.EqualTo(7));
            Assert.That(report.WarningCount, Is.Zero);
        });
    }

    [Test]
    public void ProfilesCanRemoveAndOverrideBuiltInSemanticRulesByStableIdentifier()
    {
        MessageDocument document = ReadDocument(CreateCountOnlyXml("2"));
        ValidationRequest request = CreateRequest(document);
        ProfileRuleReference reference = new ProfileRuleReference
        {
            RuleIdentifier = "iso20022.semantic.transaction_count",
            Enabled = true,
            RemoveInheritedRule = true
        };
        request.Profile = new ProfileDefinition
        {
            Identifier = "profile-a",
            Version = "2026",
            Artifact = new ArtifactManifest { IsApproved = true }
        };
        request.Profile.Rules.Add(reference);

        ValidationReport removed = new MessageValidator().ValidateMessage(request);
        reference.RemoveInheritedRule = false;
        reference.HasSeverityOverride = true;
        reference.Severity = ValidationSeverity.Warning;
        ValidationReport overridden = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(removed.Success, Is.True, removed.ErrorMessage);
            Assert.That(removed.Valid, Is.True);
            Assert.That(removed.Diagnostics, Is.Empty);
            Assert.That(overridden.Success, Is.True, overridden.ErrorMessage);
            Assert.That(overridden.Valid, Is.True);
            Assert.That(overridden.WarningCount, Is.EqualTo(1));
            Assert.That(overridden.ErrorCount, Is.Zero);
            Assert.That(overridden.Diagnostics.Single().RuleIdentifier,
                Is.EqualTo("iso20022.semantic.transaction_count"));
            Assert.That(overridden.Diagnostics.Single().Severity,
                Is.EqualTo(ValidationSeverity.Warning));
            Assert.That(overridden.Diagnostics.Single().ProfileIdentifier,
                Is.EqualTo("profile-a"));
        });
    }

    private static ValidationRequest CreateRequest(MessageDocument document)
    {
        ValidationRequest request = new ValidationRequest
        {
            Document = document,
            ExpectedIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10"
            },
            ExpectedNamespaceUri = MessageNamespace
        };
        request.Policy.Stages.Add(ValidationStage.Semantic);
        return request;
    }

    private static MessageDocument ReadDocument(string xml)
    {
        MessageReadResult result = new XmlMessageReader().ReadAutoText(
            xml,
            new MessageReadOptions());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Document, Is.Not.Null);
        return result.Document!;
    }

    private static MessageHeader CreateHeader(string messageDefinition)
    {
        DynamicXmlNode header = CreateElement("AppHdr");
        DynamicXmlNode messageDefinitionElement = CreateElement("MsgDefIdr");
        DynamicXmlNode text = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Text,
            Value = messageDefinition
        };
        messageDefinitionElement.Children.Add(text);
        header.Children.Add(messageDefinitionElement);
        return new MessageHeader
        {
            GenericContent = header
        };
    }

    private static DynamicXmlNode CreateElement(string localName)
    {
        return new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData
            {
                LocalName = localName
            }
        };
    }

    private static string CreatePaymentXml(
        string transactionCount,
        string controlSum,
        string creationDate,
        string firstReference,
        string secondReference,
        string firstCurrency,
        string secondCurrency,
        string firstBusinessIdentifier,
        string secondBusinessIdentifier)
    {
        return "<Document xmlns='" + MessageNamespace + "'>" +
            "<FIToFICstmrCdtTrf>" +
            "<BizMsgIdr>" + firstBusinessIdentifier + "</BizMsgIdr>" +
            "<BizMsgIdr>" + secondBusinessIdentifier + "</BizMsgIdr>" +
            "<GrpHdr><MsgId>GROUP-1</MsgId><NbOfTxs>" + transactionCount +
            "</NbOfTxs><CtrlSum>" + controlSum + "</CtrlSum><CreDtTm>" +
            creationDate + "</CreDtTm></GrpHdr>" +
            CreateTransactionXml(firstReference, firstCurrency, "10.00", "2026-09-01") +
            CreateTransactionXml(secondReference, secondCurrency, "20.00", "2026-09-02") +
            "</FIToFICstmrCdtTrf></Document>";
    }

    private static string CreateCountOnlyXml(string transactionCount)
    {
        return "<Document xmlns='" + MessageNamespace + "'>" +
            "<FIToFICstmrCdtTrf><GrpHdr><MsgId>GROUP-1</MsgId><NbOfTxs>" +
            transactionCount + "</NbOfTxs></GrpHdr>" +
            CreateTransactionXml("UNIQUE", "USD", "10.00", "2026-09-01") +
            "</FIToFICstmrCdtTrf></Document>";
    }

    private static string CreateTransactionXml(
        string reference,
        string currency,
        string amount,
        string settlementDate)
    {
        return "<CdtTrfTxInf><PmtId><InstrId>" + reference +
            "</InstrId></PmtId><IntrBkSttlmAmt Ccy='" + currency + "'>" + amount +
            "</IntrBkSttlmAmt><IntrBkSttlmDt>" + settlementDate +
            "</IntrBkSttlmDt></CdtTrfTxInf>";
    }
}
