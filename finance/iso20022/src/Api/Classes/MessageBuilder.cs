using System;
using System.Text;

namespace iso20022;

/// <summary>Builds exact ISO 20022 messages from stable, version-neutral semantic templates.</summary>
public class MessageBuilder : IMessageBuilder
{
    /// <summary>Initializes a semantic message builder.</summary>
    public MessageBuilder()
    {
    }

    /// <summary>Builds and optionally validates one exact target message.</summary>
    /// <param name="request">The template input, exact target identity, and validation policy.</param>
    /// <returns>The constructed message and complete target validation outcome.</returns>
    public BuilderResult BuildMessage(BuilderRequest? request)
    {
        if (request == null)
        {
            return CreateFailureResult("builder.request_missing", "A builder request is required.", string.Empty);
        }
        if (request.CancellationToken.IsCancellationRequested)
        {
            return CreateFailureResult("builder.cancelled", "Message construction was cancelled.", request.TemplateIdentifier);
        }
        if (request.Limits == null || !MessageValidator.LimitsArePositive(request.Limits) ||
            request.WriteOptions == null || request.WriteOptions.Limits == null ||
            !MessageValidator.LimitsArePositive(request.WriteOptions.Limits))
        {
            return CreateFailureResult("builder.limits_invalid", "Construction and writing resource limits must be positive.", request.TemplateIdentifier);
        }

        SemanticTemplateSelection selection = SelectSemanticInput(request);
        if (selection.Definition == null || selection.Input == null)
        {
            return CreateFailureResult(selection.ErrorCode, selection.ErrorMessage, request.TemplateIdentifier);
        }

        BuilderTarget target = ResolveBuilderTarget(request, selection.Definition);
        if (target.Identifier == null)
        {
            return CreateFailureResult(target.ErrorCode, target.ErrorMessage, selection.Definition.Identifier);
        }
        if (!SemanticTemplateRegistry.SupportsTarget(selection.Definition, target.Identifier))
        {
            return CreateFailureResult(
                "builder.target_family_mismatch",
                "The selected semantic template does not support the exact target message family.",
                selection.Definition.Identifier);
        }

        BuilderInputValidation inputValidation = ValidateSemanticValues(selection.Definition, selection.Input);
        if (!inputValidation.Success)
        {
            return CreateFailureResult(inputValidation.ErrorCode, inputValidation.ErrorMessage, selection.Definition.Identifier);
        }
        if (request.CancellationToken.IsCancellationRequested)
        {
            return CreateFailureResult("builder.cancelled", "Message construction was cancelled.", selection.Definition.Identifier);
        }

        DynamicXmlNode payloadTree = BuildPayloadTree(selection.Definition, selection.Input, target);
        MessageDocument document = CreateMessageDocument(request, target, payloadTree);
        ApplySemanticMetadata(document.Metadata, selection.Definition, selection.Input);
        BuilderResult result = CreateBaseSuccessResult(selection.Definition, target, document);

        MessageWriteResult write = new XmlMessageWriter().WriteGenericString(document, request.WriteOptions);
        if (!write.Success)
        {
            result.Success = false;
            result.ErrorCode = write.ErrorCode;
            result.ErrorMessage = write.ErrorMessage;
            return result;
        }
        result.Xml = Encoding.UTF8.GetBytes(write.Text);
        if (result.Xml.LongLength > request.Limits.MaxTotalBytes)
        {
            result.Success = false;
            result.Xml = Array.Empty<byte>();
            result.ErrorCode = "builder.output_limit";
            result.ErrorMessage = "The constructed XML exceeds the configured total-byte limit.";
            AddConstructionDiagnostic(result, result.ErrorCode, result.ErrorMessage);
            return result;
        }

        if (request.ValidateResult)
        {
            if (request.ValidationPolicy == null || request.ValidationContext == null)
            {
                result.Success = false;
                result.ErrorCode = "builder.validation_configuration_invalid";
                result.ErrorMessage = "Validation policy and context are required when target validation is selected.";
                AddConstructionDiagnostic(result, result.ErrorCode, result.ErrorMessage);
                return result;
            }

            IMessageValidator validator = request.Validator ?? new MessageValidator();
            ValidationRequest validationRequest = new ValidationRequest();
            validationRequest.Document = document;
            validationRequest.ExpectedIdentifier = target.Identifier;
            validationRequest.ExpectedNamespaceUri = target.NamespaceUri;
            validationRequest.Policy = request.ValidationPolicy;
            validationRequest.Context = request.ValidationContext;
            result.Validation = validator.ValidateMessage(validationRequest);
            AddValidationDiagnostics(result, result.Validation);
            if (!result.Validation.Success || !result.Validation.Valid)
            {
                result.Success = false;
                result.ErrorCode = "builder.target_validation_failed";
                result.ErrorMessage = "The constructed target did not pass the requested validation.";
            }
        }

        return result;
    }

    internal static SemanticTemplateSelection SelectSemanticInput(BuilderRequest request)
    {
        SemanticTemplateSelection selection = new SemanticTemplateSelection();
        int count = 0;
        object? input = null;
        if (request.SemanticInput != null)
        {
            input = request.SemanticInput;
            count++;
        }
        if (request.PaymentInitiation != null)
        {
            input = request.PaymentInitiation;
            count++;
        }
        if (request.PaymentStatus != null)
        {
            input = request.PaymentStatus;
            count++;
        }
        if (request.InterbankTransfer != null)
        {
            input = request.InterbankTransfer;
            count++;
        }
        if (request.CashManagement != null)
        {
            input = request.CashManagement;
            count++;
        }
        if (request.Securities != null)
        {
            input = request.Securities;
            count++;
        }
        if (request.Card != null)
        {
            input = request.Card;
            count++;
        }
        if (request.ForeignExchange != null)
        {
            input = request.ForeignExchange;
            count++;
        }
        if (request.Trade != null)
        {
            input = request.Trade;
            count++;
        }

        if (count == 0)
        {
            selection.ErrorCode = "builder.semantic_input_missing";
            selection.ErrorMessage = "Exactly one generated semantic template input is required.";
            return selection;
        }
        if (count != 1 || input == null)
        {
            selection.ErrorCode = "builder.semantic_input_ambiguous";
            selection.ErrorMessage = "Only one semantic template input can be supplied.";
            return selection;
        }

        SemanticTemplateDefinition? inputDefinition = SemanticTemplateRegistry.ResolveInput(input);
        if (inputDefinition == null)
        {
            selection.ErrorCode = "builder.semantic_input_unsupported";
            selection.ErrorMessage = "The semantic input is not a generated builder template.";
            return selection;
        }

        SemanticTemplateDefinition? namedDefinition = inputDefinition;
        if (request.TemplateIdentifier.Length > 0)
        {
            namedDefinition = SemanticTemplateRegistry.ResolveIdentifier(request.TemplateIdentifier);
            if (namedDefinition == null)
            {
                selection.ErrorCode = "builder.template_unknown";
                selection.ErrorMessage = "The semantic template identifier is not supported.";
                return selection;
            }
            if (namedDefinition.Kind != inputDefinition.Kind)
            {
                selection.ErrorCode = "builder.template_input_mismatch";
                selection.ErrorMessage = "The semantic input type does not match the selected template identifier.";
                return selection;
            }
        }

        selection.Definition = namedDefinition;
        selection.Input = input;
        return selection;
    }

    internal static BuilderTarget ResolveBuilderTarget(BuilderRequest request, SemanticTemplateDefinition definition)
    {
        BuilderTarget target = new BuilderTarget();
        if (request.TargetMessageIdentifier == null)
        {
            target.ErrorCode = "builder.target_identifier_missing";
            target.ErrorMessage = "An exact target message identifier is required.";
            return target;
        }

        MessageIdentifier supplied = request.TargetMessageIdentifier;
        MessageIdentifier? fromValue = null;
        if (supplied.Value.Length > 0)
        {
            fromValue = MessageIdentifierLexical.ParseMessageIdentifier(supplied.Value.AsSpan());
            if (fromValue == null)
            {
                target.ErrorCode = "builder.target_identifier_invalid";
                target.ErrorMessage = "The exact target message identifier is invalid.";
                return target;
            }
        }

        string? fromComponents = MessageIdentifierLexical.FormatMessageIdentifier(supplied);
        if (fromComponents != null && fromValue != null && fromComponents != fromValue.Value)
        {
            target.ErrorCode = "builder.target_identifier_conflict";
            target.ErrorMessage = "The target identifier value conflicts with its individual components.";
            return target;
        }
        if (fromValue == null && fromComponents != null)
        {
            fromValue = MessageIdentifierLexical.ParseMessageIdentifier(fromComponents.AsSpan());
        }
        if (fromValue == null)
        {
            target.ErrorCode = "builder.target_identifier_invalid";
            target.ErrorMessage = "The exact target message identifier is invalid.";
            return target;
        }

        target.Identifier = fromValue;
        target.IdentifierText = fromValue.Value;
        target.NamespaceUri = Iso20022Namespaces.SchemaUrnPrefix + fromValue.Value;
        MessageDefinitionMetadata? repositoryDefinition = FindRepositoryDefinition(request.Repository, fromValue, request.TargetNamespaceUri, target);
        if (target.ErrorCode.Length > 0)
        {
            target.Identifier = null;
            return target;
        }
        if (request.TargetNamespaceUri.Length > 0)
        {
            if (request.TargetNamespaceUri != target.NamespaceUri && repositoryDefinition == null)
            {
                target.Identifier = null;
                target.ErrorCode = "builder.target_namespace_mismatch";
                target.ErrorMessage = "The exact target namespace does not match the target message identifier.";
                return target;
            }
            target.NamespaceUri = request.TargetNamespaceUri;
        }

        target.MessageRootName = SemanticTemplateRegistry.GetDefaultRootName(definition, fromValue);
        if (repositoryDefinition != null)
        {
            if (repositoryDefinition.XmlTag.Length > 0 && repositoryDefinition.XmlTag != "Document")
            {
                target.MessageRootName = repositoryDefinition.XmlTag;
            }
            else if (repositoryDefinition.XmlName.Length > 0 && repositoryDefinition.XmlName != "Document")
            {
                target.MessageRootName = repositoryDefinition.XmlName;
            }
        }
        return target;
    }

    internal static MessageDefinitionMetadata? FindRepositoryDefinition(RepositorySnapshot? repository, MessageIdentifier identifier, string requestedNamespace, BuilderTarget target)
    {
        if (repository == null)
        {
            return null;
        }

        MessageDefinitionMetadata? found = null;
        int index = 0;
        while (index < repository.MessageDefinitions.Count)
        {
            MessageDefinitionMetadata candidate = repository.MessageDefinitions[index];
            string candidateIdentifier = GetExactIdentifierValue(candidate.MessageIdentifier);
            if (candidateIdentifier == identifier.Value &&
                (requestedNamespace.Length == 0 || candidate.NamespaceUri == requestedNamespace))
            {
                if (found != null)
                {
                    target.ErrorCode = "builder.target_definition_ambiguous";
                    target.ErrorMessage = "The repository contains more than one matching exact target definition.";
                    return null;
                }
                found = candidate;
            }
            index++;
        }
        return found;
    }

    internal static string GetExactIdentifierValue(MessageIdentifier identifier)
    {
        string? formatted = MessageIdentifierLexical.FormatMessageIdentifier(identifier);
        if (formatted != null)
        {
            return formatted;
        }
        MessageIdentifier? parsed = MessageIdentifierLexical.ParseMessageIdentifier(identifier.Value.AsSpan());
        if (parsed != null)
        {
            return parsed.Value;
        }
        return string.Empty;
    }

    internal static BuilderInputValidation ValidateSemanticValues(SemanticTemplateDefinition definition, object input)
    {
        switch (definition.Kind)
        {
            case SemanticTemplateRegistry.PaymentInitiation:
                return ValidatePaymentInitiation(input as PaymentInitiationTemplate);
            case SemanticTemplateRegistry.PaymentStatus:
                return ValidatePaymentStatus(input as PaymentStatusTemplate);
            case SemanticTemplateRegistry.InterbankTransfer:
                return ValidateInterbankTransfer(input as InterbankTransferTemplate);
            case SemanticTemplateRegistry.CashManagement:
                return ValidateCashManagement(input as CashManagementTemplate);
            case SemanticTemplateRegistry.Securities:
                return ValidateSecurities(input as SecuritiesTemplate);
            case SemanticTemplateRegistry.Card:
                return ValidateCard(input as CardTemplate);
            case SemanticTemplateRegistry.ForeignExchange:
                return ValidateForeignExchange(input as ForeignExchangeTemplate);
            case SemanticTemplateRegistry.Trade:
                return ValidateTrade(input as TradeTemplate);
            default:
                return InvalidInput("builder.template_unknown", "The semantic template identifier is not supported.");
        }
    }

    internal static BuilderInputValidation ValidatePaymentInitiation(PaymentInitiationTemplate? input)
    {
        if (input == null || input.MessageIdentifier.Length == 0 || input.PaymentInformationIdentifier.Length == 0 ||
            input.EndToEndIdentifier.Length == 0 || input.Debtor.Length == 0 || input.Creditor.Length == 0)
        {
            return InvalidInput("builder.required_value_missing", "Payment initiation requires message, payment, end-to-end, debtor, and creditor identifiers.");
        }
        return ValidateAmountAndDate(input.Amount, input.RequestedExecutionDate, "payment initiation");
    }

    internal static BuilderInputValidation ValidatePaymentStatus(PaymentStatusTemplate? input)
    {
        if (input == null || input.MessageIdentifier.Length == 0 || input.OriginalMessageIdentifier.Length == 0 || input.Status.Length == 0)
        {
            return InvalidInput("builder.required_value_missing", "Payment status requires message, original-message, and status values.");
        }
        return ValidInput();
    }

    internal static BuilderInputValidation ValidateInterbankTransfer(InterbankTransferTemplate? input)
    {
        if (input == null || input.MessageIdentifier.Length == 0 || input.EndToEndIdentifier.Length == 0 ||
            input.TransactionIdentifier.Length == 0 || input.Debtor.Length == 0 || input.Creditor.Length == 0)
        {
            return InvalidInput("builder.required_value_missing", "Interbank transfer requires message, end-to-end, transaction, debtor, and creditor identifiers.");
        }
        return ValidateAmountAndDate(input.SettlementAmount, input.SettlementDate, "interbank transfer");
    }

    internal static BuilderInputValidation ValidateCashManagement(CashManagementTemplate? input)
    {
        if (input == null || input.MessageIdentifier.Length == 0 || input.ReportIdentifier.Length == 0 || input.AccountIdentifier.Length == 0)
        {
            return InvalidInput("builder.required_value_missing", "Cash management requires message, report, and account identifiers.");
        }
        return ValidateAmountAndDate(input.BalanceAmount, input.BalanceDate, "cash management");
    }

    internal static BuilderInputValidation ValidateSecurities(SecuritiesTemplate? input)
    {
        if (input == null || input.MessageIdentifier.Length == 0 || input.TransactionIdentifier.Length == 0 || input.SecurityIdentifier.Length == 0)
        {
            return InvalidInput("builder.required_value_missing", "Securities construction requires message, transaction, and security identifiers.");
        }
        if (DecimalLexical.FormatDecimal(input.Quantity) == null || DateTimeLexical.FormatDate(input.SettlementDate) == null)
        {
            return InvalidInput("builder.value_invalid", "Securities quantity and settlement date must have valid ISO lexical values.");
        }
        return ValidateOptionalAmount(input.SettlementAmount, "securities settlement");
    }

    internal static BuilderInputValidation ValidateCard(CardTemplate? input)
    {
        if (input == null || input.MessageIdentifier.Length == 0 || input.TransactionIdentifier.Length == 0 || input.MerchantIdentifier.Length == 0)
        {
            return InvalidInput("builder.required_value_missing", "Card construction requires message, transaction, and merchant identifiers.");
        }
        BuilderInputValidation amount = ValidateRequiredAmount(input.Amount, "card transaction");
        if (!amount.Success)
        {
            return amount;
        }
        if (DateTimeLexical.FormatDateTime(input.TransactionDateTime) == null)
        {
            return InvalidInput("builder.value_invalid", "Card transaction date-time must have a valid ISO lexical value.");
        }
        return ValidInput();
    }

    internal static BuilderInputValidation ValidateForeignExchange(ForeignExchangeTemplate? input)
    {
        if (input == null || input.MessageIdentifier.Length == 0 || input.TradeIdentifier.Length == 0)
        {
            return InvalidInput("builder.required_value_missing", "Foreign exchange construction requires message and trade identifiers.");
        }
        BuilderInputValidation bought = ValidateRequiredAmount(input.BoughtAmount, "foreign-exchange bought");
        if (!bought.Success)
        {
            return bought;
        }
        BuilderInputValidation sold = ValidateRequiredAmount(input.SoldAmount, "foreign-exchange sold");
        if (!sold.Success)
        {
            return sold;
        }
        if (DecimalLexical.FormatDecimal(input.ExchangeRate) == null ||
            DateTimeLexical.FormatDate(input.TradeDate) == null || DateTimeLexical.FormatDate(input.SettlementDate) == null)
        {
            return InvalidInput("builder.value_invalid", "Foreign-exchange rate, trade date, and settlement date must have valid ISO lexical values.");
        }
        return ValidInput();
    }

    internal static BuilderInputValidation ValidateTrade(TradeTemplate? input)
    {
        if (input == null || input.MessageIdentifier.Length == 0 || input.TradeIdentifier.Length == 0 ||
            input.BaselineIdentifier.Length == 0 || input.Buyer.Length == 0 || input.Seller.Length == 0)
        {
            return InvalidInput("builder.required_value_missing", "Trade construction requires message, trade, baseline, buyer, and seller identifiers.");
        }
        return ValidateAmountAndDate(input.Amount, input.DueDate, "trade");
    }

    internal static BuilderInputValidation ValidateAmountAndDate(IsoAmountValue amount, IsoDateValue date, string label)
    {
        BuilderInputValidation amountValidation = ValidateRequiredAmount(amount, label);
        if (!amountValidation.Success)
        {
            return amountValidation;
        }
        if (DateTimeLexical.FormatDate(date) == null)
        {
            return InvalidInput("builder.value_invalid", "The " + label + " date must have a valid ISO lexical value.");
        }
        return ValidInput();
    }

    internal static BuilderInputValidation ValidateRequiredAmount(IsoAmountValue amount, string label)
    {
        if (amount == null || !CurrencyIsValid(amount.Currency) || DecimalLexical.FormatDecimal(amount.Amount) == null)
        {
            return InvalidInput("builder.value_invalid", "The " + label + " amount and three-letter currency must be valid.");
        }
        return ValidInput();
    }

    internal static BuilderInputValidation ValidateOptionalAmount(IsoAmountValue amount, string label)
    {
        if (amount == null)
        {
            return ValidInput();
        }
        if (amount.Currency.Length == 0 && amount.Amount.Value == decimal.Zero && amount.Amount.Scale == 0 && amount.Amount.LexicalValue.Length == 0)
        {
            return ValidInput();
        }
        return ValidateRequiredAmount(amount, label);
    }

    internal static bool CurrencyIsValid(string currency)
    {
        int index = 0;
        if (currency == null || currency.Length != 3)
        {
            return false;
        }
        while (index < currency.Length)
        {
            if (currency[index] < 'A' || currency[index] > 'Z')
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static BuilderInputValidation ValidInput()
    {
        BuilderInputValidation validation = new BuilderInputValidation();
        validation.Success = true;
        return validation;
    }

    internal static BuilderInputValidation InvalidInput(string code, string message)
    {
        BuilderInputValidation validation = new BuilderInputValidation();
        validation.ErrorCode = code;
        validation.ErrorMessage = message;
        return validation;
    }

    internal static DynamicXmlNode BuildPayloadTree(SemanticTemplateDefinition definition, object input, BuilderTarget target)
    {
        DynamicXmlNode document = CreateElementNode("Document", target.NamespaceUri);
        NamespaceDeclarationData declaration = new NamespaceDeclarationData();
        declaration.NamespaceUri = target.NamespaceUri;
        document.NamespaceDeclarations.Add(declaration);
        DynamicXmlNode message = CreateElementNode(target.MessageRootName, target.NamespaceUri);
        document.Children.Add(message);
        switch (definition.Kind)
        {
            case SemanticTemplateRegistry.PaymentInitiation:
                PopulatePaymentInitiation(message, input as PaymentInitiationTemplate, target.NamespaceUri);
                break;
            case SemanticTemplateRegistry.PaymentStatus:
                PopulatePaymentStatus(message, input as PaymentStatusTemplate, target.NamespaceUri);
                break;
            case SemanticTemplateRegistry.InterbankTransfer:
                PopulateInterbankTransfer(message, input as InterbankTransferTemplate, target.NamespaceUri);
                break;
            case SemanticTemplateRegistry.CashManagement:
                PopulateCashManagement(message, input as CashManagementTemplate, target.NamespaceUri);
                break;
            case SemanticTemplateRegistry.Securities:
                PopulateSecurities(message, input as SecuritiesTemplate, target.NamespaceUri);
                break;
            case SemanticTemplateRegistry.Card:
                PopulateCard(message, input as CardTemplate, target.NamespaceUri);
                break;
            case SemanticTemplateRegistry.ForeignExchange:
                PopulateForeignExchange(message, input as ForeignExchangeTemplate, target.NamespaceUri);
                break;
            case SemanticTemplateRegistry.Trade:
                PopulateTrade(message, input as TradeTemplate, target.NamespaceUri);
                break;
        }
        return document;
    }

    internal static void PopulatePaymentInitiation(DynamicXmlNode root, PaymentInitiationTemplate? input, string namespaceUri)
    {
        if (input == null)
        {
            return;
        }
        DynamicXmlNode header = AddElementNode(root, "GrpHdr", namespaceUri);
        AddTextNode(header, "MsgId", input.MessageIdentifier, namespaceUri);
        DynamicXmlNode payment = AddElementNode(root, "PmtInf", namespaceUri);
        AddTextNode(payment, "PmtInfId", input.PaymentInformationIdentifier, namespaceUri);
        AddDateNode(payment, "ReqdExctnDt", input.RequestedExecutionDate, namespaceUri);
        AddPartyNode(payment, "Dbtr", input.Debtor, namespaceUri);
        AddAccountNode(payment, "DbtrAcct", input.DebtorAccount, namespaceUri);
        DynamicXmlNode transaction = AddElementNode(payment, "CdtTrfTxInf", namespaceUri);
        DynamicXmlNode paymentId = AddElementNode(transaction, "PmtId", namespaceUri);
        AddTextNode(paymentId, "EndToEndId", input.EndToEndIdentifier, namespaceUri);
        DynamicXmlNode amount = AddElementNode(transaction, "Amt", namespaceUri);
        AddAmountNode(amount, "InstdAmt", input.Amount, namespaceUri);
        AddPartyNode(transaction, "Cdtr", input.Creditor, namespaceUri);
        AddAccountNode(transaction, "CdtrAcct", input.CreditorAccount, namespaceUri);
        AddRemittanceNode(transaction, input.RemittanceInformation, namespaceUri);
    }

    internal static void PopulatePaymentStatus(DynamicXmlNode root, PaymentStatusTemplate? input, string namespaceUri)
    {
        if (input == null)
        {
            return;
        }
        DynamicXmlNode header = AddElementNode(root, "GrpHdr", namespaceUri);
        AddTextNode(header, "MsgId", input.MessageIdentifier, namespaceUri);
        DynamicXmlNode original = AddElementNode(root, "OrgnlGrpInfAndSts", namespaceUri);
        AddTextNode(original, "OrgnlMsgId", input.OriginalMessageIdentifier, namespaceUri);
        DynamicXmlNode transaction = AddElementNode(root, "TxInfAndSts", namespaceUri);
        AddTextNode(transaction, "OrgnlEndToEndId", input.OriginalEndToEndIdentifier, namespaceUri);
        AddTextNode(transaction, "TxSts", input.Status, namespaceUri);
        if (input.StatusReason.Length > 0)
        {
            DynamicXmlNode reason = AddElementNode(transaction, "StsRsnInf", namespaceUri);
            AddTextNode(reason, "AddtlInf", input.StatusReason, namespaceUri);
        }
    }

    internal static void PopulateInterbankTransfer(DynamicXmlNode root, InterbankTransferTemplate? input, string namespaceUri)
    {
        if (input == null)
        {
            return;
        }
        DynamicXmlNode header = AddElementNode(root, "GrpHdr", namespaceUri);
        AddTextNode(header, "MsgId", input.MessageIdentifier, namespaceUri);
        AddDateNode(header, "IntrBkSttlmDt", input.SettlementDate, namespaceUri);
        DynamicXmlNode transaction = AddElementNode(root, "CdtTrfTxInf", namespaceUri);
        DynamicXmlNode paymentId = AddElementNode(transaction, "PmtId", namespaceUri);
        AddTextNode(paymentId, "InstrId", input.InstructionIdentifier, namespaceUri);
        AddTextNode(paymentId, "EndToEndId", input.EndToEndIdentifier, namespaceUri);
        AddTextNode(paymentId, "TxId", input.TransactionIdentifier, namespaceUri);
        AddTextNode(paymentId, "UETR", input.Uetr, namespaceUri);
        AddAmountNode(transaction, "IntrBkSttlmAmt", input.SettlementAmount, namespaceUri);
        AddPartyNode(transaction, "Dbtr", input.Debtor, namespaceUri);
        AddAccountNode(transaction, "DbtrAcct", input.DebtorAccount, namespaceUri);
        AddPartyNode(transaction, "Cdtr", input.Creditor, namespaceUri);
        AddAccountNode(transaction, "CdtrAcct", input.CreditorAccount, namespaceUri);
        AddRemittanceNode(transaction, input.RemittanceInformation, namespaceUri);
    }

    internal static void PopulateCashManagement(DynamicXmlNode root, CashManagementTemplate? input, string namespaceUri)
    {
        if (input == null)
        {
            return;
        }
        DynamicXmlNode header = AddElementNode(root, "GrpHdr", namespaceUri);
        AddTextNode(header, "MsgId", input.MessageIdentifier, namespaceUri);
        DynamicXmlNode report = AddElementNode(root, "Rpt", namespaceUri);
        AddTextNode(report, "Id", input.ReportIdentifier, namespaceUri);
        DynamicXmlNode account = AddElementNode(report, "Acct", namespaceUri);
        AddIdentifierChoiceNode(account, input.AccountIdentifier, namespaceUri);
        AddPartyNode(account, "Ownr", input.Owner, namespaceUri);
        DynamicXmlNode balance = AddElementNode(report, "Bal", namespaceUri);
        AddAmountNode(balance, "Amt", input.BalanceAmount, namespaceUri);
        DynamicXmlNode date = AddElementNode(balance, "Dt", namespaceUri);
        AddDateNode(date, "Dt", input.BalanceDate, namespaceUri);
    }

    internal static void PopulateSecurities(DynamicXmlNode root, SecuritiesTemplate? input, string namespaceUri)
    {
        if (input == null)
        {
            return;
        }
        AddTextNode(root, "MsgId", input.MessageIdentifier, namespaceUri);
        DynamicXmlNode transaction = AddElementNode(root, "Tx", namespaceUri);
        AddTextNode(transaction, "TxId", input.TransactionIdentifier, namespaceUri);
        DynamicXmlNode instrument = AddElementNode(transaction, "FinInstrmId", namespaceUri);
        AddTextNode(instrument, "ISIN", input.SecurityIdentifier, namespaceUri);
        AddTextNode(transaction, "SfkpgAcct", input.AccountIdentifier, namespaceUri);
        AddDecimalNode(transaction, "Qty", input.Quantity, namespaceUri);
        AddAmountNode(transaction, "SttlmAmt", input.SettlementAmount, namespaceUri);
        AddDateNode(transaction, "SttlmDt", input.SettlementDate, namespaceUri);
        AddPartyNode(transaction, "DlvrgPty", input.DeliveringParty, namespaceUri);
        AddPartyNode(transaction, "RcvgPty", input.ReceivingParty, namespaceUri);
    }

    internal static void PopulateCard(DynamicXmlNode root, CardTemplate? input, string namespaceUri)
    {
        if (input == null)
        {
            return;
        }
        AddTextNode(root, "MsgId", input.MessageIdentifier, namespaceUri);
        DynamicXmlNode transaction = AddElementNode(root, "Tx", namespaceUri);
        AddTextNode(transaction, "TxId", input.TransactionIdentifier, namespaceUri);
        AddTextNode(transaction, "AcqrrId", input.AcquirerIdentifier, namespaceUri);
        AddTextNode(transaction, "MrchntId", input.MerchantIdentifier, namespaceUri);
        AddTextNode(transaction, "MskdPAN", input.MaskedCardIdentifier, namespaceUri);
        AddAmountNode(transaction, "TxAmt", input.Amount, namespaceUri);
        AddDateTimeNode(transaction, "TxDtTm", input.TransactionDateTime, namespaceUri);
    }

    internal static void PopulateForeignExchange(DynamicXmlNode root, ForeignExchangeTemplate? input, string namespaceUri)
    {
        if (input == null)
        {
            return;
        }
        AddTextNode(root, "MsgId", input.MessageIdentifier, namespaceUri);
        DynamicXmlNode trade = AddElementNode(root, "Trad", namespaceUri);
        AddTextNode(trade, "TradId", input.TradeIdentifier, namespaceUri);
        AddTextNode(trade, "CtrPty", input.Counterparty, namespaceUri);
        AddAmountNode(trade, "BuyAmt", input.BoughtAmount, namespaceUri);
        AddAmountNode(trade, "SellAmt", input.SoldAmount, namespaceUri);
        AddDecimalNode(trade, "XchgRate", input.ExchangeRate, namespaceUri);
        AddDateNode(trade, "TradDt", input.TradeDate, namespaceUri);
        AddDateNode(trade, "SttlmDt", input.SettlementDate, namespaceUri);
    }

    internal static void PopulateTrade(DynamicXmlNode root, TradeTemplate? input, string namespaceUri)
    {
        if (input == null)
        {
            return;
        }
        AddTextNode(root, "MsgId", input.MessageIdentifier, namespaceUri);
        DynamicXmlNode trade = AddElementNode(root, "Trad", namespaceUri);
        AddTextNode(trade, "TradId", input.TradeIdentifier, namespaceUri);
        AddTextNode(trade, "BaselnId", input.BaselineIdentifier, namespaceUri);
        AddPartyNode(trade, "Buyr", input.Buyer, namespaceUri);
        AddPartyNode(trade, "Sellr", input.Seller, namespaceUri);
        AddAmountNode(trade, "Amt", input.Amount, namespaceUri);
        AddDateNode(trade, "DueDt", input.DueDate, namespaceUri);
        AddTextNode(trade, "GoodsDesc", input.GoodsDescription, namespaceUri);
    }

    internal static DynamicXmlNode CreateElementNode(string localName, string namespaceUri)
    {
        DynamicXmlNode node = new DynamicXmlNode();
        node.Kind = XmlNodeKind.Element;
        node.Name.LocalName = localName;
        node.Name.NamespaceUri = namespaceUri;
        return node;
    }

    internal static DynamicXmlNode AddElementNode(DynamicXmlNode parent, string localName, string namespaceUri)
    {
        DynamicXmlNode child = CreateElementNode(localName, namespaceUri);
        parent.Children.Add(child);
        return child;
    }

    internal static void AddTextNode(DynamicXmlNode parent, string localName, string value, string namespaceUri)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }
        DynamicXmlNode element = AddElementNode(parent, localName, namespaceUri);
        DynamicXmlNode text = new DynamicXmlNode();
        text.Kind = XmlNodeKind.Text;
        text.Value = value;
        element.Children.Add(text);
    }

    internal static void AddPartyNode(DynamicXmlNode parent, string localName, string value, string namespaceUri)
    {
        if (value.Length == 0)
        {
            return;
        }
        DynamicXmlNode party = AddElementNode(parent, localName, namespaceUri);
        AddTextNode(party, "Nm", value, namespaceUri);
    }

    internal static void AddAccountNode(DynamicXmlNode parent, string localName, string value, string namespaceUri)
    {
        if (value.Length == 0)
        {
            return;
        }
        DynamicXmlNode account = AddElementNode(parent, localName, namespaceUri);
        AddIdentifierChoiceNode(account, value, namespaceUri);
    }

    internal static void AddIdentifierChoiceNode(DynamicXmlNode parent, string value, string namespaceUri)
    {
        DynamicXmlNode identifier = AddElementNode(parent, "Id", namespaceUri);
        DynamicXmlNode other = AddElementNode(identifier, "Othr", namespaceUri);
        AddTextNode(other, "Id", value, namespaceUri);
    }

    internal static void AddAmountNode(DynamicXmlNode parent, string localName, IsoAmountValue amount, string namespaceUri)
    {
        if (amount == null || amount.Currency.Length == 0)
        {
            return;
        }
        string? lexical = DecimalLexical.FormatDecimal(amount.Amount);
        if (lexical == null)
        {
            return;
        }
        DynamicXmlNode element = AddElementNode(parent, localName, namespaceUri);
        XmlAttributeData currency = new XmlAttributeData();
        currency.Name.LocalName = "Ccy";
        currency.Value = amount.Currency;
        currency.IsSpecified = true;
        element.Attributes.Add(currency);
        DynamicXmlNode text = new DynamicXmlNode();
        text.Kind = XmlNodeKind.Text;
        text.Value = lexical;
        element.Children.Add(text);
    }

    internal static void AddDateNode(DynamicXmlNode parent, string localName, IsoDateValue value, string namespaceUri)
    {
        string? lexical = DateTimeLexical.FormatDate(value);
        if (lexical != null)
        {
            AddTextNode(parent, localName, lexical, namespaceUri);
        }
    }

    internal static void AddDateTimeNode(DynamicXmlNode parent, string localName, IsoDateTimeValue value, string namespaceUri)
    {
        string? lexical = DateTimeLexical.FormatDateTime(value);
        if (lexical != null)
        {
            AddTextNode(parent, localName, lexical, namespaceUri);
        }
    }

    internal static void AddDecimalNode(DynamicXmlNode parent, string localName, IsoDecimalValue value, string namespaceUri)
    {
        string? lexical = DecimalLexical.FormatDecimal(value);
        if (lexical != null)
        {
            AddTextNode(parent, localName, lexical, namespaceUri);
        }
    }

    internal static void AddRemittanceNode(DynamicXmlNode parent, string value, string namespaceUri)
    {
        if (value.Length > 0)
        {
            DynamicXmlNode remittance = AddElementNode(parent, "RmtInf", namespaceUri);
            AddTextNode(remittance, "Ustrd", value, namespaceUri);
        }
    }

    internal static MessageDocument CreateMessageDocument(BuilderRequest request, BuilderTarget target, DynamicXmlNode payloadTree)
    {
        MessagePayload payload = new MessagePayload();
        payload.Identifier = target.Identifier;
        payload.GenericContent = payloadTree;
        NamespaceDescriptor payloadNamespace = new NamespaceDescriptor();
        payloadNamespace.Uri = target.NamespaceUri;
        payloadNamespace.Kind = NamespaceKind.Message;
        payloadNamespace.Mode = NamespaceMode.Strict;
        payloadNamespace.MessageIdentifier = target.Identifier;
        payload.Namespace = payloadNamespace;

        MessageDocument document = new MessageDocument();
        document.Layout = request.Layout;
        document.Header = request.Header;
        document.HeaderPrecedesPayload = true;
        document.Payloads.Add(payload);
        document.Metadata.MessageIdentifier = target.Identifier;
        document.Metadata.NamespaceUri = target.NamespaceUri;
        if (request.Header != null)
        {
            document.Metadata.BusinessMessageIdentifier = request.Header.BusinessMessageIdentifier;
            document.Metadata.BusinessService = request.Header.BusinessService;
        }
        if (request.Layout == EnvelopeKind.BusinessMessageEnvelope)
        {
            MessageEnvelope envelope = new MessageEnvelope();
            envelope.Kind = EnvelopeKind.BusinessMessageEnvelope;
            envelope.Name.LocalName = "BizMsgEnvlp";
            envelope.Name.NamespaceUri = Iso20022Namespaces.SchemaUrnPrefix + "nvlp.001.001.02";
            envelope.Header = request.Header;
            envelope.Payloads.Add(payload);
            document.Envelope = envelope;
        }
        return document;
    }

    internal static void ApplySemanticMetadata(MessageMetadata metadata, SemanticTemplateDefinition definition, object input)
    {
        if (definition.Kind == SemanticTemplateRegistry.PaymentInitiation)
        {
            PaymentInitiationTemplate? payment = input as PaymentInitiationTemplate;
            if (payment != null)
            {
                metadata.PrimaryAmount = payment.Amount;
                metadata.ValueDate = payment.RequestedExecutionDate;
                metadata.References.Add(payment.MessageIdentifier);
                metadata.References.Add(payment.PaymentInformationIdentifier);
                metadata.References.Add(payment.EndToEndIdentifier);
            }
        }
        else if (definition.Kind == SemanticTemplateRegistry.PaymentStatus)
        {
            PaymentStatusTemplate? status = input as PaymentStatusTemplate;
            if (status != null)
            {
                metadata.References.Add(status.MessageIdentifier);
                metadata.References.Add(status.OriginalMessageIdentifier);
                if (status.OriginalEndToEndIdentifier.Length > 0)
                {
                    metadata.References.Add(status.OriginalEndToEndIdentifier);
                }
            }
        }
        else if (definition.Kind == SemanticTemplateRegistry.InterbankTransfer)
        {
            InterbankTransferTemplate? transfer = input as InterbankTransferTemplate;
            if (transfer != null)
            {
                metadata.PrimaryAmount = transfer.SettlementAmount;
                metadata.ValueDate = transfer.SettlementDate;
                metadata.Uetr = transfer.Uetr;
                metadata.References.Add(transfer.MessageIdentifier);
                metadata.References.Add(transfer.EndToEndIdentifier);
                metadata.References.Add(transfer.TransactionIdentifier);
            }
        }
        else if (definition.Kind == SemanticTemplateRegistry.CashManagement)
        {
            CashManagementTemplate? cash = input as CashManagementTemplate;
            if (cash != null)
            {
                metadata.PrimaryAmount = cash.BalanceAmount;
                metadata.ValueDate = cash.BalanceDate;
                metadata.References.Add(cash.MessageIdentifier);
                metadata.References.Add(cash.ReportIdentifier);
                metadata.References.Add(cash.AccountIdentifier);
            }
        }
        else if (definition.Kind == SemanticTemplateRegistry.Securities)
        {
            SecuritiesTemplate? securities = input as SecuritiesTemplate;
            if (securities != null)
            {
                metadata.PrimaryAmount = securities.SettlementAmount;
                metadata.ValueDate = securities.SettlementDate;
                metadata.References.Add(securities.MessageIdentifier);
                metadata.References.Add(securities.TransactionIdentifier);
                metadata.References.Add(securities.SecurityIdentifier);
            }
        }
        else if (definition.Kind == SemanticTemplateRegistry.Card)
        {
            CardTemplate? card = input as CardTemplate;
            if (card != null)
            {
                metadata.PrimaryAmount = card.Amount;
                metadata.References.Add(card.MessageIdentifier);
                metadata.References.Add(card.TransactionIdentifier);
                metadata.References.Add(card.MerchantIdentifier);
            }
        }
        else if (definition.Kind == SemanticTemplateRegistry.ForeignExchange)
        {
            ForeignExchangeTemplate? exchange = input as ForeignExchangeTemplate;
            if (exchange != null)
            {
                metadata.PrimaryAmount = exchange.BoughtAmount;
                metadata.ValueDate = exchange.SettlementDate;
                metadata.References.Add(exchange.MessageIdentifier);
                metadata.References.Add(exchange.TradeIdentifier);
            }
        }
        else if (definition.Kind == SemanticTemplateRegistry.Trade)
        {
            TradeTemplate? trade = input as TradeTemplate;
            if (trade != null)
            {
                metadata.PrimaryAmount = trade.Amount;
                metadata.ValueDate = trade.DueDate;
                metadata.References.Add(trade.MessageIdentifier);
                metadata.References.Add(trade.TradeIdentifier);
                metadata.References.Add(trade.BaselineIdentifier);
            }
        }
    }

    internal static BuilderResult CreateBaseSuccessResult(SemanticTemplateDefinition definition, BuilderTarget target, MessageDocument document)
    {
        BuilderResult result = new BuilderResult();
        result.Success = true;
        result.TemplateIdentifier = definition.Identifier;
        result.TargetMessageIdentifier = target.Identifier;
        result.TargetNamespaceUri = target.NamespaceUri;
        result.Document = document;
        return result;
    }

    internal static BuilderResult CreateFailureResult(string code, string message, string templateIdentifier)
    {
        BuilderResult result = new BuilderResult();
        result.TemplateIdentifier = templateIdentifier;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        AddConstructionDiagnostic(result, code, message);
        return result;
    }

    internal static void AddConstructionDiagnostic(BuilderResult result, string code, string message)
    {
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = code;
        diagnostic.RuleIdentifier = code;
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Stage = ValidationStage.Semantic;
        diagnostic.Kind = DiagnosticKind.Configuration;
        diagnostic.Message = message;
        result.Diagnostics.Add(diagnostic);
    }

    internal static void AddValidationDiagnostics(BuilderResult result, ValidationReport report)
    {
        int index = 0;
        while (index < report.Diagnostics.Count)
        {
            result.Diagnostics.Add(report.Diagnostics[index]);
            index++;
        }
    }
}
