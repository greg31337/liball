namespace iso20022;

internal static class SemanticTemplateRegistry
{
    internal const int PaymentInitiation = 1;
    internal const int PaymentStatus = 2;
    internal const int InterbankTransfer = 3;
    internal const int CashManagement = 4;
    internal const int Securities = 5;
    internal const int Card = 6;
    internal const int ForeignExchange = 7;
    internal const int Trade = 8;

    internal static SemanticTemplateDefinition? ResolveIdentifier(string identifier)
    {
        SemanticTemplateDefinition? definition = null;
        switch (identifier)
        {
            case "payment-initiation":
                definition = CreateDefinition("payment-initiation", PaymentInitiation);
                break;
            case "payment-status":
                definition = CreateDefinition("payment-status", PaymentStatus);
                break;
            case "interbank-credit-transfer":
            case "interbank-transfer":
                definition = CreateDefinition("interbank-credit-transfer", InterbankTransfer);
                break;
            case "cash-management":
            case "cash-statement":
            case "cash-notification":
                definition = CreateDefinition("cash-management", CashManagement);
                break;
            case "securities":
                definition = CreateDefinition("securities", Securities);
                break;
            case "cards":
            case "card":
                definition = CreateDefinition("cards", Card);
                break;
            case "foreign-exchange":
            case "fx":
                definition = CreateDefinition("foreign-exchange", ForeignExchange);
                break;
            case "trade":
            case "trade-services":
                definition = CreateDefinition("trade", Trade);
                break;
        }

        return definition;
    }

    internal static SemanticTemplateDefinition? ResolveInput(object input)
    {
        if (input == null)
        {
            return null;
        }

        if (input as PaymentInitiationTemplate != null)
        {
            return CreateDefinition("payment-initiation", PaymentInitiation);
        }
        if (input as PaymentStatusTemplate != null)
        {
            return CreateDefinition("payment-status", PaymentStatus);
        }
        if (input as InterbankTransferTemplate != null)
        {
            return CreateDefinition("interbank-credit-transfer", InterbankTransfer);
        }
        if (input as CashManagementTemplate != null)
        {
            return CreateDefinition("cash-management", CashManagement);
        }
        if (input as SecuritiesTemplate != null)
        {
            return CreateDefinition("securities", Securities);
        }
        if (input as CardTemplate != null)
        {
            return CreateDefinition("cards", Card);
        }
        if (input as ForeignExchangeTemplate != null)
        {
            return CreateDefinition("foreign-exchange", ForeignExchange);
        }
        if (input as TradeTemplate != null)
        {
            return CreateDefinition("trade", Trade);
        }

        return null;
    }

    internal static bool SupportsTarget(SemanticTemplateDefinition definition, MessageIdentifier identifier)
    {
        string area = identifier.BusinessArea;
        string functionality = identifier.MessageFunctionality;
        switch (definition.Kind)
        {
            case PaymentInitiation:
                return area == "pain" && functionality == "001";
            case PaymentStatus:
                return (area == "pain" || area == "pacs") && functionality == "002";
            case InterbankTransfer:
                return area == "pacs" && (functionality == "008" || functionality == "009");
            case CashManagement:
                return area == "camt";
            case Securities:
                return area.Length == 4 && area[0] == 's' && area[1] == 'e';
            case Card:
                return area.Length == 4 && area[0] == 'c' && area[1] == 'a';
            case ForeignExchange:
                return area == "fxtr";
            case Trade:
                return area == "tsmt";
            default:
                return false;
        }
    }

    internal static string GetDefaultRootName(SemanticTemplateDefinition definition, MessageIdentifier identifier)
    {
        if (definition.Kind == PaymentInitiation)
        {
            return "CstmrCdtTrfInitn";
        }
        if (definition.Kind == PaymentStatus)
        {
            if (identifier.BusinessArea == "pacs")
            {
                return "FIToFIPmtStsRpt";
            }
            return "CstmrPmtStsRpt";
        }
        if (definition.Kind == InterbankTransfer)
        {
            if (identifier.MessageFunctionality == "009")
            {
                return "FICdtTrf";
            }
            return "FIToFICstmrCdtTrf";
        }
        if (definition.Kind == CashManagement)
        {
            switch (identifier.MessageFunctionality)
            {
                case "052":
                    return "BkToCstmrAcctRpt";
                case "053":
                    return "BkToCstmrStmt";
                case "054":
                    return "BkToCstmrDbtCdtNtfctn";
                case "060":
                    return "AcctRptgReq";
                default:
                    return "CashMgmtMsg";
            }
        }
        if (definition.Kind == Securities)
        {
            if (identifier.BusinessArea == "semt" && identifier.MessageFunctionality == "001")
            {
                return "SctiesMsgRjctn";
            }
            return "SctiesMsg";
        }
        if (definition.Kind == Card)
        {
            return "CardMsg";
        }
        if (definition.Kind == ForeignExchange)
        {
            if (identifier.MessageFunctionality == "008")
            {
                return "FXTradStsNtfctn";
            }
            return "FXTradMsg";
        }
        if (definition.Kind == Trade)
        {
            if (identifier.MessageFunctionality == "001")
            {
                return "Ack";
            }
            return "TradSvcMsg";
        }

        return "Message";
    }

    internal static SemanticTemplateDefinition CreateDefinition(string identifier, int kind)
    {
        SemanticTemplateDefinition definition = new SemanticTemplateDefinition();
        definition.Identifier = identifier;
        definition.Kind = kind;
        return definition;
    }
}
