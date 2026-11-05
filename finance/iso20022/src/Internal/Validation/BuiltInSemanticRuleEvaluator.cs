using System;
using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class BuiltInSemanticRuleEvaluator
{
    internal static List<ISemanticRule> CreateRules()
    {
        List<ISemanticRule> rules = new List<ISemanticRule>();
        rules.Add(new BuiltInSemanticRule(BuiltInSemanticRule.TransactionCount));
        rules.Add(new BuiltInSemanticRule(BuiltInSemanticRule.ControlSum));
        rules.Add(new BuiltInSemanticRule(BuiltInSemanticRule.AmountCurrency));
        rules.Add(new BuiltInSemanticRule(BuiltInSemanticRule.Chronology));
        rules.Add(new BuiltInSemanticRule(BuiltInSemanticRule.ReferenceUniqueness));
        rules.Add(new BuiltInSemanticRule(BuiltInSemanticRule.HeaderBodyMatch));
        rules.Add(new BuiltInSemanticRule(
            BuiltInSemanticRule.BusinessIdentifierUniqueness));
        return rules;
    }

    internal static SemanticRuleDescriptor CreateDescriptor(int kind)
    {
        SemanticRuleDescriptor descriptor = new SemanticRuleDescriptor();
        descriptor.Enabled = true;
        descriptor.Severity = ValidationSeverity.Error;
        descriptor.SourceIdentifier = "iso20022.builtin";
        switch (kind)
        {
            case BuiltInSemanticRule.TransactionCount:
                ApplyDescriptor(descriptor, "iso20022.semantic.transaction_count",
                    "Transaction count consistency",
                    "Declared transaction counts must equal the contained transactions.",
                    -700);
                break;
            case BuiltInSemanticRule.ControlSum:
                ApplyDescriptor(descriptor, "iso20022.semantic.control_sum",
                    "Control sum consistency",
                    "Declared control sums must equal the contained transaction amounts.",
                    -600);
                break;
            case BuiltInSemanticRule.AmountCurrency:
                ApplyDescriptor(descriptor, "iso20022.semantic.amount_currency",
                    "Amount and currency consistency",
                    "Monetary amounts must use valid and unambiguous currencies.",
                    -500);
                break;
            case BuiltInSemanticRule.Chronology:
                ApplyDescriptor(descriptor, "iso20022.semantic.chronology",
                    "Chronological ordering",
                    "Creation and start dates must not follow related settlement or end dates.",
                    -400);
                break;
            case BuiltInSemanticRule.ReferenceUniqueness:
                ApplyDescriptor(descriptor, "iso20022.semantic.reference_unique",
                    "Reference uniqueness",
                    "Transaction references of the same kind must be unique.",
                    -300);
                break;
            case BuiltInSemanticRule.HeaderBodyMatch:
                ApplyDescriptor(descriptor, "iso20022.semantic.header_body",
                    "Header and body message definition match",
                    "The application-header message definition must match the payload.",
                    -200);
                break;
            case BuiltInSemanticRule.BusinessIdentifierUniqueness:
                ApplyDescriptor(descriptor, "iso20022.semantic.business_id_unique",
                    "Business message identifier uniqueness",
                    "Business message identifiers must be unique within one message envelope.",
                    -100);
                break;
            default:
                ApplyDescriptor(descriptor, "iso20022.semantic.unknown",
                    "Unknown built-in semantic rule",
                    "The built-in semantic rule kind must be recognized.",
                    0);
                descriptor.Enabled = false;
                break;
        }
        return descriptor;
    }

    internal static void ApplyDescriptor(
        SemanticRuleDescriptor descriptor,
        string identifier,
        string name,
        string definition,
        int order)
    {
        descriptor.Identifier = identifier;
        descriptor.Name = name;
        descriptor.Definition = definition;
        descriptor.Order = order;
    }

    internal static List<ValidationDiagnostic> EvaluateRule(
        ValidationRequest? request,
        int kind)
    {
        List<ValidationDiagnostic> diagnostics = new List<ValidationDiagnostic>();
        if (request == null || request.Document == null)
        {
            return diagnostics;
        }
        BuiltInSemanticIndex index = BuildIndex(request);
        if (!index.Continue)
        {
            return index.Diagnostics;
        }
        switch (kind)
        {
            case BuiltInSemanticRule.TransactionCount:
                ValidateTransactionCounts(index);
                break;
            case BuiltInSemanticRule.ControlSum:
                ValidateControlSums(index);
                break;
            case BuiltInSemanticRule.AmountCurrency:
                ValidateAmountCurrencies(index);
                break;
            case BuiltInSemanticRule.Chronology:
                ValidateChronology(index);
                break;
            case BuiltInSemanticRule.ReferenceUniqueness:
                ValidateReferenceUniqueness(index);
                break;
            case BuiltInSemanticRule.HeaderBodyMatch:
                ValidateHeaderBodyMatch(index);
                break;
            case BuiltInSemanticRule.BusinessIdentifierUniqueness:
                ValidateBusinessIdentifierUniqueness(index);
                break;
            default:
                break;
        }
        return index.Diagnostics;
    }

    internal static BuiltInSemanticIndex BuildIndex(ValidationRequest request)
    {
        BuiltInSemanticIndex index = new BuiltInSemanticIndex();
        index.Request = request;
        index.Limits = request.Policy.Limits;
        MessageDocument? document = request.Document;
        List<DynamicXmlNode> roots = new List<DynamicXmlNode>();
        if (document != null)
        {
            roots = CollectRoots(document);
        }
        int rootIndex = 0;
        while (rootIndex < roots.Count && index.Continue)
        {
            IndexNode(index, roots[rootIndex], null, string.Empty, 1);
            rootIndex++;
        }
        return index;
    }

    internal static List<DynamicXmlNode> CollectRoots(MessageDocument document)
    {
        List<DynamicXmlNode> roots = new List<DynamicXmlNode>();
        if (document.Envelope != null && document.Envelope.GenericContent != null)
        {
            AddRoot(roots, document.Envelope.GenericContent);
            return roots;
        }
        if (document.Header != null && document.Header.GenericContent != null)
        {
            AddRoot(roots, document.Header.GenericContent);
        }
        int payloadIndex = 0;
        while (payloadIndex < document.Payloads.Count)
        {
            DynamicXmlNode? content = document.Payloads[payloadIndex].GenericContent;
            if (content != null)
            {
                AddRoot(roots, content);
            }
            payloadIndex++;
        }
        return roots;
    }

    internal static void AddRoot(
        List<DynamicXmlNode> roots,
        DynamicXmlNode node)
    {
        int index = 0;
        while (index < roots.Count)
        {
            if (roots[index] == node)
            {
                return;
            }
            index++;
        }
        roots.Add(node);
    }

    internal static void IndexNode(
        BuiltInSemanticIndex index,
        DynamicXmlNode node,
        SemanticXmlElement? parent,
        string parentPath,
        int depth)
    {
        if (!index.Continue)
        {
            return;
        }
        if (depth > index.Limits.MaxDepth)
        {
            AddIndexResourceDiagnostic(index, parentPath);
            return;
        }
        SemanticXmlElement? nextParent = parent;
        string nextPath = parentPath;
        if (node.Kind == XmlNodeKind.Element)
        {
            index.ElementCount++;
            if (index.ElementCount > index.Limits.MaxElementCount)
            {
                AddIndexResourceDiagnostic(index, parentPath);
                return;
            }
            SemanticXmlElement element = new SemanticXmlElement();
            element.Node = node;
            element.Parent = parent;
            element.Path = parentPath + "/" + node.Name.LocalName;
            if (parent != null)
            {
                parent.Children.Add(element);
            }
            index.Elements.Add(element);
            nextParent = element;
            nextPath = element.Path;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Count && index.Continue)
        {
            IndexNode(
                index,
                node.Children[childIndex],
                nextParent,
                nextPath,
                depth + 1);
            childIndex++;
        }
    }

    internal static void AddIndexResourceDiagnostic(
        BuiltInSemanticIndex index,
        string path)
    {
        index.Diagnostics.Add(CreateDiagnostic(
            "validation.semantic.resource_limit",
            "iso20022.semantic.resource",
            DiagnosticKind.ResourceLimit,
            "Generic semantic validation exceeded a configured resource limit.",
            path,
            string.Empty,
            "A bounded generic message tree.",
            null));
        index.Continue = false;
    }

    internal static void ValidateTransactionCounts(BuiltInSemanticIndex index)
    {
        int elementIndex = 0;
        while (elementIndex < index.Elements.Count)
        {
            SemanticXmlElement claim = index.Elements[elementIndex];
            if (claim.Node.Name.LocalName == "NbOfTxs")
            {
                int expected = ParseNonnegativeInteger(CollectElementText(claim.Node));
                if (expected >= 0)
                {
                    SemanticXmlElement? scope = FindClaimScope(claim);
                    List<SemanticXmlElement> transactions = CollectTransactions(
                        index,
                        scope);
                    if (expected != transactions.Count)
                    {
                        index.Diagnostics.Add(CreateDiagnostic(
                            "validation.semantic.transaction_count",
                            "iso20022.semantic.transaction_count",
                            DiagnosticKind.ValidationFailure,
                            "The declared transaction count does not equal the contained transactions.",
                            claim.Path,
                            expected.ToString(
                                System.Globalization.CultureInfo.InvariantCulture),
                            transactions.Count.ToString(
                                System.Globalization.CultureInfo.InvariantCulture),
                            claim.Node));
                    }
                }
            }
            elementIndex++;
        }
    }

    internal static void ValidateControlSums(BuiltInSemanticIndex index)
    {
        int elementIndex = 0;
        while (elementIndex < index.Elements.Count)
        {
            SemanticXmlElement claim = index.Elements[elementIndex];
            if (claim.Node.Name.LocalName == "CtrlSum")
            {
                IsoDecimalValue? expected = DecimalLexical.ParseDecimal(
                    CollectElementText(claim.Node).AsSpan());
                SemanticXmlElement? scope = FindClaimScope(claim);
                List<SemanticXmlElement> transactions = CollectTransactions(index, scope);
                decimal actual = 0m;
                bool complete = transactions.Count > 0;
                int transactionIndex = 0;
                while (transactionIndex < transactions.Count)
                {
                    IsoDecimalValue? amount = FindTransactionAmount(
                        index,
                        transactions[transactionIndex]);
                    if (amount == null)
                    {
                        complete = false;
                    }
                    else
                    {
                        actual += amount.Value;
                    }
                    transactionIndex++;
                }
                if (expected != null && complete && expected.Value != actual)
                {
                    index.Diagnostics.Add(CreateDiagnostic(
                        "validation.semantic.control_sum",
                        "iso20022.semantic.control_sum",
                        DiagnosticKind.ValidationFailure,
                        "The declared control sum does not equal the transaction amount sum.",
                        claim.Path,
                        expected.LexicalValue,
                        actual.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        claim.Node));
                }
            }
            elementIndex++;
        }
    }

    internal static void ValidateAmountCurrencies(BuiltInSemanticIndex index)
    {
        int elementIndex = 0;
        while (elementIndex < index.Elements.Count)
        {
            SemanticXmlElement element = index.Elements[elementIndex];
            if (IsMonetaryAmountName(element.Node.Name.LocalName))
            {
                IsoDecimalValue? amount = DecimalLexical.ParseDecimal(
                    CollectElementText(element.Node).AsSpan());
                if (amount != null)
                {
                    string currency = FindAttribute(element.Node, "Ccy");
                    if (!CurrencyIsValid(currency))
                    {
                        index.Diagnostics.Add(CreateDiagnostic(
                            "validation.semantic.amount_currency",
                            "iso20022.semantic.amount_currency",
                            DiagnosticKind.ValidationFailure,
                            "A monetary amount requires a three-letter uppercase currency code.",
                            element.Path,
                            currency,
                            "One ISO-style three-letter uppercase currency code.",
                            element.Node));
                    }
                }
            }
            elementIndex++;
        }
        ValidateControlSumCurrencies(index);
    }

    internal static void ValidateControlSumCurrencies(BuiltInSemanticIndex index)
    {
        int elementIndex = 0;
        while (elementIndex < index.Elements.Count)
        {
            SemanticXmlElement claim = index.Elements[elementIndex];
            if (claim.Node.Name.LocalName == "CtrlSum")
            {
                List<SemanticXmlElement> transactions = CollectTransactions(
                    index,
                    FindClaimScope(claim));
                List<string> currencies = new List<string>();
                int transactionIndex = 0;
                while (transactionIndex < transactions.Count)
                {
                    SemanticXmlElement? amount = FindTransactionAmountElement(
                        index,
                        transactions[transactionIndex]);
                    if (amount != null)
                    {
                        string currency = FindAttribute(amount.Node, "Ccy");
                        if (currency.Length > 0 && !TextListContains(currencies, currency))
                        {
                            currencies.Add(currency);
                        }
                    }
                    transactionIndex++;
                }
                if (currencies.Count > 1)
                {
                    index.Diagnostics.Add(CreateDiagnostic(
                        "validation.semantic.amount_currency_mixed",
                        "iso20022.semantic.amount_currency",
                        DiagnosticKind.ValidationFailure,
                        "One control sum cannot unambiguously aggregate multiple currencies.",
                        claim.Path,
                        JoinText(currencies),
                        "One transaction currency for the declared control sum.",
                        claim.Node));
                }
            }
            elementIndex++;
        }
    }

    internal static void ValidateChronology(BuiltInSemanticIndex index)
    {
        SemanticXmlElement? creation = FindFirstNamedElement(
            index,
            "CreDtTm",
            "CreDt");
        if (creation != null)
        {
            IsoDateValue? creationDate = ParseSemanticDate(
                CollectElementText(creation.Node));
            int elementIndex = 0;
            while (elementIndex < index.Elements.Count)
            {
                SemanticXmlElement target = index.Elements[elementIndex];
                if (IsFutureDateName(target.Node.Name.LocalName))
                {
                    IsoDateValue? targetDate = ParseSemanticDate(
                        CollectElementText(target.Node));
                    if (creationDate != null && targetDate != null &&
                        CompareDates(creationDate, targetDate) > 0)
                    {
                        index.Diagnostics.Add(CreateDiagnostic(
                            "validation.semantic.chronology",
                            "iso20022.semantic.chronology",
                            DiagnosticKind.ValidationFailure,
                            "The message creation date follows a related execution or settlement date.",
                            target.Path,
                            targetDate.LexicalValue,
                            "A date on or after " + creationDate.LexicalValue + ".",
                            target.Node));
                        return;
                    }
                }
                elementIndex++;
            }
        }
        ValidateNamedDatePairs(index, "FrDt", "ToDt");
        ValidateNamedDatePairs(index, "StartDt", "EndDt");
    }

    internal static void ValidateNamedDatePairs(
        BuiltInSemanticIndex index,
        string startName,
        string endName)
    {
        int elementIndex = 0;
        while (elementIndex < index.Elements.Count)
        {
            SemanticXmlElement owner = index.Elements[elementIndex];
            SemanticXmlElement? start = FindDirectChild(owner, startName);
            SemanticXmlElement? end = FindDirectChild(owner, endName);
            if (start != null && end != null)
            {
                IsoDateValue? startDate = ParseSemanticDate(
                    CollectElementText(start.Node));
                IsoDateValue? endDate = ParseSemanticDate(CollectElementText(end.Node));
                if (startDate != null && endDate != null &&
                    CompareDates(startDate, endDate) > 0)
                {
                    index.Diagnostics.Add(CreateDiagnostic(
                        "validation.semantic.chronology",
                        "iso20022.semantic.chronology",
                        DiagnosticKind.ValidationFailure,
                        "A start date follows its related end date.",
                        end.Path,
                        endDate.LexicalValue,
                        "A date on or after " + startDate.LexicalValue + ".",
                        end.Node));
                    return;
                }
            }
            elementIndex++;
        }
    }

    internal static void ValidateReferenceUniqueness(BuiltInSemanticIndex index)
    {
        List<string> values = new List<string>();
        int elementIndex = 0;
        while (elementIndex < index.Elements.Count)
        {
            SemanticXmlElement element = index.Elements[elementIndex];
            if (IsReferenceName(element.Node.Name.LocalName))
            {
                string value = NamespaceService.TrimXmlWhitespace(
                    CollectElementText(element.Node));
                string key = element.Node.Name.LocalName + "\n" + value;
                if (value.Length > 0 && value != "NOTPROVIDED")
                {
                    if (TextListContains(values, key))
                    {
                        index.Diagnostics.Add(CreateDiagnostic(
                            "validation.semantic.reference_duplicate",
                            "iso20022.semantic.reference_unique",
                            DiagnosticKind.Conflict,
                            "A transaction reference is duplicated within the message.",
                            element.Path,
                            value,
                            "A unique " + element.Node.Name.LocalName + " value.",
                            element.Node));
                    }
                    else
                    {
                        values.Add(key);
                    }
                }
            }
            elementIndex++;
        }
    }

    internal static void ValidateHeaderBodyMatch(BuiltInSemanticIndex index)
    {
        MessageDocument? document = index.Request.Document;
        if (document == null || document.Header == null ||
            document.Header.GenericContent == null)
        {
            return;
        }
        string declared = FindFirstNodeText(
            document.Header.GenericContent,
            "MsgDefIdr");
        MessageIdentifier? actual = null;
        if (document.Payloads.Count > 0)
        {
            actual = document.Payloads[0].Identifier;
        }
        if (actual == null)
        {
            actual = document.Metadata.MessageIdentifier;
        }
        if (declared.Length > 0 && actual != null &&
            declared != MessageValidator.GetIdentifierText(actual))
        {
            SemanticXmlElement? declaration = FindFirstNamedElement(
                index,
                "MsgDefIdr",
                string.Empty);
            index.Diagnostics.Add(CreateDiagnostic(
                "validation.semantic.header_body_identifier",
                "iso20022.semantic.header_body",
                DiagnosticKind.Conflict,
                "The application-header message definition does not match the payload.",
                declaration == null ? "/AppHdr/MsgDefIdr" : declaration.Path,
                declared,
                MessageValidator.GetIdentifierText(actual),
                declaration == null ? document.Header.GenericContent : declaration.Node));
        }
    }

    internal static void ValidateBusinessIdentifierUniqueness(
        BuiltInSemanticIndex index)
    {
        List<string> values = new List<string>();
        int elementIndex = 0;
        while (elementIndex < index.Elements.Count)
        {
            SemanticXmlElement element = index.Elements[elementIndex];
            if (element.Node.Name.LocalName == "BizMsgIdr")
            {
                string value = NamespaceService.TrimXmlWhitespace(
                    CollectElementText(element.Node));
                if (value.Length > 0)
                {
                    if (TextListContains(values, value))
                    {
                        index.Diagnostics.Add(CreateDiagnostic(
                            "validation.semantic.business_identifier_duplicate",
                            "iso20022.semantic.business_id_unique",
                            DiagnosticKind.Conflict,
                            "A business message identifier is duplicated within the envelope.",
                            element.Path,
                            value,
                            "A unique business message identifier.",
                            element.Node));
                    }
                    else
                    {
                        values.Add(value);
                    }
                }
            }
            elementIndex++;
        }
    }

    internal static SemanticXmlElement? FindClaimScope(SemanticXmlElement claim)
    {
        SemanticXmlElement? owner = claim.Parent;
        if (owner != null && owner.Node.Name.LocalName == "GrpHdr" &&
            owner.Parent != null)
        {
            return owner.Parent;
        }
        return owner;
    }

    internal static List<SemanticXmlElement> CollectTransactions(
        BuiltInSemanticIndex index,
        SemanticXmlElement? scope)
    {
        List<SemanticXmlElement> transactions = new List<SemanticXmlElement>();
        if (scope == null)
        {
            return transactions;
        }
        int elementIndex = 0;
        while (elementIndex < index.Elements.Count)
        {
            SemanticXmlElement element = index.Elements[elementIndex];
            if (IsTransactionName(element.Node.Name.LocalName) &&
                IsDescendantOf(element, scope) &&
                !HasTransactionAncestor(element, scope))
            {
                transactions.Add(element);
            }
            elementIndex++;
        }
        return transactions;
    }

    internal static bool IsTransactionName(string name)
    {
        return name == "CdtTrfTxInf" || name == "DrctDbtTxInf" ||
            name == "TxInfAndSts" || name == "TxDtls" ||
            name == "CdtTrfTx" || name == "PmtTx";
    }

    internal static bool IsDescendantOf(
        SemanticXmlElement element,
        SemanticXmlElement ancestor)
    {
        SemanticXmlElement? current = element.Parent;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    internal static bool HasTransactionAncestor(
        SemanticXmlElement element,
        SemanticXmlElement scope)
    {
        SemanticXmlElement? current = element.Parent;
        while (current != null && current != scope)
        {
            if (IsTransactionName(current.Node.Name.LocalName))
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    internal static IsoDecimalValue? FindTransactionAmount(
        BuiltInSemanticIndex index,
        SemanticXmlElement transaction)
    {
        SemanticXmlElement? element = FindTransactionAmountElement(index, transaction);
        if (element == null)
        {
            return null;
        }
        return DecimalLexical.ParseDecimal(CollectElementText(element.Node).AsSpan());
    }

    internal static SemanticXmlElement? FindTransactionAmountElement(
        BuiltInSemanticIndex index,
        SemanticXmlElement transaction)
    {
        SemanticXmlElement? element = FindDescendantNamed(
            index,
            transaction,
            "IntrBkSttlmAmt");
        if (element == null)
        {
            element = FindDescendantNamed(index, transaction, "InstdAmt");
        }
        if (element == null)
        {
            element = FindDescendantNamed(index, transaction, "EqvtAmt");
        }
        return element;
    }

    internal static SemanticXmlElement? FindDescendantNamed(
        BuiltInSemanticIndex index,
        SemanticXmlElement owner,
        string name)
    {
        int elementIndex = 0;
        while (elementIndex < index.Elements.Count)
        {
            SemanticXmlElement element = index.Elements[elementIndex];
            if (element.Node.Name.LocalName == name && IsDescendantOf(element, owner))
            {
                return element;
            }
            elementIndex++;
        }
        return null;
    }

    internal static SemanticXmlElement? FindDirectChild(
        SemanticXmlElement owner,
        string name)
    {
        int index = 0;
        while (index < owner.Children.Count)
        {
            if (owner.Children[index].Node.Name.LocalName == name)
            {
                return owner.Children[index];
            }
            index++;
        }
        return null;
    }

    internal static SemanticXmlElement? FindFirstNamedElement(
        BuiltInSemanticIndex index,
        string firstName,
        string secondName)
    {
        int elementIndex = 0;
        while (elementIndex < index.Elements.Count)
        {
            string name = index.Elements[elementIndex].Node.Name.LocalName;
            if (name == firstName || (secondName.Length > 0 && name == secondName))
            {
                return index.Elements[elementIndex];
            }
            elementIndex++;
        }
        return null;
    }

    internal static int ParseNonnegativeInteger(string text)
    {
        string value = NamespaceService.TrimXmlWhitespace(text);
        if (value.Length == 0)
        {
            return -1;
        }
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return -1;
            }
            if (result > (int.MaxValue - (value[index] - '0')) / 10)
            {
                return -1;
            }
            result = result * 10 + (value[index] - '0');
            index++;
        }
        return result;
    }

    internal static bool IsMonetaryAmountName(string name)
    {
        return name == "IntrBkSttlmAmt" || name == "InstdAmt" ||
            name == "EqvtAmt" || name == "ChrgsInfAmt";
    }

    internal static string FindAttribute(DynamicXmlNode node, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            if (node.Attributes[index].Name.LocalName == localName)
            {
                return node.Attributes[index].Value;
            }
            index++;
        }
        return string.Empty;
    }

    internal static bool CurrencyIsValid(string currency)
    {
        return currency.Length == 3 &&
            currency[0] >= 'A' && currency[0] <= 'Z' &&
            currency[1] >= 'A' && currency[1] <= 'Z' &&
            currency[2] >= 'A' && currency[2] <= 'Z';
    }

    internal static bool IsFutureDateName(string name)
    {
        return name == "IntrBkSttlmDt" || name == "ReqdExctnDt" ||
            name == "ReqdColltnDt" || name == "SttlmDt";
    }

    internal static IsoDateValue? ParseSemanticDate(string text)
    {
        string value = NamespaceService.TrimXmlWhitespace(text);
        if (value.IndexOf('T') >= 0)
        {
            IsoDateTimeValue? dateTime = DateTimeLexical.ParseDateTime(value.AsSpan());
            if (dateTime != null)
            {
                return dateTime.Date;
            }
            return null;
        }
        return DateTimeLexical.ParseDate(value.AsSpan());
    }

    internal static int CompareDates(IsoDateValue left, IsoDateValue right)
    {
        if (left.Year != right.Year)
        {
            return left.Year < right.Year ? -1 : 1;
        }
        if (left.Month != right.Month)
        {
            return left.Month < right.Month ? -1 : 1;
        }
        if (left.Day != right.Day)
        {
            return left.Day < right.Day ? -1 : 1;
        }
        return 0;
    }

    internal static bool IsReferenceName(string name)
    {
        return name == "InstrId" || name == "EndToEndId" ||
            name == "TxId" || name == "UETR";
    }

    internal static string FindFirstNodeText(
        DynamicXmlNode node,
        string localName)
    {
        if (node.Kind == XmlNodeKind.Element && node.Name.LocalName == localName)
        {
            return NamespaceService.TrimXmlWhitespace(CollectElementText(node));
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            string value = FindFirstNodeText(node.Children[index], localName);
            if (value.Length > 0)
            {
                return value;
            }
            index++;
        }
        return string.Empty;
    }

    internal static string CollectElementText(DynamicXmlNode node)
    {
        StringBuilder builder = new StringBuilder();
        AppendElementText(node, builder);
        return builder.ToString();
    }

    internal static void AppendElementText(
        DynamicXmlNode node,
        StringBuilder builder)
    {
        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            builder.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            AppendElementText(node.Children[index], builder);
            index++;
        }
    }

    internal static bool TextListContains(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static string JoinText(List<string> values)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0)
            {
                builder.Append(',');
            }
            builder.Append(values[index]);
            index++;
        }
        return builder.ToString();
    }

    internal static ValidationDiagnostic CreateDiagnostic(
        string code,
        string ruleIdentifier,
        DiagnosticKind kind,
        string message,
        string path,
        string offendingValue,
        string expectedConstraint,
        DynamicXmlNode? node)
    {
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = code;
        diagnostic.RuleIdentifier = ruleIdentifier;
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Kind = kind;
        diagnostic.Message = message;
        diagnostic.Path.Value = path;
        diagnostic.OffendingValue = offendingValue;
        diagnostic.ExpectedConstraint = expectedConstraint;
        diagnostic.SourceIdentifier = "iso20022.builtin";
        if (node != null)
        {
            diagnostic.Location = node.Location;
        }
        return diagnostic;
    }
}
