using System.Text;
using iso20022;

namespace iso20022.Tests;

public class MessageBuilderTests
{
    [TestCase("payment-initiation", "pain.001.001.11", "CstmrCdtTrfInitn", "PAY-001")]
    [TestCase("payment-status", "pacs.002.001.16", "FIToFIPmtStsRpt", "ACCP")]
    [TestCase("interbank-credit-transfer", "pacs.008.001.10", "FIToFICstmrCdtTrf", "E2E-001")]
    [TestCase("cash-management", "camt.053.001.13", "BkToCstmrStmt", "ACCOUNT-001")]
    [TestCase("securities", "semt.001.001.04", "SctiesMsgRjctn", "US0378331005")]
    [TestCase("cards", "caaa.001.001.13", "CardMsg", "MERCHANT-001")]
    [TestCase("foreign-exchange", "fxtr.008.001.08", "FXTradStsNtfctn", "1.234500")]
    [TestCase("trade", "tsmt.001.001.03", "Ack", "BASELINE-001")]
    public void BuildsEveryGeneratedSemanticTemplateAndRoundTripsExactIdentity(
        string templateIdentifier,
        string messageIdentifier,
        string expectedRoot,
        string expectedValue)
    {
        BuilderRequest request = CreateRequest(templateIdentifier, messageIdentifier);
        IMessageBuilder builder = new MessageBuilder();

        BuilderResult result = builder.BuildMessage(request);
        MessageReadResult read = new XmlMessageReader().ReadExactBytes(
            result.Xml,
            Identifier(messageIdentifier),
            new MessageReadOptions());
        string xml = Encoding.UTF8.GetString(result.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.TemplateIdentifier, Is.EqualTo(templateIdentifier));
            Assert.That(result.TargetMessageIdentifier?.Value,
                Is.EqualTo(messageIdentifier));
            Assert.That(result.TargetNamespaceUri,
                Is.EqualTo(Namespace(messageIdentifier)));
            Assert.That(result.Document?.Payloads, Has.Count.EqualTo(1));
            Assert.That(result.Document?.Payloads[0].GenericContent?.Children[0]
                .Name.LocalName, Is.EqualTo(expectedRoot));
            Assert.That(result.Document?.Metadata.References, Is.Not.Empty);
            Assert.That(xml, Does.Contain(expectedValue));
            Assert.That(read.Success, Is.True, read.ErrorMessage);
            Assert.That(read.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo(messageIdentifier));
        });
    }

    [TestCase("payment-initiation", "pain.001.001.03", "CstmrCdtTrfInitn")]
    [TestCase("payment-status", "pain.002.001.12", "CstmrPmtStsRpt")]
    [TestCase("interbank-credit-transfer", "pacs.009.001.11", "FICdtTrf")]
    [TestCase("cash-management", "camt.054.001.13", "BkToCstmrDbtCdtNtfctn")]
    [TestCase("securities", "sese.023.001.12", "SctiesMsg")]
    [TestCase("cards", "catm.001.001.13", "CardMsg")]
    [TestCase("foreign-exchange", "fxtr.014.001.05", "FXTradMsg")]
    [TestCase("trade", "tsmt.019.001.04", "TradSvcMsg")]
    public void BuildsAlternateSupportedExactVersions(
        string templateIdentifier,
        string messageIdentifier,
        string expectedRoot)
    {
        BuilderResult result = new MessageBuilder().BuildMessage(
            CreateRequest(templateIdentifier, messageIdentifier));
        MessageReadResult read = new XmlMessageReader().ReadExactBytes(
            result.Xml,
            Identifier(messageIdentifier),
            new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.TargetMessageIdentifier?.Value,
                Is.EqualTo(messageIdentifier));
            Assert.That(result.Document?.Payloads[0].GenericContent?.Children[0]
                .Name.LocalName, Is.EqualTo(expectedRoot));
            Assert.That(read.Success, Is.True, read.ErrorMessage);
            Assert.That(read.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo(messageIdentifier));
        });
    }

    [TestCase("payment-initiation", "MessageIdentifier")]
    [TestCase("payment-initiation", "PaymentInformationIdentifier")]
    [TestCase("payment-initiation", "EndToEndIdentifier")]
    [TestCase("payment-initiation", "Debtor")]
    [TestCase("payment-initiation", "Creditor")]
    [TestCase("payment-status", "MessageIdentifier")]
    [TestCase("payment-status", "OriginalMessageIdentifier")]
    [TestCase("payment-status", "Status")]
    [TestCase("interbank-credit-transfer", "MessageIdentifier")]
    [TestCase("interbank-credit-transfer", "EndToEndIdentifier")]
    [TestCase("interbank-credit-transfer", "TransactionIdentifier")]
    [TestCase("interbank-credit-transfer", "Debtor")]
    [TestCase("interbank-credit-transfer", "Creditor")]
    [TestCase("cash-management", "MessageIdentifier")]
    [TestCase("cash-management", "ReportIdentifier")]
    [TestCase("cash-management", "AccountIdentifier")]
    [TestCase("securities", "MessageIdentifier")]
    [TestCase("securities", "TransactionIdentifier")]
    [TestCase("securities", "SecurityIdentifier")]
    [TestCase("cards", "MessageIdentifier")]
    [TestCase("cards", "TransactionIdentifier")]
    [TestCase("cards", "MerchantIdentifier")]
    [TestCase("foreign-exchange", "MessageIdentifier")]
    [TestCase("foreign-exchange", "TradeIdentifier")]
    [TestCase("trade", "MessageIdentifier")]
    [TestCase("trade", "TradeIdentifier")]
    [TestCase("trade", "BaselineIdentifier")]
    [TestCase("trade", "Buyer")]
    [TestCase("trade", "Seller")]
    public void RejectsEveryMissingRequiredBusinessField(
        string templateIdentifier,
        string fieldName)
    {
        BuilderRequest request = CreateRequest(
            templateIdentifier,
            MessageIdentifierFor(templateIdentifier));
        object input = TemplateInput(request);
        System.Reflection.FieldInfo? field = input.GetType().GetField(fieldName);
        Assert.That(field, Is.Not.Null, "Required public field was not found.");
        field!.SetValue(input, string.Empty);

        BuilderResult result = new MessageBuilder().BuildMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode,
                Is.EqualTo("builder.required_value_missing"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Xml, Is.Empty);
        });
    }

    [TestCase("payment-initiation", "amount")]
    [TestCase("payment-initiation", "date")]
    [TestCase("interbank-credit-transfer", "amount")]
    [TestCase("interbank-credit-transfer", "date")]
    [TestCase("cash-management", "amount")]
    [TestCase("cash-management", "date")]
    [TestCase("securities", "quantity")]
    [TestCase("securities", "date")]
    [TestCase("securities", "amount")]
    [TestCase("cards", "amount")]
    [TestCase("cards", "datetime")]
    [TestCase("foreign-exchange", "bought-amount")]
    [TestCase("foreign-exchange", "sold-amount")]
    [TestCase("foreign-exchange", "rate")]
    [TestCase("foreign-exchange", "trade-date")]
    [TestCase("foreign-exchange", "settlement-date")]
    [TestCase("trade", "amount")]
    [TestCase("trade", "date")]
    public void RejectsEveryInvalidRequiredBusinessValue(
        string templateIdentifier,
        string invalidValue)
    {
        BuilderRequest request = CreateRequest(
            templateIdentifier,
            MessageIdentifierFor(templateIdentifier));
        InvalidateBusinessValue(request, templateIdentifier, invalidValue);

        BuilderResult result = new MessageBuilder().BuildMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("builder.value_invalid"));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("builder.value_invalid"));
            Assert.That(result.Document, Is.Null);
        });
    }

    [Test]
    public void SemanticInputRemainsSupportedAndTemplateCanBeInferred()
    {
        BuilderRequest request = CreateRequest(
            "interbank-credit-transfer",
            "pacs.008.001.10");
        request.TemplateIdentifier = string.Empty;
        request.SemanticInput = request.InterbankTransfer;
        request.InterbankTransfer = null;

        BuilderResult result = new MessageBuilder().BuildMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.TemplateIdentifier,
                Is.EqualTo("interbank-credit-transfer"));
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Contain("<EndToEndId>E2E-001</EndToEndId>"));
        });
    }

    [Test]
    public void RepositoryDefinitionCanSelectVersionSpecificRootAndNamespace()
    {
        const string identifierText = "semt.001.001.04";
        const string legacyNamespace = "urn:swift:xsd:semt.001.001.04";
        BuilderRequest request = CreateRequest("securities", identifierText);
        request.TargetNamespaceUri = legacyNamespace;
        request.Repository = new RepositorySnapshot();
        request.Repository.MessageDefinitions.Add(new MessageDefinitionMetadata
        {
            MessageIdentifier = new MessageIdentifier { Value = identifierText },
            NamespaceUri = legacyNamespace,
            XmlTag = "RepositorySpecificRoot"
        });

        BuilderResult result = new MessageBuilder().BuildMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.TargetNamespaceUri, Is.EqualTo(legacyNamespace));
            Assert.That(result.Document?.Payloads[0].GenericContent?.Children[0]
                .Name.LocalName, Is.EqualTo("RepositorySpecificRoot"));
            Assert.That(Encoding.UTF8.GetString(result.Xml),
                Does.Contain("xmlns=\"" + legacyNamespace + "\""));
        });
    }

    [Test]
    public void RejectsAmbiguousInputFamilyMismatchAndMissingBusinessValues()
    {
        BuilderRequest ambiguous = CreateRequest(
            "payment-initiation",
            "pain.001.001.11");
        ambiguous.PaymentStatus = new PaymentStatusTemplate();
        BuilderRequest wrongFamily = CreateRequest(
            "payment-initiation",
            "pacs.008.001.10");
        BuilderRequest missing = CreateRequest(
            "payment-status",
            "pain.002.001.12");
        missing.PaymentStatus!.Status = string.Empty;

        BuilderResult ambiguousResult = new MessageBuilder().BuildMessage(ambiguous);
        BuilderResult wrongFamilyResult = new MessageBuilder().BuildMessage(wrongFamily);
        BuilderResult missingResult = new MessageBuilder().BuildMessage(missing);

        Assert.Multiple(() =>
        {
            Assert.That(ambiguousResult.ErrorCode,
                Is.EqualTo("builder.semantic_input_ambiguous"));
            Assert.That(wrongFamilyResult.ErrorCode,
                Is.EqualTo("builder.target_family_mismatch"));
            Assert.That(missingResult.ErrorCode,
                Is.EqualTo("builder.required_value_missing"));
            Assert.That(ambiguousResult.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(wrongFamilyResult.Document, Is.Null);
            Assert.That(missingResult.Xml, Is.Empty);
        });
    }

    [Test]
    public void PreservesDecimalScaleAndExposesCompleteValidationFailure()
    {
        BuilderRequest request = CreateRequest(
            "foreign-exchange",
            "fxtr.008.001.08");
        ValidationDiagnostic diagnostic = new ValidationDiagnostic
        {
            Code = "profile.rate_rejected",
            Severity = ValidationSeverity.Error,
            Stage = ValidationStage.Profile
        };
        ValidationReport report = new ValidationReport
        {
            Success = true,
            Valid = false,
            ErrorCode = string.Empty
        };
        report.Diagnostics.Add(diagnostic);
        RecordingValidator validator = new RecordingValidator(report);
        request.ValidateResult = true;
        request.Validator = validator;

        BuilderResult result = new MessageBuilder().BuildMessage(request);
        string xml = Encoding.UTF8.GetString(result.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode,
                Is.EqualTo("builder.target_validation_failed"));
            Assert.That(result.Validation, Is.SameAs(report));
            Assert.That(result.Diagnostics.Single(), Is.SameAs(diagnostic));
            Assert.That(xml, Does.Contain(">1.234500</XchgRate>"));
            Assert.That(validator.Request?.ExpectedIdentifier?.Value,
                Is.EqualTo("fxtr.008.001.08"));
            Assert.That(validator.Request?.ExpectedNamespaceUri,
                Is.EqualTo(Namespace("fxtr.008.001.08")));
            Assert.That(validator.Request?.Document, Is.SameAs(result.Document));
        });
    }

    [Test]
    public void ReturnsSuccessfulTargetValidationOutcome()
    {
        BuilderRequest request = CreateRequest(
            "payment-initiation",
            "pain.001.001.11");
        ValidationReport report = new ValidationReport
        {
            Success = true,
            Valid = true
        };
        RecordingValidator validator = new RecordingValidator(report);
        request.ValidateResult = true;
        request.Validator = validator;

        BuilderResult result = new MessageBuilder().BuildMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Validation, Is.SameAs(report));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(result.Xml, Is.Not.Empty);
            Assert.That(validator.Request?.ExpectedIdentifier?.Value,
                Is.EqualTo("pain.001.001.11"));
        });
    }

    private static string MessageIdentifierFor(string templateIdentifier)
    {
        switch (templateIdentifier)
        {
            case "payment-initiation":
                return "pain.001.001.11";
            case "payment-status":
                return "pacs.002.001.16";
            case "interbank-credit-transfer":
                return "pacs.008.001.10";
            case "cash-management":
                return "camt.053.001.13";
            case "securities":
                return "semt.001.001.04";
            case "cards":
                return "caaa.001.001.13";
            case "foreign-exchange":
                return "fxtr.008.001.08";
            case "trade":
                return "tsmt.001.001.03";
            default:
                throw new AssertionException("Unsupported test template.");
        }
    }

    private static object TemplateInput(BuilderRequest request)
    {
        return request.PaymentInitiation ??
            (object?)request.PaymentStatus ??
            (object?)request.InterbankTransfer ??
            (object?)request.CashManagement ??
            (object?)request.Securities ??
            (object?)request.Card ??
            (object?)request.ForeignExchange ??
            (object?)request.Trade ??
            throw new AssertionException("Expected semantic template input.");
    }

    private static void InvalidateBusinessValue(
        BuilderRequest request,
        string templateIdentifier,
        string invalidValue)
    {
        if (invalidValue.Contains("amount"))
        {
            IsoAmountValue amount = InvalidAmount(request, invalidValue);
            amount.Currency = "usd";
            return;
        }
        if (invalidValue == "quantity")
        {
            request.Securities!.Quantity.Scale = -1;
            return;
        }
        if (invalidValue == "rate")
        {
            request.ForeignExchange!.ExchangeRate.Scale = -1;
            return;
        }
        if (invalidValue == "datetime")
        {
            request.Card!.TransactionDateTime.Time.Hour = 25;
            return;
        }

        IsoDateValue date;
        switch (templateIdentifier)
        {
            case "payment-initiation":
                date = request.PaymentInitiation!.RequestedExecutionDate;
                break;
            case "interbank-credit-transfer":
                date = request.InterbankTransfer!.SettlementDate;
                break;
            case "cash-management":
                date = request.CashManagement!.BalanceDate;
                break;
            case "securities":
                date = request.Securities!.SettlementDate;
                break;
            case "foreign-exchange":
                date = invalidValue == "trade-date" ?
                    request.ForeignExchange!.TradeDate :
                    request.ForeignExchange!.SettlementDate;
                break;
            case "trade":
                date = request.Trade!.DueDate;
                break;
            default:
                throw new AssertionException("Unsupported invalid date case.");
        }
        date.Month = 13;
    }

    private static IsoAmountValue InvalidAmount(
        BuilderRequest request,
        string invalidValue)
    {
        if (request.PaymentInitiation != null)
        {
            return request.PaymentInitiation.Amount;
        }
        if (request.InterbankTransfer != null)
        {
            return request.InterbankTransfer.SettlementAmount;
        }
        if (request.CashManagement != null)
        {
            return request.CashManagement.BalanceAmount;
        }
        if (request.Securities != null)
        {
            return request.Securities.SettlementAmount;
        }
        if (request.Card != null)
        {
            return request.Card.Amount;
        }
        if (request.ForeignExchange != null)
        {
            return invalidValue == "bought-amount" ?
                request.ForeignExchange.BoughtAmount :
                request.ForeignExchange.SoldAmount;
        }
        if (request.Trade != null)
        {
            return request.Trade.Amount;
        }
        throw new AssertionException("Unsupported invalid amount case.");
    }

    [Test]
    public void BuilderBoundaryHelpersRejectAmbiguousIdentityAndInvalidValues()
    {
        MessageBuilder builder = new MessageBuilder();
        BuilderRequest cancelled = new BuilderRequest
        {
            CancellationToken = new CancellationToken(true)
        };
        BuilderRequest noLimits = new BuilderRequest { Limits = null! };
        BuilderRequest noWriteOptions = new BuilderRequest { WriteOptions = null! };
        BuilderRequest noInput = new BuilderRequest();
        BuilderRequest ambiguous = new BuilderRequest
        {
            PaymentInitiation = new PaymentInitiationTemplate(),
            PaymentStatus = new PaymentStatusTemplate()
        };
        BuilderRequest unsupported = new BuilderRequest { SemanticInput = new object() };
        BuilderRequest unknownTemplate = new BuilderRequest
        {
            PaymentInitiation = new PaymentInitiationTemplate(),
            TemplateIdentifier = "unknown"
        };
        BuilderRequest mismatchedTemplate = new BuilderRequest
        {
            PaymentInitiation = new PaymentInitiationTemplate(),
            TemplateIdentifier = "payment-status"
        };

        Assert.Multiple(() =>
        {
            Assert.That(builder.BuildMessage(cancelled).ErrorCode, Is.EqualTo("builder.cancelled"));
            Assert.That(builder.BuildMessage(noLimits).ErrorCode, Is.EqualTo("builder.limits_invalid"));
            Assert.That(builder.BuildMessage(noWriteOptions).ErrorCode, Is.EqualTo("builder.limits_invalid"));
            Assert.That(builder.BuildMessage(noInput).ErrorCode,
                Is.EqualTo("builder.semantic_input_missing"));
            Assert.That(builder.BuildMessage(ambiguous).ErrorCode,
                Is.EqualTo("builder.semantic_input_ambiguous"));
            Assert.That(builder.BuildMessage(unsupported).ErrorCode,
                Is.EqualTo("builder.semantic_input_unsupported"));
            Assert.That(builder.BuildMessage(unknownTemplate).ErrorCode,
                Is.EqualTo("builder.template_unknown"));
            Assert.That(builder.BuildMessage(mismatchedTemplate).ErrorCode,
                Is.EqualTo("builder.template_input_mismatch"));
            Assert.That(MessageBuilder.CurrencyIsValid(null!), Is.False);
            Assert.That(MessageBuilder.CurrencyIsValid("US"), Is.False);
            Assert.That(MessageBuilder.CurrencyIsValid("uSD"), Is.False);
            Assert.That(MessageBuilder.CurrencyIsValid("U1D"), Is.False);
            Assert.That(MessageBuilder.CurrencyIsValid("USD"), Is.True);
            Assert.That(MessageBuilder.ValidateOptionalAmount(null!, "optional").Success, Is.True);
            Assert.That(MessageBuilder.ValidateOptionalAmount(
                new IsoAmountValue(), "optional").Success, Is.True);
            Assert.That(MessageBuilder.GetExactIdentifierValue(new MessageIdentifier()), Is.Empty);
        });

        SemanticTemplateDefinition definition = SemanticTemplateRegistry.ResolveIdentifier(
            "payment-initiation") ?? throw new AssertionException("Missing template.");
        BuilderTarget missing = MessageBuilder.ResolveBuilderTarget(
            new BuilderRequest(), definition);
        BuilderTarget invalid = MessageBuilder.ResolveBuilderTarget(
            new BuilderRequest
            {
                TargetMessageIdentifier = new MessageIdentifier { Value = "bad" }
            }, definition);
        BuilderTarget conflict = MessageBuilder.ResolveBuilderTarget(
            new BuilderRequest
            {
                TargetMessageIdentifier = new MessageIdentifier
                {
                    BusinessArea = "pain", MessageFunctionality = "001",
                    Variant = "001", Version = "11", Value = "pacs.008.001.10"
                }
            }, definition);
        BuilderTarget namespaceMismatch = MessageBuilder.ResolveBuilderTarget(
            new BuilderRequest
            {
                TargetMessageIdentifier = Identifier("pain.001.001.11"),
                TargetNamespaceUri = "urn:wrong"
            }, definition);
        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCode, Is.EqualTo("builder.target_identifier_missing"));
            Assert.That(invalid.ErrorCode, Is.EqualTo("builder.target_identifier_invalid"));
            Assert.That(conflict.ErrorCode, Is.EqualTo("builder.target_identifier_conflict"));
            Assert.That(namespaceMismatch.ErrorCode,
                Is.EqualTo("builder.target_namespace_mismatch"));
        });

        DynamicXmlNode root = new DynamicXmlNode();
        MessageBuilder.PopulatePaymentInitiation(root, null, "urn:test");
        MessageBuilder.PopulatePaymentStatus(root, null, "urn:test");
        MessageBuilder.PopulateInterbankTransfer(root, null, "urn:test");
        MessageBuilder.PopulateCashManagement(root, null, "urn:test");
        MessageBuilder.PopulateSecurities(root, null, "urn:test");
        MessageBuilder.PopulateCard(root, null, "urn:test");
        MessageBuilder.PopulateForeignExchange(root, null, "urn:test");
        MessageBuilder.PopulateTrade(root, null, "urn:test");
        MessageBuilder.AddTextNode(root, "x", null!, "urn:test");
        MessageBuilder.AddAmountNode(root, "x", null!, "urn:test");
        MessageBuilder.AddAmountNode(root, "x", new IsoAmountValue { Currency = "USD",
            Amount = new IsoDecimalValue { Scale = -1 } }, "urn:test");
        Assert.That(root.Children, Is.Empty);

        BuilderRequest missingTarget = CreateRequest(
            "payment-initiation", "pain.001.001.11");
        missingTarget.TargetMessageIdentifier = null!;
        Assert.That(builder.BuildMessage(missingTarget).ErrorCode,
            Is.EqualTo("builder.target_identifier_missing"));

        BuilderRequest outputLimit = CreateRequest(
            "payment-initiation", "pain.001.001.11");
        outputLimit.Limits.MaxTotalBytes = 1;
        Assert.That(builder.BuildMessage(outputLimit).ErrorCode,
            Is.EqualTo("builder.output_limit"));

        BuilderRequest validationConfiguration = CreateRequest(
            "payment-initiation", "pain.001.001.11");
        validationConfiguration.ValidateResult = true;
        validationConfiguration.ValidationPolicy = null!;
        validationConfiguration.ValidationContext = null!;
        Assert.That(builder.BuildMessage(validationConfiguration).ErrorCode,
            Is.EqualTo("builder.validation_configuration_invalid"));

        BuilderTarget components = MessageBuilder.ResolveBuilderTarget(
            new BuilderRequest
            {
                TargetMessageIdentifier = new MessageIdentifier
                {
                    BusinessArea = "pain", MessageFunctionality = "001",
                    Variant = "001", Version = "11"
                }
            }, definition);
        Assert.That(components.Identifier?.Value, Is.EqualTo("pain.001.001.11"));
        Assert.That(MessageBuilder.ResolveBuilderTarget(
            new BuilderRequest { TargetMessageIdentifier = new MessageIdentifier() },
            definition).ErrorCode, Is.EqualTo("builder.target_identifier_invalid"));

        RepositorySnapshot repository = new RepositorySnapshot();
        MessageDefinitionMetadata repositoryDefinition = new MessageDefinitionMetadata
        {
            MessageIdentifier = Identifier("pain.001.001.11"),
            NamespaceUri = Namespace("pain.001.001.11"),
            XmlName = "AlternateDocument"
        };
        repository.MessageDefinitions.Add(repositoryDefinition);
        BuilderRequest repositoryRequest = CreateRequest(
            "payment-initiation", "pain.001.001.11");
        repositoryRequest.Repository = repository;
        BuilderTarget alternate = MessageBuilder.ResolveBuilderTarget(
            repositoryRequest, definition);
        Assert.That(alternate.MessageRootName, Is.EqualTo("AlternateDocument"));
        repository.MessageDefinitions.Add(repositoryDefinition);
        Assert.That(MessageBuilder.ResolveBuilderTarget(repositoryRequest, definition).ErrorCode,
            Is.EqualTo("builder.target_definition_ambiguous"));

        Assert.That(MessageBuilder.ValidateSemanticValues(
            new SemanticTemplateDefinition { Kind = 99 }, new object()).Success, Is.False);

        BuilderTarget documentTarget = new BuilderTarget
        {
            Identifier = Identifier("pain.001.001.11"),
            NamespaceUri = Namespace("pain.001.001.11"),
            MessageRootName = "Document"
        };
        BuilderRequest envelopeRequest = CreateRequest(
            "payment-initiation", "pain.001.001.11");
        envelopeRequest.Header = new MessageHeader
        {
            BusinessMessageIdentifier = "BIZ-1",
            BusinessService = "service"
        };
        envelopeRequest.Layout = EnvelopeKind.BusinessMessageEnvelope;
        MessageDocument envelope = MessageBuilder.CreateMessageDocument(
            envelopeRequest, documentTarget, new DynamicXmlNode());
        Assert.Multiple(() =>
        {
            Assert.That(envelope.Metadata.BusinessMessageIdentifier, Is.EqualTo("BIZ-1"));
            Assert.That(envelope.Envelope, Is.Not.Null);
        });
    }

    private static BuilderRequest CreateRequest(
        string templateIdentifier,
        string messageIdentifier)
    {
        BuilderRequest request = new BuilderRequest
        {
            TemplateIdentifier = templateIdentifier,
            TargetMessageIdentifier = Identifier(messageIdentifier),
            TargetNamespaceUri = Namespace(messageIdentifier),
            ValidateResult = false,
            WriteOptions = new MessageWriteOptions
            {
                WriteXmlDeclaration = false
            }
        };
        switch (templateIdentifier)
        {
            case "payment-initiation":
                request.PaymentInitiation = new PaymentInitiationTemplate
                {
                    MessageIdentifier = "MSG-001",
                    PaymentInformationIdentifier = "PAY-001",
                    EndToEndIdentifier = "E2E-001",
                    Debtor = "Debtor Ltd",
                    DebtorAccount = "DEBTOR-001",
                    Creditor = "Creditor Ltd",
                    CreditorAccount = "CREDITOR-001",
                    Amount = Amount("EUR", 125.2500m, 4),
                    RequestedExecutionDate = Date(2026, 9, 15),
                    RemittanceInformation = "Invoice 42"
                };
                break;
            case "payment-status":
                request.PaymentStatus = new PaymentStatusTemplate
                {
                    MessageIdentifier = "STATUS-001",
                    OriginalMessageIdentifier = "MSG-001",
                    OriginalEndToEndIdentifier = "E2E-001",
                    Status = "ACCP",
                    StatusReason = "Accepted"
                };
                break;
            case "interbank-credit-transfer":
                request.InterbankTransfer = new InterbankTransferTemplate
                {
                    MessageIdentifier = "MSG-001",
                    InstructionIdentifier = "INSTR-001",
                    EndToEndIdentifier = "E2E-001",
                    TransactionIdentifier = "TX-001",
                    Uetr = "123e4567-e89b-42d3-a456-426614174000",
                    Debtor = "Debtor Bank Customer",
                    Creditor = "Creditor Bank Customer",
                    SettlementAmount = Amount("USD", 99.9900m, 4),
                    SettlementDate = Date(2026, 9, 16)
                };
                break;
            case "cash-management":
                request.CashManagement = new CashManagementTemplate
                {
                    MessageIdentifier = "CASH-001",
                    ReportIdentifier = "REPORT-001",
                    AccountIdentifier = "ACCOUNT-001",
                    Owner = "Account Owner",
                    BalanceAmount = Amount("GBP", 1000.00m, 2),
                    BalanceDate = Date(2026, 9, 17)
                };
                break;
            case "securities":
                request.Securities = new SecuritiesTemplate
                {
                    MessageIdentifier = "SEC-001",
                    TransactionIdentifier = "SEC-TX-001",
                    SecurityIdentifier = "US0378331005",
                    Quantity = Decimal(25.000m, 3),
                    SettlementAmount = Amount("USD", 5000.00m, 2),
                    SettlementDate = Date(2026, 9, 18)
                };
                break;
            case "cards":
                request.Card = new CardTemplate
                {
                    MessageIdentifier = "CARD-001",
                    TransactionIdentifier = "CARD-TX-001",
                    AcquirerIdentifier = "ACQUIRER-001",
                    MerchantIdentifier = "MERCHANT-001",
                    MaskedCardIdentifier = "411111******1111",
                    Amount = Amount("USD", 12.340m, 3),
                    TransactionDateTime = DateTimeValue(2026, 9, 19, 14, 30, 15)
                };
                break;
            case "foreign-exchange":
                request.ForeignExchange = new ForeignExchangeTemplate
                {
                    MessageIdentifier = "FX-001",
                    TradeIdentifier = "FX-TRADE-001",
                    Counterparty = "Counterparty Bank",
                    BoughtAmount = Amount("EUR", 100.00m, 2),
                    SoldAmount = Amount("USD", 123.45m, 2),
                    ExchangeRate = Decimal(1.234500m, 6),
                    TradeDate = Date(2026, 9, 20),
                    SettlementDate = Date(2026, 9, 22)
                };
                break;
            case "trade":
                request.Trade = new TradeTemplate
                {
                    MessageIdentifier = "TRADE-001",
                    TradeIdentifier = "TRADE-TX-001",
                    BaselineIdentifier = "BASELINE-001",
                    Buyer = "Buyer Ltd",
                    Seller = "Seller Ltd",
                    Amount = Amount("EUR", 750.00m, 2),
                    DueDate = Date(2026, 10, 1),
                    GoodsDescription = "Industrial parts"
                };
                break;
        }
        return request;
    }

    private static IsoAmountValue Amount(string currency, decimal value, int scale)
    {
        return new IsoAmountValue
        {
            Currency = currency,
            Amount = Decimal(value, scale)
        };
    }

    private static IsoDecimalValue Decimal(decimal value, int scale)
    {
        return new IsoDecimalValue
        {
            Value = value,
            Scale = scale
        };
    }

    private static IsoDateValue Date(int year, int month, int day)
    {
        return new IsoDateValue
        {
            Year = year,
            Month = month,
            Day = day
        };
    }

    private static IsoDateTimeValue DateTimeValue(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        return new IsoDateTimeValue
        {
            Date = Date(year, month, day),
            Time = new IsoTimeValue
            {
                Hour = hour,
                Minute = minute,
                Second = second
            }
        };
    }

    private static MessageIdentifier Identifier(string value)
    {
        string[] parts = value.Split('.');
        return new MessageIdentifier
        {
            BusinessArea = parts[0],
            MessageFunctionality = parts[1],
            Variant = parts[2],
            Version = parts[3],
            Value = value
        };
    }

    private static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    private sealed class RecordingValidator : IMessageValidator
    {
        private readonly ValidationReport report;

        public RecordingValidator(ValidationReport report)
        {
            this.report = report;
        }

        public ValidationRequest? Request;

        public ValidationReport ValidateMessage(ValidationRequest? request)
        {
            Request = request;
            return report;
        }
    }
}
