namespace qrcode;

internal class SymbologyIdentifierSelector
{
    // QR and rMQR share code character Q. Micro uses modifier 1 and legacy
    // Model 1 uses 0. For Model 2/rMQR, explicit ECI adds one to the ordinary,
    // FNC1-first, or FNC1-second modifier (1, 3, or 5 respectively).
    // ECI means a designator was parsed, including an explicit default assignment;
    // inferred byte encodings and fixed Kanji mappings do not activate it.
    internal static string GetIdentifier(QrSymbolKind kind, bool hasEci, bool fnc1First, int fnc1SecondApplicationIndicator)
    {
        if (kind != QrSymbolKind.Model2 && kind != QrSymbolKind.Micro
            && kind != QrSymbolKind.rMQR && kind != QrSymbolKind.LegacyModel1)
        {
            throw new QrInputException("The symbol family is unsupported.", "kind");
        }

        if (fnc1SecondApplicationIndicator != -1)
        {
            Fnc1Encoder.ValidateSecondPositionIndicator(fnc1SecondApplicationIndicator);
        }

        bool fnc1Second = fnc1SecondApplicationIndicator != -1;
        if (fnc1First && fnc1Second)
        {
            throw new QrDecodeException("Both FNC1 positions cannot be active.", QrFailureReason.InvalidData, -1, 0);
        }

        if (kind == QrSymbolKind.LegacyModel1 || kind == QrSymbolKind.Micro)
        {
            if (hasEci || fnc1First || fnc1Second)
            {
                throw new QrDecodeException("The symbol family does not support ECI or FNC1 controls.", QrFailureReason.InvalidMode, -1, 0);
            }

            return kind == QrSymbolKind.LegacyModel1 ? QrSymbologyIdentifiers.LegacyModel1 : QrSymbologyIdentifiers.Model2;
        }

        if (fnc1First)
        {
            return hasEci ? QrSymbologyIdentifiers.Model2WithEciAndFnc1First : QrSymbologyIdentifiers.Model2WithFnc1First;
        }

        if (fnc1Second)
        {
            return hasEci ? QrSymbologyIdentifiers.Model2WithEciAndFnc1Second : QrSymbologyIdentifiers.Model2WithFnc1Second;
        }

        return hasEci ? QrSymbologyIdentifiers.Model2WithEci : QrSymbologyIdentifiers.Model2;
    }
}
