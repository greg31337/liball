namespace barcode;

internal static class Gs1ComponentValidator
{
    internal static void ValidateComponent(
        Gs1AiComponentDefinition definition,
        string identifier,
        string value,
        int sourceOffset,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int index = 0;
        while (index < definition.Linters.Length)
        {
            string linterName = definition.Linters[index];
            Gs1LinterResult result = Gs1ComponentLinters.Lint(
                linterName,
                value);
            if (!result.Success)
            {
                BarcodeValidationIssue issue = ValidationIssueFactory.Create(
                    BarcodeValidationSeverity.Error,
                    result.Code,
                    $"The GS1 component does not satisfy the {RuleDescription(linterName)} rule.",
                    Gs1ElementStringParser.StandardName,
                    identifier,
                    sourceOffset + result.ErrorOffset,
                    $"a value satisfying the {RuleDescription(linterName)} rule",
                    ReadObservedValue(value, result));
                issues.Add(issue);
                return;
            }

            index = index + 1;
        }
    }

    static string ReadObservedValue(
        string value,
        Gs1LinterResult result)
    {
        if (result.ErrorOffset < 0
            || result.ErrorOffset >= value.Length
            || result.ErrorLength <= 0)
        {
            return "";
        }

        int length = result.ErrorLength;
        if (result.ErrorOffset + length > value.Length)
        {
            length = value.Length - result.ErrorOffset;
        }

        return value.Substring(result.ErrorOffset, length);
    }

    static string RuleDescription(string linterName)
    {
        switch (linterName)
        {
            case "couponcode":
                return "GS1 coupon code";
            case "couponposoffer":
                return "positive-offer coupon";
            case "csum":
                return "GS1 check digit";
            case "csumalpha":
                return "GS1 alphanumeric check pair";
            case "gcppos1":
                return "GS1 Company Prefix";
            case "gcppos2":
                return "indicator-prefixed GS1 Company Prefix";
            case "hasnondigit":
                return "required non-digit character";
            case "hh":
                return "two-digit hour";
            case "hhmi":
                return "hour and minute";
            case "hyphen":
                return "hyphen";
            case "iban":
                return "IBAN";
            case "importeridx":
                return "importer index";
            case "iso3166":
                return "ISO 3166 numeric country code";
            case "iso3166999":
                return "ISO 3166 numeric country code or 999";
            case "iso3166alpha2":
                return "ISO 3166 alpha-2 country code";
            case "iso4217":
                return "ISO 4217 currency code";
            case "iso5218":
                return "ISO/IEC 5218 biological sex code";
            case "latitude":
                return "latitude";
            case "longitude":
                return "longitude";
            case "mediatype":
                return "AIDC media type";
            case "mi":
                return "two-digit minute";
            case "nonzero":
                return "non-zero value";
            case "nozeroprefix":
                return "non-zero prefix";
            case "packagetype":
                return "package type";
            case "pcenc":
                return "percent encoding";
            case "pieceoftotal":
                return "piece-of-total";
            case "posinseqslash":
                return "position-in-sequence";
            case "ss":
                return "two-digit second";
            case "winding":
                return "winding direction";
            case "yesno":
                return "yes-or-no value";
            case "yymmd0":
                return "YYMMDD date allowing day zero";
            case "yymmdd":
                return "YYMMDD date";
            case "yyyymmdd":
                return "YYYYMMDD date";
            case "zero":
                return "zero value";
            default:
                return linterName;
        }
    }
}
